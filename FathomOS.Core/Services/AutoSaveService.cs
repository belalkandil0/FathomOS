// FathomOS.Core/Services/AutoSaveService.cs
// Implementation of the reusable auto-save service
// Provides automatic periodic saving capabilities for modules

using FathomOS.Core.Logging;
using System.Timers;
using Timer = System.Timers.Timer;

namespace FathomOS.Core.Services;

/// <summary>
/// Thread-safe implementation of the auto-save service.
/// Uses a timer to periodically check for unsaved changes and fires
/// an event when auto-save should be performed.
/// </summary>
public class AutoSaveService : IAutoSaveService
{
    #region Constants

    /// <summary>
    /// Default auto-save interval in seconds.
    /// </summary>
    public const int DefaultIntervalSeconds = 60;

    /// <summary>
    /// Minimum allowed interval in seconds.
    /// </summary>
    public const int MinimumIntervalSeconds = 5;

    /// <summary>
    /// Maximum allowed interval in seconds (1 hour).
    /// </summary>
    public const int MaximumIntervalSeconds = 3600;

    #endregion

    #region Fields

    private readonly ILogger? _logger;
    private readonly object _lock = new();
    private readonly Timer _timer;

    private int _intervalSeconds;
    private bool _isEnabled;
    private bool _isDirty;
    private bool _isDisposed;

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler? AutoSaveRequested;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of AutoSaveService with default settings.
    /// </summary>
    public AutoSaveService()
        : this(null, DefaultIntervalSeconds)
    {
    }

    /// <summary>
    /// Initializes a new instance of AutoSaveService with logging support.
    /// </summary>
    /// <param name="logger">The logger instance for recording operations.</param>
    public AutoSaveService(ILogger? logger)
        : this(logger, DefaultIntervalSeconds)
    {
    }

    /// <summary>
    /// Initializes a new instance of AutoSaveService with custom interval.
    /// </summary>
    /// <param name="logger">The logger instance for recording operations.</param>
    /// <param name="intervalSeconds">The interval between auto-save checks in seconds.</param>
    public AutoSaveService(ILogger? logger, int intervalSeconds)
    {
        _logger = logger;
        _intervalSeconds = ValidateInterval(intervalSeconds);
        _isEnabled = true;
        _isDirty = false;

        _timer = new Timer(_intervalSeconds * 1000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;

        _logger?.Debug($"AutoSaveService initialized with interval: {_intervalSeconds}s", nameof(AutoSaveService));
    }

    #endregion

    #region Properties

    /// <inheritdoc />
    public int IntervalSeconds
    {
        get
        {
            lock (_lock)
            {
                return _intervalSeconds;
            }
        }
        set
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                var validatedInterval = ValidateInterval(value);
                if (_intervalSeconds != validatedInterval)
                {
                    _intervalSeconds = validatedInterval;
                    _timer.Interval = _intervalSeconds * 1000;

                    _logger?.Debug($"AutoSave interval changed to: {_intervalSeconds}s", nameof(AutoSaveService));
                }
            }
        }
    }

    /// <inheritdoc />
    public bool IsEnabled
    {
        get
        {
            lock (_lock)
            {
                return _isEnabled;
            }
        }
        set
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    _logger?.Debug($"AutoSave enabled: {_isEnabled}", nameof(AutoSaveService));
                }
            }
        }
    }

    /// <inheritdoc />
    public bool IsDirty
    {
        get
        {
            lock (_lock)
            {
                return _isDirty;
            }
        }
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public void MarkDirty()
    {
        lock (_lock)
        {
            ThrowIfDisposed();

            if (!_isDirty)
            {
                _isDirty = true;
                _logger?.Debug("Document marked as dirty", nameof(AutoSaveService));
            }
        }
    }

    /// <inheritdoc />
    public void MarkClean()
    {
        lock (_lock)
        {
            ThrowIfDisposed();

            if (_isDirty)
            {
                _isDirty = false;
                _logger?.Debug("Document marked as clean", nameof(AutoSaveService));
            }
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_lock)
        {
            ThrowIfDisposed();

            if (!_timer.Enabled)
            {
                _timer.Start();
                _logger?.Info("AutoSave timer started", nameof(AutoSaveService));
            }
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_lock)
        {
            ThrowIfDisposed();

            if (_timer.Enabled)
            {
                _timer.Stop();
                _logger?.Info("AutoSave timer stopped", nameof(AutoSaveService));
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles the timer elapsed event.
    /// Fires AutoSaveRequested if dirty and enabled.
    /// </summary>
    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        bool shouldSave;

        lock (_lock)
        {
            if (_isDisposed)
                return;

            shouldSave = _isEnabled && _isDirty;

            if (shouldSave)
            {
                _logger?.Debug("Auto-save triggered", nameof(AutoSaveService));
            }
        }

        if (shouldSave)
        {
            try
            {
                AutoSaveRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger?.Error("Error during auto-save event handling", ex, nameof(AutoSaveService));
            }
        }
    }

    /// <summary>
    /// Validates and clamps the interval to allowed range.
    /// </summary>
    /// <param name="intervalSeconds">The requested interval.</param>
    /// <returns>The validated interval within allowed bounds.</returns>
    private int ValidateInterval(int intervalSeconds)
    {
        if (intervalSeconds < MinimumIntervalSeconds)
        {
            _logger?.Warning($"Interval {intervalSeconds}s below minimum, using {MinimumIntervalSeconds}s", nameof(AutoSaveService));
            return MinimumIntervalSeconds;
        }

        if (intervalSeconds > MaximumIntervalSeconds)
        {
            _logger?.Warning($"Interval {intervalSeconds}s above maximum, using {MaximumIntervalSeconds}s", nameof(AutoSaveService));
            return MaximumIntervalSeconds;
        }

        return intervalSeconds;
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(AutoSaveService));
        }
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the AutoSaveService.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(); false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            lock (_lock)
            {
                _timer.Stop();
                _timer.Elapsed -= OnTimerElapsed;
                _timer.Dispose();

                _logger?.Debug("AutoSaveService disposed", nameof(AutoSaveService));
            }
        }

        _isDisposed = true;
    }

    #endregion
}
