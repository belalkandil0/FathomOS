namespace FathomOS.Core.Interfaces;

/// <summary>
/// Unified unit conversion service supporting length, angle, and other measurement units
/// commonly used in marine surveying and hydrographic applications.
/// </summary>
public interface IUnitConverter
{
    #region Length Conversions

    /// <summary>
    /// Converts a value between length units.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="from">Source unit.</param>
    /// <param name="to">Target unit.</param>
    /// <returns>Converted value.</returns>
    double Convert(double value, LengthUnit from, LengthUnit to);

    /// <summary>
    /// Converts a value to meters.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="from">Source unit.</param>
    /// <returns>Value in meters.</returns>
    double ToMeters(double value, LengthUnit from);

    /// <summary>
    /// Converts a value from meters.
    /// </summary>
    /// <param name="meters">Value in meters.</param>
    /// <param name="to">Target unit.</param>
    /// <returns>Converted value.</returns>
    double FromMeters(double meters, LengthUnit to);

    #endregion

    #region Angle Conversions

    /// <summary>
    /// Converts a value between angle units.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="from">Source unit.</param>
    /// <param name="to">Target unit.</param>
    /// <returns>Converted value.</returns>
    double Convert(double value, AngleUnit from, AngleUnit to);

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>Angle in radians.</returns>
    double DegreesToRadians(double degrees);

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>Angle in degrees.</returns>
    double RadiansToDegrees(double radians);

    #endregion

    #region Pressure Conversions

    /// <summary>
    /// Converts a value between pressure units.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="from">Source unit.</param>
    /// <param name="to">Target unit.</param>
    /// <returns>Converted value.</returns>
    double Convert(double value, PressureUnit from, PressureUnit to);

    #endregion

    #region Temperature Conversions

    /// <summary>
    /// Converts a value between temperature units.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="from">Source unit.</param>
    /// <param name="to">Target unit.</param>
    /// <returns>Converted value.</returns>
    double Convert(double value, TemperatureUnit from, TemperatureUnit to);

    #endregion

    #region Speed Conversions

    /// <summary>
    /// Converts a value between speed units.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="from">Source unit.</param>
    /// <param name="to">Target unit.</param>
    /// <returns>Converted value.</returns>
    double Convert(double value, SpeedUnit from, SpeedUnit to);

    #endregion

    #region Formatting

    /// <summary>
    /// Formats a length value with its unit.
    /// </summary>
    /// <param name="value">Value to format.</param>
    /// <param name="unit">Unit of the value.</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Formatted string with unit.</returns>
    string FormatWithUnit(double value, LengthUnit unit, int decimals = 2);

    /// <summary>
    /// Formats an angle value with its unit.
    /// </summary>
    /// <param name="value">Value to format.</param>
    /// <param name="unit">Unit of the value.</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Formatted string with unit.</returns>
    string FormatWithUnit(double value, AngleUnit unit, int decimals = 2);

    /// <summary>
    /// Formats a speed value with its unit.
    /// </summary>
    /// <param name="value">Value to format.</param>
    /// <param name="unit">Unit of the value.</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Formatted string with unit.</returns>
    string FormatWithUnit(double value, SpeedUnit unit, int decimals = 2);

    /// <summary>
    /// Parses a unit from a string.
    /// </summary>
    /// <param name="unitString">String representation of unit.</param>
    /// <param name="unit">Parsed length unit.</param>
    /// <returns>True if parsing succeeded.</returns>
    bool TryParseLengthUnit(string unitString, out LengthUnit unit);

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the conversion factor between two length units.
    /// </summary>
    /// <param name="from">Source unit.</param>
    /// <param name="to">Target unit.</param>
    /// <returns>Conversion factor (multiply source value by this to get target).</returns>
    double GetConversionFactor(LengthUnit from, LengthUnit to);

    /// <summary>
    /// Gets the abbreviation for a length unit.
    /// </summary>
    /// <param name="unit">Unit.</param>
    /// <returns>Abbreviation string.</returns>
    string GetAbbreviation(LengthUnit unit);

    /// <summary>
    /// Gets the display name for a length unit.
    /// </summary>
    /// <param name="unit">Unit.</param>
    /// <returns>Display name.</returns>
    string GetDisplayName(LengthUnit unit);

    #endregion
}

/// <summary>
/// Length/distance units commonly used in marine surveying.
/// </summary>
public enum LengthUnit
{
    /// <summary>
    /// Meters (SI base unit).
    /// </summary>
    Meters,

    /// <summary>
    /// Kilometers (1000 meters).
    /// </summary>
    Kilometers,

    /// <summary>
    /// Centimeters (0.01 meters).
    /// </summary>
    Centimeters,

    /// <summary>
    /// Millimeters (0.001 meters).
    /// </summary>
    Millimeters,

    /// <summary>
    /// International feet (0.3048 meters exactly).
    /// </summary>
    Feet,

    /// <summary>
    /// US Survey feet (1200/3937 meters).
    /// </summary>
    USSurveyFeet,

    /// <summary>
    /// International yards (0.9144 meters).
    /// </summary>
    Yards,

    /// <summary>
    /// Fathoms (6 feet = 1.8288 meters).
    /// </summary>
    Fathoms,

    /// <summary>
    /// Nautical miles (1852 meters exactly).
    /// </summary>
    NauticalMiles,

    /// <summary>
    /// International miles (1609.344 meters).
    /// </summary>
    Miles,

    /// <summary>
    /// Inches (0.0254 meters).
    /// </summary>
    Inches
}

/// <summary>
/// Angle units.
/// </summary>
public enum AngleUnit
{
    /// <summary>
    /// Degrees (360 per circle).
    /// </summary>
    Degrees,

    /// <summary>
    /// Radians (2*PI per circle).
    /// </summary>
    Radians,

    /// <summary>
    /// Gradians/Gons (400 per circle).
    /// </summary>
    Gradians,

    /// <summary>
    /// Minutes of arc (60 per degree).
    /// </summary>
    ArcMinutes,

    /// <summary>
    /// Seconds of arc (60 per arc minute).
    /// </summary>
    ArcSeconds,

    /// <summary>
    /// Milliradians (1000 per radian).
    /// </summary>
    Milliradians
}

/// <summary>
/// Pressure units.
/// </summary>
public enum PressureUnit
{
    /// <summary>
    /// Pascals (SI unit).
    /// </summary>
    Pascals,

    /// <summary>
    /// Kilopascals.
    /// </summary>
    Kilopascals,

    /// <summary>
    /// Bar.
    /// </summary>
    Bar,

    /// <summary>
    /// Millibar.
    /// </summary>
    Millibar,

    /// <summary>
    /// Atmospheres.
    /// </summary>
    Atmospheres,

    /// <summary>
    /// Pounds per square inch.
    /// </summary>
    PSI,

    /// <summary>
    /// Millimeters of mercury.
    /// </summary>
    MmHg,

    /// <summary>
    /// Inches of mercury.
    /// </summary>
    InHg
}

/// <summary>
/// Temperature units.
/// </summary>
public enum TemperatureUnit
{
    /// <summary>
    /// Celsius.
    /// </summary>
    Celsius,

    /// <summary>
    /// Fahrenheit.
    /// </summary>
    Fahrenheit,

    /// <summary>
    /// Kelvin.
    /// </summary>
    Kelvin
}

/// <summary>
/// Speed units.
/// </summary>
public enum SpeedUnit
{
    /// <summary>
    /// Meters per second.
    /// </summary>
    MetersPerSecond,

    /// <summary>
    /// Kilometers per hour.
    /// </summary>
    KilometersPerHour,

    /// <summary>
    /// Miles per hour.
    /// </summary>
    MilesPerHour,

    /// <summary>
    /// Knots (nautical miles per hour).
    /// </summary>
    Knots,

    /// <summary>
    /// Feet per second.
    /// </summary>
    FeetPerSecond
}
