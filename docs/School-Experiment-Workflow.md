# School Experiment Workflow - Complete Guide

## Overview

The School Experiment Workflow is a comprehensive system for hypothesis-driven development and experimentation within Unity projects. It provides a structured approach to identifying improvement opportunities, generating testable hypotheses, and executing experiments to validate development decisions.

## Workflow Stages

### Stage 0: Inventory Collection
**Purpose**: Discover and catalog all relevant assets and components in the Unity project.

**Tool**: `InventoryCollector.cs`
**Output**: `assets/experiments/school/inventory.json`

```
Tools → School → Collect Inventory
```

### Stage 1: Hypothesis Extraction  
**Purpose**: Analyze inventory data and generate testable hypotheses about potential improvements.

**Tool**: `HypothesisExtractor.cs`
**Output**: `assets/experiments/school/hypothesis_drafts.json`

```
Tools → School → Extract Hypotheses
```

### Stage 2: Experiment Manifest Generation
**Purpose**: Convert hypotheses into structured experiment configurations.

**Tool**: `ExperimentManifestGenerator.cs`
**Output**: `assets/experiments/school/manifests/experiment_*.yaml`

```
Tools → School → Generate Experiment Manifests
```

### Stage 3: Deterministic Experiment Execution
**Purpose**: Execute experiments using manifests, capture results and artifacts.

**Tool**: `ExperimentRunner.cs`
**Output**: `assets/experiments/school/outputs/runs/`

```
Tools → School → Run Experiments
```

### Stage 4: Validation & Promotion
**Purpose**: Validate experiment results, compute confidence scores, check baseline deltas, and promote validated claims.

**Tool**: `ResultValidator.cs`
**Output**: `assets/experiments/school/claims/`

```
Tools → School → Validate Results
```

### Stage 5: Report Synthesis & Actionable Intel ⭐ **NEW**
**Purpose**: Transform validated claims into comprehensive, actionable intelligence reports for developers and designers.

**Tool**: `ReportSynthesizer.cs`
**Output**: `assets/experiments/school/reports/`

```
Tools → School → Build Report
```

## Example Validation Results

Using the provided test data, Stage 4 validation produces:

```
🧙‍♂️ Found 2 experiment runs to validate
✅ Run run_0001: confidence=0.65, type=hypothesis  
✅ Run run_0002: confidence=0.65, type=hypothesis
📁 Claims promoted to assets/experiments/school/claims/hypotheses/
🎯 Average confidence: 0.65, baseline delta: +2.5%
```

## Quick Start

### Prerequisites
- Unity 6000.2.0f1+
- Python 3.11+ with `python3` command available
- Git repository initialized in project root
- Git user configured (`git config user.name` and `git config user.email`)

### Complete Workflow Execution

1. **Start with Stage 0**:
   ```
   Tools → School → Collect Inventory
   ```
   - Select Unity project directory
   - Include subdirectories for comprehensive scanning
   - Wait for inventory collection to complete
   - Verify `inventory.json` is generated

2. **Proceed to Stage 1**:
   ```
   Tools → School → Extract Hypotheses  
   ```
   - Load the generated inventory file
   - Configure hypothesis generation parameters
   - Generate hypothesis drafts
   - Review extracted hypotheses for quality

3. **Continue with Stage 2**:
   ```
   Tools → School → Generate Experiment Manifests
   ```
   - Load hypothesis drafts from Stage 1
   - Generate YAML experiment manifests
   - Verify manifests are created in `manifests/` directory

4. **Execute with Stage 3**:
   ```
   Tools → School → Run Experiments
   ```
   - Refresh and verify available manifests
   - Execute all experiments
   - Monitor progress and review results
   - Inspect output artifacts and GitHub integration data

5. **Validate with Stage 4**:
   ```
   Tools → School → Validate Results
   ```
   - Refresh and verify available experiment runs
   - Validate all results and compute confidence scores
   - Check baseline deltas and promote validated claims
   - Review promoted claims in `claims/` directories

6. **Synthesize with Stage 5** ⭐ **NEW**:
   ```
   Tools → School → Build Report
   ```
   - Refresh and verify available validated claims
   - Generate comprehensive markdown reports
   - Review actionable intelligence and recommendations
   - Optional: Push reports to GitHub issues/PRs

## Directory Structure

```
assets/experiments/school/
├── inventory.json                    # Stage 0 output
├── hypothesis_drafts.json            # Stage 1 output
├── manifests/                        # Stage 2 output
│   ├── experiment_*.yaml             # Individual experiment configs
│   └── ...
├── outputs/                          # Stage 3 output
│   └── runs/
│       ├── run_XXXX_YYYYMMDD_HHMMSS/ # Individual run results
│       │   ├── unity_manifest.yaml
│       │   ├── harness_manifest.yaml
│       │   ├── run_metadata.json
│       │   ├── execution_logs.txt
│       │   └── error.txt (if failed)
│       └── github_integration.json   # CI/CD integration metadata
└── claims/                           # Stage 4 output
    ├── validated/                    # High-confidence claims
    ├── hypotheses/                   # Medium-confidence hypotheses
    ├── regressions/                  # Low-confidence regressions
    └── github_integration.json      # Validation integration metadata
└── reports/                          # Stage 5 output ⭐ NEW
    ├── school_experiment_report_*.md # Generated markdown reports
    └── report_metadata.json         # Report generation metadata
```

## Integration Features

### Unity 6 Compatibility
- Built specifically for Unity 6 (6000.2.0f1+)
- Uses latest Unity Editor APIs
- Async/await patterns for responsive UI
- Modern C# language features

### Python Experiment Framework
- Seamless integration with `engine/experiment_harness.py`
- Automatic manifest format conversion
- Deterministic execution with proper seeding
- Comprehensive result validation

### GitHub Integration
- Automatic Git metadata capture
- Artifact path tracking for CI/CD systems
- Integration metadata in JSON format
- Commit and branch information preservation

### Developer Experience
- Intuitive Unity Editor workflow
- Progress tracking and status updates
- Detailed logging and error reporting
- File browser integration for result inspection

## Advanced Usage

### Custom Experiment Configuration

Modify `ManifestConverter.cs` to customize experiment parameters:

```csharp
// Adjust corpus size limits
["size"] = Math.Min(unity.CorpusSize, 100), // Increase limit

// Modify timeout settings  
["timeout_seconds"] = 300, // Increase timeout

// Adjust validation criteria
["max_error_rate"] = 0.1, // Stricter validation
```

### CI/CD Integration

Use the GitHub integration metadata for automated processing:

```yaml
# GitHub Actions example
- name: Process School Experiments
  run: |
    if [ -f "assets/experiments/school/outputs/runs/github_integration.json" ]; then
      python scripts/analyze_experiments.py \
        --integration-data assets/experiments/school/outputs/runs/github_integration.json \
        --output reports/experiment_analysis.html
    fi
```

### Performance Tuning

For large projects, consider these optimizations:

1. **Stage 0 (Inventory)**:
   - Use selective directory scanning
   - Exclude non-essential file types
   - Implement file size limits

2. **Stage 1 (Hypotheses)**:
   - Configure confidence thresholds
   - Limit hypothesis generation count
   - Focus on specific asset types

3. **Stage 2 (Manifests)**:
   - Batch manifest generation
   - Implement parallel processing
   - Use deterministic seeding

4. **Stage 3 (Execution)**:
   - Limit concurrent experiments
   - Adjust corpus sizes for testing
   - Implement execution timeouts

## Troubleshooting

### Common Issues

**"Tool not found in Unity menu"**
- Verify Unity Editor is 6000.2.0f1 or later
- Check that C# files are compiled without errors
- Restart Unity Editor to refresh menus

**"Python harness not found"**
- Ensure Python 3.11+ is installed: `python3 --version`
- Verify experiment harness exists: `ls engine/experiment_harness.py`
- Install required packages: `pip install -r engine/requirements.txt`

**"Git information unavailable"**
- Initialize Git repository: `git init`
- Configure Git user: `git config user.name "Your Name"`
- Make initial commit: `git add . && git commit -m "Initial commit"`

**"Stage dependencies missing"**
- Complete stages in order: 0 → 1 → 2 → 3
- Verify output files exist before proceeding to next stage
- Check file permissions and disk space

### Debug Mode

Enable detailed logging for each stage:

```csharp
// Add to any stage tool
Debug.Log($"[StageName] Debug message: {details}");
```

Check Unity Console and individual run log files for detailed information.

## Best Practices

### Workflow Management
- Complete stages sequentially for best results
- Verify outputs before proceeding to next stage
- Keep backup copies of important results
- Document experiment parameters and outcomes

### Performance Optimization
- Start with small corpus sizes for testing
- Gradually increase complexity as system stabilizes
- Monitor system resources during execution
- Use deterministic seeds for reproducible results

### Quality Assurance
- Review generated hypotheses for relevance
- Validate experiment manifests before execution
- Check result quality and success criteria
- Maintain clean output directories

### Collaboration
- Share experiment results through version control
- Document significant findings in TLDL entries
- Use GitHub integration for team coordination
- Establish experiment naming conventions

## Future Roadmap

### Stage 5: Report Synthesis & Actionable Intel ⭐ **IMPLEMENTED**
- Automated intelligence report generation from validated claims
- Comprehensive markdown and HTML output formats
- GitHub integration for automated issue/PR creation
- Evidence-based recommendations with confidence scoring

### Enhanced Integration (Planned)
- Real-time experiment monitoring
- Performance profiling and optimization
- Custom experiment processors
- Advanced CI/CD workflow integration

### Advanced Features (Planned)
- Parallel experiment execution
- Distributed computing support
- Machine learning result analysis
- Automated hypothesis generation refinement

---

**🧙‍♂️ Bootstrap Sentinel's Wisdom**: *"The school experiment workflow transforms development chaos into documented learning adventures. Each stage builds upon the last, creating a foundation of knowledge that turns debugging sessions into legendary chronicles of growth and discovery."*