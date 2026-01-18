// FathomOS.Core/Security/SecurityAuditLogger.cs
// SECURITY FIX: MISSING-006 - Security Audit Logger Implementation
// Provides tamper-resistant logging with HMAC-SHA256 signatures and log rotation

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FathomOS.Core.Interfaces;
using Microsoft.Win32;

namespace FathomOS.Core.Security;

/// <summary>
/// Thread-safe security audit logger with HMAC-SHA256 tamper detection and automatic log rotation.
/// Logs are stored in JSON format with each entry signed using a machine-specific key.
/// </summary>
public sealed class SecurityAuditLogger : ISecurityAuditLog
{
    // ==========================================
    // Constants
    // ==========================================

    private const string RegistryKeyPath = @"SOFTWARE\FathomOS";
    private const string RegistryValueName = "AuditKey";
    private const int HmacKeySizeBytes = 32; // 256 bits for HMAC-SHA256
    private const long MaxLogFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const string LogFileExtension = ".jsonl"; // JSON Lines format
    private const string LogFilePrefix = "audit_";

    // Additional entropy for DPAPI protection
    private static readonly byte[] AdditionalEntropy =
        Encoding.UTF8.GetBytes("FathomOS.SecurityAudit.HMAC.v1");

    // ==========================================
    // Fields
    // ==========================================

    private readonly string _logDirectory;
    private readonly object _lock = new();
    private readonly SemaphoreSlim _asyncLock = new(1, 1);
    private readonly string _machineId;
    private readonly int _sessionId;

    private StreamWriter? _writer;
    private string _currentLogDate = string.Empty;
    private string _currentLogFilePath = string.Empty;
    private long _currentFileSize;
    private long _sequenceNumber;
    private string? _previousHmac;
    private byte[]? _cachedHmacKey;
    private bool _disposed;

    // ==========================================
    // Constructor
    // ==========================================

    /// <summary>
    /// Initializes a new instance of the SecurityAuditLogger.
    /// </summary>
    /// <param name="logDirectory">Optional custom log directory. Defaults to AppData\FathomOS\AuditLogs.</param>
    public SecurityAuditLogger(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? GetDefaultLogDirectory();
        _machineId = GetMachineId();
        _sessionId = GetSessionId();

        EnsureLogDirectory();
        InitializeFromExistingLogs();

        // Log system startup
        LogSecurityEvent(AuditEventType.ApplicationStartup, "Security audit logger initialized", true);
    }

    // ==========================================
    // Properties
    // ==========================================

    /// <inheritdoc/>
    public string LogDirectory => _logDirectory;

    /// <inheritdoc/>
    public string CurrentLogFilePath
    {
        get
        {
            lock (_lock)
            {
                return _currentLogFilePath;
            }
        }
    }

    // ==========================================
    // Public Methods - Logging
    // ==========================================

    /// <inheritdoc/>
    public void LogLicenseValidation(string licenseKey, bool success, string reason, IDictionary<string, object>? additionalDetails = null)
    {
        var eventType = success ? AuditEventType.LicenseValidationSuccess : AuditEventType.LicenseValidationFailure;
        var maskedKey = MaskLicenseKey(licenseKey);
        var description = $"License validation {(success ? "succeeded" : "failed")}: {reason}";

        var details = additionalDetails != null
            ? new Dictionary<string, object>(additionalDetails)
            : new Dictionary<string, object>();
        details["licenseKeyMasked"] = maskedKey;

        WriteEntry(eventType, description, success, details);
    }

    /// <inheritdoc/>
    public void LogAuthentication(string username, bool success, string authMethod, string reason, IDictionary<string, object>? additionalDetails = null)
    {
        var eventType = success ? AuditEventType.LoginSuccess : AuditEventType.LoginFailure;
        var description = $"Authentication via {authMethod} {(success ? "succeeded" : "failed")} for user '{username}': {reason}";

        var details = additionalDetails != null
            ? new Dictionary<string, object>(additionalDetails)
            : new Dictionary<string, object>();
        details["authMethod"] = authMethod;

        WriteEntry(eventType, description, success, details, username);
    }

    /// <inheritdoc/>
    public void LogCertificateOperation(string operation, string certificateId, bool success, string reason, IDictionary<string, object>? additionalDetails = null)
    {
        var eventType = MapCertificateOperationToEventType(operation, success);
        var description = $"Certificate operation '{operation}' {(success ? "succeeded" : "failed")} for certificate '{certificateId}': {reason}";

        var details = additionalDetails != null
            ? new Dictionary<string, object>(additionalDetails)
            : new Dictionary<string, object>();
        details["certificateId"] = certificateId;
        details["operation"] = operation;

        WriteEntry(eventType, description, success, details);
    }

    /// <inheritdoc/>
    public void LogAccessDecision(string resource, string action, bool allowed, string reason, IDictionary<string, object>? additionalDetails = null)
    {
        var eventType = allowed ? AuditEventType.AccessGranted : AuditEventType.AccessDenied;
        var description = $"Access to '{resource}' for action '{action}' was {(allowed ? "granted" : "denied")}: {reason}";

        var details = additionalDetails != null
            ? new Dictionary<string, object>(additionalDetails)
            : new Dictionary<string, object>();
        details["resource"] = resource;
        details["action"] = action;

        WriteEntry(eventType, description, allowed, details);
    }

    /// <inheritdoc/>
    public void LogConfigurationChange(string settingName, string? oldValue, string? newValue, IDictionary<string, object>? additionalDetails = null)
    {
        var description = $"Configuration setting '{settingName}' changed from '{oldValue ?? "(null)"}' to '{newValue ?? "(null)"}'";

        var details = additionalDetails != null
            ? new Dictionary<string, object>(additionalDetails)
            : new Dictionary<string, object>();
        details["settingName"] = settingName;
        details["oldValue"] = oldValue ?? "(null)";
        details["newValue"] = newValue ?? "(null)";

        WriteEntry(AuditEventType.SettingChanged, description, true, details);
    }

    /// <inheritdoc/>
    public void LogSecurityEvent(AuditEventType eventType, string description, bool success, IDictionary<string, object>? additionalDetails = null)
    {
        var details = additionalDetails != null
            ? new Dictionary<string, object>(additionalDetails)
            : new Dictionary<string, object>();

        WriteEntry(eventType, description, success, details);
    }

    /// <inheritdoc/>
    public async Task LogSecurityEventAsync(AuditEventType eventType, string description, bool success, IDictionary<string, object>? additionalDetails = null)
    {
        var details = additionalDetails != null
            ? new Dictionary<string, object>(additionalDetails)
            : new Dictionary<string, object>();

        await WriteEntryAsync(eventType, description, success, details);
    }

    // ==========================================
    // Public Methods - Verification
    // ==========================================

    /// <inheritdoc/>
    public bool VerifyLogIntegrity(string logFilePath)
    {
        try
        {
            var result = VerifyLogFile(logFilePath);
            return result.IsValid;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public IDictionary<string, bool> VerifyAllLogs()
    {
        var results = new Dictionary<string, bool>();

        try
        {
            var logFiles = Directory.GetFiles(_logDirectory, $"{LogFilePrefix}*{LogFileExtension}");

            foreach (var logFile in logFiles)
            {
                results[logFile] = VerifyLogIntegrity(logFile);
            }
        }
        catch
        {
            // If we can't enumerate files, return empty results
        }

        return results;
    }

    /// <summary>
    /// Performs detailed verification of a log file and returns comprehensive results.
    /// </summary>
    /// <param name="logFilePath">Path to the log file to verify.</param>
    /// <returns>Detailed verification result.</returns>
    public LogVerificationResult VerifyLogFile(string logFilePath)
    {
        var result = new LogVerificationResult
        {
            FilePath = logFilePath,
            IsValid = true
        };

        try
        {
            if (!File.Exists(logFilePath))
            {
                result.IsValid = false;
                result.ErrorMessage = "Log file does not exist";
                return result;
            }

            var hmacKey = GetOrCreateHmacKey();
            string? previousHmac = null;
            long expectedSequence = 0;

            foreach (var line in File.ReadLines(logFilePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                result.TotalEntries++;

                var entry = AuditLogEntry.FromJson(line);
                if (entry == null)
                {
                    result.InvalidEntries++;
                    result.Failures.Add(($"Line {result.TotalEntries}", "Failed to parse JSON"));
                    result.IsValid = false;
                    continue;
                }

                // Verify sequence number
                if (entry.SequenceNumber != expectedSequence)
                {
                    result.InvalidEntries++;
                    result.Failures.Add((entry.Id, $"Sequence mismatch: expected {expectedSequence}, got {entry.SequenceNumber}"));
                    result.IsValid = false;
                }

                // Verify previous HMAC chain
                if (previousHmac != null && entry.PreviousHmac != previousHmac)
                {
                    result.InvalidEntries++;
                    result.Failures.Add((entry.Id, "Previous HMAC chain broken"));
                    result.IsValid = false;
                }

                // Verify HMAC signature
                var computedHmac = ComputeHmac(entry, hmacKey);
                if (entry.Hmac != computedHmac)
                {
                    result.InvalidEntries++;
                    result.Failures.Add((entry.Id, "HMAC signature mismatch - possible tampering"));
                    result.IsValid = false;
                }
                else
                {
                    result.ValidEntries++;
                }

                previousHmac = entry.Hmac;
                expectedSequence++;
            }

            // Log the verification event
            LogSecurityEvent(
                result.IsValid ? AuditEventType.AuditLogVerification : AuditEventType.AuditLogTamperingDetected,
                $"Log file verification {(result.IsValid ? "passed" : "FAILED")}: {logFilePath}",
                result.IsValid,
                new Dictionary<string, object>
                {
                    ["totalEntries"] = result.TotalEntries,
                    ["validEntries"] = result.ValidEntries,
                    ["invalidEntries"] = result.InvalidEntries
                });
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Verification error: {ex.Message}";
        }

        return result;
    }

    // ==========================================
    // Public Methods - Management
    // ==========================================

    /// <inheritdoc/>
    public void ForceRotate()
    {
        lock (_lock)
        {
            LogSecurityEvent(AuditEventType.AuditLogRotation, "Manual log rotation requested", true);
            RotateLogFile(force: true);
        }
    }

    /// <inheritdoc/>
    public void Flush()
    {
        lock (_lock)
        {
            _writer?.Flush();
        }
    }

    /// <inheritdoc/>
    public async Task FlushAsync()
    {
        await _asyncLock.WaitAsync();
        try
        {
            if (_writer != null)
            {
                await _writer.FlushAsync();
            }
        }
        finally
        {
            _asyncLock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;

            try
            {
                // Log shutdown before closing
                var entry = CreateEntry(AuditEventType.ApplicationShutdown, "Security audit logger shutting down", true, new Dictionary<string, object>());
                WriteEntryToStream(entry);
            }
            catch
            {
                // Ignore errors during shutdown
            }

            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
            _asyncLock.Dispose();

            // Clear cached key from memory
            if (_cachedHmacKey != null)
            {
                CryptographicOperations.ZeroMemory(_cachedHmacKey);
                _cachedHmacKey = null;
            }

            _disposed = true;
        }
    }

    // ==========================================
    // Private Methods - Entry Writing
    // ==========================================

    private void WriteEntry(AuditEventType eventType, string description, bool success, Dictionary<string, object> details, string? username = null)
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            var entry = CreateEntry(eventType, description, success, details, username);
            WriteEntryToStream(entry);
        }
    }

    private async Task WriteEntryAsync(AuditEventType eventType, string description, bool success, Dictionary<string, object> details, string? username = null)
    {
        if (_disposed)
            return;

        await _asyncLock.WaitAsync();
        try
        {
            var entry = CreateEntry(eventType, description, success, details, username);
            await WriteEntryToStreamAsync(entry);
        }
        finally
        {
            _asyncLock.Release();
        }
    }

    private AuditLogEntry CreateEntry(AuditEventType eventType, string description, bool success, Dictionary<string, object> details, string? username = null)
    {
        var entry = new AuditLogEntry
        {
            EventType = eventType,
            Description = description,
            Success = success,
            Username = username,
            MachineId = _machineId,
            SessionId = _sessionId,
            Source = "FathomOS.Core.Security",
            Details = details,
            SequenceNumber = _sequenceNumber,
            PreviousHmac = _previousHmac
        };

        // Compute and set HMAC
        var hmacKey = GetOrCreateHmacKey();
        entry.Hmac = ComputeHmac(entry, hmacKey);

        return entry;
    }

    private void WriteEntryToStream(AuditLogEntry entry)
    {
        try
        {
            EnsureWriterForCurrentDate();
            CheckAndRotateIfNeeded();

            var json = entry.ToJson();
            _writer?.WriteLine(json);
            _writer?.Flush();

            _currentFileSize += Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length;
            _sequenceNumber++;
            _previousHmac = entry.Hmac;
        }
        catch (IOException)
        {
            // Try to recover by recreating the writer
            CloseWriter();
            EnsureWriterForCurrentDate();

            var json = entry.ToJson();
            _writer?.WriteLine(json);
            _writer?.Flush();
        }
    }

    private async Task WriteEntryToStreamAsync(AuditLogEntry entry)
    {
        try
        {
            EnsureWriterForCurrentDate();
            CheckAndRotateIfNeeded();

            var json = entry.ToJson();
            if (_writer != null)
            {
                await _writer.WriteLineAsync(json);
                await _writer.FlushAsync();
            }

            _currentFileSize += Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length;
            _sequenceNumber++;
            _previousHmac = entry.Hmac;
        }
        catch (IOException)
        {
            CloseWriter();
            EnsureWriterForCurrentDate();

            var json = entry.ToJson();
            if (_writer != null)
            {
                await _writer.WriteLineAsync(json);
                await _writer.FlushAsync();
            }
        }
    }

    // ==========================================
    // Private Methods - File Management
    // ==========================================

    private void EnsureLogDirectory()
    {
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    private void EnsureWriterForCurrentDate()
    {
        var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (_currentLogDate != dateStr || _writer == null)
        {
            RotateLogFile(force: false, newDate: dateStr);
        }
    }

    private void CheckAndRotateIfNeeded()
    {
        if (_currentFileSize >= MaxLogFileSizeBytes)
        {
            RotateLogFile(force: true);
        }
    }

    private void RotateLogFile(bool force, string? newDate = null)
    {
        CloseWriter();

        var dateStr = newDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var timestamp = DateTime.UtcNow.ToString("HHmmss");

        string logFilePath;

        if (force || _currentLogDate == dateStr)
        {
            // Include timestamp for rotation within same day
            logFilePath = Path.Combine(_logDirectory, $"{LogFilePrefix}{dateStr}_{timestamp}{LogFileExtension}");
        }
        else
        {
            // New day, try simple filename first
            logFilePath = Path.Combine(_logDirectory, $"{LogFilePrefix}{dateStr}{LogFileExtension}");

            // If file exists (from previous session), add timestamp
            if (File.Exists(logFilePath))
            {
                logFilePath = Path.Combine(_logDirectory, $"{LogFilePrefix}{dateStr}_{timestamp}{LogFileExtension}");
            }
        }

        _writer = new StreamWriter(logFilePath, append: true, Encoding.UTF8)
        {
            AutoFlush = false
        };
        _currentLogDate = dateStr;
        _currentLogFilePath = logFilePath;
        _currentFileSize = new FileInfo(logFilePath).Exists ? new FileInfo(logFilePath).Length : 0;

        // Reset sequence for new file
        if (!File.Exists(logFilePath) || new FileInfo(logFilePath).Length == 0)
        {
            _sequenceNumber = 0;
            _previousHmac = null;
        }
    }

    private void CloseWriter()
    {
        if (_writer != null)
        {
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
        }
    }

    private void InitializeFromExistingLogs()
    {
        try
        {
            // Find the most recent log file for today
            var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var pattern = $"{LogFilePrefix}{dateStr}*{LogFileExtension}";
            var todaysLogs = Directory.GetFiles(_logDirectory, pattern)
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (todaysLogs != null && File.Exists(todaysLogs))
            {
                // Read last entry to continue sequence
                var lastLine = File.ReadLines(todaysLogs).LastOrDefault();
                if (!string.IsNullOrWhiteSpace(lastLine))
                {
                    var lastEntry = AuditLogEntry.FromJson(lastLine);
                    if (lastEntry != null)
                    {
                        _sequenceNumber = lastEntry.SequenceNumber + 1;
                        _previousHmac = lastEntry.Hmac;
                    }
                }
            }
        }
        catch
        {
            // Start fresh if we can't read existing logs
            _sequenceNumber = 0;
            _previousHmac = null;
        }
    }

    // ==========================================
    // Private Methods - HMAC Operations
    // ==========================================

    private byte[] GetOrCreateHmacKey()
    {
        if (_cachedHmacKey != null)
        {
            return _cachedHmacKey;
        }

        try
        {
            var protectedKey = GetProtectedKeyFromRegistry();

            if (protectedKey != null)
            {
                _cachedHmacKey = UnprotectKey(protectedKey);
                return _cachedHmacKey;
            }

            // Generate new key
            _cachedHmacKey = GenerateNewKey();
            var newProtectedKey = ProtectKey(_cachedHmacKey);
            StoreProtectedKeyInRegistry(newProtectedKey);

            return _cachedHmacKey;
        }
        catch (Exception)
        {
            // Fallback to machine-derived key if DPAPI fails
            _cachedHmacKey = DeriveMachineKey();
            return _cachedHmacKey;
        }
    }

    private static string ComputeHmac(AuditLogEntry entry, byte[] key)
    {
        var jsonForSigning = entry.ToJson(includeHmac: false);
        var dataBytes = Encoding.UTF8.GetBytes(jsonForSigning);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(dataBytes);
        return Convert.ToBase64String(hash);
    }

    private static byte[] GenerateNewKey()
    {
        var key = new byte[HmacKeySizeBytes];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    private static byte[] ProtectKey(byte[] plainKey)
    {
        return ProtectedData.Protect(
            plainKey,
            AdditionalEntropy,
            DataProtectionScope.LocalMachine);
    }

    private static byte[] UnprotectKey(byte[] protectedKey)
    {
        return ProtectedData.Unprotect(
            protectedKey,
            AdditionalEntropy,
            DataProtectionScope.LocalMachine);
    }

    private static byte[]? GetProtectedKeyFromRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false);
        return key?.GetValue(RegistryValueName) as byte[];
    }

    private static void StoreProtectedKeyInRegistry(byte[] protectedKey)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
        key.SetValue(RegistryValueName, protectedKey, RegistryValueKind.Binary);
    }

    private byte[] DeriveMachineKey()
    {
        // Fallback: derive key from machine ID
        var machineData = Encoding.UTF8.GetBytes(_machineId + "FathomOS.Audit.Fallback.Key.v1");
        return SHA256.HashData(machineData);
    }

    // ==========================================
    // Private Methods - Utility
    // ==========================================

    private static string GetDefaultLogDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "FathomOS", "AuditLogs");
    }

    private static string GetMachineId()
    {
        try
        {
            // Try to get machine GUID from registry
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var machineGuid = key?.GetValue("MachineGuid") as string;

            if (!string.IsNullOrEmpty(machineGuid))
            {
                return machineGuid;
            }
        }
        catch
        {
            // Ignore registry access errors
        }

        // Fallback to machine name hash
        var data = Encoding.UTF8.GetBytes(Environment.MachineName + Environment.UserDomainName);
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash[..8]).ToLowerInvariant();
    }

    private static int GetSessionId()
    {
        try
        {
            return Process.GetCurrentProcess().SessionId;
        }
        catch
        {
            return -1;
        }
    }

    private static string MaskLicenseKey(string licenseKey)
    {
        if (string.IsNullOrEmpty(licenseKey) || licenseKey.Length <= 8)
        {
            return "****";
        }

        // Show first 4 and last 4 characters
        return $"{licenseKey[..4]}...{licenseKey[^4..]}";
    }

    private static AuditEventType MapCertificateOperationToEventType(string operation, bool success)
    {
        return operation.ToLowerInvariant() switch
        {
            "create" => AuditEventType.CertificateCreated,
            "update" => AuditEventType.CertificateUpdated,
            "delete" => AuditEventType.CertificateDeleted,
            "approve" => success ? AuditEventType.CertificateApproved : AuditEventType.CertificateRejected,
            "reject" => AuditEventType.CertificateRejected,
            "revoke" => AuditEventType.CertificateRevoked,
            "export" => AuditEventType.CertificateExported,
            "sign" => AuditEventType.CertificateSigned,
            "verify" => AuditEventType.CertificateSignatureVerified,
            "sync" => AuditEventType.CertificateSynced,
            "access" => AuditEventType.CertificateAccessed,
            _ => AuditEventType.GenericSecurityEvent
        };
    }
}
