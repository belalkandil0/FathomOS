// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: SurveyLogbookModule.cs
// Purpose: IModule implementation for Fathom OS Shell integration
// Note: This is the module entry point discovered by ModuleManager
// ============================================================================

using System.IO;
using System.Windows;
using FathomOS.Core.Interfaces;
using FathomOS.Modules.SurveyLogbook.Views;

namespace FathomOS.Modules.SurveyLogbook;

/// <summary>
/// Survey Electronic Logbook module entry point.
/// Implements IModule interface for Fathom OS Shell discovery and integration.
/// </summary>
/// <remarks>
/// This module provides:
/// - Real-time survey log capture from NaviPac TCP and file monitors
/// - DVR recording tracking from VisualWorks folder structure
/// - Position fix parsing from .npc calibration files
/// - Waypoint change detection from .wp2 files
/// - DPR (Daily Progress Report) / Shift Handover management
/// - Excel and PDF export capabilities
/// </remarks>
public class SurveyLogbookModule : IModule
{
    #region Fields

    private MainWindow? _mainWindow;
    private bool _isInitialized;

    #endregion

    #region IModule Properties

    /// <summary>
    /// Gets the unique module identifier.
    /// Must match the DLL name: FathomOS.Modules.SurveyLogbook
    /// </summary>
    public string ModuleId => "SurveyLogbook";

    /// <summary>
    /// Gets the display name shown on the dashboard tile.
    /// </summary>
    public string DisplayName => "Survey Logbook";

    /// <summary>
    /// Gets the module description shown as tooltip.
    /// </summary>
    public string Description => "Electronic survey logbook with real-time NaviPac integration, " +
                                 "DVR tracking, position fix logging, and DPR/Shift Handover reports.";

    /// <summary>
    /// Gets the module version.
    /// </summary>
    public Version Version => new Version(11, 0, 0);

    /// <summary>
    /// Gets the icon resource path for the dashboard tile.
    /// </summary>
    public string IconResource => "/FathomOS.Modules.SurveyLogbook;component/Assets/icon.png";

    /// <summary>
    /// Gets the module category for dashboard grouping.
    /// </summary>
    public string Category => "Data Processing";

    /// <summary>
    /// Gets the display order on the dashboard (lower = first).
    /// </summary>
    public int DisplayOrder => 15;

    #endregion

    #region IModule Methods

    /// <summary>
    /// Called when the module is first loaded by the Shell.
    /// Performs one-time initialization tasks.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Initializing {DisplayName} v{Version}...");

            // Ensure required directories exist
            EnsureDirectoriesExist();

            // Register any global resources or services here
            // Note: Most initialization happens in MainWindow.Loaded

            _isInitialized = true;
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Initialization complete.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Initialization failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Called when the user clicks the module tile on the dashboard.
    /// Creates and shows the module's main window.
    /// </summary>
    /// <param name="owner">Optional owner window for modal positioning.</param>
    public void Launch(Window? owner = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Launching {DisplayName}...");

            // Create new window if needed or reactivate existing
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
                
                // Set owner if provided (for proper z-ordering)
                if (owner != null)
                {
                    _mainWindow.Owner = owner;
                }
            }

            // Show and activate the window
            _mainWindow.Show();
            _mainWindow.Activate();

            // Restore from minimized if needed
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Launch complete.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Launch failed: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Failed to launch {DisplayName}:\n\n{ex.Message}",
                "Module Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Called when the application is closing.
    /// Performs cleanup and resource disposal.
    /// </summary>
    public void Shutdown()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Shutting down {DisplayName}...");

            // Close the main window if open
            if (_mainWindow != null)
            {
                _mainWindow.Close();
                _mainWindow = null;
            }

            _isInitialized = false;
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Shutdown complete.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Shutdown error: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if this module can handle the specified file type.
    /// </summary>
    /// <param name="filePath">Full path to the file.</param>
    /// <returns>True if the module can open this file type.</returns>
    public bool CanHandleFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Supported file types:
        // .slog  - Survey log file (uncompressed JSON)
        // .slogz - Survey log file (GZip compressed)
        // .npc   - NaviPac calibration/position fix file
        // .wp2   - Waypoint file
        return extension is ".slog" or ".slogz" or ".npc" or ".wp2";
    }

    /// <summary>
    /// Opens a file directly in the module.
    /// Called when user double-clicks an associated file type.
    /// </summary>
    /// <param name="filePath">Full path to the file to open.</param>
    public void OpenFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Opening file: {filePath}");

            // Launch the module first
            Launch();

            // Then load the file
            // The main window will handle loading based on file type
            if (_mainWindow?.DataContext is ViewModels.MainViewModel viewModel)
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                switch (extension)
                {
                    case ".slog":
                    case ".slogz":
                        // Load survey log file
                        _ = viewModel.LoadFileAsync(filePath);
                        break;
                        
                    case ".npc":
                        // Parse and add position fix entry
                        viewModel.ImportNpcFile(filePath);
                        break;
                        
                    case ".wp2":
                        // Parse waypoint file (for manual import)
                        viewModel.ImportWaypointFile(filePath);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Failed to open file: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Failed to open file:\n\n{ex.Message}",
                "File Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Ensures required directories exist for module operation.
    /// </summary>
    private void EnsureDirectoriesExist()
    {
        try
        {
            // Create settings directory
            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FathomOS",
                "SurveyLogbook");
            
            if (!Directory.Exists(settingsPath))
            {
                Directory.CreateDirectory(settingsPath);
                System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Created settings directory: {settingsPath}");
            }

            // Create default output directory
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "FathomOS",
                "SurveyLogs");
            
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Created output directory: {outputPath}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{ModuleId}] Failed to create directories: {ex.Message}");
        }
    }

    #endregion
}
