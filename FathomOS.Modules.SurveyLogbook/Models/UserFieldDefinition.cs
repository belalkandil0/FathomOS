// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/UserFieldDefinition.cs
// Purpose: Defines a user-configurable field from NaviPac UDO data
// Version: 9.0.0
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Data types for NaviPac field values.
/// </summary>
public enum FieldDataType
{
    [Description("Auto-detect")]
    Auto,
    
    [Description("String")]
    String,
    
    [Description("Integer")]
    Integer,
    
    [Description("Decimal")]
    Decimal,
    
    [Description("Date/Time")]
    DateTime,
    
    [Description("Latitude")]
    Latitude,
    
    [Description("Longitude")]
    Longitude,
    
    [Description("Easting")]
    Easting,
    
    [Description("Northing")]
    Northing,
    
    [Description("Height/Depth")]
    HeightDepth,
    
    [Description("Heading/Bearing")]
    HeadingBearing,
    
    [Description("KP (Kilometre Post)")]
    KP,
    
    [Description("DCC (Distance Cross Course)")]
    DCC,
    
    [Description("DOL (Distance Off Line)")]
    DOL,
    
    [Description("Roll")]
    Roll,
    
    [Description("Pitch")]
    Pitch,
    
    [Description("Heave")]
    Heave,
    
    [Description("Speed")]
    Speed,
    
    [Description("Course")]
    Course,
    
    [Description("DAL (Distance Along Line)")]
    DAL,
    
    [Description("Depth")]
    Depth,
    
    [Description("Age (Position Age)")]
    Age,
    
    [Description("Event Number")]
    EventNumber
}

/// <summary>
/// Represents a user-defined field configuration for NaviPac UDO data.
/// Each field maps a position in the incoming data string to a named field.
/// </summary>
public class UserFieldDefinition : INotifyPropertyChanged
{
    private int _position;
    private string _fieldName = string.Empty;
    private FieldDataType _dataType = FieldDataType.Auto;
    private bool _showInLog = true;
    private bool _isEnabled = true;
    private string _format = string.Empty;
    private int _decimalPlaces = 3;
    private string _unit = string.Empty;
    private double? _scaleFactor;
    private double? _offset;
    
    /// <summary>
    /// Zero-based position/index of this field in the NaviPac data string.
    /// This corresponds to the order of fields in the User Defined Output.
    /// </summary>
    public int Position
    {
        get => _position;
        set { _position = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// User-defined name for this field (e.g., "Vessel Easting", "Water Depth").
    /// </summary>
    public string FieldName
    {
        get => _fieldName;
        set { _fieldName = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Expected data type for parsing and validation.
    /// </summary>
    public FieldDataType DataType
    {
        get => _dataType;
        set { _dataType = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Whether this field should be displayed in the Survey Log table.
    /// </summary>
    public bool ShowInLog
    {
        get => _showInLog;
        set { _showInLog = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Whether this field is enabled for parsing.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Custom format string for display (e.g., "F3" for 3 decimal places).
    /// </summary>
    public string Format
    {
        get => _format;
        set { _format = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Number of decimal places for numeric fields.
    /// </summary>
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set { _decimalPlaces = Math.Max(0, Math.Min(10, value)); OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Unit of measurement (e.g., "m", "deg", "m/s").
    /// </summary>
    public string Unit
    {
        get => _unit;
        set { _unit = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Optional scale factor to apply to the value.
    /// </summary>
    public double? ScaleFactor
    {
        get => _scaleFactor;
        set { _scaleFactor = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Optional offset to add to the value (applied after scaling).
    /// </summary>
    public double? Offset
    {
        get => _offset;
        set { _offset = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Creates a deep copy of this field definition.
    /// </summary>
    public UserFieldDefinition Clone()
    {
        return new UserFieldDefinition
        {
            Position = Position,
            FieldName = FieldName,
            DataType = DataType,
            ShowInLog = ShowInLog,
            IsEnabled = IsEnabled,
            Format = Format,
            DecimalPlaces = DecimalPlaces,
            Unit = Unit,
            ScaleFactor = ScaleFactor,
            Offset = Offset
        };
    }
    
    /// <summary>
    /// Returns display string for list views.
    /// </summary>
    public override string ToString()
    {
        return $"[{Position}] {FieldName} ({DataType})";
    }
    
    #region INotifyPropertyChanged
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    #endregion
    
    #region Static Factory Methods
    
    /// <summary>
    /// Creates a default set of field definitions based on standard NaviPac output.
    /// </summary>
    public static List<UserFieldDefinition> CreateDefaultFields()
    {
        return new List<UserFieldDefinition>
        {
            new() { Position = 0, FieldName = "Event", DataType = FieldDataType.EventNumber, ShowInLog = true },
            new() { Position = 1, FieldName = "Date", DataType = FieldDataType.DateTime, ShowInLog = true },
            new() { Position = 2, FieldName = "Time", DataType = FieldDataType.DateTime, ShowInLog = true },
            new() { Position = 3, FieldName = "Easting", DataType = FieldDataType.Easting, ShowInLog = true, DecimalPlaces = 2, Unit = "m" },
            new() { Position = 4, FieldName = "Northing", DataType = FieldDataType.Northing, ShowInLog = true, DecimalPlaces = 2, Unit = "m" },
            new() { Position = 5, FieldName = "Height", DataType = FieldDataType.HeightDepth, ShowInLog = true, DecimalPlaces = 2, Unit = "m" },
            new() { Position = 6, FieldName = "KP", DataType = FieldDataType.KP, ShowInLog = true, DecimalPlaces = 3, Unit = "km" },
            new() { Position = 7, FieldName = "DCC", DataType = FieldDataType.DCC, ShowInLog = true, DecimalPlaces = 2, Unit = "m" },
            new() { Position = 8, FieldName = "Latitude", DataType = FieldDataType.Latitude, ShowInLog = false },
            new() { Position = 9, FieldName = "Longitude", DataType = FieldDataType.Longitude, ShowInLog = false },
            new() { Position = 10, FieldName = "Gyro", DataType = FieldDataType.HeadingBearing, ShowInLog = true, DecimalPlaces = 1, Unit = "°" },
            new() { Position = 11, FieldName = "Roll", DataType = FieldDataType.Roll, ShowInLog = false, DecimalPlaces = 2, Unit = "°" },
            new() { Position = 12, FieldName = "Pitch", DataType = FieldDataType.Pitch, ShowInLog = false, DecimalPlaces = 2, Unit = "°" },
            new() { Position = 13, FieldName = "Heave", DataType = FieldDataType.Heave, ShowInLog = false, DecimalPlaces = 3, Unit = "m" },
            new() { Position = 14, FieldName = "Speed", DataType = FieldDataType.Speed, ShowInLog = true, DecimalPlaces = 1, Unit = "kn" },
            new() { Position = 15, FieldName = "Course", DataType = FieldDataType.HeadingBearing, ShowInLog = true, DecimalPlaces = 1, Unit = "°" }
        };
    }
    
    /// <summary>
    /// Creates a minimal set of position-only fields.
    /// </summary>
    public static List<UserFieldDefinition> CreateMinimalFields()
    {
        return new List<UserFieldDefinition>
        {
            new() { Position = 0, FieldName = "Event", DataType = FieldDataType.EventNumber, ShowInLog = true },
            new() { Position = 1, FieldName = "DateTime", DataType = FieldDataType.DateTime, ShowInLog = true },
            new() { Position = 2, FieldName = "Easting", DataType = FieldDataType.Easting, ShowInLog = true },
            new() { Position = 3, FieldName = "Northing", DataType = FieldDataType.Northing, ShowInLog = true },
            new() { Position = 4, FieldName = "Height", DataType = FieldDataType.HeightDepth, ShowInLog = true }
        };
    }
    
    #endregion
}

/// <summary>
/// A named collection of field definitions that can be saved and loaded.
/// </summary>
public class FieldTemplate : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _separator = ",";
    private DateTime _createdDate = DateTime.Now;
    private DateTime _modifiedDate = DateTime.Now;
    private List<UserFieldDefinition> _fields = new();
    
    /// <summary>
    /// Name of this template (e.g., "Standard NaviPac", "ROV Survey").
    /// </summary>
    public string Name
    {
        get => _name;
        set { _name = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Description of what this template is for.
    /// </summary>
    public string Description
    {
        get => _description;
        set { _description = value ?? string.Empty; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Field separator character(s) used in NaviPac output.
    /// </summary>
    public string Separator
    {
        get => _separator;
        set { _separator = value ?? ","; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// When this template was created.
    /// </summary>
    public DateTime CreatedDate
    {
        get => _createdDate;
        set { _createdDate = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// When this template was last modified.
    /// </summary>
    public DateTime ModifiedDate
    {
        get => _modifiedDate;
        set { _modifiedDate = value; OnPropertyChanged(); }
    }
    
    /// <summary>
    /// The field definitions in this template.
    /// </summary>
    public List<UserFieldDefinition> Fields
    {
        get => _fields;
        set { _fields = value ?? new List<UserFieldDefinition>(); OnPropertyChanged(); }
    }
    
    /// <summary>
    /// Creates a deep copy of this template.
    /// </summary>
    public FieldTemplate Clone()
    {
        return new FieldTemplate
        {
            Name = Name,
            Description = Description,
            Separator = Separator,
            CreatedDate = CreatedDate,
            ModifiedDate = DateTime.Now,
            Fields = Fields.Select(f => f.Clone()).ToList()
        };
    }
    
    /// <summary>
    /// Returns display string.
    /// </summary>
    public override string ToString()
    {
        return $"{Name} ({Fields.Count} fields)";
    }
    
    #region INotifyPropertyChanged
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    #endregion
    
    #region Static Factory Methods
    
    /// <summary>
    /// Creates a default template based on standard NaviPac output.
    /// </summary>
    public static FieldTemplate CreateDefaultTemplate()
    {
        return new FieldTemplate
        {
            Name = "Standard NaviPac UDO",
            Description = "Default NaviPac User Defined Output format with common survey fields",
            Separator = ",",
            Fields = UserFieldDefinition.CreateDefaultFields()
        };
    }
    
    /// <summary>
    /// Creates a minimal template for basic position logging.
    /// </summary>
    public static FieldTemplate CreateMinimalTemplate()
    {
        return new FieldTemplate
        {
            Name = "Minimal Position",
            Description = "Basic position-only template",
            Separator = ",",
            Fields = UserFieldDefinition.CreateMinimalFields()
        };
    }
    
    #endregion
}
