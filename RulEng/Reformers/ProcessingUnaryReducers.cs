using System;
using System.Linq;
using RulEng.Helpers;
using RulEng.Prescriptions;
using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Reformers
{
    public static class ProcessingUnaryReducers
    {
        /// <summary>
        /// Perform all Exists and Not Exists Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        public static ProcessingRulEngStore AllExists(this ProcessingRulEngStore newState, ProcessExistsRule prescription)
        {
            if (prescription == null)
            {
                return newState;
            }

            var actionDate = DateTime.UtcNow;

            // First identify the potentially relevant entities
            var entities = newState.Rules.Select(r => (IEntity)r).ToList();
            entities.AddRange(newState.Values.Select(v => (IEntity)v));
            entities.AddRange(newState.Operations.Select(o => (IEntity)o));
            entities.AddRange(newState.Requests.Select(rq => (IEntity)rq));

            // Get the corresponding Rules
            var rulesToProcess = newState.Rules.RulesToProcess(RuleType.Exists, entities);

            foreach (var ruleToProcess in rulesToProcess)
            {
                // Get the Ids of all existing Rule Results that correspond to the Rule being processed
                var relRuleResultIds = newState.RuleResults
                    .Where(rr => rr.RuleResultId == ruleToProcess.ReferenceValues.RuleResultId)
                    .Select(rr => rr.RuleResultId)
                    .ToList();

                if (!relRuleResultIds.Any())
                {
                    // An unusual circumstance, no pre-existing RuleResult
                    // Create a new RuleResult using the provided ID
                    // We are here because the result is true, so we write the opposite of NegateResult as the correct output
                    var newRuleResult = new RuleResult
                    {
                        RuleId = ruleToProcess.RuleId,
                        LastChanged = actionDate,
                        Detail = !ruleToProcess.NegateResult
                    };

                    newState.RuleResults.Add(newRuleResult);
                }
                else
                {
                    // Normal circumstances, we'd expect only one RuleResult
                    foreach(var rrId in relRuleResultIds)
                    {
                        var relevantRuleResult = newState.RuleResults.Single(rr => rr.RuleResultId == rrId);

                        relevantRuleResult.LastChanged = actionDate;
                        relevantRuleResult.Detail = !ruleToProcess.NegateResult;
                    }
                }

                // Mark this Rule as executed
                ruleToProcess.LastExecuted = actionDate;
            }

            // Mark the entities within this prescription as dispensed
            // TODO: determine the relevance of this
            foreach(var entId in prescription.Entities.EntityIds)
            {
                entId.LastChanged = actionDate;
            }

            return newState;
        }

        /// <summary>
        /// Perform all HasMeaningfulValue and Not HasMeaningfulValue Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        public static ProcessingRulEngStore AllHasMeaningfulValue(this ProcessingRulEngStore newState, ProcessHasMeaningfulValueRule prescription)
        {
            if (prescription == null)
            {
                return newState;
            }

            var actionDate = DateTime.UtcNow;

            // First identify the potentially relevant Entities
            var entities = newState.Values.Select(v => (IEntity)v).ToList();

            // Get all the rules to process
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.HasMeaningfulValue, entities);

            foreach (var ruleToProcess in rulesToProcessList)
            {
                var refValue = newState.Values.FirstOrDefault(v => v.EntityId == ruleToProcess.ReferenceValues.EntityIds[0].EntityId);

                //var entitiesToAdd = ruleToProcess.ReferenceValues.Except(entities).ToList();
                var newRuleResult = new RuleResult
                {
                    RuleId = ruleToProcess.RuleId,
                    LastChanged = actionDate,
                    Detail = refValue.HasMeaningfulValue()
                };

                newState.RuleResults.Add(newRuleResult);

                ruleToProcess.LastExecuted = actionDate;
            }

            return newState;
        }
    }
}
