// FathomOS.Core/Services/ApprovalService.cs
// Implementation of the shared approval/signatory workflow service
// Used by all calibration modules for consistent approval handling

using FathomOS.Core.Logging;
using FathomOS.Core.Models;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FathomOS.Core.Services;

/// <summary>
/// In-memory implementation of the approval service.
/// Manages signatories and approval workflows for calibration modules.
/// Can be extended to support persistent storage in the future.
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, Signatory> _signatories;
    private readonly ConcurrentDictionary<string, ApprovalRequest> _pendingRequests;
    private readonly ConcurrentDictionary<string, ApprovalResult> _approvalResults;
    private readonly string? _persistencePath;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #region Events

    /// <inheritdoc />
    public event EventHandler<Signatory>? SignatoryChanged;

    /// <inheritdoc />
    public event EventHandler<string>? SignatoryDeleted;

    /// <inheritdoc />
    public event EventHandler<ApprovalResult>? ApprovalCompleted;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of ApprovalService without logging or persistence.
    /// </summary>
    public ApprovalService()
    {
        _logger = null;
        _persistencePath = null;
        _signatories = new ConcurrentDictionary<string, Signatory>();
        _pendingRequests = new ConcurrentDictionary<string, ApprovalRequest>();
        _approvalResults = new ConcurrentDictionary<string, ApprovalResult>();
    }

    /// <summary>
    /// Initializes a new instance of ApprovalService with logging support.
    /// </summary>
    /// <param name="logger">The logger instance for recording operations and errors.</param>
    public ApprovalService(ILogger logger)
        : this()
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of ApprovalService with logging and persistence support.
    /// </summary>
    /// <param name="logger">The logger instance for recording operations and errors.</param>
    /// <param name="persistencePath">Path to the directory for storing signatory data.</param>
    public ApprovalService(ILogger? logger, string persistencePath)
        : this()
    {
        _logger = logger;
        _persistencePath = persistencePath;

        // Load persisted signatories if available
        LoadSignatoriesFromDisk();
    }

    #endregion

    #region Approval Workflow

    /// <inheritdoc />
    public Task<ApprovalResult> RequestApprovalAsync(ApprovalRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _logger?.Debug($"Processing approval request: {request.RequestId} for document: {request.DocumentId}", nameof(ApprovalService));

        // Check if request has expired
        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value < DateTime.UtcNow)
        {
            _logger?.Info($"Approval request {request.RequestId} has expired", nameof(ApprovalService));
            var expiredResult = ApprovalResult.CreateExpired(request.RequestId, request.DocumentId);
            _approvalResults[request.RequestId] = expiredResult;
            return Task.FromResult(expiredResult);
        }

        // Store the pending request
        _pendingRequests[request.RequestId] = request;

        // Create initial pending result
        var result = new ApprovalResult
        {
            RequestId = request.RequestId,
            DocumentId = request.DocumentId,
            Status = ApprovalStatus.Pending
        };

        _approvalResults[request.RequestId] = result;

        _logger?.Info($"Approval request {request.RequestId} created for document: {request.DocumentTitle}", nameof(ApprovalService));

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ApprovalResult> AddSignatureAsync(string requestId, Signatory signatory, string? comments = null)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID cannot be empty", nameof(requestId));

        if (signatory == null)
            throw new ArgumentNullException(nameof(signatory));

        _logger?.Debug($"Adding signature to request {requestId} from {signatory.Name}", nameof(ApprovalService));

        if (!_approvalResults.TryGetValue(requestId, out var result))
        {
            throw new InvalidOperationException($"Approval request {requestId} not found");
        }

        if (!_pendingRequests.TryGetValue(requestId, out var request))
        {
            throw new InvalidOperationException($"Approval request {requestId} not found in pending requests");
        }

        // Add the signature
        var signature = new ApprovalSignature
        {
            RequestId = requestId,
            Signatory = signatory,
            Comments = comments,
            SignedAt = DateTime.UtcNow,
            SignedFrom = Environment.MachineName
        };

        result.Signatures.Add(signature);

        // Check if we have enough signatures
        if (result.Signatures.Count >= request.MinimumSignaturesRequired)
        {
            result.Status = ApprovalStatus.Approved;
            result.CompletedAt = DateTime.UtcNow;
            _pendingRequests.TryRemove(requestId, out _);

            _logger?.Info($"Approval request {requestId} approved with {result.Signatures.Count} signature(s)", nameof(ApprovalService));

            // Record signatory usage
            _ = RecordSignatoryUsageAsync(signatory.Id);

            ApprovalCompleted?.Invoke(this, result);
        }
        else
        {
            _logger?.Debug($"Approval request {requestId} has {result.Signatures.Count}/{request.MinimumSignaturesRequired} signatures", nameof(ApprovalService));
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ApprovalResult> RejectApprovalAsync(string requestId, string reason, Signatory? rejectedBy = null)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID cannot be empty", nameof(requestId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason cannot be empty", nameof(reason));

        _logger?.Debug($"Rejecting approval request {requestId}: {reason}", nameof(ApprovalService));

        if (!_approvalResults.TryGetValue(requestId, out var result))
        {
            throw new InvalidOperationException($"Approval request {requestId} not found");
        }

        result.Status = ApprovalStatus.Rejected;
        result.RejectionReason = reason;
        result.CompletedAt = DateTime.UtcNow;

        if (rejectedBy != null)
        {
            result.Signatures.Add(new ApprovalSignature
            {
                RequestId = requestId,
                Signatory = rejectedBy,
                Comments = reason,
                SignedAt = DateTime.UtcNow
            });
        }

        _pendingRequests.TryRemove(requestId, out _);

        _logger?.Info($"Approval request {requestId} rejected: {reason}", nameof(ApprovalService));

        ApprovalCompleted?.Invoke(this, result);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<ApprovalResult?> GetApprovalStatusAsync(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return Task.FromResult<ApprovalResult?>(null);

        _approvalResults.TryGetValue(requestId, out var result);
        return Task.FromResult(result);
    }

    #endregion

    #region Signature Validation

    /// <inheritdoc />
    public Task<bool> ValidateSignatureAsync(byte[] signatureData, string signatoryId)
    {
        if (signatureData == null || signatureData.Length == 0)
        {
            _logger?.Warning("Signature validation failed: empty signature data", nameof(ApprovalService));
            return Task.FromResult(false);
        }

        if (string.IsNullOrWhiteSpace(signatoryId))
        {
            _logger?.Warning("Signature validation failed: empty signatory ID", nameof(ApprovalService));
            return Task.FromResult(false);
        }

        // Check if signatory exists
        if (!_signatories.TryGetValue(signatoryId, out var signatory))
        {
            _logger?.Warning($"Signature validation failed: signatory {signatoryId} not found", nameof(ApprovalService));
            return Task.FromResult(false);
        }

        // Basic validation - signatory exists and has required information
        var isValid = !string.IsNullOrWhiteSpace(signatory.Name) &&
                      !string.IsNullOrWhiteSpace(signatory.Company);

        _logger?.Debug($"Signature validation for {signatoryId}: {(isValid ? "valid" : "invalid")}", nameof(ApprovalService));

        return Task.FromResult(isValid);
    }

    /// <inheritdoc />
    public Task<bool> ValidateSignatoryAsync(string signatoryId)
    {
        if (string.IsNullOrWhiteSpace(signatoryId))
            return Task.FromResult(false);

        if (!_signatories.TryGetValue(signatoryId, out var signatory))
            return Task.FromResult(false);

        // Validate required fields
        return Task.FromResult(
            !string.IsNullOrWhiteSpace(signatory.Name) &&
            !string.IsNullOrWhiteSpace(signatory.Company));
    }

    #endregion

    #region Signatory Management

    /// <inheritdoc />
    public Task<IEnumerable<Signatory>> GetSignatoriesAsync()
    {
        var signatories = _signatories.Values
            .OrderByDescending(s => s.IsDefault)
            .ThenByDescending(s => s.UsageCount)
            .ThenBy(s => s.Name)
            .ToList();

        return Task.FromResult<IEnumerable<Signatory>>(signatories);
    }

    /// <inheritdoc />
    public Task<Signatory?> GetSignatoryByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<Signatory?>(null);

        _signatories.TryGetValue(id, out var signatory);
        return Task.FromResult(signatory);
    }

    /// <inheritdoc />
    public Task<Signatory?> GetDefaultSignatoryAsync()
    {
        var defaultSignatory = _signatories.Values.FirstOrDefault(s => s.IsDefault);
        return Task.FromResult(defaultSignatory);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Signatory>> GetRecentSignatoriesAsync(int count = 5)
    {
        var recent = _signatories.Values
            .Where(s => s.LastUsedAt.HasValue)
            .OrderByDescending(s => s.LastUsedAt)
            .Take(count)
            .ToList();

        return Task.FromResult<IEnumerable<Signatory>>(recent);
    }

    /// <inheritdoc />
    public Task SaveSignatoryAsync(Signatory signatory)
    {
        if (signatory == null)
            throw new ArgumentNullException(nameof(signatory));

        if (string.IsNullOrWhiteSpace(signatory.Name))
            throw new ArgumentException("Signatory name cannot be empty", nameof(signatory));

        if (string.IsNullOrWhiteSpace(signatory.Company))
            throw new ArgumentException("Signatory company cannot be empty", nameof(signatory));

        // If this is the first signatory, make it default
        if (_signatories.IsEmpty)
        {
            signatory.IsDefault = true;
        }

        _signatories[signatory.Id] = signatory;

        _logger?.Info($"Signatory saved: {signatory.Name} ({signatory.Id})", nameof(ApprovalService));

        // Persist to disk if enabled
        SaveSignatoriesToDisk();

        SignatoryChanged?.Invoke(this, signatory);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetDefaultSignatoryAsync(string signatoryId)
    {
        if (string.IsNullOrWhiteSpace(signatoryId))
            throw new ArgumentException("Signatory ID cannot be empty", nameof(signatoryId));

        if (!_signatories.TryGetValue(signatoryId, out var newDefault))
            throw new InvalidOperationException($"Signatory {signatoryId} not found");

        // Clear default from all other signatories
        foreach (var signatory in _signatories.Values)
        {
            if (signatory.Id != signatoryId && signatory.IsDefault)
            {
                signatory.IsDefault = false;
            }
        }

        newDefault.IsDefault = true;

        _logger?.Info($"Default signatory set to: {newDefault.Name} ({signatoryId})", nameof(ApprovalService));

        // Persist to disk if enabled
        SaveSignatoriesToDisk();

        SignatoryChanged?.Invoke(this, newDefault);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteSignatoryAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Signatory ID cannot be empty", nameof(id));

        if (_signatories.TryRemove(id, out var removed))
        {
            _logger?.Info($"Signatory deleted: {removed.Name} ({id})", nameof(ApprovalService));

            // If we deleted the default, set a new default
            if (removed.IsDefault && !_signatories.IsEmpty)
            {
                var newDefault = _signatories.Values.OrderByDescending(s => s.UsageCount).First();
                newDefault.IsDefault = true;
            }

            // Persist to disk if enabled
            SaveSignatoriesToDisk();

            SignatoryDeleted?.Invoke(this, id);
        }
        else
        {
            _logger?.Warning($"Attempted to delete non-existent signatory: {id}", nameof(ApprovalService));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordSignatoryUsageAsync(string signatoryId)
    {
        if (string.IsNullOrWhiteSpace(signatoryId))
            return Task.CompletedTask;

        if (_signatories.TryGetValue(signatoryId, out var signatory))
        {
            signatory.LastUsedAt = DateTime.UtcNow;
            signatory.UsageCount++;

            _logger?.Debug($"Recorded usage for signatory {signatoryId} (count: {signatory.UsageCount})", nameof(ApprovalService));

            // Persist to disk if enabled
            SaveSignatoriesToDisk();
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Persistence

    /// <summary>
    /// Load signatories from disk if persistence path is configured.
    /// </summary>
    private void LoadSignatoriesFromDisk()
    {
        if (string.IsNullOrWhiteSpace(_persistencePath))
            return;

        var filePath = Path.Combine(_persistencePath, "signatories.json");

        if (!File.Exists(filePath))
        {
            _logger?.Debug("No signatories file found, starting with empty collection", nameof(ApprovalService));
            return;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var signatories = JsonSerializer.Deserialize<List<Signatory>>(json, _jsonOptions);

            if (signatories != null)
            {
                foreach (var signatory in signatories)
                {
                    _signatories[signatory.Id] = signatory;
                }

                _logger?.Info($"Loaded {signatories.Count} signatories from disk", nameof(ApprovalService));
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to load signatories from disk: {filePath}", ex, nameof(ApprovalService));
        }
    }

    /// <summary>
    /// Save signatories to disk if persistence path is configured.
    /// </summary>
    private void SaveSignatoriesToDisk()
    {
        if (string.IsNullOrWhiteSpace(_persistencePath))
            return;

        var filePath = Path.Combine(_persistencePath, "signatories.json");

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var signatories = _signatories.Values.ToList();
            var json = JsonSerializer.Serialize(signatories, _jsonOptions);
            File.WriteAllText(filePath, json);

            _logger?.Debug($"Saved {signatories.Count} signatories to disk", nameof(ApprovalService));
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to save signatories to disk: {filePath}", ex, nameof(ApprovalService));
        }
    }

    #endregion
}
