using MyApp.App.Models;

namespace MyApp.App.Services
{
    /// <summary>
    /// A reminder together with the status calculated for the day the list was loaded.
    /// Produced by the Reminders view model from <see cref="ReminderService"/> data.
    /// </summary>
    public class ReminderOverview
    {
        public ReminderOverview(Reminder reminder, DateTime asOfDate)
        {
            Reminder = reminder;
            AsOfDate = asOfDate.Date;
            Status = ReminderStatusRules.GetStatus(reminder, AsOfDate);
        }

        /// <summary>The reminder entity itself.</summary>
        public Reminder Reminder { get; }

        /// <summary>The date the status was calculated for (start of day).</summary>
        public DateTime AsOfDate { get; }

        /// <summary>Overdue, upcoming or completed.</summary>
        public ReminderStatus Status { get; }

        /// <summary>Display text of the status, e.g. "Overdue".</summary>
        public string StatusText => ReminderStatusRules.GetStatusText(Status);

        /// <summary>Short timing text, e.g. "In 3 days" or "5 days overdue".</summary>
        public string ScheduleText => ReminderStatusRules.GetScheduleText(Reminder, AsOfDate);

        /// <summary>Related record as text, e.g. "Vehicle #3", or "-" when nothing is linked.</summary>
        public string RelatedDisplay
        {
            get
            {
                var entity = Reminder.RelatedEntity?.Trim();

                if (string.IsNullOrEmpty(entity))
                    return Reminder.RelatedId.HasValue ? $"#{Reminder.RelatedId.Value}" : "-";

                return Reminder.RelatedId.HasValue ? $"{entity} #{Reminder.RelatedId.Value}" : entity;
            }
        }

        public bool IsOverdue => Status == ReminderStatus.Overdue;

        public bool IsUpcoming => Status == ReminderStatus.Upcoming;

        public bool IsCompleted => Status == ReminderStatus.Completed;
    }
}
