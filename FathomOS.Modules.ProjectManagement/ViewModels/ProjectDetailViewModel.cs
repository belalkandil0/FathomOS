using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// ViewModel for project detail view with editing capabilities for all related data
/// </summary>
public class ProjectDetailViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectAssignmentService _assignmentService;
    private readonly Guid? _projectId;
    private bool _isNewProject;

    public ProjectDetailViewModel(IProjectService projectService, IProjectAssignmentService assignmentService, Guid? projectId = null)
    {
        _projectService = projectService;
        _assignmentService = assignmentService;
        _projectId = projectId;
        _isNewProject = !projectId.HasValue;

        // Initialize collections
        Clients = new ObservableCollection<Client>();
        Milestones = new ObservableCollection<ProjectMilestone>();
        Deliverables = new ObservableCollection<ProjectDeliverable>();
        VesselAssignments = new ObservableCollection<ProjectVesselAssignment>();
        EquipmentAssignments = new ObservableCollection<ProjectEquipmentAssignment>();
        PersonnelAssignments = new ObservableCollection<ProjectPersonnelAssignment>();
        ProjectTypes = new ObservableCollection<ProjectType>();
        ProjectStatuses = new ObservableCollection<ProjectStatus>();
        ProjectPhases = new ObservableCollection<ProjectPhase>();
        BillingTypes = new ObservableCollection<BillingType>();
        Currencies = new ObservableCollection<CurrencyCode>();
        PriorityLevels = new ObservableCollection<PriorityLevel>();

        // Initialize commands
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);

        // Milestone commands
        AddMilestoneCommand = new RelayCommand(AddMilestone);
        EditMilestoneCommand = new RelayCommand<ProjectMilestone>(EditMilestone, CanEditMilestone);
        DeleteMilestoneCommand = new AsyncRelayCommand<ProjectMilestone>(DeleteMilestoneAsync, CanDeleteMilestone);
        CompleteMilestoneCommand = new AsyncRelayCommand<ProjectMilestone>(CompleteMilestoneAsync, CanCompleteMilestone);

        // Deliverable commands
        AddDeliverableCommand = new RelayCommand(AddDeliverable);
        EditDeliverableCommand = new RelayCommand<ProjectDeliverable>(EditDeliverable, CanEditDeliverable);
        DeleteDeliverableCommand = new AsyncRelayCommand<ProjectDeliverable>(DeleteDeliverableAsync, CanDeleteDeliverable);
        SubmitDeliverableCommand = new AsyncRelayCommand<ProjectDeliverable>(SubmitDeliverableAsync, CanSubmitDeliverable);

        // Initialize enum collections
        foreach (ProjectType type in Enum.GetValues(typeof(ProjectType)))
            ProjectTypes.Add(type);
        foreach (ProjectStatus status in Enum.GetValues(typeof(ProjectStatus)))
            ProjectStatuses.Add(status);
        foreach (ProjectPhase phase in Enum.GetValues(typeof(ProjectPhase)))
            ProjectPhases.Add(phase);
        foreach (BillingType billing in Enum.GetValues(typeof(BillingType)))
            BillingTypes.Add(billing);
        foreach (CurrencyCode currency in Enum.GetValues(typeof(CurrencyCode)))
            Currencies.Add(currency);
        foreach (PriorityLevel priority in Enum.GetValues(typeof(PriorityLevel)))
            PriorityLevels.Add(priority);

        // Initialize new project
        if (_isNewProject)
        {
            Project = new SurveyProject
            {
                Status = ProjectStatus.Draft,
                Phase = ProjectPhase.Initiation,
                ProjectType = ProjectType.Other,
                BillingType = BillingType.DayRate,
                Currency = CurrencyCode.USD,
                Priority = PriorityLevel.Normal,
                PlannedStartDate = DateTime.Today
            };
        }
    }

    #region Project Properties

    private SurveyProject? _project;
    public SurveyProject? Project
    {
        get => _project;
        set
        {
            if (SetProperty(ref _project, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(IsExistingProject));
            }
        }
    }

    public string Title => _isNewProject ? "New Project" : $"Edit: {Project?.ProjectName}";
    public bool IsExistingProject => !_isNewProject;
    public bool IsNewProject => _isNewProject;

    // Bindable project properties
    public string ProjectNumber
    {
        get => Project?.ProjectNumber ?? "(Auto-generated)";
        set
        {
            if (Project != null && Project.ProjectNumber != value)
            {
                Project.ProjectNumber = value;
                OnPropertyChanged();
            }
        }
    }

    public string ProjectName
    {
        get => Project?.ProjectName ?? string.Empty;
        set
        {
            if (Project != null && Project.ProjectName != value)
            {
                Project.ProjectName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    public string? ProjectCode
    {
        get => Project?.ProjectCode;
        set
        {
            if (Project != null)
            {
                Project.ProjectCode = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ClientReference
    {
        get => Project?.ClientReference;
        set
        {
            if (Project != null)
            {
                Project.ClientReference = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ContractNumber
    {
        get => Project?.ContractNumber;
        set
        {
            if (Project != null)
            {
                Project.ContractNumber = value;
                OnPropertyChanged();
            }
        }
    }

    public ProjectType SelectedProjectType
    {
        get => Project?.ProjectType ?? ProjectType.Other;
        set
        {
            if (Project != null && Project.ProjectType != value)
            {
                Project.ProjectType = value;
                OnPropertyChanged();
            }
        }
    }

    public ProjectStatus SelectedStatus
    {
        get => Project?.Status ?? ProjectStatus.Draft;
        set
        {
            if (Project != null && Project.Status != value)
            {
                Project.Status = value;
                OnPropertyChanged();
            }
        }
    }

    public ProjectPhase SelectedPhase
    {
        get => Project?.Phase ?? ProjectPhase.Initiation;
        set
        {
            if (Project != null && Project.Phase != value)
            {
                Project.Phase = value;
                OnPropertyChanged();
            }
        }
    }

    public PriorityLevel SelectedPriority
    {
        get => Project?.Priority ?? PriorityLevel.Normal;
        set
        {
            if (Project != null && Project.Priority != value)
            {
                Project.Priority = value;
                OnPropertyChanged();
            }
        }
    }

    public BillingType SelectedBillingType
    {
        get => Project?.BillingType ?? BillingType.DayRate;
        set
        {
            if (Project != null && Project.BillingType != value)
            {
                Project.BillingType = value;
                OnPropertyChanged();
            }
        }
    }

    public CurrencyCode SelectedCurrency
    {
        get => Project?.Currency ?? CurrencyCode.USD;
        set
        {
            if (Project != null && Project.Currency != value)
            {
                Project.Currency = value;
                OnPropertyChanged();
            }
        }
    }

    public Client? SelectedClient
    {
        get => Clients.FirstOrDefault(c => c?.ClientId == Project?.ClientId);
        set
        {
            if (Project != null)
            {
                Project.ClientId = value?.ClientId;
                Project.Client = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Description
    {
        get => Project?.Description;
        set
        {
            if (Project != null)
            {
                Project.Description = value;
                OnPropertyChanged();
            }
        }
    }

    public string? ScopeOfWork
    {
        get => Project?.ScopeOfWork;
        set
        {
            if (Project != null)
            {
                Project.ScopeOfWork = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Objectives
    {
        get => Project?.Objectives;
        set
        {
            if (Project != null)
            {
                Project.Objectives = value;
                OnPropertyChanged();
            }
        }
    }

    public string? LocationName
    {
        get => Project?.LocationName;
        set
        {
            if (Project != null)
            {
                Project.LocationName = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Region
    {
        get => Project?.Region;
        set
        {
            if (Project != null)
            {
                Project.Region = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Country
    {
        get => Project?.Country;
        set
        {
            if (Project != null)
            {
                Project.Country = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? PlannedStartDate
    {
        get => Project?.PlannedStartDate;
        set
        {
            if (Project != null)
            {
                Project.PlannedStartDate = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? PlannedEndDate
    {
        get => Project?.PlannedEndDate;
        set
        {
            if (Project != null)
            {
                Project.PlannedEndDate = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? ActualStartDate
    {
        get => Project?.ActualStartDate;
        set
        {
            if (Project != null)
            {
                Project.ActualStartDate = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime? ActualEndDate
    {
        get => Project?.ActualEndDate;
        set
        {
            if (Project != null)
            {
                Project.ActualEndDate = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal? Budget
    {
        get => Project?.Budget;
        set
        {
            if (Project != null)
            {
                Project.Budget = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal? ContractValue
    {
        get => Project?.ContractValue;
        set
        {
            if (Project != null)
            {
                Project.ContractValue = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal? EstimatedCost
    {
        get => Project?.EstimatedCost;
        set
        {
            if (Project != null)
            {
                Project.EstimatedCost = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal? ActualCost
    {
        get => Project?.ActualCost;
        set
        {
            if (Project != null)
            {
                Project.ActualCost = value;
                OnPropertyChanged();
            }
        }
    }

    public decimal PercentComplete
    {
        get => Project?.PercentComplete ?? 0;
        set
        {
            if (Project != null)
            {
                Project.PercentComplete = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Notes
    {
        get => Project?.Notes;
        set
        {
            if (Project != null)
            {
                Project.Notes = value;
                OnPropertyChanged();
            }
        }
    }

    public string? InternalNotes
    {
        get => Project?.InternalNotes;
        set
        {
            if (Project != null)
            {
                Project.InternalNotes = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Collections

    public ObservableCollection<Client> Clients { get; }
    public ObservableCollection<ProjectMilestone> Milestones { get; }
    public ObservableCollection<ProjectDeliverable> Deliverables { get; }
    public ObservableCollection<ProjectVesselAssignment> VesselAssignments { get; }
    public ObservableCollection<ProjectEquipmentAssignment> EquipmentAssignments { get; }
    public ObservableCollection<ProjectPersonnelAssignment> PersonnelAssignments { get; }
    public ObservableCollection<ProjectType> ProjectTypes { get; }
    public ObservableCollection<ProjectStatus> ProjectStatuses { get; }
    public ObservableCollection<ProjectPhase> ProjectPhases { get; }
    public ObservableCollection<BillingType> BillingTypes { get; }
    public ObservableCollection<CurrencyCode> Currencies { get; }
    public ObservableCollection<PriorityLevel> PriorityLevels { get; }

    private ProjectMilestone? _selectedMilestone;
    public ProjectMilestone? SelectedMilestone
    {
        get => _selectedMilestone;
        set => SetProperty(ref _selectedMilestone, value);
    }

    private ProjectDeliverable? _selectedDeliverable;
    public ProjectDeliverable? SelectedDeliverable
    {
        get => _selectedDeliverable;
        set => SetProperty(ref _selectedDeliverable, value);
    }

    #endregion

    #region Commands

    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ICommand AddMilestoneCommand { get; }
    public ICommand EditMilestoneCommand { get; }
    public ICommand DeleteMilestoneCommand { get; }
    public ICommand CompleteMilestoneCommand { get; }

    public ICommand AddDeliverableCommand { get; }
    public ICommand EditDeliverableCommand { get; }
    public ICommand DeleteDeliverableCommand { get; }
    public ICommand SubmitDeliverableCommand { get; }

    #endregion

    #region Events

    public event EventHandler? SaveCompleted;
    public event EventHandler? CancelRequested;
    public event EventHandler<ProjectMilestone?>? AddMilestoneRequested;
    public event EventHandler<ProjectMilestone>? EditMilestoneRequested;
    public event EventHandler<ProjectDeliverable?>? AddDeliverableRequested;
    public event EventHandler<ProjectDeliverable>? EditDeliverableRequested;

    #endregion

    #region Methods

    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load clients
            var clients = await _projectService.GetAllClientsAsync();
            Clients.Clear();
            foreach (var client in clients)
            {
                Clients.Add(client);
            }

            // Load project if editing
            if (_projectId.HasValue)
            {
                Project = await _projectService.GetProjectByIdAsync(_projectId.Value);
                if (Project == null)
                {
                    ErrorMessage = "Project not found.";
                    return;
                }

                // Load milestones
                var milestones = await _projectService.GetMilestonesAsync(_projectId.Value);
                Milestones.Clear();
                foreach (var milestone in milestones)
                {
                    Milestones.Add(milestone);
                }

                // Load deliverables
                var deliverables = await _projectService.GetDeliverablesAsync(_projectId.Value);
                Deliverables.Clear();
                foreach (var deliverable in deliverables)
                {
                    Deliverables.Add(deliverable);
                }

                // Load assignments
                var vessels = await _assignmentService.GetProjectVesselAssignmentsAsync(_projectId.Value);
                VesselAssignments.Clear();
                foreach (var vessel in vessels)
                {
                    VesselAssignments.Add(vessel);
                }

                var equipment = await _assignmentService.GetProjectEquipmentAssignmentsAsync(_projectId.Value);
                EquipmentAssignments.Clear();
                foreach (var equip in equipment)
                {
                    EquipmentAssignments.Add(equip);
                }

                var personnel = await _assignmentService.GetProjectPersonnelAssignmentsAsync(_projectId.Value);
                PersonnelAssignments.Clear();
                foreach (var person in personnel)
                {
                    PersonnelAssignments.Add(person);
                }

                // Notify all property changes
                NotifyAllPropertiesChanged();
            }
        }, "Loading project...");
    }

    private void NotifyAllPropertiesChanged()
    {
        OnPropertyChanged(nameof(ProjectNumber));
        OnPropertyChanged(nameof(ProjectName));
        OnPropertyChanged(nameof(ProjectCode));
        OnPropertyChanged(nameof(ClientReference));
        OnPropertyChanged(nameof(ContractNumber));
        OnPropertyChanged(nameof(SelectedProjectType));
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(SelectedPhase));
        OnPropertyChanged(nameof(SelectedPriority));
        OnPropertyChanged(nameof(SelectedBillingType));
        OnPropertyChanged(nameof(SelectedCurrency));
        OnPropertyChanged(nameof(SelectedClient));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(ScopeOfWork));
        OnPropertyChanged(nameof(Objectives));
        OnPropertyChanged(nameof(LocationName));
        OnPropertyChanged(nameof(Region));
        OnPropertyChanged(nameof(Country));
        OnPropertyChanged(nameof(PlannedStartDate));
        OnPropertyChanged(nameof(PlannedEndDate));
        OnPropertyChanged(nameof(ActualStartDate));
        OnPropertyChanged(nameof(ActualEndDate));
        OnPropertyChanged(nameof(Budget));
        OnPropertyChanged(nameof(ContractValue));
        OnPropertyChanged(nameof(EstimatedCost));
        OnPropertyChanged(nameof(ActualCost));
        OnPropertyChanged(nameof(PercentComplete));
        OnPropertyChanged(nameof(Notes));
        OnPropertyChanged(nameof(InternalNotes));
    }

    private bool CanSave()
    {
        return Project != null &&
               !string.IsNullOrWhiteSpace(Project.ProjectName) &&
               !IsBusy;
    }

    private async Task SaveAsync()
    {
        if (Project == null) return;

        await ExecuteAsync(async () =>
        {
            if (_isNewProject)
            {
                await _projectService.CreateProjectAsync(Project);
            }
            else
            {
                await _projectService.UpdateProjectAsync(Project);
            }

            SaveCompleted?.Invoke(this, EventArgs.Empty);
        }, "Saving project...");
    }

    private void Cancel(object? parameter)
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    // Milestone methods
    private void AddMilestone(object? parameter)
    {
        AddMilestoneRequested?.Invoke(this, null);
    }

    private bool CanEditMilestone(ProjectMilestone? milestone)
    {
        return milestone != null;
    }

    private void EditMilestone(ProjectMilestone? milestone)
    {
        if (milestone != null)
        {
            EditMilestoneRequested?.Invoke(this, milestone);
        }
    }

    private bool CanDeleteMilestone(ProjectMilestone? milestone)
    {
        return milestone != null && milestone.Status != MilestoneStatus.Completed;
    }

    private async Task DeleteMilestoneAsync(ProjectMilestone? milestone)
    {
        if (milestone == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.DeleteMilestoneAsync(milestone.MilestoneId);
            if (result)
            {
                Milestones.Remove(milestone);
            }
        }, "Deleting milestone...");
    }

    private bool CanCompleteMilestone(ProjectMilestone? milestone)
    {
        return milestone != null && milestone.Status != MilestoneStatus.Completed;
    }

    private async Task CompleteMilestoneAsync(ProjectMilestone? milestone)
    {
        if (milestone == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.CompleteMilestoneAsync(milestone.MilestoneId);
            if (result)
            {
                milestone.Status = MilestoneStatus.Completed;
                milestone.ActualDate = DateTime.UtcNow;
                OnPropertyChanged(nameof(Milestones));
            }
        }, "Completing milestone...");
    }

    // Deliverable methods
    private void AddDeliverable(object? parameter)
    {
        AddDeliverableRequested?.Invoke(this, null);
    }

    private bool CanEditDeliverable(ProjectDeliverable? deliverable)
    {
        return deliverable != null;
    }

    private void EditDeliverable(ProjectDeliverable? deliverable)
    {
        if (deliverable != null)
        {
            EditDeliverableRequested?.Invoke(this, deliverable);
        }
    }

    private bool CanDeleteDeliverable(ProjectDeliverable? deliverable)
    {
        return deliverable != null && deliverable.Status != DeliverableStatus.Accepted;
    }

    private async Task DeleteDeliverableAsync(ProjectDeliverable? deliverable)
    {
        if (deliverable == null) return;

        await ExecuteAsync(async () =>
        {
            deliverable.IsActive = false;
            await _projectService.UpdateDeliverableAsync(deliverable);
            Deliverables.Remove(deliverable);
        }, "Deleting deliverable...");
    }

    private bool CanSubmitDeliverable(ProjectDeliverable? deliverable)
    {
        return deliverable != null &&
               deliverable.Status != DeliverableStatus.Submitted &&
               deliverable.Status != DeliverableStatus.Accepted;
    }

    private async Task SubmitDeliverableAsync(ProjectDeliverable? deliverable)
    {
        if (deliverable == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.SubmitDeliverableAsync(deliverable.DeliverableId);
            if (result)
            {
                deliverable.Status = DeliverableStatus.Submitted;
                deliverable.SubmissionDate = DateTime.UtcNow;
                OnPropertyChanged(nameof(Deliverables));
            }
        }, "Submitting deliverable...");
    }

    public async Task AddMilestoneToProjectAsync(ProjectMilestone milestone)
    {
        if (Project == null) return;

        milestone.ProjectId = Project.ProjectId;
        await ExecuteAsync(async () =>
        {
            var created = await _projectService.CreateMilestoneAsync(milestone);
            Milestones.Add(created);
        }, "Adding milestone...");
    }

    public async Task UpdateMilestoneAsync(ProjectMilestone milestone)
    {
        await ExecuteAsync(async () =>
        {
            await _projectService.UpdateMilestoneAsync(milestone);
            var index = Milestones.ToList().FindIndex(m => m.MilestoneId == milestone.MilestoneId);
            if (index >= 0)
            {
                Milestones[index] = milestone;
            }
        }, "Updating milestone...");
    }

    public async Task AddDeliverableToProjectAsync(ProjectDeliverable deliverable)
    {
        if (Project == null) return;

        deliverable.ProjectId = Project.ProjectId;
        await ExecuteAsync(async () =>
        {
            var created = await _projectService.CreateDeliverableAsync(deliverable);
            Deliverables.Add(created);
        }, "Adding deliverable...");
    }

    public async Task UpdateDeliverableAsync(ProjectDeliverable deliverable)
    {
        await ExecuteAsync(async () =>
        {
            await _projectService.UpdateDeliverableAsync(deliverable);
            var index = Deliverables.ToList().FindIndex(d => d.DeliverableId == deliverable.DeliverableId);
            if (index >= 0)
            {
                Deliverables[index] = deliverable;
            }
        }, "Updating deliverable...");
    }

    #endregion
}
