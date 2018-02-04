using Newtonsoft.Json.Linq;
using System;

namespace RulEng.Helpers
{
    public static class JsonHelpers
    {
        public static bool IsArray(this JToken token)
        {
            return token?.Type.IsArray() ?? false;
        }

        public static bool IsArray(this JTokenType type)
        {
            return type == JTokenType.Array;
        }

        public static bool IsNumeric(this JToken token)
        {
            return token?.Type.IsNumeric() ?? false;
        }

        public static bool IsNumeric(this JTokenType type)
        {
            return type == JTokenType.Integer || type == JTokenType.Float;
        }

        public static bool IsText(this JToken token)
        {
            return token?.Type.IsText() ?? false;
        }

        public static bool IsText(this JTokenType type)
        {
            return type == JTokenType.String;
        }

        public static bool IsGuid(this JToken token)
        {
            return token?.Type.IsGuid() ?? false;
        }

        public static bool IsGuid(this JTokenType type)
        {
            return type == JTokenType.Guid;
        }

        public static bool IsDate(this JToken token)
        {
            return token?.Type.IsDate() ?? false;
        }

        public static bool IsDate(this JTokenType type)
        {
            return type == JTokenType.Date;
        }

        public static bool IsBool(this JToken token)
        {
            return token?.Type.IsBool() ?? false;
        }

        public static bool IsBool(this JTokenType type)
        {
            return type == JTokenType.Boolean;
        }

        public static decimal? GetNumeric(this JToken token)
        {
            return token.IsNumeric() ? (decimal?)token : null;
        }

        public static string GetText(this JToken token)
        {
            return token.IsText() ? (string)token : null;
        }

        public static DateTime? GetDate(this JToken token)
        {
            return token.IsDate() ? (DateTime?)token : null;
        }

        public static Guid? GetGuid(this JToken token)
        {
            return token.IsGuid() ? (Guid?)token : null;
        }

        public static bool? GetBool(this JToken token)
        {
            return token.IsBool() ? (bool?)token : null;
        }

        public static string ToTextValue(this JToken token)
        {
            if (token.IsText())
            {
                return (string)token;
            }

            if (token.IsNumeric())
            {
                var decTok = (decimal?)token;
                return decTok.HasValue ? decTok.ToString() : null;
            }
            if (token.IsDate())
            {
                var dateTok = (DateTime?)token;
                return dateTok?.ToString("u");
            }
            if (token.IsGuid())
            {
                var guidTok = (Guid?)token;
                return guidTok?.ToString();
            }

            return null;
        }
    }
}
