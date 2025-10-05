# Monthly TLDL Archive Ritual Playbook

> *"When the ledger grows thick, the archivist binds it into tomes."* — **The Chronicle Keeper's Codex**

## 🎯 Purpose

The Monthly TLDL Archive Ritual is the systematic consolidation of all daily TLDL entries into structured monthly reports that preserve development lore while maintaining navigational clarity.

---

## 📋 Ritual Overview

### Timing
- **Automated**: First week of each month (1st day at 02:00 UTC)
- **Manual**: On-demand via GitHub Actions or CLI

### Scope
- All TLDL entries from the target month
- Primary source: `TLDL/entries/` (canonical location post-cutover)
- Legacy cleanup: Any remaining in `docs/` (should be minimal)
- Associated actionables, themes, and cross-references

### Outcomes
- Single consolidated `docs/TLDL-Monthly/YYYY-MM.md` file
- Updated GitBook SUMMARY.md with archive links
- Optional archival of source TLDL files to `docs/TLDL-Archive/`

---

## 🤖 Automated Execution

### GitHub Actions Workflow
The ritual runs automatically via `.github/workflows/tldl-monthly-archive.yml`:

```yaml
# Scheduled execution (1st of each month)
on:
  schedule:
    - cron: '0 2 1 * *'
```

### Manual Triggering
1. Go to **Actions** tab in GitHub repository
2. Select **"TLDL Monthly Archive Generation"** workflow
3. Click **"Run workflow"**
4. Optionally specify:
   - Target month (YYYY-MM format)
   - Whether to archive old files

---

## 🛠️ Manual Execution

### Using Shell Script
```bash
# Generate for previous month (auto-detection)
scripts/tldl-monthly-generator.sh --auto

# Generate for specific month
scripts/tldl-monthly-generator.sh 2025-08

# Full archival process
scripts/tldl-monthly-generator.sh --auto --archive-old --update-index

# Dry run (preview changes)
scripts/tldl-monthly-generator.sh --dry-run 2025-08
```

### Using Python Script Directly
```bash
# Generate monthly report
python3 scripts/tldl_monthly_generator.py --month 2025-08

# Auto-detect previous month
python3 scripts/tldl_monthly_generator.py --auto
```

---

## 📊 Archive Structure

### Generated Monthly Archive Format

```markdown
# TLDL Monthly Archive - [Month Year]

## 📊 Archive Summary
- **Archival Date**: YYYY-MM-DD
- **Total TLDL Entries**: [count]
- **Chronicle Period**: [Month Year]
- **Archive Status**: Complete

## 📜 Chronological Entry Summary
[Ordered list of all TLDL entries with summaries]

## ✅ Consolidated Actionables
### Completed Actions
[All completed checkbox/TODO items with source references]

### Pending Actions  
[All pending checkbox/TODO items with source references]

## 🎯 Key Decisions & Development Arcs
[Thematic analysis of major patterns and decisions]

## 👻 Lost Features Integration
[Cross-reference with Ghost Ledger system]

## 🔗 Cross-Links & References
[Links to related documentation and source TLDL entries]
```

### Navigation Integration
- **GitBook SUMMARY.md**: Archives listed under "TLDL Archives" section
- **docs/README.md**: Link to latest monthly archives
- **Relative paths**: All links use repository-relative paths

---

## 🔍 Quality Assurance

### Validation Checks
The ritual includes automatic validation:

1. **Content Validation**:
   - Proper archive title format
   - Required sections present
   - Entry count accuracy

2. **Link Validation**:
   - Source TLDL file references
   - Cross-documentation links
   - GitBook SUMMARY integration

3. **Structure Validation**:
   - YAML frontmatter parsing
   - Section extraction accuracy
   - Actionable item detection

### Manual Review Process
1. **Archive Quality**: Review generated monthly archive for completeness
2. **Theme Analysis**: Validate thematic categorization accuracy
3. **Actionables**: Verify actionable item status and attribution
4. **Cross-links**: Test navigation paths to source materials

---

## 🧹 Archival Management

### Source File Lifecycle
1. **Active Phase**: TLDL entries in `docs/` and `TLDL/entries/`
2. **Monthly Consolidation**: Content merged into monthly archive
3. **Archival Phase**: Source files moved to `docs/TLDL-Archive/`
4. **Retention**: Archived files preserved for historical reference

### Archive Retention Policy
- **Monthly Archives**: Permanent retention in `docs/TLDL-Monthly/`
- **Source Archives**: Permanent retention in `docs/TLDL-Archive/`
- **GitBook Integration**: Monthly archives remain in navigation indefinitely

---

## 🎛️ Configuration Options

### Script Parameters
- `--auto`: Use previous month automatically
- `--archive-old`: Move source TLDL files to archive after consolidation
- `--update-index`: Update documentation indices and SUMMARY.md
- `--dry-run`: Preview changes without making modifications

### GitHub Actions Inputs
- `target_month`: Specific month to process (YYYY-MM)
- `archive_old_files`: Boolean flag for source file archival

### Environment Variables
- `REPO_ROOT`: Repository root directory (auto-detected)
- `ARCHIVE_DIR`: Archive destination directory
- `MONTHLY_DIR`: Monthly archives directory

---

## 🚨 Troubleshooting

### Common Issues

#### Archive Generation Fails
```bash
# Check TLDL file locations
find . -name "TLDL-*.md" -type f

# Validate Python dependencies  
python3 -c "import yaml; print('PyYAML OK')"

# Run with debug output
scripts/tldl-monthly-generator.sh --dry-run [month]
```

#### Missing Dependencies
```bash
# Install required packages
pip install -r scripts/requirements.txt

# Verify installation
python3 scripts/tldl_monthly_generator.py --help
```

#### GitBook SUMMARY Issues
```bash
# Validate SUMMARY.md syntax
python3 -c "
import re
with open('docs/SUMMARY.md') as f:
    content = f.read()
    if 'TLDL-Monthly' in content:
        print('✅ SUMMARY.md contains monthly archives')
    else:
        print('⚠️ SUMMARY.md missing monthly archive links')
"
```

### Recovery Procedures

#### Regenerate Missing Archive
```bash
# Force regeneration of existing archive
rm docs/TLDL-Monthly/2025-08.md
scripts/tldl-monthly-generator.sh 2025-08
```

#### Restore Archived Files
```bash
# Move files back from archive
mv docs/TLDL-Archive/TLDL-2025-08-* docs/
```

#### Fix GitBook Navigation
```bash
# Manually update SUMMARY.md
echo "- [2025-08](TLDL-Monthly/2025-08.md)" >> docs/SUMMARY.md
```

---

## 🎓 Best Practices

### Pre-Ritual Checklist
- [ ] Validate all TLDL entries for the month are present
- [ ] Run TLDL validation: `python3 src/SymbolicLinter/validate_docs.py --tldl-path docs/`
- [ ] Update Ghost Ledger if needed
- [ ] Review any pending actionables across entries

### Post-Ritual Validation
- [ ] Review generated monthly archive for completeness
- [ ] Verify GitBook SUMMARY.md updated correctly
- [ ] Test navigation paths (≤3 clicks to archives)
- [ ] Validate cross-links to source materials

### Maintenance Schedule
- **Weekly**: Monitor TLDL entry creation and quality
- **Monthly**: Execute archival ritual and review quality
- **Quarterly**: Review archive structure and navigation efficiency
- **Annually**: Assess archival policies and retention strategies

---

## 📈 Success Metrics

### Archive Quality Indicators
- **Completeness**: All TLDL entries from target month included
- **Traceability**: All actionables linked to source entries
- **Navigation**: Archives accessible in ≤3 clicks from documentation index
- **Freshness**: Source entries archived within one week of monthly report

### Process Efficiency Metrics
- **Automation Rate**: Percentage of archives generated automatically
- **Manual Intervention**: Frequency of manual fixes required
- **Validation Pass Rate**: Percentage of archives passing quality checks
- **Documentation Coverage**: Percentage of development activities captured

---

## 🔮 Future Enhancements

### Planned Improvements
- **AI-Enhanced Theme Analysis**: Deeper pattern recognition across entries
- **Cross-Project Integration**: Links to related development activities
- **Interactive Archive Views**: Dynamic filtering and search capabilities
- **Metrics Dashboard**: Visual representation of archive statistics

### Integration Opportunities
- **Chronicle Keeper**: Enhanced automation with issue/PR triggers
- **Daily Ledger**: Improved cross-reference with daily activities  
- **Ghost Ledger**: Automated lost feature status updates
- **Capsule Scrolls**: Integration with context preservation system

---

*"The ritual of archival transforms the chaos of daily development into the wisdom of structured knowledge. Each monthly tome becomes a beacon for future adventurers navigating similar challenges."* 📚✨

---

**Maintained by**: Archive Wall Continuity System  
**Last Updated**: 2025-08-19  
**Version**: 1.0.0  
**🍑 Buttsafe Certified**: Yes