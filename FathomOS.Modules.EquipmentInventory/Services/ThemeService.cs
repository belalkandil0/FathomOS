using System.Windows;


namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Centralized theme management service
/// </summary>
public class ThemeService
{
    private bool _isDarkTheme = true;
    
    /// <summary>
    /// Singleton instance for static access
    /// </summary>
    public static ThemeService Instance { get; } = new ThemeService();
    
    /// <summary>
    /// Event fired when theme changes
    /// </summary>
    public event EventHandler<bool>? ThemeChanged;
    
    /// <summary>
    /// Static event for theme changes
    /// </summary>
    public static event EventHandler<bool>? StaticThemeChanged;
    
    /// <summary>
    /// Gets or sets whether dark theme is active
    /// </summary>
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme != value)
            {
                _isDarkTheme = value;
                ThemeChanged?.Invoke(this, value);
                StaticThemeChanged?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// Gets the current theme name
    /// </summary>
    public string CurrentThemeName => _isDarkTheme ? "Dark" : "Light";
    
    /// <summary>
    /// Apply theme by name ("Dark" or "Light")
    /// </summary>
    public void ApplyTheme(string themeName)
    {
        var useDarkTheme = themeName.Equals("Dark", StringComparison.OrdinalIgnoreCase);
        _isDarkTheme = useDarkTheme;
        
        var themeFile = useDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeFile}", UriKind.Relative);
        
        // Apply to all open windows from this module
        if (Application.Current != null)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window?.GetType().Namespace?.Contains("EquipmentInventory") == true)
                {
                    ApplyThemeToElement(window, themeUri);
                }
            }
        }
        
        ThemeChanged?.Invoke(this, useDarkTheme);
        StaticThemeChanged?.Invoke(this, useDarkTheme);
    }
    
    /// <summary>
    /// Apply theme to a specific FrameworkElement
    /// </summary>
    public void ApplyTheme(FrameworkElement element, bool useDarkTheme)
    {
        var themeFile = useDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
        var themeUri = new Uri($"/FathomOS.Modules.EquipmentInventory;component/Themes/{themeFile}", UriKind.Relative);
        ApplyThemeToElement(element, themeUri);
    }
    
    /// <summary>
    /// Static method to apply theme to element (convenience wrapper)
    /// </summary>
    public static void ApplyCurrentTheme(FrameworkElement element)
    {
        Instance.ApplyTheme(element, Instance.IsDarkTheme);
    }
    
    /// <summary>
    /// Apply theme to all open windows
    /// </summary>
    public void ApplyThemeToAllWindows()
    {
        ApplyTheme(CurrentThemeName);
    }
    
    private void ApplyThemeToElement(FrameworkElement element, Uri themeUri)
    {
        try
        {
            // Remove existing theme dictionaries
            var existingThemes = element.Resources.MergedDictionaries
                .Where(d => d.Source?.ToString().Contains("Theme.xaml") == true)
                .ToList();
            
            foreach (var theme in existingThemes)
            {
                element.Resources.MergedDictionaries.Remove(theme);
            }
            
            // Add new theme
            element.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Theme apply error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Toggle the current theme
    /// </summary>
    public void ToggleTheme()
    {
        ApplyTheme(_isDarkTheme ? "Light" : "Dark");
    }
    
    /// <summary>
    /// Initialize theme from settings
    /// </summary>
    public void Initialize()
    {
        var settings = ModuleSettings.Load();
        _isDarkTheme = settings.UseDarkTheme;
    }
}
