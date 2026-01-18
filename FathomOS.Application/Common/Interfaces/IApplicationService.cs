namespace FathomOS.Application.Common.Interfaces;

/// <summary>
/// Marker interface for application services.
/// Application services orchestrate use cases and coordinate between domain services and infrastructure.
/// </summary>
public interface IApplicationService
{
}

/// <summary>
/// Interface for transactional application services.
/// Implementations should ensure operations are executed within a transaction scope.
/// </summary>
public interface ITransactionalService : IApplicationService
{
    /// <summary>
    /// Gets or sets a value indicating whether the current operation should be executed in a transaction.
    /// </summary>
    bool UseTransaction { get; set; }
}
