using System.Windows.Input;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// A command that relays its functionality to delegates
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Creates a new command that can always execute
    /// </summary>
    /// <param name="execute">The execution logic</param>
    public RelayCommand(Action<object?> execute) : this(execute, null)
    {
    }

    /// <summary>
    /// Creates a new command with conditional execution
    /// </summary>
    /// <param name="execute">The execution logic</param>
    /// <param name="canExecute">The execution status logic</param>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Creates a new command that can always execute (parameterless)
    /// </summary>
    /// <param name="execute">The execution logic</param>
    public RelayCommand(Action execute) : this(_ => execute(), null)
    {
    }

    /// <summary>
    /// Creates a new command with conditional execution (parameterless)
    /// </summary>
    /// <param name="execute">The execution logic</param>
    /// <param name="canExecute">The execution status logic</param>
    public RelayCommand(Action execute, Func<bool> canExecute)
        : this(_ => execute(), _ => canExecute())
    {
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }

    /// <summary>
    /// Raises the CanExecuteChanged event to indicate that the command's
    /// ability to execute has changed
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// A command that relays its functionality to async delegates
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Creates a new async command that can always execute
    /// </summary>
    /// <param name="execute">The async execution logic</param>
    public AsyncRelayCommand(Func<object?, Task> execute) : this(execute, null)
    {
    }

    /// <summary>
    /// Creates a new async command with conditional execution
    /// </summary>
    /// <param name="execute">The async execution logic</param>
    /// <param name="canExecute">The execution status logic</param>
    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// Creates a new async command that can always execute (parameterless)
    /// </summary>
    /// <param name="execute">The async execution logic</param>
    public AsyncRelayCommand(Func<Task> execute) : this(_ => execute(), null)
    {
    }

    /// <summary>
    /// Creates a new async command with conditional execution (parameterless)
    /// </summary>
    /// <param name="execute">The async execution logic</param>
    /// <param name="canExecute">The execution status logic</param>
    public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
        : this(_ => execute(), _ => canExecute())
    {
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute(parameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Raises the CanExecuteChanged event to indicate that the command's
    /// ability to execute has changed
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// A generic command that relays its functionality to delegates
/// </summary>
/// <typeparam name="T">The type of the command parameter</typeparam>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;
    private readonly Func<T?, bool>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Creates a new command that can always execute
    /// </summary>
    /// <param name="execute">The execution logic</param>
    public RelayCommand(Action<T?> execute) : this(execute, null)
    {
    }

    /// <summary>
    /// Creates a new command with conditional execution
    /// </summary>
    /// <param name="execute">The execution logic</param>
    /// <param name="canExecute">The execution status logic</param>
    public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (parameter is T typedParameter)
            return _canExecute?.Invoke(typedParameter) ?? true;

        if (parameter == null && default(T) == null)
            return _canExecute?.Invoke(default) ?? true;

        return false;
    }

    public void Execute(object? parameter)
    {
        if (parameter is T typedParameter)
            _execute(typedParameter);
        else if (parameter == null && default(T) == null)
            _execute(default);
    }

    /// <summary>
    /// Raises the CanExecuteChanged event
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// A generic async command that relays its functionality to delegates
/// </summary>
/// <typeparam name="T">The type of the command parameter</typeparam>
public class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T?, Task> _execute;
    private readonly Func<T?, bool>? _canExecute;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Creates a new async command that can always execute
    /// </summary>
    /// <param name="execute">The async execution logic</param>
    public AsyncRelayCommand(Func<T?, Task> execute) : this(execute, null)
    {
    }

    /// <summary>
    /// Creates a new async command with conditional execution
    /// </summary>
    /// <param name="execute">The async execution logic</param>
    /// <param name="canExecute">The execution status logic</param>
    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (_isExecuting)
            return false;

        if (parameter is T typedParameter)
            return _canExecute?.Invoke(typedParameter) ?? true;

        if (parameter == null && default(T) == null)
            return _canExecute?.Invoke(default) ?? true;

        return false;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();

            if (parameter is T typedParameter)
                await _execute(typedParameter);
            else if (parameter == null && default(T) == null)
                await _execute(default);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Raises the CanExecuteChanged event
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}
