using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for exporting USBL verification data to DXF format with Plan View
/// </summary>
public class DxfExportService
{
    private readonly AdvancedStatisticsService _statsService = new();
    
    /// <summary>
    /// Export project data and results to a DXF file (basic export)
    /// </summary>
    public void Export(string filePath, UsblVerificationProject project, VerificationResults results)
    {
        ExportPlanView(filePath, project, results, new DxfPlanViewOptions());
    }
    
    /// <summary>
    /// Export comprehensive Plan View DXF with all visualization options
    /// </summary>
    public void ExportPlanView(string filePath, UsblVerificationProject project, 
        VerificationResults results, DxfPlanViewOptions options)
    {
        using var writer = new StreamWriter(filePath);
        
        // Calculate bounds and scale
        var allPoints = GetAllPoints(project);
        var bounds = CalculateBounds(allPoints, results?.SpinMeanPosition, project.Tolerance);
        
        // DXF Header with extents
        WriteHeader(writer, bounds);
        
        // Tables section with all layers
        WriteTables(writer, options);
        
        // Entities section
        writer.WriteLine("0");
        writer.WriteLine("SECTION");
        writer.WriteLine("2");
        writer.WriteLine("ENTITIES");
        
        // Draw coordinate grid
        if (options.ShowGrid)
        {
            DrawCoordinateGrid(writer, bounds, options.GridSpacing);
        }
        
        // Draw tolerance circles (multiple radii)
        if (results?.SpinMeanPosition != null)
        {
            var cx = results.SpinMeanPosition.Easting;
            var cy = results.SpinMeanPosition.Northing;
            
            // Main tolerance circle
            DrawCircle(writer, cx, cy, project.Tolerance, "TOLERANCE_MAIN", 2.0);
            
            // Additional tolerance rings
            if (options.ShowMultipleToleranceRings)
            {
                DrawCircle(writer, cx, cy, project.Tolerance * 0.5, "TOLERANCE_INNER", 0.5);
                DrawCircle(writer, cx, cy, project.Tolerance * 1.5, "TOLERANCE_OUTER", 0.5);
                DrawCircle(writer, cx, cy, project.Tolerance * 2.0, "TOLERANCE_2X", 0.5);
            }
            
            // CEP and R95 circles
            if (options.ShowStatisticalCircles)
            {
                var allObs = project.AllSpinTests.SelectMany(s => s.Observations.Where(o => !o.IsExcluded)).ToList();
                if (allObs.Count > 3)
                {
                    var stats = _statsService.CalculateRadialStatistics(allObs, "All Spin Data");
                    
                    // CEP circle (50%)
                    DrawCircle(writer, cx, cy, stats.CEP, "STATS_CEP", 0.5);
                    DrawText(writer, cx + stats.CEP + 0.5, cy, $"CEP ({stats.CEP:F3}m)", "STATS_LABELS", 0.8);
                    
                    // R95 circle
                    DrawCircle(writer, cx, cy, stats.R95, "STATS_R95", 0.5);
                    DrawText(writer, cx + stats.R95 + 0.5, cy, $"R95 ({stats.R95:F3}m)", "STATS_LABELS", 0.8);
                    
                    // 2DRMS circle
                    DrawCircle(writer, cx, cy, stats.TwoDRMS, "STATS_2DRMS", 0.5);
                }
            }
            
            // Confidence ellipse
            if (options.ShowConfidenceEllipse)
            {
                var allObs = project.AllSpinTests.SelectMany(s => s.Observations.Where(o => !o.IsExcluded)).ToList();
                if (allObs.Count > 3)
                {
                    var ellipse = _statsService.CalculateConfidenceEllipse(allObs);
                    DrawEllipse(writer, cx, cy, ellipse.SemiMajor, ellipse.SemiMinor, 
                        ellipse.Rotation, "CONFIDENCE_ELLIPSE");
                }
            }
            
            // Mean position marker
            DrawCrossMarker(writer, cx, cy, 0.5, "MEAN_POSITION");
            
            // Per-heading mean markers
            if (options.ShowPerHeadingMeans)
            {
                foreach (var spin in project.AllSpinTests)
                {
                    var validObs = spin.Observations.Where(o => !o.IsExcluded).ToList();
                    if (validObs.Count > 0)
                    {
                        var meanE = validObs.Average(o => o.TransponderEasting);
                        var meanN = validObs.Average(o => o.TransponderNorthing);
                        var layerName = $"SPIN_{spin.NominalHeading:000}_MEAN";
                        DrawDiamondMarker(writer, meanE, meanN, 0.3, layerName);
                    }
                }
            }
        }
        
        // Draw spin test points with color by heading
        foreach (var spin in project.AllSpinTests)
        {
            var layerName = $"SPIN_{spin.NominalHeading:000}";
            foreach (var obs in spin.Observations.Where(o => !o.IsExcluded))
            {
                if (options.UseSymbolsByHeading)
                {
                    DrawHeadingSymbol(writer, obs.TransponderEasting, obs.TransponderNorthing, 
                        (int)spin.NominalHeading, layerName, 0.15);
                }
                else
                {
                    DrawPoint(writer, obs.TransponderEasting, obs.TransponderNorthing, layerName);
                }
            }
            
            // Draw excluded points differently
            if (options.ShowExcludedPoints)
            {
                foreach (var obs in spin.Observations.Where(o => o.IsExcluded))
                {
                    DrawXMarker(writer, obs.TransponderEasting, obs.TransponderNorthing, 0.2, "EXCLUDED_POINTS");
                }
            }
        }
        
        // Draw transit test points
        DrawTransitPoints(writer, project.Transit1.Observations, "TRANSIT_1", options);
        DrawTransitPoints(writer, project.Transit2.Observations, "TRANSIT_2", options);
        
        // Draw transit lines (connecting the points)
        if (options.ShowTransitLines)
        {
            DrawTransitLine(writer, project.Transit1.Observations, "TRANSIT_1_LINE");
            DrawTransitLine(writer, project.Transit2.Observations, "TRANSIT_2_LINE");
        }
        
        // Draw vessel track
        if (options.ShowVesselTrack)
        {
            DrawVesselTrack(writer, project);
        }
        
        // Labels and annotations
        if (results?.SpinMeanPosition != null)
        {
            var labelX = bounds.MaxX - (bounds.MaxX - bounds.MinX) * 0.02;
            var labelY = bounds.MaxY - 2;
            
            DrawText(writer, labelX, labelY, $"USBL Verification - {project.ProjectName}", "TITLE", 2.0);
            DrawText(writer, labelX, labelY - 3, $"Vessel: {project.VesselName}", "LABELS", 1.2);
            DrawText(writer, labelX, labelY - 5, $"Date: {project.SurveyDate:yyyy-MM-dd}", "LABELS", 1.2);
            
            if (options.ShowCoordinateLabels)
            {
                DrawText(writer, labelX, labelY - 8, $"Mean Position:", "LABELS", 1.0);
                DrawText(writer, labelX, labelY - 9.5, $"E: {results.SpinMeanPosition.Easting:F3}", "LABELS", 1.0);
                DrawText(writer, labelX, labelY - 11, $"N: {results.SpinMeanPosition.Northing:F3}", "LABELS", 1.0);
                DrawText(writer, labelX, labelY - 12.5, $"Depth: {results.SpinMeanPosition.Depth:F2}m", "LABELS", 1.0);
            }
            
            DrawText(writer, labelX, labelY - 15, $"Tolerance: {project.Tolerance:F2}m", "LABELS", 1.0);
            DrawText(writer, labelX, labelY - 16.5, $"Result: {(results.OverallPass ? "PASS" : "FAIL")}", 
                results.OverallPass ? "PASS_TEXT" : "FAIL_TEXT", 1.5);
        }
        
        // Draw legend
        if (options.ShowLegend)
        {
            DrawLegend(writer, bounds, project);
        }
        
        // Draw scale bar
        if (options.ShowScaleBar)
        {
            DrawScaleBar(writer, bounds);
        }
        
        // Draw north arrow
        if (options.ShowNorthArrow)
        {
            DrawNorthArrow(writer, bounds);
        }
        
        // End entities
        writer.WriteLine("0");
        writer.WriteLine("ENDSEC");
        
        // End of file
        writer.WriteLine("0");
        writer.WriteLine("EOF");
    }
    
    #region Helper Methods
    
    private List<(double E, double N)> GetAllPoints(UsblVerificationProject project)
    {
        var points = new List<(double E, double N)>();
        
        foreach (var spin in project.AllSpinTests)
        {
            foreach (var obs in spin.Observations)
            {
                points.Add((obs.TransponderEasting, obs.TransponderNorthing));
            }
        }
        
        foreach (var obs in project.Transit1.Observations)
            points.Add((obs.TransponderEasting, obs.TransponderNorthing));
        foreach (var obs in project.Transit2.Observations)
            points.Add((obs.TransponderEasting, obs.TransponderNorthing));
        
        return points;
    }
    
    private DxfBounds CalculateBounds(List<(double E, double N)> points, 
        TransponderPosition? meanPos, double tolerance)
    {
        if (points.Count == 0 && meanPos == null)
        {
            return new DxfBounds { MinX = 0, MaxX = 100, MinY = 0, MaxY = 100 };
        }
        
        double minX = points.Count > 0 ? points.Min(p => p.E) : meanPos!.Easting;
        double maxX = points.Count > 0 ? points.Max(p => p.E) : meanPos!.Easting;
        double minY = points.Count > 0 ? points.Min(p => p.N) : meanPos!.Northing;
        double maxY = points.Count > 0 ? points.Max(p => p.N) : meanPos!.Northing;
        
        // Expand by tolerance and margin
        double margin = Math.Max(tolerance * 3, 10);
        
        return new DxfBounds
        {
            MinX = minX - margin,
            MaxX = maxX + margin,
            MinY = minY - margin,
            MaxY = maxY + margin
        };
    }
    
    #endregion
    
    #region Drawing Methods
    
    private void WriteHeader(StreamWriter writer, DxfBounds bounds)
    {
        writer.WriteLine("0");
        writer.WriteLine("SECTION");
        writer.WriteLine("2");
        writer.WriteLine("HEADER");
        
        // AutoCAD version
        writer.WriteLine("9");
        writer.WriteLine("$ACADVER");
        writer.WriteLine("1");
        writer.WriteLine("AC1015"); // AutoCAD 2000 format
        
        // Units (meters)
        writer.WriteLine("9");
        writer.WriteLine("$INSUNITS");
        writer.WriteLine("70");
        writer.WriteLine("6");
        
        // Drawing extents
        writer.WriteLine("9");
        writer.WriteLine("$EXTMIN");
        writer.WriteLine("10");
        writer.WriteLine(bounds.MinX.ToString("F6"));
        writer.WriteLine("20");
        writer.WriteLine(bounds.MinY.ToString("F6"));
        writer.WriteLine("30");
        writer.WriteLine("0.0");
        
        writer.WriteLine("9");
        writer.WriteLine("$EXTMAX");
        writer.WriteLine("10");
        writer.WriteLine(bounds.MaxX.ToString("F6"));
        writer.WriteLine("20");
        writer.WriteLine(bounds.MaxY.ToString("F6"));
        writer.WriteLine("30");
        writer.WriteLine("0.0");
        
        writer.WriteLine("0");
        writer.WriteLine("ENDSEC");
    }
    
    private void WriteTables(StreamWriter writer, DxfPlanViewOptions options)
    {
        writer.WriteLine("0");
        writer.WriteLine("SECTION");
        writer.WriteLine("2");
        writer.WriteLine("TABLES");
        
        // Layer table
        writer.WriteLine("0");
        writer.WriteLine("TABLE");
        writer.WriteLine("2");
        writer.WriteLine("LAYER");
        writer.WriteLine("70");
        writer.WriteLine("30");
        
        // Define layers with colors (ACI color codes)
        WriteLayer(writer, "0", 7, "CONTINUOUS");
        WriteLayer(writer, "GRID", 8, "CONTINUOUS");
        WriteLayer(writer, "TOLERANCE_MAIN", 3, "CONTINUOUS");     // Green
        WriteLayer(writer, "TOLERANCE_INNER", 92, "CONTINUOUS");   // Light green
        WriteLayer(writer, "TOLERANCE_OUTER", 93, "CONTINUOUS");   // Darker green
        WriteLayer(writer, "TOLERANCE_2X", 94, "CONTINUOUS");
        WriteLayer(writer, "STATS_CEP", 4, "CONTINUOUS");          // Cyan
        WriteLayer(writer, "STATS_R95", 5, "CONTINUOUS");          // Blue
        WriteLayer(writer, "STATS_2DRMS", 140, "CONTINUOUS");      // Light blue
        WriteLayer(writer, "STATS_LABELS", 8, "CONTINUOUS");
        WriteLayer(writer, "CONFIDENCE_ELLIPSE", 6, "CONTINUOUS"); // Magenta
        WriteLayer(writer, "MEAN_POSITION", 1, "CONTINUOUS");      // Red
        WriteLayer(writer, "SPIN_000", 1, "CONTINUOUS");           // Red
        WriteLayer(writer, "SPIN_000_MEAN", 11, "CONTINUOUS");
        WriteLayer(writer, "SPIN_090", 3, "CONTINUOUS");           // Green
        WriteLayer(writer, "SPIN_090_MEAN", 83, "CONTINUOUS");
        WriteLayer(writer, "SPIN_180", 5, "CONTINUOUS");           // Blue
        WriteLayer(writer, "SPIN_180_MEAN", 145, "CONTINUOUS");
        WriteLayer(writer, "SPIN_270", 6, "CONTINUOUS");           // Magenta
        WriteLayer(writer, "SPIN_270_MEAN", 206, "CONTINUOUS");
        WriteLayer(writer, "TRANSIT_1", 4, "CONTINUOUS");          // Cyan
        WriteLayer(writer, "TRANSIT_1_LINE", 4, "CONTINUOUS");
        WriteLayer(writer, "TRANSIT_2", 40, "CONTINUOUS");         // Orange
        WriteLayer(writer, "TRANSIT_2_LINE", 40, "CONTINUOUS");
        WriteLayer(writer, "EXCLUDED_POINTS", 8, "CONTINUOUS");
        WriteLayer(writer, "VESSEL_TRACK", 251, "CONTINUOUS");     // Gray
        WriteLayer(writer, "LABELS", 7, "CONTINUOUS");
        WriteLayer(writer, "TITLE", 7, "CONTINUOUS");
        WriteLayer(writer, "LEGEND", 7, "CONTINUOUS");
        WriteLayer(writer, "SCALE_BAR", 7, "CONTINUOUS");
        WriteLayer(writer, "NORTH_ARROW", 7, "CONTINUOUS");
        WriteLayer(writer, "PASS_TEXT", 3, "CONTINUOUS");
        WriteLayer(writer, "FAIL_TEXT", 1, "CONTINUOUS");
        
        writer.WriteLine("0");
        writer.WriteLine("ENDTAB");
        
        writer.WriteLine("0");
        writer.WriteLine("ENDSEC");
    }
    
    private void WriteLayer(StreamWriter writer, string name, int color, string lineType)
    {
        writer.WriteLine("0");
        writer.WriteLine("LAYER");
        writer.WriteLine("2");
        writer.WriteLine(name);
        writer.WriteLine("70");
        writer.WriteLine("0");
        writer.WriteLine("62");
        writer.WriteLine(color.ToString());
        writer.WriteLine("6");
        writer.WriteLine(lineType);
    }
    
    private void DrawCircle(StreamWriter writer, double cx, double cy, double radius, string layer, double lineWidth = 0)
    {
        writer.WriteLine("0");
        writer.WriteLine("CIRCLE");
        writer.WriteLine("8");
        writer.WriteLine(layer);
        writer.WriteLine("10");
        writer.WriteLine(cx.ToString("F6"));
        writer.WriteLine("20");
        writer.WriteLine(cy.ToString("F6"));
        writer.WriteLine("30");
        writer.WriteLine("0.0");
        writer.WriteLine("40");
        writer.WriteLine(radius.ToString("F6"));
    }
    
    private void DrawEllipse(StreamWriter writer, double cx, double cy, 
        double semiMajor, double semiMinor, double rotationDeg, string layer)
    {
        // Convert rotation to radians
        double rotRad = rotationDeg * Math.PI / 180.0;
        
        // Major axis endpoint relative to center
        double majorEndX = semiMajor * Math.Cos(rotRad);
        double majorEndY = semiMajor * Math.Sin(rotRad);
        
        // Ratio of minor to major axis
        double ratio = semiMinor / semiMajor;
        
        writer.WriteLine("0");
        writer.WriteLine("ELLIPSE");
        writer.WriteLine("8");
        writer.WriteLine(layer);
        // Center point
        writer.WriteLine("10");
        writer.WriteLine(cx.ToString("F6"));
        writer.WriteLine("20");
        writer.WriteLine(cy.ToString("F6"));
        writer.WriteLine("30");
        writer.WriteLine("0.0");
        // Major axis endpoint
        writer.WriteLine("11");
        writer.WriteLine(majorEndX.ToString("F6"));
        writer.WriteLine("21");
        writer.WriteLine(majorEndY.ToString("F6"));
        writer.WriteLine("31");
        writer.WriteLine("0.0");
        // Ratio
        writer.WriteLine("40");
        writer.WriteLine(ratio.ToString("F6"));
        // Start parameter (0 = full ellipse start)
        writer.WriteLine("41");
        writer.WriteLine("0.0");
        // End parameter (2*PI = full ellipse)
        writer.WriteLine("42");
        writer.WriteLine((2 * Math.PI).ToString("F6"));
    }
    
    private void DrawPoint(StreamWriter writer, double x, double y, string layer)
    {
        writer.WriteLine("0");
        writer.WriteLine("POINT");
        writer.WriteLine("8");
        writer.WriteLine(layer);
        writer.WriteLine("10");
        writer.WriteLine(x.ToString("F6"));
        writer.WriteLine("20");
        writer.WriteLine(y.ToString("F6"));
        writer.WriteLine("30");
        writer.WriteLine("0.0");
    }
    
    private void DrawLine(StreamWriter writer, double x1, double y1, double x2, double y2, string layer)
    {
        writer.WriteLine("0");
        writer.WriteLine("LINE");
        writer.WriteLine("8");
        writer.WriteLine(layer);
        writer.WriteLine("10");
        writer.WriteLine(x1.ToString("F6"));
        writer.WriteLine("20");
        writer.WriteLine(y1.ToString("F6"));
        writer.WriteLine("30");
        writer.WriteLine("0.0");
        writer.WriteLine("11");
        writer.WriteLine(x2.ToString("F6"));
        writer.WriteLine("21");
        writer.WriteLine(y2.ToString("F6"));
        writer.WriteLine("31");
        writer.WriteLine("0.0");
    }
    
    private void DrawCrossMarker(StreamWriter writer, double x, double y, double size, string layer)
    {
        DrawLine(writer, x - size, y, x + size, y, layer);
        DrawLine(writer, x, y - size, x, y + size, layer);
    }
    
    private void DrawXMarker(StreamWriter writer, double x, double y, double size, string layer)
    {
        DrawLine(writer, x - size, y - size, x + size, y + size, layer);
        DrawLine(writer, x - size, y + size, x + size, y - size, layer);
    }
    
    private void DrawDiamondMarker(StreamWriter writer, double x, double y, double size, string layer)
    {
        DrawLine(writer, x, y - size, x + size, y, layer);
        DrawLine(writer, x + size, y, x, y + size, layer);
        DrawLine(writer, x, y + size, x - size, y, layer);
        DrawLine(writer, x - size, y, x, y - size, layer);
    }
    
    private void DrawHeadingSymbol(StreamWriter writer, double x, double y, int heading, string layer, double size)
    {
        // Different symbols for different headings
        switch (heading)
        {
            case 0:   // Circle
                DrawCircle(writer, x, y, size, layer);
                break;
            case 90:  // Square
                DrawLine(writer, x - size, y - size, x + size, y - size, layer);
                DrawLine(writer, x + size, y - size, x + size, y + size, layer);
                DrawLine(writer, x + size, y + size, x - size, y + size, layer);
                DrawLine(writer, x - size, y + size, x - size, y - size, layer);
                break;
            case 180: // Triangle
                DrawLine(writer, x, y + size, x + size, y - size, layer);
                DrawLine(writer, x + size, y - size, x - size, y - size, layer);
                DrawLine(writer, x - size, y - size, x, y + size, layer);
                break;
            case 270: // Diamond
                DrawDiamondMarker(writer, x, y, size, layer);
                break;
            default:  // Default point
                DrawPoint(writer, x, y, layer);
                break;
        }
    }
    
    private void DrawSmallMarker(StreamWriter writer, double x, double y, string layer)
    {
        DrawCrossMarker(writer, x, y, 0.2, layer);
    }
    
    private void DrawText(StreamWriter writer, double x, double y, string text, string layer, double height)
    {
        writer.WriteLine("0");
        writer.WriteLine("TEXT");
        writer.WriteLine("8");
        writer.WriteLine(layer);
        writer.WriteLine("10");
        writer.WriteLine(x.ToString("F6"));
        writer.WriteLine("20");
        writer.WriteLine(y.ToString("F6"));
        writer.WriteLine("30");
        writer.WriteLine("0.0");
        writer.WriteLine("40");
        writer.WriteLine(height.ToString("F2"));
        writer.WriteLine("1");
        writer.WriteLine(text);
    }
    
    private void DrawTransitPoints(StreamWriter writer, List<UsblObservation> observations, 
        string layer, DxfPlanViewOptions options)
    {
        foreach (var obs in observations.Where(o => !o.IsExcluded))
        {
            DrawPoint(writer, obs.TransponderEasting, obs.TransponderNorthing, layer);
        }
        
        if (options.ShowExcludedPoints)
        {
            foreach (var obs in observations.Where(o => o.IsExcluded))
            {
                DrawXMarker(writer, obs.TransponderEasting, obs.TransponderNorthing, 0.2, "EXCLUDED_POINTS");
            }
        }
    }
    
    private void DrawTransitLine(StreamWriter writer, List<UsblObservation> observations, string layer)
    {
        var validObs = observations.Where(o => !o.IsExcluded).OrderBy(o => o.Timestamp).ToList();
        if (validObs.Count < 2) return;
        
        for (int i = 1; i < validObs.Count; i++)
        {
            DrawLine(writer, 
                validObs[i-1].TransponderEasting, validObs[i-1].TransponderNorthing,
                validObs[i].TransponderEasting, validObs[i].TransponderNorthing,
                layer);
        }
    }
    
    private void DrawVesselTrack(StreamWriter writer, UsblVerificationProject project)
    {
        var allObs = project.AllSpinTests
            .SelectMany(s => s.Observations.Where(o => !o.IsExcluded))
            .OrderBy(o => o.Timestamp)
            .ToList();
        
        for (int i = 1; i < allObs.Count; i++)
        {
            DrawLine(writer,
                allObs[i-1].VesselEasting, allObs[i-1].VesselNorthing,
                allObs[i].VesselEasting, allObs[i].VesselNorthing,
                "VESSEL_TRACK");
        }
    }
    
    private void DrawCoordinateGrid(StreamWriter writer, DxfBounds bounds, double spacing)
    {
        // Calculate grid start points (rounded to spacing)
        double startX = Math.Floor(bounds.MinX / spacing) * spacing;
        double startY = Math.Floor(bounds.MinY / spacing) * spacing;
        
        // Vertical lines
        for (double x = startX; x <= bounds.MaxX; x += spacing)
        {
            DrawLine(writer, x, bounds.MinY, x, bounds.MaxY, "GRID");
            DrawText(writer, x, bounds.MinY - 1, x.ToString("F0"), "GRID", 0.8);
        }
        
        // Horizontal lines
        for (double y = startY; y <= bounds.MaxY; y += spacing)
        {
            DrawLine(writer, bounds.MinX, y, bounds.MaxX, y, "GRID");
            DrawText(writer, bounds.MinX - 3, y, y.ToString("F0"), "GRID", 0.8);
        }
    }
    
    private void DrawLegend(StreamWriter writer, DxfBounds bounds, UsblVerificationProject project)
    {
        double legendX = bounds.MinX + 2;
        double legendY = bounds.MaxY - 2;
        double lineHeight = 2.5;
        int row = 0;
        
        DrawText(writer, legendX, legendY - (row++ * lineHeight), "LEGEND", "LEGEND", 1.5);
        
        // Tolerance circle
        DrawCircle(writer, legendX + 0.5, legendY - (row * lineHeight) - 0.3, 0.4, "TOLERANCE_MAIN");
        DrawText(writer, legendX + 2, legendY - (row++ * lineHeight), "Tolerance Circle", "LEGEND", 1.0);
        
        // Spin headings
        foreach (var spin in project.AllSpinTests)
        {
            var layerName = $"SPIN_{spin.NominalHeading:000}";
            DrawHeadingSymbol(writer, legendX + 0.5, legendY - (row * lineHeight) - 0.3, (int)spin.NominalHeading, layerName, 0.3);
            DrawText(writer, legendX + 2, legendY - (row++ * lineHeight), $"Heading {spin.NominalHeading}°", "LEGEND", 1.0);
        }
        
        // Transit lines
        DrawLine(writer, legendX, legendY - (row * lineHeight) - 0.3, legendX + 1, legendY - (row * lineHeight) - 0.3, "TRANSIT_1");
        DrawText(writer, legendX + 2, legendY - (row++ * lineHeight), "Transit 1", "LEGEND", 1.0);
        
        DrawLine(writer, legendX, legendY - (row * lineHeight) - 0.3, legendX + 1, legendY - (row * lineHeight) - 0.3, "TRANSIT_2");
        DrawText(writer, legendX + 2, legendY - (row++ * lineHeight), "Transit 2", "LEGEND", 1.0);
        
        // Mean position
        DrawCrossMarker(writer, legendX + 0.5, legendY - (row * lineHeight) - 0.3, 0.3, "MEAN_POSITION");
        DrawText(writer, legendX + 2, legendY - (row++ * lineHeight), "Mean Position", "LEGEND", 1.0);
    }
    
    private void DrawScaleBar(StreamWriter writer, DxfBounds bounds)
    {
        double scaleX = bounds.MinX + (bounds.MaxX - bounds.MinX) * 0.7;
        double scaleY = bounds.MinY + 3;
        
        // Determine appropriate scale length
        double range = bounds.MaxX - bounds.MinX;
        double scaleLength = range > 50 ? 10 : range > 20 ? 5 : range > 10 ? 2 : 1;
        
        // Draw scale bar
        DrawLine(writer, scaleX, scaleY, scaleX + scaleLength, scaleY, "SCALE_BAR");
        DrawLine(writer, scaleX, scaleY - 0.3, scaleX, scaleY + 0.3, "SCALE_BAR");
        DrawLine(writer, scaleX + scaleLength, scaleY - 0.3, scaleX + scaleLength, scaleY + 0.3, "SCALE_BAR");
        
        DrawText(writer, scaleX + scaleLength/2 - 0.5, scaleY + 1, $"{scaleLength}m", "SCALE_BAR", 1.0);
    }
    
    private void DrawNorthArrow(StreamWriter writer, DxfBounds bounds)
    {
        double arrowX = bounds.MaxX - 5;
        double arrowY = bounds.MaxY - 5;
        double arrowLength = 3;
        
        // Arrow shaft
        DrawLine(writer, arrowX, arrowY, arrowX, arrowY + arrowLength, "NORTH_ARROW");
        
        // Arrow head
        DrawLine(writer, arrowX, arrowY + arrowLength, arrowX - 0.5, arrowY + arrowLength - 1, "NORTH_ARROW");
        DrawLine(writer, arrowX, arrowY + arrowLength, arrowX + 0.5, arrowY + arrowLength - 1, "NORTH_ARROW");
        
        // N label
        DrawText(writer, arrowX - 0.3, arrowY + arrowLength + 1, "N", "NORTH_ARROW", 1.5);
    }
    
    #endregion
}

/// <summary>
/// Options for DXF Plan View export
/// </summary>
public class DxfPlanViewOptions
{
    // Tolerance circles
    public bool ShowMultipleToleranceRings { get; set; } = true;
    public bool ShowStatisticalCircles { get; set; } = true;
    public bool ShowConfidenceEllipse { get; set; } = true;
    
    // Points and symbols
    public bool UseSymbolsByHeading { get; set; } = true;
    public bool ShowPerHeadingMeans { get; set; } = true;
    public bool ShowExcludedPoints { get; set; } = true;
    
    // Lines and tracks
    public bool ShowTransitLines { get; set; } = true;
    public bool ShowVesselTrack { get; set; } = false;
    
    // Grid and labels
    public bool ShowGrid { get; set; } = true;
    public double GridSpacing { get; set; } = 5.0;
    public bool ShowCoordinateLabels { get; set; } = true;
    
    // Legend and decorations
    public bool ShowLegend { get; set; } = true;
    public bool ShowScaleBar { get; set; } = true;
    public bool ShowNorthArrow { get; set; } = true;
}

/// <summary>
/// Drawing bounds for DXF
/// </summary>
public class DxfBounds
{
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }
    
    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
}
