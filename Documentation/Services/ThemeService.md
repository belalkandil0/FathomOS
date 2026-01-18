# Theme Service Documentation

**Version:** 1.0.0
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Available Themes](#available-themes)
4. [Usage in Modules](#usage-in-modules)
5. [Creating Theme Resources](#creating-theme-resources)
6. [API Reference](#api-reference)

---

## Overview

The Theme Service provides centralized theme management for FathomOS, allowing consistent appearance across all modules and enabling users to switch themes dynamically.

### Key Features

- Four built-in themes (Light, Dark, Modern, Gradient)
- Dynamic theme switching at runtime
- Automatic persistence of theme preference
- Theme change notifications for modules
- Per-window theme application

---

## Architecture

### Service Location

The Theme Service is implemented in the Shell and exposed to modules via the `IThemeService` interface.

```
FathomOS.Shell
└── Services
    └── ThemeService.cs

FathomOS.Core
└── Interfaces
    └── IThemeService.cs
```

### Theme Resources

Theme resources are defined in XAML resource dictionaries:

```
FathomOS.Shell
└── Themes
    ├── LightTheme.xaml
    ├── DarkTheme.xaml
    ├── ModernTheme.xaml
    └── GradientTheme.xaml
```

Each module also contains its own theme files that the module-level ThemeService loads.

---

## Available Themes

### Light Theme

Classic light interface suitable for well-lit environments.

| Property | Value |
|----------|-------|
| Background | #FFFFFF |
| Foreground | #000000 |
| Accent | #0078D4 |
| Card Background | #F5F5F5 |

### Dark Theme (Default)

Dark interface reducing eye strain in low-light conditions.

| Property | Value |
|----------|-------|
| Background | #1E1E1E |
| Foreground | #FFFFFF |
| Accent | #0078D4 |
| Card Background | #2D2D2D |

### Modern Theme

Contemporary design with subtle gradients and shadows.

| Property | Value |
|----------|-------|
| Background | #FAFAFA |
| Foreground | #212121 |
| Accent | #6200EE |
| Card Background | #FFFFFF |

### Gradient Theme

Dynamic gradient-based styling.

| Property | Value |
|----------|-------|
| Background | Gradient |
| Foreground | #FFFFFF |
| Accent | Varies |
| Card Background | Semi-transparent |

---

## Usage in Modules

### Subscribing to Theme Changes

Modules should subscribe to theme changes in their `Initialize()` method:

```csharp
public class MyModule : IModule
{
    private readonly IThemeService? _themeService;

    public void Initialize()
    {
        if (_themeService != null)
        {
            _themeService.ThemeChanged += OnThemeChanged;
        }
    }

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        // React to theme change
        // Shell automatically applies theme to windows
        // Perform any custom theme adjustments here
        UpdateCustomColors(theme);
    }

    public void Shutdown()
    {
        if (_themeService != null)
        {
            _themeService.ThemeChanged -= OnThemeChanged;
        }
    }
}
```

### Module-Level ThemeService

Each module typically has its own ThemeService for loading module-specific theme resources:

```csharp
// In module's Services folder
public class ThemeService
{
    public static void ApplyTheme(Window window, AppTheme theme)
    {
        var dict = new ResourceDictionary();

        string themeName = theme switch
        {
            AppTheme.Light => "LightTheme",
            AppTheme.Dark => "DarkTheme",
            AppTheme.Modern => "ModernTheme",
            AppTheme.Gradient => "GradientTheme",
            _ => "DarkTheme"
        };

        dict.Source = new Uri(
            $"pack://application:,,,/FathomOS.Modules.MyModule;component/Themes/{themeName}.xaml");

        window.Resources.MergedDictionaries.Clear();
        window.Resources.MergedDictionaries.Add(dict);
    }
}
```

---

## Creating Theme Resources

### Theme XAML Structure

Each theme XAML file should define consistent resources:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Base Colors -->
    <Color x:Key="BackgroundColor">#1E1E1E</Color>
    <Color x:Key="ForegroundColor">#FFFFFF</Color>
    <Color x:Key="AccentColor">#0078D4</Color>
    <Color x:Key="CardBackgroundColor">#2D2D2D</Color>
    <Color x:Key="BorderColor">#3D3D3D</Color>

    <!-- Brushes -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource BackgroundColor}"/>
    <SolidColorBrush x:Key="ForegroundBrush" Color="{StaticResource ForegroundColor}"/>
    <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}"/>
    <SolidColorBrush x:Key="CardBackgroundBrush" Color="{StaticResource CardBackgroundColor}"/>
    <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}"/>

    <!-- Control Styles -->
    <Style TargetType="Button" x:Key="PrimaryButton">
        <Setter Property="Background" Value="{StaticResource AccentBrush}"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="BorderThickness" Value="0"/>
    </Style>

    <!-- DataGrid Styles -->
    <Style TargetType="DataGrid" x:Key="ThemedDataGrid">
        <Setter Property="Background" Value="{StaticResource CardBackgroundBrush}"/>
        <Setter Property="Foreground" Value="{StaticResource ForegroundBrush}"/>
        <Setter Property="BorderBrush" Value="{StaticResource BorderBrush}"/>
    </Style>

</ResourceDictionary>
```

### Using Theme Resources in XAML

Reference theme resources in your views:

```xml
<Window Background="{DynamicResource BackgroundBrush}">
    <Grid>
        <TextBlock Foreground="{DynamicResource ForegroundBrush}"
                   Text="Hello World"/>

        <Button Style="{DynamicResource PrimaryButton}"
                Content="Click Me"/>

        <DataGrid Style="{DynamicResource ThemedDataGrid}"/>
    </Grid>
</Window>
```

### Chart Theming

For OxyPlot charts, update plot colors based on theme:

```csharp
private void UpdateChartTheme(AppTheme theme)
{
    var isDark = theme == AppTheme.Dark || theme == AppTheme.Gradient;

    PlotModel.Background = isDark
        ? OxyColors.Transparent
        : OxyColors.White;

    PlotModel.TextColor = isDark
        ? OxyColors.White
        : OxyColors.Black;

    foreach (var axis in PlotModel.Axes)
    {
        axis.TextColor = PlotModel.TextColor;
        axis.TicklineColor = PlotModel.TextColor;
    }

    PlotModel.InvalidatePlot(true);
}
```

---

## API Reference

### IThemeService Interface

```csharp
public interface IThemeService
{
    /// <summary>
    /// Gets or sets the current application theme.
    /// </summary>
    AppTheme CurrentTheme { get; set; }

    /// <summary>
    /// Applies a theme to the application.
    /// </summary>
    void ApplyTheme(AppTheme theme);

    /// <summary>
    /// Applies a theme to a specific window.
    /// </summary>
    void ApplyThemeToWindow(Window window, AppTheme theme);

    /// <summary>
    /// Cycles to the next theme in sequence.
    /// </summary>
    void CycleTheme();

    /// <summary>
    /// Gets the display name for a theme.
    /// </summary>
    string GetThemeDisplayName(AppTheme theme);

    /// <summary>
    /// Gets all available themes.
    /// </summary>
    static AppTheme[] GetAllThemes();

    /// <summary>
    /// Raised when the theme changes.
    /// </summary>
    event EventHandler<AppTheme> ThemeChanged;
}
```

### AppTheme Enumeration

```csharp
public enum AppTheme
{
    Light,
    Dark,
    Modern,
    Gradient
}
```

### Usage Examples

```csharp
// Get current theme
var currentTheme = _themeService.CurrentTheme;

// Set theme
_themeService.CurrentTheme = AppTheme.Dark;

// Cycle theme
_themeService.CycleTheme();

// Get theme display name
string name = _themeService.GetThemeDisplayName(AppTheme.Modern);
// Returns "Modern"

// Get all themes for UI
var allThemes = ThemeService.GetAllThemes();
foreach (var theme in allThemes)
{
    menuItems.Add(new MenuItem { Header = _themeService.GetThemeDisplayName(theme) });
}
```

---

## Best Practices

### 1. Use DynamicResource

Always use `DynamicResource` for theme resources to enable runtime switching:

```xml
<!-- Good -->
<TextBlock Foreground="{DynamicResource ForegroundBrush}"/>

<!-- Bad (won't update on theme change) -->
<TextBlock Foreground="{StaticResource ForegroundBrush}"/>
```

### 2. Clean Up Subscriptions

Always unsubscribe from ThemeChanged in Shutdown():

```csharp
public void Shutdown()
{
    if (_themeService != null)
    {
        _themeService.ThemeChanged -= OnThemeChanged;
    }
}
```

### 3. Consistent Resource Names

Use consistent naming across all theme files:
- BackgroundBrush
- ForegroundBrush
- AccentBrush
- CardBackgroundBrush
- BorderBrush

### 4. Test All Themes

Test your module with all four themes to ensure readability and visual consistency.

---

## Related Documentation

- [Shell API](../API/Shell-API.md)
- [Developer Guide](../DeveloperGuide.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
