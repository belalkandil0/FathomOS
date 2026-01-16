namespace FathomOS.Modules.NetworkTimeSync.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.NetworkTimeSync.Enums;
using FathomOS.Modules.NetworkTimeSync.Infrastructure;
using FathomOS.Modules.NetworkTimeSync.Models;
using FathomOS.Modules.NetworkTimeSync.Services;

/// <summary>
/// Main ViewModel for the Network Time Sync dashboard.
/// </summary>
public class DashboardViewModel : ViewModelBase, IDisposable
{
    #region Private Fields

    private readonly TimeSyncService _syncService;
    private readonly NetworkDiscoveryService _discoveryService;
    private readonly GpsSerialService _gpsService;
    private readonly SyncHistoryService _historyService;
    private readonly ReportService _reportService;
    private StatusMonitorService? _monitorService;
    private ModuleConfiguration _config;
    private CancellationTokenSource? _operationCts;
    private System.Windows.Threading.DispatcherTimer? _uiRefreshTimer;

    private string _referenceTimeDisplay = "--:--:--";
    private string _targetTimeZoneDisplay = "UTC";
    private string _statusMessage = "Ready";
    private bool _isMonitoring;
    private bool _isBusy;
    private bool _isSettingsOpen;
    private bool _isHistoryOpen;
    private bool _isScheduleOpen;
    private NetworkComputer? _selectedComputer;
    private int _selectedCount;

    // Discovery fields
    private string _startIpAddress = "192.168.1.1";
    private string _endIpAddress = "192.168.1.254";
    private bool _isDiscovering;
    private int _discoveryProgress;
    private int _discoveryTotal;

    // Add computer dialog
    private bool _isAddComputerOpen;
    private string _newComputerIp = string.Empty;
    private string _newComputerHostname = string.Empty;

    // GPS status fields
    private bool _isGpsConnected;
    private string _gpsStatusText = "Not Connected";
    private int _gpsSatelliteCount;
    private string _gpsFixQuality = "No Fix";

    // Stats
    private int _syncedCount;
    private int _outOfSyncCount;
    private int _unreachableCount;
    
    // History
    private ObservableCollection<SyncHistoryEntry> _syncHistory = new();

    #endregion

    #region Constructor

    public DashboardViewModel()
    {
        Computers = new ObservableCollection<NetworkComputer>();
        
        // Load configuration
        _config = ConfigurationService.LoadConfiguration();
        
        // Initialize services
        _syncService = new TimeSyncService(_config.SyncConfig.AgentSecret, _config.SyncConfig.ConnectionTimeoutMs);
        _historyService = new SyncHistoryService();
        _reportService = new ReportService();
        _discoveryService = new NetworkDiscoveryService(
            _config.SyncConfig.DefaultAgentPort,
            _config.DiscoveryConfig.DiscoveryTimeoutMs,
            _config.DiscoveryConfig.ConcurrentScans,
            _config.SyncConfig.AgentSecret);
        
        // Initialize GPS service
        _gpsService = new GpsSerialService();
        _gpsService.TimeUpdated += OnGpsTimeUpdated;
        _gpsService.ConnectionChanged += OnGpsConnectionChanged;

        // Set suggested IP range (prefer detected network if config has defaults)
        var (suggestedStart, suggestedEnd) = _discoveryService.SuggestIpRange();
        
        // Use suggested range if config has default values (192.168.1.x)
        if (_config.DiscoveryConfig.StartIpAddress == "192.168.1.1" && 
            _config.DiscoveryConfig.EndIpAddress == "192.168.1.254" &&
            suggestedStart != "192.168.1.1")
        {
            // User hasn't changed defaults, use detected network
            _startIpAddress = suggestedStart;
            _endIpAddress = suggestedEnd;
            // Update config for saving
            _config.DiscoveryConfig.StartIpAddress = suggestedStart;
            _config.DiscoveryConfig.EndIpAddress = suggestedEnd;
        }
        else
        {
            // Use saved/configured values
            _startIpAddress = _config.DiscoveryConfig.StartIpAddress;
            _endIpAddress = _config.DiscoveryConfig.EndIpAddress;
        }

        // Load saved computers
        var savedComputers = ConfigurationService.LoadComputers(_config);
        foreach (var computer in savedComputers)
        {
            Computers.Add(computer);
        }

        // Get available COM ports
        AvailableComPorts = new ObservableCollection<string>(GpsSerialService.GetAvailablePorts());

        // Initialize commands
        InitializeCommands();

        // Update time display
        UpdateReferenceTime();

        // Start UI refresh timer - updates reference time and refreshes computer display properties
        _uiRefreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiRefreshTimer.Tick += (s, e) => 
        {
            UpdateReferenceTime();
            RefreshComputerDisplays();
        };
        _uiRefreshTimer.Start();
        
        // Auto-connect GPS if configured
        if (_config.SyncConfig.TimeSource == TimeSourceType.GpsSerial)
        {
            ConnectGps();
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Collection of network computers being monitored.
    /// </summary>
    public ObservableCollection<NetworkComputer> Computers { get; }

    /// <summary>
    /// Available COM ports for GPS.
    /// </summary>
    public ObservableCollection<string> AvailableComPorts { get; }

    /// <summary>
    /// Common baud rates for GPS serial connections.
    /// </summary>
    public int[] CommonBaudRates { get; } = { 4800, 9600, 19200, 38400, 57600, 115200 };

    /// <summary>
    /// Configuration settings.
    /// </summary>
    public SyncConfiguration SyncConfig => _config.SyncConfig;

    /// <summary>
    /// Discovery settings.
    /// </summary>
    public DiscoverySettings DiscoveryConfig => _config.DiscoveryConfig;

    public string ReferenceTimeDisplay
    {
        get => _referenceTimeDisplay;
        set => SetProperty(ref _referenceTimeDisplay, value);
    }

    public string TargetTimeZoneDisplay
    {
        get => _targetTimeZoneDisplay;
        set => SetProperty(ref _targetTimeZoneDisplay, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set => SetProperty(ref _isMonitoring, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    public bool IsHistoryOpen
    {
        get => _isHistoryOpen;
        set
        {
            if (SetProperty(ref _isHistoryOpen, value) && value)
            {
                LoadSyncHistory();
            }
        }
    }

    public bool IsScheduleOpen
    {
        get => _isScheduleOpen;
        set => SetProperty(ref _isScheduleOpen, value);
    }

    public ObservableCollection<SyncHistoryEntry> SyncHistory
    {
        get => _syncHistory;
        set => SetProperty(ref _syncHistory, value);
    }

    public NetworkComputer? SelectedComputer
    {
        get => _selectedComputer;
        set => SetProperty(ref _selectedComputer, value);
    }

    public int SelectedCount
    {
        get => _selectedCount;
        set => SetProperty(ref _selectedCount, value);
    }

    public string StartIpAddress
    {
        get => _startIpAddress;
        set => SetProperty(ref _startIpAddress, value);
    }

    public string EndIpAddress
    {
        get => _endIpAddress;
        set => SetProperty(ref _endIpAddress, value);
    }

    public bool IsDiscovering
    {
        get => _isDiscovering;
        set => SetProperty(ref _isDiscovering, value);
    }

    public int DiscoveryProgress
    {
        get => _discoveryProgress;
        set => SetProperty(ref _discoveryProgress, value);
    }

    public int DiscoveryTotal
    {
        get => _discoveryTotal;
        set => SetProperty(ref _discoveryTotal, value);
    }

    public bool IsAddComputerOpen
    {
        get => _isAddComputerOpen;
        set => SetProperty(ref _isAddComputerOpen, value);
    }

    public string NewComputerIp
    {
        get => _newComputerIp;
        set => SetProperty(ref _newComputerIp, value);
    }

    public string NewComputerHostname
    {
        get => _newComputerHostname;
        set => SetProperty(ref _newComputerHostname, value);
    }

    public bool IsGpsConnected
    {
        get => _isGpsConnected;
        set => SetProperty(ref _isGpsConnected, value);
    }

    public string GpsStatusText
    {
        get => _gpsStatusText;
        set => SetProperty(ref _gpsStatusText, value);
    }

    public int GpsSatelliteCount
    {
        get => _gpsSatelliteCount;
        set => SetProperty(ref _gpsSatelliteCount, value);
    }

    public string GpsFixQuality
    {
        get => _gpsFixQuality;
        set => SetProperty(ref _gpsFixQuality, value);
    }

    /// <summary>
    /// Display string for current time source.
    /// </summary>
    public string TimeSourceDisplay
    {
        get
        {
            if (IsGpsConnected)
                return "GPS Serial";
            
            return _config.SyncConfig.TimeSource switch
            {
                TimeSourceType.InternetNtp => "Internet NTP",
                TimeSourceType.LocalNtpServer => "Local NTP Server",
                TimeSourceType.HostComputer => "Host Computer",
                TimeSourceType.GpsSerial => IsGpsConnected ? "GPS Serial" : "GPS (Disconnected)",
                _ => "System Clock"
            };
        }
    }

    /// <summary>
    /// Whether Host Computer time source is selected.
    /// </summary>
    public bool UseHostTimeSource
    {
        get => _config.SyncConfig.TimeSource == TimeSourceType.HostComputer;
        set
        {
            if (value)
            {
                _config.SyncConfig.TimeSource = TimeSourceType.HostComputer;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseNtpTimeSource));
                OnPropertyChanged(nameof(UseGpsTimeSource));
                OnPropertyChanged(nameof(TimeSourceDisplay));
            }
        }
    }

    /// <summary>
    /// Whether NTP time source is selected.
    /// </summary>
    public bool UseNtpTimeSource
    {
        get => _config.SyncConfig.TimeSource == TimeSourceType.InternetNtp;
        set
        {
            if (value)
            {
                _config.SyncConfig.TimeSource = TimeSourceType.InternetNtp;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseHostTimeSource));
                OnPropertyChanged(nameof(UseGpsTimeSource));
                OnPropertyChanged(nameof(TimeSourceDisplay));
            }
        }
    }

    /// <summary>
    /// Whether GPS time source is selected.
    /// </summary>
    public bool UseGpsTimeSource
    {
        get => _config.SyncConfig.TimeSource == TimeSourceType.GpsSerial;
        set
        {
            if (value)
            {
                _config.SyncConfig.TimeSource = TimeSourceType.GpsSerial;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UseHostTimeSource));
                OnPropertyChanged(nameof(UseNtpTimeSource));
                OnPropertyChanged(nameof(TimeSourceDisplay));
            }
        }
    }

    public int SyncedCount
    {
        get => _syncedCount;
        set => SetProperty(ref _syncedCount, value);
    }

    public int OutOfSyncCount
    {
        get => _outOfSyncCount;
        set => SetProperty(ref _outOfSyncCount, value);
    }

    public int UnreachableCount
    {
        get => _unreachableCount;
        set => SetProperty(ref _unreachableCount, value);
    }

    public int TotalCount => Computers.Count;

    #endregion

    #region Commands

    public ICommand DiscoverCommand { get; private set; } = null!;
    public ICommand CancelDiscoverCommand { get; private set; } = null!;
    public ICommand RefreshStatusCommand { get; private set; } = null!;
    public ICommand SyncAllCommand { get; private set; } = null!;
    public ICommand SyncSelectedCommand { get; private set; } = null!;
    public ICommand ToggleMonitoringCommand { get; private set; } = null!;
    public ICommand OpenSettingsCommand { get; private set; } = null!;
    public ICommand CloseSettingsCommand { get; private set; } = null!;
    public ICommand SaveSettingsCommand { get; private set; } = null!;
    public ICommand OpenAddComputerCommand { get; private set; } = null!;
    public ICommand CloseAddComputerCommand { get; private set; } = null!;
    public ICommand AddComputerCommand { get; private set; } = null!;
    public ICommand AddThisComputerCommand { get; private set; } = null!;
    public ICommand RemoveSelectedCommand { get; private set; } = null!;
    public ICommand SelectAllCommand { get; private set; } = null!;
    public ICommand DeselectAllCommand { get; private set; } = null!;
    public ICommand ExportConfigCommand { get; private set; } = null!;
    public ICommand ImportConfigCommand { get; private set; } = null!;
    public ICommand ConnectGpsCommand { get; private set; } = null!;
    public ICommand DisconnectGpsCommand { get; private set; } = null!;
    public ICommand RefreshComPortsCommand { get; private set; } = null!;
    public ICommand SyncComputerCommand { get; private set; } = null!;
    public ICommand RemoveComputerCommand { get; private set; } = null!;
    
    // New commands for v2.0
    public ICommand OpenHistoryCommand { get; private set; } = null!;
    public ICommand CloseHistoryCommand { get; private set; } = null!;
    public ICommand ClearHistoryCommand { get; private set; } = null!;
    public ICommand ExportStatusCsvCommand { get; private set; } = null!;
    public ICommand ExportHistoryCsvCommand { get; private set; } = null!;
    public ICommand ExportReportHtmlCommand { get; private set; } = null!;
    public ICommand OpenScheduleCommand { get; private set; } = null!;
    public ICommand CloseScheduleCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        DiscoverCommand = new AsyncRelayCommand(DiscoverAsync, () => !IsDiscovering && !IsBusy);
        CancelDiscoverCommand = new RelayCommand(CancelDiscover, () => IsDiscovering);
        RefreshStatusCommand = new AsyncRelayCommand(RefreshStatusAsync, () => !IsBusy && Computers.Count > 0);
        SyncAllCommand = new AsyncRelayCommand(SyncAllAsync, () => !IsBusy && Computers.Count > 0);
        SyncSelectedCommand = new AsyncRelayCommand(SyncSelectedAsync, () => !IsBusy && SelectedCount > 0);
        ToggleMonitoringCommand = new RelayCommand(ToggleMonitoring);
        OpenSettingsCommand = new RelayCommand(() => IsSettingsOpen = true);
        CloseSettingsCommand = new RelayCommand(() => IsSettingsOpen = false);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        OpenAddComputerCommand = new RelayCommand(() => { NewComputerIp = ""; NewComputerHostname = ""; IsAddComputerOpen = true; });
        CloseAddComputerCommand = new RelayCommand(() => IsAddComputerOpen = false);
        AddComputerCommand = new RelayCommand(AddComputer, () => !string.IsNullOrWhiteSpace(NewComputerIp));
        AddThisComputerCommand = new AsyncRelayCommand(AddThisComputerAsync);
        RemoveSelectedCommand = new RelayCommand(RemoveSelected, () => SelectedCount > 0);
        SelectAllCommand = new RelayCommand(() => { foreach (var c in Computers) c.IsSelected = true; UpdateSelectedCount(); });
        DeselectAllCommand = new RelayCommand(() => { foreach (var c in Computers) c.IsSelected = false; UpdateSelectedCount(); });
        ExportConfigCommand = new RelayCommand(ExportConfig);
        ImportConfigCommand = new RelayCommand(ImportConfig);
        ConnectGpsCommand = new RelayCommand(ConnectGps, () => !IsGpsConnected);
        DisconnectGpsCommand = new RelayCommand(DisconnectGps, () => IsGpsConnected);
        RefreshComPortsCommand = new RelayCommand(RefreshComPorts);
        SyncComputerCommand = new RelayCommand<NetworkComputer>(SyncSingleComputer);
        RemoveComputerCommand = new RelayCommand<NetworkComputer>(RemoveSingleComputer);
        
        // New commands for v2.0
        OpenHistoryCommand = new RelayCommand(_ => IsHistoryOpen = true);
        CloseHistoryCommand = new RelayCommand(_ => IsHistoryOpen = false);
        ClearHistoryCommand = new RelayCommand(_ => ClearHistory());
        ExportStatusCsvCommand = new RelayCommand(_ => ExportStatusCsv());
        ExportHistoryCsvCommand = new RelayCommand(_ => ExportHistoryCsv());
        ExportReportHtmlCommand = new RelayCommand(_ => ExportReportHtml());
        OpenScheduleCommand = new RelayCommand(_ => IsScheduleOpen = true);
        CloseScheduleCommand = new RelayCommand(_ => IsScheduleOpen = false);
        
        // Subscribe to history service events
        _historyService.ScheduledSyncDue += OnScheduledSyncDue;
        _historyService.AlertTriggered += OnAlertTriggered;
    }

    #endregion

    #region Command Implementations

    private async Task DiscoverAsync()
    {
        IsDiscovering = true;
        IsBusy = true;
        StatusMessage = "Discovering computers on network...";
        
        _operationCts = new CancellationTokenSource();
        
        _discoveryService.ProgressChanged += OnDiscoveryProgress;
        _discoveryService.ComputerDiscovered += OnComputerDiscovered;

        try
        {
            var discovered = await _discoveryService.ScanRangeAsync(
                StartIpAddress, 
                EndIpAddress, 
                _operationCts.Token);

            StatusMessage = $"Discovery complete. Found {discovered.Count} computers with agents.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Discovery cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Discovery error: {ex.Message}";
        }
        finally
        {
            _discoveryService.ProgressChanged -= OnDiscoveryProgress;
            _discoveryService.ComputerDiscovered -= OnComputerDiscovered;
            IsDiscovering = false;
            IsBusy = false;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    private void CancelDiscover()
    {
        _operationCts?.Cancel();
    }

    private void OnDiscoveryProgress(object? sender, (int Current, int Total, string Status) e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            DiscoveryProgress = e.Current;
            DiscoveryTotal = e.Total;
            StatusMessage = e.Status;
        });
    }

    private void OnComputerDiscovered(object? sender, NetworkComputer computer)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Check if already in list
            if (!Computers.Any(c => c.IpAddress == computer.IpAddress))
            {
                Computers.Add(computer);
                OnPropertyChanged(nameof(TotalCount));
            }
        });
    }

    private async Task RefreshStatusAsync()
    {
        IsBusy = true;
        StatusMessage = "Checking status of all computers...";

        try
        {
            _operationCts = new CancellationTokenSource();
            
            var referenceTime = GetReferenceUtcTime();
            foreach (var computer in Computers)
            {
                if (_operationCts.Token.IsCancellationRequested)
                    break;

                await CheckComputerStatusAsync(computer, referenceTime);
            }

            UpdateStats();
            StatusMessage = $"Status refresh complete. {SyncedCount} synced, {OutOfSyncCount} out of sync, {UnreachableCount} unreachable.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing status: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    private async Task CheckComputerStatusAsync(NetworkComputer computer, DateTime referenceTime)
    {
        computer.Status = SyncStatus.Checking;

        try
        {
            var (timeInfo, error) = await _syncService.GetTimeAsync(
                computer.IpAddress,
                computer.Port);

            if (timeInfo == null)
            {
                computer.Status = SyncStatus.Unreachable;
                computer.CurrentTime = null;
            }
            else
            {
                var drift = (timeInfo.UtcTime - referenceTime).TotalSeconds;
                computer.CurrentTime = timeInfo.LocalTime;
                computer.TimeDriftSeconds = drift;
                computer.Status = Math.Abs(drift) <= _config.SyncConfig.ToleranceSeconds 
                    ? SyncStatus.Synced 
                    : SyncStatus.OutOfSync;
                computer.AgentInstalled = true;
            }
        }
        catch
        {
            computer.Status = SyncStatus.Unreachable;
        }

        computer.LastChecked = DateTime.Now;
    }

    private async Task SyncAllAsync()
    {
        IsBusy = true;
        StatusMessage = "Synchronizing all computers...";

        try
        {
            _operationCts = new CancellationTokenSource();
            var success = 0;
            var failed = 0;

            foreach (var computer in Computers)
            {
                if (_operationCts.Token.IsCancellationRequested)
                    break;

                var result = await SyncComputerAsync(computer);
                if (result) success++;
                else failed++;
            }

            UpdateStats();
            StatusMessage = $"Sync complete. {success} succeeded, {failed} failed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    private async Task SyncSelectedAsync()
    {
        IsBusy = true;
        var selected = Computers.Where(c => c.IsSelected).ToList();
        StatusMessage = $"Synchronizing {selected.Count} selected computers...";

        try
        {
            _operationCts = new CancellationTokenSource();
            var success = 0;
            var failed = 0;

            foreach (var computer in selected)
            {
                if (_operationCts.Token.IsCancellationRequested)
                    break;

                var result = await SyncComputerAsync(computer);
                if (result) success++;
                else failed++;
            }

            UpdateStats();
            StatusMessage = $"Sync complete. {success} succeeded, {failed} failed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _operationCts?.Dispose();
            _operationCts = null;
        }
    }

    private async void SyncSingleComputer(NetworkComputer? computer)
    {
        if (computer == null) return;

        try
        {
            StatusMessage = $"Synchronizing {computer.DisplayName}...";
            var result = await SyncComputerAsync(computer);
            UpdateStats();
            StatusMessage = result 
                ? $"Successfully synchronized {computer.DisplayName}" 
                : $"Failed to synchronize {computer.DisplayName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error syncing {computer.DisplayName}: {ex.Message}";
            computer.Status = SyncStatus.Error;
        }
    }

    private void RemoveSingleComputer(NetworkComputer? computer)
    {
        if (computer == null) return;

        Computers.Remove(computer);
        OnPropertyChanged(nameof(TotalCount));
        UpdateStats();
        SaveComputers();
        StatusMessage = $"Removed {computer.DisplayName}";
    }

    private async Task<bool> SyncComputerAsync(NetworkComputer computer)
    {
        computer.Status = SyncStatus.Syncing;
        var startTime = DateTime.Now;
        var driftBefore = computer.TimeDriftSeconds;

        try
        {
            bool success;
            string? error;

            var referenceTime = GetReferenceUtcTime();

            if (_config.SyncConfig.TimeSource == TimeSourceType.HostComputer || 
                _config.SyncConfig.TimeSource == TimeSourceType.GpsSerial)
            {
                // Set time directly from this computer or GPS
                (success, error) = await _syncService.SetTimeAsync(
                    computer.IpAddress,
                    computer.Port,
                    referenceTime);
            }
            else
            {
                // Use NTP server with fallback support
                var ntpServer = _config.SyncConfig.TimeSource == TimeSourceType.LocalNtpServer
                    ? _config.SyncConfig.LocalNtpServer
                    : _config.SyncConfig.PrimaryNtpServer;

                (success, error) = await _syncService.SyncNtpAsync(
                    computer.IpAddress,
                    computer.Port,
                    ntpServer);

                // Try fallback NTP servers if enabled and primary failed
                if (!success && _config.SyncConfig.EnableNtpFallback && 
                    _config.SyncConfig.TimeSource == TimeSourceType.InternetNtp)
                {
                    // Try secondary
                    (success, error) = await _syncService.SyncNtpAsync(
                        computer.IpAddress,
                        computer.Port,
                        _config.SyncConfig.SecondaryNtpServer);

                    if (!success && !string.IsNullOrEmpty(_config.SyncConfig.TertiaryNtpServer))
                    {
                        // Try tertiary
                        (success, error) = await _syncService.SyncNtpAsync(
                            computer.IpAddress,
                            computer.Port,
                            _config.SyncConfig.TertiaryNtpServer);
                    }
                }
            }

            var duration = DateTime.Now - startTime;

            if (success)
            {
                computer.Status = SyncStatus.Synced;
                computer.LastSynced = DateTime.Now;
                computer.TimeDriftSeconds = 0;

                // Verify
                await Task.Delay(500);
                await CheckComputerStatusAsync(computer, GetReferenceUtcTime());
                
                // Record success
                RecordSyncResult(computer, true, driftBefore, computer.TimeDriftSeconds, duration);
            }
            else
            {
                computer.Status = SyncStatus.Error;
                RecordSyncResult(computer, false, driftBefore, driftBefore, duration, error);
            }

            return success;
        }
        catch (Exception ex)
        {
            computer.Status = SyncStatus.Error;
            var duration = DateTime.Now - startTime;
            RecordSyncResult(computer, false, driftBefore, driftBefore, duration, ex.Message);
            return false;
        }
    }

    private void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            StopMonitoring();
        }
        else
        {
            StartMonitoring();
        }
    }

    private void StartMonitoring()
    {
        _monitorService = new StatusMonitorService(
            Computers, 
            _config.SyncConfig, 
            _syncService,
            GetReferenceUtcTime);  // Pass the GPS-aware time provider
        _monitorService.CycleCompleted += OnMonitorCycleCompleted;
        _monitorService.AutoCorrectionTriggered += OnAutoCorrection;
        _monitorService.Start();
        
        IsMonitoring = true;
        StatusMessage = "Continuous monitoring started.";
    }

    private void StopMonitoring()
    {
        _monitorService?.Stop();
        _monitorService?.Dispose();
        _monitorService = null;
        
        IsMonitoring = false;
        StatusMessage = "Monitoring stopped.";
    }

    private void OnMonitorCycleCompleted(object? sender, (int Checked, int Synced, int OutOfSync, int Unreachable) e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            SyncedCount = e.Synced;
            OutOfSyncCount = e.OutOfSync;
            UnreachableCount = e.Unreachable;
        });
    }

    private void OnAutoCorrection(object? sender, (NetworkComputer Computer, bool Success, string? Error) e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var status = e.Success ? "synced" : $"failed: {e.Error}";
            StatusMessage = $"Auto-corrected {e.Computer.DisplayName}: {status}";
        });
    }

    private void AddComputer()
    {
        if (string.IsNullOrWhiteSpace(NewComputerIp))
            return;

        // Check if already exists
        if (Computers.Any(c => c.IpAddress == NewComputerIp))
        {
            StatusMessage = $"Computer {NewComputerIp} is already in the list.";
            return;
        }

        var computer = new NetworkComputer
        {
            IpAddress = NewComputerIp.Trim(),
            Hostname = string.IsNullOrWhiteSpace(NewComputerHostname) ? NewComputerIp : NewComputerHostname.Trim(),
            Port = _config.SyncConfig.DefaultAgentPort,
            DiscoveryMethod = DiscoveryMethod.Manual,
            Status = SyncStatus.Unknown
        };

        Computers.Add(computer);
        OnPropertyChanged(nameof(TotalCount));
        
        IsAddComputerOpen = false;
        StatusMessage = $"Added computer {computer.DisplayName}";
        
        // Save configuration
        SaveComputers();
    }

    /// <summary>
    /// Add this computer (localhost) to the list and test connectivity.
    /// </summary>
    private async Task AddThisComputerAsync()
    {
        IsBusy = true;
        StatusMessage = "Testing local agent connection...";

        try
        {
            // Check if already exists
            if (Computers.Any(c => c.IpAddress == "127.0.0.1"))
            {
                StatusMessage = "Local computer (127.0.0.1) is already in the list.";
                return;
            }

            // Test connection to local agent
            var (pingSuccess, pingError) = await _syncService.PingAsync("127.0.0.1", _config.SyncConfig.DefaultAgentPort);

            if (!pingSuccess)
            {
                StatusMessage = $"Cannot connect to local agent: {pingError ?? "Connection failed"}. Is the service running?";
                
                // Show more helpful message
                MessageBox.Show(
                    $"Cannot connect to the local Time Sync Agent.\n\n" +
                    $"Error: {pingError ?? "Connection refused"}\n\n" +
                    $"Please verify:\n" +
                    $"1. The FathomOSTimeSyncAgent service is running\n" +
                    $"2. Port {_config.SyncConfig.DefaultAgentPort} is not blocked\n" +
                    $"3. The SharedSecret matches between module and agent\n\n" +
                    $"Run Test-Agent.bat in the agent folder for diagnostics.",
                    "Connection Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Get computer info
            var hostname = Environment.MachineName;
            var (info, _) = await _syncService.GetInfoAsync("127.0.0.1", _config.SyncConfig.DefaultAgentPort);
            if (info != null)
            {
                hostname = info.Hostname;
            }

            // Add to list
            var computer = new NetworkComputer
            {
                IpAddress = "127.0.0.1",
                Hostname = hostname + " (Local)",
                Port = _config.SyncConfig.DefaultAgentPort,
                DiscoveryMethod = DiscoveryMethod.Manual,
                AgentInstalled = true,
                Status = SyncStatus.Unknown
            };

            Computers.Add(computer);
            OnPropertyChanged(nameof(TotalCount));
            SaveComputers();

            StatusMessage = $"Added local computer: {hostname}";

            // Check status
            await CheckComputerStatusAsync(computer, GetReferenceUtcTime());
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error adding local computer: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RemoveSelected()
    {
        var selected = Computers.Where(c => c.IsSelected).ToList();
        foreach (var computer in selected)
        {
            Computers.Remove(computer);
        }
        
        OnPropertyChanged(nameof(TotalCount));
        UpdateStats();
        SaveComputers();
        
        StatusMessage = $"Removed {selected.Count} computers.";
    }

    private void SaveSettings()
    {
        try
        {
            // Update discovery config
            _config.DiscoveryConfig.StartIpAddress = StartIpAddress;
            _config.DiscoveryConfig.EndIpAddress = EndIpAddress;

            ConfigurationService.SaveConfiguration(_config);
            
            IsSettingsOpen = false;
            StatusMessage = "Settings saved.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
    }

    private void SaveComputers()
    {
        ConfigurationService.SaveComputers(Computers, _config);
        ConfigurationService.SaveConfiguration(_config);
    }

    private void ExportConfig()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Time Sync Config (*.nts)|*.nts|All Files (*.*)|*.*",
            DefaultExt = ".nts",
            FileName = "NetworkTimeSync_Config"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                ConfigurationService.SaveComputers(Computers, _config);
                ConfigurationService.ExportConfiguration(_config, dialog.FileName);
                StatusMessage = $"Configuration exported to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    private void ImportConfig()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Time Sync Config (*.nts)|*.nts|All Files (*.*)|*.*",
            DefaultExt = ".nts"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var imported = ConfigurationService.ImportConfiguration(dialog.FileName);
                
                // Merge computers
                var importedComputers = ConfigurationService.LoadComputers(imported);
                foreach (var computer in importedComputers)
                {
                    if (!Computers.Any(c => c.IpAddress == computer.IpAddress))
                    {
                        Computers.Add(computer);
                    }
                }

                OnPropertyChanged(nameof(TotalCount));
                SaveComputers();
                
                StatusMessage = $"Imported {importedComputers.Count} computers from configuration.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Import error: {ex.Message}";
            }
        }
    }

    #endregion

    #region Helper Methods

    private void UpdateReferenceTime()
    {
        DateTime displayTime;
        string sourceLabel;

        switch (_config.SyncConfig.TimeSource)
        {
            case TimeSourceType.GpsSerial:
                var gpsTime = _gpsService.GetCurrentTime();
                if (gpsTime != null && gpsTime.IsValid)
                {
                    displayTime = gpsTime.UtcTime;
                    sourceLabel = $"GPS UTC ({_gpsService.SatelliteCount} sats)";
                }
                else
                {
                    displayTime = DateTime.UtcNow;
                    sourceLabel = "GPS (No Fix) - Using System";
                }
                break;

            case TimeSourceType.HostComputer:
                // Host computer uses local system time
                displayTime = DateTime.Now;
                sourceLabel = "Host Computer (Local)";
                break;

            case TimeSourceType.LocalNtpServer:
                // Local NTP - display UTC
                displayTime = DateTime.UtcNow;
                sourceLabel = $"Local NTP: {_config.SyncConfig.LocalNtpServer}";
                break;

            case TimeSourceType.InternetNtp:
            default:
                // Internet NTP - display UTC
                displayTime = DateTime.UtcNow;
                sourceLabel = $"NTP: {_config.SyncConfig.PrimaryNtpServer}";
                break;
        }

        ReferenceTimeDisplay = displayTime.ToString("HH:mm:ss");
        TargetTimeZoneDisplay = sourceLabel;
    }

    /// <summary>
    /// Refresh display properties on all computers to update relative times ("5s ago" etc).
    /// </summary>
    private void RefreshComputerDisplays()
    {
        foreach (var computer in Computers)
        {
            // Trigger PropertyChanged for relative time displays
            computer.OnPropertyChanged("LastCheckedDisplay");
            computer.OnPropertyChanged("LastSyncedDisplay");
            computer.OnPropertyChanged("LastSyncDisplay");
            computer.OnPropertyChanged("CurrentTimeDisplay");
        }
    }

    /// <summary>
    /// Get reference time based on configured time source.
    /// </summary>
    private DateTime GetReferenceUtcTime()
    {
        if (_config.SyncConfig.TimeSource == TimeSourceType.GpsSerial)
        {
            var gpsTime = _gpsService.GetCurrentTime();
            if (gpsTime != null && gpsTime.IsValid)
            {
                return gpsTime.UtcTime;
            }
            // Fall back to system time if GPS not available
            StatusMessage = "GPS time not available, using system time";
        }
        
        return DateTime.UtcNow;
    }

    private void ConnectGps()
    {
        var config = new GpsSerialConfiguration
        {
            PortName = _config.SyncConfig.GpsPortName,
            BaudRate = _config.SyncConfig.GpsBaudRate
        };

        if (_gpsService.Connect(config))
        {
            StatusMessage = $"Connected to GPS on {config.PortName}";
        }
        else
        {
            StatusMessage = $"Failed to connect to GPS: {_gpsService.LastError}";
        }
    }

    private void DisconnectGps()
    {
        _gpsService.Disconnect();
        StatusMessage = "GPS disconnected";
    }

    private void RefreshComPorts()
    {
        AvailableComPorts.Clear();
        foreach (var port in GpsSerialService.GetAvailablePorts())
        {
            AvailableComPorts.Add(port);
        }
        StatusMessage = $"Found {AvailableComPorts.Count} COM ports";
    }

    private void OnGpsTimeUpdated(object? sender, GpsTimeEventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            GpsSatelliteCount = e.SatelliteCount;
            GpsFixQuality = e.FixQuality.ToString();
        });
    }

    private void OnGpsConnectionChanged(object? sender, bool connected)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            IsGpsConnected = connected;
            GpsStatusText = connected ? "Connected" : "Not Connected";
            
            if (!connected)
            {
                GpsSatelliteCount = 0;
                GpsFixQuality = "No Fix";
            }
        });
    }

    public void UpdateSelectedCount()
    {
        SelectedCount = Computers.Count(c => c.IsSelected);
    }

    private void UpdateStats()
    {
        SyncedCount = Computers.Count(c => c.Status == SyncStatus.Synced);
        OutOfSyncCount = Computers.Count(c => c.Status == SyncStatus.OutOfSync);
        UnreachableCount = Computers.Count(c => c.Status == SyncStatus.Unreachable || c.Status == SyncStatus.Error || c.Status == SyncStatus.Unknown);
    }

    public void LoadConfiguration(string filePath)
    {
        try
        {
            var imported = ConfigurationService.ImportConfiguration(filePath);
            _config = imported;
            
            Computers.Clear();
            var loadedComputers = ConfigurationService.LoadComputers(_config);
            foreach (var computer in loadedComputers)
            {
                Computers.Add(computer);
            }

            OnPropertyChanged(nameof(TotalCount));
            StatusMessage = $"Loaded configuration from {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading configuration: {ex.Message}";
        }
    }

    #endregion

    #region History and Export

    private void LoadSyncHistory()
    {
        var history = _historyService.GetHistory(100);
        SyncHistory = new ObservableCollection<SyncHistoryEntry>(history);
    }

    private void ClearHistory()
    {
        var result = MessageBox.Show(
            "Are you sure you want to clear all sync history?",
            "Clear History",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _historyService.ClearHistory();
            SyncHistory.Clear();
            StatusMessage = "Sync history cleared.";
        }
    }

    private void ExportStatusCsv()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Current Status",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"TimeSync_Status_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _reportService.ExportStatusToCsv(dialog.FileName, Computers);
                StatusMessage = $"Status exported to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    private void ExportHistoryCsv()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Sync History",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"TimeSync_History_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var history = _historyService.GetHistory(1000);
                _reportService.ExportHistoryToCsv(dialog.FileName, history);
                StatusMessage = $"History exported to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    private void ExportReportHtml()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export HTML Report",
            Filter = "HTML Files (*.html)|*.html",
            FileName = $"TimeSync_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var history = _historyService.GetHistory(50);
                _reportService.ExportStatusToHtml(dialog.FileName, Computers, history);
                StatusMessage = $"Report exported to {dialog.FileName}";
                
                // Optionally open in browser
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    private void RecordSyncResult(NetworkComputer computer, bool success, double driftBefore, 
        double driftAfter, TimeSpan duration, string? error = null)
    {
        var entry = new SyncHistoryEntry
        {
            Timestamp = DateTime.Now,
            ComputerIp = computer.IpAddress,
            ComputerName = computer.DisplayName,
            Success = success,
            DriftBeforeSeconds = driftBefore,
            DriftAfterSeconds = driftAfter,
            TimeSource = _config.SyncConfig.TimeSource.ToString(),
            Duration = duration,
            ErrorMessage = error
        };

        _historyService.AddEntry(entry);

        // Also record drift measurement
        _historyService.RecordDrift(computer.IpAddress, driftAfter);
    }

    private void OnScheduledSyncDue(object? sender, SyncSchedule schedule)
    {
        Application.Current.Dispatcher.Invoke(async () =>
        {
            StatusMessage = "Scheduled sync starting...";
            await SyncAllAsync();
            StatusMessage = $"Scheduled sync complete. Next run: {schedule.NextRun:HH:mm:ss}";
        });
    }

    private void OnAlertTriggered(object? sender, (string ComputerIp, double Drift, AlertLevel Level) alert)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var computer = Computers.FirstOrDefault(c => c.IpAddress == alert.ComputerIp);
            var name = computer?.DisplayName ?? alert.ComputerIp;
            var level = alert.Level == AlertLevel.Critical ? "CRITICAL" : "Warning";
            StatusMessage = $"{level}: {name} drift is {alert.Drift:F2}s";
        });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _uiRefreshTimer?.Stop();
        StopMonitoring();
        _gpsService.Dispose();
        _historyService.Dispose();
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        SaveComputers();
    }

    #endregion
}
