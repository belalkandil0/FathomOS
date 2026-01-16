# Fathom OS Network Time Sync Module

A module for the Fathom OS Shell that discovers, monitors, and synchronizes time across network computers.

## Features

- **Network Discovery**: Scan IP ranges to find computers with the Time Sync Agent
- **Manual Entry**: Add computers manually by IP address
- **Multiple Time Sources**:
  - Internet NTP servers (time.windows.com, pool.ntp.org)
  - Local NTP server on your network
  - Use this computer as the time reference
  - **GPS Serial (NMEA GGA/RMC)** - For Varipos and other GPS receivers
- **Sync Modes**:
  - One-time force sync
  - Continuous monitoring with auto-correction
- **Dashboard**: Visual status indicators (green/red) for all managed computers
- **Bulk Operations**: Sync all or selected computers at once

## GPS Serial Time Source

For offshore survey operations, the module supports GPS time from serial ports. This provides the most accurate time reference, directly from satellites.

### Supported GPS Devices
- Varipos GPS receivers
- Any GPS outputting standard NMEA-0183 sentences (GGA, RMC)

### GPS Configuration
1. Open Settings (⚙ icon)
2. Select "GPS Serial (NMEA GGA/RMC)" as Time Source
3. Choose the COM port your GPS is connected to
4. Set baud rate (typically 4800, 9600, or 115200)
5. Click "Connect GPS"

### GPS Status Indicators
- **Sats**: Number of satellites in view
- **Fix Quality**: GPS, DGPS, RTK Fixed, etc.
- When GPS is connected, satellite count appears in the header

## Requirements

- Fathom OS Shell 1.0.0 or later
- .NET 8.0 Runtime
- Network connectivity to managed computers
- Time Sync Agent installed on remote computers

## Installation

1. Build the module:
   ```cmd
   dotnet build FathomOS.Modules.NetworkTimeSync.csproj -c Release
   ```

2. Copy output to the Shell's Modules directory:
   ```
   FathomOS/
   └── Modules/
       └── NetworkTimeSync/
           ├── FathomOS.Modules.NetworkTimeSync.dll
           ├── ModuleInfo.json
           └── Assets/
               └── icon.png
   ```

3. Restart Fathom OS Shell - the module will appear on the dashboard.

## Agent Deployment

Each computer you want to manage must have the Time Sync Agent installed:

1. Copy the `FathomOS.TimeSyncAgent` package to each computer
2. Run the installer as Administrator:
   ```powershell
   .\Install-Agent.ps1 -SharedSecret "YourOrganizationSecret"
   ```

3. **IMPORTANT**: Use the same SharedSecret across all agents and in the module settings!

See the [TimeSyncAgent README](../FathomOS.TimeSyncAgent/README.md) for detailed instructions.

## Usage

### Discovering Computers

1. Launch the Network Time Sync module from the Fathom OS dashboard
2. Enter the IP range to scan (e.g., 192.168.1.1 to 192.168.1.254)
3. Click **Discover** to find computers with the agent installed
4. Discovered computers appear in the list automatically

### Adding Computers Manually

1. Click **Add** button
2. Enter the IP address and optional hostname
3. Click **Add Computer**

### Checking Status

- Click **Refresh** to check the time sync status of all computers
- Status indicators:
  - 🟢 Green (●) = Synced (within tolerance)
  - 🔴 Red (●) = Out of Sync (exceeds tolerance)
  - ⚫ Gray (○) = Unreachable or Unknown

### Synchronizing Time

- **Sync All**: Synchronizes all computers in the list
- **Sync Selected**: Select computers using checkboxes, then sync only selected

### Continuous Monitoring

1. Click the **Monitor** button to start continuous monitoring
2. The module will check all computers periodically
3. If configured, out-of-sync computers will be auto-corrected
4. Click **Stop** to end monitoring

### Settings

Click the ⚙ (gear) icon to configure:

- **Time Source**: Choose between Internet NTP, Local NTP, or Host Computer
- **Sync Mode**: One-time or Continuous
- **Tolerance**: How many seconds of drift is acceptable (default: 1.0)
- **Check Interval**: How often to check status in continuous mode
- **Agent Port**: Default port for agent communication (7700)
- **Shared Secret**: Authentication key (must match all agents!)

### Export/Import Configuration

- **Export Config**: Save computer list and settings to a `.nts` file
- **Import Config**: Load a previously saved configuration

## Configuration File

The module saves its configuration to:
```
%LOCALAPPDATA%\FathomOS\Modules\NetworkTimeSync\config.json
```

This includes:
- Sync settings
- Discovery settings
- List of managed computers

## Troubleshooting

### Computers Show as Unreachable

1. Verify the Time Sync Agent is installed and running on the remote computer
2. Check firewall allows port 7700 (or your configured port)
3. Test connectivity: `telnet <ip> 7700`
4. Verify network path exists between Fathom OS host and remote computer

### Authentication Fails

1. Ensure SharedSecret matches exactly between module settings and agent
2. Check that computer clocks aren't too far out of sync (>5 minutes causes auth failure)

### Time Not Changing on Remote Computers

1. Agent service must run as LocalSystem or have "Change system time" privilege
2. Domain policy may restrict time changes - check with your IT administrator
3. Verify `AllowTimeSet: true` in agent's appsettings.json

### Discovery Finds Nothing

1. Verify agents are installed and running on target computers
2. Check IP range is correct for your network
3. Firewall may be blocking the scan

## Project Structure

```
FathomOS.Modules.NetworkTimeSync/
├── NetworkTimeSyncModule.cs      # IModule implementation
├── ModuleInfo.json               # Module metadata
├── Enums/
│   └── SyncEnums.cs              # Status, mode enumerations
├── Models/
│   ├── NetworkComputer.cs        # Computer data model
│   ├── SyncConfiguration.cs      # Settings models
│   └── AgentProtocol.cs          # Communication protocol
├── Services/
│   ├── TimeSyncService.cs        # Agent communication
│   ├── NetworkDiscoveryService.cs # IP scanning
│   ├── StatusMonitorService.cs   # Continuous monitoring
│   ├── ConfigurationService.cs   # Save/load config
│   └── GpsSerialService.cs       # GPS NMEA parsing (GGA/RMC)
├── Infrastructure/
│   ├── ViewModelBase.cs          # MVVM base
│   ├── RelayCommand.cs           # ICommand implementation
│   └── Converters.cs             # XAML converters
├── Themes/
│   ├── ModuleResources.xaml      # Main theme with Dark colors
│   ├── DarkTheme.xaml            # Dark theme styles
│   └── LightTheme.xaml           # Light theme styles
├── ViewModels/
│   └── DashboardViewModel.cs     # Main view model
└── Views/
    ├── DashboardWindow.xaml      # Main window
    └── DashboardWindow.xaml.cs   # Code-behind
```

## Version History

- **2.0.0** - Major Update: History, Scheduling, Exports, System Tray Widget
  - **NEW**: System Tray Widget for Agent (separate application)
    - Live clock display with seconds
    - Service status indicator (green/yellow/red)
    - Connection statistics
    - Service control (Start/Stop/Restart)
  - **NEW**: Sync History Tracking
    - Records all sync operations with timestamps
    - Drift before/after measurements
    - Success/failure tracking
    - View history in module
  - **NEW**: Export Reports
    - Export current status to CSV
    - Export sync history to CSV
    - Export full HTML report (dark theme, opens in browser)
  - **NEW**: Multi-NTP Fallback
    - Primary → Secondary → Tertiary NTP server fallback
    - Configurable fallback behavior
  - **NEW**: Alert Thresholds
    - Warning threshold (default 0.5s)
    - Critical threshold (default 1.0s)
    - Sound alerts for threshold violations
  - **NEW**: Drift Tracking & Prediction
    - Historical drift measurements
    - Trend analysis for drift prediction
  - **FIXED**: Reference time display now correctly shows time source
  - **IMPROVED**: Agent tracks connection statistics

- **1.1.5** - UI Refresh & Display Fixes
  - **Fixed**: Offset now shows "+0.0s" when synced instead of "--"
  - **Fixed**: Last Sync display now updates in real-time ("5s ago", "10s ago", etc.)
  - **Fixed**: System Time column now refreshes every second
  - **Added**: UI refresh timer that updates display properties every second
  - **Improved**: Monitor button state now more visible

- **1.1.4** - UTF-8 BOM Fix (CRITICAL)
  - **CRITICAL FIX**: Agent was sending UTF-8 BOM (0xEF 0xBB 0xBF) which broke JSON parsing
  - Changed StreamWriter to use UTF-8 encoding WITHOUT BOM
  - Fixed Test-Agent.bat script errors
  - Agent version updated to 1.0.1
  - **You MUST rebuild and reinstall the agent for this fix**

- **1.1.3** - Diagnostics & Add This PC Feature
  - Added "Add This PC" button to quickly add localhost and test agent connectivity
  - Added Test-Agent.bat diagnostic tool to troubleshoot connection issues
  - Added Test-Agent.ps1 comprehensive diagnostic script
  - Better error messages when agent connection fails
  - Shows specific troubleshooting steps in MessageBox

- **1.1.2** - Discovery Fix
  - **CRITICAL FIX**: Changed discovery order to check agent port BEFORE ICMP ping
  - Windows Firewall often blocks ICMP ping but allows TCP - this was preventing agent detection
  - Agents are now discovered even when hosts don't respond to ping
  - Only falls back to ping check if agent port is not open

- **1.1.1** - Communication Fixes & Improvements
  - Fixed JSON serialization mismatch between module and agent
  - Added case-insensitive JSON deserialization for better compatibility
  - Added AgentNotInstalled status to all converters
  - Added GpsSerial to TimeSourceToStringConverter
  - Increased concurrent scans from 10 to 20 for faster discovery
  - Version consistency across all files

- **1.1.0** - UI Improvements & Fixes
  - Cancel button for network discovery (cancel scan anytime)
  - Smart IP range detection from actual network adapters
  - Dark/Light theme toggle in title bar
  - Modern borderless window with custom title bar
  - Per-row Sync and Remove buttons
  - Improved status indicators with glow effect
  - Fixed time source radio button bindings
  - Fixed property bindings for status display

- **1.0.0** - Initial release
  - IP range discovery
  - Manual computer entry
  - Internet/Local/Host/GPS time sources
  - One-time and continuous sync modes
  - Dashboard with status indicators

## License

Part of the Fathom OS suite by S7 Solutions.
