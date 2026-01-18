namespace FathomOS.Core.Interfaces;

/// <summary>
/// Unified validation service interface providing entity validation
/// with support for fluent rules, attribute-based validation, and custom validators.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates an entity using all registered validators and validation attributes.
    /// </summary>
    /// <typeparam name="T">Type of entity to validate.</typeparam>
    /// <param name="entity">Entity to validate.</param>
    /// <returns>Validation result containing any errors.</returns>
    ValidationResult Validate<T>(T entity);

    /// <summary>
    /// Validates a specific property of an entity.
    /// </summary>
    /// <typeparam name="T">Type of entity.</typeparam>
    /// <param name="entity">Entity containing the property.</param>
    /// <param name="propertyName">Name of the property to validate.</param>
    /// <returns>Validation result for the property.</returns>
    ValidationResult ValidateProperty<T>(T entity, string propertyName);

    /// <summary>
    /// Validates an entity asynchronously.
    /// </summary>
    /// <typeparam name="T">Type of entity to validate.</typeparam>
    /// <param name="entity">Entity to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result containing any errors.</returns>
    Task<ValidationResult> ValidateAsync<T>(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a validator for a specific type.
    /// </summary>
    /// <typeparam name="T">Type to register validator for.</typeparam>
    /// <param name="validator">Validator instance.</param>
    void RegisterValidator<T>(IValidator<T> validator);

    /// <summary>
    /// Checks if an entity is valid.
    /// </summary>
    /// <typeparam name="T">Type of entity.</typeparam>
    /// <param name="entity">Entity to check.</param>
    /// <returns>True if entity passes validation.</returns>
    bool IsValid<T>(T entity);

    /// <summary>
    /// Validates and throws if validation fails.
    /// </summary>
    /// <typeparam name="T">Type of entity.</typeparam>
    /// <param name="entity">Entity to validate.</param>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    void ValidateAndThrow<T>(T entity);
}

/// <summary>
/// Interface for type-specific validators.
/// </summary>
/// <typeparam name="T">Type to validate.</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Validates an entity of type T.
    /// </summary>
    /// <param name="entity">Entity to validate.</param>
    /// <returns>Validation result.</returns>
    ValidationResult Validate(T entity);

    /// <summary>
    /// Validates an entity asynchronously.
    /// </summary>
    /// <param name="entity">Entity to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<ValidationResult> ValidateAsync(T entity, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    /// <summary>
    /// Collection of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Whether the validation passed (no errors).
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Adds an error to the result.
    /// </summary>
    /// <param name="error">Error to add.</param>
    public void AddError(ValidationError error)
    {
        _errors.Add(error);
    }

    /// <summary>
    /// Adds an error with the specified details.
    /// </summary>
    /// <param name="propertyName">Name of the invalid property.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <param name="severity">Error severity.</param>
    public void AddError(string propertyName, string errorMessage, string? errorCode = null,
        ValidationSeverity severity = ValidationSeverity.Error)
    {
        _errors.Add(new ValidationError(propertyName, errorMessage, errorCode, severity));
    }

    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">Other result to merge.</param>
    public void Merge(ValidationResult other)
    {
        _errors.AddRange(other.Errors);
    }

    /// <summary>
    /// Gets all error messages as a single string.
    /// </summary>
    /// <param name="separator">Separator between messages.</param>
    /// <returns>Combined error messages.</returns>
    public string GetErrorMessages(string separator = "; ")
    {
        return string.Join(separator, _errors.Select(e => e.ErrorMessage));
    }

    /// <summary>
    /// Gets errors for a specific property.
    /// </summary>
    /// <param name="propertyName">Property name.</param>
    /// <returns>Errors for the property.</returns>
    public IEnumerable<ValidationError> GetErrorsForProperty(string propertyName)
    {
        return _errors.Where(e => e.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="propertyName">Property name.</param>
    /// <param name="errorMessage">Error message.</param>
    /// <returns>Failed validation result.</returns>
    public static ValidationResult Failure(string propertyName, string errorMessage)
    {
        var result = new ValidationResult();
        result.AddError(propertyName, errorMessage);
        return result;
    }

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    /// <param name="errors">Collection of errors.</param>
    /// <returns>Failed validation result.</returns>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors)
    {
        var result = new ValidationResult();
        foreach (var error in errors)
            result.AddError(error);
        return result;
    }

    public override string ToString()
    {
        return IsValid ? "Valid" : $"Invalid: {GetErrorMessages()}";
    }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Creates a new validation error.
    /// </summary>
    public ValidationError(string propertyName, string errorMessage, string? errorCode = null,
        ValidationSeverity severity = ValidationSeverity.Error)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
        Severity = severity;
    }

    /// <summary>
    /// Name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Optional error code for programmatic handling.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Severity of the validation error.
    /// </summary>
    public ValidationSeverity Severity { get; }

    /// <summary>
    /// The attempted value that failed validation.
    /// </summary>
    public object? AttemptedValue { get; set; }

    public override string ToString() => $"{PropertyName}: {ErrorMessage}";
}

/// <summary>
/// Severity level for validation errors.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message - not an error.
    /// </summary>
    Info,

    /// <summary>
    /// Warning - validation passed but with concerns.
    /// </summary>
    Warning,

    /// <summary>
    /// Error - validation failed.
    /// </summary>
    Error
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Creates a new validation exception.
    /// </summary>
    /// <param name="validationResult">The validation result containing errors.</param>
    public ValidationException(ValidationResult validationResult)
        : base(validationResult.GetErrorMessages())
    {
        ValidationResult = validationResult;
    }

    /// <summary>
    /// Creates a new validation exception with a custom message.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="validationResult">The validation result containing errors.</param>
    public ValidationException(string message, ValidationResult validationResult)
        : base(message)
    {
        ValidationResult = validationResult;
    }

    /// <summary>
    /// The validation result containing the errors.
    /// </summary>
    public ValidationResult ValidationResult { get; }

    /// <summary>
    /// Shortcut to validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => ValidationResult.Errors;
}
