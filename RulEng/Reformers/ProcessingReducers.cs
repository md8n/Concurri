﻿using System;
using System.Linq;
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

            // First identify rules for values that don't (yet) exist
            newState = newState.AllOperations(prescription);

            return newState.DeepClone();
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
