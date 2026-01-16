namespace FathomOS.Core.Models;

/// <summary>
/// Supported length/distance units for coordinates and KP values
/// </summary>
public enum LengthUnit
{
    Meter,
    Kilometer,
    USSurveyFeet,
    InternationalFeet,
    NauticalMile,
    InternationalMile,
    Yard,
    Fathom
}

/// <summary>
/// Display names and conversion factors for length units
/// </summary>
public static class LengthUnitExtensions
{
    /// <summary>
    /// Get the display name for a unit
    /// </summary>
    public static string GetDisplayName(this LengthUnit unit) => unit switch
    {
        LengthUnit.Meter => "Meters",
        LengthUnit.Kilometer => "Kilometers",
        LengthUnit.USSurveyFeet => "US Survey Feet",
        LengthUnit.InternationalFeet => "International Feet",
        LengthUnit.NauticalMile => "Nautical Miles",
        LengthUnit.InternationalMile => "International Miles",
        LengthUnit.Yard => "Yards",
        LengthUnit.Fathom => "Fathoms",
        _ => unit.ToString()
    };

    /// <summary>
    /// Get the short abbreviation for a unit
    /// </summary>
    public static string GetAbbreviation(this LengthUnit unit) => unit switch
    {
        LengthUnit.Meter => "m",
        LengthUnit.Kilometer => "km",
        LengthUnit.USSurveyFeet => "ftUS",
        LengthUnit.InternationalFeet => "ft",
        LengthUnit.NauticalMile => "nm",
        LengthUnit.InternationalMile => "mi",
        LengthUnit.Yard => "yd",
        LengthUnit.Fathom => "fth",
        _ => unit.ToString()
    };

    /// <summary>
    /// Parse unit string from RLX file header
    /// </summary>
    public static LengthUnit ParseFromRlx(string unitString)
    {
        var normalized = unitString.Trim().Trim('"').ToLowerInvariant();
        return normalized switch
        {
            "meter" or "meters" or "m" => LengthUnit.Meter,
            "kilometer" or "kilometers" or "km" => LengthUnit.Kilometer,
            "usfeet" or "usfoot" or "us feet" or "us survey feet" or "ftus" => LengthUnit.USSurveyFeet,
            "feet" or "foot" or "feet (international)" or "ft" => LengthUnit.InternationalFeet,
            "mile (nautical int.)" or "nautical mile" or "nm" => LengthUnit.NauticalMile,
            "mile (international)" or "mile" or "mi" => LengthUnit.InternationalMile,
            "yard" or "yards" or "yard (international)" or "yd" => LengthUnit.Yard,
            "fathom" or "fathoms" or "fth" => LengthUnit.Fathom,
            _ => LengthUnit.Meter // Default
        };
    }
}
