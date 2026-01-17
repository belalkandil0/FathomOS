using FathomOS.Core.Interfaces;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;

namespace FathomOS.Core.Charts;

/// <summary>
/// Centralized service for applying consistent theming to OxyPlot charts.
/// Subscribes to IThemeService to stay synchronized with the application theme.
/// </summary>
public class ChartThemeService : IChartThemeService, IDisposable
{
    #region Fields

    private readonly IThemeService? _themeService;
    private ChartTheme _currentTheme = ChartTheme.Dark;
    private bool _disposed;

    #endregion

    #region Color Palettes

    /// <summary>
    /// Series color palette for dark theme.
    /// Professional, high-contrast colors optimized for dark backgrounds.
    /// </summary>
    private static readonly OxyColor[] DarkThemeSeriesColors =
    {
        OxyColor.FromRgb(0, 191, 255),   // Deep Sky Blue
        OxyColor.FromRgb(50, 205, 50),   // Lime Green
        OxyColor.FromRgb(255, 165, 0),   // Orange
        OxyColor.FromRgb(255, 99, 132),  // Pink/Red
        OxyColor.FromRgb(153, 102, 255), // Purple
        OxyColor.FromRgb(255, 206, 86),  // Yellow
        OxyColor.FromRgb(75, 192, 192),  // Teal
        OxyColor.FromRgb(255, 159, 64)   // Light Orange
    };

    /// <summary>
    /// Series color palette for light theme.
    /// Darker, more saturated colors optimized for light backgrounds.
    /// </summary>
    private static readonly OxyColor[] LightThemeSeriesColors =
    {
        OxyColor.FromRgb(0, 123, 255),   // Blue
        OxyColor.FromRgb(40, 167, 69),   // Green
        OxyColor.FromRgb(255, 128, 0),   // Orange
        OxyColor.FromRgb(220, 53, 69),   // Red
        OxyColor.FromRgb(111, 66, 193),  // Purple
        OxyColor.FromRgb(255, 193, 7),   // Yellow/Amber
        OxyColor.FromRgb(23, 162, 184),  // Cyan/Teal
        OxyColor.FromRgb(253, 126, 20)   // Dark Orange
    };

    /// <summary>
    /// Dark theme colors.
    /// </summary>
    private static class DarkTheme
    {
        public static readonly OxyColor Background = OxyColor.FromRgb(30, 30, 30);
        public static readonly OxyColor PlotAreaBackground = OxyColor.FromRgb(40, 40, 40);
        public static readonly OxyColor PlotAreaBorder = OxyColor.FromRgb(80, 80, 80);
        public static readonly OxyColor AxisLine = OxyColor.FromRgb(120, 120, 120);
        public static readonly OxyColor MajorGridLine = OxyColor.FromRgb(60, 60, 60);
        public static readonly OxyColor MinorGridLine = OxyColor.FromRgb(50, 50, 50);
        public static readonly OxyColor Text = OxyColor.FromRgb(220, 220, 220);
        public static readonly OxyColor SubtitleText = OxyColor.FromRgb(180, 180, 180);
        public static readonly OxyColor LegendBackground = OxyColor.FromArgb(200, 40, 40, 40);
        public static readonly OxyColor LegendBorder = OxyColor.FromRgb(80, 80, 80);
    }

    /// <summary>
    /// Light theme colors.
    /// </summary>
    private static class LightTheme
    {
        public static readonly OxyColor Background = OxyColor.FromRgb(255, 255, 255);
        public static readonly OxyColor PlotAreaBackground = OxyColor.FromRgb(250, 250, 250);
        public static readonly OxyColor PlotAreaBorder = OxyColor.FromRgb(180, 180, 180);
        public static readonly OxyColor AxisLine = OxyColor.FromRgb(100, 100, 100);
        public static readonly OxyColor MajorGridLine = OxyColor.FromRgb(220, 220, 220);
        public static readonly OxyColor MinorGridLine = OxyColor.FromRgb(240, 240, 240);
        public static readonly OxyColor Text = OxyColor.FromRgb(30, 30, 30);
        public static readonly OxyColor SubtitleText = OxyColor.FromRgb(80, 80, 80);
        public static readonly OxyColor LegendBackground = OxyColor.FromArgb(230, 255, 255, 255);
        public static readonly OxyColor LegendBorder = OxyColor.FromRgb(180, 180, 180);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ChartThemeService class.
    /// </summary>
    /// <param name="themeService">The application theme service to sync with.</param>
    public ChartThemeService(IThemeService? themeService = null)
    {
        _themeService = themeService;

        if (_themeService != null)
        {
            // Sync initial theme
            _currentTheme = _themeService.CurrentTheme == AppTheme.Dark
                ? ChartTheme.Dark
                : ChartTheme.Light;

            // Subscribe to theme changes
            _themeService.ThemeChanged += OnAppThemeChanged;
        }
    }

    #endregion

    #region IChartThemeService Properties

    /// <inheritdoc />
    public ChartTheme CurrentTheme => _currentTheme;

    #endregion

    #region IChartThemeService Events

    /// <inheritdoc />
    public event EventHandler? ThemeChanged;

    #endregion

    #region IChartThemeService Methods

    /// <inheritdoc />
    public void ApplyTheme(PlotModel plotModel)
    {
        if (plotModel == null)
        {
            throw new ArgumentNullException(nameof(plotModel));
        }

        bool isDark = _currentTheme == ChartTheme.Dark;

        // Apply background colors
        plotModel.Background = isDark ? DarkTheme.Background : LightTheme.Background;
        plotModel.PlotAreaBackground = isDark ? DarkTheme.PlotAreaBackground : LightTheme.PlotAreaBackground;
        plotModel.PlotAreaBorderColor = isDark ? DarkTheme.PlotAreaBorder : LightTheme.PlotAreaBorder;
        plotModel.PlotAreaBorderThickness = new OxyThickness(1);

        // Apply text colors
        plotModel.TextColor = isDark ? DarkTheme.Text : LightTheme.Text;
        plotModel.SubtitleColor = isDark ? DarkTheme.SubtitleText : LightTheme.SubtitleText;
        plotModel.TitleColor = isDark ? DarkTheme.Text : LightTheme.Text;

        // Apply legend styling (OxyPlot 2.1+ uses Legends collection)
        foreach (var legend in plotModel.Legends)
        {
            ApplyLegendTheme(legend, isDark);
        }

        // Apply axis styling
        foreach (var axis in plotModel.Axes)
        {
            ApplyAxisTheme(axis, isDark);
        }

        // Invalidate the plot to apply changes
        plotModel.InvalidatePlot(false);
    }

    /// <inheritdoc />
    public OxyColor GetAxisColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.AxisLine
            : LightTheme.AxisLine;
    }

    /// <inheritdoc />
    public OxyColor GetBackgroundColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.Background
            : LightTheme.Background;
    }

    /// <inheritdoc />
    public OxyColor GetTextColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.Text
            : LightTheme.Text;
    }

    /// <inheritdoc />
    public OxyColor GetSeriesColor(int index)
    {
        var palette = _currentTheme == ChartTheme.Dark
            ? DarkThemeSeriesColors
            : LightThemeSeriesColors;

        // Use modulo to cycle through colors if index exceeds palette size
        int safeIndex = Math.Abs(index) % palette.Length;
        return palette[safeIndex];
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the plot area background color for the current theme.
    /// </summary>
    public OxyColor GetPlotAreaBackgroundColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.PlotAreaBackground
            : LightTheme.PlotAreaBackground;
    }

    /// <summary>
    /// Gets the plot area border color for the current theme.
    /// </summary>
    public OxyColor GetPlotAreaBorderColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.PlotAreaBorder
            : LightTheme.PlotAreaBorder;
    }

    /// <summary>
    /// Gets the major grid line color for the current theme.
    /// </summary>
    public OxyColor GetMajorGridLineColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.MajorGridLine
            : LightTheme.MajorGridLine;
    }

    /// <summary>
    /// Gets the minor grid line color for the current theme.
    /// </summary>
    public OxyColor GetMinorGridLineColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.MinorGridLine
            : LightTheme.MinorGridLine;
    }

    /// <summary>
    /// Gets the legend background color for the current theme.
    /// </summary>
    public OxyColor GetLegendBackgroundColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.LegendBackground
            : LightTheme.LegendBackground;
    }

    /// <summary>
    /// Gets the legend border color for the current theme.
    /// </summary>
    public OxyColor GetLegendBorderColor()
    {
        return _currentTheme == ChartTheme.Dark
            ? DarkTheme.LegendBorder
            : LightTheme.LegendBorder;
    }

    /// <summary>
    /// Creates a pre-configured PlotModel with the current theme applied.
    /// </summary>
    /// <param name="title">Optional title for the plot.</param>
    /// <returns>A new PlotModel with the current theme applied.</returns>
    public PlotModel CreateThemedPlotModel(string? title = null)
    {
        var plotModel = new PlotModel
        {
            Title = title ?? string.Empty
        };

        ApplyTheme(plotModel);
        return plotModel;
    }

    /// <summary>
    /// Creates a themed legend that can be added to a PlotModel.
    /// </summary>
    /// <param name="position">The legend position (default: RightTop).</param>
    /// <returns>A new Legend with the current theme applied.</returns>
    public Legend CreateThemedLegend(LegendPosition position = LegendPosition.RightTop)
    {
        bool isDark = _currentTheme == ChartTheme.Dark;

        var legend = new Legend
        {
            LegendPosition = position,
            LegendPlacement = LegendPlacement.Inside,
            LegendBackground = isDark ? DarkTheme.LegendBackground : LightTheme.LegendBackground,
            LegendBorder = isDark ? DarkTheme.LegendBorder : LightTheme.LegendBorder,
            LegendBorderThickness = 1,
            LegendTextColor = isDark ? DarkTheme.Text : LightTheme.Text,
            LegendTitleColor = isDark ? DarkTheme.Text : LightTheme.Text
        };

        return legend;
    }

    /// <summary>
    /// Manually sets the chart theme (useful when IThemeService is not available).
    /// </summary>
    /// <param name="theme">The theme to set.</param>
    public void SetTheme(ChartTheme theme)
    {
        if (_currentTheme == theme)
        {
            return;
        }

        _currentTheme = theme;
        OnThemeChanged();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Applies theme styling to a legend.
    /// </summary>
    private void ApplyLegendTheme(LegendBase legend, bool isDark)
    {
        legend.LegendBackground = isDark ? DarkTheme.LegendBackground : LightTheme.LegendBackground;
        legend.LegendBorder = isDark ? DarkTheme.LegendBorder : LightTheme.LegendBorder;
        legend.LegendBorderThickness = 1;
        legend.LegendTextColor = isDark ? DarkTheme.Text : LightTheme.Text;
        legend.LegendTitleColor = isDark ? DarkTheme.Text : LightTheme.Text;
    }

    /// <summary>
    /// Applies theme styling to an axis.
    /// </summary>
    private void ApplyAxisTheme(Axis axis, bool isDark)
    {
        axis.AxislineColor = isDark ? DarkTheme.AxisLine : LightTheme.AxisLine;
        axis.AxislineStyle = LineStyle.Solid;
        axis.AxislineThickness = 1;

        axis.MajorGridlineColor = isDark ? DarkTheme.MajorGridLine : LightTheme.MajorGridLine;
        axis.MajorGridlineStyle = LineStyle.Solid;

        axis.MinorGridlineColor = isDark ? DarkTheme.MinorGridLine : LightTheme.MinorGridLine;
        axis.MinorGridlineStyle = LineStyle.Dot;

        axis.TicklineColor = isDark ? DarkTheme.AxisLine : LightTheme.AxisLine;
        axis.TextColor = isDark ? DarkTheme.Text : LightTheme.Text;
        axis.TitleColor = isDark ? DarkTheme.Text : LightTheme.Text;

        axis.ExtraGridlineColor = isDark ? DarkTheme.MajorGridLine : LightTheme.MajorGridLine;
    }

    /// <summary>
    /// Handles application theme changes.
    /// </summary>
    private void OnAppThemeChanged(object? sender, AppTheme newTheme)
    {
        var newChartTheme = newTheme == AppTheme.Dark ? ChartTheme.Dark : ChartTheme.Light;

        if (_currentTheme == newChartTheme)
        {
            return;
        }

        _currentTheme = newChartTheme;
        OnThemeChanged();
    }

    /// <summary>
    /// Raises the ThemeChanged event.
    /// </summary>
    private void OnThemeChanged()
    {
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes of the service and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the service.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Unsubscribe from theme service events
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnAppThemeChanged;
            }
        }

        _disposed = true;
    }

    #endregion
}
