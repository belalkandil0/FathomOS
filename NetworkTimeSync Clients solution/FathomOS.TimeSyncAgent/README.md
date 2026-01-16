# Fathom OS Time Sync Agent

A lightweight Windows service + system tray widget for time synchronization.

## Package Contents

| Component | Description |
|-----------|-------------|
| **Agent Service** | Background Windows service that receives sync commands |
| **Tray Widget** | System tray app showing live clock and service status |

---

## For Developers: Building the Installer

### Prerequisites
- .NET 8 SDK

### Build Steps

1. Double-click **`Build-Installer.bat`**
2. Wait for build to complete (~30 seconds)
3. Find output in `Installer` folder

### Output Structure
```
Installer/
├── Setup.bat                         ← Users run this
├── FathomOS.TimeSyncAgent.exe        ← Background service
├── FathomOS.TimeSyncAgent.Tray.exe   ← System tray widget
├── appsettings.json                  ← Configuration
└── README.txt                        ← User instructions
```

A `TimeSyncAgent_Installer.zip` is also created.

---

## For End Users: Installation

### Install (One Click!)
1. **Double-click `Setup.bat`**
2. Click **Yes** for Administrator
3. Done! ✓

### After Installation
- **Service** runs in background (auto-starts with Windows)
- **Tray Icon** appears in system tray:
  - 🟢 Green = Running normally
  - 🟡 Yellow = Starting
  - 🔴 Red = Stopped/Error
  - Click icon to see details

### Uninstall
1. **Double-click `Setup.bat`**
2. Press **2** for Uninstall
3. Done!

---

## Technical Details

| Setting | Value |
|---------|-------|
| Service Name | `FathomOSTimeSyncAgent` |
| TCP Port | `7700` |
| Install Location | `C:\Program Files\FathomOS\TimeSyncAgent` |

### Configuration (appsettings.json)

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

**Note**: `SharedSecret` must match Fathom OS module settings!

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Service won't start | Check Event Viewer > Application logs |
| Can't connect | Verify firewall rule: `netsh advfirewall firewall show rule name="Fathom OS Time Sync Agent"` |
| Tray icon missing | Check hidden icons (^ in taskbar) or run Tray.exe manually |
| Port in use | Change port in appsettings.json before install |

---

## Version History

- **1.0.2** - Combined installer with Tray widget, simplified deployment
- **1.0.1** - Fixed JSON encoding
- **1.0.0** - Initial release
