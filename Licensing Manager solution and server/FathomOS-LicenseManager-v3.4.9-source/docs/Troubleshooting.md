# Troubleshooting

Common issues and their solutions.

## Server Issues

### Database Initialization Failed

**Symptoms:** Server fails to start, "Database initialization error" in logs

**Solutions:**
1. Check the database path is writable:
   ```bash
   # Linux/Docker
   chmod 777 /app/data

   # Windows
   icacls data /grant Users:F
   ```

2. Delete corrupted database and restart:
   ```bash
   rm licenses.db
   dotnet run
   ```

3. Check disk space

### Port Already in Use

**Symptoms:** "Address already in use" error

**Solutions:**
1. Find and kill the process using port 5000:
   ```bash
   # Windows
   netstat -ano | findstr :5000
   taskkill /PID <pid> /F

   # Linux
   lsof -i :5000
   kill -9 <pid>
   ```

2. Or change the port:
   ```bash
   ASPNETCORE_URLS=http://+:5001 dotnet run
   ```

### 429 Too Many Requests

**Symptoms:** API returns 429 status

**Solutions:**
1. Wait for rate limit to reset (1 minute)
2. Check if IP is blocked in admin dashboard
3. Unblock IP if needed:
   ```bash
   curl -X POST http://server/api/dashboard/security/unblock-ip \
     -H "Content-Type: application/json" \
     -d '{"ipAddress":"x.x.x.x"}'
   ```

## Desktop UI Issues

### Cannot Connect to Server

**Symptoms:** "Connection failed" error

**Solutions:**
1. Verify server URL in Settings
2. Check server is running: `curl http://server/health`
3. Check firewall allows connection
4. Verify HTTPS certificate if using SSL

### PDF Generation Failed

**Symptoms:** "QuestPDF native library not found" warning

**Solutions:**
1. Install Visual C++ Redistributable
2. Use self-contained publish which includes native libraries
3. Check Windows version (requires Windows 10+)

### Keys Not Saved

**Symptoms:** ECDSA keys reset after restart

**Solutions:**
1. Check settings file permissions:
   ```
   %LocalAppData%\FathomOSLicenseManager\settings.json
   ```
2. Run as Administrator once to create folder
3. Check antivirus isn't blocking file writes

## License Issues

### Invalid Signature

**Symptoms:** "License signature invalid" error

**Causes:**
- License was modified after signing
- Wrong public key in client
- Corrupted license file

**Solutions:**
1. Regenerate the license file
2. Verify public key matches:
   - Server: `appsettings.json` → `CertificatePublicKeyPem`
   - Client: `LicenseConstants.CertificatePublicKeyPem`
3. Re-download license file

### Hardware Mismatch

**Symptoms:** "Hardware fingerprint mismatch" error

**Causes:**
- License activated on different machine
- Hardware changed (new CPU, motherboard, etc.)

**Solutions:**
1. **For online licenses:** Customer requests transfer via portal
2. **For offline licenses:** Generate new license with new Hardware ID

### License Expired

**Symptoms:** "License has expired" error

**Solutions:**
1. Extend license in admin UI
2. Generate new license
3. Check system clock is correct (for offline licenses)

## Client Integration Issues

### Session Conflict

**Symptoms:** "License is in use on another device" error

**Solutions:**
1. Close FathomOS on other device
2. Wait 10 minutes for session timeout
3. Force terminate via admin dashboard
4. Use portal to deactivate other device

### Validation Fails Intermittently

**Symptoms:** Random validation failures

**Solutions:**
1. Check network stability
2. Increase heartbeat interval
3. Enable grace period in server config
4. Check server isn't overloaded

## Debug Commands

### Check Server Status
```bash
curl http://server/health
curl http://server/db-status
```

### View Server Logs
```bash
# Docker
docker logs license-server

# Local
# Check console output
```

### Test License Activation
```bash
curl -X POST http://server/api/license/activate \
  -H "Content-Type: application/json" \
  -d '{
    "licenseKey": "YOUR-LICENSE-KEY",
    "hardwareId": "YOUR-HARDWARE-ID",
    "machineName": "TEST-PC",
    "osVersion": "Windows 11",
    "appVersion": "1.0.0"
  }'
```

## Getting Help

If you can't resolve an issue:

1. Check [GitHub Issues](https://github.com/belalkandil0/FathomOS/issues) for similar problems
2. Create a new issue with:
   - Error message
   - Steps to reproduce
   - Environment details
   - Relevant logs
