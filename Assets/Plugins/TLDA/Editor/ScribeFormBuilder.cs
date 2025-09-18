using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LivingDevAgent.Editor.Scribe
    {
    /// <summary>
    /// 🔧 ENHANCEMENT READY - Complete form-based documentation builder with all sections
    /// Current: Full implementation of structured TLDL entry creation
    /// Enhancement path: Template integration, validation, export formats
    /// Sacred Mission: Transform complex documentation into intuitive form-based workflow!
    /// </summary>
    public class ScribeFormBuilder
        {
        private readonly ScribeDataManager _data;
        private readonly ScribeImageManager _images;
        private Vector2 _scrollPosition;

        // Events
        private static readonly System.Action NoOpContentChanged = () => { };
        public event System.Action OnContentChanged = NoOpContentChanged; // non-null default

        // Styles
        private GUIStyle _textAreaWrap;
        private GUIStyle _labelWrap;
        private GUIStyle _sectionStyle;
        private GUIStyle _addButtonStyle;
        private bool _stylesInitialized = false;

        public ScribeFormBuilder(ScribeDataManager data, ScribeImageManager images)
            {
            _data = data;
            _images = images;
            // 🔧 CRITICAL FIX: Don't initialize styles in constructor - Unity isn't ready yet!
            // InitializeStyles() will be called lazily when Draw() is first called
            }

        void InitializeStyles()
            {
            // 🔧 LEGENDARY FIX: Lazy initialization prevents NullReferenceException
            // Unity's EditorStyles aren't available during constructor time!
            if (_stylesInitialized) return;

            try
                {
                _textAreaWrap = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                _labelWrap = new GUIStyle(EditorStyles.label) { wordWrap = true };

                _sectionStyle = new GUIStyle("box")
                    {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                    };

                _addButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                    };

                _stylesInitialized = true;
                }
            catch (System.Exception e)
                {
                // 🛡️ Bootstrap Sentinel Cheek Preservation: Graceful fallback if styles fail
                Debug.LogWarning($"[ScribeFormBuilder] Style initialization failed, using defaults: {e.Message}");

                // Create basic fallback styles
                _textAreaWrap = new GUIStyle() { wordWrap = true };
                _labelWrap = new GUIStyle() { wordWrap = true };
                _sectionStyle = new GUIStyle();
                _addButtonStyle = new GUIStyle();

                _stylesInitialized = true;
                }
            }

        public void Draw()
            {
            // 🔧 LEGENDARY FIX: Initialize styles on first draw call when Unity is ready
            InitializeStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                DrawHeader();
                DrawSections();
                DrawFooter();
                }
            EditorGUILayout.EndScrollView();
            }

        void DrawHeader()
            {
            EditorGUILayout.BeginVertical(_sectionStyle);
                {
                GUILayout.Label("📋 Document Header", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                    {
                    _data.Title = EditorGUILayout.TextField("Title", _data.Title ?? "");
                    _data.Author = EditorGUILayout.TextField("Author", _data.Author ?? "");

                    GUILayout.Label("Context", EditorStyles.miniLabel);
                    _data.Context = EditorGUILayout.TextArea(_data.Context ?? "", _textAreaWrap, GUILayout.MinHeight(60));

                    GUILayout.Label("Summary", EditorStyles.miniLabel);
                    _data.Summary = EditorGUILayout.TextArea(_data.Summary ?? "", _textAreaWrap, GUILayout.MinHeight(60));

                    _data.TagsCsv = EditorGUILayout.TextField("Tags (CSV)", _data.TagsCsv ?? "");
                    }
                if (EditorGUI.EndChangeCheck())
                    {
                    OnContentChanged?.Invoke();
                    }
                }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            }

        void DrawSections()
            {
            EditorGUILayout.BeginVertical(_sectionStyle);
                {
                GUILayout.Label("📑 Document Sections", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                    {
                    _data.IncludeDiscoveries = EditorGUILayout.ToggleLeft("🔍 Include Discoveries", _data.IncludeDiscoveries);
                    _data.IncludeActions = EditorGUILayout.ToggleLeft("⚡ Include Actions", _data.IncludeActions);
                    _data.IncludeTechnicalDetails = EditorGUILayout.ToggleLeft("🔧 Include Technical Details", _data.IncludeTechnicalDetails);
                    _data.IncludeLessons = EditorGUILayout.ToggleLeft("📚 Include Lessons Learned", _data.IncludeLessons);
                    _data.IncludeNextSteps = EditorGUILayout.ToggleLeft("🎯 Include Next Steps", _data.IncludeNextSteps);
                    }
                if (EditorGUI.EndChangeCheck())
                    {
                    OnContentChanged?.Invoke();
                    }
                }
            EditorGUILayout.EndVertical();

            // Draw each enabled section
            if (_data.IncludeDiscoveries) DrawDiscoveries();
            if (_data.IncludeActions) DrawActions();
            if (_data.IncludeTechnicalDetails) DrawTechnicalDetails();
            if (_data.IncludeLessons) DrawLessons();
            if (_data.IncludeNextSteps) DrawNextSteps();
            }

        void DrawDiscoveries()
            {
            EditorGUILayout.BeginVertical(_sectionStyle);
                {
                EditorGUILayout.BeginHorizontal();
                    {
                    GUILayout.Label("🔍 Discoveries", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("➕ Add Discovery", _addButtonStyle, GUILayout.Width(120)))
                        {
                        _data.Discoveries.Add(new Discovery
                            {
                            Category = $"Discovery {_data.Discoveries.Count + 1}",
                            Foldout = true
                            });
                        OnContentChanged?.Invoke();
                        }
                    }
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < _data.Discoveries.Count; i++)
                    {
                    Discovery discovery = _data.Discoveries[i];

                    EditorGUILayout.BeginVertical("frameBox");
                        {
                        EditorGUILayout.BeginHorizontal();
                            {
                            discovery.Foldout = EditorGUILayout.Foldout(
                                discovery.Foldout,
                                string.IsNullOrEmpty(discovery.Category)
                                    ? $"🔍 Discovery {i + 1}"
                                    : $"🔍 {discovery.Category}",
                                true
                            );

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("📋", EditorStyles.miniButton, GUILayout.Width(25)))
                                {
                                Discovery clone = CloneDiscovery(discovery);
                                _data.Discoveries.Insert(i + 1, clone);
                                OnContentChanged?.Invoke();
                                break;
                                }

                            if (GUILayout.Button("❌", EditorStyles.miniButton, GUILayout.Width(25)))
                                {
                                _data.Discoveries.RemoveAt(i);
                                OnContentChanged?.Invoke();
                                break;
                                }
                            }
                        EditorGUILayout.EndHorizontal();

                        if (discovery.Foldout)
                            {
                            EditorGUI.BeginChangeCheck();
                                {
                                discovery.Category = EditorGUILayout.TextField("Category", discovery.Category ?? "");

                                GUILayout.Label("Key Finding", EditorStyles.miniLabel);
                                discovery.KeyFinding = EditorGUILayout.TextArea(discovery.KeyFinding ?? "", _textAreaWrap, GUILayout.MinHeight(50));

                                GUILayout.Label("Impact", EditorStyles.miniLabel);
                                discovery.Impact = EditorGUILayout.TextArea(discovery.Impact ?? "", _textAreaWrap, GUILayout.MinHeight(50));

                                GUILayout.Label("Evidence", EditorStyles.miniLabel);
                                discovery.Evidence = EditorGUILayout.TextArea(discovery.Evidence ?? "", _textAreaWrap, GUILayout.MinHeight(50));

                                GUILayout.Label("Root Cause (optional)", EditorStyles.miniLabel);
                                discovery.RootCause = EditorGUILayout.TextArea(discovery.RootCause ?? "", _textAreaWrap, GUILayout.MinHeight(40));

                                GUILayout.Label("Pattern Recognition (optional)", EditorStyles.miniLabel);
                                discovery.PatternRecognition = EditorGUILayout.TextArea(discovery.PatternRecognition ?? "", _textAreaWrap, GUILayout.MinHeight(40));
                                }
                            if (EditorGUI.EndChangeCheck())
                                {
                                OnContentChanged?.Invoke();
                                }
                            }
                        }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(3);
                    }

                if (_data.Discoveries.Count == 0)
                    {
                    EditorGUILayout.HelpBox("No discoveries yet. Click 'Add Discovery' to begin documenting your findings.", MessageType.Info);
                    }
                }
            EditorGUILayout.EndVertical();
            }

        void DrawActions()
            {
            EditorGUILayout.BeginVertical(_sectionStyle);
                {
                EditorGUILayout.BeginHorizontal();
                    {
                    GUILayout.Label("⚡ Actions Taken", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("➕ Add Action", _addButtonStyle, GUILayout.Width(100)))
                        {
                        _data.Actions.Add(new ActionItem
                            {
                            Name = $"Action {_data.Actions.Count + 1}",
                            Foldout = true
                            });
                        OnContentChanged?.Invoke();
                        }
                    }
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < _data.Actions.Count; i++)
                    {
                    ActionItem action = _data.Actions[i];

                    EditorGUILayout.BeginVertical("frameBox");
                        {
                        EditorGUILayout.BeginHorizontal();
                            {
                            action.Foldout = EditorGUILayout.Foldout(
                                action.Foldout,
                                string.IsNullOrEmpty(action.Name)
                                    ? $"⚡ Action {i + 1}"
                                    : $"⚡ {action.Name}",
                                true
                            );

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("📋", EditorStyles.miniButton, GUILayout.Width(25)))
                                {
                                ActionItem clone = CloneAction(action);
                                _data.Actions.Insert(i + 1, clone);
                                OnContentChanged?.Invoke();
                                break;
                                }

                            if (GUILayout.Button("❌", EditorStyles.miniButton, GUILayout.Width(25)))
                                {
                                _data.Actions.RemoveAt(i);
                                OnContentChanged?.Invoke();
                                break;
                                }
                            }
                        EditorGUILayout.EndHorizontal();

                        if (action.Foldout)
                            {
                            EditorGUI.BeginChangeCheck();
                                {
                                action.Name = EditorGUILayout.TextField("Action Name", action.Name ?? "");

                                GUILayout.Label("Description", EditorStyles.miniLabel);
                                action.Description = EditorGUILayout.TextArea(action.Description ?? "", _textAreaWrap, GUILayout.MinHeight(50));

                                GUILayout.Label("Files Changed", EditorStyles.miniLabel);
                                action.FilesChanged = EditorGUILayout.TextArea(action.FilesChanged ?? "", _textAreaWrap, GUILayout.MinHeight(40));

                                GUILayout.Label("Commands Executed", EditorStyles.miniLabel);
                                action.CommandsExecuted = EditorGUILayout.TextArea(action.CommandsExecuted ?? "", _textAreaWrap, GUILayout.MinHeight(40));

                                GUILayout.Label("Results/Outcome", EditorStyles.miniLabel);
                                action.Results = EditorGUILayout.TextArea(action.Results ?? "", _textAreaWrap, GUILayout.MinHeight(40));
                                }
                            if (EditorGUI.EndChangeCheck())
                                {
                                OnContentChanged?.Invoke();
                                }
                            }
                        }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(3);
                    }

                if (_data.Actions.Count == 0)
                    {
                    EditorGUILayout.HelpBox("No actions recorded yet. Click 'Add Action' to document what you've done.", MessageType.Info);
                    }
                }
            EditorGUILayout.EndVertical();
            }

        void DrawTechnicalDetails()
            {
            EditorGUILayout.BeginVertical(_sectionStyle);
                {
                GUILayout.Label("🔧 Technical Details", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                    {
                    GUILayout.Label("Architecture Changes", EditorStyles.miniLabel);
                    string architectureChanges = EditorGUILayout.TextArea(
                        GetTechnicalField("ArchitectureChanges"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetTechnicalField("ArchitectureChanges", architectureChanges);

                    GUILayout.Label("Performance Impact", EditorStyles.miniLabel);
                    string performanceImpact = EditorGUILayout.TextArea(
                        GetTechnicalField("PerformanceImpact"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetTechnicalField("PerformanceImpact", performanceImpact);

                    GUILayout.Label("Dependencies Added/Removed", EditorStyles.miniLabel);
                    string dependencies = EditorGUILayout.TextArea(
                        GetTechnicalField("Dependencies"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetTechnicalField("Dependencies", dependencies);

                    GUILayout.Label("Configuration Changes", EditorStyles.miniLabel);
                    string configChanges = EditorGUILayout.TextArea(
                        GetTechnicalField("ConfigChanges"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetTechnicalField("ConfigChanges", configChanges);
                    }
                if (EditorGUI.EndChangeCheck())
                    {
                    OnContentChanged?.Invoke();
                    }
                }
            EditorGUILayout.EndVertical();
            }

        void DrawLessons()
            {
            EditorGUILayout.BeginVertical(_sectionStyle);
                {
                GUILayout.Label("📚 Lessons Learned", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                    {
                    GUILayout.Label("What Worked Well", EditorStyles.miniLabel);
                    string whatWorked = EditorGUILayout.TextArea(
                        GetLessonField("WhatWorked"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetLessonField("WhatWorked", whatWorked);

                    GUILayout.Label("What Could Be Improved", EditorStyles.miniLabel);
                    string improvements = EditorGUILayout.TextArea(
                        GetLessonField("Improvements"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetLessonField("Improvements", improvements);

                    GUILayout.Label("Key Insights", EditorStyles.miniLabel);
                    string insights = EditorGUILayout.TextArea(
                        GetLessonField("Insights"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetLessonField("Insights", insights);

                    GUILayout.Label("Future Recommendations", EditorStyles.miniLabel);
                    string recommendations = EditorGUILayout.TextArea(
                        GetLessonField("Recommendations"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetLessonField("Recommendations", recommendations);
                    }
                if (EditorGUI.EndChangeCheck())
                    {
                    OnContentChanged?.Invoke();
                    }
                }
            EditorGUILayout.EndVertical();
            }

        void DrawNextSteps()
            {
            EditorGUILayout.BeginVertical(_sectionStyle);
                {
                GUILayout.Label("🎯 Next Steps", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                    {
                    GUILayout.Label("Immediate Actions (Today/This Week)", EditorStyles.miniLabel);
                    string immediateActions = EditorGUILayout.TextArea(
                        GetNextStepField("ImmediateActions"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetNextStepField("ImmediateActions", immediateActions);

                    GUILayout.Label("Short-term Goals (This Month)", EditorStyles.miniLabel);
                    string shortTermGoals = EditorGUILayout.TextArea(
                        GetNextStepField("ShortTermGoals"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetNextStepField("ShortTermGoals", shortTermGoals);

                    GUILayout.Label("Long-term Vision (This Quarter/Year)", EditorStyles.miniLabel);
                    string longTermVision = EditorGUILayout.TextArea(
                        GetNextStepField("LongTermVision"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetNextStepField("LongTermVision", longTermVision);

                    GUILayout.Label("Research Needed", EditorStyles.miniLabel);
                    string researchNeeded = EditorGUILayout.TextArea(
                        GetNextStepField("ResearchNeeded"),
                        _textAreaWrap,
                        GUILayout.MinHeight(50)
                    );
                    SetNextStepField("ResearchNeeded", researchNeeded);
                    }
                if (EditorGUI.EndChangeCheck())
                    {
                    OnContentChanged?.Invoke();
                    }
                }
            EditorGUILayout.EndVertical();
            }

        void DrawFooter()
            {
            EditorGUILayout.BeginVertical(_sectionStyle);
                {
                GUILayout.Label("🔄 Form → Editor Sync", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                    {
                    _data.AutoSyncEnabled = EditorGUILayout.ToggleLeft(
                        "Auto-sync changes to Raw Editor",
                        _data.AutoSyncEnabled
                    );
                    }
                if (EditorGUI.EndChangeCheck())
                    {
                    OnContentChanged?.Invoke();
                    }

                EditorGUILayout.BeginHorizontal();
                    {
                    if (GUILayout.Button("🔄 Generate Markdown", GUILayout.Height(30)))
                        {
                        _data.RawMarkdown = _data.GenerateMarkdown();
                        _data.RawIsDirty = false;
                        OnContentChanged?.Invoke();
                        }

                    if (GUILayout.Button("📋 Copy to Clipboard", GUILayout.Height(30)))
                        {
                        string markdown = _data.GenerateMarkdown();
                        GUIUtility.systemCopyBuffer = markdown;
                        Debug.Log("📋 Markdown copied to clipboard!");
                        }
                    }
                EditorGUILayout.EndHorizontal();
                }
            EditorGUILayout.EndVertical();
            }

        // Helper methods for technical details storage
        string GetTechnicalField(string fieldName)
            {
            // Store in a simple format within RawMarkdown or create a dedicated storage system
            return EditorPrefs.GetString($"ScribeForm_Technical_{fieldName}", "");
            }

        void SetTechnicalField(string fieldName, string value)
            {
            EditorPrefs.SetString($"ScribeForm_Technical_{fieldName}", value ?? "");
            }

        // Helper methods for lessons storage
        string GetLessonField(string fieldName)
            {
            return EditorPrefs.GetString($"ScribeForm_Lesson_{fieldName}", "");
            }

        void SetLessonField(string fieldName, string value)
            {
            EditorPrefs.SetString($"ScribeForm_Lesson_{fieldName}", value ?? "");
            }

        // Helper methods for next steps storage
        string GetNextStepField(string fieldName)
            {
            return EditorPrefs.GetString($"ScribeForm_NextStep_{fieldName}", "");
            }

        void SetNextStepField(string fieldName, string value)
            {
            EditorPrefs.SetString($"ScribeForm_NextStep_{fieldName}", value ?? "");
            }

        // Clone methods
        Discovery CloneDiscovery(Discovery source)
            {
            return new Discovery
                {
                Category = source.Category + " (Copy)",
                KeyFinding = source.KeyFinding,
                Impact = source.Impact,
                Evidence = source.Evidence,
                RootCause = source.RootCause,
                PatternRecognition = source.PatternRecognition,
                Foldout = true
                };
            }

        ActionItem CloneAction(ActionItem source)
            {
            return new ActionItem
                {
                Name = source.Name + " (Copy)",
                Description = source.Description,
                FilesChanged = source.FilesChanged,
                CommandsExecuted = source.CommandsExecuted,
                Results = source.Results,
                Foldout = true
                };
            }
        }
    }
