using System;
using System.Collections.Generic;
using System.Linq;
using RulEng.Helpers;
using RulEng.Prescriptions;
using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Reformers
{
    public static class ProcessingCompareReducers
    {
        /// <summary>
        /// Perform all Compare Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <param name="ruleType"></param>
        /// <returns></returns>
        public static ProcessingRulEngStore AllCompare(this ProcessingRulEngStore newState, IRuleValueProcessing prescription, RuleType ruleType)
        {
            if (prescription == null)
            {
                return newState;
            }

            var compareRuleTypes = new [] { RuleType.LessThan, RuleType.Equal, RuleType.GreaterThan, RuleType.RegularExpression };

            if (!compareRuleTypes.Contains(ruleType)) {
                throw new ArgumentOutOfRangeException(nameof(ruleType), $"{nameof(ruleType)} must be a comparison RuleType");
            }

            var actionDate = DateTime.UtcNow;

            // First get the potentially relevant entities (Values only) in a cleaned form
            var ruleResultEntitySets = prescription.Entities
                .Where(vi => vi.EntityIds.Count(ve => ve.EntType == EntityType.Value) >= vi.MinEntitiesRequired)
                .Select(pe => new {
                    pe.RuleResultId,
                    Entities = new List<Value>(),
                    EntityIds = pe.EntityIds
                        .Where(ve => ve.EntType == EntityType.Value)
                        .Select(ve => ve.EntityId)
                        .ToList()
                })
                .Distinct()
                .ToList();

            foreach (var ruleResultEntitySet in ruleResultEntitySets)
            {
                ruleResultEntitySet.Entities
                    .AddRange(newState.Values.Where(v => ruleResultEntitySet.EntityIds.Contains(v.EntityId)));
            }

            var entities = ruleResultEntitySets
                .SelectMany(v => v.Entities)
                .Distinct()
                .Select(v => (IEntity)v)
                .ToList();

            // Get the corresponding Rules
            var rulesToProcessList = newState.Rules.RulesToProcess(ruleType, entities);

            foreach (var presValues in ruleResultEntitySets)
            {
                var presEntities = presValues.Entities.ToArray();

                var ruleToProcess = rulesToProcessList
                    .SingleOrDefault(r => r.ReferenceValues.Any(rv => rv.RuleResultId == presValues.RuleResultId));

                RuleResult newRuleResult = null;

                switch (ruleType)
                {
                    case RuleType.LessThan:
                        newRuleResult = ruleToProcess.LessThanTest(presEntities, presValues.RuleResultId, actionDate);
                        break;
                    case RuleType.Equal:
                        newRuleResult = ruleToProcess.EqualTest(presEntities, presValues.RuleResultId, actionDate);
                        break;
                    case RuleType.GreaterThan:
                        newRuleResult = ruleToProcess.GreaterThanTest(presEntities, presValues.RuleResultId, actionDate);
                        break;
                    case RuleType.RegularExpression:
                        newRuleResult = ruleToProcess.RegexTest(presEntities, presValues.RuleResultId, actionDate);
                        break;
                }

                if (newRuleResult != null)
                {
                    newState.RuleResults.Add(newRuleResult);
                }
            }

            return newState;
        }
    }
}
