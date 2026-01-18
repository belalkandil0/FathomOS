using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace FathomOS.Shell.Views;

/// <summary>
/// Toast notification type enumeration.
/// </summary>
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Modern toast notification control with animations and auto-dismiss.
/// </summary>
public partial class ToastNotification : UserControl
{
    private DispatcherTimer? _autoDismissTimer;
    private DispatcherTimer? _progressTimer;
    private double _progressValue = 0;
    private double _duration;
    private bool _isClosing = false;

    /// <summary>
    /// Event raised when the toast should be removed.
    /// </summary>
    public event EventHandler? Dismissed;

    /// <summary>
    /// Gets or sets whether the toast is paused (e.g., during hover).
    /// </summary>
    public bool IsPaused { get; private set; }

    public ToastNotification()
    {
        InitializeComponent();
        MouseEnter += (s, e) => PauseAutoDismiss();
        MouseLeave += (s, e) => ResumeAutoDismiss();
    }

    /// <summary>
    /// Configure the toast notification.
    /// </summary>
    /// <param name="type">Type of notification (affects color and icon).</param>
    /// <param name="message">The message to display.</param>
    /// <param name="title">Optional title for the notification.</param>
    /// <param name="durationMs">Auto-dismiss duration in milliseconds (0 for no auto-dismiss).</param>
    public void Configure(ToastType type, string message, string? title = null, int durationMs = 4000)
    {
        TxtMessage.Text = message;

        if (!string.IsNullOrEmpty(title))
        {
            TxtTitle.Text = title;
            TxtTitle.Visibility = Visibility.Visible;
        }
        else
        {
            TxtTitle.Visibility = Visibility.Collapsed;
        }

        // Set colors and icon based on type
        Color accentColor;
        string icon;

        switch (type)
        {
            case ToastType.Success:
                accentColor = Color.FromRgb(166, 227, 161); // #A6E3A1
                icon = "Check";
                IconText.Text = "\u2714"; // Checkmark
                break;
            case ToastType.Error:
                accentColor = Color.FromRgb(243, 139, 168); // #F38BA8
                icon = "X";
                IconText.Text = "\u2716"; // X mark
                break;
            case ToastType.Warning:
                accentColor = Color.FromRgb(249, 226, 175); // #F9E2AF
                icon = "!";
                IconText.Text = "\u26A0"; // Warning triangle
                break;
            case ToastType.Info:
            default:
                accentColor = Color.FromRgb(137, 180, 250); // #89B4FA
                icon = "i";
                IconText.Text = "\u2139"; // Info symbol
                break;
        }

        // Apply colors
        AccentColor.Color = accentColor;
        IconForeground.Color = accentColor;
        IconBackground.Color = accentColor;
        ProgressColor.Color = accentColor;

        // Setup auto-dismiss
        if (durationMs > 0)
        {
            _duration = durationMs;
            SetupAutoDismiss(durationMs);
        }
    }

    /// <summary>
    /// Setup auto-dismiss timer and progress indicator.
    /// </summary>
    private void SetupAutoDismiss(int durationMs)
    {
        _autoDismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(durationMs)
        };
        _autoDismissTimer.Tick += (s, e) =>
        {
            _autoDismissTimer.Stop();
            _progressTimer?.Stop();
            Dismiss();
        };

        // Progress bar animation
        _progressTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60fps
        };
        _progressTimer.Tick += (s, e) =>
        {
            if (!IsPaused)
            {
                _progressValue += 16;
                double progress = Math.Min(_progressValue / _duration, 1.0);
                ProgressIndicator.Width = progress * ActualWidth;
            }
        };

        _autoDismissTimer.Start();
        _progressTimer.Start();
    }

    /// <summary>
    /// Pause auto-dismiss (e.g., when hovering).
    /// </summary>
    private void PauseAutoDismiss()
    {
        IsPaused = true;
        _autoDismissTimer?.Stop();
    }

    /// <summary>
    /// Resume auto-dismiss.
    /// </summary>
    private void ResumeAutoDismiss()
    {
        IsPaused = false;
        if (_autoDismissTimer != null && _progressValue < _duration)
        {
            _autoDismissTimer.Interval = TimeSpan.FromMilliseconds(_duration - _progressValue);
            _autoDismissTimer.Start();
        }
    }

    /// <summary>
    /// Dismiss the toast with animation.
    /// </summary>
    public void Dismiss()
    {
        if (_isClosing) return;
        _isClosing = true;

        _autoDismissTimer?.Stop();
        _progressTimer?.Stop();

        var slideOut = FindResource("SlideOutAnimation") as Storyboard;
        if (slideOut != null)
        {
            var storyboard = slideOut.Clone();
            storyboard.Completed += (s, e) => Dismissed?.Invoke(this, EventArgs.Empty);
            storyboard.Begin(this);
        }
        else
        {
            Dismissed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Toast_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Dismiss on click (but not on close button - that has its own handler)
        if (e.Source != BtnClose)
        {
            Dismiss();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }
}
