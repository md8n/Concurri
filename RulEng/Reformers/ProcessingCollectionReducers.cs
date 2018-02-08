using System;
using System.Collections.Generic;
using System.Linq;
using RulEng.Helpers;
using RulEng.Prescriptions;
using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Reformers
{
    public static class ProcessingCollectionReducers
    {
        /// <summary>
        /// Perform all Collection Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        public static ProcessingRulEngStore AllCollection(this ProcessingRulEngStore newState, IRuleRuleResultProcessing prescription, RuleType ruleType)
        {
            var collectionRuleTypes = new RuleType[] { RuleType.And, RuleType.Or, RuleType.Xor };

            if (!collectionRuleTypes.Contains(ruleType))
            {
                throw new ArgumentOutOfRangeException(nameof(ruleType), $"{nameof(ruleType)} must be a collection RuleType");
            }

            var actionDate = DateTime.UtcNow;

            // First get the potentially relevant entities in a cleaned form
            var ruleResultEntitySets = prescription.Entities
                .Where(vi => vi.EntityIds.Count(ve => ve.EntityType == EntityType.RuleResult) >= vi.MinEntitiesRequired)
                .Select(pe => new {
                    pe.RuleResultId,
                    Entities = new List<RuleResult>(),
                    EntityIds = pe.EntityIds
                        .Where(ve => ve.EntityType == EntityType.RuleResult)
                        .Select(ve => ve.EntityId)
                })
                .Distinct()
                .ToList();

            foreach (var ruleResultEntitySet in ruleResultEntitySets)
            {
                ruleResultEntitySet.Entities
                    .AddRange(newState.RuleResults.Where(v => ruleResultEntitySet.EntityIds.Contains(v.EntityId)));
            }

            var entities = ruleResultEntitySets
                .SelectMany(v => v.Entities)
                .Distinct()
                .Select(v => (TypeKey)v)
                .ToList();

            // Get the corresponding Rules
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.And, entities);

            foreach (var presValues in ruleResultEntitySets)
            {
                var presEntities = presValues.Entities.ToArray();
                var ruleToProcess = rulesToProcessList
                    .SingleOrDefault(r => r.ReferenceValues.Any(rv => rv.RuleResultId == presValues.RuleResultId));

                RuleResult newRuleResult = null;

                switch (ruleType)
                {
                    case RuleType.And:
                        newRuleResult = ruleToProcess.AndTest(presEntities, presValues.RuleResultId, actionDate);
                        break;
                    case RuleType.Or:
                        newRuleResult = ruleToProcess.OrTest(presEntities, presValues.RuleResultId, actionDate);
                        break;
                    case RuleType.Xor:
                        newRuleResult = ruleToProcess.XorTest(presEntities, presValues.RuleResultId, actionDate);
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
