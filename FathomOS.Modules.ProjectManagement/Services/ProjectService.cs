using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.ProjectManagement.Data;
using FathomOS.Modules.ProjectManagement.Models;
using Milestone = FathomOS.Modules.ProjectManagement.Models.ProjectMilestone;
using Deliverable = FathomOS.Modules.ProjectManagement.Models.ProjectDeliverable;

namespace FathomOS.Modules.ProjectManagement.Services;

/// <summary>
/// Implementation of project CRUD and status management operations
/// </summary>
public class ProjectService : IProjectService
{
    private readonly ProjectDbContext _context;

    public ProjectService(ProjectDbContext context)
    {
        _context = context;
    }

    #region Project CRUD

    public async Task<IEnumerable<SurveyProject>> GetAllProjectsAsync()
    {
        return await _context.Projects
            .Include(p => p.Client)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<SurveyProject?> GetProjectByIdAsync(Guid projectId)
    {
        return await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Milestones)
            .Include(p => p.Deliverables)
            .Include(p => p.VesselAssignments)
            .Include(p => p.EquipmentAssignments)
            .Include(p => p.PersonnelAssignments)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);
    }

    public async Task<SurveyProject?> GetProjectByNumberAsync(string projectNumber)
    {
        return await _context.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.ProjectNumber == projectNumber);
    }

    public async Task<IEnumerable<SurveyProject>> SearchProjectsAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _context.Projects
            .Include(p => p.Client)
            .Where(p => p.IsActive &&
                (p.ProjectName.ToLower().Contains(term) ||
                 p.ProjectNumber.ToLower().Contains(term) ||
                 (p.Description != null && p.Description.ToLower().Contains(term)) ||
                 (p.Client != null && p.Client.CompanyName.ToLower().Contains(term))))
            .OrderByDescending(p => p.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<SurveyProject>> GetProjectsByStatusAsync(ProjectStatus status)
    {
        return await _context.Projects
            .Include(p => p.Client)
            .Where(p => p.IsActive && p.Status == status)
            .OrderByDescending(p => p.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<SurveyProject>> GetActiveProjectsAsync()
    {
        return await _context.Projects
            .Include(p => p.Client)
            .Where(p => p.IsActive && p.Status == ProjectStatus.Active)
            .OrderByDescending(p => p.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<SurveyProject>> GetProjectsByClientAsync(Guid clientId)
    {
        return await _context.Projects
            .Where(p => p.IsActive && p.ClientId == clientId)
            .OrderByDescending(p => p.PlannedStartDate)
            .ToListAsync();
    }

    public async Task<SurveyProject> CreateProjectAsync(SurveyProject project)
    {
        project.ProjectNumber = await GenerateProjectNumberAsync();
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<SurveyProject> UpdateProjectAsync(SurveyProject project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        project.IsModifiedLocally = true;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<bool> DeleteProjectAsync(Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return false;

        project.IsActive = false;
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Status Management

    public async Task<bool> UpdateProjectStatusAsync(Guid projectId, ProjectStatus newStatus)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return false;

        project.Status = newStatus;
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProjectPhaseAsync(Guid projectId, ProjectPhase newPhase)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null) return false;

        project.Phase = newPhase;
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Milestones

    public async Task<IEnumerable<Milestone>> GetMilestonesAsync(Guid projectId)
    {
        return await _context.Milestones
            .Where(m => m.ProjectId == projectId && m.IsActive)
            .OrderBy(m => m.PlannedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Milestone>> GetUpcomingMilestonesAsync(int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context.Milestones
            .Include(m => m.Project)
            .Where(m => m.IsActive &&
                m.Status != MilestoneStatus.Completed &&
                m.PlannedDate <= cutoffDate)
            .OrderBy(m => m.PlannedDate)
            .ToListAsync();
    }

    public async Task<Milestone> CreateMilestoneAsync(Milestone milestone)
    {
        milestone.CreatedAt = DateTime.UtcNow;
        milestone.UpdatedAt = DateTime.UtcNow;
        _context.Milestones.Add(milestone);
        await _context.SaveChangesAsync();
        return milestone;
    }

    public async Task<Milestone> UpdateMilestoneAsync(Milestone milestone)
    {
        milestone.UpdatedAt = DateTime.UtcNow;
        _context.Milestones.Update(milestone);
        await _context.SaveChangesAsync();
        return milestone;
    }

    public async Task<bool> CompleteMilestoneAsync(Guid milestoneId, DateTime? completedDate = null)
    {
        var milestone = await _context.Milestones.FindAsync(milestoneId);
        if (milestone == null) return false;

        milestone.Status = MilestoneStatus.Completed;
        milestone.ActualDate = completedDate ?? DateTime.UtcNow;
        milestone.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMilestoneAsync(Guid milestoneId)
    {
        var milestone = await _context.Milestones.FindAsync(milestoneId);
        if (milestone == null) return false;

        milestone.IsActive = false;
        milestone.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Deliverables

    public async Task<IEnumerable<Deliverable>> GetDeliverablesAsync(Guid projectId)
    {
        return await _context.Deliverables
            .Where(d => d.ProjectId == projectId && d.IsActive)
            .OrderBy(d => d.PlannedDueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Deliverable>> GetPendingDeliverablesAsync()
    {
        return await _context.Deliverables
            .Include(d => d.Project)
            .Where(d => d.IsActive &&
                d.Status != DeliverableStatus.Accepted &&
                d.Status != DeliverableStatus.Submitted)
            .OrderBy(d => d.PlannedDueDate)
            .ToListAsync();
    }

    public async Task<Deliverable> CreateDeliverableAsync(Deliverable deliverable)
    {
        deliverable.CreatedAt = DateTime.UtcNow;
        deliverable.UpdatedAt = DateTime.UtcNow;
        _context.Deliverables.Add(deliverable);
        await _context.SaveChangesAsync();
        return deliverable;
    }

    public async Task<Deliverable> UpdateDeliverableAsync(Deliverable deliverable)
    {
        deliverable.UpdatedAt = DateTime.UtcNow;
        _context.Deliverables.Update(deliverable);
        await _context.SaveChangesAsync();
        return deliverable;
    }

    public async Task<bool> SubmitDeliverableAsync(Guid deliverableId, string? filePath = null)
    {
        var deliverable = await _context.Deliverables.FindAsync(deliverableId);
        if (deliverable == null) return false;

        deliverable.Status = DeliverableStatus.Submitted;
        deliverable.SubmissionDate = DateTime.UtcNow;
        if (filePath != null) deliverable.FilePath = filePath;
        deliverable.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApproveDeliverableAsync(Guid deliverableId, string? comments = null)
    {
        var deliverable = await _context.Deliverables.FindAsync(deliverableId);
        if (deliverable == null) return false;

        deliverable.Status = DeliverableStatus.Approved;
        deliverable.ApprovalDate = DateTime.UtcNow;
        deliverable.Notes = comments;
        deliverable.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectDeliverableAsync(Guid deliverableId, string reason)
    {
        var deliverable = await _context.Deliverables.FindAsync(deliverableId);
        if (deliverable == null) return false;

        deliverable.Status = DeliverableStatus.Rejected;
        deliverable.Notes = reason;
        deliverable.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Clients

    public async Task<IEnumerable<Client>> GetAllClientsAsync()
    {
        return await _context.Clients
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();
    }

    public async Task<Client?> GetClientByIdAsync(Guid clientId)
    {
        return await _context.Clients
            .Include(c => c.Projects)
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.ClientId == clientId);
    }

    public async Task<Client> CreateClientAsync(Client client)
    {
        client.CreatedAt = DateTime.UtcNow;
        client.UpdatedAt = DateTime.UtcNow;
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task<Client> UpdateClientAsync(Client client)
    {
        client.UpdatedAt = DateTime.UtcNow;
        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
        return client;
    }

    #endregion

    #region Statistics

    public async Task<int> GetTotalProjectCountAsync()
    {
        return await _context.Projects.CountAsync(p => p.IsActive);
    }

    public async Task<int> GetActiveProjectCountAsync()
    {
        return await _context.Projects.CountAsync(p => p.IsActive && p.Status == ProjectStatus.Active);
    }

    public async Task<ProjectStatistics> GetProjectStatisticsAsync(Guid projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Milestones)
            .Include(p => p.Deliverables)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null)
        {
            return new ProjectStatistics();
        }

        var activeMilestones = project.Milestones.Where(m => m.IsActive).ToList();
        var activeDeliverables = project.Deliverables.Where(d => d.IsActive).ToList();

        return new ProjectStatistics
        {
            TotalMilestones = activeMilestones.Count,
            CompletedMilestones = activeMilestones.Count(m => m.Status == MilestoneStatus.Completed),
            TotalDeliverables = activeDeliverables.Count,
            CompletedDeliverables = activeDeliverables.Count(d => d.Status == DeliverableStatus.Accepted),
            BudgetUtilization = project.Budget.HasValue && project.Budget.Value > 0 && project.ActualCost.HasValue
                ? (project.ActualCost.Value / project.Budget.Value) * 100 : 0,
            DaysRemaining = project.PlannedEndDate.HasValue ? Math.Max(0, (int)(project.PlannedEndDate.Value - DateTime.UtcNow).TotalDays) : 0,
            PercentComplete = project.PercentComplete
        };
    }

    #endregion

    #region Utilities

    private async Task<string> GenerateProjectNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PRJ-{year}-";

        var lastNumber = await _context.Projects
            .Where(p => p.ProjectNumber.StartsWith(prefix))
            .OrderByDescending(p => p.ProjectNumber)
            .Select(p => p.ProjectNumber)
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

        return $"{prefix}{sequence:D4}";
    }

    #endregion
}
