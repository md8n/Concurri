﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Jint;
using Newtonsoft.Json.Linq;
using RulEng.Prescriptions;
using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Reformers
{
    public static class ProcessingReducers
    {
        public static RulEngStore ProcessAllRulesReducer(RulEngStore previousState, IRuleProcessing prescription)
        {
            // Set up a temporary 'Processing' copy of the Store as our Unit of Work
            var newState = previousState.DeepClone();

            // First identify rules for entities that may (not) exist
            newState = newState.AllExists(prescription as ProcessExistsRule);
            // Then test for meaningful Values
            newState = newState.AllHasMeaningfulValue(prescription as ProcessHasMeaningfulValueRule);

            newState = newState.AllCompare(prescription as ProcessLessThanRule, RuleType.LessThan);
            newState = newState.AllCompare(prescription as ProcessEqualRule, RuleType.Equal);
            newState = newState.AllCompare(prescription as ProcessGreaterThanRule, RuleType.GreaterThan);
            newState = newState.AllCompare(prescription as ProcessRegexMatchRule, RuleType.RegularExpression);

            newState = newState.AllCollection(prescription as ProcessAndRule, RuleType.And);
            newState = newState.AllCollection(prescription as ProcessOrRule, RuleType.Or);
            newState = newState.AllCollection(prescription as ProcessXorRule, RuleType.Xor);

            return newState.DeepClone();
        }

        public static RulEngStore ProcessAllOperationsReducer(RulEngStore previousState, IOpReqProcessing prescription)
        {
            // Set up a temporary 'Processing' copy of the Store as our Unit of Work
            var newState = previousState.DeepClone();

            newState = newState.AllOperations(previousState, prescription);

            return newState.DeepClone();
        }

        private static ProcessingRulEngStore AllOperations(this ProcessingRulEngStore newState, RulEngStore previousState, IOpReqProcessing prescription)
        {
            // First identify rules that have just been processed successfully
            var ruleIds = newState.RuleResults
                .Where(v => v.Detail)
                .Select(v => v.RuleId)
                .ToList();
            var operationprescriptionsToProcessList = newState.Operations
                .Where(a => ruleIds.Contains(a.RuleResultId))
                .ToList();
            var requestprescriptionsToProcessList = newState.Requests
                .Where(a => ruleIds.Contains(a.RuleResultId))
                .ToList();

            // Restrict the Operation/Request Prescriptions to process to those that are guaranteed not to fail
            // Select those for which there is no conflict 
            var destinationEntities = new List<TypeKey>();
            foreach (var opPre in operationprescriptionsToProcessList)
            {
                destinationEntities.AddRange(opPre.Operands.Select(o => (TypeKey) o));
            }
            foreach (var rqPre in requestprescriptionsToProcessList)
            {
                destinationEntities.Add(rqPre);
            }

            var groupedDestinations = destinationEntities
                .GroupBy(de => new { de.EntityId, de.EntType })
                .Select(grp => new { grp.Key, Count = grp.Count() })
                .ToList();
            var conflictDestinations = groupedDestinations
                .Where(grp => grp.Count > 1)
                .Select(grp => new TypeKey { EntityId = grp.Key.EntityId, EntType = grp.Key.EntType })
                .ToList();
            var acceptableDestinations = groupedDestinations
                .Where(grp => grp.Count == 1)
                .Select(grp => new { grp.Key.EntityId, grp.Key.EntType })
                .ToList();

            // Get all of the sources from the previous state
            var acceptableSourceIds = operationprescriptionsToProcessList
                .Where(o => acceptableDestinations.Contains(new { o.EntityId, o.EntType }))
                .SelectMany(o => o.Operands.SelectMany(oo => oo.SourceValueIds))
                .ToList();
            var acceptableSources = previousState.Values
                .Where(v => acceptableSourceIds.Contains(v.EntityId))
                .ToList();

            var e = new Engine();

            var regexToken = new Regex(@".*?(?<Token>\$\{(?<Index>\d+)\}).*?");
            foreach (var ruleToProcess in ruleIds)
            {
                // Get all of the operations relevant to the Rule
                var relevantOps = operationprescriptionsToProcessList
                    .Where(o => o.RuleResultId == ruleToProcess)
                    .ToList();

                // Process the acceptable
                foreach (var relevantOp in relevantOps)
                {
                    var destEntsToProcess = relevantOp.Operands
                        .Select(ro => new {ro.EntityId, ro.EntType})
                        .Intersect(acceptableDestinations)
                        .ToList();

                    var jTempl = relevantOp.OperationTemplate;
                    var jCode = jTempl;
                    var isSubstOk = true;
                    foreach (Match match in regexToken.Matches(jTempl))
                    {
                        var token = match.Groups["Token"].Value;
                        var indexOk = int.TryParse(match.Groups["Index"].Value, out var index);

                        if (!indexOk)
                        {
                            break;
                        }

                        foreach (var destEnt in destEntsToProcess)
                        {
                            var sourceValueIds = relevantOp.Operands
                                .First(o => o.EntityId == destEnt.EntityId && o.EntType == destEnt.EntType)
                                .SourceValueIds.ToArray();

                            if (sourceValueIds.Length < index)
                            {
                                isSubstOk = false;
                                break;
                            }

                            // TODO: this is wrong, getting all the source values, but replacing only one token & index
                            var sourceVals = acceptableSources.Where(s => sourceValueIds.Contains(s.EntityId)).ToArray();
                            jCode = jCode.Replace(token, sourceVals[index].Detail.ToString());
                        }

                        if (isSubstOk)
                        {
                            var result = e.Execute(jCode).GetCompletionValue().ToObject();
                            Console.WriteLine(result);
                        }
                    }
                }

                newState.Values.RemoveWhere(v => v.ValueId == ruleToProcess);
            }

            return newState;
        }

        private static ProcessingRulEngStore OperationMxProcessing(this ProcessingRulEngStore newState, OperationMxProcessing prescription)
        {
            var actionDate = DateTime.UtcNow;

            foreach(var entity in prescription.Entities)
            {
                switch (entity.EntType)
                {
                    case EntityType.Rule:
                        var rule = newState.GetEntityFromValue<Rule>(entity);

                        newState.Rules.Remove(rule);
                        newState.Rules.Add(rule);
                        break;
                    case EntityType.Operation:
                        var operation = newState.GetEntityFromValue<Operation>(entity);

                        newState.Operations.Remove(operation);
                        newState.Operations.Add(operation);
                        break;
                    case EntityType.Request:
                        var request = newState.GetEntityFromValue<Request>(entity);

                        newState.Requests.Remove(request);
                        newState.Requests.Add(request);
                        break;
                    case EntityType.Value:
                        var value = newState.GetEntityFromValue<Value>(entity);

                        newState.Values.Remove(value);
                        newState.Values.Add(value);
                        break;
                }
            }

            return newState;
        }

        private static ProcessingRulEngStore AddOperationAction(this ProcessingRulEngStore newState, OperationMxProcessing prescription)
        {
            var actionDate = DateTime.UtcNow;

            var sumOfValues = newState.Values
                .Where(v => prescription.Entities.SelectMany(r => r.SourceValueIds).Contains(v.ValueId) && v.Detail.Type == JTokenType.Integer)
                .Select(v => v.Detail.ToObject<int>())
                .Sum();

            if (prescription.Entities.First().EntType != EntityType.Value)
            {
                return newState;
            }

            foreach (var value in prescription.Entities)
            {
                var newValue = new Value
                {
                    ValueId = value.EntityId,
                    LastChanged = actionDate,
                    Detail = sumOfValues
                };

                //var addValue = new AddValueAction()
                //{
                //    NewValue = newValue
                //};
                //rvStore.Dispatch(addValue);

                newState.Values.Remove(newValue);
                newState.Values.Add(newValue);
            }

            return newState;
        }

        public static T GetEntityFromValue<T>(this ProcessingRulEngStore newState, OperandKey entity) where T : IEntity
        {
            var newEntity = default(T);
            var refValueId = entity.SourceValueIds.FirstOrDefault();
            if (refValueId != null)
            {
                newEntity = ((JProperty)newState.Values.FirstOrDefault(v => v.ValueId == refValueId).Detail).ToObject<T>();
            }

            newEntity.EntityId = entity.EntityId;

            return newEntity;
        }
    }
}
