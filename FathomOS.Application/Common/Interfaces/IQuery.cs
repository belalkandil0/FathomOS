using FathomOS.Domain.Common;
using MediatR;

namespace FathomOS.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries that return a value.
/// Queries are used to read data without modifying system state.
/// </summary>
/// <typeparam name="TResponse">The type of the response value</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Handler interface for queries.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle</typeparam>
/// <typeparam name="TResponse">The type of the response value</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
