using System.Windows.Controls;

namespace MyApp.App
{
    public partial class AccountsView : System.Windows.Controls.UserControl
    {
        public AccountsView()
        {
            InitializeComponent();
            Loaded += AccountsView_Loaded;
        }

        private async void AccountsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AccountsViewModel viewModel)
            {
                await viewModel.LoadAccountsAsync();
            }
        }
    }
}