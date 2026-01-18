# FathomOS Licensing Manager - Deployment Guide

**Version:** 3.4.9
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

## 1. System Overview

The FathomOS Licensing Manager consists of three components:

### Server (ASP.NET Core 8.0)
- RESTful API for license management
- SQLite database for persistence
- Swagger UI for API documentation
- Admin authentication with optional 2FA
- Rate limiting and audit logging

### Desktop License Manager UI (WPF .NET 8.0)
- Windows application for generating licenses
- ECDSA key management (P-256)
- Online and offline license generation
- Module and tier management

### Client Library (LicensingSystem.Client)
- Integrates into FathomOS application
- Supports online and offline license validation
- Hardware fingerprinting
- Certificate generation and sync

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

### Setting Up Admin Account (First Run)

On first server startup, no admin accounts exist. Create one:

```bash
# Using curl
curl -X POST "http://localhost:5000/api/admin/auth/setup" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@yourcompany.com",
    "username": "admin",
    "password": "YourSecurePassword123!",
    "displayName": "System Administrator"
  }'
```

Or using PowerShell:
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/admin/auth/setup" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{
    "email": "admin@yourcompany.com",
    "username": "admin",
    "password": "YourSecurePassword123!",
    "displayName": "System Administrator"
  }'
```

### Configuring Cryptographic Keys

Keys can be configured in three ways:

1. **appsettings.json (Development Only):**
   ```json
   {
     "Licensing": {
       "PrivateKeyPem": "-----BEGIN PRIVATE KEY-----\n...",
       "CertificatePrivateKeyPem": "-----BEGIN EC PRIVATE KEY-----\n...",
       "CertificatePublicKeyPem": "-----BEGIN PUBLIC KEY-----\n..."
     }
   }
   ```

2. **Environment Variables (Recommended for Production):**
   ```bash
   export LICENSING_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----..."
   export CERTIFICATE_PRIVATE_KEY="-----BEGIN EC PRIVATE KEY-----..."
   export CERTIFICATE_PUBLIC_KEY="-----BEGIN PUBLIC KEY-----..."
   ```

3. **Desktop UI:** Generate and manage keys through the UI.

### Creating License Tiers

```bash
# Get existing tiers
curl "http://localhost:5000/api/admin/tiers"

# Tiers are pre-configured in the database schema
# Update tier modules via API:
curl -X PUT "http://localhost:5000/api/admin/tiers/Professional/modules" \
  -H "Content-Type: application/json" \
  -d '{"moduleIds": ["SurveyListing", "TideAnalysis", "DataExport"]}'
```

### Creating Modules

```bash
# Add a new module
curl -X POST "http://localhost:5000/api/admin/modules" \
  -H "Content-Type: application/json" \
  -d '{
    "moduleId": "SurveyListing",
    "displayName": "Survey Listing",
    "description": "Generate survey listing reports",
    "category": "Data Processing",
    "defaultTier": "Professional",
    "certificateCode": "SL"
  }'

# List all modules
curl "http://localhost:5000/api/admin/modules"
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

### 2FA for Admin Accounts

#### Enable 2FA:

1. **Login first:**
   ```bash
   curl -X POST "http://localhost:5000/api/admin/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"username": "admin", "password": "YourPassword"}'
   ```

2. **Setup 2FA:**
   ```bash
   curl -X POST "http://localhost:5000/api/admin/auth/setup-2fa" \
     -H "Content-Type: application/json" \
     -d '{"sessionToken": "your-session-token"}'
   ```

   Response includes QR code URL for authenticator app.

3. **Confirm 2FA:**
   ```bash
   curl -X POST "http://localhost:5000/api/admin/auth/confirm-2fa" \
     -H "Content-Type: application/json" \
     -d '{"sessionToken": "your-session-token", "code": "123456"}'
   ```

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

#### Admin - Authentication
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/admin/auth/setup` | POST | Initial admin setup |
| `/api/admin/auth/login` | POST | Admin login |
| `/api/admin/auth/verify-2fa` | POST | Verify 2FA code |
| `/api/admin/auth/logout` | POST | Logout |

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

#### Issue: "Admin setup already completed"

**Cause:** Attempting to create initial admin when one exists.

**Solution:**
1. Login with existing admin credentials
2. If lost, delete `licenses.db` to reset (WARNING: loses all data)

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
