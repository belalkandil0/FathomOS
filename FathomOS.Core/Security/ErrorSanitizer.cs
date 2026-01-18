// FathomOS.Core/Security/ErrorSanitizer.cs
// SECURITY FIX: VULN-008 / MISSING-010 - Error Message Sanitization
// Provides utility for sanitizing error messages to prevent information disclosure

using System.Collections.Concurrent;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FathomOS.Core.Logging;
using Microsoft.Data.Sqlite;

namespace FathomOS.Core.Security;

/// <summary>
/// SECURITY FIX: Utility class for sanitizing exception messages to prevent
/// information disclosure in production environments.
///
/// In DEBUG builds: Full exception details are returned for debugging.
/// In RELEASE builds: User-friendly messages are returned without technical details.
///
/// All error details are preserved for logging regardless of build configuration.
/// </summary>
public static class ErrorSanitizer
{
    // Thread-safe logger instance (can be set externally)
    private static ILogger? _logger;
    private static readonly object _loggerLock = new();

    // Error code mappings for common exception types
    private static readonly ConcurrentDictionary<Type, (string Code, string Message, ErrorCategory Category)> _errorMappings = new();

    // Patterns for sensitive data that should be scrubbed even from logs
    private static readonly Regex[] _sensitivePatterns =
    [
        new Regex(@"password\s*[=:]\s*[^\s;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"pwd\s*[=:]\s*[^\s;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"key\s*[=:]\s*[A-Za-z0-9+/=]{20,}", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"secret\s*[=:]\s*[^\s;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"token\s*[=:]\s*[^\s;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"connectionstring\s*[=:]\s*[^\r\n]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    // Static constructor to initialize default error mappings
    static ErrorSanitizer()
    {
        InitializeDefaultMappings();
    }

    /// <summary>
    /// Sets the logger instance for error logging.
    /// </summary>
    /// <param name="logger">The logger to use for error logging.</param>
    public static void SetLogger(ILogger? logger)
    {
        lock (_loggerLock)
        {
            _logger = logger;
        }
    }

    /// <summary>
    /// SECURITY FIX: Sanitizes an exception and returns a safe error representation.
    /// Logs the full error details while returning only user-appropriate information.
    /// </summary>
    /// <param name="exception">The exception to sanitize.</param>
    /// <param name="context">Optional context string to help identify where the error occurred.</param>
    /// <param name="logError">Whether to log the error (default: true).</param>
    /// <returns>A sanitized error object safe for user display.</returns>
    public static SanitizedError Sanitize(Exception exception, string? context = null, bool logError = true)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // Get the appropriate error mapping for this exception type
        var (errorCode, userMessage, category) = GetErrorMapping(exception);

        // Build technical details string
        var technicalDetails = BuildTechnicalDetails(exception, context);

        // Scrub sensitive data from technical details before logging
        var scrubbedDetails = ScrubSensitiveData(technicalDetails);

        // Create the sanitized error
        var sanitizedError = new SanitizedError(
            errorCode,
            userMessage,
            scrubbedDetails,
            exception,
            category);

        // Log the error if requested
        if (logError)
        {
            LogError(sanitizedError);
        }

        return sanitizedError;
    }

    /// <summary>
    /// SECURITY FIX: Sanitizes an exception and returns only the user-safe message string.
    /// This is a convenience method for simple scenarios.
    /// </summary>
    /// <param name="exception">The exception to sanitize.</param>
    /// <param name="context">Optional context string.</param>
    /// <param name="logError">Whether to log the error (default: true).</param>
    /// <returns>A user-safe error message string.</returns>
    public static string SanitizeToString(Exception exception, string? context = null, bool logError = true)
    {
        var sanitized = Sanitize(exception, context, logError);
        return sanitized.GetDisplayMessage();
    }

    /// <summary>
    /// SECURITY FIX: Gets a sanitized error message for display, logging the full details.
    /// Returns different content based on build configuration.
    /// </summary>
    /// <param name="exception">The exception to process.</param>
    /// <param name="context">Optional context for the error.</param>
    /// <returns>
    /// In DEBUG: Full exception details.
    /// In RELEASE: User-friendly message with error code.
    /// </returns>
    public static string GetDisplayMessage(Exception exception, string? context = null)
    {
        var sanitized = Sanitize(exception, context, logError: true);
        return sanitized.GetDisplayMessage();
    }

    /// <summary>
    /// Registers a custom error mapping for a specific exception type.
    /// </summary>
    /// <typeparam name="TException">The exception type to map.</typeparam>
    /// <param name="errorCode">The error code (e.g., "ERR-1001").</param>
    /// <param name="userMessage">The user-friendly message.</param>
    /// <param name="category">The error category.</param>
    public static void RegisterMapping<TException>(string errorCode, string userMessage, ErrorCategory category)
        where TException : Exception
    {
        _errorMappings[typeof(TException)] = (errorCode, userMessage, category);
    }

    /// <summary>
    /// Clears all custom error mappings and restores defaults.
    /// </summary>
    public static void ResetMappings()
    {
        _errorMappings.Clear();
        InitializeDefaultMappings();
    }

    #region Private Methods

    /// <summary>
    /// SECURITY FIX: Initializes the default error code mappings for common exceptions.
    /// </summary>
    private static void InitializeDefaultMappings()
    {
        // File System Errors (ERR-1xxx)
        _errorMappings[typeof(FileNotFoundException)] =
            ("ERR-1001", "The requested file could not be found.", ErrorCategory.FileSystem);

        _errorMappings[typeof(DirectoryNotFoundException)] =
            ("ERR-1002", "The specified directory could not be found.", ErrorCategory.FileSystem);

        _errorMappings[typeof(PathTooLongException)] =
            ("ERR-1003", "The file path is too long. Please use a shorter path.", ErrorCategory.FileSystem);

        _errorMappings[typeof(DriveNotFoundException)] =
            ("ERR-1004", "The specified drive could not be found.", ErrorCategory.FileSystem);

        _errorMappings[typeof(IOException)] =
            ("ERR-1005", "A file operation failed. The file may be in use or inaccessible.", ErrorCategory.FileSystem);

        // Database Errors (ERR-2xxx)
        _errorMappings[typeof(SqliteException)] =
            ("ERR-2001", "Database operation failed. Please try again.", ErrorCategory.Database);

        // Security Errors (ERR-3xxx)
        _errorMappings[typeof(UnauthorizedAccessException)] =
            ("ERR-3001", "Access denied. Please check your permissions.", ErrorCategory.Security);

        _errorMappings[typeof(SecurityException)] =
            ("ERR-3002", "A security error occurred. Please contact support.", ErrorCategory.Security);

        _errorMappings[typeof(CryptographicException)] =
            ("ERR-3003", "A cryptographic operation failed. Please try again.", ErrorCategory.Security);

        // Network Errors (ERR-4xxx)
        _errorMappings[typeof(System.Net.Http.HttpRequestException)] =
            ("ERR-4001", "Network request failed. Please check your connection.", ErrorCategory.Network);

        _errorMappings[typeof(System.Net.Sockets.SocketException)] =
            ("ERR-4002", "Network connection failed. Please check your connection.", ErrorCategory.Network);

        _errorMappings[typeof(TimeoutException)] =
            ("ERR-4003", "The operation timed out. Please try again.", ErrorCategory.Network);

        // Validation Errors (ERR-5xxx)
        _errorMappings[typeof(ArgumentException)] =
            ("ERR-5001", "Invalid input provided. Please check your data.", ErrorCategory.Validation);

        _errorMappings[typeof(ArgumentNullException)] =
            ("ERR-5002", "Required information is missing.", ErrorCategory.Validation);

        _errorMappings[typeof(ArgumentOutOfRangeException)] =
            ("ERR-5003", "A value is outside the acceptable range.", ErrorCategory.Validation);

        _errorMappings[typeof(FormatException)] =
            ("ERR-5004", "The data format is incorrect. Please check your input.", ErrorCategory.Validation);

        _errorMappings[typeof(InvalidDataException)] =
            ("ERR-5005", "The data is invalid or corrupted.", ErrorCategory.Validation);

        // Configuration Errors (ERR-6xxx)
        _errorMappings[typeof(InvalidOperationException)] =
            ("ERR-6001", "The operation is not valid in the current state.", ErrorCategory.Configuration);

        _errorMappings[typeof(NotSupportedException)] =
            ("ERR-6002", "This operation is not supported.", ErrorCategory.Configuration);

        _errorMappings[typeof(PlatformNotSupportedException)] =
            ("ERR-6003", "This feature is not supported on your platform.", ErrorCategory.Configuration);

        // External Dependency Errors (ERR-7xxx)
        _errorMappings[typeof(DllNotFoundException)] =
            ("ERR-7001", "A required component is missing. Please reinstall the application.", ErrorCategory.ExternalDependency);

        _errorMappings[typeof(BadImageFormatException)] =
            ("ERR-7002", "A component could not be loaded. Please reinstall the application.", ErrorCategory.ExternalDependency);

        _errorMappings[typeof(TypeLoadException)] =
            ("ERR-7003", "Failed to load a required component.", ErrorCategory.ExternalDependency);

        // Resource Errors (ERR-8xxx)
        _errorMappings[typeof(OutOfMemoryException)] =
            ("ERR-8001", "The system is running low on memory. Please close other applications.", ErrorCategory.Resource);

        _errorMappings[typeof(StackOverflowException)] =
            ("ERR-8002", "A critical system error occurred. Please restart the application.", ErrorCategory.Resource);

        // Concurrency Errors (ERR-9xxx)
        _errorMappings[typeof(OperationCanceledException)] =
            ("ERR-9001", "The operation was cancelled.", ErrorCategory.Concurrency);

        _errorMappings[typeof(TaskCanceledException)] =
            ("ERR-9002", "The operation was cancelled.", ErrorCategory.Concurrency);

        _errorMappings[typeof(ObjectDisposedException)] =
            ("ERR-9003", "A resource is no longer available.", ErrorCategory.Concurrency);

        // Note: ERR-9999 is reserved for unknown/generic exceptions (handled in GetErrorMapping)
    }

    /// <summary>
    /// SECURITY FIX: Gets the error mapping for an exception type, checking base types.
    /// </summary>
    private static (string Code, string Message, ErrorCategory Category) GetErrorMapping(Exception exception)
    {
        var exceptionType = exception.GetType();

        // First try exact type match
        if (_errorMappings.TryGetValue(exceptionType, out var mapping))
        {
            return mapping;
        }

        // Try base types
        var baseType = exceptionType.BaseType;
        while (baseType != null && baseType != typeof(object))
        {
            if (_errorMappings.TryGetValue(baseType, out mapping))
            {
                return mapping;
            }
            baseType = baseType.BaseType;
        }

        // Check for aggregate exception and return the first inner exception's mapping
        if (exception is AggregateException aggregateException && aggregateException.InnerExceptions.Count > 0)
        {
            return GetErrorMapping(aggregateException.InnerExceptions[0]);
        }

        // Check inner exception
        if (exception.InnerException != null)
        {
            var innerMapping = GetErrorMapping(exception.InnerException);
            if (innerMapping.Code != "ERR-9999")
            {
                return innerMapping;
            }
        }

        // Default fallback for unknown exceptions
        return ("ERR-9999", "An unexpected error occurred. Please try again or contact support.", ErrorCategory.General);
    }

    /// <summary>
    /// SECURITY FIX: Builds the technical details string for an exception.
    /// </summary>
    private static string BuildTechnicalDetails(Exception exception, string? context)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(context))
        {
            sb.AppendLine($"Context: {context}");
        }

        sb.AppendLine($"Exception Type: {exception.GetType().FullName}");
        sb.AppendLine($"Message: {exception.Message}");

        if (exception.Source != null)
        {
            sb.AppendLine($"Source: {exception.Source}");
        }

        if (exception.TargetSite != null)
        {
            sb.AppendLine($"Method: {exception.TargetSite.DeclaringType?.FullName}.{exception.TargetSite.Name}");
        }

        // Include HResult for system exceptions
        sb.AppendLine($"HResult: 0x{exception.HResult:X8}");

        // Include data dictionary if present
        if (exception.Data.Count > 0)
        {
            sb.AppendLine("Additional Data:");
            foreach (var key in exception.Data.Keys)
            {
                sb.AppendLine($"  {key}: {exception.Data[key]}");
            }
        }

        // Include stack trace
        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            sb.AppendLine("Stack Trace:");
            sb.AppendLine(exception.StackTrace);
        }

        // Handle inner exceptions
        var innerException = exception.InnerException;
        var depth = 0;
        while (innerException != null && depth < 5) // Limit depth to prevent infinite loops
        {
            depth++;
            sb.AppendLine();
            sb.AppendLine($"--- Inner Exception {depth} ---");
            sb.AppendLine($"Type: {innerException.GetType().FullName}");
            sb.AppendLine($"Message: {innerException.Message}");

            if (!string.IsNullOrEmpty(innerException.StackTrace))
            {
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(innerException.StackTrace);
            }

            innerException = innerException.InnerException;
        }

        // Handle aggregate exceptions
        if (exception is AggregateException aggregateException)
        {
            sb.AppendLine();
            sb.AppendLine($"--- Aggregate Exception ({aggregateException.InnerExceptions.Count} inner exceptions) ---");
            var index = 0;
            foreach (var inner in aggregateException.InnerExceptions.Take(5)) // Limit to first 5
            {
                sb.AppendLine($"[{index++}] {inner.GetType().Name}: {inner.Message}");
            }

            if (aggregateException.InnerExceptions.Count > 5)
            {
                sb.AppendLine($"... and {aggregateException.InnerExceptions.Count - 5} more");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// SECURITY FIX: Scrubs sensitive data from a string before logging.
    /// </summary>
    private static string ScrubSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = input;
        foreach (var pattern in _sensitivePatterns)
        {
            result = pattern.Replace(result, "[REDACTED]");
        }

        return result;
    }

    /// <summary>
    /// SECURITY FIX: Logs an error using the configured logger.
    /// </summary>
    private static void LogError(SanitizedError error)
    {
        ILogger? logger;
        lock (_loggerLock)
        {
            logger = _logger;
        }

        if (logger == null)
        {
            return;
        }

        var logMessage = error.GetLogMessage();

        if (error.OriginalException != null)
        {
            logger.Error(logMessage, error.OriginalException, nameof(ErrorSanitizer));
        }
        else
        {
            logger.Error(logMessage, nameof(ErrorSanitizer));
        }
    }

    #endregion

    #region Extension Methods Support

    /// <summary>
    /// SECURITY FIX: Creates a sanitized error from an exception without logging.
    /// Useful when you want to handle logging yourself.
    /// </summary>
    /// <param name="exception">The exception to sanitize.</param>
    /// <param name="context">Optional context string.</param>
    /// <returns>A sanitized error object.</returns>
    public static SanitizedError CreateSanitizedError(Exception exception, string? context = null)
    {
        return Sanitize(exception, context, logError: false);
    }

    /// <summary>
    /// SECURITY FIX: Checks if the current build is a debug build.
    /// </summary>
    public static bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    #endregion
}

/// <summary>
/// SECURITY FIX: Extension methods for exception sanitization.
/// </summary>
public static class ErrorSanitizerExtensions
{
    /// <summary>
    /// Sanitizes this exception and returns a user-safe message.
    /// </summary>
    /// <param name="exception">The exception to sanitize.</param>
    /// <param name="context">Optional context for the error.</param>
    /// <returns>A sanitized error object.</returns>
    public static SanitizedError Sanitize(this Exception exception, string? context = null)
    {
        return ErrorSanitizer.Sanitize(exception, context);
    }

    /// <summary>
    /// Gets a display-safe message from this exception.
    /// </summary>
    /// <param name="exception">The exception to process.</param>
    /// <param name="context">Optional context for the error.</param>
    /// <returns>A user-safe message string.</returns>
    public static string ToSafeMessage(this Exception exception, string? context = null)
    {
        return ErrorSanitizer.SanitizeToString(exception, context);
    }
}
