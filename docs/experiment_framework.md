# Experiment Harness & Benchmark Suite v0.7 Documentation

## Overview

The TWG-TLDA Experiment Harness provides a comprehensive framework for conducting cognitive experiments, performance benchmarking, and A/B testing with robust statistical analysis and regression tracking.

🧙‍♂️ *"Every hypothesis is a doorway; experimentation is the key that opens it to reveal the truth beyond."* - Bootstrap Sentinel

## Architecture

### Core Components

1. **ExperimentHarness** (`engine/experiment_harness.py`)
   - YAML-based experiment configuration
   - Automatic run generation from experimental conditions
   - Integration with existing BatchEvaluationEngine
   - Progress tracking and checkpointing
   - Statistical analysis and reporting

2. **BenchmarkSuite** (`engine/benchmark_suite.py`)
   - Standardized cognitive performance benchmarks
   - Configurable severity levels (light, standard, intensive)
   - Success criteria validation
   - Performance comparison and trending

3. **ABEvaluator** (`engine/ab_evaluator.py`)
   - A/B testing with statistical significance
   - Multiple corpus splitting strategies
   - Effect size calculation and confidence intervals
   - Sequential testing and early stopping

4. **RegressionTracker** (`engine/regression_tracker.py`)
   - Time series performance monitoring
   - Automated regression detection
   - Trend analysis and forecasting
   - HTML dashboard generation

## Quick Start

### 1. Run a Simple Experiment

```bash
# Create experiment manifest
python engine/experiment_harness.py run \
  --manifest examples/experiments/simple_intervention_test.yaml

# Results saved to experiments/results/
```

### 2. Execute Benchmark Suite

```bash
# List available benchmarks
python engine/benchmark_suite.py list

# Run cognitive performance benchmarks
python engine/benchmark_suite.py suite --type cognitive_performance

# Run all light-weight benchmarks
python engine/benchmark_suite.py suite --severity light
```

### 3. A/B Test Two Configurations

```bash
# Compare intervention thresholds
python engine/ab_evaluator.py run \
  --test-name "Threshold Comparison" \
  --manifest examples/experiments/ab_intervention_comparison.yaml \
  --control-config '{"model.instance_config.intervention_threshold": 0.8}' \
  --treatment-configs '{"model.instance_config.intervention_threshold": 0.5}'
```

### 4. Monitor Performance Regression

```bash
# Set baselines from recent data
python engine/regression_tracker.py baseline --days 7

# Check for recent regressions
python engine/regression_tracker.py check --hours 24

# Generate regression dashboard
python engine/regression_tracker.py dashboard
```

## Experiment Manifest Format

Experiments are defined using YAML manifests with the following structure:

```yaml
metadata:
  name: "My Experiment"
  description: "Description of what this experiment tests"
  version: "1.0.0"
  author: "Researcher Name"
  tags: ["cognitive", "intervention"]

model:
  type: "behavioral_governance"  # or "batch_evaluation", "simple"
  instance_config:
    enable_intervention_tracking: true
    intervention_threshold: 0.7
  performance_profile: "experiment"  # dev, balanced, perf, experiment

conditions:
  # Variables to test - creates Cartesian product of runs
  intervention_types:
    - "soft_suggestion"
    - "rewrite"
  confidence_thresholds:
    - 0.5
    - 0.8

corpus:
  type: "synthetic"  # synthetic, file, stream
  size: 100
  seed: 42

processing:
  batch_size: 10
  mode: "adaptive"  # sequential, parallel, adaptive, priority_based
  max_workers: 2

metrics:
  behavioral_metrics:
    - "intervention_acceptance_rate"
    - "response_quality_score"
  performance_metrics:
    - "throughput_items_per_sec"
    - "success_rate_pct"

validation:
  success_criteria:
    min_processed_items: 80
    max_error_rate: 0.1
```

## Available Benchmarks

### Cognitive Performance
- **cognitive_baseline**: Baseline measurement across intervention types
- **intervention_effectiveness**: Tests intervention strategy effectiveness
- **behavioral_alignment**: Measures consistency across contexts

### System Performance  
- **system_throughput**: Throughput and scaling characteristics
- **memory_efficiency**: Memory usage patterns and optimization
- **stress_test**: High-load reliability testing

Each benchmark includes:
- Predefined success criteria
- Expected duration estimates
- Resource requirements
- Automated results analysis

## A/B Testing Features

### Splitting Strategies
- **Random**: Random assignment to variants
- **Sequential**: Ordered assignment
- **Hash-based**: Deterministic based on content hash
- **Stratified**: Balanced across categories

### Statistical Analysis
- Welch's t-test for significance testing
- Cohen's d effect size calculation
- Confidence interval estimation
- Power analysis and sample size recommendations

### Advanced Testing
- **Sequential Testing**: Early stopping based on significance
- **Multi-armed Bandit**: Adaptive optimization during testing
- **Regression Analysis**: Compare against historical baselines

## Integration with Existing Systems

### BatchEvaluationEngine
```python
# Experiments automatically use BatchEvaluationEngine for processing
# Inherits all batch processing capabilities:
# - Adaptive parallelism
# - Progress checkpointing
# - Multiple replay modes
# - Error handling and recovery
```

### BehavioralGovernance
```python
# Cognitive experiments integrate with intervention tracking
# Measures intervention acceptance, style consistency, etc.
```

### Performance Profiles
```python
# Experiments use existing performance profiles:
# - dev: Development/debugging (small scale)
# - balanced: General purpose testing
# - perf: High-performance optimization
# - experiment: Research workloads
```

## Regression Tracking

### Time Series Storage
- SQLite database for efficient time series storage
- Automatic baseline computation from historical data
- Configurable retention and aggregation policies

### Alert Generation
```python
# Regression severity levels:
# - Minor: 5-15% degradation
# - Moderate: 15-30% degradation  
# - Major: 30-50% degradation
# - Critical: >50% degradation
```

### Dashboard Features
- Real-time performance monitoring
- Trend analysis with R² correlation
- Alert prioritization and tracking
- Historical comparison charts

## Programmatic Usage

### Creating Custom Experiments

```python
from engine.experiment_harness import ExperimentHarness, ExperimentManifest

# Create experiment programmatically
manifest_data = {
    "metadata": {"name": "Custom Experiment"},
    "model": {"type": "simple"},
    "conditions": {"param": [1, 2, 3]},
    "corpus": {"type": "synthetic", "size": 50},
    "processing": {"batch_size": 10},
    # ... rest of configuration
}

harness = ExperimentHarness()
manifest = ExperimentManifest(**manifest_data)

# Execute with progress tracking
def progress_callback(run):
    print(f"Completed: {run.run_id}")

results = harness.run_experiment(manifest, progress_callback)
```

### Custom Benchmark Creation

```python
from engine.benchmark_suite import BenchmarkSuite, BenchmarkType

suite = BenchmarkSuite()

# Create custom benchmark
custom_benchmark = suite.create_custom_benchmark(
    name="custom_test",
    description="My custom benchmark",
    benchmark_type=BenchmarkType.COGNITIVE_PERFORMANCE,
    manifest_template={
        # Experiment configuration
    },
    success_criteria={
        "min_accuracy": 0.9,
        "max_latency_ms": 100
    }
)

# Run the benchmark
result = suite.run_benchmark("custom_test")
```

### A/B Testing API

```python
from engine.ab_evaluator import ABEvaluator, SplitStrategy

evaluator = ABEvaluator()

# Create A/B test
ab_test = evaluator.create_ab_test(
    test_name="Configuration Comparison",
    base_manifest=manifest,
    control_config={"threshold": 0.8},
    treatment_configs=[{"threshold": 0.5}, {"threshold": 0.9}],
    split_strategy=SplitStrategy.RANDOM,
    primary_metric="intervention_acceptance_rate"
)

# Execute test
result = evaluator.run_ab_test(ab_test, manifest)

# Analyze results
if result.conclusions["overall_winner"]:
    print(f"Winner: {result.conclusions['overall_winner']}")
    for rec in result.recommendations:
        print(f"- {rec}")
```

## Best Practices

### Experiment Design
1. **Clear Hypotheses**: Define what you're testing and expected outcomes
2. **Controlled Variables**: Limit the number of changing variables per experiment
3. **Sufficient Sample Size**: Use statistical power analysis to determine minimum samples
4. **Reproducibility**: Set random seeds and document all configuration

### Performance Testing
1. **Baseline First**: Establish performance baselines before optimization
2. **Isolated Testing**: Test one change at a time for clear attribution
3. **Multiple Runs**: Average results across multiple runs to reduce noise
4. **Resource Monitoring**: Track memory, CPU, and other system resources

### A/B Testing
1. **Single Metric Focus**: Choose one primary metric for decision making
2. **Statistical Significance**: Wait for sufficient data before making decisions
3. **Practical Significance**: Consider effect size, not just p-values
4. **Segment Analysis**: Consider how results vary across user segments

### Regression Prevention
1. **Continuous Monitoring**: Set up automated regression detection
2. **Trend Analysis**: Monitor performance trends, not just absolute values
3. **Alert Tuning**: Adjust thresholds to minimize false positives
4. **Root Cause Analysis**: Investigate regressions promptly

## Advanced Features

### Sequential A/B Testing
```bash
# Run A/B test with early stopping
python engine/ab_evaluator.py sequential \
  --test-name "Early Stopping Test" \
  --manifest test.yaml \
  --control-config '{"param": 1}' \
  --treatment-configs '{"param": 2}'
```

### Multi-Armed Bandit Optimization
```bash
# Adaptive optimization during testing
python engine/ab_evaluator.py bandit \
  --test-name "Bandit Optimization" \
  --manifest test.yaml \
  --control-config '{"param": 1}' \
  --treatment-configs '{"param": 2}' '{"param": 3}'
```

### Custom Regression Metrics
```python
from engine.regression_tracker import RegressionTracker

tracker = RegressionTracker()

# Set custom baseline
tracker.set_baseline("custom_metric", 0.85, 
                    confidence_interval=(0.8, 0.9),
                    sample_size=100)

# Monitor for regressions
alerts = tracker.check_for_regressions(recent_hours=2)
```

## Troubleshooting

### Common Issues

**Experiment Fails to Start**
- Check manifest YAML syntax
- Verify all required fields are present
- Ensure corpus source is accessible

**Low Statistical Power**
- Increase sample size
- Reduce measurement noise
- Choose more sensitive metrics

**Memory Issues in Large Experiments**
- Use smaller batch sizes
- Enable checkpointing
- Switch to sequential processing mode

**Dashboard Not Updating**
- Check database permissions
- Verify time series data is being recorded
- Run baseline computation manually

### Debug Mode
```bash
# Enable detailed logging
export EXPERIMENT_DEBUG=1
python engine/experiment_harness.py run --manifest test.yaml
```

### Performance Optimization
```bash
# Use performance profile for faster execution
# Edit manifest: performance_profile: "perf"
# Or override via environment
export EXPERIMENT_PROFILE=perf
```

## File Structure

```
experiments/
├── results/              # Experiment results (JSON/CSV)
├── benchmark_results/    # Benchmark execution results  
├── ab_results/          # A/B test results
├── regression_dashboard.html  # Performance dashboard
└── regression_tracking.db     # Time series database

examples/experiments/
├── simple_intervention_test.yaml
├── ab_intervention_comparison.yaml
└── performance_stress_test.yaml

schemas/
└── experiment_manifest.yaml    # Schema documentation

engine/
├── experiment_harness.py      # Main experiment runner
├── benchmark_suite.py         # Standardized benchmarks
├── ab_evaluator.py           # A/B testing framework
└── regression_tracker.py      # Performance monitoring
```

## Integration with TWG-TLDA Ecosystem

### Chronicle Keeper (TLDL)
- Automatic TLDL generation for significant experiments
- Experiment context preservation
- Design decision documentation

### Pet Events System
- Milestone tracking for experiment completion
- Evolution triggers for performance improvements
- Gamification of research progress

### Telemetry Integration
- System metrics collection during experiments
- Developer state tracking (optional)
- Resource usage monitoring

---

🧙‍♂️ *"In the grand experiment of existence, every test teaches us not just what works, but how to ask better questions."* - Bootstrap Sentinel