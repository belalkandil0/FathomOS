using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Data;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Service for backing up and restoring the local database and associated files.
/// Supports manual backup, automatic scheduled backups, and restore operations.
/// </summary>
public class BackupService
{
    private readonly LocalDatabaseService _dbService;
    private readonly ModuleSettings _settings;
    private readonly string _backupFolder;
    private readonly string _dataFolder;
    
    public BackupService(LocalDatabaseService dbService)
    {
        _dbService = dbService;
        _settings = ModuleSettings.Load();
        
        _dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FathomOS", "EquipmentInventory");
        
        _backupFolder = _settings.BackupSettings.BackupLocation ?? 
            Path.Combine(_dataFolder, "Backups");
        
        Directory.CreateDirectory(_backupFolder);
    }
    
    #region Backup Operations
    
    /// <summary>
    /// Create a full backup of the database and associated files
    /// </summary>
    public async Task<BackupResult> CreateBackupAsync(bool includePhotos = true, bool includeDocuments = true, string? customPath = null)
    {
        var result = new BackupResult { StartTime = DateTime.Now };
        
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var backupName = $"S7Equipment_Backup_{timestamp}";
            var tempFolder = Path.Combine(Path.GetTempPath(), backupName);
            var backupPath = customPath ?? Path.Combine(_backupFolder, $"{backupName}.zip");
            
            Directory.CreateDirectory(tempFolder);
            
            // 1. Backup database
            var dbSource = Path.Combine(_dataFolder, "local.db");
            if (File.Exists(dbSource))
            {
                // Ensure all changes are saved
                await _dbService.Context.SaveChangesAsync();
                
                // Copy database file
                File.Copy(dbSource, Path.Combine(tempFolder, "equipment.db"), true);
                result.DatabaseSize = new FileInfo(dbSource).Length;
            }
            
            // 2. Backup photos folder
            if (includePhotos)
            {
                var photosFolder = Path.Combine(_dataFolder, "Photos");
                if (Directory.Exists(photosFolder))
                {
                    var destPhotos = Path.Combine(tempFolder, "Photos");
                    CopyDirectory(photosFolder, destPhotos);
                    result.PhotosCount = Directory.GetFiles(destPhotos, "*", SearchOption.AllDirectories).Length;
                }
            }
            
            // 3. Backup documents folder
            if (includeDocuments)
            {
                var docsFolder = Path.Combine(_dataFolder, "Documents");
                if (Directory.Exists(docsFolder))
                {
                    var destDocs = Path.Combine(tempFolder, "Documents");
                    CopyDirectory(docsFolder, destDocs);
                    result.DocumentsCount = Directory.GetFiles(destDocs, "*", SearchOption.AllDirectories).Length;
                }
            }
            
            // 4. Backup settings
            var settingsPath = Path.Combine(_dataFolder, "settings.json");
            if (File.Exists(settingsPath))
            {
                File.Copy(settingsPath, Path.Combine(tempFolder, "settings.json"), true);
            }
            
            // 5. Create manifest
            var manifest = new BackupManifest
            {
                BackupDate = DateTime.UtcNow,
                ModuleVersion = "1.0.0",
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                DatabaseSize = result.DatabaseSize,
                PhotosCount = result.PhotosCount,
                DocumentsCount = result.DocumentsCount
            };
            
            var manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(tempFolder, "manifest.json"), manifestJson);
            
            // 6. Create ZIP archive
            if (File.Exists(backupPath))
                File.Delete(backupPath);
            
            ZipFile.CreateFromDirectory(tempFolder, backupPath, CompressionLevel.Optimal, false);
            
            // 7. Cleanup temp folder
            Directory.Delete(tempFolder, true);
            
            // 8. Update backup history
            result.Success = true;
            result.BackupPath = backupPath;
            result.BackupSize = new FileInfo(backupPath).Length;
            result.EndTime = DateTime.Now;
            
            // Save backup info
            await SaveBackupInfoAsync(result);
            
            // Cleanup old backups if needed
            await CleanupOldBackupsAsync();
            
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.Now;
            return result;
        }
    }
    
    /// <summary>
    /// Restore from a backup file
    /// </summary>
    public async Task<RestoreResult> RestoreFromBackupAsync(string backupPath)
    {
        var result = new RestoreResult { StartTime = DateTime.Now };
        
        try
        {
            if (!File.Exists(backupPath))
            {
                result.Error = "Backup file not found";
                return result;
            }
            
            var tempFolder = Path.Combine(Path.GetTempPath(), $"S7Equipment_Restore_{Guid.NewGuid():N}");
            
            // 1. Extract backup
            ZipFile.ExtractToDirectory(backupPath, tempFolder);
            
            // 2. Verify manifest
            var manifestPath = Path.Combine(tempFolder, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                result.Error = "Invalid backup: manifest.json not found";
                Directory.Delete(tempFolder, true);
                return result;
            }
            
            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestJson);
            result.BackupDate = manifest?.BackupDate;
            
            // 3. Close current database connection
            await _dbService.Context.Database.CloseConnectionAsync();
            
            // 4. Restore database
            var dbSource = Path.Combine(tempFolder, "equipment.db");
            var dbDest = Path.Combine(_dataFolder, "local.db");
            if (File.Exists(dbSource))
            {
                // Create backup of current database first
                if (File.Exists(dbDest))
                {
                    var currentBackup = dbDest + ".pre-restore";
                    File.Copy(dbDest, currentBackup, true);
                }
                
                File.Copy(dbSource, dbDest, true);
                result.DatabaseRestored = true;
            }
            
            // 5. Restore photos
            var photosSource = Path.Combine(tempFolder, "Photos");
            var photosDest = Path.Combine(_dataFolder, "Photos");
            if (Directory.Exists(photosSource))
            {
                if (Directory.Exists(photosDest))
                    Directory.Delete(photosDest, true);
                CopyDirectory(photosSource, photosDest);
                result.PhotosRestored = Directory.GetFiles(photosDest, "*", SearchOption.AllDirectories).Length;
            }
            
            // 6. Restore documents
            var docsSource = Path.Combine(tempFolder, "Documents");
            var docsDest = Path.Combine(_dataFolder, "Documents");
            if (Directory.Exists(docsSource))
            {
                if (Directory.Exists(docsDest))
                    Directory.Delete(docsDest, true);
                CopyDirectory(docsSource, docsDest);
                result.DocumentsRestored = Directory.GetFiles(docsDest, "*", SearchOption.AllDirectories).Length;
            }
            
            // 7. Optionally restore settings
            var settingsSource = Path.Combine(tempFolder, "settings.json");
            var settingsDest = Path.Combine(_dataFolder, "settings.json");
            if (File.Exists(settingsSource))
            {
                File.Copy(settingsSource, settingsDest, true);
                result.SettingsRestored = true;
            }
            
            // 8. Cleanup
            Directory.Delete(tempFolder, true);
            
            // 9. Reopen database connection
            await _dbService.Context.Database.OpenConnectionAsync();
            
            result.Success = true;
            result.EndTime = DateTime.Now;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.EndTime = DateTime.Now;
            return result;
        }
    }
    
    #endregion
    
    #region Backup Management
    
    /// <summary>
    /// Get list of available backups
    /// </summary>
    public List<BackupInfo> GetAvailableBackups()
    {
        var backups = new List<BackupInfo>();
        
        try
        {
            if (!Directory.Exists(_backupFolder))
                return backups;
            
            foreach (var file in Directory.GetFiles(_backupFolder, "S7Equipment_Backup_*.zip"))
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var info = new BackupInfo
                    {
                        FilePath = file,
                        FileName = fileInfo.Name,
                        Size = fileInfo.Length,
                        CreatedDate = fileInfo.CreationTime
                    };
                    
                    // Try to read manifest from backup
                    using var archive = ZipFile.OpenRead(file);
                    var manifestEntry = archive.GetEntry("manifest.json");
                    if (manifestEntry != null)
                    {
                        using var stream = manifestEntry.Open();
                        using var reader = new StreamReader(stream);
                        var json = reader.ReadToEnd();
                        var manifest = JsonSerializer.Deserialize<BackupManifest>(json);
                        if (manifest != null)
                        {
                            info.BackupDate = manifest.BackupDate;
                            info.MachineName = manifest.MachineName;
                            info.DatabaseSize = manifest.DatabaseSize;
                            info.PhotosCount = manifest.PhotosCount;
                            info.DocumentsCount = manifest.DocumentsCount;
                        }
                    }
                    
                    backups.Add(info);
                }
                catch
                {
                    // Skip invalid backup files
                }
            }
        }
        catch
        {
            // Return empty list on error
        }
        
        return backups.OrderByDescending(b => b.CreatedDate).ToList();
    }
    
    /// <summary>
    /// Delete a specific backup
    /// </summary>
    public bool DeleteBackup(string backupPath)
    {
        try
        {
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Check if auto-backup is due
    /// </summary>
    public bool IsAutoBackupDue()
    {
        if (!_settings.BackupSettings.AutoBackupEnabled)
            return false;
        
        var lastBackup = _settings.BackupSettings.LastAutoBackup;
        if (!lastBackup.HasValue)
            return true;
        
        var interval = _settings.BackupSettings.BackupFrequency switch
        {
            BackupFrequency.Daily => TimeSpan.FromDays(1),
            BackupFrequency.Weekly => TimeSpan.FromDays(7),
            BackupFrequency.Monthly => TimeSpan.FromDays(30),
            _ => TimeSpan.FromDays(1)
        };
        
        return DateTime.Now - lastBackup.Value > interval;
    }
    
    /// <summary>
    /// Perform auto-backup if due
    /// </summary>
    public async Task<BackupResult?> PerformAutoBackupIfDueAsync()
    {
        if (!IsAutoBackupDue())
            return null;
        
        var result = await CreateBackupAsync();
        
        if (result.Success)
        {
            _settings.BackupSettings.LastAutoBackup = DateTime.Now;
            _settings.Save();
        }
        
        return result;
    }
    
    /// <summary>
    /// Clean up old backups beyond retention count
    /// </summary>
    private async Task CleanupOldBackupsAsync()
    {
        await Task.Run(() =>
        {
            var backups = GetAvailableBackups();
            var keepCount = _settings.BackupSettings.KeepBackupCount;
            
            if (backups.Count > keepCount)
            {
                foreach (var backup in backups.Skip(keepCount))
                {
                    try
                    {
                        File.Delete(backup.FilePath);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
        });
    }
    
    private async Task SaveBackupInfoAsync(BackupResult result)
    {
        // Save last backup time
        _settings.BackupSettings.LastBackup = result.EndTime;
        if (_settings.BackupSettings.AutoBackupEnabled)
        {
            _settings.BackupSettings.LastAutoBackup = result.EndTime;
        }
        _settings.Save();
        await Task.CompletedTask;
    }
    
    #endregion
    
    #region Export/Import Portable Format
    
    /// <summary>
    /// Export to a portable format that can be shared
    /// </summary>
    public async Task<string> ExportPortableAsync(string destinationPath)
    {
        var result = await CreateBackupAsync(true, true, destinationPath);
        return result.Success ? destinationPath : throw new Exception(result.Error);
    }
    
    /// <summary>
    /// Import from a portable backup
    /// </summary>
    public async Task<RestoreResult> ImportPortableAsync(string sourcePath)
    {
        return await RestoreFromBackupAsync(sourcePath);
    }
    
    #endregion
    
    #region Helpers
    
    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
    
    #endregion
}

#region Models

public class BackupResult
{
    public bool Success { get; set; }
    public string? BackupPath { get; set; }
    public string? Error { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long DatabaseSize { get; set; }
    public long BackupSize { get; set; }
    public int PhotosCount { get; set; }
    public int DocumentsCount { get; set; }
    
    public TimeSpan Duration => EndTime - StartTime;
}

public class RestoreResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime? BackupDate { get; set; }
    public bool DatabaseRestored { get; set; }
    public int PhotosRestored { get; set; }
    public int DocumentsRestored { get; set; }
    public bool SettingsRestored { get; set; }
    
    public TimeSpan Duration => EndTime - StartTime;
}

public class BackupManifest
{
    public DateTime BackupDate { get; set; }
    public string ModuleVersion { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public long DatabaseSize { get; set; }
    public int PhotosCount { get; set; }
    public int DocumentsCount { get; set; }
}

public class BackupInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime BackupDate { get; set; }
    public string? MachineName { get; set; }
    public long DatabaseSize { get; set; }
    public int PhotosCount { get; set; }
    public int DocumentsCount { get; set; }
    
    public string SizeDisplay => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{Size / (1024.0 * 1024):F1} MB",
        _ => $"{Size / (1024.0 * 1024 * 1024):F2} GB"
    };
}

// BackupSettings and BackupFrequency are defined in ModuleSettings.cs

#endregion
