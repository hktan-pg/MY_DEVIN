using MyApp.App.Models;

namespace MyApp.App.Services
{
    /// <summary>
    /// A vehicle together with the maintenance figures shown on the Vehicle screen.
    /// Produced by <see cref="VehicleService"/> from MaintenanceRecord data.
    /// </summary>
    public class VehicleOverview
    {
        /// <summary>The vehicle entity itself.</summary>
        public Vehicle Vehicle { get; set; } = new Vehicle();

        /// <summary>Number of maintenance records for this vehicle.</summary>
        public int MaintenanceRecordCount { get; set; }

        /// <summary>Sum of all maintenance amounts for this vehicle.</summary>
        public decimal TotalMaintenanceCost { get; set; }

        /// <summary>Service date of the most recent maintenance record, if any.</summary>
        public DateTime? LastMaintenanceDate { get; set; }

        /// <summary>Mileage recorded on the most recent maintenance record, if any.</summary>
        public int? LastMaintenanceMileage { get; set; }

        /// <summary>Display name, e.g. "Perodua Myvi".</summary>
        public string VehicleName => $"{Vehicle.Make} {Vehicle.Model}".Trim();

        /// <summary>True when at least one maintenance record exists.</summary>
        public bool HasMaintenanceRecords => MaintenanceRecordCount > 0;
    }
}
