namespace FathomOS.Domain.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    /// <inheritdoc />
    public override string ErrorCode => RuleCode;

    /// <summary>
    /// Gets the specific rule code that was violated.
    /// </summary>
    public string RuleCode { get; }

    /// <summary>
    /// Gets the name of the business rule that was violated.
    /// </summary>
    public string RuleName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
    /// </summary>
    /// <param name="ruleName">The name of the business rule</param>
    /// <param name="message">The error message</param>
    public BusinessRuleViolationException(string ruleName, string message)
        : base(message, new Dictionary<string, object> { ["RuleName"] = ruleName })
    {
        RuleName = ruleName;
        RuleCode = $"BUSINESS_RULE_{ruleName.ToUpperInvariant().Replace(' ', '_')}";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleViolationException"/> class.
    /// </summary>
    /// <param name="ruleName">The name of the business rule</param>
    /// <param name="ruleCode">The specific rule code</param>
    /// <param name="message">The error message</param>
    public BusinessRuleViolationException(string ruleName, string ruleCode, string message)
        : base(message, new Dictionary<string, object>
        {
            ["RuleName"] = ruleName,
            ["RuleCode"] = ruleCode
        })
    {
        RuleName = ruleName;
        RuleCode = ruleCode;
    }

    /// <summary>
    /// Creates an exception from a business rule violation.
    /// </summary>
    /// <param name="rule">The violated business rule</param>
    /// <returns>A new BusinessRuleViolationException</returns>
    public static BusinessRuleViolationException FromRule(IBusinessRule rule)
    {
        return new BusinessRuleViolationException(rule.RuleName, rule.Message);
    }
}

/// <summary>
/// Interface for business rules that can be checked.
/// </summary>
public interface IBusinessRule
{
    /// <summary>
    /// Gets the name of the business rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Gets the error message if the rule is broken.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Checks if the business rule is broken.
    /// </summary>
    /// <returns>True if the rule is broken; otherwise, false</returns>
    bool IsBroken();
}

/// <summary>
/// Extension methods for business rule checking.
/// </summary>
public static class BusinessRuleExtensions
{
    /// <summary>
    /// Checks a business rule and throws an exception if it is broken.
    /// </summary>
    /// <param name="rule">The business rule to check</param>
    /// <exception cref="BusinessRuleViolationException">Thrown when the rule is broken</exception>
    public static void CheckRule(this IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw BusinessRuleViolationException.FromRule(rule);
        }
    }
}
