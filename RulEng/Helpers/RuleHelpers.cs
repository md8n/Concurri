using System;
using System.Collections.Immutable;
using System.Collections.Generic;

using RulEng.States;
using System.Linq;

namespace RulEng.Helpers
{
    public static class RuleHelpers
    {
        public static Rule UpdateMaxEntitiesUsed(this Rule rule)
        {
            // Ensure the MaxEntitiesUsed value is set (for Collection prescriptions)
            foreach (var refValue in rule.ReferenceValues.Where(rv => !rv.MaxEntitiesUsed.HasValue))
            {
                refValue.MaxEntitiesUsed = refValue.EntityIds.Count;
            }

            return rule;
        }

        public static List<Rule> RulesToProcess(this HashSet<Rule> rules, RuleType ruleType, List<TypeKey> entities)
        {
            var currentTime = DateTime.UtcNow;

            // Ensure the MaxEntitiesUsed value is set (for Collection prescriptions)
            foreach (var rule in rules.Where(r => r.ReferenceValues.Any(rv => !rv.MaxEntitiesUsed.HasValue)))
            {
                rule.UpdateMaxEntitiesUsed();
            }

            var ruleRefEntities = rules
                .SelectMany(r => r.ReferenceValues.SelectMany(rv => rv.EntityIds.Take(rv.MaxEntitiesUsed.Value)).Distinct())
                .Distinct()
                .ToList();

            // (Not) Rule tests
            var notRuleList = rules
                .Where(r => r.RuleType == ruleType
                    && r.NegateResult
                    && entities.Except(ruleRefEntities).Any()
                    && r.LastExecuted < currentTime);

            // Rule tests
            var ruleList = rules
                .Where(r => r.RuleType == ruleType
                    && !r.NegateResult
                    && entities.Intersect(ruleRefEntities).Any()
                    && r.LastExecuted < currentTime);

            return notRuleList.Intersect(ruleList).ToList();
        }

        /// <summary>
        /// Create an Exists Rule for the value
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule ExistsRule(this ITypeKey entity, bool negateResult = true)
        {
            var nText = negateResult ? "non-" : "";

            var rule = new Rule
            {
                RuleId = Guid.NewGuid(),
                RuleName = $"Test for {nText}existence of {entity.EntityType.ToString()} {((TypeKey)entity).ToString()}",
                RuleType = RuleType.Exists,
                LastChanged = entity.LastChanged,
                LastExecuted = entity.LastChanged,
                NegateResult = negateResult,
                ReferenceValues = ImmutableArray.Create(entity.RulePrescription())
            };

            return rule;
        }

        /// <summary>
        /// Create an Exists Rule for the supplied entities
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule ExistsRule(this IEnumerable<ITypeKey> entities, bool negateResult = true)
        {
            var nText = negateResult ? "non-" : "";
            var earliestDate = entities.Select(e => e.LastChanged).OrderBy(e => e).First();

            var rule = new Rule
            {
                RuleId = Guid.NewGuid(),
                RuleName = $"Test for {nText}existence of entities",
                RuleType = RuleType.Exists,
                LastChanged = earliestDate,
                LastExecuted = earliestDate,
                NegateResult = negateResult,
                ReferenceValues = entities.RulePresciptions()
            };

            return rule;
        }

        /// <summary>
        /// Create a HasMeaningfulValue Rule for the value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule HasMeaningfulValueRule(this ITypeKey value, bool negateResult = false)
        {
            var rule = new Rule
            {
                RuleId = Guid.NewGuid(),
                RuleName = $"Test for meaningful value of Value {((TypeKey)value).ToString()}",
                RuleType = RuleType.HasMeaningfulValue,
                LastChanged = value.LastChanged,
                LastExecuted = value.LastChanged,
                NegateResult = negateResult,
                ReferenceValues = ImmutableArray.Create(value.RulePrescription())
            };

            return rule;
        }

    }
}
