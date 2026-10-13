using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;

namespace MyApp.App.Services
{
    public class SettingsService
    {
        private readonly AppDbContext _context;

        public SettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);
            return setting?.SettingValue;
        }

        public async Task SetSettingAsync(string key, string value)
        {
            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting != null)
            {
                setting.SettingValue = value;
                _context.AppSettings.Update(setting);
            }
            else
            {
                setting = new AppSetting
                {
                    SettingKey = key,
                    SettingValue = value
                };
                _context.AppSettings.Add(setting);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
        {
            var value = await GetSettingAsync(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task InitializeDefaultSettingsAsync()
        {
            var currency = await GetSettingAsync("Currency");
            if (string.IsNullOrEmpty(currency))
            {
                await SetSettingAsync("Currency", "MYR");
            }

            var currencySymbol = await GetSettingAsync("CurrencySymbol");
            if (string.IsNullOrEmpty(currencySymbol))
            {
                await SetSettingAsync("CurrencySymbol", "RM");
            }

            var dateFormat = await GetSettingAsync("DateFormat");
            if (string.IsNullOrEmpty(dateFormat))
            {
                await SetSettingAsync("DateFormat", "dd/MM/yyyy");
            }

            var startPage = await GetSettingAsync("StartPage");
            if (string.IsNullOrEmpty(startPage))
            {
                await SetSettingAsync("StartPage", "Dashboard");
            }
        }

        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync()
        {
            var stats = new DatabaseStatistics
            {
                DatabaseLocation = await GetDatabaseLocationAsync(),
                DatabaseSize = await GetDatabaseSizeAsync(),
                AccountCount = await _context.Accounts.CountAsync(),
                TransactionCount = await _context.Transactions.CountAsync(),
                VehicleCount = await _context.Vehicles.CountAsync(),
                MaintenanceRecordCount = await _context.MaintenanceRecords.CountAsync(),
                ReminderCount = await _context.Reminders.CountAsync()
            };

            return stats;
        }

        private async Task<string> GetDatabaseLocationAsync()
        {
            var dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyApp",
                "myapp.db");
            return dbPath;
        }

        private async Task<long> GetDatabaseSizeAsync()
        {
            var dbPath = await GetDatabaseLocationAsync();
            if (System.IO.File.Exists(dbPath))
            {
                var fileInfo = new System.IO.FileInfo(dbPath);
                return fileInfo.Length;
            }
            return 0;
        }
    }

    public class DatabaseStatistics
    {
        public string DatabaseLocation { get; set; } = string.Empty;
        public long DatabaseSize { get; set; }
        public int AccountCount { get; set; }
        public int TransactionCount { get; set; }
        public int VehicleCount { get; set; }
        public int MaintenanceRecordCount { get; set; }
        public int ReminderCount { get; set; }
    }
}