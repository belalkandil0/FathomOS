// FathomOS.Core/Security/SecurityLogger.cs
// Simple security event logger for authentication and security events

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace FathomOS.Core.Security;

/// <summary>
/// Interface for simple security event logging.
/// For more comprehensive audit logging with tamper detection, use ISecurityAuditLog.
/// </summary>
public interface ISecurityLogger
{
    /// <summary>
    /// Logs an authentication attempt.
    /// </summary>
    /// <param name="username">The username attempting authentication.</param>
    /// <param name="success">Whether the authentication was successful.</param>
    void LogAuthenticationAttempt(string username, bool success);

    /// <summary>
    /// Logs a security event with details.
    /// </summary>
    /// <param name="eventType">The type of security event.</param>
    /// <param name="details">Details about the event.</param>
    void LogSecurityEvent(string eventType, string details);

    /// <summary>
    /// Logs a security warning.
    /// </summary>
    /// <param name="message">Warning message.</param>
    /// <param name="source">Source of the warning.</param>
    void LogWarning(string message, string? source = null);

    /// <summary>
    /// Logs a security error.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="exception">Optional exception details.</param>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Gets recent security events.
    /// </summary>
    /// <param name="count">Maximum number of events to return.</param>
    /// <returns>Collection of recent security events.</returns>
    IEnumerable<SecurityLogEntry> GetRecentEvents(int count = 100);

    /// <summary>
    /// Gets failed authentication attempts for a specific user.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="withinMinutes">Time window in minutes.</param>
    /// <returns>Number of failed attempts within the time window.</returns>
    int GetFailedAuthAttempts(string username, int withinMinutes = 15);

    /// <summary>
    /// Checks if a user is currently locked out due to failed attempts.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <returns>True if the user is locked out.</returns>
    bool IsUserLockedOut(string username);

    /// <summary>
    /// Clears the lockout for a specific user.
    /// </summary>
    /// <param name="username">The username to unlock.</param>
    void ClearLockout(string username);
}

/// <summary>
/// Represents a security log entry.
/// </summary>
public sealed class SecurityLogEntry
{
    /// <summary>
    /// Unique identifier for the log entry.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Timestamp of the event in UTC.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Type of security event.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Event severity level.
    /// </summary>
    public SecurityLogLevel Level { get; init; } = SecurityLogLevel.Information;

    /// <summary>
    /// Description or details of the event.
    /// </summary>
    public string Details { get; init; } = string.Empty;

    /// <summary>
    /// Associated username, if applicable.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Source of the event (e.g., module name).
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool? Success { get; init; }

    /// <summary>
    /// IP address if available.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Machine name.
    /// </summary>
    public string MachineName { get; init; } = Environment.MachineName;

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Converts the entry to JSON.
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}

/// <summary>
/// Security log severity levels.
/// </summary>
public enum SecurityLogLevel
{
    /// <summary>
    /// Debug-level messages.
    /// </summary>
    Debug = 0,

    /// <summary>
    /// Informational messages.
    /// </summary>
    Information = 1,

    /// <summary>
    /// Warning messages.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error messages.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Critical security events.
    /// </summary>
    Critical = 4
}

/// <summary>
/// Thread-safe security logger implementation with in-memory storage and file persistence.
///
/// Features:
/// - Thread-safe logging
/// - Automatic file rotation
/// - Failed authentication tracking
/// - Automatic user lockout after threshold
/// - In-memory ring buffer for recent events
/// </summary>
public sealed class SecurityLogger : ISecurityLogger, IDisposable
{
    // Configuration
    private const int MaxInMemoryEntries = 10000;
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 30;
    private const int DefaultTimeWindowMinutes = 15;
    private const long MaxLogFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    // Storage
    private readonly ConcurrentQueue<SecurityLogEntry> _recentEvents = new();
    private readonly ConcurrentDictionary<string, List<DateTime>> _failedAttempts = new();
    private readonly ConcurrentDictionary<string, DateTime> _lockedOutUsers = new();
    private readonly string _logDirectory;
    private readonly object _fileLock = new();

    private StreamWriter? _writer;
    private string _currentLogFile = string.Empty;
    private long _currentFileSize;
    private bool _disposed;

    /// <summary>
    /// Creates a new SecurityLogger instance.
    /// </summary>
    /// <param name="logDirectory">Optional custom log directory. Defaults to AppData\FathomOS\SecurityLogs.</param>
    public SecurityLogger(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? GetDefaultLogDirectory();
        EnsureLogDirectory();
        InitializeLogFile();
    }

    /// <inheritdoc/>
    public void LogAuthenticationAttempt(string username, bool success)
    {
        var sanitizedUsername = SanitizeUsername(username);
        var eventType = success ? "AuthenticationSuccess" : "AuthenticationFailure";
        var details = success
            ? $"User '{sanitizedUsername}' authenticated successfully"
            : $"Authentication failed for user '{sanitizedUsername}'";

        var entry = new SecurityLogEntry
        {
            EventType = eventType,
            Level = success ? SecurityLogLevel.Information : SecurityLogLevel.Warning,
            Details = details,
            Username = sanitizedUsername,
            Success = success,
            Source = "Authentication"
        };

        WriteEntry(entry);

        // Track failed attempts
        if (!success)
        {
            TrackFailedAttempt(sanitizedUsername);
        }
        else
        {
            // Clear failed attempts on success
            ClearFailedAttempts(sanitizedUsername);
        }
    }

    /// <inheritdoc/>
    public void LogSecurityEvent(string eventType, string details)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            eventType = "Unknown";

        var entry = new SecurityLogEntry
        {
            EventType = SanitizeEventType(eventType),
            Level = SecurityLogLevel.Information,
            Details = SanitizeDetails(details),
            Source = "Security"
        };

        WriteEntry(entry);
    }

    /// <inheritdoc/>
    public void LogWarning(string message, string? source = null)
    {
        var entry = new SecurityLogEntry
        {
            EventType = "SecurityWarning",
            Level = SecurityLogLevel.Warning,
            Details = SanitizeDetails(message),
            Source = source ?? "Security"
        };

        WriteEntry(entry);
    }

    /// <inheritdoc/>
    public void LogError(string message, Exception? exception = null)
    {
        var details = new StringBuilder(SanitizeDetails(message));

        if (exception != null)
        {
            details.Append($" | Exception: {exception.GetType().Name}: {SanitizeDetails(exception.Message)}");
        }

        var entry = new SecurityLogEntry
        {
            EventType = "SecurityError",
            Level = SecurityLogLevel.Error,
            Details = details.ToString(),
            Source = "Security",
            Metadata = exception != null ? new Dictionary<string, object>
            {
                ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name,
                ["stackTraceAvailable"] = exception.StackTrace != null
            } : null
        };

        WriteEntry(entry);
    }

    /// <inheritdoc/>
    public IEnumerable<SecurityLogEntry> GetRecentEvents(int count = 100)
    {
        return _recentEvents
            .OrderByDescending(e => e.Timestamp)
            .Take(Math.Min(count, MaxInMemoryEntries))
            .ToList();
    }

    /// <inheritdoc/>
    public int GetFailedAuthAttempts(string username, int withinMinutes = DefaultTimeWindowMinutes)
    {
        var sanitizedUsername = SanitizeUsername(username).ToLowerInvariant();
        var cutoff = DateTime.UtcNow.AddMinutes(-withinMinutes);

        if (_failedAttempts.TryGetValue(sanitizedUsername, out var attempts))
        {
            lock (attempts)
            {
                // Clean up old entries
                attempts.RemoveAll(t => t < cutoff);
                return attempts.Count;
            }
        }

        return 0;
    }

    /// <inheritdoc/>
    public bool IsUserLockedOut(string username)
    {
        var sanitizedUsername = SanitizeUsername(username).ToLowerInvariant();

        // Check if explicitly locked out
        if (_lockedOutUsers.TryGetValue(sanitizedUsername, out var lockoutTime))
        {
            if (DateTime.UtcNow < lockoutTime.AddMinutes(LockoutMinutes))
            {
                return true;
            }

            // Lockout expired, remove it
            _lockedOutUsers.TryRemove(sanitizedUsername, out _);
        }

        // Check if threshold exceeded
        var failedAttempts = GetFailedAuthAttempts(sanitizedUsername);
        if (failedAttempts >= MaxFailedAttempts)
        {
            // Apply lockout
            _lockedOutUsers[sanitizedUsername] = DateTime.UtcNow;

            LogSecurityEvent("UserLockedOut",
                $"User '{sanitizedUsername}' locked out after {failedAttempts} failed authentication attempts");

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void ClearLockout(string username)
    {
        var sanitizedUsername = SanitizeUsername(username).ToLowerInvariant();

        _lockedOutUsers.TryRemove(sanitizedUsername, out _);
        ClearFailedAttempts(sanitizedUsername);

        LogSecurityEvent("LockoutCleared", $"Lockout cleared for user '{sanitizedUsername}'");
    }

    /// <summary>
    /// Disposes the security logger and flushes any pending writes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_fileLock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }

        _disposed = true;
    }

    #region Private Methods

    private void WriteEntry(SecurityLogEntry entry)
    {
        // Add to in-memory buffer
        _recentEvents.Enqueue(entry);

        // Trim buffer if too large
        while (_recentEvents.Count > MaxInMemoryEntries)
        {
            _recentEvents.TryDequeue(out _);
        }

        // Write to file
        WriteToFile(entry);
    }

    private void WriteToFile(SecurityLogEntry entry)
    {
        lock (_fileLock)
        {
            if (_disposed)
                return;

            try
            {
                EnsureLogFile();

                var json = entry.ToJson();
                _writer?.WriteLine(json);
                _writer?.Flush();

                _currentFileSize += Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length;

                // Check for rotation
                if (_currentFileSize >= MaxLogFileSizeBytes)
                {
                    RotateLogFile();
                }
            }
            catch (IOException)
            {
                // Try to recover
                CloseWriter();
                InitializeLogFile();

                try
                {
                    var json = entry.ToJson();
                    _writer?.WriteLine(json);
                    _writer?.Flush();
                }
                catch
                {
                    // Silently fail if we still can't write
                }
            }
        }
    }

    private void EnsureLogDirectory()
    {
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    private void EnsureLogFile()
    {
        var expectedFile = GetLogFileName();

        if (_currentLogFile != expectedFile || _writer == null)
        {
            CloseWriter();
            InitializeLogFile();
        }
    }

    private void InitializeLogFile()
    {
        var logFile = GetLogFileName();

        _writer = new StreamWriter(logFile, append: true, Encoding.UTF8)
        {
            AutoFlush = false
        };

        _currentLogFile = logFile;
        _currentFileSize = File.Exists(logFile) ? new FileInfo(logFile).Length : 0;
    }

    private void RotateLogFile()
    {
        CloseWriter();

        var timestamp = DateTime.UtcNow.ToString("HHmmss");
        var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var newLogFile = Path.Combine(_logDirectory, $"security_{dateStr}_{timestamp}.jsonl");

        _writer = new StreamWriter(newLogFile, append: false, Encoding.UTF8)
        {
            AutoFlush = false
        };

        _currentLogFile = newLogFile;
        _currentFileSize = 0;
    }

    private void CloseWriter()
    {
        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;
    }

    private string GetLogFileName()
    {
        var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return Path.Combine(_logDirectory, $"security_{dateStr}.jsonl");
    }

    private static string GetDefaultLogDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "FathomOS", "SecurityLogs");
    }

    private void TrackFailedAttempt(string username)
    {
        var key = username.ToLowerInvariant();

        var attempts = _failedAttempts.GetOrAdd(key, _ => new List<DateTime>());

        lock (attempts)
        {
            attempts.Add(DateTime.UtcNow);

            // Clean up old entries
            var cutoff = DateTime.UtcNow.AddMinutes(-DefaultTimeWindowMinutes);
            attempts.RemoveAll(t => t < cutoff);
        }
    }

    private void ClearFailedAttempts(string username)
    {
        var key = username.ToLowerInvariant();
        _failedAttempts.TryRemove(key, out _);
    }

    private static string SanitizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "[unknown]";

        // Remove control characters and limit length
        var sanitized = new string(username
            .Where(c => !char.IsControl(c) && c != '\0')
            .Take(100)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "[unknown]" : sanitized;
    }

    private static string SanitizeEventType(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return "Unknown";

        // Only allow alphanumeric and basic punctuation
        var sanitized = new string(eventType
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
            .Take(100)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
    }

    private static string SanitizeDetails(string details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return string.Empty;

        // Remove control characters (except newlines) and null bytes
        var sanitized = new string(details
            .Where(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t')
            .Where(c => c != '\0')
            .Take(4000)
            .ToArray());

        // Escape newlines for single-line JSON
        return sanitized
            .Replace("\r\n", "\\r\\n")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    #endregion
}

/// <summary>
/// Factory for creating security loggers.
/// </summary>
public static class SecurityLoggerFactory
{
    private static readonly Lazy<ISecurityLogger> DefaultInstance =
        new(() => new SecurityLogger());

    /// <summary>
    /// Gets the default security logger instance.
    /// </summary>
    public static ISecurityLogger Default => DefaultInstance.Value;

    /// <summary>
    /// Creates a new security logger with custom settings.
    /// </summary>
    /// <param name="logDirectory">Custom log directory.</param>
    /// <returns>A new security logger instance.</returns>
    public static ISecurityLogger Create(string? logDirectory = null)
    {
        return new SecurityLogger(logDirectory);
    }
}
