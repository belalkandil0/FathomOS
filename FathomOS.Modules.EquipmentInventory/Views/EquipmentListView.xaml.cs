using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.ViewModels;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class EquipmentListView : System.Windows.Controls.UserControl
{
    public EquipmentListView(LocalDatabaseService dbService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }
}
