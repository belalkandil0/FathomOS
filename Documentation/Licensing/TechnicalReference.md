# Licensing Technical Reference

**Version:** 3.5.0
**Last Updated:** January 2026

## Overview

This document provides technical implementation details for the FathomOS offline-first licensing system.

---

## Cryptography

### Algorithms

| Purpose | Algorithm | Details |
|---------|-----------|---------|
| License Signing | ECDSA P-256 | NIST P-256 curve (secp256r1) |
| Hash Function | SHA-256 | For signatures and fingerprints |
| Password Hashing | PBKDF2 | 100,000 iterations, SHA-256 |
| Local Storage Encryption | DPAPI | Windows Data Protection API |
| Key Encoding | Base64 | For signatures and keys in JSON |

### Key Specifications

**ECDSA Key Pair:**
- Curve: P-256 (secp256r1)
- Private Key: 32 bytes
- Public Key: 65 bytes (uncompressed) or 33 bytes (compressed)
- Signature: Variable length, typically 64-72 bytes

**PEM Format:**
```
-----BEGIN EC PRIVATE KEY-----
[Base64 encoded private key data]
-----END EC PRIVATE KEY-----

-----BEGIN PUBLIC KEY-----
[Base64 encoded public key data]
-----END PUBLIC KEY-----
```

### Signature Process

1. License JSON is serialized (deterministic order)
2. SHA-256 hash computed
3. Hash signed with ECDSA private key
4. Signature Base64-encoded and added to license

```csharp
// Signing (License Generator)
var hash = SHA256.HashData(licenseBytes);
var signature = ecdsa.SignHash(hash);
license.Signature = Convert.ToBase64String(signature);

// Verification (FathomOS Client)
var hash = SHA256.HashData(licenseBytes);
var signature = Convert.FromBase64String(license.Signature);
bool valid = ecdsa.VerifyHash(hash, signature);
```

---

## License Model (OfflineLicense)

### Complete Schema

```csharp
public class OfflineLicense
{
    // Identification
    public string Id { get; set; }              // "LIC-2026-001234"
    public string Version { get; set; }         // "3.5.0"

    // Client Information
    public ClientInfo Client { get; set; }

    // Product Information
    public ProductInfo Product { get; set; }

    // License Terms
    public LicenseTerms Terms { get; set; }

    // Entitlements
    public List<string> Modules { get; set; }   // ["SurveyListing", "GnssCalibration"]
    public List<string> Features { get; set; }  // ["ExcelExport", "PdfExport"]

    // Hardware Binding (Optional)
    public HardwareBinding? Binding { get; set; }

    // Cryptographic
    public string Signature { get; set; }       // Base64 ECDSA signature
    public string PublicKeyId { get; set; }     // Key identifier
}

public class ClientInfo
{
    public string Name { get; set; }            // "Oceanic Surveys Ltd"
    public string Code { get; set; }            // "OSL" (3-letter code)
    public string Email { get; set; }           // "admin@oceanic.com"
}

public class ProductInfo
{
    public string Name { get; set; }            // "FathomOS"
    public string Edition { get; set; }         // "Basic", "Professional", "Enterprise"
}

public class LicenseTerms
{
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int GracePeriodDays { get; set; }    // Default: 7
}

public class HardwareBinding
{
    public List<string> Fingerprints { get; set; }
    public int MinMatching { get; set; }        // Default: 3
}
```

### JSON Representation

```json
{
  "Id": "LIC-2026-001234",
  "Version": "3.5.0",
  "Client": {
    "Name": "Oceanic Surveys Ltd",
    "Code": "OSL",
    "Email": "admin@oceanicsurveys.com"
  },
  "Product": {
    "Name": "FathomOS",
    "Edition": "Professional"
  },
  "Terms": {
    "IssuedAt": "2026-01-15T00:00:00Z",
    "ExpiresAt": "2027-01-15T00:00:00Z",
    "GracePeriodDays": 7
  },
  "Modules": [
    "SurveyListing",
    "GnssCalibration",
    "UsblVerification",
    "MruCalibration"
  ],
  "Features": [
    "ExcelExport",
    "PdfExport",
    "DxfExport",
    "3DVisualization"
  ],
  "Binding": {
    "Fingerprints": [
      "A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6",
      "B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7",
      "C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7R8"
    ],
    "MinMatching": 3
  },
  "Signature": "MEUCIQDx...BASE64_ECDSA_SIGNATURE...==",
  "PublicKeyId": "KEY001"
}
```

---

## Key Components

### LicenseSigner (License Generator UI)

Location: `LicensingSystem.LicenseGeneratorUI/Services/LicenseSigningService.cs`

```csharp
public class LicenseSigner
{
    private readonly ECDsa _privateKey;

    public LicenseSigner(string privateKeyPem)
    {
        _privateKey = ECDsa.Create();
        _privateKey.ImportFromPem(privateKeyPem);
    }

    public string SignLicense(OfflineLicense license)
    {
        // Serialize without signature
        var json = JsonSerializer.Serialize(license, _serializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Sign
        var signature = _privateKey.SignData(bytes, HashAlgorithmName.SHA256);
        return Convert.ToBase64String(signature);
    }
}
```

### LicenseVerifier (FathomOS Client)

Location: `LicensingSystem.Client/LicenseVerifier.cs`

```csharp
public class LicenseVerifier
{
    private readonly ECDsa _publicKey;

    public LicenseVerifier(string publicKeyPem)
    {
        _publicKey = ECDsa.Create();
        _publicKey.ImportFromPem(publicKeyPem);
    }

    public bool VerifyLicense(OfflineLicense license)
    {
        // Store and remove signature
        var signature = Convert.FromBase64String(license.Signature);
        license.Signature = null;

        // Serialize and verify
        var json = JsonSerializer.Serialize(license, _serializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        return _publicKey.VerifyData(bytes, signature, HashAlgorithmName.SHA256);
    }
}
```

### LicenseSerializer

Location: `LicensingSystem.Shared/LicenseSerializer.cs`

```csharp
public static class LicenseSerializer
{
    // License file (.lic) - JSON format
    public static string ToJson(OfflineLicense license)
    {
        return JsonSerializer.Serialize(license, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    public static OfflineLicense FromJson(string json)
    {
        return JsonSerializer.Deserialize<OfflineLicense>(json, _options);
    }

    // License key string (FATHOM-XXX-...)
    public static string ToKeyString(OfflineLicense license)
    {
        var compressed = CompressAndEncode(license);
        return $"FATHOM-{GetEditionCode(license)}-{compressed}";
    }

    public static OfflineLicense FromKeyString(string key)
    {
        var parts = key.Split('-');
        var compressed = string.Join("-", parts.Skip(2));
        return DecodeAndDecompress(compressed);
    }
}
```

### MachineFingerprint

Location: `LicensingSystem.Client/MachineFingerprint.cs`

```csharp
public static class MachineFingerprint
{
    public static List<string> GetAllFingerprints()
    {
        var fingerprints = new List<string>();

        // CPU ID
        fingerprints.Add(GetCpuId());

        // BIOS Serial
        fingerprints.Add(GetBiosSerial());

        // Primary Disk Serial
        fingerprints.Add(GetDiskSerial());

        // Network Adapter MACs (physical adapters only)
        fingerprints.AddRange(GetMacAddresses());

        // Motherboard ID
        fingerprints.Add(GetMotherboardId());

        return fingerprints.Where(f => !string.IsNullOrEmpty(f)).ToList();
    }

    public static bool ValidateBinding(HardwareBinding binding)
    {
        var current = GetAllFingerprints();
        var matches = binding.Fingerprints.Count(f => current.Contains(f));
        return matches >= binding.MinMatching;
    }
}
```

---

## Storage

### License Storage

Location: `%LOCALAPPDATA%\FathomOS\License\`

| File | Content | Protection |
|------|---------|------------|
| `license.dat` | Encrypted license JSON | DPAPI |
| `license.bak` | Backup of previous license | DPAPI |

```csharp
public class LicenseStorage
{
    private readonly string _storagePath;

    public void StoreLicense(OfflineLicense license)
    {
        var json = JsonSerializer.Serialize(license);
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(json),
            null,
            DataProtectionScope.CurrentUser
        );
        File.WriteAllBytes(_storagePath, encrypted);
    }

    public OfflineLicense LoadLicense()
    {
        var encrypted = File.ReadAllBytes(_storagePath);
        var decrypted = ProtectedData.Unprotect(
            encrypted,
            null,
            DataProtectionScope.CurrentUser
        );
        var json = Encoding.UTF8.GetString(decrypted);
        return JsonSerializer.Deserialize<OfflineLicense>(json);
    }
}
```

### Local User Storage

Location: `%LOCALAPPDATA%\FathomOS\Data\users.db`

SQLite database with PBKDF2 password hashing:

```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT UNIQUE NOT NULL,
    PasswordHash TEXT NOT NULL,
    Salt TEXT NOT NULL,
    IsAdmin INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    LastLoginAt TEXT,
    FailedAttempts INTEGER NOT NULL DEFAULT 0,
    LockedUntil TEXT
);
```

```csharp
public class LocalUserManager
{
    public void CreateUser(string username, string password, bool isAdmin)
    {
        var salt = GenerateSalt(32);
        var hash = HashPassword(password, salt);

        // Store in SQLite
        _db.Execute(@"
            INSERT INTO Users (Username, PasswordHash, Salt, IsAdmin, CreatedAt)
            VALUES (@username, @hash, @salt, @isAdmin, @createdAt)",
            new { username, hash, salt, isAdmin, createdAt = DateTime.UtcNow });
    }

    private string HashPassword(string password, string salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            Convert.FromBase64String(salt),
            100_000,
            HashAlgorithmName.SHA256
        );
        return Convert.ToBase64String(pbkdf2.GetBytes(32));
    }
}
```

### Key Storage (License Generator UI)

Location: `%LOCALAPPDATA%\FathomOSLicenseManager\keys\`

| File | Content | Protection |
|------|---------|------------|
| `private.key` | ECDSA private key | DPAPI encrypted |
| `public.key` | ECDSA public key | Plain (exportable) |

---

## Validation Flow

### Startup Validation

```
Application Start
       │
       ▼
┌─────────────────┐
│ Load License    │
│ from Storage    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ Verify ECDSA    │────>│ Invalid?        │───> Show Activation
│ Signature       │     │ Prompt for      │     Window
└────────┬────────┘     │ new license     │
         │              └─────────────────┘
         │ Valid
         ▼
┌─────────────────┐     ┌─────────────────┐
│ Check Expiry    │────>│ Expired?        │───> Check Grace
│                 │     │                 │     Period
└────────┬────────┘     └─────────────────┘
         │                      │
         │ Not Expired          │ In Grace Period
         │                      │
         ▼                      ▼
┌─────────────────┐     ┌─────────────────┐
│ Check Hardware  │     │ Show Warning    │
│ Binding (if set)│     │ Banner          │
└────────┬────────┘     └────────┬────────┘
         │                       │
         │ Match                 │
         ▼                       ▼
┌─────────────────────────────────────────┐
│          License Valid                   │
│          Load Dashboard                  │
└─────────────────────────────────────────┘
```

### Module Access Check

```csharp
public bool CanAccessModule(string moduleId)
{
    if (_license == null) return false;
    if (!IsLicenseValid()) return false;

    return _license.Modules.Contains(moduleId) ||
           _license.Modules.Contains("*");  // Wildcard for all modules
}
```

---

## License Key Format

### Structure

```
FATHOM-[EDITION]-[SEGMENT1]-[SEGMENT2]-...-[CHECKSUM]

Example: FATHOM-PRO-A1B2-C3D4-E5F6-G7H8-I9J0
```

### Components

| Component | Description |
|-----------|-------------|
| FATHOM | Product identifier |
| EDITION | BAS (Basic), PRO (Professional), ENT (Enterprise) |
| SEGMENTS | Base32-encoded compressed license data |
| CHECKSUM | CRC32 checksum for validation |

### Encoding Process

1. Serialize license to minimal JSON
2. Compress with GZip
3. Encode with Base32 (no padding)
4. Split into 4-character segments
5. Calculate CRC32 checksum
6. Prepend edition code

---

## Error Codes

| Code | Message | Cause |
|------|---------|-------|
| `LIC001` | Invalid signature | License tampered or wrong public key |
| `LIC002` | License expired | Past expiration date and grace period |
| `LIC003` | Hardware mismatch | Fingerprints don't match binding |
| `LIC004` | Module not licensed | Requested module not in license |
| `LIC005` | Feature not enabled | Requested feature not in license |
| `LIC006` | License corrupted | Cannot parse license data |
| `LIC007` | Key format invalid | License key string malformed |

---

## API Reference

### ILicenseManager Interface

```csharp
public interface ILicenseManager
{
    // License Operations
    Task<LicenseResult> ActivateFromFileAsync(string filePath);
    Task<LicenseResult> ActivateFromKeyAsync(string licenseKey);
    bool IsLicenseValid();
    OfflineLicense? GetCurrentLicense();

    // Access Control
    bool HasModule(string moduleId);
    bool HasFeature(string featureId);
    string GetEdition();

    // Client Identity
    string GetClientName();
    string GetClientCode();
    string GetLicenseId();

    // Hardware
    List<string> GetHardwareFingerprints();

    // Status
    LicenseStatus GetStatus();
    int GetDaysUntilExpiry();
    bool IsInGracePeriod();
}
```

### LicenseResult

```csharp
public class LicenseResult
{
    public bool IsValid { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public OfflineLicense? License { get; set; }
}
```

### LicenseStatus

```csharp
public enum LicenseStatus
{
    Valid,
    Expired,
    InGracePeriod,
    HardwareMismatch,
    NotActivated,
    Invalid
}
```

---

## Security Considerations

### Private Key Protection

- Private key NEVER leaves License Generator UI
- Stored with DPAPI encryption
- Not transmitted to server
- Backup requires separate encryption password

### Public Key Embedding

- Public key embedded in FathomOS at build time
- Located in `LicensingSystem.Client/Resources/license-public-key.pem`
- Cannot be modified at runtime

### Tamper Resistance

- License signature covers all data
- DPAPI encryption for local storage
- Hardware binding prevents license sharing
- Clock tampering detected via certificate timestamps

### Offline Security

- No network communication for validation
- All cryptographic operations are local
- No telemetry or phone-home features

---

## Related Documentation

- [Licensing Overview](README.md) - End-user guide
- [Vendor Guide](VendorGuide.md) - License generation guide
- [Server Changes](../../Licensing%20Manager%20solution%20and%20server/FathomOS-LicenseManager-v3.4.9-source/LicensingSystem.Server/SERVER_CHANGES.md) - Server architecture

---

*Copyright 2026 Fathom OS. All rights reserved.*
