# FathomOS Documentation Index

**Complete index of all project documentation**
**Version:** 1.0.47
**Last Updated:** January 18, 2026

---

## Quick Navigation

| Section | Description |
|---------|-------------|
| [Core Documentation](#core-documentation) | Architecture, getting started, developer guide |
| [API Reference](#api-reference) | Core, Shell, and Module APIs |
| [Module Documentation](#module-documentation) | All 13 module guides |
| [Services Documentation](#services-documentation) | Theme, certification, events, modules |
| [Licensing Documentation](#licensing-documentation) | License system, vendor guide |
| [Security Documentation](#security-documentation) | Security audits, compliance |
| [Deployment](#deployment-documentation) | Installation, configuration |
| [Integration Guides](#integration-guides) | Module integration, licensing integration |
| [Planning Documents](#planning-documents) | R&D plans, upgrade plans |

---

## Core Documentation

### Main Documentation (in `/Documentation/`)

| Document | Location | Description |
|----------|----------|-------------|
| [FathomOS Overview](FathomOS-Overview.md) | Documentation/ | Complete system documentation |
| [Architecture](Architecture.md) | Documentation/ | System architecture and design patterns |
| [Getting Started](GettingStarted.md) | Documentation/ | Quick start guide for new users |
| [Developer Guide](DeveloperGuide.md) | Documentation/ | Development setup, coding standards |
| [Changelog](CHANGELOG.md) | Documentation/ | Version history and changes |
| [API Reference](API_REFERENCE.md) | Documentation/ | Comprehensive API reference |
| [User Manual](USER_MANUAL.md) | Documentation/ | End-user documentation |

### Root-Level Documentation (in project root)

| Document | Location | Description |
|----------|----------|-------------|
| [Main README](../README.md) | Root | Project overview and quick start |
| [Certificate System](../CERTIFICATE_SYSTEM_README.md) | Root | Certificate generation system |
| [Licensing Integration Manual](../FATHOM_OS_LICENSING_INTEGRATION_MANUAL.md) | Root | How to integrate licensing |
| [Module Integration Guide](../FATHOM_OS_MODULE_INTEGRATION_GUIDE.md) | Root | Creating and integrating modules |
| [Security Features](../SECURITY_FEATURES.md) | Root | Security overview |

---

## API Reference

| Document | Location | Description |
|----------|----------|-------------|
| [Core API](API/Core-API.md) | Documentation/API/ | FathomOS.Core library reference |
| [Shell API](API/Shell-API.md) | Documentation/API/ | FathomOS.Shell application reference |
| [Module API](API/Module-API.md) | Documentation/API/ | IModule interface documentation |

---

## Module Documentation

### Central Module Docs (in `/Documentation/Modules/`)

| Module | Document | Category |
|--------|----------|----------|
| Survey Listing | [SurveyListing.md](Modules/SurveyListing.md) | Data Processing |
| Survey Logbook | [SurveyLogbook.md](Modules/SurveyLogbook.md) | Data Processing |
| Sound Velocity | [SoundVelocity.md](Modules/SoundVelocity.md) | Analysis |
| GNSS Calibration | [GnssCalibration.md](Modules/GnssCalibration.md) | Calibration |
| MRU Calibration | [MruCalibration.md](Modules/MruCalibration.md) | Calibration |
| USBL Verification | [UsblVerification.md](Modules/UsblVerification.md) | Calibration |
| Tree Inclination | [TreeInclination.md](Modules/TreeInclination.md) | Calibration |
| ROV Gyro Calibration | [RovGyroCalibration.md](Modules/RovGyroCalibration.md) | Calibration |
| Vessel Gyro Calibration | [VesselGyroCalibration.md](Modules/VesselGyroCalibration.md) | Calibration |
| Network Time Sync | [NetworkTimeSync.md](Modules/NetworkTimeSync.md) | Utilities |
| Equipment Inventory | [EquipmentInventory.md](Modules/EquipmentInventory.md) | Utilities |
| Personnel Management | [PersonnelManagement.md](Modules/PersonnelManagement.md) | Operations |
| Project Management | [ProjectManagement.md](Modules/ProjectManagement.md) | Operations |

### Module-Specific Extended Documentation

**Equipment Inventory** (most comprehensive module documentation):
- Location: `FathomOS.Modules.EquipmentInventory/Documentation/`
- Files: ADMINISTRATOR_GUIDE.md, API_DOCUMENTATION.md, DEPLOYMENT_GUIDE.md, SECURITY_BEST_PRACTICES.md, SYSTEM_ARCHITECTURE.md, MOBILE_APP_REQUIREMENTS_v3.md

**USBL Verification**:
- Location: `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.UsblVerification/`
- Files: README.md, INTEGRATION.md

---

## Services Documentation

| Service | Document | Description |
|---------|----------|-------------|
| Theme Service | [ThemeService.md](Services/ThemeService.md) | Application theming (Light/Dark/Modern) |
| Certification Service | [CertificationService.md](Services/CertificationService.md) | Processing certificates |
| Event Aggregator | [EventAggregator.md](Services/EventAggregator.md) | Cross-module messaging |
| Module Manager | [ModuleManager.md](Services/ModuleManager.md) | Module discovery and loading |

---

## Licensing Documentation

### Central Licensing Docs

| Document | Location | Description |
|----------|----------|-------------|
| [Licensing Overview](Licensing/README.md) | Documentation/Licensing/ | Offline-first licensing system |
| [Vendor Guide](Licensing/VendorGuide.md) | Documentation/Licensing/ | License generation for vendors |
| [Technical Reference](Licensing/TechnicalReference.md) | Documentation/Licensing/ | Implementation details |

### License Manager Solution

Location: `Licensing Manager solution and server/FathomOS-LicenseManager-v3.4.9-source/`

| Document | Description |
|----------|-------------|
| README.md | License Manager overview |
| CHANGELOG.md | Version history |
| docs/Getting-Started.md | Development setup |
| docs/Generating-Licenses.md | License generation workflow |
| docs/Troubleshooting.md | Common issues |

### Deployment Guides

| Document | Location | Description |
|----------|----------|-------------|
| DEPLOYMENT_GUIDE.md | Licensing Manager solution and server/ | General deployment |
| RENDER_DEPLOYMENT_GUIDE.md | Licensing Manager solution and server/ | Render.com cloud deployment |

---

## Security Documentation

Location: `Security/`

| Document | Description |
|----------|-------------|
| [Security Audit Report](../Security/SecurityAuditReport.md) | Full security audit results |
| [Vulnerability Report](../Security/VulnerabilityReport.md) | Identified vulnerabilities |
| [Licensing Security Report](../Security/LicensingSecurityReport.md) | Licensing system security |
| [Recommendations Report](../Security/RecommendationsReport.md) | Security recommendations |
| [Compliance Checklist](../Security/ComplianceChecklist.md) | Security compliance checklist |
| [Bugs and Missing Implementation](../Security/BugsAndMissingImplementation.md) | Known issues |

---

## Deployment Documentation

| Document | Location | Description |
|----------|----------|-------------|
| [Installation Guide](Deployment/Installation.md) | Documentation/Deployment/ | Installation instructions |
| [Configuration Guide](Deployment/Configuration.md) | Documentation/Deployment/ | Configuration options |

---

## Integration Guides

| Document | Location | Description |
|----------|----------|-------------|
| Module Integration Guide | Root | How to create and integrate modules |
| Licensing Integration Manual | Root | Integrating the licensing system |
| Certificate System README | Root | Certificate generation and verification |

---

## Planning Documents

Location: Root directory

| Document | Description |
|----------|-------------|
| [R&D Analysis Plan](../RnD_Analysis_Plan.md) | Research and development planning |
| [R&D Implementation Delegation](../RnD_Implementation_Delegation.md) | Task delegation plan |
| [R&D Improvement Plan](../RnD_Improvement_Plan.md) | System improvement roadmap |

Location: `Documentation/`

| Document | Description |
|----------|-------------|
| [Upgrade Plan](FATHOM_OS_UPGRADE_PLAN.md) | System upgrade planning |
| [Comprehensive Upgrade Plan](FATHOM_OS_COMPREHENSIVE_UPGRADE_PLAN.md) | Detailed upgrade strategy |

---

## AI Agent Documentation

Location: `.claude/agents/`

### Infrastructure Agents
- ARCHITECTURE-AGENT.md - System design oversight
- SHELL-AGENT.md - Shell application management
- CORE-AGENT.md - Core services management
- DATABASE-AGENT.md - Database and sync management
- SECURITY-AGENT.md - Security reviews

### Support Agents
- BUILD-AGENT.md - CI/CD and builds
- TEST-AGENT.md - Testing infrastructure
- DOCUMENTATION-AGENT.md - Documentation management
- LICENSING-AGENT.md - License system management
- CERTIFICATION-AGENT.md - Certificate system

### Module Agents
- MODULE-SurveyListing.md
- MODULE-SurveyLogbook.md
- MODULE-NetworkTimeSync.md
- MODULE-EquipmentInventory.md
- MODULE-SoundVelocity.md
- MODULE-GnssCalibration.md
- MODULE-MruCalibration.md
- MODULE-UsblVerification.md
- MODULE-TreeInclination.md
- MODULE-RovGyroCalibration.md
- MODULE-VesselGyroCalibration.md
- MODULE-PersonnelManagement.md
- MODULE-ProjectManagement.md

---

## UI Design Documentation

Location: `FathomOS.UI/Documentation/`

| Document | Description |
|----------|-------------|
| DesignSystem.md | UI design system overview |
| UIDesignSystemPlan.md | Detailed UI design plan |

---

## Build Reports

Location: `BuildReports/`

| Document | Description |
|----------|-------------|
| BuildDelegationReport.md | Build task delegation |
| WarningFixProgress.md | Build warning fix progress |

---

## Document Status Legend

| Status | Meaning |
|--------|---------|
| **Current** | Up-to-date and authoritative |
| **Reference** | Historical/reference documentation |
| **Archived** | Old versions kept for history |

---

## Recommended Reading Order

### For New Users
1. [Getting Started](GettingStarted.md)
2. [User Manual](USER_MANUAL.md)
3. Module documentation for your needs

### For Developers
1. [Developer Guide](DeveloperGuide.md)
2. [Architecture](Architecture.md)
3. [Module Integration Guide](../FATHOM_OS_MODULE_INTEGRATION_GUIDE.md)
4. [API Reference](API_REFERENCE.md)

### For Administrators
1. [Installation Guide](Deployment/Installation.md)
2. [Configuration Guide](Deployment/Configuration.md)
3. [Licensing Overview](Licensing/README.md)
4. [Security Documentation](#security-documentation)

---

*This index is maintained by the DOCUMENTATION-AGENT.*
