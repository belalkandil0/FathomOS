# FathomOS v1.0.49 - Issues, Fixes & Feature Implementation Plan

**Created:** January 18, 2026
**Priority:** Critical
**Target Version:** 1.0.49

---

## Executive Summary

This document outlines all identified issues, required fixes, and new features to be implemented across the FathomOS ecosystem. All components require modernization to meet 2026 premium software standards.

---

## Issues by Component

### 1. License Manager Desktop Application

| ID | Issue | Severity | Description |
|----|-------|----------|-------------|
| LM-001 | Dropdown Visibility | Critical | UI dropdown initial background is white with white text - content not visible |
| LM-002 | UI Modernization | High | Overall UI needs upgrade to modern, high-end, premium styling |
| LM-003 | Certificate HTML Export | Medium | Only PDF download available - need HTML download option |
| LM-004 | License Code Missing | Medium | Certificate displays License ID but not the License Code |
| LM-005 | Installer Enhancement | Medium | Installer should prompt for license and server/database configuration |

### 2. FathomOS Main Application - Authentication

| ID | Issue | Severity | Description |
|----|-------|----------|-------------|
| FA-001 | Online Activation Error | Critical | Error occurs when entering license code for online activation |
| FA-002 | Account Creation Missing | Critical | After offline license activation, administrator account creation window does not appear |
| FA-003 | Default Credentials | Critical | No working default username/password for first-time login |
| FA-004 | Startup Flow Logic | Critical | Authentication flow not following correct sequence: License → Account Creation → Login |

### 3. FathomOS Main Application - User Interface

| ID | Issue | Severity | Description |
|----|-------|----------|-------------|
| FU-001 | Login Window Responsiveness | Critical | Login window not responsive - lower portion with login button not visible |
| FU-002 | Modern UI Styling | High | All windows, popups, and dialogs need modern, high-end 2026 styling |
| FU-003 | Consistent Theme | High | Ensure consistent premium theme across all application components |
| FU-004 | Window Sizing | Medium | All windows should have appropriate minimum sizes and be responsive |

### 4. Module System

| ID | Issue | Severity | Description |
|----|-------|----------|-------------|
| MS-001 | Module Codes Length | Medium | Module codes should be 3 letters instead of 2 letters |
| MS-002 | Certificate Format Update | Medium | Processing certificates need to reflect 3-letter module codes |
| MS-003 | Related Files Update | Low | All files referencing module codes need updating |

---

## Required Fixes

### Phase 1: Critical Authentication Fixes (Priority: Immediate)

#### Fix FA-001: Online License Activation
- Debug and fix the online license activation endpoint communication
- Verify server URL is correct: `https://s7fathom-license-server.onrender.com`
- Add proper error handling and user-friendly error messages
- Test with valid license codes

#### Fix FA-002 & FA-003: Account Creation Flow
- Ensure `CreateAccountWindow` opens after successful license activation
- Implement default admin account creation if no users exist
- Default credentials: `administrator` / `FathomOS2026!`
- Force password change on first login
- Fix `AppStartupService.cs` startup state logic

#### Fix FA-004: Startup Flow Correction
```
Correct Flow:
1. Check License → If missing → ActivationWindow
2. License Valid → Check Users → If none → CreateAccountWindow
3. Users Exist → LocalLoginWindow
4. Login Success → DashboardWindow
```

### Phase 2: UI Modernization (Priority: High)

#### Fix FU-001: Login Window Responsiveness
- Set appropriate MinHeight/MinWidth
- Use ScrollViewer for content overflow
- Ensure login button always visible
- Test at various screen resolutions

#### Fix FU-002 & FU-003: Modern Premium UI
Apply to ALL windows:
- Modern dark theme with accent colors
- Fluent Design principles (acrylic, reveal effects)
- Consistent spacing and typography
- Premium animations and transitions
- High-contrast readable text
- Modern button styles with hover effects
- Card-based layouts where appropriate
- Proper visual hierarchy

**Windows to Update:**
1. `LocalLoginWindow.xaml`
2. `LoginWindow.xaml`
3. `CreateAccountWindow.xaml`
4. `ActivationWindow.xaml`
5. `DashboardWindow.xaml`
6. `SettingsWindow.xaml`
7. `CertificateListWindow.xaml`
8. `CertificateViewerWindow.xaml`
9. All module windows

### Phase 3: License Manager Fixes (Priority: High)

#### Fix LM-001: Dropdown Visibility
- Update ComboBox styles with proper foreground/background colors
- Ensure dropdown items have dark background with light text
- Test all dropdowns in the application

#### Fix LM-002: UI Modernization
- Apply premium dark theme throughout
- Modern form controls styling
- Improved visual feedback
- Professional animations

#### Fix LM-003: HTML Certificate Export
- Add "Export as HTML" button alongside PDF
- Create HTML template matching PDF design
- Include all certificate data in HTML format

#### Fix LM-004: License Code in Certificate
- Add License Code field to certificate template
- Display both License ID and License Code
- Update PDF and HTML generators

#### Fix LM-005: Installer Enhancement
- Add configuration wizard to installer
- Prompt for: License Key, Server URL, Database Settings
- Save configuration to settings file
- Optional: Pre-activate license during install

### Phase 4: Module Code Updates (Priority: Medium)

#### Fix MS-001, MS-002, MS-003: 3-Letter Module Codes

| Current | New | Module |
|---------|-----|--------|
| SL | SLG | Survey Listing |
| LB | SLB | Survey Logbook |
| NT | NTS | Network Time Sync |
| EI | EQI | Equipment Inventory |
| SV | SVP | Sound Velocity |
| GC | GNS | GNSS Calibration |
| MC | MRU | MRU Calibration |
| UV | USB | USBL Verification |
| TI | TRI | Tree Inclination |
| RG | RGC | ROV Gyro Calibration |
| VG | VGC | Vessel Gyro Calibration |
| PM | PRM | Personnel Management |
| PJ | PJM | Project Management |

**Files to Update:**
- All `ModuleInfo.json` files
- `CertificationService.cs`
- `CertificateHelper.cs`
- Certificate templates
- Module registration code

---

## New Features to Implement

### Feature 1: Enhanced Certificate System
- HTML certificate download option
- License Code display on certificates
- QR code verification link
- Modern certificate design

### Feature 2: Improved Installer
- License configuration wizard
- Server connection setup
- Database configuration (optional)
- First-run setup assistant

### Feature 3: Premium UI Theme System
- Dark/Light/Modern theme options
- Acrylic and blur effects
- Smooth animations
- Responsive layouts
- Accessibility compliance

---

## Implementation Delegation

### SHELL-AGENT
- Fix authentication startup flow (FA-001 to FA-004)
- Fix login window responsiveness (FU-001)
- Modernize all Shell UI windows (FU-002, FU-003)
- Implement proper account creation flow

### LICENSING-AGENT
- Fix License Manager dropdowns (LM-001)
- Modernize License Manager UI (LM-002)
- Add HTML certificate export (LM-003)
- Add License Code to certificates (LM-004)
- Enhance installer with configuration (LM-005)

### CORE-AGENT
- Update module codes to 3 letters (MS-001)
- Update certificate format (MS-002)

### UI-AGENT
- Create modern premium theme resources
- Design consistent styling for all components
- Create reusable UI components

### BUILD-AGENT
- Rebuild all components after fixes
- Create new installer with configuration wizard
- Test deployment package

---

## Testing Requirements

### Authentication Testing
- [ ] Fresh install with no license
- [ ] Online license activation
- [ ] Offline license activation
- [ ] Account creation appears after activation
- [ ] Default credentials work
- [ ] Password change prompt on first login
- [ ] Subsequent logins work correctly

### UI Testing
- [ ] All windows display correctly at 1920x1080
- [ ] All windows display correctly at 1366x768
- [ ] All windows display correctly at 1280x720
- [ ] All dropdowns readable (dark background, light text)
- [ ] All buttons visible and clickable
- [ ] Theme consistency across all windows
- [ ] Responsive behavior verified

### Certificate Testing
- [ ] PDF certificate generates correctly
- [ ] HTML certificate generates correctly
- [ ] License Code appears on certificates
- [ ] 3-letter module codes display correctly
- [ ] QR code works

---

## Success Criteria

1. **Authentication:** User can activate license (online/offline) and create admin account seamlessly
2. **UI Quality:** All interfaces meet modern 2026 premium software standards
3. **Certificates:** Both PDF and HTML formats available with complete information
4. **Module Codes:** All modules use 3-letter codes consistently
5. **Installer:** Configuration wizard guides user through setup

---

## Timeline

| Phase | Duration | Components |
|-------|----------|------------|
| Phase 1 | Immediate | Critical authentication fixes |
| Phase 2 | High Priority | UI modernization |
| Phase 3 | High Priority | License Manager fixes |
| Phase 4 | Medium Priority | Module code updates |
| Testing | After each phase | Comprehensive testing |

---

*Document maintained by BUILD-AGENT and DOCUMENTATION-AGENT*
