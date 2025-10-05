# School Experiment - Stage 2: Experiment Manifest Generator

## Overview

The **Experiment Manifest Generator** is a Unity Editor tool that creates YAML experiment manifests for each hypothesis generated in Stage 1. This tool transforms hypothesis data into structured experiment configurations compatible with the TWG-TLDA experiment framework.

## Purpose

Stage 2 bridges the gap between hypothesis extraction (Stage 1) and experiment execution by generating standardized YAML manifests that define:
- Experiment parameters based on hypothesis characteristics
- Deterministic seeds for reproducible results
- Dataset references and corpus configurations
- Expected metrics and validation criteria
- GitHub integration metadata

## Usage

### Prerequisites

1. **Stage 1 Complete**: The hypothesis extraction tool must have been run and generated `assets/experiments/school/hypothesis_drafts.json`
2. **Unity Editor**: Unity 6 (6000.2.0f1 or later) with the project open
3. **Output Directory**: The tool will automatically create `assets/experiments/school/manifests/` if it doesn't exist

### Running Stage 2

1. **Open Unity Editor** with the TWG-TLDA project
2. **Navigate to Menu**: `Tools > School > Generate Experiment Manifests`
3. **Load Hypotheses**: Click "Load Hypotheses" to read the hypothesis_drafts.json file
4. **Generate Manifests**: Click "Generate Experiment Manifests" to create YAML files
5. **View Results**: Use "Open Output Directory" to browse generated manifests

### Workflow Integration

```
Stage 0: Inventory Collection
    ↓ (inventory.json)
Stage 1: Hypothesis Extraction
    ↓ (hypothesis_drafts.json)
Stage 2: Experiment Manifest Generation ← YOU ARE HERE
    ↓ (experiment_*.yaml manifests)
Stage 3: Experiment Execution (Future)
```

## Manifest Generation Logic

### Input Processing

The tool reads `hypothesis_drafts.json` containing:
- Hypothesis metadata (ID, title, description)
- Faculty surface information (path, type)
- Hypothesis classification (CapabilityAssertion, ImprovementTarget)
- Assessment metrics (priority, confidence, effort estimates)

### Parameter Mapping

#### Model Configuration
- **CapabilityAssertion** → `behavioral_governance` model
- **ImprovementTarget** → `batch_evaluation` model
- Intervention threshold mapped from hypothesis confidence (0.3-0.9 range)

#### Corpus Sizing
- **Base Size**: 100 samples
- **Type Adjustment**: +50% for CapabilityAssertion, +100% for ImprovementTarget
- **Priority Scaling**: High (+100%), Medium (+50%), Low (baseline)
- **Confidence Scaling**: >0.8 (+20%), <0.5 (-20%)

#### Batch Processing
- **Base Batch Size**: 10
- **Priority Adjustment**: High (20), Medium (15), Low (10)
- Parallel execution enabled for all experiments

#### Seed Generation
- Deterministic seed generated from hypothesis ID hash
- Ensures reproducible results across runs
- Range: 1000-10999 for consistent behavior

### Tag Generation

Each experiment receives tags based on:
- **Base Tags**: `school`, `hypothesis`
- **Type Tags**: `capability`/`assertion` or `improvement`/`optimization`
- **Priority Tag**: `high`, `medium`, or `low`
- **Surface Type**: `script`, `prefab`, `material`, etc.
- **Confidence Level**: `high-confidence`, `medium-confidence`, `low-confidence`

## Output Structure

### File Naming
```
assets/experiments/school/manifests/experiment_{hypothesis-id}.yaml
```

### YAML Manifest Format

```yaml
metadata:
  name: "Hypothesis Title"
  description: "Hypothesis description"
  version: "1.0.0"
  author: "School Experiment Framework"
  tags: ["school", "hypothesis", "capability", "medium", "script"]
  hypothesis_id: "uuid-string"
  faculty_surface: "Assets/Path/To/Asset.cs"
  hypothesis_type: "CapabilityAssertion"

model:
  type: "behavioral_governance"
  instance_config:
    enable_intervention_tracking: true
    intervention_threshold: 0.7
    faculty_surface_focus: "Assets/Path/To/Asset.cs"
  performance_profile: "experiment"

conditions:
  hypothesis_types: ["CapabilityAssertion"]
  priority_levels: ["Medium"]
  confidence_ranges: [0.7]
  effort_estimates: ["2-4 hours"]

corpus:
  type: "synthetic"
  size: 225
  seed: 5965
  focus_assets: ["Assets/Path/To/Asset.cs"]

processing:
  batch_size: 15
  parallel_execution: true
  checkpoint_interval: 10

validation:
  success_criteria:
    - metric: "hypothesis_validation_score"
      threshold: 0.7
    - metric: "faculty_surface_coverage"
      threshold: 0.8
  expected_duration: "2-4 hours"

reporting:
  output_format: "yaml"
  include_metrics:
    - "execution_time"
    - "resource_usage"
    - "validation_score"
    - "faculty_surface_impact"
  dashboard_integration: true

experiment_context:
  source_inventory_hash: "d0865a96189ad5c3"
  generation_timestamp: "2024-09-06T01:54:49.609911Z"
  unity_version: "6000.2.0f1"
  project_path: "/path/to/project/Assets"
  stage: "2"
  workflow: "school"
```

## Features

### Unity 6 Compatibility
- Built for Unity 6 (6000.2.0f1+)
- Uses latest Unity Editor APIs
- Async/await pattern for responsive UI

### GitHub Integration Ready
- Includes Git repository context
- Timestamp tracking for CI/CD integration
- Deterministic output for version control

### Experiment Framework Integration
- Compatible with TWG-TLDA experiment harness
- Standard YAML format for Python processing
- Validation criteria for automated testing

### User Experience
- Progress tracking during generation
- Error handling with user feedback
- File browser integration
- Responsive Unity Editor window

## Implementation Details

### Code Architecture

```
ExperimentManifestGenerator (EditorWindow)
├── UI Drawing Methods
│   ├── DrawHeader()
│   ├── DrawInputSection()
│   ├── DrawGenerationSection()
│   ├── DrawResultsSection()
│   └── DrawOutputSection()
├── Core Processing
│   ├── LoadHypotheses()
│   ├── GenerateManifests()
│   └── GenerateManifestForHypothesis()
├── Manifest Creation
│   ├── CreateExperimentManifest()
│   ├── GetExperimentParametersFromHypothesis()
│   ├── GenerateSeedFromId()
│   └── GenerateExperimentTags()
└── Data Structures (shared with Stage 1)
    ├── HypothesisDrafts
    └── Hypothesis
```

### Error Handling
- JSON parsing validation
- File system error recovery
- Progress indication with cancellation
- User-friendly error dialogs

### Performance Considerations
- Async processing for large hypothesis sets
- Minimal memory footprint
- Efficient YAML generation
- UI responsiveness during processing

## Troubleshooting

### Common Issues

**"Input file not found"**
- Ensure Stage 1 (Hypothesis Extraction) has been completed
- Verify `assets/experiments/school/hypothesis_drafts.json` exists
- Check file permissions

**"Failed to load hypotheses"**
- Validate JSON syntax in hypothesis_drafts.json
- Check file encoding (should be UTF-8)
- Verify Unity console for detailed error messages

**"Generation failed"**
- Ensure sufficient disk space for manifests
- Check write permissions to output directory
- Verify hypothesis data integrity

**Empty or Invalid Manifests**
- Check hypothesis confidence values (0.0-1.0 range)
- Verify hypothesis type is "CapabilityAssertion" or "ImprovementTarget"
- Ensure all required hypothesis fields are populated

### Debug Mode

Enable detailed logging in Unity Console:
```csharp
Debug.Log($"[ExperimentManifestGenerator] Processing hypothesis: {hypothesis.Title}");
```

### Validation

Validate generated YAML syntax:
```bash
python -c "import yaml; yaml.safe_load(open('experiment_id.yaml'))"
```

## Integration with TWG-TLDA Ecosystem

### Chronicle Keeper (TLDL)
- Experiment context preserved in manifests
- Generation timestamps for audit trails
- Hypothesis lineage tracking

### Pet Events System
- Milestone tracking for manifest generation
- Progress notifications for large batch processing
- Achievement unlocks for experiment completion

### Experiment Harness
- Direct compatibility with Python experiment runner
- Standard YAML format for seamless integration
- Validation criteria for automated testing

---

🧙‍♂️ *"From hypothesis to manifest, every experiment is a spell cast into the future of understanding."* - Bootstrap Sentinel