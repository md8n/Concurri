﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        /// <returns></returns>
        public static T RulePrescription<T>(this IEntity entity) where T: IRulePrescription, new()
        {
            var refValue = new T { RuleResultId = Guid.NewGuid(), EntityIds = ImmutableList.Create(entity) };

            return refValue;
        }

        /// <summary>
        /// Create RulePrescriptions (implicitly defining RuleResults) from an enumerable of ITypeKeys
        /// This can be used as the Entities value within an IRuleProcessing derived object
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public static T[] RulePresciptions<T>(this IEnumerable<IEntity> entities) where T : IRulePrescription, new()
        {
            var refValues = entities.Select(e => e.RulePrescription<T>()).ToArray();

            return refValues;
        }

        /// <summary>
        /// Create a RulePrescription (implicitly defining a RuleResult) from a Processable Entity
        /// This can be used as the Entities value within an IRuleProcessing derived object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TU"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static TU RulePrescription<T, TU>(this T entity) where T : IEntity where TU : IRulePrescription, new()
        {
            if (!entity.IsProcessable())
            {
                throw new ArgumentOutOfRangeException(nameof(entity), "RulePrescription helper creator is only for Processable entity types");
            }

            var entTypeKey = new TypeKey { EntityId = entity.EntityId, EntType = entity.EntType, LastChanged = entity.LastChanged };
            var refValue = new TU { RuleResultId = Guid.NewGuid(), EntityIds = ImmutableList.Create((IEntity)entTypeKey) };

            return refValue;
        }

        public static ImmutableArray<TU> RulePresciptions<T, TU>(this IEnumerable<T> entities) where T : IEntity where TU : IRulePrescription, new()
        {
            var refValues = entities.Select(e => e.RulePrescription<T, TU>()).ToArray();

            return ImmutableArray.CreateRange(refValues);
        }
    }
}
