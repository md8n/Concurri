using System.Collections.Immutable;
using RulEng.States;

namespace RulEng.Prescriptions
{
    /// <summary>
    /// These define Prescriptions (Redux Actions) to be performed by Rules
    /// </summary>
    public interface IRuleProcessing : IProcessing
    {
        IImmutableList<IRulePrescription> Entities { get; set; }
    }

    /// <summary>
    /// These define Prescriptions (Redux Actions) to be performed by Rules only targetting Values
    /// </summary>
    public interface IRuleValueProcessing : IRuleProcessing
    {
    }

}
