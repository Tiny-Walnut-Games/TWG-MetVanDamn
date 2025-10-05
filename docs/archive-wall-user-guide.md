# 🧠📜 Archive Wall Continuity System — User Guide

> *"The best defense against forgotten wisdom is a good offense of systematic preservation."* — **Chronicle Keeper's Tactical Manual, Vol. II**

---

## Overview

The Archive Wall Continuity System solves the fundamental problem of conversation threads that extend beyond the "archive wall" — the point where conversation history becomes inaccessible. This system provides three interconnected features:

1. **📜 Capsule Scrolls** — Context anchors that preserve conversation essence
2. **👻 Lost Features Ledger** — Systematic tracking of unimplemented ideas  
3. **📊 Daily Ledger & Export Ritual** — Automated daily context preservation

---

## Quick Start

### Basic Usage

```bash
# Check system status
scripts/archive-wall status

# Generate today's daily ledger
scripts/archive-wall daily-ledger --generate-today

# Create a capsule scroll for a development arc
scripts/archive-wall capsule-scroll --arc-name "My Feature Development"

# Update the lost features ledger
scripts/archive-wall ghost-sweep --update
```

### Common Workflows

#### 1. Starting a New Development Arc

```bash
# Create a capsule scroll to anchor the beginning
scripts/archive-wall capsule-scroll \
  --arc-name "New Feature Implementation" \
  --start-date "2025-08-18" \
  --status "active"

# Generate daily ledger to capture initial state  
scripts/archive-wall daily-ledger --generate-today \
  --arc-name "New Feature Implementation" \
  --mode "Feature Development"
```

#### 2. End of Day Routine

```bash
# Update daily ledger with git activity
scripts/archive-wall daily-ledger --update-existing docs/daily-ledger/$(date +%Y-%m-%d).md

# Check system status
scripts/archive-wall status

# Update ghost sweep weekly
scripts/archive-wall ghost-sweep --update
```

#### 3. Creating Context from Existing Daily Ledger

```bash
# Generate capsule scroll from daily ledger
python3 scripts/capsule_scroll_generator.py \
  --from-daily-ledger docs/daily-ledger/2025-08-18.md
```

---

## System Components

### 📜 Capsule Scrolls

**Purpose**: Preserve conversation context that extends beyond archive walls.

**Structure**: Each capsule scroll contains:
- **Arc Name**: Thematic title of the development period
- **Timeframe**: Start/end dates or ongoing status  
- **Core Decisions**: Key technical and process decisions
- **Key Artifacts**: Links to issues, PRs, commits, TLDL entries
- **Glyphs & Running Jokes**: Cultural context and terminology
- **Unresolved Threads**: Open questions and pending work
- **Re-entry Spell**: 3-sentence context reset for future conversations

**Locations**:
- `capsules/active/` — Currently relevant scrolls
- `capsules/archived/` — Completed or obsolete scrolls  
- `capsules/templates/` — Template files

### 👻 Lost Features Ledger  

**Purpose**: Systematically track discussed but unimplemented features.

**Ghost Categories**:
- **New Ghosts**: Recently discussed features
- **Recurring Phantoms**: Features that appear in multiple conversations
- **Recently Escaped**: Features successfully implemented
- **Laid to Rest**: Features explicitly rejected

**Tracking**:
- Ghost Rank (priority level)
- Spawn Potential (readiness for implementation)
- Chronicle Keeper Integration opportunities
- Buttsafe Impact assessment

### 🔮 Oracle Visions Archive

**Purpose**: Systematic archival of Oracle Faculty vision reports with full lineage tracking.

**Vision Categories**:
- **Manual Visions**: Keeper-requested strategic analysis
- **Advisor-Triggered**: High-priority findings requiring future-sight
- **System Patterns**: Auto-detected anomalies and repeated failures
- **Intuition Prompts**: Faculty consensus uncertainty resolution

**Tracking**:
- Vision UUID and source intel linkage
- Trigger reason and priority classification
- Oracle forecast scenarios and recommendations
- Disposition status (pending/adopted/rejected/deferred)
- Resulting changes and implementation tracking

**Integration**: Vision reports cross-link with TLDL entries, issues, and PRs for complete context preservation.

### 📊 Daily Ledger & Export Ritual

**Purpose**: Automated daily capture of development context.

**Sections**:
1. **Daily Ghost Sweep**: New features and capabilities detected
2. **Badge Verdict Pass**: Validation status and quality metrics  
3. **Compact Transcript**: Core decisions and key artifacts
4. **Re-entry Spell**: Context preservation for future conversations

**Automation**: Analyzes git activity, file changes, and validation results.

---

## CLI Reference

### `scripts/archive-wall`

Main command interface for all Archive Wall operations.

#### Commands

**`capsule-scroll [options]`**
- `--arc-name "Name"` — Arc title (required for manual creation)
- `--start-date YYYY-MM-DD` — Start date
- `--end-date YYYY-MM-DD` — End date  
- `--status [active|archived]` — Capsule status
- `--from-daily-ledger path` — Generate from daily ledger

**`daily-ledger [options]`**
- `--generate-today` — Create ledger for today
- `--date YYYY-MM-DD` — Create ledger for specific date
- `--update-existing path` — Update existing ledger with current git activity
- `--arc-name "Name"` — Override arc name
- `--mode "Mode"` — Override development mode

**`ghost-sweep [options]`**
- `--update` — Create or update today's Lost Features Ledger
- `--list` — List existing ledgers

**`status`**
- Show system status and health metrics

### Direct Script Usage

#### `scripts/capsule_scroll_generator.py`

```bash
# Create manual capsule scroll
python3 scripts/capsule_scroll_generator.py \
  --arc-name "Feature Development Arc" \
  --start-date "2025-08-18" \
  --status "active"

# Generate from daily ledger
python3 scripts/capsule_scroll_generator.py \
  --from-daily-ledger docs/daily-ledger/2025-08-18.md
```

#### `scripts/daily_ledger_generator.py`

```bash
# Generate today's ledger
python3 scripts/daily_ledger_generator.py --generate-today

# Generate specific date
python3 scripts/daily_ledger_generator.py --date "2025-08-18"

# Update existing ledger
python3 scripts/daily_ledger_generator.py \
  --update-existing docs/daily-ledger/2025-08-18.md
```

---

## Integration with Chronicle Keeper

The Archive Wall system integrates with the existing Chronicle Keeper for automatic TLDL generation:

### Trigger Patterns

- **🧠📜** in issue titles — Triggers both TLDL and Capsule Scroll generation
- **archive wall**, **continuity**, **context preservation** keywords
- Failed workflows that indicate context loss
- Long-running issues that cross archive boundaries

### Configuration

Chronicle Keeper configuration in `scripts/chronicle-keeper/scribe-config.yml` includes:

```yaml
# Archive Wall integration
integration:
  archive_wall:
    enabled: true
    capsule_triggers:
      - "🧠📜"
      - "archive wall"  
      - "continuity crisis"
    daily_ledger:
      enabled: true
      auto_generate: true
```

---

## File Organization

```
/home/runner/work/living-dev-agent/living-dev-agent/
├── capsules/
│   ├── README.md                    # System overview
│   ├── active/                      # Currently relevant capsules
│   ├── archived/                    # Completed capsules
│   └── templates/                   # Template files
├── docs/
│   ├── daily-ledger/               # Daily export rituals
│   ├── lost-features-ledger-*.md   # Ghost tracking
│   └── archive-wall-user-guide.md  # This file
└── scripts/
    ├── archive-wall                 # Main CLI interface
    ├── capsule_scroll_generator.py  # Capsule generation
    └── daily_ledger_generator.py    # Daily ledger automation
```

---

## Best Practices

### When to Create Capsule Scrolls

- **Arc Transitions**: Major feature development phases
- **Context Shifts**: When conversation focus changes significantly  
- **Archive Warnings**: When approaching conversation limits
- **Decision Points**: Major architectural or process decisions
- **Handoffs**: When transferring work or context to others

### Daily Ledger Maintenance

- **End of Day**: Update with `--update-existing` to capture git activity
- **Arc Changes**: Update arc name when focus shifts
- **Validation Issues**: Check Badge Verdict Pass for quality trends
- **Context Gaps**: Use Re-entry Spells to bridge understanding

### Ghost Management

- **Weekly Reviews**: Update Lost Features Ledger status
- **Priority Assessment**: Adjust Ghost Rank based on current needs
- **Spawn Decisions**: Convert high-priority ghosts to issues/capsules
- **Exorcism Planning**: Prioritize ghost-to-reality conversion

---

## Troubleshooting

### Common Issues

**"Template not found" error:**
```bash
# Ensure directories exist
mkdir -p capsules/templates capsules/active capsules/archived docs/daily-ledger
```

**Git activity not detected:**
```bash
# Check git configuration and recent commits
git log --oneline --since="yesterday"
```

**Validation errors:**
```bash  
# Run full validation suite
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/
```

### Getting Help

- `scripts/archive-wall help` — General help
- `scripts/archive-wall <command> --help` — Command-specific help
- `scripts/archive-wall status` — System health check
- Check existing capsules and ledgers for examples

---

## Advanced Usage

### Custom Templates

Modify `capsules/templates/capsule-scroll-template.md` to customize:
- Section structure
- Metadata fields  
- Integration hooks
- Styling and formatting

### Automation Integration

Add to CI/CD workflows:
```yaml
- name: Update Daily Ledger
  run: scripts/archive-wall daily-ledger --generate-today
  
- name: Archive Context on Long-Running Branches  
  run: scripts/archive-wall capsule-scroll --arc-name "$BRANCH_NAME Development"
```

### Chronicle Keeper Enhancement

Add custom trigger patterns in `scripts/chronicle-keeper/scribe-config.yml`:
```yaml
lore_keywords:
  - "your-custom-trigger"
  - "🎭" # Your custom emoji
```

---

*Generated by the Archive Wall Continuity System — Defending against the entropy of forgotten wisdom.*