// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/DvrRecording.cs
// Purpose: DVR recording session model - captures VisualWorks DVR data
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Represents a DVR recording session from VisualWorks.
/// Captures recording details including folder hierarchy and video files.
/// Based on the "DVR Reg" sheet structure from the survey log Excel file.
/// </summary>
public class DvrRecording : INotifyPropertyChanged
{
    private Guid _id;
    private DateTime _date;
    private TimeSpan _startTime;
    private TimeSpan _endTime;
    private string _vehicle = string.Empty;
    private string _folderPath = string.Empty;
    private string _projectTask = string.Empty;
    private string _subTask = string.Empty;
    private string _operation = string.Empty;
    private string _comment = string.Empty;
    private bool _isActive;
    
    // File-specific fields (for file browser mode)
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private double _fileSizeMB;
    
    // ========================================================================
    // Core Properties
    // ========================================================================
    
    /// <summary>
    /// Unique identifier for this recording session.
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    /// <summary>
    /// Date of the recording.
    /// </summary>
    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }
    
    /// <summary>
    /// Start time of the recording.
    /// </summary>
    public TimeSpan StartTime
    {
        get => _startTime;
        set
        {
            if (SetProperty(ref _startTime, value))
                OnPropertyChanged(nameof(Duration));
        }
    }
    
    /// <summary>
    /// End time of the recording.
    /// </summary>
    public TimeSpan EndTime
    {
        get => _endTime;
        set
        {
            if (SetProperty(ref _endTime, value))
                OnPropertyChanged(nameof(Duration));
        }
    }
    
    /// <summary>
    /// Vehicle or ROV identifier (e.g., "HD11", "HD12", "Ross Candies").
    /// </summary>
    public string Vehicle
    {
        get => _vehicle;
        set => SetProperty(ref _vehicle, value ?? string.Empty);
    }
    
    /// <summary>
    /// Full hierarchical folder path from VisualWorks.
    /// Example: "A.SFL_Type3_Flying_Leads_Installation\B.SFL11_SFL-1411-001\B.Landing_Laying_Operations\B.HD12"
    /// </summary>
    public string FolderPath
    {
        get => _folderPath;
        set
        {
            if (SetProperty(ref _folderPath, value ?? string.Empty))
            {
                ParseFolderPath();
            }
        }
    }
    
    /// <summary>
    /// Main project task (parsed from folder path).
    /// Example: "SFL Type3 Flying Leads Installation"
    /// </summary>
    public string ProjectTask
    {
        get => _projectTask;
        set => SetProperty(ref _projectTask, value ?? string.Empty);
    }
    
    /// <summary>
    /// Sub-task within the project (parsed from folder path).
    /// Example: "SFL11 SFL-1411-001"
    /// </summary>
    public string SubTask
    {
        get => _subTask;
        set => SetProperty(ref _subTask, value ?? string.Empty);
    }
    
    /// <summary>
    /// Operation type (parsed from folder path).
    /// Example: "Landing Laying Operations"
    /// </summary>
    public string Operation
    {
        get => _operation;
        set => SetProperty(ref _operation, value ?? string.Empty);
    }
    
    /// <summary>
    /// User comment or description.
    /// </summary>
    public string Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value ?? string.Empty);
    }
    
    /// <summary>
    /// Indicates if recording is currently active (in progress).
    /// </summary>
    [JsonIgnore]
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
    
    // ========================================================================
    // File Properties (for file browser mode)
    // ========================================================================
    
    /// <summary>
    /// Full path to the video file (for file browser mode).
    /// </summary>
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }
    
    /// <summary>
    /// File name without path (for file browser mode).
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }
    
    /// <summary>
    /// File size in megabytes (for file browser mode).
    /// </summary>
    public double FileSizeMB
    {
        get => _fileSizeMB;
        set => SetProperty(ref _fileSizeMB, value);
    }
    
    /// <summary>
    /// Gets the recording date (alias for Date property).
    /// </summary>
    [JsonIgnore]
    public DateTime RecordingDate
    {
        get => Date;
        set => Date = value;
    }
    
    // ========================================================================
    // Video Files
    // ========================================================================
    
    /// <summary>
    /// List of video files associated with this recording session.
    /// </summary>
    public List<string> VideoFiles { get; set; } = new();
    
    /// <summary>
    /// List of image/screenshot files associated with this recording session.
    /// </summary>
    public List<string> ImageFiles { get; set; } = new();
    
    /// <summary>
    /// Total file size of all video files (bytes).
    /// </summary>
    public long TotalFileSize { get; set; }
    
    /// <summary>
    /// Video format (e.g., "WMV", "MPEG-2", "H.264").
    /// </summary>
    public string? VideoFormat { get; set; }
    
    // ========================================================================
    // Computed Properties
    // ========================================================================
    
    /// <summary>
    /// Gets or sets the recording start timestamp.
    /// When set, updates Date and StartTime properties.
    /// </summary>
    [JsonIgnore]
    public DateTime StartDateTime 
    {
        get => Date.Add(StartTime);
        set
        {
            Date = value.Date;
            StartTime = value.TimeOfDay;
        }
    }
    
    /// <summary>
    /// Gets the recording start timestamp (alias for StartDateTime).
    /// </summary>
    [JsonIgnore]
    public DateTime StartTimestamp => Date.Add(StartTime);
    
    /// <summary>
    /// Gets the recording end timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime EndTimestamp => Date.Add(EndTime);
    
    /// <summary>
    /// Gets or sets the DVR channel name.
    /// </summary>
    public string Channel { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets a combined description for the recording.
    /// </summary>
    [JsonIgnore]
    public string Description => DescriptionDisplay;
    
    /// <summary>
    /// Gets the duration of the recording.
    /// </summary>
    [JsonIgnore]
    public TimeSpan Duration => EndTime > StartTime 
        ? EndTime - StartTime 
        : TimeSpan.FromDays(1) - StartTime + EndTime; // Handle midnight crossover
    
    /// <summary>
    /// Gets formatted start time string.
    /// </summary>
    [JsonIgnore]
    public string StartTimeDisplay => StartTime.ToString(@"hh\:mm");
    
    /// <summary>
    /// Gets formatted end time string.
    /// </summary>
    [JsonIgnore]
    public string EndTimeDisplay => EndTime.ToString(@"hh\:mm");
    
    /// <summary>
    /// Gets formatted date string.
    /// </summary>
    [JsonIgnore]
    public string DateDisplay => Date.ToString("dd/MM/yyyy");
    
    /// <summary>
    /// Gets formatted duration string.
    /// </summary>
    [JsonIgnore]
    public string DurationDisplay => Duration.ToString(@"hh\:mm\:ss");
    
    /// <summary>
    /// Gets the number of video files.
    /// </summary>
    [JsonIgnore]
    public int VideoFileCount => VideoFiles?.Count ?? 0;
    
    /// <summary>
    /// Gets formatted file size string.
    /// </summary>
    [JsonIgnore]
    public string FileSizeDisplay => FormatFileSize(TotalFileSize);
    
    /// <summary>
    /// Gets a combined description of the recording.
    /// </summary>
    [JsonIgnore]
    public string DescriptionDisplay
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(ProjectTask)) parts.Add(ProjectTask);
            if (!string.IsNullOrWhiteSpace(Operation)) parts.Add(Operation);
            if (!string.IsNullOrWhiteSpace(Comment)) parts.Add(Comment);
            return string.Join(" - ", parts);
        }
    }
    
    // ========================================================================
    // Constructors
    // ========================================================================
    
    /// <summary>
    /// Creates a new DVR recording with generated ID.
    /// </summary>
    public DvrRecording()
    {
        _id = Guid.NewGuid();
        _date = DateTime.Today;
    }
    
    /// <summary>
    /// Creates a DVR recording from folder detection.
    /// </summary>
    public DvrRecording(string vehicle, string folderPath, DateTime startTime)
        : this()
    {
        _vehicle = vehicle ?? string.Empty;
        _folderPath = folderPath ?? string.Empty;
        _date = startTime.Date;
        _startTime = startTime.TimeOfDay;
        _isActive = true;
        ParseFolderPath();
    }
    
    // ========================================================================
    // Methods
    // ========================================================================
    
    /// <summary>
    /// Parses the folder path to extract project task, sub-task, and operation.
    /// </summary>
    private void ParseFolderPath()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
            return;
        
        // Split folder path by backslash
        var parts = FolderPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Process each part - remove prefix (A., B., C., etc.) and convert underscores to spaces
        var cleanParts = parts
            .Select(CleanFolderName)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
        
        // Assign to properties based on position
        if (cleanParts.Count >= 1)
            _projectTask = cleanParts[0];
        if (cleanParts.Count >= 2)
            _subTask = cleanParts[1];
        if (cleanParts.Count >= 3)
            _operation = cleanParts[2];
        
        // If the last part looks like a vehicle name, use it
        var lastPart = cleanParts.LastOrDefault();
        if (!string.IsNullOrWhiteSpace(lastPart) && IsVehicleName(lastPart) && string.IsNullOrWhiteSpace(_vehicle))
        {
            _vehicle = lastPart;
        }
    }
    
    /// <summary>
    /// Cleans a folder name by removing prefixes and converting underscores.
    /// </summary>
    private static string CleanFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return string.Empty;
        
        // Remove letter prefix (A., B., C., etc.)
        var cleaned = folderName;
        if (cleaned.Length > 2 && char.IsLetter(cleaned[0]) && cleaned[1] == '.')
        {
            cleaned = cleaned.Substring(2);
        }
        
        // Replace underscores with spaces
        cleaned = cleaned.Replace('_', ' ');
        
        // Trim any leading/trailing whitespace
        return cleaned.Trim();
    }
    
    /// <summary>
    /// Checks if a folder name looks like a vehicle name.
    /// </summary>
    private static bool IsVehicleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        
        var upper = name.ToUpperInvariant();
        return upper.StartsWith("HD") || 
               upper.Contains("ROV") || 
               upper.Contains("ROSS") ||
               upper.Contains("CRANE") ||
               upper.Contains("VESSEL");
    }
    
    /// <summary>
    /// Adds a video file to the recording.
    /// </summary>
    public void AddVideoFile(string filePath, long fileSize = 0)
    {
        VideoFiles ??= new List<string>();
        if (!VideoFiles.Contains(filePath))
        {
            VideoFiles.Add(filePath);
            TotalFileSize += fileSize;
        }
    }
    
    /// <summary>
    /// Completes the recording session.
    /// </summary>
    public void Complete(DateTime endTime)
    {
        _endTime = endTime.TimeOfDay;
        _isActive = false;
        OnPropertyChanged(nameof(EndTime));
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(IsActive));
    }
    
    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
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
    
    public override string ToString() => $"DVR [{Vehicle}] {DateDisplay} {StartTimeDisplay}-{EndTimeDisplay}: {DescriptionDisplay}";
    
    // ========================================================================
    // Clone Method
    // ========================================================================
    
    /// <summary>
    /// Creates a shallow copy of this DvrRecording.
    /// </summary>
    /// <returns>A new DvrRecording with copied values.</returns>
    public DvrRecording Clone()
    {
        var clone = (DvrRecording)MemberwiseClone();
        clone._id = Guid.NewGuid();
        return clone;
    }
}
