using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for tracking equipment history and audit trail.
/// Records all changes to equipment including status changes, transfers, edits, etc.
/// </summary>
public class EquipmentHistoryService
{
    private readonly LocalDatabaseService _dbService;
    private Guid? _currentUserId;
    
    public EquipmentHistoryService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
    }
    
    public void SetCurrentUser(Guid userId) => _currentUserId = userId;
    
    #region Record History
    
    /// <summary>
    /// Record equipment creation
    /// </summary>
    public async Task RecordCreationAsync(Equipment equipment)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipment.EquipmentId,
            Action = HistoryAction.Created,
            Description = $"Equipment created: {equipment.Name}",
            NewValue = JsonSerializer.Serialize(new
            {
                equipment.AssetNumber,
                equipment.Name,
                equipment.SerialNumber,
                CategoryName = equipment.Category?.Name,
                LocationName = equipment.CurrentLocation?.Name
            }),
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record equipment update with field changes
    /// </summary>
    public async Task RecordUpdateAsync(Equipment original, Equipment updated)
    {
        var changes = new List<FieldChange>();
        
        // Compare fields
        CompareField(changes, "Name", original.Name, updated.Name);
        CompareField(changes, "Description", original.Description, updated.Description);
        CompareField(changes, "SerialNumber", original.SerialNumber, updated.SerialNumber);
        CompareField(changes, "Manufacturer", original.Manufacturer, updated.Manufacturer);
        CompareField(changes, "Model", original.Model, updated.Model);
        CompareField(changes, "Status", original.Status.ToString(), updated.Status.ToString());
        CompareField(changes, "Condition", original.Condition.ToString(), updated.Condition.ToString());
        CompareField(changes, "Notes", original.Notes, updated.Notes);
        
        if (original.CategoryId != updated.CategoryId)
        {
            changes.Add(new FieldChange
            {
                FieldName = "Category",
                OldValue = original.Category?.Name,
                NewValue = updated.Category?.Name
            });
        }
        
        if (changes.Count == 0) return;
        
        var history = new EquipmentHistory
        {
            EquipmentId = updated.EquipmentId,
            Action = HistoryAction.Updated,
            Description = $"Updated {changes.Count} field(s)",
            OldValue = JsonSerializer.Serialize(changes.ToDictionary(c => c.FieldName, c => c.OldValue)),
            NewValue = JsonSerializer.Serialize(changes.ToDictionary(c => c.FieldName, c => c.NewValue)),
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record status change
    /// </summary>
    public async Task RecordStatusChangeAsync(Guid equipmentId, EquipmentStatus oldStatus, EquipmentStatus newStatus, string? reason = null)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = HistoryAction.StatusChanged,
            Description = $"Status changed from {oldStatus} to {newStatus}" + (reason != null ? $": {reason}" : ""),
            OldValue = oldStatus.ToString(),
            NewValue = newStatus.ToString(),
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record location transfer
    /// </summary>
    public async Task RecordTransferAsync(Guid equipmentId, Location? fromLocation, Location? toLocation, Guid? manifestId = null)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = HistoryAction.Transferred,
            Description = $"Transferred from {fromLocation?.Name ?? "Unknown"} to {toLocation?.Name ?? "Unknown"}",
            OldValue = fromLocation?.Name,
            NewValue = toLocation?.Name,
            FromLocationId = fromLocation?.LocationId,
            ToLocationId = toLocation?.LocationId,
            ManifestId = manifestId,
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record certification update
    /// </summary>
    public async Task RecordCertificationAsync(Guid equipmentId, string? certNumber, DateTime? expiryDate, string? body)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = HistoryAction.Certified,
            Description = $"Certification updated: {certNumber ?? "N/A"}, expires {expiryDate?.ToString("d") ?? "N/A"}",
            NewValue = JsonSerializer.Serialize(new { certNumber, expiryDate, body }),
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record calibration
    /// </summary>
    public async Task RecordCalibrationAsync(Guid equipmentId, DateTime calibrationDate, DateTime? nextDue)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = HistoryAction.Calibrated,
            Description = $"Calibrated on {calibrationDate:d}, next due {nextDue?.ToString("d") ?? "N/A"}",
            NewValue = JsonSerializer.Serialize(new { calibrationDate, nextDue }),
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record service/maintenance
    /// </summary>
    public async Task RecordServiceAsync(Guid equipmentId, string serviceType, string? notes)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = HistoryAction.Serviced,
            Description = $"Service performed: {serviceType}",
            NewValue = notes,
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record photo added/removed
    /// </summary>
    public async Task RecordPhotoChangeAsync(Guid equipmentId, bool added, string? photoPath)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = added ? HistoryAction.PhotoAdded : HistoryAction.PhotoRemoved,
            Description = added ? "Photo added" : "Photo removed",
            NewValue = photoPath,
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record document added/removed
    /// </summary>
    public async Task RecordDocumentChangeAsync(Guid equipmentId, bool added, string documentName)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = added ? HistoryAction.DocumentAdded : HistoryAction.DocumentRemoved,
            Description = added ? $"Document added: {documentName}" : $"Document removed: {documentName}",
            NewValue = documentName,
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    /// <summary>
    /// Record custom event
    /// </summary>
    public async Task RecordCustomEventAsync(Guid equipmentId, string description, string? details = null)
    {
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = HistoryAction.Custom,
            Description = description,
            NewValue = details,
            PerformedBy = _currentUserId,
            PerformedAt = DateTime.UtcNow
        };
        
        await SaveHistoryAsync(history);
    }
    
    #endregion
    
    #region Query History
    
    /// <summary>
    /// Get full history for an equipment item
    /// </summary>
    public async Task<List<EquipmentHistoryItem>> GetEquipmentHistoryAsync(Guid equipmentId, int? limit = null)
    {
        var query = _dbService.Context.Set<EquipmentHistory>()
            .Where(h => h.EquipmentId == equipmentId)
            .OrderByDescending(h => h.PerformedAt)
            .AsQueryable();
        
        if (limit.HasValue)
            query = query.Take(limit.Value);
        
        var histories = await query.AsNoTracking().ToListAsync();
        
        // Load user names
        var userIds = histories.Where(h => h.PerformedBy.HasValue).Select(h => h.PerformedBy!.Value).Distinct().ToList();
        var users = await _dbService.Context.Users.Where(u => userIds.Contains(u.UserId)).ToDictionaryAsync(u => u.UserId, u => u.DisplayName);
        
        return histories.Select(h => new EquipmentHistoryItem
        {
            HistoryId = h.HistoryId,
            Action = h.Action,
            ActionDisplay = GetActionDisplay(h.Action),
            ActionIcon = GetActionIcon(h.Action),
            Description = h.Description ?? string.Empty,
            OldValue = h.OldValue,
            NewValue = h.NewValue,
            PerformedAt = h.PerformedAt,
            PerformedByName = h.PerformedBy.HasValue && users.ContainsKey(h.PerformedBy.Value) 
                ? users[h.PerformedBy.Value] 
                : "System"
        }).ToList();
    }
    
    /// <summary>
    /// Get recent history across all equipment
    /// </summary>
    public async Task<List<EquipmentHistoryItem>> GetRecentHistoryAsync(int count = 50)
    {
        var histories = await _dbService.Context.Set<EquipmentHistory>()
            .Include(h => h.Equipment)
            .OrderByDescending(h => h.PerformedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
        
        var userIds = histories.Where(h => h.PerformedBy.HasValue).Select(h => h.PerformedBy!.Value).Distinct().ToList();
        var users = await _dbService.Context.Users.Where(u => userIds.Contains(u.UserId)).ToDictionaryAsync(u => u.UserId, u => u.DisplayName);
        
        return histories.Select(h => new EquipmentHistoryItem
        {
            HistoryId = h.HistoryId,
            EquipmentId = h.EquipmentId,
            EquipmentName = h.Equipment?.Name ?? "Unknown",
            AssetNumber = h.Equipment?.AssetNumber ?? "Unknown",
            Action = h.Action,
            ActionDisplay = GetActionDisplay(h.Action),
            ActionIcon = GetActionIcon(h.Action),
            Description = h.Description ?? string.Empty,
            PerformedAt = h.PerformedAt,
            PerformedByName = h.PerformedBy.HasValue && users.ContainsKey(h.PerformedBy.Value) 
                ? users[h.PerformedBy.Value] 
                : "System"
        }).ToList();
    }
    
    /// <summary>
    /// Get transfer history for an equipment item
    /// </summary>
    public async Task<List<TransferHistoryItem>> GetTransferHistoryAsync(Guid equipmentId)
    {
        var histories = await _dbService.Context.Set<EquipmentHistory>()
            .Where(h => h.EquipmentId == equipmentId && h.Action == HistoryAction.Transferred)
            .OrderByDescending(h => h.PerformedAt)
            .AsNoTracking()
            .ToListAsync();
        
        var locationIds = histories.SelectMany(h => new[] { h.FromLocationId, h.ToLocationId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var locations = await _dbService.Context.Locations
            .Where(l => locationIds.Contains(l.LocationId))
            .ToDictionaryAsync(l => l.LocationId, l => l.Name);
        
        return histories.Select(h => new TransferHistoryItem
        {
            TransferDate = h.PerformedAt,
            FromLocation = h.FromLocationId.HasValue && locations.ContainsKey(h.FromLocationId.Value) 
                ? locations[h.FromLocationId.Value] : "Unknown",
            ToLocation = h.ToLocationId.HasValue && locations.ContainsKey(h.ToLocationId.Value) 
                ? locations[h.ToLocationId.Value] : "Unknown",
            ManifestId = h.ManifestId
        }).ToList();
    }
    
    #endregion
    
    #region Helpers
    
    private async Task SaveHistoryAsync(EquipmentHistory history)
    {
        _dbService.Context.Set<EquipmentHistory>().Add(history);
        await _dbService.Context.SaveChangesAsync();
    }
    
    private void CompareField(List<FieldChange> changes, string fieldName, string? oldValue, string? newValue)
    {
        if (oldValue != newValue)
        {
            changes.Add(new FieldChange { FieldName = fieldName, OldValue = oldValue, NewValue = newValue });
        }
    }
    
    private static string GetActionDisplay(HistoryAction action) => action switch
    {
        HistoryAction.Created => "Created",
        HistoryAction.Updated => "Updated",
        HistoryAction.Deleted => "Deleted",
        HistoryAction.StatusChanged => "Status Changed",
        HistoryAction.Transferred => "Transferred",
        HistoryAction.Certified => "Certified",
        HistoryAction.Calibrated => "Calibrated",
        HistoryAction.Serviced => "Serviced",
        HistoryAction.PhotoAdded => "Photo Added",
        HistoryAction.PhotoRemoved => "Photo Removed",
        HistoryAction.DocumentAdded => "Document Added",
        HistoryAction.DocumentRemoved => "Document Removed",
        HistoryAction.Custom => "Event",
        _ => action.ToString()
    };
    
    private static string GetActionIcon(HistoryAction action) => action switch
    {
        HistoryAction.Created => "Plus",
        HistoryAction.Updated => "Pencil",
        HistoryAction.Deleted => "Delete",
        HistoryAction.StatusChanged => "SwapHorizontal",
        HistoryAction.Transferred => "TruckDelivery",
        HistoryAction.Certified => "Certificate",
        HistoryAction.Calibrated => "Tune",
        HistoryAction.Serviced => "Wrench",
        HistoryAction.PhotoAdded => "Camera",
        HistoryAction.PhotoRemoved => "CameraOff",
        HistoryAction.DocumentAdded => "FileDocument",
        HistoryAction.DocumentRemoved => "FileRemove",
        HistoryAction.Custom => "Information",
        _ => "Circle"
    };
    
    #endregion
}

#region Models

// Note: HistoryAction enum is defined in Models/SupportingModels.cs

public class EquipmentHistoryItem
{
    public Guid HistoryId { get; set; }
    public Guid EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public string? AssetNumber { get; set; }
    public HistoryAction Action { get; set; }
    public string ActionDisplay { get; set; } = string.Empty;
    public string ActionIcon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime PerformedAt { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    
    // Computed properties for XAML bindings
    public bool HasValueChange => !string.IsNullOrEmpty(OldValue) || !string.IsNullOrEmpty(NewValue);
    public bool HasPerformedBy => !string.IsNullOrEmpty(PerformedByName);
    
    public string TimeAgo => GetTimeAgo(PerformedAt);
    
    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return dateTime.ToString("MMM d, yyyy");
    }
}

public class TransferHistoryItem
{
    public DateTime TransferDate { get; set; }
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    public Guid? ManifestId { get; set; }
}

public class FieldChange
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

#endregion
