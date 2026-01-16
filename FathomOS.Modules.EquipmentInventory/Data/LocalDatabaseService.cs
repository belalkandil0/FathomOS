using Microsoft.EntityFrameworkCore;
using FathomOS.Modules.EquipmentInventory.Models;
using System.IO;
using System.Text.Json;

namespace FathomOS.Modules.EquipmentInventory.Data;

/// <summary>
/// Local SQLite database service for offline-first operation
/// </summary>
public class LocalDatabaseService : IDisposable
{
    private LocalDatabaseContext? _context;
    private readonly object _lock = new();
    private bool _isInitialized;
    
    // Schema version for tracking migrations
    private const int CurrentSchemaVersion = 2;
    
    public LocalDatabaseContext Context
    {
        get
        {
            lock (_lock)
            {
                _context ??= new LocalDatabaseContext();
                return _context;
            }
        }
    }
    
    /// <summary>
    /// Initialize database and seed default data
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;
        
        try
        {
            // Check if database file exists
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FathomOS", "EquipmentInventory", "local.db");
            
            bool isNewDatabase = !File.Exists(dbPath);
            
            // Create database if needed
            Context.Database.EnsureCreated();
            
            // Run migrations for existing databases
            if (!isNewDatabase)
            {
                RunSchemaMigrations();
            }
            
            SeedDefaultData();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Run schema migrations to add missing columns
    /// </summary>
    private void RunSchemaMigrations()
    {
        try
        {
            var connection = Context.Database.GetDbConnection();
            connection.Open();
            
            // Get existing columns in Users table
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(Users)";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    existingColumns.Add(reader.GetString(1)); // column name is at index 1
                }
            }
            
            // Migration: Add IsSuperAdmin column
            if (!existingColumns.Contains("IsSuperAdmin"))
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "ALTER TABLE Users ADD COLUMN IsSuperAdmin INTEGER NOT NULL DEFAULT 0";
                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Migration: Added IsSuperAdmin column");
            }
            
            // Migration: Add MustChangePassword column
            if (!existingColumns.Contains("MustChangePassword"))
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "ALTER TABLE Users ADD COLUMN MustChangePassword INTEGER NOT NULL DEFAULT 1";
                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Migration: Added MustChangePassword column");
            }
            
            // Migration: Add PasswordChangedAt column
            if (!existingColumns.Contains("PasswordChangedAt"))
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "ALTER TABLE Users ADD COLUMN PasswordChangedAt TEXT";
                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Migration: Added PasswordChangedAt column");
            }
            
            // Migration: Add PasswordExpiryDays column
            if (!existingColumns.Contains("PasswordExpiryDays"))
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "ALTER TABLE Users ADD COLUMN PasswordExpiryDays INTEGER NOT NULL DEFAULT 90";
                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Migration: Added PasswordExpiryDays column");
            }
            
            // Migration: Add TemporaryPassword column
            if (!existingColumns.Contains("TemporaryPassword"))
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "ALTER TABLE Users ADD COLUMN TemporaryPassword TEXT";
                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Migration: Added TemporaryPassword column");
            }
            
            // Migration: Add PinHash column
            if (!existingColumns.Contains("PinHash"))
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "ALTER TABLE Users ADD COLUMN PinHash TEXT";
                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("Migration: Added PinHash column");
            }
            
            // Migration: Ensure admin user has IsSuperAdmin = true
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "UPDATE Users SET IsSuperAdmin = 1 WHERE Username = 'admin' AND IsSuperAdmin = 0";
                var rowsAffected = cmd.ExecuteNonQuery();
                if (rowsAffected > 0)
                {
                    System.Diagnostics.Debug.WriteLine("Migration: Updated admin user to super admin");
                }
            }
            
            connection.Close();;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Schema migration error: {ex.Message}");
            // Don't throw - allow the app to continue if migrations fail
        }
    }
    
    /// <summary>
    /// Ensure database is initialized (async wrapper)
    /// </summary>
    private async Task EnsureDatabaseAsync()
    {
        if (!_isInitialized)
        {
            await Task.Run(() => Initialize());
        }
    }
    
    #region Convenience Wrapper Methods
    
    public async Task<List<Equipment>> GetAllEquipmentAsync()
    {
        return await GetEquipmentAsync();
    }
    
    public async Task<List<Manifest>> GetAllManifestsAsync()
    {
        return await GetManifestsAsync();
    }
    
    public async Task<List<Location>> GetAllLocationsAsync()
    {
        return await GetLocationsAsync();
    }
    
    public async Task<List<EquipmentCategory>> GetAllCategoriesAsync()
    {
        return await GetCategoriesAsync();
    }
    
    public async Task<List<User>> GetAllUsersAsync()
    {
        await EnsureDatabaseAsync();
        return await _context!.Users.ToListAsync();
    }
    
    public async Task<List<Role>> GetAllRolesAsync()
    {
        await EnsureDatabaseAsync();
        return await _context!.Roles.ToListAsync();
    }
    
    #endregion
    
    #region User Location Management
    
    /// <summary>
    /// Get all users assigned to a specific location
    /// </summary>
    public async Task<List<User>> GetUsersForLocationAsync(Guid locationId)
    {
        await EnsureDatabaseAsync();
        return await _context!.UserLocations
            .Where(ul => ul.LocationId == locationId)
            .Include(ul => ul.User)
            .Select(ul => ul.User!)
            .Where(u => u != null && u.IsActive)
            .ToListAsync();
    }
    
    /// <summary>
    /// Get all locations assigned to a specific user
    /// </summary>
    public async Task<List<Location>> GetLocationsForUserAsync(Guid userId)
    {
        await EnsureDatabaseAsync();
        return await _context!.UserLocations
            .Where(ul => ul.UserId == userId)
            .Include(ul => ul.Location)
            .Select(ul => ul.Location!)
            .Where(l => l != null && l.IsActive)
            .ToListAsync();
    }
    
    /// <summary>
    /// Get UserLocation assignments for a specific location (includes access level)
    /// </summary>
    public async Task<List<UserLocation>> GetUserLocationAssignmentsAsync(Guid locationId)
    {
        await EnsureDatabaseAsync();
        return await _context!.UserLocations
            .Where(ul => ul.LocationId == locationId)
            .Include(ul => ul.User)
            .ToListAsync();
    }
    
    /// <summary>
    /// Assign a user to a location
    /// </summary>
    public async Task AssignUserToLocationAsync(Guid userId, Guid locationId, string accessLevel = "Read")
    {
        await EnsureDatabaseAsync();
        
        // Check if assignment already exists
        var existing = await _context!.UserLocations
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LocationId == locationId);
            
        if (existing != null)
        {
            // Update access level
            existing.AccessLevel = accessLevel;
        }
        else
        {
            // Create new assignment
            _context.UserLocations.Add(new UserLocation
            {
                UserId = userId,
                LocationId = locationId,
                AccessLevel = accessLevel
            });
        }
        
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Remove a user from a location
    /// </summary>
    public async Task RemoveUserFromLocationAsync(Guid userId, Guid locationId)
    {
        await EnsureDatabaseAsync();
        
        var assignment = await _context!.UserLocations
            .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LocationId == locationId);
            
        if (assignment != null)
        {
            _context.UserLocations.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Update all user assignments for a location (replace existing)
    /// </summary>
    public async Task UpdateLocationUsersAsync(Guid locationId, IEnumerable<(Guid UserId, string AccessLevel)> assignments)
    {
        await EnsureDatabaseAsync();
        
        // Remove existing assignments
        var existing = await _context!.UserLocations
            .Where(ul => ul.LocationId == locationId)
            .ToListAsync();
        _context.UserLocations.RemoveRange(existing);
        
        // Add new assignments
        foreach (var (userId, accessLevel) in assignments)
        {
            _context.UserLocations.Add(new UserLocation
            {
                UserId = userId,
                LocationId = locationId,
                AccessLevel = accessLevel
            });
        }
        
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Check if user has access to a location
    /// </summary>
    public async Task<bool> UserHasLocationAccessAsync(Guid userId, Guid locationId)
    {
        await EnsureDatabaseAsync();
        return await _context!.UserLocations
            .AnyAsync(ul => ul.UserId == userId && ul.LocationId == locationId);
    }
    
    #endregion
    
    private void SeedDefaultData()
    {
        // Location Types
        if (!Context.LocationTypes.Any())
        {
            Context.LocationTypes.AddRange(
                new LocationTypeRecord { Name = "Warehouse", Icon = "Warehouse", Color = "#4CAF50" },
                new LocationTypeRecord { Name = "Base", Icon = "Office", Color = "#2196F3" },
                new LocationTypeRecord { Name = "Vessel", Icon = "Ship", Color = "#00BCD4" },
                new LocationTypeRecord { Name = "Project Site", Icon = "Construction", Color = "#FF9800" },
                new LocationTypeRecord { Name = "Container", Icon = "Package", Color = "#9C27B0" },
                new LocationTypeRecord { Name = "Workshop", Icon = "Wrench", Color = "#795548" }
            );
            Context.SaveChanges();
        }
        
        // Equipment Categories
        if (!Context.Categories.Any())
        {
            var categories = new List<EquipmentCategory>
            {
                new() { Name = "Survey Equipment", Code = "SURV", Icon = "Radar", Color = "#1E88E5", RequiresCertification = true, RequiresCalibration = true, DefaultCertificationPeriodDays = 365, DefaultCalibrationPeriodDays = 180, SortOrder = 1 },
                new() { Name = "ROV Equipment", Code = "ROV", Icon = "Submarine", Color = "#00ACC1", RequiresCertification = true, RequiresCalibration = true, DefaultCertificationPeriodDays = 365, DefaultCalibrationPeriodDays = 90, SortOrder = 2 },
                new() { Name = "Lifting Equipment", Code = "LIFT", Icon = "Crane", Color = "#FB8C00", RequiresCertification = true, DefaultCertificationPeriodDays = 180, SortOrder = 3 },
                new() { Name = "Safety Equipment", Code = "SAFE", Icon = "Shield", Color = "#E53935", RequiresCertification = true, DefaultCertificationPeriodDays = 365, SortOrder = 4 },
                new() { Name = "Diving Equipment", Code = "DIVE", Icon = "Waves", Color = "#039BE5", RequiresCertification = true, DefaultCertificationPeriodDays = 365, SortOrder = 5 },
                new() { Name = "Tools - Hand", Code = "TOOL-H", Icon = "Hammer", Color = "#6D4C41", SortOrder = 6 },
                new() { Name = "Tools - Power", Code = "TOOL-P", Icon = "PowerPlug", Color = "#5D4037", RequiresCertification = true, DefaultCertificationPeriodDays = 365, SortOrder = 7 },
                new() { Name = "Electronics", Code = "ELEC", Icon = "Cpu", Color = "#7E57C2", SortOrder = 8 },
                new() { Name = "Communication", Code = "COMM", Icon = "Radio", Color = "#5C6BC0", SortOrder = 9 },
                new() { Name = "Consumables", Code = "CONS", Icon = "Package", Color = "#78909C", IsConsumable = true, SortOrder = 10 }
            };
            Context.Categories.AddRange(categories);
            Context.SaveChanges();
        }
        
        // Roles
        if (!Context.Roles.Any())
        {
            Context.Roles.AddRange(
                new Role { Name = "System Administrator", Description = "Full system access to all features and settings", IsSystemRole = true },
                new Role { Name = "Base Manager", Description = "Manage base operations, equipment, and personnel", IsSystemRole = true },
                new Role { Name = "Vessel Superintendent", Description = "Manage vessel equipment and offshore operations", IsSystemRole = true },
                new Role { Name = "Project Manager", Description = "Manage project equipment assignments and transfers", IsSystemRole = true },
                new Role { Name = "Store Keeper", Description = "Day-to-day inventory management and manifest processing", IsSystemRole = true },
                new Role { Name = "Deck Operator", Description = "Scan equipment, create and receive manifests", IsSystemRole = true },
                new Role { Name = "Auditor", Description = "Read-only access for auditing and reporting", IsSystemRole = true }
            );
            Context.SaveChanges();
        }
        
        // Permissions
        if (!Context.Permissions.Any())
        {
            Context.Permissions.AddRange(
                // Equipment permissions
                new Permission { Name = "equipment.view", Category = "Equipment", Description = "View equipment details" },
                new Permission { Name = "equipment.create", Category = "Equipment", Description = "Create new equipment" },
                new Permission { Name = "equipment.edit", Category = "Equipment", Description = "Edit equipment details" },
                new Permission { Name = "equipment.delete", Category = "Equipment", Description = "Delete equipment" },
                new Permission { Name = "equipment.transfer", Category = "Equipment", Description = "Transfer equipment between locations" },
                
                // Manifest permissions
                new Permission { Name = "manifest.view", Category = "Manifests", Description = "View manifests" },
                new Permission { Name = "manifest.create", Category = "Manifests", Description = "Create new manifests" },
                new Permission { Name = "manifest.approve", Category = "Manifests", Description = "Approve manifests" },
                new Permission { Name = "manifest.receive", Category = "Manifests", Description = "Receive manifests" },
                
                // Admin permissions
                new Permission { Name = "users.manage", Category = "Admin", Description = "Manage users" },
                new Permission { Name = "roles.manage", Category = "Admin", Description = "Manage roles" },
                new Permission { Name = "locations.manage", Category = "Admin", Description = "Manage locations" },
                new Permission { Name = "categories.manage", Category = "Admin", Description = "Manage equipment categories" },
                new Permission { Name = "reports.view", Category = "Reports", Description = "View reports" },
                new Permission { Name = "reports.export", Category = "Reports", Description = "Export reports" }
            );
            Context.SaveChanges();
        }
        
        // Sync Settings
        if (!Context.SyncSettings.Any())
        {
            Context.SyncSettings.Add(new SyncSettings
            {
                DeviceId = Guid.NewGuid().ToString("N")[..16]
            });
            Context.SaveChanges();
        }
        
        // Default Administrator User
        if (!Context.Users.Any())
        {
            // Create default super administrator
            // DEFAULT CREDENTIALS: admin / Admin@123!
            // User MUST change password on first login
            var adminUser = new User
            {
                UserId = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@company.com",
                FirstName = "System",
                LastName = "Administrator",
                // Hash of "Admin@123!" - In production, use proper hashing
                PasswordHash = HashPassword("Admin@123!"),
                Salt = GenerateSalt(),
                PinHash = "1234", // Default PIN for quick access
                IsActive = true,
                IsSuperAdmin = true,
                MustChangePassword = true, // MUST change on first login
                PasswordExpiryDays = 0, // Super admin password never expires
                CreatedAt = DateTime.UtcNow
            };
            
            Context.Users.Add(adminUser);
            Context.SaveChanges();
            
            // Assign System Administrator role to admin
            var adminRole = Context.Roles.FirstOrDefault(r => r.Name == "System Administrator");
            if (adminRole != null)
            {
                Context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.UserId,
                    RoleId = adminRole.RoleId,
                    AssignedAt = DateTime.UtcNow
                });
                Context.SaveChanges();
            }
            
            // Assign all permissions to System Administrator role
            var allPermissions = Context.Permissions.ToList();
            foreach (var permission in allPermissions)
            {
                if (adminRole != null && !Context.Set<RolePermission>().Any(rp => rp.RoleId == adminRole.RoleId && rp.PermissionId == permission.PermissionId))
                {
                    Context.Set<RolePermission>().Add(new RolePermission
                    {
                        RoleId = adminRole.RoleId,
                        PermissionId = permission.PermissionId
                    });
                }
            }
            Context.SaveChanges();
        }
    }
    
    /// <summary>
    /// Simple password hashing (use BCrypt or similar in production)
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
    
    /// <summary>
    /// Generate salt for password hashing
    /// </summary>
    private static string GenerateSalt()
    {
        var salt = new byte[16];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return Convert.ToBase64String(salt);
    }
    
    /// <summary>
    /// Verify password against hash
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        var passwordHash = HashPassword(password);
        return passwordHash == hash;
    }
    
    #region Equipment Operations
    
    public async Task<List<Equipment>> GetEquipmentAsync(
        Guid? locationId = null, 
        Guid? categoryId = null,
        EquipmentStatus? status = null, 
        string? search = null,
        bool includeInactive = false,
        int skip = 0,
        int take = 100)
    {
        var query = Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.CurrentLocation)
            .AsNoTracking()
            .AsQueryable();
        
        if (!includeInactive)
            query = query.Where(e => e.IsActive);
        if (locationId.HasValue)
            query = query.Where(e => e.CurrentLocationId == locationId);
        if (categoryId.HasValue)
            query = query.Where(e => e.CategoryId == categoryId);
        if (status.HasValue)
            query = query.Where(e => e.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(e => 
                e.Name.ToLower().Contains(search) || 
                e.AssetNumber.ToLower().Contains(search) || 
                (e.UniqueId != null && e.UniqueId.ToLower().Contains(search)) ||
                (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(search)) ||
                (e.SapNumber != null && e.SapNumber.ToLower().Contains(search)) ||
                (e.Manufacturer != null && e.Manufacturer.ToLower().Contains(search)));
        }
        
        return await query
            .OrderBy(e => e.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<Equipment?> GetEquipmentByIdAsync(Guid id)
    {
        return await Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.Type)
            .Include(e => e.CurrentLocation)
            .Include(e => e.CurrentProject)
            .Include(e => e.Supplier)
            .Include(e => e.Photos)
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(e => e.EquipmentId == id);
    }
    
    public async Task<Equipment?> GetEquipmentByQrCodeAsync(string qrCode)
    {
        // Handle both full QR code and just asset number
        var searchCode = qrCode.StartsWith("foseq:") ? qrCode : $"foseq:{qrCode}";
        var assetNumber = qrCode.StartsWith("foseq:") ? qrCode[5..] : qrCode;
        
        return await Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.CurrentLocation)
            .Include(e => e.CurrentProject)
            .Include(e => e.Photos)
            .FirstOrDefaultAsync(e => e.QrCode == searchCode || e.AssetNumber == assetNumber);
    }
    
    public async Task<Equipment?> GetEquipmentByAssetNumberAsync(string assetNumber)
    {
        return await Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.Type)
            .Include(e => e.CurrentLocation)
            .FirstOrDefaultAsync(e => e.AssetNumber == assetNumber && e.IsActive);
    }
    
    /// <summary>
    /// Get equipment by unique ID (e.g., S7WSS04068)
    /// </summary>
    public async Task<Equipment?> GetEquipmentByUniqueIdAsync(string uniqueId)
    {
        return await Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.Type)
            .Include(e => e.CurrentLocation)
            .FirstOrDefaultAsync(e => e.UniqueId == uniqueId && e.IsActive);
    }
    
    public async Task<string> GenerateAssetNumberAsync(string? categoryCode = null)
    {
        var prefix = string.IsNullOrEmpty(categoryCode) ? "EQ" : categoryCode.ToUpper();
        var year = DateTime.Now.Year;
        var pattern = $"{prefix}-{year}-";
        
        var lastEquipment = await Context.Equipment
            .Where(e => e.AssetNumber.StartsWith(pattern))
            .OrderByDescending(e => e.AssetNumber)
            .FirstOrDefaultAsync();
        
        int nextNumber = 1;
        if (lastEquipment != null)
        {
            var parts = lastEquipment.AssetNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var lastNum))
                nextNumber = lastNum + 1;
        }
        
        return $"{prefix}-{year}-{nextNumber:D5}";
    }
    
    /// <summary>
    /// Generate next unique ID for equipment (e.g., S7WSS04068)
    /// Format: {OrgCode}{CategoryCode}{SequenceNumber}
    /// </summary>
    /// <param name="organizationCode">Organization code (e.g., "S7")</param>
    /// <param name="categoryCode">Category code (e.g., "WSS")</param>
    /// <returns>Unique ID like "S7WSS00001"</returns>
    public async Task<string> GenerateUniqueIdAsync(string organizationCode, string categoryCode)
    {
        var prefix = $"{organizationCode}{categoryCode}".ToUpper();
        
        // Find the highest existing unique ID with this prefix
        var lastEquipment = await Context.Equipment
            .Where(e => e.UniqueId != null && e.UniqueId.StartsWith(prefix))
            .OrderByDescending(e => e.UniqueId)
            .FirstOrDefaultAsync();
        
        int nextNumber = 1;
        if (lastEquipment?.UniqueId != null)
        {
            // Extract the numeric portion from the end
            var numericPart = new string(lastEquipment.UniqueId.Skip(prefix.Length).ToArray());
            if (int.TryParse(numericPart, out var lastNum))
                nextNumber = lastNum + 1;
        }
        
        return $"{prefix}{nextNumber:D5}";
    }
    
    /// <summary>
    /// Get the next sequence number for a category (for unique ID generation)
    /// </summary>
    public async Task<int> GetNextUniqueIdSequenceAsync(string organizationCode, string categoryCode)
    {
        var prefix = $"{organizationCode}{categoryCode}".ToUpper();
        
        var count = await Context.Equipment
            .CountAsync(e => e.UniqueId != null && e.UniqueId.StartsWith(prefix));
        
        return count + 1;
    }
    
    public async Task<Equipment> SaveEquipmentAsync(Equipment equipment)
    {
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;
        
        var isNew = equipment.EquipmentId == Guid.Empty || 
                    !await Context.Equipment.AnyAsync(e => e.EquipmentId == equipment.EquipmentId);
        
        // Store category ID before clearing navigation properties
        var categoryId = equipment.CategoryId;
        
        // Clear navigation properties to prevent circular reference issues
        equipment.CurrentLocation = null;
        equipment.CurrentProject = null;
        equipment.CurrentCustodian = null;
        equipment.Type = null;
        equipment.Category = null;
        equipment.Supplier = null;
        
        if (isNew)
        {
            if (equipment.EquipmentId == Guid.Empty)
                equipment.EquipmentId = Guid.NewGuid();
            equipment.CreatedAt = DateTime.UtcNow;
            
            // Generate asset number if not set
            if (string.IsNullOrEmpty(equipment.AssetNumber))
            {
                string? categoryCode = null;
                if (categoryId.HasValue)
                {
                    var category = await Context.Categories.FindAsync(categoryId);
                    categoryCode = category?.Code;
                }
                equipment.AssetNumber = await GenerateAssetNumberAsync(categoryCode);
            }
            
            // Generate QR code if not set
            if (string.IsNullOrEmpty(equipment.QrCode))
                equipment.QrCode = $"foseq:{equipment.AssetNumber}";
            
            Context.Equipment.Add(equipment);
            await AddToOfflineQueueAsync("Equipment", equipment.EquipmentId, "Insert", equipment);
        }
        else
        {
            // Detach any existing tracked entity with same ID
            var trackedEntity = Context.Equipment.Local.FirstOrDefault(e => e.EquipmentId == equipment.EquipmentId);
            if (trackedEntity != null)
            {
                Context.Entry(trackedEntity).State = EntityState.Detached;
            }
            
            Context.Equipment.Update(equipment);
            await AddToOfflineQueueAsync("Equipment", equipment.EquipmentId, "Update", equipment);
        }
        
        // Add history entry
        await AddEquipmentHistoryAsync(equipment.EquipmentId, 
            isNew ? "Created" : "Updated", 
            isNew ? "Equipment created" : "Equipment updated");
        
        await Context.SaveChangesAsync();
        return equipment;
    }
    
    public async Task DeleteEquipmentAsync(Guid equipmentId, bool hardDelete = false)
    {
        var equipment = await Context.Equipment.FindAsync(equipmentId);
        if (equipment == null) return;
        
        if (hardDelete)
        {
            Context.Equipment.Remove(equipment);
            await AddToOfflineQueueAsync("Equipment", equipmentId, "Delete");
        }
        else
        {
            equipment.IsActive = false;
            equipment.UpdatedAt = DateTime.UtcNow;
            equipment.IsModifiedLocally = true;
            await AddToOfflineQueueAsync("Equipment", equipmentId, "Update", equipment);
        }
        
        await Context.SaveChangesAsync();
    }
    
    public async Task AddEquipmentHistoryAsync(Guid equipmentId, string eventType, string? description = null, 
        string? previousValue = null, string? newValue = null)
    {
        // Parse event type to HistoryAction enum
        var action = Enum.TryParse<HistoryAction>(eventType, out var parsed) ? parsed : HistoryAction.Custom;
        
        Context.EquipmentHistory.Add(new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = action,
            Description = description,
            OldValue = previousValue,
            NewValue = newValue,
            PerformedAt = DateTime.UtcNow
        });
        
        await Task.CompletedTask; // Method is async but doesn't await anything directly
    }
    
    public async Task<List<EquipmentHistory>> GetEquipmentHistoryAsync(Guid? equipmentId = null, int limit = 100)
    {
        var query = Context.EquipmentHistory
            .Include(h => h.Equipment)
            .AsQueryable();
            
        if (equipmentId.HasValue)
            query = query.Where(h => h.EquipmentId == equipmentId);
            
        return await query
            .OrderByDescending(h => h.PerformedAt)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<int> GetEquipmentCountAsync(Guid? locationId = null, EquipmentStatus? status = null)
    {
        var query = Context.Equipment.Where(e => e.IsActive);
        if (locationId.HasValue)
            query = query.Where(e => e.CurrentLocationId == locationId);
        if (status.HasValue)
            query = query.Where(e => e.Status == status);
        return await query.CountAsync();
    }
    
    public async Task<List<Equipment>> GetExpiringCertificationsAsync(int daysAhead = 30)
    {
        var cutoffDate = DateTime.Today.AddDays(daysAhead);
        return await Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.CurrentLocation)
            .Where(e => e.IsActive && e.RequiresCertification && 
                        e.CertificationExpiryDate.HasValue && 
                        e.CertificationExpiryDate.Value <= cutoffDate)
            .OrderBy(e => e.CertificationExpiryDate)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<List<Equipment>> GetDueCalibrationAsync(int daysAhead = 7)
    {
        var cutoffDate = DateTime.Today.AddDays(daysAhead);
        return await Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.CurrentLocation)
            .Where(e => e.IsActive && e.RequiresCalibration && 
                        e.NextCalibrationDate.HasValue && 
                        e.NextCalibrationDate.Value <= cutoffDate)
            .OrderBy(e => e.NextCalibrationDate)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<List<Equipment>> GetLowStockItemsAsync()
    {
        return await Context.Equipment
            .Include(e => e.Category)
            .Include(e => e.CurrentLocation)
            .Where(e => e.IsActive && e.IsConsumable && 
                        e.MinimumStockLevel.HasValue && 
                        e.QuantityOnHand <= e.MinimumStockLevel.Value)
            .OrderBy(e => e.QuantityOnHand)
            .AsNoTracking()
            .ToListAsync();
    }
    
    #endregion
    
    #region Manifest Operations
    
    public async Task<List<Manifest>> GetManifestsAsync(
        ManifestStatus? status = null, 
        ManifestType? type = null,
        Guid? locationId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int skip = 0,
        int take = 50)
    {
        var query = Context.Manifests
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.Items)
            .Where(m => m.IsActive)
            .AsNoTracking()
            .AsQueryable();
        
        if (status.HasValue)
            query = query.Where(m => m.Status == status);
        if (type.HasValue)
            query = query.Where(m => m.Type == type);
        if (locationId.HasValue)
            query = query.Where(m => m.FromLocationId == locationId || m.ToLocationId == locationId);
        if (fromDate.HasValue)
            query = query.Where(m => m.CreatedDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(m => m.CreatedDate <= toDate.Value);
        
        return await query
            .OrderByDescending(m => m.CreatedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<Manifest?> GetManifestByIdAsync(Guid id)
    {
        return await Context.Manifests
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.Project)
            .Include(m => m.Items)
            .Include(m => m.Photos)
            .FirstOrDefaultAsync(m => m.ManifestId == id);
    }
    
    public async Task<Manifest?> GetManifestByQrCodeAsync(string qrCode)
    {
        // Support both old and new QR formats
        var searchCode = qrCode;
        var manifestNumber = qrCode;
        
        if (qrCode.StartsWith("fosman:"))
        {
            searchCode = qrCode;
            manifestNumber = qrCode[7..];
        }
        else if (qrCode.StartsWith("s7mn:"))
        {
            searchCode = qrCode;
            manifestNumber = qrCode[5..];
        }
        
        return await Context.Manifests
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.Items)
            .Include(m => m.Photos)
            .FirstOrDefaultAsync(m => m.QrCode == searchCode || m.ManifestNumber == manifestNumber);
    }
    
    /// <summary>
    /// Get pending inbound manifests for a specific destination location.
    /// These are outward manifests that have been sent but not yet verified at the destination.
    /// </summary>
    public async Task<List<Manifest>> GetPendingInboundManifestsAsync(Guid destinationLocationId)
    {
        return await Context.Manifests
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.Items)
            .Where(m => m.IsActive 
                && m.ToLocationId == destinationLocationId
                && m.Type == ManifestType.Outward
                && (m.Status == ManifestStatus.Submitted || m.Status == ManifestStatus.InTransit))
            .OrderByDescending(m => m.ShippedDate ?? m.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }
    
    /// <summary>
    /// Get manifest by manifest number (e.g., "OUT-2026-00001")
    /// </summary>
    public async Task<Manifest?> GetManifestByManifestNumberAsync(string manifestNumber)
    {
        if (string.IsNullOrWhiteSpace(manifestNumber))
            return null;
            
        return await Context.Manifests
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.Items)
            .Include(m => m.Photos)
            .FirstOrDefaultAsync(m => m.ManifestNumber == manifestNumber.Trim().ToUpper());
    }
    
    public async Task<string> GenerateManifestNumberAsync()
    {
        var year = DateTime.Now.Year;
        var pattern = $"MN-{year}-";
        
        var lastManifest = await Context.Manifests
            .Where(m => m.ManifestNumber.StartsWith(pattern))
            .OrderByDescending(m => m.ManifestNumber)
            .FirstOrDefaultAsync();
        
        int nextNumber = 1;
        if (lastManifest != null)
        {
            var parts = lastManifest.ManifestNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var lastNum))
                nextNumber = lastNum + 1;
        }
        
        return $"MN-{year}-{nextNumber:D5}";
    }
    
    public async Task<Manifest> SaveManifestAsync(Manifest manifest)
    {
        manifest.UpdatedAt = DateTime.UtcNow;
        manifest.IsModifiedLocally = true;
        manifest.TotalItems = manifest.Items.Count;
        
        var isNew = manifest.ManifestId == Guid.Empty || 
                    !await Context.Manifests.AnyAsync(m => m.ManifestId == manifest.ManifestId);
        
        if (isNew)
        {
            if (manifest.ManifestId == Guid.Empty)
                manifest.ManifestId = Guid.NewGuid();
            manifest.CreatedAt = DateTime.UtcNow;
            manifest.CreatedDate = DateTime.UtcNow;
            
            if (string.IsNullOrEmpty(manifest.ManifestNumber))
                manifest.ManifestNumber = await GenerateManifestNumberAsync();
            if (string.IsNullOrEmpty(manifest.QrCode))
                manifest.QrCode = $"s7mn:{manifest.ManifestNumber}";
            
            Context.Manifests.Add(manifest);
            await AddToOfflineQueueAsync("Manifest", manifest.ManifestId, "Insert", manifest);
        }
        else
        {
            Context.Manifests.Update(manifest);
            await AddToOfflineQueueAsync("Manifest", manifest.ManifestId, "Update", manifest);
        }
        
        await Context.SaveChangesAsync();
        return manifest;
    }
    
    public async Task UpdateManifestStatusAsync(Guid manifestId, ManifestStatus newStatus, Guid? userId = null)
    {
        var manifest = await Context.Manifests.FindAsync(manifestId);
        if (manifest == null) return;
        
        manifest.Status = newStatus;
        manifest.UpdatedAt = DateTime.UtcNow;
        manifest.IsModifiedLocally = true;
        
        switch (newStatus)
        {
            case ManifestStatus.Submitted:
                manifest.SubmittedDate = DateTime.UtcNow;
                break;
            case ManifestStatus.Approved:
                manifest.ApprovedDate = DateTime.UtcNow;
                manifest.ApprovedBy = userId;
                break;
            case ManifestStatus.InTransit:
                manifest.ShippedDate = DateTime.UtcNow;
                break;
            case ManifestStatus.Received:
            case ManifestStatus.PartiallyReceived:
                manifest.ReceivedDate = DateTime.UtcNow;
                manifest.ReceivedBy = userId;
                break;
            case ManifestStatus.Completed:
                manifest.CompletedDate = DateTime.UtcNow;
                break;
        }
        
        await AddToOfflineQueueAsync("Manifest", manifestId, "Update", manifest);
        await Context.SaveChangesAsync();
    }
    
    #endregion
    
    #region Offline Queue
    
    public async Task AddToOfflineQueueAsync(string tableName, Guid recordId, string operation, object? data = null)
    {
        var existing = await Context.OfflineQueue
            .FirstOrDefaultAsync(q => q.TableName == tableName && q.RecordId == recordId && q.Status == "Pending");
        
        // Use options to handle circular references
        var jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };
        string? dataJson = data != null ? JsonSerializer.Serialize(data, jsonOptions) : null;
        
        if (existing != null)
        {
            existing.Operation = operation == "Delete" ? "Delete" : existing.Operation;
            existing.DataJson = dataJson;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            Context.OfflineQueue.Add(new OfflineQueueItem
            {
                TableName = tableName,
                RecordId = recordId,
                Operation = operation,
                DataJson = dataJson
            });
        }
    }
    
    public async Task<List<OfflineQueueItem>> GetPendingQueueItemsAsync()
    {
        return await Context.OfflineQueue
            .Where(q => q.Status == "Pending")
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.CreatedAt)
            .ToListAsync();
    }
    
    public async Task MarkQueueItemCompletedAsync(Guid queueId)
    {
        var item = await Context.OfflineQueue.FindAsync(queueId);
        if (item != null)
        {
            item.Status = "Completed";
            await Context.SaveChangesAsync();
        }
    }
    
    public async Task MarkQueueItemFailedAsync(Guid queueId, string errorMessage)
    {
        var item = await Context.OfflineQueue.FindAsync(queueId);
        if (item != null)
        {
            item.Attempts++;
            item.LastAttempt = DateTime.UtcNow;
            item.ErrorMessage = errorMessage;
            if (item.Attempts >= 5)
                item.Status = "Failed";
            await Context.SaveChangesAsync();
        }
    }
    
    public async Task ClearCompletedQueueItemsAsync()
    {
        var completed = await Context.OfflineQueue
            .Where(q => q.Status == "Completed")
            .ToListAsync();
        Context.OfflineQueue.RemoveRange(completed);
        await Context.SaveChangesAsync();
    }
    
    #endregion
    
    #region Lookups
    
    public async Task<List<Location>> GetLocationsAsync(bool includeInactive = false)
    {
        var query = Context.Locations.Include(l => l.LocationTypeRecord).AsQueryable();
        if (!includeInactive)
            query = query.Where(l => l.IsActive);
        return await query.OrderBy(l => l.Name).AsNoTracking().ToListAsync();
    }
    
    public async Task<Location?> GetLocationByIdAsync(Guid locationId)
    {
        return await Context.Locations
            .Include(l => l.LocationTypeRecord)
            .FirstOrDefaultAsync(l => l.LocationId == locationId);
    }
    
    public async Task<Location?> GetLocationByNameAsync(string name)
    {
        return await Context.Locations
            .FirstOrDefaultAsync(l => l.Name.ToLower() == name.ToLower());
    }
    
    public async Task AddLocationAsync(Location location)
    {
        Context.Locations.Add(location);
        await Context.SaveChangesAsync();
    }
    
    public async Task UpdateLocationAsync(Location location)
    {
        var existing = await Context.Locations.FindAsync(location.LocationId);
        if (existing != null)
        {
            existing.Name = location.Name;
            existing.Code = location.Code;
            existing.Type = location.Type;
            existing.Description = location.Description;
            existing.Address = location.Address;
            existing.ContactPerson = location.ContactPerson;
            existing.ContactPhone = location.ContactPhone;
            existing.ContactEmail = location.ContactEmail;
            existing.IsActive = location.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SyncVersion++;
            await Context.SaveChangesAsync();
        }
    }
    
    public async Task DeleteLocationByIdAsync(Guid locationId)
    {
        var location = await Context.Locations.FindAsync(locationId);
        if (location != null)
        {
            // Check if any equipment is assigned to this location
            var equipmentCount = await Context.Equipment.CountAsync(e => e.CurrentLocationId == locationId);
            if (equipmentCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete location. {equipmentCount} equipment items are assigned to this location.");
            }
            
            Context.Locations.Remove(location);
            await Context.SaveChangesAsync();
        }
    }
    
    public async Task<List<EquipmentCategory>> GetCategoriesAsync(bool includeInactive = false)
    {
        var query = Context.Categories.AsQueryable();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).AsNoTracking().ToListAsync();
    }
    
    public async Task<EquipmentCategory?> GetCategoryByNameAsync(string name)
    {
        return await Context.Categories
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }
    
    public async Task SaveCategoryAsync(EquipmentCategory category)
    {
        var existing = await Context.Categories.FirstOrDefaultAsync(c => c.CategoryId == category.CategoryId);
        if (existing == null)
        {
            Context.Categories.Add(category);
        }
        else
        {
            existing.Name = category.Name;
            existing.Description = category.Description;
            existing.Code = category.Code;
            existing.ParentCategoryId = category.ParentCategoryId;
            existing.IsActive = category.IsActive;
            existing.SortOrder = category.SortOrder;
        }
        await Context.SaveChangesAsync();
    }
    
    public async Task<List<EquipmentType>> GetTypesAsync(Guid? categoryId = null)
    {
        var query = Context.EquipmentTypes.Include(t => t.Category).Where(t => t.IsActive);
        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId);
        return await query.OrderBy(t => t.Name).AsNoTracking().ToListAsync();
    }
    
    public async Task<List<Project>> GetProjectsAsync(bool activeOnly = true)
    {
        var query = Context.Projects.Include(p => p.Location).Where(p => p.IsActive);
        if (activeOnly)
            query = query.Where(p => p.Status == ProjectStatus.Active);
        return await query.OrderBy(p => p.Name).AsNoTracking().ToListAsync();
    }
    
    public async Task<List<Supplier>> GetSuppliersAsync()
    {
        return await Context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).AsNoTracking().ToListAsync();
    }
    
    public async Task<List<Supplier>> GetAllSuppliersAsync(bool includeInactive = false)
    {
        var query = Context.Suppliers.AsQueryable();
        if (!includeInactive)
            query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.Name).AsNoTracking().ToListAsync();
    }
    
    public async Task<Supplier?> GetSupplierByIdAsync(Guid supplierId)
    {
        return await Context.Suppliers.FirstOrDefaultAsync(s => s.SupplierId == supplierId);
    }
    
    public async Task AddSupplierAsync(Supplier supplier)
    {
        Context.Suppliers.Add(supplier);
        await Context.SaveChangesAsync();
    }
    
    public async Task UpdateSupplierAsync(Supplier supplier)
    {
        var existing = await Context.Suppliers.FindAsync(supplier.SupplierId);
        if (existing != null)
        {
            existing.Name = supplier.Name;
            existing.Code = supplier.Code;
            existing.ContactPerson = supplier.ContactPerson;
            existing.Email = supplier.Email;
            existing.Phone = supplier.Phone;
            existing.Address = supplier.Address;
            existing.IsActive = supplier.IsActive;
            existing.SyncVersion++;
            await Context.SaveChangesAsync();
        }
    }
    
    public async Task DeleteSupplierByIdAsync(Guid supplierId)
    {
        var supplier = await Context.Suppliers.FindAsync(supplierId);
        if (supplier != null)
        {
            // Check if supplier is used by any equipment
            var equipmentCount = await Context.Equipment.CountAsync(e => e.SupplierId == supplierId);
            if (equipmentCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete supplier. {equipmentCount} equipment items are linked to this supplier.");
            }
            
            Context.Suppliers.Remove(supplier);
            await Context.SaveChangesAsync();
        }
    }
    
    public async Task<SyncSettings?> GetSyncSettingsAsync()
    {
        return await Context.SyncSettings.FirstOrDefaultAsync();
    }
    
    public async Task UpdateSyncSettingsAsync(long newVersion, DateTime syncTime, bool isFullSync = false)
    {
        var settings = await Context.SyncSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SyncSettings();
            Context.SyncSettings.Add(settings);
        }
        
        settings.LastSyncVersion = newVersion;
        if (isFullSync)
            settings.LastFullSyncAt = syncTime;
        else
            settings.LastDeltaSyncAt = syncTime;
        
        await Context.SaveChangesAsync();
    }
    
    #endregion
    
    #region Statistics
    
    public async Task<DashboardStats> GetDashboardStatsAsync(Guid? locationId = null)
    {
        var equipmentQuery = Context.Equipment.Where(e => e.IsActive);
        var manifestQuery = Context.Manifests.Where(m => m.IsActive);
        
        if (locationId.HasValue)
        {
            equipmentQuery = equipmentQuery.Where(e => e.CurrentLocationId == locationId);
            manifestQuery = manifestQuery.Where(m => m.FromLocationId == locationId || m.ToLocationId == locationId);
        }
        
        var today = DateTime.Today;
        var thirtyDaysFromNow = today.AddDays(30);
        
        return new DashboardStats
        {
            TotalEquipment = await equipmentQuery.CountAsync(),
            AvailableEquipment = await equipmentQuery.CountAsync(e => e.Status == EquipmentStatus.Available),
            InTransitEquipment = await equipmentQuery.CountAsync(e => e.Status == EquipmentStatus.InTransit),
            UnderRepairEquipment = await equipmentQuery.CountAsync(e => e.Status == EquipmentStatus.UnderRepair),
            
            ExpiringCertifications = await equipmentQuery.CountAsync(e => 
                e.RequiresCertification && e.CertificationExpiryDate.HasValue && 
                e.CertificationExpiryDate.Value <= thirtyDaysFromNow && e.CertificationExpiryDate.Value >= today),
            
            OverdueCertifications = await equipmentQuery.CountAsync(e => 
                e.RequiresCertification && e.CertificationExpiryDate.HasValue && 
                e.CertificationExpiryDate.Value < today),
            
            DueCalibrations = await equipmentQuery.CountAsync(e => 
                e.RequiresCalibration && e.NextCalibrationDate.HasValue && 
                e.NextCalibrationDate.Value <= thirtyDaysFromNow),
            
            LowStockItems = await equipmentQuery.CountAsync(e => 
                e.IsConsumable && e.MinimumStockLevel.HasValue && 
                e.QuantityOnHand <= e.MinimumStockLevel.Value),
            
            PendingManifests = await manifestQuery.CountAsync(m => 
                m.Status == ManifestStatus.Draft || m.Status == ManifestStatus.Submitted || 
                m.Status == ManifestStatus.PendingApproval),
            
            InTransitManifests = await manifestQuery.CountAsync(m => m.Status == ManifestStatus.InTransit),
            
            ManifestsThisMonth = await manifestQuery.CountAsync(m => 
                m.CreatedDate.Month == today.Month && m.CreatedDate.Year == today.Year)
        };
    }
    
    #endregion
    
    #region Maintenance Operations
    
    public async Task<List<MaintenanceRecord>> GetAllMaintenanceRecordsAsync()
    {
        await EnsureDatabaseAsync();
        try
        {
            return await _context!.Set<MaintenanceRecord>()
                .OrderByDescending(m => m.PerformedDate)
                .ToListAsync();
        }
        catch
        {
            // If MaintenanceRecord table doesn't exist, return empty list
            return new List<MaintenanceRecord>();
        }
    }
    
    public async Task<MaintenanceRecord?> GetMaintenanceRecordAsync(Guid id)
    {
        await EnsureDatabaseAsync();
        return await _context!.Set<MaintenanceRecord>()
            .Include(m => m.Equipment)
            .FirstOrDefaultAsync(m => m.MaintenanceId == id);
    }
    
    public async Task<List<MaintenanceRecord>> GetMaintenanceRecordsForEquipmentAsync(Guid equipmentId)
    {
        await EnsureDatabaseAsync();
        return await _context!.Set<MaintenanceRecord>()
            .Where(m => m.EquipmentId == equipmentId)
            .OrderByDescending(m => m.PerformedDate)
            .ToListAsync();
    }
    
    public async Task SaveMaintenanceRecordAsync(MaintenanceRecord record)
    {
        await EnsureDatabaseAsync();
        
        var existing = await _context!.Set<MaintenanceRecord>()
            .FirstOrDefaultAsync(m => m.MaintenanceId == record.MaintenanceId);
        
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(record);
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SyncVersion++;
        }
        else
        {
            record.CreatedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            _context.Set<MaintenanceRecord>().Add(record);
        }
        
        await _context.SaveChangesAsync();
    }
    
    #endregion
    
    #region Certification Operations
    
    public async Task<List<Certification>> GetAllCertificationsAsync()
    {
        await EnsureDatabaseAsync();
        try
        {
            return await _context!.Certifications
                .Include(c => c.Equipment)
                .OrderBy(c => c.ExpiryDate)
                .ToListAsync();
        }
        catch
        {
            // If Certification table doesn't exist, return empty list
            return new List<Certification>();
        }
    }
    
    public async Task<List<Certification>> GetCertificationsForEquipmentAsync(Guid equipmentId)
    {
        await EnsureDatabaseAsync();
        return await _context!.Certifications
            .Where(c => c.EquipmentId == equipmentId)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }
    
    public async Task<List<Certification>> GetExpiringCertificationRecordsAsync(int daysAhead = 30)
    {
        await EnsureDatabaseAsync();
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);
        return await _context!.Certifications
            .Include(c => c.Equipment)
            .Where(c => c.ExpiryDate <= cutoffDate && c.IsActive)
            .OrderBy(c => c.ExpiryDate)
            .ToListAsync();
    }
    
    public async Task SaveCertificationAsync(Certification cert)
    {
        await EnsureDatabaseAsync();
        
        var existing = await _context!.Certifications
            .FirstOrDefaultAsync(c => c.CertificationId == cert.CertificationId);
        
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(cert);
            existing.UpdatedAt = DateTime.UtcNow;
            existing.SyncVersion++;
        }
        else
        {
            cert.CreatedAt = DateTime.UtcNow;
            cert.UpdatedAt = DateTime.UtcNow;
            _context.Certifications.Add(cert);
        }
        
        await _context.SaveChangesAsync();
    }
    
    #endregion
    
    #region User Management Operations
    
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        await EnsureDatabaseAsync();
        return await _context!.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }
    
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        await EnsureDatabaseAsync();
        return await _context!.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }
    
    public async Task<(bool Success, User? User, string? Error)> AuthenticateUserAsync(string username, string password)
    {
        await EnsureDatabaseAsync();
        var user = await GetUserByUsernameAsync(username);
        
        if (user == null)
            return (false, null, "Invalid username or password.");
        
        if (!user.IsActive)
            return (false, null, "This account has been deactivated. Contact your administrator.");
        
        // Check if locked
        if (user.IsLocked)
        {
            var remainingMinutes = user.LockedUntil.HasValue 
                ? (int)Math.Ceiling((user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes)
                : 15;
            return (false, null, $"Account is locked. Try again in {remainingMinutes} minutes.");
        }
        
        // Verify password
        if (!VerifyPassword(password, user.PasswordHash ?? ""))
        {
            // Increment failed attempts
            user.FailedLoginAttempts++;
            var attemptsRemaining = 5 - user.FailedLoginAttempts;
            
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                return (false, null, "Account locked due to too many failed attempts. Try again in 15 minutes.");
            }
            
            await _context.SaveChangesAsync();
            return (false, null, $"Invalid username or password. {attemptsRemaining} attempts remaining.");
        }
        
        // Successful login - reset failed attempts
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return (true, user, null);
    }
    
    /// <summary>
    /// Authenticate user with PIN (for offline/quick login)
    /// </summary>
    public async Task<(bool Success, User? User, string? Error)> AuthenticateUserWithPinAsync(string username, string pin)
    {
        await EnsureDatabaseAsync();
        var user = await GetUserByUsernameAsync(username);
        
        if (user == null)
            return (false, null, "User not found.");
        
        if (!user.IsActive)
            return (false, null, "This account has been deactivated.");
        
        if (user.IsLocked)
        {
            var remainingMinutes = user.LockedUntil.HasValue 
                ? (int)Math.Ceiling((user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes)
                : 15;
            return (false, null, $"Account is locked. Try again in {remainingMinutes} minutes.");
        }
        
        // Check if user has a PIN set
        if (string.IsNullOrEmpty(user.PinHash))
            return (false, null, "PIN not set. Please login with password.");
        
        // Verify PIN (stored as hash)
        if (user.PinHash != HashPin(pin))
        {
            // Increment failed attempts
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                return (false, null, "Account locked due to too many failed attempts.");
            }
            await _context.SaveChangesAsync();
            return (false, null, "Invalid PIN.");
        }
        
        // Successful PIN login
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return (true, user, null);
    }
    
    /// <summary>
    /// Simple PIN hash (for demo purposes - use proper hashing in production)
    /// </summary>
    private static string HashPin(string pin)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(pin + "pin_salt_2024");
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
    
    public async Task<(bool Success, string? Error)> CreateUserAsync(User user, string temporaryPassword)
    {
        await EnsureDatabaseAsync();
        
        // Check if username exists
        if (await _context!.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower()))
            return (false, "Username already exists");
        
        // Check if email exists
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == user.Email.ToLower()))
            return (false, "Email already exists");
        
        // Validate password policy
        var (isValid, policyError) = ValidatePasswordPolicy(temporaryPassword);
        if (!isValid)
            return (false, policyError);
        
        // Set password
        user.Salt = GenerateSalt();
        user.PasswordHash = HashPassword(temporaryPassword);
        user.TemporaryPassword = temporaryPassword; // Store for display to admin
        user.MustChangePassword = true;
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return (true, null);
    }
    
    public async Task<(bool Success, string? Error)> UpdateUserAsync(User user)
    {
        await EnsureDatabaseAsync();
        
        var existing = await _context!.Users.FindAsync(user.UserId);
        if (existing == null)
            return (false, "User not found");
        
        // Check username uniqueness (excluding current user)
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower() && u.UserId != user.UserId))
            return (false, "Username already exists");
        
        existing.Username = user.Username;
        existing.Email = user.Email;
        existing.FirstName = user.FirstName;
        existing.LastName = user.LastName;
        existing.Phone = user.Phone;
        existing.IsActive = user.IsActive;
        existing.DefaultLocationId = user.DefaultLocationId;
        existing.PasswordExpiryDays = user.PasswordExpiryDays;
        existing.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return (true, null);
    }
    
    public async Task<(bool Success, string? Error)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        await EnsureDatabaseAsync();
        
        var user = await _context!.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");
        
        // Verify current password (skip for first-time change with temporary password)
        if (!user.MustChangePassword && !VerifyPassword(currentPassword, user.PasswordHash ?? ""))
            return (false, "Current password is incorrect");
        
        // Validate new password policy
        var (isValid, policyError) = ValidatePasswordPolicy(newPassword);
        if (!isValid)
            return (false, policyError);
        
        // Update password
        user.Salt = GenerateSalt();
        user.PasswordHash = HashPassword(newPassword);
        user.MustChangePassword = false;
        user.TemporaryPassword = null; // Clear temporary password
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return (true, null);
    }
    
    public async Task<(bool Success, string? Error)> ResetPasswordAsync(Guid userId, string newTemporaryPassword)
    {
        await EnsureDatabaseAsync();
        
        var user = await _context!.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");
        
        // Validate password policy
        var (isValid, policyError) = ValidatePasswordPolicy(newTemporaryPassword);
        if (!isValid)
            return (false, policyError);
        
        // Reset password
        user.Salt = GenerateSalt();
        user.PasswordHash = HashPassword(newTemporaryPassword);
        user.TemporaryPassword = newTemporaryPassword;
        user.MustChangePassword = true;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return (true, null);
    }
    
    public async Task<(bool Success, string? Error)> SetUserPinAsync(Guid userId, string pin)
    {
        await EnsureDatabaseAsync();
        
        var user = await _context!.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found");
        
        if (pin.Length != 4 || !pin.All(char.IsDigit))
            return (false, "PIN must be exactly 4 digits");
        
        user.PinHash = HashPin(pin); // Properly hash the PIN
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return (true, null);
    }
    
    public async Task AssignRoleToUserAsync(Guid userId, Guid roleId)
    {
        await EnsureDatabaseAsync();
        
        if (!await _context!.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId))
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task RemoveRoleFromUserAsync(Guid userId, Guid roleId)
    {
        await EnsureDatabaseAsync();
        
        var userRole = await _context!.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        
        if (userRole != null)
        {
            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<bool> DeactivateUserAsync(Guid userId)
    {
        await EnsureDatabaseAsync();
        
        var user = await _context!.Users.FindAsync(userId);
        if (user == null || user.IsSuperAdmin)
            return false;
        
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> UnlockUserAsync(Guid userId)
    {
        await EnsureDatabaseAsync();
        
        var user = await _context!.Users.FindAsync(userId);
        if (user == null)
            return false;
        
        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
    
    private static (bool IsValid, string? Error) ValidatePasswordPolicy(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password is required");
        
        if (password.Length < 8)
            return (false, "Password must be at least 8 characters");
        
        if (!password.Any(char.IsUpper))
            return (false, "Password must contain at least one uppercase letter");
        
        if (!password.Any(char.IsLower))
            return (false, "Password must contain at least one lowercase letter");
        
        if (!password.Any(char.IsDigit))
            return (false, "Password must contain at least one digit");
        
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return (false, "Password must contain at least one special character");
        
        return (true, null);
    }
    
    /// <summary>
    /// Generate a random temporary password meeting policy requirements
    /// </summary>
    public static string GenerateTemporaryPassword()
    {
        const string upperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowerChars = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%&*";
        
        var random = new Random();
        var password = new char[12];
        
        // Ensure at least one of each required type
        password[0] = upperChars[random.Next(upperChars.Length)];
        password[1] = lowerChars[random.Next(lowerChars.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];
        
        // Fill the rest
        var allChars = upperChars + lowerChars + digits + special;
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }
        
        // Shuffle
        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }
    
    #endregion
    
    #region Defect Reports (EFN)
    
    public async Task<List<DefectReport>> GetDefectReportsAsync(
        DefectReportStatus? status = null,
        ReplacementUrgency? urgency = null,
        Guid? locationId = null,
        Guid? equipmentId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? searchText = null,
        int skip = 0,
        int take = 50)
    {
        var query = Context.DefectReports
            .Include(d => d.Equipment)
            .Include(d => d.Location)
            .Include(d => d.CreatedByUser)
            .Include(d => d.EquipmentCategory)
            .Where(d => !d.IsDeleted);
        
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        
        if (urgency.HasValue)
            query = query.Where(d => d.ReplacementUrgency == urgency.Value);
        
        if (locationId.HasValue)
            query = query.Where(d => d.LocationId == locationId.Value);
        
        if (equipmentId.HasValue)
            query = query.Where(d => d.EquipmentId == equipmentId.Value);
        
        if (fromDate.HasValue)
            query = query.Where(d => d.ReportDate >= fromDate.Value);
        
        if (toDate.HasValue)
            query = query.Where(d => d.ReportDate <= toDate.Value);
        
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(d => 
                d.ReportNumber.ToLower().Contains(search) ||
                (d.SerialNumber != null && d.SerialNumber.ToLower().Contains(search)) ||
                (d.DetailedSymptoms != null && d.DetailedSymptoms.ToLower().Contains(search)) ||
                (d.MajorComponent != null && d.MajorComponent.ToLower().Contains(search)));
        }
        
        return await query
            .OrderByDescending(d => d.ReportDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<DefectReport?> GetDefectReportByIdAsync(Guid defectReportId)
    {
        return await Context.DefectReports
            .Include(d => d.Equipment)
            .Include(d => d.Location)
            .Include(d => d.CreatedByUser)
            .Include(d => d.EquipmentCategory)
            .Include(d => d.Parts)
            .Include(d => d.Attachments)
            .Include(d => d.AssignedToUser)
            .Include(d => d.ReviewedByUser)
            .Include(d => d.ResolvedByUser)
            .FirstOrDefaultAsync(d => d.DefectReportId == defectReportId);
    }
    
    public async Task<DefectReport?> GetDefectReportByNumberAsync(string reportNumber)
    {
        return await Context.DefectReports
            .Include(d => d.Equipment)
            .Include(d => d.Location)
            .Include(d => d.Parts)
            .FirstOrDefaultAsync(d => d.ReportNumber == reportNumber);
    }
    
    public async Task<string> GenerateDefectReportNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"EFN-{year}-";
        
        var lastNumber = await Context.DefectReports
            .Where(d => d.ReportNumber.StartsWith(prefix))
            .OrderByDescending(d => d.ReportNumber)
            .Select(d => d.ReportNumber)
            .FirstOrDefaultAsync();
        
        int nextSeq = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var seqStr = lastNumber.Substring(prefix.Length);
            if (int.TryParse(seqStr, out int seq))
                nextSeq = seq + 1;
        }
        
        return $"{prefix}{nextSeq:D5}";
    }
    
    public async Task<DefectReport> CreateDefectReportAsync(DefectReport report, Guid userId)
    {
        if (string.IsNullOrEmpty(report.ReportNumber))
            report.ReportNumber = await GenerateDefectReportNumberAsync();
        
        report.QrCode = $"fosefn:{report.ReportNumber}";
        report.CreatedByUserId = userId;
        report.CreatedAt = DateTime.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;
        report.Status = DefectReportStatus.Draft;
        
        Context.DefectReports.Add(report);
        
        // Add history entry
        Context.DefectReportHistory.Add(new DefectReportHistory
        {
            DefectReportId = report.DefectReportId,
            Action = "Created",
            NewStatus = DefectReportStatus.Draft,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow
        });
        
        await Context.SaveChangesAsync();
        return report;
    }
    
    public async Task<DefectReport> UpdateDefectReportAsync(DefectReport report, Guid userId)
    {
        var existing = await Context.DefectReports.FindAsync(report.DefectReportId);
        if (existing == null)
            throw new InvalidOperationException("Defect report not found");
        
        var oldStatus = existing.Status;
        
        // Update fields
        existing.ClientProject = report.ClientProject;
        existing.LocationId = report.LocationId;
        existing.ThirdPartyLocationName = report.ThirdPartyLocationName;
        existing.RovSystem = report.RovSystem;
        existing.WorkingWaterDepthMetres = report.WorkingWaterDepthMetres;
        existing.EquipmentOrigin = report.EquipmentOrigin;
        existing.EquipmentId = report.EquipmentId;
        existing.EquipmentCategoryId = report.EquipmentCategoryId;
        existing.MajorComponent = report.MajorComponent;
        existing.MinorComponent = report.MinorComponent;
        existing.OwnershipType = report.OwnershipType;
        existing.EquipmentOwner = report.EquipmentOwner;
        existing.ResponsibilityType = report.ResponsibilityType;
        existing.SapIdOrVendorAssetId = report.SapIdOrVendorAssetId;
        existing.SerialNumber = report.SerialNumber;
        existing.Manufacturer = report.Manufacturer;
        existing.Model = report.Model;
        existing.FaultCategory = report.FaultCategory;
        existing.DetailedSymptoms = report.DetailedSymptoms;
        existing.PhotosAttached = report.PhotosAttached;
        existing.ActionTaken = report.ActionTaken;
        existing.PartsAvailableOnBoard = report.PartsAvailableOnBoard;
        existing.ReplacementRequired = report.ReplacementRequired;
        existing.ReplacementUrgency = report.ReplacementUrgency;
        existing.FurtherComments = report.FurtherComments;
        existing.NextPortCallDate = report.NextPortCallDate;
        existing.NextPortCallLocation = report.NextPortCallLocation;
        existing.RepairDurationMinutes = report.RepairDurationMinutes;
        existing.DowntimeDurationMinutes = report.DowntimeDurationMinutes;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.SyncStatus = SyncStatus.Pending;
        
        // Add history if status changed
        if (oldStatus != report.Status)
        {
            existing.Status = report.Status;
            Context.DefectReportHistory.Add(new DefectReportHistory
            {
                DefectReportId = report.DefectReportId,
                Action = "Status Changed",
                OldStatus = oldStatus,
                NewStatus = report.Status,
                PerformedByUserId = userId,
                PerformedAt = DateTime.UtcNow
            });
        }
        
        await Context.SaveChangesAsync();
        return existing;
    }
    
    public async Task SubmitDefectReportAsync(Guid defectReportId, Guid userId)
    {
        var report = await Context.DefectReports.FindAsync(defectReportId);
        if (report == null)
            throw new InvalidOperationException("Defect report not found");
        
        if (!report.CanSubmit)
            throw new InvalidOperationException("Cannot submit this report in its current state");
        
        var oldStatus = report.Status;
        report.Status = DefectReportStatus.Submitted;
        report.SubmittedAt = DateTime.UtcNow;
        report.SubmittedByUserId = userId;
        report.UpdatedAt = DateTime.UtcNow;
        
        Context.DefectReportHistory.Add(new DefectReportHistory
        {
            DefectReportId = defectReportId,
            Action = "Submitted",
            OldStatus = oldStatus,
            NewStatus = DefectReportStatus.Submitted,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow
        });
        
        await Context.SaveChangesAsync();
    }
    
    public async Task ResolveDefectReportAsync(Guid defectReportId, Guid userId, string? resolutionNotes)
    {
        var report = await Context.DefectReports.FindAsync(defectReportId);
        if (report == null)
            throw new InvalidOperationException("Defect report not found");
        
        var oldStatus = report.Status;
        report.Status = DefectReportStatus.Resolved;
        report.ResolvedAt = DateTime.UtcNow;
        report.ResolvedByUserId = userId;
        report.ResolutionNotes = resolutionNotes;
        report.UpdatedAt = DateTime.UtcNow;
        
        Context.DefectReportHistory.Add(new DefectReportHistory
        {
            DefectReportId = defectReportId,
            Action = "Resolved",
            OldStatus = oldStatus,
            NewStatus = DefectReportStatus.Resolved,
            Details = resolutionNotes,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow
        });
        
        await Context.SaveChangesAsync();
    }
    
    public async Task<int> GetDefectReportCountAsync(DefectReportStatus? status = null)
    {
        var query = Context.DefectReports.Where(d => !d.IsDeleted);
        if (status.HasValue)
            query = query.Where(d => d.Status == status.Value);
        return await query.CountAsync();
    }
    
    public async Task<List<DefectReportPart>> GetDefectReportPartsAsync(Guid defectReportId)
    {
        return await Context.DefectReportParts
            .Where(p => p.DefectReportId == defectReportId)
            .OrderBy(p => p.LineNumber)
            .ToListAsync();
    }
    
    public async Task SaveDefectReportPartsAsync(Guid defectReportId, List<DefectReportPart> parts)
    {
        // Remove existing parts
        var existing = await Context.DefectReportParts
            .Where(p => p.DefectReportId == defectReportId)
            .ToListAsync();
        Context.DefectReportParts.RemoveRange(existing);
        
        // Add new parts
        foreach (var part in parts)
        {
            part.DefectReportId = defectReportId;
            Context.DefectReportParts.Add(part);
        }
        
        await Context.SaveChangesAsync();
    }
    
    public async Task<List<DefectReportHistory>> GetDefectReportHistoryAsync(Guid defectReportId)
    {
        return await Context.DefectReportHistory
            .Include(h => h.PerformedByUser)
            .Where(h => h.DefectReportId == defectReportId)
            .OrderByDescending(h => h.PerformedAt)
            .ToListAsync();
    }
    
    #endregion

    #region Shipment Verification
    
    /// <summary>
    /// Get manifest by manifest number
    /// </summary>
    public async Task<Manifest?> GetManifestByNumberAsync(string manifestNumber)
    {
        return await Context.Manifests
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.Project)
            .Include(m => m.Items)
            .FirstOrDefaultAsync(m => m.ManifestNumber == manifestNumber);
    }
    
    /// <summary>
    /// Alias for GetManifestByIdAsync
    /// </summary>
    public async Task<Manifest?> GetManifestAsync(Guid manifestId)
    {
        return await GetManifestByIdAsync(manifestId);
    }
    
    /// <summary>
    /// Get items for a manifest
    /// </summary>
    public async Task<List<ManifestItem>> GetManifestItemsAsync(Guid manifestId)
    {
        return await Context.Set<ManifestItem>()
            .Include(i => i.Equipment)
            .Where(i => i.ManifestId == manifestId)
            .ToListAsync();
    }
    
    /// <summary>
    /// Get a specific manifest item
    /// </summary>
    public async Task<ManifestItem?> GetManifestItemAsync(Guid manifestId, Guid itemId)
    {
        return await Context.Set<ManifestItem>()
            .FirstOrDefaultAsync(i => i.ManifestId == manifestId && i.ItemId == itemId);
    }
    
    /// <summary>
    /// Save or update a manifest item
    /// </summary>
    public async Task<ManifestItem> SaveManifestItemAsync(ManifestItem item)
    {
        var existing = await Context.Set<ManifestItem>()
            .FirstOrDefaultAsync(i => i.ItemId == item.ItemId);
        
        if (existing != null)
        {
            Context.Entry(existing).CurrentValues.SetValues(item);
        }
        else
        {
            Context.Set<ManifestItem>().Add(item);
        }
        
        await Context.SaveChangesAsync();
        return item;
    }
    
    /// <summary>
    /// Update equipment location
    /// </summary>
    public async Task UpdateEquipmentLocationAsync(Guid equipmentId, Guid newLocationId)
    {
        var equipment = await Context.Equipment.FindAsync(equipmentId);
        if (equipment == null) return;
        
        var previousLocationId = equipment.CurrentLocationId;
        equipment.CurrentLocationId = newLocationId;
        equipment.UpdatedAt = DateTime.UtcNow;
        equipment.IsModifiedLocally = true;
        
        // Add history record
        var history = new EquipmentHistory
        {
            EquipmentId = equipmentId,
            Action = HistoryAction.LocationChanged,
            PerformedAt = DateTime.UtcNow,
            Description = "Location updated via shipment verification",
            OldValue = previousLocationId?.ToString(),
            NewValue = newLocationId.ToString()
        };
        
        Context.EquipmentHistory.Add(history);
        await AddToOfflineQueueAsync("Equipment", equipmentId, "Update", equipment);
        await Context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Save unregistered item (items added during verification not in system)
    /// </summary>
    public async Task<UnregisteredItem> SaveUnregisteredItemAsync(UnregisteredItem item)
    {
        var existing = await Context.Set<UnregisteredItem>()
            .FirstOrDefaultAsync(u => u.UnregisteredItemId == item.UnregisteredItemId);
        
        item.UpdatedAt = DateTime.UtcNow;
        
        if (existing != null)
        {
            Context.Entry(existing).CurrentValues.SetValues(item);
        }
        else
        {
            if (item.UnregisteredItemId == Guid.Empty)
                item.UnregisteredItemId = Guid.NewGuid();
            item.CreatedAt = DateTime.UtcNow;
            Context.Set<UnregisteredItem>().Add(item);
        }
        
        await Context.SaveChangesAsync();
        return item;
    }
    
    /// <summary>
    /// Get unregistered items pending review
    /// </summary>
    public async Task<List<UnregisteredItem>> GetUnregisteredItemsAsync(UnregisteredItemStatus? status = null)
    {
        var query = Context.Set<UnregisteredItem>()
            .Include(u => u.Manifest)
            .Include(u => u.CurrentLocation)
            .AsQueryable();
        
        if (status.HasValue)
            query = query.Where(u => u.Status == status);
        
        return await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
    }
    
    /// <summary>
    /// Save manifest notification
    /// </summary>
    public async Task<ManifestNotification> SaveManifestNotificationAsync(ManifestNotification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        
        if (notification.NotificationId == Guid.Empty)
            notification.NotificationId = Guid.NewGuid();
        
        Context.Set<ManifestNotification>().Add(notification);
        await Context.SaveChangesAsync();
        return notification;
    }
    
    /// <summary>
    /// Get manifest notifications
    /// </summary>
    public async Task<List<ManifestNotification>> GetManifestNotificationsAsync(
        Guid? manifestId = null,
        Guid? locationId = null,
        bool? requiresAction = null,
        bool? isResolved = null)
    {
        var query = Context.Set<ManifestNotification>()
            .Include(n => n.Manifest)
            .Include(n => n.Location)
            .Include(n => n.Equipment)
            .AsQueryable();
        
        if (manifestId.HasValue)
            query = query.Where(n => n.ManifestId == manifestId);
        if (locationId.HasValue)
            query = query.Where(n => n.LocationId == locationId);
        if (requiresAction.HasValue)
            query = query.Where(n => n.RequiresAction == requiresAction);
        if (isResolved.HasValue)
            query = query.Where(n => n.IsResolved == isResolved);
        
        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }
    
    /// <summary>
    /// Resolve a manifest notification
    /// </summary>
    public async Task ResolveNotificationAsync(Guid notificationId, Guid? resolvedBy = null, string? notes = null)
    {
        var notification = await Context.Set<ManifestNotification>()
            .FindAsync(notificationId);
        
        if (notification != null)
        {
            notification.IsResolved = true;
            notification.ResolvedAt = DateTime.UtcNow;
            notification.ResolvedBy = resolvedBy;
            notification.ResolutionNotes = notes;
            await Context.SaveChangesAsync();
        }
    }
    
    #endregion
    
    #region Verification Workflow Methods
    
    /// <summary>
    /// Add unregistered item (alias for SaveUnregisteredItemAsync for consistency)
    /// </summary>
    public Task<UnregisteredItem> AddUnregisteredItemAsync(UnregisteredItem item)
        => SaveUnregisteredItemAsync(item);
    
    /// <summary>
    /// Add manifest notification (alias for SaveManifestNotificationAsync for consistency)
    /// </summary>
    public Task<ManifestNotification> AddManifestNotificationAsync(ManifestNotification notification)
        => SaveManifestNotificationAsync(notification);
    
    /// <summary>
    /// Get manifest item by ID
    /// </summary>
    public async Task<ManifestItem?> GetManifestItemAsync(Guid itemId)
    {
        return await Context.Set<ManifestItem>()
            .Include(i => i.Equipment)
            .FirstOrDefaultAsync(i => i.ItemId == itemId);
    }
    
    /// <summary>
    /// Add manifest item
    /// </summary>
    public async Task<ManifestItem> AddManifestItemAsync(ManifestItem item)
    {
        if (item.ItemId == Guid.Empty)
            item.ItemId = Guid.NewGuid();
        item.CreatedAt = DateTime.UtcNow;
        
        Context.Set<ManifestItem>().Add(item);
        await Context.SaveChangesAsync();
        
        // Update manifest total items count
        var manifest = await Context.Manifests.FindAsync(item.ManifestId);
        if (manifest != null)
        {
            manifest.TotalItems = await Context.Set<ManifestItem>()
                .CountAsync(i => i.ManifestId == item.ManifestId);
            await Context.SaveChangesAsync();
        }
        
        return item;
    }
    
    /// <summary>
    /// Update manifest item
    /// </summary>
    public async Task UpdateManifestItemAsync(ManifestItem item)
    {
        var existing = await Context.Set<ManifestItem>().FindAsync(item.ItemId);
        if (existing != null)
        {
            Context.Entry(existing).CurrentValues.SetValues(item);
            await Context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Add manifest
    /// </summary>
    public async Task<Manifest> AddManifestAsync(Manifest manifest)
    {
        if (manifest.ManifestId == Guid.Empty)
            manifest.ManifestId = Guid.NewGuid();
        manifest.CreatedAt = DateTime.UtcNow;
        manifest.UpdatedAt = DateTime.UtcNow;
        
        Context.Manifests.Add(manifest);
        await Context.SaveChangesAsync();
        
        return manifest;
    }
    
    /// <summary>
    /// Update manifest
    /// </summary>
    public async Task UpdateManifestAsync(Manifest manifest)
    {
        var existing = await Context.Manifests.FindAsync(manifest.ManifestId);
        if (existing != null)
        {
            manifest.UpdatedAt = DateTime.UtcNow;
            Context.Entry(existing).CurrentValues.SetValues(manifest);
            await Context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Delete manifest and its items
    /// </summary>
    public async Task DeleteManifestAsync(Guid manifestId)
    {
        var manifest = await Context.Manifests
            .Include(m => m.Items)
            .Include(m => m.Photos)
            .FirstOrDefaultAsync(m => m.ManifestId == manifestId);
            
        if (manifest != null)
        {
            // Only allow deletion of Draft or Rejected manifests
            if (manifest.Status != ManifestStatus.Draft && manifest.Status != ManifestStatus.Rejected)
            {
                throw new InvalidOperationException(
                    $"Cannot delete manifest with status '{manifest.Status}'. Only Draft or Rejected manifests can be deleted.");
            }
            
            // Remove associated items
            if (manifest.Items?.Any() == true)
            {
                Context.Set<ManifestItem>().RemoveRange(manifest.Items);
            }
            
            // Remove associated photos
            if (manifest.Photos?.Any() == true)
            {
                Context.Set<ManifestPhoto>().RemoveRange(manifest.Photos);
            }
            
            // Remove the manifest
            Context.Manifests.Remove(manifest);
            await Context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Update equipment
    /// </summary>
    public async Task UpdateEquipmentAsync(Equipment equipment)
    {
        var existing = await Context.Equipment.FindAsync(equipment.EquipmentId);
        if (existing != null)
        {
            equipment.UpdatedAt = DateTime.UtcNow;
            equipment.IsModifiedLocally = true;
            Context.Entry(existing).CurrentValues.SetValues(equipment);
            await AddToOfflineQueueAsync("Equipment", equipment.EquipmentId, "Update", equipment);
            await Context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Add equipment event/history
    /// </summary>
    public async Task AddEquipmentEventAsync(EquipmentEvent equipmentEvent)
    {
        if (equipmentEvent.EventId == Guid.Empty)
            equipmentEvent.EventId = Guid.NewGuid();
        equipmentEvent.Timestamp = DateTime.UtcNow;
        
        Context.Set<EquipmentEvent>().Add(equipmentEvent);
        await Context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Generate manifest number with prefix
    /// </summary>
    public async Task<string> GenerateManifestNumberAsync(string prefix)
    {
        var year = DateTime.UtcNow.Year;
        var pattern = $"{prefix}-{year}-";
        
        var lastNumber = await Context.Manifests
            .Where(m => m.ManifestNumber.StartsWith(pattern))
            .OrderByDescending(m => m.ManifestNumber)
            .Select(m => m.ManifestNumber)
            .FirstOrDefaultAsync();
        
        int nextSequence = 1;
        if (lastNumber != null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int lastSeq))
            {
                nextSequence = lastSeq + 1;
            }
        }
        
        return $"{prefix}-{year}-{nextSequence:D5}";
    }
    
    #endregion

    public void Dispose()
    {
        _context?.Dispose();
        _context = null;
    }
}

public class DashboardStats
{
    public int TotalEquipment { get; set; }
    public int AvailableEquipment { get; set; }
    public int InTransitEquipment { get; set; }
    public int UnderRepairEquipment { get; set; }
    public int ExpiringCertifications { get; set; }
    public int OverdueCertifications { get; set; }
    public int DueCalibrations { get; set; }
    public int LowStockItems { get; set; }
    public int PendingManifests { get; set; }
    public int InTransitManifests { get; set; }
    public int ManifestsThisMonth { get; set; }
    
    // Defect Report Stats
    public int OpenDefects { get; set; }
    public int CriticalDefects { get; set; }
    public int DefectsThisMonth { get; set; }
    public int ResolvedDefectsThisMonth { get; set; }
}
