using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using RulEng.States;

namespace RulEng.Helpers
{
    public static class RulePrescriptionHelpers
    {
        /// <summary>
        /// Create a RulePrescription (implicitly defining a RuleResult) from an ITypeKey
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IRulePrescription RulePrescription (this ITypeKey entity)
        {
            IRulePrescription refValue = new RulePrescription { RuleResultId = Guid.NewGuid(), EntityIds = ImmutableList.Create(entity) };

            return refValue;
        }

        /// <summary>
        /// Create RulePrescriptions (implicitly defining RuleResults) from an enumerable of ITypeKeys
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static ImmutableArray<IRulePrescription> RulePresciptions (this IEnumerable<ITypeKey> entities)
        {
            return ImmutableArray.CreateRange(entities.Select(e => e.RulePrescription()));
        }

        /// <summary>
        /// Create a RulePrescription (implicitly defining a RuleResult) from a Processable Entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static IRulePrescription RulePrescription<T>(this T entity) where T : IEntity
        {
            if (!entity.IsProcessable())
            {
                throw new ArgumentOutOfRangeException("entity.Type", "RulePrescription helper creator is only for Processable entity types");
            }

            var entTypeKey = new TypeKey { EntityId = entity.EntityId, EntityType = entity.Type, LastChanged = entity.LastChanged };
            IRulePrescription refValue = new RulePrescription { RuleResultId = Guid.NewGuid(), EntityIds = ImmutableList.Create((ITypeKey)entTypeKey) };

            return refValue;
        }

        public static ImmutableArray<IRulePrescription> RulePresciptions<T>(this IEnumerable<T> entities) where T : IEntity
        {
            return ImmutableArray.CreateRange(entities.Select(e => e.RulePrescription()));
        }

    }
}
