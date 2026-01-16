// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/FileMonitorService.cs
// Purpose: Service to monitor folders for new .npc, .wp2, and DVR files
// ============================================================================

using System.Collections.Concurrent;
using System.IO;
using FathomOS.Modules.SurveyLogbook.Models;
using FathomOS.Modules.SurveyLogbook.Parsers;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// Service that monitors file system folders for new survey-related files.
/// </summary>
public class FileMonitorService : IDisposable
{
    private readonly ConnectionSettings _settings;
    private readonly NpcFileParser _npcParser;
    private readonly WaypointFileParser _waypointParser;
    private readonly DvrFolderParser _dvrParser;
    
    private FileSystemWatcher? _npcWatcher;
    private FileSystemWatcher? _waypointWatcher;
    private FileSystemWatcher? _dvrWatcher;
    
    private readonly ConcurrentDictionary<string, DateTime> _processedFiles = new();
    private readonly ConcurrentDictionary<string, Waypoint[]> _waypointCache = new();
    private readonly TimeSpan _debounceTime = TimeSpan.FromMilliseconds(500);
    
    private bool _isRunning;
    private bool _disposed;
    
    public event EventHandler<PositionFixEventArgs>? PositionFixDetected;
    public event EventHandler<WaypointEventArgs>? WaypointChanged;
    public event EventHandler<DvrEventArgs>? DvrFileDetected;
    public event EventHandler<FileMonitorErrorEventArgs>? ErrorOccurred;
    public event EventHandler<MonitorStatusEventArgs>? StatusChanged;
    
    public FileMonitorService(ConnectionSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _npcParser = new NpcFileParser();
        _waypointParser = new WaypointFileParser();
        _dvrParser = new DvrFolderParser();
    }
    
    public bool IsRunning => _isRunning;
    
    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        
        try
        {
            if (_settings.EnableFixMonitoring && 
                !string.IsNullOrEmpty(_settings.FixOutputFolder) &&
                Directory.Exists(_settings.FixOutputFolder))
            {
                StartNpcMonitoring();
            }
            
            if (_settings.EnableWaypointMonitoring &&
                !string.IsNullOrEmpty(_settings.WaypointFolder) &&
                Directory.Exists(_settings.WaypointFolder))
            {
                StartWaypointMonitoring();
            }
            
            if (_settings.EnableDvrMonitoring &&
                !string.IsNullOrEmpty(_settings.VisualWorksProjectFolder) &&
                Directory.Exists(_settings.VisualWorksProjectFolder))
            {
                StartDvrMonitoring();
            }
            
            OnStatusChanged("File monitoring started");
        }
        catch (Exception ex)
        {
            OnError("Failed to start monitoring", ex);
        }
    }
    
    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        
        StopWatcher(ref _npcWatcher);
        StopWatcher(ref _waypointWatcher);
        StopWatcher(ref _dvrWatcher);
        
        OnStatusChanged("File monitoring stopped");
    }
    
    private void StartNpcMonitoring()
    {
        _npcWatcher = new FileSystemWatcher(_settings.FixOutputFolder!)
        {
            Filter = "*.npc",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = _settings.MonitorSubdirectories,
            EnableRaisingEvents = true
        };
        
        _npcWatcher.Created += OnNpcFileCreated;
        _npcWatcher.Error += OnWatcherError;
        
        OnStatusChanged($"Monitoring .npc files in: {_settings.FixOutputFolder}");
    }
    
    private void StartWaypointMonitoring()
    {
        // Cache existing waypoints
        CacheExistingWaypoints();
        
        _waypointWatcher = new FileSystemWatcher(_settings.WaypointFolder!)
        {
            Filter = "*.wp2",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            IncludeSubdirectories = _settings.MonitorSubdirectories,
            EnableRaisingEvents = true
        };
        
        _waypointWatcher.Changed += OnWaypointFileChanged;
        _waypointWatcher.Created += OnWaypointFileChanged;
        _waypointWatcher.Error += OnWatcherError;
        
        OnStatusChanged($"Monitoring .wp2 files in: {_settings.WaypointFolder}");
    }
    
    private void StartDvrMonitoring()
    {
        _dvrWatcher = new FileSystemWatcher(_settings.VisualWorksProjectFolder!)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = _settings.MonitorDvrSubdirectories,
            EnableRaisingEvents = true
        };
        
        _dvrWatcher.Created += OnDvrFileCreated;
        _dvrWatcher.Error += OnWatcherError;
        
        OnStatusChanged($"Monitoring DVR files in: {_settings.VisualWorksProjectFolder}");
    }
    
    private void OnNpcFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!ShouldProcessFile(e.FullPath)) return;
        
        Task.Run(async () =>
        {
            await Task.Delay(_debounceTime);
            ProcessNpcFile(e.FullPath);
        });
    }
    
    private void ProcessNpcFile(string filePath)
    {
        try
        {
            if (!WaitForFileAccess(filePath)) return;
            
            var fix = _npcParser.Parse(filePath);
            if (fix == null) return;
            
            var entryType = _npcParser.DetermineFixType(Path.GetFileName(filePath), fix);
            fix.Vehicle = _npcParser.ExtractVehicleName(fix.ObjectMonitored);
            
            PositionFixDetected?.Invoke(this, new PositionFixEventArgs
            {
                PositionFix = fix,
                FilePath = filePath,
                EntryType = entryType,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            OnError($"Error processing .npc file: {filePath}", ex);
        }
    }
    
    private void OnWaypointFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!ShouldProcessFile(e.FullPath)) return;
        
        Task.Run(async () =>
        {
            await Task.Delay(_debounceTime);
            ProcessWaypointFile(e.FullPath);
        });
    }
    
    private void ProcessWaypointFile(string filePath)
    {
        try
        {
            if (!WaitForFileAccess(filePath)) return;
            
            var newWaypoints = _waypointParser.Parse(filePath);
            if (newWaypoints.Count == 0) return;
            
            _waypointCache.TryGetValue(filePath, out var oldWaypoints);
            var diff = CompareWaypoints(oldWaypoints, newWaypoints.ToArray());
            
            _waypointCache[filePath] = newWaypoints.ToArray();
            
            if (diff.HasChanges)
            {
                WaypointChanged?.Invoke(this, new WaypointEventArgs
                {
                    FilePath = filePath,
                    Difference = diff,
                    AllWaypoints = newWaypoints,
                    Timestamp = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            OnError($"Error processing .wp2 file: {filePath}", ex);
        }
    }
    
    private void OnDvrFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!DvrFolderParser.IsVideoFile(e.FullPath) && 
            !DvrFolderParser.IsImageFile(e.FullPath)) return;
        
        if (!ShouldProcessFile(e.FullPath)) return;
        
        Task.Run(async () =>
        {
            await Task.Delay(_debounceTime);
            ProcessDvrFile(e.FullPath);
        });
    }
    
    private void ProcessDvrFile(string filePath)
    {
        try
        {
            var isVideo = DvrFolderParser.IsVideoFile(filePath);
            var isImage = DvrFolderParser.IsImageFile(filePath);
            
            var folderPath = Path.GetDirectoryName(filePath) ?? "";
            var recording = new DvrRecording
            {
                Id = Guid.NewGuid(),
                FolderPath = folderPath,
                Date = DateTime.Now.Date,
                StartTime = DateTime.Now.TimeOfDay
            };
            
            recording.AddVideoFile(filePath);
            recording.Vehicle = DetermineVehicleFromPath(folderPath);
            
            DvrFileDetected?.Invoke(this, new DvrEventArgs
            {
                FilePath = filePath,
                Recording = recording,
                IsVideo = isVideo,
                IsImage = isImage,
                EntryType = isVideo ? LogEntryType.DvrRecordingStart : LogEntryType.DvrImageCaptured,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            OnError($"Error processing DVR file: {filePath}", ex);
        }
    }
    
    private string DetermineVehicleFromPath(string folderPath)
    {
        if (_settings.VehicleFolderMappings != null)
        {
            foreach (var mapping in _settings.VehicleFolderMappings)
            {
                if (mapping.Matches(folderPath))
                    return mapping.VehicleName;
            }
        }
        return string.Empty;
    }
    
    private void CacheExistingWaypoints()
    {
        if (string.IsNullOrEmpty(_settings.WaypointFolder)) return;
        
        try
        {
            foreach (var file in Directory.GetFiles(_settings.WaypointFolder, "*.wp2", 
                _settings.MonitorSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
                var waypoints = _waypointParser.Parse(file);
                _waypointCache[file] = waypoints.ToArray();
            }
        }
        catch (Exception ex)
        {
            OnError("Error caching waypoints", ex);
        }
    }
    
    private WaypointDifference CompareWaypoints(Waypoint[]? oldWaypoints, Waypoint[] newWaypoints)
    {
        var diff = new WaypointDifference();
        if (oldWaypoints == null)
        {
            diff.Added.AddRange(newWaypoints);
            return diff;
        }
        
        foreach (var newWp in newWaypoints)
        {
            var match = oldWaypoints.FirstOrDefault(w => 
                w.Name == newWp.Name && 
                Math.Abs(w.Easting - newWp.Easting) < 0.001);
            if (match == null)
                diff.Added.Add(newWp);
        }
        
        foreach (var oldWp in oldWaypoints)
        {
            var match = newWaypoints.FirstOrDefault(w =>
                w.Name == oldWp.Name &&
                Math.Abs(w.Easting - oldWp.Easting) < 0.001);
            if (match == null)
                diff.Deleted.Add(oldWp);
        }
        
        return diff;
    }
    
    private bool ShouldProcessFile(string filePath)
    {
        var now = DateTime.Now;
        if (_processedFiles.TryGetValue(filePath, out var lastProcessed))
        {
            if (now - lastProcessed < _debounceTime)
                return false;
        }
        _processedFiles[filePath] = now;
        return true;
    }
    
    private bool WaitForFileAccess(string filePath, int maxAttempts = 10)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return true;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }
        }
        return false;
    }
    
    private void StopWatcher(ref FileSystemWatcher? watcher)
    {
        if (watcher == null) return;
        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
        watcher = null;
    }
    
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        OnError("FileSystemWatcher error", e.GetException());
    }
    
    private void OnError(string message, Exception? ex = null)
    {
        ErrorOccurred?.Invoke(this, new FileMonitorErrorEventArgs
        {
            Message = message,
            Exception = ex,
            Timestamp = DateTime.Now
        });
    }
    
    private void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, new MonitorStatusEventArgs
        {
            Status = status,
            Timestamp = DateTime.Now
        });
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        GC.SuppressFinalize(this);
    }
}

public class PositionFixEventArgs : EventArgs
{
    public PositionFix PositionFix { get; set; } = null!;
    public string FilePath { get; set; } = string.Empty;
    public LogEntryType EntryType { get; set; }
    public DateTime Timestamp { get; set; }
}

public class WaypointEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public WaypointDifference Difference { get; set; } = null!;
    public List<Waypoint> AllWaypoints { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class DvrEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public DvrRecording Recording { get; set; } = null!;
    public bool IsVideo { get; set; }
    public bool IsImage { get; set; }
    public LogEntryType EntryType { get; set; }
    public DateTime Timestamp { get; set; }
}

public class FileMonitorErrorEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MonitorStatusEventArgs : EventArgs
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
