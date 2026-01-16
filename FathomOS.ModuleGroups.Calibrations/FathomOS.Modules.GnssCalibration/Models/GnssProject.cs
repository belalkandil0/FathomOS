using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.GnssCalibration.Models;

/// <summary>
/// Length/distance unit for coordinates and results.
/// </summary>
public enum LengthUnit
{
    Meters,
    InternationalFeet,
    UsSurveyFeet
}

/// <summary>
/// Comparison mode - how systems are being compared.
/// </summary>
public enum ComparisonMode
{
    /// <summary>Compare two GNSS systems from the same NPD file.</summary>
    TwoSystemComparison,
    
    /// <summary>Validate GNSS against POS reference (ground truth).</summary>
    PosValidation
}

/// <summary>
/// Project container holding all settings, metadata, and data references.
/// Implements INotifyPropertyChanged for UI binding.
/// </summary>
public class GnssProject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    // Backing fields
    private string _projectName = string.Empty;
    private string _vesselName = string.Empty;
    private DateTime? _surveyDate;
    private string _location = string.Empty;
    private string _comparisonNode = string.Empty;
    private string _operator = string.Empty;
    private string _system1Name = "DGNSS 1";
    private string _system2Name = "DGNSS 2";
    private string? _npdFilePath;
    private string? _posFilePath;
    private LengthUnit _unit = LengthUnit.Meters;
    private ComparisonMode _mode = ComparisonMode.TwoSystemComparison;
    private double _sigmaThreshold = 3.0;
    private bool _autoFilterEnabled = true;
    private DateTime? _startTime;
    private DateTime? _endTime;
    private bool _usePosAsReference;
    
    // Project Metadata
    public string ProjectName
    {
        get => _projectName;
        set { _projectName = value; OnPropertyChanged(); }
    }
    
    public string VesselName
    {
        get => _vesselName;
        set { _vesselName = value; OnPropertyChanged(); }
    }
    
    public DateTime? SurveyDate
    {
        get => _surveyDate;
        set { _surveyDate = value; OnPropertyChanged(); }
    }
    
    public string Location
    {
        get => _location;
        set { _location = value; OnPropertyChanged(); }
    }
    
    public string ComparisonNode
    {
        get => _comparisonNode;
        set { _comparisonNode = value; OnPropertyChanged(); }
    }
    
    public string Operator
    {
        get => _operator;
        set { _operator = value; OnPropertyChanged(); }
    }
    
    // Additional metadata properties
    private string _clientName = string.Empty;
    private string _surveyorName = string.Empty;
    
    public string ClientName
    {
        get => _clientName;
        set { _clientName = value; OnPropertyChanged(); }
    }
    
    public string SurveyorName
    {
        get => _surveyorName;
        set { _surveyorName = value; OnPropertyChanged(); }
    }
    
    /// <summary>Alias for UnitAbbreviation for compatibility.</summary>
    public string CoordinateUnit => UnitAbbreviation;
    
    // System Identification
    public string System1Name
    {
        get => _system1Name;
        set { _system1Name = value; OnPropertyChanged(); }
    }
    
    public string System2Name
    {
        get => _system2Name;
        set { _system2Name = value; OnPropertyChanged(); }
    }
    
    // Data Files
    public string? NpdFilePath
    {
        get => _npdFilePath;
        set { _npdFilePath = value; OnPropertyChanged(); }
    }
    
    public string? PosFilePath
    {
        get => _posFilePath;
        set { _posFilePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPosFile)); }
    }
    
    /// <summary>Whether a POS file has been loaded.</summary>
    public bool HasPosFile => !string.IsNullOrEmpty(PosFilePath);
    
    /// <summary>Use POS file as reference (ground truth) instead of System 2.</summary>
    public bool UsePosAsReference
    {
        get => _usePosAsReference;
        set { _usePosAsReference = value; OnPropertyChanged(); }
    }
    
    // Settings
    public LengthUnit Unit
    {
        get => _unit;
        set { _unit = value; OnPropertyChanged(); OnPropertyChanged(nameof(UnitIndex)); }
    }
    
    /// <summary>Index-based accessor for Unit (for ComboBox binding).</summary>
    public int UnitIndex
    {
        get => (int)Unit;
        set => Unit = (LengthUnit)value;
    }
    
    public ComparisonMode Mode
    {
        get => _mode;
        set { _mode = value; OnPropertyChanged(); }
    }
    
    // Filter Settings
    public double SigmaThreshold
    {
        get => _sigmaThreshold;
        set { _sigmaThreshold = value; OnPropertyChanged(); }
    }
    
    public bool AutoFilterEnabled
    {
        get => _autoFilterEnabled;
        set { _autoFilterEnabled = value; OnPropertyChanged(); }
    }
    
    // Time Range (optional filtering)
    public DateTime? StartTime
    {
        get => _startTime;
        set { _startTime = value; OnPropertyChanged(); }
    }
    
    public DateTime? EndTime
    {
        get => _endTime;
        set { _endTime = value; OnPropertyChanged(); }
    }
    
    // Column Mapping (for NPD parsing)
    public GnssColumnMapping ColumnMapping { get; set; } = new();
    
    // POS File Settings
    public PosParseSettings PosSettings { get; set; } = new();
    
    // UTM Settings (for coordinate conversion)
    private int _utmZone = 32;
    private bool _utmNorthern = true;
    
    // Tolerance Settings
    private double _toleranceValue = 0.15;
    private bool _toleranceCheckEnabled = true;
    
    public int UtmZone
    {
        get => _utmZone;
        set { _utmZone = value; OnPropertyChanged(); }
    }
    
    public bool UtmNorthernHemisphere
    {
        get => _utmNorthern;
        set { _utmNorthern = value; OnPropertyChanged(); }
    }
    
    /// <summary>User-specified 2DRMS tolerance value for Pass/Fail check.</summary>
    public double ToleranceValue
    {
        get => _toleranceValue;
        set { _toleranceValue = value; OnPropertyChanged(); }
    }
    
    /// <summary>Whether tolerance check is enabled.</summary>
    public bool ToleranceCheckEnabled
    {
        get => _toleranceCheckEnabled;
        set { _toleranceCheckEnabled = value; OnPropertyChanged(); }
    }
    
    /// <summary>Gets the unit abbreviation for display.</summary>
    public string UnitAbbreviation => Unit switch
    {
        LengthUnit.Meters => "m",
        LengthUnit.InternationalFeet => "ft",
        LengthUnit.UsSurveyFeet => "US ft",
        _ => "m"
    };
    
    /// <summary>Gets the full unit name for reports.</summary>
    public string UnitFullName => Unit switch
    {
        LengthUnit.Meters => "Meters",
        LengthUnit.InternationalFeet => "International Feet",
        LengthUnit.UsSurveyFeet => "US Survey Feet",
        _ => "Meters"
    };
    
    /// <summary>Conversion factor from meters to selected unit.</summary>
    public double ConversionFactor => Unit switch
    {
        LengthUnit.Meters => 1.0,
        LengthUnit.InternationalFeet => 3.280839895,
        LengthUnit.UsSurveyFeet => 3.280833333,
        _ => 1.0
    };
    
    /// <summary>Gets the effective reference name based on mode.</summary>
    public string ReferenceName => UsePosAsReference ? "POS (Ground Truth)" : System2Name;
    
    /// <summary>Validates the project settings.</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(ProjectName))
            errors.Add("Project name is required.");
        
        if (string.IsNullOrWhiteSpace(VesselName))
            errors.Add("Vessel name is required.");
        
        if (string.IsNullOrWhiteSpace(NpdFilePath))
            errors.Add("NPD file is required.");
        
        if (UsePosAsReference && string.IsNullOrWhiteSpace(PosFilePath))
            errors.Add("POS file is required when using POS as reference.");
        
        if (SigmaThreshold <= 0 || SigmaThreshold > 10)
            errors.Add("Sigma threshold must be between 0.1 and 10.");
        
        return errors;
    }
}

/// <summary>
/// Column mapping configuration for NPD file parsing.
/// </summary>
public class GnssColumnMapping
{
    // Time columns
    public int DateColumnIndex { get; set; } = -1;  // -1 = auto-detect
    public int TimeColumnIndex { get; set; } = -1;
    public bool HasDateTimeSplit { get; set; } = true;  // NaviPac style
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    
    // System 1 columns - flexible patterns to match common naming conventions
    public int System1EastingIndex { get; set; } = -1;
    public int System1NorthingIndex { get; set; } = -1;
    public int System1HeightIndex { get; set; } = -1;
    // Matches: DGNSS 1 East, GNSS 1 Easting, GPS1 E, Primary East, Ref E, System1 East, Sys1East, etc.
    public string System1EastingPattern { get; set; } = @"DGNSS.?1.?East|GNSS.?1.?East|GPS.?1.?East|Primary.?East|Ref.?East|System.?1.?East|Sys.?1.?East|^East$|^Easting$|^E1$|Position.*1.*East";
    public string System1NorthingPattern { get; set; } = @"DGNSS.?1.?North|GNSS.?1.?North|GPS.?1.?North|Primary.?North|Ref.?North|System.?1.?North|Sys.?1.?North|^North$|^Northing$|^N1$|Position.*1.*North";
    public string System1HeightPattern { get; set; } = @"DGNSS.?1.?Height|GNSS.?1.?Height|GPS.?1.?Height|DGNSS.?1.?Alt|Primary.?Height|Ref.?Height|System.?1.?Height|Sys.?1.?Height|^Height$|^Altitude$|^H1$|^Alt1$|Position.*1.*Height";
    
    // System 2 columns - flexible patterns for comparison system
    public int System2EastingIndex { get; set; } = -1;
    public int System2NorthingIndex { get; set; } = -1;
    public int System2HeightIndex { get; set; } = -1;
    // Matches: DGNSS 2 East, GNSS 2 Easting, GPS2 E, Secondary East, Obs E, System2 East, Sys2East, etc.
    public string System2EastingPattern { get; set; } = @"DGNSS.?2.?East|GNSS.?2.?East|GPS.?2.?East|Secondary.?East|Obs.?East|System.?2.?East|Sys.?2.?East|Comp.?East|^E2$|Position.*2.*East";
    public string System2NorthingPattern { get; set; } = @"DGNSS.?2.?North|GNSS.?2.?North|GPS.?2.?North|Secondary.?North|Obs.?North|System.?2.?North|Sys.?2.?North|Comp.?North|^N2$|Position.*2.*North";
    public string System2HeightPattern { get; set; } = @"DGNSS.?2.?Height|GNSS.?2.?Height|GPS.?2.?Height|DGNSS.?2.?Alt|Secondary.?Height|Obs.?Height|System.?2.?Height|Sys.?2.?Height|Comp.?Height|^H2$|^Alt2$|Position.*2.*Height";
    
    /// <summary>Detected column names from file (for display in UI).</summary>
    public List<string> AvailableColumns { get; set; } = new();
}
