using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using MyApp.App.Data;
using MyApp.App.ViewModels;
using MyApp.App.Services;
using System.IO;
using WpfApplication = System.Windows.Application;

namespace MyApp.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public static ServiceProvider? ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        ServiceProvider = serviceCollection.BuildServiceProvider();

        // Initialize database
        var dbInitializationService = ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        dbInitializationService.InitializeDatabase();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyApp", "myapp.db");
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDirectory) && !Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<AccountsViewModel>();
        services.AddTransient<TransactionsViewModel>();
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<VehicleViewModel>();
        services.AddTransient<MaintenanceViewModel>();
        services.AddTransient<RemindersViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddScoped<DatabaseInitializationService>();
        services.AddScoped<AccountService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<TransactionService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<ReminderService>();
        services.AddSingleton<SettingsService>();
        services.AddScoped<BackupService>();
    }
}

