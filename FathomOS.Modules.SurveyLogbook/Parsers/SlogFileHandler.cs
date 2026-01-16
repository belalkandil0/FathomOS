// ============================================================================
// Fathom OS - Survey Electronic Logbook Module
// File: Parsers/SlogFileHandler.cs
// Purpose: Handler for .slog/.slogz file format save and load operations
// ============================================================================

using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FathomOS.Modules.SurveyLogbook.Models;

namespace FathomOS.Modules.SurveyLogbook.Parsers;

/// <summary>
/// Handles saving and loading .slog (Survey Log) files.
/// 
/// File Format Specification:
/// - .slog: Uncompressed JSON format for readability
/// - .slogz: GZip compressed JSON format for space efficiency
/// 
/// The file contains:
/// - Project information
/// - All survey log entries (DVR recordings, position fixes, EIVA data logs)
/// - DPR reports
/// - Metadata with checksums for data integrity
/// </summary>
public class SlogFileHandler
{
    /// <summary>
    /// Current file format version.
    /// </summary>
    public const string CurrentVersion = "1.0";
    
    /// <summary>
    /// File extension for uncompressed format.
    /// </summary>
    public const string UncompressedExtension = ".slog";
    
    /// <summary>
    /// File extension for compressed format.
    /// </summary>
    public const string CompressedExtension = ".slogz";
    
    /// <summary>
    /// Format type identifier.
    /// </summary>
    public const string FormatType = "FathomOS.SurveyLog";
    
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<string> _errors = new();
    
    /// <summary>
    /// Gets any errors from the last operation.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();
    
    /// <summary>
    /// Initializes a new instance of the SlogFileHandler.
    /// </summary>
    public SlogFileHandler()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }
    
    /// <summary>
    /// Saves a survey log file to disk.
    /// </summary>
    /// <param name="filePath">Path to save the file</param>
    /// <param name="logFile">Survey log file data</param>
    /// <param name="compress">Whether to use compression (.slogz)</param>
    /// <returns>True if save was successful</returns>
    public bool Save(string filePath, SurveyLogFile logFile, bool compress = false)
    {
        _errors.Clear();
        
        try
        {
            // Update metadata before saving
            logFile.UpdateMetadata();
            logFile.ExportDate = DateTime.UtcNow;
            logFile.FileVersion = CurrentVersion;
            logFile.FormatType = FormatType;
            logFile.Metadata.CreatedByVersion = GetAssemblyVersion();
            
            // Calculate checksum
            logFile.Metadata.Checksum = CalculateChecksum(logFile);
            
            // Ensure correct extension
            var extension = compress ? CompressedExtension : UncompressedExtension;
            if (!filePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                filePath = Path.ChangeExtension(filePath, extension);
            }
            
            // Serialize to JSON
            var json = JsonSerializer.Serialize(logFile, _jsonOptions);
            
            if (compress)
            {
                // Save as compressed .slogz
                using var fileStream = File.Create(filePath);
                using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);
                using var writer = new StreamWriter(gzipStream, Encoding.UTF8);
                writer.Write(json);
            }
            else
            {
                // Save as uncompressed .slog
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _errors.Add($"Failed to save file: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Loads a survey log file from disk.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>Loaded survey log file or null if loading fails</returns>
    public SurveyLogFile? Load(string filePath)
    {
        _errors.Clear();
        
        if (!File.Exists(filePath))
        {
            _errors.Add($"File not found: {filePath}");
            return null;
        }
        
        try
        {
            string json;
            
            if (filePath.EndsWith(CompressedExtension, StringComparison.OrdinalIgnoreCase))
            {
                // Load compressed .slogz
                using var fileStream = File.OpenRead(filePath);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzipStream, Encoding.UTF8);
                json = reader.ReadToEnd();
            }
            else
            {
                // Load uncompressed .slog
                json = File.ReadAllText(filePath, Encoding.UTF8);
            }
            
            var logFile = JsonSerializer.Deserialize<SurveyLogFile>(json, _jsonOptions);
            
            if (logFile == null)
            {
                _errors.Add("Failed to deserialize file content");
                return null;
            }
            
            // Validate format
            if (logFile.FormatType != FormatType)
            {
                _errors.Add($"Invalid format type: expected '{FormatType}', got '{logFile.FormatType}'");
            }
            
            // Check version compatibility
            if (!IsVersionCompatible(logFile.FileVersion))
            {
                _errors.Add($"Incompatible file version: {logFile.FileVersion}");
            }
            
            // Verify checksum
            var storedChecksum = logFile.Metadata?.Checksum;
            if (!string.IsNullOrEmpty(storedChecksum))
            {
                var calculatedChecksum = CalculateChecksum(logFile);
                if (storedChecksum != calculatedChecksum)
                {
                    _errors.Add("Checksum verification failed - file may be corrupted");
                }
            }
            
            return logFile;
        }
        catch (JsonException ex)
        {
            _errors.Add($"JSON parsing error: {ex.Message}");
            return null;
        }
        catch (InvalidDataException ex)
        {
            _errors.Add($"Decompression error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _errors.Add($"Failed to load file: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Validates a file without fully loading it.
    /// </summary>
    public ValidationResult Validate(string filePath)
    {
        var result = new ValidationResult { FilePath = filePath };
        
        if (!File.Exists(filePath))
        {
            result.Errors.Add("File not found");
            return result;
        }
        
        try
        {
            var fileInfo = new FileInfo(filePath);
            result.FileSize = fileInfo.Length;
            result.LastModified = fileInfo.LastWriteTimeUtc;
            
            // Try to load and validate
            var logFile = Load(filePath);
            
            if (logFile == null)
            {
                result.Errors.AddRange(_errors);
                return result;
            }
            
            result.IsValid = _errors.Count == 0;
            result.FileVersion = logFile.FileVersion;
            result.TotalEntries = logFile.Metadata?.TotalEntries ?? 0;
            result.DateRange = logFile.Metadata?.DateRange;
            result.HasWarnings = _errors.Count > 0;
            result.Warnings.AddRange(_errors);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Validation failed: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets file information without loading full content.
    /// </summary>
    public SlogFileInfo? GetFileInfo(string filePath)
    {
        if (!File.Exists(filePath))
            return null;
        
        try
        {
            var fileInfo = new FileInfo(filePath);
            var isCompressed = filePath.EndsWith(CompressedExtension, StringComparison.OrdinalIgnoreCase);
            
            // Load just enough to get metadata
            var logFile = Load(filePath);
            if (logFile == null)
                return null;
            
            return new SlogFileInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSize = fileInfo.Length,
                IsCompressed = isCompressed,
                FileVersion = logFile.FileVersion,
                ExportDate = logFile.ExportDate,
                ExportedBy = logFile.ExportedBy,
                ProjectName = logFile.ProjectInfo?.ProjectName,
                TotalEntries = logFile.Metadata?.TotalEntries ?? 0,
                DvrRecordingCount = logFile.Metadata?.DvrRecordingCount ?? 0,
                PositionFixCount = logFile.Metadata?.PositionFixCount ?? 0,
                EivaDataLogCount = logFile.Metadata?.EivaDataLogCount ?? 0,
                DprReportCount = logFile.Metadata?.DprReportCount ?? 0,
                DateRange = logFile.Metadata?.DateRange,
                LastModified = fileInfo.LastWriteTimeUtc
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Merges multiple .slog files into one.
    /// </summary>
    public SurveyLogFile? MergeFiles(IEnumerable<string> filePaths)
    {
        var files = filePaths.ToList();
        if (files.Count == 0)
        {
            _errors.Add("No files to merge");
            return null;
        }
        
        var merged = new SurveyLogFile();
        
        foreach (var filePath in files)
        {
            var logFile = Load(filePath);
            if (logFile == null)
            {
                _errors.Add($"Failed to load: {filePath}");
                continue;
            }
            
            // Use first file's project info
            merged.ProjectInfo ??= logFile.ProjectInfo;
            
            // Merge data
            if (logFile.SurveyLogData != null)
            {
                merged.SurveyLogData.DvrRecordings.AddRange(logFile.SurveyLogData.DvrRecordings);
                merged.SurveyLogData.PositionFixes.AddRange(logFile.SurveyLogData.PositionFixes);
                merged.SurveyLogData.EivaDataLogs.AddRange(logFile.SurveyLogData.EivaDataLogs);
                merged.SurveyLogData.ManualEntries.AddRange(logFile.SurveyLogData.ManualEntries);
            }
            
            // Merge DPR reports
            if (logFile.DprReports != null)
            {
                merged.DprReports.AddRange(logFile.DprReports);
            }
        }
        
        // Remove duplicates
        merged.SurveyLogData.DvrRecordings = merged.SurveyLogData.DvrRecordings
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .ToList();
        
        merged.SurveyLogData.PositionFixes = merged.SurveyLogData.PositionFixes
            .GroupBy(f => new { f.Date, f.Time, f.Easting, f.Northing })
            .Select(g => g.First())
            .ToList();
        
        merged.DprReports = merged.DprReports
            .GroupBy(r => r.ReportDate)
            .Select(g => g.First())
            .ToList();
        
        merged.UpdateMetadata();
        return merged;
    }
    
    /// <summary>
    /// Calculates SHA256 checksum for data integrity.
    /// </summary>
    private string CalculateChecksum(SurveyLogFile logFile)
    {
        try
        {
            // Temporarily clear checksum for calculation
            var originalChecksum = logFile.Metadata?.Checksum;
            if (logFile.Metadata != null)
            {
                logFile.Metadata.Checksum = null;
            }
            
            var json = JsonSerializer.Serialize(logFile.SurveyLogData, _jsonOptions);
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            var checksum = Convert.ToBase64String(hashBytes);
            
            // Restore original checksum
            if (logFile.Metadata != null)
            {
                logFile.Metadata.Checksum = originalChecksum;
            }
            
            return checksum;
        }
        catch
        {
            return string.Empty;
        }
    }
    
    /// <summary>
    /// Checks if a file version is compatible with current version.
    /// </summary>
    private bool IsVersionCompatible(string fileVersion)
    {
        if (string.IsNullOrEmpty(fileVersion))
            return false;
        
        // Simple major version check
        var currentMajor = CurrentVersion.Split('.')[0];
        var fileMajor = fileVersion.Split('.')[0];
        
        return currentMajor == fileMajor;
    }
    
    /// <summary>
    /// Gets the assembly version string.
    /// </summary>
    private string GetAssemblyVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0.0";
        }
        catch
        {
            return "1.0.0.0";
        }
    }
}

/// <summary>
/// Result of file validation.
/// </summary>
public class ValidationResult
{
    public string FilePath { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string FileVersion { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime LastModified { get; set; }
    public int TotalEntries { get; set; }
    public DateRange? DateRange { get; set; }
    public bool HasWarnings { get; set; }
    public List<string> Warnings { get; } = new();
    public List<string> Errors { get; } = new();
}

/// <summary>
/// Summary information about a .slog file.
/// </summary>
public class SlogFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public bool IsCompressed { get; set; }
    public string FileVersion { get; set; } = string.Empty;
    public DateTime ExportDate { get; set; }
    public string? ExportedBy { get; set; }
    public string? ProjectName { get; set; }
    public int TotalEntries { get; set; }
    public int DvrRecordingCount { get; set; }
    public int PositionFixCount { get; set; }
    public int EivaDataLogCount { get; set; }
    public int DprReportCount { get; set; }
    public DateRange? DateRange { get; set; }
    public DateTime LastModified { get; set; }
    
    public string FileSizeDisplay
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = FileSize;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
