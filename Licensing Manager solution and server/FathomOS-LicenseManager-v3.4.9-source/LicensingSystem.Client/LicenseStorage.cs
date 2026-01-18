// LicensingSystem.Client/LicenseStorage.cs
// Secure storage for license data using Windows DPAPI
// v3.4.9 - Comprehensive error handling and validation

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LicensingSystem.Shared;
using Microsoft.Win32;

namespace LicensingSystem.Client;

/// <summary>
/// Securely stores license data using Windows DPAPI encryption
/// Stores in multiple locations for redundancy and tamper detection
/// </summary>
public class LicenseStorage
{
    private readonly string _appDataPath;
    private readonly string _registryPath;

    // YOUR ENTROPY BYTES - Generated December 23, 2024
    // Used for DPAPI encryption - unique to your application
    private static readonly byte[] Entropy =
    {
        0x19, 0x8D, 0xB1, 0xBA, 0x11, 0x21, 0x2A, 0xFE,
        0xDF, 0xDE, 0x61, 0xA5, 0x72, 0x18, 0x6C, 0xCD
    };

    // YOUR CHECKSUM SALT - Generated December 23, 2024
    // Used for tamper detection - unique to your application
    private const string ChecksumSalt = "WmyD3WI+tzlhTDOl/q+68dPCc11O56rCdSoP3WtX6i8=";

    public LicenseStorage(string productName = "FathomOS")
    {
        // Get local app data path with fallbacks for edge cases
        var localAppData = GetLocalAppDataPath();

        _appDataPath = Path.Combine(localAppData, productName, "License");
        _registryPath = $@"SOFTWARE\{productName}\License";

        try
        {
            Directory.CreateDirectory(_appDataPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to create directory {_appDataPath}: {ex.Message}");
            // Try fallback to app directory
            _appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LicenseData");
            Directory.CreateDirectory(_appDataPath);
        }
    }

    /// <summary>
    /// Gets the local app data path with robust fallback logic
    /// </summary>
    private static string GetLocalAppDataPath()
    {
        // Try LocalApplicationData first
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localAppData) && IsDirectoryWritable(localAppData))
            return localAppData;

        // Fallback to UserProfile
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            var fallbackPath = Path.Combine(userProfile, ".FathomOS");
            if (TryCreateDirectory(fallbackPath))
                return fallbackPath;
        }

        // Ultimate fallback: use application directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (string.IsNullOrEmpty(baseDir))
            baseDir = Directory.GetCurrentDirectory();

        return Path.Combine(baseDir, "AppData");
    }

    private static bool IsDirectoryWritable(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $".write_test_{Guid.NewGuid():N}");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryCreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves the license to multiple secure locations
    /// </summary>
    public void SaveLicense(SignedLicense license)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));

        var json = JsonSerializer.Serialize(license);
        var encrypted = Protect(json);
        var base64 = Convert.ToBase64String(encrypted);

        // Store in file (primary)
        var filePath = Path.Combine(_appDataPath, "license.dat");
        try
        {
            File.WriteAllText(filePath, base64);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to save license file: {ex.Message}");
            throw new InvalidOperationException("Failed to save license to file storage", ex);
        }

        // Store in registry (backup)
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(_registryPath);
            key?.SetValue("Data", base64);
            key?.SetValue("Checksum", ComputeChecksum(base64));
        }
        catch (Exception ex)
        {
            // Registry access might fail, but file should work - log but don't throw
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Registry backup failed (non-critical): {ex.Message}");
        }

        // Update metadata
        UpdateLastOnlineCheck();
    }

    /// <summary>
    /// Loads the license from storage, verifying integrity
    /// </summary>
    public SignedLicense? LoadLicense()
    {
        // Try file first
        var filePath = Path.Combine(_appDataPath, "license.dat");
        if (File.Exists(filePath))
        {
            try
            {
                var base64 = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(base64))
                {
                    var encrypted = Convert.FromBase64String(base64);
                    var json = UnprotectSafe(encrypted);

                    if (!string.IsNullOrEmpty(json))
                    {
                        var license = JsonSerializer.Deserialize<SignedLicense>(json);
                        if (ValidateLicense(license))
                            return license;
                    }
                }
            }
            catch (FormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Invalid base64 in license file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Invalid JSON in license file: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Error loading license from file: {ex.Message}");
            }
        }

        // Try registry backup
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(_registryPath);
            var base64 = key?.GetValue("Data")?.ToString();
            var storedChecksum = key?.GetValue("Checksum")?.ToString();

            if (!string.IsNullOrEmpty(base64) && !string.IsNullOrEmpty(storedChecksum))
            {
                // Verify checksum
                if (ComputeChecksum(base64) == storedChecksum)
                {
                    var encrypted = Convert.FromBase64String(base64);
                    var json = UnprotectSafe(encrypted);

                    if (!string.IsNullOrEmpty(json))
                    {
                        var license = JsonSerializer.Deserialize<SignedLicense>(json);
                        if (ValidateLicense(license))
                        {
                            // Restore file from registry
                            try
                            {
                                File.WriteAllText(filePath, base64);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to restore file from registry: {ex.Message}");
                            }
                            return license;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LicenseStorage: Registry checksum mismatch - possible tampering");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Registry access failed: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Validates that a deserialized license has required fields
    /// </summary>
    private static bool ValidateLicense(SignedLicense? license)
    {
        if (license == null) return false;
        if (license.License == null) return false;
        if (string.IsNullOrEmpty(license.Signature)) return false;
        if (string.IsNullOrEmpty(license.License.LicenseId)) return false;
        return true;
    }

    /// <summary>
    /// Clears all stored license data
    /// </summary>
    public void ClearLicense()
    {
        var filePath = Path.Combine(_appDataPath, "license.dat");
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to delete license file: {ex.Message}");
        }

        try
        {
            Registry.CurrentUser.DeleteSubKey(_registryPath, false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to delete registry key: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the last online validation timestamp
    /// </summary>
    public DateTime GetLastOnlineCheck()
    {
        return LoadMetadata()?.LastOnlineCheck ?? DateTime.MinValue;
    }

    /// <summary>
    /// Updates the last online check timestamp
    /// </summary>
    public void UpdateLastOnlineCheck()
    {
        try
        {
            // Load existing metadata to preserve first activation data
            var existingMeta = LoadMetadata();

            var meta = new LicenseMetadata
            {
                LastOnlineCheck = DateTime.UtcNow,
                LastRunTime = DateTime.UtcNow,
                RunCount = (existingMeta?.RunCount ?? 0) + 1,
                // Preserve first activation data
                FirstActivatedAt = existingMeta?.FirstActivatedAt ?? DateTime.MinValue,
                OriginalLicenseId = existingMeta?.OriginalLicenseId ?? string.Empty,
                // Preserve other fields
                OfflineSyncPending = existingMeta?.OfflineSyncPending ?? false,
                PendingLicenseId = existingMeta?.PendingLicenseId,
                LocalCertificateSequence = existingMeta?.LocalCertificateSequence ?? 0
            };

            SaveMetadata(meta);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to update last online check: {ex.Message}");
        }
    }

    /// <summary>
    /// Records the first activation of a license (anti-reset attack)
    /// </summary>
    public void RecordFirstActivation(string licenseId)
    {
        if (string.IsNullOrEmpty(licenseId))
            return;

        try
        {
            var existingMeta = LoadMetadata();

            // Only record if not already recorded
            if (existingMeta?.FirstActivatedAt == DateTime.MinValue || existingMeta?.FirstActivatedAt == null)
            {
                var meta = new LicenseMetadata
                {
                    LastOnlineCheck = DateTime.UtcNow,
                    LastRunTime = DateTime.UtcNow,
                    RunCount = (existingMeta?.RunCount ?? 0) + 1,
                    FirstActivatedAt = DateTime.UtcNow,
                    OriginalLicenseId = licenseId,
                    LocalCertificateSequence = existingMeta?.LocalCertificateSequence ?? 0
                };

                SaveMetadata(meta);
                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Recorded first activation for {licenseId}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to record first activation: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a license appears to be from before first activation (suspicious)
    /// </summary>
    public bool IsLicenseSuspicious(DateTime licenseIssuedAt)
    {
        var meta = LoadMetadata();
        if (meta == null || meta.FirstActivatedAt == DateTime.MinValue)
            return false;

        // If license was issued more than 1 day before first activation, suspicious
        // This detects: user deletes meta.dat, imports OLD license file
        return licenseIssuedAt < meta.FirstActivatedAt.AddDays(-1);
    }

    /// <summary>
    /// Loads metadata from storage
    /// </summary>
    private LicenseMetadata? LoadMetadata()
    {
        try
        {
            var metaPath = Path.Combine(_appDataPath, "meta.dat");
            if (File.Exists(metaPath))
            {
                var content = File.ReadAllText(metaPath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var encrypted = Convert.FromBase64String(content);
                    var json = UnprotectSafe(encrypted);
                    if (!string.IsNullOrEmpty(json))
                    {
                        return JsonSerializer.Deserialize<LicenseMetadata>(json);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to load metadata: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Saves metadata to storage
    /// </summary>
    private void SaveMetadata(LicenseMetadata meta)
    {
        try
        {
            var json = JsonSerializer.Serialize(meta);
            var encrypted = Protect(json);

            var metaPath = Path.Combine(_appDataPath, "meta.dat");
            File.WriteAllText(metaPath, Convert.ToBase64String(encrypted));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to save metadata: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the application run count (for anti-clock-tampering)
    /// </summary>
    public int GetRunCount()
    {
        return LoadMetadata()?.RunCount ?? 0;
    }

    /// <summary>
    /// Gets the last recorded run time (for clock tampering detection)
    /// </summary>
    public DateTime GetLastRunTime()
    {
        return LoadMetadata()?.LastRunTime ?? DateTime.MinValue;
    }

    /// <summary>
    /// Checks if there's an offline license pending sync
    /// </summary>
    public bool IsOfflineSyncPending()
    {
        return LoadMetadata()?.OfflineSyncPending ?? false;
    }

    /// <summary>
    /// Marks an offline license as pending sync
    /// </summary>
    public void SetOfflineSyncPending(bool pending, string? licenseId = null)
    {
        try
        {
            var meta = LoadMetadata() ?? new LicenseMetadata();
            meta.OfflineSyncPending = pending;
            meta.PendingLicenseId = pending ? licenseId : null;
            SaveMetadata(meta);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to set offline sync pending: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the license ID that's pending sync
    /// </summary>
    public string? GetPendingSyncLicenseId()
    {
        return LoadMetadata()?.PendingLicenseId;
    }

    /// <summary>
    /// Detects if the system clock has been rolled back
    /// </summary>
    public bool DetectClockTampering()
    {
        var lastRun = GetLastRunTime();
        if (lastRun == DateTime.MinValue)
            return false;

        // If current time is significantly before last run time, clock was rolled back
        var timeDiff = DateTime.UtcNow - lastRun;

        // Clock rolled back more than 1 hour is suspicious
        if (timeDiff.TotalHours < -1)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Clock tampering detected - current time is {Math.Abs(timeDiff.TotalHours):F1} hours before last run");
            return true;
        }

        // Also check against last online check time
        var lastOnline = GetLastOnlineCheck();
        if (lastOnline != DateTime.MinValue)
        {
            var onlineDiff = DateTime.UtcNow - lastOnline;

            // If current time is before last online check, clock was rolled back
            if (onlineDiff.TotalHours < -1)
            {
                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Clock tampering detected - current time is {Math.Abs(onlineDiff.TotalHours):F1} hours before last online check");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the system time appears to have been significantly modified
    /// compared to the last known server time
    /// </summary>
    public bool DetectClockTamperingWithServerTime(DateTime serverTime)
    {
        var localTime = DateTime.UtcNow;
        var diff = Math.Abs((localTime - serverTime).TotalMinutes);

        // If local time differs from server time by more than 30 minutes, suspicious
        // (Allows for small clock drift and timezone issues)
        if (diff > 30)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Clock drift detected - {diff:F0} minutes difference from server");
            // Store the server time for future reference
            SaveServerTime(serverTime);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Saves the server time for clock comparison
    /// </summary>
    private void SaveServerTime(DateTime serverTime)
    {
        try
        {
            var filePath = Path.Combine(_appDataPath, "timeref.dat");
            var data = serverTime.ToBinary().ToString();
            var encrypted = Protect(data);
            File.WriteAllText(filePath, Convert.ToBase64String(encrypted));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to save server time: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the last known server time
    /// </summary>
    public DateTime GetLastServerTime()
    {
        try
        {
            var filePath = Path.Combine(_appDataPath, "timeref.dat");
            if (!File.Exists(filePath))
                return DateTime.MinValue;

            var base64 = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(base64))
                return DateTime.MinValue;

            var encrypted = Convert.FromBase64String(base64);
            var data = UnprotectSafe(encrypted);

            if (!string.IsNullOrEmpty(data) && long.TryParse(data, out var binary))
            {
                return DateTime.FromBinary(binary);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get last server time: {ex.Message}");
        }
        return DateTime.MinValue;
    }

    /// <summary>
    /// Encrypts data using Windows DPAPI (only decryptable by same user on same machine)
    /// </summary>
    private static byte[] Protect(string data)
    {
        if (string.IsNullOrEmpty(data))
            throw new ArgumentNullException(nameof(data));

        var bytes = Encoding.UTF8.GetBytes(data);
        // CurrentUser = only THIS user can decrypt (more secure than LocalMachine)
        return ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
    }

    /// <summary>
    /// Decrypts data using Windows DPAPI - returns empty string on failure (safe version)
    /// </summary>
    private static string UnprotectSafe(byte[] encryptedData)
    {
        if (encryptedData == null || encryptedData.Length == 0)
            return string.Empty;

        try
        {
            // CurrentUser = only THIS user can decrypt
            var bytes = ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: DPAPI decryption failed (data may be from different user/machine): {ex.Message}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Unexpected decryption error: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Computes a checksum for tamper detection
    /// </summary>
    private static string ComputeChecksum(string data)
    {
        if (string.IsNullOrEmpty(data))
            return string.Empty;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data + ChecksumSalt));
        var hexString = Convert.ToHexString(hash);
        return hexString.Length >= 16 ? hexString[..16] : hexString;
    }

    #region Local Revocation List

    /// <summary>
    /// Adds a license to the local revocation list.
    /// Once revoked, the license cannot be re-imported even from the original .lic file.
    /// </summary>
    public void AddToRevocationList(string licenseId, string? reason = null)
    {
        if (string.IsNullOrEmpty(licenseId)) return;

        try
        {
            var list = LoadRevocationList();

            // Check if already revoked
            if (list.RevokedLicenses.Any(r => r.LicenseId == licenseId))
                return;

            list.RevokedLicenses.Add(new RevokedLicenseEntry
            {
                LicenseId = licenseId,
                RevokedAt = DateTime.UtcNow,
                Reason = reason ?? "Revoked by server"
            });

            SaveRevocationList(list);
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Added {licenseId} to revocation list: {reason}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to add to revocation list: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a license from the local revocation list.
    /// Called when server confirms license is valid after re-activation.
    /// </summary>
    public void RemoveFromRevocationList(string licenseId)
    {
        if (string.IsNullOrEmpty(licenseId)) return;

        try
        {
            var list = LoadRevocationList();
            var entry = list.RevokedLicenses.FirstOrDefault(r => r.LicenseId == licenseId);

            if (entry != null)
            {
                list.RevokedLicenses.Remove(entry);
                SaveRevocationList(list);
                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Removed {licenseId} from local revocation list");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to remove from revocation list: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the entire local revocation list.
    /// Use with caution - this allows previously revoked licenses to be re-used.
    /// </summary>
    public void ClearRevocationList()
    {
        try
        {
            var filePath = Path.Combine(_appDataPath, "revoked.dat");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                System.Diagnostics.Debug.WriteLine("LicenseStorage: Cleared local revocation list");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to clear revocation list: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a license is in the local revocation list.
    /// Returns true if the license has been revoked and should NOT be accepted.
    /// </summary>
    public bool IsLicenseRevoked(string licenseId)
    {
        if (string.IsNullOrEmpty(licenseId)) return false;

        try
        {
            var list = LoadRevocationList();
            return list.RevokedLicenses.Any(r => r.LicenseId == licenseId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to check revocation status: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the revocation reason for a license, if revoked.
    /// </summary>
    public string? GetRevocationReason(string licenseId)
    {
        if (string.IsNullOrEmpty(licenseId)) return null;

        try
        {
            var list = LoadRevocationList();
            return list.RevokedLicenses.FirstOrDefault(r => r.LicenseId == licenseId)?.Reason;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get revocation reason: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all revoked license IDs (for diagnostics).
    /// </summary>
    public List<string> GetRevokedLicenseIds()
    {
        try
        {
            var list = LoadRevocationList();
            return list.RevokedLicenses.Select(r => r.LicenseId).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get revoked license IDs: {ex.Message}");
            return new List<string>();
        }
    }

    private LocalRevocationList LoadRevocationList()
    {
        try
        {
            var filePath = Path.Combine(_appDataPath, "revoked.dat");
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var encrypted = Convert.FromBase64String(content);
                    var json = UnprotectSafe(encrypted);
                    if (!string.IsNullOrEmpty(json))
                    {
                        return JsonSerializer.Deserialize<LocalRevocationList>(json) ?? new LocalRevocationList();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to load revocation list: {ex.Message}");
        }
        return new LocalRevocationList();
    }

    private void SaveRevocationList(LocalRevocationList list)
    {
        try
        {
            var json = JsonSerializer.Serialize(list);
            var encrypted = Protect(json);
            var filePath = Path.Combine(_appDataPath, "revoked.dat");
            File.WriteAllText(filePath, Convert.ToBase64String(encrypted));

            // Also store in registry as backup
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(_registryPath);
                key?.SetValue("RevokedLicenses", Convert.ToBase64String(encrypted));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Registry backup for revocation list failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to save revocation list: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Local Certificate Storage

    /// <summary>
    /// Saves a certificate to local storage (for offline use)
    /// </summary>
    public void SaveCertificateLocally(ProcessingCertificate certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));

        try
        {
            var store = LoadCertificateStore();

            // Check if already exists
            var existing = store.Certificates.FirstOrDefault(c => c.CertificateId == certificate.CertificateId);
            if (existing != null)
            {
                existing.Certificate = certificate;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                store.Certificates.Add(new LocalCertificateEntry
                {
                    CertificateId = certificate.CertificateId,
                    Certificate = certificate,
                    CreatedAt = DateTime.UtcNow,
                    IsSyncedToServer = false,
                    IsVerifiedByServer = false
                });
            }

            SaveCertificateStore(store);
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Saved certificate locally: {certificate.CertificateId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to save certificate locally: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets all certificates that haven't been synced to the server yet
    /// </summary>
    public List<ProcessingCertificate> GetUnsyncedCertificates()
    {
        try
        {
            var store = LoadCertificateStore();
            return store.Certificates
                .Where(c => !c.IsSyncedToServer)
                .Select(c => c.Certificate)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get unsynced certificates: {ex.Message}");
            return new List<ProcessingCertificate>();
        }
    }

    /// <summary>
    /// Gets all certificates that are synced but not yet verified by server
    /// </summary>
    public List<ProcessingCertificate> GetUnverifiedCertificates()
    {
        try
        {
            var store = LoadCertificateStore();
            return store.Certificates
                .Where(c => c.IsSyncedToServer && !c.IsVerifiedByServer)
                .Select(c => c.Certificate)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get unverified certificates: {ex.Message}");
            return new List<ProcessingCertificate>();
        }
    }

    /// <summary>
    /// Marks certificates as synced to server
    /// </summary>
    public void MarkCertificatesAsSynced(IEnumerable<string> certificateIds)
    {
        if (certificateIds == null) return;

        try
        {
            var store = LoadCertificateStore();
            var idsSet = certificateIds.ToHashSet();

            foreach (var entry in store.Certificates.Where(c => idsSet.Contains(c.CertificateId)))
            {
                entry.IsSyncedToServer = true;
                entry.SyncedAt = DateTime.UtcNow;
            }

            store.LastSyncAttempt = DateTime.UtcNow;
            SaveCertificateStore(store);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to mark certificates as synced: {ex.Message}");
        }
    }

    /// <summary>
    /// Marks certificates as verified by server
    /// </summary>
    public void MarkCertificatesAsVerified(IEnumerable<string> certificateIds)
    {
        if (certificateIds == null) return;

        try
        {
            var store = LoadCertificateStore();
            var idsSet = certificateIds.ToHashSet();

            foreach (var entry in store.Certificates.Where(c => idsSet.Contains(c.CertificateId)))
            {
                entry.IsVerifiedByServer = true;
                entry.VerifiedAt = DateTime.UtcNow;
            }

            SaveCertificateStore(store);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to mark certificates as verified: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all locally stored certificates
    /// </summary>
    public List<LocalCertificateEntry> GetAllLocalCertificates()
    {
        try
        {
            var store = LoadCertificateStore();
            return store.Certificates.OrderByDescending(c => c.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get all local certificates: {ex.Message}");
            return new List<LocalCertificateEntry>();
        }
    }

    /// <summary>
    /// Gets a specific certificate by ID
    /// </summary>
    public LocalCertificateEntry? GetCertificate(string certificateId)
    {
        if (string.IsNullOrEmpty(certificateId)) return null;

        try
        {
            var store = LoadCertificateStore();
            return store.Certificates.FirstOrDefault(c => c.CertificateId == certificateId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get certificate {certificateId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the next local certificate sequence number and increments it
    /// </summary>
    public int GetNextLocalCertificateSequence()
    {
        try
        {
            var meta = LoadMetadata() ?? new LicenseMetadata();
            meta.LocalCertificateSequence++;
            SaveMetadata(meta);
            return meta.LocalCertificateSequence;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get next certificate sequence: {ex.Message}");
            return new Random().Next(1000, 9999); // Fallback to random
        }
    }

    /// <summary>
    /// Gets certificate statistics
    /// </summary>
    public (int Total, int Unsynced, int Unverified, int Verified) GetCertificateStats()
    {
        try
        {
            var store = LoadCertificateStore();
            var total = store.Certificates.Count;
            var unsynced = store.Certificates.Count(c => !c.IsSyncedToServer);
            var unverified = store.Certificates.Count(c => c.IsSyncedToServer && !c.IsVerifiedByServer);
            var verified = store.Certificates.Count(c => c.IsVerifiedByServer);
            return (total, unsynced, unverified, verified);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to get certificate stats: {ex.Message}");
            return (0, 0, 0, 0);
        }
    }

    /// <summary>
    /// Deletes old verified certificates (cleanup)
    /// </summary>
    public int CleanupOldCertificates(int keepDays = 365)
    {
        try
        {
            var store = LoadCertificateStore();
            var cutoff = DateTime.UtcNow.AddDays(-keepDays);

            var toRemove = store.Certificates
                .Where(c => c.IsVerifiedByServer && c.CreatedAt < cutoff)
                .ToList();

            foreach (var cert in toRemove)
            {
                store.Certificates.Remove(cert);
            }

            if (toRemove.Count > 0)
            {
                SaveCertificateStore(store);
                System.Diagnostics.Debug.WriteLine($"LicenseStorage: Cleaned up {toRemove.Count} old certificates");
            }

            return toRemove.Count;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to cleanup old certificates: {ex.Message}");
            return 0;
        }
    }

    private LocalCertificateStore LoadCertificateStore()
    {
        try
        {
            var filePath = Path.Combine(_appDataPath, "certificates.dat");
            if (File.Exists(filePath))
            {
                var content = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var encrypted = Convert.FromBase64String(content);
                    var json = UnprotectSafe(encrypted);
                    if (!string.IsNullOrEmpty(json))
                    {
                        return JsonSerializer.Deserialize<LocalCertificateStore>(json) ?? new LocalCertificateStore();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to load certificate store: {ex.Message}");
        }
        return new LocalCertificateStore();
    }

    private void SaveCertificateStore(LocalCertificateStore store)
    {
        try
        {
            var json = JsonSerializer.Serialize(store);
            var encrypted = Protect(json);
            var filePath = Path.Combine(_appDataPath, "certificates.dat");
            File.WriteAllText(filePath, Convert.ToBase64String(encrypted));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LicenseStorage: Failed to save certificate store: {ex.Message}");
            throw;
        }
    }

    #endregion
}

/// <summary>
/// Local list of revoked licenses (persisted and encrypted)
/// </summary>
internal class LocalRevocationList
{
    public List<RevokedLicenseEntry> RevokedLicenses { get; set; } = new();
}

/// <summary>
/// Entry for a revoked license
/// </summary>
internal class RevokedLicenseEntry
{
    public string LicenseId { get; set; } = string.Empty;
    public DateTime RevokedAt { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Metadata stored alongside the license
/// </summary>
internal class LicenseMetadata
{
    public DateTime LastOnlineCheck { get; set; }
    public DateTime LastRunTime { get; set; }
    public int RunCount { get; set; }

    // Anti-reset attack fields
    public DateTime FirstActivatedAt { get; set; }
    public string OriginalLicenseId { get; set; } = string.Empty;

    // Offline license sync tracking
    public bool OfflineSyncPending { get; set; }
    public string? PendingLicenseId { get; set; }

    // Certificate sequence tracking
    public int LocalCertificateSequence { get; set; }
}

/// <summary>
/// Local certificate storage entry
/// </summary>
public class LocalCertificateEntry
{
    public string CertificateId { get; set; } = string.Empty;
    public ProcessingCertificate Certificate { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsSyncedToServer { get; set; }
    public bool IsVerifiedByServer { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
}

/// <summary>
/// Local certificate storage list
/// </summary>
internal class LocalCertificateStore
{
    public List<LocalCertificateEntry> Certificates { get; set; } = new();
    public DateTime LastSyncAttempt { get; set; }
}
