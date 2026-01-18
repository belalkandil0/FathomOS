using FathomOS.Application.Common.Interfaces;
using FathomOS.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FathomOS.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that wraps command execution in a database transaction.
/// Only applies to commands (not queries) to ensure data consistency.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
    where TResponse : Result
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="logger">The logger instance</param>
    public TransactionBehavior(
        IUnitOfWork unitOfWork,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        if (_unitOfWork.HasActiveTransaction)
        {
            return await next();
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            _logger.LogDebug("Beginning transaction for {RequestName}", requestName);

            var response = await next();

            if (response.IsSuccess)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                _logger.LogDebug("Committed transaction for {RequestName}", requestName);
            }
            else
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogDebug("Rolled back transaction for {RequestName} due to failure result", requestName);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during transaction for {RequestName}", requestName);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
