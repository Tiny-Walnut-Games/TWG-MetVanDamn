#!/usr/bin/env python3
"""
Experiment Framework Demo v0.7

Demonstrates the complete experiment harness capabilities including
experiment execution, benchmarking, A/B testing, and regression tracking.

🧙‍♂️ "A demonstration is worth a thousand explanations - 
    let the experiments speak for themselves." - Bootstrap Sentinel
"""

import os
import sys
import time
import json
import tempfile
from pathlib import Path

# Add engine to path
sys.path.insert(0, str(Path(__file__).parent))

try:
    from engine.experiment_harness import ExperimentHarness, ExperimentManifest
    from engine.benchmark_suite import BenchmarkSuite, BenchmarkType, BenchmarkSeverity
    from engine.ab_evaluator import ABEvaluator, SplitStrategy
    from engine.regression_tracker import RegressionTracker, TimeSeriesMetric
    IMPORTS_AVAILABLE = True
except ImportError as e:
    IMPORTS_AVAILABLE = False
    print(f"Warning: Could not import components: {e}")


def demo_experiment_execution():
    """Demonstrate basic experiment execution."""
    print("🧪 Demo: Basic Experiment Execution")
    print("=" * 50)
    
    if not IMPORTS_AVAILABLE:
        print("⚠️  Skipping - imports not available")
        return
    
    # Create temporary results directory
    with tempfile.TemporaryDirectory() as temp_dir:
        harness = ExperimentHarness(results_base_path=temp_dir)
        
        # Create simple experiment manifest
        manifest_data = {
            "metadata": {
                "name": "Demo Simple Experiment",
                "description": "Demonstrates basic experiment capabilities",
                "version": "1.0.0",
                "author": "Demo Script"
            },
            "model": {
                "type": "simple",
                "performance_profile": "dev"
            },
            "conditions": {
                "processing_mode": ["sequential", "parallel"],
                "batch_size": [10, 20]
            },
            "corpus": {
                "type": "synthetic",
                "size": 100,
                "seed": 42
            },
            "processing": {
                "mode": "adaptive",
                "max_workers": 2,
                "timeout_seconds": 60
            },
            "metrics": {
                "performance_metrics": [
                    "throughput_items_per_sec",
                    "success_rate_pct",
                    "processing_time_ms"
                ]
            },
            "output": {
                "base_path": temp_dir,
                "formats": ["json"]
            },
            "execution": {
                "random_seeds": {"global_seed": 42}
            },
            "validation": {
                "success_criteria": {
                    "min_processed_items": 80,
                    "max_error_rate": 0.1
                }
            },
            "integration": {
                "chronicle_integration": {"enabled": False},
                "pet_events": {"enabled": False},
                "telemetry": {"enabled": False}
            }
        }
        
        manifest = ExperimentManifest(**manifest_data)
        
        # Track progress
        completed_runs = []
        def progress_callback(run):
            completed_runs.append(run)
            print(f"   ✅ Completed run: {run.run_id} ({run.status.value})")
        
        print("🚀 Starting experiment execution...")
        start_time = time.time()
        
        results = harness.run_experiment(manifest, progress_callback)
        
        execution_time = time.time() - start_time
        
        print(f"\n📊 Experiment Results:")
        print(f"   Experiment ID: {results['experiment_id']}")
        print(f"   Duration: {execution_time:.2f}s")
        print(f"   Total runs: {results['total_runs']}")
        print(f"   Successful runs: {results['completed_runs']}")
        print(f"   Failed runs: {results['failed_runs']}")
        print(f"   Success rate: {results['completed_runs']/results['total_runs']:.1%}")
        
        # Show aggregate metrics
        if results.get('aggregate_metrics'):
            print(f"\n📈 Key Metrics:")
            for metric, value in results['aggregate_metrics'].items():
                if isinstance(value, (int, float)) and not metric.endswith('_std'):
                    print(f"   {metric}: {value:.3f}")
        
        print("✅ Demo experiment execution completed!\n")


def demo_benchmark_suite():
    """Demonstrate benchmark suite capabilities."""
    print("📊 Demo: Benchmark Suite")
    print("=" * 50)
    
    if not IMPORTS_AVAILABLE:
        print("⚠️  Skipping - imports not available")
        return
    
    suite = BenchmarkSuite()
    
    # List available benchmarks
    benchmarks = suite.list_benchmarks()
    print(f"📋 Available benchmarks: {len(benchmarks)}")
    
    for benchmark in benchmarks[:3]:  # Show first 3
        print(f"   • {benchmark.name} ({benchmark.benchmark_type.value})")
        print(f"     {benchmark.description}")
        print(f"     Duration: ~{benchmark.expected_duration_seconds}s")
    
    # Create and run a custom lightweight benchmark
    print(f"\n🧪 Creating custom benchmark...")
    
    custom_benchmark = suite.create_custom_benchmark(
        name="demo_benchmark",
        description="Lightweight demo benchmark",
        benchmark_type=BenchmarkType.COGNITIVE_PERFORMANCE,
        severity=BenchmarkSeverity.LIGHT,
        expected_duration_seconds=10.0,
        manifest_template={
            "metadata": {"name": "Demo Benchmark"},
            "model": {"type": "simple", "performance_profile": "dev"},
            "conditions": {"param": [1, 2]},
            "corpus": {"type": "synthetic", "size": 20, "seed": 42},
            "processing": {"batch_size": 5, "mode": "sequential"},
            "metrics": {"performance_metrics": ["throughput_items_per_sec"]},
            "output": {"base_path": "demo_results", "formats": ["json"]},
            "execution": {"random_seeds": {"global_seed": 42}},
            "validation": {"success_criteria": {}},
            "integration": {"chronicle_integration": {"enabled": False}}
        },
        success_criteria={
            "min_throughput_items_per_sec": 5.0
        }
    )
    
    print(f"✅ Created: {custom_benchmark.name}")
    
    # Run the benchmark
    print(f"\n🏃 Running demo benchmark...")
    start_time = time.time()
    
    result = suite.run_benchmark("demo_benchmark")
    
    execution_time = time.time() - start_time
    
    print(f"📊 Benchmark Results:")
    print(f"   Status: {result.status}")
    print(f"   Duration: {result.duration_seconds:.2f}s")
    print(f"   Success criteria met: {sum(result.success_criteria_met.values())}/{len(result.success_criteria_met)}")
    
    if result.metrics:
        print(f"   Key metrics:")
        for metric, value in result.metrics.items():
            if isinstance(value, (int, float)):
                print(f"     {metric}: {value:.3f}")
    
    print("✅ Demo benchmark suite completed!\n")


def demo_ab_testing():
    """Demonstrate A/B testing capabilities."""
    print("🔬 Demo: A/B Testing")
    print("=" * 50)
    
    if not IMPORTS_AVAILABLE:
        print("⚠️  Skipping - imports not available")
        return
    
    evaluator = ABEvaluator()
    
    # Create base manifest for A/B testing
    manifest_data = {
        "metadata": {"name": "A/B Test Demo"},
        "model": {"type": "simple", "performance_profile": "dev"},
        "conditions": {"test_param": [1]},
        "corpus": {
            "type": "synthetic", 
            "size": 200, 
            "seed": 42,
            "ab_split": {"enabled": True, "control_ratio": 0.5, "treatment_ratio": 0.5}
        },
        "processing": {"batch_size": 10, "mode": "sequential"},
        "metrics": {"performance_metrics": ["throughput_items_per_sec"]},
        "output": {"base_path": "ab_results", "formats": ["json"]},
        "execution": {"random_seeds": {"global_seed": 42}},
        "validation": {"success_criteria": {}},
        "integration": {"chronicle_integration": {"enabled": False}}
    }
    
    manifest = ExperimentManifest(**manifest_data)
    
    # Create A/B test comparing batch sizes
    print("🧪 Creating A/B test: Batch Size Comparison")
    
    ab_test = evaluator.create_ab_test(
        test_name="Batch Size Comparison Demo",
        base_manifest=manifest,
        control_config={"processing.batch_size": 10},
        treatment_configs=[{"processing.batch_size": 20}],
        split_strategy=SplitStrategy.RANDOM,
        primary_metric="throughput_items_per_sec",
        minimum_sample_size=50,
        significance_level=0.05
    )
    
    print(f"✅ A/B test created: {ab_test.test_name}")
    print(f"   Test ID: {ab_test.test_id}")
    print(f"   Primary metric: {ab_test.primary_metric}")
    print(f"   Significance level: {ab_test.significance_level}")
    
    # Demo corpus splitting (without full execution)
    print(f"\n📋 Testing corpus splitting...")
    
    variant_corpora = evaluator._split_corpus_multivariate(manifest, ab_test)
    
    total_samples = 0
    for variant_name, corpus in variant_corpora.items():
        print(f"   {variant_name}: {len(corpus)} samples ({len(corpus)/200:.1%})")
        total_samples += len(corpus)
    
    print(f"   Total samples: {total_samples}/200 ({total_samples/200:.1%})")
    
    # Demo statistical analysis with mock data
    print(f"\n📊 Demo statistical analysis...")
    
    # Simulate some results
    control_data = [15.2, 16.1, 14.8, 15.9, 15.3, 16.0, 14.7, 15.8]
    treatment_data = [18.1, 17.9, 18.5, 17.7, 18.3, 18.0, 17.8, 18.2]
    
    t_stat, p_value = evaluator._welch_t_test(control_data, treatment_data)
    
    print(f"   Sample sizes: Control={len(control_data)}, Treatment={len(treatment_data)}")
    print(f"   Means: Control={sum(control_data)/len(control_data):.2f}, Treatment={sum(treatment_data)/len(treatment_data):.2f}")
    print(f"   Statistical test: t={t_stat:.3f}, p={p_value:.3f}")
    print(f"   Significant at α=0.05: {'Yes' if p_value < 0.05 else 'No'}")
    
    # Effect size
    mock_results = {
        "control": {"aggregate_metrics": {"throughput_items_per_sec": sum(control_data)/len(control_data)}},
        "treatment_1": {"aggregate_metrics": {"throughput_items_per_sec": sum(treatment_data)/len(treatment_data)}}
    }
    
    effect_sizes = evaluator._calculate_effect_sizes(mock_results, "throughput_items_per_sec")
    
    for comparison, effect_size in effect_sizes.items():
        interpretation = evaluator._interpret_effect_size(effect_size)
        print(f"   Effect size ({comparison}): {effect_size:.3f} ({interpretation})")
    
    print("✅ Demo A/B testing completed!\n")


def demo_regression_tracking():
    """Demonstrate regression tracking capabilities."""
    print("📈 Demo: Regression Tracking")
    print("=" * 50)
    
    if not IMPORTS_AVAILABLE:
        print("⚠️  Skipping - imports not available")
        return
    
    tracker = RegressionTracker()
    
    # Simulate some time series data
    print("📊 Simulating time series data...")
    
    current_time = time.time()
    metrics = []
    
    # Generate 20 data points over the last "hour" (compressed time)
    for i in range(20):
        timestamp = current_time - (3600 - i * 180)  # 3-minute intervals
        
        # Simulate metrics with some trend and noise
        base_throughput = 25.0 + (i * 0.5)  # Improving trend
        noise = (-1 + 2 * (i % 3) / 2) * 2.0  # Some noise
        throughput = base_throughput + noise
        
        # Add some degradation at the end
        if i > 15:
            throughput *= 0.85  # 15% degradation
        
        metrics.append(TimeSeriesMetric(
            timestamp=timestamp,
            metric_name="throughput_items_per_sec",
            value=throughput,
            experiment_id="demo_experiment",
            run_id=f"demo_run_{i}",
            condition_hash="demo_hash",
            metadata={"demo": True}
        ))
        
        # Add error rate metric (should be lower is better)
        error_rate = 0.02 + (i * 0.001)  # Slowly increasing
        if i > 15:
            error_rate *= 2.5  # Spike in errors
            
        metrics.append(TimeSeriesMetric(
            timestamp=timestamp,
            metric_name="error_rate",
            value=error_rate,
            experiment_id="demo_experiment", 
            run_id=f"demo_run_{i}",
            condition_hash="demo_hash",
            metadata={"demo": True}
        ))
    
    # Record the data
    tracker.record_time_series_data(metrics)
    
    print(f"✅ Recorded {len(metrics)} time series data points")
    
    # Set baselines
    print(f"\n📏 Setting performance baselines...")
    
    tracker.set_baseline("throughput_items_per_sec", 25.0, 
                        confidence_interval=(24.0, 26.0), sample_size=10)
    tracker.set_baseline("error_rate", 0.02,
                        confidence_interval=(0.015, 0.025), sample_size=10)
    
    print(f"   Throughput baseline: 25.0 items/sec")
    print(f"   Error rate baseline: 0.02 (2%)")
    
    # Check for regressions
    print(f"\n🔍 Checking for performance regressions...")
    
    alerts = tracker.check_for_regressions(recent_hours=1)
    
    print(f"📢 Found {len(alerts)} regression alerts:")
    
    for alert in alerts:
        severity_emoji = {"minor": "🟡", "moderate": "🟠", "major": "🔴", "critical": "🚨"}
        emoji = severity_emoji.get(alert.severity.value, "⚠️")
        
        print(f"   {emoji} {alert.severity.value.upper()}: {alert.metric_name}")
        print(f"     Current: {alert.current_value:.3f}")
        print(f"     Baseline: {alert.baseline_value:.3f}")
        print(f"     Change: {alert.degradation_pct*100:+.1f}%")
    
    # Analyze trends
    print(f"\n📈 Analyzing performance trends...")
    
    for metric_name in ["throughput_items_per_sec", "error_rate"]:
        trend = tracker.analyze_trends(metric_name, days=1)
        
        if trend:
            direction_emoji = {
                "improving": "📈", "stable": "➡️", "degrading": "📉", "volatile": "📊"
            }
            emoji = direction_emoji.get(trend.direction.value, "📊")
            
            print(f"   {emoji} {metric_name}:")
            print(f"     Direction: {trend.direction.value}")
            print(f"     Slope: {trend.slope:.6f}/day")
            print(f"     Confidence (R²): {trend.r_squared:.3f}")
            print(f"     Recent mean: {trend.recent_mean:.3f}")
            print(f"     Baseline mean: {trend.baseline_mean:.3f}")
    
    # Generate dashboard
    print(f"\n📊 Generating regression dashboard...")
    
    dashboard_path = tracker.generate_regression_dashboard("demo_dashboard.html")
    
    print(f"✅ Dashboard generated: {dashboard_path}")
    print(f"   Open in browser to view detailed analysis")
    
    print("✅ Demo regression tracking completed!\n")


def main():
    """Run all demonstration functions."""
    print("🧙‍♂️ TWG-TLDA Experiment Framework v0.7 - Demo Suite")
    print("=" * 70)
    print("Demonstrating experiment harness, benchmarks, A/B testing, and regression tracking\n")
    
    if not IMPORTS_AVAILABLE:
        print("⚠️  WARNING: Some imports not available - running limited demos")
        print("    This is expected in environments without full dependencies\n")
    
    # Run all demos
    demo_functions = [
        demo_experiment_execution,
        demo_benchmark_suite,
        demo_ab_testing,
        demo_regression_tracking
    ]
    
    for demo_func in demo_functions:
        try:
            demo_func()
        except Exception as e:
            print(f"❌ Demo {demo_func.__name__} failed: {e}")
            import traceback
            traceback.print_exc()
            print()
    
    print("🎉 All demonstrations completed!")
    print("\n📜 Key Capabilities Demonstrated:")
    print("📜 Experiment harness with YAML configuration")
    print("📜 Standardized benchmark suite with success criteria")
    print("📜 A/B testing with statistical significance analysis")
    print("📜 Regression tracking with automated alert generation")
    print("📜 Integration with existing TWG-TLDA systems")
    
    print("\n🧙‍♂️ The experiment laboratory is ready for scientific discovery!")
    print("\n🚀 Next Steps:")
    print("   • Create your own experiment manifests")
    print("   • Run benchmark suites for performance validation")
    print("   • Set up A/B tests for intervention comparison")
    print("   • Monitor performance with regression tracking")


if __name__ == "__main__":
    main()