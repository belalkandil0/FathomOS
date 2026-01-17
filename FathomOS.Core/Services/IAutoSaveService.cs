// FathomOS.Core/Services/IAutoSaveService.cs
// Contract for the reusable auto-save service
// Provides automatic periodic saving capabilities for modules

namespace FathomOS.Core.Services;

/// <summary>
/// Contract for auto-save functionality.
/// Provides periodic automatic saving capabilities that modules can use
/// to protect user work from data loss.
/// </summary>
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
    /// Subscribers should implement the actual save logic.
    /// </summary>
    event EventHandler? AutoSaveRequested;
}
