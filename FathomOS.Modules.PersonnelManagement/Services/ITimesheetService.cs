using FathomOS.Modules.PersonnelManagement.Models;

namespace FathomOS.Modules.PersonnelManagement.Services;

/// <summary>
/// Interface for timesheet management and approval workflow
/// </summary>
public interface ITimesheetService
{
    // Timesheet CRUD
    Task<IEnumerable<Timesheet>> GetTimesheetsForPersonnelAsync(Guid personnelId);
    Task<Timesheet?> GetTimesheetByIdAsync(Guid timesheetId);
    Task<Timesheet?> GetTimesheetByNumberAsync(string timesheetNumber);
    Task<IEnumerable<Timesheet>> GetTimesheetsByStatusAsync(TimesheetStatus status);
    Task<IEnumerable<Timesheet>> GetTimesheetsPendingApprovalAsync(Guid? approverId = null);
    Task<Timesheet> CreateTimesheetAsync(Timesheet timesheet);
    Task<Timesheet> UpdateTimesheetAsync(Timesheet timesheet);
    Task<bool> DeleteTimesheetAsync(Guid timesheetId);

    // Timesheet Entries
    Task<IEnumerable<TimesheetEntry>> GetTimesheetEntriesAsync(Guid timesheetId);
    Task<TimesheetEntry> AddEntryAsync(TimesheetEntry entry);
    Task<TimesheetEntry> UpdateEntryAsync(TimesheetEntry entry);
    Task<bool> DeleteEntryAsync(Guid entryId);
    Task RecalculateTimesheetTotalsAsync(Guid timesheetId);

    // Approval Workflow
    Task<bool> SubmitTimesheetAsync(Guid timesheetId, string? comments);
    Task<bool> ApproveTimesheetAsync(Guid timesheetId, Guid approverId, string? comments);
    Task<bool> RejectTimesheetAsync(Guid timesheetId, Guid rejectorId, string reason);
    Task<bool> ProcessTimesheetAsync(Guid timesheetId, string? payrollBatchReference);

    // Utilities
    Task<Timesheet> CreateTimesheetForPeriodAsync(Guid personnelId, DateTime periodStart, DateTime periodEnd, Guid? vesselId = null, Guid? projectId = null);
    Task<string> GenerateTimesheetNumberAsync();
}
