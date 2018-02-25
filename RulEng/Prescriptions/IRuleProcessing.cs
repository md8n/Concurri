using RulEng.States;

namespace RulEng.Prescriptions
{
    /// <inheritdoc />
    /// <summary>
    /// These define Prescriptions (Redux Actions) to be performed by Rules
    /// </summary>
    public interface IRuleProcessing : IProcessing
    {
        IRulePrescription Entities { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// These define Prescriptions (Redux Actions) to be performed by Rules only targetting Values
    /// </summary>
    public interface IRuleValueProcessing : IRuleProcessing
    {
    }

    /// <inheritdoc />
    /// <summary>
    /// These define Prescriptions (Redux Actions) to be performed by Rules only targetting RuleResults
    /// </summary>
    public interface IRuleRuleResultProcessing : IRuleProcessing
    {
    }
}
