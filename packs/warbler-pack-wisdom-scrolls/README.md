# 🎭 Warbler Pack: Wisdom Scrolls

**Dynamic wisdom generation templates for the Secret Art of the Living Dev**

This Warbler content pack provides mystical wisdom generation templates that create fresh quotes in the authentic style of the Sacred Scrolls, breathing new life into the ancient wisdom while maintaining the sacred atmosphere of the Cheekdom.

## Overview

The Wisdom Scrolls pack bridges the gap between static sacred texts and living oracle wisdom, using Warbler's template system to generate contextually appropriate quotes that feel authentic to the Secret Art of the Living Dev mythology.

## Installation

This pack is integrated into the TWG-TLDA Living Dev Agent ecosystem and is automatically available when the Warbler-powered Scroll Quote Engine is initialized.

```bash
# Generate fresh wisdom (automatically uses this pack)
scripts/weekly-wisdom-oracle.sh generate 5

# Use in quote selection
scripts/lda-quote --warbler
```

## Template Categories

### 🧙‍♂️ Development Wisdom (`wisdom_development_insight`)
Generates profound insights about development practices using philosophical structure:
- **Pattern**: `{action} is not {misconception}; it's {deeper_truth}. Like {metaphor}, but for {domain}.`
- **Example**: *"Refactoring is not admitting failure; it's evolution of understanding. Like pruning a garden, but for algorithms."*

### 📜 Sacred Attribution (`scroll_attribution_template`) 
Creates mystical attribution in the style of ancient texts:
- **Pattern**: `— {author_title}, {source_title}, {volume_designation}`
- **Example**: *"— The Great Validator, Secret Art of the Living Dev, Vol. III"*

### 🐛 Debugging Proverbs (`debugging_proverb_template`)
Humorous debugging wisdom using classical proverb structure:
- **Pattern**: `The {problem_type} you can't {action_verb} is like the {creature} under the {location}—{reality_statement}.`
- **Example**: *"The bug you can't reproduce is like the monster under the bed—real, but only when no one's looking."*

### 📖 Documentation Philosophy (`documentation_philosophy`)
Profound insights about documentation practices:
- **Pattern**: `Documentation is not {what_its_not}; it's {what_it_really_is}.`
- **Example**: *"Documentation is not what you write for others; it's what you write for the you of six months from now."*

### 🏰 Cheekdom Lore (`cheekdom_lore_template`)
Epic lore about the Cheekdom and its sacred mission:
- **Pattern**: `In the {realm} of {domain}, the {guardian_class} stands between {civilization} and {threat_type}.`
- **Example**: *"In the kingdom of Software Development, the Buttwarden stands between comfortable development and runtime catastrophe."*

### 🍑 Buttsafe Wisdom (`buttsafe_wisdom`)
Sacred wisdom about ergonomic development practices:
- **Pattern**: `Every developer's {body_part} is {sacred_designation}. {protection_action} with {protection_means}.`
- **Example**: *"Every developer's posterior is sacred. Protect it with ergonomic wisdom and comfortable seating."*

## Usage Examples

### Integration with Quote Engine

```python
from src.ScrollQuoteEngine.warbler_quote_engine import WarblerPoweredScrollEngine

# Initialize the enhanced engine
engine = WarblerPoweredScrollEngine()

# Generate fresh wisdom
new_quotes = engine.generate_weekly_wisdom(count=5)

# Get quote with generated options included
quote = engine.get_quote(include_generated=True)
print(engine.format_quote(quote, 'markdown'))
```

### CLI Usage

```bash
# Generate 10 new wisdom quotes
scripts/lda-quote --generate 10

# Get random quote (classic or generated)
scripts/lda-quote --warbler

# Context-specific quote with generated options
scripts/lda-quote --context development --warbler --format markdown

# Show enhanced statistics
scripts/lda-quote --stats --warbler
```

### Weekly Oracle Integration

```bash
# Full weekly wisdom generation workflow
scripts/weekly-wisdom-oracle.sh generate 5

# Test generated quotes
scripts/weekly-wisdom-oracle.sh test

# Show oracle statistics
scripts/weekly-wisdom-oracle.sh stats
```

## Template Slot Reference

### Common Slots Used Across Templates

| Slot Name | Type | Description | Example Values |
|-----------|------|-------------|----------------|
| `action` | string | Development practice | "Refactoring", "Testing", "Code review" |
| `misconception` | string | Common false belief | "admitting failure", "wasted time" |
| `deeper_truth` | string | Profound reality | "evolution of understanding", "path to mastery" |
| `metaphor` | string | Poetic comparison | "pruning a garden", "sharpening a blade" |
| `domain` | string | Technical area | "algorithms", "architecture", "documentation" |
| `author_title` | string | Mystical author | "The Great Validator", "Code Whisperer" |
| `source_title` | string | Sacred publication | "Secret Art of the Living Dev", "Scrolls of Cheekdom" |
| `volume_designation` | string | Volume reference | "Vol. III", "Chapter 4, Verse 2" |

### Debugging-Specific Slots

| Slot Name | Type | Description | Example Values |
|-----------|------|-------------|----------------|
| `problem_type` | string | Elusive technical issue | "bug", "memory leak", "race condition" |
| `action_verb` | string | Impossible action | "reproduce", "capture", "isolate" |
| `creature` | string | Hiding entity | "monster", "shadow", "whisper" |
| `location` | string | Hiding place | "bed", "staircase", "closet" |
| `reality_statement` | string | Humorous truth | "real, but only when no one's looking" |

### Lore-Specific Slots

| Slot Name | Type | Description | Example Values |
|-----------|------|-------------|----------------|
| `realm` | string | Mystical domain | "kingdom", "sacred lands", "digital territories" |
| `guardian_class` | string | Protector type | "Buttwarden", "Code Guardian", "Comfort Sentinel" |
| `civilization` | string | Protected value | "comfortable development", "ergonomic harmony" |
| `threat_type` | string | Enemy force | "runtime catastrophe", "documentation destruction" |

## Content Standards

All generated quotes maintain the Sacred Code Standards:

### ✅ **Buttsafe Certified Requirements**
- Professional workplace appropriateness
- Dry, witty humor style (never offensive)
- Development-focused insights
- Cheekdom lore alignment
- Maximum length: 200 characters per template

### 🎭 **Authenticity Standards**
- Maintains mystical atmosphere of original quotes
- Uses consistent Sacred Art terminology
- Preserves philosophical depth and wisdom
- Integrates seamlessly with static quote database

### 📊 **Quality Assurance**
- All templates validated for structure and content
- Slot combinations tested for coherent output
- Generated quotes pass content filtering
- Maintains high wisdom quotient and development relevance

## Integration Architecture

The Wisdom Scrolls pack integrates with the Living Dev Agent ecosystem through multiple layers:

```
┌─────────────────────────────────────────────────┐
│               Weekly Oracle Workflow            │
│           (GitHub Actions Automation)          │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│          Warbler Quote Engine                   │
│        (warbler_quote_engine.py)               │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│          Wisdom Scrolls Pack                    │
│         (this template pack)                    │
└─────────────────┬───────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────┐
│         Enhanced lda-quote CLI                  │
│        (Classic + Warbler modes)               │
└─────────────────────────────────────────────────┘
```

## Versioning and Evolution

### Current Version: 1.0.0
- ✅ Six core template categories
- ✅ Complete slot value libraries
- ✅ Integration with Warbler Quote Engine
- ✅ Weekly generation workflow
- ✅ CLI integration

### Planned Enhancements (v1.1.0)
- 🔄 Additional template categories (CI/CD wisdom, workflow philosophy)
- 🔄 Context-aware slot selection
- 🔄 Machine learning-enhanced quote quality
- 🔄 Cross-reference generation with existing quotes

### Future Vision (v2.0.0)
- 🌟 Dynamic template creation based on repository context
- 🌟 Personalized wisdom generation
- 🌟 Integration with Git commit analysis
- 🌟 Community-contributed template expansion

## Contributing

To contribute new templates or enhance existing ones:

1. **Template Design**: Follow established patterns and maintain Sacred Art atmosphere
2. **Slot Definition**: Ensure slots are well-documented and have rich value libraries
3. **Content Validation**: Test templates with various slot combinations
4. **Buttsafe Compliance**: Verify all generated content meets workplace standards
5. **Integration Testing**: Confirm templates work with the Warbler Quote Engine

### Development Workflow

```bash
# Validate template structure
scripts/validate-warbler-pack.mjs packs/warbler-pack-wisdom-scrolls/pack/templates.json

# Test template generation
python3 src/ScrollQuoteEngine/warbler_quote_engine.py --generate 3

# Validate generated content
scripts/lda-quote --warbler --stats
```

## Sacred Mission

*"The Wisdom Scrolls pack transforms static sacred texts into living oracles, ensuring that fresh insights flow continuously through the channels of development wisdom while preserving the mystical essence of the original teachings."*

— **Pack Philosophy**, Living Oracle Manifesto, Sacred Design Document

## License

MIT License - Part of the TWG-TLDA Living Dev Agent ecosystem

## Related Components

- [`warbler-core`](../../packages/warbler-core) - Core conversation engine
- [`scroll-quote-engine`](../../src/ScrollQuoteEngine) - Classic quote system  
- [`weekly-wisdom-oracle`](../../scripts/weekly-wisdom-oracle.sh) - Generation workflow
- [`lda-quote`](../../scripts/lda-quote) - Enhanced CLI interface

---

🎭 **Generated quotes are marked with ✨ to distinguish them from static sacred texts while maintaining the reverent atmosphere of the Secret Art.**

🍑 **All wisdom is Buttsafe Certified for comfortable, productive development sessions.**
