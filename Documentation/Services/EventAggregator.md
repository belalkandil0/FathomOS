# Event Aggregator Documentation

**Version:** 1.0.0
**Last Updated:** January 2026

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Publishing Events](#publishing-events)
4. [Subscribing to Events](#subscribing-to-events)
5. [Built-in Events](#built-in-events)
6. [Creating Custom Events](#creating-custom-events)
7. [Best Practices](#best-practices)
8. [API Reference](#api-reference)

---

## Overview

The Event Aggregator provides a decoupled publish/subscribe messaging system for cross-module communication in FathomOS. It allows modules to communicate without direct references to each other.

### Key Features

- Loosely coupled messaging
- Type-safe events
- UI thread support
- Automatic subscription cleanup
- Thread-safe operations

### Use Cases

- Notify modules when a project is opened
- Broadcast when data is exported
- Signal when authentication changes
- Alert modules of theme changes

---

## Architecture

### Service Location

```
FathomOS.Shell
└── Services
    └── EventAggregator.cs

FathomOS.Core
└── Interfaces
    └── IEventAggregator.cs
```

### Message Flow

```
[Publisher Module]
       |
       | Publish(event)
       v
[Event Aggregator]
       |
       | Notify subscribers
       |
   +---+---+---+
   |   |   |   |
   v   v   v   v
[Module A] [Module B] [Module C] [Module D]
```

---

## Publishing Events

### Basic Publishing

```csharp
// Define an event class
public class ProjectOpenedEvent
{
    public string ProjectPath { get; init; }
    public string ModuleId { get; init; }
    public DateTime OpenedAt { get; init; } = DateTime.UtcNow;
}

// Publish the event
_eventAggregator?.Publish(new ProjectOpenedEvent
{
    ProjectPath = "/path/to/project.s7p",
    ModuleId = "SurveyListing"
});
```

### Publishing from ViewModel

```csharp
public class MainViewModel
{
    private readonly IEventAggregator _eventAggregator;

    public void OnExportCompleted(string outputPath)
    {
        _eventAggregator?.Publish(new DataExportedEvent
        {
            ExportPath = outputPath,
            Format = "Excel",
            RecordCount = DataItems.Count
        });
    }
}
```

---

## Subscribing to Events

### Basic Subscription

```csharp
public class MyModule : IModule
{
    private IDisposable? _subscription;
    private readonly IEventAggregator? _eventAggregator;

    public void Initialize()
    {
        _subscription = _eventAggregator?.Subscribe<ProjectOpenedEvent>(OnProjectOpened);
    }

    private void OnProjectOpened(ProjectOpenedEvent evt)
    {
        Console.WriteLine($"Project opened: {evt.ProjectPath}");
    }

    public void Shutdown()
    {
        _subscription?.Dispose();
    }
}
```

### UI Thread Subscription

For updating UI elements, use the UI thread subscription:

```csharp
_subscription = _eventAggregator?.SubscribeOnUIThread<DataExportedEvent>(evt =>
{
    // Safe to update UI here
    StatusText = $"Exported {evt.RecordCount} records";
    ProgressBar.Visibility = Visibility.Collapsed;
});
```

### Multiple Subscriptions

```csharp
private readonly List<IDisposable> _subscriptions = new();

public void Initialize()
{
    _subscriptions.Add(_eventAggregator?.Subscribe<ProjectOpenedEvent>(OnProjectOpened));
    _subscriptions.Add(_eventAggregator?.Subscribe<DataExportedEvent>(OnDataExported));
    _subscriptions.Add(_eventAggregator?.Subscribe<ThemeChangedEvent>(OnThemeChanged));
}

public void Shutdown()
{
    foreach (var subscription in _subscriptions)
    {
        subscription?.Dispose();
    }
    _subscriptions.Clear();
}
```

---

## Built-in Events

### Authentication Events

```csharp
/// <summary>
/// Raised when user authentication state changes.
/// </summary>
public class AuthenticationChangedEvent
{
    public IUser? User { get; init; }
    public bool IsAuthenticated => User != null;
}
```

### Theme Events

```csharp
/// <summary>
/// Raised when application theme changes.
/// </summary>
public class ThemeChangedEvent
{
    public AppTheme NewTheme { get; init; }
    public AppTheme OldTheme { get; init; }
}
```

### Module Events

```csharp
/// <summary>
/// Raised when a module is launched.
/// </summary>
public class ModuleLaunchedEvent
{
    public string ModuleId { get; init; }
    public DateTime LaunchedAt { get; init; }
}

/// <summary>
/// Raised when a module is closed.
/// </summary>
public class ModuleClosedEvent
{
    public string ModuleId { get; init; }
}
```

---

## Creating Custom Events

### Event Class Guidelines

1. Use immutable properties (`init` accessors)
2. Include relevant data for handlers
3. Add timestamp if timing matters
4. Keep events focused and small

### Example Event Classes

```csharp
/// <summary>
/// Raised when a project is opened.
/// </summary>
public class ProjectOpenedEvent
{
    public string ProjectPath { get; init; }
    public string ProjectName { get; init; }
    public string ModuleId { get; init; }
    public DateTime OpenedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Raised when data export completes.
/// </summary>
public class DataExportedEvent
{
    public string ExportPath { get; init; }
    public string Format { get; init; }
    public int RecordCount { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Raised when a certificate is generated.
/// </summary>
public class CertificateCreatedEvent
{
    public string CertificateId { get; init; }
    public string ModuleId { get; init; }
    public string ProjectName { get; init; }
}

/// <summary>
/// Raised when processing completes.
/// </summary>
public class ProcessingCompletedEvent
{
    public string ModuleId { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int ItemsProcessed { get; init; }
}

/// <summary>
/// Raised when equipment status changes.
/// </summary>
public class EquipmentStatusChangedEvent
{
    public string EquipmentId { get; init; }
    public string OldStatus { get; init; }
    public string NewStatus { get; init; }
    public string ChangedBy { get; init; }
}
```

---

## Best Practices

### 1. Always Dispose Subscriptions

```csharp
public void Shutdown()
{
    // Always dispose subscriptions to prevent memory leaks
    _subscription?.Dispose();
}
```

### 2. Use Null Conditional Operator

```csharp
// EventAggregator may be null in standalone testing
_eventAggregator?.Publish(new MyEvent());
_subscription = _eventAggregator?.Subscribe<MyEvent>(OnMyEvent);
```

### 3. Keep Event Handlers Fast

Event handlers should complete quickly. For long operations, use Task.Run:

```csharp
private void OnDataImported(DataImportedEvent evt)
{
    // Quick operation - OK inline
    LogService.Log($"Data imported: {evt.RecordCount} records");

    // Long operation - run async
    Task.Run(() => ProcessImportedData(evt));
}
```

### 4. Use UI Thread for UI Updates

```csharp
// Wrong - may cause cross-thread exception
_eventAggregator?.Subscribe<StatusUpdateEvent>(evt =>
{
    StatusText = evt.Message;  // UI property
});

// Correct - uses UI thread
_eventAggregator?.SubscribeOnUIThread<StatusUpdateEvent>(evt =>
{
    StatusText = evt.Message;  // Safe
});
```

### 5. Don't Publish During Subscription

Avoid publishing events from within event handlers to prevent infinite loops:

```csharp
// Dangerous - potential infinite loop
private void OnProjectOpened(ProjectOpenedEvent evt)
{
    _eventAggregator.Publish(new ProjectOpenedEvent { ... });  // Bad!
}
```

---

## API Reference

### IEventAggregator Interface

```csharp
public interface IEventAggregator
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <typeparam name="TEvent">Event type</typeparam>
    /// <param name="eventData">Event data to publish</param>
    void Publish<TEvent>(TEvent eventData);

    /// <summary>
    /// Subscribes to events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">Event type to subscribe to</typeparam>
    /// <param name="handler">Handler called when event is published</param>
    /// <returns>Subscription token - dispose to unsubscribe</returns>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);

    /// <summary>
    /// Subscribes to events and invokes handler on UI thread.
    /// </summary>
    /// <typeparam name="TEvent">Event type to subscribe to</typeparam>
    /// <param name="handler">Handler called on UI thread</param>
    /// <returns>Subscription token - dispose to unsubscribe</returns>
    IDisposable SubscribeOnUIThread<TEvent>(Action<TEvent> handler);
}
```

### Usage Examples

```csharp
// Publish
_eventAggregator.Publish(new ProjectOpenedEvent
{
    ProjectPath = path,
    ModuleId = ModuleId
});

// Subscribe
var subscription = _eventAggregator.Subscribe<ProjectOpenedEvent>(evt =>
{
    Console.WriteLine($"Project opened: {evt.ProjectPath}");
});

// Subscribe on UI thread
var uiSubscription = _eventAggregator.SubscribeOnUIThread<StatusUpdateEvent>(evt =>
{
    this.StatusText = evt.Message;
});

// Unsubscribe
subscription.Dispose();
```

---

## Troubleshooting

### Events Not Received

1. Check subscription is created before publish
2. Verify event types match exactly
3. Ensure subscription hasn't been disposed
4. Confirm EventAggregator is not null

### Cross-Thread Exceptions

Use `SubscribeOnUIThread` for UI updates:

```csharp
// This may throw if event published from background thread
_eventAggregator.Subscribe<MyEvent>(evt =>
{
    MyTextBox.Text = evt.Value;  // Exception!
});

// This is safe
_eventAggregator.SubscribeOnUIThread<MyEvent>(evt =>
{
    MyTextBox.Text = evt.Value;  // OK
});
```

### Memory Leaks

Always dispose subscriptions:

```csharp
private IDisposable? _subscription;

public void Shutdown()
{
    _subscription?.Dispose();  // Important!
    _subscription = null;
}
```

---

## Related Documentation

- [Shell API](../API/Shell-API.md#event-aggregator)
- [Developer Guide](../DeveloperGuide.md)

---

*Copyright 2026 Fathom OS. All rights reserved.*
