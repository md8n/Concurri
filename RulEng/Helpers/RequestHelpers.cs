using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Newtonsoft.Json.Linq;

using RulEng.States;
using GraphQL.Types;

namespace RulEng.Helpers
{
    public static class RequestHelpers
    {
        public static Request ExistsRequest(this RuleResult ruleResult, JTokenType valueType, IObjectGraphType query)
        {
            //object valueSource = null;
            //var entList = entities.ToList();
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

            return new Request
            {
                RequestId = Guid.NewGuid(),
                RuleId = ruleResult.RuleId,
                ValueType = valueType,
                Query = query
            };
        }

        public static Request AddRequest(this RuleResult ruleResult, JTokenType valueType, IObjectGraphType query)
        {
            return new Request
            {
                RequestId = Guid.NewGuid(),
                RuleId = ruleResult.RuleId,
                ValueType = valueType,
                Query = query
            };
        }
    }
}
