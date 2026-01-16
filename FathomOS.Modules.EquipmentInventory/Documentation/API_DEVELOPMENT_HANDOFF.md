# Fathom OS Equipment API - Development Handoff
## For Backend Developer (ASP.NET Core 8.0)

---

## ðŸŽ¯ What You're Building

A REST API server that connects:
- **Desktop Module** (WPF .NET 8) - Already built âœ…
- **Mobile App** (React Native/Flutter) - To be built

The API handles equipment management, manifest workflows, user authentication, and synchronization.

---

## ðŸ“¦ What's Included in This Package

1. **SYSTEM_ARCHITECTURE.md** - Complete system design including:
   - Database schema (PostgreSQL/SQL Server) - All tables with columns
   - All API endpoints with request/response formats
   - Sync protocol specification
   - Authentication/Authorization requirements
   - Security requirements

2. **MOBILE_APP_REQUIREMENTS.md** - Shows how mobile app will consume your API

3. **Desktop Module Source** - Reference for data models and sync logic

---

## ðŸš€ Quick Start Instructions

### Technology Stack (Required)

```
Framework: ASP.NET Core 8.0
Database: PostgreSQL (dev: Supabase free tier) OR SQL Server
ORM: Entity Framework Core 8.0
Auth: JWT Bearer tokens
API Docs: Swagger/OpenAPI
Hosting (Dev): Railway.app / Render.com (free tier)
Hosting (Prod): IIS on Windows Server
```

### Priority Order for Development

**Phase 1: Foundation (Week 1)**
- [ ] Project setup with clean architecture
- [ ] Database connection and migrations
- [ ] JWT authentication (login, refresh, PIN login)
- [ ] Basic CRUD for Equipment
- [ ] Swagger documentation

**Phase 2: Core Features (Week 2-3)**
- [ ] All Equipment endpoints
- [ ] Location/Category/Type lookup endpoints
- [ ] Equipment search and filtering
- [ ] QR code lookup endpoint

**Phase 3: Manifests (Week 3-4)**
- [ ] Manifest CRUD operations
- [ ] Manifest status workflow
- [ ] Manifest item management
- [ ] Digital signature storage

**Phase 4: Sync (Week 4-5)**
- [ ] Delta sync endpoints
- [ ] SyncVersion tracking
- [ ] Conflict detection
- [ ] Batch operations

**Phase 5: Admin (Week 5-6)**
- [ ] User management
- [ ] Role/Permission management
- [ ] Audit logging
- [ ] Reports endpoints

---

## ðŸ“ Key API Endpoints Summary

### Authentication
```
POST /api/auth/login          - Username/password login
POST /api/auth/login/pin      - PIN login for mobile
POST /api/auth/refresh        - Refresh access token
POST /api/auth/logout         - Invalidate tokens
GET  /api/auth/me             - Current user profile
```

### Equipment
```
GET    /api/equipment                    - List with pagination/filters
GET    /api/equipment/{id}               - Get by ID
GET    /api/equipment/qr/{qrCode}        - Get by QR code
POST   /api/equipment                    - Create new
PUT    /api/equipment/{id}               - Update
DELETE /api/equipment/{id}               - Soft delete
POST   /api/equipment/batch              - Batch create/update
GET    /api/equipment/search?q={query}   - Full-text search
```

### Manifests
```
GET    /api/manifests                    - List with filters
GET    /api/manifests/{id}               - Get with items
POST   /api/manifests                    - Create draft
PUT    /api/manifests/{id}               - Update
POST   /api/manifests/{id}/submit        - Submit for approval
POST   /api/manifests/{id}/approve       - Approve
POST   /api/manifests/{id}/reject        - Reject
POST   /api/manifests/{id}/ship          - Mark shipped
POST   /api/manifests/{id}/receive       - Record receipt
POST   /api/manifests/{id}/sign          - Add signature
```

### Sync
```
POST   /api/sync/pull         - Get changes since version
POST   /api/sync/push         - Upload local changes
GET    /api/sync/status       - Sync status and version
```

### Lookups
```
GET    /api/locations         - All locations
GET    /api/categories        - Equipment categories
GET    /api/types?categoryId= - Types for category
GET    /api/projects          - Active projects
GET    /api/suppliers         - Suppliers list
```

---

## ðŸ—„ï¸ Database Quick Reference

**Core Tables:**
- Users, Roles, UserRoles, RolePermissions
- Locations (hierarchical with ParentLocationId)
- EquipmentCategories, EquipmentTypes
- Projects, Suppliers

**Main Tables:**
- Equipment (~50 columns - see full schema in SYSTEM_ARCHITECTURE.md)
- EquipmentPhotos, EquipmentDocuments, EquipmentHistory
- Manifests, ManifestItems, ManifestPhotos

**Sync Tables:**
- SyncLog, ClientSyncState

---

## ðŸ” Authentication Requirements

1. **JWT Access Token**: 15-minute expiry, returned on login
2. **Refresh Token**: 7-day expiry, stored in database
3. **PIN Login**: 4-6 digit PIN for quick mobile access
4. **Device Registration**: Track deviceId for mobile clients

**Required Headers:**
```http
Authorization: Bearer {accessToken}
X-Device-Id: {deviceUuid}
X-App-Version: {version}
```

---

## ðŸ“Š Sync Protocol

**Delta Sync Flow:**
1. Client sends: `POST /api/sync/pull { lastSyncVersion: 12345 }`
2. Server returns all records with `SyncVersion > 12345`
3. Client applies changes locally
4. Client sends local changes: `POST /api/sync/push { changes: [...] }`
5. Server processes and returns conflicts if any

**Every table has:**
- `SyncVersion BIGINT` - Incremented on each update
- `IsActive BOOLEAN` - Soft delete flag
- `CreatedAt`, `UpdatedAt` - Timestamps

---

## âœ… Acceptance Criteria

The API is complete when:

1. [ ] All endpoints from SYSTEM_ARCHITECTURE.md are implemented
2. [ ] Desktop module can sync equipment and manifests
3. [ ] Mobile app can authenticate and perform all operations
4. [ ] Offline changes sync correctly without data loss
5. [ ] Conflicts are detected and reported
6. [ ] All endpoints have Swagger documentation
7. [ ] Unit tests cover critical paths
8. [ ] Can handle 100 concurrent users

---

## ðŸ“ž Questions?

Refer to:
- **SYSTEM_ARCHITECTURE.md** - Complete technical specification
- **Desktop Source Code** - See `/Data/LocalDatabaseService.cs` for sync logic
- **Models** - See `/Models/*.cs` for exact data structures

---

**Document Version:** 1.0  
**Created:** January 2025  
**For:** Backend Development Team
