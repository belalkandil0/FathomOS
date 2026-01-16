using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class EquipmentEditorDialog : MetroWindow
{
    private readonly EquipmentEditorViewModel _viewModel;
    
    public EquipmentEditorDialog(LocalDatabaseService dbService, Equipment? equipment = null)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent to avoid resource conflicts
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new EquipmentEditorViewModel(dbService, equipment);
        DataContext = _viewModel;
        
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(EquipmentEditorViewModel.DialogResult) && _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
                Close();
            }
        };
    }
    
    public Equipment? GetEquipment()
    {
        return DialogResult == true ? _viewModel.GetEquipment() : null;
    }
}
