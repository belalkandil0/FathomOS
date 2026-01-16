namespace FathomOS.Modules.NetworkTimeSync.Models;

using System;
using System.Collections.Generic;
using FathomOS.Modules.NetworkTimeSync.Enums;

/// <summary>
/// Configuration settings for time synchronization.
/// </summary>
public class SyncConfiguration
{
    /// <summary>
    /// Time source type for synchronization.
    /// </summary>
    public TimeSourceType TimeSource { get; set; } = TimeSourceType.InternetNtp;

    /// <summary>
    /// Primary NTP server address (for InternetNtp or LocalNtpServer).
    /// </summary>
    public string PrimaryNtpServer { get; set; } = "time.windows.com";

    /// <summary>
    /// Secondary NTP server address (fallback).
    /// </summary>
    public string SecondaryNtpServer { get; set; } = "pool.ntp.org";

    /// <summary>
    /// Tertiary NTP server (second fallback).
    /// </summary>
    public string TertiaryNtpServer { get; set; } = "time.nist.gov";

    /// <summary>
    /// Enable multi-NTP fallback (try secondary/tertiary if primary fails).
    /// </summary>
    public bool EnableNtpFallback { get; set; } = true;

    /// <summary>
    /// Local NTP server address (for LocalNtpServer mode).
    /// </summary>
    public string LocalNtpServer { get; set; } = string.Empty;

    /// <summary>
    /// GPS Serial port name (e.g., COM1, COM3).
    /// </summary>
    public string GpsPortName { get; set; } = "COM1";

    /// <summary>
    /// GPS Serial baud rate (typically 4800, 9600, or 115200).
    /// </summary>
    public int GpsBaudRate { get; set; } = 4800;

    /// <summary>
    /// Synchronization mode (one-time or continuous).
    /// </summary>
    public SyncMode SyncMode { get; set; } = SyncMode.Continuous;

    /// <summary>
    /// Tolerance in seconds for considering a computer "in sync".
    /// </summary>
    public double ToleranceSeconds { get; set; } = 1.0;

    /// <summary>
    /// Warning threshold in seconds (shows yellow indicator).
    /// </summary>
    public double WarningThresholdSeconds { get; set; } = 0.5;

    /// <summary>
    /// Critical threshold in seconds (shows red indicator).
    /// </summary>
    public double CriticalThresholdSeconds { get; set; } = 1.0;

    /// <summary>
    /// Auto-correct threshold in seconds (for continuous mode).
    /// Computers with drift exceeding this will be auto-synced.
    /// </summary>
    public double AutoCorrectThresholdSeconds { get; set; } = 2.0;

    /// <summary>
    /// Interval in seconds between status checks (for continuous mode).
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Default port for agent communication.
    /// </summary>
    public int DefaultAgentPort { get; set; } = 7700;

    /// <summary>
    /// Connection timeout in milliseconds.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Shared secret for agent authentication.
    /// </summary>
    public string AgentSecret { get; set; } = "FathomOSTimeSync2024";

    /// <summary>
    /// Target timezone for synchronization (e.g., "UTC", "Pacific Standard Time").
    /// </summary>
    public string TargetTimeZone { get; set; } = "UTC";

    /// <summary>
    /// Enable auto-sync on startup.
    /// </summary>
    public bool AutoSyncOnStartup { get; set; } = false;

    /// <summary>
    /// Play sound on sync errors.
    /// </summary>
    public bool PlaySoundOnError { get; set; } = true;

    /// <summary>
    /// Play sound when drift exceeds warning threshold.
    /// </summary>
    public bool PlaySoundOnWarning { get; set; } = false;

    /// <summary>
    /// Play sound when drift exceeds critical threshold.
    /// </summary>
    public bool PlaySoundOnCritical { get; set; } = true;

    /// <summary>
    /// Enable drift prediction alerts.
    /// </summary>
    public bool EnableDriftPrediction { get; set; } = true;
}

/// <summary>
/// Settings for network discovery.
/// </summary>
public class DiscoverySettings
{
    /// <summary>
    /// Start IP address for range scan.
    /// </summary>
    public string StartIpAddress { get; set; } = "192.168.1.1";

    /// <summary>
    /// End IP address for range scan.
    /// </summary>
    public string EndIpAddress { get; set; } = "192.168.1.254";

    /// <summary>
    /// Port to scan for agents.
    /// </summary>
    public int ScanPort { get; set; } = 7700;

    /// <summary>
    /// Timeout for ping/discovery in milliseconds.
    /// </summary>
    public int DiscoveryTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// Number of concurrent scans.
    /// </summary>
    public int ConcurrentScans { get; set; } = 20;
}

/// <summary>
/// Complete module configuration for save/load.
/// </summary>
public class ModuleConfiguration
{
    /// <summary>
    /// Configuration version for compatibility.
    /// </summary>
    public string Version { get; set; } = "1.1";

    /// <summary>
    /// Last saved timestamp.
    /// </summary>
    public DateTime LastSaved { get; set; } = DateTime.Now;

    /// <summary>
    /// Sync configuration settings.
    /// </summary>
    public SyncConfiguration SyncConfig { get; set; } = new();

    /// <summary>
    /// Discovery settings.
    /// </summary>
    public DiscoverySettings DiscoveryConfig { get; set; } = new();

    /// <summary>
    /// Saved list of managed computers.
    /// </summary>
    public List<SavedComputer> Computers { get; set; } = new();

    /// <summary>
    /// Computer groups for organization.
    /// </summary>
    public List<ComputerGroup> Groups { get; set; } = new();

    /// <summary>
    /// Sync schedule configuration.
    /// </summary>
    public SyncSchedule Schedule { get; set; } = new();

    /// <summary>
    /// Per-computer alert thresholds (IP -> threshold).
    /// </summary>
    public List<AlertThreshold> AlertThresholds { get; set; } = new();
}

/// <summary>
/// Simplified computer data for serialization.
/// </summary>
public class SavedComputer
{
    public string IpAddress { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public int Port { get; set; } = 7700;
    public string DiscoveryMethod { get; set; } = "Manual";
    public string Notes { get; set; } = string.Empty;
    public string? GroupId { get; set; }
}
