#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.Modules
{
    /// <summary>
    /// Base class for all TLDL Scribe window modules
    /// Provides common functionality and interface for modular UI components
    /// </summary>
    public abstract class ScribeModuleBase
    {
        protected TLDLScribeData _data;
        protected object _window; // Use object to avoid circular dependency
        
        // Shared GUI styles - initialized once and reused
        protected static GUIStyle _labelWrap;
        protected static GUIStyle _textAreaMonospace;
        protected static GUIStyle _textAreaWrap;
        protected static GUIStyle _h1, _h2, _h3, _bodyWrap, _listItem, _codeBlock;
        protected static bool _stylesInitialized = false;

        public ScribeModuleBase(TLDLScribeData data)
        {
            _data = data;
            InitializeSharedStyles();
        }

        protected static void InitializeSharedStyles()
        {
            if (_stylesInitialized) return;
            
            _labelWrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
            _textAreaMonospace = new GUIStyle(EditorStyles.textArea)
            {
                font = SafeFont("Consolas", 12),
                wordWrap = false
            };
            _textAreaWrap = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            _bodyWrap = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
            _listItem = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
            _h1 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, richText = true, wordWrap = true };
            _h2 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, richText = true, wordWrap = true };
            _h3 = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12, richText = true, wordWrap = true };
            _codeBlock = new GUIStyle(EditorStyles.textArea) { font = SafeFont("Consolas", 12), wordWrap = false };
            
            _stylesInitialized = true;
        }

        private static Font SafeFont(string family, int size)
        {
            try { return Font.CreateDynamicFontFromOSFont(family, size); } 
            catch { return EditorStyles.textArea.font; }
        }

        public virtual void Initialize() { }
        public virtual void Dispose() { }

        // Common UI helper methods
        protected void DrawHelp(string title, string body)
        {
            EditorGUILayout.HelpBox($"{title}: {body}", MessageType.None);
        }

        protected void DrawPlaceholder(string label, string hint)
        {
            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox(hint, MessageType.Info);
        }

        protected void LabelSmall(string text)
        {
            GUILayout.Label(text, EditorStyles.miniLabel);
        }

        protected static string LabeledLines(string label, string value)
        {
            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            return EditorGUILayout.TextArea(value, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, 
                GUILayout.MinHeight(56), GUILayout.ExpandWidth(true));
        }

        protected static string LabeledMultiline(string label, string value)
        {
            GUILayout.Label(label, EditorStyles.miniBoldLabel);
            return EditorGUILayout.TextArea(value, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, 
                GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));
        }

        protected static string LabeledChecklist(string label, string value)
        {
            GUILayout.Label(label + " (one per line)", EditorStyles.miniBoldLabel);
            return EditorGUILayout.TextArea(value, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, 
                GUILayout.MinHeight(80), GUILayout.ExpandWidth(true));
        }

        protected static T Clone<T>(T src) where T : new()
        {
            var t = new T();
            foreach (System.Reflection.FieldInfo f in typeof(T).GetFields())
            {
                f.SetValue(t, f.GetValue(src));
            }
            return t;
        }

        protected void SetStatus(string message)
        {
            // Use reflection to call SetStatusLine if the window has it
            if (_window != null)
            {
                var method = _window.GetType().GetMethod("SetStatusLine");
                method?.Invoke(_window, new object[] { message });
            }
        }

        public void SetWindow(object window)
        {
            _window = window;
        }
    }
}
#endif
