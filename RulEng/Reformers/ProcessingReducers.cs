using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Jint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RulEng.Helpers;
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
            // First identify RuleResults that have just been processed successfully
            var ruleResultIds = newState.RuleResults
                .Where(v => v.Detail)
                .Select(v => v.RuleResultId)
                .ToList();
            var operationprescriptionsToProcessList = newState.Operations
                .Where(a => ruleResultIds.Contains(a.RuleResultId))
                .ToList();
            var requestprescriptionsToProcessList = newState.Requests
                .Where(a => ruleResultIds.Contains(a.RuleResultId))
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
            //var conflictDestinations = groupedDestinations
            //    .Where(grp => grp.Count > 1)
            //    .Select(grp => new TypeKey { EntityId = grp.Key.EntityId, EntType = grp.Key.EntType })
            //    .ToList();
            var acceptableDestinations = groupedDestinations
                .Where(grp => grp.Count == 1)
                .Select(grp => new { grp.Key.EntityId, grp.Key.EntType })
                .ToList();

            // Get all of the sources from the previous state
            var acceptableSourceIds = new List<Guid>();
            foreach (var opPresProc in operationprescriptionsToProcessList)
            {
                var opPresOperands = opPresProc.Operands;

                var matchFound = false;
                foreach (var opo in opPresOperands)
                {
                    if (!acceptableDestinations
                        .Any(ad => ad.EntType == opo.EntType && ad.EntityId == opo.EntityId))
                    {
                        continue;
                    }

                    matchFound = true;
                    break;
                }

                if (!matchFound)
                {
                    continue;
                }

                acceptableSourceIds.AddRange(opPresOperands.SelectMany(oo => oo.SourceValueIds));
            }

            var acceptableSources = previousState.Values
                .Where(v => acceptableSourceIds.Contains(v.EntityId))
                .ToList();

            var e = new Engine();

            var regexToken = new Regex(@".*?(?<Token>\$\{(?<Index>\d+)\}).*?");
            foreach (var ruleResultIdToProcess in ruleResultIds)
            {
                // Get all of the operations relevant to the Rule
                var relevantOps = operationprescriptionsToProcessList
                    .Where(o => o.RuleResultId == ruleResultIdToProcess)
                    .ToList();

                if (!relevantOps.Any())
                {
                    // TODO: confirm if we should be doing this if there was nothing relevant to process
                    //newState.RuleResults.RemoveWhere(r => r.RuleResultId == ruleResultIdToProcess);
                    continue;
                }

                // Process the acceptable
                foreach (var relevantOp in relevantOps.Where(o => acceptableDestinations.Contains(new { o.Operands[0].EntityId, o.Operands[0].EntType })))
                {
                    var destEntsToProcess = relevantOp.Operands
                        .Select(de => new
                        {
                            de.EntityId,
                            EntType = Convert.ToInt32(de.EntType),
                            sourceValues = de.SourceValueIds
                                .Select(sv => JObject.Parse($"{{\"Id\":\"{sv}\",\"Value\":{acceptableSources.FirstOrDefault(a => a.EntityId == sv)?.Detail.ToString(Formatting.None)}}}"))
                                .ToArray()
                        })
                        .ToList();

                    var jTempl = relevantOp.OperationTemplate;
                    var jCode = jTempl;
                    foreach (var destEnt in destEntsToProcess)
                    {
                        var sourceVals = destEnt.sourceValues;
                        var isSubstOk = true;

                        foreach (Match match in regexToken.Matches(jTempl))
                        {
                            var token = match.Groups["Token"].Value;
                            var indexOk = int.TryParse(match.Groups["Index"].Value, out var index);

                            if (!indexOk)
                            {
                                break;
                            }

                            if (sourceVals.Length < index)
                            {
                                isSubstOk = false;
                                break;
                            }

                            jCode = jCode.Replace(token, sourceVals[index]["Value"].ToString(Formatting.None));
                        }

                        if (!isSubstOk)
                        {
                            Console.WriteLine(jCode);
                            continue;
                        }

                        var result = JObject.FromObject(e.Execute(jCode).GetCompletionValue().ToObject());
                        //Console.WriteLine(result);
                        switch ((EntityType)destEnt.EntType)
                        {
                            case EntityType.Rule:
                                // Create/Update a rule using destEnt.EntityId and result
                                newState.FromOperationResultAddUpdateRule(result, destEnt.EntityId);
                                break;
                            case EntityType.Operation:
                                // Create/Update an Operation using destEnt.EntityId and result
                                newState.FromOperationResultAddUpdateOperation(result, destEnt.EntityId);
                                break;
                            case EntityType.Request:
                                // Create/Update a Request using destEnt.EntityId and result
                                newState.FromOperationResultAddUpdateRequest(result, destEnt.EntityId);
                                break;
                            case EntityType.Value:
                                // Create/Update a Value using destEnt.EntityId and result
                                newState.FromOperationResultAddUpdateValue(result, destEnt.EntityId);
                                break;
                        }
                    }
                }

                // newState.RuleResults.RemoveWhere(r => r.RuleResultId == ruleResultIdToProcess);
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
