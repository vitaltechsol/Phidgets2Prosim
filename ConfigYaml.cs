using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Phidgets2Prosim
{
    /// <summary>
    /// Writes config entries compactly (omitting nulls / values equal to the class default) and edits config.yaml
    /// entry-by-entry, leaving every other line (comments, formatting, untouched entries) exactly as it was.
    /// </summary>
    internal static class ConfigYaml
    {
        private static readonly ISerializer Plain = new SerializerBuilder().Build();

        // Always written so an entry stays easy to identify even when the values are 0
        private static readonly HashSet<string> AlwaysKeep = new HashSet<string>
        {
            "Name", "Serial", "HubPort", "Channel", "OutSerial", "OutHubPort", "OutChannel",
        };

        // ── Object -> tree (dictionaries / lists / scalars) ───────────────────────

        public static object ToTree(object value, bool omitDefaults)
        {
            if (value == null) return null;
            var t = value.GetType();
            if (t.IsEnum) return value.ToString();
            if (t.IsPrimitive || value is string || value is decimal) return value;

            if (value is IDictionary dict)
            {
                var d = new Dictionary<string, object>();
                foreach (DictionaryEntry e in dict) d[Convert.ToString(e.Key)] = ToTree(e.Value, omitDefaults);
                return d;
            }
            if (value is IEnumerable en)
            {
                var l = new List<object>();
                foreach (var x in en) l.Add(ToTree(x, omitDefaults));
                return l;
            }
            return ObjectTree(value, omitDefaults);
        }

        private static int Depth(Type t) { int d = 0; for (; t != null; t = t.BaseType) d++; return d; }

        private static bool IsEmpty(object tree) =>
            tree is ICollection c && c.Count == 0;

        private static string TreeStr(object tree) =>
            Plain.Serialize(new Dictionary<string, object> { ["v"] = tree });

        private static Dictionary<string, object> ObjectTree(object obj, bool omitDefaults)
        {
            var t = obj.GetType();
            object def = null;
            if (omitDefaults) { try { def = Activator.CreateInstance(t); } catch { } }

            var result = new Dictionary<string, object>();
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0 && p.Name != "Connection")
                         .OrderBy(p => Depth(p.DeclaringType)); // base-class identity fields first
            foreach (var p in props)
            {
                var tree = ToTree(p.GetValue(obj), omitDefaults);
                if (tree == null) continue;

                object defTree = def != null ? ToTree(p.GetValue(def), true) : null;
                if (IsEmpty(tree) && (!omitDefaults || defTree == null || IsEmpty(defTree))) continue;
                if (omitDefaults && !AlwaysKeep.Contains(p.Name) && defTree != null && TreeStr(tree) == TreeStr(defTree)) continue;
                result[p.Name] = tree;
            }
            return result;
        }

        private static string SerializeItem(object item) => Plain.Serialize(ToTree(item, true));

        // ── Whole document (fallback when an in-place edit is not possible) ───────

        private static bool IsListType(PropertyInfo p) =>
            p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>);

        private static List<string> RenderItem(string yaml, string indent)
        {
            var parts = yaml.TrimEnd('\n', '\r').Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            var lines = new List<string>();
            for (int i = 0; i < parts.Count; i++)
                lines.Add(i == 0 ? indent + "- " + parts[i] : indent + "  " + parts[i]);
            return lines;
        }

        public static string FullDocument(Config cfg)
        {
            var sb = new StringBuilder();
            foreach (var p in typeof(Config).GetProperties())
            {
                var value = p.GetValue(cfg);
                if (IsListType(p))
                {
                    var items = ((IEnumerable)value ?? new object[0]).Cast<object>().ToList();
                    if (items.Count == 0) continue;
                    sb.AppendLine(p.Name + ":");
                    foreach (var item in items)
                        foreach (var line in RenderItem(SerializeItem(item), "  ")) sb.AppendLine(line);
                    sb.AppendLine();
                }
                else
                {
                    var tree = ToTree(value, false);
                    if (tree == null) continue;
                    sb.Append(Plain.Serialize(new Dictionary<string, object> { [p.Name] = tree }));
                    sb.AppendLine();
                }
            }
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        // ── In-place entry edit ───────────────────────────────────────────────────

        /// <summary>True when the written text parses back to the same config (semantically).</summary>
        public static bool Verify(string yaml, Config expected)
        {
            try
            {
                var parsed = new DeserializerBuilder().IgnoreUnmatchedProperties().Build().Deserialize<Config>(yaml);
                return TreeStr(ObjectTree(parsed, true)) == TreeStr(ObjectTree(expected, true));
            }
            catch { return false; }
        }

        /// <summary>
        /// Applies the difference between <paramref name="orig"/> (what is on disk) and <paramref name="cfg"/> to the
        /// text by replacing / adding / removing only the changed list entries. Returns false when it cannot do so safely.
        /// </summary>
        public static bool TrySplice(string text, Config orig, Config cfg, out string result)
        {
            result = null;
            string nl = text.Contains("\r\n") ? "\r\n" : "\n";
            var lines = text.Replace("\r\n", "\n").Split('\n').ToList();

            foreach (var p in typeof(Config).GetProperties())
            {
                object o = p.GetValue(orig), n = p.GetValue(cfg);
                if (!IsListType(p))
                {
                    // Settings, trim wheel, parking brake: not edited entry-wise
                    if (TreeStr(ToTree(o, false)) != TreeStr(ToTree(n, false))) return false;
                    continue;
                }

                var os = ((IEnumerable)o ?? new object[0]).Cast<object>().Select(SerializeItem).ToList();
                var ns = ((IEnumerable)n ?? new object[0]).Cast<object>().Select(SerializeItem).ToList();

                // Apply the changed runs from the last to the first so earlier indices stay valid
                int count = os.Count;
                foreach (var h in DiffHunks(os, ns).OrderByDescending(x => x.OrigFrom))
                {
                    var added = ns.Skip(h.NewFrom).Take(h.NewTo - h.NewFrom).ToList();
                    if (!SpliceSection(lines, p.Name, count, h.OrigFrom, h.OrigTo, added)) return false;
                    count += added.Count - (h.OrigTo - h.OrigFrom);
                }
            }

            result = string.Join(nl, lines);
            return true;
        }

        // Runs of entries that differ between the two lists (longest-common-subsequence on the serialized entries)
        private static List<(int OrigFrom, int OrigTo, int NewFrom, int NewTo)> DiffHunks(List<string> a, List<string> b)
        {
            int n = a.Count, m = b.Count;
            var lcs = new int[n + 1, m + 1];
            for (int i = n - 1; i >= 0; i--)
                for (int j = m - 1; j >= 0; j--)
                    lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

            var hunks = new List<(int, int, int, int)>();
            int x = 0, y = 0;
            while (x < n || y < m)
            {
                if (x < n && y < m && a[x] == b[y]) { x++; y++; continue; }
                int x0 = x, y0 = y;
                while ((x < n || y < m) && !(x < n && y < m && a[x] == b[y]))
                {
                    if (y >= m || (x < n && lcs[x + 1, y] >= lcs[x, y + 1])) x++; else y++;
                }
                hunks.Add((x0, x, y0, y));
            }
            return hunks;
        }

        private static bool IsBlankOrComment(string line)
        {
            string t = line.Trim();
            return t.Length == 0 || t.StartsWith("#");
        }

        // Replaces original items [from, to) of a section with the rendered `added` items
        private static bool SpliceSection(List<string> lines, string name, int origCount, int from, int to, List<string> added)
        {
            int header = lines.FindIndex(l => Regex.IsMatch(l, "^" + Regex.Escape(name) + @":\s*(#.*)?$"));

            if (header < 0)
            {
                if (origCount != 0) return false;
                while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
                lines.Add("");
                lines.Add(name + ":");
                foreach (var item in added) lines.AddRange(RenderItem(item, "  "));
                lines.Add("");
                return true;
            }

            // section = following lines up to the next top-level key
            int end = lines.Count;
            for (int i = header + 1; i < lines.Count; i++)
            {
                string l = lines[i];
                if (l.Length > 0 && !char.IsWhiteSpace(l[0]) && l[0] != '#' && !l.StartsWith("- ") && l != "-") { end = i; break; }
            }

            var starts = new List<int>();
            string itemIndent = null;
            for (int i = header + 1; i < end; i++)
            {
                var m = Regex.Match(lines[i], @"^(\s*)-(\s|$)");
                if (!m.Success) continue;
                if (itemIndent == null) itemIndent = m.Groups[1].Value;
                if (m.Groups[1].Value == itemIndent) starts.Add(i);
            }
            if (starts.Count != origCount) return false;
            itemIndent = itemIndent ?? "  ";

            // item i spans its start up to the next item, minus trailing blank / comment lines (they belong to the next entry)
            var ranges = new List<(int Start, int End)>();
            for (int i = 0; i < starts.Count; i++)
            {
                int e = i + 1 < starts.Count ? starts[i + 1] : end;
                while (e - 1 > starts[i] && IsBlankOrComment(lines[e - 1])) e--;
                ranges.Add((starts[i], e));
            }

            var rendered = new List<string>();
            foreach (var item in added) rendered.AddRange(RenderItem(item, itemIndent));

            int insertAt;
            if (to > from)
            {
                insertAt = ranges[from].Start;
                lines.RemoveRange(ranges[from].Start, ranges[to - 1].End - ranges[from].Start);
            }
            else if (from == 0)
                insertAt = origCount == 0 ? header + 1 : starts[0];
            else
                insertAt = ranges[from - 1].End;

            lines.InsertRange(insertAt, rendered);
            return true;
        }
    }
}
