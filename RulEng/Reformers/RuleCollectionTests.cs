using RulEng.Helpers;
using RulEng.States;
using System;
using System.Linq;

namespace RulEng.Reformers
{
    public static class RuleCollectionTests
    {
        /// <summary>
        /// Processes the supplied And Rule, sets it lastExecuted time, and returns the result
        /// </summary>
        /// <param name="ruleToProcess"></param>
        /// <param name="presEntities"></param>
        /// <param name="ruleResultId"></param>
        /// <param name="actionDate"></param>
        /// <returns></returns>
        public static RuleResult AndTest(this Rule ruleToProcess, RuleResult[] presEntities, Guid ruleResultId, DateTime? actionDate)
        {
            // There should be only 1 Rule to process, there could potentially be none
            if (ruleToProcess == null)
            {
                return null;
            }

            if (!actionDate.HasValue || actionDate.Value == DefaultHelpers.DefDate())
            {
                actionDate = DateTime.UtcNow;
            }

            var ents = presEntities.Select(pe => pe.Detail);
            var result = ents.All(e => e);

            ruleToProcess.LastExecuted = actionDate.Value;

            return new RuleResult
            {
                RuleResultId = ruleResultId,
                RuleId = ruleToProcess.RuleId,
                LastChanged = actionDate.Value,
                Detail = ruleToProcess.NegateResult ? !result : result
            };
        }

        /// <summary>
        /// Processes the supplied Or Rule, sets it lastExecuted time, and returns the result
        /// </summary>
        /// <param name="ruleToProcess"></param>
        /// <param name="presEntities"></param>
        /// <param name="ruleResultId"></param>
        /// <param name="actionDate"></param>
        /// <returns></returns>
        public static RuleResult OrTest(this Rule ruleToProcess, RuleResult[] presEntities, Guid ruleResultId, DateTime? actionDate)
        {
            // There should be only 1 Rule to process, there could potentially be none
            if (ruleToProcess == null)
            {
                return null;
            }

            if (!actionDate.HasValue || actionDate.Value == DefaultHelpers.DefDate())
            {
                actionDate = DateTime.UtcNow;
            }

            var ents = presEntities.Select(pe => pe.Detail);
            var result = ents.Any(e => e);

            ruleToProcess.LastExecuted = actionDate.Value;

            return new RuleResult
            {
                RuleResultId = ruleResultId,
                RuleId = ruleToProcess.RuleId,
                LastChanged = actionDate.Value,
                Detail = ruleToProcess.NegateResult ? !result : result
            };
        }

        /// <summary>
        /// Processes the supplied Xor Rule, sets it lastExecuted time, and returns the result
        /// </summary>
        /// <param name="ruleToProcess"></param>
        /// <param name="presEntities"></param>
        /// <param name="ruleResultId"></param>
        /// <param name="actionDate"></param>
        /// <returns></returns>
        public static RuleResult XorTest(this Rule ruleToProcess, RuleResult[] presEntities, Guid ruleResultId, DateTime? actionDate)
        {
            // There should be only 1 Rule to process, there could potentially be none
            if (ruleToProcess == null)
            {
                return null;
            }

            if (!actionDate.HasValue || actionDate.Value == DefaultHelpers.DefDate())
            {
                actionDate = DateTime.UtcNow;
            }

            var ents = presEntities.Select(pe => pe.Detail).ToList();
            var result = ents.Count - ents.Distinct().Count() == 0;

            ruleToProcess.LastExecuted = actionDate.Value;

            return new RuleResult
            {
                RuleResultId = ruleResultId,
                RuleId = ruleToProcess.RuleId,
                LastChanged = actionDate.Value,
                Detail = ruleToProcess.NegateResult ? !result : result
            };
        }

    }
}
