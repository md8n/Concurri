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
                OperationId = operationId == Guid.Empty ? Guid.NewGuid() : operationId,
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
            if (ruleResult != null)
            {
                operation.RuleResultId = ruleResult.EntityId;
            }

            operation.Operands = ImmutableArray.Create(operands?.ToArray() ?? new OperandKey[0]);

            if (!string.IsNullOrWhiteSpace(template))
            {
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
            var ops = operands?.ToArray() ?? new OperandKey[0];
            foreach (var op in ops)
            {
                if (op.SourceEntityIds.IsDefault)
                {
                    op.SourceEntityIds = ImmutableArray<Guid>.Empty;
                }
            }

            return new Operation
            {
                OperationId = operationId == Guid.Empty ? Guid.NewGuid() : operationId,
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
                OperationId = operationId == Guid.Empty ? Guid.NewGuid() : operationId,
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
                OperationId = operationId == Guid.Empty ? Guid.NewGuid() : operationId,
                RuleResultId = ruleResult.RuleResultId,
                Operands = ImmutableArray.Create(operands?.ToArray() ?? new OperandKey[0]),
                OperationType = OperationType.Search
            };
        }

        public static void FromOperationResultAddUpdateRule(this ProcessingRulEngStore newState, JToken result, Guid destEntId)
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


        public static Rule FromSearchOperationAddUpdateExistsRule(this ProcessingRulEngStore newState, IEntity sourceEnt, List<string> entTags, Guid destEntId)
        {
            // Create/Update a rule using destEnt.EntityId and result
            const RuleType rlType = RuleType.Exists;
            entTags = (entTags == null || entTags.Count == 0) ? sourceEnt.EntTags : entTags;
            var refValArray = sourceEnt.RulePrescription<RuleUnary>();
            var ruleName = $"Test for existence of {sourceEnt.EntType.ToString()} {(TypeKey)sourceEnt}";

            var rule = newState.Rules.FirstOrDefault(r => r.EntityId == destEntId);
            if (rule != null)
            {
                rule.RuleName = ruleName;
                rule.NegateResult = false;
                rule.RuleType = rlType;
                rule.EntTags = entTags;
                rule.ReferenceValues = refValArray;
                rule.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            }
            else
            {
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

        public static void FromOperationResultAddUpdateOperation(this ProcessingRulEngStore newState, JToken result,
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
            if (operation != null)
            {
                operation.OperationId = destEntId;
                operation.OperationType = opType;
                operation.RuleResultId = ruleResultId;
                operation.OperationTemplate = opTempl;
                operation.EntTags = entTags;
                //operation.ReferenceValues = refValArray;
                operation.LastChanged = DateTime.UtcNow;

                // TODO: Confirm the existing entity is updated
            }
            else
            {
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



        public static void FromOperationResultAddUpdateRequest(this ProcessingRulEngStore newState, JToken result,
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

        public static void FromOperationResultAddUpdateValue(this ProcessingRulEngStore newState, JToken result,
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
