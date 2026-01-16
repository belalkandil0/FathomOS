using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class QrLabelPrintDialog : MetroWindow
{
    private readonly QrLabelPrintViewModel _viewModel;
    
    public QrLabelPrintDialog(LocalDatabaseService dbService)
    {
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new QrLabelPrintViewModel(dbService);
        DataContext = _viewModel;
        
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(QrLabelPrintViewModel.DialogResult) && _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
                Close();
            }
        };
    }
}
