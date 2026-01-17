using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// ViewModel for project dashboard with statistics and summaries
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;

    public DashboardViewModel(IProjectService projectService)
    {
        _projectService = projectService;

        // Initialize collections
        RecentProjects = new ObservableCollection<SurveyProject>();
        UpcomingMilestones = new ObservableCollection<ProjectMilestone>();
        OverdueDeliverables = new ObservableCollection<ProjectDeliverable>();
        ProjectsByStatus = new ObservableCollection<StatusCount>();
        ProjectsByType = new ObservableCollection<TypeCount>();

        // Initialize commands
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        ViewProjectCommand = new RelayCommand<SurveyProject>(ViewProject, p => p != null);
    }

    #region Statistics Properties

    private int _totalProjects;
    public int TotalProjects
    {
        get => _totalProjects;
        set => SetProperty(ref _totalProjects, value);
    }

    private int _activeProjects;
    public int ActiveProjects
    {
        get => _activeProjects;
        set => SetProperty(ref _activeProjects, value);
    }

    private int _completedProjects;
    public int CompletedProjects
    {
        get => _completedProjects;
        set => SetProperty(ref _completedProjects, value);
    }

    private int _onHoldProjects;
    public int OnHoldProjects
    {
        get => _onHoldProjects;
        set => SetProperty(ref _onHoldProjects, value);
    }

    private int _draftProjects;
    public int DraftProjects
    {
        get => _draftProjects;
        set => SetProperty(ref _draftProjects, value);
    }

    private int _totalClients;
    public int TotalClients
    {
        get => _totalClients;
        set => SetProperty(ref _totalClients, value);
    }

    private int _upcomingMilestoneCount;
    public int UpcomingMilestoneCount
    {
        get => _upcomingMilestoneCount;
        set => SetProperty(ref _upcomingMilestoneCount, value);
    }

    private int _overdueDeliverableCount;
    public int OverdueDeliverableCount
    {
        get => _overdueDeliverableCount;
        set => SetProperty(ref _overdueDeliverableCount, value);
    }

    private decimal _totalBudget;
    public decimal TotalBudget
    {
        get => _totalBudget;
        set => SetProperty(ref _totalBudget, value);
    }

    private decimal _averageCompletion;
    public decimal AverageCompletion
    {
        get => _averageCompletion;
        set => SetProperty(ref _averageCompletion, value);
    }

    #endregion

    #region Collection Properties

    public ObservableCollection<SurveyProject> RecentProjects { get; }
    public ObservableCollection<ProjectMilestone> UpcomingMilestones { get; }
    public ObservableCollection<ProjectDeliverable> OverdueDeliverables { get; }
    public ObservableCollection<StatusCount> ProjectsByStatus { get; }
    public ObservableCollection<TypeCount> ProjectsByType { get; }

    private SurveyProject? _selectedProject;
    public SurveyProject? SelectedProject
    {
        get => _selectedProject;
        set => SetProperty(ref _selectedProject, value);
    }

    #endregion

    #region Commands

    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ViewProjectCommand { get; }

    #endregion

    #region Events

    public event EventHandler<SurveyProject>? ViewProjectRequested;

    #endregion

    #region Methods

    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load all projects
            var allProjects = (await _projectService.GetAllProjectsAsync()).ToList();

            // Calculate statistics
            TotalProjects = await _projectService.GetTotalProjectCountAsync();
            ActiveProjects = await _projectService.GetActiveProjectCountAsync();

            CompletedProjects = allProjects.Count(p => p.Status == ProjectStatus.Completed);
            OnHoldProjects = allProjects.Count(p => p.Status == ProjectStatus.OnHold);
            DraftProjects = allProjects.Count(p => p.Status == ProjectStatus.Draft);

            // Calculate total budget and average completion
            TotalBudget = allProjects.Where(p => p.Budget.HasValue).Sum(p => p.Budget!.Value);
            AverageCompletion = allProjects.Any() ? allProjects.Average(p => p.PercentComplete) : 0;

            // Load clients count
            var clients = await _projectService.GetAllClientsAsync();
            TotalClients = clients.Count();

            // Load recent projects (last 5 updated)
            RecentProjects.Clear();
            var recent = allProjects.OrderByDescending(p => p.UpdatedAt).Take(5);
            foreach (var project in recent)
            {
                RecentProjects.Add(project);
            }

            // Projects by status
            ProjectsByStatus.Clear();
            var statusGroups = allProjects.GroupBy(p => p.Status)
                .Select(g => new StatusCount { Status = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count);
            foreach (var group in statusGroups)
            {
                ProjectsByStatus.Add(group);
            }

            // Projects by type
            ProjectsByType.Clear();
            var typeGroups = allProjects.GroupBy(p => p.ProjectType)
                .Select(g => new TypeCount { Type = g.Key, Count = g.Count() })
                .OrderByDescending(t => t.Count);
            foreach (var group in typeGroups)
            {
                ProjectsByType.Add(group);
            }

            // Load upcoming milestones (next 30 days)
            UpcomingMilestones.Clear();
            var upcomingDate = DateTime.Today.AddDays(30);
            foreach (var project in allProjects.Where(p => p.Status == ProjectStatus.Active))
            {
                var milestones = await _projectService.GetMilestonesAsync(project.ProjectId);
                var upcoming = milestones.Where(m =>
                    m.Status != MilestoneStatus.Completed &&
                    m.PlannedDate.HasValue &&
                    m.PlannedDate.Value >= DateTime.Today &&
                    m.PlannedDate.Value <= upcomingDate)
                    .OrderBy(m => m.PlannedDate);

                foreach (var milestone in upcoming.Take(3))
                {
                    UpcomingMilestones.Add(milestone);
                }
            }
            UpcomingMilestoneCount = UpcomingMilestones.Count;

            // Load overdue deliverables
            OverdueDeliverables.Clear();
            foreach (var project in allProjects.Where(p => p.Status == ProjectStatus.Active))
            {
                var deliverables = await _projectService.GetDeliverablesAsync(project.ProjectId);
                var overdue = deliverables.Where(d =>
                    d.Status != DeliverableStatus.Submitted &&
                    d.Status != DeliverableStatus.Accepted &&
                    d.PlannedDueDate.HasValue &&
                    d.PlannedDueDate.Value < DateTime.Today)
                    .OrderBy(d => d.PlannedDueDate);

                foreach (var deliverable in overdue)
                {
                    OverdueDeliverables.Add(deliverable);
                }
            }
            OverdueDeliverableCount = OverdueDeliverables.Count;

        }, "Loading dashboard...");
    }

    private void ViewProject(SurveyProject? project)
    {
        if (project != null)
        {
            ViewProjectRequested?.Invoke(this, project);
        }
    }

    #endregion
}

/// <summary>
/// Helper class for status counts
/// </summary>
public class StatusCount
{
    public ProjectStatus Status { get; set; }
    public int Count { get; set; }
    public string StatusName => Status.ToString();
}

/// <summary>
/// Helper class for type counts
/// </summary>
public class TypeCount
{
    public ProjectType Type { get; set; }
    public int Count { get; set; }
    public string TypeName => Type.ToString();
}
