using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Json
{
    /// <summary>
    /// Deterministic JSON writer: object keys sorted ordinally, "\n" line endings, no trailing whitespace and a final
    /// newline. The indented form keeps arrays of primitives on one line (grids stay readable); the compact form is a
    /// single line for JSON-Lines packs. Equal documents always produce equal bytes.
    /// </summary>
    public static class CanonicalJson
    {
        public static string Write(JToken token, bool indented)
        {
            var sb = new StringBuilder();
            WriteToken(sb, token, indented, 0);
            if (indented)
            {
                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static void WriteToken(StringBuilder sb, JToken token, bool indented, int depth)
        {
            switch (token)
            {
                case JObject obj:
                    WriteObject(sb, obj, indented, depth);
                    break;
                case JArray array:
                    WriteArray(sb, array, indented, depth);
                    break;
                case JValue value:
                    WriteValue(sb, value);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported JSON token {token.Type}.");
            }
        }

        private static void WriteObject(StringBuilder sb, JObject obj, bool indented, int depth)
        {
            var properties = obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
            if (properties.Count == 0)
            {
                sb.Append("{}");
                return;
            }

            sb.Append('{');
            for (int i = 0; i < properties.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }

                if (indented)
                {
                    NewLine(sb, depth + 1);
                }

                sb.Append(JsonConvert.ToString(properties[i].Name));
                sb.Append(indented ? ": " : ":");
                WriteToken(sb, properties[i].Value, indented, depth + 1);
            }

            if (indented)
            {
                NewLine(sb, depth);
            }

            sb.Append('}');
        }

        private static void WriteArray(StringBuilder sb, JArray array, bool indented, int depth)
        {
            if (array.Count == 0)
            {
                sb.Append("[]");
                return;
            }

            bool inline = !indented || array.All(t => t is JValue);
            sb.Append('[');
            for (int i = 0; i < array.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(inline && indented ? ", " : ",");
                }

                if (!inline)
                {
                    NewLine(sb, depth + 1);
                }

                WriteToken(sb, array[i], indented, depth + 1);
            }

            if (!inline)
            {
                NewLine(sb, depth);
            }

            sb.Append(']');
        }

        private static void WriteValue(StringBuilder sb, JValue value)
        {
            switch (value.Type)
            {
                case JTokenType.String:
                    sb.Append(JsonConvert.ToString((string)value!));
                    break;
                case JTokenType.Integer:
                    sb.Append(Convert.ToString(value.Value, CultureInfo.InvariantCulture));
                    break;
                case JTokenType.Float:
                    sb.Append(FormatDecimal(Convert.ToDecimal(value.Value, CultureInfo.InvariantCulture)));
                    break;
                case JTokenType.Boolean:
                    sb.Append((bool)value ? "true" : "false");
                    break;
                case JTokenType.Null:
                    sb.Append("null");
                    break;
                default:
                    throw new NotSupportedException($"Unsupported JSON value {value.Type}.");
            }
        }

        /// <summary>Plain decimal notation without trailing zeros, so 0.50 and 0.5 serialize identically.</summary>
        private static string FormatDecimal(decimal value)
        {
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (text.IndexOf('.') >= 0)
            {
                text = text.TrimEnd('0').TrimEnd('.');
            }

            return text == "-0" ? "0" : text;
        }

        private static void NewLine(StringBuilder sb, int depth)
        {
            sb.Append('\n');
            sb.Append(' ', depth * 2);
        }
    }
}
