// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Services/ThemeService.cs
// Purpose: Theme management service for Dark/Light theme switching
// Note: Copied from SurveyListing module as per integration guide
// ============================================================================

using System.Windows;

namespace FathomOS.Modules.SurveyLogbook.Services;

/// <summary>
/// Service for managing application themes (Dark/Light mode).
/// Handles loading theme resource dictionaries and switching between themes.
/// </summary>
public class ThemeService
{
    private readonly FrameworkElement _targetElement;
    private string _currentTheme = "Dark";
    
    /// <summary>
    /// Available theme names.
    /// </summary>
    public static readonly string[] AvailableThemes = { "Dark", "Light", "Modern", "Gradient" };
    
    /// <summary>
    /// Gets the current theme name.
    /// </summary>
    public string CurrentTheme => _currentTheme;
    
    /// <summary>
    /// Event raised when theme changes.
    /// </summary>
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    
    /// <summary>
    /// Initializes a new instance of ThemeService.
    /// </summary>
    /// <param name="targetElement">The element whose resources will be updated</param>
    public ThemeService(FrameworkElement targetElement)
    {
        _targetElement = targetElement ?? throw new ArgumentNullException(nameof(targetElement));
    }
    
    /// <summary>
    /// Loads and applies a theme by name.
    /// </summary>
    /// <param name="themeName">Name of the theme (Dark, Light, Modern, Gradient)</param>
    /// <returns>True if theme was successfully applied</returns>
    public bool ApplyTheme(string themeName)
    {
        if (string.IsNullOrEmpty(themeName))
            return false;
        
        try
        {
            // Build theme resource URI
            var themeUri = new Uri(
                $"/FathomOS.Modules.SurveyLogbook;component/Themes/{themeName}Theme.xaml",
                UriKind.Relative);
            
            // Load theme resource dictionary
            var themeDict = new ResourceDictionary { Source = themeUri };
            
            // Remove existing theme dictionary
            RemoveThemeDictionary();
            
            // Add new theme dictionary
            _targetElement.Resources.MergedDictionaries.Add(themeDict);
            
            _currentTheme = themeName;
            
            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
            {
                OldTheme = _currentTheme,
                NewTheme = themeName
            });
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to apply theme '{themeName}': {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Toggles between Dark and Light themes.
    /// </summary>
    public void ToggleTheme()
    {
        var newTheme = _currentTheme == "Dark" ? "Light" : "Dark";
        ApplyTheme(newTheme);
    }
    
    /// <summary>
    /// Checks if a theme is available.
    /// </summary>
    public bool IsThemeAvailable(string themeName)
    {
        return AvailableThemes.Contains(themeName, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Removes the current theme dictionary from resources.
    /// </summary>
    private void RemoveThemeDictionary()
    {
        var toRemove = _targetElement.Resources.MergedDictionaries
            .Where(d => d.Source?.OriginalString.Contains("Theme.xaml") == true)
            .ToList();
        
        foreach (var dict in toRemove)
        {
            _targetElement.Resources.MergedDictionaries.Remove(dict);
        }
    }
    
    /// <summary>
    /// Gets a resource value from the current theme.
    /// </summary>
    public T? GetResource<T>(string key) where T : class
    {
        if (_targetElement.TryFindResource(key) is T resource)
            return resource;
        return null;
    }
    
    /// <summary>
    /// Gets a color from the current theme.
    /// </summary>
    public System.Windows.Media.Color? GetColor(string key)
    {
        if (_targetElement.TryFindResource(key) is System.Windows.Media.Color color)
            return color;
        return null;
    }
    
    /// <summary>
    /// Gets a brush from the current theme.
    /// </summary>
    public System.Windows.Media.Brush? GetBrush(string key)
    {
        if (_targetElement.TryFindResource(key) is System.Windows.Media.Brush brush)
            return brush;
        return null;
    }
}

/// <summary>
/// Event arguments for theme change events.
/// </summary>
public class ThemeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous theme name.
    /// </summary>
    public string OldTheme { get; set; } = string.Empty;
    
    /// <summary>
    /// New theme name.
    /// </summary>
    public string NewTheme { get; set; } = string.Empty;
}
