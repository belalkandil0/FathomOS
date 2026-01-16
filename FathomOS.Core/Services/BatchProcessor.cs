namespace FathomOS.Core.Services;

using FathomOS.Core.Models;
using FathomOS.Core.Parsers;

/// <summary>
/// Service for batch processing multiple NPD survey files
/// </summary>
public class BatchProcessor
{
    private readonly NpdParser _npdParser;

    public BatchProcessor()
    {
        _npdParser = new NpdParser();
    }

    /// <summary>
    /// Event raised when a file starts processing
    /// </summary>
    public event EventHandler<BatchFileEventArgs>? FileProcessingStarted;

    /// <summary>
    /// Event raised when a file finishes processing
    /// </summary>
    public event EventHandler<BatchFileEventArgs>? FileProcessingCompleted;

    /// <summary>
    /// Event raised when a file fails to process
    /// </summary>
    public event EventHandler<BatchFileErrorEventArgs>? FileProcessingFailed;

    /// <summary>
    /// Event raised to report overall progress
    /// </summary>
    public event EventHandler<BatchProgressEventArgs>? ProgressChanged;

    /// <summary>
    /// Process multiple NPD files and combine results
    /// </summary>
    /// <param name="filePaths">List of NPD file paths to process</param>
    /// <param name="mapping">Column mapping configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Combined batch result</returns>
    public async Task<BatchProcessResult> ProcessFilesAsync(
        IList<string> filePaths, 
        ColumnMapping mapping,
        CancellationToken cancellationToken = default)
    {
        var result = new BatchProcessResult
        {
            StartTime = DateTime.Now
        };

        int totalFiles = filePaths.Count;
        int completedFiles = 0;

        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileResult = new BatchFileResult
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            try
            {
                // Raise started event
                FileProcessingStarted?.Invoke(this, new BatchFileEventArgs(filePath, completedFiles + 1, totalFiles));

                // Process file
                var parseResult = await Task.Run(() => _npdParser.Parse(filePath, mapping), cancellationToken);

                fileResult.Success = true;
                fileResult.RecordCount = parseResult.TotalRecords;
                fileResult.Warnings = _npdParser.ParseWarnings.ToList();
                fileResult.StartTime = parseResult.StartTime;
                fileResult.EndTime = parseResult.EndTime;

                // Add points to combined result
                result.AllPoints.AddRange(parseResult.Points);
                result.FileResults.Add(fileResult);

                // Raise completed event
                FileProcessingCompleted?.Invoke(this, new BatchFileEventArgs(filePath, completedFiles + 1, totalFiles));
            }
            catch (Exception ex)
            {
                fileResult.Success = false;
                fileResult.ErrorMessage = ex.Message;
                result.FileResults.Add(fileResult);

                // Raise error event
                FileProcessingFailed?.Invoke(this, new BatchFileErrorEventArgs(filePath, ex));
            }

            completedFiles++;

            // Report progress
            double progressPercent = (double)completedFiles / totalFiles * 100;
            ProgressChanged?.Invoke(this, new BatchProgressEventArgs(completedFiles, totalFiles, progressPercent));
        }

        result.EndTime = DateTime.Now;
        result.CalculateStatistics();

        return result;
    }

    /// <summary>
    /// Process files synchronously
    /// </summary>
    public BatchProcessResult ProcessFiles(IList<string> filePaths, ColumnMapping mapping)
    {
        return ProcessFilesAsync(filePaths, mapping).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Validate a list of files before processing
    /// </summary>
    public List<FileValidationResult> ValidateFiles(IList<string> filePaths)
    {
        var results = new List<FileValidationResult>();

        foreach (var filePath in filePaths)
        {
            var validation = new FileValidationResult
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath)
            };

            if (!File.Exists(filePath))
            {
                validation.IsValid = false;
                validation.Issues.Add("File not found");
            }
            else
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    validation.FileSize = fileInfo.Length;
                    validation.ModifiedDate = fileInfo.LastWriteTime;

                    // Try to read headers
                    var columns = _npdParser.GetAllColumns(filePath);
                    validation.ColumnCount = columns.Count;
                    validation.IsValid = columns.Count > 0;

                    if (columns.Count == 0)
                    {
                        validation.Issues.Add("No columns found in file");
                    }
                }
                catch (Exception ex)
                {
                    validation.IsValid = false;
                    validation.Issues.Add($"Error reading file: {ex.Message}");
                }
            }

            results.Add(validation);
        }

        return results;
    }
}

/// <summary>
/// Result of batch processing multiple files
/// </summary>
public class BatchProcessResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    public List<BatchFileResult> FileResults { get; set; } = new();
    public List<SurveyPoint> AllPoints { get; set; } = new();

    public int TotalFiles => FileResults.Count;
    public int SuccessfulFiles => FileResults.Count(f => f.Success);
    public int FailedFiles => FileResults.Count(f => !f.Success);
    public int TotalRecords => AllPoints.Count;

    public DateTime? EarliestTime { get; private set; }
    public DateTime? LatestTime { get; private set; }
    public double? MinDepth { get; private set; }
    public double? MaxDepth { get; private set; }

    public void CalculateStatistics()
    {
        if (AllPoints.Count == 0)
            return;

        var validTimes = AllPoints.Where(p => p.DateTime != DateTime.MinValue).ToList();
        if (validTimes.Any())
        {
            EarliestTime = validTimes.Min(p => p.DateTime);
            LatestTime = validTimes.Max(p => p.DateTime);
        }

        var validDepths = AllPoints.Where(p => p.HasValidDepth).ToList();
        if (validDepths.Any())
        {
            MinDepth = validDepths.Min(p => p.Depth!.Value);
            MaxDepth = validDepths.Max(p => p.Depth!.Value);
        }

        // Renumber all points sequentially
        int recordNumber = 1;
        foreach (var point in AllPoints.OrderBy(p => p.DateTime))
        {
            point.RecordNumber = recordNumber++;
        }
    }
}

/// <summary>
/// Result for a single file in batch processing
/// </summary>
public class BatchFileResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int RecordCount { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

/// <summary>
/// File validation result
/// </summary>
public class FileValidationResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public long FileSize { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int ColumnCount { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Event args for batch file processing
/// </summary>
public class BatchFileEventArgs : EventArgs
{
    public string FilePath { get; }
    public int CurrentFile { get; }
    public int TotalFiles { get; }

    public BatchFileEventArgs(string filePath, int currentFile, int totalFiles)
    {
        FilePath = filePath;
        CurrentFile = currentFile;
        TotalFiles = totalFiles;
    }
}

/// <summary>
/// Event args for batch file error
/// </summary>
public class BatchFileErrorEventArgs : EventArgs
{
    public string FilePath { get; }
    public Exception Exception { get; }

    public BatchFileErrorEventArgs(string filePath, Exception exception)
    {
        FilePath = filePath;
        Exception = exception;
    }
}

/// <summary>
/// Event args for batch progress
/// </summary>
public class BatchProgressEventArgs : EventArgs
{
    public int CompletedFiles { get; }
    public int TotalFiles { get; }
    public double ProgressPercent { get; }

    public BatchProgressEventArgs(int completedFiles, int totalFiles, double progressPercent)
    {
        CompletedFiles = completedFiles;
        TotalFiles = totalFiles;
        ProgressPercent = progressPercent;
    }
}
