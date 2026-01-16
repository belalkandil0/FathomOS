// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Models/PositionFix.cs
// Purpose: Position fix data model - captures fix details from .npc files
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.SurveyLogbook.Models;

/// <summary>
/// Represents a position fix record from NaviPac.
/// Captures detailed position fix information including statistics and coordinates.
/// Based on the "Pos Fixes" sheet structure from the survey log Excel file.
/// </summary>
public class PositionFix : INotifyPropertyChanged
{
    private Guid _id;
    private int _fixNumber;
    private DateTime _date;
    private TimeSpan _time;
    private double _easting;
    private double _northing;
    private double? _height;
    private double? _sdEasting;
    private double? _sdNorthing;
    private double? _sdHeight;
    private int _numberOfFixes;
    private string _positioningAid = string.Empty;
    private string _vehicle = string.Empty;
    private double? _kp;
    private double? _dcc;
    private string _comments = string.Empty;
    private string _objectMonitored = string.Empty;
    private string _sourceFile = string.Empty;
    private PositionFixType _fixType = PositionFixType.Standard;
    private string _description = string.Empty;
    
    // Calibration/SetEastingNorthing specific fields
    private double? _requiredEasting;
    private double? _requiredNorthing;
    private double? _errorEasting;
    private double? _errorNorthing;
    
    // ========================================================================
    // Core Properties
    // ========================================================================
    
    /// <summary>
    /// Unique identifier for this position fix.
    /// </summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    /// <summary>
    /// Sequential position fix number.
    /// </summary>
    public int FixNumber
    {
        get => _fixNumber;
        set => SetProperty(ref _fixNumber, value);
    }
    
    /// <summary>
    /// Date of the position fix.
    /// </summary>
    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }
    
    /// <summary>
    /// Time of the position fix.
    /// </summary>
    public TimeSpan Time
    {
        get => _time;
        set => SetProperty(ref _time, value);
    }
    
    /// <summary>
    /// Easting coordinate (typically in US Survey Feet or meters).
    /// </summary>
    public double Easting
    {
        get => _easting;
        set => SetProperty(ref _easting, value);
    }
    
    /// <summary>
    /// Northing coordinate (typically in US Survey Feet or meters).
    /// </summary>
    public double Northing
    {
        get => _northing;
        set => SetProperty(ref _northing, value);
    }
    
    /// <summary>
    /// Height/depth value (optional).
    /// </summary>
    public double? Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }
    
    /// <summary>
    /// Standard deviation of Easting coordinate.
    /// </summary>
    public double? SdEasting
    {
        get => _sdEasting;
        set => SetProperty(ref _sdEasting, value);
    }
    
    /// <summary>
    /// Standard deviation of Northing coordinate.
    /// </summary>
    public double? SdNorthing
    {
        get => _sdNorthing;
        set => SetProperty(ref _sdNorthing, value);
    }
    
    /// <summary>
    /// Standard deviation of Height.
    /// </summary>
    public double? SdHeight
    {
        get => _sdHeight;
        set => SetProperty(ref _sdHeight, value);
    }
    
    /// <summary>
    /// Required/target Easting coordinate (for SetEastingNorthing calibrations).
    /// </summary>
    public double? RequiredEasting
    {
        get => _requiredEasting;
        set => SetProperty(ref _requiredEasting, value);
    }
    
    /// <summary>
    /// Required/target Northing coordinate (for SetEastingNorthing calibrations).
    /// </summary>
    public double? RequiredNorthing
    {
        get => _requiredNorthing;
        set => SetProperty(ref _requiredNorthing, value);
    }
    
    /// <summary>
    /// Easting error (difference between computed and required).
    /// </summary>
    public double? ErrorEasting
    {
        get => _errorEasting;
        set => SetProperty(ref _errorEasting, value);
    }
    
    /// <summary>
    /// Northing error (difference between computed and required).
    /// </summary>
    public double? ErrorNorthing
    {
        get => _errorNorthing;
        set => SetProperty(ref _errorNorthing, value);
    }
    
    /// <summary>
    /// Number of fixes used to compute this position.
    /// </summary>
    public int NumberOfFixes
    {
        get => _numberOfFixes;
        set => SetProperty(ref _numberOfFixes, value);
    }
    
    /// <summary>
    /// Positioning aid used (e.g., "USBL", "DGPS", "HiPAP").
    /// </summary>
    public string PositioningAid
    {
        get => _positioningAid;
        set => SetProperty(ref _positioningAid, value ?? string.Empty);
    }
    
    /// <summary>
    /// Vehicle or object name (e.g., "HD11", "HD12", "Ross Candies").
    /// </summary>
    public string Vehicle
    {
        get => _vehicle;
        set => SetProperty(ref _vehicle, value ?? string.Empty);
    }
    
    /// <summary>
    /// Kilometre Post (KP) value.
    /// </summary>
    public double? Kp
    {
        get => _kp;
        set => SetProperty(ref _kp, value);
    }
    
    /// <summary>
    /// Distance Cross Course (DCC) value.
    /// </summary>
    public double? Dcc
    {
        get => _dcc;
        set => SetProperty(ref _dcc, value);
    }
    
    /// <summary>
    /// Comments or notes about this fix.
    /// </summary>
    public string Comments
    {
        get => _comments;
        set => SetProperty(ref _comments, value ?? string.Empty);
    }
    
    /// <summary>
    /// Object being monitored (from .npc file header).
    /// </summary>
    public string ObjectMonitored
    {
        get => _objectMonitored;
        set => SetProperty(ref _objectMonitored, value ?? string.Empty);
    }
    
    /// <summary>
    /// Source .npc file path.
    /// </summary>
    public string SourceFile
    {
        get => _sourceFile;
        set => SetProperty(ref _sourceFile, value ?? string.Empty);
    }
    
    /// <summary>
    /// Type of position fix (Standard, Calibration, Verification, etc.).
    /// </summary>
    public PositionFixType FixType
    {
        get => _fixType;
        set => SetProperty(ref _fixType, value);
    }
    
    /// <summary>
    /// Description or notes for this position fix.
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value ?? string.Empty);
    }
    
    /// <summary>
    /// Alias for ObjectMonitored (for compatibility).
    /// </summary>
    [JsonIgnore]
    public string ObjectName
    {
        get => ObjectMonitored;
        set => ObjectMonitored = value;
    }
    
    // ========================================================================
    // Statistics from .npc file (Error/Average/Median/Min/Max/StdDev)
    // ========================================================================
    
    /// <summary>
    /// Error statistics from .npc file.
    /// </summary>
    public FixStatistics? EastingStats { get; set; }
    
    /// <summary>
    /// Northing statistics from .npc file.
    /// </summary>
    public FixStatistics? NorthingStats { get; set; }
    
    /// <summary>
    /// Height statistics from .npc file.
    /// </summary>
    public FixStatistics? HeightStats { get; set; }
    
    // ========================================================================
    // Geodesy Information (from .npc file)
    // ========================================================================
    
    /// <summary>
    /// Projection name (e.g., "BLM zone 15N (US survey feet)").
    /// </summary>
    public string? Projection { get; set; }
    
    /// <summary>
    /// Ellipsoid name (e.g., "Clarke 1866").
    /// </summary>
    public string? Ellipsoid { get; set; }
    
    /// <summary>
    /// Required/target coordinates from calibration.
    /// </summary>
    public double? RequiredX { get; set; }
    public double? RequiredY { get; set; }
    public double? RequiredH { get; set; }
    
    /// <summary>
    /// Range to target.
    /// </summary>
    public double? Range { get; set; }
    
    /// <summary>
    /// Bearing to target (degrees).
    /// </summary>
    public double? Bearing { get; set; }
    
    // ========================================================================
    // Individual Observations (from .npc file data section)
    // ========================================================================
    
    /// <summary>
    /// List of individual observations that make up this fix.
    /// </summary>
    public List<FixObservation>? Observations { get; set; }
    
    // ========================================================================
    // Computed Properties
    // ========================================================================
    
    /// <summary>
    /// Gets the combined date and time as DateTime.
    /// </summary>
    [JsonIgnore]
    public DateTime Timestamp => Date.Add(Time);
    
    /// <summary>
    /// Gets or sets the computed/observed Easting coordinate.
    /// Used for Set Easting/Northing operations where computed differs from required.
    /// Setting this value also updates the Easting property.
    /// </summary>
    [JsonIgnore]
    public double ComputedEasting 
    { 
        get => Easting;
        set => Easting = value;
    }
    
    /// <summary>
    /// Gets or sets the computed/observed Northing coordinate.
    /// Used for Set Easting/Northing operations where computed differs from required.
    /// Setting this value also updates the Northing property.
    /// </summary>
    [JsonIgnore]
    public double ComputedNorthing 
    { 
        get => Northing;
        set => Northing = value;
    }
    
    /// <summary>
    /// Alias for FixType property (for compatibility with UI code).
    /// </summary>
    [JsonIgnore]
    public PositionFixType PositionFixType
    {
        get => FixType;
        set => FixType = value;
    }
    
    /// <summary>
    /// Gets or sets the name for this fix (alias for ObjectMonitored).
    /// </summary>
    [JsonIgnore]
    public string Name
    {
        get => ObjectMonitored;
        set => ObjectMonitored = value;
    }
    
    /// <summary>
    /// Gets or sets the source for this fix (alias for SourceFile).
    /// </summary>
    [JsonIgnore]
    public string Source
    {
        get => SourceFile;
        set => SourceFile = value;
    }
    
    /// <summary>
    /// Gets formatted coordinate string.
    /// </summary>
    [JsonIgnore]
    public string CoordinateDisplay => $"E: {Easting:F3}, N: {Northing:F3}";
    
    /// <summary>
    /// Gets formatted time string.
    /// </summary>
    [JsonIgnore]
    public string TimeDisplay => Time.ToString(@"hh\:mm\:ss");
    
    /// <summary>
    /// Gets formatted date string.
    /// </summary>
    [JsonIgnore]
    public string DateDisplay => Date.ToString("dd/MM/yyyy");
    
    /// <summary>
    /// Gets the combined standard deviation (horizontal).
    /// </summary>
    [JsonIgnore]
    public double? HorizontalSd => (SdEasting.HasValue && SdNorthing.HasValue)
        ? Math.Sqrt(SdEasting.Value * SdEasting.Value + SdNorthing.Value * SdNorthing.Value)
        : null;
    
    // ========================================================================
    // Constructors
    // ========================================================================
    
    /// <summary>
    /// Creates a new position fix with generated ID.
    /// </summary>
    public PositionFix()
    {
        _id = Guid.NewGuid();
        _date = DateTime.Today;
        _time = TimeSpan.Zero;
        _numberOfFixes = 1;
    }
    
    /// <summary>
    /// Creates a position fix from coordinates.
    /// </summary>
    public PositionFix(int fixNumber, double easting, double northing, DateTime timestamp)
        : this()
    {
        _fixNumber = fixNumber;
        _easting = easting;
        _northing = northing;
        _date = timestamp.Date;
        _time = timestamp.TimeOfDay;
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
    
    public override string ToString() => $"Fix #{FixNumber}: E={Easting:F2}, N={Northing:F2} [{TimeDisplay}]";
    
    // ========================================================================
    // Clone Method
    // ========================================================================
    
    /// <summary>
    /// Creates a shallow copy of this PositionFix.
    /// </summary>
    /// <returns>A new PositionFix with copied values.</returns>
    public PositionFix Clone()
    {
        var clone = (PositionFix)MemberwiseClone();
        clone._id = Guid.NewGuid();
        
        // Deep copy statistics if present
        if (EastingStats != null)
            clone.EastingStats = new FixStatistics { Error = EastingStats.Error, Average = EastingStats.Average, Median = EastingStats.Median, Minimum = EastingStats.Minimum, Maximum = EastingStats.Maximum, StdDev = EastingStats.StdDev, Error95Percent = EastingStats.Error95Percent, Count = EastingStats.Count, TotalCount = EastingStats.TotalCount };
        if (NorthingStats != null)
            clone.NorthingStats = new FixStatistics { Error = NorthingStats.Error, Average = NorthingStats.Average, Median = NorthingStats.Median, Minimum = NorthingStats.Minimum, Maximum = NorthingStats.Maximum, StdDev = NorthingStats.StdDev, Error95Percent = NorthingStats.Error95Percent, Count = NorthingStats.Count, TotalCount = NorthingStats.TotalCount };
        if (HeightStats != null)
            clone.HeightStats = new FixStatistics { Error = HeightStats.Error, Average = HeightStats.Average, Median = HeightStats.Median, Minimum = HeightStats.Minimum, Maximum = HeightStats.Maximum, StdDev = HeightStats.StdDev, Error95Percent = HeightStats.Error95Percent, Count = HeightStats.Count, TotalCount = HeightStats.TotalCount };
        
        // Deep copy observations if present
        if (Observations != null)
            clone.Observations = Observations.Select(o => new FixObservation { Timestamp = o.Timestamp, ObservedX = o.ObservedX, ObservedY = o.ObservedY, ObservedZ = o.ObservedZ }).ToList();
        
        return clone;
    }
}

/// <summary>
/// Statistics for a coordinate component from .npc file.
/// </summary>
public class FixStatistics
{
    public double Error { get; set; }
    public double Average { get; set; }
    public double Median { get; set; }
    public double Minimum { get; set; }
    public double Maximum { get; set; }
    public double StdDev { get; set; }
    public double Error95Percent { get; set; }
    public int Count { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// Individual observation from a position fix.
/// </summary>
public class FixObservation
{
    public DateTime Timestamp { get; set; }
    public double ObservedX { get; set; }
    public double ObservedY { get; set; }
    public double? ObservedZ { get; set; }
    
    public string TimeDisplay => Timestamp.ToString("dd/MM/yyyy HH:mm:ss.fff");
}

/// <summary>
/// Type of position fix operation.
/// </summary>
public enum PositionFixType
{
    /// <summary>Standard position fix.</summary>
    Standard,
    /// <summary>Calibration fix.</summary>
    Calibration,
    /// <summary>Verification fix.</summary>
    Verification,
    /// <summary>Pre-lay fix.</summary>
    PreLay,
    /// <summary>Post-lay fix.</summary>
    PostLay,
    /// <summary>As-built fix.</summary>
    AsBuilt,
    /// <summary>Set Easting/Northing operation (XYZ calibration).</summary>
    SetEastingNorthing,
    /// <summary>Waypoint position fix.</summary>
    Waypoint,
    /// <summary>Manual position fix entry.</summary>
    Manual,
    /// <summary>Other fix type.</summary>
    Other
}
