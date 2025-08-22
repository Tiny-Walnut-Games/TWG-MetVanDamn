# 🧠📜 Auto-Quills Comment Template System

## Overview

The Auto-Quills system provides structured comment templates for Chronicle Keeper triggers, making lore preservation more consistent and accessible to contributors.

## Quick Start

```bash
# List available templates
lda template --list

# Generate a template interactively
lda template --scenario bug_discovery

# Generate with defaults (non-interactive)
lda template --scenario debugging_ritual --non-interactive

# Search for templates
lda template --search "CI"
```

## Available Templates

### 🐛 Bug Discovery (`bug_discovery`)
**Use when:** You've found an interesting bug worth documenting
**Includes:** Root cause analysis, reproduction steps, lessons learned
**Chronicle Keeper Triggers:** 📜, TLDL:

### 🔍 Debugging Ritual (`debugging_ritual`) 
**Use when:** You've completed a complex debugging session
**Includes:** Investigation process, breakthrough moments, methodologies
**Chronicle Keeper Triggers:** 📜, TLDL:

### 🚨 CI Failure Analysis (`ci_failure_analysis`)
**Use when:** Analyzing build system or deployment failures
**Includes:** Failure patterns, root causes, prevention strategies
**Chronicle Keeper Triggers:** 📜, TLDL:

### 💭 Lore Reflection (`lore_reflection`)
**Use when:** Capturing insights or development wisdom
**Includes:** Deep understanding, practical wisdom, knowledge transfer
**Chronicle Keeper Triggers:** 📜, TLDL:

## Template Structure

All templates follow the TLDL format:
- **Trigger:** What initiated this documentation
- **Discovery/Quest/Context:** The problem or situation
- **Lore:** Deep understanding and root causes  
- **Lessons:** Practical wisdom for future developers

## Integration with Chronicle Keeper

Templates automatically include Chronicle Keeper triggers (📜, TLDL:) that will:
1. Be detected by the Chronicle Keeper workflow
2. Generate appropriate TLDL entries
3. Preserve wisdom in the living documentation

## Customization

Templates are stored in `templates/comments/` as YAML files with:
- Structured placeholders with descriptions and defaults
- Metadata for categorization and search
- Usage examples and related templates

## Testing

Run the test suite to validate the template system:
```bash
python3 tests/test_template_system.py
```

## Contributing

To add new templates:
1. Create a new YAML file in `templates/comments/`
2. Update `templates/comments/registry.yaml`
3. Follow the established template structure
4. Include Chronicle Keeper triggers
5. Add appropriate tests

---

*Part of the Living Dev Agent ecosystem - Save the butts! 🍑*