using System.Collections.ObjectModel;
using System.Windows.Input;
using MyApp.App.Services;

namespace MyApp.App.ViewModels
{
    /// <summary>
    /// View model of the Reminders screen. Reminders are grouped into overdue, upcoming and
    /// completed using <see cref="ReminderStatusRules"/>, and every value comes from
    /// <see cref="ReminderService"/>; the view only displays it.
    /// </summary>
    public class RemindersViewModel : ViewModelBase
    {
        private readonly ReminderService _reminderService;

        private ObservableCollection<ReminderOverview> _overdueReminders;
        private ObservableCollection<ReminderOverview> _upcomingReminders;
        private ObservableCollection<ReminderOverview> _completedReminders;

        private ReminderOverview? _selectedOverdue;
        private ReminderOverview? _selectedUpcoming;
        private ReminderOverview? _selectedCompleted;

        public RemindersViewModel(ReminderService reminderService)
        {
            _reminderService = reminderService;

            _overdueReminders = new ObservableCollection<ReminderOverview>();
            _upcomingReminders = new ObservableCollection<ReminderOverview>();
            _completedReminders = new ObservableCollection<ReminderOverview>();

            LoadRemindersCommand = new RelayCommand(async _ => await LoadRemindersAsync());
            AddReminderCommand = new RelayCommand(_ => AddReminder());
            EditReminderCommand = new RelayCommand(_ => EditReminder(), _ => CanEditReminder());
            MarkCompletedCommand = new RelayCommand(_ => MarkCompleted(), _ => CanMarkCompleted());
            ReopenCommand = new RelayCommand(_ => Reopen(), _ => CanReopen());
        }

        /// <summary>Reminders whose date has passed and that are not completed, most overdue first.</summary>
        public ObservableCollection<ReminderOverview> OverdueReminders
        {
            get => _overdueReminders;
            set
            {
                if (SetProperty(ref _overdueReminders, value))
                {
                    OnPropertyChanged(nameof(OverdueCount));
                    OnPropertyChanged(nameof(HasOverdueReminders));
                    OnPropertyChanged(nameof(OverdueHeaderText));
                }
            }
        }

        /// <summary>Reminders due today or later, soonest first.</summary>
        public ObservableCollection<ReminderOverview> UpcomingReminders
        {
            get => _upcomingReminders;
            set
            {
                if (SetProperty(ref _upcomingReminders, value))
                {
                    OnPropertyChanged(nameof(UpcomingCount));
                    OnPropertyChanged(nameof(HasUpcomingReminders));
                    OnPropertyChanged(nameof(UpcomingHeaderText));
                }
            }
        }

        /// <summary>Reminders that have been completed, most recent first.</summary>
        public ObservableCollection<ReminderOverview> CompletedReminders
        {
            get => _completedReminders;
            set
            {
                if (SetProperty(ref _completedReminders, value))
                {
                    OnPropertyChanged(nameof(CompletedCount));
                    OnPropertyChanged(nameof(HasCompletedReminders));
                    OnPropertyChanged(nameof(CompletedHeaderText));
                }
            }
        }

        public int OverdueCount => OverdueReminders.Count;

        public int UpcomingCount => UpcomingReminders.Count;

        public int CompletedCount => CompletedReminders.Count;

        public bool HasOverdueReminders => OverdueReminders.Count > 0;

        public bool HasUpcomingReminders => UpcomingReminders.Count > 0;

        public bool HasCompletedReminders => CompletedReminders.Count > 0;

        public string OverdueHeaderText => $"OVERDUE ({OverdueCount})";

        public string UpcomingHeaderText => $"UPCOMING ({UpcomingCount})";

        public string CompletedHeaderText => $"COMPLETED ({CompletedCount})";

        /// <summary>Selection of the Overdue list.</summary>
        public ReminderOverview? SelectedOverdue
        {
            get => _selectedOverdue;
            set
            {
                if (ReferenceEquals(_selectedOverdue, value))
                    return;

                // A reminder can only be selected in one of the three lists
                SetSelection(value, null, null);
            }
        }

        /// <summary>Selection of the Upcoming list.</summary>
        public ReminderOverview? SelectedUpcoming
        {
            get => _selectedUpcoming;
            set
            {
                if (ReferenceEquals(_selectedUpcoming, value))
                    return;

                SetSelection(null, value, null);
            }
        }

        /// <summary>Selection of the Completed list.</summary>
        public ReminderOverview? SelectedCompleted
        {
            get => _selectedCompleted;
            set
            {
                if (ReferenceEquals(_selectedCompleted, value))
                    return;

                SetSelection(null, null, value);
            }
        }

        /// <summary>The reminder selected in any of the three lists; drives Edit / Mark Completed / Reopen.</summary>
        public ReminderOverview? SelectedReminder => _selectedOverdue ?? _selectedUpcoming ?? _selectedCompleted;

        public ICommand LoadRemindersCommand { get; }
        public ICommand AddReminderCommand { get; }
        public ICommand EditReminderCommand { get; }
        public ICommand MarkCompletedCommand { get; }
        public ICommand ReopenCommand { get; }

        /// <summary>Reloads the three lists from the database for today's date.</summary>
        public async Task LoadRemindersAsync()
        {
            var asOf = DateTime.Today;

            var reminders = await _reminderService.GetAllRemindersAsync();
            var overviews = reminders.Select(r => new ReminderOverview(r, asOf)).ToList();

            // GetAllRemindersAsync is ordered by date, so overdue items are listed most
            // overdue first and upcoming items soonest first.
            OverdueReminders = new ObservableCollection<ReminderOverview>(overviews.Where(o => o.IsOverdue));
            UpcomingReminders = new ObservableCollection<ReminderOverview>(overviews.Where(o => o.IsUpcoming));
            CompletedReminders = new ObservableCollection<ReminderOverview>(
                overviews.Where(o => o.IsCompleted)
                    .OrderByDescending(o => o.Reminder.ReminderDate)
                    .ThenBy(o => o.Reminder.Id));

            SetSelection(null, null, null);
        }

        private bool CanEditReminder()
        {
            return SelectedReminder != null;
        }

        private bool CanMarkCompleted()
        {
            return SelectedReminder?.IsCompleted == false;
        }

        private bool CanReopen()
        {
            return SelectedReminder?.IsCompleted == true;
        }

        private async void AddReminder()
        {
            var dialog = new ReminderDialog();
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var created = await _reminderService.CreateReminderAsync(dialog.Reminder);
                await LoadRemindersAsync();
                SelectReminderById(created.Id);
            }
        }

        private async void EditReminder()
        {
            var reminder = SelectedReminder?.Reminder;
            if (reminder == null) return;

            var dialog = new ReminderDialog(reminder);
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var edited = dialog.Reminder;

                // Copy the edited values back to the tracked entity
                reminder.Title = edited.Title;
                reminder.ReminderDate = edited.ReminderDate;
                reminder.ReminderType = edited.ReminderType;
                reminder.RelatedEntity = edited.RelatedEntity;
                reminder.RelatedId = edited.RelatedId;
                reminder.Notes = edited.Notes;
                reminder.IsCompleted = edited.IsCompleted;

                await _reminderService.UpdateReminderAsync(reminder);
                await LoadRemindersAsync();
                SelectReminderById(reminder.Id);
            }
        }

        private async void MarkCompleted()
        {
            var reminder = SelectedReminder?.Reminder;
            if (reminder == null) return;

            await _reminderService.SetCompletedAsync(reminder.Id, true);
            await LoadRemindersAsync();
            SelectReminderById(reminder.Id);
        }

        private async void Reopen()
        {
            var reminder = SelectedReminder?.Reminder;
            if (reminder == null) return;

            await _reminderService.SetCompletedAsync(reminder.Id, false);
            await LoadRemindersAsync();
            SelectReminderById(reminder.Id);
        }

        /// <summary>Selects the reminder with the given id in whichever list now holds it.</summary>
        private void SelectReminderById(int id)
        {
            var target = OverdueReminders.FirstOrDefault(o => o.Reminder.Id == id)
                         ?? UpcomingReminders.FirstOrDefault(o => o.Reminder.Id == id)
                         ?? CompletedReminders.FirstOrDefault(o => o.Reminder.Id == id);

            if (target == null)
                return;

            if (OverdueReminders.Contains(target))
                SetSelection(target, null, null);
            else if (UpcomingReminders.Contains(target))
                SetSelection(null, target, null);
            else
                SetSelection(null, null, target);
        }

        /// <summary>Applies a single selection across the three lists and refreshes the commands.</summary>
        private void SetSelection(ReminderOverview? overdue, ReminderOverview? upcoming, ReminderOverview? completed)
        {
            _selectedOverdue = overdue;
            _selectedUpcoming = upcoming;
            _selectedCompleted = completed;

            OnPropertyChanged(nameof(SelectedOverdue));
            OnPropertyChanged(nameof(SelectedUpcoming));
            OnPropertyChanged(nameof(SelectedCompleted));
            OnPropertyChanged(nameof(SelectedReminder));

            ((RelayCommand)EditReminderCommand).RaiseCanExecuteChanged();
            ((RelayCommand)MarkCompletedCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ReopenCommand).RaiseCanExecuteChanged();
        }
    }
}
