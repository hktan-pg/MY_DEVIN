using System.Windows.Controls;

namespace MyApp.App
{
    public partial class TransactionsView : System.Windows.Controls.UserControl
    {
        public TransactionsView()
        {
            InitializeComponent();
            Loaded += TransactionsView_Loaded;
        }

        private async void TransactionsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.TransactionsViewModel viewModel)
            {
                await viewModel.LoadTransactionsAsync();
            }
        }
    }
}
