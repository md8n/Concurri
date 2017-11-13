using System;
using System.Collections.Immutable;
using RulEng.States;

/// <summary>
/// These define Prescriptions (Redux Actions) performed by Rules
/// </summary>
namespace RulEng.Prescriptions
{
    /// <summary>
    /// For each entity, test whether it exists
    /// </summary>
    public class ProcessExistsRule : IRuleProcessing
    {
        public IImmutableList<ITypeKey> Entities { get; set; }
    }

    /// <summary>
    /// For each value, test whether it has a meaningful value (determined by the type of the value)
    /// </summary>
    public class ProcessHasMeaningfulValueRule : IRuleProcessing
    {
        public IImmutableList<Guid> ValueIds { get; set; }
    }

    /// <summary>
    /// For each pair of values, perform a A < B comparison relevant to their type
    /// </summary>
    public class ProcessLessThanRule : IRuleProcessing
    {
        public IImmutableList<IRuleCompare> ValueIds { get; set; }
    }

    /// <summary>
    /// For each pair of values, perform a comparison relevant to their type
    /// </summary>
    public class ProcessEqualRule : IRuleProcessing
    {
        public IImmutableList<IRuleCompare> ValueIds { get; set; }
    }

    /// <summary>
    /// For each pair of values, perform a A > B comparison relevant to their type
    /// </summary>
    public class ProcessGreaterThanRule : IRuleProcessing
    {
        public IImmutableList<IRuleCompare> ValueIds { get; set; }
    }

    /// <summary>
    /// For each pair of values, perform a B.exec(A) comparison - B is the Regex, A is (converted to) a string
    /// </summary>
    public class ProcessRegexMatchRule : IRuleProcessing
    {
        public IImmutableList<IRuleCompare> ValueIds { get; set; }
    }

    /// <summary>
    /// For each list of rule results, test they are all true
    /// </summary>
    public class ProcessAndRule : IRuleProcessing
    {
        public IImmutableList<IRuleCollect> RuleResultIds { get; set; }
    }

    /// <summary>
    /// For each list of rule results, test at least one is true
    /// </summary>
    public class ProcessOrRule : IRuleProcessing
    {
        public IImmutableList<IRuleCollect> RuleResultIds { get; set; }
    }

    /// <summary>
    /// For each list of rule results, test only one is true
    /// </summary>
    public class ProcessXorRule : IRuleProcessing
    {
        public IImmutableList<IRuleCollect> RuleResultIds { get; set; }
    }
}
