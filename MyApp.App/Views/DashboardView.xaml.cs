using System.Windows.Controls;

namespace MyApp.App
{
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            // Refresh whenever the dashboard becomes the active view.
            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DashboardViewModel viewModel)
            {
                await viewModel.RefreshAsync();
            }
        }
    }
}