namespace FathomOS.Modules.TreeInclination.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using Microsoft.Win32;
using OxyPlot;
using FathomOS.Modules.TreeInclination.Models;
using FathomOS.Modules.TreeInclination.Services;
using FathomOS.Modules.TreeInclination.Export;
using FathomOS.Modules.TreeInclination.Views;

public class MainViewModel : ViewModelBase
{
    #region Fields

    private InclinationProject _project = new();
    private ModuleSettings _settings = new();
    private string _statusMessage = "Ready";
    private bool _isBusy = false;  // Explicitly false - overlay hidden on startup
    private bool _isDarkTheme = true;
    private int _selectedVisualizationTab;
    private NpdFileInfo? _selectedFile;
    private PlotModel? _depthProfileChart;
    private PlotModel? _planViewChart;
    private PlotModel? _step3PlanViewChart;  // Separate chart for Step 3
    private PlotModel? _vectorDiagramChart;
    private PlotModel? _selectedFileChart;
    private Model3DGroup? _structure3DModel;
    private double _depthExaggeration = 10.0;
    
    // Step navigation
    private int _currentStep = 1;
    
    // Approval fields (Step 7)
    private string _reviewerName = "";
    private string _reviewerInitials = "";
    private string _reviewerTitle = "Survey Engineer";
    private string _reviewerCredentials = "";
    private string _reviewComments = "";
    private bool _isApproved;
    private DateTime _reviewDate = DateTime.Now;
    
    // Professional signatory titles for certificate
    private static readonly List<string> _signatoryTitles = new()
    {
        "Survey Supervisor",
        "Senior Survey Engineer",
        "Survey Engineer",
        "Processing Engineer",
        "Quality Control Engineer",
        "Verification Engineer",
        "Calibration Engineer",
        "Operations Manager",
        "Project Manager",
        "Survey Manager",
        "Data Processing Specialist",
        "Technical Supervisor",
        "Field Engineer",
        "Senior Technician",
        "Hydrographic Surveyor",
        "Positioning Engineer"
    };

    #endregion

    #region Constructor

    public MainViewModel()
    {
        LoadSettings();
        InitializeCommands();
        InitializeDefaultProject();
        LoadRecentFiles();
    }

    #endregion

    #region Properties - Project Info

    public string ProjectName
    {
        get => _project.ProjectName;
        set { _project.ProjectName = value; OnPropertyChanged(); }
    }

    public string ClientName
    {
        get => _project.ClientName;
        set { _project.ClientName = value; OnPropertyChanged(); }
    }

    public string VesselName
    {
        get => _project.VesselName;
        set { _project.VesselName = value; OnPropertyChanged(); }
    }

    public string StructureName
    {
        get => _project.StructureName;
        set { _project.StructureName = value; OnPropertyChanged(); }
    }
    
    public string StructureType
    {
        get => _project.StructureType;
        set { _project.StructureType = value; OnPropertyChanged(); }
    }

    public string SurveyorName
    {
        get => _project.SurveyorName;
        set { _project.SurveyorName = value; OnPropertyChanged(); }
    }

    public DateTime SurveyDate
    {
        get => _project.SurveyDate;
        set { _project.SurveyDate = value; OnPropertyChanged(); }
    }

    #endregion

    #region Properties - Settings

    public ProcessingMode ProcessingMode
    {
        get => _project.Mode;
        set { _project.Mode = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCustomMode)); }
    }

    public bool IsCustomMode => _project.Mode == ProcessingMode.Custom;

    public LengthUnit DimensionUnit
    {
        get => _project.DimensionUnit;
        set { _project.DimensionUnit = value; OnPropertyChanged(); }
    }

    public DepthInputUnit DepthUnit
    {
        get => _project.DepthUnit;
        set
        {
            _project.DepthUnit = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPressureUnit));
            OnPropertyChanged(nameof(DepthUnitSymbol));
        }
    }

    public bool IsPressureUnit => UnitConversions.IsPressureUnit(_project.DepthUnit);
    public string DepthUnitSymbol => UnitConversions.GetUnitSymbol(_project.DepthUnit);

    public LengthUnit OutputUnit
    {
        get => _project.OutputUnit;
        set { _project.OutputUnit = value; OnPropertyChanged(); }
    }

    public GeometryType GeometryType
    {
        get => _project.Geometry;
        set { _project.Geometry = value; OnPropertyChanged(); }
    }

    public double WaterDensity
    {
        get => _project.WaterDensity;
        set { _project.WaterDensity = value; OnPropertyChanged(); }
    }

    public double AtmosphericPressure
    {
        get => _project.AtmosphericPressure;
        set { _project.AtmosphericPressure = value; OnPropertyChanged(); }
    }

    public bool ApplyTideCorrection
    {
        get => _project.ApplyTideCorrection;
        set { _project.ApplyTideCorrection = value; OnPropertyChanged(); }
    }
    
    public double TideValue
    {
        get => _project.TideValue;
        set { _project.TideValue = value; OnPropertyChanged(); ApplyTideCorrections(); }
    }
    
    public double DraftOffset
    {
        get => _project.DraftOffset;
        set { _project.DraftOffset = value; OnPropertyChanged(); ApplyTideCorrections(); }
    }
    
    public double MaxAllowedInclination
    {
        get => _project.Tolerances.MaxInclination;
        set { _project.Tolerances.MaxInclination = value; OnPropertyChanged(); }
    }
    
    public double MaxAllowedMisclosure
    {
        get => _project.Tolerances.MaxMisclosure;
        set { _project.Tolerances.MaxMisclosure = value; OnPropertyChanged(); }
    }

    // Enum sources for ComboBoxes
    public Array ProcessingModes => Enum.GetValues(typeof(ProcessingMode));
    public Array LengthUnits => Enum.GetValues(typeof(LengthUnit));
    public Array DepthUnits => Enum.GetValues(typeof(DepthInputUnit));
    public Array GeometryTypes => Enum.GetValues(typeof(GeometryType));
    public Array DistributionMethods => Enum.GetValues(typeof(MisclosureDistribution));

    #endregion

    #region Properties - Collections

    public ObservableCollection<NpdFileInfo> LoadedFiles => _project.LoadedFiles;
    public ObservableCollection<CornerMeasurement> Corners => _project.Corners;
    
    /// <summary>
    /// Returns corners excluding closure point - for coordinate entry grids.
    /// Closure point shares coordinates with corner 1 (it's a depth recheck).
    /// </summary>
    public IEnumerable<CornerMeasurement> CornersWithoutClosure => 
        _project.Corners.Where(c => !c.IsClosurePoint);
    public ObservableCollection<TideRecord> TideData => _project.TideData;
    public ObservableCollection<RecentFileItem> RecentFiles { get; } = new();

    #endregion

    #region Properties - Results

    public InclinationResult? Result => _project.Result;

    public string TotalInclinationDisplay => Result != null ? $"{Result.TotalInclination:F3}°" : "--";
    public string HeadingDisplay => Result != null ? $"{Result.InclinationHeading:F2}° ({Result.HeadingDescription})" : "--";
    public string MisclosureDisplay => Result != null ? $"{Result.Misclosure:F3} m" : "--";
    public string OutOfPlaneDisplay => Result != null ? $"{Result.OutOfPlane:F3} m" : "--";
    public string PitchDisplay => Result != null ? $"{Result.AveragePitch:F3}°" : "--";
    public string RollDisplay => Result != null ? $"{Result.AverageRoll:F3}°" : "--";

    public QualityStatus InclinationStatus => Result?.InclinationStatus ?? QualityStatus.NotCalculated;
    public QualityStatus MisclosureStatus => Result?.MisclosureStatus ?? QualityStatus.NotCalculated;
    public QualityStatus OutOfPlaneStatus => Result?.OutOfPlaneStatus ?? QualityStatus.NotCalculated;
    
    // Direct value properties for UI binding
    public double TotalInclination => Result?.TotalInclination ?? 0;
    public double InclinationBearing => Result?.TrueInclinationBearing ?? Result?.InclinationHeading ?? 0;
    public double PitchAngle => Result?.AveragePitch ?? 0;
    public double RollAngle => Result?.AverageRoll ?? 0;
    public double AverageDepth => Corners.Count > 0 ? Corners.Average(c => c.CorrectedDepth) : 0;
    public double DepthRange => Corners.Count > 0 ? Corners.Max(c => c.CorrectedDepth) - Corners.Min(c => c.CorrectedDepth) : 0;
    public double Misclosure => Result?.Misclosure ?? 0;
    
    // Status properties for UI
    public string ResultStatus => Result != null 
        ? (Result.InclinationStatus == QualityStatus.OK ? "Pass" : 
           Result.InclinationStatus == QualityStatus.Warning ? "Warning" : "Fail")
        : "";
    
    public string ResultStatusText => Result != null 
        ? (Result.InclinationStatus == QualityStatus.OK ? "WITHIN TOLERANCE" : 
           Result.InclinationStatus == QualityStatus.Warning ? "NEAR TOLERANCE" : "EXCEEDS TOLERANCE")
        : "";
    
    public string ResultStatusIcon => Result != null 
        ? (Result.InclinationStatus == QualityStatus.OK ? "CheckCircle" : 
           Result.InclinationStatus == QualityStatus.Warning ? "AlertCircle" : "CloseCircle")
        : "HelpCircle";
    
    public string HeadingDescription => Result?.HeadingDescription ?? "";

    #endregion

    #region Properties - UI State

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

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                _settings.IsDarkTheme = value;
                SaveSettings();
                OnPropertyChanged(nameof(ThemeIcon));
            }
        }
    }
    
    // Theme icon for toggle button
    public string ThemeIcon => IsDarkTheme ? "WeatherSunny" : "WeatherNight";

    public int SelectedVisualizationTab
    {
        get => _selectedVisualizationTab;
        set => SetProperty(ref _selectedVisualizationTab, value);
    }

    public bool HasLoadedFiles => LoadedFiles.Count > 0;
    public bool HasResult => Result != null;
    public bool HasRecentFiles => RecentFiles.Count > 0;
    public bool CanCalculate => Corners.Count(c => !c.IsClosurePoint) >= 3;
    
    // Structure type options for dropdown
    public string[] StructureTypes => new[] { "Tree", "Template", "Manifold", "PLET", "ILT", "Subsea Structure", "Foundation", "Other" };
    
    // Tide file name for display
    public string TideFileName => _project.TideFilePath != null ? Path.GetFileName(_project.TideFilePath) : "";
    
    // Processor name
    public string ProcessorName
    {
        get => _project.ProcessorName;
        set { _project.ProcessorName = value; OnPropertyChanged(); }
    }

    public NpdFileInfo? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetProperty(ref _selectedFile, value))
            {
                OnPropertyChanged(nameof(SelectedFileColumns));
                OnPropertyChanged(nameof(HasSelectedFile));
                UpdateSelectedFileChart();
            }
        }
    }

    public bool HasSelectedFile => _selectedFile != null;
    public List<string> SelectedFileColumns => _selectedFile?.AvailableColumns ?? new List<string>();

    public string SelectedFileDepthColumn
    {
        get => _selectedFile?.SelectedDepthColumn ?? string.Empty;
        set
        {
            if (_selectedFile != null && !string.IsNullOrEmpty(value) && value != _selectedFile.SelectedDepthColumn)
            {
                _selectedFile.SelectedDepthColumn = value;
                ReprocessFile(_selectedFile);
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Properties - Step Navigation

    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                OnPropertyChanged(nameof(IsStep1));
                OnPropertyChanged(nameof(IsStep2));
                OnPropertyChanged(nameof(IsStep3));
                OnPropertyChanged(nameof(IsStep4));
                OnPropertyChanged(nameof(IsStep5));
                OnPropertyChanged(nameof(IsStep6));
                OnPropertyChanged(nameof(IsStep7));
                OnPropertyChanged(nameof(CanGoBack));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(NextButtonText));
            }
        }
    }

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsStep5 => CurrentStep == 5;
    public bool IsStep6 => CurrentStep == 6;
    public bool IsStep7 => CurrentStep == 7;

    public bool CanGoBack => CurrentStep > 1;
    public bool CanGoNext => CurrentStep < 7;

    public string NextButtonText => CurrentStep switch
    {
        1 => "Import Data",
        2 => "Configure Corners",
        3 => "Apply Corrections",
        4 => "Calculate",
        5 => "Visualize",
        6 => "Export Report",
        _ => "Next"
    };

    // Approval properties (Step 7)
    public string ReviewerName
    {
        get => _reviewerName;
        set => SetProperty(ref _reviewerName, value);
    }

    public string ReviewerInitials
    {
        get => _reviewerInitials;
        set => SetProperty(ref _reviewerInitials, value);
    }
    
    public string ReviewerTitle
    {
        get => _reviewerTitle;
        set => SetProperty(ref _reviewerTitle, value);
    }
    
    public string ReviewerCredentials
    {
        get => _reviewerCredentials;
        set => SetProperty(ref _reviewerCredentials, value);
    }
    
    public List<string> SignatoryTitles => _signatoryTitles;

    public string ReviewComments
    {
        get => _reviewComments;
        set => SetProperty(ref _reviewComments, value);
    }

    public bool IsApproved
    {
        get => _isApproved;
        set => SetProperty(ref _isApproved, value);
    }

    public DateTime ReviewDate
    {
        get => _reviewDate;
        set => SetProperty(ref _reviewDate, value);
    }
    
    /// <summary>
    /// Gets the processing data dictionary for certificate generation
    /// </summary>
    public Dictionary<string, string> GetCertificateProcessingData()
    {
        var data = new Dictionary<string, string>();
        
        if (Result == null) return data;
        
        // Structure identification
        data["Structure Name"] = StructureName;
        data["Structure Type"] = StructureType;
        data["Corners Analyzed"] = $"{CornersWithoutClosure.Count()}";
        
        // Inclination results
        data["Total Inclination"] = $"{Result.TotalInclination:F4}°";
        data["Inclination Heading"] = $"{Result.InclinationHeading:F1}° (Rel) / {Result.TrueBearing:F1}° (True)";
        data["Pitch Component"] = $"{Result.Pitch:F4}°";
        data["Roll Component"] = $"{Result.Roll:F4}°";
        
        // Depth data
        if (Corners.Any())
        {
            var depths = CornersWithoutClosure.Select(c => c.CorrectedDepth).ToList();
            data["Depth Range"] = $"{depths.Min():F3} m to {depths.Max():F3} m";
            data["Max Depth Difference"] = $"{depths.Max() - depths.Min():F3} m";
        }
        
        // Quality metrics
        if (Result.Misclosure > 0)
            data["Misclosure"] = $"{Result.Misclosure:F4} m";
        
        data["Calculation Method"] = Corners.Count(c => !c.IsClosurePoint) > 4 
            ? "Best-Fit Plane (Least Squares)" 
            : "4-Point Diagonal Method";
        
        // Corrections applied
        var corrections = new List<string>();
        if (ApplyTideCorrection && (TideValue != 0 || !string.IsNullOrEmpty(TideFileName)))
            corrections.Add("Tide Correction");
        if (DraftOffset != 0)
            corrections.Add("Draft Offset");
        data["Corrections Applied"] = corrections.Count > 0 ? string.Join(", ", corrections) : "None";
        
        // QC Status
        data["QC Status"] = Result.QcStatus.ToString().ToUpper();
        data["Max Inclination Tolerance"] = $"{MaxAllowedInclination:F2}°";
        
        return data;
    }
    
    /// <summary>
    /// Gets the list of input files for certificate
    /// </summary>
    public List<string> GetInputFileList()
    {
        var files = new List<string>();
        foreach (var file in LoadedFiles)
        {
            files.Add(Path.GetFileName(file.FilePath));
        }
        if (!string.IsNullOrEmpty(TideFileName))
            files.Add(TideFileName);
        return files;
    }

    #endregion

    #region Properties - Charts and 3D

    public PlotModel? DepthProfileChart { get => _depthProfileChart; set => SetProperty(ref _depthProfileChart, value); }
    public PlotModel? PlanViewChart { get => _planViewChart; set => SetProperty(ref _planViewChart, value); }
    public PlotModel? Step3PlanViewChart { get => _step3PlanViewChart; set => SetProperty(ref _step3PlanViewChart, value); }
    public PlotModel? VectorDiagramChart { get => _vectorDiagramChart; set => SetProperty(ref _vectorDiagramChart, value); }
    public PlotModel? SelectedFileChart { get => _selectedFileChart; set => SetProperty(ref _selectedFileChart, value); }
    public Model3DGroup? Structure3DModel { get => _structure3DModel; set => SetProperty(ref _structure3DModel, value); }

    public double DepthExaggeration
    {
        get => _depthExaggeration;
        set { if (SetProperty(ref _depthExaggeration, value)) Update3DModel(); }
    }

    #endregion

    #region Properties - Custom Mode

    public CustomModeSettings CustomSettings => _project.CustomSettings;

    public bool DistributeMisclosure
    {
        get => CustomSettings.DistributeMisclosure;
        set { CustomSettings.DistributeMisclosure = value; OnPropertyChanged(); }
    }

    public MisclosureDistribution DistributionMethod
    {
        get => CustomSettings.DistributionMethod;
        set { CustomSettings.DistributionMethod = value; OnPropertyChanged(); }
    }

    public bool RemoveOutliers
    {
        get => CustomSettings.RemoveOutliers;
        set { CustomSettings.RemoveOutliers = value; OnPropertyChanged(); }
    }

    public double OutlierSigma
    {
        get => CustomSettings.OutlierSigma;
        set { CustomSettings.OutlierSigma = value; OnPropertyChanged(); }
    }

    public double MaxInclinationTolerance
    {
        get => _project.Tolerances.MaxInclination;
        set { _project.Tolerances.MaxInclination = value; OnPropertyChanged(); }
    }

    public double MaxMisclosureTolerance
    {
        get => _project.Tolerances.MaxMisclosure;
        set { _project.Tolerances.MaxMisclosure = value; OnPropertyChanged(); }
    }

    public double MaxOutOfPlaneTolerance
    {
        get => _project.Tolerances.MaxOutOfPlane;
        set { _project.Tolerances.MaxOutOfPlane = value; OnPropertyChanged(); }
    }

    #endregion

    #region Commands

    public ICommand LoadFilesCommand { get; private set; } = null!;
    public ICommand ClearFilesCommand { get; private set; } = null!;
    public ICommand AutoDetectCornersCommand { get; private set; } = null!;
    public ICommand LoadTideFileCommand { get; private set; } = null!;
    public ICommand LoadPositionFixCommand { get; private set; } = null!;
    public ICommand CalculateCommand { get; private set; } = null!;
    public ICommand ExportExcelCommand { get; private set; } = null!;
    public ICommand ExportPdfCommand { get; private set; } = null!;
    public ICommand ExportDxfCommand { get; private set; } = null!;
    public ICommand GenerateCertificateCommand { get; private set; } = null!;
    public ICommand SaveChartsCommand { get; private set; } = null!;
    public ICommand SaveProjectCommand { get; private set; } = null!;
    public ICommand LoadProjectCommand { get; private set; } = null!;
    public ICommand OpenRecentCommand { get; private set; } = null!;
    public ICommand NewProjectCommand { get; private set; } = null!;
    public ICommand ShowHelpCommand { get; private set; } = null!;
    public ICommand ToggleThemeCommand { get; private set; } = null!;
    public ICommand ResetCameraCommand { get; private set; } = null!;
    public ICommand SetTopViewCommand { get; private set; } = null!;
    public ICommand SetSideViewCommand { get; private set; } = null!;
    public ICommand GenerateCoordsCommand { get; private set; } = null!;
    public ICommand ConvertCoordsCommand { get; private set; } = null!;
    public ICommand SetStructureHeadingCommand { get; private set; } = null!;
    public ICommand ReprocessSelectedCommand { get; private set; } = null!;
    
    // Step navigation commands
    public ICommand NextStepCommand { get; private set; } = null!;
    public ICommand PreviousStepCommand { get; private set; } = null!;
    public ICommand GoToStepCommand { get; private set; } = null!;
    public ICommand ExportChartsCommand { get; private set; } = null!;
    public ICommand AutoDetectCommand { get; private set; } = null!;
    public ICommand SetHeadingFromGyroCommand { get; private set; } = null!;
    public ICommand RefreshChartsCommand { get; private set; } = null!;
    public ICommand ClearTideCommand { get; private set; } = null!;
    public ICommand ApplyCorrectionsCommand { get; private set; } = null!;

    // Events for viewport control
    public event Action? OnCameraReset;
    public event Action? OnSetTopView;
    public event Action? OnSetSideView;

    // Structure dimensions for coordinate generation
    private double _structureWidth = 5.0;
    private double _structureLength = 5.0;
    
    public double StructureWidth
    {
        get => _structureWidth;
        set => SetProperty(ref _structureWidth, value);
    }
    
    public double StructureLength
    {
        get => _structureLength;
        set => SetProperty(ref _structureLength, value);
    }
    
    // Coordinate input mode and unit
    public CoordinateInputMode CoordinateInputMode
    {
        get => _project.CoordinateInputMode;
        set
        {
            if (_project.CoordinateInputMode != value)
            {
                _project.CoordinateInputMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAbsoluteCoordinateMode));
                OnPropertyChanged(nameof(IsRelativeOffsetMode));
            }
        }
    }
    
    public bool IsAbsoluteCoordinateMode
    {
        get => CoordinateInputMode == CoordinateInputMode.AbsoluteCoordinates;
        set => CoordinateInputMode = value ? CoordinateInputMode.AbsoluteCoordinates : CoordinateInputMode.RelativeOffset;
    }
    
    public bool IsRelativeOffsetMode
    {
        get => CoordinateInputMode == CoordinateInputMode.RelativeOffset;
        set => CoordinateInputMode = value ? CoordinateInputMode.RelativeOffset : CoordinateInputMode.AbsoluteCoordinates;
    }
    
    public LengthUnit CoordinateUnit
    {
        get => _project.CoordinateUnit;
        set
        {
            if (_project.CoordinateUnit != value)
            {
                _project.CoordinateUnit = value;
                OnPropertyChanged();
            }
        }
    }
    
    // Structure orientation (true bearing)
    public double? StructureHeading
    {
        get => _project.StructureHeading;
        set
        {
            if (_project.StructureHeading != value)
            {
                _project.StructureHeading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StructureHeadingDisplay));
            }
        }
    }
    
    public string StructureHeadingDisplay => StructureHeading.HasValue 
        ? $"{StructureHeading:F1}°" 
        : "Not set";
    
    public bool UseGyroHeading
    {
        get => _project.UseGyroHeading;
        set
        {
            if (_project.UseGyroHeading != value)
            {
                _project.UseGyroHeading = value;
                OnPropertyChanged();
            }
        }
    }
    
    // Average heading from all corners (computed)
    public double? AverageCornerHeading
    {
        get
        {
            var headings = Corners.Where(c => c.Heading.HasValue && !c.IsClosurePoint)
                                  .Select(c => c.Heading!.Value).ToList();
            if (headings.Count == 0) return null;
            
            // Circular mean
            double sumSin = headings.Sum(h => Math.Sin(h * Math.PI / 180));
            double sumCos = headings.Sum(h => Math.Cos(h * Math.PI / 180));
            double mean = Math.Atan2(sumSin, sumCos) * 180 / Math.PI;
            if (mean < 0) mean += 360;
            return mean;
        }
    }
    
    public string AverageCornerHeadingDisplay => AverageCornerHeading.HasValue 
        ? $"{AverageCornerHeading:F1}°" 
        : "-";
    
    // True bearing results
    public string TrueBearingDisplay => Result?.TrueInclinationBearing.HasValue == true 
        ? $"{Result.TrueInclinationBearing:F1}° ({Result.TrueBearingDescription})" 
        : "--";
    
    public string TiltDescriptionDisplay => Result?.GetTiltDescription() ?? "No calculation performed";
    
    public IEnumerable<CoordinateInputMode> CoordinateInputModes => Enum.GetValues<CoordinateInputMode>();

    private void InitializeCommands()
    {
        LoadFilesCommand = new RelayCommand(_ => LoadNpdFilesAsync(null));
        ClearFilesCommand = new RelayCommand(_ => ClearFiles());
        AutoDetectCornersCommand = new RelayCommand(_ => AutoDetectCorners());
        LoadTideFileCommand = new AsyncRelayCommand(async p => await LoadTideFileAsync(p));
        LoadPositionFixCommand = new AsyncRelayCommand(async p => await LoadPositionFixAsync(p));
        CalculateCommand = new RelayCommand(_ => Calculate(), _ => CanCalculate);
        ExportExcelCommand = new AsyncRelayCommand(async p => await ExportToExcelAsync(p), () => HasResult);
        ExportPdfCommand = new AsyncRelayCommand(async p => await ExportToPdfAsync(p), () => HasResult);
        ExportDxfCommand = new AsyncRelayCommand(async p => await ExportToDxfAsync(p), () => HasResult);
        GenerateCertificateCommand = new RelayCommand(_ => GenerateCertificate(), _ => HasResult && IsApproved);
        SaveChartsCommand = new RelayCommand(_ => SaveCharts(), _ => HasResult);
        SaveProjectCommand = new RelayCommand(_ => SaveProject());
        LoadProjectCommand = new AsyncRelayCommand(async p => await LoadProjectAsync(p));
        OpenRecentCommand = new RelayCommand(p => OpenRecentFile(p));
        NewProjectCommand = new RelayCommand(_ => NewProject());
        ShowHelpCommand = new RelayCommand(_ => ShowHelp());
        ToggleThemeCommand = new RelayCommand(_ => IsDarkTheme = !IsDarkTheme);
        ResetCameraCommand = new RelayCommand(_ => OnCameraReset?.Invoke());
        SetTopViewCommand = new RelayCommand(_ => OnSetTopView?.Invoke());
        SetSideViewCommand = new RelayCommand(_ => OnSetSideView?.Invoke());
        GenerateCoordsCommand = new RelayCommand(_ => GenerateRelativeCoordinates(), _ => Corners.Count >= 3);
        ConvertCoordsCommand = new RelayCommand(_ => ConvertAbsoluteToRelative(), _ => Corners.Count >= 2);
        SetStructureHeadingCommand = new RelayCommand(_ => SetStructureHeadingFromGyro(), _ => AverageCornerHeading.HasValue);
        ReprocessSelectedCommand = new RelayCommand(_ => { if (SelectedFile != null) ReprocessFile(SelectedFile); }, _ => SelectedFile != null);
        
        // Step navigation commands
        NextStepCommand = new RelayCommand(_ => GoToNextStep(), _ => CanGoNext);
        PreviousStepCommand = new RelayCommand(_ => GoToPreviousStep(), _ => CanGoBack);
        GoToStepCommand = new RelayCommand(p => GoToStep(p));
        ExportChartsCommand = new RelayCommand(_ => SaveCharts(), _ => HasResult);
        AutoDetectCommand = new RelayCommand(_ => AutoDetectCorners());
        SetHeadingFromGyroCommand = new RelayCommand(_ => SetStructureHeadingFromGyro(), _ => AverageCornerHeading.HasValue);
        RefreshChartsCommand = new RelayCommand(_ => UpdateCharts(), _ => Corners.Count > 0);
        ClearTideCommand = new RelayCommand(_ => ClearTideData());
        ApplyCorrectionsCommand = new RelayCommand(_ => ApplyTideCorrections(), _ => Corners.Count > 0);
    }
    
    private void GoToNextStep()
    {
        if (CurrentStep < 7)
        {
            // Validate current step before proceeding
            if (ValidateCurrentStep())
            {
                CurrentStep++;
                UpdateStatusForStep();
            }
        }
    }
    
    private void GoToPreviousStep()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            UpdateStatusForStep();
        }
    }
    
    private void GoToStep(object? parameter)
    {
        if (parameter is int step && step >= 1 && step <= 7)
        {
            CurrentStep = step;
            UpdateStatusForStep();
        }
    }
    
    private bool ValidateCurrentStep()
    {
        return CurrentStep switch
        {
            1 => true, // Project info - always valid
            2 => LoadedFiles.Count > 0 || ShowValidationMessage("Please load at least one NPD file."),
            3 => Corners.Count >= 3 || ShowValidationMessage("Please configure at least 3 corner positions."),
            4 => true, // Corrections - optional
            5 => HasResult || (CanCalculate && AutoCalculate()), // Auto-calculate if possible
            6 => HasResult || ShowValidationMessage("Please calculate results first."),
            _ => true
        };
    }
    
    private bool ShowValidationMessage(string message)
    {
        StatusMessage = message;
        MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }
    
    private bool AutoCalculate()
    {
        if (CanCalculate)
        {
            Calculate();
            return HasResult;
        }
        return false;
    }
    
    private void UpdateStatusForStep()
    {
        StatusMessage = CurrentStep switch
        {
            1 => "Enter project information and structure settings",
            2 => $"Loaded {LoadedFiles.Count} files with {Corners.Count} corners",
            3 => "Configure corner coordinates and structure heading",
            4 => "Apply tide and draft corrections",
            5 => HasResult ? $"Inclination: {TotalInclination:F3}° at {InclinationBearing:F1}°" : "Click Calculate to process",
            6 => "View charts and 3D visualization",
            7 => "Review and export final report",
            _ => "Ready"
        };
    }

    #endregion

    #region Methods - File Operations

    private void LoadNpdFilesAsync(object? parameter)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select NPD Files",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "Loading NPD files...";

        try
        {
            foreach (var filePath in dialog.FileNames)
            {
                // Show import dialog for column mapping
                var importDialog = new Views.FileImportDialog(
                    filePath, 
                    DepthUnit, 
                    WaterDensity, 
                    IsPressureUnit ? AtmosphericPressure : 0);
                    
                importDialog.Owner = Application.Current.MainWindow;
                
                if (importDialog.ShowDialog() == true && importDialog.Result != null)
                {
                    var info = importDialog.Result;
                    info.CornerName = importDialog.CornerName;
                    info.IsClosurePoint = importDialog.IsClosurePoint;
                    
                    LoadedFiles.Add(info);
                    
                    // Create corner from loaded file
                    CreateCornerFromFile(info);
                }
            }

            // After loading all files, normalize positions to relative coordinates
            NormalizeCornerPositions();

            OnPropertyChanged(nameof(HasLoadedFiles));
            OnPropertyChanged(nameof(CanCalculate));
            OnPropertyChanged(nameof(Corners));
            OnPropertyChanged(nameof(CornersWithoutClosure));
            StatusMessage = $"Loaded {LoadedFiles.Count} file(s)";
            
            UpdateCharts();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error loading files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CreateCornerFromFile(NpdFileInfo fileInfo)
    {
        // Check if corner already exists
        var existing = Corners.FirstOrDefault(c => c.Name == fileInfo.CornerName);
        if (existing != null)
        {
            // Update existing corner
            existing.RawDepthAverage = fileInfo.RawDepthAverage;
            existing.SourceFile = fileInfo;
            
            // Update position if available from NPD
            if (fileInfo.HasPositionData)
            {
                existing.X = fileInfo.AverageEasting ?? existing.X;
                existing.Y = fileInfo.AverageNorthing ?? existing.Y;
            }
            
            // Update heading if available
            if (fileInfo.HasHeadingData)
            {
                existing.Heading = fileInfo.AverageHeading;
            }
            
            // Initialize corrected depth = raw depth (tide/draft applied later)
            existing.CorrectedDepth = existing.RawDepthAverage;
            return;
        }
        
        // Create new corner - use 1-based indexing to match standard convention
        int cornerIndex = Corners.Count + 1;  // 1-based: first corner is 1, not 0
        
        var corner = new CornerMeasurement
        {
            Index = cornerIndex,
            Name = fileInfo.CornerName,
            IsClosurePoint = fileInfo.IsClosurePoint,
            RawDepthAverage = fileInfo.RawDepthAverage,
            CorrectedDepth = fileInfo.RawDepthAverage,  // Initialize to raw, corrections applied in Step 4
            SourceFile = fileInfo,
            TideCorrection = 0  // No tide correction yet
        };
        
        // Set position from NPD file if available
        if (fileInfo.HasPositionData)
        {
            corner.X = fileInfo.AverageEasting ?? 0;
            corner.Y = fileInfo.AverageNorthing ?? 0;
        }
        
        // Set heading from NPD file if available
        if (fileInfo.HasHeadingData)
        {
            corner.Heading = fileInfo.AverageHeading;
        }
        
        // Note: Corners is the same as _project.Corners (getter returns _project.Corners)
        // So we only need to add once
        Corners.Add(corner);
    }

    /// <summary>
    /// Normalize corner positions from absolute UTM to relative coordinates.
    /// If NPD files contain position data, this converts them to offsets from first corner.
    /// </summary>
    private void NormalizeCornerPositions()
    {
        var cornersWithPosition = Corners.Where(c => !c.IsClosurePoint && c.SourceFile?.HasPositionData == true).ToList();
        
        if (cornersWithPosition.Count < 2) return;
        
        // Get the first corner's position as origin
        var first = cornersWithPosition.FirstOrDefault();
        if (first == null) return;
        
        double originE = first.X;
        double originN = first.Y;
        
        // Check if positions are all large UTM values (> 10000)
        bool isUtm = cornersWithPosition.All(c => Math.Abs(c.X) > 10000 || Math.Abs(c.Y) > 10000);
        
        if (isUtm)
        {
            // Convert to relative coordinates
            foreach (var corner in cornersWithPosition)
            {
                corner.X = corner.X - originE;
                corner.Y = corner.Y - originN;
            }
            
            StatusMessage = $"Converted positions to relative coordinates (origin at {first.Name})";
        }
        
        // Check if all positions are still too close together (within 1m)
        double maxDist = 0;
        for (int i = 0; i < cornersWithPosition.Count; i++)
        {
            for (int j = i + 1; j < cornersWithPosition.Count; j++)
            {
                double dx = cornersWithPosition[j].X - cornersWithPosition[i].X;
                double dy = cornersWithPosition[j].Y - cornersWithPosition[i].Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                maxDist = Math.Max(maxDist, dist);
            }
        }
        
        if (maxDist < 1.0)
        {
            // Positions are all very close - NPD probably recorded vessel position, not corner positions
            // Reset to zero and prompt user
            foreach (var corner in Corners.Where(c => !c.IsClosurePoint))
            {
                corner.X = 0;
                corner.Y = 0;
            }
            
            MessageBox.Show(
                "The Easting/Northing values in the NPD files appear to be vessel tracking positions,\n" +
                "not structure corner positions (all positions within 1m of each other).\n\n" +
                "Please either:\n" +
                "1. Go to the Corners tab and enter X,Y coordinates manually\n" +
                "2. Use 'Generate Coords' button with structure dimensions\n" +
                "3. Load a position fix file with actual corner coordinates",
                "Position Data Notice", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            // Positions appear to be valid corner positions - auto-detect structure dimensions
            AutoDetectStructureDimensions(cornersWithPosition);
        }
    }
    
    /// <summary>
    /// Auto-detect structure dimensions from NPD corner positions.
    /// Calculates approximate width and length from the relative positions.
    /// </summary>
    private void AutoDetectStructureDimensions(List<CornerMeasurement> cornersWithPosition)
    {
        if (cornersWithPosition.Count < 3) return;
        
        // Calculate bounding box of all corner positions
        double minX = cornersWithPosition.Min(c => c.X);
        double maxX = cornersWithPosition.Max(c => c.X);
        double minY = cornersWithPosition.Min(c => c.Y);
        double maxY = cornersWithPosition.Max(c => c.Y);
        
        double detectedWidth = maxX - minX;
        double detectedLength = maxY - minY;
        
        // Only update if reasonable values detected (between 1m and 100m)
        if (detectedWidth >= 1 && detectedWidth <= 100 && detectedLength >= 1 && detectedLength <= 100)
        {
            StructureWidth = Math.Round(detectedWidth, 2);
            StructureLength = Math.Round(detectedLength, 2);
            
            StatusMessage = $"Auto-detected structure: {StructureWidth:F2}m × {StructureLength:F2}m from NPD positions";
        }
    }

    private void ClearFiles()
    {
        // Note: Corners is the same as _project.Corners, LoadedFiles is same as _project.LoadedFiles
        LoadedFiles.Clear();
        Corners.Clear();
        _project.Result = null;
        DepthProfileChart = null;
        PlanViewChart = null;
        Step3PlanViewChart = null;
        VectorDiagramChart = null;
        SelectedFileChart = null;
        Structure3DModel = null;

        OnPropertyChanged(nameof(HasLoadedFiles));
        OnPropertyChanged(nameof(HasResult));
        OnPropertyChanged(nameof(CanCalculate));
        OnPropertyChanged(nameof(CornersWithoutClosure));
        RefreshResultProperties();
        StatusMessage = "Cleared all files";
    }

    private void AutoDetectCorners()
    {
        if (LoadedFiles.Count == 0) return;

        // Note: Corners is the same as _project.Corners (getter returns _project.Corners)
        Corners.Clear();

        var sortedFiles = LoadedFiles.OrderBy(f => f.FileName).ToList();
        int cornerIndex = 1;  // 1-based indexing

        foreach (var file in sortedFiles)
        {
            file.CornerIndex = cornerIndex;
            file.CornerName = $"DQ{cornerIndex}";
            file.IsClosurePoint = false;

            var corner = new CornerMeasurement
            {
                Index = cornerIndex,
                Name = file.CornerName,
                RawDepthAverage = file.RawDepthAverage,
                CorrectedDepth = file.RawDepthAverage,
                SourceFile = file
            };
            
            // Use position from NPD if available
            if (file.HasPositionData)
            {
                corner.X = file.AverageEasting ?? 0;
                corner.Y = file.AverageNorthing ?? 0;
            }
            
            Corners.Add(corner);

            cornerIndex++;
        }

        // Mark last file as potential closure if it matches first file pattern
        if (sortedFiles.Count > 4 && sortedFiles[^1].FileName.Contains("CLS", StringComparison.OrdinalIgnoreCase))
        {
            sortedFiles[^1].IsClosurePoint = true;
            Corners[^1].IsClosurePoint = true;
        }

        OnPropertyChanged(nameof(CanCalculate));
        OnPropertyChanged(nameof(CornersWithoutClosure));
        UpdateCharts();
        StatusMessage = $"Detected {Corners.Count} corners";
    }

    /// <summary>
    /// Generate relative coordinates for corners based on structure geometry and dimensions.
    /// This assigns local X,Y positions assuming a standard corner layout.
    /// </summary>
    private void GenerateRelativeCoordinates()
    {
        var corners = Corners.Where(c => !c.IsClosurePoint).OrderBy(c => c.Index).ToList();
        int n = corners.Count;
        
        if (n < 3)
        {
            MessageBox.Show("At least 3 corners required to generate coordinates.", "Cannot Generate", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Confirm with user
        var result = MessageBox.Show(
            $"This will assign relative X,Y coordinates to {n} corners based on:\n\n" +
            $"Geometry: {GeometryType}\n" +
            $"Width: {StructureWidth:F2} m\n" +
            $"Length: {StructureLength:F2} m\n\n" +
            "Corner layout (viewed from above):\n" +
            "  1 -------- 2\n" +
            "  |          |\n" +
            "  |          |\n" +
            "  4 -------- 3\n\n" +
            "Do you want to continue?",
            "Generate Coordinates", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes) return;
        
        double w = StructureWidth;
        double l = StructureLength;
        
        // Standard 4-corner layout based on position in list (not corner.Index)
        // Position 0 = Corner 1: (0, 0) - Origin, top-left
        // Position 1 = Corner 2: (w, 0) - Top-right
        // Position 2 = Corner 3: (w, l) - Bottom-right  
        // Position 3 = Corner 4: (0, l) - Bottom-left
        
        var coordTemplates = new List<(double X, double Y)>
        {
            (0, 0),      // Position 0 (Corner 1)
            (w, 0),      // Position 1 (Corner 2)
            (w, l),      // Position 2 (Corner 3)
            (0, l),      // Position 3 (Corner 4)
            (w/2, l/2),  // Position 4 (Center for 5-point)
            (w/3, l/3)   // Position 5 (Offset center for 6-point)
        };
        
        // Apply coordinates based on position in sorted list
        for (int i = 0; i < corners.Count; i++)
        {
            if (i < coordTemplates.Count)
            {
                corners[i].X = coordTemplates[i].X;
                corners[i].Y = coordTemplates[i].Y;
            }
            else
            {
                // For additional corners beyond 6, distribute evenly around perimeter
                double angle = 2 * Math.PI * i / n;
                corners[i].X = w / 2 + (w / 2) * Math.Cos(angle);
                corners[i].Y = l / 2 + (l / 2) * Math.Sin(angle);
            }
        }
        
        OnPropertyChanged(nameof(Corners));
        OnPropertyChanged(nameof(CornersWithoutClosure));
        UpdateCharts();
        StatusMessage = $"Generated coordinates for {n} corners ({StructureWidth}m x {StructureLength}m)";
    }

    /// <summary>
    /// Convert absolute Easting/Northing coordinates to relative X,Y offsets.
    /// Uses Corner 1 as the origin (0,0).
    /// </summary>
    private void ConvertAbsoluteToRelative()
    {
        var corners = Corners.Where(c => !c.IsClosurePoint).OrderBy(c => c.Index).ToList();
        
        if (corners.Count < 2)
        {
            MessageBox.Show("At least 2 corners required.", "Cannot Convert", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Check if absolute coordinates are entered
        var cornersWithAbsolute = corners.Where(c => c.AbsoluteEasting.HasValue && c.AbsoluteNorthing.HasValue).ToList();
        
        if (cornersWithAbsolute.Count < 2)
        {
            MessageBox.Show("Please enter Easting and Northing values for at least 2 corners.", "Missing Data", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Get origin (first corner with absolute coordinates)
        var origin = cornersWithAbsolute.First();
        double originE = origin.AbsoluteEasting!.Value;
        double originN = origin.AbsoluteNorthing!.Value;
        
        // Convert based on unit
        double unitFactor = UnitConversions.LengthToMeters(1.0, CoordinateUnit);
        
        // Convert all corners
        foreach (var corner in cornersWithAbsolute)
        {
            double deltaE = (corner.AbsoluteEasting!.Value - originE) * unitFactor;
            double deltaN = (corner.AbsoluteNorthing!.Value - originN) * unitFactor;
            
            corner.X = deltaE;
            corner.Y = deltaN;
        }
        
        // Set corners without absolute coords to 0,0 (they'll need manual entry)
        foreach (var corner in corners.Except(cornersWithAbsolute))
        {
            corner.X = 0;
            corner.Y = 0;
        }
        
        UpdateCharts();
        
        string unitSymbol = UnitConversions.GetUnitSymbol(CoordinateUnit);
        StatusMessage = $"Converted {cornersWithAbsolute.Count} absolute coordinates to relative (origin: {origin.Name}, unit: {unitSymbol})";
        
        OnPropertyChanged(nameof(Corners));
        OnPropertyChanged(nameof(CornersWithoutClosure));
    }

    /// <summary>
    /// Set the structure heading from the average gyro heading of all corners.
    /// </summary>
    private void SetStructureHeadingFromGyro()
    {
        if (!AverageCornerHeading.HasValue)
        {
            MessageBox.Show("No gyro heading data available from NPD files.", "No Data", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        double avgHeading = AverageCornerHeading.Value;
        
        var result = MessageBox.Show(
            $"Set structure heading to {avgHeading:F1}° (from NPD gyro data)?\n\n" +
            "This will be used to calculate the true bearing of the inclination direction.\n\n" +
            "Note: This assumes the ROV/vessel was aligned with the structure during logging.",
            "Set Structure Heading", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            StructureHeading = avgHeading;
            StatusMessage = $"Structure heading set to {avgHeading:F1}° from gyro data";
        }
    }

    private async Task LoadTideFileAsync(object? parameter)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Tide File",
            Filter = "Tide Files (*.txt;*.csv;*.tid)|*.txt;*.csv;*.tid|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "Loading tide file...";

        try
        {
            var parser = new TideFileParser();
            var (records, error) = await Task.Run(() => parser.Parse(dialog.FileName));

            if (error != null)
            {
                MessageBox.Show(error, "Tide File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusMessage = error;
                return;
            }

            TideData.Clear();
            foreach (var record in records)
                TideData.Add(record);

            _project.TideFilePath = dialog.FileName;
            ApplyTideCorrection = true;

            // Apply tide corrections to corners
            ApplyTideCorrections();

            StatusMessage = $"Loaded {records.Count} tide records";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyTideCorrections()
    {
        var parser = new TideFileParser();

        foreach (var corner in Corners)
        {
            double tideCorrection = 0;
            
            // Apply tide from file if available, otherwise use manual value
            if (TideData.Count > 0 && corner.SourceFile?.LogStartTime != null)
            {
                tideCorrection = parser.InterpolateTide(TideData.ToList(), corner.SourceFile.LogStartTime.Value) ?? 0;
            }
            else if (_project.ApplyTideCorrection)
            {
                tideCorrection = _project.TideValue;
            }
            
            corner.TideCorrection = tideCorrection;
            
            // Apply corrections: CorrectedDepth = RawDepth + TideCorrection + DraftOffset
            // Convention: 
            // - Positive tide = high tide = water surface higher = actual seabed is deeper below chart datum
            // - Positive draft offset = sensor below waterline = add to depth
            // Formula: ReducedDepth = ObservedDepth + TideHeight + DraftOffset
            corner.CorrectedDepth = corner.RawDepthAverage + tideCorrection + _project.DraftOffset;
        }

        OnPropertyChanged(nameof(Corners));
        OnPropertyChanged(nameof(CornersWithoutClosure));
        UpdateCharts();
        StatusMessage = $"Applied corrections to {Corners.Count} corners";
    }

    private void ClearTideData()
    {
        TideData.Clear();
        _project.TideFilePath = null;
        _project.TideValue = 0;
        
        // Reset tide corrections on all corners
        foreach (var corner in Corners)
        {
            corner.TideCorrection = 0;
            corner.CorrectedDepth = corner.RawDepthAverage + _project.DraftOffset;
        }
        
        OnPropertyChanged(nameof(TideFileName));
        OnPropertyChanged(nameof(Corners));
        OnPropertyChanged(nameof(CornersWithoutClosure));
        UpdateCharts();
        StatusMessage = "Tide data cleared";
    }

    private async Task LoadPositionFixAsync(object? parameter)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Position Fix File",
            Filter = "Position Files (*.txt;*.csv;*.npd)|*.txt;*.csv;*.npd|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "Loading position fixes...";

        try
        {
            var parser = new PositionFixParser();
            var (fixes, error) = await Task.Run(() => parser.Parse(dialog.FileName));

            if (error != null || fixes.Count == 0)
            {
                MessageBox.Show(error ?? "No position fixes found", "Position File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Match fixes to corners by name or order
            for (int i = 0; i < Math.Min(fixes.Count, Corners.Count); i++)
            {
                var fix = fixes[i];
                var corner = Corners.FirstOrDefault(c => c.Name == fix.PointName) ?? Corners[i];

                corner.X = fix.Easting - fixes[0].Easting;
                corner.Y = fix.Northing - fixes[0].Northing;
            }

            _project.CoordSource = CoordinateSource.PositionFixFile;
            UpdateCharts();
            StatusMessage = $"Applied {fixes.Count} position fixes";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ReprocessFile(NpdFileInfo file)
    {
        try
        {
            var parser = new NpdDepthParser();
            
            // Create column selection from existing file info
            var selection = new ColumnSelection
            {
                DepthColumn = file.SelectedDepthColumn,
                HasDateTimeSplit = false // Preserve original settings if available
            };
            
            var newInfo = parser.Parse(file.FilePath, selection, DepthUnit, WaterDensity, IsPressureUnit ? AtmosphericPressure : 0);

            // Preserve user settings
            newInfo.SelectedDepthColumn = file.SelectedDepthColumn;
            newInfo.CornerIndex = file.CornerIndex;
            newInfo.CornerName = file.CornerName;
            newInfo.IsClosurePoint = file.IsClosurePoint;

            int index = LoadedFiles.IndexOf(file);
            if (index >= 0)
            {
                LoadedFiles[index] = newInfo;

                var corner = Corners.FirstOrDefault(c => c.SourceFile == file);
                if (corner != null)
                {
                    corner.SourceFile = newInfo;
                    corner.RawDepthAverage = newInfo.RawDepthAverage;
                    // Apply tide and draft corrections consistently
                    double tideCorrection = corner.TideCorrection ?? 0;
                    corner.CorrectedDepth = newInfo.RawDepthAverage + tideCorrection + _project.DraftOffset;
                }
            }

            UpdateSelectedFileChart();
            UpdateCharts();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reprocess error: {ex.Message}";
        }
    }

    #endregion

    #region Methods - Calculation

    private void Calculate()
    {
        if (!CanCalculate)
        {
            MessageBox.Show("Please load files and ensure corner coordinates are set.", "Cannot Calculate", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        StatusMessage = "Calculating...";

        try
        {
            // Ensure corrected depths are set - apply tide and draft corrections
            foreach (var corner in Corners)
            {
                double tideCorrection = corner.TideCorrection ?? 0;
                // CorrectedDepth = RawDepth + TideCorrection + DraftOffset
                corner.CorrectedDepth = corner.RawDepthAverage + tideCorrection + _project.DraftOffset;
            }

            var calculator = new InclinationCalculator(_project);
            var result = calculator.Calculate();

            OnPropertyChanged(nameof(Result));
            OnPropertyChanged(nameof(HasResult));
            RefreshResultProperties();
            UpdateCharts();
            Update3DModel();

            StatusMessage = $"Complete - Inclination: {result.TotalInclination:F3}°";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Calculation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshResultProperties()
    {
        OnPropertyChanged(nameof(TotalInclinationDisplay));
        OnPropertyChanged(nameof(HeadingDisplay));
        OnPropertyChanged(nameof(TrueBearingDisplay));
        OnPropertyChanged(nameof(TiltDescriptionDisplay));
        OnPropertyChanged(nameof(MisclosureDisplay));
        OnPropertyChanged(nameof(OutOfPlaneDisplay));
        OnPropertyChanged(nameof(PitchDisplay));
        OnPropertyChanged(nameof(RollDisplay));
        OnPropertyChanged(nameof(InclinationStatus));
        OnPropertyChanged(nameof(MisclosureStatus));
        OnPropertyChanged(nameof(OutOfPlaneStatus));
        OnPropertyChanged(nameof(AverageCornerHeading));
        OnPropertyChanged(nameof(AverageCornerHeadingDisplay));
        
        // New UI properties
        OnPropertyChanged(nameof(TotalInclination));
        OnPropertyChanged(nameof(InclinationBearing));
        OnPropertyChanged(nameof(PitchAngle));
        OnPropertyChanged(nameof(RollAngle));
        OnPropertyChanged(nameof(AverageDepth));
        OnPropertyChanged(nameof(DepthRange));
        OnPropertyChanged(nameof(Misclosure));
        OnPropertyChanged(nameof(ResultStatus));
        OnPropertyChanged(nameof(ResultStatusText));
        OnPropertyChanged(nameof(ResultStatusIcon));
        OnPropertyChanged(nameof(HeadingDescription));
    }

    #endregion

    #region Methods - Charts and Visualization

    private void UpdateCharts()
    {
        try
        {
            // Clear existing charts first to prevent PlotModel sharing errors
            // OxyPlot doesn't allow the same PlotModel to be bound to multiple controls
            DepthProfileChart = null;
            PlanViewChart = null;
            Step3PlanViewChart = null;
            VectorDiagramChart = null;
            
            // Force UI to release references before creating new models
            System.Windows.Application.Current?.Dispatcher?.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);
            
            if (Corners.Count > 0)
            {
                DepthProfileChart = ChartService.CreateDepthProfileChart(_project);
                PlanViewChart = ChartService.CreatePlanViewChart(_project);
                Step3PlanViewChart = ChartService.CreatePlanViewChart(_project);
            }

            if (Result != null)
            {
                VectorDiagramChart = ChartService.CreateVectorDiagram(Result);
            }

            Update3DModel();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chart update error: {ex.Message}");
        }
    }

    private void UpdateSelectedFileChart()
    {
        if (_selectedFile != null && _selectedFile.Readings.Count > 0)
            SelectedFileChart = ChartService.CreateTimeSeriesChart(_selectedFile);
        else
            SelectedFileChart = null;
    }

    private void Update3DModel()
    {
        if (Corners.Count >= 3)
        {
            var service = new Visualization3DService();
            Structure3DModel = service.CreateStructureModel(_project, DepthExaggeration);
        }
    }

    #endregion

    #region Methods - Export

    private async Task ExportToExcelAsync(object? parameter)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"{StructureName}_Inclination_{DateTime.Now:yyyyMMdd}.xlsx"
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "Exporting to Excel...";

        try
        {
            await Task.Run(() => new ExcelExporter().Export(dialog.FileName, _project));
            StatusMessage = "Excel export complete";

            if (MessageBox.Show("Export complete. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dialog.FileName, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportToPdfAsync(object? parameter)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export to PDF",
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"{StructureName}_Report_{DateTime.Now:yyyyMMdd}.pdf"
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "Generating PDF...";

        try
        {
            await Task.Run(() => new PdfReportGenerator().Generate(dialog.FileName, _project));
            StatusMessage = "PDF export complete";

            if (MessageBox.Show("Export complete. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dialog.FileName, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportToDxfAsync(object? parameter)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export to DXF",
            Filter = "DXF Files (*.dxf)|*.dxf",
            FileName = $"{StructureName}_Inclination_{DateTime.Now:yyyyMMdd}.dxf"
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "Exporting to DXF...";

        try
        {
            await Task.Run(() => new DxfExporter().Export(dialog.FileName, _project));
            StatusMessage = "DXF export complete";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Generates a verification certificate for the inclination analysis.
    /// This method prepares the certificate request data for the Fathom OS Certificate Service.
    /// </summary>
    private void GenerateCertificate()
    {
        if (Result == null || !IsApproved)
        {
            MessageBox.Show("Results must be approved before generating a certificate.", 
                "Certificate Generation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(ReviewerName))
        {
            MessageBox.Show("Please enter the signatory name.", 
                "Certificate Generation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            // Build the certificate request
            // NOTE: When FathomOS.Core.Certificates is available, use:
            // var request = new CertificateRequest { ... };
            // await CertificateService.CreateAsync(request);
            
            var processingData = GetCertificateProcessingData();
            var inputFiles = GetInputFileList();
            
            // For now, show a summary of what would be included in the certificate
            var summary = new System.Text.StringBuilder();
            summary.AppendLine("═══════════════════════════════════════════════════════════════");
            summary.AppendLine("     STRUCTURE INCLINATION VERIFICATION CERTIFICATE");
            summary.AppendLine("═══════════════════════════════════════════════════════════════");
            summary.AppendLine();
            summary.AppendLine($"Certificate Code: TI");
            summary.AppendLine($"Issue Date: {DateTime.UtcNow:dd MMMM yyyy, HH:mm} UTC");
            summary.AppendLine();
            summary.AppendLine("PROJECT INFORMATION:");
            summary.AppendLine($"  Project: {ProjectName}");
            if (!string.IsNullOrEmpty(ClientName))
                summary.AppendLine($"  Client: {ClientName}");
            if (!string.IsNullOrEmpty(VesselName))
                summary.AppendLine($"  Vessel: {VesselName}");
            summary.AppendLine();
            summary.AppendLine("PROCESSING DATA:");
            foreach (var kvp in processingData)
            {
                summary.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            summary.AppendLine();
            summary.AppendLine("INPUT FILES:");
            foreach (var file in inputFiles)
            {
                summary.AppendLine($"  • {file}");
            }
            summary.AppendLine();
            summary.AppendLine("CERTIFICATION STATEMENT:");
            summary.AppendLine("This is to certify that the inclination analysis of the specified");
            summary.AppendLine("subsea structure has been successfully completed using validated");
            summary.AppendLine("depth measurement processing algorithms and industry-standard");
            summary.AppendLine("calculation methods.");
            summary.AppendLine();
            summary.AppendLine("SIGNATORY:");
            summary.AppendLine($"  Name: {ReviewerName}");
            summary.AppendLine($"  Title: {ReviewerTitle}");
            if (!string.IsNullOrWhiteSpace(ReviewerCredentials))
                summary.AppendLine($"  Credentials: {ReviewerCredentials}");
            summary.AppendLine($"  Date: {ReviewDate:dd MMMM yyyy}");
            summary.AppendLine();
            summary.AppendLine("═══════════════════════════════════════════════════════════════");
            summary.AppendLine();
            summary.AppendLine("NOTE: Full certificate generation will be available when");
            summary.AppendLine("the FathomOS.Core.Certificates service is integrated.");
            
            MessageBox.Show(summary.ToString(), "Certificate Preview", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusMessage = "Certificate preview generated";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Certificate generation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveCharts()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Charts",
            Filter = "PNG Images (*.png)|*.png",
            FileName = $"{StructureName}_Charts"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            string basePath = Path.Combine(Path.GetDirectoryName(dialog.FileName)!,
                Path.GetFileNameWithoutExtension(dialog.FileName));

            int count = 0;

            if (DepthProfileChart != null)
            {
                OxyPlot.Wpf.PngExporter.Export(DepthProfileChart, $"{basePath}_DepthProfile.png", 1200, 600, 96);
                count++;
            }

            if (PlanViewChart != null)
            {
                OxyPlot.Wpf.PngExporter.Export(PlanViewChart, $"{basePath}_PlanView.png", 800, 800, 96);
                count++;
            }

            if (VectorDiagramChart != null)
            {
                OxyPlot.Wpf.PngExporter.Export(VectorDiagramChart, $"{basePath}_Vector.png", 800, 800, 96);
                count++;
            }

            StatusMessage = $"Saved {count} chart(s)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion

    #region Methods - Project Management

    private void InitializeDefaultProject()
    {
        _project = new InclinationProject
        {
            SurveyDate = DateTime.Now,
            DimensionUnit = _settings.DefaultDimensionUnit,
            DepthUnit = _settings.DefaultDepthUnit,
            WaterDensity = _settings.DefaultWaterDensity
        };
        // Don't create default corners - they come from loaded files only
    }

    // This method is no longer needed - corners come from loaded files
    // Keeping for potential future use with manual corner creation
    private void AddManualCorner()
    {
        int next = Corners.Count(c => !c.IsClosurePoint) + 1;
        Corners.Add(new CornerMeasurement { Index = next, Name = $"DQ{next}" });
        OnPropertyChanged(nameof(CanCalculate));
    }

    private void NewProject()
    {
        if (LoadedFiles.Count > 0 || HasResult)
        {
            if (MessageBox.Show("Discard current project?", "New Project", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;
        }

        InitializeDefaultProject();
        LoadedFiles.Clear();
        TideData.Clear();
        DepthProfileChart = null;
        PlanViewChart = null;
        Step3PlanViewChart = null;
        VectorDiagramChart = null;
        Structure3DModel = null;

        OnPropertyChanged(nameof(HasLoadedFiles));
        OnPropertyChanged(nameof(HasResult));
        RefreshResultProperties();
        StatusMessage = "New project created";
    }

    private void SaveProject()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Project",
            Filter = "Fathom OS Inclination Project (*.fosi)|*.fosi",
            FileName = $"{StructureName}_Inclination.fosi"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var json = JsonSerializer.Serialize(_project, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);
            AddToRecentFiles(dialog.FileName);
            StatusMessage = "Project saved";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadProjectAsync(object? parameter)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Project",
            Filter = "Fathom OS Inclination Project (*.fosi)|*.fosi"
        };

        if (dialog.ShowDialog() != true) return;
        await LoadProjectFromPathAsync(dialog.FileName);
    }

    private void OpenRecentFile(object? parameter)
    {
        if (parameter is string filePath && File.Exists(filePath))
            _ = LoadProjectFromPathAsync(filePath);
    }

    private async Task LoadProjectFromPathAsync(string filePath)
    {
        IsBusy = true;
        StatusMessage = "Loading project...";

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var project = JsonSerializer.Deserialize<InclinationProject>(json);

            if (project != null)
            {
                _project = project;
                OnPropertyChanged(nameof(HasLoadedFiles));
                OnPropertyChanged(nameof(HasResult));
                RefreshResultProperties();
                UpdateCharts();
                AddToRecentFiles(filePath);
                StatusMessage = "Project loaded";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Load error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();
        foreach (var path in _settings.RecentProjects.Take(10))
        {
            if (File.Exists(path))
            {
                RecentFiles.Add(new RecentFileItem
                {
                    FilePath = path,
                    FileName = Path.GetFileNameWithoutExtension(path),
                    LastAccessed = File.GetLastWriteTime(path)
                });
            }
        }
        OnPropertyChanged(nameof(HasRecentFiles));
    }

    private void AddToRecentFiles(string filePath)
    {
        _settings.RecentProjects.Remove(filePath);
        _settings.RecentProjects.Insert(0, filePath);
        if (_settings.RecentProjects.Count > 15)
            _settings.RecentProjects = _settings.RecentProjects.Take(15).ToList();
        SaveSettings();
        LoadRecentFiles();
    }

    #endregion

    #region Methods - Settings

    private void LoadSettings()
    {
        try
        {
            var path = GetSettingsPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _settings = JsonSerializer.Deserialize<ModuleSettings>(json) ?? new ModuleSettings();
                _isDarkTheme = _settings.IsDarkTheme;
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    private string GetSettingsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FathomOS", "TreeInclination", "settings.json");

    #endregion

    #region Methods - Help

    private void ShowHelp()
    {
        var help = new Views.HelpDialog();
        help.ShowDialog();
    }

    #endregion
}
