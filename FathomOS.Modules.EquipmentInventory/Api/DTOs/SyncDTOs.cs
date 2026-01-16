using System.Text.Json.Serialization;

namespace FathomOS.Modules.EquipmentInventory.Api.DTOs;

// ============ Sync DTOs - Match API protocol exactly ============

public record SyncPullRequest
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; init; } = string.Empty;
    
    [JsonPropertyName("lastSyncVersion")]
    public long LastSyncVersion { get; init; }
    
    [JsonPropertyName("tables")]
    public List<string> Tables { get; init; } = new()
    {
        "Equipment", "Manifests", "ManifestItems", "Locations", "Categories", "Projects"
    };
}

public record SyncPullResponse
{
    [JsonPropertyName("newSyncVersion")]
    public long NewSyncVersion { get; init; }
    
    [JsonPropertyName("changes")]
    public SyncChangesDto Changes { get; init; } = new();
    
    [JsonPropertyName("hasMore")]
    public bool HasMore { get; init; }
}

public record SyncChangesDto
{
    [JsonPropertyName("equipment")]
    public List<SyncRecord<EquipmentDto>> Equipment { get; init; } = new();
    
    [JsonPropertyName("manifests")]
    public List<SyncRecord<ManifestDto>> Manifests { get; init; } = new();
    
    [JsonPropertyName("locations")]
    public List<SyncRecord<LocationDto>> Locations { get; init; } = new();
    
    [JsonPropertyName("categories")]
    public List<SyncRecord<CategoryDto>> Categories { get; init; } = new();
    
    [JsonPropertyName("projects")]
    public List<SyncRecord<ProjectDto>> Projects { get; init; } = new();
}

public record SyncRecord<T>
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }
    
    [JsonPropertyName("operation")]
    public string Operation { get; init; } = string.Empty; // "Insert", "Update", "Delete"
    
    [JsonPropertyName("data")]
    public T? Data { get; init; }
    
    [JsonPropertyName("syncVersion")]
    public long SyncVersion { get; init; }
}

public record SyncPushRequest
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; init; } = string.Empty;
    
    [JsonPropertyName("changes")]
    public List<SyncPushChange> Changes { get; init; } = new();
}

public record SyncPushChange
{
    [JsonPropertyName("table")]
    public string Table { get; init; } = string.Empty;
    
    [JsonPropertyName("id")]
    public Guid Id { get; init; }
    
    [JsonPropertyName("operation")]
    public string Operation { get; init; } = string.Empty; // "Insert", "Update", "Delete"
    
    [JsonPropertyName("data")]
    public object? Data { get; init; }
    
    [JsonPropertyName("localTimestamp")]
    public DateTime LocalTimestamp { get; init; }
}

public record SyncPushResponse
{
    [JsonPropertyName("applied")]
    public int Applied { get; init; }
    
    [JsonPropertyName("conflicts")]
    public List<SyncConflictDto> Conflicts { get; init; } = new();
}

public record SyncConflictDto
{
    [JsonPropertyName("conflictId")]
    public Guid ConflictId { get; init; }
    
    [JsonPropertyName("table")]
    public string Table { get; init; } = string.Empty;
    
    [JsonPropertyName("recordId")]
    public Guid RecordId { get; init; }
    
    [JsonPropertyName("localData")]
    public object? LocalData { get; init; }
    
    [JsonPropertyName("serverData")]
    public object? ServerData { get; init; }
}

public record SyncStatusResponse
{
    [JsonPropertyName("currentSyncVersion")]
    public long CurrentSyncVersion { get; init; }
    
    [JsonPropertyName("lastSyncTime")]
    public DateTime? LastSyncTime { get; init; }
    
    [JsonPropertyName("pendingConflicts")]
    public int PendingConflicts { get; init; }
    
    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; init; } = true;
}

public record ResolveConflictRequest
{
    [JsonPropertyName("resolution")]
    public string Resolution { get; init; } = string.Empty; // "UseLocal", "UseServer", "Merged"
    
    [JsonPropertyName("mergedData")]
    public Dictionary<string, object>? MergedData { get; init; }
}
