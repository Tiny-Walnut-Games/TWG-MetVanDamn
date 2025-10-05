# 📓 Daily Ledger System — Keeper‑plus Ritual

> 📜 *"Good rituals make the repo hum whether you're at the keyboard or at the café."* — **Keeper's Almanac**

The Daily Ledger system preserves daily arc context, decisions, and glyphs for continuity across development sessions. It creates a structured daily export capsule that acts as a portable, structured record of decisions, glyphs, and unresolved hooks.

## 🧠 Purpose

- **Preserve Context**: Maintain daily development context across archive wall boundaries
- **Continuity Chain**: Link forward/backward to previous/next ledger for unbroken chain  
- **Arc Documentation**: Record arc names, key decisions, and running jokes
- **Re-entry Spells**: Enable context reload in new threads/sessions
- **Two-Dev Collaboration**: Support "two devs, one brain bank" workflow

## 📜 System Components

### Template System
- **Template**: `/docs/daily-ledger/_TEMPLATE.md` - Base template for all entries
- **Generated Entries**: `/docs/daily-ledger/YYYY-MM-DD.md` - Daily ledger files

### Generator Script
- **Location**: `scripts/chronicle-keeper/daily-ledger-generator.sh`
- **Purpose**: Create and maintain daily ledger entries with navigation

### Workflow Integration
- **Chronicle Keeper**: Automatic generation via `.github/workflows/chronicle-keeper.yml`
- **Triggers**: 🧠 issues, TLDL: comments, merged PRs, failed workflows, manual dispatch

## 🎯 Usage

### Manual Generation

```bash
# Generate today's ledger
scripts/chronicle-keeper/daily-ledger-generator.sh today "My Arc Name"

# Generate specific date ledger
scripts/chronicle-keeper/daily-ledger-generator.sh generate 2025-08-19 "Feature Development"

# Update navigation links in all entries
scripts/chronicle-keeper/daily-ledger-generator.sh link-entries

# List all existing entries
scripts/chronicle-keeper/daily-ledger-generator.sh list

# Dry run mode (preview changes)
scripts/chronicle-keeper/daily-ledger-generator.sh today "Test Arc" --dry-run --verbose
```

### Automatic Generation

The Daily Ledger system automatically generates entries when:

1. **🧠 Brain Emoji Issues**: Issues with 🧠 in title trigger ledger creation
2. **TLDL Comments**: Comments containing `TLDL:` or `📜` 
3. **Merged Pull Requests**: When PRs are merged successfully
4. **Failed Workflows**: When CI/build workflows fail
5. **Manual Dispatch**: Via GitHub Actions workflow dispatch

### Arc Name Detection

The system intelligently extracts arc names from:
- **Issue Titles**: Removes 🧠 emoji and uses cleaned title
- **PR Titles**: Uses PR title (first 50 chars)
- **Comments**: Uses "Lore Commentary Arc" 
- **Fallback**: Uses "Chronicle Keeper Arc" or "Daily Development Arc"

## 📋 Daily Ledger Structure

Each daily ledger entry contains:

### 1. Header
- **Date**: YYYY-MM-DD format
- **Arc**: Current development arc/theme
- **Mode**: Usually "Keeper‑plus"

### 2. Daily Ghost Sweep
- New/highlighted features since last ledger
- Current status of each feature

### 3. Badge Verdict Pass
- Status checks on key features/components
- Pass/fail/pending verdicts with reasons

### 4. Compact Transcript
- **Core Decisions**: Key prompts/decisions made
- **Artifacts & Commits**: Links to issues, PRs, commits
- **Glyphs/Running Jokes**: Memorable phrases or inside jokes
- **Unresolved Threads**: Pending tasks or questions

### 5. Re‑entry Spell
- Narrative summary in Keeper style
- Context for reloading tone/mood in future sessions

### 6. Navigation
- **Previous Ledger**: Link to previous day's entry
- **Next Ledger**: Link to next day's entry (or TBD)

## 🔗 Navigation Chain

The system maintains an unbroken chain of daily ledgers:

```
[2025-08-17] ← [2025-08-18] → [2025-08-19] → [TBD]
```

- **Forward Links**: Point to next chronological entry
- **Backward Links**: Point to previous chronological entry  
- **Auto-Update**: Links update automatically when new entries are created
- **First Entry**: Shows "None (First Entry)" for previous
- **Latest Entry**: Shows "TBD" for next

## 🧙‍♂️ Integration with Chronicle Keeper

The Daily Ledger system integrates seamlessly with the existing Chronicle Keeper:

- **TLDL Generation**: Runs alongside TLDL entry creation
- **Same Triggers**: Uses identical trigger patterns (🧠, 📜, etc.)
- **Shared Commits**: Both TLDL and Daily Ledger files committed together
- **Complementary**: TLDL preserves detailed technical lore, Daily Ledger preserves contextual flow

## ⚙️ Configuration

### Workflow Configuration

The Chronicle Keeper workflow includes embedded documentation:

```yaml
# 🧠 Daily Ledger Generator — Keeper‑plus Ritual
# Generates a new ledger from /docs/daily-ledger/_TEMPLATE.md
# Purpose: Preserve daily arc context, decisions, and glyphs for continuity.
# Usage: Fill each section before day's end. Use Re‑entry Spell to reload tone/context in new thread.
# Links: Backward/forward to previous/next ledger to maintain unbroken chain.
```

### Script Options

All generator script commands support:

- `--dry-run`: Preview changes without making them
- `--verbose`: Show detailed operation information  
- `--force`: Overwrite existing entries
- `--help`: Show detailed usage information

## 🍑 Buttsafe Impact

The Daily Ledger system provides multiple layers of cheek-preservation:

- **Context Continuity**: Prevents loss of development context
- **Decision Tracking**: Records why decisions were made
- **Arc Preservation**: Maintains narrative flow across sessions
- **Navigation Safety**: Always know where you came from and where you're going
- **Re-entry Protection**: Safe context reload via Re‑entry Spells

## 📚 Examples

### Typical Daily Ledger Entry

```markdown
### 🗓 **Date:** 2025-08-18  
**Arc:** Feature Implementation — *"The Great Refactor"*  
**Mode:** Keeper‑plus

### **1. Daily Ghost Sweep**
**New / highlighted features since last ledger:**
- **API Refactor** — Simplified endpoint structure (in progress).  
- **Database Migration** — Updated schema for v2 (completed).  
- **UI Components** — New design system integration (design review).  

### **4. Re‑entry Spell**  
> *The day we tamed the API beast and gave it proper REST. The database spoke in new tongues, and the UI donned fresh clothes. Tomorrow we test the waters.*

### **5. Navigation**
**Previous Ledger:** [2025-08-17](./2025-08-17.md) | **Next Ledger:** [2025-08-19](./2025-08-19.md)
```

## 🎯 Best Practices

1. **Fill Before Day's End**: Complete each section before ending work session
2. **Meaningful Arc Names**: Use descriptive names that capture the day's theme
3. **Update Re‑entry Spells**: Write compelling narrative summaries
4. **Link Context**: Reference relevant issues, PRs, commits in Artifacts section
5. **Preserve Glyphs**: Record memorable phrases and running jokes
6. **Track Unresolved**: Note pending items for next session

## 🔧 Troubleshooting

### Common Issues

1. **Entry Already Exists**: Use `--force` to overwrite existing entries
2. **Navigation Broken**: Run `link-entries` command to rebuild all links
3. **Template Missing**: Ensure `_TEMPLATE.md` exists in daily-ledger directory
4. **Permission Issues**: Make sure script is executable (`chmod +x`)

### Debugging

```bash
# Check current entries
scripts/chronicle-keeper/daily-ledger-generator.sh list

# Preview generation
scripts/chronicle-keeper/daily-ledger-generator.sh today "Debug Arc" --dry-run --verbose

# Fix navigation
scripts/chronicle-keeper/daily-ledger-generator.sh link-entries --verbose
```

## 🤝 Contributing

When contributing to the Daily Ledger system:

1. **Preserve Template Structure**: Don't modify core template sections
2. **Test Navigation**: Always verify forward/backward links work
3. **Validate Output**: Run validation tools after changes
4. **Document Changes**: Update this README for significant modifications
5. **Follow Patterns**: Match existing Chronicle Keeper conventions

---

**Part of the Living Dev Agent Template - Two Devs, One Brain Bank Edition**

🍑 Built with the Sacred Scrolls of Documentation and tested in the fires of actual development.