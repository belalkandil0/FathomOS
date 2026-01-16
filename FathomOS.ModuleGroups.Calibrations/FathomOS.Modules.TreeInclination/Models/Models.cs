namespace FathomOS.Modules.TreeInclination.Models;

using System.Collections.ObjectModel;
using System.IO;

#region Enums

public enum ProcessingMode { Standard, Custom }

public enum LengthUnit { Meters, Feet, UsSurveyFeet }

public enum DepthInputUnit
{
    Meters, Feet, MilliBar, Bar, KiloPascal, DeciBar, PSA, PSI, Atmosphere
}

public enum GeometryType
{
    Square, Rectangle, IrregularQuadrilateral, Pentagon, Hexagon, CustomPolygon
}

public enum QualityStatus { OK, Warning, Failed, NotCalculated }

public enum SignConvention
{
    BowUpPositive_StbdDownPositive,
    BowUpPositive_PortDownPositive,
    BowDownPositive_StbdDownPositive,
    BowDownPositive_PortDownPositive
}

public enum CoordinateSource { ManualEntry, PositionFixFile, StructureFile }

public enum CoordinateInputMode 
{ 
    RelativeOffset,      // X,Y offsets from origin (corner 1)
    AbsoluteCoordinates  // Full Easting/Northing values
}

public enum TideFileFormat { Auto, SevenTide, GenericCSV, TabDelimited, SpaceDelimited }

public enum MisclosureDistribution { Equal, WeightedByDistance, WeightedByDepth, ProportionalToDepth }

#endregion

#region Core Models

public class InclinationProject
{
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string StructureName { get; set; } = string.Empty;
    public string StructureType { get; set; } = "Tree";
    public string SurveyorName { get; set; } = string.Empty;
    public string ProcessorName { get; set; } = string.Empty;
    public DateTime SurveyDate { get; set; } = DateTime.Now;
    public string Notes { get; set; } = string.Empty;

    public ProcessingMode Mode { get; set; } = ProcessingMode.Standard;
    public LengthUnit DimensionUnit { get; set; } = LengthUnit.Meters;
    public DepthInputUnit DepthUnit { get; set; } = DepthInputUnit.Meters;
    public LengthUnit OutputUnit { get; set; } = LengthUnit.Meters;
    public GeometryType Geometry { get; set; } = GeometryType.Rectangle;
    public SignConvention SignConvention { get; set; } = SignConvention.BowUpPositive_StbdDownPositive;
    public CoordinateSource CoordSource { get; set; } = CoordinateSource.ManualEntry;
    
    // Coordinate input settings
    public CoordinateInputMode CoordinateInputMode { get; set; } = CoordinateInputMode.RelativeOffset;
    public LengthUnit CoordinateUnit { get; set; } = LengthUnit.Meters;
    
    // Structure orientation settings
    public double? StructureHeading { get; set; }  // True bearing of structure's forward direction (Corner 1→2)
    public bool UseGyroHeading { get; set; } = true;  // Auto-populate from NPD gyro data
    public double GridConvergence { get; set; } = 0;  // Grid to true north correction

    public double WaterDensity { get; set; } = 1025.0;
    public double AtmosphericPressure { get; set; } = 1013.25;
    public double Gravity { get; set; } = 9.80665;

    public bool ApplyTideCorrection { get; set; }
    public double TideValue { get; set; }  // Manual tide value in meters
    public double DraftOffset { get; set; }  // Draft correction offset in meters
    public string? TideFilePath { get; set; }
    public TideFileFormat TideFormat { get; set; } = TideFileFormat.Auto;
    public LengthUnit TideUnit { get; set; } = LengthUnit.Meters;

    public InclinationTolerances Tolerances { get; set; } = new();
    public CustomModeSettings CustomSettings { get; set; } = new();

    public ObservableCollection<CornerMeasurement> Corners { get; set; } = new();
    public ObservableCollection<NpdFileInfo> LoadedFiles { get; set; } = new();
    public ObservableCollection<TideRecord> TideData { get; set; } = new();

    public InclinationResult? Result { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
}

public class NpdFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public int CornerIndex { get; set; }
    public string CornerName { get; set; } = string.Empty;
    public bool IsClosurePoint { get; set; }

    public DateTime? LogStartTime { get; set; }
    public DateTime? LogEndTime { get; set; }
    public int RecordCount { get; set; }
    public string DetectedDepthColumn { get; set; } = string.Empty;
    public string SelectedDepthColumn { get; set; } = string.Empty;
    public List<string> AvailableColumns { get; set; } = new();

    public double RawDepthAverage { get; set; }
    public double RawDepthStdDev { get; set; }
    public double RawDepthMin { get; set; }
    public double RawDepthMax { get; set; }
    public int OutlierCount { get; set; }

    // Position data (extracted from NPD if available)
    public bool HasPositionData { get; set; }
    public double? AverageEasting { get; set; }
    public double? AverageNorthing { get; set; }
    public double? AverageHeight { get; set; }
    public double? EastingStdDev { get; set; }
    public double? NorthingStdDev { get; set; }
    
    // Heading data (extracted from NPD if available)
    public bool HasHeadingData { get; set; }
    public double? AverageHeading { get; set; }
    public double? HeadingStdDev { get; set; }
    public double? HeadingMin { get; set; }
    public double? HeadingMax { get; set; }
    
    // Display properties for UI binding
    public string HeadingDisplay => HasHeadingData && AverageHeading.HasValue 
        ? $"{AverageHeading:F1}°" : "-";
    
    // Alias properties for UI consistency
    public int PointCount => RecordCount;
    public double DepthStdDev => RawDepthStdDev;
    public double DepthMin => RawDepthMin;
    public double DepthMax => RawDepthMax;

    public List<DepthReading> Readings { get; set; } = new();

    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
}

public class DepthReading
{
    public DateTime Timestamp { get; set; }
    public double RawValue { get; set; }
    public double ConvertedDepth { get; set; }
    public bool IsOutlier { get; set; }
    public bool IsExcluded { get; set; }
    
    // Position data per reading
    public double? Easting { get; set; }
    public double? Northing { get; set; }
    public double? Height { get; set; }
    
    // Heading data per reading
    public double? Heading { get; set; }
}

public class CornerMeasurement : System.ComponentModel.INotifyPropertyChanged
{
    private int _index;
    private string _name = string.Empty;
    private bool _isClosurePoint;
    private double _x;
    private double _y;
    private double? _absoluteEasting;
    private double? _absoluteNorthing;
    private double? _heading;
    private double _rawDepthAverage;
    private double? _tideCorrection;
    private double _correctedDepth;
    
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

    public int Index { get => _index; set { _index = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public bool IsClosurePoint { get => _isClosurePoint; set { _isClosurePoint = value; OnPropertyChanged(); } }

    // Relative coordinates (used for calculation)
    public double X { get => _x; set { _x = value; OnPropertyChanged(); OnPropertyChanged(nameof(PositionDisplay)); } }
    public double Y { get => _y; set { _y = value; OnPropertyChanged(); OnPropertyChanged(nameof(PositionDisplay)); } }
    
    // Absolute coordinates (user input - Easting/Northing)
    public double? AbsoluteEasting { get => _absoluteEasting; set { _absoluteEasting = value; OnPropertyChanged(); OnPropertyChanged(nameof(PositionDisplay)); } }
    public double? AbsoluteNorthing { get => _absoluteNorthing; set { _absoluteNorthing = value; OnPropertyChanged(); OnPropertyChanged(nameof(PositionDisplay)); } }
    
    // Gyro heading at this corner (from NPD)
    public double? Heading { get => _heading; set { _heading = value; OnPropertyChanged(); OnPropertyChanged(nameof(HeadingDisplay)); } }

    public double RawDepthAverage { get => _rawDepthAverage; set { _rawDepthAverage = value; OnPropertyChanged(); } }
    public double? TideCorrection { get => _tideCorrection; set { _tideCorrection = value; OnPropertyChanged(); } }
    public double CorrectedDepth { get => _correctedDepth; set { _correctedDepth = value; OnPropertyChanged(); } }

    public NpdFileInfo? SourceFile { get; set; }
    public Dictionary<int, double> Baselines { get; set; } = new();
    
    // Display helpers
    public string PositionDisplay => AbsoluteEasting.HasValue && AbsoluteNorthing.HasValue 
        ? $"E:{AbsoluteEasting:F2}, N:{AbsoluteNorthing:F2}" 
        : $"X:{X:F3}, Y:{Y:F3}";
    
    public string HeadingDisplay => Heading.HasValue ? $"{Heading:F1}°" : "-";
}

public class TideRecord
{
    public DateTime DateTime { get; set; }
    public double TideHeight { get; set; }
    public bool IsInterpolated { get; set; }
}

public class InclinationResult
{
    public double TotalInclination { get; set; }
    public double InclinationHeading { get; set; }  // Relative to X-axis (structure coordinates)
    public string HeadingDescription => GetHeadingDescription();

    public double AveragePitch { get; set; }
    public double AverageRoll { get; set; }
    public double Misclosure { get; set; }
    public double OutOfPlane { get; set; }
    public double MeanDepth { get; set; }
    
    // Alias properties for compatibility
    public double Pitch => AveragePitch;
    public double Roll => AverageRoll;
    public double TrueBearing => TrueInclinationBearing ?? InclinationHeading;
    public QualityStatus QcStatus => InclinationStatus;
    
    // True bearing results (when structure heading is known)
    public double? TrueInclinationBearing { get; set; }  // Compass bearing of tilt direction
    public string? TrueBearingDescription { get; set; }  // "NE", "SW", etc.
    public double? StructureHeadingUsed { get; set; }    // Structure heading used in calculation
    
    // Orientation-referenced pitch/roll
    public double? ForwardPitch { get; set; }   // Pitch along structure heading (bow up/down)
    public double? StarboardRoll { get; set; }  // Roll perpendicular to heading (stbd down/up)

    public QualityStatus InclinationStatus { get; set; } = QualityStatus.NotCalculated;
    public QualityStatus MisclosureStatus { get; set; } = QualityStatus.NotCalculated;
    public QualityStatus OutOfPlaneStatus { get; set; } = QualityStatus.NotCalculated;

    public string CalculationMethod { get; set; } = string.Empty;
    public PlaneParameters? BestFitPlane { get; set; }
    public DateTime CalculatedAt { get; set; }

    private string GetHeadingDescription()
    {
        var h = TrueInclinationBearing ?? InclinationHeading;
        return h switch
        {
            >= 337.5 or < 22.5 => "N",
            >= 22.5 and < 67.5 => "NE",
            >= 67.5 and < 112.5 => "E",
            >= 112.5 and < 157.5 => "SE",
            >= 157.5 and < 202.5 => "S",
            >= 202.5 and < 247.5 => "SW",
            >= 247.5 and < 292.5 => "W",
            _ => "NW"
        };
    }
    
    public string GetTiltDescription()
    {
        if (TrueInclinationBearing.HasValue)
        {
            return $"Tilting {TotalInclination:F3}° towards {TrueInclinationBearing:F1}° ({TrueBearingDescription})";
        }
        return $"Tilting {TotalInclination:F3}° at {InclinationHeading:F1}° (relative to structure X-axis)";
    }
}

public class PlaneParameters
{
    public double A { get; set; }
    public double B { get; set; }
    public double C { get; set; }
    public double D { get; set; }
    public double RmsError { get; set; }
    public double MaxResidual { get; set; }
}

public class InclinationTolerances
{
    // Typical subsea structure inclination tolerances
    public double MaxInclination { get; set; } = 2.0;      // 2.0° = fail
    public double WarningInclination { get; set; } = 1.0;  // 1.0° = warning, below = OK
    public double MaxMisclosure { get; set; } = 0.10;      // 100mm = fail
    public double WarningMisclosure { get; set; } = 0.05;  // 50mm = warning
    public double MaxOutOfPlane { get; set; } = 0.20;      // 200mm = fail
    public double WarningOutOfPlane { get; set; } = 0.10;  // 100mm = warning
}

public class CustomModeSettings
{
    public bool DistributeMisclosure { get; set; } = true;
    public MisclosureDistribution DistributionMethod { get; set; } = MisclosureDistribution.Equal;
    public bool RemoveOutliers { get; set; }
    public double OutlierSigma { get; set; } = 3.0;
    public bool UseGyroHeading { get; set; }
    public double GyroHeading { get; set; }
    public double Convergence { get; set; }
    public bool UseBestFitPlane { get; set; } = true;
    public bool WeightByStdDev { get; set; }
    public int MinReadingsPerCorner { get; set; } = 10;
    public bool ApplyMedianFilter { get; set; }
    public int MedianWindowSize { get; set; } = 5;
    public double TemperatureCorrection { get; set; }
    public double SalinityCorrection { get; set; }
}

public class ModuleSettings
{
    public string LastProjectPath { get; set; } = string.Empty;
    public string LastExportPath { get; set; } = string.Empty;
    public bool IsDarkTheme { get; set; } = true;
    public double WindowWidth { get; set; } = 1400;
    public double WindowHeight { get; set; } = 900;
    public LengthUnit DefaultDimensionUnit { get; set; } = LengthUnit.Meters;
    public DepthInputUnit DefaultDepthUnit { get; set; } = DepthInputUnit.Meters;
    public double DefaultWaterDensity { get; set; } = 1025.0;
    public List<string> RecentProjects { get; set; } = new();
}

public class RecentFileItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime LastAccessed { get; set; }
    public string DisplayText => $"{FileName} ({LastAccessed:MMM dd, yyyy})";
}

public class PositionFix
{
    public DateTime Timestamp { get; set; }
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double? Depth { get; set; }
    public double? Heading { get; set; }
    public string? PointName { get; set; }
}

#endregion

#region Unit Conversions

public static class UnitConversions
{
    private const double FeetToMeters = 0.3048;
    private const double UsSurveyFeetToMeters = 1200.0 / 3937.0;
    private const double MbarToMetersSw = 0.00994;
    private const double BarToMetersSw = 9.94;
    private const double KpaToMetersSw = 0.0994;
    private const double DbarToMetersSw = 0.994;
    private const double PsiaToMetersSw = 0.703;
    private const double AtmToMetersSw = 10.07;

    public static double ToMeters(double value, DepthInputUnit unit, double waterDensity = 1025.0, double atmPressure = 0)
    {
        double densityFactor = 1025.0 / waterDensity;
        
        return unit switch
        {
            DepthInputUnit.Meters => value,
            DepthInputUnit.Feet => value * FeetToMeters,
            DepthInputUnit.MilliBar => (value - atmPressure) * MbarToMetersSw * densityFactor,
            DepthInputUnit.Bar => (value - atmPressure / 1000.0) * BarToMetersSw * densityFactor,
            DepthInputUnit.KiloPascal => (value - atmPressure / 10.0) * KpaToMetersSw * densityFactor,
            DepthInputUnit.DeciBar => (value - atmPressure / 100.0) * DbarToMetersSw * densityFactor,
            DepthInputUnit.PSA => (value - 14.696) * PsiaToMetersSw * densityFactor,
            DepthInputUnit.PSI => value * PsiaToMetersSw * densityFactor,
            DepthInputUnit.Atmosphere => (value - 1.0) * AtmToMetersSw * densityFactor,
            _ => value
        };
    }

    public static double LengthToMeters(double value, LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => value,
            LengthUnit.Feet => value * FeetToMeters,
            LengthUnit.UsSurveyFeet => value * UsSurveyFeetToMeters,
            _ => value
        };
    }

    public static double MetersToLength(double meters, LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => meters,
            LengthUnit.Feet => meters / FeetToMeters,
            LengthUnit.UsSurveyFeet => meters / UsSurveyFeetToMeters,
            _ => meters
        };
    }

    public static string GetUnitSymbol(DepthInputUnit unit)
    {
        return unit switch
        {
            DepthInputUnit.Meters => "m",
            DepthInputUnit.Feet => "ft",
            DepthInputUnit.MilliBar => "mbar",
            DepthInputUnit.Bar => "bar",
            DepthInputUnit.KiloPascal => "kPa",
            DepthInputUnit.DeciBar => "dbar",
            DepthInputUnit.PSA => "psia",
            DepthInputUnit.PSI => "psig",
            DepthInputUnit.Atmosphere => "atm",
            _ => ""
        };
    }

    public static string GetUnitSymbol(LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => "m",
            LengthUnit.Feet => "ft",
            LengthUnit.UsSurveyFeet => "usft",
            _ => ""
        };
    }

    public static bool IsPressureUnit(DepthInputUnit unit)
    {
        return unit is DepthInputUnit.MilliBar or DepthInputUnit.Bar or DepthInputUnit.KiloPascal
            or DepthInputUnit.DeciBar or DepthInputUnit.PSA or DepthInputUnit.PSI or DepthInputUnit.Atmosphere;
    }
}

#endregion
