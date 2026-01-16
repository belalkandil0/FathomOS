# Fathom OS - Desktop Module Integration Handoff
## For Integration with Mobile App & Central API

---

## 🎯 Purpose

This document provides everything needed to integrate the existing Desktop Module (WPF .NET 8) with the new Central API Server and ensure seamless data synchronization with the Mobile Application.

---

## 📋 Integration Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        INTEGRATION ARCHITECTURE                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐      │
│  │  Desktop Module  │    │   Mobile App     │    │  Other Clients   │      │
│  │  (WPF .NET 8)    │    │  (React Native)  │    │  (Future)        │      │
│  └────────┬─────────┘    └────────┬─────────┘    └────────┬─────────┘      │
│           │                       │                       │                 │
│           │    REST API (HTTPS)   │                       │                 │
│           └───────────────────────┼───────────────────────┘                 │
│                                   │                                         │
│                                   ▼                                         │
│                    ┌──────────────────────────────┐                         │
│                    │     CENTRAL API SERVER       │                         │
│                    │     (ASP.NET Core 8.0)       │                         │
│                    │                              │                         │
│                    │  • JWT Authentication        │                         │
│                    │  • Role-Based Authorization  │                         │
│                    │  • Delta Sync Endpoints      │                         │
│                    │  • Conflict Resolution       │                         │
│                    └──────────────┬───────────────┘                         │
│                                   │                                         │
│                                   ▼                                         │
│                    ┌──────────────────────────────┐                         │
│                    │     CENTRAL DATABASE         │                         │
│                    │  (PostgreSQL / SQL Server)   │                         │
│                    └──────────────────────────────┘                         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 📁 Files to Provide to Desktop Team

### Essential Files (MUST HAVE)

| # | File | Purpose | Priority |
|---|------|---------|----------|
| 1 | `DESKTOP_INTEGRATION_HANDOFF.md` | This document | Critical |
| 2 | `docs/API_DOCUMENTATION.md` | Complete API reference | Critical |
| 3 | `api/src/FathomOS.Api/DTOs/DataDTOs.cs` | All request/response models | Critical |
| 4 | `api/src/FathomOS.Core/Entities/*.cs` | Domain entity definitions | Critical |
| 5 | `api/src/FathomOS.Core/Enums/Enums.cs` | Status/Condition enumerations | Critical |
| 6 | `api/scripts/init-db.sql` | Database schema | Critical |

### Reference Files (HELPFUL)

| # | File | Purpose |
|---|------|---------|
| 7 | `SYSTEM_ARCHITECTURE.md` | Full system design |
| 8 | `docs/SECURITY_BEST_PRACTICES.md` | Security implementation |
| 9 | `api/src/FathomOS.Api/Controllers/*.cs` | API implementation reference |
| 10 | `api/src/FathomOS.Infrastructure/Services/AuthService.cs` | Auth implementation |

---

## 🔐 Authentication Integration

### JWT Token Flow

```
Desktop App                          API Server
    │                                    │
    │  POST /api/auth/login              │
    │  { username, password }            │
    │ ──────────────────────────────────>│
    │                                    │
    │  { accessToken, refreshToken,      │
    │    expiresIn, user }               │
    │ <──────────────────────────────────│
    │                                    │
    │  GET /api/equipment                │
    │  Authorization: Bearer {token}     │
    │ ──────────────────────────────────>│
    │                                    │
    │  { equipment data }                │
    │ <──────────────────────────────────│
    │                                    │
    │  (Token expires after 15 min)      │
    │                                    │
    │  POST /api/auth/refresh            │
    │  { refreshToken }                  │
    │ ──────────────────────────────────>│
    │                                    │
    │  { newAccessToken, newRefresh }    │
    │ <──────────────────────────────────│
```

### Login Request/Response

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
{
  "username": "john.smith",
  "password": "SecurePassword123"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 900,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "john.smith",
    "email": "john.smith@company.com",
    "firstName": "John",
    "lastName": "Smith",
    "roles": ["Base Manager", "Store Keeper"],
    "permissions": ["equipment.view", "equipment.create", "manifest.approve"],
    "defaultLocationId": "550e8400-e29b-41d4-a716-446655440001"
  }
}
```

### Required Headers for All Authenticated Requests

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json
X-Device-Id: desktop-{machine-guid}
X-App-Version: 1.0.0
```

### Token Storage Recommendation (Desktop)

```csharp
// Store tokens securely using Windows Credential Manager
using System.Security.Cryptography;
using Windows.Security.Credentials;

public class TokenStorage
{
    private const string ResourceName = "FathomOSEquipment";
    
    public void StoreTokens(string accessToken, string refreshToken)
    {
        var vault = new PasswordVault();
        vault.Add(new PasswordCredential(ResourceName, "AccessToken", accessToken));
        vault.Add(new PasswordCredential(ResourceName, "RefreshToken", refreshToken));
    }
    
    public (string accessToken, string refreshToken) GetTokens()
    {
        var vault = new PasswordVault();
        var accessCred = vault.Retrieve(ResourceName, "AccessToken");
        var refreshCred = vault.Retrieve(ResourceName, "RefreshToken");
        accessCred.RetrievePassword();
        refreshCred.RetrievePassword();
        return (accessCred.Password, refreshCred.Password);
    }
}
```

---

## 🔄 Sync Protocol

### Delta Sync Flow

The sync protocol uses **SyncVersion** (a monotonically increasing integer) to track changes.

```
Desktop App                          API Server
    │                                    │
    │  Store lastSyncVersion = 0         │
    │                                    │
    │  POST /api/sync/pull               │
    │  { deviceId, lastSyncVersion: 0,   │
    │    tables: ["Equipment",...] }     │
    │ ──────────────────────────────────>│
    │                                    │
    │  { newSyncVersion: 1250,           │
    │    changes: { equipment: [...],    │
    │               manifests: [...] }}  │
    │ <──────────────────────────────────│
    │                                    │
    │  Apply changes to local DB         │
    │  Update lastSyncVersion = 1250     │
    │                                    │
    │  (User makes local changes)        │
    │                                    │
    │  POST /api/sync/push               │
    │  { deviceId, changes: [...] }      │
    │ ──────────────────────────────────>│
    │                                    │
    │  { applied: 5, conflicts: [...] }  │
    │ <──────────────────────────────────│
    │                                    │
    │  (5 minutes later)                 │
    │                                    │
    │  POST /api/sync/pull               │
    │  { deviceId, lastSyncVersion: 1250 }│
    │ ──────────────────────────────────>│
    │                                    │
    │  { newSyncVersion: 1275,           │
    │    changes: { equipment: [3 new] }}│
    │ <──────────────────────────────────│
```

### Pull Request

**Endpoint:** `POST /api/sync/pull`

```json
{
  "deviceId": "desktop-{machine-guid}",
  "lastSyncVersion": 12345,
  "tables": ["Equipment", "Manifests", "ManifestItems", "Locations", "Categories", "Projects"]
}
```

### Pull Response

```json
{
  "newSyncVersion": 12400,
  "changes": {
    "equipment": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440002",
        "operation": "Insert",
        "data": {
          "id": "550e8400-e29b-41d4-a716-446655440002",
          "assetNumber": "EQ-2024-00125",
          "name": "ROV Camera System",
          "status": "Available",
          "currentLocationId": "...",
          "syncVersion": 12350
        },
        "syncVersion": 12350
      },
      {
        "id": "550e8400-e29b-41d4-a716-446655440003",
        "operation": "Update",
        "data": { ... },
        "syncVersion": 12375
      },
      {
        "id": "550e8400-e29b-41d4-a716-446655440004",
        "operation": "Delete",
        "data": { "id": "...", "isActive": false },
        "syncVersion": 12380
      }
    ],
    "manifests": [...],
    "locations": [...],
    "categories": [...],
    "projects": [...]
  },
  "hasMore": false
}
```

### Push Request

**Endpoint:** `POST /api/sync/push`

```json
{
  "deviceId": "desktop-{machine-guid}",
  "changes": [
    {
      "table": "Equipment",
      "id": "550e8400-e29b-41d4-a716-446655440005",
      "operation": "Insert",
      "data": {
        "name": "New Depth Sensor",
        "categoryId": "...",
        "currentLocationId": "...",
        "status": "Available"
      },
      "localTimestamp": "2024-01-20T10:30:00Z"
    },
    {
      "table": "Equipment",
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "operation": "Update",
      "data": {
        "status": "InUse",
        "currentProjectId": "..."
      },
      "localTimestamp": "2024-01-20T10:35:00Z"
    }
  ]
}
```

### Push Response

```json
{
  "applied": 2,
  "newIds": {
    "temp-local-id-1": "550e8400-e29b-41d4-a716-446655440005"
  },
  "conflicts": []
}
```

### Conflict Response (When Conflicts Occur)

```json
{
  "applied": 1,
  "conflicts": [
    {
      "conflictId": "conf-001",
      "table": "Equipment",
      "recordId": "550e8400-e29b-41d4-a716-446655440002",
      "conflictType": "Update",
      "localData": {
        "status": "InUse",
        "updatedAt": "2024-01-20T10:35:00Z"
      },
      "serverData": {
        "status": "InTransit",
        "updatedAt": "2024-01-20T10:32:00Z"
      }
    }
  ]
}
```

### Resolve Conflict

**Endpoint:** `POST /api/sync/conflicts/{conflictId}/resolve`

```json
{
  "resolution": "UseLocal"
}
```

Resolution options: `UseLocal`, `UseServer`, `Merged`

---

## 📊 Data Models (C# DTOs)

### Equipment DTO

```csharp
public class EquipmentDto
{
    public Guid Id { get; set; }
    public string AssetNumber { get; set; }
    public string? SapNumber { get; set; }
    public string? TechNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string QrCode { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    
    public Guid? CategoryId { get; set; }
    public CategoryDto? Category { get; set; }
    public Guid? TypeId { get; set; }
    public EquipmentTypeDto? Type { get; set; }
    
    public Guid? CurrentLocationId { get; set; }
    public LocationDto? CurrentLocation { get; set; }
    public Guid? CurrentProjectId { get; set; }
    public ProjectDto? CurrentProject { get; set; }
    
    public EquipmentStatus Status { get; set; }
    public EquipmentCondition Condition { get; set; }
    public OwnershipType OwnershipType { get; set; }
    
    public decimal? WeightKg { get; set; }
    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    
    public bool RequiresCertification { get; set; }
    public DateTime? CertificationExpiryDate { get; set; }
    public bool RequiresCalibration { get; set; }
    public DateTime? NextCalibrationDate { get; set; }
    
    public string? PrimaryPhotoUrl { get; set; }
    public string? QrCodeImageUrl { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long SyncVersion { get; set; }
}
```

### Manifest DTO

```csharp
public class ManifestDto
{
    public Guid Id { get; set; }
    public string ManifestNumber { get; set; }
    public string QrCode { get; set; }
    public ManifestType Type { get; set; }
    public ManifestStatus Status { get; set; }
    
    public Guid FromLocationId { get; set; }
    public LocationDto? FromLocation { get; set; }
    public string? FromContactName { get; set; }
    public string? FromContactPhone { get; set; }
    public string? FromContactEmail { get; set; }
    
    public Guid ToLocationId { get; set; }
    public LocationDto? ToLocation { get; set; }
    public string? ToContactName { get; set; }
    public string? ToContactPhone { get; set; }
    public string? ToContactEmail { get; set; }
    
    public Guid? ProjectId { get; set; }
    public ProjectDto? Project { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? ExpectedArrivalDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    
    public string? ShippingMethod { get; set; }
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    
    public int TotalItems { get; set; }
    public decimal? TotalWeight { get; set; }
    
    public List<ManifestItemDto> Items { get; set; } = new();
    
    public string? SenderSignature { get; set; }
    public DateTime? SenderSignedAt { get; set; }
    public string? ReceiverSignature { get; set; }
    public DateTime? ReceiverSignedAt { get; set; }
    
    public string? Notes { get; set; }
    public bool HasDiscrepancies { get; set; }
    
    public bool IsActive { get; set; }
    public long SyncVersion { get; set; }
}
```

### Enumerations

```csharp
public enum EquipmentStatus
{
    Available,
    InUse,
    InTransit,
    UnderRepair,
    InCalibration,
    Reserved,
    Condemned,
    Lost,
    Retired,
    OnHire
}

public enum EquipmentCondition
{
    New,
    Good,
    Fair,
    Poor,
    Damaged
}

public enum ManifestType
{
    Inward,
    Outward
}

public enum ManifestStatus
{
    Draft,
    Submitted,
    PendingApproval,
    Approved,
    Rejected,
    InTransit,
    PartiallyReceived,
    Received,
    Completed,
    Cancelled
}

public enum OwnershipType
{
    Owned,
    Rented,
    Client,
    Loaned
}
```

---

## 🏷️ QR Code Format

**CRITICAL:** Desktop must use the same QR code format as Mobile for compatibility.

| Entity | Format | Example |
|--------|--------|---------|
| Equipment | `foseq:{assetNumber}` | `foseq:EQ-2024-00001` |
| Manifest | `fosman:{manifestNumber}` | `fosman:MN-2024-00125` |
| Location | `fosloc:{locationCode}` | `fosloc:VES-EXP-001` |

### QR Code Parsing (C#)

```csharp
public class QrCodeParser
{
    public static (QrType type, string identifier) Parse(string qrCode)
    {
        if (string.IsNullOrEmpty(qrCode))
            throw new ArgumentException("QR code cannot be empty");
            
        if (qrCode.StartsWith("foseq:"))
            return (QrType.Equipment, qrCode.Substring(5));
            
        if (qrCode.StartsWith("fosman:"))
            return (QrType.Manifest, qrCode.Substring(5));
            
        if (qrCode.StartsWith("fosloc:"))
            return (QrType.Location, qrCode.Substring(6));
            
        throw new ArgumentException($"Unknown QR code format: {qrCode}");
    }
}

public enum QrType
{
    Equipment,
    Manifest,
    Location
}
```

### QR Code Generation (C#)

```csharp
// Using QRCoder NuGet package
using QRCoder;

public class QrCodeGenerator
{
    public byte[] GenerateQrCode(string content, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule);
    }
    
    public string GenerateEquipmentQrContent(string assetNumber)
        => $"foseq:{assetNumber}";
        
    public string GenerateManifestQrContent(string manifestNumber)
        => $"fosman:{manifestNumber}";
}
```

---

## 🌐 API Endpoints Summary

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Username/password login |
| POST | `/api/auth/login/pin` | PIN login (mobile) |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | Logout and revoke tokens |
| GET | `/api/auth/me` | Get current user profile |

### Equipment
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/equipment` | List with pagination/filters |
| GET | `/api/equipment/{id}` | Get by ID |
| GET | `/api/equipment/qr/{qrCode}` | Get by QR code |
| GET | `/api/equipment/asset/{assetNumber}` | Get by asset number |
| POST | `/api/equipment` | Create new |
| PUT | `/api/equipment/{id}` | Update |
| DELETE | `/api/equipment/{id}` | Soft delete |
| GET | `/api/equipment/{id}/history` | Get history |
| POST | `/api/equipment/{id}/photo` | Upload photo |

### Manifests
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/manifests` | List with filters |
| GET | `/api/manifests/{id}` | Get with items |
| GET | `/api/manifests/qr/{qrCode}` | Get by QR code |
| POST | `/api/manifests` | Create draft |
| PUT | `/api/manifests/{id}` | Update |
| POST | `/api/manifests/{id}/items` | Add items |
| POST | `/api/manifests/{id}/submit` | Submit for approval |
| POST | `/api/manifests/{id}/approve` | Approve |
| POST | `/api/manifests/{id}/reject` | Reject |
| POST | `/api/manifests/{id}/ship` | Mark shipped |
| POST | `/api/manifests/{id}/receive` | Record receipt |
| POST | `/api/manifests/{id}/sign` | Add signature |

### Sync
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sync/pull` | Pull changes since version |
| POST | `/api/sync/push` | Push local changes |
| GET | `/api/sync/status` | Get sync status |
| GET | `/api/sync/conflicts` | Get pending conflicts |
| POST | `/api/sync/conflicts/{id}/resolve` | Resolve conflict |

### Lookups
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/lookups/locations` | All locations |
| GET | `/api/lookups/categories` | Equipment categories |
| GET | `/api/lookups/types` | Equipment types |
| GET | `/api/lookups/projects` | Active projects |
| GET | `/api/lookups/suppliers` | Suppliers |

---

## 💾 Local Database Schema (Desktop SQLite)

The Desktop app should mirror the central database schema for offline support:

```sql
-- Core sync tracking
CREATE TABLE SyncMeta (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
-- Store: lastSyncVersion, lastSyncTime, deviceId

-- Equipment (mirror of server)
CREATE TABLE Equipment (
    Id TEXT PRIMARY KEY,
    AssetNumber TEXT UNIQUE NOT NULL,
    SapNumber TEXT,
    TechNumber TEXT,
    SerialNumber TEXT,
    QrCode TEXT UNIQUE,
    Name TEXT NOT NULL,
    Description TEXT,
    Manufacturer TEXT,
    Model TEXT,
    CategoryId TEXT,
    TypeId TEXT,
    CurrentLocationId TEXT,
    CurrentProjectId TEXT,
    Status TEXT NOT NULL,
    Condition TEXT NOT NULL,
    OwnershipType TEXT,
    WeightKg REAL,
    LengthCm REAL,
    WidthCm REAL,
    HeightCm REAL,
    RequiresCertification INTEGER DEFAULT 0,
    CertificationExpiryDate TEXT,
    RequiresCalibration INTEGER DEFAULT 0,
    NextCalibrationDate TEXT,
    PrimaryPhotoUrl TEXT,
    QrCodeImageUrl TEXT,
    Notes TEXT,
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    SyncVersion INTEGER DEFAULT 0,
    IsModifiedLocally INTEGER DEFAULT 0
);

-- Manifests
CREATE TABLE Manifests (
    Id TEXT PRIMARY KEY,
    ManifestNumber TEXT UNIQUE NOT NULL,
    QrCode TEXT UNIQUE,
    Type TEXT NOT NULL,
    Status TEXT NOT NULL,
    FromLocationId TEXT NOT NULL,
    FromContactName TEXT,
    ToLocationId TEXT NOT NULL,
    ToContactName TEXT,
    ProjectId TEXT,
    CreatedDate TEXT NOT NULL,
    ShippedDate TEXT,
    ExpectedArrivalDate TEXT,
    ReceivedDate TEXT,
    TotalItems INTEGER DEFAULT 0,
    TotalWeight REAL,
    Notes TEXT,
    HasDiscrepancies INTEGER DEFAULT 0,
    IsActive INTEGER DEFAULT 1,
    SyncVersion INTEGER DEFAULT 0,
    IsModifiedLocally INTEGER DEFAULT 0
);

-- Manifest Items
CREATE TABLE ManifestItems (
    Id TEXT PRIMARY KEY,
    ManifestId TEXT NOT NULL,
    EquipmentId TEXT NOT NULL,
    AssetNumber TEXT,
    Name TEXT,
    Quantity REAL DEFAULT 1,
    ConditionAtSend TEXT,
    IsReceived INTEGER DEFAULT 0,
    ReceivedQuantity REAL,
    ConditionAtReceive TEXT,
    HasDiscrepancy INTEGER DEFAULT 0,
    SyncVersion INTEGER DEFAULT 0,
    FOREIGN KEY (ManifestId) REFERENCES Manifests(Id)
);

-- Offline Queue (for changes made while offline)
CREATE TABLE OfflineQueue (
    Id TEXT PRIMARY KEY,
    TableName TEXT NOT NULL,
    RecordId TEXT NOT NULL,
    Operation TEXT NOT NULL,
    Data TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    Attempts INTEGER DEFAULT 0,
    LastError TEXT,
    Status TEXT DEFAULT 'Pending'
);

-- Indexes
CREATE INDEX idx_equipment_qr ON Equipment(QrCode);
CREATE INDEX idx_equipment_asset ON Equipment(AssetNumber);
CREATE INDEX idx_equipment_location ON Equipment(CurrentLocationId);
CREATE INDEX idx_equipment_sync ON Equipment(SyncVersion);
CREATE INDEX idx_manifest_qr ON Manifests(QrCode);
CREATE INDEX idx_manifest_status ON Manifests(Status);
```

---

## ⚙️ Configuration

### API Base URLs

| Environment | URL |
|-------------|-----|
| Development | `https://localhost:5001/api` |
| Staging | `https://s7-equipment-api-staging.up.railway.app/api` |
| Production | `https://api.yourcompany.com/api` |

### Desktop App Configuration (appsettings.json)

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.yourcompany.com/api",
    "TimeoutSeconds": 30,
    "RetryCount": 3
  },
  "SyncSettings": {
    "AutoSyncIntervalMinutes": 5,
    "MaxRetryAttempts": 3,
    "RetryDelaySeconds": 5
  },
  "LocalDatabase": {
    "Path": "%APPDATA%\\FathomOS\\Equipment\\local.db"
  }
}
```

---

## ✅ Integration Checklist

### Phase 1: Authentication
- [ ] Implement login with username/password
- [ ] Store tokens securely (Windows Credential Manager)
- [ ] Implement automatic token refresh
- [ ] Handle 401 responses (redirect to login)
- [ ] Include required headers in all requests

### Phase 2: Basic CRUD
- [ ] Equipment list with pagination
- [ ] Equipment details view
- [ ] Equipment create/update
- [ ] Manifest list/details
- [ ] Manifest workflow (create → submit → approve → ship → receive)

### Phase 3: Sync Integration
- [ ] Implement local SQLite database
- [ ] Initial full sync on first login
- [ ] Delta sync on application start
- [ ] Background sync every 5 minutes
- [ ] Offline queue for changes made offline
- [ ] Conflict detection and resolution UI

### Phase 4: Advanced Features
- [ ] QR code scanning (same format as mobile)
- [ ] QR code generation
- [ ] Photo upload/download
- [ ] Digital signature capture
- [ ] PDF report generation

---

## 🆘 Support

For integration questions:
- Review `docs/API_DOCUMENTATION.md` for detailed endpoint specs
- Review `docs/DEVELOPER_ONBOARDING.md` for setup instructions
- Contact: backend-team@company.com

---

**Document Version:** 1.0  
**Created:** January 2025  
**For:** Desktop Module Development Team
