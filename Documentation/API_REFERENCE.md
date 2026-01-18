# Fathom OS API Reference

**Version:** 1.0.45
**Last Updated:** January 2026

---

## Overview

This document provides a consolidated reference for all public APIs in Fathom OS. For detailed documentation on specific areas, see the linked detailed references.

---

## Table of Contents

1. [Core Services](#core-services)
2. [Module Interfaces](#module-interfaces)
3. [Event System](#event-system)
4. [Repository Pattern](#repository-pattern)
5. [Detailed API References](#detailed-api-references)

---

## Core Services

### ISmoothingService

Contract for smoothing services providing various signal processing algorithms.

```csharp
namespace FathomOS.Core.Services;

public interface ISmoothingService
{
    /// <summary>
    /// Applies a simple moving average filter to smooth data.
    /// </summary>
    /// <param name="data">The input data array to smooth.</param>
    /// <param name="windowSize">The size of the averaging window (should be odd).</param>
    /// <returns>A new array containing the smoothed data.</returns>
    double[] MovingAverage(double[] data, int windowSize);

    /// <summary>
    /// Applies Savitzky-Golay smoothing filter to preserve peaks and signal shape.
    /// </summary>
    /// <param name="data">The input data array to smooth.</param>
    /// <param name="windowSize">The size of the smoothing window (must be odd, >= polyOrder + 2).</param>
    /// <param name="polyOrder">The order of the polynomial to fit (typically 2 or 4).</param>
    /// <returns>A new array containing the smoothed data.</returns>
    double[] SavitzkyGolay(double[] data, int windowSize, int polyOrder);

    /// <summary>
    /// Applies a median filter to remove outliers and spike noise.
    /// </summary>
    /// <param name="data">The input data array to filter.</param>
    /// <param name="windowSize">The size of the filter window (should be odd).</param>
    /// <returns>A new array containing the filtered data.</returns>
    double[] MedianFilter(double[] data, int windowSize);

    /// <summary>
    /// Applies Gaussian smoothing using a Gaussian-weighted kernel.
    /// </summary>
    /// <param name="data">The input data array to smooth.</param>
    /// <param name="sigma">The standard deviation of the Gaussian kernel.</param>
    /// <returns>A new array containing the smoothed data.</returns>
    double[] GaussianSmooth(double[] data, double sigma);

    /// <summary>
    /// Applies Kalman smoothing for optimal state estimation.
    /// </summary>
    /// <param name="data">The input data array to smooth.</param>
    /// <param name="processNoise">Process noise covariance (Q).</param>
    /// <param name="measurementNoise">Measurement noise covariance (R).</param>
    /// <returns>A new array containing the smoothed data.</returns>
    double[] KalmanSmooth(double[] data, double processNoise, double measurementNoise);
}
```

**Implementation:** `SmoothingService`

**Usage Example:**
```csharp
var smoothingService = new SmoothingService();
var smoothedData = smoothingService.MovingAverage(rawData, windowSize: 5);
```

---

### ICacheService

Contract for a caching service providing in-memory caching with expiration support.

```csharp
namespace FathomOS.Core.Services;

public interface ICacheService
{
    /// <summary>
    /// Retrieves a cached value by key asynchronously.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Stores a value in the cache asynchronously.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Removes a value from the cache asynchronously.
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Gets a cached value or creates it using the provided factory if not found.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
}
```

**Implementation:** `CacheService`

**Usage Example:**
```csharp
var project = await _cacheService.GetOrCreateAsync(
    $"project:{projectId}",
    () => LoadProjectAsync(projectId),
    TimeSpan.FromMinutes(10));
```

---

### IApprovalService

Contract for approval workflow management with signatory support.

```csharp
namespace FathomOS.Core.Services;

public interface IApprovalService
{
    // Approval Workflow
    Task<ApprovalResult> RequestApprovalAsync(ApprovalRequest request);
    Task<ApprovalResult> AddSignatureAsync(string requestId, Signatory signatory, string? comments = null);
    Task<ApprovalResult> RejectApprovalAsync(string requestId, string reason, Signatory? rejectedBy = null);
    Task<ApprovalResult?> GetApprovalStatusAsync(string requestId);

    // Signature Validation
    Task<bool> ValidateSignatureAsync(byte[] signatureData, string signatoryId);
    Task<bool> ValidateSignatoryAsync(string signatoryId);

    // Signatory Management
    Task<IEnumerable<Signatory>> GetSignatoriesAsync();
    Task<Signatory?> GetSignatoryByIdAsync(string id);
    Task<Signatory?> GetDefaultSignatoryAsync();
    Task<IEnumerable<Signatory>> GetRecentSignatoriesAsync(int count = 5);
    Task SaveSignatoryAsync(Signatory signatory);
    Task SetDefaultSignatoryAsync(string signatoryId);
    Task DeleteSignatoryAsync(string id);
    Task RecordSignatoryUsageAsync(string signatoryId);

    // Events
    event EventHandler<Signatory>? SignatoryChanged;
    event EventHandler<string>? SignatoryDeleted;
    event EventHandler<ApprovalResult>? ApprovalCompleted;
}
```

**Implementation:** `ApprovalService`

---

### IAutoSaveService

Contract for auto-save functionality to protect user work from data loss.

```csharp
namespace FathomOS.Core.Services;

public interface IAutoSaveService : IDisposable
{
    /// <summary>
    /// Interval between auto-saves in seconds.
    /// </summary>
    int IntervalSeconds { get; set; }

    /// <summary>
    /// Whether auto-save is enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Marks the document as having unsaved changes.
    /// </summary>
    void MarkDirty();

    /// <summary>
    /// Marks the document as saved (clears dirty flag).
    /// </summary>
    void MarkClean();

    /// <summary>
    /// Whether there are unsaved changes.
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// Starts the auto-save timer.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the auto-save timer.
    /// </summary>
    void Stop();

    /// <summary>
    /// Event fired when auto-save should be performed.
    /// </summary>
    event EventHandler? AutoSaveRequested;
}
```

**Implementation:** `AutoSaveService`

**Usage Example:**
```csharp
_autoSaveService = new AutoSaveService { IntervalSeconds = 60, IsEnabled = true };
_autoSaveService.AutoSaveRequested += (s, e) => SaveProject();
_autoSaveService.Start();

// When user makes changes
_autoSaveService.MarkDirty();

// After manual save
_autoSaveService.MarkClean();
```

---

## Module Interfaces

### IModule

The core contract that all FathomOS modules must implement.

```csharp
namespace FathomOS.Core.Interfaces;

public interface IModule
{
    // Identity Properties
    string ModuleId { get; }
    string DisplayName { get; }
    string Description { get; }
    Version Version { get; }
    string IconResource { get; }
    string Category { get; }
    int DisplayOrder { get; }

    // Lifecycle Methods
    void Initialize();
    void Launch(Window? owner = null);
    void Shutdown();

    // File Handling
    bool CanHandleFile(string filePath);
    void OpenFile(string filePath);
}
```

**Detailed Documentation:** [Module API Reference](API/Module-API.md)

---

### IModuleMetadata

Module metadata from ModuleInfo.json files.

```csharp
public interface IModuleMetadata
{
    string ModuleId { get; }
    string DisplayName { get; }
    string Description { get; }
    string Version { get; }
    string Author { get; }
    string Category { get; }
    int DisplayOrder { get; }
    string Icon { get; }
    List<string> SupportedFileTypes { get; }
    List<string> Dependencies { get; }
    string MinimumShellVersion { get; }
    List<string> Features { get; }
    string CertificateCode { get; }
    string CertificateTitle { get; }
    string CertificateStatement { get; }
}
```

---

## Event System

### IEventAggregator

Cross-module event publishing and subscription.

```csharp
namespace FathomOS.Core.Services;

public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    void Publish<TEvent>(TEvent eventData);

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    void Subscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// Unsubscribes from events of a specific type.
    /// </summary>
    void Unsubscribe<TEvent>(Action<TEvent> handler);
}
```

**Common Events:**

| Event | Description |
|-------|-------------|
| `ThemeChangedEvent` | Raised when application theme changes |
| `ProjectChangedEvent` | Raised when project is loaded/saved |
| `LicenseChangedEvent` | Raised when license status changes |
| `ModuleLoadedEvent` | Raised when a module is loaded |

**Usage Example:**
```csharp
// Subscribe
_eventAggregator.Subscribe<ThemeChangedEvent>(OnThemeChanged);

// Publish
_eventAggregator.Publish(new ProjectChangedEvent
{
    ProjectId = project.Id,
    ChangeType = ChangeType.Saved
});

// Unsubscribe (in Shutdown)
_eventAggregator.Unsubscribe<ThemeChangedEvent>(OnThemeChanged);
```

---

## Repository Pattern

### Project Data Access

```csharp
namespace FathomOS.Core.Services;

public interface IProjectService
{
    Task<Project?> LoadProjectAsync(string filePath);
    Task SaveProjectAsync(string filePath, Project project);
    Task<IEnumerable<string>> GetRecentProjectsAsync();
    void AddToRecentProjects(string filePath);
}
```

### Survey Data Access

```csharp
public interface ISurveyDataRepository
{
    Task<IEnumerable<SurveyPoint>> LoadAsync(string filePath, ColumnMapping mapping);
    Task SaveAsync(string filePath, IEnumerable<SurveyPoint> points);
    Task<ColumnMapping> DetectColumnsAsync(string filePath);
}
```

---

## Detailed API References

For comprehensive documentation on specific areas:

| Document | Description |
|----------|-------------|
| [Core API Reference](API/Core-API.md) | Models, Parsers, Calculations, Export, Services |
| [Module API Reference](API/Module-API.md) | IModule interface, discovery, groups |
| [Shell API Reference](API/Shell-API.md) | Shell services, theme, licensing |

---

## Quick Reference Tables

### Smoothing Methods

| Method | Use Case | Parameters |
|--------|----------|------------|
| MovingAverage | General noise reduction | windowSize (odd) |
| SavitzkyGolay | Peak preservation | windowSize, polyOrder |
| MedianFilter | Spike/outlier removal | windowSize |
| GaussianSmooth | Natural smoothing | sigma |
| KalmanSmooth | Optimal estimation | processNoise, measurementNoise |

### Export Formats

| Format | Class | Output |
|--------|-------|--------|
| Excel | `ExcelExporter` | .xlsx |
| DXF/CAD | `DxfExporter` | .dxf |
| PDF | `PdfReportGenerator` | .pdf |
| Text | `TextExporter` | .txt, .csv |
| CAD Script | `CadScriptExporter` | .scr |

### File Types by Module

| Module | File Types |
|--------|------------|
| Survey Listing | .npd, .rlx, .s7p, .tide |
| Survey Logbook | .slog, .slogz, .npc, .wp2, .out2 |
| Sound Velocity | .000, .svp, .ctd, .bp3, .csv |
| Equipment Inventory | .xlsx, .csv, .json |
| USBL Verification | .npd, .csv, .txt, .usblproj |

---

*Copyright 2026 Fathom OS. All rights reserved.*
