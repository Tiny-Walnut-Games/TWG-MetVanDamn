# 📚 MetVanDAMN Documentation Hub
## *Your Gateway to Procedural MetroidVania Excellence*

> **"Good documentation is like a comfortable chair for your brain - it supports you exactly where you need it most."**
> — The Sacred Scrolls of Developer Wisdom

This is the **definitive documentation hub** for all MetVanDAMN namespace content, consolidating scattered documentation into a single, coherent, and GitBook-compatible structure.

---

## 🚀 **Start Here: Essential Guides**

### **🎯 New to MetVanDAMN?**
- **[📖 Complete Setup Guide](COMPLETE-METVANDAMN-SETUP-GUIDE.md)** - *The ultimate "clone to working world" experience in 15 minutes*
- **[🏛️ Project Overview](MetVanDAMN-Project-Overview.md)** - *High-level architecture and system overview*
- **[⚡ Quick Start Guide](MetVanDAMN-Quick-Start-Guide.md)** - *Rapid introduction for developers*

### **⚡ Quick References**
- **[🔧 Authoring Layer Guide](Authoring/README.md)** - *Scene-based setup and WorldBootstrap system*
- **[🧪 Testing Strategy](Authoring/Tests/TestingStrategy.md)** - *How to run and write tests*
- **[🎨 Biome Art Integration](Authoring/BiomeArtProfile_UserGuide.md)** - *Art and visual content systems*

---

## 📚 **GitBook Navigation Structure**

### **1. Anchor & Onboarding**
1. **[The A to Z MetVanDAMN Onboarding Codex](1-Anchor-Onboarding/1-The-A-to-Z-MetVanDAMN-Onboarding-Codex.md)**
   Progression‑gated ritual guide (A→Z) establishing lore, setup steps, and NG+ philosophy.
2. **[The 123 Quick TLDR Walkthrough](1-Anchor-Onboarding/2-The-123-Quick-TLDR-Walkthrough.md)**
   Minimal "just run it" path: 0–10 checklist for impatient validators.
3. **[Procedural Metroidvania Engine](1-Anchor-Onboarding/3-Procedural-Metroidvania-Engine.md)**
   Source‑of‑truth macro design: subsystems, naming conventions, progression grammar.

### **2. Core ECS Integration**
4. **[Core & Biome Systems Integration](2-Core-ECS-Integration/4-Core-Biome-Systems-Integration.md)**
   First clean compile milestone: core components (Biome / Connection / GateCondition / NodeId) + stub systems.

### **3. Worldgen Layers**
5. **[Biome System & Validation Layer](3-Worldgen-Layers/5-Biome-System-Validation-Layer.md)**
   Polarity‑aware biome assignment, strength/difficulty scaling, validation cadence.
6. **[District WFC Generation & Sector Refinement](3-Worldgen-Layers/6-District-WFC-Generation-Sector-Refine.md)**
   Macro topology (WFC) + loop / lock refinement phases (planning → loops → locks → validation).
7. **[Progression Gates & GateCondition Orchestration](3-Worldgen-Layers/7-Progression-Gates-GateCondition-Orchestration.md)**
   Polarity + ability gating model, pacing rules, soft vs hard lock semantics.

### **4. Testing & Validation**
8. **[Unit Testing & Validation Artifacts](4-Testing-Validation/8-Unit-Testing-Validation-Artifacts.md)**
   Test suite architecture (core / generation / integration seed replay) + coverage goals.
9. **[Smoke Test Scene Setup & Immediate Validation](4-Testing-Validation/9-Smoke-Test-Scene-Setup-Immediate-Validation.md)**
   One‑click scene harness seeding world, WFC, refinement, biome fields.
10. **[WfcSystemTests Logic & Assertions](4-Testing-Validation/10-WfcSystemTests-Logic-Assertions.md)**
    Intent + coverage of WFC tests (initialization, collapse, contradiction).
11. **[DistrictWfcSystem Test Harness Fix (ISystem Compliance)](4-Testing-Validation/11-DistrictWfcSystem-Test-Harness-Fix.md)**
    Migration of tests to proper unmanaged ISystem ticking via SimulationSystemGroup.

### **5. CI & Keeper Wiring**
12. **[Integration & CI Chronicle Keeper Wiring](5-CI-Keeper-Wiring/12-Integration-CI-Chronicle-Keeper-Wiring.md)**
    Automated narration: tests → badges → scroll synthesis with multiline‑safe event parsing.

---

## 🛠️ **Authoring & Development Tools**

### **🔧 Authoring Layer**
- **[README](Authoring/README.md)** - *Core authoring system overview*
- **[WorldBootstrap System](Authoring/README_WorldBootstrap.md)** - *Procedural hierarchy generation*
- **[ONBOARDING Guide](Authoring/ONBOARDING.md)** - *Getting started with authoring tools*
- **[Prefab Registry](Authoring/README-PrefabRegistry.md)** - *Prefab management system*
- **[Sudo Actions](Authoring/README.SudoActions.md)** - *Advanced authoring commands*

### **🎨 Art & Visual Content**
- **[Biome Art Profile User Guide](Authoring/BiomeArtProfile_UserGuide.md)** - *Complete art integration system*
- **[Advanced Prop Placement User Guide](Authoring/AdvancedPropPlacement_UserGuide.md)** - *Sophisticated prop placement strategies*

### **🧪 Testing & Validation**
- **[Testing Strategy](Authoring/Tests/TestingStrategy.md)** - *Comprehensive testing approach*
- **[Editor Tools](Authoring/Editor/README.md)** - *Editor-specific authoring tools*

---

## 📖 **Technical Documentation**

### **🧬 Core Systems**
- **[MetVanDAMN Technical Specification](MetVanDAMN-Technical-Specification.md)** - *Comprehensive API and system documentation*
- **[Cross-Scale Integration Guide](Cross-Scale-Integration-Guide.md)** - *District → Sector → Room hierarchy*
- **[Cross-Scale Integration Manifesto](Cross-Scale-Integration-Manifesto.md)** - *Design principles for scalable systems*

### **🏗️ Implementation Guides**
- **[Issues & Implementation Plans](Issues/)** - *Current development issues and solutions*
- **[TLDL Chronicles](TLDL/)** - *Development history and implementation chronicles*

---

## 🌟 **Related TWG Assets**

### **🤖 Living Dev Agent & TLDA**
- **[Living Dev Agent Integration](Related-TWG-Assets/Living-Dev-Agent/)** - *TLDA Unity integration and workflow*
- **[The Scribe Documentation](Related-TWG-Assets/The-Scribe/)** - *Document generation and management*
- **[The Terminus System](Related-TWG-Assets/The-Terminus/)** - *Terminal-based development tools*

### **🔧 Development Tools**
- **[TWG Grid Layer Editor](Related-TWG-Assets/Grid-Layer-Editor/)** - *Advanced Unity grid and layer management*
- **[Debug & Validation Tools](Related-TWG-Assets/Debug-Tools/)** - *Comprehensive debugging and validation systems*

---

## **Finding What You Need**

### **📍 Quick Navigation by Purpose**

#### **🚀 I want to get started immediately**
→ **[Complete Setup Guide](COMPLETE-METVANDAMN-SETUP-GUIDE.md)**

#### **🔧 I want to understand the authoring tools**
→ **[Authoring Layer Guide](Authoring/README.md)**

#### **🧬 I want to understand the ECS architecture**
→ **[Cross-Scale Integration Guide](Cross-Scale-Integration-Guide.md)**

#### **🎨 I want to add art and visual content**
→ **[Biome Art Integration](Authoring/BiomeArtProfile_UserGuide.md)**

#### **🧪 I want to run tests and validation**
→ **[Testing Strategy](Authoring/Tests/TestingStrategy.md)**

#### **📝 I want to contribute to the project**
→ **[Contributing Guidelines](../../../CONTRIBUTING.md)** + **[Sacred Symbol Preservation](../../../docs/SACRED-SYMBOL-PRESERVATION-MANIFESTO.md)**

#### **🔍 I need to debug issues**
→ **[Debug Tools Overview](../../../src/DebugOverlayValidation/README.md)** + **[Test Fixes](../../../TEST_FIXES_APPLIED.md)**

---

## **Support & Community**

### **🤝 Getting Help**
1. **Check this documentation hub** for existing guides
2. **Search TLDL entries** for similar issues or implementations
3. **Review test cases** in `Authoring/Tests/`
4. **Create GitHub issues** for bugs or feature requests
5. **Join the Living Dev Agent community** for collaborative development

### **🎯 Contributing Guidelines**
- **Follow the Save The Butts! Manifesto** for sustainable development
- **Create TLDL entries** documenting your discoveries
- **Preserve all symbols** according to the Sacred Symbol Preservation Manifesto
- **Test thoroughly** using the provided validation tools
- **Document comprehensively** for future developers

---

## 📊 **Documentation Health Status**

### **✅ Complete & Current**
- Core setup and getting started guides
- Authoring layer documentation
- ECS architecture overviews
- GitBook-compatible structure

### **🔄 Recently Consolidated**
- All scattered documentation moved to unified location
- GitBook structure properly implemented
- Cross-references updated and validated
- Related TWG assets documented

### **📋 Areas for Future Enhancement**
- Video tutorials for complex setups
- More visual examples and screenshots
- Platform-specific optimization guides
- Advanced customization tutorials

---

*"Documentation is the bridge between confusion and enlightenment - build it strong, make it beautiful, and future developers will call you blessed."*

**🍑 ✨ Happy Learning & Building! ✨ 🍑**

---

## 📄 **Hub Information**

**Version**: 2.0 - Consolidated Documentation Hub
**Created**: January 2025
**Maintained by**: Living Dev Agent Community
**Philosophy**: Save The Butts! Documentation Excellence
**Purpose**: Eliminate confusion and enable MetVanDAMN mastery through comprehensive, consolidated documentation

*This hub is living documentation - it grows with the community's knowledge and discoveries.*
