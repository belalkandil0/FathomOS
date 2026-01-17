namespace FathomOS.Core.Logging;

/// <summary>
/// Thread-safe file-based logger implementation.
/// Writes log entries to a file with automatic date-based rotation.
/// </summary>
public class FileLogger : ILogger, IDisposable
{
    private readonly string _logDirectory;
    private readonly string _logFilePrefix;
    private readonly object _lock = new();
    private readonly LogLevel _minimumLevel;
    private StreamWriter? _writer;
    private string _currentLogDate = string.Empty;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the FileLogger.
    /// </summary>
    /// <param name="logDirectory">Directory where log files will be stored.</param>
    /// <param name="logFilePrefix">Prefix for log file names (default: "FathomOS").</param>
    /// <param name="minimumLevel">Minimum log level to record (default: Info).</param>
    public FileLogger(string logDirectory, string logFilePrefix = "FathomOS", LogLevel minimumLevel = LogLevel.Info)
    {
        _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
        _logFilePrefix = logFilePrefix ?? throw new ArgumentNullException(nameof(logFilePrefix));
        _minimumLevel = minimumLevel;

        EnsureLogDirectory();
    }

    /// <summary>
    /// Gets the minimum log level. Messages below this level will be ignored.
    /// </summary>
    public LogLevel MinimumLevel => _minimumLevel;

    /// <summary>
    /// Gets the current log file path.
    /// </summary>
    public string CurrentLogFilePath => GetLogFilePath(DateTime.Now);

    /// <inheritdoc/>
    public void Log(LogLevel level, string message, string? source = null)
    {
        if (!IsEnabled(level))
            return;

        WriteLogEntry(level, message, null, source);
    }

    /// <inheritdoc/>
    public void Log(LogLevel level, string message, Exception exception, string? source = null)
    {
        if (!IsEnabled(level))
            return;

        WriteLogEntry(level, message, exception, source);
    }

    /// <inheritdoc/>
    public void Debug(string message, string? source = null) => Log(LogLevel.Debug, message, source);

    /// <inheritdoc/>
    public void Info(string message, string? source = null) => Log(LogLevel.Info, message, source);

    /// <inheritdoc/>
    public void Warning(string message, string? source = null) => Log(LogLevel.Warning, message, source);

    /// <inheritdoc/>
    public void Warning(string message, Exception exception, string? source = null) => Log(LogLevel.Warning, message, exception, source);

    /// <inheritdoc/>
    public void Error(string message, string? source = null) => Log(LogLevel.Error, message, source);

    /// <inheritdoc/>
    public void Error(string message, Exception exception, string? source = null) => Log(LogLevel.Error, message, exception, source);

    /// <inheritdoc/>
    public void Critical(string message, string? source = null) => Log(LogLevel.Critical, message, source);

    /// <inheritdoc/>
    public void Critical(string message, Exception exception, string? source = null) => Log(LogLevel.Critical, message, exception, source);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

    /// <inheritdoc/>
    public void Flush()
    {
        lock (_lock)
        {
            _writer?.Flush();
        }
    }

    /// <summary>
    /// Disposes resources used by the logger.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            lock (_lock)
            {
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
        }

        _disposed = true;
    }

    private void WriteLogEntry(LogLevel level, string message, Exception? exception, string? source)
    {
        var timestamp = DateTime.Now;
        var entry = FormatLogEntry(timestamp, level, message, exception, source);

        lock (_lock)
        {
            if (_disposed)
                return;

            EnsureWriterForDate(timestamp);

            try
            {
                _writer?.WriteLine(entry);

                // Auto-flush for errors and critical messages
                if (level >= LogLevel.Error)
                {
                    _writer?.Flush();
                }
            }
            catch (IOException)
            {
                // If write fails, try to recreate the writer
                CloseWriter();
                EnsureWriterForDate(timestamp);
                _writer?.WriteLine(entry);
            }
        }
    }

    private string FormatLogEntry(DateTime timestamp, LogLevel level, string message, Exception? exception, string? source)
    {
        var levelStr = level.ToString().ToUpperInvariant().PadRight(8);
        var sourceStr = string.IsNullOrEmpty(source) ? "" : $"[{source}] ";
        var entry = $"{timestamp:yyyy-MM-dd HH:mm:ss.fff} [{levelStr}] {sourceStr}{message}";

        if (exception != null)
        {
            entry += Environment.NewLine + FormatException(exception);
        }

        return entry;
    }

    private static string FormatException(Exception exception, int indent = 0)
    {
        var indentStr = new string(' ', indent * 2);
        var sb = new StringBuilder();

        sb.AppendLine($"{indentStr}Exception Type: {exception.GetType().FullName}");
        sb.AppendLine($"{indentStr}Message: {exception.Message}");

        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            sb.AppendLine($"{indentStr}Stack Trace:");
            foreach (var line in exception.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                sb.AppendLine($"{indentStr}  {line}");
            }
        }

        if (exception.InnerException != null)
        {
            sb.AppendLine($"{indentStr}Inner Exception:");
            sb.Append(FormatException(exception.InnerException, indent + 1));
        }

        return sb.ToString().TrimEnd();
    }

    private void EnsureLogDirectory()
    {
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    private string GetLogFilePath(DateTime date)
    {
        return Path.Combine(_logDirectory, $"{_logFilePrefix}_{date:yyyy-MM-dd}.log");
    }

    private void EnsureWriterForDate(DateTime timestamp)
    {
        var dateStr = timestamp.ToString("yyyy-MM-dd");

        if (_currentLogDate != dateStr || _writer == null)
        {
            CloseWriter();

            var logFilePath = GetLogFilePath(timestamp);
            _writer = new StreamWriter(logFilePath, append: true, Encoding.UTF8)
            {
                AutoFlush = false
            };
            _currentLogDate = dateStr;
        }
    }

    private void CloseWriter()
    {
        if (_writer != null)
        {
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
        }
    }
}
