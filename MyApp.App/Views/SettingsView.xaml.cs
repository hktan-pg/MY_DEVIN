using System.Windows.Controls;

namespace MyApp.App
{
    public partial class SettingsView : System.Windows.Controls.UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            Loaded += SettingsView_Loaded;
        }

        private async void SettingsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.SettingsViewModel viewModel)
            {
                await viewModel.LoadSettingsAsync();
                await viewModel.RefreshStatisticsAsync();
                await viewModel.LoadBackupInfo();
            }
        }
    }
}