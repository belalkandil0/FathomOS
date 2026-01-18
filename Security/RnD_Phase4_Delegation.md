# FathomOS Security Phase 4 - Delegation Plan

**Created By:** R&D-AGENT
**Date:** January 17, 2026
**Status:** PENDING DELEGATION
**Phase:** 4 (Final Security Items)

---

## Executive Summary

This document outlines the delegation plan for all remaining security items identified in the Phase 1-3 security audit reports. Following the successful completion of 73% of security items, Phase 4 focuses on completing the remaining 7 items to achieve full security compliance.

### Remaining Items Overview

| Item ID | Description | Priority | Estimated Effort |
|---------|-------------|----------|------------------|
| MISSING-003 / REC-007 / VULN-002 | Code Obfuscation | P1 (High) | 16 hours |
| MISSING-006 / REC-011 | Security Audit Logging | P2 (Medium) | 16 hours |
| MISSING-007 / REC-013 | Rate Limiting | P3 (Low) | 16 hours |
| VULN-005 / REC-010 | TLS Encryption for NaviPac/TimeSync | P2 (Medium) | 32 hours |
| VULN-008 / MISSING-010 / REC-015 | Error Message Sanitization | P3 (Low) | 8 hours |
| BUG-006 | NPD Parser Date/Time Validation | P2 (Medium) | 4 hours |
| BUG-007 | Firewall Rule Name Collision | P2 (Medium) | 2 hours |

**Total Estimated Effort:** ~94 hours

---

## Item 1: Code Obfuscation (MISSING-003 / REC-007 / VULN-002)

### Summary
No code obfuscation currently protects IL code in release builds. This leaves the application vulnerable to reverse engineering, IP theft, and license bypass through decompilation.

### Related References
- MISSING-003 (BugsAndMissingImplementation.md)
- REC-007 (RecommendationsReport.md)
- VULN-002 (VulnerabilityReport.md - partial fix)

### Assigned Agent
**BUILD-AGENT**

### Priority
**P1 - High** (Anti-tampering critical for license protection)

### Implementation Requirements
1. **Tool Selection** - Evaluate and select obfuscation solution:
   - Eazfuscator.NET (commercial, excellent .NET support)
   - ConfuserEx (open source)
   - Dotfuscator (included with Visual Studio)

2. **MSBuild Integration**:
   - Configure obfuscation as post-build step for Release configuration only
   - Ensure Debug builds remain unobfuscated for development
   - Create MSBuild targets in `Directory.Build.targets`

3. **Obfuscation Features Required**:
   - String encryption (protect hardcoded strings)
   - Control flow obfuscation
   - Symbol renaming
   - Anti-debugging enhancements
   - Anti-decompilation measures

4. **Assemblies to Protect** (Priority Order):
   - `LicensingSystem.Client.dll`
   - `FathomOS.Core.dll`
   - `FathomOS.Shell.exe`
   - All module DLLs

5. **Testing Requirements**:
   - Verify all functionality works after obfuscation
   - Confirm strong name signing still works
   - Test assembly integrity verification compatibility

### Files to Create/Modify
- `Directory.Build.targets` - Add obfuscation targets
- `obfuscation.config` or equivalent tool-specific config
- CI/CD pipeline updates for release builds

### Acceptance Criteria
- [ ] Obfuscation tool integrated into build pipeline
- [ ] Release builds produce obfuscated assemblies
- [ ] All unit tests pass on obfuscated builds
- [ ] Application launches and runs correctly
- [ ] License validation still works
- [ ] Decompilation with dnSpy/ILSpy shows obfuscated code

---

## Item 2: Security Audit Logging (MISSING-006 / REC-011)

### Summary
No comprehensive audit trail exists for security-relevant events. Security events (login attempts, license validation, certificate operations) are not centrally logged with tamper detection.

### Related References
- MISSING-006 (BugsAndMissingImplementation.md)
- REC-011 (RecommendationsReport.md)

### Assigned Agent
**CORE-AGENT**

### Priority
**P2 - Medium** (Required for compliance and incident investigation)

### Implementation Requirements
1. **Create Interface**:
   ```csharp
   // FathomOS.Core\Security\ISecurityAuditLog.cs
   public interface ISecurityAuditLog
   {
       Task LogEventAsync(SecurityEventType eventType, string details, SecurityEventSeverity severity);
       Task<IEnumerable<SecurityEvent>> GetEventsAsync(DateTime from, DateTime to);
   }
   ```

2. **Implement File-Based Audit Log**:
   - Append-only log files with rotation
   - Digital signatures for tamper detection
   - Log to: `%ProgramData%\FathomOS\SecurityLogs\`

3. **Security Events to Log**:
   - License validation attempts (success/failure)
   - License activation/deactivation
   - Certificate creation and sync
   - Assembly integrity check results
   - Authentication failures
   - Configuration changes to security settings
   - Time reference tampering detection
   - Hardware fingerprint changes

4. **Event Format**:
   ```json
   {
     "timestamp": "2026-01-17T10:30:00Z",
     "eventType": "LicenseValidation",
     "severity": "Info",
     "machineId": "ABC123...",
     "details": "License validated successfully for tier: Professional",
     "signature": "HMAC-SHA256..."
   }
   ```

5. **Tamper Detection**:
   - HMAC signature for each log entry
   - Chain hash linking entries together
   - Periodic integrity verification

### Files to Create
- `FathomOS.Core\Security\ISecurityAuditLog.cs`
- `FathomOS.Core\Security\SecurityAuditLog.cs`
- `FathomOS.Core\Security\SecurityEvent.cs`
- `FathomOS.Core\Security\SecurityEventType.cs`

### Files to Modify
- `LicensingSystem.Client\LicenseClient.cs` - Add audit logging calls
- `FathomOS.Core\Security\AssemblyIntegrityVerifier.cs` - Add logging
- `FathomOS.Shell\App.xaml.cs` - Initialize audit log

### Acceptance Criteria
- [ ] ISecurityAuditLog interface created
- [ ] File-based implementation with tamper detection
- [ ] All specified security events are logged
- [ ] Log rotation configured
- [ ] Audit logs can be retrieved and reviewed
- [ ] Tamper detection working (modifying logs triggers alert)

---

## Item 3: Rate Limiting (MISSING-007 / REC-013)

### Summary
The TCP listener service in NetworkTimeSync accepts unlimited connections, making it vulnerable to denial-of-service attacks and brute-force attempts.

### Related References
- MISSING-007 (BugsAndMissingImplementation.md)
- REC-013 (RecommendationsReport.md)

### Assigned Agent
**NETWORK-AGENT**

### Priority
**P3 - Low** (DoS protection for network services)

### Implementation Requirements
1. **Token Bucket Rate Limiter**:
   ```csharp
   // FathomOS.Core\Network\RateLimiter.cs
   public class TokenBucketRateLimiter
   {
       public bool TryConsume(string clientId);
       public void Configure(int tokensPerSecond, int bucketSize);
   }
   ```

2. **Per-IP Request Throttling**:
   - Track connections per IP address
   - Maximum 10 connections per minute per IP (configurable)
   - Block IPs exceeding threshold for 5 minutes

3. **Brute-Force Protection**:
   - Track failed authentication attempts per IP
   - After 5 failures, implement exponential backoff
   - Block after 10 failures for 30 minutes

4. **Integration Points**:
   - `TcpListenerService.cs` - Add rate limit checks before processing
   - `NetworkDiscoveryService.cs` - Add discovery rate limiting

5. **Configuration**:
   ```json
   {
     "RateLimiting": {
       "Enabled": true,
       "ConnectionsPerMinute": 10,
       "AuthFailuresBeforeBlock": 10,
       "BlockDurationMinutes": 30
     }
   }
   ```

### Files to Create
- `FathomOS.Core\Network\RateLimiter.cs`
- `FathomOS.Core\Network\IRateLimiter.cs`
- `FathomOS.Core\Network\BruteForceProtection.cs`

### Files to Modify
- `NetworkTimeSync Clients solution\FathomOS.TimeSyncAgent\Services\TcpListenerService.cs`
- `FathomOS.Modules.NetworkTimeSync\Services\NetworkDiscoveryService.cs`

### Acceptance Criteria
- [ ] Token bucket rate limiter implemented
- [ ] Per-IP throttling working
- [ ] Brute-force protection with exponential backoff
- [ ] Configuration options available
- [ ] Unit tests for rate limiting logic
- [ ] Integration tests showing blocked requests

---

## Item 4: TLS Encryption for NaviPac/TimeSync (VULN-005 / REC-010)

### Summary
NaviPac TCP/UDP connections and TimeSync Agent TCP connections transmit data in cleartext, allowing interception and man-in-the-middle attacks.

### Related References
- VULN-005 (VulnerabilityReport.md)
- REC-010 (RecommendationsReport.md)

### Assigned Agent
**NETWORK-AGENT**

### Priority
**P2 - Medium** (Network security for internal communications)

### Implementation Requirements
1. **Self-Signed Certificate Generation**:
   - Create utility to generate self-signed certificates for internal use
   - Store in Windows certificate store (LocalMachine\My)
   - Include subject alternative names for localhost

2. **TLS Wrapper for TCP**:
   ```csharp
   // FathomOS.Core\Network\SecureTcpClient.cs
   public class SecureTcpClient
   {
       public async Task<SslStream> ConnectAsync(string host, int port);
   }
   ```

3. **NaviPac Client Updates**:
   - Add TLS option for TCP connections
   - Backward compatibility mode for non-TLS servers
   - Certificate validation (with option to trust self-signed)

4. **TimeSync Agent Updates**:
   - Upgrade TcpListenerService to use SslStream
   - HMAC authentication remains as additional layer
   - TLS 1.2 minimum (prefer TLS 1.3)

5. **DTLS for UDP (Optional/Future)**:
   - Document that UDP remains unencrypted
   - Recommend using TCP with TLS for sensitive data
   - Consider DTLS for future enhancement

6. **Configuration**:
   ```json
   {
     "Network": {
       "UseTls": true,
       "TlsVersion": "1.2",
       "AllowSelfSigned": true,
       "CertificateThumbprint": "optional-pinning"
     }
   }
   ```

### Files to Create
- `FathomOS.Core\Network\SecureTcpClient.cs`
- `FathomOS.Core\Network\SecureTcpListener.cs`
- `FathomOS.Core\Network\CertificateGenerator.cs`

### Files to Modify
- `FathomOS.Modules.SurveyLogbook\Services\NaviPacClient.cs`
- `NetworkTimeSync Clients solution\FathomOS.TimeSyncAgent\Services\TcpListenerService.cs`
- Configuration files for TLS settings

### Acceptance Criteria
- [ ] Self-signed certificate generation utility created
- [ ] TLS wrapper for TCP implemented
- [ ] NaviPac client supports TLS connections
- [ ] TimeSync Agent uses TLS for TCP
- [ ] Backward compatibility maintained
- [ ] TLS 1.2+ enforced
- [ ] Integration tests verify encrypted communication

---

## Item 5: Error Message Sanitization (VULN-008 / MISSING-010 / REC-015)

### Summary
Error messages and exceptions may expose sensitive system information including file paths, stack traces, and version details to end users.

### Related References
- VULN-008 (VulnerabilityReport.md)
- MISSING-010 (BugsAndMissingImplementation.md)
- REC-015 (RecommendationsReport.md)

### Assigned Agent
**CORE-AGENT**

### Priority
**P3 - Low** (Information disclosure prevention)

### Implementation Requirements
1. **Create Error Sanitizer Utility**:
   ```csharp
   // FathomOS.Core\Errors\ErrorSanitizer.cs
   public static class ErrorSanitizer
   {
       public static string SanitizeForUser(Exception ex);
       public static string GetUserFriendlyMessage(ErrorCode code);
   }
   ```

2. **Error Code Mapping**:
   - Define standardized error codes
   - Map detailed exceptions to user-friendly messages
   - Maintain internal error details for logging

3. **Stack Trace Suppression**:
   - Remove stack traces from user-facing errors in Release builds
   - Use `#if DEBUG` for detailed error output
   - Log full stack traces to audit log

4. **Path Sanitization**:
   - Remove absolute file paths from user messages
   - Replace with generic descriptions ("configuration file", "data file")
   - Log actual paths internally

5. **Exception Handling Pattern**:
   ```csharp
   catch (Exception ex)
   {
       _auditLog.LogError(ex); // Full details
       throw new UserFacingException(ErrorSanitizer.SanitizeForUser(ex));
   }
   ```

6. **Integration Points**:
   - Global exception handler in Shell
   - All user-facing ViewModels
   - API error responses

### Files to Create
- `FathomOS.Core\Errors\ErrorSanitizer.cs`
- `FathomOS.Core\Errors\ErrorCode.cs`
- `FathomOS.Core\Errors\UserFacingException.cs`

### Files to Modify
- `FathomOS.Shell\App.xaml.cs` - Global exception handler
- Various ViewModels - Use sanitized errors

### Acceptance Criteria
- [ ] ErrorSanitizer utility created
- [ ] Error code mapping defined
- [ ] Stack traces removed in Release builds
- [ ] File paths sanitized
- [ ] User-friendly messages displayed
- [ ] Full details logged internally

---

## Item 6: NPD Parser Date/Time Validation (BUG-006)

### Summary
The NPD Parser's date/time split offset logic relies on correct configuration. Incorrect configuration leads to silent column misalignment.

### Related References
- BUG-006 (BugsAndMissingImplementation.md)

### Assigned Agent
**CORE-AGENT**

### Priority
**P2 - Medium** (Data integrity)

### Implementation Requirements
1. **Column Count Validation**:
   - Compare expected vs actual column count
   - Emit warning if mismatch detected
   - Provide diagnostic information

2. **Configuration Validation**:
   - Validate `HasDateTimeSplit` against actual data
   - Auto-detect date/time format where possible
   - Warn on ambiguous configurations

3. **Error Reporting**:
   ```csharp
   if (actualColumns != expectedColumns)
   {
       _logger.Warning("Column count mismatch: expected {Expected}, got {Actual}. " +
           "Check HasDateTimeSplit configuration.", expectedColumns, actualColumns);
   }
   ```

### Files to Modify
- `FathomOS.Core\Parsers\NpdParser.cs`

### Acceptance Criteria
- [ ] Column count validation added
- [ ] Warning emitted on mismatch
- [ ] Configuration validation improved
- [ ] Unit tests for edge cases

---

## Item 7: Firewall Rule Name Collision (BUG-007)

### Summary
Firewall rule names use a fixed prefix without namespace, potentially colliding with rules from other applications.

### Related References
- BUG-007 (BugsAndMissingImplementation.md)

### Assigned Agent
**NETWORK-AGENT**

### Priority
**P2 - Medium** (System stability)

### Implementation Requirements
1. **Unique Rule Naming**:
   - Include application GUID in rule names
   - Format: `FathomOS_{GUID}_{RuleType}_{Port}`

2. **Version/Instance Tracking**:
   - Track which rules were created by this installation
   - Clean up orphaned rules from old installations

3. **Implementation**:
   ```csharp
   private static readonly string AppGuid = "YOUR-APP-GUID-HERE";
   private string GetRuleName(string ruleType, int port) =>
       $"FathomOS_{AppGuid}_{ruleType}_{port}";
   ```

### Files to Modify
- `FathomOS.Modules.SurveyLogbook\Services\FirewallService.cs`

### Acceptance Criteria
- [ ] Firewall rules use unique names with GUID
- [ ] No collision with other applications
- [ ] Old rule cleanup mechanism in place

---

## Delegation Summary

### Agent Assignments

| Agent | Items Assigned | Total Effort |
|-------|----------------|--------------|
| **BUILD-AGENT** | Item 1 (Code Obfuscation) | 16 hours |
| **CORE-AGENT** | Item 2 (Audit Logging), Item 5 (Error Sanitization), Item 6 (NPD Parser) | 28 hours |
| **NETWORK-AGENT** | Item 3 (Rate Limiting), Item 4 (TLS Encryption), Item 7 (Firewall Names) | 50 hours |

### Implementation Order (Priority)

1. **Week 1-2: High Priority**
   - Item 1: Code Obfuscation (BUILD-AGENT)

2. **Week 3-4: Medium Priority**
   - Item 2: Security Audit Logging (CORE-AGENT)
   - Item 4: TLS Encryption (NETWORK-AGENT)
   - Item 6: NPD Parser Validation (CORE-AGENT)
   - Item 7: Firewall Rule Names (NETWORK-AGENT)

3. **Week 5: Low Priority**
   - Item 3: Rate Limiting (NETWORK-AGENT)
   - Item 5: Error Message Sanitization (CORE-AGENT)

---

## Success Metrics

Upon completion of Phase 4:

- **Security Compliance:** 100% (up from 85%)
- **Vulnerabilities Closed:** 11/11 (100%)
- **Missing Implementations:** 10/10 (100%)
- **Risk Level:** LOW (maintained)

---

## Notes for Agents

1. **BUILD-AGENT**: Coordinate with all other agents to ensure obfuscation doesn't break functionality. Run full test suite on obfuscated builds.

2. **CORE-AGENT**: Audit logging should be initialized early in application startup. Coordinate with other teams on what events to log.

3. **NETWORK-AGENT**: TLS implementation should maintain backward compatibility. Document upgrade path for existing installations.

4. **All Agents**: Update the respective security report files upon completion of assigned items.

---

*Delegation plan created by R&D-AGENT - January 17, 2026*
