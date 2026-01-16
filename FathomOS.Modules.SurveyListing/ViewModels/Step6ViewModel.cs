using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using FathomOS.Core.Models;
using FathomOS.Core.Calculations;
using FathomOS.Core.Services;
using FathomOS.Modules.SurveyListing.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace FathomOS.Modules.SurveyListing.ViewModels;

/// <summary>
/// ViewModel for Step 6: Processing & 3D Viewer
/// </summary>
public class Step6ViewModel : INotifyPropertyChanged
{
    private bool _isProcessing;
    private bool _isProcessed;
    private double _progress;
    private string _statusMessage = "Ready to process";
    private bool _fitSplineToTrack;
    private double _pointInterval = 1.0;
    
    // KP/DCC Options
    private KpDccMode _kpDccMode = KpDccMode.Both;

    // Smoothing options
    private bool _enableSmoothing;
    private bool _smoothPosition = true;
    private bool _smoothDepth = true;
    private bool _smoothAltitude = true;
    private SmoothingMethod _positionSmoothingMethod = SmoothingMethod.MedianFilter;
    private SmoothingMethod _depthSmoothingMethod = SmoothingMethod.MedianFilter;
    private int _positionWindowSize = 5;
    private int _depthWindowSize = 5;
    private double _positionThreshold = 2.0;  // meters
    private double _depthThreshold = 0.5;     // meters/feet
    private double _kalmanProcessNoise = 0.01;
    private double _kalmanMeasurementNoise = 0.1;
    private string _smoothingStatus = string.Empty;
    
    // Smoothing results
    private SmoothingResult? _lastSmoothingResult;
    private List<SmoothingComparisonItem> _smoothingComparison = new();

    // Processed data
    private List<SurveyPoint> _processedPoints = new();
    private List<SurveyPoint> _originalPoints = new(); // Before smoothing
    private List<SurveyPoint> _splineFittedPoints = new();  // Spline-fitted points
    private List<(double X, double Y, double Z, double Distance)> _intervalPoints = new(); // Interval measure points
    private Project? _project;
    
    // Deleted and added point tracking
    private HashSet<int> _deletedPointIndices = new();  // Indices of deleted points
    private List<SurveyPoint> _addedPoints = new();     // Added/digitized points
    private bool _showDeletedPoints = true;             // Show deleted points greyed out
    private bool _showAddedPoints = true;               // Show added points highlighted
    
    // Pagination settings
    private int _currentPage = 1;
    private int _pageSize = 500;
    private int _totalPages = 1;
    private const int MAX_DISPLAY_LIMIT = 10000;  // Maximum for spline/interval display
    
    // Spline fitting options
    private string _selectedSplineAlgorithm = "Catmull-Rom";
    private double _splineTension = 0.5;
    private int _splineOutputMultiplier = 5;
    private string _splineSourceLayer = "Survey (Smoothed)";
    
    // Interval measure options
    private bool _enableIntervalMeasure;
    private double _measureInterval = 10.0;
    private bool _measureAlongSurvey = true;
    private bool _measureAlongRoute;
    private string _intervalSourceLayer = "Survey (Smoothed)";
    
    // Reference to Step 2 for route data
    private Step2ViewModel? _step2ViewModel;
    
    // Reference to Step 4 for getting loaded survey data
    private Step4ViewModel? _step4ViewModel;

    public Step6ViewModel(Project project)
    {
        _project = project;
        ProcessedPoints = new ObservableCollection<ProcessedPointDisplay>();
        ProcessingLog = new ObservableCollection<string>();
        SmoothingComparisonItems = new ObservableCollection<SmoothingComparisonItem>();
        
        // New collections for restructured tabs
        RawDataItems = new ObservableCollection<RawDataDisplay>();
        FinalListingItems = new ObservableCollection<FinalListingDisplay>();
        ComparisonItems = new ObservableCollection<ComparisonDisplay>();
        SplineFittedItems = new ObservableCollection<SplineFittedDisplay>();
        IntervalPointItems = new ObservableCollection<IntervalPointDisplay>();
        
        // Initialize spline algorithm options
        SplineAlgorithms = new ObservableCollection<string>
        {
            "Catmull-Rom",
            "Cubic Spline", 
            "B-Spline",
            "AutoCAD PEDIT"
        };
        
        // Initialize available source layers for spline/interval tools
        AvailableSourceLayers = new ObservableCollection<string>
        {
            "Survey (Smoothed)",
            "Survey (Raw)",
            "Spline Fitted",
            "Interval Points"
        };
        
        // Initialize interval source layers (includes Route Line option)
        IntervalSourceLayers = new ObservableCollection<string>
        {
            "Survey (Smoothed)",
            "Survey (Raw)",
            "Spline Fitted"
        };
        
        // Initialize listing source layers for final listing generation
        ListingSourceLayers = new ObservableCollection<string>
        {
            "Survey (Processed)",  // Default - uses raw or smoothed based on settings
            "Survey (Raw)",
            "Survey (Smoothed)",
            "Spline Fitted",
            "Interval Points"
        };
        
        // Initialize smoothing source layers
        SmoothingSourceLayers = new ObservableCollection<string>
        {
            "Survey (Raw)",
            "Spline Fitted"
        };
        
        // Initialize smoothing method options
        SmoothingMethods = new ObservableCollection<SmoothingMethod>
        {
            SmoothingMethod.MovingAverage,
            SmoothingMethod.MedianFilter,
            SmoothingMethod.ThresholdBased,
            SmoothingMethod.KalmanFilter
        };
        
        // Initialize KP/DCC mode options
        KpDccModes = new ObservableCollection<KpDccMode>
        {
            KpDccMode.Both,
            KpDccMode.KpOnly,
            KpDccMode.DccOnly,
            KpDccMode.None
        };
        
        LoadProject(project);
    }

    /// <summary>
    /// Set reference to Step 2 ViewModel to access route data
    /// </summary>
    public void SetStep2Reference(Step2ViewModel step2)
    {
        _step2ViewModel = step2;
        
        // Add Route Line option to interval source layers if route is available
        if (step2?.RouteData != null && !IntervalSourceLayers.Contains("Route Line"))
        {
            IntervalSourceLayers.Add("Route Line");
        }
        OnPropertyChanged(nameof(HasRouteData));
    }

    /// <summary>
    /// Set reference to Step 4 ViewModel to access loaded survey data
    /// </summary>
    public void SetStep4Reference(Step4ViewModel step4)
    {
        _step4ViewModel = step4;
    }

    // Collections for display
    public ObservableCollection<ProcessedPointDisplay> ProcessedPoints { get; }
    public ObservableCollection<string> ProcessingLog { get; }
    public ObservableCollection<SmoothingMethod> SmoothingMethods { get; }
    public ObservableCollection<KpDccMode> KpDccModes { get; }
    public ObservableCollection<SmoothingComparisonItem> SmoothingComparisonItems { get; }
    
    // New collections for restructured tabs
    public ObservableCollection<RawDataDisplay> RawDataItems { get; }
    public ObservableCollection<FinalListingDisplay> FinalListingItems { get; }
    public ObservableCollection<ComparisonDisplay> ComparisonItems { get; }
    public ObservableCollection<SplineFittedDisplay> SplineFittedItems { get; }
    public ObservableCollection<IntervalPointDisplay> IntervalPointItems { get; }
    public ObservableCollection<string> SplineAlgorithms { get; }
    public ObservableCollection<string> AvailableSourceLayers { get; }
    public ObservableCollection<string> IntervalSourceLayers { get; }
    public ObservableCollection<string> ListingSourceLayers { get; }
    public ObservableCollection<string> SmoothingSourceLayers { get; }

    // Processing state
    public bool IsProcessing
    {
        get => _isProcessing;
        private set { _isProcessing = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanProcess)); }
    }

    public bool IsProcessed
    {
        get => _isProcessed;
        private set { _isProcessed = value; OnPropertyChanged(); }
    }

    public double Progress
    {
        get => _progress;
        private set { _progress = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool CanProcess => !IsProcessing;

    // Processing options
    public bool FitSplineToTrack
    {
        get => _fitSplineToTrack;
        set { _fitSplineToTrack = value; OnPropertyChanged(); }
    }

    // SPLINE FITTING PROPERTIES
    public string SelectedSplineAlgorithm
    {
        get => _selectedSplineAlgorithm;
        set { _selectedSplineAlgorithm = value; OnPropertyChanged(); OnPropertyChanged(nameof(SplineAlgorithmDisplay)); }
    }
    
    public string SplineSourceLayer
    {
        get => _splineSourceLayer;
        set { _splineSourceLayer = value; OnPropertyChanged(); }
    }
    
    public double SplineTension
    {
        get => _splineTension;
        set { _splineTension = Math.Max(0, Math.Min(5, value)); OnPropertyChanged(); }
    }
    
    public int SplineOutputMultiplier
    {
        get => _splineOutputMultiplier;
        set { _splineOutputMultiplier = Math.Max(1, Math.Min(50, value)); OnPropertyChanged(); OnPropertyChanged(nameof(SplineOutputDescription)); }
    }
    
    public string SplineOutputDescription => $"Approx. {_processedPoints.Count * SplineOutputMultiplier} output points";
    public string SplineAlgorithmDisplay => SelectedSplineAlgorithm;
    
    // INTERVAL MEASURE PROPERTIES
    public string IntervalSourceLayer
    {
        get => _intervalSourceLayer;
        set { _intervalSourceLayer = value; OnPropertyChanged(); }
    }
    public bool EnableIntervalMeasure
    {
        get => _enableIntervalMeasure;
        set { _enableIntervalMeasure = value; OnPropertyChanged(); }
    }
    
    public double MeasureInterval
    {
        get => _measureInterval;
        set { _measureInterval = Math.Max(0.1, value); OnPropertyChanged(); }
    }
    
    public bool MeasureAlongSurvey
    {
        get => _measureAlongSurvey;
        set { _measureAlongSurvey = value; OnPropertyChanged(); if (value) MeasureAlongRoute = false; }
    }
    
    public bool MeasureAlongRoute
    {
        get => _measureAlongRoute;
        set { _measureAlongRoute = value; OnPropertyChanged(); if (value) MeasureAlongSurvey = false; }
    }
    
    public bool HasRouteData => _step2ViewModel?.RouteData != null;
    public string MeasurePathDisplay => MeasureAlongRoute ? "Route" : "Survey";
    public int IntervalPointCount => _intervalPoints.Count;
    public bool HasIntervalPoints => _intervalPoints.Count > 0;
    
    // LISTING SOURCE PROPERTIES
    private string _listingSourceLayer = "Survey (Processed)";
    public string ListingSourceLayer
    {
        get => _listingSourceLayer;
        set 
        { 
            _listingSourceLayer = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(ListingSourceDescription));
        }
    }
    
    public string ListingSourceDescription
    {
        get
        {
            return ListingSourceLayer switch
            {
                "Survey (Processed)" => $"Using processed data ({_processedPoints.Count} points)",
                "Survey (Raw)" => $"Using raw data ({_processedPoints.Count} points)",
                "Survey (Smoothed)" => $"Using smoothed data ({_processedPoints.Count} points)",
                "Spline Fitted" => $"Using spline fitted ({_splineFittedPoints.Count} points)",
                "Interval Points" => $"Using interval points ({_intervalPoints.Count} points)",
                _ => ""
            };
        }
    }
    
    public ICommand? RefreshListingCommand => new RelayCommand(_ => RefreshFinalListing(), _ => IsProcessed);
    public string GeneratedLayersInfo
    {
        get
        {
            var layers = new List<string>();
            if (_splineFittedPoints.Count > 0) layers.Add($"Spline({_splineFittedPoints.Count})");
            if (_intervalPoints.Count > 0) layers.Add($"Interval({_intervalPoints.Count})");
            return layers.Count > 0 ? string.Join(", ", layers) : "None";
        }
    }

    public double PointInterval
    {
        get => _pointInterval;
        set { _pointInterval = value; OnPropertyChanged(); }
    }

    public KpDccMode KpDccMode
    {
        get => _kpDccMode;
        set 
        { 
            _kpDccMode = value; 
            OnPropertyChanged(); 
            OnPropertyChanged(nameof(CalculateKp));
            OnPropertyChanged(nameof(CalculateDcc));
        }
    }
    
    // Convenience properties for binding
    public bool CalculateKp => _kpDccMode == KpDccMode.Both || _kpDccMode == KpDccMode.KpOnly;
    public bool CalculateDcc => _kpDccMode == KpDccMode.Both || _kpDccMode == KpDccMode.DccOnly;
    
    // Smoothing Results Display
    public string SmoothingSummary
    {
        get
        {
            if (_lastSmoothingResult == null) return "No smoothing applied";
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Total Points: {_lastSmoothingResult.TotalPoints}");
            
            if (SmoothPosition)
                sb.AppendLine($"Position Modified: {_lastSmoothingResult.PositionPointsModified} points (max: {_lastSmoothingResult.MaxPositionCorrection:F3}m)");
            
            if (SmoothDepth)
                sb.AppendLine($"Depth Modified: {_lastSmoothingResult.DepthPointsModified} points (max: {_lastSmoothingResult.MaxDepthCorrection:F3})");
            
            if (SmoothAltitude)
                sb.AppendLine($"Altitude Modified: {_lastSmoothingResult.AltitudePointsModified} points (max: {_lastSmoothingResult.MaxAltitudeCorrection:F3})");
            
            sb.AppendLine($"Total Corrections: {_lastSmoothingResult.SpikesRemoved}");
            
            return sb.ToString();
        }
    }
    
    public bool HasSmoothingResults => _lastSmoothingResult != null && _lastSmoothingResult.SpikesRemoved > 0;

    // Smoothing options
    public bool EnableSmoothing
    {
        get => _enableSmoothing;
        set { _enableSmoothing = value; OnPropertyChanged(); }
    }
    
    private string _smoothingSourceLayer = "Survey (Raw)";
    public string SmoothingSourceLayer
    {
        get => _smoothingSourceLayer;
        set { _smoothingSourceLayer = value; OnPropertyChanged(); }
    }

    public bool SmoothPosition
    {
        get => _smoothPosition;
        set { _smoothPosition = value; OnPropertyChanged(); }
    }

    public bool SmoothDepth
    {
        get => _smoothDepth;
        set { _smoothDepth = value; OnPropertyChanged(); }
    }

    public bool SmoothAltitude
    {
        get => _smoothAltitude;
        set { _smoothAltitude = value; OnPropertyChanged(); }
    }

    public SmoothingMethod PositionSmoothingMethod
    {
        get => _positionSmoothingMethod;
        set { _positionSmoothingMethod = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowPositionWindowSize)); OnPropertyChanged(nameof(ShowPositionThreshold)); OnPropertyChanged(nameof(ShowKalmanOptions)); }
    }

    public SmoothingMethod DepthSmoothingMethod
    {
        get => _depthSmoothingMethod;
        set { _depthSmoothingMethod = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowDepthWindowSize)); OnPropertyChanged(nameof(ShowDepthThreshold)); OnPropertyChanged(nameof(ShowKalmanOptions)); }
    }

    public int PositionWindowSize
    {
        get => _positionWindowSize;
        set { _positionWindowSize = Math.Max(3, value | 1); OnPropertyChanged(); } // Ensure odd number >= 3
    }

    public int DepthWindowSize
    {
        get => _depthWindowSize;
        set { _depthWindowSize = Math.Max(3, value | 1); OnPropertyChanged(); } // Ensure odd number >= 3
    }

    public double PositionThreshold
    {
        get => _positionThreshold;
        set { _positionThreshold = value; OnPropertyChanged(); }
    }

    public double DepthThreshold
    {
        get => _depthThreshold;
        set { _depthThreshold = value; OnPropertyChanged(); }
    }

    public double KalmanProcessNoise
    {
        get => _kalmanProcessNoise;
        set { _kalmanProcessNoise = value; OnPropertyChanged(); }
    }

    public double KalmanMeasurementNoise
    {
        get => _kalmanMeasurementNoise;
        set { _kalmanMeasurementNoise = value; OnPropertyChanged(); }
    }

    public string SmoothingStatus
    {
        get => _smoothingStatus;
        private set { _smoothingStatus = value; OnPropertyChanged(); }
    }

    // Visibility helpers for smoothing options
    public bool ShowPositionWindowSize => PositionSmoothingMethod == SmoothingMethod.MovingAverage || PositionSmoothingMethod == SmoothingMethod.MedianFilter;
    public bool ShowPositionThreshold => PositionSmoothingMethod == SmoothingMethod.ThresholdBased;
    public bool ShowDepthWindowSize => DepthSmoothingMethod == SmoothingMethod.MovingAverage || DepthSmoothingMethod == SmoothingMethod.MedianFilter;
    public bool ShowDepthThreshold => DepthSmoothingMethod == SmoothingMethod.ThresholdBased;
    public bool ShowKalmanOptions => PositionSmoothingMethod == SmoothingMethod.KalmanFilter || DepthSmoothingMethod == SmoothingMethod.KalmanFilter;

    // Statistics after processing
    public int TotalPoints => _processedPoints.Count;
    public string KpRange => _processedPoints.Count > 0
        ? $"{_processedPoints.Min(p => p.Kp ?? 0):F6} to {_processedPoints.Max(p => p.Kp ?? 0):F6}"
        : "-";
    
    // Pagination properties
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (value >= 1 && value <= TotalPages)
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfo));
                RefreshCurrentPageData();
            }
        }
    }
    
    public int PageSize
    {
        get => _pageSize;
        set
        {
            _pageSize = Math.Max(50, Math.Min(2000, value));
            OnPropertyChanged();
            RecalculateTotalPages();
            CurrentPage = 1;
        }
    }
    
    public int TotalPages
    {
        get => _totalPages;
        private set { _totalPages = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageInfo)); }
    }
    
    public string PageInfo => $"Page {CurrentPage} of {TotalPages} ({TotalPoints} total points)";
    
    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;
    
    // Deleted/Added point visibility toggles
    public bool ShowDeletedPoints
    {
        get => _showDeletedPoints;
        set { _showDeletedPoints = value; OnPropertyChanged(); RefreshCurrentPageData(); }
    }
    
    public bool ShowAddedPoints
    {
        get => _showAddedPoints;
        set { _showAddedPoints = value; OnPropertyChanged(); RefreshCurrentPageData(); }
    }
    
    public int DeletedPointCount => _deletedPointIndices.Count;
    public int AddedPointCount => _addedPoints.Count;
    
    // Pagination commands
    public void GoToFirstPage() => CurrentPage = 1;
    public void GoToPreviousPage() { if (CanGoToPreviousPage) CurrentPage--; }
    public void GoToNextPage() { if (CanGoToNextPage) CurrentPage++; }
    public void GoToLastPage() => CurrentPage = TotalPages;
    
    private void RecalculateTotalPages()
    {
        int totalCount = _processedPoints.Count;
        if (!ShowDeletedPoints)
            totalCount -= _deletedPointIndices.Count;
        if (ShowAddedPoints)
            totalCount += _addedPoints.Count;
            
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / PageSize));
        
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }
    
    private void RefreshCurrentPageData()
    {
        // Build filtered list
        var filteredPoints = new List<(SurveyPoint point, bool isDeleted, bool isAdded)>();
        
        for (int i = 0; i < _processedPoints.Count; i++)
        {
            bool isDeleted = _deletedPointIndices.Contains(i);
            if (isDeleted && !ShowDeletedPoints) continue;
            filteredPoints.Add((_processedPoints[i], isDeleted, false));
        }
        
        // Add new points if enabled
        if (ShowAddedPoints)
        {
            foreach (var pt in _addedPoints)
            {
                filteredPoints.Add((pt, false, true));
            }
        }
        
        // Calculate page slice
        int skip = (CurrentPage - 1) * PageSize;
        var pageItems = filteredPoints.Skip(skip).Take(PageSize).ToList();
        
        // Update display collections
        ComparisonItems.Clear();
        FinalListingItems.Clear();
        
        foreach (var (point, isDeleted, isAdded) in pageItems)
        {
            ComparisonItems.Add(new ComparisonDisplay
            {
                RecordNumber = point.RecordNumber,
                DateTime = point.DateTime,
                RawEasting = point.Easting,
                RawNorthing = point.Northing,
                RawDepth = point.Depth,
                RawAltitude = point.Altitude,
                X = point.X,
                Y = point.Y,
                Z = point.CalculatedZ,
                Kp = point.Kp,
                Dcc = point.Dcc,
                IsDeleted = isDeleted,
                IsAdded = isAdded
            });
            
            FinalListingItems.Add(new FinalListingDisplay
            {
                Kp = point.Kp ?? 0,
                Dcc = point.Dcc ?? 0,
                X = point.X,
                Y = point.Y,
                Z = point.CalculatedZ ?? point.Depth ?? 0,
                IsDeleted = isDeleted,
                IsAdded = isAdded
            });
        }
        
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
    }
    
    /// <summary>
    /// Refresh the final listing based on the selected source layer
    /// </summary>
    private void RefreshFinalListing()
    {
        FinalListingItems.Clear();
        
        List<(double X, double Y, double Z, double? Kp, double? Dcc)> sourceData = new();
        
        switch (ListingSourceLayer)
        {
            case "Survey (Processed)":
            case "Survey (Smoothed)":
                // Use smoothed/processed data (X, Y properties return smoothed if available)
                for (int i = 0; i < _processedPoints.Count; i++)
                {
                    var pt = _processedPoints[i];
                    if (!_deletedPointIndices.Contains(i))
                    {
                        sourceData.Add((pt.X, pt.Y, pt.CalculatedZ ?? pt.Depth ?? 0, pt.Kp, pt.Dcc));
                    }
                }
                break;
                
            case "Survey (Raw)":
                // Use raw easting/northing
                for (int i = 0; i < _processedPoints.Count; i++)
                {
                    var pt = _processedPoints[i];
                    if (!_deletedPointIndices.Contains(i))
                    {
                        sourceData.Add((pt.Easting, pt.Northing, pt.Depth ?? 0, pt.Kp, pt.Dcc));
                    }
                }
                break;
                
            case "Spline Fitted":
                // Use spline fitted points
                double totalDist = 0;
                for (int i = 0; i < _splineFittedPoints.Count; i++)
                {
                    var pt = _splineFittedPoints[i];
                    if (i > 0)
                    {
                        var prevPt = _splineFittedPoints[i - 1];
                        totalDist += Math.Sqrt(Math.Pow(pt.Easting - prevPt.Easting, 2) + 
                                              Math.Pow(pt.Northing - prevPt.Northing, 2));
                    }
                    sourceData.Add((pt.Easting, pt.Northing, pt.Depth ?? 0, null, totalDist));
                }
                break;
                
            case "Interval Points":
                // Use interval points
                foreach (var pt in _intervalPoints)
                {
                    sourceData.Add((pt.X, pt.Y, pt.Z, null, pt.Distance));
                }
                break;
        }
        
        // Populate FinalListingItems
        foreach (var (x, y, z, kp, dcc) in sourceData)
        {
            FinalListingItems.Add(new FinalListingDisplay
            {
                Kp = kp ?? 0,
                Dcc = dcc ?? 0,
                X = x,
                Y = y,
                Z = z,
                IsDeleted = false,
                IsAdded = false
            });
        }
        
        Log($"Final Listing refreshed from '{ListingSourceLayer}': {FinalListingItems.Count} points");
        OnPropertyChanged(nameof(ListingSourceDescription));
    }

    public async Task ProcessAsync()
    {
        if (IsProcessing) return;

        // Check if we have data from Step 4
        if (_step4ViewModel == null || !_step4ViewModel.HasLoadedData)
        {
            await DialogService.Instance.ShowWarningAsync("Error", 
                "No survey data available. Please load and confirm data in Step 4.");
            return;
        }

        IsProcessing = true;
        IsProcessed = false;
        Progress = 0;
        ProcessingLog.Clear();
        ProcessedPoints.Clear();
        SmoothingComparisonItems.Clear();
        RawDataItems.Clear();
        FinalListingItems.Clear();
        ComparisonItems.Clear();
        _lastSmoothingResult = null;
        
        // Start ProcessingTracker for crib sheet
        ProcessingTracker.Instance.StartProcessing();

        try
        {
            Log("=== SURVEY LISTING GENERATION ===");
            Log("");
            await Task.Delay(50); // Allow UI to update

            // Step 1: Get data from Step 4
            Log("Step 1: Loading survey data from Step 4...");
            var sourcePoints = _step4ViewModel.GetLoadedPoints();
            Log($"  Loaded {sourcePoints.Count} survey points");
            Progress = 10;
            
            // Create working copy of points
            _processedPoints = sourcePoints.Select(p => p.Clone()).ToList();
            
            // Store original for comparison (before any modifications)
            _originalPoints = sourcePoints.Select(p => p.Clone()).ToList();
            
            // Populate Raw Data tab
            PopulateRawDataTab();
            
            // Validate coordinate systems match
            await ValidateCoordinateSystemsAsync();
            Progress = 15;

            // Step 2: Apply smoothing if enabled
            Log("");
            if (EnableSmoothing && (SmoothPosition || SmoothDepth || SmoothAltitude))
            {
                Log("Step 2: Applying track smoothing...");
                
                var smoothingOptions = new SmoothingOptions
                {
                    SmoothPosition = SmoothPosition,
                    PositionMethod = PositionSmoothingMethod,
                    PositionWindowSize = PositionWindowSize,
                    PositionThreshold = PositionThreshold,
                    SmoothDepth = SmoothDepth,
                    SmoothAltitude = SmoothAltitude,
                    DepthMethod = DepthSmoothingMethod,
                    DepthWindowSize = DepthWindowSize,
                    DepthThreshold = DepthThreshold,
                    ProcessNoise = KalmanProcessNoise,
                    MeasurementNoise = KalmanMeasurementNoise
                };
                
                var smoothingService = new SmoothingService(smoothingOptions);
                _lastSmoothingResult = smoothingService.Smooth(_processedPoints);
                
                if (SmoothPosition)
                    Log($"  Position: {PositionSmoothingMethod} - {_lastSmoothingResult.PositionPointsModified} points modified (max: {_lastSmoothingResult.MaxPositionCorrection:F3}m)");
                if (SmoothDepth)
                    Log($"  Depth: {DepthSmoothingMethod} - {_lastSmoothingResult.DepthPointsModified} points modified (max: {_lastSmoothingResult.MaxDepthCorrection:F3})");
                if (SmoothAltitude)
                    Log($"  Altitude: {DepthSmoothingMethod} - {_lastSmoothingResult.AltitudePointsModified} points modified (max: {_lastSmoothingResult.MaxAltitudeCorrection:F3})");
                
                SmoothingStatus = $"{_lastSmoothingResult.SpikesRemoved} corrections applied";
                Log($"  Total modifications: {_lastSmoothingResult.SpikesRemoved}");
                
                // Build smoothing comparison data
                BuildSmoothingComparison();
            }
            else
            {
                Log("Step 2: Smoothing disabled - using raw values");
                // Initialize smoothed values to raw values
                foreach (var point in _processedPoints)
                {
                    point.SmoothedEasting = point.Easting;
                    point.SmoothedNorthing = point.Northing;
                    point.SmoothedDepth = point.Depth;
                    point.SmoothedAltitude = point.Altitude;
                }
                SmoothingStatus = "Smoothing disabled";
            }
            Progress = 35;

            // Step 3: Apply tide corrections and calculate Z
            Log("");
            Log("Step 3: Calculating seabed depth (Z)...");
            
            // Check if offsets should be applied (checkbox checked AND value entered)
            bool applyOffsets = (_project?.ProcessingOptions.ApplyVerticalOffsets ?? false) 
                                && (_project?.ProcessingOptions.BathyToAltimeterOffset.HasValue ?? false);
            double offset = applyOffsets ? _project!.ProcessingOptions.BathyToAltimeterOffset!.Value : 0.0;
            
            // Check if tide corrections should be applied
            bool applyTide = _project?.ProcessingOptions.ApplyTidalCorrections ?? false;
            
            if (applyOffsets)
            {
                Log($"  Formula: Z = Depth + Altitude + Offset - Tide");
                Log($"  Bathy_Altimeter_Offset: {offset:F2}");
            }
            else
            {
                if (_project?.ProcessingOptions.ApplyVerticalOffsets == true && 
                    !_project?.ProcessingOptions.BathyToAltimeterOffset.HasValue == true)
                {
                    Log($"  Warning: Apply Offsets checked but no offset value entered - skipping offset");
                }
                Log($"  Formula: Z = Depth + Altitude - Tide (offsets disabled)");
            }
            
            if (!applyTide)
            {
                Log($"  Tide corrections: DISABLED");
            }
            
            int validZCount = 0;
            foreach (var point in _processedPoints)
            {
                // Get smoothed values (or raw if no smoothing)
                double depth = point.SmoothedDepth ?? point.Depth ?? 0;
                double altitude = point.SmoothedAltitude ?? point.Altitude ?? 0;
                double tide = applyTide ? (point.TideCorrection ?? 0) : 0;
                
                // Calculate Z = Depth + Altitude + Offset - Tide
                if (point.SmoothedDepth.HasValue || point.Depth.HasValue)
                {
                    point.CalculatedZ = depth + altitude + offset - tide;
                    point.OffsetApplied = offset;
                    validZCount++;
                }
            }
            Log($"  Calculated Z for {validZCount} points");
            Progress = 50;

            // Step 4: Calculate KP/DCC using route from Step 2
            Log("");
            if (KpDccMode != KpDccMode.None)
            {
                Log("Step 4: Calculating KP/DCC from route...");
                await CalculateKpDccAsync();
            }
            else
            {
                Log("Step 4: KP/DCC calculation disabled");
            }
            Progress = 75;

            // Step 5: Fit spline if enabled
            if (FitSplineToTrack)
            {
                Log("");
                Log($"Step 5: Fitting spline to track (Source: {SplineSourceLayer}, Algorithm: {SelectedSplineAlgorithm}, Tension: {SplineTension:F2})...");
                StatusMessage = "Fitting spline to track...";
                
                try
                {
                    // Prepare input points based on selected source layer
                    List<(double X, double Y, double Z)> inputPoints;
                    
                    if (SplineSourceLayer == "Survey (Raw)")
                    {
                        // Use raw/original coordinates
                        inputPoints = _processedPoints.Select(p => (
                            p.Easting,
                            p.Northing,
                            p.Depth ?? 0.0
                        )).ToList();
                        Log($"Using raw survey points: {inputPoints.Count} points");
                    }
                    else if (SplineSourceLayer == "Spline Fitted" && _splineFittedPoints.Count > 0)
                    {
                        // Use existing spline-fitted points (for re-processing)
                        inputPoints = _splineFittedPoints.Select(p => (
                            p.Easting,
                            p.Northing,
                            p.Depth ?? 0.0
                        )).ToList();
                        Log($"Using existing spline fitted points: {inputPoints.Count} points");
                    }
                    else
                    {
                        // Default: Use smoothed coordinates (Survey Smoothed)
                        inputPoints = _processedPoints.Select(p => (
                            p.SmoothedEasting ?? p.Easting,
                            p.SmoothedNorthing ?? p.Northing,
                            p.CorrectedDepth ?? p.Depth ?? 0.0
                        )).ToList();
                        Log($"Using smoothed survey points: {inputPoints.Count} points");
                    }
                    
                    List<(double X, double Y, double Z)> splineResults = new();
                    
                    // Map algorithm name to enum
                    var algorithm = SelectedSplineAlgorithm switch
                    {
                        "Catmull-Rom" => SplineAlgorithm.CatmullRom,
                        "Cubic Spline" => SplineAlgorithm.CubicSpline,
                        "B-Spline" => SplineAlgorithm.BSpline,
                        "AutoCAD PEDIT" => SplineAlgorithm.AutoCadPedit,
                        _ => SplineAlgorithm.CatmullRom
                    };
                    
                    int outputCount = Math.Max(inputPoints.Count * SplineOutputMultiplier, 100);
                    
                    // Run spline generation on background thread
                    await Task.Run(() =>
                    {
                        var splineService = new SplineService();
                        splineResults = splineService.GenerateSpline(
                            inputPoints, 
                            algorithm, 
                            tension: SplineTension, 
                            outputPointCount: outputCount);
                    });
                    
                    // Convert to SurveyPoints and store
                    _splineFittedPoints.Clear();
                    for (int i = 0; i < splineResults.Count; i++)
                    {
                        var pt = splineResults[i];
                        _splineFittedPoints.Add(new SurveyPoint
                        {
                            RecordNumber = i + 1,
                            Easting = pt.X,
                            Northing = pt.Y,
                            Depth = pt.Z,
                            SmoothedEasting = pt.X,
                            SmoothedNorthing = pt.Y,
                            CorrectedDepth = pt.Z
                        });
                    }
                    
                    Log($"Spline fitting complete: {_splineFittedPoints.Count} points generated");
                }
                catch (Exception ex)
                {
                    Log($"Spline fitting error: {ex.Message}");
                }
            }
            else
            {
                _splineFittedPoints.Clear();
            }
            
            // Step 6: Generate interval points if enabled
            if (EnableIntervalMeasure && MeasureInterval > 0)
            {
                Log("");
                Log($"Step 6: Generating interval points (Source: {IntervalSourceLayer}, Interval: {MeasureInterval:F1}m)...");
                StatusMessage = "Generating interval points...";
                
                try
                {
                    _intervalPoints.Clear();
                    
                    // Get source points based on IntervalSourceLayer dropdown
                    List<(double X, double Y, double Z)> intervalSourcePts;
                    
                    if (IntervalSourceLayer == "Route Line" && _step2ViewModel?.RouteData?.Segments != null && _step2ViewModel.RouteData.Segments.Count > 0)
                    {
                        // Extract points from route segments
                        var routePts = new List<(double X, double Y, double Z)>();
                        foreach (var seg in _step2ViewModel.RouteData.Segments)
                        {
                            routePts.Add((seg.StartEasting, seg.StartNorthing, 0.0));
                        }
                        // Add last endpoint
                        var lastSeg = _step2ViewModel.RouteData.Segments.Last();
                        routePts.Add((lastSeg.EndEasting, lastSeg.EndNorthing, 0.0));
                        intervalSourcePts = routePts;
                        Log($"Using route data with {intervalSourcePts.Count} points");
                    }
                    else if (IntervalSourceLayer == "Survey (Raw)")
                    {
                        // Use raw/original coordinates
                        intervalSourcePts = _processedPoints.Select(p => (
                            p.Easting,
                            p.Northing,
                            p.Depth ?? 0.0
                        )).ToList();
                        Log($"Using raw survey track with {intervalSourcePts.Count} points");
                    }
                    else if (IntervalSourceLayer == "Spline Fitted" && _splineFittedPoints.Count > 0)
                    {
                        // Use spline-fitted points
                        intervalSourcePts = _splineFittedPoints.Select(p => (
                            p.Easting,
                            p.Northing,
                            p.Depth ?? 0.0
                        )).ToList();
                        Log($"Using spline fitted track with {intervalSourcePts.Count} points");
                    }
                    else
                    {
                        // Default: Use smoothed coordinates (Survey (Smoothed))
                        intervalSourcePts = _processedPoints.Select(p => (
                            p.SmoothedEasting ?? p.Easting,
                            p.SmoothedNorthing ?? p.Northing,
                            p.CorrectedDepth ?? p.Depth ?? 0.0
                        )).ToList();
                        Log($"Using smoothed survey track with {intervalSourcePts.Count} points");
                    }
                    
                    if (intervalSourcePts.Count >= 2)
                    {
                        // Generate points at intervals
                        double totalDistance = 0;
                        double nextIntervalDistance = 0;
                        
                        // Add first point
                        _intervalPoints.Add((intervalSourcePts[0].X, intervalSourcePts[0].Y, intervalSourcePts[0].Z, 0));
                        
                        for (int i = 1; i < intervalSourcePts.Count; i++)
                        {
                            var prev = intervalSourcePts[i - 1];
                            var curr = intervalSourcePts[i];
                            
                            double dx = curr.X - prev.X;
                            double dy = curr.Y - prev.Y;
                            double segmentLength = Math.Sqrt(dx * dx + dy * dy);
                            
                            // Check for interval points along this segment
                            while (totalDistance + segmentLength >= nextIntervalDistance + MeasureInterval)
                            {
                                nextIntervalDistance += MeasureInterval;
                                double ratio = (nextIntervalDistance - totalDistance) / segmentLength;
                                
                                if (ratio >= 0 && ratio <= 1)
                                {
                                    double interpX = prev.X + dx * ratio;
                                    double interpY = prev.Y + dy * ratio;
                                    double interpZ = prev.Z + (curr.Z - prev.Z) * ratio;
                                    _intervalPoints.Add((interpX, interpY, interpZ, nextIntervalDistance));
                                }
                            }
                            
                            totalDistance += segmentLength;
                        }
                        
                        Log($"Interval measure complete: {_intervalPoints.Count} points at {MeasureInterval:F1}m intervals");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Interval measure error: {ex.Message}");
                }
            }
            else
            {
                _intervalPoints.Clear();
            }

            // Populate display collections
            Log("");
            Log("Preparing display...");
            Progress = 90;
            
            PopulateDisplayCollections();

            Progress = 100;
            Log("");
            Log("=== PROCESSING COMPLETE ===");
            StatusMessage = $"Processed {_processedPoints.Count} points";
            IsProcessed = true;
            
            // Update ProcessingTracker for crib sheet
            bool hasTide = _project?.ProcessingOptions.ApplyTidalCorrections == true;
            bool hasKpDcc = KpDccMode != KpDccMode.None;
            bool hasSmoothing = EnableSmoothing && (SmoothPosition || SmoothDepth || SmoothAltitude);
            ProcessingTracker.Instance.OnDataProcessed(hasTide, hasKpDcc, hasSmoothing);

            OnPropertyChanged(nameof(TotalPoints));
            OnPropertyChanged(nameof(KpRange));
            OnPropertyChanged(nameof(SmoothingSummary));
            OnPropertyChanged(nameof(HasSmoothingResults));
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
            await DialogService.Instance.ShowErrorAsync("Error", $"Processing error:\n\n{ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    /// <summary>
    /// Populate Raw Data tab with original parsed NPD data
    /// </summary>
    private void PopulateRawDataTab()
    {
        // Show all points - DataGrid virtualization handles large datasets
        foreach (var point in _originalPoints)
        {
            RawDataItems.Add(new RawDataDisplay
            {
                RecordNumber = point.RecordNumber,
                DateTime = point.DateTime,
                Easting = point.Easting,
                Northing = point.Northing,
                Depth = point.Depth,
                Altitude = point.Altitude,
                Heading = point.Heading
            });
        }
        
        Log($"  Raw Data tab: {RawDataItems.Count} records loaded");
    }
    
    /// <summary>
    /// Populate all display collections after processing
    /// </summary>
    private void PopulateDisplayCollections()
    {
        // Recalculate pagination
        RecalculateTotalPages();
        _currentPage = 1;
        
        // Populate first page using pagination
        RefreshCurrentPageData();
        
        // Legacy ProcessedPoints (for export - no pagination limit)
        ProcessedPoints.Clear();
        foreach (var point in _processedPoints)
        {
            ProcessedPoints.Add(new ProcessedPointDisplay
            {
                RecordNumber = point.RecordNumber,
                Kp = point.Kp ?? 0,
                Easting = point.X,
                Northing = point.Y,
                Depth = point.CalculatedZ ?? point.Depth ?? 0,
                Dcc = point.Dcc ?? 0,
                TideCorrection = point.TideCorrection ?? 0,
                CorrectedDepth = point.CalculatedZ ?? point.Depth ?? 0
            });
        }
        
        Log($"  Total data points: {_processedPoints.Count}");
        Log($"  Page size: {PageSize}, Total pages: {TotalPages}");
        if (KpDccMode == KpDccMode.None)
        {
            Log($"  Note: KP/DCC disabled - KP and DCC columns show 0");
        }
        
        // Populate Spline Fitted Items (limited for display)
        SplineFittedItems.Clear();
        if (_splineFittedPoints.Count > 0)
        {
            double totalDist = 0;
            int limit = Math.Min(_splineFittedPoints.Count, MAX_DISPLAY_LIMIT);
            for (int i = 0; i < limit; i++)
            {
                var pt = _splineFittedPoints[i];
                if (i > 0)
                {
                    var prevPt = _splineFittedPoints[i - 1];
                    double dx = pt.Easting - prevPt.Easting;
                    double dy = pt.Northing - prevPt.Northing;
                    totalDist += Math.Sqrt(dx * dx + dy * dy);
                }
                
                SplineFittedItems.Add(new SplineFittedDisplay
                {
                    Index = i + 1,
                    X = pt.Easting,
                    Y = pt.Northing,
                    Z = pt.Depth ?? 0,
                    DistanceFromStart = totalDist
                });
            }
            Log($"  Spline Fitted: {_splineFittedPoints.Count} points (displaying {SplineFittedItems.Count})");
        }
        
        // Populate Interval Point Items (limited for display)
        IntervalPointItems.Clear();
        if (_intervalPoints.Count > 0)
        {
            int limit = Math.Min(_intervalPoints.Count, MAX_DISPLAY_LIMIT);
            for (int i = 0; i < limit; i++)
            {
                var pt = _intervalPoints[i];
                IntervalPointItems.Add(new IntervalPointDisplay
                {
                    Index = i + 1,
                    Distance = pt.Distance,
                    X = pt.X,
                    Y = pt.Y,
                    Z = pt.Z
                });
            }
            Log($"  Interval Points: {_intervalPoints.Count} points (displaying {IntervalPointItems.Count})");
        }
        
        // Rebuild smoothing comparison from actual data (for editor sync cases)
        RebuildSmoothingComparisonFromData();
        
        // Update property notifications
        OnPropertyChanged(nameof(SplineFittedPointCount));
        OnPropertyChanged(nameof(IntervalPointCount));
        OnPropertyChanged(nameof(HasIntervalPoints));
        OnPropertyChanged(nameof(GeneratedLayersInfo));
        OnPropertyChanged(nameof(HasSplineFittedPoints));
        OnPropertyChanged(nameof(CurrentPage));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageInfo));
    }
    
    /// <summary>
    /// Rebuild smoothing comparison by comparing SmoothedEasting/Northing to original Easting/Northing
    /// This is used after editor sync when _lastSmoothingResult may not be available
    /// </summary>
    private void RebuildSmoothingComparisonFromData()
    {
        // If we already have smoothing comparison items from Step 6 processing, keep them
        if (SmoothingComparisonItems.Count > 0) return;
        
        // If we have no original points to compare with, skip
        if (_originalPoints.Count == 0 && _processedPoints.Count == 0) return;
        
        var items = new List<SmoothingComparisonItem>();
        
        // Use processed points (they have SmoothedEasting/SmoothedNorthing from editor)
        foreach (var pt in _processedPoints)
        {
            // Check if this point has been smoothed (SmoothedEasting != Easting)
            double originalE = pt.Easting;
            double originalN = pt.Northing;
            double smoothedE = pt.SmoothedEasting ?? pt.Easting;
            double smoothedN = pt.SmoothedNorthing ?? pt.Northing;
            
            // If there's a difference, add to comparison
            double posDiff = Math.Sqrt(Math.Pow(smoothedE - originalE, 2) + Math.Pow(smoothedN - originalN, 2));
            
            if (posDiff > 0.0001) // Threshold for "different"
            {
                items.Add(new SmoothingComparisonItem
                {
                    RecordNumber = pt.RecordNumber,
                    OriginalEasting = originalE,
                    OriginalNorthing = originalN,
                    SmoothedEasting = smoothedE,
                    SmoothedNorthing = smoothedN,
                    OriginalDepth = pt.Depth ?? 0,
                    SmoothedDepth = pt.CorrectedDepth ?? pt.Depth ?? 0,
                    PositionDiff = posDiff
                });
            }
        }
        
        // Populate the observable collection
        if (items.Count > 0)
        {
            SmoothingComparisonItems.Clear();
            foreach (var item in items.Take(MAX_DISPLAY_LIMIT)) // Limit for performance
            {
                SmoothingComparisonItems.Add(item);
            }
            Log($"  Smoothing comparison rebuilt: {items.Count} modified points (displaying {SmoothingComparisonItems.Count})");
        }
    }
    
    private async Task CalculateKpDccAsync()
    {
        // Check if we have route data from Step 2
        if (_step2ViewModel?.RouteData == null)
        {
            Log("Warning: No route data available. KP/DCC calculation skipped.");
            Log("  Please load a route file in Step 2.");
            return;
        }
        
        var routeData = _step2ViewModel.RouteData;
        Log($"Calculating KP/DCC using route: {routeData.Name}");
        Log($"  Route KP range: {routeData.StartKp:F6} to {routeData.EndKp:F6}");
        Log($"  Route has {routeData.Segments.Count} segments");
        
        // Log first segment details for debugging
        if (routeData.Segments.Count > 0)
        {
            var firstSeg = routeData.Segments[0];
            Log($"  First segment: E({firstSeg.StartEasting:F2}, {firstSeg.EndEasting:F2}) N({firstSeg.StartNorthing:F2}, {firstSeg.EndNorthing:F2})");
            Log($"  First segment KP: {firstSeg.StartKp:F6} to {firstSeg.EndKp:F6}");
        }
        
        // Log first survey point for comparison (using X, Y which are smoothed or raw)
        if (_processedPoints.Count > 0)
        {
            var firstPoint = _processedPoints[0];
            Log($"  First survey point: X={firstPoint.X:F2}, Y={firstPoint.Y:F2}");
            if (firstPoint.SmoothedEasting.HasValue)
                Log($"    (Raw: E={firstPoint.Easting:F2}, N={firstPoint.Northing:F2})");
        }
        
        // Get KP unit from project settings
        var kpUnit = _project?.KpUnit ?? LengthUnit.Kilometer;
        Log($"  Output KP unit: {kpUnit}");
        
        var kpCalculator = new KpCalculator(routeData, kpUnit);
        
        int calculated = 0;
        double maxDcc = 0;
        double minKp = double.MaxValue;
        double maxKp = double.MinValue;
        
        // Store first few results for logging
        var sampleResults = new List<(int recNum, double x, double y, double kp, double dcc)>();
        
        await Task.Run(() =>
        {
            foreach (var point in _processedPoints)
            {
                // Use X, Y (SmoothedEasting/Northing or raw if not smoothed)
                var (kp, dcc) = kpCalculator.Calculate(point.X, point.Y);
                
                if (CalculateKp)
                    point.Kp = kp;
                if (CalculateDcc)
                    point.Dcc = dcc;
                    
                if (Math.Abs(dcc) > maxDcc)
                    maxDcc = Math.Abs(dcc);
                    
                if (kp < minKp) minKp = kp;
                if (kp > maxKp) maxKp = kp;
                
                // Store first 3 sample results
                if (sampleResults.Count < 3)
                {
                    sampleResults.Add((point.RecordNumber, point.X, point.Y, kp, dcc));
                }
                    
                calculated++;
            }
        });
        
        // Log sample results
        Log("  Sample calculations:");
        foreach (var (recNum, x, y, kp, dcc) in sampleResults)
        {
            Log($"    Rec#{recNum}: X={x:F2}, Y={y:F2} → KP={kp:F6}, DCC={dcc:F3}");
        }
        
        string modeStr = KpDccMode switch
        {
            KpDccMode.Both => "KP and DCC",
            KpDccMode.KpOnly => "KP only",
            KpDccMode.DccOnly => "DCC only",
            _ => "Unknown"
        };
        
        Log($"  {modeStr} calculated for {calculated} points");
        Log($"  Calculated KP range: {minKp:F6} to {maxKp:F6}");
        Log($"  Max DCC: {maxDcc:F3} (coordinate units)");
        
        // Warn if DCC values seem unreasonable (far from route)
        if (maxDcc > 1000)
        {
            Log($"  ⚠ WARNING: Large DCC values detected!");
            Log($"    This usually means survey data is in different coordinates than route.");
            Log($"    Check if both use the same Coordinate Reference System.");
        }
        
        // Warn if calculated KP is outside route KP range
        if (minKp < routeData.StartKp - 1 || maxKp > routeData.EndKp + 1)
        {
            Log($"  ⚠ WARNING: Calculated KP values outside route KP range!");
            Log($"    Route KP: {routeData.StartKp:F6} to {routeData.EndKp:F6}");
            Log($"    Calculated KP: {minKp:F6} to {maxKp:F6}");
        }
    }
    
    /// <summary>
    /// Validates that survey data coordinates are compatible with route coordinates.
    /// This is critical - if CRS doesn't match, KP/DCC will be completely wrong!
    /// </summary>
    private async Task ValidateCoordinateSystemsAsync()
    {
        await Task.Run(() =>
        {
            if (_processedPoints.Count == 0) return;
            
            // Get survey data coordinate ranges
            double minSurveyE = _processedPoints.Min(p => p.Easting);
            double maxSurveyE = _processedPoints.Max(p => p.Easting);
            double minSurveyN = _processedPoints.Min(p => p.Northing);
            double maxSurveyN = _processedPoints.Max(p => p.Northing);
            
            Log($"  Survey data coordinate range:");
            Log($"    Easting:  {minSurveyE:F2} to {maxSurveyE:F2}");
            Log($"    Northing: {minSurveyN:F2} to {maxSurveyN:F2}");
            
            // Check if coordinates look like geographic (lat/lon)
            bool surveyLooksLikeLatLon = IsLikelyLatLon(minSurveyE, maxSurveyE, minSurveyN, maxSurveyN);
            
            if (surveyLooksLikeLatLon)
            {
                Log("  ⚠ WARNING: Survey coordinates appear to be Lat/Lon (geographic)!");
                Log("    Values between -180 and 180 suggest unprojected coordinates.");
            }
            
            // Check route if available
            if (_step2ViewModel?.RouteData != null)
            {
                var route = _step2ViewModel.RouteData;
                var firstSeg = route.Segments.FirstOrDefault();
                var lastSeg = route.Segments.LastOrDefault();
                
                if (firstSeg != null && lastSeg != null)
                {
                    double minRouteE = Math.Min(firstSeg.StartEasting, lastSeg.EndEasting);
                    double maxRouteE = Math.Max(firstSeg.StartEasting, lastSeg.EndEasting);
                    double minRouteN = Math.Min(firstSeg.StartNorthing, lastSeg.EndNorthing);
                    double maxRouteN = Math.Max(firstSeg.StartNorthing, lastSeg.EndNorthing);
                    
                    // Get actual min/max from all segments
                    foreach (var seg in route.Segments)
                    {
                        minRouteE = Math.Min(minRouteE, Math.Min(seg.StartEasting, seg.EndEasting));
                        maxRouteE = Math.Max(maxRouteE, Math.Max(seg.StartEasting, seg.EndEasting));
                        minRouteN = Math.Min(minRouteN, Math.Min(seg.StartNorthing, seg.EndNorthing));
                        maxRouteN = Math.Max(maxRouteN, Math.Max(seg.StartNorthing, seg.EndNorthing));
                    }
                    
                    Log($"  Route coordinate range:");
                    Log($"    Easting:  {minRouteE:F2} to {maxRouteE:F2}");
                    Log($"    Northing: {minRouteN:F2} to {maxRouteN:F2}");
                    
                    bool routeLooksLikeLatLon = IsLikelyLatLon(minRouteE, maxRouteE, minRouteN, maxRouteN);
                    
                    // Check magnitude compatibility
                    double surveyMagnitude = Math.Max(Math.Abs(maxSurveyE), Math.Abs(maxSurveyN));
                    double routeMagnitude = Math.Max(Math.Abs(maxRouteE), Math.Abs(maxRouteN));
                    
                    // If one is lat/lon and the other is projected, they're incompatible
                    if (surveyLooksLikeLatLon != routeLooksLikeLatLon)
                    {
                        Log("  ❌ ERROR: Survey and Route appear to be in DIFFERENT coordinate systems!");
                        Log("    One appears to be geographic (lat/lon), the other projected.");
                        Log("    KP/DCC calculations will be INCORRECT!");
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                "COORDINATE SYSTEM MISMATCH DETECTED!\n\n" +
                                "Survey data and Route appear to be in different coordinate systems.\n" +
                                "One appears to be geographic (lat/lon) and the other projected.\n\n" +
                                "KP/DCC calculations will be INCORRECT.\n\n" +
                                "Please ensure both files use the same Coordinate Reference System.",
                                "CRS Warning",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        });
                    }
                    // Check if they're in completely different regions (off by orders of magnitude)
                    else if (surveyMagnitude > 0 && routeMagnitude > 0)
                    {
                        double ratio = surveyMagnitude / routeMagnitude;
                        if (ratio > 100 || ratio < 0.01)
                        {
                            Log($"  ⚠ WARNING: Coordinate magnitudes differ significantly!");
                            Log($"    Survey magnitude: {surveyMagnitude:E2}");
                            Log($"    Route magnitude: {routeMagnitude:E2}");
                            Log($"    This may indicate different coordinate systems or units.");
                        }
                    }
                    
                    // Check if survey data overlaps with route (allow some buffer)
                    double routeWidth = Math.Max(maxRouteE - minRouteE, maxRouteN - minRouteN);
                    double buffer = routeWidth * 10; // Allow 10x route width as search area
                    
                    bool overlaps = 
                        maxSurveyE >= (minRouteE - buffer) && minSurveyE <= (maxRouteE + buffer) &&
                        maxSurveyN >= (minRouteN - buffer) && minSurveyN <= (maxRouteN + buffer);
                    
                    if (!overlaps)
                    {
                        Log("  ❌ WARNING: Survey data does NOT overlap with route area!");
                        Log("    Survey may be in a completely different location or CRS.");
                    }
                    else
                    {
                        Log("  ✓ Survey data overlaps with route area.");
                    }
                    
                    // Calculate minimum distance from survey centroid to route
                    double surveyMidE = (minSurveyE + maxSurveyE) / 2;
                    double surveyMidN = (minSurveyN + maxSurveyN) / 2;
                    
                    double minDistanceToRoute = double.MaxValue;
                    foreach (var seg in route.Segments)
                    {
                        // Distance to segment start
                        double d1 = Math.Sqrt(Math.Pow(surveyMidE - seg.StartEasting, 2) + 
                                              Math.Pow(surveyMidN - seg.StartNorthing, 2));
                        // Distance to segment end
                        double d2 = Math.Sqrt(Math.Pow(surveyMidE - seg.EndEasting, 2) + 
                                              Math.Pow(surveyMidN - seg.EndNorthing, 2));
                        minDistanceToRoute = Math.Min(minDistanceToRoute, Math.Min(d1, d2));
                    }
                    
                    const double MISMATCH_WARNING_THRESHOLD = 500.0; // 500 meters
                    
                    if (minDistanceToRoute > MISMATCH_WARNING_THRESHOLD && !surveyLooksLikeLatLon && !routeLooksLikeLatLon)
                    {
                        Log($"  ⚠ WARNING: Survey data center is {minDistanceToRoute:F1}m from nearest route point!");
                        Log("    This may indicate data mismatch or wrong coordinate system.");
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var result = MessageBox.Show(
                                $"📍 DATA LOCATION WARNING\n\n" +
                                $"The survey data center is approximately {minDistanceToRoute:F0} meters away from the route.\n\n" +
                                $"This could indicate:\n" +
                                $"  • Wrong RLX file loaded\n" +
                                $"  • Wrong survey data file loaded\n" +
                                $"  • Different coordinate systems (CRS)\n" +
                                $"  • Survey data from a different area\n\n" +
                                $"Survey Center: E {surveyMidE:F1}, N {surveyMidN:F1}\n" +
                                $"Route Range: E {minRouteE:F1}-{maxRouteE:F1}, N {minRouteN:F1}-{maxRouteN:F1}\n\n" +
                                $"Do you want to continue anyway?",
                                "⚠ RLX/Data Mismatch Detected",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning);
                            
                            if (result == MessageBoxResult.No)
                            {
                                throw new OperationCanceledException("Processing cancelled by user due to data mismatch.");
                            }
                        });
                    }
                    else if (minDistanceToRoute <= MISMATCH_WARNING_THRESHOLD)
                    {
                        Log($"  ✓ Survey data is within {minDistanceToRoute:F1}m of route - good match.");
                    }
                }
            }
        });
    }
    
    /// <summary>
    /// Check if coordinate range looks like geographic lat/lon
    /// </summary>
    private bool IsLikelyLatLon(double minE, double maxE, double minN, double maxN)
    {
        // Geographic coordinates are typically:
        // Longitude: -180 to 180
        // Latitude: -90 to 90
        // Projected coordinates are typically much larger (thousands to millions)
        
        bool eastingInLatLonRange = minE >= -180 && maxE <= 180;
        bool northingInLatLonRange = minN >= -90 && maxN <= 90;
        
        // Also check if the values are "small" (typical of degrees)
        bool eastingSmall = Math.Abs(maxE) < 200 && Math.Abs(minE) < 200;
        bool northingSmall = Math.Abs(maxN) < 100 && Math.Abs(minN) < 100;
        
        return (eastingInLatLonRange && northingInLatLonRange) || (eastingSmall && northingSmall);
    }
    
    private void BuildSmoothingComparison()
    {
        if (_lastSmoothingResult == null || _originalPoints.Count == 0) return;
        
        // Build comparison items on background thread
        var items = new List<SmoothingComparisonItem>();
        
        foreach (var index in _lastSmoothingResult.ModifiedPointIndices.OrderBy(i => i))
        {
            if (index < 0 || index >= _originalPoints.Count || index >= _processedPoints.Count)
                continue;
                
            var original = _originalPoints[index];
            var processed = _processedPoints[index];
            
            // Get smoothed values (or original if not smoothed)
            double smoothedE = processed.SmoothedEasting ?? original.Easting;
            double smoothedN = processed.SmoothedNorthing ?? original.Northing;
            double smoothedD = processed.SmoothedDepth ?? original.Depth ?? 0;
            double smoothedA = processed.SmoothedAltitude ?? original.Altitude ?? 0;
            
            items.Add(new SmoothingComparisonItem
            {
                RecordNumber = original.RecordNumber,
                OriginalEasting = original.Easting,
                OriginalNorthing = original.Northing,
                OriginalDepth = original.Depth ?? 0,
                OriginalAltitude = original.Altitude ?? 0,
                SmoothedEasting = smoothedE,
                SmoothedNorthing = smoothedN,
                SmoothedDepth = smoothedD,
                SmoothedAltitude = smoothedA,
                EastingDiff = smoothedE - original.Easting,
                NorthingDiff = smoothedN - original.Northing,
                DepthDiff = smoothedD - (original.Depth ?? 0),
                AltitudeDiff = smoothedA - (original.Altitude ?? 0),
                PositionDiff = Math.Sqrt(
                    Math.Pow(smoothedE - original.Easting, 2) + 
                    Math.Pow(smoothedN - original.Northing, 2))
            });
        }
        
        // Update ObservableCollection on UI thread
        if (Application.Current?.Dispatcher.CheckAccess() == true)
        {
            SmoothingComparisonItems.Clear();
            foreach (var item in items)
                SmoothingComparisonItems.Add(item);
        }
        else
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                SmoothingComparisonItems.Clear();
                foreach (var item in items)
                    SmoothingComparisonItems.Add(item);
            });
        }
        
        Log($"  Comparison data built for {items.Count} modified points");
    }
    
    /// <summary>
    /// Export smoothing report showing all changes
    /// </summary>
    public async Task ExportSmoothingReportAsync(string filePath)
    {
        if (_lastSmoothingResult == null || SmoothingComparisonItems.Count == 0)
        {
            MessageBox.Show("No smoothing data available to export.", "Export", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        // Copy data to local list to avoid cross-thread collection access
        var items = SmoothingComparisonItems.ToList();
        var smoothingResult = _lastSmoothingResult;
        var projectName = _project?.ProjectName ?? "Unknown";
        var positionSmoothing = SmoothPosition;
        var depthSmoothing = SmoothDepth;
        var posMethod = PositionSmoothingMethod;
        var depMethod = DepthSmoothingMethod;
        var posWindowSize = PositionWindowSize;
        var posThreshold = PositionThreshold;
        var depWindowSize = DepthWindowSize;
        var depThreshold = DepthThreshold;
        var showPosWin = ShowPositionWindowSize;
        var showPosThresh = ShowPositionThreshold;
        var showDepWin = ShowDepthWindowSize;
        var showDepThresh = ShowDepthThreshold;
        
        await Task.Run(() =>
        {
            using var writer = new StreamWriter(filePath);
            
            // Write header
            writer.WriteLine("# Smoothing Report");
            writer.WriteLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"# Project: {projectName}");
            writer.WriteLine("#");
            writer.WriteLine("# Settings:");
            writer.WriteLine($"#   Position Smoothing: {(positionSmoothing ? posMethod.ToString() : "Disabled")}");
            if (positionSmoothing)
            {
                if (showPosWin) writer.WriteLine($"#   Position Window Size: {posWindowSize}");
                if (showPosThresh) writer.WriteLine($"#   Position Threshold: {posThreshold:F2}m");
            }
            writer.WriteLine($"#   Depth Smoothing: {(depthSmoothing ? depMethod.ToString() : "Disabled")}");
            if (depthSmoothing)
            {
                if (showDepWin) writer.WriteLine($"#   Depth Window Size: {depWindowSize}");
                if (showDepThresh) writer.WriteLine($"#   Depth Threshold: {depThreshold:F2}");
            }
            writer.WriteLine("#");
            writer.WriteLine("# Summary:");
            writer.WriteLine($"#   Total Points: {smoothingResult.TotalPoints}");
            writer.WriteLine($"#   Position Points Modified: {smoothingResult.PositionPointsModified}");
            writer.WriteLine($"#   Depth Points Modified: {smoothingResult.DepthPointsModified}");
            writer.WriteLine($"#   Max Position Correction: {smoothingResult.MaxPositionCorrection:F4}m");
            writer.WriteLine($"#   Max Depth Correction: {smoothingResult.MaxDepthCorrection:F4}");
            writer.WriteLine("#");
            
            // Write column headers
            writer.WriteLine("RecNo,OrigEasting,OrigNorthing,OrigDepth,SmoothEasting,SmoothNorthing,SmoothDepth,EastingDiff,NorthingDiff,DepthDiff,PositionDiff");
            
            // Write data from local copy
            foreach (var item in items)
            {
                writer.WriteLine($"{item.RecordNumber}," +
                    $"{item.OriginalEasting:F4},{item.OriginalNorthing:F4},{item.OriginalDepth:F4}," +
                    $"{item.SmoothedEasting:F4},{item.SmoothedNorthing:F4},{item.SmoothedDepth:F4}," +
                    $"{item.EastingDiff:F4},{item.NorthingDiff:F4},{item.DepthDiff:F4},{item.PositionDiff:F4}");
            }
        });
        
        Log($"Smoothing report exported to: {Path.GetFileName(filePath)}");
    }

    /// <summary>
    /// Open the 3D Viewer window with current processed data
    /// </summary>
    public void Open3DViewer()
    {
        // NOTE: In module-based licensing, all module features are available
        // The license check is done at module launch in ModuleManager.LaunchModule()
        
        if (!IsProcessed || _processedPoints.Count == 0)
        {
            MessageBox.Show("Please process data first before opening 3D Viewer.", 
                "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var viewer = new Views.Viewer3DWindow();
            
            // Load survey data
            viewer.LoadSurveyData(_processedPoints);
            
            // Load route if available
            if (_step2ViewModel?.RouteData != null)
            {
                viewer.LoadRouteData(_step2ViewModel.RouteData);
            }
            
            // Load field layout if available
            if (_step2ViewModel?.FieldLayout != null && !string.IsNullOrEmpty(_step2ViewModel.FieldLayoutPath))
            {
                viewer.LoadFieldLayout(_step2ViewModel.FieldLayoutPath);
            }
            
            viewer.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening 3D Viewer:\n\n{ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Open the Survey Editor window for real-time editing and smoothing
    /// </summary>
    public void OpenSurveyEditor()
    {
        // NOTE: In module-based licensing, all module features are available
        // The license check is done at module launch in ModuleManager.LaunchModule()
        
        if (!IsProcessed || _processedPoints.Count == 0)
        {
            MessageBox.Show("Please process data first before opening the Editor.", 
                "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var editor = new Views.SurveyEditorWindow();
            
            // Comprehensive sync callback - handles ALL editor changes
            Action<Models.EditorSyncData> onSyncDataApplied = (syncData) =>
            {
                OnEditorSyncDataApplied(syncData);
            };
            
            // Simple callback for backwards compatibility
            Action<List<SurveyPoint>> onChangesApplied = (updatedPoints) =>
            {
                OnEditorChangesApplied(updatedPoints);
            };
            
            // Load survey data - include spline-fitted points if available
            if (_splineFittedPoints.Count > 0)
            {
                editor.LoadSurveyDataWithSpline(_processedPoints, _splineFittedPoints, _step2ViewModel?.RouteData, 
                    onChangesApplied, _processedPoints, onSyncDataApplied);
            }
            else
            {
                editor.LoadSurveyData(_processedPoints, onChangesApplied, _processedPoints, onSyncDataApplied);
                
                // Load route if available
                if (_step2ViewModel?.RouteData != null)
                {
                    editor.LoadRouteData(_step2ViewModel.RouteData);
                }
            }
            
            // Load interval points if available
            if (_intervalPoints.Count > 0)
            {
                editor.LoadIntervalPoints(_intervalPoints);
            }
            
            // Load field layout if available
            if (_step2ViewModel?.FieldLayout != null && !string.IsNullOrEmpty(_step2ViewModel.FieldLayoutPath))
            {
                editor.LoadFieldLayout(_step2ViewModel.FieldLayoutPath);
            }
            
            editor.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening Survey Editor:\n\n{ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Comprehensive sync handler - receives ALL changes from Editor
    /// Updates spline points, interval points, added points, deleted points
    /// Recalculates KP/DCC for modified points
    /// </summary>
    private void OnEditorSyncDataApplied(Models.EditorSyncData syncData)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
        {
            try
            {
                Log("Starting editor sync...");
                
                // 1. Update main survey points
                _processedPoints = syncData.SurveyPoints;
                Log($"Received {_processedPoints.Count} survey points from Editor");
                
                // 2. Update spline-fitted points
                if (syncData.HasSpline)
                {
                    _splineFittedPoints = syncData.SplinePoints;
                    Log($"Synced {_splineFittedPoints.Count} spline points from Editor");
                }
                
                // 3. Update interval points
                if (syncData.HasIntervalPoints)
                {
                    _intervalPoints = syncData.IntervalPoints;
                    Log($"Synced {_intervalPoints.Count} interval points from Editor");
                }
                
                // 4. Handle added points (merge into processed points)
                if (syncData.HasAddedPoints)
                {
                    foreach (var addedPt in syncData.AddedPoints)
                    {
                        _processedPoints.Add(addedPt);
                    }
                    Log($"Added {syncData.AddedPoints.Count} new points from Editor");
                }
                
                // 5. Handle deleted points (remove from processed points)
                if (syncData.HasDeletedPoints)
                {
                    // Sort descending so we remove from end first (preserve indices)
                    foreach (var index in syncData.DeletedPointIndices.OrderByDescending(i => i))
                    {
                        if (index >= 0 && index < _processedPoints.Count)
                        {
                            _processedPoints.RemoveAt(index);
                        }
                    }
                    Log($"Removed {syncData.DeletedPointIndices.Count} points from Editor");
                }
                
                // 6. CRITICAL: Recalculate KP/DCC for all points if route data is available
                // This ensures the tables show correct values after points are moved in the editor
                if (_step2ViewModel?.RouteData != null && KpDccMode != KpDccMode.None)
                {
                    Log("Recalculating KP/DCC for modified points...");
                    await RecalculateKpDccForEditorChangesAsync();
                }
                
                // 7. Clear and refresh all display collections
                ProcessedPoints.Clear();
                RawDataItems.Clear();
                FinalListingItems.Clear();
                ComparisonItems.Clear();
                SmoothingComparisonItems.Clear();
                SplineFittedItems.Clear();
                IntervalPointItems.Clear();
                
                // Repopulate Raw Data tab if we have original points
                if (_originalPoints.Count > 0)
                {
                    PopulateRawDataTab();
                }
                
                // Repopulate all display collections
                PopulateDisplayCollections();
                
                // 8. Update UI notifications
                OnPropertyChanged(nameof(TotalPoints));
                OnPropertyChanged(nameof(KpRange));
                OnPropertyChanged(nameof(SplineFittedPointCount));
                OnPropertyChanged(nameof(IntervalPointCount));
                OnPropertyChanged(nameof(HasIntervalPoints));
                OnPropertyChanged(nameof(HasSplineFittedPoints));
                
                // Build summary message
                var changes = new List<string>();
                changes.Add($"{_processedPoints.Count} survey points");
                if (syncData.HasSpline) changes.Add($"{_splineFittedPoints.Count} spline points");
                if (syncData.HasIntervalPoints) changes.Add($"{_intervalPoints.Count} interval points");
                if (syncData.HasAddedPoints) changes.Add($"{syncData.AddedPoints.Count} added");
                if (syncData.HasDeletedPoints) changes.Add($"{syncData.DeletedPointIndices.Count} deleted");
                
                StatusMessage = $"Editor sync complete: {string.Join(", ", changes)}";
                Log($"Full editor sync completed: {string.Join(", ", changes)}");
            }
            catch (Exception ex)
            {
                Log($"Error during editor sync: {ex.Message}");
                StatusMessage = $"Sync error: {ex.Message}";
            }
        });
    }
    
    /// <summary>
    /// Recalculate KP/DCC for all points after editor changes
    /// Uses the current X/Y (which includes SmoothedEasting/SmoothedNorthing)
    /// </summary>
    private async Task RecalculateKpDccForEditorChangesAsync()
    {
        if (_step2ViewModel?.RouteData == null) return;
        
        var routeData = _step2ViewModel.RouteData;
        var kpUnit = _project?.KpUnit ?? LengthUnit.Kilometer;
        var kpCalculator = new KpCalculator(routeData, kpUnit);
        
        await Task.Run(() =>
        {
            foreach (var point in _processedPoints)
            {
                // Use X/Y which returns SmoothedEasting/SmoothedNorthing if available
                double x = point.X;
                double y = point.Y;
                
                // Calculate KP and DCC using existing KpCalculator
                var (kp, dcc) = kpCalculator.Calculate(x, y);
                
                // Update point with recalculated values
                if (KpDccMode == KpDccMode.Both || KpDccMode == KpDccMode.KpOnly)
                {
                    point.Kp = kp;
                }
                if (KpDccMode == KpDccMode.Both || KpDccMode == KpDccMode.DccOnly)
                {
                    point.Dcc = dcc;
                }
            }
        });
        
        Log($"KP/DCC recalculated for {_processedPoints.Count} points");
    }
    
    /// <summary>
    /// Called when editor applies changes - syncs data back to Step 6 (simple version)
    /// </summary>
    private void OnEditorChangesApplied(List<SurveyPoint> updatedPoints)
    {
        // Update our processed points with the editor's changes
        _processedPoints = updatedPoints;
        
        // Refresh display collections
        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
        {
            try
            {
                // Recalculate KP/DCC if route data is available
                if (_step2ViewModel?.RouteData != null && KpDccMode != KpDccMode.None)
                {
                    await RecalculateKpDccForEditorChangesAsync();
                }
                
                ProcessedPoints.Clear();
                RawDataItems.Clear();
                FinalListingItems.Clear();
                ComparisonItems.Clear();
                SmoothingComparisonItems.Clear();
                SplineFittedItems.Clear();
                IntervalPointItems.Clear();
                
                // Repopulate Raw Data tab if we have original points
                if (_originalPoints.Count > 0)
                {
                    PopulateRawDataTab();
                }
                
                PopulateDisplayCollections();
                
                // Update UI notifications
                OnPropertyChanged(nameof(TotalPoints));
                OnPropertyChanged(nameof(KpRange));
                OnPropertyChanged(nameof(SplineFittedPointCount));
                OnPropertyChanged(nameof(IntervalPointCount));
                OnPropertyChanged(nameof(HasIntervalPoints));
                OnPropertyChanged(nameof(HasSplineFittedPoints));
                
                StatusMessage = $"Editor changes applied: {_processedPoints.Count} points updated";
                Log($"Editor changes synchronized: {_processedPoints.Count} points");
            }
            catch (Exception ex)
            {
                Log($"Error syncing editor changes: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Open the Charts window for data visualization
    /// </summary>
    public void OpenCharts()
    {
        if (!IsProcessed || _processedPoints.Count == 0)
        {
            MessageBox.Show("Please process data first before opening Charts.", 
                "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var chartWindow = new Views.ChartWindow();
            chartWindow.LoadSurveyData(_processedPoints, _step2ViewModel?.RouteData);
            chartWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening Charts:\n\n{ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Generate spline-fitted survey track asynchronously
    /// Stores the spline-fitted points - user can view in editor separately
    /// </summary>
    public async Task GenerateSplineFitAsync()
    {
        if (!IsProcessed || _processedPoints.Count < 2)
        {
            MessageBox.Show("Please process data first before generating spline fit.", 
                "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            StatusMessage = "Generating spline fit... Please wait.";
            Progress = 10;
            Log("Starting spline fit generation...");

            // Prepare input points
            var inputPoints = _processedPoints.Select(p => (
                p.SmoothedEasting ?? p.Easting,
                p.SmoothedNorthing ?? p.Northing,
                p.CorrectedDepth ?? p.Depth ?? 0.0
            )).ToList();

            List<(double X, double Y, double Z)> splineResults = new();
            
            Progress = 20;
            
            // Run spline generation on background thread
            await Task.Run(() =>
            {
                var splineService = new SplineService();
                // Use Catmull-Rom (Subsea7 recommended) with default tension
                splineResults = splineService.GenerateSpline(
                    inputPoints, 
                    SplineAlgorithm.CatmullRom, 
                    tension: 0.5, 
                    outputPointCount: Math.Max(inputPoints.Count * 5, 500));
            });

            Progress = 80;
            Log($"Generated {splineResults.Count} spline points");

            // Store spline points
            _splineFittedPoints.Clear();
            for (int i = 0; i < splineResults.Count; i++)
            {
                var pt = splineResults[i];
                _splineFittedPoints.Add(new SurveyPoint
                {
                    RecordNumber = i + 1,
                    Easting = pt.X,
                    Northing = pt.Y,
                    Depth = pt.Z,
                    SmoothedEasting = pt.X,
                    SmoothedNorthing = pt.Y,
                    CorrectedDepth = pt.Z
                });
            }

            Progress = 100;
            StatusMessage = $"Spline fit complete: {splineResults.Count} points generated. Open Editor to view.";
            Log($"Spline fit complete. {splineResults.Count} points stored. Open Editor to view the spline layer.");
            
            OnPropertyChanged(nameof(HasSplineFittedPoints));
            OnPropertyChanged(nameof(SplineFittedPointCount));
            
            MessageBox.Show($"Spline fit complete!\n\n{splineResults.Count} points generated.\n\nClick 'Editor' button to view the spline-fitted layer.", 
                "Spline Fit Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusMessage = "Spline fit failed";
            Log($"Error: {ex.Message}");
            MessageBox.Show($"Error generating spline fit:\n\n{ex.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Check if spline-fitted points are available
    /// </summary>
    public bool HasSplineFittedPoints => _splineFittedPoints.Count > 0;
    
    /// <summary>
    /// Number of spline-fitted points
    /// </summary>
    public int SplineFittedPointCount => _splineFittedPoints.Count;
    
    /// <summary>
    /// Get the spline-fitted points for use in editor
    /// </summary>
    public List<SurveyPoint> GetSplineFittedPoints() => _splineFittedPoints;
    
    /// <summary>
    /// Get the interval measure points for use in editor
    /// </summary>
    public List<(double X, double Y, double Z, double Distance)> GetIntervalPoints() => _intervalPoints;
    
    /// <summary>
    /// Get processed points for export
    /// </summary>
    public List<SurveyPoint> GetProcessedPoints() => _processedPoints;
    
    /// <summary>
    /// Get route data for export
    /// </summary>
    public RouteData? GetRouteData() => _step2ViewModel?.RouteData;
    
    /// <summary>
    /// Get all export data (processed, spline, interval points)
    /// </summary>
    public (List<SurveyPoint> Processed, List<SurveyPoint> Spline, List<(double X, double Y, double Z, double Distance)> Interval) GetAllExportData()
    {
        return (_processedPoints, _splineFittedPoints, _intervalPoints);
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        
        // Ensure we're on the UI thread when adding to ObservableCollection
        if (Application.Current?.Dispatcher.CheckAccess() == true)
        {
            ProcessingLog.Add(logEntry);
        }
        else
        {
            Application.Current?.Dispatcher.Invoke(() => ProcessingLog.Add(logEntry));
        }
    }

    public void LoadProject(Project project)
    {
        _project = project;
        FitSplineToTrack = project.ProcessingOptions.FitSplineToTrack;
        PointInterval = project.ProcessingOptions.PointInterval;
        KpDccMode = project.ProcessingOptions.KpDccMode;
        
        // Auto-configure KP/DCC mode based on survey type
        if (project.SurveyType != SurveyType.Custom)
        {
            KpDccMode = SurveyTypeConfig.GetRecommendedKpDccMode(project.SurveyType);
        }

        IsProcessed = false;
        ProcessedPoints.Clear();
        ProcessingLog.Clear();
        SmoothingComparisonItems.Clear();
        _lastSmoothingResult = null;
        StatusMessage = "Ready to process";
    }

    public void SaveToProject(Project project)
    {
        project.ProcessingOptions.FitSplineToTrack = FitSplineToTrack;
        project.ProcessingOptions.PointInterval = PointInterval;
        project.ProcessingOptions.KpDccMode = KpDccMode;
    }

    public bool Validate()
    {
        // Validation happens during processing
        return true;
    }

    /// <summary>
    /// Get processed survey points (for backwards compatibility)
    /// </summary>
    public List<SurveyPoint>? GetProcessedData()
    {
        return _processedPoints;
    }
    
    /// <summary>
    /// Get all processed data including spline and interval points
    /// This is used by Step 7 for comprehensive export
    /// </summary>
    public ProcessedDataBundle GetAllProcessedData()
    {
        return new ProcessedDataBundle
        {
            SurveyPoints = _processedPoints,
            SplinePoints = _splineFittedPoints,
            IntervalPoints = _intervalPoints,
            HasSpline = _splineFittedPoints.Count > 0,
            HasIntervalPoints = _intervalPoints.Count > 0
        };
    }
    
    /// <summary>
    /// Get spline-fitted points (for export)
    /// </summary>
    public List<SurveyPoint> GetSplinePoints() => _splineFittedPoints;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Display model for Raw Data tab - shows original parsed NPD data
/// </summary>
public class RawDataDisplay
{
    public int RecordNumber { get; set; }
    public DateTime DateTime { get; set; }
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double? Depth { get; set; }
    public double? Altitude { get; set; }
    public double? Heading { get; set; }

    public string DateTimeFormatted => DateTime.ToString("yyyy-MM-dd HH:mm:ss");
    public string DepthFormatted => Depth?.ToString("F2") ?? "-";
    public string AltitudeFormatted => Altitude?.ToString("F2") ?? "-";
    public string HeadingFormatted => Heading?.ToString("F1") ?? "-";
}

/// <summary>
/// Display model for Final Listing tab - shows KP, DCC, X, Y, Z
/// </summary>
public class FinalListingDisplay
{
    public double Kp { get; set; }
    public double Dcc { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    
    // Status flags for deleted/added points
    public bool IsDeleted { get; set; }
    public bool IsAdded { get; set; }
    
    // Row style based on status
    public string RowStyle => IsDeleted ? "Deleted" : (IsAdded ? "Added" : "Normal");

    public string KpFormatted => Kp.ToString("F6");
    public string DccFormatted => Dcc.ToString("F3");
    public string XFormatted => X.ToString("F2");
    public string YFormatted => Y.ToString("F2");
    public string ZFormatted => Z.ToString("F2");
}

/// <summary>
/// Display model for Comparison tab - shows Raw vs Calculated
/// </summary>
public class ComparisonDisplay
{
    public int RecordNumber { get; set; }
    public DateTime DateTime { get; set; }
    
    // Raw input
    public double RawEasting { get; set; }
    public double RawNorthing { get; set; }
    public double? RawDepth { get; set; }
    public double? RawAltitude { get; set; }
    
    // Calculated output
    public double X { get; set; }
    public double Y { get; set; }
    public double? Z { get; set; }
    public double? Kp { get; set; }
    public double? Dcc { get; set; }
    
    // Status flags for deleted/added points
    public bool IsDeleted { get; set; }
    public bool IsAdded { get; set; }
    
    // Row style based on status
    public string RowStyle => IsDeleted ? "Deleted" : (IsAdded ? "Added" : "Normal");

    public string DateTimeFormatted => DateTime.ToString("HH:mm:ss");
    public string KpFormatted => Kp?.ToString("F6") ?? "-";
    public string DccFormatted => Dcc?.ToString("F3") ?? "-";
    public string ZFormatted => Z?.ToString("F2") ?? "-";
}

/// <summary>
/// Display model for processed points (legacy compatibility)
/// </summary>
public class ProcessedPointDisplay
{
    public int RecordNumber { get; set; }
    public double Kp { get; set; }
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double Depth { get; set; }
    public double Dcc { get; set; }
    public double TideCorrection { get; set; }
    public double CorrectedDepth { get; set; }

    public string KpFormatted => Kp.ToString("F6");
    public string DccFormatted => Dcc.ToString("F3");
}

/// <summary>
/// Comparison item showing original vs smoothed values
/// </summary>
public class SmoothingComparisonItem
{
    public int RecordNumber { get; set; }
    
    // Original values
    public double OriginalEasting { get; set; }
    public double OriginalNorthing { get; set; }
    public double OriginalDepth { get; set; }
    public double OriginalAltitude { get; set; }
    
    // Smoothed values
    public double SmoothedEasting { get; set; }
    public double SmoothedNorthing { get; set; }
    public double SmoothedDepth { get; set; }
    public double SmoothedAltitude { get; set; }
    
    // Differences
    public double EastingDiff { get; set; }
    public double NorthingDiff { get; set; }
    public double DepthDiff { get; set; }
    public double AltitudeDiff { get; set; }
    public double PositionDiff { get; set; } // 2D distance
    
    // Formatted for display
    public string EastingDiffFormatted => EastingDiff.ToString("+0.000;-0.000;0.000");
    public string NorthingDiffFormatted => NorthingDiff.ToString("+0.000;-0.000;0.000");
    public string DepthDiffFormatted => DepthDiff.ToString("+0.000;-0.000;0.000");
    public string AltitudeDiffFormatted => AltitudeDiff.ToString("+0.000;-0.000;0.000");
    public string PositionDiffFormatted => PositionDiff.ToString("F3");
}

/// <summary>
/// Display item for spline-fitted points
/// </summary>
public class SplineFittedDisplay
{
    public int Index { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double DistanceFromStart { get; set; }
}

/// <summary>
/// Display item for interval measure points
/// </summary>
public class IntervalPointDisplay
{
    public int Index { get; set; }
    public double Distance { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

/// <summary>
/// Bundle of all processed data for export
/// Includes survey points, spline-fitted points, and interval measure points
/// </summary>
public class ProcessedDataBundle
{
    /// <summary>
    /// Main processed survey points with smoothing applied
    /// </summary>
    public List<SurveyPoint> SurveyPoints { get; set; } = new();
    
    /// <summary>
    /// Spline-fitted track points (if generated)
    /// </summary>
    public List<SurveyPoint> SplinePoints { get; set; } = new();
    
    /// <summary>
    /// Interval measure points (if generated)
    /// </summary>
    public List<(double X, double Y, double Z, double Distance)> IntervalPoints { get; set; } = new();
    
    /// <summary>
    /// Whether spline fitting was applied
    /// </summary>
    public bool HasSpline { get; set; }
    
    /// <summary>
    /// Whether interval measure was applied
    /// </summary>
    public bool HasIntervalPoints { get; set; }
    
    /// <summary>
    /// Total number of survey points
    /// </summary>
    public int SurveyPointCount => SurveyPoints?.Count ?? 0;
    
    /// <summary>
    /// Total number of spline points
    /// </summary>
    public int SplinePointCount => SplinePoints?.Count ?? 0;
    
    /// <summary>
    /// Total number of interval points
    /// </summary>
    public int IntervalPointCount => IntervalPoints?.Count ?? 0;
}
