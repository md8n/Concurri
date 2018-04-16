// These define Prescriptions (Redux Actions) to be performed on the RulEng Entities (Rules, Operations, Requests, Values) themselves

using System;
using RulEng.States;

namespace RulEng.Prescriptions
{
    public class Create<T> : ICrud where T : IEntity
    {
        public T Entity { get; set; }
    }

    public class Delete<T> : ICrud where T : IEntity
    {
        public T Entity { get; set; }
    }

    public class AddUpdate<T> : OperationMxProcessing where T : IEntity
    {
        public T Entity { get; set; }
    }

    public class DeleteEnt<T> : OperationDxProcessing where T : IEntity
    {
        public T Entity { get; set; }
    }

    public class Search<T> : OperationSxProcessing where T : IEntity
    {
        public T Entity { get; set; }
    }
}
