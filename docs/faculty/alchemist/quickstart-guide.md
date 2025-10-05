# 🧪 Alchemist Faculty Quickstart Guide

**Transform narrative energy into empirical evidence in 15 minutes!**

## What is the Alchemist Faculty?

The Alchemist Faculty is the **distillation engine** of TWG-TLDA that transforms volatile narrative substrates (Gu Pot issues) into stable, analyzable compounds (validated claims). Think of it as your **experimental lab** where stories become science.

### Core Philosophy
> *"Every narrative contains the seeds of its own validation. The Alchemist simply provides the crucible."* - Bootstrap Sentinel

## 🚀 Quick Setup (5 minutes)

### Prerequisites
- [ ] Unity 6 (6000.2.0f1 or later) with TWG-TLDA project
- [ ] Python 3.11+ with `requirements.txt` dependencies
- [ ] Active Gu Pot issue in "distilled" stage
- [ ] Git repository with proper branch setup

### Installation
```bash
# 1. Navigate to your TWG-TLDA project
cd /path/to/TWG-TLDA

# 2. Install Alchemist dependencies
pip install -r scripts/requirements.txt

# 3. Verify Alchemist tools
python scripts/alchemist-faculty/validate_setup.py

# 4. Initialize Alchemist workspace
scripts/alchemist-faculty/init_workspace.sh
```

### Directory Structure Created
```
gu_pot/
├── issue-{number}/
│   ├── manifest_v1.json        # Experiment definition
│   ├── runs/                   # Execution artifacts
│   ├── claims/                 # Generated claims
│   │   ├── hypotheses/         # Initial hypotheses
│   │   ├── validated/          # Promoted claims
│   │   └── regressions/        # Failed validations
│   └── report/                 # Synthesis outputs
└── templates/                  # Manifest templates
```

## 🎯 Your First Alchemist Experiment (10 minutes)

### Step 1: Prepare Your Gu Pot Issue

Ensure your GitHub issue has reached **"distilled"** stage with:
- ✅ Clear logline (< 280 characters)
- ✅ Defined tension and stakes
- ✅ **Irreversible shift declared**
- ✅ Measurable residue fields populated

**Example Issue Format:**
```markdown
## Logline
Combat system feels disconnected from narrative progression

## Tension
Players invest in story but battles feel like separate minigame

## Irreversible Shift
Integrate XP gain with narrative beats - combat outcomes affect story branches

## Measurable Residue
- Player engagement time in combat +15%
- Story completion rate correlation with combat participation
- Tutorial skip rate decrease
```

### Step 2: Generate Experiment Manifest

```bash
# Using Python scaffold generator
python scripts/alchemist-faculty/generate_manifest.py \
  --issue-number 123 \
  --stage distilled \
  --output gu_pot/issue-123/

# Or using Unity tool
# Unity Menu: Tools > Alchemist > Generate Manifest
```

**Generated Manifest Preview:**
```yaml
metadata:
  name: "Combat-Narrative Integration Experiment"
  alchemist_version: "0.1.0"
  generated_from: "https://github.com/OWNER/REPO/issues/123"

origin:
  type: "gu_pot"
  issue_number: 123
  stage_at_evaluation: "distilled"
  irreversible_shift_declared: true
  
experimental_context:
  baseline_metrics: ["player_engagement_time", "story_completion_rate"]
  hypothesis: "Narrative-integrated combat increases engagement"
  
validation_criteria:
  min_confidence_threshold: 0.7
  min_baseline_improvement: 0.15
```

### Step 3: Execute Experiment

```bash
# Run the experiment with deterministic seeding
python scripts/alchemist-faculty/run_experiment.py \
  --manifest gu_pot/issue-123/manifest_v1.json \
  --output-dir gu_pot/issue-123/runs/

# Monitor progress
tail -f gu_pot/issue-123/runs/latest/experiment.log
```

### Step 4: Validate and Promote Claims

```bash
# Validate experimental results
python scripts/alchemist-faculty/validate_claims.py \
  --run-dir gu_pot/issue-123/runs/2025-09-06T02-40-10Z/ \
  --output-dir gu_pot/issue-123/claims/

# Results will be categorized as:
# - validated/    (serum candidates)
# - regressions/  (failed improvements)
# - anomalies/    (requires investigation)
```

### Step 5: Update Gu Pot Issue

```bash
# Generate evidence links and update issue
python scripts/alchemist-faculty/update_issue_evidence.py \
  --issue-number 123 \
  --claims-dir gu_pot/issue-123/claims/ \
  --dry-run  # Remove for actual update
```

## 🧬 Understanding the Alchemist Pipeline

### Stage Flow
```
Gu Pot Issue (distilled) 
    ↓
Manifest Generation 
    ↓
Experiment Execution 
    ↓
Claims Validation 
    ↓
Promotion (serum/antitoxin/compost)
    ↓
Evidence Links Update
```

### Key Concepts

**🧪 Claims**: Empirical statements derived from experimental data
- *Hypothesis*: "Combat integration improves engagement by 15%"
- *Validated*: Hypothesis confirmed with statistical significance
- *Regression*: Hypothesis failed or showed negative impact

**⚗️ Origin Binding**: Every claim traces back to its Gu Pot narrative
```json
{
  "origin": {
    "type": "gu_pot",
    "issue_number": 123,
    "logline_hash": "sha256:abc123...",
    "extracted_on": "2025-09-06T02:55:00Z"
  }
}
```

**🔬 Deterministic Reproducibility**: All experiments use seeded randomness
```yaml
seed_configuration:
  global_seed: 42
  corpus_seed: 123
  processing_seed: 456
```

## 🛠️ Available Tools

### Python Scripts
- `generate_manifest.py` - Create experiment manifests from Gu Pot issues
- `run_experiment.py` - Execute deterministic experiments
- `validate_claims.py` - Promote claims based on validation criteria
- `update_issue_evidence.py` - Update GitHub issues with evidence links
- `scaffold_workspace.py` - Initialize directory structure

### C# Unity Tools
- **Alchemist Manifest Generator** - Unity Editor window for manifest creation
- **Experiment Runner** - Unity-based experiment execution
- **Claims Validator** - Built-in validation and promotion system
- **Report Synthesizer** - Generate markdown reports from claims

### Templates
- `templates/alchemist/manifest_template.yaml` - Base experiment manifest
- `templates/alchemist/claim_template.json` - Claim structure template
- `templates/alchemist/issue_evidence_template.md` - GitHub issue update template

## 🎮 Unity Integration

### Opening Alchemist Tools
1. **Main Menu**: `Tools > Alchemist Faculty`
2. **Sub-menus**:
   - `Generate Manifest` - Create experiment definitions
   - `Run Experiment` - Execute experiments in Unity
   - `Validate Claims` - Review and promote results
   - `Synthesize Report` - Generate final reports

### Workflow Integration
- **Auto-detection**: Unity tools automatically detect "distilled" Gu Pot issues
- **Real-time progress**: Watch experiment execution in Unity Console
- **Artifact browser**: Browse generated claims directly in Unity Editor

## 📊 Quality Assurance

### Promotion Gate Checklist
Before any claim becomes "serum" (fully validated), verify:
- [ ] **Deterministic run lineage** - Reproducible with same seed
- [ ] **Baseline comparison** - Delta calculated against captured baseline
- [ ] **Explicit evidence files** - All metrics and logs referenced
- [ ] **Confidence methodology** - Validation method documented
- [ ] **Origin binding** - Traceable back to Gu Pot issue

### Common Validation Failures
1. **Insufficient baseline improvement** - Results don't meet minimum threshold
2. **High variance** - Results inconsistent across runs
3. **Missing artifacts** - Incomplete experimental data
4. **Schema violations** - Malformed claim or manifest files

## 🚨 Troubleshooting

### Setup Issues
**Problem**: `ModuleNotFoundError: alchemist_faculty`
```bash
# Solution: Install in development mode
pip install -e scripts/alchemist-faculty/
```

**Problem**: Unity tools not appearing in menu
```bash
# Solution: Refresh Unity Editor and check console for errors
# Ensure TWG.TLDA.Alchemist namespace is properly imported
```

### Experiment Failures
**Problem**: Experiment hangs or times out
```bash
# Check resource limits in manifest
# Verify Unity project is not busy with other tasks
# Monitor system resources during execution
```

**Problem**: Claims validation fails
```bash
# Review validation criteria in manifest
# Check baseline metrics were captured correctly
# Verify all required artifact files exist
```

### Integration Issues
**Problem**: GitHub issue updates fail
```bash
# Verify GitHub authentication
# Check issue permissions
# Review evidence template formatting
```

## 🎯 Next Steps

### Advanced Usage
1. **Custom Validation Algorithms** - Implement domain-specific confidence scoring
2. **Batch Processing** - Run multiple experiments from issue queue
3. **CI/CD Integration** - Automate Alchemist pipeline in GitHub Actions
4. **Dashboard Integration** - Visualize narrative progression vs empirical adoption

### Learning Resources
- `docs/faculty/alchemist/promotion-gating-checklist.md` - Complete quality gates
- `docs/faculty/alchemist/schema-design-notes.md` - Schema architecture details
- `docs/School-Experiment-Stage2-Manifests.md` - Related manifest patterns
- `TLDL/entries/` - Real-world usage examples in TLDL entries

### Community
- Join #alchemist-faculty channel for support
- Share experimental results in TLDL entries
- Contribute validation algorithms and templates

---

## 🧙‍♂️ Bootstrap Sentinel's Wisdom

*"The Alchemist Faculty transforms the volatile chaos of creative inspiration into the stable gold of reproducible evidence. Each experiment is a spell cast into the future of understanding - choose your reagents wisely, document your incantations precisely, and trust the crucible of validation to reveal truth from wishful thinking."*

**Remember**: Every serum was once just a story. The Alchemist makes stories into science, but science that remembers its narrative heart.

---
*Version: 0.1.0*  
*Last Updated: 2025-09-06*  
*Compatible with: TWG-TLDA v1.0+, Unity 6+*