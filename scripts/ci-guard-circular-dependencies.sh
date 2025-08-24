#!/bin/bash
# CI Guard for Circular Dependency Prevention
# This script is designed to be run in CI/CD pipelines to prevent merging code with circular dependencies.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "🛡️  MetVanDAMN Circular Dependency CI Guard"
echo "🔍 Project Root: $PROJECT_ROOT"

# Change to project root
cd "$PROJECT_ROOT"

# Run the Python validation script
echo "⚙️  Running dependency validation..."
if python3 scripts/validate-circular-dependencies.py; then
    echo "✅ CI Guard: All dependency validation checks passed"
    exit 0
else
    echo "❌ CI Guard: Dependency validation failed"
    echo ""
    echo "🚨 BUILD BLOCKED: Circular dependencies detected!"
    echo ""
    echo "This error indicates that changes introduce or reintroduce circular dependencies"
    echo "between assemblies. This violates the project's architectural guidelines."
    echo ""
    echo "To fix this issue:"
    echo "1. Review the circular dependency path shown above"
    echo "2. Move shared types to TinyWalnutGames.MetVD.Shared namespace"
    echo "3. Ensure shared assemblies don't reference feature assemblies"
    echo "4. Update assembly definitions to remove circular references"
    echo ""
    echo "For help with refactoring, see:"
    echo "- CONTRIBUTING.md (helper placement rules)"
    echo "- TinyWalnutGames.MetVD.Shared package for examples"
    echo ""
    exit 1
fi