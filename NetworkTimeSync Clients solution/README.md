# Fathom OS Time Sync Agent

Network time synchronization agent for Fathom OS Shell.

## Package Contents

```
FathomOS.TimeSyncAgent.sln          ← Solution file (open in Visual Studio)
Build-Installer.bat                  ← One-click build script

FathomOS.TimeSyncAgent/              ← Windows Service (background)
FathomOS.TimeSyncAgent.Tray/         ← System Tray Widget (UI)
```

---

## Quick Start (Developer)

### Option 1: Command Line Build

1. Ensure .NET 8 SDK is installed
2. Double-click **`Build-Installer.bat`**
3. Find output in `Installer` folder

### Option 2: Visual Studio

1. Open `FathomOS.TimeSyncAgent.sln`
2. Build → Publish (or use Build-Installer.bat)

### Build Output

```
Installer/
├── Setup.bat                         ← Give to end users
├── FathomOS.TimeSyncAgent.exe        ← Background service
├── FathomOS.TimeSyncAgent.Tray.exe   ← System tray widget
├── appsettings.json                  ← Configuration
└── README.txt                        ← User instructions
```

Also creates: `TimeSyncAgent_Installer.zip`

---

## End User Installation

### Install
1. Double-click **`Setup.bat`**
2. Click Yes for Administrator
3. Done!

### What Gets Installed

| Component | Description |
|-----------|-------------|
| **Agent Service** | Runs in background, receives sync commands |
| **Tray Widget** | System tray icon with live clock display |

Both auto-start with Windows.

### Uninstall
1. Double-click **`Setup.bat`**
2. Select option 2

---

## Configuration

Edit `appsettings.json` before deployment:

```json
{
  "AgentSettings": {
    "Port": 7700,
    "SharedSecret": "FathomOSTimeSync2024",
    "AllowTimeSet": true,
    "AllowNtpSync": true
  }
}
```

**Important**: `SharedSecret` must match Fathom OS module settings!

---

## Technical Details

| Setting | Value |
|---------|-------|
| Service Name | `FathomOSTimeSyncAgent` |
| TCP Port | `7700` |
| Install Location | `C:\Program Files\FathomOS\TimeSyncAgent` |
| Startup | Automatic (both service and tray) |

---

## Version

- **Agent**: 1.0.2
- **Tray**: 1.0.0
