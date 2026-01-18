# Getting Started

This guide will help you set up the FathomOS License Management System.

## Prerequisites

1. **Install .NET 8.0 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify: `dotnet --version`

2. **Clone the Repository**
   ```bash
   git clone https://github.com/belalkandil0/FathomOS.git
   cd FathomOS
   ```

## Quick Start

### 1. Build the Solution

```bash
dotnet build LicensingSystem.sln -c Release
```

### 2. Start the Server

```bash
cd LicensingSystem.Server
dotnet run
```

The server will start at:
- **API:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger
- **Portal:** http://localhost:5000/portal

### 3. Set Up Admin Account

On first run, create an admin account:

```bash
curl -X POST http://localhost:5000/api/admin/auth/setup \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@company.com","password":"YourSecurePassword123!"}'
```

### 4. Run the Desktop UI

```bash
cd LicensingSystem.LicenseGeneratorUI
dotnet run
```

Or use the published executable.

### 5. Connect to Server

1. Open License Manager UI
2. Go to **Settings** page
3. Enter server URL: `http://localhost:5000`
4. Click **Test Connection**

## Next Steps

- [Deploy the Server](Server-Deployment) - Production deployment options
- [Generate Licenses](Generating-Licenses) - Create your first license
- [API Reference](API-Reference) - Full API documentation
