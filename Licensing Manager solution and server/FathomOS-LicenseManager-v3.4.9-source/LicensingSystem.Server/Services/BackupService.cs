// LicensingSystem.Server/Services/BackupService.cs
// Database backup and recovery service

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace LicensingSystem.Server.Services;

public interface IBackupService
{
    Task<DatabaseBackupRecord> CreateBackupAsync(string? createdBy = null, string? notes = null, 
        string backupType = "Manual", bool encrypt = true);
    Task<bool> RestoreBackupAsync(int backupId, string? decryptionKey = null);
    Task<List<DatabaseBackupRecord>> GetBackupsAsync(int limit = 50);
    Task<DatabaseBackupRecord?> GetLatestBackupAsync();
    Task<bool> DeleteBackupAsync(int backupId);
    Task<bool> VerifyBackupAsync(int backupId);
    Task<Stream> DownloadBackupAsync(int backupId);
    Task CleanupOldBackupsAsync(int keepCount = 10);
}

public class BackupService : IBackupService
{
    private readonly LicenseDbContext _db;
    private readonly IConfiguration _config;
    private readonly IAuditService _auditService;
    private readonly ILogger<BackupService> _logger;
    
    private readonly string _backupDirectory;

    public BackupService(
        LicenseDbContext db, 
        IConfiguration config,
        IAuditService auditService,
        ILogger<BackupService> logger)
    {
        _db = db;
        _config = config;
        _auditService = auditService;
        _logger = logger;
        
        _backupDirectory = Path.Combine(
            config["BackupDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "backups"),
            "database");
        
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<DatabaseBackupRecord> CreateBackupAsync(string? createdBy = null, 
        string? notes = null, string backupType = "Manual", bool encrypt = true)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"fathom_license_backup_{timestamp}.json.gz";
        var filePath = Path.Combine(_backupDirectory, fileName);

        // Collect all data
        var backupData = new BackupData
        {
            BackupTimestamp = DateTime.UtcNow,
            Version = "3.4.8",
            Licenses = await _db.LicenseKeys.ToListAsync(),
            Activations = await _db.LicenseActivations.ToListAsync(),
            Modules = await _db.Modules.ToListAsync(),
            Tiers = await _db.LicenseTiers.ToListAsync(),
            TierModules = await _db.TierModules.ToListAsync(),
            Certificates = await _db.Certificates.ToListAsync(),
            CertificateSequences = await _db.CertificateSequences.ToListAsync(),
            Transfers = await _db.LicenseTransfers.ToListAsync(),
            AdminUsers = await _db.AdminUsers.ToListAsync()
        };

        // Serialize
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(backupData, jsonOptions);
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

        // Compress
        using var ms = new MemoryStream();
        using (var gzip = new GZipStream(ms, CompressionLevel.Optimal, true))
        {
            await gzip.WriteAsync(jsonBytes);
        }

        var compressedBytes = ms.ToArray();

        // Encrypt if requested
        string? encryptionKeyHint = null;
        if (encrypt)
        {
            var (encryptedBytes, keyHint) = EncryptData(compressedBytes);
            compressedBytes = encryptedBytes;
            encryptionKeyHint = keyHint;
        }

        // Write to file
        await File.WriteAllBytesAsync(filePath, compressedBytes);

        // Calculate checksum
        var checksum = ComputeChecksum(compressedBytes);

        // Create backup record
        var record = new DatabaseBackupRecord
        {
            FileName = fileName,
            FilePath = filePath,
            FileSizeBytes = compressedBytes.Length,
            Checksum = checksum,
            EncryptionKey = encryptionKeyHint,
            IsEncrypted = encrypt,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            BackupType = backupType,
            LicenseCount = backupData.Licenses.Count,
            ActivationCount = backupData.Activations.Count,
            Notes = notes,
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow
        };

        _db.DatabaseBackups.Add(record);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync("BACKUP_CREATED", "Backup", record.Id.ToString(), 
            createdBy, null, null, 
            $"Backup created: {fileName} ({record.LicenseCount} licenses, {record.ActivationCount} activations)", 
            true);

        _logger.LogInformation("Created backup: {FileName} ({Size} bytes, {Licenses} licenses)", 
            fileName, record.FileSizeBytes, record.LicenseCount);

        return record;
    }

    public async Task<bool> RestoreBackupAsync(int backupId, string? decryptionKey = null)
    {
        var backup = await _db.DatabaseBackups.FindAsync(backupId);
        if (backup == null)
        {
            _logger.LogError("Backup not found: {BackupId}", backupId);
            return false;
        }

        if (!File.Exists(backup.FilePath))
        {
            _logger.LogError("Backup file not found: {FilePath}", backup.FilePath);
            return false;
        }

        try
        {
            // Create a pre-restore backup
            await CreateBackupAsync("System", "Pre-restore automatic backup", "PreUpdate");

            // Read and decrypt if needed
            var fileBytes = await File.ReadAllBytesAsync(backup.FilePath);

            if (backup.IsEncrypted)
            {
                if (string.IsNullOrEmpty(decryptionKey))
                {
                    _logger.LogError("Decryption key required for encrypted backup");
                    return false;
                }
                fileBytes = DecryptData(fileBytes, decryptionKey);
            }

            // Decompress
            using var ms = new MemoryStream(fileBytes);
            using var gzip = new GZipStream(ms, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip);
            var json = await reader.ReadToEndAsync();

            // Deserialize
            var backupData = JsonSerializer.Deserialize<BackupData>(json);
            if (backupData == null)
            {
                _logger.LogError("Failed to deserialize backup data");
                return false;
            }

            // Restore data (in transaction)
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Clear existing data (careful!)
                _db.LicenseActivations.RemoveRange(_db.LicenseActivations);
                _db.LicenseKeys.RemoveRange(_db.LicenseKeys);
                _db.TierModules.RemoveRange(_db.TierModules);
                _db.Modules.RemoveRange(_db.Modules);
                _db.LicenseTiers.RemoveRange(_db.LicenseTiers);
                await _db.SaveChangesAsync();

                // Restore data
                _db.LicenseKeys.AddRange(backupData.Licenses);
                _db.LicenseActivations.AddRange(backupData.Activations);
                _db.Modules.AddRange(backupData.Modules);
                _db.LicenseTiers.AddRange(backupData.Tiers);
                _db.TierModules.AddRange(backupData.TierModules);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogAsync("BACKUP_RESTORED", "Backup", backupId.ToString(),
                    null, null, null,
                    $"Restored from backup: {backup.FileName}", true);

                _logger.LogInformation("Restored backup: {FileName}", backup.FileName);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to restore backup");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backup restore");
            return false;
        }
    }

    public async Task<List<DatabaseBackupRecord>> GetBackupsAsync(int limit = 50)
    {
        return await _db.DatabaseBackups
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<DatabaseBackupRecord?> GetLatestBackupAsync()
    {
        return await _db.DatabaseBackups
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteBackupAsync(int backupId)
    {
        var backup = await _db.DatabaseBackups.FindAsync(backupId);
        if (backup == null) return false;

        try
        {
            if (File.Exists(backup.FilePath))
            {
                File.Delete(backup.FilePath);
            }

            _db.DatabaseBackups.Remove(backup);
            await _db.SaveChangesAsync();

            await _auditService.LogAsync("BACKUP_DELETED", "Backup", backupId.ToString(),
                null, null, null, $"Deleted backup: {backup.FileName}", true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup {BackupId}", backupId);
            return false;
        }
    }

    public async Task<bool> VerifyBackupAsync(int backupId)
    {
        var backup = await _db.DatabaseBackups.FindAsync(backupId);
        if (backup == null) return false;

        try
        {
            if (!File.Exists(backup.FilePath))
            {
                backup.IsVerified = false;
                await _db.SaveChangesAsync();
                return false;
            }

            var fileBytes = await File.ReadAllBytesAsync(backup.FilePath);
            var currentChecksum = ComputeChecksum(fileBytes);

            var isValid = currentChecksum == backup.Checksum;
            backup.IsVerified = isValid;
            backup.VerifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify backup {BackupId}", backupId);
            return false;
        }
    }

    public async Task<Stream> DownloadBackupAsync(int backupId)
    {
        var backup = await _db.DatabaseBackups.FindAsync(backupId);
        if (backup == null || !File.Exists(backup.FilePath))
        {
            throw new FileNotFoundException("Backup not found");
        }

        return File.OpenRead(backup.FilePath);
    }

    public async Task CleanupOldBackupsAsync(int keepCount = 10)
    {
        var backups = await _db.DatabaseBackups
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var toDelete = backups.Skip(keepCount).ToList();

        foreach (var backup in toDelete)
        {
            if (File.Exists(backup.FilePath))
            {
                try
                {
                    File.Delete(backup.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete backup file: {FilePath}", backup.FilePath);
                }
            }
            _db.DatabaseBackups.Remove(backup);
        }

        await _db.SaveChangesAsync();

        if (toDelete.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old backups", toDelete.Count);
        }
    }

    // ==================== Helper Methods ====================

    private static string ComputeChecksum(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash);
    }

    private static (byte[] EncryptedData, string KeyHint) EncryptData(byte[] data)
    {
        // Generate a random key
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encrypted.Length];
        aes.IV.CopyTo(result, 0);
        encrypted.CopyTo(result, aes.IV.Length);

        // Create key hint (first 4 chars of base64 key - NOT the full key!)
        var keyBase64 = Convert.ToBase64String(key);
        var keyHint = keyBase64[..4] + "..."; // Just a hint, full key should be stored securely by admin

        // In production, you'd want to store the full key securely
        // For now, we'll log it (REMOVE IN PRODUCTION!)
        Console.WriteLine($"[BACKUP] Encryption key (STORE SECURELY): {keyBase64}");

        return (result, keyHint);
    }

    private static byte[] DecryptData(byte[] encryptedData, string keyBase64)
    {
        var key = Convert.FromBase64String(keyBase64);
        
        using var aes = Aes.Create();
        aes.Key = key;

        // Extract IV from beginning of data
        var iv = new byte[16];
        Array.Copy(encryptedData, 0, iv, 0, 16);
        aes.IV = iv;

        // Decrypt the rest
        var encrypted = new byte[encryptedData.Length - 16];
        Array.Copy(encryptedData, 16, encrypted, 0, encrypted.Length);

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
    }
}

// Backup data model
public class BackupData
{
    public DateTime BackupTimestamp { get; set; }
    public string Version { get; set; } = "";
    public List<LicenseKeyRecord> Licenses { get; set; } = new();
    public List<LicenseActivationRecord> Activations { get; set; } = new();
    public List<ModuleRecord> Modules { get; set; } = new();
    public List<LicenseTierRecord> Tiers { get; set; } = new();
    public List<TierModuleRecord> TierModules { get; set; } = new();
    public List<CertificateRecord> Certificates { get; set; } = new();
    public List<CertificateSequenceRecord> CertificateSequences { get; set; } = new();
    public List<LicenseTransferRecord> Transfers { get; set; } = new();
    public List<AdminUserRecord> AdminUsers { get; set; } = new();
}
