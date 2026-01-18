using FathomOS.Application.Common.Interfaces;
using FathomOS.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FathomOS.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that logs request execution details.
/// Logs request name, user, duration, and result status.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="currentUserService">The current user service</param>
    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId ?? "Anonymous";
        var userName = _currentUserService.UserName ?? "Anonymous";

        _logger.LogInformation(
            "FathomOS Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName, userId, userName, request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            var isSuccess = response switch
            {
                Result result => result.IsSuccess,
                _ => true
            };

            if (isSuccess)
            {
                _logger.LogInformation(
                    "FathomOS Request Completed: {Name} completed successfully in {ElapsedMilliseconds}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "FathomOS Request Failed: {Name} completed with failure in {ElapsedMilliseconds}ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "FathomOS Request Error: {Name} failed after {ElapsedMilliseconds}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
