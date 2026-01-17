using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// ViewModel for the project planning view
/// </summary>
public class ProjectPlanningViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectAssignmentService _assignmentService;

    public ProjectPlanningViewModel(IProjectService projectService, IProjectAssignmentService assignmentService)
    {
        _projectService = projectService;
        _assignmentService = assignmentService;

        // Initialize collections
        Projects = new ObservableCollection<SurveyProject>();
        VesselAssignments = new ObservableCollection<ProjectVesselAssignment>();
        EquipmentAssignments = new ObservableCollection<ProjectEquipmentAssignment>();
        PersonnelAssignments = new ObservableCollection<ProjectPersonnelAssignment>();

        // Initialize commands
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        AddVesselCommand = new RelayCommand(_ => AddVessel(null), _ => CanAddResource());
        AddEquipmentCommand = new RelayCommand(_ => AddEquipment(null), _ => CanAddResource());
        AddPersonnelCommand = new RelayCommand(_ => AddPersonnel(null), _ => CanAddResource());
        EditVesselCommand = new RelayCommand<ProjectVesselAssignment>(EditVessel, CanEditVessel);
        EditEquipmentCommand = new RelayCommand<ProjectEquipmentAssignment>(EditEquipment, CanEditEquipment);
        EditPersonnelCommand = new RelayCommand<ProjectPersonnelAssignment>(EditPersonnel, CanEditPersonnel);
        RemoveVesselCommand = new AsyncRelayCommand<ProjectVesselAssignment>(RemoveVesselAsync, CanRemoveVessel);
        RemoveEquipmentCommand = new AsyncRelayCommand<ProjectEquipmentAssignment>(RemoveEquipmentAsync, CanRemoveEquipment);
        RemovePersonnelCommand = new AsyncRelayCommand<ProjectPersonnelAssignment>(RemovePersonnelAsync, CanRemovePersonnel);
    }

    #region Properties

    public ObservableCollection<SurveyProject> Projects { get; }
    public ObservableCollection<ProjectVesselAssignment> VesselAssignments { get; }
    public ObservableCollection<ProjectEquipmentAssignment> EquipmentAssignments { get; }
    public ObservableCollection<ProjectPersonnelAssignment> PersonnelAssignments { get; }

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

    #endregion

    #region Commands

    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddVesselCommand { get; }
    public ICommand AddEquipmentCommand { get; }
    public ICommand AddPersonnelCommand { get; }
    public ICommand EditVesselCommand { get; }
    public ICommand EditEquipmentCommand { get; }
    public ICommand EditPersonnelCommand { get; }
    public ICommand RemoveVesselCommand { get; }
    public ICommand RemoveEquipmentCommand { get; }
    public ICommand RemovePersonnelCommand { get; }

    #endregion

    #region Events

    public event EventHandler? AddVesselRequested;
    public event EventHandler? AddEquipmentRequested;
    public event EventHandler? AddPersonnelRequested;
    public event EventHandler<ProjectVesselAssignment>? EditVesselRequested;
    public event EventHandler<ProjectEquipmentAssignment>? EditEquipmentRequested;
    public event EventHandler<ProjectPersonnelAssignment>? EditPersonnelRequested;

    #endregion

    #region Methods

    public async Task LoadAsync()
    {
        await ExecuteAsync(async () =>
        {
            var projects = await _projectService.GetActiveProjectsAsync();
            Projects.Clear();
            foreach (var project in projects)
            {
                Projects.Add(project);
            }

            if (Projects.Any())
            {
                SelectedProject = Projects.First();
            }
        }, "Loading projects...");
    }

    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAssignmentsAsync()
    {
        if (SelectedProject == null)
        {
            VesselAssignments.Clear();
            EquipmentAssignments.Clear();
            PersonnelAssignments.Clear();
            return;
        }

        await ExecuteAsync(async () =>
        {
            var vessels = await _assignmentService.GetProjectVesselAssignmentsAsync(SelectedProject.ProjectId);
            VesselAssignments.Clear();
            foreach (var v in vessels)
            {
                VesselAssignments.Add(v);
            }

            var equipment = await _assignmentService.GetProjectEquipmentAssignmentsAsync(SelectedProject.ProjectId);
            EquipmentAssignments.Clear();
            foreach (var e in equipment)
            {
                EquipmentAssignments.Add(e);
            }

            var personnel = await _assignmentService.GetProjectPersonnelAssignmentsAsync(SelectedProject.ProjectId);
            PersonnelAssignments.Clear();
            foreach (var p in personnel)
            {
                PersonnelAssignments.Add(p);
            }
        }, "Loading assignments...");
    }

    private bool CanAddResource()
    {
        return SelectedProject != null;
    }

    private void AddVessel(object? parameter)
    {
        AddVesselRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AddEquipment(object? parameter)
    {
        AddEquipmentRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AddPersonnel(object? parameter)
    {
        AddPersonnelRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanEditVessel(ProjectVesselAssignment? assignment) => assignment != null;
    private bool CanEditEquipment(ProjectEquipmentAssignment? assignment) => assignment != null;
    private bool CanEditPersonnel(ProjectPersonnelAssignment? assignment) => assignment != null;

    private void EditVessel(ProjectVesselAssignment? assignment)
    {
        if (assignment != null)
            EditVesselRequested?.Invoke(this, assignment);
    }

    private void EditEquipment(ProjectEquipmentAssignment? assignment)
    {
        if (assignment != null)
            EditEquipmentRequested?.Invoke(this, assignment);
    }

    private void EditPersonnel(ProjectPersonnelAssignment? assignment)
    {
        if (assignment != null)
            EditPersonnelRequested?.Invoke(this, assignment);
    }

    private bool CanRemoveVessel(ProjectVesselAssignment? a) => a != null && a.Status != AssignmentStatus.Completed;
    private bool CanRemoveEquipment(ProjectEquipmentAssignment? a) => a != null && a.Status != AssignmentStatus.Completed;
    private bool CanRemovePersonnel(ProjectPersonnelAssignment? a) => a != null && a.Status != AssignmentStatus.Completed;

    private async Task RemoveVesselAsync(ProjectVesselAssignment? assignment)
    {
        if (assignment == null) return;
        await ExecuteAsync(async () =>
        {
            if (await _assignmentService.RemoveVesselAssignmentAsync(assignment.AssignmentId))
                VesselAssignments.Remove(assignment);
        }, "Removing vessel...");
    }

    private async Task RemoveEquipmentAsync(ProjectEquipmentAssignment? assignment)
    {
        if (assignment == null) return;
        await ExecuteAsync(async () =>
        {
            if (await _assignmentService.RemoveEquipmentAssignmentAsync(assignment.AssignmentId))
                EquipmentAssignments.Remove(assignment);
        }, "Removing equipment...");
    }

    private async Task RemovePersonnelAsync(ProjectPersonnelAssignment? assignment)
    {
        if (assignment == null) return;
        await ExecuteAsync(async () =>
        {
            if (await _assignmentService.RemovePersonnelAssignmentAsync(assignment.AssignmentId))
                PersonnelAssignments.Remove(assignment);
        }, "Removing personnel...");
    }

    #endregion
}
