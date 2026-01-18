using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace FathomOS.Shell.Services;

/// <summary>
/// Interface for local user management service.
/// </summary>
public interface ILocalUserService
{
    /// <summary>
    /// Checks if any users exist in the local database.
    /// </summary>
    bool HasUsers();

    /// <summary>
    /// Creates a new local user.
    /// </summary>
    LocalUser CreateUser(CreateUserRequest request);

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    LocalUser? Authenticate(string username, string password);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    LocalUser? GetUserById(string userId);

    /// <summary>
    /// Gets a user by their username.
    /// </summary>
    LocalUser? GetUserByUsername(string username);

    /// <summary>
    /// Gets a user by their email.
    /// </summary>
    LocalUser? GetUserByEmail(string email);

    /// <summary>
    /// Gets all local users.
    /// </summary>
    List<LocalUser> GetAllUsers();

    /// <summary>
    /// Updates a user's information.
    /// </summary>
    void UpdateUser(LocalUser user);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    bool ChangePassword(string userId, string oldPassword, string newPassword);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    bool DeleteUser(string userId);

    /// <summary>
    /// Records a login for the user.
    /// </summary>
    void RecordLogin(string userId);
}

/// <summary>
/// Local user model for SQLite storage.
/// </summary>
public class LocalUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public int LoginCount { get; set; }
    public string? AvatarUrl { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the display name (full name or username).
    /// </summary>
    public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : Username;

    /// <summary>
    /// Gets whether this user is an administrator.
    /// </summary>
    public bool IsAdministrator => Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                                   Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Request model for creating a new user.
/// </summary>
public class CreateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

/// <summary>
/// SQLite-based local user service for FathomOS.
/// Stores users locally with PBKDF2-hashed passwords.
/// </summary>
public class LocalUserService : ILocalUserService
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private readonly object _lock = new();

    // PBKDF2 settings
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    /// <summary>
    /// Creates a new LocalUserService with the default database path.
    /// </summary>
    public LocalUserService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDir = Path.Combine(appDataPath, "FathomOS", "Data");
        Directory.CreateDirectory(dataDir);

        _dbPath = Path.Combine(dataDir, "users.db");
        _connectionString = $"Data Source={_dbPath}";

        InitializeDatabase();
    }

    /// <summary>
    /// Creates a new LocalUserService with a custom database path.
    /// </summary>
    /// <param name="dbPath">Full path to the SQLite database file</param>
    public LocalUserService(string dbPath)
    {
        _dbPath = dbPath ?? throw new ArgumentNullException(nameof(dbPath));

        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _connectionString = $"Data Source={_dbPath}";

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id TEXT PRIMARY KEY,
                    Username TEXT UNIQUE NOT NULL,
                    Email TEXT UNIQUE NOT NULL,
                    FullName TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL DEFAULT 'User',
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    LastLoginAt TEXT,
                    LoginCount INTEGER NOT NULL DEFAULT 0,
                    AvatarUrl TEXT,
                    Metadata TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
                CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
            ";

            using var command = new SqliteCommand(createTableSql, connection);
            command.ExecuteNonQuery();
        }
    }

    public bool HasUsers()
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand("SELECT COUNT(*) FROM Users", connection);
            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
    }

    public LocalUser CreateUser(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            throw new ArgumentException("Username is required");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required");

        var user = new LocalUser
        {
            Id = Guid.NewGuid().ToString(),
            Username = request.Username.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LoginCount = 0
        };

        var passwordHash = HashPassword(request.Password);

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = @"
                INSERT INTO Users (Id, Username, Email, FullName, PasswordHash, Role, IsActive, CreatedAt, LoginCount, Metadata)
                VALUES (@Id, @Username, @Email, @FullName, @PasswordHash, @Role, @IsActive, @CreatedAt, @LoginCount, @Metadata)
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
            command.Parameters.AddWithValue("@Role", user.Role);
            command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("@LoginCount", user.LoginCount);
            command.Parameters.AddWithValue("@Metadata", JsonSerializer.Serialize(user.Metadata));

            command.ExecuteNonQuery();
        }

        System.Diagnostics.Debug.WriteLine($"LocalUserService: Created user '{user.Username}' with role '{user.Role}'");
        return user;
    }

    public LocalUser? Authenticate(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT * FROM Users WHERE (Username = @Username OR Email = @Username) AND IsActive = 1";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username.Trim().ToLowerInvariant());

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var storedHash = reader.GetString(reader.GetOrdinal("PasswordHash"));

                if (VerifyPassword(password, storedHash))
                {
                    var user = ReadUser(reader);

                    // Record login
                    RecordLoginInternal(connection, user.Id);
                    user.LastLoginAt = DateTime.UtcNow;
                    user.LoginCount++;

                    System.Diagnostics.Debug.WriteLine($"LocalUserService: Authenticated user '{user.Username}'");
                    return user;
                }
            }
        }

        System.Diagnostics.Debug.WriteLine($"LocalUserService: Authentication failed for '{username}'");
        return null;
    }

    public LocalUser? GetUserById(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand("SELECT * FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", userId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadUser(reader);
            }
        }

        return null;
    }

    public LocalUser? GetUserByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand("SELECT * FROM Users WHERE Username = @Username", connection);
            command.Parameters.AddWithValue("@Username", username.Trim().ToLowerInvariant());

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadUser(reader);
            }
        }

        return null;
    }

    public LocalUser? GetUserByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand("SELECT * FROM Users WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email.Trim().ToLowerInvariant());

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return ReadUser(reader);
            }
        }

        return null;
    }

    public List<LocalUser> GetAllUsers()
    {
        var users = new List<LocalUser>();

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand("SELECT * FROM Users ORDER BY CreatedAt DESC", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                users.Add(ReadUser(reader));
            }
        }

        return users;
    }

    public void UpdateUser(LocalUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE Users
                SET FullName = @FullName,
                    Email = @Email,
                    Role = @Role,
                    IsActive = @IsActive,
                    AvatarUrl = @AvatarUrl,
                    Metadata = @Metadata
                WHERE Id = @Id
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@FullName", user.FullName);
            command.Parameters.AddWithValue("@Email", user.Email.ToLowerInvariant());
            command.Parameters.AddWithValue("@Role", user.Role);
            command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@AvatarUrl", (object?)user.AvatarUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@Metadata", JsonSerializer.Serialize(user.Metadata));

            command.ExecuteNonQuery();
        }
    }

    public bool ChangePassword(string userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(oldPassword) ||
            string.IsNullOrWhiteSpace(newPassword))
            return false;

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Verify old password
            using (var checkCommand = new SqliteCommand("SELECT PasswordHash FROM Users WHERE Id = @Id", connection))
            {
                checkCommand.Parameters.AddWithValue("@Id", userId);
                var storedHash = checkCommand.ExecuteScalar() as string;

                if (storedHash == null || !VerifyPassword(oldPassword, storedHash))
                {
                    return false;
                }
            }

            // Update password
            var newHash = HashPassword(newPassword);
            using var updateCommand = new SqliteCommand("UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @Id", connection);
            updateCommand.Parameters.AddWithValue("@Id", userId);
            updateCommand.Parameters.AddWithValue("@PasswordHash", newHash);
            updateCommand.ExecuteNonQuery();

            return true;
        }
    }

    public bool DeleteUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = new SqliteCommand("DELETE FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", userId);

            return command.ExecuteNonQuery() > 0;
        }
    }

    public void RecordLogin(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            RecordLoginInternal(connection, userId);
        }
    }

    private void RecordLoginInternal(SqliteConnection connection, string userId)
    {
        var sql = "UPDATE Users SET LastLoginAt = @LastLoginAt, LoginCount = LoginCount + 1 WHERE Id = @Id";
        using var command = new SqliteCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", userId);
        command.Parameters.AddWithValue("@LastLoginAt", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }

    private LocalUser ReadUser(SqliteDataReader reader)
    {
        var user = new LocalUser
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
            LoginCount = reader.GetInt32(reader.GetOrdinal("LoginCount"))
        };

        var lastLoginOrdinal = reader.GetOrdinal("LastLoginAt");
        if (!reader.IsDBNull(lastLoginOrdinal))
        {
            user.LastLoginAt = DateTime.Parse(reader.GetString(lastLoginOrdinal));
        }

        var avatarOrdinal = reader.GetOrdinal("AvatarUrl");
        if (!reader.IsDBNull(avatarOrdinal))
        {
            user.AvatarUrl = reader.GetString(avatarOrdinal);
        }

        var metadataOrdinal = reader.GetOrdinal("Metadata");
        if (!reader.IsDBNull(metadataOrdinal))
        {
            var metadataJson = reader.GetString(metadataOrdinal);
            user.Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson)
                ?? new Dictionary<string, string>();
        }

        return user;
    }

    #region Password Hashing (PBKDF2)

    private static string HashPassword(string password)
    {
        // Generate salt
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash password
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        // Combine salt and hash
        var combined = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, combined, 0, SaltSize);
        Array.Copy(hash, 0, combined, SaltSize, HashSize);

        return Convert.ToBase64String(combined);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var combined = Convert.FromBase64String(storedHash);

            if (combined.Length != SaltSize + HashSize)
                return false;

            // Extract salt
            var salt = new byte[SaltSize];
            Array.Copy(combined, 0, salt, 0, SaltSize);

            // Extract stored hash
            var storedPasswordHash = new byte[HashSize];
            Array.Copy(combined, SaltSize, storedPasswordHash, 0, HashSize);

            // Compute hash of provided password
            var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize);

            // Compare hashes
            return CryptographicOperations.FixedTimeEquals(computedHash, storedPasswordHash);
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
