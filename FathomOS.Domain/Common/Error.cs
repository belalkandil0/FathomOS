namespace FathomOS.Domain.Common;

/// <summary>
/// Represents the type/category of an error.
/// Used for error classification and appropriate handling strategies.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// A validation error - the input data is invalid.
    /// Typically results in HTTP 400 Bad Request.
    /// </summary>
    Validation,

    /// <summary>
    /// The requested resource was not found.
    /// Typically results in HTTP 404 Not Found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The user is not authorized to perform the operation.
    /// Typically results in HTTP 401 Unauthorized or 403 Forbidden.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// A conflict exists with the current state.
    /// Typically results in HTTP 409 Conflict.
    /// </summary>
    Conflict,

    /// <summary>
    /// An internal/unexpected error occurred.
    /// Typically results in HTTP 500 Internal Server Error.
    /// </summary>
    Internal,

    /// <summary>
    /// The operation is not permitted due to business rules.
    /// Typically results in HTTP 403 Forbidden.
    /// </summary>
    Forbidden,

    /// <summary>
    /// A precondition for the operation was not met.
    /// Typically results in HTTP 412 Precondition Failed.
    /// </summary>
    PreconditionFailed,

    /// <summary>
    /// The request could not be processed due to external dependency failure.
    /// Typically results in HTTP 503 Service Unavailable.
    /// </summary>
    ServiceUnavailable,

    /// <summary>
    /// The operation timed out.
    /// Typically results in HTTP 408 Request Timeout or 504 Gateway Timeout.
    /// </summary>
    Timeout
}

/// <summary>
/// Represents a domain error with a code, message, and type.
/// Immutable record type for representing errors in the Result pattern.
/// </summary>
/// <param name="Code">A machine-readable error code (e.g., "CERTIFICATE_NOT_FOUND")</param>
/// <param name="Message">A human-readable error message</param>
/// <param name="Type">The error type/category</param>
public record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new validation error</returns>
    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new not found error</returns>
    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new unauthorized error</returns>
    public static Error Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new conflict error</returns>
    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Creates an internal error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new internal error</returns>
    public static Error Internal(string code, string message) =>
        new(code, message, ErrorType.Internal);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new forbidden error</returns>
    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    /// <summary>
    /// Creates a precondition failed error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new precondition failed error</returns>
    public static Error PreconditionFailed(string code, string message) =>
        new(code, message, ErrorType.PreconditionFailed);

    /// <summary>
    /// Creates a service unavailable error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new service unavailable error</returns>
    public static Error ServiceUnavailable(string code, string message) =>
        new(code, message, ErrorType.ServiceUnavailable);

    /// <summary>
    /// Creates a timeout error.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new timeout error</returns>
    public static Error Timeout(string code, string message) =>
        new(code, message, ErrorType.Timeout);

    /// <summary>
    /// A special error representing no error (for successful results).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Internal);

    /// <summary>
    /// A special error representing a null or empty value.
    /// </summary>
    public static readonly Error NullValue = new("NULL_VALUE", "A null value was provided.", ErrorType.Validation);
}

/// <summary>
/// Represents a validation error with additional details about the validation failure.
/// </summary>
/// <param name="Code">A machine-readable error code</param>
/// <param name="Message">A human-readable error message</param>
/// <param name="PropertyName">The name of the property that failed validation</param>
/// <param name="AttemptedValue">The value that failed validation</param>
public sealed record ValidationError(
    string Code,
    string Message,
    string PropertyName,
    object? AttemptedValue = null) : Error(Code, Message, ErrorType.Validation);
