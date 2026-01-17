using System.Windows;
using System.Windows.Media;
using FathomOS.Core.Interfaces;
using FathomOS.Core.Messaging;

namespace FathomOS.Shell.Services;

/// <summary>
/// Centralized theme service for the FathomOS application.
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

    #endregion

    #region Fields

    private AppTheme _currentTheme = AppTheme.Dark;
    private readonly IEventAggregator? _eventAggregator;
    private readonly ISettingsService? _settingsService;
    private ResourceDictionary? _currentThemeResources;

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
    }

    #endregion

    #region IThemeService Properties

    /// <inheritdoc />
    public AppTheme CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public bool IsDarkTheme => _currentTheme == AppTheme.Dark;

    #endregion

    #region IThemeService Events

    /// <inheritdoc />
    public event EventHandler<AppTheme>? ThemeChanged;

    #endregion

    #region IThemeService Methods

    /// <inheritdoc />
    public void ApplyTheme(AppTheme theme)
    {
        if (_currentTheme == theme)
        {
            return;
        }

        var oldTheme = _currentTheme;
        _currentTheme = theme;

        // Apply the theme to application resources
        ApplyThemeResources(theme);

        // Persist the theme preference
        SaveThemePreference(theme);

        // Fire events
        OnThemeChanged(theme);

        System.Diagnostics.Debug.WriteLine($"ThemeService: Applied theme {theme}");
    }

    /// <inheritdoc />
    public void ToggleTheme()
    {
        var newTheme = _currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
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
            _currentTheme = savedTheme;
            ApplyThemeResources(savedTheme);
            System.Diagnostics.Debug.WriteLine($"ThemeService: Loaded saved theme {savedTheme}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ThemeService: Invalid saved theme '{savedThemeString}', using default");
            ApplyThemeResources(AppTheme.Dark);
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
    /// Applies theme resources to the application.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    private void ApplyThemeResources(AppTheme theme)
    {
        if (Application.Current == null)
        {
            return;
        }

        var themeUri = theme switch
        {
            AppTheme.Light => new Uri(LightThemeResourcePath, UriKind.Relative),
            AppTheme.Dark => new Uri(DarkThemeResourcePath, UriKind.Relative),
            _ => new Uri(DarkThemeResourcePath, UriKind.Relative)
        };

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
                var packUri = theme switch
                {
                    AppTheme.Light => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/LightTheme.xaml"),
                    AppTheme.Dark => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/DarkTheme.xaml"),
                    _ => new Uri("pack://application:,,,/FathomOS.Shell;component/Themes/DarkTheme.xaml")
                };

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
}
