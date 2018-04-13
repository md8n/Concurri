using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            // (only rules set in the future will be excluded from execution with this test)
            var relevantRuleList = rules
                .Where(r => r.RuleType == ruleType && r.LastExecuted < currentTime)
                .ToList();

            // Relevant Rules for the Entities identified
            var entRelRuleList = new List<Rule>();
            foreach (var relRule in relevantRuleList)
            {
                var useRule = false;
                var relRuleEnts = relRule.ReferenceValues.EntityIds
                    .Distinct()
                    .ToList();
                var rrEntTypes = relRuleEnts.Select(rre => rre.EntType).Distinct().ToList();

                // if even one entity has changed wrt a rule reference value that is 'watching' it
                // then it is relevant for processing
                foreach (var ent in entities.Where(e => rrEntTypes.Contains(e.EntType)))
                {
                    var rrEnt = relRuleEnts.FirstOrDefault(rre =>
                        rre.EntType == ent.EntType &&
                        rre.EntityId == ent.EntityId &&
                        rre.LastChanged <= ent.LastChanged);
                    if (rrEnt != null)
                    {
                        useRule = true;
                        break;
                    }
                }

                if (!useRule)
                {
                    continue;
                }

                entRelRuleList.Add(relRule);
            }

            return entRelRuleList;

            // Get the complete set of entities to process all of the relevant rules
            // This may be a superset of the supplied entities
            //var ruleRefEntities = entRelRuleList
            //    .SelectMany(r => r.ReferenceValues.EntityIds)
            //    .Distinct()
            //    .ToList();

            // (Not) Rule tests
            //var notRuleList = entRelRuleList
            //    .Where(r => r.NegateResult);
            //var notRuleEntFilteredList = notRuleList
            //    .Where(r => entities.Except(ruleRefEntities, new IEntityComparer()).Any());

            // Rule tests
            //var ruleList = rules
            //    .Where(r => r.RuleType == ruleType
            //        && !r.NegateResult
            //        && entities.Intersect(ruleRefEntities, new IEntityComparer()).Any()
            //        && r.LastExecuted < currentTime);

            //return notRuleList.Intersect(ruleList).ToList();
        }

        /// <summary>
        /// Create an Exists Rule for the value
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="existingRule"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule ExistsRule(this IEntity entity, Rule existingRule = null, bool negateResult = true)
        {
            var nText = negateResult ? "non-" : "";
            var ruleName = $"Test for {nText}existence of {entity.EntType.ToString()} {(TypeKey)entity}";
            var refValues = (IRulePrescription)entity.RulePrescription<RuleUnary>();

            if (existingRule == null)
            {
                existingRule = new Rule
                {
                    RuleId = Guid.NewGuid()
                };
            }

            existingRule.RuleName = ruleName;
            existingRule.RuleType = RuleType.Exists;
            existingRule.LastChanged = entity.LastChanged;
            existingRule.LastExecuted = entity.LastChanged;
            existingRule.NegateResult = negateResult;
            existingRule.ReferenceValues = refValues;

            return existingRule;
        }

        /// <summary>
        /// Create a HasMeaningfulValue Rule for the value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="existingRule"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule HasMeaningfulValueRule(this Value value, Rule existingRule = null, bool negateResult = false)
        {
            if (existingRule == null)
            {
                existingRule = new Rule
                {
                    RuleId = Guid.NewGuid()
                };
            }

            existingRule.RuleName = $"Test for meaningful value of Value {(TypeKey)value}";
            existingRule.RuleType = RuleType.HasMeaningfulValue;
            existingRule.LastChanged = value.LastChanged;
            existingRule.LastExecuted = value.LastChanged;
            existingRule.NegateResult = negateResult;
            existingRule.ReferenceValues = value.RulePrescription<RuleUnary>(existingRule);

            return existingRule;
        }

        /// <summary>
        /// Create/Update an And Rule for the rule results
        /// </summary>
        /// <param name="ruleResults"></param>
        /// <param name="existingRule"></param>
        /// <param name="negateResult"></param>
        /// <returns></returns>
        public static Rule AndRule(this List<RuleResult> ruleResults, Rule existingRule = null, bool negateResult = false)
        {
            if (ruleResults.Count < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(ruleResults),
                    $"{nameof(ruleResults)} does not meet the requirements for the minimum number of members");
            }

            var refValues = ruleResults.Select(rr => (IEntity)(TypeKey)rr);

            var refValueIds = new RuleCollect
            {
                RuleResultId = existingRule?.ReferenceValues.RuleResultId ?? Guid.NewGuid(),
                EntityIds = ImmutableList.CreateRange(refValues)
            };

            var lastChanged = ruleResults.OrderByDescending(rr => rr.LastChanged).First().LastChanged;

            if (existingRule == null)
            {
                existingRule = new Rule
                {
                    RuleId = Guid.NewGuid()
                };
            }

            existingRule.RuleName = $"Test all rule results are {!negateResult}";
            existingRule.RuleType = RuleType.And;
            existingRule.LastChanged = lastChanged;
            existingRule.LastExecuted = lastChanged;
            existingRule.NegateResult = negateResult;
            existingRule.ReferenceValues = refValueIds;

            return existingRule;
        }
    }
}
