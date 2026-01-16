# Fathom OS Equipment Inventory - Mobile App Requirements & Implementation Guide

## Version 3.0 | January 2026 | Complete Feature Specification

---

## 📱 MOBILE APP HANDOFF DOCUMENT

This document provides complete specifications for implementing the Equipment Inventory mobile companion app. The mobile app should mirror the desktop WPF module's functionality while optimizing for field operations, touch interfaces, and offline capability.

**v3.0 Updates:**
- Added **Equipment Failure Notification (EFN)** / Defect Reports functionality
- Added **Shipment Verification Workflow** for inward manifest processing
- Added **Unregistered Item Handling** during verification
- Updated data models and API endpoints

---

## TABLE OF CONTENTS

1. [Executive Summary](#1-executive-summary)
2. [Technology Stack](#2-technology-stack)
3. [Data Models](#3-data-models)
4. [API Endpoints](#4-api-endpoints)
5. [Screen Specifications](#5-screen-specifications)
6. [Offline-First Architecture](#6-offline-first-architecture)
7. [Defect Reports (EFN)](#7-defect-reports-efn) ⭐ NEW
8. [Shipment Verification](#8-shipment-verification) ⭐ NEW
9. [Push Notifications](#9-push-notifications)
10. [Batch Operations](#10-batch-operations)
11. [Testing Requirements](#11-testing-requirements)
12. [Deployment Checklist](#12-deployment-checklist)

---

## 1. EXECUTIVE SUMMARY

### 1.1 Project Overview

| Aspect | Details |
|--------|---------|
| **Project Name** | Fathom OS Equipment Inventory Mobile App |
| **Platform** | React Native (recommended) or Flutter |
| **Target OS** | iOS 14+ / Android 10+ |
| **Desktop Counterpart** | S7Fathom.Modules.EquipmentInventory (WPF) |
| **Primary Users** | Field technicians, warehouse staff, logistics coordinators |
| **Key Requirement** | Offline-first architecture with robust sync |

### 1.2 Core Capabilities

The mobile app must support:

**Equipment Management**
- ✅ QR code scanning for equipment lookup
- ✅ Barcode scanning (Code 128, Code 39)
- ✅ Equipment registration and editing
- ✅ Photo capture and document attachment
- ✅ Equipment history/audit trail

**Manifest Operations**
- ✅ Manifest creation (outward/inward)
- ✅ **Shipment verification workflow** ⭐ NEW
- ✅ Item scanning and verification
- ✅ Unregistered item handling
- ✅ Digital signature capture

**Defect Reporting**
- ✅ **Equipment Failure Notification (EFN)** ⭐ NEW
- ✅ Photo evidence capture
- ✅ Parts tracking
- ✅ Resolution workflow

**Operations**
- ✅ Dashboard with alerts and statistics
- ✅ Offline data entry with background sync
- ✅ Maintenance scheduling and reminders
- ✅ Batch operations (bulk status updates)
- ✅ Push notifications for alerts
- ✅ Location-based features (GPS)

---

## 2. TECHNOLOGY STACK

### 2.1 Recommended Stack (React Native)

```javascript
// package.json
{
  "dependencies": {
    // Core
    "react-native": "^0.73.0",
    "@react-navigation/native": "^6.0",
    "@react-navigation/native-stack": "^6.0",
    "@react-navigation/bottom-tabs": "^6.0",
    
    // State Management
    "zustand": "^4.4.0",
    "@tanstack/react-query": "^5.0",
    
    // Database & Storage
    "@nozbe/watermelondb": "^0.27",
    "@react-native-async-storage/async-storage": "^1.21",
    
    // Camera & Scanning
    "react-native-vision-camera": "^3.0",
    "vision-camera-code-scanner": "^0.2",
    
    // Signatures
    "react-native-signature-canvas": "^4.7",
    
    // UI Components
    "react-native-paper": "^5.11",
    "react-native-vector-icons": "^10.0",
    "react-native-reanimated": "^3.6",
    
    // Networking
    "axios": "^1.6",
    "socket.io-client": "^4.7",
    
    // Other
    "react-native-image-picker": "^7.0",
    "react-native-fs": "^2.20",
    "react-native-push-notification": "^8.1",
    "@react-native-community/geolocation": "^3.0",
    "react-native-uuid": "^2.0"
  }
}
```

### 2.2 Alternative Stack (Flutter)

```yaml
# pubspec.yaml
dependencies:
  flutter:
    sdk: flutter
  
  # State Management
  provider: ^6.1.0
  flutter_riverpod: ^2.4.0
  
  # Database
  sqflite: ^2.3.0
  drift: ^2.14.0
  
  # Camera & Scanning
  mobile_scanner: ^3.5.0
  camera: ^0.10.0
  
  # Signatures
  signature: ^5.4.0
  
  # Networking
  dio: ^5.4.0
  web_socket_channel: ^2.4.0
  
  # UI
  flutter_material_symbols: ^0.0.1
  
  # Other
  image_picker: ^1.0.0
  path_provider: ^2.1.0
  geolocator: ^10.1.0
  flutter_local_notifications: ^16.0.0
  uuid: ^4.2.0
```

---

## 3. DATA MODELS

### 3.1 Equipment Model

```typescript
interface Equipment {
  // Identification
  equipmentId: string;           // UUID
  assetNumber: string;           // Auto-generated: "EQ-2026-00001"
  uniqueId: string;              // UniqueId for QR code (e.g., "S7WSS04068")
  name: string;
  description?: string;
  
  // Classification
  categoryId: string;
  categoryName?: string;
  typeId?: string;
  typeName?: string;
  
  // Details
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  partNumber?: string;
  
  // Status
  status: EquipmentStatus;
  condition?: string;            // "New", "Good", "Fair", "Poor"
  isActive: boolean;
  
  // Location
  currentLocationId?: string;
  currentLocationName?: string;
  lastKnownLatitude?: number;
  lastKnownLongitude?: number;
  
  // Certification
  certificationStatus?: string;
  certificationExpiryDate?: string;
  nextCalibrationDate?: string;
  nextMaintenanceDate?: string;
  
  // Financial
  purchasePrice?: number;
  purchaseDate?: string;
  supplierId?: string;
  warrantyExpiryDate?: string;
  
  // Photos
  photoUrls?: string[];
  thumbnailUrl?: string;
  
  // Metadata
  createdAt: string;
  updatedAt: string;
  syncStatus: SyncStatus;
}

enum EquipmentStatus {
  Available = "Available",
  InUse = "InUse",
  InTransit = "InTransit",
  UnderRepair = "UnderRepair",
  Retired = "Retired",
  Lost = "Lost"
}

enum SyncStatus {
  Synced = "Synced",
  PendingUpload = "PendingUpload",
  PendingUpdate = "PendingUpdate",
  Conflict = "Conflict"
}
```

### 3.2 Manifest Model

```typescript
interface Manifest {
  manifestId: string;
  manifestNumber: string;        // "OUT-2026-00001" or "INW-2026-00001"
  manifestType: ManifestType;
  status: ManifestStatus;
  
  // Locations
  originLocationId: string;
  originLocationName?: string;
  destinationLocationId: string;
  destinationLocationName?: string;
  
  // Dates
  createdAt: string;
  shippedDate?: string;
  expectedArrivalDate?: string;
  receivedDate?: string;
  
  // People
  createdByUserId: string;
  createdByName?: string;
  receivedByUserId?: string;
  receivedByName?: string;
  
  // Items
  totalItems: number;
  verifiedItems: number;
  
  // Shipping
  carrier?: string;
  trackingNumber?: string;
  
  // QR/Signatures
  qrCode?: string;
  senderSignature?: string;      // Base64
  receiverSignature?: string;    // Base64
  
  // Reference
  linkedManifestId?: string;     // For inward manifests, links to outward
  notes?: string;
  
  // Sync
  syncStatus: SyncStatus;
}

enum ManifestType {
  Outward = "Outward",
  Inward = "Inward"
}

enum ManifestStatus {
  Draft = "Draft",
  Pending = "Pending",
  InTransit = "InTransit",
  Delivered = "Delivered",
  PartiallyReceived = "PartiallyReceived",
  Completed = "Completed",
  Cancelled = "Cancelled"
}
```

### 3.3 Manifest Item Model

```typescript
interface ManifestItem {
  itemId: string;
  manifestId: string;
  equipmentId?: string;          // Null for unregistered items
  
  // Equipment details (denormalized for offline)
  assetNumber?: string;
  uniqueId?: string;
  equipmentName?: string;
  serialNumber?: string;
  
  // Verification
  isVerified: boolean;
  verifiedAt?: string;
  verifiedByUserId?: string;
  
  // Status
  itemStatus: ManifestItemStatus;
  condition?: string;
  notes?: string;
  
  // Unregistered item details
  isUnregistered: boolean;
  unregisteredDescription?: string;
  unregisteredCategory?: string;
  unregisteredPhotos?: string[];
}

enum ManifestItemStatus {
  Expected = "Expected",
  Verified = "Verified",
  Missing = "Missing",
  Damaged = "Damaged",
  Extra = "Extra"
}
```

### 3.4 Defect Report (EFN) Model ⭐ NEW

```typescript
interface DefectReport {
  defectReportId: string;
  reportNumber: string;          // "EFN-2026-00001"
  qrCode?: string;
  
  // Report metadata
  createdByUserId: string;
  createdByName?: string;
  reportDate: string;
  
  // Location
  locationId?: string;
  locationName?: string;
  thirdPartyLocationName?: string;
  
  // Project
  clientProject?: string;
  rovSystem?: string;
  workingWaterDepth?: number;
  
  // Equipment info
  equipmentId?: string;
  equipmentOrigin: EquipmentOrigin;
  equipmentCategory?: string;
  majorComponent?: string;
  minorComponent?: string;
  
  // Owner info
  isInternalOwner: boolean;
  equipmentOwner?: string;
  isStandardSupply: boolean;
  sapId?: string;
  vendorAssetId?: string;
  equipmentSerialNumber?: string;
  equipmentManufacturer?: string;
  equipmentModel?: string;
  
  // Fault details
  faultCategory: FaultCategory;
  faultSymptoms?: string;
  photosTaken: boolean;
  actionTaken?: string;
  
  // Parts
  partsAvailableOnBoard: boolean;
  replacementRequired: boolean;
  urgency: ReplacementUrgency;
  
  // Additional
  furtherComments?: string;
  nextPortCallDate?: string;
  nextPortCallLocation?: string;
  repairDurationMinutes?: number;
  downtimeDurationMinutes?: number;
  
  // Status
  status: DefectReportStatus;
  resolvedByUserId?: string;
  resolvedByName?: string;
  resolvedAt?: string;
  resolutionNotes?: string;
  
  // Sync
  createdAt: string;
  updatedAt: string;
  syncStatus: SyncStatus;
}

enum EquipmentOrigin {
  ModularHandlingSystem = "ModularHandlingSystem",
  ROV = "ROV",
  Simulator = "Simulator",
  Tooling = "Tooling",
  VesselRig = "VesselRig",
  SurveyInspection = "SurveyInspection"
}

enum FaultCategory {
  Electrical = "Electrical",
  Mechanical = "Mechanical",
  Hydraulic = "Hydraulic",
  Software = "Software",
  Structural = "Structural",
  WearAndTear = "WearAndTear",
  OperatorError = "OperatorError",
  Unknown = "Unknown"
}

enum ReplacementUrgency {
  High = "High",        // Critical, within 24 hours
  Medium = "Medium",    // Alternative available, next port call
  Low = "Low"           // Spares on board
}

enum DefectReportStatus {
  Draft = "Draft",
  Submitted = "Submitted",
  UnderReview = "UnderReview",
  InProgress = "InProgress",
  Resolved = "Resolved",
  Closed = "Closed"
}
```

### 3.5 Defect Report Parts Model ⭐ NEW

```typescript
interface DefectReportPart {
  partId: string;
  defectReportId: string;
  
  // Part details
  sapNumber?: string;
  description: string;
  modelNumber?: string;
  serialNumber?: string;
  
  // Quantities
  failedQuantity: number;
  requiredQuantity: number;
  
  // Sync
  createdAt: string;
  syncStatus: SyncStatus;
}
```

### 3.6 Unregistered Item Model ⭐ NEW

```typescript
interface UnregisteredItem {
  unregisteredItemId: string;
  manifestId: string;
  manifestItemId?: string;
  
  // Description
  description: string;
  category?: string;
  manufacturer?: string;
  model?: string;
  serialNumber?: string;
  
  // Photos
  photoUrls?: string[];
  
  // Disposition
  disposition: UnregisteredItemDisposition;
  notes?: string;
  
  // Created equipment (if registered)
  createdEquipmentId?: string;
  
  // Metadata
  createdAt: string;
  createdByUserId: string;
  syncStatus: SyncStatus;
}

enum UnregisteredItemDisposition {
  Pending = "Pending",
  RegisterAsNew = "RegisterAsNew",
  AttachToExisting = "AttachToExisting",
  ReturnToSender = "ReturnToSender",
  Discard = "Discard"
}
```

---

## 4. API ENDPOINTS

### 4.1 Authentication

```http
# Login
POST /api/auth/login
Content-Type: application/json

{
  "username": "john.smith",
  "password": "password123"
}

Response 200:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "dGhpcy...",
  "expiresIn": 900,
  "user": {
    "userId": "uuid",
    "username": "john.smith",
    "fullName": "John Smith",
    "email": "john.smith@company.com",
    "role": "Operator",
    "assignedLocations": ["uuid1", "uuid2"]
  }
}

# PIN Login (quick re-auth)
POST /api/auth/pin-login
{
  "username": "john.smith",
  "pin": "1234"
}

# Refresh Token
POST /api/auth/refresh
{
  "refreshToken": "dGhpcy..."
}
```

### 4.2 Equipment Endpoints

```http
# Get all equipment (paginated, with sync support)
GET /api/equipment?page=1&pageSize=100&updatedSince=2026-01-01T00:00:00Z

# Get single equipment by ID
GET /api/equipment/{equipmentId}

# Get equipment by UniqueId (QR scan)
GET /api/equipment/by-unique-id/{uniqueId}

# Get equipment by asset number
GET /api/equipment/by-asset/{assetNumber}

# Search equipment
GET /api/equipment/search?q=pump&location=uuid&status=Available

# Create equipment
POST /api/equipment
{
  "name": "Hydraulic Pump",
  "categoryId": "uuid",
  "serialNumber": "HP-12345",
  "currentLocationId": "uuid"
}

# Update equipment
PUT /api/equipment/{equipmentId}

# Update equipment status
PATCH /api/equipment/{equipmentId}/status
{
  "status": "InTransit",
  "notes": "Shipped to Aberdeen"
}

# Update equipment location
PATCH /api/equipment/{equipmentId}/location
{
  "locationId": "uuid",
  "latitude": 57.1497,
  "longitude": -2.0943
}

# Upload equipment photo
POST /api/equipment/{equipmentId}/photos
Content-Type: multipart/form-data
file: <image>
```

### 4.3 Manifest Endpoints

```http
# Get manifests (paginated)
GET /api/manifests?type=Outward&status=InTransit&page=1

# Get pending manifests for location
GET /api/manifests/pending/{locationId}

# Get manifest by ID
GET /api/manifests/{manifestId}

# Get manifest by QR code
GET /api/manifests/by-qr/{qrCode}

# Get manifest items
GET /api/manifests/{manifestId}/items

# Create outward manifest
POST /api/manifests
{
  "manifestType": "Outward",
  "originLocationId": "uuid",
  "destinationLocationId": "uuid",
  "carrier": "DHL",
  "expectedArrivalDate": "2026-01-20"
}

# Add item to manifest
POST /api/manifests/{manifestId}/items
{
  "equipmentId": "uuid"
}

# Add multiple items
POST /api/manifests/{manifestId}/items/batch
{
  "equipmentIds": ["uuid1", "uuid2", "uuid3"]
}

# Ship manifest
POST /api/manifests/{manifestId}/ship
{
  "senderSignature": "base64...",
  "trackingNumber": "DHL123456"
}

# Begin receiving (creates inward manifest linked to outward)
POST /api/manifests/{manifestId}/begin-receiving
{
  "receivingLocationId": "uuid"
}

# Verify item
POST /api/manifests/{manifestId}/items/{itemId}/verify
{
  "status": "Verified",
  "condition": "Good",
  "notes": "Minor scratches"
}

# Complete receiving
POST /api/manifests/{manifestId}/complete-receiving
{
  "receiverSignature": "base64...",
  "notes": "All items received"
}
```

### 4.4 Defect Report Endpoints ⭐ NEW

```http
# Get defect reports (paginated)
GET /api/defect-reports?status=Submitted&page=1

# Get defect reports for equipment
GET /api/defect-reports/by-equipment/{equipmentId}

# Get defect report by ID
GET /api/defect-reports/{defectReportId}

# Get defect report by report number
GET /api/defect-reports/by-number/{reportNumber}

# Create defect report
POST /api/defect-reports
{
  "equipmentId": "uuid",
  "locationId": "uuid",
  "equipmentOrigin": "ROV",
  "faultCategory": "Hydraulic",
  "faultSymptoms": "Pump not generating pressure",
  "actionTaken": "Inspected seals, found damage",
  "urgency": "High",
  "replacementRequired": true
}

# Update defect report
PUT /api/defect-reports/{defectReportId}

# Add photo to defect report
POST /api/defect-reports/{defectReportId}/photos
Content-Type: multipart/form-data
file: <image>

# Add part to defect report
POST /api/defect-reports/{defectReportId}/parts
{
  "description": "Hydraulic seal kit",
  "sapNumber": "SAP-12345",
  "failedQuantity": 2,
  "requiredQuantity": 2
}

# Submit defect report
POST /api/defect-reports/{defectReportId}/submit

# Resolve defect report
POST /api/defect-reports/{defectReportId}/resolve
{
  "resolutionNotes": "Replaced seals, pump operational"
}

# Generate EFN PDF
GET /api/defect-reports/{defectReportId}/pdf
Response: application/pdf
```

### 4.5 Shipment Verification Endpoints ⭐ NEW

```http
# Get verification status for manifest
GET /api/verification/{manifestId}/status

Response:
{
  "manifestId": "uuid",
  "manifestNumber": "OUT-2026-00001",
  "totalItems": 10,
  "verifiedItems": 7,
  "missingItems": 1,
  "extraItems": 2,
  "status": "InProgress"
}

# Add unregistered/extra item
POST /api/verification/{manifestId}/extra-items
{
  "description": "Unknown component",
  "category": "Tooling",
  "manufacturer": "Unknown",
  "serialNumber": "SN-XXXXX",
  "photoUrls": ["base64..."]
}

# Get unregistered items for manifest
GET /api/verification/{manifestId}/unregistered-items

# Update unregistered item disposition
PATCH /api/verification/unregistered/{unregisteredItemId}
{
  "disposition": "RegisterAsNew",
  "notes": "Create as new equipment"
}

# Register unregistered item as equipment
POST /api/verification/unregistered/{unregisteredItemId}/register
{
  "name": "Hydraulic Fitting",
  "categoryId": "uuid",
  "currentLocationId": "uuid"
}
```

### 4.6 Sync Endpoints

```http
# Get changes since timestamp
GET /api/sync/changes?since=2026-01-01T00:00:00Z

Response:
{
  "serverTime": "2026-01-15T10:30:00Z",
  "equipment": {
    "created": [...],
    "updated": [...],
    "deleted": ["uuid1", "uuid2"]
  },
  "manifests": {
    "created": [...],
    "updated": [...],
    "deleted": []
  },
  "defectReports": {
    "created": [...],
    "updated": [...],
    "deleted": []
  },
  "locations": {
    "updated": [...]
  }
}

# Upload offline changes
POST /api/sync/upload
{
  "equipment": [...],
  "manifests": [...],
  "manifestItems": [...],
  "defectReports": [...],
  "defectReportParts": [...],
  "photos": [...],
  "clientTimestamp": "2026-01-15T10:25:00Z"
}

Response:
{
  "success": true,
  "conflicts": [
    {
      "entityType": "Equipment",
      "entityId": "uuid",
      "serverVersion": {...},
      "clientVersion": {...}
    }
  ]
}
```

---

## 5. SCREEN SPECIFICATIONS

### 5.1 Authentication Screens

#### Login Screen
```
┌─────────────────────────────────────┐
│         [Company Logo]               │
│                                      │
│     Fathom OS Equipment Manager      │
│                                      │
│  ┌─────────────────────────────────┐│
│  │ 👤 Username                      ││
│  └─────────────────────────────────┘│
│                                      │
│  ┌─────────────────────────────────┐│
│  │ 🔒 Password                      ││
│  └─────────────────────────────────┘│
│                                      │
│  ☑️ Remember me                      │
│                                      │
│  ┌─────────────────────────────────┐│
│  │         🔓 LOGIN                 ││
│  └─────────────────────────────────┘│
│                                      │
│  [Offline Mode] - If cached creds    │
│                                      │
│  v3.0.0 | © 2026 Company            │
└─────────────────────────────────────┘
```

#### PIN Entry Screen
```
┌─────────────────────────────────────┐
│        Welcome back, John            │
│                                      │
│         ┌───┐ ┌───┐ ┌───┐ ┌───┐     │
│         │ ● │ │ ● │ │ ○ │ │ ○ │     │
│         └───┘ └───┘ └───┘ └───┘     │
│                                      │
│    ┌───┐   ┌───┐   ┌───┐            │
│    │ 1 │   │ 2 │   │ 3 │            │
│    └───┘   └───┘   └───┘            │
│    ┌───┐   ┌───┐   ┌───┐            │
│    │ 4 │   │ 5 │   │ 6 │            │
│    └───┘   └───┘   └───┘            │
│    ┌───┐   ┌───┐   ┌───┐            │
│    │ 7 │   │ 8 │   │ 9 │            │
│    └───┘   └───┘   └───┘            │
│            ┌───┐   ┌───┐            │
│            │ 0 │   │ ⌫ │            │
│            └───┘   └───┘            │
│                                      │
│  [Use Password Instead]              │
└─────────────────────────────────────┘
```

### 5.2 Home Dashboard

```
┌─────────────────────────────────────┐
│ ☰  Dashboard            🔔 ⚙️ 👤    │
├─────────────────────────────────────┤
│  Good morning, John!                 │
│  📍 Aberdeen Base                    │
│                                      │
│  ┌─────────────────────────────────┐│
│  │  [📷 SCAN QR]                    ││
│  │  Tap to scan equipment or       ││
│  │  shipment QR code               ││
│  └─────────────────────────────────┘│
│                                      │
│  QUICK ACTIONS                       │
│  ┌─────────┐ ┌─────────┐ ┌────────┐ │
│  │📦 New   │ │📥 Receive│ │⚠️ Report││
│  │Manifest │ │Shipment │ │Defect  ││
│  └─────────┘ └─────────┘ └────────┘ │
│                                      │
│  ALERTS                    View All >│
│  ┌─────────────────────────────────┐│
│  │ ⚠️ 3 items awaiting verification ││
│  │ 🔴 2 certifications expiring     ││
│  │ 📋 1 defect report pending       ││
│  └─────────────────────────────────┘│
│                                      │
│  RECENT ACTIVITY                     │
│  • Verified shipment OUT-2026-00042  │
│  • Updated pump location             │
│  • Created EFN-2026-00015            │
│                                      │
├─────────────────────────────────────┤
│  🏠    📦    📷    ⚠️    ⚙️         │
│ Home  Items  Scan  Defects Settings │
└─────────────────────────────────────┘
```

### 5.3 QR Scanner Screen

```
┌─────────────────────────────────────┐
│ ←  Scan QR Code              🔦     │
├─────────────────────────────────────┤
│                                      │
│  ┌─────────────────────────────────┐│
│  │                                  ││
│  │      ┌─────────────────┐        ││
│  │      │                 │        ││
│  │      │    [Camera      │        ││
│  │      │     Preview]    │        ││
│  │      │                 │        ││
│  │      │    ┌───────┐    │        ││
│  │      │    │ ▢ ▢ ▢ │    │        ││
│  │      │    │ ▢   ▢ │    │        ││
│  │      │    │ ▢ ▢ ▢ │    │        ││
│  │      │    └───────┘    │        ││
│  │      │                 │        ││
│  │      └─────────────────┘        ││
│  │                                  ││
│  └─────────────────────────────────┘│
│                                      │
│  Scanning for equipment or          │
│  shipment QR codes...               │
│                                      │
│  ┌─────────────────────────────────┐│
│  │  📝 Enter ID manually           ││
│  └─────────────────────────────────┘│
│                                      │
│  Recent Scans:                       │
│  • S7WSS04068 - Pump Unit            │
│  • OUT-2026-00042 - Shipment         │
└─────────────────────────────────────┘
```

### 5.4 Equipment Details Screen

```
┌─────────────────────────────────────┐
│ ←  Equipment Details         ⋮      │
├─────────────────────────────────────┤
│  ┌─────────────────────────────────┐│
│  │         [Equipment Photo]        ││
│  │                                  ││
│  └─────────────────────────────────┘│
│                                      │
│  Hydraulic Pump Unit                 │
│  EQ-2026-00042 | S7WSS04068          │
│                                      │
│  ┌──────────┐                        │
│  │🟢 Available│                      │
│  └──────────┘                        │
│                                      │
│  DETAILS                             │
│  ├─ Serial: HP-2024-12345            │
│  ├─ Manufacturer: Parker             │
│  ├─ Model: PVP-48                    │
│  └─ Category: Hydraulics > Pumps     │
│                                      │
│  LOCATION                            │
│  📍 Aberdeen Base - Warehouse A      │
│                                      │
│  CERTIFICATIONS                      │
│  ⚠️ Calibration due in 5 days        │
│  ✅ Inspection valid until Mar 2026  │
│                                      │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐│
│  │📋 History│ │✏️ Edit  │ │⚠️ Report││
│  └─────────┘ └─────────┘ └─────────┘│
└─────────────────────────────────────┘
```

### 5.5 Create Defect Report (EFN) Screen ⭐ NEW

```
┌─────────────────────────────────────┐
│ ←  Equipment Failure Notification   │
├─────────────────────────────────────┤
│  Step 1 of 4: Failure Details        │
│  ━━━━━━━━━━━━━○───────────────       │
│                                      │
│  EQUIPMENT                           │
│  ┌─────────────────────────────────┐│
│  │ EQ-2026-00042 - Hydraulic Pump  ││
│  │ S/N: HP-2024-12345              ││
│  └─────────────────────────────────┘│
│                                      │
│  LOCATION *                          │
│  ┌─────────────────────────────────┐│
│  │ 📍 Aberdeen Base              ▼ ││
│  └─────────────────────────────────┘│
│                                      │
│  CLIENT/PROJECT                      │
│  ┌─────────────────────────────────┐│
│  │ Equinor Johan Sverdrup          ││
│  └─────────────────────────────────┘│
│                                      │
│  EQUIPMENT ORIGIN *                  │
│  ○ Modular Handling System           │
│  ● ROV                               │
│  ○ Simulator                         │
│  ○ Tooling                           │
│  ○ Vessel / Rig                      │
│  ○ Survey & Inspection               │
│                                      │
│  ┌─────────────────────────────────┐│
│  │            NEXT →                ││
│  └─────────────────────────────────┘│
└─────────────────────────────────────┘
```

```
┌─────────────────────────────────────┐
│ ←  Equipment Failure Notification   │
├─────────────────────────────────────┤
│  Step 2 of 4: Fault Details          │
│  ━━━━━━━━━━━━━━━━━━━━○───────        │
│                                      │
│  FAULT CATEGORY *                    │
│  ┌─────────────────────────────────┐│
│  │ Hydraulic                     ▼ ││
│  └─────────────────────────────────┘│
│                                      │
│  DETAILED SYMPTOMS *                 │
│  ┌─────────────────────────────────┐│
│  │ Pump not generating sufficient  ││
│  │ pressure. Observed fluid leak   ││
│  │ around main seal area.          ││
│  │                                  ││
│  └─────────────────────────────────┘│
│                                      │
│  PHOTOS TAKEN                        │
│  ┌────┐ ┌────┐ ┌────┐               │
│  │📷  │ │📷  │ │ +  │               │
│  │img1│ │img2│ │Add │               │
│  └────┘ └────┘ └────┘               │
│                                      │
│  ACTION TAKEN                        │
│  ┌─────────────────────────────────┐│
│  │ Inspected seals, isolated unit  ││
│  │ from system.                    ││
│  └─────────────────────────────────┘│
│                                      │
│  ┌─────────┐      ┌─────────────────┐│
│  │ ← BACK  │      │     NEXT →      ││
│  └─────────┘      └─────────────────┘│
└─────────────────────────────────────┘
```

```
┌─────────────────────────────────────┐
│ ←  Equipment Failure Notification   │
├─────────────────────────────────────┤
│  Step 3 of 4: Parts & Urgency        │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━○──    │
│                                      │
│  PARTS AVAILABLE ON BOARD? *         │
│  ○ Yes    ● No                       │
│                                      │
│  REPLACEMENT REQUIRED? *             │
│  ● Yes    ○ No                       │
│                                      │
│  URGENCY *                           │
│  🔴 HIGH - Critical, within 24 hrs   │
│  ○ 🟡 MEDIUM - Next port call         │
│  ○ 🟢 LOW - Spares on board           │
│                                      │
│  PARTS FAILED / REQUIRED             │
│  ┌─────────────────────────────────┐│
│  │ 1. Seal kit (SAP-12345)         ││
│  │    Failed: 2  Required: 2       ││
│  ├─────────────────────────────────┤│
│  │ 2. O-ring set (SAP-67890)       ││
│  │    Failed: 1  Required: 2       ││
│  └─────────────────────────────────┘│
│  [+ Add Part]                        │
│                                      │
│  NEXT PORT CALL                      │
│  📅 2026-01-20  📍 Aberdeen          │
│                                      │
│  ┌─────────┐      ┌─────────────────┐│
│  │ ← BACK  │      │     NEXT →      ││
│  └─────────┘      └─────────────────┘│
└─────────────────────────────────────┘
```

```
┌─────────────────────────────────────┐
│ ←  Equipment Failure Notification   │
├─────────────────────────────────────┤
│  Step 4 of 4: Review & Submit        │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│                                      │
│  REPORT SUMMARY                      │
│  ┌─────────────────────────────────┐│
│  │ Report #: EFN-2026-00015        ││
│  │ Date: 2026-01-15                ││
│  │ Location: Aberdeen Base          ││
│  │                                  ││
│  │ Equipment: Hydraulic Pump       ││
│  │ EQ-2026-00042                   ││
│  │                                  ││
│  │ Fault: Hydraulic - Seal failure ││
│  │ Urgency: 🔴 HIGH                 ││
│  │                                  ││
│  │ Parts Required: 2 items          ││
│  │ Photos: 2 attached               ││
│  └─────────────────────────────────┘│
│                                      │
│  FURTHER COMMENTS                    │
│  ┌─────────────────────────────────┐│
│  │ Recommend expedited shipping of ││
│  │ seal kit from Houston.          ││
│  └─────────────────────────────────┘│
│                                      │
│  ┌─────────────────────────────────┐│
│  │      📨 SUBMIT REPORT            ││
│  └─────────────────────────────────┘│
│                                      │
│  [Save as Draft]                     │
└─────────────────────────────────────┘
```

### 5.6 Shipment Verification Screen ⭐ NEW

```
┌─────────────────────────────────────┐
│ ←  Receive Shipment                 │
├─────────────────────────────────────┤
│  SCAN SHIPMENT QR                    │
│  ┌─────────────────────────────────┐│
│  │        [Camera Preview]         ││
│  │         ┌───────┐               ││
│  │         │  📷   │               ││
│  │         └───────┘               ││
│  └─────────────────────────────────┘│
│                                      │
│  Or select from pending:            │
│  ┌─────────────────────────────────┐│
│  │ 📦 OUT-2026-00042               ││
│  │    From: Houston Base            ││
│  │    Items: 15 | Shipped: Jan 10   ││
│  ├─────────────────────────────────┤│
│  │ 📦 OUT-2026-00039               ││
│  │    From: Singapore               ││
│  │    Items: 8 | Shipped: Jan 8     ││
│  └─────────────────────────────────┘│
└─────────────────────────────────────┘

After scanning:

┌─────────────────────────────────────┐
│ ←  Verify Items         OUT-2026-42│
├─────────────────────────────────────┤
│  Progress: 7/10 verified             │
│  ━━━━━━━━━━━━━━━━━━━━━○───────       │
│                                      │
│  ┌─────────────────────────────────┐│
│  │ 🔍 Scan or enter item ID...     ││
│  └─────────────────────────────────┘│
│                                      │
│  EXPECTED ITEMS                      │
│  ┌─────────────────────────────────┐│
│  │ ✅ S7WSS04068 - Pump Unit       ││
│  │ ✅ S7WSS04069 - Valve Assembly  ││
│  │ ✅ S7WSS04070 - Control Unit    ││
│  │ ⬜ S7WSS04071 - Filter Set      ││
│  │ ⬜ S7WSS04072 - Hose Kit        ││
│  │ ⬜ S7WSS04073 - Connector Pack  ││
│  └─────────────────────────────────┘│
│                                      │
│  EXTRA ITEMS (not on manifest)       │
│  ┌─────────────────────────────────┐│
│  │ ➕ Unknown part (scan to add)   ││
│  └─────────────────────────────────┘│
│                                      │
│  ┌────────────────┐ ┌──────────────┐│
│  │⚠️ Mark Missing │ │✅ Complete   ││
│  └────────────────┘ └──────────────┘│
└─────────────────────────────────────┘
```

```
┌─────────────────────────────────────┐
│ ←  Add Unregistered Item            │
├─────────────────────────────────────┤
│  This item was not on the manifest.  │
│  Please provide details.             │
│                                      │
│  SCANNED CODE                        │
│  ┌─────────────────────────────────┐│
│  │ UNKNOWN-XYZ-12345               ││
│  └─────────────────────────────────┘│
│                                      │
│  DESCRIPTION *                       │
│  ┌─────────────────────────────────┐│
│  │ Hydraulic fitting assembly      ││
│  └─────────────────────────────────┘│
│                                      │
│  CATEGORY                            │
│  ┌─────────────────────────────────┐│
│  │ Hydraulics                    ▼ ││
│  └─────────────────────────────────┘│
│                                      │
│  PHOTO                               │
│  ┌────────────────────┐              │
│  │       📷           │ [Take Photo] │
│  │   [Preview]        │              │
│  └────────────────────┘              │
│                                      │
│  WHAT TO DO WITH THIS ITEM?          │
│  ○ Register as new equipment         │
│  ● Keep with shipment (pending)      │
│  ○ Return to sender                  │
│                                      │
│  ┌─────────────────────────────────┐│
│  │           ADD ITEM               ││
│  └─────────────────────────────────┘│
└─────────────────────────────────────┘
```

### 5.7 Verification Complete Screen

```
┌─────────────────────────────────────┐
│         ✅ Verification Complete     │
├─────────────────────────────────────┤
│                                      │
│  Shipment OUT-2026-00042             │
│  received at Aberdeen Base           │
│                                      │
│  ┌─────────────────────────────────┐│
│  │   SUMMARY                        ││
│  │   ─────────────────────────      ││
│  │   ✅ Verified:     10            ││
│  │   ⚠️ Missing:      0             ││
│  │   ➕ Extra items:  1             ││
│  │   🔧 Damaged:      0             ││
│  └─────────────────────────────────┘│
│                                      │
│  INWARD MANIFEST CREATED             │
│  INW-2026-00028                      │
│                                      │
│  SIGNATURE                           │
│  ┌─────────────────────────────────┐│
│  │                                  ││
│  │    [Signature Canvas]            ││
│  │                                  ││
│  └─────────────────────────────────┘│
│  Received by: John Smith             │
│                                      │
│  ┌─────────────────────────────────┐│
│  │    ✅ CONFIRM & CLOSE            ││
│  └─────────────────────────────────┘│
│                                      │
│  [View Details] [Print/Export]       │
└─────────────────────────────────────┘
```

### 5.8 Defect Reports List Screen ⭐ NEW

```
┌─────────────────────────────────────┐
│ ☰  Defect Reports            + New  │
├─────────────────────────────────────┤
│  ┌─────────────────────────────────┐│
│  │ 🔍 Search reports...            ││
│  └─────────────────────────────────┘│
│                                      │
│  Filter: [All ▼] [All Status ▼]      │
│                                      │
│  ┌─────────────────────────────────┐│
│  │ 🔴 EFN-2026-00015               ││
│  │    Hydraulic Pump - Seal Failure ││
│  │    Aberdeen | Today | HIGH       ││
│  │    Status: Submitted             ││
│  ├─────────────────────────────────┤│
│  │ 🟡 EFN-2026-00014               ││
│  │    ROV Camera - Lens damage      ││
│  │    Houston | Yesterday | MEDIUM  ││
│  │    Status: In Progress           ││
│  ├─────────────────────────────────┤│
│  │ 🟢 EFN-2026-00013               ││
│  │    Control Unit - Software bug   ││
│  │    Singapore | Jan 12 | LOW      ││
│  │    Status: Resolved              ││
│  └─────────────────────────────────┘│
│                                      │
│  Showing 3 of 15 reports             │
│                                      │
├─────────────────────────────────────┤
│  🏠    📦    📷    ⚠️    ⚙️         │
│ Home  Items  Scan  Defects Settings │
└─────────────────────────────────────┘
```

---

## 6. OFFLINE-FIRST ARCHITECTURE

### 6.1 Local Database Schema

```sql
-- Core tables
CREATE TABLE equipment (
  equipment_id TEXT PRIMARY KEY,
  asset_number TEXT NOT NULL,
  unique_id TEXT NOT NULL,
  name TEXT NOT NULL,
  -- ... all other fields
  sync_status TEXT DEFAULT 'Synced',
  local_updated_at TEXT,
  server_updated_at TEXT
);

CREATE TABLE manifests (
  manifest_id TEXT PRIMARY KEY,
  manifest_number TEXT NOT NULL,
  manifest_type TEXT NOT NULL,
  status TEXT NOT NULL,
  -- ... all other fields
  sync_status TEXT DEFAULT 'Synced',
  local_updated_at TEXT,
  server_updated_at TEXT
);

CREATE TABLE manifest_items (
  item_id TEXT PRIMARY KEY,
  manifest_id TEXT NOT NULL,
  equipment_id TEXT,
  -- ... all other fields
  sync_status TEXT DEFAULT 'Synced',
  FOREIGN KEY (manifest_id) REFERENCES manifests(manifest_id)
);

-- NEW: Defect reports table
CREATE TABLE defect_reports (
  defect_report_id TEXT PRIMARY KEY,
  report_number TEXT NOT NULL,
  equipment_id TEXT,
  location_id TEXT,
  equipment_origin TEXT,
  fault_category TEXT,
  fault_symptoms TEXT,
  urgency TEXT,
  status TEXT DEFAULT 'Draft',
  -- ... all other fields
  sync_status TEXT DEFAULT 'PendingUpload',
  local_updated_at TEXT,
  server_updated_at TEXT
);

-- NEW: Defect report parts
CREATE TABLE defect_report_parts (
  part_id TEXT PRIMARY KEY,
  defect_report_id TEXT NOT NULL,
  description TEXT NOT NULL,
  sap_number TEXT,
  failed_quantity INTEGER DEFAULT 0,
  required_quantity INTEGER DEFAULT 0,
  sync_status TEXT DEFAULT 'PendingUpload',
  FOREIGN KEY (defect_report_id) REFERENCES defect_reports(defect_report_id)
);

-- NEW: Unregistered items
CREATE TABLE unregistered_items (
  unregistered_item_id TEXT PRIMARY KEY,
  manifest_id TEXT NOT NULL,
  description TEXT NOT NULL,
  category TEXT,
  disposition TEXT DEFAULT 'Pending',
  sync_status TEXT DEFAULT 'PendingUpload',
  FOREIGN KEY (manifest_id) REFERENCES manifests(manifest_id)
);

-- Offline queue for pending uploads
CREATE TABLE offline_queue (
  queue_id INTEGER PRIMARY KEY AUTOINCREMENT,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  operation TEXT NOT NULL,
  payload TEXT NOT NULL,
  created_at TEXT NOT NULL,
  retry_count INTEGER DEFAULT 0,
  last_error TEXT
);

-- Photo queue (separate for large files)
CREATE TABLE photo_queue (
  photo_id TEXT PRIMARY KEY,
  entity_type TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  local_path TEXT NOT NULL,
  upload_status TEXT DEFAULT 'Pending',
  created_at TEXT NOT NULL
);
```

### 6.2 Sync Strategy

```typescript
class SyncService {
  private syncInProgress = false;
  private lastSyncTime: Date | null = null;
  
  async performSync(): Promise<SyncResult> {
    if (this.syncInProgress) return { skipped: true };
    
    this.syncInProgress = true;
    
    try {
      // 1. Upload local changes first
      await this.uploadPendingChanges();
      
      // 2. Upload photos
      await this.uploadPendingPhotos();
      
      // 3. Download server changes
      const changes = await this.downloadChanges();
      
      // 4. Apply changes locally
      await this.applyServerChanges(changes);
      
      // 5. Handle conflicts
      const conflicts = await this.detectConflicts();
      if (conflicts.length > 0) {
        await this.notifyConflicts(conflicts);
      }
      
      this.lastSyncTime = new Date();
      
      return { 
        success: true, 
        uploaded: changes.uploaded,
        downloaded: changes.downloaded,
        conflicts: conflicts.length 
      };
    } finally {
      this.syncInProgress = false;
    }
  }
  
  private async uploadPendingChanges(): Promise<void> {
    const queue = await db.offlineQueue.getAll();
    
    for (const item of queue) {
      try {
        await api.sync.upload(item);
        await db.offlineQueue.delete(item.queueId);
      } catch (error) {
        item.retryCount++;
        item.lastError = error.message;
        await db.offlineQueue.update(item);
      }
    }
  }
}
```

### 6.3 Conflict Resolution

```typescript
interface SyncConflict {
  entityType: 'Equipment' | 'Manifest' | 'DefectReport';
  entityId: string;
  localVersion: any;
  serverVersion: any;
  localUpdatedAt: Date;
  serverUpdatedAt: Date;
}

// Resolution options
enum ConflictResolution {
  KeepLocal = 'KeepLocal',
  KeepServer = 'KeepServer',
  Merge = 'Merge'
}

// Automatic resolution rules
const autoResolveRules = {
  // Server always wins for status changes
  'Equipment.status': 'KeepServer',
  
  // Local wins for notes/descriptions (user work)
  'DefectReport.faultSymptoms': 'KeepLocal',
  'DefectReport.actionTaken': 'KeepLocal',
  
  // Server wins for system-generated fields
  'Manifest.manifestNumber': 'KeepServer'
};
```

---

## 7. DEFECT REPORTS (EFN) ⭐ NEW

### 7.1 Overview

The Equipment Failure Notification (EFN) system allows field personnel to report equipment failures immediately, with full offline support.

### 7.2 Workflow

```
┌─────────────────────────────────────────────────────────────┐
│                    EFN WORKFLOW                              │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────┐    ┌───────────┐    ┌────────────┐            │
│  │  Draft   │ -> │ Submitted │ -> │Under Review│            │
│  └──────────┘    └───────────┘    └────────────┘            │
│       │                                  │                   │
│       │                                  ▼                   │
│       │                          ┌────────────┐             │
│  [Save for    [Submit]           │In Progress │             │
│   later]                         └────────────┘             │
│                                          │                   │
│                                          ▼                   │
│                                  ┌────────────┐             │
│                                  │  Resolved  │             │
│                                  └────────────┘             │
│                                          │                   │
│                                          ▼                   │
│                                  ┌────────────┐             │
│                                  │   Closed   │             │
│                                  └────────────┘             │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 7.3 Required Fields

| Field | Required | Offline | Notes |
|-------|----------|---------|-------|
| Equipment | Yes* | Yes | Can scan QR or select from list |
| Location | Yes | Yes | User's current location or select |
| Equipment Origin | Yes | Yes | ROV, Tooling, etc. |
| Fault Category | Yes | Yes | Dropdown selection |
| Fault Symptoms | Yes | Yes | Free text, min 20 chars |
| Photos | Recommended | Yes | Stored locally until sync |
| Urgency | Yes | Yes | High/Medium/Low |
| Replacement Required | Yes | Yes | Yes/No |

*Can report against unknown equipment if not in system

### 7.4 Photo Handling

```typescript
interface DefectPhoto {
  photoId: string;
  defectReportId: string;
  localPath: string;          // Local file path
  serverUrl?: string;         // After upload
  uploadStatus: 'Pending' | 'Uploading' | 'Uploaded' | 'Failed';
  capturedAt: Date;
  fileSize: number;
  mimeType: string;
}

// Photo capture settings
const photoSettings = {
  maxWidth: 1920,
  maxHeight: 1080,
  quality: 0.8,
  format: 'JPEG',
  includeExif: true,          // For GPS data
  maxPhotosPerReport: 10
};
```

### 7.5 Parts Entry

```typescript
interface PartEntry {
  partId: string;
  description: string;        // Required
  sapNumber?: string;         // SAP ID if known
  modelNumber?: string;
  serialNumber?: string;
  failedQuantity: number;
  requiredQuantity: number;
}

// Parts can be added from:
// 1. Manual entry
// 2. Barcode scan (if part has barcode)
// 3. Selection from parts catalog (if available offline)
```

---

## 8. SHIPMENT VERIFICATION ⭐ NEW

### 8.1 Overview

The shipment verification workflow enables receiving personnel to efficiently verify incoming shipments against the original manifest.

### 8.2 Workflow

```
┌─────────────────────────────────────────────────────────────┐
│               SHIPMENT VERIFICATION WORKFLOW                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. SCAN SHIPMENT                                           │
│     └─> Scan shipment QR or select from pending list        │
│                                                              │
│  2. LOAD EXPECTED ITEMS                                      │
│     └─> Display all items from outward manifest             │
│                                                              │
│  3. VERIFY ITEMS (one by one)                               │
│     └─> Scan each item's QR code                            │
│         ├─> ✅ Match found -> Mark as verified              │
│         ├─> ⚠️ Not expected -> Add as extra item            │
│         └─> ❌ Damaged -> Mark condition                    │
│                                                              │
│  4. HANDLE DISCREPANCIES                                     │
│     ├─> Missing items -> Flag for follow-up                 │
│     └─> Extra items -> Register or return                   │
│                                                              │
│  5. COMPLETE VERIFICATION                                    │
│     └─> Capture signature                                   │
│     └─> Create inward manifest                              │
│     └─> Update equipment locations                          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 8.3 Item States

```typescript
enum VerificationItemState {
  Expected = 'Expected',       // On manifest, not yet scanned
  Verified = 'Verified',       // Scanned and matched
  Missing = 'Missing',         // Not received
  Damaged = 'Damaged',         // Received but damaged
  Extra = 'Extra'              // Not on manifest
}

interface VerificationItem {
  itemId: string;
  manifestId: string;
  equipmentId?: string;
  
  // From manifest
  expectedAssetNumber?: string;
  expectedUniqueId?: string;
  expectedName?: string;
  
  // Verification
  state: VerificationItemState;
  verifiedAt?: Date;
  verifiedByUserId?: string;
  scannedCode?: string;
  
  // Condition
  condition?: 'Good' | 'Fair' | 'Damaged';
  conditionNotes?: string;
  conditionPhotos?: string[];
}
```

### 8.4 Unregistered Item Handling

When an item is scanned that doesn't match any expected item:

```typescript
interface UnregisteredItemFlow {
  // 1. Capture basic info
  scannedCode: string;
  description: string;
  category?: string;
  photo?: string;
  
  // 2. User selects disposition
  disposition: 
    | 'RegisterAsNew'      // Create new equipment record
    | 'AttachToExisting'   // Link to existing equipment
    | 'ReturnToSender'     // Flag for return
    | 'Pending';           // Decide later
  
  // 3. If registering as new
  newEquipmentData?: {
    name: string;
    categoryId: string;
    serialNumber?: string;
    // ... other fields
  };
}
```

### 8.5 Offline Verification

Full verification can be done offline:
- Expected items list synced when shipment is selected
- Scanning works offline (QR decode is local)
- All states saved locally
- Photos stored locally
- Signatures captured locally
- Sync happens when connection available

---

## 9. PUSH NOTIFICATIONS

### 9.1 Notification Types

```typescript
enum NotificationType {
  // Shipment notifications
  ShipmentDispatched = 'ShipmentDispatched',
  ShipmentArriving = 'ShipmentArriving',
  ShipmentReceived = 'ShipmentReceived',
  
  // Certification alerts
  CertificationExpiring = 'CertificationExpiring',
  CertificationExpired = 'CertificationExpired',
  CalibrationDue = 'CalibrationDue',
  
  // Defect reports
  DefectReportAssigned = 'DefectReportAssigned',
  DefectReportUpdated = 'DefectReportUpdated',
  DefectReportResolved = 'DefectReportResolved',
  
  // Sync status
  SyncConflict = 'SyncConflict',
  SyncComplete = 'SyncComplete'
}
```

### 9.2 Notification Payload

```typescript
interface PushNotification {
  type: NotificationType;
  title: string;
  body: string;
  data: {
    entityType: string;
    entityId: string;
    action?: string;
    deepLink?: string;
  };
}

// Example: Shipment arriving
{
  type: 'ShipmentArriving',
  title: 'Shipment Arriving',
  body: 'OUT-2026-00042 from Houston expected today',
  data: {
    entityType: 'Manifest',
    entityId: 'uuid',
    action: 'ReceiveShipment',
    deepLink: 'fathom://manifests/receive/uuid'
  }
}
```

---

## 10. BATCH OPERATIONS

### 10.1 Supported Batch Operations

```typescript
// Batch status update
POST /api/equipment/batch/status
{
  "equipmentIds": ["uuid1", "uuid2", "uuid3"],
  "newStatus": "InTransit",
  "notes": "Shipped to Aberdeen"
}

// Batch location update
POST /api/equipment/batch/location
{
  "equipmentIds": ["uuid1", "uuid2", "uuid3"],
  "newLocationId": "uuid",
  "notes": "Received at warehouse"
}

// Batch add to manifest
POST /api/manifests/{manifestId}/items/batch
{
  "equipmentIds": ["uuid1", "uuid2", "uuid3"]
}
```

### 10.2 Batch Selection UI

```
┌─────────────────────────────────────┐
│ ←  Select Items           Done (5) │
├─────────────────────────────────────┤
│  ☑️ Select All (25)                 │
│                                      │
│  ┌─────────────────────────────────┐│
│  │ ☑️ EQ-2026-00042 - Pump Unit    ││
│  │ ☑️ EQ-2026-00043 - Valve Assy   ││
│  │ ☑️ EQ-2026-00044 - Control Box  ││
│  │ ☐ EQ-2026-00045 - Filter Set    ││
│  │ ☑️ EQ-2026-00046 - Hose Kit     ││
│  │ ☑️ EQ-2026-00047 - Connector    ││
│  └─────────────────────────────────┘│
│                                      │
│  BATCH ACTIONS                       │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐│
│  │📍 Move  │ │📦 Ship  │ │🏷️ Label ││
│  └─────────┘ └─────────┘ └─────────┘│
└─────────────────────────────────────┘
```

---

## 11. TESTING REQUIREMENTS

### 11.1 Unit Tests

- [ ] Data model validation
- [ ] Offline storage operations
- [ ] Sync conflict detection
- [ ] QR code parsing
- [ ] Form validation

### 11.2 Integration Tests

- [ ] API authentication flow
- [ ] Full sync cycle
- [ ] Photo upload/download
- [ ] Push notification handling

### 11.3 E2E Tests

- [ ] Login flow (online/offline)
- [ ] Equipment scan and view
- [ ] Create and submit defect report
- [ ] Complete shipment verification
- [ ] Manifest creation workflow

### 11.4 Offline Testing

- [ ] All features work without network
- [ ] Data persists across app restarts
- [ ] Sync completes after reconnection
- [ ] Conflicts are properly flagged

---

## 12. DEPLOYMENT CHECKLIST

### 12.1 Development Timeline

| Week | Focus Area |
|------|------------|
| 1-2 | Project setup, navigation, authentication |
| 3-4 | QR scanner, equipment screens, photo capture |
| 5-6 | Defect report creation (EFN) workflow |
| 7-8 | Shipment verification workflow |
| 9-10 | Manifest creation, signatures |
| 11-12 | Offline database, sync engine |
| 13-14 | Push notifications, conflict handling |
| 15-16 | Testing, polish, app store submission |

### 12.2 Pre-Release Checklist

- [ ] All screens implemented per specs
- [ ] Offline mode fully functional
- [ ] Sync tested with production-like data
- [ ] Push notifications working (iOS/Android)
- [ ] App icons and splash screens
- [ ] Privacy policy and terms
- [ ] App store metadata
- [ ] Beta testing completed

### 12.3 App Store Requirements

**iOS:**
- Minimum iOS 14
- Camera usage description
- Location usage description
- Background sync capability

**Android:**
- Minimum API 29 (Android 10)
- Camera permission
- Location permission
- Storage permission
- Background sync service

---

## VERSION HISTORY

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Jan 2026 | Initial specification |
| 2.0 | Jan 2026 | Added batch operations, enhanced offline |
| 3.0 | Jan 2026 | Added Defect Reports (EFN), Shipment Verification, Unregistered Item handling |

---

**Document Version:** 3.0  
**Last Updated:** January 2026  
**Author:** Development Team
