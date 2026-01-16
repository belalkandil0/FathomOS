using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for managing recent projects and file history
/// </summary>
public class RecentProjectsService
{
    private readonly string _configPath;
    private RecentProjectsConfig _config;
    private const int MaxRecentProjects = 20;
    private const int MaxRecentFiles = 50;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public RecentProjectsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "FathomOS", "UsblVerification");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "recent.json");
        LoadConfig();
    }
    
    #region Configuration
    
    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _config = JsonSerializer.Deserialize<RecentProjectsConfig>(json, JsonOptions) ?? new();
            }
            else
            {
                _config = new RecentProjectsConfig();
            }
        }
        catch
        {
            _config = new RecentProjectsConfig();
        }
    }
    
    private void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(_config, JsonOptions);
            File.WriteAllText(_configPath, json);
        }
        catch { }
    }
    
    #endregion
    
    #region Recent Projects
    
    /// <summary>
    /// Add or update a recent project
    /// </summary>
    public void AddRecentProject(RecentProject project)
    {
        // Remove if exists (to update position)
        _config.RecentProjects.RemoveAll(p => 
            p.ProjectPath?.Equals(project.ProjectPath, StringComparison.OrdinalIgnoreCase) == true);
        
        project.LastOpenedAt = DateTime.Now;
        
        // Add to front
        _config.RecentProjects.Insert(0, project);
        
        // Trim to max
        if (_config.RecentProjects.Count > MaxRecentProjects)
            _config.RecentProjects = _config.RecentProjects.Take(MaxRecentProjects).ToList();
        
        SaveConfig();
    }
    
    /// <summary>
    /// Get all recent projects
    /// </summary>
    public List<RecentProject> GetRecentProjects()
    {
        // Filter out deleted projects
        var valid = _config.RecentProjects
            .Where(p => string.IsNullOrEmpty(p.ProjectPath) || File.Exists(p.ProjectPath))
            .ToList();
        
        if (valid.Count != _config.RecentProjects.Count)
        {
            _config.RecentProjects = valid;
            SaveConfig();
        }
        
        return valid;
    }
    
    /// <summary>
    /// Clear recent projects list
    /// </summary>
    public void ClearRecentProjects()
    {
        _config.RecentProjects.Clear();
        SaveConfig();
    }
    
    /// <summary>
    /// Remove a specific project from history
    /// </summary>
    public void RemoveRecentProject(string projectPath)
    {
        _config.RecentProjects.RemoveAll(p => 
            p.ProjectPath?.Equals(projectPath, StringComparison.OrdinalIgnoreCase) == true);
        SaveConfig();
    }
    
    /// <summary>
    /// Pin a project to keep it at top
    /// </summary>
    public void PinProject(string projectPath)
    {
        var project = _config.RecentProjects.FirstOrDefault(p =>
            p.ProjectPath?.Equals(projectPath, StringComparison.OrdinalIgnoreCase) == true);
        if (project != null)
        {
            project.IsPinned = true;
            SaveConfig();
        }
    }
    
    /// <summary>
    /// Unpin a project
    /// </summary>
    public void UnpinProject(string projectPath)
    {
        var project = _config.RecentProjects.FirstOrDefault(p =>
            p.ProjectPath?.Equals(projectPath, StringComparison.OrdinalIgnoreCase) == true);
        if (project != null)
        {
            project.IsPinned = false;
            SaveConfig();
        }
    }
    
    #endregion
    
    #region Recent Files
    
    /// <summary>
    /// Add a recently used file
    /// </summary>
    public void AddRecentFile(string filePath, RecentFileType fileType)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        
        // Remove if exists
        _config.RecentFiles.RemoveAll(f => 
            f.FilePath?.Equals(filePath, StringComparison.OrdinalIgnoreCase) == true);
        
        var recentFile = new RecentFile
        {
            FilePath = filePath,
            FileName = Path.GetFileName(filePath),
            FileType = fileType,
            LastUsedAt = DateTime.Now
        };
        
        _config.RecentFiles.Insert(0, recentFile);
        
        if (_config.RecentFiles.Count > MaxRecentFiles)
            _config.RecentFiles = _config.RecentFiles.Take(MaxRecentFiles).ToList();
        
        SaveConfig();
    }
    
    /// <summary>
    /// Get recent files of specific type
    /// </summary>
    public List<RecentFile> GetRecentFiles(RecentFileType? fileType = null, int count = 10)
    {
        var query = _config.RecentFiles
            .Where(f => !string.IsNullOrEmpty(f.FilePath) && File.Exists(f.FilePath));
        
        if (fileType.HasValue)
            query = query.Where(f => f.FileType == fileType.Value);
        
        return query.Take(count).ToList();
    }
    
    /// <summary>
    /// Get recently used directories
    /// </summary>
    public List<string> GetRecentDirectories(int count = 5)
    {
        return _config.RecentFiles
            .Where(f => !string.IsNullOrEmpty(f.FilePath))
            .Select(f => Path.GetDirectoryName(f.FilePath))
            .Where(d => !string.IsNullOrEmpty(d) && Directory.Exists(d))
            .Distinct()
            .Take(count)
            .ToList()!;
    }
    
    #endregion
    
    #region Last Used Paths
    
    /// <summary>
    /// Get last used directory for file dialogs
    /// </summary>
    public string? GetLastDirectory(string key = "default")
    {
        if (_config.LastDirectories.TryGetValue(key, out var dir) && Directory.Exists(dir))
            return dir;
        return null;
    }
    
    /// <summary>
    /// Set last used directory
    /// </summary>
    public void SetLastDirectory(string path, string key = "default")
    {
        var dir = File.Exists(path) ? Path.GetDirectoryName(path) : path;
        if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
        {
            _config.LastDirectories[key] = dir;
            SaveConfig();
        }
    }
    
    #endregion
    
    #region Drag and Drop Support
    
    /// <summary>
    /// Classify dropped files by type
    /// </summary>
    public DroppedFilesResult ClassifyDroppedFiles(string[] filePaths)
    {
        var result = new DroppedFilesResult();
        
        foreach (var path in filePaths)
        {
            if (!File.Exists(path)) continue;
            
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var fileInfo = new DroppedFile
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                Extension = ext
            };
            
            switch (ext)
            {
                case ".npd":
                case ".csv":
                case ".txt":
                    fileInfo.FileType = DroppedFileType.SurveyData;
                    result.SurveyFiles.Add(fileInfo);
                    break;
                    
                case ".usbl":
                case ".usp":
                    fileInfo.FileType = DroppedFileType.ProjectFile;
                    result.ProjectFiles.Add(fileInfo);
                    break;
                    
                case ".xlsx":
                case ".xls":
                    fileInfo.FileType = DroppedFileType.ExcelFile;
                    result.ExcelFiles.Add(fileInfo);
                    break;
                    
                case ".pdf":
                    fileInfo.FileType = DroppedFileType.PdfFile;
                    result.PdfFiles.Add(fileInfo);
                    break;
                    
                default:
                    fileInfo.FileType = DroppedFileType.Unknown;
                    result.UnknownFiles.Add(fileInfo);
                    break;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Detect heading from filename patterns
    /// </summary>
    public int? DetectHeadingFromFilename(string filename)
    {
        // Common patterns: "000deg", "090", "heading_180", "270_spin", etc.
        var name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();
        
        // Pattern 1: Exact degree values
        var degreePatterns = new[] { "000", "090", "180", "270", "045", "135", "225", "315" };
        foreach (var pattern in degreePatterns)
        {
            if (name.Contains(pattern))
            {
                if (int.TryParse(pattern, out var heading))
                    return heading;
            }
        }
        
        // Pattern 2: Keywords
        if (name.Contains("north") || name.Contains("000") || name.Contains("0deg")) return 0;
        if (name.Contains("east") || name.Contains("090") || name.Contains("90deg")) return 90;
        if (name.Contains("south") || name.Contains("180")) return 180;
        if (name.Contains("west") || name.Contains("270")) return 270;
        
        return null;
    }
    
    /// <summary>
    /// Auto-assign headings to a batch of files
    /// </summary>
    public Dictionary<string, int> AutoAssignHeadings(List<string> filePaths)
    {
        var assignments = new Dictionary<string, int>();
        var usedHeadings = new HashSet<int>();
        var defaultHeadings = new[] { 0, 90, 180, 270 };
        
        // First pass: detect from filenames
        foreach (var path in filePaths)
        {
            var detected = DetectHeadingFromFilename(path);
            if (detected.HasValue && !usedHeadings.Contains(detected.Value))
            {
                assignments[path] = detected.Value;
                usedHeadings.Add(detected.Value);
            }
        }
        
        // Second pass: assign remaining
        int defaultIdx = 0;
        foreach (var path in filePaths)
        {
            if (!assignments.ContainsKey(path))
            {
                while (defaultIdx < defaultHeadings.Length && usedHeadings.Contains(defaultHeadings[defaultIdx]))
                    defaultIdx++;
                
                if (defaultIdx < defaultHeadings.Length)
                {
                    assignments[path] = defaultHeadings[defaultIdx];
                    usedHeadings.Add(defaultHeadings[defaultIdx]);
                    defaultIdx++;
                }
            }
        }
        
        return assignments;
    }
    
    #endregion
}

#region Models

/// <summary>
/// Configuration for recent projects/files
/// </summary>
public class RecentProjectsConfig
{
    public List<RecentProject> RecentProjects { get; set; } = new();
    public List<RecentFile> RecentFiles { get; set; } = new();
    public Dictionary<string, string> LastDirectories { get; set; } = new();
}

/// <summary>
/// Recent project entry
/// </summary>
public class RecentProject
{
    public string? ProjectPath { get; set; }
    public string? ProjectName { get; set; }
    public string? VesselName { get; set; }
    public string? ClientName { get; set; }
    public DateTime LastOpenedAt { get; set; }
    public DateTime? VerificationDate { get; set; }
    public bool IsPinned { get; set; }
    
    // Display helpers
    public string DisplayName => ProjectName ?? Path.GetFileNameWithoutExtension(ProjectPath ?? "Unknown");
    public string LastOpenedDisplay => LastOpenedAt.ToString("yyyy-MM-dd HH:mm");
    public string FolderPath => string.IsNullOrEmpty(ProjectPath) ? "" : Path.GetDirectoryName(ProjectPath) ?? "";
}

/// <summary>
/// Recent file entry
/// </summary>
public class RecentFile
{
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public RecentFileType FileType { get; set; }
    public DateTime LastUsedAt { get; set; }
    
    public string DisplayName => FileName ?? "Unknown";
    public string FolderPath => string.IsNullOrEmpty(FilePath) ? "" : Path.GetDirectoryName(FilePath) ?? "";
}

/// <summary>
/// Recent file types
/// </summary>
public enum RecentFileType
{
    Project,
    SpinData,
    TransitData,
    Export
}

/// <summary>
/// Result of classifying dropped files
/// </summary>
public class DroppedFilesResult
{
    public List<DroppedFile> SurveyFiles { get; set; } = new();
    public List<DroppedFile> ProjectFiles { get; set; } = new();
    public List<DroppedFile> ExcelFiles { get; set; } = new();
    public List<DroppedFile> PdfFiles { get; set; } = new();
    public List<DroppedFile> UnknownFiles { get; set; } = new();
    
    public int TotalCount => SurveyFiles.Count + ProjectFiles.Count + ExcelFiles.Count + 
                            PdfFiles.Count + UnknownFiles.Count;
    public bool HasSurveyFiles => SurveyFiles.Count > 0;
    public bool HasProjectFiles => ProjectFiles.Count > 0;
}

/// <summary>
/// Information about a dropped file
/// </summary>
public class DroppedFile
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Extension { get; set; } = "";
    public DroppedFileType FileType { get; set; }
    public int? DetectedHeading { get; set; }
}

/// <summary>
/// Types of dropped files
/// </summary>
public enum DroppedFileType
{
    Unknown,
    SurveyData,
    ProjectFile,
    ExcelFile,
    PdfFile
}

#endregion
