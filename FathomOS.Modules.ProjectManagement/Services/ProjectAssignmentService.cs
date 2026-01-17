using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.ProjectManagement.Data;
using FathomOS.Modules.ProjectManagement.Models;

namespace FathomOS.Modules.ProjectManagement.Services;

/// <summary>
/// Implementation of project resource assignment operations
/// </summary>
public class ProjectAssignmentService : IProjectAssignmentService
{
    private readonly ProjectDbContext _context;

    public ProjectAssignmentService(ProjectDbContext context)
    {
        _context = context;
    }

    #region Vessel Assignments

    public async Task<IEnumerable<ProjectVesselAssignment>> GetProjectVesselAssignmentsAsync(Guid projectId)
    {
        return await _context.VesselAssignments
            .Where(a => a.ProjectId == projectId && a.IsActive)
            .OrderBy(a => a.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProjectVesselAssignment>> GetVesselProjectsAsync(Guid vesselId)
    {
        return await _context.VesselAssignments
            .Include(a => a.Project)
            .Where(a => a.VesselId == vesselId && a.IsActive)
            .OrderByDescending(a => a.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<ProjectVesselAssignment> AssignVesselAsync(ProjectVesselAssignment assignment)
    {
        assignment.CreatedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.VesselAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<ProjectVesselAssignment> UpdateVesselAssignmentAsync(ProjectVesselAssignment assignment)
    {
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.VesselAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<bool> RemoveVesselAssignmentAsync(Guid assignmentId)
    {
        var assignment = await _context.VesselAssignments.FindAsync(assignmentId);
        if (assignment == null) return false;

        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Equipment Assignments

    public async Task<IEnumerable<ProjectEquipmentAssignment>> GetProjectEquipmentAssignmentsAsync(Guid projectId)
    {
        return await _context.EquipmentAssignments
            .Where(a => a.ProjectId == projectId && a.IsActive)
            .OrderBy(a => a.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProjectEquipmentAssignment>> GetEquipmentProjectsAsync(Guid equipmentId)
    {
        return await _context.EquipmentAssignments
            .Include(a => a.Project)
            .Where(a => a.EquipmentId == equipmentId && a.IsActive)
            .OrderByDescending(a => a.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<ProjectEquipmentAssignment> AssignEquipmentAsync(ProjectEquipmentAssignment assignment)
    {
        assignment.CreatedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.EquipmentAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<ProjectEquipmentAssignment> UpdateEquipmentAssignmentAsync(ProjectEquipmentAssignment assignment)
    {
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.EquipmentAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<bool> RemoveEquipmentAssignmentAsync(Guid assignmentId)
    {
        var assignment = await _context.EquipmentAssignments.FindAsync(assignmentId);
        if (assignment == null) return false;

        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Personnel Assignments

    public async Task<IEnumerable<ProjectPersonnelAssignment>> GetProjectPersonnelAssignmentsAsync(Guid projectId)
    {
        return await _context.PersonnelAssignments
            .Where(a => a.ProjectId == projectId && a.IsActive)
            .OrderBy(a => a.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProjectPersonnelAssignment>> GetPersonnelProjectsAsync(Guid personnelId)
    {
        return await _context.PersonnelAssignments
            .Include(a => a.Project)
            .Where(a => a.UserId == personnelId && a.IsActive)
            .OrderByDescending(a => a.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<ProjectPersonnelAssignment> AssignPersonnelAsync(ProjectPersonnelAssignment assignment)
    {
        assignment.CreatedAt = DateTime.UtcNow;
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.PersonnelAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<ProjectPersonnelAssignment> UpdatePersonnelAssignmentAsync(ProjectPersonnelAssignment assignment)
    {
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.PersonnelAssignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<bool> RemovePersonnelAssignmentAsync(Guid assignmentId)
    {
        var assignment = await _context.PersonnelAssignments.FindAsync(assignmentId);
        if (assignment == null) return false;

        assignment.IsActive = false;
        assignment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Availability

    public async Task<bool> IsVesselAvailableAsync(Guid vesselId, DateTime startDate, DateTime endDate, Guid? excludeProjectId = null)
    {
        var query = _context.VesselAssignments
            .Where(a => a.VesselId == vesselId && a.IsActive &&
                a.Status != AssignmentStatus.Completed &&
                a.Status != AssignmentStatus.Cancelled);

        if (excludeProjectId.HasValue)
        {
            query = query.Where(a => a.ProjectId != excludeProjectId.Value);
        }

        return !await query.AnyAsync(a =>
            a.PlannedStartDate.HasValue && a.PlannedEndDate.HasValue &&
            ((startDate >= a.PlannedStartDate.Value && startDate <= a.PlannedEndDate.Value) ||
            (endDate >= a.PlannedStartDate.Value && endDate <= a.PlannedEndDate.Value) ||
            (startDate <= a.PlannedStartDate.Value && endDate >= a.PlannedEndDate.Value)));
    }

    public async Task<bool> IsEquipmentAvailableAsync(Guid equipmentId, DateTime startDate, DateTime endDate, Guid? excludeProjectId = null)
    {
        var query = _context.EquipmentAssignments
            .Where(a => a.EquipmentId == equipmentId && a.IsActive &&
                a.Status != AssignmentStatus.Completed &&
                a.Status != AssignmentStatus.Cancelled);

        if (excludeProjectId.HasValue)
        {
            query = query.Where(a => a.ProjectId != excludeProjectId.Value);
        }

        return !await query.AnyAsync(a =>
            a.PlannedStartDate.HasValue && a.PlannedEndDate.HasValue &&
            ((startDate >= a.PlannedStartDate.Value && startDate <= a.PlannedEndDate.Value) ||
            (endDate >= a.PlannedStartDate.Value && endDate <= a.PlannedEndDate.Value) ||
            (startDate <= a.PlannedStartDate.Value && endDate >= a.PlannedEndDate.Value)));
    }

    public async Task<bool> IsPersonnelAvailableAsync(Guid personnelId, DateTime startDate, DateTime endDate, Guid? excludeProjectId = null)
    {
        var query = _context.PersonnelAssignments
            .Where(a => a.UserId == personnelId && a.IsActive &&
                a.Status != AssignmentStatus.Completed &&
                a.Status != AssignmentStatus.Cancelled);

        if (excludeProjectId.HasValue)
        {
            query = query.Where(a => a.ProjectId != excludeProjectId.Value);
        }

        return !await query.AnyAsync(a =>
            a.PlannedStartDate.HasValue && a.PlannedEndDate.HasValue &&
            ((startDate >= a.PlannedStartDate.Value && startDate <= a.PlannedEndDate.Value) ||
            (endDate >= a.PlannedStartDate.Value && endDate <= a.PlannedEndDate.Value) ||
            (startDate <= a.PlannedStartDate.Value && endDate >= a.PlannedEndDate.Value)));
    }

    #endregion
}
