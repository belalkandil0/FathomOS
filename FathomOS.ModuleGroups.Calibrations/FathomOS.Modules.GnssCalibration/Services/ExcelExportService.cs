using ClosedXML.Excel;
using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Export options for Excel workbook generation.
/// </summary>
public class ExcelExportOptions
{
    public bool IncludeRawData { get; set; } = true;
    public bool IncludeFilteredData { get; set; } = true;
    public bool IncludeRejectedData { get; set; } = true;
    public bool IncludeStatistics { get; set; } = true;
    public bool IncludeSystemComparison { get; set; } = true;
    public bool ApplyFormatting { get; set; } = true;
}

/// <summary>
/// Generates professional Excel workbooks with GNSS comparison data.
/// Uses ClosedXML library (from Core).
/// </summary>
public class ExcelExportService
{
    private readonly ExcelExportOptions _options;
    
    // Modern professional colors
    private static readonly XLColor PrimaryBlue = XLColor.FromHtml("#1E3A5F");
    private static readonly XLColor AccentOrange = XLColor.FromHtml("#E67E22");
    private static readonly XLColor SuccessGreen = XLColor.FromHtml("#2ECC71");
    private static readonly XLColor ErrorRed = XLColor.FromHtml("#E74C3C");
    private static readonly XLColor LightBlue = XLColor.FromHtml("#EBF5FB");
    private static readonly XLColor LightGray = XLColor.FromHtml("#F8F9FA");
    private static readonly XLColor MediumGray = XLColor.FromHtml("#BDC3C7");
    private static readonly XLColor DarkText = XLColor.FromHtml("#2C3E50");
    
    public ExcelExportService() : this(new ExcelExportOptions()) { }
    
    public ExcelExportService(ExcelExportOptions options)
    {
        _options = options;
    }
    
    /// <summary>
    /// Exports GNSS comparison data to a professionally formatted Excel workbook.
    /// </summary>
    public void Export(string filePath, GnssProject project, 
        IEnumerable<GnssComparisonPoint> points, GnssStatisticsResult? statistics)
    {
        var pointList = points.ToList();
        
        using var workbook = new XLWorkbook();
        
        // Create sheets in order
        CreateSummarySheet(workbook, project, statistics, pointList);
        
        if (_options.IncludeStatistics && statistics != null)
            CreateStatisticsSheet(workbook, project, statistics);
        
        if (_options.IncludeSystemComparison && statistics != null)
            CreateSystemComparisonSheet(workbook, project, statistics);
        
        if (_options.IncludeFilteredData)
            CreateDataSheet(workbook, "Filtered Data", project, 
                pointList.Where(p => !p.IsRejected).ToList(), SuccessGreen);
        
        if (_options.IncludeRawData)
            CreateDataSheet(workbook, "All Data", project, pointList, PrimaryBlue);
        
        if (_options.IncludeRejectedData && pointList.Any(p => p.IsRejected))
            CreateDataSheet(workbook, "Rejected Points", project, 
                pointList.Where(p => p.IsRejected).ToList(), ErrorRed);
        
        workbook.SaveAs(filePath);
    }
    
    private void CreateSummarySheet(IXLWorkbook workbook, GnssProject project, 
        GnssStatisticsResult? statistics, List<GnssComparisonPoint> points)
    {
        var ws = workbook.Worksheets.Add("Summary");
        string unit = project.CoordinateUnit ?? "m";
        int row = 1;
        
        // Title banner
        ws.Cell(row, 1).Value = "GNSS CALIBRATION & VERIFICATION REPORT";
        ws.Range(row, 1, row, 8).Merge();
        StyleTitleCell(ws.Cell(row, 1));
        row += 2;
        
        // Project Information Section
        ws.Cell(row, 1).Value = "PROJECT INFORMATION";
        StyleSectionHeader(ws.Range(row, 1, row, 4));
        row++;
        
        AddInfoRow(ws, ref row, "Project Name", project.ProjectName);
        AddInfoRow(ws, ref row, "Client", project.ClientName);
        AddInfoRow(ws, ref row, "Vessel", project.VesselName);
        AddInfoRow(ws, ref row, "Survey Date", project.SurveyDate?.ToString("yyyy-MM-dd") ?? "-");
        AddInfoRow(ws, ref row, "Surveyor", project.SurveyorName);
        AddInfoRow(ws, ref row, "Location", project.Location);
        AddInfoRow(ws, ref row, "Coordinate Unit", unit);
        row++;
        
        // Systems Configuration
        ws.Cell(row, 1).Value = "SYSTEMS CONFIGURATION";
        StyleSectionHeader(ws.Range(row, 1, row, 4));
        row++;
        
        AddInfoRow(ws, ref row, "System 1 (Computed)", project.System1Name);
        AddInfoRow(ws, ref row, "System 2 (Observed)", project.System2Name);
        row++;
        
        // Data Summary Section
        ws.Cell(row, 1).Value = "DATA SUMMARY";
        StyleSectionHeader(ws.Range(row, 1, row, 4));
        row++;
        
        int accepted = points.Count(p => !p.IsRejected);
        int rejected = points.Count(p => p.IsRejected);
        double rejectPct = points.Count > 0 ? (rejected * 100.0 / points.Count) : 0;
        
        AddInfoRow(ws, ref row, "Total Points", points.Count.ToString());
        AddInfoRow(ws, ref row, "Accepted Points", accepted.ToString());
        AddInfoRow(ws, ref row, "Rejected Points", $"{rejected} ({rejectPct:F1}%)");
        
        if (points.Count > 0)
        {
            var minTime = points.Min(p => p.DateTime);
            var maxTime = points.Max(p => p.DateTime);
            var duration = maxTime - minTime;
            AddInfoRow(ws, ref row, "Time Range", $"{minTime:HH:mm:ss} - {maxTime:HH:mm:ss}");
            AddInfoRow(ws, ref row, "Duration", $"{duration.TotalMinutes:F1} minutes");
        }
        row++;
        
        // Key Results Section
        if (statistics != null)
        {
            ws.Cell(row, 1).Value = "KEY RESULTS";
            StyleSectionHeader(ws.Range(row, 1, row, 4));
            row++;
            
            var stats = statistics.FilteredStatistics;
            
            // 2DRMS highlight box
            ws.Cell(row, 1).Value = "2DRMS";
            ws.Cell(row, 2).Value = $"{stats.Delta2DRMS:F4} {unit}";
            ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = AccentOrange;
            ws.Range(row, 1, row, 2).Style.Font.FontColor = XLColor.White;
            ws.Range(row, 1, row, 2).Style.Font.Bold = true;
            ws.Range(row, 1, row, 2).Style.Font.FontSize = 14;
            ws.Range(row, 1, row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;
            
            AddInfoRow(ws, ref row, $"Mean ΔE ({unit})", $"{stats.DeltaMeanEasting:F4}");
            AddInfoRow(ws, ref row, $"Mean ΔN ({unit})", $"{stats.DeltaMeanNorthing:F4}");
            AddInfoRow(ws, ref row, $"CEP 50% ({unit})", $"{stats.Cep50:F4}");
            AddInfoRow(ws, ref row, $"CEP 95% ({unit})", $"{stats.Cep95:F4}");
            AddInfoRow(ws, ref row, $"Spread ({unit})", $"{stats.Spread:F4}");
        }
        
        // Format columns
        ws.Column(1).Width = 22;
        ws.Column(2).Width = 25;
        ws.Column(3).Width = 15;
        ws.Column(4).Width = 15;
        
        // Add border around content
        var usedRange = ws.RangeUsed();
        if (usedRange != null)
        {
            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.OutsideBorderColor = MediumGray;
        }
    }
    
    private void CreateStatisticsSheet(IXLWorkbook workbook, GnssProject project, GnssStatisticsResult statistics)
    {
        var ws = workbook.Worksheets.Add("Statistics");
        string unit = project.CoordinateUnit ?? "m";
        int row = 1;
        
        // Title
        ws.Cell(row, 1).Value = "STATISTICAL ANALYSIS";
        StyleTitleCell(ws.Cell(row, 1));
        ws.Range(row, 1, row, 7).Merge();
        row += 2;
        
        // Create side-by-side comparison tables
        
        // Raw Statistics Table (left side)
        ws.Cell(row, 1).Value = "RAW DATA STATISTICS";
        StyleSectionHeader(ws.Range(row, 1, row, 4));
        row++;
        
        CreateStatTable(ws, ref row, 1, statistics.RawStatistics, unit, PrimaryBlue);
        
        int rawEndRow = row;
        row = 4; // Reset row for filtered table
        
        // Filtered Statistics Table (right side)
        ws.Cell(row - 1, 6).Value = "FILTERED DATA STATISTICS";
        StyleSectionHeader(ws.Range(row - 1, 6, row - 1, 9));
        
        CreateStatTable(ws, ref row, 6, statistics.FilteredStatistics, unit, SuccessGreen);
        
        row = Math.Max(rawEndRow, row) + 2;
        
        // System-specific statistics
        ws.Cell(row, 1).Value = "SYSTEM-SPECIFIC STATISTICS";
        StyleSectionHeader(ws.Range(row, 1, row, 9));
        row++;
        
        var filtered = statistics.FilteredStatistics;
        
        // Headers
        ws.Cell(row, 1).Value = "Metric";
        ws.Cell(row, 2).Value = $"{project.System1Name}";
        ws.Cell(row, 3).Value = $"{project.System2Name}";
        ws.Cell(row, 4).Value = "Delta (C-O)";
        StyleTableHeader(ws.Range(row, 1, row, 4));
        row++;
        
        // 2DRMS row
        AddComparisonRow(ws, ref row, $"2DRMS ({unit})", 
            filtered.System1_2DRMS, filtered.System2_2DRMS, filtered.Delta2DRMS, true);
        
        // Spread row
        AddComparisonRow(ws, ref row, $"Spread ({unit})", 
            filtered.System1Spread, filtered.System2Spread, filtered.Spread, false);
        
        // Mean Easting
        AddComparisonRow(ws, ref row, $"Mean E ({unit})", 
            filtered.System1MeanEasting, filtered.System2MeanEasting, filtered.DeltaMeanEasting, false);
        
        // Mean Northing
        AddComparisonRow(ws, ref row, $"Mean N ({unit})", 
            filtered.System1MeanNorthing, filtered.System2MeanNorthing, filtered.DeltaMeanNorthing, false);
        
        // Format columns
        for (int c = 1; c <= 9; c++)
            ws.Column(c).Width = 16;
    }
    
    private void CreateStatTable(IXLWorksheet ws, ref int row, int startCol, 
        GnssStatistics stats, string unit, XLColor headerColor)
    {
        // Headers
        ws.Cell(row, startCol).Value = "Metric";
        ws.Cell(row, startCol + 1).Value = $"ΔE ({unit})";
        ws.Cell(row, startCol + 2).Value = $"ΔN ({unit})";
        ws.Cell(row, startCol + 3).Value = $"Radial ({unit})";
        
        var headerRange = ws.Range(row, startCol, row, startCol + 3);
        headerRange.Style.Fill.BackgroundColor = headerColor;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row++;
        
        // Data rows
        AddStatRow(ws, ref row, startCol, "Mean", stats.DeltaMeanEasting, stats.DeltaMeanNorthing, stats.MeanRadialError, false);
        AddStatRow(ws, ref row, startCol, "Std Dev (σ)", stats.DeltaSdEasting, stats.DeltaSdNorthing, stats.StdDevRadialError, true);
        AddStatRow(ws, ref row, startCol, "Max", stats.DeltaMaxEasting, stats.DeltaMaxNorthing, stats.MaxRadialError, false);
        AddStatRow(ws, ref row, startCol, "Min", stats.DeltaMinEasting, stats.DeltaMinNorthing, 0, true);
        
        // 2DRMS highlight row
        ws.Cell(row, startCol).Value = "2DRMS";
        ws.Cell(row, startCol + 1).Value = stats.Delta2DRMS;
        ws.Range(row, startCol, row, startCol + 3).Merge();
        ws.Range(row, startCol, row, startCol + 3).Style.Fill.BackgroundColor = AccentOrange;
        ws.Range(row, startCol, row, startCol + 3).Style.Font.FontColor = XLColor.White;
        ws.Range(row, startCol, row, startCol + 3).Style.Font.Bold = true;
        ws.Range(row, startCol, row, startCol + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, startCol + 1).Style.NumberFormat.Format = "0.0000";
        row++;
        
        // Additional metrics
        AddStatRow(ws, ref row, startCol, "CEP 50%", stats.Cep50, 0, 0, false, true);
        AddStatRow(ws, ref row, startCol, "CEP 95%", stats.Cep95, 0, 0, true, true);
        AddStatRow(ws, ref row, startCol, "Spread", stats.Spread, 0, 0, false, true);
    }
    
    private void AddStatRow(IXLWorksheet ws, ref int row, int startCol, string label, 
        double val1, double val2, double val3, bool alternate, bool singleValue = false)
    {
        ws.Cell(row, startCol).Value = label;
        ws.Cell(row, startCol + 1).Value = val1;
        ws.Cell(row, startCol + 1).Style.NumberFormat.Format = "0.0000";
        
        if (!singleValue)
        {
            ws.Cell(row, startCol + 2).Value = val2;
            ws.Cell(row, startCol + 3).Value = val3;
            ws.Cell(row, startCol + 2).Style.NumberFormat.Format = "0.0000";
            ws.Cell(row, startCol + 3).Style.NumberFormat.Format = "0.0000";
        }
        else
        {
            ws.Range(row, startCol + 1, row, startCol + 3).Merge();
        }
        
        if (alternate)
            ws.Range(row, startCol, row, startCol + 3).Style.Fill.BackgroundColor = LightGray;
        
        ws.Cell(row, startCol).Style.Font.Bold = true;
        row++;
    }
    
    private void AddComparisonRow(IXLWorksheet ws, ref int row, string label,
        double sys1, double sys2, double delta, bool highlight)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = sys1;
        ws.Cell(row, 3).Value = sys2;
        ws.Cell(row, 4).Value = delta;
        
        ws.Cell(row, 2).Style.NumberFormat.Format = "0.0000";
        ws.Cell(row, 3).Style.NumberFormat.Format = "0.0000";
        ws.Cell(row, 4).Style.NumberFormat.Format = "0.0000";
        
        if (highlight)
        {
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = LightBlue;
            ws.Range(row, 1, row, 4).Style.Font.Bold = true;
        }
        else if (row % 2 == 0)
        {
            ws.Range(row, 1, row, 4).Style.Fill.BackgroundColor = LightGray;
        }
        
        row++;
    }
    
    private void CreateSystemComparisonSheet(IXLWorkbook workbook, GnssProject project, GnssStatisticsResult statistics)
    {
        var ws = workbook.Worksheets.Add("System Comparison");
        string unit = project.CoordinateUnit ?? "m";
        int row = 1;
        
        ws.Cell(row, 1).Value = "SYSTEM COMPARISON SUMMARY";
        StyleTitleCell(ws.Cell(row, 1));
        ws.Range(row, 1, row, 5).Merge();
        row += 2;
        
        var stats = statistics.FilteredStatistics;
        
        // Comparison table
        ws.Cell(row, 1).Value = "";
        ws.Cell(row, 2).Value = project.System1Name;
        ws.Cell(row, 3).Value = project.System2Name;
        ws.Cell(row, 4).Value = "Difference";
        ws.Cell(row, 5).Value = "Unit";
        StyleTableHeader(ws.Range(row, 1, row, 5));
        row++;
        
        // 2DRMS
        AddCompRow(ws, ref row, "2DRMS", stats.System1_2DRMS, stats.System2_2DRMS, 
            stats.System1_2DRMS - stats.System2_2DRMS, unit, true);
        
        // Spread
        AddCompRow(ws, ref row, "Spread", stats.System1Spread, stats.System2Spread,
            stats.System1Spread - stats.System2Spread, unit, false);
        
        // Mean Easting
        AddCompRow(ws, ref row, "Mean Easting", stats.System1MeanEasting, stats.System2MeanEasting,
            stats.DeltaMeanEasting, unit, true);
        
        // Mean Northing
        AddCompRow(ws, ref row, "Mean Northing", stats.System1MeanNorthing, stats.System2MeanNorthing,
            stats.DeltaMeanNorthing, unit, false);
        
        // Mean Height
        AddCompRow(ws, ref row, "Mean Height", stats.System1MeanHeight, stats.System2MeanHeight,
            stats.DeltaMeanHeight, unit, true);
        
        // SD Easting
        AddCompRow(ws, ref row, "Std Dev Easting", stats.System1SdEasting, stats.System2SdEasting,
            stats.System1SdEasting - stats.System2SdEasting, unit, false);
        
        // SD Northing
        AddCompRow(ws, ref row, "Std Dev Northing", stats.System1SdNorthing, stats.System2SdNorthing,
            stats.System1SdNorthing - stats.System2SdNorthing, unit, true);
        
        // Format
        for (int c = 1; c <= 5; c++)
            ws.Column(c).Width = 18;
    }
    
    private void AddCompRow(IXLWorksheet ws, ref int row, string label, 
        double sys1, double sys2, double diff, string unit, bool alternate)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = sys1;
        ws.Cell(row, 3).Value = sys2;
        ws.Cell(row, 4).Value = diff;
        ws.Cell(row, 5).Value = unit;
        
        ws.Cell(row, 2).Style.NumberFormat.Format = "0.0000";
        ws.Cell(row, 3).Style.NumberFormat.Format = "0.0000";
        ws.Cell(row, 4).Style.NumberFormat.Format = "0.0000";
        
        // Color difference based on magnitude
        if (Math.Abs(diff) > 0.1)
            ws.Cell(row, 4).Style.Font.FontColor = ErrorRed;
        else if (Math.Abs(diff) > 0.01)
            ws.Cell(row, 4).Style.Font.FontColor = AccentOrange;
        else
            ws.Cell(row, 4).Style.Font.FontColor = SuccessGreen;
        
        if (alternate)
            ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = LightGray;
        
        row++;
    }
    
    private void CreateDataSheet(IXLWorkbook workbook, string sheetName, GnssProject project,
        List<GnssComparisonPoint> points, XLColor headerColor)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        string unit = project.CoordinateUnit ?? "m";
        int row = 1;
        
        // Headers
        var headers = new[]
        {
            "#", "DateTime",
            $"Sys1 E ({unit})", $"Sys1 N ({unit})", $"Sys1 H ({unit})",
            $"Sys2 E ({unit})", $"Sys2 N ({unit})", $"Sys2 H ({unit})",
            $"ΔE ({unit})", $"ΔN ({unit})", $"ΔH ({unit})",
            $"Radial ({unit})", "Status"
        };
        
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(row, c + 1).Value = headers[c];
        }
        
        var headerRange = ws.Range(row, 1, row, headers.Length);
        headerRange.Style.Fill.BackgroundColor = headerColor;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        headerRange.Style.Border.BottomBorderColor = headerColor;
        row++;
        
        // Data rows
        foreach (var point in points)
        {
            int c = 1;
            ws.Cell(row, c++).Value = point.Index;
            ws.Cell(row, c++).Value = point.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, c++).Value = point.System1Easting;
            ws.Cell(row, c++).Value = point.System1Northing;
            ws.Cell(row, c++).Value = point.System1Height;
            ws.Cell(row, c++).Value = point.System2Easting;
            ws.Cell(row, c++).Value = point.System2Northing;
            ws.Cell(row, c++).Value = point.System2Height;
            ws.Cell(row, c++).Value = point.DeltaEasting;
            ws.Cell(row, c++).Value = point.DeltaNorthing;
            ws.Cell(row, c++).Value = point.DeltaHeight;
            ws.Cell(row, c++).Value = point.RadialError;
            ws.Cell(row, c++).Value = point.IsRejected ? "Rejected" : "Accepted";
            
            // Format numbers
            for (int col = 3; col <= 12; col++)
                ws.Cell(row, col).Style.NumberFormat.Format = "0.0000";
            
            // Alternate row colors
            if (row % 2 == 0)
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = LightGray;
            
            // Status color
            if (point.IsRejected)
            {
                ws.Cell(row, 13).Style.Font.FontColor = ErrorRed;
                ws.Cell(row, 13).Style.Font.Bold = true;
            }
            else
            {
                ws.Cell(row, 13).Style.Font.FontColor = SuccessGreen;
            }
            
            row++;
        }
        
        // Auto-fit columns
        ws.Columns().AdjustToContents();
        
        // Freeze header row
        ws.SheetView.FreezeRows(1);
        
        // Add filter
        if (points.Count > 0)
            ws.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
    }
    
    private void StyleTitleCell(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontSize = 18;
        cell.Style.Font.FontColor = PrimaryBlue;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
    
    private void StyleSectionHeader(IXLRange range)
    {
        range.Merge();
        range.Style.Fill.BackgroundColor = PrimaryBlue;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = 11;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        range.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        range.Style.Border.BottomBorderColor = AccentOrange;
    }
    
    private void StyleTableHeader(IXLRange range)
    {
        range.Style.Fill.BackgroundColor = PrimaryBlue;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Font.Bold = true;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
    
    private void AddInfoRow(IXLWorksheet ws, ref int row, string label, string value)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontColor = DarkText;
        ws.Cell(row, 2).Value = value;
        
        if (row % 2 == 0)
            ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = LightGray;
        
        row++;
    }
}
