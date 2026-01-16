using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FathomOS.Modules.UsblVerification.Models;

namespace FathomOS.Modules.UsblVerification.Services;

/// <summary>
/// Service for storing and retrieving historical verification records
/// </summary>
public class HistoricalDatabaseService
{
    private readonly string _databasePath;
    private readonly string _indexPath;
    private VerificationIndex _index;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public HistoricalDatabaseService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _databasePath = Path.Combine(appData, "FathomOS", "UsblVerification", "History");
        _indexPath = Path.Combine(_databasePath, "index.json");
        
        Directory.CreateDirectory(_databasePath);
        LoadIndex();
    }
    
    #region Index Management
    
    private void LoadIndex()
    {
        try
        {
            if (File.Exists(_indexPath))
            {
                var json = File.ReadAllText(_indexPath);
                _index = JsonSerializer.Deserialize<VerificationIndex>(json, JsonOptions) ?? new();
            }
            else
            {
                _index = new VerificationIndex();
            }
        }
        catch
        {
            _index = new VerificationIndex();
        }
    }
    
    private void SaveIndex()
    {
        try
        {
            var json = JsonSerializer.Serialize(_index, JsonOptions);
            File.WriteAllText(_indexPath, json);
        }
        catch { }
    }
    
    #endregion
    
    #region Record Management
    
    /// <summary>
    /// Save a verification record to the database
    /// </summary>
    public string SaveRecord(VerificationRecord record)
    {
        if (string.IsNullOrEmpty(record.Id))
            record.Id = Guid.NewGuid().ToString("N");
        
        record.SavedAt = DateTime.Now;
        
        var recordPath = Path.Combine(_databasePath, $"{record.Id}.json");
        var json = JsonSerializer.Serialize(record, JsonOptions);
        File.WriteAllText(recordPath, json);
        
        // Update index
        var indexEntry = _index.Records.FirstOrDefault(r => r.Id == record.Id);
        if (indexEntry == null)
        {
            indexEntry = new VerificationIndexEntry { Id = record.Id };
            _index.Records.Add(indexEntry);
        }
        
        indexEntry.ProjectName = record.ProjectName;
        indexEntry.VesselName = record.VesselName;
        indexEntry.ClientName = record.ClientName;
        indexEntry.VerificationDate = record.VerificationDate;
        indexEntry.SavedAt = record.SavedAt;
        indexEntry.OverallPassed = record.OverallPassed;
        indexEntry.CertificateNumber = record.CertificateNumber;
        indexEntry.TransponderName = record.TransponderName;
        
        SaveIndex();
        
        return record.Id;
    }
    
    /// <summary>
    /// Load a verification record by ID
    /// </summary>
    public VerificationRecord? LoadRecord(string id)
    {
        var recordPath = Path.Combine(_databasePath, $"{id}.json");
        if (!File.Exists(recordPath)) return null;
        
        try
        {
            var json = File.ReadAllText(recordPath);
            return JsonSerializer.Deserialize<VerificationRecord>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Delete a verification record
    /// </summary>
    public bool DeleteRecord(string id)
    {
        var recordPath = Path.Combine(_databasePath, $"{id}.json");
        
        try
        {
            if (File.Exists(recordPath))
                File.Delete(recordPath);
            
            _index.Records.RemoveAll(r => r.Id == id);
            SaveIndex();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    #endregion
    
    #region Query Methods
    
    /// <summary>
    /// Get all verification records (index only)
    /// </summary>
    public List<VerificationIndexEntry> GetAllRecords()
    {
        return _index.Records.OrderByDescending(r => r.VerificationDate).ToList();
    }
    
    /// <summary>
    /// Search records by various criteria
    /// </summary>
    public List<VerificationIndexEntry> SearchRecords(
        string? projectName = null,
        string? vesselName = null,
        string? clientName = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        bool? passed = null)
    {
        var query = _index.Records.AsEnumerable();
        
        if (!string.IsNullOrEmpty(projectName))
            query = query.Where(r => r.ProjectName?.Contains(projectName, StringComparison.OrdinalIgnoreCase) == true);
        
        if (!string.IsNullOrEmpty(vesselName))
            query = query.Where(r => r.VesselName?.Contains(vesselName, StringComparison.OrdinalIgnoreCase) == true);
        
        if (!string.IsNullOrEmpty(clientName))
            query = query.Where(r => r.ClientName?.Contains(clientName, StringComparison.OrdinalIgnoreCase) == true);
        
        if (fromDate.HasValue)
            query = query.Where(r => r.VerificationDate >= fromDate.Value);
        
        if (toDate.HasValue)
            query = query.Where(r => r.VerificationDate <= toDate.Value);
        
        if (passed.HasValue)
            query = query.Where(r => r.OverallPassed == passed.Value);
        
        return query.OrderByDescending(r => r.VerificationDate).ToList();
    }
    
    /// <summary>
    /// Get recent records (last N)
    /// </summary>
    public List<VerificationIndexEntry> GetRecentRecords(int count = 10)
    {
        return _index.Records
            .OrderByDescending(r => r.SavedAt)
            .Take(count)
            .ToList();
    }
    
    /// <summary>
    /// Get records by vessel
    /// </summary>
    public List<VerificationIndexEntry> GetRecordsByVessel(string vesselName)
    {
        return _index.Records
            .Where(r => r.VesselName?.Equals(vesselName, StringComparison.OrdinalIgnoreCase) == true)
            .OrderByDescending(r => r.VerificationDate)
            .ToList();
    }
    
    /// <summary>
    /// Get pass/fail statistics
    /// </summary>
    public VerificationStatistics GetStatistics()
    {
        return new VerificationStatistics
        {
            TotalRecords = _index.Records.Count,
            PassedCount = _index.Records.Count(r => r.OverallPassed),
            FailedCount = _index.Records.Count(r => !r.OverallPassed),
            UniqueVessels = _index.Records.Select(r => r.VesselName).Distinct().Count(),
            UniqueClients = _index.Records.Select(r => r.ClientName).Distinct().Count(),
            OldestRecord = _index.Records.MinBy(r => r.VerificationDate)?.VerificationDate,
            NewestRecord = _index.Records.MaxBy(r => r.VerificationDate)?.VerificationDate
        };
    }
    
    #endregion
    
    #region Export/Import
    
    /// <summary>
    /// Export all records to a backup file
    /// </summary>
    public void ExportDatabase(string exportPath)
    {
        var backup = new DatabaseBackup
        {
            ExportedAt = DateTime.Now,
            Version = "1.0",
            Records = new List<VerificationRecord>()
        };
        
        foreach (var entry in _index.Records)
        {
            var record = LoadRecord(entry.Id);
            if (record != null)
                backup.Records.Add(record);
        }
        
        var json = JsonSerializer.Serialize(backup, JsonOptions);
        File.WriteAllText(exportPath, json);
    }
    
    /// <summary>
    /// Import records from a backup file
    /// </summary>
    public int ImportDatabase(string importPath, bool overwriteExisting = false)
    {
        if (!File.Exists(importPath)) return 0;
        
        var json = File.ReadAllText(importPath);
        var backup = JsonSerializer.Deserialize<DatabaseBackup>(json, JsonOptions);
        
        if (backup?.Records == null) return 0;
        
        int imported = 0;
        foreach (var record in backup.Records)
        {
            var existing = _index.Records.FirstOrDefault(r => r.Id == record.Id);
            if (existing != null && !overwriteExisting)
                continue;
            
            SaveRecord(record);
            imported++;
        }
        
        return imported;
    }
    
    #endregion
}

#region Database Models

/// <summary>
/// Index of all verification records
/// </summary>
public class VerificationIndex
{
    public List<VerificationIndexEntry> Records { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

/// <summary>
/// Lightweight index entry for quick searching
/// </summary>
public class VerificationIndexEntry
{
    public string Id { get; set; } = "";
    public string? ProjectName { get; set; }
    public string? VesselName { get; set; }
    public string? ClientName { get; set; }
    public string? TransponderName { get; set; }
    public DateTime VerificationDate { get; set; }
    public DateTime SavedAt { get; set; }
    public bool OverallPassed { get; set; }
    public string? CertificateNumber { get; set; }
    
    // Display helpers
    public string PassFailDisplay => OverallPassed ? "PASS" : "FAIL";
    public string DateDisplay => VerificationDate.ToString("yyyy-MM-dd");
}

/// <summary>
/// Full verification record with all data
/// </summary>
public class VerificationRecord
{
    public string Id { get; set; } = "";
    public DateTime SavedAt { get; set; }
    
    // Project info
    public string? ProjectName { get; set; }
    public string? VesselName { get; set; }
    public string? ClientName { get; set; }
    public string? ProcessorName { get; set; }
    public DateTime VerificationDate { get; set; }
    
    // Equipment info
    public string? TransponderName { get; set; }
    public string? TransponderSerial { get; set; }
    public string? UsblSystem { get; set; }
    
    // Results summary
    public bool OverallPassed { get; set; }
    public bool SpinTestPassed { get; set; }
    public bool TransitTestPassed { get; set; }
    public double ToleranceMeters { get; set; }
    public double QualityScore { get; set; }
    
    // Statistics
    public double SpinMeanOffset { get; set; }
    public double SpinStdDev { get; set; }
    public double SpinCEP { get; set; }
    public double SpinR95 { get; set; }
    public int SpinPointCount { get; set; }
    
    public double TransitMeanOffset { get; set; }
    public double TransitStdDev { get; set; }
    public int TransitPointCount { get; set; }
    
    // Mean position
    public double MeanEasting { get; set; }
    public double MeanNorthing { get; set; }
    public double MeanDepth { get; set; }
    
    // Per-heading results
    public List<HeadingResult> HeadingResults { get; set; } = new();
    
    // Certificate
    public string? CertificateNumber { get; set; }
    public string? CertificatePath { get; set; }
    public byte[]? DigitalSignature { get; set; }
    
    // Notes
    public string? Notes { get; set; }
}

/// <summary>
/// Statistics summary for database
/// </summary>
public class VerificationStatistics
{
    public int TotalRecords { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int UniqueVessels { get; set; }
    public int UniqueClients { get; set; }
    public DateTime? OldestRecord { get; set; }
    public DateTime? NewestRecord { get; set; }
    
    public double PassRate => TotalRecords > 0 ? (double)PassedCount / TotalRecords * 100 : 0;
}

/// <summary>
/// Database backup container
/// </summary>
public class DatabaseBackup
{
    public DateTime ExportedAt { get; set; }
    public string Version { get; set; } = "1.0";
    public List<VerificationRecord> Records { get; set; } = new();
}

#endregion
