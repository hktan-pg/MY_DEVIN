using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MyApp.App.Services;

namespace MyApp.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase? _currentViewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly SettingsService _settingsService;

        public MainViewModel(IServiceProvider serviceProvider, SettingsService settingsService)
        {
            _serviceProvider = serviceProvider;
            _settingsService = settingsService;
            
            // Initialize with start page from settings
            var startPage = _settingsService.GetSettingAsync("StartPage", "Dashboard").GetAwaiter().GetResult();
            CurrentViewModel = GetViewModel(startPage);
            
            NavigateCommand = new RelayCommand(NavigateTo);
        }

        public ViewModelBase? CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ICommand NavigateCommand { get; }

        public void NavigateTo(object? parameter)
        {
            if (parameter is string viewName)
            {
                CurrentViewModel = GetViewModel(viewName);
            }
        }

        private ViewModelBase GetViewModel(string viewName)
        {
            return viewName switch
            {
                "Dashboard" => _serviceProvider.GetRequiredService<DashboardViewModel>(),
                "Accounts" => _serviceProvider.GetRequiredService<AccountsViewModel>(),
                "Transactions" => _serviceProvider.GetRequiredService<TransactionsViewModel>(),
                "Categories" => _serviceProvider.GetRequiredService<CategoriesViewModel>(),
                "Vehicle" => _serviceProvider.GetRequiredService<VehicleViewModel>(),
                "Maintenance" => _serviceProvider.GetRequiredService<MaintenanceViewModel>(),
                "Reminders" => _serviceProvider.GetRequiredService<RemindersViewModel>(),
                "Settings" => _serviceProvider.GetRequiredService<SettingsViewModel>(),
                _ => _serviceProvider.GetRequiredService<DashboardViewModel>()
            };
        }
    }
}