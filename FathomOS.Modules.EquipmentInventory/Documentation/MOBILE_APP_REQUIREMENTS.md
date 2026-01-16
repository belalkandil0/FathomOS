# Fathom OS Equipment Manager - Mobile Application Requirements
## Version 1.0 | For Cross-Platform Development (React Native / Flutter)

---

## 1. Executive Summary

This document provides complete requirements for building the Fathom OS Equipment Manager mobile application. The mobile app works in conjunction with the Desktop Module and Central API Server to provide field operations capabilities for managing equipment and inventory across offshore and onshore locations.

### Key Capabilities
- QR code scanning for equipment lookup and manifest operations
- Create and manage inward/outward manifests
- Register new equipment with photos
- Offline-first operation with intelligent sync
- Digital signatures for manifest sign-off
- Real-time notifications and alerts

### Target Platforms
- iOS (14.0+)
- Android (8.0+, API Level 26+)
- Recommended: **React Native** or **Flutter**

---

## 2. Technology Stack Recommendation

### Option A: React Native (Recommended)

```
Framework: React Native 0.73+
State Management: Redux Toolkit + RTK Query
Navigation: React Navigation 6
Database: WatermelonDB (SQLite wrapper, great for sync)
QR Scanner: react-native-camera-kit or react-native-vision-camera
HTTP Client: Axios
Forms: React Hook Form
UI Components: React Native Paper or NativeBase
Signature: react-native-signature-canvas
Push Notifications: Firebase Cloud Messaging
```

### Option B: Flutter

```
Framework: Flutter 3.16+
State Management: Riverpod or BLoC
Database: Drift (SQLite)
QR Scanner: mobile_scanner
HTTP Client: Dio
Forms: flutter_form_builder
UI Components: Material Design 3
Signature: signature
Push Notifications: firebase_messaging
```

---

## 3. API Integration

### 3.1 Base Configuration

```
DEVELOPMENT:
Base URL: https://s7-equipment-api.up.railway.app/api
(Or your development server URL)

PRODUCTION:
Base URL: https://your-server.company.com/api
```

### 3.2 Authentication

**Login Endpoint:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "john.smith",
  "password": "SecurePassword123"
}

Response:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresIn": 900,
  "user": {
    "userId": "uuid",
    "username": "john.smith",
    "email": "john.smith@company.com",
    "firstName": "John",
    "lastName": "Smith",
    "roles": ["Deck Operator", "Store Keeper"],
    "permissions": ["equipment.view", "manifest.create", ...],
    "defaultLocationId": "uuid",
    "profilePhotoUrl": "https://..."
  }
}
```

**PIN Login:**
```http
POST /api/auth/login/pin
Content-Type: application/json

{
  "deviceId": "device-uuid-from-device-info",
  "pin": "1234"
}
```

**Token Refresh:**
```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Headers for Authenticated Requests:**
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
X-Device-Id: device-uuid
X-App-Version: 1.0.0
```

### 3.3 Equipment Endpoints

**Get Equipment by QR Code:**
```http
GET /api/equipment/qr/{qrCodeValue}

Example: GET /api/equipment/qr/foseq:EQ-2024-00001

Response:
{
  "equipmentId": "uuid",
  "assetNumber": "EQ-2024-00001",
  "sapNumber": "SAP123456",
  "techNumber": "TECH001",
  "serialNumber": "SN-12345",
  "qrCode": "foseq:EQ-2024-00001",
  "name": "ROV Camera System",
  "description": "High-definition underwater camera",
  "manufacturer": "SubC Imaging",
  "model": "Rayfin 4K",
  "category": {
    "categoryId": "uuid",
    "name": "Survey Equipment",
    "code": "SURV"
  },
  "type": {
    "typeId": "uuid",
    "name": "ROV Camera"
  },
  "currentLocation": {
    "locationId": "uuid",
    "name": "MV Explorer",
    "code": "VES-EXP-001",
    "type": "Vessel"
  },
  "currentProject": {
    "projectId": "uuid",
    "name": "Pipeline Inspection 2024",
    "code": "PROJ-2024-001"
  },
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
    {
      "photoId": "uuid",
      "url": "https://...",
      "thumbnailUrl": "https://...",
      "type": "Main"
    }
  ],
  "lastUpdated": "2024-01-20T10:30:00Z"
}
```

**Search Equipment:**
```http
GET /api/equipment?search=camera&location={locationId}&status=Available&page=1&pageSize=20

Response:
{
  "items": [...],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

**Create New Equipment:**
```http
POST /api/equipment
Content-Type: application/json

{
  "name": "Depth Sensor",
  "description": "High-precision depth sensor for ROV",
  "categoryId": "uuid",
  "typeId": "uuid",
  "serialNumber": "DS-2024-001",
  "manufacturer": "Paroscientific",
  "model": "8000-30K",
  "currentLocationId": "uuid",
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

Response:
{
  "equipmentId": "uuid",
  "assetNumber": "EQ-2024-00125",
  "qrCode": "foseq:EQ-2024-00125",
  "qrCodeImageUrl": "https://...",
  ...
}
```

**Upload Equipment Photo:**
```http
POST /api/equipment/{equipmentId}/photo
Content-Type: multipart/form-data

photo: [binary file]
photoType: "Main" | "Condition" | "Damage" | "Label"
caption: "Front view"

Response:
{
  "photoId": "uuid",
  "url": "https://...",
  "thumbnailUrl": "https://..."
}
```

### 3.4 Manifest Endpoints

**Get Manifest by QR Code:**
```http
GET /api/manifests/qr/{qrCodeValue}

Example: GET /api/manifests/qr/fosman:MN-2024-00001

Response:
{
  "manifestId": "uuid",
  "manifestNumber": "MN-2024-00001",
  "qrCode": "fosman:MN-2024-00001",
  "type": "Outward",
  "status": "InTransit",
  "fromLocation": {
    "locationId": "uuid",
    "name": "Abu Dhabi Base",
    "code": "BASE-ABU-001"
  },
  "fromContact": {
    "name": "Ahmed Hassan",
    "phone": "+971-50-123-4567",
    "email": "ahmed.hassan@company.com"
  },
  "toLocation": {
    "locationId": "uuid",
    "name": "MV Explorer",
    "code": "VES-EXP-001"
  },
  "toContact": {
    "name": "John Smith",
    "phone": "+971-50-987-6543",
    "email": "john.smith@company.com"
  },
  "project": {
    "projectId": "uuid",
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
    "weightKg": 250.5,
    "volumeCm3": 500000
  },
  "items": [
    {
      "itemId": "uuid",
      "equipment": {
        "equipmentId": "uuid",
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

**Create Manifest:**
```http
POST /api/manifests
Content-Type: application/json

{
  "type": "Outward",
  "fromLocationId": "uuid",
  "fromContactName": "Ahmed Hassan",
  "fromContactPhone": "+971-50-123-4567",
  "fromContactEmail": "ahmed.hassan@company.com",
  "toLocationId": "uuid",
  "toContactName": "John Smith",
  "toContactPhone": "+971-50-987-6543",
  "toContactEmail": "john.smith@company.com",
  "projectId": "uuid",
  "expectedArrivalDate": "2024-01-22",
  "shippingMethod": "Sea",
  "notes": "Handle with care - fragile equipment"
}

Response:
{
  "manifestId": "uuid",
  "manifestNumber": "MN-2024-00125",
  "qrCode": "fosman:MN-2024-00125",
  "status": "Draft"
}
```

**Add Items to Manifest:**
```http
POST /api/manifests/{manifestId}/items
Content-Type: application/json

{
  "items": [
    {
      "equipmentId": "uuid",
      "quantity": 1,
      "conditionAtSend": "Good",
      "conditionNotes": "Minor scratch on case"
    },
    {
      "equipmentId": "uuid",
      "quantity": 5,
      "conditionAtSend": "New"
    }
  ]
}
```

**Submit Manifest for Approval:**
```http
POST /api/manifests/{manifestId}/submit
```

**Receive Manifest (Inward):**
```http
POST /api/manifests/{manifestId}/receive
Content-Type: application/json

{
  "receivedItems": [
    {
      "itemId": "uuid",
      "receivedQuantity": 1,
      "conditionAtReceive": "Good",
      "receiptNotes": "Received in good condition"
    },
    {
      "itemId": "uuid",
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

**Upload Manifest Photo:**
```http
POST /api/manifests/{manifestId}/photo
Content-Type: multipart/form-data

photo: [binary file]
photoType: "General" | "Packaging" | "Loading" | "Damage" | "Receipt"
itemId: "uuid" (optional, for item-specific photos)
caption: "Loading at dock"
```

**Add Signature:**
```http
POST /api/manifests/{manifestId}/sign
Content-Type: application/json

{
  "signatureType": "Sender" | "Receiver" | "Approver",
  "signature": "base64-encoded-signature-image",
  "signerName": "John Smith"
}
```

**Generate Manifest PDF:**
```http
GET /api/manifests/{manifestId}/pdf

Response: Binary PDF file
Content-Type: application/pdf
```

### 3.5 Sync Endpoints

**Pull Changes:**
```http
POST /api/sync/pull
Content-Type: application/json

{
  "deviceId": "device-uuid",
  "lastSyncVersion": 12345,
  "tables": ["Equipment", "Manifests", "Locations", "Categories", "Projects"]
}

Response:
{
  "newSyncVersion": 12400,
  "changes": {
    "equipment": [
      {
        "id": "uuid",
        "operation": "Insert" | "Update" | "Delete",
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

**Push Changes:**
```http
POST /api/sync/push
Content-Type: application/json

{
  "deviceId": "device-uuid",
  "changes": [
    {
      "table": "Equipment",
      "id": "uuid",
      "operation": "Update",
      "data": {...},
      "localTimestamp": "2024-01-20T10:30:00Z"
    }
  ]
}

Response:
{
  "applied": 5,
  "conflicts": [
    {
      "conflictId": "uuid",
      "table": "Equipment",
      "recordId": "uuid",
      "localData": {...},
      "serverData": {...}
    }
  ]
}
```

**Resolve Conflict:**
```http
POST /api/sync/conflicts/{conflictId}/resolve
Content-Type: application/json

{
  "resolution": "UseLocal" | "UseServer" | "Merged",
  "mergedData": {...}  // Only if resolution is "Merged"
}
```

### 3.6 Lookup Endpoints

**Get Locations:**
```http
GET /api/locations?type=Vessel&region={regionId}

Response:
{
  "items": [
    {
      "locationId": "uuid",
      "name": "MV Explorer",
      "code": "VES-EXP-001",
      "type": "Vessel",
      "parentLocation": {...},
      "contactPerson": "John Smith",
      "contactPhone": "+971-50-123-4567"
    }
  ]
}
```

**Get Categories:**
```http
GET /api/categories

Response:
{
  "items": [
    {
      "categoryId": "uuid",
      "name": "Survey Equipment",
      "code": "SURV",
      "icon": "Radar",
      "color": "#4CAF50",
      "isConsumable": false,
      "requiresCertification": true,
      "children": [...]
    }
  ]
}
```

**Get Equipment Types:**
```http
GET /api/equipment-types?categoryId={categoryId}
```

**Get Projects:**
```http
GET /api/projects?status=Active
```

---

## 4. Local Database Schema (SQLite)

The mobile app must maintain a local SQLite database for offline operation. Here's the recommended schema:

```sql
-- Sync metadata
CREATE TABLE SyncMeta (
    key TEXT PRIMARY KEY,
    value TEXT
);
-- Store: lastSyncVersion, lastSyncTime, deviceId

-- Equipment (mirror of server)
CREATE TABLE Equipment (
    equipmentId TEXT PRIMARY KEY,
    assetNumber TEXT UNIQUE,
    sapNumber TEXT,
    techNumber TEXT,
    serialNumber TEXT,
    qrCode TEXT UNIQUE,
    name TEXT NOT NULL,
    description TEXT,
    categoryId TEXT,
    typeId TEXT,
    manufacturer TEXT,
    model TEXT,
    currentLocationId TEXT,
    currentProjectId TEXT,
    status TEXT,
    condition TEXT,
    weightKg REAL,
    lengthCm REAL,
    widthCm REAL,
    heightCm REAL,
    packagingType TEXT,
    packagingWeightKg REAL,
    primaryPhotoUrl TEXT,
    qrCodeImageUrl TEXT,
    certificationExpiryDate TEXT,
    nextCalibrationDate TEXT,
    isConsumable INTEGER DEFAULT 0,
    quantityOnHand REAL DEFAULT 1,
    dataJson TEXT,  -- Full JSON for offline display
    syncVersion INTEGER DEFAULT 0,
    isModifiedLocally INTEGER DEFAULT 0,
    lastModifiedAt TEXT
);

-- Manifests
CREATE TABLE Manifests (
    manifestId TEXT PRIMARY KEY,
    manifestNumber TEXT UNIQUE,
    qrCode TEXT UNIQUE,
    type TEXT,
    status TEXT,
    fromLocationId TEXT,
    toLocationId TEXT,
    projectId TEXT,
    dataJson TEXT,
    syncVersion INTEGER DEFAULT 0,
    isModifiedLocally INTEGER DEFAULT 0,
    lastModifiedAt TEXT
);

-- Manifest Items
CREATE TABLE ManifestItems (
    itemId TEXT PRIMARY KEY,
    manifestId TEXT,
    equipmentId TEXT,
    quantity REAL,
    conditionAtSend TEXT,
    isReceived INTEGER DEFAULT 0,
    dataJson TEXT,
    syncVersion INTEGER DEFAULT 0,
    FOREIGN KEY (manifestId) REFERENCES Manifests(manifestId)
);

-- Locations (lookup)
CREATE TABLE Locations (
    locationId TEXT PRIMARY KEY,
    name TEXT,
    code TEXT,
    type TEXT,
    parentLocationId TEXT,
    contactPerson TEXT,
    contactPhone TEXT,
    dataJson TEXT,
    syncVersion INTEGER DEFAULT 0
);

-- Categories (lookup)
CREATE TABLE Categories (
    categoryId TEXT PRIMARY KEY,
    name TEXT,
    code TEXT,
    parentCategoryId TEXT,
    icon TEXT,
    color TEXT,
    isConsumable INTEGER,
    syncVersion INTEGER DEFAULT 0
);

-- Equipment Types (lookup)
CREATE TABLE EquipmentTypes (
    typeId TEXT PRIMARY KEY,
    categoryId TEXT,
    name TEXT,
    code TEXT,
    syncVersion INTEGER DEFAULT 0
);

-- Projects (lookup)
CREATE TABLE Projects (
    projectId TEXT PRIMARY KEY,
    name TEXT,
    code TEXT,
    status TEXT,
    locationId TEXT,
    dataJson TEXT,
    syncVersion INTEGER DEFAULT 0
);

-- Offline Queue
CREATE TABLE OfflineQueue (
    queueId TEXT PRIMARY KEY,
    tableName TEXT NOT NULL,
    recordId TEXT NOT NULL,
    operation TEXT NOT NULL,  -- Insert, Update, Delete
    dataJson TEXT,
    createdAt TEXT,
    attempts INTEGER DEFAULT 0,
    lastError TEXT
);

-- Pending Photos (to upload when online)
CREATE TABLE PendingPhotos (
    photoId TEXT PRIMARY KEY,
    entityType TEXT,  -- Equipment, Manifest
    entityId TEXT,
    localPath TEXT,
    photoType TEXT,
    caption TEXT,
    createdAt TEXT,
    uploadAttempts INTEGER DEFAULT 0
);

-- Indexes
CREATE INDEX idx_equipment_qr ON Equipment(qrCode);
CREATE INDEX idx_equipment_asset ON Equipment(assetNumber);
CREATE INDEX idx_equipment_location ON Equipment(currentLocationId);
CREATE INDEX idx_equipment_modified ON Equipment(isModifiedLocally);
CREATE INDEX idx_manifest_qr ON Manifests(qrCode);
CREATE INDEX idx_manifest_status ON Manifests(status);
```

---

## 5. Screen Specifications

### 5.1 Authentication Screens

#### 5.1.1 Login Screen
```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚         S7 FATHOM               â”‚
â”‚    Equipment Manager            â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ‘¤ Username             â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ”’ Password             â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â˜ Remember me                 â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚       LOGIN             â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€ OR â”€â”€â”€â”€â”€â”€â”€            â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚  Sign in with SSO      â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Server: production â–¼          â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

**Features:**
- Username/password fields with validation
- "Remember me" to store username
- SSO button for Azure AD authentication
- Server selection (Dev/Production) for testing
- Biometric login option after first login

#### 5.1.2 PIN Entry Screen
```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚         Welcome Back            â”‚
â”‚         John Smith              â”‚
â”‚                                 â”‚
â”‚      Enter your PIN             â”‚
â”‚                                 â”‚
â”‚        â— â— â—‹ â—‹                  â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”  â”Œâ”€â”€â”€â”  â”Œâ”€â”€â”€â”          â”‚
â”‚  â”‚ 1 â”‚  â”‚ 2 â”‚  â”‚ 3 â”‚          â”‚
â”‚  â””â”€â”€â”€â”˜  â””â”€â”€â”€â”˜  â””â”€â”€â”€â”˜          â”‚
â”‚  â”Œâ”€â”€â”€â”  â”Œâ”€â”€â”€â”  â”Œâ”€â”€â”€â”          â”‚
â”‚  â”‚ 4 â”‚  â”‚ 5 â”‚  â”‚ 6 â”‚          â”‚
â”‚  â””â”€â”€â”€â”˜  â””â”€â”€â”€â”˜  â””â”€â”€â”€â”˜          â”‚
â”‚  â”Œâ”€â”€â”€â”  â”Œâ”€â”€â”€â”  â”Œâ”€â”€â”€â”          â”‚
â”‚  â”‚ 7 â”‚  â”‚ 8 â”‚  â”‚ 9 â”‚          â”‚
â”‚  â””â”€â”€â”€â”˜  â””â”€â”€â”€â”˜  â””â”€â”€â”€â”˜          â”‚
â”‚  â”Œâ”€â”€â”€â”  â”Œâ”€â”€â”€â”  â”Œâ”€â”€â”€â”          â”‚
â”‚  â”‚ âŒ« â”‚  â”‚ 0 â”‚  â”‚ âœ“ â”‚          â”‚
â”‚  â””â”€â”€â”€â”˜  â””â”€â”€â”€â”˜  â””â”€â”€â”€â”˜          â”‚
â”‚                                 â”‚
â”‚  Use different account          â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.2 Home / Dashboard

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â˜°  S7 Equipment      ðŸ”” ðŸ‘¤     â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  Good morning, John!            â”‚
â”‚  MV Explorer                    â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚     ðŸ“· SCAN QR CODE     â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Quick Actions                  â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”‚
â”‚  â”‚ ðŸ“¤       â”‚ â”‚ ðŸ“¥       â”‚    â”‚
â”‚  â”‚ Outward  â”‚ â”‚ Inward   â”‚    â”‚
â”‚  â”‚ Manifest â”‚ â”‚ Manifest â”‚    â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”‚
â”‚  â”‚ âž•       â”‚ â”‚ ðŸ”       â”‚    â”‚
â”‚  â”‚ New      â”‚ â”‚ Search   â”‚    â”‚
â”‚  â”‚Equipment â”‚ â”‚Equipment â”‚    â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â”‚
â”‚                                 â”‚
â”‚  Recent Activity                â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“¦ MN-2024-00125       â”‚   â”‚
â”‚  â”‚ In Transit â†’ MV Explorerâ”‚   â”‚
â”‚  â”‚ 15 items â€¢ 2 hours ago  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“¦ MN-2024-00124       â”‚   â”‚
â”‚  â”‚ Received âœ“              â”‚   â”‚
â”‚  â”‚ 8 items â€¢ Yesterday     â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ ðŸ     ðŸ“¦    âž•    ðŸ“‹    âš™ï¸    â”‚
â”‚ Home  Equip  Scan  Manifests   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.3 QR Scanner Screen

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Scan QR Code         ðŸ’¡     â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â”‚    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”        â”‚   â”‚
â”‚  â”‚    â”‚           â”‚        â”‚   â”‚
â”‚  â”‚    â”‚  CAMERA   â”‚        â”‚   â”‚
â”‚  â”‚    â”‚  PREVIEW  â”‚        â”‚   â”‚
â”‚  â”‚    â”‚           â”‚        â”‚   â”‚
â”‚  â”‚    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜        â”‚   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Position QR code in frame      â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€ OR â”€â”€â”€â”€â”€â”€â”€            â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚  Enter Code Manually    â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜

After scanning:
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  âœ“ Code Detected!              â”‚
â”‚  foseq:EQ-2024-00001            â”‚
â”‚                                 â”‚
â”‚  Loading equipment details...   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.4 Equipment Details Screen

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Equipment Details    â‹®      â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚      ðŸ“· PHOTO           â”‚   â”‚
â”‚  â”‚   ROV Camera System     â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  EQ-2024-00001                  â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”‚
â”‚  â”‚ Status: Available  âœ“   â”‚    â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â”‚
â”‚                                 â”‚
â”‚  ðŸ“ Location                    â”‚
â”‚  MV Explorer > Deck Storage     â”‚
â”‚                                 â”‚
â”‚  ðŸ“‹ Project                     â”‚
â”‚  Pipeline Inspection 2024       â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€   â”‚
â”‚                                 â”‚
â”‚  Details                        â”‚
â”‚  Serial: SN-12345              â”‚
â”‚  Manufacturer: SubC Imaging     â”‚
â”‚  Model: Rayfin 4K               â”‚
â”‚  Condition: Good                â”‚
â”‚                                 â”‚
â”‚  Certification                  â”‚
â”‚  âš ï¸ Expires: Jun 15, 2025      â”‚
â”‚  (147 days remaining)           â”‚
â”‚                                 â”‚
â”‚  Calibration                    â”‚
â”‚  âš ï¸ Due: Jul 15, 2024          â”‚
â”‚  (Last: Jan 15, 2024)           â”‚
â”‚                                 â”‚
â”‚  Physical                       â”‚
â”‚  Weight: 15.5 kg                â”‚
â”‚  Dimensions: 45Ã—30Ã—25 cm        â”‚
â”‚                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚  Add to Outward Manifest  â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚  View History             â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

**Context Menu (â‹®):**
- Update Status
- Update Condition
- Add Photo
- Report Damage
- View Documents
- Print QR Label

### 5.5 Equipment List Screen

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Equipment           ðŸ” â«¶    â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ” Search equipment...  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Filters: Location â–¼ Status â–¼  â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“· ROV Camera System    â”‚   â”‚
â”‚  â”‚ EQ-2024-00001           â”‚   â”‚
â”‚  â”‚ Available â€¢ MV Explorer â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“· Depth Sensor         â”‚   â”‚
â”‚  â”‚ EQ-2024-00002           â”‚   â”‚
â”‚  â”‚ In Use â€¢ MV Explorer    â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“· USBL Beacon          â”‚   â”‚
â”‚  â”‚ EQ-2024-00003           â”‚   â”‚
â”‚  â”‚ âš ï¸ Calibration Due      â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  ... (scrollable list)         â”‚
â”‚                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  Showing 50 of 523 items       â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.6 New Equipment Registration

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Register Equipment   Save   â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  ðŸ“· Add Photos                  â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”      â”‚
â”‚  â”‚  +  â”‚ â”‚     â”‚ â”‚     â”‚      â”‚
â”‚  â””â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”˜      â”‚
â”‚                                 â”‚
â”‚  Basic Information              â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Name *                         â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Category *                     â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Select category      â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Type                          â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Select type          â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Serial Number                  â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Manufacturer                   â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Model                         â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Description                    â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Location & Status              â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Location *                     â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ MV Explorer          â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Status                         â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Available            â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Condition                      â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ New                  â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Physical Properties            â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Weight (kg)    Length (cm)     â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚          â”‚   â”‚          â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Width (cm)     Height (cm)     â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚          â”‚   â”‚          â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Packaging                      â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Packaging Type                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Pelican Case         â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  (More fields...)               â”‚
â”‚                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚    Register Equipment     â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

**After Registration:**
- Display generated Asset Number
- Display generated QR Code
- Option to print QR label
- Option to add to manifest

### 5.7 Create Outward Manifest

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  New Outward Manifest  Save  â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  Step 1 of 3: Details          â”‚
â”‚  â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•   â”‚
â”‚                                 â”‚
â”‚  From Location *                â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Abu Dhabi Base       â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Sender Contact                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ‘¤ Ahmed Hassan         â”‚   â”‚
â”‚  â”‚ ðŸ“± +971-50-123-4567     â”‚   â”‚
â”‚  â”‚ âœ‰ï¸ ahmed@company.com    â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚  [Edit Contact]                 â”‚
â”‚                                 â”‚
â”‚  To Location *                  â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ MV Explorer          â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Receiver Contact               â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ‘¤ Name                 â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“± Phone                â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ âœ‰ï¸ Email                â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Project (Optional)             â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Pipeline Inspection  â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Expected Arrival               â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“… Jan 22, 2024         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Shipping Method                â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Sea                  â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Notes                          â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚     Next: Add Items       â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.8 Add Items to Manifest

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Add Items             Done  â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  Step 2 of 3: Items            â”‚
â”‚  â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚     ðŸ“· SCAN EQUIPMENT   â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€ OR â”€â”€â”€â”€â”€â”€â”€            â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚  ðŸ” Search to Add       â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Items Added (3)                â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“· ROV Camera System    â”‚   â”‚
â”‚  â”‚ EQ-2024-00001           â”‚   â”‚
â”‚  â”‚ Qty: 1 â€¢ Good           â”‚   â”‚
â”‚  â”‚               [ðŸ“·] [ðŸ—‘] â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“· Depth Sensor         â”‚   â”‚
â”‚  â”‚ EQ-2024-00002           â”‚   â”‚
â”‚  â”‚ Qty: 1 â€¢ Good           â”‚   â”‚
â”‚  â”‚               [ðŸ“·] [ðŸ—‘] â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ“¦ Cable Assemblies     â”‚   â”‚
â”‚  â”‚ EQ-2024-00010           â”‚   â”‚
â”‚  â”‚ Qty: 5 â€¢ New            â”‚   â”‚
â”‚  â”‚               [ðŸ“·] [ðŸ—‘] â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚  Total: 3 items (7 units)      â”‚
â”‚  Weight: 35.5 kg               â”‚
â”‚                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚    Next: Review & Sign    â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

**When scanning equipment:**
```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚        Add to Manifest          â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  ðŸ“· ROV Camera System           â”‚
â”‚  EQ-2024-00001                  â”‚
â”‚                                 â”‚
â”‚  Quantity                       â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚    [ - ]    1    [ + ]  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Condition at Send              â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Good                 â–¼  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Notes                          â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Minor scratch on case   â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  ðŸ“· Add Photo                   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”‚
â”‚  â”‚  Cancel  â”‚ â”‚  Add     â”‚    â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.9 Manifest Review & Signature

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Review Manifest      Submit â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  Step 3 of 3: Review           â”‚
â”‚  â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•   â”‚
â”‚                                 â”‚
â”‚  Manifest: MN-2024-00125       â”‚
â”‚                                 â”‚
â”‚  From                           â”‚
â”‚  Abu Dhabi Base                 â”‚
â”‚  Ahmed Hassan                   â”‚
â”‚  +971-50-123-4567              â”‚
â”‚                                 â”‚
â”‚  To                             â”‚
â”‚  MV Explorer                    â”‚
â”‚  John Smith                     â”‚
â”‚  +971-50-987-6543              â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Items (3)                      â”‚
â”‚  â€¢ ROV Camera System (1)       â”‚
â”‚  â€¢ Depth Sensor (1)            â”‚
â”‚  â€¢ Cable Assemblies (5)        â”‚
â”‚                                 â”‚
â”‚  Total Weight: 35.5 kg         â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  ðŸ“· Add Manifest Photos         â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”      â”‚
â”‚  â”‚  +  â”‚ â”‚ ðŸ“·  â”‚ â”‚     â”‚      â”‚
â”‚  â””â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”˜      â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Sender Signature               â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â”‚    Sign here            â”‚   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚  [Clear] [Use saved signature] â”‚
â”‚                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚   Submit for Approval     â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.10 Receive Manifest (Inward)

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Receive Manifest     Save   â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  MN-2024-00125                 â”‚
â”‚  Status: In Transit            â”‚
â”‚                                 â”‚
â”‚  From: Abu Dhabi Base          â”‚
â”‚  Shipped: Jan 20, 2024         â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Items to Receive (3)          â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ â˜ ROV Camera System    â”‚   â”‚
â”‚  â”‚ EQ-2024-00001           â”‚   â”‚
â”‚  â”‚ Expected: 1 â€¢ Good      â”‚   â”‚
â”‚  â”‚ [Scan to Receive]       â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ âœ“ Depth Sensor          â”‚   â”‚
â”‚  â”‚ EQ-2024-00002           â”‚   â”‚
â”‚  â”‚ Received: 1 â€¢ Good      â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ âš ï¸ Cable Assemblies     â”‚   â”‚
â”‚  â”‚ EQ-2024-00010           â”‚   â”‚
â”‚  â”‚ Expected: 5 â€¢ Received: 4â”‚  â”‚
â”‚  â”‚ Discrepancy: 1 Missing  â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚  Progress: 2/3 items received  â”‚
â”‚                                 â”‚
â”‚  ðŸ“· Add Receipt Photos          â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â” â”Œâ”€â”€â”€â”€â”€â”      â”‚
â”‚  â”‚  +  â”‚ â”‚ ðŸ“·  â”‚ â”‚     â”‚      â”‚
â”‚  â””â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”˜ â””â”€â”€â”€â”€â”€â”˜      â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Receiver Signature             â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚                         â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚    Complete Receipt       â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.11 Conflict Resolution Screen

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Sync Conflict               â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  âš ï¸ Conflict Detected          â”‚
â”‚                                 â”‚
â”‚  Equipment: ROV Camera System  â”‚
â”‚  EQ-2024-00001                 â”‚
â”‚                                 â”‚
â”‚  The same item was modified    â”‚
â”‚  on another device.            â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  Your Version (Local)          â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Status: In Use          â”‚   â”‚
â”‚  â”‚ Location: Deck Storage  â”‚   â”‚
â”‚  â”‚ Modified: 10:30 AM      â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Server Version                â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ Status: In Transit      â”‚   â”‚
â”‚  â”‚ Location: (In Manifest) â”‚   â”‚
â”‚  â”‚ Modified: 10:25 AM      â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚     Use My Version        â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚    Use Server Version     â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚    View Full Details      â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 5.12 Settings Screen

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚ â†  Settings                    â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                 â”‚
â”‚  Account                        â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚ ðŸ‘¤ John Smith           â”‚   â”‚
â”‚  â”‚ john.smith@company.com  â”‚   â”‚
â”‚  â”‚ MV Explorer             â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Security                       â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚  Change PIN               â†’    â”‚
â”‚  Change Password          â†’    â”‚
â”‚  Enable Biometric         â—‹    â”‚
â”‚                                 â”‚
â”‚  Sync                          â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚  Last Sync: 2 min ago          â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚      Sync Now           â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚  Auto Sync                â—    â”‚
â”‚  Sync over WiFi only      â—    â”‚
â”‚  Pending Changes: 3            â”‚
â”‚  Conflicts: 0                  â”‚
â”‚                                 â”‚
â”‚  Offline Data                  â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚  Equipment: 523 items          â”‚
â”‚  Storage Used: 45 MB           â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚   Clear Offline Data    â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                 â”‚
â”‚  Server                        â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚  Current: Production           â”‚
â”‚  URL: api.company.com          â”‚
â”‚                                 â”‚
â”‚  About                         â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚  Version: 1.0.0 (Build 125)    â”‚
â”‚  Device ID: abc123...          â”‚
â”‚                                 â”‚
â”‚  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚       Log Out           â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

---

## 6. Offline Functionality Requirements

### 6.1 What Must Work Offline

| Feature | Offline Capability |
|---------|-------------------|
| View Equipment | âœ… Full functionality |
| Search Equipment | âœ… Local search |
| Scan QR Code | âœ… Local lookup |
| Create Equipment | âœ… Queued for sync |
| Update Equipment | âœ… Queued for sync |
| Take Photos | âœ… Stored locally |
| Create Manifest | âœ… Queued for sync |
| Add Items to Manifest | âœ… Local + queue |
| Submit Manifest | âœ… Queued for sync |
| Receive Manifest | âœ… Queued for sync |
| View Manifest | âœ… From local cache |
| Generate PDF | âš ï¸ Basic only (no server template) |
| Digital Signature | âœ… Stored locally |

### 6.2 Sync Behavior

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                        SYNC FLOW                                    â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                     â”‚
â”‚  1. APP STARTUP                                                     â”‚
â”‚     â”œâ”€â”€ Check network connectivity                                  â”‚
â”‚     â”œâ”€â”€ If online: Trigger delta sync                              â”‚
â”‚     â””â”€â”€ If offline: Use cached data                                â”‚
â”‚                                                                     â”‚
â”‚  2. BACKGROUND SYNC (when online)                                  â”‚
â”‚     â”œâ”€â”€ Every 5 minutes: Check for server changes                  â”‚
â”‚     â”œâ”€â”€ Process offline queue                                       â”‚
â”‚     â””â”€â”€ Download new changes                                        â”‚
â”‚                                                                     â”‚
â”‚  3. MANUAL SYNC                                                     â”‚
â”‚     â”œâ”€â”€ User taps "Sync Now"                                       â”‚
â”‚     â”œâ”€â”€ Show progress indicator                                     â”‚
â”‚     â””â”€â”€ Report results                                              â”‚
â”‚                                                                     â”‚
â”‚  4. OFFLINE QUEUE PROCESSING                                        â”‚
â”‚     â”œâ”€â”€ Process in FIFO order                                      â”‚
â”‚     â”œâ”€â”€ Retry failed items (max 3 attempts)                        â”‚
â”‚     â”œâ”€â”€ Exponential backoff: 5s, 15s, 45s                          â”‚
â”‚     â””â”€â”€ Flag permanent failures for user review                    â”‚
â”‚                                                                     â”‚
â”‚  5. CONFLICT DETECTION                                              â”‚
â”‚     â”œâ”€â”€ Compare local version with server version                   â”‚
â”‚     â”œâ”€â”€ If conflict: Store both versions                           â”‚
â”‚     â””â”€â”€ Prompt user for resolution                                 â”‚
â”‚                                                                     â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 6.3 Data Storage Requirements

```
MUST CACHE LOCALLY:
â”œâ”€â”€ All equipment at user's assigned locations
â”œâ”€â”€ All active manifests (user's locations)
â”œâ”€â”€ All locations (lookup)
â”œâ”€â”€ All categories (lookup)
â”œâ”€â”€ All equipment types (lookup)
â”œâ”€â”€ All projects (lookup)
â”œâ”€â”€ User profile and permissions
â””â”€â”€ Pending offline changes

STORAGE ESTIMATES:
â”œâ”€â”€ 1000 equipment items â‰ˆ 5 MB
â”œâ”€â”€ 100 manifests â‰ˆ 2 MB
â”œâ”€â”€ Photos (thumbnails) â‰ˆ 20 MB
â”œâ”€â”€ Lookup data â‰ˆ 1 MB
â””â”€â”€ Total typical: 30-50 MB
```

---

## 7. Push Notifications

### 7.1 Notification Types

| Event | Title | Body Example |
|-------|-------|--------------|
| Manifest Received | New Manifest | MN-2024-00125 arrived at your location |
| Approval Required | Approval Needed | MN-2024-00125 needs your approval |
| Manifest Approved | Manifest Approved | MN-2024-00125 has been approved |
| Manifest Rejected | Manifest Rejected | MN-2024-00125 was rejected |
| Cert Expiring | Certification Alert | ROV Camera cert expires in 30 days |
| Calibration Due | Calibration Due | 5 items need calibration this week |
| Sync Conflict | Sync Conflict | Please resolve 2 sync conflicts |
| Low Stock | Low Stock Alert | Cable assemblies below minimum |

### 7.2 Implementation

```json
// FCM Message Format
{
  "to": "device-fcm-token",
  "notification": {
    "title": "New Manifest",
    "body": "MN-2024-00125 arrived at MV Explorer"
  },
  "data": {
    "type": "manifest_received",
    "manifestId": "uuid",
    "manifestNumber": "MN-2024-00125"
  }
}
```

---

## 8. QR Code Scanning Specifications

### 8.1 Supported Formats

```
PRIMARY: QR Code (Version 1-10, Error Correction L/M)

QR CODE CONTENT FORMATS:
â”œâ”€â”€ Equipment: foseq:{assetNumber}
â”‚   Example: foseq:EQ-2024-00001
â”‚
â”œâ”€â”€ Manifest: fosman:{manifestNumber}
â”‚   Example: fosman:MN-2024-00125
â”‚
â”œâ”€â”€ Location: fosloc:{locationCode}
â”‚   Example: fosloc:VES-EXP-001
â”‚
â””â”€â”€ URL (legacy): https://s7.app/e/{id}
```

### 8.2 Scan Flow

```
1. User taps "Scan" button
2. Camera opens with QR overlay
3. On successful scan:
   a. Parse QR content
   b. Determine type (equipment, manifest, location)
   c. If equipment:
      - Check local DB first
      - If not found and online: API lookup
      - Show equipment details
      - Offer context actions
   d. If manifest:
      - Load manifest details
      - Show receive/view options
   e. If location:
      - Show location inventory
4. On invalid/unknown QR:
   - Show "Unknown QR code" message
   - Option to enter code manually
```

### 8.3 Post-Scan Actions

**After scanning equipment:**
```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  âœ“ Equipment Found             â”‚
â”‚                                 â”‚
â”‚  ROV Camera System              â”‚
â”‚  EQ-2024-00001                 â”‚
â”‚                                 â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚     View Details          â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚  Add to Outward Manifest  â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚     Update Status         â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚     Report Issue          â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚                                 â”‚
â”‚  [Scan Another]                â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

---

## 9. Photo Capture Requirements

### 9.1 Photo Specifications

```
CAMERA SETTINGS:
â”œâ”€â”€ Resolution: 1920x1080 (Full HD) or device default
â”œâ”€â”€ Format: JPEG
â”œâ”€â”€ Quality: 80%
â”œâ”€â”€ Max file size: 2 MB
â””â”€â”€ Auto-orientation: Yes

THUMBNAILS:
â”œâ”€â”€ Size: 200x200
â”œâ”€â”€ Generated locally
â””â”€â”€ Stored for offline display

STORAGE:
â”œâ”€â”€ Original photos: App documents directory
â”œâ”€â”€ Thumbnails: App cache directory
â”œâ”€â”€ Upload queue: Track pending uploads
```

### 9.2 Photo Types

| Context | Photo Types |
|---------|-------------|
| Equipment | Main, Condition, Damage, Label, Certificate |
| Manifest (Send) | General, Packaging, Loading |
| Manifest (Receive) | Receipt, Damage |

---

## 10. Digital Signature Requirements

### 10.1 Signature Capture

```
SIGNATURE PAD:
â”œâ”€â”€ Canvas size: Full width Ã— 200px height
â”œâ”€â”€ Stroke color: Black (#000000)
â”œâ”€â”€ Stroke width: 2px
â”œâ”€â”€ Background: White with border
â””â”€â”€ Clear button to reset

OUTPUT:
â”œâ”€â”€ Format: PNG with transparent background
â”œâ”€â”€ Resolution: 400Ã—150 pixels
â”œâ”€â”€ Encoding: Base64 for API transmission
â”œâ”€â”€ Storage: Save to device for reuse
```

### 10.2 Signature Usage

```
WHEN REQUIRED:
â”œâ”€â”€ Outward Manifest: Sender signs before submission
â”œâ”€â”€ Inward Manifest: Receiver signs on receipt
â”œâ”€â”€ Approval: Approver signs when approving
â””â”€â”€ All signatures timestamped

SAVED SIGNATURE:
â”œâ”€â”€ User can save their signature
â”œâ”€â”€ Option to "Use saved signature"
â”œâ”€â”€ Can be updated in Settings
```

---

## 11. Error Handling

### 11.1 Error Messages

| Scenario | Message | Action |
|----------|---------|--------|
| No internet | "You're offline. Changes will sync when connected." | Continue offline |
| API error | "Server error. Please try again." | Retry button |
| Auth expired | "Session expired. Please login again." | Redirect to login |
| QR not found | "Equipment not found. Try manual search." | Search button |
| Sync conflict | "This item was modified elsewhere." | Resolution screen |
| Photo upload failed | "Photo will upload when connected." | Queue for later |
| Invalid input | "Please check the highlighted fields." | Highlight errors |

### 11.2 Offline Indicators

```
VISUAL INDICATORS:
â”œâ”€â”€ Status bar: "Offline" badge when no connection
â”œâ”€â”€ Pending changes: Badge count on sync icon
â”œâ”€â”€ Queued items: "Waiting to sync" label
â””â”€â”€ Last sync time: Show in settings
```

---

## 12. Testing Requirements

### 12.1 Test Scenarios

```
AUTHENTICATION:
â”œâ”€â”€ Login with valid credentials
â”œâ”€â”€ Login with invalid credentials
â”œâ”€â”€ PIN login (after initial setup)
â”œâ”€â”€ Session timeout handling
â”œâ”€â”€ SSO login flow
â””â”€â”€ Biometric login

QR SCANNING:
â”œâ”€â”€ Scan valid equipment QR
â”œâ”€â”€ Scan valid manifest QR
â”œâ”€â”€ Scan invalid/unknown QR
â”œâ”€â”€ Scan in low light conditions
â”œâ”€â”€ Manual code entry
â””â”€â”€ Continuous scanning mode

OFFLINE:
â”œâ”€â”€ Create manifest while offline
â”œâ”€â”€ Add equipment while offline
â”œâ”€â”€ Sync after coming online
â”œâ”€â”€ Conflict detection and resolution
â”œâ”€â”€ Photo queue processing
â””â”€â”€ Large offline queue handling

SYNC:
â”œâ”€â”€ Delta sync (normal)
â”œâ”€â”€ Full sync (initial/reset)
â”œâ”€â”€ Conflict resolution
â”œâ”€â”€ Background sync
â”œâ”€â”€ Sync interruption recovery
â””â”€â”€ Multiple device sync

MANIFESTS:
â”œâ”€â”€ Create outward manifest
â”œâ”€â”€ Add items via scan
â”œâ”€â”€ Add items via search
â”œâ”€â”€ Submit for approval
â”œâ”€â”€ Receive inward manifest
â”œâ”€â”€ Report discrepancies
â”œâ”€â”€ Add photos and signatures
â””â”€â”€ Generate PDF
```

### 12.2 Performance Targets

| Operation | Target |
|-----------|--------|
| App launch to ready | < 3 seconds |
| QR scan to result | < 1 second |
| Search results | < 500ms |
| Local data load | < 200ms |
| Photo capture | < 1 second |
| Sync (delta, 100 changes) | < 10 seconds |
| Sync (full, 1000 items) | < 60 seconds |

---

## 13. Security Requirements

### 13.1 Data Protection

```
SECURE STORAGE:
â”œâ”€â”€ Access tokens: Secure keychain/keystore
â”œâ”€â”€ Refresh tokens: Secure keychain/keystore
â”œâ”€â”€ PIN: Hashed with salt
â”œâ”€â”€ User credentials: Never stored (except username if "remember me")
â””â”€â”€ Offline data: SQLite with encryption (SQLCipher optional)

NETWORK:
â”œâ”€â”€ HTTPS only (TLS 1.2+)
â”œâ”€â”€ Certificate pinning (production)
â”œâ”€â”€ API authentication via JWT
â””â”€â”€ Timeout: 30 seconds

SESSION:
â”œâ”€â”€ Auto-logout after 5 min inactivity (PIN mode)
â”œâ”€â”€ Auto-logout after 30 min inactivity (full mode)
â”œâ”€â”€ Clear session on logout
â””â”€â”€ Revoke tokens on logout
```

### 13.2 Permissions

```
REQUIRED PERMISSIONS:
â”œâ”€â”€ Camera (QR scanning, photos)
â”œâ”€â”€ Storage (photo storage - Android)
â”œâ”€â”€ Photo Library (photo storage - iOS)
â””â”€â”€ Internet/Network state

OPTIONAL PERMISSIONS:
â”œâ”€â”€ Location (for location-aware features)
â”œâ”€â”€ Notifications (push notifications)
â””â”€â”€ Biometrics (fingerprint/face login)
```

---

## 14. Appendix: Complete Data Models

### 14.1 Equipment Model

```typescript
interface Equipment {
  equipmentId: string;
  assetNumber: string;
  sapNumber?: string;
  techNumber?: string;
  serialNumber?: string;
  qrCode: string;
  barcode?: string;
  
  typeId?: string;
  categoryId?: string;
  
  name: string;
  description?: string;
  manufacturer?: string;
  model?: string;
  partNumber?: string;
  specifications?: Record<string, any>;
  
  weightKg?: number;
  lengthCm?: number;
  widthCm?: number;
  heightCm?: number;
  volumeCm3?: number;
  
  packagingType?: string;
  packagingWeightKg?: number;
  packagingLengthCm?: number;
  packagingWidthCm?: number;
  packagingHeightCm?: number;
  packagingDescription?: string;
  
  currentLocationId?: string;
  currentProjectId?: string;
  currentCustodianId?: string;
  status: EquipmentStatus;
  condition: EquipmentCondition;
  
  ownershipType: OwnershipType;
  supplierId?: string;
  purchaseDate?: string;
  purchasePrice?: number;
  purchaseCurrency?: string;
  purchaseOrderNumber?: string;
  warrantyExpiryDate?: string;
  rentalStartDate?: string;
  rentalEndDate?: string;
  rentalRate?: number;
  rentalRatePeriod?: string;
  
  requiresCertification: boolean;
  certificationNumber?: string;
  certificationBody?: string;
  certificationDate?: string;
  certificationExpiryDate?: string;
  requiresCalibration: boolean;
  lastCalibrationDate?: string;
  nextCalibrationDate?: string;
  calibrationInterval?: number;
  
  lastServiceDate?: string;
  nextServiceDate?: string;
  serviceInterval?: number;
  lastInspectionDate?: string;
  
  isConsumable: boolean;
  quantityOnHand?: number;
  unitOfMeasure?: string;
  minimumStockLevel?: number;
  reorderLevel?: number;
  maximumStockLevel?: number;
  batchNumber?: string;
  lotNumber?: string;
  expiryDate?: string;
  
  isPermanentEquipment: boolean;
  isProjectEquipment: boolean;
  
  primaryPhotoUrl?: string;
  qrCodeImageUrl?: string;
  
  notes?: string;
  internalNotes?: string;
  
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  createdBy?: string;
  updatedBy?: string;
  syncVersion: number;
}

type EquipmentStatus = 
  | 'Available' 
  | 'InUse' 
  | 'InTransit' 
  | 'UnderRepair' 
  | 'InCalibration'
  | 'Reserved' 
  | 'Condemned' 
  | 'Lost' 
  | 'Retired' 
  | 'OnHire';

type EquipmentCondition = 
  | 'New' 
  | 'Good' 
  | 'Fair' 
  | 'Poor' 
  | 'Damaged';

type OwnershipType = 
  | 'Owned' 
  | 'Rented' 
  | 'Client' 
  | 'Loaned';
```

### 14.2 Manifest Model

```typescript
interface Manifest {
  manifestId: string;
  manifestNumber: string;
  qrCode: string;
  type: 'Inward' | 'Outward';
  
  fromLocationId: string;
  fromContactName: string;
  fromContactPhone?: string;
  fromContactEmail?: string;
  
  toLocationId: string;
  toContactName: string;
  toContactPhone?: string;
  toContactEmail?: string;
  
  projectId?: string;
  
  status: ManifestStatus;
  
  createdDate: string;
  submittedDate?: string;
  approvedDate?: string;
  shippedDate?: string;
  expectedArrivalDate?: string;
  receivedDate?: string;
  completedDate?: string;
  
  shippingMethod?: string;
  carrierName?: string;
  trackingNumber?: string;
  vehicleNumber?: string;
  driverName?: string;
  driverPhone?: string;
  
  totalItems: number;
  totalWeight?: number;
  totalVolume?: number;
  
  createdBy: string;
  submittedBy?: string;
  approvedBy?: string;
  rejectedBy?: string;
  shippedBy?: string;
  receivedBy?: string;
  
  senderSignature?: string;
  senderSignedAt?: string;
  receiverSignature?: string;
  receiverSignedAt?: string;
  approverSignature?: string;
  approverSignedAt?: string;
  
  notes?: string;
  internalNotes?: string;
  rejectionReason?: string;
  
  hasDiscrepancies: boolean;
  discrepancyNotes?: string;
  
  items: ManifestItem[];
  photos: ManifestPhoto[];
  
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  syncVersion: number;
}

type ManifestStatus = 
  | 'Draft'
  | 'Submitted'
  | 'PendingApproval'
  | 'Approved'
  | 'Rejected'
  | 'InTransit'
  | 'PartiallyReceived'
  | 'Received'
  | 'Completed'
  | 'Cancelled';

interface ManifestItem {
  itemId: string;
  manifestId: string;
  equipmentId: string;
  
  assetNumber: string;
  name: string;
  description?: string;
  serialNumber?: string;
  quantity: number;
  unitOfMeasure: string;
  
  weightKg?: number;
  
  conditionAtSend?: string;
  conditionNotes?: string;
  
  isReceived: boolean;
  receivedQuantity?: number;
  receivedDate?: string;
  receivedBy?: string;
  conditionAtReceive?: string;
  receiptNotes?: string;
  hasDiscrepancy: boolean;
  discrepancyType?: 'Missing' | 'Damaged' | 'Wrong' | 'Excess';
  discrepancyNotes?: string;
  
  sendPhotoUrl?: string;
  receivePhotoUrl?: string;
  
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
  syncVersion: number;
}
```

---

## 15. Development Checklist

### Phase 1: Foundation (Week 1-2)
- [ ] Project setup (React Native/Flutter)
- [ ] Navigation structure
- [ ] Authentication flow (login, token storage)
- [ ] API client setup with interceptors
- [ ] Local SQLite database setup
- [ ] Basic UI components

### Phase 2: Core Features (Week 3-4)
- [ ] QR code scanner integration
- [ ] Equipment list and details screens
- [ ] Equipment search and filters
- [ ] Equipment registration form
- [ ] Photo capture and storage

### Phase 3: Manifests (Week 5-6)
- [ ] Create outward manifest flow
- [ ] Add items (scan + search)
- [ ] Receive inward manifest flow
- [ ] Manifest photos and signatures
- [ ] Manifest list and filters

### Phase 4: Offline & Sync (Week 7-8)
- [ ] Offline data caching
- [ ] Offline queue management
- [ ] Delta sync implementation
- [ ] Conflict detection and resolution UI
- [ ] Background sync

### Phase 5: Polish (Week 9-10)
- [ ] Push notifications
- [ ] Settings screen
- [ ] Error handling and edge cases
- [ ] Performance optimization
- [ ] Testing and bug fixes

---

**Document Version:** 1.0  
**Last Updated:** January 2025  
**For:** Mobile Development Team  
**Desktop Module Contact:** Fathom OS Equipment Module Team
