using System.IO;
using System.Text.Json;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Persistent module settings stored in JSON
/// </summary>
public class ModuleSettings
{
    // Server connection
    public string ApiBaseUrl { get; set; } = "https://api.s7solutions.com/equipment";
    
    // Authentication
    public string? LastUsername { get; set; }
    public string? SavedUsername { get; set; }  // For "Remember me" feature
    public Guid? LastLocationId { get; set; }
    public string? DefaultLocationCode { get; set; }
    
    // Asset Generation
    public string AssetNumberPrefix { get; set; } = "S7-";
    
    // Label Settings
    public bool IncludeLogoOnLabels { get; set; } = true;
    
    // Backup Path
    public string? BackupPath { get; set; }
    
    // Appearance
    public bool UseDarkTheme { get; set; } = true;
    public double WindowWidth { get; set; } = 1400;
    public double WindowHeight { get; set; } = 900;
    public bool IsMaximized { get; set; } = false;
    
    // Sync
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalMinutes { get; set; } = 5;
    public DateTime? LastSyncTime { get; set; }
    
    // Notifications
    public bool ShowNotifications { get; set; } = true;
    public bool NotifyCertificationExpiry { get; set; } = true;
    public bool NotifyCalibrationDue { get; set; } = true;
    public bool NotifyLowStock { get; set; } = true;
    public int CertificationWarningDays { get; set; } = 30;
    public int CalibrationWarningDays { get; set; } = 7;
    
    // Export preferences
    public string? LastExportPath { get; set; }
    public string DefaultExportFormat { get; set; } = "xlsx";
    
    // QR Label / Organization preferences
    public string LabelPreset { get; set; } = "Standard";
    public string OrganizationName { get; set; } = "S7 Solutions";
    public string OrganizationCode { get; set; } = "S7";  // Used in unique ID generation (e.g., S7WSS04068)
    public bool ShowOrganizationOnLabel { get; set; } = true;
    public bool ShowUniqueIdOnLabel { get; set; } = true;
    public bool AutoGenerateUniqueId { get; set; } = true;  // Auto-generate on new equipment
    
    // Label Printer Configuration
    public LabelPrinterSettings PrinterSettings { get; set; } = new();
    
    // Backup Settings
    public BackupSettings BackupSettings { get; set; } = new();
    
    // Search & Filter Persistence
    public SearchFilterSettings LastSearchFilters { get; set; } = new();
    public bool PersistSearchFilters { get; set; } = true;
    
    // Recent Items
    public List<Guid> RecentEquipmentIds { get; set; } = new();
    public List<Guid> FavoriteEquipmentIds { get; set; } = new();
    public int MaxRecentItems { get; set; } = 20;
    
    // Legacy compatibility
    [Obsolete("Use OrganizationName instead")]
    public string? CompanyName 
    { 
        get => OrganizationName; 
        set { if (value != null) OrganizationName = value; }
    }
    
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FathomOS", "EquipmentInventory", "settings.json");
    
    public static ModuleSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<ModuleSettings>(json) ?? new ModuleSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
        }
        return new ModuleSettings();
    }
    
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(directory);
            
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
    
    public void SaveWindowState(double width, double height, bool isMaximized)
    {
        WindowWidth = width;
        WindowHeight = height;
        IsMaximized = isMaximized;
        Save();
    }
}

/// <summary>
/// Label printer configuration settings
/// </summary>
public class LabelPrinterSettings
{
    /// <summary>
    /// Selected printer name (from system printers)
    /// </summary>
    public string? PrinterName { get; set; }
    
    /// <summary>
    /// Type of label printer for appropriate driver settings
    /// </summary>
    public LabelPrinterType PrinterType { get; set; } = LabelPrinterType.Standard;
    
    /// <summary>
    /// Label size preset
    /// </summary>
    public LabelSize LabelSize { get; set; } = LabelSize.Medium50x50mm;
    
    /// <summary>
    /// Custom label width in mm (when LabelSize is Custom)
    /// </summary>
    public double CustomWidthMm { get; set; } = 50;
    
    /// <summary>
    /// Custom label height in mm (when LabelSize is Custom)
    /// </summary>
    public double CustomHeightMm { get; set; } = 50;
    
    /// <summary>
    /// Print quality DPI
    /// </summary>
    public int DPI { get; set; } = 300;
    
    /// <summary>
    /// Number of copies to print by default
    /// </summary>
    public int DefaultCopies { get; set; } = 1;
    
    /// <summary>
    /// Print orientation
    /// </summary>
    public LabelOrientation Orientation { get; set; } = LabelOrientation.Portrait;
    
    /// <summary>
    /// Show print preview before printing
    /// </summary>
    public bool ShowPreview { get; set; } = true;
    
    /// <summary>
    /// Auto-print after generating label (skip preview)
    /// </summary>
    public bool AutoPrint { get; set; } = false;
    
    /// <summary>
    /// Include equipment name on label
    /// </summary>
    public bool IncludeEquipmentName { get; set; } = false;
    
    /// <summary>
    /// Font size for text on label (points)
    /// </summary>
    public int FontSize { get; set; } = 12;
    
    /// <summary>
    /// Margin around label content in mm
    /// </summary>
    public double MarginMm { get; set; } = 2;
}

/// <summary>
/// Supported label printer types
/// </summary>
public enum LabelPrinterType
{
    /// <summary>Standard Windows printer</summary>
    Standard,
    
    /// <summary>Zebra ZPL-compatible printers (ZD420, ZT410, etc.)</summary>
    ZebraZPL,
    
    /// <summary>DYMO LabelWriter series</summary>
    DYMO,
    
    /// <summary>Brother P-Touch and QL series</summary>
    Brother,
    
    /// <summary>Epson LabelWorks</summary>
    Epson,
    
    /// <summary>TSC label printers</summary>
    TSC,
    
    /// <summary>Honeywell/Intermec printers</summary>
    Honeywell,
    
    /// <summary>Generic thermal printer</summary>
    GenericThermal
}

/// <summary>
/// Common label sizes for industrial/equipment labels
/// </summary>
public enum LabelSize
{
    /// <summary>25mm x 25mm (1" x 1")</summary>
    Small25x25mm,
    
    /// <summary>38mm x 25mm (1.5" x 1")</summary>
    Small38x25mm,
    
    /// <summary>50mm x 25mm (2" x 1")</summary>
    Medium50x25mm,
    
    /// <summary>50mm x 50mm (2" x 2") - Most common</summary>
    Medium50x50mm,
    
    /// <summary>60mm x 40mm (2.4" x 1.6")</summary>
    Medium60x40mm,
    
    /// <summary>75mm x 50mm (3" x 2")</summary>
    Large75x50mm,
    
    /// <summary>100mm x 50mm (4" x 2")</summary>
    Large100x50mm,
    
    /// <summary>100mm x 75mm (4" x 3")</summary>
    Large100x75mm,
    
    /// <summary>Custom size (use CustomWidthMm/CustomHeightMm)</summary>
    Custom
}

/// <summary>
/// Label print orientation
/// </summary>
public enum LabelOrientation
{
    Portrait,
    Landscape
}

/// <summary>
/// Persisted search and filter settings
/// </summary>
public class SearchFilterSettings
{
    public string? SearchText { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? LocationId { get; set; }
    public string? StatusFilter { get; set; }
    public bool? ShowInactiveOnly { get; set; }
    public string? SortColumn { get; set; }
    public bool SortAscending { get; set; } = true;
}

/// <summary>
/// Backup configuration settings
/// </summary>
public class BackupSettings
{
    public bool AutoBackupEnabled { get; set; } = false;
    public BackupFrequency BackupFrequency { get; set; } = BackupFrequency.Daily;
    public int KeepBackupCount { get; set; } = 10;
    public DateTime? LastBackup { get; set; }
    public DateTime? LastAutoBackup { get; set; }
    public string? BackupLocation { get; set; }
    public bool BackupToCloud { get; set; } = false;
    public string? CloudFolderPath { get; set; }
}

/// <summary>
/// How often automatic backups should run
/// </summary>
public enum BackupFrequency
{
    Daily,
    Weekly,
    Monthly
}
