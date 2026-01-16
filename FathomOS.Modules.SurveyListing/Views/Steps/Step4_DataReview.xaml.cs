using System.Windows;
using System.Windows.Controls;
using FathomOS.Modules.SurveyListing.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace FathomOS.Modules.SurveyListing.Views.Steps;

public partial class Step4_DataReview : UserControl
{
    public Step4_DataReview()
    {
        InitializeComponent();
    }

    private async void LoadData_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step4ViewModel vm)
        {
            await vm.LoadDataAsync();
        }
    }

    private void ExportPreview_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is Step4ViewModel vm)
        {
            vm.ExportPreview();
        }
    }
}
