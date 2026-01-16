namespace FathomOS.Modules.SoundVelocity.Models;

/// <summary>
/// Input file type selection (from dlgAlpha in VBA)
/// </summary>
public enum InputFileType
{
    /// <summary>QINSy Log Files - Space delimited</summary>
    QINSyLog,
    /// <summary>QINSy Export/CSV Files - Comma delimited</summary>
    QINSyExport,
    /// <summary>Valeport CTD Files - Tab delimited (.000, .001, .txt)</summary>
    Valeport,
    /// <summary>Fixed Width Files (Tritech BP3)</summary>
    FixedWidth,
    /// <summary>Semicolon delimited (SAIV SD024)</summary>
    Semicolon
}

/// <summary>
/// Calculation mode (from dlgAlpha OptSV options)
/// </summary>
public enum CalculationMode
{
    /// <summary>CTD/SV calculation using Salinity (iOptCalc = 1)</summary>
    CtdSvWithSalinity = 1,
    /// <summary>SV Profile only - external SV values (iOptCalc = 2)</summary>
    SvOnly = 2,
    /// <summary>CTD/SV calculation using Conductivity (iOptCalc = 3)</summary>
    CtdSvWithConductivity = 3
}

/// <summary>
/// Sound velocity formula selection (from dlgData cSVFormula)
/// </summary>
public enum SoundVelocityFormula
{
    /// <summary>Use sound velocity from input file</summary>
    ExternalSource = 0,
    /// <summary>Chen &amp; Millero formula (for depth ≤1000m)</summary>
    ChenMillero = 1,
    /// <summary>Del Grosso formula (for depth >1000m)</summary>
    DelGrosso = 2,
    /// <summary>Auto-select based on depth (Chen &amp; Millero ≤1000m, Del Grosso >1000m)</summary>
    Auto = 3
}

/// <summary>
/// Density formula selection (from dlgData cDFormula)
/// </summary>
public enum DensityFormula
{
    /// <summary>Use density from input file</summary>
    ExternalSource = 0,
    /// <summary>UNESCO EOS-80 equation of state</summary>
    UnescoEOS80 = 1
}

/// <summary>
/// Input data type for first column
/// </summary>
public enum DepthPressureType
{
    /// <summary>Input is Depth in meters</summary>
    Depth = 1,
    /// <summary>Input is Pressure in dbar</summary>
    Pressure = 2
}

/// <summary>
/// Geographic coordinate format
/// </summary>
public enum GeoCoordinateFormat
{
    /// <summary>Degrees; Minutes; Seconds (DD;MM;SS.ss N)</summary>
    DMS = 0,
    /// <summary>Degrees; Decimal Minutes (DD;MM.mmmm N)</summary>
    DM = 1,
    /// <summary>Decimal Degrees (DD.dddd N)</summary>
    DD = 2
}
