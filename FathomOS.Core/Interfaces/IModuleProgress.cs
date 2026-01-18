namespace FathomOS.Core.Interfaces;

/// <summary>
/// Represents the status of a long-running operation.
/// </summary>
public enum OperationStatus
{
    /// <summary>Operation has not started.</summary>
    NotStarted,
    /// <summary>Operation is currently running.</summary>
    Running,
    /// <summary>Operation completed successfully.</summary>
    Completed,
    /// <summary>Operation was cancelled.</summary>
    Cancelled,
    /// <summary>Operation failed with an error.</summary>
    Failed
}

/// <summary>
/// Progress information for long-running module operations.
/// </summary>
public class ModuleProgressInfo
{
    /// <summary>
    /// Gets or sets the operation name/description.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current step description.
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// Use -1 for indeterminate progress.
    /// </summary>
    public double ProgressPercent { get; set; } = 0;

    /// <summary>
    /// Gets or sets the current item being processed.
    /// </summary>
    public int CurrentItem { get; set; } = 0;

    /// <summary>
    /// Gets or sets the total items to process.
    /// </summary>
    public int TotalItems { get; set; } = 0;

    /// <summary>
    /// Gets or sets the current operation status.
    /// </summary>
    public OperationStatus Status { get; set; } = OperationStatus.NotStarted;

    /// <summary>
    /// Gets or sets the error message if status is Failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets whether the operation can be cancelled.
    /// </summary>
    public bool CanCancel { get; set; } = true;

    /// <summary>
    /// Gets or sets any additional data associated with the progress.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time since operation started.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Creates a simple progress update.
    /// </summary>
    public static ModuleProgressInfo Simple(string step, double percent)
    {
        return new ModuleProgressInfo
        {
            CurrentStep = step,
            ProgressPercent = percent,
            Status = percent >= 100 ? OperationStatus.Completed : OperationStatus.Running
        };
    }

    /// <summary>
    /// Creates an item-based progress update.
    /// </summary>
    public static ModuleProgressInfo Items(string step, int current, int total)
    {
        var percent = total > 0 ? (current * 100.0 / total) : 0;
        return new ModuleProgressInfo
        {
            CurrentStep = step,
            CurrentItem = current,
            TotalItems = total,
            ProgressPercent = percent,
            Status = current >= total ? OperationStatus.Completed : OperationStatus.Running
        };
    }

    /// <summary>
    /// Creates a completed progress update.
    /// </summary>
    public static ModuleProgressInfo Completed(string message = "Operation completed")
    {
        return new ModuleProgressInfo
        {
            CurrentStep = message,
            ProgressPercent = 100,
            Status = OperationStatus.Completed
        };
    }

    /// <summary>
    /// Creates a failed progress update.
    /// </summary>
    public static ModuleProgressInfo Failed(string errorMessage)
    {
        return new ModuleProgressInfo
        {
            CurrentStep = "Operation failed",
            ErrorMessage = errorMessage,
            Status = OperationStatus.Failed
        };
    }

    /// <summary>
    /// Creates an indeterminate progress update (spinner).
    /// </summary>
    public static ModuleProgressInfo Indeterminate(string step)
    {
        return new ModuleProgressInfo
        {
            CurrentStep = step,
            ProgressPercent = -1,
            Status = OperationStatus.Running
        };
    }
}

/// <summary>
/// Interface for reporting progress from long-running module operations.
/// Extends IProgress&lt;T&gt; with additional module-specific functionality.
/// </summary>
public interface IModuleProgress : IProgress<ModuleProgressInfo>
{
    /// <summary>
    /// Gets the current progress information.
    /// </summary>
    ModuleProgressInfo CurrentProgress { get; }

    /// <summary>
    /// Gets the cancellation token for the current operation.
    /// </summary>
    CancellationToken CancellationToken { get; }

    /// <summary>
    /// Requests cancellation of the current operation.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Reports a simple progress update.
    /// </summary>
    /// <param name="step">Current step description.</param>
    /// <param name="percent">Progress percentage (0-100).</param>
    void ReportProgress(string step, double percent);

    /// <summary>
    /// Reports item-based progress.
    /// </summary>
    /// <param name="step">Current step description.</param>
    /// <param name="current">Current item number.</param>
    /// <param name="total">Total items.</param>
    void ReportProgress(string step, int current, int total);

    /// <summary>
    /// Reports indeterminate progress (spinner).
    /// </summary>
    /// <param name="step">Current step description.</param>
    void ReportIndeterminate(string step);

    /// <summary>
    /// Reports completion.
    /// </summary>
    /// <param name="message">Completion message.</param>
    void ReportCompleted(string message = "Operation completed");

    /// <summary>
    /// Reports failure.
    /// </summary>
    /// <param name="error">Error message.</param>
    void ReportFailed(string error);

    /// <summary>
    /// Event raised when progress changes.
    /// </summary>
    event EventHandler<ModuleProgressInfo>? ProgressChanged;
}
