# FathomOS Developer Guide

**Version:** 1.0.45
**Last Updated:** January 2026

## Table of Contents

1. [Development Environment](#development-environment)
2. [Solution Structure](#solution-structure)
3. [Creating a New Module](#creating-a-new-module)
4. [Module Implementation](#module-implementation)
5. [Using Core Services](#using-core-services)
6. [UI Development](#ui-development)
7. [Testing](#testing)
8. [Debugging](#debugging)
9. [Best Practices](#best-practices)

---

## Development Environment

### Required Tools

| Tool | Version | Purpose |
|------|---------|---------|
| Visual Studio 2022 | 17.8+ | Primary IDE |
| .NET SDK | 8.0 | Build framework |
| Git | Latest | Version control |

### Recommended Extensions

- **XAML Styler** - Format XAML files
- **ReSharper** or **CodeRush** - Code analysis
- **Markdown Editor** - Documentation editing

### Initial Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/fathom-os/fathom-os.git
   cd fathom-os
   ```

2. **Open the solution**
   - Open `FathomOS.sln` in Visual Studio

3. **Restore packages**
   - NuGet packages restore automatically on build

4. **Build the solution**
   - Press F6 or Build > Build Solution

5. **Run the application**
   - Set `FathomOS.Shell` as startup project
   - Press F5 to debug

---

## Solution Structure

```
FathomOS.sln
├── FathomOS.Shell/              # Main application (startup project)
├── FathomOS.Core/               # Shared library
├── FathomOS.Modules.*/          # Individual modules
├── FathomOS.ModuleGroups.*/     # Grouped modules
│   └── FathomOS.Modules.*/
├── LicensingSystem.Client/      # License client
├── LicensingSystem.Shared/      # License models
└── FathomOS.Tests/              # Unit tests
```

### Project Dependencies

```
FathomOS.Shell
    └── FathomOS.Core
    └── LicensingSystem.Client
        └── LicensingSystem.Shared
    └── FathomOS.Modules.* (auto-discovered)

FathomOS.Modules.*
    └── FathomOS.Core
```

---

## Creating a New Module

### Step 1: Create Project

1. Right-click solution in Solution Explorer
2. Add > New Project
3. Select "WPF Class Library" (.NET 8.0)
4. Name: `FathomOS.Modules.YourModuleName`
5. Location: Solution folder root (for regular modules) or inside `FathomOS.ModuleGroups.X` (for grouped modules)

### Step 2: Configure Project File

Edit the `.csproj` file:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <RootNamespace>FathomOS.Modules.YourModuleName</RootNamespace>
    <AssemblyName>FathomOS.Modules.YourModuleName</AssemblyName>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>Module description</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\FathomOS.Core\FathomOS.Core.csproj" />
  </ItemGroup>

</Project>
```

### Step 3: Create Module Class

Create `YourModuleNameModule.cs`:

```csharp
using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.YourModuleName.Views;

namespace FathomOS.Modules.YourModuleName;

public class YourModuleNameModule : IModule
{
    private MainWindow? _mainWindow;
    private readonly IThemeService? _themeService;

    // Default constructor for module discovery
    public YourModuleNameModule() { }

    // DI constructor for full functionality
    public YourModuleNameModule(
        IAuthenticationService authService,
        ICertificationService certService,
        IEventAggregator eventAggregator,
        IThemeService themeService,
        IErrorReporter errorReporter)
    {
        _themeService = themeService;
    }

    #region IModule Properties

    public string ModuleId => "YourModuleName";
    public string DisplayName => "Your Module Name";
    public string Description => "Brief description of your module";
    public Version Version => new Version(1, 0, 0);
    public string IconResource => "/FathomOS.Modules.YourModuleName;component/Assets/icon.png";
    public string Category => "Data Processing"; // Or appropriate category
    public int DisplayOrder => 50;

    #endregion

    #region IModule Methods

    public void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"{DisplayName} v{Version} initialized");

        if (_themeService != null)
            _themeService.ThemeChanged += OnThemeChanged;
    }

    public void Launch(Window? owner = null)
    {
        try
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
            }
            _mainWindow.Show();
            _mainWindow.Activate();
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to launch {DisplayName}: {ex.Message}",
                "Module Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Shutdown()
    {
        if (_themeService != null)
            _themeService.ThemeChanged -= OnThemeChanged;
        _mainWindow?.Close();
        _mainWindow = null;
    }

    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".yourext" or ".csv";
    }

    public void OpenFile(string filePath)
    {
        Launch();
        // Load the file
    }

    #endregion

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        System.Diagnostics.Debug.WriteLine($"{ModuleId}: Theme changed to {theme}");
    }
}
```

### Step 4: Create Folder Structure

```
FathomOS.Modules.YourModuleName/
├── YourModuleNameModule.cs
├── Views/
│   └── MainWindow.xaml(.cs)
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── ViewModelBase.cs
│   └── RelayCommand.cs
├── Models/
├── Services/
├── Assets/
│   └── icon.png (32x32 or 64x64)
└── Themes/
    ├── LightTheme.xaml
    ├── DarkTheme.xaml
    ├── ModernTheme.xaml
    └── GradientTheme.xaml
```

### Step 5: Create MainWindow

**MainWindow.xaml:**
```xml
<Window x:Class="FathomOS.Modules.YourModuleName.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Your Module Name"
        Width="1200" Height="800"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <!-- Your UI here -->
    </Grid>
</Window>
```

### Step 6: Add Icon

Add a 32x32 or 64x64 PNG icon to `Assets/icon.png`.

Set the Build Action to "Resource" in file properties.

---

## Module Implementation

### ViewModel Pattern

Use MVVM pattern for clean separation:

**ViewModelBase.cs:**
```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.YourModuleName.ViewModels;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

**RelayCommand.cs:**
```csharp
using System.Windows.Input;

namespace FathomOS.Modules.YourModuleName.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}
```

---

## Using Core Services

### Certificate Generation

```csharp
using FathomOS.Core.Certificates;

// Using the fluent builder
var certificate = await CertificateHelper.QuickCreate(licenseManager)
    .ForModule("YourModuleName", "YM", "1.0.0")
    .WithProject(projectName, projectLocation)
    .WithVessel(vesselName)
    .WithClient(clientName)
    .AddData("Parameter", "Value")
    .AddInputFile(inputFilePath)
    .AddOutputFile(outputFilePath)
    .CreateWithDialogAsync(this);

if (certificate != null)
{
    // Certificate was created successfully
}
```

### Using Parsers

```csharp
using FathomOS.Core.Parsers;

// Parse NPD file
var parser = new NpdParser();
var columnMapping = new ColumnMapping
{
    TimeColumnPattern = "Time",
    EastingColumnPattern = "East",
    NorthingColumnPattern = "North",
    DepthColumnPattern = "Depth",
    HasDateTimeSplit = true
};

var result = parser.Parse(filePath, columnMapping);

foreach (var point in result.Points)
{
    // Process survey points
}
```

### Using Exporters

```csharp
using FathomOS.Core.Export;

// Excel export
var excelExporter = new ExcelExporter();
excelExporter.Export(outputPath, points, project);

// DXF export
var dxfExporter = new DxfExporter(new DxfExportOptions
{
    IncludeRoute = true,
    IncludeKpLabels = true,
    DepthExaggeration = 10.0
});
dxfExporter.Export(outputPath, points, route, project);
```

---

## UI Development

### Theme Support

Each module should support the four themes. Create theme XAML files:

**Themes/DarkTheme.xaml:**
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <SolidColorBrush x:Key="BackgroundBrush" Color="#1E1E1E"/>
    <SolidColorBrush x:Key="ForegroundBrush" Color="#FFFFFF"/>
    <SolidColorBrush x:Key="AccentBrush" Color="#0078D4"/>
    <!-- Add more theme resources -->

</ResourceDictionary>
```

Load theme in App.xaml or MainWindow:
```csharp
var themeUri = new Uri("pack://application:,,,/FathomOS.Modules.YourModuleName;component/Themes/DarkTheme.xaml");
Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
```

### Using MahApps.Metro

Modules can use MahApps controls (provided by Core):

```xml
<Window xmlns:mah="http://metro.mahapps.com/winfx/xaml/controls">
    <mah:MetroWindow.RightWindowCommands>
        <mah:WindowCommands>
            <Button Content="Settings" Click="Settings_Click"/>
        </mah:WindowCommands>
    </mah:MetroWindow.RightWindowCommands>
</Window>
```

### Charts with OxyPlot

```xml
<oxy:PlotView Model="{Binding PlotModel}"/>
```

```csharp
using OxyPlot;
using OxyPlot.Series;

var plotModel = new PlotModel { Title = "Chart Title" };
plotModel.Series.Add(new LineSeries
{
    ItemsSource = dataPoints,
    DataFieldX = "X",
    DataFieldY = "Y"
});
PlotModel = plotModel;
```

---

## Testing

### Unit Test Project

Tests are in `FathomOS.Tests`:

```csharp
using Xunit;
using FathomOS.Core.Parsers;

namespace FathomOS.Tests;

public class NpdParserTests
{
    [Fact]
    public void Parse_ValidFile_ReturnsPoints()
    {
        // Arrange
        var parser = new NpdParser();
        var mapping = new ColumnMapping { /* ... */ };

        // Act
        var result = parser.Parse("testfile.npd", mapping);

        // Assert
        Assert.NotEmpty(result.Points);
    }
}
```

Run tests:
```bash
dotnet test
```

---

## Debugging

### Enable Debug Output

```csharp
System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Debug message");
```

View output in Visual Studio's Output window (Debug category).

### Common Issues

**Module Not Discovered**
- Ensure project name follows `FathomOS.Modules.*` pattern
- Verify module class implements `IModule`
- Check project is in correct location

**Resources Not Found**
- Verify Build Action is "Resource" for assets
- Check pack URI format is correct
- Ensure assembly name matches namespace

---

## Best Practices

### Naming Conventions

- **Module ID**: PascalCase, no spaces (e.g., "SurveyListing")
- **Display Name**: Title Case with spaces (e.g., "Survey Listing")
- **File extensions**: Lowercase (e.g., ".npd")

### Error Handling

```csharp
try
{
    // Operation
}
catch (Exception ex)
{
    _errorReporter?.Report(ModuleId, "Operation failed", ex);
    MessageBox.Show($"Error: {ex.Message}", "Error",
        MessageBoxButton.OK, MessageBoxImage.Error);
}
```

### Resource Cleanup

Always implement proper cleanup in `Shutdown()`:

```csharp
public void Shutdown()
{
    // Unsubscribe from events
    if (_themeService != null)
        _themeService.ThemeChanged -= OnThemeChanged;

    // Close windows
    _mainWindow?.Close();
    _mainWindow = null;

    // Dispose resources
    _disposableResource?.Dispose();
}
```

### Async Operations

Use async/await for long operations:

```csharp
public async Task LoadDataAsync()
{
    IsBusy = true;
    try
    {
        var data = await Task.Run(() => LoadExpensiveOperation());
        // Update UI on main thread
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            DataItems = data;
        });
    }
    finally
    {
        IsBusy = false;
    }
}
```

---

## Related Documentation

- [Architecture](Architecture.md) - System architecture
- [Module API](API/Module-API.md) - IModule interface
- [Core API](API/Core-API.md) - Core library reference

---

*Copyright 2026 Fathom OS. All rights reserved.*
