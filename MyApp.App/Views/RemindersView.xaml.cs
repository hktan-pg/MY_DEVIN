using System.Windows.Controls;

namespace MyApp.App
{
    public partial class RemindersView : System.Windows.Controls.UserControl
    {
        public RemindersView()
        {
            InitializeComponent();

            // Load whenever the Reminders screen becomes the active view
            Loaded += RemindersView_Loaded;
        }

        private async void RemindersView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.RemindersViewModel viewModel)
            {
                await viewModel.LoadRemindersAsync();
            }
        }
    }
}