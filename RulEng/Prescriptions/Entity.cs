// These define Prescriptions (Redux Actions) to be performed on the RulEng Entities (Rules, Operations, Requests, Values) themselves

using System;
using RulEng.States;
using Newtonsoft.Json;

namespace RulEng.Prescriptions
{
    public class Create<T>: ICrud where T: IEntity
    {
        public T Entity { get; set; }
    }

    public class Delete<T> : ICrud where T : IEntity
    {
        [JsonIgnore]
        public Guid EntityId { get; set; }
    }
}
