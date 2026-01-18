// FathomOS.Core/Security/SanitizedError.cs
// SECURITY FIX: VULN-008 / MISSING-010 - Error Message Sanitization
// Provides a sanitized error model that separates user-facing messages from technical details

namespace FathomOS.Core.Security;

/// <summary>
/// SECURITY FIX: Represents a sanitized error that separates user-facing messages
/// from technical details to prevent information disclosure in production environments.
/// </summary>
public sealed class SanitizedError
{
    /// <summary>
    /// Gets the unique error code for support reference.
    /// Format: ERR-XXXX where XXXX is a numeric code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the user-friendly error message safe for display.
    /// Does not contain stack traces, internal paths, or technical details.
    /// </summary>
    public string UserMessage { get; }

    /// <summary>
    /// Gets the full technical details including stack trace.
    /// This should ONLY be used for logging, never displayed to users in production.
    /// </summary>
    public string TechnicalDetails { get; }

    /// <summary>
    /// Gets the original exception, if available.
    /// This should ONLY be used for logging, never exposed to users in production.
    /// </summary>
    public Exception? OriginalException { get; }

    /// <summary>
    /// Gets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the correlation ID for tracking this error across logs.
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Gets the error category for classification.
    /// </summary>
    public ErrorCategory Category { get; }

    /// <summary>
    /// Creates a new sanitized error instance.
    /// </summary>
    /// <param name="errorCode">The unique error code (e.g., "ERR-1001")</param>
    /// <param name="userMessage">The user-friendly message to display</param>
    /// <param name="technicalDetails">Full technical details for logging</param>
    /// <param name="originalException">The original exception, if any</param>
    /// <param name="category">The error category</param>
    internal SanitizedError(
        string errorCode,
        string userMessage,
        string technicalDetails,
        Exception? originalException,
        ErrorCategory category)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
        UserMessage = userMessage ?? throw new ArgumentNullException(nameof(userMessage));
        TechnicalDetails = technicalDetails ?? string.Empty;
        OriginalException = originalException;
        Category = category;
        Timestamp = DateTime.UtcNow;
        CorrelationId = Guid.NewGuid();
    }

    /// <summary>
    /// Gets the display message appropriate for the current build configuration.
    /// In DEBUG builds, returns full details. In RELEASE builds, returns user message only.
    /// </summary>
    public string GetDisplayMessage()
    {
#if DEBUG
        return $"{UserMessage}\n\nTechnical Details:\n{TechnicalDetails}";
#else
        return UserMessage;
#endif
    }

    /// <summary>
    /// Gets a formatted string suitable for logging that includes all details.
    /// </summary>
    public string GetLogMessage()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{CorrelationId}] [{ErrorCode}] [{Category}]\n" +
               $"User Message: {UserMessage}\n" +
               $"Technical Details:\n{TechnicalDetails}";
    }

    /// <summary>
    /// Returns the user-friendly message for display purposes.
    /// </summary>
    public override string ToString()
    {
        return GetDisplayMessage();
    }
}

/// <summary>
/// SECURITY FIX: Categories for error classification.
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// General/unclassified errors.
    /// </summary>
    General = 0,

    /// <summary>
    /// File system related errors (file not found, access denied, etc.).
    /// </summary>
    FileSystem = 1,

    /// <summary>
    /// Database operation errors.
    /// </summary>
    Database = 2,

    /// <summary>
    /// Security and authorization errors.
    /// </summary>
    Security = 3,

    /// <summary>
    /// Network and connectivity errors.
    /// </summary>
    Network = 4,

    /// <summary>
    /// Data validation and format errors.
    /// </summary>
    Validation = 5,

    /// <summary>
    /// Configuration and settings errors.
    /// </summary>
    Configuration = 6,

    /// <summary>
    /// External dependency errors (third-party libraries, services).
    /// </summary>
    ExternalDependency = 7,

    /// <summary>
    /// Resource exhaustion errors (memory, disk space, etc.).
    /// </summary>
    Resource = 8,

    /// <summary>
    /// Concurrency and threading errors.
    /// </summary>
    Concurrency = 9
}
