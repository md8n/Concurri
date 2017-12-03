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
        public static (Rule rule, RuleResult ruleResult, IRuleProcessing rulePrescription) Exists<T>(this T val) where T : IEntity
        {
            if (!val.IsProcessable())
            {
                throw new ArgumentOutOfRangeException("val.Type", "Exists helper creator is only for Processable entity types");
            }

            var vType = new TypeKey { EntityId = val.EntityId, EntityType = val.Type };

            var rule = vType.ExistsRule();
            var ruleResult = new RuleResult(rule);
            var rulePrescription = rule.Exists();

            return (rule, ruleResult, rulePrescription);
        }

        /// <summary>
        /// Add this Entity Exists test to an existing Exists Rule
        /// </summary>
        /// <param name="val"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static (RuleResult ruleResult, IAction rulePrescription) Exists<T>(this T entity, Rule rule) where T : IEntity
        {
            if (!entity.IsProcessable())
            {
                throw new ArgumentOutOfRangeException("entity.Type", "Exists helper creator is only for Processable entity types");
            }

            if (rule.RuleType != RuleType.Exists || !rule.NegateResult)
            {
                throw new ArgumentException("rule was not a 'not' 'Exists' type Rule.  It cannot have an exists test added to it.");
            }

            var vType = new TypeKey { EntityId = entity.EntityId, EntityType = entity.Type };

            // Add the test ref data to the end of the refvalues structure
            // and put the result back into the refvalues
            rule.ReferenceValues = rule.ReferenceValues.Add(vType);

            var ruleResult = new RuleResult(rule);
            var rulePrescription = rule.Exists();

            return (ruleResult, rulePrescription);
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

            var vOper = new OperandKey()
            {
                SourceValueIds = ImmutableArray.Create(entity.EntityId),
                EntityId = entity.EntityId,
                EntityType = eOfType.Type
            };
            var operation = ruleResult.AddOperation(new[] { vOper });

            var operationPrescription = operation.Create();

            return (operation, operationPrescription);
        }

        /// <summary>
        /// Build the Create Operation, Entity and Prescription to match the RuleResult
        /// </summary>
        /// <param name="ruleResult"></param>
        /// <param name="valueIds"></param>
        /// <returns></returns>
        public static (Operation operation, T value, ICrud operationPrescription) Create<T>(this RuleResult ruleResult, IEnumerable<Guid> entityIds) where T : IEntity
        {
            var entity = default(T);
            var vOper = new OperandKey() { SourceValueIds = ImmutableArray.Create(entityIds.ToArray()), EntityId = entity.EntityId, EntityType = EntityType.Value };
            var operation = ruleResult.AddOperation(new[] { vOper });

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
            var vType = new TypeKey { EntityId = val.EntityId, EntityType = val.Type };

            var rule = vType.HasMeaningfulValueRule();
            var ruleResult = new RuleResult(rule);
            var rulePrescription = rule.HasMeaningfulValue();

            return (rule, ruleResult, rulePrescription);
        }

        public static (Operation operation, IEnumerable<Value> value, OperationMxAddProcessing operationPrescription) Add(this RuleResult ruleResult, IEnumerable<IEnumerable<Guid>> valueIds)
        {
            var values = new List<Value>();
            var vOpers = new List<OperandKey>();

            foreach (var vSet in valueIds.ToArray())
            {
                var value = new Value(0);
                vOpers.Add(new OperandKey() { SourceValueIds = ImmutableArray.Create(vSet.ToArray()), EntityId = value.ValueId, EntityType = EntityType.Value });
            }

            var operation = ruleResult.AddOperation(vOpers);
            var operationPrescription = new OperationMxAddProcessing();
            operationPrescription.Entities = ImmutableArray.Create(vOpers.ToArray());

            return (operation, values, operationPrescription);
        }
    }
}
