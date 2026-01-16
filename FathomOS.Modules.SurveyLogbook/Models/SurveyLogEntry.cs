// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/SurveyLogEntry.cs
// Purpose: Core survey log entry model - represents a single log record
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Represents a single survey log entry. This is the core data model for all
/// log records regardless of their source (DVR, NaviPac, Position Fix, Manual).
/// </summary>
public class SurveyLogEntry : INotifyPropertyChanged
{
    private Guid _id;
    private DateTime _timestamp;
    private LogEntryType _entryType;
    private string _source = string.Empty;
    private string _description = string.Empty;
    private string _vehicle = string.Empty;
    private string _comments = string.Empty;
    private bool _isSelected;
    private bool _isHighlighted;
    private bool _isExcluded;
    
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }
    
    /// <summary>
    /// Type of log entry (DVR, Position Fix, NaviPac, Manual, etc.).
    /// </summary>
    public LogEntryType EntryType
    {
        get => _entryType;
        set
        {
            if (SetProperty(ref _entryType, value))
            {
                OnPropertyChanged(nameof(EntryTypeDisplay));
                OnPropertyChanged(nameof(Category));
            }
        }
    }
    
    /// <summary>
    /// Source of the log entry (e.g., "NaviPac TCP", "File Monitor", "Manual").
    /// </summary>
    public string Source
    {
        get => _source;
        set => SetProperty(ref _source, value ?? string.Empty);
    }
    
    /// <summary>
    /// Description of the log entry/event.
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value ?? string.Empty);
    }
    
    /// <summary>
    /// Vehicle or object associated with this entry (e.g., "HD11", "HD12", "Ross Candies").
    /// </summary>
    public string Vehicle
    {
        get => _vehicle;
        set => SetProperty(ref _vehicle, value ?? string.Empty);
    }
    
    /// <summary>
    /// Additional comments or notes.
    /// </summary>
    public string Comments
    {
        get => _comments;
        set => SetProperty(ref _comments, value ?? string.Empty);
    }
    
    // ========================================================================
    // Position/Survey Data Properties
    // ========================================================================
    
    private double? _easting;
    private double? _northing;
    private double? _kp;
    private double? _dcc;
    private double? _depth;
    private double? _heading;
    private string? _fixType;
    private string? _objectName;
    
    /// <summary>
    /// Easting coordinate (grid X).
    /// </summary>
    public double? Easting
    {
        get => _easting;
        set => SetProperty(ref _easting, value);
    }
    
    /// <summary>
    /// Northing coordinate (grid Y).
    /// </summary>
    public double? Northing
    {
        get => _northing;
        set => SetProperty(ref _northing, value);
    }
    
    /// <summary>
    /// Kilometre Post (chainage along route).
    /// </summary>
    public double? Kp
    {
        get => _kp;
        set => SetProperty(ref _kp, value);
    }
    
    /// <summary>
    /// Distance Cross Course (offset from centerline).
    /// </summary>
    public double? Dcc
    {
        get => _dcc;
        set => SetProperty(ref _dcc, value);
    }
    
    /// <summary>
    /// Depth value.
    /// </summary>
    public double? Depth
    {
        get => _depth;
        set => SetProperty(ref _depth, value);
    }
    
    /// <summary>
    /// Heading in degrees.
    /// </summary>
    public double? Heading
    {
        get => _heading;
        set => SetProperty(ref _heading, value);
    }
    
    /// <summary>
    /// Type of position fix (e.g., "Calibration Fix", "Set Easting/Northing").
    /// </summary>
    public string? FixType
    {
        get => _fixType;
        set => SetProperty(ref _fixType, value);
    }
    
    /// <summary>
    /// Name of the object being positioned.
    /// </summary>
    public string? ObjectName
    {
        get => _objectName;
        set => SetProperty(ref _objectName, value);
    }
    
    // ========================================================================
    // Extended Navigation Properties (v9.0.0 Dynamic Columns)
    // ========================================================================
    
    private double? _latitude;
    private double? _longitude;
    private double? _height;
    private double? _roll;
    private double? _pitch;
    private double? _heave;
    private int? _eventNumber;
    private double? _dol;
    private double? _dal;
    private double? _smg;
    private double? _cmg;
    private double? _age;
    
    /// <summary>
    /// Latitude in decimal degrees (WGS84).
    /// </summary>
    public double? Latitude
    {
        get => _latitude;
        set => SetProperty(ref _latitude, value);
    }
    
    /// <summary>
    /// Longitude in decimal degrees (WGS84).
    /// </summary>
    public double? Longitude
    {
        get => _longitude;
        set => SetProperty(ref _longitude, value);
    }
    
    /// <summary>
    /// Height or vertical coordinate.
    /// </summary>
    public double? Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }
    
    /// <summary>
    /// Roll angle in degrees.
    /// </summary>
    public double? Roll
    {
        get => _roll;
        set => SetProperty(ref _roll, value);
    }
    
    /// <summary>
    /// Pitch angle in degrees.
    /// </summary>
    public double? Pitch
    {
        get => _pitch;
        set => SetProperty(ref _pitch, value);
    }
    
    /// <summary>
    /// Heave in meters.
    /// </summary>
    public double? Heave
    {
        get => _heave;
        set => SetProperty(ref _heave, value);
    }
    
    /// <summary>
    /// NaviPac Event Number (manual or automated event marker).
    /// </summary>
    public int? EventNumber
    {
        get => _eventNumber;
        set => SetProperty(ref _eventNumber, value);
    }
    
    /// <summary>
    /// Distance Off Line (perpendicular distance from route).
    /// </summary>
    public double? DOL
    {
        get => _dol;
        set => SetProperty(ref _dol, value);
    }
    
    /// <summary>
    /// Distance Along Line (same as KP but in project units).
    /// </summary>
    public double? DAL
    {
        get => _dal;
        set => SetProperty(ref _dal, value);
    }
    
    /// <summary>
    /// Speed Made Good in knots.
    /// </summary>
    public double? SMG
    {
        get => _smg;
        set => SetProperty(ref _smg, value);
    }
    
    /// <summary>
    /// Course Made Good in degrees.
    /// </summary>
    public double? CMG
    {
        get => _cmg;
        set => SetProperty(ref _cmg, value);
    }
    
    /// <summary>
    /// Position age in seconds (time since last update).
    /// </summary>
    public double? Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }
    
    /// <summary>
    /// Extended metadata stored as key-value pairs.
    /// Used for type-specific additional data.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Metadata { get; set; }
    
    // ========================================================================
    // Dynamic NaviPac Field Data (v9.0.0)
    // ========================================================================
    
    /// <summary>
    /// Stores dynamically configured field values from NaviPac UDO.
    /// Keys are field names from UserFieldDefinition, values are parsed data.
    /// </summary>
    public Dictionary<string, object?> DynamicFields { get; set; } = new();
    
    /// <summary>
    /// Gets a dynamic field value by name.
    /// </summary>
    /// <typeparam name="T">Expected type of the value</typeparam>
    /// <param name="fieldName">Name of the field</param>
    /// <param name="defaultValue">Default value if field not found or conversion fails</param>
    /// <returns>The field value or default</returns>
    public T? GetDynamicField<T>(string fieldName, T? defaultValue = default)
    {
        if (DynamicFields == null || !DynamicFields.TryGetValue(fieldName, out var value) || value == null)
            return defaultValue;
        
        try
        {
            if (value is T typedValue)
                return typedValue;
            
            // Handle JsonElement from deserialization
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                return JsonElementToType<T>(jsonElement, defaultValue);
            }
            
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
    
    /// <summary>
    /// Sets a dynamic field value.
    /// </summary>
    /// <param name="fieldName">Name of the field</param>
    /// <param name="value">Value to store</param>
    public void SetDynamicField(string fieldName, object? value)
    {
        DynamicFields ??= new Dictionary<string, object?>();
        DynamicFields[fieldName] = value;
    }
    
    /// <summary>
    /// Checks if a dynamic field exists and has a value.
    /// </summary>
    public bool HasDynamicField(string fieldName)
    {
        return DynamicFields?.ContainsKey(fieldName) == true && DynamicFields[fieldName] != null;
    }
    
    /// <summary>
    /// Gets a formatted string representation of a dynamic field.
    /// </summary>
    /// <param name="fieldName">Name of the field</param>
    /// <param name="format">Optional format string (e.g., "N3" for 3 decimal places)</param>
    /// <returns>Formatted string or empty if field not found</returns>
    public string GetDynamicFieldFormatted(string fieldName, string? format = null)
    {
        if (!HasDynamicField(fieldName))
            return string.Empty;
        
        var value = DynamicFields![fieldName];
        
        if (value == null)
            return string.Empty;
        
        if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
            return formattable.ToString(format, null);
        
        return value.ToString() ?? string.Empty;
    }
    
    /// <summary>
    /// Helper to convert JsonElement to typed value.
    /// </summary>
    private static T? JsonElementToType<T>(System.Text.Json.JsonElement element, T? defaultValue)
    {
        try
        {
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            
            if (underlyingType == typeof(double))
                return (T)(object)element.GetDouble();
            if (underlyingType == typeof(int))
                return (T)(object)element.GetInt32();
            if (underlyingType == typeof(long))
                return (T)(object)element.GetInt64();
            if (underlyingType == typeof(string))
                return (T)(object)(element.GetString() ?? string.Empty);
            if (underlyingType == typeof(bool))
                return (T)(object)element.GetBoolean();
            if (underlyingType == typeof(DateTime))
                return (T)(object)element.GetDateTime();
            
            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }
    
    // ========================================================================
    // UI Binding Properties (not serialized)
    // ========================================================================
    
    /// <summary>
    /// Indicates if this entry is selected in the UI.
    /// </summary>
    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    /// <summary>
    /// Indicates if this entry should be highlighted in the UI.
    /// </summary>
    [JsonIgnore]
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }
    
    /// <summary>
    /// Indicates if this entry is excluded from exports/reports.
    /// </summary>
    public bool IsExcluded
    {
        get => _isExcluded;
        set => SetProperty(ref _isExcluded, value);
    }
    
    /// <summary>
    /// Raw data string associated with this entry (for debugging or raw logging).
    /// </summary>
    private string? _rawData;
    public string? RawData
    {
        get => _rawData;
        set => SetProperty(ref _rawData, value);
    }
    
    /// <summary>
    /// Gets whether this entry is a manual entry type.
    /// </summary>
    [JsonIgnore]
    public bool IsManualEntry => EntryType == LogEntryType.ManualEntry;
    
    // ========================================================================
    // Computed Properties
    // ========================================================================
    
    /// <summary>
    /// Gets the date portion of the timestamp.
    /// </summary>
    [JsonIgnore]
    public DateTime Date => Timestamp.Date;
    
    /// <summary>
    /// Gets the time portion of the timestamp.
    /// </summary>
    [JsonIgnore]
    public TimeSpan Time => Timestamp.TimeOfDay;
    
    /// <summary>
    /// Gets the formatted time string (HH:mm:ss).
    /// </summary>
    [JsonIgnore]
    public string TimeDisplay => Timestamp.ToString("HH:mm:ss");
    
    /// <summary>
    /// Gets the formatted date string (dd/MM/yyyy).
    /// </summary>
    [JsonIgnore]
    public string DateDisplay => Timestamp.ToString("dd/MM/yyyy");
    
    /// <summary>
    /// Gets the human-readable entry type name.
    /// </summary>
    [JsonIgnore]
    public string EntryTypeDisplay => EntryType.GetDisplayName();
    
    /// <summary>
    /// Gets the category of the entry type.
    /// </summary>
    [JsonIgnore]
    public string Category => EntryType.GetCategory();
    
    /// <summary>
    /// Gets the icon key for UI display.
    /// </summary>
    [JsonIgnore]
    public string IconKey => EntryType.GetIconKey();
    
    // ========================================================================
    // Constructors
    // ========================================================================
    
    /// <summary>
    /// Creates a new survey log entry with a generated ID and current timestamp.
    /// </summary>
    public SurveyLogEntry()
    {
        _id = Guid.NewGuid();
        _timestamp = DateTime.Now;
        _entryType = LogEntryType.ManualEntry;
        Metadata = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Creates a new survey log entry with specified parameters.
    /// </summary>
    /// <param name="entryType">Type of log entry</param>
    /// <param name="description">Description of the event</param>
    /// <param name="vehicle">Vehicle or object name</param>
    /// <param name="timestamp">Optional timestamp (defaults to now)</param>
    public SurveyLogEntry(LogEntryType entryType, string description, string vehicle = "", DateTime? timestamp = null)
        : this()
    {
        _entryType = entryType;
        _description = description ?? string.Empty;
        _vehicle = vehicle ?? string.Empty;
        _timestamp = timestamp ?? DateTime.Now;
    }
    
    // ========================================================================
    // Metadata Helpers
    // ========================================================================
    
    /// <summary>
    /// Sets a metadata value.
    /// </summary>
    public void SetMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
    }
    
    /// <summary>
    /// Gets a metadata value with type conversion.
    /// </summary>
    public T? GetMetadata<T>(string key, T? defaultValue = default)
    {
        if (Metadata == null || !Metadata.TryGetValue(key, out var value))
            return defaultValue;
        
        try
        {
            if (value is T typedValue)
                return typedValue;
            
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
    
    /// <summary>
    /// Checks if a metadata key exists.
    /// </summary>
    public bool HasMetadata(string key) => Metadata?.ContainsKey(key) ?? false;
    
    // ========================================================================
    // Factory Methods
    // ========================================================================
    
    /// <summary>
    /// Creates a DVR recording entry.
    /// </summary>
    public static SurveyLogEntry CreateDvrEntry(string vehicle, string folderPath, DateTime startTime, string comment = "")
    {
        var entry = new SurveyLogEntry(LogEntryType.DvrRecordingStart, $"DVR Recording: {folderPath}", vehicle, startTime)
        {
            Comments = comment,
            Source = "DVR Monitor"
        };
        entry.SetMetadata("FolderPath", folderPath);
        return entry;
    }
    
    /// <summary>
    /// Creates a position fix entry.
    /// </summary>
    public static SurveyLogEntry CreatePositionFixEntry(int fixNumber, string vehicle, double easting, double northing, 
        DateTime timestamp, string comment = "")
    {
        var entry = new SurveyLogEntry(LogEntryType.PositionFix, $"Position Fix #{fixNumber}", vehicle, timestamp)
        {
            Comments = comment,
            Source = "NPC File Monitor"
        };
        entry.SetMetadata("FixNumber", fixNumber);
        entry.SetMetadata("Easting", easting);
        entry.SetMetadata("Northing", northing);
        return entry;
    }
    
    /// <summary>
    /// Creates a NaviPac logging entry.
    /// </summary>
    public static SurveyLogEntry CreateNaviPacEntry(string fileName, string runline, string vehicle, 
        DateTime timestamp, bool isStart)
    {
        var entryType = isStart ? LogEntryType.NaviPacLoggingStart : LogEntryType.NaviPacLoggingEnd;
        var action = isStart ? "Started" : "Ended";
        var entry = new SurveyLogEntry(entryType, $"NaviPac Logging {action}: {fileName}", vehicle, timestamp)
        {
            Source = "EIVA Data Log"
        };
        entry.SetMetadata("FileName", fileName);
        entry.SetMetadata("Runline", runline);
        return entry;
    }
    
    /// <summary>
    /// Creates a manual entry.
    /// </summary>
    public static SurveyLogEntry CreateManualEntry(string description, string vehicle = "", string comments = "")
    {
        return new SurveyLogEntry(LogEntryType.ManualEntry, description, vehicle)
        {
            Comments = comments,
            Source = "Manual"
        };
    }
    
    /// <summary>
    /// Creates a DVR entry from LogEntryType with description (alternate overload).
    /// </summary>
    public static SurveyLogEntry CreateDvrEntry(LogEntryType entryType, string vehicle, string description, string folderPath)
    {
        var entry = new SurveyLogEntry(entryType, description, vehicle)
        {
            Source = "DVR Monitor"
        };
        entry.SetMetadata("FolderPath", folderPath);
        return entry;
    }
    
    /// <summary>
    /// Creates a position fix entry from a PositionFix object.
    /// </summary>
    public static SurveyLogEntry CreatePositionFixEntry(PositionFix positionFix, LogEntryType entryType, string sourceFile)
    {
        var description = $"Position Fix: {positionFix.ObjectMonitored ?? "Unknown"}";
        var entry = new SurveyLogEntry(entryType, description, positionFix.ObjectMonitored ?? "")
        {
            Timestamp = positionFix.Timestamp,
            Source = sourceFile
        };
        entry.SetMetadata("Easting", positionFix.Easting);
        entry.SetMetadata("Northing", positionFix.Northing);
        if (positionFix.Height.HasValue)
            entry.SetMetadata("Height", positionFix.Height.Value);
        return entry;
    }
    
    // ========================================================================
    // Clone Method
    // ========================================================================
    
    /// <summary>
    /// Creates a shallow copy of this SurveyLogEntry.
    /// </summary>
    /// <returns>A new SurveyLogEntry with copied values.</returns>
    public SurveyLogEntry Clone()
    {
        var clone = (SurveyLogEntry)MemberwiseClone();
        // Create new ID for the clone
        clone._id = Guid.NewGuid();
        // Deep copy the metadata dictionary
        if (Metadata != null)
        {
            clone.Metadata = new Dictionary<string, object>(Metadata);
        }
        return clone;
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
    
    // ========================================================================
    // Object Overrides
    // ========================================================================
    
    public override string ToString() => $"[{TimeDisplay}] {EntryTypeDisplay}: {Description}";
    
    public override bool Equals(object? obj)
    {
        if (obj is SurveyLogEntry other)
            return Id == other.Id;
        return false;
    }
    
    public override int GetHashCode() => Id.GetHashCode();
}
