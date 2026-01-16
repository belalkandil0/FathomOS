namespace FathomOS.Core.Calculations;

using FathomOS.Core.Models;

/// <summary>
/// Handles conversion between different length/distance units.
/// All internal calculations use meters as the base unit.
/// </summary>
public static class UnitConverter
{
    // Conversion factors TO meters (multiply source value by factor to get meters)
    private static readonly Dictionary<LengthUnit, double> ToMetersFactor = new()
    {
        { LengthUnit.Meter, 1.0 },
        { LengthUnit.Kilometer, 1000.0 },
        { LengthUnit.USSurveyFeet, 0.3048006096012192 },      // 1200/3937 meters exactly
        { LengthUnit.InternationalFeet, 0.3048 },             // Exactly 0.3048 meters
        { LengthUnit.NauticalMile, 1852.0 },                  // Exactly 1852 meters
        { LengthUnit.InternationalMile, 1609.344 },           // Exactly 1609.344 meters
        { LengthUnit.Yard, 0.9144 },                          // Exactly 0.9144 meters
        { LengthUnit.Fathom, 1.8288 }                         // 2 yards = 1.8288 meters
    };

    /// <summary>
    /// Convert a value from one unit to another
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="fromUnit">Source unit</param>
    /// <param name="toUnit">Target unit</param>
    /// <returns>Converted value</returns>
    public static double Convert(double value, LengthUnit fromUnit, LengthUnit toUnit)
    {
        if (fromUnit == toUnit)
            return value;

        // Convert to meters first, then to target unit
        double meters = value * ToMetersFactor[fromUnit];
        return meters / ToMetersFactor[toUnit];
    }

    /// <summary>
    /// Convert a value from specified unit to meters
    /// </summary>
    public static double ToMeters(double value, LengthUnit fromUnit)
    {
        return value * ToMetersFactor[fromUnit];
    }

    /// <summary>
    /// Convert a value from meters to specified unit
    /// </summary>
    public static double FromMeters(double meters, LengthUnit toUnit)
    {
        return meters / ToMetersFactor[toUnit];
    }

    /// <summary>
    /// Convert meters to US Survey Feet (convenience method)
    /// </summary>
    public static double MetersToUSSurveyFeet(double meters)
    {
        return meters / ToMetersFactor[LengthUnit.USSurveyFeet];
    }

    /// <summary>
    /// Convert US Survey Feet to meters (convenience method)
    /// </summary>
    public static double USSurveyFeetToMeters(double feet)
    {
        return feet * ToMetersFactor[LengthUnit.USSurveyFeet];
    }

    /// <summary>
    /// Convert meters to kilometers (convenience method for KP)
    /// </summary>
    public static double MetersToKilometers(double meters)
    {
        return meters / 1000.0;
    }

    /// <summary>
    /// Convert kilometers to meters (convenience method for KP)
    /// </summary>
    public static double KilometersToMeters(double km)
    {
        return km * 1000.0;
    }

    /// <summary>
    /// Get the conversion factor from one unit to another
    /// </summary>
    public static double GetConversionFactor(LengthUnit fromUnit, LengthUnit toUnit)
    {
        if (fromUnit == toUnit)
            return 1.0;

        return ToMetersFactor[fromUnit] / ToMetersFactor[toUnit];
    }

    /// <summary>
    /// Format a value with appropriate precision for display
    /// </summary>
    /// <param name="value">The value to format</param>
    /// <param name="unit">The unit (affects decimal places)</param>
    /// <param name="includeUnit">Whether to append the unit abbreviation</param>
    public static string Format(double value, LengthUnit unit, bool includeUnit = true)
    {
        // Determine decimal places based on unit magnitude
        int decimals = unit switch
        {
            LengthUnit.Kilometer => 6,      // KP typically needs high precision
            LengthUnit.NauticalMile => 6,
            LengthUnit.InternationalMile => 6,
            LengthUnit.Meter => 3,
            LengthUnit.USSurveyFeet => 3,
            LengthUnit.InternationalFeet => 3,
            LengthUnit.Yard => 3,
            LengthUnit.Fathom => 3,
            _ => 3
        };

        string formatted = value.ToString($"F{decimals}");
        return includeUnit ? $"{formatted} {unit.GetAbbreviation()}" : formatted;
    }
}
