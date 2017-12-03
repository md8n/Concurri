using System;
using System.Collections.Immutable;
using RulEng.States;

/// <summary>
/// These define Prescriptions (Redux Actions) performed by Mutation Operations
/// </summary>
namespace RulEng.Prescriptions
{
    /// <summary>
    /// Prescription to Mutate (Create / Update) Entities
    /// </summary>
    public class OperationMxBaseProcessing : IOpReqProcessing
    {
        public IImmutableList<OperandKey> Entities { get; set; }
    }

    public class OperationMxAssignProcessing : OperationMxBaseProcessing { }

    public class OperationMxAddProcessing : OperationMxBaseProcessing { }
}
