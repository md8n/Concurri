using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class OperandKeyHelpersHelpers
    {
        /// <summary>
        /// Create an OperandKey for a supplied Entity - this OperandKey will not specify any SourceEntityIds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static OperandKey OperandKey<T>(this T entity) where T : IEntity
        {
            if (entity == null || entity.EntityId == Guid.Empty || !entity.IsProcessable() )
            {
                throw new ArgumentException("The supplied entity was null, had no Id, or was not processable");
            }

            var opKey = new OperandKey { EntType = entity.EntType, EntityId = entity.EntityId, SourceEntityIds = ImmutableArray<Guid>.Empty };

            return opKey;
        }

        /// <summary>
        /// Create an OperandKey for a supplied EntityType and EntityId - this OperandKey will not specify any SourceEntityIds
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static OperandKey OperandKey(this EntityType entType, Guid entityId)
        {
            if (entityId == Guid.Empty || !entType.IsProcessable())
            {
                throw new ArgumentException("The supplied entityId was empty, or the supplied entType was not processable");
            }

            var opKey = new OperandKey { EntType = entType, EntityId = entityId, SourceEntityIds = ImmutableArray<Guid>.Empty };

            return opKey;
        }

        /// <summary>
        /// Create an OperandKey from a single Value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="entType"></param>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public static OperandKey OperandKey(this Value value, EntityType entType, Guid? entityId = null)
        {
            if (!entityId.HasValue || entityId == Guid.Empty)
            {
                entityId = Guid.NewGuid();
            }

            var opKey = new OperandKey { EntType = entType, EntityId = entityId.Value, SourceEntityIds = ImmutableArray.Create(value.EntityId) };

            return opKey;
        }

        /// <summary>
        /// Create an OperandKey from an enumerable of Values
        /// </summary>
        /// <param name="values"></param>
        /// <param name="entType"></param>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public static OperandKey OperandKey(this IEnumerable<Value> values, EntityType entType, Guid? entityId = null)
        {
            return values.Select(v => v.EntityId).OperandKey(entType, entityId);
        }

        /// <summary>
        /// Create an OperandKey from an enumerable of Entity Ids
        /// </summary>
        /// <param name="sourceValueIds"></param>
        /// <param name="entType"></param>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public static OperandKey OperandKey(this IEnumerable<Guid> sourceValueIds, EntityType entType, Guid? entityId = null)
        {
            if (!entityId.HasValue || entityId == Guid.Empty)
            {
                entityId = Guid.NewGuid();
            }

            var opKey = new OperandKey { EntType = entType, EntityId = entityId.Value, SourceEntityIds = ImmutableArray.CreateRange(sourceValueIds) };

            return opKey;
        }
    }
}
