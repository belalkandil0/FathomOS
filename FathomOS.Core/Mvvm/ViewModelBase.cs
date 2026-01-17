// ============================================================================
// Fathom OS - Core Library
// File: Mvvm/ViewModelBase.cs
// Purpose: Base class for ViewModels with INotifyPropertyChanged and IDisposable support
// ============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Core.Mvvm;

/// <summary>
/// Base class for all ViewModels providing property change notification, busy state management,
/// error handling, and disposable resource management.
/// </summary>
/// <remarks>
/// <para>
/// This class extends <see cref="ObservableObject"/> with additional functionality commonly
/// needed in ViewModels:
/// </para>
/// <list type="bullet">
/// <item><description>Busy state management with <see cref="IsBusy"/> and <see cref="BusyMessage"/></description></item>
/// <item><description>Error handling with <see cref="ErrorMessage"/> and <see cref="HasError"/></description></item>
/// <item><description>Async operation helpers with automatic busy/error state management</description></item>
/// <item><description>IDisposable implementation with virtual <see cref="Dispose(bool)"/> method</description></item>
/// </list>
/// <para>
/// The implementation is thread-safe for property change notifications.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MainViewModel : ViewModelBase
/// {
///     public async Task LoadDataAsync()
///     {
///         await ExecuteAsync(async ct =>
///         {
///             var data = await _dataService.LoadAsync(ct);
///             Items = new ObservableCollection&lt;Item&gt;(data);
///         }, "Loading data...");
///     }
///
///     protected override void Dispose(bool disposing)
///     {
///         if (disposing)
///         {
///             _dataService?.Dispose();
///         }
///         base.Dispose(disposing);
///     }
/// }
/// </code>
/// </example>
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private bool _disposed;
    private bool _isBusy;
    private string? _busyMessage;
    private string? _errorMessage;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly object _syncLock = new();

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is currently busy with an operation.
    /// </summary>
    /// <value><c>true</c> if the ViewModel is busy; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// This property is typically bound to a loading indicator in the UI.
    /// It is automatically set by the <see cref="ExecuteAsync(Func{CancellationToken, Task}, string?)"/> methods.
    /// </remarks>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the ViewModel is not busy.
    /// </summary>
    /// <value><c>true</c> if the ViewModel is not busy; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// This is the inverse of <see cref="IsBusy"/> and is useful for binding
    /// to UI elements that should be enabled when not busy.
    /// </remarks>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Gets or sets the message to display while the ViewModel is busy.
    /// </summary>
    /// <value>The busy message, or <c>null</c> if no message is set.</value>
    /// <remarks>
    /// This property is typically bound to a text element in a loading overlay.
    /// </remarks>
    public string? BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    /// <summary>
    /// Gets or sets the error message from the last failed operation.
    /// </summary>
    /// <value>The error message, or <c>null</c> if no error occurred.</value>
    /// <remarks>
    /// Setting this property also updates <see cref="HasError"/>.
    /// </remarks>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether there is a current error message.
    /// </summary>
    /// <value><c>true</c> if <see cref="ErrorMessage"/> is not null or empty; otherwise, <c>false</c>.</value>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    /// <value><c>true</c> if this instance has been disposed; otherwise, <c>false</c>.</value>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Clears the current error message.
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
    }

    /// <summary>
    /// Sets the busy state with an optional message.
    /// </summary>
    /// <param name="isBusy">If set to <c>true</c>, marks the ViewModel as busy.</param>
    /// <param name="message">The message to display while busy, or <c>null</c> to clear the message.</param>
    protected void SetBusy(bool isBusy, string? message = null)
    {
        IsBusy = isBusy;
        BusyMessage = isBusy ? message : null;
    }

    /// <summary>
    /// Cancels the current async operation if one is in progress.
    /// </summary>
    /// <remarks>
    /// This method is safe to call even if no operation is in progress.
    /// A new <see cref="CancellationTokenSource"/> will be created for the next operation.
    /// </remarks>
    public void CancelCurrentOperation()
    {
        lock (_syncLock)
        {
            _cancellationTokenSource?.Cancel();
        }
    }

    /// <summary>
    /// Executes an asynchronous operation with automatic busy state and error handling.
    /// </summary>
    /// <param name="operation">
    /// The asynchronous operation to execute. The operation receives a <see cref="CancellationToken"/>
    /// that is cancelled when <see cref="CancelCurrentOperation"/> is called or when this
    /// ViewModel is disposed.
    /// </param>
    /// <param name="busyMessage">The message to display while the operation is running.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method automatically:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Sets <see cref="IsBusy"/> to true and <see cref="BusyMessage"/> before starting</description></item>
    /// <item><description>Clears any previous <see cref="ErrorMessage"/></description></item>
    /// <item><description>Catches exceptions and sets <see cref="ErrorMessage"/></description></item>
    /// <item><description>Resets <see cref="IsBusy"/> to false when complete</description></item>
    /// </list>
    /// <para>
    /// If the ViewModel is already busy, this method returns immediately without executing the operation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await ExecuteAsync(async ct =>
    /// {
    ///     var result = await _service.FetchDataAsync(ct);
    ///     Data = result;
    /// }, "Loading...");
    /// </code>
    /// </example>
    protected async Task ExecuteAsync(Func<CancellationToken, Task> operation, string? busyMessage = null)
    {
        if (IsBusy) return;
        ThrowIfDisposed();

        CancellationTokenSource? cts;
        lock (_syncLock)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            cts = _cancellationTokenSource;
        }

        try
        {
            ClearError();
            SetBusy(true, busyMessage);
            await operation(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled - this is expected, don't set an error
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
    /// Executes an asynchronous operation with automatic busy state and error handling,
    /// returning a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="operation">
    /// The asynchronous operation to execute. The operation receives a <see cref="CancellationToken"/>
    /// that is cancelled when <see cref="CancelCurrentOperation"/> is called or when this
    /// ViewModel is disposed.
    /// </param>
    /// <param name="busyMessage">The message to display while the operation is running.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the result of the operation
    /// or <c>default(T)</c> if the operation failed or was cancelled.
    /// </returns>
    /// <remarks>
    /// See <see cref="ExecuteAsync(Func{CancellationToken, Task}, string?)"/> for additional details.
    /// </remarks>
    /// <example>
    /// <code>
    /// var data = await ExecuteAsync(async ct =>
    /// {
    ///     return await _service.FetchDataAsync(ct);
    /// }, "Loading...");
    ///
    /// if (data != null)
    /// {
    ///     ProcessData(data);
    /// }
    /// </code>
    /// </example>
    protected async Task<T?> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, string? busyMessage = null)
    {
        if (IsBusy) return default;
        ThrowIfDisposed();

        CancellationTokenSource? cts;
        lock (_syncLock)
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            cts = _cancellationTokenSource;
        }

        try
        {
            ClearError();
            SetBusy(true, busyMessage);
            return await operation(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled - this is expected, don't set an error
            return default;
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

    /// <summary>
    /// Executes an asynchronous operation with automatic busy state and error handling.
    /// This overload does not provide a cancellation token.
    /// </summary>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <param name="busyMessage">The message to display while the operation is running.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Use this overload when the operation does not support cancellation.
    /// For operations that can be cancelled, use <see cref="ExecuteAsync(Func{CancellationToken, Task}, string?)"/>.
    /// </remarks>
    protected Task ExecuteAsync(Func<Task> operation, string? busyMessage = null)
    {
        return ExecuteAsync(async _ => await operation().ConfigureAwait(false), busyMessage);
    }

    /// <summary>
    /// Executes an asynchronous operation with automatic busy state and error handling,
    /// returning a result. This overload does not provide a cancellation token.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="operation">The asynchronous operation to execute.</param>
    /// <param name="busyMessage">The message to display while the operation is running.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the result of the operation
    /// or <c>default(T)</c> if the operation failed.
    /// </returns>
    protected Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? busyMessage = null)
    {
        return ExecuteAsync(async _ => await operation().ConfigureAwait(false), busyMessage);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    /// <summary>
    /// Releases all resources used by this ViewModel.
    /// </summary>
    /// <remarks>
    /// Call this method when you are finished using the ViewModel.
    /// This method cancels any in-progress operations and releases managed resources.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by this ViewModel and optionally releases
    /// the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources.
    /// </param>
    /// <remarks>
    /// <para>
    /// Override this method in derived classes to release additional resources.
    /// Always call the base implementation to ensure proper cleanup.
    /// </para>
    /// <para>
    /// When implementing this method, check whether the method has already been called
    /// by testing <see cref="IsDisposed"/> to avoid disposing resources twice.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void Dispose(bool disposing)
    /// {
    ///     if (disposing &amp;&amp; !IsDisposed)
    ///     {
    ///         _subscription?.Dispose();
    ///         _dataService?.Dispose();
    ///     }
    ///     base.Dispose(disposing);
    /// }
    /// </code>
    /// </example>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            lock (_syncLock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        _disposed = true;
    }
}
