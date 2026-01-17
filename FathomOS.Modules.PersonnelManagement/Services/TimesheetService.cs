using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.PersonnelManagement.Data;
using FathomOS.Modules.PersonnelManagement.Models;

namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// Implementation of timesheet management and approval workflow
/// </summary>
public class TimesheetService : ITimesheetService
{
    private readonly PersonnelDbContext _context;

    public TimesheetService(PersonnelDbContext context)
    {
        _context = context;
    }

    #region Timesheet CRUD

    public async Task<IEnumerable<Timesheet>> GetTimesheetsForPersonnelAsync(Guid personnelId)
    {
        return await _context.Timesheets
            .Include(t => t.Personnel)
            .Where(t => t.PersonnelId == personnelId && t.IsActive)
            .OrderByDescending(t => t.PeriodStartDate)
            .ToListAsync();
    }

    public async Task<Timesheet?> GetTimesheetByIdAsync(Guid timesheetId)
    {
        return await _context.Timesheets
            .Include(t => t.Personnel)
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId);
    }

    public async Task<Timesheet?> GetTimesheetByNumberAsync(string timesheetNumber)
    {
        return await _context.Timesheets
            .Include(t => t.Personnel)
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.TimesheetNumber == timesheetNumber);
    }

    public async Task<IEnumerable<Timesheet>> GetTimesheetsByStatusAsync(TimesheetStatus status)
    {
        return await _context.Timesheets
            .Include(t => t.Personnel)
            .Where(t => t.Status == status && t.IsActive)
            .OrderByDescending(t => t.PeriodStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Timesheet>> GetTimesheetsPendingApprovalAsync(Guid? approverId = null)
    {
        return await _context.Timesheets
            .Include(t => t.Personnel)
            .Where(t => t.Status == TimesheetStatus.Submitted && t.IsActive)
            .OrderBy(t => t.SubmittedAt)
            .ToListAsync();
    }

    public async Task<Timesheet> CreateTimesheetAsync(Timesheet timesheet)
    {
        timesheet.TimesheetNumber = await GenerateTimesheetNumberAsync();
        timesheet.CreatedAt = DateTime.UtcNow;
        timesheet.UpdatedAt = DateTime.UtcNow;
        _context.Timesheets.Add(timesheet);
        await _context.SaveChangesAsync();
        return timesheet;
    }

    public async Task<Timesheet> UpdateTimesheetAsync(Timesheet timesheet)
    {
        timesheet.UpdatedAt = DateTime.UtcNow;
        timesheet.IsModifiedLocally = true;
        _context.Timesheets.Update(timesheet);
        await _context.SaveChangesAsync();
        return timesheet;
    }

    public async Task<bool> DeleteTimesheetAsync(Guid timesheetId)
    {
        var timesheet = await _context.Timesheets.FindAsync(timesheetId);
        if (timesheet == null || timesheet.Status != TimesheetStatus.Draft) return false;

        timesheet.IsActive = false;
        timesheet.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Timesheet Entries

    public async Task<IEnumerable<TimesheetEntry>> GetTimesheetEntriesAsync(Guid timesheetId)
    {
        return await _context.TimesheetEntries
            .Where(e => e.TimesheetId == timesheetId)
            .OrderBy(e => e.EntryDate)
            .ToListAsync();
    }

    public async Task<TimesheetEntry> AddEntryAsync(TimesheetEntry entry)
    {
        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;
        _context.TimesheetEntries.Add(entry);
        await _context.SaveChangesAsync();
        await RecalculateTimesheetTotalsAsync(entry.TimesheetId);
        return entry;
    }

    public async Task<TimesheetEntry> UpdateEntryAsync(TimesheetEntry entry)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        _context.TimesheetEntries.Update(entry);
        await _context.SaveChangesAsync();
        await RecalculateTimesheetTotalsAsync(entry.TimesheetId);
        return entry;
    }

    public async Task<bool> DeleteEntryAsync(Guid entryId)
    {
        var entry = await _context.TimesheetEntries.FindAsync(entryId);
        if (entry == null) return false;

        var timesheetId = entry.TimesheetId;
        _context.TimesheetEntries.Remove(entry);
        await _context.SaveChangesAsync();
        await RecalculateTimesheetTotalsAsync(timesheetId);
        return true;
    }

    public async Task RecalculateTimesheetTotalsAsync(Guid timesheetId)
    {
        var timesheet = await _context.Timesheets
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.TimesheetId == timesheetId);

        if (timesheet == null) return;

        timesheet.TotalRegularHours = timesheet.Entries.Sum(e => e.RegularHours);
        timesheet.TotalOvertimeHours = timesheet.Entries.Sum(e => e.OvertimeHours);
        timesheet.TotalDoubleTimeHours = timesheet.Entries.Sum(e => e.DoubleTimeHours);
        timesheet.TotalNightShiftHours = timesheet.Entries.Sum(e => e.NightShiftHours);
        timesheet.TotalStandbyHours = timesheet.Entries.Sum(e => e.StandbyHours);
        timesheet.TotalTravelHours = timesheet.Entries.Sum(e => e.TravelHours);
        timesheet.TotalLeaveDays = timesheet.Entries.Count(e => e.IsLeave);
        timesheet.TotalSickDays = timesheet.Entries.Count(e => e.IsSickDay);
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Approval Workflow

    public async Task<bool> SubmitTimesheetAsync(Guid timesheetId, string? comments)
    {
        var timesheet = await _context.Timesheets.FindAsync(timesheetId);
        if (timesheet == null || !timesheet.CanSubmit) return false;

        timesheet.Status = TimesheetStatus.Submitted;
        timesheet.SubmittedAt = DateTime.UtcNow;
        timesheet.SubmissionComments = comments;
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApproveTimesheetAsync(Guid timesheetId, Guid approverId, string? comments)
    {
        var timesheet = await _context.Timesheets.FindAsync(timesheetId);
        if (timesheet == null || !timesheet.CanApprove) return false;

        timesheet.Status = TimesheetStatus.Approved;
        timesheet.ApprovedBy = approverId;
        timesheet.ApprovedAt = DateTime.UtcNow;
        timesheet.ApprovalComments = comments;
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectTimesheetAsync(Guid timesheetId, Guid rejectorId, string reason)
    {
        var timesheet = await _context.Timesheets.FindAsync(timesheetId);
        if (timesheet == null || timesheet.Status != TimesheetStatus.Submitted) return false;

        timesheet.Status = TimesheetStatus.Rejected;
        timesheet.RejectedBy = rejectorId;
        timesheet.RejectedAt = DateTime.UtcNow;
        timesheet.RejectionReason = reason;
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ProcessTimesheetAsync(Guid timesheetId, string? payrollBatchReference)
    {
        var timesheet = await _context.Timesheets.FindAsync(timesheetId);
        if (timesheet == null || timesheet.Status != TimesheetStatus.Approved) return false;

        timesheet.Status = TimesheetStatus.Processed;
        timesheet.ProcessedAt = DateTime.UtcNow;
        timesheet.PayrollBatchReference = payrollBatchReference;
        timesheet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Utilities

    public async Task<Timesheet> CreateTimesheetForPeriodAsync(Guid personnelId, DateTime periodStart, DateTime periodEnd, Guid? vesselId = null, Guid? projectId = null)
    {
        var personnel = await _context.Personnel.FindAsync(personnelId);

        var timesheet = new Timesheet
        {
            PersonnelId = personnelId,
            PeriodStartDate = periodStart,
            PeriodEndDate = periodEnd,
            VesselId = vesselId,
            ProjectId = projectId,
            Status = TimesheetStatus.Draft
        };

        return await CreateTimesheetAsync(timesheet);
    }

    public async Task<string> GenerateTimesheetNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"TS-{year}-";

        var lastNumber = await _context.Timesheets
            .Where(t => t.TimesheetNumber.StartsWith(prefix))
            .OrderByDescending(t => t.TimesheetNumber)
            .Select(t => t.TimesheetNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var lastSeq = lastNumber.Substring(prefix.Length);
            if (int.TryParse(lastSeq, out var parsed))
            {
                sequence = parsed + 1;
            }
        }

        return $"{prefix}{sequence:D6}";
    }

    #endregion
}
