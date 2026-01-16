using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.GnssCalibration.Models;

/// <summary>
/// Represents a file item in the batch processing queue.
/// </summary>
public class BatchFileItem : INotifyPropertyChanged
{
    private string _filePath = "";
    private string _fileName = "";
    private BatchItemStatus _status = BatchItemStatus.Pending;
    private string _statusMessage = "Pending";
    private double? _twoDrms;
    private int? _totalPoints;
    private int? _acceptedPoints;
    private bool _tolerancePassed;
    private double _processingTimeMs;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged([CallerMemberName] string? name = null) 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
    /// <summary>Full path to the NPD file.</summary>
    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
            _fileName = System.IO.Path.GetFileName(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(FileName));
        }
    }
    
    /// <summary>File name only (for display).</summary>
    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }
    
    /// <summary>Processing status.</summary>
    public BatchItemStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsProcessed));
        }
    }
    
    /// <summary>Status message for display.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }
    
    /// <summary>2DRMS result (null if not processed).</summary>
    public double? TwoDrms
    {
        get => _twoDrms;
        set
        {
            _twoDrms = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TwoDrmsDisplay));
        }
    }
    
    /// <summary>Formatted 2DRMS for display.</summary>
    public string TwoDrmsDisplay => TwoDrms.HasValue ? $"{TwoDrms:F4}" : "-";
    
    /// <summary>Total points processed.</summary>
    public int? TotalPoints
    {
        get => _totalPoints;
        set { _totalPoints = value; OnPropertyChanged(); }
    }
    
    /// <summary>Points accepted after filtering.</summary>
    public int? AcceptedPoints
    {
        get => _acceptedPoints;
        set { _acceptedPoints = value; OnPropertyChanged(); }
    }
    
    /// <summary>Whether tolerance check passed.</summary>
    public bool TolerancePassed
    {
        get => _tolerancePassed;
        set
        {
            _tolerancePassed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToleranceStatus));
        }
    }
    
    /// <summary>Tolerance status text.</summary>
    public string ToleranceStatus => Status == BatchItemStatus.Completed 
        ? (TolerancePassed ? "PASS" : "FAIL") 
        : "-";
    
    /// <summary>Processing time in milliseconds.</summary>
    public double ProcessingTimeMs
    {
        get => _processingTimeMs;
        set { _processingTimeMs = value; OnPropertyChanged(); }
    }
    
    /// <summary>Status color for UI.</summary>
    public string StatusColor => Status switch
    {
        BatchItemStatus.Pending => "#9E9E9E",
        BatchItemStatus.Processing => "#2196F3",
        BatchItemStatus.Completed => TolerancePassed ? "#4CAF50" : "#FF9800",
        BatchItemStatus.Failed => "#F44336",
        _ => "#9E9E9E"
    };
    
    /// <summary>Whether this item has been processed.</summary>
    public bool IsProcessed => Status == BatchItemStatus.Completed || Status == BatchItemStatus.Failed;
}

/// <summary>
/// Status of a batch item.
/// </summary>
public enum BatchItemStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Result of batch processing a single file.
/// </summary>
public class BatchProcessResult
{
    public string FilePath { get; set; } = "";
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = "";
    public double TwoDrms { get; set; }
    public int TotalPoints { get; set; }
    public int AcceptedPoints { get; set; }
    public bool TolerancePassed { get; set; }
    public double ProcessingTimeMs { get; set; }
    public GnssStatisticsResult? Statistics { get; set; }
}
