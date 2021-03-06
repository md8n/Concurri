﻿using System;
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
        /// <param name="ruleType"></param>
        /// <returns></returns>
        public static ProcessingRulEngStore AllCollection(this ProcessingRulEngStore newState, IRuleRuleResultProcessing prescription, RuleType ruleType)
        {
            if (prescription == null)
            {
                return newState;
            }

            var collectionRuleTypes = new[] { RuleType.And, RuleType.Or, RuleType.Xor };

            if (!collectionRuleTypes.Contains(ruleType))
            {
                throw new ArgumentOutOfRangeException(nameof(ruleType), $"{nameof(ruleType)} must be a collection RuleType");
            }

            var minEntitiesRequired = prescription.Entities.MinEntitiesRequired;
            if (prescription.Entities.EntityIds.Count < minEntitiesRequired)
            {
                return newState;
            }

            var actionDate = DateTime.UtcNow;

            // First get the potentially relevant entities in a cleaned form
            var ruleResultId = prescription.Entities.RuleResultId;
            var ruleResultEntitySet = new {
                    RuleResultId = ruleResultId,
                    Entities = new List<RuleResult>(),
                    EntityIds = prescription.Entities.EntityIds
                        .Where(ve => ve.EntType == EntityType.RuleResult)
                        .Select(ve => ve.EntityId)
                };

            ruleResultEntitySet.Entities
                .AddRange(newState.RuleResults.Where(v => ruleResultEntitySet.EntityIds.Contains(v.EntityId)));

            var entities = ruleResultEntitySet.Entities
                .Distinct()
                .Select(v => (IEntity)v)
                .ToList();

            // Get the corresponding Rules
            var rulesToProcess = newState.Rules.RulesToProcess(ruleType, entities);

            var presEntities = ruleResultEntitySet.Entities.ToArray();
            var ruleToProcess = rulesToProcess
                .SingleOrDefault(r => r.ReferenceValues.RuleResultId == ruleResultId);

            RuleResult newRuleResult = null;

            switch (ruleType)
            {
                case RuleType.And:
                    newRuleResult = ruleToProcess.AndTest(presEntities, ruleResultEntitySet.RuleResultId, actionDate);
                    break;
                case RuleType.Or:
                    newRuleResult = ruleToProcess.OrTest(presEntities, ruleResultEntitySet.RuleResultId, actionDate);
                    break;
                case RuleType.Xor:
                    newRuleResult = ruleToProcess.XorTest(presEntities, ruleResultEntitySet.RuleResultId, actionDate);
                    break;
            }

            if (newRuleResult == null)
            {
                return newState;
            }

            // Get the Ids of all existing Rule Results that correspond to the Rule being processed
            var relRuleResultIds = newState.RuleResults
                .Where(rr => rr.RuleResultId == ruleToProcess.ReferenceValues.RuleResultId)
                .Select(rr => rr.RuleResultId)
                .ToList();

            if (!relRuleResultIds.Any())
            {
                // An unusual circumstance, no pre-existing RuleResult
                newState.RuleResults.Add(newRuleResult);
            }
            else
            {
                // Normal circumstances, we'd expect only one RuleResult
                foreach (var rrId in relRuleResultIds)
                {
                    var relevantRuleResult = newState.RuleResults.Single(rr => rr.RuleResultId == rrId);

                    relevantRuleResult.LastChanged = actionDate;
                    relevantRuleResult.Detail = newRuleResult.Detail;
                }
            }

            return newState;
        }
    }
}
