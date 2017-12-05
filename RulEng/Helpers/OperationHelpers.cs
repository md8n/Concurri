using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class OperationHelpers
    {
        public static Operation ExistsOperation(this RuleResult ruleResult, ITypeKey entity)
        {
            return ruleResult.ExistsOperation(new List<ITypeKey> { entity });
        }

        public static Operation ExistsOperation(this RuleResult ruleResult, IEnumerable<ITypeKey> entities)
        {
            //object valueSource = null;
            var entList = entities.ToList();
            //var valueType = entList[0].Detail.Type;

            //switch (valueType)
            //{
            //    case JTokenType.Boolean:
            //        valueSource = valList[0].Detail.ToObject<bool>();
            //        break;
            //    case JTokenType.Date:
            //        valueSource = valList[0].Detail.ToObject<DateTime>();
            //        break;
            //    case JTokenType.Float:
            //        valueSource = valList[0].Detail.ToObject<float>();
            //        break;
            //    case JTokenType.Guid:
            //        valueSource = valList[0].Detail.ToObject<Guid>();
            //        break;
            //    case JTokenType.Integer:
            //        valueSource = valList[0].Detail.ToObject<int>();
            //        break;
            //    case JTokenType.Object:
            //        valueSource = valList[0].Detail;
            //        break;
            //    case JTokenType.String:
            //        valueSource = valList[0].Detail.ToObject<string>();
            //        break;
            //    case JTokenType.Uri:
            //        valueSource = valList[0].Detail.ToObject<Uri>();
            //        break;
            //}

            return new Operation
            {
                OperationId = Guid.NewGuid(),
                RuleResultId = ruleResult.RuleResultId,
                Operands = ImmutableArray.Create(entList.Select(el => new OperandKey{SourceValueIds = ImmutableArray.Create(el.EntityId), EntityId = el.EntityId, EntityType = el.EntityType}).ToArray()),
                OperationType = OperationType.CreateUpdate
            };
        }

        public static Operation AddOperation(this RuleResult ruleResult, IEnumerable<OperandKey> operands)
        {
            return new Operation
            {
                OperationId = Guid.NewGuid(),
                RuleResultId = ruleResult.RuleResultId,
                Operands = ImmutableArray.Create(operands.ToArray()),
                OperationType = OperationType.CreateUpdate
            };
        }
    }
}
