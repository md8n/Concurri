using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RulEng.Helpers;

namespace RulEng.States
{
    /// <summary>
    /// An instantiable implementation of the IEntity interface
    /// </summary>
    public class TypeKey : IEntity
    {
        public EntityType EntType { get; set; }
        public Guid EntityId { get; set; }
        public DateTime LastChanged { get; set; } = DefaultHelpers.DefDate();

        public override string ToString()
        {
            return JObject.FromObject(this).ToString(Formatting.None);
        }
    }
}
