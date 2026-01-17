using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// ViewModel for the timesheet detail view with entry editing
/// </summary>
public class TimesheetDetailViewModel : ViewModelBase
{
    private readonly PersonnelDatabaseService _dbService;
    private readonly Guid? _timesheetId;
    private ITimesheetService? _timesheetService;
    private bool _isNewRecord;

    #region Constructor

    public TimesheetDetailViewModel(PersonnelDatabaseService dbService, Guid? timesheetId = null)
    {
        _dbService = dbService;
        _timesheetId = timesheetId;
        _isNewRecord = !timesheetId.HasValue;

        // Initialize collections
        PersonnelList = new ObservableCollection<Personnel>();
        Entries = new ObservableCollection<TimesheetEntry>();
        EntryTypes = new ObservableCollection<TimeEntryType>(Enum.GetValues(typeof(TimeEntryType)).Cast<TimeEntryType>());
        LeaveTypes = new ObservableCollection<LeaveType>(Enum.GetValues(typeof(LeaveType)).Cast<LeaveType>());

        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        CancelCommand = new RelayCommand(Cancel);
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, CanSubmit);
        AddEntryCommand = new RelayCommand(AddEntry);
        EditEntryCommand = new RelayCommand<TimesheetEntry>(EditEntry);
        DeleteEntryCommand = new AsyncRelayCommand<TimesheetEntry>(DeleteEntryAsync);
        GenerateEntriesCommand = new RelayCommand(GenerateEntries);

        // Set defaults for new timesheet
        if (_isNewRecord)
        {
            PeriodStartDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
            PeriodEndDate = PeriodStartDate.AddDays(13); // 2-week period
        }
    }

    #endregion

    #region Properties - Header Info

    private string _timesheetNumber = string.Empty;
    public string TimesheetNumber
    {
        get => _timesheetNumber;
        set => SetProperty(ref _timesheetNumber, value);
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
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
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
                OnPropertyChanged(nameof(PeriodDisplay));
                OnPropertyChanged(nameof(PeriodDays));
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
                OnPropertyChanged(nameof(PeriodDisplay));
                OnPropertyChanged(nameof(PeriodDays));
            }
        }
    }

    public string PeriodDisplay => $"{PeriodStartDate:dd MMM} - {PeriodEndDate:dd MMM yyyy}";
    public int PeriodDays => (int)(PeriodEndDate - PeriodStartDate).TotalDays + 1;

    private string? _vesselName;
    public string? VesselName
    {
        get => _vesselName;
        set => SetProperty(ref _vesselName, value);
    }

    private Guid? _vesselId;
    public Guid? VesselId
    {
        get => _vesselId;
        set => SetProperty(ref _vesselId, value);
    }

    private string? _projectName;
    public string? ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    private Guid? _projectId;
    public Guid? ProjectId
    {
        get => _projectId;
        set => SetProperty(ref _projectId, value);
    }

    private TimesheetStatus _status;
    public TimesheetStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(CanEdit));
                ((AsyncRelayCommand)SaveCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SubmitCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusDisplay => Status.ToString();
    public bool CanEdit => Status == TimesheetStatus.Draft || Status == TimesheetStatus.Rejected;

    #endregion

    #region Properties - Hour Totals

    private decimal _totalRegularHours;
    public decimal TotalRegularHours
    {
        get => _totalRegularHours;
        set => SetProperty(ref _totalRegularHours, value);
    }

    private decimal _totalOvertimeHours;
    public decimal TotalOvertimeHours
    {
        get => _totalOvertimeHours;
        set => SetProperty(ref _totalOvertimeHours, value);
    }

    private decimal _totalDoubleTimeHours;
    public decimal TotalDoubleTimeHours
    {
        get => _totalDoubleTimeHours;
        set => SetProperty(ref _totalDoubleTimeHours, value);
    }

    private decimal _totalNightShiftHours;
    public decimal TotalNightShiftHours
    {
        get => _totalNightShiftHours;
        set => SetProperty(ref _totalNightShiftHours, value);
    }

    private decimal _totalStandbyHours;
    public decimal TotalStandbyHours
    {
        get => _totalStandbyHours;
        set => SetProperty(ref _totalStandbyHours, value);
    }

    private decimal _totalTravelHours;
    public decimal TotalTravelHours
    {
        get => _totalTravelHours;
        set => SetProperty(ref _totalTravelHours, value);
    }

    public decimal TotalHours => TotalRegularHours + TotalOvertimeHours + TotalDoubleTimeHours +
                                  TotalNightShiftHours + TotalStandbyHours + TotalTravelHours;

    private decimal _totalLeaveDays;
    public decimal TotalLeaveDays
    {
        get => _totalLeaveDays;
        set => SetProperty(ref _totalLeaveDays, value);
    }

    private decimal _totalSickDays;
    public decimal TotalSickDays
    {
        get => _totalSickDays;
        set => SetProperty(ref _totalSickDays, value);
    }

    #endregion

    #region Properties - Entries

    private ObservableCollection<TimesheetEntry> _entries = null!;
    public ObservableCollection<TimesheetEntry> Entries
    {
        get => _entries;
        set => SetProperty(ref _entries, value);
    }

    private TimesheetEntry? _selectedEntry;
    public TimesheetEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => SetProperty(ref _selectedEntry, value);
    }

    private ObservableCollection<TimeEntryType> _entryTypes = null!;
    public ObservableCollection<TimeEntryType> EntryTypes
    {
        get => _entryTypes;
        set => SetProperty(ref _entryTypes, value);
    }

    private ObservableCollection<LeaveType> _leaveTypes = null!;
    public ObservableCollection<LeaveType> LeaveTypes
    {
        get => _leaveTypes;
        set => SetProperty(ref _leaveTypes, value);
    }

    #endregion

    #region Properties - Notes

    private string? _submissionComments;
    public string? SubmissionComments
    {
        get => _submissionComments;
        set => SetProperty(ref _submissionComments, value);
    }

    private string? _notes;
    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    #endregion

    #region Properties - View State

    public bool IsNewRecord => _isNewRecord;
    public string HeaderText => _isNewRecord ? "New Timesheet" : $"Timesheet: {TimesheetNumber}";

    #endregion

    #region Commands

    public ICommand LoadDataCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SubmitCommand { get; }
    public ICommand AddEntryCommand { get; }
    public ICommand EditEntryCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand GenerateEntriesCommand { get; }

    #endregion

    #region Events

    public event EventHandler<Timesheet>? SaveCompleted;
    public event EventHandler? CancelRequested;
    public event EventHandler<TimesheetEntry>? EditEntryRequested;
    public event EventHandler? AddEntryRequested;
    public event EventHandler<string>? MessageRequested;

    #endregion

    #region Methods

    public async Task LoadDataAsync()
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _timesheetService = _dbService.GetTimesheetService();
            var personnelService = _dbService.GetPersonnelService();

            // Load personnel for selection
            var personnel = await personnelService.GetActivePersonnelAsync();
            PersonnelList.Clear();
            foreach (var p in personnel)
            {
                PersonnelList.Add(p);
            }

            // Load timesheet if editing
            if (_timesheetId.HasValue)
            {
                var timesheet = await _timesheetService.GetTimesheetByIdAsync(_timesheetId.Value);
                if (timesheet != null)
                {
                    PopulateFromTimesheet(timesheet);
                }
            }

            OnPropertyChanged(nameof(HeaderText));
        }, "Loading timesheet...");
    }

    private void PopulateFromTimesheet(Timesheet timesheet)
    {
        TimesheetNumber = timesheet.TimesheetNumber;
        SelectedPersonnel = PersonnelList.FirstOrDefault(p => p.PersonnelId == timesheet.PersonnelId);
        PeriodStartDate = timesheet.PeriodStartDate;
        PeriodEndDate = timesheet.PeriodEndDate;
        VesselId = timesheet.VesselId;
        VesselName = timesheet.VesselName;
        ProjectId = timesheet.ProjectId;
        ProjectName = timesheet.ProjectName;
        Status = timesheet.Status;
        SubmissionComments = timesheet.SubmissionComments;
        Notes = timesheet.Notes;

        TotalRegularHours = timesheet.TotalRegularHours;
        TotalOvertimeHours = timesheet.TotalOvertimeHours;
        TotalDoubleTimeHours = timesheet.TotalDoubleTimeHours;
        TotalNightShiftHours = timesheet.TotalNightShiftHours;
        TotalStandbyHours = timesheet.TotalStandbyHours;
        TotalTravelHours = timesheet.TotalTravelHours;
        TotalLeaveDays = timesheet.TotalLeaveDays;
        TotalSickDays = timesheet.TotalSickDays;

        Entries.Clear();
        foreach (var entry in timesheet.Entries.OrderBy(e => e.EntryDate))
        {
            Entries.Add(entry);
        }

        OnPropertyChanged(nameof(TotalHours));
    }

    private void RecalculateTotals()
    {
        TotalRegularHours = Entries.Sum(e => e.RegularHours);
        TotalOvertimeHours = Entries.Sum(e => e.OvertimeHours);
        TotalDoubleTimeHours = Entries.Sum(e => e.DoubleTimeHours);
        TotalNightShiftHours = Entries.Sum(e => e.NightShiftHours);
        TotalStandbyHours = Entries.Sum(e => e.StandbyHours);
        TotalTravelHours = Entries.Sum(e => e.TravelHours);
        TotalLeaveDays = Entries.Count(e => e.IsLeave);
        TotalSickDays = Entries.Count(e => e.IsSickDay);

        OnPropertyChanged(nameof(TotalHours));
    }

    private bool CanSave()
    {
        return !IsBusy && SelectedPersonnel != null && CanEdit;
    }

    private bool CanSubmit()
    {
        return !IsBusy && !_isNewRecord && (Status == TimesheetStatus.Draft || Status == TimesheetStatus.Rejected);
    }

    private async Task SaveAsync()
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _timesheetService = _dbService.GetTimesheetService();

            if (_isNewRecord)
            {
                var timesheet = await _timesheetService.CreateTimesheetForPeriodAsync(
                    SelectedPersonnel!.PersonnelId,
                    PeriodStartDate,
                    PeriodEndDate,
                    VesselId,
                    ProjectId);

                // Save entries
                foreach (var entry in Entries)
                {
                    entry.TimesheetId = timesheet.TimesheetId;
                    await _timesheetService.AddEntryAsync(entry);
                }

                SaveCompleted?.Invoke(this, timesheet);
            }
            else
            {
                var timesheet = await _timesheetService.GetTimesheetByIdAsync(_timesheetId!.Value);
                if (timesheet != null)
                {
                    timesheet.VesselId = VesselId;
                    timesheet.VesselName = VesselName;
                    timesheet.ProjectId = ProjectId;
                    timesheet.ProjectName = ProjectName;
                    timesheet.Notes = Notes;

                    await _timesheetService.UpdateTimesheetAsync(timesheet);
                    SaveCompleted?.Invoke(this, timesheet);
                }
            }
        }, "Saving timesheet...");
    }

    private void Cancel()
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task SubmitAsync()
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _timesheetService = _dbService.GetTimesheetService();

            var result = await _timesheetService.SubmitTimesheetAsync(
                _timesheetId!.Value,
                SubmissionComments);

            if (result)
            {
                Status = TimesheetStatus.Submitted;
                MessageRequested?.Invoke(this, "Timesheet submitted for approval.");
            }
        }, "Submitting timesheet...");
    }

    private void AddEntry()
    {
        AddEntryRequested?.Invoke(this, EventArgs.Empty);
    }

    private void EditEntry(TimesheetEntry? entry)
    {
        if (entry != null)
        {
            EditEntryRequested?.Invoke(this, entry);
        }
    }

    private async Task DeleteEntryAsync(TimesheetEntry? entry)
    {
        if (entry == null || _timesheetService == null)
            return;

        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            var result = await _timesheetService.DeleteEntryAsync(entry.TimesheetEntryId);
            if (result)
            {
                Entries.Remove(entry);
                RecalculateTotals();
            }
        }, "Deleting entry...");
    }

    private void GenerateEntries()
    {
        // Generate entries for each day in the period
        Entries.Clear();

        for (var date = PeriodStartDate; date <= PeriodEndDate; date = date.AddDays(1))
        {
            var entry = new TimesheetEntry
            {
                TimesheetId = _timesheetId ?? Guid.Empty,
                EntryDate = date,
                EntryType = TimeEntryType.Regular,
                RegularHours = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
                    ? 0
                    : 12, // Default 12-hour days offshore
                IsRestDay = date.DayOfWeek == DayOfWeek.Sunday
            };

            Entries.Add(entry);
        }

        RecalculateTotals();
    }

    /// <summary>
    /// Adds or updates an entry
    /// </summary>
    public async Task SaveEntryAsync(TimesheetEntry entry)
    {
        _timesheetService = _dbService.GetTimesheetService();

        if (entry.TimesheetEntryId == Guid.Empty)
        {
            entry.TimesheetId = _timesheetId ?? Guid.Empty;
            if (_timesheetId.HasValue)
            {
                await _timesheetService.AddEntryAsync(entry);
            }
            Entries.Add(entry);
        }
        else
        {
            if (_timesheetId.HasValue)
            {
                await _timesheetService.UpdateEntryAsync(entry);
            }
            // Update in collection
            var index = Entries.ToList().FindIndex(e => e.TimesheetEntryId == entry.TimesheetEntryId);
            if (index >= 0)
            {
                Entries[index] = entry;
            }
        }

        RecalculateTotals();
    }

    #endregion
}
