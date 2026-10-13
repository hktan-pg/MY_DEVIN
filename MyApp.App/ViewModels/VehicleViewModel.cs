using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MyApp.App.Services;

namespace MyApp.App.ViewModels
{
    /// <summary>
    /// View model of the Vehicle screen. All data comes from <see cref="VehicleService"/>;
    /// the view only displays it and the dialog collects user input.
    /// </summary>
    public class VehicleViewModel : ViewModelBase
    {
        private readonly VehicleService _vehicleService;
        private ObservableCollection<VehicleOverview> _vehicles;
        private VehicleOverview? _selectedVehicle;

        public VehicleViewModel(VehicleService vehicleService)
        {
            _vehicleService = vehicleService;
            _vehicles = new ObservableCollection<VehicleOverview>();

            LoadVehiclesCommand = new RelayCommand(async _ => await LoadVehiclesAsync());
            AddVehicleCommand = new RelayCommand(_ => AddVehicle());
            EditVehicleCommand = new RelayCommand(_ => EditVehicle(), _ => CanEditVehicle());
            DeactivateVehicleCommand = new RelayCommand(_ => DeactivateVehicle(), _ => CanDeactivateVehicle());
        }

        /// <summary>All vehicles (active and inactive) with their maintenance totals.</summary>
        public ObservableCollection<VehicleOverview> Vehicles
        {
            get => _vehicles;
            set => SetProperty(ref _vehicles, value);
        }

        /// <summary>The vehicle currently selected in the list; also drives the details panel.</summary>
        public VehicleOverview? SelectedVehicle
        {
            get => _selectedVehicle;
            set
            {
                if (SetProperty(ref _selectedVehicle, value))
                {
                    OnPropertyChanged(nameof(HasSelectedVehicle));
                    ((RelayCommand)EditVehicleCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeactivateVehicleCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasSelectedVehicle => SelectedVehicle != null;

        public ICommand LoadVehiclesCommand { get; }
        public ICommand AddVehicleCommand { get; }
        public ICommand EditVehicleCommand { get; }
        public ICommand DeactivateVehicleCommand { get; }

        /// <summary>Loads the vehicle list and keeps the current selection when possible.</summary>
        public async Task LoadVehiclesAsync()
        {
            var previouslySelectedId = SelectedVehicle?.Vehicle.Id;

            var overviews = await _vehicleService.GetVehicleOverviewListAsync();
            Vehicles = new ObservableCollection<VehicleOverview>(overviews);

            SelectedVehicle = Vehicles.FirstOrDefault(v => v.Vehicle.Id == previouslySelectedId)
                              ?? Vehicles.FirstOrDefault();
        }

        private bool CanEditVehicle()
        {
            return SelectedVehicle != null;
        }

        /// <summary>Deactivation is only offered for a vehicle that is currently active.</summary>
        private bool CanDeactivateVehicle()
        {
            return SelectedVehicle?.Vehicle.IsActive == true;
        }

        private async void AddVehicle()
        {
            var dialog = new VehicleDialog();
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var created = await _vehicleService.CreateVehicleAsync(dialog.Vehicle);
                await LoadVehiclesAsync();

                // Show the newly added vehicle in the details panel
                SelectedVehicle = Vehicles.FirstOrDefault(v => v.Vehicle.Id == created.Id) ?? SelectedVehicle;
            }
        }

        private async void EditVehicle()
        {
            var vehicle = SelectedVehicle?.Vehicle;
            if (vehicle == null) return;

            var dialog = new VehicleDialog(vehicle);
            if (dialog.ShowDialog() == true && dialog.IsSaved)
            {
                var edited = dialog.Vehicle;

                // Copy the edited values back to the tracked entity
                vehicle.Make = edited.Make;
                vehicle.Model = edited.Model;
                vehicle.Year = edited.Year;
                vehicle.RegistrationNo = edited.RegistrationNo;
                vehicle.CurrentMileage = edited.CurrentMileage;
                vehicle.PurchaseDate = edited.PurchaseDate;
                vehicle.Notes = edited.Notes;
                vehicle.IsActive = edited.IsActive;

                await _vehicleService.UpdateVehicleAsync(vehicle);
                await LoadVehiclesAsync();
            }
        }

        private async void DeactivateVehicle()
        {
            var vehicle = SelectedVehicle?.Vehicle;
            if (vehicle == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to deactivate '{vehicle.Make} {vehicle.Model}'?",
                "Confirm Deactivation",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _vehicleService.DeactivateVehicleAsync(vehicle.Id);
                await LoadVehiclesAsync();
            }
        }
    }
}
