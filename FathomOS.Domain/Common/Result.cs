namespace FathomOS.Domain.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// This is a discriminated union that enforces explicit error handling.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the result represents a success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result represents a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error if the result is a failure; otherwise, Error.None.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Gets a collection of all errors (for aggregated validation results).
    /// </summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the Result class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded</param>
    /// <param name="error">The error if the operation failed</param>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
        Errors = error == Error.None ? [] : [error];
    }

    /// <summary>
    /// Initializes a new instance with multiple errors (for aggregated validation).
    /// </summary>
    /// <param name="errors">The collection of errors</param>
    protected Result(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
            throw new InvalidOperationException("A failed result must have at least one error.");

        IsSuccess = false;
        Error = errors[0];
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful Result instance</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error that caused the failure</param>
    /// <returns>A failed Result instance</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    /// <param name="errors">The errors that caused the failure</param>
    /// <returns>A failed Result instance</returns>
    public static Result Failure(IReadOnlyList<Error> errors) => new(errors);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="value">The value</param>
    /// <returns>A successful Result instance containing the value</returns>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <typeparam name="TValue">The type of the expected value</typeparam>
    /// <param name="error">The error that caused the failure</param>
    /// <returns>A failed Result instance</returns>
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);

    /// <summary>
    /// Creates a result from a nullable value.
    /// Returns Success if value is not null, otherwise returns Failure with the provided error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <param name="value">The nullable value</param>
    /// <param name="error">The error to use if value is null</param>
    /// <returns>A Result instance</returns>
    public static Result<TValue> Create<TValue>(TValue? value, Error error)
        where TValue : class
    {
        return value is null ? Result<TValue>.Failure(error) : Result<TValue>.Success(value);
    }

    /// <summary>
    /// Combines multiple results into a single result.
    /// If all results are successful, returns Success.
    /// If any result is a failure, returns Failure with all errors aggregated.
    /// </summary>
    /// <param name="results">The results to combine</param>
    /// <returns>A combined Result</returns>
    public static Result Combine(params Result[] results)
    {
        var errors = results
            .Where(r => r.IsFailure)
            .SelectMany(r => r.Errors)
            .ToList();

        return errors.Count == 0 ? Success() : Failure(errors);
    }
}

/// <summary>
/// Represents the result of an operation that returns a value on success.
/// </summary>
/// <typeparam name="TValue">The type of the value on success</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>
    /// Gets the value if the result is successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result. Check IsSuccess first.");

    /// <summary>
    /// Initializes a new successful result with a value.
    /// </summary>
    /// <param name="value">The value</param>
    private Result(TValue value) : base(true, Error.None)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a new failed result with an error.
    /// </summary>
    /// <param name="error">The error</param>
    private Result(Error error) : base(false, error)
    {
        _value = default;
    }

    /// <summary>
    /// Initializes a new failed result with multiple errors.
    /// </summary>
    /// <param name="errors">The errors</param>
    private Result(IReadOnlyList<Error> errors) : base(errors)
    {
        _value = default;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>A successful Result instance</returns>
    public static Result<TValue> Success(TValue value) => new(value);

    /// <summary>
    /// Creates a failed result with an error.
    /// </summary>
    /// <param name="error">The error</param>
    /// <returns>A failed Result instance</returns>
    public new static Result<TValue> Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    /// <param name="errors">The errors</param>
    /// <returns>A failed Result instance</returns>
    public new static Result<TValue> Failure(IReadOnlyList<Error> errors) => new(errors);

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// </summary>
    public static implicit operator Result<TValue>(Error error) => Failure(error);

    /// <summary>
    /// Gets the value if successful, or the default value if failed.
    /// </summary>
    /// <param name="defaultValue">The default value to return on failure</param>
    /// <returns>The value or default</returns>
    public TValue GetValueOrDefault(TValue defaultValue = default!)
    {
        return IsSuccess ? _value! : defaultValue;
    }

    /// <summary>
    /// Maps the value to a new type if successful.
    /// </summary>
    /// <typeparam name="TNewValue">The new value type</typeparam>
    /// <param name="mapper">The mapping function</param>
    /// <returns>A new result with the mapped value</returns>
    public Result<TNewValue> Map<TNewValue>(Func<TValue, TNewValue> mapper)
    {
        return IsSuccess
            ? Result<TNewValue>.Success(mapper(_value!))
            : Result<TNewValue>.Failure(Error);
    }

    /// <summary>
    /// Binds the value to a new result if successful.
    /// </summary>
    /// <typeparam name="TNewValue">The new value type</typeparam>
    /// <param name="binder">The binding function</param>
    /// <returns>The result of the binding function</returns>
    public Result<TNewValue> Bind<TNewValue>(Func<TValue, Result<TNewValue>> binder)
    {
        return IsSuccess ? binder(_value!) : Result<TNewValue>.Failure(Error);
    }

    /// <summary>
    /// Executes an action on the value if successful.
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>This result instance</returns>
    public Result<TValue> Tap(Action<TValue> action)
    {
        if (IsSuccess)
            action(_value!);
        return this;
    }

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    /// <typeparam name="TResult">The return type</typeparam>
    /// <param name="onSuccess">Function to execute on success</param>
    /// <param name="onFailure">Function to execute on failure</param>
    /// <returns>The result of the matched function</returns>
    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(_value!) : onFailure(Error);
    }

    /// <summary>
    /// Converts this result to a non-generic result, discarding the value.
    /// </summary>
    /// <returns>A non-generic Result</returns>
    public Result ToResult()
    {
        return IsSuccess ? Result.Success() : Result.Failure(Error);
    }
}

/// <summary>
/// Extension methods for Result types.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a task of a value to a task of a result.
    /// </summary>
    public static async Task<Result<T>> ToResultAsync<T>(
        this Task<T?> task,
        Error errorIfNull)
        where T : class
    {
        var value = await task;
        return value is null ? Result<T>.Failure(errorIfNull) : Result<T>.Success(value);
    }

    /// <summary>
    /// Maps the value asynchronously if successful.
    /// </summary>
    public static async Task<Result<TNew>> MapAsync<T, TNew>(
        this Result<T> result,
        Func<T, Task<TNew>> mapper)
    {
        if (result.IsFailure)
            return Result<TNew>.Failure(result.Error);

        var newValue = await mapper(result.Value);
        return Result<TNew>.Success(newValue);
    }

    /// <summary>
    /// Binds the value asynchronously if successful.
    /// </summary>
    public static async Task<Result<TNew>> BindAsync<T, TNew>(
        this Result<T> result,
        Func<T, Task<Result<TNew>>> binder)
    {
        return result.IsFailure
            ? Result<TNew>.Failure(result.Error)
            : await binder(result.Value);
    }

    /// <summary>
    /// Ensures a condition is met on the value.
    /// </summary>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error)
    {
        if (result.IsFailure)
            return result;

        return predicate(result.Value)
            ? result
            : Result<T>.Failure(error);
    }
}
