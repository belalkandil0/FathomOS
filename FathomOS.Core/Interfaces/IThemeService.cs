namespace FathomOS.Core.Interfaces;

/// <summary>
/// Defines the application theme options
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
    Modern,
    Gradient
}

/// <summary>
/// Contract for theme management across the application.
/// Implemented by Shell, consumed by all modules.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently active theme
    /// </summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Applies a new theme to the application
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    void ApplyTheme(AppTheme theme);

    /// <summary>
    /// Event fired when the theme changes
    /// </summary>
    event EventHandler<AppTheme>? ThemeChanged;
}
