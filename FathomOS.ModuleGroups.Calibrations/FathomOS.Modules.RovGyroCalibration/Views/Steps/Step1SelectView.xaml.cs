using FathomOS.Modules.RovGyroCalibration.ViewModels.Steps;
using System.Windows.Input;

namespace FathomOS.Modules.RovGyroCalibration.Views.Steps;

public partial class Step1SelectView : System.Windows.Controls.UserControl
{
    public Step1SelectView()
    {
        InitializeComponent();
    }

    private void CalibrationCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is Step1SetupViewModel vm)
            vm.IsCalibrationMode = true;
    }

    private void VerificationCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is Step1SetupViewModel vm)
            vm.IsVerificationMode = true;
    }
}
