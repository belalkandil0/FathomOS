# Fathom OS Equipment & Inventory Management System
## Complete System Architecture v1.0

---

## 1. Executive Summary

The Fathom OS Equipment & Inventory Management System is an enterprise-grade solution for managing equipment, consumables, and inventory across offshore vessels, onshore bases, and field projects. The system consists of three interconnected components:

1. **Desktop Module** (FathomOS.Modules.EquipmentInventory) - Windows WPF application
2. **Mobile Application** - Cross-platform (React Native/Flutter) for iOS/Android
3. **Central API Server** - REST API with synchronization capabilities

### Key Features
- QR code generation and scanning for equipment tracking
- Inward/Outward manifest management with approval workflows
- Offline-first architecture with intelligent sync
- Multi-location inventory tracking (100 bases, 150 vessels)
- Role-based access control with custom permissions
- Digital signatures for manifest sign-off
- Comprehensive reporting and analytics

---

## 2. System Architecture Overview

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                     S7 FATHOM EQUIPMENT MANAGEMENT SYSTEM                               â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                                         â”‚
â”‚  ONSHORE BASE                    OFFSHORE VESSEL                    MOBILE DEVICES      â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”            â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”                â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”‚
â”‚  â”‚  Desktop App    â”‚            â”‚  Desktop App    â”‚                â”‚  Mobile App     â”‚  â”‚
â”‚  â”‚  (Full Access)  â”‚            â”‚  (Full Access)  â”‚                â”‚  (Field Ops)    â”‚  â”‚
â”‚  â”‚                 â”‚            â”‚                 â”‚                â”‚                 â”‚  â”‚
â”‚  â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”‚            â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”‚                â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”‚  â”‚
â”‚  â”‚  â”‚  SQLite   â”‚  â”‚            â”‚  â”‚  SQLite   â”‚  â”‚                â”‚  â”‚  SQLite   â”‚  â”‚  â”‚
â”‚  â”‚  â”‚  Local DB â”‚  â”‚            â”‚  â”‚  Local DB â”‚  â”‚                â”‚  â”‚  Local DB â”‚  â”‚  â”‚
â”‚  â”‚  â””â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”˜  â”‚            â”‚  â””â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”˜  â”‚                â”‚  â””â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”˜  â”‚  â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”˜            â””â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”˜                â””â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â”‚
â”‚           â”‚                              â”‚                                  â”‚           â”‚
â”‚           â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”       â”‚
â”‚           â”‚  â”‚                    SYNC ENGINE                                   â”‚       â”‚
â”‚           â”‚  â”‚  â€¢ Delta Sync (changes only)                                     â”‚       â”‚
â”‚           â”‚  â”‚  â€¢ Conflict Resolution                                           â”‚       â”‚
â”‚           â”‚  â”‚  â€¢ Offline Queue Management                                      â”‚       â”‚
â”‚           â”‚  â”‚  â€¢ Retry with Exponential Backoff                                â”‚       â”‚
â”‚           â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜       â”‚
â”‚           â”‚                              â”‚                                  â”‚           â”‚
â”‚           â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜           â”‚
â”‚                                          â”‚                                              â”‚
â”‚                                          â–¼                                              â”‚
â”‚                    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”                      â”‚
â”‚                    â”‚            CENTRAL API SERVER               â”‚                      â”‚
â”‚                    â”‚                                             â”‚                      â”‚
â”‚                    â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚                      â”‚
â”‚                    â”‚  â”‚         REST API (ASP.NET Core)     â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Authentication (JWT + AD/SSO)    â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Authorization (Role-Based)       â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Sync Endpoints                   â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Business Logic                   â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Audit Logging                    â”‚   â”‚                      â”‚
â”‚                    â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚                      â”‚
â”‚                    â”‚                    â”‚                        â”‚                      â”‚
â”‚                    â”‚                    â–¼                        â”‚                      â”‚
â”‚                    â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚                      â”‚
â”‚                    â”‚  â”‚      Central Database               â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚      (PostgreSQL / SQL Server)      â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚                                     â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Equipment Registry               â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Manifest History                 â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ User Management                  â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Sync State                       â”‚   â”‚                      â”‚
â”‚                    â”‚  â”‚  â€¢ Audit Trail                      â”‚   â”‚                      â”‚
â”‚                    â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚                      â”‚
â”‚                    â”‚                                             â”‚                      â”‚
â”‚                    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜                      â”‚
â”‚                                                                                         â”‚
â”‚  DEVELOPMENT PHASE: Free Cloud Hosting                                                  â”‚
â”‚  â”œâ”€â”€ API: Railway.app / Render.com / Fly.io (free tier)                                â”‚
â”‚  â””â”€â”€ Database: Supabase (PostgreSQL, free tier) or Railway PostgreSQL                  â”‚
â”‚                                                                                         â”‚
â”‚  PRODUCTION: Windows Server Self-Hosted                                                 â”‚
â”‚  â”œâ”€â”€ API: IIS / Kestrel                                                                â”‚
â”‚  â””â”€â”€ Database: SQL Server or PostgreSQL                                                â”‚
â”‚                                                                                         â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

---

## 3. Development Infrastructure Recommendation

### 3.1 Free Cloud Hosting for Development

| Component | Service | Free Tier Limits | Notes |
|-----------|---------|------------------|-------|
| **API Server** | [Railway.app](https://railway.app) | 500 hours/month, 512MB RAM | Best for .NET Core |
| **API Alternative** | [Render.com](https://render.com) | 750 hours/month | Spins down after inactivity |
| **Database** | [Supabase](https://supabase.com) | 500MB, 50K rows | PostgreSQL with REST API |
| **Database Alt** | [Neon](https://neon.tech) | 3GB storage | Serverless PostgreSQL |
| **File Storage** | [Cloudflare R2](https://cloudflare.com/r2) | 10GB free | For QR codes, photos |

**Recommended Stack for Development:**
```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  DEVELOPMENT ENVIRONMENT                        â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  API: Railway.app                               â”‚
â”‚  URL: https://s7-equipment-api.up.railway.app   â”‚
â”‚                                                 â”‚
â”‚  Database: Supabase PostgreSQL                  â”‚
â”‚  URL: postgres://...@db.supabase.co:5432/...    â”‚
â”‚                                                 â”‚
â”‚  File Storage: Cloudflare R2                    â”‚
â”‚  URL: https://s7-files.r2.cloudflarestorage.com â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 3.2 Production Infrastructure (Windows Server)

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  PRODUCTION ENVIRONMENT                         â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  Server: Windows Server 2022                    â”‚
â”‚  Web Server: IIS with ASP.NET Core Module       â”‚
â”‚  Database: SQL Server 2022 or PostgreSQL 16     â”‚
â”‚  File Storage: Local or Network Share           â”‚
â”‚                                                 â”‚
â”‚  Recommended Specs:                             â”‚
â”‚  â€¢ CPU: 8 cores                                 â”‚
â”‚  â€¢ RAM: 32GB                                    â”‚
â”‚  â€¢ Storage: 500GB SSD                           â”‚
â”‚  â€¢ Network: 1Gbps                               â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

---

## 4. Database Schema

### 4.1 Core Tables

```sql
-- =====================================================
-- ORGANIZATION & LOCATIONS
-- =====================================================

CREATE TABLE Companies (
    CompanyId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(200) NOT NULL,
    Code VARCHAR(20) UNIQUE NOT NULL,
    Address TEXT,
    Phone VARCHAR(50),
    Email VARCHAR(100),
    Website VARCHAR(200),
    LogoUrl VARCHAR(500),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    CreatedBy UUID,
    UpdatedBy UUID
);

CREATE TABLE Regions (
    RegionId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    CompanyId UUID REFERENCES Companies(CompanyId),
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(20) NOT NULL,
    Description TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE LocationTypes (
    LocationTypeId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(50) NOT NULL, -- 'Base', 'Vessel', 'Project Site', 'Container', 'Warehouse'
    Icon VARCHAR(50),
    Color VARCHAR(7),
    IsActive BOOLEAN DEFAULT TRUE
);

CREATE TABLE Locations (
    LocationId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    RegionId UUID REFERENCES Regions(RegionId),
    ParentLocationId UUID REFERENCES Locations(LocationId), -- For hierarchical locations
    LocationTypeId UUID REFERENCES LocationTypes(LocationTypeId),
    Name VARCHAR(200) NOT NULL,
    Code VARCHAR(50) UNIQUE NOT NULL,
    Description TEXT,
    Address TEXT,
    Latitude DECIMAL(10, 8),
    Longitude DECIMAL(11, 8),
    ContactPerson VARCHAR(100),
    ContactPhone VARCHAR(50),
    ContactEmail VARCHAR(100),
    IsOffshore BOOLEAN DEFAULT FALSE,
    IsActive BOOLEAN DEFAULT TRUE,
    Capacity INT, -- Max items that can be stored
    QrCode VARCHAR(100),
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    SyncVersion BIGINT DEFAULT 0
);

CREATE TABLE Vessels (
    VesselId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    LocationId UUID REFERENCES Locations(LocationId), -- Each vessel is also a location
    IMONumber VARCHAR(20),
    CallSign VARCHAR(20),
    Flag VARCHAR(50),
    VesselType VARCHAR(50), -- 'DSV', 'CSV', 'Barge', 'Supply Vessel'
    GrossTonnage DECIMAL(12, 2),
    Length DECIMAL(8, 2),
    Beam DECIMAL(8, 2),
    Draft DECIMAL(8, 2),
    OwnerCompany VARCHAR(200),
    OperatorCompany VARCHAR(200),
    ClassificationSociety VARCHAR(100),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);

-- =====================================================
-- USER MANAGEMENT & AUTHENTICATION
-- =====================================================

CREATE TABLE Roles (
    RoleId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT,
    IsSystemRole BOOLEAN DEFAULT FALSE, -- Cannot be deleted
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE Permissions (
    PermissionId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL UNIQUE,
    Category VARCHAR(50), -- 'Equipment', 'Manifest', 'Reports', 'Admin'
    Description TEXT
);

CREATE TABLE RolePermissions (
    RoleId UUID REFERENCES Roles(RoleId),
    PermissionId UUID REFERENCES Permissions(PermissionId),
    PRIMARY KEY (RoleId, PermissionId)
);

CREATE TABLE Users (
    UserId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Username VARCHAR(100) UNIQUE NOT NULL,
    Email VARCHAR(200) UNIQUE NOT NULL,
    PasswordHash VARCHAR(500), -- NULL if using AD/SSO only
    Salt VARCHAR(100),
    FirstName VARCHAR(100),
    LastName VARCHAR(100),
    Phone VARCHAR(50),
    Pin VARCHAR(10), -- Hashed PIN for quick mobile access
    PinSalt VARCHAR(50),
    ProfilePhotoUrl VARCHAR(500),
    DefaultLocationId UUID REFERENCES Locations(LocationId),
    IsAdUser BOOLEAN DEFAULT FALSE,
    AdObjectId VARCHAR(100), -- Azure AD Object ID
    IsActive BOOLEAN DEFAULT TRUE,
    LastLoginAt TIMESTAMP,
    FailedLoginAttempts INT DEFAULT 0,
    LockedUntil TIMESTAMP,
    MustChangePassword BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE UserRoles (
    UserId UUID REFERENCES Users(UserId),
    RoleId UUID REFERENCES Roles(RoleId),
    AssignedAt TIMESTAMP DEFAULT NOW(),
    AssignedBy UUID REFERENCES Users(UserId),
    PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE UserLocations (
    UserId UUID REFERENCES Users(UserId),
    LocationId UUID REFERENCES Locations(LocationId),
    AccessLevel VARCHAR(20), -- 'Read', 'Write', 'Admin'
    PRIMARY KEY (UserId, LocationId)
);

CREATE TABLE RefreshTokens (
    TokenId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    UserId UUID REFERENCES Users(UserId),
    Token VARCHAR(500) NOT NULL,
    DeviceInfo VARCHAR(200),
    ExpiresAt TIMESTAMP NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    RevokedAt TIMESTAMP
);

-- =====================================================
-- EQUIPMENT & INVENTORY
-- =====================================================

CREATE TABLE EquipmentCategories (
    CategoryId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ParentCategoryId UUID REFERENCES EquipmentCategories(CategoryId),
    Name VARCHAR(100) NOT NULL,
    Code VARCHAR(20) UNIQUE NOT NULL,
    Description TEXT,
    Icon VARCHAR(50),
    Color VARCHAR(7),
    IsConsumable BOOLEAN DEFAULT FALSE, -- Track by quantity vs individual
    RequiresCertification BOOLEAN DEFAULT FALSE,
    RequiresCalibration BOOLEAN DEFAULT FALSE,
    DefaultCertificationPeriodDays INT,
    DefaultCalibrationPeriodDays INT,
    SortOrder INT DEFAULT 0,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    SyncVersion BIGINT DEFAULT 0
);

CREATE TABLE EquipmentTypes (
    TypeId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    CategoryId UUID REFERENCES EquipmentCategories(CategoryId),
    Name VARCHAR(200) NOT NULL,
    Code VARCHAR(50) UNIQUE NOT NULL,
    Description TEXT,
    Manufacturer VARCHAR(200),
    Model VARCHAR(200),
    DefaultUnit VARCHAR(20), -- 'Each', 'Set', 'Meter', 'Kg', 'Liter'
    SpecificationsJson JSONB, -- Flexible specs storage
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    SyncVersion BIGINT DEFAULT 0
);

CREATE TABLE Suppliers (
    SupplierId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(200) NOT NULL,
    Code VARCHAR(50) UNIQUE,
    ContactPerson VARCHAR(100),
    Email VARCHAR(200),
    Phone VARCHAR(50),
    Address TEXT,
    Website VARCHAR(200),
    TaxId VARCHAR(50),
    PaymentTerms VARCHAR(100),
    Notes TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE Projects (
    ProjectId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    CompanyId UUID REFERENCES Companies(CompanyId),
    Name VARCHAR(200) NOT NULL,
    Code VARCHAR(50) UNIQUE NOT NULL,
    ClientName VARCHAR(200),
    Description TEXT,
    LocationId UUID REFERENCES Locations(LocationId), -- Primary location
    VesselId UUID REFERENCES Vessels(VesselId), -- Primary vessel
    StartDate DATE,
    EndDate DATE,
    Status VARCHAR(20) DEFAULT 'Active', -- 'Planning', 'Active', 'OnHold', 'Completed', 'Cancelled'
    ProjectManager VARCHAR(100),
    ContactEmail VARCHAR(200),
    ContactPhone VARCHAR(50),
    Budget DECIMAL(15, 2),
    Currency VARCHAR(3) DEFAULT 'USD',
    Notes TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    SyncVersion BIGINT DEFAULT 0
);

CREATE TABLE Equipment (
    EquipmentId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Identification
    AssetNumber VARCHAR(50) UNIQUE NOT NULL, -- Auto-generated or manual
    SapNumber VARCHAR(50), -- SAP system reference
    TechNumber VARCHAR(50), -- Technical reference
    SerialNumber VARCHAR(100),
    QrCode VARCHAR(100) UNIQUE, -- Generated QR code value
    Barcode VARCHAR(100), -- External barcode if exists
    
    -- Classification
    TypeId UUID REFERENCES EquipmentTypes(TypeId),
    CategoryId UUID REFERENCES EquipmentCategories(CategoryId),
    
    -- Description
    Name VARCHAR(200) NOT NULL,
    Description TEXT,
    Manufacturer VARCHAR(200),
    Model VARCHAR(200),
    PartNumber VARCHAR(100),
    Specifications JSONB, -- Flexible specs
    
    -- Physical Properties
    WeightKg DECIMAL(10, 3),
    LengthCm DECIMAL(10, 2),
    WidthCm DECIMAL(10, 2),
    HeightCm DECIMAL(10, 2),
    VolumeCm3 DECIMAL(15, 2),
    
    -- Packaging
    PackagingType VARCHAR(50), -- 'Box', 'Case', 'Pallet', 'Drum', 'Reel'
    PackagingWeightKg DECIMAL(10, 3),
    PackagingLengthCm DECIMAL(10, 2),
    PackagingWidthCm DECIMAL(10, 2),
    PackagingHeightCm DECIMAL(10, 2),
    PackagingDescription TEXT,
    
    -- Location & Status
    CurrentLocationId UUID REFERENCES Locations(LocationId),
    CurrentProjectId UUID REFERENCES Projects(ProjectId),
    CurrentCustodianId UUID REFERENCES Users(UserId),
    Status VARCHAR(30) DEFAULT 'Available', 
    -- 'Available', 'InUse', 'InTransit', 'UnderRepair', 'InCalibration', 
    -- 'Reserved', 'Condemned', 'Lost', 'Retired', 'OnHire'
    Condition VARCHAR(20) DEFAULT 'Good', -- 'New', 'Good', 'Fair', 'Poor', 'Damaged'
    
    -- Ownership & Procurement
    OwnershipType VARCHAR(20) DEFAULT 'Owned', -- 'Owned', 'Rented', 'Client', 'Loaned'
    SupplierId UUID REFERENCES Suppliers(SupplierId),
    PurchaseDate DATE,
    PurchasePrice DECIMAL(15, 2),
    PurchaseCurrency VARCHAR(3) DEFAULT 'USD',
    PurchaseOrderNumber VARCHAR(50),
    WarrantyExpiryDate DATE,
    RentalStartDate DATE,
    RentalEndDate DATE,
    RentalRate DECIMAL(15, 2),
    RentalRatePeriod VARCHAR(20), -- 'Day', 'Week', 'Month'
    
    -- Depreciation
    DepreciationMethod VARCHAR(20), -- 'StraightLine', 'DecliningBalance'
    UsefulLifeYears INT,
    ResidualValue DECIMAL(15, 2),
    CurrentValue DECIMAL(15, 2),
    
    -- Certification & Calibration
    RequiresCertification BOOLEAN DEFAULT FALSE,
    CertificationNumber VARCHAR(100),
    CertificationBody VARCHAR(200),
    CertificationDate DATE,
    CertificationExpiryDate DATE,
    RequiresCalibration BOOLEAN DEFAULT FALSE,
    LastCalibrationDate DATE,
    NextCalibrationDate DATE,
    CalibrationInterval INT, -- Days
    
    -- Service & Maintenance
    LastServiceDate DATE,
    NextServiceDate DATE,
    ServiceInterval INT, -- Days
    LastInspectionDate DATE,
    
    -- For Consumables (when tracked by quantity)
    IsConsumable BOOLEAN DEFAULT FALSE,
    QuantityOnHand DECIMAL(15, 3) DEFAULT 1,
    UnitOfMeasure VARCHAR(20) DEFAULT 'Each',
    MinimumStockLevel DECIMAL(15, 3),
    ReorderLevel DECIMAL(15, 3),
    MaximumStockLevel DECIMAL(15, 3),
    BatchNumber VARCHAR(50),
    LotNumber VARCHAR(50),
    ExpiryDate DATE,
    
    -- Assignment
    IsPermanentEquipment BOOLEAN DEFAULT FALSE, -- Permanent to location/vessel
    IsProjectEquipment BOOLEAN DEFAULT FALSE, -- Assigned to project
    
    -- Photos & Documents
    PrimaryPhotoUrl VARCHAR(500),
    QrCodeImageUrl VARCHAR(500),
    
    -- Notes
    Notes TEXT,
    InternalNotes TEXT, -- Not shown in reports
    
    -- Audit
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    CreatedBy UUID REFERENCES Users(UserId),
    UpdatedBy UUID REFERENCES Users(UserId),
    SyncVersion BIGINT DEFAULT 0,
    
    -- Full-text search
    SearchVector TSVECTOR
);

CREATE TABLE EquipmentPhotos (
    PhotoId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EquipmentId UUID REFERENCES Equipment(EquipmentId),
    PhotoUrl VARCHAR(500) NOT NULL,
    ThumbnailUrl VARCHAR(500),
    Caption VARCHAR(200),
    PhotoType VARCHAR(20), -- 'Main', 'Condition', 'Damage', 'Label', 'Certificate'
    TakenAt TIMESTAMP,
    TakenBy UUID REFERENCES Users(UserId),
    SortOrder INT DEFAULT 0,
    CreatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE EquipmentDocuments (
    DocumentId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EquipmentId UUID REFERENCES Equipment(EquipmentId),
    DocumentUrl VARCHAR(500) NOT NULL,
    FileName VARCHAR(200),
    FileType VARCHAR(50),
    FileSizeBytes BIGINT,
    DocumentType VARCHAR(50), -- 'Certificate', 'Manual', 'Datasheet', 'Inspection', 'Other'
    Description TEXT,
    ExpiryDate DATE,
    UploadedBy UUID REFERENCES Users(UserId),
    CreatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE EquipmentHistory (
    HistoryId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    EquipmentId UUID REFERENCES Equipment(EquipmentId),
    EventType VARCHAR(50) NOT NULL,
    -- 'Created', 'Updated', 'StatusChanged', 'LocationChanged', 'CustodianChanged',
    -- 'Transferred', 'Received', 'Inspected', 'Serviced', 'Calibrated', 'Certified',
    -- 'Damaged', 'Repaired', 'Condemned', 'Retired', 'QuantityAdjusted'
    EventDescription TEXT,
    PreviousValue JSONB,
    NewValue JSONB,
    FromLocationId UUID REFERENCES Locations(LocationId),
    ToLocationId UUID REFERENCES Locations(LocationId),
    ManifestId UUID, -- Reference to manifest if applicable
    PerformedBy UUID REFERENCES Users(UserId),
    PerformedAt TIMESTAMP DEFAULT NOW(),
    Notes TEXT,
    SyncVersion BIGINT DEFAULT 0
);

-- =====================================================
-- MANIFESTS (Inward & Outward)
-- =====================================================

CREATE TABLE Manifests (
    ManifestId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Identification
    ManifestNumber VARCHAR(50) UNIQUE NOT NULL, -- Auto-generated
    QrCode VARCHAR(100) UNIQUE, -- For quick manifest retrieval
    ManifestType VARCHAR(10) NOT NULL, -- 'Inward', 'Outward'
    
    -- Origin
    FromLocationId UUID REFERENCES Locations(LocationId),
    FromContactName VARCHAR(100),
    FromContactPhone VARCHAR(50),
    FromContactEmail VARCHAR(200),
    
    -- Destination
    ToLocationId UUID REFERENCES Locations(LocationId),
    ToContactName VARCHAR(100),
    ToContactPhone VARCHAR(50),
    ToContactEmail VARCHAR(200),
    
    -- Project Reference
    ProjectId UUID REFERENCES Projects(ProjectId),
    
    -- Status & Workflow
    Status VARCHAR(20) DEFAULT 'Draft',
    -- 'Draft', 'Submitted', 'PendingApproval', 'Approved', 'Rejected',
    -- 'InTransit', 'PartiallyReceived', 'Received', 'Completed', 'Cancelled'
    
    -- Dates
    CreatedDate TIMESTAMP DEFAULT NOW(),
    SubmittedDate TIMESTAMP,
    ApprovedDate TIMESTAMP,
    ShippedDate TIMESTAMP,
    ExpectedArrivalDate DATE,
    ReceivedDate TIMESTAMP,
    CompletedDate TIMESTAMP,
    
    -- Shipping Details
    ShippingMethod VARCHAR(50), -- 'Road', 'Sea', 'Air', 'Helicopter', 'Internal'
    CarrierName VARCHAR(200),
    TrackingNumber VARCHAR(100),
    VehicleNumber VARCHAR(50),
    DriverName VARCHAR(100),
    DriverPhone VARCHAR(50),
    
    -- Totals
    TotalItems INT DEFAULT 0,
    TotalWeight DECIMAL(12, 3),
    TotalVolume DECIMAL(15, 2),
    
    -- People
    CreatedBy UUID REFERENCES Users(UserId),
    SubmittedBy UUID REFERENCES Users(UserId),
    ApprovedBy UUID REFERENCES Users(UserId),
    RejectedBy UUID REFERENCES Users(UserId),
    ShippedBy UUID REFERENCES Users(UserId),
    ReceivedBy UUID REFERENCES Users(UserId),
    
    -- Signatures
    SenderSignature TEXT, -- Base64 encoded signature image
    SenderSignedAt TIMESTAMP,
    ReceiverSignature TEXT,
    ReceiverSignedAt TIMESTAMP,
    ApproverSignature TEXT,
    ApproverSignedAt TIMESTAMP,
    
    -- Notes
    Notes TEXT,
    InternalNotes TEXT,
    RejectionReason TEXT,
    
    -- Discrepancies
    HasDiscrepancies BOOLEAN DEFAULT FALSE,
    DiscrepancyNotes TEXT,
    
    -- Audit
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    SyncVersion BIGINT DEFAULT 0
);

CREATE TABLE ManifestItems (
    ItemId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ManifestId UUID REFERENCES Manifests(ManifestId),
    EquipmentId UUID REFERENCES Equipment(EquipmentId),
    
    -- Item Details (captured at time of manifest)
    AssetNumber VARCHAR(50),
    Name VARCHAR(200),
    Description TEXT,
    SerialNumber VARCHAR(100),
    Quantity DECIMAL(15, 3) DEFAULT 1,
    UnitOfMeasure VARCHAR(20) DEFAULT 'Each',
    
    -- Physical
    WeightKg DECIMAL(10, 3),
    
    -- Condition at Send
    ConditionAtSend VARCHAR(20),
    ConditionNotes TEXT,
    
    -- Receipt Details
    IsReceived BOOLEAN DEFAULT FALSE,
    ReceivedQuantity DECIMAL(15, 3),
    ReceivedDate TIMESTAMP,
    ReceivedBy UUID REFERENCES Users(UserId),
    ConditionAtReceive VARCHAR(20),
    ReceiptNotes TEXT,
    HasDiscrepancy BOOLEAN DEFAULT FALSE,
    DiscrepancyType VARCHAR(20), -- 'Missing', 'Damaged', 'Wrong', 'Excess'
    DiscrepancyNotes TEXT,
    
    -- Photos
    SendPhotoUrl VARCHAR(500),
    ReceivePhotoUrl VARCHAR(500),
    
    -- Sort
    SortOrder INT DEFAULT 0,
    
    -- Audit
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    SyncVersion BIGINT DEFAULT 0
);

CREATE TABLE ManifestPhotos (
    PhotoId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ManifestId UUID REFERENCES Manifests(ManifestId),
    ItemId UUID REFERENCES ManifestItems(ItemId), -- NULL for general manifest photos
    PhotoUrl VARCHAR(500) NOT NULL,
    ThumbnailUrl VARCHAR(500),
    PhotoType VARCHAR(20), -- 'General', 'Packaging', 'Loading', 'Damage', 'Receipt'
    Caption VARCHAR(200),
    TakenAt TIMESTAMP,
    TakenBy UUID REFERENCES Users(UserId),
    CreatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE ManifestApprovals (
    ApprovalId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ManifestId UUID REFERENCES Manifests(ManifestId),
    ApprovalLevel INT DEFAULT 1, -- For multi-level approval
    Status VARCHAR(20), -- 'Pending', 'Approved', 'Rejected'
    ApproverId UUID REFERENCES Users(UserId),
    ApprovedAt TIMESTAMP,
    Comments TEXT,
    Signature TEXT,
    CreatedAt TIMESTAMP DEFAULT NOW()
);

-- =====================================================
-- SYNC & OFFLINE SUPPORT
-- =====================================================

CREATE TABLE SyncLog (
    SyncId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    DeviceId VARCHAR(100) NOT NULL,
    UserId UUID REFERENCES Users(UserId),
    SyncType VARCHAR(20), -- 'Full', 'Delta', 'Upload', 'Download'
    StartedAt TIMESTAMP DEFAULT NOW(),
    CompletedAt TIMESTAMP,
    Status VARCHAR(20), -- 'InProgress', 'Completed', 'Failed', 'Partial'
    RecordsUploaded INT DEFAULT 0,
    RecordsDownloaded INT DEFAULT 0,
    ConflictsFound INT DEFAULT 0,
    ConflictsResolved INT DEFAULT 0,
    ErrorMessage TEXT,
    SyncVersion BIGINT
);

CREATE TABLE SyncConflicts (
    ConflictId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TableName VARCHAR(100) NOT NULL,
    RecordId UUID NOT NULL,
    DeviceId VARCHAR(100),
    UserId UUID REFERENCES Users(UserId),
    LocalData JSONB,
    ServerData JSONB,
    ConflictType VARCHAR(20), -- 'Update', 'Delete', 'Both'
    Resolution VARCHAR(20), -- 'Pending', 'UseLocal', 'UseServer', 'Merged'
    ResolvedBy UUID REFERENCES Users(UserId),
    ResolvedAt TIMESTAMP,
    CreatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE OfflineQueue (
    QueueId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    DeviceId VARCHAR(100) NOT NULL,
    UserId UUID REFERENCES Users(UserId),
    TableName VARCHAR(100) NOT NULL,
    RecordId UUID NOT NULL,
    Operation VARCHAR(20), -- 'Insert', 'Update', 'Delete'
    Data JSONB,
    Priority INT DEFAULT 0,
    Attempts INT DEFAULT 0,
    LastAttempt TIMESTAMP,
    ErrorMessage TEXT,
    Status VARCHAR(20) DEFAULT 'Pending', -- 'Pending', 'Processing', 'Completed', 'Failed'
    CreatedAt TIMESTAMP DEFAULT NOW()
);

-- =====================================================
-- AUDIT & LOGGING
-- =====================================================

CREATE TABLE AuditLog (
    AuditId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TableName VARCHAR(100),
    RecordId UUID,
    Action VARCHAR(20), -- 'Create', 'Update', 'Delete', 'View', 'Export'
    UserId UUID REFERENCES Users(UserId),
    OldValues JSONB,
    NewValues JSONB,
    IpAddress VARCHAR(50),
    UserAgent VARCHAR(500),
    DeviceId VARCHAR(100),
    Timestamp TIMESTAMP DEFAULT NOW()
);

-- =====================================================
-- REPORTING & ALERTS
-- =====================================================

CREATE TABLE Alerts (
    AlertId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    AlertType VARCHAR(50) NOT NULL,
    -- 'CertificationExpiring', 'CalibrationDue', 'ServiceDue', 'LowStock',
    -- 'ConsumableExpiring', 'ManifestPending', 'TransferOverdue'
    EquipmentId UUID REFERENCES Equipment(EquipmentId),
    ManifestId UUID REFERENCES Manifests(ManifestId),
    LocationId UUID REFERENCES Locations(LocationId),
    Title VARCHAR(200),
    Message TEXT,
    Severity VARCHAR(20), -- 'Info', 'Warning', 'Critical'
    DueDate DATE,
    IsAcknowledged BOOLEAN DEFAULT FALSE,
    AcknowledgedBy UUID REFERENCES Users(UserId),
    AcknowledgedAt TIMESTAMP,
    IsResolved BOOLEAN DEFAULT FALSE,
    ResolvedBy UUID REFERENCES Users(UserId),
    ResolvedAt TIMESTAMP,
    CreatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE SavedReports (
    ReportId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(200) NOT NULL,
    ReportType VARCHAR(50),
    Parameters JSONB,
    Schedule VARCHAR(50), -- 'None', 'Daily', 'Weekly', 'Monthly'
    LastRunAt TIMESTAMP,
    CreatedBy UUID REFERENCES Users(UserId),
    CreatedAt TIMESTAMP DEFAULT NOW()
);

-- =====================================================
-- INDEXES FOR PERFORMANCE
-- =====================================================

CREATE INDEX idx_equipment_location ON Equipment(CurrentLocationId);
CREATE INDEX idx_equipment_project ON Equipment(CurrentProjectId);
CREATE INDEX idx_equipment_status ON Equipment(Status);
CREATE INDEX idx_equipment_category ON Equipment(CategoryId);
CREATE INDEX idx_equipment_type ON Equipment(TypeId);
CREATE INDEX idx_equipment_qr ON Equipment(QrCode);
CREATE INDEX idx_equipment_asset ON Equipment(AssetNumber);
CREATE INDEX idx_equipment_serial ON Equipment(SerialNumber);
CREATE INDEX idx_equipment_cert_expiry ON Equipment(CertificationExpiryDate);
CREATE INDEX idx_equipment_calibration ON Equipment(NextCalibrationDate);
CREATE INDEX idx_equipment_search ON Equipment USING gin(SearchVector);
CREATE INDEX idx_equipment_sync ON Equipment(SyncVersion);

CREATE INDEX idx_manifest_status ON Manifests(Status);
CREATE INDEX idx_manifest_type ON Manifests(ManifestType);
CREATE INDEX idx_manifest_from ON Manifests(FromLocationId);
CREATE INDEX idx_manifest_to ON Manifests(ToLocationId);
CREATE INDEX idx_manifest_qr ON Manifests(QrCode);
CREATE INDEX idx_manifest_sync ON Manifests(SyncVersion);

CREATE INDEX idx_history_equipment ON EquipmentHistory(EquipmentId);
CREATE INDEX idx_history_date ON EquipmentHistory(PerformedAt);

CREATE INDEX idx_audit_table ON AuditLog(TableName, RecordId);
CREATE INDEX idx_audit_user ON AuditLog(UserId);
CREATE INDEX idx_audit_time ON AuditLog(Timestamp);

-- =====================================================
-- DEFAULT DATA
-- =====================================================

-- Default Roles
INSERT INTO Roles (RoleId, Name, Description, IsSystemRole) VALUES
    ('00000000-0000-0000-0000-000000000001', 'System Administrator', 'Full system access', TRUE),
    ('00000000-0000-0000-0000-000000000002', 'Base Manager', 'Manage onshore base operations', TRUE),
    ('00000000-0000-0000-0000-000000000003', 'Vessel Superintendent', 'Manage vessel equipment', TRUE),
    ('00000000-0000-0000-0000-000000000004', 'Project Manager', 'View and approve project transfers', TRUE),
    ('00000000-0000-0000-0000-000000000005', 'Deck Operator', 'Scan and create manifests', TRUE),
    ('00000000-0000-0000-0000-000000000006', 'Store Keeper', 'Manage inventory', TRUE),
    ('00000000-0000-0000-0000-000000000007', 'Auditor', 'Read-only access for auditing', TRUE);

-- Default Permissions
INSERT INTO Permissions (PermissionId, Name, Category, Description) VALUES
    -- Equipment
    ('10000000-0000-0000-0000-000000000001', 'equipment.view', 'Equipment', 'View equipment'),
    ('10000000-0000-0000-0000-000000000002', 'equipment.create', 'Equipment', 'Create new equipment'),
    ('10000000-0000-0000-0000-000000000003', 'equipment.edit', 'Equipment', 'Edit equipment'),
    ('10000000-0000-0000-0000-000000000004', 'equipment.delete', 'Equipment', 'Delete equipment'),
    ('10000000-0000-0000-0000-000000000005', 'equipment.export', 'Equipment', 'Export equipment data'),
    ('10000000-0000-0000-0000-000000000006', 'equipment.import', 'Equipment', 'Import equipment data'),
    ('10000000-0000-0000-0000-000000000007', 'equipment.qr.generate', 'Equipment', 'Generate QR codes'),
    -- Manifests
    ('20000000-0000-0000-0000-000000000001', 'manifest.view', 'Manifest', 'View manifests'),
    ('20000000-0000-0000-0000-000000000002', 'manifest.create', 'Manifest', 'Create manifests'),
    ('20000000-0000-0000-0000-000000000003', 'manifest.edit', 'Manifest', 'Edit manifests'),
    ('20000000-0000-0000-0000-000000000004', 'manifest.delete', 'Manifest', 'Delete manifests'),
    ('20000000-0000-0000-0000-000000000005', 'manifest.approve', 'Manifest', 'Approve manifests'),
    ('20000000-0000-0000-0000-000000000006', 'manifest.receive', 'Manifest', 'Receive manifests'),
    ('20000000-0000-0000-0000-000000000007', 'manifest.export', 'Manifest', 'Export manifests'),
    -- Reports
    ('30000000-0000-0000-0000-000000000001', 'reports.view', 'Reports', 'View reports'),
    ('30000000-0000-0000-0000-000000000002', 'reports.generate', 'Reports', 'Generate reports'),
    ('30000000-0000-0000-0000-000000000003', 'reports.export', 'Reports', 'Export reports'),
    -- Admin
    ('40000000-0000-0000-0000-000000000001', 'admin.users', 'Admin', 'Manage users'),
    ('40000000-0000-0000-0000-000000000002', 'admin.roles', 'Admin', 'Manage roles'),
    ('40000000-0000-0000-0000-000000000003', 'admin.locations', 'Admin', 'Manage locations'),
    ('40000000-0000-0000-0000-000000000004', 'admin.settings', 'Admin', 'System settings'),
    ('40000000-0000-0000-0000-000000000005', 'admin.audit', 'Admin', 'View audit logs');

-- Default Location Types
INSERT INTO LocationTypes (LocationTypeId, Name, Icon, Color) VALUES
    ('01000000-0000-0000-0000-000000000001', 'Base', 'Warehouse', '#4CAF50'),
    ('01000000-0000-0000-0000-000000000002', 'Vessel', 'Ship', '#2196F3'),
    ('01000000-0000-0000-0000-000000000003', 'Project Site', 'Construction', '#FF9800'),
    ('01000000-0000-0000-0000-000000000004', 'Container', 'Package', '#9C27B0'),
    ('01000000-0000-0000-0000-000000000005', 'Storage Yard', 'Grid', '#607D8B'),
    ('01000000-0000-0000-0000-000000000006', 'Workshop', 'Wrench', '#795548');

-- Default Equipment Categories
INSERT INTO EquipmentCategories (CategoryId, Name, Code, Icon, RequiresCertification, RequiresCalibration) VALUES
    ('02000000-0000-0000-0000-000000000001', 'Survey Equipment', 'SURV', 'Radar', TRUE, TRUE),
    ('02000000-0000-0000-0000-000000000002', 'Lifting Equipment', 'LIFT', 'Crane', TRUE, FALSE),
    ('02000000-0000-0000-0000-000000000003', 'Safety Equipment', 'SAFE', 'Shield', TRUE, FALSE),
    ('02000000-0000-0000-0000-000000000004', 'Tools', 'TOOL', 'Hammer', FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000005', 'Electronics', 'ELEC', 'Cpu', FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000006', 'Consumables', 'CONS', 'Package', FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000007', 'Containers & Cases', 'CONT', 'Box', FALSE, FALSE),
    ('02000000-0000-0000-0000-000000000008', 'ROV Equipment', 'ROV', 'Submarine', TRUE, TRUE),
    ('02000000-0000-0000-0000-000000000009', 'Diving Equipment', 'DIVE', 'Waves', TRUE, FALSE),
    ('02000000-0000-0000-0000-000000000010', 'Communication', 'COMM', 'Radio', FALSE, FALSE);

-- Update consumables category
UPDATE EquipmentCategories SET IsConsumable = TRUE WHERE Code = 'CONS';
```

---

## 5. API Endpoints

### 5.1 Authentication

```
POST   /api/auth/login              - Username/password login
POST   /api/auth/login/pin          - PIN-based login
POST   /api/auth/login/ad           - Azure AD login
POST   /api/auth/refresh            - Refresh access token
POST   /api/auth/logout             - Logout and revoke tokens
POST   /api/auth/change-password    - Change password
```

### 5.2 Equipment

```
GET    /api/equipment               - List equipment (with filtering, paging)
GET    /api/equipment/{id}          - Get equipment by ID
GET    /api/equipment/qr/{qrCode}   - Get equipment by QR code
GET    /api/equipment/asset/{assetNo} - Get by asset number
POST   /api/equipment               - Create new equipment
PUT    /api/equipment/{id}          - Update equipment
DELETE /api/equipment/{id}          - Delete equipment
POST   /api/equipment/{id}/photo    - Upload photo
GET    /api/equipment/{id}/history  - Get equipment history
POST   /api/equipment/import        - Import from Excel
GET    /api/equipment/export        - Export to Excel
POST   /api/equipment/qr/generate   - Generate QR code
```

### 5.3 Manifests

```
GET    /api/manifests               - List manifests
GET    /api/manifests/{id}          - Get manifest by ID
GET    /api/manifests/qr/{qrCode}   - Get by QR code
GET    /api/manifests/number/{num}  - Get by manifest number
POST   /api/manifests               - Create manifest
PUT    /api/manifests/{id}          - Update manifest
DELETE /api/manifests/{id}          - Delete manifest
POST   /api/manifests/{id}/submit   - Submit for approval
POST   /api/manifests/{id}/approve  - Approve manifest
POST   /api/manifests/{id}/reject   - Reject manifest
POST   /api/manifests/{id}/ship     - Mark as shipped
POST   /api/manifests/{id}/receive  - Receive manifest
POST   /api/manifests/{id}/items    - Add items to manifest
POST   /api/manifests/{id}/photo    - Upload photo
POST   /api/manifests/{id}/sign     - Add signature
GET    /api/manifests/{id}/pdf      - Generate PDF
GET    /api/manifests/{id}/excel    - Generate Excel
```

### 5.4 Sync

```
POST   /api/sync/pull               - Pull changes from server
POST   /api/sync/push               - Push changes to server
GET    /api/sync/status             - Get sync status
GET    /api/sync/conflicts          - Get pending conflicts
POST   /api/sync/conflicts/{id}/resolve - Resolve conflict
GET    /api/sync/full               - Full database sync
```

### 5.5 Admin

```
GET    /api/users                   - List users
POST   /api/users                   - Create user
PUT    /api/users/{id}              - Update user
GET    /api/roles                   - List roles
POST   /api/roles                   - Create role
PUT    /api/roles/{id}              - Update role
GET    /api/locations               - List locations
POST   /api/locations               - Create location
GET    /api/categories              - List categories
GET    /api/audit                   - Get audit log
```

---

## 6. Sync Protocol

### 6.1 Delta Sync Strategy

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                     SYNC PROTOCOL                                  â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                    â”‚
â”‚  1. CLIENT CONNECTS                                                â”‚
â”‚     â”œâ”€â”€ Send: LastSyncVersion, DeviceId, UserId                   â”‚
â”‚     â””â”€â”€ Server: Returns changes since LastSyncVersion              â”‚
â”‚                                                                    â”‚
â”‚  2. PULL CHANGES (Server â†’ Client)                                â”‚
â”‚     â”œâ”€â”€ Get all records where SyncVersion > LastSyncVersion       â”‚
â”‚     â”œâ”€â”€ Include: Equipment, Manifests, Locations, Categories      â”‚
â”‚     â””â”€â”€ Return: JSON with changes + NewSyncVersion                â”‚
â”‚                                                                    â”‚
â”‚  3. PUSH CHANGES (Client â†’ Server)                                â”‚
â”‚     â”œâ”€â”€ Send: Array of changed records with LocalSyncVersion      â”‚
â”‚     â”œâ”€â”€ Server validates and applies changes                       â”‚
â”‚     â”œâ”€â”€ If conflict: Add to SyncConflicts table                   â”‚
â”‚     â””â”€â”€ Return: Applied changes + Conflicts                        â”‚
â”‚                                                                    â”‚
â”‚  4. CONFLICT RESOLUTION                                            â”‚
â”‚     â”œâ”€â”€ Show conflict UI to user                                   â”‚
â”‚     â”œâ”€â”€ Options: Use Local, Use Server, Manual Merge              â”‚
â”‚     â””â”€â”€ Send resolution to server                                  â”‚
â”‚                                                                    â”‚
â”‚  5. OFFLINE QUEUE                                                  â”‚
â”‚     â”œâ”€â”€ When offline: Queue all changes locally                   â”‚
â”‚     â”œâ”€â”€ When online: Process queue in order                        â”‚
â”‚     â””â”€â”€ Retry with exponential backoff on failure                 â”‚
â”‚                                                                    â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 6.2 Sync Data Format

```json
{
  "syncRequest": {
    "deviceId": "device-uuid",
    "userId": "user-uuid",
    "lastSyncVersion": 12345,
    "tables": ["Equipment", "Manifests", "Locations"]
  }
}

{
  "syncResponse": {
    "newSyncVersion": 12400,
    "changes": {
      "equipment": [
        { "id": "...", "data": {...}, "operation": "Update", "syncVersion": 12350 }
      ],
      "manifests": [...],
      "locations": [...]
    },
    "conflicts": [
      { "conflictId": "...", "table": "Equipment", "recordId": "...", "localData": {...}, "serverData": {...} }
    ]
  }
}
```

---

## 7. QR Code Strategy

### 7.1 QR Code Format

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  QR CODE DATA FORMAT                                               â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                    â”‚
â”‚  EQUIPMENT QR:                                                     â”‚
â”‚  foseq:{assetNumber}                                                â”‚
â”‚  Example: foseq:EQ-2024-00001                                       â”‚
â”‚                                                                    â”‚
â”‚  MANIFEST QR:                                                      â”‚
â”‚  fosman:{manifestNumber}                                             â”‚
â”‚  Example: fosman:MN-2024-00001                                       â”‚
â”‚                                                                    â”‚
â”‚  LOCATION QR:                                                      â”‚
â”‚  fosloc:{locationCode}                                              â”‚
â”‚  Example: fosloc:LOC-ABU-WH01                                       â”‚
â”‚                                                                    â”‚
â”‚  Benefits of this format:                                          â”‚
â”‚  â€¢ Prefix identifies type instantly                                â”‚
â”‚  â€¢ Human readable                                                  â”‚
â”‚  â€¢ Short enough for small QR codes                                 â”‚
â”‚  â€¢ Can include version: foseq:v1:EQ-2024-00001                      â”‚
â”‚                                                                    â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 7.2 QR Code Generation

- Use **QRCoder** library (.NET) or **react-native-qrcode-svg** (Mobile)
- Generate 300x300 pixel PNG images
- Store in file storage (Cloudflare R2 or local)
- Print labels: 50x25mm recommended size

---

## 8. Asset Number Generation

### 8.1 Format

```
{PREFIX}-{YEAR}-{SEQUENCE}

Examples:
EQ-2024-00001     Standard equipment
CN-2024-00001     Consumable
LF-2024-00001     Lifting equipment
SV-2024-00001     Survey equipment
```

### 8.2 Technical Number Format

```
{CATEGORY}{YEAR}{SEQUENCE}

Examples:
SURV2400001       Survey equipment
LIFT2400001       Lifting equipment
```

---

## 9. Security

### 9.1 Authentication Flow

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  AUTHENTICATION METHODS                                            â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                    â”‚
â”‚  1. USERNAME/PASSWORD                                              â”‚
â”‚     â”œâ”€â”€ PBKDF2 password hashing                                   â”‚
â”‚     â”œâ”€â”€ Rate limiting (5 attempts, 15 min lockout)                â”‚
â”‚     â””â”€â”€ Returns: JWT access token + refresh token                 â”‚
â”‚                                                                    â”‚
â”‚  2. AZURE AD / SSO                                                â”‚
â”‚     â”œâ”€â”€ OAuth 2.0 / OpenID Connect                                â”‚
â”‚     â”œâ”€â”€ Auto-provision users on first login                       â”‚
â”‚     â””â”€â”€ Map AD groups to roles                                     â”‚
â”‚                                                                    â”‚
â”‚  3. PIN (Mobile Quick Access)                                     â”‚
â”‚     â”œâ”€â”€ 4-6 digit PIN                                             â”‚
â”‚     â”œâ”€â”€ Device-bound (requires initial full login)                â”‚
â”‚     â”œâ”€â”€ Auto-logout after 5 minutes inactivity                    â”‚
â”‚     â””â”€â”€ Limited to read + scan operations                          â”‚
â”‚                                                                    â”‚
â”‚  JWT TOKEN:                                                        â”‚
â”‚  â€¢ Access token: 15 minutes                                        â”‚
â”‚  â€¢ Refresh token: 7 days                                           â”‚
â”‚  â€¢ Claims: userId, roles, permissions, locationAccess             â”‚
â”‚                                                                    â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

---

## 10. Reporting

### 10.1 Available Reports

| Report | Description | Format |
|--------|-------------|--------|
| Equipment Register | Full inventory list | Excel, PDF |
| Location Inventory | Equipment at specific location | Excel, PDF |
| Equipment by Project | Project-assigned equipment | Excel, PDF |
| Certification Expiry | Items with expiring certs | Excel, PDF |
| Calibration Due | Items needing calibration | Excel, PDF |
| Service Due | Items needing service | Excel, PDF |
| Movement History | Equipment movements | Excel, PDF |
| Manifest Summary | Manifest activity | Excel, PDF |
| Stock Level | Consumables inventory | Excel, PDF |
| Discrepancy Report | Manifest discrepancies | Excel, PDF |
| Utilization Report | Equipment usage stats | Excel, PDF |
| Audit Trail | System activity log | Excel, PDF |

---

## 11. Scale Considerations

```
EXPECTED DATA VOLUMES:
â”œâ”€â”€ Onshore Bases: 100
â”œâ”€â”€ Vessels: 150
â”œâ”€â”€ Items per Location: 500-1000
â”œâ”€â”€ Total Equipment: ~175,000-250,000 items
â”œâ”€â”€ Manifests per Month: ~500-1000
â”œâ”€â”€ Users: ~500-1000
â””â”€â”€ Offline Duration: Hours (not days)

PERFORMANCE TARGETS:
â”œâ”€â”€ API Response: < 200ms (95th percentile)
â”œâ”€â”€ Search Results: < 500ms
â”œâ”€â”€ Sync (delta): < 5 seconds for typical changes
â”œâ”€â”€ Sync (full): < 2 minutes for 1000 items
â”œâ”€â”€ QR Scan to Display: < 1 second
â””â”€â”€ Report Generation: < 30 seconds
```

---

**Document Version:** 1.0  
**Last Updated:** January 2025  
**Author:** Fathom OS Development Team
