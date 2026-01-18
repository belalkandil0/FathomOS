# FathomOS License Server v3.5.0 - Simplified Tracking Server

## Overview

The License Server is now **OPTIONAL** and used only for tracking purposes. License validation happens **OFFLINE** in the FathomOS application.

## New Role

The server now provides:
1. **License Record Storage** - Store copies of issued licenses (vendor records)
2. **Certificate Verification** - Public portal for verifying processing certificates
3. **License Tracking/Analytics** - Dashboard for monitoring license usage
4. **Customer Lookup** - Search and manage customer records

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
