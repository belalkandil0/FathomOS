using ClosedXML.Excel;
using FathomOS.Modules.MruCalibration.Models;

namespace FathomOS.Modules.MruCalibration.Services;

/// <summary>
/// Excel export service using ClosedXML
/// Creates comprehensive calibration data workbooks
/// </summary>
public class ExcelExportService
{
    // Subsea7 brand colors (for Excel)
    private static readonly XLColor Subsea7Orange = XLColor.FromHtml("#E85D00");
    private static readonly XLColor Subsea7Teal = XLColor.FromHtml("#009688");
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#1A1A1A");
    private static readonly XLColor TableHeaderBg = XLColor.FromHtml("#2D2D30");
    private static readonly XLColor AcceptedBg = XLColor.FromHtml("#E8F5E9");
    private static readonly XLColor RejectedBg = XLColor.FromHtml("#FFEBEE");
    
    #region Public Methods
    
    /// <summary>
    /// Export calibration data to Excel workbook
    /// </summary>
    public void ExportToExcel(string outputPath, CalibrationSession session)
    {
        using var workbook = new XLWorkbook();
        
        // Summary sheet
        CreateSummarySheet(workbook, session);
        
        // Pitch data sheets
        if (session.PitchData.IsProcessed && session.PitchData.DataPoints.Count > 0)
        {
            CreateDataSheet(workbook, session.PitchData, "Pitch Data");
            CreateStatisticsSheet(workbook, session.PitchData, "Pitch Statistics");
        }
        
        // Roll data sheets
        if (session.RollData.IsProcessed && session.RollData.DataPoints.Count > 0)
        {
            CreateDataSheet(workbook, session.RollData, "Roll Data");
            CreateStatisticsSheet(workbook, session.RollData, "Roll Statistics");
        }
        
        workbook.SaveAs(outputPath);
    }
    
    #endregion
    
    #region Summary Sheet
    
    private void CreateSummarySheet(IXLWorkbook workbook, CalibrationSession session)
    {
        var ws = workbook.Worksheets.Add("Summary");
        var info = session.ProjectInfo;
        int row = 1;
        
        // Title
        ws.Cell(row, 1).Value = "MRU CALIBRATION REPORT";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        ws.Cell(row, 1).Style.Font.FontColor = Subsea7Orange;
        ws.Range(row, 1, row, 4).Merge();
        row += 2;
        
        // Project Information
        ws.Cell(row, 1).Value = "PROJECT INFORMATION";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = Subsea7Orange;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, 4).Merge();
        row++;
        
        AddInfoRow(ws, ref row, "Project Title", info.ProjectTitle);
        AddInfoRow(ws, ref row, "Project Number", info.ProjectNumber);
        AddInfoRow(ws, ref row, "Vessel", info.VesselName);
        AddInfoRow(ws, ref row, "Location", info.Location);
        AddInfoRow(ws, ref row, "Latitude", info.Latitude);
        AddInfoRow(ws, ref row, "Purpose", info.Purpose.ToString());
        AddInfoRow(ws, ref row, "Observed By", info.ObservedBy);
        AddInfoRow(ws, ref row, "Checked By", info.CheckedBy);
        row++;
        
        // Reference System
        ws.Cell(row, 1).Value = "REFERENCE SYSTEM";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = Subsea7Teal;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, 4).Merge();
        row++;
        
        AddInfoRow(ws, ref row, "Model", info.ReferenceSystem.Model);
        AddInfoRow(ws, ref row, "Serial Number", info.ReferenceSystem.SerialNumber);
        row++;
        
        // Verified System
        ws.Cell(row, 1).Value = "VERIFIED SYSTEM";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = Subsea7Orange;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, 4).Merge();
        row++;
        
        AddInfoRow(ws, ref row, "Model", info.VerifiedSystem.Model);
        AddInfoRow(ws, ref row, "Serial Number", info.VerifiedSystem.SerialNumber);
        row += 2;
        
        // Pitch Results Summary
        if (session.PitchData.IsProcessed && session.PitchData.Statistics != null)
        {
            AddResultsSummary(ws, ref row, session.PitchData, "PITCH");
            row++;
        }
        
        // Roll Results Summary
        if (session.RollData.IsProcessed && session.RollData.Statistics != null)
        {
            AddResultsSummary(ws, ref row, session.RollData, "ROLL");
        }
        
        // Auto-fit columns
        ws.Columns(1, 4).AdjustToContents();
        ws.Column(1).Width = 20;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 20;
        ws.Column(4).Width = 30;
    }
    
    private void AddInfoRow(IXLWorksheet ws, ref int row, string label, string? value)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
        ws.Cell(row, 2).Value = value ?? "-";
        row++;
    }
    
    private void AddResultsSummary(IXLWorksheet ws, ref int row, SensorCalibrationData data, string sensorName)
    {
        var stats = data.Statistics!;
        
        ws.Cell(row, 1).Value = $"{sensorName} RESULTS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = Subsea7Orange;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, 4).Merge();
        row++;
        
        // Stats table
        ws.Cell(row, 1).Value = "Statistic";
        ws.Cell(row, 2).Value = "Value";
        ws.Cell(row, 3).Value = "Statistic";
        ws.Cell(row, 4).Value = "Value";
        ws.Range(row, 1, row, 4).Style.Font.Bold = true;
        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = TableHeaderBg;
        ws.Range(row, 1, row, 4).Style.Font.FontColor = XLColor.White;
        row++;
        
        ws.Cell(row, 1).Value = "Mean C-O";
        ws.Cell(row, 2).Value = stats.MeanCO_Accepted;
        ws.Cell(row, 2).Style.NumberFormat.Format = "0.0000";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.FontColor = Subsea7Orange;
        ws.Cell(row, 3).Value = "Std Dev";
        ws.Cell(row, 4).Value = stats.StdDevCO_Accepted;
        ws.Cell(row, 4).Style.NumberFormat.Format = "0.0000";
        row++;
        
        ws.Cell(row, 1).Value = "Min C-O";
        ws.Cell(row, 2).Value = stats.MinCO;
        ws.Cell(row, 2).Style.NumberFormat.Format = "0.0000";
        ws.Cell(row, 3).Value = "Max C-O";
        ws.Cell(row, 4).Value = stats.MaxCO;
        ws.Cell(row, 4).Style.NumberFormat.Format = "0.0000";
        row++;
        
        ws.Cell(row, 1).Value = "Total Observations";
        ws.Cell(row, 2).Value = stats.TotalObservations;
        ws.Cell(row, 3).Value = "Rejected";
        ws.Cell(row, 4).Value = $"{stats.RejectedCount} ({stats.RejectionPercentage:F1}%)";
        row++;
        
        ws.Cell(row, 1).Value = "Start Time";
        ws.Cell(row, 2).Value = stats.StartTime.ToString("HH:mm:ss");
        ws.Cell(row, 3).Value = "End Time";
        ws.Cell(row, 4).Value = stats.EndTime.ToString("HH:mm:ss");
        row++;
        
        // Decision row
        var decisionColor = data.Decision == AcceptanceDecision.Accepted ? AcceptedBg :
                           data.Decision == AcceptanceDecision.Rejected ? RejectedBg :
                           XLColor.FromHtml("#FFF3E0");
        
        ws.Cell(row, 1).Value = "DECISION";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = data.Decision.ToString().ToUpper();
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = decisionColor;
        row++;
    }
    
    #endregion
    
    #region Data Sheet
    
    private void CreateDataSheet(IXLWorkbook workbook, SensorCalibrationData data, string sheetName)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        int row = 1;
        
        // Headers
        var headers = new[] { "Index", "Time", "Reference", "Verified", "C-O", "Z-Score", "Status" };
        for (int col = 1; col <= headers.Length; col++)
        {
            ws.Cell(row, col).Value = headers[col - 1];
        }
        
        var headerRange = ws.Range(row, 1, row, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = TableHeaderBg;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        row++;
        
        // Data rows
        foreach (var point in data.DataPoints.OrderBy(p => p.Index))
        {
            ws.Cell(row, 1).Value = point.Index;
            ws.Cell(row, 2).Value = point.Timestamp.ToString("HH:mm:ss");
            ws.Cell(row, 3).Value = point.ReferenceValue;
            ws.Cell(row, 4).Value = point.VerifiedValue;
            ws.Cell(row, 5).Value = point.CO;
            ws.Cell(row, 6).Value = point.StandardizedCO;
            ws.Cell(row, 7).Value = point.Status.ToString();
            
            // Format numbers
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.0000";
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0000";
            ws.Cell(row, 5).Style.NumberFormat.Format = "0.0000";
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.00";
            
            // Highlight rejected rows
            if (point.Status == PointStatus.Rejected)
            {
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = RejectedBg;
                ws.Cell(row, 7).Style.Font.FontColor = XLColor.Red;
            }
            else if (point.Status == PointStatus.Accepted)
            {
                ws.Cell(row, 7).Style.Font.FontColor = XLColor.Green;
            }
            
            row++;
        }
        
        // Auto-fit and format
        ws.Columns(1, headers.Length).AdjustToContents();
        
        // Add filters
        ws.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
        
        // Freeze header row
        ws.SheetView.FreezeRows(1);
    }
    
    #endregion
    
    #region Statistics Sheet
    
    private void CreateStatisticsSheet(IXLWorkbook workbook, SensorCalibrationData data, string sheetName)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        var stats = data.Statistics;
        
        if (stats == null) return;
        
        int row = 1;
        
        // Title
        ws.Cell(row, 1).Value = $"{data.SensorType} Calibration Statistics";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = Subsea7Orange;
        ws.Range(row, 1, row, 2).Merge();
        row += 2;
        
        // All Points Section
        ws.Cell(row, 1).Value = "ALL POINTS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = TableHeaderBg;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, 2).Merge();
        row++;
        
        AddStatRow(ws, ref row, "Total Observations", stats.TotalObservations.ToString());
        AddStatRow(ws, ref row, "Mean C-O (All)", $"{stats.MeanCO_All:F4}°");
        AddStatRow(ws, ref row, "Std Dev (All)", $"{stats.StdDevCO_All:F4}°");
        row++;
        
        // Accepted Points Section
        ws.Cell(row, 1).Value = "ACCEPTED POINTS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = Subsea7Teal;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, 2).Merge();
        row++;
        
        AddStatRow(ws, ref row, "Accepted Count", stats.AcceptedCount.ToString());
        AddStatRow(ws, ref row, "Mean C-O (Accepted)", $"{stats.MeanCO_Accepted:F4}°");
        AddStatRow(ws, ref row, "Std Dev (Accepted)", $"{stats.StdDevCO_Accepted:F4}°");
        AddStatRow(ws, ref row, "Min C-O", $"{stats.MinCO:F4}°");
        AddStatRow(ws, ref row, "Max C-O", $"{stats.MaxCO:F4}°");
        row++;
        
        // Rejected Points Section
        ws.Cell(row, 1).Value = "REJECTED POINTS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F44336");
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, 2).Merge();
        row++;
        
        AddStatRow(ws, ref row, "Rejected Count", stats.RejectedCount.ToString());
        AddStatRow(ws, ref row, "Rejection %", $"{stats.RejectionPercentage:F1}%");
        row++;
        
        // Time Range Section
        ws.Cell(row, 1).Value = "TIME RANGE";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = TableHeaderBg;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, 2).Merge();
        row++;
        
        AddStatRow(ws, ref row, "Start Time", stats.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        AddStatRow(ws, ref row, "End Time", stats.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
        AddStatRow(ws, ref row, "Duration", (stats.EndTime - stats.StartTime).ToString(@"hh\:mm\:ss"));
        row++;
        
        // Decision Section
        var decisionColor = data.Decision == AcceptanceDecision.Accepted ? AcceptedBg :
                           data.Decision == AcceptanceDecision.Rejected ? RejectedBg :
                           XLColor.FromHtml("#FFF3E0");
        
        ws.Cell(row, 1).Value = "DECISION";
        ws.Cell(row, 2).Value = data.Decision.ToString().ToUpper();
        ws.Range(row, 1, row, 2).Style.Font.Bold = true;
        ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = decisionColor;
        ws.Range(row, 1, row, 2).Style.Font.FontSize = 12;
        
        // Auto-fit columns
        ws.Column(1).Width = 25;
        ws.Column(2).Width = 25;
    }
    
    private void AddStatRow(IXLWorksheet ws, ref int row, string label, string value)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
        ws.Cell(row, 2).Value = value;
        row++;
    }
    
    #endregion
}
