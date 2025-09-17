# Alchemist Faculty Scripts

This directory contains Python scripts for the Alchemist Faculty narrative → evidence distillation pipeline.

## Scripts

### `generate_manifest.py`
Main script for generating experiment manifests from Gu Pot GitHub issues.

```bash
# Generate manifest for specific issue
python generate_manifest.py --issue-number 123 --repo owner/repo --output gu_pot/issue-123/

# Generate from issue URL
python generate_manifest.py --issue-url https://github.com/owner/repo/issues/123

# Batch processing
python generate_manifest.py --batch --issues-file issues_list.txt --output-dir gu_pot/
```

### `linkback_automation.py`
Automates updating Gu Pot issues with validated claims and evidence links.

```bash
# Test what would happen (safe mode)
python linkback_automation.py --dry-run --issue-number 87 --repo owner/repo

# Update issue with validation results
python linkback_automation.py --issue-number 87 --repo owner/repo --github-token $GITHUB_TOKEN

# Custom claims directory
python linkback_automation.py --issue-number 123 --repo owner/repo --claims-dir path/to/claims/
```

**Features:**
- Detects newly validated claims from structured directories
- Updates GitHub issues with evidence links sections
- Applies appropriate labels based on stage decisions (serum/antitoxin/compost)
- Posts summary comments with validation results
- Comprehensive error handling and dry-run mode

### `report_synthesizer.py`
Synthesizes experiment reports from claims data and validation results.

```bash
# Generate report for specific experiment
python report_synthesizer.py --experiment-dir gu_pot/issue-123/

# Generate report with custom output file
python report_synthesizer.py --experiment-dir gu_pot/issue-123/ --output detailed_report.md

# Validate experiment structure only
python report_synthesizer.py --validate-only --experiment-dir gu_pot/issue-123/
```

**Features:**
- Analyzes claims data and generates comprehensive markdown reports
- Validates experiment directory structure and data integrity
- Provides statistical analysis and recommendations
- Supports batch processing of multiple experiments

### `claims_classifier.py`
Classifies experiment claims into categories (validated, regression, anomaly, etc.).

```bash
# Classify all claims in directory
python claims_classifier.py --claims-dir gu_pot/issue-123/claims/

# Single claim with baseline comparison
python claims_classifier.py --claim-file claim_001.json --baseline baseline.json

# Batch processing with reorganization
python claims_classifier.py --batch --input-dir gu_pot/ --output-dir classified/ --reorganize
```

**Features:**
- Advanced classification algorithms for regression vs anomaly differentiation
- Statistical analysis and confidence scoring
- Detailed reasoning and metadata generation
- Supports baseline comparison for improved accuracy

### `validate_baseline_set.py`
Validates baseline_set.json files against schema and quality requirements.

```bash
# Validate single baseline file
python validate_baseline_set.py --file gu_pot/issue-123/baseline_set.json

# Validate all baseline files in directory
python validate_baseline_set.py --directory gu_pot/issue-123/ --pattern "baseline*.json"

# Recursive validation with strict mode
python validate_baseline_set.py --directory gu_pot/ --recursive --strict
```

**Features:**
- Schema validation against baseline_set.json schema
- Quality checks for metric completeness and reasonableness
- Performance and behavioral metric validation
- Comprehensive error reporting and warnings

### `test_linkback_automation.py`
Test suite for linkback automation system with mock data support.

```bash
# Run all unit tests
python test_linkback_automation.py

# Create test data for manual testing
python test_linkback_automation.py --create-test-data

# Run manual integration test
python test_linkback_automation.py --manual-test
```

**Features:**
- Comprehensive unit test coverage
- Mock data generation for testing
- Integration test scenarios
- Automated test data creation

### `validate_setup.py`
Setup validation script that checks dependencies and project structure.

```bash
# Check if Alchemist Faculty is ready to use
python validate_setup.py
```

### `test_linkback_automation.py`
Comprehensive test suite for the linkback automation system.

```bash
# Run all tests
python test_linkback_automation.py
```

## Dependencies

Install required packages:
```bash
pip install PyYAML requests argparse
```

Or install from project requirements:
```bash
pip install -r ../../requirements.txt
```

## Usage

1. **First-time setup**: Run `python validate_setup.py`
2. **Generate manifests**: Use `generate_manifest.py` with appropriate flags or trigger via GitHub Action
3. **Run experiments**: Execute validation pipeline to generate claims
4. **Classify claims**: Use `claims_classifier.py` to categorize experimental results
5. **Validate baselines**: Use `validate_baseline_set.py` to ensure baseline quality
6. **Update issues**: Use `linkback_automation.py` to update GitHub issues with results
7. **Generate reports**: Use `report_synthesizer.py` for comprehensive analysis
8. **Integration**: Scripts integrate with Unity tools and GitHub automation

### Automated Workflow

The Alchemist Faculty now includes automated GitHub Actions:

- **Manifest Creation**: Automatically generates manifests when issues reach "gu-pot:distilled" stage
- **Determinism Testing**: CI pipeline validates reproducibility of all automation components
- **Issue Updates**: Linkback automation updates issues with validation results

## Configuration

Scripts support:
- GitHub token authentication via `--github-token` or environment variable
- Multiple output formats (YAML, JSON)
- Dry-run mode for testing
- Verbose logging for debugging
- Batch processing for multiple experiments
- Schema validation and quality checks

## Integration Points

- **Unity Editor**: C# tools can call Python scripts or vice versa
- **GitHub API**: Automatic issue data fetching with authentication
- **TWG-TLDA Ecosystem**: Compatible with Chronicle Keeper, Pet Events, etc.
- **CI/CD**: Report synthesizer designed for automated pipeline integration

## Testing

Run the test suite for the report synthesizer:
```bash
python ../../tests/test_alchemist_report_synthesizer.py
```

See `docs/faculty/alchemist/quickstart-guide.md` for complete usage instructions.