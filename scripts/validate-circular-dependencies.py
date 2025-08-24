#!/usr/bin/env python3
"""
Circular Dependency Validator for MetVanDAMN
Analyzes assembly definition files to detect circular dependencies.
"""

import json
import os
import sys
from collections import defaultdict, deque


def find_asmdef_files(root_path):
    """Find all .asmdef files in the project."""
    asmdef_files = []
    for root, dirs, files in os.walk(root_path):
        for file in files:
            if file.endswith('.asmdef'):
                asmdef_files.append(os.path.join(root, file))
    return asmdef_files


def parse_asmdef(file_path):
    """Parse an assembly definition file and extract name and references."""
    try:
        with open(file_path, 'r') as f:
            data = json.load(f)
        return {
            'name': data.get('name', ''),
            'references': data.get('references', []),
            'path': file_path
        }
    except Exception as e:
        print(f"⚠️  Error parsing {file_path}: {e}")
        return None


def build_dependency_graph(asmdef_files):
    """Build a dependency graph from assembly definitions."""
    graph = defaultdict(list)
    assemblies = {}
    
    # First pass: collect all assemblies
    for file_path in asmdef_files:
        asmdef = parse_asmdef(file_path)
        if asmdef:
            assemblies[asmdef['name']] = asmdef
    
    # Second pass: build dependency graph
    for name, asmdef in assemblies.items():
        for ref in asmdef['references']:
            if ref in assemblies:  # Only track internal dependencies
                graph[name].append(ref)
    
    return graph, assemblies


def detect_circular_dependencies(graph):
    """Detect circular dependencies using DFS."""
    def dfs(node, visited, rec_stack, path):
        visited.add(node)
        rec_stack.add(node)
        
        for neighbor in graph.get(node, []):
            if neighbor not in visited:
                cycle = dfs(neighbor, visited, rec_stack, path + [neighbor])
                if cycle:
                    return cycle
            elif neighbor in rec_stack:
                # Found a cycle
                cycle_start = path.index(neighbor)
                return path[cycle_start:] + [neighbor]
        
        rec_stack.remove(node)
        return None
    
    visited = set()
    for node in graph:
        if node not in visited:
            cycle = dfs(node, visited, set(), [node])
            if cycle:
                return cycle
    
    return None


def validate_shared_namespace_isolation(graph, assemblies):
    """Validate that shared namespace doesn't import from feature modules."""
    violations = []
    shared_assemblies = [name for name in assemblies if 'Shared' in name]
    feature_assemblies = [name for name in assemblies if not ('Shared' in name or 'Unity.' in name)]
    
    for shared in shared_assemblies:
        for ref in graph.get(shared, []):
            if ref in feature_assemblies:
                violations.append(f"❌ Shared assembly '{shared}' imports from feature assembly '{ref}'")
    
    return violations


def main():
    """Main validation function."""
    root_path = os.getcwd()
    print(f"🔍 Analyzing assembly dependencies in {root_path}")
    
    # Find and parse assembly definitions
    asmdef_files = find_asmdef_files(root_path)
    print(f"📂 Found {len(asmdef_files)} assembly definition files")
    
    graph, assemblies = build_dependency_graph(asmdef_files)
    
    # Print discovered assemblies
    print(f"🏗️  Discovered {len(assemblies)} internal assemblies:")
    for name in sorted(assemblies.keys()):
        deps = len(graph.get(name, []))
        print(f"   - {name} ({deps} dependencies)")
    
    # Check for circular dependencies
    print("\n🕸️  Checking for circular dependencies...")
    cycle = detect_circular_dependencies(graph)
    
    if cycle:
        print("❌ Circular dependency detected:")
        print("   " + " → ".join(cycle))
        return 1
    else:
        print("✅ No circular dependencies detected")
    
    # Validate shared namespace isolation
    print("\n🛡️  Validating shared namespace isolation...")
    violations = validate_shared_namespace_isolation(graph, assemblies)
    
    if violations:
        print("❌ Shared namespace isolation violations:")
        for violation in violations:
            print(f"   {violation}")
        return 1
    else:
        print("✅ Shared namespace properly isolated")
    
    print("\n🎉 All dependency validation checks passed!")
    return 0


if __name__ == "__main__":
    sys.exit(main())