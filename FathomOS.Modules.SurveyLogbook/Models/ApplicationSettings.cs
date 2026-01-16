using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FathomOS.Modules.SurveyLogbook.Services;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Application-wide settings for the Survey Electronic Logbook module.
/// Handles persistence to JSON file in user's AppData folder.
/// </summary>
public class ApplicationSettings
{
    #region NaviPac TCP/UDP Connection Settings

    /// <summary>
    /// Enable/disable NaviPac TCP/UDP connection.
    /// </summary>
    public bool EnableNaviPacConnection { get; set; } = false;

    /// <summary>
    /// NaviPac server IP address (for TCP) or local interface (for UDP).
    /// </summary>
    public string NaviPacIpAddress { get; set; } = "127.0.0.1";

    /// <summary>
    /// NaviPac server port number (TCP) or listening port (UDP).
    /// </summary>
    public int NaviPacPort { get; set; } = 4001;

    /// <summary>
    /// Protocol to use: TCP (outbound connection) or UDP (inbound listener).
    /// TCP: Client connects to NaviPac server.
    /// UDP: Listen for incoming NaviPac broadcasts/unicasts.
    /// </summary>
    public NaviPacProtocol NaviPacProtocol { get; set; } = NaviPacProtocol.TCP;

    /// <summary>
    /// Reconnection interval in seconds when connection is lost.
    /// </summary>
    public int ReconnectIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Automatically create firewall rule for UDP listening.
    /// Requires administrator privileges.
    /// </summary>
    public bool AutoCreateFirewallRule { get; set; } = false;

    /// <summary>
    /// Enable debug logging for NaviPac data (logs raw data for troubleshooting).
    /// </summary>
    public bool EnableDebugLogging { get; set; } = true;

    /// <summary>
    /// Data field separator used in NaviPac User Defined Output.
    /// </summary>
    public string NaviPacSeparator { get; set; } = ",";

    /// <summary>
    /// Use auto-detection for field parsing.
    /// </summary>
    public bool AutoDetectFields { get; set; } = true;

    /// <summary>
    /// Capture Event Number field.
    /// </summary>
    public bool CaptureEvent { get; set; } = true;

    /// <summary>
    /// Capture Date/Time field.
    /// </summary>
    public bool CaptureDateTime { get; set; } = true;

    /// <summary>
    /// Capture Easting field.
    /// </summary>
    public bool CaptureEasting { get; set; } = true;

    /// <summary>
    /// Capture Northing field.
    /// </summary>
    public bool CaptureNorthing { get; set; } = true;

    /// <summary>
    /// Capture Height/Depth field.
    /// </summary>
    public bool CaptureHeight { get; set; } = true;

    /// <summary>
    /// Capture KP (Kilometre Post) field.
    /// </summary>
    public bool CaptureKP { get; set; } = true;

    /// <summary>
    /// Capture DCC (Distance Cross Course) field.
    /// </summary>
    public bool CaptureDCC { get; set; } = true;

    /// <summary>
    /// Capture Latitude field.
    /// </summary>
    public bool CaptureLatitude { get; set; } = true;

    /// <summary>
    /// Capture Longitude field.
    /// </summary>
    public bool CaptureLongitude { get; set; } = true;

    /// <summary>
    /// Capture Gyro/Heading field.
    /// </summary>
    public bool CaptureGyro { get; set; } = true;

    /// <summary>
    /// Capture Roll field.
    /// </summary>
    public bool CaptureRoll { get; set; } = true;

    /// <summary>
    /// Capture Pitch field.
    /// </summary>
    public bool CapturePitch { get; set; } = true;

    /// <summary>
    /// Capture Heave field.
    /// </summary>
    public bool CaptureHeave { get; set; } = true;

    /// <summary>
    /// Capture SMG (Speed Made Good) field.
    /// </summary>
    public bool CaptureSMG { get; set; } = true;

    /// <summary>
    /// Capture CMG (Course Made Good) field.
    /// </summary>
    public bool CaptureCMG { get; set; } = true;

    /// <summary>
    /// User-defined field configuration for NaviPac UDO parsing.
    /// When set, overrides individual Capture* settings above.
    /// </summary>
    public List<UserFieldDefinition>? NaviPacFields { get; set; }

    /// <summary>
    /// Field separator for dynamic field configuration.
    /// Maps to NaviPacSeparator for backward compatibility.
    /// </summary>
    public string NaviPacFieldSeparator
    {
        get => NaviPacSeparator;
        set => NaviPacSeparator = value;
    }

    #endregion

    #region File Monitoring Settings

    /// <summary>
    /// Enable/disable file system monitoring.
    /// </summary>
    public bool EnableFileMonitoring { get; set; } = true;

    /// <summary>
    /// Directory path for SLOG files.
    /// </summary>
    public string SlogFilesPath { get; set; } = @"C:\EIVA\NaviPac\Data";

    /// <summary>
    /// Directory path for NPC calibration files.
    /// </summary>
    public string NpcFilesPath { get; set; } = @"C:\EIVA\NaviPac\Calibration";

    /// <summary>
    /// Directory path for DVR recordings.
    /// </summary>
    public string DvrFolderPath { get; set; } = @"C:\DVR\Recordings";

    /// <summary>
    /// Directory path for waypoint files.
    /// </summary>
    public string WaypointFilesPath { get; set; } = @"C:\EIVA\NaviPac\Waypoints";

    #endregion

    #region Auto-Save Settings

    /// <summary>
    /// Enable/disable automatic saving.
    /// </summary>
    public bool EnableAutoSave { get; set; } = true;

    /// <summary>
    /// Auto-save interval in minutes.
    /// </summary>
    public int AutoSaveIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Default directory for saving log files.
    /// </summary>
    public string DefaultSaveDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    #endregion

    #region Project Defaults

    /// <summary>
    /// Default client name for new projects.
    /// </summary>
    public string DefaultClient { get; set; } = "";

    /// <summary>
    /// Default vessel name for new projects.
    /// </summary>
    public string DefaultVessel { get; set; } = "";

    /// <summary>
    /// Default project name/number for new projects.
    /// </summary>
    public string DefaultProject { get; set; } = "";

    #endregion

    #region UI Settings

    /// <summary>
    /// Selected theme (Dark/Light).
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// Last selected tab index.
    /// </summary>
    public int LastSelectedTabIndex { get; set; } = 0;

    /// <summary>
    /// Window width on last close.
    /// </summary>
    public double WindowWidth { get; set; } = 1400;

    /// <summary>
    /// Window height on last close.
    /// </summary>
    public double WindowHeight { get; set; } = 900;

    #endregion

    #region Persistence

    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FathomOS", "SurveyLogbook");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");

    /// <summary>
    /// Loads settings from the JSON file, or returns defaults if file doesn't exist.
    /// </summary>
    public static ApplicationSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(json);
                return settings ?? new ApplicationSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }

        return new ApplicationSettings();
    }

    /// <summary>
    /// Saves current settings to the JSON file.
    /// </summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a deep copy of the current settings.
    /// </summary>
    public ApplicationSettings Clone()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<ApplicationSettings>(json) ?? new ApplicationSettings();
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public void ResetToDefaults()
    {
        var defaults = new ApplicationSettings();

        EnableNaviPacConnection = defaults.EnableNaviPacConnection;
        NaviPacIpAddress = defaults.NaviPacIpAddress;
        NaviPacPort = defaults.NaviPacPort;
        NaviPacProtocol = defaults.NaviPacProtocol;
        ReconnectIntervalSeconds = defaults.ReconnectIntervalSeconds;
        ConnectionTimeoutSeconds = defaults.ConnectionTimeoutSeconds;
        AutoCreateFirewallRule = defaults.AutoCreateFirewallRule;
        EnableDebugLogging = defaults.EnableDebugLogging;
        NaviPacSeparator = defaults.NaviPacSeparator;
        AutoDetectFields = defaults.AutoDetectFields;
        CaptureEvent = defaults.CaptureEvent;
        CaptureDateTime = defaults.CaptureDateTime;
        CaptureEasting = defaults.CaptureEasting;
        CaptureNorthing = defaults.CaptureNorthing;
        CaptureHeight = defaults.CaptureHeight;
        CaptureKP = defaults.CaptureKP;
        CaptureDCC = defaults.CaptureDCC;
        CaptureLatitude = defaults.CaptureLatitude;
        CaptureLongitude = defaults.CaptureLongitude;
        CaptureGyro = defaults.CaptureGyro;
        CaptureRoll = defaults.CaptureRoll;
        CapturePitch = defaults.CapturePitch;
        CaptureHeave = defaults.CaptureHeave;
        CaptureSMG = defaults.CaptureSMG;
        CaptureCMG = defaults.CaptureCMG;
        NaviPacFields = null; // Reset to auto-detect mode

        EnableFileMonitoring = defaults.EnableFileMonitoring;
        SlogFilesPath = defaults.SlogFilesPath;
        NpcFilesPath = defaults.NpcFilesPath;
        DvrFolderPath = defaults.DvrFolderPath;
        WaypointFilesPath = defaults.WaypointFilesPath;

        EnableAutoSave = defaults.EnableAutoSave;
        AutoSaveIntervalMinutes = defaults.AutoSaveIntervalMinutes;
        DefaultSaveDirectory = defaults.DefaultSaveDirectory;

        DefaultClient = defaults.DefaultClient;
        DefaultVessel = defaults.DefaultVessel;
        DefaultProject = defaults.DefaultProject;

        Theme = defaults.Theme;
    }

    #endregion
}
