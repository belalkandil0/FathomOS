using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using OxyPlot;
using FathomOS.Modules.MruCalibration.Models;
using FathomOS.Modules.MruCalibration.Services;

namespace FathomOS.Modules.MruCalibration.ViewModels;

/// <summary>
/// Main ViewModel for MRU Calibration Module
/// Manages the 7-step wizard workflow and all calibration data
/// </summary>
public class MainViewModel : ViewModelBase
{
    #region Private Fields
    
    private readonly MruDataImportService _importService = new();
    private readonly SessionService _sessionService = new();
    private readonly CalibrationProcessor _processor = new();
    private readonly ChartService _chartService = new();
    
    private CalibrationSession _session = new();
    private int _currentStep = 1;
    private bool _isBusy;
    private string _statusMessage = "Ready - Select calibration type to begin";
    private string _busyMessage = string.Empty;
    
    // Theme
    private bool _isDarkTheme = true;
    
    // Step 2 - File loading
    private string? _selectedFilePath;
    private ObservableCollection<string> _availableColumns = new();
    private ObservableCollection<ColumnPreviewItem> _dataPreview = new();
    private ObservableCollection<DataPreviewRow> _loadedDataPreview = new();
    private bool _hasDateTimeSplit = true;
    
    // Time range filter
    private bool _useTimeFilter;
    private DateTime? _filterStartTime;
    private DateTime? _filterEndTime;
    private DateTime? _dataStartTime;
    private DateTime? _dataEndTime;
    
    // Column selections
    private string? _selectedTimeColumn;
    private string? _selectedReferencePitchColumn;
    private string? _selectedVerifiedPitchColumn;
    private string? _selectedReferenceRollColumn;
    private string? _selectedVerifiedRollColumn;
    
    // Step 4 - Processing
    private double _rejectionThreshold = 3.0;
    private ProcessingResult? _pitchProcessingResult;
    private ProcessingResult? _rollProcessingResult;
    private ObservableCollection<string> _processingLog = new();
    private double _processingProgress;
    
    #endregion
    
    #region Constructor
    
    public MainViewModel()
    {
        InitializeCommands();
        InitializeSession();
    }
    
    private void InitializeCommands()
    {
        // Navigation
        NextStepCommand = new RelayCommand(_ => GoToNextStep(), _ => CanGoNext);
        PreviousStepCommand = new RelayCommand(_ => GoToPreviousStep(), _ => CanGoPrevious);
        GoToStepCommand = new RelayCommand(p => GoToStep(Convert.ToInt32(p)), p => CanGoToStep(Convert.ToInt32(p)));
        
        // Theme and Help
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        ShowStepHelpCommand = new RelayCommand(_ => ShowStepHelp());
        
        // Session management
        NewSessionCommand = new RelayCommand(_ => NewSession());
        LoadSessionCommand = new AsyncRelayCommand(_ => LoadSessionFromFileAsync());
        SaveSessionCommand = new AsyncRelayCommand(_ => SaveSessionToFileAsync(), _ => Session.HasAnyData);
        
        // File operations
        LoadFileCommand = new AsyncRelayCommand(p => LoadFileAsync(p as string));
        BrowseFileCommand = new AsyncRelayCommand(_ => BrowseAndLoadFileAsync());
        RefreshColumnsCommand = new AsyncRelayCommand(_ => RefreshColumnsAsync(), _ => !string.IsNullOrEmpty(SelectedFilePath));
        AutoDetectColumnsCommand = new RelayCommand(_ => AutoDetectColumns(), _ => AvailableColumns.Count > 0);
        
        // Data preview and time filter
        PreviewDataCommand = new AsyncRelayCommand(_ => PreviewLoadedDataAsync(), _ => !string.IsNullOrEmpty(SelectedFilePath));
        ApplyTimeFilterCommand = new RelayCommand(_ => ApplyTimeFilter(), _ => UseTimeFilter && FilterStartTime.HasValue && FilterEndTime.HasValue);
        ClearTimeFilterCommand = new RelayCommand(_ => ClearTimeFilter());
        
        // Import
        ImportDataCommand = new AsyncRelayCommand(_ => ImportDataAsync(), _ => CanImportData);
        
        // Processing (Step 4)
        ProcessDataCommand = new AsyncRelayCommand(_ => ProcessDataAsync(), _ => CanProcess);
        ReprocessCommand = new AsyncRelayCommand(_ => ReprocessDataAsync(), _ => IsProcessed);
        
        // Analysis (Step 5)
        GenerateChartsCommand = new RelayCommand(_ => GenerateCharts(), _ => IsProcessed);
        RefreshChartsCommand = new RelayCommand(_ => GenerateCharts(), _ => IsProcessed);
        ApplyColorSchemeCommand = new RelayCommand(scheme => ApplyColorScheme(scheme?.ToString() ?? "Default"));
        ResetColorsCommand = new RelayCommand(_ => ResetColors());
        
        // NEW v2.3.0 commands
        ExportChartsPngCommand = new RelayCommand(_ => ExportChartsToPng(), _ => IsProcessed);
        ExportChartsPdfCommand = new RelayCommand(_ => ExportChartsToPdf(), _ => IsProcessed);
        ShowHelpDialogCommand = new RelayCommand(_ => ShowHelpDialog());
        
        // Verify & Sign-off (Step 6)
        AcceptPitchCommand = new RelayCommand(_ => SetPitchDecision(AcceptanceDecision.Accepted), 
            _ => PitchData.IsProcessed && AllQcPassed);
        RejectPitchCommand = new RelayCommand(_ => SetPitchDecision(AcceptanceDecision.Rejected), 
            _ => PitchData.IsProcessed && AllQcPassed);
        AcceptRollCommand = new RelayCommand(_ => SetRollDecision(AcceptanceDecision.Accepted), 
            _ => RollData.IsProcessed && AllQcPassed);
        RejectRollCommand = new RelayCommand(_ => SetRollDecision(AcceptanceDecision.Rejected), 
            _ => RollData.IsProcessed && AllQcPassed);
        
        // Export (Step 7)
        ExportPdfCommand = new AsyncRelayCommand(_ => ExportPdfAsync(), _ => CanExport);
        ExportExcelCommand = new AsyncRelayCommand(_ => ExportExcelAsync(), _ => CanExport);
        ExportAllCommand = new AsyncRelayCommand(_ => ExportAllAsync(), _ => CanExport);
        
        // Certificate generation (Fathom OS v3.0.0)
        GenerateCertificateCommand = new RelayCommand(_ => GenerateCertificate(), _ => CanExport);
    }
    
    private void InitializeSession()
    {
        _session = new CalibrationSession();
        OnPropertyChanged(nameof(Session));
        OnPropertyChanged(nameof(ProjectInfo));
        OnPropertyChanged(nameof(PitchData));
        OnPropertyChanged(nameof(RollData));
    }
    
    #endregion
    
    #region Properties - Session
    
    public CalibrationSession Session
    {
        get => _session;
        set
        {
            if (SetProperty(ref _session, value))
            {
                OnPropertyChanged(nameof(ProjectInfo));
                OnPropertyChanged(nameof(PitchData));
                OnPropertyChanged(nameof(RollData));
            }
        }
    }
    
    public ProjectInfo ProjectInfo => _session.ProjectInfo;
    public SensorCalibrationData PitchData => _session.PitchData;
    public SensorCalibrationData RollData => _session.RollData;
    
    #endregion
    
    #region Properties - Wizard State
    
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, Math.Clamp(value, 1, 7)))
            {
                UpdateStepState();
            }
        }
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public string BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }
    
    #endregion
    
    #region Properties - Step Definitions
    
    public static IReadOnlyList<StepDefinition> Steps { get; } = new List<StepDefinition>
    {
        new(1, "Select", "Choose sensor type and purpose", "🎯"),
        new(2, "Load", "Import NPD file and map columns", "📂"),
        new(3, "Configure", "Project info and settings", "⚙️"),
        new(4, "Process", "Calculate and analyze data", "🔄"),
        new(5, "Analyze", "Review charts and statistics", "📈"),
        new(6, "Verify", "QC checks and sign-off", "✅"),
        new(7, "Export", "Generate reports", "📤")
    };
    
    public StepDefinition CurrentStepDefinition => Steps[CurrentStep - 1];
    
    #endregion
    
    #region Properties - Navigation
    
    public bool CanGoNext => CurrentStep < 7 && ValidateCurrentStep();
    public bool CanGoPrevious => CurrentStep > 1;
    
    private bool CanGoToStep(int step)
    {
        if (step < 1 || step > 7) return false;
        if (step <= CurrentStep) return true;
        
        for (int i = 1; i < step; i++)
        {
            if (!IsStepValid(i)) return false;
        }
        return true;
    }
    
    #endregion
    
    #region Properties - Step 2 (Load)
    
    public string? SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (SetProperty(ref _selectedFilePath, value))
            {
                OnPropertyChanged(nameof(HasSelectedFile));
                OnPropertyChanged(nameof(SelectedFileName));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    
    public bool HasSelectedFile => !string.IsNullOrEmpty(SelectedFilePath);
    public string SelectedFileName => Path.GetFileName(SelectedFilePath ?? string.Empty);
    
    public ObservableCollection<string> AvailableColumns
    {
        get => _availableColumns;
        set => SetProperty(ref _availableColumns, value);
    }
    
    public ObservableCollection<ColumnPreviewItem> DataPreview
    {
        get => _dataPreview;
        set => SetProperty(ref _dataPreview, value);
    }
    
    public bool HasDateTimeSplit
    {
        get => _hasDateTimeSplit;
        set => SetProperty(ref _hasDateTimeSplit, value);
    }
    
    // Column selections
    public string? SelectedTimeColumn
    {
        get => _selectedTimeColumn;
        set { SetProperty(ref _selectedTimeColumn, value); OnPropertyChanged(nameof(CanImportData)); }
    }
    
    public string? SelectedReferencePitchColumn
    {
        get => _selectedReferencePitchColumn;
        set { SetProperty(ref _selectedReferencePitchColumn, value); OnPropertyChanged(nameof(CanImportData)); OnPropertyChanged(nameof(HasPitchMapping)); }
    }
    
    public string? SelectedVerifiedPitchColumn
    {
        get => _selectedVerifiedPitchColumn;
        set { SetProperty(ref _selectedVerifiedPitchColumn, value); OnPropertyChanged(nameof(CanImportData)); OnPropertyChanged(nameof(HasPitchMapping)); }
    }
    
    public string? SelectedReferenceRollColumn
    {
        get => _selectedReferenceRollColumn;
        set { SetProperty(ref _selectedReferenceRollColumn, value); OnPropertyChanged(nameof(CanImportData)); OnPropertyChanged(nameof(HasRollMapping)); }
    }
    
    public string? SelectedVerifiedRollColumn
    {
        get => _selectedVerifiedRollColumn;
        set { SetProperty(ref _selectedVerifiedRollColumn, value); OnPropertyChanged(nameof(CanImportData)); OnPropertyChanged(nameof(HasRollMapping)); }
    }
    
    public bool CanImportData => 
        HasSelectedFile && 
        !string.IsNullOrEmpty(SelectedTimeColumn) &&
        (HasPitchMapping || HasRollMapping);
    
    public bool HasPitchMapping => 
        !string.IsNullOrEmpty(SelectedReferencePitchColumn) && 
        !string.IsNullOrEmpty(SelectedVerifiedPitchColumn);
    
    public bool HasRollMapping => 
        !string.IsNullOrEmpty(SelectedReferenceRollColumn) && 
        !string.IsNullOrEmpty(SelectedVerifiedRollColumn);
    
    #endregion
    
    #region Commands
    
    // Navigation
    public ICommand NextStepCommand { get; private set; } = null!;
    public ICommand PreviousStepCommand { get; private set; } = null!;
    public ICommand GoToStepCommand { get; private set; } = null!;
    
    // Theme and Help
    public ICommand ToggleThemeCommand { get; private set; } = null!;
    public ICommand ShowStepHelpCommand { get; private set; } = null!;
    
    // Session
    public ICommand NewSessionCommand { get; private set; } = null!;
    public ICommand LoadSessionCommand { get; private set; } = null!;
    public ICommand SaveSessionCommand { get; private set; } = null!;
    
    // File
    public ICommand LoadFileCommand { get; private set; } = null!;
    public ICommand BrowseFileCommand { get; private set; } = null!;
    public ICommand RefreshColumnsCommand { get; private set; } = null!;
    public ICommand AutoDetectColumnsCommand { get; private set; } = null!;
    public ICommand ImportDataCommand { get; private set; } = null!;
    
    // Data Preview and Time Filter
    public ICommand PreviewDataCommand { get; private set; } = null!;
    public ICommand ApplyTimeFilterCommand { get; private set; } = null!;
    public ICommand ClearTimeFilterCommand { get; private set; } = null!;
    
    // Processing (Step 4)
    public ICommand ProcessDataCommand { get; private set; } = null!;
    public ICommand ReprocessCommand { get; private set; } = null!;
    
    #endregion
    
    #region Properties - Theme
    
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                OnPropertyChanged(nameof(ThemeIcon));
                OnPropertyChanged(nameof(ThemeButtonTooltip));
            }
        }
    }
    
    public string ThemeIcon => IsDarkTheme ? "WeatherNight" : "WhiteBalanceSunny";
    public string ThemeButtonTooltip => IsDarkTheme ? "Switch to Light Theme" : "Switch to Dark Theme";
    
    #endregion
    
    #region Properties - Time Filter
    
    public bool UseTimeFilter
    {
        get => _useTimeFilter;
        set => SetProperty(ref _useTimeFilter, value);
    }
    
    public DateTime? FilterStartTime
    {
        get => _filterStartTime;
        set => SetProperty(ref _filterStartTime, value);
    }
    
    public DateTime? FilterEndTime
    {
        get => _filterEndTime;
        set => SetProperty(ref _filterEndTime, value);
    }
    
    public DateTime? DataStartTime
    {
        get => _dataStartTime;
        set => SetProperty(ref _dataStartTime, value);
    }
    
    public DateTime? DataEndTime
    {
        get => _dataEndTime;
        set => SetProperty(ref _dataEndTime, value);
    }
    
    public ObservableCollection<DataPreviewRow> LoadedDataPreview
    {
        get => _loadedDataPreview;
        set => SetProperty(ref _loadedDataPreview, value);
    }
    
    public int TotalDataPoints => LoadedDataPreview.Count;
    public int FilteredDataPoints => UseTimeFilter && FilterStartTime.HasValue && FilterEndTime.HasValue
        ? LoadedDataPreview.Count(r => r.Timestamp >= FilterStartTime && r.Timestamp <= FilterEndTime)
        : LoadedDataPreview.Count;
    
    #endregion
    
    #region Properties - Step 4 (Processing)
    
    public double RejectionThreshold
    {
        get => _rejectionThreshold;
        set
        {
            if (SetProperty(ref _rejectionThreshold, Math.Clamp(value, 1.0, 5.0)))
            {
                Session.RejectionThreshold = value;
            }
        }
    }
    
    public ProcessingResult? PitchProcessingResult
    {
        get => _pitchProcessingResult;
        set => SetProperty(ref _pitchProcessingResult, value);
    }
    
    public ProcessingResult? RollProcessingResult
    {
        get => _rollProcessingResult;
        set => SetProperty(ref _rollProcessingResult, value);
    }
    
    public ObservableCollection<string> ProcessingLog
    {
        get => _processingLog;
        set => SetProperty(ref _processingLog, value);
    }
    
    public double ProcessingProgress
    {
        get => _processingProgress;
        set => SetProperty(ref _processingProgress, value);
    }
    
    public bool CanProcess => Session.HasAnyData && !IsBusy && 
        (PitchData.IsLoaded || RollData.IsLoaded);
    
    public bool IsProcessed => PitchData.IsProcessed || RollData.IsProcessed;
    
    // Statistics display properties
    public string PitchStatsSummary => PitchData.IsProcessed && PitchData.Statistics != null
        ? $"Mean: {PitchData.Statistics.MeanCO_Accepted:F4}° | SD: {PitchData.Statistics.StdDevCO_Accepted:F4}° | Accepted: {PitchData.Statistics.AcceptedCount}/{PitchData.Statistics.TotalObservations}"
        : "Not processed";
    
    public string RollStatsSummary => RollData.IsProcessed && RollData.Statistics != null
        ? $"Mean: {RollData.Statistics.MeanCO_Accepted:F4}° | SD: {RollData.Statistics.StdDevCO_Accepted:F4}° | Accepted: {RollData.Statistics.AcceptedCount}/{RollData.Statistics.TotalObservations}"
        : "Not processed";
    
    #endregion
    
    #region Properties - Step 5 (Analysis Charts)
    
    // Existing charts
    private OxyPlot.PlotModel? _pitchCalibrationChart;
    private OxyPlot.PlotModel? _rollCalibrationChart;
    private OxyPlot.PlotModel? _pitchCOChart;
    private OxyPlot.PlotModel? _rollCOChart;
    private OxyPlot.PlotModel? _pitchHistogram;
    private OxyPlot.PlotModel? _rollHistogram;
    
    // NEW: Additional charts
    private OxyPlot.PlotModel? _pitchCorrelationChart;
    private OxyPlot.PlotModel? _rollCorrelationChart;
    private OxyPlot.PlotModel? _pitchTrendChart;
    private OxyPlot.PlotModel? _rollTrendChart;
    private OxyPlot.PlotModel? _boxPlotChart;
    private OxyPlot.PlotModel? _pitchCDFChart;
    private OxyPlot.PlotModel? _rollCDFChart;
    private OxyPlot.PlotModel? _pitchMovingAvgChart;
    private OxyPlot.PlotModel? _rollMovingAvgChart;
    private OxyPlot.PlotModel? _pitchRollCorrelationChart;
    
    // NEW v2.3.0 charts
    private OxyPlot.PlotModel? _pitchQQChart;
    private OxyPlot.PlotModel? _rollQQChart;
    private OxyPlot.PlotModel? _pitchAutocorrelationChart;
    private OxyPlot.PlotModel? _rollAutocorrelationChart;
    private OxyPlot.PlotModel? _pitchBeforeAfterChart;
    private OxyPlot.PlotModel? _rollBeforeAfterChart;
    
    private int _selectedChartTab;
    private int _selectedChartType;
    private int _movingAverageWindow = 10;
    
    // Color settings
    private ChartColorSettings _chartColorSettings = new();
    private string _selectedColorScheme = "Default";
    
    // Chart export service
    private readonly ChartExportService _chartExportService = new();
    
    // Existing chart properties
    public OxyPlot.PlotModel? PitchCalibrationChart
    {
        get => _pitchCalibrationChart;
        set => SetProperty(ref _pitchCalibrationChart, value);
    }
    
    public OxyPlot.PlotModel? RollCalibrationChart
    {
        get => _rollCalibrationChart;
        set => SetProperty(ref _rollCalibrationChart, value);
    }
    
    public OxyPlot.PlotModel? PitchCOChart
    {
        get => _pitchCOChart;
        set => SetProperty(ref _pitchCOChart, value);
    }
    
    public OxyPlot.PlotModel? RollCOChart
    {
        get => _rollCOChart;
        set => SetProperty(ref _rollCOChart, value);
    }
    
    public OxyPlot.PlotModel? PitchHistogram
    {
        get => _pitchHistogram;
        set => SetProperty(ref _pitchHistogram, value);
    }
    
    public OxyPlot.PlotModel? RollHistogram
    {
        get => _rollHistogram;
        set => SetProperty(ref _rollHistogram, value);
    }
    
    // NEW: Additional chart properties
    public OxyPlot.PlotModel? PitchCorrelationChart
    {
        get => _pitchCorrelationChart;
        set => SetProperty(ref _pitchCorrelationChart, value);
    }
    
    public OxyPlot.PlotModel? RollCorrelationChart
    {
        get => _rollCorrelationChart;
        set => SetProperty(ref _rollCorrelationChart, value);
    }
    
    public OxyPlot.PlotModel? PitchTrendChart
    {
        get => _pitchTrendChart;
        set => SetProperty(ref _pitchTrendChart, value);
    }
    
    public OxyPlot.PlotModel? RollTrendChart
    {
        get => _rollTrendChart;
        set => SetProperty(ref _rollTrendChart, value);
    }
    
    public OxyPlot.PlotModel? BoxPlotChart
    {
        get => _boxPlotChart;
        set => SetProperty(ref _boxPlotChart, value);
    }
    
    public OxyPlot.PlotModel? PitchCDFChart
    {
        get => _pitchCDFChart;
        set => SetProperty(ref _pitchCDFChart, value);
    }
    
    public OxyPlot.PlotModel? RollCDFChart
    {
        get => _rollCDFChart;
        set => SetProperty(ref _rollCDFChart, value);
    }
    
    public OxyPlot.PlotModel? PitchMovingAvgChart
    {
        get => _pitchMovingAvgChart;
        set => SetProperty(ref _pitchMovingAvgChart, value);
    }
    
    public OxyPlot.PlotModel? RollMovingAvgChart
    {
        get => _rollMovingAvgChart;
        set => SetProperty(ref _rollMovingAvgChart, value);
    }
    
    public OxyPlot.PlotModel? PitchRollCorrelationChart
    {
        get => _pitchRollCorrelationChart;
        set => SetProperty(ref _pitchRollCorrelationChart, value);
    }
    
    // NEW v2.3.0 chart properties
    public OxyPlot.PlotModel? PitchQQChart
    {
        get => _pitchQQChart;
        set => SetProperty(ref _pitchQQChart, value);
    }
    
    public OxyPlot.PlotModel? RollQQChart
    {
        get => _rollQQChart;
        set => SetProperty(ref _rollQQChart, value);
    }
    
    public OxyPlot.PlotModel? PitchAutocorrelationChart
    {
        get => _pitchAutocorrelationChart;
        set => SetProperty(ref _pitchAutocorrelationChart, value);
    }
    
    public OxyPlot.PlotModel? RollAutocorrelationChart
    {
        get => _rollAutocorrelationChart;
        set => SetProperty(ref _rollAutocorrelationChart, value);
    }
    
    public OxyPlot.PlotModel? PitchBeforeAfterChart
    {
        get => _pitchBeforeAfterChart;
        set => SetProperty(ref _pitchBeforeAfterChart, value);
    }
    
    public OxyPlot.PlotModel? RollBeforeAfterChart
    {
        get => _rollBeforeAfterChart;
        set => SetProperty(ref _rollBeforeAfterChart, value);
    }
    
    public int SelectedChartTab
    {
        get => _selectedChartTab;
        set => SetProperty(ref _selectedChartTab, value);
    }
    
    public int SelectedChartType
    {
        get => _selectedChartType;
        set
        {
            if (SetProperty(ref _selectedChartType, value))
            {
                GenerateSelectedCharts();
            }
        }
    }
    
    public int MovingAverageWindow
    {
        get => _movingAverageWindow;
        set
        {
            if (value >= 3 && value <= 50 && SetProperty(ref _movingAverageWindow, value))
            {
                RegenerateMovingAverageCharts();
            }
        }
    }
    
    public ChartColorSettings ChartColorSettings
    {
        get => _chartColorSettings;
        set
        {
            if (SetProperty(ref _chartColorSettings, value))
            {
                _chartService.ApplyColorSettings(value);
                RegenerateAllCharts();
            }
        }
    }
    
    public string SelectedColorScheme
    {
        get => _selectedColorScheme;
        set
        {
            if (SetProperty(ref _selectedColorScheme, value))
            {
                ApplyColorScheme(value);
            }
        }
    }
    
    public List<string> AvailableColorSchemes { get; } = new()
    {
        "Default",
        "High Contrast",
        "Color Blind Friendly",
        "Monochrome"
    };
    
    public ICommand GenerateChartsCommand { get; private set; } = null!;
    public ICommand RefreshChartsCommand { get; private set; } = null!;
    public ICommand ApplyColorSchemeCommand { get; private set; } = null!;
    public ICommand ResetColorsCommand { get; private set; } = null!;
    
    // NEW v2.3.0 commands
    public ICommand ExportChartsPngCommand { get; private set; } = null!;
    public ICommand ExportChartsPdfCommand { get; private set; } = null!;
    public ICommand ShowHelpDialogCommand { get; private set; } = null!;
    
    #endregion
    
    #region Properties - Step 6 (Verify & Sign-off)
    
    private readonly ReportService _reportService = new();
    private readonly ExcelExportService _excelService = new();
    
    // QC Checklist
    private bool _qcStatsWithinRange;
    private bool _qcRejectionReasonable;
    private bool _qcNoDrift;
    private bool _qcDataQuality;
    
    public bool QcStatsWithinRange
    {
        get => _qcStatsWithinRange;
        set { SetProperty(ref _qcStatsWithinRange, value); OnPropertyChanged(nameof(AllQcPassed)); }
    }
    
    public bool QcRejectionReasonable
    {
        get => _qcRejectionReasonable;
        set { SetProperty(ref _qcRejectionReasonable, value); OnPropertyChanged(nameof(AllQcPassed)); }
    }
    
    public bool QcNoDrift
    {
        get => _qcNoDrift;
        set { SetProperty(ref _qcNoDrift, value); OnPropertyChanged(nameof(AllQcPassed)); }
    }
    
    public bool QcDataQuality
    {
        get => _qcDataQuality;
        set { SetProperty(ref _qcDataQuality, value); OnPropertyChanged(nameof(AllQcPassed)); }
    }
    
    public bool AllQcPassed => QcStatsWithinRange && QcRejectionReasonable && QcNoDrift && QcDataQuality;
    
    // Sign-off Commands
    public ICommand AcceptPitchCommand { get; private set; } = null!;
    public ICommand RejectPitchCommand { get; private set; } = null!;
    public ICommand AcceptRollCommand { get; private set; } = null!;
    public ICommand RejectRollCommand { get; private set; } = null!;
    
    public bool CanSignOff => IsProcessed && AllQcPassed;
    public bool IsFullySignedOff => 
        (!PitchData.IsProcessed || PitchData.Decision != AcceptanceDecision.NotDecided) &&
        (!RollData.IsProcessed || RollData.Decision != AcceptanceDecision.NotDecided);
    
    #endregion
    
    #region Properties - Step 7 (Export)
    
    private string? _lastExportPath;
    
    public string? LastExportPath
    {
        get => _lastExportPath;
        set => SetProperty(ref _lastExportPath, value);
    }
    
    public bool CanExport => IsFullySignedOff;
    
    public ICommand ExportPdfCommand { get; private set; } = null!;
    public ICommand ExportExcelCommand { get; private set; } = null!;
    public ICommand ExportAllCommand { get; private set; } = null!;
    
    // Certificate generation (Fathom OS v3.0.0)
    public ICommand GenerateCertificateCommand { get; private set; } = null!;
    
    #endregion
    
    #region Navigation Methods
    
    private void GoToNextStep()
    {
        if (CanGoNext)
        {
            Session.StepCompleted[CurrentStep - 1] = true;
            CurrentStep++;
        }
    }
    
    private void GoToPreviousStep()
    {
        if (CanGoPrevious)
        {
            CurrentStep--;
        }
    }
    
    private void GoToStep(int step)
    {
        if (CanGoToStep(step))
        {
            CurrentStep = step;
        }
    }
    
    private void UpdateStepState()
    {
        _session.CurrentStep = CurrentStep;
        
        StatusMessage = CurrentStep switch
        {
            1 => "Select calibration type (Pitch/Roll) and purpose (Calibration/Verification)",
            2 => Session.HasAnyData 
                ? $"Data loaded: {PitchData.PointCount} pitch, {RollData.PointCount} roll points"
                : "Load NPD file and map the columns for reference and verified systems",
            3 => "Enter project information and MRU system details",
            4 => "Processing data - calculating C-O values and applying 3-sigma rejection",
            5 => "Review charts, statistics, and outlier analysis",
            6 => "Perform QC checks and sign-off on results",
            7 => "Export PDF report and Excel data package",
            _ => "Ready"
        };
        
        // Trigger step-specific actions
        OnStepChanged();
        
        CommandManager.InvalidateRequerySuggested();
    }
    
    #endregion
    
    #region Validation Methods
    
    private bool ValidateCurrentStep()
    {
        return IsStepValid(CurrentStep);
    }
    
    private bool IsStepValid(int step)
    {
        return step switch
        {
            1 => true,
            2 => Session.HasAnyData,
            3 => ProjectInfo.IsValid,
            4 => PitchData.IsProcessed || RollData.IsProcessed,
            5 => true,
            6 => HasValidSignOff(),
            7 => true,
            _ => false
        };
    }
    
    private bool HasValidSignOff()
    {
        bool pitchValid = !PitchData.HasData || PitchData.Decision != AcceptanceDecision.NotDecided;
        bool rollValid = !RollData.HasData || RollData.Decision != AcceptanceDecision.NotDecided;
        return pitchValid && rollValid;
    }
    
    #endregion
    
    #region Session Methods
    
    private void NewSession()
    {
        InitializeSession();
        CurrentStep = 1;
        ClearFileSelection();
        StatusMessage = "New session started - Select calibration type to begin";
    }
    
    private void ClearFileSelection()
    {
        SelectedFilePath = null;
        AvailableColumns.Clear();
        DataPreview.Clear();
        SelectedTimeColumn = null;
        SelectedReferencePitchColumn = null;
        SelectedVerifiedPitchColumn = null;
        SelectedReferenceRollColumn = null;
        SelectedVerifiedRollColumn = null;
    }
    
    private async Task LoadSessionFromFileAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Load Calibration Session",
            Filter = "MRU Session Files (*.mru)|*.mru|All Files (*.*)|*.*",
            DefaultExt = ".mru"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Loading session...";
                
                var session = await _sessionService.LoadSessionAsync(dialog.FileName);
                if (session != null)
                {
                    Session = session;
                    CurrentStep = Math.Max(1, session.CurrentStep);
                    StatusMessage = $"Session loaded: {session.ProjectInfo.ProjectTitle}";
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to load session file.", "Load Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }
    }
    
    private async Task SaveSessionToFileAsync()
    {
        var defaultName = _sessionService.GenerateSessionFileName(Session);
        
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save Calibration Session",
            Filter = "MRU Session Files (*.mru)|*.mru",
            DefaultExt = ".mru",
            FileName = defaultName,
            InitialDirectory = SessionService.DefaultSessionDirectory
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                BusyMessage = "Saving session...";
                
                var success = await _sessionService.SaveSessionAsync(Session, dialog.FileName);
                
                if (success)
                {
                    StatusMessage = $"Session saved: {Path.GetFileName(dialog.FileName)}";
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to save session.", "Save Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }
    }
    
    #endregion
    
    #region File Loading Methods
    
    private async Task BrowseAndLoadFileAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select NaviPac NPD File",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            CheckFileExists = true
        };
        
        if (dialog.ShowDialog() == true)
        {
            await LoadFileAsync(dialog.FileName);
        }
    }
    
    private async Task LoadFileAsync(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        
        try
        {
            IsBusy = true;
            BusyMessage = "Loading NPD file...";
            StatusMessage = "Analyzing file structure...";
            
            SelectedFilePath = filePath;
            Session.SourceNpdFilePath = filePath;
            
            await Task.Run(() =>
            {
                // Get available columns
                var columns = _importService.GetAvailableColumns(filePath);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableColumns = new ObservableCollection<string>(columns);
                });
                
                // Get preview data
                var preview = _importService.PreviewData(filePath, 10);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    DataPreview.Clear();
                    int row = 1;
                    foreach (var item in preview)
                    {
                        DataPreview.Add(new ColumnPreviewItem
                        {
                            RowNumber = row++,
                            Values = item
                        });
                    }
                });
            });
            
            // Auto-detect columns
            AutoDetectColumns();
            
            StatusMessage = $"Loaded: {Path.GetFileName(filePath)} - {AvailableColumns.Count} columns detected";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to load file:\n\n{ex.Message}",
                "Load Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
    
    private async Task RefreshColumnsAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath)) return;
        await LoadFileAsync(SelectedFilePath);
    }
    
    private void AutoDetectColumns()
    {
        if (AvailableColumns.Count == 0) return;
        
        // Auto-detect Time column
        SelectedTimeColumn = AvailableColumns.FirstOrDefault(c => 
            c.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("UTC", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("DateTime", StringComparison.OrdinalIgnoreCase));
        
        // Build list of potential MRU/Motion columns
        var allColumns = AvailableColumns.ToList();
        
        // Pitch detection patterns (order matters - more specific first)
        var pitchPatterns = new[] { "Pitch", "pitch", " P ", "_P_", ":P:", ".P.", "P[", "P]" };
        var rollPatterns = new[] { "Roll", "roll", " R ", "_R_", ":R:", ".R.", "R[", "R]" };
        
        // MRU/Motion sensor indicators
        var mruIndicators = new[] { "MRU", "Motion", "IMU", "Attitude", "AHRS", "INS" };
        
        // Reference indicators (vessel/primary/MRU1/Ref)
        var refIndicators = new[] { "Ref", "Reference", "Vessel", "Ship", "Primary", "MRU1", "MRU 1", "Motion1", "Motion 1", "_1", ":1", "System1" };
        
        // Verified indicators (ROV/secondary/MRU2/Cal/Ver)
        var verIndicators = new[] { "Ver", "Verified", "ROV", "Vehicle", "Secondary", "MRU2", "MRU 2", "Motion2", "Motion 2", "_2", ":2", "Cal", "System2" };
        
        // Find all pitch columns
        var pitchColumns = allColumns.Where(c => 
            pitchPatterns.Any(p => c.Contains(p, StringComparison.OrdinalIgnoreCase)) ||
            mruIndicators.Any(m => c.Contains(m, StringComparison.OrdinalIgnoreCase) && 
                             (c.EndsWith("P", StringComparison.OrdinalIgnoreCase) || 
                              c.Contains("_P", StringComparison.OrdinalIgnoreCase) ||
                              c.Contains(":P", StringComparison.OrdinalIgnoreCase)))).ToList();
        
        // Find all roll columns
        var rollColumns = allColumns.Where(c => 
            rollPatterns.Any(p => c.Contains(p, StringComparison.OrdinalIgnoreCase)) ||
            mruIndicators.Any(m => c.Contains(m, StringComparison.OrdinalIgnoreCase) && 
                             (c.EndsWith("R", StringComparison.OrdinalIgnoreCase) || 
                              c.Contains("_R", StringComparison.OrdinalIgnoreCase) ||
                              c.Contains(":R", StringComparison.OrdinalIgnoreCase)))).ToList();
        
        // Detect Reference Pitch
        SelectedReferencePitchColumn = pitchColumns.FirstOrDefault(c => 
            refIndicators.Any(r => c.Contains(r, StringComparison.OrdinalIgnoreCase)));
        
        // If no specific reference found, try first pitch column
        if (SelectedReferencePitchColumn == null && pitchColumns.Count > 0)
        {
            SelectedReferencePitchColumn = pitchColumns.First();
        }
        
        // Detect Verified Pitch
        SelectedVerifiedPitchColumn = pitchColumns.FirstOrDefault(c => 
            verIndicators.Any(v => c.Contains(v, StringComparison.OrdinalIgnoreCase)));
        
        // If no specific verified found, use second pitch column
        if (SelectedVerifiedPitchColumn == null && pitchColumns.Count > 1)
        {
            SelectedVerifiedPitchColumn = pitchColumns.FirstOrDefault(c => c != SelectedReferencePitchColumn);
        }
        
        // Detect Reference Roll
        SelectedReferenceRollColumn = rollColumns.FirstOrDefault(c => 
            refIndicators.Any(r => c.Contains(r, StringComparison.OrdinalIgnoreCase)));
        
        // If no specific reference found, try first roll column
        if (SelectedReferenceRollColumn == null && rollColumns.Count > 0)
        {
            SelectedReferenceRollColumn = rollColumns.First();
        }
        
        // Detect Verified Roll
        SelectedVerifiedRollColumn = rollColumns.FirstOrDefault(c => 
            verIndicators.Any(v => c.Contains(v, StringComparison.OrdinalIgnoreCase)));
        
        // If no specific verified found, use second roll column
        if (SelectedVerifiedRollColumn == null && rollColumns.Count > 1)
        {
            SelectedVerifiedRollColumn = rollColumns.FirstOrDefault(c => c != SelectedReferenceRollColumn);
        }
        
        // Build status message
        var detected = new List<string>();
        if (SelectedTimeColumn != null) detected.Add("Time");
        if (SelectedReferencePitchColumn != null) detected.Add("Ref Pitch");
        if (SelectedVerifiedPitchColumn != null) detected.Add("Ver Pitch");
        if (SelectedReferenceRollColumn != null) detected.Add("Ref Roll");
        if (SelectedVerifiedRollColumn != null) detected.Add("Ver Roll");
        
        StatusMessage = detected.Count > 0 
            ? $"Auto-detected: {string.Join(", ", detected)} - Review and adjust if needed"
            : "No MRU columns detected - Please select columns manually";
    }
    
    #endregion
    
    #region Theme and Help Methods
    
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        // Theme change is handled by raising OnThemeChanged event
        ThemeChanged?.Invoke(this, IsDarkTheme);
        StatusMessage = IsDarkTheme ? "Switched to Dark Theme" : "Switched to Light Theme";
    }
    
    public event EventHandler<bool>? ThemeChanged;
    
    private void ShowStepHelp()
    {
        if (StepHelpContent.HelpContent.TryGetValue(CurrentStep, out var help))
        {
            System.Windows.MessageBox.Show(help.Description, help.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    #endregion
    
    #region Data Preview and Time Filter Methods
    
    private async Task PreviewLoadedDataAsync()
    {
        if (string.IsNullOrEmpty(SelectedFilePath)) return;
        
        try
        {
            IsBusy = true;
            BusyMessage = "Loading data preview...";
            
            await Task.Run(() =>
            {
                var columns = _importService.GetAvailableColumns(SelectedFilePath);
                // This just reloads the available columns - actual preview comes after import
            });
            
            StatusMessage = "Data preview ready - Configure columns and import";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Preview failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void ApplyTimeFilter()
    {
        if (!UseTimeFilter || !FilterStartTime.HasValue || !FilterEndTime.HasValue) return;
        
        OnPropertyChanged(nameof(FilteredDataPoints));
        StatusMessage = $"Time filter applied: {FilteredDataPoints:N0} of {TotalDataPoints:N0} points selected";
    }
    
    private void ClearTimeFilter()
    {
        UseTimeFilter = false;
        FilterStartTime = DataStartTime;
        FilterEndTime = DataEndTime;
        OnPropertyChanged(nameof(FilteredDataPoints));
        StatusMessage = "Time filter cleared - All data points selected";
    }
    
    private void UpdateDataPreviewFromImport(List<MruDataPoint> pitchPoints, List<MruDataPoint> rollPoints)
    {
        var previewRows = new List<DataPreviewRow>();
        
        // Combine pitch and roll data by timestamp
        var allTimestamps = pitchPoints.Select(p => p.Timestamp)
            .Union(rollPoints.Select(r => r.Timestamp))
            .Distinct()
            .OrderBy(t => t)
            .ToList();
        
        int index = 1;
        foreach (var ts in allTimestamps)
        {
            var pitchPoint = pitchPoints.FirstOrDefault(p => p.Timestamp == ts);
            var rollPoint = rollPoints.FirstOrDefault(r => r.Timestamp == ts);
            
            previewRows.Add(new DataPreviewRow
            {
                Index = index++,
                Timestamp = ts,
                ReferencePitch = pitchPoint?.ReferenceValue,
                VerifiedPitch = pitchPoint?.VerifiedValue,
                PitchCO = pitchPoint?.CO,
                ReferenceRoll = rollPoint?.ReferenceValue,
                VerifiedRoll = rollPoint?.VerifiedValue,
                RollCO = rollPoint?.CO
            });
        }
        
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LoadedDataPreview = new ObservableCollection<DataPreviewRow>(previewRows);
            
            if (previewRows.Count > 0)
            {
                DataStartTime = previewRows.First().Timestamp;
                DataEndTime = previewRows.Last().Timestamp;
                FilterStartTime = DataStartTime;
                FilterEndTime = DataEndTime;
            }
            
            OnPropertyChanged(nameof(TotalDataPoints));
            OnPropertyChanged(nameof(FilteredDataPoints));
        });
    }
    
    #endregion
    
    #region Import Methods
    
    private async Task ImportDataAsync()
    {
        if (!CanImportData) return;
        
        try
        {
            IsBusy = true;
            BusyMessage = "Importing calibration data...";
            StatusMessage = "Parsing NPD file and extracting MRU values...";
            
            var mapping = new MruColumnMapping
            {
                TimeColumn = SelectedTimeColumn!,
                ReferencePitchColumn = SelectedReferencePitchColumn ?? string.Empty,
                VerifiedPitchColumn = SelectedVerifiedPitchColumn ?? string.Empty,
                ReferenceRollColumn = SelectedReferenceRollColumn ?? string.Empty,
                VerifiedRollColumn = SelectedVerifiedRollColumn ?? string.Empty,
                HasDateTimeSplit = HasDateTimeSplit
            };
            
            MruImportResult? result = null;
            
            await Task.Run(() =>
            {
                result = _importService.ImportData(SelectedFilePath!, mapping);
            });
            
            if (result == null || !result.Success)
            {
                var errors = result?.Errors ?? new List<string> { "Unknown error" };
                System.Windows.MessageBox.Show(
                    $"Failed to import data:\n\n{string.Join("\n", errors)}",
                    "Import Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                StatusMessage = "Import failed - check column mapping";
                return;
            }
            
            // Store imported data in session
            if (result.PitchPoints.Count > 0)
            {
                PitchData.DataPoints = result.PitchPoints;
                PitchData.TimeColumnName = SelectedTimeColumn!;
                PitchData.ReferenceColumnName = SelectedReferencePitchColumn!;
                PitchData.VerifiedColumnName = SelectedVerifiedPitchColumn!;
                PitchData.IsLoaded = true;
            }
            
            if (result.RollPoints.Count > 0)
            {
                RollData.DataPoints = result.RollPoints;
                RollData.TimeColumnName = SelectedTimeColumn!;
                RollData.ReferenceColumnName = SelectedReferenceRollColumn!;
                RollData.VerifiedColumnName = SelectedVerifiedRollColumn!;
                RollData.IsLoaded = true;
            }
            
            // Show warnings if any
            if (result.Warnings.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Import warnings: {string.Join("; ", result.Warnings)}");
            }
            
            Session.MarkModified();
            
            // Update data preview grid
            UpdateDataPreviewFromImport(result.PitchPoints, result.RollPoints);
            
            StatusMessage = $"Imported: {result.TotalPitchPoints} pitch, {result.TotalRollPoints} roll points";
            
            // Notify property changes
            OnPropertyChanged(nameof(Session));
            OnPropertyChanged(nameof(PitchData));
            OnPropertyChanged(nameof(RollData));
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Import failed:\n\n{ex.Message}",
                "Import Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
    
    #endregion
    
    #region Processing Methods (Step 4)
    
    private async Task ProcessDataAsync()
    {
        if (!CanProcess) return;
        
        try
        {
            IsBusy = true;
            ProcessingLog.Clear();
            ProcessingProgress = 0;
            
            BusyMessage = "Processing calibration data...";
            StatusMessage = "Calculating C-O values and applying 3-sigma rejection...";
            
            AddToLog("Starting calibration processing...");
            AddToLog($"Rejection threshold: {RejectionThreshold} sigma");
            
            await Task.Run(() =>
            {
                // Process Pitch data
                if (PitchData.IsLoaded && PitchData.PointCount > 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddToLog($"Processing Pitch data ({PitchData.PointCount} points)...");
                        ProcessingProgress = 10;
                    });
                    
                    var pitchResult = _processor.Process(PitchData, RejectionThreshold);
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        PitchProcessingResult = pitchResult;
                        ProcessingProgress = 40;
                        
                        foreach (var step in pitchResult.Steps)
                        {
                            AddToLog($"  [Pitch] {step}");
                        }
                        
                        if (pitchResult.Success)
                        {
                            AddToLog($"✓ Pitch complete: {pitchResult.Summary}");
                        }
                        else
                        {
                            foreach (var error in pitchResult.Errors)
                            {
                                AddToLog($"✗ Pitch error: {error}");
                            }
                        }
                    });
                }
                
                // Process Roll data
                if (RollData.IsLoaded && RollData.PointCount > 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        AddToLog($"Processing Roll data ({RollData.PointCount} points)...");
                        ProcessingProgress = 50;
                    });
                    
                    var rollResult = _processor.Process(RollData, RejectionThreshold);
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        RollProcessingResult = rollResult;
                        ProcessingProgress = 90;
                        
                        foreach (var step in rollResult.Steps)
                        {
                            AddToLog($"  [Roll] {step}");
                        }
                        
                        if (rollResult.Success)
                        {
                            AddToLog($"✓ Roll complete: {rollResult.Summary}");
                        }
                        else
                        {
                            foreach (var error in rollResult.Errors)
                            {
                                AddToLog($"✗ Roll error: {error}");
                            }
                        }
                    });
                }
            });
            
            ProcessingProgress = 100;
            AddToLog("Processing complete.");
            
            Session.MarkModified();
            
            // Update status
            var summaryParts = new List<string>();
            if (PitchProcessingResult?.Success == true)
                summaryParts.Add($"Pitch: {PitchProcessingResult.AcceptedPoints}/{PitchProcessingResult.TotalPoints}");
            if (RollProcessingResult?.Success == true)
                summaryParts.Add($"Roll: {RollProcessingResult.AcceptedPoints}/{RollProcessingResult.TotalPoints}");
            
            StatusMessage = $"Processing complete - {string.Join(", ", summaryParts)}";
            
            // Update UI bindings
            OnPropertyChanged(nameof(PitchData));
            OnPropertyChanged(nameof(RollData));
            OnPropertyChanged(nameof(IsProcessed));
            OnPropertyChanged(nameof(PitchStatsSummary));
            OnPropertyChanged(nameof(RollStatsSummary));
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            AddToLog($"✗ Processing failed: {ex.Message}");
            StatusMessage = $"Processing error: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Processing failed:\n\n{ex.Message}",
                "Processing Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
    
    private async Task ReprocessDataAsync()
    {
        if (!IsProcessed) return;
        
        try
        {
            IsBusy = true;
            ProcessingLog.Clear();
            ProcessingProgress = 0;
            
            BusyMessage = "Reprocessing with new threshold...";
            AddToLog($"Reprocessing with threshold: {RejectionThreshold} sigma");
            
            await Task.Run(() =>
            {
                if (PitchData.IsLoaded && PitchData.PointCount > 0)
                {
                    var pitchResult = _processor.Reprocess(PitchData, RejectionThreshold);
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        PitchProcessingResult = pitchResult;
                        ProcessingProgress = 50;
                        AddToLog($"✓ Pitch reprocessed: {pitchResult.Summary}");
                    });
                }
                
                if (RollData.IsLoaded && RollData.PointCount > 0)
                {
                    var rollResult = _processor.Reprocess(RollData, RejectionThreshold);
                    
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        RollProcessingResult = rollResult;
                        ProcessingProgress = 100;
                        AddToLog($"✓ Roll reprocessed: {rollResult.Summary}");
                    });
                }
            });
            
            Session.MarkModified();
            StatusMessage = "Reprocessing complete";
            
            OnPropertyChanged(nameof(PitchData));
            OnPropertyChanged(nameof(RollData));
            OnPropertyChanged(nameof(PitchStatsSummary));
            OnPropertyChanged(nameof(RollStatsSummary));
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            AddToLog($"✗ Reprocessing failed: {ex.Message}");
            StatusMessage = $"Reprocessing error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
    
    private void AddToLog(string message)
    {
        ProcessingLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
    
    #endregion
    
    #region Analysis Methods (Step 5)
    
    private void GenerateCharts()
    {
        if (!IsProcessed) return;
        
        try
        {
            StatusMessage = "Generating analysis charts...";
            
            // Generate Pitch charts
            if (PitchData.IsProcessed)
            {
                PitchCalibrationChart = _chartService.CreateCalibrationChart(PitchData, "Pitch Calibration");
                PitchCOChart = _chartService.CreateCOChart(PitchData, "Pitch");
                PitchHistogram = _chartService.CreateHistogram(PitchData, "Pitch");
                
                // Additional charts
                PitchCorrelationChart = _chartService.CreateCorrelationScatterPlot(PitchData, "Pitch");
                PitchTrendChart = _chartService.CreateResidualTrendPlot(PitchData, "Pitch");
                PitchCDFChart = _chartService.CreateCDFChart(PitchData, "Pitch");
                PitchMovingAvgChart = _chartService.CreateMovingAverageChart(PitchData, "Pitch", MovingAverageWindow);
                
                // NEW v2.3.0 charts
                PitchQQChart = _chartService.CreateQQPlot(PitchData, "Pitch");
                PitchAutocorrelationChart = _chartService.CreateAutocorrelationPlot(PitchData, "Pitch");
                PitchBeforeAfterChart = _chartService.CreateBeforeAfterChart(PitchData, "Pitch");
            }
            
            // Generate Roll charts
            if (RollData.IsProcessed)
            {
                RollCalibrationChart = _chartService.CreateCalibrationChart(RollData, "Roll Calibration");
                RollCOChart = _chartService.CreateCOChart(RollData, "Roll");
                RollHistogram = _chartService.CreateHistogram(RollData, "Roll");
                
                // Additional charts
                RollCorrelationChart = _chartService.CreateCorrelationScatterPlot(RollData, "Roll");
                RollTrendChart = _chartService.CreateResidualTrendPlot(RollData, "Roll");
                RollCDFChart = _chartService.CreateCDFChart(RollData, "Roll");
                RollMovingAvgChart = _chartService.CreateMovingAverageChart(RollData, "Roll", MovingAverageWindow);
                
                // NEW v2.3.0 charts
                RollQQChart = _chartService.CreateQQPlot(RollData, "Roll");
                RollAutocorrelationChart = _chartService.CreateAutocorrelationPlot(RollData, "Roll");
                RollBeforeAfterChart = _chartService.CreateBeforeAfterChart(RollData, "Roll");
            }
            
            // Generate combined charts
            if (PitchData.IsProcessed && RollData.IsProcessed)
            {
                BoxPlotChart = _chartService.CreateBoxPlot(PitchData, RollData);
                PitchRollCorrelationChart = _chartService.CreatePitchRollCorrelationChart(PitchData, RollData);
            }
            
            StatusMessage = "Charts generated successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Chart generation error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Chart error: {ex}");
        }
    }
    
    private void GenerateSelectedCharts()
    {
        // Regenerate charts based on selected chart type
        GenerateCharts();
    }
    
    private void RegenerateMovingAverageCharts()
    {
        if (!IsProcessed) return;
        
        try
        {
            if (PitchData.IsProcessed)
            {
                PitchMovingAvgChart = _chartService.CreateMovingAverageChart(PitchData, "Pitch", MovingAverageWindow);
            }
            if (RollData.IsProcessed)
            {
                RollMovingAvgChart = _chartService.CreateMovingAverageChart(RollData, "Roll", MovingAverageWindow);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Moving average chart error: {ex}");
        }
    }
    
    #region Chart Export Methods (v2.3.0)
    
    private void ExportChartsToPng()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder to save chart images",
            ShowNewFolderButton = true
        };
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Exporting charts to PNG...";
                
                var charts = GetAllChartsForExport();
                int exported = _chartExportService.ExportAllChartsToPng(charts, dialog.SelectedPath, useLightTheme: true);
                
                StatusMessage = $"Exported {exported} charts to PNG";
                System.Windows.MessageBox.Show($"Successfully exported {exported} charts to:\n{dialog.SelectedPath}", 
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
                System.Windows.MessageBox.Show($"Failed to export charts: {ex.Message}", 
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private void ExportChartsToPdf()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder to save chart PDFs",
            ShowNewFolderButton = true
        };
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Exporting charts to PDF...";
                
                var charts = GetAllChartsForExport();
                int exported = _chartExportService.ExportAllChartsToPdf(charts, dialog.SelectedPath);
                
                StatusMessage = $"Exported {exported} charts to PDF";
                System.Windows.MessageBox.Show($"Successfully exported {exported} charts to:\n{dialog.SelectedPath}", 
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
                System.Windows.MessageBox.Show($"Failed to export charts: {ex.Message}", 
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private Dictionary<string, OxyPlot.PlotModel> GetAllChartsForExport()
    {
        var charts = new Dictionary<string, OxyPlot.PlotModel>();
        
        // Pitch charts
        if (PitchCalibrationChart != null) charts["Pitch_Calibration"] = PitchCalibrationChart;
        if (PitchCOChart != null) charts["Pitch_CO_Analysis"] = PitchCOChart;
        if (PitchHistogram != null) charts["Pitch_Histogram"] = PitchHistogram;
        if (PitchCorrelationChart != null) charts["Pitch_Correlation"] = PitchCorrelationChart;
        if (PitchTrendChart != null) charts["Pitch_Trend"] = PitchTrendChart;
        if (PitchCDFChart != null) charts["Pitch_CDF"] = PitchCDFChart;
        if (PitchMovingAvgChart != null) charts["Pitch_MovingAverage"] = PitchMovingAvgChart;
        if (PitchQQChart != null) charts["Pitch_QQ_Plot"] = PitchQQChart;
        if (PitchAutocorrelationChart != null) charts["Pitch_Autocorrelation"] = PitchAutocorrelationChart;
        if (PitchBeforeAfterChart != null) charts["Pitch_BeforeAfter"] = PitchBeforeAfterChart;
        
        // Roll charts
        if (RollCalibrationChart != null) charts["Roll_Calibration"] = RollCalibrationChart;
        if (RollCOChart != null) charts["Roll_CO_Analysis"] = RollCOChart;
        if (RollHistogram != null) charts["Roll_Histogram"] = RollHistogram;
        if (RollCorrelationChart != null) charts["Roll_Correlation"] = RollCorrelationChart;
        if (RollTrendChart != null) charts["Roll_Trend"] = RollTrendChart;
        if (RollCDFChart != null) charts["Roll_CDF"] = RollCDFChart;
        if (RollMovingAvgChart != null) charts["Roll_MovingAverage"] = RollMovingAvgChart;
        if (RollQQChart != null) charts["Roll_QQ_Plot"] = RollQQChart;
        if (RollAutocorrelationChart != null) charts["Roll_Autocorrelation"] = RollAutocorrelationChart;
        if (RollBeforeAfterChart != null) charts["Roll_BeforeAfter"] = RollBeforeAfterChart;
        
        // Combined charts
        if (BoxPlotChart != null) charts["Combined_BoxPlot"] = BoxPlotChart;
        if (PitchRollCorrelationChart != null) charts["PitchRoll_Correlation"] = PitchRollCorrelationChart;
        
        return charts;
    }
    
    private void ShowHelpDialog()
    {
        try
        {
            var helpDialog = new FathomOS.Modules.MruCalibration.Views.HelpDialog(System.Windows.Application.Current.MainWindow);
            helpDialog.ShowDialog();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Help dialog error: {ex}");
        }
    }
    
    #endregion
    
    private void RegenerateAllCharts()
    {
        if (!IsProcessed) return;
        GenerateCharts();
    }
    
    private void ApplyColorScheme(string schemeName)
    {
        ChartColorSettings newSettings = schemeName switch
        {
            "High Contrast" => ChartColorSettings.HighContrast,
            "Color Blind Friendly" => ChartColorSettings.ColorBlindFriendly,
            "Monochrome" => ChartColorSettings.Monochrome,
            _ => new ChartColorSettings()
        };
        
        _chartColorSettings = newSettings;
        _chartService.ApplyColorSettings(newSettings);
        OnPropertyChanged(nameof(ChartColorSettings));
        
        // Regenerate all charts with new colors
        if (IsProcessed)
        {
            GenerateCharts();
        }
        
        StatusMessage = $"Applied color scheme: {schemeName}";
    }
    
    private void ResetColors()
    {
        _chartColorSettings = new ChartColorSettings();
        _chartService.ApplyColorSettings(_chartColorSettings);
        SelectedColorScheme = "Default";
        OnPropertyChanged(nameof(ChartColorSettings));
        
        if (IsProcessed)
        {
            GenerateCharts();
        }
        
        StatusMessage = "Colors reset to defaults";
    }
    
    /// <summary>
    /// Auto-generate charts when navigating to Step 5
    /// </summary>
    private void OnStepChanged()
    {
        if (CurrentStep == 5 && IsProcessed)
        {
            // Auto-generate charts if not already generated
            if (PitchCalibrationChart == null && RollCalibrationChart == null)
            {
                GenerateCharts();
            }
        }
    }
    
    #endregion
    
    #region Sign-off Methods (Step 6)
    
    private void SetPitchDecision(AcceptanceDecision decision)
    {
        PitchData.Decision = decision;
        PitchData.AcceptedCOValue = PitchData.Statistics?.MeanCO_Accepted;
        PitchData.SignOffDateTime = DateTime.Now;
        
        Session.MarkModified();
        
        StatusMessage = $"Pitch calibration {decision.ToString().ToLower()}";
        
        OnPropertyChanged(nameof(PitchData));
        OnPropertyChanged(nameof(IsFullySignedOff));
        OnPropertyChanged(nameof(CanExport));
        CommandManager.InvalidateRequerySuggested();
    }
    
    private void SetRollDecision(AcceptanceDecision decision)
    {
        RollData.Decision = decision;
        RollData.AcceptedCOValue = RollData.Statistics?.MeanCO_Accepted;
        RollData.SignOffDateTime = DateTime.Now;
        
        Session.MarkModified();
        
        StatusMessage = $"Roll calibration {decision.ToString().ToLower()}";
        
        OnPropertyChanged(nameof(RollData));
        OnPropertyChanged(nameof(IsFullySignedOff));
        OnPropertyChanged(nameof(CanExport));
        CommandManager.InvalidateRequerySuggested();
    }
    
    #endregion
    
    #region Export Methods (Step 7)
    
    private async Task ExportPdfAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export PDF Report",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"MRU_Calibration_{Session.ProjectInfo.VesselName}_{DateTime.Now:yyyyMMdd}.pdf"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            BusyMessage = "Generating PDF report...";
            StatusMessage = "Exporting PDF...";
            
            await Task.Run(() =>
            {
                _reportService.GenerateReport(dialog.FileName, Session);
            });
            
            LastExportPath = dialog.FileName;
            StatusMessage = $"PDF exported: {System.IO.Path.GetFileName(dialog.FileName)}";
            
            // Ask to open
            var result = System.Windows.MessageBox.Show(
                $"PDF report saved to:\n{dialog.FileName}\n\nOpen file now?",
                "Export Complete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"PDF export error: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to export PDF:\n\n{ex.Message}",
                "Export Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
    
    private async Task ExportExcelAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Excel Data",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"MRU_Calibration_{Session.ProjectInfo.VesselName}_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            BusyMessage = "Generating Excel workbook...";
            StatusMessage = "Exporting Excel...";
            
            await Task.Run(() =>
            {
                _excelService.ExportToExcel(dialog.FileName, Session);
            });
            
            LastExportPath = dialog.FileName;
            StatusMessage = $"Excel exported: {System.IO.Path.GetFileName(dialog.FileName)}";
            
            // Ask to open
            var result = System.Windows.MessageBox.Show(
                $"Excel workbook saved to:\n{dialog.FileName}\n\nOpen file now?",
                "Export Complete",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Excel export error: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to export Excel:\n\n{ex.Message}",
                "Export Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
    
    private async Task ExportAllAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Export Folder"
        };
        
        // Fallback for older .NET versions without OpenFolderDialog
        string? folderPath = null;
        
        try
        {
            if (dialog.ShowDialog() == true)
            {
                folderPath = dialog.FolderName;
            }
        }
        catch
        {
            // Fallback - use Save dialog to get folder
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Select Export Folder (filename will be auto-generated)",
                Filter = "All Files (*.*)|*.*",
                FileName = "SelectFolder"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                folderPath = System.IO.Path.GetDirectoryName(saveDialog.FileName);
            }
        }
        
        if (string.IsNullOrEmpty(folderPath)) return;
        
        try
        {
            IsBusy = true;
            BusyMessage = "Exporting all files...";
            StatusMessage = "Exporting PDF and Excel...";
            
            string baseName = $"MRU_Calibration_{Session.ProjectInfo.VesselName}_{DateTime.Now:yyyyMMdd}";
            string pdfPath = System.IO.Path.Combine(folderPath, $"{baseName}.pdf");
            string xlsxPath = System.IO.Path.Combine(folderPath, $"{baseName}.xlsx");
            
            await Task.Run(() =>
            {
                _reportService.GenerateReport(pdfPath, Session);
                _excelService.ExportToExcel(xlsxPath, Session);
            });
            
            LastExportPath = folderPath;
            StatusMessage = $"Exported to: {folderPath}";
            
            System.Windows.MessageBox.Show(
                $"Files exported successfully:\n\n• {System.IO.Path.GetFileName(pdfPath)}\n• {System.IO.Path.GetFileName(xlsxPath)}",
                "Export Complete",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to export files:\n\n{ex.Message}",
                "Export Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = string.Empty;
        }
    }
    
    /// <summary>
    /// Generate calibration certificate (Fathom OS v3.0.0)
    /// Opens signatory dialog and creates certificate request for FathomOS.Core.Certificates
    /// </summary>
    private void GenerateCertificate()
    {
        try
        {
            // Show signatory dialog
            var dialog = new Views.SignatoryDialog(
                Session.ProjectInfo.ProjectTitle,
                Session.ProjectInfo.VesselName);
            
            dialog.Owner = System.Windows.Application.Current.MainWindow;
            
            if (dialog.ShowDialog() != true || dialog.Result == null)
            {
                return; // User cancelled
            }
            
            var signatory = dialog.Result;
            
            // Build processing data for certificate
            var processingData = MruCertificateDataBuilder.BuildProcessingData(
                calibrationType: Session.ProjectInfo.Purpose == CalibrationPurpose.Calibration ? "Calibration" : "Verification",
                pitchStats: PitchData.Statistics!,
                rollStats: RollData.Statistics!,
                equipment: signatory.Equipment,
                serialNumber: signatory.EquipmentSerial);
            
            // Create certificate request (ready for FathomOS.Core.Certificates integration)
            var certificateRequest = new MruCertificateRequest
            {
                ModuleId = "MruCalibration",
                ProjectName = signatory.ProjectName,
                ProjectLocation = signatory.ProjectLocation,
                Vessel = signatory.Vessel,
                Equipment = signatory.Equipment,
                EquipmentSerial = signatory.EquipmentSerial,
                SignatoryName = signatory.SignatoryName,
                SignatoryTitle = signatory.SignatoryTitle,
                SignatoryCredentials = signatory.SignatoryCredentials,
                ProcessingData = processingData,
                InputFiles = GetInputFileList(),
                OutputFiles = GetOutputFileList()
            };
            
            // TODO: When FathomOS.Core.Certificates is available, call:
            // var certificate = await CertificateService.CreateAsync(certificateRequest);
            // await CertificateService.ShowCertificateDialogAsync(certificate);
            
            // For now, show confirmation with certificate data preview
            var previewText = BuildCertificatePreview(certificateRequest);
            
            System.Windows.MessageBox.Show(
                previewText,
                "Certificate Preview (Fathom OS Integration Pending)",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            
            StatusMessage = "Certificate request prepared - awaiting Fathom OS Certificate Service integration";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to generate certificate:\n\n{ex.Message}",
                "Certificate Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
    
    private List<CertificateFile> GetInputFileList()
    {
        var files = new List<CertificateFile>();
        
        if (!string.IsNullOrEmpty(SelectedFilePath))
        {
            files.Add(new CertificateFile
            {
                FileName = System.IO.Path.GetFileName(SelectedFilePath),
                FileHash = ComputeMD5(SelectedFilePath)
            });
        }
        
        return files;
    }
    
    private List<CertificateFile> GetOutputFileList()
    {
        var files = new List<CertificateFile>();
        
        if (!string.IsNullOrEmpty(LastExportPath))
        {
            string baseName = $"MRU_Calibration_{Session.ProjectInfo.VesselName}_{DateTime.Now:yyyyMMdd}";
            files.Add(new CertificateFile { FileName = $"{baseName}.pdf" });
            files.Add(new CertificateFile { FileName = $"{baseName}.xlsx" });
        }
        
        return files;
    }
    
    private string? ComputeMD5(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            using var md5 = System.Security.Cryptography.MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }
    
    private string BuildCertificatePreview(MruCertificateRequest request)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine("    MRU CALIBRATION VERIFICATION CERTIFICATE");
        sb.AppendLine("               (Preview Mode)");
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"Project: {request.ProjectName}");
        if (!string.IsNullOrEmpty(request.ProjectLocation))
            sb.AppendLine($"Location: {request.ProjectLocation}");
        if (!string.IsNullOrEmpty(request.Vessel))
            sb.AppendLine($"Vessel: {request.Vessel}");
        sb.AppendLine();
        sb.AppendLine("─────────── PROCESSING DATA ───────────");
        foreach (var kvp in request.ProcessingData)
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }
        sb.AppendLine();
        sb.AppendLine("─────────── SIGNATORY ───────────");
        sb.AppendLine($"  Name: {request.SignatoryName}");
        sb.AppendLine($"  Title: {request.SignatoryTitle}");
        if (!string.IsNullOrEmpty(request.SignatoryCredentials))
            sb.AppendLine($"  Credentials: {request.SignatoryCredentials}");
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════");
        sb.AppendLine("Certificate will be generated when");
        sb.AppendLine("FathomOS.Core.Certificates service is integrated.");
        sb.AppendLine("═══════════════════════════════════════════");
        
        return sb.ToString();
    }
    
    #endregion
}

#region Supporting Classes

/// <summary>
/// Definition of a wizard step
/// </summary>
public record StepDefinition(int Number, string Name, string Description, string Icon)
{
    public string DisplayName => $"{Number}. {Name}";
}

/// <summary>
/// Item for data preview grid
/// </summary>
public class ColumnPreviewItem
{
    public int RowNumber { get; set; }
    public Dictionary<string, string> Values { get; set; } = new();
}

#endregion
