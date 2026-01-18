# FathomOS License Server - Render.com Deployment Guide

Complete guide to deploy the FathomOS License Server on Render.com using your GitHub repository.

## Prerequisites

- GitHub account with repository at `https://github.com/belalkandil0/FathomOS`
- Render.com account (https://dashboard.render.com)
- Code pushed to GitHub (see Git Push section below)

---

## Part 1: Push Code to GitHub

### If Git Push Failed (Permission Error)

Your machine has credentials for `EngBelal66` but you need to push to `belalkandil0/FathomOS`.

**Solution A: Add Collaborator**
1. Go to https://github.com/belalkandil0/FathomOS/settings/access
2. Click "Add people"
3. Add `EngBelal66` as a collaborator
4. Accept the invitation from the `EngBelal66` account
5. Run: `git push -u origin master`

**Solution B: Use Personal Access Token (PAT)**
1. Go to https://github.com/settings/tokens
2. Click "Generate new token (classic)"
3. Select scopes: `repo` (full control)
4. Copy the token
5. Run:
```bash
cd "C:\FATHOMOS\01_17_2026\FathomOS\Licensing Manager solution and server\FathomOS-LicenseManager-v3.4.9-source"
git remote set-url origin https://YOUR_TOKEN_HERE@github.com/belalkandil0/FathomOS.git
git push -u origin master
```

---

## Part 2: Deploy to Render.com

### Step 1: Create New Web Service

1. Log in to https://dashboard.render.com
2. Click **"New +"** button
3. Select **"Web Service"**

### Step 2: Connect GitHub Repository

1. Click **"Connect a repository"**
2. If not connected, click **"Configure account"** to authorize Render
3. Select `belalkandil0/FathomOS` repository
4. Click **"Connect"**

### Step 3: Configure Build Settings

Enter these settings:

| Setting | Value |
|---------|-------|
| **Name** | `fathom-os-license-server` |
| **Region** | Choose closest (e.g., Oregon or Frankfurt) |
| **Branch** | `master` |
| **Root Directory** | `Licensing Manager solution and server/FathomOS-LicenseManager-v3.4.9-source` |
| **Runtime** | `Docker` |
| **Instance Type** | Free (or Starter for production) |

### Step 4: Add Dockerfile

If Docker runtime is selected, Render will use the existing `Dockerfile` in the Server folder. If not present, create one:

**File: `LicensingSystem.Server/Dockerfile`**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared projects
COPY LicensingSystem.Shared/*.csproj ./LicensingSystem.Shared/
COPY LicensingSystem.Server/*.csproj ./LicensingSystem.Server/

# Restore
WORKDIR /src/LicensingSystem.Server
RUN dotnet restore

# Copy source
WORKDIR /src
COPY LicensingSystem.Shared/ ./LicensingSystem.Shared/
COPY LicensingSystem.Server/ ./LicensingSystem.Server/

# Build
WORKDIR /src/LicensingSystem.Server
RUN dotnet publish -c Release -o /app

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Create data directory
RUN mkdir -p /app/data

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "LicensingSystem.Server.dll"]
```

### Step 5: Environment Variables

In Render dashboard, go to **Environment** tab and add:

| Key | Value | Description |
|-----|-------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment |
| `ASPNETCORE_URLS` | `http://+:10000` | Render uses port 10000 |
| `CertificatePrivateKeyPem` | `-----BEGIN EC PRIVATE KEY-----...` | Your ECDSA private key |
| `CertificatePublicKeyPem` | `-----BEGIN PUBLIC KEY-----...` | Your ECDSA public key |
| `AdminEmail` | `admin@yourcompany.com` | Initial admin email |
| `AdminPassword` | `YourSecurePassword123!` | Initial admin password |

**To Generate ECDSA Keys:**
```bash
# Generate private key
openssl ecparam -genkey -name prime256v1 -noout -out private.pem

# Extract public key
openssl ec -in private.pem -pubout -out public.pem

# View keys (copy these to Render)
cat private.pem
cat public.pem
```

### Step 6: Persistent Storage (Optional - Skip for Free Tier)

> **FREE TIER NOTE:** Skip this step if using the free tier. The database will reset
> on each deploy, which is acceptable for testing/development. When ready for production,
> either add a Disk (paid) or migrate to a cloud database like Turso.

**For paid tier only:**
1. Go to **Disks** tab
2. Click **"Add Disk"**
3. Configure:
   - Name: `license-data`
   - Mount Path: `/app/data`
   - Size: 1 GB (minimum)

**Free tier implications:**
- Database resets when you redeploy or the service restarts
- Licenses created will be lost on restart
- Good for testing the system before production

### Step 7: Deploy

1. Click **"Create Web Service"**
2. Wait for build to complete (2-5 minutes)
3. Once deployed, your server is available at:
   - `https://fathom-os-license-server.onrender.com`

---

## Part 3: Verify Deployment

### Test Health Endpoint
```bash
curl https://fathom-os-license-server.onrender.com/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-01-17T..."
}
```

### Test Database Status
```bash
curl https://fathom-os-license-server.onrender.com/db-status
```

### View Swagger Documentation
Open in browser:
```
https://fathom-os-license-server.onrender.com/swagger
```

### Test Customer Portal
Open in browser:
```
https://fathom-os-license-server.onrender.com/portal
```

### Test Certificate Verification
Open in browser:
```
https://fathom-os-license-server.onrender.com/verify.html
```

---

## Part 4: Configure Desktop UI

### Connect Desktop App to Server

1. Open **FathomOS License Manager UI**
2. Go to **Settings** page
3. Enter Server URL: `https://fathom-os-license-server.onrender.com`
4. Click **Test Connection**
5. If successful, click **Save**

### Set Up Cryptographic Keys

**Important:** The Desktop UI and Server MUST use matching keys!

1. In Desktop UI, go to **Settings** → **Keys**
2. If you generated new keys for the server, paste the same keys here
3. Or generate keys in Desktop UI and copy to server environment variables

---

## Part 5: Create Your First License

### Using Desktop UI

1. Go to **Licenses** page
2. Click **+ New License**
3. Fill in:
   - Client Name: `Test Company`
   - Client Email: `test@company.com`
   - License Type: `Online`
   - Tier: `Professional`
   - Expiry: Set appropriate date
   - Modules: Select desired modules
4. Click **Create**
5. Copy the license key

### Using API

```bash
curl -X POST https://fathom-os-license-server.onrender.com/api/admin/licenses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{
    "customerName": "Test Company",
    "customerEmail": "test@company.com",
    "tier": "Professional",
    "expiresAt": "2027-01-17T00:00:00Z",
    "modules": ["SurveyListing", "GnssCalibration"]
  }'
```

---

## Part 6: Portals Included

### Customer Self-Service Portal
**URL:** `https://fathom-os-license-server.onrender.com/portal`

Features:
- License verification (key + email)
- View license status and expiration
- Transfer license to new device
- Deactivate current device
- View hardware fingerprint details

### Certificate Verification Portal
**URL:** `https://fathom-os-license-server.onrender.com/verify.html`

Features:
- Verify processing certificates by ID
- Shows certificate details if valid
- QR code for sharing verification link

---

## Part 7: Troubleshooting

### Build Fails
- Check Root Directory is correct
- Ensure all project files are pushed to GitHub
- Check Render build logs for errors

### Database Not Persisting
- Free tier doesn't persist storage
- Add a Disk in Render dashboard
- Mount to `/app/data`

### Keys Don't Match
- Desktop UI and Server must use identical ECDSA keys
- Generate once, use in both places
- Check for extra whitespace when copying

### Connection Refused
- Server may be sleeping (free tier sleeps after 15 min inactivity)
- First request takes ~30 seconds to wake up
- Consider Starter plan for always-on

### Rate Limited (429 Error)
- Wait 1 minute and retry
- Check if IP is blocked in admin dashboard
- Unblock via API if needed

---

## Quick Reference

| Endpoint | URL |
|----------|-----|
| Server Root | `https://fathom-os-license-server.onrender.com` |
| Health Check | `/health` |
| API Docs | `/swagger` |
| Customer Portal | `/portal` |
| Certificate Verify | `/verify.html` |
| License Certificate | `/certificate.html` |

| Desktop UI Action | Server API |
|-------------------|------------|
| Create License | `POST /api/admin/licenses` |
| Activate License | `POST /api/license/activate` |
| Validate License | `POST /api/license/validate` |
| Generate Offline License | `POST /api/admin/licenses/{id}/offline` |

---

## Support

- GitHub Issues: https://github.com/belalkandil0/FathomOS/issues
- Documentation: Check `docs/` folder in repository
