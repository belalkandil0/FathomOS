using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.Services;


namespace FathomOS.Modules.EquipmentInventory.ViewModels.Dialogs;

public class SettingsViewModel : ViewModelBase
{
    private readonly ModuleSettings _settings;
    private readonly LabelPrintService _printService;
    
    // Connection settings
    private string _apiBaseUrl;
    private bool _useDarkTheme;
    private int _syncIntervalMinutes;
    private bool _autoSyncEnabled;
    private bool _showNotifications;
    private string _defaultLocationCode;
    
    // Organization settings (NEW)
    private string _organizationName;
    private string _organizationCode;
    private bool _showOrganizationOnLabel;
    private bool _showUniqueIdOnLabel;
    private bool _autoGenerateUniqueId;
    private string _labelPreset;
    
    // Printer settings
    private PrinterInfo? _selectedPrinter;
    private LabelPrinterType _selectedPrinterType;
    private LabelSize _selectedLabelSize;
    private int _printerDpi;
    private int _defaultCopies;
    private bool _showPrintPreview;
    private bool _autoPrintAfterSave;
    private double _customLabelWidth;
    private double _customLabelHeight;
    
    private bool? _dialogResult;
    private string _statusMessage = string.Empty;
    
    public SettingsViewModel()
    {
        _settings = ModuleSettings.Load();
        _printService = new LabelPrintService(_settings);
        
        // Load current settings
        _apiBaseUrl = _settings.ApiBaseUrl;
        _useDarkTheme = _settings.UseDarkTheme;
        _syncIntervalMinutes = _settings.SyncIntervalMinutes;
        _autoSyncEnabled = _settings.AutoSyncEnabled;
        _showNotifications = _settings.ShowNotifications;
        _defaultLocationCode = _settings.DefaultLocationCode ?? string.Empty;
        
        // Load Organization settings
        _organizationName = _settings.OrganizationName;
        _organizationCode = _settings.OrganizationCode;
        _showOrganizationOnLabel = _settings.ShowOrganizationOnLabel;
        _showUniqueIdOnLabel = _settings.ShowUniqueIdOnLabel;
        _autoGenerateUniqueId = _settings.AutoGenerateUniqueId;
        _labelPreset = _settings.LabelPreset;
        
        // Load Printer settings
        _selectedPrinterType = _settings.PrinterSettings.PrinterType;
        _selectedLabelSize = _settings.PrinterSettings.LabelSize;
        _printerDpi = _settings.PrinterSettings.DPI;
        _defaultCopies = _settings.PrinterSettings.DefaultCopies;
        _showPrintPreview = _settings.PrinterSettings.ShowPreview;
        _autoPrintAfterSave = _settings.PrinterSettings.AutoPrint;
        _customLabelWidth = _settings.PrinterSettings.CustomWidthMm;
        _customLabelHeight = _settings.PrinterSettings.CustomHeightMm;
        
        // Load available printers
        AvailablePrinters = new ObservableCollection<PrinterInfo>(_printService.GetAvailablePrinters());
        _selectedPrinter = AvailablePrinters.FirstOrDefault(p => p.Name == _settings.PrinterSettings.PrinterName) 
                          ?? AvailablePrinters.FirstOrDefault(p => p.IsDefault);
        
        // Initialize commands
        SaveCommand = new RelayCommand(_ => Save(), _ => !string.IsNullOrWhiteSpace(ApiBaseUrl));
        CancelCommand = new RelayCommand(_ => Cancel());
        TestConnectionCommand = new AsyncRelayCommand(async _ => await TestConnectionAsync());
        ResetToDefaultsCommand = new RelayCommand(_ => ResetToDefaults());
        RefreshPrintersCommand = new RelayCommand(_ => RefreshPrinters());
        TestPrintCommand = new RelayCommand(_ => TestPrint(), _ => SelectedPrinter != null);
    }
    
    #region Connection Properties
    
    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }
    
    public bool UseDarkTheme
    {
        get => _useDarkTheme;
        set => SetProperty(ref _useDarkTheme, value);
    }
    
    public int SyncIntervalMinutes
    {
        get => _syncIntervalMinutes;
        set => SetProperty(ref _syncIntervalMinutes, Math.Max(1, Math.Min(60, value)));
    }
    
    public bool AutoSyncEnabled
    {
        get => _autoSyncEnabled;
        set => SetProperty(ref _autoSyncEnabled, value);
    }
    
    public bool ShowNotifications
    {
        get => _showNotifications;
        set => SetProperty(ref _showNotifications, value);
    }
    
    public string DefaultLocationCode
    {
        get => _defaultLocationCode;
        set => SetProperty(ref _defaultLocationCode, value);
    }
    
    #endregion
    
    #region Organization Properties (for QR Labels)
    
    /// <summary>
    /// Organization name displayed on QR labels (e.g., "subsea 7", "S7 Solutions")
    /// </summary>
    public string OrganizationName
    {
        get => _organizationName;
        set => SetProperty(ref _organizationName, value);
    }
    
    /// <summary>
    /// Short organization code used in unique IDs (e.g., "S7" for S7WSS04068)
    /// </summary>
    public string OrganizationCode
    {
        get => _organizationCode;
        set => SetProperty(ref _organizationCode, value?.ToUpper() ?? string.Empty);
    }
    
    /// <summary>
    /// Show organization name on printed labels
    /// </summary>
    public bool ShowOrganizationOnLabel
    {
        get => _showOrganizationOnLabel;
        set => SetProperty(ref _showOrganizationOnLabel, value);
    }
    
    /// <summary>
    /// Show unique ID below QR code on labels
    /// </summary>
    public bool ShowUniqueIdOnLabel
    {
        get => _showUniqueIdOnLabel;
        set => SetProperty(ref _showUniqueIdOnLabel, value);
    }
    
    /// <summary>
    /// Auto-generate unique ID when creating new equipment
    /// </summary>
    public bool AutoGenerateUniqueId
    {
        get => _autoGenerateUniqueId;
        set => SetProperty(ref _autoGenerateUniqueId, value);
    }
    
    /// <summary>
    /// Label size preset (Small, Standard, Large, ExtraLarge)
    /// </summary>
    public string LabelPreset
    {
        get => _labelPreset;
        set => SetProperty(ref _labelPreset, value);
    }
    
    /// <summary>
    /// Available label presets for dropdown
    /// </summary>
    public List<string> LabelPresets => new() { "Small", "Standard", "Large", "ExtraLarge" };
    
    #endregion
    
    #region Printer Properties
    
    public ObservableCollection<PrinterInfo> AvailablePrinters { get; }
    
    public PrinterInfo? SelectedPrinter
    {
        get => _selectedPrinter;
        set
        {
            if (SetProperty(ref _selectedPrinter, value) && value != null)
            {
                // Auto-detect printer type when printer changes
                SelectedPrinterType = value.Type;
            }
        }
    }
    
    public LabelPrinterType SelectedPrinterType
    {
        get => _selectedPrinterType;
        set => SetProperty(ref _selectedPrinterType, value);
    }
    
    public LabelSize SelectedLabelSize
    {
        get => _selectedLabelSize;
        set
        {
            SetProperty(ref _selectedLabelSize, value);
            OnPropertyChanged(nameof(IsCustomLabelSize));
        }
    }
    
    public bool IsCustomLabelSize => SelectedLabelSize == LabelSize.Custom;
    
    public double CustomLabelWidth
    {
        get => _customLabelWidth;
        set => SetProperty(ref _customLabelWidth, Math.Max(10, Math.Min(200, value)));
    }
    
    public double CustomLabelHeight
    {
        get => _customLabelHeight;
        set => SetProperty(ref _customLabelHeight, Math.Max(10, Math.Min(200, value)));
    }
    
    public int PrinterDpi
    {
        get => _printerDpi;
        set => SetProperty(ref _printerDpi, Math.Max(150, Math.Min(600, value)));
    }
    
    public int DefaultCopies
    {
        get => _defaultCopies;
        set => SetProperty(ref _defaultCopies, Math.Max(1, Math.Min(99, value)));
    }
    
    public bool ShowPrintPreview
    {
        get => _showPrintPreview;
        set => SetProperty(ref _showPrintPreview, value);
    }
    
    public bool AutoPrintAfterSave
    {
        get => _autoPrintAfterSave;
        set => SetProperty(ref _autoPrintAfterSave, value);
    }
    
    /// <summary>
    /// Available printer types for dropdown
    /// </summary>
    public List<LabelPrinterType> PrinterTypes => Enum.GetValues<LabelPrinterType>().ToList();
    
    /// <summary>
    /// Available label sizes for dropdown
    /// </summary>
    public List<LabelSizeOption> LabelSizes => LabelPrintService.GetLabelSizeOptions();
    
    /// <summary>
    /// Available DPI options
    /// </summary>
    public List<int> DpiOptions => new() { 150, 203, 300, 600 };
    
    #endregion
    
    #region Dialog Properties
    
    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    #endregion
    
    #region Commands
    
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand RefreshPrintersCommand { get; }
    public ICommand TestPrintCommand { get; }
    
    #endregion
    
    #region Methods
    
    private void Save()
    {
        try
        {
            var previousTheme = _settings.UseDarkTheme;
            
            // Connection settings
            _settings.ApiBaseUrl = ApiBaseUrl;
            _settings.UseDarkTheme = UseDarkTheme;
            _settings.SyncIntervalMinutes = SyncIntervalMinutes;
            _settings.AutoSyncEnabled = AutoSyncEnabled;
            _settings.ShowNotifications = ShowNotifications;
            _settings.DefaultLocationCode = string.IsNullOrWhiteSpace(DefaultLocationCode) ? null : DefaultLocationCode;
            
            // Organization settings
            _settings.OrganizationName = OrganizationName;
            _settings.OrganizationCode = OrganizationCode;
            _settings.ShowOrganizationOnLabel = ShowOrganizationOnLabel;
            _settings.ShowUniqueIdOnLabel = ShowUniqueIdOnLabel;
            _settings.AutoGenerateUniqueId = AutoGenerateUniqueId;
            _settings.LabelPreset = LabelPreset;
            
            // Printer settings
            _settings.PrinterSettings.PrinterName = SelectedPrinter?.Name;
            _settings.PrinterSettings.PrinterType = SelectedPrinterType;
            _settings.PrinterSettings.LabelSize = SelectedLabelSize;
            _settings.PrinterSettings.CustomWidthMm = CustomLabelWidth;
            _settings.PrinterSettings.CustomHeightMm = CustomLabelHeight;
            _settings.PrinterSettings.DPI = PrinterDpi;
            _settings.PrinterSettings.DefaultCopies = DefaultCopies;
            _settings.PrinterSettings.ShowPreview = ShowPrintPreview;
            _settings.PrinterSettings.AutoPrint = AutoPrintAfterSave;
            
            _settings.Save();
            
            // Apply theme if changed
            if (previousTheme != UseDarkTheme)
            {
                ThemeService.Instance.ApplyTheme(UseDarkTheme ? "Dark" : "Light");
            }
            
            StatusMessage = "Settings saved";
            DialogResult = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save: {ex.Message}";
            MessageBox.Show($"Failed to save settings:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void Cancel()
    {
        DialogResult = false;
    }
    
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiBaseUrl))
        {
            StatusMessage = "Please enter an API URL";
            return;
        }
        
        StatusMessage = "Testing connection...";
        
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await client.GetAsync($"{ApiBaseUrl.TrimEnd('/')}/health");
            
            if (response.IsSuccessStatusCode)
            {
                StatusMessage = "✓ Connection successful!";
            }
            else
            {
                StatusMessage = $"✗ Server returned {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Connection failed: {ex.Message}";
        }
    }
    
    private void RefreshPrinters()
    {
        var currentPrinter = SelectedPrinter?.Name;
        
        AvailablePrinters.Clear();
        foreach (var printer in _printService.GetAvailablePrinters())
        {
            AvailablePrinters.Add(printer);
        }
        
        // Try to re-select previous printer
        SelectedPrinter = AvailablePrinters.FirstOrDefault(p => p.Name == currentPrinter) 
                         ?? AvailablePrinters.FirstOrDefault(p => p.IsDefault);
        
        StatusMessage = $"Found {AvailablePrinters.Count} printers";
    }
    
    private void TestPrint()
    {
        if (SelectedPrinter == null)
        {
            StatusMessage = "Please select a printer";
            return;
        }
        
        try
        {
            // Save current settings temporarily
            _settings.PrinterSettings.PrinterName = SelectedPrinter.Name;
            _settings.PrinterSettings.PrinterType = SelectedPrinterType;
            _settings.PrinterSettings.LabelSize = SelectedLabelSize;
            _settings.PrinterSettings.DPI = PrinterDpi;
            
            // Create test label
            var testService = new LabelPrintService(_settings);
            var qrContent = QRCodeService.GenerateEquipmentQrCodeWithUniqueId("TEST-0001", "S7TEST00001");
            var labelBytes = testService.GenerateLabelForPrinting("S7TEST00001", qrContent);
            
            if (testService.PrintLabelWithDialog(labelBytes))
            {
                StatusMessage = "Test label sent to printer";
            }
            else
            {
                StatusMessage = "Test print cancelled";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Test print failed: {ex.Message}";
        }
    }
    
    private void ResetToDefaults()
    {
        // Connection defaults
        ApiBaseUrl = "https://api.s7solutions.com/equipment";
        UseDarkTheme = true;
        SyncIntervalMinutes = 5;
        AutoSyncEnabled = true;
        ShowNotifications = true;
        DefaultLocationCode = string.Empty;
        
        // Organization defaults
        OrganizationName = "S7 Solutions";
        OrganizationCode = "S7";
        ShowOrganizationOnLabel = true;
        ShowUniqueIdOnLabel = true;
        AutoGenerateUniqueId = true;
        LabelPreset = "Standard";
        
        // Printer defaults
        SelectedPrinter = AvailablePrinters.FirstOrDefault(p => p.IsDefault);
        SelectedPrinterType = LabelPrinterType.Standard;
        SelectedLabelSize = LabelSize.Medium50x50mm;
        PrinterDpi = 300;
        DefaultCopies = 1;
        ShowPrintPreview = true;
        AutoPrintAfterSave = false;
        CustomLabelWidth = 50;
        CustomLabelHeight = 50;
        
        StatusMessage = "Reset to defaults";
    }
    
    #endregion
}
