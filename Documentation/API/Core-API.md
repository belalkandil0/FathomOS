# FathomOS Core API Reference

**Version:** 1.0.0
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Models](#models)
3. [Parsers](#parsers)
4. [Calculations](#calculations)
5. [Export](#export)
6. [Services](#services)
7. [Certificates](#certificates)

---

## Overview

FathomOS.Core is the shared library that provides common functionality used by all modules. It contains data models, file parsers, calculation engines, export functionality, and service abstractions.

### NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| MathNet.Numerics | 5.0.0 | Mathematical operations |
| ClosedXML | 0.102.1 | Excel file creation |
| netDxf | 3.0.1 | DXF/CAD file export |
| QuestPDF | 2024.3.0 | PDF report generation |
| MahApps.Metro | 2.4.10 | UI framework |
| OxyPlot.Wpf | 2.1.2 | Charting |
| HelixToolkit.Wpf.SharpDX | 2.24.0 | 3D visualization |
| Microsoft.Data.Sqlite | 8.0.0 | Certificate storage |

---

## Models

### Project

Represents a survey project with metadata and settings.

```csharp
namespace FathomOS.Core.Models;

public class Project
{
    public string ProjectName { get; set; }
    public string ClientName { get; set; }
    public string VesselName { get; set; }
    public string ProjectLocation { get; set; }
    public DateTime ProjectDate { get; set; }
    public List<SurveyFix> SurveyFixes { get; set; }
    public ProcessingOptions ProcessingOptions { get; set; }
}
```

### SurveyPoint

Represents an individual survey data point.

```csharp
public class SurveyPoint
{
    // Identifiers
    public int RecordNumber { get; set; }
    public DateTime DateTime { get; set; }

    // Original coordinates
    public double Easting { get; set; }
    public double Northing { get; set; }

    // Smoothed coordinates
    public double? SmoothedEasting { get; set; }
    public double? SmoothedNorthing { get; set; }

    // Depth values
    public double? Depth { get; set; }
    public double? CorrectedDepth { get; set; }
    public double? Altitude { get; set; }
    public string? DepthSource { get; set; }

    // Route alignment
    public double? Kp { get; set; }
    public double? Offset { get; set; }

    // Navigation
    public double? Heading { get; set; }

    // Computed values
    public double X => SmoothedEasting ?? Easting;
    public double Y => SmoothedNorthing ?? Northing;
    public double? CalculatedZ { get; set; }

    // Validation
    public bool HasValidCoordinates => Easting != 0 && Northing != 0;
    public bool HasValidDepth => Depth.HasValue;
}
```

### RouteData

Represents route alignment information.

```csharp
public class RouteData
{
    public string Name { get; set; }
    public List<RouteSegment> Segments { get; set; }
    public double TotalLength { get; }
    public double StartKp { get; }
    public double EndKp { get; }
}
```

### RouteSegment

Represents a single segment of a route.

```csharp
public class RouteSegment
{
    public double StartKp { get; set; }
    public double EndKp { get; set; }
    public double StartEasting { get; set; }
    public double StartNorthing { get; set; }
    public double EndEasting { get; set; }
    public double EndNorthing { get; set; }
    public double Radius { get; set; }  // 0 for straight
    public bool IsStraight => Radius == 0;
    public bool IsClockwise { get; set; }

    public (double E, double N) GetArcCenter();
}
```

### TideData

Represents tide correction data.

```csharp
public class TideData
{
    public List<TideReading> Readings { get; set; }

    public double? GetTideAtTime(DateTime time);
}

public class TideReading
{
    public DateTime Time { get; set; }
    public double Height { get; set; }
}
```

### ColumnMapping

Configuration for parsing data columns.

```csharp
public class ColumnMapping
{
    public string TimeColumnPattern { get; set; } = "Time";
    public string EastingColumnPattern { get; set; } = "East";
    public string NorthingColumnPattern { get; set; } = "North";
    public string DepthColumnPattern { get; set; } = "Depth|Bathy";
    public string AltitudeColumnPattern { get; set; } = "Altitude|Alt";
    public string HeadingColumnPattern { get; set; } = "Heading|Hdg";

    public int TimeColumnIndex { get; set; } = -1;
    public int EastingColumnIndex { get; set; } = -1;
    public int NorthingColumnIndex { get; set; } = -1;
    public int DepthColumnIndex { get; set; } = -1;
    public int AltitudeColumnIndex { get; set; } = -1;
    public int HeadingColumnIndex { get; set; } = -1;

    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string TimeFormat { get; set; } = "HH:mm:ss";
    public bool HasDateTimeSplit { get; set; } = true;
}
```

---

## Parsers

### NpdParser

Parses NaviPac NPD survey data files.

```csharp
namespace FathomOS.Core.Parsers;

public class NpdParser
{
    public IReadOnlyList<string> ParseWarnings { get; }

    public NpdParseResult Parse(string filePath, ColumnMapping mapping);
    public List<string> GetAvailableDepthColumns(string filePath);
    public List<string> GetAllColumns(string filePath);
}
```

**Usage:**
```csharp
var parser = new NpdParser();
var mapping = new ColumnMapping
{
    TimeColumnPattern = "Time",
    EastingColumnPattern = "East",
    NorthingColumnPattern = "North",
    DepthColumnPattern = "Bathy",
    HasDateTimeSplit = true
};

var result = parser.Parse("survey.npd", mapping);

if (result.Points.Count > 0)
{
    Console.WriteLine($"Parsed {result.TotalRecords} points");
    Console.WriteLine($"Time range: {result.StartTime} to {result.EndTime}");
    Console.WriteLine($"Depth range: {result.MinDepth:F2} to {result.MaxDepth:F2}");
}

// Check for warnings
foreach (var warning in parser.ParseWarnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

### NpdParseResult

Result from parsing an NPD file.

```csharp
public class NpdParseResult
{
    public string SourceFile { get; set; }
    public List<string> HeaderColumns { get; set; }
    public DetectedColumnIndices DetectedMapping { get; set; }
    public List<SurveyPoint> Points { get; set; }

    // Statistics
    public DateTime? StartTime { get; }
    public DateTime? EndTime { get; }
    public double? MinDepth { get; }
    public double? MaxDepth { get; }
    public int TotalRecords { get; }
    public int RecordsWithDepth { get; }

    public void CalculateStatistics();
}
```

### RlxParser

Parses route alignment files.

```csharp
public class RlxParser
{
    public RouteData Parse(string filePath);
}
```

### TideParser

Parses tide correction files.

```csharp
public class TideParser
{
    public TideData Parse(string filePath);
}
```

---

## Calculations

### DepthCalculator

Calculates final depths with offsets and corrections.

```csharp
namespace FathomOS.Core.Calculations;

public class DepthCalculator
{
    public DepthCalculator(ProcessingOptions options);

    public double CalculateSeabedDepth(double bathyDepth, double rovAltitude);
    public double CalculateRovDepth(double bathyDepth);
    public double ApplyExaggeration(double depth);

    public void ProcessAll(
        IList<SurveyPoint> points,
        SurveyType surveyType,
        IProgress<int>? progress = null);

    public DepthStatistics GetStatistics(IList<SurveyPoint> points);
}
```

**Usage:**
```csharp
var options = new ProcessingOptions
{
    BathyToAltimeterOffset = 0.5,
    BathyToRovRefOffset = 0.3,
    DepthExaggeration = 10.0,
    ApplyVerticalOffsets = true
};

var calculator = new DepthCalculator(options);
calculator.ProcessAll(points, SurveyType.Seabed);

var stats = calculator.GetStatistics(points);
Console.WriteLine($"Mean depth: {stats.MeanDepth:F2}m");
Console.WriteLine($"Std dev: {stats.StdDeviation:F2}m");
```

### DepthStatistics

Statistical analysis of depth data.

```csharp
public class DepthStatistics
{
    public double MinDepth { get; set; }
    public double MaxDepth { get; set; }
    public double MeanDepth { get; set; }
    public double DepthRange { get; set; }
    public double StdDeviation { get; set; }
    public int RecordCount { get; set; }
}
```

### KpCalculator

Calculates kilometer points along a route.

```csharp
public class KpCalculator
{
    public void CalculateKp(IList<SurveyPoint> points, RouteData route);
}
```

### UnitConverter

Unit conversion utilities.

```csharp
public static class UnitConverter
{
    public static double MetersToFeet(double meters);
    public static double FeetToMeters(double feet);
    public static double MetersToFathoms(double meters);
    public static double FathomsToMeters(double fathoms);
    public static double DegreesToRadians(double degrees);
    public static double RadiansToDegrees(double radians);
}
```

### TideCorrector

Applies tide corrections to survey data.

```csharp
public class TideCorrector
{
    public void ApplyCorrections(
        IList<SurveyPoint> points,
        TideData tideData,
        TideCorrectionMethod method = TideCorrectionMethod.Linear);
}

public enum TideCorrectionMethod
{
    Linear,
    Spline,
    Nearest
}
```

---

## Export

### DxfExporter

Exports survey data to DXF (CAD) format.

```csharp
namespace FathomOS.Core.Export;

public class DxfExporter
{
    public DxfExporter(DxfExportOptions? options = null);

    public void Export(
        string filePath,
        IList<SurveyPoint> points,
        RouteData? route,
        Project project,
        IList<SurveyPoint>? splinePoints = null,
        IList<(double X, double Y, double Z, double Distance)>? intervalPoints = null);

    public void Export3DPolyline(
        string filePath,
        IList<SurveyPoint> points,
        Project project,
        double depthExaggeration = 10.0);
}
```

### DxfExportOptions

```csharp
public class DxfExportOptions
{
    public bool IncludeRoute { get; set; } = true;
    public bool IncludePoints { get; set; } = true;
    public bool IncludeKpLabels { get; set; } = true;
    public bool IncludeFixes { get; set; } = true;
    public bool IncludeTitleBlock { get; set; } = true;
    public bool Include3DTrack { get; set; } = false;

    public double KpLabelInterval { get; set; } = 1.0;
    public double TextHeight { get; set; } = 10.0;
    public double DepthExaggeration { get; set; } = 10.0;
    public int MaxPointMarkers { get; set; } = 500;
}
```

**Usage:**
```csharp
var options = new DxfExportOptions
{
    IncludeRoute = true,
    IncludeKpLabels = true,
    KpLabelInterval = 0.5,
    Include3DTrack = true,
    DepthExaggeration = 10.0
};

var exporter = new DxfExporter(options);
exporter.Export("output.dxf", points, route, project);
```

### ExcelExporter

Exports data to Excel format.

```csharp
public class ExcelExporter
{
    public void Export(string filePath, IList<SurveyPoint> points, Project project);
    public void ExportWithCharts(string filePath, IList<SurveyPoint> points, Project project);
}
```

### TextExporter

Exports data to text/CSV format.

```csharp
public class TextExporter
{
    public void Export(string filePath, IList<SurveyPoint> points, string delimiter = ",");
    public void ExportTabDelimited(string filePath, IList<SurveyPoint> points);
}
```

### PdfReportGenerator

Generates PDF reports.

```csharp
public class PdfReportGenerator
{
    public void Generate(
        string filePath,
        Project project,
        IList<SurveyPoint> points,
        string? brandLogo = null);
}
```

---

## Services

### SurveyProcessor

Main processing service.

```csharp
namespace FathomOS.Core.Services;

public class SurveyProcessor
{
    public ProcessingResult Process(
        IList<SurveyPoint> points,
        RouteData? route,
        TideData? tideData,
        ProcessingOptions options);
}
```

### SmoothingService

Data smoothing algorithms.

```csharp
public class SmoothingService
{
    public void ApplyMovingAverage(IList<SurveyPoint> points, int windowSize);
    public void ApplySavitzkyGolay(IList<SurveyPoint> points, int windowSize, int order);
    public void ApplyMedianFilter(IList<SurveyPoint> points, int windowSize);
}
```

### SplineService

Spline fitting for data interpolation.

```csharp
public class SplineService
{
    public List<SurveyPoint> FitSpline(IList<SurveyPoint> points, int outputPoints);
    public List<(double X, double Y, double Z, double Distance)> GenerateIntervalPoints(
        IList<SurveyPoint> points,
        double interval);
}
```

### DistanceCalculator

Distance and geometry calculations.

```csharp
public class DistanceCalculator
{
    public static double Distance2D(double x1, double y1, double x2, double y2);
    public static double Distance3D(double x1, double y1, double z1, double x2, double y2, double z2);
    public static double CalculateTotalDistance(IList<SurveyPoint> points);
}
```

---

## Certificates

### CertificateHelper

Helper class for creating processing certificates.

```csharp
namespace FathomOS.Core.Certificates;

public static class CertificateHelper
{
    // Delegates set by Shell
    public static Func<string?, Window?, SignatoryInfo?> ShowSignatoryDialog { get; set; }
    public static Action<ProcessingCertificate, bool, string?, Window?> ShowCertificateViewer { get; set; }
    public static Action<LicenseManager, string?, Window?> ShowCertificateManager { get; set; }

    // Create with dialog
    public static async Task<ProcessingCertificate?> CreateWithDialogAsync(
        LicenseManager licenseManager,
        string moduleId,
        string moduleCertificateCode,
        string moduleVersion,
        string projectName,
        Dictionary<string, string>? processingData = null,
        List<string>? inputFiles = null,
        List<string>? outputFiles = null,
        string? projectLocation = null,
        string? vessel = null,
        string? client = null,
        Window? owner = null);

    // Create silently (batch processing)
    public static async Task<ProcessingCertificate> CreateSilentAsync(...);

    // Utilities
    public static string ComputeFileHash(string filePath);
    public static QuickCertificateBuilder QuickCreate(LicenseManager licenseManager);
}
```

### QuickCertificateBuilder

Fluent builder for certificate creation.

```csharp
public class QuickCertificateBuilder
{
    public QuickCertificateBuilder ForModule(string moduleId, string certificateCode, string version);
    public QuickCertificateBuilder WithProject(string name, string? location = null);
    public QuickCertificateBuilder WithVessel(string vessel);
    public QuickCertificateBuilder WithClient(string client);
    public QuickCertificateBuilder AddData(string key, string value);
    public QuickCertificateBuilder WithData(Dictionary<string, string> data);
    public QuickCertificateBuilder AddInputFile(string filePath);
    public QuickCertificateBuilder AddInputFiles(IEnumerable<string> filePaths);
    public QuickCertificateBuilder AddOutputFile(string filePath);
    public QuickCertificateBuilder AddOutputFiles(IEnumerable<string> filePaths);

    public async Task<ProcessingCertificate?> CreateWithDialogAsync(Window? owner = null);
    public async Task<ProcessingCertificate> CreateAsync(string signatoryName, string companyName, string? signatoryTitle = null);
}
```

**Usage:**
```csharp
var certificate = await CertificateHelper.QuickCreate(licenseManager)
    .ForModule("SurveyListing", "SL", "1.0.45")
    .WithProject("Pipeline Survey", "North Sea")
    .WithVessel("MV Survey One")
    .WithClient("Example Oil Co")
    .AddData("Route File", "Pipeline_Route.rlx")
    .AddData("Total Points", "15,432")
    .AddData("Survey Length", "45.7 km")
    .AddInputFile("survey_data.npd")
    .AddInputFile("tide_data.tide")
    .AddOutputFile("survey_listing.xlsx")
    .AddOutputFile("survey_track.dxf")
    .CreateWithDialogAsync(this);
```

### SignatoryInfo

Information about certificate signatory.

```csharp
public class SignatoryInfo
{
    public string SignatoryName { get; set; }
    public string SignatoryTitle { get; set; }
    public string CompanyName { get; set; }
    public bool Cancelled { get; set; }
}
```

---

## Related Documentation

- [Module API](Module-API.md) - IModule interface
- [Shell API](Shell-API.md) - Shell services
- [Developer Guide](../DeveloperGuide.md) - Development guide

---

*Copyright 2026 Fathom OS. All rights reserved.*
