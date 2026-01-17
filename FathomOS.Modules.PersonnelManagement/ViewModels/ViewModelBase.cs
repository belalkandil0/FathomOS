using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.PersonnelManagement.ViewModels;

/// <summary>
/// Base class for all ViewModels providing INotifyPropertyChanged implementation
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for a property
    /// </summary>
    /// <param name="propertyName">Name of the property that changed</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged if the value changed
    /// </summary>
    /// <typeparam name="T">Type of the property</typeparam>
    /// <param name="field">Reference to the backing field</param>
    /// <param name="value">New value to set</param>
    /// <param name="propertyName">Name of the property (auto-filled by compiler)</param>
    /// <returns>True if the value changed, false otherwise</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged for multiple properties
    /// </summary>
    /// <typeparam name="T">Type of the property</typeparam>
    /// <param name="field">Reference to the backing field</param>
    /// <param name="value">New value to set</param>
    /// <param name="propertyName">Name of the property</param>
    /// <param name="additionalPropertyNames">Additional property names to notify</param>
    /// <returns>True if the value changed, false otherwise</returns>
    protected bool SetProperty<T>(ref T field, T value, string propertyName, params string[] additionalPropertyNames)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);

        foreach (var additionalPropertyName in additionalPropertyNames)
        {
            OnPropertyChanged(additionalPropertyName);
        }

        return true;
    }

    /// <summary>
    /// Raises PropertyChanged for multiple properties at once
    /// </summary>
    /// <param name="propertyNames">Names of the properties that changed</param>
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
    /// Error message to display to the user
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Clears any error message
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
    }

    /// <summary>
    /// Sets the busy state with an optional message
    /// </summary>
    /// <param name="message">Message to display while busy</param>
    protected void SetBusy(string? message = null)
    {
        BusyMessage = message;
        IsBusy = true;
    }

    /// <summary>
    /// Clears the busy state
    /// </summary>
    protected void ClearBusy()
    {
        IsBusy = false;
        BusyMessage = null;
    }

    /// <summary>
    /// Executes an action while showing the busy indicator
    /// </summary>
    /// <param name="action">Action to execute</param>
    /// <param name="message">Message to display while busy</param>
    protected async Task ExecuteWithBusyIndicatorAsync(Func<Task> action, string? message = null)
    {
        try
        {
            SetBusy(message);
            ClearError();
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            ClearBusy();
        }
    }

    /// <summary>
    /// Executes an action while showing the busy indicator and returns a result
    /// </summary>
    /// <typeparam name="TResult">Type of the result</typeparam>
    /// <param name="action">Action to execute</param>
    /// <param name="message">Message to display while busy</param>
    /// <returns>Result of the action</returns>
    protected async Task<TResult?> ExecuteWithBusyIndicatorAsync<TResult>(Func<Task<TResult>> action, string? message = null)
    {
        try
        {
            SetBusy(message);
            ClearError();
            return await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return default;
        }
        finally
        {
            ClearBusy();
        }
    }
}
