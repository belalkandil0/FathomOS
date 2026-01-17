using System.Windows.Controls;

namespace FathomOS.Modules.PersonnelManagement.Views;

/// <summary>
/// Timesheet management view with entry grid
/// Uses MVVM pattern - all logic is in TimesheetListViewModel
/// </summary>
public partial class TimesheetView : UserControl
{
    public TimesheetView()
    {
        InitializeComponent();
    }
}
