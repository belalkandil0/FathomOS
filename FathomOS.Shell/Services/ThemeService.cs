using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FathomOS.Core.Interfaces;
using FathomOS.Core.Messaging;
using Microsoft.Win32;

namespace FathomOS.Shell.Services;

/// <summary>
/// Enhanced centralized theme service for the FathomOS application.
/// Supports Dark, Light, and System themes with smooth transitions.
/// </summary>
/// <remarks>
/// This is the single source of truth for theme management in FathomOS.
/// All modules should use this service through the IThemeService interface.
/// </remarks>
public class ThemeService : IThemeService
{
    #region Constants

    private const string ThemeSettingKey = "App.Theme";
    private const string DarkThemeResourcePath = "Themes/DarkTheme.xaml";
    private const string LightThemeResourcePath = "Themes/LightTheme.xaml";
    private const string ModernDarkThemeResourcePath = "Themes/ModernDarkTheme.xaml";
    private const string ModernLightThemeResourcePath = "Themes/ModernLightTheme.xaml";
    private const double TransitionDurationMs = 250;

    #endregion

    #region Fields

    private AppTheme _currentTheme = AppTheme.Dark;
    private AppTheme _userPreference = AppTheme.Dark;
    private readonly IEventAggregator? _eventAggregator;
    private readonly ISettingsService? _settingsService;
    private ResourceDictionary? _currentThemeResources;
    private bool _isTransitioning = false;
    private bool _useModernThemes = false;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ThemeService class.
    /// </summary>
    /// <param name="eventAggregator">Optional event aggregator for publishing theme changes.</param>
    /// <param name="settingsService">Optional settings service for persisting theme preference.</param>
    public ThemeService(IEventAggregator? eventAggregator = null, ISettingsService? settingsService = null)
    {
        _eventAggregator = eventAggregator;
        _settingsService = settingsService;

        // Subscribe to Windows theme changes
        SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;
    }

    #endregion

    #region IThemeService Properties

    /// <inheritdoc />
    public AppTheme CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public bool IsDarkTheme => _currentTheme == AppTheme.Dark ||
        (_currentTheme == AppTheme.System && IsSystemInDarkMode());

    /// <summary>
    /// Gets or sets whether to use modern theme variants.
    /// </summary>
    public bool UseModernThemes
    {
        get => _useModernThemes;
        set
        {
            if (_useModernThemes != value)
            {
                _useModernThemes = value;
                // Re-apply current theme with new variant
                ApplyThemeResources(ResolveActualTheme(_currentTheme));
            }
        }
    }

    #endregion

    #region IThemeService Events

    /// <inheritdoc />
    public event EventHandler<AppTheme>? ThemeChanged;

    #endregion

    #region IThemeService Methods

    /// <inheritdoc />
    public void ApplyTheme(AppTheme theme)
    {
        _userPreference = theme;
        var actualTheme = ResolveActualTheme(theme);

        // Skip if already at the target theme (and not transitioning)
        if (_currentTheme == theme && !_isTransitioning)
        {
            return;
        }

        var oldTheme = _currentTheme;
        _currentTheme = theme;

        // Apply the theme with smooth transition
        ApplyThemeWithTransition(actualTheme);

        // Persist the theme preference
        SaveThemePreference(theme);

        // Fire events
        OnThemeChanged(theme);

        System.Diagnostics.Debug.WriteLine($"ThemeService: Applied theme {theme} (actual: {actualTheme})");
    }

    /// <inheritdoc />
    public void ToggleTheme()
    {
        var actualCurrent = ResolveActualTheme(_currentTheme);
        var newTheme = actualCurrent == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        ApplyTheme(newTheme);
    }

    /// <inheritdoc />
    public void LoadSavedTheme()
    {
        if (_settingsService == null)
        {
            System.Diagnostics.Debug.WriteLine("ThemeService: No settings service available, using default theme");
            ApplyThemeResources(AppTheme.Dark);
            return;
        }

        var savedThemeString = _settingsService.Get(ThemeSettingKey, AppTheme.Dark.ToString());

        if (Enum.TryParse<AppTheme>(savedThemeString, out var savedTheme))
        {
            _userPreference = savedTheme;
            _currentTheme = savedTheme;
            var actualTheme = ResolveActualTheme(savedTheme);
            ApplyThemeResources(actualTheme);
            System.Diagnostics.Debug.WriteLine($"ThemeService: Loaded saved theme {savedTheme} (actual: {actualTheme})");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ThemeService: Invalid saved theme '{savedThemeString}', using default");
            ApplyThemeResources(AppTheme.Dark);
        }
    }

    #endregion

    #region System Theme Detection

    /// <summary>
    /// Checks if Windows is currently in dark mode.
    /// </summary>
    public static bool IsSystemInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return true; // Default to dark mode if we can't determine
        }
    }

    /// <summary>
    /// Resolves the actual theme to apply (handles System theme).
    /// </summary>
    private AppTheme ResolveActualTheme(AppTheme theme)
    {
        if (theme == AppTheme.System)
        {
            return IsSystemInDarkMode() ? AppTheme.Dark : AppTheme.Light;
        }
        return theme;
    }

    /// <summary>
    /// Handles Windows theme changes when using System theme.
    /// </summary>
    private void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && _userPreference == AppTheme.System)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var actualTheme = ResolveActualTheme(AppTheme.System);
                ApplyThemeWithTransition(actualTheme);
                OnThemeChanged(AppTheme.System);
                System.Diagnostics.Debug.WriteLine($"ThemeService: System theme changed, now using {actualTheme}");
            });
        }
    }

    #endregion

    #region Public Helper Methods

    /// <summary>
    /// Gets a theme resource by key.
    /// </summary>
    /// <typeparam name="T">The expected type of the resource.</typeparam>
    /// <param name="resourceKey">The resource key.</param>
    /// <returns>The resource if found, null otherwise.</returns>
    public T? GetThemeResource<T>(string resourceKey) where T : class
    {
        try
        {
            return Application.Current?.FindResource(resourceKey) as T;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a brush from theme resources.
    /// </summary>
    /// <param name="brushKey">The brush resource key.</param>
    /// <returns>The brush if found, null otherwise.</returns>
    public Brush? GetThemeBrush(string brushKey)
    {
        return GetThemeResource<Brush>(brushKey);
    }

    /// <summary>
    /// Gets a color from theme resources.
    /// </summary>
    /// <param name="colorKey">The color resource key.</param>
    /// <returns>The color if found, default color otherwise.</returns>
    public Color GetThemeColor(string colorKey)
    {
        try
        {
            if (Application.Current?.FindResource(colorKey) is Color color)
            {
                return color;
            }
        }
        catch
        {
            // Ignore errors
        }

        return default;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Applies theme resources with a smooth transition animation.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    private void ApplyThemeWithTransition(AppTheme theme)
    {
        if (Application.Current == null || _isTransitioning)
        {
            ApplyThemeResources(theme);
            return;
        }

        _isTransitioning = true;

        try
        {
            // Create fade animation for smooth transition
            var fadeOut = new DoubleAnimation(1, 0.85, TimeSpan.FromMilliseconds(TransitionDurationMs / 2));
            var fadeIn = new DoubleAnimation(0.85, 1, TimeSpan.FromMilliseconds(TransitionDurationMs / 2));

            fadeOut.Completed += (s, e) =>
            {
                // Apply the actual theme change at the midpoint
                ApplyThemeResources(theme);

                // Fade back in
                foreach (Window window in Application.Current.Windows)
                {
                    window?.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                }
            };

            fadeIn.Completed += (s, e) =>
            {
                _isTransitioning = false;
            };

            // Start fade out on all windows
            foreach (Window window in Application.Current.Windows)
            {
                window?.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ThemeService: Transition failed, applying directly: {ex.Message}");
            ApplyThemeResources(theme);
            _isTransitioning = false;
        }
    }

    /// <summary>
    /// Applies theme resources to the application.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    private void ApplyThemeResources(AppTheme theme)
    {
        if (Application.Current == null)
        {
            return;
        }

        // Determine the resource path based on theme and modern variant setting
        var themeUri = GetThemeUri(theme);

        try
        {
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

            // Remove current theme resources if present
            if (_currentThemeResources != null)
            {
                mergedDictionaries.Remove(_currentThemeResources);
            }

            // Create and add new theme resources
            _currentThemeResources = new ResourceDictionary { Source = themeUri };
            mergedDictionaries.Add(_currentThemeResources);

            // Apply to all open windows for immediate effect
            ApplyThemeToAllWindows();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ThemeService: Failed to apply theme resources: {ex.Message}");

            // Try alternative pack URI format
            try
            {
                var packUri = GetPackUri(theme);
                _currentThemeResources = new ResourceDictionary { Source = packUri };
                Application.Current.Resources.MergedDictionaries.Add(_currentThemeResources);
            }
            catch (Exception innerEx)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeService: Also failed with pack URI: {innerEx.Message}");
            }
        }
    }

    /// <summary>
    /// Gets the theme URI based on theme and modern variant setting.
    /// </summary>
    private Uri GetThemeUri(AppTheme theme)
    {
        if (_useModernThemes)
        {
            return theme switch
            {
                AppTheme.Light => new Uri(ModernLightThemeResourcePath, UriKind.Relative),
                AppTheme.Dark => new Uri(ModernDarkThemeResourcePath, UriKind.Relative),
                _ => new Uri(ModernDarkThemeResourcePath, UriKind.Relative)
            };
        }

        return theme switch
        {
            AppTheme.Light => new Uri(LightThemeResourcePath, UriKind.Relative),
            AppTheme.Dark => new Uri(DarkThemeResourcePath, UriKind.Relative),
            _ => new Uri(DarkThemeResourcePath, UriKind.Relative)
        };
    }

    /// <summary>
    /// Gets the pack URI for fallback theme loading.
    /// </summary>
    private Uri GetPackUri(AppTheme theme)
    {
        if (_useModernThemes)
        {
            return theme switch
            {
                AppTheme.Light => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/ModernLightTheme.xaml"),
                AppTheme.Dark => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/ModernDarkTheme.xaml"),
                _ => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/ModernDarkTheme.xaml")
            };
        }

        return theme switch
        {
            AppTheme.Light => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/LightTheme.xaml"),
            AppTheme.Dark => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/DarkTheme.xaml"),
            _ => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/DarkTheme.xaml")
        };
    }

    /// <summary>
    /// Applies theme to all open windows.
    /// </summary>
    private void ApplyThemeToAllWindows()
    {
        if (Application.Current == null)
        {
            return;
        }

        try
        {
            foreach (Window window in Application.Current.Windows)
            {
                window?.InvalidateVisual();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ThemeService: Error applying theme to windows: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the theme preference to settings.
    /// </summary>
    /// <param name="theme">The theme to save.</param>
    private void SaveThemePreference(AppTheme theme)
    {
        if (_settingsService == null)
        {
            return;
        }

        try
        {
            _settingsService.Set(ThemeSettingKey, theme.ToString());
            _settingsService.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ThemeService: Failed to save theme preference: {ex.Message}");
        }
    }

    /// <summary>
    /// Raises the ThemeChanged event and publishes via event aggregator.
    /// </summary>
    /// <param name="theme">The new theme.</param>
    private void OnThemeChanged(AppTheme theme)
    {
        // Fire direct event
        ThemeChanged?.Invoke(this, theme);

        // Publish via event aggregator for cross-module communication
        _eventAggregator?.Publish(new ThemeChangedEvent(theme));
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleanup resources and event subscriptions.
    /// </summary>
    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
    }

    #endregion
}
