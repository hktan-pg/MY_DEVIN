using System.Windows.Controls;

namespace MyApp.App
{
    public partial class CategoriesView : System.Windows.Controls.UserControl
    {
        public CategoriesView()
        {
            InitializeComponent();
            Loaded += CategoriesView_Loaded;
        }

        private async void CategoriesView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.CategoriesViewModel viewModel)
            {
                await viewModel.LoadCategoriesAsync();
            }
        }
    }
}