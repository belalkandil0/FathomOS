// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/EivaDataLog.cs
// Purpose: EIVA NaviPac/NaviScan data log model - captures logging session info
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Represents an EIVA NaviPac/NaviScan logging session.
/// Captures file names, runlines, and KP information.
/// Based on the "EIVA Data Log" sheet structure from the survey log Excel file.
/// </summary>
public class EivaDataLog : INotifyPropertyChanged
{
    private Guid _id;
    private string _naviPacStartFile = string.Empty;
    private string _naviScanStartFile = string.Empty;
    private string _rovinsFile = string.Empty;
    private string _runline = string.Empty;
    private DateTime _date;
    private string _vehicle = string.Empty;
    private TimeSpan _startTime;
    private TimeSpan _endTime;
    private double? _startKp;
    private double? _endKp;
    private string _comments = string.Empty;
    private bool _isActive;
    
    // ========================================================================
    // Core Properties
    // ========================================================================
    
    /// <summary>
    /// Unique identifier for this data log entry.
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    /// <summary>
    /// NaviPac start file name.
    /// </summary>
    public string NaviPacStartFile
    {
        get => _naviPacStartFile;
        set => SetProperty(ref _naviPacStartFile, value ?? string.Empty);
    }
    
    /// <summary>
    /// NaviScan start file name.
    /// </summary>
    public string NaviScanStartFile
    {
        get => _naviScanStartFile;
        set => SetProperty(ref _naviScanStartFile, value ?? string.Empty);
    }
    
    /// <summary>
    /// ROVINS file name.
    /// </summary>
    public string RovinsFile
    {
        get => _rovinsFile;
        set => SetProperty(ref _rovinsFile, value ?? string.Empty);
    }
    
    /// <summary>
    /// Runline name or identifier.
    /// </summary>
    public string Runline
    {
        get => _runline;
        set => SetProperty(ref _runline, value ?? string.Empty);
    }
    
    /// <summary>
    /// Date of the logging session.
    /// </summary>
    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }
    
    /// <summary>
    /// Vehicle or ROV identifier.
    /// </summary>
    public string Vehicle
    {
        get => _vehicle;
        set => SetProperty(ref _vehicle, value ?? string.Empty);
    }
    
    /// <summary>
    /// Start time of the logging session.
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
    /// End time of the logging session.
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
    /// Start KP (Kilometre Post) value.
    /// </summary>
    public double? StartKp
    {
        get => _startKp;
        set
        {
            if (SetProperty(ref _startKp, value))
                OnPropertyChanged(nameof(KpRange));
        }
    }
    
    /// <summary>
    /// End KP (Kilometre Post) value.
    /// </summary>
    public double? EndKp
    {
        get => _endKp;
        set
        {
            if (SetProperty(ref _endKp, value))
                OnPropertyChanged(nameof(KpRange));
        }
    }
    
    /// <summary>
    /// Comments or notes about the session.
    /// </summary>
    public string Comments
    {
        get => _comments;
        set => SetProperty(ref _comments, value ?? string.Empty);
    }
    
    /// <summary>
    /// Indicates if the logging session is currently active.
    /// </summary>
    [JsonIgnore]
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
    
    // ========================================================================
    // Session Type Flags
    // ========================================================================
    
    /// <summary>
    /// Indicates if NaviPac was logging during this session.
    /// </summary>
    [JsonIgnore]
    public bool HasNaviPac => !string.IsNullOrWhiteSpace(NaviPacStartFile);
    
    /// <summary>
    /// Indicates if NaviScan was logging during this session.
    /// </summary>
    [JsonIgnore]
    public bool HasNaviScan => !string.IsNullOrWhiteSpace(NaviScanStartFile);
    
    /// <summary>
    /// Indicates if ROVINS was logging during this session.
    /// </summary>
    [JsonIgnore]
    public bool HasRovins => !string.IsNullOrWhiteSpace(RovinsFile);
    
    // ========================================================================
    // Computed Properties
    // ========================================================================
    
    /// <summary>
    /// Gets the session start timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime StartTimestamp => Date.Add(StartTime);
    
    /// <summary>
    /// Gets the session start timestamp (alias for StartTimestamp).
    /// </summary>
    [JsonIgnore]
    public DateTime StartDateTime => StartTimestamp;
    
    /// <summary>
    /// Gets the primary file name for this session (NaviPac file preferred).
    /// </summary>
    [JsonIgnore]
    public string FileName => !string.IsNullOrWhiteSpace(NaviPacStartFile) 
        ? NaviPacStartFile 
        : !string.IsNullOrWhiteSpace(NaviScanStartFile) 
            ? NaviScanStartFile 
            : RovinsFile;
    
    /// <summary>
    /// Gets the session end timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime EndTimestamp => Date.Add(EndTime);
    
    /// <summary>
    /// Gets the duration of the logging session.
    /// </summary>
    [JsonIgnore]
    public TimeSpan Duration => EndTime > StartTime 
        ? EndTime - StartTime 
        : TimeSpan.FromDays(1) - StartTime + EndTime; // Handle midnight crossover
    
    /// <summary>
    /// Gets the KP range covered.
    /// </summary>
    [JsonIgnore]
    public double? KpRange => (StartKp.HasValue && EndKp.HasValue)
        ? Math.Abs(EndKp.Value - StartKp.Value)
        : null;
    
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
    /// Gets formatted KP range string.
    /// </summary>
    [JsonIgnore]
    public string KpRangeDisplay => (StartKp.HasValue && EndKp.HasValue)
        ? $"{StartKp.Value:F3} - {EndKp.Value:F3}"
        : "-";
    
    /// <summary>
    /// Gets a summary of active logging systems.
    /// </summary>
    [JsonIgnore]
    public string SystemsDisplay
    {
        get
        {
            var systems = new List<string>();
            if (HasNaviPac) systems.Add("NaviPac");
            if (HasNaviScan) systems.Add("NaviScan");
            if (HasRovins) systems.Add("ROVINS");
            return string.Join(", ", systems);
        }
    }
    
    // ========================================================================
    // Constructors
    // ========================================================================
    
    /// <summary>
    /// Creates a new EIVA data log with generated ID.
    /// </summary>
    public EivaDataLog()
    {
        _id = Guid.NewGuid();
        _date = DateTime.Today;
    }
    
    /// <summary>
    /// Creates an EIVA data log for a new logging session.
    /// </summary>
    public EivaDataLog(string vehicle, string runline, DateTime startTime, string naviPacFile = "")
        : this()
    {
        _vehicle = vehicle ?? string.Empty;
        _runline = runline ?? string.Empty;
        _date = startTime.Date;
        _startTime = startTime.TimeOfDay;
        _naviPacStartFile = naviPacFile ?? string.Empty;
        _isActive = true;
    }
    
    // ========================================================================
    // Methods
    // ========================================================================
    
    /// <summary>
    /// Completes the logging session.
    /// </summary>
    public void Complete(DateTime endTime, double? endKp = null)
    {
        _endTime = endTime.TimeOfDay;
        _endKp = endKp;
        _isActive = false;
        OnPropertyChanged(nameof(EndTime));
        OnPropertyChanged(nameof(EndKp));
        OnPropertyChanged(nameof(Duration));
        OnPropertyChanged(nameof(KpRange));
        OnPropertyChanged(nameof(IsActive));
    }
    
    /// <summary>
    /// Sets the NaviScan file for this session.
    /// </summary>
    public void SetNaviScanFile(string fileName)
    {
        NaviScanStartFile = fileName;
    }
    
    /// <summary>
    /// Sets the ROVINS file for this session.
    /// </summary>
    public void SetRovinsFile(string fileName)
    {
        RovinsFile = fileName;
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
    
    public override string ToString() => $"EIVA [{Vehicle}] {DateDisplay} {StartTimeDisplay}-{EndTimeDisplay}: {Runline}";
    
    // ========================================================================
    // Clone Method
    // ========================================================================
    
    /// <summary>
    /// Creates a shallow copy of this EivaDataLog.
    /// </summary>
    /// <returns>A new EivaDataLog with copied values.</returns>
    public EivaDataLog Clone()
    {
        var clone = (EivaDataLog)MemberwiseClone();
        clone._id = Guid.NewGuid();
        return clone;
    }
}
