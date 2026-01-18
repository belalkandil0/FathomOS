# FathomOS Licensing Manager - Deployment Guide

**Version:** 3.5.0
**Last Updated:** January 2026

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Prerequisites](#2-prerequisites)
3. [Server Deployment Options](#3-server-deployment-options)
4. [Desktop UI Setup](#4-desktop-ui-setup)
5. [Initial Configuration](#5-initial-configuration)
6. [Generating Licenses](#6-generating-licenses)
7. [Security Configuration](#7-security-configuration)
8. [API Reference](#8-api-reference-quick-reference)
9. [Troubleshooting](#9-troubleshooting)
10. [File Locations Reference](#10-file-locations-reference)

---

## Important: Offline-First Architecture (v3.5.0)

**The licensing system has been redesigned for offline-first operation:**

| Component | Old Role (v3.4.x) | New Role (v3.5.0) |
|-----------|-------------------|-------------------|
| Server | Required for activation | **Optional** - tracking only |
| License Generator UI | Connected to server | **Standalone** - works offline |
| FathomOS Client | Online validation | **Offline validation** (ECDSA signatures) |
| Authentication | Username/password + 2FA | **API key** (for server) |
| User Accounts | Server-based | **Local** (in FathomOS) |

---

## 1. System Overview

The FathomOS Licensing Manager consists of three components:

### Server (ASP.NET Core 8.0) - OPTIONAL
- **Purpose:** License tracking and analytics (NOT validation)
- RESTful API for license record storage
- SQLite database for persistence
- Swagger UI for API documentation
- **Simple API key authentication** (no username/password)
- Public certificate verification portal

### Desktop License Manager UI (WPF .NET 8.0)
- **Standalone** Windows application for generating licenses
- ECDSA key management (P-256)
- **Offline license generation** (no server required)
- Optional server sync for tracking
- Module and tier management

### Client Library (LicensingSystem.Client)
- Integrates into FathomOS application
- **Offline license validation** using ECDSA signatures
- Hardware fingerprinting
- Local user account management
- Certificate generation (local) with optional sync

---

## 2. Prerequisites

### For Server Deployment

**Local/Docker:**
- .NET 8.0 SDK or Runtime
- Docker (optional, for containerized deployment)

**Cloud (Render.com):**
- GitHub account (for repository connection)
- Render.com account

### For Desktop UI

- Windows 10/11
- .NET 8.0 Desktop Runtime
- Visual Studio 2022 (for building from source)

### For Client Integration

- .NET 8.0
- Windows (for hardware fingerprinting)

---

## 3. Server Deployment Options

### Option A: Local Deployment

#### Step 1: Build the Server

```bash
# Navigate to the solution root
cd "FathomOS-LicenseManager-v3.4.9-source"

# Restore dependencies
dotnet restore LicensingSystem.Server/LicensingSystem.Server.csproj

# Build the project
dotnet build LicensingSystem.Server/LicensingSystem.Server.csproj -c Release
```

#### Step 2: Run Locally

```bash
# Run the server
dotnet run --project LicensingSystem.Server/LicensingSystem.Server.csproj

# Or run the published version
dotnet publish LicensingSystem.Server -c Release -o ./publish
cd publish
dotnet LicensingSystem.Server.dll
```

The server will start on:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001` (development only)

#### Step 3: Database Location

By default, the SQLite database (`licenses.db`) is created in:
- **Linux/Docker:** `/app/data/licenses.db`
- **Windows (local):** Current working directory or `licenses.db`

To specify a custom location, set the `DB_PATH` environment variable:

```bash
# Windows
set DB_PATH=C:\Data\licenses.db
dotnet run --project LicensingSystem.Server

# Linux/macOS
export DB_PATH=/var/data/licenses.db
dotnet run --project LicensingSystem.Server
```

#### Step 4: Configuration via appsettings.json

The `appsettings.json` file contains configuration for:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=licenses.db"
  },
  "Licensing": {
    "ProductName": "FathomOS",
    "PrivateKeyPem": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----",
    "CertificatePrivateKeyPem": "-----BEGIN EC PRIVATE KEY-----\n...\n-----END EC PRIVATE KEY-----",
    "CertificatePublicKeyPem": "-----BEGIN PUBLIC KEY-----\n...\n-----END PUBLIC KEY-----"
  }
}
```

**Important:** For production, move private keys to environment variables (see [Security Configuration](#7-security-configuration)).

---

### Option B: Docker Deployment

#### Step 1: Build the Docker Image

```bash
# Navigate to solution root (where Dockerfile is located)
cd "FathomOS-LicenseManager-v3.4.9-source"

# Build the image
docker build -t fathomos-license-server -f LicensingSystem.Server/Dockerfile .
```

#### Step 2: Run the Container

```bash
# Basic run
docker run -p 5000:5000 fathomos-license-server

# With persistent data volume
docker run -p 5000:5000 \
  -v $(pwd)/data:/app/data \
  fathomos-license-server

# With environment variables for keys (recommended)
docker run -p 5000:5000 \
  -v $(pwd)/data:/app/data \
  -e LICENSING_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----..." \
  -e CERTIFICATE_PRIVATE_KEY="-----BEGIN EC PRIVATE KEY-----..." \
  -e CERTIFICATE_PUBLIC_KEY="-----BEGIN PUBLIC KEY-----..." \
  fathomos-license-server
```

#### Step 3: Volume Mounts

| Mount Path | Purpose |
|------------|---------|
| `/app/data` | SQLite database and backups |

#### Step 4: Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `DB_PATH` | Database file path | No (default: `/app/data/licenses.db`) |
| `ASPNETCORE_ENVIRONMENT` | Environment (Production/Development) | No |
| `ASPNETCORE_URLS` | Bind URL | No (default: `http://+:5000`) |
| `LICENSING_PRIVATE_KEY` | ECDSA private key for signing | Recommended |
| `CERTIFICATE_PRIVATE_KEY` | Certificate private key | Recommended |
| `CERTIFICATE_PUBLIC_KEY` | Certificate public key | Recommended |

---

### Option C: Cloud Deployment (Render.com)

#### Step 1: Prepare Your Repository

1. Push the solution to a GitHub repository
2. Ensure `render.yaml` is in the repository root or `LicensingSystem.Server/` folder

#### Step 2: Connect to Render.com

1. Log in to [Render.com](https://render.com)
2. Click "New" > "Blueprint"
3. Connect your GitHub repository
4. Select the repository containing the code

#### Step 3: Configure render.yaml

The included `render.yaml` configures:

```yaml
services:
  - type: web
    name: fathomos-license-server
    env: docker
    dockerfilePath: ./LicensingSystem.Server/Dockerfile
    dockerContext: .
    plan: free  # Change to starter/standard for production
    healthCheckPath: /health
    envVars:
      - key: ASPNETCORE_ENVIRONMENT
        value: Production
      - key: DB_PATH
        value: /app/data/licenses.db
    disk:
      name: license-data
      mountPath: /app/data
      sizeGB: 1
```

#### Step 4: Set Environment Variables

In the Render dashboard, add these environment variables:

1. Go to your service > "Environment"
2. Add secret environment variables:
   - `LICENSING_PRIVATE_KEY`: Your ECDSA private key
   - `CERTIFICATE_PRIVATE_KEY`: Certificate signing key
   - `CERTIFICATE_PUBLIC_KEY`: Certificate public key

**Never store private keys in `render.yaml` or git!**

#### Step 5: Persistent Disk Configuration

The `render.yaml` includes a persistent disk:

```yaml
disk:
  name: license-data
  mountPath: /app/data
  sizeGB: 1  # Adjust as needed
```

This ensures your SQLite database persists across deployments.

---

## 4. Desktop UI Setup

### Building the Application

```bash
# Navigate to solution root
cd "FathomOS-LicenseManager-v3.4.9-source"

# Build the Desktop UI
dotnet build LicensingSystem.LicenseGeneratorUI/LicensingSystem.LicenseGeneratorUI.csproj -c Release

# Publish for distribution
dotnet publish LicensingSystem.LicenseGeneratorUI -c Release -o ./publish-ui --self-contained -r win-x64
```

### Published Output

After publishing, you will find:
- `FathomOSLicenseManager.exe` - Main executable
- Supporting DLLs and runtime files

### Configuring Server URL

On first run, the Desktop UI uses the default server URL:
```
https://fathom-os-license-server.onrender.com
```

To change this:
1. Launch the application
2. Go to Settings/Configuration
3. Update the "Server URL" field
4. Save settings

Settings are stored in:
```
%LOCALAPPDATA%\FathomOSLicenseManager\settings.json
```

### First-Time Setup: Generating ECDSA Keys

If no keys exist, the application will prompt you to generate them:

1. **Generate New Keys:**
   - Click "Generate Keys" in the Settings panel
   - The application creates ECDSA P-256 key pair
   - Private key is stored locally
   - Public key should be deployed to clients

2. **Import Existing Keys:**
   - If you have existing keys, you can import them
   - Supports PEM format

**Key Storage Location:**
```
%LOCALAPPDATA%\FathomOSLicenseManager\keys\
```

**Important:** Back up your private keys securely! Lost keys cannot be recovered.

---

## 5. Initial Configuration

### API Key Authentication (v3.5.0)

The server now uses **simple API key authentication** instead of username/password.

#### Getting Your API Key

**Option 1: Auto-Generated (First Run)**
1. Start the server
2. Check the console output for the API key
3. The key is displayed only once - save it immediately!

```
+==================================================================+
|  API Key (SAVE THIS - shown only once):                          |
|  abc123xyz-your-api-key-here                                     |
+==================================================================+
```

**Option 2: Environment Variable**
Set the `ADMIN_API_KEY` environment variable before starting:

```bash
# Linux/macOS
export ADMIN_API_KEY="your-secure-api-key-here"

# Windows PowerShell
$env:ADMIN_API_KEY = "your-secure-api-key-here"
```

#### Using the API Key

Include the key in the `X-API-Key` header for all admin requests:

```bash
curl -H "X-API-Key: your-api-key-here" \
  https://your-server/api/admin/licenses
```

### DEPRECATED: Username/Password Setup

The web setup wizard and username/password authentication from v3.4.x are deprecated. The endpoints remain for backward compatibility but return deprecation messages.

### Configuring Cryptographic Keys (Optional)

The server can optionally store cryptographic keys for certificate signing. However, the **License Generator UI is the primary tool** for key management.

Keys can be configured via environment variables:

```bash
export CERTIFICATE_PRIVATE_KEY="-----BEGIN EC PRIVATE KEY-----..."
export CERTIFICATE_PUBLIC_KEY="-----BEGIN PUBLIC KEY-----..."
```

### Creating License Tiers

```bash
# Get existing tiers
curl -H "X-API-Key: your-key" "http://localhost:5000/api/admin/tiers"

# Update tier modules via API:
curl -X PUT -H "X-API-Key: your-key" \
  -H "Content-Type: application/json" \
  "http://localhost:5000/api/admin/tiers/Professional/modules" \
  -d '{"moduleIds": ["SurveyListing", "TideAnalysis", "DataExport"]}'
```

### Creating Modules

```bash
# Add a new module
curl -X POST -H "X-API-Key: your-key" \
  -H "Content-Type: application/json" \
  "http://localhost:5000/api/admin/modules" \
  -d '{
    "moduleId": "SurveyListing",
    "displayName": "Survey Listing",
    "description": "Generate survey listing reports",
    "category": "Data Processing",
    "defaultTier": "Professional",
    "certificateCode": "SL"
  }'

# List all modules
curl -H "X-API-Key: your-key" "http://localhost:5000/api/admin/modules"
```

---

## 6. Generating Licenses

### Online Licenses

Online licenses require server connectivity for activation and periodic validation.

#### Step 1: Create License via Desktop UI

1. Open the License Manager UI
2. Fill in customer details:
   - Customer Email (required)
   - Customer Name
   - Edition (Basic/Professional/Enterprise)
   - Subscription Type (Monthly/Yearly/Lifetime)
   - Duration
3. Select modules/features
4. Click "Generate License"

#### Step 2: Create License via API

```bash
curl -X POST "http://localhost:5000/api/admin/licenses" \
  -H "Content-Type: application/json" \
  -d '{
    "customerEmail": "customer@example.com",
    "customerName": "John Doe",
    "edition": "Professional",
    "subscriptionType": "Yearly",
    "durationMonths": 12,
    "features": ["Tier:Professional", "Module:SurveyListing", "Module:TideAnalysis"],
    "brand": "Acme Corp",
    "licenseeCode": "AC"
  }'
```

#### Step 3: Activate in FathomOS Client

In your FathomOS application:

```csharp
var licenseManager = new LicenseManager("FathomOS", "https://your-server.com");

// Activate with license key
var result = await licenseManager.ActivateOnlineAsync(
    licenseKey: "XXXX-XXXX-XXXX-XXXX",
    email: "customer@example.com",
    appVersion: "1.0.0"
);

if (result.IsValid)
{
    Console.WriteLine($"Activated: {result.License.Edition}");
}
```

---

### Offline Licenses

Offline licenses are file-based and do not require server connectivity after installation.

#### When to Use Offline Licenses

- Air-gapped environments
- Restricted network access
- Vessel/offshore deployments
- Sites with unreliable internet

#### Step 1: Obtain Hardware ID from Client

On the client machine, run this code to get hardware fingerprints:

```csharp
var licenseManager = new LicenseManager("FathomOS");

// Get fingerprints for license generation
string fingerprints = licenseManager.GetHardwareFingerprintsForLicense();
Console.WriteLine("Copy these fingerprints to License Generator:");
Console.WriteLine(fingerprints);

// Output example:
// A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6
// B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7
// C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7R8
```

#### Step 2: Generate Offline License in Desktop UI

1. Open License Manager UI
2. Select "Offline License" mode
3. Fill in customer details
4. Paste the hardware fingerprints (one per line)
5. Click "Generate Offline License"
6. Save the `.lic` file

#### Step 3: Generate via API

```bash
curl -X POST "http://localhost:5000/api/admin/licenses" \
  -H "Content-Type: application/json" \
  -d '{
    "customerEmail": "customer@example.com",
    "customerName": "Offshore Team",
    "edition": "Enterprise",
    "subscriptionType": "Yearly",
    "durationMonths": 12,
    "licenseType": "Offline",
    "hardwareId": "A1B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6,B2C3D4E5F6G7H8I9J0K1L2M3N4O5P6Q7",
    "features": ["Tier:Enterprise", "Module:SurveyListing"]
  }'
```

The response includes `LicenseFileContent` - save this as a `.lic` file.

#### Step 4: Install Offline License on Client

```csharp
var licenseManager = new LicenseManager("FathomOS");

// Activate from file
var result = licenseManager.ActivateFromFile(@"C:\path\to\license.lic");

if (result.IsValid)
{
    Console.WriteLine($"Offline license activated: {result.License.Edition}");
    Console.WriteLine($"Expires: {result.License.ExpiresAt}");
}
else
{
    Console.WriteLine($"Activation failed: {result.Message}");
}
```

#### Hardware ID Requirements

- Minimum 3 matching fingerprints required (configurable)
- Fingerprints are based on: CPU ID, BIOS serial, MAC addresses, disk serial, motherboard ID
- Changes to hardware may require license reactivation

---

## 7. Security Configuration

### Moving Private Keys to Environment Variables

**Never store private keys in source control or configuration files!**

#### Linux/macOS:
```bash
# Add to ~/.bashrc or ~/.profile
export LICENSING_PRIVATE_KEY=$(cat /secure/path/private-key.pem)
export CERTIFICATE_PRIVATE_KEY=$(cat /secure/path/cert-private-key.pem)
export CERTIFICATE_PUBLIC_KEY=$(cat /secure/path/cert-public-key.pem)
```

#### Windows:
```powershell
# Set system environment variables
[Environment]::SetEnvironmentVariable(
    "LICENSING_PRIVATE_KEY",
    (Get-Content "C:\secure\private-key.pem" -Raw),
    "Machine"
)
```

#### Docker:
```bash
docker run -p 5000:5000 \
  -e LICENSING_PRIVATE_KEY="$(cat private-key.pem)" \
  fathomos-license-server
```

### CORS Configuration for Production

The default configuration allows all origins. For production, modify `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
            "https://yourapp.com",
            "https://admin.yourcompany.com"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});
```

### HTTPS Setup

#### For Render.com:
HTTPS is automatically handled by Render's proxy.

#### For Self-Hosted:
1. Obtain SSL certificate (Let's Encrypt recommended)
2. Configure in `appsettings.json`:
   ```json
   {
     "Kestrel": {
       "Endpoints": {
         "Https": {
           "Url": "https://0.0.0.0:5001",
           "Certificate": {
             "Path": "/path/to/cert.pfx",
             "Password": "certificate-password"
           }
         }
       }
     }
   }
   ```

#### Using Reverse Proxy (Recommended):
Use nginx or Traefik as reverse proxy with SSL termination.

### DEPRECATED: 2FA for Admin Accounts

**Note:** 2FA and username/password authentication are deprecated in v3.5.0. The server now uses API key authentication.

The 2FA endpoints remain for backward compatibility but return deprecation messages. If you need admin authentication, use the `X-API-Key` header instead.

---

## 8. API Reference (Quick Reference)

### Swagger UI

Access interactive API documentation at:
```
http://localhost:5000/swagger
```

### Key Endpoints

#### Health & Status
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check endpoint |
| `/` | GET | Server info and version |
| `/db-status` | GET | Database connection status |

#### License Operations (Public)
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/license/activate` | POST | Activate a license key |
| `/api/license/validate` | POST | Validate existing license |
| `/api/license/heartbeat` | POST | License heartbeat |
| `/api/license/revoked/{licenseId}` | GET | Check revocation status |
| `/api/license/time` | GET | Get server time |

#### Admin - Licenses
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/licenses` | GET | List all licenses |
| `/api/admin/licenses` | POST | Create new license |
| `/api/admin/licenses/{id}` | GET | Get license details |
| `/api/admin/licenses/{id}/revoke` | POST | Revoke a license |
| `/api/admin/licenses/{id}/reinstate` | POST | Reinstate a license |
| `/api/admin/licenses/{id}/extend` | POST | Extend expiration |
| `/api/admin/stats` | GET | Dashboard statistics |

#### Admin - Modules & Tiers
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/modules` | GET | List modules |
| `/api/admin/modules` | POST | Add module |
| `/api/admin/tiers` | GET | List tiers |
| `/api/admin/tiers/{tierId}/modules` | PUT | Update tier modules |

#### License Sync (v3.5.0)
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/licenses/sync` | POST | Sync a license record from UI |
| `/api/admin/licenses/sync-bulk` | POST | Bulk sync multiple licenses |
| `/api/admin/licenses/synced` | GET | List synced license records |
| `/api/admin/licenses/synced/{id}/revoke` | POST | Mark license as revoked |

#### Admin - Authentication (DEPRECATED)
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/auth/setup` | POST | ~~Initial admin setup~~ Returns deprecation message |
| `/api/admin/auth/login` | POST | ~~Admin login~~ Returns deprecation message |
| `/api/admin/auth/verify-2fa` | POST | ~~Verify 2FA code~~ Returns deprecation message |
| `/api/admin/auth/logout` | POST | ~~Logout~~ Returns deprecation message |

**Note:** Use `X-API-Key` header authentication instead of the deprecated auth endpoints.

---

## 9. Troubleshooting

### Common Issues and Solutions

#### Issue: "Database initialization error"

**Cause:** SQLite database cannot be created or accessed.

**Solution:**
1. Check file permissions on the data directory
2. Verify `DB_PATH` environment variable
3. Ensure the directory exists:
   ```bash
   mkdir -p /app/data
   chmod 777 /app/data
   ```

#### Issue: "License activation failed - Hardware mismatch"

**Cause:** Client hardware fingerprints don't match the license.

**Solution:**
1. Get current fingerprints from client:
   ```csharp
   var diag = licenseManager.DiagnoseHardwareMismatch();
   Console.WriteLine(diag);
   ```
2. Generate new offline license with correct fingerprints
3. For online licenses, reset activations via admin API

#### Issue: "Invalid signature" on offline license

**Cause:** License was signed with different key or file corrupted.

**Solution:**
1. Verify the public key in client matches server
2. Check license file wasn't modified
3. Regenerate the license

#### Issue: Server returns 429 "Too many requests"

**Cause:** Rate limiting triggered.

**Solution:**
1. Wait 15 minutes for rate limit to reset
2. For legitimate high-volume use, adjust rate limits in code

#### Issue: "Admin setup already completed" (DEPRECATED)

**Note:** Admin setup is no longer required in v3.5.0. Use API key authentication instead.

#### Issue: "API key not found" or "Unauthorized"

**Cause:** Missing or invalid API key.

**Solution:**
1. Check the `X-API-Key` header is included in the request
2. Verify the API key is correct (check server console output or `ADMIN_API_KEY` env var)
3. If you lost the auto-generated key, restart the server with `ADMIN_API_KEY` env var set

#### Issue: Desktop UI can't connect to server

**Solution:**
1. Verify server URL in settings
2. Check firewall/network connectivity
3. Test with: `curl http://server:5000/health`

### Diagnostic Commands

```bash
# Check server health
curl http://localhost:5000/health

# Check database status
curl http://localhost:5000/db-status

# Get server info
curl http://localhost:5000/

# List licenses (admin)
curl http://localhost:5000/api/admin/licenses

# Get stats
curl http://localhost:5000/api/admin/stats
```

---

## 10. File Locations Reference

### Solution Structure

```
FathomOS-LicenseManager-v3.4.9-source/
├── LicensingSystem.Server/           # ASP.NET Core server
│   ├── Controllers/                  # API controllers
│   │   ├── AdminController.cs        # License management API
│   │   ├── AdminAuthController.cs    # Authentication with 2FA
│   │   ├── LicenseController.cs      # Public license API
│   │   └── ...
│   ├── Services/                     # Business logic
│   │   ├── LicenseService.cs
│   │   ├── AuditService.cs
│   │   └── ...
│   ├── Data/                         # Database context
│   ├── Program.cs                    # Application entry point
│   ├── appsettings.json             # Configuration
│   ├── Dockerfile                    # Docker configuration
│   └── render.yaml                   # Render.com deployment
│
├── LicensingSystem.LicenseGeneratorUI/   # Desktop WPF application
│   ├── Views/                        # XAML views
│   ├── ViewModels/                   # MVVM view models
│   ├── Services/                     # Business services
│   │   ├── SettingsService.cs       # App settings
│   │   └── LicenseSigningService.cs # Signing logic
│   └── ...
│
├── LicensingSystem.Client/           # Client library
│   ├── LicenseManager.cs             # Main API
│   ├── LicenseValidator.cs           # Validation logic
│   ├── HardwareFingerprint.cs        # Hardware ID generation
│   └── LicenseStorage.cs             # Local storage
│
└── LicensingSystem.Shared/           # Shared models
    └── LicenseModels.cs              # DTOs and constants
```

### Runtime Paths

#### Server
| Path | Description |
|------|-------------|
| `/app/data/licenses.db` | SQLite database (Docker) |
| `./licenses.db` | SQLite database (local) |
| `/app/data/backups/` | Database backups |

#### Desktop UI (Windows)
| Path | Description |
|------|-------------|
| `%LOCALAPPDATA%\FathomOSLicenseManager\settings.json` | App settings |
| `%LOCALAPPDATA%\FathomOSLicenseManager\keys\` | ECDSA key storage |

#### Client (FathomOS Application)
| Path | Description |
|------|-------------|
| `%LOCALAPPDATA%\FathomOS\License\license.dat` | Stored license |
| `%LOCALAPPDATA%\FathomOS\License\revoked.dat` | Revocation list |

---

## Support

For issues and questions:
1. Check this deployment guide
2. Review server logs: `docker logs <container>`
3. Use diagnostic endpoints: `/db-status`, `/health`
4. Check Swagger UI for API documentation: `/swagger`

---

*Document generated for FathomOS Licensing Manager v3.4.9*
