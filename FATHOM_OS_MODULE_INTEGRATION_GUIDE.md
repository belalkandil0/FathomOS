# Fathom OS - Module Integration Guide

## Version 2.5 | December 2024 | Complete Specification

---

## ⚠️ CRITICAL: READ THIS FIRST

**DO NOT:**
- ❌ Create your own Dark/Light theme files - COPY from SurveyListing module
- ❌ Install MahApps, OxyPlot, HelixToolkit - ALREADY IN CORE
- ❌ Install your own PDF library - USE `QuestPDF` (already in Core)
- ❌ Install your own Excel library - USE `ClosedXML` (already in Core)
- ❌ Install your own DXF library - USE `netDxf` (already in Core)
- ❌ Create custom SurveyPoint models - USE the one in Core
- ❌ Duplicate parsers (NPD, RLX, Tide) - USE the ones in Core
- ❌ Create your own RelayCommand - COPY from SurveyListing module
- ❌ Create your own Converters - COPY from SurveyListing module

**DO:**
- ✅ Reference `FathomOS.Core` project (gives you ALL packages)
- ✅ Implement `IModule` interface
- ✅ Copy theme files, RelayCommand, Converters from SurveyListing module
- ✅ Use shared services and models from Core

**Packages You Get FREE from Core:**
- MahApps.Metro + IconPacks (UI framework)
- OxyPlot.Wpf (charting)
- HelixToolkit.Wpf.SharpDX (3D visualization)
- ClosedXML (Excel export)
- QuestPDF (PDF export)
- netDxf (DXF export)
- MathNet.Numerics (calculations)

**AUTOMATIC MODULE DISCOVERY:**
- Root modules: Place at solution root: `FathomOS.Modules.YourName\`
- Grouped modules: Place in group folder: `FathomOS.ModuleGroups.GroupName\FathomOS.Modules.YourName\`
- No need to edit Shell.csproj - everything is auto-discovered!

---

## 1. Architecture Overview

### 1.1 Development Structure (Your Project Folder)

**Two types of modules:**
1. **Root Modules** - Appear directly on main dashboard
2. **Grouped Modules** - Appear under a group tile (e.g., "Calibrations")

```
FathomOS/                                    ← Solution root
├── FathomOS.sln
│
├── FathomOS.Core/                           ← SHARED LIBRARY
│   ├── Interfaces/IModule.cs
│   ├── Models/, Parsers/, Services/, Export/
│   └── AppInfo.cs
│
├── FathomOS.Shell/                          ← Main application
│   ├── Views/DashboardWindow.xaml           ← Shows modules & groups
│   └── Services/ModuleManager.cs            ← Discovers modules & groups
│
│   # ROOT MODULES (appear directly on dashboard)
├── FathomOS.Modules.SurveyListing/          ← Root module
├── FathomOS.Modules.NetworkTimeSync/        ← Root module
├── FathomOS.Modules.SoundVelocity/          ← Root module
│
│   # MODULE GROUPS (appear as group tiles)
├── FathomOS.ModuleGroups.Calibrations/      ← Group folder
│   ├── GroupInfo.json                        ← Group metadata
│   ├── icon.png                              ← Group icon (optional)
│   ├── FathomOS.Modules.GyroCalibration/    ← Grouped module
│   ├── FathomOS.Modules.MruCalibration/     ← Grouped module
│   └── FathomOS.Modules.UsblCalibration/    ← Grouped module
│
└── FathomOS.Tests/
```

### 1.2 Build Output Structure (Automatic)

```
bin/Debug/net8.0-windows/
├── FathomOS.exe
├── FathomOS.Core.dll
└── Modules/
    │   # Root modules
    ├── SurveyListing/
    │   ├── FathomOS.Modules.SurveyListing.dll
    │   ├── ModuleInfo.json
    │   └── Assets/icon.png
    ├── NetworkTimeSync/
    │   └── ...
    │
    │   # Module groups
    └── _Groups/
        └── Calibrations/
            ├── GroupInfo.json
            ├── Assets/icon.png
            ├── GyroCalibration/
            │   ├── FathomOS.Modules.GyroCalibration.dll
            │   ├── ModuleInfo.json
            │   └── Assets/icon.png
            └── MruCalibration/
                └── ...
```

### 1.3 Dashboard Navigation

```
┌─────────────────────────────────────────────────────────────┐
│  S7 FATHOM - Main Dashboard                                 │
│                                                             │
│  MODULE GROUPS                                              │
│  ┌──────────┐                                               │
│  │📁        │                                               │
│  │Calibra-  │  ← Click to see grouped modules               │
│  │  tions   │                                               │
│  │(5 modules│                                               │
│  └──────────┘                                               │
│                                                             │
│  MODULES                                                    │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                    │
│  │ Survey   │ │ Network  │ │  Sound   │  ← Root modules     │
│  │ Listing  │ │ Time     │ │ Velocity │                    │
│  └──────────┘ └──────────┘ └──────────┘                    │
└─────────────────────────────────────────────────────────────┘

After clicking "Calibrations":

┌─────────────────────────────────────────────────────────────┐
│  S7 FATHOM - Calibrations                                   │
│  ┌────────────────┐                                         │
│  │ 🏠 Home        │  ← Returns to main dashboard            │
│  └────────────────┘                                         │
│                                                             │
│  CALIBRATIONS MODULES                                       │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐       │
│  │  Gyro    │ │  MRU     │ │  USBL    │ │  DVL     │       │
│  │Calibrate │ │Calibrate │ │Calibrate │ │Calibrate │       │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘       │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Technology Stack (MANDATORY)

| Technology | Version | Notes |
|------------|---------|-------|
| **.NET** | **8.0** | Target: `net8.0-windows` |
| **WPF** | Built-in | Windows Presentation Foundation |
| **C#** | 12.0 | Latest language features |
| **Windows** | 10/11 | Minimum supported OS |

---

## 3. NuGet Packages

### 3.1 Packages in FathomOS.Core (Available to ALL Modules)

When you reference Core, you automatically get access to ALL these packages:

```xml
<!-- Data Processing -->
<PackageReference Include="MathNet.Numerics" Version="5.0.0" />
<PackageReference Include="System.Text.Json" Version="8.0.5" />

<!-- Export Libraries -->
<PackageReference Include="ClosedXML" Version="0.102.1" />      <!-- Excel export -->
<PackageReference Include="netDxf" Version="3.0.1" />           <!-- DXF export -->
<PackageReference Include="QuestPDF" Version="2024.3.0" />      <!-- PDF export -->
<PackageReference Include="SkiaSharp" Version="2.88.7" />       <!-- Graphics for PDF -->

<!-- UI Framework (shared by all modules) -->
<PackageReference Include="MahApps.Metro" Version="2.4.10" />
<PackageReference Include="MahApps.Metro.IconPacks.Material" Version="5.0.0" />
<PackageReference Include="Microsoft.Xaml.Behaviors.Wpf" Version="1.1.77" />

<!-- Charting (shared by all modules) -->
<PackageReference Include="OxyPlot.Wpf" Version="2.1.2" />

<!-- 3D Visualization (shared by all modules) -->
<PackageReference Include="HelixToolkit.Wpf.SharpDX" Version="2.24.0" />
```

**DO NOT add these packages to your module** - they come automatically from Core!

### 3.2 Your Module Only Needs

```xml
<ItemGroup>
  <!-- This gives you EVERYTHING: MahApps, OxyPlot, HelixToolkit, ClosedXML, QuestPDF, netDxf -->
  <ProjectReference Include="..\FathomOS.Core\FathomOS.Core.csproj" />
</ItemGroup>

<!-- No additional packages needed - everything comes from Core! -->
```

### 3.3 BANNED Packages (DO NOT USE)

| ❌ Don't Use | ✅ Use Instead (from Core) |
|-------------|---------------------------|
| PdfSharp | QuestPDF |
| iTextSharp | QuestPDF |
| EPPlus | ClosedXML |
| NPOI | ClosedXML |
| LiveCharts | OxyPlot |
| Any other PDF library | QuestPDF |
| Any other Excel library | ClosedXML |
| Any other charting library | OxyPlot |

---

## 4. Project File Template (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <OutputType>Library</OutputType>  <!-- MUST be Library, not WinExe -->
    <RootNamespace>FathomOS.Modules.YourModuleName</RootNamespace>
    <AssemblyName>FathomOS.Modules.YourModuleName</AssemblyName>
    <Version>1.0.0</Version>
    <Authors>S7 Solutions</Authors>
    <Description>Your module description</Description>
  </PropertyGroup>

  <ItemGroup>
    <!-- 
      REQUIRED: Reference Core
      This gives you access to:
      - MahApps.Metro (UI framework)
      - OxyPlot (charting)
      - HelixToolkit (3D visualization)
      - ClosedXML (Excel export)
      - QuestPDF (PDF export)
      - netDxf (DXF export)
      - All Core parsers, models, services
    -->
    <ProjectReference Include="..\FathomOS.Core\FathomOS.Core.csproj" />
  </ItemGroup>

  <!-- No additional packages needed - everything comes from Core! -->

  <!-- Module assets -->
  <ItemGroup>
    <Resource Include="Assets\icon.png" />
  </ItemGroup>

</Project>
```

**That's it!** Your module gets all packages from Core - no need to add MahApps, OxyPlot, HelixToolkit, etc.

---

## 5. IModule Interface (REQUIRED)

Every module MUST implement this interface from `FathomOS.Core.Interfaces`:

```csharp
namespace FathomOS.Core.Interfaces;

using System.Windows;

public interface IModule
{
    // === PROPERTIES (all required) ===
    string ModuleId { get; }           // Must match DLL name
    string DisplayName { get; }        // Dashboard tile name
    string Description { get; }        // Tooltip text
    Version Version { get; }           // new Version(1, 0, 0)
    string IconResource { get; }       // "/FathomOS.Modules.X;component/Assets/icon.png"
    string Category { get; }           // "Data Processing", "Quality Control", "Utilities"
    int DisplayOrder { get; }          // Sort order (10, 20, 30...)
    
    // === METHODS (all required) ===
    void Initialize();                 // Called when module loads
    void Launch(Window? owner = null); // Called when user clicks tile
    void Shutdown();                   // Called on app exit
    bool CanHandleFile(string path);   // File association check
    void OpenFile(string path);        // Open file directly
}
```

---

## 6. Complete Module Implementation Example

```csharp
using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.YourModule.Views;

namespace FathomOS.Modules.YourModule;

public class YourModuleModule : IModule
{
    private MainWindow? _mainWindow;
    
    #region IModule Properties
    
    public string ModuleId => "YourModule";  // MUST match DLL name
    public string DisplayName => "Your Module Name";
    public string Description => "Brief description of what this module does.";
    public Version Version => new Version(1, 0, 0);
    public string IconResource => "/FathomOS.Modules.YourModule;component/Assets/icon.png";
    public string Category => "Data Processing";
    public int DisplayOrder => 30;
    
    #endregion
    
    #region IModule Methods
    
    public void Initialize()
    {
        System.Diagnostics.Debug.WriteLine($"{DisplayName} v{Version} initialized");
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
        try { _mainWindow?.Close(); _mainWindow = null; }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Shutdown error: {ex.Message}"); }
    }
    
    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".yourext" or ".s7p";
    }
    
    public void OpenFile(string filePath)
    {
        Launch();
        // Load file in _mainWindow
    }
    
    #endregion
}
```

---

## 7. ModuleInfo.json (REQUIRED)

```json
{
    "moduleId": "YourModule",
    "displayName": "Your Module Name",
    "description": "Brief description of module functionality.",
    "version": "1.0.0",
    "author": "S7 Solutions",
    "category": "Data Processing",
    "displayOrder": 30,
    "icon": "icon.png",
    "supportedFileTypes": [".yourext", ".s7p"],
    "dependencies": [],
    "minimumShellVersion": "1.0.0",
    "features": ["Feature 1", "Feature 2"]
}
```

**CRITICAL:** `moduleId` MUST match your class property, DLL name, and namespace.

---

## 8. Module Project Structure (at Solution Root)

Your module project folder sits at the **solution root** (same level as FathomOS.sln):

```
FathomOS/                                ← Solution root
├── FathomOS.sln
├── FathomOS.Core/
├── FathomOS.Shell/
│
└── FathomOS.Modules.YourModule/         ← Your module HERE (not in Modules/!)
    ├── FathomOS.Modules.YourModule.csproj
    ├── YourModuleModule.cs              ← IModule implementation
    ├── ModuleInfo.json                  ← Module metadata
    │
    ├── Assets/
    │   └── icon.png                     ← 128×128 PNG with transparency
    │
    ├── Views/
    │   ├── MainWindow.xaml
    │   └── MainWindow.xaml.cs
    │
    ├── ViewModels/
    │   ├── MainViewModel.cs
    │   └── RelayCommand.cs              ← COPY from SurveyListing
    │
    ├── Models/                          ← Module-specific models ONLY
    │
    ├── Services/
    │   └── ThemeService.cs              ← COPY from SurveyListing
    │
    ├── Converters/
    │   └── Converters.cs                ← COPY from SurveyListing
    │
    └── Themes/                          ← COPY ALL from SurveyListing
        ├── DarkTheme.xaml
        ├── LightTheme.xaml
        ├── ModernTheme.xaml
        └── GradientTheme.xaml
```

---

## 9. Files to Copy from SurveyListing

Copy these files to your module (update namespaces):

### 9.1 RelayCommand.cs (ViewModels/)

```csharp
using System.Windows.Input;

namespace FathomOS.Modules.YourModule.ViewModels;

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

### 9.2 ViewModelBase.cs (ViewModels/) - CREATE THIS

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FathomOS.Modules.YourModule.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

### 9.3 Converters.cs (Converters/)

```csharp
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FathomOS.Modules.YourModule.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? Visibility.Collapsed : Visibility.Visible;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v != Visibility.Visible;
    }
}

public class NullToDefaultConverter : IValueConverter
{
    public object DefaultValue { get; set; } = "-";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            return parameter ?? DefaultValue;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
```

---

## 10. Themes (COPY FROM SurveyListing)

Copy the entire `Themes/` folder from SurveyListing. Update the theme URI in ThemeService if you copy it.

### Available Theme Resource Keys

```xml
<!-- Colors -->
<Color x:Key="PrimaryColor"/>
<Color x:Key="AccentColor"/>
<Color x:Key="BackgroundColor"/>
<Color x:Key="SurfaceColor"/>
<Color x:Key="CardColor"/>
<Color x:Key="TextPrimaryColor"/>
<Color x:Key="TextSecondaryColor"/>
<Color x:Key="BorderColor"/>
<Color x:Key="SuccessColor"/>
<Color x:Key="WarningColor"/>
<Color x:Key="ErrorColor"/>

<!-- Brushes -->
<SolidColorBrush x:Key="PrimaryBrush"/>
<SolidColorBrush x:Key="AccentBrush"/>
<SolidColorBrush x:Key="BackgroundBrush"/>
<SolidColorBrush x:Key="SurfaceBrush"/>
<SolidColorBrush x:Key="CardBrush"/>
<SolidColorBrush x:Key="TextPrimaryBrush"/>
<SolidColorBrush x:Key="TextSecondaryBrush"/>
<SolidColorBrush x:Key="BorderBrush"/>
<SolidColorBrush x:Key="SuccessBrush"/>
<SolidColorBrush x:Key="WarningBrush"/>
<SolidColorBrush x:Key="ErrorBrush"/>

<!-- Styles -->
<Style x:Key="CardBorder"/>
<Style x:Key="ElevatedCard"/>
<Style x:Key="SectionHeader"/>
<Style x:Key="FieldLabel"/>
<Style x:Key="FieldValue"/>
<Style x:Key="PrimaryButton"/>
<Style x:Key="SecondaryButton"/>
```

### Load Theme in Window Constructor

```csharp
public partial class MainWindow : MetroWindow
{
    public MainWindow()
    {
        // Load theme BEFORE InitializeComponent
        var themeUri = new Uri("/FathomOS.Modules.YourModule;component/Themes/DarkTheme.xaml", UriKind.Relative);
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
        
        InitializeComponent();
    }
}
```

---

## 11. Using Core Services

### 11.1 Parsers (FathomOS.Core.Parsers)

```csharp
using FathomOS.Core.Parsers;
using FathomOS.Core.Models;

// NPD file parser
var npdParser = new NpdParser();
List<SurveyPoint> points = npdParser.Parse(filePath, columnMapping);

// Route file parser
var rlxParser = new RlxParser();
RouteData route = rlxParser.Parse(routeFilePath);

// Tide file parser
var tideParser = new TideParser();
TideData tide = tideParser.Parse(tideFilePath);
```

### 11.2 Exporters (FathomOS.Core.Export)

```csharp
using FathomOS.Core.Export;
using FathomOS.Core.Models;

// Excel export
var excelExporter = new ExcelExporter();
excelExporter.Export(filePath, points, project);

// PDF report - NOTE: QuestPDF Canvas uses object parameter
var pdfGenerator = new PdfReportGenerator(options);
pdfGenerator.Generate(filePath, points, project);

// DXF export
var dxfExporter = new DxfExporter();
dxfExporter.Export(filePath, points, options);
```

### 11.3 QuestPDF Usage (Important!)

When using QuestPDF Canvas for custom drawing:

```csharp
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

// Canvas callback signature - parameter is 'object', cast to SKCanvas
column.Item().Height(100).Canvas((object canvasObj, Size size) => {
    var canvas = (SKCanvas)canvasObj;  // MUST cast!
    
    using var paint = new SKPaint
    {
        Color = SKColors.Blue,
        StrokeWidth = 2,
        IsAntialias = true
    };
    
    canvas.DrawLine(0, 0, (float)size.Width, (float)size.Height, paint);
});
```

### 11.4 ClosedXML Usage

```csharp
using ClosedXML.Excel;

using var workbook = new XLWorkbook();
var worksheet = workbook.Worksheets.Add("Data");

// Set headers
worksheet.Cell(1, 1).Value = "Index";
worksheet.Cell(1, 2).Value = "Easting";
worksheet.Cell(1, 3).Value = "Northing";

// Add data
for (int i = 0; i < points.Count; i++)
{
    worksheet.Cell(i + 2, 1).Value = points[i].Index;
    worksheet.Cell(i + 2, 2).Value = points[i].Easting;
    worksheet.Cell(i + 2, 3).Value = points[i].Northing;
}

// Style header row
var headerRow = worksheet.Row(1);
headerRow.Style.Font.Bold = true;
headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

workbook.SaveAs(filePath);
```

### 11.5 netDxf Usage

```csharp
using netDxf;
using netDxf.Entities;

var doc = new DxfDocument();

// Add polyline
var polyline = new Polyline3D(
    points.Select(p => new Vector3(p.Easting, p.Northing, p.BestDepth ?? 0))
);
doc.Entities.Add(polyline);

// Add text label
var text = new Text("KP 0.000", new Vector2(x, y), 2.5);
doc.Entities.Add(text);

doc.Save(filePath);
```

### 11.6 OxyPlot Charting (from Core)

```csharp
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

// Create plot model
var plotModel = new PlotModel { Title = "Depth Profile" };

// Add axes
plotModel.Axes.Add(new LinearAxis 
{ 
    Position = AxisPosition.Bottom, 
    Title = "KP (km)" 
});
plotModel.Axes.Add(new LinearAxis 
{ 
    Position = AxisPosition.Left, 
    Title = "Depth (m)",
    StartPosition = 1,  // Invert for depth
    EndPosition = 0
});

// Add line series
var series = new LineSeries
{
    Title = "Seabed",
    Color = OxyColors.Blue,
    StrokeThickness = 2
};

foreach (var point in points.Where(p => p.Kp.HasValue && p.BestDepth.HasValue))
{
    series.Points.Add(new DataPoint(point.Kp!.Value, point.BestDepth!.Value));
}

plotModel.Series.Add(series);

// In XAML: <oxy:PlotView Model="{Binding PlotModel}"/>
// Or programmatically:
var plotView = new PlotView { Model = plotModel };
```

### 11.7 HelixToolkit 3D Visualization (from Core)

```csharp
using HelixToolkit.Wpf.SharpDX;
using SharpDX;

// Create 3D points
var positions = new Vector3Collection();
var colors = new Color4Collection();

foreach (var point in points)
{
    positions.Add(new Vector3(
        (float)point.Easting,
        (float)point.Northing,
        (float)(point.BestDepth ?? 0)
    ));
    
    // Color by depth
    float depthNormalized = (float)((point.BestDepth ?? 0) / maxDepth);
    colors.Add(new Color4(depthNormalized, 0, 1 - depthNormalized, 1));
}

// Create point cloud geometry
var pointGeometry = new PointGeometry3D
{
    Positions = positions,
    Colors = colors
};

// In XAML with HelixToolkit viewport:
// <hx:PointGeometryModel3D Geometry="{Binding PointGeometry}" Size="3"/>
```

### 11.8 Services (FathomOS.Core.Services)

```csharp
using FathomOS.Core.Services;

// Distance calculations
double dist = DistanceCalculator.Calculate(x1, y1, x2, y2);

// Smoothing
var smoothingService = new SmoothingService();
var smoothedPoints = smoothingService.Smooth(points, windowSize);

// Spline fitting
var splineService = new SplineService();
var splinePoints = splineService.FitCatmullRom(points, tension);
```

---

## 12. Core Models Reference

### 12.1 SurveyPoint (Main Data Model)

```csharp
public class SurveyPoint
{
    // Identification
    public int Index { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? Name { get; set; }
    public string? Source { get; set; }
    
    // Raw Coordinates
    public double Easting { get; set; }
    public double Northing { get; set; }
    public double? Z { get; set; }           // Depth
    public double? Altitude { get; set; }
    public double? Heading { get; set; }
    
    // Smoothed Data
    public double? SmoothedEasting { get; set; }
    public double? SmoothedNorthing { get; set; }
    public double? SmoothedDepth { get; set; }
    
    // Calculated Values
    public double X => SmoothedEasting ?? Easting;
    public double Y => SmoothedNorthing ?? Northing;
    public double? ProcessedDepth { get; set; }
    public double? Kp { get; set; }          // Kilometre Post
    public double? Dcc { get; set; }         // Distance to Centerline
    
    // Corrections
    public double? TideCorrection { get; set; }
    public double? DraftCorrection { get; set; }
    
    // Status
    public bool IsExcluded { get; set; }
    public bool IsInterpolated { get; set; }
    
    // Additional Data
    public Dictionary<string, string> AdditionalData { get; set; }
    
    // Helpers
    public double? BestDepth => ProcessedDepth ?? SmoothedDepth ?? Z;
    public bool HasValidCoordinates => !double.IsNaN(Easting) && !double.IsNaN(Northing);
}
```

### 12.2 Project (Settings Container)

```csharp
public class Project
{
    // Project Info
    public string ProjectName { get; set; }
    public string ClientName { get; set; }
    public string VesselName { get; set; }
    public DateTime? SurveyDate { get; set; }
    public SurveyType SurveyType { get; set; }
    
    // Files
    public string RouteFilePath { get; set; }
    public List<string> SurveyDataFiles { get; set; }
    public string TideFilePath { get; set; }
    
    // Settings
    public LengthUnit InputUnit { get; set; }
    public LengthUnit OutputUnit { get; set; }
    public ProcessingOptions ProcessingOptions { get; set; }
    public OutputOptions OutputOptions { get; set; }
    
    // Validation
    public List<string> Validate();
    public Project Clone();
}
```

---

## 13. MainWindow Template (MahApps.Metro)

```xml
<mah:MetroWindow x:Class="FathomOS.Modules.YourModule.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mah="http://metro.mahapps.com/winfx/xaml/controls"
        xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks"
        xmlns:converters="clr-namespace:FathomOS.Modules.YourModule.Converters"
        Title="Your Module - Fathom OS"
        Height="800" Width="1200"
        MinHeight="600" MinWidth="900"
        WindowStartupLocation="CenterScreen"
        Background="{DynamicResource BackgroundBrush}"
        BorderBrush="{DynamicResource AccentBrush}"
        BorderThickness="1"
        GlowBrush="{DynamicResource AccentBrush}"
        ResizeMode="CanResizeWithGrip"
        TitleCharacterCasing="Normal">

    <Window.Resources>
        <converters:BoolToVisibilityConverter x:Key="BoolToVisibility"/>
        <converters:InverseBoolToVisibilityConverter x:Key="InverseBoolToVisibility"/>
    </Window.Resources>
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Header -->
            <RowDefinition Height="*"/>     <!-- Content -->
            <RowDefinition Height="Auto"/>  <!-- Footer -->
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <Border Grid.Row="0" Background="{DynamicResource HeaderBrush}" Padding="15">
            <TextBlock Text="Your Module" 
                       FontSize="20" FontWeight="SemiBold"
                       Foreground="{DynamicResource TextPrimaryBrush}"/>
        </Border>
        
        <!-- Content -->
        <Grid Grid.Row="1" Margin="15">
            <!-- Your content here -->
        </Grid>
        
        <!-- Footer/Status Bar -->
        <Border Grid.Row="2" Background="{DynamicResource SurfaceBrush}" Padding="10">
            <TextBlock Text="Ready" Foreground="{DynamicResource TextSecondaryBrush}"/>
        </Border>
    </Grid>
    
</mah:MetroWindow>
```

---

## 14. MVVM ViewModel Example

```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using FathomOS.Core.Models;

namespace FathomOS.Modules.YourModule.ViewModels;

public class MainViewModel : ViewModelBase
{
    private string _statusMessage = "Ready";
    private bool _isBusy;
    private ObservableCollection<SurveyPoint> _points = new();
    
    public MainViewModel()
    {
        LoadCommand = new RelayCommand(_ => LoadData(), _ => !IsBusy);
        SaveCommand = new RelayCommand(_ => SaveData(), _ => !IsBusy && Points.Count > 0);
    }
    
    // Properties
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
    
    public ObservableCollection<SurveyPoint> Points
    {
        get => _points;
        set => SetProperty(ref _points, value);
    }
    
    // Commands
    public ICommand LoadCommand { get; }
    public ICommand SaveCommand { get; }
    
    // Methods
    private async void LoadData()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading...";
            
            await Task.Run(() =>
            {
                // Load data here
            });
            
            StatusMessage = $"Loaded {Points.Count} points";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private void SaveData()
    {
        // Save implementation
    }
}
```

---

## 15. File Dialogs

```csharp
using Microsoft.Win32;

// Open file dialog
var openDialog = new OpenFileDialog
{
    Title = "Select Survey File",
    Filter = "NPD Files (*.npd)|*.npd|All Files (*.*)|*.*",
    Multiselect = true
};

if (openDialog.ShowDialog() == true)
{
    foreach (var file in openDialog.FileNames)
    {
        // Process file
    }
}

// Save file dialog
var saveDialog = new SaveFileDialog
{
    Title = "Export Report",
    Filter = "Excel Files (*.xlsx)|*.xlsx|PDF Files (*.pdf)|*.pdf",
    FileName = "Report"
};

if (saveDialog.ShowDialog() == true)
{
    // Save to saveDialog.FileName
}

// Folder browser (requires System.Windows.Forms reference or use this pattern)
var folderDialog = new OpenFileDialog
{
    Title = "Select Output Folder",
    CheckFileExists = false,
    CheckPathExists = true,
    FileName = "Select Folder"
};
// Or use Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog
```

---

## 16. Error Handling Pattern

```csharp
public void ProcessData()
{
    try
    {
        // Processing code
    }
    catch (FileNotFoundException ex)
    {
        MessageBox.Show($"File not found: {ex.FileName}", 
            "File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
    catch (FormatException ex)
    {
        MessageBox.Show($"Data format error: {ex.Message}", 
            "Format Error", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"An unexpected error occurred:\n\n{ex.Message}", 
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        System.Diagnostics.Debug.WriteLine($"Error: {ex}");
    }
}
```

---

## 17. Settings Persistence

Save module settings to JSON:

```csharp
using System.Text.Json;

public class ModuleSettings
{
    public string LastFolder { get; set; } = "";
    public bool DarkTheme { get; set; } = true;
    public double WindowWidth { get; set; } = 1200;
    
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FathomOS", "YourModule", "settings.json");
    
    public static ModuleSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<ModuleSettings>(json) ?? new();
            }
        }
        catch { }
        return new ModuleSettings();
    }
    
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
```

---

## 18. Module Discovery Process

```
1. Shell starts → ModuleManager scans ./Modules/
2. For each subdirectory:
   a. Reads ModuleInfo.json
   b. Finds FathomOS.Modules.{ModuleId}.dll
   c. Uses reflection to find IModule implementation
   d. Creates instance, calls Initialize()
3. Dashboard displays tiles
4. User clicks tile → Launch() called
5. App closing → Shutdown() called
```

---

## 19. Development Setup

### 19.1 Where to Create Your Module Project

**Create your module project at the SOLUTION ROOT (same level as FathomOS.sln):**

```
FathomOS/
├── FathomOS.sln                          ← Solution file
├── FathomOS.Core/
├── FathomOS.Shell/
├── FathomOS.Modules.SurveyListing/
├── FathomOS.Modules.SensorCalibration/
├── FathomOS.Modules.NetworkTimeSync/
└── FathomOS.Modules.YourNewModule/       ← CREATE HERE!
```

**DO NOT create a `Modules/` folder and put your project inside it!**

### 19.2 Adding Your Module to the Solution

```bash
# From solution root (where FathomOS.sln is)
cd FathomOS

# Create project at solution root
dotnet new wpflib -n FathomOS.Modules.YourModule -o FathomOS.Modules.YourModule

# Add to solution
dotnet sln FathomOS.sln add FathomOS.Modules.YourModule

# Add reference to Core
cd FathomOS.Modules.YourModule
dotnet add reference ../FathomOS.Core/FathomOS.Core.csproj
```

### 19.3 Automatic Module Discovery (NO MANUAL REGISTRATION NEEDED!)

**Good news! Shell.csproj now automatically discovers ALL modules!**

As long as your module folder:
1. Is named `FathomOS.Modules.YourModuleName`
2. Is at the solution root (same level as FathomOS.sln)
3. Contains a valid .csproj file

**It will be automatically:**
- Found and built with the solution
- Deployed to `bin/Modules/{ModuleName}/` folder
- Discovered by ModuleManager at runtime

**You do NOT need to:**
- ❌ Edit Shell.csproj
- ❌ Add ProjectReference manually
- ❌ Add post-build copy targets
- ❌ Ask the main solution chat for registration

**The solution runs fine whether it finds 0, 1, or 100 modules!**

### 19.4 How Automatic Module Discovery Works

**At Build Time:**
```
Shell.csproj contains:
  <DiscoveredModules Include="..\FathomOS.Modules.*\FathomOS.Modules.*.csproj" />
  <ProjectReference Include="@(DiscoveredModules)" />
```

1. MSBuild wildcard finds all `FathomOS.Modules.*` folders
2. Each found .csproj is added as a ProjectReference
3. All modules are built together with Shell
4. PowerShell script deploys each module to `bin/Modules/{Name}/`

**At Runtime:**
1. ModuleManager scans `Modules/` folder
2. Finds `ModuleInfo.json` in each subfolder
3. Loads the DLL and finds `IModule` implementation
4. Module appears on Dashboard sorted by `DisplayOrder`

**Folder Structure After Build:**
```
bin/Debug/net8.0-windows/
├── FathomOS.exe
├── FathomOS.Core.dll
├── Modules/
│   ├── SurveyListing/
│   │   ├── FathomOS.Modules.SurveyListing.dll
│   │   ├── ModuleInfo.json
│   │   └── Assets/icon.png
│   ├── SensorCalibration/
│   │   ├── FathomOS.Modules.SensorCalibration.dll
│   │   ├── ModuleInfo.json
│   │   └── Assets/icon.png
│   └── YourNewModule/        ← Automatically deployed!
│       ├── FathomOS.Modules.YourNewModule.dll
│       ├── ModuleInfo.json
│       └── Assets/icon.png
```

---

## 20. Common Errors and Fixes

| Error | Cause | Fix |
|-------|-------|-----|
| `CS0006: Metadata file not found` | Core didn't build | Build FathomOS.Core first |
| Module not appearing | Wrong ModuleId | Ensure ModuleId matches DLL name |
| Icon not showing | Wrong path format | Use `/FathomOS.Modules.X;component/Assets/icon.png` |
| `MC3089: Border has child` | Border with multiple children | Wrap children in Grid |
| Theme resources not found | Theme not loaded | Load in window constructor |
| `CS1678: Parameter type mismatch` | QuestPDF Canvas | Cast: `(SKCanvas)canvasObj` |
| XAML designer errors | Missing design-time data | Add `d:DataContext` |

---

## 21. Module Checklist

### Project Setup
- [ ] Target: `net8.0-windows`
- [ ] OutputType: `Library`
- [ ] References `FathomOS.Core`
- [ ] Correct NuGet versions
- [ ] NO banned packages

### Required Files
- [ ] `[ModuleName]Module.cs` implements `IModule`
- [ ] `ModuleInfo.json` with matching `moduleId`
- [ ] `Assets/icon.png` (128×128)
- [ ] `Views/MainWindow.xaml`
- [ ] `Themes/` copied from SurveyListing
- [ ] `ViewModels/RelayCommand.cs` copied
- [ ] `ViewModels/ViewModelBase.cs` created
- [ ] `Converters/Converters.cs` copied

### IModule Implementation
- [ ] All 7 properties
- [ ] All 5 methods
- [ ] ModuleId matches DLL

### Testing
- [ ] Builds without errors
- [ ] Module loads in Shell
- [ ] Icon displays
- [ ] Launch works
- [ ] Shutdown cleans up

---

## 22. API Reference - Parsers

### 22.1 NpdParser - NPD Survey File Parser

```csharp
using FathomOS.Core.Parsers;
using FathomOS.Core.Models;

var parser = new NpdParser();

// Option 1: Use predefined template
var mapping = ColumnMappingTemplates.NaviPacDefault;

// Option 2: Use custom mapping
var mapping = new ColumnMapping
{
    Name = "Custom",
    TimeColumnPattern = "Time",           // Pattern to match column header
    EastingColumnPattern = "East",
    NorthingColumnPattern = "North",
    DepthColumnPattern = "Bathy|Depth",   // Pipe = OR (matches either)
    AltitudeColumnPattern = "Alt",
    HeadingColumnPattern = "Heading|Hdg",
    HasDateTimeSplit = true,              // NaviPac: header "Time" = data "Date,Time"
    DateFormat = "dd/MM/yyyy",
    TimeFormat = "HH:mm:ss"
};

// Parse file
NpdParseResult result = parser.Parse(filePath, mapping);

// Access results
List<SurveyPoint> points = result.Points;
List<string> headers = result.HeaderColumns;
DetectedColumnIndices indices = result.DetectedMapping;

// Statistics (auto-calculated)
DateTime? startTime = result.StartTime;
DateTime? endTime = result.EndTime;
double? minDepth = result.MinDepth;
double? maxDepth = result.MaxDepth;
int totalRecords = result.TotalRecords;
int recordsWithDepth = result.RecordsWithDepth;

// Get warnings
IReadOnlyList<string> warnings = parser.ParseWarnings;

// Utility methods
List<string> allColumns = parser.GetAllColumns(filePath);
List<string> depthColumns = parser.GetAvailableDepthColumns(filePath);
```

**Available ColumnMapping Templates:**
```csharp
ColumnMappingTemplates.NaviPacDefault  // NaviPac HD11 Sprint
ColumnMappingTemplates.QINSyRov        // QINSy ROV surveys
ColumnMappingTemplates.GenericCsv      // Generic CSV with standard columns

// Find template by name
var template = ColumnMappingTemplates.FindByName("NaviPac Default");

// Get all templates
IReadOnlyList<ColumnMapping> all = ColumnMappingTemplates.AllTemplates;
```

### 22.2 RlxParser - EIVA Route File Parser

```csharp
using FathomOS.Core.Parsers;
using FathomOS.Core.Models;

var parser = new RlxParser();

// Parse route file
RouteData route = parser.Parse(filePath);

// Access route data
string routeName = route.Name;
double offset = route.Offset;
LengthUnit unit = route.CoordinateUnit;
List<RouteSegment> segments = route.Segments;

// Each segment contains:
foreach (var segment in route.Segments)
{
    double startE = segment.StartEasting;
    double startN = segment.StartNorthing;
    double endE = segment.EndEasting;
    double endN = segment.EndNorthing;
    double startKp = segment.StartKp;
    double endKp = segment.EndKp;
    double radius = segment.Radius;          // 0 = straight, +/- = arc
    int typeCode = segment.TypeCode;         // 64 = line, 128 = arc
    bool isStraight = segment.IsStraightLine;
    bool isArc = segment.IsArc;
}

// Route statistics
double totalLength = route.TotalLength;
double startKp = route.StartKp;
double endKp = route.EndKp;

// Utility methods
bool isValid = parser.IsValidRlxFile(filePath);
var summary = parser.GetFileSummary(filePath);  // (RouteName, Unit, SegmentCount)
```

### 22.3 TideParser - Tide Data Parser

```csharp
using FathomOS.Core.Parsers;
using FathomOS.Core.Models;

var parser = new TideParser();

// Parse tide file (auto-detects 7Tide or generic format)
TideData tide = parser.Parse(filePath);

// Access tide data
string software = tide.Software;          // e.g., "7Tide"
string version = tide.Version;
double? latitude = tide.Latitude;
double? longitude = tide.Longitude;
double? timeZoneOffset = tide.TimeZoneOffset;
DateTime? listingDate = tide.ListingDate;
List<TideRecord> records = tide.Records;

// Each record contains:
foreach (var record in tide.Records)
{
    DateTime dateTime = record.DateTime;
    double meters = record.TideMeters;
    double feet = record.TideFeet;
}

// Get tide at specific time (interpolated)
double? tideAtTime = tide.GetTideAtTime(dateTime);

// Quick summary without full parsing
TideFileSummary? summary = parser.GetFileSummary(filePath);
if (summary != null)
{
    int count = summary.RecordCount;
    DateTime start = summary.StartTime;
    DateTime end = summary.EndTime;
    TimeSpan duration = summary.Duration;
}
```

---

## 23. API Reference - Exporters

### 23.1 ExcelExporter - Excel Workbook Export

```csharp
using FathomOS.Core.Export;
using FathomOS.Core.Models;

// Create with options
var options = new ExcelExportOptions
{
    IncludeRawData = true,           // Raw data sheet
    IncludeCalculations = true,       // Calculation breakdown sheet
    IncludeSmoothedData = true,       // Smoothed vs original comparison
    IncludeSplineData = true,         // Spline fitted points
    IncludeIntervalPoints = true,     // Interval/DAL points
    ApplyFormatting = true            // Auto-fit columns, borders
};

var exporter = new ExcelExporter(options);

// Optional: Set report template for branding
exporter.SetTemplate(reportTemplate, logoPath);

// Export basic
exporter.Export(outputPath, points, project);

// Export with spline and interval data
exporter.Export(outputPath, points, project, splinePoints, intervalPoints);
```

**Generated Sheets:**
- Summary - Project info and statistics
- Survey Listing - Main KP, DCC, X, Y, Z output
- Raw Data - Original parsed values
- Calculations - Depth calculation breakdown
- Smoothed Comparison - Original vs smoothed
- Spline Fitted - Spline fitted points
- Interval Points - Regular interval points

### 23.2 PdfReportGenerator - PDF Report Generation

```csharp
using FathomOS.Core.Export;
using FathomOS.Core.Models;

// Create with options
var options = new PdfReportOptions
{
    IncludeDepthChart = true,         // Depth profile chart page
    IncludeFullDataTable = true,      // Full data listing pages
    IncludeCribSheet = true,          // Processing log page
    IncludePlanView = false,          // Plan view chart
    MaxDataRowsPerPage = 40           // Rows per page in data table
};

var generator = new PdfReportGenerator(options);

// Optional: Set report template for branding
generator.SetTemplate(reportTemplate, logoPath);

// Generate report
generator.Generate(outputPath, points, project);

// Generate with route and processing tracker
generator.Generate(outputPath, points, project, routeData, processingTracker);
```

**IMPORTANT - QuestPDF Canvas:**
When using QuestPDF Canvas for custom drawing, the callback parameter is `object`, not `SKCanvas`:

```csharp
using QuestPDF.Fluent;
using SkiaSharp;

column.Item().Height(200).Canvas((object canvasObj, Size size) => {
    var canvas = (SKCanvas)canvasObj;  // MUST cast to SKCanvas!
    
    using var paint = new SKPaint
    {
        Color = SKColors.Blue,
        StrokeWidth = 2,
        IsAntialias = true,
        Style = SKPaintStyle.Stroke
    };
    
    // Draw on canvas
    canvas.DrawLine(0, 0, (float)size.Width, (float)size.Height, paint);
    canvas.DrawRect(10, 10, (float)size.Width - 20, (float)size.Height - 20, paint);
});
```

### 23.3 DxfExporter - AutoCAD DXF Export

```csharp
using FathomOS.Core.Export;
using FathomOS.Core.Models;

var options = new DxfExportOptions
{
    IncludePolyline = true,
    IncludePoints = true,
    IncludeKpLabels = true,
    KpLabelInterval = 0.1,           // KP label every 100m
    TextHeight = 2.5,
    PointLayerName = "SURVEY_POINTS",
    LineLayerName = "SURVEY_LINE",
    LabelLayerName = "KP_LABELS"
};

var exporter = new DxfExporter(options);
exporter.Export(outputPath, points, project);
```

### 23.4 TextExporter - Text/CSV Export

```csharp
using FathomOS.Core.Export;
using FathomOS.Core.Models;

var options = new TextExportOptions
{
    Delimiter = "\t",                // Tab, comma, etc.
    IncludeHeader = true,
    DecimalPlaces = 3,
    DateTimeFormat = "yyyy-MM-dd HH:mm:ss",
    Columns = new[] { "KP", "DCC", "X", "Y", "Z", "DateTime" }
};

var exporter = new TextExporter(options);
exporter.Export(outputPath, points, project);
```

---

## 24. API Reference - Services

### 24.1 SmoothingService - Data Smoothing

```csharp
using FathomOS.Core.Services;
using FathomOS.Core.Models;

// Smoothing methods available
public enum SmoothingMethod
{
    None,
    MovingAverage,
    SavitzkyGolay,
    SplineFit,
    Gaussian,
    MedianFilter,
    ThresholdBased,
    KalmanFilter
}

// Create settings
var settings = new SmoothingSettings
{
    Method = SmoothingMethod.MovingAverage,
    WindowSize = 5,                  // Odd number for moving average
    PolynomialOrder = 2,             // For Savitzky-Golay
    GaussianSigma = 1.0,             // For Gaussian
    SplineTension = 0.5,             // For Spline (0=cubic, 1=linear)
    Threshold = 1.0,                 // For threshold-based
    SmoothEasting = true,
    SmoothNorthing = true,
    SmoothDepth = false
};

// Create options
var options = new SmoothingOptions
{
    Settings = settings
};

// Apply smoothing
var service = new SmoothingService(options);
SmoothingResult result = service.Smooth(points);

// Results are stored in the points themselves:
// - point.SmoothedEasting
// - point.SmoothedNorthing
// - point.SmoothedDepth

// Access stats
int smoothedCount = result.SmoothedPoints;
double maxDelta = result.MaxPositionDelta;
```

### 24.2 SplineService - Spline Fitting

```csharp
using FathomOS.Core.Services;
using FathomOS.Core.Models;

var service = new SplineService();

// Fit Catmull-Rom spline through points
double tension = 0.5;  // 0 = smooth, 1 = linear
List<SurveyPoint> splinePoints = service.FitCatmullRom(points, tension);

// Generate points at regular intervals along spline
double interval = 10.0;  // meters
List<SurveyPoint> intervalPoints = service.GenerateIntervalPoints(splinePoints, interval);
```

### 24.3 DistanceCalculator - Distance Calculations

```csharp
using FathomOS.Core.Services;

// 2D distance
double dist2d = DistanceCalculator.Calculate(x1, y1, x2, y2);

// 3D distance
double dist3d = DistanceCalculator.Calculate3D(x1, y1, z1, x2, y2, z2);

// Distance along points (cumulative)
List<double> distances = DistanceCalculator.CumulativeDistances(points);

// Total line length
double totalLength = DistanceCalculator.TotalLength(points);
```

### 24.4 TideCorrectionService - Tide Corrections

```csharp
using FathomOS.Core.Services;
using FathomOS.Core.Models;

var service = new TideCorrectionService();

// Apply tide corrections to points
service.ApplyTideCorrections(points, tideData);

// After correction, each point has:
// - point.TideCorrection (the tide value applied)
// - point.ProcessedDepth (depth with tide applied)

// Get tide at specific time
double? tide = tideData.GetTideAtTime(dateTime);
```

### 24.5 SurveyProcessor - Complete Processing Pipeline

```csharp
using FathomOS.Core.Services;
using FathomOS.Core.Models;

var processor = new SurveyProcessor();

// Process survey data through full pipeline
ProcessingResult result = processor.Process(
    points,
    project,
    routeData,      // optional
    tideData        // optional
);

// Pipeline steps:
// 1. Apply tide corrections (if tide data provided)
// 2. Calculate KP/DCC (if route data provided)
// 3. Apply smoothing (if enabled in project)
// 4. Calculate final depths
```

---

## 25. API Reference - Models

### 25.1 Project - Project Settings Container

```csharp
using FathomOS.Core.Models;

var project = new Project
{
    // Basic Info
    ProjectName = "Pipeline Survey 2024",
    ClientName = "Acme Energy",
    VesselName = "MV Explorer",
    ProcessorName = "John Smith",
    SurveyDate = DateTime.Now,
    SurveyType = SurveyType.Pipelay,
    
    // Coordinate Settings
    CoordinateSystem = "WGS84 UTM Zone 32N",
    CoordinateUnit = LengthUnit.Meters,
    KpUnit = LengthUnit.Kilometers,
    
    // File Paths
    RouteFilePath = "route.rlx",
    TideFilePath = "tide.txt",
    SurveyDataFiles = new List<string> { "survey1.npd", "survey2.npd" },
    
    // Processing Options
    ProcessingOptions = new ProcessingOptions
    {
        ApplyTideCorrections = true,
        ApplySmoothing = true,
        SmoothingMethod = SmoothingMethod.MovingAverage,
        SmoothingWindowSize = 5,
        CalculateKpDcc = true,
        BathyAltOffset = 0.5
    },
    
    // Output Options
    OutputOptions = new OutputOptions
    {
        OutputDirectory = "C:\\Output",
        ExportExcel = true,
        ExportPdf = true,
        ExportDxf = true,
        ExportText = true
    }
};

// Validation
List<string> errors = project.Validate();
if (errors.Count == 0)
{
    // Project is valid
}

// Clone for modification
Project copy = project.Clone();
```

### 25.2 SurveyType Enum

```csharp
public enum SurveyType
{
    Seabed,           // General seabed survey
    RovDynamic,       // ROV dynamic positioning survey
    Pipelay,          // Pipeline laying survey
    EFL,              // End of Flexible Lay
    SFL,              // Start of Flexible Lay
    Umbilical,        // Umbilical lay survey
    Cable,            // Cable lay survey
    Touchdown,        // Touchdown monitoring
    AsBuilt,          // As-built survey
    PreLay,           // Pre-lay route survey
    PostLay,          // Post-lay inspection
    FreeSpan,         // Free span survey
    Inspection,       // General inspection
    Custom            // Custom survey type
}
```

### 25.3 LengthUnit Enum

```csharp
public enum LengthUnit
{
    Meters,
    Feet,
    Kilometers,
    NauticalMiles,
    UsSurveyFeet
}

// Extension methods
string displayName = LengthUnit.Meters.GetDisplayName();  // "Meters (m)"
string abbreviation = LengthUnit.Meters.GetAbbreviation(); // "m"

// Conversion
double meters = 100;
double feet = meters * LengthUnit.Meters.ConversionFactor(LengthUnit.Feet);
```

### 25.4 ReportTemplate - PDF/Excel Branding

```csharp
using FathomOS.Core.Models;

var template = new ReportTemplate
{
    Company = new CompanyInfo
    {
        Name = "S7 Survey Solutions",
        Address = "123 Survey Street",
        Phone = "+1 234 567 8900",
        Email = "info@s7survey.com",
        Website = "www.s7survey.com"
    },
    Header = new HeaderSettings
    {
        Title = "SURVEY LISTING REPORT",
        ShowLogo = true,
        ShowCompanyName = true,
        LogoWidth = 80
    },
    Footer = new FooterSettings
    {
        LeftText = "{ProjectName}",           // Placeholders supported
        RightText = "Generated: {GeneratedDate}",
        ShowPageNumbers = true
    },
    Colors = new ColorScheme
    {
        PrimaryColor = "#1E3A5F",
        SecondaryColor = "#4A90D9",
        AccentColor = "#2ECC71",
        HeaderBackground = "#1E3A5F",
        HeaderText = "#FFFFFF"
    }
};

// Available placeholders:
// {ProjectName}, {ClientName}, {VesselName}, {SurveyDate}, 
// {GeneratedDate}, {ProcessorName}, {CoordinateSystem}

// Use with exporters
excelExporter.SetTemplate(template, logoPath);
pdfGenerator.SetTemplate(template, logoPath);
```

---

## 26. Current Modules

| Module | ModuleId | Category | DisplayOrder |
|--------|----------|----------|--------------|
| Survey Listing Generator | `SurveyListing` | Data Processing | 10 |
| Sound Velocity Profiler | `SoundVelocity` | Data Processing | 15 |
| Sensor Calibration Tool | `SensorCalibration` | Quality Control | 20 |
| Network Time Sync | `NetworkTimeSync` | Utilities | 30 |

---

## 27. Version Information

```csharp
using FathomOS.Core;

string version = AppInfo.VersionString;      // "1.0.26"
string display = AppInfo.VersionDisplay;     // "v1.0.26"
Version ver = AppInfo.Version;               // Version object
```

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Dec 2024 | Initial specification |
| 2.0 | Dec 2024 | Added package restrictions, theme guidance |
| 2.1 | Dec 2024 | Added RelayCommand, ViewModelBase, Converters, MVVM patterns, QuestPDF/ClosedXML/netDxf usage, settings persistence, error handling |
| 2.2 | Dec 2024 | Added comprehensive API Reference for Parsers, Exporters, Services, and Models |
| 2.3 | Dec 2024 | Moved MahApps, OxyPlot, HelixToolkit to Core - modules no longer need to add these packages. Added OxyPlot and HelixToolkit usage examples. |
| 2.4 | Dec 2024 | Automatic module discovery - Shell auto-discovers all modules, no manual registration needed. |
| 2.5 | Dec 2024 | **MODULE GROUPS** - Added support for grouping related modules under a single dashboard tile with navigation. Create `FathomOS.ModuleGroups.GroupName\` folders to group modules. |

---

**END OF MODULE INTEGRATION GUIDE**
