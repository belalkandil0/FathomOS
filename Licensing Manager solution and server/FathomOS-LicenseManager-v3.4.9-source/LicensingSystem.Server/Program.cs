// LicensingSystem.Server/Program.cs
// FathomOS License Server - OPTIONAL tracking server
// License validation is performed OFFLINE in the FathomOS app
// This server is for: license record storage, certificate verification, analytics
// Updated v3.5.0 - Simplified with API key authentication

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using LicensingSystem.Server.Services;
using LicensingSystem.Server.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ==================== Services ====================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FathomOS License Server", Version = "v3.5.0" });
});
builder.Services.AddHttpContextAccessor();

// ==================== Database ====================

var dbPath = Environment.GetEnvironmentVariable("DB_PATH");

if (string.IsNullOrWhiteSpace(dbPath))
{
    // Try /app/data first (for persistent storage in containers)
    var preferredPath = "/app/data/licenses.db";
    var preferredDir = Path.GetDirectoryName(preferredPath)!;

    if (Directory.Exists(preferredDir))
    {
        dbPath = preferredPath;
    }
    else
    {
        try
        {
            Directory.CreateDirectory(preferredDir);
            dbPath = preferredPath;
        }
        catch
        {
            // Fall back to current working directory
            dbPath = Path.Combine(Directory.GetCurrentDirectory(), "licenses.db");
        }
    }
}
else
{
    var dir = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
    {
        try { Directory.CreateDirectory(dir); }
        catch { dbPath = "licenses.db"; }
    }
}

builder.Services.AddDbContext<LicenseDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ==================== Register Services ====================

// API Key authentication service
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// Core license service
builder.Services.AddScoped<ILicenseService, LicenseService>();

// Audit logging
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

// Webhook notification service
builder.Services.AddHttpClient("Webhooks", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<IWebhookService, WebhookService>();

// DEPRECATED: Setup service (kept for backward compatibility)
builder.Services.AddScoped<ISetupService, SetupService>();

// ==================== CORS ====================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// ==================== Middleware Pipeline ====================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FathomOS License Server v3.5.0");
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

// API Key authentication middleware (replaces old setup/admin auth)
app.UseApiKeyAuth();

app.UseAuthorization();
app.MapControllers();

// ==================== Health Endpoint ====================

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    version = "3.5.0",
    mode = "tracking",
    message = "License validation is performed offline. This server is for tracking only."
}));

// ==================== Root Endpoint ====================

app.MapGet("/", () => Results.Ok(new
{
    service = "FathomOS License Server",
    status = "running",
    version = "3.5.0",
    mode = "tracking",
    description = "License validation is performed OFFLINE in FathomOS app. This server provides license tracking and certificate verification.",
    endpoints = new
    {
        admin = new[]
        {
            "POST /api/admin/licenses/sync - Sync license records (requires X-API-Key)",
            "GET  /api/admin/licenses - List all licenses (requires X-API-Key)",
            "GET  /api/admin/licenses/{id} - Get license details (requires X-API-Key)",
            "GET  /api/admin/stats - Dashboard statistics (requires X-API-Key)"
        },
        analytics = new[]
        {
            "GET  /api/analytics/licenses/stats - License statistics (requires X-API-Key)",
            "GET  /api/analytics/usage/{licenseId} - License usage stats (requires X-API-Key)",
            "GET  /api/analytics/activations - Activation trends (requires X-API-Key)",
            "GET  /api/analytics/expiring - Expiring licenses (requires X-API-Key)",
            "GET  /api/analytics/certificates - Certificate analytics (requires X-API-Key)",
            "GET  /api/analytics/dashboard - Analytics dashboard (requires X-API-Key)"
        },
        health = new[]
        {
            "GET  /api/health - Basic health check (public)",
            "GET  /api/health/detailed - Detailed health (public)",
            "GET  /api/health/database - Database health (public)",
            "GET  /api/health/metrics - Server metrics (public)",
            "GET  /api/health/ready - Readiness probe (public)",
            "GET  /api/health/live - Liveness probe (public)"
        },
        certificates = new[]
        {
            "GET  /api/certificates/verify/{id} - Verify certificate (public)",
            "POST /api/certificates/verify/batch - Batch verify (public)",
            "GET  /api/certificates/stats/{licenseeCode} - Certificate stats (requires X-API-Key)",
            "GET  /api/certificates/search - Search certificates (requires X-API-Key)",
            "POST /api/certificates/sync - Sync certificates (requires X-API-Key)"
        },
        legacy = new[]
        {
            "GET  /health - Server health status (public)",
            "GET  /db-status - Database status (public)"
        }
    },
    docs = "/swagger"
}));

// ==================== Database Status ====================

app.MapGet("/db-status", async (LicenseDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        var licenseCount = canConnect ? await db.LicenseKeys.CountAsync() : -1;
        var syncedLicenseCount = canConnect ? await db.SyncedLicenses.CountAsync() : -1;
        var certificateCount = canConnect ? await db.Certificates.CountAsync() : -1;

        return Results.Ok(new
        {
            status = "ok",
            canConnect,
            licenseCount,
            syncedLicenseCount,
            certificateCount,
            dbPath,
            mode = "tracking",
            timestamp = DateTime.UtcNow,
            version = "3.5.0"
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "error",
            canConnect = false,
            error = ex.Message,
            dbPath,
            timestamp = DateTime.UtcNow
        });
    }
});

// ==================== Database Initialization ====================

Console.WriteLine();
Console.WriteLine("Initializing FathomOS License Server...");

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();

    // Create database if needed
    var created = db.Database.EnsureCreated();
    if (created)
    {
        Console.WriteLine("[OK] Database created");
    }

    Console.WriteLine("[OK] Database initialized successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Database initialization error: {ex.Message}");
}

// ==================== API Key Initialization ====================

string? displayApiKey = null;
bool isFirstRun = false;

try
{
    using var scope = app.Services.CreateScope();
    var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

    var (apiKey, firstRun) = await apiKeyService.GetOrCreateApiKeyAsync();
    displayApiKey = apiKey;
    isFirstRun = firstRun;
}
catch (Exception ex)
{
    Console.WriteLine($"[WARNING] API key initialization error: {ex.Message}");
}

// ==================== Startup Banner ====================

Console.WriteLine();
Console.WriteLine("+==================================================================+");
Console.WriteLine("|                 FathomOS License Server v3.5.0                  |");
Console.WriteLine("+==================================================================+");
Console.WriteLine("|  Status: Running                                                |");
Console.WriteLine("|  Mode:   License Tracking (validation is offline)               |");
Console.WriteLine("|                                                                  |");
Console.WriteLine("|  Endpoints:                                                      |");
Console.WriteLine("|    * POST /api/admin/licenses/sync - Sync license records       |");
Console.WriteLine("|    * GET  /api/admin/licenses - List licenses                   |");
Console.WriteLine("|    * GET  /api/certificates/verify/{id} - Public verification   |");
Console.WriteLine("|                                                                  |");
Console.WriteLine("|  Protected endpoints require X-API-Key header                   |");

if (isFirstRun && !string.IsNullOrEmpty(displayApiKey))
{
    Console.WriteLine("|                                                                  |");
    Console.WriteLine("+------------------------------------------------------------------+");
    Console.WriteLine("|  API Key (SAVE THIS - shown only once):                          |");
    Console.WriteLine($"|  {displayApiKey,-62} |");
    Console.WriteLine("+------------------------------------------------------------------+");
    Console.WriteLine("|  Use this key in License Generator UI to connect.               |");
}
else
{
    var envKeySet = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ADMIN_API_KEY"));
    if (envKeySet)
    {
        Console.WriteLine("|                                                                  |");
        Console.WriteLine("|  API Key: Using ADMIN_API_KEY environment variable              |");
    }
    else
    {
        using var scope = app.Services.CreateScope();
        var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
        var hint = await apiKeyService.GetApiKeyHintAsync();
        if (hint != null)
        {
            Console.WriteLine("|                                                                  |");
            Console.WriteLine($"|  API Key: {hint,-52} |");
        }
    }
}

Console.WriteLine("+==================================================================+");
Console.WriteLine();

// ==================== Background Cleanup Task ====================

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
