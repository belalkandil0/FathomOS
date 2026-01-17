using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// Main ViewModel for the Personnel Management module
/// Coordinates navigation between views and manages shared state
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly PersonnelDatabaseService _dbService;

    #region Constructor

    public MainViewModel(PersonnelDatabaseService dbService)
    {
        _dbService = dbService;

        // Initialize child ViewModels
        PersonnelListViewModel = new PersonnelListViewModel(_dbService);
        TimesheetListViewModel = new TimesheetListViewModel(_dbService);
        CertificationViewModel = new CertificationViewModel(_dbService);
        VesselAssignmentViewModel = new VesselAssignmentViewModel(_dbService);

        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        ImportCommand = new RelayCommand(Import);
        ExportCommand = new RelayCommand(Export);
        GenerateReportCommand = new RelayCommand(GenerateReport);
    }

    #endregion

    #region Child ViewModels

    private PersonnelListViewModel _personnelListViewModel = null!;
    public PersonnelListViewModel PersonnelListViewModel
    {
        get => _personnelListViewModel;
        set => SetProperty(ref _personnelListViewModel, value);
    }

    private TimesheetListViewModel _timesheetListViewModel = null!;
    public TimesheetListViewModel TimesheetListViewModel
    {
        get => _timesheetListViewModel;
        set => SetProperty(ref _timesheetListViewModel, value);
    }

    private CertificationViewModel _certificationViewModel = null!;
    public CertificationViewModel CertificationViewModel
    {
        get => _certificationViewModel;
        set => SetProperty(ref _certificationViewModel, value);
    }

    private VesselAssignmentViewModel _vesselAssignmentViewModel = null!;
    public VesselAssignmentViewModel VesselAssignmentViewModel
    {
        get => _vesselAssignmentViewModel;
        set => SetProperty(ref _vesselAssignmentViewModel, value);
    }

    #endregion

    #region Properties

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    #endregion

    #region Commands

    public ICommand LoadDataCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand GenerateReportCommand { get; }

    #endregion

    #region Events

    public event EventHandler? ImportRequested;
    public event EventHandler? ExportRequested;
    public event EventHandler? GenerateReportRequested;

    #endregion

    #region Methods

    /// <summary>
    /// Loads all data for the module
    /// </summary>
    public async Task LoadDataAsync()
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            // Load data for all child ViewModels
            await PersonnelListViewModel.LoadDataAsync();
            await TimesheetListViewModel.LoadDataAsync();
            await CertificationViewModel.LoadDataAsync();
            await VesselAssignmentViewModel.LoadDataAsync();
        }, "Loading data...");
    }

    /// <summary>
    /// Gets the database service for child components
    /// </summary>
    public PersonnelDatabaseService GetDatabaseService() => _dbService;

    private void Import()
    {
        ImportRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Export()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    private void GenerateReport()
    {
        GenerateReportRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
