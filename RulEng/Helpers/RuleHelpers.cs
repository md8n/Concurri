using System;
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

            // Relevant Rules by RuleType and LastExecuted values
            var relevantRuleList = rules
                .Where(r => r.RuleType == ruleType && r.LastExecuted < currentTime)
                .ToList();

            // Relevant Rules for the Entities identified
            var entRelRuleList = new List<Rule>();
            foreach (var relRule in relevantRuleList)
            {
                var relRuleEnts = relRule.ReferenceValues.EntityIds
                    .Distinct()
                    .ToList();
            }

            var ruleRefEntities = rules
                .SelectMany(r => r.ReferenceValues.EntityIds)
                .Distinct()
                .ToList();

            // (Not) Rule tests
            var notRuleList = relevantRuleList
                .Where(r => r.NegateResult);
            var notRuleEntFilteredList = notRuleList
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
            var refValues = (IRulePrescription)entity.RulePrescription<RuleUnary>();

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
                ReferenceValues = value.RulePrescription<RuleUnary>()
            };

            return rule;
        }
    }
}
