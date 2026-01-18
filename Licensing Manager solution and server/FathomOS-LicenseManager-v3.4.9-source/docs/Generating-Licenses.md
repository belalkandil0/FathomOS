# Generating Licenses

This guide covers how to generate both online and offline licenses.

## Online Licenses

Online licenses require the client to connect to the license server for activation and periodic validation.

### Using the Desktop UI

1. Open **License Manager UI**
2. Go to **Licenses** page
3. Click **+ New License**
4. Fill in the details:
   - **Client Name:** Customer's company name
   - **Client Email:** Customer's email
   - **License Type:** Select "Online"
   - **Tier:** Select license tier (Basic/Professional/Enterprise)
   - **Expiry Date:** Set expiration
   - **Modules:** Select enabled modules
5. Click **Create**
6. Copy the **License Key** and send to customer

### Activation in FathomOS

The customer enters the license key in FathomOS:

```csharp
var result = await licensing.ActivateLicenseAsync("XXXXX-XXXXX-XXXXX-XXXXX");
if (result.Success)
{
    // License activated
}
```

## Offline Licenses

Offline licenses work without internet connection. They are bound to specific hardware.

### When to Use Offline Licenses

- Customer has no internet access
- Air-gapped environments
- Vessels without reliable connectivity

### Generating Offline Licenses

#### Step 1: Get Hardware ID

The customer must run FathomOS and get their Hardware ID:

```csharp
var hardwareId = HardwareFingerprint.Generate();
// Returns: "ABC123DEF456..."
```

Or use the built-in hardware ID display in FathomOS settings.

#### Step 2: Generate Offline License

1. In **License Manager UI**, create a new license
2. Select **License Type: Offline**
3. Enter the customer's **Hardware ID**
4. Configure modules and expiry
5. Click **Generate Offline License**
6. Save the `.lic` file

#### Step 3: Deliver the License File

Send the `.lic` file to the customer via:
- Email attachment
- USB drive
- Secure file transfer

#### Step 4: Install in FathomOS

The customer places the `.lic` file in:
```
%LocalAppData%\FathomOS\license.lic
```

Or uses the import function in FathomOS.

## License File Format

Offline licenses are JSON files with digital signatures:

```json
{
  "LicenseId": "LIC-2026-001",
  "LicenseKey": "XXXXX-XXXXX-XXXXX-XXXXX",
  "ClientName": "Oceanic Surveys Ltd",
  "ProductName": "FathomOS",
  "Tier": "Professional",
  "HardwareId": "ABC123...",
  "IssuedAt": "2026-01-18T00:00:00Z",
  "ExpiresAt": "2027-01-18T00:00:00Z",
  "Modules": ["SurveyListing", "GnssCalibration"],
  "Signature": "MEUCIQ..."
}
```

## PDF Certificates

Generate professional PDF certificates for customers:

1. Select a license in the UI
2. Click **Generate Certificate**
3. Choose theme (Light/Dark)
4. Save or print the PDF

The certificate includes:
- License details
- QR code for verification
- Support code
- Branding information

## Managing Licenses

### Revoke a License

1. Select the license
2. Click **Revoke**
3. Enter reason
4. Confirm

### Extend a License

1. Select the license
2. Click **Extend**
3. Set new expiry date
4. Confirm

### Transfer a License

Customers can self-service transfer via the portal:

1. Go to `/portal` on the server
2. Enter license key and email
3. Request transfer
4. Verify on new machine
