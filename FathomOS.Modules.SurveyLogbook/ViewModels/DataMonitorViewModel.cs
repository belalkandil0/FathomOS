// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: ViewModels/DataMonitorViewModel.cs
// Purpose: ViewModel for real-time NaviPac data monitoring window
// Version: 9.0.0
// ============================================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FathomOS.Modules.SurveyLogbook.Models;
using FathomOS.Modules.SurveyLogbook.Services;

namespace FathomOS.Modules.SurveyLogbook.ViewModels;

/// <summary>
/// Represents a single raw data message with timestamp.
/// </summary>
public class RawDataMessage : INotifyPropertyChanged
{
    public DateTime Timestamp { get; set; }
    public string Data { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int ByteCount { get; set; }
    
    /// <summary>
    /// Formatted display string for the message.
    /// </summary>
    public string Display => $"[{Timestamp:HH:mm:ss.fff}] {Data}";
    
    /// <summary>
    /// Short display for list view.
    /// </summary>
    public string ShortDisplay => Data.Length > 100 ? Data.Substring(0, 100) + "..." : Data;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents a parsed field from NaviPac data.
/// </summary>
public class ParsedField : INotifyPropertyChanged
{
    public int Index { get; set; }
    public string RawValue { get; set; } = string.Empty;
    public string DetectedType { get; set; } = "Unknown";
    public string AssignedName { get; set; } = string.Empty;
    public bool IsNumeric { get; set; }
    
    /// <summary>
    /// Display string showing field details.
    /// </summary>
    public string Display => $"Field {Index}: {RawValue,-20} → {AssignedName}";
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// ViewModel for the real-time NaviPac data monitoring window.
/// Provides visibility into raw data reception and parsing.
/// </summary>
public class DataMonitorViewModel : ViewModelBase, IDisposable
{
    #region Constants
    
    private const int MAX_MESSAGES = 500;
    private const int RATE_CALCULATION_WINDOW_SECONDS = 5;
    
    #endregion
    
    #region Fields
    
    private readonly NaviPacClient? _naviPacClient;
    private readonly DispatcherTimer _updateTimer;
    private readonly Queue<DateTime> _messageTimestamps = new();
    private readonly object _lockObject = new();
    
    private bool _isPaused;
    private bool _autoScroll = true;
    private string _connectionStatus = "Not Connected";
    private string _connectionStatusColor = "Gray";
    private string _protocolInfo = "N/A";
    private long _totalMessages;
    private long _totalBytes;
    private double _messagesPerSecond;
    private DateTime _lastDataTime;
    private string _selectedSeparator = ",";
    private RawDataMessage? _selectedMessage;
    private bool _isDisposed;
    
    #endregion
    
    #region Constructor
    
    /// <summary>
    /// Initializes a new DataMonitorViewModel.
    /// </summary>
    /// <param name="naviPacClient">The NaviPac client to monitor (can be null for design mode).</param>
    public DataMonitorViewModel(NaviPacClient? naviPacClient = null)
    {
        _naviPacClient = naviPacClient;
        
        // Initialize collections
        RawMessages = new ObservableCollection<RawDataMessage>();
        ParsedFields = new ObservableCollection<ParsedField>();
        
        // Initialize commands
        ClearCommand = new RelayCommand(_ => Clear(), _ => RawMessages.Count > 0);
        PauseCommand = new RelayCommand(_ => TogglePause());
        ExportLogCommand = new RelayCommand(_ => ExportLog(), _ => RawMessages.Count > 0);
        CopyLastMessageCommand = new RelayCommand(_ => CopyLastMessage(), _ => RawMessages.Count > 0);
        CopySelectedMessageCommand = new RelayCommand(_ => CopySelectedMessage(), _ => SelectedMessage != null);
        
        // Setup update timer for rate calculation
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
        
        // Subscribe to NaviPac events if client provided
        if (_naviPacClient != null)
        {
            _naviPacClient.RawDataReceived += NaviPacClient_RawDataReceived;
            _naviPacClient.ConnectionStatusChanged += NaviPacClient_ConnectionStatusChanged;
            UpdateConnectionStatus();
        }
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Collection of raw data messages received.
    /// </summary>
    public ObservableCollection<RawDataMessage> RawMessages { get; }
    
    /// <summary>
    /// Collection of parsed fields from the selected/last message.
    /// </summary>
    public ObservableCollection<ParsedField> ParsedFields { get; }
    
    /// <summary>
    /// Whether message collection is paused.
    /// </summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (SetProperty(ref _isPaused, value))
            {
                OnPropertyChanged(nameof(PauseButtonText));
                OnPropertyChanged(nameof(PauseButtonIcon));
            }
        }
    }
    
    /// <summary>
    /// Text for pause button.
    /// </summary>
    public string PauseButtonText => IsPaused ? "Resume" : "Pause";
    
    /// <summary>
    /// Icon name for pause button.
    /// </summary>
    public string PauseButtonIcon => IsPaused ? "Play" : "Pause";
    
    /// <summary>
    /// Whether to auto-scroll to latest message.
    /// </summary>
    public bool AutoScroll
    {
        get => _autoScroll;
        set => SetProperty(ref _autoScroll, value);
    }
    
    /// <summary>
    /// Connection status text.
    /// </summary>
    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetProperty(ref _connectionStatus, value);
    }
    
    /// <summary>
    /// Connection status indicator color.
    /// </summary>
    public string ConnectionStatusColor
    {
        get => _connectionStatusColor;
        set => SetProperty(ref _connectionStatusColor, value);
    }
    
    /// <summary>
    /// Protocol information (TCP/UDP, port, etc.).
    /// </summary>
    public string ProtocolInfo
    {
        get => _protocolInfo;
        set => SetProperty(ref _protocolInfo, value);
    }
    
    /// <summary>
    /// Total number of messages received.
    /// </summary>
    public long TotalMessages
    {
        get => _totalMessages;
        set => SetProperty(ref _totalMessages, value);
    }
    
    /// <summary>
    /// Total bytes received.
    /// </summary>
    public long TotalBytes
    {
        get => _totalBytes;
        set => SetProperty(ref _totalBytes, value);
    }
    
    /// <summary>
    /// Formatted total bytes string.
    /// </summary>
    public string TotalBytesFormatted
    {
        get
        {
            if (TotalBytes < 1024) return $"{TotalBytes} B";
            if (TotalBytes < 1024 * 1024) return $"{TotalBytes / 1024.0:F1} KB";
            return $"{TotalBytes / (1024.0 * 1024.0):F2} MB";
        }
    }
    
    /// <summary>
    /// Messages per second rate.
    /// </summary>
    public double MessagesPerSecond
    {
        get => _messagesPerSecond;
        set => SetProperty(ref _messagesPerSecond, value);
    }
    
    /// <summary>
    /// Formatted rate string.
    /// </summary>
    public string RateDisplay => $"{MessagesPerSecond:F1} msg/sec";
    
    /// <summary>
    /// Last data received time.
    /// </summary>
    public DateTime LastDataTime
    {
        get => _lastDataTime;
        set
        {
            if (SetProperty(ref _lastDataTime, value))
            {
                OnPropertyChanged(nameof(LastDataTimeDisplay));
                OnPropertyChanged(nameof(TimeSinceLastData));
            }
        }
    }
    
    /// <summary>
    /// Formatted last data time.
    /// </summary>
    public string LastDataTimeDisplay => 
        LastDataTime == DateTime.MinValue ? "Never" : LastDataTime.ToString("HH:mm:ss.fff");
    
    /// <summary>
    /// Time since last data was received.
    /// </summary>
    public string TimeSinceLastData
    {
        get
        {
            if (LastDataTime == DateTime.MinValue) return "N/A";
            var elapsed = DateTime.Now - LastDataTime;
            if (elapsed.TotalSeconds < 1) return "< 1 sec ago";
            if (elapsed.TotalSeconds < 60) return $"{elapsed.TotalSeconds:F0} sec ago";
            return $"{elapsed.TotalMinutes:F1} min ago";
        }
    }
    
    /// <summary>
    /// Selected separator for parsing display.
    /// </summary>
    public string SelectedSeparator
    {
        get => _selectedSeparator;
        set
        {
            if (SetProperty(ref _selectedSeparator, value))
            {
                // Re-parse current message with new separator
                if (SelectedMessage != null)
                {
                    ParseMessage(SelectedMessage.Data);
                }
            }
        }
    }
    
    /// <summary>
    /// Currently selected message for detailed parsing.
    /// </summary>
    public RawDataMessage? SelectedMessage
    {
        get => _selectedMessage;
        set
        {
            if (SetProperty(ref _selectedMessage, value) && value != null)
            {
                ParseMessage(value.Data);
            }
        }
    }
    
    /// <summary>
    /// Full text of selected message for display.
    /// </summary>
    public string SelectedMessageText => SelectedMessage?.Data ?? "(No message selected)";
    
    #endregion
    
    #region Commands
    
    public ICommand ClearCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ExportLogCommand { get; }
    public ICommand CopyLastMessageCommand { get; }
    public ICommand CopySelectedMessageCommand { get; }
    
    #endregion
    
    #region Event Handlers
    
    private void NaviPacClient_RawDataReceived(object? sender, RawDataEventArgs e)
    {
        if (IsPaused || _isDisposed) return;
        
        Application.Current?.Dispatcher?.BeginInvoke(() =>
        {
            try
            {
                AddMessage(new RawDataMessage
                {
                    Timestamp = e.Timestamp,
                    Data = e.Data,
                    Source = $"{e.Protocol} {e.RemoteEndPoint?.ToString() ?? "Unknown"}",
                    ByteCount = e.Data?.Length ?? 0
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding raw data message: {ex.Message}");
            }
        });
    }
    
    private void NaviPacClient_ConnectionStatusChanged(object? sender, ConnectionStatusEventArgs e)
    {
        Application.Current?.Dispatcher?.BeginInvoke(() =>
        {
            UpdateConnectionStatus();
        });
    }
    
    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_isDisposed) return;
        
        CalculateMessageRate();
        OnPropertyChanged(nameof(TimeSinceLastData));
        OnPropertyChanged(nameof(TotalBytesFormatted));
        OnPropertyChanged(nameof(RateDisplay));
    }
    
    #endregion
    
    #region Methods
    
    /// <summary>
    /// Adds a raw data message to the collection.
    /// </summary>
    public void AddMessage(RawDataMessage message)
    {
        if (IsPaused) return;
        
        lock (_lockObject)
        {
            // Add to collection
            RawMessages.Add(message);
            
            // Trim old messages
            while (RawMessages.Count > MAX_MESSAGES)
            {
                RawMessages.RemoveAt(0);
            }
            
            // Update statistics
            TotalMessages++;
            TotalBytes += message.ByteCount;
            LastDataTime = message.Timestamp;
            
            // Track for rate calculation
            _messageTimestamps.Enqueue(message.Timestamp);
            
            // Auto-select last message if auto-scroll enabled
            if (AutoScroll)
            {
                SelectedMessage = message;
            }
        }
    }
    
    /// <summary>
    /// Manually adds test data (for testing without NaviPac connection).
    /// </summary>
    public void AddTestMessage(string data)
    {
        AddMessage(new RawDataMessage
        {
            Timestamp = DateTime.Now,
            Data = data,
            Source = "Test",
            ByteCount = data.Length
        });
    }
    
    /// <summary>
    /// Parses a message into individual fields.
    /// </summary>
    private void ParseMessage(string data)
    {
        ParsedFields.Clear();
        
        if (string.IsNullOrEmpty(data)) return;
        
        // Determine separator character
        char separator = SelectedSeparator switch
        {
            "," => ',',
            ";" => ';',
            ":" => ':',
            "Space" => ' ',
            "Tab" => '\t',
            _ => ','
        };
        
        // Split the data
        var fields = data.Split(separator);
        
        for (int i = 0; i < fields.Length; i++)
        {
            var rawValue = fields[i].Trim();
            var parsedField = new ParsedField
            {
                Index = i,
                RawValue = rawValue,
                DetectedType = DetectFieldType(rawValue),
                AssignedName = GuessFieldName(rawValue, i),
                IsNumeric = double.TryParse(rawValue, out _)
            };
            
            ParsedFields.Add(parsedField);
        }
        
        OnPropertyChanged(nameof(SelectedMessageText));
    }
    
    /// <summary>
    /// Attempts to detect the type of a field value.
    /// </summary>
    private string DetectFieldType(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Empty";
        
        // Check for integer
        if (int.TryParse(value, out _)) return "Integer";
        
        // Check for decimal
        if (double.TryParse(value, out var dblValue))
        {
            // Check if it looks like coordinates
            if (Math.Abs(dblValue) > 1000000) return "Large Decimal (Coordinate?)";
            if (Math.Abs(dblValue) >= 0 && Math.Abs(dblValue) <= 360) return "Decimal (Angle?)";
            return "Decimal";
        }
        
        // Check for date/time patterns
        if (DateTime.TryParse(value, out _)) return "DateTime";
        if (value.Contains(":") && value.Length <= 12) return "Time";
        if (value.Contains("/") || value.Contains("-")) return "Date?";
        
        // Check for lat/lon DMS format
        if (value.Contains("°") || value.Contains("'")) return "DMS Coordinate";
        
        return "String";
    }
    
    /// <summary>
    /// Attempts to guess a field name based on value patterns.
    /// </summary>
    private string GuessFieldName(string value, int index)
    {
        if (string.IsNullOrWhiteSpace(value)) return $"Field_{index}";
        
        if (double.TryParse(value, out var dblValue))
        {
            // Large values likely coordinates
            if (Math.Abs(dblValue) > 100000)
            {
                // Check magnitude for Easting/Northing
                if (Math.Abs(dblValue) > 1000000) return "Easting/Northing?";
            }
            
            // Values 0-360 could be heading/bearing
            if (dblValue >= 0 && dblValue <= 360) return "Heading/Bearing?";
            
            // Small values could be motion (roll/pitch/heave)
            if (Math.Abs(dblValue) < 30) return "Roll/Pitch/Heave?";
            
            // Depth range
            if (dblValue > 0 && dblValue < 5000) return "Depth/Height?";
        }
        
        // Integer could be event
        if (int.TryParse(value, out var intValue))
        {
            if (intValue > 0 && intValue < 100000) return "Event/Counter?";
        }
        
        // Time patterns
        if (value.Contains(":")) return "Time?";
        
        // DMS coordinates
        if (value.Contains("°")) return "Lat/Lon DMS?";
        
        return $"Field_{index}";
    }
    
    /// <summary>
    /// Updates connection status display from NaviPac client.
    /// </summary>
    private void UpdateConnectionStatus()
    {
        if (_naviPacClient == null)
        {
            ConnectionStatus = "No NaviPac Client";
            ConnectionStatusColor = "Gray";
            ProtocolInfo = "N/A";
            return;
        }
        
        // Get protocol info from settings
        var settings = GetNaviPacSettings();
        var protocol = settings?.NaviPacProtocol ?? NaviPacProtocol.UDP;
        var port = settings?.NaviPacPort ?? 0;
        var host = settings?.NaviPacHost ?? "localhost";
        
        ProtocolInfo = protocol == NaviPacProtocol.TCP 
            ? $"TCP Server on port {port}" 
            : $"UDP Listener on port {port}";
        
        if (_naviPacClient.IsConnected)
        {
            if (_naviPacClient.IsReceivingData)
            {
                ConnectionStatus = "Connected - Receiving Data";
                ConnectionStatusColor = "Green";
            }
            else
            {
                ConnectionStatus = "Connected - Waiting for Data";
                ConnectionStatusColor = "Orange";
            }
        }
        else
        {
            ConnectionStatus = "Disconnected";
            ConnectionStatusColor = "Red";
        }
    }
    
    /// <summary>
    /// Gets NaviPac settings (tries to access via reflection or direct reference).
    /// </summary>
    private ConnectionSettings? GetNaviPacSettings()
    {
        try
        {
            // Try to get from the client via reflection if needed
            var settingsField = _naviPacClient?.GetType()
                .GetField("_settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return settingsField?.GetValue(_naviPacClient) as ConnectionSettings;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Calculates the current message rate.
    /// </summary>
    private void CalculateMessageRate()
    {
        lock (_lockObject)
        {
            var cutoff = DateTime.Now.AddSeconds(-RATE_CALCULATION_WINDOW_SECONDS);
            
            // Remove old timestamps
            while (_messageTimestamps.Count > 0 && _messageTimestamps.Peek() < cutoff)
            {
                _messageTimestamps.Dequeue();
            }
            
            // Calculate rate
            MessagesPerSecond = _messageTimestamps.Count / (double)RATE_CALCULATION_WINDOW_SECONDS;
        }
    }
    
    /// <summary>
    /// Clears all messages.
    /// </summary>
    private void Clear()
    {
        lock (_lockObject)
        {
            RawMessages.Clear();
            ParsedFields.Clear();
            _messageTimestamps.Clear();
            SelectedMessage = null;
        }
    }
    
    /// <summary>
    /// Toggles pause state.
    /// </summary>
    private void TogglePause()
    {
        IsPaused = !IsPaused;
    }
    
    /// <summary>
    /// Exports the log to a file.
    /// </summary>
    private void ExportLog()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Data Monitor Log",
                Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"NaviPac_DataLog_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".txt"
            };
            
            if (dialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"NaviPac Data Monitor Export");
                sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Total Messages: {TotalMessages}");
                sb.AppendLine($"Total Bytes: {TotalBytesFormatted}");
                sb.AppendLine(new string('=', 80));
                sb.AppendLine();
                
                foreach (var msg in RawMessages)
                {
                    sb.AppendLine(msg.Display);
                }
                
                File.WriteAllText(dialog.FileName, sb.ToString());
                
                System.Windows.MessageBox.Show($"Log exported successfully to:\n{dialog.FileName}", 
                    "Export Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error exporting log: {ex.Message}", 
                "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Copies the last message to clipboard.
    /// </summary>
    private void CopyLastMessage()
    {
        if (RawMessages.Count > 0)
        {
            var lastMessage = RawMessages[RawMessages.Count - 1];
            System.Windows.Clipboard.SetText(lastMessage.Data);
        }
    }
    
    /// <summary>
    /// Copies the selected message to clipboard.
    /// </summary>
    private void CopySelectedMessage()
    {
        if (SelectedMessage != null)
        {
            System.Windows.Clipboard.SetText(SelectedMessage.Data);
        }
    }
    
    #endregion
    
    #region IDisposable
    
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        
        _updateTimer.Stop();
        
        if (_naviPacClient != null)
        {
            _naviPacClient.RawDataReceived -= NaviPacClient_RawDataReceived;
            _naviPacClient.ConnectionStatusChanged -= NaviPacClient_ConnectionStatusChanged;
        }
        
        GC.SuppressFinalize(this);
    }
    
    #endregion
}
