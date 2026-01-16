using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FathomOS.Modules.MruCalibration.Models;

namespace FathomOS.Modules.MruCalibration.Services;

/// <summary>
/// Service for saving and loading calibration sessions
/// </summary>
public class SessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    
    /// <summary>
    /// Default directory for saving sessions
    /// </summary>
    public static string DefaultSessionDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FathomOS", "MruCalibration", "Sessions");
    
    /// <summary>
    /// Save a calibration session to file
    /// </summary>
    public async Task<bool> SaveSessionAsync(CalibrationSession session, string filePath)
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            session.FilePath = filePath;
            session.MarkModified();
            
            var json = JsonSerializer.Serialize(session, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Load a calibration session from file
    /// </summary>
    public async Task<CalibrationSession?> LoadSessionAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;
            
            var json = await File.ReadAllTextAsync(filePath);
            var session = JsonSerializer.Deserialize<CalibrationSession>(json, JsonOptions);
            
            if (session != null)
            {
                session.FilePath = filePath;
            }
            
            return session;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Get list of recent sessions
    /// </summary>
    public List<SessionInfo> GetRecentSessions(int maxCount = 10)
    {
        var sessions = new List<SessionInfo>();
        
        try
        {
            if (!Directory.Exists(DefaultSessionDirectory))
                return sessions;
            
            var files = Directory.GetFiles(DefaultSessionDirectory, "*.mru")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .Take(maxCount);
            
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var session = JsonSerializer.Deserialize<CalibrationSession>(json, JsonOptions);
                    
                    if (session != null)
                    {
                        sessions.Add(new SessionInfo
                        {
                            FilePath = file,
                            ProjectName = session.ProjectInfo.ProjectTitle,
                            VesselName = session.ProjectInfo.VesselName,
                            CreatedAt = session.CreatedAt,
                            ModifiedAt = session.ModifiedAt,
                            HasPitchData = session.HasPitchData,
                            HasRollData = session.HasRollData
                        });
                    }
                }
                catch
                {
                    // Skip invalid session files
                }
            }
        }
        catch
        {
            // Return empty list on error
        }
        
        return sessions;
    }
    
    /// <summary>
    /// Generate default filename for a new session
    /// </summary>
    public string GenerateSessionFileName(CalibrationSession session)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var projectName = SanitizeFileName(session.ProjectInfo.ProjectTitle);
        var vesselName = SanitizeFileName(session.ProjectInfo.VesselName);
        
        if (!string.IsNullOrEmpty(projectName) && !string.IsNullOrEmpty(vesselName))
        {
            return $"{projectName}_{vesselName}_{timestamp}.mru";
        }
        else if (!string.IsNullOrEmpty(projectName))
        {
            return $"{projectName}_{timestamp}.mru";
        }
        else
        {
            return $"MRU_Calibration_{timestamp}.mru";
        }
    }
    
    /// <summary>
    /// Sanitize a string for use in a filename
    /// </summary>
    private string SanitizeFileName(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(input
            .Where(c => !invalid.Contains(c))
            .Take(50)
            .ToArray());
        
        return sanitized.Trim().Replace(' ', '_');
    }
}

/// <summary>
/// Summary info about a saved session
/// </summary>
public class SessionInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string VesselName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool HasPitchData { get; set; }
    public bool HasRollData { get; set; }
    
    public string DisplayName => !string.IsNullOrEmpty(ProjectName) 
        ? $"{ProjectName} - {VesselName}" 
        : Path.GetFileNameWithoutExtension(FilePath);
    
    public string LastModified => (ModifiedAt ?? CreatedAt).ToString("g");
}
