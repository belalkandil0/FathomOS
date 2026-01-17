// ============================================================================
// Fathom OS - Core Library
// File: Mvvm/RelayCommand.cs
// Purpose: ICommand implementations for MVVM command binding
// ============================================================================

using System.Windows.Input;

namespace FathomOS.Core.Mvvm;

/// <summary>
/// A command that relays its functionality to delegates.
/// </summary>
/// <remarks>
/// <para>
/// This implementation integrates with the WPF <see cref="CommandManager"/> for automatic
/// CanExecute re-evaluation when the UI state changes.
/// </para>
/// <para>
/// The command is thread-safe for CanExecuteChanged event subscription.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple command
/// SaveCommand = new RelayCommand(() => Save(), () => CanSave);
///
/// // Command with parameter
/// DeleteCommand = new RelayCommand(param => Delete(param), param => param != null);
/// </code>
/// </example>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class that can always execute.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public RelayCommand(Action<object?> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic, or <c>null</c> to always allow execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class without a parameter.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public RelayCommand(Action execute)
        : this(_ => execute(), null)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand"/> class without a parameter.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic, or <c>null</c> to always allow execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public RelayCommand(Action execute, Func<bool>? canExecute)
        : this(_ => execute(), canExecute == null ? null : _ => canExecute())
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    /// <remarks>
    /// This event is implemented using the WPF <see cref="CommandManager.RequerySuggested"/> event,
    /// which automatically triggers re-evaluation of CanExecute when the UI state changes.
    /// </remarks>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the command can be executed; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to indicate that the result of
    /// <see cref="CanExecute"/> may have changed.
    /// </summary>
    /// <remarks>
    /// This method uses <see cref="CommandManager.InvalidateRequerySuggested"/> to trigger
    /// re-evaluation of all commands that use the CommandManager.
    /// </remarks>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// A generic command that relays its functionality to delegates with a typed parameter.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
/// <remarks>
/// <para>
/// This implementation provides type-safe command parameters without requiring explicit casting.
/// </para>
/// <para>
/// For value types, a <c>null</c> parameter is converted to <c>default(T)</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// DeleteItemCommand = new RelayCommand&lt;Item&gt;(
///     item => DeleteItem(item),
///     item => item != null &amp;&amp; item.CanDelete);
/// </code>
/// </example>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class that can always execute.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public RelayCommand(Action<T?> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand{T}"/> class.
    /// </summary>
    /// <param name="execute">The execution logic.</param>
    /// <param name="canExecute">The execution status logic, or <c>null</c> to always allow execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the command can be executed; otherwise, <c>false</c>.</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(ConvertParameter(parameter));
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    public void Execute(object? parameter)
    {
        _execute(ConvertParameter(parameter));
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to indicate that the result of
    /// <see cref="CanExecute"/> may have changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Converts the parameter to the expected type.
    /// </summary>
    private static T? ConvertParameter(object? parameter)
    {
        if (parameter == null)
        {
            return default;
        }

        if (parameter is T typedParameter)
        {
            return typedParameter;
        }

        // Attempt conversion for compatible types
        try
        {
            return (T)Convert.ChangeType(parameter, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}

/// <summary>
/// An asynchronous command that relays its functionality to async delegates.
/// </summary>
/// <remarks>
/// <para>
/// This command prevents concurrent execution - while an async operation is in progress,
/// <see cref="CanExecute"/> returns <c>false</c>.
/// </para>
/// <para>
/// The command supports cancellation through <see cref="Cancel"/> method and can optionally
/// track the cancellation token for cooperative cancellation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// LoadDataCommand = new AsyncRelayCommand(
///     async ct => await LoadDataAsync(ct),
///     () => !IsLoading);
///
/// // In the ViewModel:
/// public void OnNavigatedFrom()
/// {
///     LoadDataCommand.Cancel();
/// }
/// </code>
/// </example>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isExecuting;
    private readonly object _syncLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class that can always execute.
    /// </summary>
    /// <param name="execute">The asynchronous execution logic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public AsyncRelayCommand(Func<CancellationToken, Task> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class.
    /// </summary>
    /// <param name="execute">The asynchronous execution logic.</param>
    /// <param name="canExecute">The execution status logic, or <c>null</c> to always allow execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class without cancellation support.
    /// </summary>
    /// <param name="execute">The asynchronous execution logic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public AsyncRelayCommand(Func<Task> execute)
        : this(_ => execute(), null)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand"/> class without cancellation support.
    /// </summary>
    /// <param name="execute">The asynchronous execution logic.</param>
    /// <param name="canExecute">The execution status logic, or <c>null</c> to always allow execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute)
        : this(_ => execute(), canExecute)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Gets a value indicating whether the command is currently executing.
    /// </summary>
    public bool IsExecuting => _isExecuting;

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the command can be executed (not currently executing and CanExecute delegate returns true);
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute == null || _canExecute());
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    /// <remarks>
    /// If the command is already executing, this method returns immediately without starting another execution.
    /// The method signature is <c>async void</c> to comply with <see cref="ICommand"/> interface,
    /// but exceptions are not swallowed - they will propagate to the synchronization context.
    /// </remarks>
    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    /// <summary>
    /// Executes the command asynchronously and returns a task that completes when the operation finishes.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Use this method instead of <see cref="Execute"/> when you need to await the completion
    /// of the command or handle exceptions.
    /// </remarks>
    public async Task ExecuteAsync(object? parameter)
    {
        if (_isExecuting) return;

        CancellationTokenSource? cts;
        lock (_syncLock)
        {
            if (_isExecuting) return;
            _isExecuting = true;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            cts = _cancellationTokenSource;
        }

        RaiseCanExecuteChanged();

        try
        {
            await _execute(cts.Token).ConfigureAwait(false);
        }
        finally
        {
            lock (_syncLock)
            {
                _isExecuting = false;
            }
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Cancels the currently executing operation, if any.
    /// </summary>
    /// <remarks>
    /// This method is safe to call even if no operation is currently executing.
    /// The operation must cooperatively check the cancellation token for this to have effect.
    /// </remarks>
    public void Cancel()
    {
        lock (_syncLock)
        {
            _cancellationTokenSource?.Cancel();
        }
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to indicate that the result of
    /// <see cref="CanExecute"/> may have changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// A generic asynchronous command that relays its functionality to async delegates with a typed parameter.
/// </summary>
/// <typeparam name="T">The type of the command parameter.</typeparam>
/// <remarks>
/// <para>
/// This command provides type-safe parameters for asynchronous operations and prevents concurrent execution.
/// </para>
/// <para>
/// For value types, a <c>null</c> parameter is converted to <c>default(T)</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// ProcessItemCommand = new AsyncRelayCommand&lt;Item&gt;(
///     async (item, ct) => await ProcessItemAsync(item, ct),
///     item => item != null);
/// </code>
/// </example>
public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, CancellationToken, Task> _execute;
    private readonly Predicate<T?>? _canExecute;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isExecuting;
    private readonly object _syncLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand{T}"/> class that can always execute.
    /// </summary>
    /// <param name="execute">The asynchronous execution logic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public AsyncRelayCommand(Func<T?, CancellationToken, Task> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand{T}"/> class.
    /// </summary>
    /// <param name="execute">The asynchronous execution logic.</param>
    /// <param name="canExecute">The execution status logic, or <c>null</c> to always allow execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public AsyncRelayCommand(Func<T?, CancellationToken, Task> execute, Predicate<T?>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand{T}"/> class without cancellation support.
    /// </summary>
    /// <param name="execute">The asynchronous execution logic.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public AsyncRelayCommand(Func<T?, Task> execute)
        : this((param, _) => execute(param), null)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncRelayCommand{T}"/> class without cancellation support.
    /// </summary>
    /// <param name="execute">The asynchronous execution logic.</param>
    /// <param name="canExecute">The execution status logic, or <c>null</c> to always allow execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="execute"/> is <c>null</c>.</exception>
    public AsyncRelayCommand(Func<T?, Task> execute, Predicate<T?>? canExecute)
        : this((param, _) => execute(param), canExecute)
    {
        if (execute == null) throw new ArgumentNullException(nameof(execute));
    }

    /// <summary>
    /// Occurs when changes occur that affect whether the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Gets a value indicating whether the command is currently executing.
    /// </summary>
    public bool IsExecuting => _isExecuting;

    /// <summary>
    /// Determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the command can be executed (not currently executing and CanExecute delegate returns true);
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute == null || _canExecute(ConvertParameter(parameter)));
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    public async void Execute(object? parameter)
    {
        await ExecuteAsync(parameter);
    }

    /// <summary>
    /// Executes the command asynchronously and returns a task that completes when the operation finishes.
    /// </summary>
    /// <param name="parameter">
    /// Data used by the command. If the command does not require data, this can be <c>null</c>.
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(object? parameter)
    {
        if (_isExecuting) return;

        CancellationTokenSource? cts;
        lock (_syncLock)
        {
            if (_isExecuting) return;
            _isExecuting = true;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            cts = _cancellationTokenSource;
        }

        RaiseCanExecuteChanged();

        try
        {
            await _execute(ConvertParameter(parameter), cts.Token).ConfigureAwait(false);
        }
        finally
        {
            lock (_syncLock)
            {
                _isExecuting = false;
            }
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Cancels the currently executing operation, if any.
    /// </summary>
    public void Cancel()
    {
        lock (_syncLock)
        {
            _cancellationTokenSource?.Cancel();
        }
    }

    /// <summary>
    /// Raises the <see cref="CanExecuteChanged"/> event to indicate that the result of
    /// <see cref="CanExecute"/> may have changed.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }

    /// <summary>
    /// Converts the parameter to the expected type.
    /// </summary>
    private static T? ConvertParameter(object? parameter)
    {
        if (parameter == null)
        {
            return default;
        }

        if (parameter is T typedParameter)
        {
            return typedParameter;
        }

        // Attempt conversion for compatible types
        try
        {
            return (T)Convert.ChangeType(parameter, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
