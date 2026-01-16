# Fathom OS Equipment Manager - Mobile App Handoff
## For Mobile Developer (React Native / Flutter)

---

## ðŸŽ¯ What You're Building

A cross-platform mobile application for field operations that works with:
- **Central API Server** (ASP.NET Core) - Being built in parallel
- **Desktop Module** (WPF .NET 8) - Already complete âœ…

The mobile app enables deck crews and store keepers to:
- Scan QR codes to look up equipment
- Create and manage transfer manifests
- Register new equipment with photos
- Work completely offline when at sea

---

## ðŸ“¦ What's Included in This Package

1. **MOBILE_APP_REQUIREMENTS.md** (2,161 lines) - Your main specification:
   - Complete screen specifications with wireframes
   - All API endpoints with request/response examples
   - Local SQLite database schema
   - Offline sync requirements
   - QR code scanning specifications
   - Photo capture requirements
   - Digital signature requirements
   - Development checklist (10 weeks)

2. **SYSTEM_ARCHITECTURE.md** - System overview (if you need context)

3. **Desktop Module Source** - Reference for data models

---

## ðŸš€ Technology Recommendation

### React Native (Recommended)

```json
{
  "dependencies": {
    "react-native": "0.73+",
    "@reduxjs/toolkit": "^2.0",
    "@react-navigation/native": "^6.0",
    "react-native-camera-kit": "^13.0",
    "axios": "^1.6",
    "@nozbe/watermelondb": "^0.27",
    "react-native-signature-canvas": "^4.7",
    "react-native-paper": "^5.0",
    "@react-native-firebase/messaging": "^18.0"
  }
}
```

### Flutter Alternative

```yaml
dependencies:
  flutter: sdk
  riverpod: ^2.4
  dio: ^5.4
  drift: ^2.14
  mobile_scanner: ^4.0
  signature: ^5.4
  firebase_messaging: ^14.7
```

---

## ðŸ“± Screens to Build (Priority Order)

### Phase 1: Foundation
1. **Login Screen** - Username/password + remember me
2. **PIN Entry Screen** - Quick re-login with 4-6 digit PIN
3. **Home Dashboard** - Quick actions + recent activity

### Phase 2: Core Features
4. **QR Scanner** - Camera-based QR code scanning
5. **Equipment Details** - View scanned equipment info
6. **Equipment List** - Search and filter equipment
7. **New Equipment** - Registration form with photo capture

### Phase 3: Manifests
8. **Create Outward Manifest** - Multi-step wizard
9. **Add Items** - Scan or search to add equipment
10. **Manifest Review** - Summary with signature capture
11. **Receive Manifest** - Process incoming items

### Phase 4: Offline & Settings
12. **Sync Status** - Show pending changes
13. **Conflict Resolution** - Handle sync conflicts
14. **Settings** - API URL, theme, notifications

---

## ðŸ”Œ API Notes

**During Development:**
- API may not be ready immediately
- Use mock data for initial development
- All endpoints are documented in MOBILE_APP_REQUIREMENTS.md Section 3

**Base URLs:**
```
Development: https://s7-equipment-api.up.railway.app/api
Production: https://your-server.company.com/api
```

**Authentication:**
```http
POST /api/auth/login
{
  "username": "john.smith",
  "password": "password123"
}

Response:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "dGhpcy...",
  "expiresIn": 900,
  "user": { ... }
}
```

---

## ðŸ“´ Offline Requirements

**Must Work Offline:**
- View all cached equipment
- Create new manifests
- Add items to manifests
- Capture photos
- Record signatures
- View pending sync queue

**Requires Connection:**
- Initial login
- First data download
- Uploading photos to server
- Final manifest sync

---

## ðŸŽ¯ Key Success Metrics

1. **QR Scan Speed**: < 1 second to decode
2. **Equipment Lookup**: < 200ms from local cache
3. **Offline Storage**: Support 50,000 equipment records
4. **Sync Performance**: < 30 seconds for delta sync
5. **Battery**: < 5% per hour in active use
6. **App Size**: < 50MB installed

---

## âœ… Development Checklist

From MOBILE_APP_REQUIREMENTS.md Section 15:

- [ ] Week 1-2: Project setup, navigation, authentication
- [ ] Week 3-4: QR scanner, equipment screens, photo capture
- [ ] Week 5-6: Manifest creation, item management, signatures
- [ ] Week 7-8: Offline database, sync engine, conflict handling
- [ ] Week 9-10: Push notifications, polish, testing

---

## ðŸ“ž Reference Documents

| Document | Purpose |
|----------|---------|
| **MOBILE_APP_REQUIREMENTS.md** | Main specification - READ THIS FIRST |
| **SYSTEM_ARCHITECTURE.md** | System overview, database schema |
| **Desktop Models/*.cs** | Data model reference |

---

**Document Version:** 1.0  
**Created:** January 2025  
**For:** Mobile Development Team
