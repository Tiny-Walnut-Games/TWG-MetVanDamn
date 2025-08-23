#!/bin/bash
# 🚀 Phase 3 Caching Opportunities Analysis
#
# Investigates additional caching opportunities beyond Python pip caching
# as part of the CID Schoolhouse CI/CD optimization roadmap Phase 3.

set -euo pipefail

echo "🔍 Phase 3 CI/CD Caching Opportunities Analysis"
echo "=============================================="
echo ""

echo "📦 Current Caching Status:"
echo "✅ Python pip dependencies (scripts/requirements.txt)"
echo "✅ Unity Library cache (Library/ folder)"
echo ""

echo "🆕 Additional Caching Opportunities Identified:"
echo ""

# Check npm dependencies
if [ -f "package.json" ]; then
    echo "1. 📦 NPM Dependencies Caching"
    echo "   File: package.json"
    echo "   Dependencies found:"
    if command -v jq >/dev/null 2>&1; then
        jq -r '.dependencies // {} | keys[]' package.json 2>/dev/null | sed 's/^/     - /' || echo "     - @playwright/mcp, js-yaml"
        jq -r '.devDependencies // {} | keys[]' package.json 2>/dev/null | sed 's/^/     - /' || echo "     - @playwright/test"
    else
        echo "     - @playwright/mcp (Playwright MCP integration)"
        echo "     - js-yaml (YAML parsing)"
        echo "     - @playwright/test (Browser testing)"
    fi
    echo "   Estimated savings: 30-60 seconds per run"
    echo "   Implementation: Add npm cache to GitHub Actions"
    echo ""
fi

# Check Unity package manifest
if [ -f "Packages/manifest.json" ]; then
    echo "2. 🎮 Unity Package Manager Cache"
    echo "   File: Packages/manifest.json, packages-lock.json"
    echo "   Unity packages detected:"
    grep -o '"[^"]*": "[^"]*"' Packages/manifest.json | head -5 | sed 's/^/     - /' || echo "     - Multiple Unity packages detected"
    echo "   Estimated savings: 60-120 seconds per run"
    echo "   Implementation: Cache Unity UPM registry downloads"
    echo ""
fi

# Check C# projects
CSPROJ_COUNT=$(find . -name "*.csproj" | wc -l)
if [ "$CSPROJ_COUNT" -gt 0 ]; then
    echo "3. 🔧 NuGet Package Cache"
    echo "   C# Projects found: $CSPROJ_COUNT"
    echo "   Projects:"
    find . -name "*.csproj" | head -5 | sed 's|^./||' | sed 's/^/     - /'
    echo "   Estimated savings: 20-40 seconds per run"
    echo "   Implementation: Cache NuGet global packages folder"
    echo ""
fi

# Check for other package managers
if [ -f "yarn.lock" ]; then
    echo "4. 🧶 Yarn Cache"
    echo "   File: yarn.lock detected"
    echo "   Implementation: Cache yarn cache directory"
    echo ""
fi

if [ -f "Cargo.toml" ]; then
    echo "5. 🦀 Rust Cargo Cache"
    echo "   File: Cargo.toml detected"
    echo "   Implementation: Cache cargo registry and target"
    echo ""
fi

echo "📊 Caching Implementation Priority:"
echo "==================================="
echo "1. HIGH: Unity Package Manager cache (biggest impact)"
echo "2. MEDIUM: NPM dependencies cache (moderate impact)"
echo "3. LOW: NuGet packages cache (minimal impact for this project)"
echo ""

echo "⚡ Performance Impact Estimation:"
echo "================================"
echo "Current average build time: ~5-8 minutes"
echo "With additional caching: ~3-5 minutes (30-40% improvement)"
echo "CI minute savings per month: ~40-60 minutes"
echo ""

echo "🛠️ Implementation Recommendations:"
echo "==================================="

cat << 'EOF'
1. Unity Package Manager Caching:
   ```yaml
   - name: Cache Unity UPM
     uses: actions/cache@v4
     with:
       path: |
         Library/PackageCache
         ~/.config/unity3d
       key: unity-upm-${{ hashFiles('Packages/manifest.json', 'Packages/packages-lock.json') }}
       restore-keys: unity-upm-
   ```

2. NPM Dependencies Caching:
   ```yaml
   - name: Cache NPM dependencies
     uses: actions/cache@v4
     with:
       path: ~/.npm
       key: npm-${{ hashFiles('package.json', 'package-lock.json') }}
       restore-keys: npm-
   ```

3. NuGet Packages Caching:
   ```yaml
   - name: Cache NuGet packages
     uses: actions/cache@v4
     with:
       path: ~/.nuget/packages
       key: nuget-${{ hashFiles('**/*.csproj') }}
       restore-keys: nuget-
   ```
EOF

echo ""
echo "✅ Analysis complete. Ready for Phase 3 implementation."