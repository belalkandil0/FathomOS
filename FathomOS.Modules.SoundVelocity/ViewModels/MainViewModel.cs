using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using FathomOS.Modules.SoundVelocity.Models;
using FathomOS.Modules.SoundVelocity.Services;

namespace FathomOS.Modules.SoundVelocity.ViewModels;

/// <summary>
/// Main ViewModel for SVP Processing Module
/// Steps:
/// 1. Project Info + File Selection
/// 2. Data Preview & Column Mapping (auto-detected, user confirms)
/// 3. Processing Settings
/// 4. Visualization & Smoothing
/// 5. Export
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly FileParserService _fileParser = new();
    private readonly DataProcessingService _dataProcessor = new();
    private readonly ExportService _exportService = new();
    private readonly SmoothingService _smoothingService = new();

    #region Events

    public event EventHandler? RequestThemeToggle;

    #endregion

    #region Private Fields

    private int _currentStep = 1;
    private string _statusMessage = "Ready - Enter project information and load a CTD/SVP file";
    private bool _isBusy;

    // Step 1: Project Info + File Selection
    private ProjectInfo _projectInfo = new();
    private string _loadedFileName = string.Empty;
    private ParsedFileResult? _parseResult;

    // Step 2: Data Preview & Column Mapping
    private ObservableCollection<DataRowViewModel> _previewData = new();
    private ObservableCollection<ColumnInfo> _columnInfos = new();
    private ColumnMapping _columnMapping = new();
    
    // Step 3: Processing Settings
    private ProcessingSettings _processingSettings = new();
    
    // Step 4: Visualization & Smoothing
    private PlotModel? _svpPlotModel;
    private bool _showSvProfile = true;
    private bool _showTemperatureProfile = true;
    private bool _showSalinityProfile = false;
    private bool _showDensityProfile = false;
    private bool _showSmoothedData = true;
    private bool _showRawData = true;
    private SmoothingMethod _selectedSmoothingMethod = SmoothingMethod.MovingAverage;
    private int _smoothingWindowSize = 5;
    private bool _enableSmoothing = false;
    
    // Step 5: Results & Export
    private ObservableCollection<CtdDataPoint> _processedDataPoints = new();
    private ObservableCollection<CtdDataPoint> _smoothedDataPoints = new();
    private DataStatistics _statistics = new();
    private ExportOptions _exportOptions = new();

    #endregion

    #region Constructor

    public MainViewModel()
    {
        // Initialize commands
        LoadFileCommand = new RelayCommand(_ => ExecuteLoadFile(), _ => !IsBusy);
        NextStepCommand = new RelayCommand(_ => NextStep(), _ => CanGoNext());
        PreviousStepCommand = new RelayCommand(_ => PreviousStep(), _ => CurrentStep > 1);
        ProcessDataCommand = new RelayCommand(_ => ProcessData(), _ => CanProcess());
        ExportCommand = new RelayCommand(_ => Export(), _ => ProcessedDataPoints.Count > 0);
        BrowseOutputCommand = new RelayCommand(_ => BrowseOutput());
        RefreshChartCommand = new RelayCommand(_ => UpdateChart());
        ApplySmoothingCommand = new RelayCommand(_ => ApplySmoothing(), _ => ProcessedDataPoints.Count > 0);
        ToggleThemeCommand = new RelayCommand(_ => RequestThemeToggle?.Invoke(this, EventArgs.Empty));

        // Set default export options
        _exportOptions.ExportExcel = true;
        _exportOptions.ExportUsr = true;
        
        // Initialize plot model
        InitializePlotModel();
    }

    #endregion

    #region Commands

    public ICommand LoadFileCommand { get; }
    public ICommand NextStepCommand { get; }
    public ICommand PreviousStepCommand { get; }
    public ICommand ProcessDataCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand BrowseOutputCommand { get; }
    public ICommand RefreshChartCommand { get; }
    public ICommand ApplySmoothingCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    #endregion

    #region Properties - Navigation

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
                OnPropertyChanged(nameof(IsStep2OrHigher));
                OnPropertyChanged(nameof(IsStep3OrHigher));
                OnPropertyChanged(nameof(IsStep4OrHigher));
                OnPropertyChanged(nameof(NextButtonText));
            }
        }
    }

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsStep5 => CurrentStep == 5;
    public bool IsStep2OrHigher => CurrentStep >= 2;
    public bool IsStep3OrHigher => CurrentStep >= 3;
    public bool IsStep4OrHigher => CurrentStep >= 4;

    public string NextButtonText => CurrentStep == 5 ? "Finish" : "Next →";

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

    #endregion

    #region Properties - Step 1: Project Info + File Selection

    public string ProjectTitle
    {
        get => _projectInfo.ProjectTitle;
        set { _projectInfo.ProjectTitle = value; OnPropertyChanged(); }
    }

    public string VesselName
    {
        get => _projectInfo.VesselName;
        set { _projectInfo.VesselName = value; OnPropertyChanged(); }
    }

    public string Location
    {
        get => _projectInfo.Location;
        set { _projectInfo.Location = value; OnPropertyChanged(); }
    }

    public double KP
    {
        get => _projectInfo.KP;
        set { _projectInfo.KP = value; OnPropertyChanged(); }
    }

    public string Equipment
    {
        get => _projectInfo.Equipment;
        set { _projectInfo.Equipment = value; OnPropertyChanged(); }
    }

    public string ObservedBy
    {
        get => _projectInfo.ObservedBy;
        set { _projectInfo.ObservedBy = value; OnPropertyChanged(); }
    }

    public string CheckedBy
    {
        get => _projectInfo.CheckedBy;
        set { _projectInfo.CheckedBy = value; OnPropertyChanged(); }
    }

    public DateTime CastDateTime
    {
        get => _projectInfo.CastDateTime;
        set { _projectInfo.CastDateTime = value; OnPropertyChanged(); }
    }

    public string LatitudeString
    {
        get => _projectInfo.LatitudeString;
        set
        {
            _projectInfo.LatitudeString = value;
            _projectInfo.Latitude = OceanographicCalculations.ParseCoordinate(value, _projectInfo.CoordinateFormat, out _);
            OnPropertyChanged();
            OnPropertyChanged(nameof(CalculatedLatitude));
        }
    }

    public string LongitudeString
    {
        get => _projectInfo.LongitudeString;
        set
        {
            _projectInfo.LongitudeString = value;
            _projectInfo.Longitude = OceanographicCalculations.ParseCoordinate(value, _projectInfo.CoordinateFormat, out _);
            OnPropertyChanged();
            OnPropertyChanged(nameof(CalculatedLongitude));
        }
    }

    public double CalculatedLatitude => _projectInfo.Latitude;
    public double CalculatedLongitude => _projectInfo.Longitude;

    public GeoCoordinateFormat CoordinateFormat
    {
        get => _projectInfo.CoordinateFormat;
        set
        {
            _projectInfo.CoordinateFormat = value;
            OnPropertyChanged();
            _projectInfo.Latitude = OceanographicCalculations.ParseCoordinate(_projectInfo.LatitudeString, value, out _);
            _projectInfo.Longitude = OceanographicCalculations.ParseCoordinate(_projectInfo.LongitudeString, value, out _);
            OnPropertyChanged(nameof(CalculatedLatitude));
            OnPropertyChanged(nameof(CalculatedLongitude));
        }
    }

    public string LoadedFileName
    {
        get => _loadedFileName;
        set
        {
            if (SetProperty(ref _loadedFileName, value))
                OnPropertyChanged(nameof(HasLoadedFile));
        }
    }

    public bool HasLoadedFile => !string.IsNullOrEmpty(LoadedFileName);

    public int LoadedRowCount => _parseResult?.DataRows.Count ?? 0;
    public int LoadedColumnCount => _parseResult?.ColumnCount ?? 0;
    public string DetectedFileType => _parseResult?.DetectedFileType.ToString() ?? "None";

    public Array CoordinateFormatOptions => Enum.GetValues(typeof(GeoCoordinateFormat));

    #endregion

    #region Properties - Step 2: Data Preview & Column Mapping

    public ObservableCollection<DataRowViewModel> PreviewData
    {
        get => _previewData;
        set => SetProperty(ref _previewData, value);
    }

    public ObservableCollection<ColumnInfo> ColumnInfos
    {
        get => _columnInfos;
        set => SetProperty(ref _columnInfos, value);
    }

    public int DepthColumnIndex
    {
        get => _columnMapping.DepthColumn;
        set { _columnMapping.DepthColumn = value; OnPropertyChanged(); UpdateColumnDetection(); }
    }

    public int SvColumnIndex
    {
        get => _columnMapping.SoundVelocityColumn;
        set { _columnMapping.SoundVelocityColumn = value; OnPropertyChanged(); UpdateColumnDetection(); }
    }

    public int TemperatureColumnIndex
    {
        get => _columnMapping.TemperatureColumn;
        set { _columnMapping.TemperatureColumn = value; OnPropertyChanged(); UpdateColumnDetection(); }
    }

    public int SalinityColumnIndex
    {
        get => _columnMapping.SalinityColumn;
        set { _columnMapping.SalinityColumn = value; OnPropertyChanged(); UpdateColumnDetection(); }
    }

    public int DensityColumnIndex
    {
        get => _columnMapping.DensityColumn;
        set { _columnMapping.DensityColumn = value; OnPropertyChanged(); UpdateColumnDetection(); }
    }

    public ObservableCollection<ColumnOption> ColumnOptions { get; } = new();

    #endregion

    #region Properties - Step 3: Processing Settings

    public CalculationMode SelectedCalculationMode
    {
        get => _processingSettings.CalculationMode;
        set
        {
            _processingSettings.CalculationMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowTemperatureSalinity));
            OnPropertyChanged(nameof(ShowSvColumn));
            OnPropertyChanged(nameof(SalinityLabel));
        }
    }

    public SoundVelocityFormula SelectedSvFormula
    {
        get => _processingSettings.SvFormula;
        set
        {
            _processingSettings.SvFormula = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowSvColumn));
        }
    }

    public DensityFormula SelectedDensityFormula
    {
        get => _processingSettings.DensityFormula;
        set
        {
            _processingSettings.DensityFormula = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowDensityColumn));
        }
    }

    public DepthPressureType SelectedInputType
    {
        get => _processingSettings.InputType;
        set { _processingSettings.InputType = value; OnPropertyChanged(); }
    }

    public double DepthInterval
    {
        get => _processingSettings.DepthInterval;
        set { _processingSettings.DepthInterval = value; OnPropertyChanged(); }
    }

    public double TransducerDepth
    {
        get => _processingSettings.TransducerDepth;
        set { _processingSettings.TransducerDepth = value; OnPropertyChanged(); }
    }

    public bool UseLatitudeForGravity
    {
        get => _processingSettings.UseLatitudeForGravity;
        set { _processingSettings.UseLatitudeForGravity = value; OnPropertyChanged(); }
    }

    // Visibility helpers
    public bool ShowTemperatureSalinity => SelectedCalculationMode != CalculationMode.SvOnly;
    public bool ShowSvColumn => SelectedSvFormula == SoundVelocityFormula.ExternalSource || 
                                 SelectedCalculationMode == CalculationMode.SvOnly;
    public bool ShowDensityColumn => SelectedDensityFormula == DensityFormula.ExternalSource;
    public string SalinityLabel => SelectedCalculationMode == CalculationMode.CtdSvWithConductivity ? "Conductivity" : "Salinity";

    public Array CalculationModeOptions => Enum.GetValues(typeof(CalculationMode));
    public Array SvFormulaOptions => Enum.GetValues(typeof(SoundVelocityFormula));
    public Array DensityFormulaOptions => Enum.GetValues(typeof(DensityFormula));
    public Array InputTypeOptions => Enum.GetValues(typeof(DepthPressureType));

    #endregion

    #region Properties - Step 4: Visualization & Smoothing

    public PlotModel? SvpPlotModel
    {
        get => _svpPlotModel;
        set => SetProperty(ref _svpPlotModel, value);
    }

    public bool ShowSvProfile
    {
        get => _showSvProfile;
        set { SetProperty(ref _showSvProfile, value); UpdateChart(); }
    }

    public bool ShowTemperatureProfile
    {
        get => _showTemperatureProfile;
        set { SetProperty(ref _showTemperatureProfile, value); UpdateChart(); }
    }

    public bool ShowSalinityProfile
    {
        get => _showSalinityProfile;
        set { SetProperty(ref _showSalinityProfile, value); UpdateChart(); }
    }

    public bool ShowDensityProfile
    {
        get => _showDensityProfile;
        set { SetProperty(ref _showDensityProfile, value); UpdateChart(); }
    }

    public bool ShowSmoothedData
    {
        get => _showSmoothedData;
        set { SetProperty(ref _showSmoothedData, value); UpdateChart(); }
    }

    public bool ShowRawData
    {
        get => _showRawData;
        set { SetProperty(ref _showRawData, value); UpdateChart(); }
    }

    public SmoothingMethod SelectedSmoothingMethod
    {
        get => _selectedSmoothingMethod;
        set 
        { 
            SetProperty(ref _selectedSmoothingMethod, value);
            if (EnableSmoothing) ApplySmoothing();
        }
    }

    public int SmoothingWindowSize
    {
        get => _smoothingWindowSize;
        set 
        { 
            SetProperty(ref _smoothingWindowSize, Math.Max(3, Math.Min(21, value)));
            if (EnableSmoothing) ApplySmoothing();
        }
    }

    public bool EnableSmoothing
    {
        get => _enableSmoothing;
        set 
        { 
            SetProperty(ref _enableSmoothing, value);
            if (value) ApplySmoothing();
            else UpdateChart();
        }
    }

    public Array SmoothingMethodOptions => Enum.GetValues(typeof(SmoothingMethod));

    #endregion

    #region Properties - Step 5: Results & Export

    public ObservableCollection<CtdDataPoint> ProcessedDataPoints
    {
        get => _processedDataPoints;
        set => SetProperty(ref _processedDataPoints, value);
    }

    public ObservableCollection<CtdDataPoint> SmoothedDataPoints
    {
        get => _smoothedDataPoints;
        set => SetProperty(ref _smoothedDataPoints, value);
    }

    public DataStatistics Statistics
    {
        get => _statistics;
        set => SetProperty(ref _statistics, value);
    }

    public bool ExportUsr
    {
        get => _exportOptions.ExportUsr;
        set { _exportOptions.ExportUsr = value; OnPropertyChanged(); }
    }

    public bool ExportVel
    {
        get => _exportOptions.ExportVel;
        set { _exportOptions.ExportVel = value; OnPropertyChanged(); }
    }

    public bool ExportPro
    {
        get => _exportOptions.ExportPro;
        set { _exportOptions.ExportPro = value; OnPropertyChanged(); }
    }

    public bool ExportExcel
    {
        get => _exportOptions.ExportExcel;
        set { _exportOptions.ExportExcel = value; OnPropertyChanged(); }
    }

    public bool ExportCsv
    {
        get => _exportOptions.ExportCsv;
        set { _exportOptions.ExportCsv = value; OnPropertyChanged(); }
    }

    public bool ExportSmoothed
    {
        get => _exportOptions.ExportSmoothed;
        set { _exportOptions.ExportSmoothed = value; OnPropertyChanged(); }
    }

    public string OutputDirectory
    {
        get => _exportOptions.OutputDirectory;
        set { _exportOptions.OutputDirectory = value; OnPropertyChanged(); }
    }

    #endregion

    #region Methods - File Loading

    private void ExecuteLoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open CTD/SVP File",
            Filter = "CTD/SVP Files (*.000;*.001;*.txt;*.csv;*.npd;*.log)|*.000;*.001;*.002;*.003;*.txt;*.csv;*.npd;*.log;*.dat|" +
                     "Valeport Files (*.000;*.001;*.002)|*.000;*.001;*.002;*.003|" +
                     "CSV/Text Files (*.csv;*.txt)|*.csv;*.txt|" +
                     "All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true) return;

        LoadFile(dialog.FileName);
    }

    public void LoadFile(string filePath)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Parsing file...";

            // Parse file with auto-detection
            _parseResult = _fileParser.ParseFile(filePath);
            
            if (!_parseResult.Success)
            {
                MessageBox.Show($"Error parsing file: {_parseResult.ErrorMessage}", 
                    "Parse Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusMessage = "File parse failed";
                return;
            }

            LoadedFileName = _parseResult.FileName;

            // Copy auto-detected column mappings
            _columnMapping = _parseResult.ColumnMappings;
            OnPropertyChanged(nameof(DepthColumnIndex));
            OnPropertyChanged(nameof(SvColumnIndex));
            OnPropertyChanged(nameof(TemperatureColumnIndex));
            OnPropertyChanged(nameof(SalinityColumnIndex));
            OnPropertyChanged(nameof(DensityColumnIndex));

            // Update column options for dropdowns
            UpdateColumnOptions();

            // Update column infos display
            ColumnInfos = new ObservableCollection<ColumnInfo>(_parseResult.ColumnInfos);

            // Update preview data
            UpdatePreviewData();

            // Apply extracted metadata to project info
            ApplyExtractedMetadata();

            // Set output directory
            OutputDirectory = Path.GetDirectoryName(filePath) ?? "";
            _exportOptions.BaseFileName = "SVP_" + Path.GetFileNameWithoutExtension(filePath);

            OnPropertyChanged(nameof(LoadedRowCount));
            OnPropertyChanged(nameof(LoadedColumnCount));
            OnPropertyChanged(nameof(DetectedFileType));

            StatusMessage = $"✓ Loaded {_parseResult.DataRows.Count} rows, {_parseResult.ColumnCount} columns - Auto-detected: {GetDetectionSummary()}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading file: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error loading file";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateColumnOptions()
    {
        ColumnOptions.Clear();
        ColumnOptions.Add(new ColumnOption { Index = -1, DisplayName = "(Not Used)" });
        
        if (_parseResult != null)
        {
            for (int i = 0; i < _parseResult.DetectedHeaders.Length; i++)
            {
                ColumnOptions.Add(new ColumnOption 
                { 
                    Index = i, 
                    DisplayName = $"{(char)('A' + i)}: {_parseResult.DetectedHeaders[i]}" 
                });
            }
        }
    }

    private void UpdatePreviewData()
    {
        PreviewData.Clear();
        if (_parseResult == null) return;

        // Show ALL rows - no limit (DataGrid virtualization handles performance)
        foreach (var row in _parseResult.DataRows)
        {
            PreviewData.Add(new DataRowViewModel
            {
                RowIndex = row.RowIndex,
                Values = row.Values,
                Headers = _parseResult.DetectedHeaders
            });
        }
    }

    private void ApplyExtractedMetadata()
    {
        if (_parseResult == null) return;

        if (_parseResult.ExtractedDateTime.HasValue)
            CastDateTime = _parseResult.ExtractedDateTime.Value;
        
        if (!string.IsNullOrEmpty(_parseResult.ExtractedVessel))
            VesselName = _parseResult.ExtractedVessel;
        
        if (!string.IsNullOrEmpty(_parseResult.ExtractedLatitude))
            LatitudeString = _parseResult.ExtractedLatitude;
        
        if (!string.IsNullOrEmpty(_parseResult.ExtractedLongitude))
            LongitudeString = _parseResult.ExtractedLongitude;
        
        if (!string.IsNullOrEmpty(_parseResult.ExtractedStation))
            Location = _parseResult.ExtractedStation;
        
        if (!string.IsNullOrEmpty(_parseResult.ExtractedEquipment))
            Equipment = _parseResult.ExtractedEquipment;
    }

    private string GetDetectionSummary()
    {
        var detected = new List<string>();
        if (_columnMapping.DepthColumn >= 0) detected.Add("Depth");
        if (_columnMapping.SoundVelocityColumn >= 0) detected.Add("SV");
        if (_columnMapping.TemperatureColumn >= 0) detected.Add("Temp");
        if (_columnMapping.SalinityColumn >= 0) detected.Add("Sal");
        if (_columnMapping.DensityColumn >= 0) detected.Add("Dens");
        return detected.Count > 0 ? string.Join(", ", detected) : "None";
    }

    private void UpdateColumnDetection()
    {
        // Update column info display when user changes mappings
        if (_parseResult == null) return;
        
        foreach (var info in ColumnInfos)
        {
            info.DetectedType = GetDetectedType(info.Index);
        }
        OnPropertyChanged(nameof(ColumnInfos));
    }

    private string GetDetectedType(int index)
    {
        if (_columnMapping.DepthColumn == index) return "✓ Depth";
        if (_columnMapping.SoundVelocityColumn == index) return "✓ Sound Velocity";
        if (_columnMapping.TemperatureColumn == index) return "✓ Temperature";
        if (_columnMapping.SalinityColumn == index) return "✓ Salinity/Conductivity";
        if (_columnMapping.DensityColumn == index) return "✓ Density";
        return "—";
    }

    #endregion

    #region Methods - Navigation

    private bool CanGoNext()
    {
        return CurrentStep switch
        {
            1 => _parseResult != null && _parseResult.Success,
            2 => _columnMapping.DepthColumn >= 0,
            3 => true,
            4 => ProcessedDataPoints.Count > 0,
            5 => false,
            _ => false
        };
    }

    private void NextStep()
    {
        if (CurrentStep < 5)
        {
            CurrentStep++;
            
            if (CurrentStep == 4)
            {
                // Process data when entering visualization step
                ProcessData();
            }
        }
    }

    private void PreviousStep()
    {
        if (CurrentStep > 1)
            CurrentStep--;
    }

    private bool CanProcess()
    {
        return _parseResult != null && _parseResult.Success && _columnMapping.DepthColumn >= 0;
    }

    #endregion

    #region Methods - Processing

    private void ProcessData()
    {
        if (_parseResult == null) return;

        try
        {
            IsBusy = true;
            StatusMessage = "Processing data...";

            // Update parse result with current column mapping
            _parseResult.ColumnMappings = _columnMapping;

            // Convert to data points
            var rawPoints = _fileParser.ConvertToDataPoints(_parseResult);

            // Process (sort, interpolate, calculate)
            var processed = _dataProcessor.ProcessData(rawPoints, _processingSettings, 
                _projectInfo.Latitude, UseLatitudeForGravity);
            
            ProcessedDataPoints = new ObservableCollection<CtdDataPoint>(processed);

            // Calculate statistics
            Statistics = _dataProcessor.CalculateStatistics(processed);
            OnPropertyChanged(nameof(Statistics));

            // Apply smoothing if enabled
            if (EnableSmoothing)
            {
                ApplySmoothing();
            }
            else
            {
                SmoothedDataPoints = new ObservableCollection<CtdDataPoint>();
            }

            // Update chart
            UpdateChart();

            StatusMessage = $"✓ Processed {ProcessedDataPoints.Count} points";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error processing data: {ex.Message}", "Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error processing data";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySmoothing()
    {
        if (ProcessedDataPoints.Count == 0) return;

        try
        {
            var smoothed = _smoothingService.SmoothData(
                ProcessedDataPoints.ToList(), 
                SelectedSmoothingMethod, 
                SmoothingWindowSize);

            SmoothedDataPoints = new ObservableCollection<CtdDataPoint>(smoothed);
            UpdateChart();
            StatusMessage = $"Applied {SelectedSmoothingMethod} smoothing (window={SmoothingWindowSize})";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Smoothing error: {ex.Message}";
        }
    }

    #endregion

    #region Methods - Chart

    private void InitializePlotModel()
    {
        var model = new PlotModel
        {
            Title = "Sound Velocity Profile",
            Background = OxyColors.Transparent,
            PlotAreaBorderColor = OxyColor.FromRgb(88, 91, 112),
            TextColor = OxyColor.FromRgb(205, 214, 244),
            TitleColor = OxyColor.FromRgb(205, 214, 244)
        };

        // Depth axis (Y, inverted - depth increases downward)
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Depth (m)",
            StartPosition = 1,
            EndPosition = 0,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(88, 91, 112),
            AxislineColor = OxyColor.FromRgb(88, 91, 112),
            TextColor = OxyColor.FromRgb(166, 173, 200),
            TitleColor = OxyColor.FromRgb(166, 173, 200)
        });

        // Sound Velocity axis (X, bottom)
        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Sound Velocity (m/s)",
            Key = "SV",
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(88, 91, 112),
            AxislineColor = OxyColor.FromRgb(88, 91, 112),
            TextColor = OxyColor.FromRgb(166, 173, 200),
            TitleColor = OxyColor.FromRgb(166, 173, 200)
        });

        SvpPlotModel = model;
    }

    private void UpdateChart()
    {
        if (SvpPlotModel == null || ProcessedDataPoints.Count == 0) return;

        SvpPlotModel.Series.Clear();

        var dataToPlot = ProcessedDataPoints.ToList();
        var smoothedToPlot = SmoothedDataPoints.Count > 0 ? SmoothedDataPoints.ToList() : null;

        // Sound Velocity series - Raw/Processed
        if (ShowSvProfile && ShowRawData)
        {
            var svSeries = new LineSeries
            {
                Title = "Sound Velocity (Raw)",
                Color = OxyColor.FromRgb(137, 180, 250),
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                MarkerFill = OxyColor.FromRgb(137, 180, 250)
            };
            foreach (var point in dataToPlot)
            {
                svSeries.Points.Add(new DataPoint(point.SoundVelocity, point.Depth));
            }
            SvpPlotModel.Series.Add(svSeries);
        }

        // Sound Velocity series - Smoothed
        if (ShowSvProfile && ShowSmoothedData && EnableSmoothing && smoothedToPlot != null)
        {
            var svSmoothedSeries = new LineSeries
            {
                Title = "Sound Velocity (Smoothed)",
                Color = OxyColor.FromRgb(249, 226, 175),
                StrokeThickness = 3,
                MarkerType = MarkerType.None
            };
            foreach (var point in smoothedToPlot)
            {
                svSmoothedSeries.Points.Add(new DataPoint(point.SoundVelocity, point.Depth));
            }
            SvpPlotModel.Series.Add(svSmoothedSeries);
        }

        // Temperature series (scaled to fit)
        if (ShowTemperatureProfile && ShowRawData && dataToPlot.Any(p => p.Temperature != 0))
        {
            var tempSeries = new LineSeries
            {
                Title = "Temperature (scaled)",
                Color = OxyColor.FromRgb(243, 139, 168),
                StrokeThickness = 2,
                MarkerType = MarkerType.Triangle,
                MarkerSize = 3,
                MarkerFill = OxyColor.FromRgb(243, 139, 168)
            };
            double svMin = dataToPlot.Min(p => p.SoundVelocity);
            double svMax = dataToPlot.Max(p => p.SoundVelocity);
            double tempMin = dataToPlot.Min(p => p.Temperature);
            double tempMax = dataToPlot.Max(p => p.Temperature);
            double tempRange = tempMax - tempMin;
            if (tempRange < 0.001) tempRange = 1;
            
            foreach (var point in dataToPlot)
            {
                double scaledTemp = svMin + (point.Temperature - tempMin) / tempRange * (svMax - svMin);
                tempSeries.Points.Add(new DataPoint(scaledTemp, point.Depth));
            }
            SvpPlotModel.Series.Add(tempSeries);
        }

        // Salinity series (scaled)
        if (ShowSalinityProfile && ShowRawData && dataToPlot.Any(p => p.SalinityOrConductivity != 0))
        {
            var salSeries = new LineSeries
            {
                Title = "Salinity (scaled)",
                Color = OxyColor.FromRgb(166, 227, 161),
                StrokeThickness = 2,
                MarkerType = MarkerType.Square,
                MarkerSize = 3,
                MarkerFill = OxyColor.FromRgb(166, 227, 161)
            };
            double svMin = dataToPlot.Min(p => p.SoundVelocity);
            double svMax = dataToPlot.Max(p => p.SoundVelocity);
            double salMin = dataToPlot.Min(p => p.SalinityOrConductivity);
            double salMax = dataToPlot.Max(p => p.SalinityOrConductivity);
            double salRange = salMax - salMin;
            if (salRange < 0.001) salRange = 1;
            
            foreach (var point in dataToPlot)
            {
                double scaledSal = svMin + (point.SalinityOrConductivity - salMin) / salRange * (svMax - svMin);
                salSeries.Points.Add(new DataPoint(scaledSal, point.Depth));
            }
            SvpPlotModel.Series.Add(salSeries);
        }

        // Density series (scaled)
        if (ShowDensityProfile && ShowRawData && dataToPlot.Any(p => p.Density != 0))
        {
            var denSeries = new LineSeries
            {
                Title = "Density (scaled)",
                Color = OxyColor.FromRgb(203, 166, 247),
                StrokeThickness = 2,
                MarkerType = MarkerType.Diamond,
                MarkerSize = 3,
                MarkerFill = OxyColor.FromRgb(203, 166, 247)
            };
            double svMin = dataToPlot.Min(p => p.SoundVelocity);
            double svMax = dataToPlot.Max(p => p.SoundVelocity);
            double denMin = dataToPlot.Min(p => p.Density);
            double denMax = dataToPlot.Max(p => p.Density);
            double denRange = denMax - denMin;
            if (denRange < 0.001) denRange = 1;
            
            foreach (var point in dataToPlot)
            {
                double scaledDen = svMin + (point.Density - denMin) / denRange * (svMax - svMin);
                denSeries.Points.Add(new DataPoint(scaledDen, point.Depth));
            }
            SvpPlotModel.Series.Add(denSeries);
        }

        SvpPlotModel.InvalidatePlot(true);
    }

    #endregion

    #region Methods - Export

    private void Export()
    {
        if (ProcessedDataPoints.Count == 0)
        {
            MessageBox.Show("No processed data to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = "Exporting files...";

            var exportedFiles = _exportService.ExportAll(
                ProcessedDataPoints.ToList(),
                _processingSettings,
                _projectInfo,
                _exportOptions,
                Statistics,
                SmoothedDataPoints.Count > 0 ? SmoothedDataPoints.ToList() : null);

            StatusMessage = $"✓ Exported {exportedFiles.Count} file(s)";
            MessageBox.Show($"Successfully exported {exportedFiles.Count} file(s):\n\n" + 
                          string.Join("\n", exportedFiles.Select(f => Path.GetFileName(f))),
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting files: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Error exporting files";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BrowseOutput()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Select Output Directory",
            FileName = "Select Folder",
            Filter = "Folder|*.folder",
            CheckFileExists = false,
            CheckPathExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            OutputDirectory = Path.GetDirectoryName(dialog.FileName) ?? "";
        }
    }

    #endregion
}

#region Helper Classes

public class DataRowViewModel
{
    public int RowIndex { get; set; }
    public double[] Values { get; set; } = Array.Empty<double>();
    public string[] Headers { get; set; } = Array.Empty<string>();
    
    // Dynamic property access for DataGrid
    public double Col0 => Values.Length > 0 ? Values[0] : 0;
    public double Col1 => Values.Length > 1 ? Values[1] : 0;
    public double Col2 => Values.Length > 2 ? Values[2] : 0;
    public double Col3 => Values.Length > 3 ? Values[3] : 0;
    public double Col4 => Values.Length > 4 ? Values[4] : 0;
    public double Col5 => Values.Length > 5 ? Values[5] : 0;
    public double Col6 => Values.Length > 6 ? Values[6] : 0;
    public double Col7 => Values.Length > 7 ? Values[7] : 0;
}

public class ColumnOption
{
    public int Index { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

#endregion
