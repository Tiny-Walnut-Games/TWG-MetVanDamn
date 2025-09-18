using System;
using System.Collections.Generic;
using System.Text;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// Central data management for The Scribe - single source of truth
    /// KeeperNote: This is the "memory palace vault" - all data flows through here
    /// </summary>
    public class ScribeDataManager
        {
        // Document metadata
        public string Title { get; set; } = "";
        public string Author { get; set; } = "@copilot";
        public string Context { get; set; } = "";
        public string Summary { get; set; } = "";
        public string TagsCsv { get; set; } = "";

        // Section toggles
        public bool IncludeDiscoveries { get; set; } = true;
        public bool IncludeActions { get; set; } = true;
        public bool IncludeTechnicalDetails { get; set; } = true;
        public bool IncludeLessons { get; set; } = true;
        public bool IncludeNextSteps { get; set; } = true;

        // Content collections
        public List<Discovery> Discoveries { get; } = new List<Discovery>
        {
            new() { Category = "Discovery Category 1" }
        };

        public List<ActionItem> Actions { get; } = new List<ActionItem>
        {
            new() { Name = "Action 1" }
        };

        // Raw content
        public string RawMarkdown { get; set; } = "";
        public bool RawIsDirty { get; set; } = false;

        // Settings
        public bool AutoSyncEnabled { get; set; } = false;
        public string RootPath { get; set; } = "";
        public string ActiveDirectory { get; set; } = "";

        // Events
        private static readonly Action NoOpDataChanged = () => { };
        public event Action OnDataChanged = NoOpDataChanged; // non-null default

        /// <summary>
        /// Generate markdown from current form data
        /// </summary>
        public string GenerateMarkdown()
            {
            var generator = new ScribeMarkdownGenerator(this);
            return generator.Generate();
            }

        /// <summary>
        /// Parse markdown to populate form fields
        /// </summary>
        public void ParseMarkdown(string markdown)
            {
            var parser = new ScribeMarkdownParser(this);
            parser.Parse(markdown);
            OnDataChanged?.Invoke();
            }

        /// <summary>
        /// Sync form data to raw markdown
        /// </summary>
        public void SyncFormToRaw()
            {
            if (!RawIsDirty)
                {
                RawMarkdown = GenerateMarkdown();
                OnDataChanged?.Invoke();
                }
            }
        }

    [Serializable]
    public class Discovery
        {
        public string Category = "";
        public string KeyFinding = "";
        public string Impact = "";
        public string Evidence = "";
        public string RootCause = "";
        public string PatternRecognition = "";
        public bool Foldout = true;
        }

    [Serializable]
    public class ActionItem
        {
        public string Name = "";
        public string Description = "";
        public string FilesChanged = "";
        public string CommandsExecuted = "";
        public string Results = "";
        public bool Foldout = true;

        // Legacy fields for backward compatibility
        public string What = "";
        public string Why = "";
        public string How = "";
        public string Result = "";
        public string Validation = "";
        }
    }
