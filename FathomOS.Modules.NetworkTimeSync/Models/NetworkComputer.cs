namespace FathomOS.Modules.NetworkTimeSync.Models;

using System;
using FathomOS.Modules.NetworkTimeSync.Enums;

/// <summary>
/// Represents a computer on the network being monitored for time sync.
/// </summary>
public class NetworkComputer : Infrastructure.ViewModelBase
{
    private string _ipAddress = string.Empty;
    private string _hostname = string.Empty;
    private SyncStatus _status = SyncStatus.Unknown;
    private DateTime? _currentTime;
    private DateTime? _lastChecked;
    private DateTime? _lastSynced;
    private double _timeDriftSeconds;
    private bool _isSelected;
    private bool _agentInstalled;
    private string _osVersion = string.Empty;
    private string _agentVersion = string.Empty;
    private DiscoveryMethod _discoveryMethod;
    private string _notes = string.Empty;
    private int _port = 7700;

    /// <summary>
    /// IP address of the computer.
    /// </summary>
    public string IpAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    /// <summary>
    /// Hostname of the computer.
    /// </summary>
    public string Hostname
    {
        get => _hostname;
        set => SetProperty(ref _hostname, value);
    }

    /// <summary>
    /// Current sync status.
    /// </summary>
    public SyncStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(StatusColorMedia));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(IsOutOfSync));
            }
        }
    }

    /// <summary>
    /// Current time reported by the computer.
    /// </summary>
    public DateTime? CurrentTime
    {
        get => _currentTime;
        set
        {
            if (SetProperty(ref _currentTime, value))
            {
                OnPropertyChanged(nameof(CurrentTimeDisplay));
            }
        }
    }

    /// <summary>
    /// Last time the status was checked.
    /// </summary>
    public DateTime? LastChecked
    {
        get => _lastChecked;
        set
        {
            if (SetProperty(ref _lastChecked, value))
            {
                OnPropertyChanged(nameof(LastCheckedDisplay));
            }
        }
    }

    /// <summary>
    /// Last time the computer was synchronized.
    /// </summary>
    public DateTime? LastSynced
    {
        get => _lastSynced;
        set
        {
            if (SetProperty(ref _lastSynced, value))
            {
                OnPropertyChanged(nameof(LastSyncedDisplay));
            }
        }
    }

    /// <summary>
    /// Time drift in seconds (positive = ahead, negative = behind).
    /// </summary>
    public double TimeDriftSeconds
    {
        get => _timeDriftSeconds;
        set
        {
            if (SetProperty(ref _timeDriftSeconds, value))
            {
                OnPropertyChanged(nameof(TimeDriftDisplay));
            }
        }
    }

    /// <summary>
    /// Whether this computer is selected in the UI.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    /// Whether the sync agent is installed on this computer.
    /// </summary>
    public bool AgentInstalled
    {
        get => _agentInstalled;
        set => SetProperty(ref _agentInstalled, value);
    }

    /// <summary>
    /// Operating system version.
    /// </summary>
    public string OsVersion
    {
        get => _osVersion;
        set => SetProperty(ref _osVersion, value);
    }

    /// <summary>
    /// Version of the installed agent.
    /// </summary>
    public string AgentVersion
    {
        get => _agentVersion;
        set => SetProperty(ref _agentVersion, value);
    }

    /// <summary>
    /// How this computer was discovered.
    /// </summary>
    public DiscoveryMethod DiscoveryMethod
    {
        get => _discoveryMethod;
        set => SetProperty(ref _discoveryMethod, value);
    }

    /// <summary>
    /// User notes for this computer.
    /// </summary>
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    /// <summary>
    /// Port number for agent communication.
    /// </summary>
    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    #region Display Properties

    /// <summary>
    /// Display name (hostname or IP if hostname unavailable).
    /// </summary>
    public string DisplayName => !string.IsNullOrEmpty(Hostname) ? Hostname : IpAddress;

    /// <summary>
    /// Status color for UI indicators (string hex format).
    /// </summary>
    public string StatusColor => Status switch
    {
        SyncStatus.Synced => "#6CCB5F",           // Green
        SyncStatus.OutOfSync => "#FF6B6B",        // Red
        SyncStatus.Unreachable => "#606060",      // Gray
        SyncStatus.Checking => "#60CDFF",         // Blue
        SyncStatus.Syncing => "#FCE100",          // Yellow
        SyncStatus.Error => "#FF6B6B",            // Red
        SyncStatus.AgentNotInstalled => "#FFA500", // Orange
        _ => "#A0A0A0"                             // Gray
    };

    /// <summary>
    /// Status color as Media.Color for effects binding.
    /// </summary>
    public System.Windows.Media.Color StatusColorMedia
    {
        get
        {
            try
            {
                return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(StatusColor);
            }
            catch
            {
                return System.Windows.Media.Colors.Gray;
            }
        }
    }

    /// <summary>
    /// Status text for display.
    /// </summary>
    public string StatusText => Status switch
    {
        SyncStatus.Synced => "Synced",
        SyncStatus.OutOfSync => "Out of Sync",
        SyncStatus.Unreachable => "Unreachable",
        SyncStatus.Checking => "Checking...",
        SyncStatus.Syncing => "Syncing...",
        SyncStatus.Error => "Error",
        SyncStatus.AgentNotInstalled => "No Agent",
        _ => "Unknown"
    };

    /// <summary>
    /// Current time display string.
    /// </summary>
    public string CurrentTimeDisplay => CurrentTime?.ToString("HH:mm:ss") ?? "--:--:--";

    /// <summary>
    /// Time drift display string.
    /// </summary>
    public string TimeDriftDisplay
    {
        get
        {
            // Only show "--" for states where we genuinely don't have data
            if (Status == SyncStatus.Unreachable || Status == SyncStatus.AgentNotInstalled)
                return "--";
            
            // For Synced, show the actual drift (should be near 0)
            // For OutOfSync, show the drift
            // For Unknown/Checking/Syncing, show current drift if we have data
            var sign = TimeDriftSeconds >= 0 ? "+" : "";
            return $"{sign}{TimeDriftSeconds:F1}s";
        }
    }

    /// <summary>
    /// Last checked display (relative time).
    /// </summary>
    public string LastCheckedDisplay
    {
        get
        {
            if (!LastChecked.HasValue) return "Never";
            var elapsed = DateTime.Now - LastChecked.Value;
            if (elapsed.TotalSeconds < 60) return $"{(int)elapsed.TotalSeconds}s ago";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            return $"{(int)elapsed.TotalHours}h ago";
        }
    }

    /// <summary>
    /// Last synced display (relative time).
    /// </summary>
    public string LastSyncedDisplay
    {
        get
        {
            if (!LastSynced.HasValue) return "Never";
            var elapsed = DateTime.Now - LastSynced.Value;
            if (elapsed.TotalSeconds < 60) return $"{(int)elapsed.TotalSeconds}s ago";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
            return LastSynced.Value.ToString("yyyy-MM-dd HH:mm");
        }
    }

    /// <summary>
    /// Alias for LastSyncedDisplay for XAML binding compatibility.
    /// </summary>
    public string LastSyncDisplay => LastSyncedDisplay;

    /// <summary>
    /// Alias for TimeDriftDisplay for XAML binding compatibility.
    /// </summary>
    public string TimeOffsetDisplay => TimeDriftDisplay;

    /// <summary>
    /// Whether the computer is out of sync (for UI styling).
    /// </summary>
    public bool IsOutOfSync => Status == SyncStatus.OutOfSync || Status == SyncStatus.Error;

    #endregion

    /// <summary>
    /// Creates a copy of this computer for serialization.
    /// </summary>
    public NetworkComputer Clone()
    {
        return new NetworkComputer
        {
            IpAddress = this.IpAddress,
            Hostname = this.Hostname,
            Port = this.Port,
            DiscoveryMethod = this.DiscoveryMethod,
            Notes = this.Notes
        };
    }
}
