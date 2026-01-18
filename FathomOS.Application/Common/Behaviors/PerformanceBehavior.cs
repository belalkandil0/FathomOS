using FathomOS.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FathomOS.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that monitors request performance.
/// Logs warnings for requests that exceed the configured threshold.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// The threshold in milliseconds after which a warning is logged.
    /// </summary>
    private const int WarningThresholdMs = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="currentUserService">The current user service</param>
    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _timer = new Stopwatch();
        _logger = logger;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > WarningThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId ?? "Anonymous";
            var userName = _currentUserService.UserName ?? "Anonymous";

            _logger.LogWarning(
                "FathomOS Long Running Request: {Name} ({ElapsedMilliseconds}ms) {@UserId} {@UserName} {@Request}",
                requestName, elapsedMilliseconds, userId, userName, request);
        }

        return response;
    }
}
