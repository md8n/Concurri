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
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.Exists, entities);

            foreach (var ruleToProcess in rulesToProcessList)
            {
                //var entitiesToAdd = ruleToProcess.ReferenceValues.Except(entities).ToList();
                var newRuleResult = new RuleResult
                {
                    RuleId = ruleToProcess.RuleId,
                    LastChanged = actionDate,
                    Detail = true
                };

                newState.RuleResults.Add(newRuleResult);

                ruleToProcess.LastExecuted = actionDate;
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

            if (rulesToProcessList.Any(r => r.ReferenceValues.Count() != 1))
            {
                throw new Exception("HasMeaningfulValue Rules currently only support testing a single value");
            }

            foreach (var ruleToProcess in rulesToProcessList)
            {
                var refValue = newState.Values.FirstOrDefault(v => v.EntityId == ruleToProcess.ReferenceValues[0].EntityIds[0].EntityId);

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
