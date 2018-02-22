﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class OperandKeyHelpersHelpers
    {
        /// <summary>
        /// Create an OperandKey from a Value
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

            var opKey = new OperandKey { EntType = entType, EntityId = entityId.Value, SourceValueIds = ImmutableArray.Create(value.EntityId) };

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
            if (!entityId.HasValue || entityId == Guid.Empty)
            {
                entityId = Guid.NewGuid();
            }

            var opKey = new OperandKey { EntType = entType, EntityId = entityId.Value, SourceValueIds = ImmutableArray.CreateRange(values.Select(v => v.EntityId)) };

            return opKey;
        }
    }
}