using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// ViewModel for vessel assignment management with sign-on/sign-off workflow
/// </summary>
public class VesselAssignmentViewModel : ViewModelBase
{
    private readonly PersonnelDatabaseService _dbService;
    private IPersonnelService? _personnelService;

    #region Constructor

    public VesselAssignmentViewModel(PersonnelDatabaseService dbService)
    {
        _dbService = dbService;

        // Initialize collections
        ActiveAssignments = new ObservableCollection<VesselAssignment>();
        UpcomingAssignments = new ObservableCollection<VesselAssignment>();
        AllAssignments = new ObservableCollection<VesselAssignment>();
        PersonnelList = new ObservableCollection<Personnel>();
        Statuses = new ObservableCollection<string>(new[] { "All Statuses" }.Concat(Enum.GetNames(typeof(AssignmentStatus))));

        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        NewAssignmentCommand = new RelayCommand(NewAssignment);
        EditAssignmentCommand = new RelayCommand(EditAssignment, () => SelectedAssignment != null);
        ViewAssignmentCommand = new RelayCommand(ViewAssignment, () => SelectedAssignment != null);
        SignOnCommand = new AsyncRelayCommand(SignOnAsync, CanSignOn);
        SignOffCommand = new AsyncRelayCommand(SignOffAsync, CanSignOff);
        CancelAssignmentCommand = new AsyncRelayCommand(CancelAssignmentAsync, () => SelectedAssignment?.Status == AssignmentStatus.Scheduled);
        ViewPersonnelCommand = new RelayCommand(ViewPersonnel, () => SelectedAssignment?.Personnel != null);
        ExportCommand = new RelayCommand(Export);

        // Set defaults
        SelectedStatus = "All Statuses";
        DaysAhead = 30;
    }

    #endregion

    #region Properties - Lists

    private ObservableCollection<VesselAssignment> _activeAssignments = null!;
    /// <summary>
    /// Personnel currently signed on to vessels
    /// </summary>
    public ObservableCollection<VesselAssignment> ActiveAssignments
    {
        get => _activeAssignments;
        set => SetProperty(ref _activeAssignments, value);
    }

    private ObservableCollection<VesselAssignment> _upcomingAssignments = null!;
    /// <summary>
    /// Scheduled assignments within the specified days
    /// </summary>
    public ObservableCollection<VesselAssignment> UpcomingAssignments
    {
        get => _upcomingAssignments;
        set => SetProperty(ref _upcomingAssignments, value);
    }

    private ObservableCollection<VesselAssignment> _allAssignments = null!;
    /// <summary>
    /// All assignments (filtered view)
    /// </summary>
    public ObservableCollection<VesselAssignment> AllAssignments
    {
        get => _allAssignments;
        set => SetProperty(ref _allAssignments, value);
    }

    private VesselAssignment? _selectedAssignment;
    public VesselAssignment? SelectedAssignment
    {
        get => _selectedAssignment;
        set
        {
            if (SetProperty(ref _selectedAssignment, value))
            {
                ((RelayCommand)EditAssignmentCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ViewAssignmentCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SignOnCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SignOffCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)CancelAssignmentCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ViewPersonnelCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private ObservableCollection<Personnel> _personnelList = null!;
    public ObservableCollection<Personnel> PersonnelList
    {
        get => _personnelList;
        set => SetProperty(ref _personnelList, value);
    }

    private Personnel? _selectedPersonnel;
    public Personnel? SelectedPersonnel
    {
        get => _selectedPersonnel;
        set
        {
            if (SetProperty(ref _selectedPersonnel, value))
            {
                ApplyFilters();
            }
        }
    }

    private ObservableCollection<string> _statuses = null!;
    public ObservableCollection<string> Statuses
    {
        get => _statuses;
        set => SetProperty(ref _statuses, value);
    }

    private string? _selectedStatus;
    public string? SelectedStatus
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

    private string? _vesselFilter;
    public string? VesselFilter
    {
        get => _vesselFilter;
        set
        {
            if (SetProperty(ref _vesselFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    private int _daysAhead;
    public int DaysAhead
    {
        get => _daysAhead;
        set
        {
            if (SetProperty(ref _daysAhead, value))
            {
                _ = LoadUpcomingAssignmentsAsync();
            }
        }
    }

    #endregion

    #region Properties - Sign-On/Sign-Off Dialog

    private DateTime _signOnDateTime = DateTime.Now;
    public DateTime SignOnDateTime
    {
        get => _signOnDateTime;
        set => SetProperty(ref _signOnDateTime, value);
    }

    private string? _signOnLocation;
    public string? SignOnLocation
    {
        get => _signOnLocation;
        set => SetProperty(ref _signOnLocation, value);
    }

    private string? _signOnPort;
    public string? SignOnPort
    {
        get => _signOnPort;
        set => SetProperty(ref _signOnPort, value);
    }

    private DateTime _signOffDateTime = DateTime.Now;
    public DateTime SignOffDateTime
    {
        get => _signOffDateTime;
        set => SetProperty(ref _signOffDateTime, value);
    }

    private string? _signOffLocation;
    public string? SignOffLocation
    {
        get => _signOffLocation;
        set => SetProperty(ref _signOffLocation, value);
    }

    private string? _signOffPort;
    public string? SignOffPort
    {
        get => _signOffPort;
        set => SetProperty(ref _signOffPort, value);
    }

    private string? _signOffReason;
    public string? SignOffReason
    {
        get => _signOffReason;
        set => SetProperty(ref _signOffReason, value);
    }

    #endregion

    #region Properties - Statistics

    private int _activeCount;
    public int ActiveCount
    {
        get => _activeCount;
        set => SetProperty(ref _activeCount, value);
    }

    private int _upcomingCount;
    public int UpcomingCount
    {
        get => _upcomingCount;
        set => SetProperty(ref _upcomingCount, value);
    }

    private int _overdueCount;
    public int OverdueCount
    {
        get => _overdueCount;
        set => SetProperty(ref _overdueCount, value);
    }

    public string ActiveText => $"{ActiveCount} currently onboard";
    public string UpcomingText => $"{UpcomingCount} upcoming in {DaysAhead} days";
    public string OverdueText => $"{OverdueCount} overdue for sign-on";

    #endregion

    #region Commands

    public ICommand LoadDataCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NewAssignmentCommand { get; }
    public ICommand EditAssignmentCommand { get; }
    public ICommand ViewAssignmentCommand { get; }
    public ICommand SignOnCommand { get; }
    public ICommand SignOffCommand { get; }
    public ICommand CancelAssignmentCommand { get; }
    public ICommand ViewPersonnelCommand { get; }
    public ICommand ExportCommand { get; }

    #endregion

    #region Events

    public event EventHandler? NewAssignmentRequested;
    public event EventHandler<VesselAssignment>? EditAssignmentRequested;
    public event EventHandler<VesselAssignment>? ViewAssignmentRequested;
    public event EventHandler<VesselAssignment>? SignOnRequested;
    public event EventHandler<VesselAssignment>? SignOffRequested;
    public event EventHandler<Personnel>? ViewPersonnelRequested;
    public event EventHandler? ExportRequested;
    public event EventHandler<string>? MessageRequested;

    #endregion

    #region Methods

    public async Task LoadDataAsync()
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _personnelService = _dbService.GetPersonnelService();

            // Load personnel for filter
            var personnel = await _personnelService.GetActivePersonnelAsync();
            PersonnelList.Clear();
            foreach (var p in personnel)
            {
                PersonnelList.Add(p);
            }

            // Load active assignments
            var active = await _personnelService.GetActiveVesselAssignmentsAsync();
            ActiveAssignments.Clear();
            foreach (var a in active)
            {
                ActiveAssignments.Add(a);
            }
            ActiveCount = ActiveAssignments.Count;

            // Load upcoming assignments
            await LoadUpcomingAssignmentsAsync();

            // Load all assignments
            var all = await Task.Run(() => _dbService.Context.VesselAssignments
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.ScheduledStartDate)
                .ToList());
            AllAssignments.Clear();
            foreach (var a in all)
            {
                AllAssignments.Add(a);
            }

            // Count overdue
            OverdueCount = AllAssignments.Count(a => a.IsOverdueForSignOn);

            ApplyFilters();
            OnPropertyChanged(nameof(ActiveText));
            OnPropertyChanged(nameof(UpcomingText));
            OnPropertyChanged(nameof(OverdueText));
        }, "Loading assignments...");
    }

    private async Task LoadUpcomingAssignmentsAsync()
    {
        if (_personnelService == null)
        {
            _personnelService = _dbService.GetPersonnelService();
        }

        var upcoming = await _personnelService.GetUpcomingAssignmentsAsync(DaysAhead);
        UpcomingAssignments.Clear();
        foreach (var a in upcoming.OrderBy(a => a.ScheduledStartDate))
        {
            UpcomingAssignments.Add(a);
        }
        UpcomingCount = UpcomingAssignments.Count;

        OnPropertyChanged(nameof(UpcomingText));
    }

    private void ApplyFilters()
    {
        // Filtering could be applied to AllAssignments if needed
        OnPropertyChanged(nameof(ActiveText));
    }

    private bool CanSignOn()
    {
        return SelectedAssignment != null && SelectedAssignment.Status == AssignmentStatus.Scheduled;
    }

    private bool CanSignOff()
    {
        return SelectedAssignment != null && SelectedAssignment.Status == AssignmentStatus.SignedOn;
    }

    private void NewAssignment()
    {
        NewAssignmentRequested?.Invoke(this, EventArgs.Empty);
    }

    private void EditAssignment()
    {
        if (SelectedAssignment != null)
        {
            EditAssignmentRequested?.Invoke(this, SelectedAssignment);
        }
    }

    private void ViewAssignment()
    {
        if (SelectedAssignment != null)
        {
            ViewAssignmentRequested?.Invoke(this, SelectedAssignment);
        }
    }

    private Task SignOnAsync()
    {
        if (SelectedAssignment == null || _personnelService == null)
            return Task.CompletedTask;

        // In a real app, we'd show a dialog to get sign-on details
        SignOnRequested?.Invoke(this, SelectedAssignment);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the actual sign-on operation after dialog confirmation
    /// </summary>
    public async Task PerformSignOnAsync(Guid assignmentId, DateTime signOnDateTime, string? location, string? port)
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _personnelService = _dbService.GetPersonnelService();
            var result = await _personnelService.SignOnAsync(assignmentId, signOnDateTime, location, port);

            if (result)
            {
                MessageRequested?.Invoke(this, "Personnel signed on successfully.");
                await LoadDataAsync();
            }
        }, "Signing on...");
    }

    private Task SignOffAsync()
    {
        if (SelectedAssignment == null || _personnelService == null)
            return Task.CompletedTask;

        // In a real app, we'd show a dialog to get sign-off details
        SignOffRequested?.Invoke(this, SelectedAssignment);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the actual sign-off operation after dialog confirmation
    /// </summary>
    public async Task PerformSignOffAsync(Guid assignmentId, DateTime signOffDateTime, string? location, string? port, string? reason)
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _personnelService = _dbService.GetPersonnelService();
            var result = await _personnelService.SignOffAsync(assignmentId, signOffDateTime, location, port, reason);

            if (result)
            {
                MessageRequested?.Invoke(this, "Personnel signed off successfully.");
                await LoadDataAsync();
            }
        }, "Signing off...");
    }

    private async Task CancelAssignmentAsync()
    {
        if (SelectedAssignment == null)
            return;

        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            SelectedAssignment.Status = AssignmentStatus.Cancelled;
            SelectedAssignment.UpdatedAt = DateTime.UtcNow;

            _personnelService = _dbService.GetPersonnelService();
            await _personnelService.UpdateVesselAssignmentAsync(SelectedAssignment);

            MessageRequested?.Invoke(this, "Assignment cancelled.");
            await LoadDataAsync();
        }, "Cancelling assignment...");
    }

    private void ViewPersonnel()
    {
        if (SelectedAssignment?.Personnel != null)
        {
            ViewPersonnelRequested?.Invoke(this, SelectedAssignment.Personnel);
        }
    }

    private void Export()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets assignments grouped by vessel
    /// </summary>
    public IEnumerable<IGrouping<string, VesselAssignment>> GetAssignmentsByVessel()
    {
        return ActiveAssignments.GroupBy(a => a.VesselName ?? "Unassigned");
    }

    /// <summary>
    /// Gets the count of personnel per vessel
    /// </summary>
    public Dictionary<string, int> GetPersonnelCountByVessel()
    {
        return ActiveAssignments
            .GroupBy(a => a.VesselName ?? "Unassigned")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    #endregion
}
