using System.Windows.Input;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class EquipmentSearchDialog : MetroWindow
{
    private readonly EquipmentSearchViewModel _viewModel;
    
    public EquipmentSearchDialog(LocalDatabaseService dbService)
    {
        // Load theme before InitializeComponent
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        _viewModel = new EquipmentSearchViewModel(dbService);
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.DialogResult) && _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
            }
        };
        
        DataContext = _viewModel;
        
        // Focus search box
        Loaded += (s, e) => SearchBox.Focus();
    }
    
    /// <summary>
    /// The selected equipment (available after dialog closes with OK)
    /// </summary>
    public Equipment? SelectedEquipment => _viewModel.SelectedEquipment;
    
    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedEquipment != null)
        {
            _viewModel.SelectCommand.Execute(null);
        }
    }
}
