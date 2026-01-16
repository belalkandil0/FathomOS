using ClosedXML.Excel;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Export;

/// <summary>
/// Excel export service using ClosedXML from S7Fathom.Core
/// </summary>
public class ExcelExportService
{
    /// <summary>
    /// Export equipment register to Excel
    /// </summary>
    public void ExportEquipmentRegister(string filePath, IEnumerable<Equipment> equipment, string? title = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Equipment Register");
        
        // Title
        var titleRow = 1;
        worksheet.Cell(titleRow, 1).Value = title ?? "Equipment Register";
        worksheet.Cell(titleRow, 1).Style.Font.Bold = true;
        worksheet.Cell(titleRow, 1).Style.Font.FontSize = 16;
        worksheet.Range(titleRow, 1, titleRow, 10).Merge();
        
        // Generated date
        worksheet.Cell(titleRow + 1, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        worksheet.Cell(titleRow + 1, 1).Style.Font.Italic = true;
        
        // Headers
        var headerRow = 4;
        var headers = new[] { "Asset #", "Name", "Category", "Location", "Status", "Condition", 
                              "Manufacturer", "Model", "Serial #", "Cert. Expiry", "Calib. Due", "Notes" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        
        // Data rows
        var row = headerRow + 1;
        foreach (var eq in equipment)
        {
            worksheet.Cell(row, 1).Value = eq.AssetNumber;
            worksheet.Cell(row, 2).Value = eq.Name;
            worksheet.Cell(row, 3).Value = eq.Category?.Name ?? "";
            worksheet.Cell(row, 4).Value = eq.CurrentLocation?.Name ?? "";
            worksheet.Cell(row, 5).Value = eq.Status.ToString();
            worksheet.Cell(row, 6).Value = eq.Condition.ToString();
            worksheet.Cell(row, 7).Value = eq.Manufacturer ?? "";
            worksheet.Cell(row, 8).Value = eq.Model ?? "";
            worksheet.Cell(row, 9).Value = eq.SerialNumber ?? "";
            worksheet.Cell(row, 10).Value = eq.CertificationExpiryDate?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 11).Value = eq.NextCalibrationDate?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 12).Value = eq.Notes ?? "";
            
            // Highlight expiring/overdue items
            if (eq.IsCertificationExpired)
                worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.LightCoral;
            else if (eq.IsCertificationExpiring)
                worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.LightYellow;
            
            if (eq.IsCalibrationOverdue)
                worksheet.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.LightCoral;
            else if (eq.IsCalibrationDue)
                worksheet.Cell(row, 11).Style.Fill.BackgroundColor = XLColor.LightYellow;
            
            // Alternate row colors
            if (row % 2 == 0)
            {
                worksheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            }
            
            row++;
        }
        
        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        
        // Add filters
        worksheet.Range(headerRow, 1, row - 1, headers.Length).SetAutoFilter();
        
        // Freeze header row
        worksheet.SheetView.FreezeRows(headerRow);
        
        workbook.SaveAs(filePath);
    }
    
    /// <summary>
    /// Export manifest to Excel
    /// </summary>
    public void ExportManifest(string filePath, Manifest manifest)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Manifest");
        
        // Header section
        worksheet.Cell(1, 1).Value = "MANIFEST";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 18;
        
        worksheet.Cell(2, 1).Value = "Number:";
        worksheet.Cell(2, 2).Value = manifest.ManifestNumber;
        worksheet.Cell(3, 1).Value = "Type:";
        worksheet.Cell(3, 2).Value = manifest.Type.ToString();
        worksheet.Cell(4, 1).Value = "Status:";
        worksheet.Cell(4, 2).Value = manifest.Status.ToString();
        worksheet.Cell(5, 1).Value = "Created:";
        worksheet.Cell(5, 2).Value = manifest.CreatedDate.ToString("yyyy-MM-dd HH:mm");
        
        worksheet.Cell(2, 4).Value = "From:";
        worksheet.Cell(2, 5).Value = manifest.FromLocation?.Name ?? "";
        worksheet.Cell(3, 4).Value = "To:";
        worksheet.Cell(3, 5).Value = manifest.ToLocation?.Name ?? "";
        worksheet.Cell(4, 4).Value = "Contact:";
        worksheet.Cell(4, 5).Value = manifest.FromContactName ?? "";
        
        // Items header
        var headerRow = 8;
        var headers = new[] { "#", "Asset Number", "Name", "Quantity", "Condition", "Received", "Discrepancy" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
        
        // Items
        var row = headerRow + 1;
        var itemNum = 1;
        foreach (var item in manifest.Items)
        {
            worksheet.Cell(row, 1).Value = itemNum++;
            worksheet.Cell(row, 2).Value = item.AssetNumber ?? "";
            worksheet.Cell(row, 3).Value = item.Name ?? "";
            worksheet.Cell(row, 4).Value = item.Quantity;
            worksheet.Cell(row, 5).Value = item.ConditionAtSend ?? "";
            worksheet.Cell(row, 6).Value = item.IsReceived ? "Yes" : "No";
            worksheet.Cell(row, 7).Value = item.HasDiscrepancy ? item.DiscrepancyType?.ToString() ?? "Yes" : "";
            
            if (item.HasDiscrepancy)
                worksheet.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightCoral;
            
            row++;
        }
        
        // Summary
        row += 2;
        worksheet.Cell(row, 1).Value = $"Total Items: {manifest.TotalItems}";
        if (manifest.HasDiscrepancies)
        {
            worksheet.Cell(row + 1, 1).Value = "⚠ This manifest has discrepancies";
            worksheet.Cell(row + 1, 1).Style.Font.FontColor = XLColor.Red;
        }
        
        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
    }
    
    /// <summary>
    /// Export certification/calibration due report
    /// </summary>
    public void ExportDueReport(string filePath, IEnumerable<Equipment> equipment, string reportType)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add($"{reportType} Due");
        
        worksheet.Cell(1, 1).Value = $"{reportType} Due Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        
        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        
        var headerRow = 4;
        var headers = new[] { "Asset #", "Name", "Location", "Due Date", "Days Remaining", "Status" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
        
        var row = headerRow + 1;
        foreach (var eq in equipment)
        {
            var dueDate = reportType == "Certification" ? eq.CertificationExpiryDate : eq.NextCalibrationDate;
            var daysRemaining = reportType == "Certification" ? eq.DaysUntilCertificationExpiry : eq.DaysUntilCalibrationDue;
            var isOverdue = reportType == "Certification" ? eq.IsCertificationExpired : eq.IsCalibrationOverdue;
            
            worksheet.Cell(row, 1).Value = eq.AssetNumber;
            worksheet.Cell(row, 2).Value = eq.Name;
            worksheet.Cell(row, 3).Value = eq.CurrentLocation?.Name ?? "";
            worksheet.Cell(row, 4).Value = dueDate?.ToString("yyyy-MM-dd") ?? "";
            worksheet.Cell(row, 5).Value = daysRemaining ?? 0;
            worksheet.Cell(row, 6).Value = isOverdue ? "OVERDUE" : (daysRemaining <= 7 ? "DUE SOON" : "OK");
            
            if (isOverdue)
                worksheet.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.LightCoral;
            else if (daysRemaining <= 7)
                worksheet.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.LightYellow;
            
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        worksheet.Range(headerRow, 1, row - 1, headers.Length).SetAutoFilter();
        
        workbook.SaveAs(filePath);
    }
    
    /// <summary>
    /// Export manifest history to Excel
    /// </summary>
    public void ExportManifestHistory(string filePath, IEnumerable<Manifest> manifests)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Manifest History");
        
        worksheet.Cell(1, 1).Value = "Manifest History Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Range(1, 1, 1, 8).Merge();
        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        
        var headerRow = 4;
        var headers = new[] { "Manifest #", "Type", "Status", "From", "To", "Items", "Created", "Completed" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
        
        var row = headerRow + 1;
        foreach (var m in manifests.OrderByDescending(m => m.CreatedDate))
        {
            worksheet.Cell(row, 1).Value = m.ManifestNumber;
            worksheet.Cell(row, 2).Value = m.Type.ToString();
            worksheet.Cell(row, 3).Value = m.Status.ToString();
            worksheet.Cell(row, 4).Value = m.FromLocation?.Name ?? "";
            worksheet.Cell(row, 5).Value = m.ToLocation?.Name ?? "";
            worksheet.Cell(row, 6).Value = m.TotalItems;
            worksheet.Cell(row, 7).Value = m.CreatedDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 8).Value = m.CompletedDate?.ToString("yyyy-MM-dd") ?? "";
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        worksheet.Range(headerRow, 1, row - 1, headers.Length).SetAutoFilter();
        workbook.SaveAs(filePath);
    }
    
    /// <summary>
    /// Export location summary to Excel
    /// </summary>
    public void ExportLocationSummary(string filePath, IEnumerable<Location> locations, IEnumerable<Equipment> allEquipment)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Location Summary");
        
        worksheet.Cell(1, 1).Value = "Location Summary Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Range(1, 1, 1, 6).Merge();
        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        
        var headerRow = 4;
        var headers = new[] { "Location", "Code", "Type", "Equipment Count", "Total Value", "Active Items" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
        
        var row = headerRow + 1;
        var equipmentList = allEquipment.ToList();
        
        foreach (var loc in locations.Where(l => l.IsActive).OrderBy(l => l.Name))
        {
            var locEquipment = equipmentList.Where(e => e.CurrentLocationId == loc.LocationId).ToList();
            worksheet.Cell(row, 1).Value = loc.Name;
            worksheet.Cell(row, 2).Value = loc.Code;
            worksheet.Cell(row, 3).Value = loc.Type.ToString();
            worksheet.Cell(row, 4).Value = locEquipment.Count;
            worksheet.Cell(row, 5).Value = locEquipment.Sum(e => e.CurrentValue ?? e.PurchasePrice ?? 0);
            worksheet.Cell(row, 6).Value = locEquipment.Count(e => e.IsActive);
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        worksheet.Range(headerRow, 1, row - 1, headers.Length).SetAutoFilter();
        workbook.SaveAs(filePath);
    }
    
    /// <summary>
    /// Export asset movement history to Excel
    /// </summary>
    public void ExportAssetMovement(string filePath, IEnumerable<EquipmentHistory> history)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Asset Movement");
        
        worksheet.Cell(1, 1).Value = "Asset Movement Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Range(1, 1, 1, 6).Merge();
        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        
        var headerRow = 4;
        var headers = new[] { "Date", "Asset #", "Equipment", "Action", "From", "To", "User" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
        }
        
        var row = headerRow + 1;
        foreach (var h in history.Where(h => h.Action == HistoryAction.Transferred || h.Action == HistoryAction.StatusChanged)
                                 .OrderByDescending(h => h.PerformedAt))
        {
            worksheet.Cell(row, 1).Value = h.PerformedAt.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(row, 2).Value = h.Equipment?.AssetNumber ?? "";
            worksheet.Cell(row, 3).Value = h.Equipment?.Name ?? "";
            worksheet.Cell(row, 4).Value = h.Action.ToString();
            worksheet.Cell(row, 5).Value = h.OldValue ?? "";
            worksheet.Cell(row, 6).Value = h.NewValue ?? "";
            worksheet.Cell(row, 7).Value = h.PerformedBy?.ToString() ?? "";
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        worksheet.Range(headerRow, 1, row - 1, headers.Length).SetAutoFilter();
        workbook.SaveAs(filePath);
    }
    
    /// <summary>
    /// Export defect reports (EFN) to Excel
    /// </summary>
    public void ExportDefectReports(IEnumerable<DefectReport> defects, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Defect Reports");
        
        // Title
        worksheet.Cell(1, 1).Value = "Equipment Failure Notification Report";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Range(1, 1, 1, 12).Merge();
        
        worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        
        // Headers
        var headerRow = 4;
        var headers = new[] { "Report #", "Date", "Status", "Urgency", "Location", "Equipment Origin",
                              "Major Component", "Fault Category", "Manufacturer", "Model", "Serial #",
                              "Symptoms", "Action Taken", "Repair (min)", "Downtime (min)" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        
        // Data rows
        var row = headerRow + 1;
        foreach (var d in defects.OrderByDescending(d => d.ReportDate))
        {
            worksheet.Cell(row, 1).Value = d.ReportNumber;
            worksheet.Cell(row, 2).Value = d.ReportDate.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 3).Value = d.Status.ToString();
            worksheet.Cell(row, 4).Value = d.ReplacementUrgency.ToString();
            worksheet.Cell(row, 5).Value = d.Location?.DisplayName ?? d.ThirdPartyLocationName ?? "";
            worksheet.Cell(row, 6).Value = d.EquipmentOrigin ?? "";
            worksheet.Cell(row, 7).Value = d.MajorComponent ?? "";
            worksheet.Cell(row, 8).Value = d.FaultCategory.ToString();
            worksheet.Cell(row, 9).Value = d.Manufacturer ?? "";
            worksheet.Cell(row, 10).Value = d.Model ?? "";
            worksheet.Cell(row, 11).Value = d.SerialNumber ?? "";
            worksheet.Cell(row, 12).Value = d.DetailedSymptoms ?? "";
            worksheet.Cell(row, 13).Value = d.ActionTaken ?? "";
            worksheet.Cell(row, 14).Value = d.RepairDurationMinutes?.ToString() ?? "";
            worksheet.Cell(row, 15).Value = d.DowntimeDurationMinutes?.ToString() ?? "";
            
            // Color code by urgency
            var urgencyColor = d.ReplacementUrgency switch
            {
                ReplacementUrgency.High => XLColor.FromHtml("#FFCCCC"),
                ReplacementUrgency.Medium => XLColor.FromHtml("#FFF3CD"),
                ReplacementUrgency.Low => XLColor.FromHtml("#D4EDDA"),
                _ => XLColor.White
            };
            worksheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = urgencyColor;
            
            row++;
        }
        
        worksheet.Columns().AdjustToContents();
        worksheet.Column(12).Width = 50; // Symptoms column wider
        worksheet.Column(13).Width = 50; // Action taken column wider
        worksheet.Range(headerRow, 1, row - 1, headers.Length).SetAutoFilter();
        
        // Summary sheet
        var summarySheet = workbook.Worksheets.Add("Summary");
        summarySheet.Cell(1, 1).Value = "Defect Report Summary";
        summarySheet.Cell(1, 1).Style.Font.Bold = true;
        summarySheet.Cell(1, 1).Style.Font.FontSize = 14;
        
        var defectsList = defects.ToList();
        summarySheet.Cell(3, 1).Value = "Total Reports:";
        summarySheet.Cell(3, 2).Value = defectsList.Count;
        summarySheet.Cell(4, 1).Value = "Open (Non-Closed):";
        summarySheet.Cell(4, 2).Value = defectsList.Count(d => d.Status != DefectReportStatus.Closed && d.Status != DefectReportStatus.Cancelled);
        summarySheet.Cell(5, 1).Value = "High Urgency:";
        summarySheet.Cell(5, 2).Value = defectsList.Count(d => d.ReplacementUrgency == ReplacementUrgency.High);
        summarySheet.Cell(6, 1).Value = "Medium Urgency:";
        summarySheet.Cell(6, 2).Value = defectsList.Count(d => d.ReplacementUrgency == ReplacementUrgency.Medium);
        summarySheet.Cell(7, 1).Value = "Low Urgency:";
        summarySheet.Cell(7, 2).Value = defectsList.Count(d => d.ReplacementUrgency == ReplacementUrgency.Low);
        
        summarySheet.Cell(9, 1).Value = "By Status:";
        summarySheet.Cell(9, 1).Style.Font.Bold = true;
        var statusRow = 10;
        foreach (var statusGroup in defectsList.GroupBy(d => d.Status))
        {
            summarySheet.Cell(statusRow, 1).Value = statusGroup.Key.ToString();
            summarySheet.Cell(statusRow, 2).Value = statusGroup.Count();
            statusRow++;
        }
        
        summarySheet.Cell(statusRow + 1, 1).Value = "By Fault Category:";
        summarySheet.Cell(statusRow + 1, 1).Style.Font.Bold = true;
        var catRow = statusRow + 2;
        foreach (var catGroup in defectsList.GroupBy(d => d.FaultCategory).OrderByDescending(g => g.Count()))
        {
            summarySheet.Cell(catRow, 1).Value = catGroup.Key.ToString();
            summarySheet.Cell(catRow, 2).Value = catGroup.Count();
            catRow++;
        }
        
        summarySheet.Columns().AdjustToContents();
        
        workbook.SaveAs(filePath);
    }
}
