using FathomOS.Modules.ProjectManagement.Models;
using Milestone = FathomOS.Modules.ProjectManagement.Models.ProjectMilestone;
using Deliverable = FathomOS.Modules.ProjectManagement.Models.ProjectDeliverable;

namespace FathomOS.Modules.ProjectManagement.Services;

/// <summary>
/// Interface for project CRUD and status management operations
/// </summary>
public interface IProjectService
{
    // Project CRUD
    Task<IEnumerable<SurveyProject>> GetAllProjectsAsync();
    Task<SurveyProject?> GetProjectByIdAsync(Guid projectId);
    Task<SurveyProject?> GetProjectByNumberAsync(string projectNumber);
    Task<IEnumerable<SurveyProject>> SearchProjectsAsync(string searchTerm);
    Task<IEnumerable<SurveyProject>> GetProjectsByStatusAsync(ProjectStatus status);
    Task<IEnumerable<SurveyProject>> GetActiveProjectsAsync();
    Task<IEnumerable<SurveyProject>> GetProjectsByClientAsync(Guid clientId);
    Task<SurveyProject> CreateProjectAsync(SurveyProject project);
    Task<SurveyProject> UpdateProjectAsync(SurveyProject project);
    Task<bool> DeleteProjectAsync(Guid projectId);

    // Soft Delete Operations
    /// <summary>
    /// Restores a soft-deleted project
    /// </summary>
    Task<bool> RestoreProjectAsync(Guid projectId);

    /// <summary>
    /// Gets all soft-deleted projects
    /// </summary>
    Task<IEnumerable<SurveyProject>> GetDeletedProjectsAsync();

    /// <summary>
    /// Permanently deletes a project from the database
    /// </summary>
    Task<bool> PermanentlyDeleteProjectAsync(Guid projectId);

    /// <summary>
    /// Gets the count of soft-deleted projects
    /// </summary>
    Task<int> GetDeletedProjectCountAsync();

    // Status Management
    Task<bool> UpdateProjectStatusAsync(Guid projectId, ProjectStatus newStatus);
    Task<bool> UpdateProjectPhaseAsync(Guid projectId, ProjectPhase newPhase);

    // Milestones
    Task<IEnumerable<Milestone>> GetMilestonesAsync(Guid projectId);
    Task<IEnumerable<Milestone>> GetUpcomingMilestonesAsync(int daysAhead = 30);
    Task<Milestone> CreateMilestoneAsync(Milestone milestone);
    Task<Milestone> UpdateMilestoneAsync(Milestone milestone);
    Task<bool> CompleteMilestoneAsync(Guid milestoneId, DateTime? completedDate = null);
    Task<bool> DeleteMilestoneAsync(Guid milestoneId);

    // Deliverables
    Task<IEnumerable<Deliverable>> GetDeliverablesAsync(Guid projectId);
    Task<IEnumerable<Deliverable>> GetPendingDeliverablesAsync();
    Task<Deliverable> CreateDeliverableAsync(Deliverable deliverable);
    Task<Deliverable> UpdateDeliverableAsync(Deliverable deliverable);
    Task<bool> SubmitDeliverableAsync(Guid deliverableId, string? filePath = null);
    Task<bool> ApproveDeliverableAsync(Guid deliverableId, string? comments = null);
    Task<bool> RejectDeliverableAsync(Guid deliverableId, string reason);

    // Clients
    Task<IEnumerable<Client>> GetAllClientsAsync();
    Task<Client?> GetClientByIdAsync(Guid clientId);
    Task<Client> CreateClientAsync(Client client);
    Task<Client> UpdateClientAsync(Client client);

    // Statistics
    Task<int> GetTotalProjectCountAsync();
    Task<int> GetActiveProjectCountAsync();
    Task<ProjectStatistics> GetProjectStatisticsAsync(Guid projectId);
}

/// <summary>
/// Project statistics DTO
/// </summary>
public class ProjectStatistics
{
    public int TotalMilestones { get; set; }
    public int CompletedMilestones { get; set; }
    public int TotalDeliverables { get; set; }
    public int CompletedDeliverables { get; set; }
    public decimal BudgetUtilization { get; set; }
    public int DaysRemaining { get; set; }
    public decimal PercentComplete { get; set; }
}
