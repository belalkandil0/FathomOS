using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using FathomOS.Core.Models;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace FathomOS.Modules.SurveyListing.Views;

public partial class ChartWindow : MetroWindow
{
    private List<SurveyPoint> _surveyPoints = new();
    private RouteData? _routeData;
    private bool _isLoaded = false;

    public ChartWindow()
    {
        InitializeComponent();
    }
    
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        if (_surveyPoints.Count > 0)
        {
            UpdateChart();
        }
    }

    public void LoadSurveyData(IList<SurveyPoint> points, RouteData? route)
    {
        _surveyPoints = points?.ToList() ?? new List<SurveyPoint>();
        _routeData = route;
        
        // Only update if window is loaded and we have data
        if (_isLoaded && _surveyPoints.Count > 0 && CboChartType != null)
        {
            UpdateChart();
        }
    }

    private void ChartType_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoaded)
        {
            UpdateChart();
        }
    }

    private void UpdateChart()
    {
        try
        {
            if (CboChartType == null || CboChartType.SelectedIndex < 0 || _surveyPoints.Count == 0) return;
            
            if (CboChartType.SelectedIndex == 7) // All Charts
            {
                SingleChart.Visibility = Visibility.Collapsed;
                MultiChartGrid.Visibility = Visibility.Visible;
                
                Chart1.Model = CreateKpDepthChart();
                Chart2.Model = CreateKpDccChart();
                Chart3.Model = CreateDepthHistogram();
                Chart4.Model = CreateTideCorrectionChart();
            }
            else if (CboChartType.SelectedIndex == 6) // Statistics Dashboard
            {
                SingleChart.Visibility = Visibility.Collapsed;
                MultiChartGrid.Visibility = Visibility.Visible;
                
                Chart1.Model = CreateStatisticsSummaryChart();
                Chart2.Model = CreateDepthHistogram();
                Chart3.Model = CreateDccDistribution();
                Chart4.Model = CreatePointDensityChart();
            }
            else
            {
                SingleChart.Visibility = Visibility.Visible;
                MultiChartGrid.Visibility = Visibility.Collapsed;
                
                SingleChart.Model = CboChartType.SelectedIndex switch
                {
                    0 => CreateKpDepthChart(),
                    1 => CreateKpDccChart(),
                    2 => CreatePlanViewChart(),
                    3 => CreateDepthHistogram(),
                    4 => CreateDccDistribution(),
                    5 => CreateTideCorrectionChart(),
                    _ => CreateKpDepthChart()
                };
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error creating chart: {ex.Message}", "Chart Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private PlotModel CreateKpDepthChart()
    {
        var model = new PlotModel
        {
            Title = "KP vs Depth Profile",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            Background = OxyColor.FromRgb(30, 30, 30)
        };

        // Axes
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "KP (km)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Depth (ft)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            StartPosition = 1,  // Invert depth axis
            EndPosition = 0
        });

        // Check if we have KP data
        var pointsWithKp = _surveyPoints.Where(p => p.Kp.HasValue).OrderBy(p => p.Kp).ToList();
        if (pointsWithKp.Count == 0)
        {
            model.Subtitle = "No KP data available";
            model.SubtitleColor = OxyColors.Orange;
            return model;
        }

        // Raw data series
        var rawSeries = new LineSeries
        {
            Title = "Raw Depth",
            Color = OxyColor.FromRgb(100, 255, 100),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash
        };

        // Calculated data series
        var calcSeries = new LineSeries
        {
            Title = "Calculated Depth",
            Color = OxyColors.Cyan,
            StrokeThickness = 2
        };

        foreach (var pt in pointsWithKp)
        {
            rawSeries.Points.Add(new DataPoint(pt.Kp!.Value, pt.Depth ?? 0));
            calcSeries.Points.Add(new DataPoint(pt.Kp!.Value, pt.CalculatedZ ?? pt.Depth ?? 0));
        }

        model.Series.Add(rawSeries);
        model.Series.Add(calcSeries);

        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendBackground = OxyColor.FromArgb(180, 30, 30, 30),
            LegendTextColor = OxyColors.White
        });

        return model;
    }

    private PlotModel CreateKpDccChart()
    {
        var model = new PlotModel
        {
            Title = "KP vs DCC (Cross-Track Error)",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            Background = OxyColor.FromRgb(30, 30, 30)
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "KP (km)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "DCC (ft)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        // Add zero reference line
        model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
        {
            Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
            Y = 0,
            Color = OxyColors.Yellow,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash
        });

        // Check if we have KP/DCC data
        var pointsWithData = _surveyPoints.Where(p => p.Kp.HasValue && p.Dcc.HasValue).OrderBy(p => p.Kp).ToList();
        if (pointsWithData.Count == 0)
        {
            model.Subtitle = "No KP/DCC data available";
            model.SubtitleColor = OxyColors.Orange;
            return model;
        }

        var series = new LineSeries
        {
            Title = "DCC",
            Color = OxyColors.Orange,
            StrokeThickness = 2
        };

        foreach (var pt in pointsWithData)
        {
            series.Points.Add(new DataPoint(pt.Kp!.Value, pt.Dcc!.Value));
        }

        model.Series.Add(series);

        return model;
    }

    private PlotModel CreatePlanViewChart()
    {
        var model = new PlotModel
        {
            Title = "Plan View (Easting vs Northing)",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            Background = OxyColor.FromRgb(30, 30, 30),
            PlotType = PlotType.Cartesian
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Easting (ft)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Northing (ft)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        // Route
        if (_routeData != null)
        {
            var routeSeries = new LineSeries
            {
                Title = "Route",
                Color = OxyColors.Red,
                StrokeThickness = 2
            };

            foreach (var seg in _routeData.Segments)
            {
                if (routeSeries.Points.Count == 0 || 
                    (routeSeries.Points.Last().X != seg.StartEasting || 
                     routeSeries.Points.Last().Y != seg.StartNorthing))
                {
                    routeSeries.Points.Add(new DataPoint(seg.StartEasting, seg.StartNorthing));
                }
                routeSeries.Points.Add(new DataPoint(seg.EndEasting, seg.EndNorthing));
            }

            model.Series.Add(routeSeries);
        }

        // Raw survey points
        var rawSeries = new LineSeries
        {
            Title = "Survey (Raw)",
            Color = OxyColor.FromRgb(100, 255, 100),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dot
        };

        foreach (var pt in _surveyPoints)
        {
            rawSeries.Points.Add(new DataPoint(pt.Easting, pt.Northing));
        }

        model.Series.Add(rawSeries);

        // Smoothed survey points
        var smoothedSeries = new LineSeries
        {
            Title = "Survey (Smoothed)",
            Color = OxyColors.Cyan,
            StrokeThickness = 2
        };

        foreach (var pt in _surveyPoints)
        {
            double x = pt.SmoothedEasting ?? pt.Easting;
            double y = pt.SmoothedNorthing ?? pt.Northing;
            smoothedSeries.Points.Add(new DataPoint(x, y));
        }

        model.Series.Add(smoothedSeries);

        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendBackground = OxyColor.FromArgb(180, 30, 30, 30),
            LegendTextColor = OxyColors.White
        });

        return model;
    }

    private PlotModel CreateDepthHistogram()
    {
        var model = new PlotModel
        {
            Title = "Depth Distribution",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            Background = OxyColor.FromRgb(30, 30, 30)
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Depth (ft)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Count",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            Minimum = 0
        });

        if (_surveyPoints.Count == 0) return model;

        // Calculate histogram (filter out nulls)
        var depths = _surveyPoints
            .Where(p => p.CalculatedZ.HasValue)
            .Select(p => p.CalculatedZ!.Value)
            .ToList();
        
        if (depths.Count == 0) return model;
        
        double minDepth = depths.Min();
        double maxDepth = depths.Max();
        int binCount = 30;
        double binWidth = (maxDepth - minDepth) / binCount;
        
        if (binWidth <= 0) binWidth = 1;

        var histogram = new int[binCount];
        foreach (var depth in depths)
        {
            int bin = (int)((depth - minDepth) / binWidth);
            bin = Math.Min(bin, binCount - 1);
            histogram[bin]++;
        }

        var series = new RectangleBarSeries
        {
            Title = "Depth Count",
            FillColor = OxyColors.Cyan,
            StrokeColor = OxyColors.DarkCyan,
            StrokeThickness = 1
        };

        for (int i = 0; i < binCount; i++)
        {
            double x0 = minDepth + i * binWidth;
            double x1 = minDepth + (i + 1) * binWidth;
            series.Items.Add(new RectangleBarItem(x0, 0, x1, histogram[i]));
        }

        model.Series.Add(series);

        return model;
    }

    private PlotModel CreateDccDistribution()
    {
        var model = new PlotModel
        {
            Title = "DCC Distribution (Cross-Track Error)",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            Background = OxyColor.FromRgb(30, 30, 30)
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "DCC (ft)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Count",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            Minimum = 0
        });

        var dccValues = _surveyPoints.Where(p => p.Dcc.HasValue).Select(p => p.Dcc!.Value).ToList();
        
        if (dccValues.Count == 0) return model;

        // Calculate histogram
        double minDcc = dccValues.Min();
        double maxDcc = dccValues.Max();
        int binCount = 30;
        double binWidth = (maxDcc - minDcc) / binCount;
        
        if (binWidth <= 0) binWidth = 1;

        var histogram = new int[binCount];
        foreach (var dcc in dccValues)
        {
            int bin = (int)((dcc - minDcc) / binWidth);
            bin = Math.Min(bin, binCount - 1);
            histogram[bin]++;
        }

        var series = new RectangleBarSeries
        {
            Title = "DCC Count",
            FillColor = OxyColors.Orange,
            StrokeColor = OxyColors.DarkOrange,
            StrokeThickness = 1
        };

        for (int i = 0; i < binCount; i++)
        {
            double x0 = minDcc + i * binWidth;
            double x1 = minDcc + (i + 1) * binWidth;
            series.Items.Add(new RectangleBarItem(x0, 0, x1, histogram[i]));
        }

        model.Series.Add(series);

        // Add statistics annotation
        double mean = dccValues.Average();
        double stdDev = Math.Sqrt(dccValues.Average(d => Math.Pow(d - mean, 2)));
        
        model.Annotations.Add(new OxyPlot.Annotations.TextAnnotation
        {
            Text = $"Mean: {mean:F3}\nStd Dev: {stdDev:F3}",
            TextPosition = new DataPoint(maxDcc, histogram.Max() * 0.9),
            TextColor = OxyColors.White,
            Background = OxyColor.FromArgb(150, 30, 30, 30),
            Padding = new OxyThickness(5)
        });

        return model;
    }

    /// <summary>
    /// Create tide correction comparison chart (Before vs After)
    /// </summary>
    private PlotModel CreateTideCorrectionChart()
    {
        var model = new PlotModel
        {
            Title = "Tide Correction Comparison",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            Background = OxyColor.FromRgb(30, 30, 30)
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "KP (km)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Depth (ft)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            StartPosition = 1,  // Invert depth axis
            EndPosition = 0
        });

        // Check if we have tide correction data
        var pointsWithTide = _surveyPoints
            .Where(p => p.Kp.HasValue && p.TideCorrection.HasValue && p.TideCorrection != 0)
            .OrderBy(p => p.Kp)
            .ToList();
            
        if (pointsWithTide.Count == 0)
        {
            // No tide correction - show raw vs calculated
            var pointsWithKp = _surveyPoints.Where(p => p.Kp.HasValue).OrderBy(p => p.Kp).ToList();
            if (pointsWithKp.Count == 0)
            {
                model.Subtitle = "No KP data available";
                model.SubtitleColor = OxyColors.Orange;
                return model;
            }
            
            model.Subtitle = "No tide correction applied (showing Raw vs Calculated)";
            model.SubtitleColor = OxyColors.Yellow;
            
            var rawSeries = new LineSeries
            {
                Title = "Raw Depth",
                Color = OxyColor.FromRgb(255, 100, 100),
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash
            };

            var calcSeries = new LineSeries
            {
                Title = "Calculated Depth",
                Color = OxyColors.Cyan,
                StrokeThickness = 2
            };

            foreach (var pt in pointsWithKp)
            {
                rawSeries.Points.Add(new DataPoint(pt.Kp!.Value, pt.Depth ?? 0));
                calcSeries.Points.Add(new DataPoint(pt.Kp!.Value, pt.CalculatedZ ?? pt.Depth ?? 0));
            }

            model.Series.Add(rawSeries);
            model.Series.Add(calcSeries);
        }
        else
        {
            // Show before/after tide correction
            var beforeSeries = new LineSeries
            {
                Title = "Before Tide Correction",
                Color = OxyColor.FromRgb(255, 100, 100),
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash
            };

            var afterSeries = new LineSeries
            {
                Title = "After Tide Correction",
                Color = OxyColors.Lime,
                StrokeThickness = 2
            };

            var tideSeries = new LineSeries
            {
                Title = "Tide Correction",
                Color = OxyColors.Yellow,
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot,
                YAxisKey = "tide"
            };

            // Add secondary Y axis for tide correction value
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Right,
                Key = "tide",
                Title = "Tide (ft)",
                TitleColor = OxyColors.Yellow,
                AxislineColor = OxyColors.Yellow,
                TicklineColor = OxyColors.Yellow,
                TextColor = OxyColors.Yellow
            });

            foreach (var pt in pointsWithTide)
            {
                double rawDepth = pt.Depth ?? 0;
                double correctedDepth = pt.CalculatedZ ?? (rawDepth + (pt.TideCorrection ?? 0));
                
                beforeSeries.Points.Add(new DataPoint(pt.Kp!.Value, rawDepth));
                afterSeries.Points.Add(new DataPoint(pt.Kp!.Value, correctedDepth));
                tideSeries.Points.Add(new DataPoint(pt.Kp!.Value, pt.TideCorrection ?? 0));
            }

            model.Series.Add(beforeSeries);
            model.Series.Add(afterSeries);
            model.Series.Add(tideSeries);
            
            // Add statistics
            double avgTide = pointsWithTide.Average(p => p.TideCorrection ?? 0);
            double minTide = pointsWithTide.Min(p => p.TideCorrection ?? 0);
            double maxTide = pointsWithTide.Max(p => p.TideCorrection ?? 0);
            
            model.Subtitle = $"Tide: Avg={avgTide:F2}ft, Min={minTide:F2}ft, Max={maxTide:F2}ft";
            model.SubtitleColor = OxyColors.Yellow;
        }

        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendBackground = OxyColor.FromArgb(180, 30, 30, 30),
            LegendTextColor = OxyColors.White
        });

        return model;
    }

    /// <summary>
    /// Create statistics summary chart with key metrics
    /// </summary>
    private PlotModel CreateStatisticsSummaryChart()
    {
        var model = new PlotModel
        {
            Title = "Survey Statistics Summary",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            Background = OxyColor.FromRgb(30, 30, 30)
        };

        // Category axis for metrics
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Left,
            TextColor = OxyColors.White,
            TicklineColor = OxyColors.Gray
        };

        model.Axes.Add(categoryAxis);
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Value",
            TitleColor = OxyColors.White,
            TextColor = OxyColors.LightGray,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            Minimum = 0
        });

        // Calculate statistics
        var depths = _surveyPoints.Where(p => p.CalculatedZ.HasValue).Select(p => p.CalculatedZ!.Value).ToList();
        var dccValues = _surveyPoints.Where(p => p.Dcc.HasValue).Select(p => Math.Abs(p.Dcc!.Value)).ToList();
        var kpValues = _surveyPoints.Where(p => p.Kp.HasValue).Select(p => p.Kp!.Value).ToList();
        
        var series = new BarSeries
        {
            FillColor = OxyColors.Cyan,
            StrokeColor = OxyColors.DarkCyan,
            StrokeThickness = 1
        };

        // Add metrics
        var metrics = new List<(string Name, double Value, OxyColor Color)>();
        
        metrics.Add(("Total Points", _surveyPoints.Count, OxyColors.Lime));
        
        if (depths.Count > 0)
        {
            metrics.Add(("Min Depth (ft)", depths.Min(), OxyColors.Cyan));
            metrics.Add(("Max Depth (ft)", depths.Max(), OxyColors.Cyan));
            metrics.Add(("Avg Depth (ft)", depths.Average(), OxyColors.Cyan));
        }
        
        if (dccValues.Count > 0)
        {
            metrics.Add(("Avg |DCC| (ft)", dccValues.Average(), OxyColors.Orange));
            metrics.Add(("Max |DCC| (ft)", dccValues.Max(), OxyColors.Orange));
        }
        
        if (kpValues.Count > 0)
        {
            metrics.Add(("KP Range (km)", kpValues.Max() - kpValues.Min(), OxyColors.Yellow));
        }

        foreach (var (name, value, color) in metrics)
        {
            categoryAxis.Labels.Add(name);
            series.Items.Add(new BarItem(value) { Color = color });
        }

        model.Series.Add(series);

        return model;
    }

    /// <summary>
    /// Create point density chart (points per KP interval)
    /// </summary>
    private PlotModel CreatePointDensityChart()
    {
        var model = new PlotModel
        {
            Title = "Point Density (Points per 0.1 km)",
            TitleColor = OxyColors.White,
            PlotAreaBorderColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            Background = OxyColor.FromRgb(30, 30, 30)
        };

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "KP (km)",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Point Count",
            TitleColor = OxyColors.White,
            AxislineColor = OxyColors.Gray,
            TicklineColor = OxyColors.Gray,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            Minimum = 0
        });

        var pointsWithKp = _surveyPoints.Where(p => p.Kp.HasValue).OrderBy(p => p.Kp).ToList();
        
        if (pointsWithKp.Count == 0)
        {
            model.Subtitle = "No KP data available";
            model.SubtitleColor = OxyColors.Orange;
            return model;
        }

        // Group points by 0.1 km intervals
        double interval = 0.1;
        double minKp = Math.Floor(pointsWithKp.Min(p => p.Kp!.Value) / interval) * interval;
        double maxKp = Math.Ceiling(pointsWithKp.Max(p => p.Kp!.Value) / interval) * interval;

        var areaSeries = new AreaSeries
        {
            Title = "Point Density",
            Color = OxyColors.Cyan,
            Fill = OxyColor.FromAColor(100, OxyColors.Cyan),
            StrokeThickness = 2
        };

        for (double kp = minKp; kp <= maxKp; kp += interval)
        {
            int count = pointsWithKp.Count(p => p.Kp >= kp && p.Kp < kp + interval);
            areaSeries.Points.Add(new DataPoint(kp + interval / 2, count));
        }

        model.Series.Add(areaSeries);

        // Add average line
        double avgDensity = pointsWithKp.Count / ((maxKp - minKp) / interval);
        model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
        {
            Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
            Y = avgDensity,
            Color = OxyColors.Yellow,
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash,
            Text = $"Avg: {avgDensity:F1}",
            TextColor = OxyColors.Yellow,
            TextPosition = new DataPoint(minKp, avgDensity)
        });

        return model;
    }

    private void ExportImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png|SVG Image (*.svg)|*.svg",
            DefaultExt = ".png",
            FileName = "SurveyChart"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var plotView = SingleChart.Visibility == Visibility.Visible ? SingleChart : Chart1;
                
                if (dialog.FileName.EndsWith(".svg"))
                {
                    using var stream = File.Create(dialog.FileName);
                    var exporter = new OxyPlot.SvgExporter { Width = 1200, Height = 800 };
                    exporter.Export(plotView.Model, stream);
                }
                else
                {
                    using var stream = File.Create(dialog.FileName);
                    var exporter = new OxyPlot.Wpf.PngExporter { Width = 1200, Height = 800 };
                    exporter.Export(plotView.Model, stream);
                }

                MessageBox.Show($"Chart exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting chart: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var plotView = SingleChart.Visibility == Visibility.Visible ? SingleChart : Chart1;
            
            using var stream = new MemoryStream();
            var exporter = new OxyPlot.Wpf.PngExporter { Width = 1200, Height = 800 };
            exporter.Export(plotView.Model, stream);
            stream.Position = 0;
            
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            
            Clipboard.SetImage(bitmap);
            
            MessageBox.Show("Chart copied to clipboard.", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
