using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

using GraphQL.Types;

using RulEng.ProcessingState;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class OperationHelpers
    {
        /// <summary>
        /// Create a new CreateUpdate Operation based on the supplied RuleResult and OperandKeys
        /// </summary>
        /// <param name="ruleResult"></param>
        /// <param name="operands"></param>
        /// <param name="operationId"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static Operation CreateUpdateOperation(this RuleResult ruleResult, IEnumerable<OperandKey> operands, Guid operationId, string template)
        {
            return ruleResult.RuleResultId.CreateUpdateOperation(operands, operationId, template);
        }

        /// <summary>
        /// Create a new CreateUpdate Operation based on the supplied RuleResultId and OperandKeys
        /// </summary>
        /// <param name="ruleResultId"></param>
        /// <param name="operands"></param>
        /// <param name="operationId"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static Operation CreateUpdateOperation(this Guid ruleResultId, IEnumerable<OperandKey> operands, Guid operationId, string template)
        {
            return new Operation
            {
                OperationId = operationId.NonEmptyUuid(),
                RuleResultId = ruleResultId,
                Operands = ImmutableArray.Create(operands?.ToArray() ?? new OperandKey[0]),
                OperationTemplate = template,
                OperationType = OperationType.CreateUpdate
            };
        }

        /// <summary>
        /// Rebuild a CreateUpdate Operation based on an existing Operation and the optional RuleResult, OperandKeys and Template
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="ruleResult"></param>
        /// <param name="operands"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static Operation RecreateUpdateOperation(this Operation operation, RuleResult ruleResult = null, IEnumerable<OperandKey> operands = null, string template = null)
        {
            if (ruleResult != null) {
                operation.RuleResultId = ruleResult.EntityId;
            }

            operation.Operands = ImmutableArray.Create(operands?.ToArray() ?? new OperandKey[0]);

            if (!string.IsNullOrWhiteSpace(template)) {
                operation.OperationTemplate = template.Trim();
            }

            operation.LastChanged = DateTime.UtcNow;
            operation.OperationType = OperationType.CreateUpdate;

            return operation;
        }

        /// <summary>
        /// Create a new Search Operation based on the supplied RuleResult and OperandKeys
        /// </summary>
        /// <param name="ruleResult"></param>
        /// <param name="operands"></param>
        /// <param name="operationId"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static Operation SearchOperation(this RuleResult ruleResult, IEnumerable<OperandKey> operands, Guid operationId, string template)
        {
            return ruleResult.RuleResultId.SearchOperation(operands, operationId, template);
        }

        /// <summary>
        /// Create a new Search Operation based on the supplied RuleResultId and OperandKeys
        /// </summary>
        /// <param name="ruleResultId"></param>
        /// <param name="operands"></param>
        /// <param name="operationId"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static Operation SearchOperation(this Guid ruleResultId, IEnumerable<OperandKey> operands, Guid operationId, string template)
        {
            var ops = operands?.ToArray() ?? [];
            foreach (var op in ops) {
                if (op.SourceEntityIds.IsDefault) {
                    op.SourceEntityIds = ImmutableArray<Guid>.Empty;
                }
            }

            return new Operation
            {
                OperationId = operationId.NonEmptyUuid(),
                EntTags = ops.SelectMany(o => o.EntTags).Distinct().ToList(),
                RuleResultId = ruleResultId,
                Operands = ImmutableArray.Create(ops),
                OperationTemplate = template,
                OperationType = OperationType.Search
            };
        }

        // TODO: Add the Re Search Operation method

        /// <summary>
        /// Create a new Delete Operation based on the supplied RuleResult and OperandKeys
        /// </summary>
        /// <param name="ruleResult"></param>
        /// <param name="operands"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        public static Operation DeleteOperation(this RuleResult ruleResult, IEnumerable<OperandKey> operands, Guid operationId)
        {
            return new Operation
            {
                OperationId = operationId.NonEmptyUuid(),
                RuleResultId = ruleResult.RuleResultId,
                Operands = ImmutableArray.Create(operands?.ToArray() ?? new OperandKey[0]),
                OperationType = OperationType.Delete
            };
        }

        /// <summary>
        /// Create a new Search Operation based on the supplied RuleResult and OperandKeys
        /// </summary>
        /// <param name="ruleResult"></param>
        /// <param name="operands"></param>
        /// <param name="operationId"></param>
        /// <returns></returns>
        public static Operation SearchOperation(this RuleResult ruleResult, IEnumerable<OperandKey> operands, Guid operationId)
        {
            return new Operation
            {
                OperationId = operationId.NonEmptyUuid(),
                RuleResultId = ruleResult.RuleResultId,
                Operands = ImmutableArray.Create(operands?.ToArray() ?? new OperandKey[0]),
                OperationType = OperationType.Search
            };
        }

        /// <summary>
        /// Create/Update a rule using destEnt.EntityId and result
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="result"></param>
        /// <param name="destEntId"></param>
        public static void FromOperationResultAddUpdateRule(this ProcessingRulEngStore newState, JsonElement result, Guid destEntId)
        {
            RuleType rlType = RuleType.Unknown;
            bool hasRuleType = result.TryGetProperty("RuleType", out JsonElement ruleType);
            if (hasRuleType) {
                if (!Enum.TryParse(ruleType.GetRawText(), out rlType)) {
                    rlType = RuleType.Unknown;
                }
            }

            bool negResult = false;
            bool hasNegateResult = result.TryGetProperty("NegateResult", out JsonElement negateResult);
            if (hasNegateResult) {
                if (!bool.TryParse(ruleType.GetRawText(), out negResult)) {
                    rlType = RuleType.Unknown;
                }
            }

            IRulePrescription refValArray = null;
            bool hasRefValues = result.TryGetProperty("RuleType", out JsonElement referenceValues);
            if (hasRefValues) {
                // TODO: Need to check that the RuleResultId (Guid) and EntityIds (ImmutableList<IEntity>) are deserialized correctly 
                refValArray = JsonSerializer.Deserialize<IRulePrescription>(referenceValues);
            }
            if (!hasRefValues || refValArray == null) {
                rlType = RuleType.Error;
            }

            var rule = newState.Rules.FirstOrDefault(r => r.EntityId == destEntId);
            if (rule != null) {
                if (hasRuleType) {
                    rule.RuleType = rlType;
                }
                if (hasNegateResult) {
                    rule.NegateResult = negResult;
                }
                if (hasRefValues) {
                    rule.ReferenceValues = refValArray;
                }
                rule.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            } else {
                rule = new Rule
                {
                    EntityId = destEntId,
                    NegateResult = hasNegateResult && negResult,
                    RuleType = rlType,
                    ReferenceValues = refValArray,
                    LastChanged = DateTime.UtcNow
                };

                newState.Rules.Add(rule);
            }
        }


        public static Rule FromSearchOperationAddUpdateExistsRule(this ProcessingRulEngStore newState, IEntity sourceEnt, List<string> entTags, Guid destEntId)
        {
            // Create/Update a rule using destEnt.EntityId and result
            const RuleType rlType = RuleType.Exists;
            entTags = (entTags == null || entTags.Count == 0) ? sourceEnt.EntTags : entTags;
            var refValArray = sourceEnt.RulePrescription<RuleUnary>();
            var ruleName = $"Test for existence of {sourceEnt.EntType} {(TypeKey)sourceEnt}";

            var rule = newState.Rules.FirstOrDefault(r => r.EntityId == destEntId);
            if (rule != null) {
                rule.RuleName = ruleName;
                rule.NegateResult = false;
                rule.RuleType = rlType;
                rule.EntTags = entTags;
                rule.ReferenceValues = refValArray;
                rule.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            } else {
                rule = new Rule
                {
                    EntityId = destEntId,
                    RuleName = ruleName,
                    NegateResult = false,
                    RuleType = rlType,
                    EntTags = entTags,
                    ReferenceValues = refValArray,
                    LastChanged = DateTime.UtcNow
                };

                newState.Rules.Add(rule);
            }

            return rule;
        }

        /// <summary>
        /// Create/Update an Operation using destEnt.EntityId and result
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="result"></param>
        /// <param name="destEntId"></param>
        public static void FromOperationResultAddUpdateOperation(this ProcessingRulEngStore newState, JsonElement result,
            Guid destEntId)
        {
            OperationType opType = OperationType.Unknown;
            bool hasOperationType = result.TryGetProperty("OperationType", out JsonElement operationType);
            if (hasOperationType) {
                if (!Enum.TryParse(operationType.GetRawText(), out opType)) {
                    opType = OperationType.Unknown;
                }
            }

            (bool hasRuleResultId, JsonElement? ruleResultId, Guid rlResId) = result.GetRuleResultId();

            bool hasOperationTemplate = result.TryGetProperty("OperationTemplate", out JsonElement operationTemplate);
            string opTempl = hasOperationTemplate ? operationTemplate.GetRawText().Trim() : string.Empty;

            ImmutableArray<OperandKey> oprndArray = ImmutableArray<OperandKey>.Empty;
            bool hasOperands = result.TryGetProperty("Operands", out JsonElement operands);
            if (hasOperands) {
                // TODO: Need to check that all of the OperandKey values are deserialized correctly 
                oprndArray = JsonSerializer.Deserialize<ImmutableArray<OperandKey>>(operands);
            }

            if (!hasRuleResultId || string.IsNullOrWhiteSpace(opTempl) || !hasOperands || oprndArray == null) {
                opType = OperationType.Error;
            }

            var operation = newState.Operations.FirstOrDefault(o => o.EntityId == destEntId);
            if (operation != null) {
                if (hasOperationType) {
                    operation.OperationType = opType;
                }
                if (hasRuleResultId) {
                    operation.RuleResultId = rlResId;
                }
                if (hasOperationTemplate) {
                    operation.OperationTemplate = opTempl;
                }
                if (hasOperands) {
                    operation.Operands = oprndArray;
                }
                operation.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            } else {
                operation = new Operation
                {
                    EntityId = destEntId,
                    OperationType = opType,
                    RuleResultId = rlResId,
                    OperationTemplate = opTempl,
                    Operands = oprndArray,
                    LastChanged = DateTime.UtcNow
                };

                newState.Operations.Add(operation);
            }
        }

        public static Operation FromSearchOperationAddUpdateOperation(this ProcessingRulEngStore newState, IEntity sourceEnt, List<string> entTags, OperationType opType, string operationTemplate, Guid destEntId)
        {
            // Create/Update an operation using destEnt.EntityId and result
            var ruleResultId = sourceEnt.EntityId;
            var opTempl = string.IsNullOrWhiteSpace(operationTemplate) ? string.Empty : operationTemplate.Trim();

            entTags = (entTags == null || entTags.Count == 0) ? sourceEnt.EntTags : entTags;
            var refValArray = sourceEnt.RulePrescription<RuleUnary>();

            var operation = newState.Operations.FirstOrDefault(r => r.EntityId == destEntId);
            if (operation != null) {
                operation.OperationId = destEntId;
                operation.OperationType = opType;
                operation.RuleResultId = ruleResultId;
                operation.OperationTemplate = opTempl;
                operation.EntTags = entTags;
                //operation.ReferenceValues = refValArray;
                operation.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            } else {
                operation = new Operation
                {
                    OperationId = destEntId,
                    OperationType = opType,
                    RuleResultId = ruleResultId,
                    OperationTemplate = opTempl,
                    EntTags = entTags,
                    // Operands = oprndArray,
                    LastChanged = DateTime.UtcNow
                };

                newState.Operations.Add(operation);
            }

            return operation;
        }

        /// <summary>
        /// Create/Update a Request using destEnt.EntityId and result
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="result"></param>
        /// <param name="destEntId"></param>
        public static void FromOperationResultAddUpdateRequest(this ProcessingRulEngStore newState, JsonElement result,
            Guid destEntId)
        {
            JsonValueKind vlKind = JsonValueKind.Undefined;
            bool hasValueKind = result.TryGetProperty("ValueKind", out JsonElement valueKind);
            if (hasValueKind) {
                if (!Enum.TryParse(valueKind.GetRawText(), out vlKind)) {
                    vlKind = JsonValueKind.Undefined;
                }
            }

            (bool hasRuleResultId, JsonElement? ruleResultId, Guid rlResId) = result.GetRuleResultId();

            IObjectGraphType qry = null;
            bool hasQuery = result.TryGetProperty("Query", out JsonElement query);
            if (hasQuery) {
                // TODO: Need to check that all of the IObjectGraphType values are deserialized correctly 
                qry = JsonSerializer.Deserialize<IObjectGraphType>(query);
            }

            if (!hasRuleResultId || !hasQuery || qry == null) {
                vlKind = JsonValueKind.Undefined;
            }

            var request = newState.Requests.FirstOrDefault(o => o.EntityId == destEntId);
            if (request != null) {
                if (hasValueKind) {
                    request.ValueKind = vlKind;
                }
                if (hasRuleResultId) {
                    request.RuleResultId = rlResId;
                }
                if (hasQuery) {
                    request.Query = qry;
                }
                request.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            } else {
                request = new Request
                {
                    EntityId = destEntId,
                    ValueKind = vlKind,
                    Query = qry,
                    RuleResultId = rlResId,
                    LastChanged = DateTime.UtcNow
                };

                newState.Requests.Add(request);
            }
        }

        public static void FromOperationResultAddUpdateValue(this ProcessingRulEngStore newState, JsonElement result,
            Guid destEntId)
        {
            // Create/Update a Value using destEnt.EntityId and result
            JsonNode detail = JsonValue.Create(result);

            var value = newState.Values.FirstOrDefault(o => o.EntityId == destEntId);
            if (value != null) {
                if (detail != null) {
                    value.Detail = detail;
                }
                value.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            } else {
                value = new Value
                {
                    EntityId = destEntId,
                    Detail = detail,
                    LastChanged = DateTime.UtcNow
                };

                newState.Values.Add(value);
            }
        }

        private static (bool hasRuleResultId, JsonElement? ruleResultId, Guid rlResId) GetRuleResultId(this JsonElement result) {
            bool hasRuleResultId = result.TryGetProperty("RuleResultId", out JsonElement ruleResultId);
            Guid rlResId = Guid.Empty;
            if (hasRuleResultId) {
                var rrI = JsonHelpers.GetGuid(ruleResultId);
                if (rrI.HasValue) {
                    rlResId = rrI.Value;
                } else {
                    hasRuleResultId = false;
                }
            }

            return (hasRuleResultId, ruleResultId, rlResId);
        }
    }
}
