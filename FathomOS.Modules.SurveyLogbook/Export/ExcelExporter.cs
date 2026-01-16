// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Export/ExcelExporter.cs
// Purpose: Excel export using ClosedXML (from Core)
// Note: Uses ClosedXML as per integration guide - NO EPPlus or NPOI
// ============================================================================

using System.IO;
using ClosedXML.Excel;
using FathomOS.Modules.SurveyLogbook.Models;
using System.Linq;

namespace FathomOS.Modules.SurveyLogbook.Export;

/// <summary>
/// Excel exporter for survey logbook data using ClosedXML.
/// Creates formatted workbooks with multiple sheets for different data types.
/// </summary>
public class ExcelExporter
{
    #region Fields

    private readonly ExcelExportOptions _options;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ExcelExporter class.
    /// </summary>
    public ExcelExporter() : this(new ExcelExportOptions()) { }

    /// <summary>
    /// Initializes a new instance of the ExcelExporter class with specified options.
    /// </summary>
    public ExcelExporter(ExcelExportOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Exports survey log data to an Excel file.
    /// </summary>
    public void Export(string filePath, SurveyLogFile logFile)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        if (logFile == null)
            throw new ArgumentNullException(nameof(logFile));

        using var workbook = new XLWorkbook();
        
        // Create sheets
        CreateSummarySheet(workbook, logFile);
        CreateLogEntriesSheet(workbook, logFile);
        
        if (_options.IncludePositionFixes && logFile.PositionFixes.Any())
            CreatePositionFixesSheet(workbook, logFile);
        
        if (_options.IncludeDvrRecordings && logFile.DvrRecordings.Any())
            CreateDvrRecordingsSheet(workbook, logFile);
        
        if (_options.IncludeDprReports && logFile.DprReports.Any())
            CreateDprReportsSheet(workbook, logFile);

        // Save the workbook
        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// Exports a list of log entries to an Excel file.
    /// </summary>
    public void ExportEntries(string filePath, IEnumerable<SurveyLogEntry> entries, string? projectName = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Survey Log");

        // Set up headers
        var headers = new[] { "Date", "Time", "Type", "Description", "Easting", "Northing", "KP", "DCC", "Depth", "Source", "Vehicle" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = headers[i];
        }

        // Style header row
        var headerRange = worksheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Add data rows
        int row = 2;
        foreach (var entry in entries.OrderByDescending(e => e.Timestamp))
        {
            worksheet.Cell(row, 1).Value = entry.Timestamp.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 2).Value = entry.Timestamp.ToString("HH:mm:ss");
            worksheet.Cell(row, 3).Value = entry.EntryType.ToString();
            worksheet.Cell(row, 4).Value = entry.Description ?? "";
            worksheet.Cell(row, 5).Value = entry.Easting;
            worksheet.Cell(row, 6).Value = entry.Northing;
            worksheet.Cell(row, 7).Value = entry.Kp;
            worksheet.Cell(row, 8).Value = entry.Dcc;
            worksheet.Cell(row, 9).Value = entry.Depth;
            worksheet.Cell(row, 10).Value = entry.Source ?? "";
            worksheet.Cell(row, 11).Value = entry.Vehicle ?? "";

            // Format numeric cells
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.000";
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.000";
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "0.000";
            worksheet.Cell(row, 8).Style.NumberFormat.Format = "0.00";
            worksheet.Cell(row, 9).Style.NumberFormat.Format = "0.00";

            row++;
        }

        // Apply alternating row colors
        if (_options.ApplyFormatting)
        {
            for (int r = 2; r < row; r++)
            {
                if (r % 2 == 0)
                {
                    worksheet.Range(r, 1, r, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F7FA");
                }
            }
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Freeze header row
        worksheet.SheetView.Freeze(1, 0);

        workbook.SaveAs(filePath);
    }

    #endregion

    #region Private Methods - Sheet Creation

    private void CreateSummarySheet(IXLWorkbook workbook, SurveyLogFile logFile)
    {
        var ws = workbook.Worksheets.Add("Summary");

        // Title
        ws.Cell(1, 1).Value = "Survey Electronic Logbook Export";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, 4).Merge();

        // Project info
        int row = 3;
        AddSummaryRow(ws, ref row, "Project", logFile.ProjectInfo?.ProjectName);
        AddSummaryRow(ws, ref row, "Vessel", logFile.ProjectInfo?.Vessel);
        AddSummaryRow(ws, ref row, "Client", logFile.ProjectInfo?.Client);
        AddSummaryRow(ws, ref row, "Export Date", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        AddSummaryRow(ws, ref row, "Exported By", logFile.ExportedBy);

        row++;
        AddSummaryRow(ws, ref row, "Log Entries", logFile.LogEntries.Count().ToString());
        AddSummaryRow(ws, ref row, "Position Fixes", logFile.PositionFixes.Count.ToString());
        AddSummaryRow(ws, ref row, "DVR Recordings", logFile.DvrRecordings.Count.ToString());
        AddSummaryRow(ws, ref row, "DPR Reports", logFile.DprReports.Count.ToString());

        if (logFile.LogEntries.Any())
        {
            row++;
            AddSummaryRow(ws, ref row, "First Entry", logFile.LogEntries.Min(e => e.Timestamp).ToString("dd/MM/yyyy HH:mm:ss"));
            AddSummaryRow(ws, ref row, "Last Entry", logFile.LogEntries.Max(e => e.Timestamp).ToString("dd/MM/yyyy HH:mm:ss"));
        }

        ws.Columns().AdjustToContents();
    }

    private void AddSummaryRow(IXLWorksheet ws, ref int row, string label, string? value)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = value ?? "-";
        row++;
    }

    private void CreateLogEntriesSheet(IXLWorkbook workbook, SurveyLogFile logFile)
    {
        var ws = workbook.Worksheets.Add("Log Entries");

        // Headers
        var headers = new[] { "ID", "Date", "Time", "Type", "Description", "Easting", "Northing", "KP", "DCC", "Depth", "Heading", "Source", "Vehicle", "Comments" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }
        StyleHeaderRow(ws, 1, headers.Length);

        // Data rows
        int row = 2;
        foreach (var entry in logFile.LogEntries.OrderByDescending(e => e.Timestamp))
        {
            ws.Cell(row, 1).Value = entry.Id.ToString();
            ws.Cell(row, 2).Value = entry.Timestamp.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = entry.Timestamp.ToString("HH:mm:ss.fff");
            ws.Cell(row, 4).Value = entry.EntryType.ToString();
            ws.Cell(row, 5).Value = entry.Description ?? "";
            ws.Cell(row, 6).Value = entry.Easting;
            ws.Cell(row, 7).Value = entry.Northing;
            ws.Cell(row, 8).Value = entry.Kp;
            ws.Cell(row, 9).Value = entry.Dcc;
            ws.Cell(row, 10).Value = entry.Depth;
            ws.Cell(row, 11).Value = entry.Heading;
            ws.Cell(row, 12).Value = entry.Source ?? "";
            ws.Cell(row, 13).Value = entry.Vehicle ?? "";
            ws.Cell(row, 14).Value = entry.Comments ?? "";

            // Number formatting
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.000";
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.000";
            ws.Cell(row, 8).Style.NumberFormat.Format = "0.000";
            ws.Cell(row, 9).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 10).Style.NumberFormat.Format = "0.00";
            ws.Cell(row, 11).Style.NumberFormat.Format = "0.00";

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.Freeze(1, 0);
    }

    private void CreatePositionFixesSheet(IXLWorkbook workbook, SurveyLogFile logFile)
    {
        var ws = workbook.Worksheets.Add("Position Fixes");

        var headers = new[] { "Timestamp", "Object", "Type", "Easting", "Northing", "Height", "Error E", "Error N", "Error H", "Std Dev E", "Std Dev N", "Count", "Source File" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }
        StyleHeaderRow(ws, 1, headers.Length);

        int row = 2;
        foreach (var fix in logFile.PositionFixes.OrderByDescending(f => f.Timestamp))
        {
            ws.Cell(row, 1).Value = fix.Timestamp.ToString("dd/MM/yyyy HH:mm:ss");
            ws.Cell(row, 2).Value = fix.ObjectMonitored ?? "";
            ws.Cell(row, 3).Value = fix.FixType.ToString();
            ws.Cell(row, 4).Value = fix.Easting;
            ws.Cell(row, 5).Value = fix.Northing;
            ws.Cell(row, 6).Value = fix.Height;
            ws.Cell(row, 7).Value = fix.EastingStats?.Error;
            ws.Cell(row, 8).Value = fix.NorthingStats?.Error;
            ws.Cell(row, 9).Value = fix.HeightStats?.Error;
            ws.Cell(row, 10).Value = fix.SdEasting;
            ws.Cell(row, 11).Value = fix.SdNorthing;
            ws.Cell(row, 12).Value = fix.NumberOfFixes;
            ws.Cell(row, 13).Value = fix.SourceFile ?? "";

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.Freeze(1, 0);
    }

    private void CreateDvrRecordingsSheet(IXLWorkbook workbook, SurveyLogFile logFile)
    {
        var ws = workbook.Worksheets.Add("DVR Recordings");

        var headers = new[] { "Vehicle", "Task", "Sub-Task", "Operation", "Start Time", "End Time", "Duration", "Video Count", "Image Count", "Folder Path" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }
        StyleHeaderRow(ws, 1, headers.Length);

        int row = 2;
        foreach (var dvr in logFile.DvrRecordings.OrderByDescending(d => d.StartTime))
        {
            ws.Cell(row, 1).Value = dvr.Vehicle ?? "";
            ws.Cell(row, 2).Value = dvr.ProjectTask ?? "";
            ws.Cell(row, 3).Value = dvr.SubTask ?? "";
            ws.Cell(row, 4).Value = dvr.Operation ?? "";
            ws.Cell(row, 5).Value = dvr.StartDateTime.ToString("dd/MM/yyyy HH:mm:ss");
            ws.Cell(row, 6).Value = dvr.Date.Add(dvr.EndTime).ToString("dd/MM/yyyy HH:mm:ss");
            ws.Cell(row, 7).Value = dvr.Duration.ToString(@"hh\:mm\:ss");
            ws.Cell(row, 8).Value = dvr.VideoFiles.Count;
            ws.Cell(row, 9).Value = dvr.ImageFiles.Count;
            ws.Cell(row, 10).Value = dvr.FolderPath ?? "";

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.Freeze(1, 0);
    }

    private void CreateDprReportsSheet(IXLWorkbook workbook, SurveyLogFile logFile)
    {
        var ws = workbook.Worksheets.Add("DPR Reports");

        var headers = new[] { "Date", "Shift", "Client", "Vessel", "Project", "Offshore Manager", "Project Surveyor", "Party Chief", "Crew Count", "Highlights", "Issues" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }
        StyleHeaderRow(ws, 1, headers.Length);

        int row = 2;
        foreach (var dpr in logFile.DprReports.OrderByDescending(d => d.ReportDate))
        {
            ws.Cell(row, 1).Value = dpr.ReportDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = dpr.Shift;
            ws.Cell(row, 3).Value = dpr.Client ?? "";
            ws.Cell(row, 4).Value = dpr.Vessel ?? "";
            ws.Cell(row, 5).Value = dpr.ProjectNumber ?? "";
            ws.Cell(row, 6).Value = dpr.OffshoreManager ?? "";
            ws.Cell(row, 7).Value = dpr.ProjectSurveyor ?? "";
            ws.Cell(row, 8).Value = dpr.PartyChief ?? "";
            ws.Cell(row, 9).Value = dpr.CrewOnDuty?.Count ?? 0;
            ws.Cell(row, 10).Value = dpr.Last24HrsHighlights ?? "";
            ws.Cell(row, 11).Value = dpr.KnownIssues ?? "";

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.Freeze(1, 0);
    }

    private void StyleHeaderRow(IXLWorksheet ws, int row, int columnCount)
    {
        var headerRange = ws.Range(row, 1, row, columnCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
    }

    #endregion
}

/// <summary>
/// Options for Excel export.
/// </summary>
public class ExcelExportOptions
{
    /// <summary>
    /// Include position fixes sheet.
    /// </summary>
    public bool IncludePositionFixes { get; set; } = true;

    /// <summary>
    /// Include DVR recordings sheet.
    /// </summary>
    public bool IncludeDvrRecordings { get; set; } = true;

    /// <summary>
    /// Include DPR reports sheet.
    /// </summary>
    public bool IncludeDprReports { get; set; } = true;

    /// <summary>
    /// Apply formatting (colors, borders, etc.).
    /// </summary>
    public bool ApplyFormatting { get; set; } = true;
}
