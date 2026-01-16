# Fathom OS Equipment API Documentation
## Version 1.0 | ASP.NET Core 8.0

---

## Overview

The Fathom OS Equipment API provides RESTful endpoints for managing equipment, manifests, locations, and users across offshore and onshore operations. The API supports offline-first mobile clients with delta synchronization.

**Base URL:**
- Development: `https://localhost:5001/api`
- Production: `https://your-server.company.com/api`

---

## Authentication

### JWT Bearer Token

All authenticated endpoints require a valid JWT token in the Authorization header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Token Lifetimes:**
- Access Token: 15 minutes
- Refresh Token: 7 days

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `Authorization` | Yes* | Bearer token for authenticated endpoints |
| `X-Device-Id` | Recommended | Unique device identifier for mobile clients |
| `X-App-Version` | Recommended | Client application version |
| `Content-Type` | Yes | `application/json` for POST/PUT requests |

---

## Authentication Endpoints

### POST /api/auth/login

Authenticate with username and password.

**Request:**
```json
{
  "username": "john.smith",
  "password": "SecurePassword123"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g...",
  "expiresIn": 900,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "username": "john.smith",
    "email": "john.smith@company.com",
    "firstName": "John",
    "lastName": "Smith",
    "roles": ["Deck Operator", "Store Keeper"],
    "permissions": ["equipment.view", "manifest.create"],
    "defaultLocationId": "550e8400-e29b-41d4-a716-446655440001"
  }
}
```

**Errors:**
- `401 Unauthorized`: Invalid credentials
- `423 Locked`: Account locked after failed attempts

---

### POST /api/auth/login/pin

Quick login with PIN (requires prior device registration).

**Request:**
```json
{
  "deviceId": "device-uuid-from-device-info",
  "pin": "1234"
}
```

**Response:** Same as `/api/auth/login`

---

### POST /api/auth/refresh

Refresh an expired access token.

**Request:**
```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "bmV3IHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 900
}
```

---

### POST /api/auth/logout

Invalidate the current session and refresh token.

**Request:**
```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2g..."
}
```

**Response:** `204 No Content`

---

### GET /api/auth/me

Get current user profile.

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "username": "john.smith",
  "email": "john.smith@company.com",
  "firstName": "John",
  "lastName": "Smith",
  "phone": "+971-50-123-4567",
  "roles": ["Deck Operator"],
  "permissions": ["equipment.view", "manifest.create"],
  "defaultLocation": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "name": "MV Explorer",
    "code": "VES-EXP-001"
  }
}
```

---

## Equipment Endpoints

### GET /api/equipment

List equipment with pagination and filtering.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number (default: 1) |
| `pageSize` | int | Items per page (default: 20, max: 100) |
| `search` | string | Search in name, asset number, serial number |
| `locationId` | guid | Filter by location |
| `categoryId` | guid | Filter by category |
| `status` | string | Filter by status |
| `condition` | string | Filter by condition |
| `requiresCertification` | bool | Filter certified items |
| `certificationExpiring` | int | Days until certification expires |

**Example:**
```http
GET /api/equipment?search=camera&locationId=550e8400...&status=Available&page=1&pageSize=20
```

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "assetNumber": "EQ-2024-00001",
      "qrCode": "foseq:EQ-2024-00001",
      "name": "ROV Camera System",
      "description": "High-definition underwater camera",
      "manufacturer": "SubC Imaging",
      "model": "Rayfin 4K",
      "serialNumber": "SN-12345",
      "category": {
        "id": "...",
        "name": "Survey Equipment",
        "code": "SURV"
      },
      "currentLocation": {
        "id": "...",
        "name": "MV Explorer",
        "code": "VES-EXP-001"
      },
      "status": "Available",
      "condition": "Good",
      "primaryPhotoUrl": "https://...",
      "certificationExpiryDate": "2025-06-15",
      "nextCalibrationDate": "2024-07-15"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

---

### GET /api/equipment/{id}

Get equipment by ID.

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440002",
  "assetNumber": "EQ-2024-00001",
  "sapNumber": "SAP123456",
  "techNumber": "TECH001",
  "serialNumber": "SN-12345",
  "qrCode": "foseq:EQ-2024-00001",
  "name": "ROV Camera System",
  "description": "High-definition underwater camera",
  "manufacturer": "SubC Imaging",
  "model": "Rayfin 4K",
  "category": { "id": "...", "name": "Survey Equipment", "code": "SURV" },
  "type": { "id": "...", "name": "ROV Camera" },
  "currentLocation": { "id": "...", "name": "MV Explorer", "code": "VES-EXP-001", "type": "Vessel" },
  "currentProject": { "id": "...", "name": "Pipeline Inspection 2024", "code": "PROJ-2024-001" },
  "status": "Available",
  "condition": "Good",
  "ownershipType": "Owned",
  "physical": {
    "weightKg": 15.5,
    "lengthCm": 45,
    "widthCm": 30,
    "heightCm": 25
  },
  "packaging": {
    "type": "Pelican Case",
    "weightKg": 8.2,
    "lengthCm": 60,
    "widthCm": 45,
    "heightCm": 35
  },
  "certification": {
    "required": true,
    "number": "CERT-2024-001",
    "body": "DNV",
    "expiryDate": "2025-06-15"
  },
  "calibration": {
    "required": true,
    "lastDate": "2024-01-15",
    "nextDate": "2024-07-15",
    "intervalDays": 180
  },
  "photos": [
    { "id": "...", "url": "https://...", "thumbnailUrl": "https://...", "type": "Main" }
  ],
  "createdAt": "2024-01-10T08:00:00Z",
  "updatedAt": "2024-01-20T10:30:00Z"
}
```

---

### GET /api/equipment/qr/{qrCode}

Get equipment by QR code value.

**Example:**
```http
GET /api/equipment/qr/foseq:EQ-2024-00001
```

---

### POST /api/equipment

Create new equipment.

**Request:**
```json
{
  "name": "Depth Sensor",
  "description": "High-precision depth sensor for ROV",
  "categoryId": "550e8400-e29b-41d4-a716-446655440003",
  "typeId": "550e8400-e29b-41d4-a716-446655440004",
  "serialNumber": "DS-2024-001",
  "manufacturer": "Paroscientific",
  "model": "8000-30K",
  "currentLocationId": "550e8400-e29b-41d4-a716-446655440001",
  "status": "Available",
  "condition": "New",
  "weightKg": 2.5,
  "lengthCm": 20,
  "widthCm": 15,
  "heightCm": 10,
  "purchaseDate": "2024-01-15",
  "purchasePrice": 15000.00,
  "purchaseCurrency": "USD",
  "requiresCertification": true,
  "requiresCalibration": true
}
```

**Response (201 Created):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440005",
  "assetNumber": "EQ-2024-00125",
  "qrCode": "foseq:EQ-2024-00125",
  "qrCodeImageUrl": "https://...",
  ...
}
```

---

### PUT /api/equipment/{id}

Update equipment.

**Request:** Same structure as POST, all fields optional.

---

### DELETE /api/equipment/{id}

Soft delete equipment.

**Response:** `204 No Content`

---

### GET /api/equipment/{id}/history

Get equipment movement and change history.

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "...",
      "eventType": "LocationChanged",
      "eventDescription": "Transferred from Abu Dhabi Base to MV Explorer",
      "fromLocation": { "id": "...", "name": "Abu Dhabi Base" },
      "toLocation": { "id": "...", "name": "MV Explorer" },
      "manifestId": "...",
      "performedBy": { "id": "...", "name": "Ahmed Hassan" },
      "performedAt": "2024-01-20T10:30:00Z"
    }
  ]
}
```

---

### POST /api/equipment/{id}/photo

Upload equipment photo.

**Request:** `multipart/form-data`

| Field | Type | Description |
|-------|------|-------------|
| `photo` | file | Image file (JPEG, PNG) |
| `photoType` | string | Main, Condition, Damage, Label |
| `caption` | string | Optional caption |

**Response (201 Created):**
```json
{
  "id": "...",
  "url": "https://...",
  "thumbnailUrl": "https://..."
}
```

---

## Manifest Endpoints

### GET /api/manifests

List manifests with filtering.

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `page` | int | Page number |
| `pageSize` | int | Items per page |
| `type` | string | Inward, Outward |
| `status` | string | Draft, Submitted, Approved, InTransit, Received |
| `fromLocationId` | guid | Filter by origin |
| `toLocationId` | guid | Filter by destination |
| `fromDate` | date | Created after |
| `toDate` | date | Created before |

---

### GET /api/manifests/{id}

Get manifest with items.

**Response (200 OK):**
```json
{
  "id": "...",
  "manifestNumber": "MN-2024-00001",
  "qrCode": "fosman:MN-2024-00001",
  "type": "Outward",
  "status": "InTransit",
  "fromLocation": {
    "id": "...",
    "name": "Abu Dhabi Base",
    "code": "BASE-ABU-001"
  },
  "fromContact": {
    "name": "Ahmed Hassan",
    "phone": "+971-50-123-4567",
    "email": "ahmed.hassan@company.com"
  },
  "toLocation": {
    "id": "...",
    "name": "MV Explorer",
    "code": "VES-EXP-001"
  },
  "toContact": {
    "name": "John Smith",
    "phone": "+971-50-987-6543"
  },
  "project": {
    "id": "...",
    "name": "Pipeline Inspection 2024"
  },
  "dates": {
    "created": "2024-01-20T08:00:00Z",
    "submitted": "2024-01-20T08:30:00Z",
    "approved": "2024-01-20T09:00:00Z",
    "shipped": "2024-01-20T10:00:00Z",
    "expectedArrival": "2024-01-22"
  },
  "shipping": {
    "method": "Sea",
    "carrier": "Supply Vessel SV-001",
    "trackingNumber": "TRK-12345"
  },
  "totals": {
    "items": 15,
    "weightKg": 250.5
  },
  "items": [
    {
      "id": "...",
      "equipment": {
        "id": "...",
        "assetNumber": "EQ-2024-00001",
        "name": "ROV Camera System"
      },
      "quantity": 1,
      "conditionAtSend": "Good",
      "isReceived": false
    }
  ],
  "signatures": {
    "sender": {
      "signature": "base64...",
      "signedAt": "2024-01-20T10:00:00Z",
      "signedBy": "Ahmed Hassan"
    }
  },
  "photos": [...]
}
```

---

### POST /api/manifests

Create new manifest.

**Request:**
```json
{
  "type": "Outward",
  "fromLocationId": "...",
  "fromContactName": "Ahmed Hassan",
  "fromContactPhone": "+971-50-123-4567",
  "fromContactEmail": "ahmed.hassan@company.com",
  "toLocationId": "...",
  "toContactName": "John Smith",
  "toContactPhone": "+971-50-987-6543",
  "projectId": "...",
  "expectedArrivalDate": "2024-01-22",
  "shippingMethod": "Sea",
  "notes": "Handle with care - fragile equipment"
}
```

---

### POST /api/manifests/{id}/items

Add items to manifest.

**Request:**
```json
{
  "items": [
    {
      "equipmentId": "...",
      "quantity": 1,
      "conditionAtSend": "Good",
      "conditionNotes": "Minor scratch on case"
    }
  ]
}
```

---

### Manifest Workflow Endpoints

| Endpoint | Description | Required Permission |
|----------|-------------|---------------------|
| `POST /api/manifests/{id}/submit` | Submit for approval | manifest.create |
| `POST /api/manifests/{id}/approve` | Approve manifest | manifest.approve |
| `POST /api/manifests/{id}/reject` | Reject manifest | manifest.approve |
| `POST /api/manifests/{id}/ship` | Mark as shipped | manifest.create |
| `POST /api/manifests/{id}/receive` | Record receipt | manifest.receive |

**Receive Request:**
```json
{
  "receivedItems": [
    {
      "itemId": "...",
      "receivedQuantity": 1,
      "conditionAtReceive": "Good",
      "receiptNotes": "Received in good condition"
    },
    {
      "itemId": "...",
      "receivedQuantity": 4,
      "conditionAtReceive": "Damaged",
      "hasDiscrepancy": true,
      "discrepancyType": "Damaged",
      "discrepancyNotes": "One unit has cracked screen"
    }
  ],
  "generalNotes": "Shipment received with minor issues",
  "signature": "base64-encoded-signature-image"
}
```

---

### POST /api/manifests/{id}/sign

Add digital signature.

**Request:**
```json
{
  "signatureType": "Sender",
  "signature": "base64-encoded-signature-image",
  "signerName": "Ahmed Hassan"
}
```

---

## Sync Endpoints

### POST /api/sync/pull

Pull changes from server.

**Request:**
```json
{
  "deviceId": "device-uuid",
  "lastSyncVersion": 12345,
  "tables": ["Equipment", "Manifests", "Locations", "Categories", "Projects"]
}
```

**Response (200 OK):**
```json
{
  "newSyncVersion": 12400,
  "changes": {
    "equipment": [
      {
        "id": "...",
        "operation": "Update",
        "data": {...},
        "syncVersion": 12350
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

---

### POST /api/sync/push

Push local changes to server.

**Request:**
```json
{
  "deviceId": "device-uuid",
  "changes": [
    {
      "table": "Equipment",
      "id": "...",
      "operation": "Update",
      "data": {...},
      "localTimestamp": "2024-01-20T10:30:00Z"
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "applied": 5,
  "conflicts": [
    {
      "conflictId": "...",
      "table": "Equipment",
      "recordId": "...",
      "localData": {...},
      "serverData": {...}
    }
  ]
}
```

---

### POST /api/sync/conflicts/{id}/resolve

Resolve sync conflict.

**Request:**
```json
{
  "resolution": "UseLocal",
  "mergedData": null
}
```

Resolution options: `UseLocal`, `UseServer`, `Merged`

---

## Lookup Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/lookups/locations` | All locations |
| `GET /api/lookups/locations?type=Vessel` | Filtered by type |
| `GET /api/lookups/categories` | Equipment categories |
| `GET /api/lookups/types?categoryId=...` | Types for category |
| `GET /api/lookups/projects` | Active projects |
| `GET /api/lookups/suppliers` | Suppliers list |
| `GET /api/lookups/conditions` | Condition values |
| `GET /api/lookups/statuses` | Status values |

---

## Reports Endpoints

### GET /api/reports/equipment-register

Generate equipment register report.

**Query Parameters:**
- `locationId` - Filter by location
- `categoryId` - Filter by category
- `format` - Response format: `json`, `excel`, `pdf`

---

### GET /api/reports/certification-expiry

Get items with expiring certifications.

**Query Parameters:**
- `days` - Days until expiry (default: 30)
- `locationId` - Filter by location

---

### GET /api/reports/calibration-due

Get items needing calibration.

---

### GET /api/reports/manifest-summary

Manifest activity summary.

**Query Parameters:**
- `fromDate` - Start date
- `toDate` - End date
- `locationId` - Filter by location

---

## Error Responses

All errors follow a consistent format:

```json
{
  "message": "Human-readable error message",
  "code": "ERROR_CODE",
  "details": {
    "field": "Additional context"
  }
}
```

**Common HTTP Status Codes:**

| Code | Description |
|------|-------------|
| 400 | Bad Request - Invalid input |
| 401 | Unauthorized - Authentication required |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource doesn't exist |
| 409 | Conflict - Resource conflict |
| 422 | Unprocessable Entity - Validation failed |
| 500 | Internal Server Error |

---

## Rate Limiting

- 100 requests per minute per user
- 1000 requests per minute per IP
- Sync endpoints: 10 requests per minute

Headers returned:
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642680000
```

---

## Versioning

API version is included in the URL path. Current version: `v1`

Future versions will be available at `/api/v2/...`

---

**Document Version:** 1.0  
**Last Updated:** January 2025  
**API Version:** 1.0.0
