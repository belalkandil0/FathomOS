using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

using MediaColor = System.Windows.Media.Color;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class ManifestWizardDialog : MetroWindow
{
    private readonly ManifestWizardViewModel _viewModel;
    
    /// <summary>
    /// Constructor for creating a new manifest
    /// </summary>
    public ManifestWizardDialog(LocalDatabaseService dbService, ManifestType manifestType)
    {
        // Add local converters BEFORE InitializeComponent (these are not in XAML)
        Resources.Add("StepDotConverter", new StepDotConverter());
        Resources.Add("StepLineConverter", new StepLineConverter());
        
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new ManifestWizardViewModel(dbService, manifestType);
        DataContext = _viewModel;
        
        SetupDialogResultBinding();
    }
    
    /// <summary>
    /// Constructor for editing an existing manifest
    /// </summary>
    public ManifestWizardDialog(LocalDatabaseService dbService, ManifestType manifestType, Manifest existingManifest)
    {
        // Add local converters BEFORE InitializeComponent (these are not in XAML)
        Resources.Add("StepDotConverter", new StepDotConverter());
        Resources.Add("StepLineConverter", new StepLineConverter());
        
        InitializeComponent();
        
        // Apply theme AFTER InitializeComponent
        var settings = ModuleSettings.Load();
        ThemeService.Instance.ApplyTheme(this, settings.UseDarkTheme);
        
        _viewModel = new ManifestWizardViewModel(dbService, manifestType, existingManifest);
        DataContext = _viewModel;
        
        SetupDialogResultBinding();
    }
    
    private void SetupDialogResultBinding()
    {
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ManifestWizardViewModel.DialogResult) && _viewModel.DialogResult.HasValue)
            {
                DialogResult = _viewModel.DialogResult;
                Close();
            }
        };
    }
    
    public Manifest? GetManifest() => _viewModel.GetManifest();
}

public class StepDotConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isActive = value is bool b && b;
        return isActive ? new SolidColorBrush(MediaColor.FromRgb(74, 144, 217)) : new SolidColorBrush(MediaColor.FromRgb(100, 100, 100));
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StepLineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isPast = value is bool b && b;
        return isPast ? new SolidColorBrush(MediaColor.FromRgb(74, 144, 217)) : new SolidColorBrush(MediaColor.FromRgb(60, 60, 60));
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
