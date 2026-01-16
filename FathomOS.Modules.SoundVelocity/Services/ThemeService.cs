using System;
using System.Linq;
using System.Windows;

namespace FathomOS.Modules.SoundVelocity.Services;

/// <summary>
/// Theme service - copied from SurveyListing module
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private bool _isDarkTheme = true;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            _isDarkTheme = value;
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ThemeChanged;

    public ResourceDictionary GetCurrentTheme()
    {
        var themeName = IsDarkTheme ? "DarkTheme" : "LightTheme";
        var uri = new Uri($"/FathomOS.Modules.SoundVelocity;component/Themes/{themeName}.xaml", UriKind.Relative);
        return new ResourceDictionary { Source = uri };
    }

    public void ApplyTheme(FrameworkElement element)
    {
        var existingTheme = element.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme.xaml") == true);
        
        if (existingTheme != null)
            element.Resources.MergedDictionaries.Remove(existingTheme);
        
        element.Resources.MergedDictionaries.Add(GetCurrentTheme());
    }

    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }
}
