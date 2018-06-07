using System;
using System.Collections.Immutable;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class RulePrescriptionHelpers
    {
        /// <summary>
        /// Create a RulePrescription (implicitly defining a RuleResult) from an ITypeKey.
        /// This can be used as the Entities value within an IRuleProcessing derived object
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="existingRule"></param>
        /// <returns></returns>
        public static T RulePrescription<T>(this IEntity entity, Rule existingRule = null) where T: IRulePrescription, new()
        {
            var refValue = new T { RuleResultId = existingRule?.ReferenceValues.RuleResultId ?? GuidHelpers.NewTimeUuid(), EntityIds = ImmutableList.Create(entity) };

            return refValue;
        }

        /// <summary>
        /// Create a RulePrescription (implicitly defining a RuleResult) from a Processable Entity
        /// This can be used as the Entities value within an IRuleProcessing derived object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU"></typeparam>
        /// <param name="entity"></param>
        /// <param name="existingRule"></param>
        /// <returns></returns>
        public static TU RulePrescription<T, TU>(this T entity, Rule existingRule = null) where T : IEntity where TU : IRulePrescription, new()
        {
            if (!entity.IsProcessable())
            {
                throw new ArgumentOutOfRangeException(nameof(entity), "RulePrescription helper creator is only for Processable entity types");
            }

            var entTypeKey = new TypeKey { EntityId = entity.EntityId, EntType = entity.EntType, EntTags = entity.EntTags, LastChanged = entity.LastChanged };
            var refValue = new TU { RuleResultId = existingRule?.ReferenceValues.RuleResultId ?? GuidHelpers.NewTimeUuid(), EntityIds = ImmutableList.Create((IEntity)entTypeKey) };

            return refValue;
        }
    }
}
