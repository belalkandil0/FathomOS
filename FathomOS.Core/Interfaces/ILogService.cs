namespace FathomOS.Core.Interfaces;

/// <summary>
/// Log service interface providing structured logging with Serilog-compatible API.
/// Supports scoped logging, context enrichment, and multiple output sinks.
/// </summary>
public interface ILogService
{
    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    /// <param name="message">Message template (supports structured logging placeholders).</param>
    /// <param name="args">Arguments for the message template.</param>
    void Debug(string message, params object[] args);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">Message template.</param>
    /// <param name="args">Arguments for the message template.</param>
    void Info(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">Message template.</param>
    /// <param name="args">Arguments for the message template.</param>
    void Warning(string message, params object[] args);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">Message template.</param>
    /// <param name="ex">Optional exception to include.</param>
    /// <param name="args">Arguments for the message template.</param>
    void Error(string message, Exception? ex = null, params object[] args);

    /// <summary>
    /// Logs a fatal error message.
    /// </summary>
    /// <param name="message">Message template.</param>
    /// <param name="ex">Optional exception to include.</param>
    /// <param name="args">Arguments for the message template.</param>
    void Fatal(string message, Exception? ex = null, params object[] args);

    /// <summary>
    /// Logs a message with the specified level.
    /// </summary>
    /// <param name="level">Log level.</param>
    /// <param name="message">Message template.</param>
    /// <param name="args">Arguments for the message template.</param>
    void Log(LogServiceLevel level, string message, params object[] args);

    /// <summary>
    /// Logs a message with the specified level and exception.
    /// </summary>
    /// <param name="level">Log level.</param>
    /// <param name="exception">Exception to include.</param>
    /// <param name="message">Message template.</param>
    /// <param name="args">Arguments for the message template.</param>
    void Log(LogServiceLevel level, Exception exception, string message, params object[] args);

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <param name="name">Scope name.</param>
    /// <param name="state">Optional state object to include in log context.</param>
    /// <returns>Disposable scope that ends when disposed.</returns>
    IDisposable BeginScope(string name, object? state = null);

    /// <summary>
    /// Begins a scope with structured properties.
    /// </summary>
    /// <param name="properties">Properties to add to log context.</param>
    /// <returns>Disposable scope.</returns>
    IDisposable BeginScope(IDictionary<string, object> properties);

    /// <summary>
    /// Adds a property to all subsequent log entries.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="value">Property value.</param>
    void PushProperty(string name, object value);

    /// <summary>
    /// Removes a previously pushed property.
    /// </summary>
    /// <param name="name">Property name.</param>
    void PopProperty(string name);

    /// <summary>
    /// Checks if logging at the specified level is enabled.
    /// </summary>
    /// <param name="level">Log level to check.</param>
    /// <returns>True if logging at this level is enabled.</returns>
    bool IsEnabled(LogServiceLevel level);

    /// <summary>
    /// Creates a logger for a specific type/category.
    /// </summary>
    /// <typeparam name="T">Type to create logger for.</typeparam>
    /// <returns>Logger instance.</returns>
    ILogService ForContext<T>();

    /// <summary>
    /// Creates a logger for a specific category name.
    /// </summary>
    /// <param name="categoryName">Category name.</param>
    /// <returns>Logger instance.</returns>
    ILogService ForContext(string categoryName);

    /// <summary>
    /// Flushes any buffered log entries.
    /// </summary>
    void Flush();
}

/// <summary>
/// Log levels for the log service.
/// </summary>
public enum LogServiceLevel
{
    /// <summary>
    /// Verbose/Trace level - very detailed information.
    /// </summary>
    Verbose = 0,

    /// <summary>
    /// Debug level - detailed information for debugging.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Information level - general operational information.
    /// </summary>
    Information = 2,

    /// <summary>
    /// Warning level - potential issues or unexpected situations.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Error level - errors that should be investigated.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Fatal level - critical errors that cause application failure.
    /// </summary>
    Fatal = 5
}

/// <summary>
/// Configuration for the log service.
/// </summary>
public class LogServiceOptions
{
    /// <summary>
    /// Minimum log level to capture.
    /// </summary>
    public LogServiceLevel MinimumLevel { get; set; } = LogServiceLevel.Information;

    /// <summary>
    /// Enable console output.
    /// </summary>
    public bool EnableConsole { get; set; } = true;

    /// <summary>
    /// Enable file output.
    /// </summary>
    public bool EnableFile { get; set; } = true;

    /// <summary>
    /// Log file directory.
    /// </summary>
    public string LogDirectory { get; set; } = GetDefaultLogDirectory();

    private static string GetDefaultLogDirectory() =>
        Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "FathomOS", "Logs");

    /// <summary>
    /// Log file name pattern (supports date placeholders).
    /// </summary>
    public string FileNamePattern { get; set; } = "FathomOS-{Date}.log";

    /// <summary>
    /// Maximum log file size in bytes before rolling.
    /// </summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Number of log files to retain.
    /// </summary>
    public int RetainedFileCount { get; set; } = 10;

    /// <summary>
    /// Output format template.
    /// </summary>
    public string OutputTemplate { get; set; } =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Enable structured JSON output.
    /// </summary>
    public bool UseJsonFormat { get; set; } = false;

    /// <summary>
    /// Application name to include in logs.
    /// </summary>
    public string ApplicationName { get; set; } = "FathomOS";

    /// <summary>
    /// Environment name to include in logs.
    /// </summary>
    public string Environment { get; set; } = "Production";

    /// <summary>
    /// Additional properties to include in all log entries.
    /// </summary>
    public Dictionary<string, object> GlobalProperties { get; set; } = new();
}

/// <summary>
/// Represents a log entry.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Log level.
    /// </summary>
    public LogServiceLevel Level { get; set; }

    /// <summary>
    /// Log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Message template (for structured logging).
    /// </summary>
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Exception if one was logged.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Source context (logger category).
    /// </summary>
    public string? SourceContext { get; set; }

    /// <summary>
    /// Scope name if within a scope.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Additional properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Thread ID that created the log entry.
    /// </summary>
    public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
}
