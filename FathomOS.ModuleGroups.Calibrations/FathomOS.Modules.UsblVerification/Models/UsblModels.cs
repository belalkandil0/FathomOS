using System;
using System.Collections.Generic;
using System.Linq;
using FathomOS.Modules.UsblVerification.Services;

namespace FathomOS.Modules.UsblVerification.Models;

/// <summary>
/// Chart color theme options
/// </summary>
public enum ChartTheme
{
    Professional,    // Blue/gray professional look
    Vibrant,         // Bright colorful
    Ocean,           // Blue/teal ocean theme
    Earth,           // Brown/green earth tones
    Monochrome,      // Grayscale
    HighContrast     // High visibility
}

/// <summary>
/// Represents a single USBL observation point
/// </summary>
public class UsblObservation
{
    public int Index { get; set; }
    public DateTime DateTime { get; set; }
    public DateTime Timestamp => DateTime;  // Alias for XAML binding
    public double VesselGyro { get; set; }
    public double Gyro => VesselGyro;  // Alias for XAML binding
    public double VesselEasting { get; set; }
    public double VesselNorthing { get; set; }
    public double TransponderEasting { get; set; }
    public double TransponderNorthing { get; set; }
    public double TransponderDepth { get; set; }
    
    // Calculated deltas from average
    public double DeltaEasting { get; set; }
    public double DeltaNorthing { get; set; }
    public double DeltaDepth { get; set; }
    
    // For transit data - vessel offset from transponder
    public double VesselOffsetEasting { get; set; }
    public double VesselOffsetNorthing { get; set; }
    public double VesselOffsetDistance { get; set; }
    public double VesselOffsetSign { get; set; }
    
    public bool IsExcluded { get; set; }
    
    /// <summary>
    /// Marked as statistical outlier (spike) but not yet excluded
    /// </summary>
    public bool IsOutlier { get; set; }
    
    /// <summary>
    /// Radial distance from mean position (calculated during outlier detection)
    /// </summary>
    public double RadialFromMean { get; set; }
    
    /// <summary>
    /// Source file path for this observation
    /// </summary>
    public string SourceFile { get; set; } = "";
    
    // Smoothed values (populated by SmoothingService)
    public double? SmoothedEasting { get; set; }
    public double? SmoothedNorthing { get; set; }
    public double? SmoothedDepth { get; set; }
    
    // Best values (smoothed if available, otherwise raw)
    public double BestEasting => SmoothedEasting ?? TransponderEasting;
    public double BestNorthing => SmoothedNorthing ?? TransponderNorthing;
    public double BestDepth => SmoothedDepth ?? TransponderDepth;
}

/// <summary>
/// Statistics for a single test (spin heading or transit line)
/// </summary>
public class TestStatistics
{
    public string Name { get; set; } = "";
    public double AverageGyro { get; set; }
    public double AverageVesselEasting { get; set; }
    public double AverageVesselNorthing { get; set; }
    public double AverageTransponderEasting { get; set; }
    public double AverageTransponderNorthing { get; set; }
    public double AverageDepth { get; set; }
    
    public double StdDevEasting2Sigma { get; set; }
    public double StdDevNorthing2Sigma { get; set; }
    public double StdDevDepth2Sigma { get; set; }
    
    public double TransducerOffset { get; set; }
    public double SlantRange { get; set; }
    public double ToleranceValue { get; set; }
    
    public int PointCount { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    // For transit
    public double TransitLength { get; set; }
    public double TransitDirection { get; set; }
}

/// <summary>
/// Data for a single spin test (one vessel heading)
/// </summary>
public class SpinTestData
{
    public string Name { get; set; } = "";
    public double NominalHeading { get; set; }
    public List<UsblObservation> Observations { get; set; } = new();
    public TestStatistics Statistics { get; set; } = new();
    public bool HasData => Observations.Any(o => !o.IsExcluded);
    public string SourceFile { get; set; } = "";
    
    /// <summary>
    /// Calculate actual heading from average gyro readings in observations
    /// </summary>
    public double ActualHeading
    {
        get
        {
            var validObs = Observations.Where(o => !o.IsExcluded).ToList();
            if (!validObs.Any()) return NominalHeading;
            
            // Use circular mean for heading (handles 359° to 1° wraparound)
            double sinSum = 0, cosSum = 0;
            foreach (var obs in validObs)
            {
                double rad = obs.VesselGyro * Math.PI / 180.0;
                sinSum += Math.Sin(rad);
                cosSum += Math.Cos(rad);
            }
            double avgRad = Math.Atan2(sinSum, cosSum);
            double avgDeg = avgRad * 180.0 / Math.PI;
            if (avgDeg < 0) avgDeg += 360;
            return Math.Round(avgDeg, 1);
        }
    }
    
    /// <summary>
    /// Display name showing actual heading (e.g., "Spin 45.2°" instead of "Spin 0°")
    /// </summary>
    public string DisplayName => $"Spin {ActualHeading:F1}°";
}

/// <summary>
/// Data for a transit test line
/// </summary>
public class TransitTestData
{
    public string Name { get; set; } = "";
    public int LineNumber { get; set; }
    public List<UsblObservation> Observations { get; set; } = new();
    public TestStatistics Statistics { get; set; } = new();
    public bool HasData => Observations.Any(o => !o.IsExcluded);
    public string SourceFile { get; set; } = "";
    
    // Alignment calculations
    public double ResidualAlignment { get; set; }
    public double ScaleFactor { get; set; } = 1.0;
}

/// <summary>
/// Complete USBL verification project data
/// </summary>
public class UsblVerificationProject
{
    // Project Info
    public string ProjectName { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string VesselName { get; set; } = "";
    public string TransponderName { get; set; } = "";
    public string TransponderId { get; set; } = "";
    public string TransponderSerial { get; set; } = "";
    public string TransponderModel { get; set; } = "";
    public string TransponderFrequency { get; set; } = "";
    public double NominalDepth { get; set; }
    public string TransducerLocation { get; set; } = ""; // Port/Starboard
    public DateTime? SurveyDate { get; set; }
    public string SurveyorName { get; set; } = "";
    public string ProcessorName { get; set; } = "";
    public string Comments { get; set; } = "";
    
    // USBL System Info
    public string UsblModel { get; set; } = "";
    public string UsblSerialNumber { get; set; } = "";
    
    // Project file path (for saving/loading)
    public string ProjectFilePath { get; set; } = "";
    
    // Spin Tests (4 headings)
    public SpinTestData Spin000 { get; set; } = new() { Name = "Spin 0°", NominalHeading = 0 };
    public SpinTestData Spin090 { get; set; } = new() { Name = "Spin 90°", NominalHeading = 90 };
    public SpinTestData Spin180 { get; set; } = new() { Name = "Spin 180°", NominalHeading = 180 };
    public SpinTestData Spin270 { get; set; } = new() { Name = "Spin 270°", NominalHeading = 270 };
    
    // Transit Tests (2 lines)
    public TransitTestData Transit1 { get; set; } = new() { Name = "Transit Line 1", LineNumber = 1 };
    public TransitTestData Transit2 { get; set; } = new() { Name = "Transit Line 2", LineNumber = 2 };
    
    // Settings
    public LengthUnit InputUnit { get; set; } = LengthUnit.Meters;
    public LengthUnit OutputUnit { get; set; } = LengthUnit.Meters;
    public ChartTheme ChartTheme { get; set; } = ChartTheme.Professional;
    
    public double SpinToleranceFixed { get; set; } = 0.5; // meters
    public double SpinTolerancePercent { get; set; } = 0.002; // 0.2%
    public double TransitToleranceFixed { get; set; } = 1.0; // meters
    public double TransitTolerancePercent { get; set; } = 0.005; // 0.5%
    public double AlignmentTolerance { get; set; } = 0.1; // degrees
    public double ScaleFactorTolerance { get; set; } = 0.005; // 0.5%
    
    /// <summary>
    /// Primary tolerance value (spin test tolerance in meters)
    /// </summary>
    public double Tolerance
    {
        get => SpinToleranceFixed;
        set => SpinToleranceFixed = value;
    }
    
    public double SpinGraphZoom { get; set; } = 0;
    public double TransitGraphZoom { get; set; } = 0;
    public double DotSize { get; set; } = 3.0;
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    
    public IEnumerable<SpinTestData> AllSpinTests => new[] { Spin000, Spin090, Spin180, Spin270 };
    public IEnumerable<TransitTestData> AllTransitTests => new[] { Transit1, Transit2 };
    
    public bool HasSpinData => AllSpinTests.Any(s => s.HasData);
    public bool HasTransitData => AllTransitTests.Any(t => t.HasData);
}

/// <summary>
/// Aggregated verification results
/// </summary>
public class VerificationResults
{
    // Spin Results
    public double SpinOverallAverageEasting { get; set; }
    public double SpinOverallAverageNorthing { get; set; }
    public double SpinOverallAverageDepth { get; set; }
    
    public double SpinStdDevEasting2Sigma { get; set; }
    public double SpinStdDevNorthing2Sigma { get; set; }
    public double SpinStdDevDepth2Sigma { get; set; }
    
    public double SpinMaxDiffEasting { get; set; }
    public double SpinMaxDiffNorthing { get; set; }
    public double SpinMaxDiffDepth { get; set; }
    public double SpinMaxDiffRadial { get; set; }
    
    public double Spin2DRMS { get; set; }
    public double SpinMaxAllowableDiff { get; set; }
    public bool SpinPassFail { get; set; }
    
    // Computed spin properties for ViewModel bindings
    public TransponderPosition SpinMeanPosition => new TransponderPosition
    {
        Easting = SpinOverallAverageEasting,
        Northing = SpinOverallAverageNorthing,
        Depth = SpinOverallAverageDepth
    };
    
    public double SpinMeanOffset => SpinMaxDiffRadial;
    public double SpinStdDev => Math.Sqrt(SpinStdDevEasting2Sigma * SpinStdDevEasting2Sigma + 
                                          SpinStdDevNorthing2Sigma * SpinStdDevNorthing2Sigma) / 2;
    
    // Transit Results
    public double TransitCombinedAverageEasting { get; set; }
    public double TransitCombinedAverageNorthing { get; set; }
    public double TransitCombinedAverageDepth { get; set; }
    
    public double TransitMaxDiffBetweenLines { get; set; }
    public double TransitMaxDiffFromSpin { get; set; }
    public double TransitMaxDiffRadial { get; set; }
    
    public double TransitStdDevEasting2Sigma { get; set; }
    public double TransitStdDevNorthing2Sigma { get; set; }
    
    public double Transit2DRMS { get; set; }
    public double TransitMaxAllowableSpread { get; set; }
    public bool TransitPassFail { get; set; }
    
    // Computed transit properties for ViewModel bindings
    public double TransitMeanOffset => TransitMaxDiffFromSpin;
    public double TransitStdDev => Math.Sqrt(TransitStdDevEasting2Sigma * TransitStdDevEasting2Sigma + 
                                             TransitStdDevNorthing2Sigma * TransitStdDevNorthing2Sigma) / 2;
    public double TransitMaxOffset => TransitMaxDiffRadial;
    
    // Absolute Position Check
    public double AbsolutePositionDiffEasting { get; set; }
    public double AbsolutePositionDiffNorthing { get; set; }
    public double AbsolutePositionDiffDepth { get; set; }
    public double AbsolutePositionRange { get; set; }
    public double AbsolutePositionBearing { get; set; }
    
    // Alignment Results
    public double Line1ResidualAlignment { get; set; }
    public double Line2ResidualAlignment { get; set; }
    public bool Line1AlignmentPass { get; set; }
    public bool Line2AlignmentPass { get; set; }
    
    public double Line1ScaleFactor { get; set; } = 1.0;
    public double Line2ScaleFactor { get; set; } = 1.0;
    public bool Line1ScalePass { get; set; } = true;
    public bool Line2ScalePass { get; set; } = true;
    
    // Per-heading results for display
    public List<HeadingResult> HeadingResults { get; set; } = new();
    
    // Quality Score (0-100)
    public double QualityScore { get; set; } = 100;
    
    /// <summary>
    /// Whether transit test was performed (has valid data)
    /// </summary>
    public bool TransitWasPerformed { get; set; }
    
    // Overall - Transit is optional. If skipped, only spin determines pass/fail
    public bool OverallPass => TransitWasPerformed 
        ? (SpinPassFail && TransitPassFail && Line1AlignmentPass && Line2AlignmentPass)
        : SpinPassFail;
        
    public string OverallStatus => OverallPass ? "PASS" : "FAIL";
    
    // Alias properties for consistent API
    public bool SpinTestPassed => SpinPassFail;
    public bool TransitTestPassed => TransitPassFail;
    
    // Mean position (alias to SpinMeanPosition)
    public TransponderPosition MeanPosition => SpinMeanPosition;
    
    // Certificate number (generated when certificate is created)
    public string CertificateNumber { get; set; } = "";
    
    // Spin statistics dictionary (heading -> statistics)
    public Dictionary<string, TestStatistics> SpinStatistics { get; set; } = new();
}

/// <summary>
/// Simple position structure for computed properties
/// </summary>
public class TransponderPosition
{
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double Depth { get; set; }
}

/// <summary>
/// Results for a single spin heading
/// </summary>
public class HeadingResult
{
    public int Heading { get; set; }  // Nominal heading (0, 90, 180, 270)
    public double ActualHeading { get; set; }  // Actual average heading from data
    public string HeadingLabel { get; set; } = "";
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double Depth { get; set; }
    public double DiffEasting { get; set; }
    public double DiffNorthing { get; set; }
    public double DiffRadial { get; set; }
    
    // Properties for UI display
    public double MeanOffset { get; set; }
    public double StdDev { get; set; }
    public int PointCount { get; set; }
    public bool Passed { get; set; }
    
    /// <summary>
    /// Display name showing actual heading (e.g., "45.2°")
    /// </summary>
    public string ActualHeadingDisplay => $"{ActualHeading:F1}°";
}

/// <summary>
/// Column mapping for NPD file parsing
/// </summary>
public class UsblColumnMapping
{
    public int DateColumn { get; set; } = 0;
    public int TimeColumn { get; set; } = 1;
    public int VesselGyroColumn { get; set; } = 2;
    public int VesselEastingColumn { get; set; } = 3;
    public int VesselNorthingColumn { get; set; } = 4;
    public int TransponderEastingColumn { get; set; } = 5;
    public int TransponderNorthingColumn { get; set; } = 6;
    public int TransponderDepthColumn { get; set; } = 7;
    
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public bool HasDateTimeSplit { get; set; } = true;
    public char Delimiter { get; set; } = ',';  // Default to comma for NPD files
    public int HeaderRows { get; set; } = 1;
    
    public static UsblColumnMapping Default => new();
}

/// <summary>
/// Report template for custom company branding
/// </summary>
public class ReportTemplate
{
    public string TemplateName { get; set; } = "Default";
    public bool IsDefault { get; set; } = true;
    
    // Company Info
    public string CompanyName { get; set; } = "S7 Survey Solutions";
    public string CompanyAddress { get; set; } = "";
    public string CompanyPhone { get; set; } = "";
    public string CompanyEmail { get; set; } = "";
    public string CompanyWebsite { get; set; } = "";
    public string LogoPath { get; set; } = "";
    public byte[]? LogoData { get; set; }
    
    // Header Settings
    public string ReportTitle { get; set; } = "USBL VERIFICATION REPORT";
    public string CertificateTitle { get; set; } = "USBL VERIFICATION CERTIFICATE";
    public bool ShowLogo { get; set; } = true;
    public bool ShowCompanyName { get; set; } = true;
    public double LogoWidth { get; set; } = 80;
    public double LogoHeight { get; set; } = 40;
    
    // Footer Settings
    public string FooterLeftText { get; set; } = "{ProjectName}";
    public string FooterRightText { get; set; } = "Generated: {GeneratedDate}";
    public bool ShowPageNumbers { get; set; } = true;
    
    // Colors (hex format)
    public string PrimaryColor { get; set; } = "#1E3A5F";
    public string SecondaryColor { get; set; } = "#4A90D9";
    public string AccentColor { get; set; } = "#2ECC71";
    public string HeaderBackground { get; set; } = "#1E3A5F";
    public string HeaderTextColor { get; set; } = "#FFFFFF";
    public string PassColor { get; set; } = "#2ECC71";
    public string FailColor { get; set; } = "#E74C3C";
    
    // Certificate specific
    public string CertificateNumber { get; set; } = "CERT-{Year}-{Sequence}";
    public string SignatoryName { get; set; } = "";
    public string SignatoryTitle { get; set; } = "Survey Manager";
    public string SignaturePath { get; set; } = "";
    public byte[]? SignatureData { get; set; }
    
    // Placeholders: {ProjectName}, {ClientName}, {VesselName}, {SurveyDate}, 
    // {GeneratedDate}, {ProcessorName}, {Year}, {Sequence}
}

/// <summary>
/// Quality metrics for a dataset
/// </summary>
public class QualityMetrics
{
    public string DatasetName { get; set; } = "";
    public int TotalPoints { get; set; }
    public int ValidPoints { get; set; }
    public int ExcludedPoints { get; set; }
    public int OutliersDetected { get; set; }
    
    // Scores (0-100)
    public double OverallScore { get; set; }
    public double PointCountScore { get; set; }
    public double OutlierScore { get; set; }
    public double SpreadScore { get; set; }
    public double ConsistencyScore { get; set; }
    
    // Statistics
    public double MeanEasting { get; set; }
    public double MeanNorthing { get; set; }
    public double StdDevEasting { get; set; }
    public double StdDevNorthing { get; set; }
    public double MaxRadialSpread { get; set; }
    public double MinRadialSpread { get; set; }
    public double TwoSigmaRadius { get; set; }
    
    // Time analysis
    public DateTime? FirstTimestamp { get; set; }
    public DateTime? LastTimestamp { get; set; }
    public TimeSpan Duration => (LastTimestamp - FirstTimestamp) ?? TimeSpan.Zero;
    public double AverageUpdateRate { get; set; } // Hz
    
    // Quality indicators
    public string QualityGrade => OverallScore switch
    {
        >= 90 => "Excellent",
        >= 75 => "Good",
        >= 60 => "Fair",
        >= 40 => "Poor",
        _ => "Critical"
    };
    
    public string QualityColor => OverallScore switch
    {
        >= 90 => "#2ECC71", // Green
        >= 75 => "#27AE60", // Dark Green
        >= 60 => "#F39C12", // Orange
        >= 40 => "#E67E22", // Dark Orange
        _ => "#E74C3C"      // Red
    };
    
    // Properties for DataGrid display
    public string Name => DatasetName;
    public string Value => $"{OverallScore:F1}";
    public string Threshold => "80.0";
    public string Status => OverallScore >= 80 ? "PASS" : "FAIL";
    
    // Alias for consistent naming
    public double QualityScore => OverallScore;
}

/// <summary>
/// Selected point info for chart interaction
/// </summary>
public class SelectedPointInfo
{
    public UsblObservation? Observation { get; set; }
    public string DatasetName { get; set; } = "";
    public int PointIndex { get; set; }
    public double ChartX { get; set; }
    public double ChartY { get; set; }
    public double DistanceFromCenter { get; set; }
    
    public bool HasSelection => Observation != null;
    
    public string Summary => Observation != null
        ? $"Point #{Observation.Index} | {Observation.DateTime:HH:mm:ss} | E:{Observation.TransponderEasting:F2} N:{Observation.TransponderNorthing:F2}"
        : "No point selected";
}

/// <summary>
/// Batch import result
/// </summary>
public class BatchImportResult
{
    public bool Success { get; set; }
    public string FolderPath { get; set; } = "";
    public List<string> LoadedFiles { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalPointsLoaded { get; set; }
    public LengthUnit DetectedUnit { get; set; } = LengthUnit.Meters;
    public bool UnitAutoDetected { get; set; }
    
    public Dictionary<int, string> SpinFileMapping { get; set; } = new(); // heading -> filename
    public Dictionary<int, string> TransitFileMapping { get; set; } = new(); // line -> filename
}

/// <summary>
/// Auto-detection result for units
/// </summary>
public class UnitDetectionResult
{
    public LengthUnit DetectedUnit { get; set; } = LengthUnit.Meters;
    public double Confidence { get; set; } // 0-1
    public string Reason { get; set; } = "";
    public double MaxCoordinate { get; set; }
    public double MinCoordinate { get; set; }
    
    public bool IsHighConfidence => Confidence >= 0.8;
}

/// <summary>
/// Summary data for spin test display in UI
/// </summary>
public class SpinDataSummary
{
    public string HeadingLabel { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int PointCount { get; set; }
    public bool HasData { get; set; }
    public double ActualHeading { get; set; }
}

#region Advanced Statistics Models

/// <summary>
/// Comprehensive radial statistics for a dataset
/// </summary>
public class RadialStatistics
{
    public string DatasetName { get; set; } = "";
    public int PointCount { get; set; }
    
    // Mean position
    public double MeanEasting { get; set; }
    public double MeanNorthing { get; set; }
    
    // Radial statistics
    public double MinRadial { get; set; }
    public double MaxRadial { get; set; }
    public double MeanRadial { get; set; }
    public double MedianRadial { get; set; }
    public double StdDevRadial { get; set; }
    
    // Coordinate standard deviations
    public double StdDevEasting { get; set; }
    public double StdDevNorthing { get; set; }
    
    // Accuracy metrics
    public double CEP { get; set; }           // Circular Error Probable (50%)
    public double R95 { get; set; }           // 95% radius
    public double R99 { get; set; }           // 99% radius
    public double DRMS { get; set; }          // Distance Root Mean Square
    public double TwoDRMS { get; set; }       // 2x DRMS (≈95% for 2D normal)
    public double TwoSigmaRadius { get; set; } // 2σ radius
    
    // Confidence ellipse
    public double EllipseSemiMajor { get; set; }
    public double EllipseSemiMinor { get; set; }
    public double EllipseRotation { get; set; } // Degrees from East
    
    // Display helpers
    public string CEPDisplay => $"{CEP:F3} m";
    public string R95Display => $"{R95:F3} m";
    public string EllipseDisplay => $"{EllipseSemiMajor:F3} × {EllipseSemiMinor:F3} m @ {EllipseRotation:F1}°";
}

/// <summary>
/// Confidence ellipse parameters
/// </summary>
public class ConfidenceEllipse
{
    public double CenterEasting { get; set; }
    public double CenterNorthing { get; set; }
    public double SemiMajor { get; set; }
    public double SemiMinor { get; set; }
    public double Rotation { get; set; }  // Degrees from East
    public double ConfidenceLevel { get; set; } = 0.95;
    
    public double Area => Math.PI * SemiMajor * SemiMinor;
    public double Eccentricity => SemiMajor > 0 
        ? Math.Sqrt(1 - Math.Pow(SemiMinor / SemiMajor, 2)) 
        : 0;
}

/// <summary>
/// Monte Carlo simulation results
/// </summary>
public class MonteCarloResult
{
    public int Iterations { get; set; }
    public double ConfidenceLevel { get; set; }
    
    // Mean position estimate
    public double MeanEasting { get; set; }
    public double MeanNorthing { get; set; }
    
    // Confidence bounds for position
    public double EastingLowerBound { get; set; }
    public double EastingUpperBound { get; set; }
    public double NorthingLowerBound { get; set; }
    public double NorthingUpperBound { get; set; }
    
    // CEP confidence bounds
    public double CEPMean { get; set; }
    public double CEPLowerBound { get; set; }
    public double CEPUpperBound { get; set; }
    
    // Standard errors
    public double EastingStdError { get; set; }
    public double NorthingStdError { get; set; }
    
    // Display helpers
    public string EastingConfidenceDisplay => 
        $"{MeanEasting:F3} [{EastingLowerBound:F3}, {EastingUpperBound:F3}]";
    public string NorthingConfidenceDisplay => 
        $"{MeanNorthing:F3} [{NorthingLowerBound:F3}, {NorthingUpperBound:F3}]";
    public string CEPConfidenceDisplay => 
        $"{CEPMean:F3} [{CEPLowerBound:F3}, {CEPUpperBound:F3}]";
}

/// <summary>
/// Trend analysis results
/// </summary>
public class TrendAnalysisResult
{
    public bool HasSufficientData { get; set; }
    public double DurationMinutes { get; set; }
    
    // Linear trends
    public LinearTrend EastingTrend { get; set; } = new();
    public LinearTrend NorthingTrend { get; set; } = new();
    public LinearTrend DepthTrend { get; set; } = new();
    
    // Combined radial drift
    public double RadialDriftRatePerMinute { get; set; } // m/min
    public double TotalDrift { get; set; }                // m total
    public double DriftDirection { get; set; }            // Degrees
    public bool HasSignificantDrift { get; set; }
    
    // Display helpers
    public string DriftRateDisplay => $"{RadialDriftRatePerMinute * 1000:F2} mm/min";
    public string DriftDirectionDisplay => $"{DriftDirection:F1}° True";
    public string Status => HasSignificantDrift ? "⚠️ Drift Detected" : "✓ Stable";
}

/// <summary>
/// Linear trend parameters from regression
/// </summary>
public class LinearTrend
{
    public double Slope { get; set; }       // Units per second
    public double Intercept { get; set; }
    public double RSquared { get; set; }    // Goodness of fit (0-1)
    
    public double SlopePerMinute => Slope * 60;
    public double SlopePerHour => Slope * 3600;
    public bool IsSignificant => RSquared > 0.5;
}

/// <summary>
/// Time series data for charting
/// </summary>
public class TimeSeriesData
{
    public List<DateTime> Timestamps { get; set; } = new();
    public List<double> EastingOffsets { get; set; } = new();
    public List<double> NorthingOffsets { get; set; } = new();
    public List<double> RadialOffsets { get; set; } = new();
    public List<double> Depths { get; set; } = new();
    public List<double> GyroReadings { get; set; } = new();
    
    public int Count => Timestamps.Count;
    public TimeSpan Duration => Count > 1 
        ? Timestamps.Last() - Timestamps.First() 
        : TimeSpan.Zero;
}

/// <summary>
/// Histogram data for error distribution visualization
/// </summary>
public class HistogramData
{
    public int BinCount { get; set; }
    public double BinWidth { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    
    public List<double> BinEdges { get; set; } = new();
    public List<int> BinCounts { get; set; } = new();
    public List<double> BinFrequencies { get; set; } = new();
    
    // Get bin center for plotting
    public List<double> BinCenters => 
        Enumerable.Range(0, BinCount)
            .Select(i => MinValue + (i + 0.5) * BinWidth)
            .ToList();
}

/// <summary>
/// Box plot statistics for outlier visualization
/// </summary>
public class BoxPlotData
{
    public string DatasetName { get; set; } = "";
    
    // Five-number summary
    public double Minimum { get; set; }
    public double Q1 { get; set; }          // 25th percentile
    public double Median { get; set; }      // 50th percentile
    public double Q3 { get; set; }          // 75th percentile
    public double Maximum { get; set; }
    
    // Whiskers (1.5 × IQR rule)
    public double LowerWhisker { get; set; }
    public double UpperWhisker { get; set; }
    public double IQR { get; set; }         // Q3 - Q1
    
    // Outliers (beyond whiskers)
    public List<(int Index, double Value)> Outliers { get; set; } = new();
    
    public int OutlierCount => Outliers.Count;
}

/// <summary>
/// Polar plot data point
/// </summary>
public class PolarPoint
{
    public double Radius { get; set; }      // Distance from center (m)
    public double Angle { get; set; }       // Angle in degrees (0-360, 0=East)
    public int Index { get; set; }
    public DateTime DateTime { get; set; }
    public double VesselHeading { get; set; }
    
    // Convert to Cartesian for plotting
    public double X => Radius * Math.Cos(Angle * Math.PI / 180);
    public double Y => Radius * Math.Sin(Angle * Math.PI / 180);
}

/// <summary>
/// 3D point for scatter plot visualization
/// </summary>
public class ScatterPoint3D
{
    public double X { get; set; }  // Easting offset
    public double Y { get; set; }  // Northing offset
    public double Z { get; set; }  // Depth
    public int Index { get; set; }
    public string Dataset { get; set; } = "";
    public DateTime DateTime { get; set; }
}

#endregion
