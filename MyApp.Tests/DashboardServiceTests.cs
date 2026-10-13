using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyApp.App;
using MyApp.App.Data;
using MyApp.App.Models;
using MyApp.App.Services;
using Xunit;

namespace MyApp.Tests
{
    /// <summary>
    /// Verifies the dashboard calculations against a real (in-memory) SQLite database.
    /// </summary>
    public class DashboardServiceTests
    {
        [Fact]
        public async Task SummaryCards_UseActiveAccountsCurrentMonthAndIgnoreTransfers()
        {
            using var harness = new DashboardTestHarness();
            var context = harness.Context;
            var asOf = new DateTime(2026, 9, 22);

            var maybank = new Account { Name = "Maybank", AccountType = "Bank", OpeningBalance = 1000m, IsActive = true };
            var cash = new Account { Name = "Cash", AccountType = "Cash", OpeningBalance = 200m, IsActive = true };
            var closed = new Account { Name = "Closed", AccountType = "Bank", OpeningBalance = 500m, IsActive = false };
            context.Accounts.AddRange(maybank, cash, closed);
            await context.SaveChangesAsync();

            context.Transactions.AddRange(
                NewTransaction(maybank.Id, 1, "Income", 3000m, new DateTime(2026, 9, 5), "September salary"),
                NewTransaction(maybank.Id, 5, "Expense", 250m, new DateTime(2026, 9, 10), "Groceries"),
                NewTransaction(maybank.Id, null, "Transfer", 500m, new DateTime(2026, 9, 12), "Move to cash", cash.Id),
                NewTransaction(cash.Id, 2, "Income", 100m, new DateTime(2026, 9, 20), "Interest"),
                NewTransaction(maybank.Id, 6, "Expense", 100m, new DateTime(2026, 8, 31), "August petrol"),
                NewTransaction(cash.Id, 5, "Expense", 40m, new DateTime(2026, 9, 21), "Lunch"));
            await context.SaveChangesAsync();

            var snapshot = await harness.DashboardService.GetDashboardSnapshotAsync(asOf);

            // Maybank: 1000 + 3000 - 250 - 500 - 100 (August) = 3150, Cash: 200 + 500 + 100 - 40 = 760
            // Inactive accounts are ignored (the 500 opening balance of the closed account is excluded).
            Assert.Equal(3910m, snapshot.TotalAccountBalance);
            Assert.Equal(3100m, snapshot.CurrentMonthIncome);
            Assert.Equal(290m, snapshot.CurrentMonthExpenses);
            Assert.Equal(2810m, snapshot.NetCashFlow);

            // Last six calendar months: Apr - Sep 2026
            Assert.Equal(6, snapshot.MonthlyExpenses.Count);
            Assert.Equal(new DateTime(2026, 4, 1), snapshot.MonthlyExpenses[0].Month);
            Assert.Equal("Apr 2026", snapshot.MonthlyExpenses[0].MonthLabel);
            Assert.Equal(0m, snapshot.MonthlyExpenses[0].TotalExpense);
            Assert.Equal(100m, snapshot.MonthlyExpenses[4].TotalExpense); // August
            Assert.Equal(290m, snapshot.MonthlyExpenses[5].TotalExpense); // September
        }

        [Fact]
        public async Task RecentTransactions_ReturnOnlyTheFiveNewestFirst()
        {
            using var harness = new DashboardTestHarness();
            var context = harness.Context;

            var account = new Account { Name = "Wallet", AccountType = "Cash", OpeningBalance = 0m };
            context.Accounts.Add(account);
            await context.SaveChangesAsync();

            for (var day = 1; day <= 7; day++)
            {
                context.Transactions.Add(NewTransaction(
                    account.Id, 5, "Expense", day * 10m, new DateTime(2026, 9, day), $"Expense {day}"));
            }

            await context.SaveChangesAsync();

            var snapshot = await harness.DashboardService.GetDashboardSnapshotAsync(new DateTime(2026, 9, 22));

            Assert.Equal(5, snapshot.RecentTransactions.Count);
            Assert.Equal("Expense 7", snapshot.RecentTransactions[0].Description);
            Assert.Equal("Expense 3", snapshot.RecentTransactions[4].Description);
        }

        [Fact]
        public async Task RemindersAndVehicle_AreSummarisedAndEmptyStatesReported()
        {
            using var harness = new DashboardTestHarness();
            var context = harness.Context;
            var asOf = new DateTime(2026, 9, 22);

            // Empty database -> the dashboard must report the empty states
            var empty = await harness.DashboardService.GetDashboardSnapshotAsync(asOf);
            Assert.Empty(empty.UpcomingReminders);
            Assert.Empty(empty.OverdueReminders);
            Assert.Null(empty.Vehicle);

            for (var i = 1; i <= 6; i++)
            {
                context.Reminders.Add(new Reminder
                {
                    Title = $"Reminder {i}",
                    ReminderType = "General",
                    ReminderDate = asOf.AddDays(i),
                    IsCompleted = false
                });
            }

            context.Reminders.Add(new Reminder
            {
                Title = "Completed reminder",
                ReminderType = "General",
                ReminderDate = asOf.AddDays(1),
                IsCompleted = true
            });
            context.Reminders.Add(new Reminder
            {
                Title = "Overdue reminder",
                ReminderType = "General",
                ReminderDate = asOf.AddDays(-3),
                IsCompleted = false
            });

            var vehicle = new Vehicle
            {
                Make = "Perodua",
                Model = "Myvi",
                RegistrationNo = "WXY 1234",
                CurrentMileage = 45000,
                IsActive = true
            };
            context.Vehicles.Add(vehicle);
            await context.SaveChangesAsync();

            context.MaintenanceRecords.AddRange(
                new MaintenanceRecord
                {
                    VehicleId = vehicle.Id,
                    ServiceDate = new DateTime(2025, 3, 10),
                    Mileage = 30000,
                    MaintenanceType = "Oil Change",
                    Amount = 150m
                },
                new MaintenanceRecord
                {
                    VehicleId = vehicle.Id,
                    ServiceDate = new DateTime(2026, 6, 15),
                    Mileage = 42000,
                    MaintenanceType = "Major Service",
                    Amount = 800m
                });
            await context.SaveChangesAsync();

            var snapshot = await harness.DashboardService.GetDashboardSnapshotAsync(asOf);

            // Only the five earliest outstanding future reminders are shown
            Assert.Equal(5, snapshot.UpcomingReminders.Count);
            Assert.Equal("Reminder 1", snapshot.UpcomingReminders[0].Title);
            Assert.Equal("Reminder 5", snapshot.UpcomingReminders[4].Title);

            // The overdue reminder is reported separately and never mixed into the upcoming list
            Assert.Single(snapshot.OverdueReminders);
            Assert.Equal("Overdue reminder", snapshot.OverdueReminders[0].Title);
            Assert.DoesNotContain(snapshot.UpcomingReminders, r => r.Title == "Overdue reminder");
            Assert.DoesNotContain(snapshot.UpcomingReminders, r => r.IsCompleted);

            Assert.NotNull(snapshot.Vehicle);
            Assert.Equal("Perodua Myvi", snapshot.Vehicle!.VehicleName);
            Assert.Equal("WXY 1234", snapshot.Vehicle.RegistrationNo);
            Assert.Equal(45000, snapshot.Vehicle.CurrentMileage);
            Assert.Equal(new DateTime(2026, 6, 15), snapshot.Vehicle.LastMaintenanceDate);
        }

        [Theory]
        [InlineData(1250.0, "RM 1,250.00")]
        [InlineData(0.0, "RM 0.00")]
        [InlineData(-12.5, "-RM 12.50")]
        public void CurrencyFormatter_FormatsMalaysianRinggit(double amount, string expected)
        {
            Assert.Equal(expected, CurrencyFormatter.Format((decimal)amount));
        }

        private static Transaction NewTransaction(
            int accountId,
            int? categoryId,
            string transactionType,
            decimal amount,
            DateTime date,
            string description,
            int? transferToAccountId = null)
        {
            return new Transaction
            {
                AccountId = accountId,
                CategoryId = categoryId,
                TransactionType = transactionType,
                Amount = amount,
                TransactionDate = date,
                Description = description,
                TransferToAccountId = transferToAccountId
            };
        }

        /// <summary>Keeps an in-memory SQLite database and its services alive for a single test.</summary>
        private sealed class DashboardTestHarness : IDisposable
        {
            private readonly SqliteConnection _connection;

            public DashboardTestHarness()
            {
                _connection = new SqliteConnection("Data Source=:memory:");
                _connection.Open();

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(_connection)
                    .Options;

                Context = new AppDbContext(options);
                Context.Database.EnsureCreated();

                var accountService = new AccountService(Context);
                var transactionService = new TransactionService(Context);

                DashboardService = new DashboardService(Context, accountService, transactionService);
            }

            public AppDbContext Context { get; }

            public DashboardService DashboardService { get; }

            public void Dispose()
            {
                Context.Dispose();
                _connection.Dispose();
            }
        }
    }
}
