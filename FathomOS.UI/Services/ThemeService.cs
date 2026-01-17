using System;
using System.Windows;
using FathomOS.Core.Interfaces;

namespace FathomOS.UI.Services;

/// <summary>
/// Theme management service for FathomOS.
/// Handles runtime theme switching between Dark and Light themes.
/// Owned by: UI-AGENT
/// </summary>
public class ThemeService : IThemeService
{
    private const string ThemeSettingKey = "AppTheme";
    private const string DarkThemeUri = "pack://application:,,,/FathomOS.UI;component/Themes/Colors/DarkThemeColors.xaml";
    private const string LightThemeUri = "pack://application:,,,/FathomOS.UI;component/Themes/Colors/LightThemeColors.xaml";

    private AppTheme _currentTheme = AppTheme.Dark; // Default to Dark as per user decision
    private ResourceDictionary? _currentThemeResources;

    /// <inheritdoc/>
    public AppTheme CurrentTheme => _currentTheme;

    /// <inheritdoc/>
    public bool IsDarkTheme => _currentTheme == AppTheme.Dark;

    /// <inheritdoc/>
    public event EventHandler<AppTheme>? ThemeChanged;

    /// <summary>
    /// Initializes a new instance of the ThemeService.
    /// </summary>
    public ThemeService()
    {
        // Theme will be loaded when LoadSavedTheme is called during app startup
    }

    /// <inheritdoc/>
    public void LoadSavedTheme()
    {
        try
        {
            // Try to load saved preference from application settings
            if (Application.Current?.Properties[ThemeSettingKey] is string savedTheme)
            {
                if (Enum.TryParse<AppTheme>(savedTheme, out var theme))
                {
                    ApplyTheme(theme);
                    return;
                }
            }

            // Also check Properties.Settings if available
            // For now, apply default theme (Dark)
            ApplyTheme(AppTheme.Dark);
        }
        catch
        {
            // If loading fails, use default
            ApplyTheme(AppTheme.Dark);
        }
    }

    /// <inheritdoc/>
    public void ApplyTheme(AppTheme theme)
    {
        if (Application.Current == null)
            return;

        var app = Application.Current;
        var mergedDictionaries = app.Resources.MergedDictionaries;

        // Remove current theme resources if present
        if (_currentThemeResources != null)
        {
            mergedDictionaries.Remove(_currentThemeResources);
        }

        // Create new theme resource dictionary
        var themeUri = theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri;

        try
        {
            _currentThemeResources = new ResourceDictionary
            {
                Source = new Uri(themeUri, UriKind.Absolute)
            };

            // Add new theme resources
            mergedDictionaries.Add(_currentThemeResources);

            // Update current theme
            var previousTheme = _currentTheme;
            _currentTheme = theme;

            // Save preference
            SaveThemePreference(theme);

            // Notify subscribers if theme actually changed
            if (previousTheme != theme)
            {
                ThemeChanged?.Invoke(this, theme);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply theme: {ex.Message}");
            // Fallback to ensure app doesn't crash
        }
    }

    /// <inheritdoc/>
    public void ToggleTheme()
    {
        var newTheme = _currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        ApplyTheme(newTheme);
    }

    /// <summary>
    /// Saves the theme preference to application settings.
    /// </summary>
    private void SaveThemePreference(AppTheme theme)
    {
        try
        {
            if (Application.Current != null)
            {
                Application.Current.Properties[ThemeSettingKey] = theme.ToString();
            }
        }
        catch
        {
            // Ignore save errors
        }
    }

    /// <summary>
    /// Gets the appropriate resource dictionary for a theme.
    /// Can be used for previewing themes without applying.
    /// </summary>
    /// <param name="theme">The theme to get resources for</param>
    /// <returns>Resource dictionary for the specified theme</returns>
    public static ResourceDictionary GetThemeResources(AppTheme theme)
    {
        var themeUri = theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri;
        return new ResourceDictionary
        {
            Source = new Uri(themeUri, UriKind.Absolute)
        };
    }

    /// <summary>
    /// Gets a specific brush from the current theme.
    /// </summary>
    /// <param name="brushKey">The resource key for the brush</param>
    /// <returns>The brush or null if not found</returns>
    public System.Windows.Media.Brush? GetThemeBrush(string brushKey)
    {
        try
        {
            return Application.Current?.FindResource(brushKey) as System.Windows.Media.Brush;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a specific color from the current theme.
    /// </summary>
    /// <param name="colorKey">The resource key for the color</param>
    /// <returns>The color or default if not found</returns>
    public System.Windows.Media.Color GetThemeColor(string colorKey)
    {
        try
        {
            if (Application.Current?.FindResource(colorKey) is System.Windows.Media.Color color)
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
}
