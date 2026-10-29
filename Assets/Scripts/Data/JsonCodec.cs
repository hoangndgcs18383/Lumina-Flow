using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace LuminaFlow
{
    // Small, bounded JSON codec for the public-field DTOs. No Unity or package dependency.
    internal static class JsonCodec
    {
        internal static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

        internal static string Write(object value)
        {
            var output = new StringBuilder();
            WriteValue(output, value);
            return output.ToString();
        }

        internal static object Read(string text)
        {
            if (text == null) throw new FormatException("JSON is missing.");
            // Reject isolated UTF-16 surrogates, even when decoding direct JSON rather than a share.
            try { Utf8.GetByteCount(text); }
            catch (EncoderFallbackException) { throw new FormatException("Invalid Unicode."); }
            var parser = new Parser(text);
            object value = parser.Value(0);
            parser.Space();
            if (parser.index != text.Length) throw new FormatException("Unexpected trailing JSON.");
            return value;
        }

        internal static T ConvertTo<T>(object value) { return (T)ConvertValue(value, typeof(T)); }

        private static object ConvertValue(object value, Type type)
        {
            if (type == typeof(string))
            {
                if (!(value is string)) throw new FormatException("Expected a string.");
                return value;
            }
            if (type == typeof(int))
            {
                if (!(value is long) || (long)value < int.MinValue || (long)value > int.MaxValue)
                    throw new FormatException("Expected a 32-bit integer.");
                return (int)(long)value;
            }
            if (type == typeof(bool))
            {
                if (!(value is bool)) throw new FormatException("Expected a boolean.");
                return value;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var source = value as List<object>;
                if (source == null) throw new FormatException("Expected an array.");
                var list = (IList)Activator.CreateInstance(type);
                foreach (object item in source) list.Add(ConvertValue(item, type.GetGenericArguments()[0]));
                return list;
            }
            var map = value as Dictionary<string, object>;
            if (map == null) throw new FormatException("Expected an object.");
            object instance = Activator.CreateInstance(type);
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                object member;
                if (map.TryGetValue(field.Name, out member)) field.SetValue(instance, ConvertValue(member, field.FieldType));
            }
            var piece = instance as PieceData;
            if (piece != null && !map.ContainsKey("fixed"))
                piece.@fixed = piece.type != "mirror" && piece.type != "splitter";
            return instance;
        }

        private static void WriteValue(StringBuilder output, object value)
        {
            if (value == null) { output.Append("null"); return; }
            var text = value as string;
            if (text != null) { Quote(output, text); return; }
            if (value is bool) { output.Append((bool)value ? "true" : "false"); return; }
            if (value is int || value is long) { output.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            var list = value as IList;
            if (list != null)
            {
                output.Append('[');
                for (int i = 0; i < list.Count; i++)
                { if (i > 0) output.Append(','); WriteValue(output, list[i]); }
                output.Append(']'); return;
            }
            output.Append('{');
            FieldInfo[] fields = value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) output.Append(',');
                Quote(output, fields[i].Name); output.Append(':'); WriteValue(output, fields[i].GetValue(value));
            }
            output.Append('}');
        }

        private static void Quote(StringBuilder output, string value)
        {
            Utf8.GetByteCount(value);
            output.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': output.Append("\\\""); break;
                    case '\\': output.Append("\\\\"); break;
                    case '\n': output.Append("\\n"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\t': output.Append("\\t"); break;
                    default:
                        if (c < 32) output.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else output.Append(c);
                        break;
                }
            }
            output.Append('"');
        }

        private sealed class Parser
        {
            private readonly string text;
            internal int index;
            internal Parser(string text) { this.text = text; }
            internal void Space()
            { while (index < text.Length && (text[index] == ' ' || text[index] == '\t' || text[index] == '\r' || text[index] == '\n')) index++; }
            private bool Take(char c)
            { Space(); if (index < text.Length && text[index] == c) { index++; return true; } return false; }
            private void Require(char c)
            { if (!Take(c)) throw new FormatException("Expected '" + c + "'."); }

            internal object Value(int depth)
            {
                if (depth > 32) throw new FormatException("JSON nesting is too deep.");
                Space();
                if (index >= text.Length) throw new FormatException("Unexpected end of JSON.");
                char c = text[index];
                if (c == '"') return String();
                if (Take('{'))
                {
                    var map = new Dictionary<string, object>(StringComparer.Ordinal);
                    if (Take('}')) return map;
                    do
                    {
                        Space();
                        if (index >= text.Length || text[index] != '"') throw new FormatException("Expected an object key.");
                        string key = String(); Require(':');
                        if (map.ContainsKey(key)) throw new FormatException("Duplicate JSON field: " + key);
                        map.Add(key, Value(depth + 1));
                        if (Take('}')) return map;
                        Require(',');
                    } while (true);
                }
                if (Take('['))
                {
                    var list = new List<object>();
                    if (Take(']')) return list;
                    do
                    {
                        list.Add(Value(depth + 1));
                        if (Take(']')) return list;
                        Require(',');
                    } while (true);
                }
                if (Literal("true")) return true;
                if (Literal("false")) return false;
                if (Literal("null")) return null;
                return Number();
            }

            private bool Literal(string literal)
            {
                if (index + literal.Length > text.Length || string.CompareOrdinal(text, index, literal, 0, literal.Length) != 0) return false;
                index += literal.Length; return true;
            }

            private object Number()
            {
                int start = index;
                if (index < text.Length && text[index] == '-') index++;
                if (index >= text.Length || text[index] < '0' || text[index] > '9') throw new FormatException("Invalid JSON number.");
                if (text[index] == '0') index++;
                else while (index < text.Length && text[index] >= '0' && text[index] <= '9') index++;
                bool integral = true;
                if (index < text.Length && text[index] == '.')
                {
                    integral = false; index++; Digits();
                }
                if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
                {
                    integral = false; index++;
                    if (index < text.Length && (text[index] == '+' || text[index] == '-')) index++;
                    Digits();
                }
                if (index - start > 64) throw new FormatException("JSON number is too long.");
                string token = text.Substring(start, index - start);
                long whole;
                if (integral && long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out whole)) return whole;
                double real;
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out real) || double.IsInfinity(real))
                    throw new FormatException("JSON number is out of range.");
                return real;
            }

            private void Digits()
            {
                int start = index;
                while (index < text.Length && text[index] >= '0' && text[index] <= '9') index++;
                if (start == index) throw new FormatException("Expected number digits.");
            }

            private string String()
            {
                index++;
                var output = new StringBuilder();
                while (index < text.Length)
                {
                    char c = text[index++];
                    if (c == '"')
                    {
                        string value = output.ToString();
                        try { Utf8.GetByteCount(value); }
                        catch (EncoderFallbackException) { throw new FormatException("Invalid Unicode escape."); }
                        return value;
                    }
                    if (c < 32) throw new FormatException("Unescaped control character.");
                    if (c != '\\') { output.Append(c); continue; }
                    if (index >= text.Length) break;
                    switch (text[index++])
                    {
                        case '"': output.Append('"'); break;
                        case '\\': output.Append('\\'); break;
                        case '/': output.Append('/'); break;
                        case 'b': output.Append('\b'); break;
                        case 'f': output.Append('\f'); break;
                        case 'n': output.Append('\n'); break;
                        case 'r': output.Append('\r'); break;
                        case 't': output.Append('\t'); break;
                        case 'u':
                            if (index + 4 > text.Length) throw new FormatException("Incomplete Unicode escape.");
                            int code;
                            if (!int.TryParse(text.Substring(index, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out code))
                                throw new FormatException("Invalid Unicode escape.");
                            output.Append((char)code); index += 4; break;
                        default: throw new FormatException("Invalid JSON escape.");
                    }
                }
                throw new FormatException("Unterminated JSON string.");
            }
        }
    }
}
