using System.Windows;
using System.Windows.Controls;
using FathomOS.Modules.SurveyListing.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace FathomOS.Modules.SurveyListing.Views.Steps;

public partial class Step3_SurveyData : UserControl
{
    public Step3_SurveyData()
    {
        InitializeComponent();
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step3ViewModel vm) vm.AddFiles();
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && 
            button.DataContext is SurveyFileInfo file &&
            DataContext is Step3ViewModel vm)
        {
            vm.RemoveFile(file);
        }
    }

    private void ClearFiles_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step3ViewModel vm) vm.ClearFiles();
    }

    private void AutoDetect_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step3ViewModel vm) vm.AutoDetectColumns();
    }
}
