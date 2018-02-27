using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RulEng.Helpers;
using System.Collections.Generic;

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

    public class TypeKeyComparer : IEqualityComparer<TypeKey>
    {
        public bool Equals(TypeKey x, TypeKey y)
        {
            //Check whether the objects are the same object. 
            if (object.ReferenceEquals(x, y)) return true;

            //Check whether the products' properties are equal. 
            return x != null && y != null && x.EntType.Equals(y.EntType) && x.EntityId.Equals(y.EntityId);
        }

        public int GetHashCode(TypeKey obj)
        {
            //Get hash code for the EntityId field if it is not null. 
            int hashEntityId = obj.EntityId == null ? 0 : obj.EntityId.GetHashCode();

            //Get hash code for the EntType field. 
            int hashEntType = obj.EntType.GetHashCode();

            //Calculate the hash code for the product. 
            return hashEntityId ^ hashEntType;
        }
    }
}
