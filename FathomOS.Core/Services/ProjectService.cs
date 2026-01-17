namespace FathomOS.Core.Services;

using FathomOS.Core.Logging;
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

    private readonly ILogger? _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initializes a new instance of ProjectService without logging.
    /// </summary>
    public ProjectService()
    {
        _logger = null;
    }

    /// <summary>
    /// Initializes a new instance of ProjectService with logging support.
    /// </summary>
    /// <param name="logger">The logger instance for recording operations and errors.</param>
    public ProjectService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
    /// <param name="filePath">Path to the project file to load.</param>
    /// <returns>The loaded project.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the project file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails or returns null.</exception>
    /// <exception cref="JsonException">Thrown when the file contains invalid JSON.</exception>
    public Project Load(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty", nameof(filePath));

        if (!File.Exists(filePath))
        {
            _logger?.Error($"Project file not found: {filePath}", nameof(ProjectService));
            throw new FileNotFoundException($"Project file not found: {filePath}");
        }

        _logger?.Debug($"Loading project from: {filePath}", nameof(ProjectService));

        string json;
        try
        {
            json = File.ReadAllText(filePath);
        }
        catch (IOException ex)
        {
            _logger?.Error($"Failed to read project file: {filePath}", ex, nameof(ProjectService));
            throw;
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            _logger?.Error($"Project file is empty: {filePath}", nameof(ProjectService));
            throw new InvalidOperationException($"Project file is empty: {filePath}");
        }

        Project? project;
        try
        {
            project = JsonSerializer.Deserialize<Project>(json, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger?.Error($"Failed to parse project file JSON: {filePath}", ex, nameof(ProjectService));
            throw new InvalidOperationException($"Failed to parse project file: {filePath}. The file may be corrupted or in an invalid format.", ex);
        }

        if (project == null)
        {
            _logger?.Error($"Deserialization returned null for project file: {filePath}", nameof(ProjectService));
            throw new InvalidOperationException($"Failed to deserialize project file: {filePath}. The file content could not be converted to a valid project.");
        }

        // Set the file path reference
        project.ProjectFilePath = filePath;

        // Handle file version migrations if needed
        MigrateIfNeeded(project);

        _logger?.Info($"Successfully loaded project: {project.ProjectName ?? "Unnamed"} from {filePath}", nameof(ProjectService));

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
    /// <param name="filePath">Path to the file to validate.</param>
    /// <returns>True if the file is a valid project file; otherwise, false.</returns>
    public bool IsValidProjectFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger?.Debug("IsValidProjectFile called with empty path", nameof(ProjectService));
            return false;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                _logger?.Debug($"File does not exist: {filePath}", nameof(ProjectService));
                return false;
            }

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger?.Debug($"File is empty: {filePath}", nameof(ProjectService));
                return false;
            }

            var project = JsonSerializer.Deserialize<Project>(json, _jsonOptions);

            if (project == null)
            {
                _logger?.Debug($"Deserialization returned null for: {filePath}", nameof(ProjectService));
                return false;
            }

            var isValid = project.FileVersion >= 1;
            _logger?.Debug($"File validation result for {filePath}: {(isValid ? "valid" : "invalid version")}", nameof(ProjectService));
            return isValid;
        }
        catch (JsonException ex)
        {
            _logger?.Debug($"JSON parsing failed for {filePath}: {ex.Message}", nameof(ProjectService));
            return false;
        }
        catch (IOException ex)
        {
            _logger?.Debug($"IO error reading {filePath}: {ex.Message}", nameof(ProjectService));
            return false;
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Unexpected error validating project file {filePath}: {ex.Message}", ex, nameof(ProjectService));
            return false;
        }
    }

    /// <summary>
    /// Get project info without full loading
    /// </summary>
    /// <param name="filePath">Path to the project file.</param>
    /// <returns>Project info if successful; null if the file cannot be read or is invalid.</returns>
    public ProjectInfo? GetProjectInfo(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger?.Warning("GetProjectInfo called with empty path", nameof(ProjectService));
            return null;
        }

        try
        {
            var project = Load(filePath);

            // Ensure SurveyDataFiles is not null before accessing Count
            var surveyFileCount = project.SurveyDataFiles?.Count ?? 0;

            return new ProjectInfo
            {
                FilePath = filePath,
                ProjectName = project.ProjectName ?? string.Empty,
                ClientName = project.ClientName ?? string.Empty,
                CreatedDate = project.CreatedDate,
                ModifiedDate = project.ModifiedDate,
                SurveyFileCount = surveyFileCount
            };
        }
        catch (FileNotFoundException ex)
        {
            _logger?.Warning($"Project file not found when getting info: {filePath}", ex, nameof(ProjectService));
            return null;
        }
        catch (JsonException ex)
        {
            _logger?.Warning($"Invalid JSON in project file: {filePath}", ex, nameof(ProjectService));
            return null;
        }
        catch (InvalidOperationException ex)
        {
            _logger?.Warning($"Failed to load project info: {filePath}", ex, nameof(ProjectService));
            return null;
        }
        catch (Exception ex)
        {
            // Log unexpected exceptions with full details before returning null
            _logger?.Error($"Unexpected error loading project info from {filePath}", ex, nameof(ProjectService));
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
