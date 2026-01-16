using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using FathomOS.Core.Interfaces;
using LicensingSystem.Shared;
using LicensingSystem.Client;
using Microsoft.Extensions.DependencyInjection;

namespace FathomOS.Shell.Services;

/// <summary>
/// Manages discovery, lazy loading, and lifecycle of FathomOS modules and module groups.
/// Implements lazy loading - only metadata is loaded during discovery, DLLs are loaded on demand.
/// Owned by: SHELL-AGENT
/// </summary>
public class ModuleManager : IModuleManager
{
    private readonly Dictionary<string, ModuleMetadata> _moduleMetadata = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ModuleGroupMetadata> _groupMetadata = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IModule> _loadedModules = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _modulesPath;
    private readonly IServiceProvider? _serviceProvider;

    public ModuleManager(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
        var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        _modulesPath = Path.Combine(exePath, "Modules");
    }

    /// <summary>
    /// All discovered module metadata (not yet loaded modules).
    /// </summary>
    public IReadOnlyDictionary<string, ModuleMetadata> ModuleMetadataCollection => _moduleMetadata;

    /// <summary>
    /// All discovered group metadata.
    /// </summary>
    public IReadOnlyDictionary<string, ModuleGroupMetadata> GroupMetadataCollection => _groupMetadata;

    /// <summary>
    /// All loaded modules (DLLs that have been loaded and instantiated).
    /// </summary>
    public IReadOnlyDictionary<string, IModule> LoadedModules => _loadedModules;

    /// <summary>
    /// Get root modules (not in groups) sorted by display order.
    /// For backward compatibility.
    /// </summary>
    public IReadOnlyList<IModuleMetadata> Modules =>
        _moduleMetadata.Values
            .Where(m => m.GroupId == null)
            .OrderBy(m => m.DisplayOrder)
            .Cast<IModuleMetadata>()
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Get all groups sorted by display order.
    /// For backward compatibility.
    /// </summary>
    public IReadOnlyList<IModuleGroupMetadata> Groups =>
        _groupMetadata.Values
            .OrderBy(g => g.DisplayOrder)
            .Cast<IModuleGroupMetadata>()
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Discover all modules and groups by reading metadata files only.
    /// NO DLL loading happens during discovery - that's lazy loaded on demand.
    /// </summary>
    public void DiscoverModules()
    {
        _moduleMetadata.Clear();
        _groupMetadata.Clear();
        // Note: Don't clear _loadedModules to preserve already-loaded modules

        if (!Directory.Exists(_modulesPath))
        {
            Directory.CreateDirectory(_modulesPath);
            return;
        }

        // Discover root modules (metadata only - NO DLL loading)
        foreach (var moduleDir in Directory.GetDirectories(_modulesPath))
        {
            var dirName = Path.GetFileName(moduleDir);

            // Skip the _Groups folder
            if (dirName == "_Groups")
                continue;

            try
            {
                var metadata = LoadModuleMetadata(moduleDir);
                if (metadata != null)
                {
                    _moduleMetadata[metadata.ModuleId] = metadata;
                    System.Diagnostics.Debug.WriteLine($"Discovered module: {metadata.DisplayName} (lazy)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read module metadata from {moduleDir}: {ex.Message}");
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
                    var group = LoadGroupMetadata(groupDir);
                    if (group != null)
                    {
                        _groupMetadata[group.GroupId] = group;
                        System.Diagnostics.Debug.WriteLine($"Discovered group: {group.DisplayName} with {group.ModuleIds.Count} modules (lazy)");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to read group metadata from {groupDir}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Load only the metadata from a module directory (NO DLL loading).
    /// </summary>
    private ModuleMetadata? LoadModuleMetadata(string moduleDir, string? groupId = null)
    {
        var infoPath = Path.Combine(moduleDir, "ModuleInfo.json");
        if (!File.Exists(infoPath))
        {
            System.Diagnostics.Debug.WriteLine($"No ModuleInfo.json found in {moduleDir}");
            return null;
        }

        var json = File.ReadAllText(infoPath);
        var info = JsonSerializer.Deserialize<ModuleInfoJson>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (info == null || string.IsNullOrEmpty(info.ModuleId))
        {
            System.Diagnostics.Debug.WriteLine($"Invalid ModuleInfo.json in {moduleDir}");
            return null;
        }

        // Find the DLL path without loading it
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

        // Build icon path
        var iconPath = !string.IsNullOrEmpty(info.Icon)
            ? Path.Combine(moduleDir, "Assets", info.Icon)
            : Path.Combine(moduleDir, "Assets", "icon.png");

        return new ModuleMetadata
        {
            ModuleId = info.ModuleId,
            DisplayName = info.DisplayName ?? info.ModuleId,
            Description = info.Description ?? "",
            Version = info.Version ?? "1.0.0",
            Category = info.Category ?? "General",
            DisplayOrder = info.DisplayOrder,
            SupportedFileTypes = info.SupportedFileTypes ?? Array.Empty<string>(),
            DllPath = dllPath,
            IconPath = iconPath,
            GroupId = groupId,
            ModuleDirectory = moduleDir
        };
    }

    /// <summary>
    /// Load only the group metadata from a directory.
    /// </summary>
    private ModuleGroupMetadata? LoadGroupMetadata(string groupDir)
    {
        var groupInfoPath = Path.Combine(groupDir, "GroupInfo.json");

        GroupInfoJson? info = null;

        if (File.Exists(groupInfoPath))
        {
            var json = File.ReadAllText(groupInfoPath);
            info = JsonSerializer.Deserialize<GroupInfoJson>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        var groupName = Path.GetFileName(groupDir);
        info ??= new GroupInfoJson { GroupId = groupName, DisplayName = groupName };

        var group = new ModuleGroupMetadata
        {
            GroupId = info.GroupId ?? groupName,
            DisplayName = info.DisplayName ?? groupName,
            Description = info.Description ?? "",
            DisplayOrder = info.DisplayOrder,
            IconPath = Path.Combine(groupDir, "Assets", "icon.png")
        };

        // Discover modules inside the group (metadata only)
        foreach (var moduleDir in Directory.GetDirectories(groupDir))
        {
            if (Path.GetFileName(moduleDir) == "Assets")
                continue;

            try
            {
                var metadata = LoadModuleMetadata(moduleDir, group.GroupId);
                if (metadata != null)
                {
                    _moduleMetadata[metadata.ModuleId] = metadata;
                    group.AddModuleId(metadata.ModuleId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to read grouped module metadata from {moduleDir}: {ex.Message}");
            }
        }

        return group.ModuleIds.Count > 0 ? group : null;
    }

    /// <summary>
    /// Lazily load a module's DLL and create an instance.
    /// This is the only place where DLLs are loaded.
    /// </summary>
    private IModule? LoadModuleDll(ModuleMetadata metadata)
    {
        if (_loadedModules.TryGetValue(metadata.ModuleId, out var existing))
        {
            return existing;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"Loading module DLL: {metadata.DllPath}");

            var assembly = Assembly.LoadFrom(metadata.DllPath);

            var moduleType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (moduleType == null)
            {
                System.Diagnostics.Debug.WriteLine($"No IModule implementation found in {metadata.DllPath}");
                return null;
            }

            IModule? module = null;

            // Try DI first if service provider is available
            if (_serviceProvider != null)
            {
                try
                {
                    module = (IModule?)ActivatorUtilities.CreateInstance(_serviceProvider, moduleType);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DI instantiation failed, falling back to Activator: {ex.Message}");
                }
            }

            // Fallback to Activator.CreateInstance
            module ??= (IModule?)Activator.CreateInstance(moduleType);

            if (module != null)
            {
                module.Initialize();
                _loadedModules[metadata.ModuleId] = module;
                metadata.MarkAsLoaded();
                System.Diagnostics.Debug.WriteLine($"Module loaded and initialized: {module.DisplayName}");
            }

            return module;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load module DLL {metadata.DllPath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Launch a module by ID. This triggers lazy loading if the module hasn't been loaded yet.
    /// </summary>
    public void LaunchModule(string moduleId, Window? owner = null)
    {
        // SECURITY: Verify license before launching any module
        var licenseStatus = App.LicenseManager?.GetStatusInfo();
        if (licenseStatus == null || (!licenseStatus.IsLicensed && licenseStatus.Status != LicenseStatus.GracePeriod))
        {
            MessageBox.Show(
                "A valid license is required to use FathomOS modules.\n\n" +
                "Please activate your license to continue.",
                "License Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Find module metadata
        if (!_moduleMetadata.TryGetValue(moduleId, out var metadata))
        {
            System.Diagnostics.Debug.WriteLine($"Module not found: {moduleId}");
            return;
        }

        // SECURITY: Check if this specific module is licensed
        if (!IsModuleLicensed(moduleId))
        {
            ShowModuleLockedDialog(metadata, licenseStatus);
            return;
        }

        // Lazy load the module DLL if not already loaded
        var module = LoadModuleDll(metadata);

        if (module != null)
        {
            module.Launch(owner);
        }
    }

    /// <summary>
    /// Get a loaded module by ID (returns null if not yet loaded).
    /// </summary>
    public IModule? GetLoadedModule(string moduleId)
    {
        return _loadedModules.TryGetValue(moduleId, out var module) ? module : null;
    }

    /// <summary>
    /// Get module metadata by ID.
    /// </summary>
    public ModuleMetadata? GetModuleMetadata(string moduleId)
    {
        return _moduleMetadata.TryGetValue(moduleId, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Check if a specific module is licensed.
    /// </summary>
    public bool IsModuleLicensed(string moduleId)
    {
        var licenseManager = App.LicenseManager;
        if (licenseManager == null)
            return false;

        return licenseManager.IsModuleLicensed(moduleId);
    }

    /// <summary>
    /// Show dialog when user tries to access a locked module.
    /// </summary>
    private void ShowModuleLockedDialog(ModuleMetadata metadata, LicenseStatusInfo licenseStatus)
    {
        var currentTier = licenseStatus.Tier ?? "current";

        MessageBox.Show(
            $"The '{metadata.DisplayName}' module is not included in your {currentTier} license.\n\n" +
            $"Please upgrade your license to access this module.\n\n" +
            $"Contact support for upgrade options.",
            "Module Not Licensed",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Get a group by ID.
    /// </summary>
    public ModuleGroupMetadata? GetGroup(string groupId)
    {
        return _groupMetadata.TryGetValue(groupId, out var group) ? group : null;
    }

    /// <summary>
    /// Get modules in a group.
    /// </summary>
    public IReadOnlyList<ModuleMetadata> GetModulesInGroup(string groupId)
    {
        if (!_groupMetadata.TryGetValue(groupId, out var group))
            return Array.Empty<ModuleMetadata>();

        return group.ModuleIds
            .Select(id => _moduleMetadata.TryGetValue(id, out var m) ? m : null)
            .Where(m => m != null)
            .Cast<ModuleMetadata>()
            .OrderBy(m => m.DisplayOrder)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Find modules that can handle a file type and open the file.
    /// </summary>
    public bool OpenFileWithModule(string filePath, Window? owner = null)
    {
        // SECURITY: Verify license before opening files with modules
        var licenseStatus = App.LicenseManager?.GetStatusInfo();
        if (licenseStatus == null || (!licenseStatus.IsLicensed && licenseStatus.Status != LicenseStatus.GracePeriod))
        {
            MessageBox.Show(
                "A valid license is required to use FathomOS.\n\n" +
                "Please activate your license to continue.",
                "License Required",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // Find modules that support this file type (from metadata, no DLL loading yet)
        var handlers = _moduleMetadata.Values
            .Where(m => m.SupportedFileTypes.Any(ft =>
                ft.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
                ft.Equals("*" + extension, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (handlers.Count == 0)
        {
            MessageBox.Show($"No module can handle this file type: {extension}",
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

        // Lazy load the module and open the file
        var module = LoadModuleDll(handler);
        if (module != null)
        {
            module.OpenFile(filePath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Shutdown all loaded modules.
    /// </summary>
    public void ShutdownAll()
    {
        foreach (var module in _loadedModules.Values)
        {
            try
            {
                module.Shutdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error shutting down {module.ModuleId}: {ex.Message}");
            }
        }

        _loadedModules.Clear();
    }

    /// <summary>
    /// Get count of loaded modules (for diagnostics).
    /// </summary>
    public int LoadedModuleCount => _loadedModules.Count;

    /// <summary>
    /// Get count of discovered modules (for diagnostics).
    /// </summary>
    public int DiscoveredModuleCount => _moduleMetadata.Count;
}

/// <summary>
/// Concrete implementation of module metadata for lazy loading.
/// </summary>
public class ModuleMetadata : IModuleMetadata
{
    public string ModuleId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public string Version { get; init; } = "1.0.0";
    public string Category { get; init; } = "General";
    public int DisplayOrder { get; init; } = 100;
    public string[] SupportedFileTypes { get; init; } = Array.Empty<string>();
    public string DllPath { get; init; } = "";
    public string IconPath { get; init; } = "";
    public bool IsLoaded { get; private set; }
    public string? GroupId { get; init; }

    /// <summary>
    /// Directory where the module is located.
    /// </summary>
    public string ModuleDirectory { get; init; } = "";

    internal void MarkAsLoaded() => IsLoaded = true;
}

/// <summary>
/// Concrete implementation of module group metadata.
/// </summary>
public class ModuleGroupMetadata : IModuleGroupMetadata
{
    private readonly List<string> _moduleIds = new();

    public string GroupId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public int DisplayOrder { get; init; } = 100;
    public string IconPath { get; init; } = "";
    public IReadOnlyList<string> ModuleIds => _moduleIds.AsReadOnly();

    internal void AddModuleId(string moduleId) => _moduleIds.Add(moduleId);
}

/// <summary>
/// JSON deserialization model for ModuleInfo.json.
/// </summary>
internal class ModuleInfoJson
{
    public string ModuleId { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
    public int DisplayOrder { get; set; } = 100;
    public string? Icon { get; set; }
    public string[]? SupportedFileTypes { get; set; }
    public string[]? Dependencies { get; set; }
    public string? MinimumShellVersion { get; set; }
    public string[]? Features { get; set; }
}

/// <summary>
/// JSON deserialization model for GroupInfo.json.
/// </summary>
internal class GroupInfoJson
{
    public string? GroupId { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 100;
}
