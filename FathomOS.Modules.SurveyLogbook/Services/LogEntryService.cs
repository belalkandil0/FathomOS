// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/LogEntryService.cs
// Purpose: Central service for managing survey log entries
// ============================================================================

using System.Collections.ObjectModel;
using System.IO;
using FathomOS.Modules.SurveyLogbook.Models;
using FathomOS.Modules.SurveyLogbook.Parsers;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// Central service for managing survey log entries from all sources.
/// </summary>
public class LogEntryService : IDisposable
{
    private readonly ConnectionSettings _settings;
    private readonly FileMonitorService _fileMonitor;
    private readonly NaviPacClient? _naviPacClient;
    private readonly SlogFileHandler _slogHandler;
    private NaviPacDataParser? _dataParser;
    
    private readonly ObservableCollection<SurveyLogEntry> _entries = new();
    private readonly List<PositionFix> _positionFixes = new();
    private readonly List<DvrRecording> _dvrRecordings = new();
    private readonly List<EivaDataLog> _eivaDataLogs = new();
    private readonly List<DprReport> _dprReports = new();
    
    private readonly object _lock = new();
    private bool _disposed;
    private DateTime? _sessionStart;
    
    /// <summary>
    /// Field configuration for dynamic column generation.
    /// </summary>
    private List<UserFieldDefinition>? _fieldConfiguration;
    
    public event EventHandler<LogEntryAddedEventArgs>? EntryAdded;
    public event EventHandler<ServiceStatusEventArgs>? StatusChanged;
    public event EventHandler? FieldConfigurationChanged;
    
    public LogEntryService(ConnectionSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _slogHandler = new SlogFileHandler();
        
        _fileMonitor = new FileMonitorService(_settings);
        _fileMonitor.PositionFixDetected += OnPositionFixDetected;
        _fileMonitor.WaypointChanged += OnWaypointChanged;
        _fileMonitor.DvrFileDetected += OnDvrFileDetected;
        _fileMonitor.StatusChanged += OnFileMonitorStatus;
        
        // Initialize unified NaviPac client (supports both TCP and UDP)
        _naviPacClient = new NaviPacClient(_settings);
        _naviPacClient.EventReceived += OnNaviPacEvent;
        _naviPacClient.LoggingStatusChanged += OnLoggingStatusChanged;
        _naviPacClient.ConnectionStatusChanged += OnNaviPacConnectionChanged;
        _naviPacClient.DataReceived += OnNaviPacDataReceived;
        _naviPacClient.ErrorOccurred += OnNaviPacError;
    }
    
    public ObservableCollection<SurveyLogEntry> Entries => _entries;
    public IReadOnlyList<PositionFix> PositionFixes => _positionFixes.AsReadOnly();
    public IReadOnlyList<DvrRecording> DvrRecordings => _dvrRecordings.AsReadOnly();
    public IReadOnlyList<EivaDataLog> EivaDataLogs => _eivaDataLogs.AsReadOnly();
    public IReadOnlyList<DprReport> DprReports => _dprReports.AsReadOnly();
    public bool IsRunning { get; private set; }
    
    /// <summary>
    /// Gets the current connection protocol being used.
    /// </summary>
    public NaviPacProtocol CurrentProtocol => _settings.NaviPacProtocol;
    
    /// <summary>
    /// Gets the NaviPacClient instance for monitoring/debugging purposes.
    /// </summary>
    public NaviPacClient? NaviPacClient => _naviPacClient;
    
    /// <summary>
    /// Gets or sets the field configuration for dynamic columns.
    /// </summary>
    public List<UserFieldDefinition>? FieldConfiguration
    {
        get => _fieldConfiguration;
        set
        {
            _fieldConfiguration = value;
            UpdateDataParser();
            FieldConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    
    /// <summary>
    /// Gets the source label for NaviPac entries based on protocol.
    /// </summary>
    private string NaviPacSource => _settings.NaviPacProtocol == NaviPacProtocol.TCP 
        ? "NaviPac TCP" 
        : "NaviPac UDP";
    
    /// <summary>
    /// Updates the data parser with the current field configuration.
    /// </summary>
    private void UpdateDataParser()
    {
        if (_fieldConfiguration != null && _fieldConfiguration.Count > 0)
        {
            _dataParser = new NaviPacDataParser(
                _fieldConfiguration,
                _settings.NaviPacSeparator.ToString(),
                NaviPacSource);
        }
        else
        {
            _dataParser = NaviPacDataParser.CreateDefault();
        }
    }
    
    public async Task StartAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        _sessionStart = DateTime.Now;
        
        // Initialize data parser if not already done
        if (_dataParser == null)
        {
            UpdateDataParser();
        }
        
        _fileMonitor.Start();
        
        if (_settings.EnableNaviPacConnection)
        {
            await _naviPacClient!.ConnectAsync();
        }
        
        AddSystemEntry("Session started");
        OnStatusChanged("Service started");
    }
    
    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        
        _fileMonitor.Stop();
        _naviPacClient?.Disconnect();
        
        AddSystemEntry("Session ended");
        OnStatusChanged("Service stopped");
    }
    
    public void AddManualEntry(string description, string? vehicle = null, string? comments = null)
    {
        var entry = SurveyLogEntry.CreateManualEntry(description, vehicle ?? "", comments ?? "");
        AddEntry(entry);
    }
    
    public void AddEntry(SurveyLogEntry entry)
    {
        lock (_lock)
        {
            _entries.Insert(0, entry); // Most recent first
        }
        EntryAdded?.Invoke(this, new LogEntryAddedEventArgs { Entry = entry });
    }
    
    private void AddSystemEntry(string description)
    {
        var entry = new SurveyLogEntry
        {
            EntryType = LogEntryType.SystemEvent,
            Source = "System",
            Description = description
        };
        AddEntry(entry);
    }
    
    private void OnPositionFixDetected(object? sender, PositionFixEventArgs e)
    {
        lock (_lock)
        {
            _positionFixes.Add(e.PositionFix);
        }
        
        var entry = SurveyLogEntry.CreatePositionFixEntry(
            e.PositionFix, 
            e.EntryType,
            Path.GetFileName(e.FilePath));
        AddEntry(entry);
    }
    
    private void OnWaypointChanged(object? sender, WaypointEventArgs e)
    {
        foreach (var wp in e.Difference.Added)
        {
            var entry = new SurveyLogEntry
            {
                EntryType = LogEntryType.WaypointAdded,
                Source = Path.GetFileName(e.FilePath),
                Description = $"Waypoint added: {wp.Name} ({wp.CoordinateDisplay})"
            };
            AddEntry(entry);
        }
        
        foreach (var wp in e.Difference.Deleted)
        {
            var entry = new SurveyLogEntry
            {
                EntryType = LogEntryType.WaypointDeleted,
                Source = Path.GetFileName(e.FilePath),
                Description = $"Waypoint deleted: {wp.Name}"
            };
            AddEntry(entry);
        }
    }
    
    private void OnDvrFileDetected(object? sender, DvrEventArgs e)
    {
        if (e.IsVideo)
        {
            lock (_lock)
            {
                var existing = _dvrRecordings.FirstOrDefault(r => r.FolderPath == e.Recording.FolderPath);
                if (existing != null)
                {
                    existing.AddVideoFile(e.FilePath);
                }
                else
                {
                    _dvrRecordings.Add(e.Recording);
                }
            }
        }
        
        var entry = SurveyLogEntry.CreateDvrEntry(
            e.EntryType,
            e.Recording.Vehicle,
            Path.GetFileName(e.FilePath),
            e.Recording.FolderPath);
        AddEntry(entry);
    }
    
    private void OnNaviPacEvent(object? sender, NaviPacEventArgs e)
    {
        var entry = new SurveyLogEntry
        {
            EntryType = LogEntryType.NaviPacEvent,
            Source = NaviPacSource,
            Description = $"Event {e.EventNumber}: {e.EventText}"
        };
        entry.SetMetadata("EventNumber", e.EventNumber);
        AddEntry(entry);
    }
    
    private void OnLoggingStatusChanged(object? sender, LoggingStatusEventArgs e)
    {
        LogEntryType entryType;
        string description;
        
        switch (e.System.ToUpperInvariant())
        {
            case "NAVIPAC":
                entryType = e.IsLogging ? LogEntryType.NaviPacLoggingStart : LogEntryType.NaviPacLoggingEnd;
                break;
            case "NAVISCAN":
                entryType = e.IsLogging ? LogEntryType.NaviScanLoggingStart : LogEntryType.NaviScanLoggingEnd;
                break;
            default:
                entryType = e.IsLogging ? LogEntryType.NaviPacLoggingStart : LogEntryType.NaviPacLoggingEnd;
                break;
        }
        
        description = e.IsLogging 
            ? $"{e.System} logging started" + (string.IsNullOrEmpty(e.RunlineName) ? "" : $": {e.RunlineName}")
            : $"{e.System} logging stopped";
        
        var entry = new SurveyLogEntry
        {
            EntryType = entryType,
            Source = NaviPacSource,
            Description = description
        };
        
        if (!string.IsNullOrEmpty(e.RunlineName))
            entry.SetMetadata("Runline", e.RunlineName);
        
        AddEntry(entry);
        
        // Track EIVA data logs
        if (e.IsLogging)
        {
            var dataLog = new EivaDataLog
            {
                Date = DateTime.Now.Date,
                StartTime = DateTime.Now.TimeOfDay,
                Runline = e.RunlineName
            };
            
            if (e.System.Equals("NaviPac", StringComparison.OrdinalIgnoreCase))
                dataLog.NaviPacStartFile = $"NaviPac_{DateTime.Now:yyyyMMdd_HHmmss}";
            else if (e.System.Equals("NaviScan", StringComparison.OrdinalIgnoreCase))
                dataLog.NaviScanStartFile = $"NaviScan_{DateTime.Now:yyyyMMdd_HHmmss}";
            
            lock (_lock)
            {
                _eivaDataLogs.Add(dataLog);
            }
        }
    }
    
    private void OnNaviPacConnectionChanged(object? sender, ConnectionStatusEventArgs e)
    {
        OnStatusChanged($"{NaviPacSource}: {e.Message}");
    }
    
    private void OnNaviPacDataReceived(object? sender, NaviPacDataEventArgs e)
    {
        // Use the data parser to create a log entry with dynamic fields
        if (_dataParser != null && !string.IsNullOrWhiteSpace(e.Data.RawLine))
        {
            var entry = _dataParser.Parse(e.Data.RawLine);
            if (entry != null)
            {
                // Set entry type based on message type
                entry.EntryType = e.Data.EventNumber.HasValue 
                    ? LogEntryType.NaviPacEvent 
                    : LogEntryType.PositionUpdate;
                
                // Only add to log if it's an event or other significant entry
                // Position updates are high-frequency and would flood the log
                if (entry.EntryType == LogEntryType.NaviPacEvent)
                {
                    AddEntry(entry);
                }
            }
        }
        
        // Debug logging for position data
        System.Diagnostics.Debug.WriteLine($"[NaviPac] Position: E={e.Data.Easting:F3}, N={e.Data.Northing:F3}");
    }
    
    /// <summary>
    /// Creates a log entry from raw NaviPac data using the current field configuration.
    /// </summary>
    public SurveyLogEntry? ParseNaviPacData(string rawData)
    {
        return _dataParser?.Parse(rawData);
    }
    
    /// <summary>
    /// Creates a log entry from NaviPacData using the current field configuration.
    /// </summary>
    public SurveyLogEntry CreateEntryFromNaviPacData(NaviPacData data)
    {
        var entry = new SurveyLogEntry
        {
            EntryType = data.EventNumber.HasValue ? LogEntryType.NaviPacEvent : LogEntryType.PositionUpdate,
            Source = NaviPacSource,
            Timestamp = data.GetTimestamp()
        };

        // Set standard properties
        if (data.Easting.HasValue) entry.Easting = data.Easting.Value;
        if (data.Northing.HasValue) entry.Northing = data.Northing.Value;
        if (data.KP.HasValue) entry.Kp = data.KP.Value;
        if (data.DCC.HasValue) entry.Dcc = data.DCC.Value;
        if (data.Latitude.HasValue) entry.Latitude = data.Latitude.Value;
        if (data.Longitude.HasValue) entry.Longitude = data.Longitude.Value;
        if (data.Height.HasValue) entry.Height = data.Height.Value;
        if (data.Heading.HasValue) entry.Heading = data.Heading.Value;
        if (data.Roll.HasValue) entry.Roll = data.Roll.Value;
        if (data.Pitch.HasValue) entry.Pitch = data.Pitch.Value;
        if (data.Heave.HasValue) entry.Heave = data.Heave.Value;
        if (data.EventNumber.HasValue) entry.EventNumber = data.EventNumber.Value;

        // Generate description
        entry.Description = _dataParser?.GenerateDescription(data.RawLine ?? "")
            ?? $"Position update at {entry.Timestamp:HH:mm:ss}";

        return entry;
    }
    
    private void OnNaviPacError(object? sender, NaviPacErrorEventArgs e)
    {
        OnStatusChanged($"{NaviPacSource} Error: {e.Message}");
        System.Diagnostics.Debug.WriteLine($"[NaviPac Error] {e.Message}: {e.Exception?.Message}");
    }
    
    private void OnFileMonitorStatus(object? sender, MonitorStatusEventArgs e)
    {
        OnStatusChanged($"File Monitor: {e.Status}");
    }
    
    public SurveyLogFile CreateExportFile(string? exportedBy = null)
    {
        lock (_lock)
        {
            var logFile = new SurveyLogFile
            {
                ExportedBy = exportedBy ?? string.Empty,
                ProjectInfo = _settings.ProjectInfo?.Clone() ?? new ProjectInfo()
            };
            
            logFile.SurveyLogData.DvrRecordings.AddRange(_dvrRecordings.Select(r => r.Clone()));
            logFile.SurveyLogData.PositionFixes.AddRange(_positionFixes.Select(p => p.Clone()));
            logFile.SurveyLogData.EivaDataLogs.AddRange(_eivaDataLogs.Select(e => e.Clone()));
            logFile.SurveyLogData.ManualEntries.AddRange(
                _entries.Where(e => e.EntryType == LogEntryType.ManualEntry)
                        .Select(e => e.Clone()));
            
            logFile.DprReports.AddRange(_dprReports.Select(d => d.Clone()));
            logFile.UpdateMetadata();
            
            return logFile;
        }
    }
    
    public bool SaveToFile(string filePath, bool compress = false, string? exportedBy = null)
    {
        var logFile = CreateExportFile(exportedBy);
        return _slogHandler.Save(filePath, logFile, compress);
    }
    
    public bool LoadFromFile(string filePath)
    {
        var logFile = _slogHandler.Load(filePath);
        if (logFile == null) return false;
        
        lock (_lock)
        {
            _dvrRecordings.Clear();
            _positionFixes.Clear();
            _eivaDataLogs.Clear();
            _dprReports.Clear();
            _entries.Clear();
            
            if (logFile.SurveyLogData != null)
            {
                _dvrRecordings.AddRange(logFile.SurveyLogData.DvrRecordings);
                _positionFixes.AddRange(logFile.SurveyLogData.PositionFixes);
                _eivaDataLogs.AddRange(logFile.SurveyLogData.EivaDataLogs);
                
                // Recreate log entries
                foreach (var fix in _positionFixes)
                {
                    var entry = SurveyLogEntry.CreatePositionFixEntry(fix, LogEntryType.PositionFix, fix.SourceFile);
                    _entries.Add(entry);
                }
                
                foreach (var dvr in _dvrRecordings)
                {
                    var entry = SurveyLogEntry.CreateDvrEntry(LogEntryType.DvrRecordingStart, dvr.Vehicle, 
                        dvr.DescriptionDisplay, dvr.FolderPath);
                    _entries.Add(entry);
                }
                
                foreach (var manualEntry in logFile.SurveyLogData.ManualEntries)
                {
                    _entries.Add(manualEntry);
                }
            }
            
            if (logFile.DprReports != null)
            {
                _dprReports.AddRange(logFile.DprReports);
            }
        }
        
        return true;
    }
    
    public void AddDprReport(DprReport report)
    {
        lock (_lock)
        {
            _dprReports.Add(report);
        }
    }
    
    /// <summary>
    /// Adds a position fix to the collection.
    /// </summary>
    public void AddPositionFix(PositionFix fix)
    {
        lock (_lock)
        {
            _positionFixes.Add(fix);
        }
    }
    
    public IEnumerable<SurveyLogEntry> GetEntriesByType(LogEntryType type)
    {
        return _entries.Where(e => e.EntryType == type);
    }
    
    public IEnumerable<SurveyLogEntry> GetEntriesInDateRange(DateTime start, DateTime end)
    {
        return _entries.Where(e => e.Timestamp >= start && e.Timestamp <= end);
    }
    
    public void ClearAll()
    {
        lock (_lock)
        {
            _entries.Clear();
            _positionFixes.Clear();
            _dvrRecordings.Clear();
            _eivaDataLogs.Clear();
            _dprReports.Clear();
        }
    }
    
    private void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, new ServiceStatusEventArgs { Status = status });
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _fileMonitor.Dispose();
        _naviPacClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class LogEntryAddedEventArgs : EventArgs
{
    public SurveyLogEntry Entry { get; set; } = null!;
}

public class LogEntryRemovedEventArgs : EventArgs
{
    public SurveyLogEntry Entry { get; set; } = null!;
}

public class ServiceStatusEventArgs : EventArgs
{
    public string Status { get; set; } = string.Empty;
}
