using System.Windows.Input;
using FathomOS.Modules.ProjectManagement.Models;
using FathomOS.Modules.ProjectManagement.Services;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// Main ViewModel for the Project Management module window
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly ProjectDatabaseService _dbService;

    public MainViewModel(ProjectDatabaseService dbService)
    {
        _dbService = dbService;

        // Initialize child ViewModels
        ProjectListViewModel = new ProjectListViewModel(_dbService.GetProjectService());
        DashboardViewModel = new DashboardViewModel(_dbService.GetProjectService());
        ClientListViewModel = new ClientListViewModel(_dbService.GetProjectService());
        PlanningViewModel = new ProjectPlanningViewModel(
            _dbService.GetProjectService(),
            _dbService.GetAssignmentService());

        // Wire up project list events
        ProjectListViewModel.AddProjectRequested += OnAddProjectRequested;
        ProjectListViewModel.EditProjectRequested += OnEditProjectRequested;
        ProjectListViewModel.ViewProjectRequested += OnViewProjectRequested;
        ProjectListViewModel.SelectedProjectChanged += OnSelectedProjectChanged;

        // Wire up dashboard events
        DashboardViewModel.ViewProjectRequested += (s, p) => OnEditProjectRequested(s, p);

        // Wire up client events
        ClientListViewModel.AddClientRequested += OnAddClientRequested;
        ClientListViewModel.EditClientRequested += OnEditClientRequested;

        // Initialize commands
        NavigateToDashboardCommand = new RelayCommand(_ => NavigateTo("Dashboard"));
        NavigateToProjectsCommand = new RelayCommand(_ => NavigateTo("Projects"));
        NavigateToClientsCommand = new RelayCommand(_ => NavigateTo("Clients"));
        NavigateToPlanningCommand = new RelayCommand(_ => NavigateTo("Planning"));
        NavigateToReportsCommand = new RelayCommand(_ => NavigateTo("Reports"));
        GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);

        // Set default view
        CurrentView = "Projects";
    }

    #region Properties

    public ProjectListViewModel ProjectListViewModel { get; }
    public DashboardViewModel DashboardViewModel { get; }
    public ClientListViewModel ClientListViewModel { get; }
    public ProjectPlanningViewModel PlanningViewModel { get; }

    private ProjectDetailViewModel? _projectDetailViewModel;
    public ProjectDetailViewModel? ProjectDetailViewModel
    {
        get => _projectDetailViewModel;
        set => SetProperty(ref _projectDetailViewModel, value);
    }

    private string _currentView = "Projects";
    public string CurrentView
    {
        get => _currentView;
        set
        {
            if (SetProperty(ref _currentView, value))
            {
                OnPropertyChanged(nameof(IsDashboardView));
                OnPropertyChanged(nameof(IsProjectsView));
                OnPropertyChanged(nameof(IsClientsView));
                OnPropertyChanged(nameof(IsPlanningView));
                OnPropertyChanged(nameof(IsReportsView));

                // Load data for the view
                _ = LoadCurrentViewDataAsync();
            }
        }
    }

    public bool IsDashboardView => CurrentView == "Dashboard";
    public bool IsProjectsView => CurrentView == "Projects";
    public bool IsClientsView => CurrentView == "Clients";
    public bool IsPlanningView => CurrentView == "Planning";
    public bool IsReportsView => CurrentView == "Reports";

    private SurveyProject? _selectedProject;
    public SurveyProject? SelectedProject
    {
        get => _selectedProject;
        set => SetProperty(ref _selectedProject, value);
    }

    private bool _isDetailViewOpen;
    public bool IsDetailViewOpen
    {
        get => _isDetailViewOpen;
        set => SetProperty(ref _isDetailViewOpen, value);
    }

    #endregion

    #region Commands

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToProjectsCommand { get; }
    public ICommand NavigateToClientsCommand { get; }
    public ICommand NavigateToPlanningCommand { get; }
    public ICommand NavigateToReportsCommand { get; }
    public ICommand GenerateReportCommand { get; }
    public ICommand ExportCommand { get; }

    #endregion

    #region Events

    public event EventHandler<SurveyProject?>? OpenProjectDetail;
    public event EventHandler? CloseProjectDetail;
    public event EventHandler<Client?>? OpenClientDetail;
    public event EventHandler? CloseClientDetail;

    #endregion

    #region Methods

    public async Task InitializeAsync()
    {
        await _dbService.Context.Database.EnsureCreatedAsync();
        await ProjectListViewModel.LoadProjectsAsync();
    }

    private async Task LoadCurrentViewDataAsync()
    {
        switch (CurrentView)
        {
            case "Dashboard":
                await DashboardViewModel.LoadAsync();
                break;
            case "Projects":
                await ProjectListViewModel.LoadProjectsAsync();
                break;
            case "Clients":
                await ClientListViewModel.LoadAsync();
                break;
            case "Planning":
                await PlanningViewModel.LoadAsync();
                break;
        }
    }

    private void NavigateTo(string view)
    {
        CurrentView = view;
    }

    private void OnAddProjectRequested(object? sender, SurveyProject? project)
    {
        ProjectDetailViewModel = new ProjectDetailViewModel(
            _dbService.GetProjectService(),
            _dbService.GetAssignmentService(),
            null);

        ProjectDetailViewModel.SaveCompleted += OnProjectSaveCompleted;
        ProjectDetailViewModel.CancelRequested += OnProjectCancelRequested;

        IsDetailViewOpen = true;
        OpenProjectDetail?.Invoke(this, null);
    }

    private void OnEditProjectRequested(object? sender, SurveyProject project)
    {
        ProjectDetailViewModel = new ProjectDetailViewModel(
            _dbService.GetProjectService(),
            _dbService.GetAssignmentService(),
            project.ProjectId);

        ProjectDetailViewModel.SaveCompleted += OnProjectSaveCompleted;
        ProjectDetailViewModel.CancelRequested += OnProjectCancelRequested;

        IsDetailViewOpen = true;
        OpenProjectDetail?.Invoke(this, project);
    }

    private void OnViewProjectRequested(object? sender, SurveyProject project)
    {
        OnEditProjectRequested(sender, project);
    }

    private void OnSelectedProjectChanged(object? sender, SurveyProject? project)
    {
        SelectedProject = project;
    }

    private async void OnProjectSaveCompleted(object? sender, EventArgs e)
    {
        IsDetailViewOpen = false;
        ProjectDetailViewModel = null;
        CloseProjectDetail?.Invoke(this, EventArgs.Empty);
        await ProjectListViewModel.LoadProjectsAsync();

        // Refresh dashboard if visible
        if (IsDashboardView)
        {
            await DashboardViewModel.LoadAsync();
        }
    }

    private void OnProjectCancelRequested(object? sender, EventArgs e)
    {
        IsDetailViewOpen = false;
        ProjectDetailViewModel = null;
        CloseProjectDetail?.Invoke(this, EventArgs.Empty);
    }

    private void OnAddClientRequested(object? sender, EventArgs e)
    {
        OpenClientDetail?.Invoke(this, null);
    }

    private void OnEditClientRequested(object? sender, Client client)
    {
        OpenClientDetail?.Invoke(this, client);
    }

    private async Task GenerateReportAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Report generation logic
            await Task.Delay(100);
        }, "Generating report...");
    }

    private async Task ExportAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Export logic
            await Task.Delay(100);
        }, "Exporting...");
    }

    #endregion
}
