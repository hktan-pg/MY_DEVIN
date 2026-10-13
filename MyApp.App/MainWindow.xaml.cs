using System.Windows;
using System.Windows.Controls;
using MyApp.App.ViewModels;

namespace MyApp.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Unlock dark title bar to match the black theme
        WindowThemeHelper.EnableDarkTitleBar(this);
    }

    private void NavigationButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string viewName)
        {
            _viewModel.NavigateTo(viewName);
        }
    }
}