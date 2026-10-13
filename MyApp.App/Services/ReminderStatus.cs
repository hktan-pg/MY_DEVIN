using MyApp.App.Models;

namespace MyApp.App.Services
{
    /// <summary>Status of a reminder, derived from its date and its completion flag.</summary>
    public enum ReminderStatus
    {
        Overdue,
        Upcoming,
        Completed
    }

    /// <summary>
    /// The reminder status rules:
    /// overdue when the date is before today and the reminder is not completed,
    /// upcoming when the date is today or later and the reminder is not completed,
    /// completed as soon as IsCompleted is set.
    /// </summary>
    public static class ReminderStatusRules
    {
        /// <summary>Calculates the status of a reminder for the given date.</summary>
        public static ReminderStatus GetStatus(Reminder reminder, DateTime asOfDate)
        {
            if (reminder.IsCompleted)
                return ReminderStatus.Completed;

            return reminder.ReminderDate.Date < asOfDate.Date
                ? ReminderStatus.Overdue
                : ReminderStatus.Upcoming;
        }

        /// <summary>Display text of a status, e.g. "Overdue" or "Completed".</summary>
        public static string GetStatusText(ReminderStatus status)
        {
            return status switch
            {
                ReminderStatus.Overdue => "Overdue",
                ReminderStatus.Completed => "Completed",
                _ => "Upcoming"
            };
        }

        /// <summary>Short timing text, e.g. "Today", "In 3 days" or "5 days overdue".</summary>
        public static string GetScheduleText(Reminder reminder, DateTime asOfDate)
        {
            if (reminder.IsCompleted)
                return "Completed";

            var days = (reminder.ReminderDate.Date - asOfDate.Date).Days;

            if (days == 0)
                return "Today";

            if (days == 1)
                return "Tomorrow";

            if (days > 1)
                return $"In {days} days";

            return days == -1 ? "1 day overdue" : $"{Math.Abs(days)} days overdue";
        }
    }
}
