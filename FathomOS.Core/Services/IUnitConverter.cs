namespace FathomOS.Core.Services;

/// <summary>
/// Unified unit type enum supporting length, distance, and angular measurements.
/// </summary>
public enum Unit
{
    // Length/Distance units
    /// <summary>Meters - SI base unit for length.</summary>
    Meters,

    /// <summary>International feet (0.3048 meters exactly).</summary>
    Feet,

    /// <summary>Fathoms (2 yards = 1.8288 meters) - traditional unit for water depth.</summary>
    Fathoms,

    /// <summary>Kilometers (1000 meters).</summary>
    Kilometers,

    /// <summary>Nautical miles (1852 meters exactly) - standard unit for marine navigation.</summary>
    NauticalMiles,

    /// <summary>US Survey Feet (1200/3937 meters) - used in US land surveys.</summary>
    USSurveyFeet,

    /// <summary>International miles (1609.344 meters).</summary>
    Miles,

    /// <summary>Yards (0.9144 meters).</summary>
    Yards,

    // Angular units
    /// <summary>Degrees - standard angular measurement (360 per full rotation).</summary>
    Degrees,

    /// <summary>Radians - SI unit for angular measurement (2*PI per full rotation).</summary>
    Radians,

    /// <summary>Gradians/Gons (400 per full rotation) - used in surveying.</summary>
    Gradians,

    /// <summary>Minutes of arc (60 per degree).</summary>
    ArcMinutes,

    /// <summary>Seconds of arc (60 per arc minute).</summary>
    ArcSeconds
}

/// <summary>
/// Contract for a unified unit conversion service supporting length, distance, and angular units.
/// </summary>
public interface IUnitConverter
{
    /// <summary>
    /// Converts a value from one unit to another.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="from">The source unit.</param>
    /// <param name="to">The target unit.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when converting between incompatible unit types (e.g., length to angle).</exception>
    double Convert(double value, Unit from, Unit to);

    /// <summary>
    /// Formats a value with its unit abbreviation for display.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="unit">The unit to display.</param>
    /// <param name="decimals">Number of decimal places (default: 2).</param>
    /// <returns>A formatted string with value and unit abbreviation.</returns>
    string FormatWithUnit(double value, Unit unit, int decimals = 2);

    /// <summary>
    /// Gets the abbreviation for a unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The standard abbreviation for the unit.</returns>
    string GetAbbreviation(Unit unit);

    /// <summary>
    /// Gets the display name for a unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The human-readable display name.</returns>
    string GetDisplayName(Unit unit);

    /// <summary>
    /// Checks if two units are compatible for conversion.
    /// </summary>
    /// <param name="unit1">First unit.</param>
    /// <param name="unit2">Second unit.</param>
    /// <returns>True if the units can be converted between each other.</returns>
    bool AreCompatible(Unit unit1, Unit unit2);

    /// <summary>
    /// Gets all units in a specific category.
    /// </summary>
    /// <param name="category">The unit category (Length or Angle).</param>
    /// <returns>Collection of units in that category.</returns>
    IEnumerable<Unit> GetUnitsByCategory(UnitCategory category);
}

/// <summary>
/// Categories of measurement units.
/// </summary>
public enum UnitCategory
{
    /// <summary>Length and distance measurements.</summary>
    Length,

    /// <summary>Angular measurements.</summary>
    Angle
}
