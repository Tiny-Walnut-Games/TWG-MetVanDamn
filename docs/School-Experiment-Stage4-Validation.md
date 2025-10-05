# School Experiment - Stage 4: Result Validation & Promotion

## Overview

Stage 4 of the School Experiment Workflow validates experiment results from Stage 3, computes confidence scores, checks for baseline deltas, and promotes validated claims. This tool provides automated quality assessment and promotion pipeline for experiment outcomes.

## Purpose

- **Validate experiment results**: Analyze run outputs for success patterns and consistency
- **Compute confidence scores**: Generate statistical confidence metrics for each experiment run
- **Check baseline deltas**: Compare current results against historical baselines
- **Promote validated claims**: Categorize and store validated outcomes in structured directories

## Features

### Core Validation Pipeline

- **Automated result analysis**: Processes all experiment runs from Stage 3 outputs
- **Multi-factor confidence scoring**: Combines success rate, consistency, and metadata completeness
- **Baseline comparison**: Computes deltas against captured baseline metrics
- **Three-tier promotion system**: Validates, hypotheses, and regressions categories

### Unity 6 Integration

- **Modern Editor UI**: Built with Unity 6 EditorWindow APIs
- **Async validation processing**: Non-blocking UI with progress tracking
- **Real-time status updates**: Live progress indicators during validation
- **Integrated file browser**: Direct access to claims directories

### GitHub Integration

- **Git metadata capture**: Automatically captures commit and branch information
- **CI/CD compatibility**: Generates integration metadata for automated pipelines
- **Artifact tracking**: Documents all generated claims for downstream processing
- **UnityWebRequest ready**: Prepared for GitHub API integration if needed

## Usage

### Access the Tool

```
Tools → School → Validate Results
```

### Prerequisites

1. **Completed Stage 3**: Must have experiment runs in `assets/experiments/school/outputs/runs/`
2. **Unity 6000.2.0f1+**: Required for Editor window compatibility
3. **Git repository**: Git commands must be available for metadata capture
4. **File system permissions**: Write access to `assets/experiments/school/claims/`

### Validation Workflow

1. **Launch the validator**:
   - Open Unity Editor
   - Navigate to `Tools → School → Validate Results`
   - The window will automatically scan for available runs

2. **Review available runs**:
   - Check the "Available Experiment Runs" section
   - Verify all expected runs are detected
   - Use "Refresh Runs" if needed

3. **Execute validation**:
   - Click "Validate All Results"
   - Monitor progress in real-time
   - Wait for completion dialog

4. **Review results**:
   - Check validation summary statistics
   - Use "Show Claims Directory" to browse outputs
   - Review GitHub integration data if needed

## Validation Example

### Real Test Results

Using the provided test data from Stage 3 runs, here's what a validation cycle produces:

**Input**: 2 experiment runs in `assets/experiments/school/outputs/runs/`
- `run_0001`: experiment_c3d4e5f6-g7h8-9012-3456-789012cdefgh (success: true)
- `run_0002`: experiment_e5f6g7h8-i9j0-1234-5678-901234efghij (success: true)

**Validation Results**:
- Both runs achieved 0.65 confidence score (hypothesis tier)
- Claims promoted to `assets/experiments/school/claims/hypotheses/`
- Average confidence: 0.65
- Baseline delta: 2.5% improvement

**Generated Claims Structure**:
```
assets/experiments/school/claims/
├── hypotheses/
│   ├── claim_run_0001_20250906_023829.json
│   └── claim_run_0002_20250906_023829.json
├── regressions/ (empty)
├── validated/ (empty)
└── github_integration.json
```

## Output Structure

### Claims Directory Structure

The validation process creates a structured claims hierarchy with enhanced classification:

```
assets/experiments/school/claims/
├── validated/                        # High-confidence claims (>= 75%)
│   ├── claim_run_0001_YYYYMMDD_HHMMSS.json
│   └── ...
├── hypotheses/                       # Medium-confidence hypotheses (50-74%)
│   ├── claim_run_0002_YYYYMMDD_HHMMSS.json
│   └── ...
├── regressions/                      # Low-confidence regressions (< 50%)
│   ├── claim_run_0003_YYYYMMDD_HHMMSS.json
│   └── ...
├── anomalies/                        # Anomalous patterns (expected/unexpected)
│   ├── claim_run_0004_YYYYMMDD_HHMMSS.json
│   └── ...
├── improvements/                     # Significant positive changes
│   ├── claim_run_0005_YYYYMMDD_HHMMSS.json
│   └── ...
├── new_phenomena/                    # Novel patterns and breakthroughs
│   ├── claim_run_0006_YYYYMMDD_HHMMSS.json
│   └── ...
└── github_integration.json          # GitHub integration metadata
```

### Enhanced Claim Data Format

Each claim file contains structured validation results with enhanced classification metadata:

```json
{
  "RunId": "run_0001",
  "ExperimentName": "Editor Tool Enhancement Potential",
  "HypothesisId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "ConfidenceScore": 0.87,
  "ClaimType": "improvement",
  "ValidationTime": "2025-09-06T02:20:00Z",
  "Success": true,
  "BaselineComparison": "2.5% improvement",
  "Classification": {
    "PrimaryType": "improvement",
    "SecondaryType": "performance",
    "AnomalyScore": 0.15,
    "ClassificationFlags": [
      "high_confidence",
      "successful_execution",
      "performance_optimization"
    ],
    "ClassificationReason": "Significant improvement detected (score: 0.95)",
    "TrendSignificance": 0.87,
    "BaselineDeviation": "2.5% positive improvement",
    "IsExpectedAnomaly": false,
    "PhenomenonType": ""
  }
}
```

### GitHub Integration Metadata

The `github_integration.json` file contains comprehensive CI/CD metadata:

```json
{
  "Timestamp": "2025-09-06T02:20:00Z",
  "UnityVersion": "6000.2.0f1",
  "ProjectPath": "/path/to/project/Assets",
  "GitCommit": "abc123def456...",
  "GitBranch": "main",
  "RunsValidated": 6,
  "ClaimsPromoted": 5,
  "AverageConfidence": 0.78,
  "BaselineDelta": 2.5,
  "OutputDirectory": "assets/experiments/school/claims",
  "ClaimsPaths": [
    "assets/experiments/school/claims/validated/claim_run_0001.json",
    "..."
  ],
  "Stage": "4",
  "Workflow": "school"
}
```

## Confidence Scoring Algorithm

### Multi-Factor Scoring

The confidence algorithm combines multiple factors:

1. **Base Success Score**: 
   - Successful runs: 0.8 base confidence
   - Failed runs: 0.2 base confidence

2. **Consistency Factor**:
   - Analyzes execution logs for error patterns
   - Fewer errors = higher confidence
   - Formula: `max(0.1, 1.0 - (errorCount * 0.1))`

3. **Metadata Completeness Factor**:
   - Missing hypothesis ID: -10% confidence
   - Missing git commit: -5% confidence
   - Missing git branch: -5% confidence

4. **Final Calculation**:
   ```
   confidence = baseScore * consistencyFactor * metadataFactor
   confidence = min(0.95, max(0.05, confidence))
   ```

### Promotion Thresholds

Claims are promoted based on confidence scores:

- **Validated** (>= 75%): High-confidence, actionable claims
- **Hypotheses** (50-74%): Medium-confidence, requires further validation
- **Regressions** (< 50%): Low-confidence, potential issues to investigate

## Baseline Comparison

### Baseline Metrics Integration

The validator integrates with the baseline metrics system:

- **Baseline file**: `data/baseline_metrics.json` (if available)
- **Delta calculation**: Compares current performance against baseline
- **Improvement tracking**: Positive deltas indicate performance improvements
- **Regression detection**: Negative deltas flag potential regressions

### Delta Interpretation

- **Positive delta**: Performance improvement over baseline
- **Negative delta**: Performance regression from baseline
- **Zero delta**: No significant change from baseline

## GitHub Integration Details

### Git Metadata Capture

The validator automatically captures:

- **Commit hash**: Current HEAD commit for reproducibility
- **Branch name**: Active branch for context
- **Repository state**: Working directory status

### UnityWebRequest Integration Pattern

For future GitHub API integration, the validator is prepared to use UnityWebRequest:

```csharp
// Example GitHub API integration pattern
public async Task<string> CallGitHubAPI(string endpoint, string data)
{
    using (var request = UnityWebRequest.Post(apiUrl, data))
    {
        request.SetRequestHeader("Authorization", "Bearer " + token);
        await request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            return request.downloadHandler.text;
        }
        return null;
    }
}
```

### CI/CD Pipeline Integration

The generated integration metadata is designed for:

- **Automated artifact collection**: CI systems can parse claims directories
- **Quality gates**: Confidence scores can trigger deployment decisions
- **Trend analysis**: Historical confidence tracking across builds
- **Regression detection**: Automated alerts for negative baseline deltas

## Troubleshooting

### Common Issues

**"No experiment runs found"**
- Verify Stage 3 has been completed
- Check that `assets/experiments/school/outputs/runs/` exists
- Ensure run directories contain `run_metadata.json` files

**"Validation failed with errors"**
- Check Unity Console for detailed error messages
- Verify file system permissions for claims directory
- Ensure Git commands are available in PATH

**"Baseline delta calculation failed"**
- Baseline comparison is optional - validator continues without it
- Check if `data/baseline_metrics.json` exists
- Run baseline capture script if needed

**"Git information unavailable"**
- Ensure Git repository is initialized
- Configure Git user: `git config user.name "Your Name"`
- Make sure Git is available in system PATH

### Debug Mode

Enable detailed logging by checking the validation logs section in the tool:

```csharp
// Enable debug logging in validator
Debug.Log($"[ResultValidator] Debug message: {details}");
```

## Integration with CI/CD

### Automated Validation

The validator is designed for CI/CD integration:

```bash
# Example CI script usage
Unity -batchmode -projectPath . -executeMethod TWG.TLDA.School.Editor.ResultValidator.ValidateResultsHeadless
```

### Quality Gates

Use confidence scores for deployment decisions:

```yaml
# Example GitHub Actions workflow
- name: Validate Experiment Results
  run: |
    # Run Unity validation
    # Parse confidence scores
    # Fail if average confidence < threshold
```

### Artifact Collection

CI systems can collect generated claims:

```bash
# Collect validation artifacts
tar -czf validation-results.tar.gz assets/experiments/school/claims/
```

## Performance Considerations

### Execution Time

- **Typical validation time**: 5-30 seconds for 10-50 runs
- **Scaling**: Linear with number of runs to validate
- **Memory usage**: Minimal - processes runs sequentially

### File System Impact

- **Disk usage**: ~1-10KB per claim file
- **I/O operations**: Moderate - reads run metadata, writes claims
- **Directory structure**: Shallow hierarchy for fast access

## Future Enhancements

### Planned Features

- **Advanced statistical analysis**: More sophisticated confidence algorithms
- **Historical trend tracking**: Long-term confidence score trends
- **Automated baseline updates**: Dynamic baseline adjustment
- **Integration with external systems**: Direct GitHub issue creation
- **Custom validation rules**: User-defined promotion criteria

### Extension Points

- **Custom confidence algorithms**: Pluggable scoring systems
- **External baseline sources**: API-based baseline comparison
- **Notification systems**: Slack/Teams integration for validation results
- **Dashboard integration**: Real-time validation status displays

---

*Stage 4 completes the School Experiment Workflow by providing automated quality assessment and promotion of experimental findings, enabling data-driven development decisions with confidence scoring and baseline tracking.*