using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;
using FathomOS.Modules.EquipmentInventory.Services;
using FathomOS.Modules.EquipmentInventory.Export;

namespace FathomOS.Modules.EquipmentInventory.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly LocalDatabaseService _dbService;
    private readonly SyncService _syncService;
    private readonly AuthenticationService _authService;
    private readonly QRCodeService _qrService;
    private readonly ExcelExportService _excelExporter;
    private readonly PdfExportService _pdfExporter;
    
    private string _statusMessage = "Ready";
    private bool _isBusy;
    private bool _isAuthenticated;
    private string _searchText = string.Empty;
    private int _selectedTabIndex;
    private Equipment? _selectedEquipment;
    private Manifest? _selectedManifest;
    private Location? _selectedLocation;
    private EquipmentCategory? _selectedCategory;
    private DashboardStats? _dashboardStats;
    
    // Defect Report fields
    private DefectReport? _selectedDefectReport;
    private string _defectSearchText = string.Empty;
    private string _selectedDefectStatusFilter = "All Statuses";
    private string _selectedDefectUrgencyFilter = "All Urgencies";
    
    public MainViewModel(LocalDatabaseService dbService, SyncService syncService, AuthenticationService authService)
    {
        _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _qrService = new QRCodeService();
        _excelExporter = new ExcelExportService();
        _pdfExporter = new PdfExportService();
        
        Equipment = new ObservableCollection<Equipment>();
        Manifests = new ObservableCollection<Manifest>();
        Locations = new ObservableCollection<Location>();
        Categories = new ObservableCollection<EquipmentCategory>();
        Projects = new ObservableCollection<Project>();
        Alerts = new ObservableCollection<Alert>();
        ExpiringCertifications = new ObservableCollection<Equipment>();
        DueCalibrations = new ObservableCollection<Equipment>();
        LowStockItems = new ObservableCollection<Equipment>();
        DefectReports = new ObservableCollection<DefectReport>();
        
        // Initialize defect filter options
        DefectStatusOptions = new ObservableCollection<string> { "All Statuses", "Draft", "Submitted", "Under Review", "In Progress", "Resolved", "Closed" };
        DefectUrgencyOptions = new ObservableCollection<string> { "All Urgencies", "High", "Medium", "Low" };
        _selectedDefectStatusFilter = "All Statuses";
        _selectedDefectUrgencyFilter = "All Urgencies";
        
        InitializeCommands();
        InitializeBatchCommands();
        
        _authService.AuthenticationChanged += (s, user) =>
        {
            IsAuthenticated = user != null;
            if (user != null) _ = LoadDataAsync();
        };
        
        _syncService.SyncProgress += (s, e) => StatusMessage = e.Message;
        _syncService.ConflictDetected += (s, conflict) =>
        {
            // Handle conflict - show notification
            StatusMessage = $"Sync conflict detected for {conflict.Conflict.TableName}";
        };
    }
    
    #region Collections
    
    public ObservableCollection<Equipment> Equipment { get; }
    public ObservableCollection<Manifest> Manifests { get; }
    public ObservableCollection<Location> Locations { get; }
    public ObservableCollection<EquipmentCategory> Categories { get; }
    public ObservableCollection<Project> Projects { get; }
    public ObservableCollection<Alert> Alerts { get; }
    public ObservableCollection<Equipment> ExpiringCertifications { get; }
    public ObservableCollection<Equipment> DueCalibrations { get; }
    public ObservableCollection<Equipment> LowStockItems { get; }
    public ObservableCollection<DefectReport> DefectReports { get; }
    
    #endregion
    
    #region Properties
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        set => SetProperty(ref _isAuthenticated, value);
    }
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _ = SearchEquipmentAsync();
        }
    }
    
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }
    
    public Equipment? SelectedEquipment
    {
        get => _selectedEquipment;
        set
        {
            SetProperty(ref _selectedEquipment, value);
            OnPropertyChanged(nameof(HasSelectedEquipment));
        }
    }
    
    public Manifest? SelectedManifest
    {
        get => _selectedManifest;
        set
        {
            SetProperty(ref _selectedManifest, value);
            OnPropertyChanged(nameof(HasSelectedManifest));
        }
    }
    
    public Location? SelectedLocation
    {
        get => _selectedLocation;
        set
        {
            SetProperty(ref _selectedLocation, value);
            _ = FilterByLocationAsync();
        }
    }
    
    public EquipmentCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            SetProperty(ref _selectedCategory, value);
            _ = SearchEquipmentAsync();
        }
    }
    
    public DashboardStats? DashboardStats
    {
        get => _dashboardStats;
        set => SetProperty(ref _dashboardStats, value);
    }
    
    public bool HasSelectedEquipment => SelectedEquipment != null;
    public bool HasSelectedManifest => SelectedManifest != null;
    public bool HasSelectedDefect => SelectedDefectReport != null;
    public int PendingSyncCount => _syncService.PendingChanges;
    public string LastSyncDisplay => _syncService.LastSyncTime?.ToString("g") ?? "Never";
    public bool IsSyncing => _syncService.IsSyncing;
    
    // Defect Report Properties
    public DefectReport? SelectedDefectReport
    {
        get => _selectedDefectReport;
        set
        {
            SetProperty(ref _selectedDefectReport, value);
            OnPropertyChanged(nameof(HasSelectedDefect));
        }
    }
    
    public string DefectSearchText
    {
        get => _defectSearchText;
        set
        {
            if (SetProperty(ref _defectSearchText, value))
                _ = SearchDefectsAsync();
        }
    }
    
    public ObservableCollection<string> DefectStatusOptions { get; }
    public ObservableCollection<string> DefectUrgencyOptions { get; }
    
    public string SelectedDefectStatusFilter
    {
        get => _selectedDefectStatusFilter;
        set
        {
            if (SetProperty(ref _selectedDefectStatusFilter, value))
                _ = SearchDefectsAsync();
        }
    }
    
    public string SelectedDefectUrgencyFilter
    {
        get => _selectedDefectUrgencyFilter;
        set
        {
            if (SetProperty(ref _selectedDefectUrgencyFilter, value))
                _ = SearchDefectsAsync();
        }
    }
    
    // Status filter options
    public ObservableCollection<string> StatusOptions { get; } = new()
    {
        "All Statuses",
        "Available",
        "In Use",
        "In Transit",
        "Under Repair",
        "Retired"
    };
    
    private string _selectedStatusFilter = "All Statuses";
    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                UpdateStatusFilters();
                _ = SearchEquipmentAsync();
            }
        }
    }
    
    private void UpdateStatusFilters()
    {
        switch (_selectedStatusFilter)
        {
            case "Available":
                FilterAvailable = true; FilterInUse = false; FilterInTransit = false; FilterUnderRepair = false; FilterRetired = false;
                break;
            case "In Use":
                FilterAvailable = false; FilterInUse = true; FilterInTransit = false; FilterUnderRepair = false; FilterRetired = false;
                break;
            case "In Transit":
                FilterAvailable = false; FilterInUse = false; FilterInTransit = true; FilterUnderRepair = false; FilterRetired = false;
                break;
            case "Under Repair":
                FilterAvailable = false; FilterInUse = false; FilterInTransit = false; FilterUnderRepair = true; FilterRetired = false;
                break;
            case "Retired":
                FilterAvailable = false; FilterInUse = false; FilterInTransit = false; FilterUnderRepair = false; FilterRetired = true;
                break;
            default: // All Statuses
                FilterAvailable = true; FilterInUse = true; FilterInTransit = true; FilterUnderRepair = true; FilterRetired = false;
                break;
        }
    }

    // New properties for modern UI
    private bool _isDarkTheme = true;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => SetProperty(ref _isDarkTheme, value);
    }
    
    public string CurrentUserInitials
    {
        get
        {
            var user = _authService.CurrentUser;
            if (user == null) return "?";
            
            // Split and filter out empty strings to handle double spaces or leading/trailing spaces
            var parts = user.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length >= 1 && parts[0].Length >= 2)
                return parts[0].Substring(0, 2).ToUpper();
            if (parts.Length >= 1 && parts[0].Length == 1)
                return parts[0].ToUpper();
            return user.Username?.Substring(0, Math.Min(2, user.Username?.Length ?? 0)).ToUpper() ?? "?";
        }
    }
    
    private bool _hasNotifications;
    public bool HasNotifications
    {
        get => _hasNotifications;
        set => SetProperty(ref _hasNotifications, value);
    }
    
    private object? _currentPage;
    public object? CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }
    
    // Filter properties for equipment list
    private bool _filterAvailable = true;
    public bool FilterAvailable { get => _filterAvailable; set { SetProperty(ref _filterAvailable, value); _ = SearchEquipmentAsync(); } }
    
    private bool _filterInUse = true;
    public bool FilterInUse { get => _filterInUse; set { SetProperty(ref _filterInUse, value); _ = SearchEquipmentAsync(); } }
    
    private bool _filterInTransit = true;
    public bool FilterInTransit { get => _filterInTransit; set { SetProperty(ref _filterInTransit, value); _ = SearchEquipmentAsync(); } }
    
    private bool _filterUnderRepair = true;
    public bool FilterUnderRepair { get => _filterUnderRepair; set { SetProperty(ref _filterUnderRepair, value); _ = SearchEquipmentAsync(); } }
    
    private bool _filterRetired = false;
    public bool FilterRetired { get => _filterRetired; set { SetProperty(ref _filterRetired, value); _ = SearchEquipmentAsync(); } }
    
    private bool _filterExpiringCert;
    public bool FilterExpiringCert { get => _filterExpiringCert; set { SetProperty(ref _filterExpiringCert, value); _ = SearchEquipmentAsync(); } }
    
    private bool _filterExpiredCert;
    public bool FilterExpiredCert { get => _filterExpiredCert; set { SetProperty(ref _filterExpiredCert, value); _ = SearchEquipmentAsync(); } }
    
    public ICommand ClearFiltersCommand { get; private set; } = null!;
    
    #endregion
    
    #region Commands
    
    public ICommand RefreshCommand { get; private set; } = null!;
    public ICommand SyncCommand { get; private set; } = null!;
    public ICommand NewEquipmentCommand { get; private set; } = null!;
    public ICommand EditEquipmentCommand { get; private set; } = null!;
    public ICommand DeleteEquipmentCommand { get; private set; } = null!;
    public ICommand ViewHistoryCommand { get; private set; } = null!;
    public ICommand CopyUniqueIdCommand { get; private set; } = null!;
    public ICommand NewOutwardManifestCommand { get; private set; } = null!;
    public ICommand NewInwardManifestCommand { get; private set; } = null!;
    public ICommand VerifyShipmentCommand { get; private set; } = null!;
    public ICommand ViewManifestCommand { get; private set; } = null!;
    public ICommand ExportEquipmentExcelCommand { get; private set; } = null!;
    public ICommand ExportEquipmentPdfCommand { get; private set; } = null!;
    public ICommand ExportManifestPdfCommand { get; private set; } = null!;
    public ICommand ImportFromFileCommand { get; private set; } = null!;
    public ICommand ImportConfigurationCommand { get; private set; } = null!;
    public ICommand GenerateQrCodeCommand { get; private set; } = null!;
    public ICommand PrintQrLabelsCommand { get; private set; } = null!;
    public ICommand SettingsCommand { get; private set; } = null!;
    public ICommand LoginCommand { get; private set; } = null!;
    public ICommand LogoutCommand { get; private set; } = null!;
    public ICommand ExportCertificationsReportCommand { get; private set; } = null!;
    public ICommand ExportCalibrationsReportCommand { get; private set; } = null!;
    public ICommand ExportManifestHistoryReportCommand { get; private set; } = null!;
    public ICommand ExportLocationSummaryReportCommand { get; private set; } = null!;
    public ICommand ExportAssetMovementReportCommand { get; private set; } = null!;
    public ICommand OpenReportBuilderCommand { get; private set; } = null!;
    
    // Defect Report Commands
    public ICommand RefreshDefectsCommand { get; private set; } = null!;
    public ICommand NewDefectReportCommand { get; private set; } = null!;
    public ICommand EditDefectReportCommand { get; private set; } = null!;
    public ICommand SubmitDefectReportCommand { get; private set; } = null!;
    public ICommand ResolveDefectReportCommand { get; private set; } = null!;
    public ICommand PrintDefectReportCommand { get; private set; } = null!;
    public ICommand ExportDefectsCommand { get; private set; } = null!;
    public ICommand ClearDefectFiltersCommand { get; private set; } = null!;
    
    private void InitializeCommands()
    {
        RefreshCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
        SyncCommand = new AsyncRelayCommand(async _ => await SyncAsync(), _ => !IsBusy && !IsSyncing);
        NewEquipmentCommand = new RelayCommand(_ => ShowNewEquipmentDialog());
        EditEquipmentCommand = new RelayCommand(_ => ShowEditEquipmentDialog(), _ => HasSelectedEquipment);
        DeleteEquipmentCommand = new AsyncRelayCommand(async _ => await DeleteSelectedEquipmentAsync(), _ => HasSelectedEquipment);
        ViewHistoryCommand = new RelayCommand(_ => ShowEquipmentHistory(), _ => HasSelectedEquipment);
        CopyUniqueIdCommand = new RelayCommand(_ => CopyUniqueIdToClipboard(), _ => HasSelectedEquipment && !string.IsNullOrEmpty(SelectedEquipment?.UniqueId));
        NewOutwardManifestCommand = new RelayCommand(_ => ShowNewManifestDialog(ManifestType.Outward));
        NewInwardManifestCommand = new RelayCommand(_ => ShowNewManifestDialog(ManifestType.Inward));
        VerifyShipmentCommand = new RelayCommand(_ => ShowShipmentVerificationDialog());
        ViewManifestCommand = new RelayCommand(_ => ShowManifestDetails(), _ => HasSelectedManifest);
        ExportEquipmentExcelCommand = new AsyncRelayCommand(async _ => await ExportEquipmentToExcelAsync());
        ExportEquipmentPdfCommand = new AsyncRelayCommand(async _ => await ExportEquipmentToPdfAsync());
        ExportManifestPdfCommand = new AsyncRelayCommand(async _ => await ExportManifestToPdfAsync(), _ => HasSelectedManifest);
        ImportFromFileCommand = new AsyncRelayCommand(async p => await ImportFromFileAsync(p as string));
        ImportConfigurationCommand = new RelayCommand(p => ImportConfiguration(p as string));
        GenerateQrCodeCommand = new RelayCommand(_ => GenerateQrCode(), _ => HasSelectedEquipment);
        PrintQrLabelsCommand = new RelayCommand(_ => PrintQrLabels());
        SettingsCommand = new RelayCommand(_ => ShowSettings());
        LoginCommand = new RelayCommand(_ => ShowLoginDialog());
        LogoutCommand = new RelayCommand(_ => _authService.Logout());
        ExportCertificationsReportCommand = new AsyncRelayCommand(async _ => await ExportCertificationsReportAsync());
        ExportCalibrationsReportCommand = new AsyncRelayCommand(async _ => await ExportCalibrationsReportAsync());
        ExportManifestHistoryReportCommand = new AsyncRelayCommand(async _ => await ExportManifestHistoryReportAsync());
        ExportLocationSummaryReportCommand = new AsyncRelayCommand(async _ => await ExportLocationSummaryReportAsync());
        ExportAssetMovementReportCommand = new AsyncRelayCommand(async _ => await ExportAssetMovementReportAsync());
        OpenReportBuilderCommand = new RelayCommand(_ => OpenReportBuilder());
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
        
        // Defect Report Commands
        RefreshDefectsCommand = new AsyncRelayCommand(async _ => await LoadDefectsAsync());
        NewDefectReportCommand = new RelayCommand(_ => ShowNewDefectReportDialog());
        EditDefectReportCommand = new RelayCommand(_ => ShowEditDefectReportDialog(), _ => HasSelectedDefect);
        SubmitDefectReportCommand = new AsyncRelayCommand(async _ => await SubmitDefectReportAsync(), _ => HasSelectedDefect && (SelectedDefectReport?.CanSubmit ?? false));
        ResolveDefectReportCommand = new AsyncRelayCommand(async _ => await ResolveDefectReportAsync(), _ => HasSelectedDefect && (SelectedDefectReport?.CanResolve ?? false));
        PrintDefectReportCommand = new RelayCommand(_ => PrintDefectReport(), _ => HasSelectedDefect);
        ExportDefectsCommand = new AsyncRelayCommand(async _ => await ExportDefectsToExcelAsync());
        ClearDefectFiltersCommand = new RelayCommand(_ => ClearDefectFilters());
    }
    
    private void ClearFilters()
    {
        FilterAvailable = true;
        FilterInUse = true;
        FilterInTransit = true;
        FilterUnderRepair = true;
        FilterRetired = false;
        FilterExpiringCert = false;
        FilterExpiredCert = false;
        SelectedCategory = null;
        SelectedLocation = Locations.FirstOrDefault();
        SearchText = "";
        SelectedStatusFilter = "All Statuses";
    }
    
    private void ShowEquipmentHistory()
    {
        if (SelectedEquipment == null) return;
        
        try
        {
            var historyWindow = new Views.EquipmentHistoryView(
                SelectedEquipment.EquipmentId, 
                SelectedEquipment.AssetNumber, 
                SelectedEquipment.Name);
            historyWindow.Owner = Application.Current.MainWindow;
            
            var settings = Services.ModuleSettings.Load();
            Services.ThemeService.Instance.ApplyTheme(historyWindow, settings.UseDarkTheme);
            
            historyWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error showing history: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void CopyUniqueIdToClipboard()
    {
        if (SelectedEquipment?.UniqueId == null) return;
        
        try
        {
            System.Windows.Clipboard.SetText(SelectedEquipment.UniqueId);
            StatusMessage = $"Copied: {SelectedEquipment.UniqueId}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to copy: {ex.Message}";
        }
    }
    
    #endregion
    
    #region Data Operations
    
    public async Task LoadDataAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Loading data...";
        
        try
        {
            // Load lookups first
            var locations = await _dbService.GetLocationsAsync();
            Locations.Clear();
            Locations.Add(new Location { LocationId = Guid.Empty, Name = "All Locations", Code = "ALL" });
            foreach (var l in locations) Locations.Add(l);
            
            var categories = await _dbService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var c in categories) Categories.Add(c);
            
            var projects = await _dbService.GetProjectsAsync();
            Projects.Clear();
            foreach (var p in projects) Projects.Add(p);
            
            // Load equipment
            var equipment = await _dbService.GetEquipmentAsync(
                locationId: SelectedLocation?.LocationId == Guid.Empty ? null : SelectedLocation?.LocationId,
                categoryId: SelectedCategory?.CategoryId);
            Equipment.Clear();
            foreach (var e in equipment) Equipment.Add(e);
            
            // Load manifests
            var manifests = await _dbService.GetManifestsAsync();
            Manifests.Clear();
            foreach (var m in manifests) Manifests.Add(m);
            
            // Load alerts
            var expiringCerts = await _dbService.GetExpiringCertificationsAsync();
            ExpiringCertifications.Clear();
            foreach (var e in expiringCerts) ExpiringCertifications.Add(e);
            
            var dueCalibs = await _dbService.GetDueCalibrationAsync();
            DueCalibrations.Clear();
            foreach (var e in dueCalibs) DueCalibrations.Add(e);
            
            var lowStock = await _dbService.GetLowStockItemsAsync();
            LowStockItems.Clear();
            foreach (var e in lowStock) LowStockItems.Add(e);
            
            // Load dashboard stats
            DashboardStats = await _dbService.GetDashboardStatsAsync(
                SelectedLocation?.LocationId == Guid.Empty ? null : SelectedLocation?.LocationId);
            
            StatusMessage = $"Loaded {Equipment.Count} equipment, {Manifests.Count} manifests";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"LoadDataAsync error: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task SearchEquipmentAsync()
    {
        if (IsBusy) return;
        
        try
        {
            var results = await _dbService.GetEquipmentAsync(
                locationId: SelectedLocation?.LocationId == Guid.Empty ? null : SelectedLocation?.LocationId,
                categoryId: SelectedCategory?.CategoryId,
                search: SearchText);
            
            Equipment.Clear();
            foreach (var e in results) Equipment.Add(e);
            StatusMessage = $"Found {Equipment.Count} items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search failed: {ex.Message}";
        }
    }
    
    private async Task FilterByLocationAsync()
    {
        await SearchEquipmentAsync();
        
        // Update stats for selected location
        DashboardStats = await _dbService.GetDashboardStatsAsync(
            SelectedLocation?.LocationId == Guid.Empty ? null : SelectedLocation?.LocationId);
    }
    
    private async Task SyncAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _syncService.SyncAsync();
            if (result.Success)
            {
                StatusMessage = $"Sync complete: {result.ItemsPulled} pulled, {result.ItemsPushed} pushed";
                if (result.Conflicts.Count > 0)
                {
                    StatusMessage += $", {result.Conflicts.Count} conflicts";
                }
                await LoadDataAsync();
            }
            else
            {
                StatusMessage = result.Error ?? "Sync failed";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sync failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(PendingSyncCount));
            OnPropertyChanged(nameof(LastSyncDisplay));
            OnPropertyChanged(nameof(IsSyncing));
        }
    }
    
    private async Task DeleteSelectedEquipmentAsync()
    {
        if (SelectedEquipment == null) return;
        
        var result = MessageBox.Show(
            $"Are you sure you want to delete '{SelectedEquipment.Name}'?\n\nAsset: {SelectedEquipment.AssetNumber}",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            await _dbService.DeleteEquipmentAsync(SelectedEquipment.EquipmentId);
            Equipment.Remove(SelectedEquipment);
            SelectedEquipment = null;
            StatusMessage = "Equipment deleted";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
        }
    }
    
    #endregion
    
    #region Export Operations
    
    private async Task ExportEquipmentToExcelAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Equipment Register",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"Equipment_Register_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        IsBusy = true;
        StatusMessage = "Exporting to Excel...";
        
        try
        {
            await Task.Run(() =>
            {
                var items = Equipment.ToList();
                _excelExporter.ExportEquipmentRegister(dialog.FileName, items);
            });
            
            StatusMessage = $"Exported {Equipment.Count} items to Excel";
            
            if (MessageBox.Show("Export complete. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportEquipmentToPdfAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Equipment Register PDF",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"Equipment_Register_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        IsBusy = true;
        StatusMessage = "Generating PDF...";
        
        try
        {
            await Task.Run(() =>
            {
                var items = Equipment.ToList();
                _pdfExporter.GenerateEquipmentRegister(dialog.FileName, items);
            });
            
            StatusMessage = "PDF generated successfully";
            
            if (MessageBox.Show("PDF generated. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"PDF generation failed: {ex.Message}";
            MessageBox.Show($"Failed to generate PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportManifestToPdfAsync()
    {
        if (SelectedManifest == null) return;
        
        var dialog = new SaveFileDialog
        {
            Title = "Export Manifest",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"Manifest_{SelectedManifest.ManifestNumber}_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        IsBusy = true;
        StatusMessage = "Generating manifest PDF...";
        
        try
        {
            // Get full manifest with items
            var manifest = await _dbService.GetManifestByIdAsync(SelectedManifest.ManifestId);
            if (manifest == null)
            {
                StatusMessage = "Manifest not found";
                return;
            }
            
            await Task.Run(() =>
            {
                _pdfExporter.GenerateManifest(dialog.FileName, manifest);
            });
            
            StatusMessage = "Manifest PDF generated";
            
            if (MessageBox.Show("PDF generated. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"PDF generation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportCertificationsReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Certifications Due Report",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"Certifications_Due_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        IsBusy = true;
        try
        {
            var items = await _dbService.GetExpiringCertificationsAsync(90);
            await Task.Run(() => _excelExporter.ExportDueReport(dialog.FileName, items, "Certification"));
            StatusMessage = $"Exported {items.Count} items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportCalibrationsReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Calibrations Due Report",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"Calibrations_Due_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        IsBusy = true;
        try
        {
            var items = await _dbService.GetDueCalibrationAsync(30);
            await Task.Run(() => _excelExporter.ExportDueReport(dialog.FileName, items, "Calibration"));
            StatusMessage = $"Exported {items.Count} items";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportManifestHistoryReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Manifest History Report",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"Manifest_History_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        IsBusy = true;
        try
        {
            var manifests = await _dbService.GetManifestsAsync();
            await Task.Run(() => _excelExporter.ExportManifestHistory(dialog.FileName, manifests));
            StatusMessage = $"Exported {manifests.Count} manifests";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportLocationSummaryReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Location Summary Report",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"Location_Summary_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        IsBusy = true;
        try
        {
            var locations = await _dbService.GetAllLocationsAsync();
            var equipment = await _dbService.GetAllEquipmentAsync();
            await Task.Run(() => _excelExporter.ExportLocationSummary(dialog.FileName, locations, equipment));
            StatusMessage = $"Exported {locations.Count} locations";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ExportAssetMovementReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Asset Movement Report",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"Asset_Movement_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        IsBusy = true;
        try
        {
            var history = await _dbService.GetEquipmentHistoryAsync(null, 1000);
            await Task.Run(() => _excelExporter.ExportAssetMovement(dialog.FileName, history));
            StatusMessage = $"Exported {history.Count} movements";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void OpenReportBuilder()
    {
        try
        {
            var reportBuilder = new Views.ReportBuilderView();
            reportBuilder.Owner = Application.Current.MainWindow;
            
            var settings = Services.ModuleSettings.Load();
            Services.ThemeService.Instance.ApplyTheme(reportBuilder, settings.UseDarkTheme);
            
            reportBuilder.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening Report Builder: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task ImportFromFileAsync(string? filePath)
    {
        var dialog = new Views.Dialogs.ImportDialog(_dbService);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true && dialog.ImportedCount > 0)
        {
            StatusMessage = $"Imported {dialog.ImportedCount} items";
            await LoadDataAsync();
        }
    }
    
    #endregion
    
    #region UI Operations
    
    private void ShowNewEquipmentDialog()
    {
        var dialog = new Views.Dialogs.EquipmentEditorDialog(_dbService);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            var equipment = dialog.GetEquipment();
            if (equipment != null)
            {
                Equipment.Insert(0, equipment);
                SelectedEquipment = equipment;
                StatusMessage = $"Created: {equipment.AssetNumber}";
            }
        }
    }
    
    private void ShowEditEquipmentDialog()
    {
        if (SelectedEquipment == null) return;
        
        var dialog = new Views.Dialogs.EquipmentEditorDialog(_dbService, SelectedEquipment);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            var updated = dialog.GetEquipment();
            if (updated != null)
            {
                var index = Equipment.IndexOf(SelectedEquipment);
                if (index >= 0)
                {
                    Equipment[index] = updated;
                    SelectedEquipment = updated;
                }
                StatusMessage = $"Updated: {updated.AssetNumber}";
            }
        }
    }
    
    private void ShowNewManifestDialog(ManifestType type)
    {
        var dialog = new Views.Dialogs.ManifestWizardDialog(_dbService, type);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            var manifest = dialog.GetManifest();
            if (manifest != null)
            {
                Manifests.Insert(0, manifest);
                SelectedManifest = manifest;
                StatusMessage = $"Created manifest: {manifest.ManifestNumber}";
            }
        }
    }
    
    private void ShowManifestDetails()
    {
        if (SelectedManifest == null) return;
        // For now, export the manifest to PDF for viewing
        _ = ExportManifestToPdfAsync();
    }
    
    private void ShowShipmentVerificationDialog()
    {
        var vm = new ViewModels.Dialogs.ShipmentVerificationViewModel(_dbService, _authService);
        var dialog = new Views.Dialogs.ShipmentVerificationDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };
        
        if (dialog.ShowDialog() == true)
        {
            // Refresh data after verification
            _ = LoadDataAsync();
            StatusMessage = "Shipment verification completed";
        }
    }
    
    private void ShowShipmentVerificationDialog(Manifest manifest)
    {
        var vm = new ViewModels.Dialogs.ShipmentVerificationViewModel(_dbService, _authService, manifest);
        var dialog = new Views.Dialogs.ShipmentVerificationDialog
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };
        
        if (dialog.ShowDialog() == true)
        {
            _ = LoadDataAsync();
            StatusMessage = "Shipment verification completed";
        }
    }
    
    private void GenerateQrCode()
    {
        if (SelectedEquipment == null) return;
        
        var dialog = new SaveFileDialog
        {
            Title = "Save QR Code",
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"QR_{SelectedEquipment.AssetNumber}"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            var qrContent = QRCodeService.GenerateEquipmentQrCode(SelectedEquipment.AssetNumber);
            _qrService.SaveQrCodeToFile(qrContent, dialog.FileName, 15);
            StatusMessage = $"QR code saved: {dialog.FileName}";
            
            if (MessageBox.Show("QR code saved. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to generate QR: {ex.Message}";
        }
    }
    
    private void PrintQrLabels()
    {
        var dialog = new Views.Dialogs.QrLabelPrintDialog(_dbService);
        dialog.Owner = Application.Current.MainWindow;
        dialog.ShowDialog();
    }
    
    private void ShowSettings()
    {
        var dialog = new Views.Dialogs.SettingsDialog();
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            StatusMessage = "Settings saved";
            // Reload any settings-dependent data
        }
    }
    
    private void ShowLoginDialog()
    {
        var dialog = new Views.Dialogs.LoginDialog(_authService);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true)
        {
            StatusMessage = "Login successful";
            _ = LoadDataAsync();
        }
    }
    
    private async void ImportConfiguration(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) 
        {
            // Open file dialog if no path provided
            var openDialog = new OpenFileDialog
            {
                Title = "Import Configuration",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*"
            };
            
            if (openDialog.ShowDialog() != true)
                return;
            
            filePath = openDialog.FileName;
        }
        
        try
        {
            IsBusy = true;
            StatusMessage = "Importing configuration...";
            
            var json = await File.ReadAllTextAsync(filePath);
            var config = System.Text.Json.JsonSerializer.Deserialize<ModuleConfigurationImport>(json);
            
            if (config == null)
            {
                MessageBox.Show("Invalid configuration file.", "Import Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            int imported = 0;
            
            // Import categories
            if (config.Categories?.Any() == true)
            {
                foreach (var cat in config.Categories)
                {
                    var existing = await _dbService.GetCategoryByNameAsync(cat.Name);
                    if (existing == null)
                    {
                        await _dbService.SaveCategoryAsync(new EquipmentCategory
                        {
                            Name = cat.Name,
                            Description = cat.Description,
                            ParentCategoryId = null
                        });
                        imported++;
                    }
                }
            }
            
            // Import locations
            if (config.Locations?.Any() == true)
            {
                foreach (var loc in config.Locations)
                {
                    var existing = await _dbService.GetLocationByNameAsync(loc.Name);
                    if (existing == null)
                    {
                        await _dbService.AddLocationAsync(new Location
                        {
                            Name = loc.Name,
                            Code = loc.Code,
                            IsActive = true
                        });
                        imported++;
                    }
                }
            }
            
            await LoadDataAsync();
            StatusMessage = $"Configuration imported: {imported} items added";
            
            MessageBox.Show($"Configuration imported successfully.\n{imported} new items added.", 
                "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error importing configuration: {ex.Message}", "Import Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Import failed";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    #endregion
    
    #region Defect Reports (EFN)
    
    public async Task LoadDefectsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading defect reports...";
            
            DefectReportStatus? status = SelectedDefectStatusFilter switch
            {
                "Draft" => DefectReportStatus.Draft,
                "Submitted" => DefectReportStatus.Submitted,
                "Under Review" => DefectReportStatus.UnderReview,
                "In Progress" => DefectReportStatus.InProgress,
                "Resolved" => DefectReportStatus.Resolved,
                "Closed" => DefectReportStatus.Closed,
                _ => null
            };
            
            ReplacementUrgency? urgency = SelectedDefectUrgencyFilter switch
            {
                "High" => ReplacementUrgency.High,
                "Medium" => ReplacementUrgency.Medium,
                "Low" => ReplacementUrgency.Low,
                _ => null
            };
            
            var defects = await _dbService.GetDefectReportsAsync(
                status: status,
                urgency: urgency,
                searchText: DefectSearchText);
            
            DefectReports.Clear();
            foreach (var defect in defects)
                DefectReports.Add(defect);
            
            StatusMessage = $"Loaded {defects.Count} defect reports";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading defects: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task SearchDefectsAsync()
    {
        await LoadDefectsAsync();
    }
    
    private void ClearDefectFilters()
    {
        DefectSearchText = "";
        SelectedDefectStatusFilter = "All Statuses";
        SelectedDefectUrgencyFilter = "All Urgencies";
        _ = LoadDefectsAsync();
    }
    
    private void ShowNewDefectReportDialog()
    {
        var dialog = new Views.Dialogs.DefectReportEditorDialog(_dbService, null, _authService.CurrentUser?.UserId ?? Guid.Empty);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true && dialog.SavedReport != null)
        {
            DefectReports.Insert(0, dialog.SavedReport);
            SelectedDefectReport = dialog.SavedReport;
            StatusMessage = $"Created defect report {dialog.SavedReport.ReportNumber}";
        }
    }
    
    private void ShowEditDefectReportDialog()
    {
        if (SelectedDefectReport == null) return;
        
        var dialog = new Views.Dialogs.DefectReportEditorDialog(_dbService, SelectedDefectReport, _authService.CurrentUser?.UserId ?? Guid.Empty);
        dialog.Owner = Application.Current.MainWindow;
        
        if (dialog.ShowDialog() == true && dialog.SavedReport != null)
        {
            var index = DefectReports.IndexOf(SelectedDefectReport);
            if (index >= 0)
            {
                DefectReports[index] = dialog.SavedReport;
                SelectedDefectReport = dialog.SavedReport;
            }
            StatusMessage = $"Updated defect report {dialog.SavedReport.ReportNumber}";
        }
    }
    
    private async Task SubmitDefectReportAsync()
    {
        if (SelectedDefectReport == null || !SelectedDefectReport.CanSubmit) return;
        
        var result = MessageBox.Show(
            $"Submit defect report {SelectedDefectReport.ReportNumber} for review?",
            "Confirm Submit",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            IsBusy = true;
            await _dbService.SubmitDefectReportAsync(
                SelectedDefectReport.DefectReportId,
                _authService.CurrentUser?.UserId ?? Guid.Empty);
            
            // Reload the report
            var updated = await _dbService.GetDefectReportByIdAsync(SelectedDefectReport.DefectReportId);
            if (updated != null)
            {
                var index = DefectReports.IndexOf(SelectedDefectReport);
                if (index >= 0)
                {
                    DefectReports[index] = updated;
                    SelectedDefectReport = updated;
                }
            }
            
            StatusMessage = $"Submitted {SelectedDefectReport.ReportNumber}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error submitting report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task ResolveDefectReportAsync()
    {
        if (SelectedDefectReport == null || !SelectedDefectReport.CanResolve) return;
        
        // TODO: Show resolution dialog to capture resolution notes
        var result = MessageBox.Show(
            $"Mark defect report {SelectedDefectReport.ReportNumber} as resolved?",
            "Confirm Resolve",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        try
        {
            IsBusy = true;
            await _dbService.ResolveDefectReportAsync(
                SelectedDefectReport.DefectReportId,
                _authService.CurrentUser?.UserId ?? Guid.Empty,
                "Resolved by user");
            
            // Reload the report
            var updated = await _dbService.GetDefectReportByIdAsync(SelectedDefectReport.DefectReportId);
            if (updated != null)
            {
                var index = DefectReports.IndexOf(SelectedDefectReport);
                if (index >= 0)
                {
                    DefectReports[index] = updated;
                    SelectedDefectReport = updated;
                }
            }
            
            StatusMessage = $"Resolved {SelectedDefectReport.ReportNumber}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error resolving report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void PrintDefectReport()
    {
        if (SelectedDefectReport == null) return;
        
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Save Defect Report",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"EFN_{SelectedDefectReport.ReportNumber}_{DateTime.Now:yyyyMMdd}"
            };
            
            if (saveDialog.ShowDialog() != true) return;
            
            var pdfService = new Export.PdfExportService();
            pdfService.GenerateDefectReport(saveDialog.FileName, SelectedDefectReport);
            
            StatusMessage = $"Defect report saved: {saveDialog.FileName}";
            
            // Ask to open the file
            var result = MessageBox.Show(
                "Defect report generated. Would you like to open it?",
                "Report Generated",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = saveDialog.FileName,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private async Task ExportDefectsToExcelAsync()
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Export Defect Reports",
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"DefectReports_{DateTime.Now:yyyyMMdd}"
            };
            
            if (saveDialog.ShowDialog() != true) return;
            
            IsBusy = true;
            StatusMessage = "Exporting defect reports...";
            
            // Get all defects for export
            var defects = await _dbService.GetDefectReportsAsync(take: 1000);
            
            // Export to Excel using ExcelExportService
            await Task.Run(() => _excelExporter.ExportDefectReports(defects, saveDialog.FileName));
            
            StatusMessage = $"Exported {defects.Count} defect reports";
            
            // Open file
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = saveDialog.FileName,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    #endregion
}

/// <summary>
/// Helper class for configuration import
/// </summary>
public class ModuleConfigurationImport
{
    public List<CategoryImport>? Categories { get; set; }
    public List<LocationImport>? Locations { get; set; }
    public List<TypeImport>? EquipmentTypes { get; set; }
}

public class CategoryImport
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentCategory { get; set; }
}

public class LocationImport
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Type { get; set; }
}

public class TypeImport
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
}
