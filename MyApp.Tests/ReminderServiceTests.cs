using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;
using MyApp.App.Services;
using Xunit;

namespace MyApp.Tests
{
    /// <summary>
    /// Verifies the reminder status rules and the Reminder module data access against
    /// a real (in-memory) SQLite database.
    /// </summary>
    public class ReminderServiceTests
    {
        private static readonly DateTime AsOf = new DateTime(2026, 9, 22);

        [Fact]
        public void StatusRules_ClassifyOverdueUpcomingAndCompleted()
        {
            Assert.Equal(ReminderStatus.Overdue, ReminderStatusRules.GetStatus(NewReminder("Past", AsOf.AddDays(-1)), AsOf));
            Assert.Equal(ReminderStatus.Upcoming, ReminderStatusRules.GetStatus(NewReminder("Today", AsOf), AsOf));
            Assert.Equal(ReminderStatus.Upcoming, ReminderStatusRules.GetStatus(NewReminder("Future", AsOf.AddDays(10)), AsOf));
            Assert.Equal(ReminderStatus.Completed, ReminderStatusRules.GetStatus(NewReminder("Done late", AsOf.AddDays(-5), true), AsOf));
            Assert.Equal(ReminderStatus.Completed, ReminderStatusRules.GetStatus(NewReminder("Done early", AsOf.AddDays(5), true), AsOf));

            Assert.Equal("Overdue", ReminderStatusRules.GetStatusText(ReminderStatus.Overdue));
            Assert.Equal("Upcoming", ReminderStatusRules.GetStatusText(ReminderStatus.Upcoming));
            Assert.Equal("Completed", ReminderStatusRules.GetStatusText(ReminderStatus.Completed));

            Assert.Equal("Today", ReminderStatusRules.GetScheduleText(NewReminder("Today", AsOf), AsOf));
            Assert.Equal("Tomorrow", ReminderStatusRules.GetScheduleText(NewReminder("Tomorrow", AsOf.AddDays(1)), AsOf));
            Assert.Equal("In 3 days", ReminderStatusRules.GetScheduleText(NewReminder("Soon", AsOf.AddDays(3)), AsOf));
            Assert.Equal("5 days overdue", ReminderStatusRules.GetScheduleText(NewReminder("Late", AsOf.AddDays(-5)), AsOf));
            Assert.Equal("Completed", ReminderStatusRules.GetScheduleText(NewReminder("Done", AsOf, true), AsOf));
        }

        [Fact]
        public async Task Reminders_CanBeCreatedEditedCompletedAndReopened()
        {
            using var harness = new ReminderTestHarness();
            var service = harness.ReminderService;

            var reminder = await service.CreateReminderAsync(new Reminder
            {
                Title = "Car insurance renewal",
                ReminderDate = new DateTime(2026, 10, 1),
                ReminderType = ReminderTypes.CarInsurance,
                RelatedEntity = "Vehicle",
                RelatedId = 1,
                Notes = "Renew with the same insurer"
            });

            Assert.True(reminder.Id > 0);

            // Edit
            var loaded = await service.GetReminderByIdAsync(reminder.Id);
            Assert.NotNull(loaded);
            loaded!.Title = "Car insurance renewal (updated)";
            loaded.ReminderDate = new DateTime(2026, 10, 15);
            await service.UpdateReminderAsync(loaded);

            var updated = await service.GetReminderByIdAsync(reminder.Id);
            Assert.Equal("Car insurance renewal (updated)", updated!.Title);
            Assert.Equal(new DateTime(2026, 10, 15), updated.ReminderDate);
            Assert.False(updated.IsCompleted);

            // Mark completed, then reopen
            Assert.True(await service.SetCompletedAsync(reminder.Id, true));
            Assert.True((await service.GetReminderByIdAsync(reminder.Id))!.IsCompleted);

            Assert.True(await service.SetCompletedAsync(reminder.Id, false));
            Assert.False((await service.GetReminderByIdAsync(reminder.Id))!.IsCompleted);

            // An unknown reminder cannot be completed
            Assert.False(await service.SetCompletedAsync(9999, true));
        }

        [Fact]
        public async Task Overviews_GroupByStatusAndSortUpcomingByDate()
        {
            using var harness = new ReminderTestHarness();
            var service = harness.ReminderService;

            await service.CreateReminderAsync(NewReminder("Overdue older", AsOf.AddDays(-10)));
            await service.CreateReminderAsync(NewReminder("Overdue newer", AsOf.AddDays(-2)));
            await service.CreateReminderAsync(NewReminder("Upcoming later", AsOf.AddDays(30)));
            await service.CreateReminderAsync(NewReminder("Upcoming today", AsOf));
            await service.CreateReminderAsync(NewReminder("Completed", AsOf.AddDays(-1), true));

            var overviews = (await service.GetAllRemindersAsync())
                .Select(r => new ReminderOverview(r, AsOf))
                .ToList();

            // Overdue reminders are listed most overdue first
            var overdue = overviews.Where(o => o.IsOverdue).ToList();
            Assert.Equal(new[] { "Overdue older", "Overdue newer" }, overdue.Select(o => o.Reminder.Title));
            Assert.Equal("10 days overdue", overdue[0].ScheduleText);
            Assert.Equal("Overdue", overdue[0].StatusText);

            // Upcoming reminders are sorted by date, soonest first
            var upcoming = overviews.Where(o => o.IsUpcoming).ToList();
            Assert.Equal(new[] { "Upcoming today", "Upcoming later" }, upcoming.Select(o => o.Reminder.Title));
            Assert.Equal("Today", upcoming[0].ScheduleText);

            var completed = overviews.Where(o => o.IsCompleted).ToList();
            Assert.Single(completed);
            Assert.Equal("Completed", completed[0].StatusText);

            // Related record text shown on the Reminders screen
            var related = new Reminder
            {
                Title = "Insurance",
                ReminderDate = AsOf,
                ReminderType = ReminderTypes.CarInsurance,
                RelatedEntity = "Vehicle",
                RelatedId = 3
            };
            Assert.Equal("Vehicle #3", new ReminderOverview(related, AsOf).RelatedDisplay);
            Assert.Equal("-", new ReminderOverview(NewReminder("Plain", AsOf), AsOf).RelatedDisplay);
        }

        private static Reminder NewReminder(string title, DateTime date, bool isCompleted = false)
        {
            return new Reminder
            {
                Title = title,
                ReminderType = ReminderTypes.Other,
                ReminderDate = date,
                IsCompleted = isCompleted
            };
        }

        /// <summary>Keeps an in-memory SQLite database and its services alive for a single test.</summary>
        private sealed class ReminderTestHarness : IDisposable
        {
            private readonly SqliteConnection _connection;

            public ReminderTestHarness()
            {
                _connection = new SqliteConnection("Data Source=:memory:");
                _connection.Open();

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(_connection)
                    .Options;

                Context = new AppDbContext(options);
                Context.Database.EnsureCreated();

                ReminderService = new ReminderService(Context);
            }

            public AppDbContext Context { get; }

            public ReminderService ReminderService { get; }

            public void Dispose()
            {
                Context.Dispose();
                _connection.Dispose();
            }
        }
    }
}
