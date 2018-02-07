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
        /// Perform all And and Not And Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        public static ProcessingRulEngStore AllAnd(this ProcessingRulEngStore newState, ProcessAndRule prescription)
        {
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

                // There should be only 1 Rule to process, there could potentially be none
                if (ruleToProcess == null)
                {
                    continue;
                }

                var ents = presEntities.Select(pe => pe.Detail);
                var result = ents.All(e => e);

                var newRuleResult = new RuleResult
                {
                    RuleResultId = presValues.RuleResultId,
                    RuleId = ruleToProcess.RuleId,
                    LastChanged = actionDate,
                    Detail = ruleToProcess.NegateResult ? !result : result
                };

                newState.RuleResults.Add(newRuleResult);

                ruleToProcess.LastExecuted = actionDate;
            }

            return newState;
        }

        /// <summary>
        /// Perform all Or and Not Or Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        public static ProcessingRulEngStore AllOr(this ProcessingRulEngStore newState, ProcessOrRule prescription)
        {
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
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.Or, entities);

            foreach (var presValues in ruleResultEntitySets)
            {
                var presEntities = presValues.Entities.ToArray();
                var ruleToProcess = rulesToProcessList
                    .SingleOrDefault(r => r.ReferenceValues.Any(rv => rv.RuleResultId == presValues.RuleResultId));

                // There should be only 1 Rule to process, there could potentially be none
                if (ruleToProcess == null)
                {
                    continue;
                }

                var ents = presEntities.Select(pe => pe.Detail);
                var result = ents.Any(e => e == true);

                var newRuleResult = new RuleResult
                {
                    RuleResultId = presValues.RuleResultId,
                    RuleId = ruleToProcess.RuleId,
                    LastChanged = actionDate,
                    Detail = ruleToProcess.NegateResult ? !result : result
                };

                newState.RuleResults.Add(newRuleResult);

                ruleToProcess.LastExecuted = actionDate;
            }

            return newState;
        }

        /// <summary>
        /// Perform all Xor and Not Xor Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        public static ProcessingRulEngStore AllXor(this ProcessingRulEngStore newState, ProcessXorRule prescription)
        {
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
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.Xor, entities);

            foreach (var presValues in ruleResultEntitySets)
            {
                var presEntities = presValues.Entities.ToArray();
                var ruleToProcess = rulesToProcessList
                    .SingleOrDefault(r => r.ReferenceValues.Any(rv => rv.RuleResultId == presValues.RuleResultId));

                // There should be only 1 Rule to process, there could potentially be none
                if (ruleToProcess == null)
                {
                    continue;
                }

                var ents = presEntities.Select(pe => pe.Detail).ToList();
                var result = ents.Count - ents.Distinct().Count() == 0;

                var newRuleResult = new RuleResult
                {
                    RuleResultId = presValues.RuleResultId,
                    RuleId = ruleToProcess.RuleId,
                    LastChanged = actionDate,
                    Detail = ruleToProcess.NegateResult ? !result : result
                };

                newState.RuleResults.Add(newRuleResult);

                ruleToProcess.LastExecuted = actionDate;
            }

            return newState;
        }
    }
}
