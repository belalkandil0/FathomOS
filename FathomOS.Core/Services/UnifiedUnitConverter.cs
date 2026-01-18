namespace FathomOS.Core.Services;

/// <summary>
/// Unified unit conversion service supporting length, distance, and angular measurements.
/// All length conversions use meters as the intermediate unit.
/// All angular conversions use radians as the intermediate unit.
/// </summary>
public class UnifiedUnitConverter : IUnitConverter
{
    // Conversion factors TO base unit (meters for length, radians for angles)
    private static readonly Dictionary<Unit, double> ToBaseUnit = new()
    {
        // Length units - conversion to meters
        { Unit.Meters, 1.0 },
        { Unit.Feet, 0.3048 },                          // International feet
        { Unit.Fathoms, 1.8288 },                       // 2 yards
        { Unit.Kilometers, 1000.0 },
        { Unit.NauticalMiles, 1852.0 },
        { Unit.USSurveyFeet, 0.3048006096012192 },      // 1200/3937 meters
        { Unit.Miles, 1609.344 },                       // International miles
        { Unit.Yards, 0.9144 },

        // Angular units - conversion to radians
        { Unit.Radians, 1.0 },
        { Unit.Degrees, Math.PI / 180.0 },
        { Unit.Gradians, Math.PI / 200.0 },
        { Unit.ArcMinutes, Math.PI / 10800.0 },        // degrees/60 to radians
        { Unit.ArcSeconds, Math.PI / 648000.0 }        // degrees/3600 to radians
    };

    // Unit abbreviations
    private static readonly Dictionary<Unit, string> Abbreviations = new()
    {
        { Unit.Meters, "m" },
        { Unit.Feet, "ft" },
        { Unit.Fathoms, "fth" },
        { Unit.Kilometers, "km" },
        { Unit.NauticalMiles, "nm" },
        { Unit.USSurveyFeet, "ftUS" },
        { Unit.Miles, "mi" },
        { Unit.Yards, "yd" },
        { Unit.Radians, "rad" },
        { Unit.Degrees, "\u00B0" },                     // degree symbol
        { Unit.Gradians, "gon" },
        { Unit.ArcMinutes, "'" },
        { Unit.ArcSeconds, "\"" }
    };

    // Display names
    private static readonly Dictionary<Unit, string> DisplayNames = new()
    {
        { Unit.Meters, "Meters" },
        { Unit.Feet, "International Feet" },
        { Unit.Fathoms, "Fathoms" },
        { Unit.Kilometers, "Kilometers" },
        { Unit.NauticalMiles, "Nautical Miles" },
        { Unit.USSurveyFeet, "US Survey Feet" },
        { Unit.Miles, "International Miles" },
        { Unit.Yards, "Yards" },
        { Unit.Radians, "Radians" },
        { Unit.Degrees, "Degrees" },
        { Unit.Gradians, "Gradians" },
        { Unit.ArcMinutes, "Arc Minutes" },
        { Unit.ArcSeconds, "Arc Seconds" }
    };

    // Unit categories
    private static readonly HashSet<Unit> LengthUnits = new()
    {
        Unit.Meters, Unit.Feet, Unit.Fathoms, Unit.Kilometers,
        Unit.NauticalMiles, Unit.USSurveyFeet, Unit.Miles, Unit.Yards
    };

    private static readonly HashSet<Unit> AngleUnits = new()
    {
        Unit.Radians, Unit.Degrees, Unit.Gradians, Unit.ArcMinutes, Unit.ArcSeconds
    };

    /// <inheritdoc/>
    public double Convert(double value, Unit from, Unit to)
    {
        // Same unit - no conversion needed
        if (from == to)
            return value;

        // Check compatibility
        if (!AreCompatible(from, to))
        {
            throw new InvalidOperationException(
                $"Cannot convert between incompatible units: {from} and {to}. " +
                $"Length units can only be converted to other length units, " +
                $"and angular units can only be converted to other angular units.");
        }

        // Convert to base unit, then to target unit
        var baseValue = value * ToBaseUnit[from];
        return baseValue / ToBaseUnit[to];
    }

    /// <inheritdoc/>
    public string FormatWithUnit(double value, Unit unit, int decimals = 2)
    {
        var formattedValue = value.ToString($"F{decimals}");
        var abbreviation = GetAbbreviation(unit);

        // Special formatting for angular units
        if (unit == Unit.Degrees || unit == Unit.ArcMinutes || unit == Unit.ArcSeconds)
        {
            return $"{formattedValue}{abbreviation}";
        }

        return $"{formattedValue} {abbreviation}";
    }

    /// <inheritdoc/>
    public string GetAbbreviation(Unit unit)
    {
        return Abbreviations.TryGetValue(unit, out var abbr) ? abbr : unit.ToString();
    }

    /// <inheritdoc/>
    public string GetDisplayName(Unit unit)
    {
        return DisplayNames.TryGetValue(unit, out var name) ? name : unit.ToString();
    }

    /// <inheritdoc/>
    public bool AreCompatible(Unit unit1, Unit unit2)
    {
        var cat1 = GetCategory(unit1);
        var cat2 = GetCategory(unit2);
        return cat1 == cat2;
    }

    /// <inheritdoc/>
    public IEnumerable<Unit> GetUnitsByCategory(UnitCategory category)
    {
        return category switch
        {
            UnitCategory.Length => LengthUnits,
            UnitCategory.Angle => AngleUnits,
            _ => Enumerable.Empty<Unit>()
        };
    }

    /// <summary>
    /// Gets the category of a unit.
    /// </summary>
    /// <param name="unit">The unit to categorize.</param>
    /// <returns>The unit's category.</returns>
    public static UnitCategory GetCategory(Unit unit)
    {
        if (LengthUnits.Contains(unit))
            return UnitCategory.Length;
        if (AngleUnits.Contains(unit))
            return UnitCategory.Angle;

        throw new ArgumentException($"Unknown unit: {unit}", nameof(unit));
    }

    /// <summary>
    /// Converts degrees, minutes, seconds to decimal degrees.
    /// </summary>
    /// <param name="degrees">Degrees component.</param>
    /// <param name="minutes">Minutes component.</param>
    /// <param name="seconds">Seconds component.</param>
    /// <returns>Decimal degrees.</returns>
    public static double DmsToDecimalDegrees(double degrees, double minutes, double seconds)
    {
        var sign = Math.Sign(degrees);
        if (sign == 0) sign = 1;

        return sign * (Math.Abs(degrees) + minutes / 60.0 + seconds / 3600.0);
    }

    /// <summary>
    /// Converts decimal degrees to degrees, minutes, seconds.
    /// </summary>
    /// <param name="decimalDegrees">Decimal degrees value.</param>
    /// <returns>Tuple of (degrees, minutes, seconds).</returns>
    public static (int degrees, int minutes, double seconds) DecimalDegreesToDms(double decimalDegrees)
    {
        var sign = Math.Sign(decimalDegrees);
        var absValue = Math.Abs(decimalDegrees);

        var degrees = (int)absValue;
        var fractionalDegrees = absValue - degrees;

        var totalMinutes = fractionalDegrees * 60.0;
        var minutes = (int)totalMinutes;
        var seconds = (totalMinutes - minutes) * 60.0;

        return (sign * degrees, minutes, seconds);
    }

    /// <summary>
    /// Formats an angle as degrees, minutes, seconds string.
    /// </summary>
    /// <param name="decimalDegrees">Decimal degrees value.</param>
    /// <param name="secondsDecimals">Number of decimal places for seconds.</param>
    /// <returns>Formatted DMS string.</returns>
    public static string FormatAsDms(double decimalDegrees, int secondsDecimals = 2)
    {
        var (degrees, minutes, seconds) = DecimalDegreesToDms(decimalDegrees);
        return $"{degrees}\u00B0 {minutes}' {seconds.ToString($"F{secondsDecimals}")}\"";
    }

    /// <summary>
    /// Normalizes an angle to the range [0, 360) degrees.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The normalized angle.</returns>
    public static double NormalizeDegrees(double degrees)
    {
        var result = degrees % 360.0;
        if (result < 0)
            result += 360.0;
        return result;
    }

    /// <summary>
    /// Normalizes an angle to the range [-180, 180) degrees.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The normalized angle.</returns>
    public static double NormalizeDegreesSymmetric(double degrees)
    {
        var result = NormalizeDegrees(degrees);
        if (result >= 180.0)
            result -= 360.0;
        return result;
    }

    /// <summary>
    /// Calculates the shortest angular distance between two angles.
    /// </summary>
    /// <param name="from">Starting angle in degrees.</param>
    /// <param name="to">Ending angle in degrees.</param>
    /// <returns>The shortest angular distance (positive or negative).</returns>
    public static double AngularDistance(double from, double to)
    {
        var diff = NormalizeDegrees(to - from);
        if (diff > 180.0)
            diff -= 360.0;
        return diff;
    }
}
