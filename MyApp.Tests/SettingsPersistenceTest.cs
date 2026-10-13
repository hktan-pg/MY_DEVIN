using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Services;
using Xunit;
using System.IO;

namespace MyApp.Tests
{
    public class SettingsPersistenceTest
    {
        [Fact]
        public async Task SettingsPersistAcrossContexts()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyApp",
                "myapp.db");

            // First context - set a setting
            var options1 = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using (var context1 = new AppDbContext(options1))
            {
                var settingsService1 = new SettingsService(context1);
                await settingsService1.SetSettingAsync("TestPersistence", "PersistedValue");
            }

            // Second context - read the setting
            var options2 = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using (var context2 = new AppDbContext(options2))
            {
                var settingsService2 = new SettingsService(context2);
                var value = await settingsService2.GetSettingAsync("TestPersistence");
                Assert.Equal("PersistedValue", value);
            }
        }
    }
}