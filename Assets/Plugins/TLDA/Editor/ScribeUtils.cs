#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LivingDevAgent.Editor
{
    internal static class ScribeUtils
    {
        public static string SanitizeTitle(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Entry";
            }

            string safe = Regex.Replace(input.Trim(), "[^A-Za-z0-9_-]+", "");
            return string.IsNullOrEmpty(safe) ? "Entry" : safe;
        }
        public static string Bulletize(string lines)
        {
            if (string.IsNullOrEmpty(lines))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            using var reader = new StringReader(lines);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                sb.AppendLine("- " + line);
            }
            return sb.ToString().TrimEnd();
        }
        public static string Checklist(string lines)
        {
            if (string.IsNullOrEmpty(lines))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            using var reader = new StringReader(lines);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                sb.AppendLine("- [ ] " + line);
            }
            return sb.ToString().TrimEnd();
        }
        public static string FormatTags(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
            {
                return string.Empty;
            }

            string[] parts = csv.Split(',');
            var sb = new StringBuilder();
            foreach (string p in parts)
            {
                string t = p.Trim();
                if (t.Length == 0)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                sb.Append('#');
                sb.Append(Regex.Replace(t, "\n+", " ").Replace(' ', '-'));
            }
            return sb.ToString();
        }
    }
}
#endif
