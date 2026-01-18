# Changelog

All notable changes to the FathomOS License Management System will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.4.9] - 2026-01-18

### Added
- GitHub repository setup with CI/CD workflows
- Comprehensive deployment guide
- Issue templates for bug reports and feature requests
- Self-contained publishing for Desktop UI

### Fixed
- Build errors in solution file (removed missing project reference)
- `ValidateLicenseAsync` method reference in FathomOSIntegration.cs
- Duplicate class definitions in DashboardController.cs
- Property name mismatches (`IsOffline` → `LicenseType`, `Activations` → `LicenseActivations`)

### Changed
- Desktop UI now publishes as self-contained single-file executable
- Updated GitHub URLs in Desktop UI to point to correct repository

## [3.4.8] - 2026-01-15

### Added
- Customer self-service portal for license transfer
- Two-factor authentication (TOTP) for admin accounts
- Database backup and restore functionality
- Health monitoring dashboard
- Rate limiting with IP blocking
- Audit logging for all operations

### Changed
- Improved session management with heartbeat mechanism
- Enhanced license validation with grace period support

## [3.4.0] - 2026-01-01

### Added
- White-label branding support (Brand, LicenseeCode, SupportCode)
- Processing certificate management
- QR code verification for certificates
- License obfuscation service

### Changed
- Upgraded to .NET 8.0
- Improved hardware fingerprinting algorithm

## [3.0.0] - 2025-10-01

### Added
- Initial release of unified License Management System
- License Server with REST API
- Desktop License Manager UI
- Client integration library
- Online and offline license support
- ECDSA digital signatures
- SQLite database storage
- Docker deployment support

---

For more details, see the [GitHub Releases](https://github.com/belalkandil0/FathomOS/releases).
