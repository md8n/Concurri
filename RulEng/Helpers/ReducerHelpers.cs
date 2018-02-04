using System;

using Newtonsoft.Json.Linq;
using RulEng.States;

namespace RulEng.Helpers
{
    public static class ReducerHelpers
    {
        public static bool HasMeaningfulValue(this Value value)
        {
            if (value?.Detail == null)
            {
                return false;
            }

            var detail = value.Detail;
            var jType = detail.Type;
            switch (jType)
            {
                case JTokenType.None: // 0	No token type has been set.
                case JTokenType.Null: // 10	A null value.
                case JTokenType.Undefined: // 11	An undefined value.
                    return false;
                case JTokenType.Comment: // 5 A comment.
                    // Deliberately not supported
                    return false;
                case JTokenType.Object: // 1	A JSON object.
                    // Todo: check whether further testing is required for a valid result
                    var jObj = (JObject)detail;
                    return jObj != null;
                case JTokenType.Array: // 2	A JSON array.
                    // Todo: check whether further testing is required for a valid result
                    var jArr = (JArray)detail;
                    return jArr != null && jArr.Count > 0;
                    //Constructor 3   A JSON constructor.
                    //Property    4   A JSON object property.
                case JTokenType.Integer: // 6 An integer value.
                    var jInt = detail.Value<int?>();
                    return jInt.HasValue;
                case JTokenType.Float: // 7	A float value.
                    var jFloat = detail.Value<float?>();
                    return jFloat.HasValue;
                case JTokenType.String: // 8	A string value.
                    var jString = detail.Value<string>();
                    return !string.IsNullOrWhiteSpace(jString);
                case JTokenType.Boolean: // 9	A boolean value.
                    var jBool = detail.Value<bool?>();
                    return jBool.HasValue;
                case JTokenType.Date: // 12	A date value.
                    var jDate = detail.Value<DateTime?>();
                    return jDate.HasValue;
                    //Raw 13  A raw JSON value.
                    //Bytes   14  A collection of bytes value.
                case JTokenType.Guid: // 15	A Guid value.
                    var jGuid = detail.Value<Guid?>();
                    return jGuid.HasValue;
                case JTokenType.Uri: // 16	A Uri value.
                    // Todo: verify this test is valid
                    var jUri = detail.Value<Uri>();
                    return jUri.Segments.Length > 0;
                case JTokenType.TimeSpan: // 17	A TimeSpan value.
                    var jTimeSpan = detail.Value<TimeSpan?>();
                    return jTimeSpan.HasValue;
                case JTokenType.Constructor:
                case JTokenType.Property:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                default:
                    return false;
            }
        }
    }
}
