using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using System.Linq;

namespace MyApp.App.Services
{
    public class DatabaseInitializationService
    {
        private readonly AppDbContext _context;

        public DatabaseInitializationService(AppDbContext context)
        {
            _context = context;
        }

        public void InitializeDatabase()
        {
            try
            {
                // Create database if it doesn't exist
                _context.Database.EnsureCreated();
                
                // Check if database was just created (no categories)
                var hasCategories = _context.Categories.Any();
                if (!hasCategories)
                {
                    // Database was just created with seeded data
                    Console.WriteLine("Database initialized with default categories.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }
    }
}