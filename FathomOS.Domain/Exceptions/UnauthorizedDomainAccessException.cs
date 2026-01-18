namespace FathomOS.Domain.Exceptions;

/// <summary>
/// Exception thrown when an operation is attempted without proper authorization within the domain.
/// </summary>
public sealed class UnauthorizedDomainAccessException : DomainException
{
    /// <inheritdoc />
    public override string ErrorCode => "UNAUTHORIZED_DOMAIN_ACCESS";

    /// <summary>
    /// Gets the resource that access was denied to.
    /// </summary>
    public string? Resource { get; }

    /// <summary>
    /// Gets the operation that was attempted.
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Gets the user or identity that attempted the operation.
    /// </summary>
    public string? UserId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedDomainAccessException"/> class.
    /// </summary>
    /// <param name="message">The error message</param>
    public UnauthorizedDomainAccessException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedDomainAccessException"/> class.
    /// </summary>
    /// <param name="resource">The resource that access was denied to</param>
    /// <param name="operation">The operation that was attempted</param>
    public UnauthorizedDomainAccessException(string resource, string operation)
        : base($"Access denied to resource '{resource}' for operation '{operation}'.",
            new Dictionary<string, object>
            {
                ["Resource"] = resource,
                ["Operation"] = operation
            })
    {
        Resource = resource;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedDomainAccessException"/> class.
    /// </summary>
    /// <param name="resource">The resource that access was denied to</param>
    /// <param name="operation">The operation that was attempted</param>
    /// <param name="userId">The user or identity that attempted the operation</param>
    public UnauthorizedDomainAccessException(string resource, string operation, string userId)
        : base($"User '{userId}' is not authorized to perform '{operation}' on resource '{resource}'.",
            new Dictionary<string, object>
            {
                ["Resource"] = resource,
                ["Operation"] = operation,
                ["UserId"] = userId
            })
    {
        Resource = resource;
        Operation = operation;
        UserId = userId;
    }

    /// <summary>
    /// Creates an exception for insufficient permissions.
    /// </summary>
    /// <param name="requiredPermission">The required permission</param>
    /// <returns>A new UnauthorizedDomainAccessException</returns>
    public static UnauthorizedDomainAccessException InsufficientPermissions(string requiredPermission)
    {
        return new UnauthorizedDomainAccessException(
            $"Insufficient permissions. Required permission: '{requiredPermission}'.");
    }

    /// <summary>
    /// Creates an exception for license restriction.
    /// </summary>
    /// <param name="feature">The restricted feature</param>
    /// <returns>A new UnauthorizedDomainAccessException</returns>
    public static UnauthorizedDomainAccessException LicenseRestriction(string feature)
    {
        return new UnauthorizedDomainAccessException(
            $"Access to '{feature}' is restricted by the current license.");
    }
}
