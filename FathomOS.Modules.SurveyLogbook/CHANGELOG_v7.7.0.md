# Survey Logbook Module - v7.7.0 Changelog

## Release Date: January 2, 2026

## Major Changes

### NaviPac Connection Architecture Rewrite

**CRITICAL FIX: TCP Mode Now Works Correctly**

The previous implementation had the TCP client/server roles reversed:
- **OLD (Broken)**: Module tried to CONNECT TO NaviPac as a client
- **NEW (Fixed)**: Module starts a TCP SERVER (TcpListener) that NaviPac connects to

This matches how NaviPac User Defined Output actually works:
- NaviPac is configured with an IP:Port destination
- NaviPac SENDS data TO that destination
- We must LISTEN for incoming connections/datagrams

### Protocol Support

| Mode | How It Works |
|------|--------------|
| **TCP** | Module starts TcpListener on configured port. NaviPac connects and sends data. |
| **UDP** | Module binds UdpClient to port. NaviPac sends datagrams. |

### New Features

1. **Configurable Separator Character**
   - Supports all NaviPac separator options: Comma, Semicolon, Colon, Space, Tab
   - Default: Comma (,)

2. **Debug Logging**
   - When enabled, creates detailed log file in `%APPDATA%\FathomOS\SurveyLogbook\Logs\`
   - Logs all raw data received, parsed fields, and connection events
   - Helpful for troubleshooting data reception issues

3. **Field Mapping Configuration**
   - New `NaviPacFieldMapping` class for explicit field position mapping
   - Auto-detect mode (default) tries to intelligently parse any field order
   - Explicit mapping mode for known configurations

4. **Enhanced Statistics**
   - Tracks bytes received, messages parsed, packets received
   - Records source IP addresses for UDP connections
   - Connection count tracking

### Settings Changes

New settings in `ConnectionSettings`:
- `NaviPacSeparator` (char) - Separator character for parsing (default: ',')
- `EnableDebugLogging` (bool) - Enable detailed debug logging (default: true)
- `FieldMapping` - Field mapping configuration

Default port changed from 4001 to 8123 to match typical NaviPac configurations.

### Bug Fixes

- **TCP Connection**: Fixed fundamental architecture issue where TCP was trying to connect as client instead of listening as server
- **UDP Reception**: Improved UDP listener stability and error handling
- **Data Parsing**: More robust parsing with configurable separator
- **Resource Cleanup**: Better cleanup of TCP/UDP resources on disconnect

### Data Format Support

Supports parsing these NaviPac User Defined Output fields:
- Event number/text
- Gyro/Heading
- Roll, Pitch, Heave
- Easting, Northing (grid coordinates)
- Latitude, Longitude (decimal degrees)
- Height
- KP (Kilometre Post)
- DAL (Distance Along Line)
- DCC/DOL (Distance Cross Course / Off Line)
- Date/Time

### How to Configure NaviPac

1. In NaviPac Configuration, add a **User Defined Output** instrument
2. Set I/O to:
   - **UDP**: `udp://[SurveyLogbook_PC_IP]:8123/` 
   - **TCP**: `tcp://[SurveyLogbook_PC_IP]:8123/`
3. Configure the Format with desired fields
4. Set Item Separator to match Survey Logbook settings
5. In Survey Logbook Settings, set port to 8123 (or matching port)
6. Select UDP or TCP protocol
7. Click Connect

### Troubleshooting

If you don't see data coming in:

1. **Check Firewall**: 
   - Windows Firewall may block incoming connections
   - Add exception for the port (default 8123)

2. **Verify NaviPac Output is ON**:
   - In NaviPac, ensure I/O Mode is set to ON

3. **Check Network**:
   - Ensure NaviPac PC can reach Survey Logbook PC
   - For localhost testing, use 127.0.0.1

4. **Enable Debug Logging**:
   - Turn on debug logging in settings
   - Check log files in `%APPDATA%\FathomOS\SurveyLogbook\Logs\`

5. **Verify Separator**:
   - Check NaviPac Item Separator setting
   - Match in Survey Logbook settings

---

## Files Changed

- `Services/NaviPacClient.cs` - Complete rewrite (TCP Server mode)
- `Models/ConnectionSettings.cs` - Added separator, debug logging, field mapping
- `SurveyLogbookModule.cs` - Version update
- `FathomOS.Modules.SurveyLogbook.csproj` - Version update
- `ModuleInfo.json` - Version update

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 7.7.0 | 2026-01-02 | TCP/UDP architecture fix, configurable separator |
| 7.6.1 | 2026-01-02 | Compilation error fixes |
| 7.6.0 | 2026-01-02 | Feature completion |
