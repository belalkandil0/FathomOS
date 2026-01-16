using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using FathomOS.Modules.VesselGyroCalibration.Models;
using FathomOS.Modules.VesselGyroCalibration.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;

namespace FathomOS.Modules.VesselGyroCalibration.ViewModels.Steps;

#region Step 1: Project Setup

/// <summary>
/// Step 1: Project setup - Mode selection and project details.
/// Clean UI without 3D preview (3D moved to Step 5 for results visualization).
/// </summary>
public class Step1SelectViewModel : WizardStepViewModelBase
{
    public Step1SelectViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        // Subscribe to Project property changes for validation
        Project.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CalibrationProject.ProjectTitle) || 
                e.PropertyName == nameof(CalibrationProject.VesselName))
            {
                OnPropertyChanged(nameof(CanProceed));
                _mainViewModel.RaiseCanGoNextChanged();
            }
        };
    }

    public CalibrationProject Project => _mainViewModel.Project;

    public bool IsCalibrationMode
    {
        get => Project.Purpose == ExercisePurpose.Calibration;
        set { if (value) { Project.Purpose = ExercisePurpose.Calibration; OnPropertyChanged(); OnPropertyChanged(nameof(IsVerificationMode)); } }
    }

    public bool IsVerificationMode
    {
        get => Project.Purpose == ExercisePurpose.Verification;
        set { if (value) { Project.Purpose = ExercisePurpose.Verification; OnPropertyChanged(); OnPropertyChanged(nameof(IsCalibrationMode)); } }
    }

    public IEnumerable<UnitOption> UnitOptions => UnitConversionService.GetUnitOptions();
    
    public LengthUnit SelectedUnit
    {
        get => Project.DisplayUnit;
        set 
        { 
            Project.DisplayUnit = value; 
            _mainViewModel.UpdateDisplayUnit(value);
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(UnitAbbreviation));
        }
    }
    
    public string UnitAbbreviation => UnitConversionService.GetAbbreviation(SelectedUnit);
    
    public bool RoundingEnabled
    {
        get => Project.RoundingEnabled;
        set { Project.RoundingEnabled = value; OnPropertyChanged(); }
    }

    public override bool Validate()
    {
        ValidationMessage = "";
        if (string.IsNullOrWhiteSpace(Project.ProjectTitle) || Project.ProjectTitle == "Name of Project")
        { ValidationMessage = "Please enter a project title."; return false; }
        if (string.IsNullOrWhiteSpace(Project.VesselName))
        { ValidationMessage = "Please enter a vessel name."; return false; }
        return true;
    }
}

#endregion

#region Step 2: Load File

/// <summary>
/// Step 2: Load the NPD/CSV file and show raw data preview.
/// User can see columns available before mapping.
/// </summary>
public class Step2ImportViewModel : WizardStepViewModelBase
{
    private string _selectedFilePath = "";
    private string _statusMessage = "Select a data file to load.";
    private bool _isLoading;
    private RawFileData? _rawData;

    public Step2ImportViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        BrowseCommand = new RelayCommand(_ => BrowseFile());
        ClearCommand = new RelayCommand(_ => ClearData(), _ => HasFile);
    }

    // Properties
    public string SelectedFilePath { get => _selectedFilePath; set { SetProperty(ref _selectedFilePath, value); OnPropertyChanged(nameof(HasFile)); OnPropertyChanged(nameof(FileName)); } }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasFile => _rawData != null && _rawData.TotalRows > 0;
    public string FileName => string.IsNullOrEmpty(SelectedFilePath) ? "" : Path.GetFileName(SelectedFilePath);
    
    public RawFileData? RawData { get => _rawData; set { SetProperty(ref _rawData, value); OnPropertyChanged(nameof(HasFile)); OnPropertyChanged(nameof(ColumnCount)); OnPropertyChanged(nameof(RowCount)); OnPropertyChanged(nameof(CanProceed)); _mainViewModel.RaiseCanGoNextChanged(); } }
    
    public int ColumnCount => _rawData?.Headers.Count ?? 0;
    public int RowCount => _rawData?.TotalRows ?? 0;
    
    // Preview data for DataGrid - using ObservableRangeCollection for bulk operations
    public ObservableRangeCollection<RawDataRow> PreviewRows { get; } = new();
    public ObservableRangeCollection<string> ColumnHeaders { get; } = new();

    // Commands
    public ICommand BrowseCommand { get; }
    public ICommand ClearCommand { get; }

    private void BrowseFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Select Gyro Data File"
        };
        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            LoadFile();
        }
    }

    private void LoadFile()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading file...";

            var parser = new DataParsingService();
            RawData = parser.LoadRawFile(SelectedFilePath);
            
            // Store in MainViewModel for next steps
            _mainViewModel.RawFileData = RawData;

            // Populate column headers with single notification
            ColumnHeaders.ReplaceAll(RawData.Headers);

            // Build all rows first, then replace collection (single UI update)
            var allRows = RawData.GetAllRows();
            var rowList = new List<RawDataRow>(allRows.Count);
            for (int i = 0; i < allRows.Count; i++)
            {
                rowList.Add(new RawDataRow { Index = i + 1, Values = allRows[i] });
            }
            PreviewRows.ReplaceAll(rowList);

            StatusMessage = $"Loaded {RawData.TotalRows:N0} rows with {RawData.Headers.Count} columns.";
            IsCompleted = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
            RawData = null;
            IsCompleted = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearData()
    {
        SelectedFilePath = "";
        RawData = null;
        _mainViewModel.RawFileData = null;
        PreviewRows.Clear();
        ColumnHeaders.Clear();
        StatusMessage = "Select a data file to load.";
        IsCompleted = false;
    }

    public override bool Validate() => HasFile;
}

/// <summary>Helper class for raw data grid display</summary>
public class RawDataRow
{
    public int Index { get; set; }
    public string[] Values { get; set; } = Array.Empty<string>();
    public string Col0 => Values.Length > 0 ? Values[0] : "";
    public string Col1 => Values.Length > 1 ? Values[1] : "";
    public string Col2 => Values.Length > 2 ? Values[2] : "";
    public string Col3 => Values.Length > 3 ? Values[3] : "";
    public string Col4 => Values.Length > 4 ? Values[4] : "";
    public string Col5 => Values.Length > 5 ? Values[5] : "";
    public string Col6 => Values.Length > 6 ? Values[6] : "";
    public string Col7 => Values.Length > 7 ? Values[7] : "";
}

#endregion

#region Step 3: Map Columns

/// <summary>
/// Step 3: User selects which columns contain Reference and Calibrated headings.
/// </summary>
public class Step3ConfigureViewModel : WizardStepViewModelBase
{
    private string _selectedTimeColumn = "";
    private string _selectedReferenceColumn = "";
    private string _selectedCalibratedColumn = "";
    private bool _hasDateTimeSplit = true;
    private string _dateFormat = "dd/MM/yyyy";
    private string _timeFormat = "HH:mm:ss";
    private string _statusMessage = "";

    public Step3ConfigureViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        AutoDetectCommand = new RelayCommand(_ => AutoDetectColumns());
    }

    public override void OnActivated()
    {
        base.OnActivated();
        // Populate column options from loaded file
        AvailableColumns.Clear();
        if (_mainViewModel.RawFileData != null)
        {
            foreach (var col in _mainViewModel.RawFileData.Headers)
                AvailableColumns.Add(col);
            
            // Auto-detect on activation
            AutoDetectColumns();
        }
    }

    // Column options
    public ObservableCollection<string> AvailableColumns { get; } = new();

    // Selected columns
    public string SelectedTimeColumn
    {
        get => _selectedTimeColumn;
        set { SetProperty(ref _selectedTimeColumn, value); UpdateMapping(); OnPropertyChanged(nameof(CanProceed)); _mainViewModel.RaiseCanGoNextChanged(); }
    }

    public string SelectedReferenceColumn
    {
        get => _selectedReferenceColumn;
        set { SetProperty(ref _selectedReferenceColumn, value); UpdateMapping(); OnPropertyChanged(nameof(CanProceed)); _mainViewModel.RaiseCanGoNextChanged(); }
    }

    public string SelectedCalibratedColumn
    {
        get => _selectedCalibratedColumn;
        set { SetProperty(ref _selectedCalibratedColumn, value); UpdateMapping(); OnPropertyChanged(nameof(CanProceed)); _mainViewModel.RaiseCanGoNextChanged(); }
    }

    // Format options
    public bool HasDateTimeSplit
    {
        get => _hasDateTimeSplit;
        set { SetProperty(ref _hasDateTimeSplit, value); UpdateMapping(); }
    }

    public string DateFormat { get => _dateFormat; set { SetProperty(ref _dateFormat, value); UpdateMapping(); } }
    public string TimeFormat { get => _timeFormat; set { SetProperty(ref _timeFormat, value); UpdateMapping(); } }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public ICommand AutoDetectCommand { get; }

    private void AutoDetectColumns()
    {
        if (_mainViewModel.RawFileData == null) return;

        var parser = new DataParsingService();
        var mapping = parser.AutoDetectMapping(_mainViewModel.RawFileData);

        // Set selected columns based on detection
        if (mapping.TimeColumnIndex >= 0 && mapping.TimeColumnIndex < AvailableColumns.Count)
            SelectedTimeColumn = AvailableColumns[mapping.TimeColumnIndex];
        if (mapping.ReferenceHeadingColumnIndex >= 0 && mapping.ReferenceHeadingColumnIndex < AvailableColumns.Count)
            SelectedReferenceColumn = AvailableColumns[mapping.ReferenceHeadingColumnIndex];
        if (mapping.CalibratedHeadingColumnIndex >= 0 && mapping.CalibratedHeadingColumnIndex < AvailableColumns.Count)
            SelectedCalibratedColumn = AvailableColumns[mapping.CalibratedHeadingColumnIndex];

        StatusMessage = "Columns auto-detected. Please verify selections.";
    }

    private void UpdateMapping()
    {
        var mapping = _mainViewModel.ColumnMapping;
        mapping.TimeColumn = SelectedTimeColumn;
        mapping.ReferenceHeadingColumn = SelectedReferenceColumn;
        mapping.CalibratedHeadingColumn = SelectedCalibratedColumn;
        mapping.TimeColumnIndex = AvailableColumns.IndexOf(SelectedTimeColumn);
        mapping.ReferenceHeadingColumnIndex = AvailableColumns.IndexOf(SelectedReferenceColumn);
        mapping.CalibratedHeadingColumnIndex = AvailableColumns.IndexOf(SelectedCalibratedColumn);
        mapping.HasDateTimeSplit = HasDateTimeSplit;
        mapping.DateFormat = DateFormat;
        mapping.TimeFormat = TimeFormat;
    }

    public override bool Validate()
    {
        ValidationMessage = "";
        if (string.IsNullOrEmpty(SelectedReferenceColumn))
        { ValidationMessage = "Please select Reference Heading column."; return false; }
        if (string.IsNullOrEmpty(SelectedCalibratedColumn))
        { ValidationMessage = "Please select Calibrated Heading column."; return false; }
        if (SelectedReferenceColumn == SelectedCalibratedColumn)
        { ValidationMessage = "Reference and Calibrated columns must be different."; return false; }
        return true;
    }

    public override bool CanProceed => 
        !string.IsNullOrEmpty(SelectedReferenceColumn) && 
        !string.IsNullOrEmpty(SelectedCalibratedColumn) &&
        SelectedReferenceColumn != SelectedCalibratedColumn;
}

#endregion

#region Step 4: Filter & Preview

/// <summary>
/// Step 4: Apply column mapping, preview C-O values, and filter by time.
/// </summary>
public class Step4ProcessViewModel : WizardStepViewModelBase
{
    private DateTime? _filterStartTime;
    private DateTime? _filterEndTime;
    private DateTime? _dataStartTime;
    private DateTime? _dataEndTime;
    private int _totalCount;
    private int _filteredCount;
    private string _statusMessage = "";
    private bool _isProcessing;
    private double _previewMeanCO;
    private double _previewStdDev;
    private List<GyroDataPoint> _allParsedPoints = new();
    
    // Slider properties
    private double _sliderMinimum;
    private double _sliderMaximum = 100;
    private double _sliderStart;
    private double _sliderEnd = 100;

    public Step4ProcessViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        ApplyFilterCommand = new RelayCommand(_ => ApplyTimeFilter(), _ => HasData);
        ResetFilterCommand = new RelayCommand(_ => ResetTimeFilter(), _ => HasData);
        ParseDataCommand = new RelayCommand(_ => ParseData());
    }

    public override void OnActivated()
    {
        base.OnActivated();
        // Parse data with the column mapping from Step 3
        ParseData();
    }

    // Time filter properties
    public DateTime? FilterStartTime { get => _filterStartTime; set { SetProperty(ref _filterStartTime, value); } }
    public DateTime? FilterEndTime { get => _filterEndTime; set { SetProperty(ref _filterEndTime, value); } }
    public DateTime? DataStartTime { get => _dataStartTime; set => SetProperty(ref _dataStartTime, value); }
    public DateTime? DataEndTime { get => _dataEndTime; set => SetProperty(ref _dataEndTime, value); }
    
    // Slider properties for time range
    public double SliderMinimum { get => _sliderMinimum; set => SetProperty(ref _sliderMinimum, value); }
    public double SliderMaximum { get => _sliderMaximum; set => SetProperty(ref _sliderMaximum, value); }
    
    public double SliderStart 
    { 
        get => _sliderStart; 
        set 
        { 
            if (SetProperty(ref _sliderStart, value))
            {
                FilterStartTime = TicksToDateTime(value);
                OnPropertyChanged(nameof(FormattedStartTime));
            }
        } 
    }
    
    public double SliderEnd 
    { 
        get => _sliderEnd; 
        set 
        { 
            if (SetProperty(ref _sliderEnd, value))
            {
                FilterEndTime = TicksToDateTime(value);
                OnPropertyChanged(nameof(FormattedEndTime));
            }
        } 
    }
    
    // Formatted time display
    public string FormattedStartTime => FilterStartTime?.ToString("HH:mm:ss") ?? "--:--:--";
    public string FormattedEndTime => FilterEndTime?.ToString("HH:mm:ss") ?? "--:--:--";
    public string FormattedDateRange => DataStartTime.HasValue && DataEndTime.HasValue
        ? $"{DataStartTime:yyyy-MM-dd HH:mm} to {DataEndTime:HH:mm}"
        : "No data";
    
    // Helper methods for slider-DateTime conversion
    private DateTime? TicksToDateTime(double ticks)
    {
        if (!DataStartTime.HasValue || !DataEndTime.HasValue) return null;
        var range = DataEndTime.Value.Ticks - DataStartTime.Value.Ticks;
        if (range <= 0) return DataStartTime;
        var normalized = (ticks - SliderMinimum) / (SliderMaximum - SliderMinimum);
        return new DateTime(DataStartTime.Value.Ticks + (long)(normalized * range));
    }
    
    private double DateTimeToTicks(DateTime dt)
    {
        if (!DataStartTime.HasValue || !DataEndTime.HasValue) return SliderMinimum;
        var range = DataEndTime.Value.Ticks - DataStartTime.Value.Ticks;
        if (range <= 0) return SliderMinimum;
        var normalized = (double)(dt.Ticks - DataStartTime.Value.Ticks) / range;
        return SliderMinimum + normalized * (SliderMaximum - SliderMinimum);
    }

    // Counts
    public int TotalCount { get => _totalCount; set => SetProperty(ref _totalCount, value); }
    public int FilteredCount { get => _filteredCount; set { SetProperty(ref _filteredCount, value); OnPropertyChanged(nameof(HasData)); OnPropertyChanged(nameof(CanProceed)); _mainViewModel.RaiseCanGoNextChanged(); } }
    public bool HasData => FilteredCount > 0;

    // Preview statistics
    public double PreviewMeanCO { get => _previewMeanCO; set => SetProperty(ref _previewMeanCO, value); }
    public double PreviewStdDev { get => _previewStdDev; set => SetProperty(ref _previewStdDev, value); }

    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsProcessing { get => _isProcessing; set => SetProperty(ref _isProcessing, value); }

    // Preview data
    public ObservableCollection<GyroDataPoint> PreviewData { get; } = new();

    // Commands
    public ICommand ApplyFilterCommand { get; }
    public ICommand ResetFilterCommand { get; }
    public ICommand ParseDataCommand { get; }

    private void ParseData()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Parsing data with column mapping...";

            if (_mainViewModel.RawFileData == null)
            {
                StatusMessage = "No file data available. Go back to Step 2.";
                return;
            }

            var parser = new DataParsingService();
            _allParsedPoints = parser.ParseWithMapping(_mainViewModel.RawFileData, _mainViewModel.ColumnMapping);

            TotalCount = _allParsedPoints.Count;

            // Set time range
            if (_allParsedPoints.Any())
            {
                var validTimes = _allParsedPoints.Where(p => p.Timestamp > DateTime.MinValue).ToList();
                if (validTimes.Any())
                {
                    DataStartTime = validTimes.Min(p => p.Timestamp);
                    DataEndTime = validTimes.Max(p => p.Timestamp);
                    FilterStartTime = DataStartTime;
                    FilterEndTime = DataEndTime;
                    
                    // Initialize slider (0-100 range)
                    SliderMinimum = 0;
                    SliderMaximum = 100;
                    _sliderStart = 0;
                    _sliderEnd = 100;
                    OnPropertyChanged(nameof(SliderStart));
                    OnPropertyChanged(nameof(SliderEnd));
                    OnPropertyChanged(nameof(FormattedStartTime));
                    OnPropertyChanged(nameof(FormattedEndTime));
                    OnPropertyChanged(nameof(FormattedDateRange));
                }
            }

            // Apply filter (all data initially)
            ApplyTimeFilter();

            StatusMessage = $"Parsed {TotalCount:N0} records. C-O values calculated.";
            IsCompleted = HasData;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            FilteredCount = 0;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void ApplyTimeFilter()
    {
        var filtered = _allParsedPoints.AsEnumerable();

        if (FilterStartTime.HasValue)
            filtered = filtered.Where(p => p.Timestamp >= FilterStartTime.Value);
        if (FilterEndTime.HasValue)
            filtered = filtered.Where(p => p.Timestamp <= FilterEndTime.Value);

        var filteredList = filtered.ToList();
        FilteredCount = filteredList.Count;

        // Update MainViewModel data points
        _mainViewModel.DataPoints.Clear();
        foreach (var p in filteredList)
            _mainViewModel.DataPoints.Add(p);

        // Show all filtered data (virtualized DataGrid handles performance)
        PreviewData.Clear();
        foreach (var p in filteredList)
            PreviewData.Add(p);

        // Calculate preview statistics
        if (filteredList.Any())
        {
            var coValues = filteredList.Select(p => p.CO).ToList();
            PreviewMeanCO = coValues.Average();
            PreviewStdDev = CalculateStdDev(coValues);
        }

        StatusMessage = $"Filtered: {FilteredCount:N0} of {TotalCount:N0} records. Mean C-O: {PreviewMeanCO:F3}°";
    }

    private void ResetTimeFilter()
    {
        FilterStartTime = DataStartTime;
        FilterEndTime = DataEndTime;
        _sliderStart = SliderMinimum;
        _sliderEnd = SliderMaximum;
        OnPropertyChanged(nameof(SliderStart));
        OnPropertyChanged(nameof(SliderEnd));
        OnPropertyChanged(nameof(FormattedStartTime));
        OnPropertyChanged(nameof(FormattedEndTime));
        ApplyTimeFilter();
    }

    private static double CalculateStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        double mean = values.Average();
        double sumSq = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSq / (values.Count - 1));
    }

    public override bool Validate() => HasData;
}

#endregion

#region Step 5: Analyze

/// <summary>
/// Step 5: Full processing with 3-sigma outlier rejection and statistical analysis.
/// Phase 3: Enhanced with iteration history and 3-sigma limit lines on charts.
/// </summary>
public class Step5AnalyzeViewModel : WizardStepViewModelBase
{
    private bool _isProcessing;
    private string _statusMessage = "";
    private PlotModel? _coPlotModel;
    private PlotModel? _histogramModel;
    private PlotModel? _qqPlotModel;
    private PlotModel? _residualsPlotModel;
    private PlotModel? _boxPlotModel;
    private PlotModel? _polarPlotModel;
    private ChartColorPalette _selectedPalette;
    
    // 3D Visualization
    private readonly Visualization3DService _visualization3D = new();
    private DefaultEffectsManager? _effectsManager;
    private HelixToolkit.Wpf.SharpDX.Camera? _camera;

    public Step5AnalyzeViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        ProcessCommand = new RelayCommand(async _ => await ProcessAsync());
        RecalculateCommand = new RelayCommand(async _ => await ProcessAsync(), _ => HasData);
        ResetViewCommand = new RelayCommand(_ => ResetCamera());
        TopViewCommand = new RelayCommand(_ => SetTopView());
        RefreshChartsCommand = new RelayCommand(_ => UpdateCharts());
        
        // Initialize with first palette
        _selectedPalette = ChartThemeService.AvailablePalettes[0];
        
        Initialize3D();
    }

    public override void OnActivated()
    {
        base.OnActivated();
        // Auto-process when entering this step
        if (_mainViewModel.DataPoints.Count > 0 && _mainViewModel.Result.TotalObservations == 0)
        {
            _ = ProcessAsync();
        }
        else
        {
            UpdateCharts();
            Update3DVisualization();
        }
    }

    // Chart Theme/Palette Selection
    public List<ChartColorPalette> AvailablePalettes => ChartThemeService.AvailablePalettes;
    
    public ChartColorPalette SelectedPalette
    {
        get => _selectedPalette;
        set
        {
            if (SetProperty(ref _selectedPalette, value))
            {
                // Refresh all charts when palette changes
                UpdateCharts();
            }
        }
    }
    
    public ICommand RefreshChartsCommand { get; }

    public bool IsProcessing { get => _isProcessing; set => SetProperty(ref _isProcessing, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool HasData => _mainViewModel.DataPoints.Count > 0;
    public CalibrationResult Result => _mainViewModel.Result;

    // Charts - Main
    public PlotModel? COPlotModel { get => _coPlotModel; set => SetProperty(ref _coPlotModel, value); }
    public PlotModel? HistogramModel { get => _histogramModel; set => SetProperty(ref _histogramModel, value); }
    
    // Charts - Additional (Phase 3)
    public PlotModel? QQPlotModel { get => _qqPlotModel; set => SetProperty(ref _qqPlotModel, value); }
    public PlotModel? ResidualsPlotModel { get => _residualsPlotModel; set => SetProperty(ref _residualsPlotModel, value); }
    public PlotModel? BoxPlotModel { get => _boxPlotModel; set => SetProperty(ref _boxPlotModel, value); }
    public PlotModel? PolarPlotModel { get => _polarPlotModel; set => SetProperty(ref _polarPlotModel, value); }
    
    // New Professional Charts (v35)
    private PlotModel? _controlChartModel;
    private PlotModel? _cusumChartModel;
    public PlotModel? ControlChartModel { get => _controlChartModel; set => SetProperty(ref _controlChartModel, value); }
    public PlotModel? CusumChartModel { get => _cusumChartModel; set => SetProperty(ref _cusumChartModel, value); }
    
    // Additional Professional Charts (v37)
    private PlotModel? _headingCoverageModel;
    private PlotModel? _movingAverageModel;
    private PlotModel? _scatterPlotModel;
    private PlotModel? _acfPlotModel;
    public PlotModel? HeadingCoverageModel { get => _headingCoverageModel; set => SetProperty(ref _headingCoverageModel, value); }
    public PlotModel? MovingAverageModel { get => _movingAverageModel; set => SetProperty(ref _movingAverageModel, value); }
    public PlotModel? ScatterPlotModel { get => _scatterPlotModel; set => SetProperty(ref _scatterPlotModel, value); }
    public PlotModel? AcfPlotModel { get => _acfPlotModel; set => SetProperty(ref _acfPlotModel, value); }
    
    // 3D Visualization Properties
    public DefaultEffectsManager? EffectsManager { get => _effectsManager; set => SetProperty(ref _effectsManager, value); }
    public HelixToolkit.Wpf.SharpDX.Camera? Camera { get => _camera; set => SetProperty(ref _camera, value); }
    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D? VesselGeometry { get; private set; }
    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D? SeaPlaneGeometry { get; private set; }
    public HelixToolkit.Wpf.SharpDX.LineGeometry3D? CompassRoseGeometry { get; private set; }
    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D? ReferenceArrowGeometry { get; private set; }
    public HelixToolkit.Wpf.SharpDX.MeshGeometry3D? CalibratedArrowGeometry { get; private set; }
    public HelixToolkit.Wpf.SharpDX.LineGeometry3D? COArcGeometry { get; private set; }
    public PhongMaterial? VesselMaterial { get; private set; }
    public PhongMaterial? SeaPlaneMaterial { get; private set; }
    public PhongMaterial? ReferenceArrowMaterial { get; private set; }
    public PhongMaterial? CalibratedArrowMaterial { get; private set; }
    public bool ShowHeadingArrows => true;
    public ICommand ResetViewCommand { get; }
    public ICommand TopViewCommand { get; }
    
    private void Initialize3D()
    {
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera
        {
            Position = new System.Windows.Media.Media3D.Point3D(150, -150, 120),
            LookDirection = new System.Windows.Media.Media3D.Vector3D(-150, 150, -100),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1),
            FieldOfView = 45
        };
        SeaPlaneGeometry = _visualization3D.CreateSeaPlaneGeometry(200);
        SeaPlaneMaterial = _visualization3D.CreateSeaMaterial();
        VesselGeometry = _visualization3D.CreateVesselGeometry(80, 16, 12);
        VesselMaterial = _visualization3D.CreateVesselMaterial();
        CompassRoseGeometry = _visualization3D.CreateCompassRoseGeometry(60);
        // Initialize with placeholder arrows
        ReferenceArrowGeometry = _visualization3D.CreateArrowGeometry(new Vector3(0, 0, 15), 0, 50);
        ReferenceArrowMaterial = _visualization3D.CreateArrowMaterial(true);
        CalibratedArrowGeometry = _visualization3D.CreateArrowGeometry(new Vector3(0, 0, 15), 0, 45);
        CalibratedArrowMaterial = _visualization3D.CreateArrowMaterial(false);
        COArcGeometry = _visualization3D.CreateCOArcGeometry(0, 35);
    }
    
    private void Update3DVisualization()
    {
        if (Result == null || Result.TotalObservations == 0) return;
        
        // Use mean C-O as the angle difference to display
        double meanCO = Result.MeanCOAccepted;
        double referenceHeading = 0; // Reference points North
        double calibratedHeading = -meanCO; // Show the error offset
        
        // Recreate arrows showing the calibration result
        ReferenceArrowGeometry = _visualization3D.CreateArrowGeometry(new Vector3(0, 0, 15), (float)referenceHeading, 50);
        CalibratedArrowGeometry = _visualization3D.CreateArrowGeometry(new Vector3(0, 0, 15), (float)calibratedHeading, 45);
        COArcGeometry = _visualization3D.CreateCOArcGeometry(meanCO, 35);
        
        // Notify all 3D properties
        OnPropertyChanged(nameof(VesselGeometry)); OnPropertyChanged(nameof(SeaPlaneGeometry));
        OnPropertyChanged(nameof(CompassRoseGeometry)); OnPropertyChanged(nameof(ReferenceArrowGeometry));
        OnPropertyChanged(nameof(CalibratedArrowGeometry)); OnPropertyChanged(nameof(COArcGeometry));
    }
    
    private void ResetCamera()
    {
        Camera = new PerspectiveCamera
        {
            Position = new System.Windows.Media.Media3D.Point3D(150, -150, 120),
            LookDirection = new System.Windows.Media.Media3D.Vector3D(-150, 150, -100),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1),
            FieldOfView = 45
        };
        OnPropertyChanged(nameof(Camera));
    }
    
    private void SetTopView()
    {
        Camera = new PerspectiveCamera
        {
            Position = new System.Windows.Media.Media3D.Point3D(0, 0, 200),
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -1),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
            FieldOfView = 45
        };
        OnPropertyChanged(nameof(Camera));
    }

    // Phase 3: Iteration history
    public ObservableCollection<IterationResult> IterationHistory { get; } = new();

    public ICommand ProcessCommand { get; }
    public ICommand RecalculateCommand { get; }

    private async Task ProcessAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Processing data with 3-sigma outlier rejection...";

            await Task.Run(() =>
            {
                var calculator = _mainViewModel.CalculationService;
                var points = _mainViewModel.DataPoints.ToList();
                var result = calculator.Calculate(points, _mainViewModel.Project, _mainViewModel.Project.QcCriteria);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _mainViewModel.Result = result;
                    OnPropertyChanged(nameof(Result));
                    
                    // Phase 3: Update iteration history
                    IterationHistory.Clear();
                    foreach (var iter in result.Iterations)
                        IterationHistory.Add(iter);
                });
            });

            UpdateCharts();
            Update3DVisualization();
            StatusMessage = $"Complete. {Result.AcceptedCount:N0} accepted, {Result.RejectedCount} rejected ({Result.RejectionPercentage:F1}%)";
            IsCompleted = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void UpdateCharts()
    {
        var p = SelectedPalette; // Shorthand for current palette
        
        // C-O Time Series Plot with theme colors
        var coPlot = new PlotModel { Title = "C-O Values Over Time" };
        coPlot.Background = p.Background;
        coPlot.TextColor = p.TextColor;
        coPlot.PlotAreaBorderColor = p.GridLines;
        
        // Configure legend for visibility
        coPlot.IsLegendVisible = true;
        
        coPlot.Axes.Add(new DateTimeAxis 
        { 
            Position = AxisPosition.Bottom, 
            Title = "Time", 
            StringFormat = "HH:mm",
            AxislineColor = p.AxisColor,
            TicklineColor = p.AxisColor,
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });
        coPlot.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Left, 
            Title = "C-O (°)",
            AxislineColor = p.AxisColor,
            TicklineColor = p.AxisColor,
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });

        var acceptedSeries = new ScatterSeries 
        { 
            Title = "Accepted", 
            MarkerType = MarkerType.Circle, 
            MarkerSize = 4, 
            MarkerFill = p.AcceptedPoints,
            MarkerStroke = OxyColor.FromAColor(180, p.AcceptedPoints),
            MarkerStrokeThickness = 1
        };
        var rejectedSeries = new ScatterSeries 
        { 
            Title = "Rejected (Outliers)", 
            MarkerType = MarkerType.Cross, 
            MarkerSize = 5, 
            MarkerFill = p.RejectedPoints,
            MarkerStroke = p.RejectedPoints,
            MarkerStrokeThickness = 2
        };

        foreach (var pt in _mainViewModel.DataPoints)
        {
            var point = new ScatterPoint(DateTimeAxis.ToDouble(pt.Timestamp), pt.CO);
            if (pt.IsRejected) rejectedSeries.Points.Add(point);
            else acceptedSeries.Points.Add(point);
        }

        coPlot.Series.Add(acceptedSeries);
        coPlot.Series.Add(rejectedSeries);

        // Phase 3: Add mean and 3-sigma limit lines
        if (Result.Iterations.Count > 0)
        {
            var lastIter = Result.Iterations.Last();
            
            // Mean line
            coPlot.Annotations.Add(new LineAnnotation 
            { 
                Type = LineAnnotationType.Horizontal, 
                Y = lastIter.MeanCO, 
                Color = p.MeanLine, 
                StrokeThickness = 2,
                LineStyle = LineStyle.Solid,
                Text = $"Mean: {lastIter.MeanCO:F3}°",
                TextColor = p.TextColor
            });
            
            // Upper 3-sigma limit
            coPlot.Annotations.Add(new LineAnnotation 
            { 
                Type = LineAnnotationType.Horizontal, 
                Y = lastIter.UpperLimit, 
                Color = p.UpperLimit, 
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash,
                Text = $"+3σ: {lastIter.UpperLimit:F3}°",
                TextColor = p.TextColor
            });
            
            // Lower 3-sigma limit
            coPlot.Annotations.Add(new LineAnnotation 
            { 
                Type = LineAnnotationType.Horizontal, 
                Y = lastIter.LowerLimit, 
                Color = p.LowerLimit, 
                StrokeThickness = 1,
                LineStyle = LineStyle.Dash,
                Text = $"-3σ: {lastIter.LowerLimit:F3}°",
                TextColor = p.TextColor
            });
        }

        COPlotModel = coPlot;

        // Histogram with theme colors
        var histPlot = new PlotModel { Title = "C-O Distribution" };
        histPlot.Background = p.Background;
        histPlot.TextColor = p.TextColor;
        histPlot.PlotAreaBorderColor = p.GridLines;
        histPlot.IsLegendVisible = true;
        
        histPlot.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Bottom, 
            Title = "C-O (°)",
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });
        histPlot.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Left, 
            Title = "Count",
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });

        var acceptedCO = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).Select(pt => pt.CO).ToList();
        if (acceptedCO.Any())
        {
            var histogram = new HistogramSeries 
            { 
                Title = "C-O Frequency",
                FillColor = p.HistogramBars, 
                StrokeColor = OxyColor.FromAColor(180, p.DataSeries1), 
                StrokeThickness = 1 
            };
            double min = acceptedCO.Min();
            double max = acceptedCO.Max();
            double binWidth = (max - min) / 20;
            if (binWidth > 0)
            {
                for (double x = min; x < max; x += binWidth)
                {
                    int count = acceptedCO.Count(v => v >= x && v < x + binWidth);
                    histogram.Items.Add(new HistogramItem(x, x + binWidth, count, count));
                }
            }
            histPlot.Series.Add(histogram);
            
            // Phase 3: Add mean and ±1σ markers
            if (Result.Iterations.Count > 0)
            {
                var lastIter = Result.Iterations.Last();
                
                // Mean line
                histPlot.Annotations.Add(new LineAnnotation
                {
                    Type = LineAnnotationType.Vertical,
                    X = lastIter.MeanCO,
                    Color = p.MeanLine,
                    StrokeThickness = 2,
                    Text = $"Mean: {lastIter.MeanCO:F3}°",
                    TextColor = p.TextColor
                });
                
                // ±1σ shaded area
                histPlot.Annotations.Add(new RectangleAnnotation
                {
                    MinimumX = lastIter.MeanCO - lastIter.StdDev,
                    MaximumX = lastIter.MeanCO + lastIter.StdDev,
                    Fill = OxyColor.FromArgb(50, 255, 255, 0),
                    Text = "±1σ"
                });
            }
        }

        HistogramModel = histPlot;
        
        // Generate additional charts (Phase 3)
        UpdateQQPlot(acceptedCO);
        UpdateResidualsPlot();
        UpdateBoxPlot();
        UpdatePolarPlot();
        
        // Generate professional charts (v35)
        UpdateControlChart();
        UpdateCusumChart();
        
        // Generate new professional charts (v37)
        UpdateHeadingCoverageChart();
        UpdateMovingAverageChart();
        UpdateScatterPlot();
        UpdateAcfChart();
    }

    private void UpdateQQPlot(List<double> acceptedCO)
    {
        var p = SelectedPalette;
        var qqPlot = new PlotModel { Title = "Q-Q Plot (Normality Check)" };
        qqPlot.Background = p.Background;
        qqPlot.TextColor = p.TextColor;
        qqPlot.PlotAreaBorderColor = p.GridLines;
        qqPlot.IsLegendVisible = true;
        
        qqPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Theoretical Quantiles", TitleColor = p.TextColor, TextColor = p.TextColor, AxislineColor = p.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = p.GridLines });
        qqPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Sample Quantiles (C-O °)", TitleColor = p.TextColor, TextColor = p.TextColor, AxislineColor = p.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = p.GridLines });

        if (acceptedCO.Count > 2)
        {
            var sorted = acceptedCO.OrderBy(x => x).ToList();
            int n = sorted.Count;
            double mean = sorted.Average();
            double stdDev = Math.Sqrt(sorted.Sum(x => Math.Pow(x - mean, 2)) / (n - 1));
            
            var qqSeries = new ScatterSeries { Title = "Data Points", MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = p.DataSeries1, MarkerStroke = OxyColor.FromAColor(180, p.DataSeries1) };
            
            for (int i = 0; i < n; i++)
            {
                double prob = (i + 0.5) / n;
                double theoreticalQuantile = NormInv(prob); // Standard normal quantile
                double sampleQuantile = (sorted[i] - mean) / (stdDev > 0 ? stdDev : 1);
                qqSeries.Points.Add(new ScatterPoint(theoreticalQuantile, sampleQuantile));
            }
            qqPlot.Series.Add(qqSeries);
            
            // Reference line y = x
            qqPlot.Annotations.Add(new LineAnnotation { Slope = 1, Intercept = 0, Color = p.RejectedPoints, StrokeThickness = 1, LineStyle = LineStyle.Dash, Text = "Reference (y=x)", TextColor = p.TextColor });
        }
        
        QQPlotModel = qqPlot;
    }

    private void UpdateResidualsPlot()
    {
        var p = SelectedPalette;
        var resPlot = new PlotModel { Title = "Residuals vs Time" };
        resPlot.Background = p.Background;
        resPlot.TextColor = p.TextColor;
        resPlot.PlotAreaBorderColor = p.GridLines;
        resPlot.IsLegendVisible = true;
        
        resPlot.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Time", StringFormat = "HH:mm", TitleColor = p.TextColor, TextColor = p.TextColor, AxislineColor = p.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = p.GridLines });
        resPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Residual (°)", TitleColor = p.TextColor, TextColor = p.TextColor, AxislineColor = p.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = p.GridLines });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).ToList();
        if (acceptedPoints.Any())
        {
            double mean = acceptedPoints.Average(pt => pt.CO);
            
            var resSeries = new ScatterSeries { Title = "Residuals", MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = p.AcceptedPoints, MarkerStroke = OxyColor.FromAColor(180, p.AcceptedPoints) };
            foreach (var pt in acceptedPoints)
            {
                resSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(pt.Timestamp), pt.CO - mean));
            }
            resPlot.Series.Add(resSeries);
            
            // Zero line
            resPlot.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = 0, Color = p.MeanLine, StrokeThickness = 1, Text = "Zero", TextColor = p.TextColor });
        }
        
        ResidualsPlotModel = resPlot;
    }

    private void UpdateBoxPlot()
    {
        var p = SelectedPalette;
        var boxPlot = new PlotModel { Title = "C-O by Heading Quadrant" };
        boxPlot.Background = p.Background;
        boxPlot.TextColor = p.TextColor;
        boxPlot.PlotAreaBorderColor = p.GridLines;
        
        var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, TitleColor = p.TextColor, TextColor = p.TextColor };
        categoryAxis.Labels.Add("N (315-45°)");
        categoryAxis.Labels.Add("E (45-135°)");
        categoryAxis.Labels.Add("S (135-225°)");
        categoryAxis.Labels.Add("W (225-315°)");
        boxPlot.Axes.Add(categoryAxis);
        boxPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "C-O (°)", TitleColor = p.TextColor, TextColor = p.TextColor, AxislineColor = p.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = p.GridLines });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).ToList();
        var quadrants = new List<List<double>> { new(), new(), new(), new() };
        
        foreach (var pt in acceptedPoints)
        {
            double hdg = pt.ReferenceHeading;
            while (hdg < 0) hdg += 360;
            while (hdg >= 360) hdg -= 360;
            
            int q = hdg switch { >= 315 or < 45 => 0, >= 45 and < 135 => 1, >= 135 and < 225 => 2, _ => 3 };
            quadrants[q].Add(pt.CO);
        }

        var boxSeries = new BoxPlotSeries { Fill = p.DataSeries1, Stroke = p.TextColor };
        for (int i = 0; i < 4; i++)
        {
            if (quadrants[i].Count >= 5)
            {
                var sorted = quadrants[i].OrderBy(x => x).ToList();
                double q1 = sorted[(int)(sorted.Count * 0.25)];
                double median = sorted[(int)(sorted.Count * 0.5)];
                double q3 = sorted[(int)(sorted.Count * 0.75)];
                double min = sorted.First();
                double max = sorted.Last();
                boxSeries.Items.Add(new BoxPlotItem(i, min, q1, median, q3, max));
            }
        }
        boxPlot.Series.Add(boxSeries);
        
        BoxPlotModel = boxPlot;
    }

    private void UpdatePolarPlot()
    {
        var p = SelectedPalette;
        var polarPlot = new PlotModel { Title = "C-O by Heading Direction" };
        polarPlot.Background = p.Background;
        polarPlot.TextColor = p.TextColor;
        polarPlot.PlotAreaBorderColor = p.GridLines;
        polarPlot.IsLegendVisible = true;
        
        // Use a regular scatter plot with transformed coordinates (polar → cartesian)
        polarPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "East-West", TitleColor = p.TextColor, TextColor = p.TextColor, AxislineColor = p.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = p.GridLines });
        polarPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "North-South", TitleColor = p.TextColor, TextColor = p.TextColor, AxislineColor = p.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = p.GridLines });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).ToList();
        if (acceptedPoints.Any())
        {
            double maxCO = acceptedPoints.Max(pt => Math.Abs(pt.CO));
            if (maxCO < 0.001) maxCO = 1;

            var polarSeries = new ScatterSeries { Title = "C-O Points", MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = p.AcceptedPoints, MarkerStroke = OxyColor.FromAColor(180, p.AcceptedPoints) };
            foreach (var pt in acceptedPoints)
            {
                double radians = (90 - pt.ReferenceHeading) * Math.PI / 180; // Convert heading to math angle
                double radius = Math.Abs(pt.CO) / maxCO; // Normalize
                double x = radius * Math.Cos(radians);
                double y = radius * Math.Sin(radians);
                polarSeries.Points.Add(new ScatterPoint(x, y) { Value = pt.CO });
            }
            polarPlot.Series.Add(polarSeries);
            
            // Add compass directions
            polarPlot.Annotations.Add(new TextAnnotation { Text = "N", TextPosition = new DataPoint(0, 1.1), TextColor = p.TextColor });
            polarPlot.Annotations.Add(new TextAnnotation { Text = "E", TextPosition = new DataPoint(1.1, 0), TextColor = p.TextColor });
            polarPlot.Annotations.Add(new TextAnnotation { Text = "S", TextPosition = new DataPoint(0, -1.1), TextColor = p.TextColor });
            polarPlot.Annotations.Add(new TextAnnotation { Text = "W", TextPosition = new DataPoint(-1.1, 0), TextColor = p.TextColor });
        }
        
        PolarPlotModel = polarPlot;
    }

    /// <summary>
    /// Individual Control Chart (I-Chart) with Moving Range
    /// Industry standard SPC chart showing process stability
    /// </summary>
    private void UpdateControlChart()
    {
        var p = SelectedPalette;
        var controlChart = new PlotModel { Title = "Individual Control Chart (I-Chart)" };
        ApplyTheme(controlChart, p);
        
        controlChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Bottom, 
            Title = "Observation #", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines,
            Minimum = 0
        });
        controlChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Left, 
            Title = "C-O (°)", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).OrderBy(pt => pt.Timestamp).ToList();
        if (acceptedPoints.Count < 3)
        {
            ControlChartModel = controlChart;
            return;
        }

        // Calculate control limits using moving range
        var coValues = acceptedPoints.Select(pt => pt.CO).ToList();
        double mean = coValues.Average();
        
        // Calculate moving ranges (MR)
        var movingRanges = new List<double>();
        for (int i = 1; i < coValues.Count; i++)
        {
            movingRanges.Add(Math.Abs(coValues[i] - coValues[i - 1]));
        }
        double avgMR = movingRanges.Average();
        
        // Control limits: UCL = X̄ + 2.66*MR̄, LCL = X̄ - 2.66*MR̄ (d2 = 1.128 for n=2)
        double ucl = mean + 2.66 * avgMR;
        double lcl = mean - 2.66 * avgMR;
        
        // Warning limits (±2σ): X̄ ± 1.77*MR̄
        double uwl = mean + 1.77 * avgMR;
        double lwl = mean - 1.77 * avgMR;

        // Data series
        var dataSeries = new LineSeries 
        { 
            Title = "C-O Values", 
            Color = p.DataSeries1,
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = p.DataSeries1
        };
        
        for (int i = 0; i < coValues.Count; i++)
        {
            dataSeries.Points.Add(new DataPoint(i + 1, coValues[i]));
        }
        controlChart.Series.Add(dataSeries);

        // Center line (mean)
        controlChart.Annotations.Add(new LineAnnotation 
        { 
            Type = LineAnnotationType.Horizontal, 
            Y = mean, 
            Color = p.MeanLine, 
            StrokeThickness = 2,
            Text = $"CL: {mean:F4}°",
            TextColor = p.TextColor
        });

        // Upper Control Limit
        controlChart.Annotations.Add(new LineAnnotation 
        { 
            Type = LineAnnotationType.Horizontal, 
            Y = ucl, 
            Color = p.UpperLimit, 
            StrokeThickness = 2,
            LineStyle = LineStyle.Solid,
            Text = $"UCL: {ucl:F4}°",
            TextColor = p.TextColor
        });

        // Lower Control Limit
        controlChart.Annotations.Add(new LineAnnotation 
        { 
            Type = LineAnnotationType.Horizontal, 
            Y = lcl, 
            Color = p.LowerLimit, 
            StrokeThickness = 2,
            LineStyle = LineStyle.Solid,
            Text = $"LCL: {lcl:F4}°",
            TextColor = p.TextColor
        });

        // Upper Warning Limit (±2σ)
        controlChart.Annotations.Add(new LineAnnotation 
        { 
            Type = LineAnnotationType.Horizontal, 
            Y = uwl, 
            Color = p.WarningLine, 
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            Text = "UWL (2σ)",
            TextColor = p.TextColor
        });

        // Lower Warning Limit
        controlChart.Annotations.Add(new LineAnnotation 
        { 
            Type = LineAnnotationType.Horizontal, 
            Y = lwl, 
            Color = p.WarningLine, 
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            Text = "LWL (2σ)",
            TextColor = p.TextColor
        });

        ControlChartModel = controlChart;
    }

    /// <summary>
    /// CUSUM Chart - Cumulative Sum for detecting drift/trends
    /// Shows cumulative deviation from target (useful for detecting gradual changes)
    /// </summary>
    private void UpdateCusumChart()
    {
        var p = SelectedPalette;
        var cusumChart = new PlotModel { Title = "CUSUM Chart (Trend Detection)" };
        ApplyTheme(cusumChart, p);
        
        cusumChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Bottom, 
            Title = "Observation #", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines,
            Minimum = 0
        });
        cusumChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Left, 
            Title = "Cumulative Sum (°)", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).OrderBy(pt => pt.Timestamp).ToList();
        if (acceptedPoints.Count < 3)
        {
            CusumChartModel = cusumChart;
            return;
        }

        var coValues = acceptedPoints.Select(pt => pt.CO).ToList();
        double target = coValues.Average(); // Use mean as target

        // Calculate CUSUM (cumulative sum of deviations from target)
        var cusumPlus = new List<double> { 0 };
        var cusumMinus = new List<double> { 0 };
        double cPlus = 0, cMinus = 0;
        double k = 0.5; // Slack value (typically 0.5σ)
        double stdDev = Math.Sqrt(coValues.Sum(x => Math.Pow(x - target, 2)) / (coValues.Count - 1));
        double slack = k * stdDev;

        for (int i = 0; i < coValues.Count; i++)
        {
            double deviation = coValues[i] - target;
            cPlus = Math.Max(0, cPlus + deviation - slack);
            cMinus = Math.Min(0, cMinus + deviation + slack);
            cusumPlus.Add(cPlus);
            cusumMinus.Add(cMinus);
        }

        // CUSUM+ series (detects upward shifts)
        var cusumPlusSeries = new LineSeries 
        { 
            Title = "CUSUM+ (↑ shift)", 
            Color = p.RejectedPoints,
            StrokeThickness = 2
        };
        for (int i = 0; i < cusumPlus.Count; i++)
        {
            cusumPlusSeries.Points.Add(new DataPoint(i, cusumPlus[i]));
        }
        cusumChart.Series.Add(cusumPlusSeries);

        // CUSUM- series (detects downward shifts)
        var cusumMinusSeries = new LineSeries 
        { 
            Title = "CUSUM- (↓ shift)", 
            Color = p.DataSeries1,
            StrokeThickness = 2
        };
        for (int i = 0; i < cusumMinus.Count; i++)
        {
            cusumMinusSeries.Points.Add(new DataPoint(i, cusumMinus[i]));
        }
        cusumChart.Series.Add(cusumMinusSeries);

        // Decision interval H (typically 4-5σ)
        double h = 4 * stdDev;
        cusumChart.Annotations.Add(new LineAnnotation 
        { 
            Type = LineAnnotationType.Horizontal, 
            Y = h, 
            Color = p.WarningLine, 
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            Text = $"H = {h:F3}°",
            TextColor = p.TextColor
        });
        cusumChart.Annotations.Add(new LineAnnotation 
        { 
            Type = LineAnnotationType.Horizontal, 
            Y = -h, 
            Color = p.WarningLine, 
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            Text = $"-H = {-h:F3}°",
            TextColor = p.TextColor
        });

        // Zero line
        cusumChart.Annotations.Add(new LineAnnotation 
        { 
            Type = LineAnnotationType.Horizontal, 
            Y = 0, 
            Color = p.MeanLine, 
            StrokeThickness = 1,
            TextColor = p.TextColor
        });

        CusumChartModel = cusumChart;
    }

    /// <summary>
    /// Heading Coverage Rose Chart - Polar histogram showing data distribution by heading direction
    /// Critical for ensuring adequate heading coverage during calibration
    /// </summary>
    private void UpdateHeadingCoverageChart()
    {
        var p = SelectedPalette;
        var coverageChart = new PlotModel { Title = "Heading Coverage Distribution" };
        ApplyTheme(coverageChart, p);
        
        // For OxyPlot 2.x: Use CategoryAxis on Left for horizontal bars
        var categoryAxis = new CategoryAxis 
        { 
            Position = AxisPosition.Left, 
            Title = "Heading Sector",
            TitleColor = p.TextColor,
            TextColor = p.TextColor
        };
        
        // 12 sectors of 30° each (like a clock)
        string[] sectorLabels = { "N", "NNE", "ENE", "E", "ESE", "SSE", "S", "SSW", "WSW", "W", "WNW", "NNW" };
        foreach (var label in sectorLabels)
            categoryAxis.Labels.Add(label);
        coverageChart.Axes.Add(categoryAxis);
        
        coverageChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Bottom, 
            Title = "Count",
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines,
            Minimum = 0
        });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).ToList();
        if (acceptedPoints.Count == 0)
        {
            HeadingCoverageModel = coverageChart;
            return;
        }

        // Count points in each 30° sector
        var sectorCounts = new int[12];
        foreach (var pt in acceptedPoints)
        {
            double hdg = pt.ReferenceHeading;
            while (hdg < 0) hdg += 360;
            while (hdg >= 360) hdg -= 360;
            int sector = (int)((hdg + 15) / 30) % 12; // +15 to center sectors
            sectorCounts[sector]++;
        }

        var barSeries = new BarSeries
        {
            FillColor = p.DataSeries1,
            StrokeColor = OxyColor.FromAColor(200, p.DataSeries1),
            StrokeThickness = 1
        };

        for (int i = 0; i < 12; i++)
        {
            var color = sectorCounts[i] == 0 ? p.RejectedPoints : // Red for empty sectors
                        sectorCounts[i] < 5 ? p.WarningLine : // Yellow for sparse
                        p.AcceptedPoints; // Green for good coverage
            barSeries.Items.Add(new BarItem(sectorCounts[i]) { Color = color });
        }
        coverageChart.Series.Add(barSeries);

        // Add minimum recommended line (vertical since axis is swapped)
        double minRecommended = acceptedPoints.Count / 24.0; // ~4% per sector for even distribution
        coverageChart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            X = minRecommended,
            Color = p.WarningLine,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            Text = "Min Recommended",
            TextColor = p.TextColor
        });

        HeadingCoverageModel = coverageChart;
    }

    /// <summary>
    /// Moving Average Chart with ±2σ Envelope
    /// Shows smoothed trend with confidence bands for identifying systematic drift
    /// </summary>
    private void UpdateMovingAverageChart()
    {
        var p = SelectedPalette;
        var maChart = new PlotModel { Title = "Moving Average with ±2σ Bands" };
        ApplyTheme(maChart, p);
        
        maChart.Axes.Add(new DateTimeAxis 
        { 
            Position = AxisPosition.Bottom, 
            Title = "Time", 
            StringFormat = "HH:mm",
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });
        maChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Left, 
            Title = "C-O (°)", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).OrderBy(pt => pt.Timestamp).ToList();
        if (acceptedPoints.Count < 10)
        {
            MovingAverageModel = maChart;
            return;
        }

        var coValues = acceptedPoints.Select(pt => pt.CO).ToList();
        int windowSize = Math.Min(10, acceptedPoints.Count / 5); // 10-point window or 20% of data
        if (windowSize < 3) windowSize = 3;

        // Calculate moving average and standard deviation
        var maValues = new List<(DateTime time, double ma, double upperBand, double lowerBand)>();
        for (int i = windowSize - 1; i < acceptedPoints.Count; i++)
        {
            var window = coValues.Skip(i - windowSize + 1).Take(windowSize).ToList();
            double mean = window.Average();
            double stdDev = Math.Sqrt(window.Sum(x => Math.Pow(x - mean, 2)) / (window.Count - 1));
            maValues.Add((acceptedPoints[i].Timestamp, mean, mean + 2 * stdDev, mean - 2 * stdDev));
        }

        // Raw data series
        var rawSeries = new ScatterSeries
        {
            Title = "C-O Values",
            MarkerType = MarkerType.Circle,
            MarkerSize = 2,
            MarkerFill = OxyColor.FromAColor(100, p.DataSeries1)
        };
        foreach (var pt in acceptedPoints)
        {
            rawSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(pt.Timestamp), pt.CO));
        }
        maChart.Series.Add(rawSeries);

        // Moving average line
        var maSeries = new LineSeries
        {
            Title = $"MA({windowSize})",
            Color = p.AcceptedPoints,
            StrokeThickness = 2
        };
        foreach (var v in maValues)
        {
            maSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(v.time), v.ma));
        }
        maChart.Series.Add(maSeries);

        // Upper band (area series)
        var upperBand = new LineSeries
        {
            Title = "+2σ",
            Color = OxyColor.FromAColor(150, p.WarningLine),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash
        };
        foreach (var v in maValues)
        {
            upperBand.Points.Add(new DataPoint(DateTimeAxis.ToDouble(v.time), v.upperBand));
        }
        maChart.Series.Add(upperBand);

        // Lower band
        var lowerBand = new LineSeries
        {
            Title = "-2σ",
            Color = OxyColor.FromAColor(150, p.WarningLine),
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash
        };
        foreach (var v in maValues)
        {
            lowerBand.Points.Add(new DataPoint(DateTimeAxis.ToDouble(v.time), v.lowerBand));
        }
        maChart.Series.Add(lowerBand);

        MovingAverageModel = maChart;
    }

    /// <summary>
    /// C-O vs Heading Scatter Plot
    /// Critical for detecting heading-dependent biases in calibration
    /// </summary>
    private void UpdateScatterPlot()
    {
        var p = SelectedPalette;
        var scatterChart = new PlotModel { Title = "C-O vs Heading (Bias Detection)" };
        ApplyTheme(scatterChart, p);
        
        scatterChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Bottom, 
            Title = "Reference Heading (°)", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines,
            Minimum = 0,
            Maximum = 360
        });
        scatterChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Left, 
            Title = "C-O (°)", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).ToList();
        if (acceptedPoints.Count == 0)
        {
            ScatterPlotModel = scatterChart;
            return;
        }

        // Scatter series
        var scatterSeries = new ScatterSeries
        {
            Title = "C-O vs Heading",
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = p.DataSeries1,
            MarkerStroke = OxyColor.FromAColor(180, p.DataSeries1)
        };
        foreach (var pt in acceptedPoints)
        {
            double hdg = pt.ReferenceHeading;
            while (hdg < 0) hdg += 360;
            while (hdg >= 360) hdg -= 360;
            scatterSeries.Points.Add(new ScatterPoint(hdg, pt.CO));
        }
        scatterChart.Series.Add(scatterSeries);

        // Mean line
        double mean = acceptedPoints.Average(pt => pt.CO);
        scatterChart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = mean,
            Color = p.MeanLine,
            StrokeThickness = 2,
            Text = $"Mean: {mean:F4}°",
            TextColor = p.TextColor
        });

        // Fit sine curve to detect heading-dependent bias
        // Using simple regression: CO = A + B*sin(heading) + C*cos(heading)
        var x1 = acceptedPoints.Select(pt => Math.Sin(pt.ReferenceHeading * Math.PI / 180)).ToList();
        var x2 = acceptedPoints.Select(pt => Math.Cos(pt.ReferenceHeading * Math.PI / 180)).ToList();
        var y = acceptedPoints.Select(pt => pt.CO).ToList();
        
        // Simple least squares for A, B, C
        double n = y.Count;
        double sumY = y.Sum();
        double sumX1 = x1.Sum();
        double sumX2 = x2.Sum();
        double sumX1Y = x1.Zip(y, (a, b) => a * b).Sum();
        double sumX2Y = x2.Zip(y, (a, b) => a * b).Sum();
        double sumX1X2 = x1.Zip(x2, (a, b) => a * b).Sum();
        double sumX1Sq = x1.Sum(x => x * x);
        double sumX2Sq = x2.Sum(x => x * x);
        
        // Simplified: just use mean and estimate amplitude
        double avgSinCoeff = sumX1Y / n - mean * sumX1 / n;
        double avgCosCoeff = sumX2Y / n - mean * sumX2 / n;
        double amplitude = Math.Sqrt(avgSinCoeff * avgSinCoeff + avgCosCoeff * avgCosCoeff);
        
        if (amplitude > 0.01) // Only show if significant heading-dependent bias
        {
            var fitSeries = new LineSeries
            {
                Title = $"Heading Bias (Amp: {amplitude:F3}°)",
                Color = p.RejectedPoints,
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash
            };
            for (double hdg = 0; hdg <= 360; hdg += 5)
            {
                double fitValue = mean + avgSinCoeff * Math.Sin(hdg * Math.PI / 180) + avgCosCoeff * Math.Cos(hdg * Math.PI / 180);
                fitSeries.Points.Add(new DataPoint(hdg, fitValue));
            }
            scatterChart.Series.Add(fitSeries);
        }

        ScatterPlotModel = scatterChart;
    }

    /// <summary>
    /// Autocorrelation Function (ACF) Chart
    /// Checks for serial correlation in residuals - essential for validating independence assumption
    /// </summary>
    private void UpdateAcfChart()
    {
        var p = SelectedPalette;
        var acfChart = new PlotModel { Title = "Autocorrelation Function (Independence Check)" };
        ApplyTheme(acfChart, p);
        
        acfChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Bottom, 
            Title = "Lag", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines,
            Minimum = 0
        });
        acfChart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Left, 
            Title = "ACF", 
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            AxislineColor = p.AxisColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines,
            Minimum = -1,
            Maximum = 1
        });

        var acceptedPoints = _mainViewModel.DataPoints.Where(pt => !pt.IsRejected).OrderBy(pt => pt.Timestamp).ToList();
        if (acceptedPoints.Count < 20)
        {
            AcfPlotModel = acfChart;
            return;
        }

        var coValues = acceptedPoints.Select(pt => pt.CO).ToList();
        double mean = coValues.Average();
        int nPoints = coValues.Count;
        int maxLag = Math.Min(30, nPoints / 4); // Up to 30 lags or 25% of data
        
        // Calculate variance
        double variance = coValues.Sum(x => Math.Pow(x - mean, 2)) / nPoints;
        
        // Calculate ACF for each lag
        var acfSeries = new StemSeries
        {
            Title = "ACF",
            Color = p.DataSeries1,
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = p.DataSeries1
        };
        
        for (int lag = 0; lag <= maxLag; lag++)
        {
            double covariance = 0;
            for (int i = 0; i < nPoints - lag; i++)
            {
                covariance += (coValues[i] - mean) * (coValues[i + lag] - mean);
            }
            covariance /= nPoints;
            double acf = covariance / variance;
            acfSeries.Points.Add(new DataPoint(lag, acf));
        }
        acfChart.Series.Add(acfSeries);

        // 95% confidence bounds: ±1.96/√n
        double bound = 1.96 / Math.Sqrt(nPoints);
        acfChart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = bound,
            Color = p.UpperLimit,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            Text = "95% CI",
            TextColor = p.TextColor
        });
        acfChart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = -bound,
            Color = p.LowerLimit,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash,
            TextColor = p.TextColor
        });
        acfChart.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Horizontal,
            Y = 0,
            Color = p.MeanLine,
            StrokeThickness = 1,
            TextColor = p.TextColor
        });

        AcfPlotModel = acfChart;
    }

    /// <summary>Apply consistent dark theme to charts</summary>
    private static void ApplyTheme(PlotModel plot, ChartColorPalette p)
    {
        plot.Background = p.Background;
        plot.TextColor = p.TextColor;
        plot.PlotAreaBorderColor = p.GridLines;
        plot.IsLegendVisible = true;
        // Note: In OxyPlot 2.x, legend styling is handled automatically
    }

    // Normal inverse function (approximation)
    private static double NormInv(double prob)
    {
        if (prob <= 0) return double.NegativeInfinity;
        if (prob >= 1) return double.PositiveInfinity;
        
        // Rational approximation for lower region
        double[] a = { -3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02, 1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00 };
        double[] b = { -5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02, 6.680131188771972e+01, -1.328068155288572e+01 };
        double[] c = { -7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00, -2.549732539343734e+00, 4.374664141464968e+00, 2.938163982698783e+00 };
        double[] d = { 7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00, 3.754408661907416e+00 };

        double pLow = 0.02425, pHigh = 1 - pLow;
        double q, r;

        if (prob < pLow)
        {
            q = Math.Sqrt(-2 * Math.Log(prob));
            return (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }
        else if (prob <= pHigh)
        {
            q = prob - 0.5;
            r = q * q;
            return (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q / (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
        }
        else
        {
            q = Math.Sqrt(-2 * Math.Log(1 - prob));
            return -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) / ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }
    }

    public override bool Validate() => Result.TotalObservations > 0;
}

#endregion

#region Step 6: Validate

/// <summary>
/// Step 6: QC checks and validation.
/// </summary>
public class Step6ValidateViewModel : WizardStepViewModelBase
{
    private string _statusMessage = "";
    private string _notes = "";

    public Step6ValidateViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        RunValidationCommand = new RelayCommand(_ => RunValidation());
        AcceptCommand = new RelayCommand(_ => AcceptResults());
        RejectCommand = new RelayCommand(_ => RejectResults());
    }

    public override void OnActivated()
    {
        base.OnActivated();
        RunValidation();
    }

    // Core properties
    public ValidationResult Validation => _mainViewModel.Validation;
    public CalibrationResult Result => _mainViewModel.Result;
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    
    // Pass-through properties for XAML binding
    public ObservableCollection<QcCheck> QcChecks => Validation?.Checks ?? new();
    public QcStatus OverallStatus => Validation?.OverallStatus ?? QcStatus.NotChecked;
    public string OverallStatusText => Validation?.OverallStatusText ?? "NOT CHECKED";
    public double MeanCO => Result?.MeanCOAccepted ?? 0;
    
    // Summary counts
    public int PassedChecks => Validation?.PassCount ?? 0;
    public int TotalChecks => Validation?.Checks.Count ?? 0;
    public int WarningChecks => Validation?.WarningCount ?? 0;
    public int FailedChecks => Validation?.FailCount ?? 0;
    
    // Mode guidance
    public string ModeGuidanceTitle => _mainViewModel.Project.Purpose == ExercisePurpose.Calibration 
        ? "Calibration Mode" : "Verification Mode";
    
    public string ModeGuidance => _mainViewModel.Project.Purpose == ExercisePurpose.Calibration 
        ? "Apply the Mean C-O value as the correction in your navigation system. The system gyro will then read correctly relative to the reference."
        : "A Mean C-O close to 0° indicates the existing correction is valid. Values significantly different from 0° suggest the correction may need updating.";
    
    public string ResultInterpretation
    {
        get
        {
            if (Result == null) return "No results available.";
            var mean = Result.MeanCOAccepted;
            var stdDev = Result.StdDevAccepted;
            
            if (_mainViewModel.Project.Purpose == ExercisePurpose.Verification)
            {
                if (Math.Abs(mean) < 0.1)
                    return $"Mean C-O of {mean:F3}° is very close to zero. The existing correction is valid.";
                if (Math.Abs(mean) < 0.5)
                    return $"Mean C-O of {mean:F3}° is acceptable. Consider monitoring for drift.";
                return $"Mean C-O of {mean:F3}° suggests the correction needs updating.";
            }
            else
            {
                return $"Apply C-O correction of {mean:F3}° (±{stdDev:F3}°) to the system gyro.";
            }
        }
    }
    
    // Notes
    public string Notes
    {
        get => _notes;
        set
        {
            if (SetProperty(ref _notes, value) && Validation != null)
                Validation.Notes = value;
        }
    }

    // Commands
    public ICommand RunValidationCommand { get; }
    public ICommand AcceptCommand { get; }
    public ICommand RejectCommand { get; }

    private void RunValidation()
    {
        var calculator = _mainViewModel.CalculationService;
        _mainViewModel.Validation = calculator.ValidateResults(Result, _mainViewModel.Project);
        
        // Notify all properties
        OnPropertyChanged(nameof(Validation));
        OnPropertyChanged(nameof(QcChecks));
        OnPropertyChanged(nameof(OverallStatus));
        OnPropertyChanged(nameof(OverallStatusText));
        OnPropertyChanged(nameof(MeanCO));
        OnPropertyChanged(nameof(PassedChecks));
        OnPropertyChanged(nameof(TotalChecks));
        OnPropertyChanged(nameof(WarningChecks));
        OnPropertyChanged(nameof(FailedChecks));
        OnPropertyChanged(nameof(ResultInterpretation));

        StatusMessage = $"Validation complete: {Validation.OverallStatusText}";
        IsCompleted = true;
    }
    
    private void AcceptResults()
    {
        if (Validation != null)
        {
            Validation.Decision = "ACCEPTED";
            Validation.DecisionTime = DateTime.Now;
        }
        StatusMessage = "Results accepted. Proceed to export.";
    }
    
    private void RejectResults()
    {
        if (Validation != null)
        {
            Validation.Decision = "REJECTED";
            Validation.DecisionTime = DateTime.Now;
        }
        StatusMessage = "Results rejected. Re-test recommended.";
    }

    public override bool Validate() => true;
}

#endregion

#region Step 7: Export

/// <summary>
/// Step 7: Export reports and data.
/// </summary>
public class Step7ExportViewModel : WizardStepViewModelBase
{
    private bool _exportPdf = true;
    private bool _exportExcel = true;
    private bool _exportCsv;
    private string _outputDirectory = "";
    private string _statusMessage = "";
    private bool _isExporting;

    public Step7ExportViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        BrowseOutputCommand = new RelayCommand(_ => BrowseOutput());
        ExportCommand = new AsyncRelayCommand(async _ => await ExportAsync(), _ => CanExport);
        OpenOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder());

        // Default output directory
        OutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public bool ExportPdf { get => _exportPdf; set => SetProperty(ref _exportPdf, value); }
    public bool ExportExcel { get => _exportExcel; set => SetProperty(ref _exportExcel, value); }
    public bool ExportCsv { get => _exportCsv; set => SetProperty(ref _exportCsv, value); }
    public string OutputDirectory { get => _outputDirectory; set { SetProperty(ref _outputDirectory, value); OnPropertyChanged(nameof(CanExport)); } }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsExporting { get => _isExporting; set => SetProperty(ref _isExporting, value); }
    public bool CanExport => !string.IsNullOrEmpty(OutputDirectory) && (ExportPdf || ExportExcel || ExportCsv);

    public CalibrationResult Result => _mainViewModel.Result;
    public ValidationResult Validation => _mainViewModel.Validation;
    
    // Summary properties for display
    public string Decision => _mainViewModel.Validation?.Decision ?? "PENDING";
    public double MeanCO => _mainViewModel.Result?.MeanCOAccepted ?? 0;
    public double StdDev => _mainViewModel.Result?.SDCOAccepted ?? 0;
    public int AcceptedCount => _mainViewModel.Result?.AcceptedCount ?? 0;
    public int TotalCount => _mainViewModel.Result?.TotalObservations ?? 0;

    public ICommand BrowseOutputCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }

    private void BrowseOutput()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select Output Folder",
            SelectedPath = OutputDirectory
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            OutputDirectory = dialog.SelectedPath;
        }
    }
    
    private void OpenOutputFolder()
    {
        try
        {
            if (System.IO.Directory.Exists(OutputDirectory))
                System.Diagnostics.Process.Start("explorer.exe", OutputDirectory);
        }
        catch { }
    }

    private async Task ExportAsync()
    {
        try
        {
            IsExporting = true;
            await _mainViewModel.ExportReportAsync(OutputDirectory, ExportPdf, ExportExcel, ExportCsv);
            StatusMessage = "Export complete!";
            IsCompleted = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    public override bool Validate() => true;
}

#endregion

// WizardSteps is defined in ViewModelBase.cs
