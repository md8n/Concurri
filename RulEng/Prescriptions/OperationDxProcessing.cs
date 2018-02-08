// These define Prescriptions (Redux Actions) performed by Operations and Requests

using System.Collections.Immutable;
using RulEng.States;

namespace RulEng.Prescriptions
{
    /// <summary>
    /// Prescription to Delete / Deprecate Entities
    /// </summary>
    public class OperationDxProcessing : IOpReqProcessing
    {
        public IImmutableList<ITypeKey> Entities { get; set; }
    }
}
