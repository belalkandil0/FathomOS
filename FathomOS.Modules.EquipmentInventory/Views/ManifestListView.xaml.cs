using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.ViewModels;


namespace FathomOS.Modules.EquipmentInventory.Views;

public partial class ManifestListView : System.Windows.Controls.UserControl
{
    public ManifestListView(LocalDatabaseService dbService, MainViewModel mainViewModel)
    {
        InitializeComponent();
        DataContext = mainViewModel;
    }
}
