using System.Linq;
using System.Windows;
using System.Windows.Input;
using MyApp.App.Models;

namespace MyApp.App
{
    public partial class VehicleDialog : Window
    {
        private const int MinimumVehicleYear = 1900;

        private static readonly int MaximumVehicleYear = DateTime.Today.Year + 1;

        public Vehicle Vehicle { get; private set; }
        public bool IsSaved { get; private set; } = false;

        public VehicleDialog(Vehicle? vehicle = null)
        {
            InitializeComponent();

            // Unlock dark title bar to match the black theme
            WindowThemeHelper.EnableDarkTitleBar(this);

            if (vehicle != null)
            {
                // Work with a copy so cancel doesn't mutate the original
                Vehicle = new Vehicle
                {
                    Id = vehicle.Id,
                    Make = vehicle.Make,
                    Model = vehicle.Model,
                    Year = vehicle.Year,
                    RegistrationNo = vehicle.RegistrationNo,
                    CurrentMileage = vehicle.CurrentMileage,
                    PurchaseDate = vehicle.PurchaseDate,
                    Notes = vehicle.Notes,
                    IsActive = vehicle.IsActive
                };
                Title = "Edit Vehicle";
            }
            else
            {
                Vehicle = new Vehicle
                {
                    IsActive = true,
                    CurrentMileage = 0
                };
                Title = "Add Vehicle";
            }

            DataContext = Vehicle;

            YearTextBox.Text = Vehicle.Year.HasValue ? Vehicle.Year.Value.ToString() : string.Empty;
            CurrentMileageTextBox.Text = Vehicle.CurrentMileage.ToString();
            PurchaseDatePicker.SelectedDate = Vehicle.PurchaseDate;
        }

        private void DigitsOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Numeric fields only accept digits, so invalid numbers can never be typed
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Vehicle.Make))
            {
                System.Windows.MessageBox.Show("Make is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Vehicle.Model))
            {
                System.Windows.MessageBox.Show("Model is required.", "Validation Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            int? year = null;
            var yearText = YearTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(yearText))
            {
                if (!int.TryParse(yearText, out var parsedYear) ||
                    parsedYear < MinimumVehicleYear ||
                    parsedYear > MaximumVehicleYear)
                {
                    System.Windows.MessageBox.Show(
                        $"Year must be a valid year between {MinimumVehicleYear} and {MaximumVehicleYear}.",
                        "Validation Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                year = parsedYear;
            }

            if (!int.TryParse(CurrentMileageTextBox.Text.Trim(), out var currentMileage) || currentMileage < 0)
            {
                System.Windows.MessageBox.Show(
                    "Current Mileage must be a whole number that is not negative.",
                    "Validation Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            Vehicle.Make = Vehicle.Make.Trim();
            Vehicle.Model = Vehicle.Model.Trim();
            Vehicle.Year = year;
            Vehicle.CurrentMileage = currentMileage;
            Vehicle.RegistrationNo = string.IsNullOrWhiteSpace(Vehicle.RegistrationNo)
                ? null
                : Vehicle.RegistrationNo.Trim();
            Vehicle.PurchaseDate = PurchaseDatePicker.SelectedDate;
            Vehicle.Notes = string.IsNullOrWhiteSpace(Vehicle.Notes) ? null : Vehicle.Notes;

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
