using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace FathomOS.Shell.Views;

/// <summary>
/// Professional splash screen with loading progress and animations.
/// Shows during application startup while modules are being discovered.
/// </summary>
public partial class SplashScreen : Window
{
    private readonly Storyboard? _fadeOutStoryboard;
    private TaskCompletionSource<bool>? _closingTask;
    private double _currentProgress = 0;

    public SplashScreen()
    {
        InitializeComponent();

        // Get the fade out storyboard for closing animation
        _fadeOutStoryboard = FindResource("FadeOutAnimation") as Storyboard;
        if (_fadeOutStoryboard != null)
        {
            _fadeOutStoryboard.Completed += (s, e) =>
            {
                _closingTask?.TrySetResult(true);
                base.Close();
            };
        }
    }

    /// <summary>
    /// Update the status text shown on the splash screen.
    /// </summary>
    /// <param name="status">The status message to display.</param>
    public void UpdateStatus(string status)
    {
        Dispatcher.Invoke(() =>
        {
            TxtStatus.Text = status;
        });
    }

    /// <summary>
    /// Update the progress bar (0-100).
    /// </summary>
    /// <param name="progress">Progress percentage from 0 to 100.</param>
    public void UpdateProgress(double progress)
    {
        Dispatcher.Invoke(() =>
        {
            _currentProgress = Math.Min(100, Math.Max(0, progress));
            double targetWidth = (_currentProgress / 100.0) * 320; // 320 is the max width (updated)

            // Animate the progress bar width
            var animation = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            ProgressBar.BeginAnimation(WidthProperty, animation);
            ProgressGlow.BeginAnimation(WidthProperty, animation);
        });
    }

    /// <summary>
    /// Set the version text.
    /// </summary>
    /// <param name="version">Version string to display.</param>
    public void SetVersion(string version)
    {
        Dispatcher.Invoke(() =>
        {
            TxtVersion.Text = version;
        });
    }

    /// <summary>
    /// Set the license status display.
    /// </summary>
    /// <param name="isLicensed">Whether the software is licensed.</param>
    /// <param name="tier">The license tier (e.g., "Professional", "Enterprise").</param>
    public void SetLicenseStatus(bool isLicensed, string tier)
    {
        Dispatcher.Invoke(() =>
        {
            if (isLicensed && !string.IsNullOrEmpty(tier))
            {
                LicenseBadge.Visibility = Visibility.Visible;
                TxtLicenseStatus.Text = tier.ToUpper();

                // Set badge color based on tier
                var gradient = new System.Windows.Media.LinearGradientBrush();
                gradient.StartPoint = new Point(0, 0);
                gradient.EndPoint = new Point(1, 0);

                switch (tier.ToLower())
                {
                    case "enterprise":
                        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(
                            System.Windows.Media.Color.FromRgb(156, 39, 176), 0));
                        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(
                            System.Windows.Media.Color.FromRgb(186, 104, 200), 1));
                        break;
                    case "professional":
                        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(
                            System.Windows.Media.Color.FromRgb(46, 125, 50), 0));
                        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(
                            System.Windows.Media.Color.FromRgb(56, 142, 60), 1));
                        break;
                    default:
                        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(
                            System.Windows.Media.Color.FromRgb(25, 118, 210), 0));
                        gradient.GradientStops.Add(new System.Windows.Media.GradientStop(
                            System.Windows.Media.Color.FromRgb(33, 150, 243), 1));
                        break;
                }

                LicenseBadge.Background = gradient;
            }
            else
            {
                LicenseBadge.Visibility = Visibility.Collapsed;
            }
        });
    }

    /// <summary>
    /// Close the splash screen with a fade-out animation.
    /// Returns a task that completes when the animation finishes.
    /// </summary>
    public async Task CloseWithAnimationAsync()
    {
        _closingTask = new TaskCompletionSource<bool>();

        await Dispatcher.InvokeAsync(() =>
        {
            if (_fadeOutStoryboard != null)
            {
                _fadeOutStoryboard.Begin(this);
            }
            else
            {
                _closingTask.TrySetResult(true);
                base.Close();
            }
        });

        await _closingTask.Task;
    }

    /// <summary>
    /// Override Close to use animation by default.
    /// </summary>
    public new void Close()
    {
        if (_fadeOutStoryboard != null)
        {
            _closingTask = new TaskCompletionSource<bool>();
            _fadeOutStoryboard.Completed += (s, e) => base.Close();
            _fadeOutStoryboard.Begin(this);
        }
        else
        {
            base.Close();
        }
    }

    /// <summary>
    /// Simulate loading progress for demonstration purposes.
    /// </summary>
    public async Task SimulateLoadingAsync(CancellationToken cancellationToken = default)
    {
        var stages = new[]
        {
            ("Initializing core services...", 10),
            ("Loading configuration...", 20),
            ("Validating license...", 35),
            ("Discovering modules...", 50),
            ("Loading module metadata...", 65),
            ("Initializing theme system...", 80),
            ("Preparing user interface...", 90),
            ("Ready!", 100)
        };

        foreach (var (status, progress) in stages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            UpdateStatus(status);
            UpdateProgress(progress);
            await Task.Delay(150, cancellationToken).ConfigureAwait(false);
        }
    }
}
