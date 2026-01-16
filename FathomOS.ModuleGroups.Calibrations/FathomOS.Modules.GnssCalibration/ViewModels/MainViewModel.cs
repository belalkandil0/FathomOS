using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using FathomOS.Modules.GnssCalibration.Models;
using FathomOS.Modules.GnssCalibration.Services;

namespace FathomOS.Modules.GnssCalibration.ViewModels;

/// <summary>
/// Represents a step in the 7-step wizard with property change notification.
/// </summary>
public class WizardStep : INotifyPropertyChanged
{
    private bool _isActive;
    private bool _isCompleted;
    
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    
    public bool IsCompleted 
    { 
        get => _isCompleted;
        set { _isCompleted = value; OnPropertyChanged(); }
    }
    
    public bool IsActive 
    { 
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); }
    }
    
    public bool CanNavigate { get; set; } = true;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Main ViewModel controlling the 7-step wizard workflow.
/// </summary>
public class MainViewModel : ViewModelBase
{
    #region Fields

    private int _currentStepIndex = 1;
    private WizardStep? _currentStep;
    private GnssProject _project = new();
    private ObservableCollection<GnssComparisonPoint> _comparisonPoints = new();
    private GnssStatisticsResult? _statistics;
    private string _statusMessage = "Ready";
    private bool _isBusy;
    private bool _canGoNext;
    private bool _canGoBack;
    
    // Services
    private readonly DataProcessingService _processingService;
    private readonly ProjectFileService _projectFileService;
    
    // Settings and History
    private ModuleSettings _settings = null!;
    private ComparisonHistory _comparisonHistory = null!;
    private string? _currentProjectFilePath;
    private bool _hasUnsavedChanges;
    
    // Selected point for highlighting
    private GnssComparisonPoint? _selectedPoint;
    private int _selectedPointIndex = -1;
    
    // Batch processing
    private ObservableCollection<BatchFileItem> _batchFiles = new();
    private bool _isBatchProcessing;
    private int _batchProgress;
    private string _batchStatusMessage = "";
    
    // Column mapping
    private ObservableCollection<string> _availableColumns = new();
    private string? _selectedSystem1EastingColumn;
    private string? _selectedSystem1NorthingColumn;
    private string? _selectedSystem1HeightColumn;
    private string? _selectedSystem2EastingColumn;
    private string? _selectedSystem2NorthingColumn;
    private string? _selectedSystem2HeightColumn;
    
    // CRITICAL: Maps unique column display names to their actual indices
    // This fixes the duplicate column name bug where IndexOf returns first match
    private readonly Dictionary<string, int> _columnNameToIndex = new();
    
    // Processing results
    private FilterResult? _filterResult;
    private bool _dataLoaded;
    private bool _dataProcessed;
    
    // Charts
    private PlotModel? _scatterPlotModel;
    private PlotModel? _deltaScatterPlotModel;
    private PlotModel? _rawPositionPlotModel;
    private PlotModel? _filteredPositionPlotModel;
    private PlotModel? _timeSeriesPlotModel;
    private PlotModel? _histogramPlotModel;
    private PlotModel? _cdfPlotModel;
    private PlotModel? _heightAnalysisPlotModel;
    private PlotModel? _polarErrorPlotModel;
    private PlotModel? _errorComponentsPlotModel;
    private PlotModel? _qqPlotModel;
    private PlotModel? _runningStatsPlotModel;
    private PlotModel? _errorEllipsePlotModel;
    
    // Theme
    private bool _isDarkTheme = true;
    
    // Time filter
    private bool _timeFilterEnabled;
    private DateTime? _filterStartTime;
    private DateTime? _filterEndTime;
    private DateTime? _dataStartTime;  // Full data range start
    private DateTime? _dataEndTime;    // Full data range end
    
    // Time slider values (ticks converted to double for slider binding)
    private double _timeSliderMinimum;
    private double _timeSliderMaximum = 100;
    private double _timeSliderStart;
    private double _timeSliderEnd = 100;
    private bool _isLoadingAllData;
    private bool _isSettingColumnsFromDetection;
    
    // Data preview
    private ObservableCollection<DataPreviewRow> _dataPreview = new();
    
    // Row selection for filtering
    private int _startRowIndex = 1;
    private int _endRowIndex = -1; // -1 means last row
    private int _totalDataRows;
    private DataPreviewRow? _selectedStartRow;
    private DataPreviewRow? _selectedEndRow;
    
    // Chart color preset (0=Professional, 1=Vibrant, 2=Ocean, 3=Earth)
    private int _chartColorPresetIndex;
    
    // Data preview confirmation (Step 2)
    private bool _dataPreviewConfirmed;
    
    // Supervisor approval (Step 7)
    private string _supervisorName = "";
    private string _supervisorInitials = "";
    private bool _isApproved;
    private DateTime? _approvalDateTime;

    #endregion

    #region Constructor

    public MainViewModel()
    {
        // Initialize services
        _processingService = new DataProcessingService();
        _projectFileService = new ProjectFileService();
        
        // Load settings and history
        _settings = ModuleSettings.Load();
        _comparisonHistory = new ComparisonHistory();
        
        // Apply settings to project defaults
        ApplySettingsToProject();
        
        // Initialize wizard steps
        InitializeSteps();
        
        // Subscribe to Project property changes for navigation updates
        _project.PropertyChanged += OnProjectPropertyChanged;
        
        // Initialize commands
        NextStepCommand = new RelayCommand(_ => GoToNextStep(), _ => CanGoNext && !IsBusy);
        PreviousStepCommand = new RelayCommand(_ => GoToPreviousStep(), _ => CanGoBack && !IsBusy);
        GoToStepCommand = new RelayCommand<int>(step => GoToStep(step), step => CanGoToStep(step));
        LoadFileCommand = new RelayCommand<string>(file => LoadFile(file));
        NewProjectCommand = new RelayCommand(_ => NewProject());
        
        // Theme and Help commands
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        ShowHelpCommand = new RelayCommand(_ => ShowStepHelp());
        
        // Time filter commands
        ApplyTimeFilterCommand = new RelayCommand(_ => ApplyTimeFilter(), _ => TimeFilterEnabled && FilterStartTime.HasValue);
        ClearTimeFilterCommand = new RelayCommand(_ => ClearTimeFilter());
        
        // Data processing commands
        BrowseFileCommand = new RelayCommand(_ => BrowseForFile());
        BrowsePosFileCommand = new RelayCommand(_ => BrowseForPosFile());
        ParseFileCommand = new RelayCommand(_ => ParseDataFile(), _ => !string.IsNullOrEmpty(Project.NpdFilePath) && !IsBusy);
        ProcessDataCommand = new RelayCommand(_ => ProcessData(), _ => CanProcessData());
        ApplyFilterCommand = new RelayCommand(_ => ApplyFilter(), _ => _dataLoaded && !IsBusy);
        AcceptAllCommand = new RelayCommand(_ => AcceptAllPoints(), _ => ComparisonPoints.Count > 0);
        RejectFlaggedCommand = new RelayCommand(_ => RejectFlaggedPoints(), _ => ComparisonPoints.Count > 0);
        RecalculateCommand = new RelayCommand(_ => RecalculateStatistics(), _ => ComparisonPoints.Count > 0);
        
        // Export commands
        ExportExcelCommand = new RelayCommand(_ => ExportToExcel(), _ => Statistics != null && !IsBusy);
        ExportPdfCommand = new RelayCommand(_ => ExportToPdf(), _ => Statistics != null && !IsBusy);
        ExportFullDataPdfCommand = new RelayCommand(_ => ExportFullDataPdf(), _ => ComparisonPoints.Count > 0 && !IsBusy);
        ExportAllCommand = new RelayCommand(_ => ExportAll(), _ => Statistics != null && !IsBusy);
        ExportChartsCommand = new RelayCommand(_ => ExportCharts(), _ => DeltaScatterPlotModel != null && !IsBusy);
        
        // Project file commands
        SaveProjectCommand = new RelayCommand(_ => SaveProject(), _ => !IsBusy);
        SaveProjectAsCommand = new RelayCommand(_ => SaveProjectAs(), _ => !IsBusy);
        OpenProjectCommand = new RelayCommand(_ => OpenProject(), _ => !IsBusy);
        
        // Batch processing commands
        AddBatchFileCommand = new RelayCommand(_ => AddBatchFile(), _ => !IsBatchProcessing);
        RemoveBatchFileCommand = new RelayCommand(_ => RemoveSelectedBatchFile(), _ => !IsBatchProcessing && BatchFiles.Count > 0);
        ClearBatchFilesCommand = new RelayCommand(_ => ClearBatchFiles(), _ => !IsBatchProcessing && BatchFiles.Count > 0);
        RunBatchProcessCommand = new RelayCommand(_ => RunBatchProcess(), _ => !IsBatchProcessing && BatchFiles.Count > 0);
        
        // History commands
        ClearHistoryCommand = new RelayCommand(_ => ClearHistory(), _ => ComparisonHistory.Entries.Count > 0);
        
        // Point selection command
        SelectPointCommand = new RelayCommand<GnssComparisonPoint>(point => SelectPoint(point));
        
        // Complete/Supervisor confirmation command
        CompleteCommand = new RelayCommand(_ => ShowCompletionConfirmation(), _ => Statistics != null && CurrentStepIndex == 6);
        
        // Data preview popup command
        ShowDataPreviewCommand = new RelayCommand(_ => ShowDataPreviewPopup(), _ => ComparisonPoints.Count > 0 || DataPreview.Count > 0);
        
        // Completion popup command for Step 7
        ShowCompletionPopupCommand = new RelayCommand(_ => ShowCompletionPopup(), _ => Statistics != null && CurrentStepIndex == 7);
        
        // Supervisor approval command
        ApproveResultsCommand = new RelayCommand(_ => ApproveResults(), _ => CanApprove);
        
        UpdateNavigationState();
    }
    
    /// <summary>
    /// Applies saved settings to the project defaults.
    /// </summary>
    private void ApplySettingsToProject()
    {
        Project.SigmaThreshold = _settings.DefaultSigmaThreshold;
        Project.ToleranceValue = _settings.DefaultToleranceValue;
        Project.ToleranceCheckEnabled = _settings.DefaultToleranceEnabled;
        Project.Unit = (LengthUnit)_settings.DefaultUnitIndex;
        
        // Apply theme from settings
        IsDarkTheme = _settings.DarkMode;
    }
    
    /// <summary>
    /// Saves current settings from project to persistent storage.
    /// </summary>
    public void SaveSettings()
    {
        _settings.DefaultSigmaThreshold = Project.SigmaThreshold;
        _settings.DefaultToleranceValue = Project.ToleranceValue;
        _settings.DefaultToleranceEnabled = Project.ToleranceCheckEnabled;
        _settings.DefaultUnitIndex = (int)Project.Unit;
        _settings.DarkMode = IsDarkTheme;
        _settings.Save();
    }
    
    private void OnProjectPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update navigation state when ANY Project property changes
        // This ensures validation is always rechecked
        UpdateNavigationState();
        _hasUnsavedChanges = true;
    }

    #endregion

    #region Properties

    /// <summary>The 7 wizard steps.</summary>
    public ObservableCollection<WizardStep> Steps { get; } = new();

    /// <summary>Current step index (1-7).</summary>
    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        set
        {
            if (SetProperty(ref _currentStepIndex, value))
            {
                UpdateCurrentStep();
                UpdateNavigationState();
                OnPropertyChanged(nameof(NextButtonText));
                OnPropertyChanged(nameof(NextButtonIcon));
            }
        }
    }

    /// <summary>Current active step.</summary>
    public WizardStep? CurrentStep
    {
        get => _currentStep;
        private set => SetProperty(ref _currentStep, value);
    }

    /// <summary>Project configuration and metadata.</summary>
    public GnssProject Project
    {
        get => _project;
        set
        {
            if (_project != null)
            {
                _project.PropertyChanged -= OnProjectPropertyChanged;
            }
            if (SetProperty(ref _project, value) && _project != null)
            {
                _project.PropertyChanged += OnProjectPropertyChanged;
                UpdateNavigationState();
            }
        }
    }

    /// <summary>All comparison data points.</summary>
    public ObservableCollection<GnssComparisonPoint> ComparisonPoints
    {
        get => _comparisonPoints;
        set => SetProperty(ref _comparisonPoints, value);
    }

    /// <summary>Calculated statistics.</summary>
    public GnssStatisticsResult? Statistics
    {
        get => _statistics;
        set
        {
            if (SetProperty(ref _statistics, value))
            {
                UpdateScatterPlot();
            }
        }
    }

    /// <summary>OxyPlot model for Delta E vs Delta N scatter chart.</summary>
    public PlotModel? ScatterPlotModel
    {
        get => _scatterPlotModel;
        set => SetProperty(ref _scatterPlotModel, value);
    }
    
    /// <summary>OxyPlot model for Delta scatter with 2DRMS circle.</summary>
    public PlotModel? DeltaScatterPlotModel
    {
        get => _deltaScatterPlotModel;
        set => SetProperty(ref _deltaScatterPlotModel, value);
    }
    
    /// <summary>OxyPlot model for raw position comparison.</summary>
    public PlotModel? RawPositionPlotModel
    {
        get => _rawPositionPlotModel;
        set => SetProperty(ref _rawPositionPlotModel, value);
    }
    
    /// <summary>OxyPlot model for filtered position comparison.</summary>
    public PlotModel? FilteredPositionPlotModel
    {
        get => _filteredPositionPlotModel;
        set => SetProperty(ref _filteredPositionPlotModel, value);
    }
    
    /// <summary>OxyPlot model for time series.</summary>
    public PlotModel? TimeSeriesPlotModel
    {
        get => _timeSeriesPlotModel;
        set => SetProperty(ref _timeSeriesPlotModel, value);
    }
    
    /// <summary>OxyPlot model for histogram.</summary>
    public PlotModel? HistogramPlotModel
    {
        get => _histogramPlotModel;
        set => SetProperty(ref _histogramPlotModel, value);
    }
    
    /// <summary>OxyPlot model for CDF (Cumulative Distribution Function).</summary>
    public PlotModel? CdfPlotModel
    {
        get => _cdfPlotModel;
        set => SetProperty(ref _cdfPlotModel, value);
    }
    
    /// <summary>OxyPlot model for height analysis.</summary>
    public PlotModel? HeightAnalysisPlotModel
    {
        get => _heightAnalysisPlotModel;
        set => SetProperty(ref _heightAnalysisPlotModel, value);
    }
    
    /// <summary>OxyPlot model for polar error plot.</summary>
    public PlotModel? PolarErrorPlotModel
    {
        get => _polarErrorPlotModel;
        set => SetProperty(ref _polarErrorPlotModel, value);
    }
    
    /// <summary>OxyPlot model for error components box plot.</summary>
    public PlotModel? ErrorComponentsPlotModel
    {
        get => _errorComponentsPlotModel;
        set => SetProperty(ref _errorComponentsPlotModel, value);
    }
    
    /// <summary>OxyPlot model for Q-Q probability plot.</summary>
    public PlotModel? QqPlotModel
    {
        get => _qqPlotModel;
        set => SetProperty(ref _qqPlotModel, value);
    }
    
    /// <summary>OxyPlot model for running statistics.</summary>
    public PlotModel? RunningStatsPlotModel
    {
        get => _runningStatsPlotModel;
        set => SetProperty(ref _runningStatsPlotModel, value);
    }
    
    /// <summary>OxyPlot model for error ellipse.</summary>
    public PlotModel? ErrorEllipsePlotModel
    {
        get => _errorEllipsePlotModel;
        set => SetProperty(ref _errorEllipsePlotModel, value);
    }
    
    #region Theme Colors for Tables
    
    /// <summary>Table header background color based on theme.</summary>
    public System.Windows.Media.SolidColorBrush TableHeaderBackground
    {
        get
        {
            var colors = GetChartColors();
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(colors.System1.R, colors.System1.G, colors.System1.B));
        }
    }
    
    /// <summary>Table accent color based on theme.</summary>
    public System.Windows.Media.SolidColorBrush TableAccentColor
    {
        get
        {
            var colors = GetChartColors();
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(colors.Circle.R, colors.Circle.G, colors.Circle.B));
        }
    }
    
    /// <summary>Accepted/success color for tables.</summary>
    public System.Windows.Media.SolidColorBrush TableSuccessColor
    {
        get
        {
            var colors = GetChartColors();
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(colors.Accepted.R, colors.Accepted.G, colors.Accepted.B));
        }
    }
    
    /// <summary>Rejected/error color for tables.</summary>
    public System.Windows.Media.SolidColorBrush TableErrorColor
    {
        get
        {
            var colors = GetChartColors();
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(colors.Rejected.R, colors.Rejected.G, colors.Rejected.B));
        }
    }
    
    #endregion
    
    /// <summary>Whether dark theme is active.</summary>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => SetProperty(ref _isDarkTheme, value);
    }
    
    /// <summary>Theme toggle tooltip.</summary>
    public string ThemeToggleTooltip => IsDarkTheme ? "Switch to Light Theme" : "Switch to Dark Theme";
    
    /// <summary>Whether time filter is enabled.</summary>
    public bool TimeFilterEnabled
    {
        get => _timeFilterEnabled;
        set => SetProperty(ref _timeFilterEnabled, value);
    }
    
    /// <summary>Filter start time.</summary>
    public DateTime? FilterStartTime
    {
        get => _filterStartTime;
        set
        {
            // Constrain to data range
            if (value.HasValue && _dataStartTime.HasValue && value < _dataStartTime)
                value = _dataStartTime;
            if (value.HasValue && _dataEndTime.HasValue && value > _dataEndTime)
                value = _dataEndTime;
            SetProperty(ref _filterStartTime, value);
        }
    }
    
    /// <summary>Filter end time.</summary>
    public DateTime? FilterEndTime
    {
        get => _filterEndTime;
        set
        {
            // Constrain to data range
            if (value.HasValue && _dataStartTime.HasValue && value < _dataStartTime)
                value = _dataStartTime;
            if (value.HasValue && _dataEndTime.HasValue && value > _dataEndTime)
                value = _dataEndTime;
            SetProperty(ref _filterEndTime, value);
        }
    }
    
    /// <summary>Data range start time (from loaded file).</summary>
    public DateTime? DataStartTime
    {
        get => _dataStartTime;
        set => SetProperty(ref _dataStartTime, value);
    }
    
    /// <summary>Data range end time (from loaded file).</summary>
    public DateTime? DataEndTime
    {
        get => _dataEndTime;
        set => SetProperty(ref _dataEndTime, value);
    }
    
    #region Time Range Slider
    
    /// <summary>Minimum value for time slider (start of data).</summary>
    public double TimeSliderMinimum
    {
        get => _timeSliderMinimum;
        set => SetProperty(ref _timeSliderMinimum, value);
    }
    
    /// <summary>Maximum value for time slider (end of data).</summary>
    public double TimeSliderMaximum
    {
        get => _timeSliderMaximum;
        set => SetProperty(ref _timeSliderMaximum, value);
    }
    
    /// <summary>Current start position on time slider.</summary>
    public double TimeSliderStart
    {
        get => _timeSliderStart;
        set
        {
            if (SetProperty(ref _timeSliderStart, value))
            {
                UpdateFilterTimesFromSlider();
            }
        }
    }
    
    /// <summary>Current end position on time slider.</summary>
    public double TimeSliderEnd
    {
        get => _timeSliderEnd;
        set
        {
            if (SetProperty(ref _timeSliderEnd, value))
            {
                UpdateFilterTimesFromSlider();
            }
        }
    }
    
    /// <summary>Whether data is currently being loaded.</summary>
    public bool IsLoadingAllData
    {
        get => _isLoadingAllData;
        set => SetProperty(ref _isLoadingAllData, value);
    }
    
    /// <summary>Display string for filter start time.</summary>
    public string FilterStartTimeDisplay => FilterStartTime?.ToString("HH:mm:ss") ?? "--:--:--";
    
    /// <summary>Display string for filter end time.</summary>
    public string FilterEndTimeDisplay => FilterEndTime?.ToString("HH:mm:ss") ?? "--:--:--";
    
    /// <summary>Display string for data duration.</summary>
    public string DataDurationDisplay
    {
        get
        {
            if (!DataStartTime.HasValue || !DataEndTime.HasValue)
                return "No data loaded";
            var duration = DataEndTime.Value - DataStartTime.Value;
            return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }
    
    /// <summary>Display string for filter duration.</summary>
    public string FilterDurationDisplay
    {
        get
        {
            if (!FilterStartTime.HasValue || !FilterEndTime.HasValue)
                return "--:--:--";
            var duration = FilterEndTime.Value - FilterStartTime.Value;
            return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }
    
    /// <summary>Number of points in current filter range.</summary>
    public int FilteredDataCount
    {
        get
        {
            if (DataPreview.Count == 0 || !FilterStartTime.HasValue || !FilterEndTime.HasValue)
                return 0;
            return DataPreview.Count(r => r.DateTime.HasValue && 
                r.DateTime >= FilterStartTime && r.DateTime <= FilterEndTime);
        }
    }
    
    /// <summary>Updates filter times based on slider positions.</summary>
    private void UpdateFilterTimesFromSlider()
    {
        if (!DataStartTime.HasValue || !DataEndTime.HasValue) return;
        
        var totalTicks = (DataEndTime.Value - DataStartTime.Value).Ticks;
        if (totalTicks <= 0) return;
        
        var startTicks = (long)((TimeSliderStart / 100.0) * totalTicks);
        var endTicks = (long)((TimeSliderEnd / 100.0) * totalTicks);
        
        _filterStartTime = DataStartTime.Value.AddTicks(startTicks);
        _filterEndTime = DataStartTime.Value.AddTicks(endTicks);
        
        RaisePropertyChanged(nameof(FilterStartTime));
        RaisePropertyChanged(nameof(FilterEndTime));
        RaisePropertyChanged(nameof(FilterStartTimeDisplay));
        RaisePropertyChanged(nameof(FilterEndTimeDisplay));
        RaisePropertyChanged(nameof(FilterDurationDisplay));
        RaisePropertyChanged(nameof(FilteredDataCount));
    }
    
    /// <summary>Updates slider positions based on filter times.</summary>
    private void UpdateSliderFromFilterTimes()
    {
        if (!DataStartTime.HasValue || !DataEndTime.HasValue) return;
        
        var totalTicks = (DataEndTime.Value - DataStartTime.Value).Ticks;
        if (totalTicks <= 0) return;
        
        if (FilterStartTime.HasValue)
        {
            var startTicks = (FilterStartTime.Value - DataStartTime.Value).Ticks;
            _timeSliderStart = Math.Max(0, Math.Min(100, (startTicks * 100.0) / totalTicks));
            RaisePropertyChanged(nameof(TimeSliderStart));
        }
        
        if (FilterEndTime.HasValue)
        {
            var endTicks = (FilterEndTime.Value - DataStartTime.Value).Ticks;
            _timeSliderEnd = Math.Max(0, Math.Min(100, (endTicks * 100.0) / totalTicks));
            RaisePropertyChanged(nameof(TimeSliderEnd));
        }
    }
    
    #endregion
    
    /// <summary>Data preview rows from loaded file.</summary>
    public ObservableCollection<DataPreviewRow> DataPreview
    {
        get => _dataPreview;
        set => SetProperty(ref _dataPreview, value);
    }
    
    /// <summary>Start row index for data processing (1-based).</summary>
    public int StartRowIndex
    {
        get => _startRowIndex;
        set
        {
            if (SetProperty(ref _startRowIndex, Math.Max(1, value)))
            {
                UpdateFilterFromRows();
            }
        }
    }
    
    /// <summary>End row index for data processing (1-based, -1 = last).</summary>
    public int EndRowIndex
    {
        get => _endRowIndex;
        set
        {
            if (SetProperty(ref _endRowIndex, value))
            {
                UpdateFilterFromRows();
            }
        }
    }
    
    /// <summary>Total rows in loaded data.</summary>
    public int TotalDataRows
    {
        get => _totalDataRows;
        set => SetProperty(ref _totalDataRows, value);
    }
    
    /// <summary>Selected start row in data preview.</summary>
    public DataPreviewRow? SelectedStartRow
    {
        get => _selectedStartRow;
        set
        {
            if (SetProperty(ref _selectedStartRow, value) && value != null)
            {
                StartRowIndex = value.Index;
            }
        }
    }
    
    /// <summary>Selected end row in data preview.</summary>
    public DataPreviewRow? SelectedEndRow
    {
        get => _selectedEndRow;
        set
        {
            if (SetProperty(ref _selectedEndRow, value) && value != null)
            {
                EndRowIndex = value.Index;
            }
        }
    }
    
    /// <summary>Display text for row range selection.</summary>
    public string RowRangeDisplay => EndRowIndex < 0 
        ? $"Rows {StartRowIndex} to {TotalDataRows} ({TotalDataRows - StartRowIndex + 1} rows)"
        : $"Rows {StartRowIndex} to {EndRowIndex} ({EndRowIndex - StartRowIndex + 1} rows)";
    
    /// <summary>Available color presets for charts.</summary>
    public List<string> AvailableColorPresets { get; } = new()
    {
        "Subsea7 Professional",
        "High Contrast",
        "Ocean Blue (Subsea)",
        "Monochrome (Print)",
        "Earth Tones",
        "Vibrant Modern"
    };
    
    /// <summary>Selected color preset name.</summary>
    public string SelectedColorPreset
    {
        get => AvailableColorPresets[Math.Min(_chartColorPresetIndex, AvailableColorPresets.Count - 1)];
        set
        {
            var index = AvailableColorPresets.IndexOf(value);
            if (index >= 0) ChartColorPresetIndex = index;
        }
    }
    
    /// <summary>Chart color preset index (0=Professional, 1=Vibrant, 2=Ocean, 3=Earth).</summary>
    public int ChartColorPresetIndex
    {
        get => _chartColorPresetIndex;
        set
        {
            if (SetProperty(ref _chartColorPresetIndex, value))
            {
                // Notify table color properties changed
                OnPropertyChanged(nameof(TableHeaderBackground));
                OnPropertyChanged(nameof(TableAccentColor));
                OnPropertyChanged(nameof(TableSuccessColor));
                OnPropertyChanged(nameof(TableErrorColor));
                OnPropertyChanged(nameof(SelectedColorPreset));
                
                // Regenerate charts with new colors
                if (Statistics != null && ComparisonPoints.Count > 0)
                {
                    UpdateAllCharts();
                }
            }
        }
    }
    
    /// <summary>Help text for current step.</summary>
    public string CurrentStepHelpText => GetStepHelpText(CurrentStepIndex);
    
    /// <summary>Dynamic text for the Next button based on current step.</summary>
    public string NextButtonText => CurrentStepIndex switch
    {
        4 => "Complete Analysis",  // Step 4: Process & Align - main action step
        7 => "Finish",             // Step 7: Export - final step
        _ => "Next"
    };
    
    /// <summary>Dynamic icon for the Next button based on current step.</summary>
    public string NextButtonIcon => CurrentStepIndex switch
    {
        4 => "CheckAll",           // Checkmark for complete analysis
        7 => "Check",              // Check for finish
        _ => "ArrowRight"          // Arrow for navigation
    };
    
    /// <summary>Data preview confirmed checkbox (Step 2).</summary>
    public bool DataPreviewConfirmed
    {
        get => _dataPreviewConfirmed;
        set
        {
            if (SetProperty(ref _dataPreviewConfirmed, value))
            {
                UpdateNavigationState();
            }
        }
    }
    
    /// <summary>Supervisor name for approval.</summary>
    public string SupervisorName
    {
        get => _supervisorName;
        set
        {
            if (SetProperty(ref _supervisorName, value))
            {
                OnPropertyChanged(nameof(CanApprove));
            }
        }
    }
    
    /// <summary>Supervisor initials for approval.</summary>
    public string SupervisorInitials
    {
        get => _supervisorInitials;
        set
        {
            if (SetProperty(ref _supervisorInitials, value?.ToUpper() ?? ""))
            {
                OnPropertyChanged(nameof(CanApprove));
            }
        }
    }
    
    /// <summary>Whether results have been approved.</summary>
    public bool IsApproved
    {
        get => _isApproved;
        set
        {
            if (SetProperty(ref _isApproved, value))
            {
                OnPropertyChanged(nameof(ApprovalInfo));
            }
        }
    }
    
    /// <summary>Whether the approve button can be clicked.</summary>
    public bool CanApprove => !IsApproved && 
                              !string.IsNullOrWhiteSpace(SupervisorName) && 
                              !string.IsNullOrWhiteSpace(SupervisorInitials) &&
                              SupervisorInitials.Length >= 2 &&
                              Statistics != null;
    
    /// <summary>Approval info string for display.</summary>
    public string ApprovalInfo => IsApproved 
        ? $"{SupervisorName} ({SupervisorInitials}) - {_approvalDateTime:yyyy-MM-dd HH:mm}" 
        : "";
    
    /// <summary>Data range string for display.</summary>
    public string DataTimeRange
    {
        get
        {
            if (_dataStartTime == null || _dataEndTime == null)
            {
                if (ComparisonPoints.Count == 0) return "No data loaded";
                var minTime = ComparisonPoints.Min(p => p.DateTime);
                var maxTime = ComparisonPoints.Max(p => p.DateTime);
                var duration = maxTime - minTime;
                return $"{minTime:HH:mm:ss} - {maxTime:HH:mm:ss} ({duration.TotalMinutes:F1} min)";
            }
            var dur = _dataEndTime.Value - _dataStartTime.Value;
            return $"{_dataStartTime:HH:mm:ss} - {_dataEndTime:HH:mm:ss} ({dur.TotalMinutes:F1} min)";
        }
    }
    
    /// <summary>Filtered point count for display.</summary>
    public int FilteredPointCount => TimeFilterEnabled && FilterStartTime.HasValue
        ? ComparisonPoints.Count(p => p.DateTime >= FilterStartTime && 
                                       (!FilterEndTime.HasValue || p.DateTime <= FilterEndTime))
        : ComparisonPoints.Count;

    #endregion
    
    #region Settings and History Properties
    
    /// <summary>Module settings (persistent).</summary>
    public ModuleSettings Settings => _settings;
    
    /// <summary>Comparison history entries.</summary>
    public ComparisonHistory ComparisonHistory => _comparisonHistory;
    
    /// <summary>History entries for binding.</summary>
    public ObservableCollection<ComparisonHistoryEntry> HistoryEntries => 
        new ObservableCollection<ComparisonHistoryEntry>(_comparisonHistory.Entries);
    
    /// <summary>Current project file path (null if not saved).</summary>
    public string? CurrentProjectFilePath
    {
        get => _currentProjectFilePath;
        set
        {
            if (SetProperty(ref _currentProjectFilePath, value))
            {
                RaisePropertyChanged(nameof(HasProjectFile));
                RaisePropertyChanged(nameof(ProjectFileDisplayName));
                RaisePropertyChanged(nameof(WindowTitle));
            }
        }
    }
    
    /// <summary>Whether project has been saved to a file.</summary>
    public bool HasProjectFile => !string.IsNullOrEmpty(CurrentProjectFilePath);
    
    /// <summary>Display name for current project file.</summary>
    public string ProjectFileDisplayName => HasProjectFile 
        ? Path.GetFileName(CurrentProjectFilePath!) 
        : "Untitled Project";
    
    /// <summary>Window title with project name and unsaved indicator.</summary>
    public string WindowTitle => $"GNSS Calibration - {ProjectFileDisplayName}{(_hasUnsavedChanges ? " *" : "")}";
    
    /// <summary>Whether there are unsaved changes.</summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                RaisePropertyChanged(nameof(WindowTitle));
            }
        }
    }
    
    /// <summary>Recent NPD files for quick access.</summary>
    public List<string> RecentNpdFiles => _settings.RecentNpdFiles;
    
    /// <summary>Recent project files for quick access.</summary>
    public List<string> RecentProjectFiles => _settings.RecentProjectFiles;
    
    #endregion
    
    #region Batch Processing Properties
    
    /// <summary>Files queued for batch processing.</summary>
    public ObservableCollection<BatchFileItem> BatchFiles
    {
        get => _batchFiles;
        set => SetProperty(ref _batchFiles, value);
    }
    
    /// <summary>Whether batch processing is running.</summary>
    public bool IsBatchProcessing
    {
        get => _isBatchProcessing;
        set => SetProperty(ref _isBatchProcessing, value);
    }
    
    /// <summary>Batch processing progress (0-100).</summary>
    public int BatchProgress
    {
        get => _batchProgress;
        set => SetProperty(ref _batchProgress, value);
    }
    
    /// <summary>Batch processing status message.</summary>
    public string BatchStatusMessage
    {
        get => _batchStatusMessage;
        set => SetProperty(ref _batchStatusMessage, value);
    }
    
    #endregion
    
    #region Point Selection Properties
    
    /// <summary>Currently selected point for highlighting.</summary>
    public GnssComparisonPoint? SelectedPoint
    {
        get => _selectedPoint;
        set
        {
            if (SetProperty(ref _selectedPoint, value))
            {
                _selectedPointIndex = value?.Index ?? -1;
                RaisePropertyChanged(nameof(HasSelectedPoint));
                RaisePropertyChanged(nameof(SelectedPointInfo));
                // Update charts to highlight selected point
                HighlightPointInCharts(value);
            }
        }
    }
    
    /// <summary>Index of selected point.</summary>
    public int SelectedPointIndex
    {
        get => _selectedPointIndex;
        set
        {
            if (SetProperty(ref _selectedPointIndex, value))
            {
                // Find and select the point
                SelectedPoint = value >= 0 && value < ComparisonPoints.Count 
                    ? ComparisonPoints.FirstOrDefault(p => p.Index == value)
                    : null;
            }
        }
    }
    
    /// <summary>Whether a point is selected.</summary>
    public bool HasSelectedPoint => SelectedPoint != null;
    
    /// <summary>Selected point info for display.</summary>
    public string SelectedPointInfo => SelectedPoint != null
        ? $"Point {SelectedPoint.Index}: ΔE={SelectedPoint.DeltaEasting:F4}, ΔN={SelectedPoint.DeltaNorthing:F4}, Radial={SelectedPoint.RadialDistance:F4}"
        : "No point selected";

    #region POS File Properties
    
    /// <summary>POS file format index for ComboBox binding.</summary>
    public int PosFormatIndex
    {
        get => (int)Project.PosSettings.Format;
        set
        {
            Project.PosSettings.Format = (PosFileFormat)value;
            RaisePropertyChanged();
        }
    }
    
    /// <summary>POS coordinate system index for ComboBox binding.</summary>
    public int PosCoordSystemIndex
    {
        get => (int)Project.PosSettings.CoordinateSystem;
        set
        {
            Project.PosSettings.CoordinateSystem = (PosCoordinateSystem)value;
            RaisePropertyChanged();
        }
    }
    
    /// <summary>POS time format index for ComboBox binding.</summary>
    public int PosTimeFormatIndex
    {
        get => (int)Project.PosSettings.TimeFormat;
        set
        {
            Project.PosSettings.TimeFormat = (PosTimeFormat)value;
            RaisePropertyChanged();
        }
    }
    
    /// <summary>UTM Zone for POS file conversion.</summary>
    public int PosUtmZone
    {
        get => Project.PosSettings.UtmZone;
        set
        {
            Project.PosSettings.UtmZone = value;
            RaisePropertyChanged();
        }
    }
    
    /// <summary>Hemisphere index (0=North, 1=South).</summary>
    public int PosHemisphereIndex
    {
        get => Project.PosSettings.UtmNorthernHemisphere ? 0 : 1;
        set
        {
            Project.PosSettings.UtmNorthernHemisphere = value == 0;
            RaisePropertyChanged();
        }
    }
    
    /// <summary>Loaded POS file name for display.</summary>
    public string LoadedPosFileName => string.IsNullOrEmpty(Project.PosFilePath) 
        ? "No POS file" 
        : System.IO.Path.GetFileName(Project.PosFilePath);
    
    /// <summary>POS data points (ground truth).</summary>
    private List<PosDataPoint> _posDataPoints = new();
    public List<PosDataPoint> PosDataPoints
    {
        get => _posDataPoints;
        set => SetProperty(ref _posDataPoints, value);
    }
    
    /// <summary>POS parse result info.</summary>
    private PosParseResult? _posParseResult;
    public PosParseResult? PosParseResult
    {
        get => _posParseResult;
        set => SetProperty(ref _posParseResult, value);
    }
    
    /// <summary>POS file info string for display.</summary>
    public string PosFileInfo
    {
        get
        {
            if (_posParseResult == null || !_posParseResult.Success)
                return string.Empty;
            
            return $"Format: {_posParseResult.DetectedFormat}, " +
                   $"Points: {_posParseResult.ParsedRecords}, " +
                   $"Duration: {_posParseResult.Duration?.TotalMinutes:F1} min";
        }
    }
    
    /// <summary>POS file loaded indicator for UI binding.</summary>
    public bool HasPosFile => !string.IsNullOrEmpty(Project.PosFilePath);
    
    #endregion

    /// <summary>Status bar message.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Indicates if a long operation is in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                UpdateNavigationState();
            }
        }
    }

    /// <summary>Whether Next button should be enabled.</summary>
    public bool CanGoNext
    {
        get => _canGoNext;
        private set => SetProperty(ref _canGoNext, value);
    }

    /// <summary>Whether Back button should be enabled.</summary>
    public bool CanGoBack
    {
        get => _canGoBack;
        private set => SetProperty(ref _canGoBack, value);
    }

    /// <summary>Total number of data points loaded.</summary>
    public int TotalPointCount => ComparisonPoints.Count;

    /// <summary>Number of accepted (non-rejected) points.</summary>
    public int AcceptedPointCount => ComparisonPoints.Count(p => !p.IsRejected);

    /// <summary>Number of rejected points.</summary>
    public int RejectedPointCount => ComparisonPoints.Count(p => p.IsRejected);
    
    /// <summary>Available columns from loaded file.</summary>
    public ObservableCollection<string> AvailableColumns
    {
        get => _availableColumns;
        set => SetProperty(ref _availableColumns, value);
    }
    
    /// <summary>Selected System 1 Easting column.</summary>
    public string? SelectedSystem1EastingColumn
    {
        get => _selectedSystem1EastingColumn;
        set
        {
            if (SetProperty(ref _selectedSystem1EastingColumn, value))
                UpdateColumnMapping();
        }
    }
    
    /// <summary>Selected System 1 Northing column.</summary>
    public string? SelectedSystem1NorthingColumn
    {
        get => _selectedSystem1NorthingColumn;
        set
        {
            if (SetProperty(ref _selectedSystem1NorthingColumn, value))
                UpdateColumnMapping();
        }
    }
    
    /// <summary>Selected System 1 Height column.</summary>
    public string? SelectedSystem1HeightColumn
    {
        get => _selectedSystem1HeightColumn;
        set
        {
            if (SetProperty(ref _selectedSystem1HeightColumn, value))
                UpdateColumnMapping();
        }
    }
    
    /// <summary>Selected System 2 Easting column.</summary>
    public string? SelectedSystem2EastingColumn
    {
        get => _selectedSystem2EastingColumn;
        set
        {
            if (SetProperty(ref _selectedSystem2EastingColumn, value))
                UpdateColumnMapping();
        }
    }
    
    /// <summary>Selected System 2 Northing column.</summary>
    public string? SelectedSystem2NorthingColumn
    {
        get => _selectedSystem2NorthingColumn;
        set
        {
            if (SetProperty(ref _selectedSystem2NorthingColumn, value))
                UpdateColumnMapping();
        }
    }
    
    /// <summary>Selected System 2 Height column.</summary>
    public string? SelectedSystem2HeightColumn
    {
        get => _selectedSystem2HeightColumn;
        set
        {
            if (SetProperty(ref _selectedSystem2HeightColumn, value))
                UpdateColumnMapping();
        }
    }
    
    /// <summary>Filter result summary.</summary>
    public FilterResult? FilterResult
    {
        get => _filterResult;
        set => SetProperty(ref _filterResult, value);
    }
    
    /// <summary>Whether data has been loaded from file.</summary>
    public bool DataLoaded
    {
        get => _dataLoaded;
        set => SetProperty(ref _dataLoaded, value);
    }
    
    /// <summary>Whether data has been fully processed.</summary>
    public bool DataProcessed
    {
        get => _dataProcessed;
        set => SetProperty(ref _dataProcessed, value);
    }
    
    /// <summary>Loaded file name for display.</summary>
    public string LoadedFileName => string.IsNullOrEmpty(Project.NpdFilePath) 
        ? "No file loaded" 
        : System.IO.Path.GetFileName(Project.NpdFilePath);
    
    /// <summary>Loaded file info for display (columns and rows).</summary>
    public string LoadedFileInfo => DataLoaded 
        ? $"{AvailableColumns.Count} columns, {TotalDataRows} rows"
        : "";
    
    /// <summary>DEBUG: Shows current column mapping indices.</summary>
    public string MappingDebugInfo => 
        $"Sys1: E={Project.ColumnMapping.System1EastingIndex}, N={Project.ColumnMapping.System1NorthingIndex}, H={Project.ColumnMapping.System1HeightIndex} | " +
        $"Sys2: E={Project.ColumnMapping.System2EastingIndex}, N={Project.ColumnMapping.System2NorthingIndex}, H={Project.ColumnMapping.System2HeightIndex}";
    
    /// <summary>POS file name for display.</summary>
    public string PosLoadedFileName => string.IsNullOrEmpty(Project.PosFilePath) 
        ? "No file loaded" 
        : System.IO.Path.GetFileName(Project.PosFilePath);

    #endregion

    #region Commands

    public ICommand NextStepCommand { get; }
    public ICommand PreviousStepCommand { get; }
    public RelayCommand<int> GoToStepCommand { get; }
    public RelayCommand<string> LoadFileCommand { get; }
    public ICommand NewProjectCommand { get; }
    
    // Theme and Help commands
    public ICommand ToggleThemeCommand { get; }
    public ICommand ShowHelpCommand { get; }
    
    // Time filter commands
    public ICommand ApplyTimeFilterCommand { get; }
    public ICommand ClearTimeFilterCommand { get; }
    
    // Data processing commands
    public ICommand BrowseFileCommand { get; }
    public ICommand BrowsePosFileCommand { get; }
    public ICommand ParseFileCommand { get; }
    public ICommand ProcessDataCommand { get; }
    public ICommand ApplyFilterCommand { get; }
    public ICommand AcceptAllCommand { get; }
    public ICommand RejectFlaggedCommand { get; }
    public ICommand RecalculateCommand { get; }
    
    // Export commands
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand ExportFullDataPdfCommand { get; }
    public ICommand ExportAllCommand { get; }
    public ICommand ExportChartsCommand { get; }
    
    // Project file commands
    public ICommand SaveProjectCommand { get; }
    public ICommand SaveProjectAsCommand { get; }
    public ICommand OpenProjectCommand { get; }
    
    // Batch processing commands
    public ICommand AddBatchFileCommand { get; }
    public ICommand RemoveBatchFileCommand { get; }
    public ICommand ClearBatchFilesCommand { get; }
    public ICommand RunBatchProcessCommand { get; }
    
    // History commands
    public ICommand ClearHistoryCommand { get; }
    
    // Point selection command
    public RelayCommand<GnssComparisonPoint> SelectPointCommand { get; }
    
    // Complete/Supervisor confirmation command
    public ICommand CompleteCommand { get; }
    
    // Data preview popup command
    public ICommand ShowDataPreviewCommand { get; }
    
    // Completion popup command for Step 7
    public ICommand ShowCompletionPopupCommand { get; }
    
    // Supervisor approval command
    public ICommand ApproveResultsCommand { get; }

    #endregion

    #region Step Initialization

    private void InitializeSteps()
    {
        Steps.Clear();
        Steps.Add(new WizardStep
        {
            Number = 1,
            Name = "Project",
            Description = "Project & Files",
            Icon = "FolderOpen",
            IsActive = true
        });
        Steps.Add(new WizardStep
        {
            Number = 2,
            Name = "Columns",
            Description = "Columns & Preview",
            Icon = "TableColumn"
        });
        Steps.Add(new WizardStep
        {
            Number = 3,
            Name = "Settings",
            Description = "Processing Settings",
            Icon = "Cog"
        });
        Steps.Add(new WizardStep
        {
            Number = 4,
            Name = "Process",
            Description = "Process & Data",
            Icon = "DatabaseCog"
        });
        Steps.Add(new WizardStep
        {
            Number = 5,
            Name = "QC",
            Description = "Quality Control",
            Icon = "FilterCheck"
        });
        Steps.Add(new WizardStep
        {
            Number = 6,
            Name = "Statistics",
            Description = "Results & Tables",
            Icon = "ChartBox"
        });
        Steps.Add(new WizardStep
        {
            Number = 7,
            Name = "Charts",
            Description = "Charts & Export",
            Icon = "ChartLine"
        });

        CurrentStep = Steps[0];
    }

    #endregion

    #region Navigation

    private void GoToNextStep()
    {
        if (CurrentStepIndex < 7)
        {
            // Perform step-specific actions before advancing
            PerformStepAction(CurrentStepIndex);
            
            // Mark current step as completed
            if (CurrentStep != null)
            {
                CurrentStep.IsCompleted = true;
                CurrentStep.IsActive = false;
            }
            
            CurrentStepIndex++;
        }
    }
    
    private void PerformStepAction(int stepIndex)
    {
        switch (stepIndex)
        {
            case 2: // After Load - DO NOT re-detect columns here!
                // DetectColumns() is already called when file is loaded via LoadFile()
                // Calling it again here would overwrite user's manual column selections!
                // Just ensure mapping is up-to-date from current selections
                if (!string.IsNullOrEmpty(Project.NpdFilePath) && AvailableColumns.Count > 0)
                {
                    // Force update mapping from current dropdown selections
                    // This ensures user's manual changes are persisted
                    UpdateColumnMapping();
                    System.Diagnostics.Debug.WriteLine($"[GNSS] Step 2 complete - mapping preserved: Sys1H={Project.ColumnMapping.System1HeightIndex}, Sys2H={Project.ColumnMapping.System2HeightIndex}");
                }
                break;
            case 4: // After Align - process data
                ProcessData();
                break;
        }
    }

    private void GoToPreviousStep()
    {
        if (CurrentStepIndex > 1)
        {
            if (CurrentStep != null)
            {
                CurrentStep.IsActive = false;
            }
            
            CurrentStepIndex--;
        }
    }

    private void GoToStep(int? stepNumber)
    {
        if (stepNumber.HasValue && stepNumber >= 1 && stepNumber <= 7)
        {
            if (CurrentStep != null)
            {
                CurrentStep.IsActive = false;
            }
            
            CurrentStepIndex = stepNumber.Value;
        }
    }

    private bool CanGoToStep(int? stepNumber)
    {
        if (!stepNumber.HasValue || IsBusy) return false;
        
        // Can go to completed steps or the next available step
        var step = Steps.FirstOrDefault(s => s.Number == stepNumber);
        if (step == null) return false;
        
        return step.IsCompleted || stepNumber <= CurrentStepIndex;
    }

    private void UpdateCurrentStep()
    {
        foreach (var step in Steps)
        {
            step.IsActive = step.Number == CurrentStepIndex;
        }
        
        CurrentStep = Steps.FirstOrDefault(s => s.Number == CurrentStepIndex);
        
        RaisePropertyChanged(nameof(CurrentStep));
    }

    private void UpdateNavigationState()
    {
        // Ensure we're on UI thread for property change notifications
        if (System.Windows.Application.Current?.Dispatcher != null && 
            !System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            System.Windows.Application.Current.Dispatcher.Invoke(UpdateNavigationState);
            return;
        }
        
        // Update navigation state
        var canGoBack = CurrentStepIndex > 1 && !IsBusy;
        var canGoNext = CurrentStepIndex < 7 && !IsBusy && ValidateCurrentStep();
        
        // Only update if changed (SetProperty handles this, but be explicit)
        if (_canGoBack != canGoBack)
        {
            _canGoBack = canGoBack;
            RaisePropertyChanged(nameof(CanGoBack));
        }
        
        if (_canGoNext != canGoNext)
        {
            _canGoNext = canGoNext;
            RaisePropertyChanged(nameof(CanGoNext));
        }
        
        // Force WPF to re-evaluate command CanExecute
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    private bool ValidateCurrentStep()
    {
        // Validation rules for each step
        return CurrentStepIndex switch
        {
            1 => !string.IsNullOrWhiteSpace(Project.ProjectName) && 
                 !string.IsNullOrWhiteSpace(Project.VesselName),
            2 => !string.IsNullOrWhiteSpace(Project.NpdFilePath),
            3 => true, // Settings step - always valid with defaults
            4 => AvailableColumns.Count > 0, // Need columns detected
            5 => ComparisonPoints.Count > 0, // Need data for QC
            6 => Statistics != null, // Need statistics calculated
            7 => true, // Export step - always valid
            _ => true
        };
    }

    #endregion

    #region File Operations

    private void BrowseForFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select GNSS Survey Data File",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = !string.IsNullOrEmpty(_settings.LastNpdFolder) 
                ? _settings.LastNpdFolder 
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() == true)
        {
            // Update settings
            _settings.UpdateFolderFromFile(dialog.FileName, FolderType.Npd);
            _settings.AddRecentNpdFile(dialog.FileName);
            _settings.Save();
            RaisePropertyChanged(nameof(RecentNpdFiles));
            
            LoadFile(dialog.FileName);
        }
    }

    private void LoadFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        
        Project.NpdFilePath = filePath;
        StatusMessage = $"File selected: {System.IO.Path.GetFileName(filePath)}";
        RaisePropertyChanged(nameof(LoadedFileName));
        
        // Load column headers
        DetectColumns();
        
        UpdateNavigationState();
    }
    
    private void BrowseForPosFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select POS File (Ground Truth Reference)",
            Filter = "POS Files (*.pos)|*.pos|Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FilterIndex = 1,
            InitialDirectory = !string.IsNullOrEmpty(_settings.LastPosFolder)
                ? _settings.LastPosFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() == true)
        {
            // Update settings
            _settings.UpdateFolderFromFile(dialog.FileName, FolderType.Pos);
            _settings.Save();
            
            LoadPosFile(dialog.FileName);
        }
    }
    
    /// <summary>
    /// Clears the loaded POS file and disables POS reference mode.
    /// </summary>
    public void ClearPosFile()
    {
        Project.PosFilePath = null;
        Project.UsePosAsReference = false;
        _posDataPoints.Clear();
        _posParseResult = null;
        
        RaisePropertyChanged(nameof(LoadedPosFileName));
        RaisePropertyChanged(nameof(HasPosFile));
        RaisePropertyChanged(nameof(PosFileInfo));
        
        StatusMessage = "POS file cleared.";
        UpdateNavigationState();
    }
    
    private void LoadPosFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;
        
        Project.PosFilePath = filePath;
        StatusMessage = $"POS file selected: {System.IO.Path.GetFileName(filePath)}";
        RaisePropertyChanged(nameof(LoadedPosFileName));
        
        // Parse POS file
        ParsePosFile();
        
        // Enable POS as reference automatically
        Project.UsePosAsReference = true;
        Project.Mode = ComparisonMode.PosValidation;
        RaisePropertyChanged(nameof(Project));
        
        UpdateNavigationState();
    }
    
    private void ParsePosFile()
    {
        if (string.IsNullOrEmpty(Project.PosFilePath)) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Parsing POS file...";
            
            var parser = new PosFileParser();
            PosParseResult = parser.Parse(Project.PosFilePath, Project.PosSettings);
            
            if (PosParseResult.Success)
            {
                PosDataPoints = PosParseResult.Points;
                
                // Auto-detect UTM zone if coordinates are geographic
                if (PosParseResult.DetectedCoordinateSystem == PosCoordinateSystem.Geographic)
                {
                    var (zone, northern) = CoordinateConverter.DetectUtmZone(PosDataPoints);
                    Project.PosSettings.UtmZone = zone;
                    Project.PosSettings.UtmNorthernHemisphere = northern;
                    RaisePropertyChanged(nameof(PosUtmZone));
                    RaisePropertyChanged(nameof(PosHemisphereIndex));
                    
                    // Convert to projected coordinates for comparison
                    CoordinateConverter.ConvertToProjected(PosDataPoints, zone, northern);
                }
                
                // Update UI
                RaisePropertyChanged(nameof(PosFileInfo));
                StatusMessage = $"POS file loaded: {PosParseResult.ParsedRecords} points, " +
                               $"Format: {PosParseResult.DetectedFormat}";
                
                if (PosParseResult.Warnings.Count > 0)
                {
                    StatusMessage += $" ({PosParseResult.Warnings.Count} warnings)";
                }
            }
            else
            {
                StatusMessage = $"POS parse error: {PosParseResult.ErrorMessage}";
                PosDataPoints.Clear();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error parsing POS file: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void DetectColumns()
    {
        if (string.IsNullOrEmpty(Project.NpdFilePath)) return;
        
        try
        {
            var headers = _processingService.GetColumnHeaders(Project.NpdFilePath);
            
            // CRITICAL FIX: Make column names unique if there are duplicates
            // This fixes the bug where IndexOf("North") returns the first match
            // even when the user selected the second "North" column
            var uniqueHeaders = MakeColumnNamesUnique(headers);
            
            // Store mapping from unique name to original index
            _columnNameToIndex.Clear();
            for (int i = 0; i < uniqueHeaders.Count; i++)
            {
                _columnNameToIndex[uniqueHeaders[i]] = i;
            }
            
            System.Diagnostics.Debug.WriteLine($"[GNSS] DetectColumns: {headers.Count} headers, {_columnNameToIndex.Count} unique mappings");
            
            // Use unique headers for dropdown display
            AvailableColumns = new ObservableCollection<string>(uniqueHeaders);
            Project.ColumnMapping.AvailableColumns = uniqueHeaders;
            
            // Auto-detect column indices (use original headers for pattern matching)
            var detected = _processingService.AutoDetectColumns(Project.NpdFilePath, Project.ColumnMapping);
            
            // Track what was detected for status message
            var detectedCols = new List<string>();
            
            // Temporarily disable preview updates during bulk column setting
            // Use try/finally to ENSURE the flag gets reset even if an exception occurs
            _isSettingColumnsFromDetection = true;
            try
            {
                // Set selected columns based on detection - use UNIQUE names
                if (detected.System1EastingIndex >= 0 && detected.System1EastingIndex < uniqueHeaders.Count)
                {
                    SelectedSystem1EastingColumn = uniqueHeaders[detected.System1EastingIndex];
                    detectedCols.Add($"Sys1E: {uniqueHeaders[detected.System1EastingIndex]}");
                }
                if (detected.System1NorthingIndex >= 0 && detected.System1NorthingIndex < uniqueHeaders.Count)
                {
                    SelectedSystem1NorthingColumn = uniqueHeaders[detected.System1NorthingIndex];
                    detectedCols.Add($"Sys1N: {uniqueHeaders[detected.System1NorthingIndex]}");
                }
                if (detected.System1HeightIndex >= 0 && detected.System1HeightIndex < uniqueHeaders.Count)
                {
                    SelectedSystem1HeightColumn = uniqueHeaders[detected.System1HeightIndex];
                }
                if (detected.System2EastingIndex >= 0 && detected.System2EastingIndex < uniqueHeaders.Count)
                {
                    SelectedSystem2EastingColumn = uniqueHeaders[detected.System2EastingIndex];
                    detectedCols.Add($"Sys2E: {uniqueHeaders[detected.System2EastingIndex]}");
                }
                if (detected.System2NorthingIndex >= 0 && detected.System2NorthingIndex < uniqueHeaders.Count)
                {
                    SelectedSystem2NorthingColumn = uniqueHeaders[detected.System2NorthingIndex];
                    detectedCols.Add($"Sys2N: {uniqueHeaders[detected.System2NorthingIndex]}");
                }
                if (detected.System2HeightIndex >= 0 && detected.System2HeightIndex < uniqueHeaders.Count)
                {
                    SelectedSystem2HeightColumn = uniqueHeaders[detected.System2HeightIndex];
                }
            }
            finally
            {
                // ALWAYS reset the flag, even if exception occurred
                _isSettingColumnsFromDetection = false;
            }
            
            // Update mapping indices from selected columns
            UpdateColumnMapping();
            
            // Build status message
            if (detectedCols.Count >= 4)
            {
                StatusMessage = $"Found {uniqueHeaders.Count} columns. Auto-detected: {string.Join(", ", detectedCols.Take(4))}";
            }
            else if (detectedCols.Count > 0)
            {
                StatusMessage = $"Found {headers.Count} columns. Partially detected: {string.Join(", ", detectedCols)}. Please verify column mapping.";
            }
            else
            {
                StatusMessage = $"Found {headers.Count} columns. No GNSS columns auto-detected. Please select columns manually.";
            }
        }
        catch (Exception ex)
        {
            // Ensure flag is reset even in outer exception
            _isSettingColumnsFromDetection = false;
            StatusMessage = $"Error reading file: {ex.Message}";
        }
    }
    
    private async void PopulateDataPreview()
    {
        if (string.IsNullOrEmpty(Project.NpdFilePath)) return;
        
        // Debug: Log mapping state before loading preview
        System.Diagnostics.Debug.WriteLine($"[GNSS PopulateDataPreview] Starting. ColumnMapping hash: {Project.ColumnMapping.GetHashCode()}");
        System.Diagnostics.Debug.WriteLine($"  Mapping indices: Sys1E={Project.ColumnMapping.System1EastingIndex}, Sys1N={Project.ColumnMapping.System1NorthingIndex}, Sys1H={Project.ColumnMapping.System1HeightIndex}");
        System.Diagnostics.Debug.WriteLine($"  Mapping indices: Sys2E={Project.ColumnMapping.System2EastingIndex}, Sys2N={Project.ColumnMapping.System2NorthingIndex}, Sys2H={Project.ColumnMapping.System2HeightIndex}");
        System.Diagnostics.Debug.WriteLine($"  Selected dropdowns: Sys2E='{SelectedSystem2EastingColumn}', Sys2N='{SelectedSystem2NorthingColumn}', Sys2H='{SelectedSystem2HeightColumn}'");
        
        try
        {
            IsLoadingAllData = true;
            IsBusy = true;
            StatusMessage = "Loading all data rows...";
            
            await Task.Run(() =>
            {
                // Load ALL rows (-1 = unlimited)
                var rawPreview = _processingService.GetRawPreview(Project.NpdFilePath, Project.ColumnMapping, -1);
                
                int index = 1;
                var previewRows = rawPreview.Select(r => new DataPreviewRow
                {
                    Index = index++,
                    RowNumber = r.RowNumber,
                    Time = r.TimeDisplay,
                    DateTime = r.Time != DateTime.MinValue ? r.Time : (DateTime?)null,
                    System1E = r.Sys1E,
                    System1N = r.Sys1N,
                    System1H = r.Sys1H,
                    System2E = r.Sys2E,
                    System2N = r.Sys2N,
                    System2H = r.Sys2H
                }).ToList();
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    DataPreview = new ObservableCollection<DataPreviewRow>(previewRows);
                    
                    // Set total rows
                    TotalDataRows = previewRows.Count;
                    StartRowIndex = 1;
                    EndRowIndex = TotalDataRows;
                    
                    // Set time range from ALL data for filtering
                    var validTimes = previewRows.Where(r => r.DateTime.HasValue).Select(r => r.DateTime!.Value).ToList();
                    if (validTimes.Count > 0)
                    {
                        DataStartTime = validTimes.Min();
                        DataEndTime = validTimes.Max();
                        
                        // Initialize filter times to full range
                        FilterStartTime = DataStartTime;
                        FilterEndTime = DataEndTime;
                        
                        // Initialize time slider (0-100 range)
                        TimeSliderMinimum = 0;
                        TimeSliderMaximum = 100;
                        _timeSliderStart = 0;
                        _timeSliderEnd = 100;
                        RaisePropertyChanged(nameof(TimeSliderStart));
                        RaisePropertyChanged(nameof(TimeSliderEnd));
                        
                        RaisePropertyChanged(nameof(DataTimeRange));
                        RaisePropertyChanged(nameof(DataDurationDisplay));
                        RaisePropertyChanged(nameof(FilterDurationDisplay));
                        RaisePropertyChanged(nameof(FilterStartTimeDisplay));
                        RaisePropertyChanged(nameof(FilterEndTimeDisplay));
                        RaisePropertyChanged(nameof(FilteredDataCount));
                    }
                    
                    // Update file info
                    RaisePropertyChanged(nameof(DataPreview));
                    RaisePropertyChanged(nameof(RowRangeDisplay));
                    
                    StatusMessage = $"Loaded {TotalDataRows:N0} data rows. Ready for processing.";
                    DataLoaded = previewRows.Count > 0;
                });
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            IsLoadingAllData = false;
            IsBusy = false;
            UpdateNavigationState();
        }
    }
    
    /// <summary>
    /// Tries to parse a time string to DateTime.
    /// </summary>
    private DateTime? TryParseDateTime(string timeStr)
    {
        if (string.IsNullOrEmpty(timeStr)) return null;
        if (DateTime.TryParse(timeStr, out var dt)) return dt;
        if (DateTime.TryParseExact(timeStr, "HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out dt)) return dt;
        if (DateTime.TryParseExact(timeStr, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out dt)) return dt;
        return null;
    }
    
    /// <summary>
    /// Makes column names unique by appending " (N)" suffix to duplicates.
    /// This fixes the bug where IndexOf("North") returns the first match
    /// even when the user selected the second "North" column.
    /// </summary>
    private List<string> MakeColumnNamesUnique(List<string> headers)
    {
        var result = new List<string>();
        var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var header in headers)
        {
            var trimmed = header.Trim();
            
            // Count occurrences
            if (!nameCounts.ContainsKey(trimmed))
            {
                nameCounts[trimmed] = 0;
            }
            nameCounts[trimmed]++;
            
            // First occurrence: use as-is
            // Subsequent occurrences: append " (N)"
            if (nameCounts[trimmed] == 1)
            {
                result.Add(trimmed);
            }
            else
            {
                result.Add($"{trimmed} ({nameCounts[trimmed]})");
            }
        }
        
        // Debug: Log any duplicates found
        var duplicates = nameCounts.Where(kv => kv.Value > 1).ToList();
        if (duplicates.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[GNSS] MakeColumnNamesUnique: Found {duplicates.Count} duplicate column names:");
            foreach (var dup in duplicates)
            {
                System.Diagnostics.Debug.WriteLine($"  '{dup.Key}' appears {dup.Value} times");
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the column index from a unique column name using the stored mapping.
    /// Returns -1 if not found.
    /// </summary>
    private int GetColumnIndex(string? uniqueColumnName)
    {
        if (string.IsNullOrEmpty(uniqueColumnName))
            return -1;
            
        if (_columnNameToIndex.TryGetValue(uniqueColumnName, out int index))
            return index;
            
        // Fallback: try exact match in AvailableColumns (for backwards compatibility)
        var headers = AvailableColumns.ToList();
        return headers.IndexOf(uniqueColumnName);
    }
    
    private void UpdateColumnMapping()
    {
        // Skip updates during bulk column setting from detection
        if (_isSettingColumnsFromDetection) 
        {
            System.Diagnostics.Debug.WriteLine("[GNSS] UpdateColumnMapping: SKIPPED - _isSettingColumnsFromDetection is true");
            return;
        }
        
        // Update mapping indices based on selected column names
        if (AvailableColumns.Count == 0) 
        {
            System.Diagnostics.Debug.WriteLine("[GNSS] UpdateColumnMapping: SKIPPED - AvailableColumns is empty");
            return;
        }
        
        System.Diagnostics.Debug.WriteLine($"[GNSS] UpdateColumnMapping called. AvailableColumns: {AvailableColumns.Count}, NameToIndex mappings: {_columnNameToIndex.Count}");
        
        // Find indices for selected columns using the stored mapping (not IndexOf!)
        // This is the critical fix for duplicate column names
        int sys1EIdx = GetColumnIndex(SelectedSystem1EastingColumn);
        int sys1NIdx = GetColumnIndex(SelectedSystem1NorthingColumn);
        int sys1HIdx = GetColumnIndex(SelectedSystem1HeightColumn);
        int sys2EIdx = GetColumnIndex(SelectedSystem2EastingColumn);
        int sys2NIdx = GetColumnIndex(SelectedSystem2NorthingColumn);
        int sys2HIdx = GetColumnIndex(SelectedSystem2HeightColumn);
        
        System.Diagnostics.Debug.WriteLine($"  GetColumnIndex results:");
        System.Diagnostics.Debug.WriteLine($"    Sys1E: '{SelectedSystem1EastingColumn}' → {sys1EIdx}");
        System.Diagnostics.Debug.WriteLine($"    Sys1N: '{SelectedSystem1NorthingColumn}' → {sys1NIdx}");
        System.Diagnostics.Debug.WriteLine($"    Sys1H: '{SelectedSystem1HeightColumn}' → {sys1HIdx}");
        System.Diagnostics.Debug.WriteLine($"    Sys2E: '{SelectedSystem2EastingColumn}' → {sys2EIdx}");
        System.Diagnostics.Debug.WriteLine($"    Sys2N: '{SelectedSystem2NorthingColumn}' → {sys2NIdx}");
        System.Diagnostics.Debug.WriteLine($"    Sys2H: '{SelectedSystem2HeightColumn}' → {sys2HIdx}");
        
        // Apply to mapping
        Project.ColumnMapping.System1EastingIndex = sys1EIdx;
        Project.ColumnMapping.System1NorthingIndex = sys1NIdx;
        Project.ColumnMapping.System1HeightIndex = sys1HIdx;
        Project.ColumnMapping.System2EastingIndex = sys2EIdx;
        Project.ColumnMapping.System2NorthingIndex = sys2NIdx;
        Project.ColumnMapping.System2HeightIndex = sys2HIdx;
        
        // Debug verify - also check the Project.ColumnMapping object reference
        System.Diagnostics.Debug.WriteLine($"[GNSS] UpdateColumnMapping complete. ColumnMapping object hash: {Project.ColumnMapping.GetHashCode()}");
        System.Diagnostics.Debug.WriteLine($"  Final: Sys1E={Project.ColumnMapping.System1EastingIndex}, Sys1N={Project.ColumnMapping.System1NorthingIndex}, Sys1H={Project.ColumnMapping.System1HeightIndex}");
        System.Diagnostics.Debug.WriteLine($"  Final: Sys2E={Project.ColumnMapping.System2EastingIndex}, Sys2N={Project.ColumnMapping.System2NorthingIndex}, Sys2H={Project.ColumnMapping.System2HeightIndex}");
        
        // Refresh data preview with new column mapping
        if (!string.IsNullOrEmpty(Project.NpdFilePath) && !IsLoadingAllData)
        {
            System.Diagnostics.Debug.WriteLine("[GNSS] UpdateColumnMapping: Calling PopulateDataPreview...");
            PopulateDataPreview();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[GNSS] UpdateColumnMapping: NOT calling PopulateDataPreview. NpdFilePath empty: {string.IsNullOrEmpty(Project.NpdFilePath)}, IsLoadingAllData: {IsLoadingAllData}");
        }
        
        // Update debug display
        RaisePropertyChanged(nameof(MappingDebugInfo));
        
        UpdateNavigationState();
    }
    
    private void ParseDataFile()
    {
        ProcessData();
    }

    private void NewProject()
    {
        Project = new GnssProject();
        ComparisonPoints.Clear();
        Statistics = null;
        FilterResult = null;
        AvailableColumns.Clear();
        DataLoaded = false;
        DataProcessed = false;
        CurrentStepIndex = 1;
        
        // Clear column selections
        SelectedSystem1EastingColumn = null;
        SelectedSystem1NorthingColumn = null;
        SelectedSystem1HeightColumn = null;
        SelectedSystem2EastingColumn = null;
        SelectedSystem2NorthingColumn = null;
        SelectedSystem2HeightColumn = null;
        
        // Reset all steps
        foreach (var step in Steps)
        {
            step.IsCompleted = false;
            step.IsActive = step.Number == 1;
        }
        
        StatusMessage = "New project created";
        RaisePropertyChanged(nameof(LoadedFileName));
        UpdateNavigationState();
    }

    #endregion
    
    #region Data Processing

    /// <summary>
    /// Determines if data can be processed.
    /// </summary>
    private bool CanProcessData()
    {
        if (IsBusy) return false;
        if (string.IsNullOrEmpty(Project.NpdFilePath)) return false;
        
        // Need at least System 1 columns mapped
        var mapping = Project.ColumnMapping;
        bool hasSystem1 = mapping.System1EastingIndex >= 0 && mapping.System1NorthingIndex >= 0;
        
        // If using POS, only need System 1
        if (Project.UsePosAsReference)
        {
            return hasSystem1;
        }
        
        // Otherwise need System 2 as well (or already processed)
        bool hasSystem2 = mapping.System2EastingIndex >= 0 && mapping.System2NorthingIndex >= 0;
        return hasSystem1 && hasSystem2;
    }

    private async void ProcessData()
    {
        if (string.IsNullOrEmpty(Project.NpdFilePath)) return;
        
        // Debug: Log mapping state before processing
        System.Diagnostics.Debug.WriteLine($"[GNSS] ProcessData - Column Mapping State:");
        System.Diagnostics.Debug.WriteLine($"  Sys1E={Project.ColumnMapping.System1EastingIndex}, Sys1N={Project.ColumnMapping.System1NorthingIndex}, Sys1H={Project.ColumnMapping.System1HeightIndex}");
        System.Diagnostics.Debug.WriteLine($"  Sys2E={Project.ColumnMapping.System2EastingIndex}, Sys2N={Project.ColumnMapping.System2NorthingIndex}, Sys2H={Project.ColumnMapping.System2HeightIndex}");
        System.Diagnostics.Debug.WriteLine($"  Selected columns: Sys1H='{SelectedSystem1HeightColumn}', Sys2H='{SelectedSystem2HeightColumn}'");
        
        // Check for POS validation mode
        if (Project.UsePosAsReference && PosDataPoints.Count == 0)
        {
            StatusMessage = "POS validation mode requires a loaded POS file.";
            return;
        }
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            IsBusy = true;
            StatusMessage = Project.UsePosAsReference 
                ? "Processing NPD vs POS comparison..." 
                : "Processing data...";
            
            await Task.Run(() =>
            {
                // Process with or without POS reference based on mode
                var posData = Project.UsePosAsReference ? PosDataPoints : null;
                var result = _processingService.Process(Project, posData);
                
                if (result.Success)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Store all points
                        var allPoints = result.Points;
                        
                        // Apply time filter if enabled
                        if (TimeFilterEnabled && FilterStartTime.HasValue)
                        {
                            allPoints = allPoints.Where(p => p.DateTime >= FilterStartTime.Value).ToList();
                            
                            if (FilterEndTime.HasValue)
                            {
                                allPoints = allPoints.Where(p => p.DateTime <= FilterEndTime.Value).ToList();
                            }
                            
                            StatusMessage = $"Time filter applied: {allPoints.Count} of {result.Points.Count} points in range.";
                        }
                        
                        ComparisonPoints = new ObservableCollection<GnssComparisonPoint>(allPoints);
                        Statistics = result.Statistics;
                        ApplyToleranceSettings();
                        FilterResult = result.FilterResult;
                        DataLoaded = true;
                        DataProcessed = true;
                        
                        // Add to history
                        stopwatch.Stop();
                        AddToHistory(stopwatch.ElapsedMilliseconds);
                        
                        if (!TimeFilterEnabled)
                        {
                            var modeStr = Project.UsePosAsReference ? "NPD vs POS" : "";
                            StatusMessage = $"Processed {allPoints.Count} {modeStr} points in {result.ProcessingTime.TotalMilliseconds:F0}ms. " +
                                           $"Accepted: {AcceptedPointCount}, Rejected: {RejectedPointCount}";
                        }
                        
                        RefreshPointCounts();
                        
                        // Update time range for filter UI (use full data range)
                        if (result.Points.Count > 0)
                        {
                            var minTime = result.Points.Min(p => p.DateTime);
                            var maxTime = result.Points.Max(p => p.DateTime);
                            _dataStartTime = minTime;
                            _dataEndTime = maxTime;
                            
                            // Set default filter times if not set
                            if (!FilterStartTime.HasValue)
                                FilterStartTime = minTime;
                            if (!FilterEndTime.HasValue)
                                FilterEndTime = maxTime;
                        }
                        
                        // Create all charts after processing
                        CreateAllCharts();
                        
                        // Update time range info
                        RaisePropertyChanged(nameof(DataTimeRange));
                        RaisePropertyChanged(nameof(FilteredPointCount));
                    });
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = $"Processing failed: {result.ErrorMessage}";
                        DataLoaded = false;
                        DataProcessed = false;
                    });
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateNavigationState();
        }
    }
    
    private void ApplyFilter()
    {
        if (ComparisonPoints.Count == 0) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Applying filter...";
            
            var points = ComparisonPoints.ToList();
            FilterResult = _processingService.RefilterData(points, Project.SigmaThreshold);
            
            // Update observable collection
            ComparisonPoints = new ObservableCollection<GnssComparisonPoint>(points);
            
            // Recalculate statistics
            Statistics = _processingService.RecalculateStatistics(ComparisonPoints, Project.SigmaThreshold);
            ApplyToleranceSettings();
            
            StatusMessage = $"Filter applied ({Project.SigmaThreshold}σ). Accepted: {AcceptedPointCount}, Rejected: {RejectedPointCount}";
            RefreshPointCounts();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Filter error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Updates time filter based on row selection.
    /// </summary>
    private void UpdateFilterFromRows()
    {
        if (DataPreview.Count == 0) return;
        
        // Get start time from start row
        int startIdx = Math.Max(0, StartRowIndex - 1);
        if (startIdx < DataPreview.Count && DataPreview[startIdx].DateTime.HasValue)
        {
            FilterStartTime = DataPreview[startIdx].DateTime;
        }
        
        // Get end time from end row
        int endIdx = EndRowIndex < 0 ? DataPreview.Count - 1 : Math.Min(EndRowIndex - 1, DataPreview.Count - 1);
        if (endIdx >= 0 && endIdx < DataPreview.Count && DataPreview[endIdx].DateTime.HasValue)
        {
            FilterEndTime = DataPreview[endIdx].DateTime;
        }
        
        RaisePropertyChanged(nameof(RowRangeDisplay));
        RaisePropertyChanged(nameof(FilteredPointCount));
    }
    
    private void AcceptAllPoints()
    {
        if (ComparisonPoints.Count == 0) return;
        
        var points = ComparisonPoints.ToList();
        _processingService.AcceptAllPoints(points);
        ComparisonPoints = new ObservableCollection<GnssComparisonPoint>(points);
        
        Statistics = _processingService.RecalculateStatistics(ComparisonPoints, Project.SigmaThreshold);
        ApplyToleranceSettings();
        
        StatusMessage = $"All {ComparisonPoints.Count} points accepted.";
        RefreshPointCounts();
    }
    
    private void RejectFlaggedPoints()
    {
        if (ComparisonPoints.Count == 0) return;
        
        // Reject points with high standardized score
        var flaggedIndices = ComparisonPoints
            .Where(p => Math.Abs(p.StandardizedScore) > Project.SigmaThreshold)
            .Select(p => p.Index)
            .ToList();
        
        var points = ComparisonPoints.ToList();
        _processingService.RejectPoints(points, flaggedIndices);
        ComparisonPoints = new ObservableCollection<GnssComparisonPoint>(points);
        
        Statistics = _processingService.RecalculateStatistics(ComparisonPoints, Project.SigmaThreshold);
        ApplyToleranceSettings();
        
        StatusMessage = $"Rejected {flaggedIndices.Count} flagged points.";
        RefreshPointCounts();
    }
    
    private void RecalculateStatistics()
    {
        if (ComparisonPoints.Count == 0) return;
        
        Statistics = _processingService.RecalculateStatistics(ComparisonPoints, Project.SigmaThreshold);
        ApplyToleranceSettings();
        
        StatusMessage = "Statistics recalculated.";
        RaisePropertyChanged(nameof(Statistics));
    }
    
    /// <summary>
    /// Applies the tolerance settings from the project to the statistics result.
    /// </summary>
    private void ApplyToleranceSettings()
    {
        if (Statistics == null) return;
        
        Statistics.ToleranceCheckEnabled = Project.ToleranceCheckEnabled;
        Statistics.ToleranceValue = Project.ToleranceValue;
        Statistics.ToleranceUnit = Project.UnitAbbreviation;
        
        // Notify UI of tolerance-related changes
        RaisePropertyChanged(nameof(ToleranceStatus));
        RaisePropertyChanged(nameof(TolerancePassed));
    }
    
    /// <summary>
    /// Gets the tolerance check status string for display.
    /// </summary>
    public string ToleranceStatus => Statistics?.ToleranceDescription ?? "No data";
    
    /// <summary>
    /// Gets whether the current 2DRMS passes the specified tolerance.
    /// </summary>
    public bool TolerancePassed => Statistics?.PassesTolerance ?? false;

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates point counts and raises property changed notifications.
    /// </summary>
    public void RefreshPointCounts()
    {
        RaisePropertyChanged(nameof(TotalPointCount));
        RaisePropertyChanged(nameof(AcceptedPointCount));
        RaisePropertyChanged(nameof(RejectedPointCount));
        UpdateNavigationState();
    }

    /// <summary>
    /// Marks a step as completed.
    /// </summary>
    public void CompleteStep(int stepNumber)
    {
        var step = Steps.FirstOrDefault(s => s.Number == stepNumber);
        if (step != null)
        {
            step.IsCompleted = true;
        }
        UpdateNavigationState();
    }
    
    /// <summary>
    /// Generates 2DRMS circle points for charting.
    /// </summary>
    public List<(double X, double Y)> Get2DRMSCirclePoints()
    {
        if (Statistics?.FilteredStatistics == null) 
            return new List<(double, double)>();
        
        return _processingService.Generate2DRMSCircle(Statistics.FilteredStatistics);
    }

    /// <summary>
    /// Creates/updates the scatter plot with Delta E vs Delta N and 2DRMS circle.
    /// </summary>
    private void UpdateScatterPlot()
    {
        if (Statistics?.FilteredStatistics == null || ComparisonPoints.Count == 0)
        {
            ScatterPlotModel = null;
            return;
        }

        var model = new PlotModel
        {
            Title = "Delta E vs Delta N",
            TitleFontSize = 14,
            PlotAreaBorderColor = OxyColors.Gray
        };

        // Configure axes
        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = $"ΔE ({Project.UnitAbbreviation})",
            TitleFontSize = 12,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 80)
        };

        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"ΔN ({Project.UnitAbbreviation})",
            TitleFontSize = 12,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 80)
        };

        model.Axes.Add(xAxis);
        model.Axes.Add(yAxis);

        // Accepted points (green)
        var acceptedSeries = new ScatterSeries
        {
            Title = "Accepted",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColor.FromRgb(46, 204, 113)
        };

        // Rejected points (red)
        var rejectedSeries = new ScatterSeries
        {
            Title = "Rejected",
            MarkerType = MarkerType.Cross,
            MarkerSize = 4,
            MarkerFill = OxyColor.FromRgb(231, 76, 60)
        };

        foreach (var point in ComparisonPoints)
        {
            var scatterPoint = new ScatterPoint(point.DeltaEasting, point.DeltaNorthing);
            if (point.IsRejected)
                rejectedSeries.Points.Add(scatterPoint);
            else
                acceptedSeries.Points.Add(scatterPoint);
        }

        model.Series.Add(acceptedSeries);
        if (rejectedSeries.Points.Count > 0)
            model.Series.Add(rejectedSeries);

        // 2DRMS Circle
        var stats = Statistics.FilteredStatistics;
        var circlePoints = Get2DRMSCirclePoints();
        
        if (circlePoints.Count > 0)
        {
            var circleSeries = new LineSeries
            {
                Title = $"2DRMS = {stats.Delta2DRMS:F3}",
                Color = OxyColor.FromRgb(155, 89, 182),
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash
            };

            foreach (var (x, y) in circlePoints)
            {
                circleSeries.Points.Add(new DataPoint(x, y));
            }

            model.Series.Add(circleSeries);
        }

        // Mean point marker
        var meanAnnotation = new PointAnnotation
        {
            X = stats.DeltaMeanEasting,
            Y = stats.DeltaMeanNorthing,
            Shape = MarkerType.Diamond,
            Size = 8,
            Fill = OxyColor.FromRgb(230, 126, 34),
            Text = "Mean"
        };
        model.Annotations.Add(meanAnnotation);

        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => ScatterPlotModel = model);
    }

    #endregion

    #region Export Methods

    private void ExportToExcel()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export to Excel",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"GNSS_Report_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}",
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Exporting to Excel...";

                var exporter = new ExcelExportService();
                exporter.Export(dialog.FileName, Project, ComparisonPoints, Statistics);

                StatusMessage = $"Excel exported: {System.IO.Path.GetFileName(dialog.FileName)}";
                System.Windows.MessageBox.Show(
                    $"Excel report saved to:\n{dialog.FileName}",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Excel export failed: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to export Excel:\n{ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private void ExportToPdf()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export to PDF",
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = $"GNSS_Report_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Generating PDF report with charts...";

                // Export chart images to memory
                var chartImages = GetChartImagesForPdf();
                
                var generator = new PdfReportService();
                generator.GenerateWithCharts(dialog.FileName, Project, ComparisonPoints, Statistics, chartImages);

                StatusMessage = $"PDF exported: {System.IO.Path.GetFileName(dialog.FileName)}";
                System.Windows.MessageBox.Show(
                    $"PDF report saved to:\n{dialog.FileName}\n\nIncludes statistics and {chartImages.Count} charts.",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"PDF export failed: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to export PDF:\n{ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    /// <summary>
    /// Generates chart images as byte arrays for PDF embedding.
    /// </summary>
    private List<(byte[] ImageData, string Title)> GetChartImagesForPdf()
    {
        var charts = new List<(byte[] ImageData, string Title)>();
        var chartsToExport = new (PlotModel? Model, string Title, int Width, int Height)[]
        {
            (DeltaScatterPlotModel, "Delta Scatter Plot with 2DRMS", 800, 500),
            (TimeSeriesPlotModel, "Time Series Analysis", 800, 400),
            (HistogramPlotModel, "Error Distribution Histogram", 700, 400),
            (CdfPlotModel, "Cumulative Distribution Function", 700, 400),
            (HeightAnalysisPlotModel, "Height Difference Analysis", 800, 400),
            (PolarErrorPlotModel, "Polar Error Distribution", 600, 600),
            (ErrorComponentsPlotModel, "Error Components Comparison", 700, 400),
            (QqPlotModel, "Q-Q Normality Plot", 550, 550),
            (RunningStatsPlotModel, "Running Statistics", 800, 400),
            (ErrorEllipsePlotModel, "Confidence Ellipse Analysis", 600, 600),
            (RawPositionPlotModel, "Raw Position Plot", 800, 500),
            (FilteredPositionPlotModel, "Filtered Position Plot", 800, 500),
        };
        
        foreach (var (model, title, width, height) in chartsToExport)
        {
            if (model != null)
            {
                var imageData = ExportPlotToBytes(model, width, height);
                if (imageData != null)
                {
                    charts.Add((imageData, title));
                }
            }
        }
        
        return charts;
    }
    
    /// <summary>
    /// Export a PlotModel to PNG bytes.
    /// </summary>
    private byte[]? ExportPlotToBytes(PlotModel model, int width, int height)
    {
        try
        {
            // Set white background for PDF
            model.Background = OxyColors.White;
            
            using var stream = new MemoryStream();
            var exporter = new OxyPlot.Wpf.PngExporter { Width = width, Height = height };
            exporter.Export(model, stream);
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
    }
    
    private void ExportFullDataPdf()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Full Data to PDF",
            Filter = "PDF Document (*.pdf)|*.pdf",
            FileName = $"GNSS_FullData_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Generating full data PDF...";

                var generator = new PdfReportService();
                generator.GenerateFullDataPdf(dialog.FileName, Project, ComparisonPoints);

                StatusMessage = $"Full data PDF exported: {System.IO.Path.GetFileName(dialog.FileName)}";
                System.Windows.MessageBox.Show(
                    $"Full data PDF saved to:\n{dialog.FileName}\n\nContains all {ComparisonPoints.Count} data points.",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Full data PDF export failed: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Failed to export full data PDF:\n{ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    private void ExportAll()
    {
        // Use SaveFileDialog to select output location (folder derived from file path)
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Export Location",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"GNSS_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}",
            DefaultExt = ".xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                var folder = System.IO.Path.GetDirectoryName(dialog.FileName)!;
                var baseName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);

                // Export Excel
                StatusMessage = "Exporting Excel...";
                var excelPath = System.IO.Path.Combine(folder, $"{baseName}.xlsx");
                var excelExporter = new ExcelExportService();
                excelExporter.Export(excelPath, Project, ComparisonPoints, Statistics);

                // Export PDF
                StatusMessage = "Generating PDF...";
                var pdfPath = System.IO.Path.Combine(folder, $"{baseName}.pdf");
                var pdfGenerator = new PdfReportService();
                pdfGenerator.Generate(pdfPath, Project, ComparisonPoints, Statistics);

                StatusMessage = "All exports complete";
                System.Windows.MessageBox.Show(
                    $"Reports exported to:\n{folder}\n\n• {baseName}.xlsx\n• {baseName}.pdf",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Export failed:\n{ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    /// <summary>Shows the completion/supervisor confirmation dialog.</summary>
    private void ShowCompletionConfirmation()
    {
        if (Statistics == null) return;
        
        // Build summary text
        string status = TolerancePassed ? "✓ PASS" : "✗ FAIL";
        string summary = $"GNSS Calibration Results Summary\n" +
                        $"{'─', 40}\n\n" +
                        $"Project: {Project.ProjectName}\n" +
                        $"Vessel: {Project.VesselName}\n" +
                        $"Date: {Project.SurveyDate:yyyy-MM-dd}\n" +
                        $"Location: {Project.Location}\n\n" +
                        $"Systems Compared:\n" +
                        $"  • {Project.System1Name} vs {Project.System2Name}\n\n" +
                        $"Results:\n" +
                        $"  • 2DRMS: {Statistics.FilteredStatistics.TwoDRMS:F4} {Project.UnitAbbreviation}\n" +
                        $"  • Tolerance: {Project.ToleranceValue:F4} {Project.UnitAbbreviation}\n" +
                        $"  • Status: {status}\n\n" +
                        $"Data Points:\n" +
                        $"  • Total: {TotalPointCount}\n" +
                        $"  • Accepted: {AcceptedPointCount}\n" +
                        $"  • Rejected: {RejectedPointCount}\n\n" +
                        $"{'─', 40}\n" +
                        $"Do you confirm and approve these results?";
        
        var result = System.Windows.MessageBox.Show(
            summary,
            "Supervisor Confirmation - GNSS Calibration Complete",
            System.Windows.MessageBoxButton.YesNo,
            TolerancePassed ? System.Windows.MessageBoxImage.Question : System.Windows.MessageBoxImage.Warning);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            // User confirmed - offer to export final reports
            var exportResult = System.Windows.MessageBox.Show(
                "Results approved.\n\nWould you like to export final reports now?",
                "Confirmation Accepted",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Information);
            
            if (exportResult == System.Windows.MessageBoxResult.Yes)
            {
                ExportAll();
            }
            
            StatusMessage = "Calibration completed and approved by supervisor";
        }
        else
        {
            StatusMessage = "Calibration results pending review";
        }
    }
    
    /// <summary>Shows a popup window with all data points for review.</summary>
    private void ShowDataPreviewPopup()
    {
        // Determine which data to show based on current step
        var isProcessed = ComparisonPoints.Count > 0;
        
        // Create window
        var window = new System.Windows.Window
        {
            Title = isProcessed 
                ? $"Processed Data - {ComparisonPoints.Count:N0} points" 
                : $"Raw Data Preview - {DataPreview.Count:N0} rows",
            Width = 1100,
            Height = 700,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252))
        };
        
        var mainGrid = new System.Windows.Controls.Grid();
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        
        // Header
        var header = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 58, 95)),
            Padding = new System.Windows.Thickness(20, 15, 20, 15)
        };
        var headerStack = new System.Windows.Controls.StackPanel();
        headerStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = isProcessed ? "PROCESSED COMPARISON DATA" : "RAW DATA PREVIEW",
            FontSize = 16,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        });
        headerStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = isProcessed 
                ? $"Total: {ComparisonPoints.Count:N0} | Accepted: {AcceptedPointCount:N0} | Rejected: {RejectedPointCount:N0}"
                : $"Total Rows: {DataPreview.Count:N0} | Time Range: {DataTimeRange}",
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 200)),
            Margin = new System.Windows.Thickness(0, 5, 0, 0)
        });
        header.Child = headerStack;
        System.Windows.Controls.Grid.SetRow(header, 0);
        mainGrid.Children.Add(header);
        
        // DataGrid with virtualization for performance
        var dataGrid = new System.Windows.Controls.DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            CanUserReorderColumns = true,
            CanUserResizeColumns = true,
            CanUserSortColumns = true,
            EnableRowVirtualization = true,
            EnableColumnVirtualization = true,
            Margin = new System.Windows.Thickness(15),
            GridLinesVisibility = System.Windows.Controls.DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility = System.Windows.Controls.DataGridHeadersVisibility.Column,
            AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            RowBackground = System.Windows.Media.Brushes.White,
            BorderThickness = new System.Windows.Thickness(1),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240))
        };
        
        // Set attached properties for virtualization
        System.Windows.Controls.VirtualizingPanel.SetIsVirtualizing(dataGrid, true);
        System.Windows.Controls.VirtualizingPanel.SetVirtualizationMode(dataGrid, System.Windows.Controls.VirtualizationMode.Recycling);
        
        if (isProcessed && ComparisonPoints.Count > 0)
        {
            // Processed data columns
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "#", Binding = new System.Windows.Data.Binding("Index"), Width = 60 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Date/Time", Binding = new System.Windows.Data.Binding("DateTime") { StringFormat = "yyyy-MM-dd HH:mm:ss" }, Width = 150 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "ΔE", Binding = new System.Windows.Data.Binding("DeltaEasting") { StringFormat = "F4" }, Width = 90 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "ΔN", Binding = new System.Windows.Data.Binding("DeltaNorthing") { StringFormat = "F4" }, Width = 90 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "ΔH", Binding = new System.Windows.Data.Binding("DeltaHeight") { StringFormat = "F4" }, Width = 90 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Radial", Binding = new System.Windows.Data.Binding("RadialDistance") { StringFormat = "F4" }, Width = 90 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Z-Score", Binding = new System.Windows.Data.Binding("StandardizedScore") { StringFormat = "F2" }, Width = 80 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("StatusText"), Width = 80 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys1 E", Binding = new System.Windows.Data.Binding("System1Easting") { StringFormat = "F3" }, Width = 110 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys1 N", Binding = new System.Windows.Data.Binding("System1Northing") { StringFormat = "F3" }, Width = 110 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys2 E", Binding = new System.Windows.Data.Binding("System2Easting") { StringFormat = "F3" }, Width = 110 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys2 N", Binding = new System.Windows.Data.Binding("System2Northing") { StringFormat = "F3" }, Width = 110 });
            
            dataGrid.ItemsSource = ComparisonPoints;
        }
        else if (DataPreview.Count > 0)
        {
            // Raw data columns
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "#", Binding = new System.Windows.Data.Binding("Index"), Width = 60 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Time", Binding = new System.Windows.Data.Binding("Time"), Width = 100 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys1 Easting", Binding = new System.Windows.Data.Binding("System1E"), Width = 120 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys1 Northing", Binding = new System.Windows.Data.Binding("System1N"), Width = 120 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys1 Height", Binding = new System.Windows.Data.Binding("System1H"), Width = 100 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys2 Easting", Binding = new System.Windows.Data.Binding("System2E"), Width = 120 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys2 Northing", Binding = new System.Windows.Data.Binding("System2N"), Width = 120 });
            dataGrid.Columns.Add(new System.Windows.Controls.DataGridTextColumn { Header = "Sys2 Height", Binding = new System.Windows.Data.Binding("System2H"), Width = 100 });
            
            dataGrid.ItemsSource = DataPreview;
        }
        
        System.Windows.Controls.Grid.SetRow(dataGrid, 1);
        mainGrid.Children.Add(dataGrid);
        
        // Footer with export button
        var footer = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
            Padding = new System.Windows.Thickness(20, 12, 20, 12)
        };
        var footerStack = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        
        var exportBtn = new System.Windows.Controls.Button
        {
            Content = "Export to CSV",
            Padding = new System.Windows.Thickness(20, 8, 20, 8),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new System.Windows.Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        exportBtn.Click += (s, e) => ExportDataPreviewToCsv(isProcessed);
        footerStack.Children.Add(exportBtn);
        
        var closeBtn = new System.Windows.Controls.Button
        {
            Content = "Close",
            Padding = new System.Windows.Thickness(20, 8, 20, 8),
            Margin = new System.Windows.Thickness(10, 0, 0, 0),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new System.Windows.Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        closeBtn.Click += (s, e) => window.Close();
        footerStack.Children.Add(closeBtn);
        
        footer.Child = footerStack;
        System.Windows.Controls.Grid.SetRow(footer, 2);
        mainGrid.Children.Add(footer);
        
        window.Content = mainGrid;
        window.ShowDialog();
    }
    
    /// <summary>Exports data preview to CSV file.</summary>
    private void ExportDataPreviewToCsv(bool isProcessed)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Data to CSV",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"GNSS_Data_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new System.IO.StreamWriter(dialog.FileName);
                
                if (isProcessed && ComparisonPoints.Count > 0)
                {
                    writer.WriteLine("Index,DateTime,DeltaE,DeltaN,DeltaH,Radial,ZScore,Status,Sys1E,Sys1N,Sys1H,Sys2E,Sys2N,Sys2H");
                    foreach (var p in ComparisonPoints)
                    {
                        writer.WriteLine($"{p.Index},{p.DateTime:yyyy-MM-dd HH:mm:ss},{p.DeltaEasting:F6},{p.DeltaNorthing:F6},{p.DeltaHeight:F6},{p.RadialDistance:F6},{p.StandardizedScore:F4},{p.StatusText},{p.System1Easting:F4},{p.System1Northing:F4},{p.System1Height:F4},{p.System2Easting:F4},{p.System2Northing:F4},{p.System2Height:F4}");
                    }
                }
                else if (DataPreview.Count > 0)
                {
                    writer.WriteLine("Index,Time,Sys1E,Sys1N,Sys1H,Sys2E,Sys2N,Sys2H");
                    foreach (var r in DataPreview)
                    {
                        writer.WriteLine($"{r.Index},{r.Time},{r.System1E},{r.System1N},{r.System1H},{r.System2E},{r.System2N},{r.System2H}");
                    }
                }
                
                StatusMessage = $"Data exported to {System.IO.Path.GetFileName(dialog.FileName)}";
                System.Windows.MessageBox.Show($"Data exported successfully to:\n{dialog.FileName}", "Export Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed: {ex.Message}", "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
    
    /// <summary>Shows the completion popup with approval, export options, and digital certificate.</summary>
    private void ShowCompletionPopup()
    {
        if (Statistics == null) return;
        
        var window = new System.Windows.Window
        {
            Title = "Complete Analysis - GNSS Calibration",
            Width = 700,
            Height = 850,
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
            WindowStyle = System.Windows.WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ResizeMode = System.Windows.ResizeMode.NoResize
        };
        
        // Main container with shadow
        var mainBorder = new System.Windows.Controls.Border
        {
            Background = System.Windows.Media.Brushes.White,
            CornerRadius = new System.Windows.CornerRadius(16),
            Margin = new System.Windows.Thickness(20),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 30,
                ShadowDepth = 5,
                Opacity = 0.3,
                Direction = 270
            }
        };
        
        var mainGrid = new System.Windows.Controls.Grid();
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto }); // Header
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) }); // Content
        mainGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto }); // Footer
        
        // === HEADER ===
        var headerBorder = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.LinearGradientBrush(
                System.Windows.Media.Color.FromRgb(30, 58, 95),
                System.Windows.Media.Color.FromRgb(44, 82, 130),
                45),
            CornerRadius = new System.Windows.CornerRadius(16, 16, 0, 0),
            Padding = new System.Windows.Thickness(25, 20, 25, 20)
        };
        var headerGrid = new System.Windows.Controls.Grid();
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = System.Windows.GridLength.Auto });
        
        var titleStack = new System.Windows.Controls.StackPanel();
        titleStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "GNSS CALIBRATION COMPLETE",
            FontSize = 18,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.White
        });
        titleStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Review, approve, and export your calibration results",
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 200, 220)),
            Margin = new System.Windows.Thickness(0, 5, 0, 0)
        });
        System.Windows.Controls.Grid.SetColumn(titleStack, 0);
        headerGrid.Children.Add(titleStack);
        
        // Close button
        var closeBtn = new System.Windows.Controls.Button
        {
            Content = "✕",
            Width = 32,
            Height = 32,
            FontSize = 16,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new System.Windows.Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        closeBtn.Click += (s, e) => window.Close();
        System.Windows.Controls.Grid.SetColumn(closeBtn, 1);
        headerGrid.Children.Add(closeBtn);
        headerBorder.Child = headerGrid;
        System.Windows.Controls.Grid.SetRow(headerBorder, 0);
        mainGrid.Children.Add(headerBorder);
        
        // === CONTENT ===
        var scrollViewer = new System.Windows.Controls.ScrollViewer
        {
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            Padding = new System.Windows.Thickness(25, 20, 25, 20)
        };
        var contentStack = new System.Windows.Controls.StackPanel();
        
        // Results Summary Card
        var statusColor = TolerancePassed 
            ? System.Windows.Media.Color.FromRgb(34, 197, 94) 
            : System.Windows.Media.Color.FromRgb(239, 68, 68);
        var resultCard = CreatePopupCard("ANALYSIS RESULT", new System.Windows.Media.SolidColorBrush(statusColor));
        var resultContent = new System.Windows.Controls.Grid { Margin = new System.Windows.Thickness(0, 10, 0, 0) };
        resultContent.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        resultContent.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        
        var resultLeft = new System.Windows.Controls.StackPanel();
        resultLeft.Children.Add(CreatePopupField("Status", Statistics?.ToleranceStatus ?? "---", TolerancePassed ? "#166534" : "#991B1B", 24, true));
        resultLeft.Children.Add(CreatePopupField("2DRMS", $"{Statistics?.FilteredStatistics.TwoDRMS:F4} {Project.UnitAbbreviation}", "#1E3A5F", 18, true));
        System.Windows.Controls.Grid.SetColumn(resultLeft, 0);
        resultContent.Children.Add(resultLeft);
        
        var resultRight = new System.Windows.Controls.StackPanel();
        resultRight.Children.Add(CreatePopupField("Tolerance", $"{Project.ToleranceValue:F4} {Project.UnitAbbreviation}", "#7C3AED", 18, true));
        resultRight.Children.Add(CreatePopupField("Points", $"{Statistics?.FilteredStatistics.SampleCount:N0} accepted", "#0369A1", 18, true));
        System.Windows.Controls.Grid.SetColumn(resultRight, 1);
        resultContent.Children.Add(resultRight);
        
        ((System.Windows.Controls.StackPanel)resultCard.Child).Children.Add(resultContent);
        contentStack.Children.Add(resultCard);
        
        // Supervisor Approval Section
        var approvalCard = CreatePopupCard("SUPERVISOR APPROVAL", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)));
        var approvalStack = (System.Windows.Controls.StackPanel)approvalCard.Child;
        
        var nameLabel = new System.Windows.Controls.TextBlock { Text = "Supervisor Name", FontSize = 11, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), Margin = new System.Windows.Thickness(0, 10, 0, 5) };
        approvalStack.Children.Add(nameLabel);
        var nameBox = new System.Windows.Controls.TextBox 
        { 
            Text = SupervisorName,
            Height = 40,
            FontSize = 14,
            Padding = new System.Windows.Thickness(12, 8, 12, 8)
        };
        nameBox.TextChanged += (s, e) => SupervisorName = nameBox.Text;
        approvalStack.Children.Add(nameBox);
        
        var initialsLabel = new System.Windows.Controls.TextBlock { Text = "Initials", FontSize = 11, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), Margin = new System.Windows.Thickness(0, 10, 0, 5) };
        approvalStack.Children.Add(initialsLabel);
        var initialsBox = new System.Windows.Controls.TextBox 
        { 
            Text = SupervisorInitials,
            Height = 40,
            FontSize = 14,
            MaxLength = 5,
            Width = 120,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Padding = new System.Windows.Thickness(12, 8, 12, 8)
        };
        initialsBox.TextChanged += (s, e) => SupervisorInitials = initialsBox.Text;
        approvalStack.Children.Add(initialsBox);
        
        contentStack.Children.Add(approvalCard);
        
        // Export Options Section
        var exportCard = CreatePopupCard("EXPORT OPTIONS", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)));
        var exportStack = (System.Windows.Controls.StackPanel)exportCard.Child;
        
        var exportPdfCheck = new System.Windows.Controls.CheckBox { Content = "PDF Report (Full calibration report)", IsChecked = true, Margin = new System.Windows.Thickness(0, 10, 0, 5), FontSize = 13 };
        var exportExcelCheck = new System.Windows.Controls.CheckBox { Content = "Excel Workbook (Data & statistics)", IsChecked = true, Margin = new System.Windows.Thickness(0, 5, 0, 5), FontSize = 13 };
        var exportChartsCheck = new System.Windows.Controls.CheckBox { Content = "Charts (PNG images)", IsChecked = false, Margin = new System.Windows.Thickness(0, 5, 0, 5), FontSize = 13 };
        var exportCertCheck = new System.Windows.Controls.CheckBox { Content = "Digital Certificate (Pass/Fail certificate PDF)", IsChecked = true, Margin = new System.Windows.Thickness(0, 5, 0, 5), FontSize = 13 };
        
        exportStack.Children.Add(exportPdfCheck);
        exportStack.Children.Add(exportExcelCheck);
        exportStack.Children.Add(exportChartsCheck);
        exportStack.Children.Add(exportCertCheck);
        
        contentStack.Children.Add(exportCard);
        
        // Project Summary
        var summaryCard = CreatePopupCard("PROJECT SUMMARY", new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)));
        var summaryStack = (System.Windows.Controls.StackPanel)summaryCard.Child;
        summaryStack.Children.Add(CreatePopupInfoRow("Project", Project.ProjectName));
        summaryStack.Children.Add(CreatePopupInfoRow("Vessel", Project.VesselName));
        summaryStack.Children.Add(CreatePopupInfoRow("Date", Project.SurveyDate?.ToString("yyyy-MM-dd") ?? "---"));
        summaryStack.Children.Add(CreatePopupInfoRow("GNSS 1", Project.System1Name));
        summaryStack.Children.Add(CreatePopupInfoRow("GNSS 2", Project.System2Name));
        contentStack.Children.Add(summaryCard);
        
        scrollViewer.Content = contentStack;
        System.Windows.Controls.Grid.SetRow(scrollViewer, 1);
        mainGrid.Children.Add(scrollViewer);
        
        // === FOOTER ===
        var footerBorder = new System.Windows.Controls.Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
            CornerRadius = new System.Windows.CornerRadius(0, 0, 16, 16),
            Padding = new System.Windows.Thickness(25, 15, 25, 15),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
            BorderThickness = new System.Windows.Thickness(0, 1, 0, 0)
        };
        var footerStack = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        
        var cancelBtn = new System.Windows.Controls.Button
        {
            Content = "Cancel",
            Width = 100,
            Height = 42,
            FontSize = 14,
            Margin = new System.Windows.Thickness(0, 0, 10, 0),
            Background = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219)),
            BorderThickness = new System.Windows.Thickness(1)
        };
        cancelBtn.Click += (s, e) => window.Close();
        footerStack.Children.Add(cancelBtn);
        
        var exportBtn = new System.Windows.Controls.Button
        {
            Width = 180,
            Height = 42,
            FontSize = 14,
            FontWeight = System.Windows.FontWeights.SemiBold,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new System.Windows.Thickness(0)
        };
        var exportBtnContent = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        exportBtnContent.Children.Add(new System.Windows.Controls.TextBlock { Text = "✓", FontSize = 16, Margin = new System.Windows.Thickness(0, 0, 8, 0), VerticalAlignment = System.Windows.VerticalAlignment.Center });
        exportBtnContent.Children.Add(new System.Windows.Controls.TextBlock { Text = "Approve & Export", VerticalAlignment = System.Windows.VerticalAlignment.Center });
        exportBtn.Content = exportBtnContent;
        exportBtn.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(initialsBox.Text))
            {
                System.Windows.MessageBox.Show("Please enter supervisor name and initials.", "Validation", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            SupervisorName = nameBox.Text;
            SupervisorInitials = initialsBox.Text;
            _approvalDateTime = DateTime.Now;
            IsApproved = true;
            
            window.Close();
            
            // Execute exports
            var exportFolder = SelectExportFolder();
            if (!string.IsNullOrEmpty(exportFolder))
            {
                var exportedFiles = new List<string>();
                
                if (exportPdfCheck.IsChecked == true)
                {
                    var pdfPath = System.IO.Path.Combine(exportFolder, $"GNSS_Report_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.pdf");
                    ExportPdfToPath(pdfPath);
                    exportedFiles.Add("PDF Report");
                }
                
                if (exportExcelCheck.IsChecked == true)
                {
                    var excelPath = System.IO.Path.Combine(exportFolder, $"GNSS_Data_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.xlsx");
                    ExportExcelToPath(excelPath);
                    exportedFiles.Add("Excel Workbook");
                }
                
                if (exportChartsCheck.IsChecked == true)
                {
                    ExportChartsToFolder(exportFolder);
                    exportedFiles.Add("Charts");
                }
                
                if (exportCertCheck.IsChecked == true)
                {
                    var certPath = System.IO.Path.Combine(exportFolder, $"GNSS_Certificate_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.pdf");
                    ExportDigitalCertificate(certPath);
                    exportedFiles.Add("Digital Certificate");
                }
                
                StatusMessage = $"Analysis approved and exported: {string.Join(", ", exportedFiles)}";
                System.Windows.MessageBox.Show(
                    $"Analysis approved by {SupervisorName} ({SupervisorInitials})\n\n" +
                    $"Exported to: {exportFolder}\n\n" +
                    $"Files: {string.Join(", ", exportedFiles)}",
                    "Export Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        };
        footerStack.Children.Add(exportBtn);
        
        footerBorder.Child = footerStack;
        System.Windows.Controls.Grid.SetRow(footerBorder, 2);
        mainGrid.Children.Add(footerBorder);
        
        mainBorder.Child = mainGrid;
        window.Content = mainBorder;
        
        // Enable window dragging
        window.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) window.DragMove(); };
        
        window.ShowDialog();
    }
    
    private System.Windows.Controls.Border CreatePopupCard(string title, System.Windows.Media.Brush accentColor)
    {
        var card = new System.Windows.Controls.Border
        {
            Background = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
            BorderThickness = new System.Windows.Thickness(1),
            CornerRadius = new System.Windows.CornerRadius(10),
            Padding = new System.Windows.Thickness(18),
            Margin = new System.Windows.Thickness(0, 0, 0, 15)
        };
        var stack = new System.Windows.Controls.StackPanel();
        var headerStack = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        headerStack.Children.Add(new System.Windows.Controls.Border
        {
            Width = 4,
            Height = 18,
            Background = accentColor,
            CornerRadius = new System.Windows.CornerRadius(2),
            Margin = new System.Windows.Thickness(0, 0, 10, 0)
        });
        headerStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = System.Windows.FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128))
        });
        stack.Children.Add(headerStack);
        card.Child = stack;
        return card;
    }
    
    private System.Windows.Controls.StackPanel CreatePopupField(string label, string value, string colorHex, double fontSize, bool isBold)
    {
        var stack = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(0, 5, 0, 5) };
        stack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175))
        });
        var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
        stack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = value,
            FontSize = fontSize,
            FontWeight = isBold ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal,
            Foreground = new System.Windows.Media.SolidColorBrush(color)
        });
        return stack;
    }
    
    private System.Windows.Controls.Grid CreatePopupInfoRow(string label, string value)
    {
        var grid = new System.Windows.Controls.Grid { Margin = new System.Windows.Thickness(0, 5, 0, 5) };
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(80) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        var labelTb = new System.Windows.Controls.TextBlock
        {
            Text = label + ":",
            FontSize = 12,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128))
        };
        System.Windows.Controls.Grid.SetColumn(labelTb, 0);
        grid.Children.Add(labelTb);
        var valueTb = new System.Windows.Controls.TextBlock
        {
            Text = value ?? "---",
            FontSize = 12,
            FontWeight = System.Windows.FontWeights.SemiBold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55))
        };
        System.Windows.Controls.Grid.SetColumn(valueTb, 1);
        grid.Children.Add(valueTb);
        return grid;
    }
    
    private string? SelectExportFolder()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Export Location",
            Filter = "Export Folder|*.folder",
            FileName = $"GNSS_Export_{DateTime.Now:yyyyMMdd}",
            CheckFileExists = false
        };
        
        if (dialog.ShowDialog() == true)
        {
            return System.IO.Path.GetDirectoryName(dialog.FileName);
        }
        return null;
    }
    
    private void ExportPdfToPath(string path)
    {
        try
        {
            var generator = new Services.PdfReportService();
            generator.GenerateReport(path, Project, ComparisonPoints.ToList(), Statistics!, 
                SupervisorName, SupervisorInitials, _approvalDateTime);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF export error: {ex.Message}");
        }
    }
    
    private void ExportExcelToPath(string path)
    {
        try
        {
            var exporter = new Services.ExcelExportService();
            exporter.Export(path, Project, ComparisonPoints.ToList(), Statistics!);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Excel export error: {ex.Message}");
        }
    }
    
    private void ExportChartsToFolder(string folder)
    {
        try
        {
            var baseName = $"GNSS_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}";
            
            if (DeltaScatterPlotModel != null)
                ExportPlotToPng(DeltaScatterPlotModel, System.IO.Path.Combine(folder, $"{baseName}_DeltaScatter.png"));
            if (ErrorEllipsePlotModel != null)
                ExportPlotToPng(ErrorEllipsePlotModel, System.IO.Path.Combine(folder, $"{baseName}_2DRMS.png"));
            if (TimeSeriesPlotModel != null)
                ExportPlotToPng(TimeSeriesPlotModel, System.IO.Path.Combine(folder, $"{baseName}_TimeSeries.png"));
            if (HistogramPlotModel != null)
                ExportPlotToPng(HistogramPlotModel, System.IO.Path.Combine(folder, $"{baseName}_Histogram.png"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chart export error: {ex.Message}");
        }
    }
    
    /// <summary>Exports a digital calibration certificate PDF.</summary>
    private void ExportDigitalCertificate(string path)
    {
        try
        {
            var generator = new Services.PdfReportService();
            generator.GenerateDigitalCertificate(path, Project, Statistics!, 
                SupervisorName, SupervisorInitials, _approvalDateTime, TolerancePassed);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Certificate export error: {ex.Message}");
        }
    }
    
    /// <summary>Approves the results with supervisor information.</summary>
    private void ApproveResults()
    {
        if (!CanApprove) return;
        
        // Show confirmation
        string summary = $"SUPERVISOR APPROVAL CONFIRMATION\n\n" +
                        $"Supervisor: {SupervisorName}\n" +
                        $"Initials: {SupervisorInitials}\n" +
                        $"Date/Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                        $"2DRMS Result: {Statistics?.FilteredStatistics.TwoDRMS:F4} {Project.UnitAbbreviation}\n" +
                        $"Tolerance: {Project.ToleranceValue:F4} {Project.UnitAbbreviation}\n" +
                        $"Status: {(TolerancePassed ? "PASS" : "FAIL")}\n\n" +
                        $"By clicking 'Yes', you confirm that you have reviewed\n" +
                        $"the results and approve them for export.\n\n" +
                        $"Continue with approval?";
        
        var result = System.Windows.MessageBox.Show(
            summary,
            "Confirm Approval",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            _approvalDateTime = DateTime.Now;
            IsApproved = true;
            StatusMessage = $"Results approved by {SupervisorName} ({SupervisorInitials})";
            
            System.Windows.MessageBox.Show(
                $"Results have been approved.\n\n" +
                $"You can now export reports using the export buttons.",
                "Approval Complete",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
    }
    
    private void ExportCharts()
    {
        // Use SaveFileDialog to determine export location (folder derived from path)
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select Export Location for Charts",
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"GNSS_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}_Charts",
            DefaultExt = ".png"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                var folder = System.IO.Path.GetDirectoryName(dialog.FileName)!;
                var baseName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                var exportedCount = 0;
                
                // Export all available charts
                var chartsToExport = new (PlotModel? Model, string Name, int Width, int Height)[]
                {
                    (DeltaScatterPlotModel, "01_DeltaScatter", 1200, 800),
                    (TimeSeriesPlotModel, "02_TimeSeries", 1400, 600),
                    (HistogramPlotModel, "03_Histogram", 1000, 600),
                    (CdfPlotModel, "04_CDF", 1000, 600),
                    (HeightAnalysisPlotModel, "05_HeightAnalysis", 1200, 600),
                    (PolarErrorPlotModel, "06_PolarError", 900, 900),
                    (ErrorComponentsPlotModel, "07_ErrorComponents", 1000, 600),
                    (QqPlotModel, "08_QQPlot", 800, 800),
                    (RunningStatsPlotModel, "09_RunningStats", 1200, 600),
                    (ErrorEllipsePlotModel, "10_ErrorEllipse", 900, 900),
                    (RawPositionPlotModel, "11_RawPosition", 1200, 800),
                    (FilteredPositionPlotModel, "12_FilteredPosition", 1200, 800),
                };
                
                foreach (var (model, name, width, height) in chartsToExport)
                {
                    if (model != null)
                    {
                        StatusMessage = $"Exporting {name.Substring(3)} chart...";
                        var path = System.IO.Path.Combine(folder, $"{baseName}_{name}.png");
                        ExportPlotToPng(model, path, width, height);
                        exportedCount++;
                    }
                }
                
                StatusMessage = $"Exported {exportedCount} charts to {folder}";
                System.Windows.MessageBox.Show(
                    $"Exported {exportedCount} chart images to:\n{folder}",
                    "Charts Exported",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Chart export failed: {ex.Message}";
                System.Windows.MessageBox.Show(
                    $"Chart export failed:\n{ex.Message}",
                    "Export Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private void ExportPlotToPng(PlotModel model, string filePath, int width = 1200, int height = 800)
    {
        // Use OxyPlot's built-in PNG export
        using var stream = System.IO.File.Create(filePath);
        
        // Set background on model before export
        var bgColor = IsDarkTheme ? OxyColors.Black : OxyColors.White;
        model.Background = bgColor;
        
        var pngExporter = new OxyPlot.Wpf.PngExporter 
        { 
            Width = width, 
            Height = height
        };
        pngExporter.Export(model, stream);
    }

    #endregion
    
    #region Theme Methods
    
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        RaisePropertyChanged(nameof(ThemeToggleTooltip));
        
        // Raise event for MainWindow to handle theme switch
        OnThemeChanged?.Invoke(this, IsDarkTheme);
    }
    
    /// <summary>Event raised when theme changes.</summary>
    public event EventHandler<bool>? OnThemeChanged;
    
    #endregion
    
    #region Help Methods
    
    private void ShowStepHelp()
    {
        // Open modern help window
        OnShowHelpWindow?.Invoke(this, EventArgs.Empty);
    }
    
    /// <summary>Event to request showing help window (handled by MainWindow).</summary>
    public event EventHandler? OnShowHelpWindow;
    
    private string GetStepHelpText(int stepIndex)
    {
        return stepIndex switch
        {
            1 => "Step 1: Project Setup\n\n" +
                 "Enter the basic project information:\n\n" +
                 "• Project Name: Unique identifier for this calibration session\n" +
                 "• Vessel Name: Name of the survey vessel\n" +
                 "• Survey Date: Date when the GNSS data was collected\n" +
                 "• Location: Survey area or field name\n" +
                 "• Comparison Node: Reference point identifier\n" +
                 "• Operator: Person processing the data\n" +
                 "• Coordinate Unit: Meters or Feet\n\n" +
                 "Required fields: Project Name, Vessel Name",
                 
            2 => "Step 2: Load Data\n\n" +
                 "Load your GNSS survey data file:\n\n" +
                 "• Supported formats: NPD, CSV\n" +
                 "• File should contain dual GNSS system positions\n" +
                 "• Both systems must have Easting, Northing, and Height columns\n" +
                 "• Time/Date columns are auto-detected\n\n" +
                 "The data preview shows the first rows of detected data.\n" +
                 "Column auto-detection will attempt to identify GNSS columns.",
                 
            3 => "Step 3: Configure Parameters\n\n" +
                 "Set up column mapping and processing options:\n\n" +
                 "Column Mapping:\n" +
                 "• Select the correct column for each GNSS system's E, N, H values\n" +
                 "• System 1 is typically the reference (e.g., PPP or higher accuracy)\n" +
                 "• System 2 is the system being calibrated\n\n" +
                 "Processing Settings:\n" +
                 "• Sigma Threshold: Outlier rejection level (default: 3σ)\n" +
                 "• Auto-filter: Automatically reject outliers\n\n" +
                 "Time Filter:\n" +
                 "• Enable to process only a portion of the data\n" +
                 "• Useful for excluding vessel movement periods",
                 
            4 => "Step 4: Process & Align\n\n" +
                 "Process the loaded data:\n\n" +
                 "• Click 'Process Data' to calculate differences\n" +
                 "• Delta E = System 2 Easting - System 1 Easting\n" +
                 "• Delta N = System 2 Northing - System 1 Northing\n" +
                 "• Delta H = System 2 Height - System 1 Height\n" +
                 "• Radial = √(ΔE² + ΔN²)\n\n" +
                 "Processing also calculates statistics and applies outlier filtering.",
                 
            5 => "Step 5: Quality Control\n\n" +
                 "Review and refine the data quality:\n\n" +
                 "• View all data points with their delta values\n" +
                 "• Z-Score shows how far each point is from the mean\n" +
                 "• Points with |Z-Score| > threshold are flagged\n\n" +
                 "Actions:\n" +
                 "• Apply Filter: Re-apply sigma filter with current threshold\n" +
                 "• Accept All: Mark all points as accepted\n" +
                 "• Reject Flagged: Reject all points exceeding threshold\n\n" +
                 "Click on individual points to toggle accept/reject status.",
                 
            6 => "Step 6: Results\n\n" +
                 "Review statistical results and charts:\n\n" +
                 "Statistics Tables:\n" +
                 "• Mean delta values for E, N, H\n" +
                 "• Standard deviation (σ)\n" +
                 "• 2DRMS = 2 × √(σE² + σN²)\n" +
                 "• Min/Max values\n\n" +
                 "Charts:\n" +
                 "• Delta E vs Delta N scatter plot with 2DRMS circle\n" +
                 "• Position comparison plots\n" +
                 "• Time series of deltas\n" +
                 "• Histogram of radial distances",
                 
            7 => "Step 7: Export\n\n" +
                 "Generate professional reports:\n\n" +
                 "Excel Export:\n" +
                 "• Summary sheet with project info\n" +
                 "• Statistics tables\n" +
                 "• Full data listing\n" +
                 "• Accepted and rejected points separated\n\n" +
                 "PDF Report:\n" +
                 "• Professional formatted report\n" +
                 "• Embedded charts and tables\n" +
                 "• Ready for client submission\n\n" +
                 "Export All: Creates both Excel and PDF in one click.",
                 
            _ => "No help available for this step."
        };
    }
    
    #endregion
    
    #region Time Filter Methods
    
    private void ApplyTimeFilter()
    {
        if (!TimeFilterEnabled || !FilterStartTime.HasValue) return;
        
        // Re-process data with time filter applied
        if (DataLoaded)
        {
            ProcessData();
        }
        
        RaisePropertyChanged(nameof(FilteredPointCount));
        RaisePropertyChanged(nameof(DataTimeRange));
    }
    
    private void ClearTimeFilter()
    {
        TimeFilterEnabled = false;
        
        // Reset filter times to full data range
        FilterStartTime = _dataStartTime;
        FilterEndTime = _dataEndTime;
        
        // Re-process data without time filter
        if (DataLoaded)
        {
            ProcessData();
        }
        
        RaisePropertyChanged(nameof(FilteredPointCount));
        RaisePropertyChanged(nameof(DataTimeRange));
        
        StatusMessage = "Time filter cleared - showing all data";
    }
    
    #endregion
    
    #region Enhanced Chart Methods
    
    // Maximum points to display per chart for performance
    private const int MaxChartPoints = 50000;  // Show all points for accurate representation
    
    private void CreateAllCharts()
    {
        if (ComparisonPoints.Count == 0 || Statistics == null) return;
        
        // Run chart creation on background thread for performance
        Task.Run(() =>
        {
            UpdateScatterPlot();
            CreateDeltaScatterPlot();
            CreateRawPositionPlot();
            CreateFilteredPositionPlot();
            CreateTimeSeriesPlot();
            CreateHistogramPlot();
            CreateCdfPlot();
            CreateHeightAnalysisPlot();
            CreatePolarErrorPlot();
            CreateErrorComponentsPlot();
            CreateQqPlot();
            CreateRunningStatsPlot();
            CreateErrorEllipsePlot();
        });
    }
    
    /// <summary>Decimate points for large datasets to improve performance.</summary>
    private List<T> DecimatePoints<T>(IEnumerable<T> points) where T : class
    {
        var list = points.ToList();
        if (list.Count <= MaxChartPoints) return list;
        
        // Sample evenly from the dataset
        var step = (double)list.Count / MaxChartPoints;
        var result = new List<T>(MaxChartPoints);
        for (double i = 0; i < list.Count; i += step)
        {
            result.Add(list[(int)i]);
        }
        return result;
    }
    
    /// <summary>Update all charts (for color preset changes).</summary>
    private void UpdateAllCharts() => CreateAllCharts();
    
    /// <summary>Get chart colors based on selected preset.</summary>
    private (OxyColor Accepted, OxyColor Rejected, OxyColor System1, OxyColor System2, OxyColor Circle, OxyColor Line) GetChartColors()
    {
        return _chartColorPresetIndex switch
        {
            1 => ( // High Contrast - for presentations
                OxyColor.FromRgb(0, 128, 0),      // Accepted - Pure Green
                OxyColor.FromRgb(220, 20, 60),    // Rejected - Crimson Red
                OxyColor.FromRgb(0, 0, 200),      // System1 - Pure Blue
                OxyColor.FromRgb(255, 140, 0),    // System2 - Dark Orange
                OxyColor.FromRgb(138, 43, 226),   // Circle - Blue Violet
                OxyColor.FromRgb(0, 0, 0)         // Line - Black
            ),
            2 => ( // Ocean Blue - Subsea theme
                OxyColor.FromRgb(0, 191, 165),    // Accepted - Teal
                OxyColor.FromRgb(244, 67, 54),    // Rejected - Red
                OxyColor.FromRgb(30, 58, 95),     // System1 - Subsea7 Navy
                OxyColor.FromRgb(0, 150, 199),    // System2 - Ocean Blue
                OxyColor.FromRgb(255, 152, 0),    // Circle - Orange (S7 accent)
                OxyColor.FromRgb(41, 128, 185)    // Line - Medium Blue
            ),
            3 => ( // Monochrome - for printing
                OxyColor.FromRgb(60, 60, 60),     // Accepted - Dark Gray
                OxyColor.FromRgb(180, 180, 180),  // Rejected - Light Gray
                OxyColor.FromRgb(0, 0, 0),        // System1 - Black
                OxyColor.FromRgb(120, 120, 120),  // System2 - Medium Gray
                OxyColor.FromRgb(80, 80, 80),     // Circle - Charcoal
                OxyColor.FromRgb(40, 40, 40)      // Line - Near Black
            ),
            4 => ( // Earth Tones - natural colors
                OxyColor.FromRgb(34, 139, 34),    // Accepted - Forest Green
                OxyColor.FromRgb(178, 34, 34),    // Rejected - Firebrick
                OxyColor.FromRgb(139, 90, 43),    // System1 - Saddle Brown
                OxyColor.FromRgb(184, 134, 11),   // System2 - Dark Goldenrod
                OxyColor.FromRgb(85, 107, 47),    // Circle - Dark Olive Green
                OxyColor.FromRgb(105, 105, 105)   // Line - Dim Gray
            ),
            5 => ( // Vibrant Modern - colorful
                OxyColor.FromRgb(46, 204, 113),   // Accepted - Emerald
                OxyColor.FromRgb(231, 76, 60),    // Rejected - Alizarin
                OxyColor.FromRgb(52, 152, 219),   // System1 - Peter River Blue
                OxyColor.FromRgb(155, 89, 182),   // System2 - Amethyst Purple
                OxyColor.FromRgb(241, 196, 15),   // Circle - Sun Flower Yellow
                OxyColor.FromRgb(44, 62, 80)      // Line - Wet Asphalt
            ),
            _ => ( // Subsea7 Professional (default) - Official branding
                OxyColor.FromRgb(0, 166, 81),     // Accepted - Subsea7 Green
                OxyColor.FromRgb(192, 57, 43),    // Rejected - Deep Red
                OxyColor.FromRgb(30, 58, 95),     // System1 - Subsea7 Navy Blue
                OxyColor.FromRgb(255, 152, 0),    // System2 - Subsea7 Orange
                OxyColor.FromRgb(142, 68, 173),   // Circle - Purple
                OxyColor.FromRgb(52, 73, 94)      // Line - Dark Slate
            )
        };
    }
    
    private void CreateDeltaScatterPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        // Get title from project info
        string gnss1Name = !string.IsNullOrEmpty(Project.System1Name) ? Project.System1Name : "GNSS1";
        string gnss2Name = !string.IsNullOrEmpty(Project.System2Name) ? Project.System2Name : "GNSS2";
        
        var model = new PlotModel
        {
            Title = $"GNSS Position Comparison - Delta Easting and Northing",
            Subtitle = $"{gnss1Name} v {gnss2Name}",
            TitleFontSize = 13,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            SubtitleFontSize = 11,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            PlotAreaBorderThickness = new OxyThickness(1),
            Padding = new OxyThickness(10),
            TextColor = OxyColor.FromRgb(50, 50, 50),
            PlotType = PlotType.Cartesian  // Ensure Cartesian coordinates
        };
        
        // Calculate data range for equal axis scaling
        var allPoints = ComparisonPoints.ToList();
        double minE = allPoints.Min(p => p.DeltaEasting);
        double maxE = allPoints.Max(p => p.DeltaEasting);
        double minN = allPoints.Min(p => p.DeltaNorthing);
        double maxN = allPoints.Max(p => p.DeltaNorthing);
        
        // Add padding around data
        double rangeE = maxE - minE;
        double rangeN = maxN - minN;
        double maxRange = Math.Max(rangeE, rangeN);
        
        // If 2DRMS is larger than data range, use that instead
        if (Statistics?.FilteredStatistics != null)
        {
            double twodrms = Statistics.FilteredStatistics.Delta2DRMS;
            double meanE = Statistics.FilteredStatistics.DeltaMeanEasting;
            double meanN = Statistics.FilteredStatistics.DeltaMeanNorthing;
            
            // Ensure 2DRMS circle fits
            maxRange = Math.Max(maxRange, twodrms * 2.5);
            
            // Center on mean
            double centerE = meanE;
            double centerN = meanN;
            double halfRange = maxRange / 2 * 1.3;  // 30% padding
            
            minE = centerE - halfRange;
            maxE = centerE + halfRange;
            minN = centerN - halfRange;
            maxN = centerN + halfRange;
        }
        else
        {
            // Add 20% padding
            double padding = maxRange * 0.2;
            double centerE = (minE + maxE) / 2;
            double centerN = (minN + maxN) / 2;
            double halfRange = maxRange / 2 + padding;
            
            minE = centerE - halfRange;
            maxE = centerE + halfRange;
            minN = centerN - halfRange;
            maxN = centerN + halfRange;
        }
        
        // X-Axis (Delta Easting) - with fixed range for equal scaling
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = $"Delta Easting ({unit})",
            TitleFontSize = 10,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(220, 220, 220),
            MinorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = OxyColor.FromRgb(240, 240, 240),
            AxislineStyle = LineStyle.Solid,
            AxislineColor = OxyColor.FromRgb(100, 100, 100),
            FontSize = 9,
            TickStyle = TickStyle.Outside,
            Minimum = minE,
            Maximum = maxE,
            IsZoomEnabled = true,
            IsPanEnabled = true
        });
        
        // Y-Axis (Delta Northing) - with fixed range for equal scaling
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"Delta Northing ({unit})",
            TitleFontSize = 10,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(220, 220, 220),
            MinorGridlineStyle = LineStyle.Solid,
            MinorGridlineColor = OxyColor.FromRgb(240, 240, 240),
            AxislineStyle = LineStyle.Solid,
            AxislineColor = OxyColor.FromRgb(100, 100, 100),
            FontSize = 9,
            TickStyle = TickStyle.Outside,
            Minimum = minN,
            Maximum = maxN,
            IsZoomEnabled = true,
            IsPanEnabled = true
        });
        
        // Decimate points for performance
        var accepted = DecimatePoints(ComparisonPoints.Where(p => !p.IsRejected).ToList());
        var rejected = DecimatePoints(ComparisonPoints.Where(p => p.IsRejected).ToList());
        
        // Filtered data as connected LINE series (matches reference image style)
        var filteredSeries = new LineSeries
        {
            Title = "Filtered Data",
            Color = OxyColor.FromRgb(0, 0, 200),  // Blue like reference
            StrokeThickness = 0.8,
            LineStyle = LineStyle.Solid,
            MarkerType = MarkerType.Circle,
            MarkerSize = 2,
            MarkerFill = OxyColor.FromRgb(0, 0, 200),
            TrackerFormatString = "ΔE: {2:F4}\nΔN: {4:F4}"
        };
        foreach (var point in accepted.OrderBy(p => p.DateTime))
        {
            filteredSeries.Points.Add(new DataPoint(point.DeltaEasting, point.DeltaNorthing));
        }
        model.Series.Add(filteredSeries);
        
        // Rejected points as scatter (red dots like reference)
        if (rejected.Any())
        {
            var rejectedSeries = new ScatterSeries
            {
                Title = "Rejected Data",
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = OxyColor.FromRgb(192, 57, 43),  // Red
                MarkerStroke = OxyColors.Transparent,
                MarkerStrokeThickness = 0,
                TrackerFormatString = "REJECTED\nΔE: {2:F4}\nΔN: {4:F4}"
            };
            foreach (var point in rejected)
            {
                rejectedSeries.Points.Add(new ScatterPoint(point.DeltaEasting, point.DeltaNorthing));
            }
            model.Series.Add(rejectedSeries);
        }
        
        // Add 2DRMS circle and average marker if stats available
        if (Statistics?.FilteredStatistics != null)
        {
            var stats = Statistics.FilteredStatistics;
            var twodrms = stats.Delta2DRMS;
            var meanE = stats.DeltaMeanEasting;
            var meanN = stats.DeltaMeanNorthing;
            
            // 2DRMS circle - RED like reference image
            model.Annotations.Add(new OxyPlot.Annotations.EllipseAnnotation
            {
                X = meanE, Y = meanN,
                Width = twodrms * 2, Height = twodrms * 2,
                Fill = OxyColors.Transparent,
                Stroke = OxyColor.FromRgb(200, 0, 0),  // Red circle
                StrokeThickness = 2.0
            });
            
            // Average marker - pink square like reference
            var avgSeries = new ScatterSeries
            {
                Title = "Average",
                MarkerType = MarkerType.Square,
                MarkerSize = 6,
                MarkerFill = OxyColor.FromRgb(255, 105, 180),  // Pink
                MarkerStroke = OxyColor.FromRgb(199, 21, 133),
                MarkerStrokeThickness = 1
            };
            avgSeries.Points.Add(new ScatterPoint(meanE, meanN));
            model.Series.Add(avgSeries);
            
            // Add 2DRMS circle to legend (as a dummy series for legend)
            var circleLegendSeries = new LineSeries
            {
                Title = "2DRMS",
                Color = OxyColor.FromRgb(200, 0, 0),
                StrokeThickness = 2,
                MarkerType = MarkerType.None
            };
            circleLegendSeries.Points.Add(new DataPoint(double.NaN, double.NaN));
            model.Series.Add(circleLegendSeries);
            
            // Add simple info text annotation at bottom left
            string infoText = $"2DRMS: {twodrms:F4} {unit}  |  " +
                             $"Max Radial from Avg: {stats.MaxRadialFromAverage:F4} {unit}  |  " +
                             $"Points: {Statistics.AcceptedCount}/{Statistics.AcceptedCount + Statistics.RejectedCount}";
            
            model.Annotations.Add(new OxyPlot.Annotations.TextAnnotation
            {
                Text = infoText,
                TextPosition = new DataPoint(minE + (maxE - minE) * 0.02, minN + (maxN - minN) * 0.02),
                TextHorizontalAlignment = HorizontalAlignment.Left,
                TextVerticalAlignment = VerticalAlignment.Bottom,
                Stroke = OxyColors.Transparent,
                TextColor = OxyColor.FromRgb(80, 80, 80),
                FontSize = 9,
                Background = OxyColor.FromArgb(200, 255, 255, 255),
                Padding = new OxyThickness(5)
            });
        }
        
        // Legend at right side
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.RightBottom,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Outside,
            LegendBackground = OxyColor.FromArgb(240, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendBorderThickness = 1,
            LegendFontSize = 9,
            LegendMargin = 8,
            LegendPadding = 5,
            LegendItemSpacing = 4
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => DeltaScatterPlotModel = model);
    }
    
    private void CreateRawPositionPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Position Comparison - Raw Data",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = $"Easting ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"Northing ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        // Decimate for performance
        var allPoints = DecimatePoints(ComparisonPoints.ToList());
        
        // System 1 scatter
        var system1Series = new ScatterSeries
        {
            Title = Project.System1Name,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = colors.System1,
            MarkerStroke = OxyColors.Transparent
        };
        
        // System 2 scatter
        var system2Series = new ScatterSeries
        {
            Title = Project.System2Name,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = colors.System2,
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var point in allPoints)
        {
            system1Series.Points.Add(new ScatterPoint(point.System1Easting, point.System1Northing));
            system2Series.Points.Add(new ScatterPoint(point.System2Easting, point.System2Northing));
        }
        
        model.Series.Add(system1Series);
        model.Series.Add(system2Series);
        
        // Average markers
        if (allPoints.Count > 0)
        {
            double avgE1 = allPoints.Average(p => p.System1Easting);
            double avgN1 = allPoints.Average(p => p.System1Northing);
            double avgE2 = allPoints.Average(p => p.System2Easting);
            double avgN2 = allPoints.Average(p => p.System2Northing);
            
            var avg1Series = new ScatterSeries
            {
                Title = $"Mean {Project.System1Name}",
                MarkerType = MarkerType.Diamond,
                MarkerSize = 8,
                MarkerFill = colors.System1,
                MarkerStroke = OxyColors.White,
                MarkerStrokeThickness = 2
            };
            avg1Series.Points.Add(new ScatterPoint(avgE1, avgN1));
            model.Series.Add(avg1Series);
            
            var avg2Series = new ScatterSeries
            {
                Title = $"Mean {Project.System2Name}",
                MarkerType = MarkerType.Diamond,
                MarkerSize = 8,
                MarkerFill = colors.System2,
                MarkerStroke = OxyColors.White,
                MarkerStrokeThickness = 2
            };
            avg2Series.Points.Add(new ScatterPoint(avgE2, avgN2));
            model.Series.Add(avg2Series);
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => RawPositionPlotModel = model);
    }
    
    private void CreateFilteredPositionPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Position Comparison - Filtered Data",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = $"Easting ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"Northing ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        // Decimate for performance
        var filteredPoints = DecimatePoints(ComparisonPoints.Where(p => !p.IsRejected).ToList());
        
        var system1Series = new ScatterSeries
        {
            Title = Project.System1Name,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = colors.System1,
            MarkerStroke = OxyColors.Transparent
        };
        
        var system2Series = new ScatterSeries
        {
            Title = Project.System2Name,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = colors.System2,
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var point in filteredPoints)
        {
            system1Series.Points.Add(new ScatterPoint(point.System1Easting, point.System1Northing));
            system2Series.Points.Add(new ScatterPoint(point.System2Easting, point.System2Northing));
        }
        
        model.Series.Add(system1Series);
        model.Series.Add(system2Series);
        
        if (filteredPoints.Count > 0)
        {
            double avgE1 = filteredPoints.Average(p => p.System1Easting);
            double avgN1 = filteredPoints.Average(p => p.System1Northing);
            double avgE2 = filteredPoints.Average(p => p.System2Easting);
            double avgN2 = filteredPoints.Average(p => p.System2Northing);
            
            var avg1Series = new ScatterSeries
            {
                Title = $"Mean {Project.System1Name}",
                MarkerType = MarkerType.Diamond, MarkerSize = 8,
                MarkerFill = colors.System1, MarkerStroke = OxyColors.White, MarkerStrokeThickness = 2
            };
            avg1Series.Points.Add(new ScatterPoint(avgE1, avgN1));
            model.Series.Add(avg1Series);
            
            var avg2Series = new ScatterSeries
            {
                Title = $"Mean {Project.System2Name}",
                MarkerType = MarkerType.Diamond, MarkerSize = 8,
                MarkerFill = colors.System2, MarkerStroke = OxyColors.White, MarkerStrokeThickness = 2
            };
            avg2Series.Points.Add(new ScatterPoint(avgE2, avgN2));
            model.Series.Add(avg2Series);
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => FilteredPositionPlotModel = model);
    }
    
    private void CreateTimeSeriesPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Delta Values Over Time",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        // Time axis
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            TitleFontSize = 11,
            StringFormat = "HH:mm:ss",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"Delta Value ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        // Decimate for performance
        var filteredPoints = DecimatePoints(ComparisonPoints.Where(p => !p.IsRejected).OrderBy(p => p.DateTime).ToList());
        
        // Delta Easting series (line only, no markers for speed)
        var deltaESeries = new LineSeries
        {
            Title = $"ΔE ({unit})",
            Color = OxyColor.FromRgb(41, 128, 185),
            StrokeThickness = 1.5,
            MarkerType = MarkerType.Circle,
            MarkerSize = 2,
            MarkerFill = OxyColor.FromRgb(41, 128, 185),
            TrackerFormatString = "Time: {2:HH:mm:ss}\nΔE: {4:F4} {5}"
        };
        
        // Delta Northing series
        var deltaNSeries = new LineSeries
        {
            Title = $"ΔN ({unit})",
            Color = OxyColor.FromRgb(230, 126, 34),
            StrokeThickness = 1.5,
            MarkerType = MarkerType.Circle,
            MarkerSize = 2,
            MarkerFill = OxyColor.FromRgb(230, 126, 34),
            TrackerFormatString = "Time: {2:HH:mm:ss}\nΔN: {4:F4} {5}"
        };
        
        // Radial error series
        var radialSeries = new LineSeries
        {
            Title = $"Radial ({unit})",
            Color = OxyColor.FromRgb(155, 89, 182),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 2,
            MarkerFill = OxyColor.FromRgb(155, 89, 182),
            TrackerFormatString = "Time: {2:HH:mm:ss}\nRadial: {4:F4} {5}"
        };
        
        foreach (var point in filteredPoints)
        {
            var x = DateTimeAxis.ToDouble(point.DateTime);
            deltaESeries.Points.Add(new DataPoint(x, point.DeltaEasting));
            deltaNSeries.Points.Add(new DataPoint(x, point.DeltaNorthing));
            radialSeries.Points.Add(new DataPoint(x, point.RadialError));
        }
        
        model.Series.Add(deltaESeries);
        model.Series.Add(deltaNSeries);
        model.Series.Add(radialSeries);
        
        // Zero reference line
        model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
        {
            Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
            Y = 0,
            Color = OxyColor.FromRgb(150, 150, 150),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash
        });
        
        // 2DRMS reference line
        if (Statistics?.FilteredStatistics != null)
        {
            var twodrms = Statistics.FilteredStatistics.Delta2DRMS;
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                Y = twodrms,
                Color = colors.Circle,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                Text = $"2DRMS = {twodrms:F4}",
                TextColor = colors.Circle
            });
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => TimeSeriesPlotModel = model);
    }
    
    private void CreateHistogramPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Radial Error Distribution",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = $"Radial Error ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.None,
            FontSize = 9,
            Minimum = 0
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Frequency",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9,
            Minimum = 0
        });
        
        var acceptedRadials = ComparisonPoints.Where(p => !p.IsRejected).Select(p => p.RadialError).ToList();
        
        if (acceptedRadials.Count > 0)
        {
            var min = 0.0;
            var max = acceptedRadials.Max() * 1.1;
            var binCount = Math.Min(20, Math.Max(10, acceptedRadials.Count / 20));
            var binWidth = (max - min) / binCount;
            
            var histogram = new RectangleBarSeries
            {
                Title = "Frequency",
                FillColor = OxyColor.FromArgb(180, colors.Accepted.R, colors.Accepted.G, colors.Accepted.B),
                StrokeColor = colors.Accepted,
                StrokeThickness = 1
            };
            
            for (int i = 0; i < binCount; i++)
            {
                var binStart = min + i * binWidth;
                var binEnd = binStart + binWidth;
                var count = acceptedRadials.Count(r => r >= binStart && r < binEnd);
                histogram.Items.Add(new RectangleBarItem(binStart, 0, binEnd, count));
            }
            
            model.Series.Add(histogram);
        }
        
        // Statistical reference lines
        if (Statistics?.FilteredStatistics != null && acceptedRadials.Count > 0)
        {
            var stats = Statistics.FilteredStatistics;
            
            // Mean line
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
                X = stats.MeanRadialError,
                Color = OxyColor.FromRgb(41, 128, 185),
                StrokeThickness = 2,
                Text = $"Mean = {stats.MeanRadialError:F4}",
                TextColor = OxyColor.FromRgb(41, 128, 185)
            });
            
            // 2DRMS line
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
                X = stats.Delta2DRMS,
                Color = colors.Circle,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                Text = $"2DRMS = {stats.Delta2DRMS:F4}",
                TextColor = colors.Circle
            });
            
            // CEP95 line
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
                X = stats.Cep95,
                Color = OxyColor.FromRgb(230, 126, 34),
                StrokeThickness = 1.5,
                LineStyle = LineStyle.DashDot,
                Text = $"CEP95 = {stats.Cep95:F4}",
                TextColor = OxyColor.FromRgb(230, 126, 34)
            });
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => HistogramPlotModel = model);
    }
    
    /// <summary>Create CDF (Cumulative Distribution Function) chart.</summary>
    private void CreateCdfPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Cumulative Distribution Function",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = $"Radial Error ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9,
            Minimum = 0
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Cumulative Probability (%)",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9,
            Minimum = 0,
            Maximum = 100
        });
        
        var acceptedRadials = ComparisonPoints.Where(p => !p.IsRejected)
            .Select(p => p.RadialError)
            .OrderBy(r => r)
            .ToList();
        
        if (acceptedRadials.Count > 0)
        {
            // CDF series
            var cdfSeries = new LineSeries
            {
                Title = "CDF",
                Color = colors.System1,
                StrokeThickness = 2.5,
                MarkerType = MarkerType.None
            };
            
            for (int i = 0; i < acceptedRadials.Count; i++)
            {
                double percentile = (i + 1) * 100.0 / acceptedRadials.Count;
                cdfSeries.Points.Add(new DataPoint(acceptedRadials[i], percentile));
            }
            model.Series.Add(cdfSeries);
            
            // Add reference lines
            if (Statistics?.FilteredStatistics != null)
            {
                var stats = Statistics.FilteredStatistics;
                
                // 50% line (CEP50)
                model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
                {
                    Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                    Y = 50,
                    Color = OxyColor.FromRgb(52, 152, 219),
                    StrokeThickness = 1.5,
                    LineStyle = LineStyle.Dash,
                    Text = $"50% @ {stats.Cep50:F4}"
                });
                
                // 95% line (CEP95)
                model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
                {
                    Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                    Y = 95,
                    Color = OxyColor.FromRgb(230, 126, 34),
                    StrokeThickness = 1.5,
                    LineStyle = LineStyle.Dash,
                    Text = $"95% @ {stats.Cep95:F4}"
                });
                
                // 2DRMS vertical line
                model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
                {
                    Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
                    X = stats.Delta2DRMS,
                    Color = colors.Circle,
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Solid,
                    Text = $"2DRMS = {stats.Delta2DRMS:F4}"
                });
            }
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.BottomRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => CdfPlotModel = model);
    }
    
    /// <summary>Create height analysis chart (ΔH over time).</summary>
    private void CreateHeightAnalysisPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Height Difference Analysis",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            TitleFontSize = 11,
            StringFormat = "HH:mm:ss",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"ΔH ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        var filteredPoints = DecimatePoints(ComparisonPoints.Where(p => !p.IsRejected).OrderBy(p => p.DateTime).ToList());
        
        // ΔH series
        var deltaHSeries = new LineSeries
        {
            Title = $"ΔH ({unit})",
            Color = OxyColor.FromRgb(155, 89, 182),
            StrokeThickness = 1.5,
            MarkerType = MarkerType.None
        };
        
        foreach (var point in filteredPoints)
        {
            var x = DateTimeAxis.ToDouble(point.DateTime);
            deltaHSeries.Points.Add(new DataPoint(x, point.DeltaHeight));
        }
        model.Series.Add(deltaHSeries);
        
        // Reference lines
        if (Statistics?.FilteredStatistics != null)
        {
            var stats = Statistics.FilteredStatistics;
            
            // Zero line
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                Y = 0,
                Color = OxyColor.FromRgb(100, 100, 100),
                StrokeThickness = 1,
                LineStyle = LineStyle.Solid
            });
            
            // Mean ΔH line
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                Y = stats.MeanDeltaHeight,
                Color = OxyColor.FromRgb(41, 128, 185),
                StrokeThickness = 1.5,
                LineStyle = LineStyle.Dash,
                Text = $"Mean = {stats.MeanDeltaHeight:F4}"
            });
            
            // ±1σ bands
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                Y = stats.MeanDeltaHeight + stats.StdDevDeltaHeight,
                Color = OxyColor.FromRgb(46, 204, 113),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot,
                Text = $"+1σ = {stats.MeanDeltaHeight + stats.StdDevDeltaHeight:F4}"
            });
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                Y = stats.MeanDeltaHeight - stats.StdDevDeltaHeight,
                Color = OxyColor.FromRgb(46, 204, 113),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot,
                Text = $"-1σ = {stats.MeanDeltaHeight - stats.StdDevDeltaHeight:F4}"
            });
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => HeightAnalysisPlotModel = model);
    }
    
    /// <summary>Create polar error plot (errors by direction).</summary>
    private void CreatePolarErrorPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Error Direction Analysis",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        // X axis: bearing/direction
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Bearing (°)",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9,
            Minimum = 0,
            Maximum = 360,
            MajorStep = 45
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"Radial Error ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9,
            Minimum = 0
        });
        
        var filteredPoints = DecimatePoints(ComparisonPoints.Where(p => !p.IsRejected).ToList());
        
        // Calculate bearing for each point
        var polarSeries = new ScatterSeries
        {
            Title = "Errors by Direction",
            MarkerType = MarkerType.Circle,
            MarkerSize = 3,
            MarkerFill = colors.Accepted,
            MarkerStroke = OxyColors.Transparent
        };
        
        foreach (var point in filteredPoints)
        {
            // Calculate bearing from delta values (0° = North, clockwise)
            double bearing = Math.Atan2(point.DeltaEasting, point.DeltaNorthing) * 180 / Math.PI;
            if (bearing < 0) bearing += 360;
            
            polarSeries.Points.Add(new ScatterPoint(bearing, point.RadialError));
        }
        model.Series.Add(polarSeries);
        
        // Add 2DRMS reference line
        if (Statistics?.FilteredStatistics != null)
        {
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                Y = Statistics.FilteredStatistics.Delta2DRMS,
                Color = colors.Circle,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                Text = $"2DRMS = {Statistics.FilteredStatistics.Delta2DRMS:F4}"
            });
        }
        
        // Add cardinal direction labels
        foreach (var (angle, label) in new[] { (0, "N"), (90, "E"), (180, "S"), (270, "W") })
        {
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
                X = angle,
                Color = OxyColor.FromArgb(60, 100, 100, 100),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot
            });
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => PolarErrorPlotModel = model);
    }
    
    /// <summary>Create error components comparison chart.</summary>
    private void CreateErrorComponentsPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Error Components Comparison - Mean, Std Dev, RMS",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(10),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        // Use LinearAxis for Y (value axis - vertical bars)
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"Value ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9,
            Minimum = 0
        });
        
        // Use LinearAxis for X with custom labels
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Component",
            TitleFontSize = 11,
            Minimum = -0.5,
            Maximum = 3.5,
            MajorStep = 1,
            MinorStep = 1,
            FontSize = 9,
            LabelFormatter = x => x switch
            {
                0 => "ΔE",
                1 => "ΔN",
                2 => "ΔH",
                3 => "Radial",
                _ => ""
            }
        });
        
        if (Statistics?.FilteredStatistics != null)
        {
            var stats = Statistics.FilteredStatistics;
            double barWidth = 0.25;  // Width of each bar
            double groupSpacing = 0.05;  // Space between groups
            
            // Mean bars (Blue)
            var meanSeries = new RectangleBarSeries
            {
                Title = "Mean",
                FillColor = OxyColor.FromRgb(41, 128, 185),
                StrokeColor = OxyColors.White,
                StrokeThickness = 1
            };
            double[] meanValues = { Math.Abs(stats.MeanDeltaEasting), Math.Abs(stats.MeanDeltaNorthing), 
                                    Math.Abs(stats.MeanDeltaHeight), Math.Abs(stats.MeanRadialError) };
            for (int i = 0; i < 4; i++)
            {
                double x = i - barWidth - groupSpacing;
                meanSeries.Items.Add(new RectangleBarItem(x, 0, x + barWidth, meanValues[i]));
            }
            model.Series.Add(meanSeries);
            
            // StdDev bars (Orange)
            var stdDevSeries = new RectangleBarSeries
            {
                Title = "Std Dev",
                FillColor = OxyColor.FromRgb(230, 126, 34),
                StrokeColor = OxyColors.White,
                StrokeThickness = 1
            };
            double[] stdDevValues = { stats.StdDevDeltaEasting, stats.StdDevDeltaNorthing, 
                                      stats.StdDevDeltaHeight, stats.SdRadialDistance };
            for (int i = 0; i < 4; i++)
            {
                double x = i;
                stdDevSeries.Items.Add(new RectangleBarItem(x, 0, x + barWidth, stdDevValues[i]));
            }
            model.Series.Add(stdDevSeries);
            
            // RMS bars (Purple)
            var rmsSeries = new RectangleBarSeries
            {
                Title = "RMS",
                FillColor = OxyColor.FromRgb(155, 89, 182),
                StrokeColor = OxyColors.White,
                StrokeThickness = 1
            };
            double[] rmsValues = { stats.RmsDeltaEasting, stats.RmsDeltaNorthing, 
                                   stats.RmsDeltaHeight, stats.RmsRadial };
            for (int i = 0; i < 4; i++)
            {
                double x = i + barWidth + groupSpacing;
                rmsSeries.Items.Add(new RectangleBarItem(x, 0, x + barWidth, rmsValues[i]));
            }
            model.Series.Add(rmsSeries);
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9,
            LegendOrientation = OxyPlot.Legends.LegendOrientation.Horizontal
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => ErrorComponentsPlotModel = model);
    }
    
    /// <summary>Create Q-Q (Quantile-Quantile) probability plot to check normality.</summary>
    private void CreateQqPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Q-Q Plot (Normal Probability)",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Theoretical Quantiles (Standard Normal)",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"Sample Quantiles - Radial Error ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        var radialErrors = ComparisonPoints.Where(p => !p.IsRejected)
            .Select(p => p.RadialError)
            .OrderBy(r => r)
            .ToList();
        
        if (radialErrors.Count > 1)
        {
            int n = radialErrors.Count;
            var qqSeries = new ScatterSeries
            {
                Title = "Data Points",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = colors.Accepted,
                MarkerStroke = OxyColors.Transparent
            };
            
            // Calculate theoretical quantiles (inverse normal CDF)
            for (int i = 0; i < n; i++)
            {
                double p = (i + 0.5) / n;  // Plotting position
                double z = NormalInverseCdf(p);  // Theoretical quantile
                qqSeries.Points.Add(new ScatterPoint(z, radialErrors[i]));
            }
            model.Series.Add(qqSeries);
            
            // Add reference line (if data is normal, points should fall on this line)
            var stats = Statistics?.FilteredStatistics;
            if (stats != null)
            {
                double mean = stats.MeanRadialError;
                double sd = stats.SdRadialDistance;
                
                // Line from -3 to +3 standard deviations
                var refLine = new LineSeries
                {
                    Title = "Normal Reference",
                    Color = colors.Circle,
                    StrokeThickness = 2,
                    LineStyle = LineStyle.Dash
                };
                refLine.Points.Add(new DataPoint(-3, mean - 3 * sd));
                refLine.Points.Add(new DataPoint(3, mean + 3 * sd));
                model.Series.Add(refLine);
            }
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.BottomRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => QqPlotModel = model);
    }
    
    /// <summary>Inverse normal CDF (probit function) approximation.</summary>
    private static double NormalInverseCdf(double p)
    {
        // Rational approximation for inverse normal CDF (Abramowitz & Stegun)
        if (p <= 0) return double.NegativeInfinity;
        if (p >= 1) return double.PositiveInfinity;
        if (p == 0.5) return 0;
        
        double t = p < 0.5 ? Math.Sqrt(-2 * Math.Log(p)) : Math.Sqrt(-2 * Math.Log(1 - p));
        double c0 = 2.515517, c1 = 0.802853, c2 = 0.010328;
        double d1 = 1.432788, d2 = 0.189269, d3 = 0.001308;
        double z = t - (c0 + c1 * t + c2 * t * t) / (1 + d1 * t + d2 * t * t + d3 * t * t * t);
        return p < 0.5 ? -z : z;
    }
    
    /// <summary>Create running statistics plot (moving average of radial error).</summary>
    private void CreateRunningStatsPlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Running Statistics (Moving Window)",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        model.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            TitleFontSize = 11,
            StringFormat = "HH:mm:ss",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"Radial Error ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9,
            Minimum = 0
        });
        
        var points = ComparisonPoints.Where(p => !p.IsRejected).OrderBy(p => p.DateTime).ToList();
        
        if (points.Count > 10)
        {
            int windowSize = Math.Max(10, points.Count / 50);  // Adaptive window size
            
            // Raw data (decimated)
            var rawSeries = new LineSeries
            {
                Title = "Raw Error",
                Color = OxyColor.FromArgb(80, colors.Accepted.R, colors.Accepted.G, colors.Accepted.B),
                StrokeThickness = 1,
                MarkerType = MarkerType.None
            };
            
            var decimated = DecimatePoints(points);
            foreach (var p in decimated)
            {
                rawSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(p.DateTime), p.RadialError));
            }
            model.Series.Add(rawSeries);
            
            // Moving average
            var maSeries = new LineSeries
            {
                Title = $"Moving Avg ({windowSize} pts)",
                Color = colors.System1,
                StrokeThickness = 2.5,
                MarkerType = MarkerType.None
            };
            
            for (int i = windowSize - 1; i < points.Count; i++)
            {
                double sum = 0;
                for (int j = i - windowSize + 1; j <= i; j++)
                {
                    sum += points[j].RadialError;
                }
                double avg = sum / windowSize;
                maSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(points[i].DateTime), avg));
            }
            model.Series.Add(maSeries);
            
            // Moving standard deviation band (upper)
            var upperSeries = new LineSeries
            {
                Title = "Upper 1σ Band",
                Color = OxyColor.FromArgb(150, colors.Circle.R, colors.Circle.G, colors.Circle.B),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash,
                MarkerType = MarkerType.None
            };
            
            var lowerSeries = new LineSeries
            {
                Title = "Lower 1σ Band",
                Color = OxyColor.FromArgb(150, colors.Circle.R, colors.Circle.G, colors.Circle.B),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash,
                MarkerType = MarkerType.None
            };
            
            for (int i = windowSize - 1; i < points.Count; i++)
            {
                var window = new List<double>();
                for (int j = i - windowSize + 1; j <= i; j++)
                {
                    window.Add(points[j].RadialError);
                }
                double avg = window.Average();
                double variance = window.Sum(v => Math.Pow(v - avg, 2)) / window.Count;
                double sd = Math.Sqrt(variance);
                
                var t = DateTimeAxis.ToDouble(points[i].DateTime);
                upperSeries.Points.Add(new DataPoint(t, avg + sd));
                lowerSeries.Points.Add(new DataPoint(t, Math.Max(0, avg - sd)));
            }
            model.Series.Add(upperSeries);
            model.Series.Add(lowerSeries);
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => RunningStatsPlotModel = model);
    }
    
    /// <summary>Create error ellipse plot with confidence ellipses.</summary>
    private void CreateErrorEllipsePlot()
    {
        var colors = GetChartColors();
        string unit = Project.CoordinateUnit ?? "m";
        
        var model = new PlotModel
        {
            Title = "Confidence Ellipses (ΔE vs ΔN)",
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Padding = new OxyThickness(5),
            TextColor = OxyColor.FromRgb(50, 50, 50)
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = $"ΔE ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = $"ΔN ({unit})",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(230, 230, 230),
            MinorGridlineStyle = LineStyle.None,
            FontSize = 9
        });
        
        var accepted = ComparisonPoints.Where(p => !p.IsRejected).ToList();
        
        if (accepted.Count > 2)
        {
            // Calculate covariance matrix
            double meanE = accepted.Average(p => p.DeltaEasting);
            double meanN = accepted.Average(p => p.DeltaNorthing);
            
            double varE = accepted.Sum(p => Math.Pow(p.DeltaEasting - meanE, 2)) / accepted.Count;
            double varN = accepted.Sum(p => Math.Pow(p.DeltaNorthing - meanN, 2)) / accepted.Count;
            double covEN = accepted.Sum(p => (p.DeltaEasting - meanE) * (p.DeltaNorthing - meanN)) / accepted.Count;
            
            // Add confidence ellipses at 39.3% (1σ), 86.5% (2σ), and 98.9% (3σ)
            var chiSquareValues = new[] { (1.0, "39%", OxyColor.FromRgb(52, 152, 219)), 
                                           (2.3, "68%", OxyColor.FromRgb(46, 204, 113)), 
                                           (6.18, "95%", OxyColor.FromRgb(230, 126, 34)),
                                           (11.83, "99%", colors.Circle) };
            
            foreach (var (chiSq, label, color) in chiSquareValues)
            {
                var ellipseSeries = new LineSeries
                {
                    Title = $"{label} Confidence",
                    Color = color,
                    StrokeThickness = label == "95%" ? 2 : 1.5,
                    LineStyle = label == "95%" ? LineStyle.Solid : LineStyle.Dash,
                    MarkerType = MarkerType.None
                };
                
                // Generate ellipse points using eigenvalue decomposition
                for (int i = 0; i <= 100; i++)
                {
                    double theta = 2 * Math.PI * i / 100;
                    
                    // Standard parametric ellipse, scaled by sqrt(chi-square)
                    double scale = Math.Sqrt(chiSq);
                    double x = scale * Math.Sqrt(varE) * Math.Cos(theta);
                    double y = scale * Math.Sqrt(varN) * Math.Sin(theta);
                    
                    // Apply correlation rotation
                    double correlation = covEN / (Math.Sqrt(varE) * Math.Sqrt(varN));
                    double rotAngle = 0.5 * Math.Atan2(2 * covEN, varE - varN);
                    
                    double xRot = x * Math.Cos(rotAngle) - y * Math.Sin(rotAngle);
                    double yRot = x * Math.Sin(rotAngle) + y * Math.Cos(rotAngle);
                    
                    ellipseSeries.Points.Add(new DataPoint(meanE + xRot, meanN + yRot));
                }
                model.Series.Add(ellipseSeries);
            }
            
            // Add data points
            var decimated = DecimatePoints(accepted);
            var pointSeries = new ScatterSeries
            {
                Title = $"Data ({decimated.Count} pts)",
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = OxyColor.FromArgb(100, colors.Accepted.R, colors.Accepted.G, colors.Accepted.B),
                MarkerStroke = OxyColors.Transparent
            };
            
            foreach (var p in decimated)
            {
                pointSeries.Points.Add(new ScatterPoint(p.DeltaEasting, p.DeltaNorthing));
            }
            model.Series.Add(pointSeries);
            
            // Add center marker using crosshair lines
            double markerSize = Math.Max(Math.Sqrt(varE), Math.Sqrt(varN)) * 0.3;
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                Y = meanN,
                MinimumX = meanE - markerSize,
                MaximumX = meanE + markerSize,
                Color = OxyColors.Black,
                StrokeThickness = 2
            });
            model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
                X = meanE,
                MinimumY = meanN - markerSize,
                MaximumY = meanN + markerSize,
                Color = OxyColors.Black,
                StrokeThickness = 2
            });
            // Mean label
            model.Annotations.Add(new OxyPlot.Annotations.TextAnnotation
            {
                TextPosition = new DataPoint(meanE, meanN + markerSize * 1.5),
                Text = "Mean",
                FontSize = 10,
                TextColor = OxyColors.Black,
                Stroke = OxyColors.Transparent
            });
        }
        
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(220, 255, 255, 255),
            LegendBorder = OxyColor.FromRgb(200, 200, 200),
            LegendFontSize = 9
        });
        
        model.ResetAllAxes();
        System.Windows.Application.Current?.Dispatcher.Invoke(() => ErrorEllipsePlotModel = model);
    }

    #endregion
    
    #region Project File Methods
    
    /// <summary>
    /// Saves the project to the current file path, or prompts for a new path.
    /// </summary>
    private void SaveProject()
    {
        if (string.IsNullOrEmpty(CurrentProjectFilePath))
        {
            SaveProjectAs();
            return;
        }
        
        SaveProjectToFile(CurrentProjectFilePath);
    }
    
    /// <summary>
    /// Prompts for a file path and saves the project.
    /// </summary>
    private void SaveProjectAs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save GNSS Project",
            Filter = "GNSS Project (*.gnss)|*.gnss",
            FileName = !string.IsNullOrEmpty(Project.ProjectName) 
                ? $"{Project.ProjectName}.gnss" 
                : "GnssProject.gnss",
            DefaultExt = ".gnss",
            InitialDirectory = !string.IsNullOrEmpty(_settings.LastProjectFolder) 
                ? _settings.LastProjectFolder 
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        
        if (dialog.ShowDialog() == true)
        {
            SaveProjectToFile(dialog.FileName);
        }
    }
    
    /// <summary>
    /// Saves the project to the specified file path.
    /// </summary>
    private void SaveProjectToFile(string filePath)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Saving project...";
            
            var result = _projectFileService.SaveProject(Project, filePath);
            
            if (result.Success)
            {
                CurrentProjectFilePath = filePath;
                HasUnsavedChanges = false;
                
                // Update settings
                _settings.UpdateFolderFromFile(filePath, FolderType.Project);
                _settings.AddRecentProjectFile(filePath);
                _settings.Save();
                
                RaisePropertyChanged(nameof(RecentProjectFiles));
                StatusMessage = result.Message;
            }
            else
            {
                StatusMessage = result.Message;
                System.Windows.MessageBox.Show(result.Message, "Save Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Opens a project from file.
    /// </summary>
    private void OpenProject()
    {
        // Check for unsaved changes
        if (HasUnsavedChanges)
        {
            var response = System.Windows.MessageBox.Show(
                "You have unsaved changes. Do you want to save before opening a new project?",
                "Unsaved Changes",
                System.Windows.MessageBoxButton.YesNoCancel,
                System.Windows.MessageBoxImage.Question);
            
            if (response == System.Windows.MessageBoxResult.Cancel)
                return;
            
            if (response == System.Windows.MessageBoxResult.Yes)
                SaveProject();
        }
        
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Open GNSS Project",
            Filter = "GNSS Project (*.gnss)|*.gnss|All Files (*.*)|*.*",
            InitialDirectory = !string.IsNullOrEmpty(_settings.LastProjectFolder)
                ? _settings.LastProjectFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        
        if (dialog.ShowDialog() == true)
        {
            LoadProjectFromFile(dialog.FileName);
        }
    }
    
    /// <summary>
    /// Loads a project from the specified file path.
    /// </summary>
    public void LoadProjectFromFile(string filePath)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading project...";
            
            var result = _projectFileService.LoadProject(filePath);
            
            if (result.Success && result.Project != null)
            {
                // Apply loaded project
                Project = result.Project;
                CurrentProjectFilePath = filePath;
                HasUnsavedChanges = false;
                
                // Update settings
                _settings.UpdateFolderFromFile(filePath, FolderType.Project);
                _settings.AddRecentProjectFile(filePath);
                _settings.Save();
                
                RaisePropertyChanged(nameof(RecentProjectFiles));
                StatusMessage = result.Message;
                
                // Reset to step 1
                GoToStep(1);
            }
            else
            {
                StatusMessage = result.Message;
                System.Windows.MessageBox.Show(result.Message, "Load Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    #endregion
    
    #region Batch Processing Methods
    
    /// <summary>
    /// Adds files to the batch processing queue.
    /// </summary>
    private void AddBatchFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Add Files for Batch Processing",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Multiselect = true,
            InitialDirectory = !string.IsNullOrEmpty(_settings.LastNpdFolder)
                ? _settings.LastNpdFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                // Don't add duplicates
                if (!BatchFiles.Any(f => f.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                {
                    BatchFiles.Add(new BatchFileItem { FilePath = file });
                }
            }
            
            StatusMessage = $"Added {dialog.FileNames.Length} file(s) to batch queue. Total: {BatchFiles.Count}";
        }
    }
    
    /// <summary>
    /// Removes selected batch file from queue.
    /// </summary>
    private void RemoveSelectedBatchFile()
    {
        // Remove first pending item (or implement proper selection)
        var pending = BatchFiles.FirstOrDefault(f => f.Status == BatchItemStatus.Pending);
        if (pending != null)
        {
            BatchFiles.Remove(pending);
            StatusMessage = $"Removed file from batch queue. Remaining: {BatchFiles.Count}";
        }
    }
    
    /// <summary>
    /// Clears all batch files.
    /// </summary>
    private void ClearBatchFiles()
    {
        BatchFiles.Clear();
        BatchProgress = 0;
        BatchStatusMessage = "";
        StatusMessage = "Batch queue cleared";
    }
    
    /// <summary>
    /// Runs batch processing on all queued files.
    /// </summary>
    private async void RunBatchProcess()
    {
        if (BatchFiles.Count == 0) return;
        
        try
        {
            IsBatchProcessing = true;
            IsBusy = true;
            BatchProgress = 0;
            
            int processed = 0;
            int total = BatchFiles.Count;
            int passed = 0;
            int failed = 0;
            
            foreach (var item in BatchFiles)
            {
                if (item.Status != BatchItemStatus.Pending) continue;
                
                item.Status = BatchItemStatus.Processing;
                item.StatusMessage = "Processing...";
                BatchStatusMessage = $"Processing {item.FileName}...";
                
                var result = await Task.Run(() => ProcessBatchFile(item.FilePath));
                
                if (result.Success)
                {
                    item.Status = BatchItemStatus.Completed;
                    item.TwoDrms = result.TwoDrms;
                    item.TotalPoints = result.TotalPoints;
                    item.AcceptedPoints = result.AcceptedPoints;
                    item.TolerancePassed = result.TolerancePassed;
                    item.ProcessingTimeMs = result.ProcessingTimeMs;
                    item.StatusMessage = $"2DRMS: {result.TwoDrms:F4}";
                    
                    if (result.TolerancePassed) passed++;
                    
                    // Add to history
                    if (result.Statistics != null)
                    {
                        var historyEntry = ComparisonHistoryEntry.FromResults(
                            Project, result.Statistics, result.TotalPoints, result.ProcessingTimeMs);
                        historyEntry.NpdFileName = Path.GetFileName(item.FilePath);
                        _comparisonHistory.Add(historyEntry);
                    }
                }
                else
                {
                    item.Status = BatchItemStatus.Failed;
                    item.StatusMessage = result.ErrorMessage;
                    failed++;
                }
                
                processed++;
                BatchProgress = (int)((double)processed / total * 100);
            }
            
            BatchStatusMessage = $"Batch complete: {passed} passed, {failed} failed out of {total}";
            StatusMessage = BatchStatusMessage;
            
            RaisePropertyChanged(nameof(HistoryEntries));
        }
        finally
        {
            IsBatchProcessing = false;
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Processes a single file in batch mode.
    /// </summary>
    private BatchProcessResult ProcessBatchFile(string filePath)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Create a temporary project with this file
            var batchProject = new GnssProject
            {
                NpdFilePath = filePath,
                ProjectName = Path.GetFileNameWithoutExtension(filePath),
                SigmaThreshold = Project.SigmaThreshold,
                ToleranceValue = Project.ToleranceValue,
                ToleranceCheckEnabled = Project.ToleranceCheckEnabled,
                Unit = Project.Unit,
                UsePosAsReference = false // Batch doesn't use POS
            };
            
            // Copy column mapping
            batchProject.ColumnMapping.System1EastingPattern = Project.ColumnMapping.System1EastingPattern;
            batchProject.ColumnMapping.System1NorthingPattern = Project.ColumnMapping.System1NorthingPattern;
            batchProject.ColumnMapping.System1HeightPattern = Project.ColumnMapping.System1HeightPattern;
            batchProject.ColumnMapping.System2EastingPattern = Project.ColumnMapping.System2EastingPattern;
            batchProject.ColumnMapping.System2NorthingPattern = Project.ColumnMapping.System2NorthingPattern;
            batchProject.ColumnMapping.System2HeightPattern = Project.ColumnMapping.System2HeightPattern;
            batchProject.ColumnMapping.TimeColumnIndex = Project.ColumnMapping.TimeColumnIndex;
            batchProject.ColumnMapping.DateColumnIndex = Project.ColumnMapping.DateColumnIndex;
            batchProject.ColumnMapping.DateFormat = Project.ColumnMapping.DateFormat;
            batchProject.ColumnMapping.TimeFormat = Project.ColumnMapping.TimeFormat;
            batchProject.ColumnMapping.HasDateTimeSplit = Project.ColumnMapping.HasDateTimeSplit;
            
            // Process data
            var processResult = _processingService.Process(batchProject);
            
            stopwatch.Stop();
            
            if (processResult.Success && processResult.Statistics != null)
            {
                // Apply tolerance settings
                processResult.Statistics.ToleranceCheckEnabled = Project.ToleranceCheckEnabled;
                processResult.Statistics.ToleranceValue = Project.ToleranceValue;
                processResult.Statistics.ToleranceUnit = Project.UnitAbbreviation;
                
                return new BatchProcessResult
                {
                    FilePath = filePath,
                    Success = true,
                    TwoDrms = processResult.Statistics.FilteredStatistics.Delta2DRMS,
                    TotalPoints = processResult.Points.Count,
                    AcceptedPoints = processResult.Statistics.AcceptedCount,
                    TolerancePassed = processResult.Statistics.PassesTolerance,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Statistics = processResult.Statistics
                };
            }
            else
            {
                return new BatchProcessResult
                {
                    FilePath = filePath,
                    Success = false,
                    ErrorMessage = processResult.ErrorMessage ?? "Processing failed"
                };
            }
        }
        catch (Exception ex)
        {
            return new BatchProcessResult
            {
                FilePath = filePath,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    #endregion
    
    #region History Methods
    
    /// <summary>
    /// Clears the comparison history.
    /// </summary>
    private void ClearHistory()
    {
        var response = System.Windows.MessageBox.Show(
            "Are you sure you want to clear all comparison history?",
            "Clear History",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        
        if (response == System.Windows.MessageBoxResult.Yes)
        {
            _comparisonHistory.Clear();
            RaisePropertyChanged(nameof(HistoryEntries));
            StatusMessage = "Comparison history cleared";
        }
    }
    
    /// <summary>
    /// Adds current results to comparison history.
    /// </summary>
    private void AddToHistory(double processingTimeMs)
    {
        if (Statistics == null) return;
        
        var entry = ComparisonHistoryEntry.FromResults(
            Project, Statistics, ComparisonPoints.Count, processingTimeMs);
        
        _comparisonHistory.Add(entry);
        RaisePropertyChanged(nameof(HistoryEntries));
    }
    
    #endregion
    
    #region Point Selection Methods
    
    /// <summary>
    /// Selects a point for highlighting.
    /// </summary>
    private void SelectPoint(GnssComparisonPoint? point)
    {
        SelectedPoint = point;
    }
    
    /// <summary>
    /// Highlights the selected point in all charts.
    /// </summary>
    private void HighlightPointInCharts(GnssComparisonPoint? point)
    {
        if (point == null)
        {
            // Clear highlighting - would need to refresh charts
            return;
        }
        
        // Update delta scatter plot with highlight
        if (DeltaScatterPlotModel != null)
        {
            // Remove existing highlight annotation
            var existingHighlight = DeltaScatterPlotModel.Annotations
                .OfType<OxyPlot.Annotations.PointAnnotation>()
                .FirstOrDefault(a => a.Tag?.ToString() == "highlight");
            
            if (existingHighlight != null)
                DeltaScatterPlotModel.Annotations.Remove(existingHighlight);
            
            // Add new highlight
            DeltaScatterPlotModel.Annotations.Add(new OxyPlot.Annotations.PointAnnotation
            {
                X = point.DeltaEasting,
                Y = point.DeltaNorthing,
                Fill = OxyColor.FromRgb(255, 215, 0), // Gold
                Stroke = OxyColor.FromRgb(255, 140, 0), // Dark orange
                StrokeThickness = 2,
                Size = 10,
                Shape = MarkerType.Star,
                Tag = "highlight"
            });
            
            DeltaScatterPlotModel.InvalidatePlot(false);
        }
        
        // Similar for time series plot
        if (TimeSeriesPlotModel != null)
        {
            var existingHighlight = TimeSeriesPlotModel.Annotations
                .OfType<OxyPlot.Annotations.LineAnnotation>()
                .FirstOrDefault(a => a.Tag?.ToString() == "highlight");
            
            if (existingHighlight != null)
                TimeSeriesPlotModel.Annotations.Remove(existingHighlight);
            
            if (point.DateTime != DateTime.MinValue)
            {
                TimeSeriesPlotModel.Annotations.Add(new OxyPlot.Annotations.LineAnnotation
                {
                    Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
                    X = DateTimeAxis.ToDouble(point.DateTime),
                    Color = OxyColor.FromRgb(255, 215, 0),
                    StrokeThickness = 2,
                    Tag = "highlight"
                });
                
                TimeSeriesPlotModel.InvalidatePlot(false);
            }
        }
    }
    
    #endregion
    
    #region Keyboard Shortcut Methods
    
    /// <summary>
    /// Handles keyboard shortcuts. Call from MainWindow.
    /// </summary>
    public bool HandleKeyDown(System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers)
    {
        // F5 = Process Data
        if (key == System.Windows.Input.Key.F5 && modifiers == System.Windows.Input.ModifierKeys.None)
        {
            if (ProcessDataCommand.CanExecute(null))
            {
                ProcessDataCommand.Execute(null);
                return true;
            }
        }
        
        // Ctrl+S = Save Project
        if (key == System.Windows.Input.Key.S && modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            if (SaveProjectCommand.CanExecute(null))
            {
                SaveProjectCommand.Execute(null);
                return true;
            }
        }
        
        // Ctrl+Shift+S = Save Project As
        if (key == System.Windows.Input.Key.S && modifiers == (System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift))
        {
            if (SaveProjectAsCommand.CanExecute(null))
            {
                SaveProjectAsCommand.Execute(null);
                return true;
            }
        }
        
        // Ctrl+O = Open Project
        if (key == System.Windows.Input.Key.O && modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            if (OpenProjectCommand.CanExecute(null))
            {
                OpenProjectCommand.Execute(null);
                return true;
            }
        }
        
        // Ctrl+E = Export Excel
        if (key == System.Windows.Input.Key.E && modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            if (ExportExcelCommand.CanExecute(null))
            {
                ExportExcelCommand.Execute(null);
                return true;
            }
        }
        
        // Ctrl+P = Export PDF
        if (key == System.Windows.Input.Key.P && modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            if (ExportPdfCommand.CanExecute(null))
            {
                ExportPdfCommand.Execute(null);
                return true;
            }
        }
        
        // Ctrl+N = New Project
        if (key == System.Windows.Input.Key.N && modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            if (NewProjectCommand.CanExecute(null))
            {
                NewProjectCommand.Execute(null);
                return true;
            }
        }
        
        // F1 = Help
        if (key == System.Windows.Input.Key.F1 && modifiers == System.Windows.Input.ModifierKeys.None)
        {
            if (ShowHelpCommand.CanExecute(null))
            {
                ShowHelpCommand.Execute(null);
                return true;
            }
        }
        
        // Escape = Clear selection
        if (key == System.Windows.Input.Key.Escape && modifiers == System.Windows.Input.ModifierKeys.None)
        {
            SelectedPoint = null;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets list of available keyboard shortcuts for display.
    /// </summary>
    public static List<(string Shortcut, string Description)> GetKeyboardShortcuts()
    {
        return new List<(string, string)>
        {
            ("F5", "Process Data"),
            ("Ctrl+S", "Save Project"),
            ("Ctrl+Shift+S", "Save Project As"),
            ("Ctrl+O", "Open Project"),
            ("Ctrl+N", "New Project"),
            ("Ctrl+E", "Export to Excel"),
            ("Ctrl+P", "Export to PDF"),
            ("F1", "Show Help"),
            ("Escape", "Clear Selection")
        };
    }
    
    #endregion
}
