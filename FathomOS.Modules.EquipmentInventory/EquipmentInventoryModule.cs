using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.EquipmentInventory.Views;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Services;


namespace FathomOS.Modules.EquipmentInventory;

/// <summary>
/// Main module entry point implementing the Fathom OS IModule interface.
/// Equipment &amp; Inventory Management Module for offshore and onshore operations.
/// 
/// Certificate System Integration:
/// - Certificate Code: EI
/// - Certificate Title: Equipment &amp; Inventory Management Verification Certificate
/// 
/// NOTE: Namespace will change from S7Fathom to FathomOS upon integration.
/// </summary>
public class EquipmentInventoryModule : IModule
{
    private MainWindow? _mainWindow;
    private readonly LocalDatabaseService _databaseService;
    private readonly SyncService _syncService;
    private readonly AuthenticationService _authService;
    
    // Certificate configuration (matches ModuleInfo.json)
    public const string CertificateCode = "EI";
    public const string CertificateTitle = "Equipment & Inventory Management Verification Certificate";
    public const string CertificateStatement = "This is to certify that the equipment inventory and manifest operations documented herein have been successfully processed and verified in accordance with industry standards for asset tracking and management.";
    
    public EquipmentInventoryModule()
    {
        _databaseService = new LocalDatabaseService();
        _authService = new AuthenticationService();
        _syncService = new SyncService(_databaseService, _authService);
    }
    
    #region IModule Properties
    
    /// <summary>
    /// Unique identifier for the module. Must match DLL name.
    /// </summary>
    public string ModuleId => "EquipmentInventory";
    
    /// <summary>
    /// Display name shown on dashboard tile.
    /// </summary>
    public string DisplayName => "Equipment & Inventory";
    
    /// <summary>
    /// Description shown in tooltips and module info.
    /// </summary>
    public string Description => "Manage equipment, inventory, and transfer manifests across offshore and onshore locations with QR code tracking and mobile sync.";
    
    /// <summary>
    /// Module version.
    /// </summary>
    public Version Version => new Version(1, 0, 0);
    
    /// <summary>
    /// Path to module icon resource.
    /// NOTE: Will change to FathomOS.Modules.EquipmentInventory upon integration.
    /// </summary>
    public string IconResource => "/FathomOS.Modules.EquipmentInventory;component/Assets/icon.png";
    
    /// <summary>
    /// Module category for grouping.
    /// </summary>
    public string Category => "Operations Management";
    
    /// <summary>
    /// Sort order for dashboard display.
    /// </summary>
    public int DisplayOrder => 5;
    
    #endregion
    
    #region IModule Methods
    
    /// <summary>
    /// Called when module is loaded. Initialize services and database.
    /// </summary>
    public void Initialize()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Initializing v{Version}...");
            
            // Initialize local database
            _databaseService.Initialize();
            
            // Load settings
            var settings = ModuleSettings.Load();
            
            // Initialize sync service with settings
            _syncService.Configure(settings.ApiBaseUrl);
            
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Initialization complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Initialization error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called when user clicks module tile. Launch main window after authentication.
    /// </summary>
    public void Launch(Window? owner = null)
    {
        try
        {
            // Check if already authenticated and window is open
            if (_mainWindow != null && _mainWindow.IsLoaded && _authService.IsAuthenticated)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
                if (_mainWindow.WindowState == WindowState.Minimized)
                    _mainWindow.WindowState = WindowState.Normal;
                return;
            }
            
            // Show login window first
            var loginWindow = new LoginWindow(_authService, _databaseService);
            loginWindow.Owner = owner;
            
            var loginResult = loginWindow.ShowDialog();
            
            if (loginResult != true || !loginWindow.IsAuthenticated)
            {
                // User cancelled or failed to authenticate
                System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Authentication cancelled or failed");
                return;
            }
            
            // Authentication successful - launch main window
            _mainWindow = new MainWindow(_databaseService, _syncService, _authService);
            _mainWindow.Show();
            _mainWindow.Activate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to launch {DisplayName}:\n\n{ex.Message}",
                "Module Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Launch error: {ex}");
        }
    }
    
    /// <summary>
    /// Called when application is closing. Clean up resources.
    /// </summary>
    public void Shutdown()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Shutting down...");
            
            // Save any pending changes
            _syncService?.SyncAsync().Wait(TimeSpan.FromSeconds(5));
            
            // Close main window
            _mainWindow?.Close();
            _mainWindow = null;
            
            // Dispose services
            _databaseService?.Dispose();
            
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Shutdown complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{DisplayName}] Shutdown error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Check if this module can handle the specified file type.
    /// </summary>
    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".xlsx" or ".csv" or ".json";
    }
    
    /// <summary>
    /// Open a file directly in this module.
    /// </summary>
    public void OpenFile(string filePath)
    {
        try
        {
            Launch();
            
            if (_mainWindow?.DataContext is ViewModels.MainViewModel viewModel)
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                switch (extension)
                {
                    case ".xlsx":
                    case ".csv":
                        // Import equipment from file
                        viewModel.ImportFromFileCommand.Execute(filePath);
                        break;
                    case ".json":
                        // Import configuration or data
                        viewModel.ImportConfigurationCommand.Execute(filePath);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open file:\n\n{ex.Message}",
                "File Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
    
    #endregion
    
    #region Certificate Support
    
    /// <summary>
    /// Get certificate processing data for manifest or inventory operations.
    /// Called by Fathom OS certificate service.
    /// </summary>
    /// <param name="operationType">Type of operation (Manifest, Inventory, Audit, etc.)</param>
    /// <param name="context">Operation-specific context data</param>
    /// <returns>Dictionary of key-value pairs for certificate</returns>
    public Dictionary<string, string> GetCertificateProcessingData(string operationType, object? context)
    {
        var data = new Dictionary<string, string>();
        
        switch (operationType)
        {
            case "ManifestCompletion":
                if (context is Models.Manifest manifest)
                {
                    data["Manifest Number"] = manifest.ManifestNumber;
                    data["Manifest Type"] = manifest.Type.ToString();
                    data["Equipment Items"] = manifest.Items.Count.ToString();
                    data["From Location"] = manifest.FromLocation?.DisplayName ?? "N/A";
                    data["To Location"] = manifest.ToLocation?.DisplayName ?? "N/A";
                    data["Transfer Date"] = manifest.ShippedDate?.ToString("dd MMM yyyy") ?? manifest.CreatedAt.ToString("dd MMM yyyy");
                    data["Status"] = manifest.Status.ToString();
                }
                break;
                
            case "InventoryAudit":
                // Use Task.Run to avoid potential deadlocks on UI thread
                var equipmentCount = Task.Run(async () => await _databaseService.GetEquipmentCountAsync()).GetAwaiter().GetResult();
                var locations = Task.Run(async () => await _databaseService.GetLocationsAsync()).GetAwaiter().GetResult();
                data["Total Equipment Items"] = equipmentCount.ToString();
                data["Locations Covered"] = locations.Count.ToString();
                data["Audit Date"] = DateTime.Now.ToString("dd MMM yyyy HH:mm UTC");
                data["Items Verified"] = "All";
                break;
                
            case "EquipmentCertification":
                if (context is Models.Equipment equipment)
                {
                    data["Asset Number"] = equipment.AssetNumber;
                    data["Equipment Name"] = equipment.Name;
                    data["Manufacturer"] = equipment.Manufacturer ?? "N/A";
                    data["Model"] = equipment.Model ?? "N/A";
                    data["Serial Number"] = equipment.SerialNumber ?? "N/A";
                    data["Current Location"] = equipment.CurrentLocation?.DisplayName ?? "N/A";
                    data["Certification Status"] = equipment.CertificationExpiryDate.HasValue 
                        ? (equipment.CertificationExpiryDate > DateTime.Now ? "Valid" : "Expired")
                        : "N/A";
                    data["Calibration Status"] = equipment.NextCalibrationDate.HasValue
                        ? (equipment.NextCalibrationDate > DateTime.Now ? "Current" : "Due")
                        : "N/A";
                }
                break;
                
            case "DefectReport":
                if (context is Models.DefectReport defect)
                {
                    data["Report Number"] = defect.ReportNumber;
                    data["Report Date"] = defect.ReportDate.ToString("dd MMM yyyy");
                    data["Fault Category"] = defect.FaultCategory.ToString();
                    data["Major Component"] = defect.MajorComponent ?? "N/A";
                    data["Status"] = defect.Status.ToString();
                    data["Urgency"] = defect.ReplacementUrgency.ToString();
                    data["Resolution"] = defect.Status == Models.DefectReportStatus.Resolved 
                        ? $"Resolved on {defect.ResolvedAt?.ToString("dd MMM yyyy") ?? "N/A"}"
                        : "Pending";
                }
                break;
        }
        
        return data;
    }
    
    /// <summary>
    /// Get recommended signatory titles for this module.
    /// </summary>
    public List<string> GetSignatoryTitles()
    {
        return new List<string>
        {
            "Operations Manager",
            "Logistics Coordinator",
            "Inventory Manager",
            "Equipment Manager",
            "Supply Chain Supervisor",
            "Asset Manager",
            "Warehouse Supervisor",
            "Project Manager",
            "Technical Supervisor",
            "Quality Control Engineer",
            "Senior Technician"
        };
    }
    
    #endregion
}
