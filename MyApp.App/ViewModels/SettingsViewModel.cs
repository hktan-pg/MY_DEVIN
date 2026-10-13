using System.Windows.Input;
using MyApp.App.Services;

namespace MyApp.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsService _settingsService;
        private readonly BackupService _backupService;
        private string _currency = "MYR";
        private string _currencySymbol = "RM";
        private string _dateFormat = "dd/MM/yyyy";
        private string _startPage = "Dashboard";
        private DatabaseStatistics _statistics = new DatabaseStatistics();
        private BackupInfo _lastBackupInfo = new BackupInfo();

        public SettingsViewModel(SettingsService settingsService, BackupService backupService)
        {
            _settingsService = settingsService;
            _backupService = backupService;
            
            LoadSettingsCommand = new RelayCommand(_ => LoadSettingsAsync().GetAwaiter().GetResult());
            SaveSettingsCommand = new RelayCommand(_ => SaveSettingsAsync().GetAwaiter().GetResult());
            RefreshStatisticsCommand = new RelayCommand(_ => RefreshStatisticsAsync().GetAwaiter().GetResult());
            BackupCommand = new RelayCommand(_ => Backup());
            RestoreCommand = new RelayCommand(_ => Restore());
            RefreshBackupInfoCommand = new RelayCommand(_ => LoadBackupInfo().GetAwaiter().GetResult());
            
            // Load initial settings
            _ = LoadSettingsAsync();
            _ = RefreshStatisticsAsync();
            _ = LoadBackupInfo();
        }

        public string Currency
        {
            get => _currency;
            set => SetProperty(ref _currency, value);
        }

        public string CurrencySymbol
        {
            get => _currencySymbol;
            set => SetProperty(ref _currencySymbol, value);
        }

        public string DateFormat
        {
            get => _dateFormat;
            set => SetProperty(ref _dateFormat, value);
        }

        public string StartPage
        {
            get => _startPage;
            set => SetProperty(ref _startPage, value);
        }

        public DatabaseStatistics Statistics
        {
            get => _statistics;
            set => SetProperty(ref _statistics, value);
        }

        public BackupInfo LastBackupInfo
        {
            get => _lastBackupInfo;
            set => SetProperty(ref _lastBackupInfo, value);
        }

        public ICommand LoadSettingsCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand RefreshStatisticsCommand { get; }
        public ICommand BackupCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand RefreshBackupInfoCommand { get; }

        public async Task LoadSettingsAsync()
        {
            await _settingsService.InitializeDefaultSettingsAsync();
            
            Currency = await _settingsService.GetSettingAsync("Currency", "MYR");
            CurrencySymbol = await _settingsService.GetSettingAsync("CurrencySymbol", "RM");
            DateFormat = await _settingsService.GetSettingAsync("DateFormat", "dd/MM/yyyy");
            StartPage = await _settingsService.GetSettingAsync("StartPage", "Dashboard");
        }

        public async Task SaveSettingsAsync()
        {
            await _settingsService.SetSettingAsync("Currency", Currency);
            await _settingsService.SetSettingAsync("CurrencySymbol", CurrencySymbol);
            await _settingsService.SetSettingAsync("DateFormat", DateFormat);
            await _settingsService.SetSettingAsync("StartPage", StartPage);
        }

        public async Task RefreshStatisticsAsync()
        {
            Statistics = await _settingsService.GetDatabaseStatisticsAsync();
        }

        public async Task LoadBackupInfo()
        {
            LastBackupInfo = _backupService.GetLastBackupInfo();
        }

        private async void Backup()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var result = await _backupService.CreateBackupAsync(dialog.SelectedPath);
                
                if (result.Success)
                {
                    System.Windows.MessageBox.Show(result.Message, "Backup Successful", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    await LoadBackupInfo();
                }
                else
                {
                    System.Windows.MessageBox.Show(result.Message, "Backup Failed", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private async void Restore()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "MyApp Backup Files (*.db)|*.db|All Files (*.*)|*.*";
            dialog.Title = "Select MyApp Backup File";
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var result = System.Windows.MessageBox.Show(
                    "Restoring a database will replace the current MyApp data. Make sure you have a backup of the current database.\n\nDo you want to continue?",
                    "Confirm Restore",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var restoreResult = await _backupService.RestoreDatabaseAsync(dialog.FileName);
                    
                    if (restoreResult.Success)
                    {
                        System.Windows.MessageBox.Show(restoreResult.Message, "Restore Successful", 
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        System.Windows.MessageBox.Show(restoreResult.Message, "Restore Failed", 
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}