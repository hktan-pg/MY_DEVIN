using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.Models;
using Xunit;

namespace MyApp.Tests
{
    public class DatabaseTests
    {
        [Fact]
        public void CanCreateDatabase()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            // Verify tables exist by checking if we can query them
            Assert.NotNull(context.Accounts);
            Assert.NotNull(context.Categories);
            Assert.NotNull(context.Transactions);
            Assert.NotNull(context.Vehicles);
            Assert.NotNull(context.MaintenanceRecords);
            Assert.NotNull(context.Reminders);
            Assert.NotNull(context.AppSettings);
        }

        [Fact]
        public void CanSeedDefaultCategories()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var categories = context.Categories.ToList();
            Assert.Equal(12, categories.Count);
            
            // Verify income categories
            Assert.Contains(categories, c => c.Name == "Salary" && c.CategoryType == "Income");
            Assert.Contains(categories, c => c.Name == "Interest" && c.CategoryType == "Income");
            Assert.Contains(categories, c => c.Name == "Dividend" && c.CategoryType == "Income");
            Assert.Contains(categories, c => c.Name == "Other Income" && c.CategoryType == "Income");
            
            // Verify expense categories
            Assert.Contains(categories, c => c.Name == "Food" && c.CategoryType == "Expense");
            Assert.Contains(categories, c => c.Name == "Petrol" && c.CategoryType == "Expense");
            Assert.Contains(categories, c => c.Name == "Car" && c.CategoryType == "Expense");
            Assert.Contains(categories, c => c.Name == "Utilities" && c.CategoryType == "Expense");
            Assert.Contains(categories, c => c.Name == "Shopping" && c.CategoryType == "Expense");
            Assert.Contains(categories, c => c.Name == "Household" && c.CategoryType == "Expense");
            Assert.Contains(categories, c => c.Name == "Medical" && c.CategoryType == "Expense");
            Assert.Contains(categories, c => c.Name == "Other Expense" && c.CategoryType == "Expense");
        }

        [Fact]
        public void CanCreateAccount()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var account = new Account
            {
                Name = "Test Account",
                AccountType = "Bank",
                AccountNumber = "123456789",
                OpeningBalance = 1000.50m,
                IsActive = true
            };

            context.Accounts.Add(account);
            context.SaveChanges();

            var savedAccount = context.Accounts.FirstOrDefault(a => a.Name == "Test Account");
            Assert.NotNull(savedAccount);
            Assert.Equal("Test Account", savedAccount.Name);
            Assert.Equal("Bank", savedAccount.AccountType);
            Assert.Equal(1000.50m, savedAccount.OpeningBalance);
        }

        [Fact]
        public void CanCreateTransaction()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            using var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            var account = new Account
            {
                Name = "Test Account",
                AccountType = "Bank",
                OpeningBalance = 1000m
            };

            context.Accounts.Add(account);
            context.SaveChanges();

            var transaction = new Transaction
            {
                TransactionDate = DateTime.UtcNow,
                AccountId = account.Id,
                CategoryId = 5, // Food category
                TransactionType = "Expense",
                Amount = 50.25m,
                Description = "Grocery shopping"
            };

            context.Transactions.Add(transaction);
            context.SaveChanges();

            var savedTransaction = context.Transactions.FirstOrDefault(t => t.Description == "Grocery shopping");
            Assert.NotNull(savedTransaction);
            Assert.Equal(50.25m, savedTransaction.Amount);
            Assert.Equal("Expense", savedTransaction.TransactionType);
        }
    }
}