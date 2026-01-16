using System;
using System.Linq;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Length unit options
/// </summary>
public enum LengthUnit
{
    Meters,
    InternationalFeet,
    UsSurveyFeet
}

/// <summary>
/// Service for unit conversions - critical for accurate USBL calculations
/// </summary>
public class UnitConversionService
{
    // Conversion factors to meters
    private const double MetersPerInternationalFoot = 0.3048;           // Exact definition
    private const double MetersPerUsSurveyFoot = 0.3048006096012192;    // 1200/3937 meters
    
    // Inverse factors (for converting FROM meters)
    private const double InternationalFeetPerMeter = 3.280839895013123;
    private const double UsSurveyFeetPerMeter = 3.280833333333333;
    
    /// <summary>
    /// Convert a value from one unit to another
    /// </summary>
    public double Convert(double value, LengthUnit fromUnit, LengthUnit toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        // Convert to meters first (base unit)
        double meters = ToMeters(value, fromUnit);
        
        // Convert from meters to target unit
        return FromMeters(meters, toUnit);
    }
    
    /// <summary>
    /// Convert value to meters
    /// </summary>
    public double ToMeters(double value, LengthUnit fromUnit)
    {
        return fromUnit switch
        {
            LengthUnit.Meters => value,
            LengthUnit.InternationalFeet => value * MetersPerInternationalFoot,
            LengthUnit.UsSurveyFeet => value * MetersPerUsSurveyFoot,
            _ => value
        };
    }
    
    /// <summary>
    /// Convert value from meters to target unit
    /// </summary>
    public double FromMeters(double value, LengthUnit toUnit)
    {
        return toUnit switch
        {
            LengthUnit.Meters => value,
            LengthUnit.InternationalFeet => value * InternationalFeetPerMeter,
            LengthUnit.UsSurveyFeet => value * UsSurveyFeetPerMeter,
            _ => value
        };
    }
    
    /// <summary>
    /// Get display name for unit
    /// </summary>
    public static string GetDisplayName(LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => "Meters (m)",
            LengthUnit.InternationalFeet => "International Feet (ft)",
            LengthUnit.UsSurveyFeet => "US Survey Feet (sft)",
            _ => unit.ToString()
        };
    }
    
    /// <summary>
    /// Get abbreviation for unit
    /// </summary>
    public static string GetAbbreviation(LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => "m",
            LengthUnit.InternationalFeet => "ft",
            LengthUnit.UsSurveyFeet => "sft",
            _ => ""
        };
    }
    
    /// <summary>
    /// Get conversion factor from one unit to another
    /// </summary>
    public double GetConversionFactor(LengthUnit fromUnit, LengthUnit toUnit)
    {
        if (fromUnit == toUnit) return 1.0;
        return Convert(1.0, fromUnit, toUnit);
    }
    
    /// <summary>
    /// Convert all position values in observation
    /// </summary>
    public void ConvertObservation(Models.UsblObservation obs, LengthUnit fromUnit, LengthUnit toUnit)
    {
        if (fromUnit == toUnit) return;
        
        obs.VesselEasting = Convert(obs.VesselEasting, fromUnit, toUnit);
        obs.VesselNorthing = Convert(obs.VesselNorthing, fromUnit, toUnit);
        obs.TransponderEasting = Convert(obs.TransponderEasting, fromUnit, toUnit);
        obs.TransponderNorthing = Convert(obs.TransponderNorthing, fromUnit, toUnit);
        obs.TransponderDepth = Convert(obs.TransponderDepth, fromUnit, toUnit);
        
        // Also convert deltas if they've been calculated
        obs.DeltaEasting = Convert(obs.DeltaEasting, fromUnit, toUnit);
        obs.DeltaNorthing = Convert(obs.DeltaNorthing, fromUnit, toUnit);
        obs.DeltaDepth = Convert(obs.DeltaDepth, fromUnit, toUnit);
    }
    
    /// <summary>
    /// Validate that the difference between International and US Survey Feet is significant
    /// for the given measurement range
    /// </summary>
    public static string GetUnitWarning(double maxCoordinate, LengthUnit inputUnit)
    {
        if (inputUnit == LengthUnit.Meters) return string.Empty;
        
        // Calculate the difference between Int'l and US Survey at this scale
        // US Survey foot is slightly smaller: 1 US sft = 0.999998 Int'l ft
        double diffPerFoot = Math.Abs(MetersPerUsSurveyFoot - MetersPerInternationalFoot);
        double totalDiff = maxCoordinate * diffPerFoot;
        
        if (totalDiff > 0.01) // More than 1cm difference
        {
            return $"Note: At coordinate {maxCoordinate:N0}, the difference between " +
                   $"Int'l and US Survey feet is {totalDiff:F3}m ({totalDiff * 1000:F1}mm). " +
                   "Ensure you've selected the correct foot definition.";
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Auto-detect units based on coordinate magnitude
    /// </summary>
    public Models.UnitDetectionResult DetectUnits(double maxCoordinate, double minCoordinate)
    {
        var result = new Models.UnitDetectionResult
        {
            MaxCoordinate = maxCoordinate,
            MinCoordinate = minCoordinate
        };
        
        // Typical coordinate ranges:
        // UTM Eastings: 100,000 - 900,000 (meters) or 328,084 - 2,952,756 (feet)
        // UTM Northings: 0 - 10,000,000 (meters) or 0 - 32,808,399 (feet)
        // State Plane (US): often in feet, can be 1,000,000+ feet
        
        // Heuristic: If max coordinate is > 1,000,000, likely feet
        // If max coordinate is between 100,000 and 1,000,000, could be either
        // If max coordinate < 100,000, could be meters (local grid) or feet (small state plane)
        
        if (maxCoordinate > 3_000_000)
        {
            // Very large - almost certainly US State Plane in feet
            result.DetectedUnit = LengthUnit.UsSurveyFeet;
            result.Confidence = 0.9;
            result.Reason = $"Very large coordinates ({maxCoordinate:N0}) suggest US State Plane in feet";
        }
        else if (maxCoordinate > 1_000_000)
        {
            // Large coordinates - likely feet (State Plane or similar)
            result.DetectedUnit = LengthUnit.InternationalFeet;
            result.Confidence = 0.75;
            result.Reason = $"Large coordinates ({maxCoordinate:N0}) suggest feet";
        }
        else if (maxCoordinate > 100_000 && maxCoordinate < 1_000_000)
        {
            // Could be UTM in meters or smaller State Plane in feet
            // Check depth values if available, or use meter as default for offshore
            result.DetectedUnit = LengthUnit.Meters;
            result.Confidence = 0.6;
            result.Reason = $"Medium coordinates ({maxCoordinate:N0}) - likely UTM meters";
        }
        else if (maxCoordinate > 10_000)
        {
            // Smaller values - likely meters (local or small area UTM)
            result.DetectedUnit = LengthUnit.Meters;
            result.Confidence = 0.7;
            result.Reason = $"Coordinates ({maxCoordinate:N0}) suggest meters";
        }
        else
        {
            // Very small - local grid, assume meters
            result.DetectedUnit = LengthUnit.Meters;
            result.Confidence = 0.5;
            result.Reason = $"Small coordinates ({maxCoordinate:N0}) - assuming local grid in meters";
        }
        
        return result;
    }
    
    /// <summary>
    /// Detect units from a list of observations
    /// </summary>
    public Models.UnitDetectionResult DetectUnitsFromObservations(System.Collections.Generic.List<Models.UsblObservation> observations)
    {
        if (observations == null || observations.Count == 0)
        {
            return new Models.UnitDetectionResult
            {
                DetectedUnit = LengthUnit.Meters,
                Confidence = 0,
                Reason = "No observations to analyze"
            };
        }
        
        var eastings = observations.Select(o => Math.Abs(o.TransponderEasting)).ToList();
        var northings = observations.Select(o => Math.Abs(o.TransponderNorthing)).ToList();
        
        double maxCoord = Math.Max(eastings.Max(), northings.Max());
        double minCoord = Math.Min(eastings.Min(), northings.Min());
        
        return DetectUnits(maxCoord, minCoord);
    }
}

/// <summary>
/// Extension methods for LengthUnit
/// </summary>
public static class LengthUnitExtensions
{
    public static string ToDisplayString(this LengthUnit unit) => UnitConversionService.GetDisplayName(unit);
    public static string ToAbbreviation(this LengthUnit unit) => UnitConversionService.GetAbbreviation(unit);
}
