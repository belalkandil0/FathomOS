using FathomOS.Core.Interfaces;
using System.Windows;

namespace FathomOS.Shell.Services;

/// <summary>
/// Unified theme service for the application.
/// Manages theme switching, persistence, and notifies subscribers of changes.
/// </summary>
public class ThemeService : IThemeService
{
    private const string ThemeSettingKey = "App.Theme";

    private AppTheme _currentTheme = AppTheme.Dark;
    private readonly IEventAggregator? _eventAggregator;
    private readonly ISettingsService? _settingsService;

    public ThemeService(IEventAggregator? eventAggregator = null, ISettingsService? settingsService = null)
    {
        _eventAggregator = eventAggregator;
        _settingsService = settingsService;
    }

    /// <inheritdoc />
    public AppTheme CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public bool IsDarkTheme => _currentTheme == AppTheme.Dark;

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

        // Persist the theme preference
        if (_settingsService != null)
        {
            _settingsService.Set(ThemeSettingKey, theme.ToString());
            _settingsService.Save();
        }

        // Notify via event
        ThemeChanged?.Invoke(this, theme);

        // Publish via event aggregator
        _eventAggregator?.Publish(new Core.Messaging.ThemeChangedEvent(theme));

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
            return;
        }

        var savedThemeString = _settingsService.Get(ThemeSettingKey, AppTheme.Dark.ToString());

        if (Enum.TryParse<AppTheme>(savedThemeString, out var savedTheme))
        {
            // Use direct assignment to avoid early-exit check in ApplyTheme
            // when loading the same theme as the default
            _currentTheme = savedTheme;

            var themeUri = savedTheme switch
            {
                AppTheme.Light => new Uri("pack://application:,,,/FathomOS;component/Themes/LightTheme.xaml"),
                AppTheme.Dark => new Uri("pack://application:,,,/FathomOS;component/Themes/DarkTheme.xaml"),
                _ => new Uri("pack://application:,,,/FathomOS;component/Themes/DarkTheme.xaml")
            };

            try
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
                System.Diagnostics.Debug.WriteLine($"ThemeService: Loaded saved theme {savedTheme}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemeService: Failed to load saved theme: {ex.Message}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"ThemeService: Invalid saved theme '{savedThemeString}', using default");
        }
    }
}
