using System.Windows;
using MahApps.Metro.Controls;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

namespace FathomOS.Modules.EquipmentInventory.Views.Dialogs;

public partial class ShipmentVerificationDialog : MetroWindow
{
    /// <summary>
    /// Default constructor - used when DataContext is set externally
    /// </summary>
    public ShipmentVerificationDialog()
    {
        LoadThemeAndInitialize();
    }
    
    /// <summary>
    /// Constructor for verifying a specific manifest
    /// </summary>
    public ShipmentVerificationDialog(LocalDatabaseService dbService, Manifest manifest)
    {
        LoadThemeAndInitialize();
        
        // Create and set the ViewModel with the manifest
        var authService = new AuthenticationService();
        var viewModel = new ShipmentVerificationViewModel(dbService, authService, manifest);
        DataContext = viewModel;
        SetupDialogResultBinding(viewModel);
    }
    
    /// <summary>
    /// Constructor for general shipment verification (select from list)
    /// </summary>
    public ShipmentVerificationDialog(LocalDatabaseService dbService, AuthenticationService authService)
    {
        LoadThemeAndInitialize();
        
        var viewModel = new ShipmentVerificationViewModel(dbService, authService);
        DataContext = viewModel;
        SetupDialogResultBinding(viewModel);
    }
    
    /// <summary>
    /// Constructor with user location context
    /// </summary>
    public ShipmentVerificationDialog(LocalDatabaseService dbService, AuthenticationService authService, Guid userLocationId)
    {
        LoadThemeAndInitialize();
        
        var viewModel = new ShipmentVerificationViewModel(dbService, authService, userLocationId);
        DataContext = viewModel;
        SetupDialogResultBinding(viewModel);
    }
    
    private void LoadThemeAndInitialize()
    {
        // Load theme
        var settings = ModuleSettings.Load();
        var themeName = settings.UseDarkTheme ? "DarkTheme" : "LightTheme";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeName}.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
        
        // Set up dialog result binding if DataContext is already set
        if (DataContext is ShipmentVerificationViewModel vm)
        {
            SetupDialogResultBinding(vm);
        }
    }
    
    private void SetupDialogResultBinding(ShipmentVerificationViewModel vm)
    {
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(vm.DialogResult) && vm.DialogResult.HasValue)
            {
                DialogResult = vm.DialogResult;
                Close();
            }
        };
    }
}
