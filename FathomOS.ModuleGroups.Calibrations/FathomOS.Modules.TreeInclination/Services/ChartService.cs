namespace FathomOS.Modules.TreeInclination.Services;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using FathomOS.Modules.TreeInclination.Models;

/// <summary>OxyPlot chart generation service</summary>
public static class ChartService
{
    private static readonly OxyColor BackgroundColor = OxyColor.FromRgb(26, 29, 35);
    private static readonly OxyColor TextColor = OxyColor.FromRgb(243, 244, 246);
    private static readonly OxyColor GridColor = OxyColor.FromRgb(55, 65, 81);
    private static readonly OxyColor AxisColor = OxyColor.FromRgb(156, 163, 175);
    private static readonly OxyColor BlueColor = OxyColor.FromRgb(59, 130, 246);
    private static readonly OxyColor GreenColor = OxyColor.FromRgb(34, 197, 94);
    private static readonly OxyColor RedColor = OxyColor.FromRgb(239, 68, 68);
    private static readonly OxyColor OrangeColor = OxyColor.FromRgb(245, 158, 11);

    public static PlotModel CreateDepthProfileChart(InclinationProject project)
    {
        var model = new PlotModel
        {
            Title = "Depth Profile by Corner",
            TitleFontSize = 14,
            TitleFontWeight = FontWeights.Bold,
            Background = BackgroundColor,
            TextColor = TextColor,
            PlotAreaBorderColor = GridColor
        };

        var corners = project.Corners.Where(c => !c.IsClosurePoint).OrderBy(c => c.Index).ToList();
        if (corners.Count == 0) return model;

        // Category axis for corners
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Left,
            Title = "Corner",
            AxislineColor = AxisColor,
            TicklineColor = AxisColor,
            TitleColor = AxisColor,
            TextColor = AxisColor,
            ItemsSource = corners.Select(c => c.Name).ToList()
        };
        model.Axes.Add(categoryAxis);

        // Value axis for depth (inverted)
        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Depth (m)",
            AxislineColor = AxisColor,
            TicklineColor = AxisColor,
            TitleColor = AxisColor,
            TextColor = AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = GridColor
        };
        model.Axes.Add(valueAxis);

        // Raw depth bars
        var rawSeries = new BarSeries
        {
            Title = "Raw Depth",
            FillColor = BlueColor,
            StrokeColor = OxyColor.FromRgb(37, 99, 235),
            StrokeThickness = 1
        };

        // Corrected depth bars
        var correctedSeries = new BarSeries
        {
            Title = "Corrected Depth",
            FillColor = GreenColor,
            StrokeColor = OxyColor.FromRgb(22, 163, 74),
            StrokeThickness = 1
        };

        foreach (var corner in corners)
        {
            rawSeries.Items.Add(new BarItem { Value = corner.RawDepthAverage });
            correctedSeries.Items.Add(new BarItem { Value = corner.CorrectedDepth });
        }

        model.Series.Add(rawSeries);
        model.Series.Add(correctedSeries);

        // Mean depth line
        if (project.Result != null)
        {
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = project.Result.MeanDepth,
                Color = OrangeColor,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                Text = $"Mean: {project.Result.MeanDepth:F3} m",
                TextColor = OrangeColor
            });
        }

        return model;
    }

    public static PlotModel CreatePlanViewChart(InclinationProject project)
    {
        var model = new PlotModel
        {
            Title = "Structure Plan View",
            TitleFontSize = 14,
            TitleFontWeight = FontWeights.Bold,
            Background = BackgroundColor,
            TextColor = TextColor,
            PlotAreaBorderColor = GridColor,
            IsLegendVisible = false
        };

        var corners = project.Corners.Where(c => !c.IsClosurePoint).OrderBy(c => c.Index).ToList();
        if (corners.Count < 3) return model;

        // Calculate bounds with padding
        double minX = corners.Min(c => c.X);
        double maxX = corners.Max(c => c.X);
        double minY = corners.Min(c => c.Y);
        double maxY = corners.Max(c => c.Y);
        
        double rangeX = maxX - minX;
        double rangeY = maxY - minY;
        double maxRange = Math.Max(rangeX, rangeY);
        if (maxRange < 0.1) maxRange = 10; // Default range if all points are same
        
        double padding = maxRange * 0.2;
        double centerX = (minX + maxX) / 2;
        double centerY = (minY + maxY) / 2;

        // Equal aspect ratio axes with fixed limits
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "X (m)",
            Minimum = centerX - maxRange / 2 - padding,
            Maximum = centerX + maxRange / 2 + padding,
            AxislineColor = AxisColor,
            TicklineColor = AxisColor,
            TitleColor = AxisColor,
            TextColor = AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = GridColor,
            IsPanEnabled = true,
            IsZoomEnabled = true,
            MinimumRange = 0.1,
            MaximumRange = maxRange * 10
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Y (m)",
            Minimum = centerY - maxRange / 2 - padding,
            Maximum = centerY + maxRange / 2 + padding,
            AxislineColor = AxisColor,
            TicklineColor = AxisColor,
            TitleColor = AxisColor,
            TextColor = AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = GridColor,
            IsPanEnabled = true,
            IsZoomEnabled = true,
            MinimumRange = 0.1,
            MaximumRange = maxRange * 10
        });

        // Structure outline
        var outlineSeries = new LineSeries
        {
            Title = "Structure",
            Color = BlueColor,
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 8,
            MarkerFill = BlueColor,
            MarkerStroke = OxyColor.FromRgb(37, 99, 235),
            MarkerStrokeThickness = 2
        };

        foreach (var corner in corners)
        {
            outlineSeries.Points.Add(new DataPoint(corner.X, corner.Y));
        }
        outlineSeries.Points.Add(new DataPoint(corners[0].X, corners[0].Y)); // Close polygon
        model.Series.Add(outlineSeries);

        // Corner labels
        foreach (var corner in corners)
        {
            model.Annotations.Add(new TextAnnotation
            {
                Text = corner.Name,
                TextPosition = new DataPoint(corner.X, corner.Y),
                TextColor = TextColor,
                FontSize = 10,
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Bottom,
                Stroke = OxyColors.Transparent
            });
        }

        // Inclination vector
        if (project.Result != null && project.Result.TotalInclination > 0.001)
        {
            double cx = corners.Average(c => c.X);
            double cy = corners.Average(c => c.Y);

            double scale = maxRange * 0.3;

            double headingRad = project.Result.InclinationHeading * Math.PI / 180.0;
            double dx = scale * Math.Sin(headingRad);
            double dy = scale * Math.Cos(headingRad);

            var vectorSeries = new LineSeries
            {
                Title = "Inclination",
                Color = RedColor,
                StrokeThickness = 3,
                MarkerType = MarkerType.Triangle,
                MarkerSize = 8,
                MarkerFill = RedColor
            };
            vectorSeries.Points.Add(new DataPoint(cx, cy));
            vectorSeries.Points.Add(new DataPoint(cx + dx, cy + dy));
            model.Series.Add(vectorSeries);
        }

        return model;
    }

    public static PlotModel CreateTimeSeriesChart(NpdFileInfo fileInfo)
    {
        var model = new PlotModel
        {
            Title = $"Time Series - {fileInfo.FileName}",
            TitleFontSize = 14,
            TitleFontWeight = FontWeights.Bold,
            Background = BackgroundColor,
            TextColor = TextColor,
            PlotAreaBorderColor = GridColor
        };

        if (fileInfo.Readings.Count == 0) return model;

        // Time axis
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            AxislineColor = AxisColor,
            TicklineColor = AxisColor,
            TitleColor = AxisColor,
            TextColor = AxisColor,
            StringFormat = "HH:mm:ss",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = GridColor
        });

        // Depth axis (inverted)
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Depth (m)",
            StartPosition = 1,
            EndPosition = 0,
            AxislineColor = AxisColor,
            TicklineColor = AxisColor,
            TitleColor = AxisColor,
            TextColor = AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = GridColor
        });

        // Normal readings
        var normalSeries = new LineSeries
        {
            Title = "Depth",
            Color = BlueColor,
            StrokeThickness = 1.5
        };

        // Outliers
        var outlierSeries = new ScatterSeries
        {
            Title = "Outliers",
            MarkerType = MarkerType.Cross,
            MarkerSize = 5,
            MarkerFill = RedColor,
            MarkerStroke = RedColor
        };

        foreach (var reading in fileInfo.Readings.OrderBy(r => r.Timestamp))
        {
            double x = DateTimeAxis.ToDouble(reading.Timestamp);

            if (reading.IsOutlier)
            {
                outlierSeries.Points.Add(new ScatterPoint(x, reading.ConvertedDepth));
            }
            else
            {
                normalSeries.Points.Add(new DataPoint(x, reading.ConvertedDepth));
            }
        }

        model.Series.Add(normalSeries);
        if (outlierSeries.Points.Count > 0)
            model.Series.Add(outlierSeries);

        // Mean line
        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = fileInfo.RawDepthAverage,
            Color = OrangeColor,
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash,
            Text = $"Mean: {fileInfo.RawDepthAverage:F3} m",
            TextColor = OrangeColor
        });

        // Std dev bands
        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = fileInfo.RawDepthAverage + fileInfo.RawDepthStdDev,
            Color = GreenColor,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dot
        });

        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = fileInfo.RawDepthAverage - fileInfo.RawDepthStdDev,
            Color = GreenColor,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dot
        });

        return model;
    }

    public static PlotModel CreateVectorDiagram(InclinationResult result)
    {
        var model = new PlotModel
        {
            Title = "Inclination Vector",
            TitleFontSize = 14,
            TitleFontWeight = FontWeights.Bold,
            Background = BackgroundColor,
            TextColor = TextColor,
            PlotAreaBorderColor = GridColor,
            PlotType = PlotType.Polar
        };

        // Angle axis (0-360 degrees, 0 = North)
        model.Axes.Add(new AngleAxis
        {
            Minimum = 0,
            Maximum = 360,
            MajorStep = 45,
            MinorStep = 15,
            StartAngle = 90,
            EndAngle = -270,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = GridColor,
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromArgb(100, 55, 65, 81),
            AxislineColor = AxisColor,
            TextColor = AxisColor
        });

        // Magnitude axis
        double maxMag = Math.Max(1.0, Math.Ceiling(result.TotalInclination * 1.5));
        model.Axes.Add(new MagnitudeAxis
        {
            Minimum = 0,
            Maximum = maxMag,
            MajorStep = maxMag / 4,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = GridColor,
            AxislineColor = AxisColor,
            TextColor = AxisColor
        });

        // Cardinal direction labels
        var directions = new[] { ("N", 0), ("E", 90), ("S", 180), ("W", 270) };
        foreach (var (label, angle) in directions)
        {
            model.Annotations.Add(new TextAnnotation
            {
                Text = label,
                TextPosition = new DataPoint(angle, maxMag * 1.1),
                TextColor = TextColor,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Stroke = OxyColors.Transparent
            });
        }

        // Vector from origin to inclination point
        var vectorSeries = new LineSeries
        {
            Color = RedColor,
            StrokeThickness = 3,
            MarkerType = MarkerType.Circle,
            MarkerSize = 8,
            MarkerFill = RedColor
        };

        vectorSeries.Points.Add(new DataPoint(0, 0));
        vectorSeries.Points.Add(new DataPoint(result.InclinationHeading, result.TotalInclination));
        model.Series.Add(vectorSeries);

        // Pitch/Roll components
        var pitchPoint = new ScatterSeries
        {
            Title = $"Pitch: {result.AveragePitch:F3}°",
            MarkerType = MarkerType.Square,
            MarkerSize = 6,
            MarkerFill = BlueColor
        };
        pitchPoint.Points.Add(new ScatterPoint(0, Math.Abs(result.AveragePitch)));
        model.Series.Add(pitchPoint);

        var rollPoint = new ScatterSeries
        {
            Title = $"Roll: {result.AverageRoll:F3}°",
            MarkerType = MarkerType.Diamond,
            MarkerSize = 6,
            MarkerFill = GreenColor
        };
        rollPoint.Points.Add(new ScatterPoint(90, Math.Abs(result.AverageRoll)));
        model.Series.Add(rollPoint);

        return model;
    }
}
