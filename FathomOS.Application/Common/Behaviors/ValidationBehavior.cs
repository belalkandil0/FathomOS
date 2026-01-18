using FathomOS.Domain.Common;
using FluentValidation;
using MediatR;

namespace FathomOS.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior that validates requests using FluentValidation.
/// Returns a failed Result if validation fails instead of throwing exceptions.
/// </summary>
/// <typeparam name="TRequest">The type of request being validated</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="validators">The validators for the request type</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
        {
            return CreateValidationResult(failures);
        }

        return await next();
    }

    private static TResponse CreateValidationResult(
        IReadOnlyList<FluentValidation.Results.ValidationFailure> failures)
    {
        var errors = failures
            .Select(f => new ValidationError(
                $"VALIDATION_{f.PropertyName.ToUpperInvariant()}",
                f.ErrorMessage,
                f.PropertyName,
                f.AttemptedValue))
            .Cast<Error>()
            .ToList();

        // Handle Result<T> responses
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>)
                .MakeGenericType(resultType)
                .GetMethod("Failure", [typeof(IReadOnlyList<Error>)]);

            return (TResponse)failureMethod!.Invoke(null, [errors])!;
        }

        // Handle non-generic Result responses
        return (TResponse)(object)Result.Failure(errors);
    }
}
