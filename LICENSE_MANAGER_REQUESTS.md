# Requests for License Manager Chat

## Overview

This document tracks security changes between the Fathom OS shell and the LicensingSystem library.

---

## ✅ COMPLETED BY LICENSE MANAGER CHAT

### 1. Fix DPAPI Scope (LicenseStorage.cs) ✅ DONE
Changed from `LocalMachine` to `CurrentUser` - only the logged-in user can decrypt.

### 2. Add License Type Field (LicenseModels.cs) ✅ DONE
Added `LicenseType` enum with `Online` and `Offline` values.

### 4. Add First Activation Tracking (LicenseStorage.cs) ✅ DONE
- Added `RecordFirstActivation()` method
- Added `IsLicenseSuspicious()` to detect reset attacks
- Added `FirstActivatedAt` and `OriginalLicenseId` to metadata

### 5. Enhanced Heartbeat Response ✅ DONE
Added `RevokedAt`, `RevokeReason`, `LicenseStatus`, `ServerTime` fields to response DTOs.

---

## 🔴 REMAINING SERVER-SIDE TASKS

These require changes on the **license server**:

### 3. Server: Track Offline License Activations

**New Endpoint:** `POST /api/license/report-offline-activation`

**Request:**
```json
{
    "licenseId": "string",
    "hardwareFingerprints": ["array of strings"],
    "activatedAt": "datetime"
}
```

**Server Logic:**
1. Check if this licenseId has been activated before
2. If YES and hardware doesn't match → Return error: "License already used on another device"
3. If YES and hardware matches → Allow (reinstall scenario)
4. If NO → Record activation with hardware fingerprint

### 6. Server: Validate Endpoint Should Check Revocation

The `/api/license/validate` endpoint should:
1. Check if license exists in database
2. Check if license is marked as revoked
3. Check if hardware fingerprint matches (or is within threshold)
4. Return revocation details in response

### 7. (Future) Offline License One-Time Use

For offline license files that are meant to be used once:
1. License file should have a field: `"singleUse": true`
2. When activated, client reports to server (when online)
3. Server marks license as "used" with hardware fingerprint
4. If same license file is imported again on different hardware → Server returns error

---

## Priority Summary

| Priority | Change | Status |
|----------|--------|--------|
| HIGH | #1 DPAPI Scope | ✅ DONE |
| HIGH | #6 Validate Revocation | 🔴 Server-side |
| MEDIUM | #2 License Type | ✅ DONE |
| MEDIUM | #3 Offline Tracking | 🔴 Server-side |
| MEDIUM | #4 First Activation | ✅ DONE |
| LOW | #5 Heartbeat | ✅ DONE |
| LOW | #7 One-Time Use | 🔵 Future |

---

**Last Updated:** December 2024
