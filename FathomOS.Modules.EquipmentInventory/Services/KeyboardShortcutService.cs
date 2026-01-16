using System.Windows;
using System.Windows.Input;
using FathomOS.Modules.EquipmentInventory.ViewModels;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for managing keyboard shortcuts.
/// Provides centralized shortcut handling and customization.
/// </summary>
public class KeyboardShortcutService
{
    private readonly Dictionary<KeyGesture, ShortcutAction> _shortcuts;
    private readonly Dictionary<string, Action> _actionHandlers;
    
    public KeyboardShortcutService()
    {
        _shortcuts = new Dictionary<KeyGesture, ShortcutAction>();
        _actionHandlers = new Dictionary<string, Action>();
        RegisterDefaultShortcuts();
    }
    
    #region Default Shortcuts
    
    private void RegisterDefaultShortcuts()
    {
        // File operations
        RegisterShortcut(Key.N, ModifierKeys.Control, "NewEquipment", "Create new equipment");
        RegisterShortcut(Key.O, ModifierKeys.Control, "OpenFile", "Open file");
        RegisterShortcut(Key.S, ModifierKeys.Control, "Save", "Save current item");
        RegisterShortcut(Key.S, ModifierKeys.Control | ModifierKeys.Shift, "SaveAll", "Save all");
        
        // Navigation
        RegisterShortcut(Key.F, ModifierKeys.Control, "Search", "Focus search box");
        RegisterShortcut(Key.G, ModifierKeys.Control, "GoTo", "Go to equipment by asset number");
        RegisterShortcut(Key.Home, ModifierKeys.Control, "GoToDashboard", "Go to dashboard");
        RegisterShortcut(Key.Tab, ModifierKeys.Control, "NextTab", "Next tab");
        RegisterShortcut(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift, "PreviousTab", "Previous tab");
        
        // Equipment actions
        RegisterShortcut(Key.E, ModifierKeys.Control, "EditSelected", "Edit selected equipment");
        RegisterShortcut(Key.D, ModifierKeys.Control, "DuplicateSelected", "Duplicate selected");
        RegisterShortcut(Key.Delete, ModifierKeys.None, "DeleteSelected", "Delete selected");
        RegisterShortcut(Key.P, ModifierKeys.Control, "PrintLabel", "Print label");
        RegisterShortcut(Key.P, ModifierKeys.Control | ModifierKeys.Shift, "PrintReport", "Print report");
        
        // View options
        RegisterShortcut(Key.F5, ModifierKeys.None, "Refresh", "Refresh data");
        RegisterShortcut(Key.F11, ModifierKeys.None, "ToggleFullscreen", "Toggle fullscreen");
        RegisterShortcut(Key.Add, ModifierKeys.Control, "ZoomIn", "Zoom in");
        RegisterShortcut(Key.Subtract, ModifierKeys.Control, "ZoomOut", "Zoom out");
        RegisterShortcut(Key.D0, ModifierKeys.Control, "ZoomReset", "Reset zoom");
        
        // Quick access
        RegisterShortcut(Key.B, ModifierKeys.Control, "ToggleFavorite", "Add/remove from favorites");
        RegisterShortcut(Key.H, ModifierKeys.Control, "ShowHistory", "Show equipment history");
        RegisterShortcut(Key.M, ModifierKeys.Control, "NewManifest", "Create new manifest");
        RegisterShortcut(Key.T, ModifierKeys.Control, "TransferEquipment", "Transfer equipment");
        
        // Export
        RegisterShortcut(Key.E, ModifierKeys.Control | ModifierKeys.Shift, "ExportExcel", "Export to Excel");
        RegisterShortcut(Key.R, ModifierKeys.Control | ModifierKeys.Shift, "ExportPdf", "Export to PDF");
        
        // Sync
        RegisterShortcut(Key.R, ModifierKeys.Control, "Sync", "Sync with server");
        
        // Help
        RegisterShortcut(Key.F1, ModifierKeys.None, "ShowHelp", "Show help");
        RegisterShortcut(Key.OemQuestion, ModifierKeys.Control, "ShowShortcuts", "Show keyboard shortcuts");
        
        // Selection
        RegisterShortcut(Key.A, ModifierKeys.Control, "SelectAll", "Select all");
        RegisterShortcut(Key.Escape, ModifierKeys.None, "ClearSelection", "Clear selection");
        
        // Undo/Redo (future)
        RegisterShortcut(Key.Z, ModifierKeys.Control, "Undo", "Undo");
        RegisterShortcut(Key.Y, ModifierKeys.Control, "Redo", "Redo");
    }
    
    #endregion
    
    #region Registration
    
    /// <summary>
    /// Register a keyboard shortcut
    /// </summary>
    public void RegisterShortcut(Key key, ModifierKeys modifiers, string actionId, string description)
    {
        var gesture = new KeyGesture(key, modifiers);
        _shortcuts[gesture] = new ShortcutAction
        {
            ActionId = actionId,
            Description = description,
            Key = key,
            Modifiers = modifiers
        };
    }
    
    /// <summary>
    /// Register an action handler
    /// </summary>
    public void RegisterHandler(string actionId, Action handler)
    {
        _actionHandlers[actionId] = handler;
    }
    
    /// <summary>
    /// Unregister an action handler
    /// </summary>
    public void UnregisterHandler(string actionId)
    {
        _actionHandlers.Remove(actionId);
    }
    
    #endregion
    
    #region Execution
    
    /// <summary>
    /// Handle a key event
    /// </summary>
    public bool HandleKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        foreach (var kvp in _shortcuts)
        {
            if (kvp.Key.Key == e.Key && kvp.Key.Modifiers == Keyboard.Modifiers)
            {
                if (_actionHandlers.TryGetValue(kvp.Value.ActionId, out var handler))
                {
                    handler();
                    e.Handled = true;
                    return true;
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// Execute an action by ID
    /// </summary>
    public bool ExecuteAction(string actionId)
    {
        if (_actionHandlers.TryGetValue(actionId, out var handler))
        {
            handler();
            return true;
        }
        return false;
    }
    
    #endregion
    
    #region Queries
    
    /// <summary>
    /// Get all registered shortcuts
    /// </summary>
    public List<ShortcutAction> GetAllShortcuts()
    {
        return _shortcuts.Values.OrderBy(s => s.Description).ToList();
    }
    
    /// <summary>
    /// Get shortcuts grouped by category
    /// </summary>
    public Dictionary<string, List<ShortcutAction>> GetShortcutsByCategory()
    {
        var categories = new Dictionary<string, List<ShortcutAction>>
        {
            ["File"] = new(),
            ["Navigation"] = new(),
            ["Equipment"] = new(),
            ["View"] = new(),
            ["Export"] = new(),
            ["Other"] = new()
        };
        
        foreach (var shortcut in _shortcuts.Values)
        {
            var category = CategorizeAction(shortcut.ActionId);
            categories[category].Add(shortcut);
        }
        
        return categories.Where(c => c.Value.Count > 0).ToDictionary(c => c.Key, c => c.Value);
    }
    
    /// <summary>
    /// Get shortcut for an action
    /// </summary>
    public ShortcutAction? GetShortcutForAction(string actionId)
    {
        return _shortcuts.Values.FirstOrDefault(s => s.ActionId == actionId);
    }
    
    /// <summary>
    /// Get display string for a shortcut
    /// </summary>
    public static string GetShortcutDisplayString(Key key, ModifierKeys modifiers)
    {
        var parts = new List<string>();
        
        if (modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        
        parts.Add(GetKeyDisplayName(key));
        
        return string.Join("+", parts);
    }
    
    #endregion
    
    #region Window Integration
    
    /// <summary>
    /// Attach to a window for keyboard handling
    /// </summary>
    public void AttachToWindow(Window window)
    {
        window.PreviewKeyDown += (s, e) => HandleKeyDown(e);
    }
    
    /// <summary>
    /// Create input bindings for a window
    /// </summary>
    public void CreateInputBindings(Window window)
    {
        foreach (var kvp in _shortcuts)
        {
            var actionId = kvp.Value.ActionId;
            var command = new RelayCommand(_ => ExecuteAction(actionId));
            window.InputBindings.Add(new KeyBinding(command, kvp.Key));
        }
    }
    
    #endregion
    
    #region Helpers
    
    private static string CategorizeAction(string actionId)
    {
        return actionId switch
        {
            "NewEquipment" or "OpenFile" or "Save" or "SaveAll" => "File",
            "Search" or "GoTo" or "GoToDashboard" or "NextTab" or "PreviousTab" => "Navigation",
            "EditSelected" or "DuplicateSelected" or "DeleteSelected" or "PrintLabel" 
                or "ToggleFavorite" or "ShowHistory" or "NewManifest" or "TransferEquipment" => "Equipment",
            "Refresh" or "ToggleFullscreen" or "ZoomIn" or "ZoomOut" or "ZoomReset" => "View",
            "ExportExcel" or "ExportPdf" or "PrintReport" => "Export",
            _ => "Other"
        };
    }
    
    private static string GetKeyDisplayName(Key key)
    {
        return key switch
        {
            Key.OemQuestion => "?",
            Key.OemPlus or Key.Add => "+",
            Key.OemMinus or Key.Subtract => "-",
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            _ => key.ToString()
        };
    }
    
    #endregion
}

#region Models

public class ShortcutAction
{
    public string ActionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Key Key { get; set; }
    public ModifierKeys Modifiers { get; set; }
    
    public string DisplayString => KeyboardShortcutService.GetShortcutDisplayString(Key, Modifiers);
}

#endregion
