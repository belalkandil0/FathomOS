namespace FathomOS.Core.Interfaces;

/// <summary>
/// Defines the types of toast notifications available.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Informational notification (blue accent)
    /// </summary>
    Info,

    /// <summary>
    /// Success notification (green accent)
    /// </summary>
    Success,

    /// <summary>
    /// Warning notification (yellow/amber accent)
    /// </summary>
    Warning,

    /// <summary>
    /// Error notification (red accent)
    /// </summary>
    Error
}

/// <summary>
/// Configuration options for a notification.
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// Duration in milliseconds before auto-dismissing (0 = no auto-dismiss).
    /// Default is 4000ms.
    /// </summary>
    public int DurationMs { get; set; } = 4000;

    /// <summary>
    /// Optional action to execute when the notification is clicked.
    /// </summary>
    public Action? OnClick { get; set; }

    /// <summary>
    /// Whether the notification can be dismissed by clicking it.
    /// Default is true.
    /// </summary>
    public bool IsDismissible { get; set; } = true;

    /// <summary>
    /// Optional action button text.
    /// </summary>
    public string? ActionText { get; set; }

    /// <summary>
    /// Optional action to execute when the action button is clicked.
    /// </summary>
    public Action? OnAction { get; set; }
}

/// <summary>
/// Contract for toast notification services across the application.
/// Implemented by Shell (NotificationService), consumed by all modules.
/// Owned by: SHELL-AGENT (implementation), UI-AGENT (design)
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a success notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <param name="options">Optional configuration for the notification.</param>
    void ShowSuccess(string message, string? title = null, NotificationOptions? options = null);

    /// <summary>
    /// Shows an error notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <param name="options">Optional configuration for the notification.</param>
    void ShowError(string message, string? title = null, NotificationOptions? options = null);

    /// <summary>
    /// Shows a warning notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <param name="options">Optional configuration for the notification.</param>
    void ShowWarning(string message, string? title = null, NotificationOptions? options = null);

    /// <summary>
    /// Shows an informational notification.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <param name="options">Optional configuration for the notification.</param>
    void ShowInfo(string message, string? title = null, NotificationOptions? options = null);

    /// <summary>
    /// Shows a notification with the specified type.
    /// </summary>
    /// <param name="type">The type of notification to show.</param>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <param name="options">Optional configuration for the notification.</param>
    void Show(NotificationType type, string message, string? title = null, NotificationOptions? options = null);

    /// <summary>
    /// Dismisses all currently visible notifications.
    /// </summary>
    void DismissAll();

    /// <summary>
    /// Gets the maximum number of notifications that can be displayed at once.
    /// </summary>
    int MaxVisibleNotifications { get; }

    /// <summary>
    /// Sets the maximum number of notifications that can be displayed at once.
    /// </summary>
    /// <param name="max">The maximum number of visible notifications (1-10).</param>
    void SetMaxVisibleNotifications(int max);
}
