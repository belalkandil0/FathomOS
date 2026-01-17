using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.ProjectManagement.ViewModels;

/// <summary>
/// Base class for all ViewModels providing INotifyPropertyChanged implementation
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for the specified property
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the property value and raises PropertyChanged if the value changed
    /// </summary>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets the property value, raises PropertyChanged, and executes callback if changed
    /// </summary>
    protected bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref storage, value, propertyName))
        {
            onChanged?.Invoke();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Raises PropertyChanged for multiple properties
    /// </summary>
    protected void OnPropertiesChanged(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    private bool _isBusy;
    /// <summary>
    /// Indicates whether the ViewModel is currently busy with an async operation
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string? _busyMessage;
    /// <summary>
    /// Message to display while busy
    /// </summary>
    public string? BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    private string? _errorMessage;
    /// <summary>
    /// Error message from the last operation
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            SetProperty(ref _errorMessage, value);
            OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>
    /// Indicates whether there is an error message
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Clears the current error message
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
    }

    /// <summary>
    /// Sets busy state with optional message
    /// </summary>
    protected void SetBusy(bool isBusy, string? message = null)
    {
        IsBusy = isBusy;
        BusyMessage = isBusy ? message : null;
    }

    /// <summary>
    /// Executes an async operation with busy indicator and error handling
    /// </summary>
    protected async Task ExecuteAsync(Func<Task> operation, string? busyMessage = null)
    {
        if (IsBusy) return;

        try
        {
            ClearError();
            SetBusy(true, busyMessage);
            await operation();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Executes an async operation with busy indicator, error handling, and return value
    /// </summary>
    protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? busyMessage = null)
    {
        if (IsBusy) return default;

        try
        {
            ClearError();
            SetBusy(true, busyMessage);
            return await operation();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return default;
        }
        finally
        {
            SetBusy(false);
        }
    }
}
