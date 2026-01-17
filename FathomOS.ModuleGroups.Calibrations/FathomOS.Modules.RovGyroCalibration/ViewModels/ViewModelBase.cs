using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace FathomOS.Modules.RovGyroCalibration.ViewModels;

#region ObservableRangeCollection

/// <summary>
/// An ObservableCollection that supports bulk operations with single notification.
/// Use AddRange() or ReplaceAll() instead of adding items one-by-one.
/// </summary>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification = false;

    public ObservableRangeCollection() : base() { }

    public ObservableRangeCollection(IEnumerable<T> collection) : base(collection) { }

    /// <summary>
    /// Adds a range of items and raises a single CollectionChanged notification.
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        try
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Clears the collection and adds new items with a single notification.
    /// Most efficient way to replace all items.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        try
        {
            Clear();
            foreach (var item in items)
            {
                Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Removes a range of items and raises a single CollectionChanged notification.
    /// </summary>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        try
        {
            foreach (var item in items)
            {
                Remove(item);
            }
        }
        finally
        {
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }
}

#endregion

#region InitializationErrorEventArgs

/// <summary>
/// Event arguments for initialization errors in ViewModels.
/// Used to pass error information to the View for MVVM-compliant error display.
/// </summary>
public class InitializationErrorEventArgs : EventArgs
{
    /// <summary>
    /// The error message to display.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Creates a new instance of InitializationErrorEventArgs.
    /// </summary>
    /// <param name="errorMessage">The error message to display.</param>
    public InitializationErrorEventArgs(string errorMessage)
    {
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
    }
}

#endregion

#region ViewModelBase

/// <summary>
/// Base class for all ViewModels providing INotifyPropertyChanged implementation
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

#endregion

#region RelayCommand

/// <summary>
/// Standard ICommand implementation for MVVM
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// Raises CanExecuteChanged to re-evaluate command availability
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CommandManager.InvalidateRequerySuggested();
    }
}

/// <summary>
/// Async-friendly ICommand implementation
/// </summary>
public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;

        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            await _execute(parameter);
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}

#endregion

#region WizardStepViewModelBase

/// <summary>
/// Base class for wizard step ViewModels
/// </summary>
public abstract class WizardStepViewModelBase : ViewModelBase
{
    private string _stepTitle = "";
    private string _stepDescription = "";
    private int _stepNumber;
    private bool _isCompleted;
    private bool _isActive;
    private string _validationMessage = "";
    private bool _hasValidationError;

    /// <summary>
    /// Reference to the main view model
    /// </summary>
    protected MainViewModel Main { get; }
    
    /// <summary>
    /// Alias for Main for compatibility
    /// </summary>
    protected MainViewModel _mainViewModel => Main;

    /// <summary>
    /// Constructor with MainViewModel reference
    /// </summary>
    protected WizardStepViewModelBase(MainViewModel main)
    {
        Main = main ?? throw new ArgumentNullException(nameof(main));
    }

    /// <summary>
    /// The step number (1-7)
    /// </summary>
    public int StepNumber
    {
        get => _stepNumber;
        set => SetProperty(ref _stepNumber, value);
    }

    /// <summary>
    /// Display title for the step
    /// </summary>
    public string StepTitle
    {
        get => _stepTitle;
        set => SetProperty(ref _stepTitle, value);
    }

    /// <summary>
    /// Description of what this step does
    /// </summary>
    public string StepDescription
    {
        get => _stepDescription;
        set => SetProperty(ref _stepDescription, value);
    }

    /// <summary>
    /// Whether this step has been completed
    /// </summary>
    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetProperty(ref _isCompleted, value);
    }

    /// <summary>
    /// Whether this is the currently active step
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    /// <summary>
    /// Validation error message to display
    /// </summary>
    public string ValidationMessage
    {
        get => _validationMessage;
        set
        {
            if (SetProperty(ref _validationMessage, value))
            {
                HasValidationError = !string.IsNullOrEmpty(value);
            }
        }
    }

    /// <summary>
    /// Whether there's a validation error
    /// </summary>
    public bool HasValidationError
    {
        get => _hasValidationError;
        private set => SetProperty(ref _hasValidationError, value);
    }

    /// <summary>
    /// Called when the step becomes active
    /// </summary>
    public virtual void OnActivated() { }

    /// <summary>
    /// Called when leaving this step
    /// </summary>
    public virtual void OnDeactivated() { }

    /// <summary>
    /// Validates the step data. Returns true if valid.
    /// </summary>
    public abstract bool Validate();

    /// <summary>
    /// Gets whether the user can proceed to the next step
    /// </summary>
    public virtual bool CanProceed 
    { 
        get 
        {
            // Always call Validate() first to reset ValidationMessage
            // Then check the result - don't short-circuit!
            bool isValid = Validate();
            return isValid && !HasValidationError;
        }
    }
}

#endregion

#region Step Information

/// <summary>
/// Static step definitions for the 7-step wizard
/// </summary>
public static class WizardSteps
{
    public static readonly (int Number, string Name, string Icon, string Description)[] Steps = 
    {
        (1, "Select", "🎯", "Choose calibration or verification mode and enter project details"),
        (2, "Import", "📥", "Load NPD data file containing heading measurements"),
        (3, "Configure", "⚙️", "Map data columns and configure processing options"),
        (4, "Process", "⚡", "Calculate C-O values and apply outlier detection"),
        (5, "Analyze", "📊", "Review results through charts and statistics"),
        (6, "Validate", "✓", "Quality control checks and acceptance decision"),
        (7, "Export", "📤", "Generate reports and export data")
    };

    public static string GetStepName(int stepNumber) => 
        Steps.FirstOrDefault(s => s.Number == stepNumber).Name ?? $"Step {stepNumber}";

    public static string GetStepIcon(int stepNumber) => 
        Steps.FirstOrDefault(s => s.Number == stepNumber).Icon ?? "•";

    public static string GetStepDescription(int stepNumber) => 
        Steps.FirstOrDefault(s => s.Number == stepNumber).Description ?? "";
}

#endregion
