#!/usr/bin/env python3
"""
Benchmark Suite v0.7 - Standardized Cognitive & Performance Benchmarks

Provides a collection of standardized benchmarks for measuring cognitive
performance, intervention effectiveness, and system capabilities.

🧙‍♂️ "A benchmark without context is just a number; 
    with context, it becomes wisdom." - Bootstrap Sentinel
"""

from __future__ import annotations
import sys
import time
import random
import json
import hashlib
from pathlib import Path
from typing import Dict, Any, List, Optional, Callable, Union
from dataclasses import dataclass, asdict
from enum import Enum
import datetime

# Add engine to path for imports
sys.path.insert(0, str(Path(__file__).parent))

try:
    from experiment_harness import ExperimentHarness, ExperimentManifest
    from batch_evaluation import BatchEvaluationEngine, ReplayMode
    from behavioral_governance import BehavioralGovernance
    from intervention_metrics import InterventionMetrics, InterventionType
    from performance_profiles import PerformanceProfileManager, get_global_profile_manager
    DEPENDENCIES_AVAILABLE = True
except ImportError as e:
    DEPENDENCIES_AVAILABLE = False
    print(f"Warning: Some dependencies not available: {e}")


class BenchmarkType(Enum):
    """Type of benchmark being executed."""
    COGNITIVE_PERFORMANCE = "cognitive_performance"
    INTERVENTION_EFFECTIVENESS = "intervention_effectiveness" 
    SYSTEM_THROUGHPUT = "system_throughput"
    MEMORY_EFFICIENCY = "memory_efficiency"
    BEHAVIORAL_ALIGNMENT = "behavioral_alignment"
    STRESS_TEST = "stress_test"


class BenchmarkSeverity(Enum):
    """Benchmark execution intensity."""
    LIGHT = "light"      # Quick validation, minimal resources
    STANDARD = "standard"  # Comprehensive testing
    INTENSIVE = "intensive"  # Stress testing, maximum resources


@dataclass
class BenchmarkDefinition:
    """Definition of a standardized benchmark."""
    name: str
    description: str
    benchmark_type: BenchmarkType
    severity: BenchmarkSeverity
    expected_duration_seconds: float
    resource_requirements: Dict[str, Any]
    success_criteria: Dict[str, Any]
    manifest_template: Dict[str, Any]
    version: str = "1.0.0"


@dataclass
class BenchmarkResult:
    """Result of executing a benchmark."""
    benchmark_name: str
    start_time: float
    end_time: float
    duration_seconds: float
    status: str  # "passed", "failed", "error"
    metrics: Dict[str, Any]
    success_criteria_met: Dict[str, bool]
    raw_experiment_results: Dict[str, Any]
    error_message: Optional[str] = None


class BenchmarkSuite:
    """
    Collection of standardized benchmarks for cognitive systems.
    
    Provides pre-defined benchmarks for common testing scenarios:
    - Cognitive performance under various conditions
    - Intervention effectiveness measurement
    - System throughput and scaling
    - Memory efficiency validation
    - Behavioral alignment verification
    """
    
    def __init__(self, harness: Optional[ExperimentHarness] = None):
        self.harness = harness or ExperimentHarness()
        self.benchmarks = self._initialize_benchmark_definitions()
        self.results_history: List[BenchmarkResult] = []
    
    def _initialize_benchmark_definitions(self) -> Dict[str, BenchmarkDefinition]:
        """Initialize all standard benchmark definitions."""
        benchmarks = {}
        
        # Cognitive Performance Benchmarks
        benchmarks["cognitive_baseline"] = BenchmarkDefinition(
            name="cognitive_baseline",
            description="Baseline cognitive performance measurement across intervention types",
            benchmark_type=BenchmarkType.COGNITIVE_PERFORMANCE,
            severity=BenchmarkSeverity.STANDARD,
            expected_duration_seconds=60.0,
            resource_requirements={
                "memory_mb": 256,
                "cpu_cores": 2,
                "corpus_size": 500
            },
            success_criteria={
                "min_throughput_items_per_sec": 10.0,
                "max_error_rate": 0.05,
                "min_intervention_acceptance_rate": 0.25
            },
            manifest_template={
                "metadata": {
                    "name": "Cognitive Baseline Benchmark",
                    "description": "Measures baseline cognitive performance",
                    "tags": ["cognitive", "baseline", "benchmark"]
                },
                "model": {
                    "type": "behavioral_governance",
                    "instance_config": {
                        "enable_intervention_tracking": True,
                        "intervention_threshold": 0.7
                    },
                    "performance_profile": "balanced"
                },
                "conditions": {
                    "intervention_types": ["soft_suggestion", "rewrite", "style_guidance"],
                    "confidence_thresholds": [0.5, 0.7, 0.9]
                },
                "corpus": {
                    "type": "synthetic",
                    "size": 500,
                    "seed": 42
                },
                "processing": {
                    "batch_size": 25,
                    "mode": "adaptive",
                    "max_workers": 2
                },
                "metrics": {
                    "behavioral_metrics": [
                        "intervention_acceptance_rate",
                        "response_quality_score",
                        "processing_time_ms"
                    ],
                    "performance_metrics": [
                        "throughput_items_per_sec",
                        "success_rate_pct",
                        "memory_usage_mb"
                    ]
                }
            }
        )
        
        # Intervention Effectiveness Benchmark
        benchmarks["intervention_effectiveness"] = BenchmarkDefinition(
            name="intervention_effectiveness",
            description="Measures effectiveness of different intervention strategies",
            benchmark_type=BenchmarkType.INTERVENTION_EFFECTIVENESS,
            severity=BenchmarkSeverity.STANDARD,
            expected_duration_seconds=90.0,
            resource_requirements={
                "memory_mb": 512,
                "cpu_cores": 2,
                "corpus_size": 300
            },
            success_criteria={
                "min_soft_suggestion_acceptance": 0.4,
                "min_rewrite_effectiveness": 0.6,
                "max_intervention_latency_ms": 100.0
            },
            manifest_template={
                "metadata": {
                    "name": "Intervention Effectiveness Benchmark",
                    "description": "Tests intervention strategy effectiveness",
                    "tags": ["intervention", "effectiveness", "benchmark"]
                },
                "model": {
                    "type": "behavioral_governance",
                    "instance_config": {
                        "enable_intervention_tracking": True,
                        "style_adaptation_enabled": True
                    },
                    "performance_profile": "experiment"
                },
                "conditions": {
                    "intervention_types": [
                        "soft_suggestion", 
                        "rewrite", 
                        "style_guidance", 
                        "safety_intervention"
                    ],
                    "style_contexts": [
                        "technical_documentation",
                        "code_review", 
                        "casual_conversation"
                    ]
                },
                "corpus": {
                    "type": "synthetic",
                    "size": 300,
                    "seed": 123
                },
                "processing": {
                    "batch_size": 20,
                    "mode": "parallel"
                }
            }
        )
        
        # System Throughput Benchmark
        benchmarks["system_throughput"] = BenchmarkDefinition(
            name="system_throughput",
            description="Measures system throughput and scaling characteristics",
            benchmark_type=BenchmarkType.SYSTEM_THROUGHPUT,
            severity=BenchmarkSeverity.INTENSIVE,
            expected_duration_seconds=120.0,
            resource_requirements={
                "memory_mb": 1024,
                "cpu_cores": 4,
                "corpus_size": 2000
            },
            success_criteria={
                "min_throughput_items_per_sec": 50.0,
                "max_memory_usage_mb": 800.0,
                "min_cpu_efficiency": 0.7
            },
            manifest_template={
                "metadata": {
                    "name": "System Throughput Benchmark",
                    "description": "Tests system throughput and scaling",
                    "tags": ["throughput", "scaling", "performance"]
                },
                "model": {
                    "type": "batch_evaluation",
                    "performance_profile": "perf"
                },
                "conditions": {
                    "batch_sizes": [10, 25, 50, 100],
                    "worker_counts": [1, 2, 4, 8]
                },
                "corpus": {
                    "type": "synthetic",
                    "size": 2000,
                    "seed": 456
                },
                "processing": {
                    "mode": "adaptive",
                    "timeout_seconds": 300
                }
            }
        )
        
        # Memory Efficiency Benchmark
        benchmarks["memory_efficiency"] = BenchmarkDefinition(
            name="memory_efficiency",
            description="Validates memory usage patterns and efficiency optimizations",
            benchmark_type=BenchmarkType.MEMORY_EFFICIENCY,
            severity=BenchmarkSeverity.STANDARD,
            expected_duration_seconds=75.0,
            resource_requirements={
                "memory_mb": 512,
                "cpu_cores": 2,
                "corpus_size": 1000
            },
            success_criteria={
                "max_peak_memory_mb": 400.0,
                "min_memory_reuse_rate": 0.6,
                "max_gc_collections": 50
            },
            manifest_template={
                "metadata": {
                    "name": "Memory Efficiency Benchmark",
                    "description": "Tests memory usage and optimization",
                    "tags": ["memory", "efficiency", "optimization"]
                },
                "model": {
                    "type": "behavioral_governance",
                    "performance_profile": "balanced"
                },
                "conditions": {
                    "memory_pool_sizes": [100, 500, 1000],
                    "anchor_counts": [50, 200, 500]
                },
                "corpus": {
                    "type": "synthetic",
                    "size": 1000,
                    "seed": 789
                },
                "processing": {
                    "batch_size": 50,
                    "mode": "sequential"
                }
            }
        )
        
        # Behavioral Alignment Benchmark
        benchmarks["behavioral_alignment"] = BenchmarkDefinition(
            name="behavioral_alignment",
            description="Measures behavioral alignment and consistency across contexts",
            benchmark_type=BenchmarkType.BEHAVIORAL_ALIGNMENT,
            severity=BenchmarkSeverity.STANDARD,
            expected_duration_seconds=100.0,
            resource_requirements={
                "memory_mb": 384,
                "cpu_cores": 2,
                "corpus_size": 400
            },
            success_criteria={
                "min_style_consistency": 0.8,
                "max_context_switching_penalty": 0.1,
                "min_alignment_score": 0.75
            },
            manifest_template={
                "metadata": {
                    "name": "Behavioral Alignment Benchmark",
                    "description": "Tests behavioral consistency and alignment",
                    "tags": ["behavioral", "alignment", "consistency"]
                },
                "model": {
                    "type": "behavioral_governance",
                    "instance_config": {
                        "enable_intervention_tracking": True,
                        "style_adaptation_enabled": True
                    },
                    "performance_profile": "experiment"
                },
                "conditions": {
                    "style_contexts": [
                        "technical_documentation",
                        "code_review",
                        "casual_conversation",
                        "formal_presentation"
                    ],
                    "intervention_thresholds": [0.3, 0.5, 0.7, 0.9]
                },
                "corpus": {
                    "type": "synthetic",
                    "size": 400,
                    "seed": 321
                }
            }
        )
        
        # Stress Test Benchmark
        benchmarks["stress_test"] = BenchmarkDefinition(
            name="stress_test",
            description="Stress testing under high load and adverse conditions",
            benchmark_type=BenchmarkType.STRESS_TEST,
            severity=BenchmarkSeverity.INTENSIVE,
            expected_duration_seconds=180.0,
            resource_requirements={
                "memory_mb": 2048,
                "cpu_cores": 8,
                "corpus_size": 5000
            },
            success_criteria={
                "min_success_rate": 0.9,
                "max_response_time_p99": 5000.0,  # 5 seconds
                "max_error_burst_rate": 0.02
            },
            manifest_template={
                "metadata": {
                    "name": "System Stress Test",
                    "description": "High-load stress testing",
                    "tags": ["stress", "load", "reliability"]
                },
                "model": {
                    "type": "behavioral_governance",
                    "performance_profile": "perf"
                },
                "conditions": {
                    "load_levels": ["normal", "high", "extreme"],
                    "error_injection_rates": [0.0, 0.01, 0.05]
                },
                "corpus": {
                    "type": "synthetic",
                    "size": 5000,
                    "seed": 999
                },
                "processing": {
                    "batch_size": 100,
                    "mode": "parallel",
                    "max_workers": 8,
                    "timeout_seconds": 600
                }
            }
        )
        
        return benchmarks
    
    def list_benchmarks(self, 
                       benchmark_type: Optional[BenchmarkType] = None,
                       severity: Optional[BenchmarkSeverity] = None) -> List[BenchmarkDefinition]:
        """List available benchmarks with optional filtering."""
        benchmarks = list(self.benchmarks.values())
        
        if benchmark_type:
            benchmarks = [b for b in benchmarks if b.benchmark_type == benchmark_type]
        
        if severity:
            benchmarks = [b for b in benchmarks if b.severity == severity]
        
        return benchmarks
    
    def get_benchmark(self, name: str) -> Optional[BenchmarkDefinition]:
        """Get specific benchmark definition by name."""
        return self.benchmarks.get(name)
    
    def run_benchmark(self, benchmark_name: str, **kwargs) -> BenchmarkResult:
        """Execute a single benchmark."""
        benchmark = self.get_benchmark(benchmark_name)
        if not benchmark:
            raise ValueError(f"Benchmark '{benchmark_name}' not found")
        
        print(f"🧙‍♂️ Starting benchmark: {benchmark.name}")
        print(f"   Description: {benchmark.description}")
        print(f"   Type: {benchmark.benchmark_type.value}")
        print(f"   Severity: {benchmark.severity.value}")
        print(f"   Expected duration: {benchmark.expected_duration_seconds}s")
        
        start_time = time.time()
        
        try:
            # Create experiment manifest from template
            manifest_data = benchmark.manifest_template.copy()
            
            # Apply any runtime overrides from kwargs
            if kwargs:
                self._apply_manifest_overrides(manifest_data, kwargs)
            
            # Create temporary manifest file
            manifest = ExperimentManifest(**manifest_data)
            
            # Execute experiment
            experiment_results = self.harness.run_experiment(manifest)
            
            end_time = time.time()
            duration = end_time - start_time
            
            # Extract metrics
            metrics = experiment_results.get("aggregate_metrics", {})
            
            # Evaluate success criteria
            success_criteria_met = self._evaluate_success_criteria(
                metrics, benchmark.success_criteria
            )
            
            # Determine overall status
            status = "passed" if all(success_criteria_met.values()) else "failed"
            
            result = BenchmarkResult(
                benchmark_name=benchmark_name,
                start_time=start_time,
                end_time=end_time,
                duration_seconds=duration,
                status=status,
                metrics=metrics,
                success_criteria_met=success_criteria_met,
                raw_experiment_results=experiment_results
            )
            
            self.results_history.append(result)
            
            print(f"✅ Benchmark {benchmark_name} completed: {status}")
            print(f"   Duration: {duration:.1f}s")
            print(f"   Success criteria: {sum(success_criteria_met.values())}/{len(success_criteria_met)} met")
            
            return result
            
        except Exception as e:
            end_time = time.time()
            duration = end_time - start_time
            
            result = BenchmarkResult(
                benchmark_name=benchmark_name,
                start_time=start_time,
                end_time=end_time,
                duration_seconds=duration,
                status="error",
                metrics={},
                success_criteria_met={},
                raw_experiment_results={},
                error_message=str(e)
            )
            
            self.results_history.append(result)
            
            print(f"❌ Benchmark {benchmark_name} failed with error: {e}")
            
            return result
    
    def run_benchmark_suite(self, 
                           benchmark_names: Optional[List[str]] = None,
                           benchmark_type: Optional[BenchmarkType] = None,
                           severity: Optional[BenchmarkSeverity] = None,
                           max_duration_seconds: Optional[float] = None) -> Dict[str, Any]:
        """Run multiple benchmarks as a suite."""
        
        # Determine which benchmarks to run
        if benchmark_names:
            benchmarks_to_run = [self.get_benchmark(name) for name in benchmark_names]
            benchmarks_to_run = [b for b in benchmarks_to_run if b is not None]
        else:
            benchmarks_to_run = self.list_benchmarks(benchmark_type, severity)
        
        if not benchmarks_to_run:
            raise ValueError("No benchmarks to run")
        
        print(f"🧙‍♂️ Starting benchmark suite with {len(benchmarks_to_run)} benchmarks")
        
        suite_start_time = time.time()
        suite_results = {
            "suite_id": f"benchmark_suite_{int(suite_start_time)}",
            "start_time": suite_start_time,
            "benchmarks": {},
            "summary": {}
        }
        
        total_expected_duration = sum(b.expected_duration_seconds for b in benchmarks_to_run)
        print(f"   Expected total duration: {total_expected_duration:.1f}s")
        
        if max_duration_seconds and total_expected_duration > max_duration_seconds:
            print(f"⚠️  Warning: Expected duration ({total_expected_duration:.1f}s) exceeds limit ({max_duration_seconds:.1f}s)")
        
        passed_count = 0
        failed_count = 0
        error_count = 0
        
        for i, benchmark in enumerate(benchmarks_to_run):
            print(f"\n📊 Progress: {i+1}/{len(benchmarks_to_run)}")
            
            # Check time limit
            elapsed_time = time.time() - suite_start_time
            if max_duration_seconds and elapsed_time > max_duration_seconds:
                print(f"⏰ Suite time limit exceeded, stopping at benchmark {i}")
                break
            
            result = self.run_benchmark(benchmark.name)
            suite_results["benchmarks"][benchmark.name] = asdict(result)
            
            if result.status == "passed":
                passed_count += 1
            elif result.status == "failed":
                failed_count += 1
            else:
                error_count += 1
        
        suite_end_time = time.time()
        suite_duration = suite_end_time - suite_start_time
        
        # Generate summary
        suite_results.update({
            "end_time": suite_end_time,
            "duration_seconds": suite_duration,
            "summary": {
                "total_benchmarks": len(benchmarks_to_run),
                "passed": passed_count,
                "failed": failed_count,
                "errors": error_count,
                "success_rate": passed_count / len(benchmarks_to_run) if benchmarks_to_run else 0,
                "overall_status": "passed" if failed_count == 0 and error_count == 0 else "failed"
            }
        })
        
        print(f"\n🎉 Benchmark suite completed!")
        print(f"   Duration: {suite_duration:.1f}s")
        print(f"   Results: {passed_count} passed, {failed_count} failed, {error_count} errors")
        print(f"   Success rate: {suite_results['summary']['success_rate']:.1%}")
        
        # Save suite results
        self._save_suite_results(suite_results)
        
        return suite_results
    
    def create_custom_benchmark(self, 
                               name: str,
                               description: str,
                               benchmark_type: BenchmarkType,
                               manifest_template: Dict[str, Any],
                               success_criteria: Dict[str, Any],
                               severity: BenchmarkSeverity = BenchmarkSeverity.STANDARD,
                               expected_duration_seconds: float = 60.0) -> BenchmarkDefinition:
        """Create a custom benchmark definition."""
        
        benchmark = BenchmarkDefinition(
            name=name,
            description=description,
            benchmark_type=benchmark_type,
            severity=severity,
            expected_duration_seconds=expected_duration_seconds,
            resource_requirements={},  # Will be estimated
            success_criteria=success_criteria,
            manifest_template=manifest_template
        )
        
        self.benchmarks[name] = benchmark
        
        print(f"📝 Created custom benchmark: {name}")
        return benchmark
    
    def compare_benchmark_results(self, 
                                 result1: BenchmarkResult, 
                                 result2: BenchmarkResult) -> Dict[str, Any]:
        """Compare two benchmark results."""
        if result1.benchmark_name != result2.benchmark_name:
            raise ValueError("Cannot compare results from different benchmarks")
        
        comparison = {
            "benchmark_name": result1.benchmark_name,
            "comparison_timestamp": time.time(),
            "result1": {
                "timestamp": result1.start_time,
                "duration": result1.duration_seconds,
                "status": result1.status
            },
            "result2": {
                "timestamp": result2.start_time,
                "duration": result2.duration_seconds,
                "status": result2.status
            },
            "metric_comparisons": {},
            "improvements": [],
            "regressions": []
        }
        
        # Compare metrics
        for metric_name in result1.metrics:
            if metric_name in result2.metrics:
                val1 = result1.metrics[metric_name]
                val2 = result2.metrics[metric_name]
                
                if isinstance(val1, (int, float)) and isinstance(val2, (int, float)):
                    diff = val2 - val1
                    pct_change = (diff / val1 * 100) if val1 != 0 else 0
                    
                    comparison["metric_comparisons"][metric_name] = {
                        "previous": val1,
                        "current": val2,
                        "absolute_diff": diff,
                        "percent_change": pct_change
                    }
                    
                    # Classify as improvement or regression
                    if "throughput" in metric_name.lower() or "success_rate" in metric_name.lower():
                        if pct_change > 5:
                            comparison["improvements"].append(metric_name)
                        elif pct_change < -5:
                            comparison["regressions"].append(metric_name)
                    elif "error" in metric_name.lower() or "latency" in metric_name.lower():
                        if pct_change < -5:
                            comparison["improvements"].append(metric_name)
                        elif pct_change > 5:
                            comparison["regressions"].append(metric_name)
        
        return comparison
    
    def generate_benchmark_report(self, 
                                 results: Union[BenchmarkResult, List[BenchmarkResult]],
                                 output_format: str = "markdown") -> str:
        """Generate a formatted report from benchmark results."""
        
        if isinstance(results, BenchmarkResult):
            results = [results]
        
        if output_format == "markdown":
            return self._generate_markdown_report(results)
        elif output_format == "json":
            return json.dumps([asdict(r) for r in results], indent=2, default=str)
        else:
            raise ValueError(f"Unsupported output format: {output_format}")
    
    def _apply_manifest_overrides(self, manifest_data: Dict[str, Any], overrides: Dict[str, Any]):
        """Apply runtime overrides to manifest data."""
        for key, value in overrides.items():
            if "." in key:
                # Nested key like "processing.batch_size"
                parts = key.split(".")
                current = manifest_data
                for part in parts[:-1]:
                    if part not in current:
                        current[part] = {}
                    current = current[part]
                current[parts[-1]] = value
            else:
                manifest_data[key] = value
    
    def _evaluate_success_criteria(self, 
                                  metrics: Dict[str, Any], 
                                  criteria: Dict[str, Any]) -> Dict[str, bool]:
        """Evaluate whether success criteria are met."""
        results = {}
        
        for criterion, threshold in criteria.items():
            if criterion.startswith("min_"):
                metric_name = criterion[4:]  # Remove "min_" prefix
                if metric_name in metrics:
                    results[criterion] = metrics[metric_name] >= threshold
                else:
                    results[criterion] = False
            elif criterion.startswith("max_"):
                metric_name = criterion[4:]  # Remove "max_" prefix
                if metric_name in metrics:
                    results[criterion] = metrics[metric_name] <= threshold
                else:
                    results[criterion] = False
            else:
                # Direct comparison
                if criterion in metrics:
                    results[criterion] = metrics[criterion] == threshold
                else:
                    results[criterion] = False
        
        return results
    
    def _save_suite_results(self, suite_results: Dict[str, Any]):
        """Save benchmark suite results to file."""
        results_dir = Path("experiments/benchmark_results")
        results_dir.mkdir(parents=True, exist_ok=True)
        
        timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        filename = f"benchmark_suite_{timestamp}.json"
        
        with open(results_dir / filename, 'w') as f:
            json.dump(suite_results, f, indent=2, default=str)
        
        print(f"💾 Suite results saved to: {results_dir / filename}")
    
    def _generate_markdown_report(self, results: List[BenchmarkResult]) -> str:
        """Generate markdown report from benchmark results."""
        report_lines = [
            "# Benchmark Results Report",
            f"Generated at: {datetime.datetime.now().isoformat()}",
            f"Total benchmarks: {len(results)}",
            "",
            "## Summary",
            ""
        ]
        
        passed = len([r for r in results if r.status == "passed"])
        failed = len([r for r in results if r.status == "failed"])
        errors = len([r for r in results if r.status == "error"])
        
        report_lines.extend([
            f"- ✅ Passed: {passed}",
            f"- ❌ Failed: {failed}",
            f"- 🚫 Errors: {errors}",
            f"- Success Rate: {passed/len(results):.1%}",
            "",
            "## Individual Results",
            ""
        ])
        
        for result in results:
            status_emoji = "✅" if result.status == "passed" else "❌" if result.status == "failed" else "🚫"
            
            report_lines.extend([
                f"### {status_emoji} {result.benchmark_name}",
                f"- Duration: {result.duration_seconds:.1f}s",
                f"- Status: {result.status}",
                ""
            ])
            
            if result.metrics:
                report_lines.append("**Metrics:**")
                for metric, value in result.metrics.items():
                    if isinstance(value, (int, float)):
                        report_lines.append(f"- {metric}: {value:.2f}")
                    else:
                        report_lines.append(f"- {metric}: {value}")
                report_lines.append("")
            
            if result.success_criteria_met:
                report_lines.append("**Success Criteria:**")
                for criterion, met in result.success_criteria_met.items():
                    status_text = "✅ Met" if met else "❌ Not met"
                    report_lines.append(f"- {criterion}: {status_text}")
                report_lines.append("")
            
            if result.error_message:
                report_lines.extend([
                    "**Error:**",
                    f"```",
                    result.error_message,
                    f"```",
                    ""
                ])
        
        return "\n".join(report_lines)


# CLI interface and utility functions
def main():
    """CLI interface for benchmark suite."""
    import argparse
    
    parser = argparse.ArgumentParser(description="Benchmark Suite v0.7")
    parser.add_argument("command", choices=["list", "run", "suite", "report"])
    parser.add_argument("--benchmark", "-b", help="Specific benchmark name")
    parser.add_argument("--benchmarks", nargs="+", help="Multiple benchmark names")
    parser.add_argument("--type", choices=[t.value for t in BenchmarkType], help="Filter by benchmark type")
    parser.add_argument("--severity", choices=[s.value for s in BenchmarkSeverity], help="Filter by severity")
    parser.add_argument("--max-duration", type=float, help="Maximum suite duration in seconds")
    parser.add_argument("--output-format", choices=["markdown", "json"], default="markdown", help="Report output format")
    parser.add_argument("--results-file", help="Results file for report generation")
    
    args = parser.parse_args()
    
    suite = BenchmarkSuite()
    
    try:
        if args.command == "list":
            benchmark_type = BenchmarkType(args.type) if args.type else None
            severity = BenchmarkSeverity(args.severity) if args.severity else None
            
            benchmarks = suite.list_benchmarks(benchmark_type, severity)
            
            print(f"Available benchmarks ({len(benchmarks)}):")
            for benchmark in benchmarks:
                print(f"  {benchmark.name}")
                print(f"    Type: {benchmark.benchmark_type.value}")
                print(f"    Severity: {benchmark.severity.value}")
                print(f"    Duration: ~{benchmark.expected_duration_seconds}s")
                print(f"    Description: {benchmark.description}")
                print()
        
        elif args.command == "run":
            if not args.benchmark:
                print("Error: --benchmark required for run command")
                return 1
            
            result = suite.run_benchmark(args.benchmark)
            print(f"\nBenchmark result: {result.status}")
            
        elif args.command == "suite":
            benchmark_names = args.benchmarks
            benchmark_type = BenchmarkType(args.type) if args.type else None
            severity = BenchmarkSeverity(args.severity) if args.severity else None
            
            results = suite.run_benchmark_suite(
                benchmark_names=benchmark_names,
                benchmark_type=benchmark_type,
                severity=severity,
                max_duration_seconds=args.max_duration
            )
            
            print(f"\nSuite completed with {results['summary']['success_rate']:.1%} success rate")
            
        elif args.command == "report":
            if args.results_file:
                with open(args.results_file, 'r') as f:
                    data = json.load(f)
                # Convert back to BenchmarkResult objects if needed
                print("Report generation from file not yet implemented")
            else:
                if suite.results_history:
                    report = suite.generate_benchmark_report(
                        suite.results_history, 
                        args.output_format
                    )
                    print(report)
                else:
                    print("No benchmark results available for report")
        
        return 0
        
    except Exception as e:
        print(f"Error: {e}")
        return 1


if __name__ == "__main__":
    exit(main())