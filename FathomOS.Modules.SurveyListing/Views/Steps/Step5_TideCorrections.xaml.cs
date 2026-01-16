using System.Windows;
using System.Windows.Controls;
using FathomOS.Modules.SurveyListing.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace FathomOS.Modules.SurveyListing.Views.Steps;

public partial class Step5_TideCorrections : UserControl
{
    public Step5_TideCorrections()
    {
        InitializeComponent();
    }

    private void BrowseTide_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step5ViewModel vm) vm.BrowseTideFile();
    }

    private void ClearTide_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step5ViewModel vm) vm.ClearTideFile();
    }

    private void AddFix_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step5ViewModel vm) vm.AddSurveyFix();
    }

    private void ImportFixes_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step5ViewModel vm) vm.ImportFixesFromCsv();
    }
}
