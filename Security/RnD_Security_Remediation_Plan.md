# FathomOS R&D Security Remediation Plan

**Generated:** January 17, 2026
**Version:** 1.0
**Prepared by:** R&D-AGENT
**Based on:** Security Audit Reports + RnD_Implementation_Delegation.md

---

## Executive Summary

This document provides a comprehensive status of all security findings from the SECURITY-AGENT audit reports and outlines the remediation plan for remaining issues. Phase 1 and Phase 2 critical/high priority fixes have been completed. This plan addresses the remaining PENDING issues for Phases 3 and 4.

---

## PART 1: Status Report

### Fixed Issues (Verified)

| Issue ID | Description | Severity | Fixed By | Verification Status |
|----------|-------------|----------|----------|---------------------|
| BUG-001 | License delegate defaults to true | CRITICAL | CORE-AGENT | VERIFIED - Defaults now throw exception or return false/null |
| BUG-002 | Hardware fingerprint fallback to machine name | CRITICAL | LICENSING-AGENT | VERIFIED - Now throws exception if no hardware IDs |
| BUG-003 | Time reference anti-tamper incomplete | HIGH | LICENSING-AGENT | VERIFIED - Multi-location storage + integrity checksums |
| BUG-004 | NaviPac connection state race condition | HIGH | MODULE-SurveyLogbook-AGENT | VERIFIED - Uses Interlocked operations |
| BUG-005 | Certificate sync retry count not reset | HIGH | CORE-AGENT | VERIFIED - SyncAttempts = 0 added to SQL |
| VULN-001 | Hardcoded shared secret (FathomOSTimeSync2024) | HIGH | MODULE-NetworkTimeSync-AGENT | VERIFIED - Default now empty, requires configuration |
| VULN-003 | MD5 used for file hashing | MEDIUM | CORE-AGENT | VERIFIED - Upgraded to SHA-256 |
| VULN-007 | License bypass potential (delegate defaults) | MEDIUM | CORE-AGENT | VERIFIED - Same fix as BUG-001 |

### Pending Issues (Not Yet Implemented)

#### HIGH Priority - Phase 3

| Issue ID | Description | Severity | Report Source | Recommended Agent |
|----------|-------------|----------|---------------|-------------------|
| VULN-002 | Weak anti-tamper protection | HIGH | VulnerabilityReport | SECURITY-AGENT / External |
| VULN-004 | Unencrypted database storage | MEDIUM | VulnerabilityReport | DATABASE-AGENT |
| VULN-005 | Network data transmission without encryption | MEDIUM | VulnerabilityReport | MODULE-SurveyLogbook-AGENT |
| VULN-006 | Insufficient input validation | MEDIUM | VulnerabilityReport | CORE-AGENT |
| MISSING-001 | Database encryption (SQLCipher) | HIGH | BugsAndMissing | DATABASE-AGENT |
| MISSING-002 | Certificate pinning | HIGH | BugsAndMissing | LICENSING-AGENT |
| MISSING-003 | Code obfuscation | HIGH | BugsAndMissing | BUILD-AGENT / External |
| MISSING-004 | Assembly integrity verification | MEDIUM | BugsAndMissing | CORE-AGENT |
| MISSING-005 | Secure configuration storage | HIGH | BugsAndMissing | MODULE-NetworkTimeSync-AGENT |

#### MEDIUM Priority - Phase 4

| Issue ID | Description | Severity | Report Source | Recommended Agent |
|----------|-------------|----------|---------------|-------------------|
| VULN-008 | Information leakage in error messages | LOW | VulnerabilityReport | CORE-AGENT |
| VULN-009 | Missing certificate pinning | LOW | VulnerabilityReport | LICENSING-AGENT |
| VULN-010 | Insecure default configuration | LOW | VulnerabilityReport | MODULE-NetworkTimeSync-AGENT |
| VULN-011 | Timing attack vulnerability | LOW | VulnerabilityReport | LICENSING-AGENT |
| MISSING-006 | Audit logging | MEDIUM | BugsAndMissing | CORE-AGENT |
| MISSING-007 | Rate limiting | MEDIUM | BugsAndMissing | MODULE-NetworkTimeSync-AGENT |
| MISSING-008 | Input sanitization | MEDIUM | BugsAndMissing | CORE-AGENT |
| MISSING-009 | Secure defaults | LOW | BugsAndMissing | MODULE-NetworkTimeSync-AGENT |
| MISSING-010 | Error message sanitization | LOW | BugsAndMissing | CORE-AGENT |
| BUG-006 | NPD Parser date/time offset logic | MEDIUM | BugsAndMissing | CORE-AGENT |
| BUG-007 | Firewall rule name collision | MEDIUM | BugsAndMissing | MODULE-SurveyLogbook-AGENT |
| BUG-008 | UDP socket reuse without proper cleanup | MEDIUM | BugsAndMissing | MODULE-SurveyLogbook-AGENT |
| BUG-009 | JSON deserialization exception handling | MEDIUM | BugsAndMissing | LICENSING-AGENT |

#### LOW Priority - Future/Backlog

| Issue ID | Description | Severity | Report Source | Notes |
|----------|-------------|----------|---------------|-------|
| INCOMPLETE-001 | Offline license grace period edge cases | LOW | BugsAndMissing | Feature enhancement |
| INCOMPLETE-002 | Certificate revocation (offline CRL) | LOW | BugsAndMissing | Feature enhancement |
| INCOMPLETE-003 | Multi-language support | LOW | BugsAndMissing | Feature enhancement |
| DEBT-001 | Inconsistent async patterns | LOW | BugsAndMissing | Technical debt |
| DEBT-002 | Magic numbers | LOW | BugsAndMissing | Technical debt |
| DEBT-003 | Missing interface abstractions | LOW | BugsAndMissing | Technical debt |
| DEBT-004 | Incomplete XML documentation | LOW | BugsAndMissing | Technical debt |

---

## PART 2: Compliance Gap Analysis

### Current Compliance Score: 54%

Based on ComplianceChecklist.md, the following areas need improvement:

| Category | Current Score | Target Score | Gap |
|----------|---------------|--------------|-----|
| Authentication | 50% | 80% | Hardcoded secrets (FIXED), rate limiting (PENDING) |
| Session Management | 100% | 100% | COMPLIANT |
| Access Control | 60% | 80% | Audit logging (PENDING) |
| Cryptography | 56% | 80% | MD5 (FIXED), DB encryption (PENDING), cert pinning (PENDING) |
| Error Handling | 17% | 60% | Audit logging (PENDING), error sanitization (PENDING) |
| Data Protection | 33% | 70% | DB encryption (PENDING), network encryption (PENDING) |
| Business Logic | 20% | 60% | Rate limiting (PENDING), automation detection (PENDING) |

---

## PART 3: Detailed Remediation Plan

### Phase 3: Medium Priority Security Fixes (30-90 Days)

#### Task 3.1: Implement Database Encryption

| Field | Value |
|-------|-------|
| **Issue ID** | VULN-004 / MISSING-001 |
| **Assigned Agent** | DATABASE-AGENT |
| **Priority** | HIGH |
| **Estimated Effort** | 16-24 hours |
| **Timeline** | 30 days |

**Problem:**
Certificate data is stored in SQLite databases without encryption. Sensitive information including signatory names, project details is stored in plaintext.

**Locations:**
- `FathomOS.Core\Data\SqliteCertificateRepository.cs`
- `FathomOS.Core\Data\SqliteConnectionFactory.cs`

**Implementation Requirements:**
1. Add SQLCipher NuGet package: `Microsoft.Data.Sqlite.SqlCipher`
2. Modify `SqliteConnectionFactory.cs` to apply encryption key on connection open
3. Implement key management using DPAPI for machine-specific keys
4. Create migration strategy for existing unencrypted databases
5. Add unit tests for encrypted database operations

**Recommended Fix:**
```csharp
public async Task<SqliteConnection> CreateConnectionAsync()
{
    var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText = $"PRAGMA key = '{GetEncryptionKey()}';";
    await command.ExecuteNonQueryAsync();

    return connection;
}

private string GetEncryptionKey()
{
    // Derive from DPAPI-protected machine key
    return SecretManager.GetOrCreateDatabaseKey();
}
```

---

#### Task 3.2: Implement Certificate Pinning

| Field | Value |
|-------|-------|
| **Issue ID** | VULN-009 / MISSING-002 |
| **Assigned Agent** | LICENSING-AGENT |
| **Priority** | HIGH |
| **Estimated Effort** | 8-12 hours |
| **Timeline** | 30 days |

**Problem:**
HTTPS connections to the license server do not implement certificate pinning, allowing MITM attacks.

**Location:**
- `LicensingSystem.Client\LicenseClient.cs`

**Implementation Requirements:**
1. Create `PinnedHttpClientHandler` class
2. Add server certificate thumbprints as constants (with backup certificate)
3. Implement certificate validation callback
4. Add fallback mechanism for certificate rotation
5. Log certificate validation failures for security monitoring

**Recommended Fix:**
```csharp
public class PinnedHttpClientHandler : HttpClientHandler
{
    private static readonly string[] ValidThumbprints =
    {
        "PRIMARY_CERT_THUMBPRINT",
        "BACKUP_CERT_THUMBPRINT"
    };

    public PinnedHttpClientHandler()
    {
        ServerCertificateCustomValidationCallback = ValidateCertificate;
    }

    private bool ValidateCertificate(HttpRequestMessage msg, X509Certificate2? cert,
        X509Chain? chain, SslPolicyErrors errors)
    {
        if (cert == null) return false;
        return ValidThumbprints.Contains(cert.Thumbprint);
    }
}
```

---

#### Task 3.3: Implement Code Obfuscation

| Field | Value |
|-------|-------|
| **Issue ID** | VULN-002 / MISSING-003 |
| **Assigned Agent** | BUILD-AGENT (or External Tool) |
| **Priority** | HIGH |
| **Estimated Effort** | 16-24 hours |
| **Timeline** | 60 days |

**Problem:**
No code obfuscation protects the IL code, allowing easy reverse engineering and license bypass.

**Scope:** All release builds

**Implementation Requirements:**
1. Evaluate and select obfuscation tool (ConfuserEx, Eazfuscator.NET, or Dotfuscator)
2. Create MSBuild integration for Release configuration
3. Configure obfuscation rules:
   - String encryption
   - Control flow obfuscation
   - Rename obfuscation
   - Anti-tamper protection
4. Exclude necessary public APIs from obfuscation
5. Test obfuscated builds thoroughly

**Recommended Configuration (ConfuserEx):**
```xml
<project outputDir=".\Obfuscated" baseDir=".\bin\Release\net8.0-windows">
  <module path="FathomOS.Shell.dll">
    <rule pattern="true" preset="aggressive" inherit="false">
      <protection id="rename" />
      <protection id="constants" />
      <protection id="ctrl flow" />
      <protection id="ref proxy" />
      <protection id="anti tamper" />
    </rule>
  </module>
  <module path="LicensingSystem.Client.dll">
    <!-- Same protections -->
  </module>
</project>
```

---

#### Task 3.4: Implement Assembly Integrity Verification

| Field | Value |
|-------|-------|
| **Issue ID** | MISSING-004 |
| **Assigned Agent** | CORE-AGENT |
| **Priority** | MEDIUM |
| **Estimated Effort** | 8-12 hours |
| **Timeline** | 60 days |

**Problem:**
No runtime verification that assemblies haven't been modified.

**Location:**
- `FathomOS.Shell\App.xaml.cs` (startup)

**Implementation Requirements:**
1. Create `AssemblyIntegrityVerifier` class
2. Generate and embed expected assembly hashes at build time
3. Verify hashes at application startup
4. Fail closed if verification fails
5. Integrate with anti-debug checks

**Recommended Fix:**
```csharp
private static bool VerifyAssemblyIntegrity()
{
    var assembliesToVerify = new[]
    {
        typeof(App).Assembly,
        typeof(LicenseManager).Assembly,
        typeof(LicenseValidator).Assembly
    };

    foreach (var assembly in assembliesToVerify)
    {
        var expectedHash = GetExpectedHash(assembly.GetName().Name);
        var actualHash = ComputeFileHash(assembly.Location);

        if (!actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            LogSecurityEvent("Assembly integrity verification failed");
            return false;
        }
    }
    return true;
}
```

---

#### Task 3.5: Implement Secure Configuration Storage (DPAPI)

| Field | Value |
|-------|-------|
| **Issue ID** | MISSING-005 |
| **Assigned Agent** | MODULE-NetworkTimeSync-AGENT |
| **Priority** | HIGH |
| **Estimated Effort** | 8-12 hours |
| **Timeline** | 30 days |

**Problem:**
Shared secrets stored in plain config files (partially addressed in VULN-001 but storage not DPAPI-protected).

**Locations:**
- `FathomOS.Modules.NetworkTimeSync\Models\SyncConfiguration.cs`
- `NetworkTimeSync Clients solution\FathomOS.TimeSyncAgent\`

**Implementation Requirements:**
1. Create `SecureConfigurationManager` class
2. Use `ProtectedData.Protect` / `ProtectedData.Unprotect` with `DataProtectionScope.LocalMachine`
3. Store protected configuration in registry or encrypted file
4. Implement secure first-time setup workflow
5. Add admin UI for secret regeneration

**Recommended Fix:**
```csharp
public static class SecureConfigurationManager
{
    public static void StoreSecret(string name, string secret)
    {
        var protectedBytes = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(secret),
            null,
            DataProtectionScope.LocalMachine);
        // Store in registry or file
    }

    public static string GetSecret(string name)
    {
        // Load protected bytes
        var unprotectedBytes = ProtectedData.Unprotect(
            protectedBytes,
            null,
            DataProtectionScope.LocalMachine);
        return Encoding.UTF8.GetString(unprotectedBytes);
    }
}
```

---

#### Task 3.6: Implement Input Validation Framework

| Field | Value |
|-------|-------|
| **Issue ID** | VULN-006 / MISSING-008 |
| **Assigned Agent** | CORE-AGENT |
| **Priority** | MEDIUM |
| **Estimated Effort** | 16-24 hours |
| **Timeline** | 60 days |

**Problem:**
File parsers accept user-provided files without comprehensive validation (no max file size, limited path traversal protection).

**Locations:**
- `FathomOS.Core\Parsers\NpdParser.cs`
- `FathomOS.Modules.SurveyLogbook\Parsers\*`

**Implementation Requirements:**
1. Create `FileValidator` class with:
   - Maximum file size validation (configurable, default 100MB)
   - Path traversal detection
   - Magic bytes / file type verification
   - Content sanitization
2. Integrate validation into all file parsers
3. Add streaming support for large files
4. Implement comprehensive error handling

**Recommended Fix:**
```csharp
public static class FileValidator
{
    private const long MaxFileSizeMB = 100;

    public static FileValidationResult Validate(string filePath, FileType expectedType)
    {
        var result = new FileValidationResult();
        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length > MaxFileSizeMB * 1024 * 1024)
            result.Errors.Add($"File exceeds maximum size of {MaxFileSizeMB}MB");

        if (ContainsPathTraversal(filePath))
            result.Errors.Add("Invalid file path detected");

        if (!VerifyMagicBytes(filePath, expectedType))
            result.Errors.Add("File type mismatch");

        result.IsValid = result.Errors.Count == 0;
        return result;
    }
}
```

---

### Phase 4: Lower Priority Fixes (90-180 Days)

#### Task 4.1: Implement Security Audit Logging

| Field | Value |
|-------|-------|
| **Issue ID** | MISSING-006 |
| **Assigned Agent** | CORE-AGENT |
| **Priority** | MEDIUM |
| **Estimated Effort** | 16-24 hours |
| **Timeline** | 90 days |

**Implementation Requirements:**
1. Create `ISecurityAuditLog` interface
2. Implement file-based tamper-evident logging
3. Log events: authentication, authorization, data access, security violations
4. Add hash chaining for tamper detection
5. Configure log rotation and retention

---

#### Task 4.2: Implement Rate Limiting

| Field | Value |
|-------|-------|
| **Issue ID** | MISSING-007 |
| **Assigned Agent** | MODULE-NetworkTimeSync-AGENT |
| **Priority** | MEDIUM |
| **Estimated Effort** | 8-12 hours |
| **Timeline** | 90 days |

**Implementation Requirements:**
1. Create `RateLimiter` class with token bucket algorithm
2. Implement per-IP request throttling
3. Add brute-force protection (lockout after N failures)
4. Integrate with TcpListenerService

---

#### Task 4.3: Fix Error Message Sanitization

| Field | Value |
|-------|-------|
| **Issue ID** | VULN-008 / MISSING-010 |
| **Assigned Agent** | CORE-AGENT |
| **Priority** | LOW |
| **Estimated Effort** | 4-8 hours |
| **Timeline** | 120 days |

**Implementation Requirements:**
1. Create `ErrorSanitizer` class
2. Map detailed exceptions to user-friendly messages
3. Sanitize file paths (remove usernames, system paths)
4. Log detailed errors internally, show generic to users

---

#### Task 4.4: Fix Secure Default Configuration

| Field | Value |
|-------|-------|
| **Issue ID** | VULN-010 / MISSING-009 |
| **Assigned Agent** | MODULE-NetworkTimeSync-AGENT |
| **Priority** | LOW |
| **Estimated Effort** | 2-4 hours |
| **Timeline** | 90 days |

**Implementation Requirements:**
1. Change `AllowTimeSet` default to `false`
2. Change `AllowNtpSync` default to `false`
3. Require explicit configuration for dangerous operations
4. Add configuration validation on startup

---

#### Task 4.5: Fix Timing Attack Vulnerability

| Field | Value |
|-------|-------|
| **Issue ID** | VULN-011 |
| **Assigned Agent** | LICENSING-AGENT |
| **Priority** | LOW |
| **Estimated Effort** | 2-4 hours |
| **Timeline** | 120 days |

**Location:**
- `LicensingSystem.Client\HardwareFingerprint.cs:82-87`

**Implementation Requirements:**
1. Use `CryptographicOperations.FixedTimeEquals()` for security-sensitive comparisons

---

#### Task 4.6: Fix Bug-006 to Bug-009

| Task | Issue | Agent | Effort | Timeline |
|------|-------|-------|--------|----------|
| 4.6.1 | BUG-006: NPD Parser date/time offset | CORE-AGENT | 4-8 hrs | 120 days |
| 4.6.2 | BUG-007: Firewall rule name collision | MODULE-SurveyLogbook | 2-4 hrs | 120 days |
| 4.6.3 | BUG-008: UDP socket reuse cleanup | MODULE-SurveyLogbook | 4-8 hrs | 120 days |
| 4.6.4 | BUG-009: JSON deserialization handling | LICENSING-AGENT | 4-8 hrs | 120 days |

---

## PART 4: Implementation Timeline

```
PHASE 1 & 2 - COMPLETED (January 2026)
==========================================
[X] BUG-001: License delegate defaults
[X] BUG-002: Hardware fingerprint fallback
[X] BUG-003: Time reference anti-tamper
[X] BUG-004: NaviPac race condition
[X] BUG-005: Certificate sync retry count
[X] VULN-001: Hardcoded shared secrets
[X] VULN-003: MD5 to SHA-256

PHASE 3 - PLANNED (February - March 2026)
==========================================
Week 1-2 (Feb 1-14):
  [ ] Task 3.1: Database encryption (DATABASE-AGENT)
  [ ] Task 3.5: DPAPI secure storage (MODULE-NetworkTimeSync)

Week 3-4 (Feb 15-28):
  [ ] Task 3.2: Certificate pinning (LICENSING-AGENT)
  [ ] Task 3.4: Assembly integrity (CORE-AGENT)

Week 5-8 (Mar 1-31):
  [ ] Task 3.3: Code obfuscation (BUILD-AGENT)
  [ ] Task 3.6: Input validation (CORE-AGENT)

PHASE 4 - PLANNED (April - June 2026)
==========================================
Month 4 (April):
  [ ] Task 4.1: Audit logging (CORE-AGENT)
  [ ] Task 4.2: Rate limiting (MODULE-NetworkTimeSync)

Month 5 (May):
  [ ] Task 4.3: Error sanitization (CORE-AGENT)
  [ ] Task 4.4: Secure defaults (MODULE-NetworkTimeSync)

Month 6 (June):
  [ ] Task 4.5: Timing attack fix (LICENSING-AGENT)
  [ ] Task 4.6: Bug fixes 006-009 (Various)
```

---

## PART 5: Agent Assignment Summary

### CORE-AGENT Tasks
- [COMPLETED] BUG-001: License delegate defaults
- [COMPLETED] VULN-003 / BUG-005: MD5 upgrade, sync retry
- [PENDING] Task 3.4: Assembly integrity verification
- [PENDING] Task 3.6: Input validation framework
- [PENDING] Task 4.1: Audit logging
- [PENDING] Task 4.3: Error sanitization
- [PENDING] BUG-006: NPD Parser fix

### LICENSING-AGENT Tasks
- [COMPLETED] BUG-002: Hardware fingerprint fallback
- [COMPLETED] BUG-003: Time reference anti-tamper
- [PENDING] Task 3.2: Certificate pinning
- [PENDING] Task 4.5: Timing attack fix
- [PENDING] BUG-009: JSON deserialization

### DATABASE-AGENT Tasks
- [PENDING] Task 3.1: Database encryption (SQLCipher)

### MODULE-NetworkTimeSync-AGENT Tasks
- [COMPLETED] VULN-001: Hardcoded secrets
- [PENDING] Task 3.5: DPAPI secure storage
- [PENDING] Task 4.2: Rate limiting
- [PENDING] Task 4.4: Secure defaults

### MODULE-SurveyLogbook-AGENT Tasks
- [COMPLETED] BUG-004: NaviPac race condition
- [PENDING] VULN-005: Network encryption (TLS)
- [PENDING] BUG-007: Firewall rule names
- [PENDING] BUG-008: UDP socket cleanup

### BUILD-AGENT / External Tasks
- [PENDING] Task 3.3: Code obfuscation

---

## PART 6: Security Report Update Request

### For SECURITY-AGENT Review

Please update the following security reports to reflect completed fixes:

1. **SecurityAuditReport.md**
   - Update Section 2 "Security Findings Summary": Reduce HIGH count from 2 to 0
   - Update Section 6.4 "File Hashing": MD5 has been upgraded to SHA-256
   - Update Section 5.3 "Network Time Sync Agent": Hardcoded secret removed
   - Update Recommendations: Remove items 1-3 from "Immediate (0-30 days)"

2. **VulnerabilityReport.md**
   - Update VULN-001: Status -> CLOSED (secrets removed)
   - Update VULN-003: Status -> CLOSED (SHA-256 implemented)
   - Update VULN-007: Status -> CLOSED (delegates fixed)
   - Recalculate vulnerability statistics

3. **BugsAndMissingImplementation.md**
   - Update BUG-001 through BUG-005: Status -> FIXED
   - Update MISSING-005 (Secure Configuration): Partially addressed
   - Update Priority Matrix with completed items
   - Update Sprint Items with completion dates

4. **LicensingSecurityReport.md**
   - Update Section 3.3 "Fallback Behavior": CRITICAL ISSUE RESOLVED
   - Update Section 4.2 "Time Tampering Protection": Enhanced to multi-location
   - Update Section 8.1 "Critical (Immediate)": Items 1-2 completed

5. **ComplianceChecklist.md**
   - Update V2.5.1 "Default credentials": PASS (was FAIL)
   - Update V2.9.1 "Cryptographic secrets unique": PARTIAL (was FAIL)
   - Update CWE-798 "Hard-coded Credentials": PASS (was FAIL)
   - Recalculate Overall Compliance Score

6. **RecommendationsReport.md**
   - Update REC-001: Status -> COMPLETED
   - Update REC-002: Status -> COMPLETED
   - Update REC-003: Status -> COMPLETED
   - Update REC-004: Status -> COMPLETED (MD5 -> SHA-256)
   - Update Implementation Roadmap: Month 1 items completed

---

## Appendix A: Estimated Compliance Score After All Phases

| Phase | Target Completion | Expected Compliance Score |
|-------|-------------------|---------------------------|
| Phase 1 & 2 (DONE) | Jan 2026 | 62% (+8% from 54%) |
| Phase 3 | Mar 2026 | 75% (+13%) |
| Phase 4 | Jun 2026 | 82% (+7%) |

---

## Appendix B: Risk Register

| Risk ID | Risk Description | Likelihood | Impact | Mitigation |
|---------|------------------|------------|--------|------------|
| R1 | Database encryption breaks existing deployments | Medium | High | Implement migration tool |
| R2 | Obfuscation causes runtime issues | Medium | Medium | Thorough testing, staged rollout |
| R3 | Certificate pinning blocks legitimate server updates | Low | High | Include backup certificate, grace period |
| R4 | Rate limiting blocks legitimate users | Low | Medium | Configurable thresholds, whitelist |

---

*Document prepared by R&D-AGENT*
*Date: January 17, 2026*
*For SECURITY-AGENT review and report updates*
