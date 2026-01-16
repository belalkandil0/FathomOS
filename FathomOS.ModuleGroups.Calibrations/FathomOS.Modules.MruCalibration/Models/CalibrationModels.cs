using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.MruCalibration.Models;

#region Enums

/// <summary>
/// Type of sensor being calibrated
/// </summary>
public enum SensorType
{
    Pitch,
    Roll
}

/// <summary>
/// Purpose of the calibration exercise
/// </summary>
public enum CalibrationPurpose
{
    /// <summary>
    /// Determine the C-O value of the system being calibrated
    /// Reference system has C-O applied, calibrated system has C-O = 0
    /// </summary>
    Calibration,
    
    /// <summary>
    /// Verify that the C-O of the system is still valid
    /// Both systems have their known C-O values applied
    /// </summary>
    Verification
}

/// <summary>
/// Status of a data point after QC processing
/// </summary>
public enum PointStatus
{
    Pending,
    Accepted,
    Rejected
}

/// <summary>
/// Result of the calibration acceptance decision
/// </summary>
public enum AcceptanceDecision
{
    NotDecided,
    Accepted,
    Rejected
}

#endregion

#region MRU System Info

/// <summary>
/// Information about an MRU system
/// </summary>
public class MruSystemInfo
{
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    
    public bool IsValid => !string.IsNullOrWhiteSpace(Model);
    
    public override string ToString() => 
        string.IsNullOrWhiteSpace(SerialNumber) ? Model : $"{Model} (S/N: {SerialNumber})";
}

#endregion

#region Project Info

/// <summary>
/// Project and survey information
/// </summary>
public class ProjectInfo
{
    // Project Details
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectNumber { get; set; } = string.Empty;
    
    // Survey Details
    public DateTime? PitchDateTime { get; set; }
    public DateTime? RollDateTime { get; set; }
    
    // Personnel
    public string ObservedBy { get; set; } = string.Empty;
    public string CheckedBy { get; set; } = string.Empty;
    
    // Location
    public string VesselName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Latitude { get; set; } = string.Empty;
    
    // MRU Systems
    public MruSystemInfo ReferenceSystem { get; set; } = new();
    public MruSystemInfo VerifiedSystem { get; set; } = new();
    
    // Calibration Settings
    public CalibrationPurpose Purpose { get; set; } = CalibrationPurpose.Verification;
    
    public bool IsValid => 
        !string.IsNullOrWhiteSpace(ProjectTitle) && 
        !string.IsNullOrWhiteSpace(VesselName) &&
        ReferenceSystem.IsValid && 
        VerifiedSystem.IsValid;
}

#endregion

#region Data Points

/// <summary>
/// Single MRU calibration data point
/// </summary>
public class MruDataPoint
{
    // Raw Data
    public int Index { get; set; }
    public DateTime Timestamp { get; set; }
    public double ReferenceValue { get; set; }
    public double VerifiedValue { get; set; }
    
    // Calculated Values
    public double CO { get; set; }                    // Computed - Observed (Reference - Verified)
    public double StandardizedCO { get; set; }       // Z-score
    public PointStatus Status { get; set; } = PointStatus.Pending;
    
    // For plotting rejected points
    public double? RejectedPlotValue => Status == PointStatus.Rejected ? CO : null;
    public double? AcceptedCO => Status == PointStatus.Accepted ? CO : null;
    
    /// <summary>
    /// Calculate C-O and Z-score
    /// </summary>
    public void Calculate(double meanCO, double stdDevCO)
    {
        CO = ReferenceValue - VerifiedValue;
        
        if (stdDevCO > 0)
        {
            StandardizedCO = Math.Abs((CO - meanCO) / stdDevCO);
        }
        else
        {
            StandardizedCO = 0;
        }
    }
    
    /// <summary>
    /// Apply 3-sigma rejection rule
    /// </summary>
    public void ApplyRejection(double threshold = 3.0)
    {
        Status = StandardizedCO > threshold ? PointStatus.Rejected : PointStatus.Accepted;
    }
}

#endregion

#region Statistics

/// <summary>
/// Statistical results for a calibration dataset
/// </summary>
public class CalibrationStatistics
{
    // All Points
    public double MeanCO_All { get; set; }
    public double StdDevCO_All { get; set; }
    
    // Accepted Points Only
    public double MeanCO_Accepted { get; set; }
    public double StdDevCO_Accepted { get; set; }
    
    // Enhanced Statistics (v2.3.0)
    public double RMSE { get; set; }                      // Root Mean Square Error
    public double ConfidenceInterval95_Lower { get; set; } // 95% CI lower bound
    public double ConfidenceInterval95_Upper { get; set; } // 95% CI upper bound
    public double ShapiroWilkW { get; set; }              // Shapiro-Wilk W statistic
    public double ShapiroWilkPValue { get; set; }         // Shapiro-Wilk p-value
    public bool IsNormallyDistributed { get; set; }       // p > 0.05
    public double Skewness { get; set; }                  // Distribution skewness
    public double Kurtosis { get; set; }                  // Distribution kurtosis
    public double Median { get; set; }                    // Median C-O value
    public double Q1 { get; set; }                        // First quartile
    public double Q3 { get; set; }                        // Third quartile
    public double IQR => Q3 - Q1;                         // Interquartile range
    
    // Ranges
    public double MinReferenceValue { get; set; }
    public double MaxReferenceValue { get; set; }
    public double MinVerifiedValue { get; set; }
    public double MaxVerifiedValue { get; set; }
    public double MinCO { get; set; }
    public double MaxCO { get; set; }
    
    // Time Range
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    
    // Counts
    public int TotalObservations { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public double RejectionPercentage => TotalObservations > 0 
        ? (RejectedCount * 100.0 / TotalObservations) 
        : 0;
    
    // Final Result
    public double FinalCO => MeanCO_Accepted;
    public double FinalSD => StdDevCO_Accepted;
    
    // Display helpers
    public string ConfidenceInterval95Display => 
        $"{ConfidenceInterval95_Lower:F4}° to {ConfidenceInterval95_Upper:F4}°";
    public string NormalityDisplay => 
        IsNormallyDistributed ? "Normal (p > 0.05)" : "Non-Normal (p ≤ 0.05)";
}

#endregion

#region Sensor Data Container

/// <summary>
/// Container for all data related to a single sensor (Pitch or Roll)
/// </summary>
public class SensorCalibrationData
{
    public SensorType SensorType { get; set; }
    public List<MruDataPoint> DataPoints { get; set; } = new();
    public CalibrationStatistics Statistics { get; set; } = new();
    
    // Column mapping from NPD file
    public string TimeColumnName { get; set; } = string.Empty;
    public string ReferenceColumnName { get; set; } = string.Empty;
    public string VerifiedColumnName { get; set; } = string.Empty;
    
    // Processing state
    public bool IsLoaded { get; set; }
    public bool IsProcessed { get; set; }
    
    // Sign-off
    public AcceptanceDecision Decision { get; set; } = AcceptanceDecision.NotDecided;
    public double? AcceptedCOValue { get; set; }
    public string SurveyorInitials { get; set; } = string.Empty;
    public string WitnessInitials { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public DateTime? SignOffDateTime { get; set; }
    
    public int PointCount => DataPoints.Count;
    public bool HasData => DataPoints.Count > 0;
}

#endregion

#region Calibration Session

/// <summary>
/// Complete calibration session - can be saved/loaded
/// </summary>
public class CalibrationSession
{
    // Session metadata
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ModifiedAt { get; set; }
    public string? FilePath { get; set; }
    
    // Project information
    public ProjectInfo ProjectInfo { get; set; } = new();
    
    // Sensor data
    public SensorCalibrationData PitchData { get; set; } = new() { SensorType = SensorType.Pitch };
    public SensorCalibrationData RollData { get; set; } = new() { SensorType = SensorType.Roll };
    
    // Source file
    public string? SourceNpdFilePath { get; set; }
    
    // Processing settings
    public double RejectionThreshold { get; set; } = 3.0;  // 3-sigma default
    
    // Workflow state
    public int CurrentStep { get; set; } = 1;
    public bool[] StepCompleted { get; set; } = new bool[7];
    
    // Validation
    public bool HasPitchData => PitchData.HasData;
    public bool HasRollData => RollData.HasData;
    public bool HasAnyData => HasPitchData || HasRollData;
    public bool IsComplete => StepCompleted.All(s => s);
    
    /// <summary>
    /// Mark session as modified
    /// </summary>
    public void MarkModified()
    {
        ModifiedAt = DateTime.Now;
    }
    
    /// <summary>
    /// Get sensor data by type
    /// </summary>
    public SensorCalibrationData GetSensorData(SensorType type) =>
        type == SensorType.Pitch ? PitchData : RollData;
}

#endregion

#region Column Mapping

/// <summary>
/// Column mapping configuration for NPD file parsing
/// </summary>
public class MruColumnMapping
{
    public string TimeColumn { get; set; } = string.Empty;
    public string ReferencePitchColumn { get; set; } = string.Empty;
    public string ReferenceRollColumn { get; set; } = string.Empty;
    public string VerifiedPitchColumn { get; set; } = string.Empty;
    public string VerifiedRollColumn { get; set; } = string.Empty;
    
    public bool HasDateTimeSplit { get; set; } = true;  // NaviPac default
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    
    public bool IsValidForPitch => 
        !string.IsNullOrEmpty(TimeColumn) && 
        !string.IsNullOrEmpty(ReferencePitchColumn) && 
        !string.IsNullOrEmpty(VerifiedPitchColumn);
    
    public bool IsValidForRoll => 
        !string.IsNullOrEmpty(TimeColumn) && 
        !string.IsNullOrEmpty(ReferenceRollColumn) && 
        !string.IsNullOrEmpty(VerifiedRollColumn);
}

#endregion

#region Report Options

/// <summary>
/// Options for report generation
/// </summary>
public class ReportOptions
{
    public bool IncludePitchReport { get; set; } = true;
    public bool IncludeRollReport { get; set; } = true;
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeDataTable { get; set; } = true;
    public bool IncludeStatistics { get; set; } = true;
    public bool IncludeSignOff { get; set; } = true;
    
    // Branding
    public bool UseSubsea7Branding { get; set; } = true;
    public string? CustomLogoPath { get; set; }
}

/// <summary>
/// Represents a row in the data preview grid with all calculated values
/// Enhanced in v2.3.0 with full statistics display
/// </summary>
public class DataPreviewRow
{
    public int Index { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Pitch Data
    public double? ReferencePitch { get; set; }
    public double? VerifiedPitch { get; set; }
    public double? PitchCO { get; set; }
    public double? PitchZScore { get; set; }
    public PointStatus PitchStatus { get; set; } = PointStatus.Pending;
    
    // Roll Data
    public double? ReferenceRoll { get; set; }
    public double? VerifiedRoll { get; set; }
    public double? RollCO { get; set; }
    public double? RollZScore { get; set; }
    public PointStatus RollStatus { get; set; } = PointStatus.Pending;
    
    // Legacy properties for backwards compatibility
    public double? PitchDiff => PitchCO;
    public double? RollDiff => RollCO;
    public bool IsSelected { get; set; } = true;
    
    // Display Properties
    public string TimestampDisplay => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
    public string ReferencePitchDisplay => ReferencePitch?.ToString("F4") ?? "-";
    public string VerifiedPitchDisplay => VerifiedPitch?.ToString("F4") ?? "-";
    public string PitchCODisplay => PitchCO?.ToString("F4") ?? "-";
    public string PitchDiffDisplay => PitchCODisplay; // Backward compatibility
    public string PitchZScoreDisplay => PitchZScore?.ToString("F2") ?? "-";
    public string PitchStatusDisplay => PitchStatus.ToString();
    
    public string ReferenceRollDisplay => ReferenceRoll?.ToString("F4") ?? "-";
    public string VerifiedRollDisplay => VerifiedRoll?.ToString("F4") ?? "-";
    public string RollCODisplay => RollCO?.ToString("F4") ?? "-";
    public string RollDiffDisplay => RollCODisplay; // Backward compatibility
    public string RollZScoreDisplay => RollZScore?.ToString("F2") ?? "-";
    public string RollStatusDisplay => RollStatus.ToString();
    
    // Status colors (for UI binding)
    public string PitchStatusColor => PitchStatus switch
    {
        PointStatus.Accepted => "#4CAF50",
        PointStatus.Rejected => "#F44336",
        _ => "#808080"
    };
    
    public string RollStatusColor => RollStatus switch
    {
        PointStatus.Accepted => "#4CAF50",
        PointStatus.Rejected => "#F44336",
        _ => "#808080"
    };
}

/// <summary>
/// Step help content
/// </summary>
public static class StepHelpContent
{
    public static readonly Dictionary<int, (string Title, string Description)> HelpContent = new()
    {
        { 1, ("Step 1: Select Calibration Type", 
            "Choose the purpose of this session:\n\n" +
            "• CALIBRATION: Use when establishing initial sensor offsets or after maintenance. " +
            "Results will determine calibration values to be applied.\n\n" +
            "• VERIFICATION: Use to confirm existing calibration is still valid. " +
            "Results will be compared against acceptance criteria.") },
        
        { 2, ("Step 2: Load Data File", 
            "Load your NPD survey data file and configure column mapping:\n\n" +
            "1. Click 'Browse' to select your NPD or CSV file\n" +
            "2. Use 'Auto-Detect' to automatically find MRU columns\n" +
            "3. Manually adjust column selections if needed\n" +
            "4. Enable 'Date/Time Split' for NaviPac files\n\n" +
            "TIP: Column names containing 'Pitch', 'Roll', 'MRU', 'Motion' are auto-detected.") },
        
        { 3, ("Step 3: Configure Session", 
            "Enter project details and MRU system information:\n\n" +
            "• Project Name, Client, Vessel - for report headers\n" +
            "• Reference MRU - the trusted/primary sensor\n" +
            "• Verified MRU - the sensor being calibrated/verified\n" +
            "• Personnel names - for sign-off traceability\n\n" +
            "You can also review and filter the loaded data time range.") },
        
        { 4, ("Step 4: Process Data", 
            "Run the calibration calculation with 3-sigma rejection:\n\n" +
            "1. Set rejection threshold (default: 3.0 sigma)\n" +
            "2. Click 'Process Data' to run calculations\n" +
            "3. Review processing log for iterations\n" +
            "4. Optionally adjust threshold and reprocess\n\n" +
            "The algorithm iteratively removes outliers until stable.") },
        
        { 5, ("Step 5: Analyze Results", 
            "Review calibration charts and statistics:\n\n" +
            "• Calibration Chart: Time series of Reference vs Verified values\n" +
            "• C-O Chart: Scatter plot of differences with sigma boundaries\n" +
            "• Histogram: Distribution of accepted C-O values\n\n" +
            "Green points = Accepted, Red points = Rejected") },
        
        { 6, ("Step 6: Verify & Sign-Off", 
            "Complete QC checks and sign-off:\n\n" +
            "1. Review QC checklist items\n" +
            "2. Enter your initials and comments\n" +
            "3. Accept or Reject each sensor's results\n\n" +
            "All QC checks must pass before sign-off is enabled.") },
        
        { 7, ("Step 7: Export Reports", 
            "Generate and save calibration reports:\n\n" +
            "• PDF Report: Formal document with charts and sign-off\n" +
            "• Excel Export: Detailed data for further analysis\n" +
            "• Export All: Generate both formats\n\n" +
            "Reports include all session data, statistics, and sign-off.") }
    };
}

#endregion
