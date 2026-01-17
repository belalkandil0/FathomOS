namespace FathomOS.Core.Logging;

/// <summary>
/// Interface for centralized logging throughout FathomOS.
/// Provides thread-safe logging capabilities with multiple severity levels.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Gets the minimum log level. Messages below this level will be ignored.
    /// </summary>
    LogLevel MinimumLevel { get; }

    /// <summary>
    /// Logs a message with the specified level.
    /// </summary>
    /// <param name="level">The severity level of the log message.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="source">Optional source identifier (class name, module, etc.).</param>
    void Log(LogLevel level, string message, string? source = null);

    /// <summary>
    /// Logs a message with an associated exception.
    /// </summary>
    /// <param name="level">The severity level of the log message.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to include in the log.</param>
    /// <param name="source">Optional source identifier (class name, module, etc.).</param>
    void Log(LogLevel level, string message, Exception exception, string? source = null);

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="source">Optional source identifier.</param>
    void Debug(string message, string? source = null);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="source">Optional source identifier.</param>
    void Info(string message, string? source = null);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="source">Optional source identifier.</param>
    void Warning(string message, string? source = null);

    /// <summary>
    /// Logs a warning message with an exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to include.</param>
    /// <param name="source">Optional source identifier.</param>
    void Warning(string message, Exception exception, string? source = null);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="source">Optional source identifier.</param>
    void Error(string message, string? source = null);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to include.</param>
    /// <param name="source">Optional source identifier.</param>
    void Error(string message, Exception exception, string? source = null);

    /// <summary>
    /// Logs a critical error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="source">Optional source identifier.</param>
    void Critical(string message, string? source = null);

    /// <summary>
    /// Logs a critical error message with an exception.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to include.</param>
    /// <param name="source">Optional source identifier.</param>
    void Critical(string message, Exception exception, string? source = null);

    /// <summary>
    /// Checks if the specified log level is enabled.
    /// </summary>
    /// <param name="level">The log level to check.</param>
    /// <returns>True if logging at this level is enabled.</returns>
    bool IsEnabled(LogLevel level);

    /// <summary>
    /// Flushes any buffered log entries to the underlying storage.
    /// </summary>
    void Flush();
}
