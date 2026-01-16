using System.Text.Json;
using FathomOS.Modules.EquipmentInventory.Api;
using FathomOS.Modules.EquipmentInventory.Api.DTOs;
using FathomOS.Modules.EquipmentInventory.Data;
using FathomOS.Modules.EquipmentInventory.Models;

namespace FathomOS.Modules.EquipmentInventory.Services;

/// <summary>
/// Handles synchronization between local SQLite database and central API server.
/// Implements delta sync using SyncVersion tracking.
/// </summary>
public class SyncService
{
    private readonly LocalDatabaseService _dbService;
    private readonly AuthenticationService _authService;
    private ApiClient? _apiClient;
    private bool _isSyncing;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    
    public event EventHandler<SyncProgressEventArgs>? SyncProgress;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
    public event EventHandler<SyncConflictEventArgs>? ConflictDetected;
    public event EventHandler<string>? SyncError;
    
    public bool IsSyncing => _isSyncing;
    public long LastSyncVersion { get; private set; }
    public DateTime? LastSyncTime { get; private set; }
    
    private int _pendingChanges;
    public int PendingChanges 
    { 
        get => _pendingChanges;
        private set => _pendingChanges = value;
    }
    
    public SyncService(LocalDatabaseService dbService, AuthenticationService authService)
    {
        _dbService = dbService;
        _authService = authService;
    }
    
    /// <summary>
    /// Configure the sync service with API URL
    /// </summary>
    public void Configure(string apiBaseUrl)
    {
        _apiClient?.Dispose();
        _apiClient = new ApiClient(apiBaseUrl);
        
        _apiClient.TokenExpired += (s, e) => 
        {
            SyncError?.Invoke(this, "Authentication expired. Please log in again.");
        };
        
        _apiClient.ApiError += (s, error) =>
        {
            SyncError?.Invoke(this, error);
        };
    }
    
    /// <summary>
    /// Set authentication tokens from login
    /// </summary>
    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        if (_apiClient == null) return false;
        
        var (success, error) = await _apiClient.LoginAsync(username, password);
        if (!success)
        {
            SyncError?.Invoke(this, error ?? "Authentication failed");
        }
        return success;
    }
    
    /// <summary>
    /// Perform full sync - pull changes then push local changes
    /// </summary>
    public async Task<SyncResult> SyncAsync()
    {
        if (_apiClient == null || !_apiClient.IsAuthenticated)
        {
            return new SyncResult { Success = false, Error = "Not authenticated" };
        }
        
        if (!await _syncLock.WaitAsync(0))
        {
            return new SyncResult { Success = false, Error = "Sync already in progress" };
        }
        
        try
        {
            _isSyncing = true;
            var result = new SyncResult();
            
            // Step 1: Pull changes from server
            SyncProgress?.Invoke(this, new SyncProgressEventArgs("Pulling changes...", 0, 100));
            
            var pullResult = await PullChangesAsync();
            result.ItemsPulled = pullResult.ItemsPulled;
            
            if (!pullResult.Success)
            {
                result.Error = pullResult.Error;
                return result;
            }
            
            // Step 2: Push local changes to server
            SyncProgress?.Invoke(this, new SyncProgressEventArgs("Pushing changes...", 50, 100));
            
            var pushResult = await PushChangesAsync();
            result.ItemsPushed = pushResult.ItemsPushed;
            result.Conflicts = pushResult.Conflicts;
            
            if (!pushResult.Success)
            {
                result.Error = pushResult.Error;
                // Continue even if push partially failed
            }
            
            // Step 3: Update sync metadata
            LastSyncTime = DateTime.UtcNow;
            await _dbService.UpdateSyncSettingsAsync(LastSyncVersion, LastSyncTime.Value);
            
            result.Success = true;
            SyncProgress?.Invoke(this, new SyncProgressEventArgs("Sync complete", 100, 100));
            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs(result));
            
            return result;
        }
        catch (Exception ex)
        {
            var result = new SyncResult { Success = false, Error = ex.Message };
            SyncError?.Invoke(this, ex.Message);
            return result;
        }
        finally
        {
            _isSyncing = false;
            _syncLock.Release();
        }
    }
    
    /// <summary>
    /// Pull changes from server since last sync version
    /// </summary>
    private async Task<(bool Success, int ItemsPulled, string? Error)> PullChangesAsync()
    {
        if (_apiClient == null) return (false, 0, "Not configured");
        
        try
        {
            // Get current sync version from database
            var syncSettings = await _dbService.GetSyncSettingsAsync();
            LastSyncVersion = syncSettings?.LastSyncVersion ?? 0;
            
            var response = await _apiClient.PullChangesAsync(LastSyncVersion);
            if (response == null)
            {
                return (false, 0, "Failed to pull changes from server");
            }
            
            int itemsProcessed = 0;
            
            // Apply equipment changes
            foreach (var change in response.Changes.Equipment)
            {
                await ApplyEquipmentChangeAsync(change);
                itemsProcessed++;
            }
            
            // Apply manifest changes
            foreach (var change in response.Changes.Manifests)
            {
                await ApplyManifestChangeAsync(change);
                itemsProcessed++;
            }
            
            // Apply location changes
            foreach (var change in response.Changes.Locations)
            {
                await ApplyLocationChangeAsync(change);
                itemsProcessed++;
            }
            
            // Apply category changes
            foreach (var change in response.Changes.Categories)
            {
                await ApplyCategoryChangeAsync(change);
                itemsProcessed++;
            }
            
            // Apply project changes
            foreach (var change in response.Changes.Projects)
            {
                await ApplyProjectChangeAsync(change);
                itemsProcessed++;
            }
            
            // Update sync version
            LastSyncVersion = response.NewSyncVersion;
            
            // Check if more changes available
            if (response.HasMore)
            {
                // Recursively pull more changes
                var moreResult = await PullChangesAsync();
                itemsProcessed += moreResult.ItemsPulled;
            }
            
            return (true, itemsProcessed, null);
        }
        catch (Exception ex)
        {
            return (false, 0, ex.Message);
        }
    }
    
    /// <summary>
    /// Push local changes to server
    /// </summary>
    private async Task<(bool Success, int ItemsPushed, List<SyncConflictInfo> Conflicts, string? Error)> PushChangesAsync()
    {
        if (_apiClient == null) return (false, 0, new(), "Not configured");
        
        try
        {
            // Get pending offline queue items
            var pendingItems = await _dbService.GetPendingQueueItemsAsync();
            if (!pendingItems.Any())
            {
                return (true, 0, new(), null);
            }
            
            // Convert to API format
            var changes = pendingItems.Select(item => new SyncPushChange
            {
                Table = item.TableName,
                Id = item.RecordId,
                Operation = item.Operation,
                Data = JsonSerializer.Deserialize<object>(item.DataJson),
                LocalTimestamp = item.CreatedAt
            }).ToList();
            
            var response = await _apiClient.PushChangesAsync(changes);
            if (response == null)
            {
                return (false, 0, new(), "Failed to push changes to server");
            }
            
            // Mark successfully pushed items as completed
            foreach (var item in pendingItems.Take(response.Applied))
            {
                await _dbService.MarkQueueItemCompletedAsync(item.QueueId);
            }
            
            // Handle conflicts
            var conflicts = new List<SyncConflictInfo>();
            foreach (var conflict in response.Conflicts)
            {
                var syncConflict = new SyncConflictInfo
                {
                    ConflictId = conflict.ConflictId,
                    TableName = conflict.Table,
                    RecordId = conflict.RecordId,
                    LocalData = conflict.LocalData?.ToString(),
                    ServerData = conflict.ServerData?.ToString(),
                    DetectedAt = DateTime.UtcNow
                };
                conflicts.Add(syncConflict);
                
                ConflictDetected?.Invoke(this, new SyncConflictEventArgs(syncConflict));
            }
            
            return (true, response.Applied, conflicts, null);
        }
        catch (Exception ex)
        {
            return (false, 0, new(), ex.Message);
        }
    }
    
    #region Apply Changes
    
    private async Task ApplyEquipmentChangeAsync(SyncRecord<EquipmentDto> change)
    {
        if (change.Data == null && change.Operation != "Delete") return;
        
        switch (change.Operation)
        {
            case "Insert":
            case "Update":
                var equipment = MapDtoToEquipment(change.Data!);
                equipment.SyncVersion = change.SyncVersion;
                equipment.IsModifiedLocally = false;
                await _dbService.SaveEquipmentAsync(equipment);
                break;
                
            case "Delete":
                await _dbService.DeleteEquipmentAsync(change.Id);
                break;
        }
    }
    
    private async Task ApplyManifestChangeAsync(SyncRecord<ManifestDto> change)
    {
        if (change.Data == null && change.Operation != "Delete") return;
        
        switch (change.Operation)
        {
            case "Insert":
            case "Update":
                var manifest = MapDtoToManifest(change.Data!);
                manifest.SyncVersion = change.SyncVersion;
                manifest.IsModifiedLocally = false;
                await _dbService.SaveManifestAsync(manifest);
                break;
                
            case "Delete":
                // Manifests typically not deleted, just cancelled
                break;
        }
    }
    
    private async Task ApplyLocationChangeAsync(SyncRecord<LocationDto> change)
    {
        // Locations are lookup data - apply directly
        if (change.Data == null) return;
        
        var location = new Location
        {
            LocationId = change.Data.LocationId,
            Name = change.Data.Name,
            Code = change.Data.Code,
            ParentLocationId = change.Data.ParentLocationId,
            ContactPerson = change.Data.ContactPerson,
            ContactPhone = change.Data.ContactPhone,
            IsOffshore = change.Data.IsOffshore,
            SyncVersion = change.SyncVersion
        };
        
        // Save to local database
        // Implementation depends on LocalDatabaseService
    }
    
    private async Task ApplyCategoryChangeAsync(SyncRecord<CategoryDto> change)
    {
        // Categories are lookup data
        if (change.Data == null) return;
        
        var category = new EquipmentCategory
        {
            CategoryId = change.Data.CategoryId,
            Name = change.Data.Name,
            Code = change.Data.Code,
            Icon = change.Data.Icon,
            Color = change.Data.Color,
            IsConsumable = change.Data.IsConsumable,
            RequiresCertification = change.Data.RequiresCertification,
            RequiresCalibration = change.Data.RequiresCalibration
        };
        
        // Save to local database
    }
    
    private async Task ApplyProjectChangeAsync(SyncRecord<ProjectDto> change)
    {
        // Projects are lookup data
        if (change.Data == null) return;
        
        var project = new Project
        {
            ProjectId = change.Data.ProjectId,
            Name = change.Data.Name,
            Code = change.Data.Code,
            LocationId = change.Data.LocationId
        };
        
        // Save to local database
    }
    
    #endregion
    
    #region DTO Mapping
    
    private Equipment MapDtoToEquipment(EquipmentDto dto)
    {
        return new Equipment
        {
            EquipmentId = dto.EquipmentId,
            AssetNumber = dto.AssetNumber,
            SapNumber = dto.SapNumber,
            TechNumber = dto.TechNumber,
            SerialNumber = dto.SerialNumber,
            QrCode = dto.QrCode,
            Name = dto.Name,
            Description = dto.Description,
            Manufacturer = dto.Manufacturer,
            Model = dto.Model,
            CategoryId = dto.Category?.CategoryId,
            TypeId = dto.Type?.TypeId,
            CurrentLocationId = dto.CurrentLocation?.LocationId,
            CurrentProjectId = dto.CurrentProject?.ProjectId,
            Status = Enum.TryParse<EquipmentStatus>(dto.Status, out var status) ? status : EquipmentStatus.Available,
            Condition = Enum.TryParse<EquipmentCondition>(dto.Condition, out var cond) ? cond : EquipmentCondition.Good,
            OwnershipType = Enum.TryParse<OwnershipType>(dto.OwnershipType, out var own) ? own : Models.OwnershipType.Owned,
            WeightKg = dto.Physical?.WeightKg,
            LengthCm = dto.Physical?.LengthCm,
            WidthCm = dto.Physical?.WidthCm,
            HeightCm = dto.Physical?.HeightCm,
            RequiresCertification = dto.Certification?.Required ?? false,
            CertificationNumber = dto.Certification?.Number,
            CertificationBody = dto.Certification?.Body,
            CertificationExpiryDate = dto.Certification?.ExpiryDate,
            RequiresCalibration = dto.Calibration?.Required ?? false,
            LastCalibrationDate = dto.Calibration?.LastDate,
            NextCalibrationDate = dto.Calibration?.NextDate,
            CalibrationInterval = dto.Calibration?.IntervalDays,
            PrimaryPhotoUrl = dto.PrimaryPhotoUrl,
            QrCodeImageUrl = dto.QrCodeImageUrl,
            Notes = dto.Notes,
            UpdatedAt = dto.LastUpdated ?? DateTime.UtcNow,
            SyncVersion = dto.SyncVersion
        };
    }
    
    private Manifest MapDtoToManifest(ManifestDto dto)
    {
        var manifest = new Manifest
        {
            ManifestId = dto.ManifestId,
            ManifestNumber = dto.ManifestNumber,
            QrCode = dto.QrCode,
            Type = Enum.TryParse<ManifestType>(dto.Type, out var type) ? type : ManifestType.Outward,
            Status = Enum.TryParse<ManifestStatus>(dto.Status, out var status) ? status : ManifestStatus.Draft,
            FromLocationId = dto.FromLocation?.LocationId ?? Guid.Empty,
            FromContactName = dto.FromContact?.Name,
            FromContactPhone = dto.FromContact?.Phone,
            FromContactEmail = dto.FromContact?.Email,
            ToLocationId = dto.ToLocation?.LocationId ?? Guid.Empty,
            ToContactName = dto.ToContact?.Name,
            ToContactPhone = dto.ToContact?.Phone,
            ToContactEmail = dto.ToContact?.Email,
            ProjectId = dto.Project?.ProjectId,
            CreatedDate = dto.Dates.Created ?? DateTime.UtcNow,
            SubmittedDate = dto.Dates.Submitted,
            ApprovedDate = dto.Dates.Approved,
            ShippedDate = dto.Dates.Shipped,
            ExpectedArrivalDate = dto.Dates.ExpectedArrival,
            ReceivedDate = dto.Dates.Received,
            ShippingMethod = dto.Shipping?.Method,
            CarrierName = dto.Shipping?.Carrier,
            TrackingNumber = dto.Shipping?.TrackingNumber,
            TotalItems = dto.Totals.Items,
            TotalWeight = dto.Totals.WeightKg,
            Notes = dto.Notes,
            HasDiscrepancies = dto.HasDiscrepancies,
            SyncVersion = dto.SyncVersion
        };
        
        // Map items
        foreach (var itemDto in dto.Items)
        {
            manifest.Items.Add(new ManifestItem
            {
                ItemId = itemDto.ItemId,
                ManifestId = manifest.ManifestId,
                EquipmentId = itemDto.Equipment?.EquipmentId ?? Guid.Empty,
                AssetNumber = itemDto.Equipment?.AssetNumber,
                Name = itemDto.Equipment?.Name,
                SerialNumber = itemDto.Equipment?.SerialNumber,
                Quantity = (int)itemDto.Quantity,
                ConditionAtSend = itemDto.ConditionAtSend,
                ConditionNotes = itemDto.ConditionNotes,
                IsReceived = itemDto.IsReceived,
                ReceivedQuantity = (int?)itemDto.ReceivedQuantity,
                ConditionAtReceive = itemDto.ConditionAtReceive,
                HasDiscrepancy = itemDto.HasDiscrepancy,
                DiscrepancyNotes = itemDto.DiscrepancyNotes
            });
        }
        
        return manifest;
    }
    
    #endregion
    
    /// <summary>
    /// Resolve a sync conflict
    /// </summary>
    public async Task<bool> ResolveConflictAsync(Guid conflictId, string resolution, object? mergedData = null)
    {
        if (_apiClient == null) return false;
        
        var request = new ResolveConflictRequest
        {
            Resolution = resolution,
            MergedData = mergedData as Dictionary<string, object>
        };
        
        return await _apiClient.ResolveConflictAsync(conflictId, request);
    }
}

#region Event Args

public class SyncProgressEventArgs : EventArgs
{
    public string Message { get; }
    public int Current { get; }
    public int Total { get; }
    public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    
    public SyncProgressEventArgs(string message, int current, int total)
    {
        Message = message;
        Current = current;
        Total = total;
    }
}

public class SyncCompletedEventArgs : EventArgs
{
    public SyncResult Result { get; }
    public SyncCompletedEventArgs(SyncResult result) => Result = result;
}

public class SyncConflictEventArgs : EventArgs
{
    public SyncConflictInfo Conflict { get; }
    public SyncConflictEventArgs(SyncConflictInfo conflict) => Conflict = conflict;
}

public class SyncResult
{
    public bool Success { get; set; }
    public int ItemsPulled { get; set; }
    public int ItemsPushed { get; set; }
    public List<SyncConflictInfo> Conflicts { get; set; } = new();
    public string? Error { get; set; }
}

/// <summary>
/// Lightweight sync conflict info for event handling (not EF entity)
/// </summary>
public class SyncConflictInfo
{
    public Guid ConflictId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string? LocalData { get; set; }
    public string? ServerData { get; set; }
    public DateTime DetectedAt { get; set; }
}

#endregion
