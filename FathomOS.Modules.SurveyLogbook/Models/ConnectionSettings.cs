// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/ConnectionSettings.cs
// Purpose: Connection and monitoring settings for the module
// ============================================================================

using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using FathomOS.Modules.SurveyLogbook.Services;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Contains all connection and monitoring settings for the Survey Logbook module.
/// </summary>
public class ConnectionSettings : INotifyPropertyChanged
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FathomOS", "SurveyLogbook", "settings.json");
    
    // NaviPac TCP/UDP Settings
    private bool _enableNaviPacConnection = true;
    private string _naviPacHost = "localhost";
    private int _naviPacPort = 8123;
    private NaviPacProtocol _naviPacProtocol = NaviPacProtocol.UDP;
    private bool _autoReconnect = true;
    private int _reconnectIntervalSeconds = 5;
    private int _connectionTimeoutMs = 10000;
    private bool _autoCreateFirewallRule = false;
    private char _naviPacSeparator = ',';
    private bool _enableDebugLogging = true;
    
    // v9.0.0 Phase 4: UDP Enhancements
    private string _udpBindInterface = string.Empty;  // Empty = all interfaces
    private bool _enableSourceIpFilter = false;
    private string _allowedSourceIps = string.Empty;  // Comma-separated list
    private bool _enableMulticast = false;
    private string _multicastGroup = string.Empty;
    private int _multicastTtl = 1;
    private bool _enableUdpBroadcast = true;
    
    // NaviPac File Monitoring
    private bool _enableFixMonitoring = true;
    private string _fixOutputFolder = @"C:\EIVA\NaviPac\Data";
    private bool _enableWaypointMonitoring = true;
    private string _waypointFolder = @"C:\EIVA\NaviPac\Waypoints";
    private bool _monitorSubdirectories = true;
    
    // DVR/VisualWorks Monitoring
    private bool _enableDvrMonitoring = true;
    private string _visualWorksProjectFolder = string.Empty;
    private bool _parseFolderHierarchy = true;
    private bool _monitorDvrSubdirectories = true;
    
    // Output Settings
    private string _defaultExportFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private int _autoSaveIntervalMinutes = 5;
    private bool _enableAutoSave = true;
    
    // Security Settings (v4.0)
    private TlsConfiguration? _tlsConfiguration;
    private RateLimitConfiguration? _rateLimitConfiguration;

    // UI Settings
    private bool _darkTheme = true;
    private double _windowWidth = 1400;
    private double _windowHeight = 900;
    
    // ========================================================================
    // NaviPac TCP/UDP Connection Settings
    // ========================================================================
    
    /// <summary>
    /// Enable NaviPac network connection.
    /// </summary>
    public bool EnableNaviPacConnection
    {
        get => _enableNaviPacConnection;
        set => SetProperty(ref _enableNaviPacConnection, value);
    }
    
    /// <summary>
    /// NaviPac host address (IP or hostname).
    /// For TCP: The NaviPac server address to connect to.
    /// For UDP: Not used (listens on all interfaces).
    /// </summary>
    public string NaviPacHost
    {
        get => _naviPacHost;
        set => SetProperty(ref _naviPacHost, value ?? "localhost");
    }
    
    /// <summary>
    /// NaviPac port number.
    /// For TCP: The port NaviPac is listening on.
    /// For UDP: The local port to listen on for incoming datagrams.
    /// </summary>
    public int NaviPacPort
    {
        get => _naviPacPort;
        set => SetProperty(ref _naviPacPort, value > 0 && value <= 65535 ? value : 4001);
    }
    
    /// <summary>
    /// Protocol to use for NaviPac connection.
    /// TCP: Client connects to NaviPac server (outbound connection).
    /// UDP: Listen for incoming NaviPac broadcasts/unicasts (inbound, requires firewall rule).
    /// </summary>
    public NaviPacProtocol NaviPacProtocol
    {
        get => _naviPacProtocol;
        set => SetProperty(ref _naviPacProtocol, value);
    }
    
    /// <summary>
    /// Enable automatic reconnection on disconnect.
    /// </summary>
    public bool AutoReconnect
    {
        get => _autoReconnect;
        set => SetProperty(ref _autoReconnect, value);
    }
    
    /// <summary>
    /// Interval between reconnection attempts (seconds).
    /// </summary>
    public int ReconnectIntervalSeconds
    {
        get => _reconnectIntervalSeconds;
        set => SetProperty(ref _reconnectIntervalSeconds, Math.Max(1, value));
    }
    
    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    public int ConnectionTimeoutMs
    {
        get => _connectionTimeoutMs;
        set => SetProperty(ref _connectionTimeoutMs, Math.Max(1000, Math.Min(60000, value)));
    }
    
    /// <summary>
    /// Automatically create firewall rule for UDP listening.
    /// Requires administrator privileges.
    /// </summary>
    public bool AutoCreateFirewallRule
    {
        get => _autoCreateFirewallRule;
        set => SetProperty(ref _autoCreateFirewallRule, value);
    }
    
    /// <summary>
    /// Separator character used in NaviPac User Defined Output.
    /// Common values: ',' (comma), ';' (semicolon), ':' (colon), ' ' (space), '\t' (tab)
    /// </summary>
    public char NaviPacSeparator
    {
        get => _naviPacSeparator;
        set => SetProperty(ref _naviPacSeparator, value);
    }
    
    /// <summary>
    /// Enable debug logging for NaviPac data reception.
    /// When enabled, raw data is logged for troubleshooting.
    /// </summary>
    public bool EnableDebugLogging
    {
        get => _enableDebugLogging;
        set => SetProperty(ref _enableDebugLogging, value);
    }
    
    /// <summary>
    /// Field mapping for NaviPac User Defined Output.
    /// Maps field indices to data types.
    /// </summary>
    public NaviPacFieldMapping FieldMapping { get; set; } = new();
    
    // ========================================================================
    // UDP Enhancements (v9.0.0 Phase 4)
    // ========================================================================
    
    /// <summary>
    /// Network interface IP to bind UDP listener to.
    /// Empty string = bind to all interfaces (IPAddress.Any).
    /// Specify an IP address to bind to a specific network interface.
    /// </summary>
    public string UdpBindInterface
    {
        get => _udpBindInterface;
        set => SetProperty(ref _udpBindInterface, value ?? string.Empty);
    }
    
    /// <summary>
    /// Enable filtering of incoming UDP packets by source IP.
    /// When enabled, only packets from IPs in AllowedSourceIps are processed.
    /// </summary>
    public bool EnableSourceIpFilter
    {
        get => _enableSourceIpFilter;
        set => SetProperty(ref _enableSourceIpFilter, value);
    }
    
    /// <summary>
    /// Comma-separated list of allowed source IP addresses.
    /// Only effective when EnableSourceIpFilter is true.
    /// Example: "192.168.1.100,192.168.1.101"
    /// </summary>
    public string AllowedSourceIps
    {
        get => _allowedSourceIps;
        set => SetProperty(ref _allowedSourceIps, value ?? string.Empty);
    }
    
    /// <summary>
    /// Enable multicast group listening for UDP.
    /// When enabled, joins the specified multicast group.
    /// </summary>
    public bool EnableMulticast
    {
        get => _enableMulticast;
        set => SetProperty(ref _enableMulticast, value);
    }
    
    /// <summary>
    /// Multicast group address to join (e.g., "239.1.1.1").
    /// Only effective when EnableMulticast is true.
    /// </summary>
    public string MulticastGroup
    {
        get => _multicastGroup;
        set => SetProperty(ref _multicastGroup, value ?? string.Empty);
    }
    
    /// <summary>
    /// Multicast TTL (Time To Live) for multicast packets.
    /// Controls how many network hops multicast packets can traverse.
    /// </summary>
    public int MulticastTtl
    {
        get => _multicastTtl;
        set => SetProperty(ref _multicastTtl, Math.Max(1, Math.Min(255, value)));
    }
    
    /// <summary>
    /// Enable UDP broadcast reception.
    /// Allows receiving broadcast packets on the network.
    /// </summary>
    public bool EnableUdpBroadcast
    {
        get => _enableUdpBroadcast;
        set => SetProperty(ref _enableUdpBroadcast, value);
    }
    
    /// <summary>
    /// Gets the list of allowed source IPs as an array.
    /// </summary>
    [JsonIgnore]
    public string[] AllowedSourceIpList =>
        string.IsNullOrWhiteSpace(AllowedSourceIps)
            ? Array.Empty<string>()
            : AllowedSourceIps.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // ========================================================================
    // Security Settings (v4.0 - VULN-005, MISSING-007)
    // ========================================================================

    /// <summary>
    /// TLS encryption configuration for TCP connections.
    /// When enabled, provides secure communication between NaviPac and this module.
    /// </summary>
    public TlsConfiguration? TlsConfiguration
    {
        get => _tlsConfiguration;
        set => SetProperty(ref _tlsConfiguration, value);
    }

    /// <summary>
    /// Rate limiting configuration for connection security.
    /// Limits connections per IP to prevent DoS attacks.
    /// </summary>
    public RateLimitConfiguration? RateLimitConfiguration
    {
        get => _rateLimitConfiguration;
        set => SetProperty(ref _rateLimitConfiguration, value);
    }

    // ========================================================================
    // NaviPac File Monitoring Settings
    // ========================================================================
    
    /// <summary>
    /// Enable monitoring of .npc position fix files.
    /// </summary>
    public bool EnableFixMonitoring
    {
        get => _enableFixMonitoring;
        set => SetProperty(ref _enableFixMonitoring, value);
    }
    
    /// <summary>
    /// Folder path for NaviPac position fix output files.
    /// </summary>
    public string FixOutputFolder
    {
        get => _fixOutputFolder;
        set => SetProperty(ref _fixOutputFolder, value ?? string.Empty);
    }
    
    /// <summary>
    /// Enable monitoring of .wp2 waypoint files.
    /// </summary>
    public bool EnableWaypointMonitoring
    {
        get => _enableWaypointMonitoring;
        set => SetProperty(ref _enableWaypointMonitoring, value);
    }
    
    /// <summary>
    /// Folder path for NaviPac waypoint files.
    /// </summary>
    public string WaypointFolder
    {
        get => _waypointFolder;
        set => SetProperty(ref _waypointFolder, value ?? string.Empty);
    }
    
    /// <summary>
    /// Monitor subdirectories for NaviPac files.
    /// </summary>
    public bool MonitorSubdirectories
    {
        get => _monitorSubdirectories;
        set => SetProperty(ref _monitorSubdirectories, value);
    }
    
    // ========================================================================
    // DVR/VisualWorks Monitoring Settings
    // ========================================================================
    
    /// <summary>
    /// Enable monitoring of VisualWorks DVR folders.
    /// </summary>
    public bool EnableDvrMonitoring
    {
        get => _enableDvrMonitoring;
        set => SetProperty(ref _enableDvrMonitoring, value);
    }
    
    /// <summary>
    /// VisualWorks project folder path.
    /// </summary>
    public string VisualWorksProjectFolder
    {
        get => _visualWorksProjectFolder;
        set => SetProperty(ref _visualWorksProjectFolder, value ?? string.Empty);
    }
    
    /// <summary>
    /// Parse folder hierarchy for project structure.
    /// </summary>
    public bool ParseFolderHierarchy
    {
        get => _parseFolderHierarchy;
        set => SetProperty(ref _parseFolderHierarchy, value);
    }
    
    /// <summary>
    /// Monitor subdirectories for DVR files.
    /// </summary>
    public bool MonitorDvrSubdirectories
    {
        get => _monitorDvrSubdirectories;
        set => SetProperty(ref _monitorDvrSubdirectories, value);
    }
    
    /// <summary>
    /// Vehicle folder mappings (folder pattern -> vehicle name).
    /// </summary>
    public List<VehicleFolderMapping> VehicleFolderMappings { get; set; } = new()
    {
        new VehicleFolderMapping { FolderPattern = "HD11", VehicleName = "HD11" },
        new VehicleFolderMapping { FolderPattern = "HD12", VehicleName = "HD12" },
        new VehicleFolderMapping { FolderPattern = "Ross*", VehicleName = "Ross Candies" }
    };
    
    // ========================================================================
    // Output Settings
    // ========================================================================
    
    /// <summary>
    /// Default folder for exports.
    /// </summary>
    public string DefaultExportFolder
    {
        get => _defaultExportFolder;
        set => SetProperty(ref _defaultExportFolder, value ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
    }
    
    /// <summary>
    /// Interval for auto-save (minutes).
    /// </summary>
    public int AutoSaveIntervalMinutes
    {
        get => _autoSaveIntervalMinutes;
        set => SetProperty(ref _autoSaveIntervalMinutes, Math.Max(1, value));
    }
    
    /// <summary>
    /// Enable automatic saving.
    /// </summary>
    public bool EnableAutoSave
    {
        get => _enableAutoSave;
        set => SetProperty(ref _enableAutoSave, value);
    }
    
    // ========================================================================
    // UI Settings
    // ========================================================================
    
    /// <summary>
    /// Use dark theme.
    /// </summary>
    public bool DarkTheme
    {
        get => _darkTheme;
        set => SetProperty(ref _darkTheme, value);
    }
    
    /// <summary>
    /// Main window width.
    /// </summary>
    public double WindowWidth
    {
        get => _windowWidth;
        set => SetProperty(ref _windowWidth, Math.Max(800, value));
    }
    
    /// <summary>
    /// Main window height.
    /// </summary>
    public double WindowHeight
    {
        get => _windowHeight;
        set => SetProperty(ref _windowHeight, Math.Max(600, value));
    }
    
    /// <summary>
    /// Project information (for auto-population).
    /// </summary>
    public ProjectInfo ProjectInfo { get; set; } = new();
    
    // ========================================================================
    // Video File Extensions
    // ========================================================================
    
    /// <summary>
    /// List of video file extensions to monitor.
    /// </summary>
    [JsonIgnore]
    public string[] VideoExtensions => new[] { ".wmv", ".mpg", ".mp4", ".mpeg", ".m2t", ".ts", ".avi" };
    
    // ========================================================================
    // Persistence Methods
    // ========================================================================
    
    /// <summary>
    /// Loads settings from file or creates default.
    /// </summary>
    public static ConnectionSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<ConnectionSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return settings ?? new ConnectionSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        
        return new ConnectionSettings();
    }
    
    /// <summary>
    /// Saves settings to file.
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Validates settings and returns any error messages.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        if (EnableFixMonitoring && !string.IsNullOrWhiteSpace(FixOutputFolder) && !Directory.Exists(FixOutputFolder))
        {
            errors.Add($"Fix output folder does not exist: {FixOutputFolder}");
        }
        
        if (EnableWaypointMonitoring && !string.IsNullOrWhiteSpace(WaypointFolder) && !Directory.Exists(WaypointFolder))
        {
            errors.Add($"Waypoint folder does not exist: {WaypointFolder}");
        }
        
        if (EnableDvrMonitoring && !string.IsNullOrWhiteSpace(VisualWorksProjectFolder) && !Directory.Exists(VisualWorksProjectFolder))
        {
            errors.Add($"VisualWorks project folder does not exist: {VisualWorksProjectFolder}");
        }
        
        return errors;
    }
    
    /// <summary>
    /// Creates a deep copy of these settings.
    /// </summary>
    public ConnectionSettings Clone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<ConnectionSettings>(json) ?? new ConnectionSettings();
    }
    
    // ========================================================================
    // INotifyPropertyChanged Implementation
    // ========================================================================
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// Mapping between folder pattern and vehicle name.
/// </summary>
public class VehicleFolderMapping
{
    public string FolderPattern { get; set; } = string.Empty;
    public string VehicleName { get; set; } = string.Empty;
    
    /// <summary>
    /// Checks if a folder name matches this pattern.
    /// </summary>
    public bool Matches(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName) || string.IsNullOrWhiteSpace(FolderPattern))
            return false;
        
        // Simple wildcard matching
        if (FolderPattern.EndsWith("*"))
        {
            var prefix = FolderPattern.TrimEnd('*');
            return folderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        
        return folderName.Equals(FolderPattern, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Field type in NaviPac User Defined Output.
/// </summary>
public enum NaviPacFieldType
{
    Unknown,
    DateTime,
    Date,
    Time,
    Event,
    Gyro,
    Roll,
    Pitch,
    Heave,
    Easting,
    Northing,
    Latitude,
    Longitude,
    LatitudeDMS,
    LongitudeDMS,
    Height,
    KP,
    DAL,
    DOL,
    DCC,
    DAQ,
    SMG,
    CMG,
    Age,
    FreeText
}

/// <summary>
/// Maps NaviPac User Defined Output field positions to data types.
/// Used to parse incoming data correctly regardless of field order.
/// </summary>
public class NaviPacFieldMapping
{
    /// <summary>
    /// List of fields in order they appear in the NaviPac output.
    /// Empty list means auto-detect (legacy behavior).
    /// </summary>
    public List<NaviPacFieldType> Fields { get; set; } = new();
    
    /// <summary>
    /// Whether to use auto-detection instead of explicit mapping.
    /// </summary>
    public bool AutoDetect { get; set; } = true;
    
    /// <summary>
    /// Gets the index of a specific field type, or -1 if not found.
    /// </summary>
    public int GetFieldIndex(NaviPacFieldType fieldType)
    {
        return Fields.IndexOf(fieldType);
    }
    
    /// <summary>
    /// Checks if a field type is present in the mapping.
    /// </summary>
    public bool HasField(NaviPacFieldType fieldType)
    {
        return Fields.Contains(fieldType);
    }
    
    /// <summary>
    /// Creates a default mapping based on typical NaviPac configuration.
    /// </summary>
    public static NaviPacFieldMapping CreateDefault()
    {
        return new NaviPacFieldMapping
        {
            AutoDetect = false,
            Fields = new List<NaviPacFieldType>
            {
                NaviPacFieldType.Event,
                NaviPacFieldType.Gyro,
                NaviPacFieldType.Roll,
                NaviPacFieldType.Pitch,
                NaviPacFieldType.Heave,
                NaviPacFieldType.Easting,
                NaviPacFieldType.Northing,
                NaviPacFieldType.Latitude,
                NaviPacFieldType.Longitude,
                NaviPacFieldType.LatitudeDMS,
                NaviPacFieldType.LongitudeDMS,
                NaviPacFieldType.Height,
                NaviPacFieldType.KP,
                NaviPacFieldType.DAL
            }
        };
    }
    
    /// <summary>
    /// Creates a mapping for position-only output.
    /// </summary>
    public static NaviPacFieldMapping CreatePositionOnly()
    {
        return new NaviPacFieldMapping
        {
            AutoDetect = false,
            Fields = new List<NaviPacFieldType>
            {
                NaviPacFieldType.DateTime,
                NaviPacFieldType.Easting,
                NaviPacFieldType.Northing,
                NaviPacFieldType.Height,
                NaviPacFieldType.KP,
                NaviPacFieldType.DCC
            }
        };
    }
}
