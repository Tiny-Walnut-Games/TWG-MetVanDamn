#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
{
    /// <summary>
    /// 📊 Centralized data model for TLDL Scribe Window
    /// Contains all form data, UI state, and configuration for the modular dashboard
    /// </summary>
    [Serializable]
    public class TLDLScribeData
    {
        // File & path management
        public string CurrentFilePath = "";
        public string RootPath = "";
        public string ActiveDirPath = "";

        // Header fields
        public string Title = "";
        public string Author = "@copilot";
        public string Context = "";
        public string Summary = "";
        public string TagsCsv = "";

        // Section toggle flags
        public bool IncludeDiscoveries = true;
        public bool IncludeActions = true;
        public bool IncludeTechnicalDetails = false;
        public bool IncludeTerminalProof = false;
        public bool IncludeDependencies = false;
        public bool IncludeLessons = true;
        public bool IncludeNextSteps = true;
        public bool IncludeReferences = false;
        public bool IncludeDevTimeTravel = false;
        public bool IncludeMetadata = true;
        public bool IncludeImages = false;

        // Section content fields (strings for text areas)
        public string DiscoveriesText = "";
        public string ActionsTaken = "";
        public string TechnicalDetails = "";
        public string TerminalProof = "";
        public string Dependencies = "";
        public string LessonsLearned = "";
        public string NextSteps = "";
        public string References = "";
        public string DevTimeTravel = "";
        public string ImagePaths = "";
        public string CustomMetadata = "";

        // Legacy compatibility for existing Discoveries list
        public List<Discovery> Discoveries = new();

        // Metadata enum fields
        public ComplexityLevel Complexity = ComplexityLevel.Medium;
        public ImpactLevel Impact = ImpactLevel.Medium;
        public string Duration = "";
        public string TeamMembers = "";

        // 🕐 NEW: Time Tracking System
        public bool IsTimerActive = false;
        public System.DateTime SessionStartTime = System.DateTime.MinValue;
        public double TotalSessionMinutes = 0.0;
        public List<TimeSession> TimeSessions = new();
        public string ActiveTaskDescription = "";

        // Raw editor content
        public string RawContent = "";
        public string RawGeneratedSnapshot = "";
        public bool RawDirty = false;

        // Raw editor UI state
        public bool RawWrap = true;
        public Vector2 RawScroll = Vector2.zero;
        public string RawEditorControlName = "TLDL_RawEditor";
        public int RawCursorIndex = 0;
        public bool PendingScrollToCursor = false;

        // Navigator UI state
        public Vector2 NavScroll = Vector2.zero;
        public Dictionary<string, bool> FolderExpanded = new();
        public Dictionary<string, Texture2D> ImageCache = new();
        public List<string> ImageCacheOrder = new();

        // Auto-sync tracking
        public bool AutoSyncEditor = false;
        public string LastFormSnapshot = "";

        // UI state
        public Vector2 FormScroll = Vector2.zero;
        public Vector2 NavigatorScroll = Vector2.zero;
        public Vector2 EditorScroll = Vector2.zero;
        public Vector2 PreviewScroll = Vector2.zero;
        
        // Tab management
        public int SelectedTab = 0; // 0=Form, 1=Editor, 2=Preview, 3=TaskMaster

        // Template system
        public List<TemplateInfo> Templates;
        public int SelectedTemplateIndex = 0;

        // Status and messaging
        public string StatusMessage = "Ready to begin your documentation quest!";
        public float StatusTimestamp = 0f;

        // Constants for UI styling
        public static readonly float StatusDisplayDuration = 5f;
        public static readonly int MaxStatusLength = 200;
        
        // Navigator constants
        public static readonly string EditorPrefsRootKey = "TLDL_Scribe_RootPath";
        public static readonly int ImageCacheMax = 50;
        public static readonly HashSet<string> AllowedExts = new() { ".md", ".txt", ".log", ".xml", ".json", ".yaml", ".yml" };
        public static readonly HashSet<string> ImageExts = new() { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tga" };
    }

    /// <summary>
    /// Template information for the template system
    /// </summary>
    [Serializable]
    public class TemplateInfo
    {
        public string Key;
        public string Title;
        public string File;
        public string AbsPath;
    }

    /// <summary>
    /// Discovery entry for backwards compatibility
    /// </summary>
    [Serializable]
    public class Discovery
    {
        public string Title;
        public string Content;
        public string Category;
    }

    /// <summary>
    /// Complexity levels for metadata
    /// </summary>
    public enum ComplexityLevel
    {
        Low,
        Medium,
        High,
        Epic
    }

    /// <summary>
    /// Impact levels for metadata
    /// </summary>
    public enum ImpactLevel
    {
        Minor,
        Medium,
        Major,
        Critical
    }

    /// <summary>
    /// 🕐 Time tracking session for productivity metrics
    /// </summary>
    [System.Serializable]
    public class TimeSession
    {
        public System.DateTime StartTime;
        public System.DateTime EndTime;
        public double DurationMinutes;
        public string TaskDescription;
        public string SessionNotes;
        
        public TimeSession(System.DateTime startTime, string taskDescription = "")
        {
            StartTime = startTime;
            TaskDescription = taskDescription;
        }
        
        public void EndSession(string notes = "")
        {
            EndTime = System.DateTime.Now;
            DurationMinutes = (EndTime - StartTime).TotalMinutes;
            SessionNotes = notes;
        }
        
        public override string ToString()
        {
            return $"{StartTime:HH:mm} - {EndTime:HH:mm} ({DurationMinutes:F1}m): {TaskDescription}";
        }
    }
}
#endif
