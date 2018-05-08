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

            var opType = prescription is OperationMxProcessing
                ? OperationType.CreateUpdate
                : prescription is OperationDxProcessing
                    ? OperationType.Delete
                    : prescription is OperationSxProcessing 
                        ? OperationType.Search 
                        : OperationType.Unknown; // When the prescription is for requests 

            // Find all relevant operations/requests for the identified rule results and then filter down by execution date
            var possibleOps = newState.Operations
                .Where(a => ruleResultIds.Contains(a.RuleResultId) && a.OperationType == opType).ToList();
            var operationprescriptionsToProcessList = (
                from op in possibleOps
                let rr = newState.RuleResults.First(r => r.RuleResultId == op.RuleResultId)
                where rr.LastChanged > op.LastExecuted
                select op)
                .ToList();
            var requestprescriptionsToProcessList = (
                from rq in newState.Requests.Where(a => ruleResultIds.Contains(a.RuleResultId))
                let rr = newState.RuleResults.First(r => r.RuleResultId == rq.RuleResultId)
                where rr.LastChanged > rq.LastExecuted
                select rq)
                .ToList();

            // Restrict the Operation/Request Prescriptions to process to those that are guaranteed not to fail
            // Select those for which there is no conflict 
            var destinationEntities = new List<TypeKey>();
            foreach (var opPre in operationprescriptionsToProcessList)
            {
                destinationEntities.AddRange(opPre.Operands.Select(o => (TypeKey)o));
            }
            foreach (var rqPre in requestprescriptionsToProcessList)
            {
                destinationEntities.Add(rqPre);
            }

            var groupedDestinations = destinationEntities
                .GroupBy(de => new EntMatch { EntityId = de.EntityId, EntType = de.EntType })
                .Select(grp => new { grp.Key, Count = grp.Count() })
                .ToList();
            //var conflictDestinations = groupedDestinations
            //    .Where(grp => grp.Count > 1)
            //    .Select(grp => new TypeKey { EntityId = grp.Key.EntityId, EntTags = grp.Key.EntTags, EntType = grp.Key.EntType })
            //    .ToList();
            var acceptableDestinations = groupedDestinations
                .Where(grp => grp.Count == 1)
                .Select(grp => grp.Key)
                .ToList();

            if (prescription is OperationMxProcessing)
            {
                newState = OperationMxProcessing(newState, previousState, ruleResultIds, operationprescriptionsToProcessList, acceptableDestinations);
            }
            if (prescription is OperationDxProcessing)
            {
                newState = OperationDxProcessing(newState, ruleResultIds, operationprescriptionsToProcessList, acceptableDestinations);
            }
            if (prescription is OperationSxProcessing)
            {
                newState = OperationSxProcessing(newState, previousState, ruleResultIds, operationprescriptionsToProcessList, acceptableDestinations);
            }

            return newState;
        }

        private static ProcessingRulEngStore OperationMxProcessing(this ProcessingRulEngStore newState, RulEngStore previousState,
            List<Guid> ruleResultIds, List<Operation> operationprescriptionsToProcessList,
            List<EntMatch> acceptableDestinations)
        {
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

                acceptableSourceIds.AddRange(opPresOperands.SelectMany(oo => oo.SourceEntityIds));
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
                foreach (var relevantOp in relevantOps)
                {
                    // Ensure the first entity in the operation is an acceptable destination
                    EntMatch firstEnt = relevantOp.Operands[0];

                    if (!acceptableDestinations.Any(ad =>
                        ad.EntType == firstEnt.EntType && ad.EntityId == firstEnt.EntityId))
                    {
                        continue;
                    }

                    var destEntsToProcess = relevantOp.Operands
                        .Select(de => new
                        {
                            de.EntityId,
                            EntType = Convert.ToInt32(de.EntType),
                            sourceValues = de.SourceEntityIds
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

                        JToken result = null;
                        if (jCode.StartsWith("{"))
                        {
                            result = JObject.FromObject(e.Execute(jCode).GetCompletionValue().ToObject());
                        }
                        if (jCode.StartsWith("["))
                        {
                            result = JArray.FromObject(e.Execute(jCode).GetCompletionValue().ToObject());
                        }
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

                        // Mark the operation as Executed
                        var actionDate = DateTime.UtcNow;

                        // Mark this Rule as executed
                        relevantOp.LastExecuted = actionDate;
                    }
                }

                // newState.RuleResults.RemoveWhere(r => r.RuleResultId == ruleResultIdToProcess);
            }

            return newState;
        }

        private static ProcessingRulEngStore OperationDxProcessing(this ProcessingRulEngStore newState,
            List<Guid> ruleResultIds, List<Operation> operationprescriptionsToProcessList,
            List<EntMatch> acceptableDestinations)
        {
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
                foreach (var relevantOp in relevantOps)
                {
                    var firstEnt = new EntMatch
                    {
                        EntityId = relevantOp.Operands[0].EntityId,
                        EntType = relevantOp.Operands[0].EntType
                    };

                    if (!acceptableDestinations.Any(ad =>
                        ad.EntType == firstEnt.EntType && ad.EntityId == firstEnt.EntityId))
                    {
                        continue;
                    }

                    //Console.WriteLine(result);
                    switch (firstEnt.EntType)
                    {
                        case EntityType.Rule:
                            var removeRl = newState.Rules.FirstOrDefault(r => r.RuleId == firstEnt.EntityId);
                            var removeRr = newState.RuleResults.FirstOrDefault(r => r.RuleId == firstEnt.EntityId);
                            var rrId = removeRr.RuleResultId;
                            newState.Rules.Remove(removeRl);
                            newState.RuleResults.Remove(removeRr);

                            // The directly dependent Operations and Requests are now orphaned
                            // They should be deleted, they will never execute again
                            var removeOp = newState.Operations.Where(o => o.RuleResultId == rrId);
                            var removeRq = newState.Requests.Where(r => r.RuleResultId == rrId);
                            foreach (var op in removeOp)
                            {
                                newState.Operations.Remove(op);
                            }
                            foreach (var rq in removeRq)
                            {
                                newState.Requests.Remove(rq);
                            }
                            break;
                        case EntityType.Operation:
                            newState.Operations.RemoveWhere(r => r.OperationId == firstEnt.EntityId);
                            break;
                        case EntityType.Request:
                            newState.Requests.RemoveWhere(r => r.RequestId == firstEnt.EntityId);
                            break;
                        case EntityType.Value:
                            var removeVal = newState.Values.FirstOrDefault(v => v.ValueId == firstEnt.EntityId);
                            newState.Values.Remove(removeVal);
                            break;
                    }

                    // Mark the operation as Executed
                    var actionDate = DateTime.UtcNow;

                    // Mark this Rule as executed
                    relevantOp.LastExecuted = actionDate;
                }

                // newState.RuleResults.RemoveWhere(r => r.RuleResultId == ruleResultIdToProcess);
            }

            return newState;
        }


        private static ProcessingRulEngStore OperationSxProcessing(this ProcessingRulEngStore newState, RulEngStore previousState,
            List<Guid> ruleResultIds, List<Operation> operationprescriptionsToProcessList,
            List<EntMatch> acceptableDestinations)
        {
            // Get all of the sources from the previous state
            var acceptableSourceIds = new List<Guid>();
            //foreach (var opPresProc in operationprescriptionsToProcessList)
            //{
            //    var opPresOperands = opPresProc.Operands;

            //    var matchFound = false;
            //    foreach (var opo in opPresOperands)
            //    {
            //        if (!acceptableDestinations
            //            .Any(ad => ad.EntType == opo.EntType && (ad.EntityId == opo.EntityId || opo.EntityId == Guid.Empty)))
            //        {
            //            continue;
            //        }

            //        matchFound = true;
            //        break;
            //    }

            //    if (!matchFound)
            //    {
            //        continue;
            //    }

            //    acceptableSourceIds.AddRange(opPresOperands.SelectMany(oo => oo.SourceValueIds));
            //}

            //var acceptableSources = previousState.Values
            //    .Where(v => acceptableSourceIds.Contains(v.EntityId))
            //    .ToList();

            var e = new Engine();

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
                foreach (var relevantOp in relevantOps)
                {
                    // Note: A Search operation does not specify an output destination as it does not 'know' in advance how many results will be found
                    // However, it may specify a reduced set of Ids to search through.
                    // These will be provided in the SourceValueIds field of each relevantOp.Operand

                    //        var firstEnt = new EntMatch
                    //        {
                    //            EntityId = relevantOp.Operands[0].EntityId,
                    //            EntType = relevantOp.Operands[0].EntType
                    //        };

                    //        if (!acceptableDestinations.Any(ad =>
                    //            ad.EntType == firstEnt.EntType && ad.EntityId == firstEnt.EntityId))
                    //        {
                    //            continue;
                    //        }

                    //        var destEntsToProcess = relevantOp.Operands
                    //            .Select(de => new
                    //            {
                    //                de.EntityId,
                    //                EntType = Convert.ToInt32(de.EntType),
                    //                sourceValues = de.SourceValueIds
                    //                    .Select(sv => JObject.Parse($"{{\"Id\":\"{sv}\",\"Value\":{acceptableSources.FirstOrDefault(a => a.EntityId == sv)?.Detail.ToString(Formatting.None)}}}"))
                    //                    .ToArray()
                    //            })
                    //            .ToList();

                    switch (relevantOp.Operands[0].SourceEntType)
                    {
                        case EntityType.Rule:
                            e.SetValue("source", JsonConvert.SerializeObject(previousState.Rules.ToArray()));
                            break;
                        case EntityType.RuleResult:
                            e.SetValue("source", JsonConvert.SerializeObject(previousState.RuleResults.ToArray()));
                            break;
                        case EntityType.Operation:
                            e.SetValue("source", JsonConvert.SerializeObject(previousState.Operations.ToArray()));
                            break;
                        case EntityType.Request:
                            e.SetValue("source", JsonConvert.SerializeObject(previousState.Requests.ToArray()));
                            break;
                        case EntityType.Value:
                            e.SetValue("source", JsonConvert.SerializeObject(previousState.Values.ToArray()));
                            break;
                    }

                    // The result must be a list of guids for the entities that met the search criteria
                    var result = e
                        .Execute(relevantOp.OperationTemplate)
                        .GetCompletionValue();

                    var sourceGuids = result.ToString()
                        .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(Guid.Parse)
                        .ToList();

                    for (var ix = 0; ix < sourceGuids.Count; ix++)
                    {
                        var sourceEnt = new TypeKey
                        {
                            EntityId = sourceGuids[ix],
                            EntType = relevantOp.Operands[0].SourceEntType,
                            EntTags = relevantOp.EntTags
                        } as IEntity;
                        switch (relevantOp.Operands[0].EntType)
                        {
                            case EntityType.Rule:
                                // Create/Update a rule using destEnt.EntityId and result
                                var rl = newState.FromSearchOperationAddUpdateExistsRule(sourceEnt, relevantOp.EntTags, Guid.NewGuid());
                                var rr = new RuleResult(rl);
                                newState.RuleResults.Add(rr);
                                break;
                            case EntityType.Operation:
                                // Create/Update an Operation using destEnt.EntityId and result
                                var op = newState.FromSearchOperationAddUpdateOperation(sourceEnt, relevantOp.EntTags, OperationType.Delete, "", Guid.NewGuid());
                                //var rr = new RuleResult(rl);
                                //newState.RuleResults.Add(rr);

                                //newState.FromOperationResultAddUpdateOperation(result, destEnt.EntityId);
                                break;
                                //case EntityType.Request:
                                //    // Create/Update a Request using destEnt.EntityId and result
                                //    newState.FromOperationResultAddUpdateRequest(result, destEnt.EntityId);
                                //    break;
                                //case EntityType.Value:
                                //    // Create/Update a Value using destEnt.EntityId and result
                                //    newState.FromOperationResultAddUpdateValue(result, destEnt.EntityId);
                                //    break;
                                //            }
                        }

                        //var values = new List<string>();
                        //for (var i = 0; i < a.GetLength(); i++)
                        //{
                        //    values.Add(a.Get(i.ToString()).AsString());
                        //}
                        //return values;

                        Console.WriteLine(JsonConvert.SerializeObject(result));



                        //        foreach (var destEnt in destEntsToProcess)
                        //        {
                        //            var sourceVals = destEnt.sourceValues;
                        //            var isSubstOk = true;

                        //            foreach (Match match in regexToken.Matches(jTempl))
                        //            {
                        //                var token = match.Groups["Token"].Value;
                        //                var indexOk = int.TryParse(match.Groups["Index"].Value, out var index);

                        //                if (!indexOk)
                        //                {
                        //                    break;
                        //                }

                        //                if (sourceVals.Length < index)
                        //                {
                        //                    isSubstOk = false;
                        //                    break;
                        //                }

                        //                jCode = jCode.Replace(token, sourceVals[index]["Value"].ToString(Formatting.None));
                        //            }

                        //            if (!isSubstOk)
                        //            {
                        //                Console.WriteLine(jCode);
                        //                continue;
                        //            }

                        //            JToken result = null;
                        //            if (jCode.StartsWith("{"))
                        //            {
                        //                result = JObject.FromObject(e.Execute(jCode).GetCompletionValue().ToObject());
                        //            }
                        //            if (jCode.StartsWith("["))
                        //            {
                        //                result = JArray.FromObject(e.Execute(jCode).GetCompletionValue().ToObject());
                        //            }
                        //            //Console.WriteLine(result);
                        //            switch ((EntityType)destEnt.EntType)
                        //            {
                        //                case EntityType.Rule:
                        //                    // Create/Update a rule using destEnt.EntityId and result
                        //                    newState.FromOperationResultAddUpdateRule(result, destEnt.EntityId);
                        //                    break;
                        //                case EntityType.Operation:
                        //                    // Create/Update an Operation using destEnt.EntityId and result
                        //                    newState.FromOperationResultAddUpdateOperation(result, destEnt.EntityId);
                        //                    break;
                        //                case EntityType.Request:
                        //                    // Create/Update a Request using destEnt.EntityId and result
                        //                    newState.FromOperationResultAddUpdateRequest(result, destEnt.EntityId);
                        //                    break;
                        //                case EntityType.Value:
                        //                    // Create/Update a Value using destEnt.EntityId and result
                        //                    newState.FromOperationResultAddUpdateValue(result, destEnt.EntityId);
                        //                    break;
                        //            }

                        //            // Mark the operation as Executed
                        //            var actionDate = DateTime.UtcNow;

                        //            // Mark this Rule as executed
                        //            relevantOp.LastExecuted = actionDate;
                        //        }
                    }

                    // newState.RuleResults.RemoveWhere(r => r.RuleResultId == ruleResultIdToProcess);
                }
            }

            return newState;
        }

        public static T GetEntityFromValue<T>(this ProcessingRulEngStore newState, OperandKey entity) where T : IEntity
        {
            var newEntity = default(T);
            var refValueId = entity.SourceEntityIds.FirstOrDefault();
            if (refValueId != null)
            {
                newEntity = ((JProperty)newState.Values.FirstOrDefault(v => v.ValueId == refValueId).Detail).ToObject<T>();
            }

            newEntity.EntityId = entity.EntityId;

            return newEntity;
        }
    }
}
