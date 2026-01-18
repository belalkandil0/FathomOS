using FathomOS.Application.Common.Interfaces;
using FathomOS.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FathomOS.Application.Common.Behaviors;

/// <summary>
/// Attribute to specify required roles for a request.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the required roles (comma-separated).
    /// </summary>
    public string Roles { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the required policy name.
    /// </summary>
    public string Policy { get; set; } = string.Empty;
}

/// <summary>
/// Pipeline behavior that enforces authorization requirements on requests.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="currentUserService">The current user service</param>
    /// <param name="logger">The logger instance</param>
    public AuthorizationBehavior(
        ICurrentUserService currentUserService,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var authorizeAttributes = typeof(TRequest)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .ToList();

        if (authorizeAttributes.Count == 0)
        {
            return await next();
        }

        // Check if user is authenticated
        if (!_currentUserService.IsAuthenticated)
        {
            _logger.LogWarning(
                "Unauthorized access attempt to {RequestName}",
                typeof(TRequest).Name);

            return CreateUnauthorizedResult("User is not authenticated.");
        }

        // Check role-based authorization
        foreach (var authorizeAttribute in authorizeAttributes)
        {
            if (!string.IsNullOrWhiteSpace(authorizeAttribute.Roles))
            {
                var requiredRoles = authorizeAttribute.Roles
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .ToList();

                var hasRequiredRole = requiredRoles.Any(role => _currentUserService.IsInRole(role));

                if (!hasRequiredRole)
                {
                    _logger.LogWarning(
                        "User {UserId} lacks required roles {Roles} for {RequestName}",
                        _currentUserService.UserId,
                        authorizeAttribute.Roles,
                        typeof(TRequest).Name);

                    return CreateUnauthorizedResult(
                        $"User does not have required role(s): {authorizeAttribute.Roles}");
                }
            }
        }

        return await next();
    }

    private static TResponse CreateUnauthorizedResult(string message)
    {
        var error = Error.Unauthorized("UNAUTHORIZED", message);

        // Handle Result<T> responses
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(resultType)
                .GetMethod("Failure", [typeof(Error)]);

            return (TResponse)failureMethod!.Invoke(null, [error])!;
        }

        // Handle non-generic Result responses
        return (TResponse)(object)Result.Failure(error);
    }
}
