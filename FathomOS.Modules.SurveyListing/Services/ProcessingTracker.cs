using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.SurveyListing.Services;

/// <summary>
/// Tracks which processing steps have been completed for the crib sheet
/// </summary>
public class ProcessingTracker : INotifyPropertyChanged
{
    private static ProcessingTracker? _instance;
    public static ProcessingTracker Instance => _instance ??= new ProcessingTracker();
    
    // Source Data / Support Files
    private bool _productLayRouteLoaded;
    private bool _tidalDataLoaded;
    private bool _rawDataFilesLoaded;
    private bool _cadBackgroundLoaded;
    
    // Raw Data and Working Excel Workbook
    private bool _rawDataAdded;
    private bool _tideDataAdded;
    private bool _calculationsUpdated;
    private bool _parametersUpdated;
    
    // Process Product Position & Depth
    private bool _workingTrackCreated;
    private bool _fixesAddedToCad;
    private bool _xyzScriptCreated;
    private bool _trackAddedToBricsCAD;
    private bool _dataCoverageReviewed;
    private bool _splineFitCreated;
    private bool _productPositionReviewed;
    private bool _pointsAt1mCreated;
    private bool _pointsXyzExtracted;
    
    // 7KPCalc
    private bool _seabedDepthKpDccCalculated;
    private bool _rovDepthKpDccCalculated;
    
    // Final
    private bool _finalListingProduced;
    
    // File names for tracking
    private string _productLayRouteFile = string.Empty;
    private string _tidalDataFile = string.Empty;
    private string _rawDataStartFile = string.Empty;
    private string _rawDataEndFile = string.Empty;
    private string _cadBackgroundFile = string.Empty;
    
    // Timestamps
    private DateTime? _processingStartTime;
    private DateTime? _processingEndTime;
    
    #region Properties
    
    public bool ProductLayRouteLoaded
    {
        get => _productLayRouteLoaded;
        set { _productLayRouteLoaded = value; OnPropertyChanged(); }
    }
    
    public bool TidalDataLoaded
    {
        get => _tidalDataLoaded;
        set { _tidalDataLoaded = value; OnPropertyChanged(); }
    }
    
    public bool RawDataFilesLoaded
    {
        get => _rawDataFilesLoaded;
        set { _rawDataFilesLoaded = value; OnPropertyChanged(); }
    }
    
    public bool CadBackgroundLoaded
    {
        get => _cadBackgroundLoaded;
        set { _cadBackgroundLoaded = value; OnPropertyChanged(); }
    }
    
    public bool RawDataAdded
    {
        get => _rawDataAdded;
        set { _rawDataAdded = value; OnPropertyChanged(); }
    }
    
    public bool TideDataAdded
    {
        get => _tideDataAdded;
        set { _tideDataAdded = value; OnPropertyChanged(); }
    }
    
    public bool CalculationsUpdated
    {
        get => _calculationsUpdated;
        set { _calculationsUpdated = value; OnPropertyChanged(); }
    }
    
    public bool ParametersUpdated
    {
        get => _parametersUpdated;
        set { _parametersUpdated = value; OnPropertyChanged(); }
    }
    
    public bool WorkingTrackCreated
    {
        get => _workingTrackCreated;
        set { _workingTrackCreated = value; OnPropertyChanged(); }
    }
    
    public bool FixesAddedToCad
    {
        get => _fixesAddedToCad;
        set { _fixesAddedToCad = value; OnPropertyChanged(); }
    }
    
    public bool XyzScriptCreated
    {
        get => _xyzScriptCreated;
        set { _xyzScriptCreated = value; OnPropertyChanged(); }
    }
    
    public bool TrackAddedToBricsCAD
    {
        get => _trackAddedToBricsCAD;
        set { _trackAddedToBricsCAD = value; OnPropertyChanged(); }
    }
    
    public bool DataCoverageReviewed
    {
        get => _dataCoverageReviewed;
        set { _dataCoverageReviewed = value; OnPropertyChanged(); }
    }
    
    public bool SplineFitCreated
    {
        get => _splineFitCreated;
        set { _splineFitCreated = value; OnPropertyChanged(); }
    }
    
    public bool ProductPositionReviewed
    {
        get => _productPositionReviewed;
        set { _productPositionReviewed = value; OnPropertyChanged(); }
    }
    
    public bool PointsAt1mCreated
    {
        get => _pointsAt1mCreated;
        set { _pointsAt1mCreated = value; OnPropertyChanged(); }
    }
    
    public bool PointsXyzExtracted
    {
        get => _pointsXyzExtracted;
        set { _pointsXyzExtracted = value; OnPropertyChanged(); }
    }
    
    public bool SeabedDepthKpDccCalculated
    {
        get => _seabedDepthKpDccCalculated;
        set { _seabedDepthKpDccCalculated = value; OnPropertyChanged(); }
    }
    
    public bool RovDepthKpDccCalculated
    {
        get => _rovDepthKpDccCalculated;
        set { _rovDepthKpDccCalculated = value; OnPropertyChanged(); }
    }
    
    public bool FinalListingProduced
    {
        get => _finalListingProduced;
        set { _finalListingProduced = value; OnPropertyChanged(); }
    }
    
    public string ProductLayRouteFile
    {
        get => _productLayRouteFile;
        set { _productLayRouteFile = value; OnPropertyChanged(); }
    }
    
    public string TidalDataFile
    {
        get => _tidalDataFile;
        set { _tidalDataFile = value; OnPropertyChanged(); }
    }
    
    public string RawDataStartFile
    {
        get => _rawDataStartFile;
        set { _rawDataStartFile = value; OnPropertyChanged(); }
    }
    
    public string RawDataEndFile
    {
        get => _rawDataEndFile;
        set { _rawDataEndFile = value; OnPropertyChanged(); }
    }
    
    public string CadBackgroundFile
    {
        get => _cadBackgroundFile;
        set { _cadBackgroundFile = value; OnPropertyChanged(); }
    }
    
    public DateTime? ProcessingStartTime
    {
        get => _processingStartTime;
        set { _processingStartTime = value; OnPropertyChanged(); }
    }
    
    public DateTime? ProcessingEndTime
    {
        get => _processingEndTime;
        set { _processingEndTime = value; OnPropertyChanged(); }
    }
    
    #endregion
    
    #region Methods
    
    public void Reset()
    {
        ProductLayRouteLoaded = false;
        TidalDataLoaded = false;
        RawDataFilesLoaded = false;
        CadBackgroundLoaded = false;
        RawDataAdded = false;
        TideDataAdded = false;
        CalculationsUpdated = false;
        ParametersUpdated = false;
        WorkingTrackCreated = false;
        FixesAddedToCad = false;
        XyzScriptCreated = false;
        TrackAddedToBricsCAD = false;
        DataCoverageReviewed = false;
        SplineFitCreated = false;
        ProductPositionReviewed = false;
        PointsAt1mCreated = false;
        PointsXyzExtracted = false;
        SeabedDepthKpDccCalculated = false;
        RovDepthKpDccCalculated = false;
        FinalListingProduced = false;
        
        ProductLayRouteFile = string.Empty;
        TidalDataFile = string.Empty;
        RawDataStartFile = string.Empty;
        RawDataEndFile = string.Empty;
        CadBackgroundFile = string.Empty;
        
        ProcessingStartTime = null;
        ProcessingEndTime = null;
    }
    
    public void StartProcessing()
    {
        ProcessingStartTime = DateTime.Now;
    }
    
    public void EndProcessing()
    {
        ProcessingEndTime = DateTime.Now;
    }
    
    /// <summary>
    /// Mark route file as loaded
    /// </summary>
    public void OnRouteFileLoaded(string filePath)
    {
        ProductLayRouteLoaded = true;
        ProductLayRouteFile = System.IO.Path.GetFileName(filePath);
    }
    
    /// <summary>
    /// Mark tide file as loaded
    /// </summary>
    public void OnTideFileLoaded(string filePath)
    {
        TidalDataLoaded = true;
        TidalDataFile = System.IO.Path.GetFileName(filePath);
    }
    
    /// <summary>
    /// Mark survey data files as loaded
    /// </summary>
    public void OnSurveyDataLoaded(IEnumerable<string> filePaths)
    {
        var fileList = new List<string>(filePaths);
        RawDataFilesLoaded = fileList.Count > 0;
        
        if (fileList.Count >= 1)
            RawDataStartFile = System.IO.Path.GetFileName(fileList[0]);
        if (fileList.Count >= 2)
            RawDataEndFile = System.IO.Path.GetFileName(fileList[fileList.Count - 1]);
    }
    
    /// <summary>
    /// Mark field layout DXF as loaded
    /// </summary>
    public void OnFieldLayoutLoaded(string filePath)
    {
        CadBackgroundLoaded = true;
        CadBackgroundFile = System.IO.Path.GetFileName(filePath);
    }
    
    /// <summary>
    /// Mark data processing as complete
    /// </summary>
    public void OnDataProcessed(bool hasTide, bool hasKpDcc, bool hasSmoothing)
    {
        RawDataAdded = true;
        CalculationsUpdated = true;
        ParametersUpdated = true;
        
        if (hasTide)
        {
            TideDataAdded = true;
        }
        
        if (hasKpDcc)
        {
            SeabedDepthKpDccCalculated = true;
            PointsXyzExtracted = true;
        }
        
        if (hasSmoothing)
        {
            SplineFitCreated = true;
            ProductPositionReviewed = true;
        }
    }
    
    /// <summary>
    /// Mark export as complete
    /// </summary>
    public void OnExportComplete(bool hasXyzScript, bool hasDxf, bool hasPdf)
    {
        if (hasXyzScript)
        {
            XyzScriptCreated = true;
            TrackAddedToBricsCAD = true;
        }
        
        if (hasDxf)
        {
            WorkingTrackCreated = true;
            FixesAddedToCad = true;
        }
        
        FinalListingProduced = true;
        EndProcessing();
    }
    
    /// <summary>
    /// Get summary of completed steps for display
    /// </summary>
    public string GetCompletionSummary()
    {
        int total = 20;
        int completed = 0;
        
        if (ProductLayRouteLoaded) completed++;
        if (TidalDataLoaded) completed++;
        if (RawDataFilesLoaded) completed++;
        if (CadBackgroundLoaded) completed++;
        if (RawDataAdded) completed++;
        if (TideDataAdded) completed++;
        if (CalculationsUpdated) completed++;
        if (ParametersUpdated) completed++;
        if (WorkingTrackCreated) completed++;
        if (FixesAddedToCad) completed++;
        if (XyzScriptCreated) completed++;
        if (TrackAddedToBricsCAD) completed++;
        if (DataCoverageReviewed) completed++;
        if (SplineFitCreated) completed++;
        if (ProductPositionReviewed) completed++;
        if (PointsAt1mCreated) completed++;
        if (PointsXyzExtracted) completed++;
        if (SeabedDepthKpDccCalculated) completed++;
        if (RovDepthKpDccCalculated) completed++;
        if (FinalListingProduced) completed++;
        
        return $"{completed}/{total} steps completed";
    }
    
    #endregion
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
