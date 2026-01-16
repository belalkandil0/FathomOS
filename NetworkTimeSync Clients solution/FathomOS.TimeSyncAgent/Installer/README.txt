FATHOM OS TIME SYNC AGENT
=========================

This package installs TWO components:

1. TIME SYNC AGENT (Background Service)
   - Runs silently in background
   - Receives time sync commands from Fathom OS
   - Auto-starts with Windows

2. TRAY WIDGET (System Tray Monitor)
   - Shows live clock in system tray
   - Green/Yellow/Red status indicator
   - Click to see service status and controls
   - Auto-starts with Windows

INSTALLATION
------------
1. Double-click Setup.bat
2. Click "Yes" when asked for Administrator permission
3. Done!

After installation:
- Look for the clock icon in your system tray
- Green = Service running normally
- Click the icon to see status details

UNINSTALLATION
--------------
1. Double-click Setup.bat
2. Select option 2 (Uninstall)

DETAILS
-------
Service Name:    FathomOSTimeSyncAgent
Port:            7700
Install Folder:  C:\Program Files\FathomOS\TimeSyncAgent

TROUBLESHOOTING
---------------
If Setup.bat doesn't request admin permissions:
  Right-click Setup.bat -> "Run as administrator"

If the service won't start:
  Open Services (services.msc) and check FathomOSTimeSyncAgent

If tray icon doesn't appear:
  Check hidden icons in system tray (click ^ arrow)
  Or run: "C:\Program Files\FathomOS\TimeSyncAgent\FathomOS.TimeSyncAgent.Tray.exe"

To verify service is running:
  Open Command Prompt: sc query FathomOSTimeSyncAgent
  Should show: STATE: RUNNING
