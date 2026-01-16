using System.Windows;

namespace FathomOS.TimeSyncAgent.Tray;

public partial class StatusWindow : Window
{
    public StatusWindow()
    {
        InitializeComponent();
    }

    private TrayViewModel? ViewModel => DataContext as TrayViewModel;

    private void Window_Deactivated(object sender, EventArgs e)
    {
        // Hide when clicking outside
        Hide();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void StartService_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.StartService();
    }

    private void StopService_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.StopService();
    }

    private void RestartService_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.RestartService();
    }
}
