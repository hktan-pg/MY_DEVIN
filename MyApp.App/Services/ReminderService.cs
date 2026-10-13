using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;

namespace MyApp.App.Services
{
    /// <summary>
    /// Data access for the Reminder module. All reminder queries live here; the view and
    /// the view model never touch the DbContext directly.
    /// </summary>
    public class ReminderService
    {
        private readonly AppDbContext _context;

        public ReminderService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns every reminder (past, future and completed) ordered by date, soonest first.
        /// The Reminders screen sorts the result into upcoming / overdue / completed.
        /// </summary>
        public async Task<List<Reminder>> GetAllRemindersAsync()
        {
            return await _context.Reminders
                .OrderBy(r => r.ReminderDate)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        public async Task<Reminder?> GetReminderByIdAsync(int id)
        {
            return await _context.Reminders.FindAsync(id);
        }

        public async Task<Reminder> CreateReminderAsync(Reminder reminder)
        {
            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();
            return reminder;
        }

        public async Task<Reminder> UpdateReminderAsync(Reminder reminder)
        {
            _context.Reminders.Update(reminder);
            await _context.SaveChangesAsync();
            return reminder;
        }

        /// <summary>Marks a reminder completed (or reopens it); false when the reminder is gone.</summary>
        public async Task<bool> SetCompletedAsync(int id, bool isCompleted)
        {
            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null)
                return false;

            reminder.IsCompleted = isCompleted;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
