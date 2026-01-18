using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using LicensingSystem.Shared;

namespace LicenseGeneratorUI.Services;

/// <summary>
/// SQLite database for storing created licenses locally.
/// Enables standalone mode with optional server sync.
/// Thread-safe with connection timeout support.
/// </summary>
public class LocalLicenseDatabase : IDisposable
{
    private readonly string _databasePath;
    private SQLiteConnection? _connection;
    private bool _disposed;
    private static readonly object _connectionLock = new object();

    public LocalLicenseDatabase()
    {
        var localAppData = GetLocalAppDataPath();
        var appDataPath = Path.Combine(localAppData, "FathomOSLicenseManager");

        try
        {
            Directory.CreateDirectory(appDataPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Failed to create directory {appDataPath}: {ex.Message}");
            // Fallback to app directory
            appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", "LicenseData");
            Directory.CreateDirectory(appDataPath);
        }

        _databasePath = Path.Combine(appDataPath, "licenses.db");
        System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Using database path: {_databasePath}");

        InitializeDatabase();
    }

    /// <summary>
    /// Gets the local app data path with robust fallback logic
    /// </summary>
    private static string GetLocalAppDataPath()
    {
        // Try LocalApplicationData first
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localAppData))
        {
            System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Using LocalApplicationData: {localAppData}");
            return localAppData;
        }

        // Fallback to UserProfile
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            var fallbackPath = Path.Combine(userProfile, ".FathomOS");
            System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Using UserProfile fallback: {fallbackPath}");
            return fallbackPath;
        }

        // Ultimate fallback: use application directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = Directory.GetCurrentDirectory();
        }
        if (string.IsNullOrEmpty(baseDir))
        {
            baseDir = ".";
        }

        var appDataPath = Path.Combine(baseDir, "AppData");
        System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Using application directory fallback: {appDataPath}");
        return appDataPath;
    }

    private void InitializeDatabase()
    {
        lock (_connectionLock)
        {
            try
            {
                // Connection string with timeout to avoid hanging on locked database
                var connectionString = $"Data Source={_databasePath};Version=3;BusyTimeout=5000;Journal Mode=WAL;";
                _connection = new SQLiteConnection(connectionString);
                _connection.Open();

                // Create tables if they don't exist
                using var cmd = new SQLiteCommand(_connection);
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Licenses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        LicenseId TEXT UNIQUE NOT NULL,
                        LicenseKey TEXT,
                        CustomerName TEXT NOT NULL,
                        CustomerEmail TEXT NOT NULL,
                        Edition TEXT NOT NULL,
                        Tier TEXT,
                        SubscriptionType TEXT,
                        Brand TEXT,
                        LicenseeCode TEXT,
                        SupportCode TEXT,
                        HardwareId TEXT,
                        Features TEXT,
                        IssuedAt TEXT NOT NULL,
                        ExpiresAt TEXT NOT NULL,
                        IsRevoked INTEGER DEFAULT 0,
                        RevokedAt TEXT,
                        RevokeReason TEXT,
                        IsSynced INTEGER DEFAULT 0,
                        SyncedAt TEXT,
                        LicenseFileJson TEXT,
                        SignedLicenseJson TEXT,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    );

                    CREATE INDEX IF NOT EXISTS idx_licenses_customer_email ON Licenses(CustomerEmail);
                    CREATE INDEX IF NOT EXISTS idx_licenses_licensee_code ON Licenses(LicenseeCode);
                    CREATE INDEX IF NOT EXISTS idx_licenses_is_synced ON Licenses(IsSynced);
                    CREATE INDEX IF NOT EXISTS idx_licenses_is_revoked ON Licenses(IsRevoked);
                ";
                cmd.ExecuteNonQuery();

                System.Diagnostics.Debug.WriteLine("LocalLicenseDatabase: Database initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Failed to initialize database: {ex.Message}");
                // Clean up on failure
                _connection?.Dispose();
                _connection = null;
                throw new InvalidOperationException($"Failed to initialize license database: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Save a created license to the local database
    /// </summary>
    public void SaveLicense(LicenseFile license, SignedLicense? signedLicense = null, string? licenseKey = null)
    {
        if (license == null)
            throw new ArgumentNullException(nameof(license));

        lock (_connectionLock)
        {
            EnsureConnection();

            using var cmd = new SQLiteCommand(_connection);
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Licenses (
                    LicenseId, LicenseKey, CustomerName, CustomerEmail, Edition, Tier,
                    SubscriptionType, Brand, LicenseeCode, SupportCode, HardwareId, Features,
                    IssuedAt, ExpiresAt, LicenseFileJson, SignedLicenseJson, CreatedAt, UpdatedAt
                ) VALUES (
                    @LicenseId, @LicenseKey, @CustomerName, @CustomerEmail, @Edition, @Tier,
                    @SubscriptionType, @Brand, @LicenseeCode, @SupportCode, @HardwareId, @Features,
                    @IssuedAt, @ExpiresAt, @LicenseFileJson, @SignedLicenseJson, @CreatedAt, @UpdatedAt
                )";

            cmd.Parameters.AddWithValue("@LicenseId", license.LicenseId);
            cmd.Parameters.AddWithValue("@LicenseKey", (object?)licenseKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CustomerName", license.CustomerName ?? string.Empty);
            cmd.Parameters.AddWithValue("@CustomerEmail", license.CustomerEmail ?? string.Empty);
            cmd.Parameters.AddWithValue("@Edition", license.Edition ?? string.Empty);
            cmd.Parameters.AddWithValue("@Tier", (object?)GetTierFromFeatures(license.Features) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SubscriptionType", license.SubscriptionType.ToString());
            cmd.Parameters.AddWithValue("@Brand", (object?)license.Brand ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LicenseeCode", (object?)license.LicenseeCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@SupportCode", (object?)license.SupportCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HardwareId", (object?)license.HardwareFingerprints?.FirstOrDefault() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Features", JsonSerializer.Serialize(license.Features ?? new List<string>()));
            cmd.Parameters.AddWithValue("@IssuedAt", license.IssuedAt.ToString("o"));
            cmd.Parameters.AddWithValue("@ExpiresAt", license.ExpiresAt.ToString("o"));
            cmd.Parameters.AddWithValue("@LicenseFileJson", JsonSerializer.Serialize(license));
            cmd.Parameters.AddWithValue("@SignedLicenseJson", signedLicense != null ? JsonSerializer.Serialize(signedLicense) : DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

            cmd.ExecuteNonQuery();
            System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Saved license {license.LicenseId}");
        }
    }

    /// <summary>
    /// Get all licenses from the local database
    /// </summary>
    public List<LocalLicenseRecord> GetAllLicenses()
    {
        lock (_connectionLock)
        {
            EnsureConnection();

            var licenses = new List<LocalLicenseRecord>();
            using var cmd = new SQLiteCommand("SELECT * FROM Licenses ORDER BY CreatedAt DESC", _connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                try
                {
                    licenses.Add(ReadLicenseRecord(reader));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Error reading license record: {ex.Message}");
                }
            }

            return licenses;
        }
    }

    /// <summary>
    /// Search licenses by query (searches customer name, email, licensee code)
    /// </summary>
    public List<LocalLicenseRecord> Search(string query)
    {
        if (string.IsNullOrEmpty(query))
            return GetAllLicenses();

        lock (_connectionLock)
        {
            EnsureConnection();

            var licenses = new List<LocalLicenseRecord>();
            using var cmd = new SQLiteCommand(_connection);
            cmd.CommandText = @"
                SELECT * FROM Licenses
                WHERE CustomerName LIKE @Query
                   OR CustomerEmail LIKE @Query
                   OR LicenseeCode LIKE @Query
                   OR LicenseId LIKE @Query
                   OR SupportCode LIKE @Query
                ORDER BY CreatedAt DESC";
            cmd.Parameters.AddWithValue("@Query", $"%{query}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    licenses.Add(ReadLicenseRecord(reader));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Error reading license record in search: {ex.Message}");
                }
            }

            return licenses;
        }
    }

    /// <summary>
    /// Get a license by its ID
    /// </summary>
    public LocalLicenseRecord? GetById(string licenseId)
    {
        if (string.IsNullOrEmpty(licenseId))
            return null;

        lock (_connectionLock)
        {
            EnsureConnection();

            using var cmd = new SQLiteCommand("SELECT * FROM Licenses WHERE LicenseId = @LicenseId", _connection);
            cmd.Parameters.AddWithValue("@LicenseId", licenseId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                try
                {
                    return ReadLicenseRecord(reader);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Error reading license {licenseId}: {ex.Message}");
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Mark a license as revoked
    /// </summary>
    public void RevokeLicense(string licenseId, string reason)
    {
        if (string.IsNullOrEmpty(licenseId))
            throw new ArgumentNullException(nameof(licenseId));

        lock (_connectionLock)
        {
            EnsureConnection();

            using var cmd = new SQLiteCommand(_connection);
            cmd.CommandText = @"
                UPDATE Licenses SET
                    IsRevoked = 1,
                    RevokedAt = @RevokedAt,
                    RevokeReason = @Reason,
                    IsSynced = 0,
                    UpdatedAt = @UpdatedAt
                WHERE LicenseId = @LicenseId";

            cmd.Parameters.AddWithValue("@LicenseId", licenseId);
            cmd.Parameters.AddWithValue("@RevokedAt", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@Reason", reason ?? "Revoked");
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

            cmd.ExecuteNonQuery();
            System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Revoked license {licenseId}: {reason}");
        }
    }

    /// <summary>
    /// Mark a license as synced to server
    /// </summary>
    public void MarkSynced(string licenseId)
    {
        if (string.IsNullOrEmpty(licenseId))
            return;

        lock (_connectionLock)
        {
            EnsureConnection();

            using var cmd = new SQLiteCommand(_connection);
            cmd.CommandText = @"
                UPDATE Licenses SET
                    IsSynced = 1,
                    SyncedAt = @SyncedAt,
                    UpdatedAt = @UpdatedAt
                WHERE LicenseId = @LicenseId";

            cmd.Parameters.AddWithValue("@LicenseId", licenseId);
            cmd.Parameters.AddWithValue("@SyncedAt", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Get all licenses that haven't been synced to server
    /// </summary>
    public List<LocalLicenseRecord> GetUnsyncedLicenses()
    {
        lock (_connectionLock)
        {
            EnsureConnection();

            var licenses = new List<LocalLicenseRecord>();
            using var cmd = new SQLiteCommand("SELECT * FROM Licenses WHERE IsSynced = 0 ORDER BY CreatedAt ASC", _connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                try
                {
                    licenses.Add(ReadLicenseRecord(reader));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Error reading unsynced license: {ex.Message}");
                }
            }

            return licenses;
        }
    }

    /// <summary>
    /// Get license statistics
    /// </summary>
    public LicenseStatistics GetStatistics()
    {
        lock (_connectionLock)
        {
            EnsureConnection();

            var stats = new LicenseStatistics();

            try
            {
                using var cmd = new SQLiteCommand(_connection);

                // Total count
                cmd.CommandText = "SELECT COUNT(*) FROM Licenses";
                stats.TotalLicenses = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                // Active count (not revoked and not expired)
                cmd.CommandText = "SELECT COUNT(*) FROM Licenses WHERE IsRevoked = 0 AND ExpiresAt > @Now";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("o"));
                stats.ActiveLicenses = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                // Expiring soon (within 30 days)
                cmd.CommandText = "SELECT COUNT(*) FROM Licenses WHERE IsRevoked = 0 AND ExpiresAt > @Now AND ExpiresAt < @Soon";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("@Soon", DateTime.UtcNow.AddDays(30).ToString("o"));
                stats.ExpiringSoon = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                // Revoked count
                cmd.CommandText = "SELECT COUNT(*) FROM Licenses WHERE IsRevoked = 1";
                cmd.Parameters.Clear();
                stats.RevokedLicenses = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);

                // Unsynced count
                cmd.CommandText = "SELECT COUNT(*) FROM Licenses WHERE IsSynced = 0";
                cmd.Parameters.Clear();
                stats.UnsyncedLicenses = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Error getting statistics: {ex.Message}");
            }

            return stats;
        }
    }

    /// <summary>
    /// Delete a license from the local database
    /// </summary>
    public void DeleteLicense(string licenseId)
    {
        if (string.IsNullOrEmpty(licenseId))
            return;

        lock (_connectionLock)
        {
            EnsureConnection();

            using var cmd = new SQLiteCommand("DELETE FROM Licenses WHERE LicenseId = @LicenseId", _connection);
            cmd.Parameters.AddWithValue("@LicenseId", licenseId);
            cmd.ExecuteNonQuery();
            System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Deleted license {licenseId}");
        }
    }

    private LocalLicenseRecord ReadLicenseRecord(SQLiteDataReader reader)
    {
        var record = new LocalLicenseRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            LicenseId = reader.GetString(reader.GetOrdinal("LicenseId")),
            CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
            CustomerEmail = reader.GetString(reader.GetOrdinal("CustomerEmail")),
            Edition = reader.GetString(reader.GetOrdinal("Edition")),
            IssuedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("IssuedAt"))),
            ExpiresAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("ExpiresAt"))),
            IsRevoked = reader.GetInt32(reader.GetOrdinal("IsRevoked")) == 1,
            IsSynced = reader.GetInt32(reader.GetOrdinal("IsSynced")) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")))
        };

        // Optional fields with safe reading
        ReadOptionalString(reader, "LicenseKey", value => record.LicenseKey = value);
        ReadOptionalString(reader, "Tier", value => record.Tier = value);
        ReadOptionalString(reader, "SubscriptionType", value => record.SubscriptionType = value);
        ReadOptionalString(reader, "Brand", value => record.Brand = value);
        ReadOptionalString(reader, "LicenseeCode", value => record.LicenseeCode = value);
        ReadOptionalString(reader, "SupportCode", value => record.SupportCode = value);
        ReadOptionalString(reader, "HardwareId", value => record.HardwareId = value);
        ReadOptionalString(reader, "RevokeReason", value => record.RevokeReason = value);

        // Features JSON
        ReadOptionalString(reader, "Features", value =>
        {
            try
            {
                record.Features = JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>();
            }
            catch
            {
                record.Features = new List<string>();
            }
        });

        // DateTime fields
        ReadOptionalString(reader, "RevokedAt", value =>
        {
            if (DateTime.TryParse(value, out var dt))
                record.RevokedAt = dt;
        });

        ReadOptionalString(reader, "SyncedAt", value =>
        {
            if (DateTime.TryParse(value, out var dt))
                record.SyncedAt = dt;
        });

        // JSON objects
        ReadOptionalString(reader, "LicenseFileJson", value =>
        {
            try
            {
                record.LicenseFile = JsonSerializer.Deserialize<LicenseFile>(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Error deserializing LicenseFile: {ex.Message}");
            }
        });

        ReadOptionalString(reader, "SignedLicenseJson", value =>
        {
            try
            {
                record.SignedLicense = JsonSerializer.Deserialize<SignedLicense>(value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Error deserializing SignedLicense: {ex.Message}");
            }
        });

        return record;
    }

    private static void ReadOptionalString(SQLiteDataReader reader, string columnName, Action<string> setter)
    {
        try
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (!reader.IsDBNull(ordinal))
            {
                setter(reader.GetString(ordinal));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Error reading column {columnName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts tier from features list with bounds checking
    /// </summary>
    private static string? GetTierFromFeatures(List<string>? features)
    {
        if (features == null || features.Count == 0)
            return null;

        var tierFeature = features.FirstOrDefault(f =>
            !string.IsNullOrEmpty(f) &&
            f.StartsWith("Tier:", StringComparison.OrdinalIgnoreCase));

        if (tierFeature == null)
            return null;

        // Safe substring with bounds check
        const int prefixLength = 5; // "Tier:".Length
        if (tierFeature.Length > prefixLength)
        {
            return tierFeature.Substring(prefixLength);
        }

        return null;
    }

    private void EnsureConnection()
    {
        // Caller should already hold the lock
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            try
            {
                _connection?.Dispose();

                var connectionString = $"Data Source={_databasePath};Version=3;BusyTimeout=5000;Journal Mode=WAL;";
                _connection = new SQLiteConnection(connectionString);
                _connection.Open();

                System.Diagnostics.Debug.WriteLine("LocalLicenseDatabase: Connection reopened");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalLicenseDatabase: Failed to reopen connection: {ex.Message}");
                throw new InvalidOperationException("Failed to connect to license database", ex);
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                lock (_connectionLock)
                {
                    _connection?.Close();
                    _connection?.Dispose();
                    _connection = null;
                }
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Local license record from the database
/// </summary>
public class LocalLicenseRecord
{
    public int Id { get; set; }
    public string LicenseId { get; set; } = "";
    public string? LicenseKey { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string Edition { get; set; } = "";
    public string? Tier { get; set; }
    public string? SubscriptionType { get; set; }
    public string? Brand { get; set; }
    public string? LicenseeCode { get; set; }
    public string? SupportCode { get; set; }
    public string? HardwareId { get; set; }
    public List<string> Features { get; set; } = new();
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public bool IsSynced { get; set; }
    public DateTime? SyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public LicenseFile? LicenseFile { get; set; }
    public SignedLicense? SignedLicense { get; set; }

    /// <summary>
    /// Get the status display string
    /// </summary>
    public string Status
    {
        get
        {
            if (IsRevoked) return "Revoked";
            if (ExpiresAt < DateTime.UtcNow) return "Expired";
            return "Active";
        }
    }
}

/// <summary>
/// License statistics
/// </summary>
public class LicenseStatistics
{
    public int TotalLicenses { get; set; }
    public int ActiveLicenses { get; set; }
    public int ExpiringSoon { get; set; }
    public int RevokedLicenses { get; set; }
    public int UnsyncedLicenses { get; set; }
}
