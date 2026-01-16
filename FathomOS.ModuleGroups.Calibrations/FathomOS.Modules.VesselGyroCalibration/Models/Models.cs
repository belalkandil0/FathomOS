using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.VesselGyroCalibration.Models;

#region Enums

/// <summary>
/// Length/distance unit for measurements
/// </summary>
public enum LengthUnit
{
    [Description("Meters")]
    Meters,
    
    [Description("International Feet")]
    InternationalFeet,
    
    [Description("US Survey Feet")]
    USSurveyFeet,
    
    [Description("US Feet")]
    USFeet
}

/// <summary>
/// Purpose of the calibration exercise
/// </summary>
public enum ExercisePurpose
{
    /// <summary>
    /// Calibration: Determine the C-O value of a system being calibrated
    /// </summary>
    [Description("Calibration")]
    Calibration,
    
    /// <summary>
    /// Verification: Check that the existing C-O value is still valid (should be ≈0)
    /// </summary>
    [Description("Verification")]
    Verification
}

/// <summary>
/// QC Check result status
/// </summary>
public enum QcStatus
{
    Pass,
    Warning,
    Fail,
    NotChecked
}

/// <summary>
/// Data point acceptance status
/// </summary>
public enum PointStatus
{
    Pending,
    Accepted,
    Rejected
}

#endregion

#region Project Information

/// <summary>
/// Contains all project metadata and configuration for a calibration exercise
/// </summary>
public class CalibrationProject : INotifyPropertyChanged
{
    private string _projectTitle = "Name of Project";
    private string _projectNumber = "";
    private DateTime _dateTime = DateTime.Now;
    private string _observedBy = "";
    private string _checkedBy = "";
    private string _vesselName = "";
    private string _location = "";
    private double? _latitude;
    private string _referenceSystemModel = "";
    private string _referenceSystemSerial = "";
    private string _calibratedSystemModel = "";
    private string _calibratedSystemSerial = "";
    private ExercisePurpose _purpose = ExercisePurpose.Verification;
    private LengthUnit _displayUnit = LengthUnit.Meters;
    private bool _roundingEnabled = true;
    private int _roundingDecimalPlaces = 4;

    // Project Information
    public string ProjectTitle
    {
        get => _projectTitle;
        set => SetProperty(ref _projectTitle, value);
    }

    public string ProjectNumber
    {
        get => _projectNumber;
        set => SetProperty(ref _projectNumber, value);
    }

    public DateTime DateTime
    {
        get => _dateTime;
        set => SetProperty(ref _dateTime, value);
    }

    public string ObservedBy
    {
        get => _observedBy;
        set => SetProperty(ref _observedBy, value);
    }

    public string CheckedBy
    {
        get => _checkedBy;
        set => SetProperty(ref _checkedBy, value);
    }

    // Location Information
    public string VesselName
    {
        get => _vesselName;
        set => SetProperty(ref _vesselName, value);
    }

    public string Location
    {
        get => _location;
        set => SetProperty(ref _location, value);
    }

    public double? Latitude
    {
        get => _latitude;
        set => SetProperty(ref _latitude, value);
    }

    // Reference System (the "truth")
    public string ReferenceSystemModel
    {
        get => _referenceSystemModel;
        set => SetProperty(ref _referenceSystemModel, value);
    }

    public string ReferenceSystemSerial
    {
        get => _referenceSystemSerial;
        set => SetProperty(ref _referenceSystemSerial, value);
    }

    // Calibrated System (the one being checked)
    public string CalibratedSystemModel
    {
        get => _calibratedSystemModel;
        set => SetProperty(ref _calibratedSystemModel, value);
    }

    public string CalibratedSystemSerial
    {
        get => _calibratedSystemSerial;
        set => SetProperty(ref _calibratedSystemSerial, value);
    }

    // Exercise Purpose
    public ExercisePurpose Purpose
    {
        get => _purpose;
        set => SetProperty(ref _purpose, value);
    }

    // Display Unit for measurements
    public LengthUnit DisplayUnit
    {
        get => _displayUnit;
        set => SetProperty(ref _displayUnit, value);
    }

    // Rounding Options
    public bool RoundingEnabled
    {
        get => _roundingEnabled;
        set => SetProperty(ref _roundingEnabled, value);
    }

    public int RoundingDecimalPlaces
    {
        get => _roundingDecimalPlaces;
        set => SetProperty(ref _roundingDecimalPlaces, value);
    }

    // QC Criteria
    public QcCriteria QcCriteria { get; set; } = new();

    // Alias properties for export service compatibility
    public string ProjectName => ProjectTitle;
    public string ClientName => Location;  // Using Location as client for now
    public DateTime SurveyDate => DateTime;
    public string SurveyorName => ObservedBy;

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

#endregion

#region Data Points

/// <summary>
/// Represents a single gyro heading measurement with calculated values
/// </summary>
public class GyroDataPoint : INotifyPropertyChanged
{
    private int _index;
    private DateTime _timestamp;
    private double _referenceHeading;
    private double _calibratedHeading;
    private double _co;
    private double _standardizedCO;
    private bool _isRejected;
    private double? _acceptedCO;

    /// <summary>
    /// Row index in the original data
    /// </summary>
    public int Index
    {
        get => _index;
        set => SetProperty(ref _index, value);
    }

    /// <summary>
    /// UTC timestamp of the measurement
    /// </summary>
    public DateTime Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    /// <summary>
    /// Reference system heading in degrees (the "truth")
    /// </summary>
    public double ReferenceHeading
    {
        get => _referenceHeading;
        set => SetProperty(ref _referenceHeading, value);
    }

    /// <summary>
    /// Calibrated system heading in degrees (being verified)
    /// </summary>
    public double CalibratedHeading
    {
        get => _calibratedHeading;
        set => SetProperty(ref _calibratedHeading, value);
    }

    /// <summary>
    /// Calculated C-O value (Reference - Calibrated) with wrap-around
    /// </summary>
    public double CO
    {
        get => _co;
        set => SetProperty(ref _co, value);
    }

    /// <summary>
    /// Alias for CO property (for export service compatibility)
    /// </summary>
    public double CalculatedCO => CO;

    /// <summary>
    /// Alias for Timestamp property (for export service compatibility)
    /// </summary>
    public DateTime DateTime => Timestamp;

    /// <summary>
    /// Z-score of the C-O value for outlier detection
    /// </summary>
    public double StandardizedCO
    {
        get => _standardizedCO;
        set => SetProperty(ref _standardizedCO, value);
    }

    /// <summary>
    /// Alias for StandardizedCO (for export service compatibility)
    /// </summary>
    public double ZScore => StandardizedCO;

    /// <summary>
    /// Whether this point was rejected as an outlier (Z > 3)
    /// </summary>
    public bool IsRejected
    {
        get => _isRejected;
        set
        {
            if (SetProperty(ref _isRejected, value))
            {
                // Update AcceptedCO when rejection status changes
                _acceptedCO = value ? null : _co;
                OnPropertyChanged(nameof(AcceptedCO));
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    /// <summary>
    /// Point status (for export service compatibility)
    /// </summary>
    public PointStatus Status => IsRejected ? PointStatus.Rejected : PointStatus.Accepted;

    /// <summary>
    /// C-O value if accepted, null if rejected
    /// </summary>
    public double? AcceptedCO
    {
        get => _acceptedCO;
        set => SetProperty(ref _acceptedCO, value);
    }

    /// <summary>
    /// Time as fraction of day (0-1) for chart plotting
    /// </summary>
    public double TimeFraction => Timestamp.TimeOfDay.TotalDays;

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

#endregion

#region Results

/// <summary>
/// Contains all calculated results from the calibration process
/// </summary>
public class CalibrationResult : INotifyPropertyChanged
{
    private double _meanCOAll;
    private double _meanCOAccepted;
    private double _sdCOAll;
    private double _sdCOAccepted;
    private double _minReferenceHeading;
    private double _maxReferenceHeading;
    private double _minCalibratedHeading;
    private double _maxCalibratedHeading;
    private double _minCOAccepted;
    private double _maxCOAccepted;
    private DateTime _startTime;
    private DateTime _endTime;
    private int _totalObservations;
    private int _rejectedCount;
    private bool _isNewCOAccepted;
    private double? _appliedCOValue;
    private string _surveyorInitials = "";
    private string _witnessInitials = "";

    // C-O Statistics
    public double MeanCOAll
    {
        get => _meanCOAll;
        set => SetProperty(ref _meanCOAll, value);
    }

    public double MeanCOAccepted
    {
        get => _meanCOAccepted;
        set => SetProperty(ref _meanCOAccepted, value);
    }

    public double SDCOAll
    {
        get => _sdCOAll;
        set => SetProperty(ref _sdCOAll, value);
    }

    public double SDCOAccepted
    {
        get => _sdCOAccepted;
        set => SetProperty(ref _sdCOAccepted, value);
    }

    // Heading Ranges
    public double MinReferenceHeading
    {
        get => _minReferenceHeading;
        set => SetProperty(ref _minReferenceHeading, value);
    }

    public double MaxReferenceHeading
    {
        get => _maxReferenceHeading;
        set => SetProperty(ref _maxReferenceHeading, value);
    }

    public double MinCalibratedHeading
    {
        get => _minCalibratedHeading;
        set => SetProperty(ref _minCalibratedHeading, value);
    }

    public double MaxCalibratedHeading
    {
        get => _maxCalibratedHeading;
        set => SetProperty(ref _maxCalibratedHeading, value);
    }

    // C-O Range (accepted only)
    public double MinCOAccepted
    {
        get => _minCOAccepted;
        set => SetProperty(ref _minCOAccepted, value);
    }

    public double MaxCOAccepted
    {
        get => _maxCOAccepted;
        set => SetProperty(ref _maxCOAccepted, value);
    }

    // Time Range
    public DateTime StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public DateTime EndTime
    {
        get => _endTime;
        set => SetProperty(ref _endTime, value);
    }

    // Observation Counts
    public int TotalObservations
    {
        get => _totalObservations;
        set
        {
            if (SetProperty(ref _totalObservations, value))
                OnPropertyChanged(nameof(RejectionPercentage));
        }
    }

    public int RejectedCount
    {
        get => _rejectedCount;
        set
        {
            if (SetProperty(ref _rejectedCount, value))
                OnPropertyChanged(nameof(RejectionPercentage));
        }
    }

    public int AcceptedCount => TotalObservations - RejectedCount;

    public double RejectionPercentage => TotalObservations > 0
        ? (double)RejectedCount / TotalObservations * 100
        : 0;

    // Alias properties for export service compatibility
    public int TotalCount => TotalObservations;
    public double StdDevAccepted => SDCOAccepted;
    public double StdDevAll => SDCOAll;
    public double RejectionRate => RejectionPercentage;
    public double MinCO => MinCOAccepted;
    public double MaxCO => MaxCOAccepted;
    public double MinHeading => MinReferenceHeading;
    public double MaxHeading => MaxReferenceHeading;
    public double HeadingCoverage => MaxReferenceHeading - MinReferenceHeading;

    // Duration
    public TimeSpan Duration => EndTime - StartTime;

    // Iteration results
    public List<IterationResult> Iterations { get; set; } = new();

    // Decision
    public bool IsNewCOAccepted
    {
        get => _isNewCOAccepted;
        set => SetProperty(ref _isNewCOAccepted, value);
    }

    public double? AppliedCOValue
    {
        get => _appliedCOValue;
        set => SetProperty(ref _appliedCOValue, value);
    }

    public string SurveyorInitials
    {
        get => _surveyorInitials;
        set => SetProperty(ref _surveyorInitials, value);
    }

    public string WitnessInitials
    {
        get => _witnessInitials;
        set => SetProperty(ref _witnessInitials, value);
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

#endregion

#region QC Models

/// <summary>
/// Represents a single QC check result
/// </summary>
public class QcCheck
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public double? Value { get; set; }
    public double? ThresholdValue { get; set; }
    public string Unit { get; set; } = "";
    public QcStatus Status { get; set; } = QcStatus.NotChecked;
    public string StatusMessage { get; set; } = "";

    public string FormattedValue => Value.HasValue ? $"{Value:F2} {Unit}" : "N/A";
    public string FormattedThreshold => ThresholdValue.HasValue ? $"{ThresholdValue:F2} {Unit}" : "N/A";
    
    // Alias properties for export service compatibility
    public string ActualValue => FormattedValue;
    public string Threshold => FormattedThreshold;
}

/// <summary>
/// Collection of QC checks for validation step
/// </summary>
public class ValidationResult
{
    public ObservableCollection<QcCheck> Checks { get; } = new();
    
    // Decision properties
    public string Decision { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime? DecisionTime { get; set; }

    public QcStatus OverallStatus
    {
        get
        {
            if (Checks.Any(c => c.Status == QcStatus.Fail)) return QcStatus.Fail;
            if (Checks.Any(c => c.Status == QcStatus.Warning)) return QcStatus.Warning;
            if (Checks.All(c => c.Status == QcStatus.Pass)) return QcStatus.Pass;
            return QcStatus.NotChecked;
        }
    }

    public string OverallStatusText => OverallStatus switch
    {
        QcStatus.Pass => "PASS",
        QcStatus.Warning => "WARNING",
        QcStatus.Fail => "FAIL",
        _ => "NOT CHECKED"
    };

    public int PassCount => Checks.Count(c => c.Status == QcStatus.Pass);
    public int WarningCount => Checks.Count(c => c.Status == QcStatus.Warning);
    public int FailCount => Checks.Count(c => c.Status == QcStatus.Fail);
}

#endregion

#region Column Mapping

/// <summary>
/// Configuration for mapping NPD file columns to required data fields
/// </summary>
public class GyroColumnMapping : INotifyPropertyChanged
{
    private int _timeColumnIndex = -1;
    private int _referenceHeadingColumnIndex = -1;
    private int _calibratedHeadingColumnIndex = -1;
    private bool _hasDateTimeSplit = true;
    private string _dateFormat = "dd/MM/yyyy";
    private string _timeFormat = "HH:mm:ss";
    
    // String column names (for UI binding)
    private string _timeColumn = "";
    private string _referenceHeadingColumn = "";
    private string _calibratedHeadingColumn = "";
    
    public string TimeColumn
    {
        get => _timeColumn;
        set => SetProperty(ref _timeColumn, value);
    }
    
    public string ReferenceHeadingColumn
    {
        get => _referenceHeadingColumn;
        set => SetProperty(ref _referenceHeadingColumn, value);
    }
    
    public string CalibratedHeadingColumn
    {
        get => _calibratedHeadingColumn;
        set => SetProperty(ref _calibratedHeadingColumn, value);
    }

    public int TimeColumnIndex
    {
        get => _timeColumnIndex;
        set => SetProperty(ref _timeColumnIndex, value);
    }

    public int ReferenceHeadingColumnIndex
    {
        get => _referenceHeadingColumnIndex;
        set => SetProperty(ref _referenceHeadingColumnIndex, value);
    }

    public int CalibratedHeadingColumnIndex
    {
        get => _calibratedHeadingColumnIndex;
        set => SetProperty(ref _calibratedHeadingColumnIndex, value);
    }

    /// <summary>
    /// NaviPac: Header shows "Time" as 1 column, but data has "Date,Time" as 2 columns
    /// </summary>
    public bool HasDateTimeSplit
    {
        get => _hasDateTimeSplit;
        set => SetProperty(ref _hasDateTimeSplit, value);
    }

    public string DateFormat
    {
        get => _dateFormat;
        set => SetProperty(ref _dateFormat, value);
    }

    public string TimeFormat
    {
        get => _timeFormat;
        set => SetProperty(ref _timeFormat, value);
    }

    /// <summary>
    /// Validates that all required columns are mapped
    /// </summary>
    public bool IsValid =>
        TimeColumnIndex >= 0 &&
        ReferenceHeadingColumnIndex >= 0 &&
        CalibratedHeadingColumnIndex >= 0;

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

#endregion

#region Iteration Result

/// <summary>Result of a single outlier removal iteration</summary>
public class IterationResult
{
    public int IterationNumber { get; set; }
    public int PointsInIteration { get; set; }
    public int PointsRejected { get; set; }
    public double MeanCO { get; set; }
    public double StdDev { get; set; }
    public double UpperLimit { get; set; }
    public double LowerLimit { get; set; }
    public bool IsConverged { get; set; }
    public string Notes { get; set; } = "";
}

#endregion

#region QC Criteria

/// <summary>Quality control criteria for calibration</summary>
public class QcCriteria
{
    public double ZScoreThreshold { get; set; } = 3.0;
    public double MaxStdDev { get; set; } = 0.5;
    public double MaxRejectionRate { get; set; } = 10.0;
    public int MinObservations { get; set; } = 10;
    public int MaxIterations { get; set; } = 10;
}

#endregion

#region Calculation Progress

/// <summary>Progress reporting for calculation</summary>
public class CalculationProgress
{
    public int Percent { get; set; }
    public string Message { get; set; } = "";
    public int Iteration { get; set; }
}

#endregion

#region File Preview

/// <summary>Information about a file preview</summary>
public class FilePreviewInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public int TotalRows { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<string[]> PreviewRows { get; set; } = new();
}

#endregion

#region Column Mapping

#endregion
