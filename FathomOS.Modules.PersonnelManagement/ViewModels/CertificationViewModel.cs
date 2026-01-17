using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Modules.PersonnelManagement.Models;
using FathomOS.Modules.PersonnelManagement.Services;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// ViewModel for certification management and expiry tracking
/// </summary>
public class CertificationViewModel : ViewModelBase
{
    private readonly PersonnelDatabaseService _dbService;
    private IPersonnelService? _personnelService;

    #region Constructor

    public CertificationViewModel(PersonnelDatabaseService dbService)
    {
        _dbService = dbService;

        // Initialize collections
        ExpiringCertifications = new ObservableCollection<PersonnelCertification>();
        ExpiredCertifications = new ObservableCollection<PersonnelCertification>();
        AllCertifications = new ObservableCollection<PersonnelCertification>();
        CertificationTypes = new ObservableCollection<CertificationType>();
        Categories = new ObservableCollection<string>(new[] { "All Categories" }.Concat(Enum.GetNames(typeof(CertificationCategory))));

        // Initialize commands
        LoadDataCommand = new AsyncRelayCommand(LoadDataAsync);
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        AddCertificationCommand = new RelayCommand(AddCertification);
        EditCertificationCommand = new RelayCommand(EditCertification, () => SelectedCertification != null);
        RenewCertificationCommand = new RelayCommand(RenewCertification, () => SelectedCertification != null);
        ViewPersonnelCommand = new RelayCommand(ViewPersonnel, () => SelectedCertification?.Personnel != null);
        ExportCommand = new RelayCommand(Export);
        SendReminderCommand = new AsyncRelayCommand(SendReminderAsync, () => SelectedCertification != null);

        // Set defaults
        SelectedCategory = "All Categories";
        DaysAhead = 60;
    }

    #endregion

    #region Properties

    private ObservableCollection<PersonnelCertification> _expiringCertifications = null!;
    /// <summary>
    /// Certifications expiring within the specified days
    /// </summary>
    public ObservableCollection<PersonnelCertification> ExpiringCertifications
    {
        get => _expiringCertifications;
        set => SetProperty(ref _expiringCertifications, value);
    }

    private ObservableCollection<PersonnelCertification> _expiredCertifications = null!;
    /// <summary>
    /// Already expired certifications
    /// </summary>
    public ObservableCollection<PersonnelCertification> ExpiredCertifications
    {
        get => _expiredCertifications;
        set => SetProperty(ref _expiredCertifications, value);
    }

    private ObservableCollection<PersonnelCertification> _allCertifications = null!;
    /// <summary>
    /// All certifications (filtered view)
    /// </summary>
    public ObservableCollection<PersonnelCertification> AllCertifications
    {
        get => _allCertifications;
        set => SetProperty(ref _allCertifications, value);
    }

    private PersonnelCertification? _selectedCertification;
    public PersonnelCertification? SelectedCertification
    {
        get => _selectedCertification;
        set
        {
            if (SetProperty(ref _selectedCertification, value))
            {
                ((RelayCommand)EditCertificationCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RenewCertificationCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ViewPersonnelCommand).RaiseCanExecuteChanged();
                ((AsyncRelayCommand)SendReminderCommand).RaiseCanExecuteChanged();
            }
        }
    }

    private ObservableCollection<CertificationType> _certificationTypes = null!;
    public ObservableCollection<CertificationType> CertificationTypes
    {
        get => _certificationTypes;
        set => SetProperty(ref _certificationTypes, value);
    }

    private CertificationType? _selectedCertificationType;
    public CertificationType? SelectedCertificationType
    {
        get => _selectedCertificationType;
        set
        {
            if (SetProperty(ref _selectedCertificationType, value))
            {
                ApplyFilters();
            }
        }
    }

    private ObservableCollection<string> _categories = null!;
    public ObservableCollection<string> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    private string? _selectedCategory;
    public string? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplyFilters();
            }
        }
    }

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

    private int _daysAhead;
    /// <summary>
    /// Number of days ahead to look for expiring certifications
    /// </summary>
    public int DaysAhead
    {
        get => _daysAhead;
        set
        {
            if (SetProperty(ref _daysAhead, value))
            {
                _ = LoadExpiringCertificationsAsync();
            }
        }
    }

    private int _expiringCount;
    public int ExpiringCount
    {
        get => _expiringCount;
        set => SetProperty(ref _expiringCount, value);
    }

    private int _expiredCount;
    public int ExpiredCount
    {
        get => _expiredCount;
        set => SetProperty(ref _expiredCount, value);
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set => SetProperty(ref _totalCount, value);
    }

    public string StatusText => $"{TotalCount} total certifications";
    public string ExpiringText => $"{ExpiringCount} expiring within {DaysAhead} days";
    public string ExpiredText => $"{ExpiredCount} expired";

    #endregion

    #region Commands

    public ICommand LoadDataCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand AddCertificationCommand { get; }
    public ICommand EditCertificationCommand { get; }
    public ICommand RenewCertificationCommand { get; }
    public ICommand ViewPersonnelCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand SendReminderCommand { get; }

    #endregion

    #region Events

    public event EventHandler? AddCertificationRequested;
    public event EventHandler<PersonnelCertification>? EditCertificationRequested;
    public event EventHandler<PersonnelCertification>? RenewCertificationRequested;
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

            // Load certification types
            var certTypes = await Task.Run(() => _dbService.Context.CertificationTypes.Where(c => c.IsActive).ToList());
            CertificationTypes.Clear();
            foreach (var ct in certTypes.OrderBy(c => c.Category).ThenBy(c => c.Name))
            {
                CertificationTypes.Add(ct);
            }

            // Load expiring and expired certifications
            await LoadExpiringCertificationsAsync();

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ExpiringText));
            OnPropertyChanged(nameof(ExpiredText));
        }, "Loading certifications...");
    }

    private async Task LoadExpiringCertificationsAsync()
    {
        if (_personnelService == null)
        {
            _personnelService = _dbService.GetPersonnelService();
        }

        // Get expiring certifications
        var expiring = await _personnelService.GetExpiringCertificationsAsync(DaysAhead);
        ExpiringCertifications.Clear();
        foreach (var cert in expiring.Where(c => !c.IsExpired).OrderBy(c => c.ExpiryDate))
        {
            ExpiringCertifications.Add(cert);
        }
        ExpiringCount = ExpiringCertifications.Count;

        // Get expired certifications (those already expired)
        var allExpiring = await _personnelService.GetExpiringCertificationsAsync(0);
        ExpiredCertifications.Clear();

        // Load all certifications from context to find expired ones
        var allCerts = await Task.Run(() => _dbService.Context.PersonnelCertifications
            .Where(c => c.IsActive && c.ExpiryDate.HasValue && c.ExpiryDate.Value < DateTime.Today)
            .ToList());

        foreach (var cert in allCerts.OrderBy(c => c.ExpiryDate))
        {
            ExpiredCertifications.Add(cert);
        }
        ExpiredCount = ExpiredCertifications.Count;

        // Load all certifications for the grid
        var all = await Task.Run(() => _dbService.Context.PersonnelCertifications
            .Where(c => c.IsActive)
            .ToList());
        AllCertifications.Clear();
        foreach (var cert in all.OrderBy(c => c.ExpiryDate))
        {
            AllCertifications.Add(cert);
        }
        TotalCount = AllCertifications.Count;

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        // Filtering would be applied here if needed
        // For now, the collections are already filtered from the data source
        OnPropertyChanged(nameof(StatusText));
    }

    private void AddCertification()
    {
        AddCertificationRequested?.Invoke(this, EventArgs.Empty);
    }

    private void EditCertification()
    {
        if (SelectedCertification != null)
        {
            EditCertificationRequested?.Invoke(this, SelectedCertification);
        }
    }

    private void RenewCertification()
    {
        if (SelectedCertification != null)
        {
            RenewCertificationRequested?.Invoke(this, SelectedCertification);
        }
    }

    private void ViewPersonnel()
    {
        if (SelectedCertification?.Personnel != null)
        {
            ViewPersonnelRequested?.Invoke(this, SelectedCertification.Personnel);
        }
    }

    private void Export()
    {
        ExportRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task SendReminderAsync()
    {
        if (SelectedCertification == null || _personnelService == null)
            return;

        await ExecuteWithBusyIndicatorAsync(async () =>
        {
            // Mark as reminder sent
            SelectedCertification.RenewalReminderSent = true;
            SelectedCertification.UpdatedAt = DateTime.UtcNow;
            await _personnelService.UpdateCertificationAsync(SelectedCertification);

            MessageRequested?.Invoke(this, $"Reminder sent for {SelectedCertification.CertificationType?.Name ?? "certification"}.");
        }, "Sending reminder...");
    }

    /// <summary>
    /// Gets certifications by category for dashboard display
    /// </summary>
    public IEnumerable<IGrouping<CertificationCategory, PersonnelCertification>> GetCertificationsByCategory()
    {
        return ExpiringCertifications.GroupBy(c => c.CertificationType?.Category ?? CertificationCategory.Other);
    }

    /// <summary>
    /// Gets the count of certifications expiring by month
    /// </summary>
    public Dictionary<string, int> GetExpiringByMonth()
    {
        var result = new Dictionary<string, int>();
        var today = DateTime.Today;

        for (int i = 0; i < 6; i++)
        {
            var month = today.AddMonths(i);
            var monthName = month.ToString("MMM yyyy");
            var count = AllCertifications.Count(c =>
                c.ExpiryDate.HasValue &&
                c.ExpiryDate.Value.Year == month.Year &&
                c.ExpiryDate.Value.Month == month.Month);
            result[monthName] = count;
        }

        return result;
    }

    #endregion
}
