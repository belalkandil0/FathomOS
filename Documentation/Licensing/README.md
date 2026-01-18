# FathomOS Licensing System

**Version:** 3.5.0
**Last Updated:** January 2026

## Overview

FathomOS uses an **offline-first licensing system** based on ECDSA digital signatures. This approach allows:

- License validation without internet connectivity
- Ideal for offshore, vessel, and air-gapped environments
- Cryptographically secure license verification
- Optional server integration for tracking and analytics

---

## For End Users (FathomOS Application)

### Obtaining a License

Contact your vendor to receive one of the following:

| Format | Description | Example |
|--------|-------------|---------|
| License File | `.lic` file containing JSON license data | `FathomOS_License_2026.lic` |
| License Key | Compact string representation | `FATHOM-PRO-A1B2-C3D4-E5F6` |

### Activating Your License

#### Option 1: License File

1. Launch FathomOS
2. Click **"Load License File"** on the activation screen
3. Browse to and select your `.lic` file
4. The license is verified automatically using the embedded signature

#### Option 2: License Key

1. Launch FathomOS
2. Click **"Enter License Key"** on the activation screen
3. Enter your license key string
4. Click **"Activate"**

### Creating Your Local Account

After successful license activation:

1. Create your local administrator account
2. Enter a username and password
3. This account is stored locally on your machine
4. No server connection required

### Starting Your Work

Once activated:
1. Log in with your local account
2. Access licensed modules from the dashboard
3. Create projects and process data
4. Generate certificates and reports

---

## License Formats

### License File (.lic)

License files are JSON documents containing:

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
  "Modules": ["SurveyListing", "GnssCalibration", "UsblVerification"],
  "Features": ["ExcelExport", "PdfExport", "3DVisualization"],
  "Signature": "BASE64_ECDSA_SIGNATURE",
  "PublicKeyId": "KEY001"
}
```

### License Key String

License keys are compact representations:

```
FATHOM-PRO-A1B2-C3D4-E5F6-G7H8-I9J0
```

Components:
- `FATHOM` - Product identifier
- `PRO` - Edition (BAS/PRO/ENT)
- Remaining segments - Encoded license data and checksum

---

## Offline Validation

### How It Works

1. **At Build Time**: FathomOS embeds the vendor's ECDSA public key
2. **At Activation**: FathomOS reads the license file or decodes the key string
3. **Signature Verification**: The license signature is verified using the embedded public key
4. **Local Storage**: Valid licenses are stored locally in encrypted form
5. **Runtime Checks**: License validity checked on each application start

### Security Features

| Feature | Description |
|---------|-------------|
| ECDSA P-256 | Industry-standard elliptic curve cryptography |
| SHA-256 Hashing | Secure hash algorithm for signature generation |
| DPAPI Encryption | Windows Data Protection API for local storage |
| Tamper Detection | License integrity verified on every check |

### No Internet Required

- License validation happens entirely on the local machine
- No license server communication needed
- Works in air-gapped environments
- Ideal for offshore and vessel deployments

---

## License Contents

Each license specifies:

| Field | Description |
|-------|-------------|
| Client Name | Organization name (appears on reports) |
| Client Code | 3-letter code (used in certificate IDs) |
| Edition | Basic, Professional, or Enterprise |
| Modules | List of licensed module identifiers |
| Features | List of enabled feature flags |
| Expiration | License validity end date |
| Grace Period | Days of operation after expiration (default: 7) |
| Hardware Binding | Optional machine fingerprints |

---

## Hardware Binding (Optional)

Some licenses include hardware binding for additional security:

### How Hardware Binding Works

1. **Fingerprint Generation**: FathomOS generates fingerprints from:
   - CPU identifier
   - BIOS serial number
   - Primary disk serial
   - Network adapter MAC addresses
   - Motherboard identifier

2. **Matching**: License requires 3+ fingerprints to match (configurable)

3. **Flexibility**: Allows for some hardware changes without invalidation

### When Hardware Changes

If you replace significant hardware:
1. Contact your vendor with your new hardware fingerprints
2. Request a replacement license
3. Activate the new license file

---

## License Expiration

### Before Expiration

FathomOS displays warnings starting 30 days before expiration.

### Grace Period

After expiration, a grace period allows continued operation:
- Default: 7 days
- Full functionality available
- Warning banners displayed
- Contact vendor for renewal

### After Grace Period

- Application launches in read-only mode
- Existing data accessible
- Processing disabled
- Contact vendor to renew

---

## Troubleshooting

### "Invalid License Signature"

- License file may be corrupted
- License was not issued by authorized vendor
- Request a new license file from vendor

### "License Expired"

- Check system date is correct
- Contact vendor for license renewal
- Grace period may still be active

### "Hardware Mismatch"

- Significant hardware changes detected
- Contact vendor with new fingerprints
- Request replacement license

### "Module Not Licensed"

- Your license tier does not include this module
- Contact vendor about upgrading
- Check licensed modules in About dialog

---

## Related Documentation

- [Getting Started](../GettingStarted.md) - Installation and activation guide
- [Vendor Guide](VendorGuide.md) - For license vendors
- [Technical Reference](TechnicalReference.md) - Implementation details

---

*Copyright 2026 Fathom OS. All rights reserved.*
