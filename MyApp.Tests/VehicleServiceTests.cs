using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;
using MyApp.App.Services;
using System.IO;
using Xunit;

namespace MyApp.Tests
{
    /// <summary>
    /// Verifies the Vehicle module data access against real SQLite databases.
    /// </summary>
    public class VehicleServiceTests
    {
        [Fact]
        public async Task Vehicle_CanBeCreatedUpdatedAndDeactivated()
        {
            using var harness = new VehicleTestHarness();
            var service = harness.VehicleService;

            var vehicle = await service.CreateVehicleAsync(new Vehicle
            {
                Make = "Toyota",
                Model = "Vios",
                Year = 2018,
                RegistrationNo = "ABC 1234",
                CurrentMileage = 55000,
                PurchaseDate = new DateTime(2018, 3, 20),
                Notes = "First car",
                IsActive = true
            });

            Assert.True(vehicle.Id > 0);

            var loaded = await service.GetVehicleByIdAsync(vehicle.Id);
            Assert.NotNull(loaded);
            Assert.Equal("Toyota", loaded!.Make);

            // Edit
            loaded.Model = "Vios G";
            loaded.CurrentMileage = 57250;
            loaded.Year = 2019;
            await service.UpdateVehicleAsync(loaded);

            var updated = await service.GetVehicleByIdAsync(vehicle.Id);
            Assert.Equal("Vios G", updated!.Model);
            Assert.Equal(57250, updated.CurrentMileage);
            Assert.Equal(2019, updated.Year);

            // Deactivate
            Assert.True(await service.DeactivateVehicleAsync(vehicle.Id));

            var deactivated = await service.GetVehicleByIdAsync(vehicle.Id);
            Assert.False(deactivated!.IsActive);
        }

        [Fact]
        public async Task VehicleOverview_CalculatesMaintenanceFiguresFromRecords()
        {
            using var harness = new VehicleTestHarness();
            var context = harness.Context;
            var service = harness.VehicleService;

            var withHistory = await service.CreateVehicleAsync(new Vehicle
            {
                Make = "Perodua",
                Model = "Myvi",
                RegistrationNo = "VAB 1000",
                CurrentMileage = 48500,
                IsActive = true
            });

            var withoutHistory = await service.CreateVehicleAsync(new Vehicle
            {
                Make = "Proton",
                Model = "Saga",
                IsActive = false
            });

            context.MaintenanceRecords.AddRange(
                new MaintenanceRecord
                {
                    VehicleId = withHistory.Id,
                    ServiceDate = new DateTime(2024, 5, 10),
                    Mileage = 30000,
                    MaintenanceType = "Oil Change",
                    Amount = 150m
                },
                new MaintenanceRecord
                {
                    VehicleId = withHistory.Id,
                    ServiceDate = new DateTime(2025, 11, 2),
                    Mileage = 40000,
                    MaintenanceType = "Major Service",
                    Amount = 800m
                },
                new MaintenanceRecord
                {
                    VehicleId = withHistory.Id,
                    ServiceDate = new DateTime(2026, 5, 22),
                    Mileage = 45000,
                    MaintenanceType = "Tyres",
                    Amount = 220m
                });
            await context.SaveChangesAsync();

            var overviews = await service.GetVehicleOverviewListAsync();

            var myvi = overviews.Single(o => o.Vehicle.Id == withHistory.Id);
            Assert.Equal("Perodua Myvi", myvi.VehicleName);
            Assert.Equal(3, myvi.MaintenanceRecordCount);
            Assert.True(myvi.HasMaintenanceRecords);
            Assert.Equal(1170m, myvi.TotalMaintenanceCost);
            Assert.Equal(new DateTime(2026, 5, 22), myvi.LastMaintenanceDate);
            Assert.Equal(45000, myvi.LastMaintenanceMileage);

            var saga = overviews.Single(o => o.Vehicle.Id == withoutHistory.Id);
            Assert.Equal(0, saga.MaintenanceRecordCount);
            Assert.False(saga.HasMaintenanceRecords);
            Assert.Equal(0m, saga.TotalMaintenanceCost);
            Assert.Null(saga.LastMaintenanceDate);
            Assert.Null(saga.LastMaintenanceMileage);
        }

        /// <summary>Keeps an in-memory SQLite database and its services alive for a single test.</summary>
        private sealed class VehicleTestHarness : IDisposable
        {
            private readonly SqliteConnection _connection;

            public VehicleTestHarness()
            {
                _connection = new SqliteConnection("Data Source=:memory:");
                _connection.Open();

                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(_connection)
                    .Options;

                Context = new AppDbContext(options);
                Context.Database.EnsureCreated();

                VehicleService = new VehicleService(Context);
            }

            public AppDbContext Context { get; }

            public VehicleService VehicleService { get; }

            public void Dispose()
            {
                Context.Dispose();
                _connection.Dispose();
            }
        }
    }
}
