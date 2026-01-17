namespace FathomOS.Core.Interfaces;

/// <summary>
/// Defines the application theme options.
/// FathomOS supports Dark Professional and Light themes.
/// Default: Dark (user-approved decision)
/// </summary>
public enum AppTheme
{
    /// <summary>
    /// Dark Professional theme (VS Code/Figma inspired)
    /// </summary>
    Dark,

    /// <summary>
    /// Clean Light theme for daytime use
    /// </summary>
    Light
}

/// <summary>
/// Contract for theme management across the application.
/// Implemented by Shell (ThemeService), consumed by all modules.
/// Owned by: SHELL-AGENT (implementation), UI-AGENT (design)
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    AppTheme CurrentTheme { get; }

    /// <summary>
    /// Gets whether dark theme is currently active.
    /// </summary>
    bool IsDarkTheme { get; }

    /// <summary>
    /// Applies a new theme to the application.
    /// This will update all theme-aware resources and fire ThemeChanged.
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    void ApplyTheme(AppTheme theme);

    /// <summary>
    /// Toggles between Dark and Light themes.
    /// </summary>
    void ToggleTheme();

    /// <summary>
    /// Loads the saved theme preference from settings.
    /// Called during application startup.
    /// </summary>
    void LoadSavedTheme();

    /// <summary>
    /// Event fired when the theme changes.
    /// Subscribers should update their theme-dependent resources.
    /// </summary>
    event EventHandler<AppTheme>? ThemeChanged;
}
