using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service implementation for managing calibration reminders and notifications.
/// </summary>
public class CalibrationReminderService : ICalibrationReminderService
{
    private readonly LocalDatabaseService _dbService;
    private int _reminderThresholdDays = 30;

    public CalibrationReminderService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <inheritdoc />
    public async Task<List<CalibrationReminderItem>> GetUpcomingCalibrationsAsync(int daysAhead = 30)
    {
        var today = DateTime.Today;
        var futureDate = today.AddDays(daysAhead);

        var equipment = await _dbService.Context.Equipment
            .Include(e => e.CurrentLocation)
            .Include(e => e.Category)
            .Where(e => e.IsActive &&
                        e.RequiresCalibration &&
                        e.NextCalibrationDate != null &&
                        e.NextCalibrationDate >= today &&
                        e.NextCalibrationDate <= futureDate)
            .OrderBy(e => e.NextCalibrationDate)
            .ToListAsync();

        return equipment.Select(e => MapToReminderItem(e)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<CalibrationReminderItem>> GetOverdueCalibrationsAsync()
    {
        var today = DateTime.Today;

        var equipment = await _dbService.Context.Equipment
            .Include(e => e.CurrentLocation)
            .Include(e => e.Category)
            .Where(e => e.IsActive &&
                        e.RequiresCalibration &&
                        e.NextCalibrationDate != null &&
                        e.NextCalibrationDate < today)
            .OrderBy(e => e.NextCalibrationDate)
            .ToListAsync();

        return equipment.Select(e => MapToReminderItem(e)).ToList();
    }

    /// <inheritdoc />
    public void SetReminderThresholdDays(int days)
    {
        if (days < 1)
        {
            throw new ArgumentException("Reminder threshold must be at least 1 day.", nameof(days));
        }
        _reminderThresholdDays = days;
    }

    /// <inheritdoc />
    public int GetReminderThresholdDays()
    {
        return _reminderThresholdDays;
    }

    /// <inheritdoc />
    public async Task<List<CalibrationReminderItem>> GetAllCalibrationRemindersAsync()
    {
        var overdue = await GetOverdueCalibrationsAsync();
        var upcoming = await GetUpcomingCalibrationsAsync(_reminderThresholdDays);

        // Combine and sort by due date (overdue first, then upcoming)
        return overdue
            .Concat(upcoming)
            .OrderBy(r => r.NextCalibrationDate)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<CalibrationSummary> GetCalibrationSummaryAsync()
    {
        var today = DateTime.Today;
        var thresholdDate = today.AddDays(_reminderThresholdDays);

        var equipmentRequiringCalibration = await _dbService.Context.Equipment
            .Include(e => e.CurrentLocation)
            .Include(e => e.Category)
            .Where(e => e.IsActive && e.RequiresCalibration)
            .ToListAsync();

        var summary = new CalibrationSummary
        {
            TotalRequiringCalibration = equipmentRequiringCalibration.Count
        };

        foreach (var equipment in equipmentRequiringCalibration)
        {
            var status = GetCalibrationStatus(equipment, today, thresholdDate);
            var item = MapToReminderItem(equipment);

            switch (status)
            {
                case CalibrationStatus.Current:
                    summary.CurrentCount++;
                    break;
                case CalibrationStatus.DueSoon:
                    summary.DueSoonCount++;
                    summary.DueSoonItems.Add(item);
                    break;
                case CalibrationStatus.Overdue:
                    summary.OverdueCount++;
                    summary.OverdueItems.Add(item);
                    break;
                case CalibrationStatus.Unknown:
                    summary.UnknownCount++;
                    break;
            }
        }

        // Sort the items by due date
        summary.DueSoonItems = summary.DueSoonItems.OrderBy(i => i.NextCalibrationDate).ToList();
        summary.OverdueItems = summary.OverdueItems.OrderBy(i => i.NextCalibrationDate).ToList();

        return summary;
    }

    /// <inheritdoc />
    public async Task<List<Equipment>> GetEquipmentByCalibrationStatusAsync(CalibrationStatus status)
    {
        var today = DateTime.Today;
        var thresholdDate = today.AddDays(_reminderThresholdDays);

        var query = _dbService.Context.Equipment
            .Include(e => e.CurrentLocation)
            .Include(e => e.Category)
            .Where(e => e.IsActive);

        switch (status)
        {
            case CalibrationStatus.NotRequired:
                query = query.Where(e => !e.RequiresCalibration);
                break;

            case CalibrationStatus.Current:
                query = query.Where(e => e.RequiresCalibration &&
                                         e.NextCalibrationDate != null &&
                                         e.NextCalibrationDate > thresholdDate);
                break;

            case CalibrationStatus.DueSoon:
                query = query.Where(e => e.RequiresCalibration &&
                                         e.NextCalibrationDate != null &&
                                         e.NextCalibrationDate >= today &&
                                         e.NextCalibrationDate <= thresholdDate);
                break;

            case CalibrationStatus.Overdue:
                query = query.Where(e => e.RequiresCalibration &&
                                         e.NextCalibrationDate != null &&
                                         e.NextCalibrationDate < today);
                break;

            case CalibrationStatus.Unknown:
                query = query.Where(e => e.RequiresCalibration &&
                                         e.NextCalibrationDate == null);
                break;
        }

        return await query
            .OrderBy(e => e.NextCalibrationDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Equipment> RecordCalibrationAsync(
        Guid equipmentId,
        DateTime calibrationDate,
        string? performedBy = null,
        string? notes = null,
        string? certificateNumber = null)
    {
        var equipment = await _dbService.Context.Equipment
            .FirstOrDefaultAsync(e => e.EquipmentId == equipmentId);

        if (equipment == null)
        {
            throw new InvalidOperationException($"Equipment with ID {equipmentId} not found.");
        }

        if (!equipment.RequiresCalibration)
        {
            throw new InvalidOperationException($"Equipment '{equipment.Name}' does not require calibration.");
        }

        // Update equipment calibration dates
        equipment.LastCalibrationDate = calibrationDate;

        // Calculate next calibration date based on interval
        if (equipment.CalibrationIntervalDays.HasValue && equipment.CalibrationIntervalDays > 0)
        {
            equipment.NextCalibrationDate = calibrationDate.AddDays(equipment.CalibrationIntervalDays.Value);
        }
        else
        {
            // Default to 1 year if no interval is set
            equipment.NextCalibrationDate = calibrationDate.AddYears(1);
        }

        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;

        // Create maintenance record for calibration
        var maintenanceRecord = new MaintenanceRecord
        {
            EquipmentId = equipmentId,
            MaintenanceType = MaintenanceType.Calibration,
            Description = $"Calibration performed",
            PerformedDate = calibrationDate,
            PerformedBy = performedBy,
            NextDueDate = equipment.NextCalibrationDate,
            Notes = notes,
            WorkOrderNumber = certificateNumber,
            IsCompleted = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbService.Context.MaintenanceRecords.Add(maintenanceRecord);

        // Record history
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = HistoryAction.Calibrated,
            Description = $"Calibration performed by {performedBy ?? "Unknown"}",
            OldValue = equipment.LastCalibrationDate?.ToString("yyyy-MM-dd"),
            NewValue = calibrationDate.ToString("yyyy-MM-dd"),
            PerformedAt = DateTime.UtcNow,
            Notes = notes
        };
        _dbService.Context.EquipmentHistory.Add(history);

        await _dbService.Context.SaveChangesAsync();

        return equipment;
    }

    /// <inheritdoc />
    public async Task<List<MaintenanceRecord>> GetCalibrationHistoryAsync(Guid equipmentId)
    {
        return await _dbService.Context.MaintenanceRecords
            .Where(m => m.EquipmentId == equipmentId &&
                        m.MaintenanceType == MaintenanceType.Calibration)
            .OrderByDescending(m => m.PerformedDate)
            .ToListAsync();
    }

    #region Private Helper Methods

    private CalibrationReminderItem MapToReminderItem(Equipment equipment)
    {
        var today = DateTime.Today;
        var daysUntilDue = equipment.NextCalibrationDate.HasValue
            ? (int)(equipment.NextCalibrationDate.Value - today).TotalDays
            : int.MaxValue;

        return new CalibrationReminderItem
        {
            EquipmentId = equipment.EquipmentId,
            AssetNumber = equipment.AssetNumber,
            UniqueId = equipment.UniqueId,
            Name = equipment.Name,
            Manufacturer = equipment.Manufacturer,
            Model = equipment.Model,
            SerialNumber = equipment.SerialNumber,
            LocationName = equipment.CurrentLocation?.Name,
            LocationId = equipment.CurrentLocationId,
            CategoryName = equipment.Category?.Name,
            LastCalibrationDate = equipment.LastCalibrationDate,
            NextCalibrationDate = equipment.NextCalibrationDate,
            CalibrationIntervalDays = equipment.CalibrationIntervalDays,
            DaysUntilDue = daysUntilDue,
            Status = GetCalibrationStatus(equipment, today, today.AddDays(_reminderThresholdDays)),
            Equipment = equipment
        };
    }

    private static CalibrationStatus GetCalibrationStatus(Equipment equipment, DateTime today, DateTime thresholdDate)
    {
        if (!equipment.RequiresCalibration)
        {
            return CalibrationStatus.NotRequired;
        }

        if (!equipment.NextCalibrationDate.HasValue)
        {
            return CalibrationStatus.Unknown;
        }

        var nextDue = equipment.NextCalibrationDate.Value;

        if (nextDue < today)
        {
            return CalibrationStatus.Overdue;
        }

        if (nextDue <= thresholdDate)
        {
            return CalibrationStatus.DueSoon;
        }

        return CalibrationStatus.Current;
    }

    #endregion
}
