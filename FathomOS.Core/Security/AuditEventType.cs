// FathomOS.Core/Security/AuditEventType.cs
// SECURITY FIX: MISSING-006 - Security Audit Event Type Enumeration
// Defines the types of security-relevant events that can be logged

namespace FathomOS.Core.Security;

/// <summary>
/// Enumeration of security-relevant event types for audit logging.
/// These events are logged with HMAC signatures for tamper detection.
/// </summary>
public enum AuditEventType
{
    // ==========================================
    // License Events (1000-1099)
    // ==========================================

    /// <summary>
    /// License validation was attempted and succeeded.
    /// </summary>
    LicenseValidationSuccess = 1000,

    /// <summary>
    /// License validation was attempted and failed.
    /// </summary>
    LicenseValidationFailure = 1001,

    /// <summary>
    /// License was activated on this machine.
    /// </summary>
    LicenseActivation = 1002,

    /// <summary>
    /// License was deactivated on this machine.
    /// </summary>
    LicenseDeactivation = 1003,

    /// <summary>
    /// License expiration warning triggered.
    /// </summary>
    LicenseExpirationWarning = 1004,

    /// <summary>
    /// License has expired.
    /// </summary>
    LicenseExpired = 1005,

    /// <summary>
    /// License key format is invalid.
    /// </summary>
    LicenseInvalidFormat = 1006,

    /// <summary>
    /// License check was performed offline.
    /// </summary>
    LicenseOfflineCheck = 1007,

    // ==========================================
    // Authentication Events (2000-2099)
    // ==========================================

    /// <summary>
    /// User login was successful.
    /// </summary>
    LoginSuccess = 2000,

    /// <summary>
    /// User login failed.
    /// </summary>
    LoginFailure = 2001,

    /// <summary>
    /// User logged out.
    /// </summary>
    Logout = 2002,

    /// <summary>
    /// User session timed out.
    /// </summary>
    SessionTimeout = 2003,

    /// <summary>
    /// User session was refreshed/extended.
    /// </summary>
    SessionRefresh = 2004,

    /// <summary>
    /// Password was changed.
    /// </summary>
    PasswordChange = 2005,

    /// <summary>
    /// Password reset was requested.
    /// </summary>
    PasswordResetRequest = 2006,

    /// <summary>
    /// Password reset was completed.
    /// </summary>
    PasswordResetComplete = 2007,

    /// <summary>
    /// Account was locked due to failed login attempts.
    /// </summary>
    AccountLocked = 2008,

    /// <summary>
    /// Account was unlocked.
    /// </summary>
    AccountUnlocked = 2009,

    /// <summary>
    /// Token refresh was successful.
    /// </summary>
    TokenRefreshSuccess = 2010,

    /// <summary>
    /// Token refresh failed.
    /// </summary>
    TokenRefreshFailure = 2011,

    /// <summary>
    /// PIN login was attempted.
    /// </summary>
    PinLogin = 2012,

    // ==========================================
    // Certificate Events (3000-3099)
    // ==========================================

    /// <summary>
    /// Certificate was created.
    /// </summary>
    CertificateCreated = 3000,

    /// <summary>
    /// Certificate was updated.
    /// </summary>
    CertificateUpdated = 3001,

    /// <summary>
    /// Certificate was deleted.
    /// </summary>
    CertificateDeleted = 3002,

    /// <summary>
    /// Certificate was approved.
    /// </summary>
    CertificateApproved = 3003,

    /// <summary>
    /// Certificate approval was rejected.
    /// </summary>
    CertificateRejected = 3004,

    /// <summary>
    /// Certificate was revoked.
    /// </summary>
    CertificateRevoked = 3005,

    /// <summary>
    /// Certificate was exported (PDF generated).
    /// </summary>
    CertificateExported = 3006,

    /// <summary>
    /// Certificate was digitally signed.
    /// </summary>
    CertificateSigned = 3007,

    /// <summary>
    /// Certificate signature was verified.
    /// </summary>
    CertificateSignatureVerified = 3008,

    /// <summary>
    /// Certificate was synced to/from server.
    /// </summary>
    CertificateSynced = 3009,

    /// <summary>
    /// Certificate access was attempted.
    /// </summary>
    CertificateAccessed = 3010,

    // ==========================================
    // Access Control Events (4000-4099)
    // ==========================================

    /// <summary>
    /// Access to a resource was granted.
    /// </summary>
    AccessGranted = 4000,

    /// <summary>
    /// Access to a resource was denied.
    /// </summary>
    AccessDenied = 4001,

    /// <summary>
    /// Permission check was performed.
    /// </summary>
    PermissionCheck = 4002,

    /// <summary>
    /// Role was assigned to a user.
    /// </summary>
    RoleAssigned = 4003,

    /// <summary>
    /// Role was removed from a user.
    /// </summary>
    RoleRemoved = 4004,

    /// <summary>
    /// Elevated privileges were requested.
    /// </summary>
    ElevationRequest = 4005,

    /// <summary>
    /// Module access was granted.
    /// </summary>
    ModuleAccessGranted = 4006,

    /// <summary>
    /// Module access was denied.
    /// </summary>
    ModuleAccessDenied = 4007,

    // ==========================================
    // Configuration Events (5000-5099)
    // ==========================================

    /// <summary>
    /// Application setting was changed.
    /// </summary>
    SettingChanged = 5000,

    /// <summary>
    /// Security setting was changed.
    /// </summary>
    SecuritySettingChanged = 5001,

    /// <summary>
    /// Database encryption key was rotated.
    /// </summary>
    EncryptionKeyRotated = 5002,

    /// <summary>
    /// Audit log settings were changed.
    /// </summary>
    AuditSettingChanged = 5003,

    /// <summary>
    /// Module configuration was changed.
    /// </summary>
    ModuleConfigChanged = 5004,

    /// <summary>
    /// User preferences were changed.
    /// </summary>
    UserPreferencesChanged = 5005,

    // ==========================================
    // System Events (6000-6099)
    // ==========================================

    /// <summary>
    /// Application started.
    /// </summary>
    ApplicationStartup = 6000,

    /// <summary>
    /// Application shut down.
    /// </summary>
    ApplicationShutdown = 6001,

    /// <summary>
    /// Application crashed or encountered a critical error.
    /// </summary>
    ApplicationCrash = 6002,

    /// <summary>
    /// Database migration was performed.
    /// </summary>
    DatabaseMigration = 6003,

    /// <summary>
    /// Assembly integrity verification was performed.
    /// </summary>
    IntegrityCheck = 6004,

    /// <summary>
    /// Assembly integrity check failed (possible tampering).
    /// </summary>
    IntegrityCheckFailure = 6005,

    /// <summary>
    /// Audit log rotation occurred.
    /// </summary>
    AuditLogRotation = 6006,

    /// <summary>
    /// Audit log integrity verification was performed.
    /// </summary>
    AuditLogVerification = 6007,

    /// <summary>
    /// Audit log tampering was detected.
    /// </summary>
    AuditLogTamperingDetected = 6008,

    // ==========================================
    // Data Events (7000-7099)
    // ==========================================

    /// <summary>
    /// Sensitive data was exported.
    /// </summary>
    DataExported = 7000,

    /// <summary>
    /// Sensitive data was imported.
    /// </summary>
    DataImported = 7001,

    /// <summary>
    /// Data backup was created.
    /// </summary>
    DataBackup = 7002,

    /// <summary>
    /// Data was restored from backup.
    /// </summary>
    DataRestore = 7003,

    /// <summary>
    /// Sensitive data was accessed.
    /// </summary>
    SensitiveDataAccess = 7004,

    // ==========================================
    // Generic/Custom Events (9000-9999)
    // ==========================================

    /// <summary>
    /// Generic security event not covered by other types.
    /// </summary>
    GenericSecurityEvent = 9000,

    /// <summary>
    /// Custom event defined by modules.
    /// </summary>
    CustomEvent = 9999
}
