using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FathomOS.Shell.Controls;

/// <summary>
/// A modern loading overlay control with spinning indicator, optional message,
/// progress bar, and cancel functionality.
/// </summary>
public partial class LoadingOverlay : UserControl
{
    #region Dependency Properties

    /// <summary>
    /// Message to display on the loading overlay.
    /// </summary>
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(LoadingOverlay),
            new PropertyMetadata("Loading...", OnMessageChanged));

    /// <summary>
    /// Whether the loading overlay is visible.
    /// </summary>
    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingOverlay),
            new PropertyMetadata(false, OnIsLoadingChanged));

    /// <summary>
    /// Progress value (0-100). Set to -1 for indeterminate.
    /// </summary>
    public static readonly DependencyProperty ProgressProperty =
        DependencyProperty.Register(nameof(Progress), typeof(double), typeof(LoadingOverlay),
            new PropertyMetadata(-1.0, OnProgressChanged));

    /// <summary>
    /// Whether the cancel button is visible.
    /// </summary>
    public static readonly DependencyProperty IsCancellableProperty =
        DependencyProperty.Register(nameof(IsCancellable), typeof(bool), typeof(LoadingOverlay),
            new PropertyMetadata(false, OnIsCancellableChanged));

    /// <summary>
    /// Background opacity for the overlay.
    /// </summary>
    public static readonly DependencyProperty OverlayOpacityProperty =
        DependencyProperty.Register(nameof(OverlayOpacity), typeof(double), typeof(LoadingOverlay),
            new PropertyMetadata(0.5, OnOverlayOpacityChanged));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the message displayed on the loading overlay.
    /// </summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the loading overlay is visible.
    /// </summary>
    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>
    /// Gets or sets the progress value (0-100). Set to -1 for indeterminate mode.
    /// </summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the cancel button is visible.
    /// </summary>
    public bool IsCancellable
    {
        get => (bool)GetValue(IsCancellableProperty);
        set => SetValue(IsCancellableProperty, value);
    }

    /// <summary>
    /// Gets or sets the background overlay opacity (0.0 to 1.0).
    /// </summary>
    public double OverlayOpacity
    {
        get => (double)GetValue(OverlayOpacityProperty);
        set => SetValue(OverlayOpacityProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the cancel button is clicked.
    /// </summary>
    public event EventHandler? Cancelled;

    #endregion

    #region Constructor

    public LoadingOverlay()
    {
        InitializeComponent();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Shows the loading overlay with an optional message.
    /// </summary>
    /// <param name="message">Optional message to display.</param>
    public void Show(string? message = null)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Message = message;
        }
        IsLoading = true;
    }

    /// <summary>
    /// Hides the loading overlay.
    /// </summary>
    public void Hide()
    {
        IsLoading = false;
    }

    /// <summary>
    /// Updates the message while showing.
    /// </summary>
    /// <param name="message">The new message to display.</param>
    public void UpdateMessage(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Updates the progress value.
    /// </summary>
    /// <param name="progress">Progress value from 0 to 100.</param>
    public void UpdateProgress(double progress)
    {
        Progress = Math.Max(0, Math.Min(100, progress));
    }

    #endregion

    #region Property Changed Handlers

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingOverlay overlay)
        {
            overlay.TxtMessage.Text = e.NewValue as string ?? "Loading...";
        }
    }

    private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingOverlay overlay)
        {
            var isLoading = (bool)e.NewValue;

            if (isLoading)
            {
                var showAnimation = overlay.FindResource("ShowAnimation") as Storyboard;
                showAnimation?.Begin(overlay);
            }
            else
            {
                var hideAnimation = overlay.FindResource("HideAnimation") as Storyboard;
                hideAnimation?.Begin(overlay);
            }
        }
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingOverlay overlay)
        {
            var progress = (double)e.NewValue;

            if (progress >= 0)
            {
                overlay.ProgressContainer.Visibility = Visibility.Visible;
                overlay.TxtProgress.Visibility = Visibility.Visible;
                overlay.TxtProgress.Text = $"{progress:0}%";

                // Animate progress bar width
                var targetWidth = (progress / 100.0) * overlay.ProgressContainer.ActualWidth;
                if (targetWidth > 0)
                {
                    var animation = new DoubleAnimation
                    {
                        To = targetWidth,
                        Duration = TimeSpan.FromMilliseconds(200),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    overlay.ProgressBar.BeginAnimation(WidthProperty, animation);
                }
            }
            else
            {
                // Indeterminate mode - hide progress bar
                overlay.ProgressContainer.Visibility = Visibility.Collapsed;
                overlay.TxtProgress.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static void OnIsCancellableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingOverlay overlay)
        {
            overlay.BtnCancel.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnOverlayOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingOverlay overlay)
        {
            var opacity = (double)e.NewValue;
            var color = Colors.Black;
            color.A = (byte)(opacity * 255);
            overlay.BackgroundOverlay.Background = new SolidColorBrush(color);
        }
    }

    #endregion

    #region Event Handlers

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
