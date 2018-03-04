using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GraphQL.Types;
using Newtonsoft.Json.Linq;
using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class OperationHelpers
    {
        public static Operation CreateOperation(this RuleResult ruleResult, IEnumerable<OperandKey> operands)
        {
            return new Operation
            {
                OperationId = Guid.NewGuid(),
                RuleResultId = ruleResult.RuleResultId,
                Operands = ImmutableArray.Create(operands.ToArray()),
                OperationType = OperationType.CreateUpdate
            };
        }

        public static void FromOperationResultAddUpdateRule(this ProcessingRulEngStore newState, JObject result, Guid destEntId)
        {
            // Create/Update a rule using destEnt.EntityId and result
            var ruleType = result["RuleType"];
            var negateResult = result["NegateResult"];
            var referenceValues = result["ReferenceValues"];
            var rlType = ruleType?.ToObject<RuleType>() ?? RuleType.Unknown;
            var refValArray = referenceValues?.ToObject<IRulePrescription>();
            if (refValArray == null)
            {
                rlType = RuleType.Error;
            }

            var rule = newState.Rules.FirstOrDefault(r => r.EntityId == destEntId);
            if (rule != null)
            {
                if (negateResult != null)
                {
                    rule.NegateResult = (bool)negateResult;
                }
                if (ruleType != null)
                {
                    rule.RuleType = rlType;
                }
                if (referenceValues != null)
                {
                    rule.ReferenceValues = refValArray;
                }
                rule.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            }
            else
            {
                rule = new Rule
                {
                    EntityId = destEntId,
                    NegateResult = negateResult != null && (bool)negateResult,
                    RuleType = rlType,
                    ReferenceValues = refValArray,
                    LastChanged = DateTime.UtcNow
                };

                newState.Rules.Add(rule);
            }
        }

        public static void FromOperationResultAddUpdateOperation(this ProcessingRulEngStore newState, JObject result,
            Guid destEntId)
        {
            // Create/Update an Operation using destEnt.EntityId and result
            var operationType = result["OperationType"];
            var ruleResultId = result["RuleResultId"];
            var operationTemplate = result["OperationTemplate"];
            var operands = result["Operands"];
            var opType = operationType?.ToObject<OperationType>() ?? OperationType.Unknown;
            var rlResId = ruleResultId?.ToObject<Guid>() ?? Guid.Empty;
            var opTempl = operationTemplate == null ? string.Empty : operationTemplate.ToString().Trim();
            var oprndArray = operands == null
                ? ImmutableArray<OperandKey>.Empty
                : ImmutableArray.Create(operands.ToObject<OperandKey[]>());
            if (ruleResultId == null || string.IsNullOrWhiteSpace(opTempl) || operands == null)
            {
                opType = OperationType.Error;
            }

            var operation = newState.Operations.FirstOrDefault(o => o.EntityId == destEntId);
            if (operation != null)
            {
                if (operationType != null)
                {
                    operation.OperationType = opType;
                }
                if (ruleResultId != null)
                {
                    operation.RuleResultId = rlResId;
                }
                if (operationTemplate != null)
                {
                    operation.OperationTemplate = opTempl;
                }
                if (operands != null)
                {
                    operation.Operands = oprndArray;
                }
                operation.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            }
            else
            {
                operation = new Operation
                {
                    EntityId = destEntId,
                    OperationType = opType,
                    RuleResultId = rlResId,
                    OperationTemplate = operationTemplate == null ? string.Empty : operationTemplate.ToString(),
                    Operands = oprndArray,
                    LastChanged = DateTime.UtcNow
                };

                newState.Operations.Add(operation);
            }
        }

        public static void FromOperationResultAddUpdateRequest(this ProcessingRulEngStore newState, JObject result,
            Guid destEntId)
        {
            // Create/Update a Request using destEnt.EntityId and result
            var valueType = result["ValueType"];
            var ruleResultId = result["RuleResultId"];
            var query = result["Query"];
            var vlType = valueType?.ToObject<JTokenType>() ?? JTokenType.None;
            var rlResId = ruleResultId?.ToObject<Guid>() ?? Guid.Empty;
            var qry = query?.ToObject<IObjectGraphType>();
            if (ruleResultId == null || qry == null)
            {
                vlType = JTokenType.Undefined;
            }

            var request = newState.Requests.FirstOrDefault(o => o.EntityId == destEntId);
            if (request != null)
            {
                if (valueType != null)
                {
                    request.ValueType = vlType;
                }
                if (ruleResultId != null)
                {
                    request.RuleResultId = rlResId;
                }
                if (query != null)
                {
                    request.Query = qry;
                }
                request.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            }
            else
            {
                request = new Request
                {
                    EntityId = destEntId,
                    ValueType = vlType,
                    Query = qry,
                    RuleResultId = rlResId,
                    LastChanged = DateTime.UtcNow
                };

                newState.Requests.Add(request);
            }
        }

        public static void FromOperationResultAddUpdateValue(this ProcessingRulEngStore newState, JObject result,
            Guid destEntId)
        {
            // Create/Update a Value using destEnt.EntityId and result
            var detail = result;

            var value = newState.Values.FirstOrDefault(o => o.EntityId == destEntId);
            if (value != null)
            {
                if (detail != null)
                {
                    value.Detail = detail;
                }
                value.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            }
            else
            {
                value = new Value
                {
                    EntityId = destEntId,
                    Detail = detail,
                    LastChanged = DateTime.UtcNow
                };

                newState.Values.Add(value);
            }
        }
    }
}
