# Fathom OS Changelog

All notable changes to Fathom OS are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Planned
- Mobile app companion for Equipment Inventory
- Cloud sync for project files
- Additional export formats

---

## [1.0.48] - 2026-01-18

### Changed
- Updated all server URL references from `fathom-os-license-server` to `s7fathom-license-server`
- Rebuilt License Manager with correct server endpoint
- Server version updated to 3.5.1

---

## [1.0.47] - 2026-01-18

### Added
- **Multi-Seat Licensing Server Endpoints**: Full server-side support for concurrent user licensing
  - `POST /api/license/seats/acquire` - Acquire a seat for the current session
  - `POST /api/license/seats/release` - Release a seat when done
  - `POST /api/license/seats/heartbeat` - Keep seat active with heartbeat
  - `GET /api/license/seats/status` - Get seat status for a license
  - `GET /api/license/seats/sessions` - Get active sessions
  - `POST /api/license/seats/force-release` - Force release a seat (admin)
- **Email Service for License Transfers**: SMTP-based email notifications
  - Transfer verification emails with HTML templates
  - Activation confirmation emails
  - Expiration warning notifications
  - Revocation notifications
- **Issuer-Based Certificate Validation**: Improved SSL/TLS pinning for Render.com deployments
  - Support for Let's Encrypt auto-rotating certificates
  - Configurable trusted issuers list
  - Runtime configuration via `CertificatePinningConfig`
- **Comprehensive Documentation Index**: `Documentation/INDEX.md` with full navigation
- **FathomOS-Specific .gitignore Entries**: Proper exclusions for build outputs

### Changed
- `PinnedHttpClientHandler.cs` now uses issuer-based validation by default
- Updated Let's Encrypt ISRG Root CA thumbprints (X1 and X2)
- Documentation version updated to 1.0.47

### Fixed
- `AntiDebug.cs` missing `System.IO` using statement
- `SettingsWindow.xaml.cs` property name mismatch (`ExpirationDate` → `ExpiresAt`)
- `LicenseController.cs` class structure for multi-seat endpoints

---

## [1.0.46] - 2026-01-18

### Added
- Documentation consolidation and organization
- Updated README.md with recent changes summary

### Changed
- Standardized documentation version numbers

---

## [1.0.45] - 2026-01-18

### Added
- **Digital Certificate System**: Generate professional processing certificates
  - Certificate Manager window for viewing all certificates
  - Certificate Viewer with PDF-like display
  - Signatory management with default selection
  - File hash tracking for input/output verification
- **White-Label Support**: Certificates include licensee branding
- **Offline Certificate Generation**: Create certificates locally, sync when online
- Comprehensive documentation suite:
  - Updated README.md
  - Architecture documentation
  - Developer guide with coding standards
  - API reference
  - User manual
  - Module-specific documentation

### Changed
- Dashboard footer now includes Certificates button
- Improved license validation performance
- Updated MahApps.Metro to latest version

### Fixed
- Theme switching now applies to all open module windows
- License grace period calculation corrected
- Memory optimization for large survey files

---

## [1.0.44] - 2026-01-15

### Added
- **Certificate API for Modules**: Fluent builder pattern for easy certificate creation
  - `CertificateHelper.QuickCreate()` for streamlined certificate generation
  - Support for processing data, input files, and output files
- **Signatory Selection Dialog**: Quick selection of recent signatories
- New files added:
  - `FathomOS.Core/Certificates/CertificateHelper.cs`
  - `FathomOS.Core/Certificates/CertificatePdfGenerator.cs`
  - `FathomOS.Shell/Views/CertificateListWindow.xaml`
  - `FathomOS.Shell/Views/CertificateViewerWindow.xaml`
  - `FathomOS.Shell/Views/SignatoryDialog.xaml`

### Changed
- LicensingSystem.Client updated with certificate storage and sync
- LicensingSystem.Shared updated with certificate models
- FathomOS.Core.csproj now includes Microsoft.Data.Sqlite

---

## [1.0.43] - 2026-01-10

### Added
- Network Time Sync module version 2.0
  - GPS time source support
  - Reference computer mode
  - Network client management
  - Improved sync status dashboard

### Changed
- Survey Logbook upgraded to version 11.0.0
  - Enhanced NaviPac integration
  - DVR monitoring status
  - Position fix management improvements

### Fixed
- Equipment Inventory QR code generation on high-DPI displays
- Project Management milestone date calculations

---

## [1.0.42] - 2026-01-05

### Added
- USBL Verification module version 1.7.0
  - 7-step guided workflow
  - Quality dashboard with visual gauges
  - IQR outlier detection with auto-exclude
  - 3D scatter plot visualization
  - 6 chart themes with legends

### Changed
- Calibration modules now share common approval workflow
- Unified certificate generation across all calibration modules

### Fixed
- DXF export layer naming consistency
- Excel export date formatting for non-US locales

---

## [1.0.40] - 2025-12-20

### Added
- Module Groups feature for organizing related modules
  - Calibrations group containing all calibration modules
  - Expandable tiles on dashboard
- Tree Inclination module for Christmas tree verticality analysis
- ROV Gyro Calibration module
- Vessel Gyro Calibration module

### Changed
- Dashboard now groups related calibration modules
- Improved module discovery for grouped modules

---

## [1.0.35] - 2025-12-15

### Added
- Equipment Inventory module version 1.0
  - QR code generation and scanning
  - Inward/Outward manifest generation
  - Calibration tracking
  - Multi-location support
  - Equipment history tracking

### Changed
- Personnel Management enhanced with competency matrix
- Project Management milestone tracking improvements

---

## [1.0.30] - 2025-12-01

### Added
- Personnel Management module
- Project Management module
- Auto-save service for all modules

### Changed
- Unified theme service across modules
- Improved error reporting with `IErrorReporter`

### Fixed
- License validation on systems with multiple network adapters
- Theme persistence across application restarts

---

## [1.0.20] - 2025-11-15

### Added
- MRU Calibration module
- GNSS Calibration module
- Event aggregator for cross-module communication

### Changed
- Survey Listing now supports additional smoothing algorithms:
  - Savitzky-Golay filter
  - Gaussian smoothing
  - Kalman smoothing

### Fixed
- Route alignment calculations for curved segments
- Tide interpolation edge cases

---

## [1.0.10] - 2025-10-15

### Added
- Sound Velocity Profile module version 1.2
  - Chen-Millero formula support
  - Del Grosso formula support
  - Multiple input format support

### Changed
- Improved NPD parser with better column detection
- DXF export now includes 3D polyline option

### Fixed
- Memory usage optimization for large files (>100,000 points)
- KP calculation accuracy for long routes

---

## [1.0.0] - 2024-12-12

### Added
- Initial release of Fathom OS modular architecture
- FathomOS.Shell - Main application dashboard
- FathomOS.Core - Shared library with models, parsers, services
- Survey Listing Generator module version 1.5.6
  - NPD file parsing with column mapping
  - RLX route alignment processing
  - Tide corrections with interpolation
  - Position and depth smoothing
  - KP and DCC calculations
  - Multiple export formats (Excel, DXF, PDF, Text)
- Survey Electronic Logbook module
  - Event logging with timestamps
  - NaviPac integration
  - DPR generation
- Offline-first licensing system
  - ECDSA signature validation
  - Hardware fingerprinting
  - Module-based licensing

---

## Migration Notes

### Upgrading from 1.0.x to 1.1.x (Future)

When version 1.1.0 is released:
1. Backup your projects and settings
2. Uninstall previous version (settings will be preserved)
3. Install new version
4. Reactivate license if prompted

### Known Breaking Changes

#### Version 1.0.40
- Module paths changed to support groups
- Projects saved with older versions may need manual re-linking of module data

#### Version 1.0.30
- Settings file format updated
- Previous settings will be migrated automatically on first launch

---

## Version Numbering

Fathom OS follows Semantic Versioning:

- **MAJOR** (1.x.x): Breaking changes, major feature releases
- **MINOR** (x.1.x): New features, backward compatible
- **PATCH** (x.x.1): Bug fixes, minor improvements

---

## Release Schedule

| Release Type | Frequency | Contains |
|--------------|-----------|----------|
| Major | Annual | Breaking changes, major features |
| Minor | Quarterly | New features, enhancements |
| Patch | As needed | Bug fixes, security updates |

---

## Support Policy

| Version | Support Status | End of Support |
|---------|----------------|----------------|
| 1.0.x | Active | December 2027 |
| 0.9.x | End of Life | - |

---

*For the latest updates, check the official Fathom OS website.*

---

[Unreleased]: https://github.com/fathomos/fathomos/compare/v1.0.45...HEAD
[1.0.45]: https://github.com/fathomos/fathomos/compare/v1.0.44...v1.0.45
[1.0.44]: https://github.com/fathomos/fathomos/compare/v1.0.43...v1.0.44
[1.0.43]: https://github.com/fathomos/fathomos/compare/v1.0.42...v1.0.43
[1.0.42]: https://github.com/fathomos/fathomos/compare/v1.0.40...v1.0.42
[1.0.40]: https://github.com/fathomos/fathomos/compare/v1.0.35...v1.0.40
[1.0.35]: https://github.com/fathomos/fathomos/compare/v1.0.30...v1.0.35
[1.0.30]: https://github.com/fathomos/fathomos/compare/v1.0.20...v1.0.30
[1.0.20]: https://github.com/fathomos/fathomos/compare/v1.0.10...v1.0.20
[1.0.10]: https://github.com/fathomos/fathomos/compare/v1.0.0...v1.0.10
[1.0.0]: https://github.com/fathomos/fathomos/releases/tag/v1.0.0
