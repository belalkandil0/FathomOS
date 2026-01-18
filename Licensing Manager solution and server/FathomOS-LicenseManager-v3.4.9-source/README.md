# FathomOS License Management System

[![Build Status](https://github.com/belalkandil0/FathomOS/actions/workflows/build.yml/badge.svg)](https://github.com/belalkandil0/FathomOS/actions/workflows/build.yml)
[![License](https://img.shields.io/badge/license-Proprietary-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

A comprehensive license management system for FathomOS - offshore survey software platform.

## Components

| Component | Description | Technology |
|-----------|-------------|------------|
| **License Server** | REST API for license management | ASP.NET Core 8.0, SQLite |
| **License Manager UI** | Desktop application for administrators | WPF .NET 8.0 |
| **Client Library** | Integration library for FathomOS | .NET 8.0 |

## Features

### License Server
- Online and offline license generation
- Hardware fingerprint-based activation
- Session management (prevent concurrent use)
- Customer self-service portal
- Admin dashboard with 2FA authentication
- Comprehensive audit logging
- Rate limiting and IP blocking
- Health monitoring and alerts
- Database backup/restore
- Docker and cloud deployment ready

### License Manager UI
- Create, edit, and revoke licenses
- Generate offline license files (.lic)
- PDF certificate generation
- Module and tier management
- ECDSA key management
- Server administration

## Quick Start

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (for Desktop UI)
- Docker (optional, for server deployment)

### Build the Solution

```bash
cd FathomOS-LicenseManager-v3.4.9-source
dotnet build -c Release
```

### Run the Server Locally

```bash
cd LicensingSystem.Server
dotnet run
```

Server will be available at:
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/health

### Run the Desktop UI

```bash
cd LicensingSystem.LicenseGeneratorUI
dotnet run
```

Or use the published executable:
```bash
dotnet publish -c Release -p:PublishProfile=FolderProfile
# Output: LicensingSystem.LicenseGeneratorUI\bin\Publish\FathomOSLicenseManager.exe
```

## Deployment

### Docker Deployment

```bash
# Build
docker build -t fathomos-license-server .

# Run
docker run -d \
  -p 5000:5000 \
  -v ./data:/app/data \
  --name license-server \
  fathomos-license-server
```

### Cloud Deployment (Render.com)

The repository includes `render.yaml` for one-click deployment to Render.com.

See [DEPLOYMENT_GUIDE.md](../DEPLOYMENT_GUIDE.md) for detailed instructions.

## Configuration

### Server Configuration

Key settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=licenses.db"
  },
  "Licensing": {
    "ProductName": "FathomOS"
  }
}
```

**For production**, use environment variables for sensitive data:
- `LICENSING_PRIVATE_KEY` - License signing key
- `CERTIFICATE_PRIVATE_KEY` - Certificate signing key
- `CERTIFICATE_PUBLIC_KEY` - Public key for verification

### Desktop UI Configuration

Settings are stored in `%LocalAppData%\FathomOSLicenseManager\settings.json`:
- Server URL
- ECDSA keys for offline license signing

## API Documentation

Full API documentation is available at `/swagger` when the server is running.

### Key Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/license/activate` | POST | Activate a license |
| `/api/license/validate` | POST | Validate license status |
| `/api/admin/licenses` | GET/POST | Manage licenses |
| `/api/admin/licenses/{id}/offline` | POST | Generate offline license |
| `/api/portal/verify` | POST | Customer portal verification |

## Project Structure

```
FathomOS-LicenseManager-v3.4.9-source/
├── LicensingSystem.Server/          # License server API
│   ├── Controllers/                 # API endpoints
│   ├── Services/                    # Business logic
│   ├── Data/                        # Database context
│   └── wwwroot/                     # Customer portal
├── LicensingSystem.LicenseGeneratorUI/  # Desktop application
│   ├── Views/                       # WPF windows
│   ├── ViewModels/                  # MVVM view models
│   ├── Services/                    # UI services
│   └── Models/                      # Data models
├── LicensingSystem.Client/          # Client library
└── LicensingSystem.Shared/          # Shared models
```

## Security

- ECDSA P-256 digital signatures for licenses
- TOTP-based 2FA for admin authentication
- PBKDF2 password hashing (100,000 iterations)
- Rate limiting with IP blocking
- Hardware fingerprinting for license binding

## Support

- [Documentation](https://github.com/belalkandil0/FathomOS/wiki)
- [Report Issues](https://github.com/belalkandil0/FathomOS/issues)
- [Discussions](https://github.com/belalkandil0/FathomOS/discussions)

## License

Copyright (c) 2024-2026 S7 Solutions. All rights reserved.

This software is proprietary and confidential.
