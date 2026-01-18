# License Generator - Vendor Guide

**Version:** 3.5.0
**Last Updated:** January 2026

## Overview

This guide is for vendors who generate and distribute FathomOS licenses. The License Generator UI is a standalone Windows application that creates cryptographically signed licenses for your customers.

---

## Key Concepts

### Offline-First Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         VENDOR SIDE                                  │
│  ┌─────────────────────┐     ┌─────────────────────┐                │
│  │ License Generator   │     │  Tracking Server    │                │
│  │ UI (Desktop App)    │────>│  (Optional)         │                │
│  │                     │     │                     │                │
│  │ - Generate keys     │     │ - License records   │                │
│  │ - Sign licenses     │     │ - Analytics         │                │
│  │ - Export .lic files │     │ - Cert verification │                │
│  └─────────────────────┘     └─────────────────────┘                │
│            │                                                         │
│            │ Export .lic file or license key                        │
│            ▼                                                         │
└─────────────────────────────────────────────────────────────────────┘
                              │
                    Email/USB/Download
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        CUSTOMER SIDE                                 │
│  ┌─────────────────────┐                                            │
│  │  FathomOS App       │                                            │
│  │                     │                                            │
│  │ - Load license      │  No server connection required!            │
│  │ - Verify signature  │                                            │
│  │ - Local admin acct  │                                            │
│  │ - Process data      │                                            │
│  └─────────────────────┘                                            │
└─────────────────────────────────────────────────────────────────────┘
```

### Your Responsibilities

1. Generate and manage ECDSA key pairs
2. Create licenses for customers
3. Distribute license files securely
4. Optionally sync licenses to tracking server

---

## First-Time Setup

### Step 1: Install License Generator UI

1. Download `FathomOSLicenseManager.exe`
2. Run the application
3. Accept any Windows security prompts

### Step 2: Generate ECDSA Key Pair

On first launch, generate your cryptographic keys:

1. Navigate to **Settings > Key Management**
2. Click **"Generate New Key Pair"**
3. The application creates an ECDSA P-256 key pair:
   - **Private Key**: Used to sign licenses (KEEP SECRET!)
   - **Public Key**: Embedded in FathomOS application

**Key Storage Location:**
```
%LOCALAPPDATA%\FathomOSLicenseManager\keys\
├── private.key    (DPAPI encrypted)
└── public.key     (exportable)
```

### Step 3: Export Public Key for FathomOS Build

The public key must be embedded in the FathomOS application:

1. Click **"Export Public Key"**
2. Save as `license-public-key.pem`
3. Provide to the FathomOS build team
4. The key is embedded in `LicensingSystem.Client`

### Step 4: Backup Your Private Key (Critical!)

1. Click **"Export Private Key (Encrypted)"**
2. Enter a strong backup password
3. Save the encrypted backup securely
4. Store in multiple secure locations

**WARNING**: Lost private keys cannot be recovered. All licenses signed with a lost key become unverifiable.

---

## Creating Licenses

### Step 1: Enter Customer Details

| Field | Description | Example |
|-------|-------------|---------|
| Client Name | Organization name | "Oceanic Surveys Ltd" |
| Client Code | 3-letter code | "OSL" |
| Email | Contact email | "admin@oceanicsurveys.com" |
| Edition | License tier | Professional |

### Step 2: Select Edition and Modules

**Editions:**
| Edition | Typical Modules |
|---------|-----------------|
| Basic | Survey Listing, Equipment Inventory |
| Professional | + Calibrations, Sound Velocity |
| Enterprise | + All modules, Priority support |

**Module Selection:**
- Check each module to include
- Or select edition preset
- Custom module combinations supported

### Step 3: Set License Terms

| Setting | Description |
|---------|-------------|
| Issue Date | When license becomes valid (default: today) |
| Expiration | When license expires |
| Grace Period | Days allowed after expiration (default: 7) |
| Duration Preset | 1 month, 1 year, 3 years, perpetual |

### Step 4: Hardware Binding (Optional)

For hardware-locked licenses:

1. Enable **"Hardware Binding"** checkbox
2. Paste customer's hardware fingerprints
3. Set minimum matching fingerprints (default: 3)

**Getting Customer Fingerprints:**
```csharp
// Customer runs this in FathomOS or diagnostic tool
var fingerprints = MachineFingerprint.GetAllFingerprints();
// Returns list of hardware IDs to paste into License Generator
```

### Step 5: Sign and Export

1. Click **"Generate License"**
2. The license is signed with your private key
3. Choose export format:
   - **License File (.lic)**: JSON file for easy distribution
   - **License Key**: Compact string for manual entry

---

## License Distribution

### Option A: Email License File

1. Export as `.lic` file
2. Email to customer
3. Customer loads file in FathomOS

### Option B: Provide License Key

1. Copy the generated license key string
2. Send via email, ticket, or portal
3. Customer enters key in FathomOS

### Best Practices

- Use secure channels for distribution
- Include customer name in file name: `FathomOS_OceanicSurveys_2026.lic`
- Keep records of issued licenses
- Set calendar reminders for renewals

---

## Server Integration (Optional)

### When to Use the Server

The tracking server is optional but useful for:
- Maintaining license records
- Analytics and reporting
- Public certificate verification
- Customer management

### Connecting to Server

1. Navigate to **Settings > Server Connection**
2. Enter server URL: `https://your-server.com`
3. Enter API key (from server console output)
4. Click **"Test Connection"**

### Syncing Licenses

**Automatic Sync:**
- Enable "Auto-sync on creation"
- Licenses sync immediately after generation

**Manual Sync:**
- Select licenses in the list
- Click "Sync to Server"
- Confirm sync completion

### API Key Authentication

The server uses API key authentication:

```http
POST /api/admin/licenses/sync
X-API-Key: your-api-key-here
Content-Type: application/json

{
  "licenseJson": "...",
  "clientName": "...",
  "clientCode": "..."
}
```

---

## Managing Existing Licenses

### Viewing License History

The License Generator maintains a local database of:
- All generated licenses
- Customer information
- Expiration dates
- Sync status

### Revoking a License

For server-synced licenses:

1. Select the license in the list
2. Click **"Revoke"**
3. Enter revocation reason
4. The revocation is recorded on the server

Note: Revocation only affects server-tracked features. Offline licenses continue to work until expiration (by design).

### Renewing/Extending

1. Find the existing license
2. Click **"Create Renewal"**
3. Adjust expiration date
4. Generate new license
5. Distribute to customer

---

## Key Management

### Key Rotation

Periodically rotating keys improves security:

1. Generate new key pair
2. Export new public key for FathomOS build
3. Keep old private key for verifying existing licenses
4. Issue new licenses with new key

### Multiple Keys

You can maintain multiple key pairs:
- Production keys (current)
- Legacy keys (for old licenses)
- Test keys (for development)

### Key Recovery

If you lose your private key:
1. Restore from encrypted backup
2. Enter backup password
3. Verify key restoration

If backup is lost:
- Generate new key pair
- Update FathomOS public key
- Reissue all active licenses

---

## Troubleshooting

### "Failed to sign license"

- Private key not loaded
- Key file corrupted
- Restart application and reload keys

### "Server sync failed"

- Check network connectivity
- Verify server URL
- Confirm API key is valid
- Check server is running

### "License validation failed"

- Public key in FathomOS doesn't match private key
- Ensure correct public key is embedded in build

### Customer reports "Invalid signature"

- License file may be corrupted during transfer
- Regenerate and resend license
- Verify customer has correct FathomOS version

---

## Quick Reference

### License File Format

```json
{
  "Id": "LIC-2026-001234",
  "Version": "3.5.0",
  "Client": { "Name": "...", "Code": "...", "Email": "..." },
  "Product": { "Name": "FathomOS", "Edition": "Professional" },
  "Terms": { "IssuedAt": "...", "ExpiresAt": "...", "GracePeriodDays": 7 },
  "Modules": ["SurveyListing", "GnssCalibration"],
  "Features": ["ExcelExport", "PdfExport"],
  "Binding": { "Fingerprints": ["..."], "MinMatching": 3 },
  "Signature": "BASE64_SIGNATURE",
  "PublicKeyId": "KEY001"
}
```

### License Key Format

```
FATHOM-PRO-A1B2-C3D4-E5F6-G7H8-I9J0-K1L2
       │    └──────────────────────────┘
       │           Encoded data + checksum
       Edition
```

### File Locations

| Path | Content |
|------|---------|
| `%LOCALAPPDATA%\FathomOSLicenseManager\keys\` | Cryptographic keys |
| `%LOCALAPPDATA%\FathomOSLicenseManager\settings.json` | Application settings |
| `%LOCALAPPDATA%\FathomOSLicenseManager\licenses.db` | License history |

---

## Related Documentation

- [Licensing Overview](README.md) - End-user documentation
- [Technical Reference](TechnicalReference.md) - Implementation details
- [Server Deployment](../../Licensing%20Manager%20solution%20and%20server/DEPLOYMENT_GUIDE.md) - Server setup

---

*Copyright 2026 Fathom OS. All rights reserved.*
