using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using FathomOS.Core.Models;
using FathomOS.Core.Parsers;
using FathomOS.Core.Services;
using FathomOS.Modules.SurveyListing.Services;
using MessageBox = System.Windows.MessageBox;

namespace FathomOS.Modules.SurveyListing.ViewModels;

/// <summary>
/// ViewModel for Step 4: Data Review
/// </summary>
public class Step4ViewModel : INotifyPropertyChanged
{
    private readonly BatchProcessor _batchProcessor;
    private bool _isLoading;
    private bool _isDataLoaded;
    private bool _isDataConfirmed;
    private string _statusMessage = "Click 'Load Data' to extract records";
    private int _totalRecords;
    private DateTime? _startTime;
    private DateTime? _endTime;
    private double? _minDepth;
    private double? _maxDepth;
    private int _recordsWithWarnings;

    // Reference to Step 3 for getting files and mapping
    private Step3ViewModel? _step3ViewModel;
    private Project? _project;
    
    // Store actual loaded points for use by Step 6
    private List<SurveyPoint> _loadedPoints = new();

    public Step4ViewModel(Project project)
    {
        _project = project;
        _batchProcessor = new BatchProcessor();
        SurveyPoints = new ObservableCollection<SurveyPointDisplay>();
        Warnings = new ObservableCollection<string>();

        // Subscribe to batch processor events
        _batchProcessor.ProgressChanged += (s, e) =>
        {
            StatusMessage = $"Processing file {e.CompletedFiles} of {e.TotalFiles}...";
        };
    }

    public ObservableCollection<SurveyPointDisplay> SurveyPoints { get; }
    public ObservableCollection<string> Warnings { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanLoad)); }
    }

    public bool IsDataLoaded
    {
        get => _isDataLoaded;
        private set { _isDataLoaded = value; OnPropertyChanged(); }
    }

    public bool IsDataConfirmed
    {
        get => _isDataConfirmed;
        set { _isDataConfirmed = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool CanLoad => !IsLoading;

    // Statistics
    public int TotalRecords
    {
        get => _totalRecords;
        private set { _totalRecords = value; OnPropertyChanged(); }
    }

    public string TimeRange => _startTime.HasValue && _endTime.HasValue
        ? $"{_startTime:yyyy-MM-dd HH:mm:ss} to {_endTime:HH:mm:ss}"
        : "-";

    public string DepthRange => _minDepth.HasValue && _maxDepth.HasValue
        ? $"{_minDepth:F2} to {_maxDepth:F2}"
        : "-";

    public int RecordsWithWarnings
    {
        get => _recordsWithWarnings;
        private set { _recordsWithWarnings = value; OnPropertyChanged(); }
    }

    public void SetStep3Reference(Step3ViewModel step3)
    {
        _step3ViewModel = step3;
    }

    public async Task LoadDataAsync()
    {
        if (_step3ViewModel == null || _step3ViewModel.Files.Count == 0)
        {
            MessageBox.Show("No survey data files configured. Please go back to Step 3.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        IsDataLoaded = false;
        IsDataConfirmed = false;
        SurveyPoints.Clear();
        Warnings.Clear();

        try
        {
            var files = _step3ViewModel.Files.Select(f => f.FilePath).ToList();
            var mapping = _step3ViewModel.GetCurrentMapping();

            StatusMessage = "Loading survey data...";

            var result = await _batchProcessor.ProcessFilesAsync(files, mapping);

            // Store ALL points for Step 6 processing (not just display limit)
            _loadedPoints = result.AllPoints.OrderBy(p => p.DateTime).ToList();

            // Populate display collection
            int displayLimit = 10000; // Limit for UI performance
            int count = 0;
            foreach (var point in _loadedPoints)
            {
                if (count >= displayLimit) break;

                SurveyPoints.Add(new SurveyPointDisplay
                {
                    RecordNumber = point.RecordNumber,
                    DateTime = point.DateTime,
                    Easting = point.Easting,
                    Northing = point.Northing,
                    Depth = point.Depth,
                    Altitude = point.Altitude,
                    Heading = point.Heading
                });
                count++;
            }

            // Collect warnings
            foreach (var fileResult in result.FileResults)
            {
                foreach (var warning in fileResult.Warnings.Take(10))
                {
                    Warnings.Add($"[{fileResult.FileName}] {warning}");
                }
                if (fileResult.Warnings.Count > 10)
                {
                    Warnings.Add($"[{fileResult.FileName}] ... and {fileResult.Warnings.Count - 10} more warnings");
                }
            }

            // Update statistics
            TotalRecords = result.TotalRecords;
            _startTime = result.EarliestTime;
            _endTime = result.LatestTime;
            _minDepth = result.MinDepth;
            _maxDepth = result.MaxDepth;
            RecordsWithWarnings = Warnings.Count;

            OnPropertyChanged(nameof(TimeRange));
            OnPropertyChanged(nameof(DepthRange));

            IsDataLoaded = true;

            if (result.TotalRecords > displayLimit)
            {
                StatusMessage = $"Loaded {result.TotalRecords} records (showing first {displayLimit})";
            }
            else
            {
                StatusMessage = $"Loaded {result.TotalRecords} records successfully";
            }

            if (result.FailedFiles > 0)
            {
                MessageBox.Show(
                    $"{result.FailedFiles} file(s) failed to process. Check warnings for details.",
                    "Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            MessageBox.Show($"Error loading data:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ExportPreview()
    {
        if (SurveyPoints.Count == 0)
        {
            MessageBox.Show("No data to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv",
            Title = "Export Preview Data",
            FileName = "survey_preview.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new StreamWriter(dialog.FileName);
                writer.WriteLine("RecordNo,DateTime,Easting,Northing,Depth,Altitude,Heading");
                
                foreach (var point in SurveyPoints)
                {
                    writer.WriteLine($"{point.RecordNumber},{point.DateTime:yyyy-MM-dd HH:mm:ss}," +
                        $"{point.Easting:F4},{point.Northing:F4}," +
                        $"{point.Depth?.ToString("F4") ?? ""}," +
                        $"{point.Altitude?.ToString("F4") ?? ""}," +
                        $"{point.Heading?.ToString("F4") ?? ""}");
                }

                MessageBox.Show($"Exported {SurveyPoints.Count} records.", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public void LoadProject(Project project)
    {
        _project = project;
        // Data will be loaded when user clicks Load button
        IsDataLoaded = false;
        IsDataConfirmed = false;
        SurveyPoints.Clear();
        Warnings.Clear();
        StatusMessage = "Click 'Load Data' to extract records";
    }

    public void SaveToProject(Project project)
    {
        // Data confirmation state is transient, not saved to project
    }

    public bool Validate()
    {
        if (!IsDataLoaded)
        {
            MessageBox.Show(
                "Please load and review the survey data before continuing.",
                "Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        if (!IsDataConfirmed)
        {
            var result = MessageBox.Show(
                "You have not confirmed the data is correct.\n\nDo you want to continue anyway?",
                "Confirm Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        return true;
    }

    /// <summary>
    /// Get all loaded survey points for processing in Step 6
    /// </summary>
    public List<SurveyPoint> GetLoadedPoints()
    {
        return _loadedPoints;
    }

    /// <summary>
    /// Check if data is available for processing
    /// </summary>
    public bool HasLoadedData => _loadedPoints.Count > 0 && IsDataConfirmed;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Display model for survey points in the data grid
/// </summary>
public class SurveyPointDisplay
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
