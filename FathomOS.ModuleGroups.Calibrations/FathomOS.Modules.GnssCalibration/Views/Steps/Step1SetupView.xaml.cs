using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using FathomOS.Modules.GnssCalibration.ViewModels;

namespace FathomOS.Modules.GnssCalibration.Views.Steps;

public partial class Step1SetupView : UserControl
{
    public Step1SetupView()
    {
        InitializeComponent();
    }
    
    private void BrowseNpdFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select GNSS Survey Data File",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FilterIndex = 1,
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            if (DataContext is MainViewModel vm)
            {
                if (vm.LoadFileCommand.CanExecute(dialog.FileName))
                {
                    vm.LoadFileCommand.Execute(dialog.FileName);
                }
            }
        }
    }
    
    private void BrowsePosFile_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (vm.BrowsePosFileCommand.CanExecute(null))
            {
                vm.BrowsePosFileCommand.Execute(null);
            }
        }
    }
}
