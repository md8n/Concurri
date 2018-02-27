using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Redux;
using RulEng.States;
using RulEng.Prescriptions;

namespace RulEng.Helpers
{
    public static class RulEngHelpers
    {
        /// <summary>
        /// For a given entity (Rule, Operation, Request, Value) this creates:
        /// A (not) Exists Rule to test for its presence
        /// A RuleResult to accept the result of the Exists Rule
        /// A RulePrescription referencing the Rule to be performed
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static (Rule rule, RuleResult ruleResult, IRuleProcessing ruleProcessing) Exists<T>(this T val) where T : IEntity
        {
            if (!val.IsProcessable())
            {
                throw new ArgumentOutOfRangeException(nameof(val), "Exists helper creator is only for Processable entity types");
            }

            var vType = new TypeKey { EntityId = val.EntityId, EntType = val.EntType, LastChanged = val.LastChanged };

            var rule = vType.ExistsRule();

            var rulePrescription = IRuleProcessing rule.RulePrescription<RuleUnary>();
            var ruleResult = new RuleResult(rule)
            {
                RuleResultId = rulePrescription.RuleResultId
            };

            rule.ReferenceValues.RuleResultId = rulePrescription.RuleResultId;

            return (rule, ruleResult, rulePrescription);
        }

        /// <summary>
        /// For the result of a given Rule 
        /// build the corresponding Operation and OperationPrescription
        /// to effect the creation of the Entity identified
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ruleResult"></param>
        /// <param name="entity">The Value from which the Entity will be created</param>
        /// <returns></returns>
        public static (Operation operation, ICrud operationPrescription) Create<T>(this RuleResult ruleResult, Value entity) where T : IEntity
        {
            var eOfType = default(T);

            var vOper = new OperandKey
            {
                SourceValueIds = ImmutableArray.Create(entity.EntityId),
                EntityId = entity.EntityId,
                EntType = eOfType.EntType
            };
            var operation = ruleResult.CreateOperation(new[] { vOper });

            var operationPrescription = operation.Create();

            return (operation, operationPrescription);
        }

        /// <summary>
        /// Build the Create Operation, Entity and Prescription to match the RuleResult
        /// </summary>
        /// <param name="ruleResult"></param>
        /// <param name="entityIds"></param>
        /// <returns></returns>
        public static (Operation operation, T value, ICrud operationPrescription) Create<T>(this RuleResult ruleResult, IEnumerable<Guid> entityIds) where T : IEntity
        {
            var entity = default(T);
            var vOper = new OperandKey
            {
                SourceValueIds = ImmutableArray.Create(entityIds.ToArray()),
                EntityId = entity.EntityId,
                EntType = EntityType.Value
            };
            var operation = ruleResult.CreateOperation(new[] { vOper });

            var operationPrescription = operation.Create();

            return (operation, entity, operationPrescription);
        }

        /// <summary>
        /// For a given Value this creates:
        /// A HasMeaningfulValue Rule to test its value
        /// A RuleResult to accept the result of the HasMeaningfulValue Rule
        /// A RulePrescription referencing the Rule to be performed
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static (Rule rule, RuleResult ruleResult, IRuleProcessing rulePrescription) HasMeaningfulValue<T>(this T val) where T : Value
        {
            var vType = new TypeKey { EntityId = val.EntityId, EntType = val.EntType, LastChanged = val.LastChanged };

            var rule = vType.HasMeaningfulValueRule();
            var ruleResult = new RuleResult(rule);
            var rulePrescription = rule.HasMeaningfulValue();

            return (rule, ruleResult, rulePrescription);
        }

        /// <summary>
        /// For a given Rule this creates:
        /// A RuleResult to accept the result of the Rule
        /// A RulePrescription referencing the Rule to be performed
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static (RuleResult ruleResult, T rulePrescription) ResultAndPrescription<T>(this Rule rule) where T : IRulePrescription, new()
        {
            var rulePrescription = rule.RulePrescription<T>();
            var ruleResult = new RuleResult(rule)
            {
                RuleResultId = rulePrescription.RuleResultId
            };

            rule.ReferenceValues.RuleResultId = rulePrescription.RuleResultId;

            return (ruleResult, rulePrescription);
        }

        public static (Operation operation, IEnumerable<Value> value, OperationMxProcessing operationPrescription) Add(this RuleResult ruleResult, IEnumerable<IEnumerable<Guid>> valueIds)
        {
            var values = new List<Value>();
            var vOpers = (from vSet in valueIds.ToArray()
                let value = new Value(0)
                select new OperandKey
                {
                    SourceValueIds = ImmutableArray.Create(vSet.ToArray()),
                    EntityId = value.ValueId,
                    EntType = EntityType.Value
                })
                .ToArray();

            var operation = ruleResult.CreateOperation(vOpers);
            var operationPrescription =
                new OperationMxProcessing {Entities = ImmutableArray.Create(vOpers)};

            return (operation, values, operationPrescription);
        }
    }
}
