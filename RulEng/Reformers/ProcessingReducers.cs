using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using RulEng.Prescriptions;
using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Reducers
{
    public static class ProcessingReducers
    {
        public static RulEngStore ProcessAllRulesReducer(RulEngStore previousState, IRuleProcessing prescription)
        {
            // Set up a temporary 'Processing' copy of the Store as our Unit of Work
            var newState = previousState.DeepClone();

            // First identify rules for values that don't (yet) exist
            newState = newState.AllNotExists(prescription as ProcessExistsRule);

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

        private static ProcessingRulEngStore AllNotExists(this ProcessingRulEngStore newState, ProcessExistsRule prescription)
        {
            // First identify rules for entities that don't (yet) exist
            var entities = newState.Rules.Select(r => (TypeKey)r).ToList();
            entities.AddRange(newState.Values.Select(v => (TypeKey)v));
            entities.AddRange(newState.Operations.Select(o => (TypeKey)o));
            entities.AddRange(newState.Requests.Select(rq => (TypeKey)rq));
            var rulesToProcessList = newState.Rules
                .Where(r => r.RuleType == RuleType.Exists && r.NegateResult && entities.Except(r.ReferenceValues).Any() && r.LastExecuted < DateTime.UtcNow);

            var actionDate = DateTime.UtcNow;

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

        private static ProcessingRulEngStore AllOperations(this ProcessingRulEngStore newState, IOpReqProcessing prescription)
        {
            // First identify rules that have just been processed successfully
            var ruleIds = newState.RuleResults
                .Where(v => v.Detail)
                .Select(v => v.RuleId)
                .ToList();
            var operationprescriptionsToProcessList = newState.Operations
                .Where(a => ruleIds.Contains(a.RuleId))
                .ToList();
            var requestprescriptionsToProcessList = newState.Requests
                .Where(a => ruleIds.Contains(a.RuleId))
                .ToList();

            foreach (var ruleToProcess in ruleIds)
            {
                var relevantOps = operationprescriptionsToProcessList
                    .Where(o => o.RuleId == ruleToProcess)
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
            T newEntity = default(T);
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
