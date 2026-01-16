namespace FathomOS.Modules.TreeInclination.Export;

using netDxf;
using netDxf.Entities;
using netDxf.Tables;
using FathomOS.Modules.TreeInclination.Models;

/// <summary>DXF export for CAD integration using netDxf</summary>
public class DxfExporter
{
    private const string LAYER_STRUCTURE = "STRUCTURE";
    private const string LAYER_CORNERS = "CORNERS";
    private const string LAYER_LABELS = "LABELS";
    private const string LAYER_DEPTHS = "DEPTHS";
    private const string LAYER_VECTOR = "VECTOR";
    private const string LAYER_ANNOTATIONS = "ANNOTATIONS";

    public void Export(string filePath, InclinationProject project)
    {
        var doc = new DxfDocument();

        CreateLayers(doc);

        var corners = project.Corners.Where(c => !c.IsClosurePoint).OrderBy(c => c.Index).ToList();
        if (corners.Count < 3) return;

        DrawStructureOutline(doc, corners);
        DrawCornerPoints(doc, corners);
        DrawCornerLabels(doc, corners);
        DrawDepthLabels(doc, corners);

        if (project.Result != null)
        {
            DrawInclinationVector(doc, corners, project.Result);
            DrawResultAnnotations(doc, corners, project);
        }

        DrawTitleBlock(doc, project, corners);

        doc.Save(filePath);
    }

    private void CreateLayers(DxfDocument doc)
    {
        doc.Layers.Add(new Layer(LAYER_STRUCTURE) { Color = AciColor.Blue, Lineweight = Lineweight.W35 });
        doc.Layers.Add(new Layer(LAYER_CORNERS) { Color = AciColor.Cyan, Lineweight = Lineweight.W50 });
        doc.Layers.Add(new Layer(LAYER_LABELS) { Color = AciColor.Green });
        doc.Layers.Add(new Layer(LAYER_DEPTHS) { Color = AciColor.Magenta });
        doc.Layers.Add(new Layer(LAYER_VECTOR) { Color = AciColor.Red, Lineweight = Lineweight.W50 });
        doc.Layers.Add(new Layer(LAYER_ANNOTATIONS) { Color = AciColor.Default });
    }

    private void DrawStructureOutline(DxfDocument doc, List<CornerMeasurement> corners)
    {
        // Create polyline for structure outline
        var vertices = corners.Select(c => new Vector2(c.X, c.Y)).ToList();
        var polyline = new Polyline2D(vertices, true) { Layer = doc.Layers[LAYER_STRUCTURE] };
        doc.Entities.Add(polyline);

        // Draw diagonals for 4-point structures
        if (corners.Count == 4)
        {
            doc.Entities.Add(new Line(
                new Vector2(corners[0].X, corners[0].Y),
                new Vector2(corners[2].X, corners[2].Y))
            { Layer = doc.Layers[LAYER_STRUCTURE], Linetype = Linetype.Dashed });

            doc.Entities.Add(new Line(
                new Vector2(corners[1].X, corners[1].Y),
                new Vector2(corners[3].X, corners[3].Y))
            { Layer = doc.Layers[LAYER_STRUCTURE], Linetype = Linetype.Dashed });
        }
    }

    private void DrawCornerPoints(DxfDocument doc, List<CornerMeasurement> corners)
    {
        double size = GetTextHeight(corners) * 0.5;

        foreach (var corner in corners)
        {
            // Cross marker
            doc.Entities.Add(new Line(
                new Vector2(corner.X - size, corner.Y),
                new Vector2(corner.X + size, corner.Y))
            { Layer = doc.Layers[LAYER_CORNERS] });

            doc.Entities.Add(new Line(
                new Vector2(corner.X, corner.Y - size),
                new Vector2(corner.X, corner.Y + size))
            { Layer = doc.Layers[LAYER_CORNERS] });

            // Circle marker
            doc.Entities.Add(new Circle(new Vector2(corner.X, corner.Y), size * 0.8)
            { Layer = doc.Layers[LAYER_CORNERS] });
        }
    }

    private void DrawCornerLabels(DxfDocument doc, List<CornerMeasurement> corners)
    {
        double textHeight = GetTextHeight(corners);

        foreach (var corner in corners)
        {
            var offset = GetLabelOffset(corner, corners, textHeight);

            var text = new Text(corner.Name, new Vector2(corner.X + offset.X, corner.Y + offset.Y), textHeight)
            {
                Layer = doc.Layers[LAYER_LABELS],
                Alignment = TextAlignment.MiddleCenter
            };
            doc.Entities.Add(text);
        }
    }

    private void DrawDepthLabels(DxfDocument doc, List<CornerMeasurement> corners)
    {
        double textHeight = GetTextHeight(corners) * 0.8;

        foreach (var corner in corners)
        {
            var offset = GetLabelOffset(corner, corners, textHeight);

            var text = new Text($"Z={corner.CorrectedDepth:F3}m",
                new Vector2(corner.X + offset.X, corner.Y + offset.Y - textHeight * 1.5), textHeight)
            {
                Layer = doc.Layers[LAYER_DEPTHS],
                Alignment = TextAlignment.MiddleCenter
            };
            doc.Entities.Add(text);
        }
    }

    private void DrawInclinationVector(DxfDocument doc, List<CornerMeasurement> corners, InclinationResult result)
    {
        double cx = corners.Average(c => c.X);
        double cy = corners.Average(c => c.Y);

        double scale = Math.Max(
            corners.Max(c => c.X) - corners.Min(c => c.X),
            corners.Max(c => c.Y) - corners.Min(c => c.Y)) * 0.3;

        double headingRad = result.InclinationHeading * Math.PI / 180.0;
        double dx = scale * Math.Sin(headingRad);
        double dy = scale * Math.Cos(headingRad);

        // Main vector line
        doc.Entities.Add(new Line(new Vector2(cx, cy), new Vector2(cx + dx, cy + dy))
        { Layer = doc.Layers[LAYER_VECTOR] });

        // Arrow head
        double arrowSize = scale * 0.15;
        double arrowAngle = 25 * Math.PI / 180.0;

        double angle1 = headingRad + Math.PI - arrowAngle;
        double angle2 = headingRad + Math.PI + arrowAngle;

        doc.Entities.Add(new Line(
            new Vector2(cx + dx, cy + dy),
            new Vector2(cx + dx + arrowSize * Math.Sin(angle1), cy + dy + arrowSize * Math.Cos(angle1)))
        { Layer = doc.Layers[LAYER_VECTOR] });

        doc.Entities.Add(new Line(
            new Vector2(cx + dx, cy + dy),
            new Vector2(cx + dx + arrowSize * Math.Sin(angle2), cy + dy + arrowSize * Math.Cos(angle2)))
        { Layer = doc.Layers[LAYER_VECTOR] });

        // Vector label
        double labelX = cx + dx * 1.1;
        double labelY = cy + dy * 1.1;
        double textHeight = GetTextHeight(corners) * 0.7;

        var label = new Text($"{result.TotalInclination:F3}° @ {result.InclinationHeading:F1}°",
            new Vector2(labelX, labelY), textHeight)
        {
            Layer = doc.Layers[LAYER_VECTOR],
            Alignment = TextAlignment.MiddleLeft
        };
        doc.Entities.Add(label);
    }

    private void DrawResultAnnotations(DxfDocument doc, List<CornerMeasurement> corners, InclinationProject project)
    {
        double minX = corners.Min(c => c.X);
        double maxY = corners.Max(c => c.Y);
        double textHeight = GetTextHeight(corners) * 0.6;
        double lineSpacing = textHeight * 1.5;

        double x = minX;
        double y = maxY + textHeight * 3;

        var result = project.Result!;

        var annotations = new[]
        {
            $"Inclination: {result.TotalInclination:F3}° ({result.InclinationStatus})",
            $"Heading: {result.InclinationHeading:F2}° {result.HeadingDescription}",
            $"Pitch: {result.AveragePitch:F3}°  Roll: {result.AverageRoll:F3}°",
            $"Misclosure: {result.Misclosure:F3}m ({result.MisclosureStatus})",
            $"Out of Plane: {result.OutOfPlane:F3}m ({result.OutOfPlaneStatus})",
            $"Method: {result.CalculationMethod}"
        };

        foreach (var annotation in annotations)
        {
            doc.Entities.Add(new Text(annotation, new Vector2(x, y), textHeight)
            {
                Layer = doc.Layers[LAYER_ANNOTATIONS],
                Alignment = TextAlignment.BaselineLeft
            });
            y += lineSpacing;
        }
    }

    private void DrawTitleBlock(DxfDocument doc, InclinationProject project, List<CornerMeasurement> corners)
    {
        double minX = corners.Min(c => c.X);
        double minY = corners.Min(c => c.Y);
        double textHeight = GetTextHeight(corners) * 0.5;
        double lineSpacing = textHeight * 1.3;

        double x = minX;
        double y = minY - textHeight * 4;

        var lines = new[]
        {
            $"Project: {project.ProjectName}",
            $"Client: {project.ClientName}",
            $"Structure: {project.StructureName}",
            $"Date: {project.SurveyDate:yyyy-MM-dd}",
            $"Surveyor: {project.SurveyorName}"
        };

        foreach (var line in lines)
        {
            doc.Entities.Add(new Text(line, new Vector2(x, y), textHeight)
            {
                Layer = doc.Layers[LAYER_ANNOTATIONS],
                Alignment = TextAlignment.BaselineLeft
            });
            y -= lineSpacing;
        }
    }

    private double GetTextHeight(List<CornerMeasurement> corners)
    {
        double sizeX = corners.Max(c => c.X) - corners.Min(c => c.X);
        double sizeY = corners.Max(c => c.Y) - corners.Min(c => c.Y);
        double maxSize = Math.Max(sizeX, sizeY);
        return Math.Max(maxSize * 0.03, 0.5);
    }

    private (double X, double Y) GetLabelOffset(CornerMeasurement corner,
        List<CornerMeasurement> corners, double textHeight)
    {
        double cx = corners.Average(c => c.X);
        double cy = corners.Average(c => c.Y);

        double dx = corner.X - cx;
        double dy = corner.Y - cy;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist < 0.001) return (textHeight, textHeight);

        double offsetDist = textHeight * 2;
        return (dx / dist * offsetDist, dy / dist * offsetDist);
    }
}
