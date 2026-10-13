using System.Linq;
using System.Windows;
using System.Windows.Input;
using MyApp.App.Models;

namespace MyApp.App
{
    public partial class ReminderDialog : Window
    {
        public Reminder Reminder { get; private set; }
        public bool IsSaved { get; private set; } = false;

        public ReminderDialog(Reminder? reminder = null)
        {
            InitializeComponent();

            // Unlock dark title bar to match the black theme
            WindowThemeHelper.EnableDarkTitleBar(this);

            if (reminder != null)
            {
                // Work with a copy so cancel doesn't mutate the original
                Reminder = new Reminder
                {
                    Id = reminder.Id,
                    Title = reminder.Title,
                    ReminderDate = reminder.ReminderDate,
                    ReminderType = reminder.ReminderType,
                    RelatedEntity = reminder.RelatedEntity,
                    RelatedId = reminder.RelatedId,
                    Notes = reminder.Notes,
                    IsCompleted = reminder.IsCompleted,
                    CreatedAt = reminder.CreatedAt
                };
                Title = "Edit Reminder";
            }
            else
            {
                Reminder = new Reminder
                {
                    IsCompleted = false,
                    ReminderDate = DateTime.Today,
                    ReminderType = ReminderTypes.Other
                };
                Title = "Add Reminder";
            }

            DataContext = Reminder;

            LoadReminderTypes();

            ReminderDatePicker.SelectedDate = Reminder.ReminderDate;
            RelatedIdTextBox.Text = Reminder.RelatedId.HasValue ? Reminder.RelatedId.Value.ToString() : string.Empty;
        }

        /// <summary>
        /// Fills the type drop down with the standard types. A type that is already stored
        /// but is not part of the list is kept so editing never loses it.
        /// </summary>
        private void LoadReminderTypes()
        {
            var types = ReminderTypes.All.ToList();

            if (!string.IsNullOrWhiteSpace(Reminder.ReminderType) && !types.Contains(Reminder.ReminderType))
            {
                types.Insert(0, Reminder.ReminderType);
            }

            ReminderTypeComboBox.ItemsSource = types;
            ReminderTypeComboBox.SelectedItem = string.IsNullOrWhiteSpace(Reminder.ReminderType)
                ? ReminderTypes.Other
                : Reminder.ReminderType;
        }

        private void DigitsOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // The related id only accepts digits, so an invalid number can never be typed
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Reminder.Title))
            {
                System.Windows.MessageBox.Show("Title is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!ReminderDatePicker.SelectedDate.HasValue)
            {
                System.Windows.MessageBox.Show("Reminder Date is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            int? relatedId = null;
            var relatedIdText = RelatedIdTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(relatedIdText))
            {
                if (!int.TryParse(relatedIdText, out var parsedId) || parsedId <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "Related ID must be a whole number greater than zero.",
                        "Validation Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                relatedId = parsedId;
            }

            Reminder.Title = Reminder.Title.Trim();
            Reminder.ReminderDate = ReminderDatePicker.SelectedDate.Value.Date;
            Reminder.ReminderType = ReminderTypeComboBox.SelectedItem as string ?? ReminderTypes.Other;
            Reminder.RelatedEntity = string.IsNullOrWhiteSpace(Reminder.RelatedEntity)
                ? null
                : Reminder.RelatedEntity.Trim();
            Reminder.RelatedId = relatedId;
            Reminder.Notes = string.IsNullOrWhiteSpace(Reminder.Notes) ? null : Reminder.Notes;

            IsSaved = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
