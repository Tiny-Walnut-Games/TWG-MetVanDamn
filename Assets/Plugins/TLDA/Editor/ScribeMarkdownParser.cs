using System;
using System.IO;
using System.Text.RegularExpressions;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// 🔧 ENHANCEMENT READY - Markdown parser to extract form data from existing documents
    /// Current: Basic regex-based TLDL parsing with field extraction
    /// Enhancement path: AST parsing, natural language processing, AI-powered content analysis
    /// Sacred Symbol Preservation: This is the "decoder ring" - reconstructs form data from markdown tomb!
    /// KeeperNote: This is the "decoder ring" - reconstructs form data from markdown
    /// </summary>
    public class ScribeMarkdownParser
        {
        private readonly ScribeDataManager _data;

        public ScribeMarkdownParser(ScribeDataManager data)
            {
            _data = data;
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Main parsing orchestrator
        /// Current: Sequential section parsing with exception handling
        /// Enhancement path: Parallel parsing, semantic analysis, multi-format support
        /// </summary>
        public void Parse(string markdown)
            {
            if (string.IsNullOrEmpty(markdown))
                return;

            try
                {
                // 🍑 Cheek-preserving parsing - extract what we can, gracefully handle missing sections
                ParseMetadata(markdown);
                ParseDiscoveries(markdown);
                ParseActions(markdown);
                // 🔧 ENHANCEMENT READY - Add technical details, lessons, next steps parsing
                }
            catch (Exception e)
                {
                UnityEngine.Debug.LogWarning($"[ScribeParser] Error parsing markdown: {e.Message}");
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - TLDL metadata extraction
        /// Current: Line-by-line regex matching for standard TLDL fields
        /// Enhancement path: YAML front-matter parsing, smart field inference, multi-format headers
        /// </summary>
        void ParseMetadata(string markdown)
            {
            using var reader = new StringReader(markdown);
            string line;
            while ((line = reader.ReadLine()) != null)
                {
                // Title from header
                if (line.StartsWith("# ") && string.IsNullOrEmpty(_data.Title))
                    {
                    _data.Title = line[2..].Trim();
                    }

                // Author
                if (line.StartsWith("**Author:**"))
                    {
                    string value = line["**Author:**".Length..].Trim();
                    if (!string.IsNullOrEmpty(value))
                        _data.Author = value;
                    }

                // Context
                if (line.StartsWith("**Context:**"))
                    {
                    string value = line["**Context:**".Length..].Trim();
                    if (!string.IsNullOrEmpty(value))
                        _data.Context = value;
                    }

                // Summary
                if (line.StartsWith("**Summary:**"))
                    {
                    string value = line["**Summary:**".Length..].Trim();
                    if (!string.IsNullOrEmpty(value))
                        _data.Summary = value;
                    }

                // Tags
                if (line.StartsWith("**Tags:"))
                    {
                    Match match = Regex.Match(line, @"\*\*Tags:\*\*\s*(.+)");
                    if (match.Success)
                        {
                        string tags = match.Groups[1].Value;
                        // Convert from #tag format to CSV
                        tags = tags.Replace("#", "").Replace("  ", " ").Trim();
                        _data.TagsCsv = string.Join(",", tags.Split(' '));
                        }
                    }
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Discovery section parsing with structured field extraction
        /// Current: Regex-based section extraction with manual field mapping
        /// Enhancement path: Machine learning categorization, impact scoring, evidence validation
        /// </summary>
        void ParseDiscoveries(string markdown)
            {
            // KeeperNote: Complex parsing - extracts structured discovery data from markdown sections
            Match discoveriesMatch = Regex.Match(
                markdown,
                @"## Discoveries\s*\n(.*?)(?=\n## |\z)",
                RegexOptions.Singleline
            );

            if (!discoveriesMatch.Success)
                return;

            string discoveriesSection = discoveriesMatch.Groups[1].Value;

            // Clear existing and parse new
            _data.Discoveries.Clear();

            // Match discovery subsections
            MatchCollection discoveryMatches = Regex.Matches(
                discoveriesSection,
                @"### \[([^\]]+)\](.*?)(?=\n### |\z)",
                RegexOptions.Singleline
            );

            foreach (Match match in discoveryMatches)
                {
                var discovery = new Discovery
                    {
                    Category = match.Groups[1].Value.Trim()
                    };

                string content = match.Groups[2].Value;

                // Extract fields using Sacred Field Extraction Protocol
                ExtractField(content, "Key Finding", out discovery.KeyFinding);
                ExtractField(content, "Impact", out discovery.Impact);
                ExtractField(content, "Evidence", out discovery.Evidence);
                ExtractField(content, "Root Cause", out discovery.RootCause);
                ExtractField(content, "Pattern Recognition", out discovery.PatternRecognition);

                _data.Discoveries.Add(discovery);
                }

            // 🍑 Cheek-preserving fallback - ensure at least one discovery exists for form stability
            if (_data.Discoveries.Count == 0)
                {
                _data.Discoveries.Add(new Discovery { Category = "Discovery 1" });
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Action item parsing with workflow analysis
        /// Current: Numbered list parsing with field extraction
        /// Enhancement path: Dependency analysis, result validation, automated workflow generation
        /// </summary>
        void ParseActions(string markdown)
            {
            Match actionsMatch = Regex.Match(
                markdown,
                @"## Actions Taken\s*\n(.*?)(?=\n## |\z)",
                RegexOptions.Singleline
            );

            if (!actionsMatch.Success)
                return;

            string actionsSection = actionsMatch.Groups[1].Value;

            // Clear existing and parse new
            _data.Actions.Clear();

            // Match numbered action items
            MatchCollection actionMatches = Regex.Matches(
                actionsSection,
                @"\d+\.\s*\*\*\[([^\]]+)\]\*\*(.*?)(?=\n\d+\.|\z)",
                RegexOptions.Singleline
            );

            foreach (Match match in actionMatches)
                {
                var action = new ActionItem
                    {
                    Name = match.Groups[1].Value.Trim()
                    };

                string content = match.Groups[2].Value;

                // Extract action workflow fields
                ExtractField(content, "What", out action.What);
                ExtractField(content, "Why", out action.Why);
                ExtractField(content, "How", out action.How);
                ExtractField(content, "Result", out action.Result);
                ExtractField(content, "Files Changed", out action.FilesChanged);
                ExtractField(content, "Validation", out action.Validation);

                _data.Actions.Add(action);
                }

            // 🍑 Cheek-preserving fallback - ensure at least one action exists for form stability
            if (_data.Actions.Count == 0)
                {
                _data.Actions.Add(new ActionItem { Name = "Action 1" });
                }
            }

        /// <summary>
        /// 🔧 ENHANCEMENT READY - Sacred Field Extraction Protocol
        /// Current: Simple regex pattern matching for bold field labels
        /// Enhancement path: Natural language processing, context-aware extraction, fuzzy matching
        /// </summary>
        bool ExtractField(string content, string fieldName, out string value)
            {
            string pattern = $@"-\s*\*\*{fieldName}\*\*:\s*(.+?)(?=\n\s*-|\z)";
            Match match = Regex.Match(content, pattern, RegexOptions.Singleline);

            if (match.Success)
                {
                value = match.Groups[1].Value.Trim();
                return true;
                }

            value = "";
            return false;
            }
        }
    }
