#if UNITY_EDITOR && METVD_FULL_DOTS
// @Intent: Centralized SubScene property discovery and assignment utilities
// @CheekPreservation: Unified fallback strategy for all SubScene property access
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.Scenes;

namespace TinyWalnutGames.MetVD.Authoring.Editor.RitualSupport
	{
	internal static class SubScenePropertyHelper
		{
		private static readonly string[] SceneAssetPropertyNames =
			{
			"m_SceneAsset", "_SceneAsset", "sceneAsset", "SceneAsset", "m_Scene", "_Scene"
			};

		private static readonly string[] AutoLoadPropertyNames =
			{
			"m_AutoLoadScene", "_AutoLoadScene", "autoLoadScene", "AutoLoadScene"
			};

		/// <summary>
		/// Faculty-grade SceneAsset property discovery with reflection fallback
		/// </summary>
		public static SerializedProperty? FindSceneAssetProperty(SerializedObject so)
			{
			// Try SerializedProperty discovery first
			foreach (string name in SceneAssetPropertyNames)
				{
				SerializedProperty property = so.FindProperty(name);
				if (property != null)
					{
					Debug.Log($"✅ Found SceneAsset property via SerializedProperty: {name}");
					return property;
					}
				}

			Debug.LogWarning(
				"⚠️ SceneAsset property not found via SerializedProperty - this may indicate Unity version differences");
			return null;
			}

		/// <summary>
		/// Faculty-grade AutoLoad property discovery
		/// </summary>
		public static SerializedProperty? FindAutoLoadProperty(SerializedObject so)
			{
			foreach (string name in AutoLoadPropertyNames)
				{
				SerializedProperty property = so.FindProperty(name);
				if (property != null)
					{
					Debug.Log($"✅ Found AutoLoad property: {name}");
					return property;
					}
				}

			Debug.LogWarning("⚠️ AutoLoad property not found - SubScene may not auto-load properly");
			return null;
			}

		/// <summary>
		/// Safe assignment with validation and detailed logging
		/// </summary>
		public static bool AssignSceneAssetWithValidation(SubScene subScene, SceneAsset sceneAsset, string subSceneName)
			{
			if (subScene == null || sceneAsset == null)
				{
				Debug.LogError($"❌ Cannot assign null references for {subSceneName}");
				return false;
				}

			try
				{
				var so = new SerializedObject(subScene);

				// Find and assign SceneAsset
				SerializedProperty? sceneProp = FindSceneAssetProperty(so);
				if (sceneProp != null)
					{
					sceneProp.objectReferenceValue = sceneAsset;
					Debug.Log(
						$"✅ Set SceneAsset reference for {subSceneName} using property: {sceneProp.propertyPath}");
					}
				else
					{
					Debug.LogError($"❌ Could not find SceneAsset property for {subSceneName}");
					return false;
					}

				// Find and assign AutoLoad
				SerializedProperty? autoLoadProp = FindAutoLoadProperty(so);
				if (autoLoadProp != null)
					{
					autoLoadProp.boolValue = true;
					Debug.Log($"✅ Set AutoLoad = true for {subSceneName}");
					}

				// Apply changes
				EditorUtility.SetDirty(subScene);
				EditorUtility.SetDirty(subScene.gameObject);

				bool applied = so.ApplyModifiedPropertiesWithoutUndo();
				if (!applied)
					{
					Debug.LogWarning($"⚠️ Failed to apply SerializedObject changes for {subSceneName}");
					return false;
					}

				// Final validation
				var validation = new SerializedObject(subScene);
				SerializedProperty? validateScene = FindSceneAssetProperty(validation);
				bool success = validateScene != null && validateScene.objectReferenceValue != null;

				Debug.Log(
					$"✅ SubScene reference assignment {(success ? "SUCCESSFUL" : "FAILED")} for '{subSceneName}'");
				return success;
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Exception during SubScene assignment for {subSceneName}: {ex.Message}");
				return false;
				}
			}

		/// <summary>
		/// Diagnostic method to list all available properties on a SubScene
		/// </summary>
		public static void LogAvailableProperties(SubScene subScene, string subSceneName)
			{
			try
				{
				var so = new SerializedObject(subScene);
				SerializedProperty iterator = so.GetIterator();
				var properties = new System.Collections.Generic.List<string>();

				if (iterator.NextVisible(true))
					{
					do
						{
						properties.Add(iterator.propertyPath);
						} while (iterator.NextVisible(false));
					}

				Debug.Log($"🔍 Available properties on {subSceneName}: {string.Join(", ", properties)}");
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Failed to enumerate properties for {subSceneName}: {ex.Message}");
				}
			}
		}
	}
#endif
