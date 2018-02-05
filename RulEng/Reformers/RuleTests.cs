using RulEng.Helpers;
using RulEng.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RulEng.Reformers
{
    public static class RuleTests
    {
        /// <summary>
        /// Processes the supplied Less Than Rule, sets it lastExecuted time, and returns the result
        /// </summary>
        /// <param name="ruleToProcess"></param>
        /// <param name="presEntities"></param>
        /// <param name="ruleResultId"></param>
        /// <param name="actionDate"></param>
        /// <returns></returns>
        public static RuleResult LessThanTest(this Rule ruleToProcess, Value[] presEntities, Guid ruleResultId, DateTime? actionDate)
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

            var result = true;

            // All the Details must be of the same type
            var firstDetailType = presEntities[0].Detail.Type;
            if (presEntities.Any(pe => pe.Detail.Type != firstDetailType))
            {
                result = false;
            }
            else
            {
                for (var ix = 1; ix < presEntities.Length; ix++)
                {
                    var prevDetail = presEntities[ix - 1].Detail;
                    var nextDetail = presEntities[ix].Detail;
                    if (prevDetail.IsNumeric())
                    {
                        if (prevDetail.GetNumeric() < nextDetail.GetNumeric())
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsDate())
                    {
                        if (prevDetail.GetDate() < nextDetail.GetDate())
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsText())
                    {
                        if (string.CompareOrdinal(prevDetail.GetText(), nextDetail.GetText()) < 0)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsGuid())
                    {
                        if (string.CompareOrdinal(prevDetail.GetGuid().ToString(), nextDetail.GetGuid().ToString()) < 0)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsBool())
                    {
                        var min1Val = !prevDetail.GetBool().HasValue
                            ? -1 : prevDetail.GetBool().Value ? 0 : 1;
                        var currVal = !nextDetail.GetBool().HasValue
                            ? -1 : nextDetail.GetBool().Value ? 0 : 1;

                        if (min1Val < currVal)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }
                }
            }

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
        /// Processes the supplied Equal Rule, sets it lastExecuted time, and returns the result
        /// </summary>
        /// <param name="ruleToProcess"></param>
        /// <param name="presEntities"></param>
        /// <param name="ruleResultId"></param>
        /// <param name="actionDate"></param>
        /// <returns></returns>
        public static RuleResult EqualTest(this Rule ruleToProcess, Value[] presEntities, Guid ruleResultId, DateTime? actionDate)
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

            var result = true;

            // All the Details must be of the same type
            var firstDetailType = presEntities[0].Detail.Type;
            if (presEntities.Any(pe => pe.Detail.Type != firstDetailType))
            {
                result = false;
            }
            else
            {
                for (var ix = 1; ix < presEntities.Length; ix++)
                {
                    var prevDetail = presEntities[ix - 1].Detail;
                    var nextDetail = presEntities[ix].Detail;
                    if (prevDetail.IsNumeric())
                    {
                        if (prevDetail.GetNumeric() == nextDetail.GetNumeric())
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsDate())
                    {
                        if (prevDetail.GetDate() == nextDetail.GetDate())
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsText())
                    {
                        if (string.CompareOrdinal(prevDetail.GetText(), nextDetail.GetText()) == 0)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsGuid())
                    {
                        if (string.CompareOrdinal(prevDetail.GetGuid().ToString(), nextDetail.GetGuid().ToString()) == 0)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsBool())
                    {
                        var min1Val = !prevDetail.GetBool().HasValue
                            ? -1 : prevDetail.GetBool().Value ? 0 : 1;
                        var currVal = !nextDetail.GetBool().HasValue
                            ? -1 : nextDetail.GetBool().Value ? 0 : 1;

                        if (min1Val < currVal)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }
                }
            }

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
        /// Processes the supplied Greater Than Rule, sets it lastExecuted time, and returns the result
        /// </summary>
        /// <param name="ruleToProcess"></param>
        /// <param name="presEntities"></param>
        /// <param name="ruleResultId"></param>
        /// <param name="actionDate"></param>
        /// <returns></returns>
        public static RuleResult GreaterThanTest(this Rule ruleToProcess, Value[] presEntities, Guid ruleResultId, DateTime? actionDate)
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

            var result = true;

            // All the Details must be of the same type
            var firstDetailType = presEntities[0].Detail.Type;
            if (presEntities.Any(pe => pe.Detail.Type != firstDetailType))
            {
                result = false;
            }
            else
            {
                for (var ix = 1; ix < presEntities.Length; ix++)
                {
                    var prevDetail = presEntities[ix - 1].Detail;
                    var nextDetail = presEntities[ix].Detail;
                    if (prevDetail.IsNumeric())
                    {
                        if (prevDetail.GetNumeric() > nextDetail.GetNumeric())
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsDate())
                    {
                        if (prevDetail.GetDate() > nextDetail.GetDate())
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsText())
                    {
                        if (string.CompareOrdinal(prevDetail.GetText(), nextDetail.GetText()) > 0)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsGuid())
                    {
                        if (string.CompareOrdinal(prevDetail.GetGuid().ToString(), nextDetail.GetGuid().ToString()) > 0)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }

                    if (prevDetail.IsBool())
                    {
                        var min1Val = !prevDetail.GetBool().HasValue
                            ? -1 : prevDetail.GetBool().Value ? 0 : 1;
                        var currVal = !nextDetail.GetBool().HasValue
                            ? -1 : nextDetail.GetBool().Value ? 0 : 1;

                        if (min1Val > currVal)
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }
                }
            }

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
