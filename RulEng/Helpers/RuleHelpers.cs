using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;

using RulEng.States;

namespace RulEng.Helpers
{
    public static class RuleHelpers
    {
        public static List<Rule> RulesToProcess(this HashSet<Rule> rules, RuleType ruleType, List<IEntity> entities)
        {
            var currentTime = DateTime.UtcNow;

            var ruleRefEntities = rules
                .SelectMany(r => r.ReferenceValues.SelectMany(rv => rv.EntityIds).Distinct())
                .Distinct()
                .Select(r => (IEntity)r)
                .ToList();

            // (Not) Rule tests
            var notRuleList = rules
                .Where(r => r.RuleType == ruleType
                    && r.NegateResult
                    && entities.Except(ruleRefEntities, new IEntityComparer()).Any()
                    && r.LastExecuted < currentTime);

            // Rule tests
            var ruleList = rules
                .Where(r => r.RuleType == ruleType
                    && !r.NegateResult
                    && entities.Intersect(ruleRefEntities, new IEntityComparer()).Any()
                    && r.LastExecuted < currentTime);

            return notRuleList.Intersect(ruleList).ToList();
        }

        /// <summary>
        /// Create an Exists Rule for the value
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule ExistsRule(this IEntity entity, bool negateResult = true)
        {
            var nText = negateResult ? "non-" : "";
            var ruleName = $"Test for {nText}existence of {entity.EntType.ToString()} {((TypeKey)entity)}";
            var refValues = ImmutableArray.Create((IRulePrescription)entity.RulePrescription<RuleUnary>());

            var rule = new Rule
            {
                RuleId = Guid.NewGuid(),
                RuleName = ruleName,
                RuleType = RuleType.Exists,
                LastChanged = entity.LastChanged,
                LastExecuted = entity.LastChanged,
                NegateResult = negateResult,
                ReferenceValues = refValues
            };

            return rule;
        }

        /// <summary>
        /// Create an Exists Rule for the supplied entities
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule ExistsRule(this IEnumerable<IEntity> entities, bool negateResult = true)
        {
            var nText = negateResult ? "non-" : "";
            var earliestDate = entities.Select(e => e.LastChanged).OrderBy(e => e).First();

            var refValues = entities.RulePresciptions<RuleUnary>();

            var rule = new Rule
            {
                RuleId = Guid.NewGuid(),
                RuleName = $"Test for {nText}existence of entities",
                RuleType = RuleType.Exists,
                LastChanged = earliestDate,
                LastExecuted = earliestDate,
                NegateResult = negateResult,
                ReferenceValues = ImmutableArray.CreateRange(refValues.Select(r => (IRulePrescription)r))
            };

            return rule;
        }

        /// <summary>
        /// Create a HasMeaningfulValue Rule for the value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule HasMeaningfulValueRule(this IEntity value, bool negateResult = false)
        {
            var rule = new Rule
            {
                RuleId = Guid.NewGuid(),
                RuleName = $"Test for meaningful value of Value {(TypeKey)value}",
                RuleType = RuleType.HasMeaningfulValue,
                LastChanged = value.LastChanged,
                LastExecuted = value.LastChanged,
                NegateResult = negateResult,
                ReferenceValues = ImmutableArray.Create((IRulePrescription)value.RulePrescription<RuleUnary>())
            };

            return rule;
        }
    }
}
