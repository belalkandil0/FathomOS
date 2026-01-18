// LicensingSystem.Server/Program.cs
// ASP.NET Core server setup - Configured for Render.com
// Updated v3.4.9 with Customer Portal, Security, Health Monitoring, First-Time Setup

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Services;
using LicensingSystem.Server.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Fathom OS License API", Version = "v3.4.9" });
});
builder.Services.AddHttpContextAccessor();

// Add database context - handle database path properly
var dbPath = Environment.GetEnvironmentVariable("DB_PATH");
Console.WriteLine($"DB_PATH env var: '{dbPath ?? "(not set)"}'");

// Determine the best database location
if (string.IsNullOrWhiteSpace(dbPath))
{
    // Try /app/data first (for persistent storage)
    var preferredPath = "/app/data/licenses.db";
    var preferredDir = Path.GetDirectoryName(preferredPath)!;
    
    if (Directory.Exists(preferredDir))
    {
        dbPath = preferredPath;
        Console.WriteLine($"Using existing data directory: {preferredDir}");
    }
    else
    {
        // Try to create it
        try
        {
            Directory.CreateDirectory(preferredDir);
            dbPath = preferredPath;
            Console.WriteLine($"Created data directory: {preferredDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cannot create {preferredDir}: {ex.Message}");
            // Fall back to current working directory
            dbPath = Path.Combine(Directory.GetCurrentDirectory(), "licenses.db");
            Console.WriteLine($"Falling back to: {dbPath}");
        }
    }
}
else
{
    // Use the provided path, ensure directory exists
    var dir = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
    {
        try
        {
            Directory.CreateDirectory(dir);
            Console.WriteLine($"Created directory from DB_PATH: {dir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cannot create directory from DB_PATH: {ex.Message}");
            dbPath = "licenses.db";
        }
    }
}

Console.WriteLine($"Final database path: {dbPath}");

builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ==================== Register Services ====================

// Core license service
builder.Services.AddScoped<ILicenseService, LicenseService>();

// Audit logging (register first as other services depend on it)
builder.Services.AddScoped<IAuditService, AuditService>();

// Security services
builder.Services.AddScoped<IRateLimitService, RateLimitService>();

// Session tracking
builder.Services.AddScoped<ISessionService, SessionService>();

// Health monitoring
builder.Services.AddScoped<IHealthMonitorService, HealthMonitorService>();

// Backup service
builder.Services.AddScoped<IBackupService, BackupService>();

// QR Code service
builder.Services.AddScoped<IQrCodeService, QrCodeService>();

// License obfuscation
builder.Services.AddScoped<ILicenseObfuscationService, LicenseObfuscationService>();

// First-time admin setup service
builder.Services.AddScoped<ISetupService, SetupService>();

// Admin setup file service (for offline/file-based deployments)
builder.Services.AddScoped<IAdminSetupFileService, AdminSetupFileService>();

// ==================== CORS ====================

// Add CORS for desktop app and customer portal
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Enable Swagger in all environments for now (can disable later)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fathom OS License API v3.3");
    c.RoutePrefix = "swagger";
});

// Don't use HTTPS redirection on Render (handled by their proxy)
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

// Serve static files for customer portal
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AllowAll");

// Setup middleware - redirects to setup page if first-time setup is required
app.UseSetupMiddleware();

// Map /setup to serve setup.html
app.MapGet("/setup", async context =>
{
    context.Response.ContentType = "text/html";
    var setupPath = Path.Combine(app.Environment.WebRootPath, "setup.html");
    await context.Response.SendFileAsync(setupPath);
});

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Root endpoint for quick testing
app.MapGet("/", () => Results.Ok(new 
{ 
    service = "FathomOS License Server",
    status = "running",
    version = "3.4.9",
    features = new[] 
    {
        "License Management",
        "Customer Portal",
        "Session Tracking",
        "Health Monitoring",
        "Audit Logging",
        "Database Backup"
    },
    docs = "/swagger",
    portal = "/portal",
    debug = "/db-status"
}));

// Database status endpoint for debugging
app.MapGet("/db-status", async (LicenseDbContext db) => 
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        var licenseCount = canConnect ? await db.LicenseKeys.CountAsync() : -1;
        var activationCount = canConnect ? await db.LicenseActivations.CountAsync() : -1;
        var transferCount = canConnect ? await db.LicenseTransfers.CountAsync() : -1;
        var auditLogCount = canConnect ? await db.AuditLogs.CountAsync() : -1;
        
        return Results.Ok(new
        {
            status = "ok",
            canConnect,
            licenseCount,
            activationCount,
            transferCount,
            auditLogCount,
            dbPath = dbPath,
            workingDirectory = Directory.GetCurrentDirectory(),
            timestamp = DateTime.UtcNow,
            version = "3.4.8"
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "error",
            canConnect = false,
            error = ex.Message,
            innerError = ex.InnerException?.Message,
            dbPath = dbPath,
            workingDirectory = Directory.GetCurrentDirectory(),
            timestamp = DateTime.UtcNow
        });
    }
});

// Ensure database is created with detailed error handling
Console.WriteLine("Initializing database...");
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
    
    // Check if we can connect
    var canConnect = db.Database.CanConnect();
    Console.WriteLine($"Database can connect: {canConnect}");
    
    // Create database if needed
    var created = db.Database.EnsureCreated();
    Console.WriteLine($"Database created: {created}");
    
    // Verify tables exist
    var tableCount = db.Model.GetEntityTypes().Count();
    Console.WriteLine($"Entity types configured: {tableCount}");
    
    Console.WriteLine("[OK] Database initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Database initialization error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
    }
    Console.WriteLine($"   Stack trace: {ex.StackTrace}");
    // Continue running - endpoints will show more specific errors
}

// ==================== First-Time Admin Setup ====================

// Step 1: Check for pre-seeded admin credentials file (for offline deployments)
Console.WriteLine("Checking for pre-seeded admin credentials file...");
try
{
    using var fileSetupScope = app.Services.CreateScope();
    var fileSetupService = fileSetupScope.ServiceProvider.GetService<IAdminSetupFileService>();
    var setupServiceForFile = fileSetupScope.ServiceProvider.GetRequiredService<ISetupService>();

    if (fileSetupService != null)
    {
        var credentials = await fileSetupService.ReadSetupFileAsync();

        if (credentials != null && await setupServiceForFile.IsSetupRequiredAsync())
        {
            Console.WriteLine("");
            Console.WriteLine("========================================");
            Console.WriteLine("   FILE-BASED ADMIN SETUP DETECTED");
            Console.WriteLine("========================================");
            Console.WriteLine($"   Found admin-credentials.json");
            Console.WriteLine($"   Email: {credentials.Email}");
            Console.WriteLine($"   Username: {credentials.Username}");
            Console.WriteLine("   Creating admin account from file...");

            var result = await setupServiceForFile.CompleteSetupAsync(new SetupCompletionRequest
            {
                Email = credentials.Email,
                Username = credentials.Username,
                Password = credentials.Password,
                DisplayName = credentials.DisplayName ?? credentials.Username
            }, "file-based-setup");

            if (result.Success)
            {
                Console.WriteLine("");
                Console.WriteLine("[OK] Admin account created from admin-credentials.json");
                Console.WriteLine("   Securely deleting credentials file...");
                await fileSetupService.DeleteSetupFileAsync();
                Console.WriteLine("[OK] Credentials file deleted");
                Console.WriteLine("========================================");
                Console.WriteLine("");

                if (credentials.ForcePasswordChange)
                {
                    Console.WriteLine("[INFO] ForcePasswordChange is enabled - user should change password on first login");
                }
            }
            else
            {
                Console.WriteLine($"[WARNING] Failed to create admin from file: {result.ErrorMessage}");
                Console.WriteLine("   The credentials file will NOT be deleted.");
                Console.WriteLine("   Fix the issue and restart the server.");
                Console.WriteLine("========================================");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[WARNING] File-based setup check error: {ex.Message}");
    // Continue - the existing setup check will handle things
}

// Step 2: Check existing setup status and offer alternatives
Console.WriteLine("Checking first-time setup status...");
try
{
    using var setupScope = app.Services.CreateScope();
    var setupService = setupScope.ServiceProvider.GetRequiredService<ISetupService>();

    var setupRequired = await setupService.IsSetupRequiredAsync();

    if (setupRequired)
    {
        Console.WriteLine("");
        Console.WriteLine("========================================");
        Console.WriteLine("   FIRST-TIME SETUP REQUIRED");
        Console.WriteLine("========================================");

        // Try auto-setup from environment variables
        var autoSetupSuccess = await setupService.TryAutoSetupFromEnvironmentAsync();

        if (autoSetupSuccess)
        {
            Console.WriteLine("[OK] Auto-setup from environment variables completed!");
            Console.WriteLine("   Admin account created from ADMIN_EMAIL, ADMIN_USERNAME, ADMIN_PASSWORD");
        }
        else
        {
            // Generate and display setup token
            var setupToken = await setupService.GenerateSetupTokenAsync();

            Console.WriteLine("");
            Console.WriteLine("   No admin account exists. Complete setup to continue.");
            Console.WriteLine("");
            Console.WriteLine("   OPTION 1: Use the Web Setup Wizard");
            Console.WriteLine("   Navigate to: http://localhost:5000/setup");
            Console.WriteLine("");
            Console.WriteLine("   Setup Token (valid for 24 hours):");
            Console.WriteLine($"   {setupToken}");
            Console.WriteLine("");
            Console.WriteLine("   OPTION 2: Use the Desktop UI Manager");
            Console.WriteLine("   Launch the License Manager UI - it will prompt for setup");
            Console.WriteLine("   (No token required when running on same machine)");
            Console.WriteLine("");
            Console.WriteLine("   OPTION 3: Use Environment Variables");
            Console.WriteLine("   Set ADMIN_EMAIL, ADMIN_USERNAME, ADMIN_PASSWORD and restart");
            Console.WriteLine("");
            Console.WriteLine("   OPTION 4: Use admin-credentials.json (for offline deployments)");
            Console.WriteLine("   Place file in data directory and restart server");
            Console.WriteLine("");
            Console.WriteLine("========================================");
            Console.WriteLine("");
        }
    }
    else
    {
        Console.WriteLine("[OK] Setup already completed - admin account exists");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[WARNING] Setup check error: {ex.Message}");
    // Continue running - the middleware will handle setup checks
}

// Start background cleanup task
_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            await Task.Delay(TimeSpan.FromHours(1));
            
            using var scope = app.Services.CreateScope();
            
            // Cleanup rate limits
            var rateLimitService = scope.ServiceProvider.GetRequiredService<IRateLimitService>();
            await rateLimitService.CleanupExpiredAsync();
            
            // Cleanup inactive sessions
            var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
            await sessionService.CleanupInactiveSessionsAsync(TimeSpan.FromMinutes(10));
            
            // Cleanup old health metrics
            var healthService = scope.ServiceProvider.GetRequiredService<IHealthMonitorService>();
            await healthService.CleanupOldMetricsAsync(30);
            
            Console.WriteLine($"[{DateTime.UtcNow:u}] Background cleanup completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.UtcNow:u}] Background cleanup error: {ex.Message}");
        }
    }
});

app.Run();
