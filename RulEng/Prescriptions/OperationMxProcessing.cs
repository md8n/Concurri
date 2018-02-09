// These define Prescriptions (Redux Actions) performed by Mutation Operations

using System.Collections.Immutable;
using RulEng.States;

namespace RulEng.Prescriptions
{
    /// <summary>
    /// Prescription to Mutate (Create / Update) Entities
    /// </summary>
    public class OperationMxProcessing : IOpReqProcessing
    {
        public IImmutableList<OperandKey> Entities { get; set; }
    }
}
