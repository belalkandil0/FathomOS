using FathomOS.Core.Interfaces;
using System.Collections.Concurrent;

namespace FathomOS.Shell.Services;

/// <summary>
/// Centralized error reporting service.
/// Collects and logs errors from all modules.
/// </summary>
public class ErrorReporter : IErrorReporter
{
    private readonly ConcurrentQueue<ErrorReportedEventArgs> _errorLog = new();
    private readonly IEventAggregator? _eventAggregator;
    private const int MaxLogEntries = 1000;

    public ErrorReporter(IEventAggregator? eventAggregator = null)
    {
        _eventAggregator = eventAggregator;
    }

    /// <inheritdoc />
    public event EventHandler<ErrorReportedEventArgs>? ErrorReported;

    /// <inheritdoc />
    public void Report(string moduleId, string message, Exception? exception = null, ErrorSeverity severity = ErrorSeverity.Error)
    {
        var args = new ErrorReportedEventArgs
        {
            ModuleId = moduleId,
            Message = message,
            Exception = exception,
            Severity = severity,
            Timestamp = DateTime.UtcNow
        };

        // Add to log
        _errorLog.Enqueue(args);

        // Trim log if too large
        while (_errorLog.Count > MaxLogEntries)
        {
            _errorLog.TryDequeue(out _);
        }

        // Log to debug output
        var logMessage = $"[{severity}] {moduleId}: {message}";
        if (exception != null)
        {
            logMessage += $" | Exception: {exception.Message}";
        }
        System.Diagnostics.Debug.WriteLine($"ErrorReporter: {logMessage}");

        // Notify subscribers
        ErrorReported?.Invoke(this, args);

        // Publish via event aggregator
        _eventAggregator?.Publish(new Core.Messaging.ErrorOccurredEvent(moduleId, message, exception));
    }

    /// <inheritdoc />
    public void ReportInfo(string moduleId, string message)
    {
        Report(moduleId, message, null, ErrorSeverity.Info);
    }

    /// <inheritdoc />
    public void ReportWarning(string moduleId, string message)
    {
        Report(moduleId, message, null, ErrorSeverity.Warning);
    }

    /// <summary>
    /// Get all logged errors
    /// </summary>
    public IReadOnlyList<ErrorReportedEventArgs> GetErrorLog()
    {
        return _errorLog.ToList().AsReadOnly();
    }

    /// <summary>
    /// Clear the error log
    /// </summary>
    public void ClearErrorLog()
    {
        while (_errorLog.TryDequeue(out _)) { }
    }
}
