namespace FathomOS.Domain.Exceptions;

/// <summary>
/// Base class for all domain-specific exceptions.
/// These exceptions represent violations of business rules or domain invariants.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Gets the unique error code identifying this type of exception.
    /// Used for logging, monitoring, and client-side error handling.
    /// </summary>
    public abstract string ErrorCode { get; }

    /// <summary>
    /// Gets additional details about the exception for debugging purposes.
    /// </summary>
    public IReadOnlyDictionary<string, object> Details { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    protected DomainException(string message)
        : base(message)
    {
        Details = new Dictionary<string, object>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="details">Additional details about the exception</param>
    protected DomainException(string message, IDictionary<string, object> details)
        : base(message)
    {
        Details = new Dictionary<string, object>(details);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        Details = new Dictionary<string, object>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="details">Additional details about the exception</param>
    /// <param name="innerException">The inner exception</param>
    protected DomainException(string message, IDictionary<string, object> details, Exception innerException)
        : base(message, innerException)
    {
        Details = new Dictionary<string, object>(details);
    }
}
