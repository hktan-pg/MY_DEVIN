using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;

namespace MyApp.App.Services
{
    /// <summary>
    /// Data access for the Vehicle module. All vehicle queries live here; the view
    /// and the view model never touch the DbContext directly.
    /// </summary>
    public class VehicleService
    {
        private readonly AppDbContext _context;

        public VehicleService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns every vehicle (active and inactive) together with its maintenance totals,
        /// ordered by make and model.
        /// </summary>
        public async Task<List<VehicleOverview>> GetVehicleOverviewListAsync()
        {
            var vehicles = await _context.Vehicles
                .OrderBy(v => v.Make)
                .ThenBy(v => v.Model)
                .ToListAsync();

            // Maintenance totals are aggregated in memory: SQLite stores decimal
            // columns as text, so SUM(decimal) cannot be translated to SQL.
            var maintenanceRecords = await _context.MaintenanceRecords
                .Select(m => new { m.Id, m.VehicleId, m.Amount, m.ServiceDate, m.Mileage })
                .ToListAsync();

            var overviews = new List<VehicleOverview>(vehicles.Count);
            foreach (var vehicle in vehicles)
            {
                var records = maintenanceRecords
                    .Where(m => m.VehicleId == vehicle.Id)
                    .ToList();

                var lastRecord = records
                    .OrderByDescending(m => m.ServiceDate)
                    .ThenByDescending(m => m.Id)
                    .FirstOrDefault();

                overviews.Add(new VehicleOverview
                {
                    Vehicle = vehicle,
                    MaintenanceRecordCount = records.Count,
                    TotalMaintenanceCost = records.Sum(m => m.Amount),
                    LastMaintenanceDate = lastRecord?.ServiceDate,
                    LastMaintenanceMileage = lastRecord?.Mileage
                });
            }

            return overviews;
        }

        public async Task<Vehicle?> GetVehicleByIdAsync(int id)
        {
            return await _context.Vehicles.FindAsync(id);
        }

        public async Task<Vehicle> CreateVehicleAsync(Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task<Vehicle> UpdateVehicleAsync(Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task<bool> DeactivateVehicleAsync(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
                return false;

            vehicle.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
