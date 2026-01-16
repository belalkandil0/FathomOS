using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class SettingsDialog : MetroWindow
{
    private readonly SettingsViewModel _viewModel;
    
    public SettingsDialog()
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new SettingsViewModel();
        DataContext = _viewModel;
        
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.DialogResult) && _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
                Close();
            }
        };
        
        // Subscribe to theme changes to update this dialog
        ThemeService.Instance.ThemeChanged += (s, isDark) =>
        {
            ThemeService.Instance.ApplyTheme(this, isDark);
        };
    }
}
