using netDxf;
using netDxf.Entities;
using FathomOS.Core.Models;

namespace FathomOS.Core.Parsers;

/// <summary>
/// Parses DXF files to extract field layout entities for visualization
/// </summary>
public class DxfLayoutParser
{
    /// <summary>
    /// Parse a DXF file and extract all entities for visualization
    /// </summary>
    public FieldLayout Parse(string filePath)
    {
        var layout = new FieldLayout
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath
        };

        // Check file extension
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".dwg")
        {
            throw new InvalidOperationException(
                "DWG files are not supported. Please convert to DXF format using AutoCAD, BricsCAD, or a DWG to DXF converter.");
        }
        
        if (ext != ".dxf")
        {
            throw new InvalidOperationException($"Unsupported file format: {ext}. Only DXF files are supported.");
        }

        try
        {
            var doc = DxfDocument.Load(filePath);
            if (doc == null)
                throw new InvalidOperationException($"Failed to load DXF file: {filePath}. The file may be corrupted or use an unsupported DXF version.");

            // Extract all entity types
            foreach (var entity in doc.Entities.All)
            {
                try
                {
                    ProcessEntity(entity, layout);
                }
                catch
                {
                    // Skip entities that fail to process
                }
            }

            // Calculate bounds
            layout.CalculateBounds();
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            throw new InvalidOperationException(
                $"Error reading DXF file: {ex.Message}\n\n" +
                "This may be due to:\n" +
                "• Unsupported DXF version (try saving as AutoCAD 2013 or earlier)\n" +
                "• Corrupted file\n" +
                "• DXF file created by incompatible software\n\n" +
                "Try re-saving the file from AutoCAD/BricsCAD as 'AutoCAD 2013 DXF (*.dxf)'");
        }

        return layout;
    }

    private void ProcessEntity(EntityObject entity, FieldLayout layout)
    {
        switch (entity)
        {
            case Line line:
                layout.Lines.Add(new LayoutLine
                {
                    StartX = line.StartPoint.X,
                    StartY = line.StartPoint.Y,
                    StartZ = line.StartPoint.Z,
                    EndX = line.EndPoint.X,
                    EndY = line.EndPoint.Y,
                    EndZ = line.EndPoint.Z,
                    Layer = line.Layer.Name,
                    Color = GetColorIndex(line)
                });
                break;

            case Polyline2D polyline2D:
                var poly2D = new LayoutPolyline { Layer = polyline2D.Layer.Name, Color = GetColorIndex(polyline2D) };
                foreach (var vertex in polyline2D.Vertexes)
                {
                    // Polyline2DVertex.Position returns Vector2
                    poly2D.Vertices.Add(new Point3D(vertex.Position.X, vertex.Position.Y, 0));
                }
                poly2D.IsClosed = polyline2D.IsClosed;
                layout.Polylines.Add(poly2D);
                break;

            case Polyline3D polyline3D:
                var poly3D = new LayoutPolyline { Layer = polyline3D.Layer.Name, Color = GetColorIndex(polyline3D), Is3D = true };
                foreach (var vertex in polyline3D.Vertexes)
                {
                    // Polyline3D.Vertexes contains Vector3 directly
                    poly3D.Vertices.Add(new Point3D(vertex.X, vertex.Y, vertex.Z));
                }
                layout.Polylines.Add(poly3D);
                break;

            case Circle circle:
                layout.Circles.Add(new LayoutCircle
                {
                    CenterX = circle.Center.X,
                    CenterY = circle.Center.Y,
                    CenterZ = circle.Center.Z,
                    Radius = circle.Radius,
                    Layer = circle.Layer.Name,
                    Color = GetColorIndex(circle)
                });
                break;

            case Arc arc:
                layout.Arcs.Add(new LayoutArc
                {
                    CenterX = arc.Center.X,
                    CenterY = arc.Center.Y,
                    CenterZ = arc.Center.Z,
                    Radius = arc.Radius,
                    StartAngle = arc.StartAngle,
                    EndAngle = arc.EndAngle,
                    Layer = arc.Layer.Name,
                    Color = GetColorIndex(arc)
                });
                break;

            case Text text:
                layout.Texts.Add(new LayoutText
                {
                    X = text.Position.X,
                    Y = text.Position.Y,
                    Z = text.Position.Z,
                    Content = text.Value,
                    Height = text.Height,
                    Rotation = text.Rotation,
                    Layer = text.Layer.Name,
                    Color = GetColorIndex(text)
                });
                break;

            case MText mtext:
                layout.Texts.Add(new LayoutText
                {
                    X = mtext.Position.X,
                    Y = mtext.Position.Y,
                    Z = mtext.Position.Z,
                    Content = mtext.PlainText(),
                    Height = mtext.Height,
                    Rotation = mtext.Rotation,
                    Layer = mtext.Layer.Name,
                    Color = GetColorIndex(mtext)
                });
                break;

            case Point point:
                layout.Points.Add(new LayoutPoint
                {
                    X = point.Position.X,
                    Y = point.Position.Y,
                    Z = point.Position.Z,
                    Layer = point.Layer.Name,
                    Color = GetColorIndex(point)
                });
                break;

            case Ellipse ellipse:
                layout.Ellipses.Add(new LayoutEllipse
                {
                    CenterX = ellipse.Center.X,
                    CenterY = ellipse.Center.Y,
                    CenterZ = ellipse.Center.Z,
                    MajorAxis = ellipse.MajorAxis,
                    MinorAxis = ellipse.MinorAxis,
                    Rotation = ellipse.Rotation,
                    Layer = ellipse.Layer.Name,
                    Color = GetColorIndex(ellipse)
                });
                break;
                
            // Handle other polyline types by checking type name
            default:
                // Try to handle LwPolyline and other types via reflection/type checking
                var typeName = entity.GetType().Name;
                if (typeName == "LwPolyline")
                {
                    ProcessLwPolyline(entity, layout);
                }
                else if (typeName == "Spline")
                {
                    ProcessSpline(entity, layout);
                }
                break;
        }
    }

    private void ProcessLwPolyline(EntityObject entity, FieldLayout layout)
    {
        try
        {
            // Use dynamic to access LwPolyline properties
            dynamic lwPoly = entity;
            var layoutPoly = new LayoutPolyline 
            { 
                Layer = entity.Layer.Name, 
                Color = GetColorIndex(entity) 
            };
            
            double elevation = (double)lwPoly.Elevation;
            foreach (var vertex in lwPoly.Vertexes)
            {
                var pos = vertex.Position;
                layoutPoly.Vertices.Add(new Point3D(pos.X, pos.Y, elevation));
            }
            layoutPoly.IsClosed = (bool)lwPoly.IsClosed;
            layout.Polylines.Add(layoutPoly);
        }
        catch
        {
            // Skip if we can't process it
        }
    }

    private void ProcessSpline(EntityObject entity, FieldLayout layout)
    {
        try
        {
            // Use dynamic to access Spline and convert to polyline
            dynamic spline = entity;
            var layoutPoly = new LayoutPolyline 
            { 
                Layer = entity.Layer.Name, 
                Color = GetColorIndex(entity) 
            };
            
            // Get control points as approximation
            foreach (var pt in spline.ControlPoints)
            {
                layoutPoly.Vertices.Add(new Point3D(pt.X, pt.Y, pt.Z));
            }
            layout.Polylines.Add(layoutPoly);
        }
        catch
        {
            // Skip if we can't process it
        }
    }

    private int GetColorIndex(EntityObject entity)
    {
        if (entity.Color.IsByLayer)
            return entity.Layer.Color.Index;
        return entity.Color.Index;
    }
}

/// <summary>
/// Represents a complete field layout from DXF
/// </summary>
public class FieldLayout
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    
    public List<LayoutLine> Lines { get; set; } = new();
    public List<LayoutPolyline> Polylines { get; set; } = new();
    public List<LayoutCircle> Circles { get; set; } = new();
    public List<LayoutArc> Arcs { get; set; } = new();
    public List<LayoutText> Texts { get; set; } = new();
    public List<LayoutPoint> Points { get; set; } = new();
    public List<LayoutEllipse> Ellipses { get; set; } = new();

    // Bounds
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }
    public double MinZ { get; set; }
    public double MaxZ { get; set; }

    public int TotalEntities => Lines.Count + Polylines.Count + Circles.Count + 
                                 Arcs.Count + Texts.Count + Points.Count + Ellipses.Count;

    public void CalculateBounds()
    {
        var allX = new List<double>();
        var allY = new List<double>();
        var allZ = new List<double>();

        foreach (var line in Lines)
        {
            allX.AddRange(new[] { line.StartX, line.EndX });
            allY.AddRange(new[] { line.StartY, line.EndY });
            allZ.AddRange(new[] { line.StartZ, line.EndZ });
        }

        foreach (var poly in Polylines)
        {
            allX.AddRange(poly.Vertices.Select(v => v.X));
            allY.AddRange(poly.Vertices.Select(v => v.Y));
            allZ.AddRange(poly.Vertices.Select(v => v.Z));
        }

        foreach (var circle in Circles)
        {
            allX.AddRange(new[] { circle.CenterX - circle.Radius, circle.CenterX + circle.Radius });
            allY.AddRange(new[] { circle.CenterY - circle.Radius, circle.CenterY + circle.Radius });
            allZ.Add(circle.CenterZ);
        }

        foreach (var text in Texts)
        {
            allX.Add(text.X);
            allY.Add(text.Y);
            allZ.Add(text.Z);
        }

        foreach (var point in Points)
        {
            allX.Add(point.X);
            allY.Add(point.Y);
            allZ.Add(point.Z);
        }

        if (allX.Count > 0)
        {
            MinX = allX.Min();
            MaxX = allX.Max();
            MinY = allY.Min();
            MaxY = allY.Max();
            MinZ = allZ.Min();
            MaxZ = allZ.Max();
        }
    }
}

public class Point3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Point3D() { }
    public Point3D(double x, double y, double z) { X = x; Y = y; Z = z; }
}

public class LayoutLine
{
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double StartZ { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
    public double EndZ { get; set; }
    public string Layer { get; set; } = string.Empty;
    public int Color { get; set; }
}

public class LayoutPolyline
{
    public List<Point3D> Vertices { get; set; } = new();
    public bool IsClosed { get; set; }
    public bool Is3D { get; set; }
    public string Layer { get; set; } = string.Empty;
    public int Color { get; set; }
}

public class LayoutCircle
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double CenterZ { get; set; }
    public double Radius { get; set; }
    public string Layer { get; set; } = string.Empty;
    public int Color { get; set; }
}

public class LayoutArc
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double CenterZ { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }
    public string Layer { get; set; } = string.Empty;
    public int Color { get; set; }
}

public class LayoutText
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public string Content { get; set; } = string.Empty;
    public double Height { get; set; }
    public double Rotation { get; set; }
    public string Layer { get; set; } = string.Empty;
    public int Color { get; set; }
}

public class LayoutPoint
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public string Layer { get; set; } = string.Empty;
    public int Color { get; set; }
}

public class LayoutEllipse
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double CenterZ { get; set; }
    public double MajorAxis { get; set; }
    public double MinorAxis { get; set; }
    public double Rotation { get; set; }
    public string Layer { get; set; } = string.Empty;
    public int Color { get; set; }
}
