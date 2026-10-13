using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using System.IO;

namespace MyApp.App.Services
{
    public class BackupService
    {
        private readonly AppDbContext _context;
        private readonly string _databasePath;

        public BackupService(AppDbContext context)
        {
            _context = context;
            _databasePath = GetDatabasePath();
        }

        private string GetDatabasePath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyApp",
                "myapp.db");
        }

        public async Task<BackupResult> CreateBackupAsync(string destinationFolder)
        {
            try
            {
                // Ensure pending changes are saved
                await _context.SaveChangesAsync();

                // Create backup filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"MyApp_Backup_{timestamp}.db";
                var backupPath = Path.Combine(destinationFolder, backupFileName);

                // Ensure destination directory exists
                Directory.CreateDirectory(destinationFolder);

                // Copy database file
                File.Copy(_databasePath, backupPath, true);

                return new BackupResult
                {
                    Success = true,
                    BackupPath = backupPath,
                    BackupDate = DateTime.Now,
                    Message = $"Backup created successfully at {backupPath}"
                };
            }
            catch (Exception ex)
            {
                return new BackupResult
                {
                    Success = false,
                    Message = $"Backup failed: {ex.Message}"
                };
            }
        }

        public async Task<RestoreResult> RestoreDatabaseAsync(string backupFilePath)
        {
            try
            {
                // Validate backup file
                if (!File.Exists(backupFilePath))
                {
                    return new RestoreResult
                    {
                        Success = false,
                        Message = "Backup file not found."
                    };
                }

                if (!IsValidMyAppDatabase(backupFilePath))
                {
                    return new RestoreResult
                    {
                        Success = false,
                        Message = "Invalid MyApp database file."
                    };
                }

                // Create emergency backup of current database
                var emergencyBackupPath = await CreateEmergencyBackupAsync();
                if (string.IsNullOrEmpty(emergencyBackupPath))
                {
                    return new RestoreResult
                    {
                        Success = false,
                        Message = "Failed to create emergency backup."
                    };
                }

                // Close database connections
                await _context.Database.CloseConnectionAsync();

                // Replace database file
                File.Copy(backupFilePath, _databasePath, true);

                return new RestoreResult
                {
                    Success = true,
                    EmergencyBackupPath = emergencyBackupPath,
                    Message = "Database restored successfully. Please restart the application."
                };
            }
            catch (Exception ex)
            {
                return new RestoreResult
                {
                    Success = false,
                    Message = $"Restore failed: {ex.Message}"
                };
            }
        }

        private async Task<string> CreateEmergencyBackupAsync()
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var emergencyBackupFileName = $"MyApp_EmergencyBackup_{timestamp}.db";
                var emergencyBackupPath = Path.Combine(
                    Path.GetDirectoryName(_databasePath) ?? "",
                    emergencyBackupFileName);

                // Save pending changes
                await _context.SaveChangesAsync();

                // Close connection
                await _context.Database.CloseConnectionAsync();

                // Copy file
                File.Copy(_databasePath, emergencyBackupPath, true);

                return emergencyBackupPath;
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool IsValidMyAppDatabase(string filePath)
        {
            try
            {
                // Check if file is a valid SQLite database
                using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={filePath}");
                connection.Open();

                // Check for expected MyApp tables
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Accounts', 'Categories', 'Transactions', 'Vehicles', 'MaintenanceRecords', 'Reminders', 'AppSettings')";
                
                var tableCount = 0;
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    tableCount++;
                }

                // Require at least some expected tables to be a valid MyApp database
                return tableCount >= 3;
            }
            catch
            {
                return false;
            }
        }

        public BackupInfo GetLastBackupInfo()
        {
            var backupDirectory = Path.GetDirectoryName(_databasePath) ?? "";
            var backupFiles = Directory.GetFiles(backupDirectory, "MyApp_Backup_*.db")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (backupFiles == null)
            {
                return new BackupInfo
                {
                    HasBackup = false,
                    Message = "No backup found"
                };
            }

            var fileInfo = new FileInfo(backupFiles);
            return new BackupInfo
            {
                HasBackup = true,
                BackupPath = backupFiles,
                BackupDate = fileInfo.CreationTime,
                Message = $"Last backup: {fileInfo.Name}"
            };
        }
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string? BackupPath { get; set; }
        public DateTime BackupDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RestoreResult
    {
        public bool Success { get; set; }
        public string? EmergencyBackupPath { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BackupInfo
    {
        public bool HasBackup { get; set; }
        public string? BackupPath { get; set; }
        public DateTime BackupDate { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}