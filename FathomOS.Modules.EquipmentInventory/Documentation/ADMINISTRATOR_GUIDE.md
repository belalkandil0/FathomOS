# Equipment Inventory Module - Administrator Guide

## First-Time Setup

### Default Administrator Credentials

When the system is first installed, a default administrator account is created:

```
Username: admin
Password: Admin@123!
```

**⚠️ IMPORTANT: This password must be changed on first login.**

The system will automatically prompt you to change the password. You cannot continue using the system until you set a new password.

---

## Password Policy

All passwords must meet these requirements:

| Requirement | Description |
|-------------|-------------|
| Length | At least 8 characters |
| Uppercase | At least one uppercase letter (A-Z) |
| Lowercase | At least one lowercase letter (a-z) |
| Digit | At least one number (0-9) |
| Special | At least one special character (!@#$%^&*) |

**Example valid password:** `SecureP@ss123`

---

## Authentication Workflow

### First-Time Admin Login Flow

```
1. Launch application → Login Window appears
2. Enter: admin / Admin@123!
3. System validates credentials
4. "Change Password Required" dialog appears (mandatory)
5. Enter new password meeting all requirements
6. Confirm new password
7. Click "Change Password"
8. Access granted to main application
```

### Regular User Login Flow

```
1. Enter username/email and password
2. If MustChangePassword = true:
   - "Change Password" dialog appears (mandatory)
   - User sets new password
3. If password expired (> PasswordExpiryDays):
   - "Change Password" dialog appears
4. Access granted to main application
```

### PIN Login (Quick Access)

Users can set up a 4-digit PIN for quick authentication:
- PIN login is available from the login screen
- Useful for mobile app and quick desktop access
- PIN must be set up by admin or user in their profile

---

## User Management

### Creating a New User

1. Navigate to **Administration** → **Users** tab
2. Click **"Add User"** button
3. Fill in required fields:
   - **Username** (required, unique)
   - **Email** (required, unique)
   - **First Name** / **Last Name** (optional but recommended)
   - **Phone** (optional)
4. Select a **Role** (determines permissions)
5. Select **Default Location** (optional)
6. A **temporary password** is automatically generated
7. Click **"Create User"**
8. **Share the temporary password with the user securely**

**The new user will be forced to change their password on first login.**

### Editing an Existing User

1. Navigate to **Administration** → **Users** tab
2. Double-click the user or select and click **"Edit"**
3. Modify fields as needed
4. Click **"Save Changes"**

### Resetting a User's Password

1. Navigate to **Administration** → **Users** tab
2. Select the user
3. Click **"Reset Password"**
4. A new temporary password is generated
5. Share this password with the user securely
6. User must change password on next login

### Deactivating a User

1. Navigate to **Administration** → **Users** tab
2. Edit the user
3. Uncheck **"Account is active"**
4. Click **"Save Changes"**

Deactivated users cannot log in but their data is preserved.

---

## Roles & Permissions

### Default Roles

| Role | Description | Key Permissions |
|------|-------------|-----------------|
| **System Administrator** | Full system access | All permissions |
| **Operations Manager** | Manage operations | Equipment, manifests, locations, reports |
| **Store Keeper** | Day-to-day operations | Equipment view/edit, manifest create, reports |
| **Field Technician** | Mobile/field access | Equipment view, manifest participate |
| **Viewer** | Read-only access | View equipment, manifests, reports |

### Creating Custom Roles

1. Navigate to **Administration** → **Roles** tab
2. Click **"Add Role"**
3. Enter role name and description
4. Assign permissions from available list
5. Click **"Create Role"**

---

## Security Settings

### Password Expiry

Default: **90 days**

To change for a user:
1. Edit user account
2. Set **"Password Expires"** value (0 = never expires)

### Account Lockout

After **5 failed login attempts**, the account is temporarily locked for **15 minutes**.

The admin can unlock an account:
1. Navigate to **Administration** → **Users** tab
2. Select the locked user
3. Click **"Unlock Account"**

---

## Recommended First-Time Setup Steps

### Step 1: Change Admin Password
1. Log in with `admin` / `Admin@123!`
2. Change to a secure password you will remember
3. Document this password securely

### Step 2: Create Your Personal Admin Account
1. Navigate to Administration → Users
2. Create a new user with your details
3. Assign **System Administrator** role
4. Log out and log in with your new account

### Step 3: Disable or Rename Default Admin (Optional)
1. Edit the `admin` account
2. Either deactivate it or change the username
3. This prevents unauthorized access attempts

### Step 4: Create User Accounts
1. Create accounts for all users who need access
2. Assign appropriate roles
3. Distribute temporary passwords securely (in person or encrypted email)

### Step 5: Configure Locations
1. Navigate to Administration → Locations
2. Set up your organizational locations (bases, vessels, warehouses)
3. Assign users to their default locations

---

## Troubleshooting

### "Invalid username or password"
- Verify username spelling (case-insensitive)
- Verify password (case-sensitive)
- Check if account is locked or deactivated

### "Account is locked"
- Wait 15 minutes, or
- Contact administrator to unlock

### "Must change password"
- This is required for security
- Cannot be bypassed
- Choose a password meeting all requirements

### "Password expired"
- Your password has exceeded the expiry period
- Change to a new password to continue

### User Cannot Access Feature
- Verify user's role has required permission
- Check if user's account is active
- Verify user has access to the required location

---

## API Authentication (For Developers)

The system supports JWT token authentication for API access:

```
POST /api/auth/login
{
    "username": "admin",
    "password": "Admin@123!"
}

Response:
{
    "accessToken": "eyJ...",
    "refreshToken": "abc...",
    "expiresIn": 3600,
    "user": { ... }
}
```

Use the `accessToken` in the Authorization header:
```
Authorization: Bearer eyJ...
```

---

## Contact & Support

For technical support or questions about user management, contact your system administrator or S7 Solutions support team.

**Document Version:** 1.0  
**Last Updated:** January 2025
