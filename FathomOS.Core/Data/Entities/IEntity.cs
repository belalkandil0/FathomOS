// FathomOS.Core/Data/Entities/IEntity.cs
// Base entity interfaces for the database layer
// Provides consistent entity structure across all modules

namespace FathomOS.Core.Data.Entities;

/// <summary>
/// Base interface for all entities with a GUID primary key.
/// All database entities should implement this interface.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    Guid Id { get; set; }
}

/// <summary>
/// Interface for entities that track creation and modification timestamps
/// </summary>
public interface IAuditableEntity : IEntity
{
    /// <summary>
    /// When the entity was created (UTC)
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the entity was last modified (UTC)
    /// </summary>
    DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User ID who created the entity
    /// </summary>
    Guid? CreatedBy { get; set; }

    /// <summary>
    /// User ID who last modified the entity
    /// </summary>
    Guid? ModifiedBy { get; set; }
}

/// <summary>
/// Interface for entities that support soft delete
/// </summary>
public interface ISoftDeletableEntity : IEntity
{
    /// <summary>
    /// Whether the entity has been soft-deleted
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// When the entity was deleted (UTC)
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User ID who deleted the entity
    /// </summary>
    Guid? DeletedBy { get; set; }
}

/// <summary>
/// Interface for entities that support sync operations
/// </summary>
public interface ISyncableEntity : IEntity
{
    /// <summary>
    /// Version number for optimistic concurrency and sync
    /// </summary>
    long SyncVersion { get; set; }

    /// <summary>
    /// When the entity was last synced (UTC)
    /// </summary>
    DateTime? LastSyncedAt { get; set; }

    /// <summary>
    /// Whether the entity has local changes pending sync
    /// </summary>
    bool HasPendingChanges { get; set; }

    /// <summary>
    /// Unique identifier for the source of the sync (device/client ID)
    /// </summary>
    string? SyncSourceId { get; set; }
}

/// <summary>
/// Combined interface for entities with full tracking capabilities
/// </summary>
public interface ITrackedEntity : IAuditableEntity, ISoftDeletableEntity, ISyncableEntity
{
}

/// <summary>
/// Base implementation of IEntity
/// </summary>
public abstract class EntityBase : IEntity
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Base implementation with audit tracking
/// </summary>
public abstract class AuditableEntity : EntityBase, IAuditableEntity
{
    /// <inheritdoc />
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime? ModifiedAt { get; set; }

    /// <inheritdoc />
    public Guid? CreatedBy { get; set; }

    /// <inheritdoc />
    public Guid? ModifiedBy { get; set; }
}

/// <summary>
/// Base implementation with soft delete support
/// </summary>
public abstract class SoftDeletableEntity : AuditableEntity, ISoftDeletableEntity
{
    /// <inheritdoc />
    public bool IsDeleted { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <inheritdoc />
    public Guid? DeletedBy { get; set; }
}

/// <summary>
/// Base implementation with full sync and tracking support
/// </summary>
public abstract class TrackedEntity : SoftDeletableEntity, ISyncableEntity, ITrackedEntity
{
    /// <inheritdoc />
    public long SyncVersion { get; set; }

    /// <inheritdoc />
    public DateTime? LastSyncedAt { get; set; }

    /// <inheritdoc />
    public bool HasPendingChanges { get; set; }

    /// <inheritdoc />
    public string? SyncSourceId { get; set; }
}
