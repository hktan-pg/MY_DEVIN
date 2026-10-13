using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Services;
using Xunit;

namespace MyApp.Tests
{
    public class SettingsTests
    {
        [Fact]
        public async Task CanInitializeDefaultSettings()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var settingsService = new SettingsService(context);
            await settingsService.InitializeDefaultSettingsAsync();

            var currency = await settingsService.GetSettingAsync("Currency");
            var currencySymbol = await settingsService.GetSettingAsync("CurrencySymbol");
            var dateFormat = await settingsService.GetSettingAsync("DateFormat");
            var startPage = await settingsService.GetSettingAsync("StartPage");

            Assert.Equal("MYR", currency);
            Assert.Equal("RM", currencySymbol);
            Assert.Equal("dd/MM/yyyy", dateFormat);
            Assert.Equal("Dashboard", startPage);
        }

        [Fact]
        public async Task CanSetAndGetSettings()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var settingsService = new SettingsService(context);
            
            await settingsService.SetSettingAsync("TestKey", "TestValue");
            var value = await settingsService.GetSettingAsync("TestKey");

            Assert.Equal("TestValue", value);
        }

        [Fact]
        public async Task CanUpdateExistingSetting()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var settingsService = new SettingsService(context);
            
            await settingsService.SetSettingAsync("Currency", "USD");
            var value = await settingsService.GetSettingAsync("Currency");

            Assert.Equal("USD", value);
        }

        [Fact]
        public async Task CanGetDatabaseStatistics()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var settingsService = new SettingsService(context);
            var stats = await settingsService.GetDatabaseStatisticsAsync();

            Assert.NotNull(stats);
            Assert.Equal(0, stats.AccountCount);
            Assert.Equal(0, stats.TransactionCount);
            Assert.Equal(0, stats.VehicleCount);
            Assert.Equal(0, stats.MaintenanceRecordCount);
            Assert.Equal(0, stats.ReminderCount);
        }
    }
}