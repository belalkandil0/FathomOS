using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using FathomOS.Core.Models;

namespace FathomOS.Core.Export;

/// <summary>
/// Exports survey data to DXF format using netDxf library
/// </summary>
public class DxfExporter
{
    private readonly DxfExportOptions _options;

    public DxfExporter(DxfExportOptions? options = null)
    {
        _options = options ?? new DxfExportOptions();
    }

    /// <summary>
    /// Export survey data to DXF file
    /// </summary>
    /// <param name="filePath">Output file path</param>
    /// <param name="points">Main survey points</param>
    /// <param name="route">Optional route data</param>
    /// <param name="project">Project settings</param>
    /// <param name="splinePoints">Optional spline-fitted points</param>
    /// <param name="intervalPoints">Optional interval measure points</param>
    public void Export(string filePath, IList<SurveyPoint> points, RouteData? route, Project project,
        IList<SurveyPoint>? splinePoints = null, 
        IList<(double X, double Y, double Z, double Distance)>? intervalPoints = null)
    {
        var doc = new DxfDocument();

        // Create layers
        var trackLayer = CreateLayer(doc, "Survey_Track", AciColor.Green);
        var pointsLayer = CreateLayer(doc, "Survey_Points", AciColor.Cyan);
        var labelsLayer = CreateLayer(doc, "KP_Labels", AciColor.Yellow);
        var routeLayer = CreateLayer(doc, "Route_Centerline", AciColor.Red);
        var fixesLayer = CreateLayer(doc, "Survey_Fixes", AciColor.Magenta);
        
        // Additional layers for generated data
        var splineLayer = CreateLayer(doc, "Spline_Fit", new AciColor(40)); // Orange
        var intervalLayer = CreateLayer(doc, "Interval_Points", AciColor.Magenta);

        // Draw route centerline if available
        if (route != null && _options.IncludeRoute)
        {
            DrawRoute(doc, route, routeLayer);
        }

        // Draw survey track as polyline (using X, Y coordinates)
        if (points.Count > 0)
        {
            DrawTrack(doc, points, trackLayer);
        }

        // Draw survey points
        if (_options.IncludePoints)
        {
            DrawPoints(doc, points, pointsLayer);
        }

        // Draw KP labels
        if (_options.IncludeKpLabels && _options.KpLabelInterval > 0)
        {
            DrawKpLabels(doc, points, labelsLayer);
        }

        // Draw survey fixes
        if (_options.IncludeFixes && project.SurveyFixes.Count > 0)
        {
            DrawFixes(doc, project.SurveyFixes, fixesLayer);
        }
        
        // Draw spline-fitted points if provided
        if (splinePoints != null && splinePoints.Count > 1)
        {
            DrawSplineTrack(doc, splinePoints, splineLayer);
        }
        
        // Draw interval points if provided
        if (intervalPoints != null && intervalPoints.Count > 0)
        {
            DrawIntervalPoints(doc, intervalPoints, intervalLayer);
        }

        // Add title block
        if (_options.IncludeTitleBlock)
        {
            DrawTitleBlock(doc, project);
        }

        doc.Save(filePath);
    }
    
    /// <summary>
    /// Draw spline-fitted track
    /// </summary>
    private void DrawSplineTrack(DxfDocument doc, IList<SurveyPoint> points, Layer layer)
    {
        if (points.Count < 2) return;
        
        // Calculate gap threshold (10x average spacing)
        var avgSpacing = CalculateAverageSpacing(points);
        double gapThreshold = avgSpacing * 10;
        
        // Split points into segments at gaps
        var segments = new List<List<netDxf.Vector3>>();
        var currentSegment = new List<netDxf.Vector3>();
        
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var vertex = new netDxf.Vector3(
                p.SmoothedEasting ?? p.Easting, 
                p.SmoothedNorthing ?? p.Northing, 
                0);
            
            if (i == 0)
            {
                currentSegment.Add(vertex);
            }
            else
            {
                var prevP = points[i - 1];
                double dx = (p.SmoothedEasting ?? p.Easting) - (prevP.SmoothedEasting ?? prevP.Easting);
                double dy = (p.SmoothedNorthing ?? p.Northing) - (prevP.SmoothedNorthing ?? prevP.Northing);
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                if (distance > gapThreshold && currentSegment.Count > 1)
                {
                    // Gap detected - save current segment and start new one
                    segments.Add(currentSegment);
                    currentSegment = new List<netDxf.Vector3>();
                }
                currentSegment.Add(vertex);
            }
        }
        
        // Add final segment
        if (currentSegment.Count > 1)
        {
            segments.Add(currentSegment);
        }
        
        // Create polylines for each segment (not closed)
        foreach (var segment in segments)
        {
            var polyline = new Polyline3D(segment) 
            { 
                Layer = layer,
                IsClosed = false  // Ensure not closed
            };
            doc.Entities.Add(polyline);
        }
    }
    
    private double CalculateAverageSpacing(IList<SurveyPoint> points)
    {
        if (points.Count < 2) return 100.0;
        
        double totalDistance = 0;
        int count = 0;
        
        for (int i = 1; i < Math.Min(points.Count, 100); i++) // Sample first 100 points
        {
            var p1 = points[i - 1];
            var p2 = points[i];
            double dx = (p2.SmoothedEasting ?? p2.Easting) - (p1.SmoothedEasting ?? p1.Easting);
            double dy = (p2.SmoothedNorthing ?? p2.Northing) - (p1.SmoothedNorthing ?? p1.Northing);
            totalDistance += Math.Sqrt(dx * dx + dy * dy);
            count++;
        }
        
        return count > 0 ? totalDistance / count : 100.0;
    }
    
    /// <summary>
    /// Draw interval measure points
    /// </summary>
    private void DrawIntervalPoints(DxfDocument doc, IList<(double X, double Y, double Z, double Distance)> points, Layer layer)
    {
        foreach (var pt in points)
        {
            var point = new netDxf.Entities.Point(pt.X, pt.Y, 0) { Layer = layer };
            doc.Entities.Add(point);
        }
        
        // Also draw as polyline if there are enough points (not closed)
        if (points.Count > 1)
        {
            var vertices = points.Select(p => new netDxf.Vector3(p.X, p.Y, 0)).ToList();
            var polyline = new Polyline3D(vertices) 
            { 
                Layer = layer,
                IsClosed = false  // Ensure not closed
            };
            doc.Entities.Add(polyline);
        }
    }

    /// <summary>
    /// Export 3D polyline DXF file with depth exaggeration
    /// Uses X, Y for horizontal position and Z = CalculatedZ * -1 * Exaggeration
    /// </summary>
    public void Export3DPolyline(string filePath, IList<SurveyPoint> points, Project project, double depthExaggeration = 10.0)
    {
        var doc = new DxfDocument();

        // Create layers
        var trackLayer = CreateLayer(doc, "Survey_Track_3D", AciColor.Green);
        var seabedLayer = CreateLayer(doc, "Seabed_3D", AciColor.Blue);

        // Filter points with valid Z values
        var validPoints = points.Where(p => p.CalculatedZ.HasValue).ToList();
        
        if (validPoints.Count < 2)
            return;

        // Create 3D polyline with exaggerated depth
        // Z = CalculatedZ × -1 × Exaggeration (negative because depth goes down in CAD)
        var vertices3D = validPoints
            .Select(p => new netDxf.Vector3(
                p.X,  // Smoothed Easting
                p.Y,  // Smoothed Northing
                -p.CalculatedZ!.Value * depthExaggeration))  // Seabed depth (negative, exaggerated)
            .ToList();

        var polyline3D = new Polyline3D(vertices3D)
        {
            Layer = seabedLayer
        };
        doc.Entities.Add(polyline3D);

        // Also create 2D polyline at Z=0 for reference
        var vertices2D = validPoints
            .Select(p => new netDxf.Vector2(p.X, p.Y))
            .ToList();

        var polyline2D = new Polyline2D(vertices2D)
        {
            Layer = trackLayer
        };
        doc.Entities.Add(polyline2D);

        // Add info text
        DrawTitleBlock3D(doc, project, depthExaggeration);

        doc.Save(filePath);
    }

    private Layer CreateLayer(DxfDocument doc, string name, AciColor color)
    {
        var layer = new Layer(name)
        {
            Color = color
        };
        doc.Layers.Add(layer);
        return layer;
    }

    private void DrawRoute(DxfDocument doc, RouteData route, Layer layer)
    {
        foreach (var segment in route.Segments)
        {
            if (segment.IsStraight)
            {
                // Draw straight line
                var line = new Line(
                    new netDxf.Vector2(segment.StartEasting, segment.StartNorthing),
                    new netDxf.Vector2(segment.EndEasting, segment.EndNorthing))
                {
                    Layer = layer
                };
                doc.Entities.Add(line);
            }
            else
            {
                // Draw arc
                var (centerE, centerN) = segment.GetArcCenter();
                double radius = Math.Abs(segment.Radius);

                // Calculate start and end angles
                double startAngle = Math.Atan2(
                    segment.StartNorthing - centerN,
                    segment.StartEasting - centerE) * 180.0 / Math.PI;
                double endAngle = Math.Atan2(
                    segment.EndNorthing - centerN,
                    segment.EndEasting - centerE) * 180.0 / Math.PI;

                // netDxf Arc uses counter-clockwise angles from east axis
                var arc = new Arc(
                    new netDxf.Vector2(centerE, centerN),
                    radius,
                    segment.IsClockwise ? endAngle : startAngle,
                    segment.IsClockwise ? startAngle : endAngle)
                {
                    Layer = layer
                };
                doc.Entities.Add(arc);
            }
        }
    }

    private void DrawTrack(DxfDocument doc, IList<SurveyPoint> points, Layer layer)
    {
        if (points.Count < 2)
            return;
            
        // Calculate gap threshold (10x average spacing)
        var avgSpacing = CalculateAverageSpacing(points);
        double gapThreshold = avgSpacing * 10;
        
        // Split points into segments at gaps
        var segments = new List<List<netDxf.Vector2>>();
        var currentSegment = new List<netDxf.Vector2>();
        
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var vertex = new netDxf.Vector2(p.X, p.Y);
            
            if (i == 0)
            {
                currentSegment.Add(vertex);
            }
            else
            {
                var prevP = points[i - 1];
                double dx = p.X - prevP.X;
                double dy = p.Y - prevP.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                
                if (distance > gapThreshold && currentSegment.Count > 1)
                {
                    // Gap detected - save current segment and start new one
                    segments.Add(currentSegment);
                    currentSegment = new List<netDxf.Vector2>();
                }
                currentSegment.Add(vertex);
            }
        }
        
        // Add final segment
        if (currentSegment.Count > 1)
        {
            segments.Add(currentSegment);
        }
        
        // Create polylines for each segment (not closed)
        foreach (var segment in segments)
        {
            var polyline = new Polyline2D(segment)
            {
                Layer = layer,
                IsClosed = false
            };
            doc.Entities.Add(polyline);
        }

        // If 3D option is enabled, also create 3D polyline with depth
        if (_options.Include3DTrack)
        {
            var pointsWithZ = points.Where(p => p.CalculatedZ.HasValue).ToList();
            if (pointsWithZ.Count >= 2)
            {
                var track3DLayer = CreateLayer(doc, "Survey_Track_3D", AciColor.Blue);
                
                // Also split 3D by gaps
                var segments3D = new List<List<netDxf.Vector3>>();
                var currentSegment3D = new List<netDxf.Vector3>();
                
                for (int i = 0; i < pointsWithZ.Count; i++)
                {
                    var p = pointsWithZ[i];
                    var vertex = new netDxf.Vector3(p.X, p.Y, -p.CalculatedZ!.Value * _options.DepthExaggeration);
                    
                    if (i == 0)
                    {
                        currentSegment3D.Add(vertex);
                    }
                    else
                    {
                        var prevP = pointsWithZ[i - 1];
                        double dx = p.X - prevP.X;
                        double dy = p.Y - prevP.Y;
                        double distance = Math.Sqrt(dx * dx + dy * dy);
                        
                        if (distance > gapThreshold && currentSegment3D.Count > 1)
                        {
                            segments3D.Add(currentSegment3D);
                            currentSegment3D = new List<netDxf.Vector3>();
                        }
                        currentSegment3D.Add(vertex);
                    }
                }
                
                if (currentSegment3D.Count > 1)
                {
                    segments3D.Add(currentSegment3D);
                }
                
                foreach (var seg3D in segments3D)
                {
                    var polyline3D = new Polyline3D(seg3D)
                    {
                        Layer = track3DLayer,
                        IsClosed = false
                    };
                    doc.Entities.Add(polyline3D);
                }
            }
        }
    }

    private void DrawPoints(DxfDocument doc, IList<SurveyPoint> points, Layer layer)
    {
        // Draw points at regular intervals to avoid clutter
        int step = Math.Max(1, points.Count / _options.MaxPointMarkers);

        for (int i = 0; i < points.Count; i += step)
        {
            var point = points[i];
            
            // Draw point marker (small circle) using X, Y
            var circle = new Circle(
                new netDxf.Vector2(point.X, point.Y),
                _options.PointMarkerSize)
            {
                Layer = layer
            };
            doc.Entities.Add(circle);
        }
    }

    private void DrawKpLabels(DxfDocument doc, IList<SurveyPoint> points, Layer layer)
    {
        if (points.Count == 0 || !points.Any(p => p.Kp.HasValue))
            return;

        // Group points by KP intervals
        double interval = _options.KpLabelInterval;
        var labeledKps = new HashSet<int>();

        foreach (var point in points)
        {
            if (!point.Kp.HasValue)
                continue;

            int kpInterval = (int)(point.Kp.Value / interval);
            
            if (!labeledKps.Contains(kpInterval))
            {
                labeledKps.Add(kpInterval);
                double labelKp = kpInterval * interval;

                // Create label text
                string labelText = $"KP {labelKp:F3}";
                
                // Use X, Y coordinates
                var text = new Text(labelText, 
                    new netDxf.Vector2(point.X + _options.TextOffset, point.Y + _options.TextOffset),
                    _options.TextHeight)
                {
                    Layer = layer,
                    Rotation = 0
                };
                doc.Entities.Add(text);

                // Draw tick mark
                var tick = new Line(
                    new netDxf.Vector2(point.X - _options.TickSize, point.Y),
                    new netDxf.Vector2(point.X + _options.TickSize, point.Y))
                {
                    Layer = layer
                };
                doc.Entities.Add(tick);
            }
        }
    }

    private void DrawFixes(DxfDocument doc, IList<SurveyFix> fixes, Layer layer)
    {
        foreach (var fix in fixes)
        {
            // Draw cross marker
            double size = _options.FixMarkerSize;
            
            var line1 = new Line(
                new netDxf.Vector2(fix.Easting - size, fix.Northing - size),
                new netDxf.Vector2(fix.Easting + size, fix.Northing + size))
            {
                Layer = layer
            };
            doc.Entities.Add(line1);

            var line2 = new Line(
                new netDxf.Vector2(fix.Easting - size, fix.Northing + size),
                new netDxf.Vector2(fix.Easting + size, fix.Northing - size))
            {
                Layer = layer
            };
            doc.Entities.Add(line2);

            // Draw label
            if (!string.IsNullOrEmpty(fix.Name))
            {
                var text = new Text(fix.Name,
                    new netDxf.Vector2(fix.Easting + _options.TextOffset, fix.Northing + _options.TextOffset),
                    _options.TextHeight * 0.8)
                {
                    Layer = layer
                };
                doc.Entities.Add(text);
            }
        }
    }

    private void DrawTitleBlock(DxfDocument doc, Project project)
    {
        // This would typically use a block insert from a template
        // For now, create simple text annotations in a corner

        var infoLayer = new Layer("Title_Block") { Color = new AciColor(7) }; // White = color index 7
        doc.Layers.Add(infoLayer);

        // Position in lower-left (this would need adjustment based on actual extents)
        double x = 0;
        double y = -100; // Below origin
        double lineHeight = _options.TextHeight * 1.5;

        var lines = new[]
        {
            $"Project: {project.ProjectName}",
            $"Client: {project.ClientName}",
            $"Vessel: {project.VesselName}",
            $"Date: {DateTime.Now:yyyy-MM-dd}",
            $"Generated by Fathom OS Survey Listing"
        };

        foreach (var line in lines)
        {
            var text = new Text(line, new netDxf.Vector2(x, y), _options.TextHeight)
            {
                Layer = infoLayer
            };
            doc.Entities.Add(text);
            y -= lineHeight;
        }
    }

    private void DrawTitleBlock3D(DxfDocument doc, Project project, double depthExaggeration)
    {
        var infoLayer = new Layer("Info") { Color = new AciColor(7) };
        doc.Layers.Add(infoLayer);

        double x = 0;
        double y = -100;
        double lineHeight = 15;

        var lines = new[]
        {
            $"3D SEABED POLYLINE",
            $"Project: {project.ProjectName}",
            $"Depth Exaggeration: {depthExaggeration}x",
            $"Z = SeabedDepth × -1 × {depthExaggeration}",
            $"Date: {DateTime.Now:yyyy-MM-dd}",
            $"Generated by Fathom OS Survey Listing"
        };

        foreach (var line in lines)
        {
            var text = new Text(line, new netDxf.Vector2(x, y), 10)
            {
                Layer = infoLayer
            };
            doc.Entities.Add(text);
            y -= lineHeight;
        }
    }
}

/// <summary>
/// Options for DXF export
/// </summary>
public class DxfExportOptions
{
    public bool IncludeRoute { get; set; } = true;
    public bool IncludePoints { get; set; } = true;
    public bool IncludeKpLabels { get; set; } = true;
    public bool IncludeFixes { get; set; } = true;
    public bool IncludeTitleBlock { get; set; } = true;
    public bool Include3DTrack { get; set; } = false;
    
    public double KpLabelInterval { get; set; } = 1.0; // km
    public double TextHeight { get; set; } = 10.0;
    public double TextOffset { get; set; } = 15.0;
    public double PointMarkerSize { get; set; } = 5.0;
    public double FixMarkerSize { get; set; } = 10.0;
    public double TickSize { get; set; } = 5.0;
    public double DepthExaggeration { get; set; } = 10.0;
    public int MaxPointMarkers { get; set; } = 500;
}
