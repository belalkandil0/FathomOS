using System;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Excel export service for USBL verification reports
/// </summary>
public class ExcelExportService
{
    public void Export(string filePath, UsblVerificationProject project, VerificationResults results)
    {
        using var workbook = new XLWorkbook();
        
        // Create summary sheet
        CreateSummarySheet(workbook, project, results);
        
        // Create spin data sheets
        if (project.Spin000.HasData) CreateSpinDataSheet(workbook, project.Spin000, "Spin_000");
        if (project.Spin090.HasData) CreateSpinDataSheet(workbook, project.Spin090, "Spin_090");
        if (project.Spin180.HasData) CreateSpinDataSheet(workbook, project.Spin180, "Spin_180");
        if (project.Spin270.HasData) CreateSpinDataSheet(workbook, project.Spin270, "Spin_270");
        
        // Create transit data sheets
        if (project.Transit1.HasData) CreateTransitDataSheet(workbook, project.Transit1, "Transit_1");
        if (project.Transit2.HasData) CreateTransitDataSheet(workbook, project.Transit2, "Transit_2");
        
        workbook.SaveAs(filePath);
    }
    
    private void CreateSummarySheet(IXLWorkbook workbook, UsblVerificationProject project, VerificationResults results)
    {
        var ws = workbook.Worksheets.Add("Summary");
        int row = 1;
        
        // Title
        ws.Cell(row, 1).Value = "USBL VERIFICATION SUMMARY REPORT";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        ws.Range(row, 1, row, 6).Merge();
        row += 2;
        
        // Project Information
        ws.Cell(row, 1).Value = "PROJECT INFORMATION";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.Range(row, 1, row, 2).Merge();
        row++;
        
        ws.Cell(row, 1).Value = "Project Name:";
        ws.Cell(row, 2).Value = project.ProjectName;
        row++;
        ws.Cell(row, 1).Value = "Client:";
        ws.Cell(row, 2).Value = project.ClientName;
        row++;
        ws.Cell(row, 1).Value = "Vessel:";
        ws.Cell(row, 2).Value = project.VesselName;
        row++;
        ws.Cell(row, 1).Value = "Transponder:";
        ws.Cell(row, 2).Value = project.TransponderName;
        row++;
        ws.Cell(row, 1).Value = "Transducer:";
        ws.Cell(row, 2).Value = project.TransducerLocation;
        row++;
        ws.Cell(row, 1).Value = "Survey Date:";
        ws.Cell(row, 2).Value = project.SurveyDate?.ToString("dd/MM/yyyy") ?? "";
        row++;
        ws.Cell(row, 1).Value = "Processor:";
        ws.Cell(row, 2).Value = project.ProcessorName;
        row++;
        ws.Cell(row, 1).Value = "Report Date:";
        ws.Cell(row, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        row += 2;
        
        // Spin Verification Results
        ws.Cell(row, 1).Value = "SPIN POSITION VERIFICATION";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.Range(row, 1, row, 4).Merge();
        row++;
        
        // Headers
        ws.Cell(row, 2).Value = "Easting (m)";
        ws.Cell(row, 3).Value = "Northing (m)";
        ws.Cell(row, 4).Value = "Depth (m)";
        ws.Cell(row, 5).Value = "Diff E";
        ws.Cell(row, 6).Value = "Diff N";
        ws.Cell(row, 7).Value = "Radial";
        ws.Range(row, 2, row, 7).Style.Font.Bold = true;
        row++;
        
        // Per-heading results
        foreach (var heading in results.HeadingResults)
        {
            ws.Cell(row, 1).Value = heading.Heading;
            ws.Cell(row, 2).Value = heading.Easting;
            ws.Cell(row, 3).Value = heading.Northing;
            ws.Cell(row, 4).Value = heading.Depth;
            ws.Cell(row, 5).Value = heading.DiffEasting;
            ws.Cell(row, 6).Value = heading.DiffNorthing;
            ws.Cell(row, 7).Value = heading.DiffRadial;
            row++;
        }
        row++;
        
        // Summary statistics
        ws.Cell(row, 1).Value = "Overall Average:";
        ws.Cell(row, 2).Value = results.SpinOverallAverageEasting;
        ws.Cell(row, 3).Value = results.SpinOverallAverageNorthing;
        ws.Cell(row, 4).Value = results.SpinOverallAverageDepth;
        row++;
        
        ws.Cell(row, 1).Value = "2σ Std Dev:";
        ws.Cell(row, 2).Value = results.SpinStdDevEasting2Sigma;
        ws.Cell(row, 3).Value = results.SpinStdDevNorthing2Sigma;
        ws.Cell(row, 4).Value = results.SpinStdDevDepth2Sigma;
        row++;
        
        ws.Cell(row, 1).Value = "Max Diff:";
        ws.Cell(row, 2).Value = results.SpinMaxDiffEasting;
        ws.Cell(row, 3).Value = results.SpinMaxDiffNorthing;
        ws.Cell(row, 4).Value = results.SpinMaxDiffDepth;
        row++;
        
        ws.Cell(row, 1).Value = "Max Radial Diff:";
        ws.Cell(row, 2).Value = results.SpinMaxDiffRadial;
        row++;
        
        ws.Cell(row, 1).Value = "2DRMS:";
        ws.Cell(row, 2).Value = results.Spin2DRMS;
        row++;
        
        ws.Cell(row, 1).Value = "Max Allowable:";
        ws.Cell(row, 2).Value = results.SpinMaxAllowableDiff;
        ws.Cell(row, 3).Value = "±0.5m or 0.2% slant";
        row++;
        
        ws.Cell(row, 1).Value = "RESULT:";
        ws.Cell(row, 2).Value = results.SpinPassFail ? "PASS" : "FAIL";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.FontColor = results.SpinPassFail ? XLColor.Green : XLColor.Red;
        row += 2;
        
        // Transit Verification Results
        ws.Cell(row, 1).Value = "TRANSIT POSITION VERIFICATION";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.Range(row, 1, row, 4).Merge();
        row++;
        
        ws.Cell(row, 1).Value = "Combined Average:";
        ws.Cell(row, 2).Value = results.TransitCombinedAverageEasting;
        ws.Cell(row, 3).Value = results.TransitCombinedAverageNorthing;
        ws.Cell(row, 4).Value = results.TransitCombinedAverageDepth;
        row++;
        
        ws.Cell(row, 1).Value = "2σ Std Dev:";
        ws.Cell(row, 2).Value = results.TransitStdDevEasting2Sigma;
        ws.Cell(row, 3).Value = results.TransitStdDevNorthing2Sigma;
        row++;
        
        ws.Cell(row, 1).Value = "Max Diff from Spin:";
        ws.Cell(row, 2).Value = results.TransitMaxDiffFromSpin;
        row++;
        
        ws.Cell(row, 1).Value = "2DRMS:";
        ws.Cell(row, 2).Value = results.Transit2DRMS;
        row++;
        
        ws.Cell(row, 1).Value = "Max Allowable:";
        ws.Cell(row, 2).Value = results.TransitMaxAllowableSpread;
        ws.Cell(row, 3).Value = "±1m or 0.5% slant";
        row++;
        
        ws.Cell(row, 1).Value = "RESULT:";
        ws.Cell(row, 2).Value = results.TransitPassFail ? "PASS" : "FAIL";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.FontColor = results.TransitPassFail ? XLColor.Green : XLColor.Red;
        row += 2;
        
        // Alignment Verification
        ws.Cell(row, 1).Value = "ALIGNMENT VERIFICATION";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.Range(row, 1, row, 4).Merge();
        row++;
        
        ws.Cell(row, 2).Value = "Line 1 (Roll)";
        ws.Cell(row, 3).Value = "Line 2 (Pitch)";
        ws.Cell(row, 4).Value = "Tolerance";
        row++;
        
        ws.Cell(row, 1).Value = "Residual Alignment:";
        ws.Cell(row, 2).Value = results.Line1ResidualAlignment;
        ws.Cell(row, 3).Value = results.Line2ResidualAlignment;
        ws.Cell(row, 4).Value = project.AlignmentTolerance;
        row++;
        
        ws.Cell(row, 1).Value = "Alignment Pass/Fail:";
        ws.Cell(row, 2).Value = results.Line1AlignmentPass ? "PASS" : "FAIL";
        ws.Cell(row, 3).Value = results.Line2AlignmentPass ? "PASS" : "FAIL";
        ws.Cell(row, 2).Style.Font.FontColor = results.Line1AlignmentPass ? XLColor.Green : XLColor.Red;
        ws.Cell(row, 3).Style.Font.FontColor = results.Line2AlignmentPass ? XLColor.Green : XLColor.Red;
        row++;
        
        ws.Cell(row, 1).Value = "Scale Factor:";
        ws.Cell(row, 2).Value = results.Line1ScaleFactor;
        ws.Cell(row, 3).Value = results.Line2ScaleFactor;
        ws.Cell(row, 4).Value = project.ScaleFactorTolerance;
        row++;
        
        ws.Cell(row, 1).Value = "Scale Pass/Fail:";
        ws.Cell(row, 2).Value = results.Line1ScalePass ? "PASS" : "FAIL";
        ws.Cell(row, 3).Value = results.Line2ScalePass ? "PASS" : "FAIL";
        ws.Cell(row, 2).Style.Font.FontColor = results.Line1ScalePass ? XLColor.Green : XLColor.Red;
        ws.Cell(row, 3).Style.Font.FontColor = results.Line2ScalePass ? XLColor.Green : XLColor.Red;
        row += 2;
        
        // Absolute Position Check
        ws.Cell(row, 1).Value = "ABSOLUTE POSITION CHECK";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        ws.Range(row, 1, row, 4).Merge();
        row++;
        
        ws.Cell(row, 1).Value = "Difference (E/N/D):";
        ws.Cell(row, 2).Value = results.AbsolutePositionDiffEasting;
        ws.Cell(row, 3).Value = results.AbsolutePositionDiffNorthing;
        ws.Cell(row, 4).Value = results.AbsolutePositionDiffDepth;
        row++;
        
        ws.Cell(row, 1).Value = "Range:";
        ws.Cell(row, 2).Value = results.AbsolutePositionRange;
        row++;
        
        ws.Cell(row, 1).Value = "Bearing:";
        ws.Cell(row, 2).Value = results.AbsolutePositionBearing;
        row += 2;
        
        // Overall Result
        ws.Cell(row, 1).Value = "OVERALL RESULT:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 2).Value = results.OverallStatus;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.FontSize = 14;
        ws.Cell(row, 2).Style.Font.FontColor = results.OverallPass ? XLColor.Green : XLColor.Red;
        
        // Format numbers
        ws.Columns().AdjustToContents();
        ws.Column(2).Width = 15;
        ws.Column(3).Width = 15;
        ws.Column(4).Width = 15;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 12;
        
        // Number formatting
        var usedRange = ws.RangeUsed();
        if (usedRange != null)
        {
            foreach (var cell in usedRange.Cells())
            {
                if (cell.Value.IsNumber)
                {
                    cell.Style.NumberFormat.Format = "0.000";
                }
            }
        }
    }
    
    private void CreateSpinDataSheet(IXLWorkbook workbook, SpinTestData spinData, string sheetName)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        int row = 1;
        
        // Header info
        ws.Cell(row, 1).Value = spinData.Name;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row++;
        
        ws.Cell(row, 1).Value = $"Source: {Path.GetFileName(spinData.SourceFile)}";
        row++;
        
        ws.Cell(row, 1).Value = $"Points: {spinData.Statistics.PointCount}";
        row++;
        
        ws.Cell(row, 1).Value = $"Average Gyro: {spinData.Statistics.AverageGyro:F1}°";
        row += 2;
        
        // Statistics
        ws.Cell(row, 1).Value = "STATISTICS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        
        ws.Cell(row, 1).Value = "Avg TP Easting:";
        ws.Cell(row, 2).Value = spinData.Statistics.AverageTransponderEasting;
        row++;
        ws.Cell(row, 1).Value = "Avg TP Northing:";
        ws.Cell(row, 2).Value = spinData.Statistics.AverageTransponderNorthing;
        row++;
        ws.Cell(row, 1).Value = "Avg Depth:";
        ws.Cell(row, 2).Value = spinData.Statistics.AverageDepth;
        row++;
        ws.Cell(row, 1).Value = "2σ Std Dev E:";
        ws.Cell(row, 2).Value = spinData.Statistics.StdDevEasting2Sigma;
        row++;
        ws.Cell(row, 1).Value = "2σ Std Dev N:";
        ws.Cell(row, 2).Value = spinData.Statistics.StdDevNorthing2Sigma;
        row++;
        ws.Cell(row, 1).Value = "Slant Range:";
        ws.Cell(row, 2).Value = spinData.Statistics.SlantRange;
        row++;
        ws.Cell(row, 1).Value = "Tolerance:";
        ws.Cell(row, 2).Value = spinData.Statistics.ToleranceValue;
        row += 2;
        
        // Data table
        ws.Cell(row, 1).Value = "DATA";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        
        // Headers
        var headers = new[] { "Index", "Date", "Time", "Gyro", "Vessel E", "Vessel N", "TP E", "TP N", "TP Depth", "ΔE", "ΔN", "ΔD" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;
        
        // Data
        foreach (var obs in spinData.Observations)
        {
            ws.Cell(row, 1).Value = obs.Index;
            ws.Cell(row, 2).Value = obs.DateTime.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = obs.DateTime.ToString("HH:mm:ss");
            ws.Cell(row, 4).Value = obs.VesselGyro;
            ws.Cell(row, 5).Value = obs.VesselEasting;
            ws.Cell(row, 6).Value = obs.VesselNorthing;
            ws.Cell(row, 7).Value = obs.TransponderEasting;
            ws.Cell(row, 8).Value = obs.TransponderNorthing;
            ws.Cell(row, 9).Value = obs.TransponderDepth;
            ws.Cell(row, 10).Value = obs.DeltaEasting;
            ws.Cell(row, 11).Value = obs.DeltaNorthing;
            ws.Cell(row, 12).Value = obs.DeltaDepth;
            row++;
        }
        
        ws.Columns().AdjustToContents();
    }
    
    private void CreateTransitDataSheet(IXLWorkbook workbook, TransitTestData transitData, string sheetName)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        int row = 1;
        
        // Header info
        ws.Cell(row, 1).Value = transitData.Name;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row++;
        
        ws.Cell(row, 1).Value = $"Source: {Path.GetFileName(transitData.SourceFile)}";
        row++;
        
        ws.Cell(row, 1).Value = $"Points: {transitData.Statistics.PointCount}";
        row++;
        
        ws.Cell(row, 1).Value = $"Transit Length: {transitData.Statistics.TransitLength:F1}m";
        row++;
        
        ws.Cell(row, 1).Value = $"Transit Direction: {transitData.Statistics.TransitDirection:F1}°";
        row += 2;
        
        // Statistics
        ws.Cell(row, 1).Value = "STATISTICS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        
        ws.Cell(row, 1).Value = "Avg TP Easting:";
        ws.Cell(row, 2).Value = transitData.Statistics.AverageTransponderEasting;
        row++;
        ws.Cell(row, 1).Value = "Avg TP Northing:";
        ws.Cell(row, 2).Value = transitData.Statistics.AverageTransponderNorthing;
        row++;
        ws.Cell(row, 1).Value = "Avg Depth:";
        ws.Cell(row, 2).Value = transitData.Statistics.AverageDepth;
        row++;
        ws.Cell(row, 1).Value = "Slant Range:";
        ws.Cell(row, 2).Value = transitData.Statistics.SlantRange;
        row++;
        ws.Cell(row, 1).Value = "Tolerance:";
        ws.Cell(row, 2).Value = transitData.Statistics.ToleranceValue;
        row += 2;
        
        // Data table
        ws.Cell(row, 1).Value = "DATA";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        
        // Headers
        var headers = new[] { "Index", "Date", "Time", "Gyro", "Vessel E", "Vessel N", "TP E", "TP N", "TP Depth", "ΔE", "ΔN", "Offset", "Sign" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;
        
        // Data
        foreach (var obs in transitData.Observations)
        {
            ws.Cell(row, 1).Value = obs.Index;
            ws.Cell(row, 2).Value = obs.DateTime.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = obs.DateTime.ToString("HH:mm:ss");
            ws.Cell(row, 4).Value = obs.VesselGyro;
            ws.Cell(row, 5).Value = obs.VesselEasting;
            ws.Cell(row, 6).Value = obs.VesselNorthing;
            ws.Cell(row, 7).Value = obs.TransponderEasting;
            ws.Cell(row, 8).Value = obs.TransponderNorthing;
            ws.Cell(row, 9).Value = obs.TransponderDepth;
            ws.Cell(row, 10).Value = obs.DeltaEasting;
            ws.Cell(row, 11).Value = obs.DeltaNorthing;
            ws.Cell(row, 12).Value = obs.VesselOffsetDistance;
            ws.Cell(row, 13).Value = obs.VesselOffsetSign;
            row++;
        }
        
        ws.Columns().AdjustToContents();
    }
}
