# 📱 Fathom OS Mobile App - Equipment Module Integration Update

## Version: January 2026 | Priority: HIGH

---

## 🎯 Overview

This message contains all the updates required for the mobile app to integrate with the desktop Equipment & Inventory Module. The desktop module has been enhanced with:

1. **Shipment Verification Workflow** - Complete inward manifest verification
2. **UniqueId Auto-Generation** - Automatic asset identification
3. **Unregistered Items** - Support for items not in the system
4. **Label Printing** - QR code label generation and printing
5. **Enhanced QR Code Format** - New format with backward compatibility
6. **Location User Assignment** - Assign users to locations for filtering
7. **Multiple Entry Points** - Select from pending manifests OR manual entry
8. **⭐ Defect Reports (EFN)** - Equipment Failure Notification system (NEW)

---

## 📦 1. SHIPMENT VERIFICATION WORKFLOW

### Overview
When an outward manifest arrives at the destination, users verify the shipment by:
1. **Option A**: Select from pending inbound shipments list (filtered by user's location)
2. **Option B**: Enter manifest number manually
3. **Option C**: Scan QR code
4. Scan/search each item to verify receipt
5. Mark items as damaged/missing/extra
6. Complete verification → create inward manifest

### Mobile App Requirements

#### 1.1 Start Verification Screen (UPDATED)
```
┌─────────────────────────────────────────┐
│  📦 Verify Shipment                     │
├─────────────────────────────────────────┤
│                                         │
│  PENDING INBOUND SHIPMENTS (3)     [↻]  │
│  ┌─────────────────────────────────┐    │
│  │ OUT-2026-00015  Aberdeen    12  │ ▶  │
│  │ OUT-2026-00012  Houston      8  │ ▶  │
│  │ OUT-2026-00009  Singapore   25  │ ▶  │
│  └─────────────────────────────────┘    │
│                                         │
│  ─────────── OR ───────────             │
│                                         │
│  ENTER MANIFEST NUMBER                  │
│  ┌─────────────────────────────────┐    │
│  │ OUT-2026-00015                  │    │
│  └─────────────────────────────────┘    │
│  [Load Shipment]                        │
│                                         │
│  ─────────── OR ───────────             │
│                                         │
│  [🔍 Scan QR Code]                      │
│                                         │
└─────────────────────────────────────────┘
```

#### 1.2 Verification Screen (UPDATED)
```
┌─────────────────────────────────────────┐
│  📦 OUT-2026-00001                      │
│  From: Aberdeen Base → Vessel Explorer  │
├─────────────────────────────────────────┤
│  Progress: ████████░░ 8/10 (80%)        │
│  ✓ Verified: 6  ⚠️ Damaged: 1  ❌ Missing: 1│
├─────────────────────────────────────────┤
│                                         │
│  SCAN OR ENTER ITEM CODE                │
│  ┌──────────────────────────────────┐   │
│  │ S7WSS04068                       │🔍 │
│  └──────────────────────────────────┘   │
│  [Verify] [Search 🔎] [+ Add Manual]    │
│                                         │
│  Accepts: UniqueId, Asset #, Serial #,  │
│           or full QR code               │
│                                         │
│  EXPECTED ITEMS                         │
│  ┌─────────────────────────────────┐    │
│  │ ✓ ROV Camera Unit     S7CAM001  │    │
│  │ ✓ Sonar Transducer    S7SON002  │    │
│  │ ⚠️ Hydraulic Pump      S7HYD003  │    │
│  │   [View Damage Notes]           │    │
│  │ ❌ Cable Assembly      S7CAB004  │    │
│  │   [Mark Found]                  │    │
│  │ ⏳ DVL Sensor          S7DVL005  │    │
│  │   [Mark Missing] [Mark Damaged] │    │
│  └─────────────────────────────────┘    │
│                                         │
│  EXTRA ITEMS (not on manifest)          │
│  ┌─────────────────────────────────┐    │
│  │ ➕ Spare Parts Kit    S7SPR099   │    │
│  └─────────────────────────────────┘    │
│                                         │
│  [+ Add Unregistered Item]              │
│                                         │
├─────────────────────────────────────────┤
│  [Save Progress]    [Complete ✓]        │
└─────────────────────────────────────────┘
```

#### 1.3 Search Expected Items Dialog (NEW)
When user taps the Search button, show a dialog with ONLY items from the current manifest:
```
┌─────────────────────────────────────────┐
│  🔍 Search Expected Items               │
├─────────────────────────────────────────┤
│  ┌─────────────────────────────────┐    │
│  │ Search by name, ID, serial...   │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ┌─────────────────────────────────┐    │
│  │ ⏳ ROV Camera        S7CAM001   │    │
│  │ ⏳ Sonar Unit        S7SON002   │    │
│  │ ⏳ DVL Sensor        S7DVL005   │    │
│  └─────────────────────────────────┘    │
│                                         │
│  [Cancel]            [Verify Selected]  │
└─────────────────────────────────────────┘
```

#### 1.4 API Endpoints for Verification

```http
# NEW: Get pending inbound manifests for a location
GET /api/manifests/pending-inbound/{locationId}
Response: List<Manifest> where status = Submitted|InTransit and ToLocationId = locationId

# Get shipment by manifest number or QR code
GET /api/manifests/by-number/{manifestNumber}
GET /api/manifests/by-qr/{qrCode}

# Get manifest items for verification
GET /api/manifests/{manifestId}/items

# Update item verification status
PATCH /api/manifests/{manifestId}/items/{itemId}/verify
Body: {
  "verificationStatus": "Verified|Damaged|Missing|Extra",
  "damageNotes": "string (optional)",
  "verifiedAt": "2026-01-11T10:30:00Z"
}

# Add extra item (scanned but not on manifest)
POST /api/manifests/{manifestId}/items/extra
Body: {
  "equipmentId": "guid (if in system)",
  "assetNumber": "string",
  "uniqueId": "string",
  "name": "string",
  "verificationStatus": "Extra"
}

# Save verification progress
PATCH /api/manifests/{manifestId}/verification-progress
Body: {
  "verifiedItemCount": 8,
  "discrepancyCount": 2,
  "verificationStatus": "InProgress"
}

# Complete verification
POST /api/manifests/{manifestId}/complete-verification
Body: {
  "verificationSummary": "8/10 verified, 1 damaged, 1 missing",
  "hasDiscrepancies": true
}
```

#### 1.5 Smart Input Parsing (IMPORTANT)
The item input field should intelligently recognize multiple formats:

```typescript
function parseItemCode(input: string): ParseResult {
  const trimmed = input.trim();
  
  // Full QR code formats
  if (trimmed.startsWith('foseq:')) {
    const [uniqueId, assetNumber] = trimmed.substring(6).split('|');
    return { type: 'equipment', uniqueId, assetNumber };
  }
  if (trimmed.startsWith('s7eq:')) {
    const [assetNumber, uniqueId] = trimmed.substring(5).split('|');
    return { type: 'equipment', uniqueId, assetNumber };
  }
  
  // UniqueId pattern (e.g., S7WSS04068)
  if (/^[A-Z]{2,5}[A-Z]{2,4}\d{5}$/.test(trimmed)) {
    return { type: 'uniqueId', value: trimmed };
  }
  
  // Asset number pattern (e.g., EQ-2026-00001)
  if (/^[A-Z]{2,4}-\d{4}-\d{5}$/.test(trimmed)) {
    return { type: 'assetNumber', value: trimmed };
  }
  
  // Serial number (any other format)
  return { type: 'serialNumber', value: trimmed };
}
```

---

## 👥 2. LOCATION USER ASSIGNMENT (NEW)

### Overview
Users can now be assigned to locations. This enables:
- Filtering pending manifests by user's assigned location
- Restricting access to location-specific operations
- Identifying who can receive shipments at each location

### Location Assignment Model
```typescript
interface UserLocation {
  userId: string;
  locationId: string;
  accessLevel: 'Read' | 'Write' | 'Admin';
}
```

### API Endpoints
```http
# Get users assigned to a location
GET /api/locations/{locationId}/users
Response: List<UserLocation>

# Assign user to location
POST /api/locations/{locationId}/users
Body: { "userId": "guid", "accessLevel": "Write" }

# Remove user from location
DELETE /api/locations/{locationId}/users/{userId}

# Get locations for current user
GET /api/users/me/locations
Response: List<Location>
```

### Mobile Implementation
```typescript
// Get current user's location for filtering
const userLocations = await api.get('/users/me/locations');
const primaryLocation = userLocations[0]; // Or let user select

// Load pending manifests for user's location
const pendingManifests = await api.get(
  `/manifests/pending-inbound/${primaryLocation.locationId}`
);
```

---

## 🆔 3. UNIQUE ID SYSTEM

### 3.1 UniqueId Format
```
Format: {OrgCode}{CategoryCode}{Sequence}

Examples:
- S7WSS04068  (S7 = org, WSS = category, 04068 = sequence)
- FOSCAM00123 (FOS = org, CAM = category, 00123 = sequence)
```

### 3.2 Auto-Generation Rules
- **Server-side generation** - UniqueId is generated by the server when equipment is created
- **Category-based** - Uses the equipment category code
- **Sequential** - 5-digit sequence number per category
- **Immutable** - Once assigned, UniqueId never changes

---

## 📷 4. QR CODE FORMAT UPDATE

### 4.1 New QR Code Format
```
Equipment QR:  foseq:{UniqueId}|{AssetNumber}
               foseq:S7WSS04068|EQ-2026-00001

Manifest QR:   fosman:{ManifestNumber}
               fosman:OUT-2026-00001

Defect/EFN QR: fosefn:{EfnNumber}
               fosefn:EFN-2026-00001
```

### 4.2 Backward Compatibility
The mobile app MUST support both old and new formats:

```typescript
function parseQrCode(qrCode: string): QrParseResult {
  // New format (Fathom OS)
  if (qrCode.startsWith('foseq:')) {
    const content = qrCode.substring(6);
    const [uniqueId, assetNumber] = content.split('|');
    return { type: 'equipment', uniqueId, assetNumber };
  }
  if (qrCode.startsWith('fosman:')) {
    return { type: 'manifest', manifestNumber: qrCode.substring(7) };
  }
  if (qrCode.startsWith('fosefn:')) {
    return { type: 'defect', efnNumber: qrCode.substring(7) };
  }
  
  // Legacy format (S7 Fathom)
  if (qrCode.startsWith('s7eq:')) {
    const content = qrCode.substring(5);
    const [assetNumber, uniqueId] = content.split('|');
    return { type: 'equipment', uniqueId, assetNumber };
  }
  if (qrCode.startsWith('s7mn:')) {
    return { type: 'manifest', manifestNumber: qrCode.substring(5) };
  }
  if (qrCode.startsWith('s7efn:')) {
    return { type: 'defect', efnNumber: qrCode.substring(6) };
  }
  
  // Plain manifest number
  if (qrCode.match(/^(OUT|IN)-\d{4}-\d{5}$/)) {
    return { type: 'manifest', manifestNumber: qrCode };
  }
  
  // Plain UniqueId
  if (qrCode.match(/^[A-Z]{2,5}[A-Z]{2,4}\d{5}$/)) {
    return { type: 'equipment', uniqueId: qrCode };
  }
  
  return { type: 'unknown', raw: qrCode };
}
```

---

## 📝 5. UNREGISTERED ITEMS

### 5.1 Overview
Users can add items that are NOT in the equipment database. These go to a **Pending Review** list for inventory management to process.

### 5.2 Unregistered Item Model
```typescript
interface UnregisteredItem {
  unregisteredItemId: string;
  manifestId: string;
  name: string;
  description?: string;
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  partNumber?: string;
  quantity: number;
  unitOfMeasure?: string;
  suggestedCategoryId?: string;
  suggestedTypeId?: string;
  isConsumable: boolean;
  currentLocationId?: string;
  status: 'PendingReview' | 'ConvertedToEquipment' | 'KeptAsConsumable' | 'Rejected';
  photoUrls?: string[];
  createdBy?: string;
  createdAt: string;
}
```

### 5.3 API Endpoints
```http
POST /api/unregistered-items
GET /api/unregistered-items?status=PendingReview
POST /api/unregistered-items/{id}/convert-to-equipment
POST /api/unregistered-items/{id}/keep-as-consumable
POST /api/unregistered-items/{id}/reject
```

---

## 🖨️ 6. LABEL PRINTING (REQUIRED)

### 6.1 Supported Printer Types
| Printer Type | Connection | Notes |
|--------------|------------|-------|
| Zebra ZD410/ZD420 | Bluetooth | Primary recommendation |
| Brother QL-820NWB | Bluetooth/Wi-Fi | Good alternative |
| Generic Thermal | Bluetooth | Basic support |
| AirPrint | Wi-Fi | iOS only |
| Share/Export | N/A | Fallback - save as image |

### 6.2 Print Flow
```
Equipment Detail → [Print Label] → Select Printer → Select Size → Print
                                  → [Share Image] (fallback)
```

---

## ⌨️ 7. KEYBOARD SHORTCUTS (Desktop Reference)

The desktop app supports these shortcuts - mobile should implement equivalents where possible:

| Shortcut | Action |
|----------|--------|
| F5 | Refresh pending manifests |
| Ctrl+F | Search expected items |
| Ctrl+S | Save verification progress |
| Enter | Load manifest / Verify item |
| Escape | Cancel |

---

## ⚠️ 8. DEFECT REPORTS (EFN) - Equipment Failure Notification ⭐ NEW

### 8.1 Overview
The Equipment Failure Notification (EFN) system allows field personnel to report equipment failures immediately, with full offline support. Based on Subsea7 EFN Form FO-GL-ITS-EQP-003.

### 8.2 EFN Workflow
```
┌──────────────────────────────────────────────────────────────┐
│                    EFN WORKFLOW                               │
├──────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────┐    ┌───────────┐    ┌────────────┐             │
│  │  Draft   │ -> │ Submitted │ -> │Under Review│             │
│  └──────────┘    └───────────┘    └────────────┘             │
│       │                                  │                    │
│       │                                  ▼                    │
│  [Save for      [Submit]          ┌────────────┐             │
│   later]                          │In Progress │             │
│                                   └────────────┘             │
│                                          │                    │
│                                          ▼                    │
│                                   ┌────────────┐             │
│                                   │  Resolved  │             │
│                                   └────────────┘             │
│                                          │                    │
│                                          ▼                    │
│                                   ┌────────────┐             │
│                                   │   Closed   │             │
│                                   └────────────┘             │
│                                                               │
└──────────────────────────────────────────────────────────────┘
```

### 8.3 Defect Report Model
```typescript
interface DefectReport {
  defectReportId: string;
  reportNumber: string;          // "EFN-2026-00001"
  qrCode?: string;
  
  // Failure Details (Page 1)
  createdByUserId: string;
  createdByName?: string;
  reportDate: string;
  clientProject?: string;
  locationId?: string;
  locationName?: string;
  thirdPartyLocationName?: string;
  rovSystem?: string;
  workingWaterDepthMetres?: number;
  
  // Equipment Info
  equipmentOrigin?: EquipmentOrigin;
  equipmentId?: string;
  equipmentCategoryId?: string;
  majorComponent?: string;
  minorComponent?: string;
  
  // Equipment Details (Page 2)
  ownershipType: 'Internal' | 'External';
  equipmentOwner?: string;
  responsibilityType: 'Standard' | 'Project';
  sapIdOrVendorAssetId?: string;
  serialNumber?: string;
  manufacturer?: string;
  model?: string;
  
  // Symptoms / Action Taken
  faultCategory: FaultCategory;
  detailedSymptoms?: string;
  photosAttached: boolean;
  actionTaken?: string;
  partsAvailableOnBoard: boolean;
  replacementRequired: boolean;
  replacementUrgency: ReplacementUrgency;
  furtherComments?: string;
  nextPortCallDate?: string;
  nextPortCallLocation?: string;
  repairDurationMinutes?: number;
  downtimeDurationMinutes?: number;
  
  // Workflow Status
  status: DefectReportStatus;
  submittedAt?: string;
  submittedByUserId?: string;
  assignedToUserId?: string;
  reviewedAt?: string;
  reviewedByUserId?: string;
  reviewNotes?: string;
  resolvedAt?: string;
  resolvedByUserId?: string;
  resolutionNotes?: string;
  closedAt?: string;
  closedByUserId?: string;
  
  // Metadata
  createdAt: string;
  updatedAt: string;
  syncStatus: SyncStatus;
}

// Equipment Origin Options (from EFN form)
type EquipmentOrigin = 
  | 'Modular Handling System'
  | 'ROV'
  | 'Simulator'
  | 'Tooling'
  | 'Vessel / Rig'
  | 'Survey & Inspection';

// Fault Categories (17 types)
type FaultCategory = 
  | 'MechanicalFailure'
  | 'ElectricalFailure'
  | 'HydraulicFailure'
  | 'SoftwareIssue'
  | 'CalibrationIssue'
  | 'CommunicationFailure'
  | 'PhysicalDamage'
  | 'CorrosionWear'
  | 'Leak'
  | 'Overheating'
  | 'VibrationNoise'
  | 'SensorMalfunction'
  | 'PowerSupplyIssue'
  | 'ConnectorCableIssue'
  | 'UserError'
  | 'Unknown'
  | 'Other';

// Urgency Levels
type ReplacementUrgency = 
  | 'High'    // Critical, within 24 hours
  | 'Medium'  // Alternative available, next port call
  | 'Low';    // Spares on board

// Workflow Status
type DefectReportStatus = 
  | 'Draft'
  | 'Submitted'
  | 'UnderReview'
  | 'Returned'
  | 'InProgress'
  | 'Resolved'
  | 'Closed'
  | 'Cancelled';
```

### 8.4 Defect Report Parts Model
```typescript
interface DefectReportPart {
  partId: string;
  defectReportId: string;
  
  // Part Details
  sapNumber?: string;
  description: string;
  modelNumber?: string;
  serialNumber?: string;
  
  // Quantities
  failedQuantity: number;
  requiredQuantity: number;
  
  createdAt: string;
  syncStatus: SyncStatus;
}
```

### 8.5 Create Defect Report Screen (Step 1: Failure Details)
```
┌─────────────────────────────────────────┐
│ ← Equipment Failure Notification        │
├─────────────────────────────────────────┤
│  Step 1 of 4: Failure Details           │
│  ━━━━━━━━━━━━━○───────────────          │
│                                         │
│  EQUIPMENT (Optional)                   │
│  ┌─────────────────────────────────┐    │
│  │ [🔍 Scan] or [Search Equipment] │    │
│  └─────────────────────────────────┘    │
│  Selected: S7WSS04068 - Hydraulic Pump  │
│                                         │
│  LOCATION *                             │
│  ┌─────────────────────────────────┐    │
│  │ 📍 Aberdeen Base              ▼ │    │
│  └─────────────────────────────────┘    │
│                                         │
│  Or 3rd Party Location:                 │
│  ┌─────────────────────────────────┐    │
│  │                                 │    │
│  └─────────────────────────────────┘    │
│                                         │
│  CLIENT/PROJECT                         │
│  ┌─────────────────────────────────┐    │
│  │ Equinor Johan Sverdrup          │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ROV SYSTEM                             │
│  ┌─────────────────────────────────┐    │
│  │ Schilling HD                    │    │
│  └─────────────────────────────────┘    │
│                                         │
│  WORKING WATER DEPTH (metres)           │
│  ┌─────────────────────────────────┐    │
│  │ 350                             │    │
│  └─────────────────────────────────┘    │
│                                         │
│  EQUIPMENT ORIGIN *                     │
│  ○ Modular Handling System              │
│  ● ROV                                  │
│  ○ Simulator                            │
│  ○ Tooling                              │
│  ○ Vessel / Rig                         │
│  ○ Survey & Inspection                  │
│                                         │
│  ┌─────────────────────────────────┐    │
│  │            NEXT →                │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

### 8.6 Create Defect Report Screen (Step 2: Fault Details)
```
┌─────────────────────────────────────────┐
│ ← Equipment Failure Notification        │
├─────────────────────────────────────────┤
│  Step 2 of 4: Fault Details             │
│  ━━━━━━━━━━━━━━━━━━━━○───────           │
│                                         │
│  FAULT CATEGORY *                       │
│  ┌─────────────────────────────────┐    │
│  │ Hydraulic Failure             ▼ │    │
│  └─────────────────────────────────┘    │
│                                         │
│  DETAILED SYMPTOMS *                    │
│  ┌─────────────────────────────────┐    │
│  │ Pump not generating sufficient  │    │
│  │ pressure. Observed fluid leak   │    │
│  │ around main seal area during    │    │
│  │ operations at 350m depth.       │    │
│  │                                 │    │
│  └─────────────────────────────────┘    │
│                                         │
│  PHOTOS TAKEN                           │
│  ┌────┐ ┌────┐ ┌────┐                   │
│  │📷  │ │📷  │ │ +  │                   │
│  │img1│ │img2│ │Add │                   │
│  └────┘ └────┘ └────┘                   │
│                                         │
│  ACTION TAKEN                           │
│  ┌─────────────────────────────────┐    │
│  │ Inspected seals, isolated unit  │    │
│  │ from hydraulic system. Attempted│    │
│  │ minor seal replacement but leak │    │
│  │ persists.                       │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ┌─────────┐      ┌─────────────────┐   │
│  │ ← BACK  │      │     NEXT →      │   │
│  └─────────┘      └─────────────────┘   │
└─────────────────────────────────────────┘
```

### 8.7 Create Defect Report Screen (Step 3: Parts & Urgency)
```
┌─────────────────────────────────────────┐
│ ← Equipment Failure Notification        │
├─────────────────────────────────────────┤
│  Step 3 of 4: Parts & Urgency           │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━○──       │
│                                         │
│  PARTS AVAILABLE ON BOARD? *            │
│  ○ Yes    ● No                          │
│                                         │
│  REPLACEMENT REQUIRED? *                │
│  ● Yes    ○ No                          │
│                                         │
│  URGENCY *                              │
│  🔴 HIGH - Critical, within 24 hrs      │
│  ○ 🟡 MEDIUM - Next port call           │
│  ○ 🟢 LOW - Spares on board             │
│                                         │
│  PARTS FAILED / REQUIRED                │
│  ┌─────────────────────────────────┐    │
│  │ 1. Seal kit                     │    │
│  │    SAP: 12345678                │    │
│  │    Failed: 2  Required: 2       │    │
│  ├─────────────────────────────────┤    │
│  │ 2. O-ring set                   │    │
│  │    SAP: 87654321                │    │
│  │    Failed: 1  Required: 2       │    │
│  └─────────────────────────────────┘    │
│  [+ Add Part]                           │
│                                         │
│  NEXT PORT CALL                         │
│  📅 2026-01-20  📍 Aberdeen             │
│                                         │
│  REPAIR DURATION        DOWNTIME        │
│  ┌──────────┐          ┌──────────┐     │
│  │ 120 mins │          │ 240 mins │     │
│  └──────────┘          └──────────┘     │
│                                         │
│  ┌─────────┐      ┌─────────────────┐   │
│  │ ← BACK  │      │     NEXT →      │   │
│  └─────────┘      └─────────────────┘   │
└─────────────────────────────────────────┘
```

### 8.8 Create Defect Report Screen (Step 4: Review & Submit)
```
┌─────────────────────────────────────────┐
│ ← Equipment Failure Notification        │
├─────────────────────────────────────────┤
│  Step 4 of 4: Review & Submit           │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│                                         │
│  REPORT SUMMARY                         │
│  ┌─────────────────────────────────┐    │
│  │ Report #: EFN-2026-00015        │    │
│  │ Date: 2026-01-15                │    │
│  │ Location: Aberdeen Base         │    │
│  │                                 │    │
│  │ Equipment: Hydraulic Pump       │    │
│  │ UniqueId: S7WSS04068            │    │
│  │                                 │    │
│  │ Origin: ROV                     │    │
│  │ Fault: Hydraulic Failure        │    │
│  │ Urgency: 🔴 HIGH                │    │
│  │                                 │    │
│  │ Parts Required: 2 items         │    │
│  │ Photos: 2 attached              │    │
│  └─────────────────────────────────┘    │
│                                         │
│  FURTHER COMMENTS                       │
│  ┌─────────────────────────────────┐    │
│  │ Recommend expedited shipping of │    │
│  │ seal kit from Houston warehouse.│    │
│  │ Equipment critical for upcoming │    │
│  │ operations.                     │    │
│  └─────────────────────────────────┘    │
│                                         │
│  ┌─────────────────────────────────┐    │
│  │      📨 SUBMIT REPORT           │    │
│  └─────────────────────────────────┘    │
│                                         │
│  [Save as Draft]     [← Back]           │
└─────────────────────────────────────────┘
```

### 8.9 Defect Reports List Screen
```
┌─────────────────────────────────────────┐
│ ☰  Defect Reports              + New    │
├─────────────────────────────────────────┤
│  ┌─────────────────────────────────┐    │
│  │ 🔍 Search reports...            │    │
│  └─────────────────────────────────┘    │
│                                         │
│  Filter: [All ▼] [All Status ▼]         │
│                                         │
│  ┌─────────────────────────────────┐    │
│  │ 🔴 EFN-2026-00015               │    │
│  │    Hydraulic Pump - Seal Failure│    │
│  │    Aberdeen | Today | HIGH      │    │
│  │    Status: Submitted            │    │
│  ├─────────────────────────────────┤    │
│  │ 🟡 EFN-2026-00014               │    │
│  │    ROV Camera - Lens damage     │    │
│  │    Houston | Yesterday | MEDIUM │    │
│  │    Status: In Progress          │    │
│  ├─────────────────────────────────┤    │
│  │ 🟢 EFN-2026-00013               │    │
│  │    Control Unit - Software bug  │    │
│  │    Singapore | Jan 12 | LOW     │    │
│  │    Status: Resolved             │    │
│  └─────────────────────────────────┘    │
│                                         │
│  Showing 3 of 15 reports                │
└─────────────────────────────────────────┘
```

### 8.10 API Endpoints for Defect Reports
```http
# Get defect reports (paginated)
GET /api/defect-reports?status=Submitted&page=1&pageSize=20

# Get defect reports for equipment
GET /api/defect-reports/by-equipment/{equipmentId}

# Get defect report by ID
GET /api/defect-reports/{defectReportId}

# Get defect report by report number
GET /api/defect-reports/by-number/{reportNumber}

# Create defect report
POST /api/defect-reports
Body: {
  "equipmentId": "guid (optional)",
  "locationId": "guid",
  "equipmentOrigin": "ROV",
  "faultCategory": "HydraulicFailure",
  "detailedSymptoms": "Pump not generating pressure...",
  "actionTaken": "Inspected seals...",
  "replacementUrgency": "High",
  "replacementRequired": true,
  ...
}

# Update defect report
PUT /api/defect-reports/{defectReportId}

# Add photo to defect report
POST /api/defect-reports/{defectReportId}/photos
Content-Type: multipart/form-data
file: <image>

# Add part to defect report
POST /api/defect-reports/{defectReportId}/parts
Body: {
  "description": "Hydraulic seal kit",
  "sapNumber": "12345678",
  "failedQuantity": 2,
  "requiredQuantity": 2
}

# Submit defect report
POST /api/defect-reports/{defectReportId}/submit

# Resolve defect report (admin)
POST /api/defect-reports/{defectReportId}/resolve
Body: {
  "resolutionNotes": "Replaced seals, pump operational"
}

# Generate EFN PDF
GET /api/defect-reports/{defectReportId}/pdf
Response: application/pdf
```

### 8.11 Offline Support for Defect Reports
Defect reports MUST work fully offline:
- Create new reports offline
- Capture photos offline (stored locally)
- Save drafts locally
- Queue submissions for sync
- View previously synced reports

```typescript
// Local SQLite table
CREATE TABLE defect_reports (
  defect_report_id TEXT PRIMARY KEY,
  report_number TEXT NOT NULL,
  equipment_id TEXT,
  location_id TEXT,
  fault_category TEXT NOT NULL,
  detailed_symptoms TEXT,
  replacement_urgency TEXT,
  status TEXT DEFAULT 'Draft',
  -- ... all other fields
  sync_status TEXT DEFAULT 'PendingUpload',
  local_updated_at TEXT,
  server_updated_at TEXT
);

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

CREATE TABLE defect_report_photos (
  photo_id TEXT PRIMARY KEY,
  defect_report_id TEXT NOT NULL,
  local_path TEXT NOT NULL,
  server_url TEXT,
  upload_status TEXT DEFAULT 'Pending',
  FOREIGN KEY (defect_report_id) REFERENCES defect_reports(defect_report_id)
);
```

---

## ✅ 9. IMPLEMENTATION CHECKLIST

### Phase 1: Core (Required)
- [ ] Update QR code parser for new format (backward compatible)
- [ ] Add UniqueId field to equipment model
- [ ] Implement smart input parsing (UniqueId, Asset #, Serial #)
- [ ] Update equipment detail screen to show UniqueId

### Phase 2: Verification (Required)
- [ ] Show pending inbound manifests for user's location
- [ ] Implement manual manifest number entry
- [ ] Add "Search" button for expected items
- [ ] Item scanning during verification
- [ ] Mark damaged/missing functionality
- [ ] Add extra item scanning
- [ ] Add unregistered items
- [ ] Save progress
- [ ] Complete verification

### Phase 3: Location Management
- [ ] Get user's assigned locations
- [ ] Filter manifests by location
- [ ] Location selection if user has multiple

### Phase 4: Printing (Required)
- [ ] Integrate Bluetooth printer library
- [ ] Implement label generation
- [ ] Add print button to equipment detail
- [ ] Printer settings screen

### Phase 5: Defect Reports (EFN) ⭐ NEW
- [ ] Create defect report list screen
- [ ] Implement 4-step report creation wizard
- [ ] Equipment origin selection
- [ ] Fault category dropdown (17 options)
- [ ] Photo capture and attachment
- [ ] Parts tracking (add/edit/delete)
- [ ] Urgency level selection
- [ ] Save as draft functionality
- [ ] Submit report
- [ ] Offline support for defect reports
- [ ] View report details
- [ ] Generate/share PDF (if online)

---

## 📞 QUESTIONS?

If you have any questions about these integrations, please reach out to the desktop team.

---

**Document Version**: 4.0  
**Last Updated**: January 12, 2026  
**Desktop Module Version**: Equipment & Inventory Module v1.4.0

### What's New in v4.0:
- ⭐ Added complete Defect Reports (EFN) section with:
  - Full data model and enums
  - 4-step creation wizard UI mockups
  - Defect reports list screen
  - API endpoints
  - Offline support requirements
  - Parts tracking
- Updated implementation checklist with Phase 5
