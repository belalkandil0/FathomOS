using FathomOS.Modules.RovGyroCalibration.Models;
using System.ComponentModel;

namespace FathomOS.Modules.RovGyroCalibration.Services;

/// <summary>
/// Provides unit conversion utilities for length/distance measurements.
/// All internal calculations use meters; display values are converted to user's selected unit.
/// </summary>
public static class UnitConversionService
{
    // Conversion factors to meters (base unit for all internal calculations)
    private const double MetersPerInternationalFoot = 0.3048;
    private const double MetersPerUSSurveyFoot = 1200.0 / 3937.0; // Exactly 0.3048006096...
    private const double MetersPerUSFoot = 0.3048; // Same as international foot
    
    /// <summary>
    /// Convert a value from one unit to another
    /// </summary>
    public static double Convert(double value, LengthUnit fromUnit, LengthUnit toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        // First convert to meters (internal base unit)
        double meters = ToMeters(value, fromUnit);
        
        // Then convert to target unit
        return FromMeters(meters, toUnit);
    }
    
    /// <summary>
    /// Convert a value to meters (internal base unit)
    /// </summary>
    public static double ToMeters(double value, LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => value,
            LengthUnit.InternationalFeet => value * MetersPerInternationalFoot,
            LengthUnit.USSurveyFeet => value * MetersPerUSSurveyFoot,
            LengthUnit.USFeet => value * MetersPerUSFoot,
            _ => value
        };
    }
    
    /// <summary>
    /// Convert from meters (internal base unit) to display unit
    /// </summary>
    public static double FromMeters(double meters, LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => meters,
            LengthUnit.InternationalFeet => meters / MetersPerInternationalFoot,
            LengthUnit.USSurveyFeet => meters / MetersPerUSSurveyFoot,
            LengthUnit.USFeet => meters / MetersPerUSFoot,
            _ => meters
        };
    }
    
    /// <summary>
    /// Get the abbreviation for a unit
    /// </summary>
    public static string GetAbbreviation(LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => "m",
            LengthUnit.InternationalFeet => "ft",
            LengthUnit.USSurveyFeet => "ft (US)",
            LengthUnit.USFeet => "ft",
            _ => ""
        };
    }
    
    /// <summary>
    /// Get the display name for a unit
    /// </summary>
    public static string GetDisplayName(LengthUnit unit)
    {
        return unit switch
        {
            LengthUnit.Meters => "Meters",
            LengthUnit.InternationalFeet => "International Feet",
            LengthUnit.USSurveyFeet => "US Survey Feet",
            LengthUnit.USFeet => "US Feet",
            _ => unit.ToString()
        };
    }
    
    /// <summary>
    /// Get all available length units
    /// </summary>
    public static IEnumerable<LengthUnit> GetAllUnits()
    {
        return Enum.GetValues<LengthUnit>();
    }
    
    /// <summary>
    /// Get unit options for UI binding
    /// </summary>
    public static IEnumerable<UnitOption> GetUnitOptions()
    {
        return GetAllUnits().Select(u => new UnitOption
        {
            Unit = u,
            DisplayName = GetDisplayName(u),
            Abbreviation = GetAbbreviation(u)
        });
    }
    
    /// <summary>
    /// Format a distance value with unit abbreviation
    /// </summary>
    public static string FormatDistance(double valueInMeters, LengthUnit displayUnit, int decimals = 3)
    {
        double displayValue = FromMeters(valueInMeters, displayUnit);
        return $"{displayValue.ToString($"F{decimals}")} {GetAbbreviation(displayUnit)}";
    }
    
    /// <summary>
    /// Format an angle value with degree symbol
    /// </summary>
    public static string FormatAngle(double degrees, int decimals = 4)
    {
        return $"{degrees.ToString($"F{decimals}")}°";
    }
    
    /// <summary>
    /// Get label text for distance fields (e.g., "Distance (m)" or "Distance (ft)")
    /// </summary>
    public static string GetDistanceLabel(string baseName, LengthUnit unit)
    {
        return $"{baseName} ({GetAbbreviation(unit)})";
    }
}

/// <summary>
/// Helper class for UI binding of unit options
/// </summary>
public class UnitOption
{
    public LengthUnit Unit { get; set; }
    public string DisplayName { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    
    public override string ToString() => DisplayName;
}

/// <summary>
/// Observable unit context for dynamic UI updates.
/// Bind to this in ViewModels to get automatic label updates when unit changes.
/// </summary>
public class UnitContext : INotifyPropertyChanged
{
    private LengthUnit _displayUnit = LengthUnit.Meters;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    public LengthUnit DisplayUnit
    {
        get => _displayUnit;
        set
        {
            if (_displayUnit != value)
            {
                _displayUnit = value;
                OnPropertyChanged(nameof(DisplayUnit));
                OnPropertyChanged(nameof(UnitAbbreviation));
                OnPropertyChanged(nameof(UnitName));
                OnPropertyChanged(nameof(DistanceLabel));
                OnPropertyChanged(nameof(BaselineLabel));
                OnPropertyChanged(nameof(OffsetLabel));
                OnPropertyChanged(nameof(PortLabel));
                OnPropertyChanged(nameof(StarboardLabel));
                OnPropertyChanged(nameof(ForwardLabel));
                OnPropertyChanged(nameof(AftLabel));
            }
        }
    }
    
    public string UnitAbbreviation => UnitConversionService.GetAbbreviation(_displayUnit);
    public string UnitName => UnitConversionService.GetDisplayName(_displayUnit);
    public string DistanceLabel => $"Distance ({UnitAbbreviation})";
    public string BaselineLabel => $"Baseline Distance ({UnitAbbreviation})";
    public string OffsetLabel => $"Offset ({UnitAbbreviation})";
    public string PortLabel => $"Port ({UnitAbbreviation})";
    public string StarboardLabel => $"Starboard ({UnitAbbreviation})";
    public string ForwardLabel => $"Forward ({UnitAbbreviation})";
    public string AftLabel => $"Aft ({UnitAbbreviation})";
    
    /// <summary>
    /// Convert a value from meters to display unit
    /// </summary>
    public double ToDisplay(double meters) => UnitConversionService.FromMeters(meters, _displayUnit);
    
    /// <summary>
    /// Convert a value from display unit to meters
    /// </summary>
    public double FromDisplay(double displayValue) => UnitConversionService.ToMeters(displayValue, _displayUnit);
    
    /// <summary>
    /// Format a distance value for display
    /// </summary>
    public string FormatDistance(double meters, int decimals = 3) => 
        UnitConversionService.FormatDistance(meters, _displayUnit, decimals);
    
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
