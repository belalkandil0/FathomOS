# Fathom OS Equipment Inventory - Mobile App Requirements & Implementation Guide

## Version 2.0 | January 2026 | Complete Feature Specification

---

## 📱 MOBILE APP HANDOFF DOCUMENT

This document provides complete specifications for implementing the Equipment Inventory mobile companion app. The mobile app should mirror the desktop WPF module's functionality while optimizing for field operations, touch interfaces, and offline capability.

---

## 1. EXECUTIVE SUMMARY

### 1.1 Project Overview

| Aspect | Details |
|--------|---------|
| **Project Name** | Fathom OS Equipment Inventory Mobile App |
| **Platform** | React Native (recommended) or Flutter |
| **Target OS** | iOS 14+ / Android 10+ |
| **Desktop Counterpart** | FathomOS.Modules.EquipmentInventory (WPF) |
| **Primary Users** | Field technicians, warehouse staff, logistics coordinators |
| **Key Requirement** | Offline-first architecture with robust sync |

### 1.2 Core Capabilities

The mobile app must support:
- ✅ QR code scanning for equipment lookup
- ✅ Barcode scanning (Code 128, Code 39)
- ✅ Equipment registration and editing
- ✅ Manifest creation and receipt confirmation
- ✅ Photo capture and document attachment
- ✅ Offline data entry with background sync
- ✅ Dashboard with alerts and statistics
- ✅ Maintenance scheduling and reminders
- ✅ Equipment history/audit trail
- ✅ Batch operations (bulk status updates)
- ✅ Push notifications for alerts
- ✅ Location-based features (GPS)

---

## 2. TECHNOLOGY STACK

### 2.1 Recommended Stack (React Native)

```javascript
// Core
"react-native": "^0.73.0",
"@react-navigation/native": "^6.0",
"@react-navigation/native-stack": "^6.0",

// State Management
"zustand": "^4.4.0",           // Lightweight state management
"@tanstack/react-query": "^5.0", // Server state & caching

// Database & Storage
"@nozbe/watermelondb": "^0.27", // SQLite wrapper (offline-first)
"@react-native-async-storage/async-storage": "^1.21",

// Camera & Scanning
"react-native-camera-kit": "^13.0",
"react-native-vision-camera": "^3.0",
"vision-camera-code-scanner": "^0.2",

// UI Components
"react-native-paper": "^5.11",
"react-native-vector-icons": "^10.0",
"react-native-reanimated": "^3.6",

// Networking
"axios": "^1.6",
"socket.io-client": "^4.7",     // Real-time updates

// Other
"react-native-image-picker": "^7.0",
"react-native-fs": "^2.20",
"react-native-push-notification": "^8.1",
"@react-native-community/geolocation": "^3.0",
```

### 2.2 Alternative Stack (Flutter)

```yaml
dependencies:
  flutter:
    sdk: flutter
  
  # State Management
  provider: ^6.1.0
  flutter_riverpod: ^2.4.0
  
  # Database
  sqflite: ^2.3.0
  hive: ^2.2.0
  
  # Camera & Scanning
  mobile_scanner: ^3.5.0
  camera: ^0.10.0
  
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
```

---

## 3. DATA MODELS

### 3.1 Equipment Model

```typescript
interface Equipment {
  // Identification
  equipmentId: string;           // UUID
  assetNumber: string;           // Auto-generated: "EQ-240001"
  uniqueId: string;              // UUID for QR code
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
  currentProjectId?: string;
  
  // Certification
  requiresCertification: boolean;
  certificationNumber?: string;
  certificationBody?: string;
  certificationExpiryDate?: string; // ISO date
  
  // Calibration
  requiresCalibration: boolean;
  lastCalibrationDate?: string;
  nextCalibrationDate?: string;
  calibrationIntervalDays?: number;
  
  // Service
  lastServiceDate?: string;
  nextServiceDate?: string;
  serviceIntervalDays?: number;
  
  // Physical Specs
  weightKg?: number;
  lengthCm?: number;
  widthCm?: number;
  heightCm?: number;
  
  // Purchase Info
  purchasePrice?: number;
  purchaseDate?: string;
  supplierId?: string;
  warrantyExpiryDate?: string;
  
  // Consumable
  isConsumable: boolean;
  quantityOnHand?: number;
  minimumQuantity?: number;
  reorderPoint?: number;
  unitOfMeasure?: string;
  
  // Photos
  primaryPhotoUrl?: string;
  photoUrls: string[];
  
  // Sync
  createdAt: string;
  updatedAt: string;
  isModifiedLocally: boolean;
  localVersion: number;
  serverVersion: number;
  
  // Flags
  isFavorite: boolean;
}

enum EquipmentStatus {
  Available = "Available",
  InUse = "InUse",
  InTransit = "InTransit",
  UnderRepair = "UnderRepair",
  Retired = "Retired",
  Lost = "Lost"
}
```

### 3.2 Manifest Model

```typescript
interface Manifest {
  manifestId: string;
  manifestNumber: string;         // Auto: "MAN-2024-0001"
  manifestType: ManifestType;
  status: ManifestStatus;
  
  // Locations
  sourceLocationId: string;
  sourceLocationName?: string;
  destinationLocationId: string;
  destinationLocationName?: string;
  
  // Project
  projectId?: string;
  projectName?: string;
  
  // Details
  description?: string;
  notes?: string;
  
  // Dates
  createdAt: string;
  dispatchedAt?: string;
  receivedAt?: string;
  expectedArrivalDate?: string;
  
  // Personnel
  createdByUserId: string;
  createdByName?: string;
  dispatchedByUserId?: string;
  dispatchedByName?: string;
  receivedByUserId?: string;
  receivedByName?: string;
  
  // Items
  items: ManifestItem[];
  totalItems: number;
  receivedItems: number;
  
  // Sync
  isModifiedLocally: boolean;
}

interface ManifestItem {
  itemId: string;
  manifestId: string;
  equipmentId: string;
  assetNumber: string;
  equipmentName: string;
  
  quantitySent: number;
  quantityReceived?: number;
  
  condition?: string;
  notes?: string;
  
  isReceived: boolean;
  receivedAt?: string;
}

enum ManifestType {
  Outward = "Outward",
  Inward = "Inward",
  Transfer = "Transfer",
  Return = "Return"
}

enum ManifestStatus {
  Draft = "Draft",
  Pending = "Pending",
  Dispatched = "Dispatched",
  InTransit = "InTransit",
  PartiallyReceived = "PartiallyReceived",
  Received = "Received",
  Cancelled = "Cancelled"
}
```

### 3.3 History/Audit Model

```typescript
interface EquipmentHistoryItem {
  historyId: string;
  equipmentId: string;
  assetNumber: string;
  equipmentName: string;
  
  action: HistoryAction;
  description: string;
  
  oldValue?: string;
  newValue?: string;
  
  performedAt: string;
  performedByUserId?: string;
  performedByName?: string;
  
  // Display helpers
  actionDisplay: string;
  actionIcon: string;
  timeAgo: string;
}

enum HistoryAction {
  Created = "Created",
  Updated = "Updated",
  Deleted = "Deleted",
  StatusChanged = "StatusChanged",
  Transferred = "Transferred",
  Certified = "Certified",
  Calibrated = "Calibrated",
  Serviced = "Serviced",
  PhotoAdded = "PhotoAdded",
  PhotoRemoved = "PhotoRemoved",
  DocumentAdded = "DocumentAdded",
  DocumentRemoved = "DocumentRemoved",
  Custom = "Custom"
}
```

### 3.4 Maintenance Model

```typescript
interface MaintenanceItem {
  equipmentId: string;
  assetNumber: string;
  equipmentName: string;
  locationName?: string;
  categoryName?: string;
  
  maintenanceType: MaintenanceType;
  dueDate: string;
  isOverdue: boolean;
  daysUntilDue: number;
  description: string;
  
  // Display
  statusText: string;
  statusColor: string;
}

enum MaintenanceType {
  Certification = "Certification",
  Calibration = "Calibration",
  Service = "Service",
  Inspection = "Inspection"
}

interface MaintenanceSummary {
  totalOverdue: number;
  totalUpcoming: number;
  overdueCertifications: number;
  overdueCalibrations: number;
  overdueServices: number;
  overdueInspections: number;
  upcomingThisWeek: number;
  upcomingThisMonth: number;
}
```

### 3.5 Dashboard Model

```typescript
interface DashboardSummary {
  totalEquipment: number;
  availableEquipment: number;
  inUseEquipment: number;
  inTransitEquipment: number;
  
  certificationsExpiringSoon: number;
  certificationsExpired: number;
  calibrationsDueSoon: number;
  calibrationsOverdue: number;
  
  pendingManifests: number;
  inTransitManifests: number;
  
  totalLocations: number;
  totalValue: number;
}

interface AlertItem {
  equipmentId: string;
  assetNumber: string;
  equipmentName: string;
  alertType: AlertType;
  message: string;
  severity: AlertSeverity;
  locationName?: string;
  severityColor: string;
}

enum AlertType {
  CertificationExpired = "CertificationExpired",
  CertificationExpiring = "CertificationExpiring",
  CalibrationOverdue = "CalibrationOverdue",
  CalibrationDue = "CalibrationDue",
  MaintenanceDue = "MaintenanceDue",
  LowStock = "LowStock"
}

enum AlertSeverity {
  Low = "Low",
  Medium = "Medium",
  High = "High"
}
```

---

## 4. API ENDPOINTS

### 4.1 Authentication

```
POST   /api/auth/login
       Body: { username, password }
       Response: { token, refreshToken, user, expiresAt }

POST   /api/auth/pin-login
       Body: { userId, pin }
       Response: { token, refreshToken, user, expiresAt }

POST   /api/auth/refresh
       Body: { refreshToken }
       Response: { token, expiresAt }

POST   /api/auth/logout
```

### 4.2 Equipment

```
GET    /api/equipment
       Query: ?page=1&pageSize=50&search=&categoryId=&locationId=&status=
       Response: { items: Equipment[], totalCount, page, pageSize }

GET    /api/equipment/{id}
       Response: Equipment

GET    /api/equipment/by-asset/{assetNumber}
       Response: Equipment

GET    /api/equipment/by-qr/{uniqueId}
       Response: Equipment

POST   /api/equipment
       Body: Equipment
       Response: Equipment

PUT    /api/equipment/{id}
       Body: Equipment
       Response: Equipment

DELETE /api/equipment/{id}

POST   /api/equipment/{id}/photo
       Body: FormData (image file)
       Response: { photoUrl }

DELETE /api/equipment/{id}/photo/{photoId}

GET    /api/equipment/{id}/history
       Response: EquipmentHistoryItem[]

POST   /api/equipment/batch/status
       Body: { equipmentIds: string[], status: EquipmentStatus, reason?: string }
       Response: { updated: number, failed: number }

POST   /api/equipment/batch/location
       Body: { equipmentIds: string[], locationId: string }
       Response: { updated: number, failed: number }

POST   /api/equipment/duplicate
       Body: { equipmentId: string, options: DuplicateOptions }
       Response: Equipment
```

### 4.3 Manifests

```
GET    /api/manifests
       Query: ?status=&type=&locationId=
       Response: Manifest[]

GET    /api/manifests/{id}
       Response: Manifest (with items)

POST   /api/manifests
       Body: Manifest
       Response: Manifest

PUT    /api/manifests/{id}
       Body: Manifest
       Response: Manifest

POST   /api/manifests/{id}/dispatch
       Response: Manifest

POST   /api/manifests/{id}/receive
       Body: { items: { itemId, quantityReceived, condition?, notes? }[] }
       Response: Manifest

POST   /api/manifests/{id}/cancel
       Response: Manifest
```

### 4.4 Sync

```
GET    /api/sync/changes
       Query: ?since={timestamp}&tables=equipment,manifests,locations
       Response: { 
         equipment: { added: [], updated: [], deleted: [] },
         manifests: { added: [], updated: [], deleted: [] },
         ...
         serverTime: string
       }

POST   /api/sync/push
       Body: { 
         equipment: { added: [], updated: [], deleted: [] },
         manifests: { added: [], updated: [], deleted: [] },
         clientTime: string
       }
       Response: { 
         conflicts: SyncConflict[],
         processed: number,
         serverTime: string
       }

GET    /api/sync/full
       Query: ?tables=equipment,manifests,locations
       Response: { equipment: [], manifests: [], locations: [], ... }
```

### 4.5 Dashboard & Maintenance

```
GET    /api/dashboard/summary
       Response: DashboardSummary

GET    /api/dashboard/alerts
       Response: AlertItem[]

GET    /api/dashboard/activity
       Query: ?limit=20
       Response: ActivityItem[]

GET    /api/maintenance/upcoming
       Query: ?daysAhead=30
       Response: MaintenanceItem[]

GET    /api/maintenance/overdue
       Response: MaintenanceItem[]

GET    /api/maintenance/summary
       Response: MaintenanceSummary

POST   /api/maintenance/complete/certification
       Body: { equipmentId, certificationNumber, certificationBody, expiryDate }

POST   /api/maintenance/complete/calibration
       Body: { equipmentId, calibrationDate, nextDueDate? }

POST   /api/maintenance/complete/service
       Body: { equipmentId, serviceDate, serviceType, notes? }
```

### 4.6 Reference Data

```
GET    /api/categories
       Response: Category[]

GET    /api/locations
       Response: Location[]

GET    /api/location-types
       Response: LocationType[]

GET    /api/equipment-types
       Response: EquipmentType[]

GET    /api/projects
       Response: Project[]

GET    /api/suppliers
       Response: Supplier[]
```

### 4.7 Reports

```
GET    /api/reports/equipment
       Query: ?format=pdf|excel&categoryId=&locationId=&status=
       Response: Blob (file download)

GET    /api/reports/certification-status
       Query: ?format=pdf|excel
       Response: Blob

GET    /api/reports/calibration-due
       Query: ?format=pdf|excel
       Response: Blob
```

---

## 5. SCREEN SPECIFICATIONS

### 5.1 Login Screen

```
┌─────────────────────────────────────┐
│                                     │
│          [S7 FATHOM LOGO]           │
│                                     │
│     Equipment Inventory Module      │
│                                     │
│  ┌─────────────────────────────┐   │
│  │ Username                    │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ Password                    │   │
│  └─────────────────────────────┘   │
│                                     │
│  [✓] Remember me                    │
│                                     │
│  ┌─────────────────────────────┐   │
│  │         LOGIN               │   │
│  └─────────────────────────────┘   │
│                                     │
│         ─── OR ───                  │
│                                     │
│  ┌─────────────────────────────┐   │
│  │    LOGIN WITH PIN           │   │
│  └─────────────────────────────┘   │
│                                     │
│  [Offline Mode] if no connection   │
│                                     │
└─────────────────────────────────────┘
```

**Features:**
- Username/password authentication
- PIN login for returning users
- Biometric login (Face ID / Fingerprint)
- "Remember me" checkbox
- Offline mode indicator
- Server URL configuration (settings)

### 5.2 Dashboard Screen (Home)

```
┌─────────────────────────────────────┐
│  ☰  Dashboard           🔔 ⚙️      │
├─────────────────────────────────────┤
│                                     │
│  Welcome back, John!                │
│  Last sync: 2 mins ago  [↻ Sync]   │
│                                     │
│  ┌─────────┐ ┌─────────┐           │
│  │  1,247  │ │   892   │           │
│  │  Total  │ │Available│           │
│  └─────────┘ └─────────┘           │
│  ┌─────────┐ ┌─────────┐           │
│  │   156   │ │   12    │           │
│  │ In Use  │ │In Transit│          │
│  └─────────┘ └─────────┘           │
│                                     │
│  ⚠️ ALERTS (5)                     │
│  ├─ 2 Certifications expired       │
│  ├─ 3 Calibrations overdue         │
│  └─ [View All →]                   │
│                                     │
│  📋 PENDING MANIFESTS (3)          │
│  ├─ MAN-2024-0123 → Base Alpha     │
│  ├─ MAN-2024-0124 → Vessel Echo    │
│  └─ [View All →]                   │
│                                     │
│  🕐 RECENT ACTIVITY                 │
│  ├─ EQ-001234 status → In Use      │
│  ├─ MAN-0123 dispatched            │
│  └─ [View All →]                   │
│                                     │
├─────────────────────────────────────┤
│  🏠    📦    📷    📋    👤        │
│ Home  Equip  Scan Manifests Profile │
└─────────────────────────────────────┘
```

**Features:**
- Summary statistics cards
- Alerts section with badge count
- Pending manifests quick view
- Recent activity feed
- Pull-to-refresh for sync
- Quick action buttons

### 5.3 QR/Barcode Scanner Screen

```
┌─────────────────────────────────────┐
│  ←  Scanner                   🔦   │
├─────────────────────────────────────┤
│                                     │
│  ┌─────────────────────────────┐   │
│  │                             │   │
│  │                             │   │
│  │      [CAMERA VIEWFINDER]    │   │
│  │                             │   │
│  │     ┌───────────────┐       │   │
│  │     │  QR/Barcode   │       │   │
│  │     │   Target Box  │       │   │
│  │     └───────────────┘       │   │
│  │                             │   │
│  │                             │   │
│  └─────────────────────────────┘   │
│                                     │
│  Point camera at QR code or        │
│  barcode on equipment label        │
│                                     │
│  ┌───────────────────────────────┐ │
│  │ 📷 QR Code │ ▮▮▮ Barcode     │ │
│  └───────────────────────────────┘ │
│                                     │
│  💡 Tap flashlight icon for torch  │
│                                     │
│  ─────────────────────────────────  │
│  Recent Scans:                      │
│  • EQ-001234 - Gyro Sensor         │
│  • EQ-001235 - ROV Camera          │
│                                     │
└─────────────────────────────────────┘
```

**Features:**
- Camera viewfinder with targeting overlay
- Support for QR codes and 1D barcodes
- Flashlight toggle
- Auto-focus and auto-detect
- Recent scans history
- Haptic feedback on successful scan
- Audio beep option
- Navigate to equipment detail on scan

### 5.4 Equipment List Screen

```
┌─────────────────────────────────────┐
│  ←  Equipment              🔍 ⚙️   │
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐   │
│  │ 🔍 Search equipment...      │   │
│  └─────────────────────────────┘   │
│                                     │
│  [All ▼] [Category ▼] [Status ▼]   │
│                                     │
│  ┌─────────────────────────────┐   │
│  │ 📦 EQ-001234                │   │
│  │    Gyro Calibration Unit    │   │
│  │    📍 Warehouse A           │   │
│  │    ● Available     ★        │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ 📦 EQ-001235                │   │
│  │    ROV Camera System        │   │
│  │    📍 Vessel Echo           │   │
│  │    ● In Use                 │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ 📦 EQ-001236                │   │
│  │    Dive Computer           │   │
│  │    📍 In Transit            │   │
│  │    ● In Transit  ⚠️ Cal Due │   │
│  └─────────────────────────────┘   │
│                                     │
│  [Load More...]                    │
│                                     │
├─────────────────────────────────────┤
│              [+ Add Equipment]      │
└─────────────────────────────────────┘
```

**Features:**
- Search with debounce
- Filter by category, location, status
- Sort options (name, asset#, date)
- Pull-to-refresh
- Infinite scroll pagination
- Favorite toggle on items
- Status badges with colors
- Alert indicators (cal due, cert expiring)
- Floating action button for new equipment

### 5.5 Equipment Detail Screen

```
┌─────────────────────────────────────┐
│  ←  EQ-001234              ✏️ ⋮    │
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐   │
│  │                             │   │
│  │      [EQUIPMENT PHOTO]      │   │
│  │                             │   │
│  │  ● ● ○ ○  (photo carousel)  │   │
│  └─────────────────────────────┘   │
│                                     │
│  Gyro Calibration Unit    ★        │
│  Serial: GCU-2024-001              │
│  ● Available                       │
│                                     │
│  ═══════════════════════════════   │
│                                     │
│  QUICK ACTIONS                     │
│  [📷 Scan] [📍 Transfer] [📋 Add]  │
│                                     │
│  ═══════════════════════════════   │
│                                     │
│  📍 LOCATION                        │
│  Warehouse A - Shelf B-12          │
│                                     │
│  📦 DETAILS                         │
│  Category: Survey Equipment        │
│  Type: Calibration Tool            │
│  Manufacturer: Kongsberg           │
│  Model: GCU-5000                   │
│                                     │
│  📜 CERTIFICATION                   │
│  Number: CERT-2024-001             │
│  Body: DNV-GL                      │
│  Expires: Mar 15, 2025  ⚠️ 70 days │
│                                     │
│  🔧 CALIBRATION                     │
│  Last: Jan 15, 2024                │
│  Next: Apr 15, 2024  ✓ OK         │
│  Interval: 90 days                 │
│                                     │
│  📝 HISTORY                         │
│  [View Full History →]             │
│                                     │
└─────────────────────────────────────┘
```

**Features:**
- Photo carousel with swipe
- Favorite toggle
- Status badge
- Quick action buttons
- Collapsible sections
- Edit button in header
- More menu (delete, duplicate, print label)
- Share equipment details
- View QR code
- Navigation to history screen

### 5.6 Equipment Editor Screen

```
┌─────────────────────────────────────┐
│  ←  Edit Equipment          💾     │
├─────────────────────────────────────┤
│                                     │
│  BASIC INFORMATION                 │
│  ─────────────────────────────────  │
│  Asset Number*                     │
│  ┌─────────────────────────────┐   │
│  │ EQ-001234              🔒   │   │
│  └─────────────────────────────┘   │
│                                     │
│  Name*                             │
│  ┌─────────────────────────────┐   │
│  │ Gyro Calibration Unit       │   │
│  └─────────────────────────────┘   │
│                                     │
│  Category*                         │
│  ┌─────────────────────────────┐   │
│  │ Survey Equipment        ▼   │   │
│  └─────────────────────────────┘   │
│                                     │
│  Status*                           │
│  ┌─────────────────────────────┐   │
│  │ Available               ▼   │   │
│  └─────────────────────────────┘   │
│                                     │
│  Location*                         │
│  ┌─────────────────────────────┐   │
│  │ Warehouse A             ▼   │   │
│  └─────────────────────────────┘   │
│                                     │
│  Serial Number                     │
│  ┌─────────────────────────────┐   │
│  │ GCU-2024-001                │   │
│  └─────────────────────────────┘   │
│                                     │
│  [Show More Fields ▼]              │
│                                     │
│  PHOTOS                            │
│  ─────────────────────────────────  │
│  [📷] [📷] [📷] [+ Add Photo]      │
│                                     │
├─────────────────────────────────────┤
│  [Cancel]              [Save]      │
└─────────────────────────────────────┘
```

**Features:**
- Form validation with error messages
- Required field indicators
- Dropdown pickers for categories/locations
- Expandable "more fields" section
- Photo management (add/remove/reorder)
- Camera integration for new photos
- Photo library access
- Unsaved changes warning
- Auto-save draft

### 5.7 Manifest List Screen

```
┌─────────────────────────────────────┐
│  ←  Manifests              🔍 ⚙️   │
├─────────────────────────────────────┤
│  [All] [Pending] [In Transit] [Done]│
│                                     │
│  ┌─────────────────────────────┐   │
│  │ 📋 MAN-2024-0123            │   │
│  │    Warehouse A → Base Alpha │   │
│  │    15 items                 │   │
│  │    ● Dispatched  Jan 5      │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ 📋 MAN-2024-0124            │   │
│  │    Base Alpha → Vessel Echo │   │
│  │    8 items                  │   │
│  │    ● Pending                │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ 📋 MAN-2024-0125            │   │
│  │    Vessel Echo → Workshop   │   │
│  │    3 items                  │   │
│  │    ✓ Received    Jan 3      │   │
│  └─────────────────────────────┘   │
│                                     │
│  [Load More...]                    │
│                                     │
├─────────────────────────────────────┤
│              [+ New Manifest]      │
└─────────────────────────────────────┘
```

### 5.8 Manifest Detail Screen

```
┌─────────────────────────────────────┐
│  ←  MAN-2024-0123           ⋮      │
├─────────────────────────────────────┤
│                                     │
│  ● DISPATCHED                      │
│  Jan 5, 2024 at 10:30 AM          │
│                                     │
│  FROM                              │
│  📍 Warehouse A, Aberdeen          │
│                                     │
│  TO                                │
│  📍 Base Alpha, North Sea          │
│                                     │
│  ═══════════════════════════════   │
│                                     │
│  ITEMS (15)                        │
│  ┌─────────────────────────────┐   │
│  │ ☐ EQ-001234                 │   │
│  │   Gyro Calibration Unit     │   │
│  │   Qty: 1                    │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ ☐ EQ-001235                 │   │
│  │   ROV Camera System         │   │
│  │   Qty: 2                    │   │
│  └─────────────────────────────┘   │
│  ... more items ...                │
│                                     │
├─────────────────────────────────────┤
│  [📷 Scan to Receive]              │
│                                     │
│  [Mark All Received]               │
│  [Complete Receipt]                │
└─────────────────────────────────────┘
```

**Features:**
- Status timeline
- Source and destination display
- Item list with checkboxes
- Scan button to receive items
- Batch receive all
- Partial receipt support
- Notes for discrepancies
- Photo attachment for condition

### 5.9 Maintenance Schedule Screen

```
┌─────────────────────────────────────┐
│  ←  Maintenance             🔔     │
├─────────────────────────────────────┤
│                                     │
│  ┌─────────┐ ┌─────────┐           │
│  │    5    │ │   12    │           │
│  │ Overdue │ │This Week│           │
│  └─────────┘ └─────────┘           │
│                                     │
│  [All] [Cert] [Cal] [Service]      │
│                                     │
│  ⚠️ OVERDUE                        │
│  ┌─────────────────────────────┐   │
│  │ 🔴 EQ-001234                │   │
│  │    Calibration overdue      │   │
│  │    Due: Dec 15 (21 days ago)│   │
│  │    [Complete →]             │   │
│  └─────────────────────────────┘   │
│                                     │
│  📅 UPCOMING                        │
│  ┌─────────────────────────────┐   │
│  │ 🟡 EQ-001235                │   │
│  │    Certification expiring   │   │
│  │    Due: Jan 20 (15 days)    │   │
│  │    [Complete →]             │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ 🟢 EQ-001236                │   │
│  │    Service due              │   │
│  │    Due: Feb 1 (27 days)     │   │
│  │    [Complete →]             │   │
│  └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

### 5.10 Equipment History Screen

```
┌─────────────────────────────────────┐
│  ←  History - EQ-001234            │
├─────────────────────────────────────┤
│                                     │
│  Filter: [All Actions ▼]           │
│                                     │
│  TODAY                             │
│  ┌─────────────────────────────┐   │
│  │ ● Status Changed            │   │
│  │   Available → In Use        │   │
│  │   10:30 AM • John Smith     │   │
│  └─────────────────────────────┘   │
│                                     │
│  YESTERDAY                         │
│  ┌─────────────────────────────┐   │
│  │ ● Transferred               │   │
│  │   Warehouse A → Vessel Echo │   │
│  │   MAN-2024-0123             │   │
│  │   2:15 PM • Jane Doe        │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │ ● Photo Added               │   │
│  │   Equipment photo updated   │   │
│  │   11:00 AM • John Smith     │   │
│  └─────────────────────────────┘   │
│                                     │
│  JAN 3, 2024                       │
│  ┌─────────────────────────────┐   │
│  │ ● Calibrated                │   │
│  │   Calibration completed     │   │
│  │   Next due: Apr 3, 2024     │   │
│  │   9:00 AM • Calibration Lab │   │
│  └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

---

## 6. OFFLINE-FIRST ARCHITECTURE

### 6.1 Local Database Schema

```sql
-- Equipment table (local SQLite)
CREATE TABLE equipment (
  equipment_id TEXT PRIMARY KEY,
  asset_number TEXT UNIQUE NOT NULL,
  unique_id TEXT UNIQUE NOT NULL,
  name TEXT,
  -- ... all equipment fields ...
  
  -- Sync fields
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL,
  is_modified_locally INTEGER DEFAULT 0,
  local_version INTEGER DEFAULT 1,
  server_version INTEGER DEFAULT 0,
  sync_status TEXT DEFAULT 'pending', -- 'synced', 'pending', 'conflict'
  last_synced_at TEXT
);

-- Offline queue table
CREATE TABLE offline_queue (
  queue_id TEXT PRIMARY KEY,
  entity_type TEXT NOT NULL,    -- 'equipment', 'manifest', etc.
  entity_id TEXT NOT NULL,
  operation TEXT NOT NULL,      -- 'create', 'update', 'delete'
  payload TEXT NOT NULL,        -- JSON of changes
  created_at TEXT NOT NULL,
  retry_count INTEGER DEFAULT 0,
  last_error TEXT
);

-- Sync metadata table
CREATE TABLE sync_metadata (
  key TEXT PRIMARY KEY,
  value TEXT
);
-- Keys: 'last_sync_time', 'sync_in_progress', etc.
```

### 6.2 Sync Flow

```
┌─────────────────────────────────────────────────────────────┐
│                      SYNC PROCESS                           │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. CHECK CONNECTIVITY                                      │
│     └─→ If offline, queue changes locally                   │
│                                                             │
│  2. PUSH LOCAL CHANGES (if online)                          │
│     ├─→ Get items from offline_queue                        │
│     ├─→ POST /api/sync/push                                 │
│     ├─→ Handle conflicts                                    │
│     └─→ Clear processed queue items                         │
│                                                             │
│  3. PULL SERVER CHANGES                                     │
│     ├─→ GET /api/sync/changes?since={lastSync}              │
│     ├─→ Apply changes to local DB                           │
│     └─→ Update sync metadata                                │
│                                                             │
│  4. RESOLVE CONFLICTS                                       │
│     ├─→ Show conflict UI if needed                          │
│     ├─→ Let user choose: Keep Local / Keep Server / Merge   │
│     └─→ Apply resolution                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 6.3 Conflict Resolution UI

```
┌─────────────────────────────────────┐
│  ⚠️ Sync Conflict                   │
├─────────────────────────────────────┤
│                                     │
│  Equipment: EQ-001234               │
│  Gyro Calibration Unit              │
│                                     │
│  CONFLICT: Status field             │
│                                     │
│  ┌──────────────┬──────────────┐   │
│  │ Your Change  │ Server Value │   │
│  ├──────────────┼──────────────┤   │
│  │  In Use      │  In Transit  │   │
│  └──────────────┴──────────────┘   │
│                                     │
│  Modified by: Jane Doe              │
│  Server time: Jan 5, 10:45 AM      │
│                                     │
│  ┌─────────────────────────────┐   │
│  │      Keep My Changes        │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │     Accept Server Value     │   │
│  └─────────────────────────────┘   │
│  ┌─────────────────────────────┐   │
│  │     Review & Merge          │   │
│  └─────────────────────────────┘   │
│                                     │
└─────────────────────────────────────┘
```

---

## 7. PUSH NOTIFICATIONS

### 7.1 Notification Types

| Type | Trigger | Priority |
|------|---------|----------|
| `certification_expiring` | 30/14/7 days before expiry | High |
| `certification_expired` | On expiry date | Critical |
| `calibration_due` | 7 days before due | Medium |
| `calibration_overdue` | On due date | High |
| `manifest_dispatched` | When manifest dispatched to user's location | Medium |
| `manifest_received` | When user's manifest marked received | Low |
| `equipment_transferred` | Equipment transferred to user's location | Medium |
| `sync_required` | After 24h without sync | Low |
| `low_stock_alert` | Consumable below minimum | Medium |

### 7.2 Notification Payload

```json
{
  "notification": {
    "title": "Calibration Due",
    "body": "EQ-001234 - Gyro Unit calibration due in 7 days"
  },
  "data": {
    "type": "calibration_due",
    "equipmentId": "abc-123-def",
    "assetNumber": "EQ-001234",
    "dueDate": "2024-01-15",
    "action": "view_equipment"
  }
}
```

---

## 8. BATCH OPERATIONS (MOBILE)

### 8.1 Bulk Status Update

```
┌─────────────────────────────────────┐
│  ←  Bulk Update Status             │
├─────────────────────────────────────┤
│                                     │
│  Selected: 5 items                  │
│                                     │
│  NEW STATUS                        │
│  ┌─────────────────────────────┐   │
│  │ In Use                   ▼   │   │
│  └─────────────────────────────┘   │
│                                     │
│  REASON (optional)                 │
│  ┌─────────────────────────────┐   │
│  │ Deployed to Project X       │   │
│  └─────────────────────────────┘   │
│                                     │
│  SELECTED ITEMS                    │
│  ☑ EQ-001234 - Gyro Unit          │
│  ☑ EQ-001235 - ROV Camera         │
│  ☑ EQ-001236 - Dive Computer      │
│  ☑ EQ-001237 - Sonar Array        │
│  ☑ EQ-001238 - GPS Module         │
│                                     │
├─────────────────────────────────────┤
│  [Cancel]           [Update All]   │
└─────────────────────────────────────┘
```

### 8.2 Multi-Select Mode

- Long-press on equipment item to enter selection mode
- Tap items to toggle selection
- Action bar appears with bulk actions
- Actions: Update Status, Transfer, Add to Manifest, Delete

---

## 9. TESTING REQUIREMENTS

### 9.1 Test Scenarios

1. **Offline Mode Testing**
   - Create equipment while offline
   - Edit equipment while offline
   - Queue manifest while offline
   - Sync when connection restored
   - Conflict resolution

2. **Scanner Testing**
   - QR code scanning in various lighting
   - Barcode scanning (Code 128, Code 39)
   - Invalid code handling
   - Camera permissions

3. **Sync Testing**
   - Full sync with large dataset
   - Delta sync performance
   - Conflict scenarios
   - Network interruption recovery

4. **Performance Testing**
   - List scrolling with 10,000+ items
   - Search response time
   - Photo upload/download
   - App startup time

---

## 10. DEPLOYMENT CHECKLIST

### 10.1 Pre-Release

- [ ] API endpoint configuration
- [ ] Push notification setup (FCM/APNS)
- [ ] Analytics integration
- [ ] Crash reporting (Crashlytics/Sentry)
- [ ] Deep linking configuration
- [ ] App store assets (screenshots, descriptions)

### 10.2 App Store Requirements

**iOS:**
- Minimum iOS 14.0
- Camera usage description
- Photo library usage description
- Location usage description (if GPS features)
- Background fetch capability

**Android:**
- Minimum SDK 29 (Android 10)
- Camera permission
- Storage permission
- Network state permission
- Background sync permission

---

## 11. CONTACT & SUPPORT

For questions about the desktop module implementation or API specifications:

- **Desktop Module:** WPF implementation complete (~16,000 lines)
- **Architecture:** MVVM with offline-first SQLite
- **Sync Protocol:** Delta sync with conflict resolution
- **Export Formats:** Excel (ClosedXML), PDF (QuestPDF)

---

## VERSION HISTORY

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Dec 2024 | Initial specification |
| 2.0 | Jan 2026 | Complete feature update with all new services |

---

**END OF MOBILE APP REQUIREMENTS DOCUMENT**
