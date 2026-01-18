# FathomOS License Manager - Documentation

Welcome to the FathomOS License Management System documentation.

## Quick Links

- [Getting Started](Getting-Started)
- [Server Deployment](Server-Deployment)
- [Desktop UI Guide](Desktop-UI-Guide)
- [API Reference](API-Reference)
- [Generating Licenses](Generating-Licenses)
- [Troubleshooting](Troubleshooting)

## Overview

The FathomOS License Management System consists of three main components:

### 1. License Server
A REST API built with ASP.NET Core 8.0 that handles:
- License creation and management
- Online activation and validation
- Session tracking
- Customer self-service portal
- Admin authentication with 2FA

### 2. License Manager UI
A Windows desktop application (WPF .NET 8.0) for administrators to:
- Create and manage licenses
- Generate offline license files
- Manage modules and tiers
- Monitor server health

### 3. Client Library
A .NET library integrated into FathomOS applications for:
- License activation
- Periodic validation
- Offline license support
- Hardware fingerprinting

## System Requirements

| Component | Requirements |
|-----------|--------------|
| Server | .NET 8.0 Runtime, Linux/Windows/Docker |
| Desktop UI | Windows 10/11, .NET 8.0 Runtime (or self-contained) |
| Client | .NET 8.0, Windows |

## Support

- [Report Issues](https://github.com/belalkandil0/FathomOS/issues)
- [Discussions](https://github.com/belalkandil0/FathomOS/discussions)
