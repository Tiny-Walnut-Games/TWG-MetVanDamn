#if UNITY_EDITOR
using LivingDevAgent.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor
	{
	/// <summary>
	/// 🧙‍♂️ The Scribe - A Modular TLDL Documentation Dashboard
	/// 
	/// Transformed from a monolithic 2400+ line file into a clean modular architecture!
	/// Each module is a specialized widget that can be independently maintained and extended.
	/// 
	/// Key architectural improvements:
	/// - Templates moved to prime toolbar real estate (🎭 visible emojis!)
	/// - Navigator with enhanced visual styling and left border
	/// - Dashboard approach enables other devs to slot in custom modules
	/// - Centralized data model prevents state drift between modules
	/// - Each module handles its own UI lifecycle and operations
	/// 
	/// This modular approach treats each section as a "widget" in a documentation dashboard,
	/// making it easy for teams to extend with custom functionality while maintaining
	/// the core TLDL workflow integrity.
	/// </summary>
	public class TLDLScribeWindow : EditorWindow
		{
		// Core data model - single source of truth
		private readonly TLDLScribeData _data = new();

		// Module instances
		private TemplateModule _templateModule;
		private NavigatorModule _navigatorModule;
		private FormModule _formModule;
		private EditorModule _editorModule;
		private PreviewModule _previewModule;

		// Tab management - TaskMaster removed, pure documentation focus
		private readonly string [ ] _tabNames = { "📋 Form", "✏️ Editor", "👁️ Preview" };

		// UI state
		private string _statusLine = "🎭 Ready to begin your documentation quest!";

		// Enhanced visual styling
		private GUIStyle _navBackgroundStyle;
		private bool _stylesInitialized = false;

		[MenuItem("Tools/Living Dev Agent/The Scribe", priority = 20)]
		public static void OpenScribe ()
			{
			OpenWindow("📜 The Scribe");
			}

		[MenuItem("Tools/Living Dev Agent/TLDL Wizard (Deprecated)")]
		public static void OpenDeprecated ()
			{
			OpenWindow("📜 The Scribe (Legacy)");
			}

		// Back-compat: keep old entry point name but route to Scribe
		public static void ShowWindow ()
			{
			OpenWindow("📜 The Scribe");
			}

		private static void OpenWindow (string title)
			{
			TLDLScribeWindow wnd = GetWindow<TLDLScribeWindow>(true, title, true);
			wnd.minSize = new Vector2(900, 600);
			wnd.Show();
			}

		private void OnEnable ()
			{
			this.InitializeModules();
			this.InitializeStyles();
			}

		private void InitializeModules ()
			{
			// Create the dashboard modules - pure documentation focus
			this._templateModule = new TemplateModule(this._data);
			this._navigatorModule = new NavigatorModule(this._data);
			this._formModule = new FormModule(this._data);
			this._editorModule = new EditorModule(this._data);
			this._previewModule = new PreviewModule(this._data);
			// TaskMaster removed - now standalone

			// Link modules to window for status updates
			this._templateModule.SetWindow(this);
			this._navigatorModule.SetWindow(this);
			this._formModule.SetWindow(this);
			this._editorModule.SetWindow(this);
			this._previewModule.SetWindow(this);

			// Initialize all modules
			this._templateModule.Initialize();
			this._navigatorModule.Initialize();
			this._formModule.Initialize();
			this._editorModule.Initialize();
			this._previewModule.Initialize();
			}

		private void InitializeStyles ()
			{
			if (this._stylesInitialized) return; //⚠ False Flag ⚠-  @jmeyer1980 - IL for legibility

			// Create nav background style with left border and enhanced styling
			this._navBackgroundStyle = new GUIStyle("Box")
				{
				normal =
				{
					background = this.CreateNavBackgroundTexture(),
					textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black
				},
				border = new RectOffset(3, 1, 1, 1), // Enhanced left border
				padding = new RectOffset(10, 8, 8, 8),
				margin = new RectOffset(0, 4, 0, 0)
				};

			this._stylesInitialized = true;
			}

		private Texture2D CreateNavBackgroundTexture ()
			{
			var texture = new Texture2D(8, 1);
			Color bgColor = EditorGUIUtility.isProSkin
				? new Color(0.15f, 0.15f, 0.15f, 1f)  // Darker for contrast in dark theme
				: new Color(0.85f, 0.85f, 0.85f, 1f); // Lighter for contrast in light theme
			Color borderColor = EditorGUIUtility.isProSkin
				? new Color(0.4f, 0.6f, 1f, 1f)       // Blue accent for dark theme
				: new Color(0.2f, 0.4f, 0.8f, 1f);    // Darker blue for light theme

			// ⚠ Nitpick ⚠ - @jmeyer1980 - double-line for clarity - not an if statement, so easier to read this way
			// Create gradient with left border
			for (int x = 0; x < 3; x++)
				texture.SetPixel(x, 0, borderColor);
			for (int x = 3; x < 8; x++)
				texture.SetPixel(x, 0, bgColor);

			texture.Apply();
			return texture;
			}

		private void OnGUI ()
			{
			// ⚠ nitpick ⚠ - @jmeyer1980 - IL for legibility
			if (!this._stylesInitialized) this.InitializeStyles();

			this.DrawTopToolbar();

			EditorGUILayout.Space(2);
			using (new EditorGUILayout.HorizontalScope())
				{
				this.DrawNavigatorPanel(260);
				this.DrawMainContent();
				}

			EditorGUILayout.Space(4);

			// Status line with emoji support
			using (new EditorGUILayout.HorizontalScope("box"))
				{
				GUIContent statusContent = new(this._statusLine);
				EditorGUILayout.LabelField(statusContent, EditorStyles.miniLabel);
				}
			}

		private void DrawTopToolbar ()
			{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
				{
				// 🎭 Template section first (moved to prime real estate!)
				this._templateModule.DrawToolbar();

				GUILayout.Space(8);

				// 🗺️ Navigator controls
				this._navigatorModule.DrawToolbar();

				GUILayout.Space(8);

				// ✏️ File operations
				this._editorModule.DrawToolbar();

				GUILayout.FlexibleSpace();

				// 📝 Form operations
				this._formModule.DrawToolbar();

				// Handle tab switching from form module
				if (this._formModule.ShouldSwitchToEditor)
					{
					this._data.SelectedTab = 1;
					this._formModule.ResetSwitchFlags();
					}
				if (this._formModule.ShouldSwitchToPreview)
					{
					this._data.SelectedTab = 2;
					this._formModule.ResetSwitchFlags();
					}
				}
			}

		private void DrawNavigatorPanel (float width)
			{
			// Enhanced navigator panel with visual styling and left border
			using (new EditorGUILayout.VerticalScope(this._navBackgroundStyle, GUILayout.MaxWidth(width), GUILayout.MinWidth(width)))
				{
				this._navigatorModule.DrawPanel(width);
				}
			}

		private void DrawMainContent ()
			{
			using (new EditorGUILayout.VerticalScope())
				{
				// Tab bar with emoji icons for clarity
				int newTab = GUILayout.Toolbar(this._data.SelectedTab, this._tabNames);

				if (newTab != this._data.SelectedTab)
					{
					this._data.SelectedTab = newTab;
					// Update status when switching tabs
					string [ ] tabNames = new [ ] { "Form Builder", "Raw Editor", "Live Preview" };
					this.SetStatusLine($"🔄 Switched to {tabNames [ this._data.SelectedTab ]}");
					}

				EditorGUILayout.Space(6);

				// Main content area toolbar
				using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
					{
					switch (this._data.SelectedTab)
						{
						case 0: this._formModule.DrawToolbar(); break;
						case 1: this._editorModule.DrawToolbar(); break;
						case 2: /* Preview has no toolbar */ break;
						default:
							break;
						}
					}

				// Main content area
				Rect mainContentRect = EditorGUILayout.GetControlRect(false, 400, GUILayout.ExpandHeight(true));
				switch (this._data.SelectedTab)
					{
					case 0: // Form
						this._formModule.DrawContent(mainContentRect);
						this.HandleFormModuleSwitching();
						break;

					case 1: // Editor
						this._editorModule.DrawContent(mainContentRect);
						break;

					case 2: // Preview
						this._previewModule.DrawContent(mainContentRect);
						break;
					default:
						break;
					}
				}
			}

		private void HandleFormModuleSwitching ()
			{
			// Handle tab switching from form module
			if (this._formModule.ShouldSwitchToEditor)
				{
				this._data.SelectedTab = 1;
				this._formModule.ResetSwitchFlags();
				}
			if (this._formModule.ShouldSwitchToPreview)
				{
				this._data.SelectedTab = 2;
				this._formModule.ResetSwitchFlags();
				}
			}

		/// <summary>
		/// Public API for modules to update status line with emoji support
		/// </summary>
		public void SetStatusLine (string status)
			{
			this._statusLine = status;
			this.Repaint();
			}

		private void OnDestroy ()
			{
			// Cleanup module resources
			this._navigatorModule?.Dispose();
			this._templateModule?.Dispose();
			this._formModule?.Dispose();
			this._editorModule?.Dispose();
			this._previewModule?.Dispose();
			}

		private void OnDisable ()
			{
			// Additional cleanup if needed
			this._templateModule?.Dispose();
			this._navigatorModule?.Dispose();
			this._formModule?.Dispose();
			this._editorModule?.Dispose();
			this._previewModule?.Dispose();
			}
		}
	}
#endif
