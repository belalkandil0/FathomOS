using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace FathomOS.TimeSyncAgent.Tray;

public class TrayViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _statusTimer;
    private readonly string _serviceName = "FathomOSTimeSyncAgent";
    private readonly int _agentPort = 7700;

    private string _currentTime = "--:--:--";
    private string _currentDate = "---";
    private string _serviceStatusText = "Unknown";
    private string _statusColor = "Gray";
    private bool _isServiceRunning;
    private bool _isPortListening;
    private string _ipAddress = "Unknown";
    private int _connectionsToday;
    private string _lastConnection = "Never";
    private string _uptime = "--";
    private DateTime? _serviceStartTime;
    private string _agentVersion = "Unknown";

    public TrayViewModel()
    {
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100) // Update frequently for smooth seconds
        };
        _clockTimer.Tick += (s, e) => UpdateClock();

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5) // Check status every 5 seconds
        };
        _statusTimer.Tick += async (s, e) => await UpdateStatusAsync();

        // Get IP address
        UpdateIpAddress();
    }

    #region Properties

    public string CurrentTime
    {
        get => _currentTime;
        set => SetProperty(ref _currentTime, value);
    }

    public string CurrentDate
    {
        get => _currentDate;
        set => SetProperty(ref _currentDate, value);
    }

    public string ServiceStatusText
    {
        get => _serviceStatusText;
        set => SetProperty(ref _serviceStatusText, value);
    }

    public string StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    public bool IsServiceRunning
    {
        get => _isServiceRunning;
        set => SetProperty(ref _isServiceRunning, value);
    }

    public bool IsPortListening
    {
        get => _isPortListening;
        set => SetProperty(ref _isPortListening, value);
    }

    public string IpAddress
    {
        get => _ipAddress;
        set => SetProperty(ref _ipAddress, value);
    }

    public int ConnectionsToday
    {
        get => _connectionsToday;
        set => SetProperty(ref _connectionsToday, value);
    }

    public string LastConnection
    {
        get => _lastConnection;
        set => SetProperty(ref _lastConnection, value);
    }

    public string Uptime
    {
        get => _uptime;
        set => SetProperty(ref _uptime, value);
    }

    public string AgentVersion
    {
        get => _agentVersion;
        set => SetProperty(ref _agentVersion, value);
    }

    public int AgentPort => _agentPort;

    #endregion

    #region Methods

    public void StartMonitoring()
    {
        _clockTimer.Start();
        _statusTimer.Start();
        UpdateClock();
        _ = UpdateStatusAsync();
    }

    public void StopMonitoring()
    {
        _clockTimer.Stop();
        _statusTimer.Stop();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("dddd, MMMM d, yyyy");

        // Update uptime
        if (_serviceStartTime.HasValue)
        {
            var uptime = DateTime.Now - _serviceStartTime.Value;
            if (uptime.TotalDays >= 1)
                Uptime = $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
            else if (uptime.TotalHours >= 1)
                Uptime = $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
            else
                Uptime = $"{uptime.Minutes}m {uptime.Seconds}s";
        }
        else
        {
            Uptime = "--";
        }
    }

    private async Task UpdateStatusAsync()
    {
        // Check service status
        IsServiceRunning = CheckServiceRunning();

        // Check port listening
        IsPortListening = CheckPortListening();

        // Update status display
        if (IsServiceRunning && IsPortListening)
        {
            ServiceStatusText = "Running";
            StatusColor = "Green";

            // Try to get agent info
            await GetAgentInfoAsync();
        }
        else if (IsServiceRunning && !IsPortListening)
        {
            ServiceStatusText = "Starting...";
            StatusColor = "Yellow";
            _serviceStartTime = null;
        }
        else
        {
            ServiceStatusText = "Stopped";
            StatusColor = "Red";
            _serviceStartTime = null;
        }
    }

    private bool CheckServiceRunning()
    {
        try
        {
            using var sc = new ServiceController(_serviceName);
            return sc.Status == ServiceControllerStatus.Running;
        }
        catch
        {
            return false;
        }
    }

    private bool CheckPortListening()
    {
        try
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var listeners = properties.GetActiveTcpListeners();
            return listeners.Any(l => l.Port == _agentPort);
        }
        catch
        {
            return false;
        }
    }

    private async Task GetAgentInfoAsync()
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync("127.0.0.1", _agentPort);
            if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                return;

            await connectTask; // Propagate exceptions

            using var stream = client.GetStream();
            var utf8NoBom = new UTF8Encoding(false);
            using var reader = new StreamReader(stream, utf8NoBom);
            using var writer = new StreamWriter(stream, utf8NoBom) { AutoFlush = true };

            // Send GetInfo command
            // SECURITY FIX (VULN-001): Load secret from configuration instead of hardcoding
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var secret = GetConfiguredSecret();
            if (string.IsNullOrEmpty(secret))
            {
                // Cannot authenticate without configured secret
                return;
            }
            var authData = $"{secret}:{timestamp}";
            var authHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(authData))).ToLowerInvariant();

            var request = new
            {
                command = "GetInfo",
                timestamp = timestamp,
                auth = authHash
            };

            await writer.WriteLineAsync(JsonSerializer.Serialize(request));

            var responseJson = await reader.ReadLineAsync();
            if (!string.IsNullOrEmpty(responseJson))
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var status) && 
                    status.GetString() == "Success")
                {
                    if (root.TryGetProperty("agentVersion", out var version))
                        AgentVersion = version.GetString() ?? "Unknown";

                    if (root.TryGetProperty("startTime", out var startTime))
                    {
                        if (DateTime.TryParse(startTime.GetString(), out var st))
                            _serviceStartTime = st;
                    }

                    if (root.TryGetProperty("totalConnections", out var connections))
                        ConnectionsToday = connections.GetInt32();

                    if (root.TryGetProperty("lastConnectionTime", out var lastConn))
                    {
                        if (DateTime.TryParse(lastConn.GetString(), out var lc))
                        {
                            var elapsed = DateTime.Now - lc;
                            if (elapsed.TotalSeconds < 60)
                                LastConnection = $"{(int)elapsed.TotalSeconds}s ago";
                            else if (elapsed.TotalMinutes < 60)
                                LastConnection = $"{(int)elapsed.TotalMinutes}m ago";
                            else
                                LastConnection = lc.ToString("HH:mm:ss");
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore connection errors
        }
    }

    private void UpdateIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .Where(a => !a.ToString().StartsWith("169.254"))
                .FirstOrDefault(a => a.ToString().StartsWith("192.168") ||
                                     a.ToString().StartsWith("10.") ||
                                     a.ToString().StartsWith("172."));

            IpAddress = ip?.ToString() ?? "Unknown";
        }
        catch
        {
            IpAddress = "Unknown";
        }
    }

    /// <summary>
    /// Gets the configured shared secret from appsettings.json.
    /// SECURITY FIX (VULN-001): Secret must be configured - no hardcoded defaults.
    /// </summary>
    private string GetConfiguredSecret()
    {
        try
        {
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("TimeSyncAgent", out var agent) &&
                    agent.TryGetProperty("SharedSecret", out var secret))
                {
                    var secretValue = secret.GetString();
                    // Reject known weak default
                    if (secretValue == "FathomOSTimeSync2024")
                    {
                        return string.Empty;
                    }
                    return secretValue ?? string.Empty;
                }
            }
        }
        catch { }
        return string.Empty;
    }

    public void StartService()
    {
        try
        {
            using var sc = new ServiceController(_serviceName);
            if (sc.Status != ServiceControllerStatus.Running)
            {
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to start service: {ex.Message}\n\nRun as Administrator to manage services.",
                "Service Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public void StopService()
    {
        try
        {
            using var sc = new ServiceController(_serviceName);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to stop service: {ex.Message}\n\nRun as Administrator to manage services.",
                "Service Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    public void RestartService()
    {
        try
        {
            using var sc = new ServiceController(_serviceName);
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            }
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to restart service: {ex.Message}\n\nRun as Administrator to manage services.",
                "Service Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        StopMonitoring();
    }

    #endregion
}
