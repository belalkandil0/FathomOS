using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// ViewModel for the timesheet list view with filtering and approval workflow
/// </summary>
public class TimesheetListViewModel : ViewModelBase
{
    private readonly PersonnelDatabaseService _dbService;
    private ITimesheetService? _timesheetService;
    private IPersonnelService? _personnelService;

    #region Constructor

    public TimesheetListViewModel(PersonnelDatabaseService dbService)
    {
        _dbService = dbService;

        // Initialize collections
        TimesheetList = new ObservableCollection<Timesheet>();
        FilteredTimesheetList = new ObservableCollection<Timesheet>();
        PersonnelList = new ObservableCollection<Personnel>();
        Statuses = new ObservableCollection<string>(new[] { "All Statuses" }.Concat(Enum.GetNames(typeof(TimesheetStatus))));

        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        NewTimesheetCommand = new RelayCommand(NewTimesheet);
        ViewTimesheetCommand = new RelayCommand(ViewTimesheet, () => SelectedTimesheet != null);
        EditTimesheetCommand = new RelayCommand(EditTimesheet, () => SelectedTimesheet != null && SelectedTimesheet.CanEdit);
        ApproveCommand = new AsyncRelayCommand(ApproveTimesheetAsync, () => SelectedTimesheet != null && SelectedTimesheet.CanApprove);
        RejectCommand = new AsyncRelayCommand(RejectTimesheetAsync, () => SelectedTimesheet != null && SelectedTimesheet.CanApprove);
        SubmitCommand = new AsyncRelayCommand(SubmitTimesheetAsync, () => SelectedTimesheet != null && SelectedTimesheet.CanSubmit);
        ExportCommand = new RelayCommand(Export);
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        // Set defaults
        SelectedStatus = "All Statuses";
        PeriodStartDate = DateTime.Today.AddMonths(-1);
        PeriodEndDate = DateTime.Today;
    }

    #endregion

    #region Properties

    private ObservableCollection<Timesheet> _timesheetList = null!;
    public ObservableCollection<Timesheet> TimesheetList
    {
        get => _timesheetList;
        set => SetProperty(ref _timesheetList, value);
    }

    private ObservableCollection<Timesheet> _filteredTimesheetList = null!;
    public ObservableCollection<Timesheet> FilteredTimesheetList
    {
        get => _filteredTimesheetList;
        set => SetProperty(ref _filteredTimesheetList, value);
    }

    private Timesheet? _selectedTimesheet;
    public Timesheet? SelectedTimesheet
    {
        get => _selectedTimesheet;
        set
        {
            if (SetProperty(ref _selectedTimesheet, value))
            {
                ((RelayCommand)ViewTimesheetCommand).RaiseCanExecuteChanged();
                ((RelayCommand)EditTimesheetCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)ApproveCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)RejectCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SubmitCommand).RaiseCanExecuteChanged();
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

    private DateTime _periodStartDate;
    public DateTime PeriodStartDate
    {
        get => _periodStartDate;
        set
        {
            if (SetProperty(ref _periodStartDate, value))
            {
                ApplyFilters();
            }
        }
    }

    private DateTime _periodEndDate;
    public DateTime PeriodEndDate
    {
        get => _periodEndDate;
        set
        {
            if (SetProperty(ref _periodEndDate, value))
            {
                ApplyFilters();
            }
        }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    private int _pendingApprovalCount;
    public int PendingApprovalCount
    {
        get => _pendingApprovalCount;
        set => SetProperty(ref _pendingApprovalCount, value);
    }

    public string StatusText => $"{FilteredTimesheetList.Count} timesheets";
    public string PendingText => $"{PendingApprovalCount} pending approval";

    #endregion

    #region Commands

    public ICommand LoadDataCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand NewTimesheetCommand { get; }
    public ICommand ViewTimesheetCommand { get; }
    public ICommand EditTimesheetCommand { get; }
    public ICommand ApproveCommand { get; }
    public ICommand RejectCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    #endregion

    #region Events

    public event EventHandler? NewTimesheetRequested;
    public event EventHandler<Timesheet>? ViewTimesheetRequested;
    public event EventHandler<Timesheet>? EditTimesheetRequested;
    public event EventHandler? ExportRequested;
    public event EventHandler<string>? MessageRequested;

    #endregion

    #region Methods

    public async Task LoadDataAsync()
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _timesheetService = _dbService.GetTimesheetService();
            _personnelService = _dbService.GetPersonnelService();

            // Load personnel for filter
            var personnel = await _personnelService.GetActivePersonnelAsync();
            PersonnelList.Clear();
            PersonnelList.Add(new Personnel { FirstName = "All", LastName = "Personnel" }); // Placeholder
            foreach (var p in personnel)
            {
                PersonnelList.Add(p);
            }

            // Load timesheets
            await LoadTimesheetsAsync();

            // Count pending approvals
            var pending = await _timesheetService.GetTimesheetsPendingApprovalAsync();
            PendingApprovalCount = pending.Count();

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(PendingText));
        }, "Loading timesheets...");
    }

    private async Task LoadTimesheetsAsync()
    {
        if (_timesheetService == null) return;

        TimesheetList.Clear();

        IEnumerable<Timesheet> timesheets;

        if (SelectedPersonnel != null && SelectedPersonnel.PersonnelId != Guid.Empty)
        {
            timesheets = await _timesheetService.GetTimesheetsForPersonnelAsync(SelectedPersonnel.PersonnelId);
        }
        else if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "All Statuses")
        {
            if (Enum.TryParse<TimesheetStatus>(SelectedStatus, out var status))
            {
                timesheets = await _timesheetService.GetTimesheetsByStatusAsync(status);
            }
            else
            {
                timesheets = await _timesheetService.GetTimesheetsByStatusAsync(TimesheetStatus.Draft);
            }
        }
        else
        {
            timesheets = await _timesheetService.GetTimesheetsByStatusAsync(TimesheetStatus.Draft);
        }

        foreach (var t in timesheets)
        {
            TimesheetList.Add(t);
        }

        TotalCount = TimesheetList.Count;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = TimesheetList.AsEnumerable();

        // Filter by period dates
        filtered = filtered.Where(t =>
            t.PeriodStartDate >= PeriodStartDate &&
            t.PeriodEndDate <= PeriodEndDate);

        // Filter by status
        if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "All Statuses")
        {
            if (Enum.TryParse<TimesheetStatus>(SelectedStatus, out var status))
            {
                filtered = filtered.Where(t => t.Status == status);
            }
        }

        // Filter by personnel
        if (SelectedPersonnel != null && SelectedPersonnel.PersonnelId != Guid.Empty)
        {
            filtered = filtered.Where(t => t.PersonnelId == SelectedPersonnel.PersonnelId);
        }

        FilteredTimesheetList.Clear();
        foreach (var t in filtered)
        {
            FilteredTimesheetList.Add(t);
        }

        OnPropertyChanged(nameof(StatusText));
    }

    private void ClearFilters()
    {
        SelectedStatus = "All Statuses";
        SelectedPersonnel = null;
        PeriodStartDate = DateTime.Today.AddMonths(-1);
        PeriodEndDate = DateTime.Today;
    }

    private void NewTimesheet()
    {
        NewTimesheetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ViewTimesheet()
    {
        if (SelectedTimesheet != null)
        {
            ViewTimesheetRequested?.Invoke(this, SelectedTimesheet);
        }
    }

    private void EditTimesheet()
    {
        if (SelectedTimesheet != null)
        {
            EditTimesheetRequested?.Invoke(this, SelectedTimesheet);
        }
    }

    private async Task ApproveTimesheetAsync()
    {
        if (SelectedTimesheet == null || _timesheetService == null)
            return;

        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            var result = await _timesheetService.ApproveTimesheetAsync(
                SelectedTimesheet.TimesheetId,
                Guid.Empty, // Would use actual user ID
                null);

            if (result)
            {
                MessageRequested?.Invoke(this, "Timesheet approved successfully.");
                await LoadDataAsync();
            }
        }, "Approving timesheet...");
    }

    private async Task RejectTimesheetAsync()
    {
        if (SelectedTimesheet == null || _timesheetService == null)
            return;

        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            var result = await _timesheetService.RejectTimesheetAsync(
                SelectedTimesheet.TimesheetId,
                Guid.Empty, // Would use actual user ID
                "Rejected by supervisor");

            if (result)
            {
                MessageRequested?.Invoke(this, "Timesheet rejected.");
                await LoadDataAsync();
            }
        }, "Rejecting timesheet...");
    }

    private async Task SubmitTimesheetAsync()
    {
        if (SelectedTimesheet == null || _timesheetService == null)
            return;

        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            var result = await _timesheetService.SubmitTimesheetAsync(
                SelectedTimesheet.TimesheetId,
                null);

            if (result)
            {
                MessageRequested?.Invoke(this, "Timesheet submitted for approval.");
                await LoadDataAsync();
            }
        }, "Submitting timesheet...");
    }

    private void Export()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
