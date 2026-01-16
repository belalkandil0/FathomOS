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
using FathomOS.Modules.RovGyroCalibration.Models;
using FathomOS.Modules.RovGyroCalibration.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Annotations;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;

namespace FathomOS.Modules.RovGyroCalibration.ViewModels.Steps;

#region Step 1: Project Setup with ROV Geometric Configuration

/// <summary>
/// Step 1: Project setup with ROV geometric configuration.
/// Clean UI without 3D preview (3D moved to Step 5 for results visualization).
/// </summary>
public class Step1SetupViewModel : WizardStepViewModelBase
{
    // ROV Configuration
    private double _portMeasurement = 0.0;
    private double _starboardMeasurement = 0.0;
    private double _fwdMeasurement = 0.0;
    private double _aftMeasurement = 0.0;
    private bool _useFwdAftMeasurements = true;
    private double _baselineOffset = 0.0;
    private double _baselineDistance = 10.0;

    public Step1SetupViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        // Initialize facing direction options for dropdown
        FacingDirectionOptions = new List<FacingDirectionOption>
        {
            new FacingDirectionOption { Direction = RovFacingDirection.Forward, DisplayName = "Forward (0°)" },
            new FacingDirectionOption { Direction = RovFacingDirection.Starboard, DisplayName = "Starboard (+90°)" },
            new FacingDirectionOption { Direction = RovFacingDirection.Aft, DisplayName = "Aft (180°)" },
            new FacingDirectionOption { Direction = RovFacingDirection.Port, DisplayName = "Port (-90°)" }
        };
        _selectedFacingDirection = FacingDirectionOptions[0];
    }

    public CalibrationProject Project => _mainViewModel.Project;
    public RovConfiguration RovConfig => _mainViewModel.Project.RovConfig;

    #region Project Properties
    public string ProjectName 
    { 
        get => Project.ProjectName; 
        set { Project.ProjectName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProceed)); _mainViewModel.RaiseCanGoNextChanged(); } 
    }
    public string VesselName 
    { 
        get => Project.VesselName; 
        set { Project.VesselName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProceed)); _mainViewModel.RaiseCanGoNextChanged(); } 
    }
    public string RovName 
    { 
        get => Project.RovName; 
        set { Project.RovName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProceed)); _mainViewModel.RaiseCanGoNextChanged(); } 
    }
    public string ObservedBy 
    { 
        get => Project.SurveyorName; 
        set { Project.SurveyorName = value; OnPropertyChanged(); } 
    }
    public DateTime SurveyDate 
    { 
        get => Project.SurveyDate; 
        set { Project.SurveyDate = value; OnPropertyChanged(); } 
    }
    #endregion

    #region Mode Selection
    public bool IsCalibrationMode
    {
        get => Project.Purpose == CalibrationPurpose.Calibration;
        set { if (value) { Project.Purpose = CalibrationPurpose.Calibration; OnPropertyChanged(); OnPropertyChanged(nameof(IsVerificationMode)); } }
    }

    public bool IsVerificationMode
    {
        get => Project.Purpose == CalibrationPurpose.Verification;
        set { if (value) { Project.Purpose = CalibrationPurpose.Verification; OnPropertyChanged(); OnPropertyChanged(nameof(IsCalibrationMode)); } }
    }
    #endregion

    #region Units
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
            // Update all distance label properties when unit changes
            OnPropertyChanged(nameof(BaselineDistanceLabel));
            OnPropertyChanged(nameof(PortLabel));
            OnPropertyChanged(nameof(StarboardLabel));
            OnPropertyChanged(nameof(ForwardLabel));
            OnPropertyChanged(nameof(AftLabel));
            OnPropertyChanged(nameof(OffsetLabel));
        }
    }
    
    public string UnitAbbreviation => UnitConversionService.GetAbbreviation(SelectedUnit);
    
    // Dynamic labels that update when unit changes
    public string BaselineDistanceLabel => $"Baseline Distance ({UnitAbbreviation})";
    public string PortLabel => $"Port ({UnitAbbreviation})";
    public string StarboardLabel => $"Starboard ({UnitAbbreviation})";
    public string ForwardLabel => $"Forward ({UnitAbbreviation})";
    public string AftLabel => $"Aft ({UnitAbbreviation})";
    public string OffsetLabel => $"Baseline Offset (°)";
    
    public bool RoundingEnabled
    {
        get => Project.RoundingEnabled;
        set { Project.RoundingEnabled = value; OnPropertyChanged(); }
    }
    #endregion

    #region ROV Geometric Configuration
    
    public class FacingDirectionOption
    {
        public RovFacingDirection Direction { get; set; }
        public string DisplayName { get; set; } = "";
    }
    
    public List<FacingDirectionOption> FacingDirectionOptions { get; }
    
    private FacingDirectionOption _selectedFacingDirection;
    public FacingDirectionOption SelectedFacingDirection
    {
        get => _selectedFacingDirection;
        set
        {
            if (SetProperty(ref _selectedFacingDirection, value) && value != null)
            {
                RovConfig.FacingDirection = value.Direction;
                OnPropertyChanged(nameof(FacingDirectionOffset));
                OnPropertyChanged(nameof(TotalCorrection));
            }
        }
    }

    public double PortMeasurement
    {
        get => _portMeasurement;
        set
        {
            if (SetProperty(ref _portMeasurement, value))
            {
                RovConfig.PortMeasurement = value;
                OnPropertyChanged(nameof(CalculatedTheta));
                OnPropertyChanged(nameof(TotalCorrection));
            }
        }
    }

    public double StarboardMeasurement
    {
        get => _starboardMeasurement;
        set
        {
            if (SetProperty(ref _starboardMeasurement, value))
            {
                RovConfig.StarboardMeasurement = value;
                OnPropertyChanged(nameof(CalculatedTheta));
                OnPropertyChanged(nameof(TotalCorrection));
            }
        }
    }

    public double ForwardMeasurement
    {
        get => _fwdMeasurement;
        set
        {
            if (SetProperty(ref _fwdMeasurement, value))
            {
                RovConfig.FwdMeasurement = value;
                OnPropertyChanged(nameof(CalculatedTheta));
                OnPropertyChanged(nameof(TotalCorrection));
            }
        }
    }

    public double AftMeasurement
    {
        get => _aftMeasurement;
        set
        {
            if (SetProperty(ref _aftMeasurement, value))
            {
                RovConfig.AftMeasurement = value;
                OnPropertyChanged(nameof(CalculatedTheta));
                OnPropertyChanged(nameof(TotalCorrection));
            }
        }
    }

    public bool UseFwdAftMeasurements
    {
        get => _useFwdAftMeasurements;
        set
        {
            if (SetProperty(ref _useFwdAftMeasurements, value))
            {
                RovConfig.UseFwdAftMeasurements = value;
                OnPropertyChanged(nameof(CalculatedTheta));
                OnPropertyChanged(nameof(TotalCorrection));
                OnPropertyChanged(nameof(MeasurementModeText));
            }
        }
    }
    
    public string MeasurementModeText => UseFwdAftMeasurements 
        ? "Forward/Aft Mode (Fwd/Aft distances)" 
        : "Port/Starboard Mode (P/S distances)";

    public double BaselineOffset
    {
        get => _baselineOffset;
        set
        {
            if (SetProperty(ref _baselineOffset, value))
            {
                RovConfig.BaselineOffset = value;
                OnPropertyChanged(nameof(TotalCorrection));
            }
        }
    }

    public double BaselineDistance
    {
        get => _baselineDistance;
        set
        {
            if (SetProperty(ref _baselineDistance, value))
            {
                RovConfig.BaselineDistance = value;
                OnPropertyChanged(nameof(CalculatedTheta));
                OnPropertyChanged(nameof(TotalCorrection));
            }
        }
    }

    /// <summary>Calculated baseline angle θ from measurements</summary>
    public double CalculatedTheta => RovConfig.CalculateBaselineAngle();

    /// <summary>Facing direction offset in degrees</summary>
    public double FacingDirectionOffset => RovConfig.FacingDirectionOffset;

    /// <summary>Total correction: D + θ + FacingOffset</summary>
    public double TotalCorrection => BaselineOffset + CalculatedTheta + FacingDirectionOffset;

    #endregion

    public override bool Validate()
    {
        ValidationMessage = "";
        if (string.IsNullOrWhiteSpace(Project.ProjectName))
        { ValidationMessage = "Please enter a project name."; return false; }
        if (string.IsNullOrWhiteSpace(Project.VesselName))
        { ValidationMessage = "Please enter a vessel name."; return false; }
        if (string.IsNullOrWhiteSpace(Project.RovName))
        { ValidationMessage = "Please enter an ROV name."; return false; }
        return true;
    }
}

#endregion

#region Step 2: Load File

public class Step2ImportViewModel : WizardStepViewModelBase
{
    private string _selectedFilePath = "";
    private bool _isLoading;
    private string _statusMessage = "No file loaded";
    private RawFileData? _rawFileData;

    public Step2ImportViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        BrowseCommand = new RelayCommand(_ => BrowseForFile());
        ClearCommand = new RelayCommand(_ => ClearFile(), _ => HasFile);
    }

    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set { if (SetProperty(ref _selectedFilePath, value)) { OnPropertyChanged(nameof(HasFile)); OnPropertyChanged(nameof(FileName)); } }
    }

    public string FileName => string.IsNullOrEmpty(_selectedFilePath) ? "" : Path.GetFileName(_selectedFilePath);
    public bool HasFile => !string.IsNullOrEmpty(_selectedFilePath);
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public RawFileData? RawFileData { get => _rawFileData; set { SetProperty(ref _rawFileData, value); OnPropertyChanged(nameof(RowCount)); OnPropertyChanged(nameof(ColumnCount)); } }

    public int RowCount => _rawFileData?.TotalRows ?? 0;
    public int ColumnCount => _rawFileData?.Headers?.Count ?? 0;

    // Preview data for DataGrid - using ObservableRangeCollection for bulk operations
    public ObservableRangeCollection<RawDataRow> PreviewRows { get; } = new();
    public ObservableRangeCollection<string> ColumnHeaders { get; } = new();

    public ICommand BrowseCommand { get; }
    public ICommand ClearCommand { get; }

    private async void BrowseForFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select NPD/CSV Data File",
            Filter = "NPD Files (*.npd)|*.npd|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadFileAsync(dialog.FileName);
        }
    }

    private async Task LoadFileAsync(string filePath)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading file...";

            var rawData = _mainViewModel.ParsingService.LoadRawFile(filePath);
            
            RawFileData = rawData;
            _mainViewModel.RawFileData = rawData;
            _mainViewModel.LoadedFilePath = filePath;
            SelectedFilePath = filePath;
            
            // Populate column headers with single notification
            ColumnHeaders.ReplaceAll(rawData.Headers);

            // Build all rows first, then replace collection (single UI update)
            var allRows = rawData.GetAllRows();
            var rowList = new List<RawDataRow>(allRows.Count);
            for (int i = 0; i < allRows.Count; i++)
            {
                rowList.Add(new RawDataRow(i + 1, allRows[i]));
            }
            PreviewRows.ReplaceAll(rowList);

            _mainViewModel.AvailableColumns.Clear();
            foreach (var h in rawData.Headers) _mainViewModel.AvailableColumns.Add(h);

            StatusMessage = $"Loaded {RawFileData?.TotalRows:N0} rows, {ColumnHeaders.Count} columns";
            IsCompleted = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearFile()
    {
        SelectedFilePath = "";
        RawFileData = null;
        _mainViewModel.RawFileData = null;
        PreviewRows.Clear();
        ColumnHeaders.Clear();
        StatusMessage = "No file loaded";
        IsCompleted = false;
    }

    public override bool Validate()
    {
        ValidationMessage = "";
        if (!HasFile) { ValidationMessage = "Please load a data file."; return false; }
        if (RawFileData == null || RawFileData.TotalRows == 0) { ValidationMessage = "File contains no data."; return false; }
        return true;
    }
}

public class RawDataRow
{
    public int RowNum { get; }
    public string Col0 { get; } = "";
    public string Col1 { get; } = "";
    public string Col2 { get; } = "";
    public string Col3 { get; } = "";
    public string Col4 { get; } = "";
    public string Col5 { get; } = "";
    public string Col6 { get; } = "";
    public string Col7 { get; } = "";

    public RawDataRow(int rowNum, string[] values)
    {
        RowNum = rowNum;
        if (values.Length > 0) Col0 = values[0];
        if (values.Length > 1) Col1 = values[1];
        if (values.Length > 2) Col2 = values[2];
        if (values.Length > 3) Col3 = values[3];
        if (values.Length > 4) Col4 = values[4];
        if (values.Length > 5) Col5 = values[5];
        if (values.Length > 6) Col6 = values[6];
        if (values.Length > 7) Col7 = values[7];
    }
}

#endregion

#region Step 3: Map Columns

public class Step3ConfigureViewModel : WizardStepViewModelBase
{
    private string? _selectedTimeColumn;
    private string? _selectedVesselColumn;
    private string? _selectedRovColumn;
    private bool _hasDateTimeSplit = true;
    private string _dateFormat = "dd/MM/yyyy";
    private string _timeFormat = "HH:mm:ss";
    private string _statusMessage = "";

    public Step3ConfigureViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        AutoDetectCommand = new RelayCommand(_ => AutoDetectColumns(), _ => AvailableColumns.Count > 0);
    }

    public ObservableCollection<string> AvailableColumns => _mainViewModel.AvailableColumns;

    public string? SelectedTimeColumn
    {
        get => _selectedTimeColumn;
        set { if (SetProperty(ref _selectedTimeColumn, value)) UpdateMapping(); }
    }

    public string? SelectedVesselColumn
    {
        get => _selectedVesselColumn;
        set { if (SetProperty(ref _selectedVesselColumn, value)) UpdateMapping(); }
    }

    public string? SelectedRovColumn
    {
        get => _selectedRovColumn;
        set { if (SetProperty(ref _selectedRovColumn, value)) UpdateMapping(); }
    }

    public bool HasDateTimeSplit
    {
        get => _hasDateTimeSplit;
        set { if (SetProperty(ref _hasDateTimeSplit, value)) UpdateMapping(); }
    }

    public string DateFormat
    {
        get => _dateFormat;
        set { if (SetProperty(ref _dateFormat, value)) UpdateMapping(); }
    }

    public string TimeFormat
    {
        get => _timeFormat;
        set { if (SetProperty(ref _timeFormat, value)) UpdateMapping(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ICommand AutoDetectCommand { get; }

    public override void OnActivated()
    {
        base.OnActivated();
        if (AvailableColumns.Count > 0 && string.IsNullOrEmpty(SelectedVesselColumn))
        {
            AutoDetectColumns();
        }
    }

    private void AutoDetectColumns()
    {
        if (_mainViewModel.RawFileData == null) return;
        
        var mapping = _mainViewModel.ParsingService.AutoDetectMapping(_mainViewModel.RawFileData);
        
        if (mapping.TimeColumnIndex >= 0 && mapping.TimeColumnIndex < AvailableColumns.Count)
            SelectedTimeColumn = AvailableColumns[mapping.TimeColumnIndex];
        if (mapping.VesselHeadingColumnIndex >= 0 && mapping.VesselHeadingColumnIndex < AvailableColumns.Count)
            SelectedVesselColumn = AvailableColumns[mapping.VesselHeadingColumnIndex];
        if (mapping.RovHeadingColumnIndex >= 0 && mapping.RovHeadingColumnIndex < AvailableColumns.Count)
            SelectedRovColumn = AvailableColumns[mapping.RovHeadingColumnIndex];
        
        HasDateTimeSplit = mapping.HasDateTimeSplit;
        DateFormat = mapping.DateFormat;
        TimeFormat = mapping.TimeFormat;
        
        StatusMessage = "Auto-detection complete. Please verify column selections.";
    }

    private void UpdateMapping()
    {
        var mapping = _mainViewModel.ColumnMapping;
        mapping.TimeColumn = SelectedTimeColumn ?? "";
        mapping.VesselHeadingColumn = SelectedVesselColumn ?? "";
        mapping.RovHeadingColumn = SelectedRovColumn ?? "";
        mapping.TimeColumnIndex = string.IsNullOrEmpty(SelectedTimeColumn) ? -1 : AvailableColumns.IndexOf(SelectedTimeColumn);
        mapping.VesselHeadingColumnIndex = string.IsNullOrEmpty(SelectedVesselColumn) ? -1 : AvailableColumns.IndexOf(SelectedVesselColumn);
        mapping.RovHeadingColumnIndex = string.IsNullOrEmpty(SelectedRovColumn) ? -1 : AvailableColumns.IndexOf(SelectedRovColumn);
        mapping.HasDateTimeSplit = HasDateTimeSplit;
        mapping.DateFormat = DateFormat;
        mapping.TimeFormat = TimeFormat;
    }

    public override bool Validate()
    {
        ValidationMessage = "";
        if (string.IsNullOrEmpty(SelectedVesselColumn))
        { ValidationMessage = "Please select a Vessel Heading column."; return false; }
        if (string.IsNullOrEmpty(SelectedRovColumn))
        { ValidationMessage = "Please select an ROV Heading column."; return false; }
        if (SelectedVesselColumn == SelectedRovColumn)
        { ValidationMessage = "Vessel and ROV columns must be different."; return false; }
        return true;
    }
}

#endregion

#region Step 4: Filter & Preview

public class Step4ProcessViewModel : WizardStepViewModelBase
{
    private bool _isProcessing;
    private string _statusMessage = "";
    private DateTime? _filterStartTime;
    private DateTime? _filterEndTime;
    private DateTime? _dataStartTime;
    private DateTime? _dataEndTime;
    private double _previewMeanCO;
    private double _previewStdDev;
    private int _totalCount;
    private int _filteredCount;
    private List<RovGyroDataPoint> _allParsedPoints = new();
    
    // Slider properties
    private double _sliderMinimum;
    private double _sliderMaximum = 100;
    private double _sliderStart;
    private double _sliderEnd = 100;

    public Step4ProcessViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        ApplyFilterCommand = new RelayCommand(_ => ApplyFilter());
        ResetFilterCommand = new RelayCommand(_ => ResetFilter());
    }

    public bool IsProcessing { get => _isProcessing; set => SetProperty(ref _isProcessing, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool HasData => PreviewData.Count > 0;
    
    public DateTime? FilterStartTime { get => _filterStartTime; set => SetProperty(ref _filterStartTime, value); }
    public DateTime? FilterEndTime { get => _filterEndTime; set => SetProperty(ref _filterEndTime, value); }
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
    
    public double PreviewMeanCO { get => _previewMeanCO; set => SetProperty(ref _previewMeanCO, value); }
    public double PreviewStdDev { get => _previewStdDev; set => SetProperty(ref _previewStdDev, value); }
    public int TotalCount { get => _totalCount; set => SetProperty(ref _totalCount, value); }
    public int FilteredCount { get => _filteredCount; set => SetProperty(ref _filteredCount, value); }

    public ObservableCollection<RovGyroDataPoint> PreviewData { get; } = new();

    public ICommand ApplyFilterCommand { get; }
    public ICommand ResetFilterCommand { get; }

    public override async void OnActivated()
    {
        base.OnActivated();
        if (_mainViewModel.RawFileData != null && _allParsedPoints.Count == 0)
        {
            await ParseDataAsync();
        }
    }

    private async Task ParseDataAsync()
    {
        if (_mainViewModel.RawFileData == null) return;

        try
        {
            IsProcessing = true;
            StatusMessage = "Parsing data with geometric corrections...";

            await Task.Run(() =>
            {
                // Parse with current mapping
                _allParsedPoints = _mainViewModel.ParsingService.ParseWithMapping(
                    _mainViewModel.RawFileData, 
                    _mainViewModel.ColumnMapping);
                
                // Apply geometric corrections
                _mainViewModel.CalculationService.ApplyGeometricCorrections(
                    _allParsedPoints, 
                    _mainViewModel.Project.RovConfig);
                
                // Calculate C-O values
                _mainViewModel.CalculationService.CalculateCO(_allParsedPoints);
            });

            TotalCount = _allParsedPoints.Count;
            
            if (_allParsedPoints.Count > 0)
            {
                DataStartTime = _allParsedPoints.Min(p => p.DateTime);
                DataEndTime = _allParsedPoints.Max(p => p.DateTime);
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

            ApplyFilter();
            StatusMessage = $"Parsed {TotalCount:N0} points with geometric corrections applied.";
            OnPropertyChanged(nameof(HasData));
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

    private void ApplyFilter()
    {
        var filteredList = _allParsedPoints.AsEnumerable();
        
        if (FilterStartTime.HasValue)
            filteredList = filteredList.Where(p => p.DateTime >= FilterStartTime.Value);
        if (FilterEndTime.HasValue)
            filteredList = filteredList.Where(p => p.DateTime <= FilterEndTime.Value);

        var filtered = filteredList.ToList();
        FilteredCount = filtered.Count;

        // Update MainViewModel data points
        _mainViewModel.DataPoints.Clear();
        foreach (var p in filtered) _mainViewModel.DataPoints.Add(p);

        // Calculate preview statistics
        if (filtered.Count > 0)
        {
            var coValues = filtered.Select(p => p.CalculatedCO).ToList();
            PreviewMeanCO = coValues.Average();
            PreviewStdDev = CalculateStdDev(coValues);
        }

        // Update preview grid - Show ALL filtered data (virtualized DataGrid handles performance)
        PreviewData.Clear();
        foreach (var p in filtered) PreviewData.Add(p);

        StatusMessage = $"Filtered: {FilteredCount:N0} of {TotalCount:N0} points";
        OnPropertyChanged(nameof(HasData));
    }

    private void ResetFilter()
    {
        FilterStartTime = DataStartTime;
        FilterEndTime = DataEndTime;
        _sliderStart = SliderMinimum;
        _sliderEnd = SliderMaximum;
        OnPropertyChanged(nameof(SliderStart));
        OnPropertyChanged(nameof(SliderEnd));
        OnPropertyChanged(nameof(FormattedStartTime));
        OnPropertyChanged(nameof(FormattedEndTime));
        ApplyFilter();
    }

    private static double CalculateStdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        double mean = values.Average();
        double sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }

    public override bool Validate()
    {
        ValidationMessage = "";
        if (FilteredCount == 0) { ValidationMessage = "No data points after filtering."; return false; }
        if (FilteredCount < 10) { ValidationMessage = "Need at least 10 points for processing."; return false; }
        return true;
    }
}

#endregion

#region Step 5: Process & Analyze with Enhanced Statistics (Phase 3)

public class Step5AnalyzeViewModel : WizardStepViewModelBase
{
    private bool _isProcessing;
    private string _statusMessage = "";
    private CalibrationResult? _result;
    private PlotModel? _coPlotModel;
    private PlotModel? _histogramModel;
    private PlotModel? _qqPlotModel;
    private PlotModel? _residualsPlotModel;
    private PlotModel? _boxPlotModel;
    private PlotModel? _polarPlotModel;
    private ChartColorPalette _selectedPalette;

    public Step5AnalyzeViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        RecalculateCommand = new AsyncRelayCommand(_ => ProcessAsync());
        RefreshChartsCommand = new RelayCommand(_ => UpdateCharts());
        
        // Initialize with first palette
        _selectedPalette = ChartThemeService.AvailablePalettes[0];
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
    
    public CalibrationResult? Result
    {
        get => _result;
        set { if (SetProperty(ref _result, value)) OnPropertyChanged(nameof(HasResult)); }
    }
    public bool HasResult => _result != null;

    // Charts - Main
    public PlotModel? COPlotModel { get => _coPlotModel; set => SetProperty(ref _coPlotModel, value); }
    public PlotModel? HistogramModel { get => _histogramModel; set => SetProperty(ref _histogramModel, value); }
    
    // Charts - Additional (Phase 3)
    public PlotModel? QQPlotModel { get => _qqPlotModel; set => SetProperty(ref _qqPlotModel, value); }
    public PlotModel? ResidualsPlotModel { get => _residualsPlotModel; set => SetProperty(ref _residualsPlotModel, value); }
    public PlotModel? BoxPlotModel { get => _boxPlotModel; set => SetProperty(ref _boxPlotModel, value); }
    public PlotModel? PolarPlotModel { get => _polarPlotModel; set => SetProperty(ref _polarPlotModel, value); }
    
    // Professional Charts (v35)
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

    // Phase 3: Iteration history
    public ObservableCollection<IterationResult> IterationHistory { get; } = new();

    public ICommand RecalculateCommand { get; }

    public override async void OnActivated()
    {
        base.OnActivated();
        if (_mainViewModel.DataPoints.Count > 0 && Result == null)
        {
            await ProcessAsync();
        }
    }

    private async Task ProcessAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Processing with 3-sigma outlier detection...";

            await Task.Run(() =>
            {
                var points = _mainViewModel.DataPoints.ToList();
                var config = _mainViewModel.Project.RovConfig;
                var criteria = _mainViewModel.Project.QcCriteria;
                
                // Process and get result with iteration history
                var result = _mainViewModel.CalculationService.Calculate(points, config, criteria);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Result = result;
                    _mainViewModel.Result = result;
                    
                    // Update iteration history
                    IterationHistory.Clear();
                    foreach (var iter in result.Iterations)
                        IterationHistory.Add(iter);
                    
                    CreateCOPlot(points, result);
                    CreateHistogram(points, result);
                    CreateQQPlot(points, result);
                    CreateResidualsPlot(points, result);
                    CreateBoxPlot(points, result);
                    CreatePolarPlot(points, result);
                    CreateControlChart(points, result);
                    CreateCusumChart(points, result);
                    
                    // New professional charts (v37)
                    CreateHeadingCoverageChart(points, result);
                    CreateMovingAverageChart(points, result);
                    CreateScatterPlot(points, result);
                    CreateAcfChart(points, result);
                });
            });

            StatusMessage = $"Complete: Mean C-O = {Result?.MeanCOAccepted:F3}° ± {Result?.StdDevAccepted:F3}°";
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
        if (Result == null || _mainViewModel.DataPoints == null || _mainViewModel.DataPoints.Count == 0)
            return;
            
        var points = _mainViewModel.DataPoints.ToList();
        var result = Result;
        
        CreateCOPlot(points, result);
        CreateHistogram(points, result);
        CreateQQPlot(points, result);
        CreateResidualsPlot(points, result);
        CreateBoxPlot(points, result);
        CreatePolarPlot(points, result);
        CreateControlChart(points, result);
        CreateCusumChart(points, result);
        CreateHeadingCoverageChart(points, result);
        CreateMovingAverageChart(points, result);
        CreateScatterPlot(points, result);
        CreateAcfChart(points, result);
    }

    private void CreateCOPlot(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var p = SelectedPalette; // Shorthand for current palette
        var model = new PlotModel { Title = "C-O Over Time" };
        
        // Apply theme from palette
        model.Background = p.Background;
        model.TextColor = p.TextColor;
        model.PlotAreaBorderColor = p.GridLines;
        
        // Configure legend
        model.IsLegendVisible = true;
        
        // X axis (time)
        var timeAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Time",
            StringFormat = "HH:mm:ss",
            AxislineColor = p.AxisColor,
            TicklineColor = p.AxisColor,
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        };
        model.Axes.Add(timeAxis);
        
        // Y axis (C-O)
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "C-O (°)",
            AxislineColor = p.AxisColor,
            TicklineColor = p.AxisColor,
            TitleColor = p.TextColor,
            TextColor = p.TextColor,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = p.GridLines
        };
        model.Axes.Add(yAxis);
        
        // Phase 3: Add 3-sigma limit lines
        if (result.Iterations.Count > 0)
        {
            var lastIter = result.Iterations.Last();
            
            // Mean line
            model.Annotations.Add(new LineAnnotation
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
            model.Annotations.Add(new LineAnnotation
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
            model.Annotations.Add(new LineAnnotation
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
        
        // Accepted points
        var acceptedSeries = new ScatterSeries
        {
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = p.AcceptedPoints,
            MarkerStroke = OxyColor.FromAColor(180, p.AcceptedPoints),
            MarkerStrokeThickness = 1,
            Title = "Accepted"
        };
        
        // Rejected points
        var rejectedSeries = new ScatterSeries
        {
            MarkerType = MarkerType.Cross,
            MarkerSize = 5,
            MarkerFill = p.RejectedPoints,
            MarkerStroke = p.RejectedPoints,
            MarkerStrokeThickness = 2,
            Title = "Rejected (Outliers)"
        };
        
        foreach (var pt in points)
        {
            var point = new ScatterPoint(DateTimeAxis.ToDouble(pt.DateTime), pt.CalculatedCO);
            if (pt.Status == PointStatus.Rejected)
                rejectedSeries.Points.Add(point);
            else
                acceptedSeries.Points.Add(point);
        }
        
        model.Series.Add(acceptedSeries);
        model.Series.Add(rejectedSeries);
        
        COPlotModel = model;
    }

    private void CreateHistogram(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var model = new PlotModel { Title = "C-O Distribution" };
        
        // Apply theme from palette
        model.Background = pl.Background;
        model.TextColor = pl.TextColor;
        model.PlotAreaBorderColor = pl.GridLines;
        model.IsLegendVisible = true;
        
        var acceptedCO = points.Where(pt => pt.Status != PointStatus.Rejected).Select(pt => pt.CalculatedCO).ToList();
        if (acceptedCO.Count == 0) { HistogramModel = model; return; }
        
        // Calculate histogram bins
        double min = acceptedCO.Min();
        double max = acceptedCO.Max();
        int numBins = Math.Min(30, Math.Max(10, acceptedCO.Count / 20));
        double binWidth = (max - min) / numBins;
        if (binWidth < 0.001) binWidth = 0.01;
        
        var bins = new int[numBins];
        foreach (var co in acceptedCO)
        {
            int binIndex = Math.Min((int)((co - min) / binWidth), numBins - 1);
            if (binIndex >= 0) bins[binIndex]++;
        }
        
        // Bar series
        var barSeries = new RectangleBarSeries
        {
            FillColor = pl.HistogramBars,
            StrokeColor = OxyColor.FromAColor(200, pl.HistogramBars),
            StrokeThickness = 1
        };
        
        for (int i = 0; i < numBins; i++)
        {
            double x0 = min + i * binWidth;
            double x1 = x0 + binWidth;
            barSeries.Items.Add(new RectangleBarItem(x0, 0, x1, bins[i]));
        }
        
        model.Series.Add(barSeries);
        
        // Phase 3: Add mean and confidence interval
        if (result.Iterations.Count > 0)
        {
            var lastIter = result.Iterations.Last();
            
            // Mean line
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = lastIter.MeanCO,
                Color = pl.MeanLine,
                StrokeThickness = 2,
                Text = $"Mean: {lastIter.MeanCO:F3}°",
                TextColor = pl.TextColor
            });
            
            // ±1σ area
            model.Annotations.Add(new RectangleAnnotation
            {
                MinimumX = lastIter.MeanCO - lastIter.StdDev,
                MaximumX = lastIter.MeanCO + lastIter.StdDev,
                Fill = OxyColor.FromAColor(50, pl.MeanLine),
                Text = "±1σ",
                TextColor = pl.TextColor
            });
        }
        
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "C-O (°)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Count", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });
        
        HistogramModel = model;
    }

    private void CreateQQPlot(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var qqPlot = new PlotModel { Title = "Q-Q Plot (Normality Check)" };
        ApplyTheme(qqPlot, pl);
        
        qqPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Theoretical Quantiles", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });
        qqPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Sample Quantiles (C-O °)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });

        var acceptedCO = points.Where(pt => pt.Status != PointStatus.Rejected).Select(pt => pt.CalculatedCO).ToList();
        if (acceptedCO.Count > 2)
        {
            var sorted = acceptedCO.OrderBy(x => x).ToList();
            int n = sorted.Count;
            double mean = sorted.Average();
            double stdDev = Math.Sqrt(sorted.Sum(x => Math.Pow(x - mean, 2)) / (n - 1));
            
            var qqSeries = new ScatterSeries { Title = "Data Points", MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = pl.DataSeries1, MarkerStroke = OxyColor.FromAColor(180, pl.DataSeries1) };
            
            for (int i = 0; i < n; i++)
            {
                double prob = (i + 0.5) / n;
                double theoreticalQuantile = NormInv(prob);
                double sampleQuantile = (sorted[i] - mean) / (stdDev > 0 ? stdDev : 1);
                qqSeries.Points.Add(new ScatterPoint(theoreticalQuantile, sampleQuantile));
            }
            qqPlot.Series.Add(qqSeries);
            
            qqPlot.Annotations.Add(new LineAnnotation { Slope = 1, Intercept = 0, Color = pl.RejectedPoints, StrokeThickness = 1, LineStyle = LineStyle.Dash, Text = "Reference (y=x)", TextColor = pl.TextColor });
        }
        
        QQPlotModel = qqPlot;
    }

    private void CreateResidualsPlot(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var resPlot = new PlotModel { Title = "Residuals vs Time" };
        ApplyTheme(resPlot, pl);
        
        resPlot.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Time", StringFormat = "HH:mm", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });
        resPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Residual (°)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).ToList();
        if (acceptedPoints.Any())
        {
            double mean = acceptedPoints.Average(pt => pt.CalculatedCO);
            
            var resSeries = new ScatterSeries { Title = "Residuals", MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = pl.AcceptedPoints, MarkerStroke = OxyColor.FromAColor(180, pl.AcceptedPoints) };
            foreach (var pt in acceptedPoints)
            {
                resSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(pt.Timestamp), pt.CalculatedCO - mean));
            }
            resPlot.Series.Add(resSeries);
            
            resPlot.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = 0, Color = pl.MeanLine, StrokeThickness = 1, Text = "Zero", TextColor = pl.TextColor });
        }
        
        ResidualsPlotModel = resPlot;
    }

    private void CreateBoxPlot(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var boxPlot = new PlotModel { Title = "C-O by Heading Quadrant" };
        ApplyTheme(boxPlot, pl);
        
        var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, TitleColor = pl.TextColor, TextColor = pl.TextColor };
        categoryAxis.Labels.Add("N (315-45°)");
        categoryAxis.Labels.Add("E (45-135°)");
        categoryAxis.Labels.Add("S (135-225°)");
        categoryAxis.Labels.Add("W (225-315°)");
        boxPlot.Axes.Add(categoryAxis);
        boxPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "C-O (°)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).ToList();
        var quadrants = new List<List<double>> { new(), new(), new(), new() };
        
        foreach (var pt in acceptedPoints)
        {
            double hdg = pt.VesselHeading;
            while (hdg < 0) hdg += 360;
            while (hdg >= 360) hdg -= 360;
            
            int q = hdg switch { >= 315 or < 45 => 0, >= 45 and < 135 => 1, >= 135 and < 225 => 2, _ => 3 };
            quadrants[q].Add(pt.CalculatedCO);
        }

        var boxSeries = new BoxPlotSeries { Fill = pl.DataSeries1, Stroke = pl.TextColor };
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

    private void CreatePolarPlot(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var polarPlot = new PlotModel { Title = "C-O by Heading Direction" };
        ApplyTheme(polarPlot, pl);
        
        polarPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "East-West", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });
        polarPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "North-South", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).ToList();
        if (acceptedPoints.Any())
        {
            double maxCO = acceptedPoints.Max(pt => Math.Abs(pt.CalculatedCO));
            if (maxCO < 0.001) maxCO = 1;

            var polarSeries = new ScatterSeries { Title = "C-O Points", MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = pl.AcceptedPoints, MarkerStroke = OxyColor.FromAColor(180, pl.AcceptedPoints) };
            foreach (var pt in acceptedPoints)
            {
                double radians = (90 - pt.VesselHeading) * Math.PI / 180;
                double radius = Math.Abs(pt.CalculatedCO) / maxCO;
                double x = radius * Math.Cos(radians);
                double y = radius * Math.Sin(radians);
                polarSeries.Points.Add(new ScatterPoint(x, y) { Value = pt.CalculatedCO });
            }
            polarPlot.Series.Add(polarSeries);
            
            polarPlot.Annotations.Add(new TextAnnotation { Text = "N", TextPosition = new DataPoint(0, 1.1), TextColor = pl.TextColor });
            polarPlot.Annotations.Add(new TextAnnotation { Text = "E", TextPosition = new DataPoint(1.1, 0), TextColor = pl.TextColor });
            polarPlot.Annotations.Add(new TextAnnotation { Text = "S", TextPosition = new DataPoint(0, -1.1), TextColor = pl.TextColor });
            polarPlot.Annotations.Add(new TextAnnotation { Text = "W", TextPosition = new DataPoint(-1.1, 0), TextColor = pl.TextColor });
        }
        
        PolarPlotModel = polarPlot;
    }

    /// <summary>Individual Control Chart (I-Chart) with Moving Range</summary>
    private void CreateControlChart(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var controlChart = new PlotModel { Title = "Individual Control Chart (I-Chart)" };
        ApplyTheme(controlChart, pl);
        
        controlChart.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Observation #", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines, Minimum = 0 });
        controlChart.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "C-O (°)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).OrderBy(pt => pt.Timestamp).ToList();
        if (acceptedPoints.Count < 3) { ControlChartModel = controlChart; return; }

        var coValues = acceptedPoints.Select(pt => pt.CalculatedCO).ToList();
        double mean = coValues.Average();
        
        // Calculate moving ranges
        var movingRanges = new List<double>();
        for (int i = 1; i < coValues.Count; i++)
            movingRanges.Add(Math.Abs(coValues[i] - coValues[i - 1]));
        double avgMR = movingRanges.Average();
        
        // Control limits: UCL = X̄ + 2.66*MR̄, LCL = X̄ - 2.66*MR̄
        double ucl = mean + 2.66 * avgMR;
        double lcl = mean - 2.66 * avgMR;
        double uwl = mean + 1.77 * avgMR;
        double lwl = mean - 1.77 * avgMR;

        var dataSeries = new LineSeries { Title = "C-O Values", Color = pl.DataSeries1, MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = pl.DataSeries1 };
        for (int i = 0; i < coValues.Count; i++)
            dataSeries.Points.Add(new DataPoint(i + 1, coValues[i]));
        controlChart.Series.Add(dataSeries);

        controlChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = mean, Color = pl.MeanLine, StrokeThickness = 2, Text = $"CL: {mean:F4}°", TextColor = pl.TextColor });
        controlChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = ucl, Color = pl.UpperLimit, StrokeThickness = 2, Text = $"UCL: {ucl:F4}°", TextColor = pl.TextColor });
        controlChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = lcl, Color = pl.LowerLimit, StrokeThickness = 2, Text = $"LCL: {lcl:F4}°", TextColor = pl.TextColor });
        controlChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = uwl, Color = pl.WarningLine, StrokeThickness = 1, LineStyle = LineStyle.Dash, Text = "UWL (2σ)", TextColor = pl.TextColor });
        controlChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = lwl, Color = pl.WarningLine, StrokeThickness = 1, LineStyle = LineStyle.Dash, Text = "LWL (2σ)", TextColor = pl.TextColor });

        ControlChartModel = controlChart;
    }

    /// <summary>CUSUM Chart for trend detection</summary>
    private void CreateCusumChart(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var cusumChart = new PlotModel { Title = "CUSUM Chart (Trend Detection)" };
        ApplyTheme(cusumChart, pl);
        
        cusumChart.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Observation #", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines, Minimum = 0 });
        cusumChart.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Cumulative Sum (°)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).OrderBy(pt => pt.Timestamp).ToList();
        if (acceptedPoints.Count < 3) { CusumChartModel = cusumChart; return; }

        var coValues = acceptedPoints.Select(pt => pt.CalculatedCO).ToList();
        double target = coValues.Average();
        double stdDev = Math.Sqrt(coValues.Sum(x => Math.Pow(x - target, 2)) / (coValues.Count - 1));
        double slack = 0.5 * stdDev;

        var cusumPlus = new List<double> { 0 };
        var cusumMinus = new List<double> { 0 };
        double cPlus = 0, cMinus = 0;

        for (int i = 0; i < coValues.Count; i++)
        {
            double deviation = coValues[i] - target;
            cPlus = Math.Max(0, cPlus + deviation - slack);
            cMinus = Math.Min(0, cMinus + deviation + slack);
            cusumPlus.Add(cPlus);
            cusumMinus.Add(cMinus);
        }

        var cusumPlusSeries = new LineSeries { Title = "CUSUM+ (↑ shift)", Color = pl.RejectedPoints, StrokeThickness = 2 };
        var cusumMinusSeries = new LineSeries { Title = "CUSUM- (↓ shift)", Color = pl.DataSeries1, StrokeThickness = 2 };
        for (int i = 0; i < cusumPlus.Count; i++)
        {
            cusumPlusSeries.Points.Add(new DataPoint(i, cusumPlus[i]));
            cusumMinusSeries.Points.Add(new DataPoint(i, cusumMinus[i]));
        }
        cusumChart.Series.Add(cusumPlusSeries);
        cusumChart.Series.Add(cusumMinusSeries);

        double h = 4 * stdDev;
        cusumChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = h, Color = pl.WarningLine, StrokeThickness = 1, LineStyle = LineStyle.Dash, Text = $"H = {h:F3}°", TextColor = pl.TextColor });
        cusumChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = -h, Color = pl.WarningLine, StrokeThickness = 1, LineStyle = LineStyle.Dash, Text = $"-H = {-h:F3}°", TextColor = pl.TextColor });
        cusumChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = 0, Color = pl.MeanLine, StrokeThickness = 1, TextColor = pl.TextColor });

        CusumChartModel = cusumChart;
    }

    /// <summary>Heading Coverage Rose Chart</summary>
    private void CreateHeadingCoverageChart(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var coverageChart = new PlotModel { Title = "Heading Coverage Distribution" };
        ApplyTheme(coverageChart, pl);
        
        // For OxyPlot 2.x: Use CategoryAxis on Left for horizontal bars
        var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Heading Sector", TitleColor = pl.TextColor, TextColor = pl.TextColor };
        string[] sectorLabels = { "N", "NNE", "ENE", "E", "ESE", "SSE", "S", "SSW", "WSW", "W", "WNW", "NNW" };
        foreach (var label in sectorLabels) categoryAxis.Labels.Add(label);
        coverageChart.Axes.Add(categoryAxis);
        coverageChart.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Count", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines, Minimum = 0 });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).ToList();
        if (acceptedPoints.Count == 0) { HeadingCoverageModel = coverageChart; return; }

        var sectorCounts = new int[12];
        foreach (var pt in acceptedPoints)
        {
            double hdg = pt.VesselHeading;
            while (hdg < 0) hdg += 360;
            while (hdg >= 360) hdg -= 360;
            int sector = (int)((hdg + 15) / 30) % 12;
            sectorCounts[sector]++;
        }

        var barSeries = new BarSeries { FillColor = pl.DataSeries1, StrokeColor = OxyColor.FromAColor(200, pl.DataSeries1), StrokeThickness = 1 };
        for (int i = 0; i < 12; i++)
        {
            var color = sectorCounts[i] == 0 ? pl.RejectedPoints :
                        sectorCounts[i] < 5 ? pl.WarningLine :
                        pl.AcceptedPoints;
            barSeries.Items.Add(new BarItem(sectorCounts[i]) { Color = color });
        }
        coverageChart.Series.Add(barSeries);

        double minRecommended = acceptedPoints.Count / 24.0;
        coverageChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Vertical, X = minRecommended, Color = pl.WarningLine, StrokeThickness = 1, LineStyle = LineStyle.Dash, Text = "Min Recommended", TextColor = pl.TextColor });

        HeadingCoverageModel = coverageChart;
    }

    /// <summary>Moving Average Chart with ±2σ Envelope</summary>
    private void CreateMovingAverageChart(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var maChart = new PlotModel { Title = "Moving Average with ±2σ Bands" };
        ApplyTheme(maChart, pl);
        
        maChart.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Time", StringFormat = "HH:mm", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });
        maChart.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "C-O (°)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).OrderBy(pt => pt.Timestamp).ToList();
        if (acceptedPoints.Count < 10) { MovingAverageModel = maChart; return; }

        var coValues = acceptedPoints.Select(pt => pt.CalculatedCO).ToList();
        int windowSize = Math.Min(10, acceptedPoints.Count / 5);
        if (windowSize < 3) windowSize = 3;

        var maValues = new List<(DateTime time, double ma, double upper, double lower)>();
        for (int i = windowSize - 1; i < acceptedPoints.Count; i++)
        {
            var window = coValues.Skip(i - windowSize + 1).Take(windowSize).ToList();
            double mean = window.Average();
            double stdDev = Math.Sqrt(window.Sum(x => Math.Pow(x - mean, 2)) / (window.Count - 1));
            maValues.Add((acceptedPoints[i].Timestamp, mean, mean + 2 * stdDev, mean - 2 * stdDev));
        }

        var rawSeries = new ScatterSeries { Title = "C-O Values", MarkerType = MarkerType.Circle, MarkerSize = 2, MarkerFill = OxyColor.FromAColor(100, pl.DataSeries1) };
        foreach (var pt in acceptedPoints) rawSeries.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(pt.Timestamp), pt.CalculatedCO));
        maChart.Series.Add(rawSeries);

        var maSeries = new LineSeries { Title = $"MA({windowSize})", Color = pl.AcceptedPoints, StrokeThickness = 2 };
        var upperBand = new LineSeries { Title = "+2σ", Color = OxyColor.FromAColor(150, pl.WarningLine), StrokeThickness = 1, LineStyle = LineStyle.Dash };
        var lowerBand = new LineSeries { Title = "-2σ", Color = OxyColor.FromAColor(150, pl.WarningLine), StrokeThickness = 1, LineStyle = LineStyle.Dash };
        foreach (var v in maValues)
        {
            maSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(v.time), v.ma));
            upperBand.Points.Add(new DataPoint(DateTimeAxis.ToDouble(v.time), v.upper));
            lowerBand.Points.Add(new DataPoint(DateTimeAxis.ToDouble(v.time), v.lower));
        }
        maChart.Series.Add(maSeries);
        maChart.Series.Add(upperBand);
        maChart.Series.Add(lowerBand);

        MovingAverageModel = maChart;
    }

    /// <summary>C-O vs Heading Scatter Plot</summary>
    private void CreateScatterPlot(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var scatterChart = new PlotModel { Title = "C-O vs Heading (Bias Detection)" };
        ApplyTheme(scatterChart, pl);
        
        scatterChart.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Vessel Heading (°)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines, Minimum = 0, Maximum = 360 });
        scatterChart.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "C-O (°)", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).ToList();
        if (acceptedPoints.Count == 0) { ScatterPlotModel = scatterChart; return; }

        var scatterSeries = new ScatterSeries { Title = "C-O vs Heading", MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = pl.DataSeries1, MarkerStroke = OxyColor.FromAColor(180, pl.DataSeries1) };
        foreach (var pt in acceptedPoints)
        {
            double hdg = pt.VesselHeading;
            while (hdg < 0) hdg += 360;
            while (hdg >= 360) hdg -= 360;
            scatterSeries.Points.Add(new ScatterPoint(hdg, pt.CalculatedCO));
        }
        scatterChart.Series.Add(scatterSeries);

        double mean = acceptedPoints.Average(pt => pt.CalculatedCO);
        scatterChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = mean, Color = pl.MeanLine, StrokeThickness = 2, Text = $"Mean: {mean:F4}°", TextColor = pl.TextColor });

        // Fit sine curve to detect heading-dependent bias
        var x1 = acceptedPoints.Select(pt => Math.Sin(pt.VesselHeading * Math.PI / 180)).ToList();
        var x2 = acceptedPoints.Select(pt => Math.Cos(pt.VesselHeading * Math.PI / 180)).ToList();
        var y = acceptedPoints.Select(pt => pt.CalculatedCO).ToList();
        double n = y.Count;
        double avgSinCoeff = x1.Zip(y, (a, b) => a * b).Sum() / n - mean * x1.Sum() / n;
        double avgCosCoeff = x2.Zip(y, (a, b) => a * b).Sum() / n - mean * x2.Sum() / n;
        double amplitude = Math.Sqrt(avgSinCoeff * avgSinCoeff + avgCosCoeff * avgCosCoeff);

        if (amplitude > 0.01)
        {
            var fitSeries = new LineSeries { Title = $"Heading Bias (Amp: {amplitude:F3}°)", Color = pl.RejectedPoints, StrokeThickness = 2, LineStyle = LineStyle.Dash };
            for (double hdg = 0; hdg <= 360; hdg += 5)
            {
                double fitValue = mean + avgSinCoeff * Math.Sin(hdg * Math.PI / 180) + avgCosCoeff * Math.Cos(hdg * Math.PI / 180);
                fitSeries.Points.Add(new DataPoint(hdg, fitValue));
            }
            scatterChart.Series.Add(fitSeries);
        }

        ScatterPlotModel = scatterChart;
    }

    /// <summary>Autocorrelation Function (ACF) Chart</summary>
    private void CreateAcfChart(List<RovGyroDataPoint> points, CalibrationResult result)
    {
        var pl = SelectedPalette;
        var acfChart = new PlotModel { Title = "Autocorrelation Function (Independence Check)" };
        ApplyTheme(acfChart, pl);
        
        acfChart.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Lag", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines, Minimum = 0 });
        acfChart.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "ACF", TitleColor = pl.TextColor, TextColor = pl.TextColor, AxislineColor = pl.AxisColor, MajorGridlineStyle = LineStyle.Dot, MajorGridlineColor = pl.GridLines, Minimum = -1, Maximum = 1 });

        var acceptedPoints = points.Where(pt => pt.Status != PointStatus.Rejected).OrderBy(pt => pt.Timestamp).ToList();
        if (acceptedPoints.Count < 20) { AcfPlotModel = acfChart; return; }

        var coValues = acceptedPoints.Select(pt => pt.CalculatedCO).ToList();
        double mean = coValues.Average();
        int nPoints = coValues.Count;
        int maxLag = Math.Min(30, nPoints / 4);
        double variance = coValues.Sum(x => Math.Pow(x - mean, 2)) / nPoints;

        var acfSeries = new StemSeries { Title = "ACF", Color = pl.DataSeries1, MarkerType = MarkerType.Circle, MarkerSize = 4, MarkerFill = pl.DataSeries1 };
        for (int lag = 0; lag <= maxLag; lag++)
        {
            double covariance = 0;
            for (int i = 0; i < nPoints - lag; i++)
                covariance += (coValues[i] - mean) * (coValues[i + lag] - mean);
            covariance /= nPoints;
            double acf = covariance / variance;
            acfSeries.Points.Add(new DataPoint(lag, acf));
        }
        acfChart.Series.Add(acfSeries);

        double bound = 1.96 / Math.Sqrt(nPoints);
        acfChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = bound, Color = pl.UpperLimit, StrokeThickness = 1, LineStyle = LineStyle.Dash, Text = "95% CI", TextColor = pl.TextColor });
        acfChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = -bound, Color = pl.LowerLimit, StrokeThickness = 1, LineStyle = LineStyle.Dash, TextColor = pl.TextColor });
        acfChart.Annotations.Add(new LineAnnotation { Type = LineAnnotationType.Horizontal, Y = 0, Color = pl.MeanLine, StrokeThickness = 1, TextColor = pl.TextColor });

        AcfPlotModel = acfChart;
    }

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

    public override bool Validate() => Result != null;
}

#endregion

#region Step 6: Validate

public class Step6ValidateViewModel : WizardStepViewModelBase
{
    private ValidationResult? _validation;
    private string _statusMessage = "";
    private string _notes = "";

    public Step6ValidateViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        AcceptCommand = new RelayCommand(_ => AcceptResults());
        RejectCommand = new RelayCommand(_ => RejectResults());
    }

    public ValidationResult? Validation
    {
        get => _validation;
        set { if (SetProperty(ref _validation, value)) OnPropertyChanged(nameof(HasValidation)); }
    }
    public bool HasValidation => _validation != null;
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    
    // Pass-through properties for XAML binding
    public ObservableCollection<QcCheck> QcChecks => Validation?.Checks ?? new();
    public QcStatus OverallStatus => Validation?.OverallStatus ?? QcStatus.NotChecked;
    public string OverallStatusText => Validation?.OverallStatusText ?? "NOT CHECKED";
    public double MeanCO => _mainViewModel.Result?.MeanCOAccepted ?? 0;
    
    // Summary counts
    public int PassedChecks => Validation?.PassCount ?? 0;
    public int TotalChecks => Validation?.Checks.Count ?? 0;
    public int WarningChecks => Validation?.WarningCount ?? 0;
    public int FailedChecks => Validation?.FailCount ?? 0;
    
    // Mode guidance
    public string ModeGuidanceTitle => _mainViewModel.Project.Purpose == CalibrationPurpose.Calibration 
        ? "ROV Calibration Mode" : "ROV Verification Mode";
    
    public string ModeGuidance => _mainViewModel.Project.Purpose == CalibrationPurpose.Calibration 
        ? "Apply the Mean C-O value as the ROV gyro correction. This includes geometric corrections for ROV orientation relative to vessel."
        : "A Mean C-O close to 0° indicates the existing correction is valid. Values significantly different suggest re-calibration.";
    
    public string ResultInterpretation
    {
        get
        {
            if (_mainViewModel.Result == null) return "No results available.";
            var mean = _mainViewModel.Result.MeanCOAccepted;
            var stdDev = _mainViewModel.Result.StdDevAccepted;
            
            if (_mainViewModel.Project.Purpose == CalibrationPurpose.Verification)
            {
                if (Math.Abs(mean) < 0.1)
                    return $"Mean C-O of {mean:F3}° is very close to zero. The ROV gyro correction is valid.";
                if (Math.Abs(mean) < 0.5)
                    return $"Mean C-O of {mean:F3}° is acceptable. Monitor for drift.";
                return $"Mean C-O of {mean:F3}° suggests the ROV gyro correction needs updating.";
            }
            else
            {
                return $"Apply C-O correction of {mean:F3}° (±{stdDev:F3}°) to the ROV gyro system.";
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
    public ICommand AcceptCommand { get; }
    public ICommand RejectCommand { get; }

    public override void OnActivated()
    {
        base.OnActivated();
        RunValidation();
    }

    private void RunValidation()
    {
        if (_mainViewModel.Result == null) return;
        
        Validation = _mainViewModel.CalculationService.ValidateResults(_mainViewModel.Result, _mainViewModel.Project);
        _mainViewModel.Validation = Validation;
        
        // Notify all properties
        OnPropertyChanged(nameof(QcChecks));
        OnPropertyChanged(nameof(OverallStatus));
        OnPropertyChanged(nameof(OverallStatusText));
        OnPropertyChanged(nameof(MeanCO));
        OnPropertyChanged(nameof(PassedChecks));
        OnPropertyChanged(nameof(TotalChecks));
        OnPropertyChanged(nameof(WarningChecks));
        OnPropertyChanged(nameof(FailedChecks));
        OnPropertyChanged(nameof(ResultInterpretation));
        
        StatusMessage = Validation.OverallStatus == QcStatus.Pass ? "All checks passed!" :
                        Validation.OverallStatus == QcStatus.Warning ? "Some checks have warnings." :
                        "Some checks failed. Review results.";
        
        IsCompleted = Validation.OverallStatus != QcStatus.Fail;
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

    public override bool Validate() => Validation != null;
}

#endregion

#region Step 7: Export

public class Step7ExportViewModel : WizardStepViewModelBase
{
    private string _outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    private bool _exportPdf = true;
    private bool _exportExcel = true;
    private bool _exportCsv;
    private bool _isExporting;
    private string _statusMessage = "";

    public Step7ExportViewModel(MainViewModel mainViewModel) : base(mainViewModel)
    {
        BrowseOutputCommand = new RelayCommand(_ => BrowseOutput());
        ExportCommand = new AsyncRelayCommand(_ => ExportAsync(), _ => !IsExporting);
        OpenOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder());
    }

    public string OutputDirectory { get => _outputDirectory; set => SetProperty(ref _outputDirectory, value); }
    public bool ExportPdf { get => _exportPdf; set => SetProperty(ref _exportPdf, value); }
    public bool ExportExcel { get => _exportExcel; set => SetProperty(ref _exportExcel, value); }
    public bool ExportCsv { get => _exportCsv; set => SetProperty(ref _exportCsv, value); }
    public bool IsExporting { get => _isExporting; set => SetProperty(ref _isExporting, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    
    // Summary properties for display
    public string Decision => _mainViewModel.Validation?.Decision ?? "PENDING";
    public double MeanCO => _mainViewModel.Result?.MeanCOAccepted ?? 0;
    public double StdDev => _mainViewModel.Result?.StdDevAccepted ?? 0;
    public double BaselineAngle => _mainViewModel.Project.RovConfig.CalculateBaselineAngle();
    public int AcceptedCount => _mainViewModel.Result?.AcceptedCount ?? 0;
    public int TotalCount => _mainViewModel.Result?.TotalObservations ?? 0;

    public ICommand BrowseOutputCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand OpenOutputFolderCommand { get; }

    private void BrowseOutput()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select Output Directory",
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
