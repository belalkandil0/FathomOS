using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using FathomOS.Core.Interfaces;
using FathomOS.Shell.Views;

namespace FathomOS.Shell.Services;

/// <summary>
/// Toast notification service for displaying modern, animated notifications.
/// Manages a notification stack in the bottom-right corner of the main window.
/// </summary>
public class NotificationService : INotificationService
{
    #region Fields

    private readonly List<ToastNotification> _activeNotifications = new();
    private readonly object _lock = new();
    private int _maxVisibleNotifications = 5;
    private Grid? _notificationContainer;
    private StackPanel? _notificationStack;
    private Window? _targetWindow;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the NotificationService.
    /// </summary>
    public NotificationService()
    {
        // Container will be created lazily when first notification is shown
    }

    #endregion

    #region INotificationService Implementation

    /// <inheritdoc />
    public int MaxVisibleNotifications => _maxVisibleNotifications;

    /// <inheritdoc />
    public void ShowSuccess(string message, string? title = null, NotificationOptions? options = null)
    {
        Show(NotificationType.Success, message, title, options);
    }

    /// <inheritdoc />
    public void ShowError(string message, string? title = null, NotificationOptions? options = null)
    {
        Show(NotificationType.Error, message, title ?? "Error", options ?? new NotificationOptions { DurationMs = 6000 });
    }

    /// <inheritdoc />
    public void ShowWarning(string message, string? title = null, NotificationOptions? options = null)
    {
        Show(NotificationType.Warning, message, title ?? "Warning", options ?? new NotificationOptions { DurationMs = 5000 });
    }

    /// <inheritdoc />
    public void ShowInfo(string message, string? title = null, NotificationOptions? options = null)
    {
        Show(NotificationType.Info, message, title, options);
    }

    /// <inheritdoc />
    public void Show(NotificationType type, string message, string? title = null, NotificationOptions? options = null)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            EnsureContainerExists();

            if (_notificationStack == null) return;

            options ??= new NotificationOptions();

            // Create and configure the toast
            var toast = new ToastNotification();
            var toastType = type switch
            {
                NotificationType.Success => ToastType.Success,
                NotificationType.Error => ToastType.Error,
                NotificationType.Warning => ToastType.Warning,
                _ => ToastType.Info
            };

            toast.Configure(toastType, message, title, options.DurationMs);

            // Handle click action
            if (options.OnClick != null)
            {
                toast.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.Source != toast.FindName("BtnClose"))
                    {
                        options.OnClick();
                    }
                };
            }

            // Handle dismissal
            toast.Dismissed += (s, e) =>
            {
                RemoveNotification(toast);
            };

            // Add to container and track
            lock (_lock)
            {
                // Remove oldest notifications if we're at max
                while (_activeNotifications.Count >= _maxVisibleNotifications)
                {
                    var oldest = _activeNotifications[0];
                    oldest.Dismiss();
                }

                _activeNotifications.Add(toast);
                _notificationStack.Children.Insert(0, toast);
            }

            System.Diagnostics.Debug.WriteLine($"NotificationService: Showed {type} notification - {message}");
        });
    }

    /// <inheritdoc />
    public void DismissAll()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                foreach (var notification in _activeNotifications.ToList())
                {
                    notification.Dismiss();
                }
            }
        });
    }

    /// <inheritdoc />
    public void SetMaxVisibleNotifications(int max)
    {
        _maxVisibleNotifications = Math.Max(1, Math.Min(10, max));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Ensures the notification container exists and is attached to the main window.
    /// </summary>
    private void EnsureContainerExists()
    {
        if (_notificationStack != null && _targetWindow != null && _targetWindow == Application.Current?.MainWindow)
        {
            return;
        }

        _targetWindow = Application.Current?.MainWindow;
        if (_targetWindow == null)
        {
            System.Diagnostics.Debug.WriteLine("NotificationService: No main window available");
            return;
        }

        // Find or create the adorner layer/container
        var content = _targetWindow.Content as UIElement;
        if (content == null)
        {
            System.Diagnostics.Debug.WriteLine("NotificationService: Window has no content");
            return;
        }

        // Try to find existing container
        if (_targetWindow.Content is Grid existingGrid)
        {
            _notificationContainer = existingGrid.Children.OfType<Grid>()
                .FirstOrDefault(g => g.Name == "NotificationContainer");

            if (_notificationContainer != null)
            {
                _notificationStack = _notificationContainer.Children.OfType<StackPanel>().FirstOrDefault();
                if (_notificationStack != null) return;
            }
        }

        // Create overlay container
        _notificationContainer = new Grid
        {
            Name = "NotificationContainer",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 16, 60), // Offset from bottom-right, above footer
            IsHitTestVisible = true
        };

        _notificationStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        _notificationContainer.Children.Add(_notificationStack);

        // Add container to window
        if (_targetWindow.Content is Grid windowGrid)
        {
            // Make sure it's on top (highest z-index)
            Panel.SetZIndex(_notificationContainer, 9999);
            windowGrid.Children.Add(_notificationContainer);
        }
        else if (_targetWindow.Content is Border windowBorder && windowBorder.Child is Grid innerGrid)
        {
            // Handle windows with Border as root (like DashboardWindow)
            Panel.SetZIndex(_notificationContainer, 9999);
            innerGrid.Children.Add(_notificationContainer);
        }
        else
        {
            // Wrap existing content in a Grid
            var newRoot = new Grid();
            _targetWindow.Content = null;
            newRoot.Children.Add(content as UIElement ?? new Border());
            Panel.SetZIndex(_notificationContainer, 9999);
            newRoot.Children.Add(_notificationContainer);
            _targetWindow.Content = newRoot;
        }

        System.Diagnostics.Debug.WriteLine("NotificationService: Created notification container");
    }

    /// <summary>
    /// Removes a notification from the container.
    /// </summary>
    private void RemoveNotification(ToastNotification toast)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            lock (_lock)
            {
                _activeNotifications.Remove(toast);
                _notificationStack?.Children.Remove(toast);
            }
        });
    }

    #endregion
}
