using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FathomOS.Modules.GnssCalibration.Models;

namespace FathomOS.Modules.GnssCalibration.Services;

/// <summary>
/// Service for saving and loading GNSS project files (.gnss).
/// </summary>
public class ProjectFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    
    /// <summary>
    /// Saves a project to a .gnss file.
    /// </summary>
    public ProjectSaveResult SaveProject(GnssProject project, string filePath)
    {
        try
        {
            var projectFile = new GnssProjectFile
            {
                Version = "1.0",
                SavedAt = DateTime.Now,
                Project = project
            };
            
            var json = JsonSerializer.Serialize(projectFile, JsonOptions);
            File.WriteAllText(filePath, json);
            
            return new ProjectSaveResult
            {
                Success = true,
                FilePath = filePath,
                Message = $"Project saved to {Path.GetFileName(filePath)}"
            };
        }
        catch (Exception ex)
        {
            return new ProjectSaveResult
            {
                Success = false,
                Message = $"Failed to save project: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Loads a project from a .gnss file.
    /// </summary>
    public ProjectLoadResult LoadProject(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new ProjectLoadResult
                {
                    Success = false,
                    Message = $"File not found: {filePath}"
                };
            }
            
            var json = File.ReadAllText(filePath);
            var projectFile = JsonSerializer.Deserialize<GnssProjectFile>(json, JsonOptions);
            
            if (projectFile?.Project == null)
            {
                return new ProjectLoadResult
                {
                    Success = false,
                    Message = "Invalid project file format"
                };
            }
            
            return new ProjectLoadResult
            {
                Success = true,
                Project = projectFile.Project,
                FilePath = filePath,
                SavedAt = projectFile.SavedAt,
                FileVersion = projectFile.Version,
                Message = $"Project loaded from {Path.GetFileName(filePath)}"
            };
        }
        catch (JsonException ex)
        {
            return new ProjectLoadResult
            {
                Success = false,
                Message = $"Invalid project file format: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ProjectLoadResult
            {
                Success = false,
                Message = $"Failed to load project: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Validates a project file without fully loading it.
    /// </summary>
    public bool IsValidProjectFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;
            
            var json = File.ReadAllText(filePath);
            var projectFile = JsonSerializer.Deserialize<GnssProjectFile>(json, JsonOptions);
            
            return projectFile?.Project != null;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Gets basic info from a project file without full load.
    /// </summary>
    public ProjectFileInfo? GetProjectInfo(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            
            var json = File.ReadAllText(filePath);
            var projectFile = JsonSerializer.Deserialize<GnssProjectFile>(json, JsonOptions);
            
            if (projectFile?.Project == null) return null;
            
            return new ProjectFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                ProjectName = projectFile.Project.ProjectName,
                VesselName = projectFile.Project.VesselName,
                SurveyDate = projectFile.Project.SurveyDate,
                SavedAt = projectFile.SavedAt,
                FileVersion = projectFile.Version
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Container for serializing project to file.
/// </summary>
public class GnssProjectFile
{
    public string Version { get; set; } = "1.0";
    public DateTime SavedAt { get; set; }
    public GnssProject Project { get; set; } = new();
}

/// <summary>
/// Result of saving a project.
/// </summary>
public class ProjectSaveResult
{
    public bool Success { get; set; }
    public string FilePath { get; set; } = "";
    public string Message { get; set; } = "";
}

/// <summary>
/// Result of loading a project.
/// </summary>
public class ProjectLoadResult
{
    public bool Success { get; set; }
    public GnssProject? Project { get; set; }
    public string FilePath { get; set; } = "";
    public DateTime? SavedAt { get; set; }
    public string? FileVersion { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// Basic info about a project file.
/// </summary>
public class ProjectFileInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string VesselName { get; set; } = "";
    public DateTime? SurveyDate { get; set; }
    public DateTime SavedAt { get; set; }
    public string FileVersion { get; set; } = "";
}
