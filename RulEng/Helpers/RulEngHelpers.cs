using System;
using System.Collections.Generic;
using Redux;
using RulEng.States;
using System.Collections.Immutable;
using System.Linq;
using RulEng.Prescriptions;

namespace RulEng.Helpers
{
    public static class RulEngHelpers
    {
        public static (Rule rule, RuleResult ruleResult, Operation operation, Value value, IRuleProcessing rulePrescription, ICrud operationPrescription) Exists(this object val){
            var value = new Value(val);
            var vType = new TypeKey { EntityId = value.ValueId, EntityType = EntityType.Value };
            var rule = vType.ExistsRule();
            var ruleResult = new RuleResult(rule);
            var operation = ruleResult.ExistsOperation(vType);

            var rulePrescription = rule.Exists();
            var operationPrescription = operation.Create();

            return (rule, ruleResult, operation, value, rulePrescription, operationPrescription);
        }

        public static (Rule rule, RuleResult ruleResult, Value value, IRuleProcessing rulePrescription) Exists(this object val)
        {
            var value = (val as Value) == null ? new Value(val) : (val as Value);
            var vType = new TypeKey { EntityId = value.ValueId, EntityType = EntityType.Value };
            var rule = vType.ExistsRule();
            var ruleResult = new RuleResult(rule);

            var rulePrescription = rule.Exists();

            return (rule, ruleResult, value, rulePrescription);
        }

        /// <summary>
        /// Add this Value Exists test to an existing Exists Rule
        /// </summary>
        /// <param name="val"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public static (RuleResult ruleResult, Operation operation, Value value, IAction operationPrescription) Exists(this object val, Rule rule)
        {
            if (rule.RuleType != RuleType.Exists || !rule.NegateResult)
            {
                throw new ArgumentException("rule was not a 'not' 'Exists' type Rule.  It cannot have an exists test added to it.");
            }
            var value = new Value(val);
            var vType = new TypeKey { EntityId = value.ValueId, EntityType = EntityType.Value };

            // Add the test ref data to the end of the refvalues structure
            // and put the result back into the refvalues
            rule.ReferenceValues = rule.ReferenceValues.Add(vType);
            
            var ruleResult = new RuleResult(rule);
            var operation = ruleResult.ExistsOperation(vType);
            var operationPrescription = operation.Exists();

            return (ruleResult, operation, value, operationPrescription);
        }

        public static (Operation operation, Value value, IAction operationPrescription) Exists(this object val, RuleResult ruleResult)
        {
            var value = new Value(val);
            var vType = new TypeKey() { EntityId = value.ValueId, EntityType = EntityType.Value };
            var operation = ruleResult.ExistsOperation(vType);

            var operationPrescription = operation.Exists();

            return (operation, value, operationPrescription);
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

        public static (Operation operation, IEnumerable<Value> value, IAction operationPrescription) Add(this RuleResult ruleResult, IEnumerable<IEnumerable<Guid>> valueIds)
        {
            var values = new List<Value>();
            var vOpers = new List<OperandKey>();

            foreach(var vSet in valueIds.ToArray())
            {
                var value = new Value(0);
                vOpers.Add(new OperandKey() { SourceValueIds = ImmutableArray.Create(vSet.ToArray()), EntityId = value.ValueId, EntityType = EntityType.Value });
            }

            var operation = ruleResult.AddOperation(vOpers);
            var operationPrescription = operation.Exists();

            return (operation, values, operationPrescription);
        }
    }
}
