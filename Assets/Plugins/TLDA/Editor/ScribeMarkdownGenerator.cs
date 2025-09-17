using System;
using System.Text;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// Markdown generation engine - converts form data to markdown
    /// KeeperNote: This is the "printing press" - transforms structured data into readable documentation
    /// </summary>
    public class ScribeMarkdownGenerator
        {
        private readonly ScribeDataManager _data;

        public ScribeMarkdownGenerator(ScribeDataManager data)
            {
            _data = data;
            }

        public string Generate()
            {
            var sb = new StringBuilder();

            GenerateHeader(sb);
            GenerateQuote(sb);

            if (_data.IncludeDiscoveries && _data.Discoveries.Count > 0)
                GenerateDiscoveries(sb);

            if (_data.IncludeActions && _data.Actions.Count > 0)
                GenerateActions(sb);

            if (_data.IncludeTechnicalDetails)
                GenerateTechnicalDetails(sb);

            if (_data.IncludeLessons)
                GenerateLessons(sb);

            if (_data.IncludeNextSteps)
                GenerateNextSteps(sb);

            GenerateMetadata(sb);

            return sb.ToString();
            }

        void GenerateHeader(StringBuilder sb)
            {
            string date = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss-utc");
            string safeTitle = SanitizeTitle(_data.Title);

            sb.AppendLine("# TLDL Entry Template");
            sb.AppendLine($"**Entry ID:** TLDL-{date}-{timestamp}-{safeTitle}");
            sb.AppendLine($"**Author:** {_data.Author}");
            sb.AppendLine($"**Context:** {_data.Context}");
            sb.AppendLine($"**Summary:** {_data.Summary}");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            }

        void GenerateQuote(StringBuilder sb)
            {
            sb.AppendLine("> 📜 \"[Insert inspirational quote from Secret Art of the Living Dev]\"");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            }

        void GenerateDiscoveries(StringBuilder sb)
            {
            sb.AppendLine("## Discoveries");
            sb.AppendLine();

            foreach (Discovery discovery in _data.Discoveries)
                {
                string heading = string.IsNullOrWhiteSpace(discovery.Category)
                    ? "[Discovery]"
                    : $"[{discovery.Category}]";

                sb.AppendLine($"### {heading}");

                if (!string.IsNullOrWhiteSpace(discovery.KeyFinding))
                    sb.AppendLine($"- **Key Finding**: {discovery.KeyFinding}");

                if (!string.IsNullOrWhiteSpace(discovery.Impact))
                    sb.AppendLine($"- **Impact**: {discovery.Impact}");

                if (!string.IsNullOrWhiteSpace(discovery.Evidence))
                    sb.AppendLine($"- **Evidence**: {discovery.Evidence}");

                if (!string.IsNullOrWhiteSpace(discovery.RootCause))
                    sb.AppendLine($"- **Root Cause**: {discovery.RootCause}");

                if (!string.IsNullOrWhiteSpace(discovery.PatternRecognition))
                    sb.AppendLine($"- **Pattern Recognition**: {discovery.PatternRecognition}");

                sb.AppendLine();
                }
            }

        void GenerateActions(StringBuilder sb)
            {
            sb.AppendLine("## Actions Taken");
            sb.AppendLine();

            for (int i = 0; i < _data.Actions.Count; i++)
                {
                ActionItem action = _data.Actions[i];
                int idx = i + 1;

                sb.AppendLine($"{idx}. **[{(string.IsNullOrWhiteSpace(action.Name) ? $"Action {idx}" : action.Name)}]**");

                if (!string.IsNullOrWhiteSpace(action.What))
                    sb.AppendLine($"   - **What**: {action.What}");

                if (!string.IsNullOrWhiteSpace(action.Why))
                    sb.AppendLine($"   - **Why**: {action.Why}");

                if (!string.IsNullOrWhiteSpace(action.How))
                    sb.AppendLine($"   - **How**: {action.How}");

                if (!string.IsNullOrWhiteSpace(action.Result))
                    sb.AppendLine($"   - **Result**: {action.Result}");

                if (!string.IsNullOrWhiteSpace(action.FilesChanged))
                    sb.AppendLine($"   - **Files Changed**: {action.FilesChanged}");

                if (!string.IsNullOrWhiteSpace(action.Validation))
                    sb.AppendLine($"   - **Validation**: {action.Validation}");

                sb.AppendLine();
                }
            }

        void GenerateTechnicalDetails(StringBuilder sb)
            {
            sb.AppendLine("## Technical Details");
            sb.AppendLine();
            // Add code changes, config updates, etc.
            }

        void GenerateLessons(StringBuilder sb)
            {
            sb.AppendLine("## Lessons Learned");
            sb.AppendLine();
            // Add lessons content
            }

        void GenerateNextSteps(StringBuilder sb)
            {
            sb.AppendLine("## Next Steps");
            sb.AppendLine();
            // Add next steps content
            }

        void GenerateMetadata(StringBuilder sb)
            {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## TLDL Metadata");

            string tags = FormatTags(_data.TagsCsv);
            if (!string.IsNullOrWhiteSpace(tags))
                sb.AppendLine($"**Tags**: {tags}");

            sb.AppendLine($"**Created**: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss 'UTC'}");
            sb.AppendLine($"**Status**: Draft");
            }

        string SanitizeTitle(string title)
            {
            if (string.IsNullOrWhiteSpace(title))
                return "Entry";

            // Remove invalid filename characters
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            string result = title;

            foreach (char c in invalid)
                {
                result = result.Replace(c.ToString(), "");
                }

            return result.Replace(" ", "-");
            }

        string FormatTags(string csv)
            {
            if (string.IsNullOrWhiteSpace(csv))
                return "";

            string[] tags = csv.Split(',');
            var formatted = new StringBuilder();

            foreach (string tag in tags)
                {
                string trimmed = tag.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    {
                    formatted.Append($"#{trimmed} ");
                    }
                }

            return formatted.ToString().TrimEnd();
            }
        }
    }
