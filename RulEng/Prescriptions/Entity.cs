using System;
using RulEng.States;

/// <summary>
/// These define Prescriptions (Redux Actions) to be performed on the RulEng Entities (Rules, Operations, Requests, Values) themselves
/// </summary>
namespace RulEng.Prescriptions
{
    public class Create<T>: ICrud where T: IEntity
    {
        public T Entity { get; set; }
    }

    public class Delete<T> : ICrud where T : IEntity
    {
        public Guid EntityId { get; set; }
    }
}
