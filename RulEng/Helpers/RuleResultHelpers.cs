using RulEng.States;

namespace RulEng.Helpers
{
    public static class RuleResultHelpers
    {
        public static RuleResult ExistsRuleResult(this Rule rule)
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

            return new RuleResult
            {
                RuleId = rule.RuleId
            };
        }

        public static RuleResult AddRuleResult(this Rule rule)
        {
            return new RuleResult
            {
                RuleId = rule.RuleId
            };
        }
    }
}
