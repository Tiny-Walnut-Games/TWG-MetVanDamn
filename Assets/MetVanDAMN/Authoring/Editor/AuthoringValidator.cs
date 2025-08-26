using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Entities;
using TinyWalnutGames.MetVD.Core;
using System.Collections.Generic;
using System.Linq;

namespace TinyWalnutGames.MetVD.Authoring.Editor
{
    /// <summary>
    /// Authoring validation system for detecting missing connections, unreferenced districts, and duplicate nodeIds
    /// Addresses TODOs: "Validation warnings for duplicate nodeIds" and "Authoring validation report"
    /// </summary>
    public static class AuthoringValidator
    {
        [System.Serializable]
        public class ValidationReport
        {
            public List<ValidationIssue> issues = new List<ValidationIssue>();
            public int errorCount;
            public int warningCount;
            public bool hasErrors => errorCount > 0;
            public bool hasWarnings => warningCount > 0;
        }

        [System.Serializable]
        public class ValidationIssue
        {
            public ValidationSeverity severity;
            public string category;
            public string message;
            public Object targetObject;
            public Vector3 worldPosition;

            public ValidationIssue(ValidationSeverity severity, string category, string message, Object target = null, Vector3 position = default)
            {
                this.severity = severity;
                this.category = category;
                this.message = message;
                this.targetObject = target;
                this.worldPosition = position;
            }
        }

        public enum ValidationSeverity
        {
            Error,
            Warning,
            Info
        }

        /// <summary>
        /// Validate all MetVanDAMN authoring components in the scene
        /// </summary>
        [MenuItem("Tools/MetVanDAMN/Validate Scene Authoring")]
        public static void ValidateSceneAuthoring()
        {
            var report = ValidateScene();
            DisplayValidationReport(report);
        }

        public static ValidationReport ValidateScene()
        {
            var report = new ValidationReport();
            
            // Find all authoring components
            var districtAuthorings = Object.FindObjectsOfType<DistrictAuthoring>();
            var connectionAuthorings = Object.FindObjectsOfType<ConnectionAuthoring>();
            var biomeAuthorings = Object.FindObjectsOfType<BiomeFieldAuthoring>();
            var gateAuthorings = Object.FindObjectsOfType<GateConditionAuthoring>();
            
            // Validate districts
            ValidateDistricts(districtAuthorings, report);
            
            // Validate connections  
            ValidateConnections(connectionAuthorings, districtAuthorings, report);
            
            // Validate biomes
            ValidateBiomes(biomeAuthorings, report);
            
            // Validate gates
            ValidateGates(gateAuthorings, connectionAuthorings, report);
            
            // Cross-validate relationships
            ValidateDistrictConnections(districtAuthorings, connectionAuthorings, report);
            
            // Count issues by severity
            report.errorCount = report.issues.Count(i => i.severity == ValidationSeverity.Error);
            report.warningCount = report.issues.Count(i => i.severity == ValidationSeverity.Warning);
            
            return report;
        }

        private static void ValidateDistricts(DistrictAuthoring[] districts, ValidationReport report)
        {
            var nodeIds = new HashSet<uint>();
            var duplicateIds = new HashSet<uint>();
            
            foreach (var district in districts)
            {
                if (district == null) continue;
                
                uint nodeId = district.nodeId.value;
                
                // Check for duplicate node IDs
                if (nodeIds.Contains(nodeId))
                {
                    duplicateIds.Add(nodeId);
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "Duplicate NodeId",
                        $"District NodeId {nodeId} is used by multiple districts. Each district must have a unique NodeId.",
                        district,
                        district.transform.position
                    ));
                }
                else
                {
                    nodeIds.Add(nodeId);
                }
                
                // Check for invalid node ID (0 is typically reserved)
                if (nodeId == 0)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Invalid NodeId", 
                        "District has NodeId 0, which may cause issues. Consider using a non-zero value.",
                        district,
                        district.transform.position
                    ));
                }
                
                // Check for missing sector count
                if (district.targetSectorCount <= 0)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "Invalid Sector Count",
                        "District has zero or negative target sector count.",
                        district,
                        district.transform.position
                    ));
                }
            }
            
            // Report summary of duplicate IDs
            foreach (uint duplicateId in duplicateIds)
            {
                var conflictingDistricts = districts.Where(d => d != null && d.nodeId.value == duplicateId).ToArray();
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "NodeId Conflict Summary",
                    $"NodeId {duplicateId} is used by {conflictingDistricts.Length} districts: {string.Join(", ", conflictingDistricts.Select(d => d.name))}"
                ));
            }
        }

        private static void ValidateConnections(ConnectionAuthoring[] connections, DistrictAuthoring[] districts, ValidationReport report)
        {
            var validNodeIds = new HashSet<uint>(districts.Where(d => d != null).Select(d => d.nodeId.value));
            
            foreach (var connection in connections)
            {
                if (connection == null) continue;
                
                // Check if source district exists
                if (!validNodeIds.Contains(connection.sourceNode.value))
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "Missing Source District",
                        $"Connection references source NodeId {connection.sourceNode.value} but no district with this ID exists.",
                        connection,
                        connection.transform.position
                    ));
                }
                
                // Check if target district exists
                if (!validNodeIds.Contains(connection.targetNode.value))
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "Missing Target District", 
                        $"Connection references target NodeId {connection.targetNode.value} but no district with this ID exists.",
                        connection,
                        connection.transform.position
                    ));
                }
                
                // Check for self-referencing connections
                if (connection.sourceNode.value == connection.targetNode.value)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Self-Referencing Connection",
                        "Connection has the same source and target district.",
                        connection,
                        connection.transform.position
                    ));
                }
            }
        }

        private static void ValidateBiomes(BiomeFieldAuthoring[] biomes, ValidationReport report)
        {
            foreach (var biome in biomes)
            {
                if (biome == null) continue;
                
                // Check if biome has valid art profile
                if (biome.artProfile == null)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Missing Art Profile",
                        "BiomeField has no art profile assigned. Visual generation may be incomplete.",
                        biome,
                        biome.transform.position
                    ));
                }
                else
                {
                    // Validate art profile content
                    var profile = biome.artProfile;
                    if (profile.tiles == null || profile.tiles.Length == 0)
                    {
                        report.issues.Add(new ValidationIssue(
                            ValidationSeverity.Warning,
                            "Empty Tile Array",
                            "BiomeArtProfile has no tiles configured.",
                            profile
                        ));
                    }
                    
                    if (profile.propPrefabs == null || profile.propPrefabs.Length == 0)
                    {
                        report.issues.Add(new ValidationIssue(
                            ValidationSeverity.Info,
                            "No Props Configured",
                            "BiomeArtProfile has no prop prefabs. Only tiles will be generated.",
                            profile
                        ));
                    }
                }
                
                // Check biome field size
                if (biome.fieldRadius <= 0)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "Invalid Biome Radius",
                        "BiomeField has zero or negative radius.",
                        biome,
                        biome.transform.position
                    ));
                }
            }
        }

        private static void ValidateGates(GateConditionAuthoring[] gates, ConnectionAuthoring[] connections, ValidationReport report)
        {
            var connectionNodePairs = new HashSet<(uint, uint)>();
            foreach (var connection in connections.Where(c => c != null))
            {
                connectionNodePairs.Add((connection.sourceNode.value, connection.targetNode.value));
            }
            
            foreach (var gate in gates)
            {
                if (gate == null) continue;
                
                // Check if gate has valid connection reference
                bool hasValidConnection = connectionNodePairs.Contains((gate.sourceNode.value, gate.targetNode.value));
                
                if (!hasValidConnection)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Orphaned Gate Condition",
                        $"Gate condition exists for connection {gate.sourceNode.value} -> {gate.targetNode.value} but no such connection exists.",
                        gate,
                        gate.transform.position
                    ));
                }
                
                // Check gate condition description length
                if (string.IsNullOrEmpty(gate.description.ToString()))
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Info,
                        "Empty Gate Description",
                        "Gate condition has no description.",
                        gate,
                        gate.transform.position
                    ));
                }
            }
        }

        private static void ValidateDistrictConnections(DistrictAuthoring[] districts, ConnectionAuthoring[] connections, ValidationReport report)
        {
            var districtNodeIds = new HashSet<uint>(districts.Where(d => d != null).Select(d => d.nodeId.value));
            var connectedNodes = new HashSet<uint>();
            
            // Track which districts have connections
            foreach (var connection in connections.Where(c => c != null))
            {
                connectedNodes.Add(connection.sourceNode.value);
                connectedNodes.Add(connection.targetNode.value);
            }
            
            // Find isolated districts (no connections)
            foreach (var district in districts.Where(d => d != null))
            {
                if (!connectedNodes.Contains(district.nodeId.value))
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Isolated District",
                        "District has no connections to other districts. It may be unreachable.",
                        district,
                        district.transform.position
                    ));
                }
            }
            
            // Check for connection density (too few or too many connections)
            float connectionRatio = connections.Length / (float)Mathf.Max(1, districts.Length);
            if (connectionRatio < 0.5f)
            {
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    "Low Connection Density",
                    $"Connection to district ratio is {connectionRatio:F2}. Consider adding more connections for better connectivity."
                ));
            }
            else if (connectionRatio > 3.0f)
            {
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    "High Connection Density", 
                    $"Connection to district ratio is {connectionRatio:F2}. Consider reducing connections to avoid overcomplexity."
                ));
            }
        }

        private static void DisplayValidationReport(ValidationReport report)
        {
            string title = "MetVanDAMN Authoring Validation Report";
            string message;
            
            if (report.issues.Count == 0)
            {
                message = "✅ All authoring components are valid! No issues found.";
                EditorUtility.DisplayDialog(title, message, "OK");
                return;
            }
            
            message = $"Found {report.issues.Count} issues:\n" +
                     $"• {report.errorCount} errors\n" +
                     $"• {report.warningCount} warnings\n" +
                     $"• {report.issues.Count - report.errorCount - report.warningCount} info items\n\n";
            
            if (report.hasErrors)
            {
                message += "⚠️ Errors must be fixed before the scene can generate properly.\n\n";
            }
            
            // Show first few issues in dialog
            int maxShow = 5;
            for (int i = 0; i < Mathf.Min(maxShow, report.issues.Count); i++)
            {
                var issue = report.issues[i];
                string severityIcon = issue.severity == ValidationSeverity.Error ? "❌" :
                                    issue.severity == ValidationSeverity.Warning ? "⚠️" : "ℹ️";
                message += $"{severityIcon} {issue.category}: {issue.message}\n";
            }
            
            if (report.issues.Count > maxShow)
            {
                message += $"... and {report.issues.Count - maxShow} more issues.";
            }
            
            message += "\n\nSee Console for detailed report.";
            
            EditorUtility.DisplayDialog(title, message, "OK");
            
            // Log detailed report to console
            LogDetailedReport(report);
        }

        private static void LogDetailedReport(ValidationReport report)
        {
            Debug.Log($"=== MetVanDAMN Authoring Validation Report ===");
            Debug.Log($"Total Issues: {report.issues.Count} ({report.errorCount} errors, {report.warningCount} warnings)");
            
            var groupedIssues = report.issues.GroupBy(i => i.category);
            
            foreach (var group in groupedIssues.OrderBy(g => g.Key))
            {
                Debug.Log($"\n--- {group.Key} ({group.Count()} issues) ---");
                
                foreach (var issue in group)
                {
                    string logMessage = $"{issue.severity}: {issue.message}";
                    if (issue.targetObject != null)
                    {
                        logMessage += $" (Object: {issue.targetObject.name})";
                    }
                    
                    switch (issue.severity)
                    {
                        case ValidationSeverity.Error:
                            Debug.LogError(logMessage, issue.targetObject);
                            break;
                        case ValidationSeverity.Warning:
                            Debug.LogWarning(logMessage, issue.targetObject);
                            break;
                        default:
                            Debug.Log(logMessage, issue.targetObject);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Quick validation check for use in play mode or before baking
        /// </summary>
        public static bool QuickValidate()
        {
            var report = ValidateScene();
            return !report.hasErrors;
        }
    }
}