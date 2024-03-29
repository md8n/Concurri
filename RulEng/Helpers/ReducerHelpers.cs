using System.Text.Json;

using RulEng.States;

namespace RulEng.Helpers;

public static class ReducerHelpers {
    public static bool HasMeaningfulValue(this Value value) {
        if (value?.Detail == null) {
            return false;
        }

        var detail = value.Detail;
        var jType = detail.GetValueKind();
        switch (jType)
        {
            //case JsonValueKind.None: // 0	No token type has been set.
            case JsonValueKind.Null: // 10	A null value.
            case JsonValueKind.Undefined: // 11	An undefined value.
                return false;
            //case JsonValueKind.Comment: // 5 A comment.
            //    // Deliberately not supported
            //    return false;
            case JsonValueKind.Object: // 1	A JSON object.
                // Todo: check whether further testing is required for a valid result
                var jObj = detail.AsObject();
                return jObj != null;
            case JsonValueKind.Array: // 2	A JSON array.
                // Todo: check whether further testing is required for a valid result
                var jArr = detail.AsArray();
                return jArr != null && jArr.Count > 0;
            //Constructor 3   A JSON constructor.
            //Property    4   A JSON object property.
            case JsonValueKind.Number: // (5) An numeric value.
                var jDec = detail.GetNumeric();
                return jDec.HasValue;
            //case JTokenType.Integer: // 6 An integer value.
            //    var jInt = detail.Value<int?>();
            //    return jInt.HasValue;
            //case JTokenType.Float: // 7	A float value.
            //    var jFloat = detail.Value<float?>();
            //    return jFloat.HasValue;
            case JsonValueKind.String: // 8	A string value.
                var jString = detail.GetText();
                return !string.IsNullOrWhiteSpace(jString);
            //case JsonValueKind.Boolean: // 9	A boolean value.
            //    var jBool = detail.Value<bool?>();
            //    return jBool.HasValue;
            case JsonValueKind.True: // (10)	A boolean true value.
                return true;
            case JsonValueKind.False: // (11)	A boolean false value.
                return true; // Ironically
            //case JTokenType.Date: // 12	A date value.
            //    var jDate = detail.Value<DateTime?>();
            //    return jDate.HasValue;
            //    //Raw 13  A raw JSON value.
            //    //Bytes   14  A collection of bytes value.
            //case JTokenType.Guid: // 15	A Guid value.
            //    var jGuid = detail.Value<Guid?>();
            //    return jGuid.HasValue;
            //case JTokenType.Uri: // 16	A Uri value.
            //    // Todo: verify this test is valid
            //    var jUri = detail.Value<Uri>();
            //    return jUri.Segments.Length > 0;
            //case JTokenType.TimeSpan: // 17	A TimeSpan value.
            //    var jTimeSpan = detail.Value<TimeSpan?>();
            //    return jTimeSpan.HasValue;
            //case JTokenType.Constructor:
            //case JTokenType.Property:
            //case JTokenType.Raw:
            //case JTokenType.Bytes:
            default:
                return false;
        }
    }
}
