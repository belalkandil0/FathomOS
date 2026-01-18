using FathomOS.Domain.Common;
using MediatR;

namespace FathomOS.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that do not return a value.
/// Commands represent intent to change the state of the system.
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Marker interface for commands that return a value.
/// Commands represent intent to change the state of the system.
/// </summary>
/// <typeparam name="TResponse">The type of the response value</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Handler interface for commands that do not return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Handler interface for commands that return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
/// <typeparam name="TResponse">The type of the response value</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
