using Microsoft.EntityFrameworkCore;
using MyApp.App.Models;
using System.IO;

namespace MyApp.App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyApp", "myapp.db");
                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Account configuration
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.AccountType);
                entity.Property(e => e.AccountType).HasDefaultValue("Bank");
            });

            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.CategoryType);
                entity.HasIndex(e => e.ParentId);
                entity.HasOne(e => e.Parent)
                    .WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.CategoryType).HasDefaultValue("Expense");
            });

            // Transaction configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(e => e.TransactionDate);
                entity.HasIndex(e => e.AccountId);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.TransactionType);
                entity.HasOne(e => e.Account)
                    .WithMany(e => e.Transactions)
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Category)
                    .WithMany(e => e.Transactions)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.TransferToAccount)
                    .WithMany()
                    .HasForeignKey(e => e.TransferToAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.TransactionType).HasDefaultValue("Expense");
            });

            // Vehicle configuration
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasIndex(e => e.Make);
                entity.HasIndex(e => e.Model);
                entity.HasIndex(e => e.RegistrationNo);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // MaintenanceRecord configuration
            modelBuilder.Entity<MaintenanceRecord>(entity =>
            {
                entity.HasIndex(e => e.VehicleId);
                entity.HasIndex(e => e.ServiceDate);
                entity.HasIndex(e => e.Mileage);
                entity.HasOne(e => e.Vehicle)
                    .WithMany(e => e.MaintenanceRecords)
                    .HasForeignKey(e => e.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Reminder configuration
            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.HasIndex(e => e.ReminderDate);
                entity.HasIndex(e => e.ReminderType);
                entity.HasIndex(e => e.IsCompleted);
                entity.Property(e => e.IsCompleted).HasDefaultValue(false);
            });

            // AppSetting configuration
            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.HasIndex(e => e.SettingKey).IsUnique();
            });

            // Seed default categories
            modelBuilder.Entity<Category>().HasData(
                // Income categories
                new Category { Id = 1, Name = "Salary", CategoryType = "Income", IsActive = true },
                new Category { Id = 2, Name = "Interest", CategoryType = "Income", IsActive = true },
                new Category { Id = 3, Name = "Dividend", CategoryType = "Income", IsActive = true },
                new Category { Id = 4, Name = "Other Income", CategoryType = "Income", IsActive = true },
                // Expense categories
                new Category { Id = 5, Name = "Food", CategoryType = "Expense", IsActive = true },
                new Category { Id = 6, Name = "Petrol", CategoryType = "Expense", IsActive = true },
                new Category { Id = 7, Name = "Car", CategoryType = "Expense", IsActive = true },
                new Category { Id = 8, Name = "Utilities", CategoryType = "Expense", IsActive = true },
                new Category { Id = 9, Name = "Shopping", CategoryType = "Expense", IsActive = true },
                new Category { Id = 10, Name = "Household", CategoryType = "Expense", IsActive = true },
                new Category { Id = 11, Name = "Medical", CategoryType = "Expense", IsActive = true },
                new Category { Id = 12, Name = "Other Expense", CategoryType = "Expense", IsActive = true }
            );
        }
    }
}