using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            newState = newState.AllLessThan(prescription as ProcessLessThanRule);
            newState = newState.AllEqual(prescription as ProcessEqualRule);
            newState = newState.AllGreaterThan(prescription as ProcessGreaterThanRule);

            newState = newState.AllRegexMatch(prescription as ProcessRegexMatchRule);

            newState = newState.AllAnd(prescription as ProcessAndRule);
            newState = newState.AllOr(prescription as ProcessOrRule);
            newState = newState.AllXor(prescription as ProcessXorRule);

            return newState.DeepClone();
        }

        public static RulEngStore ProcessAllOperationsReducer(RulEngStore previousState, IOpReqProcessing prescription)
        {
            // Set up a temporary 'Processing' copy of the Store as our Unit of Work
            var newState = previousState.DeepClone();

            // First identify rules for values that don't (yet) exist
            newState = newState.AllOperations(prescription);

            return newState.DeepClone();
        }

        /// <summary>
        /// Perform all Exists and Not Exists Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        private static ProcessingRulEngStore AllExists(this ProcessingRulEngStore newState, ProcessExistsRule prescription)
        {
            var actionDate = DateTime.UtcNow;

            // First identify the potentially relevant entities
            var entities = newState.Rules.Select(r => (TypeKey)r).ToList();
            entities.AddRange(newState.Values.Select(v => (TypeKey)v));
            entities.AddRange(newState.Operations.Select(o => (TypeKey)o));
            entities.AddRange(newState.Requests.Select(rq => (TypeKey)rq));

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
        private static ProcessingRulEngStore AllHasMeaningfulValue(this ProcessingRulEngStore newState, ProcessHasMeaningfulValueRule prescription)
        {
            var actionDate = DateTime.UtcNow;

            // First identify the potentially relevant Entities
            var entities = newState.Values.Select(v => (TypeKey)v).ToList();

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

        /// <summary>
        /// Perform all LessThan and Not LessThan Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        private static ProcessingRulEngStore AllLessThan(this ProcessingRulEngStore newState, ProcessLessThanRule prescription)
        {
            var actionDate = DateTime.UtcNow;

            // First get the potentially relevant entities in a cleaned form
            var ruleResultEntitySets = prescription.Entities
                .Where(vi => vi.EntityIds.Count(ve => ve.EntityType == EntityType.Value) >= vi.MinEntitiesRequired)
                .Select(pe => new {
                    pe.RuleResultId,
                    Entities = new List<Value>(),
                    EntityIds = pe.EntityIds
                        .Where(ve => ve.EntityType == EntityType.Value)
                        .Select(ve => ve.EntityId)
                        .ToList()
                })
                .Distinct()
                .ToList();

            foreach(var ruleResultEntitySet in ruleResultEntitySets)
            {
                ruleResultEntitySet.Entities
                    .AddRange(newState.Values.Where(v => ruleResultEntitySet.EntityIds.Contains(v.EntityId)));
            }

            var entities = ruleResultEntitySets
                .SelectMany(v => v.Entities)
                .Distinct()
                .Select(v => (TypeKey)v)
                .ToList();

            // Get the corresponding Rules
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.LessThan, entities);

            foreach (var presValues in ruleResultEntitySets)
            {
                var presEntities = presValues.Entities.ToArray();

                var ruleToProcess = rulesToProcessList
                    .SingleOrDefault(r => r.ReferenceValues.Any(rv => rv.RuleResultId == presValues.RuleResultId));

                var newRuleResult = ruleToProcess.LessThanTest(presEntities, presValues.RuleResultId, actionDate);

                newState.RuleResults.Add(newRuleResult);
            }

            return newState;
        }

        /// <summary>
        /// Perform all Equals and Not Equals Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        private static ProcessingRulEngStore AllEqual(this ProcessingRulEngStore newState, ProcessEqualRule prescription)
        {
            var actionDate = DateTime.UtcNow;

            // First get the potentially relevant entities in a cleaned form
            var ruleResultEntitySets = prescription.Entities
                .Where(vi => vi.EntityIds.Count(ve => ve.EntityType == EntityType.Value) >= vi.MinEntitiesRequired)
                .Select(pe => new {
                    pe.RuleResultId,
                    Entities = new List<Value>(),
                    EntityIds = pe.EntityIds
                        .Where(ve => ve.EntityType == EntityType.Value)
                        .Select(ve => ve.EntityId)
                })
                .Distinct()
                .ToList();

            foreach (var ruleResultEntitySet in ruleResultEntitySets)
            {
                ruleResultEntitySet.Entities
                    .AddRange(newState.Values.Where(v => ruleResultEntitySet.EntityIds.Contains(v.EntityId)));
            }

            var entities = ruleResultEntitySets
                .SelectMany(v => v.Entities)
                .Distinct()
                .Select(v => (TypeKey)v)
                .ToList();

            // Get the corresponding Rules
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.Equal, entities);

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

                var newRuleResult = ruleToProcess.EqualTest(presEntities, presValues.RuleResultId, actionDate);

                newState.RuleResults.Add(newRuleResult);
            }

            return newState;
        }

        /// <summary>
        /// Perform all Greater Than and Not Greater Than Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        private static ProcessingRulEngStore AllGreaterThan(this ProcessingRulEngStore newState, ProcessGreaterThanRule prescription)
        {
            var actionDate = DateTime.UtcNow;

            // First get the potentially relevant entities in a cleaned form
            var ruleResultEntitySets = prescription.Entities
                .Where(vi => vi.EntityIds.Count(ve => ve.EntityType == EntityType.Value) >= vi.MinEntitiesRequired)
                .Select(pe => new {
                    pe.RuleResultId,
                    Entities = new List<Value>(),
                    EntityIds = pe.EntityIds
                        .Where(ve => ve.EntityType == EntityType.Value)
                        .Select(ve => ve.EntityId)
                })
                .Distinct()
                .ToList();

            foreach (var ruleResultEntitySet in ruleResultEntitySets)
            {
                ruleResultEntitySet.Entities
                    .AddRange(newState.Values.Where(v => ruleResultEntitySet.EntityIds.Contains(v.EntityId)));
            }

            var entities = ruleResultEntitySets
                .SelectMany(v => v.Entities)
                .Distinct()
                .Select(v => (TypeKey)v)
                .ToList();

            // Get the corresponding Rules
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.GreaterThan, entities);

            foreach (var presValues in ruleResultEntitySets)
            {
                var presEntities = presValues.Entities.ToArray();
                var ruleToProcess = rulesToProcessList
                    .SingleOrDefault(r => r.ReferenceValues.Any(rv => rv.RuleResultId == presValues.RuleResultId));

                var newRuleResult = ruleToProcess.LessThanTest(presEntities, presValues.RuleResultId, actionDate);

                newState.RuleResults.Add(newRuleResult);
            }

            return newState;
        }

        /// <summary>
        /// Perform all RegexMatch and Not RegexMatch Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        private static ProcessingRulEngStore AllRegexMatch(this ProcessingRulEngStore newState, ProcessRegexMatchRule prescription)
        {
            var actionDate = DateTime.UtcNow;

            // First get the potentially relevant entities in a cleaned form
            var ruleResultEntitySets = prescription.Entities
                .Where(vi => vi.EntityIds.Count(ve => ve.EntityType == EntityType.Value) >= vi.MinEntitiesRequired)
                .Select(pe => new {
                    pe.RuleResultId,
                    Entities = new List<Value>(),
                    EntityIds = pe.EntityIds
                        .Where(ve => ve.EntityType == EntityType.Value)
                        .Select(ve => ve.EntityId)
                })
                .Distinct()
                .ToList();

            foreach (var ruleResultEntitySet in ruleResultEntitySets)
            {
                ruleResultEntitySet.Entities
                    .AddRange(newState.Values.Where(v => ruleResultEntitySet.EntityIds.Contains(v.EntityId)));
            }

            var entities = ruleResultEntitySets
                .SelectMany(v => v.Entities)
                .Distinct()
                .Select(v => (TypeKey)v)
                .ToList();

            // Get the corresponding Rules
            var rulesToProcessList = newState.Rules.RulesToProcess(RuleType.RegularExpression, entities);

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

                var result = true;

                // All the Details must be of the same type
                var secondDetailType = presEntities[1].Detail.Type;
                if (!presEntities[0].Detail.Type.IsText())
                {
                    result = false;
                }
                else if (presEntities.Skip(2).Any(pe => pe.Detail.Type != secondDetailType))
                {
                    result = false;
                }
                else
                {
                    var regex = new Regex(presEntities[0].Detail.GetText());

                    for (var ix = 1; ix < presEntities.Length; ix++)
                    {
                        // TODO: Change this to test for regex and testable type
                        if (!presEntities[ix].Detail.IsArray())
                        {
                            result = false;
                            break;
                        }

                        var testValue = presEntities[ix].Detail.ToTextValue();
                        if (regex.IsMatch(testValue))
                        {
                            continue;
                        }

                        result = false;
                        break;
                    }
                }

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
        /// Perform all And and Not And Rules
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="prescription"></param>
        /// <returns></returns>
        private static ProcessingRulEngStore AllAnd(this ProcessingRulEngStore newState, ProcessAndRule prescription)
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

                var ents = presEntities.Select((t, ix) => presEntities[ix - 1].Detail).ToList();
                var result = ents.All(e => e == ents[0]);

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
        private static ProcessingRulEngStore AllOr(this ProcessingRulEngStore newState, ProcessOrRule prescription)
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

                var result = presEntities.Select((t, ix) => presEntities[ix - 1].Detail).Any(e => e);

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
        private static ProcessingRulEngStore AllXor(this ProcessingRulEngStore newState, ProcessXorRule prescription)
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

                var ents = presEntities.Select((t, ix) => presEntities[ix - 1].Detail).ToList();
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

        private static ProcessingRulEngStore AllOperations(this ProcessingRulEngStore newState, IOpReqProcessing prescription)
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

            foreach (var ruleToProcess in ruleIds)
            {
                var relevantOps = operationprescriptionsToProcessList
                    .Where(o => o.RuleResultId == ruleToProcess)
                    .ToList();

                // Always do Assign Operation Prescriptions first
                var assignOps = relevantOps.Where(a => a.OperationType == OperationType.CreateUpdate && a is OperationMxAssignProcessing);
                foreach (var prescriptionsToProcess in assignOps)
                {
                    var omap = new OperationMxAssignProcessing
                    {
                        Entities = prescriptionsToProcess.Operands
                    };
                    // reducer goes here - not prescription
                    newState = OperationMxAssignProcessing(newState, omap);
                }

                //foreach (var prescriptionsToProcess in actionsToProcessList.Where(a => a.RuleId == ruleToProcess && a.ValueAction is AddOperationAction))
                //{
                //    var aoa = ((AddOperationAction)prescriptionsToProcess.ValueAction);
                //    aoa.operation = prescriptionsToProcess;
                //    newState = AddOperationAction(newState, aoa);
                //}

                newState.Values.RemoveWhere(v => v.ValueId == ruleToProcess);
            }

            return newState;
        }

        private static ProcessingRulEngStore OperationMxAssignProcessing(this ProcessingRulEngStore newState, OperationMxAssignProcessing prescription)
        {
            var actionDate = DateTime.UtcNow;

            foreach(var entity in prescription.Entities)
            {
                switch (entity.EntityType)
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

        private static ProcessingRulEngStore AddOperationAction(this ProcessingRulEngStore newState, OperationMxAddProcessing prescription)
        {
            var actionDate = DateTime.UtcNow;

            var sumOfValues = newState.Values
                .Where(v => prescription.Entities.SelectMany(r => r.SourceValueIds).Contains(v.ValueId) && v.Detail.Type == JTokenType.Integer)
                .Select(v => v.Detail.ToObject<int>())
                .Sum();

            if (prescription.Entities.First().EntityType != EntityType.Value)
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
