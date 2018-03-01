using System;
using System.Collections.Generic;

namespace RulEng.States
{
    public interface IEntity
    {
        Guid EntityId { get; set; }

        EntityType EntType { get; }

        DateTime LastChanged { get; set; }
    }


    public class IEntityComparer : IEqualityComparer<IEntity>
    {
        public bool Equals(IEntity x, IEntity y)
        {
            //Check whether the objects are the same object. 
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            //Check whether the products' properties are equal. 
            return x != null && y != null && x.EntType.Equals(y.EntType) && x.EntityId.Equals(y.EntityId);
        }

        public int GetHashCode(IEntity obj)
        {
            //Get hash code for the EntityId field if it is not null. 
            var hashEntityId = obj.EntityId.GetHashCode();

            //Get hash code for the EntType field. 
            var hashEntType = obj.EntType.GetHashCode();

            //Calculate the hash code for the product. 
            return hashEntityId ^ hashEntType;
        }
    }
}
