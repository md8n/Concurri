using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RulEng.Helpers
{
    public static class JsonHelpers
    {
        public static bool IsArray(this JsonElement? token) {
            return token.HasValue && token.Value.ValueKind.IsArray();
        }

        public static bool IsArray(this JsonNode token) {
            return token.GetValueKind().IsArray();
        }

        public static bool IsArray(this JsonValueKind type) {
            return type == JsonValueKind.Array;
        }

        public static bool IsNumeric(this JsonElement? token) {
            return token.HasValue && token.Value.ValueKind.IsNumeric();
        }

        public static bool IsNumeric(this JsonNode token) {
            return token.GetValueKind().IsNumeric();
        }

        public static bool IsNumeric(this JsonValueKind type) {
            return type == JsonValueKind.Number;
        }

        public static bool IsText(this JsonElement? token) {
            return token.HasValue && token.Value.ValueKind.IsText();
        }

        public static bool IsText(this JsonNode token) {
            return token.GetValueKind().IsText();
        }

        public static bool IsText(this JsonValueKind type) {
            return type == JsonValueKind.String;
        }

        public static bool IsGuid(this JsonElement? token)
        {
            return token.GetGuid().HasValue;
        }

        public static bool IsGuid(this JsonNode token) {
            return token.GetGuid().HasValue;
        }

        public static bool IsDate(this JsonElement? token) {
            return token.GetDate().HasValue;
        }

        public static bool IsDate(this JsonNode token) {
            return token.GetDate().HasValue;
        }

        public static bool IsBool(this JsonElement? token) {
            return token.HasValue && token.Value.ValueKind.IsBool();
        }

        public static bool IsBool(this JsonNode token) {
            return token.GetValueKind().IsBool();
        }

        public static bool IsBool(this JsonValueKind type) {
            return type == JsonValueKind.False || type == JsonValueKind.True;
        }

        public static decimal? GetNumeric(this JsonElement? token) {
            return token.IsNumeric() ? token.Value.GetDecimal() : null;
        }

        public static decimal? GetNumeric(this JsonNode token) {
            return token.IsNumeric() ? token.GetValue<decimal>() : null;
        }

        public static string GetText(this JsonElement? token) {
            return token.IsText() ? token.Value.GetRawText() : null;
        }

        public static string GetText(this JsonNode token) {
            return token.IsText() ? token.GetValue<string>() : null;
        }

        public static DateTime? GetDate(this JsonElement? token) {
            if (!token.HasValue || !token.IsText()) {
                return null;
            }

            if (token.Value.TryGetDateTime(out DateTime elemDate)) {
                return elemDate;
            } else {
                return null;
            }
        }

        public static DateTime? GetDate(this JsonNode token) {
            if (token.GetValueKind() != JsonValueKind.String) {
                return null;
            }

            if (DateTime.TryParse(token.GetText(), out DateTime elemDate)) {
                return elemDate;
            } else {
                return null;
            }
        }

        public static Guid? GetGuid(this JsonElement? token) {
            if (!token.HasValue) {
                return null;
            }

            if (token.Value.TryGetGuid(out Guid elemGuid)) {
                return elemGuid;
            } else {
                return null;
            }
        }

        public static Guid? GetGuid(this JsonNode token) {
            if (token.GetValueKind() != JsonValueKind.String) {
                return null;
            }

            if (Guid.TryParse(token.GetText(), out Guid elemGuid)) {
                return elemGuid;
            } else {
                return null;
            }
        }

        public static bool? GetBool(this JsonElement? token) {
            return token.IsBool() ? token.Value.GetBoolean() : null;
        }

        public static bool? GetBool(this JsonNode token) {
            return token.IsBool() ? token.GetValue<bool>() : null;
        }

        public static string ToTextValue(this JsonElement? token) {
            if (token.HasValue) {
                return null;
            }

            if (token.IsText()) {
                return token.Value.GetRawText();
            }

            if (token.IsNumeric()) {
                return token.Value.GetDecimal().ToString();
            }
            var tokDate = token.GetDate();
            if (tokDate.HasValue) {
                return tokDate.Value.ToString("u");
            }
            var tokGuid = token.GetGuid();
            if (tokGuid.HasValue) {
                return tokGuid.Value.ToString();
            }

            return null;
        }

        public static string ToTextValue(this JsonNode token) {
            if (token.IsText()) {
                return token.GetText();
            }

            if (token.IsNumeric()) {
                return token.GetNumeric().ToString();
            }
            var tokDate = token.GetDate();
            if (tokDate.HasValue) {
                return tokDate.Value.ToString("u");
            }
            var tokGuid = token.GetGuid();
            if (tokGuid.HasValue) {
                return tokGuid.Value.ToString();
            }

            return null;
        }
    }
}
