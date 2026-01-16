using System;
using System.Collections.Generic;

namespace FathomOS.Modules.SoundVelocity.Models;

/// <summary>
/// Represents a single CTD/SVP data point with all oceanographic parameters
/// </summary>
public class CtdDataPoint
{
    public int Index { get; set; }
    
    /// <summary>Timestamp from the original data</summary>
    public DateTime? Timestamp { get; set; }
    
    /// <summary>Depth in meters</summary>
    public double Depth { get; set; }
    
    /// <summary>Sound velocity in m/s</summary>
    public double SoundVelocity { get; set; }
    
    /// <summary>Temperature in °C</summary>
    public double Temperature { get; set; }
    
    /// <summary>Salinity in PSU or Conductivity in mS/cm</summary>
    public double SalinityOrConductivity { get; set; }
    
    /// <summary>Water density in kg/m³ (or density anomaly)</summary>
    public double Density { get; set; }
    
    /// <summary>Pressure in dbar (calculated from depth if needed)</summary>
    public double Pressure { get; set; }
    
    /// <summary>Whether this is an interpolated value</summary>
    public bool IsInterpolated { get; set; }
    
    /// <summary>Whether this point is excluded from processing</summary>
    public bool IsExcluded { get; set; }
    
    /// <summary>Whether this point has been smoothed</summary>
    public bool IsSmoothed { get; set; }
    
    /// <summary>
    /// Create a deep copy of this data point
    /// </summary>
    public CtdDataPoint Clone()
    {
        return new CtdDataPoint
        {
            Index = Index,
            Timestamp = Timestamp,
            Depth = Depth,
            SoundVelocity = SoundVelocity,
            Temperature = Temperature,
            SalinityOrConductivity = SalinityOrConductivity,
            Density = Density,
            Pressure = Pressure,
            IsInterpolated = IsInterpolated,
            IsExcluded = IsExcluded,
            IsSmoothed = IsSmoothed
        };
    }
}

/// <summary>
/// Represents the project information and metadata
/// </summary>
public class ProjectInfo
{
    public string ProjectTitle { get; set; } = string.Empty;
    public string ProjectNumber { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double KP { get; set; }
    public string Equipment { get; set; } = string.Empty;
    public string ObservedBy { get; set; } = string.Empty;
    public string CheckedBy { get; set; } = string.Empty;
    public DateTime CastDateTime { get; set; } = DateTime.Now;
    
    /// <summary>Latitude string in selected format (e.g., "54;30;15.5N")</summary>
    public string LatitudeString { get; set; } = string.Empty;
    
    /// <summary>Longitude string in selected format</summary>
    public string LongitudeString { get; set; } = string.Empty;
    
    /// <summary>Calculated latitude in decimal degrees</summary>
    public double Latitude { get; set; }
    
    /// <summary>Calculated longitude in decimal degrees</summary>
    public double Longitude { get; set; }
    
    /// <summary>Barometric pressure in mbar</summary>
    public double BarometricPressure { get; set; } = 1013.25;
    
    /// <summary>Coordinate format used</summary>
    public GeoCoordinateFormat CoordinateFormat { get; set; } = GeoCoordinateFormat.DMS;
}

/// <summary>
/// Processing settings for the CTD/SVP calculation
/// </summary>
public class ProcessingSettings
{
    /// <summary>Calculation mode (CTD/SV or SV only)</summary>
    public CalculationMode CalculationMode { get; set; } = CalculationMode.CtdSvWithSalinity;
    
    /// <summary>Sound velocity formula to use</summary>
    public SoundVelocityFormula SvFormula { get; set; } = SoundVelocityFormula.ExternalSource;
    
    /// <summary>Density formula to use</summary>
    public DensityFormula DensityFormula { get; set; } = DensityFormula.UnescoEOS80;
    
    /// <summary>Input type for first column</summary>
    public DepthPressureType InputType { get; set; } = DepthPressureType.Depth;
    
    /// <summary>Depth interval for interpolation in meters</summary>
    public double DepthInterval { get; set; } = 1.0;
    
    /// <summary>Transducer depth in meters (data above this is ignored)</summary>
    public double TransducerDepth { get; set; } = 0.0;
    
    /// <summary>Use latitude for gravity calculation</summary>
    public bool UseLatitudeForGravity { get; set; } = true;
    
    /// <summary>Include bottom temperature in report</summary>
    public bool IncludeBottomTemperature { get; set; }
}

/// <summary>
/// Column mapping configuration for input file parsing
/// </summary>
public class ColumnMapping
{
    /// <summary>Column index (0-based) for Depth/Pressure</summary>
    public int DepthColumn { get; set; } = -1;
    
    /// <summary>Column index for Sound Velocity</summary>
    public int SoundVelocityColumn { get; set; } = -1;
    
    /// <summary>Column index for Temperature</summary>
    public int TemperatureColumn { get; set; } = -1;
    
    /// <summary>Column index for Salinity/Conductivity</summary>
    public int SalinityColumn { get; set; } = -1;
    
    /// <summary>Column index for Density</summary>
    public int DensityColumn { get; set; } = -1;
    
    /// <summary>Whether the file has a header row</summary>
    public bool HasHeader { get; set; } = true;
    
    /// <summary>Header row number (1-based)</summary>
    public int HeaderRow { get; set; } = 1;
    
    /// <summary>Creates a deep copy of the column mapping</summary>
    public ColumnMapping Clone()
    {
        return new ColumnMapping
        {
            DepthColumn = DepthColumn,
            SoundVelocityColumn = SoundVelocityColumn,
            TemperatureColumn = TemperatureColumn,
            SalinityColumn = SalinityColumn,
            DensityColumn = DensityColumn,
            HasHeader = HasHeader,
            HeaderRow = HeaderRow
        };
    }
    
    /// <summary>Validates that required columns are mapped</summary>
    public bool IsValid(CalculationMode mode, SoundVelocityFormula svFormula, DensityFormula densityFormula)
    {
        // Depth is always required
        if (DepthColumn < 0) return false;
        
        // SV required only if using external source
        if (svFormula == SoundVelocityFormula.ExternalSource && SoundVelocityColumn < 0) 
            return false;
        
        // Temperature and Salinity required for CTD calculations
        if (mode != CalculationMode.SvOnly)
        {
            if (TemperatureColumn < 0) return false;
            if (SalinityColumn < 0) return false;
        }
        
        // Density required only if using external source
        if (densityFormula == DensityFormula.ExternalSource && DensityColumn < 0)
            return false;
            
        return true;
    }
}

/// <summary>
/// Export options configuration
/// </summary>
public class ExportOptions
{
    /// <summary>Export USR file (QINSy format)</summary>
    public bool ExportUsr { get; set; }
    
    /// <summary>Export VEL file (EIVA/NaviModel format)</summary>
    public bool ExportVel { get; set; }
    
    /// <summary>Export PRO file (generic profile)</summary>
    public bool ExportPro { get; set; }
    
    /// <summary>Export Excel report with chart</summary>
    public bool ExportExcel { get; set; } = true;
    
    /// <summary>Export CSV file</summary>
    public bool ExportCsv { get; set; }
    
    /// <summary>Export PDF report</summary>
    public bool ExportPdf { get; set; }
    
    /// <summary>Export smoothed data files</summary>
    public bool ExportSmoothed { get; set; }
    
    /// <summary>Output directory path</summary>
    public string OutputDirectory { get; set; } = string.Empty;
    
    /// <summary>Base filename (without extension)</summary>
    public string BaseFileName { get; set; } = string.Empty;
}

/// <summary>
/// Represents loaded file data before column mapping
/// </summary>
public class RawFileData
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public InputFileType FileType { get; set; }
    public List<string[]> Rows { get; set; } = new();
    public string[] Headers { get; set; } = Array.Empty<string>();
    public int ColumnCount { get; set; }
    public int RowCount { get; set; }
    
    /// <summary>File metadata extracted from header (for Valeport files)</summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Statistical summary of processed CTD/SVP data
/// </summary>
public class DataStatistics
{
    public int PointCount { get; set; }
    
    public double MinDepth { get; set; }
    public double MaxDepth { get; set; }
    public double AvgDepth { get; set; }
    
    public double MinSoundVelocity { get; set; }
    public double MaxSoundVelocity { get; set; }
    public double AvgSoundVelocity { get; set; }
    
    public double MinTemperature { get; set; }
    public double MaxTemperature { get; set; }
    public double AvgTemperature { get; set; }
    
    public double MinSalinity { get; set; }
    public double MaxSalinity { get; set; }
    public double AvgSalinity { get; set; }
    
    public double MinDensity { get; set; }
    public double MaxDensity { get; set; }
    public double AvgDensity { get; set; }
}
