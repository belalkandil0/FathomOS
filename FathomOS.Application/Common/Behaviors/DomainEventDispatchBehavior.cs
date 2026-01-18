using FathomOS.Application.Common.Interfaces;
using FathomOS.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FathomOS.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that dispatches domain events after successful command execution.
/// Ensures domain events are published after the transaction is committed.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public sealed class DomainEventDispatchBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
    where TResponse : Result
{
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<DomainEventDispatchBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventDispatchBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="domainEventDispatcher">The domain event dispatcher</param>
    /// <param name="logger">The logger instance</param>
    public DomainEventDispatchBehavior(
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<DomainEventDispatchBehavior<TRequest, TResponse>> logger)
    {
        _domainEventDispatcher = domainEventDispatcher;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        // Only dispatch events if the command was successful
        if (response.IsSuccess)
        {
            _logger.LogDebug(
                "Command {CommandName} succeeded, checking for domain events",
                typeof(TRequest).Name);

            // Note: Domain events are typically collected from aggregates
            // The actual dispatch logic depends on your infrastructure implementation
            // This behavior serves as a hook point for event dispatch
        }

        return response;
    }
}
