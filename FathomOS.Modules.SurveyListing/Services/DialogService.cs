using System.Windows;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace FathomOS.Modules.SurveyListing.Services;

/// <summary>
/// Service for showing dialogs using standard WPF MessageBox
/// </summary>
public class DialogService
{
    private static DialogService? _instance;
    private static readonly object _lock = new();
    
    public static DialogService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new DialogService();
                }
            }
            return _instance;
        }
    }

    private Window? _mainWindow;

    /// <summary>
    /// Initialize the dialog service with the main window reference
    /// </summary>
    public void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow;
    }

    /// <summary>
    /// Get the current active window
    /// </summary>
    private Window? GetActiveWindow()
    {
        if (_mainWindow != null)
            return _mainWindow;
            
        return Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive) 
            ?? Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault();
    }

    /// <summary>
    /// Show an information message
    /// </summary>
    public Task ShowInfoAsync(string title, string message)
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show an error message
    /// </summary>
    public Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show a warning message
    /// </summary>
    public Task ShowWarningAsync(string title, string message)
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Show a Yes/No confirmation dialog
    /// </summary>
    /// <returns>True if Yes was clicked, False otherwise</returns>
    public Task<bool> ShowConfirmAsync(string title, string message)
    {
        var result = MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    /// <summary>
    /// Show a Yes/No/Cancel confirmation dialog
    /// </summary>
    /// <returns>True for Yes, False for No, null for Cancel</returns>
    public Task<bool?> ShowConfirmWithCancelAsync(string title, string message)
    {
        var result = MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        bool? returnValue = result switch
        {
            MessageBoxResult.Yes => true,
            MessageBoxResult.No => false,
            _ => null
        };
        return Task.FromResult(returnValue);
    }

    /// <summary>
    /// Show an input dialog (simplified - returns default value)
    /// </summary>
    public Task<string?> ShowInputAsync(string title, string message, string defaultValue = "")
    {
        // Simple input dialog would require a custom window
        // For now, return the default value
        return Task.FromResult<string?>(defaultValue);
    }

    // ========== Synchronous wrappers ==========

    /// <summary>
    /// Show info message synchronously
    /// </summary>
    public void ShowInfo(string title, string message)
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// Show error message synchronously
    /// </summary>
    public void ShowError(string title, string message)
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// Show warning message synchronously
    /// </summary>
    public void ShowWarning(string title, string message)
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    /// <summary>
    /// Synchronous confirmation
    /// </summary>
    public bool ShowConfirmSync(string title, string message)
    {
        var result = MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Synchronous Yes/No/Cancel
    /// </summary>
    public MessageBoxResult ShowConfirmWithCancelSync(string title, string message)
    {
        return MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
    }
}
