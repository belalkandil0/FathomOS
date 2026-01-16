using System.Windows;
using System.Windows.Controls;
using FathomOS.Core.Models;
using FathomOS.Modules.SurveyListing.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace FathomOS.Modules.SurveyListing.Views.Steps;

public partial class Step1_ProjectSetup : UserControl
{
    public Step1_ProjectSetup()
    {
        InitializeComponent();
    }
    
    private void PresetUsFeet_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step1ViewModel vm)
        {
            vm.SelectedInputUnit = LengthUnit.USSurveyFeet;
            vm.SelectedOutputUnit = LengthUnit.USSurveyFeet;
        }
    }
    
    private void PresetMeters_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step1ViewModel vm)
        {
            vm.SelectedInputUnit = LengthUnit.Meter;
            vm.SelectedOutputUnit = LengthUnit.Meter;
        }
    }
    
    private void PresetMetersToUsFeet_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step1ViewModel vm)
        {
            vm.SelectedInputUnit = LengthUnit.Meter;
            vm.SelectedOutputUnit = LengthUnit.USSurveyFeet;
        }
    }
}
