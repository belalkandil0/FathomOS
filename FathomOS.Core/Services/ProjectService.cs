namespace FathomOS.Core.Services;

using FathomOS.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Service for saving and loading project files (.slproj)
/// </summary>
public class ProjectService
{
    /// <summary>
    /// Default file extension for project files
    /// </summary>
    public const string ProjectFileExtension = ".slproj";

    /// <summary>
    /// File filter for open/save dialogs
    /// </summary>
    public const string FileFilter = "Survey Listing Projects (*.slproj)|*.slproj|All Files (*.*)|*.*";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Save a project to a file
    /// </summary>
    public void Save(Project project, string filePath)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        // Ensure correct extension
        if (!filePath.EndsWith(ProjectFileExtension, StringComparison.OrdinalIgnoreCase))
            filePath += ProjectFileExtension;

        // Update modification date
        project.ModifiedDate = DateTime.Now;

        // Serialize and save
        var json = JsonSerializer.Serialize(project, _jsonOptions);
        
        // Create directory if it doesn't exist
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, json);
        
        // Update project's file path reference
        project.ProjectFilePath = filePath;
    }

    /// <summary>
    /// Load a project from a file
    /// </summary>
    public Project Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Project file not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var project = JsonSerializer.Deserialize<Project>(json, _jsonOptions);

        if (project == null)
            throw new InvalidOperationException("Failed to deserialize project file");

        // Set the file path reference
        project.ProjectFilePath = filePath;

        // Handle file version migrations if needed
        MigrateIfNeeded(project);

        return project;
    }

    /// <summary>
    /// Create a new empty project with default settings
    /// </summary>
    public Project CreateNew()
    {
        return new Project
        {
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now,
            CoordinateUnit = LengthUnit.USSurveyFeet,
            KpUnit = LengthUnit.Kilometer,
            ColumnMapping = ColumnMappingTemplates.NaviPacDefault.Clone(),
            ProcessingOptions = new ProcessingOptions
            {
                ApplyTidalCorrections = true,
                ApplyVerticalOffsets = true,
                DepthExaggeration = 10.0
            },
            OutputOptions = new OutputOptions
            {
                ExportTextFile = true,
                ExportExcel = true,
                ExportDxf = true,
                ExportCadScript = true
            }
        };
    }

    /// <summary>
    /// Check if a file is a valid project file
    /// </summary>
    public bool IsValidProjectFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var json = File.ReadAllText(filePath);
            var project = JsonSerializer.Deserialize<Project>(json, _jsonOptions);
            
            return project != null && project.FileVersion >= 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get project info without full loading
    /// </summary>
    public ProjectInfo? GetProjectInfo(string filePath)
    {
        try
        {
            var project = Load(filePath);
            return new ProjectInfo
            {
                FilePath = filePath,
                ProjectName = project.ProjectName,
                ClientName = project.ClientName,
                CreatedDate = project.CreatedDate,
                ModifiedDate = project.ModifiedDate,
                SurveyFileCount = project.SurveyDataFiles.Count
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Handle version migrations for older project files
    /// </summary>
    private void MigrateIfNeeded(Project project)
    {
        // Version 1 is current, no migration needed
        if (project.FileVersion < 1)
        {
            // Handle legacy format if needed in the future
            project.FileVersion = 1;
        }

        // Ensure column mapping exists
        project.ColumnMapping ??= ColumnMappingTemplates.NaviPacDefault.Clone();

        // Ensure processing options exist
        project.ProcessingOptions ??= new ProcessingOptions();

        // Ensure output options exist
        project.OutputOptions ??= new OutputOptions();
    }

    /// <summary>
    /// Export project settings to a template file (without file paths)
    /// </summary>
    public void ExportTemplate(Project project, string filePath)
    {
        var template = project.Clone();
        
        // Clear file-specific paths
        template.RouteFilePath = string.Empty;
        template.SurveyDataFiles.Clear();
        template.TideFilePath = string.Empty;
        template.OutputOptions.OutputFolder = string.Empty;
        template.OutputOptions.DwgTemplatePath = string.Empty;
        template.SurveyFixes.Clear();

        Save(template, filePath);
    }

    /// <summary>
    /// Apply a template to a project (keeps file paths, updates settings)
    /// </summary>
    public void ApplyTemplate(Project project, string templatePath)
    {
        var template = Load(templatePath);

        // Keep current file paths
        var routeFile = project.RouteFilePath;
        var surveyFiles = project.SurveyDataFiles;
        var tideFile = project.TideFilePath;
        var outputFolder = project.OutputOptions.OutputFolder;
        var dwgTemplate = project.OutputOptions.DwgTemplatePath;
        var fixes = project.SurveyFixes;

        // Copy template settings
        project.ColumnMapping = template.ColumnMapping;
        project.ProcessingOptions = template.ProcessingOptions;
        project.OutputOptions = template.OutputOptions;
        project.CoordinateUnit = template.CoordinateUnit;
        project.KpUnit = template.KpUnit;
        project.UseFeetForTide = template.UseFeetForTide;

        // Restore file paths
        project.RouteFilePath = routeFile;
        project.SurveyDataFiles = surveyFiles;
        project.TideFilePath = tideFile;
        project.OutputOptions.OutputFolder = outputFolder;
        project.OutputOptions.DwgTemplatePath = dwgTemplate;
        project.SurveyFixes = fixes;
    }
}

/// <summary>
/// Basic project information for listing
/// </summary>
public class ProjectInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public int SurveyFileCount { get; set; }

    public string FileName => Path.GetFileName(FilePath);

    public override string ToString()
    {
        return $"{ProjectName} ({ClientName}) - {ModifiedDate:yyyy-MM-dd}";
    }
}
