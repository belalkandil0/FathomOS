using System.Linq.Expressions;

namespace FathomOS.Domain.Specifications;

/// <summary>
/// Defines a specification for querying entities.
/// The Specification pattern encapsulates query logic in a reusable, composable manner.
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the criteria expression for filtering entities.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the include expressions for eager loading related entities.
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the string-based include paths for eager loading related entities.
    /// Used for complex navigation paths.
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the ordering expression for ascending order.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Gets the ordering expression for descending order.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Gets the secondary ordering expressions for then-by clauses.
    /// </summary>
    IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> ThenByExpressions { get; }

    /// <summary>
    /// Gets the number of entities to take (for pagination).
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the number of entities to skip (for pagination).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets a value indicating whether pagination is enabled.
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether to use split queries for includes.
    /// </summary>
    bool AsSplitQuery { get; }

    /// <summary>
    /// Gets a value indicating whether to disable change tracking.
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// Gets a value indicating whether to use no tracking with identity resolution.
    /// </summary>
    bool AsNoTrackingWithIdentityResolution { get; }

    /// <summary>
    /// Determines whether the specified entity satisfies this specification.
    /// </summary>
    /// <param name="entity">The entity to evaluate</param>
    /// <returns>True if the entity satisfies the specification; otherwise, false</returns>
    bool IsSatisfiedBy(T entity);
}

/// <summary>
/// Extended specification interface for projections.
/// </summary>
/// <typeparam name="T">The source entity type</typeparam>
/// <typeparam name="TResult">The projected result type</typeparam>
public interface ISpecification<T, TResult> : ISpecification<T>
{
    /// <summary>
    /// Gets the selector expression for projecting entities.
    /// </summary>
    Expression<Func<T, TResult>>? Selector { get; }

    /// <summary>
    /// Gets the post-processing action for transforming results after database query.
    /// </summary>
    Func<IEnumerable<TResult>, IEnumerable<TResult>>? PostProcessingAction { get; }
}
