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

            var minEntitiesRequired = prescription.Entities.MinEntitiesRequired;
            if (prescription.Entities.EntityIds.Count < minEntitiesRequired)
            {
                return newState;
            }

            var compareRuleTypes = new [] { RuleType.LessThan, RuleType.Equal, RuleType.GreaterThan, RuleType.RegularExpression };

            if (!compareRuleTypes.Contains(ruleType)) {
                throw new ArgumentOutOfRangeException(nameof(ruleType), $"{nameof(ruleType)} must be a comparison RuleType");
            }

            var actionDate = DateTime.UtcNow;


            // First get the potentially relevant entities (Values only) in a cleaned form
            var ruleResultId = prescription.Entities.RuleResultId;
            var ruleResultEntitySet = new {
                    RuleResultId = ruleResultId,
                    Entities = new List<Value>(),
                    EntityIds = prescription.Entities.EntityIds
                        .Where(ve => ve.EntType == EntityType.Value)
                        .Select(ve => ve.EntityId)
                        .ToList()
                };

            ruleResultEntitySet.Entities
                .AddRange(newState.Values.Where(v => ruleResultEntitySet.EntityIds.Contains(v.EntityId)));

            var entities = ruleResultEntitySet.Entities
                .Select(v => (IEntity)v)
                .ToList();

            // Get the corresponding Rules
            var rulesToProcessList = newState.Rules.RulesToProcess(ruleType, entities);

            var presEntities = ruleResultEntitySet.Entities.ToArray();

            var ruleToProcess = rulesToProcessList
                .SingleOrDefault(r => r.ReferenceValues.RuleResultId == ruleResultId);

            RuleResult newRuleResult = null;

            switch (ruleType)
            {
                case RuleType.LessThan:
                    newRuleResult = ruleToProcess.LessThanTest(presEntities, ruleResultEntitySet.RuleResultId, actionDate);
                    break;
                case RuleType.Equal:
                    newRuleResult = ruleToProcess.EqualTest(presEntities, ruleResultEntitySet.RuleResultId, actionDate);
                    break;
                case RuleType.GreaterThan:
                    newRuleResult = ruleToProcess.GreaterThanTest(presEntities, ruleResultEntitySet.RuleResultId, actionDate);
                    break;
                case RuleType.RegularExpression:
                    newRuleResult = ruleToProcess.RegexTest(presEntities, ruleResultEntitySet.RuleResultId, actionDate);
                    break;
            }

            if (newRuleResult != null)
            {
                newState.RuleResults.Add(newRuleResult);
            }

            return newState;
        }
    }
}
