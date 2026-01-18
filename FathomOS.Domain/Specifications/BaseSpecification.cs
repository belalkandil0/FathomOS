using System.Linq.Expressions;

namespace FathomOS.Domain.Specifications;

/// <summary>
/// Base implementation of the Specification pattern.
/// Provides a fluent API for building query specifications.
/// </summary>
/// <typeparam name="T">The type of entity this specification applies to</typeparam>
public abstract class BaseSpecification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<(Expression<Func<T, object>> KeySelector, bool Descending)> _thenByExpressions = [];

    /// <inheritdoc />
    public Expression<Func<T, bool>>? Criteria { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    /// <inheritdoc />
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <inheritdoc />
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> ThenByExpressions =>
        _thenByExpressions.AsReadOnly();

    /// <inheritdoc />
    public int? Take { get; private set; }

    /// <inheritdoc />
    public int? Skip { get; private set; }

    /// <inheritdoc />
    public bool IsPagingEnabled { get; private set; }

    /// <inheritdoc />
    public bool AsSplitQuery { get; private set; }

    /// <inheritdoc />
    public bool AsNoTracking { get; private set; } = true;

    /// <inheritdoc />
    public bool AsNoTrackingWithIdentityResolution { get; private set; }

    /// <summary>
    /// Initializes a new specification with no criteria.
    /// </summary>
    protected BaseSpecification()
    {
    }

    /// <summary>
    /// Initializes a new specification with the specified criteria.
    /// </summary>
    /// <param name="criteria">The filter criteria expression</param>
    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds a filter criteria to the specification.
    /// </summary>
    /// <param name="criteria">The filter criteria expression</param>
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Combines existing criteria with a new criteria using AND logic.
    /// </summary>
    /// <param name="criteria">The additional filter criteria expression</param>
    protected void AndCriteria(Expression<Func<T, bool>> criteria)
    {
        if (Criteria is null)
        {
            Criteria = criteria;
            return;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var combined = Expression.AndAlso(
            Expression.Invoke(Criteria, parameter),
            Expression.Invoke(criteria, parameter));
        Criteria = Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    /// Combines existing criteria with a new criteria using OR logic.
    /// </summary>
    /// <param name="criteria">The additional filter criteria expression</param>
    protected void OrCriteria(Expression<Func<T, bool>> criteria)
    {
        if (Criteria is null)
        {
            Criteria = criteria;
            return;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var combined = Expression.OrElse(
            Expression.Invoke(Criteria, parameter),
            Expression.Invoke(criteria, parameter));
        Criteria = Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    /// <summary>
    /// Adds an include expression for eager loading.
    /// </summary>
    /// <param name="includeExpression">The include expression</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        _includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds a string-based include path for eager loading.
    /// </summary>
    /// <param name="includeString">The include path string</param>
    protected void AddInclude(string includeString)
    {
        _includeStrings.Add(includeString);
    }

    /// <summary>
    /// Sets the ordering to ascending by the specified key.
    /// </summary>
    /// <param name="orderByExpression">The ordering key selector</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
        OrderByDescending = null;
    }

    /// <summary>
    /// Sets the ordering to descending by the specified key.
    /// </summary>
    /// <param name="orderByDescendingExpression">The ordering key selector</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
        OrderBy = null;
    }

    /// <summary>
    /// Adds a secondary ordering ascending by the specified key.
    /// </summary>
    /// <param name="thenByExpression">The secondary ordering key selector</param>
    protected void ApplyThenBy(Expression<Func<T, object>> thenByExpression)
    {
        _thenByExpressions.Add((thenByExpression, false));
    }

    /// <summary>
    /// Adds a secondary ordering descending by the specified key.
    /// </summary>
    /// <param name="thenByDescendingExpression">The secondary ordering key selector</param>
    protected void ApplyThenByDescending(Expression<Func<T, object>> thenByDescendingExpression)
    {
        _thenByExpressions.Add((thenByDescendingExpression, true));
    }

    /// <summary>
    /// Applies pagination to the specification.
    /// </summary>
    /// <param name="skip">The number of entities to skip</param>
    /// <param name="take">The number of entities to take</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Enables split queries for includes (reduces data duplication).
    /// </summary>
    protected void EnableSplitQuery()
    {
        AsSplitQuery = true;
    }

    /// <summary>
    /// Enables change tracking for the query results.
    /// </summary>
    protected void EnableTracking()
    {
        AsNoTracking = false;
        AsNoTrackingWithIdentityResolution = false;
    }

    /// <summary>
    /// Enables no tracking with identity resolution.
    /// </summary>
    protected void EnableNoTrackingWithIdentityResolution()
    {
        AsNoTracking = false;
        AsNoTrackingWithIdentityResolution = true;
    }

    /// <inheritdoc />
    public virtual bool IsSatisfiedBy(T entity)
    {
        if (Criteria is null)
            return true;

        return Criteria.Compile()(entity);
    }
}

/// <summary>
/// Base implementation of the Specification pattern with projection support.
/// </summary>
/// <typeparam name="T">The source entity type</typeparam>
/// <typeparam name="TResult">The projected result type</typeparam>
public abstract class BaseSpecification<T, TResult> : BaseSpecification<T>, ISpecification<T, TResult>
{
    /// <inheritdoc />
    public Expression<Func<T, TResult>>? Selector { get; private set; }

    /// <inheritdoc />
    public Func<IEnumerable<TResult>, IEnumerable<TResult>>? PostProcessingAction { get; private set; }

    /// <summary>
    /// Initializes a new specification with no criteria.
    /// </summary>
    protected BaseSpecification()
    {
    }

    /// <summary>
    /// Initializes a new specification with the specified criteria.
    /// </summary>
    /// <param name="criteria">The filter criteria expression</param>
    protected BaseSpecification(Expression<Func<T, bool>> criteria) : base(criteria)
    {
    }

    /// <summary>
    /// Sets the selector for projecting entities.
    /// </summary>
    /// <param name="selector">The projection selector expression</param>
    protected void ApplySelector(Expression<Func<T, TResult>> selector)
    {
        Selector = selector;
    }

    /// <summary>
    /// Sets a post-processing action for transforming results.
    /// </summary>
    /// <param name="postProcessingAction">The post-processing function</param>
    protected void ApplyPostProcessing(Func<IEnumerable<TResult>, IEnumerable<TResult>> postProcessingAction)
    {
        PostProcessingAction = postProcessingAction;
    }
}
