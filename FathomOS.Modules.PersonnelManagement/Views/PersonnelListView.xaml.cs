using System.Windows.Controls;

namespace FathomOS.Modules.PersonnelManagement.Views;

/// <summary>
/// Personnel list view with search/filter capabilities
/// Uses MVVM pattern - all logic is in PersonnelListViewModel
/// </summary>
public partial class PersonnelListView : UserControl
{
    public PersonnelListView()
    {
        InitializeComponent();
    }
}
