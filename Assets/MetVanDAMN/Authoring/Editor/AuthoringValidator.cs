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
            
            // Validate gates with enhanced condition checking
            ValidateGateConditions(gateAuthorings, connectionAuthorings, report);
            
            // Cross-validate relationships
            ValidateDistrictConnections(districtAuthorings, connectionAuthorings, report);
            
            // Enhanced orphaned asset detection with project scanning
            ValidateOrphanedAssets(report);
            
            // Auto-fix suggestions for missing NodeIds
            SuggestAutoFixes(districtAuthorings, biomeAuthorings, report);
            
            // Validate navigation graph connectivity and reachability
            ValidateNavigationConnectivity(report);
            
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

        private static void ValidateGateConditions(GateAuthoring[] gates, ConnectionAuthoring[] connections, ValidationReport report)
        {
            // Enhanced gate condition validation with connection reference checking
            var validConnectionIds = new HashSet<uint>(connections.Where(c => c != null).Select(c => c.connectionId.value));
            
            foreach (var gate in gates)
            {
                if (gate == null) continue;
                
                // Validate gate conditions reference existing connections
                foreach (var condition in gate.gateConditions)
                {
                    if (condition.requiredConnectionId.value != 0 && !validConnectionIds.Contains(condition.requiredConnectionId.value))
                    {
                        report.issues.Add(new ValidationIssue(
                            ValidationSeverity.Error,
                            "Invalid Gate Condition Reference",
                            $"Gate condition references connection ID {condition.requiredConnectionId.value} which does not exist.",
                            gate,
                            gate.transform.position
                        ));
                    }
                    
                    // Check for circular dependencies in gate conditions
                    if (condition.requiredConnectionId.value == gate.connectionId.value)
                    {
                        report.issues.Add(new ValidationIssue(
                            ValidationSeverity.Warning,
                            "Circular Gate Dependency",
                            "Gate condition references its own connection, creating a circular dependency.",
                            gate,
                            gate.transform.position
                        ));
                    }
                }
                
                // Validate gate activation logic consistency
                if (gate.gateConditions.Length > 0)
                {
                    bool hasValidCondition = false;
                    foreach (var condition in gate.gateConditions)
                    {
                        if (condition.requiredConnectionId.value != 0 || condition.isDefault)
                        {
                            hasValidCondition = true;
                            break;
                        }
                    }
                    
                    if (!hasValidCondition)
                    {
                        report.issues.Add(new ValidationIssue(
                            ValidationSeverity.Warning,
                            "Ineffective Gate Configuration",
                            "Gate has conditions but none are properly configured (no valid connection IDs or default condition).",
                            gate,
                            gate.transform.position
                        ));
                    }
                }
                
                // Check for duplicate conditions
                var conditionConnectionIds = gate.gateConditions
                    .Where(c => c.requiredConnectionId.value != 0)
                    .Select(c => c.requiredConnectionId.value)
                    .ToArray();
                    
                var duplicateConditions = conditionConnectionIds.GroupBy(id => id)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);
                    
                foreach (var duplicateId in duplicateConditions)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Duplicate Gate Condition",
                        $"Gate has multiple conditions referencing the same connection ID {duplicateId}.",
                        gate,
                        gate.transform.position
                    ));
                }
            }
        }

        private static void ValidateOrphanedAssets(ValidationReport report)
        {
            // Enhanced orphaned asset detection with project asset scanning
            var biomeProfiles = UnityEditor.AssetDatabase.FindAssets("t:BiomeArtProfile")
                .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<BiomeArtProfile>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                .Where(profile => profile != null)
                .ToArray();
                
            var districtAuthorings = Object.FindObjectsByType<DistrictAuthoring>(FindObjectsSortMode.None);
            var biomeAuthorings = Object.FindObjectsByType<BiomeFieldAuthoring>(FindObjectsSortMode.None);
            
            // Find biome profiles not linked to any biome
            var usedProfiles = new HashSet<BiomeArtProfile>(biomeAuthorings
                .Where(b => b.artProfile != null)
                .Select(b => b.artProfile));
                
            foreach (var profile in biomeProfiles)
            {
                if (!usedProfiles.Contains(profile))
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Info,
                        "Orphaned Biome Profile",
                        $"BiomeArtProfile '{profile.name}' exists in project but is not linked to any BiomeField.",
                        profile
                    ));
                }
            }
            
            // Find prop prefabs not used in any biome profile
            var allPropPrefabs = biomeProfiles
                .SelectMany(p => p.propPrefabs ?? new GameObject[0])
                .Where(prefab => prefab != null)
                .ToHashSet();
                
            var allPrefabsInProject = UnityEditor.AssetDatabase.FindAssets("t:GameObject")
                .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                .Where(go => go != null && go.GetComponent<Renderer>() != null) // Likely to be props
                .ToArray();
                
            foreach (var prefab in allPrefabsInProject)
            {
                if (!allPropPrefabs.Contains(prefab) && IsLikelyPropPrefab(prefab))
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Info,
                        "Potentially Orphaned Prop",
                        $"GameObject '{prefab.name}' appears to be a prop but is not used in any BiomeArtProfile.",
                        prefab
                    ));
                }
            }
            
            // Find tile assets not used in any biome profile
            var allTilesInProfiles = biomeProfiles
                .SelectMany(p => p.tiles ?? new TileBase[0])
                .Where(tile => tile != null)
                .ToHashSet();
                
            var allTilesInProject = UnityEditor.AssetDatabase.FindAssets("t:TileBase")
                .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                .Where(tile => tile != null)
                .ToArray();
                
            foreach (var tile in allTilesInProject)
            {
                if (!allTilesInProfiles.Contains(tile))
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Info,
                        "Orphaned Tile Asset",
                        $"Tile '{tile.name}' exists in project but is not used in any BiomeArtProfile.",
                        tile
                    ));
                }
            }
        }
        
        private static bool IsLikelyPropPrefab(GameObject prefab)
        {
            // Heuristics to determine if a GameObject is likely a prop
            if (prefab.GetComponent<Renderer>() == null) return false;
            if (prefab.GetComponent<Collider>() != null) return true; // Props often have colliders
            if (prefab.name.ToLowerInvariant().Contains("prop")) return true;
            if (prefab.name.ToLowerInvariant().Contains("decoration")) return true;
            if (prefab.name.ToLowerInvariant().Contains("furniture")) return true;
            if (prefab.name.ToLowerInvariant().Contains("plant")) return true;
            
            // Check if it's in a typical props folder
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(prefab);
            if (assetPath.ToLowerInvariant().Contains("prop")) return true;
            if (assetPath.ToLowerInvariant().Contains("decoration")) return true;
            
            return false;
        }

        private static void ValidateBiomes(BiomeFieldAuthoring[] biomes, ValidationReport report)
        {
            var biomeNodeIds = new HashSet<uint>();
            
            foreach (var biome in biomes)
            {
                if (biome == null) continue;
                
                // Check for valid NodeId assignment
                if (biome.nodeId.value == 0)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "Invalid Biome Assignment",
                        "BiomeField has invalid or missing NodeId assignment.",
                        biome,
                        biome.transform.position
                    ));
                }
                else
                {
                    biomeNodeIds.Add(biome.nodeId.value);
                }
                
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

                    // Validate biome configuration coherence
                    ValidateBiomeConfigurationCoherence(biome, profile, report);
                }
                
                // Check biome field size
                if (biome.fieldRadius <= 0)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Error,
                        "Invalid Field Radius",
                        "BiomeField has invalid field radius. Must be greater than 0.",
                        biome,
                        biome.transform.position
                    ));
                }
                
                // Check for overlapping biome fields
                ValidateBiomeOverlaps(biome, biomes, report);
            }
        }

        private static void ValidateBiomeConfigurationCoherence(BiomeFieldAuthoring biome, BiomeArtProfile profile, ValidationReport report)
        {
            // Check coherence between biome polarity and art profile settings
            if (biome.polarity.IsNeutral() && profile.propSettings.strategy == PropPlacementStrategy.Terrain)
            {
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Biome Configuration Mismatch",
                    "Neutral polarity biome using terrain-aware prop placement may not behave as expected.",
                    biome,
                    biome.transform.position
                ));
            }

            // Validate prop density settings
            if (profile.propSettings.baseDensity <= 0 && profile.propSettings.densityMultiplier > 0)
            {
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Prop Density Configuration Issue",
                    "Prop density multiplier is set but base density is zero or negative.",
                    profile
                ));
            }

            // Check for reasonable clustering settings
            if (profile.propSettings.strategy == PropPlacementStrategy.Clustered)
            {
                if (profile.propSettings.clustering.clusterSize <= 1)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Ineffective Clustering",
                        "Clustered placement strategy selected but cluster size is 1 or less.",
                        profile
                    ));
                }
            }
        }

        private static void ValidateBiomeOverlaps(BiomeFieldAuthoring targetBiome, BiomeFieldAuthoring[] allBiomes, ValidationReport report)
        {
            foreach (var otherBiome in allBiomes)
            {
                if (otherBiome == null || otherBiome == targetBiome) continue;

                float distance = Vector3.Distance(targetBiome.transform.position, otherBiome.transform.position);
                float combinedRadius = targetBiome.fieldRadius + otherBiome.fieldRadius;

                if (distance < combinedRadius * 0.8f) // Allow some overlap but warn about excessive overlap
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Biome Field Overlap",
                        $"BiomeField overlaps significantly with another biome (NodeId: {otherBiome.nodeId.value}). This may cause conflicts.",
                        targetBiome,
                        targetBiome.transform.position
                    ));
                }
            }
        }
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
                    // Enhanced orphaned gate detection with quick-fix suggestion
                    var suggestedFix = FindBestConnectionMatch(gate, connections);
                    string fixSuggestion = suggestedFix != null 
                        ? $" Suggested fix: Update to connection {suggestedFix.sourceNode.value} -> {suggestedFix.targetNode.value}"
                        : " Consider removing this gate or creating the missing connection.";
                        
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Orphaned Gate Condition",
                        $"Gate condition exists for connection {gate.sourceNode.value} -> {gate.targetNode.value} but no such connection exists.{fixSuggestion}",
                        gate,
                        gate.transform.position
                    ));
                }
                
                // Enhanced gate condition validation
                ValidateGateConditionDetails(gate, report);
            }
            
            // Validate gate conditions referencing non-existent connections  
            ValidateGateConnectionReferences(gates, connections, report);
        }

        private static ConnectionAuthoring FindBestConnectionMatch(GateConditionAuthoring gate, ConnectionAuthoring[] connections)
        {
            // Find the closest matching connection based on spatial proximity or node similarity
            ConnectionAuthoring bestMatch = null;
            float bestScore = float.MaxValue;
            
            foreach (var connection in connections.Where(c => c != null))
            {
                // Score based on node ID similarity and spatial distance
                float nodeScore = Mathf.Abs((int)gate.sourceNode.value - (int)connection.sourceNode.value) +
                                 Mathf.Abs((int)gate.targetNode.value - (int)connection.targetNode.value);
                
                float spatialScore = Vector3.Distance(gate.transform.position, connection.transform.position);
                float combinedScore = nodeScore * 10f + spatialScore;
                
                if (combinedScore < bestScore)
                {
                    bestScore = combinedScore;
                    bestMatch = connection;
                }
            }
            
            return bestScore < 50f ? bestMatch : null; // Only suggest if reasonably close
        }

        private static void ValidateGateConditionDetails(GateConditionAuthoring gate, ValidationReport report)
        {
            // Check gate condition description length
            if (string.IsNullOrEmpty(gate.description.ToString()))
            {
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    "Empty Gate Description",
                    "Gate condition has no description. Consider adding descriptive text for better authoring experience.",
                    gate,
                    gate.transform.position
                ));
            }
            
            // Validate gate condition logic consistency
            if (gate.sourceNode.value == gate.targetNode.value)
            {
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Error,
                    "Self-Referencing Gate",
                    "Gate condition has the same source and target node. This creates an invalid loop.",
                    gate,
                    gate.transform.position
                ));
            }
        }

        private static void ValidateGateConnectionReferences(GateConditionAuthoring[] gates, ConnectionAuthoring[] connections, ValidationReport report)
        {
            // Check for connections that might need gate conditions
            var gatedConnections = new HashSet<(uint, uint)>();
            foreach (var gate in gates.Where(g => g != null))
            {
                gatedConnections.Add((gate.sourceNode.value, gate.targetNode.value));
            }
            
            foreach (var connection in connections.Where(c => c != null))
            {
                var connectionPair = (connection.sourceNode.value, connection.targetNode.value);
                
                if (!gatedConnections.Contains(connectionPair))
                {
                    // Suggest gate conditions for long-distance or inter-district connections
                    float distance = Vector3.Distance(connection.transform.position, Vector3.zero);
                    if (distance > 20f) // Arbitrary threshold for "long-distance"
                    {
                        report.issues.Add(new ValidationIssue(
                            ValidationSeverity.Info,
                            "Missing Gate Condition",
                            $"Long-distance connection {connection.sourceNode.value} -> {connection.targetNode.value} might benefit from a gate condition for proper MetroidVania progression.",
                            connection,
                            connection.transform.position
                        ));
                    }
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

        /// <summary>
        /// Validates orphaned props and tile prototypes not linked to any biome/district
        /// </summary>
        private static void ValidateOrphanedAssets(BiomeFieldAuthoring[] biomes, ValidationReport report)
        {
            // Collect all referenced props and tiles from biomes
            var referencedProps = new HashSet<GameObject>();
            var referencedTiles = new HashSet<UnityEngine.Tilemaps.TileBase>();

            foreach (var biome in biomes.Where(b => b != null && b.artProfile != null))
            {
                var profile = biome.artProfile;
                
                // Collect referenced props
                if (profile.propSettings?.propPrefabs != null)
                {
                    foreach (var prop in profile.propSettings.propPrefabs.Where(p => p != null))
                    {
                        referencedProps.Add(prop);
                    }
                }
                
                // Collect referenced tiles
                if (profile.tiles != null)
                {
                    foreach (var tile in profile.tiles.Where(t => t != null))
                    {
                        referencedTiles.Add(tile);
                    }
                }
                
                // Individual tile references
                if (profile.floorTile != null) referencedTiles.Add(profile.floorTile);
                if (profile.wallTile != null) referencedTiles.Add(profile.wallTile);
                if (profile.backgroundTile != null) referencedTiles.Add(profile.backgroundTile);
            }

            // Find potential orphaned assets in the scene
            var allGameObjects = Object.FindObjectsOfType<GameObject>();
            var potentialOrphans = allGameObjects.Where(go => 
                go.name.ToLower().Contains("prop") || 
                go.name.ToLower().Contains("tile") ||
                go.GetComponent<SpriteRenderer>() != null).ToArray();

            foreach (var obj in potentialOrphans)
            {
                // Check if this GameObject could be a prop but isn't referenced
                if (obj.GetComponent<SpriteRenderer>() != null && !referencedProps.Contains(obj))
                {
                    // Check if it's likely an orphaned prop (not just a regular sprite)
                    if (obj.name.ToLower().Contains("prop") || obj.transform.parent?.name.ToLower().Contains("prop") == true)
                    {
                        report.issues.Add(new ValidationIssue(
                            ValidationSeverity.Info,
                            "Potentially Orphaned Prop",
                            $"GameObject '{obj.name}' appears to be a prop but is not referenced by any BiomeArtProfile. Consider adding it to a prop array or removing if unused.",
                            obj,
                            obj.transform.position
                        ));
                    }
                }
            }

            // Check for unused tile assets in project (simplified check)
            ValidateUnusedTileAssets(referencedTiles, report);
        }

        private static void ValidateUnusedTileAssets(HashSet<UnityEngine.Tilemaps.TileBase> referencedTiles, ValidationReport report)
        {
            // Comprehensive project asset scanning for unused tile assets
            var allTileAssets = new HashSet<UnityEngine.Tilemaps.TileBase>();
            
            // Scan all tile assets in the project
            var tileGUIDs = AssetDatabase.FindAssets("t:TileBase");
            foreach (var guid in tileGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var tileAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.TileBase>(assetPath);
                if (tileAsset != null)
                {
                    allTileAssets.Add(tileAsset);
                }
            }
            
            // Also scan for ScriptableObject tiles (custom tile types)
            var scriptableObjectGUIDs = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in scriptableObjectGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset is UnityEngine.Tilemaps.TileBase tileBase)
                {
                    allTileAssets.Add(tileBase);
                }
            }
            
            // Find tiles that are in project but not referenced
            var orphanedTiles = allTileAssets.Except(referencedTiles).ToList();
            
            foreach (var orphanedTile in orphanedTiles)
            {
                // Skip built-in Unity tiles
                var assetPath = AssetDatabase.GetAssetPath(orphanedTile);
                if (assetPath.StartsWith("Library/") || assetPath.StartsWith("Packages/com.unity."))
                    continue;
                    
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    $"Orphaned tile asset: '{orphanedTile.name}' at '{assetPath}' is not referenced by any BiomeArtProfile",
                    "Consider removing unused tile assets or verifying they should be referenced",
                    orphanedTile
                ));
            }
            
            // Also validate BiomeArtProfile tile references for consistency
            var allProfiles = AssetDatabase.FindAssets("t:BiomeArtProfile")
                .Select(guid => AssetDatabase.LoadAssetAtPath<BiomeArtProfile>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(profile => profile != null);

            foreach (var profile in allProfiles)
            {
                // Check for null tile references that might indicate orphaned or missing tiles
                if (profile.tiles != null)
                {
                    for (int i = 0; i < profile.tiles.Length; i++)
                    {
                        if (profile.tiles[i] == null)
                        {
                            report.issues.Add(new ValidationIssue(
                                ValidationSeverity.Warning,
                                "Null Tile Reference",
                                $"BiomeArtProfile '{profile.name}' has a null tile reference at index {i}. This may indicate a missing or deleted tile asset.",
                                profile
                            ));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Suggests automatic fixes for common authoring issues like missing NodeIds
        /// </summary>
        private static void SuggestAutoFixes(DistrictAuthoring[] districts, BiomeFieldAuthoring[] biomes, ValidationReport report)
        {
            // Auto-assign missing NodeIds
            var usedNodeIds = new HashSet<uint>();
            
            // Collect existing NodeIds
            foreach (var district in districts.Where(d => d != null && d.nodeId.value != 0))
            {
                usedNodeIds.Add(district.nodeId.value);
            }
            foreach (var biome in biomes.Where(b => b != null && b.nodeId.value != 0))
            {
                usedNodeIds.Add(biome.nodeId.value);
            }

            // Suggest NodeIds for objects missing them
            uint nextAvailableId = 1;
            foreach (var district in districts.Where(d => d != null && d.nodeId.value == 0))
            {
                while (usedNodeIds.Contains(nextAvailableId))
                {
                    nextAvailableId++;
                }
                
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    "Auto-Fix Suggestion",
                    $"District '{district.name}' is missing NodeId. Suggested auto-assignment: {nextAvailableId}. You can manually set this in the inspector.",
                    district,
                    district.transform.position
                ));
                
                usedNodeIds.Add(nextAvailableId);
                nextAvailableId++;
            }

            foreach (var biome in biomes.Where(b => b != null && b.nodeId.value == 0))
            {
                while (usedNodeIds.Contains(nextAvailableId))
                {
                    nextAvailableId++;
                }
                
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Info,
                    "Auto-Fix Suggestion", 
                    $"BiomeField '{biome.name}' is missing NodeId. Suggested auto-assignment: {nextAvailableId}. You can manually set this in the inspector.",
                    biome,
                    biome.transform.position
                ));
                
                usedNodeIds.Add(nextAvailableId);
                nextAvailableId++;
            }
        }

        /// <summary>
        /// Validates navigation graph connectivity and identifies unreachable areas
        /// Integrates with NavigationValidationSystem to provide comprehensive reachability analysis
        /// </summary>
        private static void ValidateNavigationConnectivity(ValidationReport report)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                report.issues.Add(new ValidationIssue(
                    ValidationSeverity.Warning,
                    "Navigation Validation Skipped",
                    "World not available for navigation validation. This may be normal during scene load.",
                    null
                ));
                return;
            }

            // Generate navigation validation report
            var navReport = NavigationValidationUtility.GenerateValidationReport(world);
            
            try
            {
                // Check for unreachable areas
                if (navReport.HasUnreachableAreas)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Unreachable Navigation Areas",
                        $"Found {navReport.UnreachableNodeCount} unreachable navigation nodes. Some areas may be inaccessible to AI agents.",
                        null
                    ));
                }

                // Report on isolated components
                if (navReport.IsolatedComponentCount > 1)
                {
                    report.issues.Add(new ValidationIssue(
                        ValidationSeverity.Warning,
                        "Isolated Navigation Components",
                        $"Navigation graph has {navReport.IsolatedComponentCount} isolated components. Consider adding connections between disconnected areas.",
                        null
                    ));
                }

                // Check for specific navigation issues
                for (int i = 0; i < navReport.Issues.Length; i++)
                {
                    var issue = navReport.Issues[i];
                    var severity = issue.Type switch
                    {
                        NavigationIssueType.HardGateBlocking => ValidationSeverity.Error,
                        NavigationIssueType.UnreachableNode => ValidationSeverity.Warning,
                        NavigationIssueType.RequiresUnavailablePolarity => ValidationSeverity.Warning,
                        NavigationIssueType.RequiresUnavailableAbility => ValidationSeverity.Info,
                        _ => ValidationSeverity.Info
                    };

                    report.issues.Add(new ValidationIssue(
                        severity,
                        $"Navigation Issue: {issue.Type}",
                        issue.Description.ToString(),
                        null
                    ));
                }

                // Generate and suggest navigation quick fixes
                var quickFixes = NavigationValidationUtility.GenerateQuickFixSuggestions(navReport);
                for (int i = 0; i < quickFixes.Length; i++)
                {
                    var fix = quickFixes[i];
                    if (fix.Type != NavigationQuickFixType.None)
                    {
                        report.issues.Add(new ValidationIssue(
                            ValidationSeverity.Info,
                            "Navigation Quick-Fix Suggestion",
                            $"Node {fix.TargetNodeId}: {fix.Description}",
                            null
                        ));
                    }
                }

                quickFixes.Dispose();
            }
            finally
            {
                navReport.Dispose();
            }
        }
    }
}