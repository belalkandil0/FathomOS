# Fathom OS Equipment System - Production Deployment Guide
## Version 1.0

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Infrastructure Requirements](#infrastructure-requirements)
3. [Database Setup](#database-setup)
4. [API Server Deployment](#api-server-deployment)
5. [Mobile App Distribution](#mobile-app-distribution)
6. [Security Configuration](#security-configuration)
7. [Monitoring & Logging](#monitoring--logging)
8. [Backup & Recovery](#backup--recovery)
9. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Server Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 4 cores | 8 cores |
| RAM | 16 GB | 32 GB |
| Storage | 200 GB SSD | 500 GB SSD |
| OS | Windows Server 2019 | Windows Server 2022 |
| Network | 100 Mbps | 1 Gbps |

### Software Requirements

- **Windows Server 2019/2022** with IIS enabled
- **.NET 8.0 Runtime** (ASP.NET Core Hosting Bundle)
- **PostgreSQL 16** or **SQL Server 2022**
- **SSL Certificate** (Let's Encrypt or commercial)

### Network Requirements

| Port | Service | Direction |
|------|---------|-----------|
| 443 | HTTPS API | Inbound |
| 5432 | PostgreSQL | Internal |
| 1433 | SQL Server | Internal |

---

## Infrastructure Requirements

### Option A: Single Server (Small-Medium)

```
┌─────────────────────────────────────────┐
│           Windows Server 2022           │
│  ┌─────────────┐  ┌─────────────────┐  │
│  │   IIS       │  │  PostgreSQL/    │  │
│  │   + API     │  │  SQL Server     │  │
│  └─────────────┘  └─────────────────┘  │
│         │                 │             │
│         └────────┬────────┘             │
│                  │                      │
│            Local Storage                │
│         (uploads, logs)                 │
└─────────────────────────────────────────┘
```

### Option B: Distributed (Large Scale)

```
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Load        │───>│  API Server  │───>│  Database    │
│  Balancer    │    │  (Primary)   │    │  (Primary)   │
└──────────────┘    └──────────────┘    └──────────────┘
       │                   │                    │
       │            ┌──────────────┐    ┌──────────────┐
       └───────────>│  API Server  │───>│  Database    │
                    │  (Replica)   │    │  (Replica)   │
                    └──────────────┘    └──────────────┘
                           │
                    ┌──────────────┐
                    │  File        │
                    │  Storage     │
                    └──────────────┘
```

---

## Database Setup

### PostgreSQL Installation

1. **Download PostgreSQL 16** from https://www.postgresql.org/download/windows/

2. **Run installer** with options:
   - Install PostgreSQL Server
   - Install pgAdmin 4
   - Install Command Line Tools

3. **Configure postgresql.conf**:
   ```conf
   # Connection Settings
   listen_addresses = 'localhost'  # Or specific IP for remote access
   port = 5432
   max_connections = 200
   
   # Memory Settings (adjust based on RAM)
   shared_buffers = 4GB
   effective_cache_size = 12GB
   maintenance_work_mem = 1GB
   work_mem = 64MB
   
   # WAL Settings
   wal_buffers = 64MB
   checkpoint_completion_target = 0.9
   
   # Logging
   logging_collector = on
   log_directory = 'pg_log'
   log_filename = 'postgresql-%Y-%m-%d_%H%M%S.log'
   log_rotation_age = 1d
   log_rotation_size = 100MB
   ```

4. **Configure pg_hba.conf** for authentication:
   ```conf
   # Local connections
   local   all   all                   scram-sha-256
   # IPv4 local connections
   host    all   all   127.0.0.1/32    scram-sha-256
   # IPv4 API server connections (if on different server)
   host    all   all   10.0.0.0/24     scram-sha-256
   ```

5. **Create database and user**:
   ```sql
   -- Connect as postgres superuser
   CREATE USER s7fathom WITH PASSWORD 'your-secure-password-here';
   CREATE DATABASE s7fathom_equipment OWNER s7fathom;
   GRANT ALL PRIVILEGES ON DATABASE s7fathom_equipment TO s7fathom;
   
   -- Connect to the new database
   \c s7fathom_equipment
   
   -- Enable required extensions
   CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
   CREATE EXTENSION IF NOT EXISTS "pg_trgm";
   ```

6. **Initialize schema**:
   ```bash
   psql -U s7fathom -d s7fathom_equipment -f scripts/init-db.sql
   ```

### SQL Server Alternative

1. **Install SQL Server 2022**
2. **Create database**:
   ```sql
   CREATE DATABASE FathomOSEquipment;
   GO
   
   CREATE LOGIN s7fathom WITH PASSWORD = 'your-secure-password-here';
   GO
   
   USE FathomOSEquipment;
   CREATE USER s7fathom FOR LOGIN s7fathom;
   ALTER ROLE db_owner ADD MEMBER s7fathom;
   GO
   ```

---

## API Server Deployment

### Step 1: Install Prerequisites

1. **Install .NET 8.0 Hosting Bundle**:
   - Download from https://dotnet.microsoft.com/download/dotnet/8.0
   - Run `dotnet-hosting-8.0.x-win.exe`
   - Restart IIS: `iisreset`

2. **Install IIS Features**:
   ```powershell
   # Run as Administrator
   Install-WindowsFeature -Name Web-Server -IncludeManagementTools
   Install-WindowsFeature -Name Web-Asp-Net45
   Install-WindowsFeature -Name Web-WebSockets
   ```

### Step 2: Build and Publish API

```bash
# On development machine
cd api
dotnet publish -c Release -o ./publish

# Copy publish folder to server
# Target: C:\inetpub\wwwroot\s7fathom-api
```

### Step 3: Configure IIS

1. **Create Application Pool**:
   - Name: `FathomOSApiPool`
   - .NET CLR Version: `No Managed Code`
   - Managed Pipeline Mode: `Integrated`
   - Identity: `ApplicationPoolIdentity` or dedicated service account

2. **Create Website**:
   - Site name: `FathomOSApi`
   - Application pool: `FathomOSApiPool`
   - Physical path: `C:\inetpub\wwwroot\s7fathom-api`
   - Binding: HTTPS, port 443, hostname: `api.yourcompany.com`

3. **Set folder permissions**:
   ```powershell
   # Grant IIS_IUSRS write access to uploads and logs
   icacls "C:\inetpub\wwwroot\s7fathom-api\uploads" /grant "IIS_IUSRS:(OI)(CI)M"
   icacls "C:\inetpub\wwwroot\s7fathom-api\logs" /grant "IIS_IUSRS:(OI)(CI)M"
   ```

### Step 4: Configure Application

1. **Create appsettings.Production.json**:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=s7fathom_equipment;Username=s7fathom;Password=your-secure-password"
     },
     "DatabaseProvider": "PostgreSQL",
     "Jwt": {
       "Secret": "your-256-bit-secret-key-here-minimum-32-characters",
       "Issuer": "FathomOSApi",
       "Audience": "FathomOSClients",
       "AccessTokenExpirationMinutes": 15,
       "RefreshTokenExpirationDays": 7
     },
     "FileStorage": {
       "BasePath": "C:\\inetpub\\wwwroot\\s7fathom-api\\uploads",
       "MaxFileSizeMB": 10,
       "AllowedExtensions": [".jpg", ".jpeg", ".png", ".pdf"]
     },
     "Cors": {
       "AllowedOrigins": [
         "https://app.yourcompany.com",
         "capacitor://localhost",
         "ionic://localhost"
       ]
     },
     "Serilog": {
       "MinimumLevel": {
         "Default": "Information",
         "Override": {
           "Microsoft": "Warning",
           "System": "Warning"
         }
       }
     }
   }
   ```

2. **Set environment variable**:
   ```powershell
   # In IIS Application Pool Advanced Settings
   # Or in web.config
   ```

3. **Update web.config**:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <location path="." inheritInChildApplications="false">
       <system.webServer>
         <handlers>
           <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
         </handlers>
         <aspNetCore processPath="dotnet" 
                     arguments=".\FathomOS.Api.dll" 
                     stdoutLogEnabled="true" 
                     stdoutLogFile=".\logs\stdout" 
                     hostingModel="inprocess">
           <environmentVariables>
             <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
           </environmentVariables>
         </aspNetCore>
       </system.webServer>
     </location>
   </configuration>
   ```

### Step 5: SSL Certificate

1. **Option A: Let's Encrypt (Free)**:
   ```powershell
   # Install win-acme
   choco install win-acme
   
   # Run certificate wizard
   wacs.exe
   ```

2. **Option B: Commercial Certificate**:
   - Purchase from DigiCert, Comodo, etc.
   - Import in IIS Server Certificates
   - Bind to site

### Step 6: Verify Deployment

```powershell
# Test health endpoint
Invoke-WebRequest -Uri "https://api.yourcompany.com/health" -UseBasicParsing

# Test API
Invoke-WebRequest -Uri "https://api.yourcompany.com/api/lookups/categories" -UseBasicParsing
```

---

## Mobile App Distribution

### iOS (App Store / Enterprise)

1. **App Store Distribution**:
   - Enroll in Apple Developer Program ($99/year)
   - Configure app in App Store Connect
   - Build with production certificates
   - Submit for review

2. **Enterprise Distribution** (MDM):
   - Enroll in Apple Developer Enterprise Program ($299/year)
   - Build with enterprise certificate
   - Host IPA on internal server
   - Deploy via MDM (Intune, JAMF, etc.)

### Android (Play Store / Enterprise)

1. **Play Store Distribution**:
   - Create Google Play Developer account ($25 one-time)
   - Upload signed APK/AAB
   - Configure store listing
   - Submit for review

2. **Enterprise Distribution**:
   - Sign APK with release keystore
   - Host on internal server
   - Deploy via MDM or manual installation

### Build Commands

```bash
# iOS Production Build
cd mobile
npx react-native run-ios --configuration Release

# Android Production Build
cd android
./gradlew assembleRelease

# APK location: android/app/build/outputs/apk/release/
```

---

## Security Configuration

### Firewall Rules

```powershell
# Allow HTTPS
New-NetFirewallRule -DisplayName "Fathom OS API (HTTPS)" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow

# Block direct database access from internet
# (Only allow from API server IP)
```

### JWT Secret Generation

```powershell
# Generate secure 256-bit key
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

### Database Connection Security

- Use SSL/TLS for database connections
- Rotate passwords quarterly
- Use least-privilege accounts
- Enable audit logging

### API Security Checklist

- [ ] HTTPS only (redirect HTTP)
- [ ] Strong JWT secret (256-bit minimum)
- [ ] CORS configured for specific origins
- [ ] Rate limiting enabled
- [ ] Input validation on all endpoints
- [ ] SQL injection prevention (parameterized queries)
- [ ] XSS prevention (output encoding)
- [ ] Authentication on all sensitive endpoints
- [ ] Authorization checks (role-based)
- [ ] Audit logging enabled

---

## Monitoring & Logging

### Log Locations

| Log | Location | Retention |
|-----|----------|-----------|
| API Logs | `C:\inetpub\wwwroot\s7fathom-api\logs\` | 30 days |
| IIS Logs | `C:\inetpub\logs\LogFiles\` | 90 days |
| PostgreSQL | `C:\Program Files\PostgreSQL\16\data\pg_log\` | 30 days |
| Windows Events | Event Viewer | 90 days |

### Monitoring Endpoints

```http
GET /health              # Basic health check
GET /health/ready        # Readiness check
GET /health/live         # Liveness check
```

### Recommended Monitoring Tools

- **Application**: Application Insights, Serilog + Seq
- **Infrastructure**: Prometheus + Grafana, PRTG
- **Uptime**: Pingdom, UptimeRobot

### Alert Thresholds

| Metric | Warning | Critical |
|--------|---------|----------|
| CPU Usage | 70% | 90% |
| Memory Usage | 80% | 95% |
| Disk Space | 80% | 95% |
| Response Time | >500ms | >2000ms |
| Error Rate | >1% | >5% |

---

## Backup & Recovery

### Database Backup

**PostgreSQL Automated Backup Script** (`backup-db.ps1`):
```powershell
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "D:\Backups\PostgreSQL"
$pgDump = "C:\Program Files\PostgreSQL\16\bin\pg_dump.exe"

# Create backup
& $pgDump -h localhost -U s7fathom -Fc s7fathom_equipment > "$backupPath\s7fathom_$timestamp.dump"

# Remove backups older than 30 days
Get-ChildItem $backupPath -Filter "*.dump" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | 
    Remove-Item

# Upload to offsite storage (Azure Blob, AWS S3, etc.)
# azcopy copy "$backupPath\s7fathom_$timestamp.dump" "https://storage.blob.core.windows.net/backups/"
```

**Schedule with Task Scheduler**:
- Daily at 2:00 AM
- Run whether user is logged on or not

### File Backup

```powershell
# Backup uploads folder
$timestamp = Get-Date -Format "yyyyMMdd"
Compress-Archive -Path "C:\inetpub\wwwroot\s7fathom-api\uploads" -DestinationPath "D:\Backups\Files\uploads_$timestamp.zip"
```

### Recovery Procedures

**Database Recovery**:
```bash
# Restore from backup
pg_restore -h localhost -U s7fathom -d s7fathom_equipment -c backup_file.dump
```

**Full System Recovery**:
1. Provision new server
2. Install prerequisites
3. Restore database from backup
4. Deploy API application
5. Restore uploads folder
6. Update DNS/Load balancer
7. Verify functionality

---

## Troubleshooting

### Common Issues

#### API Not Starting

1. **Check logs**: `C:\inetpub\wwwroot\s7fathom-api\logs\`
2. **Check Event Viewer**: Application logs
3. **Verify .NET runtime**: `dotnet --list-runtimes`
4. **Test database connection**: 
   ```powershell
   Test-NetConnection -ComputerName localhost -Port 5432
   ```

#### Database Connection Failed

1. **Verify PostgreSQL is running**:
   ```powershell
   Get-Service postgresql*
   ```
2. **Test connection**:
   ```bash
   psql -h localhost -U s7fathom -d s7fathom_equipment
   ```
3. **Check pg_hba.conf** authentication settings

#### 502 Bad Gateway

1. **Check application pool** is running
2. **Verify process path** in web.config
3. **Check .NET Hosting Bundle** installation
4. **Review stdout logs**

#### Slow Performance

1. **Check database queries** with `EXPLAIN ANALYZE`
2. **Review API response times** in logs
3. **Check server resources** (CPU, RAM, Disk I/O)
4. **Optimize database indexes**

### Health Check Commands

```powershell
# API Health
Invoke-WebRequest -Uri "https://api.yourcompany.com/health" -UseBasicParsing

# Database Health
& "C:\Program Files\PostgreSQL\16\bin\pg_isready.exe" -h localhost -p 5432

# IIS Status
Get-IISSite
Get-IISAppPool

# Port Availability
Test-NetConnection -ComputerName localhost -Port 443
Test-NetConnection -ComputerName localhost -Port 5432
```

### Support Escalation

For issues not resolved by this guide:

1. Collect logs from all sources
2. Document steps to reproduce
3. Note server specifications and versions
4. Contact: support@s7fathom.com

---

**Document Version:** 1.0  
**Last Updated:** January 2025  
**Author:** Fathom OS DevOps Team
