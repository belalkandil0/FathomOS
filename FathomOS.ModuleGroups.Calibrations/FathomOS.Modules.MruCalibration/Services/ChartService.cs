using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using FathomOS.Modules.MruCalibration.Models;

namespace FathomOS.Modules.MruCalibration.Services;

/// <summary>
/// Service for creating OxyPlot charts for calibration analysis
/// Enhanced with color customization and additional chart types
/// </summary>
public class ChartService
{
    #region Default Colors
    
    // Subsea7 brand colors
    private static readonly OxyColor Subsea7Orange = OxyColor.FromRgb(232, 93, 0);
    private static readonly OxyColor Subsea7Teal = OxyColor.FromRgb(0, 150, 136);
    private static readonly OxyColor ChartBackground = OxyColor.FromRgb(30, 30, 35);
    private static readonly OxyColor ChartForeground = OxyColor.FromRgb(200, 200, 200);
    private static readonly OxyColor GridLines = OxyColor.FromRgb(60, 60, 65);
    
    #endregion
    
    #region Color Settings
    
    /// <summary>
    /// Current chart color settings - can be customized by user
    /// </summary>
    public ChartColorSettings ColorSettings { get; set; } = new();
    
    /// <summary>
    /// Apply color settings from a ChartColorSettings object
    /// </summary>
    public void ApplyColorSettings(ChartColorSettings settings)
    {
        ColorSettings = settings ?? new ChartColorSettings();
    }
    
    #endregion
    
    #region Main Calibration Chart
    
    /// <summary>
    /// Create the main calibration time series chart
    /// Shows Reference, Verified, and C-O values over time
    /// </summary>
    public PlotModel CreateCalibrationChart(SensorCalibrationData data, string title)
    {
        var model = CreateBaseModel(title);
        
        if (data.DataPoints.Count == 0)
            return model;
        
        // Time axis (X)
        var timeAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Key = "Time",
            Title = "Time",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = GridLines,
            MinorGridlineStyle = LineStyle.Dot,
            StringFormat = "HH:mm:ss",
            IntervalType = DateTimeIntervalType.Auto
        };
        model.Axes.Add(timeAxis);
        
        // Primary Y axis (Reference/Verified values)
        var primaryAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Value (°)",
            Key = "Primary",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid
        };
        model.Axes.Add(primaryAxis);
        
        // Secondary Y axis (C-O values) - with FIXED range based on data
        double coMin = data.DataPoints.Min(p => p.CO);
        double coMax = data.DataPoints.Max(p => p.CO);
        double coRange = coMax - coMin;
        double coMargin = Math.Max(coRange * 0.2, 0.01); // 20% margin or minimum 0.01
        
        var secondaryAxis = new LinearAxis
        {
            Position = AxisPosition.Right,
            Title = "C-O (°)",
            Key = "Secondary",
            TitleColor = ColorSettings.COLineColor,
            TextColor = ColorSettings.COLineColor,
            TicklineColor = ColorSettings.COLineColor,
            Minimum = coMin - coMargin,  // FIXED range to prevent jumping
            Maximum = coMax + coMargin,
            IsPanEnabled = false,  // Disable panning on secondary axis
            IsZoomEnabled = false  // Disable zooming on secondary axis
        };
        model.Axes.Add(secondaryAxis);
        
        // Reference series
        var refSeries = new LineSeries
        {
            Title = "Reference",
            Color = ColorSettings.ReferenceLineColor,
            StrokeThickness = 1.5,
            YAxisKey = "Primary",
            MarkerType = MarkerType.None
        };
        
        // Verified series
        var verSeries = new LineSeries
        {
            Title = "Verified",
            Color = ColorSettings.VerifiedLineColor,
            StrokeThickness = 1.5,
            YAxisKey = "Primary",
            MarkerType = MarkerType.None
        };
        
        // C-O series (accepted)
        var coSeries = new LineSeries
        {
            Title = "C-O",
            Color = ColorSettings.COLineColor,
            StrokeThickness = 2,
            YAxisKey = "Secondary",
            MarkerType = MarkerType.None
        };
        
        // Rejected points scatter
        var rejectedSeries = new ScatterSeries
        {
            Title = "Rejected",
            MarkerType = MarkerType.Cross,
            MarkerSize = 6,
            MarkerFill = ColorSettings.RejectedPointColor,
            MarkerStroke = ColorSettings.RejectedPointColor,
            MarkerStrokeThickness = 2,
            YAxisKey = "Secondary"
        };
        
        foreach (var point in data.DataPoints.OrderBy(p => p.Timestamp))
        {
            var time = DateTimeAxis.ToDouble(point.Timestamp);
            
            refSeries.Points.Add(new DataPoint(time, point.ReferenceValue));
            verSeries.Points.Add(new DataPoint(time, point.VerifiedValue));
            
            if (point.Status == PointStatus.Rejected)
            {
                rejectedSeries.Points.Add(new ScatterPoint(time, point.CO));
            }
            else
            {
                coSeries.Points.Add(new DataPoint(time, point.CO));
            }
        }
        
        model.Series.Add(refSeries);
        model.Series.Add(verSeries);
        model.Series.Add(coSeries);
        
        if (rejectedSeries.Points.Count > 0)
        {
            model.Series.Add(rejectedSeries);
        }
        
        // Add 3-sigma boundaries if processed
        if (data.IsProcessed && data.Statistics != null)
        {
            AddSigmaBoundaries(model, data.Statistics, data.DataPoints, useSecondaryAxis: true);
        }
        
        return model;
    }
    
    #endregion
    
    #region C-O Only Chart
    
    /// <summary>
    /// Create a focused C-O chart with mean line and sigma boundaries
    /// </summary>
    public PlotModel CreateCOChart(SensorCalibrationData data, string title)
    {
        var model = CreateBaseModel($"{title} - C-O Analysis");
        
        if (data.DataPoints.Count == 0 || !data.IsProcessed || data.Statistics == null)
            return model;
        
        var stats = data.Statistics;
        
        // Time axis
        var timeAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Key = "Time",
            Title = "Time",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            StringFormat = "HH:mm:ss"
        };
        model.Axes.Add(timeAxis);
        
        // C-O axis - with fixed range based on ±4σ
        double mean = stats.MeanCO_Accepted;
        double sd = stats.StdDevCO_Accepted;
        double axisMin = mean - 4 * sd;
        double axisMax = mean + 4 * sd;
        
        // Ensure we show all data points
        double dataMin = data.DataPoints.Min(p => p.CO);
        double dataMax = data.DataPoints.Max(p => p.CO);
        axisMin = Math.Min(axisMin, dataMin - Math.Abs(dataMin) * 0.1);
        axisMax = Math.Max(axisMax, dataMax + Math.Abs(dataMax) * 0.1);
        
        var coAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Key = "CO",  // Explicit key for this chart
            Title = "C-O (°)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            Minimum = axisMin,
            Maximum = axisMax,
            IsPanEnabled = false,
            IsZoomEnabled = false
        };
        model.Axes.Add(coAxis);
        
        // Accepted C-O points
        var acceptedSeries = new ScatterSeries
        {
            Title = "Accepted",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = ColorSettings.AcceptedPointColor,
            MarkerStroke = OxyColors.Transparent,
            YAxisKey = "CO"
        };
        
        // Rejected C-O points
        var rejectedSeries = new ScatterSeries
        {
            Title = "Rejected",
            MarkerType = MarkerType.Cross,
            MarkerSize = 6,
            MarkerFill = ColorSettings.RejectedPointColor,
            MarkerStroke = ColorSettings.RejectedPointColor,
            MarkerStrokeThickness = 2,
            YAxisKey = "CO"
        };
        
        foreach (var point in data.DataPoints)
        {
            var time = DateTimeAxis.ToDouble(point.Timestamp);
            
            if (point.Status == PointStatus.Rejected)
            {
                rejectedSeries.Points.Add(new ScatterPoint(time, point.CO));
            }
            else
            {
                acceptedSeries.Points.Add(new ScatterPoint(time, point.CO));
            }
        }
        
        model.Series.Add(acceptedSeries);
        if (rejectedSeries.Points.Count > 0)
        {
            model.Series.Add(rejectedSeries);
        }
        
        // Add mean and sigma lines (using CO axis, not Secondary)
        AddSigmaBoundaries(model, stats, data.DataPoints, useSecondaryAxis: false, yAxisKey: "CO");
        
        return model;
    }
    
    #endregion
    
    #region Histogram
    
    /// <summary>
    /// Create a histogram of C-O distribution
    /// </summary>
    public PlotModel CreateHistogram(SensorCalibrationData data, string title, int binCount = 20)
    {
        var model = CreateBaseModel($"{title} - C-O Distribution");
        
        if (data.DataPoints.Count == 0)
            return model;
        
        var acceptedPoints = data.DataPoints
            .Where(p => p.Status == PointStatus.Accepted)
            .Select(p => p.CO)
            .ToList();
        
        if (acceptedPoints.Count == 0)
            return model;
        
        // Calculate histogram bins
        double min = acceptedPoints.Min();
        double max = acceptedPoints.Max();
        double range = max - min;
        
        if (range < 0.0001)
        {
            range = 0.01;
            min -= range / 2;
            max += range / 2;
        }
        
        double binWidth = range / binCount;
        var bins = new int[binCount];
        
        foreach (var value in acceptedPoints)
        {
            int binIndex = (int)((value - min) / binWidth);
            if (binIndex >= binCount) binIndex = binCount - 1;
            if (binIndex < 0) binIndex = 0;
            bins[binIndex]++;
        }
        
        // Create histogram series
        var histogramSeries = new RectangleBarSeries
        {
            Title = "C-O Distribution",
            FillColor = ColorSettings.COLineColor,
            StrokeColor = OxyColors.White,
            StrokeThickness = 1
        };
        
        for (int i = 0; i < binCount; i++)
        {
            double x0 = min + i * binWidth;
            double x1 = x0 + binWidth;
            histogramSeries.Items.Add(new RectangleBarItem(x0, 0, x1, bins[i]));
        }
        
        model.Series.Add(histogramSeries);
        
        // X axis (C-O values)
        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "C-O (°)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid
        };
        model.Axes.Add(xAxis);
        
        // Y axis (frequency)
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Frequency",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            Minimum = 0
        };
        model.Axes.Add(yAxis);
        
        // Add mean line
        if (data.Statistics != null)
        {
            var meanLine = new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = data.Statistics.MeanCO_Accepted,
                Color = ColorSettings.MeanLineColor,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                Text = $"Mean: {data.Statistics.MeanCO_Accepted:F4}°",
                TextColor = ColorSettings.MeanLineColor,
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Top
            };
            model.Annotations.Add(meanLine);
        }
        
        return model;
    }
    
    #endregion
    
    #region NEW: Scatter Plot (Reference vs Verified)
    
    /// <summary>
    /// Create a scatter plot showing Reference vs Verified values
    /// Ideal correlation would show points along the diagonal
    /// </summary>
    public PlotModel CreateCorrelationScatterPlot(SensorCalibrationData data, string title)
    {
        var model = CreateBaseModel($"{title} - Reference vs Verified Correlation");
        
        if (data.DataPoints.Count == 0)
            return model;
        
        // Calculate axis range
        double allMin = Math.Min(data.DataPoints.Min(p => p.ReferenceValue), data.DataPoints.Min(p => p.VerifiedValue));
        double allMax = Math.Max(data.DataPoints.Max(p => p.ReferenceValue), data.DataPoints.Max(p => p.VerifiedValue));
        double margin = (allMax - allMin) * 0.1;
        
        // X axis (Reference)
        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Reference (°)",
            TitleColor = ColorSettings.ReferenceLineColor,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            Minimum = allMin - margin,
            Maximum = allMax + margin
        };
        model.Axes.Add(xAxis);
        
        // Y axis (Verified)
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Verified (°)",
            TitleColor = ColorSettings.VerifiedLineColor,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            Minimum = allMin - margin,
            Maximum = allMax + margin
        };
        model.Axes.Add(yAxis);
        
        // Ideal correlation line (diagonal)
        var idealLine = new LineSeries
        {
            Title = "Perfect Correlation",
            Color = OxyColor.FromAColor(128, ChartForeground),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash
        };
        idealLine.Points.Add(new DataPoint(allMin - margin, allMin - margin));
        idealLine.Points.Add(new DataPoint(allMax + margin, allMax + margin));
        model.Series.Add(idealLine);
        
        // Accepted points
        var acceptedSeries = new ScatterSeries
        {
            Title = "Accepted",
            MarkerType = MarkerType.Circle,
            MarkerSize = 5,
            MarkerFill = ColorSettings.AcceptedPointColor,
            MarkerStroke = OxyColors.Transparent
        };
        
        // Rejected points
        var rejectedSeries = new ScatterSeries
        {
            Title = "Rejected",
            MarkerType = MarkerType.Cross,
            MarkerSize = 6,
            MarkerFill = ColorSettings.RejectedPointColor,
            MarkerStroke = ColorSettings.RejectedPointColor,
            MarkerStrokeThickness = 2
        };
        
        foreach (var point in data.DataPoints)
        {
            if (point.Status == PointStatus.Rejected)
            {
                rejectedSeries.Points.Add(new ScatterPoint(point.ReferenceValue, point.VerifiedValue));
            }
            else
            {
                acceptedSeries.Points.Add(new ScatterPoint(point.ReferenceValue, point.VerifiedValue));
            }
        }
        
        model.Series.Add(acceptedSeries);
        if (rejectedSeries.Points.Count > 0)
        {
            model.Series.Add(rejectedSeries);
        }
        
        return model;
    }
    
    #endregion
    
    #region NEW: Residual/Trend Plot
    
    /// <summary>
    /// Create a residual plot showing C-O over time with linear trend line
    /// Helps identify systematic drift
    /// </summary>
    public PlotModel CreateResidualTrendPlot(SensorCalibrationData data, string title)
    {
        var model = CreateBaseModel($"{title} - Residual Trend Analysis");
        
        if (data.DataPoints.Count == 0)
            return model;
        
        var acceptedPoints = data.DataPoints
            .Where(p => p.Status == PointStatus.Accepted)
            .OrderBy(p => p.Timestamp)
            .ToList();
        
        if (acceptedPoints.Count < 2)
            return model;
        
        // Time axis
        var timeAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            StringFormat = "HH:mm:ss"
        };
        model.Axes.Add(timeAxis);
        
        // C-O axis
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "C-O (°)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid
        };
        model.Axes.Add(yAxis);
        
        // C-O scatter points
        var coSeries = new ScatterSeries
        {
            Title = "C-O Values",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = ColorSettings.COLineColor,
            MarkerStroke = OxyColors.Transparent
        };
        
        // Collect data for trend calculation
        var xValues = new List<double>();
        var yValues = new List<double>();
        
        foreach (var point in acceptedPoints)
        {
            var time = DateTimeAxis.ToDouble(point.Timestamp);
            coSeries.Points.Add(new ScatterPoint(time, point.CO));
            xValues.Add(time);
            yValues.Add(point.CO);
        }
        
        model.Series.Add(coSeries);
        
        // Calculate linear regression for trend line
        if (xValues.Count >= 2)
        {
            var (slope, intercept) = CalculateLinearRegression(xValues, yValues);
            
            var trendLine = new LineSeries
            {
                Title = $"Trend (slope: {slope * 86400:F6}°/day)",  // Convert to per-day
                Color = OxyColor.FromRgb(255, 193, 7),  // Amber
                StrokeThickness = 2,
                LineStyle = LineStyle.Solid
            };
            
            double minX = xValues.Min();
            double maxX = xValues.Max();
            trendLine.Points.Add(new DataPoint(minX, slope * minX + intercept));
            trendLine.Points.Add(new DataPoint(maxX, slope * maxX + intercept));
            
            model.Series.Add(trendLine);
        }
        
        // Add mean line
        if (data.Statistics != null)
        {
            double minTime = DateTimeAxis.ToDouble(acceptedPoints.First().Timestamp);
            double maxTime = DateTimeAxis.ToDouble(acceptedPoints.Last().Timestamp);
            
            var meanLine = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = data.Statistics.MeanCO_Accepted,
                MinimumX = minTime,
                MaximumX = maxTime,
                Color = ColorSettings.MeanLineColor,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                Text = $"Mean: {data.Statistics.MeanCO_Accepted:F4}°"
            };
            model.Annotations.Add(meanLine);
        }
        
        return model;
    }
    
    #endregion
    
    #region NEW: Box Plot Summary
    
    /// <summary>
    /// Create a box plot showing statistical summary
    /// </summary>
    public PlotModel CreateBoxPlot(SensorCalibrationData pitchData, SensorCalibrationData rollData)
    {
        var model = CreateBaseModel("C-O Distribution - Box Plot Summary");
        
        // Category axis
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Sensor",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground
        };
        categoryAxis.Labels.Add("Pitch");
        categoryAxis.Labels.Add("Roll");
        model.Axes.Add(categoryAxis);
        
        // Value axis
        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "C-O (°)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid
        };
        model.Axes.Add(valueAxis);
        
        // Box plot series
        var boxSeries = new BoxPlotSeries
        {
            Fill = OxyColor.FromAColor(128, ColorSettings.COLineColor),
            Stroke = ColorSettings.COLineColor,
            StrokeThickness = 2,
            WhiskerWidth = 0.5,
            BoxWidth = 0.4
        };
        
        // Add Pitch box
        if (pitchData.DataPoints.Count > 0)
        {
            var pitchValues = pitchData.DataPoints
                .Where(p => p.Status == PointStatus.Accepted)
                .Select(p => p.CO)
                .OrderBy(v => v)
                .ToList();
            
            if (pitchValues.Count > 0)
            {
                var pitchBox = CreateBoxPlotItem(0, pitchValues);
                boxSeries.Items.Add(pitchBox);
            }
        }
        
        // Add Roll box
        if (rollData.DataPoints.Count > 0)
        {
            var rollValues = rollData.DataPoints
                .Where(p => p.Status == PointStatus.Accepted)
                .Select(p => p.CO)
                .OrderBy(v => v)
                .ToList();
            
            if (rollValues.Count > 0)
            {
                var rollBox = CreateBoxPlotItem(1, rollValues);
                boxSeries.Items.Add(rollBox);
            }
        }
        
        model.Series.Add(boxSeries);
        
        return model;
    }
    
    private BoxPlotItem CreateBoxPlotItem(double x, List<double> sortedValues)
    {
        int n = sortedValues.Count;
        double min = sortedValues[0];
        double max = sortedValues[n - 1];
        double median = GetPercentile(sortedValues, 50);
        double q1 = GetPercentile(sortedValues, 25);
        double q3 = GetPercentile(sortedValues, 75);
        double iqr = q3 - q1;
        
        // Whiskers extend to min/max within 1.5*IQR
        double lowerWhisker = Math.Max(min, q1 - 1.5 * iqr);
        double upperWhisker = Math.Min(max, q3 + 1.5 * iqr);
        
        // Find outliers
        var outliers = sortedValues.Where(v => v < lowerWhisker || v > upperWhisker).ToList();
        
        return new BoxPlotItem(x, lowerWhisker, q1, median, q3, upperWhisker)
        {
            Outliers = outliers
        };
    }
    
    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        int n = sortedValues.Count;
        double index = (percentile / 100.0) * (n - 1);
        int lower = (int)Math.Floor(index);
        int upper = (int)Math.Ceiling(index);
        
        if (lower == upper || upper >= n)
            return sortedValues[lower];
        
        double fraction = index - lower;
        return sortedValues[lower] + fraction * (sortedValues[upper] - sortedValues[lower]);
    }
    
    #endregion
    
    #region NEW: Cumulative Distribution Function (CDF)
    
    /// <summary>
    /// Create a cumulative distribution function chart
    /// Shows probability of C-O values within certain ranges
    /// </summary>
    public PlotModel CreateCDFChart(SensorCalibrationData data, string title)
    {
        var model = CreateBaseModel($"{title} - Cumulative Distribution");
        
        if (data.DataPoints.Count == 0)
            return model;
        
        var acceptedValues = data.DataPoints
            .Where(p => p.Status == PointStatus.Accepted)
            .Select(p => p.CO)
            .OrderBy(v => v)
            .ToList();
        
        if (acceptedValues.Count == 0)
            return model;
        
        // X axis (C-O values)
        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "C-O (°)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid
        };
        model.Axes.Add(xAxis);
        
        // Y axis (cumulative probability)
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Cumulative Probability",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            Minimum = 0,
            Maximum = 1,
            StringFormat = "P0"
        };
        model.Axes.Add(yAxis);
        
        // CDF line
        var cdfSeries = new LineSeries
        {
            Title = "Empirical CDF",
            Color = ColorSettings.COLineColor,
            StrokeThickness = 2
        };
        
        int n = acceptedValues.Count;
        for (int i = 0; i < n; i++)
        {
            double probability = (i + 1.0) / n;
            cdfSeries.Points.Add(new DataPoint(acceptedValues[i], probability));
        }
        
        model.Series.Add(cdfSeries);
        
        // Add reference lines at common percentiles
        var percentileLines = new[] { 0.05, 0.25, 0.50, 0.75, 0.95 };
        foreach (var p in percentileLines)
        {
            var line = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = p,
                Color = OxyColor.FromAColor(80, ChartForeground),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot,
                Text = $"{p:P0}",
                TextColor = ChartForeground,
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right
            };
            model.Annotations.Add(line);
        }
        
        return model;
    }
    
    #endregion
    
    #region NEW: Moving Average Chart
    
    /// <summary>
    /// Create a C-O chart with moving average overlay
    /// Helps identify gradual drift trends
    /// </summary>
    public PlotModel CreateMovingAverageChart(SensorCalibrationData data, string title, int windowSize = 10)
    {
        var model = CreateBaseModel($"{title} - Moving Average Analysis");
        
        if (data.DataPoints.Count == 0)
            return model;
        
        var sortedPoints = data.DataPoints
            .Where(p => p.Status == PointStatus.Accepted)
            .OrderBy(p => p.Timestamp)
            .ToList();
        
        if (sortedPoints.Count < windowSize)
            return model;
        
        // Time axis
        var timeAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            StringFormat = "HH:mm:ss"
        };
        model.Axes.Add(timeAxis);
        
        // C-O axis
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "C-O (°)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid
        };
        model.Axes.Add(yAxis);
        
        // Raw C-O scatter
        var rawSeries = new ScatterSeries
        {
            Title = "C-O Values",
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = OxyColor.FromAColor(128, ColorSettings.COLineColor),
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var point in sortedPoints)
        {
            rawSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(point.Timestamp), point.CO));
        }
        model.Series.Add(rawSeries);
        
        // Moving average line
        var maSeries = new LineSeries
        {
            Title = $"Moving Average ({windowSize} pts)",
            Color = OxyColor.FromRgb(255, 193, 7),  // Amber
            StrokeThickness = 2.5
        };
        
        for (int i = windowSize - 1; i < sortedPoints.Count; i++)
        {
            double sum = 0;
            for (int j = i - windowSize + 1; j <= i; j++)
            {
                sum += sortedPoints[j].CO;
            }
            double avg = sum / windowSize;
            var centerPoint = sortedPoints[i - windowSize / 2];
            maSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(centerPoint.Timestamp), avg));
        }
        
        model.Series.Add(maSeries);
        
        // Add mean line
        if (data.Statistics != null)
        {
            var meanLine = new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = data.Statistics.MeanCO_Accepted,
                Color = ColorSettings.MeanLineColor,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                Text = $"Mean: {data.Statistics.MeanCO_Accepted:F4}°"
            };
            model.Annotations.Add(meanLine);
        }
        
        return model;
    }
    
    #endregion
    
    #region NEW: Pitch vs Roll Correlation
    
    /// <summary>
    /// Create a chart showing correlation between Pitch and Roll C-O values
    /// Helps detect coupled errors between sensors
    /// </summary>
    public PlotModel CreatePitchRollCorrelationChart(SensorCalibrationData pitchData, SensorCalibrationData rollData)
    {
        var model = CreateBaseModel("Pitch vs Roll C-O Correlation");
        
        if (pitchData.DataPoints.Count == 0 || rollData.DataPoints.Count == 0)
            return model;
        
        // Match points by timestamp
        var pitchDict = pitchData.DataPoints
            .Where(p => p.Status == PointStatus.Accepted)
            .ToDictionary(p => p.Timestamp, p => p.CO);
        
        var matchedPoints = rollData.DataPoints
            .Where(p => p.Status == PointStatus.Accepted && pitchDict.ContainsKey(p.Timestamp))
            .Select(p => (PitchCO: pitchDict[p.Timestamp], RollCO: p.CO))
            .ToList();
        
        if (matchedPoints.Count == 0)
            return model;
        
        // Calculate axis ranges
        double pitchMin = matchedPoints.Min(p => p.PitchCO);
        double pitchMax = matchedPoints.Max(p => p.PitchCO);
        double rollMin = matchedPoints.Min(p => p.RollCO);
        double rollMax = matchedPoints.Max(p => p.RollCO);
        
        double pitchMargin = (pitchMax - pitchMin) * 0.15;
        double rollMargin = (rollMax - rollMin) * 0.15;
        
        // Handle case where margin is zero
        if (pitchMargin < 0.001) pitchMargin = 0.01;
        if (rollMargin < 0.001) rollMargin = 0.01;
        
        // X axis (Pitch C-O)
        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Pitch C-O (°)",
            TitleColor = OxyColor.FromRgb(33, 150, 243),  // Blue
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            Minimum = pitchMin - pitchMargin,
            Maximum = pitchMax + pitchMargin
        };
        model.Axes.Add(xAxis);
        
        // Y axis (Roll C-O)
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Roll C-O (°)",
            TitleColor = OxyColor.FromRgb(76, 175, 80),  // Green
            TextColor = ChartForeground,
            TicklineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Solid,
            Minimum = rollMin - rollMargin,
            Maximum = rollMax + rollMargin
        };
        model.Axes.Add(yAxis);
        
        // Scatter points
        var scatterSeries = new ScatterSeries
        {
            Title = "Matched Points",
            MarkerType = MarkerType.Circle,
            MarkerSize = 5,
            MarkerFill = OxyColor.FromRgb(156, 39, 176),  // Purple
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var point in matchedPoints)
        {
            scatterSeries.Points.Add(new ScatterPoint(point.PitchCO, point.RollCO));
        }
        
        model.Series.Add(scatterSeries);
        
        // Calculate and show correlation coefficient
        if (matchedPoints.Count >= 2)
        {
            var pitchValues = matchedPoints.Select(p => p.PitchCO).ToList();
            var rollValues = matchedPoints.Select(p => p.RollCO).ToList();
            double correlation = CalculateCorrelation(pitchValues, rollValues);
            
            var annotation = new TextAnnotation
            {
                Text = $"r = {correlation:F4}",
                TextPosition = new DataPoint(pitchMax - pitchMargin * 0.5, rollMax - rollMargin * 0.5),
                TextColor = ChartForeground,
                FontSize = 14,
                FontWeight = 700,  // Bold
                Stroke = OxyColors.Transparent
            };
            model.Annotations.Add(annotation);
        }
        
        // Add zero lines
        var zeroXLine = new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            X = 0,
            Color = OxyColor.FromAColor(100, ChartForeground),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dot
        };
        model.Annotations.Add(zeroXLine);
        
        var zeroYLine = new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 0,
            Color = OxyColor.FromAColor(100, ChartForeground),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dot
        };
        model.Annotations.Add(zeroYLine);
        
        return model;
    }
    
    #endregion
    
    #region Helper Methods
    
    private PlotModel CreateBaseModel(string title)
    {
        var model = new PlotModel
        {
            Title = title,
            TitleColor = ChartForeground,
            PlotAreaBorderColor = GridLines,
            PlotAreaBackground = ChartBackground,
            Background = ChartBackground
        };
        
        // Configure legend
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendBackground = OxyColor.FromAColor(200, ChartBackground),
            LegendBorder = GridLines,
            LegendTextColor = ChartForeground,
            LegendTitleColor = ChartForeground
        });
        
        return model;
    }
    
    private void AddSigmaBoundaries(PlotModel model, CalibrationStatistics stats, List<MruDataPoint> points, 
        bool useSecondaryAxis = true, string? yAxisKey = null)
    {
        if (points.Count == 0) return;
        
        var sortedPoints = points.OrderBy(p => p.Timestamp).ToList();
        double minTime = DateTimeAxis.ToDouble(sortedPoints.First().Timestamp);
        double maxTime = DateTimeAxis.ToDouble(sortedPoints.Last().Timestamp);
        
        double mean = stats.MeanCO_Accepted;
        double sd = stats.StdDevCO_Accepted;
        
        // Determine which axis key to use
        string axisKey = yAxisKey ?? (useSecondaryAxis ? "Secondary" : "");
        
        // Mean line
        var meanLine = new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = mean,
            MinimumX = minTime,
            MaximumX = maxTime,
            Color = ColorSettings.MeanLineColor,
            StrokeThickness = 2,
            LineStyle = LineStyle.Solid,
            Text = $"Mean: {mean:F4}°",
            TextColor = ColorSettings.MeanLineColor,
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right
        };
        if (!string.IsNullOrEmpty(axisKey))
            meanLine.YAxisKey = axisKey;
        model.Annotations.Add(meanLine);
        
        // +3σ line
        var upperLine = new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = mean + 3 * sd,
            MinimumX = minTime,
            MaximumX = maxTime,
            Color = ColorSettings.SigmaBoundaryColor,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.Dash,
            Text = $"+3σ: {mean + 3 * sd:F4}°",
            TextColor = ColorSettings.SigmaBoundaryColor,
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right
        };
        if (!string.IsNullOrEmpty(axisKey))
            upperLine.YAxisKey = axisKey;
        model.Annotations.Add(upperLine);
        
        // -3σ line
        var lowerLine = new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = mean - 3 * sd,
            MinimumX = minTime,
            MaximumX = maxTime,
            Color = ColorSettings.SigmaBoundaryColor,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.Dash,
            Text = $"-3σ: {mean - 3 * sd:F4}°",
            TextColor = ColorSettings.SigmaBoundaryColor,
            TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Right
        };
        if (!string.IsNullOrEmpty(axisKey))
            lowerLine.YAxisKey = axisKey;
        model.Annotations.Add(lowerLine);
    }
    
    private (double slope, double intercept) CalculateLinearRegression(List<double> x, List<double> y)
    {
        int n = x.Count;
        double sumX = x.Sum();
        double sumY = y.Sum();
        double sumXY = x.Zip(y, (a, b) => a * b).Sum();
        double sumX2 = x.Sum(v => v * v);
        
        double denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < 1e-10)
            return (0, y.Average());
            
        double slope = (n * sumXY - sumX * sumY) / denominator;
        double intercept = (sumY - slope * sumX) / n;
        
        return (slope, intercept);
    }
    
    private double CalculateCorrelation(List<double> x, List<double> y)
    {
        int n = x.Count;
        double meanX = x.Average();
        double meanY = y.Average();
        
        double sumXY = 0, sumX2 = 0, sumY2 = 0;
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            sumXY += dx * dy;
            sumX2 += dx * dx;
            sumY2 += dy * dy;
        }
        
        double denominator = Math.Sqrt(sumX2 * sumY2);
        return denominator > 0 ? sumXY / denominator : 0;
    }
    
    #endregion
    
    #region New Chart Types (v2.3.0)
    
    /// <summary>
    /// Create Q-Q Plot to check normality of residuals
    /// </summary>
    public PlotModel CreateQQPlot(SensorCalibrationData data, string title)
    {
        var model = CreateBaseModel($"{title} Q-Q Plot (Normality Check)");
        
        var acceptedPoints = data.DataPoints
            .Where(p => p.Status == PointStatus.Accepted)
            .OrderBy(p => p.CO)
            .ToList();
            
        if (acceptedPoints.Count < 3) return model;
        
        // Calculate theoretical quantiles
        var n = acceptedPoints.Count;
        var theoreticalQuantiles = new List<double>();
        var sampleQuantiles = new List<double>();
        
        for (int i = 0; i < n; i++)
        {
            // Blom's plotting position for Q-Q plot
            double p = (i + 0.375) / (n + 0.25);
            double z = NormInv(p);
            theoreticalQuantiles.Add(z);
            sampleQuantiles.Add(acceptedPoints[i].CO);
        }
        
        // Standardize sample quantiles
        double mean = sampleQuantiles.Average();
        double sd = Math.Sqrt(sampleQuantiles.Sum(x => Math.Pow(x - mean, 2)) / (n - 1));
        var standardizedSample = sampleQuantiles.Select(x => (x - mean) / sd).ToList();
        
        // Add axes
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Theoretical Quantiles (Standard Normal)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            AxislineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Dot
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Sample Quantiles (Standardized C-O)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            AxislineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Dot
        });
        
        // Add scatter points
        var scatter = new ScatterSeries
        {
            MarkerType = MarkerType.Circle,
            MarkerSize = 5,
            MarkerFill = ColorSettings.AcceptedPointColor,
            MarkerStroke = OxyColors.Transparent,
            Title = "C-O Values"
        };
        
        for (int i = 0; i < n; i++)
        {
            scatter.Points.Add(new ScatterPoint(theoreticalQuantiles[i], standardizedSample[i]));
        }
        model.Series.Add(scatter);
        
        // Add ideal line (y = x)
        double minQ = theoreticalQuantiles.Min();
        double maxQ = theoreticalQuantiles.Max();
        
        var idealLine = new LineSeries
        {
            Color = ColorSettings.MeanLineColor,
            StrokeThickness = 2,
            LineStyle = LineStyle.Solid,
            Title = "Normal Distribution"
        };
        idealLine.Points.Add(new DataPoint(minQ, minQ));
        idealLine.Points.Add(new DataPoint(maxQ, maxQ));
        model.Series.Add(idealLine);
        
        // Add normality indicator annotation
        bool isNormal = data.Statistics?.IsNormallyDistributed ?? true;
        var annotation = new TextAnnotation
        {
            Text = isNormal ? "✓ Normally Distributed" : "⚠ Non-Normal Distribution",
            TextPosition = new DataPoint(minQ + 0.5, maxQ - 0.3),
            TextColor = isNormal ? ColorSettings.AcceptedPointColor : ColorSettings.RejectedPointColor,
            FontSize = 12,
            FontWeight = 700,
            Stroke = OxyColors.Transparent
        };
        model.Annotations.Add(annotation);
        
        return model;
    }
    
    /// <summary>
    /// Create Autocorrelation Plot to detect patterns in residuals
    /// </summary>
    public PlotModel CreateAutocorrelationPlot(SensorCalibrationData data, string title, int maxLag = 20)
    {
        var model = CreateBaseModel($"{title} Autocorrelation (Pattern Detection)");
        
        var acceptedPoints = data.DataPoints
            .Where(p => p.Status == PointStatus.Accepted)
            .OrderBy(p => p.Timestamp)
            .Select(p => p.CO)
            .ToList();
            
        if (acceptedPoints.Count < maxLag + 5) 
        {
            maxLag = Math.Max(5, acceptedPoints.Count / 2);
        }
        
        int n = acceptedPoints.Count;
        double mean = acceptedPoints.Average();
        double variance = acceptedPoints.Sum(x => Math.Pow(x - mean, 2)) / n;
        
        if (variance < 1e-10) return model;
        
        // Calculate autocorrelations
        var lags = new List<int>();
        var acf = new List<double>();
        
        for (int lag = 0; lag <= maxLag; lag++)
        {
            double sum = 0;
            for (int i = 0; i < n - lag; i++)
            {
                sum += (acceptedPoints[i] - mean) * (acceptedPoints[i + lag] - mean);
            }
            double r = sum / (n * variance);
            lags.Add(lag);
            acf.Add(r);
        }
        
        // Add axes
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Lag",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            AxislineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Dot,
            Minimum = -0.5,
            Maximum = maxLag + 0.5
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Autocorrelation",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            AxislineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Dot,
            Minimum = -1.1,
            Maximum = 1.1
        });
        
        // Add bars for autocorrelation values
        var barSeries = new RectangleBarSeries
        {
            Title = "ACF"
        };
        
        for (int i = 0; i < lags.Count; i++)
        {
            var color = Math.Abs(acf[i]) > 2.0 / Math.Sqrt(n) 
                ? ColorSettings.RejectedPointColor 
                : ColorSettings.AcceptedPointColor;
                
            barSeries.Items.Add(new RectangleBarItem(lags[i] - 0.3, 0, lags[i] + 0.3, acf[i])
            {
                Color = color
            });
        }
        model.Series.Add(barSeries);
        
        // Add confidence bounds (95%)
        double bound = 1.96 / Math.Sqrt(n);
        
        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = bound,
            Color = ColorSettings.SigmaBoundaryColor,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.Dash,
            Text = "+95% CI"
        });
        
        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = -bound,
            Color = ColorSettings.SigmaBoundaryColor,
            StrokeThickness = 1.5,
            LineStyle = LineStyle.Dash,
            Text = "-95% CI"
        });
        
        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 0,
            Color = ChartForeground,
            StrokeThickness = 1,
            LineStyle = LineStyle.Solid
        });
        
        return model;
    }
    
    /// <summary>
    /// Create Before/After Comparison Chart showing effect of outlier rejection
    /// </summary>
    public PlotModel CreateBeforeAfterChart(SensorCalibrationData data, string title)
    {
        var model = CreateBaseModel($"{title} Before/After Outlier Rejection");
        
        var allPoints = data.DataPoints.OrderBy(p => p.Timestamp).ToList();
        if (allPoints.Count == 0) return model;
        
        var acceptedPoints = allPoints.Where(p => p.Status == PointStatus.Accepted).ToList();
        var rejectedPoints = allPoints.Where(p => p.Status == PointStatus.Rejected).ToList();
        
        // Add time axis
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            AxislineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Dot,
            StringFormat = "HH:mm:ss"
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "C-O Value (°)",
            TitleColor = ChartForeground,
            TextColor = ChartForeground,
            AxislineColor = ChartForeground,
            MajorGridlineColor = GridLines,
            MajorGridlineStyle = LineStyle.Dot
        });
        
        // Before: All points as scatter
        var beforeSeries = new ScatterSeries
        {
            Title = "All Points (Before)",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColor.FromAColor(100, ChartForeground),
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var point in allPoints)
        {
            beforeSeries.Points.Add(new ScatterPoint(
                DateTimeAxis.ToDouble(point.Timestamp), 
                point.CO));
        }
        model.Series.Add(beforeSeries);
        
        // After: Accepted points highlighted
        var acceptedSeries = new ScatterSeries
        {
            Title = $"Accepted ({acceptedPoints.Count})",
            MarkerType = MarkerType.Circle,
            MarkerSize = 6,
            MarkerFill = ColorSettings.AcceptedPointColor,
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var point in acceptedPoints)
        {
            acceptedSeries.Points.Add(new ScatterPoint(
                DateTimeAxis.ToDouble(point.Timestamp), 
                point.CO));
        }
        model.Series.Add(acceptedSeries);
        
        // Rejected points
        var rejectedSeries = new ScatterSeries
        {
            Title = $"Rejected ({rejectedPoints.Count})",
            MarkerType = MarkerType.Cross,
            MarkerSize = 7,
            MarkerFill = ColorSettings.RejectedPointColor,
            MarkerStroke = ColorSettings.RejectedPointColor,
            MarkerStrokeThickness = 2
        };
        
        foreach (var point in rejectedPoints)
        {
            rejectedSeries.Points.Add(new ScatterPoint(
                DateTimeAxis.ToDouble(point.Timestamp), 
                point.CO));
        }
        model.Series.Add(rejectedSeries);
        
        // Add statistics annotations
        if (data.Statistics != null)
        {
            double meanAll = data.Statistics.MeanCO_All;
            double meanAccepted = data.Statistics.MeanCO_Accepted;
            
            double minTime = DateTimeAxis.ToDouble(allPoints.First().Timestamp);
            double maxTime = DateTimeAxis.ToDouble(allPoints.Last().Timestamp);
            
            // Before mean line
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = meanAll,
                MinimumX = minTime,
                MaximumX = maxTime,
                Color = OxyColor.FromAColor(150, ChartForeground),
                StrokeThickness = 1.5,
                LineStyle = LineStyle.Dash,
                Text = $"Mean (All): {meanAll:F4}°"
            });
            
            // After mean line
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Horizontal,
                Y = meanAccepted,
                MinimumX = minTime,
                MaximumX = maxTime,
                Color = ColorSettings.MeanLineColor,
                StrokeThickness = 2,
                LineStyle = LineStyle.Solid,
                Text = $"Mean (Accepted): {meanAccepted:F4}°"
            });
        }
        
        return model;
    }
    
    /// <summary>
    /// Inverse normal CDF for Q-Q plot
    /// </summary>
    private double NormInv(double p)
    {
        if (p <= 0) return -10;
        if (p >= 1) return 10;
        
        double a1 = -3.969683028665376e+01;
        double a2 = 2.209460984245205e+02;
        double a3 = -2.759285104469687e+02;
        double a4 = 1.383577518672690e+02;
        double a5 = -3.066479806614716e+01;
        double a6 = 2.506628277459239e+00;
        
        double b1 = -5.447609879822406e+01;
        double b2 = 1.615858368580409e+02;
        double b3 = -1.556989798598866e+02;
        double b4 = 6.680131188771972e+01;
        double b5 = -1.328068155288572e+01;
        
        double c1 = -7.784894002430293e-03;
        double c2 = -3.223964580411365e-01;
        double c3 = -2.400758277161838e+00;
        double c4 = -2.549732539343734e+00;
        double c5 = 4.374664141464968e+00;
        double c6 = 2.938163982698783e+00;
        
        double d1 = 7.784695709041462e-03;
        double d2 = 3.224671290700398e-01;
        double d3 = 2.445134137142996e+00;
        double d4 = 3.754408661907416e+00;
        
        double pLow = 0.02425;
        double pHigh = 1 - pLow;
        double q, r;
        
        if (p < pLow)
        {
            q = Math.Sqrt(-2 * Math.Log(p));
            return (((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                   ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
        else if (p <= pHigh)
        {
            q = p - 0.5;
            r = q * q;
            return (((((a1 * r + a2) * r + a3) * r + a4) * r + a5) * r + a6) * q /
                   (((((b1 * r + b2) * r + b3) * r + b4) * r + b5) * r + 1);
        }
        else
        {
            q = Math.Sqrt(-2 * Math.Log(1 - p));
            return -(((((c1 * q + c2) * q + c3) * q + c4) * q + c5) * q + c6) /
                    ((((d1 * q + d2) * q + d3) * q + d4) * q + 1);
        }
    }
    
    #endregion
}

#region Chart Color Settings

/// <summary>
/// User-customizable color settings for charts
/// </summary>
public class ChartColorSettings
{
    // Line colors
    public OxyColor ReferenceLineColor { get; set; } = OxyColor.FromRgb(66, 165, 245);   // Blue
    public OxyColor VerifiedLineColor { get; set; } = OxyColor.FromRgb(102, 187, 106);   // Green
    public OxyColor COLineColor { get; set; } = OxyColor.FromRgb(232, 93, 0);            // Subsea7 Orange
    
    // Point colors
    public OxyColor AcceptedPointColor { get; set; } = OxyColor.FromRgb(102, 187, 106); // Green
    public OxyColor RejectedPointColor { get; set; } = OxyColor.FromRgb(239, 83, 80);   // Red
    
    // Annotation colors
    public OxyColor MeanLineColor { get; set; } = OxyColor.FromRgb(0, 150, 136);         // Subsea7 Teal
    public OxyColor SigmaBoundaryColor { get; set; } = OxyColor.FromRgb(239, 83, 80);   // Red
    
    /// <summary>
    /// Create a copy of current settings
    /// </summary>
    public ChartColorSettings Clone()
    {
        return new ChartColorSettings
        {
            ReferenceLineColor = ReferenceLineColor,
            VerifiedLineColor = VerifiedLineColor,
            COLineColor = COLineColor,
            AcceptedPointColor = AcceptedPointColor,
            RejectedPointColor = RejectedPointColor,
            MeanLineColor = MeanLineColor,
            SigmaBoundaryColor = SigmaBoundaryColor
        };
    }
    
    /// <summary>
    /// Reset to default Subsea7 brand colors
    /// </summary>
    public void ResetToDefaults()
    {
        ReferenceLineColor = OxyColor.FromRgb(66, 165, 245);
        VerifiedLineColor = OxyColor.FromRgb(102, 187, 106);
        COLineColor = OxyColor.FromRgb(232, 93, 0);
        AcceptedPointColor = OxyColor.FromRgb(102, 187, 106);
        RejectedPointColor = OxyColor.FromRgb(239, 83, 80);
        MeanLineColor = OxyColor.FromRgb(0, 150, 136);
        SigmaBoundaryColor = OxyColor.FromRgb(239, 83, 80);
    }
    
    /// <summary>
    /// Predefined color scheme: High Contrast
    /// </summary>
    public static ChartColorSettings HighContrast => new()
    {
        ReferenceLineColor = OxyColor.FromRgb(0, 176, 255),      // Cyan
        VerifiedLineColor = OxyColor.FromRgb(0, 255, 0),         // Lime
        COLineColor = OxyColor.FromRgb(255, 255, 0),             // Yellow
        AcceptedPointColor = OxyColor.FromRgb(0, 255, 0),        // Lime
        RejectedPointColor = OxyColor.FromRgb(255, 0, 0),        // Red
        MeanLineColor = OxyColor.FromRgb(255, 255, 255),         // White
        SigmaBoundaryColor = OxyColor.FromRgb(255, 0, 255)       // Magenta
    };
    
    /// <summary>
    /// Predefined color scheme: Color Blind Friendly
    /// </summary>
    public static ChartColorSettings ColorBlindFriendly => new()
    {
        ReferenceLineColor = OxyColor.FromRgb(0, 114, 178),      // Blue
        VerifiedLineColor = OxyColor.FromRgb(230, 159, 0),       // Orange
        COLineColor = OxyColor.FromRgb(204, 121, 167),           // Reddish Purple
        AcceptedPointColor = OxyColor.FromRgb(0, 158, 115),      // Bluish Green
        RejectedPointColor = OxyColor.FromRgb(213, 94, 0),       // Vermilion
        MeanLineColor = OxyColor.FromRgb(86, 180, 233),          // Sky Blue
        SigmaBoundaryColor = OxyColor.FromRgb(240, 228, 66)      // Yellow
    };
    
    /// <summary>
    /// Predefined color scheme: Monochrome
    /// </summary>
    public static ChartColorSettings Monochrome => new()
    {
        ReferenceLineColor = OxyColor.FromRgb(100, 100, 100),
        VerifiedLineColor = OxyColor.FromRgb(150, 150, 150),
        COLineColor = OxyColor.FromRgb(200, 200, 200),
        AcceptedPointColor = OxyColor.FromRgb(180, 180, 180),
        RejectedPointColor = OxyColor.FromRgb(80, 80, 80),
        MeanLineColor = OxyColor.FromRgb(255, 255, 255),
        SigmaBoundaryColor = OxyColor.FromRgb(120, 120, 120)
    };
}

#endregion
