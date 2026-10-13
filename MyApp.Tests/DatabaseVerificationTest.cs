using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;
using System.IO;
using Xunit;

namespace MyApp.Tests
{
    public class DatabaseVerificationTest
    {
        [Fact]
        public void VerifyRealDatabaseCreation()
        {
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyApp",
                "myapp.db");

            Assert.True(File.Exists(dbPath), $"Database file should exist at {dbPath}");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using var context = new AppDbContext(options);
            
            // Verify all tables exist and can be queried
            var accountCount = context.Accounts.Count();
            var categoryCount = context.Categories.Count();
            var transactionCount = context.Transactions.Count();
            var vehicleCount = context.Vehicles.Count();
            var maintenanceCount = context.MaintenanceRecords.Count();
            var reminderCount = context.Reminders.Count();
            var settingCount = context.AppSettings.Count();

            // Verify seeded categories
            Assert.Equal(12, categoryCount);
            
            // Verify specific seeded categories
            var salaryCategory = context.Categories.FirstOrDefault(c => c.Name == "Salary");
            Assert.NotNull(salaryCategory);
            Assert.Equal("Income", salaryCategory.CategoryType);

            var foodCategory = context.Categories.FirstOrDefault(c => c.Name == "Food");
            Assert.NotNull(foodCategory);
            Assert.Equal("Expense", foodCategory.CategoryType);
        }
    }
}