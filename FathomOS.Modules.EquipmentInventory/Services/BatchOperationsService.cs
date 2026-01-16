using System.IO;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for batch operations: batch label printing, bulk status updates, and equipment duplication.
/// </summary>
public class BatchOperationsService
{
    private readonly LocalDatabaseService _dbService;
    private readonly QRCodeService _qrService;
    private readonly LabelPrintService _printService;
    private readonly EquipmentHistoryService _historyService;
    private readonly ModuleSettings _settings;
    
    public BatchOperationsService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        _settings = ModuleSettings.Load();
        _qrService = new QRCodeService(_settings);
        _printService = new LabelPrintService(_settings);
        _historyService = new EquipmentHistoryService(dbService);
    }
    
    #region Batch Label Printing
    
    /// <summary>
    /// Generate labels for multiple equipment items
    /// </summary>
    public async Task<List<LabelData>> GenerateLabelsAsync(List<Guid> equipmentIds)
    {
        var labels = new List<LabelData>();
        
        var equipment = await _dbService.Context.Equipment
            .Where(e => equipmentIds.Contains(e.EquipmentId))
            .AsNoTracking()
            .ToListAsync();
        
        foreach (var eq in equipment)
        {
            var uniqueId = eq.UniqueId ?? eq.AssetNumber;
            var qrContent = QRCodeService.GenerateEquipmentQrCodeWithUniqueId(eq.AssetNumber, eq.UniqueId);
            var labelBytes = _qrService.GenerateLabelPng(uniqueId, qrContent);
            
            labels.Add(new LabelData
            {
                EquipmentId = eq.EquipmentId,
                AssetNumber = eq.AssetNumber,
                UniqueId = eq.UniqueId,
                Name = eq.Name,
                LabelImageBytes = labelBytes
            });
        }
        
        return labels;
    }
    
    /// <summary>
    /// Print labels for multiple equipment items
    /// </summary>
    public async Task<BatchPrintResult> PrintLabelsAsync(List<Guid> equipmentIds, int copiesPerLabel = 1)
    {
        var result = new BatchPrintResult { TotalRequested = equipmentIds.Count };
        
        try
        {
            var labels = await GenerateLabelsAsync(equipmentIds);
            
            foreach (var label in labels)
            {
                try
                {
                    for (int i = 0; i < copiesPerLabel; i++)
                    {
                        if (_printService.PrintLabel(label.LabelImageBytes, 1))
                        {
                            result.SuccessfulPrints++;
                        }
                        else
                        {
                            result.FailedPrints++;
                            result.Errors.Add($"{label.AssetNumber}: Print cancelled or failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.FailedPrints++;
                    result.Errors.Add($"{label.AssetNumber}: {ex.Message}");
                }
            }
            
            result.Success = result.FailedPrints == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Batch print failed: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Export labels to PDF for batch printing
    /// </summary>
    public async Task<string> ExportLabelsToPdfAsync(List<Guid> equipmentIds, string outputPath)
    {
        var labels = await GenerateLabelsAsync(equipmentIds);
        
        // Create PDF with all labels using QuestPDF
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10);
                
                page.Content().Column(column =>
                {
                    // 3 columns, 4 rows per page for 50x50mm labels
                    var labelsPerRow = 3;
                    var rows = labels.Chunk(labelsPerRow);
                    
                    foreach (var row in rows)
                    {
                        column.Item().Row(r =>
                        {
                            foreach (var label in row)
                            {
                                r.RelativeItem().Padding(5).Image(label.LabelImageBytes);
                            }
                            // Fill empty cells
                            for (int i = row.Length; i < labelsPerRow; i++)
                            {
                                r.RelativeItem();
                            }
                        });
                    }
                });
            });
        }).GeneratePdf(outputPath);
        
        return outputPath;
    }
    
    #endregion
    
    #region Bulk Status Update
    
    /// <summary>
    /// Update status for multiple equipment items
    /// </summary>
    public async Task<BulkUpdateResult> BulkUpdateStatusAsync(List<Guid> equipmentIds, EquipmentStatus newStatus, string? reason = null)
    {
        var result = new BulkUpdateResult { TotalRequested = equipmentIds.Count };
        
        try
        {
            var equipment = await _dbService.Context.Equipment
                .Where(e => equipmentIds.Contains(e.EquipmentId))
                .ToListAsync();
            
            foreach (var eq in equipment)
            {
                var oldStatus = eq.Status;
                if (oldStatus == newStatus)
                {
                    result.Skipped++;
                    continue;
                }
                
                eq.Status = newStatus;
                eq.UpdatedAt = DateTime.UtcNow;
                eq.IsModifiedLocally = true;
                
                // Record history
                await _historyService.RecordStatusChangeAsync(eq.EquipmentId, oldStatus, newStatus, reason);
                
                result.Updated++;
            }
            
            await _dbService.Context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        
        return result;
    }
    
    /// <summary>
    /// Bulk update location for multiple equipment
    /// </summary>
    public async Task<BulkUpdateResult> BulkUpdateLocationAsync(List<Guid> equipmentIds, Guid newLocationId)
    {
        var result = new BulkUpdateResult { TotalRequested = equipmentIds.Count };
        
        try
        {
            var newLocation = await _dbService.Context.Locations.FindAsync(newLocationId);
            if (newLocation == null)
            {
                result.Error = "Location not found";
                return result;
            }
            
            var equipment = await _dbService.Context.Equipment
                .Include(e => e.CurrentLocation)
                .Where(e => equipmentIds.Contains(e.EquipmentId))
                .ToListAsync();
            
            foreach (var eq in equipment)
            {
                if (eq.CurrentLocationId == newLocationId)
                {
                    result.Skipped++;
                    continue;
                }
                
                var oldLocation = eq.CurrentLocation;
                eq.CurrentLocationId = newLocationId;
                eq.UpdatedAt = DateTime.UtcNow;
                eq.IsModifiedLocally = true;
                
                // Record transfer
                await _historyService.RecordTransferAsync(eq.EquipmentId, oldLocation, newLocation);
                
                result.Updated++;
            }
            
            await _dbService.Context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        
        return result;
    }
    
    /// <summary>
    /// Bulk delete (soft delete) equipment
    /// </summary>
    public async Task<BulkUpdateResult> BulkDeleteAsync(List<Guid> equipmentIds)
    {
        var result = new BulkUpdateResult { TotalRequested = equipmentIds.Count };
        
        try
        {
            var equipment = await _dbService.Context.Equipment
                .Where(e => equipmentIds.Contains(e.EquipmentId))
                .ToListAsync();
            
            foreach (var eq in equipment)
            {
                eq.IsActive = false;
                eq.Status = EquipmentStatus.Disposed;
                eq.UpdatedAt = DateTime.UtcNow;
                eq.IsModifiedLocally = true;
                result.Updated++;
            }
            
            await _dbService.Context.SaveChangesAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
        }
        
        return result;
    }
    
    #endregion
    
    #region Equipment Duplication
    
    /// <summary>
    /// Duplicate an equipment item with options
    /// </summary>
    public async Task<Equipment?> DuplicateEquipmentAsync(Guid sourceEquipmentId, DuplicateOptions? options = null)
    {
        options ??= new DuplicateOptions();
        
        var source = await _dbService.Context.Equipment
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EquipmentId == sourceEquipmentId);
        
        if (source == null) return null;
        
        // Create duplicate
        var duplicate = new Equipment
        {
            EquipmentId = Guid.NewGuid(),
            AssetNumber = await _dbService.GenerateAssetNumberAsync(source.Category?.Code),
            Name = options.NewName ?? $"{source.Name} (Copy)",
            Description = source.Description,
            CategoryId = source.CategoryId,
            TypeId = source.TypeId,
            Manufacturer = source.Manufacturer,
            Model = source.Model,
            PartNumber = source.PartNumber,
            
            // Reset serial/unique identifiers
            SerialNumber = options.CopySerialNumber ? source.SerialNumber : null,
            UniqueId = null, // Will be generated
            
            // Copy location or use specified
            CurrentLocationId = options.NewLocationId ?? source.CurrentLocationId,
            
            // Reset status
            Status = EquipmentStatus.Available,
            Condition = source.Condition,
            
            // Copy specs
            WeightKg = source.WeightKg,
            LengthCm = source.LengthCm,
            WidthCm = source.WidthCm,
            HeightCm = source.HeightCm,
            
            // Copy certification/calibration settings but reset dates
            RequiresCertification = source.RequiresCertification,
            RequiresCalibration = source.RequiresCalibration,
            CalibrationIntervalDays = source.CalibrationIntervalDays,
            
            // Copy purchase info if requested
            PurchasePrice = options.CopyPurchaseInfo ? source.PurchasePrice : null,
            SupplierId = options.CopyPurchaseInfo ? source.SupplierId : null,
            PurchaseDate = options.CopyPurchaseInfo ? source.PurchaseDate : null,
            WarrantyExpiryDate = options.CopyPurchaseInfo ? source.WarrantyExpiryDate : null,
            
            // Consumable settings
            IsConsumable = source.IsConsumable,
            MinimumQuantity = source.MinimumQuantity,
            ReorderPoint = source.ReorderPoint,
            
            // Reset to new
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Generate unique ID
        if (_settings.AutoGenerateUniqueId && source.Category != null)
        {
            duplicate.UniqueId = await _dbService.GenerateUniqueIdAsync(
                _settings.OrganizationCode, 
                source.Category.Code ?? "EQP");
        }
        
        // Generate QR code
        duplicate.QrCode = QRCodeService.GenerateEquipmentQrCodeWithUniqueId(duplicate.AssetNumber, duplicate.UniqueId);
        
        _dbService.Context.Equipment.Add(duplicate);
        await _dbService.Context.SaveChangesAsync();
        
        // Record creation history
        await _historyService.RecordCreationAsync(duplicate);
        
        return duplicate;
    }
    
    /// <summary>
    /// Create multiple copies of an equipment item
    /// </summary>
    public async Task<List<Equipment>> DuplicateMultipleAsync(Guid sourceEquipmentId, int count, DuplicateOptions? options = null)
    {
        var duplicates = new List<Equipment>();
        
        for (int i = 0; i < count; i++)
        {
            var opts = options ?? new DuplicateOptions();
            if (count > 1)
            {
                // Auto-number the copies
                var baseName = opts.NewName ?? (await _dbService.Context.Equipment.FindAsync(sourceEquipmentId))?.Name ?? "Equipment";
                opts.NewName = $"{baseName} ({i + 1})";
            }
            
            var duplicate = await DuplicateEquipmentAsync(sourceEquipmentId, opts);
            if (duplicate != null)
                duplicates.Add(duplicate);
        }
        
        return duplicates;
    }
    
    #endregion
    
    #region Quick Actions
    
    /// <summary>
    /// Mark equipment as in use
    /// </summary>
    public async Task<bool> MarkAsInUseAsync(Guid equipmentId, Guid? projectId = null, string? notes = null)
    {
        var eq = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (eq == null) return false;
        
        var oldStatus = eq.Status;
        eq.Status = EquipmentStatus.InUse;
        eq.CurrentProjectId = projectId;
        eq.Notes = notes ?? eq.Notes;
        eq.UpdatedAt = DateTime.UtcNow;
        eq.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        await _historyService.RecordStatusChangeAsync(equipmentId, oldStatus, EquipmentStatus.InUse, notes);
        
        return true;
    }
    
    /// <summary>
    /// Return equipment to available
    /// </summary>
    public async Task<bool> ReturnToAvailableAsync(Guid equipmentId, string? notes = null)
    {
        var eq = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (eq == null) return false;
        
        var oldStatus = eq.Status;
        eq.Status = EquipmentStatus.Available;
        eq.CurrentProjectId = null;
        eq.Notes = notes ?? eq.Notes;
        eq.UpdatedAt = DateTime.UtcNow;
        eq.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        await _historyService.RecordStatusChangeAsync(equipmentId, oldStatus, EquipmentStatus.Available, notes);
        
        return true;
    }
    
    /// <summary>
    /// Send equipment for repair
    /// </summary>
    public async Task<bool> SendForRepairAsync(Guid equipmentId, string? issueDescription = null)
    {
        var eq = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (eq == null) return false;
        
        var oldStatus = eq.Status;
        eq.Status = EquipmentStatus.UnderRepair;
        eq.Notes = issueDescription ?? eq.Notes;
        eq.UpdatedAt = DateTime.UtcNow;
        eq.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        await _historyService.RecordStatusChangeAsync(equipmentId, oldStatus, EquipmentStatus.UnderRepair, issueDescription);
        
        return true;
    }
    
    #endregion
}

#region Models

public class LabelData
{
    public Guid EquipmentId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string? UniqueId { get; set; }
    public string? Name { get; set; }
    public byte[] LabelImageBytes { get; set; } = Array.Empty<byte>();
}

public class BatchPrintResult
{
    public bool Success { get; set; }
    public int TotalRequested { get; set; }
    public int SuccessfulPrints { get; set; }
    public int FailedPrints { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class BulkUpdateResult
{
    public bool Success { get; set; }
    public int TotalRequested { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public string? Error { get; set; }
}

public class DuplicateOptions
{
    public string? NewName { get; set; }
    public Guid? NewLocationId { get; set; }
    public bool CopySerialNumber { get; set; } = false;
    public bool CopyPurchaseInfo { get; set; } = false;
    public bool CopyPhotos { get; set; } = false;
    public bool CopyDocuments { get; set; } = false;
}

#endregion
