// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Parsers/DvrFolderParser.cs
// Purpose: Parser for VisualWorks DVR folder hierarchy and video files
// ============================================================================

using System.IO;
using System.Text.RegularExpressions;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Parsers;

/// <summary>
/// Parses VisualWorks DVR folder structures to extract recording information.
/// 
/// Typical folder hierarchy:
/// [Project Root]/
///   └── [Vehicle Name]/
///       └── [Date YYYY-MM-DD]/
///           └── A.Project_Task/
///               └── B.Sub_Task/
///                   └── C.Operation/
///                       └── video_001.wmv
///                       └── video_002.wmv
/// 
/// The parser monitors these folders for new video files and extracts
/// metadata from the folder structure.
/// </summary>
public class DvrFolderParser
{
    // Common video file extensions from VisualWorks
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wmv", ".avi", ".mpg", ".mpeg", ".mp4", ".mkv", ".mov", ".ts", ".mts"
    };
    
    // Image extensions
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif"
    };
    
    // Pattern to match letter prefix (A., B., C., etc.)
    private static readonly Regex LetterPrefixPattern = new(@"^[A-Z]\.\s*", RegexOptions.Compiled);
    
    // Pattern to match date folder (YYYY-MM-DD or YYYYMMDD)
    private static readonly Regex DateFolderPattern = new(
        @"^(\d{4})-?(\d{2})-?(\d{2})$", 
        RegexOptions.Compiled);
    
    // Pattern to extract time from filename (HH-MM-SS or HHMMSS)
    private static readonly Regex TimeFromFilenamePattern = new(
        @"(\d{2})-?(\d{2})-?(\d{2})", 
        RegexOptions.Compiled);
    
    private readonly List<string> _parseWarnings = new();
    
    /// <summary>
    /// Gets any warnings generated during parsing.
    /// </summary>
    public IReadOnlyList<string> ParseWarnings => _parseWarnings.AsReadOnly();
    
    /// <summary>
    /// Scans a folder and its subfolders for DVR recordings.
    /// </summary>
    /// <param name="rootPath">Root path to scan</param>
    /// <param name="vehicleMappings">Optional vehicle folder mappings</param>
    /// <returns>List of DVR recording sessions</returns>
    public List<DvrRecording> ScanFolder(string rootPath, List<VehicleFolderMapping>? vehicleMappings = null)
    {
        _parseWarnings.Clear();
        var recordings = new List<DvrRecording>();
        
        if (!Directory.Exists(rootPath))
        {
            _parseWarnings.Add($"Directory not found: {rootPath}");
            return recordings;
        }
        
        try
        {
            // Recursively find all video files
            var videoFiles = FindVideoFiles(rootPath);
            
            // Group by parent folder (each folder = one recording session)
            var folderGroups = videoFiles
                .GroupBy(f => Path.GetDirectoryName(f) ?? "")
                .Where(g => !string.IsNullOrEmpty(g.Key));
            
            foreach (var group in folderGroups)
            {
                var recording = ParseFolderAsRecording(group.Key, group.ToList(), vehicleMappings);
                if (recording != null)
                {
                    recordings.Add(recording);
                }
            }
        }
        catch (Exception ex)
        {
            _parseWarnings.Add($"Error scanning folder: {ex.Message}");
        }
        
        return recordings.OrderByDescending(r => r.StartTimestamp).ToList();
    }
    
    /// <summary>
    /// Parses a single folder as a DVR recording session.
    /// </summary>
    private DvrRecording? ParseFolderAsRecording(
        string folderPath, 
        List<string> videoFiles,
        List<VehicleFolderMapping>? vehicleMappings)
    {
        if (videoFiles.Count == 0)
            return null;
        
        var recording = new DvrRecording
        {
            Id = Guid.NewGuid(),
            FolderPath = folderPath
        };
        
        // Parse folder hierarchy
        ParseFolderHierarchy(recording, folderPath);
        
        // Determine vehicle from folder path or mappings
        recording.Vehicle = DetermineVehicle(folderPath, vehicleMappings);
        
        // Add video files and determine times
        DateTime? earliestTime = null;
        DateTime? latestTime = null;
        
        foreach (var videoFile in videoFiles.OrderBy(f => f))
        {
            recording.AddVideoFile(videoFile);
            
            // Try to get time from file
            var fileTime = GetFileTime(videoFile);
            
            if (!earliestTime.HasValue || fileTime < earliestTime)
                earliestTime = fileTime;
            
            if (!latestTime.HasValue || fileTime > latestTime)
                latestTime = fileTime;
        }
        
        // Set date and times
        if (earliestTime.HasValue)
        {
            recording.Date = earliestTime.Value.Date;
            recording.StartTime = earliestTime.Value.TimeOfDay;
        }
        
        if (latestTime.HasValue)
        {
            // Estimate end time (add typical video duration or use file modification time)
            var lastFile = videoFiles.Last();
            var lastFileInfo = new FileInfo(lastFile);
            recording.EndTime = lastFileInfo.LastWriteTime.TimeOfDay;
        }
        
        return recording;
    }
    
    /// <summary>
    /// Parses the folder hierarchy to extract project/task/operation.
    /// </summary>
    private void ParseFolderHierarchy(DvrRecording recording, string folderPath)
    {
        var parts = folderPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        // Work backwards through path looking for A./B./C. prefixes
        var taskParts = new List<(int level, string name)>();
        
        foreach (var part in parts)
        {
            // Check for date folder
            var dateMatch = DateFolderPattern.Match(part);
            if (dateMatch.Success)
            {
                if (DateTime.TryParse($"{dateMatch.Groups[1]}-{dateMatch.Groups[2]}-{dateMatch.Groups[3]}", 
                    out var date))
                {
                    recording.Date = date;
                }
                continue;
            }
            
            // Check for lettered prefix (A., B., C., etc.)
            if (LetterPrefixPattern.IsMatch(part))
            {
                var letter = part[0];
                var level = letter - 'A'; // A=0, B=1, C=2, etc.
                var cleanName = CleanFolderName(part);
                taskParts.Add((level, cleanName));
            }
        }
        
        // Assign to appropriate fields
        foreach (var (level, name) in taskParts.OrderBy(t => t.level))
        {
            switch (level)
            {
                case 0: // A.
                    recording.ProjectTask = name;
                    break;
                case 1: // B.
                    recording.SubTask = name;
                    break;
                case 2: // C.
                    recording.Operation = name;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Cleans a folder name by removing prefix and converting underscores.
    /// </summary>
    public static string CleanFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return string.Empty;
        
        // Remove letter prefix (A., B., C., etc.)
        var cleaned = LetterPrefixPattern.Replace(folderName, "");
        
        // Replace underscores with spaces
        cleaned = cleaned.Replace('_', ' ');
        
        // Trim and normalize whitespace
        cleaned = Regex.Replace(cleaned.Trim(), @"\s+", " ");
        
        return cleaned;
    }
    
    /// <summary>
    /// Determines vehicle name from folder path.
    /// </summary>
    private string DetermineVehicle(string folderPath, List<VehicleFolderMapping>? mappings)
    {
        if (mappings != null)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.Matches(folderPath))
                {
                    return mapping.VehicleName;
                }
            }
        }
        
        // Try to find vehicle from common patterns
        var parts = folderPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        // Look for common vehicle folder names
        foreach (var part in parts)
        {
            var upper = part.ToUpperInvariant();
            
            if (upper.Contains("ROV") || 
                upper.Contains("VESSEL") ||
                upper.Contains("AUV") ||
                upper.Contains("SURVEY"))
            {
                return part;
            }
        }
        
        // Return first folder after date or project root
        var lastDateIndex = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (DateFolderPattern.IsMatch(parts[i]))
            {
                lastDateIndex = i;
            }
        }
        
        if (lastDateIndex >= 0 && lastDateIndex + 1 < parts.Length)
        {
            return CleanFolderName(parts[lastDateIndex + 1]);
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Gets the time associated with a file (creation or modification).
    /// </summary>
    private DateTime GetFileTime(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // Try to extract time from filename first
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var timeMatch = TimeFromFilenamePattern.Match(fileName);
            
            if (timeMatch.Success)
            {
                var hours = int.Parse(timeMatch.Groups[1].Value);
                var minutes = int.Parse(timeMatch.Groups[2].Value);
                var seconds = int.Parse(timeMatch.Groups[3].Value);
                
                if (hours < 24 && minutes < 60 && seconds < 60)
                {
                    return fileInfo.CreationTime.Date
                        .AddHours(hours)
                        .AddMinutes(minutes)
                        .AddSeconds(seconds);
                }
            }
            
            // Fall back to file creation time
            return fileInfo.CreationTime < fileInfo.LastWriteTime 
                ? fileInfo.CreationTime 
                : fileInfo.LastWriteTime;
        }
        catch
        {
            return DateTime.Now;
        }
    }
    
    /// <summary>
    /// Finds all video files in a directory and subdirectories.
    /// </summary>
    private List<string> FindVideoFiles(string rootPath)
    {
        var files = new List<string>();
        
        try
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file);
                if (VideoExtensions.Contains(ext))
                {
                    files.Add(file);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _parseWarnings.Add($"Access denied: {ex.Message}");
        }
        catch (Exception ex)
        {
            _parseWarnings.Add($"Error finding files: {ex.Message}");
        }
        
        return files;
    }
    
    /// <summary>
    /// Finds all image files (captures) in a directory.
    /// </summary>
    public List<string> FindImageFiles(string folderPath)
    {
        var files = new List<string>();
        
        if (!Directory.Exists(folderPath))
            return files;
        
        try
        {
            foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file);
                if (ImageExtensions.Contains(ext))
                {
                    files.Add(file);
                }
            }
        }
        catch (Exception ex)
        {
            _parseWarnings.Add($"Error finding images: {ex.Message}");
        }
        
        return files;
    }
    
    /// <summary>
    /// Monitors a folder for new video files.
    /// </summary>
    public FileSystemWatcher CreateFolderWatcher(
        string rootPath, 
        Action<string> onNewFile,
        Action<string>? onDeletedFile = null)
    {
        var watcher = new FileSystemWatcher(rootPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        
        watcher.Created += (sender, e) =>
        {
            var ext = Path.GetExtension(e.FullPath);
            if (VideoExtensions.Contains(ext) || ImageExtensions.Contains(ext))
            {
                onNewFile?.Invoke(e.FullPath);
            }
        };
        
        if (onDeletedFile != null)
        {
            watcher.Deleted += (sender, e) =>
            {
                var ext = Path.GetExtension(e.FullPath);
                if (VideoExtensions.Contains(ext) || ImageExtensions.Contains(ext))
                {
                    onDeletedFile(e.FullPath);
                }
            };
        }
        
        return watcher;
    }
    
    /// <summary>
    /// Checks if a file is a video file.
    /// </summary>
    public static bool IsVideoFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return VideoExtensions.Contains(ext);
    }
    
    /// <summary>
    /// Checks if a file is an image file.
    /// </summary>
    public static bool IsImageFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ImageExtensions.Contains(ext);
    }
    
    /// <summary>
    /// Gets file size in human-readable format.
    /// </summary>
    public static string GetFileSizeDisplay(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}
