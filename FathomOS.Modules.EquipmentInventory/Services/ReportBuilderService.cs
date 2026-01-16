using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for building custom reports with configurable columns, filters, and formatting.
/// Supports Excel and PDF output with saved report templates.
/// </summary>
public class ReportBuilderService
{
    private readonly LocalDatabaseService _dbService;
    private readonly ModuleSettings _settings;
    private readonly string _templatesPath;
    
    public ReportBuilderService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        _settings = ModuleSettings.Load();
        _templatesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "EquipmentInventory", "ReportTemplates");
        Directory.CreateDirectory(_templatesPath);
    }
    
    #region Available Fields
    
    /// <summary>
    /// Get all available report fields
    /// </summary>
    public List<ReportField> GetAvailableFields()
    {
        return new List<ReportField>
        {
            // Basic Info
            new() { Id = "AssetNumber", Name = "Asset Number", Category = "Basic", DataType = ReportFieldType.Text },
            new() { Id = "UniqueId", Name = "Unique ID", Category = "Basic", DataType = ReportFieldType.Text },
            new() { Id = "Name", Name = "Equipment Name", Category = "Basic", DataType = ReportFieldType.Text },
            new() { Id = "Description", Name = "Description", Category = "Basic", DataType = ReportFieldType.Text },
            new() { Id = "SerialNumber", Name = "Serial Number", Category = "Basic", DataType = ReportFieldType.Text },
            new() { Id = "Status", Name = "Status", Category = "Basic", DataType = ReportFieldType.Text },
            new() { Id = "Condition", Name = "Condition", Category = "Basic", DataType = ReportFieldType.Text },
            
            // Classification
            new() { Id = "CategoryName", Name = "Category", Category = "Classification", DataType = ReportFieldType.Text },
            new() { Id = "TypeName", Name = "Type", Category = "Classification", DataType = ReportFieldType.Text },
            new() { Id = "Manufacturer", Name = "Manufacturer", Category = "Classification", DataType = ReportFieldType.Text },
            new() { Id = "Model", Name = "Model", Category = "Classification", DataType = ReportFieldType.Text },
            new() { Id = "PartNumber", Name = "Part Number", Category = "Classification", DataType = ReportFieldType.Text },
            
            // Location
            new() { Id = "LocationName", Name = "Current Location", Category = "Location", DataType = ReportFieldType.Text },
            new() { Id = "LocationCode", Name = "Location Code", Category = "Location", DataType = ReportFieldType.Text },
            new() { Id = "ProjectName", Name = "Project", Category = "Location", DataType = ReportFieldType.Text },
            
            // Physical
            new() { Id = "WeightKg", Name = "Weight (kg)", Category = "Physical", DataType = ReportFieldType.Number },
            new() { Id = "LengthCm", Name = "Length (cm)", Category = "Physical", DataType = ReportFieldType.Number },
            new() { Id = "WidthCm", Name = "Width (cm)", Category = "Physical", DataType = ReportFieldType.Number },
            new() { Id = "HeightCm", Name = "Height (cm)", Category = "Physical", DataType = ReportFieldType.Number },
            
            // Certification
            new() { Id = "RequiresCertification", Name = "Requires Certification", Category = "Certification", DataType = ReportFieldType.Boolean },
            new() { Id = "CertificationNumber", Name = "Certification #", Category = "Certification", DataType = ReportFieldType.Text },
            new() { Id = "CertificationBody", Name = "Certification Body", Category = "Certification", DataType = ReportFieldType.Text },
            new() { Id = "CertificationExpiryDate", Name = "Certification Expiry", Category = "Certification", DataType = ReportFieldType.Date },
            
            // Calibration
            new() { Id = "RequiresCalibration", Name = "Requires Calibration", Category = "Calibration", DataType = ReportFieldType.Boolean },
            new() { Id = "LastCalibrationDate", Name = "Last Calibration", Category = "Calibration", DataType = ReportFieldType.Date },
            new() { Id = "NextCalibrationDate", Name = "Next Calibration", Category = "Calibration", DataType = ReportFieldType.Date },
            new() { Id = "CalibrationIntervalDays", Name = "Calibration Interval (days)", Category = "Calibration", DataType = ReportFieldType.Number },
            
            // Purchase
            new() { Id = "PurchasePrice", Name = "Purchase Price", Category = "Purchase", DataType = ReportFieldType.Currency },
            new() { Id = "PurchaseDate", Name = "Purchase Date", Category = "Purchase", DataType = ReportFieldType.Date },
            new() { Id = "SupplierName", Name = "Supplier", Category = "Purchase", DataType = ReportFieldType.Text },
            new() { Id = "WarrantyExpiryDate", Name = "Warranty Expiry", Category = "Purchase", DataType = ReportFieldType.Date },
            
            // Dates
            new() { Id = "CreatedAt", Name = "Created Date", Category = "Dates", DataType = ReportFieldType.DateTime },
            new() { Id = "UpdatedAt", Name = "Last Updated", Category = "Dates", DataType = ReportFieldType.DateTime },
            new() { Id = "LastServiceDate", Name = "Last Service", Category = "Dates", DataType = ReportFieldType.Date },
            
            // Consumable
            new() { Id = "IsConsumable", Name = "Is Consumable", Category = "Consumable", DataType = ReportFieldType.Boolean },
            new() { Id = "QuantityOnHand", Name = "Quantity", Category = "Consumable", DataType = ReportFieldType.Number },
            new() { Id = "MinimumQuantity", Name = "Minimum Qty", Category = "Consumable", DataType = ReportFieldType.Number },
            new() { Id = "UnitOfMeasure", Name = "Unit", Category = "Consumable", DataType = ReportFieldType.Text }
        };
    }
    
    #endregion
    
    #region Report Generation
    
    /// <summary>
    /// Generate report based on configuration
    /// </summary>
    public async Task<ReportData> GenerateReportAsync(ReportConfiguration config)
    {
        var equipment = await GetFilteredEquipmentAsync(config.Filters);
        
        var reportData = new ReportData
        {
            Title = config.Title,
            GeneratedAt = DateTime.Now,
            Columns = config.Columns,
            Rows = new List<Dictionary<string, object?>>()
        };
        
        foreach (var eq in equipment)
        {
            var row = new Dictionary<string, object?>();
            foreach (var column in config.Columns)
            {
                row[column.FieldId] = GetFieldValue(eq, column.FieldId);
            }
            reportData.Rows.Add(row);
        }
        
        // Apply sorting
        if (!string.IsNullOrEmpty(config.SortField))
        {
            reportData.Rows = config.SortAscending
                ? reportData.Rows.OrderBy(r => r.GetValueOrDefault(config.SortField)).ToList()
                : reportData.Rows.OrderByDescending(r => r.GetValueOrDefault(config.SortField)).ToList();
        }
        
        return reportData;
    }
    
    /// <summary>
    /// Export report to Excel
    /// </summary>
    public async Task<string> ExportToExcelAsync(ReportConfiguration config, string outputPath)
    {
        var reportData = await GenerateReportAsync(config);
        
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Report");
        
        // Title
        worksheet.Cell(1, 1).Value = reportData.Title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, config.Columns.Count).Merge();
        
        worksheet.Cell(2, 1).Value = $"Generated: {reportData.GeneratedAt:g}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        
        // Headers
        int headerRow = 4;
        for (int i = 0; i < config.Columns.Count; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = config.Columns[i].DisplayName;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
        
        // Data
        int dataRow = headerRow + 1;
        foreach (var row in reportData.Rows)
        {
            for (int i = 0; i < config.Columns.Count; i++)
            {
                var cell = worksheet.Cell(dataRow, i + 1);
                var value = row.GetValueOrDefault(config.Columns[i].FieldId);
                
                if (value is DateTime dt)
                    cell.Value = dt;
                else if (value is decimal d)
                    cell.Value = d;
                else if (value is bool b)
                    cell.Value = b ? "Yes" : "No";
                else
                    cell.Value = value?.ToString() ?? "";
                
                // Alternate row colors
                if ((dataRow - headerRow) % 2 == 0)
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            }
            dataRow++;
        }
        
        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        
        workbook.SaveAs(outputPath);
        return outputPath;
    }
    
    /// <summary>
    /// Export report to PDF
    /// </summary>
    public async Task<string> ExportToPdfAsync(ReportConfiguration config, string outputPath)
    {
        var reportData = await GenerateReportAsync(config);
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(config.Orientation == ReportOrientation.Landscape 
                    ? PageSizes.A4.Landscape() 
                    : PageSizes.A4);
                page.Margin(20);
                
                // Header
                page.Header().Element(header =>
                {
                    header.Column(col =>
                    {
                        col.Item().Text(reportData.Title).FontSize(16).Bold();
                        col.Item().Text($"Generated: {reportData.GeneratedAt:g}").FontSize(10).Italic();
                        col.Item().PaddingBottom(10);
                    });
                });
                
                // Content - Table
                page.Content().Element(content =>
                {
                    content.Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var col in config.Columns)
                            {
                                columns.RelativeColumn();
                            }
                        });
                        
                        // Header row
                        table.Header(header =>
                        {
                            foreach (var col in config.Columns)
                            {
                                header.Cell().Background("#1E3A5F").Padding(5)
                                    .Text(col.DisplayName).FontColor("#FFFFFF").FontSize(10).Bold();
                            }
                        });
                        
                        // Data rows
                        bool alternate = false;
                        foreach (var row in reportData.Rows)
                        {
                            var bgColor = alternate ? "#F5F5F5" : "#FFFFFF";
                            
                            foreach (var col in config.Columns)
                            {
                                var value = row.GetValueOrDefault(col.FieldId);
                                var displayValue = FormatValue(value, col.DataType);
                                
                                table.Cell().Background(bgColor).Padding(3)
                                    .Text(displayValue).FontSize(9);
                            }
                            
                            alternate = !alternate;
                        }
                    });
                });
                
                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });
        
        document.GeneratePdf(outputPath);
        return outputPath;
    }
    
    #endregion
    
    #region Report Templates
    
    /// <summary>
    /// Save report configuration as template
    /// </summary>
    public async Task SaveTemplateAsync(ReportConfiguration config, string templateName)
    {
        config.TemplateName = templateName;
        config.SavedAt = DateTime.UtcNow;
        
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(_templatesPath, $"{Guid.NewGuid()}.json");
        await File.WriteAllTextAsync(filePath, json);
    }
    
    /// <summary>
    /// Get all saved templates
    /// </summary>
    public async Task<List<ReportConfiguration>> GetTemplatesAsync()
    {
        var templates = new List<ReportConfiguration>();
        
        foreach (var file in Directory.GetFiles(_templatesPath, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var config = JsonSerializer.Deserialize<ReportConfiguration>(json);
                if (config != null)
                    templates.Add(config);
            }
            catch { }
        }
        
        return templates.OrderBy(t => t.TemplateName).ToList();
    }
    
    /// <summary>
    /// Get preset report templates
    /// </summary>
    public List<ReportConfiguration> GetPresetTemplates()
    {
        return new List<ReportConfiguration>
        {
            new()
            {
                TemplateName = "Equipment Inventory",
                Title = "Equipment Inventory Report",
                Columns = new List<ReportColumn>
                {
                    new() { FieldId = "AssetNumber", DisplayName = "Asset #" },
                    new() { FieldId = "Name", DisplayName = "Name" },
                    new() { FieldId = "CategoryName", DisplayName = "Category" },
                    new() { FieldId = "Status", DisplayName = "Status" },
                    new() { FieldId = "LocationName", DisplayName = "Location" },
                    new() { FieldId = "Condition", DisplayName = "Condition" }
                }
            },
            new()
            {
                TemplateName = "Certification Status",
                Title = "Certification Status Report",
                Columns = new List<ReportColumn>
                {
                    new() { FieldId = "AssetNumber", DisplayName = "Asset #" },
                    new() { FieldId = "Name", DisplayName = "Equipment" },
                    new() { FieldId = "CertificationNumber", DisplayName = "Cert #" },
                    new() { FieldId = "CertificationBody", DisplayName = "Body" },
                    new() { FieldId = "CertificationExpiryDate", DisplayName = "Expiry" },
                    new() { FieldId = "LocationName", DisplayName = "Location" }
                },
                Filters = new ReportFilters { RequiresCertification = true }
            },
            new()
            {
                TemplateName = "Calibration Due",
                Title = "Calibration Due Report",
                Columns = new List<ReportColumn>
                {
                    new() { FieldId = "AssetNumber", DisplayName = "Asset #" },
                    new() { FieldId = "Name", DisplayName = "Equipment" },
                    new() { FieldId = "LastCalibrationDate", DisplayName = "Last Cal." },
                    new() { FieldId = "NextCalibrationDate", DisplayName = "Next Due" },
                    new() { FieldId = "CalibrationIntervalDays", DisplayName = "Interval" },
                    new() { FieldId = "LocationName", DisplayName = "Location" }
                },
                Filters = new ReportFilters { RequiresCalibration = true }
            },
            new()
            {
                TemplateName = "Asset Value",
                Title = "Asset Value Report",
                Columns = new List<ReportColumn>
                {
                    new() { FieldId = "AssetNumber", DisplayName = "Asset #" },
                    new() { FieldId = "Name", DisplayName = "Equipment" },
                    new() { FieldId = "CategoryName", DisplayName = "Category" },
                    new() { FieldId = "PurchasePrice", DisplayName = "Value" },
                    new() { FieldId = "PurchaseDate", DisplayName = "Purchased" },
                    new() { FieldId = "SupplierName", DisplayName = "Supplier" }
                }
            }
        };
    }
    
    #endregion
    
    #region Helpers
    
    private async Task<List<Equipment>> GetFilteredEquipmentAsync(ReportFilters? filters)
    {
        var query = _dbService.Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.Type)
            .Include(e => e.CurrentLocation)
            .Include(e => e.Supplier)
            .Where(e => e.IsActive)
            .AsQueryable();
        
        if (filters != null)
        {
            if (filters.CategoryId.HasValue)
                query = query.Where(e => e.CategoryId == filters.CategoryId);
            
            if (filters.LocationId.HasValue)
                query = query.Where(e => e.CurrentLocationId == filters.LocationId);
            
            if (filters.Status.HasValue)
                query = query.Where(e => e.Status == filters.Status);
            
            if (filters.RequiresCertification.HasValue)
                query = query.Where(e => e.RequiresCertification == filters.RequiresCertification);
            
            if (filters.RequiresCalibration.HasValue)
                query = query.Where(e => e.RequiresCalibration == filters.RequiresCalibration);
            
            if (filters.CertificationExpiringWithinDays.HasValue)
            {
                var expiryDate = DateTime.Today.AddDays(filters.CertificationExpiringWithinDays.Value);
                query = query.Where(e => e.CertificationExpiryDate <= expiryDate);
            }
        }
        
        return await query.AsNoTracking().ToListAsync();
    }
    
    private static object? GetFieldValue(Equipment eq, string fieldId)
    {
        return fieldId switch
        {
            "AssetNumber" => eq.AssetNumber,
            "UniqueId" => eq.UniqueId,
            "Name" => eq.Name,
            "Description" => eq.Description,
            "SerialNumber" => eq.SerialNumber,
            "Status" => eq.Status.ToString(),
            "Condition" => eq.Condition,
            "CategoryName" => eq.Category?.Name,
            "TypeName" => eq.Type?.Name,
            "Manufacturer" => eq.Manufacturer,
            "Model" => eq.Model,
            "PartNumber" => eq.PartNumber,
            "LocationName" => eq.CurrentLocation?.Name,
            "LocationCode" => eq.CurrentLocation?.Code,
            "ProjectName" => eq.CurrentProject?.Name,
            "WeightKg" => eq.WeightKg,
            "LengthCm" => eq.LengthCm,
            "WidthCm" => eq.WidthCm,
            "HeightCm" => eq.HeightCm,
            "RequiresCertification" => eq.RequiresCertification,
            "CertificationNumber" => eq.CertificationNumber,
            "CertificationBody" => eq.CertificationBody,
            "CertificationExpiryDate" => eq.CertificationExpiryDate,
            "RequiresCalibration" => eq.RequiresCalibration,
            "LastCalibrationDate" => eq.LastCalibrationDate,
            "NextCalibrationDate" => eq.NextCalibrationDate,
            "CalibrationIntervalDays" => eq.CalibrationIntervalDays,
            "PurchasePrice" => eq.PurchasePrice,
            "PurchaseDate" => eq.PurchaseDate,
            "SupplierName" => eq.Supplier?.Name,
            "WarrantyExpiryDate" => eq.WarrantyExpiryDate,
            "CreatedAt" => eq.CreatedAt,
            "UpdatedAt" => eq.UpdatedAt,
            "LastServiceDate" => eq.LastServiceDate,
            "IsConsumable" => eq.IsConsumable,
            "QuantityOnHand" => eq.QuantityOnHand,
            "MinimumQuantity" => eq.MinimumQuantity,
            "UnitOfMeasure" => eq.UnitOfMeasure,
            _ => null
        };
    }
    
    private static string FormatValue(object? value, ReportFieldType type)
    {
        if (value == null) return "";
        
        return type switch
        {
            ReportFieldType.Date when value is DateTime dt => dt.ToString("d"),
            ReportFieldType.DateTime when value is DateTime dt => dt.ToString("g"),
            ReportFieldType.Currency when value is decimal d => d.ToString("C2"),
            ReportFieldType.Number when value is decimal d => d.ToString("N2"),
            ReportFieldType.Boolean when value is bool b => b ? "Yes" : "No",
            _ => value.ToString() ?? ""
        };
    }
    
    #endregion
}

#region Models

public class ReportField
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ReportFieldType DataType { get; set; }
}

public enum ReportFieldType
{
    Text,
    Number,
    Currency,
    Date,
    DateTime,
    Boolean
}

public class ReportConfiguration
{
    public string? TemplateName { get; set; }
    public string Title { get; set; } = "Equipment Report";
    public List<ReportColumn> Columns { get; set; } = new();
    public ReportFilters? Filters { get; set; }
    public string? SortField { get; set; }
    public bool SortAscending { get; set; } = true;
    public ReportOrientation Orientation { get; set; } = ReportOrientation.Portrait;
    public DateTime? SavedAt { get; set; }
}

public class ReportColumn
{
    public string FieldId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Width { get; set; } = 100;
    public ReportFieldType DataType { get; set; }
}

public class ReportFilters
{
    public Guid? CategoryId { get; set; }
    public Guid? LocationId { get; set; }
    public EquipmentStatus? Status { get; set; }
    public bool? RequiresCertification { get; set; }
    public bool? RequiresCalibration { get; set; }
    public int? CertificationExpiringWithinDays { get; set; }
    public string? SearchText { get; set; }
}

public enum ReportOrientation
{
    Portrait,
    Landscape
}

public class ReportData
{
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<ReportColumn> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows => Rows.Count;
}

#endregion
