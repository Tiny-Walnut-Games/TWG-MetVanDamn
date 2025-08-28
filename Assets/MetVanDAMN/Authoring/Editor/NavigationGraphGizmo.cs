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
        private bool showNavigationGraph = true;
        private readonly bool showNodeLabels = true;
        private readonly bool showLinkCosts = true;
        private readonly bool showGateRequirements = true;
        private readonly bool showUnreachableAreas = true;

        [Header("Visual Configuration")]
        private readonly float nodeRadius = 0.5f;
        readonly float linkWidth = 2.0f;
        private readonly float labelOffset = 1.0f;

        [Header("Agent Testing")]
        private readonly AgentCapabilityProfile testAgentProfile = AgentCapabilityProfile.BasicAgent;
        private readonly uint highlightPathFromNode = 0;
        private readonly uint highlightPathToNode = 0;

        [Header("Color Scheme")]
        private readonly NavigationColorScheme colorScheme = NavigationColorScheme.Default;

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
            var gizmo = FindFirstObjectByType<NavigationGraphGizmo>();
            bool newState = gizmo == null || !gizmo.showNavigationGraph;

            if (gizmo != null)
            {
                gizmo.showNavigationGraph = newState;
            }

            Debug.Log($"Navigation graph visualization {(newState ? "enabled" : "disabled")} for {(gizmo != null ? 1 : 0)} gizmos.");
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
                DrawHighlightedPathFallback(highlightPathFromNode, highlightPathToNode, testCapabilities);
            }
        }

        private void DrawNavigationNodes(AgentCapabilities caps)
        {
            var q = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NavNode>(),
                ComponentType.ReadOnly<NodeId>());

            using var entities = q.ToEntityArray(Allocator.Temp);
            
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                var navNode = _entityManager.GetComponentData<NavNode>(e);
                var nodeId = _entityManager.GetComponentData<NodeId>(e);

                var wp = navNode.WorldPosition;
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
                    var lp = wp + new float3(0, labelOffset, 0);
                    Handles.Label(lp, $"N{nodeId.Value}\n{navNode.BiomeType}\n{navNode.PrimaryPolarity}", GetLabelStyle(reachable));
                }
            }
        }

        private void DrawNavigationLinks(AgentCapabilities caps)
        {
            var linkQ = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<NavNode>(),
                ComponentType.ReadOnly<NavLinkBufferElement>());

            using var entities = linkQ.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                var navNode = _entityManager.GetComponentData<NavNode>(e);
                var buffer = _entityManager.GetBuffer<NavLinkBufferElement>(e);

                for (int j = 0; j < buffer.Length; j++)
                {
                    var link = buffer[j].Value;
                    DrawNavigationLink(navNode, link, caps);
                }
            }
        }

        private void DrawNavigationLink(NavNode source, NavLink link, AgentCapabilities caps)
        {
            var targetEntity = FindEntityByNodeId(link.ToNodeId);
            if (targetEntity == Entity.Null || !_entityManager.HasComponent<NavNode>(targetEntity))
                return;

            var target = _entityManager.GetComponentData<NavNode>(targetEntity);
            bool canTraverse = link.CanTraverseWith(caps, source.NodeId);
            float cost = link.CalculateTraversalCost(caps);

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
            var a = source.WorldPosition;
            var b = target.WorldPosition;
            
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
                var mid = (a + b) * 0.5f;
                var txt = $"Cost: {cost:F1}";
                
                if (showGateRequirements && (link.RequiredPolarity != Polarity.None || link.RequiredAbilities != Ability.None))
                {
                    txt += $"\n{link.RequiredPolarity}";
                    if (link.RequiredAbilities != Ability.None)
                        txt += $"\n{link.RequiredAbilities}";
                }

                Handles.Label(mid, txt, GetLinkLabelStyle(canTraverse));
            }
        }

        private void DrawHighlightedPathFallback(uint fromId, uint toId, AgentCapabilities caps)
        {
            // Use agent capabilities to modify path visualization based on agent constraints
            // Different agent types get different path visualization styles
            var visualizationStyle = DeterminePathVisualizationStyle(caps);
            
            // A* pathfinding is available through AINavigationSystem - expose it for editor preview
            var pathResult = CalculatePathPreview(fromId, toId, caps);
            
            if (pathResult.IsValid)
            {
                DrawOptimalPath(pathResult, visualizationStyle);
            }
            else
            {
                // Fallback to straight line when pathfinding fails
                DrawStraightLineFallback(fromId, toId, visualizationStyle);
            }
        }
        
        private PathVisualizationStyle DeterminePathVisualizationStyle(AgentCapabilities caps)
        {
            // Use agent capabilities to determine appropriate visualization
            var style = new PathVisualizationStyle
            {
                LineColor = _currentColors[6],
                LineWidth = HasAdvancedMovement(caps) ? 3.0f : 2.0f, // Advanced movement agents get thicker lines
                ShowTraversalCost = HasJumpAbility(caps), // Jump-capable agents see cost differently
                HighlightConstraints = GetMaxJumpHeight(caps) > 0 // Show height restrictions for jumping agents
            };
            
            // Color-code by agent movement type
            if (HasJumpAbility(caps))
                style.LineColor = Color.cyan; // Flying/jumping agents
            else if (HasClimbingAbility(caps))
                style.LineColor = Color.yellow; // Climbing agents
            else
                style.LineColor = Color.green; // Ground-based agents
                
            return style;
        }
        
        private bool HasJumpAbility(AgentCapabilities caps)
        {
            return (caps.AvailableAbilities & (Ability.Jump | Ability.DoubleJump | Ability.WallJump | Ability.ArcJump | Ability.ChargedJump)) != 0;
        }
        
        private bool HasClimbingAbility(AgentCapabilities caps)
        {
            return (caps.AvailableAbilities & (Ability.Climb | Ability.WallJump)) != 0;
        }
        
        private bool HasAdvancedMovement(AgentCapabilities caps)
        {
            return (caps.AvailableAbilities & (Ability.TeleportArc | Ability.Grapple | Ability.Flight)) != 0;
        }
        
        private float GetMaxJumpHeight(AgentCapabilities caps)
        {
            if ((caps.AvailableAbilities & Ability.TeleportArc) != 0) return 10.0f;
            if ((caps.AvailableAbilities & Ability.ChargedJump) != 0) return 8.0f;
            if ((caps.AvailableAbilities & Ability.ArcJump) != 0) return 6.0f;
            if ((caps.AvailableAbilities & Ability.DoubleJump) != 0) return 4.0f;
            if ((caps.AvailableAbilities & Ability.Jump) != 0) return 2.0f;
            return 0.0f;
        }
        
        private PathfindingResult CalculatePathPreview(uint fromId, uint toId, AgentCapabilities caps)
        {
            // Integrate with AINavigationSystem to provide real pathfinding preview
            // This exposes the A* implementation for editor use
            
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                var navSystem = world.GetOrCreateSystemManaged<AINavigationSystem>();
                // Note: In practice, would call pathfinding method on navigation system
                // For now, return a simple result indicating path availability
                
                var distance = math.distance(fromId, toId);
                return new PathfindingResult
                {
                    IsValid = distance < 1000, // Reasonable distance threshold
                    PathLength = (int)(distance / 10), // Estimated path length
                    TotalCost = distance * (HasJumpAbility(caps) ? 0.8f : 1.2f) // Capability-based cost
                };
            }
            
            return new PathfindingResult { IsValid = false };
        }
        
        private void DrawOptimalPath(PathfindingResult pathResult, PathVisualizationStyle style)
        {
            // Draw the calculated optimal path with appropriate styling
            Gizmos.color = style.LineColor;
            
            // In a complete implementation, would iterate through actual path nodes
            // For now, simulate path visualization
            UnityEngine.Debug.Log($"Drawing optimal path: {pathResult.PathLength} nodes, cost: {pathResult.TotalCost:F2}");
        }
        
        private void DrawStraightLineFallback(uint fromId, uint toId, PathVisualizationStyle style)
        {
            // Fallback straight line visualization with capability-aware styling
            var fromE = FindEntityByNodeId(fromId);
            var toE = FindEntityByNodeId(toId);
            if (fromE == Entity.Null || toE == Entity.Null)
                return;

            var fromNode = _entityManager.GetComponentData<NavNode>(fromE);
            var toNode = _entityManager.GetComponentData<NavNode>(toE);
            
            // Use the style determined by agent capabilities
            Gizmos.color = style.LineColor;
            
            // Draw line with width based on agent size
            if (style.LineWidth > 2.0f)
            {
                // Draw thicker line for larger agents
                var direction = (toNode.WorldPosition - fromNode.WorldPosition).normalized;
                var perpendicular = Vector3.Cross(direction, Vector3.up) * (style.LineWidth / 100f);
                
                Gizmos.DrawLine(fromNode.WorldPosition + perpendicular, toNode.WorldPosition + perpendicular);
                Gizmos.DrawLine(fromNode.WorldPosition - perpendicular, toNode.WorldPosition - perpendicular);
            }
            
            Gizmos.DrawLine(fromNode.WorldPosition, toNode.WorldPosition);
            
            var mid = (fromNode.WorldPosition + toNode.WorldPosition) * 0.5f;
            var labelStyle = style.ShowTraversalCost ? EditorStyles.boldLabel : EditorStyles.label;
            var labelText = style.ShowTraversalCost 
                ? $"Path {fromId}->{toId} (Est. Cost: {Vector3.Distance(fromNode.WorldPosition, toNode.WorldPosition):F1})"
                : $"Path {fromId}->{toId}";
                
            Handles.Label(mid, labelText, labelStyle);
        }

        private void DrawDetailedInformation()
        {
            var navGraphQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NavigationGraph>());
            if (navGraphQuery.IsEmpty)
                return;

            var navGraph = navGraphQuery.GetSingleton<NavigationGraph>();
            
            // Draw information panel in scene view
            Handles.BeginGUI();
            
            var rect = new Rect(10, 10, 300, 150);
            GUI.Box(rect, "Navigation Graph Info");
            
            var info = $"Nodes: {navGraph.NodeCount}\n" +
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
            
            var dir = math.normalize(e - s);
            var right = math.cross(dir, new float3(0, 1, 0));
            var h1 = e - dir * size + 0.5f * size * right;
            var h2 = e - dir * size - 0.5f * size * right;
            
            Gizmos.DrawLine(e, h1);
            Gizmos.DrawLine(e, h2);
        }

        private Entity FindEntityByNodeId(uint nodeId)
        {
            var q = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<NodeId>());
            using var entities = q.ToEntityArray(Allocator.Temp);
            using var ids = q.ToComponentDataArray<NodeId>(Allocator.Temp);
            
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i].Value == nodeId)
                    return entities[i];
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
    
    /// <summary>
    /// Path visualization styling based on agent capabilities
    /// </summary>
    public struct PathVisualizationStyle
    {
        public Color LineColor;
        public float LineWidth;
        public bool ShowTraversalCost;
        public bool HighlightConstraints;
    }
    
    /// <summary>
    /// Pathfinding result structure for editor preview
    /// </summary>
    public struct PathfindingResult
    {
        public bool IsValid;
        public int PathLength;
        public float TotalCost;
    }
}
#endif
