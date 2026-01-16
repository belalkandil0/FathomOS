# Fathom OS Time Sync Agent - System Tray Widget

A system tray application that monitors the Time Sync Agent service and displays a live clock with connection status.

## Features

- **System Tray Icon** - Color-coded status indicator (green/yellow/red)
- **Live Clock** - Full time display with seconds, updates smoothly
- **Service Status** - Running/Stopped/Starting states
- **Connection Statistics** - Total connections, last connection time
- **Uptime Display** - How long the service has been running
- **Service Control** - Start/Stop/Restart from the tray menu

## Installation

1. Build the application:
   ```
   dotnet build -c Release
   ```

2. Copy the output to the Agent folder or any location

3. Run `FathomOS.TimeSyncAgent.Tray.exe`

## Auto-Start with Windows

To have the tray widget start automatically:

1. Press `Win + R`, type `shell:startup`, press Enter
2. Create a shortcut to `FathomOS.TimeSyncAgent.Tray.exe` in this folder

Or run this command (as Administrator):
```cmd
REG ADD "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "FathomOSTimeSyncTray" /t REG_SZ /d "C:\Path\To\FathomOS.TimeSyncAgent.Tray.exe" /f
```

## Usage

- **Left-click** the tray icon to show/hide the status window
- **Right-click** for context menu with service controls
- Status window auto-hides when clicking outside

## Status Colors

| Color | Meaning |
|-------|---------|
| 🟢 Green | Service running, port listening |
| 🟡 Yellow | Service starting or port not yet open |
| 🔴 Red | Service stopped |
| ⚪ Gray | Unknown status |

## Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Fathom OS Time Sync Agent service installed

## Version History

- **1.0.0** - Initial release
  - System tray icon with status colors
  - Live clock display
  - Connection statistics
  - Service control (Start/Stop/Restart)
