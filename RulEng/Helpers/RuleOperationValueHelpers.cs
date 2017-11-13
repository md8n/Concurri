using System;
using System.Collections.Generic;
using Redux;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class RuleOperationValueHelpers
    {
        public static (Rule rule, Operation operation, Value value, IAction ruleAction, IAction operationAction) Exists(this object val){
            var value = new Value(val);
            var rule = ((ITypeKey)value).ExistsRule();
            var operation = rule.ExistsOperation((ITypeKey)value);

            var ruleAction = rule.ExistsAction();
            var operationAction = operation.ExistsAction();

            return (rule, operation, value, ruleAction, operationAction);
        }

        public static (Operation operation, Value value, IAction operationAction) Exists(this object val, Rule rule)
        {
            var value = new Value(val);
            var operation = rule.ExistsOperation((ITypeKey)value);

            var operationAction = operation.ExistsAction();

            return (operation, value, operationAction);
        }

        public static (Operation operation, Value value, IAction operationAction) Add(this Rule rule, IEnumerable<Guid> valueIds)
        {
            var value = new Value(0);
            var operation = rule.AddOperation(value, valueIds);

            var operationAction = operation.ExistsAction();

            return (operation, value, operationAction);
        }
    }
}
