#!/usr/bin/env python3
"""
Baseline Set Validation Script for Alchemist Faculty

Validates baseline_set.json files against the Alchemist Faculty schema.
Ensures baseline measurements meet quality and completeness requirements.

Usage:
    python validate_baseline_set.py --file baseline_set.json
    python validate_baseline_set.py --directory gu_pot/issue-123/ --pattern "baseline*.json"
    python validate_baseline_set.py --directory gu_pot/ --recursive
"""

import argparse
import json
import sys
from pathlib import Path
from typing import Dict, List, Tuple, Any, Optional
import jsonschema
from datetime import datetime

# Version information
SCRIPT_VERSION = "0.1.0"
ALCHEMIST_VERSION = "0.1.0"

class BaselineValidator:
    """Validates baseline_set.json files against schema and quality requirements"""
    
    def __init__(self, schema_path: Optional[str] = None):
        self.schema_path = schema_path or self._find_schema_file()
        self.schema = self._load_schema()
        self.validation_results = []
    
    def _find_schema_file(self) -> str:
        """Find the baseline_set.json schema file"""
        # Look for schema in common locations
        possible_paths = [
            "schemas/alchemist/baseline_set.json",
            "../schemas/alchemist/baseline_set.json",
            "../../schemas/alchemist/baseline_set.json",
            Path(__file__).parent.parent.parent / "schemas" / "alchemist" / "baseline_set.json"
        ]
        
        for path in possible_paths:
            if Path(path).exists():
                return str(path)
        
        raise FileNotFoundError("Could not find baseline_set.json schema file")
    
    def _load_schema(self) -> Dict[str, Any]:
        """Load and parse the JSON schema"""
        try:
            with open(self.schema_path, 'r') as f:
                return json.load(f)
        except Exception as e:
            raise Exception(f"Failed to load schema from {self.schema_path}: {e}")
    
    def validate_file(self, file_path: str) -> Tuple[bool, List[str]]:
        """Validate a single baseline file"""
        try:
            with open(file_path, 'r') as f:
                data = json.load(f)
        except json.JSONDecodeError as e:
            return False, [f"Invalid JSON: {e}"]
        except FileNotFoundError:
            return False, [f"File not found: {file_path}"]
        except Exception as e:
            return False, [f"Error reading file: {e}"]
        
        return self.validate_data(data, file_path)
    
    def validate_data(self, data: Dict[str, Any], source: str = "data") -> Tuple[bool, List[str]]:
        """Validate baseline data against schema and quality requirements"""
        errors = []
        warnings = []
        
        # Schema validation
        try:
            jsonschema.validate(data, self.schema)
        except jsonschema.ValidationError as e:
            errors.append(f"Schema validation failed: {e.message}")
            # Continue with quality checks even if schema fails
        except Exception as e:
            errors.append(f"Schema validation error: {e}")
            return False, errors
        
        # Quality validation checks
        quality_errors, quality_warnings = self._validate_quality(data)
        errors.extend(quality_errors)
        warnings.extend(quality_warnings)
        
        # Store result
        result = {
            "source": source,
            "valid": len(errors) == 0,
            "errors": errors,
            "warnings": warnings,
            "timestamp": datetime.now().isoformat()
        }
        self.validation_results.append(result)
        
        return len(errors) == 0, errors + warnings
    
    def _validate_quality(self, data: Dict[str, Any]) -> Tuple[List[str], List[str]]:
        """Perform quality validation checks beyond schema"""
        errors = []
        warnings = []
        
        # Check baseline ID format
        baseline_id = data.get("baseline_id", "")
        if not baseline_id.startswith("baseline_"):
            errors.append("baseline_id should start with 'baseline_'")
        
        # Check experiment ID format
        experiment_id = data.get("experiment_id", "")
        if not experiment_id.startswith("exp_"):
            errors.append("experiment_id should start with 'exp_'")
        
        # Validate timestamp
        try:
            datetime.fromisoformat(data.get("capture_timestamp", "").replace('Z', '+00:00'))
        except ValueError:
            errors.append("Invalid capture_timestamp format")
        
        # Check metrics completeness
        metrics = data.get("metrics", {})
        if not self._check_metrics_completeness(metrics):
            warnings.append("Some important metrics are missing")
        
        # Validate performance metrics reasonableness
        perf_errors, perf_warnings = self._validate_performance_metrics(metrics.get("performance", {}))
        errors.extend(perf_errors)
        warnings.extend(perf_warnings)
        
        # Validate behavioral metrics
        behav_errors, behav_warnings = self._validate_behavioral_metrics(metrics.get("behavioral", {}))
        errors.extend(behav_errors)
        warnings.extend(behav_warnings)
        
        # Check validation section if present
        if "validation" in data:
            val_errors, val_warnings = self._validate_validation_section(data["validation"])
            errors.extend(val_errors)
            warnings.extend(val_warnings)
        
        return errors, warnings
    
    def _check_metrics_completeness(self, metrics: Dict[str, Any]) -> bool:
        """Check if all important metrics are present"""
        required_sections = ["performance", "behavioral"]
        for section in required_sections:
            if section not in metrics:
                return False
        
        # Check performance subsections
        performance = metrics.get("performance", {})
        perf_required = ["throughput", "latency", "resource_usage"]
        for subsection in perf_required:
            if subsection not in performance:
                return False
        
        # Check behavioral subsections
        behavioral = metrics.get("behavioral", {})
        behav_required = ["user_engagement", "system_stability"]
        for subsection in behav_required:
            if subsection not in behavioral:
                return False
        
        return True
    
    def _validate_performance_metrics(self, performance: Dict[str, Any]) -> Tuple[List[str], List[str]]:
        """Validate performance metrics for reasonableness"""
        errors = []
        warnings = []
        
        # Validate throughput
        throughput = performance.get("throughput", {})
        if "value" in throughput:
            value = throughput["value"]
            if value < 0:
                errors.append("Throughput value cannot be negative")
            elif value == 0:
                warnings.append("Throughput value is zero - system may be idle")
            elif value > 100000:  # Very high throughput
                warnings.append("Very high throughput detected - verify measurement accuracy")
        
        # Validate latency
        latency = performance.get("latency", {})
        if "mean_ms" in latency:
            mean_ms = latency["mean_ms"]
            if mean_ms < 0:
                errors.append("Latency cannot be negative")
            elif mean_ms > 10000:  # > 10 seconds
                warnings.append("Very high latency detected - may indicate performance issues")
        
        # Check latency percentiles ordering
        percentiles = latency.get("percentiles", {})
        p_values = []
        for p in ["p50", "p90", "p95", "p99", "p99_9"]:
            if p in percentiles:
                p_values.append((p, percentiles[p]))
        
        p_values.sort(key=lambda x: float(x[0].replace('p', '').replace('_', '.')))
        for i in range(1, len(p_values)):
            if p_values[i][1] < p_values[i-1][1]:
                errors.append(f"Latency percentiles not in ascending order: {p_values[i-1][0]} > {p_values[i][0]}")
        
        # Validate resource usage
        resource_usage = performance.get("resource_usage", {})
        if "cpu_percent" in resource_usage:
            cpu = resource_usage["cpu_percent"]
            if cpu < 0 or cpu > 100:
                errors.append("CPU usage must be between 0 and 100 percent")
            elif cpu > 90:
                warnings.append("Very high CPU usage detected")
        
        if "memory_mb" in resource_usage:
            memory = resource_usage["memory_mb"]
            if memory < 0:
                errors.append("Memory usage cannot be negative")
            elif memory > 32000:  # > 32GB
                warnings.append("Very high memory usage detected")
        
        return errors, warnings
    
    def _validate_behavioral_metrics(self, behavioral: Dict[str, Any]) -> Tuple[List[str], List[str]]:
        """Validate behavioral metrics for reasonableness"""
        errors = []
        warnings = []
        
        # Validate user engagement
        engagement = behavioral.get("user_engagement", {})
        if "task_completion_rate" in engagement:
            rate = engagement["task_completion_rate"]
            if rate < 0 or rate > 1:
                errors.append("Task completion rate must be between 0 and 1")
            elif rate < 0.5:
                warnings.append("Low task completion rate detected")
        
        if "error_rate" in engagement:
            rate = engagement["error_rate"]
            if rate < 0 or rate > 1:
                errors.append("Error rate must be between 0 and 1")
            elif rate > 0.1:
                warnings.append("High error rate detected")
        
        if "satisfaction_score" in engagement:
            score = engagement["satisfaction_score"]
            if score < 1 or score > 10:
                errors.append("Satisfaction score must be between 1 and 10")
            elif score < 5:
                warnings.append("Low satisfaction score detected")
        
        # Validate system stability
        stability = behavioral.get("system_stability", {})
        if "uptime_percent" in stability:
            uptime = stability["uptime_percent"]
            if uptime < 0 or uptime > 100:
                errors.append("Uptime percentage must be between 0 and 100")
            elif uptime < 95:
                warnings.append("Low system uptime detected")
        
        if "error_count" in stability:
            count = stability["error_count"]
            if count < 0:
                errors.append("Error count cannot be negative")
            elif count > 100:
                warnings.append("High error count detected")
        
        return errors, warnings
    
    def _validate_validation_section(self, validation: Dict[str, Any]) -> Tuple[List[str], List[str]]:
        """Validate the validation section metadata"""
        errors = []
        warnings = []
        
        quality_score = validation.get("quality_score")
        if quality_score is not None:
            if quality_score < 0 or quality_score > 1:
                errors.append("Quality score must be between 0 and 1")
            elif quality_score < 0.7:
                warnings.append("Low quality score detected")
        
        is_valid = validation.get("is_valid")
        if is_valid is False:
            warnings.append("Baseline marked as invalid in validation section")
        
        # Check for critical anomalies
        anomalies = validation.get("anomalies_detected", [])
        for anomaly in anomalies:
            if anomaly.get("severity") == "critical":
                warnings.append(f"Critical anomaly detected: {anomaly.get('description', 'Unknown')}")
        
        return errors, warnings
    
    def get_validation_summary(self) -> Dict[str, Any]:
        """Get summary of all validation results"""
        total_files = len(self.validation_results)
        valid_files = sum(1 for r in self.validation_results if r["valid"])
        
        all_errors = []
        all_warnings = []
        
        for result in self.validation_results:
            all_errors.extend(result["errors"])
            all_warnings.extend(result["warnings"])
        
        return {
            "total_files": total_files,
            "valid_files": valid_files,
            "invalid_files": total_files - valid_files,
            "total_errors": len(all_errors),
            "total_warnings": len(all_warnings),
            "success_rate": valid_files / total_files if total_files > 0 else 0,
            "results": self.validation_results
        }

def find_baseline_files(directory: str, pattern: str = "*.json", recursive: bool = False) -> List[str]:
    """Find baseline files in directory"""
    dir_path = Path(directory)
    
    if not dir_path.exists():
        return []
    
    if recursive:
        return [str(f) for f in dir_path.rglob(pattern) if "baseline" in f.name.lower()]
    else:
        return [str(f) for f in dir_path.glob(pattern) if "baseline" in f.name.lower()]

def main():
    parser = argparse.ArgumentParser(
        description="Validate Alchemist Faculty baseline_set.json files",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Validate single file
  python validate_baseline_set.py --file gu_pot/issue-123/baseline_set.json

  # Validate all baseline files in directory
  python validate_baseline_set.py --directory gu_pot/issue-123/ --pattern "baseline*.json"

  # Recursive validation
  python validate_baseline_set.py --directory gu_pot/ --recursive

  # Use custom schema
  python validate_baseline_set.py --file baseline.json --schema custom_schema.json
        """
    )
    
    # Input options
    input_group = parser.add_mutually_exclusive_group(required=True)
    input_group.add_argument('--file', type=str,
                            help='Path to single baseline file to validate')
    input_group.add_argument('--directory', type=str,
                            help='Directory containing baseline files')
    
    # Directory options
    parser.add_argument('--pattern', type=str, default="*.json",
                       help='File pattern for directory mode (default: *.json)')
    parser.add_argument('--recursive', action='store_true',
                       help='Search directory recursively')
    
    # Validation options
    parser.add_argument('--schema', type=str,
                       help='Path to custom baseline schema file')
    parser.add_argument('--strict', action='store_true',
                       help='Treat warnings as errors')
    parser.add_argument('--summary-only', action='store_true',
                       help='Show only summary, not individual file results')
    
    # Output options
    parser.add_argument('--verbose', '-v', action='store_true',
                       help='Verbose output')
    parser.add_argument('--json-output', action='store_true',
                       help='Output results as JSON')
    parser.add_argument('--version', action='version', 
                       version=f'%(prog)s {SCRIPT_VERSION} (Alchemist Faculty {ALCHEMIST_VERSION})')
    
    args = parser.parse_args()
    
    # Initialize validator
    try:
        validator = BaselineValidator(args.schema)
        if args.verbose:
            print(f"Using schema: {validator.schema_path}")
    except Exception as e:
        print(f"❌ Error initializing validator: {e}")
        sys.exit(1)
    
    # Collect files to validate
    files_to_validate = []
    
    if args.file:
        files_to_validate = [args.file]
    else:
        files_to_validate = find_baseline_files(args.directory, args.pattern, args.recursive)
        
        if not files_to_validate:
            print(f"❌ No baseline files found in {args.directory} with pattern {args.pattern}")
            sys.exit(1)
        
        if args.verbose:
            print(f"Found {len(files_to_validate)} baseline files to validate")
    
    # Validate files
    overall_success = True
    
    for file_path in files_to_validate:
        if not args.summary_only:
            print(f"📋 Validating {file_path}...")
        
        is_valid, messages = validator.validate_file(file_path)
        
        if not is_valid:
            overall_success = False
        
        if not args.summary_only:
            if is_valid:
                print(f"✅ {file_path} - Valid")
            else:
                print(f"❌ {file_path} - Invalid")
            
            # Show messages
            if messages and (args.verbose or not is_valid):
                for message in messages:
                    if "warning" in message.lower():
                        print(f"  ⚠️  {message}")
                        if args.strict:
                            overall_success = False
                    else:
                        print(f"  ❌ {message}")
    
    # Show summary
    summary = validator.get_validation_summary()
    
    if args.json_output:
        print(json.dumps(summary, indent=2))
    else:
        print("\n" + "=" * 60)
        print("📊 Validation Summary")
        print("=" * 60)
        print(f"Total files: {summary['total_files']}")
        print(f"Valid files: {summary['valid_files']}")
        print(f"Invalid files: {summary['invalid_files']}")
        print(f"Success rate: {summary['success_rate']:.1%}")
        print(f"Total errors: {summary['total_errors']}")
        print(f"Total warnings: {summary['total_warnings']}")
        
        if overall_success:
            print("\n✅ All validations passed!")
        else:
            print("\n❌ Some validations failed!")
    
    sys.exit(0 if overall_success else 1)

if __name__ == '__main__':
    main()