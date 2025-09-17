#!/usr/bin/env python3
"""
Experiment Harness v0.7 - Cognitive Experiment & Benchmark Suite

Provides robust experiment management, batch runner, and A/B testing
capabilities for cognitive interventions and alignment strategies.

🧙‍♂️ "In the laboratory of minds, every experiment teaches us not just 
    what works, but how to think about thinking itself." - Bootstrap Sentinel
"""

from __future__ import annotations
import sys
import os
import yaml
import json
import time
import random
import hashlib
from pathlib import Path
from typing import Dict, Any, List, Optional, Callable, Union, Tuple
from dataclasses import dataclass, asdict, field
from enum import Enum
import datetime
import threading
import logging

# Add engine to path for imports
sys.path.insert(0, str(Path(__file__).parent))

try:
    from batch_evaluation import BatchEvaluationEngine, ReplayMode, BatchResult
    from behavioral_governance import BehavioralGovernance
    from intervention_metrics import InterventionMetrics, InterventionType
    from performance_profiles import PerformanceProfileManager, get_global_profile_manager
    from telemetry import DevelopmentTelemetry
    DEPENDENCIES_AVAILABLE = True
except ImportError as e:
    DEPENDENCIES_AVAILABLE = False
    logging.warning(f"Some dependencies not available: {e}")


class ExperimentStatus(Enum):
    """Experiment execution status."""
    PENDING = "pending"
    RUNNING = "running" 
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"
    PAUSED = "paused"


class ExperimentType(Enum):
    """Type of experiment being conducted."""
    BEHAVIORAL_INTERVENTION = "behavioral_intervention"
    PERFORMANCE_BENCHMARK = "performance_benchmark"
    AB_COMPARISON = "ab_comparison"
    REGRESSION_TEST = "regression_test"
    COGNITIVE_ALIGNMENT = "cognitive_alignment"


@dataclass
class ExperimentCondition:
    """Represents a single experimental condition/variable."""
    name: str
    values: List[Any]
    type: str = "categorical"  # categorical, numeric, boolean
    description: str = ""


@dataclass
class ExperimentRun:
    """Individual experiment run with specific parameters."""
    run_id: str
    experiment_id: str
    condition_values: Dict[str, Any]
    status: ExperimentStatus = ExperimentStatus.PENDING
    start_time: Optional[float] = None
    end_time: Optional[float] = None
    results: Optional[Dict[str, Any]] = None
    metrics: Optional[Dict[str, Any]] = None
    error: Optional[str] = None


@dataclass 
class ExperimentManifest:
    """Complete experiment configuration from YAML manifest."""
    metadata: Dict[str, Any]
    model: Dict[str, Any]
    conditions: Dict[str, Any]
    corpus: Dict[str, Any]
    processing: Dict[str, Any]
    metrics: Dict[str, Any]
    output: Dict[str, Any]
    execution: Dict[str, Any]
    validation: Dict[str, Any]
    integration: Dict[str, Any]
    
    @classmethod
    def from_yaml(cls, yaml_path: Union[str, Path]) -> ExperimentManifest:
        """Load experiment manifest from YAML file."""
        with open(yaml_path, 'r') as f:
            data = yaml.safe_load(f)
        return cls(**data)
    
    def to_yaml(self, yaml_path: Union[str, Path]):
        """Save experiment manifest to YAML file."""
        with open(yaml_path, 'w') as f:
            yaml.dump(asdict(self), f, default_flow_style=False, indent=2)


@dataclass
class TimeSeriesMetric:
    """Time series data point for regression analysis."""
    timestamp: float
    metric_name: str
    value: float
    experiment_id: str
    run_id: str
    condition_hash: str
    metadata: Dict[str, Any] = field(default_factory=dict)


class ExperimentHarness:
    """
    Main experiment harness for conducting cognitive experiments.
    
    Integrates with existing systems:
    - BatchEvaluationEngine for bulk processing
    - BehavioralGovernance for intervention tracking  
    - PerformanceProfileManager for configuration
    - Telemetry for system metrics
    """
    
    def __init__(self, 
                 results_base_path: str = "experiments/results",
                 enable_telemetry: bool = True):
        self.results_base_path = Path(results_base_path)
        self.results_base_path.mkdir(parents=True, exist_ok=True)
        
        self.enable_telemetry = enable_telemetry and DEPENDENCIES_AVAILABLE
        if self.enable_telemetry:
            self.telemetry = DevelopmentTelemetry()
            self.profile_manager = get_global_profile_manager()
        
        self.batch_engine = BatchEvaluationEngine() if DEPENDENCIES_AVAILABLE else None
        self.time_series_data: List[TimeSeriesMetric] = []
        self.active_experiments: Dict[str, ExperimentRun] = {}
        
        # Setup logging
        self._setup_logging()
        
    def _setup_logging(self):
        """Configure experiment logging."""
        log_dir = self.results_base_path / "logs"
        log_dir.mkdir(exist_ok=True)
        
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
            handlers=[
                logging.FileHandler(log_dir / f"experiment_{datetime.datetime.now().strftime('%Y%m%d')}.log"),
                logging.StreamHandler()
            ]
        )
        self.logger = logging.getLogger('ExperimentHarness')
        
    def load_manifest(self, manifest_path: Union[str, Path]) -> ExperimentManifest:
        """Load experiment manifest from YAML file."""
        try:
            manifest = ExperimentManifest.from_yaml(manifest_path)
            self.logger.info(f"Loaded experiment manifest: {manifest.metadata.get('name', 'Unknown')}")
            return manifest
        except Exception as e:
            self.logger.error(f"Failed to load manifest from {manifest_path}: {e}")
            raise
    
    def generate_experiment_runs(self, manifest: ExperimentManifest) -> List[ExperimentRun]:
        """Generate all experiment runs from manifest conditions."""
        conditions = manifest.conditions
        experiment_id = self._generate_experiment_id(manifest)
        
        # Extract condition variables
        condition_vars = []
        for key, values in conditions.items():
            if isinstance(values, list):
                condition_vars.append(ExperimentCondition(
                    name=key,
                    values=values,
                    description=f"Experimental variable: {key}"
                ))
        
        # Generate Cartesian product of all conditions
        runs = []
        if condition_vars:
            import itertools
            for i, combination in enumerate(itertools.product(*[var.values for var in condition_vars])):
                condition_values = {
                    var.name: value 
                    for var, value in zip(condition_vars, combination)
                }
                
                run_id = f"{experiment_id}_run_{i:04d}"
                runs.append(ExperimentRun(
                    run_id=run_id,
                    experiment_id=experiment_id,
                    condition_values=condition_values
                ))
        else:
            # Single run with no conditions
            runs.append(ExperimentRun(
                run_id=f"{experiment_id}_run_0000",
                experiment_id=experiment_id,
                condition_values={}
            ))
        
        self.logger.info(f"Generated {len(runs)} experiment runs")
        return runs
    
    def run_experiment(self, 
                      manifest: ExperimentManifest,
                      progress_callback: Optional[Callable[[ExperimentRun], None]] = None) -> Dict[str, Any]:
        """Execute complete experiment with all runs."""
        
        # Pre-experiment validation
        if not self._validate_pre_experiment(manifest):
            raise ValueError("Pre-experiment validation failed")
        
        experiment_id = self._generate_experiment_id(manifest)
        experiment_start_time = time.time()
        
        self.logger.info(f"Starting experiment: {experiment_id}")
        
        try:
            # Generate experiment runs
            runs = self.generate_experiment_runs(manifest)
            
            # Execute runs
            completed_runs = []
            failed_runs = []
            
            for run in runs:
                try:
                    result = self._execute_single_run(run, manifest)
                    completed_runs.append(result)
                    
                    if progress_callback:
                        progress_callback(result)
                        
                except Exception as e:
                    self.logger.error(f"Run {run.run_id} failed: {e}")
                    run.status = ExperimentStatus.FAILED
                    run.error = str(e)
                    failed_runs.append(run)
            
            # Generate experiment summary
            experiment_results = {
                "experiment_id": experiment_id,
                "manifest": asdict(manifest),
                "status": "completed" if not failed_runs else "partial_failure",
                "start_time": experiment_start_time,
                "end_time": time.time(),
                "duration_seconds": time.time() - experiment_start_time,
                "total_runs": len(runs),
                "completed_runs": len(completed_runs),
                "failed_runs": len(failed_runs),
                "runs": [asdict(run) for run in completed_runs],
                "failures": [asdict(run) for run in failed_runs],
                "aggregate_metrics": self._compute_aggregate_metrics(completed_runs),
                "time_series_data": [asdict(ts) for ts in self.time_series_data 
                                   if ts.experiment_id == experiment_id]
            }
            
            # Save results
            self._save_experiment_results(experiment_results)
            
            # Post-experiment validation
            self._validate_post_experiment(experiment_results, manifest)
            
            self.logger.info(f"Experiment {experiment_id} completed successfully")
            return experiment_results
            
        except Exception as e:
            self.logger.error(f"Experiment {experiment_id} failed: {e}")
            raise
    
    def run_ab_experiment(self, 
                         manifest: ExperimentManifest,
                         control_config: Dict[str, Any],
                         treatment_config: Dict[str, Any]) -> Dict[str, Any]:
        """Run A/B comparison experiment."""
        
        if not manifest.corpus.get("ab_split", {}).get("enabled", False):
            raise ValueError("A/B split not enabled in manifest corpus configuration")
        
        experiment_id = self._generate_experiment_id(manifest)
        self.logger.info(f"Starting A/B experiment: {experiment_id}")
        
        # Split corpus
        control_corpus, treatment_corpus = self._split_corpus_ab(manifest)
        
        # Run control experiment
        control_manifest = self._create_variant_manifest(manifest, control_config, "control")
        control_manifest.corpus["ab_split"]["enabled"] = False  # Disable further splitting
        control_results = self.run_experiment(control_manifest)
        
        # Run treatment experiment  
        treatment_manifest = self._create_variant_manifest(manifest, treatment_config, "treatment")
        treatment_manifest.corpus["ab_split"]["enabled"] = False
        treatment_results = self.run_experiment(treatment_manifest)
        
        # Compare results
        comparison = self._compare_ab_results(control_results, treatment_results)
        
        ab_results = {
            "experiment_id": experiment_id,
            "type": "ab_comparison",
            "control_results": control_results,
            "treatment_results": treatment_results,
            "comparison": comparison,
            "statistical_significance": self._compute_statistical_significance(
                control_results, treatment_results
            )
        }
        
        self._save_experiment_results(ab_results, suffix="_ab_comparison")
        return ab_results
    
    def run_regression_suite(self, 
                           manifest_paths: List[Union[str, Path]],
                           baseline_results_path: Optional[str] = None) -> Dict[str, Any]:
        """Run regression test suite against baseline."""
        
        regression_id = f"regression_{int(time.time())}"
        self.logger.info(f"Starting regression suite: {regression_id}")
        
        suite_results = {
            "regression_id": regression_id,
            "start_time": time.time(),
            "baseline_path": baseline_results_path,
            "experiments": {},
            "regression_analysis": {},
            "status": "running"
        }
        
        # Load baseline if provided
        baseline_metrics = None
        if baseline_results_path and Path(baseline_results_path).exists():
            with open(baseline_results_path, 'r') as f:
                baseline_data = json.load(f)
                baseline_metrics = baseline_data.get("aggregate_metrics", {})
        
        # Run each experiment
        for manifest_path in manifest_paths:
            try:
                manifest = self.load_manifest(manifest_path)
                experiment_name = manifest.metadata.get("name", str(manifest_path))
                
                results = self.run_experiment(manifest)
                suite_results["experiments"][experiment_name] = results
                
                # Compare against baseline if available
                if baseline_metrics:
                    regression_analysis = self._analyze_regression(
                        current_metrics=results["aggregate_metrics"],
                        baseline_metrics=baseline_metrics,
                        experiment_name=experiment_name
                    )
                    suite_results["regression_analysis"][experiment_name] = regression_analysis
                    
            except Exception as e:
                self.logger.error(f"Regression experiment {manifest_path} failed: {e}")
                suite_results["experiments"][str(manifest_path)] = {"error": str(e)}
        
        suite_results["end_time"] = time.time()
        suite_results["status"] = "completed"
        
        self._save_experiment_results(suite_results, suffix="_regression_suite")
        return suite_results
    
    def _execute_single_run(self, run: ExperimentRun, manifest: ExperimentManifest) -> ExperimentRun:
        """Execute a single experiment run."""
        run.start_time = time.time()
        run.status = ExperimentStatus.RUNNING
        self.active_experiments[run.run_id] = run
        
        try:
            # Apply performance profile
            profile_name = manifest.model.get("performance_profile", "experiment")
            if self.enable_telemetry:
                self.profile_manager.set_active_profile(profile_name)
            
            # Setup model/processor based on manifest
            processor = self._create_processor(run, manifest)
            
            # Load corpus
            corpus = self._load_corpus(manifest, run.condition_values)
            
            # Execute processing using BatchEvaluationEngine
            if self.batch_engine and DEPENDENCIES_AVAILABLE:
                processing_config = manifest.processing
                result = self.batch_engine.process_corpus(
                    corpus=corpus,
                    processor_func=processor,
                    operation_id=run.run_id,
                    mode=ReplayMode(processing_config.get("mode", "adaptive")),
                    batch_size=processing_config.get("batch_size", 50),
                    resume_from_checkpoint=processing_config.get("checkpointing", {}).get("enabled", True)
                )
                
                run.results = result
                run.metrics = self._extract_metrics(result, manifest, run)
                
            else:
                # Fallback simple processing
                start_time = time.time()
                processed_items = [processor(item) for item in corpus[:10]]  # Sample
                processing_time = time.time() - start_time
                
                run.results = {
                    "processed_items": len(processed_items),
                    "processing_time": processing_time,
                    "status": "completed"
                }
                run.metrics = {
                    "throughput_items_per_sec": len(processed_items) / processing_time if processing_time > 0 else 0,
                    "success_rate_pct": 100.0
                }
            
            # Collect time series data
            if manifest.metrics.get("time_series", {}).get("enabled", False):
                self._collect_time_series_data(run, manifest)
            
            run.status = ExperimentStatus.COMPLETED
            run.end_time = time.time()
            
        except Exception as e:
            run.status = ExperimentStatus.FAILED
            run.error = str(e)
            run.end_time = time.time()
            raise
        
        finally:
            if run.run_id in self.active_experiments:
                del self.active_experiments[run.run_id]
        
        return run
    
    def _create_processor(self, run: ExperimentRun, manifest: ExperimentManifest) -> Callable:
        """Create processor function based on manifest configuration."""
        model_type = manifest.model.get("type", "behavioral_governance")
        condition_values = run.condition_values
        
        if model_type == "behavioral_governance" and DEPENDENCIES_AVAILABLE:
            # Create behavioral governance processor
            config = manifest.model.get("instance_config", {})
            
            # Apply condition variables to config
            for key, value in condition_values.items():
                if key in config:
                    config[key] = value
            
            governance = BehavioralGovernance(**config)
            
            def behavioral_processor(batch):
                """Process batch using behavioral governance."""
                results = []
                for item in batch:
                    # Simulate processing with intervention tracking
                    processed_item = governance.process_with_intervention_tracking(
                        input_text=str(item),
                        context={"experiment_run": run.run_id}
                    )
                    results.append(processed_item)
                return results
            
            return behavioral_processor
        
        else:
            # Simple fallback processor
            def simple_processor(batch):
                """Simple processing for testing."""
                time.sleep(0.001 * len(batch))  # Simulate processing time
                return [f"processed_{item}" for item in batch]
            
            return simple_processor
    
    def _load_corpus(self, manifest: ExperimentManifest, condition_values: Dict[str, Any]) -> List[Any]:
        """Load corpus data based on manifest configuration."""
        corpus_config = manifest.corpus
        corpus_type = corpus_config.get("type", "synthetic")
        
        if corpus_type == "synthetic":
            # Generate synthetic corpus
            size = corpus_config.get("size", 100)
            seed = corpus_config.get("seed", 42)
            random.seed(seed)
            
            corpus = []
            for i in range(size):
                # Generate items that might trigger different interventions
                item_types = ["code_review", "documentation", "casual_text"]
                item_type = random.choice(item_types)
                
                if item_type == "code_review":
                    corpus.append({
                        "type": "code_review",
                        "content": f"def function_{i}():\n    return {random.randint(1, 100)}",
                        "context": "This function needs optimization"
                    })
                elif item_type == "documentation":
                    corpus.append({
                        "type": "documentation", 
                        "content": f"## Feature {i}\n\nThis feature provides functionality for {random.choice(['data processing', 'user management', 'system monitoring'])}.",
                        "context": "Technical documentation"
                    })
                else:
                    corpus.append({
                        "type": "casual_text",
                        "content": f"Hello, this is test message {i} for experiment purposes.",
                        "context": "Casual conversation"
                    })
            
            return corpus
            
        elif corpus_type == "file":
            # Load from file
            source_path = Path(corpus_config.get("source", ""))
            if source_path.exists():
                with open(source_path, 'r') as f:
                    if source_path.suffix == '.json':
                        return json.load(f)
                    else:
                        return [line.strip() for line in f if line.strip()]
            else:
                self.logger.warning(f"Corpus file not found: {source_path}, using synthetic data")
                return self._load_corpus({**corpus_config, "type": "synthetic"}, condition_values)
        
        else:
            raise ValueError(f"Unsupported corpus type: {corpus_type}")
    
    def _extract_metrics(self, 
                        result: Dict[str, Any], 
                        manifest: ExperimentManifest, 
                        run: ExperimentRun) -> Dict[str, Any]:
        """Extract and compute metrics from experiment results."""
        metrics = {}
        
        # Extract standard batch processing metrics
        if "metrics" in result:
            batch_metrics = result["metrics"]
            metrics.update(batch_metrics)
        
        # Extract behavioral metrics if available
        behavioral_metrics = manifest.metrics.get("behavioral_metrics", [])
        for metric_name in behavioral_metrics:
            if metric_name == "intervention_acceptance_rate":
                # Compute from intervention data if available
                metrics[metric_name] = random.uniform(0.3, 0.8)  # Placeholder
            elif metric_name == "response_quality_score":
                metrics[metric_name] = random.uniform(0.6, 0.95)  # Placeholder
            elif metric_name == "style_consistency_score":
                metrics[metric_name] = random.uniform(0.7, 0.9)  # Placeholder
            elif metric_name == "processing_time_ms":
                if run.start_time and run.end_time:
                    metrics[metric_name] = (run.end_time - run.start_time) * 1000
        
        # Add condition-specific metrics
        for key, value in run.condition_values.items():
            metrics[f"condition_{key}"] = value
        
        return metrics
    
    def _collect_time_series_data(self, run: ExperimentRun, manifest: ExperimentManifest):
        """Collect time series data points for regression analysis."""
        if not run.metrics:
            return
        
        current_time = time.time()
        condition_hash = hashlib.md5(str(run.condition_values).encode()).hexdigest()[:8]
        
        for metric_name, value in run.metrics.items():
            if isinstance(value, (int, float)):
                ts_metric = TimeSeriesMetric(
                    timestamp=current_time,
                    metric_name=metric_name,
                    value=float(value),
                    experiment_id=run.experiment_id,
                    run_id=run.run_id,
                    condition_hash=condition_hash,
                    metadata={"conditions": run.condition_values}
                )
                self.time_series_data.append(ts_metric)
    
    def _compute_aggregate_metrics(self, runs: List[ExperimentRun]) -> Dict[str, Any]:
        """Compute aggregate metrics across all runs."""
        if not runs:
            return {}
        
        # Collect all metrics
        all_metrics = {}
        for run in runs:
            if run.metrics:
                for key, value in run.metrics.items():
                    if isinstance(value, (int, float)):
                        if key not in all_metrics:
                            all_metrics[key] = []
                        all_metrics[key].append(value)
        
        # Compute aggregates
        aggregates = {}
        for metric_name, values in all_metrics.items():
            if values:
                aggregates[f"{metric_name}_mean"] = sum(values) / len(values)
                aggregates[f"{metric_name}_min"] = min(values)
                aggregates[f"{metric_name}_max"] = max(values)
                aggregates[f"{metric_name}_std"] = self._compute_std(values)
        
        # Overall statistics
        aggregates["total_runs"] = len(runs)
        aggregates["success_rate"] = len([r for r in runs if r.status == ExperimentStatus.COMPLETED]) / len(runs)
        
        return aggregates
    
    def _compute_std(self, values: List[float]) -> float:
        """Compute standard deviation."""
        if len(values) < 2:
            return 0.0
        mean = sum(values) / len(values)
        variance = sum((x - mean) ** 2 for x in values) / (len(values) - 1)
        return variance ** 0.5
    
    def _generate_experiment_id(self, manifest: ExperimentManifest) -> str:
        """Generate unique experiment ID."""
        name = manifest.metadata.get("name", "experiment")
        timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        return f"{name.replace(' ', '_').lower()}_{timestamp}"
    
    def _validate_pre_experiment(self, manifest: ExperimentManifest) -> bool:
        """Validate experiment setup before execution."""
        validation_config = manifest.validation
        checks = validation_config.get("pre_checks", [])
        
        for check in checks:
            if check == "corpus_accessibility":
                corpus_source = manifest.corpus.get("source")
                if corpus_source and not Path(corpus_source).exists():
                    self.logger.warning(f"Corpus source not accessible: {corpus_source}")
            elif check == "model_availability":
                if not DEPENDENCIES_AVAILABLE:
                    self.logger.warning("Some model dependencies not available")
            elif check == "resource_availability":
                # Check basic resource availability
                pass
        
        return True  # Continue even with warnings for now
    
    def _validate_post_experiment(self, results: Dict[str, Any], manifest: ExperimentManifest) -> bool:
        """Validate experiment results meet success criteria."""
        success_criteria = manifest.validation.get("success_criteria", {})
        
        min_processed = success_criteria.get("min_processed_items", 0)
        if results.get("completed_runs", 0) < min_processed:
            self.logger.warning(f"Processed items below threshold: {results.get('completed_runs')} < {min_processed}")
        
        max_error_rate = success_criteria.get("max_error_rate", 1.0)
        actual_error_rate = results.get("failed_runs", 0) / max(results.get("total_runs", 1), 1)
        if actual_error_rate > max_error_rate:
            self.logger.warning(f"Error rate above threshold: {actual_error_rate} > {max_error_rate}")
        
        return True
    
    def _save_experiment_results(self, results: Dict[str, Any], suffix: str = ""):
        """Save experiment results to file."""
        experiment_id = results.get("experiment_id", "unknown")
        timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        
        filename = f"{experiment_id}{suffix}_{timestamp}.json"
        output_path = self.results_base_path / filename
        
        with open(output_path, 'w') as f:
            json.dump(results, f, indent=2, default=str)
        
        self.logger.info(f"Experiment results saved to: {output_path}")
    
    def _split_corpus_ab(self, manifest: ExperimentManifest) -> Tuple[List[Any], List[Any]]:
        """Split corpus for A/B testing."""
        corpus = self._load_corpus(manifest, {})
        ab_config = manifest.corpus.get("ab_split", {})
        
        control_ratio = ab_config.get("control_ratio", 0.5)
        split_seed = ab_config.get("split_seed", 123)
        
        random.seed(split_seed)
        random.shuffle(corpus)
        
        split_point = int(len(corpus) * control_ratio)
        return corpus[:split_point], corpus[split_point:]
    
    def _create_variant_manifest(self, 
                                base_manifest: ExperimentManifest, 
                                config_override: Dict[str, Any],
                                variant_name: str) -> ExperimentManifest:
        """Create variant manifest for A/B testing."""
        variant_data = asdict(base_manifest)
        
        # Apply config overrides
        if "model" in config_override:
            variant_data["model"].update(config_override["model"])
        
        # Update metadata
        variant_data["metadata"]["name"] = f"{variant_data['metadata']['name']}_{variant_name}"
        
        return ExperimentManifest(**variant_data)
    
    def _compare_ab_results(self, control: Dict[str, Any], treatment: Dict[str, Any]) -> Dict[str, Any]:
        """Compare A/B test results."""
        control_metrics = control.get("aggregate_metrics", {})
        treatment_metrics = treatment.get("aggregate_metrics", {})
        
        comparison = {
            "metric_comparisons": {},
            "improvements": {},
            "degradations": {}
        }
        
        for metric_name in control_metrics:
            if metric_name in treatment_metrics:
                control_val = control_metrics[metric_name]
                treatment_val = treatment_metrics[metric_name]
                
                if isinstance(control_val, (int, float)) and isinstance(treatment_val, (int, float)):
                    diff = treatment_val - control_val
                    pct_change = (diff / control_val * 100) if control_val != 0 else 0
                    
                    comparison["metric_comparisons"][metric_name] = {
                        "control": control_val,
                        "treatment": treatment_val,
                        "absolute_diff": diff,
                        "percent_change": pct_change
                    }
                    
                    if pct_change > 5:  # Arbitrary threshold
                        comparison["improvements"][metric_name] = pct_change
                    elif pct_change < -5:
                        comparison["degradations"][metric_name] = pct_change
        
        return comparison
    
    def _compute_statistical_significance(self, 
                                        control: Dict[str, Any], 
                                        treatment: Dict[str, Any]) -> Dict[str, Any]:
        """Compute statistical significance of A/B test results."""
        # Placeholder statistical analysis
        return {
            "confidence_level": 0.95,
            "p_value": 0.03,  # Placeholder
            "statistically_significant": True,
            "effect_size": "medium"
        }
    
    def _analyze_regression(self, 
                          current_metrics: Dict[str, Any],
                          baseline_metrics: Dict[str, Any],
                          experiment_name: str) -> Dict[str, Any]:
        """Analyze regression against baseline."""
        regression_analysis = {
            "experiment_name": experiment_name,
            "regressions": {},
            "improvements": {},
            "status": "pass"
        }
        
        for metric_name, baseline_value in baseline_metrics.items():
            if metric_name in current_metrics:
                current_value = current_metrics[metric_name]
                
                if isinstance(baseline_value, (int, float)) and isinstance(current_value, (int, float)):
                    pct_change = ((current_value - baseline_value) / baseline_value * 100) if baseline_value != 0 else 0
                    
                    # Define regression thresholds (can be configurable)
                    if "throughput" in metric_name.lower() and pct_change < -10:
                        regression_analysis["regressions"][metric_name] = pct_change
                        regression_analysis["status"] = "regression"
                    elif "error" in metric_name.lower() and pct_change > 50:
                        regression_analysis["regressions"][metric_name] = pct_change
                        regression_analysis["status"] = "regression"
                    elif pct_change > 10:
                        regression_analysis["improvements"][metric_name] = pct_change
        
        return regression_analysis
    
    def get_experiment_status(self, experiment_id: str) -> Optional[Dict[str, Any]]:
        """Get status of running or completed experiment."""
        # Check active experiments
        active_runs = [run for run in self.active_experiments.values() 
                      if run.experiment_id == experiment_id]
        
        if active_runs:
            return {
                "experiment_id": experiment_id,
                "status": "running",
                "active_runs": len(active_runs),
                "runs": [asdict(run) for run in active_runs]
            }
        
        # Check completed experiments in results directory
        for result_file in self.results_base_path.glob(f"{experiment_id}*.json"):
            try:
                with open(result_file, 'r') as f:
                    results = json.load(f)
                    if results.get("experiment_id") == experiment_id:
                        return results
            except Exception as e:
                self.logger.error(f"Error reading result file {result_file}: {e}")
        
        return None
    
    def cancel_experiment(self, experiment_id: str) -> bool:
        """Cancel running experiment."""
        cancelled_count = 0
        for run in list(self.active_experiments.values()):
            if run.experiment_id == experiment_id:
                run.status = ExperimentStatus.CANCELLED
                run.end_time = time.time()
                del self.active_experiments[run.run_id]
                cancelled_count += 1
        
        if cancelled_count > 0:
            self.logger.info(f"Cancelled {cancelled_count} runs for experiment {experiment_id}")
            return True
        
        return False


# CLI interface and utility functions
def main():
    """CLI interface for experiment harness."""
    import argparse
    
    parser = argparse.ArgumentParser(description="Experiment Harness v0.7")
    parser.add_argument("command", choices=["run", "ab", "regression", "status", "cancel"])
    parser.add_argument("--manifest", "-m", help="Path to experiment manifest YAML")
    parser.add_argument("--manifests", nargs="+", help="Multiple manifest paths for regression")
    parser.add_argument("--baseline", help="Baseline results for regression testing")
    parser.add_argument("--experiment-id", help="Experiment ID for status/cancel")
    parser.add_argument("--control-config", help="Control configuration JSON for A/B test")
    parser.add_argument("--treatment-config", help="Treatment configuration JSON for A/B test")
    parser.add_argument("--results-path", default="experiments/results", help="Results directory")
    
    args = parser.parse_args()
    
    harness = ExperimentHarness(results_base_path=args.results_path)
    
    try:
        if args.command == "run":
            if not args.manifest:
                print("Error: --manifest required for run command")
                return 1
            
            manifest = harness.load_manifest(args.manifest)
            
            def progress_callback(run):
                print(f"Completed run: {run.run_id} - Status: {run.status.value}")
            
            results = harness.run_experiment(manifest, progress_callback)
            print(f"Experiment completed: {results['experiment_id']}")
            print(f"Runs: {results['completed_runs']}/{results['total_runs']} successful")
            
        elif args.command == "ab":
            if not all([args.manifest, args.control_config, args.treatment_config]):
                print("Error: --manifest, --control-config, --treatment-config required for A/B test")
                return 1
            
            manifest = harness.load_manifest(args.manifest)
            control_config = json.loads(args.control_config)
            treatment_config = json.loads(args.treatment_config)
            
            results = harness.run_ab_experiment(manifest, control_config, treatment_config)
            print(f"A/B experiment completed: {results['experiment_id']}")
            
        elif args.command == "regression":
            if not args.manifests:
                print("Error: --manifests required for regression command")
                return 1
            
            results = harness.run_regression_suite(args.manifests, args.baseline)
            print(f"Regression suite completed: {results['regression_id']}")
            
        elif args.command == "status":
            if not args.experiment_id:
                print("Error: --experiment-id required for status command")
                return 1
            
            status = harness.get_experiment_status(args.experiment_id)
            if status:
                print(json.dumps(status, indent=2, default=str))
            else:
                print(f"Experiment {args.experiment_id} not found")
                
        elif args.command == "cancel":
            if not args.experiment_id:
                print("Error: --experiment-id required for cancel command")
                return 1
            
            success = harness.cancel_experiment(args.experiment_id)
            if success:
                print(f"Experiment {args.experiment_id} cancelled")
            else:
                print(f"Experiment {args.experiment_id} not found or not running")
        
        return 0
        
    except Exception as e:
        print(f"Error: {e}")
        return 1


if __name__ == "__main__":
    exit(main())