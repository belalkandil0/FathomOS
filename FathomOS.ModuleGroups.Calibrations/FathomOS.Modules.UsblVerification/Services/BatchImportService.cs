using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FathomOS.Modules.UsblVerification.Models;
using FathomOS.Modules.UsblVerification.Parsers;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for batch importing USBL data files from a folder
/// </summary>
public class BatchImportService
{
    private readonly UsblNpdParser _parser;
    private readonly UnitConversionService _unitService;
    
    // Patterns to identify spin test files by heading
    private static readonly Dictionary<int, string[]> SpinPatterns = new()
    {
        { 0,   new[] { "000", "0deg", "north", "hdg000", "hdg_000", "spin_000", "spin000", "heading_0" } },
        { 90,  new[] { "090", "90deg", "east", "hdg090", "hdg_090", "spin_090", "spin090", "heading_90" } },
        { 180, new[] { "180", "180deg", "south", "hdg180", "hdg_180", "spin_180", "spin180", "heading_180" } },
        { 270, new[] { "270", "270deg", "west", "hdg270", "hdg_270", "spin_270", "spin270", "heading_270" } }
    };
    
    // Patterns to identify transit files
    private static readonly Dictionary<int, string[]> TransitPatterns = new()
    {
        { 1, new[] { "transit_1", "transit1", "line_1", "line1", "reciprocal_1", "pass_1", "run_1" } },
        { 2, new[] { "transit_2", "transit2", "line_2", "line2", "reciprocal_2", "pass_2", "run_2" } }
    };
    
    public BatchImportService()
    {
        _parser = new UsblNpdParser();
        _unitService = new UnitConversionService();
    }
    
    /// <summary>
    /// Import all spin test files from a folder
    /// </summary>
    public BatchImportResult ImportSpinFromFolder(string folderPath, bool autoDetectUnits = true)
    {
        var result = new BatchImportResult { FolderPath = folderPath };
        
        if (!Directory.Exists(folderPath))
        {
            result.Errors.Add($"Folder not found: {folderPath}");
            return result;
        }
        
        var npdFiles = Directory.GetFiles(folderPath, "*.npd", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(folderPath, "*.NPD", SearchOption.TopDirectoryOnly))
            .Distinct()
            .ToList();
        
        if (!npdFiles.Any())
        {
            result.Errors.Add("No NPD files found in the selected folder");
            return result;
        }
        
        // Try to match files to headings
        var matchedFiles = new Dictionary<int, string>();
        var unmatchedFiles = new List<string>();
        
        foreach (var file in npdFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            bool matched = false;
            
            foreach (var (heading, patterns) in SpinPatterns)
            {
                if (patterns.Any(p => fileName.Contains(p)))
                {
                    if (!matchedFiles.ContainsKey(heading))
                    {
                        matchedFiles[heading] = file;
                        matched = true;
                        break;
                    }
                }
            }
            
            if (!matched)
                unmatchedFiles.Add(file);
        }
        
        // If we couldn't match all 4, try to match by gyro values in the files
        if (matchedFiles.Count < 4 && unmatchedFiles.Any())
        {
            foreach (var file in unmatchedFiles.ToList())
            {
                try
                {
                    var observations = _parser.Parse(file);
                    if (observations.Any())
                    {
                        var avgGyro = observations.Average(o => o.VesselGyro);
                        var heading = GetNearestHeading(avgGyro);
                        
                        if (!matchedFiles.ContainsKey(heading))
                        {
                            matchedFiles[heading] = file;
                            unmatchedFiles.Remove(file);
                            result.Warnings.Add($"Matched '{Path.GetFileName(file)}' to {heading}° based on gyro values (avg: {avgGyro:F1}°)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Could not analyze '{Path.GetFileName(file)}': {ex.Message}");
                }
            }
        }
        
        // Validate we have all 4 headings
        var missingHeadings = new[] { 0, 90, 180, 270 }.Where(h => !matchedFiles.ContainsKey(h)).ToList();
        if (missingHeadings.Any())
        {
            result.Warnings.Add($"Could not find files for headings: {string.Join(", ", missingHeadings.Select(h => $"{h}°"))}");
        }
        
        result.SpinFileMapping = matchedFiles.ToDictionary(
            kvp => kvp.Key, 
            kvp => Path.GetFileName(kvp.Value));
        
        // Auto-detect units from first file
        if (autoDetectUnits && matchedFiles.Any())
        {
            var firstFile = matchedFiles.Values.First();
            var detection = DetectUnitsFromFile(firstFile);
            result.DetectedUnit = detection.DetectedUnit;
            result.UnitAutoDetected = detection.IsHighConfidence;
            
            if (detection.IsHighConfidence)
            {
                result.Warnings.Add($"Auto-detected units: {detection.DetectedUnit} (confidence: {detection.Confidence:P0}, reason: {detection.Reason})");
            }
        }
        
        result.LoadedFiles = matchedFiles.Values.Select(Path.GetFileName).ToList()!;
        result.TotalPointsLoaded = 0; // Will be set by caller after loading
        result.Success = matchedFiles.Count >= 4;
        
        return result;
    }
    
    /// <summary>
    /// Import transit files from a folder
    /// </summary>
    public BatchImportResult ImportTransitFromFolder(string folderPath)
    {
        var result = new BatchImportResult { FolderPath = folderPath };
        
        if (!Directory.Exists(folderPath))
        {
            result.Errors.Add($"Folder not found: {folderPath}");
            return result;
        }
        
        var npdFiles = Directory.GetFiles(folderPath, "*.npd", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(folderPath, "*.NPD", SearchOption.TopDirectoryOnly))
            .Distinct()
            .ToList();
        
        var matchedFiles = new Dictionary<int, string>();
        
        foreach (var file in npdFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            
            foreach (var (lineNum, patterns) in TransitPatterns)
            {
                if (patterns.Any(p => fileName.Contains(p)))
                {
                    if (!matchedFiles.ContainsKey(lineNum))
                    {
                        matchedFiles[lineNum] = file;
                        break;
                    }
                }
            }
        }
        
        result.TransitFileMapping = matchedFiles.ToDictionary(
            kvp => kvp.Key,
            kvp => Path.GetFileName(kvp.Value));
        
        result.LoadedFiles = matchedFiles.Values.Select(Path.GetFileName).ToList()!;
        result.Success = matchedFiles.Count >= 2;
        
        return result;
    }
    
    /// <summary>
    /// Get the full file path from batch result
    /// </summary>
    public string? GetSpinFilePath(BatchImportResult result, int heading)
    {
        if (result.SpinFileMapping.TryGetValue(heading, out var fileName))
        {
            return Path.Combine(result.FolderPath, fileName);
        }
        return null;
    }
    
    /// <summary>
    /// Get transit file path from batch result
    /// </summary>
    public string? GetTransitFilePath(BatchImportResult result, int lineNumber)
    {
        if (result.TransitFileMapping.TryGetValue(lineNumber, out var fileName))
        {
            return Path.Combine(result.FolderPath, fileName);
        }
        return null;
    }
    
    /// <summary>
    /// Detect units from a file based on coordinate magnitudes
    /// </summary>
    public UnitDetectionResult DetectUnitsFromFile(string filePath)
    {
        var result = new UnitDetectionResult();
        
        try
        {
            var observations = _parser.Parse(filePath);
            if (!observations.Any())
            {
                result.Reason = "No data points found";
                return result;
            }
            
            // Get coordinate ranges
            var eastings = observations.Select(o => Math.Abs(o.TransponderEasting)).ToList();
            var northings = observations.Select(o => Math.Abs(o.TransponderNorthing)).ToList();
            
            result.MaxCoordinate = Math.Max(eastings.Max(), northings.Max());
            result.MinCoordinate = Math.Min(eastings.Min(), northings.Min());
            
            // Apply detection logic
            result = _unitService.DetectUnits(result.MaxCoordinate, result.MinCoordinate);
        }
        catch (Exception ex)
        {
            result.Reason = $"Error: {ex.Message}";
            result.Confidence = 0;
        }
        
        return result;
    }
    
    private static int GetNearestHeading(double gyro)
    {
        // Normalize to 0-360
        while (gyro < 0) gyro += 360;
        while (gyro >= 360) gyro -= 360;
        
        // Find nearest cardinal heading
        var headings = new[] { 0, 90, 180, 270 };
        return headings.OrderBy(h => Math.Min(Math.Abs(gyro - h), 360 - Math.Abs(gyro - h))).First();
    }
}
