using System.Windows;
using System.Windows.Controls;
using FathomOS.Modules.SurveyListing.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace FathomOS.Modules.SurveyListing.Views.Steps;

public partial class Step7_OutputOptions : UserControl
{
    public Step7_OutputOptions()
    {
        InitializeComponent();
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step7ViewModel vm) vm.BrowseOutputFolder();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step7ViewModel vm) vm.OpenOutputFolder();
    }

    private void BrowseTemplate_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step7ViewModel vm) vm.BrowseDwgTemplate();
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step7ViewModel vm)
        {
            await vm.ExportAsync();
        }
    }
}
