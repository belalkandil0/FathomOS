using ClosedXML.Excel;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Import;

/// <summary>
/// Excel import service for bulk equipment import
/// </summary>
public class ExcelImportService
{
    private readonly LocalDatabaseService _dbService;
    
    public ExcelImportService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
    }
    
    /// <summary>
    /// Import equipment from Excel file
    /// </summary>
    public async Task<ImportResult> ImportEquipmentAsync(string filePath, ImportOptions options)
    {
        var result = new ImportResult();
        
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.First();
            
            // Find header row
            var headerRow = worksheet.Row(options.HeaderRowNumber);
            var columnMap = MapColumns(headerRow, options);
            
            if (!columnMap.ContainsKey("Name"))
            {
                result.Errors.Add("Required column 'Name' not found");
                return result;
            }
            
            // Get categories and locations for lookup
            var categories = await _dbService.GetCategoriesAsync();
            var locations = await _dbService.GetLocationsAsync();
            
            // Process data rows
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? options.HeaderRowNumber;
            
            for (int row = options.HeaderRowNumber + 1; row <= lastRow; row++)
            {
                try
                {
                    var equipment = await ParseEquipmentRow(worksheet.Row(row), columnMap, categories, locations, options);
                    
                    if (equipment != null)
                    {
                        // Check for duplicates
                        if (!string.IsNullOrEmpty(equipment.SerialNumber))
                        {
                            var existing = await _dbService.GetEquipmentByAssetNumberAsync(equipment.AssetNumber);
                            if (existing != null)
                            {
                                if (options.UpdateExisting)
                                {
                                    equipment.EquipmentId = existing.EquipmentId;
                                    await _dbService.SaveEquipmentAsync(equipment);
                                    result.Updated++;
                                }
                                else
                                {
                                    result.Skipped++;
                                    result.Warnings.Add($"Row {row}: Duplicate asset number '{equipment.AssetNumber}' - skipped");
                                }
                                continue;
                            }
                        }
                        
                        await _dbService.SaveEquipmentAsync(equipment);
                        result.Imported++;
                    }
                    else
                    {
                        result.Skipped++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Row {row}: {ex.Message}");
                }
            }
            
            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Failed to read file: {ex.Message}");
        }
        
        return result;
    }
    
    private Dictionary<string, int> MapColumns(IXLRow headerRow, ImportOptions options)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        // Standard column mappings
        var standardMappings = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Name", new[] { "Name", "Description", "Equipment Name", "Item Name" } },
            { "AssetNumber", new[] { "Asset Number", "Asset #", "Asset No", "Asset", "Tag" } },
            { "SerialNumber", new[] { "Serial Number", "Serial #", "Serial No", "S/N", "SN" } },
            { "Category", new[] { "Category", "Type", "Equipment Type", "Class" } },
            { "Location", new[] { "Location", "Site", "Base", "Warehouse" } },
            { "Manufacturer", new[] { "Manufacturer", "Make", "Brand", "OEM" } },
            { "Model", new[] { "Model", "Model Number", "Model #" } },
            { "Status", new[] { "Status", "Condition", "State" } },
            { "PartNumber", new[] { "Part Number", "Part #", "Part No", "P/N" } },
            { "Quantity", new[] { "Quantity", "Qty", "Count", "Amount" } },
            { "UnitOfMeasure", new[] { "Unit", "UOM", "Unit of Measure", "Units" } },
            { "Weight", new[] { "Weight", "Weight (kg)", "Mass" } },
            { "Notes", new[] { "Notes", "Comments", "Remarks", "Description" } },
            { "SapNumber", new[] { "SAP Number", "SAP #", "SAP", "SAP No" } },
            { "CertificationExpiry", new[] { "Certification Expiry", "Cert Expiry", "Cert Due" } },
            { "CalibrationDue", new[] { "Calibration Due", "Calib Due", "Next Calibration" } },
            { "PurchaseDate", new[] { "Purchase Date", "Acquired", "Date Purchased" } },
            { "PurchasePrice", new[] { "Purchase Price", "Cost", "Price", "Value" } },
        };
        
        // Apply custom mappings
        if (options.ColumnMappings != null)
        {
            foreach (var custom in options.ColumnMappings)
            {
                if (standardMappings.ContainsKey(custom.Key))
                    standardMappings[custom.Key] = new[] { custom.Value };
            }
        }
        
        // Find columns
        foreach (var cell in headerRow.CellsUsed())
        {
            var headerText = cell.GetString().Trim();
            if (string.IsNullOrEmpty(headerText)) continue;
            
            foreach (var mapping in standardMappings)
            {
                if (mapping.Value.Any(v => v.Equals(headerText, StringComparison.OrdinalIgnoreCase)))
                {
                    map[mapping.Key] = cell.Address.ColumnNumber;
                    break;
                }
            }
        }
        
        return map;
    }
    
    private async Task<Equipment?> ParseEquipmentRow(
        IXLRow row, 
        Dictionary<string, int> columnMap,
        List<EquipmentCategory> categories,
        List<Location> locations,
        ImportOptions options)
    {
        var name = GetCellValue(row, columnMap, "Name");
        if (string.IsNullOrWhiteSpace(name)) return null;
        
        var equipment = new Equipment
        {
            Name = name,
            SerialNumber = GetCellValue(row, columnMap, "SerialNumber"),
            Manufacturer = GetCellValue(row, columnMap, "Manufacturer"),
            Model = GetCellValue(row, columnMap, "Model"),
            PartNumber = GetCellValue(row, columnMap, "PartNumber"),
            Notes = GetCellValue(row, columnMap, "Notes"),
            SapNumber = GetCellValue(row, columnMap, "SapNumber"),
        };
        
        // Asset number
        var assetNumber = GetCellValue(row, columnMap, "AssetNumber");
        if (!string.IsNullOrEmpty(assetNumber))
        {
            equipment.AssetNumber = assetNumber;
            equipment.QrCode = $"foseq:{assetNumber}";
        }
        else
        {
            // Will be auto-generated when saved
        }
        
        // Category
        var categoryName = GetCellValue(row, columnMap, "Category");
        if (!string.IsNullOrEmpty(categoryName))
        {
            var category = categories.FirstOrDefault(c => 
                c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            if (category != null)
            {
                equipment.CategoryId = category.CategoryId;
                equipment.RequiresCertification = category.RequiresCertification;
                equipment.RequiresCalibration = category.RequiresCalibration;
            }
        }
        else if (options.DefaultCategoryId.HasValue)
        {
            equipment.CategoryId = options.DefaultCategoryId;
        }
        
        // Location
        var locationName = GetCellValue(row, columnMap, "Location");
        if (!string.IsNullOrEmpty(locationName))
        {
            var location = locations.FirstOrDefault(l => 
                l.Name.Equals(locationName, StringComparison.OrdinalIgnoreCase) ||
                l.Code.Equals(locationName, StringComparison.OrdinalIgnoreCase));
            if (location != null)
                equipment.CurrentLocationId = location.LocationId;
        }
        else if (options.DefaultLocationId.HasValue)
        {
            equipment.CurrentLocationId = options.DefaultLocationId;
        }
        
        // Status
        var statusStr = GetCellValue(row, columnMap, "Status");
        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<EquipmentStatus>(statusStr, true, out var status))
            equipment.Status = status;
        
        // Quantity (for consumables)
        var qtyStr = GetCellValue(row, columnMap, "Quantity");
        if (decimal.TryParse(qtyStr, out var qty) && qty > 1)
        {
            equipment.IsConsumable = true;
            equipment.QuantityOnHand = qty;
        }
        
        // Weight
        var weightStr = GetCellValue(row, columnMap, "Weight");
        if (decimal.TryParse(weightStr, out var weight))
            equipment.WeightKg = weight;
        
        // Dates
        var certExpiry = GetDateValue(row, columnMap, "CertificationExpiry");
        if (certExpiry.HasValue)
            equipment.CertificationExpiryDate = certExpiry;
        
        var calibDue = GetDateValue(row, columnMap, "CalibrationDue");
        if (calibDue.HasValue)
            equipment.NextCalibrationDate = calibDue;
        
        var purchaseDate = GetDateValue(row, columnMap, "PurchaseDate");
        if (purchaseDate.HasValue)
            equipment.PurchaseDate = purchaseDate;
        
        var priceStr = GetCellValue(row, columnMap, "PurchasePrice");
        if (decimal.TryParse(priceStr, out var price))
            equipment.PurchasePrice = price;
        
        return equipment;
    }
    
    private string? GetCellValue(IXLRow row, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var colNum)) return null;
        return row.Cell(colNum).GetString().Trim();
    }
    
    private DateTime? GetDateValue(IXLRow row, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var colNum)) return null;
        
        var cell = row.Cell(colNum);
        if (cell.TryGetValue<DateTime>(out var date))
            return date;
        
        var str = cell.GetString().Trim();
        if (DateTime.TryParse(str, out date))
            return date;
        
        return null;
    }
    
    /// <summary>
    /// Generate a sample import template
    /// </summary>
    public void GenerateTemplate(string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Equipment Import");
        
        var headers = new[] 
        { 
            "Name", "Asset Number", "Serial Number", "Category", "Location", 
            "Manufacturer", "Model", "Part Number", "Status", "Quantity", 
            "Unit", "Weight (kg)", "SAP Number", "Certification Expiry", 
            "Calibration Due", "Purchase Date", "Purchase Price", "Notes"
        };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
        
        // Sample data row
        worksheet.Cell(2, 1).Value = "Sample Equipment";
        worksheet.Cell(2, 2).Value = "EQ-2024-00001";
        worksheet.Cell(2, 3).Value = "SN12345";
        worksheet.Cell(2, 4).Value = "Survey Equipment";
        worksheet.Cell(2, 5).Value = "Main Warehouse";
        worksheet.Cell(2, 6).Value = "Manufacturer Inc";
        worksheet.Cell(2, 7).Value = "Model X";
        worksheet.Cell(2, 8).Value = "PN-001";
        worksheet.Cell(2, 9).Value = "Available";
        worksheet.Cell(2, 10).Value = 1;
        worksheet.Cell(2, 11).Value = "Each";
        worksheet.Cell(2, 12).Value = 5.5;
        worksheet.Cell(2, 13).Value = "SAP-123";
        worksheet.Cell(2, 14).Value = DateTime.Today.AddYears(1);
        worksheet.Cell(2, 15).Value = DateTime.Today.AddMonths(6);
        worksheet.Cell(2, 16).Value = DateTime.Today.AddYears(-1);
        worksheet.Cell(2, 17).Value = 1500.00;
        worksheet.Cell(2, 18).Value = "Sample notes";
        
        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
}

public class ImportOptions
{
    public int HeaderRowNumber { get; set; } = 1;
    public bool UpdateExisting { get; set; } = false;
    public Guid? DefaultCategoryId { get; set; }
    public Guid? DefaultLocationId { get; set; }
    public Dictionary<string, string>? ColumnMappings { get; set; }
}

public class ImportResult
{
    public bool Success { get; set; }
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public string Summary => $"Imported: {Imported}, Updated: {Updated}, Skipped: {Skipped}, Errors: {Errors.Count}";
}
