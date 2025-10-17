---
description: Repository Information Overview
alwaysApply: true
---

# TWG-MetVanDamn Information

## Summary
TWG-MetVanDamn is an advanced Unity project that combines procedural MetroidVania world generation with Entity Component System (ECS/DOTS) architecture. It features a sophisticated Living Dev Agent workflow with comprehensive development tooling and documentation practices.

## Structure
- **Assets/**: Core Unity assets including MetVanDAMN game systems, scenes, and settings
- **Packages/**: Custom packages for core, biome, graph, authoring, utility, and samples
- **src/**: Validation and linting tools
- **docs/**: Documentation hub with guides, tutorials, and reference materials
- **TLDL/**: Living Dev Log entries for development tracking
- **tests/**: Test files for various components of the system

## Language & Runtime
**Language**: C# with Unity ECS/DOTS
**Version**: Unity 6000.2+
**Build System**: Unity Build Pipeline
**Package Manager**: Unity Package Manager

## Dependencies
**Main Dependencies**:
- com.unity.entities (1.3.14)
- com.unity.collections (2.5.7)
- com.unity.burst (1.8.24)
- com.unity.mathematics (1.3.2)
- com.unity.entities.graphics (1.4.12)
- com.unity.render-pipelines.universal (17.2.0)
- com.unity.2d.animation (12.0.2)

**Development Dependencies**:
- com.unity.test-framework (1.6.0)
- com.unity.testtools.codecoverage (1.2.7)
- com.unity.ide.rider (3.0.38)
- com.unity.ide.visualstudio (2.0.23)

## Build & Installation
```bash
# Clone the repository
git clone https://github.com/jmeyer1980/TWG-MetVanDamn.git
cd TWG-MetVanDamn

# Initialize Living Dev Agent environment
mkdir -p .github/workflows
chmod +x scripts/init_agent_context.sh scripts/clone-and-clean.sh
scripts/init_agent_context.sh

# Open in Unity 6000.2+
# Unity will automatically resolve package dependencies
```

## Testing
**Framework**: Unity Test Framework
**Test Location**: Assets/MetVanDAMN/*/Tests/ and Packages/com.tinywalnutgames.metvd.*/Tests/
**Naming Convention**: *Tests.cs
**Configuration**: com.unity.test-framework
**Run Command**:
```bash
# Run tests through Unity Test Runner
# Or use the following command for validation
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/
python3 src/DebugOverlayValidation/debug_overlay_validator.py --path src/
python3 src/SymbolicLinter/symbolic_linter.py --path src/
```

## Projects

### MetVanDAMN Core
**Configuration File**: Packages/com.tinywalnutgames.metvd.core/package.json

#### Language & Runtime
**Language**: C#
**Version**: Unity 6000.2+
**Build System**: Unity Build Pipeline
**Package Manager**: Unity Package Manager

#### Dependencies
**Main Dependencies**:
- com.unity.entities (1.2.0+)
- com.unity.collections (1.2.4+)

#### Build & Installation
```bash
# Included as part of the main project build
```

### MetVanDAMN Biome
**Configuration File**: Packages/com.tinywalnutgames.metvd.biome/package.json

#### Language & Runtime
**Language**: C#
**Version**: Unity 6000.2+
**Build System**: Unity Build Pipeline
**Package Manager**: Unity Package Manager

#### Dependencies
**Main Dependencies**:
- com.tinywalnutgames.metvd.core
- com.unity.entities
- com.unity.mathematics

#### Build & Installation
```bash
# Included as part of the main project build
```

### MetVanDAMN Graph
**Configuration File**: Packages/com.tinywalnutgames.metvd.graph/package.json

#### Language & Runtime
**Language**: C#
**Version**: Unity 6000.2+
**Build System**: Unity Build Pipeline
**Package Manager**: Unity Package Manager

#### Dependencies
**Main Dependencies**:
- com.tinywalnutgames.metvd.core
- com.tinywalnutgames.metvd.biome
- com.unity.entities
- com.unity.mathematics

#### Build & Installation
```bash
# Included as part of the main project build
```

### MetVanDAMN Authoring
**Configuration File**: Packages/com.tinywalnutgames.metvd.authoring/package.json

#### Language & Runtime
**Language**: C#
**Version**: Unity 6000.2+
**Build System**: Unity Build Pipeline
**Package Manager**: Unity Package Manager

#### Dependencies
**Main Dependencies**:
- com.tinywalnutgames.metvd.core
- com.tinywalnutgames.metvd.graph
- com.unity.entities

#### Testing
**Framework**: Unity Test Framework
**Test Location**: Packages/com.tinywalnutgames.metvd.authoring/Tests/
**Naming Convention**: *Tests.cs
**Run Command**:
```bash
# Run through Unity Test Runner
```