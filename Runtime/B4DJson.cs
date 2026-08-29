using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace B4D
{
    /// Minimal JSON reader and writer. Unity's JsonUtility cannot tell an absent
    /// field from a zero one, and campaign JSON leans on that difference: a zone
    /// with no "roof" key is open to the sky, a zone with "roof": 0 is not the
    /// same thing. So the kit does its own reading and writing.
    public static class B4DJson
    {
        // ---------- writing ----------

        public static string Write(object value, bool pretty = true)
        {
            var sb = new StringBuilder();
            WriteValue(sb, value, pretty, 0);
            return sb.ToString();
        }

        static void WriteValue(StringBuilder sb, object v, bool pretty, int depth)
        {
            switch (v)
            {
                case null: sb.Append("null"); return;
                case string s: WriteString(sb, s); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case float f: sb.Append(Num(f)); return;
                case double d: sb.Append(Num((float)d)); return;
                case IDictionary dict: WriteObject(sb, dict, pretty, depth); return;
                case IEnumerable list: WriteArray(sb, list, pretty, depth); return;
                default: WriteString(sb, v.ToString()); return;
            }
        }

        /// Trims float noise so exported maps diff cleanly between saves.
        static string Num(float f)
        {
            var rounded = (float)Math.Round(f, 4);
            if (Math.Abs(rounded - Math.Round(rounded)) < 1e-6f) return ((long)Math.Round(rounded)).ToString(CultureInfo.InvariantCulture);
            return rounded.ToString("0.####", CultureInfo.InvariantCulture);
        }

        static void WriteObject(StringBuilder sb, IDictionary dict, bool pretty, int depth)
        {
            if (dict.Count == 0) { sb.Append("{}"); return; }
            sb.Append('{');
            var first = true;
            foreach (DictionaryEntry e in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                if (pretty) { sb.Append('\n'); Indent(sb, depth + 1); }
                WriteString(sb, e.Key.ToString());
                sb.Append(pretty ? ": " : ":");
                WriteValue(sb, e.Value, pretty, depth + 1);
            }
            if (pretty) { sb.Append('\n'); Indent(sb, depth); }
            sb.Append('}');
        }

        static void WriteArray(StringBuilder sb, IEnumerable list, bool pretty, int depth)
        {
            var items = new List<object>();
            foreach (var item in list) items.Add(item);
            if (items.Count == 0) { sb.Append("[]"); return; }
            // Short all-number arrays (positions, collider extents) stay on one line.
            var flat = items.TrueForAll(x => x is float || x is int || x is double || x is long);
            sb.Append('[');
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append(flat ? ", " : ",");
                if (pretty && !flat) { sb.Append('\n'); Indent(sb, depth + 1); }
                WriteValue(sb, items[i], pretty, depth + 1);
            }
            if (pretty && !flat) { sb.Append('\n'); Indent(sb, depth); }
            sb.Append(']');
        }

        static void Indent(StringBuilder sb, int depth) => sb.Append(' ', depth * 2);

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---------- reading ----------

        public static object Parse(string text)
        {
            var pos = 0;
            var value = ParseValue(text, ref pos);
            SkipWhitespace(text, ref pos);
            if (pos < text.Length) throw new FormatException($"trailing characters at {pos}");
            return value;
        }

        static object ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) throw new FormatException("unexpected end of JSON");
            switch (s[i])
            {
                case '{': return ParseObject(s, ref i);
                case '[': return ParseArray(s, ref i);
                case '"': return ParseString(s, ref i);
                case 't': Expect(s, ref i, "true"); return true;
                case 'f': Expect(s, ref i, "false"); return false;
                case 'n': Expect(s, ref i, "null"); return null;
                default: return ParseNumber(s, ref i);
            }
        }

        static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var result = new Dictionary<string, object>();
            i++; // {
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return result; }
            while (true)
            {
                SkipWhitespace(s, ref i);
                var key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                if (s[i] != ':') throw new FormatException($"expected ':' at {i}");
                i++;
                result[key] = ParseValue(s, ref i);
                SkipWhitespace(s, ref i);
                if (i >= s.Length) throw new FormatException("unterminated object");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return result; }
                throw new FormatException($"expected ',' or '}}' at {i}");
            }
        }

        static List<object> ParseArray(string s, ref int i)
        {
            var result = new List<object>();
            i++; // [
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return result; }
            while (true)
            {
                result.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (i >= s.Length) throw new FormatException("unterminated array");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return result; }
                throw new FormatException($"expected ',' or ']' at {i}");
            }
        }

        static string ParseString(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException($"expected string at {i}");
            i++;
            var sb = new StringBuilder();
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    switch (s[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; break;
                        default: sb.Append(s[i]); break;
                    }
                }
                else sb.Append(s[i]);
                i++;
            }
            i++;
            return sb.ToString();
        }

        static double ParseNumber(string s, ref int i)
        {
            var start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }

        static void Expect(string s, ref int i, string word)
        {
            if (i + word.Length > s.Length || s.Substring(i, word.Length) != word)
                throw new FormatException($"expected '{word}' at {i}");
            i += word.Length;
        }

        static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        // ---------- typed access helpers ----------

        public static Dictionary<string, object> Obj(object v) => v as Dictionary<string, object>;
        public static List<object> Arr(object v) => v as List<object>;

        public static bool Has(Dictionary<string, object> o, string key) => o != null && o.ContainsKey(key) && o[key] != null;

        public static float F(Dictionary<string, object> o, string key, float fallback = 0f)
            => Has(o, key) ? Convert.ToSingle(o[key], CultureInfo.InvariantCulture) : fallback;

        public static int I(Dictionary<string, object> o, string key, int fallback = 0)
            => Has(o, key) ? Convert.ToInt32(o[key], CultureInfo.InvariantCulture) : fallback;

        public static string S(Dictionary<string, object> o, string key, string fallback = "")
            => Has(o, key) ? o[key].ToString() : fallback;

        public static bool B(Dictionary<string, object> o, string key, bool fallback)
            => Has(o, key) ? Convert.ToBoolean(o[key]) : fallback;
    }
}
