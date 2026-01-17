using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// ViewModel for the project list view with search, filter, and status management
/// </summary>
public class ProjectListViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;

    public ProjectListViewModel(IProjectService projectService)
    {
        _projectService = projectService;

        Projects = new ObservableCollection<SurveyProject>();
        FilteredProjects = new ObservableCollection<SurveyProject>();
        Clients = new ObservableCollection<Client>();
        StatusOptions = new ObservableCollection<ProjectStatus?>();

        // Initialize commands
        LoadProjectsCommand = new AsyncRelayCommand(LoadProjectsAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        AddProjectCommand = new RelayCommand(AddProject);
        EditProjectCommand = new RelayCommand<SurveyProject>(EditProject, CanEditProject);
        ViewProjectCommand = new RelayCommand<SurveyProject>(ViewProject, CanViewProject);
        DeleteProjectCommand = new AsyncRelayCommand<SurveyProject>(DeleteProjectAsync, CanDeleteProject);
        UpdateStatusCommand = new AsyncRelayCommand<ProjectStatus>(async s => await UpdateStatusAsync(s), s => CanUpdateStatus(s));
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        // Soft-delete commands
        RestoreProjectCommand = new AsyncRelayCommand<SurveyProject>(RestoreProjectAsync, CanRestoreProject);
        PermanentlyDeleteProjectCommand = new AsyncRelayCommand<SurveyProject>(PermanentlyDeleteProjectAsync, CanPermanentlyDeleteProject);
        LoadDeletedProjectsCommand = new AsyncRelayCommand(LoadDeletedProjectsAsync);
        ToggleDeletedViewCommand = new RelayCommand(ToggleDeletedView);

        // Initialize status options
        StatusOptions.Add(null); // All statuses
        foreach (ProjectStatus status in Enum.GetValues(typeof(ProjectStatus)))
        {
            StatusOptions.Add(status);
        }
    }

    #region Properties

    private ObservableCollection<SurveyProject> _projects = null!;
    public ObservableCollection<SurveyProject> Projects
    {
        get => _projects;
        set => SetProperty(ref _projects, value);
    }

    private ObservableCollection<SurveyProject> _filteredProjects = null!;
    public ObservableCollection<SurveyProject> FilteredProjects
    {
        get => _filteredProjects;
        set => SetProperty(ref _filteredProjects, value);
    }

    private ObservableCollection<Client> _clients = null!;
    public ObservableCollection<Client> Clients
    {
        get => _clients;
        set => SetProperty(ref _clients, value);
    }

    private ObservableCollection<ProjectStatus?> _statusOptions = null!;
    public ObservableCollection<ProjectStatus?> StatusOptions
    {
        get => _statusOptions;
        set => SetProperty(ref _statusOptions, value);
    }

    private SurveyProject? _selectedProject;
    public SurveyProject? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                OnPropertyChanged(nameof(HasSelectedProject));
                SelectedProjectChanged?.Invoke(this, value);
            }
        }
    }

    public bool HasSelectedProject => SelectedProject != null;

    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilters();
            }
        }
    }

    private ProjectStatus? _selectedStatus;
    public ProjectStatus? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                ApplyFilters();
            }
        }
    }

    private Client? _selectedClient;
    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                ApplyFilters();
            }
        }
    }

    private int _totalProjectCount;
    public int TotalProjectCount
    {
        get => _totalProjectCount;
        set => SetProperty(ref _totalProjectCount, value);
    }

    private int _activeProjectCount;
    public int ActiveProjectCount
    {
        get => _activeProjectCount;
        set => SetProperty(ref _activeProjectCount, value);
    }

    private int _filteredProjectCount;
    public int FilteredProjectCount
    {
        get => _filteredProjectCount;
        set => SetProperty(ref _filteredProjectCount, value);
    }

    private int _deletedProjectCount;
    public int DeletedProjectCount
    {
        get => _deletedProjectCount;
        set => SetProperty(ref _deletedProjectCount, value);
    }

    private bool _showDeletedProjects;
    public bool ShowDeletedProjects
    {
        get => _showDeletedProjects;
        set
        {
            if (SetProperty(ref _showDeletedProjects, value))
            {
                OnPropertyChanged(nameof(ViewModeLabel));
                _ = RefreshAsync();
            }
        }
    }

    public string ViewModeLabel => ShowDeletedProjects ? "Viewing Deleted Projects" : "Viewing Active Projects";

    #endregion

    #region Commands

    public ICommand LoadProjectsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddProjectCommand { get; }
    public ICommand EditProjectCommand { get; }
    public ICommand ViewProjectCommand { get; }
    public ICommand DeleteProjectCommand { get; }
    public ICommand UpdateStatusCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    // Soft-delete commands
    public ICommand RestoreProjectCommand { get; }
    public ICommand PermanentlyDeleteProjectCommand { get; }
    public ICommand LoadDeletedProjectsCommand { get; }
    public ICommand ToggleDeletedViewCommand { get; }

    #endregion

    #region Events

    public event EventHandler<SurveyProject?>? SelectedProjectChanged;
    public event EventHandler<SurveyProject?>? AddProjectRequested;
    public event EventHandler<SurveyProject>? EditProjectRequested;
    public event EventHandler<SurveyProject>? ViewProjectRequested;
    public event EventHandler<SurveyProject>? ProjectDeleted;
    public event EventHandler<SurveyProject>? ProjectRestored;
    public event EventHandler<SurveyProject>? ProjectPermanentlyDeleted;

    #endregion

    #region Methods

    public async Task LoadProjectsAsync()
    {
        await ExecuteAsync(async () =>
        {
            IEnumerable<SurveyProject> projects;

            if (ShowDeletedProjects)
            {
                projects = await _projectService.GetDeletedProjectsAsync();
            }
            else
            {
                projects = await _projectService.GetAllProjectsAsync();
            }

            Projects.Clear();
            foreach (var project in projects)
            {
                Projects.Add(project);
            }

            // Load clients for filter
            var clients = await _projectService.GetAllClientsAsync();
            Clients.Clear();
            Clients.Add(null!); // All clients option
            foreach (var client in clients)
            {
                Clients.Add(client);
            }

            // Update counts
            TotalProjectCount = await _projectService.GetTotalProjectCountAsync();
            ActiveProjectCount = await _projectService.GetActiveProjectCountAsync();
            DeletedProjectCount = await _projectService.GetDeletedProjectCountAsync();

            ApplyFilters();
        }, ShowDeletedProjects ? "Loading deleted projects..." : "Loading projects...");
    }

    private async Task RefreshAsync()
    {
        await LoadProjectsAsync();
    }

    private void ApplyFilters()
    {
        var filtered = Projects.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(p =>
                p.ProjectName.ToLower().Contains(search) ||
                p.ProjectNumber.ToLower().Contains(search) ||
                (p.Description?.ToLower().Contains(search) ?? false) ||
                (p.Client?.CompanyName.ToLower().Contains(search) ?? false) ||
                (p.LocationName?.ToLower().Contains(search) ?? false));
        }

        // Apply status filter
        if (SelectedStatus.HasValue)
        {
            filtered = filtered.Where(p => p.Status == SelectedStatus.Value);
        }

        // Apply client filter
        if (SelectedClient != null)
        {
            filtered = filtered.Where(p => p.ClientId == SelectedClient.ClientId);
        }

        FilteredProjects.Clear();
        foreach (var project in filtered)
        {
            FilteredProjects.Add(project);
        }

        FilteredProjectCount = FilteredProjects.Count;
    }

    private void ClearFilters()
    {
        SearchText = null;
        SelectedStatus = null;
        SelectedClient = null;
    }

    private void AddProject(object? parameter)
    {
        AddProjectRequested?.Invoke(this, null);
    }

    private bool CanEditProject(SurveyProject? project)
    {
        return project != null;
    }

    private void EditProject(SurveyProject? project)
    {
        if (project != null)
        {
            EditProjectRequested?.Invoke(this, project);
        }
    }

    private bool CanViewProject(SurveyProject? project)
    {
        return project != null;
    }

    private void ViewProject(SurveyProject? project)
    {
        if (project != null)
        {
            ViewProjectRequested?.Invoke(this, project);
        }
    }

    private bool CanDeleteProject(SurveyProject? project)
    {
        return project != null && project.Status != ProjectStatus.Active;
    }

    private async Task DeleteProjectAsync(SurveyProject? project)
    {
        if (project == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.DeleteProjectAsync(project.ProjectId);
            if (result)
            {
                Projects.Remove(project);
                FilteredProjects.Remove(project);
                TotalProjectCount--;
                DeletedProjectCount++;
                ProjectDeleted?.Invoke(this, project);
            }
        }, "Deleting project...");
    }

    private bool CanUpdateStatus(ProjectStatus? status)
    {
        return SelectedProject != null && status.HasValue;
    }

    private async Task UpdateStatusAsync(ProjectStatus? newStatus)
    {
        if (SelectedProject == null || !newStatus.HasValue) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.UpdateProjectStatusAsync(SelectedProject.ProjectId, newStatus.Value);
            if (result)
            {
                SelectedProject.Status = newStatus.Value;
                OnPropertyChanged(nameof(SelectedProject));
                ApplyFilters();
            }
        }, "Updating status...");
    }

    public async Task SearchAsync(string searchTerm)
    {
        await ExecuteAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                ApplyFilters();
                return;
            }

            var results = await _projectService.SearchProjectsAsync(searchTerm);
            FilteredProjects.Clear();
            foreach (var project in results)
            {
                FilteredProjects.Add(project);
            }
            FilteredProjectCount = FilteredProjects.Count;
        }, "Searching...");
    }

    #region Soft-Delete Operations

    private bool CanRestoreProject(SurveyProject? project)
    {
        return project != null && project.IsDeleted;
    }

    private async Task RestoreProjectAsync(SurveyProject? project)
    {
        if (project == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.RestoreProjectAsync(project.ProjectId);
            if (result)
            {
                Projects.Remove(project);
                FilteredProjects.Remove(project);
                DeletedProjectCount--;
                TotalProjectCount++;
                ProjectRestored?.Invoke(this, project);
            }
        }, "Restoring project...");
    }

    private bool CanPermanentlyDeleteProject(SurveyProject? project)
    {
        return project != null && project.IsDeleted;
    }

    private async Task PermanentlyDeleteProjectAsync(SurveyProject? project)
    {
        if (project == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.PermanentlyDeleteProjectAsync(project.ProjectId);
            if (result)
            {
                Projects.Remove(project);
                FilteredProjects.Remove(project);
                DeletedProjectCount--;
                ProjectPermanentlyDeleted?.Invoke(this, project);
            }
        }, "Permanently deleting project...");
    }

    private async Task LoadDeletedProjectsAsync()
    {
        ShowDeletedProjects = true;
        await LoadProjectsAsync();
    }

    private void ToggleDeletedView(object? parameter)
    {
        ShowDeletedProjects = !ShowDeletedProjects;
    }

    #endregion

    #endregion
}
