using System.Windows.Controls;

namespace FathomOS.Modules.PersonnelManagement.Views;

/// <summary>
/// Vessel assignment view with sign-on/sign-off workflow
/// Uses MVVM pattern - all logic is in VesselAssignmentViewModel
/// </summary>
public partial class VesselAssignmentView : UserControl
{
    public VesselAssignmentView()
    {
        InitializeComponent();
    }
}
