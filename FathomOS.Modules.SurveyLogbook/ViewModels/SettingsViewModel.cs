using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Input;
using FathomOS.Modules.SurveyLogbook.Models;
using FathomOS.Modules.SurveyLogbook.Services;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// Handles all configuration settings including NaviPac connection, file monitoring, and auto-save.
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    #region Private Fields

    private ApplicationSettings _originalSettings;
    private bool _enableNaviPacConnection;
    private string _naviPacIpAddress = "127.0.0.1";
    private int _naviPacPort = 4001;
    private NaviPacProtocol _naviPacProtocol = NaviPacProtocol.TCP;
    private int _reconnectIntervalSeconds = 30;
    private int _connectionTimeoutSeconds = 10;
    private bool _autoCreateFirewallRule;
    private bool _enableFileMonitoring = true;
    private string _slogFilesPath = "";
    private string _npcFilesPath = "";
    private string _dvrFolderPath = "";
    private bool _enableAutoSave = true;
    private int _autoSaveIntervalMinutes = 5;
    private string _defaultSaveDirectory = "";
    private string _defaultClient = "";
    private string _defaultVessel = "";
    private string _defaultProject = "";
    private bool? _connectionTestResult;
    private string _connectionTestMessage = "Not tested";
    private bool _isTesting;
    private string _firewallStatus = "";
    private bool _firewallRuleExists;
    private bool _isCheckingFirewall;
    
    // NaviPac Data Configuration
    private bool _enableDebugLogging = true;
    private string _naviPacSeparator = ",";
    private bool _autoDetectFields = true;
    private bool _captureEvent = true;
    private bool _captureDateTime = true;
    private bool _captureEasting = true;
    private bool _captureNorthing = true;
    private bool _captureHeight = true;
    private bool _captureKP = true;
    private bool _captureDCC = true;
    private bool _captureLatitude = true;
    private bool _captureLongitude = true;
    private bool _captureGyro = true;
    private bool _captureRoll = true;
    private bool _capturePitch = true;
    private bool _captureHeave = true;
    private bool _captureSMG = true;
    private bool _captureCMG = true;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the window should close. Parameter indicates dialog result.
    /// </summary>
    public event Action<bool?>? RequestClose;

    #endregion

    #region Constructor

    public SettingsViewModel() : this(new ApplicationSettings())
    {
    }

    public SettingsViewModel(ApplicationSettings settings)
    {
        _originalSettings = settings.Clone();
        LoadSettings(settings);

        // Initialize commands
        TestConnectionCommand = new RelayCommand(_ => TestConnectionAsync(), _ => EnableNaviPacConnection && !_isTesting);
        BrowseSlogPathCommand = new RelayCommand(_ => BrowsePath(path => SlogFilesPath = path));
        BrowseNpcPathCommand = new RelayCommand(_ => BrowsePath(path => NpcFilesPath = path));
        BrowseDvrPathCommand = new RelayCommand(_ => BrowsePath(path => DvrFolderPath = path));
        BrowseSavePathCommand = new RelayCommand(_ => BrowsePath(path => DefaultSaveDirectory = path));
        ResetToDefaultsCommand = new RelayCommand(_ => ResetToDefaults());
        SaveCommand = new RelayCommand(_ => Save());
        CancelCommand = new RelayCommand(_ => Cancel());
        CreateFirewallRuleCommand = new RelayCommand(_ => CreateFirewallRuleAsync(), _ => NaviPacProtocol == NaviPacProtocol.UDP && !_firewallRuleExists);
        CheckFirewallStatusCommand = new RelayCommand(_ => { _ = CheckFirewallStatusAsync(); }, _ => !_isCheckingFirewall);
        CopyFirewallCommandCommand = new RelayCommand(_ => CopyFirewallCommand());
        SelectAllFieldsCommand = new RelayCommand(_ => SelectAllFields());
        ClearAllFieldsCommand = new RelayCommand(_ => ClearAllFields());
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the current settings based on view model property values.
    /// </summary>
    public ApplicationSettings? Settings => GetSettings();

    #endregion

    #region Properties - NaviPac Connection

    public bool EnableNaviPacConnection
    {
        get => _enableNaviPacConnection;
        set => SetProperty(ref _enableNaviPacConnection, value);
    }

    public string NaviPacIpAddress
    {
        get => _naviPacIpAddress;
        set => SetProperty(ref _naviPacIpAddress, value);
    }

    public int NaviPacPort
    {
        get => _naviPacPort;
        set => SetProperty(ref _naviPacPort, value);
    }

    public int ReconnectIntervalSeconds
    {
        get => _reconnectIntervalSeconds;
        set => SetProperty(ref _reconnectIntervalSeconds, value);
    }

    public int ConnectionTimeoutSeconds
    {
        get => _connectionTimeoutSeconds;
        set => SetProperty(ref _connectionTimeoutSeconds, value);
    }

    public NaviPacProtocol NaviPacProtocol
    {
        get => _naviPacProtocol;
        set
        {
            if (SetProperty(ref _naviPacProtocol, value))
            {
                OnPropertyChanged(nameof(IsTcpProtocol));
                OnPropertyChanged(nameof(IsUdpProtocol));
                OnPropertyChanged(nameof(ProtocolDescription));
                _ = CheckFirewallStatusAsync();
            }
        }
    }

    public bool IsTcpProtocol
    {
        get => NaviPacProtocol == NaviPacProtocol.TCP;
        set { if (value) NaviPacProtocol = NaviPacProtocol.TCP; }
    }

    public bool IsUdpProtocol
    {
        get => NaviPacProtocol == NaviPacProtocol.UDP;
        set { if (value) NaviPacProtocol = NaviPacProtocol.UDP; }
    }

    public string ProtocolDescription => NaviPacProtocol switch
    {
        NaviPacProtocol.TCP => "TCP: Connects to NaviPac server (outbound connection, no firewall rule needed)",
        NaviPacProtocol.UDP => "UDP: Listens for NaviPac broadcasts (inbound, requires firewall rule)",
        _ => ""
    };

    public bool AutoCreateFirewallRule
    {
        get => _autoCreateFirewallRule;
        set => SetProperty(ref _autoCreateFirewallRule, value);
    }

    public string FirewallStatus
    {
        get => _firewallStatus;
        set => SetProperty(ref _firewallStatus, value);
    }

    public bool FirewallRuleExists
    {
        get => _firewallRuleExists;
        set => SetProperty(ref _firewallRuleExists, value);
    }

    #endregion

    #region Properties - NaviPac Data Configuration

    public bool EnableDebugLogging
    {
        get => _enableDebugLogging;
        set => SetProperty(ref _enableDebugLogging, value);
    }

    public string NaviPacSeparator
    {
        get => _naviPacSeparator;
        set => SetProperty(ref _naviPacSeparator, value);
    }

    public bool AutoDetectFields
    {
        get => _autoDetectFields;
        set
        {
            if (SetProperty(ref _autoDetectFields, value))
                OnPropertyChanged(nameof(ManualFieldMapping));
        }
    }

    public bool ManualFieldMapping
    {
        get => !_autoDetectFields;
        set => AutoDetectFields = !value;
    }

    public bool CaptureEvent
    {
        get => _captureEvent;
        set => SetProperty(ref _captureEvent, value);
    }

    public bool CaptureDateTime
    {
        get => _captureDateTime;
        set => SetProperty(ref _captureDateTime, value);
    }

    public bool CaptureEasting
    {
        get => _captureEasting;
        set => SetProperty(ref _captureEasting, value);
    }

    public bool CaptureNorthing
    {
        get => _captureNorthing;
        set => SetProperty(ref _captureNorthing, value);
    }

    public bool CaptureHeight
    {
        get => _captureHeight;
        set => SetProperty(ref _captureHeight, value);
    }

    public bool CaptureKP
    {
        get => _captureKP;
        set => SetProperty(ref _captureKP, value);
    }

    public bool CaptureDCC
    {
        get => _captureDCC;
        set => SetProperty(ref _captureDCC, value);
    }

    public bool CaptureLatitude
    {
        get => _captureLatitude;
        set => SetProperty(ref _captureLatitude, value);
    }

    public bool CaptureLongitude
    {
        get => _captureLongitude;
        set => SetProperty(ref _captureLongitude, value);
    }

    public bool CaptureGyro
    {
        get => _captureGyro;
        set => SetProperty(ref _captureGyro, value);
    }

    public bool CaptureRoll
    {
        get => _captureRoll;
        set => SetProperty(ref _captureRoll, value);
    }

    public bool CapturePitch
    {
        get => _capturePitch;
        set => SetProperty(ref _capturePitch, value);
    }

    public bool CaptureHeave
    {
        get => _captureHeave;
        set => SetProperty(ref _captureHeave, value);
    }

    public bool CaptureSMG
    {
        get => _captureSMG;
        set => SetProperty(ref _captureSMG, value);
    }

    public bool CaptureCMG
    {
        get => _captureCMG;
        set => SetProperty(ref _captureCMG, value);
    }

    #endregion

    #region Properties - File Monitoring

    public bool EnableFileMonitoring
    {
        get => _enableFileMonitoring;
        set => SetProperty(ref _enableFileMonitoring, value);
    }

    public string SlogFilesPath
    {
        get => _slogFilesPath;
        set => SetProperty(ref _slogFilesPath, value);
    }

    public string NpcFilesPath
    {
        get => _npcFilesPath;
        set => SetProperty(ref _npcFilesPath, value);
    }

    public string DvrFolderPath
    {
        get => _dvrFolderPath;
        set => SetProperty(ref _dvrFolderPath, value);
    }

    #endregion

    #region Properties - Auto-Save

    public bool EnableAutoSave
    {
        get => _enableAutoSave;
        set => SetProperty(ref _enableAutoSave, value);
    }

    public int AutoSaveIntervalMinutes
    {
        get => _autoSaveIntervalMinutes;
        set => SetProperty(ref _autoSaveIntervalMinutes, value);
    }

    public string DefaultSaveDirectory
    {
        get => _defaultSaveDirectory;
        set => SetProperty(ref _defaultSaveDirectory, value);
    }

    #endregion

    #region Properties - Project Defaults

    public string DefaultClient
    {
        get => _defaultClient;
        set => SetProperty(ref _defaultClient, value);
    }

    public string DefaultVessel
    {
        get => _defaultVessel;
        set => SetProperty(ref _defaultVessel, value);
    }

    public string DefaultProject
    {
        get => _defaultProject;
        set => SetProperty(ref _defaultProject, value);
    }

    #endregion

    #region Properties - Connection Test

    public bool? ConnectionTestResult
    {
        get => _connectionTestResult;
        set => SetProperty(ref _connectionTestResult, value);
    }

    public string ConnectionTestMessage
    {
        get => _connectionTestMessage;
        set => SetProperty(ref _connectionTestMessage, value);
    }

    #endregion

    #region Commands

    public ICommand TestConnectionCommand { get; }
    public ICommand BrowseSlogPathCommand { get; }
    public ICommand BrowseNpcPathCommand { get; }
    public ICommand BrowseDvrPathCommand { get; }
    public ICommand BrowseSavePathCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand CreateFirewallRuleCommand { get; }
    public ICommand CheckFirewallStatusCommand { get; }
    public ICommand CopyFirewallCommandCommand { get; }
    public ICommand SelectAllFieldsCommand { get; }
    public ICommand ClearAllFieldsCommand { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads settings into the view model properties.
    /// </summary>
    public void LoadSettings(ApplicationSettings settings)
    {
        _originalSettings = settings.Clone();

        EnableNaviPacConnection = settings.EnableNaviPacConnection;
        NaviPacIpAddress = settings.NaviPacIpAddress;
        NaviPacPort = settings.NaviPacPort;
        NaviPacProtocol = settings.NaviPacProtocol;
        ReconnectIntervalSeconds = settings.ReconnectIntervalSeconds;
        ConnectionTimeoutSeconds = settings.ConnectionTimeoutSeconds;
        AutoCreateFirewallRule = settings.AutoCreateFirewallRule;
        
        // NaviPac Data Configuration
        EnableDebugLogging = settings.EnableDebugLogging;
        NaviPacSeparator = settings.NaviPacSeparator;
        AutoDetectFields = settings.AutoDetectFields;
        CaptureEvent = settings.CaptureEvent;
        CaptureDateTime = settings.CaptureDateTime;
        CaptureEasting = settings.CaptureEasting;
        CaptureNorthing = settings.CaptureNorthing;
        CaptureHeight = settings.CaptureHeight;
        CaptureKP = settings.CaptureKP;
        CaptureDCC = settings.CaptureDCC;
        CaptureLatitude = settings.CaptureLatitude;
        CaptureLongitude = settings.CaptureLongitude;
        CaptureGyro = settings.CaptureGyro;
        CaptureRoll = settings.CaptureRoll;
        CapturePitch = settings.CapturePitch;
        CaptureHeave = settings.CaptureHeave;
        CaptureSMG = settings.CaptureSMG;
        CaptureCMG = settings.CaptureCMG;

        EnableFileMonitoring = settings.EnableFileMonitoring;
        SlogFilesPath = settings.SlogFilesPath;
        NpcFilesPath = settings.NpcFilesPath;
        DvrFolderPath = settings.DvrFolderPath;

        EnableAutoSave = settings.EnableAutoSave;
        AutoSaveIntervalMinutes = settings.AutoSaveIntervalMinutes;
        DefaultSaveDirectory = settings.DefaultSaveDirectory;

        DefaultClient = settings.DefaultClient;
        DefaultVessel = settings.DefaultVessel;
        DefaultProject = settings.DefaultProject;

        // Check firewall status for UDP
        _ = CheckFirewallStatusAsync();
    }

    /// <summary>
    /// Gets the current settings from the view model properties.
    /// </summary>
    public ApplicationSettings GetSettings()
    {
        return new ApplicationSettings
        {
            EnableNaviPacConnection = EnableNaviPacConnection,
            NaviPacIpAddress = NaviPacIpAddress,
            NaviPacPort = NaviPacPort,
            NaviPacProtocol = NaviPacProtocol,
            ReconnectIntervalSeconds = ReconnectIntervalSeconds,
            ConnectionTimeoutSeconds = ConnectionTimeoutSeconds,
            AutoCreateFirewallRule = AutoCreateFirewallRule,
            
            // NaviPac Data Configuration
            EnableDebugLogging = EnableDebugLogging,
            NaviPacSeparator = NaviPacSeparator,
            AutoDetectFields = AutoDetectFields,
            CaptureEvent = CaptureEvent,
            CaptureDateTime = CaptureDateTime,
            CaptureEasting = CaptureEasting,
            CaptureNorthing = CaptureNorthing,
            CaptureHeight = CaptureHeight,
            CaptureKP = CaptureKP,
            CaptureDCC = CaptureDCC,
            CaptureLatitude = CaptureLatitude,
            CaptureLongitude = CaptureLongitude,
            CaptureGyro = CaptureGyro,
            CaptureRoll = CaptureRoll,
            CapturePitch = CapturePitch,
            CaptureHeave = CaptureHeave,
            CaptureSMG = CaptureSMG,
            CaptureCMG = CaptureCMG,

            EnableFileMonitoring = EnableFileMonitoring,
            SlogFilesPath = SlogFilesPath,
            NpcFilesPath = NpcFilesPath,
            DvrFolderPath = DvrFolderPath,

            EnableAutoSave = EnableAutoSave,
            AutoSaveIntervalMinutes = AutoSaveIntervalMinutes,
            DefaultSaveDirectory = DefaultSaveDirectory,

            DefaultClient = DefaultClient,
            DefaultVessel = DefaultVessel,
            DefaultProject = DefaultProject,

            // Preserve UI settings from original
            Theme = _originalSettings.Theme,
            LastSelectedTabIndex = _originalSettings.LastSelectedTabIndex,
            WindowWidth = _originalSettings.WindowWidth,
            WindowHeight = _originalSettings.WindowHeight
        };
    }

    #endregion

    #region Private Methods

    private async void TestConnectionAsync()
    {
        _isTesting = true;
        ConnectionTestResult = null;
        ConnectionTestMessage = $"Testing {NaviPacProtocol} connection...";

        try
        {
            if (NaviPacProtocol == NaviPacProtocol.TCP)
            {
                await TestTcpConnectionAsync();
            }
            else
            {
                await TestUdpConnectionAsync();
            }
        }
        catch (SocketException ex)
        {
            ConnectionTestResult = false;
            ConnectionTestMessage = $"Connection failed: {ex.SocketErrorCode} - {ex.Message}";
        }
        catch (Exception ex)
        {
            ConnectionTestResult = false;
            ConnectionTestMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isTesting = false;
        }
    }

    private async Task TestTcpConnectionAsync()
    {
        await Task.Run(async () =>
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(NaviPacIpAddress, NaviPacPort);

            if (await Task.WhenAny(connectTask, Task.Delay(ConnectionTimeoutSeconds * 1000)) == connectTask)
            {
                if (client.Connected)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConnectionTestResult = true;
                        ConnectionTestMessage = $"TCP: Connected successfully to {NaviPacIpAddress}:{NaviPacPort}";
                    });
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConnectionTestResult = false;
                        ConnectionTestMessage = "TCP: Connection failed - server not responding";
                    });
                }
            }
            else
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionTestResult = false;
                    ConnectionTestMessage = $"TCP: Connection timed out after {ConnectionTimeoutSeconds} seconds";
                });
            }
        });
    }

    private async Task TestUdpConnectionAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                // For UDP, test if we can bind to the port
                using var testClient = new System.Net.Sockets.UdpClient(NaviPacPort);
                testClient.Client.SetSocketOption(
                    System.Net.Sockets.SocketOptionLevel.Socket,
                    System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionTestResult = true;
                    ConnectionTestMessage = $"UDP: Port {NaviPacPort} is available for listening.\n" +
                        "Note: Ensure NaviPac is configured to send data to this port.";
                });
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionTestResult = false;
                    ConnectionTestMessage = $"UDP: Port {NaviPacPort} is already in use by another application.";
                });
            }
            catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.AccessDenied)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionTestResult = false;
                    ConnectionTestMessage = $"UDP: Access denied to port {NaviPacPort}. Check firewall settings.";
                });
            }
        });
    }

    private async Task CheckFirewallStatusAsync()
    {
        if (NaviPacProtocol != NaviPacProtocol.UDP)
        {
            FirewallStatus = "TCP connections typically don't require firewall rules.";
            FirewallRuleExists = true; // Not needed for TCP
            return;
        }

        _isCheckingFirewall = true;
        FirewallStatus = "Checking firewall status...";

        try
        {
            var ruleExists = await FirewallService.RuleExistsAsync(NaviPacPort, NaviPacProtocol.UDP);
            FirewallRuleExists = ruleExists;

            if (ruleExists)
            {
                FirewallStatus = $"✓ Firewall rule exists for UDP port {NaviPacPort}";
            }
            else
            {
                FirewallStatus = $"⚠ No firewall rule found for UDP port {NaviPacPort}.\n" +
                    "NaviPac data may be blocked. Click 'Create Rule' or run command manually.";
            }
        }
        catch (Exception ex)
        {
            FirewallStatus = $"Could not check firewall status: {ex.Message}";
            FirewallRuleExists = false;
        }
        finally
        {
            _isCheckingFirewall = false;
        }
    }

    private async void CreateFirewallRuleAsync()
    {
        if (FirewallService.IsRunningAsAdministrator)
        {
            var result = await FirewallService.CreateRuleAsync(NaviPacPort, NaviPacProtocol.UDP,
                "Allow NaviPac UDP data for Fathom OS Survey Logbook");

            if (result.Success)
            {
                System.Windows.MessageBox.Show(result.Message, "Firewall Rule Created",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                await CheckFirewallStatusAsync();
            }
            else
            {
                System.Windows.MessageBox.Show(result.Message, "Firewall Rule Creation Failed",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        else
        {
            var dialogResult = System.Windows.MessageBox.Show(
                "Administrator privileges are required to create firewall rules.\n\n" +
                "Would you like to:\n" +
                "• Click 'Yes' to open an elevated command prompt\n" +
                "• Click 'No' to copy the command to clipboard (run manually)\n" +
                "• Click 'Cancel' to cancel",
                "Administrator Required",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);

            if (dialogResult == System.Windows.MessageBoxResult.Yes)
            {
                FirewallService.RequestElevatedRuleCreation(NaviPacPort, NaviPacProtocol.UDP);
            }
            else if (dialogResult == System.Windows.MessageBoxResult.No)
            {
                CopyFirewallCommand();
            }
        }
    }

    private void CopyFirewallCommand()
    {
        var command = FirewallService.GetManualRuleCommand(NaviPacPort, NaviPacProtocol.UDP);
        System.Windows.Clipboard.SetText(command);
        System.Windows.MessageBox.Show(
            "The following command has been copied to clipboard:\n\n" + command + "\n\n" +
            "Run this command in an elevated (Administrator) Command Prompt.",
            "Command Copied",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private void SelectAllFields()
    {
        CaptureEvent = true;
        CaptureDateTime = true;
        CaptureEasting = true;
        CaptureNorthing = true;
        CaptureHeight = true;
        CaptureKP = true;
        CaptureDCC = true;
        CaptureLatitude = true;
        CaptureLongitude = true;
        CaptureGyro = true;
        CaptureRoll = true;
        CapturePitch = true;
        CaptureHeave = true;
        CaptureSMG = true;
        CaptureCMG = true;
    }

    private void ClearAllFields()
    {
        CaptureEvent = false;
        CaptureDateTime = false;
        CaptureEasting = false;
        CaptureNorthing = false;
        CaptureHeight = false;
        CaptureKP = false;
        CaptureDCC = false;
        CaptureLatitude = false;
        CaptureLongitude = false;
        CaptureGyro = false;
        CaptureRoll = false;
        CapturePitch = false;
        CaptureHeave = false;
        CaptureSMG = false;
        CaptureCMG = false;
    }

    private void BrowsePath(Action<string> setPath)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select Directory",
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            setPath(dialog.SelectedPath);
        }
    }

    private void ResetToDefaults()
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to reset all settings to their default values?",
            "Reset Settings",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            var defaults = new ApplicationSettings();
            LoadSettings(defaults);
        }
    }

    private void Save()
    {
        RequestClose?.Invoke(true);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    #endregion
}
