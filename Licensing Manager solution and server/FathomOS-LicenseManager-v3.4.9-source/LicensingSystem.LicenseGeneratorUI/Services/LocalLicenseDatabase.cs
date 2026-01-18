using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using LicensingSystem.Shared;

namespace LicenseGeneratorUI.Services;

/// <summary>
/// SQLite database for storing created licenses locally.
/// Enables standalone mode with optional server sync.
/// </summary>
public class LocalLicenseDatabase : IDisposable
{
    private readonly string _databasePath;
    private SQLiteConnection? _connection;
    private bool _disposed;

    public LocalLicenseDatabase()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FathomOSLicenseManager");

        Directory.CreateDirectory(appDataPath);
        _databasePath = Path.Combine(appDataPath, "licenses.db");

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        _connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;");
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
    }

    /// <summary>
    /// Save a created license to the local database
    /// </summary>
    public void SaveLicense(LicenseFile license, SignedLicense? signedLicense = null, string? licenseKey = null)
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
        cmd.Parameters.AddWithValue("@CustomerName", license.CustomerName);
        cmd.Parameters.AddWithValue("@CustomerEmail", license.CustomerEmail);
        cmd.Parameters.AddWithValue("@Edition", license.Edition);
        cmd.Parameters.AddWithValue("@Tier", (object?)GetTierFromFeatures(license.Features) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SubscriptionType", license.SubscriptionType.ToString());
        cmd.Parameters.AddWithValue("@Brand", (object?)license.Brand ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LicenseeCode", (object?)license.LicenseeCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SupportCode", (object?)license.SupportCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@HardwareId", (object?)license.HardwareFingerprints.FirstOrDefault() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Features", JsonSerializer.Serialize(license.Features));
        cmd.Parameters.AddWithValue("@IssuedAt", license.IssuedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@ExpiresAt", license.ExpiresAt.ToString("o"));
        cmd.Parameters.AddWithValue("@LicenseFileJson", JsonSerializer.Serialize(license));
        cmd.Parameters.AddWithValue("@SignedLicenseJson", signedLicense != null ? JsonSerializer.Serialize(signedLicense) : DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Get all licenses from the local database
    /// </summary>
    public List<LocalLicenseRecord> GetAllLicenses()
    {
        EnsureConnection();

        var licenses = new List<LocalLicenseRecord>();
        using var cmd = new SQLiteCommand("SELECT * FROM Licenses ORDER BY CreatedAt DESC", _connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            licenses.Add(ReadLicenseRecord(reader));
        }

        return licenses;
    }

    /// <summary>
    /// Search licenses by query (searches customer name, email, licensee code)
    /// </summary>
    public List<LocalLicenseRecord> Search(string query)
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
            licenses.Add(ReadLicenseRecord(reader));
        }

        return licenses;
    }

    /// <summary>
    /// Get a license by its ID
    /// </summary>
    public LocalLicenseRecord? GetById(string licenseId)
    {
        EnsureConnection();

        using var cmd = new SQLiteCommand("SELECT * FROM Licenses WHERE LicenseId = @LicenseId", _connection);
        cmd.Parameters.AddWithValue("@LicenseId", licenseId);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return ReadLicenseRecord(reader);
        }

        return null;
    }

    /// <summary>
    /// Mark a license as revoked
    /// </summary>
    public void RevokeLicense(string licenseId, string reason)
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
        cmd.Parameters.AddWithValue("@Reason", reason);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Mark a license as synced to server
    /// </summary>
    public void MarkSynced(string licenseId)
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

    /// <summary>
    /// Get all licenses that haven't been synced to server
    /// </summary>
    public List<LocalLicenseRecord> GetUnsyncedLicenses()
    {
        EnsureConnection();

        var licenses = new List<LocalLicenseRecord>();
        using var cmd = new SQLiteCommand("SELECT * FROM Licenses WHERE IsSynced = 0 ORDER BY CreatedAt ASC", _connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            licenses.Add(ReadLicenseRecord(reader));
        }

        return licenses;
    }

    /// <summary>
    /// Get license statistics
    /// </summary>
    public LicenseStatistics GetStatistics()
    {
        EnsureConnection();

        var stats = new LicenseStatistics();

        using var cmd = new SQLiteCommand(_connection);

        // Total count
        cmd.CommandText = "SELECT COUNT(*) FROM Licenses";
        stats.TotalLicenses = Convert.ToInt32(cmd.ExecuteScalar());

        // Active count (not revoked and not expired)
        cmd.CommandText = "SELECT COUNT(*) FROM Licenses WHERE IsRevoked = 0 AND ExpiresAt > @Now";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("o"));
        stats.ActiveLicenses = Convert.ToInt32(cmd.ExecuteScalar());

        // Expiring soon (within 30 days)
        cmd.CommandText = "SELECT COUNT(*) FROM Licenses WHERE IsRevoked = 0 AND ExpiresAt > @Now AND ExpiresAt < @Soon";
        cmd.Parameters.Clear();
        cmd.Parameters.AddWithValue("@Now", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("@Soon", DateTime.UtcNow.AddDays(30).ToString("o"));
        stats.ExpiringSoon = Convert.ToInt32(cmd.ExecuteScalar());

        // Revoked count
        cmd.CommandText = "SELECT COUNT(*) FROM Licenses WHERE IsRevoked = 1";
        cmd.Parameters.Clear();
        stats.RevokedLicenses = Convert.ToInt32(cmd.ExecuteScalar());

        // Unsynced count
        cmd.CommandText = "SELECT COUNT(*) FROM Licenses WHERE IsSynced = 0";
        cmd.Parameters.Clear();
        stats.UnsyncedLicenses = Convert.ToInt32(cmd.ExecuteScalar());

        return stats;
    }

    /// <summary>
    /// Delete a license from the local database
    /// </summary>
    public void DeleteLicense(string licenseId)
    {
        EnsureConnection();

        using var cmd = new SQLiteCommand("DELETE FROM Licenses WHERE LicenseId = @LicenseId", _connection);
        cmd.Parameters.AddWithValue("@LicenseId", licenseId);
        cmd.ExecuteNonQuery();
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

        // Optional fields
        var ordinal = reader.GetOrdinal("LicenseKey");
        if (!reader.IsDBNull(ordinal)) record.LicenseKey = reader.GetString(ordinal);

        ordinal = reader.GetOrdinal("Tier");
        if (!reader.IsDBNull(ordinal)) record.Tier = reader.GetString(ordinal);

        ordinal = reader.GetOrdinal("SubscriptionType");
        if (!reader.IsDBNull(ordinal)) record.SubscriptionType = reader.GetString(ordinal);

        ordinal = reader.GetOrdinal("Brand");
        if (!reader.IsDBNull(ordinal)) record.Brand = reader.GetString(ordinal);

        ordinal = reader.GetOrdinal("LicenseeCode");
        if (!reader.IsDBNull(ordinal)) record.LicenseeCode = reader.GetString(ordinal);

        ordinal = reader.GetOrdinal("SupportCode");
        if (!reader.IsDBNull(ordinal)) record.SupportCode = reader.GetString(ordinal);

        ordinal = reader.GetOrdinal("HardwareId");
        if (!reader.IsDBNull(ordinal)) record.HardwareId = reader.GetString(ordinal);

        ordinal = reader.GetOrdinal("Features");
        if (!reader.IsDBNull(ordinal))
        {
            var featuresJson = reader.GetString(ordinal);
            record.Features = JsonSerializer.Deserialize<List<string>>(featuresJson) ?? new List<string>();
        }

        ordinal = reader.GetOrdinal("RevokedAt");
        if (!reader.IsDBNull(ordinal)) record.RevokedAt = DateTime.Parse(reader.GetString(ordinal));

        ordinal = reader.GetOrdinal("RevokeReason");
        if (!reader.IsDBNull(ordinal)) record.RevokeReason = reader.GetString(ordinal);

        ordinal = reader.GetOrdinal("SyncedAt");
        if (!reader.IsDBNull(ordinal)) record.SyncedAt = DateTime.Parse(reader.GetString(ordinal));

        ordinal = reader.GetOrdinal("LicenseFileJson");
        if (!reader.IsDBNull(ordinal))
        {
            var json = reader.GetString(ordinal);
            record.LicenseFile = JsonSerializer.Deserialize<LicenseFile>(json);
        }

        ordinal = reader.GetOrdinal("SignedLicenseJson");
        if (!reader.IsDBNull(ordinal))
        {
            var json = reader.GetString(ordinal);
            record.SignedLicense = JsonSerializer.Deserialize<SignedLicense>(json);
        }

        return record;
    }

    private static string? GetTierFromFeatures(List<string> features)
    {
        var tierFeature = features.FirstOrDefault(f => f.StartsWith("Tier:", StringComparison.OrdinalIgnoreCase));
        return tierFeature?.Substring(5);
    }

    private void EnsureConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;");
            _connection.Open();
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
                _connection?.Close();
                _connection?.Dispose();
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
