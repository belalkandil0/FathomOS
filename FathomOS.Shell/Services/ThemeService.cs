using FathomOS.Core.Interfaces;
using System.Windows;

namespace FathomOS.Shell.Services;

/// <summary>
/// Unified theme service for the application.
/// Manages theme switching and notifies subscribers of changes.
/// </summary>
public class ThemeService : IThemeService
{
    private AppTheme _currentTheme = AppTheme.Dark;
    private readonly IEventAggregator? _eventAggregator;

    public ThemeService(IEventAggregator? eventAggregator = null)
    {
        _eventAggregator = eventAggregator;
    }

    /// <inheritdoc />
    public AppTheme CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public event EventHandler<AppTheme>? ThemeChanged;

    /// <inheritdoc />
    public void ApplyTheme(AppTheme theme)
    {
        if (_currentTheme == theme) return;

        _currentTheme = theme;

        var themeUri = theme switch
        {
            AppTheme.Light => new Uri("pack://application:,,,/FathomOS;component/Themes/LightTheme.xaml"),
            AppTheme.Dark => new Uri("pack://application:,,,/FathomOS;component/Themes/DarkTheme.xaml"),
            AppTheme.Modern => new Uri("pack://application:,,,/FathomOS;component/Themes/DarkTheme.xaml"),
            AppTheme.Gradient => new Uri("pack://application:,,,/FathomOS;component/Themes/DarkTheme.xaml"),
            _ => new Uri("pack://application:,,,/FathomOS;component/Themes/DarkTheme.xaml")
        };

        try
        {
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ThemeService: Failed to apply theme: {ex.Message}");
        }

        // Notify via event
        ThemeChanged?.Invoke(this, theme);

        // Publish via event aggregator
        _eventAggregator?.Publish(new Core.Messaging.ThemeChangedEvent(theme));

        System.Diagnostics.Debug.WriteLine($"ThemeService: Applied theme {theme}");
    }
}
