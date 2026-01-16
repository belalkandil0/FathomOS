using FathomOS.Core.Calculations;
using FathomOS.Core.Export;
using FathomOS.Core.Models;
using FathomOS.Core.Parsers;

namespace FathomOS.Core.Services;

/// <summary>
/// Main processing service that orchestrates all survey processing steps
/// </summary>
public class SurveyProcessor
{
    private readonly Project _project;
    private RouteData? _routeData;
    private TideData? _tideData;
    private List<SurveyPoint> _surveyPoints = new();
    private List<SurveyPoint> _processedPoints = new();

    public event EventHandler<ProcessingEventArgs>? ProgressChanged;
    public event EventHandler<ProcessingEventArgs>? StepStarted;
    public event EventHandler<ProcessingEventArgs>? StepCompleted;
    public event EventHandler<ProcessingErrorEventArgs>? ErrorOccurred;

    public SurveyProcessor(Project project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    /// <summary>
    /// Get the processed survey points
    /// </summary>
    public IReadOnlyList<SurveyPoint> ProcessedPoints => _processedPoints;

    /// <summary>
    /// Get the loaded route data
    /// </summary>
    public RouteData? RouteData => _routeData;

    /// <summary>
    /// Get the loaded tide data
    /// </summary>
    public TideData? TideData => _tideData;

    /// <summary>
    /// Run complete processing pipeline
    /// </summary>
    public async Task<ProcessingResult> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var result = new ProcessingResult();
        var startTime = DateTime.Now;

        try
        {
            // Step 1: Load route file
            await LoadRouteAsync(cancellationToken);
            result.Steps.Add("Route loaded successfully");

            // Step 2: Load survey data
            await LoadSurveyDataAsync(cancellationToken);
            result.Steps.Add($"Loaded {_surveyPoints.Count} survey points");

            // Step 3: Load tide data (optional)
            if (!string.IsNullOrEmpty(_project.TideFilePath))
            {
                await LoadTideDataAsync(cancellationToken);
                result.Steps.Add("Tide data loaded");
            }

            // Step 4: Calculate KP/DCC
            if (_project.ProcessingOptions.CalculateKpDcc && _routeData != null)
            {
                await CalculateKpDccAsync(cancellationToken);
                result.Steps.Add("KP/DCC calculated");
            }

            // Step 5: Apply tidal corrections
            if (_project.ProcessingOptions.ApplyTidalCorrections && _tideData != null)
            {
                await ApplyTidalCorrectionsAsync(cancellationToken);
                result.Steps.Add("Tidal corrections applied");
            }

            // Step 6: Apply vertical offsets
            if (_project.ProcessingOptions.ApplyVerticalOffsets)
            {
                await ApplyVerticalOffsetsAsync(cancellationToken);
                result.Steps.Add("Vertical offsets applied");
            }

            // Copy to processed points
            _processedPoints = new List<SurveyPoint>(_surveyPoints);

            result.IsSuccess = true;
            result.TotalPoints = _processedPoints.Count;
        }
        catch (OperationCanceledException)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "Processing was cancelled";
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            OnErrorOccurred(ex.Message);
        }

        result.ProcessingTime = DateTime.Now - startTime;
        return result;
    }

    /// <summary>
    /// Export processed data to all selected formats
    /// </summary>
    public async Task<ExportResult> ExportAsync(CancellationToken cancellationToken = default)
    {
        var result = new ExportResult();

        if (_processedPoints.Count == 0)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "No processed data to export";
            return result;
        }

        var outputFolder = _project.OutputOptions.OutputFolder;
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string baseName = SanitizeFileName(_project.ProjectName);

        try
        {
            // Export Text File
            if (_project.OutputOptions.ExportTextFile)
            {
                OnStepStarted("Exporting text file...");
                var format = TextExporter.ParseFormat(_project.OutputOptions.TextFormat);
                string ext = format == TextExporter.TextFormat.Csv ? ".csv" : ".txt";
                string path = Path.Combine(outputFolder, baseName + ext);

                var exporter = new TextExporter(format, _project.OutputOptions.TextIncludeHeader);
                await Task.Run(() => exporter.Export(path, _processedPoints, _project), cancellationToken);

                result.GeneratedFiles.Add(path);
                OnStepCompleted("Text file exported");
            }

            // Export Excel
            if (_project.OutputOptions.ExportExcel)
            {
                OnStepStarted("Exporting Excel workbook...");
                string path = Path.Combine(outputFolder, baseName + ".xlsx");

                var options = new ExcelExportOptions
                {
                    IncludeRawData = _project.OutputOptions.ExcelIncludeRawData,
                    IncludeCalculations = _project.OutputOptions.ExcelIncludeCalculations,
                    ApplyFormatting = _project.OutputOptions.ExcelApplyFormatting
                };
                var exporter = new ExcelExporter(options);
                await Task.Run(() => exporter.Export(path, _processedPoints, _project), cancellationToken);

                result.GeneratedFiles.Add(path);
                OnStepCompleted("Excel workbook exported");
            }

            // Export DXF
            if (_project.OutputOptions.ExportDxf)
            {
                OnStepStarted("Exporting DXF drawing...");
                string path = Path.Combine(outputFolder, baseName + ".dxf");

                var options = new DxfExportOptions
                {
                    KpLabelInterval = _project.OutputOptions.KpLabelInterval,
                    DepthExaggeration = _project.ProcessingOptions.DepthExaggeration
                };
                var exporter = new DxfExporter(options);
                await Task.Run(() => exporter.Export(path, _processedPoints, _routeData, _project), cancellationToken);

                result.GeneratedFiles.Add(path);
                OnStepCompleted("DXF drawing exported");
            }

            // Export CAD Script
            if (_project.OutputOptions.ExportCadScript)
            {
                OnStepStarted("Exporting CAD script...");
                string path = Path.Combine(outputFolder, baseName + ".scr");

                var options = new CadScriptOptions
                {
                    TemplatePath = _project.OutputOptions.DwgTemplatePath,
                    KpLabelInterval = _project.OutputOptions.KpLabelInterval
                };
                var exporter = new CadScriptExporter(options);
                await Task.Run(() => exporter.Export(path, _processedPoints, _routeData, _project), cancellationToken);

                result.GeneratedFiles.Add(path);
                OnStepCompleted("CAD script exported");
            }

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            OnErrorOccurred(ex.Message);
        }

        return result;
    }

    private async Task LoadRouteAsync(CancellationToken cancellationToken)
    {
        OnStepStarted("Loading route file...");
        
        var parser = new RlxParser();
        _routeData = await Task.Run(() => parser.Parse(_project.RouteFilePath), cancellationToken);
        
        OnStepCompleted($"Route loaded: {_routeData.Name}");
    }

    private async Task LoadSurveyDataAsync(CancellationToken cancellationToken)
    {
        OnStepStarted("Loading survey data...");

        var batchProcessor = new BatchProcessor();
        batchProcessor.ProgressChanged += (s, e) => 
            OnProgressChanged($"Processing file {e.CompletedFiles} of {e.TotalFiles}", 
                (int)(100.0 * e.CompletedFiles / e.TotalFiles));

        var result = await batchProcessor.ProcessFilesAsync(
            _project.SurveyDataFiles, 
            _project.ColumnMapping, 
            cancellationToken);

        _surveyPoints = result.AllPoints;

        OnStepCompleted($"Loaded {_surveyPoints.Count} survey points from {result.SuccessfulFiles} files");
    }

    private async Task LoadTideDataAsync(CancellationToken cancellationToken)
    {
        OnStepStarted("Loading tide data...");

        var parser = new TideParser();
        _tideData = await Task.Run(() => parser.Parse(_project.TideFilePath), cancellationToken);

        OnStepCompleted($"Tide data loaded: {_tideData.RecordCount} records");
    }

    private async Task CalculateKpDccAsync(CancellationToken cancellationToken)
    {
        OnStepStarted("Calculating KP/DCC...");

        var calculator = new KpCalculator(_routeData!, _project.KpUnit);
        
        var progress = new Progress<int>(percent => OnProgressChanged("Calculating KP/DCC", percent));
        
        await Task.Run(() => calculator.CalculateAll(_surveyPoints, progress), cancellationToken);

        OnStepCompleted("KP/DCC calculation complete");
    }

    private async Task ApplyTidalCorrectionsAsync(CancellationToken cancellationToken)
    {
        OnStepStarted("Applying tidal corrections...");

        var corrector = new TideCorrector(_tideData!, _project.UseFeetForTide);
        
        var progress = new Progress<int>(percent => OnProgressChanged("Applying tide", percent));
        
        int corrected = await Task.Run(() => corrector.ApplyToAll(_surveyPoints, progress), cancellationToken);

        OnStepCompleted($"Tidal corrections applied to {corrected} points");
    }

    private async Task ApplyVerticalOffsetsAsync(CancellationToken cancellationToken)
    {
        OnStepStarted("Applying vertical offsets...");

        var calculator = new DepthCalculator(_project.ProcessingOptions);
        
        var progress = new Progress<int>(percent => OnProgressChanged("Applying offsets", percent));
        
        await Task.Run(() => calculator.ProcessAll(_surveyPoints, _project.SurveyType, progress), cancellationToken);

        OnStepCompleted("Vertical offsets applied");
    }

    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "survey_output";

        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalid.Contains(c)).ToArray())
            .Replace(' ', '_');
    }

    private void OnProgressChanged(string message, int percent)
    {
        ProgressChanged?.Invoke(this, new ProcessingEventArgs(message, percent));
    }

    private void OnStepStarted(string message)
    {
        StepStarted?.Invoke(this, new ProcessingEventArgs(message, 0));
    }

    private void OnStepCompleted(string message)
    {
        StepCompleted?.Invoke(this, new ProcessingEventArgs(message, 100));
    }

    private void OnErrorOccurred(string message)
    {
        ErrorOccurred?.Invoke(this, new ProcessingErrorEventArgs(message));
    }
}

/// <summary>
/// Event args for processing progress
/// </summary>
public class ProcessingEventArgs : EventArgs
{
    public string Message { get; }
    public int ProgressPercent { get; }

    public ProcessingEventArgs(string message, int progressPercent)
    {
        Message = message;
        ProgressPercent = progressPercent;
    }
}

/// <summary>
/// Event args for processing errors
/// </summary>
public class ProcessingErrorEventArgs : EventArgs
{
    public string ErrorMessage { get; }

    public ProcessingErrorEventArgs(string errorMessage)
    {
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Result of processing operation
/// </summary>
public class ProcessingResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public int TotalPoints { get; set; }
    public List<string> Steps { get; } = new();
}

/// <summary>
/// Result of export operation
/// </summary>
public class ExportResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> GeneratedFiles { get; } = new();
}
