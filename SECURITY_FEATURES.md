# Fathom OS Security Features

This document describes the security and anti-piracy measures implemented in Fathom OS.

---

## Table of Contents

1. [Anti-Debug Protection](#anti-debug-protection)
2. [ConfuserEx Obfuscation](#confuserex-obfuscation)
3. [Dotfuscator Community Obfuscation](#dotfuscator-community-obfuscation)
4. [License Protection](#license-protection)
5. [Build Process](#build-process)
6. [Future Enhancements](#future-enhancements)

---

## Anti-Debug Protection

**Location:** `FathomOS.Shell/Security/AntiDebug.cs`

The AntiDebug class provides multiple layers of protection against reverse engineering:

### Detection Methods

| Method | Description |
|--------|-------------|
| **Debugger.IsAttached** | Checks if managed debugger is attached |
| **IsDebuggerPresent** | Windows API check for native debugger |
| **CheckRemoteDebuggerPresent** | Detects remote debugging sessions |
| **NtQueryInformationProcess** | Low-level check for debug port |
| **Timing Check** | Detects stepping (debuggers slow execution) |
| **Process Scanner** | Detects dnSpy, ILSpy, x64dbg, etc. |

### Protected Debugger Tools

- dnSpy, ILSpy, dotPeek (decompilers)
- x64dbg, x32dbg, OllyDbg, WinDbg (debuggers)
- IDA Pro, Ghidra (disassemblers)
- Cheat Engine, Process Hacker (memory tools)
- Fiddler, Wireshark (network analysis)

### Continuous Monitoring

The app starts a background thread that checks for debugger attachment every 5 seconds:

```csharp
AntiDebug.StartContinuousMonitoring(() =>
{
    // Application shuts down if debugger detected
});
```

### Development Mode

Anti-debug is disabled in DEBUG builds:
```csharp
#if !DEBUG
    if (AntiDebug.IsDebuggingDetected()) { ... }
#endif
```

---

## ConfuserEx Obfuscation

**Configuration:** `FathomOS.crproj`

ConfuserEx is a free, open-source .NET obfuscator.

### Installation

1. Download from: https://github.com/mkaring/ConfuserEx/releases
2. Extract to `C:\Tools\ConfuserEx\` (or update path in Build-Release.ps1)

### Protections Applied

| Protection | Description |
|------------|-------------|
| **Anti-Tamper** | Prevents modification of the assembly |
| **Anti-Debug** | Additional debugger detection layer |
| **Anti-Dump** | Prevents memory dumping |
| **Anti-ILDasm** | Blocks IL disassembly tools |
| **Constants** | Encrypts strings and constants |
| **Control Flow** | Scrambles code execution flow |
| **Invalid Metadata** | Confuses decompilers |
| **Reference Proxy** | Hides method references |
| **Rename** | Renames all identifiers to unreadable names |
| **Resources** | Encrypts embedded resources |

### Running ConfuserEx

**Command Line:**
```bash
Confuser.CLI.exe FathomOS.crproj
```

**GUI:**
1. Open ConfuserEx.exe
2. Drag FathomOS.crproj onto the window
3. Click "Protect!"

**Output:** `.\Confused\` folder

---

## Dotfuscator Community Obfuscation

**Configuration:** `FathomOS.Dotfuscator.xml`

Dotfuscator Community Edition comes free with Visual Studio.

### Installation

Already included with Visual Studio 2022:
```
Tools > PreEmptive Protection - Dotfuscator Community
```

### Protections Applied

| Protection | Description |
|------------|-------------|
| **Renaming** | Renames identifiers with unprintable characters |
| **Control Flow** | High-level code flow obfuscation |
| **String Encryption** | Encrypts string literals |
| **Watermarking** | Hidden identifier for tracking copies |
| **Removal** | Removes unused code |

### Running Dotfuscator

**Visual Studio:**
1. Tools > PreEmptive Protection - Dotfuscator Community
2. File > Open Project > FathomOS.Dotfuscator.xml
3. Build > Build Solution

**Command Line:**
```bash
dotfuscator.exe FathomOS.Dotfuscator.xml
```

**Output:** `FathomOS.Shell\bin\Release\net8.0-windows\Dotfuscated\`

---

## License Protection

### Storage Security

- **DPAPI Encryption**: License data encrypted using Windows DPAPI (CurrentUser scope)
- **Multiple Locations**: Stored in both file and registry for redundancy
- **Checksum Verification**: Tamper detection using SHA256
- **First Activation Tracking**: Prevents "delete meta.dat" reset attacks

### Validation Features

| Feature | Description |
|---------|-------------|
| **Hardware Binding** | License bound to CPU, motherboard, BIOS, etc. |
| **Fuzzy Matching** | Survives minor hardware changes (3/5 must match) |
| **Server Validation** | Periodic online verification |
| **Revocation Check** | Background monitoring for revoked licenses |
| **Clock Tampering** | Detects system time manipulation |
| **Grace Period** | 14-day grace after expiration |
| **License Type** | Distinguishes Online vs Offline licenses |

### Anti-Piracy Measures

1. **Single Device**: Each license key can only activate on ONE computer
2. **Server Sync**: Offline activations are reported to server when online
3. **Heartbeat**: Regular check-ins to verify license status
4. **Revocation**: Server can instantly revoke compromised licenses
5. **First Activation Lock**: Prevents resetting by deleting local files

---

## Build Process

### Automated Build Script

Use `Build-Release.ps1` for production builds:

```powershell
# Build with ConfuserEx obfuscation (recommended)
.\Build-Release.ps1 -ObfuscationTool ConfuserEx

# Build with Dotfuscator obfuscation
.\Build-Release.ps1 -ObfuscationTool Dotfuscator

# Build with both obfuscators
.\Build-Release.ps1 -ObfuscationTool Both

# Build without obfuscation (development only!)
.\Build-Release.ps1 -ObfuscationTool None
```

### Recommended Distribution Build

For maximum protection, use ConfuserEx:

```powershell
.\Build-Release.ps1 -ObfuscationTool ConfuserEx
```

Then distribute files from `.\Confused\` folder.

### Build Checklist

- [ ] Build in Release mode (not Debug)
- [ ] Run obfuscation (ConfuserEx or Dotfuscator)
- [ ] Test obfuscated build works correctly
- [ ] Verify license activation works
- [ ] Package from obfuscated output folder

---

## Future Enhancements

### Code Signing Certificate (Paid)

**Cost:** $70-400/year

Prevents "Unknown Publisher" warnings and proves authenticity.

**Providers:**
- Sectigo (Comodo): ~$75/year
- DigiCert: ~$400/year
- SSL.com: ~$70/year

### Additional Protections (If Needed)

| Enhancement | Cost | Description |
|-------------|------|-------------|
| **.NET Reactor** | $179+ | Commercial obfuscator with more features |
| **VMProtect** | $250+ | Virtual machine-based protection |
| **Themida** | $199+ | Advanced anti-tamper |

### Recommendations

**Current protection level is good for most cases.** The combination of:
- Anti-debug protection
- ConfuserEx obfuscation
- Server-based licensing
- Hardware binding

...provides strong protection against casual piracy. Professional crackers can break any protection, but the goal is to make it harder than buying a license.

---

## Troubleshooting

### Anti-Debug False Positives

Some legitimate software may trigger detection:
- Development tools (Visual Studio - only triggers in Release builds)
- Performance profilers
- Some antivirus software

**Solution:** Anti-debug only runs in Release builds (`#if !DEBUG`)

### Obfuscation Breaking WPF

WPF XAML bindings can break if types are renamed.

**Solution:** Exclusions are configured in both `.crproj` and `.Dotfuscator.xml`:
```xml
<excludelist>
  <type name="FathomOS.Shell.Views.*" regex="true" />
</excludelist>
```

### License Issues After Obfuscation

JSON serialization needs original property names.

**Solution:** LicensingSystem.Shared types are excluded from renaming.

---

## Security Contact

If you discover a security vulnerability, please contact:
[Your support email]

---

**Version:** 1.0.34
**Last Updated:** December 2024
