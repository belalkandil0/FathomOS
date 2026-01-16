using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for saving and loading USBL verification projects
/// </summary>
public class ProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Save project to JSON file
    /// </summary>
    public void SaveProject(UsblVerificationProject project, string filePath)
    {
        project.ModifiedDate = DateTime.Now;
        var json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Load project from JSON file
    /// </summary>
    public UsblVerificationProject LoadProject(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Project file not found", filePath);

        var json = File.ReadAllText(filePath);
        var project = JsonSerializer.Deserialize<UsblVerificationProject>(json, JsonOptions);
        
        return project ?? throw new InvalidDataException("Failed to deserialize project");
    }

    /// <summary>
    /// Create project backup before saving
    /// </summary>
    public void CreateBackup(string filePath)
    {
        if (!File.Exists(filePath)) return;

        var backupDir = Path.Combine(Path.GetDirectoryName(filePath) ?? ".", "backups");
        Directory.CreateDirectory(backupDir);

        var backupName = $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        var backupPath = Path.Combine(backupDir, backupName);

        File.Copy(filePath, backupPath, true);

        // Keep only last 10 backups
        var backups = Directory.GetFiles(backupDir, "*.json")
            .OrderByDescending(f => File.GetCreationTime(f))
            .Skip(10)
            .ToList();

        foreach (var old in backups)
        {
            try { File.Delete(old); } catch { }
        }
    }

    /// <summary>
    /// Get default project file path
    /// </summary>
    public string GetDefaultProjectPath(string projectName)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var projectDir = Path.Combine(appData, "FathomOS", "UsblVerification", "Projects");
        Directory.CreateDirectory(projectDir);

        var safeName = string.Join("_", projectName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(projectDir, $"{safeName}.usblproj");
    }

    /// <summary>
    /// Export project summary to text file
    /// </summary>
    public void ExportSummary(UsblVerificationProject project, VerificationResults results, string filePath)
    {
        using var writer = new StreamWriter(filePath);
        
        writer.WriteLine("=" + new string('=', 60));
        writer.WriteLine("USBL VERIFICATION SUMMARY REPORT");
        writer.WriteLine("=" + new string('=', 60));
        writer.WriteLine();
        
        writer.WriteLine("PROJECT INFORMATION");
        writer.WriteLine("-" + new string('-', 40));
        writer.WriteLine($"Project Name:    {project.ProjectName}");
        writer.WriteLine($"Client:          {project.ClientName}");
        writer.WriteLine($"Vessel:          {project.VesselName}");
        writer.WriteLine($"Transponder:     {project.TransponderId}");
        writer.WriteLine($"Survey Date:     {project.SurveyDate:yyyy-MM-dd}");
        writer.WriteLine($"Surveyor:        {project.SurveyorName}");
        writer.WriteLine();
        
        writer.WriteLine("SPIN TEST RESULTS");
        writer.WriteLine("-" + new string('-', 40));
        writer.WriteLine($"Overall Average Easting:   {results.SpinOverallAverageEasting:F3} m");
        writer.WriteLine($"Overall Average Northing:  {results.SpinOverallAverageNorthing:F3} m");
        writer.WriteLine($"Overall Average Depth:     {results.SpinOverallAverageDepth:F2} m");
        writer.WriteLine($"2DRMS:                     {results.Spin2DRMS:F3} m");
        writer.WriteLine($"Max Radial Difference:     {results.SpinMaxDiffRadial:F3} m");
        writer.WriteLine($"Tolerance:                 {results.SpinMaxAllowableDiff:F3} m");
        writer.WriteLine($"Result:                    {(results.SpinPassFail ? "PASS" : "FAIL")}");
        writer.WriteLine();
        
        writer.WriteLine("TRANSIT TEST RESULTS");
        writer.WriteLine("-" + new string('-', 40));
        writer.WriteLine($"Combined Average Easting:  {results.TransitCombinedAverageEasting:F3} m");
        writer.WriteLine($"Combined Average Northing: {results.TransitCombinedAverageNorthing:F3} m");
        writer.WriteLine($"Max Difference:            {results.TransitMaxDiffRadial:F3} m");
        writer.WriteLine($"Tolerance:                 {results.TransitMaxAllowableSpread:F3} m");
        writer.WriteLine($"Result:                    {(results.TransitPassFail ? "PASS" : "FAIL")}");
        writer.WriteLine();
        
        writer.WriteLine("ALIGNMENT VERIFICATION");
        writer.WriteLine("-" + new string('-', 40));
        writer.WriteLine($"Line 1 Residual Alignment: {results.Line1ResidualAlignment:F4}°");
        writer.WriteLine($"Line 2 Residual Alignment: {results.Line2ResidualAlignment:F4}°");
        writer.WriteLine($"Line 1 Scale Factor:       {results.Line1ScaleFactor:F5}");
        writer.WriteLine($"Line 2 Scale Factor:       {results.Line2ScaleFactor:F5}");
        writer.WriteLine($"Alignment Result:          {(results.Line1AlignmentPass && results.Line2AlignmentPass ? "PASS" : "FAIL")}");
        writer.WriteLine();
        
        writer.WriteLine("=" + new string('=', 60));
        writer.WriteLine($"OVERALL RESULT: {(results.OverallPass ? "PASS" : "FAIL")}");
        writer.WriteLine("=" + new string('=', 60));
        writer.WriteLine();
        writer.WriteLine($"Report generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }
}
