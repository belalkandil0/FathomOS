using FathomOS.Modules.EquipmentInventory.ViewModels;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class ReportsView : System.Windows.Controls.UserControl
{
    public ReportsView(MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }
}
