using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class AddUnregisteredItemDialog : MetroWindow
{
    private readonly AddUnregisteredItemViewModel _viewModel;
    
    public AddUnregisteredItemDialog(LocalDatabaseService dbService, Guid? locationId = null)
    {
        // Load theme before InitializeComponent
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        _viewModel = new AddUnregisteredItemViewModel(dbService, locationId);
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.DialogResult) && _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
            }
        };
        
        DataContext = _viewModel;
    }
    
    /// <summary>
    /// The created unregistered item (available after dialog closes with OK)
    /// </summary>
    public UnregisteredItem? CreatedItem => _viewModel.CreatedItem;
}
