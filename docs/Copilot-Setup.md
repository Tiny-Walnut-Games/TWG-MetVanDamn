# 🤖 Copilot & Living Dev Agent Setup Guide

This guide will help you set up your development environment to work effectively with GitHub Copilot and the Living Dev Agent workflow integrated into this template.

---

## 🎯 Quick Setup

### 1. **Clone & Initialize**
```bash
# Clone this template repository
git clone https://github.com/your-username/your-new-repo.git
cd your-new-repo

# Run the initialization script
chmod +x scripts/init_agent_context.sh
scripts/init_agent_context.sh
```

### 2. **Configure Copilot**
- Ensure GitHub Copilot is installed in your IDE (VS Code, JetBrains, etc.)
- Review and customize `TWG-Copilot-Agent.yaml` for your project
- Configure MCP (Model Context Protocol) settings in `mcp-config.json`

### 3. **Validate Setup**
```bash
# Run linters and validation
python scripts/ecs_system_linter.py --path src/
python scripts/validate_docs.py --tldl-path docs/

# Test CI pipeline locally (if using act)
act -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:act-latest
```

---

## 🪟 Windows + Rider Quick Start (No bash knowledge required)

Use these copy-paste steps in Rider’s Terminal (PowerShell) or Windows Terminal:

```powershell
# 1) Ensure Python and deps
python -V
pip install -r "Assets/Plugins/living-dev-agent/scripts/requirements.txt"

# 2) Initialize LDA (Windows wrapper uses Git Bash automatically)
"Assets/Plugins/living-dev-agent/scripts/init_agent_context.cmd"

# Optional: create a TLDL entry right away
"Assets/Plugins/living-dev-agent/scripts/init_agent_context.cmd" --create-tldl "MyJourneyToButtsafety"

# 3) Start the local MCP server (keep this running)
"Assets/Plugins/living-dev-agent/scripts/mcp_server.cmd"
```

If Python isn’t on PATH, install it from https://www.python.org/downloads/ and re-open the terminal.

### Rider Run Configs (recommended)
- Add Configuration → Shell Script → Script path:
  - Init: Assets/Plugins/living-dev-agent/scripts/init_agent_context.cmd
  - MCP: Assets/Plugins/living-dev-agent/scripts/mcp_server.cmd
- Working directory: Assets/Plugins/living-dev-agent
- Environment (optional): LDA_ASSUME_YES=1

### Git in Rider: nested repo visibility
The Living Dev Agent folder is its own Git repo (`Assets/Plugins/living-dev-agent/.git`). To see changes (like new TLDL files) in Rider:
- File → Settings → Version Control → Directory Mappings → Add
- Select: Assets/Plugins/living-dev-agent
- Apply. Now commits for that folder appear in its own Git root.

---

## 📋 Living Dev Log (TLDL) Workflow

### **What is TLDL?**
TLDL (The Living Dev Log) is our structured approach to documenting development decisions, discoveries, and learnings in real-time.

### **TLDL Entry Structure**
```yaml
# docs/tldl_template.yaml
entry_id: "TLDL-YYYY-MM-DD-DescriptiveTitle"
author: "Your Name or @copilot"
context: "Issue #XX or Feature Description"
summary: "One-line summary of what was accomplished"

discoveries:
  - key_finding: "What you learned"
    impact: "Why it matters"
    evidence: "Link to code/docs/discussion"

actions_taken:
  - action: "What you did"
    rationale: "Why you did it"
    result: "What happened"

next_steps:
  - step: "What to do next"
    priority: "high|medium|low"
    assignee: "Who should do it"
```

### **Creating TLDL Entries**
```bash
# Manual creation
cp docs/tldl_template.yaml TLDL/entries/TLDL-$(date +%Y-%m-%d)-YourTopicHere.md

# Automated via script
scripts/init_agent_context.sh --create-tldl "YourTopicHere"
```

On Windows:
```powershell
"Assets/Plugins/living-dev-agent/scripts/init_agent_context.cmd" --create-tldl "YourTopicHere"
```

TLDLs are created in: Assets/Plugins/living-dev-agent/docs/

---

## 🕰️ DevTimeTravel Integration

### **What is DevTimeTravel?**
DevTimeTravel captures development state snapshots, enabling you to understand the context and decisions that led to current code state.

### **Snapshot Configuration**
```yaml
# docs/devtimetravel_snapshot.yaml
project_info:
  name: "Your Project Name"
  version: "1.0.0"
  description: "Project description"
  
snapshot_config:
  frequency: "on_commit"  # daily, on_commit, manual
  include_patterns:
    - "src/**/*"
    - "docs/**/*"
    - "scripts/**/*"
  exclude_patterns:
    - "node_modules/**"
    - "*.tmp"
    - ".git/**"
    
context_capture:
  git_metadata: true
  file_checksums: true
  dependency_graph: true
  build_artifacts: false
```

### **Capturing Snapshots**
```bash
# Manual snapshot
scripts/devtimetravel_snapshot.sh --message "Feature implementation complete"

# Automated on commit (via git hooks)
# See .git/hooks/post-commit for setup
```

On Windows (HTTP via MCP server):
```powershell
# With server running, you can POST via REST Client or curl
# curl -X POST http://localhost:8000/tools/capture_devtimetravel_snapshot -H "Content-Type: application/json" -d '{"message":"Feature complete"}'
```

---

## 🔍 Linting & Validation Tools

### One-shot validator
```powershell
# Windows
"Assets/Plugins/living-dev-agent/scripts/validate_setup.sh"
```

### **ECS System Linter**
Validates ECS system architecture and component usage:
```bash
python scripts/ecs_system_linter.py --path src/ --output-format json --health-threshold 0.8
```

### **Symbol Resolution Linter**
Checks for missing dependencies and symbol resolution issues:
```bash
python scripts/symbolic_linter.py --path src/ --strict-mode
```

### **Debug Overlay Validation**
Ensures debug systems are properly integrated:
```bash
python scripts/debug_overlay_validator.py --path src/DebugOverlayValidation/
```

---

## 🛠️ IDE Configuration

### **VS Code Settings**
```json
{
  "github.copilot.enable": {
    "*": true,
    "yaml": true,
    "markdown": true
  },
  "github.copilot.advanced": {
    "debug.overrideEngine": "copilot-codex",
    "debug.testOverrideEngine": "copilot-codex"
  },
  "files.associations": {
    "*.tldl": "yaml",
    "devtimetravel_*.yaml": "yaml"
  }
}
```

### **JetBrains Rider**
- Install GitHub Copilot plugin (Settings → Plugins → Marketplace → GitHub Copilot)
- Add the two Run Configurations described above (Init, MCP)
- Map nested Git root (Version Control → Directory Mappings)
- Optional: Install "REST Client" plugin to call MCP endpoints from Rider

---

## 🚀 Advanced Workflows

### **Ping-and-Fix Methodology for AI Collaboration**

**Revolutionary Discovery**: Interactive guidance during task execution creates 2x faster resolution compared to traditional review cycles.

#### **The New Workflow Pattern**
```bash
# Instead of: Code → Review → Change Request → Fix → Re-Review
# Use: Code → @copilot issue found, fix please → Immediate Fix → Continue
```

#### **Practical Implementation**
```bash
# When CI fails:
# ❌ Traditional: Wait for full completion, then create formal review
# ✅ Optimized: @copilot CI failing on PyYAML import - add dependency step

# When validation has false positives:  
# ❌ Traditional: Create detailed change request document
# ✅ Optimized: @copilot neutrality check flagging .md files - exclude documentation
```

#### **Key Benefits**
- **Immediate course correction**: Issues resolved in real-time
- **Prevents error compounding**: Small fixes prevent large rework
- **Continuous progress**: No workflow bottlenecks
- **Context preservation**: Fixes applied while context is fresh

### **Slot Instructions Philosophy**

**Core Concept**: Provide contextual instructions **during task execution**, not just at the beginning or end.

This approach enables:
- **Dynamic task adaptation**: Requirements evolve based on discoveries
- **Real-time quality assurance**: Issues caught and resolved immediately  
- **Collaborative debugging**: Human insight + AI execution in tight loops

### **Interactive vs Batch AI Collaboration**

| Batch Processing (Traditional)  | Interactive Guidance (Optimized)         |
|---------------------------------|------------------------------------------|
| Complete instructions upfront   | Initial direction + real-time guidance   |
| Wait for full task completion   | Monitor progress continuously            |
| Review entire result at end     | Incremental corrections during execution |
| Request changes if needed       | Achieve correct result in first cycle    |

**Evidence**: See `TLDL/entries/TLDL-2025-08-01-WorkflowEvolutionInsights.md` for detailed analysis of methodology effectiveness.

### **Automated TLDL Generation**
Configure Copilot to auto-generate TLDL entries:
```yaml
# In TWG-Copilot-Agent.yaml
copilot_config:
  auto_tldl:
    enabled: true
    triggers:
      - "on_commit"
      - "on_pr_merge"
    template: "docs/tldl_template.yaml"
```

### **DevTimeTravel CI Integration**
Add to your CI pipeline:
```yaml
- name: Capture DevTimeTravel Snapshot
  run: |
    scripts/devtimetravel_snapshot.sh --ci-mode --branch ${{ github.ref_name }}
```

### **Real-time Code Health Monitoring**
Set up continuous linting:
```bash
# Watch mode for development
python scripts/ecs_system_linter.py --watch --path src/
```

---

## 🐛 Troubleshooting

### Why didn’t my TLDL appear in the commit window?
- The Living Dev Agent folder is a separate Git repo. Add it as a Git root in Rider so new files under Assets/Plugins/living-dev-agent/docs/ show up for commit.
- Verify creation: check that a file like `Assets/Plugins/living-dev-agent/TLDL/entries/TLDL-YYYY-MM-DD-MyJourneyToButtsafety.md` exists.
- If you ran from PowerShell and nothing happened: try the Windows wrapper `init_agent_context.cmd` or set `LDA_ASSUME_YES=1` to avoid interactive prompts.

### **Common Issues**

1. **Linting fails with "Module not found"**
   ```bash
   pip install -r scripts/requirements.txt
   python -m pip install --upgrade pip
   ```

2. **DevTimeTravel snapshots too large**
   - Adjust `exclude_patterns` in `devtimetravel_snapshot.yaml`
   - Use `--compress` flag for snapshot script

3. **Copilot not suggesting TLDL entries**
   - Check `TWG-Copilot-Agent.yaml` configuration
   - Ensure MCP server is running: `mcp-config.json`

### **Debug Commands**
```bash
# Validate complete setup
scripts/validate_setup.sh

# Check linter health
python scripts/ecs_system_linter.py --self-test

# Test DevTimeTravel integration
scripts/devtimetravel_snapshot.sh --dry-run --verbose
```

---

## 📚 Additional Resources

- **TLDL Best Practices**: See existing entries in `docs/` for examples
- **ECS Linting Rules**: `scripts/ecs_system_linter.py --help`
- **DevTimeTravel Documentation**: [Link to full docs]
- **Copilot Optimization**: `TWG-Copilot-Agent.yaml` comments
- **[Workflow Evolution Insights](../TLDL/entries/TLDL-2025-08-01-WorkflowEvolutionInsights.md)**: Revolutionary AI collaboration methodology discoveries

### **🚀 Breakthrough Workflow Patterns**

#### **Interactive AI Collaboration**
- **Ping-and-Fix Methodology**: Real-time issue resolution with @copilot
- **Slot Instructions Philosophy**: Guidance during task execution, not just at start/end
- **Collaborative Debugging**: Human insight + AI execution in tight feedback loops

#### **Efficiency Multipliers**  
- **2x faster resolution** compared to traditional review cycles
- **Real-time course correction** prevents compounding errors
- **Context preservation** while issues are fresh and actionable

#### **Proven Implementation Examples**
```bash
# CI/CD Issues
@copilot CI failing on PyYAML import - add dependency installation step

# Validation Problems  
@copilot neutrality check has false positives - exclude .md documentation files

# Code Quality
@copilot this function needs error handling for the edge case when input is null
```

See detailed analysis and methodology in `../TLDL/entries/TLDL-2025-08-01-WorkflowEvolutionInsights.md`.

---

## 🤝 Contributing

When contributing to this project:

1. **Always create TLDL entries** for significant changes
2. **Run linters before committing**: `scripts/run_all_linters.sh`
3. **Update DevTimeTravel config** if changing project structure
4. **Follow the established patterns** shown in existing code

For detailed contribution guidelines, see [CONTRIBUTING.md](../CONTRIBUTING.md).
