#if UNITY_EDITOR
using TinyWalnutGames.MetVD.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace TinyWalnutGames.MetVD.Authoring.Editor
	{
	/// <summary>
	/// Editor gizmo for visualizing the navigation graph with nodes, links, and gate costs
	/// Provides interactive debugging for AI navigation pathfinding
	/// </summary>
	public class NavigationGraphGizmo : MonoBehaviour
		{
		[Header("Display Settings")]
		[SerializeField] private bool showNavigationGraph = true;
		[SerializeField] private readonly bool showNodeLabels = true;
		[SerializeField] private readonly bool showLinkCosts = true;
		[SerializeField] private readonly bool showGateRequirements = true;
		[SerializeField] private readonly bool showUnreachableAreas = true;

		[Header("Visual Configuration")]
		[SerializeField] private readonly float nodeRadius = 0.5f;
		[SerializeField] private readonly float linkWidth = 2.0f;
		[SerializeField] private readonly float labelOffset = 1.0f;

		[Header("Agent Testing")]
		[SerializeField] private readonly AgentCapabilityProfile testAgentProfile = AgentCapabilityProfile.BasicAgent;
		[SerializeField] private readonly uint highlightPathFromNode = 0;
		[SerializeField] private readonly uint highlightPathToNode = 0;

		[Header("Color Scheme")]
		[SerializeField] private readonly NavigationColorScheme colorScheme = NavigationColorScheme.Default;

		private static readonly Color[] DefaultColors = new Color[]
		{
			Color.green,      // Reachable nodes
            Color.red,        // Unreachable nodes
            Color.blue,       // Normal links
            Color.orange,     // Gate links
            Color.purple,     // Hard gates
            Color.yellow,     // Soft gates
            Color.cyan        // Highlighted path
        };

		private static readonly Color[] HighContrastColors = new Color[]
		{
			new(0.2f, 0.8f, 0.2f),  // Bright green
            new(0.8f, 0.2f, 0.2f),  // Bright red
            new(0.2f, 0.4f, 0.8f),  // Deep blue
            new(1.0f, 0.6f, 0.2f),  // Orange
            new(0.8f, 0.2f, 0.8f),  // Magenta
            new(0.9f, 0.9f, 0.2f),  // Bright yellow
            new(0.2f, 0.9f, 0.9f)   // Bright cyan
        };

		private Color[] _currentColors;
		private World _world;
		private EntityManager _entityManager;

		/// <summary>
		/// Menu item to create a navigation graph gizmo in the scene
		/// </summary>
		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Debug/Gizmos/Navigation/Create Navigation Graph Gizmo")]
		public static void CreateNavigationGraphGizmo()
			{
			var gizmoGO = new GameObject("NavigationGraphGizmo");
			gizmoGO.AddComponent<NavigationGraphGizmo>();
			Selection.activeGameObject = gizmoGO;

			Debug.Log("NavigationGraphGizmo created. Configure visualization settings in the inspector.");
			}

		/// <summary>
		/// Menu item to toggle navigation graph visualization for all gizmos in scene
		/// </summary>
		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Debug/Gizmos/Navigation/Toggle Navigation Graph Visualization")]
		public static void ToggleNavigationGraphVisualization()
			{
			NavigationGraphGizmo[] gizmos = FindObjectsByType<NavigationGraphGizmo>(FindObjectsSortMode.None);
			bool newState = gizmos.Length == 0 || !gizmos[0].showNavigationGraph;

			foreach (NavigationGraphGizmo gizmo in gizmos)
				{
				gizmo.showNavigationGraph = newState;
				}

			Debug.Log($"Navigation graph visualization {(newState ? "enabled" : "disabled")} for {gizmos.Length} gizmos.");
			}

		/// <summary>
		/// Menu item to validate navigation connectivity and show results
		/// </summary>
		[MenuItem("Tiny Walnut Games/MetVanDAMN!/Debug/Navigation/Validate Navigation Connectivity")]
		public static void ValidateNavigationConnectivity()
			{
			World world = World.DefaultGameObjectInjectionWorld;
			if (world == null || !world.IsCreated)
				{
				Debug.LogWarning("No world available for navigation validation.");
				return;
				}

			NavigationValidationReport report = NavigationValidationUtility.GenerateValidationReport(world);

			try
				{
				string title = "Navigation Connectivity Report";
				string message = $"Navigation Graph Status:\n" +
							 $"• Total Nodes: {report.TotalNodes}\n" +
							 $"• Total Links: {report.TotalLinks}\n" +
							 $"• Unreachable Areas: {(report.HasUnreachableAreas ? "Yes" : "None")}\n" +
							 $"• Unreachable Node Count: {report.UnreachableNodeCount}\n" +
							 $"• Isolated Components: {report.IsolatedComponentCount}\n\n";

				if (report.Issues.Length > 0)
					{
					message += $"Found {report.Issues.Length} connectivity issues. See Console for details.";

					// Log detailed issues
					Debug.Log("=== Navigation Connectivity Issues ===");
					for (int i = 0; i < report.Issues.Length; i++)
						{
						NavigationIssue issue = report.Issues[i];
						Debug.LogWarning($"{issue.Type}: {issue.Description} (Node: {issue.NodeId})");
						}
					}
				else
					{
					message += "No connectivity issues found!";
					}

				EditorUtility.DisplayDialog(title, message, "OK");
				}
			finally
				{
				report.Dispose();
				}
			}

		private void Start()
			{
			_currentColors = colorScheme == NavigationColorScheme.Default ? DefaultColors : HighContrastColors;
			_world = World.DefaultGameObjectInjectionWorld;
			if (_world != null)
				{
				_entityManager = _world.EntityManager;
				}
			}

		private void OnDrawGizmos()
			{
			if (!showNavigationGraph || _world == null || !_world.IsCreated)
				{
				return;
				}

			DrawNavigationGraph();
			}

		private void OnDrawGizmosSelected()
			{
			if (!showNavigationGraph || _world == null || !_world.IsCreated)
				{
				return;
				}

			DrawNavigationGraph();
			DrawDetailedInformation();
			}

		private void DrawNavigationGraph()
			{
			EntityQuery navGraphQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NavigationGraph>());
			if (navGraphQuery.IsEmpty)
				{
				return;
				}

			NavigationGraph navGraph = navGraphQuery.GetSingleton<NavigationGraph>();
			if (!navGraph.IsReady)
				{
				return;
				}

			// Get test agent capabilities
			AgentCapabilities testCapabilities = GetTestAgentCapabilities();

			// Draw navigation nodes
			DrawNavigationNodes(testCapabilities);

			// Draw navigation links
			DrawNavigationLinks(testCapabilities);

			// Draw highlighted path if specified
			if (highlightPathFromNode != 0 && highlightPathToNode != 0)
				{
				DrawHighlightedPathFallback(highlightPathFromNode, highlightPathToNode, testCapabilities);
				}
			}

		private void DrawNavigationNodes(AgentCapabilities caps)
			{
			EntityQuery q = _entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<NavNode>(),
				ComponentType.ReadOnly<NodeId>());

			using NativeArray<Entity> entities = q.ToEntityArray(Allocator.Temp);

			for (int i = 0; i < entities.Length; i++)
				{
				Entity e = entities[i];
				NavNode navNode = _entityManager.GetComponentData<NavNode>(e);
				NodeId nodeId = _entityManager.GetComponentData<NodeId>(e);

				float3 wp = navNode.WorldPosition;
				bool reachable = navNode.IsCompatibleWith(caps);

				// Choose color based on reachability
				Gizmos.color = reachable ? _currentColors[0] : _currentColors[1];

				// Draw node sphere
				Gizmos.DrawWireSphere(wp, nodeRadius);

				if (!reachable && showUnreachableAreas)
					{
					Gizmos.color = Color.red;
					Gizmos.DrawSphere(wp, nodeRadius * 0.3f);
					}

				// Draw node labels
				if (showNodeLabels)
					{
					float3 lp = wp + new float3(0, labelOffset, 0);
					Handles.Label(lp, $"N{nodeId._value}\n{navNode.BiomeType}\n{navNode.PrimaryPolarity}", GetLabelStyle(reachable));
					}
				}
			}

		private void DrawNavigationLinks(AgentCapabilities caps)
			{
			EntityQuery linkQ = _entityManager.CreateEntityQuery(
				ComponentType.ReadOnly<NavNode>(),
				ComponentType.ReadOnly<NavLinkBufferElement>());

			using NativeArray<Entity> entities = linkQ.ToEntityArray(Allocator.Temp);

			for (int i = 0; i < entities.Length; i++)
				{
				Entity e = entities[i];
				NavNode navNode = _entityManager.GetComponentData<NavNode>(e);
				DynamicBuffer<NavLinkBufferElement> buffer = _entityManager.GetBuffer<NavLinkBufferElement>(e);

				for (int j = 0; j < buffer.Length; j++)
					{
					NavLink link = buffer[j].Value;
					DrawNavigationLink(navNode, link, caps);
					}
				}
			}

		private void DrawNavigationLink(NavNode source, NavLink link, AgentCapabilities caps)
			{
			Entity targetEntity = FindEntityByNodeId(link.ToNodeId);
			if (targetEntity == Entity.Null || !_entityManager.HasComponent<NavNode>(targetEntity))
				{
				return;
				}

			NavNode target = _entityManager.GetComponentData<NavNode>(targetEntity);
			bool canTraverse = link.CanTraverseWith(caps, source.NodeId);
			float cost = link.CalculateTraversalCost(caps);

			// Choose link color based on gate requirements and traversability
			Color linkColor = link.RequiredPolarity != Polarity.None || link.RequiredAbilities != Ability.None
				? link.GateSoftness == GateSoftness.Hard ? _currentColors[4] : _currentColors[5]
				: _currentColors[2];
			if (!canTraverse)
				{
				linkColor = Color.gray;
				}

			// Draw link line
			Gizmos.color = linkColor;
			float3 a = source.WorldPosition;
			float3 b = target.WorldPosition;

			// Draw arrow for directional links
			if (link.ConnectionType != ConnectionType.Bidirectional)
				{
				DrawArrowLine(a, b, linkWidth * 0.01f);
				}
			else
				{
				Gizmos.DrawLine(a, b);
				}

			// Draw link cost labels
			if (showLinkCosts)
				{
				float3 mid = (a + b) * 0.5f;
				string txt = $"Cost: {cost:F1}";

				if (showGateRequirements && (link.RequiredPolarity != Polarity.None || link.RequiredAbilities != Ability.None))
					{
					txt += $"\n{link.RequiredPolarity}";
					if (link.RequiredAbilities != Ability.None)
						{
						txt += $"\n{link.RequiredAbilities}";
						}
					}

				Handles.Label(mid, txt, GetLinkLabelStyle(canTraverse));
				}
			}

		private void DrawHighlightedPathFallback(uint fromId, uint toId, AgentCapabilities caps)
			{
			// Fallback simple straight line highlight with capability-based visualization
			Entity fromE = FindEntityByNodeId(fromId);
			Entity toE = FindEntityByNodeId(toId);
			if (fromE == Entity.Null || toE == Entity.Null)
				{
				return;
				}

			NavNode fromNode = _entityManager.GetComponentData<NavNode>(fromE);
			NavNode toNode = _entityManager.GetComponentData<NavNode>(toE);

			// Use agent capabilities to determine path visualization style
			Color pathColor = GetPathColorByCapabilities(caps);
			PathVisualizationStyle pathStyle = GetPathStyleByCapabilities(caps);

			Gizmos.color = pathColor;

			// Draw path with capability-appropriate style
			if (pathStyle == PathVisualizationStyle.Dashed)
				{
				DrawDashedLine(fromNode.WorldPosition, toNode.WorldPosition);
				}
			else
				{
				Gizmos.DrawLine(fromNode.WorldPosition, toNode.WorldPosition);
				}

			float3 mid = (fromNode.WorldPosition + toNode.WorldPosition) * 0.5f;
			string capabilityText = GetCapabilityDisplayText(caps);
			Handles.Label(mid, $"(Preview Path) {fromId}->{toId} [{capabilityText}]", EditorStyles.boldLabel);
			}

		private void DrawDetailedInformation()
			{
			EntityQuery navGraphQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NavigationGraph>());
			if (navGraphQuery.IsEmpty)
				{
				return;
				}

			NavigationGraph navGraph = navGraphQuery.GetSingleton<NavigationGraph>();

			// Draw information panel in scene view
			Handles.BeginGUI();

			var rect = new Rect(10, 10, 300, 150);
			GUI.Box(rect, "Navigation Graph Info");

			string info = $"Nodes: {navGraph.NodeCount}\n" +
					   $"Links: {navGraph.LinkCount}\n" +
					   $"Ready: {navGraph.IsReady}\n" +
					   $"Unreachable Areas: {navGraph.UnreachableAreaCount}\n" +
					   $"Test Agent: {testAgentProfile}\n" +
					   $"Last Rebuild: {navGraph.LastRebuildTime:F2}s";

			GUI.Label(new Rect(rect.x + 10, rect.y + 20, rect.width - 20, rect.height - 30), info);

			Handles.EndGUI();
			}

		private void DrawArrowLine(float3 s, float3 e, float size)
			{
			Gizmos.DrawLine(s, e);

			float3 dir = math.normalize(e - s);
			float3 right = math.cross(dir, new float3(0, 1, 0));
			float3 h1 = e - dir * size + 0.5f * size * right;
			float3 h2 = e - dir * size - 0.5f * size * right;

			Gizmos.DrawLine(e, h1);
			Gizmos.DrawLine(e, h2);
			}

		private Entity FindEntityByNodeId(uint nodeId)
			{
			EntityQuery q = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeId>());
			using NativeArray<Entity> entities = q.ToEntityArray(Allocator.Temp);
			using NativeArray<NodeId> ids = q.ToComponentDataArray<NodeId>(Allocator.Temp);

			for (int i = 0; i < ids.Length; i++)
				{
				if (ids[i]._value == nodeId)
					{
					return entities[i];
					}
				}

			return Entity.Null;
			}

		private AgentCapabilities GetTestAgentCapabilities()
			{
			return testAgentProfile switch
				{
					AgentCapabilityProfile.BasicAgent => new AgentCapabilities(Polarity.None, Ability.None, 0f, "BasicAgent"),
					AgentCapabilityProfile.MovementAgent => new AgentCapabilities(Polarity.None, Ability.AllMovement, 0.8f, "MovementAgent"),
					AgentCapabilityProfile.EnvironmentalAgent => new AgentCapabilities(Polarity.HeatCold | Polarity.EarthWind, Ability.AllEnvironmental, 0.6f, "EnvironmentalAgent"),
					AgentCapabilityProfile.PolarityAgent => new AgentCapabilities(Polarity.Any, Ability.AllPolarity, 1f, "PolarityAgent"),
					AgentCapabilityProfile.MasterAgent => new AgentCapabilities(Polarity.Any, Ability.Everything, 1f, "MasterAgent"),
					_ => new AgentCapabilities()
					};
			}

		private GUIStyle GetLabelStyle(bool r)
			{
			var s = new GUIStyle(EditorStyles.label);
			s.normal.textColor = r ? Color.green : Color.red;
			s.fontSize = 10;
			return s;
			}

		private GUIStyle GetLinkLabelStyle(bool t)
			{
			var s = new GUIStyle(EditorStyles.miniLabel);
			s.normal.textColor = t ? Color.blue : Color.red;
			s.fontSize = 9;
			return s;
			}

		private Color GetPathColorByCapabilities(AgentCapabilities caps)
			{
			// Determine path color based on agent movement capabilities
			if ((caps.AvailableAbilities & Ability.Jump) != 0)
				{
				return Color.cyan;  // Jumping agents get cyan paths
				}

			if ((caps.AvailableAbilities & Ability.Dash) != 0)
				{
				return Color.yellow; // Dash agents get yellow paths
				}

			if ((caps.AvailableAbilities & Ability.Grapple) != 0)
				{
				return Color.magenta; // Grapple agents get magenta paths
				}

			return _currentColors[6]; // Default path color for basic agents
			}

		private PathVisualizationStyle GetPathStyleByCapabilities(AgentCapabilities caps)
			{
			// Complex movement capabilities get dashed lines to show they can take alternative routes
			return (caps.AvailableAbilities & (Ability.WallJump | Ability.Grapple)) != 0
				? PathVisualizationStyle.Dashed
				: PathVisualizationStyle.Solid;
			}

		private string GetCapabilityDisplayText(AgentCapabilities caps)
			{
			// Build a short text description of agent capabilities
			var abilities = new System.Collections.Generic.List<string>();

			if ((caps.AvailableAbilities & Ability.Jump) != 0)
				{
				abilities.Add("J");
				}

			if ((caps.AvailableAbilities & Ability.Dash) != 0)
				{
				abilities.Add("D");
				}

			if ((caps.AvailableAbilities & Ability.WallJump) != 0)
				{
				abilities.Add("WJ");
				}

			if ((caps.AvailableAbilities & Ability.Grapple) != 0)
				{
				abilities.Add("G");
				}

			if ((caps.AvailableAbilities & Ability.DoubleJump) != 0)
				{
				abilities.Add("DJ");
				}

			return abilities.Count > 0 ? string.Join(",", abilities) : "Basic";
			}

		private void DrawDashedLine(Vector3 start, Vector3 end)
			{
			// Draw a dashed line for complex movement paths
			Vector3 direction = (end - start).normalized;
			float distance = Vector3.Distance(start, end);
			float dashLength = 0.5f;
			float gapLength = 0.2f;
			float segmentLength = dashLength + gapLength;

			float currentDistance = 0f;
			while (currentDistance < distance)
				{
				Vector3 segmentStart = start + direction * currentDistance;
				Vector3 segmentEnd = start + direction * Mathf.Min(currentDistance + dashLength, distance);

				Gizmos.DrawLine(segmentStart, segmentEnd);
				currentDistance += segmentLength;
				}
			}
		}

	/// <summary>
	/// Navigation color scheme enumeration
	/// </summary>
	public enum NavigationColorScheme
		{
		Default = 0,
		HighContrast = 1
		}

	/// <summary>
	/// Path visualization style for different agent capabilities
	/// </summary>
	public enum PathVisualizationStyle
		{
		Solid = 0,
		Dashed = 1
		}

	/// <summary>
	/// Agent capability profiles for testing
	/// </summary>
	public enum AgentCapabilityProfile
		{
		BasicAgent = 0,
		MovementAgent = 1,
		EnvironmentalAgent = 2,
		PolarityAgent = 3,
		MasterAgent = 4
		}
	}
#endif
