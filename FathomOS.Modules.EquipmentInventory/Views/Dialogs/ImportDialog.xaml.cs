using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class ImportDialog : MetroWindow
{
    private readonly ImportViewModel _viewModel;
    
    public ImportDialog(LocalDatabaseService dbService)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new ImportViewModel(dbService);
        DataContext = _viewModel;
        
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ImportViewModel.DialogResult) && _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
                Close();
            }
        };
    }
    
    public int ImportedCount => _viewModel.Result?.Imported ?? 0;
}
