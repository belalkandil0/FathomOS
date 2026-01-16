# Tree Inclination Analysis Module

**Fathom OS Module** | Version 1.0

## Overview

The Tree Inclination Analysis module calculates the inclination (tilt) of subsea structures such as Christmas trees, manifolds, and templates by analyzing depth measurements at multiple corner points.

## Features

- **NPD File Parsing**: Parse depth data from NaviPac and other navigation systems
- **Flexible Corner Support**: 3+ corners (4-point standard, 5/6+ for complex structures)
- **Multiple Coordinate Input Methods**:
  - Easting/Northing from survey drawings
  - Structure dimensions (auto-generate coordinates)
  - Direct X/Y entry
- **Tide Correction**: Automatic depth corrections from tide files
- **Gyro Heading**: Extract heading for true bearing calculation
- **Interactive Visualization**: 2D charts and 3D model
- **Export Options**: Excel, PDF, DXF, Chart images
- **Certificate Generation**: Professional verification certificates

## Quick Start

1. **Load NPD Files**: Click "Load Files" and select depth files for each corner
2. **Enter Coordinates**: Use E/N from drawings, structure dimensions, or direct X/Y
3. **Optional - Tide**: Load tide file for depth corrections
4. **Calculate**: Click "Calculate Inclination" (Ctrl+Enter)

## Coordinate Input Methods

### Method 1: Easting/Northing
Enter coordinates from as-built survey drawings. Click "Convert to X/Y" to calculate relative positions using the first corner as origin.

### Method 2: Structure Dimensions
Enter structure width and length. Click "Generate Coordinates" to auto-populate corners in standard layout:
- Corner 1: (0, 0)
- Corner 2: (Width, 0)
- Corner 3: (Width, Length)
- Corner 4: (0, Length)

### Method 3: Direct X/Y
Enter relative coordinates directly in meters.

## Important Notes

⚠️ **Position Data from NPD Files**: The Easting/Northing values extracted from NPD files are typically vessel/ROV positions, NOT the actual corner coordinates. Always enter the structure's true corner positions from as-built drawings.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Enter | Calculate Inclination |
| Ctrl+S | Save Project |
| Ctrl+O | Open Project |
| Ctrl+N | New Project |
| F1 | Open Help |

## Calculation Methods

- **4-Point Diagonal**: Uses diagonal depth differences for pitch/roll calculation
- **Best-Fit Plane**: Least-squares plane fitting for 5+ corners

## QC Status

| Status | Inclination | Description |
|--------|-------------|-------------|
| ✅ OK | < 0.5° | Within acceptable tolerance |
| ⚠️ Warning | 0.5° - 1.0° | Marginal - review required |
| ❌ Fail | > 1.0° | Exceeds tolerance |

## Structure Heading

Set the structure heading (compass bearing) to enable true bearing calculation for the tilt direction. You can:
- Enter manually from as-built drawings
- Use average gyro heading from NPD files (click "Use" button)

## Certificate System

This module supports Fathom OS certificate generation. After successful processing, you can generate a professional verification certificate that includes:

- Structure identification and project details
- Inclination measurements and tolerances
- Corner depth data
- QC status and approval
- Digital signature verification

## Requirements

- FathomOS.Core reference
- .NET 8.0 (Windows)
- WPF

## Author

Fathom OS

## License

Proprietary - Fathom OS
