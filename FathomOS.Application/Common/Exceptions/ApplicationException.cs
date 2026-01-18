namespace FathomOS.Application.Common.Exceptions;

/// <summary>
/// Base exception for application layer errors.
/// </summary>
public abstract class ApplicationException : Exception
{
    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public abstract string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    protected ApplicationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    protected ApplicationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when validation fails in the application layer.
/// </summary>
public sealed class ValidationException : ApplicationException
{
    /// <inheritdoc />
    public override string ErrorCode => "APPLICATION_VALIDATION_FAILED";

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="errors">The validation errors grouped by property name</param>
    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="propertyName">The property name</param>
    /// <param name="errorMessage">The error message</param>
    public ValidationException(string propertyName, string errorMessage)
        : base(errorMessage)
    {
        Errors = new Dictionary<string, string[]>
        {
            [propertyName] = [errorMessage]
        };
    }
}

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public sealed class NotFoundException : ApplicationException
{
    /// <inheritdoc />
    public override string ErrorCode => "RESOURCE_NOT_FOUND";

    /// <summary>
    /// Gets the name of the resource that was not found.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets the key of the resource that was not found.
    /// </summary>
    public object Key { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource</param>
    /// <param name="key">The key used to look up the resource</param>
    public NotFoundException(string resourceName, object key)
        : base($"Resource '{resourceName}' with key '{key}' was not found.")
    {
        ResourceName = resourceName;
        Key = key;
    }

    /// <summary>
    /// Creates a NotFoundException for a specific type.
    /// </summary>
    /// <typeparam name="T">The type of the resource</typeparam>
    /// <param name="key">The key used to look up the resource</param>
    /// <returns>A new NotFoundException</returns>
    public static NotFoundException For<T>(object key) => new(typeof(T).Name, key);
}

/// <summary>
/// Exception thrown when access to a resource is forbidden.
/// </summary>
public sealed class ForbiddenAccessException : ApplicationException
{
    /// <inheritdoc />
    public override string ErrorCode => "ACCESS_FORBIDDEN";

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenAccessException"/> class.
    /// </summary>
    public ForbiddenAccessException()
        : base("Access to this resource is forbidden.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenAccessException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when a conflict occurs during an operation.
/// </summary>
public sealed class ConflictException : ApplicationException
{
    /// <inheritdoc />
    public override string ErrorCode => "RESOURCE_CONFLICT";

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    public ConflictException(string message) : base(message)
    {
    }
}
