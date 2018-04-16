// These define Prescriptions (Redux Actions) performed by Search Operations

using System.Collections.Immutable;
using RulEng.States;

namespace RulEng.Prescriptions
{
    /// <summary>
    /// Prescription to Search for Entities
    /// </summary>
    public class OperationSxProcessing : IOpReqProcessing
    {
        public IImmutableList<OperandKey> Entities { get; set; }
    }
}
