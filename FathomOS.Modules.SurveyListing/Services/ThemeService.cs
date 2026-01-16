using System;
using System.Windows;
using Application = System.Windows.Application;

namespace FathomOS.Modules.SurveyListing.Services;

/// <summary>
/// Available application themes
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
    Modern,
    Gradient
}

/// <summary>
/// Service for managing application themes
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();
    
    private AppTheme _currentTheme = AppTheme.Dark;
    
    // All theme file names for removal
    private static readonly string[] ThemeFileNames = { "LightTheme", "DarkTheme", "ModernTheme", "GradientTheme" };
    
    public AppTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ApplyTheme(value);
                ThemeChanged?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// Set the current theme state without triggering events or re-applying
    /// Used when theme is already applied manually
    /// </summary>
    public void SetCurrentTheme(AppTheme theme)
    {
        _currentTheme = theme;
    }
    
    public event EventHandler<AppTheme>? ThemeChanged;
    
    public void ApplyTheme(AppTheme theme)
    {
        _currentTheme = theme;
        
        var app = Application.Current;
        if (app == null) return;
        
        // Remove existing theme dictionaries
        var toRemove = new System.Collections.Generic.List<ResourceDictionary>();
        foreach (var dict in app.Resources.MergedDictionaries)
        {
            var source = dict.Source?.ToString() ?? "";
            foreach (var themeName in ThemeFileNames)
            {
                if (source.Contains(themeName))
                {
                    toRemove.Add(dict);
                    break;
                }
            }
        }
        foreach (var dict in toRemove)
        {
            app.Resources.MergedDictionaries.Remove(dict);
        }
        
        // Apply new theme
        var themeUri = GetThemeUri(theme);
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }
    
    /// <summary>
    /// Apply theme to a specific window (when app resources aren't available)
    /// </summary>
    public void ApplyThemeToWindow(Window window, AppTheme theme)
    {
        _currentTheme = theme;
        
        // Remove existing theme dictionaries from window
        var toRemove = new System.Collections.Generic.List<ResourceDictionary>();
        foreach (var dict in window.Resources.MergedDictionaries)
        {
            var source = dict.Source?.ToString() ?? "";
            foreach (var themeName in ThemeFileNames)
            {
                if (source.Contains(themeName))
                {
                    toRemove.Add(dict);
                    break;
                }
            }
        }
        foreach (var dict in toRemove)
        {
            window.Resources.MergedDictionaries.Remove(dict);
        }
        
        // Apply new theme to window
        var themeUri = GetThemeUri(theme);
        window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        ThemeChanged?.Invoke(this, theme);
    }
    
    private static Uri GetThemeUri(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Light => new Uri("pack://application:,,,/FathomOS.Modules.SurveyListing;component/Themes/LightTheme.xaml"),
            AppTheme.Dark => new Uri("pack://application:,,,/FathomOS.Modules.SurveyListing;component/Themes/DarkTheme.xaml"),
            AppTheme.Modern => new Uri("pack://application:,,,/FathomOS.Modules.SurveyListing;component/Themes/ModernTheme.xaml"),
            AppTheme.Gradient => new Uri("pack://application:,,,/FathomOS.Modules.SurveyListing;component/Themes/GradientTheme.xaml"),
            _ => new Uri("pack://application:,,,/FathomOS.Modules.SurveyListing;component/Themes/DarkTheme.xaml")
        };
    }
    
    public string GetThemeDisplayName(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Light => "☀️ Light",
            AppTheme.Dark => "🌙 Dark",
            AppTheme.Modern => "🎨 Modern",
            AppTheme.Gradient => "🌈 Gradient",
            _ => theme.ToString()
        };
    }
    
    public void CycleTheme()
    {
        CurrentTheme = CurrentTheme switch
        {
            AppTheme.Light => AppTheme.Dark,
            AppTheme.Dark => AppTheme.Modern,
            AppTheme.Modern => AppTheme.Gradient,
            AppTheme.Gradient => AppTheme.Light,
            _ => AppTheme.Light
        };
    }
    
    /// <summary>
    /// Get all available themes
    /// </summary>
    public static AppTheme[] GetAllThemes()
    {
        return new[] { AppTheme.Light, AppTheme.Dark, AppTheme.Modern, AppTheme.Gradient };
    }
}
