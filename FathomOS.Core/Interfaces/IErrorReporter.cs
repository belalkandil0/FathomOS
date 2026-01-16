namespace FathomOS.Core.Interfaces;

/// <summary>
/// Severity level for reported errors
/// </summary>
public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Contract for centralized error reporting.
/// Implemented by Shell for consistent error handling across modules.
/// </summary>
public interface IErrorReporter
{
    /// <summary>
    /// Report an error with optional exception
    /// </summary>
    /// <param name="moduleId">The module where the error occurred</param>
    /// <param name="message">Human-readable error message</param>
    /// <param name="exception">Optional exception details</param>
    /// <param name="severity">Error severity level</param>
    void Report(string moduleId, string message, Exception? exception = null, ErrorSeverity severity = ErrorSeverity.Error);

    /// <summary>
    /// Report an informational message
    /// </summary>
    /// <param name="moduleId">The module source</param>
    /// <param name="message">The message to report</param>
    void ReportInfo(string moduleId, string message);

    /// <summary>
    /// Report a warning
    /// </summary>
    /// <param name="moduleId">The module source</param>
    /// <param name="message">The warning message</param>
    void ReportWarning(string moduleId, string message);

    /// <summary>
    /// Event fired when an error is reported
    /// </summary>
    event EventHandler<ErrorReportedEventArgs>? ErrorReported;
}

/// <summary>
/// Event arguments for error reporting
/// </summary>
public class ErrorReportedEventArgs : EventArgs
{
    public string ModuleId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public ErrorSeverity Severity { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
