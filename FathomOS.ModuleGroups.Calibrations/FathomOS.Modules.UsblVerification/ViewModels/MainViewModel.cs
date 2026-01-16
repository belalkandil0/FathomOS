using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;
using FathomOS.Modules.UsblVerification.Models;
using FathomOS.Modules.UsblVerification.Parsers;
using FathomOS.Modules.UsblVerification.Services;

namespace FathomOS.Modules.UsblVerification.ViewModels;

public class MainViewModel : ViewModelBase
{
    #region Private Fields
    
    private readonly UsblCalculationService _calculationService;
    private readonly UnitConversionService _unitConversionService;
    private readonly DataValidationService _validationService;
    private readonly ProjectService _projectService;
    private readonly ChartExportService _chartExportService;
    private readonly BatchImportService _batchImportService;
    private readonly QualityDashboardService _qualityService;
    private readonly CertificateService _certificateService;
    private readonly ReportTemplateService _templateService;
    private readonly AdvancedStatisticsService _advancedStatsService;
    private readonly AdvancedChartService _advancedChartService;
    private readonly SmoothingService _smoothingService;
    private readonly HistoricalDatabaseService _historyService;
    private readonly DigitalSignatureService _signatureService;
    private readonly RecentProjectsService _recentProjectsService;
    
    private UsblVerificationProject _project;
    private VerificationResults _results;
    private PlotModel _spinPlotModel;
    private PlotModel _transitPlotModel;
    private PlotModel _livePreviewPlotModel;
    
    private int _currentStep = 1;
    private bool _isDarkTheme = true;
    private bool _isBusy;
    private bool _isHelpOpen;
    private bool _isTemplateEditorOpen;
    private string _statusMessage = "Ready - Step 1 of 7: Project Setup";
    
    // Unit selection
    private LengthUnit _selectedInputUnit = LengthUnit.Meters;
    private LengthUnit _selectedOutputUnit = LengthUnit.Meters;
    private string _unitWarning = "";
    private bool _autoDetectUnits = true;
    
    // Chart theme
    private ChartTheme _selectedChartTheme = ChartTheme.Professional;
    
    // Preview data
    private string _selectedSpinPreview = "Spin 0°";
    private string _selectedTransitPreview = "Transit Line 1";
    private ObservableCollection<UsblObservation> _previewSpinData = new();
    private ObservableCollection<UsblObservation> _previewTransitData = new();
    
    // Collections for data grids
    private ObservableCollection<UsblObservation> _spin000Observations = new();
    private ObservableCollection<UsblObservation> _spin090Observations = new();
    private ObservableCollection<UsblObservation> _spin180Observations = new();
    private ObservableCollection<UsblObservation> _spin270Observations = new();
    private ObservableCollection<UsblObservation> _transit1Observations = new();
    private ObservableCollection<UsblObservation> _transit2Observations = new();
    
    // Quality metrics
    private ObservableCollection<QualityMetrics> _qualityMetrics = new();
    private QualityMetrics _overallQuality;
    
    // Point selection
    private SelectedPointInfo _selectedPoint = new();
    
    // Report template
    private ReportTemplate _selectedTemplate;
    private ObservableCollection<ReportTemplate> _availableTemplates = new();
    
    // File collections for Step 1 UI
    private ObservableCollection<LoadedFileInfo> _spinFiles = new();
    private ObservableCollection<LoadedFileInfo> _transitFiles = new();
    private LoadedFileInfo? _selectedSpinFile;
    private LoadedFileInfo? _selectedTransitFile;
    
    // Preview data for Step 2
    private ObservableCollection<UsblObservation> _previewData = new();
    private bool _showSpinPreview = true;
    private bool _showTransitPreview;
    private int _previewRecordCount;
    
    #endregion
    
    #region Events
    
    /// <summary>
    /// Event raised when a single file column mapping dialog is needed
    /// </summary>
    public event EventHandler<ColumnMappingEventArgs>? RequestColumnMapping;
    
    /// <summary>
    /// Event raised when a batch column mapping dialog is needed
    /// </summary>
    public event EventHandler<BatchColumnMappingEventArgs>? RequestBatchColumnMapping;
    
    /// <summary>
    /// Event raised when 3D scene is updated
    /// </summary>
    public event EventHandler<Model3DGroup?>? Scene3DUpdated;
    
    #endregion
    
    #region Constructor
    
    public MainViewModel()
    {
        _project = new UsblVerificationProject();
        _results = new VerificationResults();
        _calculationService = new UsblCalculationService();
        _unitConversionService = new UnitConversionService();
        _validationService = new DataValidationService();
        _projectService = new ProjectService();
        _chartExportService = new ChartExportService();
        _batchImportService = new BatchImportService();
        _qualityService = new QualityDashboardService();
        _certificateService = new CertificateService();
        _templateService = new ReportTemplateService();
        _advancedStatsService = new AdvancedStatisticsService();
        _advancedChartService = new AdvancedChartService();
        _smoothingService = new SmoothingService();
        _historyService = new HistoricalDatabaseService();
        _signatureService = new DigitalSignatureService();
        _recentProjectsService = new RecentProjectsService();
        
        _overallQuality = new QualityMetrics { DatasetName = "Overall" };
        
        InitializeCommands();
        InitializePlotModels();
        LoadTemplates();
        LoadRecentProjects();
    }
    
    #endregion
    
    #region Properties - Project & Results
    
    public UsblVerificationProject Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }
    
    public VerificationResults Results
    {
        get => _results;
        set => SetProperty(ref _results, value);
    }
    
    // Pass-through properties for direct XAML binding
    public string ProjectName
    {
        get => Project.ProjectName;
        set { Project.ProjectName = value; OnPropertyChanged(); }
    }
    
    public DateTime? SurveyDate
    {
        get => Project.SurveyDate;
        set { Project.SurveyDate = value; OnPropertyChanged(); }
    }
    
    public string TransponderModel
    {
        get => Project.TransponderModel;
        set { Project.TransponderModel = value; OnPropertyChanged(); }
    }
    
    public string TransponderFrequency
    {
        get => Project.TransponderFrequency;
        set { Project.TransponderFrequency = value; OnPropertyChanged(); }
    }
    
    public double ToleranceMeters
    {
        get => Project.Tolerance;
        set { Project.Tolerance = value; OnPropertyChanged(); OnPropertyChanged(nameof(Threshold)); }
    }
    
    public double Threshold => Project.Tolerance;
    
    #endregion
    
    #region Properties - Step Navigation
    
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                UpdateStepStatus();
                OnPropertyChanged(nameof(CanGoPrevious));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CurrentStepDescription));
                OnPropertyChanged(nameof(NextButtonText));
                
                // CRITICAL: Notify all IsStep properties for view switching
                OnPropertyChanged(nameof(IsStep1));
                OnPropertyChanged(nameof(IsStep2));
                OnPropertyChanged(nameof(IsStep3));
                OnPropertyChanged(nameof(IsStep4));
                OnPropertyChanged(nameof(IsStep5));
                OnPropertyChanged(nameof(IsStep6));
                OnPropertyChanged(nameof(IsStep7));
                
                // Update step indicators
                UpdateStepIndicators();
                
                // Auto-generate advanced charts when entering Step 6
                if (value == 6 && HasAllSpinData)
                {
                    UpdateAdvancedCharts();
                }
            }
        }
    }
    
    public string CurrentStepDescription => CurrentStep switch
    {
        1 => "Step 1 of 7: Project Setup",
        2 => "Step 2 of 7: Load Spin Data",
        3 => "Step 3 of 7: Preview & Validate Spin Data",
        4 => "Step 4 of 7: Load Transit Data",
        5 => "Step 5 of 7: Preview & Validate Transit Data",
        6 => "Step 6 of 7: Process Data",
        7 => "Step 7 of 7: Results & Export",
        _ => "Unknown Step"
    };
    
    public string NextButtonText => CurrentStep switch
    {
        2 => HasAllSpinData ? "Next" : "Skip Transit",
        4 => HasAllTransitData ? "Next" : "Skip to Process",
        6 => "Process",
        7 => "Complete",
        _ => "Next"
    };
    
    // Progress tracking (0-100 based on current step completion)
    public double OverallProgress => CurrentStep switch
    {
        1 => HasProjectSetup ? 14 : 7,
        2 => HasAllSpinData ? 28 : 21,
        3 => SpinDataValidated ? 42 : 35,
        4 => HasAllTransitData ? 56 : 49,
        5 => TransitDataValidated ? 70 : 63,
        6 => Results?.SpinPassFail != null ? 85 : 77,
        7 => IsVerificationComplete ? 100 : 92,
        _ => 0
    };
    
    private bool _isVerificationComplete;
    public bool IsVerificationComplete
    {
        get => _isVerificationComplete;
        set => SetProperty(ref _isVerificationComplete, value);
    }
    
    private bool HasProjectSetup => !string.IsNullOrEmpty(Project.ProjectName) && 
                                    !string.IsNullOrEmpty(Project.ClientName);
    
    private bool SpinDataValidated => HasAllSpinData; // Can be expanded for validation status
    private bool TransitDataValidated => true; // Transit is optional
    
    public bool CanGoPrevious => CurrentStep > 1;
    public bool CanGoNext => CurrentStep < 7;
    public bool CanProcess => HasAllSpinData; // Minimum requirement
    
    // Step visibility properties
    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsStep5 => CurrentStep == 5;
    public bool IsStep6 => CurrentStep == 6;
    public bool IsStep7 => CurrentStep == 7;
    
    // Steps collection for header display
    public ObservableCollection<StepInfo> Steps { get; } = new()
    {
        new StepInfo { Number = "1", Title = "Setup", Subtitle = "Project & Files" },
        new StepInfo { Number = "2", Title = "Preview", Subtitle = "Verify Data" },
        new StepInfo { Number = "3", Title = "Spin", Subtitle = "Analysis" },
        new StepInfo { Number = "4", Title = "Transit", Subtitle = "Analysis" },
        new StepInfo { Number = "5", Title = "Results", Subtitle = "Summary" },
        new StepInfo { Number = "6", Title = "Quality", Subtitle = "Dashboard" },
        new StepInfo { Number = "7", Title = "Export", Subtitle = "Reports" }
    };
    
    #endregion
    
    #region Properties - Units
    
    public List<LengthUnit> LengthUnits => new()
    {
        LengthUnit.Meters,
        LengthUnit.InternationalFeet,
        LengthUnit.UsSurveyFeet
    };
    
    // Alias for XAML binding
    public List<LengthUnit> AvailableUnits => LengthUnits;
    
    // Status icon for footer
    public string StatusIcon => IsBusy ? "Loading" : (Results?.OverallPass == true ? "CheckCircle" : "Information");
    
    public LengthUnit SelectedInputUnit
    {
        get => _selectedInputUnit;
        set
        {
            if (SetProperty(ref _selectedInputUnit, value))
            {
                Project.InputUnit = value;
                UpdateUnitWarning();
                OnPropertyChanged(nameof(UnitAbbreviation));
            }
        }
    }
    
    public LengthUnit SelectedOutputUnit
    {
        get => _selectedOutputUnit;
        set
        {
            if (SetProperty(ref _selectedOutputUnit, value))
            {
                Project.OutputUnit = value;
                OnPropertyChanged(nameof(OutputUnitAbbreviation));
            }
        }
    }
    
    public string UnitAbbreviation => UnitConversionService.GetAbbreviation(SelectedInputUnit);
    public string OutputUnitAbbreviation => UnitConversionService.GetAbbreviation(SelectedOutputUnit);
    
    public string UnitWarning
    {
        get => _unitWarning;
        set => SetProperty(ref _unitWarning, value);
    }
    
    public bool HasUnitWarning => !string.IsNullOrEmpty(UnitWarning);
    
    #endregion
    
    #region Properties - Chart Theme
    
    public List<ChartTheme> ChartThemes => Enum.GetValues<ChartTheme>().ToList();
    
    public ChartTheme SelectedChartTheme
    {
        get => _selectedChartTheme;
        set
        {
            if (SetProperty(ref _selectedChartTheme, value))
            {
                Project.ChartTheme = value;
                UpdateChartTheme();
            }
        }
    }
    
    #endregion
    
    #region Properties - Data Status
    
    public bool HasSpin000Data => Project.Spin000.Observations.Any(o => !o.IsExcluded);
    public bool HasSpin090Data => Project.Spin090.Observations.Any(o => !o.IsExcluded);
    public bool HasSpin180Data => Project.Spin180.Observations.Any(o => !o.IsExcluded);
    public bool HasSpin270Data => Project.Spin270.Observations.Any(o => !o.IsExcluded);
    public bool HasTransit1Data => Project.Transit1.Observations.Any(o => !o.IsExcluded);
    public bool HasTransit2Data => Project.Transit2.Observations.Any(o => !o.IsExcluded);
    
    public bool HasAllSpinData => HasSpin000Data && HasSpin090Data && HasSpin180Data && HasSpin270Data;
    public bool HasAllTransitData => HasTransit1Data && HasTransit2Data;
    
    public int Spin000PointCount => Project.Spin000.Observations.Count(o => !o.IsExcluded);
    public int Spin090PointCount => Project.Spin090.Observations.Count(o => !o.IsExcluded);
    public int Spin180PointCount => Project.Spin180.Observations.Count(o => !o.IsExcluded);
    public int Spin270PointCount => Project.Spin270.Observations.Count(o => !o.IsExcluded);
    public int Transit1PointCount => Project.Transit1.Observations.Count(o => !o.IsExcluded);
    public int Transit2PointCount => Project.Transit2.Observations.Count(o => !o.IsExcluded);
    
    public string SpinDataStatus
    {
        get
        {
            if (!HasAllSpinData)
                return "Load NPD files for all 4 spin headings";
            
            // Show actual headings from data
            var headings = Project.AllSpinTests
                .Where(s => s.HasData)
                .Select(s => $"{s.ActualHeading:F0}°")
                .ToList();
            
            int totalPoints = Spin000PointCount + Spin090PointCount + Spin180PointCount + Spin270PointCount;
            return $"Loaded at headings: {string.Join(", ", headings)} ({totalPoints} points)";
        }
    }
    
    public string TransitDataStatus => HasAllTransitData 
        ? $"Both transit lines loaded ({Transit1PointCount + Transit2PointCount} total points)"
        : "Load NPD files for both transit lines (optional)";
    
    // File collections for Step 1 UI
    public ObservableCollection<LoadedFileInfo> SpinFiles
    {
        get => _spinFiles;
        set => SetProperty(ref _spinFiles, value);
    }
    
    public ObservableCollection<LoadedFileInfo> TransitFiles
    {
        get => _transitFiles;
        set => SetProperty(ref _transitFiles, value);
    }
    
    public LoadedFileInfo? SelectedSpinFile
    {
        get => _selectedSpinFile;
        set => SetProperty(ref _selectedSpinFile, value);
    }
    
    public LoadedFileInfo? SelectedTransitFile
    {
        get => _selectedTransitFile;
        set => SetProperty(ref _selectedTransitFile, value);
    }
    
    public bool HasNoSpinFiles => SpinFiles.Count == 0;
    public bool HasNoTransitFiles => TransitFiles.Count == 0;
    
    // Step 2 Preview
    public ObservableCollection<UsblObservation> PreviewData
    {
        get => _previewData;
        set => SetProperty(ref _previewData, value);
    }
    
    public bool ShowSpinPreview
    {
        get => _showSpinPreview;
        set
        {
            if (SetProperty(ref _showSpinPreview, value))
            {
                _showTransitPreview = !value;
                OnPropertyChanged(nameof(ShowTransitPreview));
                RefreshPreviewData();
            }
        }
    }
    
    public bool ShowTransitPreview
    {
        get => _showTransitPreview;
        set
        {
            if (SetProperty(ref _showTransitPreview, value))
            {
                _showSpinPreview = !value;
                OnPropertyChanged(nameof(ShowSpinPreview));
                RefreshPreviewData();
            }
        }
    }
    
    public int PreviewRecordCount
    {
        get => _previewRecordCount;
        set => SetProperty(ref _previewRecordCount, value);
    }
    
    // Statistics for Step 2 Data Preview
    public int SpinFileCount => Project.AllSpinTests.Count(s => s.HasData);
    public int TransitFileCount => Project.AllTransitTests.Count(t => t.HasData);
    public int TotalPointCount => Project.AllSpinTests.Sum(s => s.Observations.Count(o => !o.IsExcluded)) +
                                  Project.AllTransitTests.Sum(t => t.Observations.Count(o => !o.IsExcluded));
    public int HeadingCount => Project.AllSpinTests.Count(s => s.HasData);
    
    public int DataCoveragePercent => (SpinFileCount * 25); // Each spin file = 25% coverage
    
    public int SpinTotalPoints => Project.AllSpinTests.Sum(s => s.Observations.Count(o => !o.IsExcluded));
    
    public string HeadingSpreadStatus
    {
        get
        {
            if (SpinFileCount < 4) return "Incomplete";
            
            // Check if headings are roughly 90° apart
            var headings = Project.AllSpinTests.Where(s => s.HasData).Select(s => s.ActualHeading).OrderBy(h => h).ToList();
            if (headings.Count < 4) return "Incomplete";
            
            // Calculate angular differences
            bool goodSpread = true;
            for (int i = 0; i < headings.Count; i++)
            {
                int next = (i + 1) % headings.Count;
                double diff = headings[next] - headings[i];
                if (diff < 0) diff += 360;
                if (diff < 60 || diff > 120) goodSpread = false;
            }
            
            return goodSpread ? "Good (≈90° apart)" : "Check Spread";
        }
    }
    
    public ObservableCollection<SpinDataSummary> SpinDataSummaries { get; } = new();
    
    private void UpdateSpinDataSummaries()
    {
        SpinDataSummaries.Clear();
        
        foreach (var spin in Project.AllSpinTests)
        {
            SpinDataSummaries.Add(new SpinDataSummary
            {
                // Use ActualHeading when data is available, NominalHeading otherwise
                HeadingLabel = spin.HasData ? $"{spin.ActualHeading:F0}°" : $"{spin.NominalHeading}°",
                DisplayName = spin.HasData ? spin.DisplayName : $"Spin {spin.NominalHeading}° (No Data)",
                PointCount = spin.Observations.Count(o => !o.IsExcluded),
                HasData = spin.HasData,
                ActualHeading = spin.HasData ? spin.ActualHeading : spin.NominalHeading
            });
        }
        
        OnPropertyChanged(nameof(SpinDataSummaries));
        OnPropertyChanged(nameof(SpinFileCount));
        OnPropertyChanged(nameof(TransitFileCount));
        OnPropertyChanged(nameof(TotalPointCount));
        OnPropertyChanged(nameof(HeadingCount));
        OnPropertyChanged(nameof(DataCoveragePercent));
        OnPropertyChanged(nameof(HeadingSpreadStatus));
    }
    
    #endregion
    
    #region Properties - Preview Data
    
    public List<string> SpinPreviewOptions => new() { "Spin 0°", "Spin 90°", "Spin 180°", "Spin 270°", "All Spin Data" };
    public List<string> TransitPreviewOptions => new() { "Transit Line 1", "Transit Line 2", "All Transit Data" };
    
    public string SelectedSpinPreview
    {
        get => _selectedSpinPreview;
        set
        {
            if (SetProperty(ref _selectedSpinPreview, value))
                UpdateSpinPreview();
        }
    }
    
    public string SelectedTransitPreview
    {
        get => _selectedTransitPreview;
        set
        {
            if (SetProperty(ref _selectedTransitPreview, value))
                UpdateTransitPreview();
        }
    }
    
    public ObservableCollection<UsblObservation> PreviewSpinData
    {
        get => _previewSpinData;
        set => SetProperty(ref _previewSpinData, value);
    }
    
    public ObservableCollection<UsblObservation> PreviewTransitData
    {
        get => _previewTransitData;
        set => SetProperty(ref _previewTransitData, value);
    }
    
    // Preview statistics
    public int PreviewTotalPoints => PreviewSpinData.Count;
    public int PreviewExcludedPoints => PreviewSpinData.Count(o => o.IsExcluded);
    public double PreviewAvgEasting => PreviewSpinData.Where(o => !o.IsExcluded).Select(o => o.TransponderEasting).DefaultIfEmpty().Average();
    public double PreviewAvgNorthing => PreviewSpinData.Where(o => !o.IsExcluded).Select(o => o.TransponderNorthing).DefaultIfEmpty().Average();
    
    public int PreviewTransitTotalPoints => PreviewTransitData.Count;
    public int PreviewTransitExcludedPoints => PreviewTransitData.Count(o => o.IsExcluded);
    public double PreviewTransitAvgEasting => PreviewTransitData.Where(o => !o.IsExcluded).Select(o => o.TransponderEasting).DefaultIfEmpty().Average();
    public double PreviewTransitAvgNorthing => PreviewTransitData.Where(o => !o.IsExcluded).Select(o => o.TransponderNorthing).DefaultIfEmpty().Average();
    
    #endregion
    
    #region Properties - Tolerance Display
    
    public double SpinTolerancePercentDisplay
    {
        get => Project.SpinTolerancePercent * 100;
        set
        {
            Project.SpinTolerancePercent = value / 100;
            OnPropertyChanged();
        }
    }
    
    public double TransitTolerancePercentDisplay
    {
        get => Project.TransitTolerancePercent * 100;
        set
        {
            Project.TransitTolerancePercent = value / 100;
            OnPropertyChanged();
        }
    }
    
    #endregion
    
    #region Properties - UI State
    
    public event EventHandler<bool>? ThemeChanged;
    
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                ThemeChanged?.Invoke(this, value);
            }
        }
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public bool IsHelpOpen
    {
        get => _isHelpOpen;
        set => SetProperty(ref _isHelpOpen, value);
    }
    
    public bool IsTemplateEditorOpen
    {
        get => _isTemplateEditorOpen;
        set => SetProperty(ref _isTemplateEditorOpen, value);
    }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool AutoDetectUnits
    {
        get => _autoDetectUnits;
        set => SetProperty(ref _autoDetectUnits, value);
    }
    
    #endregion
    
    #region Properties - Quality Dashboard
    
    public ObservableCollection<QualityMetrics> QualityMetrics
    {
        get => _qualityMetrics;
        set => SetProperty(ref _qualityMetrics, value);
    }
    
    public QualityMetrics OverallQuality
    {
        get => _overallQuality;
        set => SetProperty(ref _overallQuality, value);
    }
    
    // Quality plot for dashboard
    private PlotModel _qualityPlotModel = new();
    public PlotModel QualityPlotModel
    {
        get => _qualityPlotModel;
        set => SetProperty(ref _qualityPlotModel, value);
    }
    
    #endregion
    
    #region Properties - Spin Analysis Results
    
    public double SpinMeanEasting => Results?.SpinMeanPosition?.Easting ?? 0;
    public double SpinMeanNorthing => Results?.SpinMeanPosition?.Northing ?? 0;
    public double SpinMeanDepth => Results?.SpinMeanPosition?.Depth ?? 0;
    public double SpinMeanOffset => Results?.SpinMeanOffset ?? 0;
    public double SpinStdDev => Results?.SpinStdDev ?? 0;
    public int SpinPointCount => Project.AllSpinTests.Sum(s => s.Observations.Count(o => !o.IsExcluded));
    public bool SpinTestPassed => Results?.SpinPassFail ?? false;
    
    // Spin heading results collection for UI display
    private ObservableCollection<HeadingResult> _spinHeadingResults = new();
    public ObservableCollection<HeadingResult> SpinHeadingResults
    {
        get => _spinHeadingResults;
        set => SetProperty(ref _spinHeadingResults, value);
    }
    
    #endregion
    
    #region Properties - Transit Analysis Results
    
    public double TransitMeanOffset => Results?.TransitMeanOffset ?? 0;
    public double TransitStdDev => Results?.TransitStdDev ?? 0;
    public double TransitMaxOffset => Results?.TransitMaxOffset ?? 0;
    public int TransitPointCount => Project.Transit1.Observations.Count(o => !o.IsExcluded) + 
                                    Project.Transit2.Observations.Count(o => !o.IsExcluded);
    public bool TransitTestPassed => Results?.TransitPassFail ?? true;
    
    #endregion
    
    #region Properties - Overall Results
    
    private string _processingStatus = "NotProcessed";
    public string ProcessingStatus
    {
        get => _processingStatus;
        set
        {
            if (SetProperty(ref _processingStatus, value))
            {
                OnPropertyChanged(nameof(IsProcessed));
                OnPropertyChanged(nameof(ShowNotProcessedMessage));
            }
        }
    }
    
    public bool IsProcessed => ProcessingStatus == "Pass" || ProcessingStatus == "Fail";
    public bool ShowNotProcessedMessage => ProcessingStatus == "NotProcessed";
    
    public bool OverallPassed => Results?.OverallPass ?? false;
    public double OverallQualityScore => Results?.QualityScore ?? 0;
    
    #endregion
    
    #region Properties - Point Selection
    
    public SelectedPointInfo SelectedPoint
    {
        get => _selectedPoint;
        set
        {
            if (SetProperty(ref _selectedPoint, value))
            {
                OnPropertyChanged(nameof(HasSelectedPoint));
            }
        }
    }
    
    public bool HasSelectedPoint => SelectedPoint?.HasSelection ?? false;
    
    #endregion
    
    #region Properties - Report Templates
    
    public ObservableCollection<ReportTemplate> AvailableTemplates
    {
        get => _availableTemplates;
        set => SetProperty(ref _availableTemplates, value);
    }
    
    public ReportTemplate SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetProperty(ref _selectedTemplate, value);
    }
    
    #endregion
    
    #region Properties - Charts
    
    public PlotModel SpinPlotModel
    {
        get => _spinPlotModel;
        set => SetProperty(ref _spinPlotModel, value);
    }
    
    public PlotModel TransitPlotModel
    {
        get => _transitPlotModel;
        set => SetProperty(ref _transitPlotModel, value);
    }
    
    public PlotModel LivePreviewPlotModel
    {
        get => _livePreviewPlotModel;
        set => SetProperty(ref _livePreviewPlotModel, value);
    }
    
    #endregion
    
    #region Commands
    
    // Navigation
    public ICommand NextStepCommand { get; private set; }
    public ICommand PreviousStepCommand { get; private set; }
    public ICommand GoToStepCommand { get; private set; }
    
    // Project
    public ICommand NewProjectCommand { get; private set; }
    public ICommand LoadProjectCommand { get; private set; }
    public ICommand SaveProjectCommand { get; private set; }
    
    // Data Loading
    public ICommand LoadSpin000Command { get; private set; }
    public ICommand LoadSpin090Command { get; private set; }
    public ICommand LoadSpin180Command { get; private set; }
    public ICommand LoadSpin270Command { get; private set; }
    public ICommand LoadTransit1Command { get; private set; }
    public ICommand LoadTransit2Command { get; private set; }
    public ICommand BatchImportSpinCommand { get; private set; }
    public ICommand BatchImportTransitCommand { get; private set; }
    
    // New Step 1 Commands
    public ICommand LoadSpinFilesCommand { get; private set; }
    public ICommand LoadTransitFilesCommand { get; private set; }
    public ICommand RemoveSpinFileCommand { get; private set; }
    public ICommand RemoveTransitFileCommand { get; private set; }
    public ICommand RefreshPreviewCommand { get; private set; }
    
    // Validation
    public ICommand ValidateSpinDataCommand { get; private set; }
    public ICommand ValidateTransitDataCommand { get; private set; }
    public ICommand ValidateAllDataCommand { get; private set; }
    
    // Processing
    public ICommand ProcessCommand { get; private set; }
    public ICommand ProcessSpinDataCommand { get; private set; }
    public ICommand ProcessTransitDataCommand { get; private set; }
    
    // Export
    public ICommand ExportExcelCommand { get; private set; }
    public ICommand ExportPdfCommand { get; private set; }
    public ICommand ExportDxfCommand { get; private set; }
    public ICommand ExportDxfPlanViewCommand { get; private set; }
    public ICommand ExportChartCommand { get; private set; }
    public ICommand ExportSummaryCommand { get; private set; }
    public ICommand ExportCertificateCommand { get; private set; }
    
    // Point Selection
    public ICommand SelectPointCommand { get; private set; }
    public ICommand ExcludeSelectedPointCommand { get; private set; }
    public ICommand ClearSelectionCommand { get; private set; }
    
    // Templates
    public ICommand OpenTemplateEditorCommand { get; private set; }
    public ICommand SaveTemplateCommand { get; private set; }
    public ICommand ImportLogoCommand { get; private set; }
    public ICommand ImportSignatureCommand { get; private set; }
    
    // UI
    public ICommand ToggleThemeCommand { get; private set; }
    public ICommand ShowHelpCommand { get; private set; }
    
    // Advanced Analytics
    public ICommand GenerateAllAnalyticsCommand { get; private set; }
    public ICommand RunMonteCarloCommand { get; private set; }
    public ICommand ApplySmoothingCommand { get; private set; }
    public ICommand ResetSmoothingCommand { get; private set; }
    
    // History
    public ICommand SaveToHistoryCommand { get; private set; }
    public ICommand LoadHistoryCommand { get; private set; }
    public ICommand ExportHistoryCommand { get; private set; }
    public ICommand ImportHistoryCommand { get; private set; }
    
    // Recent Projects
    public ICommand ClearRecentProjectsCommand { get; private set; }
    
    // 3D Visualization Commands
    public ICommand Generate3DViewCommand { get; private set; }
    public ICommand Reset3DViewCommand { get; private set; }
    
    // Advanced Charts Command
    public ICommand RefreshAdvancedChartsCommand { get; private set; }
    
    private void InitializeCommands()
    {
        // Navigation
        NextStepCommand = new RelayCommand(_ => NextStep(), _ => CanGoNext);
        PreviousStepCommand = new RelayCommand(_ => PreviousStep(), _ => CanGoPrevious);
        GoToStepCommand = new RelayCommand(p => GoToStep(p), _ => true);
        
        // Project
        NewProjectCommand = new RelayCommand(_ => NewProject());
        LoadProjectCommand = new RelayCommand(_ => LoadProject());
        SaveProjectCommand = new RelayCommand(_ => SaveProject());
        
        // Data Loading
        LoadSpin000Command = new RelayCommand(_ => LoadSpinData(0));
        LoadSpin090Command = new RelayCommand(_ => LoadSpinData(90));
        LoadSpin180Command = new RelayCommand(_ => LoadSpinData(180));
        LoadSpin270Command = new RelayCommand(_ => LoadSpinData(270));
        LoadTransit1Command = new RelayCommand(_ => LoadTransitData(1));
        LoadTransit2Command = new RelayCommand(_ => LoadTransitData(2));
        BatchImportSpinCommand = new RelayCommand(_ => BatchImportSpin());
        BatchImportTransitCommand = new RelayCommand(_ => BatchImportTransit());
        
        // New Step 1 Commands
        LoadSpinFilesCommand = new RelayCommand(_ => LoadSpinFilesWithMapping());
        LoadTransitFilesCommand = new RelayCommand(_ => LoadTransitFilesWithMapping());
        RemoveSpinFileCommand = new RelayCommand(p => RemoveSpinFile(p as LoadedFileInfo));
        RemoveTransitFileCommand = new RelayCommand(p => RemoveTransitFile(p as LoadedFileInfo));
        RefreshPreviewCommand = new RelayCommand(_ => RefreshPreviewData());
        
        // Validation
        ValidateSpinDataCommand = new RelayCommand(_ => ValidateSpinData(), _ => HasAllSpinData);
        ValidateTransitDataCommand = new RelayCommand(_ => ValidateTransitData(), _ => HasAllTransitData);
        ValidateAllDataCommand = new RelayCommand(_ => ValidateAllData(), _ => HasAllSpinData);
        
        // Processing
        ProcessCommand = new RelayCommand(_ => ProcessData(), _ => CanProcess && !IsBusy);
        ProcessSpinDataCommand = new RelayCommand(_ => ProcessSpinAnalysis(), _ => HasAllSpinData && !IsBusy);
        ProcessTransitDataCommand = new RelayCommand(_ => ProcessTransitAnalysis(), _ => HasAllTransitData && !IsBusy);
        
        // Export
        ExportExcelCommand = new RelayCommand(_ => ExportToExcel(), _ => Results?.SpinPassFail != null);
        ExportPdfCommand = new RelayCommand(_ => ExportToPdf(), _ => Results?.SpinPassFail != null);
        ExportDxfCommand = new RelayCommand(_ => ExportToDxf(), _ => Results?.SpinPassFail != null);
        ExportDxfPlanViewCommand = new RelayCommand(_ => ExportDxfPlanView(), _ => Results?.SpinPassFail != null);
        ExportChartCommand = new RelayCommand(_ => ExportCharts(), _ => SpinPlotModel != null);
        ExportSummaryCommand = new RelayCommand(_ => ExportSummary(), _ => Results?.SpinPassFail != null);
        ExportCertificateCommand = new RelayCommand(_ => ExportCertificate(), _ => Results?.OverallPass == true);
        
        // Point Selection
        SelectPointCommand = new RelayCommand(p => SelectPoint(p));
        ExcludeSelectedPointCommand = new RelayCommand(_ => ExcludeSelectedPoint(), _ => HasSelectedPoint);
        ClearSelectionCommand = new RelayCommand(_ => ClearSelection());
        
        // Templates
        OpenTemplateEditorCommand = new RelayCommand(_ => IsTemplateEditorOpen = !IsTemplateEditorOpen);
        SaveTemplateCommand = new RelayCommand(_ => SaveCurrentTemplate(), _ => SelectedTemplate != null && !SelectedTemplate.IsDefault);
        ImportLogoCommand = new RelayCommand(_ => ImportLogo());
        ImportSignatureCommand = new RelayCommand(_ => ImportSignature());
        
        // UI
        ToggleThemeCommand = new RelayCommand(_ => ToggleTheme());
        ShowHelpCommand = new RelayCommand(_ => IsHelpOpen = !IsHelpOpen);
        
        // Advanced Analytics
        GenerateAllAnalyticsCommand = new RelayCommand(_ => GenerateAllAnalytics(), _ => HasAllSpinData && !IsBusy);
        RunMonteCarloCommand = new RelayCommand(_ => RunMonteCarloSimulation(GetAllSpinObservations()), _ => HasAllSpinData && !IsBusy);
        ApplySmoothingCommand = new RelayCommand(_ => ApplySmoothingToCurrentData(), _ => HasAllSpinData && !IsBusy);
        ResetSmoothingCommand = new RelayCommand(_ => ResetSmoothing(), _ => HasAllSpinData);
        
        // History
        SaveToHistoryCommand = new RelayCommand(_ => SaveToHistory(), _ => Results?.SpinPassFail != null);
        LoadHistoryCommand = new RelayCommand(_ => LoadHistoryRecords());
        ExportHistoryCommand = new RelayCommand(_ => ExportHistoryToFile());
        ImportHistoryCommand = new RelayCommand(_ => ImportHistoryFromFile());
        
        // Recent Projects
        ClearRecentProjectsCommand = new RelayCommand(_ => ClearRecentProjects());
        
        // 3D Visualization Commands
        Generate3DViewCommand = new RelayCommand(_ => Generate3DView(), _ => HasAllSpinData && !IsBusy);
        Reset3DViewCommand = new RelayCommand(_ => Reset3DView());
        
        // Advanced Charts Command
        RefreshAdvancedChartsCommand = new RelayCommand(_ => RefreshAdvancedCharts(), _ => HasAllSpinData && !IsBusy);
        
        // Outlier Filtering Commands
        ExcludeOutlierCommand = new RelayCommand(p => ExcludeOutlier(p as UsblObservation), _ => OutlierCount > 0);
        ExcludeAllOutliersCommand = new RelayCommand(_ => ExcludeAllOutliers(), _ => OutlierCount > 0);
        RestoreExcludedCommand = new RelayCommand(_ => RestoreAllExcludedPoints(), _ => HasAllSpinData);
    }
    
    #endregion
    
    #region Navigation Methods
    
    private void NextStep()
    {
        if (CurrentStep == 6)
        {
            ProcessData();
            return;
        }
        
        // Step 7: Complete verification
        if (CurrentStep == 7)
        {
            CompleteVerification();
            return;
        }
        
        // Special handling for skip scenarios
        if (CurrentStep == 2 && !HasAllSpinData)
        {
            System.Windows.MessageBox.Show("Please load all 4 spin data files before proceeding.", "Incomplete Data",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (CurrentStep == 4 && !HasAllTransitData)
        {
            var result = System.Windows.MessageBox.Show("Transit data is not complete. Skip transit verification?", 
                "Skip Transit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                CurrentStep = 6; // Skip to process
            return;
        }
        
        if (CurrentStep < 7)
            CurrentStep++;
    }
    
    private void CompleteVerification()
    {
        try
        {
            // Save to history
            if (Results?.SpinPassFail != null)
            {
                SaveToHistory();
            }
            
            IsVerificationComplete = true;
            OnPropertyChanged(nameof(OverallProgress));
            
            var result = System.Windows.MessageBox.Show(
                $"Verification completed successfully!\n\n" +
                $"Overall Result: {(Results?.OverallPass == true ? "PASSED" : "FAILED")}\n\n" +
                "Would you like to:\n" +
                "• Yes - Save project and start new\n" +
                "• No - Continue with current project\n" +
                "• Cancel - Return to results",
                "Verification Complete",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Information);
            
            if (result == MessageBoxResult.Yes)
            {
                // Save current project
                SaveProject();
                
                // Ask if user wants to start new
                var newResult = System.Windows.MessageBox.Show(
                    "Start a new verification project?",
                    "New Project",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                    
                if (newResult == MessageBoxResult.Yes)
                {
                    NewProject();
                }
            }
            
            StatusMessage = "Verification complete - saved to history";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error completing verification: {ex.Message}";
        }
    }
    
    private void PreviousStep()
    {
        if (CurrentStep > 1)
            CurrentStep--;
    }
    
    private void GoToStep(object parameter)
    {
        if (parameter is string stepStr && int.TryParse(stepStr, out int step))
        {
            if (step >= 1 && step <= 7)
                CurrentStep = step;
        }
    }
    
    private void UpdateStepStatus()
    {
        StatusMessage = CurrentStepDescription;
        
        // Update data when entering different steps
        if (CurrentStep == 2)
        {
            // Load preview data when entering Step 2
            RefreshPreviewData();
            UpdateSpinDataSummaries();
        }
        else if (CurrentStep == 3)
            UpdateSpinPreview();
        else if (CurrentStep == 5)
            UpdateTransitPreview();
        else if (CurrentStep == 6)
        {
            // Initialize results display with neutral state if not processed
            if (Results == null || !HasAllSpinData)
            {
                StatusMessage = "Load and process data to view results";
            }
        }
    }
    
    /// <summary>
    /// Update step indicator states for the header display
    /// </summary>
    private void UpdateStepIndicators()
    {
        for (int i = 0; i < Steps.Count; i++)
        {
            var step = Steps[i];
            step.IsCurrent = (i + 1) == CurrentStep;
            step.IsCompleted = (i + 1) < CurrentStep;
        }
    }
    
    #endregion
    
    #region Data Loading Methods
    
    private void LoadSpinData(int heading)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Select NPD file for Spin {heading}°",
            Filter = "NPD Files (*.npd)|*.npd|All Files (*.*)|*.*",
            DefaultExt = ".npd"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = $"Loading Spin {heading}° data...";
            
            var parser = new UsblNpdParser();
            var observations = parser.Parse(dialog.FileName);
            
            // Apply unit conversion if needed
            if (SelectedInputUnit != LengthUnit.Meters)
            {
                foreach (var obs in observations)
                    _unitConversionService.ConvertObservation(obs, SelectedInputUnit, LengthUnit.Meters);
            }
            
            var spinData = heading switch
            {
                0 => Project.Spin000,
                90 => Project.Spin090,
                180 => Project.Spin180,
                270 => Project.Spin270,
                _ => throw new ArgumentException($"Invalid heading: {heading}")
            };
            
            spinData.Observations = observations;
            spinData.SourceFile = Path.GetFileName(dialog.FileName);
            
            // Update UI
            RefreshSpinCollections();
            UpdateDataStatusProperties();
            UpdateUnitWarning();
            
            StatusMessage = $"Loaded {observations.Count} points for Spin {heading}°";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading file: {ex.Message}", "Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error loading file";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void LoadTransitData(int lineNumber)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Select NPD file for Transit Line {lineNumber}",
            Filter = "NPD Files (*.npd)|*.npd|All Files (*.*)|*.*",
            DefaultExt = ".npd"
        };
        
        if (dialog.ShowDialog() != true) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = $"Loading Transit Line {lineNumber} data...";
            
            var parser = new UsblNpdParser();
            var observations = parser.Parse(dialog.FileName);
            
            // Apply unit conversion if needed
            if (SelectedInputUnit != LengthUnit.Meters)
            {
                foreach (var obs in observations)
                    _unitConversionService.ConvertObservation(obs, SelectedInputUnit, LengthUnit.Meters);
            }
            
            var transitData = lineNumber == 1 ? Project.Transit1 : Project.Transit2;
            transitData.Observations = observations;
            transitData.SourceFile = Path.GetFileName(dialog.FileName);
            
            // Update UI
            RefreshTransitCollections();
            UpdateDataStatusProperties();
            
            StatusMessage = $"Loaded {observations.Count} points for Transit Line {lineNumber}";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading file: {ex.Message}", "Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error loading file";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    #endregion
    
    #region New File Loading Methods (with Column Mapping)
    
    /// <summary>
    /// Load spin files with column mapping dialog
    /// </summary>
    private void LoadSpinFilesWithMapping()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Spin Test NPD Files",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Multiselect = true
        };
        
        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0) return;
        
        var fileList = dialog.FileNames.ToList();
        
        // Request column mapping for multiple files
        var args = new BatchColumnMappingEventArgs { FilePaths = fileList };
        RequestBatchColumnMapping?.Invoke(this, args);
        
        if (!args.Confirmed || args.ResultMappings == null) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Loading spin files...";
            
            var parser = new UsblNpdParser();
            int totalLoaded = 0;
            
            foreach (var filePath in args.ResultMappings.Keys)
            {
                var mapping = args.ResultMappings[filePath];
                var observations = parser.Parse(filePath, mapping);
                
                if (observations.Count == 0) continue;
                
                // Apply unit conversion if needed
                if (SelectedInputUnit != LengthUnit.Meters)
                {
                    foreach (var obs in observations)
                        _unitConversionService.ConvertObservation(obs, SelectedInputUnit, LengthUnit.Meters);
                }
                
                // Try to auto-detect heading from filename
                int heading = DetectHeadingFromFilename(Path.GetFileName(filePath));
                
                // Calculate actual average heading from observations
                double actualHeading = CalculateAverageHeading(observations);
                
                // Create file info
                var fileInfo = new LoadedFileInfo
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    RecordCount = observations.Count,
                    HeadingLabel = $"{actualHeading:F0}°",  // Use actual heading
                    Mapping = mapping,
                    ActualHeading = actualHeading
                };
                
                SpinFiles.Add(fileInfo);
                
                // Assign to appropriate spin dataset
                if (heading >= 0)
                {
                    var spinData = heading switch
                    {
                        0 => Project.Spin000,
                        90 => Project.Spin090,
                        180 => Project.Spin180,
                        270 => Project.Spin270,
                        _ => null
                    };
                    
                    if (spinData != null)
                    {
                        spinData.Observations = observations;
                        spinData.SourceFile = Path.GetFileName(filePath);
                        foreach (var obs in observations)
                            obs.SourceFile = Path.GetFileName(filePath);
                    }
                }
                else
                {
                    // Auto-assign based on order or average heading
                    AutoAssignSpinData(observations, Path.GetFileName(filePath));
                }
                
                totalLoaded += observations.Count;
            }
            
            RefreshSpinCollections();
            UpdateDataStatusProperties();
            OnPropertyChanged(nameof(HasNoSpinFiles));
            
            StatusMessage = $"Loaded {SpinFiles.Count} files ({totalLoaded} total points)";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading files: {ex.Message}", "Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Load transit files with column mapping dialog
    /// </summary>
    private void LoadTransitFilesWithMapping()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Transit Test NPD Files",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Multiselect = true
        };
        
        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0) return;
        
        var fileList = dialog.FileNames.ToList();
        
        // Request column mapping for multiple files
        var args = new BatchColumnMappingEventArgs { FilePaths = fileList };
        RequestBatchColumnMapping?.Invoke(this, args);
        
        if (!args.Confirmed || args.ResultMappings == null) return;
        
        try
        {
            IsBusy = true;
            StatusMessage = "Loading transit files...";
            
            var parser = new UsblNpdParser();
            int totalLoaded = 0;
            int lineNumber = 1;
            
            foreach (var filePath in args.ResultMappings.Keys)
            {
                var mapping = args.ResultMappings[filePath];
                var observations = parser.Parse(filePath, mapping);
                
                if (observations.Count == 0) continue;
                
                // Apply unit conversion if needed
                if (SelectedInputUnit != LengthUnit.Meters)
                {
                    foreach (var obs in observations)
                        _unitConversionService.ConvertObservation(obs, SelectedInputUnit, LengthUnit.Meters);
                }
                
                // Create file info
                var fileInfo = new LoadedFileInfo
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    RecordCount = observations.Count,
                    HeadingLabel = $"Line {lineNumber}",
                    Mapping = mapping
                };
                
                TransitFiles.Add(fileInfo);
                
                // Assign to transit dataset
                var transitData = lineNumber == 1 ? Project.Transit1 : Project.Transit2;
                transitData.Observations = observations;
                transitData.SourceFile = Path.GetFileName(filePath);
                foreach (var obs in observations)
                    obs.SourceFile = Path.GetFileName(filePath);
                
                totalLoaded += observations.Count;
                lineNumber++;
            }
            
            RefreshTransitCollections();
            UpdateDataStatusProperties();
            OnPropertyChanged(nameof(HasNoTransitFiles));
            
            StatusMessage = $"Loaded {TransitFiles.Count} files ({totalLoaded} total points)";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error loading files: {ex.Message}", "Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Remove a spin file from the list
    /// </summary>
    private void RemoveSpinFile(LoadedFileInfo? file)
    {
        if (file == null) return;
        
        SpinFiles.Remove(file);
        
        // Clear corresponding project data if it was from this file
        ClearSpinDataFromFile(file.FileName);
        
        RefreshSpinCollections();
        UpdateDataStatusProperties();
        OnPropertyChanged(nameof(HasNoSpinFiles));
    }
    
    /// <summary>
    /// Remove a transit file from the list
    /// </summary>
    private void RemoveTransitFile(LoadedFileInfo? file)
    {
        if (file == null) return;
        
        TransitFiles.Remove(file);
        
        // Clear corresponding project data
        if (Project.Transit1.SourceFile == file.FileName)
        {
            Project.Transit1.Observations.Clear();
            Project.Transit1.SourceFile = "";
        }
        else if (Project.Transit2.SourceFile == file.FileName)
        {
            Project.Transit2.Observations.Clear();
            Project.Transit2.SourceFile = "";
        }
        
        RefreshTransitCollections();
        UpdateDataStatusProperties();
        OnPropertyChanged(nameof(HasNoTransitFiles));
    }
    
    /// <summary>
    /// Refresh the preview data grid based on current selection
    /// </summary>
    private void RefreshPreviewData()
    {
        PreviewData.Clear();
        
        var allObservations = ShowSpinPreview
            ? Project.AllSpinTests.SelectMany(s => s.Observations).ToList()
            : new List<UsblObservation>(Project.Transit1.Observations.Concat(Project.Transit2.Observations));
        
        foreach (var obs in allObservations)
            PreviewData.Add(obs);
        
        PreviewRecordCount = PreviewData.Count;
        
        // Notify UI that collections have changed
        OnPropertyChanged(nameof(PreviewData));
        OnPropertyChanged(nameof(PreviewRecordCount));
        OnPropertyChanged(nameof(PreviewTotalPoints));
        OnPropertyChanged(nameof(PreviewAvgEasting));
        OnPropertyChanged(nameof(PreviewAvgNorthing));
        
        // Also update the spin/transit preview collections for statistics
        UpdateSpinPreview();
        UpdateTransitPreview();
    }
    
    /// <summary>
    /// Detect heading from filename patterns like "spin_000.npd", "heading_090.npd", etc.
    /// </summary>
    private int DetectHeadingFromFilename(string fileName)
    {
        var patterns = new[]
        {
            (@"(?:spin|hdg|heading)[_\s-]*0+(?:\D|$)", 0),
            (@"(?:spin|hdg|heading)[_\s-]*90(?:\D|$)", 90),
            (@"(?:spin|hdg|heading)[_\s-]*180(?:\D|$)", 180),
            (@"(?:spin|hdg|heading)[_\s-]*270(?:\D|$)", 270),
            (@"_0+[_\.]", 0),
            (@"_90[_\.]", 90),
            (@"_180[_\.]", 180),
            (@"_270[_\.]", 270),
        };
        
        foreach (var (pattern, heading) in patterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(fileName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return heading;
        }
        
        return -1;
    }
    
    /// <summary>
    /// Calculate average heading from observations using circular mean
    /// </summary>
    private double CalculateAverageHeading(List<UsblObservation> observations)
    {
        if (observations == null || observations.Count == 0) return 0;
        
        // Use circular mean for heading (handles 359° to 1° wraparound)
        double sinSum = 0, cosSum = 0;
        foreach (var obs in observations)
        {
            double rad = obs.VesselGyro * Math.PI / 180.0;
            sinSum += Math.Sin(rad);
            cosSum += Math.Cos(rad);
        }
        
        double avgRad = Math.Atan2(sinSum, cosSum);
        double avgDeg = avgRad * 180.0 / Math.PI;
        if (avgDeg < 0) avgDeg += 360;
        
        return Math.Round(avgDeg, 1);
    }
    
    /// <summary>
    /// Auto-assign spin data when heading cannot be detected
    /// </summary>
    private void AutoAssignSpinData(List<UsblObservation> observations, string fileName)
    {
        // Find first empty spin slot
        if (Project.Spin000.Observations.Count == 0)
        {
            Project.Spin000.Observations = observations;
            Project.Spin000.SourceFile = fileName;
        }
        else if (Project.Spin090.Observations.Count == 0)
        {
            Project.Spin090.Observations = observations;
            Project.Spin090.SourceFile = fileName;
        }
        else if (Project.Spin180.Observations.Count == 0)
        {
            Project.Spin180.Observations = observations;
            Project.Spin180.SourceFile = fileName;
        }
        else if (Project.Spin270.Observations.Count == 0)
        {
            Project.Spin270.Observations = observations;
            Project.Spin270.SourceFile = fileName;
        }
        
        foreach (var obs in observations)
            obs.SourceFile = fileName;
    }
    
    /// <summary>
    /// Clear spin data that came from a specific file
    /// </summary>
    private void ClearSpinDataFromFile(string fileName)
    {
        if (Project.Spin000.SourceFile == fileName)
        {
            Project.Spin000.Observations.Clear();
            Project.Spin000.SourceFile = "";
        }
        if (Project.Spin090.SourceFile == fileName)
        {
            Project.Spin090.Observations.Clear();
            Project.Spin090.SourceFile = "";
        }
        if (Project.Spin180.SourceFile == fileName)
        {
            Project.Spin180.Observations.Clear();
            Project.Spin180.SourceFile = "";
        }
        if (Project.Spin270.SourceFile == fileName)
        {
            Project.Spin270.Observations.Clear();
            Project.Spin270.SourceFile = "";
        }
    }
    
    #endregion
    
    #region Preview Methods
    
    private void UpdateSpinPreview()
    {
        PreviewSpinData.Clear();
        
        var observations = SelectedSpinPreview switch
        {
            "Spin 0°" => Project.Spin000.Observations,
            "Spin 90°" => Project.Spin090.Observations,
            "Spin 180°" => Project.Spin180.Observations,
            "Spin 270°" => Project.Spin270.Observations,
            "All Spin Data" => Project.AllSpinTests.SelectMany(s => s.Observations).ToList(),
            _ => new List<UsblObservation>()
        };
        
        foreach (var obs in observations)
            PreviewSpinData.Add(obs);
        
        OnPropertyChanged(nameof(PreviewTotalPoints));
        OnPropertyChanged(nameof(PreviewExcludedPoints));
        OnPropertyChanged(nameof(PreviewAvgEasting));
        OnPropertyChanged(nameof(PreviewAvgNorthing));
    }
    
    private void UpdateTransitPreview()
    {
        PreviewTransitData.Clear();
        
        var observations = SelectedTransitPreview switch
        {
            "Transit Line 1" => Project.Transit1.Observations,
            "Transit Line 2" => Project.Transit2.Observations,
            "All Transit Data" => Project.AllTransitTests.SelectMany(t => t.Observations).ToList(),
            _ => new List<UsblObservation>()
        };
        
        foreach (var obs in observations)
            PreviewTransitData.Add(obs);
        
        OnPropertyChanged(nameof(PreviewTransitTotalPoints));
        OnPropertyChanged(nameof(PreviewTransitExcludedPoints));
        OnPropertyChanged(nameof(PreviewTransitAvgEasting));
        OnPropertyChanged(nameof(PreviewTransitAvgNorthing));
    }
    
    #endregion
    
    #region Validation Methods
    
    private void ValidateSpinData()
    {
        var warnings = new List<string>();
        
        foreach (var spin in Project.AllSpinTests)
        {
            if (!spin.HasData) continue;
            
            var result = _validationService.ValidateObservations(spin.Observations);
            if (result.Warnings.Any())
            {
                warnings.Add($"--- {spin.Name} ---");
                warnings.AddRange(result.Warnings);
            }
        }
        
        if (warnings.Any())
        {
            var message = string.Join("\n", warnings);
            var result = System.Windows.MessageBox.Show(
                $"Validation found the following issues:\n\n{message}\n\nWould you like to auto-exclude detected outliers?",
                "Validation Results", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
                AutoExcludeOutliers();
        }
        else
        {
            System.Windows.MessageBox.Show("All spin data passed validation.", "Validation Passed",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        UpdateDataStatusProperties();
    }
    
    private void ValidateTransitData()
    {
        var warnings = new List<string>();
        
        foreach (var transit in Project.AllTransitTests)
        {
            if (!transit.HasData) continue;
            
            var result = _validationService.ValidateObservations(transit.Observations);
            if (result.Warnings.Any())
            {
                warnings.Add($"--- {transit.Name} ---");
                warnings.AddRange(result.Warnings);
            }
        }
        
        if (warnings.Any())
        {
            var message = string.Join("\n", warnings);
            System.Windows.MessageBox.Show($"Validation found issues:\n\n{message}", "Validation Results",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            System.Windows.MessageBox.Show("All transit data passed validation.", "Validation Passed",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    private void ValidateAllData()
    {
        ValidateSpinData();
        if (HasAllTransitData)
            ValidateTransitData();
    }
    
    private void AutoExcludeOutliers()
    {
        foreach (var spin in Project.AllSpinTests)
        {
            if (spin.HasData)
                _validationService.AutoExcludeOutliers(spin.Observations);
        }
        
        foreach (var transit in Project.AllTransitTests)
        {
            if (transit.HasData)
                _validationService.AutoExcludeOutliers(transit.Observations);
        }
        
        RefreshAllCollections();
        UpdateDataStatusProperties();
        StatusMessage = "Outliers automatically excluded";
    }
    
    #endregion
    
    #region Processing Methods
    
    private void ProcessData()
    {
        if (!CanProcess)
        {
            System.Windows.MessageBox.Show("Please load all spin data files before processing.", "Incomplete Data",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            IsBusy = true;
            StatusMessage = "Processing verification data...";
            
            // Calculate results
            Results = _calculationService.CalculateResults(Project);
            
            // Update charts with legends
            UpdateCharts();
            
            // Update result properties
            UpdateResultProperties();
            
            // Move to results step
            CurrentStep = 7;
            
            StatusMessage = Results.OverallPass 
                ? "Processing complete - VERIFICATION PASSED" 
                : "Processing complete - VERIFICATION FAILED";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error processing data: {ex.Message}", "Processing Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Processing error";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Process spin test analysis only (Step 3)
    /// </summary>
    private void ProcessSpinAnalysis()
    {
        if (!HasAllSpinData)
        {
            System.Windows.MessageBox.Show("Please load all 4 spin data files before processing.", "Incomplete Data",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            IsBusy = true;
            StatusMessage = "Processing spin test data...";
            
            // Calculate spin results
            Results = _calculationService.CalculateResults(Project);
            
            // Update spin heading results for UI
            UpdateSpinHeadingResults();
            
            // Update spin chart
            UpdateSpinChart();
            
            // Update properties
            UpdateResultProperties();
            
            // Update processing status
            ProcessingStatus = Results.SpinPassFail ? "Pass" : "Fail";
            
            StatusMessage = Results.SpinPassFail 
                ? "Spin test PASSED" 
                : "Spin test FAILED";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error processing spin data: {ex.Message}", "Processing Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Processing error";
            ProcessingStatus = "NotProcessed";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Process transit test analysis only (Step 4)
    /// </summary>
    private void ProcessTransitAnalysis()
    {
        if (!HasAllTransitData)
        {
            System.Windows.MessageBox.Show("Please load transit data files before processing.", "Incomplete Data",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            IsBusy = true;
            StatusMessage = "Processing transit test data...";
            
            // Calculate transit results (recalculate full results to include transit)
            Results = _calculationService.CalculateResults(Project);
            
            // Update transit chart
            UpdateTransitChart();
            
            // Update properties
            UpdateResultProperties();
            
            StatusMessage = Results.TransitPassFail 
                ? "Transit test PASSED" 
                : "Transit test FAILED";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error processing transit data: {ex.Message}", "Processing Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Processing error";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Update spin heading results collection for UI display
    /// </summary>
    private void UpdateSpinHeadingResults()
    {
        SpinHeadingResults.Clear();
        
        if (Results == null) return;
        
        var spinTests = new[] { Project.Spin000, Project.Spin090, Project.Spin180, Project.Spin270 };
        
        foreach (var spinData in spinTests)
        {
            if (!spinData.HasData) continue;
            
            var observations = spinData.Observations.Where(o => !o.IsExcluded).ToList();
            if (observations.Count == 0) continue;
            
            // Calculate statistics for this heading
            var meanE = observations.Average(o => o.TransponderEasting);
            var meanN = observations.Average(o => o.TransponderNorthing);
            
            var offsets = observations.Select(o =>
            {
                var de = o.TransponderEasting - Results.SpinMeanPosition.Easting;
                var dn = o.TransponderNorthing - Results.SpinMeanPosition.Northing;
                return Math.Sqrt(de * de + dn * dn);
            }).ToList();
            
            var meanOffset = offsets.Average();
            var stdDev = Math.Sqrt(offsets.Average(o => (o - meanOffset) * (o - meanOffset)));
            
            // Use ActualHeading from data, not nominal heading
            SpinHeadingResults.Add(new HeadingResult
            {
                Heading = (int)spinData.NominalHeading,
                ActualHeading = spinData.ActualHeading,
                HeadingLabel = $"{spinData.ActualHeading:F1}°",  // Show actual heading
                MeanOffset = meanOffset,
                StdDev = stdDev,
                PointCount = observations.Count,
                Passed = meanOffset <= Project.Tolerance
            });
        }
    }
    
    /// <summary>
    /// Update all result-related properties for UI binding
    /// </summary>
    private void UpdateResultProperties()
    {
        OnPropertyChanged(nameof(SpinMeanEasting));
        OnPropertyChanged(nameof(SpinMeanNorthing));
        OnPropertyChanged(nameof(SpinMeanDepth));
        OnPropertyChanged(nameof(SpinMeanOffset));
        OnPropertyChanged(nameof(SpinStdDev));
        OnPropertyChanged(nameof(SpinPointCount));
        OnPropertyChanged(nameof(SpinTestPassed));
        OnPropertyChanged(nameof(TransitMeanOffset));
        OnPropertyChanged(nameof(TransitStdDev));
        OnPropertyChanged(nameof(TransitMaxOffset));
        OnPropertyChanged(nameof(TransitPointCount));
        OnPropertyChanged(nameof(TransitTestPassed));
        OnPropertyChanged(nameof(OverallPassed));
        OnPropertyChanged(nameof(OverallQualityScore));
        OnPropertyChanged(nameof(StatusIcon));
        
        // Update quality metrics for Step 6 dashboard
        UpdateQualityMetrics();
    }
    
    #endregion
    
    #region Chart Methods
    
    private void InitializePlotModels()
    {
        SpinPlotModel = CreateEmptyPlotModel("Spin Test Results");
        TransitPlotModel = CreateEmptyPlotModel("Transit Test Results");
        LivePreviewPlotModel = CreateEmptyPlotModel("Live Data Preview");
    }
    
    private PlotModel CreateEmptyPlotModel(string title)
    {
        var model = new PlotModel
        {
            Title = title,
            TitleFontSize = 14,
            TitleFontWeight = OxyPlot.FontWeights.Bold,
            PlotAreaBorderThickness = new OxyThickness(1),
            PlotAreaBorderColor = OxyColors.Gray
        };
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Easting Offset (m)",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromRgb(40, 40, 40)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Northing Offset (m)",
            TitleFontSize = 11,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60),
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColor.FromRgb(40, 40, 40)
        });
        
        // Add legend
        model.Legends.Add(new Legend
        {
            LegendPosition = LegendPosition.RightTop,
            LegendPlacement = LegendPlacement.Outside,
            LegendOrientation = LegendOrientation.Vertical,
            LegendFontSize = 10,
            LegendBorder = OxyColors.Gray,
            LegendBorderThickness = 1
        });
        
        return model;
    }
    
    private void UpdateCharts()
    {
        UpdateSpinChart();
        UpdateTransitChart();
    }
    
    private void UpdateSpinChart()
    {
        var colors = GetChartColors(SelectedChartTheme);
        var model = CreateEmptyPlotModel("Spin Test - Transponder Position Scatter");
        
        // Add tolerance circle
        var tolerance = Results.SpinMaxAllowableDiff;
        var toleranceCircle = new LineSeries
        {
            Title = $"Tolerance ({tolerance:F2}m)",
            Color = OxyColors.Red,
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash
        };
        
        for (int i = 0; i <= 360; i++)
        {
            double angle = i * Math.PI / 180;
            toleranceCircle.Points.Add(new DataPoint(tolerance * Math.Cos(angle), tolerance * Math.Sin(angle)));
        }
        model.Series.Add(toleranceCircle);
        
        // Add center point
        var centerSeries = new ScatterSeries
        {
            Title = "Average Position",
            MarkerType = MarkerType.Cross,
            MarkerSize = 10,
            MarkerFill = OxyColors.Black,
            MarkerStroke = OxyColors.White,
            MarkerStrokeThickness = 2
        };
        centerSeries.Points.Add(new ScatterPoint(0, 0));
        model.Series.Add(centerSeries);
        
        // Add spin heading averages - use ACTUAL headings from data
        var spinTests = new[]
        {
            (Project.Spin000, colors[0]),
            (Project.Spin090, colors[1]),
            (Project.Spin180, colors[2]),
            (Project.Spin270, colors[3])
        };
        
        int colorIndex = 0;
        foreach (var (spin, color) in spinTests)
        {
            if (!spin.HasData) continue;
            
            // Use actual heading from data
            string actualLabel = $"{spin.ActualHeading:F1}°";
            
            var avgE = spin.Statistics.AverageTransponderEasting;
            var avgN = spin.Statistics.AverageTransponderNorthing;
            var overallAvgE = Results.SpinOverallAverageEasting;
            var overallAvgN = Results.SpinOverallAverageNorthing;
            
            var series = new ScatterSeries
            {
                Title = $"Spin {actualLabel}",
                MarkerType = MarkerType.Circle,
                MarkerSize = 8,
                MarkerFill = color,
                MarkerStroke = OxyColors.White,
                MarkerStrokeThickness = 1
            };
            
            // Add offset from overall average
            series.Points.Add(new ScatterPoint(avgE - overallAvgE, avgN - overallAvgN));
            model.Series.Add(series);
            
            // Add individual points (smaller) - use BestEasting/BestNorthing for smoothed values
            var pointSeries = new ScatterSeries
            {
                Title = $"Spin {actualLabel} Points",
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = OxyColor.FromAColor(150, color),
                MarkerStroke = OxyColors.Transparent
            };
            
            foreach (var obs in spin.Observations.Where(o => !o.IsExcluded))
            {
                // Use BestEasting/BestNorthing which returns smoothed if available
                double x = IsSmoothingEnabled ? obs.BestEasting : obs.TransponderEasting;
                double y = IsSmoothingEnabled ? obs.BestNorthing : obs.TransponderNorthing;
                
                // Skip outliers for main series - they'll be added separately
                if (ShowOutliersHighlighted && obs.IsOutlier) continue;
                
                pointSeries.Points.Add(new ScatterPoint(x - overallAvgE, y - overallAvgN));
            }
            model.Series.Add(pointSeries);
            
            // Add outliers as separate series if highlighting is enabled
            if (ShowOutliersHighlighted && IsOutlierFilterEnabled)
            {
                var outlierSeries = new ScatterSeries
                {
                    Title = $"Outliers {actualLabel}",
                    MarkerType = MarkerType.Triangle,
                    MarkerSize = 6,
                    MarkerFill = OxyColor.FromRgb(245, 158, 11), // Warning orange
                    MarkerStroke = OxyColors.White,
                    MarkerStrokeThickness = 1
                };
                
                foreach (var obs in spin.Observations.Where(o => !o.IsExcluded && o.IsOutlier))
                {
                    double x = IsSmoothingEnabled ? obs.BestEasting : obs.TransponderEasting;
                    double y = IsSmoothingEnabled ? obs.BestNorthing : obs.TransponderNorthing;
                    outlierSeries.Points.Add(new ScatterPoint(x - overallAvgE, y - overallAvgN));
                }
                
                if (outlierSeries.Points.Count > 0)
                    model.Series.Add(outlierSeries);
            }
            
            colorIndex++;
        }
        
        // Update model
        SpinPlotModel = model;
        SpinPlotModel.InvalidatePlot(true);
    }
    
    private void UpdateTransitChart()
    {
        if (!HasAllTransitData)
        {
            TransitPlotModel = CreateEmptyPlotModel("Transit Test - No Data");
            return;
        }
        
        var colors = GetChartColors(SelectedChartTheme);
        var model = CreateEmptyPlotModel("Transit Test - Transponder Position Scatter");
        
        // Reference is spin average
        var refE = Results.SpinOverallAverageEasting;
        var refN = Results.SpinOverallAverageNorthing;
        
        // Add tolerance circle
        var tolerance = Results.TransitMaxAllowableSpread;
        var toleranceCircle = new LineSeries
        {
            Title = $"Tolerance ({tolerance:F2}m)",
            Color = OxyColors.Red,
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash
        };
        
        for (int i = 0; i <= 360; i++)
        {
            double angle = i * Math.PI / 180;
            toleranceCircle.Points.Add(new DataPoint(tolerance * Math.Cos(angle), tolerance * Math.Sin(angle)));
        }
        model.Series.Add(toleranceCircle);
        
        // Add center (spin average reference)
        var centerSeries = new ScatterSeries
        {
            Title = "Spin Reference",
            MarkerType = MarkerType.Cross,
            MarkerSize = 10,
            MarkerFill = OxyColors.Black,
            MarkerStroke = OxyColors.White,
            MarkerStrokeThickness = 2
        };
        centerSeries.Points.Add(new ScatterPoint(0, 0));
        model.Series.Add(centerSeries);
        
        // Add transit lines
        var transitData = new[]
        {
            (Project.Transit1, "Line 1", colors[4]),
            (Project.Transit2, "Line 2", colors[5])
        };
        
        foreach (var (transit, label, color) in transitData)
        {
            if (!transit.HasData) continue;
            
            var avgE = transit.Statistics.AverageTransponderEasting;
            var avgN = transit.Statistics.AverageTransponderNorthing;
            
            // Average marker
            var avgSeries = new ScatterSeries
            {
                Title = $"Transit {label} Avg",
                MarkerType = MarkerType.Diamond,
                MarkerSize = 10,
                MarkerFill = color,
                MarkerStroke = OxyColors.White,
                MarkerStrokeThickness = 2
            };
            avgSeries.Points.Add(new ScatterPoint(avgE - refE, avgN - refN));
            model.Series.Add(avgSeries);
            
            // Individual points
            var pointSeries = new ScatterSeries
            {
                Title = $"Transit {label} Points",
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = OxyColor.FromAColor(150, color),
                MarkerStroke = OxyColors.Transparent
            };
            
            foreach (var obs in transit.Observations.Where(o => !o.IsExcluded))
            {
                pointSeries.Points.Add(new ScatterPoint(
                    obs.TransponderEasting - refE,
                    obs.TransponderNorthing - refN));
            }
            model.Series.Add(pointSeries);
        }
        
        TransitPlotModel = model;
        TransitPlotModel.InvalidatePlot(true);
    }
    
    private OxyColor[] GetChartColors(ChartTheme theme)
    {
        return theme switch
        {
            ChartTheme.Professional => new[]
            {
                OxyColor.FromRgb(41, 128, 185),   // Blue
                OxyColor.FromRgb(39, 174, 96),    // Green
                OxyColor.FromRgb(243, 156, 18),   // Orange
                OxyColor.FromRgb(142, 68, 173),   // Purple
                OxyColor.FromRgb(231, 76, 60),    // Red
                OxyColor.FromRgb(52, 73, 94)      // Dark Gray
            },
            ChartTheme.Vibrant => new[]
            {
                OxyColor.FromRgb(255, 99, 132),   // Pink
                OxyColor.FromRgb(54, 162, 235),   // Blue
                OxyColor.FromRgb(255, 206, 86),   // Yellow
                OxyColor.FromRgb(75, 192, 192),   // Teal
                OxyColor.FromRgb(153, 102, 255),  // Purple
                OxyColor.FromRgb(255, 159, 64)    // Orange
            },
            ChartTheme.Ocean => new[]
            {
                OxyColor.FromRgb(0, 119, 182),    // Deep Blue
                OxyColor.FromRgb(0, 180, 216),    // Cyan
                OxyColor.FromRgb(144, 224, 239),  // Light Blue
                OxyColor.FromRgb(72, 202, 228),   // Turquoise
                OxyColor.FromRgb(0, 150, 136),    // Teal
                OxyColor.FromRgb(38, 70, 83)      // Dark Teal
            },
            ChartTheme.Earth => new[]
            {
                OxyColor.FromRgb(139, 90, 43),    // Brown
                OxyColor.FromRgb(85, 107, 47),    // Olive
                OxyColor.FromRgb(188, 143, 143),  // Rosy Brown
                OxyColor.FromRgb(107, 142, 35),   // Yellow Green
                OxyColor.FromRgb(160, 82, 45),    // Sienna
                OxyColor.FromRgb(34, 139, 34)     // Forest Green
            },
            ChartTheme.Monochrome => new[]
            {
                OxyColor.FromRgb(50, 50, 50),
                OxyColor.FromRgb(100, 100, 100),
                OxyColor.FromRgb(150, 150, 150),
                OxyColor.FromRgb(180, 180, 180),
                OxyColor.FromRgb(70, 70, 70),
                OxyColor.FromRgb(130, 130, 130)
            },
            ChartTheme.HighContrast => new[]
            {
                OxyColor.FromRgb(255, 0, 0),      // Red
                OxyColor.FromRgb(0, 255, 0),      // Green
                OxyColor.FromRgb(0, 0, 255),      // Blue
                OxyColor.FromRgb(255, 255, 0),    // Yellow
                OxyColor.FromRgb(255, 0, 255),    // Magenta
                OxyColor.FromRgb(0, 255, 255)     // Cyan
            },
            _ => GetChartColors(ChartTheme.Professional)
        };
    }
    
    private void UpdateChartTheme()
    {
        if (Results != null && HasAllSpinData)
        {
            UpdateCharts();
        }
    }
    
    #endregion
    
    #region Export Methods
    
    private void ExportToExcel()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"USBL_Verification_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Exporting to Excel...";
                
                var exporter = new ExcelExportService();
                exporter.Export(dialog.FileName, Project, Results);
                
                StatusMessage = "Excel export complete";
                System.Windows.MessageBox.Show($"Report exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export error: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"USBL_Verification_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.pdf"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Exporting to PDF...";
                
                var exporter = new PdfExportService();
                exporter.Export(dialog.FileName, Project, Results);
                
                StatusMessage = "PDF export complete";
                System.Windows.MessageBox.Show($"Report exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export error: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private void ExportToDxf()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export to DXF",
            Filter = "DXF Files (*.dxf)|*.dxf",
            FileName = $"USBL_Verification_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.dxf"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Exporting to DXF...";
                
                var exporter = new DxfExportService();
                exporter.Export(dialog.FileName, Project, Results);
                
                StatusMessage = "DXF export complete";
                System.Windows.MessageBox.Show($"DXF exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export error: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private void ExportDxfPlanView()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export DXF Plan View",
            Filter = "DXF Files (*.dxf)|*.dxf",
            FileName = $"USBL_PlanView_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.dxf"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Exporting DXF Plan View...";
                
                var exporter = new DxfExportService();
                var options = new DxfPlanViewOptions
                {
                    ShowMultipleToleranceRings = true,
                    ShowStatisticalCircles = true,
                    ShowConfidenceEllipse = true,
                    UseSymbolsByHeading = true,
                    ShowPerHeadingMeans = true,
                    ShowExcludedPoints = true,
                    ShowTransitLines = true,
                    ShowVesselTrack = false,
                    ShowGrid = true,
                    GridSpacing = Math.Max(1, Project.Tolerance / 2),
                    ShowCoordinateLabels = true,
                    ShowLegend = true,
                    ShowScaleBar = true,
                    ShowNorthArrow = true
                };
                
                exporter.ExportPlanView(dialog.FileName, Project, Results, options);
                
                StatusMessage = "DXF Plan View export complete";
                System.Windows.MessageBox.Show(
                    $"DXF Plan View exported to:\n{dialog.FileName}\n\n" +
                    "Features included:\n" +
                    "• Tolerance circles (0.5x, 1x, 1.5x, 2x)\n" +
                    "• CEP, R95, 2DRMS statistical circles\n" +
                    "• 95% Confidence ellipse\n" +
                    "• Per-heading symbols and mean markers\n" +
                    "• Coordinate grid\n" +
                    "• Legend, scale bar, north arrow",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export error: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private void ExportCharts()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Charts",
            Filter = "PNG Images (*.png)|*.png",
            FileName = $"USBL_Charts_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.png"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                var folder = Path.GetDirectoryName(dialog.FileName);
                var baseName = Path.GetFileNameWithoutExtension(dialog.FileName);
                
                _chartExportService.ExportAllCharts(SpinPlotModel, TransitPlotModel, folder, baseName);
                
                StatusMessage = "Charts exported";
                System.Windows.MessageBox.Show("Charts exported successfully!", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export error: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private void ExportSummary()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Summary",
            Filter = "Text Files (*.txt)|*.txt",
            FileName = $"USBL_Summary_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _projectService.ExportSummary(Project, Results, dialog.FileName);
                StatusMessage = "Summary exported";
                System.Windows.MessageBox.Show($"Summary exported to:\n{dialog.FileName}", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export error: {ex.Message}", "Export Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    #endregion
    
    #region Project Methods
    
    private void NewProject()
    {
        var result = System.Windows.MessageBox.Show("Create a new project? Unsaved changes will be lost.",
            "New Project", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            Project = new UsblVerificationProject();
            Results = new VerificationResults();
            CurrentStep = 1;
            RefreshAllCollections();
            UpdateDataStatusProperties();
            InitializePlotModels();
            StatusMessage = "New project created";
        }
    }
    
    private void LoadProject()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Open USBL Verification Project",
            Filter = "USBL Project Files (*.usblproj)|*.usblproj|All Files (*.*)|*.*",
            DefaultExt = ".usblproj"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading project...";
                
                Project = _projectService.LoadProject(dialog.FileName);
                SelectedInputUnit = Project.InputUnit;
                SelectedOutputUnit = Project.OutputUnit;
                SelectedChartTheme = Project.ChartTheme;
                
                RefreshAllCollections();
                UpdateDataStatusProperties();
                
                // Re-process if data exists
                if (HasAllSpinData)
                {
                    Results = _calculationService.CalculateResults(Project);
                    UpdateCharts();
                }
                
                StatusMessage = $"Project loaded: {Project.ProjectName}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading project: {ex.Message}", "Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private void SaveProject()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save USBL Verification Project",
            Filter = "USBL Project Files (*.usblproj)|*.usblproj",
            FileName = $"{Project.ProjectName}.usblproj",
            DefaultExt = ".usblproj"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                _projectService.SaveProject(Project, dialog.FileName);
                StatusMessage = $"Project saved: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving project: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        // Theme switching is handled by MainWindow code-behind
    }
    
    private void RefreshAllCollections()
    {
        RefreshSpinCollections();
        RefreshTransitCollections();
    }
    
    private void RefreshSpinCollections()
    {
        _spin000Observations.Clear();
        foreach (var obs in Project.Spin000.Observations)
            _spin000Observations.Add(obs);
        
        _spin090Observations.Clear();
        foreach (var obs in Project.Spin090.Observations)
            _spin090Observations.Add(obs);
        
        _spin180Observations.Clear();
        foreach (var obs in Project.Spin180.Observations)
            _spin180Observations.Add(obs);
        
        _spin270Observations.Clear();
        foreach (var obs in Project.Spin270.Observations)
            _spin270Observations.Add(obs);
        
        UpdateSpinPreview();
    }
    
    private void RefreshTransitCollections()
    {
        _transit1Observations.Clear();
        foreach (var obs in Project.Transit1.Observations)
            _transit1Observations.Add(obs);
        
        _transit2Observations.Clear();
        foreach (var obs in Project.Transit2.Observations)
            _transit2Observations.Add(obs);
        
        UpdateTransitPreview();
    }
    
    private void UpdateDataStatusProperties()
    {
        OnPropertyChanged(nameof(HasSpin000Data));
        OnPropertyChanged(nameof(HasSpin090Data));
        OnPropertyChanged(nameof(HasSpin180Data));
        OnPropertyChanged(nameof(HasSpin270Data));
        OnPropertyChanged(nameof(HasTransit1Data));
        OnPropertyChanged(nameof(HasTransit2Data));
        OnPropertyChanged(nameof(HasAllSpinData));
        OnPropertyChanged(nameof(HasAllTransitData));
        OnPropertyChanged(nameof(Spin000PointCount));
        OnPropertyChanged(nameof(Spin090PointCount));
        OnPropertyChanged(nameof(Spin180PointCount));
        OnPropertyChanged(nameof(Spin270PointCount));
        OnPropertyChanged(nameof(Transit1PointCount));
        OnPropertyChanged(nameof(Transit2PointCount));
        OnPropertyChanged(nameof(SpinDataStatus));
        OnPropertyChanged(nameof(TransitDataStatus));
        OnPropertyChanged(nameof(CanProcess));
        
        // Update spin data summaries for Step 2 statistics panel
        UpdateSpinDataSummaries();
    }
    
    private void UpdateUnitWarning()
    {
        if (SelectedInputUnit == LengthUnit.Meters)
        {
            UnitWarning = "";
            OnPropertyChanged(nameof(HasUnitWarning));
            return;
        }
        
        // Find max coordinate value
        double maxCoord = 0;
        foreach (var spin in Project.AllSpinTests)
        {
            foreach (var obs in spin.Observations)
            {
                maxCoord = Math.Max(maxCoord, Math.Abs(obs.VesselEasting));
                maxCoord = Math.Max(maxCoord, Math.Abs(obs.VesselNorthing));
            }
        }
        
        UnitWarning = UnitConversionService.GetUnitWarning(maxCoord, SelectedInputUnit);
        OnPropertyChanged(nameof(HasUnitWarning));
    }
    
    #endregion
    
    #region Batch Import Methods
    
    private void BatchImportSpin()
    {
        // Use OpenFileDialog with multiselect for better control
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Spin Test NPD Files (select multiple)",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true
        };
        
        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            var fileList = dialog.FileNames.ToList();
            
            // Request batch column mapping dialog
            var args = new BatchColumnMappingEventArgs { FilePaths = fileList };
            RequestBatchColumnMapping?.Invoke(this, args);
            
            if (!args.Confirmed || args.ResultMappings == null || args.ResultMappings.Count == 0)
            {
                return;
            }
            
            try
            {
                IsBusy = true;
                StatusMessage = "Importing spin files with configured mappings...";
                
                var parser = new UsblNpdParser();
                int totalPoints = 0;
                
                foreach (var filePath in args.ResultMappings.Keys)
                {
                    var mapping = args.ResultMappings[filePath];
                    var observations = parser.Parse(filePath, mapping);
                    
                    if (observations.Count == 0)
                    {
                        System.Windows.MessageBox.Show($"No data parsed from: {Path.GetFileName(filePath)}", 
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }
                    
                    // Convert units if needed
                    if (SelectedInputUnit != LengthUnit.Meters)
                    {
                        foreach (var obs in observations)
                            _unitConversionService.ConvertObservation(obs, SelectedInputUnit, LengthUnit.Meters);
                    }
                    
                    // Detect heading from filename
                    var fileName = Path.GetFileName(filePath);
                    int heading = DetectHeadingFromFilename(fileName);
                    
                    // Calculate actual average heading from observations
                    double actualHeading = CalculateAverageHeading(observations);
                    
                    if (heading >= 0)
                    {
                        var spinData = heading switch
                        {
                            0 => Project.Spin000,
                            90 => Project.Spin090,
                            180 => Project.Spin180,
                            270 => Project.Spin270,
                            _ => null
                        };
                        
                        if (spinData != null)
                        {
                            spinData.Observations = observations;
                            spinData.SourceFile = fileName;
                            foreach (var obs in observations) obs.SourceFile = fileName;
                        }
                    }
                    else
                    {
                        AutoAssignSpinData(observations, fileName);
                    }
                    
                    // Add to file list with actual heading
                    SpinFiles.Add(new LoadedFileInfo
                    {
                        FilePath = filePath,
                        FileName = fileName,
                        RecordCount = observations.Count,
                        HeadingLabel = $"{actualHeading:F0}°",
                        Mapping = mapping,
                        ActualHeading = actualHeading
                    });
                    
                    totalPoints += observations.Count;
                }
                
                RefreshSpinCollections();
                UpdateDataStatusProperties();
                UpdateQualityMetrics();
                UpdateLivePreviewWithSmoothing();  // Update live preview chart
                OnPropertyChanged(nameof(HasNoSpinFiles));
                
                var message = $"Batch import complete:\n";
                message += $"• Loaded {args.ResultMappings.Count} files\n";
                message += $"• Total {totalPoints} points";
                
                System.Windows.MessageBox.Show(message, "Batch Import", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusMessage = $"Batch imported {args.ResultMappings.Count} spin files";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Batch import error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    private void BatchImportTransit()
    {
        // Use OpenFileDialog with multiselect for better control
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Transit Test NPD Files (select multiple)",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true
        };
        
        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            var fileList = dialog.FileNames.ToList();
            
            // Request batch column mapping dialog
            var args = new BatchColumnMappingEventArgs { FilePaths = fileList };
            RequestBatchColumnMapping?.Invoke(this, args);
            
            if (!args.Confirmed || args.ResultMappings == null || args.ResultMappings.Count == 0)
            {
                return;
            }
            
            try
            {
                IsBusy = true;
                StatusMessage = "Importing transit files with configured mappings...";
                
                var parser = new UsblNpdParser();
                int totalPoints = 0;
                int lineNum = 1;
                
                foreach (var filePath in args.ResultMappings.Keys)
                {
                    var mapping = args.ResultMappings[filePath];
                    var observations = parser.Parse(filePath, mapping);
                    
                    if (observations.Count == 0)
                    {
                        System.Windows.MessageBox.Show($"No data parsed from: {Path.GetFileName(filePath)}", 
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue;
                    }
                    
                    if (SelectedInputUnit != LengthUnit.Meters)
                    {
                        foreach (var obs in observations)
                            _unitConversionService.ConvertObservation(obs, SelectedInputUnit, LengthUnit.Meters);
                    }
                    
                    var fileName = Path.GetFileName(filePath);
                    var transitData = lineNum == 1 ? Project.Transit1 : Project.Transit2;
                    transitData.Observations = observations;
                    transitData.SourceFile = fileName;
                    foreach (var obs in observations) obs.SourceFile = fileName;
                    
                    // Add to file list
                    TransitFiles.Add(new LoadedFileInfo
                    {
                        FilePath = filePath,
                        FileName = fileName,
                        RecordCount = observations.Count,
                        HeadingLabel = $"Line {lineNum}",
                        Mapping = mapping
                    });
                    
                    totalPoints += observations.Count;
                    lineNum++;
                }
                
                RefreshTransitCollections();
                UpdateDataStatusProperties();
                UpdateQualityMetrics();
                UpdateLivePreviewWithSmoothing();  // Update live preview chart
                OnPropertyChanged(nameof(HasNoTransitFiles));
                
                StatusMessage = $"Batch imported {args.ResultMappings.Count} transit files ({totalPoints} points)";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Batch import error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    #endregion
    
    #region Live Preview Methods
    
    private void UpdateLivePreview()
    {
        UpdateLivePreviewWithSmoothing();
    }
    
    /// <summary>
    /// Update live preview chart with optional smoothing visualization
    /// </summary>
    private void UpdateLivePreviewWithSmoothing()
    {
        var model = new PlotModel
        {
            Title = IsSmoothingEnabled ? "Live Data Preview (Smoothing Applied)" : "Live Data Preview",
            TitleFontSize = 12,
            PlotAreaBorderColor = OxyColors.Gray
        };
        
        // Add legend
        model.Legends.Add(new OxyPlot.Legends.Legend
        {
            LegendPosition = OxyPlot.Legends.LegendPosition.TopRight,
            LegendPlacement = OxyPlot.Legends.LegendPlacement.Inside,
            LegendBackground = OxyColor.FromArgb(200, 50, 50, 50),
            LegendTextColor = OxyColors.White
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Easting",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });
        
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Northing",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColor.FromRgb(60, 60, 60)
        });
        
        var colors = GetChartColors(SelectedChartTheme);
        
        // Add data for each loaded spin
        var spinTests = new[]
        {
            (Project.Spin000, colors[0]),
            (Project.Spin090, colors[1]),
            (Project.Spin180, colors[2]),
            (Project.Spin270, colors[3])
        };
        
        foreach (var (spin, color) in spinTests)
        {
            if (!spin.HasData) continue;
            
            var validObs = spin.Observations.Where(o => !o.IsExcluded).ToList();
            
            // If smoothing is enabled and showing difference, display both
            if (IsSmoothingEnabled && ShowSmoothedDifference)
            {
                // Original points (faded)
                var originalSeries = new ScatterSeries
                {
                    Title = $"{spin.DisplayName} (Original)",
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 3,
                    MarkerFill = OxyColor.FromAColor(80, color), // Semi-transparent
                    MarkerStroke = OxyColors.Transparent
                };
                
                // Smoothed points (solid)
                var smoothedSeries = new ScatterSeries
                {
                    Title = $"{spin.DisplayName} (Smoothed)",
                    MarkerType = MarkerType.Diamond,
                    MarkerSize = 5,
                    MarkerFill = color,
                    MarkerStroke = OxyColors.White,
                    MarkerStrokeThickness = 1
                };
                
                // Connecting lines showing movement
                var lineSeries = new LineSeries
                {
                    Title = null, // No legend entry
                    Color = OxyColor.FromAColor(100, color),
                    StrokeThickness = 0.5,
                    LineStyle = LineStyle.Dot
                };
                
                foreach (var obs in validObs)
                {
                    // Original position
                    originalSeries.Points.Add(new ScatterPoint(obs.TransponderEasting, obs.TransponderNorthing));
                    
                    // Smoothed position (or original if no smoothing applied)
                    var smoothedE = obs.SmoothedEasting ?? obs.TransponderEasting;
                    var smoothedN = obs.SmoothedNorthing ?? obs.TransponderNorthing;
                    smoothedSeries.Points.Add(new ScatterPoint(smoothedE, smoothedN));
                    
                    // Draw line from original to smoothed if they differ
                    if (obs.SmoothedEasting.HasValue && 
                        (Math.Abs(obs.TransponderEasting - smoothedE) > 0.001 || 
                         Math.Abs(obs.TransponderNorthing - smoothedN) > 0.001))
                    {
                        lineSeries.Points.Add(new DataPoint(obs.TransponderEasting, obs.TransponderNorthing));
                        lineSeries.Points.Add(new DataPoint(smoothedE, smoothedN));
                        lineSeries.Points.Add(new DataPoint(double.NaN, double.NaN)); // Break line
                    }
                }
                
                model.Series.Add(lineSeries);
                model.Series.Add(originalSeries);
                model.Series.Add(smoothedSeries);
            }
            else
            {
                // Standard view - show either original or smoothed based on smoothing state
                var series = new ScatterSeries
                {
                    Title = spin.DisplayName,
                    MarkerType = MarkerType.Circle,
                    MarkerSize = 4,
                    MarkerFill = color
                };
                
                foreach (var obs in validObs)
                {
                    // Use BestEasting/BestNorthing which returns smoothed if available
                    double x = IsSmoothingEnabled ? obs.BestEasting : obs.TransponderEasting;
                    double y = IsSmoothingEnabled ? obs.BestNorthing : obs.TransponderNorthing;
                    series.Points.Add(new ScatterPoint(x, y));
                }
                
                model.Series.Add(series);
            }
        }
        
        LivePreviewPlotModel = model;
        LivePreviewPlotModel.InvalidatePlot(true);
    }
    
    #endregion
    
    #region Quality Dashboard Methods
    
    private void UpdateQualityMetrics()
    {
        QualityMetrics.Clear();
        
        foreach (var spin in Project.AllSpinTests)
        {
            if (spin.HasData)
            {
                var metrics = _qualityService.CalculateMetrics(spin);
                QualityMetrics.Add(metrics);
            }
        }
        
        foreach (var transit in Project.AllTransitTests)
        {
            if (transit.HasData)
            {
                var metrics = _qualityService.CalculateMetrics(transit);
                QualityMetrics.Add(metrics);
            }
        }
        
        OverallQuality = _qualityService.CalculateOverallQuality(Project);
    }
    
    /// <summary>
    /// Update all advanced analytics charts
    /// </summary>
    private void UpdateAdvancedCharts()
    {
        try
        {
            var allSpinObs = GetAllSpinObservations();
            
            if (allSpinObs.Count < 3)
            {
                // Clear charts if insufficient data
                ErrorEllipsePlotModel = null;
                RadarPlotModel = null;
                ResidualTimeSeriesPlotModel = null;
                ControlChartPlotModel = null;
                DepthSlantRangePlotModel = null;
                RoseDiagramPlotModel = null;
                SmoothingComparisonPlotModel = null;
                return;
            }
            
            // Error Ellipse Chart - shows 95% confidence ellipse
            ErrorEllipsePlotModel = _advancedChartService.CreateErrorEllipseChart(
                allSpinObs, 0.95, "Position Error Ellipse (95% Confidence)");
            
            // Heading Comparison Radar Chart
            RadarPlotModel = _advancedChartService.CreateHeadingComparisonRadar(
                Project, "Heading Performance Comparison");
            
            // Residual Time Series - for quality control
            ResidualTimeSeriesPlotModel = _advancedChartService.CreateResidualTimeSeriesChart(
                allSpinObs, "Residual Analysis Over Time");
            
            // Statistical Control Chart (X-bar)
            ControlChartPlotModel = _advancedChartService.CreateControlChart(
                allSpinObs, 5, "X-bar Control Chart");
            
            // Depth vs Slant Range
            DepthSlantRangePlotModel = _advancedChartService.CreateDepthSlantRangeChart(
                allSpinObs, "Depth vs Slant Range Analysis");
            
            // Rose Diagram - directional error distribution
            RoseDiagramPlotModel = _advancedChartService.CreateRoseDiagram(
                allSpinObs, 36, "Error Direction Distribution");
            
            // Before/After Smoothing Comparison
            SmoothingComparisonPlotModel = _advancedChartService.CreateSmoothingComparisonChart(
                allSpinObs, "Smoothing Effect Analysis");
            
            StatusMessage = "Advanced charts updated";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating advanced charts: {ex.Message}");
            StatusMessage = "Error updating advanced charts";
        }
    }
    
    /// <summary>
    /// Command handler to refresh all advanced charts
    /// </summary>
    private void RefreshAdvancedCharts()
    {
        if (!HasAllSpinData)
        {
            System.Windows.MessageBox.Show("Please load spin data before generating advanced charts.", 
                "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        IsBusy = true;
        StatusMessage = "Generating advanced charts...";
        
        try
        {
            UpdateAdvancedCharts();
            System.Windows.MessageBox.Show(
                "Advanced charts generated successfully!\n\n" +
                "Available charts:\n" +
                "• Error Ellipse (confidence regions)\n" +
                "• Heading Comparison Radar\n" +
                "• Residual Time Series\n" +
                "• Statistical Control Chart\n" +
                "• Depth vs Slant Range\n" +
                "• Rose Diagram (directional errors)\n" +
                "• Smoothing Comparison",
                "Charts Generated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    #endregion
    
    #region Point Selection Methods
    
    private void SelectPoint(object parameter)
    {
        // Parameter format: "DatasetName:Index" or from chart click
        if (parameter is string pointStr)
        {
            var parts = pointStr.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int index))
            {
                var datasetName = parts[0];
                UsblObservation? obs = FindObservation(datasetName, index);
                
                if (obs != null)
                {
                    SelectedPoint = new SelectedPointInfo
                    {
                        Observation = obs,
                        DatasetName = datasetName,
                        PointIndex = index,
                        ChartX = obs.TransponderEasting,
                        ChartY = obs.TransponderNorthing
                    };
                }
            }
        }
    }
    
    private UsblObservation? FindObservation(string datasetName, int index)
    {
        return datasetName switch
        {
            "Spin 0°" => Project.Spin000.Observations.FirstOrDefault(o => o.Index == index),
            "Spin 90°" => Project.Spin090.Observations.FirstOrDefault(o => o.Index == index),
            "Spin 180°" => Project.Spin180.Observations.FirstOrDefault(o => o.Index == index),
            "Spin 270°" => Project.Spin270.Observations.FirstOrDefault(o => o.Index == index),
            "Transit 1" => Project.Transit1.Observations.FirstOrDefault(o => o.Index == index),
            "Transit 2" => Project.Transit2.Observations.FirstOrDefault(o => o.Index == index),
            _ => null
        };
    }
    
    private void ExcludeSelectedPoint()
    {
        if (SelectedPoint?.Observation != null)
        {
            SelectedPoint.Observation.IsExcluded = !SelectedPoint.Observation.IsExcluded;
            
            // Update preview and quality
            UpdateSpinPreview();
            UpdateTransitPreview();
            UpdateQualityMetrics();
            UpdateLivePreview();
            
            StatusMessage = SelectedPoint.Observation.IsExcluded 
                ? $"Point #{SelectedPoint.Observation.Index} excluded"
                : $"Point #{SelectedPoint.Observation.Index} included";
        }
    }
    
    private void ClearSelection()
    {
        SelectedPoint = new SelectedPointInfo();
    }
    
    // Call this from chart click event
    public void HandleChartClick(string seriesTitle, int pointIndex, double x, double y)
    {
        // Map series title to dataset
        var datasetName = seriesTitle.Replace(" Points", "").Replace("Spin ", "Spin ");
        
        // Find the observation at this position
        foreach (var spin in Project.AllSpinTests)
        {
            if (!spin.HasData) continue;
            
            var matchingObs = spin.Observations
                .Where(o => !o.IsExcluded)
                .FirstOrDefault(o => Math.Abs(o.TransponderEasting - x) < 0.01 && 
                                      Math.Abs(o.TransponderNorthing - y) < 0.01);
            
            if (matchingObs != null)
            {
                SelectedPoint = new SelectedPointInfo
                {
                    Observation = matchingObs,
                    DatasetName = spin.Name,
                    PointIndex = matchingObs.Index,
                    ChartX = x,
                    ChartY = y,
                    DistanceFromCenter = Math.Sqrt(x * x + y * y)
                };
                return;
            }
        }
    }
    
    #endregion
    
    #region Template Methods
    
    private void LoadTemplates()
    {
        AvailableTemplates.Clear();
        foreach (var template in _templateService.GetAllTemplates())
        {
            AvailableTemplates.Add(template);
        }
        
        SelectedTemplate = AvailableTemplates.FirstOrDefault() ?? new ReportTemplate();
    }
    
    private void SaveCurrentTemplate()
    {
        if (SelectedTemplate == null || SelectedTemplate.IsDefault) return;
        
        try
        {
            _templateService.SaveTemplate(SelectedTemplate);
            StatusMessage = $"Template '{SelectedTemplate.TemplateName}' saved";
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving template: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ImportLogo()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Logo Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*"
        };
        
        if (dialog.ShowDialog() == true && SelectedTemplate != null)
        {
            try
            {
                SelectedTemplate.LogoData = _templateService.LoadLogoFromFile(dialog.FileName);
                SelectedTemplate.LogoPath = dialog.FileName;
                StatusMessage = "Logo loaded";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading logo: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void ImportSignature()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Signature Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*"
        };
        
        if (dialog.ShowDialog() == true && SelectedTemplate != null)
        {
            try
            {
                SelectedTemplate.SignatureData = _templateService.LoadSignatureFromFile(dialog.FileName);
                SelectedTemplate.SignaturePath = dialog.FileName;
                StatusMessage = "Signature loaded";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading signature: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    #endregion
    
    #region Certificate Methods
    
    private void ExportCertificate()
    {
        if (Results == null || !Results.OverallPass)
        {
            System.Windows.MessageBox.Show("Cannot generate certificate. Verification must PASS to generate a certificate.",
                "Certificate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Verification Certificate",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"USBL_Certificate_{Project.ProjectName}_{DateTime.Now:yyyyMMdd}.pdf"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Generating certificate...";
                
                if (SelectedTemplate != null)
                {
                    _certificateService.SetTemplate(SelectedTemplate);
                }
                
                _certificateService.GenerateCertificate(dialog.FileName, Project, Results);
                
                StatusMessage = "Certificate generated";
                System.Windows.MessageBox.Show($"Verification certificate saved to:\n{dialog.FileName}",
                    "Certificate Generated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error generating certificate: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    
    #endregion
    
    #region Advanced Analytics
    
    private PlotModel? _timeSeriesPlotModel;
    private PlotModel? _histogramPlotModel;
    private PlotModel? _polarPlotModel;
    private PlotModel? _trendPlotModel;
    
    // New advanced chart models
    private PlotModel? _errorEllipsePlotModel;
    private PlotModel? _radarPlotModel;
    private PlotModel? _residualTimeSeriesPlotModel;
    private PlotModel? _controlChartPlotModel;
    private PlotModel? _depthSlantRangePlotModel;
    private PlotModel? _roseDiagramPlotModel;
    private PlotModel? _smoothingComparisonPlotModel;
    
    private RadialStatistics? _selectedRadialStats;
    private MonteCarloResult? _monteCarloResult;
    private TrendAnalysisResult? _trendAnalysis;
    private SmoothingMethod _selectedSmoothingMethod = SmoothingMethod.None;
    private int _smoothingWindowSize = 5;
    private bool _isSmoothingEnabled = false;
    private double _smoothingStrength = 50;
    private double _smoothingRmsResidual = 0;
    private int _smoothingPointsAffected = 0;
    private bool _showSmoothedDifference = true; // Show original vs smoothed in different colors
    private ObservableCollection<RecentProject> _recentProjects = new();
    private ObservableCollection<VerificationIndexEntry> _historyRecords = new();
    
    // 3D Visualization fields
    private readonly Visualization3DService _visualization3DService = new();
    private Model3DGroup? _scene3D;
    private bool _has3DScene = false;
    private bool _show3DAxes = true;
    private bool _show3DGrid = true;
    private bool _show3DTolerance = true;
    private bool _show3DStats = true;
    private bool _show3DTransit = true;
    private double _pointSize3D = 0.15;
    private Point3D _camera3DPosition = new Point3D(15, 15, 10);
    private Vector3D _camera3DLookDirection = new Vector3D(-1, -1, -0.5);
    private int _info3DPointCount = 0;
    private string _info3DDepthRange = "";
    
    public PlotModel? TimeSeriesPlotModel
    {
        get => _timeSeriesPlotModel;
        set => SetProperty(ref _timeSeriesPlotModel, value);
    }
    
    public PlotModel? HistogramPlotModel
    {
        get => _histogramPlotModel;
        set => SetProperty(ref _histogramPlotModel, value);
    }
    
    public PlotModel? PolarPlotModel
    {
        get => _polarPlotModel;
        set => SetProperty(ref _polarPlotModel, value);
    }
    
    public PlotModel? TrendPlotModel
    {
        get => _trendPlotModel;
        set => SetProperty(ref _trendPlotModel, value);
    }
    
    // New advanced chart properties
    public PlotModel? ErrorEllipsePlotModel
    {
        get => _errorEllipsePlotModel;
        set => SetProperty(ref _errorEllipsePlotModel, value);
    }
    
    public PlotModel? RadarPlotModel
    {
        get => _radarPlotModel;
        set => SetProperty(ref _radarPlotModel, value);
    }
    
    public PlotModel? ResidualTimeSeriesPlotModel
    {
        get => _residualTimeSeriesPlotModel;
        set => SetProperty(ref _residualTimeSeriesPlotModel, value);
    }
    
    public PlotModel? ControlChartPlotModel
    {
        get => _controlChartPlotModel;
        set => SetProperty(ref _controlChartPlotModel, value);
    }
    
    public PlotModel? DepthSlantRangePlotModel
    {
        get => _depthSlantRangePlotModel;
        set => SetProperty(ref _depthSlantRangePlotModel, value);
    }
    
    public PlotModel? RoseDiagramPlotModel
    {
        get => _roseDiagramPlotModel;
        set => SetProperty(ref _roseDiagramPlotModel, value);
    }
    
    public PlotModel? SmoothingComparisonPlotModel
    {
        get => _smoothingComparisonPlotModel;
        set => SetProperty(ref _smoothingComparisonPlotModel, value);
    }
    
    public RadialStatistics? SelectedRadialStats
    {
        get => _selectedRadialStats;
        set => SetProperty(ref _selectedRadialStats, value);
    }
    
    public MonteCarloResult? MonteCarloResult
    {
        get => _monteCarloResult;
        set => SetProperty(ref _monteCarloResult, value);
    }
    
    public TrendAnalysisResult? TrendAnalysis
    {
        get => _trendAnalysis;
        set => SetProperty(ref _trendAnalysis, value);
    }
    
    public SmoothingMethod SelectedSmoothingMethod
    {
        get => _selectedSmoothingMethod;
        set
        {
            if (SetProperty(ref _selectedSmoothingMethod, value))
            {
                ApplyRealTimeSmoothing();
            }
        }
    }
    
    public int SmoothingWindowSize
    {
        get => _smoothingWindowSize;
        set
        {
            if (SetProperty(ref _smoothingWindowSize, value))
            {
                ApplyRealTimeSmoothing();
            }
        }
    }
    
    public bool IsSmoothingEnabled
    {
        get => _isSmoothingEnabled;
        set
        {
            if (SetProperty(ref _isSmoothingEnabled, value))
            {
                ApplyRealTimeSmoothing();
            }
        }
    }
    
    public double SmoothingStrength
    {
        get => _smoothingStrength;
        set
        {
            if (SetProperty(ref _smoothingStrength, value))
            {
                ApplyRealTimeSmoothing();
            }
        }
    }
    
    public double SmoothingRmsResidual
    {
        get => _smoothingRmsResidual;
        set => SetProperty(ref _smoothingRmsResidual, value);
    }
    
    public int SmoothingPointsAffected
    {
        get => _smoothingPointsAffected;
        set => SetProperty(ref _smoothingPointsAffected, value);
    }
    
    /// <summary>
    /// When enabled, shows original points and smoothed points in different colors
    /// </summary>
    public bool ShowSmoothedDifference
    {
        get => _showSmoothedDifference;
        set
        {
            if (SetProperty(ref _showSmoothedDifference, value))
            {
                UpdateLivePreviewWithSmoothing();
            }
        }
    }
    
    #endregion
    
    #region Outlier/Spike Filtering Properties
    
    private bool _isOutlierFilterEnabled;
    private double _outlierThresholdSigma = 2.5;
    private int _outlierCount;
    private ObservableCollection<UsblObservation> _detectedOutliers = new();
    private bool _showOutliersHighlighted = true;
    
    /// <summary>
    /// Enable outlier detection and filtering
    /// </summary>
    public bool IsOutlierFilterEnabled
    {
        get => _isOutlierFilterEnabled;
        set
        {
            if (SetProperty(ref _isOutlierFilterEnabled, value))
            {
                DetectOutliers();
            }
        }
    }
    
    /// <summary>
    /// Threshold in standard deviations for outlier detection (default 2.5σ)
    /// </summary>
    public double OutlierThresholdSigma
    {
        get => _outlierThresholdSigma;
        set
        {
            if (SetProperty(ref _outlierThresholdSigma, value))
            {
                DetectOutliers();
            }
        }
    }
    
    /// <summary>
    /// Number of detected outliers
    /// </summary>
    public int OutlierCount
    {
        get => _outlierCount;
        set => SetProperty(ref _outlierCount, value);
    }
    
    /// <summary>
    /// List of detected outliers for user review
    /// </summary>
    public ObservableCollection<UsblObservation> DetectedOutliers
    {
        get => _detectedOutliers;
        set => SetProperty(ref _detectedOutliers, value);
    }
    
    /// <summary>
    /// Highlight outliers in charts
    /// </summary>
    public bool ShowOutliersHighlighted
    {
        get => _showOutliersHighlighted;
        set
        {
            if (SetProperty(ref _showOutliersHighlighted, value))
            {
                UpdateSpinPlot();
            }
        }
    }
    
    /// <summary>
    /// Detect outliers based on statistical threshold
    /// </summary>
    private void DetectOutliers()
    {
        if (!HasAllSpinData) return;
        
        var allObs = GetAllSpinObservations();
        if (allObs.Count < 10) return;
        
        DetectedOutliers.Clear();
        
        if (!IsOutlierFilterEnabled)
        {
            OutlierCount = 0;
            UpdateSpinPlot();
            return;
        }
        
        try
        {
            // Calculate mean position
            var validObs = allObs.Where(o => !o.IsExcluded).ToList();
            double meanE = validObs.Average(o => o.TransponderEasting);
            double meanN = validObs.Average(o => o.TransponderNorthing);
            
            // Calculate standard deviation of radial distance
            var radials = validObs.Select(o => 
                Math.Sqrt(Math.Pow(o.TransponderEasting - meanE, 2) + 
                         Math.Pow(o.TransponderNorthing - meanN, 2))).ToList();
            
            double meanRadial = radials.Average();
            double stdRadial = Math.Sqrt(radials.Average(r => Math.Pow(r - meanRadial, 2)));
            
            // Threshold based on sigma
            double threshold = meanRadial + (OutlierThresholdSigma * stdRadial);
            
            // Detect outliers
            foreach (var obs in validObs)
            {
                double radial = Math.Sqrt(Math.Pow(obs.TransponderEasting - meanE, 2) + 
                                         Math.Pow(obs.TransponderNorthing - meanN, 2));
                
                // Always set radial from mean for display purposes
                obs.RadialFromMean = radial;
                
                if (radial > threshold)
                {
                    obs.IsOutlier = true;
                    DetectedOutliers.Add(obs);
                }
                else
                {
                    obs.IsOutlier = false;
                }
            }
            
            OutlierCount = DetectedOutliers.Count;
            OnPropertyChanged(nameof(DetectedOutliers));
            
            StatusMessage = $"Detected {OutlierCount} outliers (>{OutlierThresholdSigma:F1}σ from mean)";
            
            // Update visualization
            UpdateSpinPlot();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Outlier detection error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Exclude a single detected outlier
    /// </summary>
    public void ExcludeOutlier(UsblObservation outlier)
    {
        if (outlier == null) return;
        
        outlier.IsExcluded = true;
        outlier.IsOutlier = false;
        DetectedOutliers.Remove(outlier);
        OutlierCount = DetectedOutliers.Count;
        
        UpdateSpinPlot();
        StatusMessage = $"Excluded outlier point - {OutlierCount} remaining";
    }
    
    /// <summary>
    /// Exclude all detected outliers at once
    /// </summary>
    public void ExcludeAllOutliers()
    {
        if (DetectedOutliers.Count == 0) return;
        
        var result = System.Windows.MessageBox.Show(
            $"Exclude all {DetectedOutliers.Count} detected outliers?\n\nThis action can be undone by resetting the data.",
            "Confirm Exclude All",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
            
        if (result != MessageBoxResult.Yes) return;
        
        foreach (var outlier in DetectedOutliers.ToList())
        {
            outlier.IsExcluded = true;
            outlier.IsOutlier = false;
        }
        
        int excluded = DetectedOutliers.Count;
        DetectedOutliers.Clear();
        OutlierCount = 0;
        
        UpdateSpinPlot();
        UpdateSpinDataSummaries();
        StatusMessage = $"Excluded {excluded} outlier points";
    }
    
    /// <summary>
    /// Restore all excluded points
    /// </summary>
    public void RestoreAllExcludedPoints()
    {
        var allObs = GetAllSpinObservations();
        int restored = 0;
        
        foreach (var obs in allObs.Where(o => o.IsExcluded))
        {
            obs.IsExcluded = false;
            restored++;
        }
        
        // Re-detect outliers
        DetectOutliers();
        UpdateSpinDataSummaries();
        StatusMessage = $"Restored {restored} excluded points";
    }
    
    // Commands for outlier filtering
    public ICommand ExcludeOutlierCommand { get; private set; }
    public ICommand ExcludeAllOutliersCommand { get; private set; }
    public ICommand RestoreExcludedCommand { get; private set; }
    
    #endregion
    
    #region Recent Projects
    
    public ObservableCollection<RecentProject> RecentProjects
    {
        get => _recentProjects;
        set => SetProperty(ref _recentProjects, value);
    }
    
    public ObservableCollection<VerificationIndexEntry> HistoryRecords
    {
        get => _historyRecords;
        set => SetProperty(ref _historyRecords, value);
    }
    
    #endregion
    
    public IEnumerable<SmoothingMethod> AvailableSmoothingMethods => 
        Enum.GetValues(typeof(SmoothingMethod)).Cast<SmoothingMethod>();
    
    #region 3D Visualization Properties
    
    public Model3DGroup? Scene3D
    {
        get => _scene3D;
        set => SetProperty(ref _scene3D, value);
    }
    
    public bool Has3DScene
    {
        get => _has3DScene;
        set => SetProperty(ref _has3DScene, value);
    }
    
    public bool Show3DAxes
    {
        get => _show3DAxes;
        set
        {
            if (SetProperty(ref _show3DAxes, value))
                Regenerate3DScene();
        }
    }
    
    public bool Show3DGrid
    {
        get => _show3DGrid;
        set
        {
            if (SetProperty(ref _show3DGrid, value))
                Regenerate3DScene();
        }
    }
    
    public bool Show3DTolerance
    {
        get => _show3DTolerance;
        set
        {
            if (SetProperty(ref _show3DTolerance, value))
                Regenerate3DScene();
        }
    }
    
    public bool Show3DStats
    {
        get => _show3DStats;
        set
        {
            if (SetProperty(ref _show3DStats, value))
                Regenerate3DScene();
        }
    }
    
    public bool Show3DTransit
    {
        get => _show3DTransit;
        set
        {
            if (SetProperty(ref _show3DTransit, value))
                Regenerate3DScene();
        }
    }
    
    public double PointSize3D
    {
        get => _pointSize3D;
        set
        {
            if (SetProperty(ref _pointSize3D, value))
                Regenerate3DScene();
        }
    }
    
    public Point3D Camera3DPosition
    {
        get => _camera3DPosition;
        set => SetProperty(ref _camera3DPosition, value);
    }
    
    public Vector3D Camera3DLookDirection
    {
        get => _camera3DLookDirection;
        set => SetProperty(ref _camera3DLookDirection, value);
    }
    
    public int Info3DPointCount
    {
        get => _info3DPointCount;
        set => SetProperty(ref _info3DPointCount, value);
    }
    
    public string Info3DDepthRange
    {
        get => _info3DDepthRange;
        set => SetProperty(ref _info3DDepthRange, value);
    }
    
    #endregion
    
    #region Smoothing Methods
    
    /// <summary>
    /// Calculate and display advanced radial statistics
    /// </summary>
    public void CalculateRadialStatistics(string datasetName, List<UsblObservation> observations)
    {
        SelectedRadialStats = _advancedStatsService.CalculateRadialStatistics(observations, datasetName);
    }
    
    /// <summary>
    /// Run Monte Carlo simulation for confidence bounds
    /// </summary>
    public void RunMonteCarloSimulation(List<UsblObservation> observations, int iterations = 10000)
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"Running Monte Carlo simulation ({iterations} iterations)...";
            
            MonteCarloResult = _advancedStatsService.RunMonteCarloSimulation(observations, iterations);
            
            StatusMessage = "Monte Carlo simulation complete";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Monte Carlo error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Analyze position drift trend
    /// </summary>
    public void AnalyzeTrend(List<UsblObservation> observations)
    {
        TrendAnalysis = _advancedStatsService.AnalyzeTrend(observations);
        TrendPlotModel = _advancedChartService.CreateTrendChart(observations);
    }
    
    /// <summary>
    /// Generate time series chart
    /// </summary>
    public void GenerateTimeSeriesChart(List<UsblObservation> observations)
    {
        TimeSeriesPlotModel = _advancedChartService.CreatePositionDriftChart(observations);
    }
    
    /// <summary>
    /// Generate histogram chart
    /// </summary>
    public void GenerateHistogramChart(List<UsblObservation> observations)
    {
        HistogramPlotModel = _advancedChartService.CreateHistogramChart(observations);
    }
    
    /// <summary>
    /// Generate polar plot
    /// </summary>
    public void GeneratePolarPlot(List<UsblObservation> observations)
    {
        PolarPlotModel = _advancedChartService.CreatePolarPlot(observations, Project.Tolerance);
    }
    
    /// <summary>
    /// Apply smoothing to data
    /// </summary>
    public void ApplySmoothing(List<UsblObservation> observations)
    {
        if (SelectedSmoothingMethod == SmoothingMethod.None)
        {
            _smoothingService.ClearSmoothing(observations);
            StatusMessage = "Smoothing removed";
            return;
        }
        
        var options = new SmoothingOptions { WindowSize = SmoothingWindowSize };
        var result = _smoothingService.ApplySmoothing(observations, SelectedSmoothingMethod, options);
        
        if (result.Success)
        {
            StatusMessage = $"Smoothing applied: RMS residual = {result.RmsResidual:F4}m";
        }
        else
        {
            StatusMessage = $"Smoothing failed: {result.Message}";
        }
    }
    
    /// <summary>
    /// Generate all advanced analytics for current spin data
    /// </summary>
    public void GenerateAllAnalytics()
    {
        var allSpinObs = GetAllSpinObservations();
        if (allSpinObs.Count < 3)
        {
            StatusMessage = "Insufficient data for analytics";
            return;
        }
        
        try
        {
            IsBusy = true;
            StatusMessage = "Generating advanced analytics...";
            
            // Calculate radial statistics
            SelectedRadialStats = _advancedStatsService.CalculateRadialStatistics(allSpinObs, "All Spin Data");
            
            // Generate charts
            TimeSeriesPlotModel = _advancedChartService.CreatePositionDriftChart(allSpinObs);
            HistogramPlotModel = _advancedChartService.CreateHistogramChart(allSpinObs);
            PolarPlotModel = _advancedChartService.CreatePolarPlot(allSpinObs, Project.Tolerance);
            
            // Analyze trend
            TrendAnalysis = _advancedStatsService.AnalyzeTrend(allSpinObs);
            TrendPlotModel = _advancedChartService.CreateTrendChart(allSpinObs);
            
            StatusMessage = "Advanced analytics complete";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Analytics error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private List<UsblObservation> GetAllSpinObservations()
    {
        var all = new List<UsblObservation>();
        all.AddRange(_spin000Observations);
        all.AddRange(_spin090Observations);
        all.AddRange(_spin180Observations);
        all.AddRange(_spin270Observations);
        return all;
    }
    
    /// <summary>
    /// Apply smoothing to current spin data (button click)
    /// </summary>
    private void ApplySmoothingToCurrentData()
    {
        var allObs = GetAllSpinObservations();
        if (allObs.Count == 0)
        {
            StatusMessage = "No data to smooth";
            return;
        }
        
        ApplySmoothing(allObs);
        
        // Refresh charts after smoothing
        UpdateSpinPlot();
    }
    
    /// <summary>
    /// Apply real-time smoothing when slider or settings change
    /// </summary>
    private void ApplyRealTimeSmoothing()
    {
        if (!HasAllSpinData) return;
        
        var allObs = GetAllSpinObservations();
        if (allObs.Count == 0) return;
        
        if (!IsSmoothingEnabled || SelectedSmoothingMethod == SmoothingMethod.None)
        {
            // Clear smoothing - restore original values
            _smoothingService.ClearSmoothing(allObs);
            SmoothingRmsResidual = 0;
            SmoothingPointsAffected = 0;
            UpdateLivePreviewWithSmoothing();
            UpdateSpinPlot();
            
            // Update smoothing comparison chart
            SmoothingComparisonPlotModel = _advancedChartService.CreateSmoothingComparisonChart(
                allObs, "Smoothing Effect Analysis");
            return;
        }
        
        try
        {
            // Calculate effective window size based on strength
            // Strength 0% = minimum effect, 100% = maximum effect
            int effectiveWindow = Math.Max(3, (int)(3 + (SmoothingWindowSize - 3) * (SmoothingStrength / 100.0)));
            if (effectiveWindow % 2 == 0) effectiveWindow++; // Ensure odd
            
            var options = new SmoothingOptions 
            { 
                WindowSize = effectiveWindow,
                // Pass strength for methods that can use it (like Gaussian sigma)
                Sigma = 0.5 + (SmoothingStrength / 100.0) * 2.0 // 0.5 to 2.5
            };
            
            var result = _smoothingService.ApplySmoothing(allObs, SelectedSmoothingMethod, options);
            
            if (result.Success)
            {
                SmoothingRmsResidual = result.RmsResidual;
                SmoothingPointsAffected = result.PointsSmoothed;
            }
            
            // Update charts with smoothed data
            UpdateLivePreviewWithSmoothing();
            UpdateSpinPlot();
            
            // Update smoothing comparison chart to show before/after
            SmoothingComparisonPlotModel = _advancedChartService.CreateSmoothingComparisonChart(
                allObs, "Smoothing Effect Analysis");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Real-time smoothing error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Reset smoothing to original data
    /// </summary>
    private void ResetSmoothing()
    {
        var allObs = GetAllSpinObservations();
        if (allObs.Count == 0) return;
        
        _smoothingService.ClearSmoothing(allObs);
        
        IsSmoothingEnabled = false;
        SmoothingStrength = 50;
        SmoothingWindowSize = 5;
        SelectedSmoothingMethod = SmoothingMethod.None;
        SmoothingRmsResidual = 0;
        SmoothingPointsAffected = 0;
        
        UpdateSpinPlot();
        StatusMessage = "Smoothing reset to original data";
    }
    
    /// <summary>
    /// Refresh the spin plot with current data
    /// </summary>
    private void UpdateSpinPlot()
    {
        // Trigger full chart update
        if (HasAllSpinData)
        {
            UpdateSpinChart();
        }
    }
    
    #endregion
    
    #region 3D Visualization Methods
    
    /// <summary>
    /// Generate 3D visualization scene
    /// </summary>
    private void Generate3DView()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Generating 3D visualization...";
            
            var options = new Visualization3DOptions
            {
                ShowAxes = Show3DAxes,
                ShowGrid = Show3DGrid,
                ShowToleranceSphere = Show3DTolerance,
                ShowStatisticalSurfaces = Show3DStats,
                ShowTransitData = Show3DTransit,
                PointSize = PointSize3D,
                ShowMeanPosition = true
            };
            
            Scene3D = _visualization3DService.CreateScene(Project, Results, options);
            Has3DScene = true;
            
            // Notify view to update 3D model
            Scene3DUpdated?.Invoke(this, Scene3D);
            
            // Update info
            var allObs = GetAllSpinObservations();
            Info3DPointCount = allObs.Count;
            if (allObs.Count > 0)
            {
                var minDepth = allObs.Min(o => o.TransponderDepth);
                var maxDepth = allObs.Max(o => o.TransponderDepth);
                Info3DDepthRange = $"Depth: {minDepth:F1}m - {maxDepth:F1}m";
            }
            
            StatusMessage = $"3D view generated with {Info3DPointCount} points";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error generating 3D view: {ex.Message}";
            Has3DScene = false;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Regenerate 3D scene when options change
    /// </summary>
    private void Regenerate3DScene()
    {
        if (Has3DScene && HasAllSpinData)
        {
            Generate3DView();
        }
    }
    
    /// <summary>
    /// Reset 3D camera view
    /// </summary>
    private void Reset3DView()
    {
        Camera3DPosition = new Point3D(15, 15, 10);
        Camera3DLookDirection = new Vector3D(-1, -1, -0.5);
        OnPropertyChanged(nameof(Camera3DPosition));
        OnPropertyChanged(nameof(Camera3DLookDirection));
    }
    
    #endregion
    
    #region Recent Projects
    
    private void LoadRecentProjects()
    {
        var recent = _recentProjectsService.GetRecentProjects();
        RecentProjects = new ObservableCollection<RecentProject>(recent);
    }
    
    public void OpenRecentProject(RecentProject project)
    {
        if (string.IsNullOrEmpty(project.ProjectPath) || !File.Exists(project.ProjectPath))
        {
            MessageBox.Show("Project file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            _recentProjectsService.RemoveRecentProject(project.ProjectPath ?? "");
            LoadRecentProjects();
            return;
        }
        
        try
        {
            var loadedProject = _projectService.LoadProject(project.ProjectPath);
            Project = loadedProject;
            StatusMessage = $"Loaded project: {project.DisplayName}";
            
            // Update recent list
            _recentProjectsService.AddRecentProject(project);
            LoadRecentProjects();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    public void SaveCurrentProjectToRecent()
    {
        if (string.IsNullOrEmpty(Project.ProjectFilePath)) return;
        
        var recent = new RecentProject
        {
            ProjectPath = Project.ProjectFilePath,
            ProjectName = Project.ProjectName,
            VesselName = Project.VesselName,
            ClientName = Project.ClientName,
            VerificationDate = Project.SurveyDate
        };
        
        _recentProjectsService.AddRecentProject(recent);
        LoadRecentProjects();
    }
    
    public void ClearRecentProjects()
    {
        _recentProjectsService.ClearRecentProjects();
        LoadRecentProjects();
    }
    
    #endregion
    
    #region Historical Database
    
    public void LoadHistoryRecords()
    {
        var records = _historyService.GetRecentRecords(50);
        HistoryRecords = new ObservableCollection<VerificationIndexEntry>(records);
    }
    
    public void SaveToHistory()
    {
        if (Results == null) return;
        
        try
        {
            var record = new VerificationRecord
            {
                ProjectName = Project.ProjectName,
                VesselName = Project.VesselName,
                ClientName = Project.ClientName,
                ProcessorName = Project.ProcessorName,
                VerificationDate = Project.SurveyDate ?? DateTime.Now,
                TransponderName = Project.TransponderName,
                TransponderSerial = Project.TransponderSerial,
                UsblSystem = Project.UsblModel,
                OverallPassed = Results.OverallPass,
                SpinTestPassed = Results.SpinTestPassed,
                TransitTestPassed = Results.TransitTestPassed,
                ToleranceMeters = Project.Tolerance,
                QualityScore = OverallQuality?.QualityScore ?? 0,
                SpinMeanOffset = Results.SpinStatistics?.Values.Average(s => 
                    Math.Sqrt(Math.Pow(s.AverageTransponderEasting - (Results.MeanPosition?.Easting ?? 0), 2) +
                              Math.Pow(s.AverageTransponderNorthing - (Results.MeanPosition?.Northing ?? 0), 2))) ?? 0,
                SpinPointCount = Project.Spin000.Observations.Count(o => !o.IsExcluded) + 
                                 Project.Spin090.Observations.Count(o => !o.IsExcluded) + 
                                 Project.Spin180.Observations.Count(o => !o.IsExcluded) + 
                                 Project.Spin270.Observations.Count(o => !o.IsExcluded),
                MeanEasting = Results.MeanPosition?.Easting ?? 0,
                MeanNorthing = Results.MeanPosition?.Northing ?? 0,
                MeanDepth = Results.MeanPosition?.Depth ?? 0
            };
            
            // Add per-heading results
            foreach (var spin in Results.SpinStatistics ?? new Dictionary<string, TestStatistics>())
            {
                record.HeadingResults.Add(new HeadingResult
                {
                    Heading = (int)Math.Round(spin.Value.AverageGyro),
                    ActualHeading = spin.Value.AverageGyro,
                    MeanOffset = spin.Value.TransducerOffset,
                    StdDev = spin.Value.StdDevEasting2Sigma / 2,
                    PointCount = spin.Value.PointCount,
                    Passed = spin.Value.TransducerOffset <= Project.Tolerance
                });
            }
            
            var id = _historyService.SaveRecord(record);
            LoadHistoryRecords();
            
            StatusMessage = $"Verification saved to history (ID: {id.Substring(0, 8)}...)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving to history: {ex.Message}";
        }
    }
    
    public void LoadFromHistory(string recordId)
    {
        var record = _historyService.LoadRecord(recordId);
        if (record == null)
        {
            MessageBox.Show("Record not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Populate project from record
        Project.ProjectName = record.ProjectName ?? "";
        Project.VesselName = record.VesselName ?? "";
        Project.ClientName = record.ClientName ?? "";
        Project.ProcessorName = record.ProcessorName ?? "";
        Project.SurveyDate = record.VerificationDate;
        Project.TransponderName = record.TransponderName ?? "";
        Project.TransponderSerial = record.TransponderSerial ?? "";
        Project.UsblModel = record.UsblSystem ?? "";
        Project.Tolerance = record.ToleranceMeters;
        
        OnPropertyChanged(nameof(Project));
        StatusMessage = $"Loaded historical record: {record.ProjectName}";
    }
    
    public void ExportHistory(string filePath)
    {
        try
        {
            _historyService.ExportDatabase(filePath);
            StatusMessage = "History exported successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }
    
    public void ImportHistory(string filePath)
    {
        try
        {
            int count = _historyService.ImportDatabase(filePath);
            LoadHistoryRecords();
            StatusMessage = $"Imported {count} records";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import error: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Export history database with file dialog
    /// </summary>
    private void ExportHistoryToFile()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Verification History",
            Filter = "JSON Files (*.json)|*.json",
            FileName = $"USBL_History_Backup_{DateTime.Now:yyyyMMdd}.json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            ExportHistory(dialog.FileName);
            MessageBox.Show($"History exported to:\n{dialog.FileName}", "Export Complete", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
    
    /// <summary>
    /// Import history database with file dialog
    /// </summary>
    private void ImportHistoryFromFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Verification History",
            Filter = "JSON Files (*.json)|*.json"
        };
        
        if (dialog.ShowDialog() == true)
        {
            var result = MessageBox.Show(
                "This will merge imported records with existing history.\n\nContinue?",
                "Import History", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ImportHistory(dialog.FileName);
            }
        }
    }
    
    #endregion
    
    #region Digital Signatures
    
    public void GenerateSigningKeys(string? password = null)
    {
        try
        {
            if (_signatureService.GenerateKeyPair(password))
            {
                StatusMessage = "Signing key pair generated successfully";
            }
            else
            {
                StatusMessage = "Failed to generate signing keys";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Key generation error: {ex.Message}";
        }
    }
    
    public bool HasSigningKey => _signatureService.HasSigningKey();
    
    public void SignCertificatePdf(string pdfPath, string? keyPassword = null)
    {
        if (Results == null || !Results.OverallPass)
        {
            StatusMessage = "Cannot sign: verification must pass";
            return;
        }
        
        try
        {
            var signingData = new CertificateSigningData
            {
                CertificateNumber = Results.CertificateNumber ?? Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                ProjectName = Project.ProjectName,
                VesselName = Project.VesselName,
                ClientName = Project.ClientName,
                VerificationDate = Project.SurveyDate ?? DateTime.Now,
                TransponderName = Project.TransponderName,
                OverallPassed = Results.OverallPass,
                MeanEasting = Results.MeanPosition?.Easting ?? 0,
                MeanNorthing = Results.MeanPosition?.Northing ?? 0,
                MeanDepth = Results.MeanPosition?.Depth ?? 0,
                ToleranceMeters = Project.Tolerance,
                QualityScore = OverallQuality?.QualityScore ?? 0
            };
            
            var result = _signatureService.SignPdfFile(pdfPath, signingData, keyPassword);
            
            if (result.Success)
            {
                StatusMessage = $"Certificate signed. Signature file: {result.MetadataPath}";
            }
            else
            {
                StatusMessage = $"Signing failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Signing error: {ex.Message}";
        }
    }
    
    #endregion
    
    #region Drag and Drop Support
    
    public void HandleDroppedFiles(string[] filePaths)
    {
        var classified = _recentProjectsService.ClassifyDroppedFiles(filePaths);
        
        if (classified.HasProjectFiles && classified.ProjectFiles.Count == 1)
        {
            // Single project file - load it
            var projectPath = classified.ProjectFiles[0].FilePath;
            try
            {
                var project = _projectService.LoadProject(projectPath);
                Project = project;
                StatusMessage = $"Loaded project: {project.ProjectName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading project: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return;
        }
        
        if (classified.HasSurveyFiles)
        {
            // Survey data files - auto-assign headings and import
            var assignments = _recentProjectsService.AutoAssignHeadings(
                classified.SurveyFiles.Select(f => f.FilePath).ToList());
            
            int imported = 0;
            foreach (var (path, heading) in assignments)
            {
                try
                {
                    // Track files for recent
                    _recentProjectsService.AddRecentFile(path, RecentFileType.SpinData);
                    imported++;
                }
                catch { }
            }
            
            StatusMessage = $"Prepared {imported} files for import. Use Batch Import to configure column mapping.";
            MessageBox.Show(
                $"Detected {classified.SurveyFiles.Count} survey files.\n\n" +
                "Please use 'Batch Import Spin Files' to configure column mapping and complete the import.",
                "Files Detected", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        if (classified.UnknownFiles.Count > 0)
        {
            StatusMessage = $"Warning: {classified.UnknownFiles.Count} file(s) with unknown type were skipped";
        }
    }
    
    /// <summary>
    /// Get last used directory for file dialogs
    /// </summary>
    public string? GetLastDirectory(string key = "default")
    {
        return _recentProjectsService.GetLastDirectory(key);
    }
    
    /// <summary>
    /// Set last used directory
    /// </summary>
    public void SetLastDirectory(string path, string key = "default")
    {
        _recentProjectsService.SetLastDirectory(path, key);
    }
    
    #endregion
}
