// LicensingSystem.Server/Services/WebhookService.cs
// Webhook notification service for license events
// Sends HTTP POST notifications to configured endpoints when license events occur

using Microsoft.EntityFrameworkCore;
using LicensingSystem.Server.Data;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace LicensingSystem.Server.Services;

/// <summary>
/// Webhook event types for license lifecycle events.
/// </summary>
public enum WebhookEventType
{
    LicenseActivated,
    LicenseDeactivated,
    LicenseExpired,
    LicenseRevoked,
    LicenseRenewed,
    CertificateIssued,
    SessionStarted,
    SessionEnded
}

public interface IWebhookService
{
    /// <summary>
    /// Sends a webhook notification for a license event.
    /// </summary>
    Task SendWebhookAsync(WebhookEventType eventType, object payload);

    /// <summary>
    /// Sends a license activated webhook.
    /// </summary>
    Task NotifyLicenseActivatedAsync(string licenseId, string customerEmail, string? machineName);

    /// <summary>
    /// Sends a license deactivated webhook.
    /// </summary>
    Task NotifyLicenseDeactivatedAsync(string licenseId, string reason);

    /// <summary>
    /// Sends a license expired webhook.
    /// </summary>
    Task NotifyLicenseExpiredAsync(string licenseId, string customerEmail, DateTime expiresAt);

    /// <summary>
    /// Sends a license revoked webhook.
    /// </summary>
    Task NotifyLicenseRevokedAsync(string licenseId, string customerEmail, string reason);

    /// <summary>
    /// Sends a license renewed webhook.
    /// </summary>
    Task NotifyLicenseRenewedAsync(string licenseId, string customerEmail, DateTime newExpiresAt);

    /// <summary>
    /// Sends a certificate issued webhook.
    /// </summary>
    Task NotifyCertificateIssuedAsync(string certificateId, string licenseId, string moduleId, string projectName);

    /// <summary>
    /// Gets webhook delivery history.
    /// </summary>
    Task<List<WebhookDeliveryRecord>> GetDeliveryHistoryAsync(int count = 50);

    /// <summary>
    /// Retries failed webhook deliveries.
    /// </summary>
    Task RetryFailedWebhooksAsync();

    /// <summary>
    /// Gets configured webhooks.
    /// </summary>
    List<WebhookEndpointConfig> GetConfiguredWebhooks();
}

public class WebhookService : IWebhookService
{
    private readonly LicenseDbContext _db;
    private readonly ILogger<WebhookService> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly List<WebhookEndpointConfig> _webhookEndpoints;

    // Signing key for webhook payloads
    private readonly string? _signingKey;

    public WebhookService(
        LicenseDbContext db,
        ILogger<WebhookService> logger,
        IConfiguration config,
        IHttpClientFactory? httpClientFactory = null)
    {
        _db = db;
        _logger = logger;
        _config = config;

        // Create HTTP client with timeout
        _httpClient = httpClientFactory?.CreateClient("Webhooks") ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Load webhook configuration
        _webhookEndpoints = LoadWebhookConfig();
        _signingKey = Environment.GetEnvironmentVariable("WEBHOOK_SIGNING_KEY")
            ?? _config["Webhooks:SigningKey"];
    }

    private List<WebhookEndpointConfig> LoadWebhookConfig()
    {
        var endpoints = new List<WebhookEndpointConfig>();

        // Load from environment variables (for production)
        var envWebhookUrl = Environment.GetEnvironmentVariable("WEBHOOK_URL");
        if (!string.IsNullOrEmpty(envWebhookUrl))
        {
            var eventsEnv = Environment.GetEnvironmentVariable("WEBHOOK_EVENTS") ?? "*";
            var events = eventsEnv == "*"
                ? Enum.GetValues<WebhookEventType>().ToList()
                : eventsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => Enum.TryParse<WebhookEventType>(e.Trim(), out var et) ? et : (WebhookEventType?)null)
                    .Where(e => e.HasValue)
                    .Select(e => e!.Value)
                    .ToList();

            endpoints.Add(new WebhookEndpointConfig
            {
                Name = "Primary",
                Url = envWebhookUrl,
                Secret = Environment.GetEnvironmentVariable("WEBHOOK_SECRET"),
                Events = events,
                IsActive = true
            });
        }

        // Load from configuration (for development)
        var configSection = _config.GetSection("Webhooks:Endpoints");
        if (configSection.Exists())
        {
            var configEndpoints = configSection.Get<List<WebhookEndpointConfig>>() ?? new List<WebhookEndpointConfig>();
            endpoints.AddRange(configEndpoints.Where(e => e.IsActive && !string.IsNullOrEmpty(e.Url)));
        }

        return endpoints;
    }

    public async Task SendWebhookAsync(WebhookEventType eventType, object payload)
    {
        if (!_webhookEndpoints.Any())
        {
            _logger.LogDebug("No webhook endpoints configured, skipping notification for {EventType}", eventType);
            return;
        }

        var relevantEndpoints = _webhookEndpoints
            .Where(e => e.IsActive && (e.Events.Contains(eventType) || !e.Events.Any()))
            .ToList();

        if (!relevantEndpoints.Any())
        {
            _logger.LogDebug("No webhooks configured for event type {EventType}", eventType);
            return;
        }

        var webhookPayload = new WebhookPayload
        {
            Id = Guid.NewGuid().ToString("N"),
            EventType = eventType.ToString(),
            Timestamp = DateTime.UtcNow,
            Data = payload
        };

        var json = JsonSerializer.Serialize(webhookPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        foreach (var endpoint in relevantEndpoints)
        {
            await DeliverWebhookAsync(endpoint, webhookPayload.Id, eventType, json);
        }
    }

    private async Task DeliverWebhookAsync(
        WebhookEndpointConfig endpoint,
        string webhookId,
        WebhookEventType eventType,
        string jsonPayload)
    {
        var deliveryRecord = new WebhookDeliveryRecord
        {
            WebhookId = webhookId,
            EndpointName = endpoint.Name ?? "Unknown",
            EndpointUrl = endpoint.Url,
            EventType = eventType.ToString(),
            Payload = jsonPayload,
            AttemptedAt = DateTime.UtcNow
        };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            // Add standard headers
            request.Headers.Add("X-Webhook-Id", webhookId);
            request.Headers.Add("X-Webhook-Event", eventType.ToString());
            request.Headers.Add("X-Webhook-Timestamp", DateTime.UtcNow.ToString("O"));

            // Add signature if secret is configured
            if (!string.IsNullOrEmpty(endpoint.Secret))
            {
                var signature = ComputeSignature(jsonPayload, endpoint.Secret);
                request.Headers.Add("X-Webhook-Signature", signature);
            }

            // Add global signature if configured
            if (!string.IsNullOrEmpty(_signingKey))
            {
                var globalSignature = ComputeSignature(jsonPayload, _signingKey);
                request.Headers.Add("X-Webhook-Signature-256", globalSignature);
            }

            _logger.LogInformation("Sending webhook {WebhookId} ({EventType}) to {Endpoint}",
                webhookId, eventType, endpoint.Url);

            var response = await _httpClient.SendAsync(request);

            deliveryRecord.ResponseStatusCode = (int)response.StatusCode;
            deliveryRecord.Success = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                deliveryRecord.ResponseBody = responseBody.Length > 1000
                    ? responseBody[..1000]
                    : responseBody;

                _logger.LogWarning("Webhook delivery failed: {WebhookId} to {Endpoint} - Status: {StatusCode}",
                    webhookId, endpoint.Url, response.StatusCode);
            }
            else
            {
                _logger.LogInformation("Webhook delivered successfully: {WebhookId} to {Endpoint}",
                    webhookId, endpoint.Url);
            }
        }
        catch (TaskCanceledException)
        {
            deliveryRecord.Success = false;
            deliveryRecord.ErrorMessage = "Request timed out";
            _logger.LogWarning("Webhook delivery timed out: {WebhookId} to {Endpoint}",
                webhookId, endpoint.Url);
        }
        catch (HttpRequestException ex)
        {
            deliveryRecord.Success = false;
            deliveryRecord.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Webhook delivery failed: {WebhookId} to {Endpoint}",
                webhookId, endpoint.Url);
        }
        catch (Exception ex)
        {
            deliveryRecord.Success = false;
            deliveryRecord.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Unexpected error delivering webhook: {WebhookId}", webhookId);
        }

        // Store delivery record
        try
        {
            _db.WebhookDeliveries.Add(deliveryRecord);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store webhook delivery record");
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    public async Task NotifyLicenseActivatedAsync(string licenseId, string customerEmail, string? machineName)
    {
        await SendWebhookAsync(WebhookEventType.LicenseActivated, new
        {
            licenseId,
            customerEmail = MaskEmail(customerEmail),
            machineName,
            activatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyLicenseDeactivatedAsync(string licenseId, string reason)
    {
        await SendWebhookAsync(WebhookEventType.LicenseDeactivated, new
        {
            licenseId,
            reason,
            deactivatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyLicenseExpiredAsync(string licenseId, string customerEmail, DateTime expiresAt)
    {
        await SendWebhookAsync(WebhookEventType.LicenseExpired, new
        {
            licenseId,
            customerEmail = MaskEmail(customerEmail),
            expiredAt = expiresAt,
            notifiedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyLicenseRevokedAsync(string licenseId, string customerEmail, string reason)
    {
        await SendWebhookAsync(WebhookEventType.LicenseRevoked, new
        {
            licenseId,
            customerEmail = MaskEmail(customerEmail),
            reason,
            revokedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyLicenseRenewedAsync(string licenseId, string customerEmail, DateTime newExpiresAt)
    {
        await SendWebhookAsync(WebhookEventType.LicenseRenewed, new
        {
            licenseId,
            customerEmail = MaskEmail(customerEmail),
            newExpiresAt,
            renewedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyCertificateIssuedAsync(string certificateId, string licenseId, string moduleId, string projectName)
    {
        await SendWebhookAsync(WebhookEventType.CertificateIssued, new
        {
            certificateId,
            licenseId,
            moduleId,
            projectName,
            issuedAt = DateTime.UtcNow
        });
    }

    public async Task<List<WebhookDeliveryRecord>> GetDeliveryHistoryAsync(int count = 50)
    {
        return await _db.WebhookDeliveries
            .OrderByDescending(w => w.AttemptedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task RetryFailedWebhooksAsync()
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var failedWebhooks = await _db.WebhookDeliveries
            .Where(w => !w.Success && w.AttemptedAt > cutoff && w.RetryCount < 3)
            .ToListAsync();

        _logger.LogInformation("Retrying {Count} failed webhooks", failedWebhooks.Count);

        foreach (var delivery in failedWebhooks)
        {
            var endpoint = _webhookEndpoints.FirstOrDefault(e => e.Url == delivery.EndpointUrl);
            if (endpoint == null)
            {
                continue;
            }

            delivery.RetryCount++;
            delivery.AttemptedAt = DateTime.UtcNow;

            if (Enum.TryParse<WebhookEventType>(delivery.EventType, out var eventType))
            {
                await DeliverWebhookAsync(endpoint, delivery.WebhookId, eventType, delivery.Payload);
            }
        }

        await _db.SaveChangesAsync();
    }

    public List<WebhookEndpointConfig> GetConfiguredWebhooks()
    {
        return _webhookEndpoints.Select(e => new WebhookEndpointConfig
        {
            Name = e.Name,
            Url = e.Url,
            Events = e.Events,
            IsActive = e.IsActive
            // Don't return secret
        }).ToList();
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";

        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];

        var maskedName = name.Length <= 2 ? "**" : $"{name[0]}***{name[^1]}";
        return $"{maskedName}@{domain}";
    }
}

// ============================================================================
// Webhook DTOs and Models
// ============================================================================

/// <summary>
/// Configuration for a webhook endpoint.
/// </summary>
public class WebhookEndpointConfig
{
    public string? Name { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public List<WebhookEventType> Events { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Webhook payload wrapper.
/// </summary>
public class WebhookPayload
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public object Data { get; set; } = new();
}

/// <summary>
/// Record of webhook delivery attempt.
/// </summary>
public class WebhookDeliveryRecord
{
    public int Id { get; set; }
    public string WebhookId { get; set; } = string.Empty;
    public string EndpointName { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
    public bool Success { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
