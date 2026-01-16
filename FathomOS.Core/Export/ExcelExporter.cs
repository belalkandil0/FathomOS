using ClosedXML.Excel;
using FathomOS.Core.Models;

namespace FathomOS.Core.Export;

/// <summary>
/// Exports survey data to Excel workbook (.xlsx) using ClosedXML
/// </summary>
public class ExcelExporter
{
    private readonly ExcelExportOptions _options;
    private ReportTemplate? _template;
    private string? _logoPath;

    public ExcelExporter(ExcelExportOptions? options = null)
    {
        _options = options ?? new ExcelExportOptions();
    }
    
    /// <summary>
    /// Set report template for branding
    /// </summary>
    public void SetTemplate(ReportTemplate template, string? logoPath = null)
    {
        _template = template;
        _logoPath = logoPath;
    }

    /// <summary>
    /// Export survey data to Excel workbook
    /// </summary>
    public void Export(string filePath, IList<SurveyPoint> points, Project project)
    {
        Export(filePath, points, project, null, null);
    }
    
    /// <summary>
    /// Export survey data to Excel workbook with spline and interval points
    /// </summary>
    public void Export(string filePath, IList<SurveyPoint> points, Project project,
        IList<SurveyPoint>? splinePoints, IList<(double X, double Y, double Z, double Distance)>? intervalPoints)
    {
        using var workbook = new XLWorkbook();

        // Create summary sheet with template header
        CreateSummarySheet(workbook, project, points, splinePoints, intervalPoints);

        // Create Survey Listing sheet (always included - main output)
        CreateSurveyListingSheet(workbook, points, project);

        // Create raw data sheet if requested
        if (_options.IncludeRawData)
        {
            CreateRawDataSheet(workbook, points);
        }

        // Create calculations sheet if requested
        if (_options.IncludeCalculations)
        {
            CreateCalculationsSheet(workbook, points, project);
        }
        
        // Create smoothed data comparison sheet
        if (_options.IncludeSmoothedData)
        {
            CreateSmoothedDataSheet(workbook, points);
        }
        
        // Create spline fitted sheet
        if (_options.IncludeSplineData && splinePoints != null && splinePoints.Count > 0)
        {
            CreateSplineSheet(workbook, splinePoints);
        }
        
        // Create interval points sheet
        if (_options.IncludeIntervalPoints && intervalPoints != null && intervalPoints.Count > 0)
        {
            CreateIntervalPointsSheet(workbook, intervalPoints);
        }
        
        // Apply print settings with header/footer to all sheets
        ApplyPrintSettings(workbook, project);

        workbook.SaveAs(filePath);
    }
    
    /// <summary>
    /// Apply print settings with header/footer to all worksheets
    /// </summary>
    private void ApplyPrintSettings(IXLWorkbook workbook, Project project)
    {
        foreach (var sheet in workbook.Worksheets)
        {
            // Page setup
            sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
            sheet.PageSetup.FitToPages(1, 0); // Fit to 1 page wide, unlimited height
            sheet.PageSetup.Margins.SetLeft(0.5);
            sheet.PageSetup.Margins.SetRight(0.5);
            sheet.PageSetup.Margins.SetTop(0.75);
            sheet.PageSetup.Margins.SetBottom(0.75);
            sheet.PageSetup.Margins.SetHeader(0.3);
            sheet.PageSetup.Margins.SetFooter(0.3);
            
            // Header with company/project info
            string headerLeft = _template?.Company.Name ?? "Fathom OS";
            string headerCenter = _template?.Header.Title ?? "SURVEY LISTING REPORT";
            string headerRight = $"{project.ProjectName}";
            
            sheet.PageSetup.Header.Left.AddText(headerLeft, XLHFOccurrence.AllPages);
            sheet.PageSetup.Header.Center.AddText(headerCenter, XLHFOccurrence.AllPages);
            sheet.PageSetup.Header.Right.AddText(headerRight, XLHFOccurrence.AllPages);
            
            // Footer with page numbers and date
            string footerLeft = ReportTemplate.ReplacePlaceholders(
                _template?.Footer.LeftText ?? "{ProjectName}", project);
            string footerRight = ReportTemplate.ReplacePlaceholders(
                _template?.Footer.RightText ?? "{GeneratedDate}", project);
            
            sheet.PageSetup.Footer.Left.AddText(footerLeft, XLHFOccurrence.AllPages);
            sheet.PageSetup.Footer.Center.AddText("Page ", XLHFOccurrence.AllPages);
            sheet.PageSetup.Footer.Center.AddText(XLHFPredefinedText.PageNumber, XLHFOccurrence.AllPages);
            sheet.PageSetup.Footer.Center.AddText(" of ", XLHFOccurrence.AllPages);
            sheet.PageSetup.Footer.Center.AddText(XLHFPredefinedText.NumberOfPages, XLHFOccurrence.AllPages);
            sheet.PageSetup.Footer.Right.AddText(footerRight, XLHFOccurrence.AllPages);
            
            // Print titles (repeat header row on each page)
            sheet.PageSetup.SetRowsToRepeatAtTop(1, 1);
        }
    }
    
    private void CreateSummarySheet(IXLWorkbook workbook, Project project, IList<SurveyPoint> points,
        IList<SurveyPoint>? splinePoints = null, IList<(double X, double Y, double Z, double Distance)>? intervalPoints = null)
    {
        var sheet = workbook.Worksheets.Add("Summary");
        
        int row = 1;
        
        // Logo placeholder area (row 1-3, column A-B)
        if (_template?.Header.ShowLogo == true && !string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
        {
            try
            {
                var picture = sheet.AddPicture(_logoPath);
                picture.MoveTo(sheet.Cell("A1"));
                picture.Scale(0.5); // Adjust scale as needed
                row = 4;
            }
            catch
            {
                // Logo loading failed, continue without it
            }
        }

        // Title with template styling
        string title = _template?.Header.Title ?? "SURVEY LISTING REPORT";
        sheet.Cell(row, 1).Value = title;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 18;
        
        // Apply template colors if available
        if (_template != null)
        {
            try
            {
                var headerColor = XLColor.FromHtml(_template.Colors.PrimaryColor);
                sheet.Cell(row, 1).Style.Font.FontColor = headerColor;
            }
            catch { }
        }
        row++;
        
        // Company name
        if (_template?.Header.ShowCompanyName == true)
        {
            sheet.Cell(row, 1).Value = _template.Company.Name;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;
        }
        row++;

        // Project Info section
        sheet.Cell(row, 1).Value = "PROJECT INFORMATION";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 12;
        try
        {
            var sectionColor = XLColor.FromHtml(_template?.Colors.SecondaryColor ?? "#4A90D9");
            sheet.Cell(row, 1).Style.Font.FontColor = sectionColor;
        }
        catch { }
        row++;
        
        AddInfoRow(sheet, ref row, "Project:", project.ProjectName);
        AddInfoRow(sheet, ref row, "Client:", project.ClientName);
        AddInfoRow(sheet, ref row, "Vessel:", project.VesselName);
        AddInfoRow(sheet, ref row, "Processor:", project.ProcessorName);
        AddInfoRow(sheet, ref row, "Product:", project.ProductName ?? "");
        AddInfoRow(sheet, ref row, "ROV:", project.RovName ?? "");
        AddInfoRow(sheet, ref row, "Survey Date:", project.SurveyDate?.ToString("yyyy-MM-dd") ?? "");
        AddInfoRow(sheet, ref row, "Survey Type:", project.SurveyType.ToString());
        AddInfoRow(sheet, ref row, "Coordinate System:", project.CoordinateSystem);
        AddInfoRow(sheet, ref row, "Coordinate Units:", project.CoordinateUnit.GetDisplayName());
        AddInfoRow(sheet, ref row, "KP Units:", project.KpUnit.GetDisplayName());

        row += 2;
        sheet.Cell(row, 1).Value = "STATISTICS";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 12;
        try
        {
            var sectionColor = XLColor.FromHtml(_template?.Colors.SecondaryColor ?? "#4A90D9");
            sheet.Cell(row, 1).Style.Font.FontColor = sectionColor;
        }
        catch { }
        row++;

        // Calculate statistics using CalculatedZ
        AddInfoRow(sheet, ref row, "Total Records:", points.Count.ToString("N0"));
        
        if (points.Count > 0)
        {
            var startTime = points.Min(p => p.DateTime);
            var endTime = points.Max(p => p.DateTime);
            AddInfoRow(sheet, ref row, "Start Time:", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
            AddInfoRow(sheet, ref row, "End Time:", endTime.ToString("yyyy-MM-dd HH:mm:ss"));
            AddInfoRow(sheet, ref row, "Duration:", (endTime - startTime).ToString(@"hh\:mm\:ss"));

            // Use CalculatedZ for depth statistics
            var depths = points.Where(p => p.CalculatedZ.HasValue).Select(p => p.CalculatedZ!.Value).ToList();
            if (depths.Count > 0)
            {
                AddInfoRow(sheet, ref row, "Min Z (Seabed):", $"{depths.Min():F2}");
                AddInfoRow(sheet, ref row, "Max Z (Seabed):", $"{depths.Max():F2}");
                AddInfoRow(sheet, ref row, "Avg Z (Seabed):", $"{depths.Average():F2}");
            }

            var kps = points.Where(p => p.Kp.HasValue).Select(p => p.Kp!.Value).ToList();
            if (kps.Count > 0)
            {
                AddInfoRow(sheet, ref row, "Start KP:", $"{kps.Min():F6}");
                AddInfoRow(sheet, ref row, "End KP:", $"{kps.Max():F6}");
                AddInfoRow(sheet, ref row, "KP Range:", $"{(kps.Max() - kps.Min()):F6}");
            }
            
            // Coordinate range (using X, Y)
            AddInfoRow(sheet, ref row, "Easting Range:", $"{points.Min(p => p.X):F2} to {points.Max(p => p.X):F2}");
            AddInfoRow(sheet, ref row, "Northing Range:", $"{points.Min(p => p.Y):F2} to {points.Max(p => p.Y):F2}");
            
            // Smoothing statistics
            var smoothedCount = points.Count(p => p.SmoothedEasting.HasValue);
            if (smoothedCount > 0)
            {
                AddInfoRow(sheet, ref row, "Smoothed Points:", smoothedCount.ToString("N0"));
                
                // Calculate smoothing statistics
                var smoothedDiffs = points
                    .Where(p => p.SmoothedEasting.HasValue && p.SmoothedNorthing.HasValue)
                    .Select(p => Math.Sqrt(
                        Math.Pow(p.SmoothedEasting!.Value - p.Easting, 2) +
                        Math.Pow(p.SmoothedNorthing!.Value - p.Northing, 2)))
                    .ToList();
                
                if (smoothedDiffs.Count > 0)
                {
                    AddInfoRow(sheet, ref row, "Avg Smoothing Shift:", $"{smoothedDiffs.Average():F3}");
                    AddInfoRow(sheet, ref row, "Max Smoothing Shift:", $"{smoothedDiffs.Max():F3}");
                }
            }
        }
        
        // Spline and Interval statistics
        if (splinePoints != null && splinePoints.Count > 0)
        {
            AddInfoRow(sheet, ref row, "Spline Fitted Points:", splinePoints.Count.ToString("N0"));
        }
        
        if (intervalPoints != null && intervalPoints.Count > 0)
        {
            AddInfoRow(sheet, ref row, "Interval Points:", intervalPoints.Count.ToString("N0"));
            if (intervalPoints.Count > 1)
            {
                var interval = intervalPoints[1].Distance - intervalPoints[0].Distance;
                AddInfoRow(sheet, ref row, "Point Interval:", $"{interval:F2} m");
            }
        }

        row += 2;
        sheet.Cell(row, 1).Value = "EXPORT INFORMATION";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        row++;
        AddInfoRow(sheet, ref row, "Generated:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        AddInfoRow(sheet, ref row, "Generator:", AppInfo.GeneratorString);
        AddInfoRow(sheet, ref row, "Template:", _template?.Company.Name ?? "Default");

        // Auto-fit columns
        sheet.Column(1).AdjustToContents();
        sheet.Column(2).AdjustToContents();
        
        // Add border around info section
        var infoRange = sheet.Range(1, 1, row, 2);
        infoRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    /// <summary>
    /// Create the main Survey Listing sheet with KP, DCC, X, Y, Z
    /// </summary>
    private void CreateSurveyListingSheet(IXLWorkbook workbook, IList<SurveyPoint> points, Project project)
    {
        var sheet = workbook.Worksheets.Add("Survey Listing");

        // Title block
        sheet.Cell("A1").Value = "SURVEY LISTING";
        sheet.Cell("A1").Style.Font.Bold = true;
        sheet.Cell("A1").Style.Font.FontSize = 14;

        sheet.Cell("A2").Value = $"Project: {project.ProjectName}";
        sheet.Cell("A3").Value = $"Client: {project.ClientName}";
        sheet.Cell("A4").Value = $"Coordinate System: {project.CoordinateSystem}";

        // Headers - KP, DCC, X, Y, Z format
        int headerRow = 6;
        var headers = new[] { "KP", "DCC", "X", "Y", "Z" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(headerRow, i + 1).Value = headers[i];
            sheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
            sheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.DarkBlue;
            sheet.Cell(headerRow, i + 1).Style.Font.FontColor = XLColor.White;
            sheet.Cell(headerRow, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data - using calculated values
        int row = headerRow + 1;
        foreach (var point in points)
        {
            // Include all points with valid calculated Z
            // KP and DCC may be 0 if not calculated
            if (!point.CalculatedZ.HasValue)
                continue;

            sheet.Cell(row, 1).Value = point.Kp ?? 0;  // 0 if KP not calculated
            sheet.Cell(row, 2).Value = point.Dcc ?? 0;
            sheet.Cell(row, 3).Value = point.X;  // Smoothed or raw Easting
            sheet.Cell(row, 4).Value = point.Y;  // Smoothed or raw Northing
            sheet.Cell(row, 5).Value = point.CalculatedZ;  // Calculated seabed depth

            // Alternate row coloring
            if (_options.ApplyFormatting && row % 2 == 0)
            {
                sheet.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }
            row++;
        }

        if (_options.ApplyFormatting)
        {
            FormatDataSheet(sheet, row - 1, headerRow);

            // Number formats
            sheet.Column(1).Style.NumberFormat.Format = "0.000000";   // KP
            sheet.Column(2).Style.NumberFormat.Format = "0.000";      // DCC
            sheet.Column(3).Style.NumberFormat.Format = "#,##0.00";   // X
            sheet.Column(4).Style.NumberFormat.Format = "#,##0.00";   // Y
            sheet.Column(5).Style.NumberFormat.Format = "0.00";       // Z

            // Freeze header
            sheet.SheetView.FreezeRows(headerRow);
        }
    }

    private void CreateRawDataSheet(IXLWorkbook workbook, IList<SurveyPoint> points)
    {
        var sheet = workbook.Worksheets.Add("Raw Data");

        // Headers - original NPD data
        var headers = new[] { "RecNo", "DateTime", "Easting", "Northing", "Depth", "Altitude", "Heading" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
            sheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data - original raw values
        int row = 2;
        foreach (var point in points)
        {
            sheet.Cell(row, 1).Value = point.RecordNumber;
            sheet.Cell(row, 2).Value = point.DateTime;
            sheet.Cell(row, 3).Value = point.Easting;   // Original Easting
            sheet.Cell(row, 4).Value = point.Northing;  // Original Northing
            sheet.Cell(row, 5).Value = point.Depth;     // Original Depth
            sheet.Cell(row, 6).Value = point.Altitude;  // Original Altitude
            sheet.Cell(row, 7).Value = point.Heading;
            row++;
        }

        // Format
        if (_options.ApplyFormatting)
        {
            FormatDataSheet(sheet, row - 1);
        }
    }

    private void CreateCalculationsSheet(IXLWorkbook workbook, IList<SurveyPoint> points, Project project)
    {
        var sheet = workbook.Worksheets.Add("Calculations");

        // Description row
        sheet.Cell("A1").Value = "Z Calculation: Z = Depth + Altitude + Offset - Tide";
        sheet.Cell("A1").Style.Font.Italic = true;
        var offsetVal = project.ProcessingOptions.ApplyVerticalOffsets && project.ProcessingOptions.BathyToAltimeterOffset.HasValue
            ? project.ProcessingOptions.BathyToAltimeterOffset.Value.ToString("F2")
            : "N/A (disabled)";
        sheet.Cell("A2").Value = $"Bathy_Altimeter_Offset: {offsetVal}";

        // Headers
        int headerRow = 4;
        var headers = new[] { "RecNo", "DateTime", "Raw Depth", "Raw Alt", "Tide", "Offset", "Calc Z", "KP", "DCC", "X", "Y" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(headerRow, i + 1).Value = headers[i];
            sheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
            sheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data showing calculation breakdown
        int row = headerRow + 1;
        foreach (var point in points)
        {
            sheet.Cell(row, 1).Value = point.RecordNumber;
            sheet.Cell(row, 2).Value = point.DateTime;
            sheet.Cell(row, 3).Value = point.Depth;
            sheet.Cell(row, 4).Value = point.Altitude;
            sheet.Cell(row, 5).Value = point.TideCorrection ?? 0;
            sheet.Cell(row, 6).Value = point.OffsetApplied ?? 0;
            sheet.Cell(row, 7).Value = point.CalculatedZ;
            sheet.Cell(row, 8).Value = point.Kp;
            sheet.Cell(row, 9).Value = point.Dcc;
            sheet.Cell(row, 10).Value = point.X;
            sheet.Cell(row, 11).Value = point.Y;
            row++;
        }

        if (_options.ApplyFormatting)
        {
            FormatDataSheet(sheet, row - 1, headerRow);
        }
    }
    
    /// <summary>
    /// Create smoothed data comparison sheet (Original vs Smoothed)
    /// </summary>
    private void CreateSmoothedDataSheet(IXLWorkbook workbook, IList<SurveyPoint> points)
    {
        // Only create if there are smoothed points
        var smoothedPoints = points.Where(p => p.SmoothedEasting.HasValue || p.SmoothedNorthing.HasValue).ToList();
        if (smoothedPoints.Count == 0) return;
        
        var sheet = workbook.Worksheets.Add("Smoothed Comparison");
        
        // Headers
        int headerRow = 1;
        var headers = new[] { "Record", "Original X", "Original Y", "Smoothed X", "Smoothed Y", "Delta X", "Delta Y", "Distance" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(headerRow, i + 1).Value = headers[i];
            sheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
            sheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.DarkGreen;
            sheet.Cell(headerRow, i + 1).Style.Font.FontColor = XLColor.White;
        }
        
        // Data
        int row = headerRow + 1;
        foreach (var point in smoothedPoints)
        {
            double smoothedX = point.SmoothedEasting ?? point.Easting;
            double smoothedY = point.SmoothedNorthing ?? point.Northing;
            double deltaX = smoothedX - point.Easting;
            double deltaY = smoothedY - point.Northing;
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            
            sheet.Cell(row, 1).Value = point.RecordNumber;
            sheet.Cell(row, 2).Value = point.Easting;
            sheet.Cell(row, 3).Value = point.Northing;
            sheet.Cell(row, 4).Value = smoothedX;
            sheet.Cell(row, 5).Value = smoothedY;
            sheet.Cell(row, 6).Value = deltaX;
            sheet.Cell(row, 7).Value = deltaY;
            sheet.Cell(row, 8).Value = distance;
            row++;
        }
        
        if (_options.ApplyFormatting)
        {
            FormatDataSheet(sheet, row - 1, headerRow);
        }
    }
    
    /// <summary>
    /// Create spline fitted points sheet
    /// </summary>
    private void CreateSplineSheet(IXLWorkbook workbook, IList<SurveyPoint> splinePoints)
    {
        var sheet = workbook.Worksheets.Add("Spline Fitted");
        
        // Headers
        int headerRow = 1;
        var headers = new[] { "Index", "X", "Y", "Z", "Distance from Start" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(headerRow, i + 1).Value = headers[i];
            sheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
            sheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.Orange;
            sheet.Cell(headerRow, i + 1).Style.Font.FontColor = XLColor.White;
        }
        
        // Data
        int row = headerRow + 1;
        double cumulativeDistance = 0;
        SurveyPoint? lastPoint = null;
        
        for (int i = 0; i < splinePoints.Count; i++)
        {
            var point = splinePoints[i];
            
            if (lastPoint != null)
            {
                double dx = point.X - lastPoint.X;
                double dy = point.Y - lastPoint.Y;
                cumulativeDistance += Math.Sqrt(dx * dx + dy * dy);
            }
            
            sheet.Cell(row, 1).Value = i + 1;
            sheet.Cell(row, 2).Value = point.X;
            sheet.Cell(row, 3).Value = point.Y;
            sheet.Cell(row, 4).Value = point.CalculatedZ ?? point.Depth ?? 0;
            sheet.Cell(row, 5).Value = cumulativeDistance;
            
            lastPoint = point;
            row++;
        }
        
        if (_options.ApplyFormatting)
        {
            FormatDataSheet(sheet, row - 1, headerRow);
        }
    }
    
    /// <summary>
    /// Create interval points sheet
    /// </summary>
    private void CreateIntervalPointsSheet(IXLWorkbook workbook, IList<(double X, double Y, double Z, double Distance)> intervalPoints)
    {
        var sheet = workbook.Worksheets.Add("Interval Points");
        
        // Headers
        int headerRow = 1;
        var headers = new[] { "Index", "Distance/DAL", "X", "Y", "Z" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cell(headerRow, i + 1).Value = headers[i];
            sheet.Cell(headerRow, i + 1).Style.Font.Bold = true;
            sheet.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.Magenta;
            sheet.Cell(headerRow, i + 1).Style.Font.FontColor = XLColor.White;
        }
        
        // Data
        int row = headerRow + 1;
        for (int i = 0; i < intervalPoints.Count; i++)
        {
            var point = intervalPoints[i];
            
            sheet.Cell(row, 1).Value = i + 1;
            sheet.Cell(row, 2).Value = point.Distance;
            sheet.Cell(row, 3).Value = point.X;
            sheet.Cell(row, 4).Value = point.Y;
            sheet.Cell(row, 5).Value = point.Z;
            row++;
        }
        
        if (_options.ApplyFormatting)
        {
            FormatDataSheet(sheet, row - 1, headerRow);
        }
    }

    private void AddInfoRow(IXLWorksheet sheet, ref int row, string label, string value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = value;
        row++;
    }

    private void FormatDataSheet(IXLWorksheet sheet, int lastDataRow, int headerRow = 1)
    {
        // Auto-fit columns
        sheet.Columns().AdjustToContents();

        // Add borders
        var dataRange = sheet.Range(headerRow, 1, lastDataRow, sheet.LastColumnUsed().ColumnNumber());
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }
}

/// <summary>
/// Options for Excel export
/// </summary>
public class ExcelExportOptions
{
    public bool IncludeRawData { get; set; } = true;
    public bool IncludeCalculations { get; set; } = true;
    public bool ApplyFormatting { get; set; } = true;
    public bool IncludeSmoothedData { get; set; } = true;
    public bool IncludeSplineData { get; set; } = true;
    public bool IncludeIntervalPoints { get; set; } = true;
}
