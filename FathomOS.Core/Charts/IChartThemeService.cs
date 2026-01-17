namespace FathomOS.Core.Charts;

using OxyPlot;

/// <summary>
/// Service for applying consistent theming to OxyPlot charts across all modules.
/// </summary>
public interface IChartThemeService
{
    /// <summary>
    /// Gets the current chart theme (based on app theme).
    /// </summary>
    ChartTheme CurrentTheme { get; }

    /// <summary>
    /// Applies the current theme to a PlotModel.
    /// </summary>
    void ApplyTheme(PlotModel plotModel);

    /// <summary>
    /// Gets the default axis style for the current theme.
    /// </summary>
    OxyColor GetAxisColor();

    /// <summary>
    /// Gets the background color for the current theme.
    /// </summary>
    OxyColor GetBackgroundColor();

    /// <summary>
    /// Gets the text color for the current theme.
    /// </summary>
    OxyColor GetTextColor();

    /// <summary>
    /// Gets a series color from the palette.
    /// </summary>
    OxyColor GetSeriesColor(int index);

    /// <summary>
    /// Event fired when chart theme changes.
    /// </summary>
    event EventHandler? ThemeChanged;
}

/// <summary>
/// Defines the available chart themes.
/// </summary>
public enum ChartTheme
{
    /// <summary>
    /// Dark theme for charts (matches dark app theme).
    /// </summary>
    Dark,

    /// <summary>
    /// Light theme for charts (matches light app theme).
    /// </summary>
    Light
}
