using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// ViewModel for resource assignment management with availability checking
/// </summary>
public class ResourceAssignmentViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectAssignmentService _assignmentService;

    public ResourceAssignmentViewModel(IProjectService projectService, IProjectAssignmentService assignmentService)
    {
        _projectService = projectService;
        _assignmentService = assignmentService;

        // Initialize collections
        Projects = new ObservableCollection<SurveyProject>();
        VesselAssignments = new ObservableCollection<ProjectVesselAssignment>();
        EquipmentAssignments = new ObservableCollection<ProjectEquipmentAssignment>();
        PersonnelAssignments = new ObservableCollection<ProjectPersonnelAssignment>();
        VesselRoles = new ObservableCollection<VesselRole>();
        EquipmentRoles = new ObservableCollection<EquipmentRole>();
        PersonnelRoles = new ObservableCollection<PersonnelRole>();
        AssignmentStatuses = new ObservableCollection<AssignmentStatus>();

        // Initialize enum collections
        foreach (VesselRole role in Enum.GetValues(typeof(VesselRole)))
            VesselRoles.Add(role);
        foreach (EquipmentRole role in Enum.GetValues(typeof(EquipmentRole)))
            EquipmentRoles.Add(role);
        foreach (PersonnelRole role in Enum.GetValues(typeof(PersonnelRole)))
            PersonnelRoles.Add(role);
        foreach (AssignmentStatus status in Enum.GetValues(typeof(AssignmentStatus)))
            AssignmentStatuses.Add(status);

        // Initialize commands
        LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);
        LoadAssignmentsCommand = new AsyncRelayCommand(LoadAssignmentsAsync);
        AddVesselAssignmentCommand = new RelayCommand(AddVesselAssignment);
        AddEquipmentAssignmentCommand = new RelayCommand(AddEquipmentAssignment);
        AddPersonnelAssignmentCommand = new RelayCommand(AddPersonnelAssignment);
        EditVesselAssignmentCommand = new RelayCommand<ProjectVesselAssignment>(EditVesselAssignment, CanEditAssignment);
        EditEquipmentAssignmentCommand = new RelayCommand<ProjectEquipmentAssignment>(EditEquipmentAssignment, CanEditEquipmentAssignment);
        EditPersonnelAssignmentCommand = new RelayCommand<ProjectPersonnelAssignment>(EditPersonnelAssignment, CanEditPersonnelAssignment);
        RemoveVesselAssignmentCommand = new AsyncRelayCommand<ProjectVesselAssignment>(RemoveVesselAssignmentAsync, CanRemoveAssignment);
        RemoveEquipmentAssignmentCommand = new AsyncRelayCommand<ProjectEquipmentAssignment>(RemoveEquipmentAssignmentAsync, CanRemoveEquipmentAssignment);
        RemovePersonnelAssignmentCommand = new AsyncRelayCommand<ProjectPersonnelAssignment>(RemovePersonnelAssignmentAsync, CanRemovePersonnelAssignment);
        CheckAvailabilityCommand = new AsyncRelayCommand(CheckAvailabilityAsync);
    }

    #region Properties

    public ObservableCollection<SurveyProject> Projects { get; }
    public ObservableCollection<ProjectVesselAssignment> VesselAssignments { get; }
    public ObservableCollection<ProjectEquipmentAssignment> EquipmentAssignments { get; }
    public ObservableCollection<ProjectPersonnelAssignment> PersonnelAssignments { get; }
    public ObservableCollection<VesselRole> VesselRoles { get; }
    public ObservableCollection<EquipmentRole> EquipmentRoles { get; }
    public ObservableCollection<PersonnelRole> PersonnelRoles { get; }
    public ObservableCollection<AssignmentStatus> AssignmentStatuses { get; }

    private SurveyProject? _selectedProject;
    public SurveyProject? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                OnPropertyChanged(nameof(HasSelectedProject));
                _ = LoadAssignmentsAsync();
            }
        }
    }

    public bool HasSelectedProject => SelectedProject != null;

    private ProjectVesselAssignment? _selectedVesselAssignment;
    public ProjectVesselAssignment? SelectedVesselAssignment
    {
        get => _selectedVesselAssignment;
        set => SetProperty(ref _selectedVesselAssignment, value);
    }

    private ProjectEquipmentAssignment? _selectedEquipmentAssignment;
    public ProjectEquipmentAssignment? SelectedEquipmentAssignment
    {
        get => _selectedEquipmentAssignment;
        set => SetProperty(ref _selectedEquipmentAssignment, value);
    }

    private ProjectPersonnelAssignment? _selectedPersonnelAssignment;
    public ProjectPersonnelAssignment? SelectedPersonnelAssignment
    {
        get => _selectedPersonnelAssignment;
        set => SetProperty(ref _selectedPersonnelAssignment, value);
    }

    private DateTime? _checkStartDate;
    public DateTime? CheckStartDate
    {
        get => _checkStartDate;
        set => SetProperty(ref _checkStartDate, value);
    }

    private DateTime? _checkEndDate;
    public DateTime? CheckEndDate
    {
        get => _checkEndDate;
        set => SetProperty(ref _checkEndDate, value);
    }

    private string? _availabilityResult;
    public string? AvailabilityResult
    {
        get => _availabilityResult;
        set => SetProperty(ref _availabilityResult, value);
    }

    #endregion

    #region Commands

    public ICommand LoadProjectsCommand { get; }
    public ICommand LoadAssignmentsCommand { get; }
    public ICommand AddVesselAssignmentCommand { get; }
    public ICommand AddEquipmentAssignmentCommand { get; }
    public ICommand AddPersonnelAssignmentCommand { get; }
    public ICommand EditVesselAssignmentCommand { get; }
    public ICommand EditEquipmentAssignmentCommand { get; }
    public ICommand EditPersonnelAssignmentCommand { get; }
    public ICommand RemoveVesselAssignmentCommand { get; }
    public ICommand RemoveEquipmentAssignmentCommand { get; }
    public ICommand RemovePersonnelAssignmentCommand { get; }
    public ICommand CheckAvailabilityCommand { get; }

    #endregion

    #region Events

    public event EventHandler? AddVesselAssignmentRequested;
    public event EventHandler? AddEquipmentAssignmentRequested;
    public event EventHandler? AddPersonnelAssignmentRequested;
    public event EventHandler<ProjectVesselAssignment>? EditVesselAssignmentRequested;
    public event EventHandler<ProjectEquipmentAssignment>? EditEquipmentAssignmentRequested;
    public event EventHandler<ProjectPersonnelAssignment>? EditPersonnelAssignmentRequested;

    #endregion

    #region Methods

    public async Task LoadProjectsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var projects = await _projectService.GetActiveProjectsAsync();
            Projects.Clear();
            foreach (var project in projects)
            {
                Projects.Add(project);
            }

            if (Projects.Any() && SelectedProject == null)
            {
                SelectedProject = Projects.First();
            }
        }, "Loading projects...");
    }

    public async Task LoadAssignmentsAsync()
    {
        if (SelectedProject == null) return;

        await ExecuteAsync(async () =>
        {
            // Load vessel assignments
            var vessels = await _assignmentService.GetProjectVesselAssignmentsAsync(SelectedProject.ProjectId);
            VesselAssignments.Clear();
            foreach (var vessel in vessels)
            {
                VesselAssignments.Add(vessel);
            }

            // Load equipment assignments
            var equipment = await _assignmentService.GetProjectEquipmentAssignmentsAsync(SelectedProject.ProjectId);
            EquipmentAssignments.Clear();
            foreach (var equip in equipment)
            {
                EquipmentAssignments.Add(equip);
            }

            // Load personnel assignments
            var personnel = await _assignmentService.GetProjectPersonnelAssignmentsAsync(SelectedProject.ProjectId);
            PersonnelAssignments.Clear();
            foreach (var person in personnel)
            {
                PersonnelAssignments.Add(person);
            }
        }, "Loading assignments...");
    }

    private void AddVesselAssignment(object? parameter)
    {
        AddVesselAssignmentRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AddEquipmentAssignment(object? parameter)
    {
        AddEquipmentAssignmentRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AddPersonnelAssignment(object? parameter)
    {
        AddPersonnelAssignmentRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanEditAssignment(ProjectVesselAssignment? assignment)
    {
        return assignment != null;
    }

    private bool CanEditEquipmentAssignment(ProjectEquipmentAssignment? assignment)
    {
        return assignment != null;
    }

    private bool CanEditPersonnelAssignment(ProjectPersonnelAssignment? assignment)
    {
        return assignment != null;
    }

    private void EditVesselAssignment(ProjectVesselAssignment? assignment)
    {
        if (assignment != null)
        {
            EditVesselAssignmentRequested?.Invoke(this, assignment);
        }
    }

    private void EditEquipmentAssignment(ProjectEquipmentAssignment? assignment)
    {
        if (assignment != null)
        {
            EditEquipmentAssignmentRequested?.Invoke(this, assignment);
        }
    }

    private void EditPersonnelAssignment(ProjectPersonnelAssignment? assignment)
    {
        if (assignment != null)
        {
            EditPersonnelAssignmentRequested?.Invoke(this, assignment);
        }
    }

    private bool CanRemoveAssignment(ProjectVesselAssignment? assignment)
    {
        return assignment != null && assignment.Status != AssignmentStatus.Completed;
    }

    private bool CanRemoveEquipmentAssignment(ProjectEquipmentAssignment? assignment)
    {
        return assignment != null && assignment.Status != AssignmentStatus.Completed;
    }

    private bool CanRemovePersonnelAssignment(ProjectPersonnelAssignment? assignment)
    {
        return assignment != null && assignment.Status != AssignmentStatus.Completed;
    }

    private async Task RemoveVesselAssignmentAsync(ProjectVesselAssignment? assignment)
    {
        if (assignment == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _assignmentService.RemoveVesselAssignmentAsync(assignment.AssignmentId);
            if (result)
            {
                VesselAssignments.Remove(assignment);
            }
        }, "Removing vessel assignment...");
    }

    private async Task RemoveEquipmentAssignmentAsync(ProjectEquipmentAssignment? assignment)
    {
        if (assignment == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _assignmentService.RemoveEquipmentAssignmentAsync(assignment.AssignmentId);
            if (result)
            {
                EquipmentAssignments.Remove(assignment);
            }
        }, "Removing equipment assignment...");
    }

    private async Task RemovePersonnelAssignmentAsync(ProjectPersonnelAssignment? assignment)
    {
        if (assignment == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _assignmentService.RemovePersonnelAssignmentAsync(assignment.AssignmentId);
            if (result)
            {
                PersonnelAssignments.Remove(assignment);
            }
        }, "Removing personnel assignment...");
    }

    private async Task CheckAvailabilityAsync()
    {
        if (!CheckStartDate.HasValue || !CheckEndDate.HasValue)
        {
            AvailabilityResult = "Please select start and end dates.";
            return;
        }

        await ExecuteAsync(async () =>
        {
            // Check vessel availability example
            if (SelectedVesselAssignment != null)
            {
                var available = await _assignmentService.IsVesselAvailableAsync(
                    SelectedVesselAssignment.VesselId,
                    CheckStartDate.Value,
                    CheckEndDate.Value,
                    SelectedProject?.ProjectId);

                AvailabilityResult = available
                    ? "Vessel is AVAILABLE for the selected dates."
                    : "Vessel is NOT AVAILABLE for the selected dates.";
            }
            else
            {
                AvailabilityResult = "Select a vessel assignment to check availability.";
            }
        }, "Checking availability...");
    }

    public async Task AssignVesselAsync(ProjectVesselAssignment assignment)
    {
        if (SelectedProject == null) return;

        assignment.ProjectId = SelectedProject.ProjectId;
        await ExecuteAsync(async () =>
        {
            var created = await _assignmentService.AssignVesselAsync(assignment);
            VesselAssignments.Add(created);
        }, "Assigning vessel...");
    }

    public async Task AssignEquipmentAsync(ProjectEquipmentAssignment assignment)
    {
        if (SelectedProject == null) return;

        assignment.ProjectId = SelectedProject.ProjectId;
        await ExecuteAsync(async () =>
        {
            var created = await _assignmentService.AssignEquipmentAsync(assignment);
            EquipmentAssignments.Add(created);
        }, "Assigning equipment...");
    }

    public async Task AssignPersonnelAsync(ProjectPersonnelAssignment assignment)
    {
        if (SelectedProject == null) return;

        assignment.ProjectId = SelectedProject.ProjectId;
        await ExecuteAsync(async () =>
        {
            var created = await _assignmentService.AssignPersonnelAsync(assignment);
            PersonnelAssignments.Add(created);
        }, "Assigning personnel...");
    }

    #endregion
}
