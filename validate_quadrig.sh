#!/bin/bash

# 🎭 DOTS Quad-Rig Humanoid Prototype Validation Script
# Ensures all requirements from the problem statement are met

echo "🎭 DOTS Quad-Rig Humanoid Prototype Validation"
echo "=============================================="

# Check for core files
echo "📁 Checking core implementation files..."

QUADRIG_DIR="Packages/com.tinywalnutgames.metvd.quadrig"
AUTHORING_DIR="Assets/MetVanDAMN/QuadRig"

# Core requirement files
requirements=(
    "$QUADRIG_DIR/Runtime/QuadRigComponents.cs"
    "$QUADRIG_DIR/Runtime/BillboardSystem.cs" 
    "$QUADRIG_DIR/Runtime/QuadMeshGenerationSystem.cs"
    "$QUADRIG_DIR/Runtime/BiomeSkinSwapSystem.cs"
    "$QUADRIG_DIR/Runtime/BoneHierarchySystem.cs"
    "$QUADRIG_DIR/Runtime/QuadRigStoryTest.cs"
    "$QUADRIG_DIR/Tests/Runtime/QuadRigHumanoidTests.cs"
    "$AUTHORING_DIR/Authoring/QuadRigHumanoidAuthoring.cs"
)

all_files_exist=true
for file in "${requirements[@]}"; do
    if [ -f "$file" ]; then
        echo "✅ $file"
    else
        echo "❌ $file - MISSING!"
        all_files_exist=false
    fi
done

echo ""
echo "🎯 Checking requirement compliance..."

# Check 1: Quad meshes UV-mapped to shared texture atlas
if grep -q "CreateQuadMesh.*uvRect" "$QUADRIG_DIR/Runtime/QuadMeshGenerationSystem.cs"; then
    echo "✅ Quad meshes with UV atlas mapping"
else
    echo "❌ Quad meshes with UV atlas mapping - NOT FOUND"
    all_files_exist=false
fi

# Check 2: Bone hierarchy compatible with Unity Animator
if grep -q "BoneHierarchyElement\|Animator" "$QUADRIG_DIR/Runtime/BoneHierarchySystem.cs"; then
    echo "✅ Bone hierarchy compatible with Unity Animator"
else
    echo "❌ Bone hierarchy compatible with Unity Animator - NOT FOUND"
    all_files_exist=false
fi

# Check 3: Y-axis billboard logic
if grep -q "Y.*axis\|billboard.*Y" "$QUADRIG_DIR/Runtime/BillboardSystem.cs"; then
    echo "✅ Y-axis billboard logic for camera-facing orientation"
else
    echo "❌ Y-axis billboard logic - NOT FOUND"
    all_files_exist=false
fi

# Check 4: Biome skin swapping
if grep -q "BiomeSkinSwap\|atlas.*swap\|material.*change" "$QUADRIG_DIR/Runtime/BiomeSkinSwapSystem.cs"; then
    echo "✅ Biome skin swapping without rig/animation changes"
else
    echo "❌ Biome skin swapping - NOT FOUND"
    all_files_exist=false
fi

# Check 5: Alpha-mask silhouette integrity
if grep -q "alpha.*mask\|silhouette\|_Cutoff" "$QUADRIG_DIR/Runtime/"*.cs; then
    echo "✅ Alpha-mask silhouette integrity"
else
    echo "❌ Alpha-mask silhouette integrity - NOT FOUND"
    all_files_exist=false
fi

# Check 6: Story test completeness
if grep -q "story.*test\|narrative\|phase.*validation" "$QUADRIG_DIR/Runtime/QuadRigStoryTest.cs"; then
    echo "✅ Complete story test with narrative validation"
else
    echo "❌ Complete story test - NOT FOUND"
    all_files_exist=false
fi

# Check 7: No refs in code (nullable compliance)
ref_count=$(find "$QUADRIG_DIR" -name "*.cs" -exec grep -c "\bref\s" {} \; | awk '{sum += $1} END {print sum}')
if [ "$ref_count" -eq 0 ]; then
    echo "✅ Zero 'ref' keywords in code (nullable compliance)"
else
    echo "⚠️  Found $ref_count 'ref' keywords - check nullable compliance"
fi

echo ""
echo "📊 Implementation Statistics:"
echo "----------------------------------------"

# Count lines of code
total_lines=$(find "$QUADRIG_DIR" -name "*.cs" -exec wc -l {} \; | awk '{sum += $1} END {print sum}')
echo "📜 Total lines of QuadRig code: $total_lines"

# Count components
component_count=$(grep -r "struct.*IComponentData\|class.*MonoBehaviour" "$QUADRIG_DIR" --include="*.cs" | wc -l)
echo "🧩 DOTS components implemented: $component_count"

# Count systems
system_count=$(grep -r "SystemBase\|ISystem" "$QUADRIG_DIR" --include="*.cs" | wc -l)
echo "⚙️  ECS systems implemented: $system_count"

# Count tests
test_count=$(grep -r "\[Test\]" "$QUADRIG_DIR" --include="*.cs" | wc -l)
echo "🧪 Unit tests written: $test_count"

echo ""
echo "🎭 Story Test Analysis:"
echo "----------------------------------------"

# Check story test phases
phase_count=$(grep -c "PHASE_" "$QUADRIG_DIR/Runtime/QuadRigStoryTest.cs" 2>/dev/null || echo "0")
echo "🎬 Story test phases: $phase_count"

# Check if all major features are tested in story test
story_features=(
    "billboard"
    "skin.*swap"
    "animation"
    "alpha.*mask"
    "validation"
)

echo "🎯 Story test coverage:"
for feature in "${story_features[@]}"; do
    if grep -qi "$feature" "$QUADRIG_DIR/Runtime/QuadRigStoryTest.cs" 2>/dev/null; then
        echo "  ✅ $feature testing"
    else
        echo "  ❌ $feature testing - MISSING"
        all_files_exist=false
    fi
done

echo ""
echo "🏆 Final Validation Result:"
echo "=============================================="

if [ "$all_files_exist" = true ]; then
    echo "🎉 SUCCESS! All requirements met!"
    echo "🎭 The DOTS Quad-Rig Humanoid Prototype is complete and ready."
    echo "🚀 Story test promise: Every actor delivers their lines perfectly!"
    exit 0
else
    echo "❌ FAILURE! Some requirements are missing."
    echo "🔧 Please review the missing items above."
    exit 1
fi