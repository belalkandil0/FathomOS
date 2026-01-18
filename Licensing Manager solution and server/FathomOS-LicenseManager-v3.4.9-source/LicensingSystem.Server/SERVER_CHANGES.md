# FathomOS License Server v3.5.0 - Simplified Tracking Server

## Overview

The License Server is now **OPTIONAL** and used only for tracking purposes. License validation happens **OFFLINE** in the FathomOS application using ECDSA digital signatures.

## Architecture Change Summary

```
┌─────────────────────────────────────────────────────────────────────┐
│                        OLD ARCHITECTURE (v3.4.x)                     │
│                                                                      │
│  FathomOS App ──────> License Server ──────> Validate & Respond      │
│       │                     │                                        │
│       │              (Server Required)                               │
│       │                     │                                        │
│       └── Must be online ──┘                                         │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                        NEW ARCHITECTURE (v3.5.0)                     │
│                                                                      │
│  License Generator UI                                                │
│       │                                                              │
│       ├── Sign license (ECDSA private key)                          │
│       ├── Export .lic file ─────────────> Customer                   │
│       │                                       │                      │
│       └── (Optional) Sync to Server           │                      │
│                 │                             │                      │
│                 ▼                             ▼                      │
│          Tracking Server              FathomOS App                   │
│          (Analytics only)             (Offline validation)           │
│                                             │                        │
│                                   Verify with public key             │
│                                   (No server needed!)                │
└─────────────────────────────────────────────────────────────────────┘
```

## New Role

The server now provides:
1. **License Record Storage** - Store copies of issued licenses (vendor records)
2. **Certificate Verification** - Public portal for verifying processing certificates
3. **License Tracking/Analytics** - Dashboard for monitoring license usage
4. **Customer Lookup** - Search and manage customer records

**The server does NOT:**
- Validate licenses (done offline in FathomOS)
- Issue licenses (done by License Generator UI)
- Manage user accounts (done locally in FathomOS)
- Require setup wizards or complex authentication

## Key Changes

### Authentication: API Key (replaces Username/Password)

**Before (v3.4.9):**
- Complex setup wizard with tokens
- Username/password authentication
- Session management with 2FA

**After (v3.5.0):**
- Simple API key authentication
- Key generated automatically on first run
- Or use `ADMIN_API_KEY` environment variable

### Getting Your API Key

1. **First Run**: Check the console output - the API key is displayed once
2. **Environment Variable**: Set `ADMIN_API_KEY` before starting the server
3. **Database**: Key hash stored in `ApiKeys` table

### Using the API Key

Include in the `X-API-Key` header for all admin requests:

```bash
curl -H "X-API-Key: your-api-key-here" https://your-server/api/admin/licenses
```

## Endpoints

### Public Endpoints (No authentication required)

| Endpoint | Description |
|----------|-------------|
| `GET /health` | Server health status |
| `GET /db-status` | Database connection status |
| `GET /api/certificates/verify/{id}` | Verify a processing certificate |
| `GET /api/license/*` | License endpoints for FathomOS client |

### Protected Endpoints (Require X-API-Key header)

| Endpoint | Description |
|----------|-------------|
| `POST /api/admin/licenses/sync` | Sync a license record from License Generator UI |
| `POST /api/admin/licenses/sync-bulk` | Bulk sync multiple license records |
| `GET /api/admin/licenses/synced` | List all synced license records |
| `POST /api/admin/licenses/synced/{id}/revoke` | Mark a license as revoked |
| `GET /api/admin/licenses` | List all licenses |
| `GET /api/admin/stats` | Dashboard statistics |

## Configuration

### Environment Variables

| Variable | Description |
|----------|-------------|
| `ADMIN_API_KEY` | Optional. If set, uses this as the API key (instead of generating one) |
| `DB_PATH` | Optional. Path to the SQLite database file |

### Example Docker Compose

```yaml
services:
  license-server:
    image: fathomos/license-server:3.5.0
    environment:
      - ADMIN_API_KEY=your-secure-api-key-here
      - DB_PATH=/app/data/licenses.db
    volumes:
      - license-data:/app/data
    ports:
      - "5000:5000"

volumes:
  license-data:
```

## Console Output (First Run)

```
+==================================================================+
|                 FathomOS License Server v3.5.0                  |
+==================================================================+
|  Status: Running                                                |
|  Mode:   License Tracking (validation is offline)               |
|                                                                  |
|  Endpoints:                                                      |
|    * POST /api/admin/licenses/sync - Sync license records       |
|    * GET  /api/admin/licenses - List licenses                   |
|    * GET  /api/certificates/verify/{id} - Public verification   |
|                                                                  |
|  Protected endpoints require X-API-Key header                   |
|                                                                  |
+------------------------------------------------------------------+
|  API Key (SAVE THIS - shown only once):                          |
|  abc123xyz-your-api-key-here                                    |
+------------------------------------------------------------------+
|  Use this key in License Generator UI to connect.               |
+==================================================================+
```

## Migration from v3.4.9

1. No setup wizard required
2. Delete any `admin_users` records (no longer used)
3. API key will be generated automatically on first run
4. Update License Generator UI to use X-API-Key header

## Database Schema Changes

### New Tables

**ApiKeys**
- `Id` (int, PK)
- `KeyHash` (string) - SHA256 hash of API key
- `KeyHint` (string) - Last 4 chars for identification
- `CreatedAt` (datetime)
- `LastUsedAt` (datetime)
- `IsActive` (bool)

**SyncedLicenses**
- `Id` (int, PK)
- `LicenseId` (string, unique)
- `ClientName` (string)
- `ClientCode` (string)
- `Edition` (string)
- `LicenseJson` (text) - Full license data
- `IssuedAt` (datetime)
- `ExpiresAt` (datetime)
- `SyncedAt` (datetime)
- `IsRevoked` (bool)
- `RevokedReason` (string)
- `CustomerEmail` (string)
- `Features` (string)
- `Brand` (string)

## Deprecated Components

The following are deprecated and return compatibility messages:
- SetupMiddleware - No longer blocks requests
- SetupService - Returns "setup complete"
- SetupController - Returns deprecation messages
- AdminAuthController - Returns deprecation messages

These are kept for backward compatibility with existing integrations.

## Comparison: Old vs New

| Feature | v3.4.x (Old) | v3.5.0 (New) |
|---------|--------------|--------------|
| License Validation | Server-based | **Offline (ECDSA)** |
| Server Required | Yes | **No (optional)** |
| Authentication | Username/password + 2FA | **API key** |
| Setup | Web wizard required | **Auto-configured** |
| User Accounts | Server-managed | **Local in FathomOS** |
| License Generator | Server-connected | **Standalone** |
| Internet Required | Always | **Never (for validation)** |

## Benefits of New Architecture

1. **Offline-First**: Perfect for offshore, vessel, and air-gapped deployments
2. **Simpler Setup**: No web wizards, no account creation, just an API key
3. **More Secure**: Private keys never leave the License Generator UI
4. **More Reliable**: No single point of failure (server downtime doesn't block users)
5. **Faster**: No network round-trips for validation

## License Generator UI Changes

The Desktop UI now:
- Works completely offline
- Generates and manages ECDSA key pairs locally
- Signs licenses without server connection
- Optionally syncs license records to server for tracking

## FathomOS Client Changes

The FathomOS application now:
- Validates licenses locally using embedded public key
- Stores licenses in DPAPI-encrypted local storage
- Manages local user accounts (no server accounts)
- Optionally syncs certificates to server

## Migration Checklist

1. [ ] Update License Generator UI to v3.5.0
2. [ ] Generate ECDSA key pair in License Generator UI
3. [ ] Export public key and embed in FathomOS build
4. [ ] (Optional) Deploy tracking server with API key
5. [ ] (Optional) Configure License Generator UI to sync to server
6. [ ] Inform customers about new offline license files

## Related Documentation

- [Deployment Guide](../../../DEPLOYMENT_GUIDE.md)
- [Vendor Guide](../../../../Documentation/Licensing/VendorGuide.md)
- [Technical Reference](../../../../Documentation/Licensing/TechnicalReference.md)
