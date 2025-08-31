#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Editor.TaskMaster
	{
	/// <summary>
	/// 🐙 GitHub Integration for TaskMaster
	/// Synchronizes TaskMaster tasks with GitHub Issues
	/// Supports assignee mapping, status sync, and time tracking
	/// </summary>
	public static class GitHubIntegration
		{
		// GitHub API configuration
		private static string _githubToken = "";
		private static string _repositoryOwner = "";
		private static string _repositoryName = "";

		// Username mapping for assignees
		private static readonly Dictionary<string, string> _usernameMapping = new()
		{
			{ "@copilot", "github-actions[bot]" },
			{ "@jmeyer1980", "jmeyer1980" },
            // Add more mappings as needed
        };

		[MenuItem("Tools/Living Dev Agent/TaskMaster/GitHub Integration", priority = 15)]
		public static void ShowGitHubIntegrationWindow ()
			{
			GitHubIntegrationWindow window = EditorWindow.GetWindow<GitHubIntegrationWindow>("🐙 GitHub Integration");
			window.minSize = new Vector2(400, 300);
			window.Show();
			}

		public static async Task<bool> CreateGitHubIssue (TaskMasterWindow.TaskCard task)
			{
			if (!IsConfigured())
				{
				EditorUtility.DisplayDialog("GitHub Not Configured",
					"Please configure GitHub integration first.", "OK");
				return false;
				}

			try
				{
				string issueData = CreateIssueFromTask(task);
				string result = await PostToGitHubAPI($"repos/{_repositoryOwner}/{_repositoryName}/issues", issueData);

				if (result != null)
					{
					Debug.Log($"✅ Created GitHub issue for task: {task.Title}");
					return true;
					}
				}
			catch (System.Exception ex)
				{
				Debug.LogError($"❌ Failed to create GitHub issue: {ex.Message}");
				EditorUtility.DisplayDialog("GitHub Error", $"Failed to create issue:\n{ex.Message}", "OK");
				}

			return false;
			}

		private static string CreateIssueFromTask (TaskMasterWindow.TaskCard task)
			{
			string assignee = GetGitHubUsername(task.AssignedTo);
			string [ ] labels = GetLabelsFromTask(task);

			string issueBody = $"{task.Description}\n\n";
			issueBody += $"**Task Details:**\n";
			issueBody += $"- Priority: {task.Priority}\n";
			issueBody += $"- Status: {task.Status}\n";
			issueBody += $"- Created: {task.CreatedAt:yyyy-MM-dd HH:mm}\n";

			if (task.Deadline.HasValue)
				{
				issueBody += $"- Deadline: {task.Deadline.Value:yyyy-MM-dd}\n";
				}

			if (task.TimeTracked > 0)
				{
				issueBody += $"- Time Tracked: {task.TimeTracked:F2} hours\n";
				}

			issueBody += $"\n---\n*Created from TaskMaster*";

			var issueData = new
				{
				title = task.Title,
				body = issueBody,
				assignees = !string.IsNullOrEmpty(assignee) ? new [ ] { assignee } : new string [ 0 ],
				labels
				};

			return JsonUtility.ToJson(issueData);
			}

		private static string GetGitHubUsername (string taskAssignee)
			{
			if (_usernameMapping.TryGetValue(taskAssignee, out string githubUsername))
				{
				return githubUsername;
				}

			// Remove @ prefix if present
			return taskAssignee?.StartsWith("@") == true ? taskAssignee [ 1.. ] : taskAssignee;
			}

		private static string [ ] GetLabelsFromTask (TaskMasterWindow.TaskCard task)
			{
			var labels = new List<string>
			{
                // Add priority label
                $"priority-{task.Priority.ToString().ToLower()}",

                // Add status label
                $"status-{task.Status.ToString().ToLower()}",

                // Add TaskMaster label
                "taskmaster"
			};

			// Add task-specific tags if any
			labels.AddRange(task.Tags.Where(tag => !string.IsNullOrEmpty(tag)));

			return labels.ToArray();
			}

		private static async Task<string> PostToGitHubAPI (string endpoint, string jsonData)
			{
			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Authorization", $"token {_githubToken}");
			client.DefaultRequestHeaders.Add("User-Agent", "TaskMaster-Unity-Integration");

			var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
			HttpResponseMessage response = await client.PostAsync($"https://api.github.com/{endpoint}", content);

			if (response.IsSuccessStatusCode)
				{
				return await response.Content.ReadAsStringAsync();
				}
			else
				{
				string error = await response.Content.ReadAsStringAsync();
				throw new System.Exception($"GitHub API error: {response.StatusCode} - {error}");
				}
			}

		public static bool IsConfigured ()
			{
			return !string.IsNullOrEmpty(_githubToken) &&
				   !string.IsNullOrEmpty(_repositoryOwner) &&
				   !string.IsNullOrEmpty(_repositoryName);
			}

		public static void SetConfiguration (string token, string owner, string repo)
			{
			_githubToken = token;
			_repositoryOwner = owner;
			_repositoryName = repo;

			Debug.Log($"🐙 GitHub configured: {owner}/{repo}");
			}
		}

	/// <summary>
	/// Configuration window for GitHub integration
	/// </summary>
	public class GitHubIntegrationWindow : EditorWindow
		{
		private string _githubToken = "";
		private string _repositoryOwner = "";
		private string _repositoryName = "";
		private Vector2 _scrollPosition;

		private void OnGUI ()
			{
			using var scroll = new EditorGUILayout.ScrollViewScope(this._scrollPosition);
			GUILayout.Label("🐙 GitHub Integration Setup", EditorStyles.boldLabel);

			EditorGUILayout.Space();

			EditorGUILayout.HelpBox(
				"Configure GitHub integration to create issues from TaskMaster tasks.\n" +
				"You'll need a GitHub Personal Access Token with 'repo' permissions.",
				MessageType.Info);

			EditorGUILayout.Space();

			// GitHub token field (masked)
			GUILayout.Label("GitHub Personal Access Token:");
			this._githubToken = EditorGUILayout.PasswordField(this._githubToken);

			EditorGUILayout.Space();

			// Repository details
			GUILayout.Label("Repository Owner (username or org):");
			this._repositoryOwner = EditorGUILayout.TextField(this._repositoryOwner);

			GUILayout.Label("Repository Name:");
			this._repositoryName = EditorGUILayout.TextField(this._repositoryName);

			EditorGUILayout.Space();

			// Configuration status
			bool isConfigured = GitHubIntegration.IsConfigured();
			string statusText = isConfigured ? "✅ Configured" : "❌ Not Configured";
			EditorGUILayout.LabelField("Status:", statusText);

			EditorGUILayout.Space();

			// Action buttons
			using (new EditorGUILayout.HorizontalScope())
				{
				if (GUILayout.Button("💾 Save Configuration"))
					{
					this.SaveConfiguration();
					}

				if (GUILayout.Button("🧪 Test Connection"))
					{
					this.TestConnection();
					}
				}

			EditorGUILayout.Space();

			// Usage instructions
			EditorGUILayout.LabelField("Usage Instructions:", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(
				"1. Create a GitHub Personal Access Token at: https://github.com/settings/tokens\n" +
				"2. Grant 'repo' permissions to the token\n" +
				"3. Enter your token, repository owner, and repository name above\n" +
				"4. Save configuration and test the connection\n" +
				"5. Use 'Export to GitHub' buttons in TaskMaster to create issues",
				MessageType.None);
			}

		private void SaveConfiguration ()
			{
			if (string.IsNullOrEmpty(this._githubToken) ||
				string.IsNullOrEmpty(this._repositoryOwner) ||
				string.IsNullOrEmpty(this._repositoryName))
				{
				EditorUtility.DisplayDialog("Invalid Configuration",
					"Please fill in all fields before saving.", "OK");
				return;
				}

			GitHubIntegration.SetConfiguration(this._githubToken, this._repositoryOwner, this._repositoryName);

			EditorUtility.DisplayDialog("Configuration Saved",
				$"GitHub integration configured for {this._repositoryOwner}/{this._repositoryName}", "OK");
			}

		private void TestConnection ()
			{
			if (!GitHubIntegration.IsConfigured())
				{
				EditorUtility.DisplayDialog("Configuration Required",
					"Please configure GitHub integration first.", "OK");
				return;
				}

			EditorUtility.DisplayDialog("Test Connection",
				"Connection test coming soon!\n\nFor now, try creating an issue from TaskMaster to test.", "OK");
			}
		}
	}
#endif
