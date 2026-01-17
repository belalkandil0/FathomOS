using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// ViewModel for the personnel list view with search, filter, and CRUD operations
/// </summary>
public class PersonnelListViewModel : ViewModelBase
{
    private readonly PersonnelDatabaseService _dbService;
    private IPersonnelService? _personnelService;

    #region Constructor

    public PersonnelListViewModel(PersonnelDatabaseService dbService)
    {
        _dbService = dbService;
        _personnelService = _dbService.GetPersonnelService();

        // Initialize collections
        PersonnelList = new ObservableCollection<Personnel>();
        FilteredPersonnelList = new ObservableCollection<Personnel>();
        Departments = new ObservableCollection<string>(new[] { "All Departments" }.Concat(Enum.GetNames(typeof(Department))));
        Statuses = new ObservableCollection<string>(new[] { "All Statuses" }.Concat(Enum.GetNames(typeof(EmploymentStatus))));

        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        AddPersonnelCommand = new RelayCommand(AddPersonnel);
        EditPersonnelCommand = new RelayCommand(EditPersonnel, () => SelectedPersonnel != null);
        ViewPersonnelCommand = new RelayCommand(ViewPersonnel, () => SelectedPersonnel != null);
        DeletePersonnelCommand = new AsyncRelayCommand(DeletePersonnelAsync, () => SelectedPersonnel != null);
        ExportCommand = new RelayCommand(Export);
        ClearFiltersCommand = new RelayCommand(ClearFilters);

        // Set defaults
        SelectedDepartment = "All Departments";
        SelectedStatus = "All Statuses";
    }

    #endregion

    #region Properties

    private ObservableCollection<Personnel> _personnelList = null!;
    /// <summary>
    /// Full list of personnel (unfiltered)
    /// </summary>
    public ObservableCollection<Personnel> PersonnelList
    {
        get => _personnelList;
        set => SetProperty(ref _personnelList, value);
    }

    private ObservableCollection<Personnel> _filteredPersonnelList = null!;
    /// <summary>
    /// Filtered list of personnel displayed in the grid
    /// </summary>
    public ObservableCollection<Personnel> FilteredPersonnelList
    {
        get => _filteredPersonnelList;
        set => SetProperty(ref _filteredPersonnelList, value);
    }

    private Personnel? _selectedPersonnel;
    /// <summary>
    /// Currently selected personnel in the grid
    /// </summary>
    public Personnel? SelectedPersonnel
    {
        get => _selectedPersonnel;
        set
        {
            if (SetProperty(ref _selectedPersonnel, value))
            {
                ((RelayCommand)EditPersonnelCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ViewPersonnelCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)DeletePersonnelCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private string? _searchText;
    /// <summary>
    /// Search text for filtering personnel
    /// </summary>
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

    private ObservableCollection<string> _departments = null!;
    /// <summary>
    /// Available departments for filtering
    /// </summary>
    public ObservableCollection<string> Departments
    {
        get => _departments;
        set => SetProperty(ref _departments, value);
    }

    private string? _selectedDepartment;
    /// <summary>
    /// Selected department filter
    /// </summary>
    public string? SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            if (SetProperty(ref _selectedDepartment, value))
            {
                ApplyFilters();
            }
        }
    }

    private ObservableCollection<string> _statuses = null!;
    /// <summary>
    /// Available statuses for filtering
    /// </summary>
    public ObservableCollection<string> Statuses
    {
        get => _statuses;
        set => SetProperty(ref _statuses, value);
    }

    private string? _selectedStatus;
    /// <summary>
    /// Selected status filter
    /// </summary>
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

    private int _totalCount;
    /// <summary>
    /// Total count of personnel
    /// </summary>
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    private int _filteredCount;
    /// <summary>
    /// Count of filtered personnel
    /// </summary>
    public int FilteredCount
    {
        get => _filteredCount;
        set => SetProperty(ref _filteredCount, value);
    }

    private int _offshoreCount;
    /// <summary>
    /// Count of personnel currently offshore
    /// </summary>
    public int OffshoreCount
    {
        get => _offshoreCount;
        set => SetProperty(ref _offshoreCount, value);
    }

    private int _activeCount;
    /// <summary>
    /// Count of active personnel
    /// </summary>
    public int ActiveCount
    {
        get => _activeCount;
        set => SetProperty(ref _activeCount, value);
    }

    /// <summary>
    /// Status text for display
    /// </summary>
    public string StatusText => $"{FilteredCount} of {TotalCount} personnel";

    /// <summary>
    /// Offshore status text for display
    /// </summary>
    public string OffshoreText => $"{OffshoreCount} offshore";

    #endregion

    #region Commands

    public ICommand LoadDataCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddPersonnelCommand { get; }
    public ICommand EditPersonnelCommand { get; }
    public ICommand ViewPersonnelCommand { get; }
    public ICommand DeletePersonnelCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when requesting to add new personnel
    /// </summary>
    public event EventHandler? AddPersonnelRequested;

    /// <summary>
    /// Event raised when requesting to edit personnel
    /// </summary>
    public event EventHandler<Personnel>? EditPersonnelRequested;

    /// <summary>
    /// Event raised when requesting to view personnel
    /// </summary>
    public event EventHandler<Personnel>? ViewPersonnelRequested;

    /// <summary>
    /// Event raised when requesting to export personnel
    /// </summary>
    public event EventHandler? ExportRequested;

    #endregion

    #region Methods

    /// <summary>
    /// Loads all personnel data from the database
    /// </summary>
    public async Task LoadDataAsync()
    {
        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            _personnelService = _dbService.GetPersonnelService();

            var personnel = await _personnelService.GetAllPersonnelAsync();
            PersonnelList.Clear();
            foreach (var p in personnel)
            {
                PersonnelList.Add(p);
            }

            TotalCount = await _personnelService.GetTotalPersonnelCountAsync();
            ActiveCount = await _personnelService.GetActivePersonnelCountAsync();
            OffshoreCount = await _personnelService.GetOffshorePersonnelCountAsync();

            ApplyFilters();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(OffshoreText));
        }, "Loading personnel...");
    }

    /// <summary>
    /// Applies search and filter criteria to the personnel list
    /// </summary>
    private void ApplyFilters()
    {
        var filtered = PersonnelList.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filtered = filtered.Where(p =>
                p.FirstName.ToLower().Contains(searchLower) ||
                p.LastName.ToLower().Contains(searchLower) ||
                p.EmployeeNumber.ToLower().Contains(searchLower) ||
                (p.Email?.ToLower().Contains(searchLower) ?? false) ||
                p.FullName.ToLower().Contains(searchLower));
        }

        // Apply department filter
        if (!string.IsNullOrEmpty(SelectedDepartment) && SelectedDepartment != "All Departments")
        {
            if (Enum.TryParse<Department>(SelectedDepartment, out var department))
            {
                filtered = filtered.Where(p => p.Department == department);
            }
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "All Statuses")
        {
            if (Enum.TryParse<EmploymentStatus>(SelectedStatus, out var status))
            {
                filtered = filtered.Where(p => p.EmploymentStatus == status);
            }
        }

        FilteredPersonnelList.Clear();
        foreach (var p in filtered)
        {
            FilteredPersonnelList.Add(p);
        }

        FilteredCount = FilteredPersonnelList.Count;
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>
    /// Clears all filters
    /// </summary>
    private void ClearFilters()
    {
        SearchText = null;
        SelectedDepartment = "All Departments";
        SelectedStatus = "All Statuses";
    }

    /// <summary>
    /// Raises the AddPersonnelRequested event
    /// </summary>
    private void AddPersonnel()
    {
        AddPersonnelRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raises the EditPersonnelRequested event
    /// </summary>
    private void EditPersonnel()
    {
        if (SelectedPersonnel != null)
        {
            EditPersonnelRequested?.Invoke(this, SelectedPersonnel);
        }
    }

    /// <summary>
    /// Raises the ViewPersonnelRequested event
    /// </summary>
    private void ViewPersonnel()
    {
        if (SelectedPersonnel != null)
        {
            ViewPersonnelRequested?.Invoke(this, SelectedPersonnel);
        }
    }

    /// <summary>
    /// Deletes the selected personnel
    /// </summary>
    private async Task DeletePersonnelAsync()
    {
        if (SelectedPersonnel == null || _personnelService == null)
            return;

        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            var result = await _personnelService.DeletePersonnelAsync(SelectedPersonnel.PersonnelId);
            if (result)
            {
                await LoadDataAsync();
            }
        }, "Deleting personnel...");
    }

    /// <summary>
    /// Raises the ExportRequested event
    /// </summary>
    private void Export()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Refreshes the data after an edit
    /// </summary>
    public async Task RefreshAfterEditAsync()
    {
        await LoadDataAsync();
    }

    #endregion
}
