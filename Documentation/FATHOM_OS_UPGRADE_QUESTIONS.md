# FathomOS Upgrade - Decision Questions

**Document Version:** 2.0 (Comprehensive)
**Date:** January 18, 2026
**Instructions:** Please review each question and provide your answer by marking your choice with [X] or writing your preference.

---

## Section 1: User Interface & Experience

### Question 1: Window Title Format

**Context:** The main FathomOS window needs to display license information. Currently shows only "Fathom OS".

**Options:**

- [ yes] **Option A (Recommended):** `"FathomOS - {ClientName} - {Edition}"`
  - Example: `"FathomOS - Oceanic Surveys Ltd - Professional"`
  - Best for: Client recognition and branding

- [ ] **Option B:** `"FathomOS {Edition} - {ClientName}"`
  - Example: `"FathomOS Professional - Oceanic Surveys Ltd"`
  - Best for: Emphasizing the edition level

- [ ] **Option C:** `"{ClientName} - FathomOS {Edition}"`
  - Example: `"Oceanic Surveys Ltd - FathomOS Professional"`
  - Best for: White-label emphasis

- [ ] **Option D:** Custom format (please specify)
  - Your format: _______________________________________________

**Your Answer:** Option A
---Option A

### Question 2: Server Connection Indicator Location

**Context:** Users need to see if FathomOS is connected to the central server for sync.

**Options:**

- [ ] **Option A (Recommended):** Status bar at bottom of dashboard
  - Shows: `[●] Connected | Last sync: 2 min ago | 3 pending`
  - Pros: Always visible, non-intrusive

- [ ] **Option B:** Header/title bar area
  - Small icon next to window title
  - Pros: Compact, visible in taskbar

- [ ] **Option C:** Floating notification badge
  - Corner badge that expands on hover
  - Pros: Modern look

- [ ] **Option D:** Sidebar panel (always visible)
  - Dedicated sync panel with full details
  - Pros: Comprehensive

**Your Answer:**Option A

---Option A

### Question 3: Module Loading Behavior

**Context:** When a user clicks a module, loading can take 1-10 seconds.

**Options:**

- [ ] **Option A (Recommended):** Full-screen loading overlay
  - Dimmed background + centered spinner + "Loading {ModuleName}..."
  - User cannot interact until loaded
  - Pros: Clear feedback, prevents double-clicks

- [ ] **Option B:** Module tile loading indicator
  - Spinner replaces module icon, rest of dashboard usable
  - Pros: Non-blocking
  - Cons: User might click another module

- [ ] **Option C:** Progress bar in status bar
  - Subtle progress indication at bottom
  - Pros: Minimal intrusion
  - Cons: Easy to miss

- [ ] **Option D:** Modal dialog with cancel option
  - Popup with spinner and "Cancel" button
  - Pros: Gives user control

**Your Answer:**Option A

---

### Question 4: Module Loading Timeout

**Context:** If a module fails to load, how long should the system wait?

**Options:**

- [ ] **Option A:** 15 seconds (fast timeout, may fail on slow systems)
- [ ] **Option B (Recommended):** 30 seconds (balanced)
- [ ] **Option C:** 60 seconds (patient, good for network drives)
- [ ] **Option D:** No timeout (user must manually cancel)

**Your Answer:** Option B

---

### Question 5: Theme Selection

**Context:** The SHELL-AGENT identified that users should have theme options.

**Options:**

- [ ] **Option A (Recommended):** Light, Dark, Modern Dark (gradient accents)
- [ ] **Option B:** Light, Dark, System (follow Windows setting)
- [ ] **Option C:** Light, Dark, Modern Dark, High Contrast
- [ ] **Option D:** Custom theme editor with color picker

**Your Answer:** Option A and the responsible agent need to review this themes creation and make sure that the coloring for each theme is cobitable to work together and maybe make the search for the best practice and choose the top tire options.

---

### Question 6: Splash Screen

**Context:** The SHELL-AGENT recommends a splash screen during startup.

**Options:**

- [ ] **Option A (Recommended):** Branded splash with progress bar and status text
  - Shows: Logo, version, loading status, progress %

- [ ] **Option B:** Simple logo only (minimal)
  - Shows: Logo for 2-3 seconds

- [ ] **Option C:** No splash screen (direct to dashboard)
  - May show blank window during load

**Your Answer:**Option A

---

## Section 2: Data & Synchronization

### Question 7: First-Time Setup Flow After License Activation

**Context:** When a user first activates FathomOS with a license, what should happen?

**Options:**

- [ ] **Option A (Recommended):** Guided Setup Wizard
  1. Welcome screen with license info
  2. Create admin account (username/password)
  3. Configure server connection (optional)
  4. Choose theme preference
  5. Show dashboard
  - Pros: Professional, guides new users

- [ ] **Option B:** Minimal Setup
  1. Create admin account only
  2. Go directly to dashboard
  - Pros: Fast

- [ ] **Option C:** Auto-login with License Info
  1. Create admin account using license email
  2. Generate temporary password (shown once)
  3. Show dashboard
  - Pros: Fastest

**Your Answer:** Option A and it needs to be clear that the user choose between online or offline version and each version have its work flow and based on his choice the setup eather local database or server connection and that will require the serversetup , let me know what do youthink about that.

---

### Question 8: Data Sync Strategy

**Context:** FathomOS needs to sync data between local SQLite and central server. The DATABASE-AGENT identified this as critical for multi-installation scenarios.

**Options:**

- [ ] **Option A (Recommended):** Server Wins (with local backup)
  - If conflict: Server version overwrites local
  - Local version saved to conflict archive
  - User can review and restore if needed
  - Best for: Centralized control

- [ ] **Option B:** Local Wins (push to server)
  - If conflict: Local version pushed to server
  - Server version archived
  - Best for: Distributed teams with authority

- [ ] **Option C:** Manual Resolution
  - If conflict: Show comparison dialog
  - User chooses which version to keep
  - Best for: Critical data accuracy
  - Cons: Requires user attention

- [ ] **Option D:** Last-Write-Wins (timestamp based)
  - Most recent change wins automatically
  - No user intervention
  - Risk: Can lose intentional changes

**Your Answer:** Option C and maybe that require a system log or changes log and the user with ahigher permeation can take the decision , please also ask the responsible agent to create a system log if there is no and a user permitions management system so different user role can have a specific permetions that is managed by the superadmin user , let me know what do youthink about that 

---

### Question 9: Offline Mode Behavior

**Context:** When server is unreachable, how should FathomOS behave?

**Options:**

- [ ] **Option A (Recommended):** Full Offline Mode
  - All features work normally
  - Data saved locally
  - Sync when connection restored
  - Yellow indicator shows offline status

- [ ] **Option B:** Limited Offline Mode
  - Read-only access to existing data
  - New work disabled until online
  - Pros: Prevents sync complexity

- [ ] **Option C:** Prompt User Each Time
  - When offline detected, ask: "Continue offline or wait?"
  - User decides per session

**Your Answer:** Option A

---

### Question 10: Sync Frequency

**Context:** How often should FathomOS sync with the server when online?

**Options:**

- [ ] **Option A:** Manual only (user clicks "Sync" button)
- [ ] **Option B:** Every 5 minutes (real-time feel)
- [ ] **Option C (Recommended):** Every 15 minutes (balanced)
- [ ] **Option D:** Every hour (minimal traffic)
- [ ] **Option E:** On module close (sync when leaving a module)

**Your Answer:** Option B and add option for the user to configure it in the settings.

---

## Section 3: Security & Authentication

### Question 11: Password Policy

**Context:** The SECURITY-AGENT identified weak password requirements (6 char minimum).

**Options:**

- [ ] **Option A:** Keep current (6 characters minimum)
- [ ] **Option B (Recommended):** 12 characters, 1 uppercase, 1 number, 1 special
- [ ] **Option C:** 8 characters, 1 uppercase, 1 number
- [ ] **Option D:** Passphrase (4+ words)

**Your Answer:** Option B

---

### Question 12: Multi-Factor Authentication (MFA)

**Context:** The SECURITY-AGENT identified no MFA as a critical gap.

**Options:**

- [ ] **Option A:** Not needed (single-user workstations)
- [ ] **Option B (Recommended):** Optional MFA (user can enable TOTP)
- [ ] **Option C:** Required MFA for admin accounts only
- [ ] **Option D:** Required MFA for all users

**Your Answer:** Option B

---

### Question 13: Database Encryption

**Context:** The SECURITY-AGENT recommends SQLCipher for encrypted databases.

**Options:**

- [ ] **Option A (Recommended):** Encrypt all local databases with SQLCipher
  - Pros: Data at rest protection
  - Cons: ~5-10% performance overhead

- [ ] **Option B:** Encrypt sensitive data only (user credentials, license)
  - Partial protection
  - Lower overhead

- [ ] **Option C:** No encryption (current state)
  - Fastest performance
  - Data accessible if device stolen

**Your Answer:** Option A

---

### Question 14: Session Management

**Context:** Should FathomOS enforce single concurrent session per license?

**Options:**

- [ ] **Option A (Recommended):** Single session with takeover option
  - New login shows: "Session active on Device X. Take over?"
  - Previous session logged out

- [ ] **Option B:** Multiple concurrent sessions allowed
  - Same user can be logged in on multiple devices

- [ ] **Option C:** Single session, no takeover (locked until timeout)
  - Must wait for existing session to expire

**Your Answer:** Option A that may require having a log for the user wanted to take over let me know what do you think about that

---

## Section 4: Server & Infrastructure

### Question 15: Server Deployment Platform

**Context:** The SERVER-AGENT analyzed deployment options.

**Options:**

- [ ] **Option A:** Continue with Render.com
  - Current: https://fathom-os-license-server.onrender.com
  - Pros: Simple, already set up
  - Cons: Limited scaling, no Kubernetes
  - Cost: ~$25/month

- [ ] **Option B (Recommended for Enterprise):** Azure
  - App Service, Azure SQL, Redis Cache
  - Pros: Full enterprise features, compliance
  - Cons: Higher cost, complexity
  - Cost: ~$200-500/month

- [ ] **Option C:** AWS
  - ECS/EKS, RDS, ElastiCache
  - Pros: Wide service selection
  - Cost: ~$150-400/month

- [ ] **Option D:** Self-hosted (on-premise)
  - Customer provides infrastructure
  - Best for: High-security environments

**Your Answer:** Option D , please provide stand alone guide explaining step by step how to do it 

---

### Question 16: API Versioning Strategy

**Context:** The SERVER-AGENT recommends API versioning for backward compatibility.

**Options:**

- [ ] **Option A (Recommended):** URL path versioning (`/api/v1/...`, `/api/v2/...`)
- [ ] **Option B:** Header versioning (`Api-Version: 1`)
- [ ] **Option C:** Query parameter (`?api-version=1`)

**Your Answer:** Option A

---

## Section 5: Licensing System

### Question 17: License Manager Key Generation

**Context:** First-time License Manager users must generate encryption keys. The LICENSING-AGENT found this process confusing.

**Options:**

- [ ] **Option A (Recommended):** Auto-generate on first launch with mandatory backup download
  - Keys generated automatically
  - User prompted to download backup file
  - Warning shown about key importance

- [ ] **Option B:** Guided wizard requiring explicit key generation
  - First launch shows wizard
  - User must click "Generate Keys" button
  - Explains what keys are for

- [ ] **Option C:** Import existing keys OR generate new
  - First launch offers choice
  - Supports migration from other systems

**Your Answer:** Option A but if this will require a server env update guide the user through the requirments

---

### Question 18: Certificate PDF Branding

**Context:** When generating license certificate PDFs, what branding should appear?

**Options:**

- [ ] **Option A (Recommended):** FathomOS branding + Client edition name
  - Header: "FathomOS License Certificate"
  - Footer: Edition name (e.g., "Professional Edition")
  - Client logo if provided in license

- [ ] **Option B:** Fully white-labeled (client branding only)
  - No FathomOS name visible
  - Client name/logo only

- [ ] **Option C:** Minimal branding
  - Just license information
  - No logos or brand names

**Your Answer:** Option A , and make sure that the license key is mentioned in this certificates and also add a HTML version with high end quality and modern premium because i am not satisfied with the pdf version so both options generated and the html version is the version goes to the server .

---

### Question 19: License Renewal Handling

**Context:** How should FathomOS handle expiring licenses?

**Options:**

- [ ] **Option A (Recommended):** Progressive warnings + grace period
  - 30 days before: Info banner
  - 7 days before: Warning dialog on each launch
  - Expired: 14-day grace period with persistent warning
  - Post-grace: Read-only mode

- [ ] **Option B:** Single warning + grace period
  - 7 days before: Warning dialog
  - Expired: 7-day grace period
  - Post-grace: Complete lockout

- [ ] **Option C:** No warnings, immediate lockout
  - On expiry date: Application stops working
  - Must renew before next use

**Your Answer:** Option A 

---

## Section 6: Settings & Configuration

### Question 20: Settings Dialog Scope

**Context:** What settings should be configurable from the Settings dialog?

**Options (select all that apply):**

- [ ] **Theme selection** (Light/Dark/Modern/Custom)
- [ ] **Server URL configuration**
- [ ] **Auto-sync interval** (manual/5min/15min/1hour)
- [ ] **Database backup location**
- [ ] **Module visibility** (show/hide specific modules)
- [ ] **Language selection** (if multi-language planned)
- [ ] **Certificate storage path**
- [ ] **Export default formats** (Excel/PDF/DXF defaults)
- [ ] **Notification preferences** (sync alerts, warnings)
- [ ] **User profile editing** (name, password change)
- [ ] **License information display** (read-only)
- [ ] **Debug/logging options** (for troubleshooting)

**Your Selections:** User profile editing need to be more professional with more information and thats need to be integrated with the personal management module as well and the theme selection can be better placed in the main window top title bare beside the main window controls , also i am sure there should be more settings to implement and add for this kind of professional softwares , let me know what do you think about that 

---

### Question 21: High-DPI Scaling Approach

**Context:** The application needs to work on various screen resolutions (1080p to 4K).

**Options:**

- [ ] **Option A (Recommended):** Per-Monitor DPI Awareness
  - Scales correctly when moved between monitors
  - Best quality rendering
  - Requires: Windows 10/11

- [ ] **Option B:** System DPI Awareness
  - Uses system-wide DPI setting
  - May blur when moving between monitors
  - Simpler implementation

- [ ] **Option C:** Manual Scaling Option
  - User selects UI scale (100%, 125%, 150%, 200%)
  - Independent of Windows settings

**Your Answer:** Option A

---

## Section 7: Modules & Features

### Question 22: Module Health Monitoring

**Context:** The ARCHITECTURE-AGENT recommends module health monitoring for enterprise users.

**Options:**

- [ ] **Option A (Recommended):** Dashboard health status for each module
  - Shows: Healthy (green), Degraded (yellow), Unhealthy (red)
  - Details on hover: DB status, last error, memory usage

- [ ] **Option B:** Simple module status in Settings
  - List of modules with basic OK/Error status
  - Less visible but less intrusive

- [ ] **Option C:** No health monitoring
  - Errors shown only when they occur

**Your Answer:** Option A

---

### Question 23: 3D Visualization Memory Management

**Context:** The MODULES-AGENT identified memory leaks in 3D visualization modules (TreeInclination, RovGyro, VesselGyro).

**Options:**

- [ ] **Option A (Recommended):** Aggressive cleanup with memory limits
  - Auto-dispose 3D scenes when module closes
  - Memory limit: 500MB per module
  - Warning when approaching limit

- [ ] **Option B:** Manual cleanup with user control
  - User can click "Clear Memory" button
  - No automatic limits

- [ ] **Option C:** Keep current behavior
  - May cause memory issues on long sessions

**Your Answer:** Option A but allow more than 500MB and you may add option for the user to configure the number based on his system 

---

## Section 8: Architecture Decisions

### Question 24: Error Handling Pattern

**Context:** The ARCHITECTURE-AGENT recommends Result<T> pattern for better error handling.

**Options:**

- [ ] **Option A (Recommended):** Result<T> pattern with error categories
  - All services return Result<T> with success/failure
  - Error categories: Validation, Business, Database, Network, Unknown
  - Consistent error handling across app

- [ ] **Option B:** Traditional exception-based
  - Keep current try/catch patterns
  - Custom exception types for different errors

- [ ] **Option C:** Hybrid approach
  - Validation errors via Result<T>
  - System errors via exceptions

**Your Answer:** Option A

---

### Question 25: Code Duplication Resolution

**Context:** ~4000 lines of code are duplicated across modules. The CORE-AGENT identified services that should be consolidated.

**Which services should be moved to FathomOS.Core?**

- [ ] **SmoothingService** (currently in 4 modules)
- [ ] **ExcelExportService** (currently in 6 modules)
- [ ] **UnitConversionService** (currently in 3 modules)
- [ ] **Visualization3DService** (currently in 3 modules)
- [ ] **All of the above (Recommended)**
- [ ] **None - keep module independence**

**Your Answer:** All of the above however i think they are not all the same and that will require a plan for each module to fix his code to work with the new move and if yes they are not the same then make sure that the best service is the one that is keept and keep a backup till i confirm after testing if all modules are using the core centerlized services or there are anything need to be fixed .

---

### Question 26: Testing Strategy

**Context:** Current test coverage is <5%. The ARCHITECTURE-AGENT recommends comprehensive testing.

**What test coverage target should we aim for?**

- [ ] **Option A:** 80% unit test coverage (industry standard)
- [ ] **Option B (Recommended):** 60% unit + key integration tests
- [ ] **Option C:** 40% coverage (critical paths only)
- [ ] **Option D:** Manual testing only (current state)

**Your Answer:** Option A

---

## Section 9: Implementation Priorities

### Question 27: Implementation Priority Ranking

**Context:** Given limited time, which areas should be prioritized first?

**Please rank from 1 (highest priority) to 8 (lowest priority):**

| Area | Your Rank (1-8) |
|------|-----------------|
| Fix crashes and freezes (Critical bugs) | 6 |
| Window title and branding | 7 |
| Server sync functionality | 5 |
| Settings dialog | 6 |
| High-DPI support | 8|
| License Manager improvements | 8 |
| Security hardening | 7 |
| Module code consolidation | 6 |

---

### Question 28: Timeline Preference

**Context:** The comprehensive plan estimates 24 weeks for full transformation.

**Options:**

- [ ] **Option A:** Full transformation (24 weeks)
  - All features, highest quality
  - Enterprise-ready at completion

- [ ] **Option B (Recommended):** Phased approach (12 weeks MVP + ongoing)
  - Phase 1 (4 weeks): Critical bugs, stability
  - Phase 2 (4 weeks): Core UX improvements
  - Phase 3 (4 weeks): Sync & settings
  - Future: Security, enterprise features

- [ ] **Option C:** Minimal fixes only (4 weeks)
  - Fix crashes and freezes only
  - No new features

**Your Answer:** Option B

---

## Section 10: Additional Input

### Question 29: Server URL Confirmation

**Context:** There are two different server URLs in the codebase:
- `https://s7fathom-license-server.onrender.com` (in SettingsService.cs)
- `https://fathom-os-license-server.onrender.com` (mentioned in requirements)

**Which is the correct server URL?**

**Your Answer:** https://s7fathom-license-server.onrender.com

---

### Question 30: Additional Features or Requirements

Please list any additional features, requirements, or concerns not covered above:

```
_i am expecting the R&D agent to prepare additional features however i am expecting an extra improvments based on the current documentations and also a UI standarizations and also if there are any missing views or windows or extra implemntations that will make the software more professional and modern and up to the high standards  ______________________________________________________________________________

_______________________________________________________________________________

_______________________________________________________________________________

_______________________________________________________________________________

_______________________________________________________________________________

_______________________________________________________________________________
```

---

## How to Submit Your Answers

1. Mark your preferred options with [X] or write your choice
2. For multi-select questions, mark all that apply
3. For ranking questions, provide numbers 1-8
4. Add any comments in the Additional Input section
5. Save this file and inform me when ready to proceed

---

**Thank you for your input. Your decisions will directly shape the implementation.**

**Document prepared by:** 8 Specialized Agent Analysis Teams
**Review deadline:** [User to specify]
