namespace FathomOS.Modules.RovGyroCalibration.Models;

using System.Collections.ObjectModel;
using System.ComponentModel;

#region Enums

/// <summary>Length/distance unit for measurements</summary>
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

public enum CalibrationPurpose { Calibration, Verification }
public enum PointStatus { Pending, Accepted, Rejected }
public enum QcStatus { NotChecked, Pass, Warning, Fail }

/// <summary>ROV facing direction relative to vessel</summary>
public enum RovFacingDirection
{
    Forward,    // ROV faces vessel bow (0°)
    Aft,        // ROV faces vessel stern (+180°)
    Port,       // ROV faces port (-90°)
    Starboard   // ROV faces starboard (+90°)
}

/// <summary>Which side of vessel the baseline is on</summary>
public enum BaselineSide { Port, Starboard, Forward, Aft }

/// <summary>Baseline orientation relative to vessel centerline</summary>
public enum BaselineOrientation { Parallel, Perpendicular }

#endregion

#region ROV Configuration

/// <summary>ROV geometric setup configuration</summary>
public class RovConfiguration
{
    public RovFacingDirection FacingDirection { get; set; } = RovFacingDirection.Forward;
    public BaselineSide BaselineSide { get; set; } = BaselineSide.Port;
    public BaselineOrientation BaselineOrientation { get; set; } = BaselineOrientation.Parallel;
    
    /// <summary>Baseline angular offset D (degrees)</summary>
    public double BaselineOffset { get; set; } = 0.0;
    
    /// <summary>Port baseline measurement P (meters)</summary>
    public double PortMeasurement { get; set; } = 0.0;
    
    /// <summary>Starboard baseline measurement S (meters)</summary>
    public double StarboardMeasurement { get; set; } = 0.0;
    
    /// <summary>Forward baseline measurement (meters) - Alternative to Port/Stbd</summary>
    public double FwdMeasurement { get; set; } = 0.0;
    
    /// <summary>Aft baseline measurement (meters) - Alternative to Port/Stbd</summary>
    public double AftMeasurement { get; set; } = 0.0;
    
    /// <summary>Baseline distance for angle calculation (meters)</summary>
    public double BaselineDistance { get; set; } = 10.0;
    
    /// <summary>Whether to use Fwd/Aft inputs instead of Port/Stbd</summary>
    public bool UseFwdAftMeasurements { get; set; } = true;
    
    /// <summary>Get facing direction offset in degrees</summary>
    public double FacingDirectionOffset => FacingDirection switch
    {
        RovFacingDirection.Forward => 0.0,
        RovFacingDirection.Aft => 180.0,
        RovFacingDirection.Port => -90.0,
        RovFacingDirection.Starboard => 90.0,
        _ => 0.0
    };
    
    /// <summary>Calculate baseline angle θ from measurements</summary>
    public double CalculateBaselineAngle()
    {
        if (BaselineDistance <= 0) return 0;
        
        double diff;
        if (UseFwdAftMeasurements)
        {
            // Fwd/Aft measurements (baseline parallel to keel)
            diff = FwdMeasurement - AftMeasurement;
        }
        else
        {
            // Port/Stbd measurements (baseline perpendicular to keel)
            diff = PortMeasurement - StarboardMeasurement;
        }
        
        return Math.Atan(diff / BaselineDistance) * (180.0 / Math.PI);
    }
    
    /// <summary>Get current difference value based on measurement mode</summary>
    public double MeasurementDifference => UseFwdAftMeasurements 
        ? (FwdMeasurement - AftMeasurement) 
        : (PortMeasurement - StarboardMeasurement);
}

#endregion

#region Project

public class CalibrationProject
{
    // Project Info
    public string ProjectName { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string VesselName { get; set; } = "";
    public string RovName { get; set; } = "";
    public DateTime SurveyDate { get; set; } = DateTime.Today;
    public string SurveyorName { get; set; } = "";
    
    // Calibration Settings
    public CalibrationPurpose Purpose { get; set; } = CalibrationPurpose.Calibration;
    public double ExistingCO { get; set; } = 0.0;
    
    // Display Unit
    public LengthUnit DisplayUnit { get; set; } = LengthUnit.Meters;
    
    // Rounding Options
    public bool RoundingEnabled { get; set; } = true;
    public int RoundingDecimalPlaces { get; set; } = 4;
    
    // ROV Configuration
    public RovConfiguration RovConfig { get; set; } = new();
    
    // Equipment
    public string RovGyroModel { get; set; } = "";
    public string RovGyroSerial { get; set; } = "";
    public string ReferenceGyroModel { get; set; } = "";
    public string ReferenceGyroSerial { get; set; } = "";
    
    // QC Criteria
    public QcCriteria QcCriteria { get; set; } = new();
    
    public string PurposeText => Purpose == CalibrationPurpose.Calibration ? "Calibration Mode" : "Verification Mode";
    
    public bool IsValid => !string.IsNullOrWhiteSpace(ProjectName) && 
                           !string.IsNullOrWhiteSpace(VesselName) &&
                           !string.IsNullOrWhiteSpace(RovName);
}

#endregion

#region Data Points

/// <summary>ROV gyro data point with geometric corrections</summary>
public class RovGyroDataPoint
{
    public int Index { get; set; }
    public DateTime DateTime { get; set; }
    
    // Alias for compatibility
    public DateTime Timestamp { get => DateTime; set => DateTime = value; }
    
    // Raw values
    public double VesselHeading { get; set; }      // Reference vessel heading
    public double RovHeading { get; set; }          // Observed ROV heading
    
    // Geometric correction components
    public double BaselineOffset { get; set; }      // D
    public double BaselineAngle { get; set; }       // θ  
    public double FacingDirectionOffset { get; set; } // Facing correction
    
    // Geometric correction steps (from Excel formulas)
    public double VPlusD { get; set; }              // V + D (vessel + baseline offset)
    public double VPlusDPlusTheta { get; set; }     // V + D + θ (with baseline angle)
    public double CorrectedReference { get; set; }  // C = final corrected reference heading
    public double CalculatedHeading { get; set; }   // Calculated expected heading
    
    // Results
    public double CalculatedCO { get; set; }        // C-O = CorrectedReference - RovHeading
    public double ZScore { get; set; }
    public PointStatus Status { get; set; } = PointStatus.Pending;
    public bool IsRejected { get; set; }
    public string? RejectionReason { get; set; }
    
    // Additional data
    public Dictionary<string, object> RawData { get; set; } = new();
}

#endregion

#region Results

public class CalibrationResult
{
    // All observations
    public int TotalCount { get; set; }
    public double MeanCOAll { get; set; }
    public double StdDevAll { get; set; }
    public double MinCO { get; set; }
    public double MaxCO { get; set; }
    
    // Accepted only
    public int AcceptedCount { get; set; }
    public double MeanCOAccepted { get; set; }
    public double StdDevAccepted { get; set; }
    public double MinCOAccepted { get; set; }
    public double MaxCOAccepted { get; set; }
    
    // Rejected
    public int RejectedCount { get; set; }
    public double RejectionRate => TotalCount > 0 ? (RejectedCount * 100.0 / TotalCount) : 0;
    
    // Aliases for compatibility
    public int TotalObservations => TotalCount;
    public double RejectionPercentage => RejectionRate;
    public double MinReferenceHeading => MinHeading;
    public double MaxReferenceHeading => MaxHeading;
    
    // Heading coverage
    public double MinHeading { get; set; }
    public double MaxHeading { get; set; }
    public double HeadingCoverage { get; set; }
    
    // Time range
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    // Geometric corrections applied
    public double BaselineAngleTheta { get; set; }
    public double BaselineOffsetD { get; set; }
    public double FacingDirectionOffset { get; set; }
    
    // Iteration results
    public List<IterationResult> Iterations { get; set; } = new();
    
    // Decision
    public bool IsNewCOAccepted { get; set; }
    public double? AppliedCOValue { get; set; }
    public string SurveyorInitials { get; set; } = "";
    public string WitnessInitials { get; set; } = "";
}

public class ValidationResult
{
    public ObservableCollection<QcCheck> Checks { get; } = new();
    
    // Decision properties
    public string Decision { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime? DecisionTime { get; set; }
    public string? DecidedBy { get; set; }

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

public class QcCheck
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public QcStatus Status { get; set; } = QcStatus.NotChecked;
    public string ActualValue { get; set; } = "";
    public string Threshold { get; set; } = "";
}

#endregion

#region Column Mapping

public class RovGyroColumnMapping
{
    public string Name { get; set; } = "ROV Gyro Calibration";
    
    // String column names (for UI binding)
    public string TimeColumn { get; set; } = "";
    public string VesselHeadingColumn { get; set; } = "";
    public string RovHeadingColumn { get; set; } = "";
    
    // Column indices (-1 = not set)
    public int TimeColumnIndex { get; set; } = -1;
    public int VesselHeadingColumnIndex { get; set; } = -1;
    public int RovHeadingColumnIndex { get; set; } = -1;
    
    // Optional baseline measurements (if in data file)
    public int PortMeasurementColumnIndex { get; set; } = -1;
    public int StarboardMeasurementColumnIndex { get; set; } = -1;
    
    // NaviPac date/time handling
    public bool HasDateTimeSplit { get; set; } = true;
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    
    public bool IsValid => TimeColumnIndex >= 0 && 
                          VesselHeadingColumnIndex >= 0 && 
                          RovHeadingColumnIndex >= 0;
}

#endregion

#region File Preview

public class FilePreviewInfo
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int TotalRows { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<string[]> PreviewRows { get; set; } = new();
}

#endregion

#region Geometric Config (for 3D visualization)

/// <summary>Configuration for ROV geometric setup visualization</summary>
public class RovGeometricConfig
{
    public double VesselHeading { get; set; } = 0;
    public double RovHeading { get; set; } = 0;
    public double BaselineAngle { get; set; } = 0;        // θ
    public double BaselineOffset { get; set; } = 0;        // D
    public double BaselineDistance { get; set; } = 10.0;   // Distance between baseline points
    public RovFacingDirection FacingDirection { get; set; } = RovFacingDirection.Forward;
    public BaselineSide BaselineSide { get; set; } = BaselineSide.Port;
    
    public double FacingDirectionOffset => FacingDirection switch
    {
        RovFacingDirection.Forward => 0.0,
        RovFacingDirection.Aft => 180.0,
        RovFacingDirection.Port => -90.0,
        RovFacingDirection.Starboard => 90.0,
        _ => 0.0
    };
    
    /// <summary>C = V + D + θ + FacingDirectionOffset</summary>
    public double CalculatedReference => VesselHeading + BaselineOffset + BaselineAngle + FacingDirectionOffset;
    
    /// <summary>C-O difference</summary>
    public double CODifference => CalculatedReference - RovHeading;
}

#endregion

#region Iteration Result (for processing iterations)

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
