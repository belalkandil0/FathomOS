using FathomOS.Core.Services;
using FathomOS.Core.Interfaces;

namespace FathomOS.Modules.SurveyListing.Settings;

/// <summary>
/// Survey Listing module-specific settings.
/// Inherits common settings from ModuleSettingsBase.
/// </summary>
public class SurveyListingSettings : ModuleSettingsBase
{
    /// <summary>
    /// Module identifier for settings storage.
    /// </summary>
    public const string MODULE_ID = "SurveyListing";

    #region Default Values

    /// <summary>
    /// Default depth decimal places.
    /// </summary>
    public const int DEFAULT_DEPTH_DECIMALS = 2;

    /// <summary>
    /// Default position decimal places.
    /// </summary>
    public const int DEFAULT_POSITION_DECIMALS = 3;

    /// <summary>
    /// Default KP decimal places.
    /// </summary>
    public const int DEFAULT_KP_DECIMALS = 6;

    #endregion

    #region Appearance Settings

    /// <summary>
    /// Whether to use dark theme (synced with Shell theme).
    /// </summary>
    public bool UseDarkTheme { get; set; } = true;

    /// <summary>
    /// Whether to show the step indicator.
    /// </summary>
    public bool ShowStepIndicator { get; set; } = true;

    #endregion

    #region Processing Settings

    /// <summary>
    /// Number of decimal places for depth values.
    /// </summary>
    public int DepthDecimalPlaces { get; set; } = DEFAULT_DEPTH_DECIMALS;

    /// <summary>
    /// Number of decimal places for position (Easting/Northing) values.
    /// </summary>
    public int PositionDecimalPlaces { get; set; } = DEFAULT_POSITION_DECIMALS;

    /// <summary>
    /// Number of decimal places for KP values.
    /// </summary>
    public int KpDecimalPlaces { get; set; } = DEFAULT_KP_DECIMALS;

    /// <summary>
    /// Default smoothing window size.
    /// </summary>
    public int DefaultSmoothingWindow { get; set; } = 5;

    /// <summary>
    /// Default smoothing method.
    /// </summary>
    public string DefaultSmoothingMethod { get; set; } = "MovingAverage";

    /// <summary>
    /// Whether to apply tide corrections by default.
    /// </summary>
    public bool ApplyTideCorrectionByDefault { get; set; } = true;

    /// <summary>
    /// Default length unit (Meters or Kilometers).
    /// </summary>
    public string DefaultLengthUnit { get; set; } = "Meters";

    #endregion

    #region Export Settings

    /// <summary>
    /// Default export format.
    /// </summary>
    public string DefaultExportFormat { get; set; } = "xlsx";

    /// <summary>
    /// Whether to include headers in exports.
    /// </summary>
    public bool IncludeHeadersInExport { get; set; } = true;

    /// <summary>
    /// Whether to auto-generate output filename.
    /// </summary>
    public bool AutoGenerateOutputFilename { get; set; } = true;

    /// <summary>
    /// Output filename template (supports placeholders like {ProjectName}, {Date}).
    /// </summary>
    public string OutputFilenameTemplate { get; set; } = "{ProjectName}_{Date}";

    /// <summary>
    /// Whether to open output folder after export.
    /// </summary>
    public bool OpenFolderAfterExport { get; set; } = false;

    #endregion

    #region Certificate Settings

    /// <summary>
    /// Whether to prompt for certificate generation after processing.
    /// </summary>
    public bool PromptForCertificate { get; set; } = true;

    /// <summary>
    /// Default signatory name for certificates.
    /// </summary>
    public string? DefaultSignatoryName { get; set; }

    /// <summary>
    /// Default signatory title for certificates.
    /// </summary>
    public string? DefaultSignatoryTitle { get; set; }

    /// <summary>
    /// Default company name for certificates.
    /// </summary>
    public string? DefaultCompanyName { get; set; }

    #endregion

    #region Chart Settings

    /// <summary>
    /// Default chart line color (hex).
    /// </summary>
    public string ChartLineColor { get; set; } = "#007ACC";

    /// <summary>
    /// Default chart background color (hex).
    /// </summary>
    public string ChartBackgroundColor { get; set; } = "#1E1E1E";

    /// <summary>
    /// Whether to show grid lines on charts.
    /// </summary>
    public bool ShowChartGridLines { get; set; } = true;

    /// <summary>
    /// Whether to show data points on charts.
    /// </summary>
    public bool ShowChartDataPoints { get; set; } = false;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Loads the SurveyListing settings.
    /// </summary>
    /// <returns>The loaded or default settings.</returns>
    public static SurveyListingSettings Load()
    {
        return ModuleSettings.Load<SurveyListingSettings>(MODULE_ID);
    }

    /// <summary>
    /// Saves the current settings.
    /// </summary>
    /// <param name="settings">Settings to save.</param>
    public static void Save(SurveyListingSettings settings)
    {
        ModuleSettings.Save(MODULE_ID, settings);
    }

    #endregion
}
