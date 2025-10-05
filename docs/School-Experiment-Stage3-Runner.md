# School Experiment - Stage 3: Deterministic Experiment Runner

## Overview

The **Experiment Runner** is a Unity Editor tool that executes experiment manifests generated in Stage 2, providing deterministic experiment execution with Unity 6 compatibility and GitHub integration for artifact tracking.

## Purpose

Stage 3 completes the school experiment workflow by executing the experiments defined in Stage 2 manifests:
- Loads experiment manifests from `assets/experiments/school/manifests/`
- Converts Unity manifests to Python experiment harness format
- Executes experiments using the TWG-TLDA experiment framework
- Captures results, metrics, logs, and outputs
- Stores raw artifacts in `assets/experiments/school/outputs/runs/`
- Generates GitHub integration metadata for CI/CD tracking

## Features

### Unity 6 Compatibility
- Built for Unity 6 (6000.2.0f1+)
- Uses latest Unity Editor APIs
- Async/await pattern for responsive UI
- Process execution with proper timeout handling

### GitHub Integration
- Generates comprehensive artifact tracking metadata
- Git commit and branch information capture
- Timestamp tracking for CI/CD integration
- Artifact path documentation for external systems

### Experiment Framework Integration
- Seamless integration with Python experiment harness
- Manifest format conversion from Unity to harness format
- Deterministic execution with proper seed handling
- Result validation and success criteria checking

### User Experience
- Progress tracking during execution
- Detailed logging with expandable log viewer
- Error handling with comprehensive error reporting
- File browser integration for result inspection

## Usage

### Prerequisites

1. **Completed Stage 1 and Stage 2**:
   - Stage 1: Hypothesis extraction completed
   - Stage 2: Experiment manifests generated in `assets/experiments/school/manifests/`

2. **Python Environment**:
   - Python 3.11+ available as `python3`
   - Required Python packages installed (`pip install -r engine/requirements.txt`)

3. **Git Repository**:
   - Git repository initialized in project root
   - Git user.name and user.email configured

### Running Stage 3

1. **Open Unity Editor** (Unity 6000.2.0f1+)

2. **Open Experiment Runner**:
   ```
   Tools → School → Run Experiments
   ```

3. **Verify Manifests**:
   - Click "Refresh Manifests" to load available experiments
   - Verify experiments are listed with their details
   - Check hypothesis types and confidence levels

4. **Execute Experiments**:
   - Click "Execute All Experiments" to start processing
   - Monitor progress bar and status messages
   - View detailed logs in the expandable log section

5. **Review Results**:
   - Open results directory to inspect output files
   - View GitHub integration data for CI/CD metadata
   - Check individual run directories for detailed artifacts

### Workflow Integration

```
Stage 0: Inventory Collection
    ↓ (inventory.json)
Stage 1: Hypothesis Extraction
    ↓ (hypothesis_drafts.json)
Stage 2: Experiment Manifest Generation
    ↓ (experiment_*.yaml manifests)
Stage 3: Deterministic Experiment Execution ← YOU ARE HERE
    ↓ (run results, metrics, artifacts)
Stage 4: Analysis & Reporting (Future)
```

## Output Structure

### Run Directory Structure

Each experiment execution creates a timestamped run directory:

```
assets/experiments/school/outputs/runs/
├── run_XXXX_YYYYMMDD_HHMMSS/
│   ├── unity_manifest.yaml          # Original Unity manifest
│   ├── harness_manifest.yaml        # Converted harness format
│   ├── run_metadata.json            # Execution metadata
│   ├── execution_logs.txt            # Python harness output
│   └── error.txt                     # Error details (if failed)
└── github_integration.json          # GitHub integration metadata
```

### GitHub Integration Metadata

The `github_integration.json` file contains:

```json
{
  "timestamp": "2025-09-06T02:20:00Z",
  "unity_version": "6000.2.0f1",
  "project_path": "/path/to/project/Assets",
  "git_commit": "abc123def456...",
  "git_branch": "main",
  "experiments_executed": 6,
  "output_directory": "assets/experiments/school/outputs/runs",
  "artifact_paths": [
    "assets/experiments/school/outputs/runs/run_0001/...",
    "..."
  ],
  "stage": "3",
  "workflow": "school"
}
```

### Run Metadata

Each run includes detailed metadata:

```json
{
  "run_id": "run_0001",
  "experiment_name": "Editor Tool Enhancement Potential",
  "hypothesis_id": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "hypothesis_type": "CapabilityAssertion",
  "manifest_file": "experiment_a1b2c3d4...yaml",
  "execution_time": "2025-09-06T02:20:00Z",
  "unity_version": "6000.2.0f1",
  "project_path": "/path/to/project/Assets",
  "git_commit": "abc123def456...",
  "git_branch": "main",
  "exit_code": 0,
  "success": true,
  "stage": "3",
  "workflow": "school"
}
```

## Manifest Conversion

The experiment runner automatically converts Unity-generated manifests to the format expected by the Python experiment harness:

### Unity Manifest Format (Input)
```yaml
metadata:
  name: Editor Tool Enhancement Potential
  hypothesis_type: CapabilityAssertion
  
corpus:
  size: 225
  seed: 5965
  
processing:
  batch_size: 15
  
validation:
  success_criteria:
    - threshold: 0.7
```

### Harness Manifest Format (Output)
```yaml
metadata:
  name: "Editor Tool Enhancement Potential"
  tags: ["school", "hypothesis", "capabilityassertion"]

model:
  type: "behavioral_governance"
  
corpus:
  type: "synthetic"
  size: 20    # Limited for testing
  seed: 5965

processing:
  batch_size: 5    # Limited for testing
  timeout_seconds: 60

validation:
  success_criteria:
    min_processed_items: 14    # 70% of corpus
    max_error_rate: 0.2
```

## Troubleshooting

### Common Issues

**"No experiment manifests found"**
- Ensure Stage 2 (Experiment Manifest Generator) has been completed
- Verify `assets/experiments/school/manifests/` directory exists and contains `.yaml` files
- Click "Refresh Manifests" to reload

**"Python harness execution failed"**
- Verify Python 3.11+ is installed and available as `python3`
- Check that required packages are installed: `pip install -r engine/requirements.txt`
- Review execution logs for specific Python errors

**"Git commit information unavailable"**
- Ensure project is in a Git repository: `git init` if needed
- Configure Git user: `git config user.name "Name"` and `git config user.email "email@example.com"`
- Make at least one commit to establish Git history

**"Process timeout"**
- Increase timeout in `ManifestConverter.cs` if needed
- Check system resources and close unnecessary applications
- Verify Python harness dependencies are properly installed

### Debug Mode

Enable detailed logging in Unity Console:
```csharp
Debug.Log($"[ExperimentRunner] Processing manifest: {manifest.Name}");
```

View execution logs in the expandable log section of the UI, or check individual run directories for `execution_logs.txt`.

### Validation

The experiment runner includes validation checks:
- Manifest format validation before conversion
- Python harness exit code checking
- Output file existence verification
- Git repository status validation

## Performance Considerations

- **Corpus Size**: Limited to 50 items for testing to ensure reasonable execution times
- **Batch Size**: Limited to 10 items to prevent memory issues
- **Timeout**: Set to 60 seconds per experiment for responsive UI
- **Parallel Execution**: Currently sequential to avoid resource conflicts

## Integration with CI/CD

The generated `github_integration.json` file provides metadata for CI/CD integration:

```yaml
# Example GitHub Actions workflow
- name: Process Experiment Results
  run: |
    python scripts/process_experiment_results.py \
      --integration-data assets/experiments/school/outputs/runs/github_integration.json \
      --commit ${{ github.sha }}
```

## Future Enhancements

- **Parallel Execution**: Support for running multiple experiments simultaneously
- **Real-time Monitoring**: Live progress updates and resource usage monitoring  
- **Result Visualization**: Built-in charts and analysis within Unity Editor
- **Automated Analysis**: Integration with Stage 4 analysis and reporting tools
- **Custom Processors**: Support for Unity-specific experiment processors
- **Performance Profiling**: Detailed performance metrics and optimization suggestions