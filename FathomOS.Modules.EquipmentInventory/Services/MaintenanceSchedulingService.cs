using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for managing maintenance schedules, reminders, and service tracking.
/// Supports recurring maintenance, calibration, and certification schedules.
/// </summary>
public class MaintenanceSchedulingService
{
    private readonly LocalDatabaseService _dbService;
    private readonly EquipmentHistoryService _historyService;
    
    public MaintenanceSchedulingService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        _historyService = new EquipmentHistoryService(dbService);
    }
    
    #region Schedule Management
    
    /// <summary>
    /// Get all upcoming maintenance items
    /// </summary>
    public async Task<List<MaintenanceItem>> GetUpcomingMaintenanceAsync(int daysAhead = 30)
    {
        var items = new List<MaintenanceItem>();
        var today = DateTime.Today;
        var endDate = today.AddDays(daysAhead);
        
        var equipment = await _dbService.Context.Equipment
            .Include(e => e.CurrentLocation)
            .Include(e => e.Category)
            .Where(e => e.IsActive)
            .AsNoTracking()
            .ToListAsync();
        
        foreach (var eq in equipment)
        {
            // Certification expiry
            if (eq.RequiresCertification && eq.CertificationExpiryDate.HasValue)
            {
                if (eq.CertificationExpiryDate.Value <= endDate)
                {
                    items.Add(new MaintenanceItem
                    {
                        EquipmentId = eq.EquipmentId,
                        AssetNumber = eq.AssetNumber,
                        EquipmentName = eq.Name ?? "Unknown",
                        LocationName = eq.CurrentLocation?.Name,
                        CategoryName = eq.Category?.Name,
                        MaintenanceType = MaintenanceType.Certification,
                        DueDate = eq.CertificationExpiryDate.Value,
                        IsOverdue = eq.CertificationExpiryDate.Value < today,
                        DaysUntilDue = (eq.CertificationExpiryDate.Value - today).Days,
                        Description = $"Certification expires: {eq.CertificationNumber}"
                    });
                }
            }
            
            // Calibration due
            if (eq.RequiresCalibration && eq.NextCalibrationDate.HasValue)
            {
                if (eq.NextCalibrationDate.Value <= endDate)
                {
                    items.Add(new MaintenanceItem
                    {
                        EquipmentId = eq.EquipmentId,
                        AssetNumber = eq.AssetNumber,
                        EquipmentName = eq.Name ?? "Unknown",
                        LocationName = eq.CurrentLocation?.Name,
                        CategoryName = eq.Category?.Name,
                        MaintenanceType = MaintenanceType.Calibration,
                        DueDate = eq.NextCalibrationDate.Value,
                        IsOverdue = eq.NextCalibrationDate.Value < today,
                        DaysUntilDue = (eq.NextCalibrationDate.Value - today).Days,
                        Description = $"Calibration due (interval: {eq.CalibrationIntervalDays} days)"
                    });
                }
            }
            
            // Service due
            if (eq.NextServiceDate.HasValue)
            {
                if (eq.NextServiceDate.Value <= endDate)
                {
                    items.Add(new MaintenanceItem
                    {
                        EquipmentId = eq.EquipmentId,
                        AssetNumber = eq.AssetNumber,
                        EquipmentName = eq.Name ?? "Unknown",
                        LocationName = eq.CurrentLocation?.Name,
                        CategoryName = eq.Category?.Name,
                        MaintenanceType = MaintenanceType.Service,
                        DueDate = eq.NextServiceDate.Value,
                        IsOverdue = eq.NextServiceDate.Value < today,
                        DaysUntilDue = (eq.NextServiceDate.Value - today).Days,
                        Description = $"Service due (interval: {eq.ServiceIntervalDays} days)"
                    });
                }
            }
            
            // Inspection due
            if (eq.LastInspectionDate.HasValue)
            {
                var nextInspection = eq.LastInspectionDate.Value.AddDays(90); // Quarterly inspections
                if (nextInspection <= endDate)
                {
                    items.Add(new MaintenanceItem
                    {
                        EquipmentId = eq.EquipmentId,
                        AssetNumber = eq.AssetNumber,
                        EquipmentName = eq.Name ?? "Unknown",
                        LocationName = eq.CurrentLocation?.Name,
                        CategoryName = eq.Category?.Name,
                        MaintenanceType = MaintenanceType.Inspection,
                        DueDate = nextInspection,
                        IsOverdue = nextInspection < today,
                        DaysUntilDue = (nextInspection - today).Days,
                        Description = "Quarterly inspection due"
                    });
                }
            }
        }
        
        return items.OrderBy(i => i.DueDate).ToList();
    }
    
    /// <summary>
    /// Get overdue maintenance items
    /// </summary>
    public async Task<List<MaintenanceItem>> GetOverdueMaintenanceAsync()
    {
        var all = await GetUpcomingMaintenanceAsync(365);
        return all.Where(i => i.IsOverdue).ToList();
    }
    
    /// <summary>
    /// Get maintenance summary by type
    /// </summary>
    public async Task<MaintenanceSummary> GetMaintenanceSummaryAsync()
    {
        var items = await GetUpcomingMaintenanceAsync(30);
        var overdue = items.Where(i => i.IsOverdue).ToList();
        var upcoming = items.Where(i => !i.IsOverdue).ToList();
        
        return new MaintenanceSummary
        {
            TotalOverdue = overdue.Count,
            TotalUpcoming = upcoming.Count,
            OverdueCertifications = overdue.Count(i => i.MaintenanceType == MaintenanceType.Certification),
            OverdueCalibrations = overdue.Count(i => i.MaintenanceType == MaintenanceType.Calibration),
            OverdueServices = overdue.Count(i => i.MaintenanceType == MaintenanceType.Service),
            OverdueInspections = overdue.Count(i => i.MaintenanceType == MaintenanceType.Inspection),
            UpcomingThisWeek = upcoming.Count(i => i.DaysUntilDue <= 7),
            UpcomingThisMonth = upcoming.Count(i => i.DaysUntilDue <= 30)
        };
    }
    
    #endregion
    
    #region Complete Maintenance
    
    /// <summary>
    /// Record certification completion
    /// </summary>
    public async Task<bool> CompleteCertificationAsync(Guid equipmentId, CertificationCompletionData data)
    {
        var equipment = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (equipment == null) return false;
        
        equipment.CertificationNumber = data.CertificationNumber;
        equipment.CertificationBody = data.CertificationBody;
        equipment.CertificationExpiryDate = data.ExpiryDate;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        await _historyService.RecordCertificationAsync(equipmentId, data.CertificationNumber, data.ExpiryDate, data.CertificationBody);
        
        return true;
    }
    
    /// <summary>
    /// Record calibration completion
    /// </summary>
    public async Task<bool> CompleteCalibrationAsync(Guid equipmentId, CalibrationCompletionData data)
    {
        var equipment = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (equipment == null) return false;
        
        equipment.LastCalibrationDate = data.CalibrationDate;
        
        // Calculate next calibration date
        if (equipment.CalibrationIntervalDays.HasValue)
        {
            equipment.NextCalibrationDate = data.CalibrationDate.AddDays(equipment.CalibrationIntervalDays.Value);
        }
        else if (data.NextDueDate.HasValue)
        {
            equipment.NextCalibrationDate = data.NextDueDate;
        }
        
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        await _historyService.RecordCalibrationAsync(equipmentId, data.CalibrationDate, equipment.NextCalibrationDate);
        
        return true;
    }
    
    /// <summary>
    /// Record service completion
    /// </summary>
    public async Task<bool> CompleteServiceAsync(Guid equipmentId, ServiceCompletionData data)
    {
        var equipment = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (equipment == null) return false;
        
        equipment.LastServiceDate = data.ServiceDate;
        
        // Calculate next service date
        if (equipment.ServiceIntervalDays.HasValue)
        {
            equipment.NextServiceDate = data.ServiceDate.AddDays(equipment.ServiceIntervalDays.Value);
        }
        else if (data.NextDueDate.HasValue)
        {
            equipment.NextServiceDate = data.NextDueDate;
        }
        
        // Update condition if changed during service
        if (!string.IsNullOrEmpty(data.ConditionAfterService))
        {
            if (Enum.TryParse<EquipmentCondition>(data.ConditionAfterService, out var newCondition))
                equipment.Condition = newCondition;
        }
        
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        await _historyService.RecordServiceAsync(equipmentId, data.ServiceType, data.Notes);
        
        return true;
    }
    
    /// <summary>
    /// Record inspection completion
    /// </summary>
    public async Task<bool> CompleteInspectionAsync(Guid equipmentId, InspectionCompletionData data)
    {
        var equipment = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (equipment == null) return false;
        
        equipment.LastInspectionDate = data.InspectionDate;
        
        // Update condition based on inspection result
        if (!string.IsNullOrEmpty(data.ConditionAfterInspection))
        {
            if (Enum.TryParse<EquipmentCondition>(data.ConditionAfterInspection, out var newCondition))
                equipment.Condition = newCondition;
        }
        
        // If failed inspection, mark for repair
        if (data.InspectionResult == InspectionResult.Failed)
        {
            equipment.Status = EquipmentStatus.UnderRepair;
        }
        
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        await _historyService.RecordCustomEventAsync(equipmentId, 
            $"Inspection completed: {data.InspectionResult}", data.Notes);
        
        return true;
    }
    
    #endregion
    
    #region Schedule Configuration
    
    /// <summary>
    /// Set calibration schedule for equipment
    /// </summary>
    public async Task<bool> SetCalibrationScheduleAsync(Guid equipmentId, int intervalDays, DateTime? startDate = null)
    {
        var equipment = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (equipment == null) return false;
        
        equipment.RequiresCalibration = true;
        equipment.CalibrationIntervalDays = intervalDays;
        
        if (startDate.HasValue)
        {
            equipment.LastCalibrationDate = startDate;
            equipment.NextCalibrationDate = startDate.Value.AddDays(intervalDays);
        }
        else if (equipment.LastCalibrationDate.HasValue)
        {
            equipment.NextCalibrationDate = equipment.LastCalibrationDate.Value.AddDays(intervalDays);
        }
        else
        {
            equipment.NextCalibrationDate = DateTime.Today.AddDays(intervalDays);
        }
        
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        return true;
    }
    
    /// <summary>
    /// Set service schedule for equipment
    /// </summary>
    public async Task<bool> SetServiceScheduleAsync(Guid equipmentId, int intervalDays, DateTime? startDate = null)
    {
        var equipment = await _dbService.Context.Equipment.FindAsync(equipmentId);
        if (equipment == null) return false;
        
        equipment.ServiceIntervalDays = intervalDays;
        
        if (startDate.HasValue)
        {
            equipment.LastServiceDate = startDate;
            equipment.NextServiceDate = startDate.Value.AddDays(intervalDays);
        }
        else if (equipment.LastServiceDate.HasValue)
        {
            equipment.NextServiceDate = equipment.LastServiceDate.Value.AddDays(intervalDays);
        }
        else
        {
            equipment.NextServiceDate = DateTime.Today.AddDays(intervalDays);
        }
        
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;
        
        await _dbService.Context.SaveChangesAsync();
        return true;
    }
    
    /// <summary>
    /// Bulk set schedules for multiple equipment
    /// </summary>
    public async Task<int> BulkSetCalibrationScheduleAsync(List<Guid> equipmentIds, int intervalDays)
    {
        int updated = 0;
        foreach (var id in equipmentIds)
        {
            if (await SetCalibrationScheduleAsync(id, intervalDays))
                updated++;
        }
        return updated;
    }
    
    #endregion
    
    #region Reminders
    
    /// <summary>
    /// Get items needing reminder notifications
    /// </summary>
    public async Task<List<MaintenanceReminder>> GetPendingRemindersAsync(int warningDays = 7)
    {
        var reminders = new List<MaintenanceReminder>();
        var items = await GetUpcomingMaintenanceAsync(warningDays);
        
        foreach (var item in items.Where(i => !i.IsOverdue))
        {
            reminders.Add(new MaintenanceReminder
            {
                EquipmentId = item.EquipmentId,
                AssetNumber = item.AssetNumber,
                EquipmentName = item.EquipmentName,
                ReminderType = item.MaintenanceType,
                DueDate = item.DueDate,
                DaysUntilDue = item.DaysUntilDue,
                Message = $"{item.MaintenanceType} due in {item.DaysUntilDue} days for {item.AssetNumber}",
                Priority = item.DaysUntilDue <= 3 ? ReminderPriority.High 
                    : item.DaysUntilDue <= 7 ? ReminderPriority.Medium 
                    : ReminderPriority.Low
            });
        }
        
        return reminders.OrderBy(r => r.DueDate).ToList();
    }
    
    #endregion
}

#region Models

public class MaintenanceItem
{
    public Guid EquipmentId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string? LocationName { get; set; }
    public string? CategoryName { get; set; }
    public Models.MaintenanceType MaintenanceType { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public string StatusText => IsOverdue 
        ? $"Overdue by {Math.Abs(DaysUntilDue)} days" 
        : $"Due in {DaysUntilDue} days";
    
    public string StatusColor => IsOverdue ? "#E53935" 
        : DaysUntilDue <= 7 ? "#FF9800" 
        : "#4CAF50";
}

public class MaintenanceSummary
{
    public int TotalOverdue { get; set; }
    public int TotalUpcoming { get; set; }
    public int OverdueCertifications { get; set; }
    public int OverdueCalibrations { get; set; }
    public int OverdueServices { get; set; }
    public int OverdueInspections { get; set; }
    public int UpcomingThisWeek { get; set; }
    public int UpcomingThisMonth { get; set; }
}

public class CertificationCompletionData
{
    public string? CertificationNumber { get; set; }
    public string? CertificationBody { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class CalibrationCompletionData
{
    public DateTime CalibrationDate { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string? CalibratedBy { get; set; }
    public string? Notes { get; set; }
}

public class ServiceCompletionData
{
    public DateTime ServiceDate { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public DateTime? NextDueDate { get; set; }
    public string? ConditionAfterService { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }
}

public class InspectionCompletionData
{
    public DateTime InspectionDate { get; set; }
    public InspectionResult InspectionResult { get; set; }
    public string? ConditionAfterInspection { get; set; }
    public string? InspectorName { get; set; }
    public string? Notes { get; set; }
}

public enum InspectionResult
{
    Passed,
    PassedWithNotes,
    Failed,
    RequiresFollowUp
}

public class MaintenanceReminder
{
    public Guid EquipmentId { get; set; }
    public string AssetNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public MaintenanceType ReminderType { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysUntilDue { get; set; }
    public string Message { get; set; } = string.Empty;
    public ReminderPriority Priority { get; set; }
}

public enum ReminderPriority
{
    Low,
    Medium,
    High
}

#endregion
