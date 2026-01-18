namespace FathomOS.Domain.Exceptions;

/// <summary>
/// Exception thrown when domain validation fails.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    /// <inheritdoc />
    public override string ErrorCode => "DOMAIN_VALIDATION_FAILED";

    /// <summary>
    /// Gets the validation errors that caused this exception.
    /// </summary>
    public IReadOnlyList<ValidationFailure> ValidationFailures { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    public DomainValidationException(string message)
        : base(message)
    {
        ValidationFailures = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    /// <param name="failures">The validation failures</param>
    public DomainValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more domain validation errors occurred.")
    {
        ValidationFailures = failures.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation</param>
    /// <param name="errorMessage">The error message</param>
    public DomainValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        ValidationFailures = [new ValidationFailure(propertyName, errorMessage)];
    }

    /// <summary>
    /// Creates a validation exception for a required field.
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <returns>A new DomainValidationException</returns>
    public static DomainValidationException Required(string propertyName)
    {
        return new DomainValidationException(propertyName, $"{propertyName} is required.");
    }

    /// <summary>
    /// Creates a validation exception for an invalid value.
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <param name="reason">The reason the value is invalid</param>
    /// <returns>A new DomainValidationException</returns>
    public static DomainValidationException Invalid(string propertyName, string reason)
    {
        return new DomainValidationException(propertyName, $"{propertyName} is invalid: {reason}");
    }
}

/// <summary>
/// Represents a single validation failure.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation</param>
/// <param name="ErrorMessage">The error message describing the failure</param>
/// <param name="AttemptedValue">The value that was attempted (optional)</param>
/// <param name="ErrorCode">A machine-readable error code (optional)</param>
public sealed record ValidationFailure(
    string PropertyName,
    string ErrorMessage,
    object? AttemptedValue = null,
    string? ErrorCode = null);
