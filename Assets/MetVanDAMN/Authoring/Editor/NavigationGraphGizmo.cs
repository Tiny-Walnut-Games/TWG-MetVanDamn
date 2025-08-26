#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using TinyWalnutGames.MetVD.Core;

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
        [SerializeField] private bool showNodeLabels = true;
        [SerializeField] private bool showLinkCosts = true;
        [SerializeField] private bool showGateRequirements = true;
        [SerializeField] private bool showUnreachableAreas = true;

        [Header("Visual Configuration")]
        [SerializeField] private float nodeRadius = 0.5f;
        [SerializeField] private float linkWidth = 2.0f;
        [SerializeField] private float labelOffset = 1.0f;

        [Header("Agent Testing")]
        [SerializeField] private AgentCapabilityProfile testAgentProfile = AgentCapabilityProfile.BasicAgent;
        [SerializeField] private uint highlightPathFromNode = 0;
        [SerializeField] private uint highlightPathToNode = 0;

        [Header("Color Scheme")]
        [SerializeField] private NavigationColorScheme colorScheme = NavigationColorScheme.Default;

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
            new Color(0.2f, 0.8f, 0.2f),  // Bright green
            new Color(0.8f, 0.2f, 0.2f),  // Bright red
            new Color(0.2f, 0.4f, 0.8f),  // Deep blue
            new Color(1.0f, 0.6f, 0.2f),  // Orange
            new Color(0.8f, 0.2f, 0.8f),  // Magenta
            new Color(0.9f, 0.9f, 0.2f),  // Bright yellow
            new Color(0.2f, 0.9f, 0.9f)   // Bright cyan
        };

        private Color[] _currentColors;
        private World _world;
        private EntityManager _entityManager;

        /// <summary>
        /// Menu item to create a navigation graph gizmo in the scene
        /// </summary>
        [MenuItem("Tools/MetVanDAMN/Create Navigation Graph Gizmo")]
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
        [MenuItem("Tools/MetVanDAMN/Toggle Navigation Graph Visualization")]
        public static void ToggleNavigationGraphVisualization()
        {
            var gizmos = FindObjectsOfType<NavigationGraphGizmo>();
            bool newState = true;
            
            if (gizmos.Length > 0)
            {
                newState = !gizmos[0].showNavigationGraph;
            }
            
            foreach (var gizmo in gizmos)
            {
                gizmo.showNavigationGraph = newState;
            }
            
            Debug.Log($"Navigation graph visualization {(newState ? "enabled" : "disabled")} for {gizmos.Length} gizmos.");
        }

        /// <summary>
        /// Menu item to validate navigation connectivity and show results
        /// </summary>
        [MenuItem("Tools/MetVanDAMN/Validate Navigation Connectivity")]
        public static void ValidateNavigationConnectivity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                Debug.LogWarning("No world available for navigation validation.");
                return;
            }

            var report = NavigationValidationUtility.GenerateValidationReport(world);
            
            try
            {
                var title = "Navigation Connectivity Report";
                var message = $"Navigation Graph Status:\n" +
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
                        var issue = report.Issues[i];
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
                return;

            DrawNavigationGraph();
        }

        private void OnDrawGizmosSelected()
        {
            if (!showNavigationGraph || _world == null || !_world.IsCreated)
                return;

            DrawNavigationGraph();
            DrawDetailedInformation();
        }

        private void DrawNavigationGraph()
        {
            var navGraphQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NavigationGraph>());
            if (navGraphQuery.IsEmpty)
                return;

            var navGraph = navGraphQuery.GetSingleton<NavigationGraph>();
            if (!navGraph.IsReady)
                return;

            // Get test agent capabilities
            var testCapabilities = GetTestAgentCapabilities();

            // Draw navigation nodes
            DrawNavigationNodes(testCapabilities);

            // Draw navigation links
            DrawNavigationLinks(testCapabilities);

            // Draw highlighted path if specified
            if (highlightPathFromNode != 0 && highlightPathToNode != 0)
            {
                DrawHighlightedPath(highlightPathFromNode, highlightPathToNode, testCapabilities);
            }
        }

        private void DrawNavigationNodes(AgentCapabilities testCapabilities)
        {
            var navNodeQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NavNode>(),
                ComponentType.ReadOnly<NodeId>());

            using var entities = navNodeQuery.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var navNode = _entityManager.GetComponentData<NavNode>(entity);
                var nodeId = _entityManager.GetComponentData<NodeId>(entity);

                var worldPos = navNode.WorldPosition;
                var isReachable = navNode.IsCompatibleWith(testCapabilities);

                // Choose color based on reachability
                Gizmos.color = isReachable ? _currentColors[0] : _currentColors[1];

                // Draw node sphere
                Gizmos.DrawWireSphere(worldPos, nodeRadius);
                
                if (!isReachable && showUnreachableAreas)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(worldPos, nodeRadius * 0.3f);
                }

                // Draw node labels
                if (showNodeLabels)
                {
                    var labelPos = worldPos + new float3(0, labelOffset, 0);
                    var labelText = $"N{nodeId.Value}\n{navNode.BiomeType}\n{navNode.PrimaryPolarity}";
                    
                    Handles.Label(labelPos, labelText, GetLabelStyle(isReachable));
                }
            }
        }

        private void DrawNavigationLinks(AgentCapabilities testCapabilities)
        {
            var linkQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NavNode>(),
                ComponentType.ReadOnly<NavLinkBufferElement>());

            using var entities = linkQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var navNode = _entityManager.GetComponentData<NavNode>(entity);
                var linkBuffer = _entityManager.GetBuffer<NavLinkBufferElement>(entity);

                for (int j = 0; j < linkBuffer.Length; j++)
                {
                    var link = linkBuffer[j].Value;
                    DrawNavigationLink(navNode, link, testCapabilities);
                }
            }
        }

        private void DrawNavigationLink(NavNode sourceNode, NavLink link, AgentCapabilities testCapabilities)
        {
            var targetEntity = FindEntityByNodeId(link.ToNodeId);
            if (targetEntity == Entity.Null || !_entityManager.HasComponent<NavNode>(targetEntity))
                return;

            var targetNode = _entityManager.GetComponentData<NavNode>(targetEntity);
            var canTraverse = link.CanTraverseWith(testCapabilities, sourceNode.NodeId);
            var traversalCost = link.CalculateTraversalCost(testCapabilities);

            // Choose link color based on gate requirements and traversability
            Color linkColor;
            if (link.RequiredPolarity != Polarity.None || link.RequiredAbilities != Ability.None)
            {
                linkColor = link.GateSoftness == GateSoftness.Hard ? _currentColors[4] : _currentColors[5];
            }
            else
            {
                linkColor = _currentColors[2];
            }

            if (!canTraverse)
            {
                linkColor = Color.gray;
            }

            // Draw link line
            Gizmos.color = linkColor;
            var startPos = sourceNode.WorldPosition;
            var endPos = targetNode.WorldPosition;
            
            // Draw arrow for directional links
            if (link.ConnectionType != ConnectionType.Bidirectional)
            {
                DrawArrowLine(startPos, endPos, linkWidth * 0.01f);
            }
            else
            {
                Gizmos.DrawLine(startPos, endPos);
            }

            // Draw link cost labels
            if (showLinkCosts)
            {
                var midPos = (startPos + endPos) * 0.5f;
                var costText = $"Cost: {traversalCost:F1}";
                
                if (showGateRequirements && (link.RequiredPolarity != Polarity.None || link.RequiredAbilities != Ability.None))
                {
                    costText += $"\n{link.RequiredPolarity}";
                    if (link.RequiredAbilities != Ability.None)
                        costText += $"\n{link.RequiredAbilities}";
                }

                Handles.Label(midPos, costText, GetLinkLabelStyle(canTraverse));
            }
        }

        private void DrawHighlightedPath(uint fromNodeId, uint toNodeId, AgentCapabilities testCapabilities)
        {
            // This would integrate with AINavigationSystem to get actual path
            // For now, draw a simple direct line as placeholder
            var fromEntity = FindEntityByNodeId(fromNodeId);
            var toEntity = FindEntityByNodeId(toNodeId);
            
            if (fromEntity == Entity.Null || toEntity == Entity.Null)
                return;

            var fromNode = _entityManager.GetComponentData<NavNode>(fromEntity);
            var toNode = _entityManager.GetComponentData<NavNode>(toEntity);

            Gizmos.color = _currentColors[6];
            Gizmos.DrawLine(fromNode.WorldPosition, toNode.WorldPosition);
            
            // Draw path cost estimate
            var midPos = (fromNode.WorldPosition + toNode.WorldPosition) * 0.5f;
            var distance = math.distance(fromNode.WorldPosition, toNode.WorldPosition);
            Handles.Label(midPos, $"Path: {distance:F1}", EditorStyles.boldLabel);
        }

        private void DrawDetailedInformation()
        {
            if (_world == null || !_world.IsCreated)
                return;

            var navGraphQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NavigationGraph>());
            if (navGraphQuery.IsEmpty)
                return;

            var navGraph = navGraphQuery.GetSingleton<NavigationGraph>();
            
            // Draw information panel in scene view
            Handles.BeginGUI();
            
            var rect = new Rect(10, 10, 300, 150);
            GUI.Box(rect, "Navigation Graph Info");
            
            var contentRect = new Rect(rect.x + 10, rect.y + 20, rect.width - 20, rect.height - 30);
            
            var infoText = $"Nodes: {navGraph.NodeCount}\n" +
                          $"Links: {navGraph.LinkCount}\n" +
                          $"Ready: {navGraph.IsReady}\n" +
                          $"Unreachable Areas: {navGraph.UnreachableAreaCount}\n" +
                          $"Test Agent: {testAgentProfile}\n" +
                          $"Last Rebuild: {navGraph.LastRebuildTime:F2}s";
            
            GUI.Label(contentRect, infoText);
            
            Handles.EndGUI();
        }

        private void DrawArrowLine(float3 start, float3 end, float arrowSize)
        {
            Gizmos.DrawLine(start, end);
            
            var direction = math.normalize(end - start);
            var right = math.cross(direction, new float3(0, 1, 0));
            var arrowHead1 = end - direction * arrowSize + right * arrowSize * 0.5f;
            var arrowHead2 = end - direction * arrowSize - right * arrowSize * 0.5f;
            
            Gizmos.DrawLine(end, arrowHead1);
            Gizmos.DrawLine(end, arrowHead2);
        }

        private Entity FindEntityByNodeId(uint nodeId)
        {
            var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeId>());
            using var entities = query.ToEntityArray(Allocator.Temp);
            using var nodeIds = query.ToComponentDataArray<NodeId>(Allocator.Temp);
            
            for (int i = 0; i < nodeIds.Length; i++)
            {
                if (nodeIds[i].Value == nodeId)
                    return entities[i];
            }
            
            return Entity.Null;
        }

        private AgentCapabilities GetTestAgentCapabilities()
        {
            return testAgentProfile switch
            {
                AgentCapabilityProfile.BasicAgent => new AgentCapabilities(Polarity.None, Ability.None, 0.0f, "BasicAgent"),
                AgentCapabilityProfile.MovementAgent => new AgentCapabilities(Polarity.None, Ability.AllMovement, 0.8f, "MovementAgent"),
                AgentCapabilityProfile.EnvironmentalAgent => new AgentCapabilities(Polarity.HeatCold | Polarity.EarthWind, Ability.AllEnvironmental, 0.6f, "EnvironmentalAgent"),
                AgentCapabilityProfile.PolarityAgent => new AgentCapabilities(Polarity.Any, Ability.AllPolarity, 1.0f, "PolarityAgent"),
                AgentCapabilityProfile.MasterAgent => new AgentCapabilities(Polarity.Any, Ability.Everything, 1.0f, "MasterAgent"),
                _ => new AgentCapabilities()
            };
        }

        private GUIStyle GetLabelStyle(bool isReachable)
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = isReachable ? Color.green : Color.red;
            style.fontSize = 10;
            return style;
        }

        private GUIStyle GetLinkLabelStyle(bool canTraverse)
        {
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = canTraverse ? Color.blue : Color.red;
            style.fontSize = 9;
            return style;
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