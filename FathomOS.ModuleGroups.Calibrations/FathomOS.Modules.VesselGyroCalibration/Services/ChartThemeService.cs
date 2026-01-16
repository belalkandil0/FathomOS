using OxyPlot;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.VesselGyroCalibration.Services;

/// <summary>
/// Provides color palettes and theming for OxyPlot charts.
/// </summary>
public class ChartThemeService : INotifyPropertyChanged
{
    private static ChartThemeService? _instance;
    public static ChartThemeService Instance => _instance ??= new ChartThemeService();

    private ChartColorPalette _currentPalette;

    public ChartThemeService()
    {
        _currentPalette = AvailablePalettes[0]; // Default to "Vibrant"
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Currently selected color palette
    /// </summary>
    public ChartColorPalette CurrentPalette
    {
        get => _currentPalette;
        set
        {
            if (_currentPalette != value)
            {
                _currentPalette = value;
                OnPropertyChanged();
                PaletteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Event fired when palette changes - charts should refresh
    /// </summary>
    public event EventHandler? PaletteChanged;

    /// <summary>
    /// All available color palettes
    /// </summary>
    public static List<ChartColorPalette> AvailablePalettes { get; } = new()
    {
        new ChartColorPalette
        {
            Name = "Vibrant",
            Description = "Bold, high-contrast colors",
            AcceptedPoints = OxyColor.FromRgb(46, 204, 113),    // Green
            RejectedPoints = OxyColor.FromRgb(231, 76, 60),     // Red
            MeanLine = OxyColor.FromRgb(241, 196, 15),          // Yellow
            UpperLimit = OxyColor.FromRgb(230, 126, 34),        // Orange
            LowerLimit = OxyColor.FromRgb(230, 126, 34),        // Orange
            WarningLine = OxyColor.FromRgb(155, 89, 182),       // Purple
            DataSeries1 = OxyColor.FromRgb(52, 152, 219),       // Blue
            DataSeries2 = OxyColor.FromRgb(26, 188, 156),       // Teal
            DataSeries3 = OxyColor.FromRgb(155, 89, 182),       // Purple
            DataSeries4 = OxyColor.FromRgb(241, 196, 15),       // Yellow
            DataSeries5 = OxyColor.FromRgb(230, 126, 34),       // Orange
            HistogramBars = OxyColor.FromRgb(52, 152, 219),     // Blue
            ReferenceLineColor = OxyColor.FromRgb(149, 165, 166), // Gray
            GridLines = OxyColor.FromRgb(80, 80, 100),
            Background = OxyColor.FromRgb(30, 30, 46),
            TextColor = OxyColors.White,
            AxisColor = OxyColors.White
        },
        new ChartColorPalette
        {
            Name = "Ocean",
            Description = "Cool blue and teal tones",
            AcceptedPoints = OxyColor.FromRgb(0, 188, 212),     // Cyan
            RejectedPoints = OxyColor.FromRgb(244, 67, 54),     // Red
            MeanLine = OxyColor.FromRgb(255, 235, 59),          // Yellow
            UpperLimit = OxyColor.FromRgb(255, 152, 0),         // Orange
            LowerLimit = OxyColor.FromRgb(255, 152, 0),         // Orange
            WarningLine = OxyColor.FromRgb(156, 39, 176),       // Purple
            DataSeries1 = OxyColor.FromRgb(33, 150, 243),       // Blue
            DataSeries2 = OxyColor.FromRgb(0, 150, 136),        // Teal
            DataSeries3 = OxyColor.FromRgb(63, 81, 181),        // Indigo
            DataSeries4 = OxyColor.FromRgb(0, 188, 212),        // Cyan
            DataSeries5 = OxyColor.FromRgb(139, 195, 74),       // Light Green
            HistogramBars = OxyColor.FromRgb(0, 150, 136),      // Teal
            ReferenceLineColor = OxyColor.FromRgb(176, 190, 197), // Blue Gray
            GridLines = OxyColor.FromRgb(55, 71, 79),
            Background = OxyColor.FromRgb(38, 50, 56),
            TextColor = OxyColors.White,
            AxisColor = OxyColor.FromRgb(176, 190, 197)
        },
        new ChartColorPalette
        {
            Name = "Sunset",
            Description = "Warm orange and red tones",
            AcceptedPoints = OxyColor.FromRgb(76, 175, 80),     // Green
            RejectedPoints = OxyColor.FromRgb(229, 57, 53),     // Red
            MeanLine = OxyColor.FromRgb(255, 241, 118),         // Light Yellow
            UpperLimit = OxyColor.FromRgb(255, 138, 101),       // Light Orange
            LowerLimit = OxyColor.FromRgb(255, 138, 101),       // Light Orange
            WarningLine = OxyColor.FromRgb(186, 104, 200),      // Light Purple
            DataSeries1 = OxyColor.FromRgb(255, 112, 67),       // Deep Orange
            DataSeries2 = OxyColor.FromRgb(255, 167, 38),       // Orange
            DataSeries3 = OxyColor.FromRgb(255, 202, 40),       // Amber
            DataSeries4 = OxyColor.FromRgb(251, 140, 0),        // Orange 800
            DataSeries5 = OxyColor.FromRgb(239, 83, 80),        // Red 400
            HistogramBars = OxyColor.FromRgb(255, 138, 101),    // Deep Orange 300
            ReferenceLineColor = OxyColor.FromRgb(161, 136, 127), // Brown 300
            GridLines = OxyColor.FromRgb(78, 52, 46),
            Background = OxyColor.FromRgb(62, 39, 35),
            TextColor = OxyColor.FromRgb(255, 243, 224),
            AxisColor = OxyColor.FromRgb(215, 204, 200)
        },
        new ChartColorPalette
        {
            Name = "Forest",
            Description = "Natural green tones",
            AcceptedPoints = OxyColor.FromRgb(129, 199, 132),   // Light Green
            RejectedPoints = OxyColor.FromRgb(239, 83, 80),     // Red
            MeanLine = OxyColor.FromRgb(255, 241, 118),         // Yellow
            UpperLimit = OxyColor.FromRgb(255, 183, 77),        // Orange
            LowerLimit = OxyColor.FromRgb(255, 183, 77),        // Orange
            WarningLine = OxyColor.FromRgb(149, 117, 205),      // Purple
            DataSeries1 = OxyColor.FromRgb(67, 160, 71),        // Green
            DataSeries2 = OxyColor.FromRgb(102, 187, 106),      // Light Green
            DataSeries3 = OxyColor.FromRgb(156, 204, 101),      // Lime
            DataSeries4 = OxyColor.FromRgb(0, 150, 136),        // Teal
            DataSeries5 = OxyColor.FromRgb(77, 182, 172),       // Teal 300
            HistogramBars = OxyColor.FromRgb(76, 175, 80),      // Green
            ReferenceLineColor = OxyColor.FromRgb(165, 214, 167), // Green 200
            GridLines = OxyColor.FromRgb(46, 64, 51),
            Background = OxyColor.FromRgb(27, 38, 33),
            TextColor = OxyColor.FromRgb(232, 245, 233),
            AxisColor = OxyColor.FromRgb(200, 230, 201)
        },
        new ChartColorPalette
        {
            Name = "Monochrome",
            Description = "Professional grayscale with accent",
            AcceptedPoints = OxyColor.FromRgb(66, 165, 245),    // Blue accent
            RejectedPoints = OxyColor.FromRgb(239, 83, 80),     // Red accent
            MeanLine = OxyColor.FromRgb(255, 235, 59),          // Yellow
            UpperLimit = OxyColor.FromRgb(255, 167, 38),        // Orange
            LowerLimit = OxyColor.FromRgb(255, 167, 38),        // Orange
            WarningLine = OxyColor.FromRgb(171, 71, 188),       // Purple
            DataSeries1 = OxyColor.FromRgb(97, 97, 97),         // Gray 700
            DataSeries2 = OxyColor.FromRgb(117, 117, 117),      // Gray 600
            DataSeries3 = OxyColor.FromRgb(158, 158, 158),      // Gray 500
            DataSeries4 = OxyColor.FromRgb(189, 189, 189),      // Gray 400
            DataSeries5 = OxyColor.FromRgb(224, 224, 224),      // Gray 300
            HistogramBars = OxyColor.FromRgb(66, 165, 245),     // Blue accent
            ReferenceLineColor = OxyColor.FromRgb(158, 158, 158), // Gray
            GridLines = OxyColor.FromRgb(66, 66, 66),
            Background = OxyColor.FromRgb(33, 33, 33),
            TextColor = OxyColors.White,
            AxisColor = OxyColor.FromRgb(189, 189, 189)
        },
        new ChartColorPalette
        {
            Name = "Light",
            Description = "Light background for printing",
            AcceptedPoints = OxyColor.FromRgb(76, 175, 80),     // Green
            RejectedPoints = OxyColor.FromRgb(244, 67, 54),     // Red
            MeanLine = OxyColor.FromRgb(255, 152, 0),           // Orange
            UpperLimit = OxyColor.FromRgb(233, 30, 99),         // Pink
            LowerLimit = OxyColor.FromRgb(233, 30, 99),         // Pink
            WarningLine = OxyColor.FromRgb(156, 39, 176),       // Purple
            DataSeries1 = OxyColor.FromRgb(33, 150, 243),       // Blue
            DataSeries2 = OxyColor.FromRgb(0, 150, 136),        // Teal
            DataSeries3 = OxyColor.FromRgb(103, 58, 183),       // Deep Purple
            DataSeries4 = OxyColor.FromRgb(255, 152, 0),        // Orange
            DataSeries5 = OxyColor.FromRgb(233, 30, 99),        // Pink
            HistogramBars = OxyColor.FromRgb(33, 150, 243),     // Blue
            ReferenceLineColor = OxyColor.FromRgb(158, 158, 158), // Gray
            GridLines = OxyColor.FromRgb(224, 224, 224),
            Background = OxyColors.White,
            TextColor = OxyColor.FromRgb(33, 33, 33),
            AxisColor = OxyColor.FromRgb(97, 97, 97)
        }
    };

    /// <summary>
    /// Apply current palette theme to a PlotModel
    /// </summary>
    public void ApplyTheme(PlotModel plot)
    {
        plot.Background = CurrentPalette.Background;
        plot.TextColor = CurrentPalette.TextColor;
        plot.PlotAreaBorderColor = CurrentPalette.GridLines;
        
        foreach (var axis in plot.Axes)
        {
            axis.AxislineColor = CurrentPalette.AxisColor;
            axis.TicklineColor = CurrentPalette.AxisColor;
            axis.TitleColor = CurrentPalette.TextColor;
            axis.TextColor = CurrentPalette.TextColor;
            axis.MajorGridlineColor = CurrentPalette.GridLines;
            axis.MinorGridlineColor = OxyColor.FromAColor(128, CurrentPalette.GridLines);
        }
    }

    /// <summary>
    /// Get color for data series by index (cycles through available colors)
    /// </summary>
    public OxyColor GetSeriesColor(int index)
    {
        var colors = new[] 
        { 
            CurrentPalette.DataSeries1, 
            CurrentPalette.DataSeries2, 
            CurrentPalette.DataSeries3, 
            CurrentPalette.DataSeries4, 
            CurrentPalette.DataSeries5 
        };
        return colors[index % colors.Length];
    }
}

/// <summary>
/// Defines a complete color palette for charts
/// </summary>
public class ChartColorPalette
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    
    // Data point colors
    public OxyColor AcceptedPoints { get; set; }
    public OxyColor RejectedPoints { get; set; }
    
    // Line colors
    public OxyColor MeanLine { get; set; }
    public OxyColor UpperLimit { get; set; }
    public OxyColor LowerLimit { get; set; }
    public OxyColor WarningLine { get; set; }
    
    // Series colors (for multiple data series)
    public OxyColor DataSeries1 { get; set; }
    public OxyColor DataSeries2 { get; set; }
    public OxyColor DataSeries3 { get; set; }
    public OxyColor DataSeries4 { get; set; }
    public OxyColor DataSeries5 { get; set; }
    
    // Other elements
    public OxyColor HistogramBars { get; set; }
    public OxyColor ReferenceLineColor { get; set; }
    
    // Background/Theme
    public OxyColor GridLines { get; set; }
    public OxyColor Background { get; set; }
    public OxyColor TextColor { get; set; }
    public OxyColor AxisColor { get; set; }

    public override string ToString() => Name;
}
