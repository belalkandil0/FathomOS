using System.Diagnostics;
using FathomOS.Core.Interfaces;

namespace FathomOS.Core.Services;

/// <summary>
/// Standard implementation of IModuleProgress for reporting progress from module operations.
/// Thread-safe and supports cancellation.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// // In module code
/// using var progress = new ModuleProgressReporter("Processing files");
///
/// for (int i = 0; i &lt; files.Count; i++)
/// {
///     progress.ThrowIfCancellationRequested();
///     progress.ReportProgress("Processing file", i + 1, files.Count);
///     await ProcessFileAsync(files[i], progress.CancellationToken);
/// }
///
/// progress.ReportCompleted($"Processed {files.Count} files");
/// </code>
/// </remarks>
public class ModuleProgressReporter : IModuleProgress, IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly Stopwatch _stopwatch;
    private readonly object _lock = new();
    private ModuleProgressInfo _currentProgress;
    private bool _disposed;

    /// <summary>
    /// Creates a new progress reporter.
    /// </summary>
    /// <param name="operationName">Name of the operation being tracked.</param>
    public ModuleProgressReporter(string operationName = "")
    {
        _cts = new CancellationTokenSource();
        _stopwatch = new Stopwatch();
        _currentProgress = new ModuleProgressInfo
        {
            OperationName = operationName,
            Status = OperationStatus.NotStarted
        };
    }

    /// <summary>
    /// Gets the current progress information.
    /// </summary>
    public ModuleProgressInfo CurrentProgress
    {
        get
        {
            lock (_lock)
            {
                return _currentProgress;
            }
        }
    }

    /// <summary>
    /// Gets the cancellation token for the current operation.
    /// </summary>
    public CancellationToken CancellationToken => _cts.Token;

    /// <summary>
    /// Event raised when progress changes.
    /// </summary>
    public event EventHandler<ModuleProgressInfo>? ProgressChanged;

    /// <summary>
    /// Requests cancellation of the current operation.
    /// </summary>
    public void Cancel()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            UpdateProgress(p =>
            {
                p.Status = OperationStatus.Cancelled;
                p.CurrentStep = "Operation cancelled";
            });
        }
    }

    /// <summary>
    /// Throws if cancellation has been requested.
    /// </summary>
    public void ThrowIfCancellationRequested()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Reports progress using the IProgress&lt;T&gt; interface.
    /// </summary>
    public void Report(ModuleProgressInfo value)
    {
        EnsureStarted();
        UpdateProgress(p =>
        {
            p.CurrentStep = value.CurrentStep;
            p.ProgressPercent = value.ProgressPercent;
            p.CurrentItem = value.CurrentItem;
            p.TotalItems = value.TotalItems;
            p.Status = value.Status;
            p.ErrorMessage = value.ErrorMessage;
            p.Data = value.Data;
            p.EstimatedTimeRemaining = value.EstimatedTimeRemaining;
        });
    }

    /// <summary>
    /// Reports a simple progress update.
    /// </summary>
    public void ReportProgress(string step, double percent)
    {
        EnsureStarted();
        UpdateProgress(p =>
        {
            p.CurrentStep = step;
            p.ProgressPercent = Math.Min(100, Math.Max(0, percent));
            p.Status = percent >= 100 ? OperationStatus.Completed : OperationStatus.Running;
        });
    }

    /// <summary>
    /// Reports item-based progress.
    /// </summary>
    public void ReportProgress(string step, int current, int total)
    {
        EnsureStarted();
        var percent = total > 0 ? (current * 100.0 / total) : 0;

        UpdateProgress(p =>
        {
            p.CurrentStep = step;
            p.CurrentItem = current;
            p.TotalItems = total;
            p.ProgressPercent = percent;
            p.Status = current >= total ? OperationStatus.Completed : OperationStatus.Running;

            // Estimate remaining time based on current rate
            if (current > 0 && _stopwatch.Elapsed.TotalSeconds > 0)
            {
                var rate = current / _stopwatch.Elapsed.TotalSeconds;
                var remaining = (total - current) / rate;
                p.EstimatedTimeRemaining = TimeSpan.FromSeconds(remaining);
            }
        });
    }

    /// <summary>
    /// Reports indeterminate progress.
    /// </summary>
    public void ReportIndeterminate(string step)
    {
        EnsureStarted();
        UpdateProgress(p =>
        {
            p.CurrentStep = step;
            p.ProgressPercent = -1; // Indicates indeterminate
            p.Status = OperationStatus.Running;
        });
    }

    /// <summary>
    /// Reports completion.
    /// </summary>
    public void ReportCompleted(string message = "Operation completed")
    {
        _stopwatch.Stop();
        UpdateProgress(p =>
        {
            p.CurrentStep = message;
            p.ProgressPercent = 100;
            p.Status = OperationStatus.Completed;
            p.EstimatedTimeRemaining = null;
        });
    }

    /// <summary>
    /// Reports failure.
    /// </summary>
    public void ReportFailed(string error)
    {
        _stopwatch.Stop();
        UpdateProgress(p =>
        {
            p.CurrentStep = "Operation failed";
            p.ErrorMessage = error;
            p.Status = OperationStatus.Failed;
            p.EstimatedTimeRemaining = null;
        });
    }

    /// <summary>
    /// Starts the operation timer if not already started.
    /// </summary>
    private void EnsureStarted()
    {
        if (!_stopwatch.IsRunning && _currentProgress.Status == OperationStatus.NotStarted)
        {
            _stopwatch.Start();
        }
    }

    /// <summary>
    /// Updates progress in a thread-safe manner and raises the event.
    /// </summary>
    private void UpdateProgress(Action<ModuleProgressInfo> updateAction)
    {
        lock (_lock)
        {
            updateAction(_currentProgress);
            _currentProgress.ElapsedTime = _stopwatch.Elapsed;
        }

        ProgressChanged?.Invoke(this, _currentProgress);
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cts.Dispose();
            _stopwatch.Stop();
            _disposed = true;
        }
    }
}

/// <summary>
/// Extension methods for IProgress&lt;ModuleProgressInfo&gt;.
/// </summary>
public static class ModuleProgressExtensions
{
    /// <summary>
    /// Creates a progress reporter that forwards to the given IProgress instance.
    /// </summary>
    public static ModuleProgressReporter CreateReporter(this IProgress<ModuleProgressInfo>? progress, string operationName = "")
    {
        var reporter = new ModuleProgressReporter(operationName);
        if (progress != null)
        {
            reporter.ProgressChanged += (_, info) => progress.Report(info);
        }
        return reporter;
    }

    /// <summary>
    /// Reports simple percentage progress if progress is not null.
    /// </summary>
    public static void ReportIfNotNull(this IProgress<ModuleProgressInfo>? progress, string step, double percent)
    {
        progress?.Report(ModuleProgressInfo.Simple(step, percent));
    }

    /// <summary>
    /// Reports item progress if progress is not null.
    /// </summary>
    public static void ReportIfNotNull(this IProgress<ModuleProgressInfo>? progress, string step, int current, int total)
    {
        progress?.Report(ModuleProgressInfo.Items(step, current, total));
    }

    /// <summary>
    /// Reports indeterminate progress if progress is not null.
    /// </summary>
    public static void ReportIndeterminateIfNotNull(this IProgress<ModuleProgressInfo>? progress, string step)
    {
        progress?.Report(ModuleProgressInfo.Indeterminate(step));
    }
}
