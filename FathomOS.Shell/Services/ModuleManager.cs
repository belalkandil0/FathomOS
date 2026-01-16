using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using FathomOS.Core.Interfaces;
using LicensingSystem.Shared;
using LicensingSystem.Client;

namespace FathomOS.Shell.Services;

/// <summary>
/// Manages discovery, loading, and lifecycle of Fathom OS modules and module groups
/// </summary>
public class ModuleManager
{
    private readonly List<IModule> _modules = new();
    private readonly List<ModuleGroup> _groups = new();
    private readonly string _modulesPath;
    
    public ModuleManager()
    {
        var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        _modulesPath = Path.Combine(exePath, "Modules");
    }
    
    /// <summary>
    /// All discovered root modules (not in groups)
    /// </summary>
    public IReadOnlyList<IModule> Modules => _modules.AsReadOnly();
    
    /// <summary>
    /// All discovered module groups
    /// </summary>
    public IReadOnlyList<ModuleGroup> Groups => _groups.AsReadOnly();
    
    /// <summary>
    /// Discover and load all modules and groups
    /// </summary>
    public void DiscoverModules()
    {
        _modules.Clear();
        _groups.Clear();
        
        if (!Directory.Exists(_modulesPath))
        {
            Directory.CreateDirectory(_modulesPath);
            return;
        }
        
        // Discover root modules
        foreach (var moduleDir in Directory.GetDirectories(_modulesPath))
        {
            var dirName = Path.GetFileName(moduleDir);
            
            // Skip the _Groups folder
            if (dirName == "_Groups")
                continue;
            
            try
            {
                var module = LoadModule(moduleDir);
                if (module != null)
                {
                    _modules.Add(module);
                    System.Diagnostics.Debug.WriteLine($"Loaded root module: {module.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load module from {moduleDir}: {ex.Message}");
            }
        }
        
        // Discover module groups
        var groupsPath = Path.Combine(_modulesPath, "_Groups");
        if (Directory.Exists(groupsPath))
        {
            foreach (var groupDir in Directory.GetDirectories(groupsPath))
            {
                try
                {
                    var group = LoadGroup(groupDir);
                    if (group != null)
                    {
                        _groups.Add(group);
                        System.Diagnostics.Debug.WriteLine($"Loaded group: {group.DisplayName} with {group.Modules.Count} modules");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load group from {groupDir}: {ex.Message}");
                }
            }
        }
        
        // Sort modules and groups by display order
        _modules.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
        _groups.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
    }
    
    /// <summary>
    /// Load a module group from a directory
    /// </summary>
    private ModuleGroup? LoadGroup(string groupDir)
    {
        var groupInfoPath = Path.Combine(groupDir, "GroupInfo.json");
        
        GroupInfo? info = null;
        
        if (File.Exists(groupInfoPath))
        {
            var json = File.ReadAllText(groupInfoPath);
            info = JsonSerializer.Deserialize<GroupInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        
        // Use folder name if no GroupInfo.json
        var groupName = Path.GetFileName(groupDir);
        info ??= new GroupInfo { GroupId = groupName, DisplayName = groupName };
        
        var group = new ModuleGroup
        {
            GroupId = info.GroupId ?? groupName,
            DisplayName = info.DisplayName ?? groupName,
            Description = info.Description ?? "",
            DisplayOrder = info.DisplayOrder,
            IconPath = Path.Combine(groupDir, "Assets", "icon.png")
        };
        
        // Load modules inside the group
        foreach (var moduleDir in Directory.GetDirectories(groupDir))
        {
            if (Path.GetFileName(moduleDir) == "Assets")
                continue;
                
            try
            {
                var module = LoadModule(moduleDir);
                if (module != null)
                {
                    group.Modules.Add(module);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load grouped module from {moduleDir}: {ex.Message}");
            }
        }
        
        // Sort modules within group
        group.Modules.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
        
        return group.Modules.Count > 0 ? group : null;
    }
    
    /// <summary>
    /// Load a module from a directory
    /// </summary>
    private IModule? LoadModule(string moduleDir)
    {
        var infoPath = Path.Combine(moduleDir, "ModuleInfo.json");
        if (!File.Exists(infoPath))
        {
            System.Diagnostics.Debug.WriteLine($"No ModuleInfo.json found in {moduleDir}");
            return null;
        }
        
        var json = File.ReadAllText(infoPath);
        var info = JsonSerializer.Deserialize<ModuleInfo>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        if (info == null || string.IsNullOrEmpty(info.ModuleId))
        {
            System.Diagnostics.Debug.WriteLine($"Invalid ModuleInfo.json in {moduleDir}");
            return null;
        }
        
        var dllName = $"FathomOS.Modules.{info.ModuleId}.dll";
        var dllPath = Path.Combine(moduleDir, dllName);
        
        if (!File.Exists(dllPath))
        {
            var dllFiles = Directory.GetFiles(moduleDir, "*.dll");
            dllPath = dllFiles.FirstOrDefault(f => f.Contains(info.ModuleId, StringComparison.OrdinalIgnoreCase));
            
            if (dllPath == null)
            {
                System.Diagnostics.Debug.WriteLine($"Module DLL not found for {info.ModuleId}");
                return null;
            }
        }
        
        var assembly = Assembly.LoadFrom(dllPath);
        
        var moduleType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        
        if (moduleType == null)
        {
            System.Diagnostics.Debug.WriteLine($"No IModule implementation found in {dllPath}");
            return null;
        }
        
        var module = (IModule?)Activator.CreateInstance(moduleType);
        
        if (module != null)
        {
            module.Initialize();
        }
        
        return module;
    }
    
    /// <summary>
    /// Launch a module by ID
    /// </summary>
    public void LaunchModule(string moduleId, Window? owner = null)
    {
        // SECURITY: Verify license before launching any module
        var licenseStatus = App.LicenseManager?.GetStatusInfo();
        if (licenseStatus == null || (!licenseStatus.IsLicensed && licenseStatus.Status != LicensingSystem.Shared.LicenseStatus.GracePeriod))
        {
            MessageBox.Show(
                "A valid license is required to use Fathom OS modules.\n\n" +
                "Please activate your license to continue.",
                "License Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        
        // Check root modules
        var module = _modules.FirstOrDefault(m => m.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
        
        // Check grouped modules if not found
        if (module == null)
        {
            foreach (var group in _groups)
            {
                module = group.Modules.FirstOrDefault(m => m.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
                if (module != null) break;
            }
        }
        
        if (module == null)
            return;
        
        // SECURITY: Check if this specific module is licensed
        if (!IsModuleLicensed(module.ModuleId))
        {
            ShowModuleLockedDialog(module, licenseStatus);
            return;
        }
        
        module.Launch(owner);
    }
    
    /// <summary>
    /// Check if a specific module is licensed
    /// </summary>
    public bool IsModuleLicensed(string moduleId)
    {
        var licenseManager = App.LicenseManager;
        if (licenseManager == null)
            return false;
            
        return licenseManager.IsModuleLicensed(moduleId);
    }
    
    /// <summary>
    /// Show dialog when user tries to access a locked module
    /// </summary>
    private void ShowModuleLockedDialog(IModule module, LicensingSystem.Client.LicenseStatusInfo licenseStatus)
    {
        var currentTier = licenseStatus.Tier ?? "current";
        
        MessageBox.Show(
            $"The '{module.DisplayName}' module is not included in your {currentTier} license.\n\n" +
            $"Please upgrade your license to access this module.\n\n" +
            $"Contact support for upgrade options.",
            "Module Not Licensed",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
    
    /// <summary>
    /// Get a group by ID
    /// </summary>
    public ModuleGroup? GetGroup(string groupId)
    {
        return _groups.FirstOrDefault(g => g.GroupId.Equals(groupId, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Find modules that can handle a file type
    /// </summary>
    public bool OpenFileWithModule(string filePath, Window? owner = null)
    {
        // SECURITY: Verify license before opening files with modules
        var licenseStatus = App.LicenseManager?.GetStatusInfo();
        if (licenseStatus == null || (!licenseStatus.IsLicensed && licenseStatus.Status != LicenseStatus.GracePeriod))
        {
            MessageBox.Show(
                "A valid license is required to use Fathom OS.\n\n" +
                "Please activate your license to continue.",
                "License Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }
        
        var allModules = _modules.Concat(_groups.SelectMany(g => g.Modules)).ToList();
        var handlers = allModules.Where(m => m.CanHandleFile(filePath)).ToList();
        
        if (handlers.Count == 0)
        {
            MessageBox.Show($"No module can handle this file type: {Path.GetExtension(filePath)}",
                "Unsupported File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        
        // SECURITY: Check if the handler module is licensed
        var handler = handlers[0];
        if (!IsModuleLicensed(handler.ModuleId))
        {
            ShowModuleLockedDialog(handler, licenseStatus);
            return false;
        }
        
        handler.OpenFile(filePath);
        return true;
    }
    
    /// <summary>
    /// Shutdown all modules
    /// </summary>
    public void ShutdownAll()
    {
        foreach (var module in _modules)
        {
            try { module.Shutdown(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error shutting down {module.ModuleId}: {ex.Message}"); }
        }
        
        foreach (var group in _groups)
        {
            foreach (var module in group.Modules)
            {
                try { module.Shutdown(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error shutting down {module.ModuleId}: {ex.Message}"); }
            }
        }
    }
}

/// <summary>
/// Represents a group of related modules
/// </summary>
public class ModuleGroup
{
    public string GroupId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int DisplayOrder { get; set; } = 100;
    public string IconPath { get; set; } = "";
    public List<IModule> Modules { get; } = new();
}

/// <summary>
/// Group metadata from GroupInfo.json
/// </summary>
internal class GroupInfo
{
    public string GroupId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int DisplayOrder { get; set; } = 100;
}

/// <summary>
/// Module metadata from ModuleInfo.json
/// </summary>
internal class ModuleInfo
{
    public string ModuleId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "General";
    public int DisplayOrder { get; set; } = 100;
    public string Icon { get; set; } = "icon.png";
    public string[] SupportedFileTypes { get; set; } = Array.Empty<string>();
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    public string MinimumShellVersion { get; set; } = "1.0.0";
    public string[] Features { get; set; } = Array.Empty<string>();
}
