// File: 'Assets/Plugins/living-dev-agent/src/SymbolicLinter/SymbolResolutionLinter.cs'
/*
 * Symbol Resolution Linter for Living Dev Agent Template
 *
 * Copyright (C) 2025 Bellok
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

// @SystemType: Development Tool
// @Domain: LivingDevAgent.Core
// @Role: Symbol Resolution Validation

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LivingDevAgent.Core.Editor
{
    /// <summary>
    /// Symbol resolution linter that scans for unresolved conditional compilation symbols.
    /// Addresses Issue #26 symbol resolution audit requirements.
    /// </summary>
    [InitializeOnLoad]
    public static class SymbolResolutionLinter
    {
        // Loads Unity symbols from a configuration file for maintainability.
        private static List<string> LoadUnitySymbols()
        {
            var configPath = "Assets/Editor/UnitySymbols.txt";
            if (File.Exists(configPath))
            {
                // Each line is a symbol, ignore empty/comment lines
                return File.ReadAllLines(configPath)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                    .ToList();
            }
            else
            {
                Debug.LogWarning($"[SymbolResolutionLinter] Unity symbols config file not found at {configPath}. Using minimal fallback list.");
                return new List<string> {
                    "UNITY_EDITOR",
                    "UNITY_STANDALONE",
                    "UNITY_ANDROID",
                    "UNITY_IOS",
                    "UNITY_WEBGL"
                };
            }
        }

        // Cached list of known Unity symbols loaded from config.
        private static readonly List<string> KnownUnitySymbols = LoadUnitySymbols();

        static SymbolResolutionLinter()
        {
            // Run validation after Unity finishes loading
            EditorApplication.delayCall += ValidateSymbols;
        }

        [MenuItem("Tools/Living Dev Agent/Validate Symbol Resolution")]
        public static void ValidateSymbols()
        {
            Debug.Log("[SymbolResolutionLinter] Starting symbol resolution validation...");

            var allDefinedSymbols = GetAllDefinedSymbols();
            var flaggedFiles = ScanForUnresolvedSymbols(allDefinedSymbols);

            if (flaggedFiles.Count == 0)
            {
                Debug.Log("[SymbolResolutionLinter] ✓ No unresolved symbols found - all conditional compilation directives are valid");
            }
            else
            {
                Debug.LogWarning($"[SymbolResolutionLinter] Found {flaggedFiles.Count} potential unresolved symbols:\n" +
                    string.Join("\n", flaggedFiles.Take(10))); // Limit output for readability

                if (flaggedFiles.Count > 10)
                {
                    Debug.LogWarning($"[SymbolResolutionLinter] ... and {flaggedFiles.Count - 10} more. See full report in console.");
                }
            }
        }

        private static HashSet<string> GetAllDefinedSymbols()
        {
            var symbols = new HashSet<string>();

            // Add built-in Unity symbols
            foreach (var symbol in KnownUnitySymbols)
            {
                symbols.Add(symbol);
            }

            // Add project-defined symbols for all build targets
            foreach (BuildTargetGroup group in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown) continue;

                try
                {
                    var projectSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                    if (!string.IsNullOrEmpty(projectSymbols))
                    {
                        foreach (var symbol in projectSymbols.Split(';', ','))
                        {
                            var trimmed = symbol.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                            {
                                symbols.Add(trimmed);
                            }
                        }
                    }
                }
                catch
                {
                    // Some build target groups might not be available
                }
            }

            return symbols;
        }

        private static List<string> ScanForUnresolvedSymbols(HashSet<string> knownSymbols)
        {
            var flaggedFiles = new List<string>();
            var conditionalPattern = new Regex(@"#(?:if|elif)\s+([A-Z_][A-Z0-9_]*)", RegexOptions.Compiled);

            // Scan project directories
            ScanDirectory("Assets/Scripts", conditionalPattern, knownSymbols, flaggedFiles);
            ScanDirectory("Assets/LivingDevAgent", conditionalPattern, knownSymbols, flaggedFiles);

            // Scan TTG directory if it exists
            // Scan standard Unity project directories
            if (Directory.Exists("Assets/Scripts"))
            {
                ScanDirectory("Assets/Scripts", conditionalPattern, knownSymbols, flaggedFiles);
            }

            if (Directory.Exists("Assets/LivingDevAgent"))
            {
                ScanDirectory("Assets/LivingDevAgent", conditionalPattern, knownSymbols, flaggedFiles);
            }

            return flaggedFiles;
        }

        private static void ScanDirectory(string directory, Regex pattern, HashSet<string> knownSymbols, List<string> flaggedFiles)
        {
            if (!Directory.Exists(directory)) return;

            foreach (var filePath in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                try
                {
                    var lines = File.ReadAllLines(filePath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        var matches = pattern.Matches(line);

                        foreach (Match match in matches)
                        {
                            var symbol = match.Groups[1].Value;
                            if (!knownSymbols.Contains(symbol))
                            {
                                var relativePath = filePath.Replace("\\", "/");
                                flaggedFiles.Add($"{relativePath}:{i+1}: {line.Trim()} (symbol: {symbol})");
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SymbolResolutionLinter] Error scanning {filePath}: {e.Message}");
                }
            }
        }

        [MenuItem("Tools/Living Dev Agent/Show Defined Symbols")]
        public static void ShowDefinedSymbols()
        {
            var symbols = GetAllDefinedSymbols();
            Debug.Log($"[SymbolResolutionLinter] Currently defined symbols ({symbols.Count}):\n" +
                string.Join(", ", symbols.OrderBy(s => s)));
        }
    }
}
#endif
