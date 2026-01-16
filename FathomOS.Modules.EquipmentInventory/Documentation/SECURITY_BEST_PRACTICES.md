# Fathom OS Equipment System - Security Best Practices
## Comprehensive Security Guide

---

## Overview

This document outlines security best practices for the Fathom OS Equipment Management System. Following these guidelines ensures the protection of sensitive equipment data, user credentials, and system integrity.

---

## Table of Contents

1. [Authentication Security](#authentication-security)
2. [Authorization & Access Control](#authorization--access-control)
3. [Data Protection](#data-protection)
4. [API Security](#api-security)
5. [Mobile App Security](#mobile-app-security)
6. [Infrastructure Security](#infrastructure-security)
7. [Audit & Monitoring](#audit--monitoring)
8. [Incident Response](#incident-response)
9. [Compliance Checklist](#compliance-checklist)

---

## Authentication Security

### Password Requirements

| Requirement | Specification |
|-------------|---------------|
| Minimum Length | 12 characters |
| Complexity | Uppercase, lowercase, number, special character |
| History | Cannot reuse last 5 passwords |
| Expiration | 90 days (configurable) |
| Lockout | 5 failed attempts → 15 minute lockout |

### JWT Token Security

**Token Configuration:**
```json
{
  "Jwt": {
    "Secret": "[256-bit minimum secret key]",
    "Issuer": "FathomOSApi",
    "Audience": "FathomOSClients",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Best Practices:**
- ✅ Use 256-bit (32+ characters) secret keys
- ✅ Rotate secrets quarterly
- ✅ Store secrets in environment variables or secret manager
- ✅ Use short-lived access tokens (15 minutes)
- ✅ Implement token refresh mechanism
- ❌ Never expose tokens in URLs
- ❌ Never log tokens

**Secret Key Generation:**
```bash
# Generate secure 256-bit key
openssl rand -base64 32

# Or using .NET
dotnet run --project tools/GenerateSecret
```

### PIN Security (Mobile)

**Requirements:**
- 4-6 digit PIN
- Hashed with BCrypt (cost factor 12)
- Device-bound (associated with device ID)
- Auto-logout after 5 minutes inactivity
- Maximum 3 failed attempts before lockout

**Implementation:**
```csharp
// PIN hashing
var hashedPin = BCrypt.Net.BCrypt.HashPassword(pin, workFactor: 12);

// PIN verification
var isValid = BCrypt.Net.BCrypt.Verify(inputPin, storedHash);
```

### Session Management

| Setting | Value | Purpose |
|---------|-------|---------|
| Access Token Lifetime | 15 minutes | Limit exposure window |
| Refresh Token Lifetime | 7 days | Balance security/convenience |
| Sliding Expiration | Disabled | Prevent indefinite sessions |
| Concurrent Sessions | 5 per user | Limit attack surface |

---

## Authorization & Access Control

### Role-Based Access Control (RBAC)

**Default Roles:**

| Role | Description | Permissions |
|------|-------------|-------------|
| System Administrator | Full system access | All permissions |
| Base Manager | Manage onshore operations | equipment.*, manifest.*, reports.view |
| Vessel Superintendent | Manage vessel equipment | equipment.*, manifest.*, reports.view |
| Project Manager | Project oversight | manifest.approve, reports.* |
| Deck Operator | Field operations | equipment.view, manifest.create |
| Store Keeper | Inventory management | equipment.*, manifest.create |
| Auditor | Read-only access | *.view, reports.* |

### Permission Enforcement

```csharp
// Controller-level authorization
[Authorize(Policy = "CanApproveManifests")]
[HttpPost("{id}/approve")]
public async Task<IActionResult> ApproveManifest(Guid id)
{
    // Only users with manifest.approve permission can access
}

// Service-level authorization
public async Task<bool> CanUserAccessEquipment(Guid userId, Guid equipmentId)
{
    var userLocations = await _unitOfWork.Users.GetUserLocationsAsync(userId);
    var equipment = await _unitOfWork.Equipment.GetByIdAsync(equipmentId);
    return userLocations.Contains(equipment.CurrentLocationId);
}
```

### Location-Based Access

Users are restricted to equipment and manifests at their assigned locations:

```csharp
// Filter equipment by user's locations
var userLocationIds = await _userRepository.GetUserLocationIdsAsync(userId);
var equipment = await _equipmentRepository
    .Query()
    .Where(e => userLocationIds.Contains(e.CurrentLocationId))
    .ToListAsync();
```

---

## Data Protection

### Encryption at Rest

**Database:**
- PostgreSQL: Use `pgcrypto` extension for sensitive columns
- SQL Server: Enable Transparent Data Encryption (TDE)

**File Storage:**
- Encrypt uploaded files using AES-256
- Store encryption keys in Azure Key Vault or AWS KMS

**Mobile Device:**
- WatermelonDB with SQLCipher encryption
- iOS Keychain for credentials
- Android Keystore for credentials

### Encryption in Transit

**Requirements:**
- TLS 1.2 minimum (TLS 1.3 preferred)
- Strong cipher suites only
- HSTS enabled with 1-year max-age

**IIS Configuration:**
```xml
<!-- web.config -->
<system.webServer>
  <rewrite>
    <rules>
      <rule name="HTTPS Redirect" stopProcessing="true">
        <match url="(.*)" />
        <conditions>
          <add input="{HTTPS}" pattern="^OFF$" />
        </conditions>
        <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
      </rule>
    </rules>
  </rewrite>
</system.webServer>
```

### Sensitive Data Handling

| Data Type | Storage | Transmission | Logging |
|-----------|---------|--------------|---------|
| Passwords | BCrypt hash only | Never transmitted plain | Never logged |
| JWT Tokens | Memory only | HTTPS header | Never logged |
| PINs | BCrypt hash | HTTPS body | Never logged |
| Equipment Data | Database | HTTPS | Audit log |
| User PII | Database | HTTPS | Masked in logs |

### Data Masking

```csharp
// Mask email in logs
public static string MaskEmail(string email)
{
    var parts = email.Split('@');
    if (parts.Length != 2) return "***";
    var name = parts[0];
    var masked = name.Length > 2 
        ? name[0] + new string('*', name.Length - 2) + name[^1]
        : "**";
    return $"{masked}@{parts[1]}";
}

// Usage in logging
_logger.LogInformation("User {Email} logged in", MaskEmail(user.Email));
// Output: "User j***n@company.com logged in"
```

---

## API Security

### Input Validation

**Always validate:**
- Request body against DTOs with data annotations
- Query parameters with explicit parsing
- Path parameters with type constraints
- File uploads for type and size

```csharp
// DTO with validation
public class CreateEquipmentRequest
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Range(0, 100000)]
    public decimal? WeightKg { get; set; }

    [RegularExpression(@"^[A-Z0-9-]+$")]
    public string? SerialNumber { get; set; }
}

// Controller validation
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateEquipmentRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    // ...
}
```

### SQL Injection Prevention

**Always use:**
- Entity Framework Core (parameterized queries)
- Stored procedures with parameters
- Never string concatenation for queries

```csharp
// ✅ SAFE - Parameterized query
var equipment = await _context.Equipment
    .Where(e => e.Name.Contains(searchTerm))
    .ToListAsync();

// ❌ DANGEROUS - String concatenation
var sql = $"SELECT * FROM Equipment WHERE Name LIKE '%{searchTerm}%'";
```

### XSS Prevention

**Output Encoding:**
```csharp
// API returns JSON - automatically encoded
// For any HTML rendering, use:
var encoded = System.Web.HttpUtility.HtmlEncode(userInput);
```

**Content Security Policy:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    await next();
});
```

### Rate Limiting

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

app.UseRateLimiter();
```

### CORS Configuration

```csharp
// Production CORS - restrict origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
            "https://app.company.com",
            "capacitor://localhost",  // iOS app
            "https://localhost"       // Android app
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});
```

### Security Headers

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=()");
    await next();
});
```

---

## Mobile App Security

### Secure Storage

**iOS:**
```typescript
// Use Keychain for sensitive data
import * as Keychain from 'react-native-keychain';

await Keychain.setGenericPassword('accessToken', token, {
  service: 's7fathom',
  accessible: Keychain.ACCESSIBLE.WHEN_UNLOCKED_THIS_DEVICE_ONLY,
});
```

**Android:**
```typescript
// Use encrypted SharedPreferences
import EncryptedStorage from 'react-native-encrypted-storage';

await EncryptedStorage.setItem('accessToken', token);
```

### Certificate Pinning

```typescript
// src/services/api.ts
import { fetch } from 'react-native-ssl-pinning';

const response = await fetch(url, {
  method: 'GET',
  sslPinning: {
    certs: ['api_certificate'], // Certificate in assets
  },
  headers: {
    Authorization: `Bearer ${token}`,
  },
});
```

### Biometric Authentication

```typescript
import ReactNativeBiometrics from 'react-native-biometrics';

const { available, biometryType } = await ReactNativeBiometrics.isSensorAvailable();

if (available) {
  const { success } = await ReactNativeBiometrics.simplePrompt({
    promptMessage: 'Confirm your identity',
  });
  if (success) {
    // Proceed with authentication
  }
}
```

### Jailbreak/Root Detection

```typescript
import JailMonkey from 'jail-monkey';

if (JailMonkey.isJailBroken()) {
  Alert.alert(
    'Security Warning',
    'This device appears to be jailbroken. Some features may be disabled.',
  );
}
```

### Secure Data Clearing

```typescript
// Clear all sensitive data on logout
const handleLogout = async () => {
  // Clear Redux state
  dispatch(authSlice.actions.logout());
  
  // Clear secure storage
  await Keychain.resetGenericPassword();
  await EncryptedStorage.clear();
  
  // Clear WatermelonDB
  await database.write(async () => {
    await database.unsafeResetDatabase();
  });
  
  // Navigate to login
  navigation.reset({ index: 0, routes: [{ name: 'Login' }] });
};
```

---

## Infrastructure Security

### Network Segmentation

```
┌─────────────────────────────────────────────────────────────┐
│                      DMZ Zone                                │
│  ┌─────────────────┐                                        │
│  │  Load Balancer  │ ← HTTPS only (443)                     │
│  └────────┬────────┘                                        │
└───────────┼─────────────────────────────────────────────────┘
            │
┌───────────┼─────────────────────────────────────────────────┐
│           │           Application Zone                       │
│  ┌────────▼────────┐                                        │
│  │   API Servers   │ ← Internal network only                │
│  └────────┬────────┘                                        │
└───────────┼─────────────────────────────────────────────────┘
            │
┌───────────┼─────────────────────────────────────────────────┐
│           │           Data Zone                              │
│  ┌────────▼────────┐                                        │
│  │    Database     │ ← Database port only (5432/1433)       │
│  └─────────────────┘                                        │
└─────────────────────────────────────────────────────────────┘
```

### Firewall Rules

```powershell
# Allow HTTPS from internet
New-NetFirewallRule -DisplayName "S7 API HTTPS" -Direction Inbound `
    -Protocol TCP -LocalPort 443 -Action Allow

# Allow API to Database (internal only)
New-NetFirewallRule -DisplayName "S7 PostgreSQL" -Direction Outbound `
    -Protocol TCP -RemotePort 5432 -RemoteAddress 10.0.2.0/24 -Action Allow

# Block all other inbound
Set-NetFirewallProfile -Profile Domain,Public,Private -DefaultInboundAction Block
```

### Database Security

**PostgreSQL:**
```sql
-- Use strong passwords
ALTER USER s7fathom WITH PASSWORD 'complex-password-here';

-- Grant minimal permissions
REVOKE ALL ON DATABASE s7fathom_equipment FROM PUBLIC;
GRANT CONNECT ON DATABASE s7fathom_equipment TO s7fathom;
GRANT USAGE ON SCHEMA public TO s7fathom;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO s7fathom;

-- Enable SSL connections
-- In postgresql.conf:
-- ssl = on
-- ssl_cert_file = 'server.crt'
-- ssl_key_file = 'server.key'
```

---

## Audit & Monitoring

### Audit Log Requirements

**What to Log:**
- All authentication attempts (success/failure)
- All authorization decisions
- All data modifications (create/update/delete)
- All admin actions
- All sync operations
- Security-relevant events

**Log Format:**
```json
{
  "timestamp": "2024-01-20T10:30:00Z",
  "level": "Information",
  "category": "Security",
  "action": "Login",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john.smith",
  "ipAddress": "192.168.1.100",
  "userAgent": "FathomOSMobile/1.0.0",
  "deviceId": "device-uuid",
  "result": "Success",
  "details": {}
}
```

### Security Monitoring Alerts

| Event | Threshold | Action |
|-------|-----------|--------|
| Failed logins | 5 in 5 minutes | Alert + temporary lockout |
| Password changes | Any | Email notification to user |
| Admin actions | Any | Immediate alert to security team |
| Unusual access pattern | Anomaly detection | Review and investigate |
| API errors spike | >5% error rate | Alert operations team |

### Log Retention

| Log Type | Retention | Storage |
|----------|-----------|---------|
| Security logs | 2 years | Encrypted archive |
| Audit logs | 7 years | Encrypted archive |
| Application logs | 90 days | Hot storage |
| Access logs | 1 year | Compressed archive |

---

## Incident Response

### Incident Classification

| Severity | Description | Response Time |
|----------|-------------|---------------|
| Critical | Data breach, system compromise | Immediate |
| High | Unauthorized access attempt | 1 hour |
| Medium | Policy violation | 24 hours |
| Low | Security misconfiguration | 72 hours |

### Response Procedures

**1. Data Breach:**
```
1. Isolate affected systems
2. Preserve evidence (logs, memory dumps)
3. Notify security team and management
4. Assess scope of breach
5. Implement containment measures
6. Notify affected users (if required)
7. Document incident and remediation
8. Conduct post-incident review
```

**2. Compromised Credentials:**
```
1. Disable affected user accounts
2. Revoke all active sessions/tokens
3. Reset passwords
4. Review audit logs for unauthorized access
5. Assess data accessed
6. Notify user and management
```

**3. API Attack:**
```
1. Enable enhanced rate limiting
2. Block attacking IP addresses
3. Enable additional logging
4. Review for vulnerability exploitation
5. Apply patches if needed
6. Monitor for continued attempts
```

### Emergency Contacts

| Role | Contact | Escalation |
|------|---------|------------|
| Security Lead | security@company.com | Immediate |
| DevOps On-Call | +1-XXX-XXX-XXXX | 15 minutes |
| Management | management@company.com | 1 hour |
| Legal | legal@company.com | As needed |

---

## Compliance Checklist

### Pre-Deployment Security Review

- [ ] JWT secret is 256+ bits and securely stored
- [ ] All endpoints require authentication (except /health, /auth/login)
- [ ] HTTPS enforced with TLS 1.2+
- [ ] CORS configured for specific origins only
- [ ] Rate limiting enabled
- [ ] Input validation on all endpoints
- [ ] SQL injection prevention verified
- [ ] XSS prevention verified
- [ ] Security headers configured
- [ ] Audit logging enabled
- [ ] Database encryption enabled
- [ ] Backup encryption enabled
- [ ] Certificate pinning (mobile)
- [ ] Secure storage (mobile)
- [ ] Penetration testing completed

### Periodic Security Tasks

**Weekly:**
- [ ] Review security alerts
- [ ] Check for failed login patterns
- [ ] Verify backup integrity

**Monthly:**
- [ ] Review user access permissions
- [ ] Update dependencies
- [ ] Review audit logs
- [ ] Test incident response procedures

**Quarterly:**
- [ ] Rotate JWT secrets
- [ ] Rotate database passwords
- [ ] Conduct access review
- [ ] Update security documentation
- [ ] Security awareness training

**Annually:**
- [ ] Penetration testing
- [ ] Comprehensive security audit
- [ ] Disaster recovery test
- [ ] Policy review and update

---

## Quick Reference

### Security Configuration Values

```json
{
  "Security": {
    "PasswordMinLength": 12,
    "PasswordRequireUppercase": true,
    "PasswordRequireLowercase": true,
    "PasswordRequireDigit": true,
    "PasswordRequireSpecialChar": true,
    "PasswordHistoryCount": 5,
    "PasswordExpirationDays": 90,
    "MaxFailedLoginAttempts": 5,
    "LockoutDurationMinutes": 15,
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7,
    "PinMaxAttempts": 3,
    "PinLockoutMinutes": 30,
    "SessionIdleTimeoutMinutes": 30,
    "MaxConcurrentSessions": 5
  }
}
```

---

**Document Version:** 1.0  
**Last Updated:** January 2025  
**Review Frequency:** Quarterly  
**Owner:** Security Team
