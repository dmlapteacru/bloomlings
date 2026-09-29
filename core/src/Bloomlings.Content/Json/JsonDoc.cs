using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Json
{
    /// <summary>
    /// Strict reading helpers over Newtonsoft's JObject. Documents are parsed without date or float conversion, unknown
    /// properties are rejected (the schemas use <c>additionalProperties: false</c>) and every error names its JSON path.
    /// No reflection is used, which keeps the code safe under IL2CPP stripping.
    /// </summary>
    internal static class JsonDoc
    {
        private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
        {
            DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
            CommentHandling = CommentHandling.Ignore,
            LineInfoHandling = LineInfoHandling.Ignore,
        };

        public static JObject ParseObject(string json, string what)
        {
            try
            {
                using var reader = new JsonTextReader(new StringReader(json))
                {
                    DateParseHandling = DateParseHandling.None,
                    FloatParseHandling = FloatParseHandling.Decimal,
                };
                JToken token = JToken.ReadFrom(reader, LoadSettings);
                if (reader.Read())
                {
                    throw new ContentFormatException(what, "unexpected content after the JSON document");
                }

                if (!(token is JObject obj))
                {
                    throw new ContentFormatException(what, "expected a JSON object");
                }

                return obj;
            }
            catch (JsonException ex)
            {
                throw new ContentFormatException(what, "invalid JSON: " + ex.Message);
            }
        }

        public static void AllowOnly(JObject obj, string path, params string[] allowed)
        {
            var set = new HashSet<string>(allowed, StringComparer.Ordinal);
            foreach (JProperty property in obj.Properties())
            {
                if (!set.Contains(property.Name))
                {
                    throw new ContentFormatException(Join(path, property.Name), "unknown property");
                }
            }
        }

        public static JToken Required(JObject obj, string path, string name)
        {
            JToken? token = obj[name];
            if (token == null || token.Type == JTokenType.Null)
            {
                throw new ContentFormatException(Join(path, name), "required property is missing");
            }

            return token;
        }

        public static JToken? Optional(JObject obj, string name)
        {
            JToken? token = obj[name];
            return token == null || token.Type == JTokenType.Null ? null : token;
        }

        public static JObject Object(JToken token, string path)
        {
            if (!(token is JObject obj))
            {
                throw new ContentFormatException(path, "expected an object");
            }

            return obj;
        }

        public static JArray Array(JToken token, string path, int minItems = 0, int maxItems = int.MaxValue)
        {
            if (!(token is JArray array))
            {
                throw new ContentFormatException(path, "expected an array");
            }

            if (array.Count < minItems || array.Count > maxItems)
            {
                string range = maxItems == int.MaxValue ? $"at least {minItems}" : $"{minItems}–{maxItems}";
                throw new ContentFormatException(path, $"expected {range} items, got {array.Count}");
            }

            return array;
        }

        public static string String(JToken token, string path)
        {
            if (token.Type != JTokenType.String)
            {
                throw new ContentFormatException(path, "expected a string");
            }

            return (string)token!;
        }

        public static string Id(JToken token, string path)
        {
            string value = String(token, path);
            if (value.Length == 0)
            {
                throw new ContentFormatException(path, "must not be empty");
            }

            foreach (char c in value)
            {
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))
                {
                    throw new ContentFormatException(path, $"'{value}' must match ^[a-z0-9_]+$");
                }
            }

            return value;
        }

        public static int Int(JToken token, string path, int min = int.MinValue, int max = int.MaxValue)
        {
            if (token.Type != JTokenType.Integer)
            {
                throw new ContentFormatException(path, "expected an integer");
            }

            long value = token.Value<long>();
            if (value < min || value > max)
            {
                throw new ContentFormatException(path, $"{value} is outside {min}..{max}");
            }

            return (int)value;
        }

        public static bool Bool(JToken token, string path)
        {
            if (token.Type != JTokenType.Boolean)
            {
                throw new ContentFormatException(path, "expected true or false");
            }

            return (bool)token;
        }

        public static decimal Number(JToken token, string path, decimal min, decimal max)
        {
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
            {
                throw new ContentFormatException(path, "expected a number");
            }

            decimal value = Convert.ToDecimal(((JValue)token).Value, CultureInfo.InvariantCulture);
            if (value < min || value > max)
            {
                throw new ContentFormatException(path, $"{value} is outside {min}..{max}");
            }

            return value;
        }

        public static T Enum<T>(JToken token, string path, EnumNames<T> names)
            where T : struct, System.Enum
        {
            string wire = String(token, path);
            if (!names.TryParse(wire, out T value))
            {
                throw new ContentFormatException(path, $"'{wire}' is not one of: {names.Allowed}");
            }

            return value;
        }

        public static string Join(string path, string name) => path.Length == 0 ? name : path + "." + name;

        public static string Index(string path, int index) => $"{path}[{index}]";
    }
}
