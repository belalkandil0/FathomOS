using System;
using System.Collections.Generic;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Advanced chart generation service for USBL verification analytics
/// </summary>
public class AdvancedChartService
{
    private readonly AdvancedStatisticsService _statsService = new();
    
    #region Time Series Charts
    
    /// <summary>
    /// Create position drift over time chart
    /// </summary>
    public PlotModel CreatePositionDriftChart(List<UsblObservation> observations, string title = "Position Drift Over Time")
    {
        var model = CreateBaseModel(title);
        var timeData = _statsService.GenerateTimeSeriesData(observations);
        
        if (timeData.Count == 0) return model;
        
        // Time axis
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            StringFormat = "HH:mm:ss",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // Value axis
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Offset from Mean (m)",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // Easting offset series
        var eastingSeries = new LineSeries
        {
            Title = "Easting Offset",
            Color = OxyColor.FromRgb(52, 152, 219),
            StrokeThickness = 2
        };
        
        // Northing offset series
        var northingSeries = new LineSeries
        {
            Title = "Northing Offset",
            Color = OxyColor.FromRgb(46, 204, 113),
            StrokeThickness = 2
        };
        
        // Radial offset series
        var radialSeries = new LineSeries
        {
            Title = "Radial Offset",
            Color = OxyColor.FromRgb(231, 76, 60),
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash
        };
        
        for (int i = 0; i < timeData.Count; i++)
        {
            var time = DateTimeAxis.ToDouble(timeData.Timestamps[i]);
            eastingSeries.Points.Add(new DataPoint(time, timeData.EastingOffsets[i]));
            northingSeries.Points.Add(new DataPoint(time, timeData.NorthingOffsets[i]));
            radialSeries.Points.Add(new DataPoint(time, timeData.RadialOffsets[i]));
        }
        
        model.Series.Add(eastingSeries);
        model.Series.Add(northingSeries);
        model.Series.Add(radialSeries);
        
        return model;
    }
    
    /// <summary>
    /// Create gyro stability chart
    /// </summary>
    public PlotModel CreateGyroStabilityChart(List<UsblObservation> observations, string title = "Gyro Stability")
    {
        var model = CreateBaseModel(title);
        var timeData = _statsService.GenerateTimeSeriesData(observations);
        
        if (timeData.Count == 0) return model;
        
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            StringFormat = "HH:mm:ss",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Heading (°)",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        var gyroSeries = new LineSeries
        {
            Title = "Vessel Gyro",
            Color = OxyColor.FromRgb(155, 89, 182),
            StrokeThickness = 2
        };
        
        for (int i = 0; i < timeData.Count; i++)
        {
            var time = DateTimeAxis.ToDouble(timeData.Timestamps[i]);
            gyroSeries.Points.Add(new DataPoint(time, timeData.GyroReadings[i]));
        }
        
        model.Series.Add(gyroSeries);
        
        // Add mean line
        double meanGyro = timeData.GyroReadings.Average();
        var meanLine = new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = meanGyro,
            Color = OxyColor.FromRgb(241, 196, 15),
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash,
            Text = $"Mean: {meanGyro:F1}°"
        };
        model.Annotations.Add(meanLine);
        
        return model;
    }
    
    /// <summary>
    /// Create depth variation chart
    /// </summary>
    public PlotModel CreateDepthVariationChart(List<UsblObservation> observations, string title = "Depth Variation")
    {
        var model = CreateBaseModel(title);
        var timeData = _statsService.GenerateTimeSeriesData(observations);
        
        if (timeData.Count == 0) return model;
        
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            StringFormat = "HH:mm:ss",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Depth (m)",
            StartPosition = 1,  // Invert axis for depth
            EndPosition = 0,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        var depthSeries = new LineSeries
        {
            Title = "Transponder Depth",
            Color = OxyColor.FromRgb(52, 73, 94),
            StrokeThickness = 2
        };
        
        for (int i = 0; i < timeData.Count; i++)
        {
            var time = DateTimeAxis.ToDouble(timeData.Timestamps[i]);
            depthSeries.Points.Add(new DataPoint(time, timeData.Depths[i]));
        }
        
        model.Series.Add(depthSeries);
        
        return model;
    }
    
    #endregion
    
    #region Polar Plot
    
    /// <summary>
    /// Create polar plot showing position offsets relative to heading
    /// </summary>
    public PlotModel CreatePolarPlot(List<UsblObservation> observations, double tolerance, string title = "Polar Position Plot")
    {
        var model = new PlotModel
        {
            Title = title,
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            PlotAreaBorderColor = OxyColors.Transparent
        };
        
        var polarData = _statsService.GeneratePolarPlotData(observations);
        if (polarData.Count == 0) return model;
        
        // Create magnitude axis (radial)
        var magnitudeAxis = new LinearAxis
        {
            Position = AxisPosition.None,
            Minimum = 0,
            Maximum = tolerance * 1.5,
            MajorStep = tolerance / 2,
            MinorStep = tolerance / 4,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(60, 128, 128, 128),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromArgb(30, 128, 128, 128)
        };
        model.Axes.Add(magnitudeAxis);
        
        // Create angle axis
        var angleAxis = new AngleAxis
        {
            Minimum = 0,
            Maximum = 360,
            MajorStep = 45,
            MinorStep = 15,
            StartAngle = 90,
            EndAngle = 450,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(60, 128, 128, 128),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromArgb(30, 128, 128, 128)
        };
        model.Axes.Add(angleAxis);
        
        // Add tolerance circle annotation
        var toleranceCircle = new EllipseAnnotation
        {
            X = 0,
            Y = 0,
            Width = tolerance * 2,
            Height = tolerance * 2,
            Fill = OxyColor.FromArgb(30, 46, 204, 113),
            Stroke = OxyColor.FromRgb(46, 204, 113),
            StrokeThickness = 2,
            Layer = AnnotationLayer.BelowSeries
        };
        model.Annotations.Add(toleranceCircle);
        
        // Group by heading quadrant and color
        var series = new ScatterSeries
        {
            Title = "Position Offsets",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColor.FromRgb(52, 152, 219)
        };
        
        foreach (var point in polarData)
        {
            // Convert to Cartesian for scatter plot
            series.Points.Add(new ScatterPoint(point.X, point.Y));
        }
        
        model.Series.Add(series);
        
        return model;
    }
    
    #endregion
    
    #region Histogram
    
    /// <summary>
    /// Create error distribution histogram
    /// </summary>
    public PlotModel CreateHistogramChart(List<UsblObservation> observations, int binCount = 20, string title = "Error Distribution")
    {
        var model = CreateBaseModel(title);
        var histData = _statsService.GenerateHistogram(observations, binCount);
        
        if (histData.BinCounts.Count == 0) return model;
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Radial Error (m)",
            MajorGridlineStyle = LineStyle.None
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Frequency",
            Minimum = 0,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        var barSeries = new RectangleBarSeries
        {
            Title = "Error Count",
            FillColor = OxyColor.FromRgb(52, 152, 219),
            StrokeColor = OxyColor.FromRgb(41, 128, 185),
            StrokeThickness = 1
        };
        
        for (int i = 0; i < histData.BinCount; i++)
        {
            barSeries.Items.Add(new RectangleBarItem(
                histData.BinEdges[i], 
                0, 
                histData.BinEdges[i + 1], 
                histData.BinCounts[i]));
        }
        
        model.Series.Add(barSeries);
        
        // Add normal distribution curve overlay
        int totalCount = histData.BinCounts.Sum();
        if (totalCount > 0 && histData.BinWidth > 0)
        {
            double mean = histData.BinCenters.Zip(histData.BinCounts, (c, n) => c * n).Sum() / totalCount;
            double variance = histData.BinCenters.Zip(histData.BinCounts, (c, n) => Math.Pow(c - mean, 2) * n).Sum() / totalCount;
            double stdDev = Math.Sqrt(variance);
            
            if (stdDev > 0.0001)
            {
                var normalSeries = new LineSeries
                {
                    Title = "Normal Fit",
                    Color = OxyColor.FromRgb(231, 76, 60),
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Dash
                };
                
                for (double x = histData.MinValue; x <= histData.MaxValue; x += histData.BinWidth / 10)
                {
                    double normalY = totalCount * histData.BinWidth * 
                        Math.Exp(-Math.Pow(x - mean, 2) / (2 * stdDev * stdDev)) / 
                        (stdDev * Math.Sqrt(2 * Math.PI));
                    normalSeries.Points.Add(new DataPoint(x, normalY));
                }
                
                model.Series.Add(normalSeries);
            }
        }
        
        return model;
    }
    
    #endregion
    
    #region Box Plot
    
    /// <summary>
    /// Create box plot for multiple datasets (comparison)
    /// </summary>
    public PlotModel CreateBoxPlotChart(List<(string Name, List<UsblObservation> Data)> datasets, string title = "Error Distribution Comparison")
    {
        var model = CreateBaseModel(title);
        
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Dataset"
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Radial Error (m)",
            Minimum = 0,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        var boxSeries = new BoxPlotSeries
        {
            Fill = OxyColor.FromRgb(52, 152, 219),
            Stroke = OxyColor.FromRgb(41, 128, 185),
            StrokeThickness = 1,
            WhiskerWidth = 0.5,
            BoxWidth = 0.4
        };
        
        int index = 0;
        foreach (var (name, data) in datasets)
        {
            categoryAxis.Labels.Add(name);
            var boxData = _statsService.GenerateBoxPlotData(data, name);
            
            var item = new BoxPlotItem(
                index,
                boxData.LowerWhisker,
                boxData.Q1,
                boxData.Median,
                boxData.Q3,
                boxData.UpperWhisker);
            
            // Add outliers
            foreach (var (_, value) in boxData.Outliers)
            {
                item.Outliers.Add(value);
            }
            
            boxSeries.Items.Add(item);
            index++;
        }
        
        model.Axes.Add(categoryAxis);
        model.Series.Add(boxSeries);
        
        return model;
    }
    
    /// <summary>
    /// Create single dataset box plot
    /// </summary>
    public PlotModel CreateSingleBoxPlot(List<UsblObservation> observations, string datasetName, string title = "Error Distribution")
    {
        return CreateBoxPlotChart(new List<(string, List<UsblObservation>)> { (datasetName, observations) }, title);
    }
    
    #endregion
    
    #region Confidence Ellipse Chart
    
    /// <summary>
    /// Create scatter plot with confidence ellipse overlay
    /// </summary>
    public PlotModel CreateConfidenceEllipseChart(
        List<UsblObservation> observations, 
        double confidenceLevel = 0.95,
        string title = "Position Scatter with Confidence Ellipse")
    {
        var model = CreateBaseModel(title);
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        
        if (validObs.Count < 3) return model;
        
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Easting Offset (m)",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Northing Offset (m)",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // Add scatter points (offset from mean)
        var scatterSeries = new ScatterSeries
        {
            Title = "Observations",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColor.FromRgb(52, 152, 219)
        };
        
        foreach (var obs in validObs)
        {
            scatterSeries.Points.Add(new ScatterPoint(
                obs.TransponderEasting - meanE,
                obs.TransponderNorthing - meanN));
        }
        
        model.Series.Add(scatterSeries);
        
        // Calculate and draw confidence ellipse
        var ellipse = _statsService.CalculateConfidenceEllipse(validObs, confidenceLevel);
        var ellipsePoints = _statsService.GenerateEllipsePoints(ellipse);
        
        var ellipseSeries = new LineSeries
        {
            Title = $"{confidenceLevel * 100:F0}% Confidence",
            Color = OxyColor.FromRgb(231, 76, 60),
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash
        };
        
        foreach (var (e, n) in ellipsePoints)
        {
            ellipseSeries.Points.Add(new DataPoint(e - meanE, n - meanN));
        }
        
        model.Series.Add(ellipseSeries);
        
        // Add center marker
        var centerSeries = new ScatterSeries
        {
            Title = "Center",
            MarkerType = MarkerType.Cross,
            MarkerSize = 8,
            MarkerStroke = OxyColor.FromRgb(231, 76, 60),
            MarkerStrokeThickness = 2
        };
        centerSeries.Points.Add(new ScatterPoint(0, 0));
        model.Series.Add(centerSeries);
        
        return model;
    }
    
    #endregion
    
    #region Trend Analysis Chart
    
    /// <summary>
    /// Create chart showing position trend with regression line
    /// </summary>
    public PlotModel CreateTrendChart(List<UsblObservation> observations, string title = "Position Trend Analysis")
    {
        var model = CreateBaseModel(title);
        var trendResult = _statsService.AnalyzeTrend(observations);
        
        if (!trendResult.HasSufficientData) return model;
        
        var validObs = observations.Where(o => !o.IsExcluded).OrderBy(o => o.DateTime).ToList();
        if (validObs.Count < 2) return model;
        
        var startTime = validObs.First().DateTime;
        
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time (minutes)",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Offset from Mean (m)",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // Easting data
        var eastingSeries = new ScatterSeries
        {
            Title = "Easting",
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = OxyColor.FromRgb(52, 152, 219)
        };
        
        // Northing data
        var northingSeries = new ScatterSeries
        {
            Title = "Northing",
            MarkerType = MarkerType.Square,
            MarkerSize = 3,
            MarkerFill = OxyColor.FromRgb(46, 204, 113)
        };
        
        foreach (var obs in validObs)
        {
            double minutes = (obs.DateTime - startTime).TotalMinutes;
            eastingSeries.Points.Add(new ScatterPoint(minutes, obs.TransponderEasting - meanE));
            northingSeries.Points.Add(new ScatterPoint(minutes, obs.TransponderNorthing - meanN));
        }
        
        model.Series.Add(eastingSeries);
        model.Series.Add(northingSeries);
        
        // Add trend lines
        double maxMinutes = (validObs.Last().DateTime - startTime).TotalMinutes;
        
        var eastingTrendLine = new LineSeries
        {
            Title = $"E Trend (R²={trendResult.EastingTrend.RSquared:F3})",
            Color = OxyColor.FromRgb(52, 152, 219),
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash
        };
        eastingTrendLine.Points.Add(new DataPoint(0, trendResult.EastingTrend.Intercept - meanE));
        eastingTrendLine.Points.Add(new DataPoint(maxMinutes, 
            trendResult.EastingTrend.Intercept + trendResult.EastingTrend.Slope * maxMinutes * 60 - meanE));
        model.Series.Add(eastingTrendLine);
        
        var northingTrendLine = new LineSeries
        {
            Title = $"N Trend (R²={trendResult.NorthingTrend.RSquared:F3})",
            Color = OxyColor.FromRgb(46, 204, 113),
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash
        };
        northingTrendLine.Points.Add(new DataPoint(0, trendResult.NorthingTrend.Intercept - meanN));
        northingTrendLine.Points.Add(new DataPoint(maxMinutes, 
            trendResult.NorthingTrend.Intercept + trendResult.NorthingTrend.Slope * maxMinutes * 60 - meanN));
        model.Series.Add(northingTrendLine);
        
        return model;
    }
    
    #endregion
    
    #region Error Ellipse Chart
    
    /// <summary>
    /// Create error ellipse chart showing confidence regions
    /// </summary>
    public PlotModel CreateErrorEllipseChart(List<UsblObservation> observations, 
        double confidenceLevel = 0.95, string title = "Position Error Ellipse")
    {
        var model = CreateBaseModel(title);
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        
        if (validObs.Count < 3) return model;
        
        // Calculate statistics
        var eastings = validObs.Select(o => o.TransponderEasting).ToArray();
        var northings = validObs.Select(o => o.TransponderNorthing).ToArray();
        
        double meanE = eastings.Average();
        double meanN = northings.Average();
        
        // Calculate covariance matrix
        double varE = eastings.Select(e => Math.Pow(e - meanE, 2)).Average();
        double varN = northings.Select(n => Math.Pow(n - meanN, 2)).Average();
        double covEN = eastings.Zip(northings, (e, n) => (e - meanE) * (n - meanN)).Average();
        
        // Eigenvalues and eigenvectors for ellipse
        double trace = varE + varN;
        double det = varE * varN - covEN * covEN;
        double discriminant = Math.Sqrt(Math.Max(0, trace * trace / 4 - det));
        
        double lambda1 = trace / 2 + discriminant;
        double lambda2 = trace / 2 - discriminant;
        
        // Rotation angle
        double angle = Math.Atan2(2 * covEN, varE - varN) / 2;
        
        // Chi-square value for confidence level (approximation for 2 DOF)
        double chiSquare = confidenceLevel == 0.95 ? 5.991 : 
                          confidenceLevel == 0.99 ? 9.210 : 
                          confidenceLevel == 0.90 ? 4.605 : 5.991;
        
        // Semi-axes
        double a = Math.Sqrt(chiSquare * lambda1);
        double b = Math.Sqrt(chiSquare * lambda2);
        
        // Axes
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Easting Offset (m)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromArgb(20, 128, 128, 128)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Northing Offset (m)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromArgb(20, 128, 128, 128)
        });
        
        // Draw scatter points (offset from mean)
        var scatterSeries = new ScatterSeries
        {
            Title = "Observations",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColor.FromArgb(150, 52, 152, 219),
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var obs in validObs)
        {
            scatterSeries.Points.Add(new ScatterPoint(obs.TransponderEasting - meanE, obs.TransponderNorthing - meanN));
        }
        model.Series.Add(scatterSeries);
        
        // Draw error ellipses at different confidence levels
        var confidenceLevels = new[] { 0.50, 0.90, 0.95, 0.99 };
        var colors = new[] { 
            OxyColor.FromArgb(60, 46, 204, 113),
            OxyColor.FromArgb(60, 241, 196, 15),
            OxyColor.FromArgb(60, 230, 126, 34),
            OxyColor.FromArgb(60, 231, 76, 60)
        };
        var labels = new[] { "50%", "90%", "95%", "99%" };
        
        for (int ci = 0; ci < confidenceLevels.Length; ci++)
        {
            double chi2 = confidenceLevels[ci] == 0.50 ? 1.386 :
                         confidenceLevels[ci] == 0.90 ? 4.605 :
                         confidenceLevels[ci] == 0.95 ? 5.991 :
                         confidenceLevels[ci] == 0.99 ? 9.210 : 5.991;
            
            double semiA = Math.Sqrt(chi2 * lambda1);
            double semiB = Math.Sqrt(chi2 * lambda2);
            
            var ellipseSeries = new LineSeries
            {
                Title = $"{labels[ci]} Confidence",
                Color = colors[ci],
                StrokeThickness = ci == 2 ? 2.5 : 1.5, // Highlight 95%
                LineStyle = ci == 2 ? LineStyle.Solid : LineStyle.Dash
            };
            
            // Generate ellipse points
            for (int i = 0; i <= 100; i++)
            {
                double t = 2 * Math.PI * i / 100;
                double x = semiA * Math.Cos(t);
                double y = semiB * Math.Sin(t);
                
                // Rotate
                double xRot = x * Math.Cos(angle) - y * Math.Sin(angle);
                double yRot = x * Math.Sin(angle) + y * Math.Cos(angle);
                
                ellipseSeries.Points.Add(new DataPoint(xRot, yRot));
            }
            
            model.Series.Add(ellipseSeries);
        }
        
        // Add center point
        var centerSeries = new ScatterSeries
        {
            Title = "Mean Position",
            MarkerType = MarkerType.Cross,
            MarkerSize = 10,
            MarkerStroke = OxyColors.Black,
            MarkerStrokeThickness = 2
        };
        centerSeries.Points.Add(new ScatterPoint(0, 0));
        model.Series.Add(centerSeries);
        
        // Add major/minor axis lines
        var majorAxis = new LineSeries
        {
            Title = $"Major Axis ({a:F3}m)",
            Color = OxyColors.DarkRed,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.DashDot
        };
        double majorX = a * Math.Cos(angle);
        double majorY = a * Math.Sin(angle);
        majorAxis.Points.Add(new DataPoint(-majorX, -majorY));
        majorAxis.Points.Add(new DataPoint(majorX, majorY));
        model.Series.Add(majorAxis);
        
        var minorAxis = new LineSeries
        {
            Title = $"Minor Axis ({b:F3}m)",
            Color = OxyColors.DarkBlue,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.DashDot
        };
        double minorX = b * Math.Cos(angle + Math.PI / 2);
        double minorY = b * Math.Sin(angle + Math.PI / 2);
        minorAxis.Points.Add(new DataPoint(-minorX, -minorY));
        minorAxis.Points.Add(new DataPoint(minorX, minorY));
        model.Series.Add(minorAxis);
        
        // Calculate max extent for annotation positioning
        double maxExtent = Math.Max(a, b) * 1.5;
        
        // Add statistics annotation
        var annotation = new TextAnnotation
        {
            Text = $"n={validObs.Count}\nσE={Math.Sqrt(varE):F3}m\nσN={Math.Sqrt(varN):F3}m\nAngle={angle * 180 / Math.PI:F1}°",
            TextPosition = new DataPoint(maxExtent * 0.5, maxExtent * 0.7),
            FontSize = 10,
            TextColor = OxyColors.DarkGray,
            Stroke = OxyColors.Transparent
        };
        model.Annotations.Add(annotation);
        
        return model;
    }
    
    #endregion
    
    #region Heading Comparison Radar Chart
    
    /// <summary>
    /// Create radar/spider chart comparing metrics across headings
    /// </summary>
    public PlotModel CreateHeadingComparisonRadar(UsblVerificationProject project, string title = "Heading Comparison Radar")
    {
        var model = new PlotModel
        {
            Title = title,
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            PlotType = PlotType.Polar,
            PlotAreaBorderThickness = new OxyThickness(0)
        };
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.RightTop,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside,
            LegendBackground = OxyColor.FromArgb(200, 255, 255, 255)
        });
        
        // Polar axes
        model.Axes.Add(new AngleAxis
        {
            Minimum = 0,
            Maximum = 360,
            MajorStep = 90,
            MinorStep = 45,
            StartAngle = 90,
            EndAngle = 450,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(60, 128, 128, 128),
            MinorGridlineColor = OxyColor.FromArgb(30, 128, 128, 128)
        });
        
        model.Axes.Add(new MagnitudeAxis
        {
            Minimum = 0,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(60, 128, 128, 128)
        });
        
        // Collect data for each heading
        var headingData = new List<(double Heading, double StdDev, double MaxOffset, double PointCount, OxyColor Color)>();
        var colors = new[] {
            OxyColor.FromRgb(52, 152, 219),
            OxyColor.FromRgb(46, 204, 113),
            OxyColor.FromRgb(155, 89, 182),
            OxyColor.FromRgb(241, 196, 15)
        };
        
        var spinTests = new[] { project.Spin000, project.Spin090, project.Spin180, project.Spin270 };
        
        double maxStdDev = 0.001;
        int colorIdx = 0;
        
        foreach (var spin in spinTests)
        {
            if (!spin.HasData) continue;
            
            var stats = spin.Statistics;
            var validObs = spin.Observations.Where(o => !o.IsExcluded).ToList();
            
            // Use 1-sigma values (2-sigma / 2)
            double stdE = stats.StdDevEasting2Sigma / 2;
            double stdN = stats.StdDevNorthing2Sigma / 2;
            maxStdDev = Math.Max(maxStdDev, stdE);
            maxStdDev = Math.Max(maxStdDev, stdN);
            
            // Calculate max radial offset from observations
            double maxRadialOffset = 0;
            if (validObs.Count > 0)
            {
                double meanE = validObs.Average(o => o.TransponderEasting);
                double meanN = validObs.Average(o => o.TransponderNorthing);
                maxRadialOffset = validObs.Max(o => 
                    Math.Sqrt(Math.Pow(o.TransponderEasting - meanE, 2) + 
                             Math.Pow(o.TransponderNorthing - meanN, 2)));
            }
            
            headingData.Add((
                spin.ActualHeading,
                Math.Sqrt(stdE * stdE + stdN * stdN),
                maxRadialOffset,
                validObs.Count,
                colors[colorIdx++ % colors.Length]
            ));
        }
        
        if (headingData.Count == 0) return model;
        
        // Normalize to max value
        double maxVal = headingData.Max(h => Math.Max(h.StdDev, h.MaxOffset));
        ((MagnitudeAxis)model.Axes[1]).Maximum = maxVal * 1.2;
        
        // Create series for each metric
        var stdDevSeries = new LineSeries
        {
            Title = "Std Dev (Radial)",
            Color = OxyColor.FromRgb(231, 76, 60),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 6,
            MarkerFill = OxyColor.FromRgb(231, 76, 60)
        };
        
        var maxOffsetSeries = new LineSeries
        {
            Title = "Max Offset",
            Color = OxyColor.FromRgb(52, 152, 219),
            StrokeThickness = 2,
            MarkerType = MarkerType.Square,
            MarkerSize = 6,
            MarkerFill = OxyColor.FromRgb(52, 152, 219)
        };
        
        // Add points and close the polygon - use degrees for polar axis
        foreach (var data in headingData.OrderBy(h => h.Heading))
        {
            stdDevSeries.Points.Add(new DataPoint(data.Heading, data.StdDev));
            maxOffsetSeries.Points.Add(new DataPoint(data.Heading, data.MaxOffset));
        }
        
        // Close polygons
        if (headingData.Count > 0)
        {
            var first = headingData.OrderBy(h => h.Heading).First();
            stdDevSeries.Points.Add(new DataPoint(first.Heading, first.StdDev));
            maxOffsetSeries.Points.Add(new DataPoint(first.Heading, first.MaxOffset));
        }
        
        model.Series.Add(stdDevSeries);
        model.Series.Add(maxOffsetSeries);
        
        // Add heading markers
        foreach (var data in headingData)
        {
            var marker = new ScatterSeries
            {
                MarkerType = MarkerType.Diamond,
                MarkerSize = 10,
                MarkerFill = data.Color,
                MarkerStroke = OxyColors.White,
                MarkerStrokeThickness = 2
            };
            marker.Points.Add(new ScatterPoint(data.Heading, maxVal * 1.1));
            model.Series.Add(marker);
        }
        
        return model;
    }
    
    #endregion
    
    #region Residual Time Series Chart
    
    /// <summary>
    /// Create residual time series chart for quality control
    /// </summary>
    public PlotModel CreateResidualTimeSeriesChart(List<UsblObservation> observations, 
        string title = "Residual Time Series")
    {
        var model = CreateBaseModel(title);
        var validObs = observations.Where(o => !o.IsExcluded).OrderBy(o => o.DateTime).ToList();
        
        if (validObs.Count < 2) return model;
        
        // Calculate mean position
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        // Time axis
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            StringFormat = "HH:mm:ss",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // Residual axis
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Residual (m)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // Calculate residuals
        var residuals = validObs.Select(o => 
            Math.Sqrt(Math.Pow(o.TransponderEasting - meanE, 2) + Math.Pow(o.TransponderNorthing - meanN, 2))).ToList();
        
        double meanResidual = residuals.Average();
        double stdResidual = Math.Sqrt(residuals.Select(r => Math.Pow(r - meanResidual, 2)).Average());
        
        // Residual series
        var residualSeries = new LineSeries
        {
            Title = "Radial Residual",
            Color = OxyColor.FromRgb(52, 152, 219),
            StrokeThickness = 1.5,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = OxyColor.FromRgb(52, 152, 219)
        };
        
        for (int i = 0; i < validObs.Count; i++)
        {
            var time = DateTimeAxis.ToDouble(validObs[i].DateTime);
            residualSeries.Points.Add(new DataPoint(time, residuals[i]));
        }
        model.Series.Add(residualSeries);
        
        // Mean line
        var meanLine = new LineSeries
        {
            Title = $"Mean ({meanResidual:F3}m)",
            Color = OxyColors.DarkGreen,
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash
        };
        var startTime = DateTimeAxis.ToDouble(validObs.First().DateTime);
        var endTime = DateTimeAxis.ToDouble(validObs.Last().DateTime);
        meanLine.Points.Add(new DataPoint(startTime, meanResidual));
        meanLine.Points.Add(new DataPoint(endTime, meanResidual));
        model.Series.Add(meanLine);
        
        // +1σ and -1σ lines
        var plus1Sigma = new LineSeries
        {
            Title = $"+1σ ({meanResidual + stdResidual:F3}m)",
            Color = OxyColors.Orange,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.Dot
        };
        plus1Sigma.Points.Add(new DataPoint(startTime, meanResidual + stdResidual));
        plus1Sigma.Points.Add(new DataPoint(endTime, meanResidual + stdResidual));
        model.Series.Add(plus1Sigma);
        
        // +2σ line
        var plus2Sigma = new LineSeries
        {
            Title = $"+2σ ({meanResidual + 2 * stdResidual:F3}m)",
            Color = OxyColors.Red,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.DashDot
        };
        plus2Sigma.Points.Add(new DataPoint(startTime, meanResidual + 2 * stdResidual));
        plus2Sigma.Points.Add(new DataPoint(endTime, meanResidual + 2 * stdResidual));
        model.Series.Add(plus2Sigma);
        
        // Highlight outliers (>2σ)
        var outlierSeries = new ScatterSeries
        {
            Title = "Outliers (>2σ)",
            MarkerType = MarkerType.Triangle,
            MarkerSize = 8,
            MarkerFill = OxyColors.Red,
            MarkerStroke = OxyColors.DarkRed,
            MarkerStrokeThickness = 1
        };
        
        for (int i = 0; i < validObs.Count; i++)
        {
            if (residuals[i] > meanResidual + 2 * stdResidual)
            {
                var time = DateTimeAxis.ToDouble(validObs[i].DateTime);
                outlierSeries.Points.Add(new ScatterPoint(time, residuals[i]));
            }
        }
        model.Series.Add(outlierSeries);
        
        return model;
    }
    
    #endregion
    
    #region Statistical Control Chart (X-bar/R)
    
    /// <summary>
    /// Create X-bar and R control chart for process monitoring
    /// </summary>
    public PlotModel CreateControlChart(List<UsblObservation> observations, int subgroupSize = 5,
        string title = "Statistical Control Chart")
    {
        var model = CreateBaseModel(title);
        var validObs = observations.Where(o => !o.IsExcluded).OrderBy(o => o.DateTime).ToList();
        
        if (validObs.Count < subgroupSize * 3) return model;
        
        // Calculate subgroup statistics
        var subgroups = new List<(int Index, double Mean, double Range, DateTime Time)>();
        
        for (int i = 0; i <= validObs.Count - subgroupSize; i += subgroupSize)
        {
            var group = validObs.Skip(i).Take(subgroupSize).ToList();
            var radials = group.Select(o => 
                Math.Sqrt(Math.Pow(o.TransponderEasting, 2) + Math.Pow(o.TransponderNorthing, 2))).ToList();
            
            subgroups.Add((
                subgroups.Count + 1,
                radials.Average(),
                radials.Max() - radials.Min(),
                group.First().DateTime
            ));
        }
        
        if (subgroups.Count < 3) return model;
        
        // Calculate control limits
        double xBar = subgroups.Average(s => s.Mean);
        double rBar = subgroups.Average(s => s.Range);
        
        // A2 and D3, D4 factors for subgroup size 5
        double A2 = subgroupSize switch { 2 => 1.880, 3 => 1.023, 4 => 0.729, 5 => 0.577, _ => 0.577 };
        double D3 = subgroupSize switch { 2 => 0, 3 => 0, 4 => 0, 5 => 0, _ => 0 };
        double D4 = subgroupSize switch { 2 => 3.267, 3 => 2.575, 4 => 2.282, 5 => 2.115, _ => 2.115 };
        
        double UCL_X = xBar + A2 * rBar;
        double LCL_X = xBar - A2 * rBar;
        double UCL_R = D4 * rBar;
        double LCL_R = D3 * rBar;
        
        // Create two y-axes for X-bar and R charts
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Subgroup",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "X-bar (Mean)",
            Key = "XBar",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // X-bar series
        var xBarSeries = new LineSeries
        {
            Title = "X-bar",
            Color = OxyColor.FromRgb(52, 152, 219),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 5,
            MarkerFill = OxyColor.FromRgb(52, 152, 219),
            YAxisKey = "XBar"
        };
        
        foreach (var sg in subgroups)
        {
            xBarSeries.Points.Add(new DataPoint(sg.Index, sg.Mean));
        }
        model.Series.Add(xBarSeries);
        
        // Control limits
        var centerLine = new LineSeries
        {
            Title = $"CL ({xBar:F3})",
            Color = OxyColors.DarkGreen,
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash,
            YAxisKey = "XBar"
        };
        centerLine.Points.Add(new DataPoint(1, xBar));
        centerLine.Points.Add(new DataPoint(subgroups.Count, xBar));
        model.Series.Add(centerLine);
        
        var uclLine = new LineSeries
        {
            Title = $"UCL ({UCL_X:F3})",
            Color = OxyColors.Red,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.DashDot,
            YAxisKey = "XBar"
        };
        uclLine.Points.Add(new DataPoint(1, UCL_X));
        uclLine.Points.Add(new DataPoint(subgroups.Count, UCL_X));
        model.Series.Add(uclLine);
        
        var lclLine = new LineSeries
        {
            Title = $"LCL ({LCL_X:F3})",
            Color = OxyColors.Red,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.DashDot,
            YAxisKey = "XBar"
        };
        lclLine.Points.Add(new DataPoint(1, LCL_X));
        lclLine.Points.Add(new DataPoint(subgroups.Count, LCL_X));
        model.Series.Add(lclLine);
        
        // Highlight out-of-control points
        var oocSeries = new ScatterSeries
        {
            Title = "Out of Control",
            MarkerType = MarkerType.Triangle,
            MarkerSize = 10,
            MarkerFill = OxyColors.Red,
            MarkerStroke = OxyColors.DarkRed,
            MarkerStrokeThickness = 2
        };
        
        foreach (var sg in subgroups)
        {
            if (sg.Mean > UCL_X || sg.Mean < LCL_X)
            {
                oocSeries.Points.Add(new ScatterPoint(sg.Index, sg.Mean));
            }
        }
        model.Series.Add(oocSeries);
        
        return model;
    }
    
    #endregion
    
    #region Depth vs Slant Range Scatter
    
    /// <summary>
    /// Create depth vs slant range scatter plot
    /// </summary>
    public PlotModel CreateDepthSlantRangeChart(List<UsblObservation> observations,
        string title = "Depth vs Slant Range")
    {
        var model = CreateBaseModel(title);
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        
        if (validObs.Count == 0) return model;
        
        // Axes
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Slant Range (m)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Depth (m)",
            StartPosition = 1,
            EndPosition = 0, // Invert for depth
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // Calculate slant range for each observation
        var dataPoints = validObs.Select(o => new
        {
            SlantRange = Math.Sqrt(
                Math.Pow(o.TransponderEasting, 2) + 
                Math.Pow(o.TransponderNorthing, 2) + 
                Math.Pow(o.TransponderDepth, 2)),
            Depth = o.TransponderDepth
        }).ToList();
        
        // Scatter series
        var scatterSeries = new ScatterSeries
        {
            Title = "Observations",
            MarkerType = MarkerType.Circle,
            MarkerSize = 5,
            MarkerFill = OxyColor.FromRgb(52, 152, 219),
            MarkerStroke = OxyColors.White,
            MarkerStrokeThickness = 1
        };
        
        foreach (var dp in dataPoints)
        {
            scatterSeries.Points.Add(new ScatterPoint(dp.SlantRange, dp.Depth));
        }
        model.Series.Add(scatterSeries);
        
        // Theoretical relationship line (depth = slant range for vertical)
        if (dataPoints.Count > 0)
        {
            double minSlant = dataPoints.Min(d => d.SlantRange);
            double maxSlant = dataPoints.Max(d => d.SlantRange);
            
            var theoreticalLine = new LineSeries
            {
                Title = "Theoretical (Vertical)",
                Color = OxyColors.Gray,
                StrokeThickness = 1.5,
                LineStyle = LineStyle.Dash
            };
            theoreticalLine.Points.Add(new DataPoint(minSlant, minSlant));
            theoreticalLine.Points.Add(new DataPoint(maxSlant, maxSlant));
            model.Series.Add(theoreticalLine);
            
            // Linear regression
            var slants = dataPoints.Select(d => d.SlantRange).ToArray();
            var depths = dataPoints.Select(d => d.Depth).ToArray();
            
            double meanX = slants.Average();
            double meanY = depths.Average();
            double sxx = slants.Select(x => Math.Pow(x - meanX, 2)).Sum();
            
            // Guard against division by zero
            if (sxx > 0.0001)
            {
                double slope = slants.Zip(depths, (x, y) => (x - meanX) * (y - meanY)).Sum() / sxx;
                double intercept = meanY - slope * meanX;
                
                // R-squared
                double ssRes = slants.Zip(depths, (x, y) => Math.Pow(y - (slope * x + intercept), 2)).Sum();
                double ssTot = depths.Select(y => Math.Pow(y - meanY, 2)).Sum();
                double rSquared = ssTot > 0.0001 ? 1 - ssRes / ssTot : 0;
                
                var regressionLine = new LineSeries
                {
                    Title = $"Regression (R²={rSquared:F3})",
                    Color = OxyColor.FromRgb(231, 76, 60),
                    StrokeThickness = 2
                };
                regressionLine.Points.Add(new DataPoint(minSlant, slope * minSlant + intercept));
                regressionLine.Points.Add(new DataPoint(maxSlant, slope * maxSlant + intercept));
                model.Series.Add(regressionLine);
            }
        }
        
        return model;
    }
    
    #endregion
    
    #region Rose Diagram / Directional Error Plot
    
    /// <summary>
    /// Create rose diagram showing directional distribution of errors
    /// </summary>
    public PlotModel CreateRoseDiagram(List<UsblObservation> observations, int binCount = 36,
        string title = "Directional Error Distribution")
    {
        var model = new PlotModel
        {
            Title = title,
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            PlotType = PlotType.Polar,
            PlotAreaBorderThickness = new OxyThickness(0)
        };
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.RightTop,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside
        });
        
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        
        if (validObs.Count < 3) return model;
        
        // Calculate mean position
        double meanE = validObs.Average(o => o.TransponderEasting);
        double meanN = validObs.Average(o => o.TransponderNorthing);
        
        // Calculate bearing and distance from mean for each point
        var errorVectors = validObs.Select(o =>
        {
            double dE = o.TransponderEasting - meanE;
            double dN = o.TransponderNorthing - meanN;
            double bearing = Math.Atan2(dE, dN) * 180 / Math.PI;
            if (bearing < 0) bearing += 360;
            double distance = Math.Sqrt(dE * dE + dN * dN);
            return (Bearing: bearing, Distance: distance);
        }).ToList();
        
        // Create histogram bins
        double binWidth = 360.0 / binCount;
        var bins = new double[binCount];
        var binDistances = new List<double>[binCount];
        
        for (int i = 0; i < binCount; i++)
            binDistances[i] = new List<double>();
        
        foreach (var ev in errorVectors)
        {
            int binIndex = (int)(ev.Bearing / binWidth) % binCount;
            bins[binIndex]++;
            binDistances[binIndex].Add(ev.Distance);
        }
        
        // Polar axes
        model.Axes.Add(new AngleAxis
        {
            Minimum = 0,
            Maximum = 360,
            MajorStep = 45,
            MinorStep = 15,
            StartAngle = 90,
            EndAngle = 450,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromArgb(60, 128, 128, 128),
            MinorGridlineColor = OxyColor.FromArgb(30, 128, 128, 128),
            FormatAsFractions = false,
            FractionUnit = Math.PI,
            FractionUnitSymbol = "π"
        });
        
        double maxCount = bins.Max();
        model.Axes.Add(new MagnitudeAxis
        {
            Minimum = 0,
            Maximum = maxCount * 1.1,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(60, 128, 128, 128),
            Title = "Count"
        });
        
        // Create rose petals using line series (more compatible with polar plots)
        for (int i = 0; i < binCount; i++)
        {
            double startAngle = i * binWidth;
            double endAngle = (i + 1) * binWidth;
            double count = bins[i];
            
            if (count == 0) continue;
            
            // Color based on average distance in bin
            double avgDistance = binDistances[i].Count > 0 ? binDistances[i].Average() : 0;
            double maxDistance = errorVectors.Max(e => e.Distance);
            byte colorIntensity = maxDistance > 0 ? (byte)(255 * avgDistance / maxDistance) : (byte)128;
            var color = OxyColor.FromRgb((byte)(52 + colorIntensity * 0.7), (byte)(152 - colorIntensity * 0.3), (byte)(219 - colorIntensity * 0.5));
            
            // Use LineSeries for polar plot - draw filled polygon outline
            // Note: OxyPlot polar plots use degrees for angle axis
            var petalSeries = new LineSeries
            {
                Color = color,
                StrokeThickness = 2,
                LineStyle = LineStyle.Solid
            };
            
            // Draw petal outline (wedge shape) - angles in degrees
            petalSeries.Points.Add(new DataPoint(startAngle, 0));
            petalSeries.Points.Add(new DataPoint(startAngle, count));
            
            // Arc at the top
            for (double a = startAngle; a <= endAngle; a += 2)
            {
                petalSeries.Points.Add(new DataPoint(a, count));
            }
            
            petalSeries.Points.Add(new DataPoint(endAngle, count));
            petalSeries.Points.Add(new DataPoint(endAngle, 0));
            petalSeries.Points.Add(new DataPoint(startAngle, 0)); // Close the shape
            
            model.Series.Add(petalSeries);
        }
        
        // Add cardinal direction labels as annotations
        var directions = new[] { ("N", 0), ("E", 90), ("S", 180), ("W", 270) };
        foreach (var (label, angle) in directions)
        {
            model.Annotations.Add(new TextAnnotation
            {
                Text = label,
                TextPosition = new DataPoint(angle, maxCount * 1.2),
                FontSize = 12,
                FontWeight = OxyPlot.FontWeights.Bold,
                TextColor = OxyColors.DarkGray,
                Stroke = OxyColors.Transparent
            });
        }
        
        return model;
    }
    
    #endregion
    
    #region Before/After Smoothing Overlay
    
    /// <summary>
    /// Create before/after smoothing comparison chart
    /// </summary>
    public PlotModel CreateSmoothingComparisonChart(List<UsblObservation> observations,
        string title = "Before/After Smoothing Comparison")
    {
        var model = CreateBaseModel(title);
        var validObs = observations.Where(o => !o.IsExcluded).ToList();
        
        if (validObs.Count == 0) return model;
        
        // Check if smoothing has been applied
        bool hasSmoothing = validObs.Any(o => o.SmoothedEasting.HasValue);
        
        // Axes
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Easting (m)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Northing (m)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromArgb(40, 128, 128, 128)
        });
        
        // Calculate center for relative positioning
        double centerE = validObs.Average(o => o.TransponderEasting);
        double centerN = validObs.Average(o => o.TransponderNorthing);
        
        // Original data series
        var originalSeries = new ScatterSeries
        {
            Title = "Original",
            MarkerType = MarkerType.Circle,
            MarkerSize = 5,
            MarkerFill = OxyColor.FromArgb(150, 231, 76, 60),
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var obs in validObs)
        {
            originalSeries.Points.Add(new ScatterPoint(
                obs.TransponderEasting - centerE, 
                obs.TransponderNorthing - centerN));
        }
        model.Series.Add(originalSeries);
        
        if (hasSmoothing)
        {
            // Smoothed data series
            var smoothedSeries = new ScatterSeries
            {
                Title = "Smoothed",
                MarkerType = MarkerType.Diamond,
                MarkerSize = 6,
                MarkerFill = OxyColor.FromRgb(46, 204, 113),
                MarkerStroke = OxyColors.White,
                MarkerStrokeThickness = 1
            };
            
            // Connection lines showing movement
            var connectionSeries = new LineSeries
            {
                Title = "Movement",
                Color = OxyColor.FromArgb(80, 128, 128, 128),
                StrokeThickness = 0.5,
                LineStyle = LineStyle.Solid
            };
            
            foreach (var obs in validObs.Where(o => o.SmoothedEasting.HasValue))
            {
                double origE = obs.TransponderEasting - centerE;
                double origN = obs.TransponderNorthing - centerN;
                double smoothE = obs.SmoothedEasting!.Value - centerE;
                double smoothN = obs.SmoothedNorthing!.Value - centerN;
                
                smoothedSeries.Points.Add(new ScatterPoint(smoothE, smoothN));
                
                // Draw connection line
                connectionSeries.Points.Add(new DataPoint(origE, origN));
                connectionSeries.Points.Add(new DataPoint(smoothE, smoothN));
                connectionSeries.Points.Add(new DataPoint(double.NaN, double.NaN)); // Break
            }
            
            model.Series.Add(connectionSeries);
            model.Series.Add(smoothedSeries);
            
            // Statistics comparison
            double origStdE = Math.Sqrt(validObs.Select(o => Math.Pow(o.TransponderEasting - centerE, 2)).Average());
            double origStdN = Math.Sqrt(validObs.Select(o => Math.Pow(o.TransponderNorthing - centerN, 2)).Average());
            double origRadialStd = Math.Sqrt(origStdE * origStdE + origStdN * origStdN);
            
            var smoothedObs = validObs.Where(o => o.SmoothedEasting.HasValue).ToList();
            if (smoothedObs.Count > 0 && origRadialStd > 0.0001)
            {
                double smoothCenterE = smoothedObs.Average(o => o.SmoothedEasting!.Value);
                double smoothCenterN = smoothedObs.Average(o => o.SmoothedNorthing!.Value);
                double smoothStdE = Math.Sqrt(smoothedObs.Select(o => Math.Pow(o.SmoothedEasting!.Value - smoothCenterE, 2)).Average());
                double smoothStdN = Math.Sqrt(smoothedObs.Select(o => Math.Pow(o.SmoothedNorthing!.Value - smoothCenterN, 2)).Average());
                double smoothRadialStd = Math.Sqrt(smoothStdE * smoothStdE + smoothStdN * smoothStdN);
                
                double reduction = (1 - smoothRadialStd / origRadialStd) * 100;
                
                model.Subtitle = $"Scatter Reduction: {reduction:F1}%";
                model.SubtitleFontSize = 11;
            }
            else
            {
                model.Subtitle = "Smoothing applied but insufficient data for statistics";
                model.SubtitleFontSize = 11;
            }
        }
        else
        {
            model.Subtitle = "No smoothing applied - showing original data only";
            model.SubtitleFontSize = 11;
        }
        
        // Add center crosshair
        var centerSeries = new ScatterSeries
        {
            Title = "Center",
            MarkerType = MarkerType.Cross,
            MarkerSize = 12,
            MarkerStroke = OxyColors.Black,
            MarkerStrokeThickness = 2
        };
        centerSeries.Points.Add(new ScatterPoint(0, 0));
        model.Series.Add(centerSeries);
        
        return model;
    }
    
    #endregion
    
    #region Helper Methods
    
    private PlotModel CreateBaseModel(string title)
    {
        var model = new PlotModel
        {
            Title = title,
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            PlotAreaBorderThickness = new OxyThickness(1),
            PlotAreaBorderColor = OxyColor.FromArgb(60, 128, 128, 128)
        };
        
        // Add legend using the new API
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(200, 255, 255, 255),
            LegendBorder = OxyColor.FromArgb(60, 128, 128, 128)
        });
        
        return model;
    }
    
    #endregion
}
