using System.Windows.Controls;

namespace MyApp.App
{
    public partial class VehicleView : System.Windows.Controls.UserControl
    {
        public VehicleView()
        {
            InitializeComponent();

            // Load whenever the Vehicle screen becomes the active view
            Loaded += VehicleView_Loaded;
        }

        private async void VehicleView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.VehicleViewModel viewModel)
            {
                await viewModel.LoadVehiclesAsync();
            }
        }
    }
}