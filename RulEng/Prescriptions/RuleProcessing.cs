using System.Collections.Immutable;
using RulEng.States;

/// <summary>
/// These define Prescriptions (Redux Actions) performed by Rules
/// </summary>
namespace RulEng.Prescriptions
{
    /// <summary>
    /// For each Entity, test whether it exists
    /// </summary>
    public class ProcessExistsRule : IRuleProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// For each Value, test whether it has a meaningful Value (determined by the type of the Value.detail)
    /// </summary>
    public class ProcessHasMeaningfulValueRule : IRuleProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// For each pair of Values, perform a A < B comparison relevant to their type
    /// </summary>
    public class ProcessLessThanRule : IRuleValueProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// For each pair of Values, perform a comparison relevant to their type
    /// </summary>
    public class ProcessEqualRule : IRuleValueProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// For each pair of Values, perform a A > B comparison relevant to their type
    /// </summary>
    public class ProcessGreaterThanRule : IRuleValueProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// For each pair of values, perform a B.exec(A) comparison - B is the Regex, A is (converted to) a string
    /// </summary>
    public class ProcessRegexMatchRule : IRuleValueProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// For each list of rule results, test they are all true
    /// </summary>
    public class ProcessAndRule : IRuleProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// For each list of rule results, test at least one is true
    /// </summary>
    public class ProcessOrRule : IRuleProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// For each list of rule results, test only one is true
    /// </summary>
    public class ProcessXorRule : IRuleProcessing
    {
        public IImmutableList<IRulePrescription> Entities { get; set; }
    }
}
