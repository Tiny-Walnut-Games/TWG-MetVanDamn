# 📜 Scroll Quote Engine Documentation

The Scroll Quote Engine provides ambient inspiration through randomized quotes from the *Secret Art of the Living Dev*, ensuring buttsafe compliance and TLDL alignment across the entire repository ecosystem.

## 🛡️ Badges Earned

[![Buttsafe Certified](https://img.shields.io/badge/Buttsafe-Certified-gold?style=for-the-badge&logo=shield&logoColor=white)](#content-filtering)
[![Content Filter Sentinel](https://img.shields.io/badge/Content_Filter-Sentinel-blue?style=for-the-badge&logo=eye&logoColor=white)](#content-filtering)
[![Scrollsmith Approved](https://img.shields.io/badge/Scrollsmith-Approved-green?style=for-the-badge&logo=scroll&logoColor=white)](#quote-database)
[![TLDL Whisperer](https://img.shields.io/badge/TLDL-Whisperer-purple?style=for-the-badge&logo=magic&logoColor=white)](#tldl-integration)

## 🎯 Features

### Core Functionality
- **📚 Rich Quote Database**: 46+ quotes across 9 categories from the Secret Art
- **🔄 Context-Aware Selection**: Appropriate quotes for different contexts (README, TLDL, CI, etc.)
- **🛡️ Content Filtering**: Buttsafe certified quotes with professional appropriateness
- **🎨 Multiple Formats**: CLI, Markdown, and plain text output formats
- **📊 Validation & Stats**: Database validation and comprehensive statistics

### Integration Points
- **🚀 Init Script Integration**: Ambient quotes during agent context initialization
- **📝 TLDL Templates**: Quote placeholders in Living Dev Log entries
- **🐛 Issue Templates**: Inspirational quotes in GitHub issue templates
- **📖 README Generation**: Dynamic quote injection for documentation headers
- **✅ Validation Pipeline**: Quote database validation in existing linters

## 🚀 Quick Start

### Basic Usage
```bash
# Get a random quote
scripts/lda-quote

# Get context-specific quote
scripts/lda-quote --context readme --format markdown

# Get category-specific quote
scripts/lda-quote --category debugging

# Show database statistics
scripts/lda-quote --stats
```

### Integration Examples
```bash
# Use in TLDL entries
python3 src/ScrollQuoteEngine/quote_engine.py --context documentation --format markdown

# Use in README headers
scripts/inject-readme-quote.py --readme README.md

# Validate quote database
python3 src/ScrollQuoteEngine/quote_engine.py --validate
```

## 📊 Quote Categories

The quote database contains the following categories:

| Category | Count | Purpose |
|----------|--------|---------|
| **General** | 6 quotes | Universal wisdom and inspiration |
| **Development** | 5 quotes | Software development practices |
| **Debugging** | 5 quotes | Troubleshooting and problem-solving |
| **Documentation** | 5 quotes | Writing and maintaining docs |
| **Commits** | 5 quotes | Version control best practices |
| **CI/CD** | 5 quotes | Continuous integration wisdom |
| **Buttsafe** | 5 quotes | Ergonomic and comfort practices |
| **Workflow** | 5 quotes | Process and methodology guidance |
| **Lore** | 5 quotes | Cheekdom mythology and backstory |

## 🛠️ CLI Reference

### `scripts/lda-quote` - Main CLI Interface

```bash
Usage: scripts/lda-quote [options]

Options:
  --context <context>     Get quote for specific context
                         (readme, tldl, ci, debug, development, workflow, etc.)
  --category <category>   Get quote from specific category
                         (general, development, debugging, documentation, 
                          commits, ci_cd, buttsafe, workflow, lore)
  --format <format>       Output format: cli, markdown, plain (default: cli)
  --buttsafe             Only show buttsafe certified quotes
  --stats                Show database statistics
  --validate             Validate quote database
  --help, -h             Show help
```

### `src/ScrollQuoteEngine/quote_engine.py` - Core Engine

```bash
Usage: python3 src/ScrollQuoteEngine/quote_engine.py [options]

Options:
  --category CATEGORY     Quote category to select from
  --context CONTEXT       Context hint for appropriate quote selection
  --tags TAG [TAG ...]    Filter by tags
  --format {cli,markdown,plain}  Output format (default: cli)
  --validate              Validate quotes database
  --stats                 Show database statistics
  --quotes-path PATH      Path to custom quotes database
```

### `src/ScrollQuoteEngine/content_filter.py` - Content Filtering

```bash
Usage: python3 src/ScrollQuoteEngine/content_filter.py [options]

Options:
  --text TEXT            Quote text to evaluate
  --author AUTHOR        Quote author
  --source SOURCE        Quote source
  --batch-file FILE      JSON file with quotes to evaluate
  --suggest              Provide improvement suggestions
  --report               Generate compliance report
```

## 🔧 Integration Guide

### Adding Quotes to Scripts

```bash
#!/usr/bin/env bash
# Add to any bash script for ambient inspiration

show_scroll_quote() {
    local context="$1"
    if command -v python3 &> /dev/null; then
        quote=$(python3 src/ScrollQuoteEngine/quote_engine.py --context "$context" --format cli 2>/dev/null)
        if [[ -n "$quote" ]]; then
            echo "📜 From the Secret Art of the Living Dev:"
            echo "$quote"
            echo ""
        fi
    fi
}

# Use it
show_scroll_quote "development"
```

### Adding Quotes to Python Scripts

```python
import sys
from pathlib import Path

# Add ScrollQuoteEngine to path
sys.path.insert(0, str(Path(__file__).parent / "src"))

try:
    from ScrollQuoteEngine import ScrollQuoteEngine
    
    # Initialize and get quote
    engine = ScrollQuoteEngine()
    quote = engine.get_quote_for_context('readme')
    print(quote.format_markdown())
    
except ImportError:
    print("ScrollQuoteEngine not available")
```

### TLDL Integration

The TLDL template now includes quote integration instructions:

```markdown
---

> 📜 *"[Insert inspirational quote from Secret Art of the Living Dev using: `python3 src/ScrollQuoteEngine/quote_engine.py --context documentation --format markdown`]*"

---
```

### GitHub Issue Templates

Issue templates now include contextual quotes:

```markdown
> 📜 *"The bug you can't reproduce is like the monster under the bed—real, but only when no one's looking."* — **Tales from the Debug Trenches, Vol. IV**

<!-- Rest of issue template -->

---
*Need debugging wisdom? `scripts/lda-quote --category debugging --format markdown`*
```

## 🛡️ Content Filtering

The Content Filter Sentinel ensures all quotes meet buttsafe standards:

### Approval Criteria
- **Professional Appropriateness**: SFW content suitable for workplace
- **Cheekdom Compliance**: Aligns with repository lore and values
- **Development Relevance**: Contains meaningful insights for developers
- **Quality Standards**: Well-written with appropriate length and depth

### Automatic Filtering
- Checks for problematic patterns and content
- Validates professional terminology usage
- Ensures appropriate humor style (dry, witty, non-offensive)
- Scores quotes on relevance and quality metrics

### Buttsafe Certification
Quotes achieving 60%+ quality score receive **🍑 Buttsafe Certified** status, ensuring:
- Safe for all workplace environments
- Aligned with cheekdom values and lore
- Professionally appropriate humor and wisdom
- Development-focused insights and guidance

## 📈 Validation & Quality Assurance

### Database Validation

```bash
# Validate quote database
python3 src/ScrollQuoteEngine/quote_engine.py --validate

# Include in TLDL validation
python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/

# Skip quote validation if needed  
python3 src/SymbolicLinter/validate_docs.py --skip-quotes
```

### Quality Metrics
- **Total Quotes**: 46 across 9 categories
- **Buttsafe Certified**: 100% (46/46)
- **Average Quality Score**: High
- **Content Compliance**: Full alignment with Cheekdom standards

## 🎨 Customization

### Adding Custom Quotes

1. Edit `data/secret_art_quotes.yaml`
2. Add quotes to appropriate categories
3. Ensure buttsafe compliance
4. Run validation: `python3 src/ScrollQuoteEngine/quote_engine.py --validate`

### Quote Format
```yaml
quotes:
  category_name:
    - text: "Your inspirational quote text here"
      author: "Quote Author"
      source: "Source Publication"
      volume: "Vol. X"
      tags: ["tag1", "tag2", "tag3"]
      buttsafe_certified: true
```

### Custom Categories

Add new categories by extending the `QuoteCategory` enum in `src/ScrollQuoteEngine/quote_engine.py`:

```python
class QuoteCategory(Enum):
    # ... existing categories ...
    CUSTOM_CATEGORY = "custom_category"
```

## 🚨 Troubleshooting

### Common Issues

**Quote Engine Not Found**
```bash
# Ensure Python path is correct
export PYTHONPATH="${PYTHONPATH}:./src"
python3 -c "from ScrollQuoteEngine import ScrollQuoteEngine; print('Success!')"
```

**Database Loading Errors**
```bash
# Validate YAML syntax
python3 -c "import yaml; yaml.safe_load(open('data/secret_art_quotes.yaml'))"

# Check file permissions
ls -la data/secret_art_quotes.yaml
```

**Import Errors**
```bash
# Install required dependencies
pip install PyYAML argparse

# Check Python version (3.7+ required)
python3 --version
```

### Emergency Fallback

If the quote database fails to load, the engine provides emergency quotes to ensure functionality continues:

```
"When the quote engine fails, improvisation becomes the highest art."
— Emergency Protocols, Disaster Recovery Scrolls, Vol. 404
```

## 🎉 Examples in Action

### Repository Usage

The scroll quote engine is integrated throughout the Living Dev Agent ecosystem:

1. **Initialization**: Quotes appear during `scripts/init_agent_context.sh`
2. **Documentation**: TLDL entries include quote integration instructions
3. **Issues**: GitHub issue templates feature contextual inspiration
4. **Validation**: Quote database validation runs with existing linters
5. **CLI Access**: Easy quote access through `scripts/lda-quote`

### Sample Output

```
📜 From the Secret Art of the Living Dev:
🪶 "A well-formed header is the first line of defense against the Merge Goblins."
   — Scrolls of the Cheekdom, Vol. II
```

```markdown
> *"When in doubt, validate. When validated, doubt the validation."* — **Secret Art of the Living Dev, Vol. III**
```

## 🤝 Contributing

### Adding New Quotes
1. Follow the existing format in `data/secret_art_quotes.yaml`
2. Ensure buttsafe compliance and professional appropriateness
3. Test with content filter: `python3 src/ScrollQuoteEngine/content_filter.py --text "your quote"`
4. Validate database: `python3 src/ScrollQuoteEngine/quote_engine.py --validate`

### Extending Functionality
- Add new quote categories as needed
- Enhance content filtering rules
- Improve context-aware selection logic
- Add new output formats or integration points

---

> 📜 *"Documentation is not what you write for others; it's what you write for the you of six months from now."* — **Documents of Remembrance, Vol. I**

**🍑 Buttsafe Certified**: This documentation meets the highest standards of clarity, usefulness, and cheek-preservation protocols.