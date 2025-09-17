#!/usr/bin/env python3
"""
🧙‍♂️ Alchemist Baseline Set Validator v1.0

Validates baseline_set.json files used in Alchemist experiments against
the defined JSON schema. Ensures structural integrity, data consistency,
and adherence to experimental baseline standards.

Features:
- JSON schema validation against baseline_set.json schema
- Semantic validation of metric definitions and thresholds
- Consistency checks between primary/secondary/derived metrics
- Provenance validation for experimental lineage
- Integration with CI/CD pipelines for automated validation

🧙‍♂️ "A baseline without validation is merely a suggestion masquerading as truth." 
    - Bootstrap Sentinel
"""

import json
import sys
import argparse
import logging
from pathlib import Path
from typing import Dict, List, Any, Tuple, Optional
from datetime import datetime
import re

try:
    import jsonschema
    from jsonschema import validate, ValidationError, Draft202012Validator
    JSONSCHEMA_AVAILABLE = True
except ImportError:
    JSONSCHEMA_AVAILABLE = False
    print("⚠️  Warning: jsonschema not available. Install with: pip install jsonschema")

class BaselineSetValidator:
    """Validates baseline_set.json files for Alchemist experiments."""
    
    def __init__(self, schema_path: Optional[str] = None):
        # Configure logging first
        logging.basicConfig(
            level=logging.INFO,
            format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
        )
        self.logger = logging.getLogger('BaselineSetValidator')
        
        self.schema_path = schema_path or self._find_schema()
        self.schema = self._load_schema()
        self.errors = []
        self.warnings = []
        
    def _find_schema(self) -> str:
        """Find the baseline_set.json schema file."""
        # Try common locations
        schema_paths = [
            "schemas/baseline_set.json",
            "../schemas/baseline_set.json", 
            "../../schemas/baseline_set.json",
            Path(__file__).parent.parent / "schemas" / "baseline_set.json"
        ]
        
        for path in schema_paths:
            if Path(path).exists():
                return str(path)
        
        raise FileNotFoundError("Could not find baseline_set.json schema file")
    
    def _load_schema(self) -> Dict[str, Any]:
        """Load the JSON schema for baseline_set.json."""
        try:
            with open(self.schema_path, 'r') as f:
                schema = json.load(f)
            self.logger.info(f"Loaded schema from: {self.schema_path}")
            return schema
        except Exception as e:
            raise RuntimeError(f"Failed to load schema: {e}")
    
    def validate_file(self, baseline_path: str) -> Tuple[bool, List[str], List[str]]:
        """
        Validate a baseline_set.json file.
        
        Returns:
            Tuple of (is_valid, errors, warnings)
        """
        self.errors = []
        self.warnings = []
        
        try:
            # Load baseline file
            with open(baseline_path, 'r') as f:
                baseline_data = json.load(f)
            
            self.logger.info(f"Validating baseline file: {baseline_path}")
            
            # Schema validation
            if JSONSCHEMA_AVAILABLE:
                self._validate_schema(baseline_data)
            else:
                self.warnings.append("JSON schema validation skipped - jsonschema not available")
            
            # Semantic validation
            self._validate_semantics(baseline_data)
            
            # Consistency validation
            self._validate_consistency(baseline_data)
            
            # Provenance validation  
            self._validate_provenance(baseline_data)
            
            is_valid = len(self.errors) == 0
            
            if is_valid:
                self.logger.info(f"✅ Baseline file validation passed: {baseline_path}")
            else:
                self.logger.error(f"❌ Baseline file validation failed: {baseline_path}")
                
            return is_valid, self.errors, self.warnings
            
        except Exception as e:
            self.errors.append(f"Failed to process file: {e}")
            return False, self.errors, self.warnings
    
    def _validate_schema(self, data: Dict[str, Any]) -> None:
        """Validate against JSON schema."""
        try:
            validator = Draft202012Validator(self.schema)
            
            # Collect all validation errors
            schema_errors = []
            for error in validator.iter_errors(data):
                error_path = " -> ".join(str(p) for p in error.absolute_path)
                if error_path:
                    schema_errors.append(f"Path '{error_path}': {error.message}")
                else:
                    schema_errors.append(f"Root: {error.message}")
            
            if schema_errors:
                self.errors.extend(schema_errors)
            else:
                self.logger.info("✅ JSON schema validation passed")
                
        except Exception as e:
            self.errors.append(f"Schema validation error: {e}")
    
    def _validate_semantics(self, data: Dict[str, Any]) -> None:
        """Validate semantic correctness of baseline data."""
        try:
            # Validate metadata
            self._validate_metadata(data.get('metadata', {}))
            
            # Validate baseline metrics
            self._validate_baseline_metrics(data.get('baseline_metrics', {}))
            
            # Validate experiment context
            self._validate_experiment_context(data.get('experiment_context', {}))
            
        except Exception as e:
            self.errors.append(f"Semantic validation error: {e}")
    
    def _validate_metadata(self, metadata: Dict[str, Any]) -> None:
        """Validate metadata section."""
        # Check version format
        version = metadata.get('version', '')
        if version and not re.match(r'^\d+\.\d+\.\d+$', version):
            self.errors.append(f"Invalid version format: {version} (expected semver)")
        
        # Check timestamps
        for field in ['created_at', 'updated_at']:
            if field in metadata:
                try:
                    datetime.fromisoformat(metadata[field].replace('Z', '+00:00'))
                except ValueError:
                    self.errors.append(f"Invalid timestamp format in {field}: {metadata[field]}")
        
        # Check baseline_id format
        baseline_id = metadata.get('baseline_id', '')
        if baseline_id and not re.match(r'^[a-zA-Z0-9_-]+$', baseline_id):
            self.errors.append(f"Invalid baseline_id format: {baseline_id}")
        
        if len(baseline_id) > 64:
            self.errors.append(f"baseline_id too long: {len(baseline_id)} > 64 characters")
    
    def _validate_baseline_metrics(self, metrics: Dict[str, Any]) -> None:
        """Validate baseline metrics section."""
        primary_metrics = metrics.get('primary_metrics', {})
        secondary_metrics = metrics.get('secondary_metrics', {})
        derived_metrics = metrics.get('derived_metrics', {})
        
        # Validate metric names
        all_metric_names = set()
        
        for metric_set_name, metric_set in [
            ('primary_metrics', primary_metrics),
            ('secondary_metrics', secondary_metrics)
        ]:
            for metric_name, metric_data in metric_set.items():
                if not re.match(r'^[a-zA-Z_][a-zA-Z0-9_]*$', metric_name):
                    self.errors.append(f"Invalid metric name in {metric_set_name}: {metric_name}")
                
                all_metric_names.add(metric_name)
                
                # Validate thresholds
                value = metric_data.get('value')
                threshold_upper = metric_data.get('threshold_upper')
                threshold_lower = metric_data.get('threshold_lower')
                
                if threshold_upper is not None and threshold_lower is not None:
                    if threshold_upper <= threshold_lower:
                        self.errors.append(f"Invalid thresholds for {metric_name}: upper ({threshold_upper}) <= lower ({threshold_lower})")
                
                # Validate confidence interval
                ci = metric_data.get('confidence_interval')
                if ci:
                    lower = ci.get('lower')
                    upper = ci.get('upper')
                    if lower is not None and upper is not None and lower >= upper:
                        self.errors.append(f"Invalid confidence interval for {metric_name}: lower >= upper")
        
        # Validate derived metrics formulas
        for metric_name, metric_data in derived_metrics.items():
            if not re.match(r'^[a-zA-Z_][a-zA-Z0-9_]*$', metric_name):
                self.errors.append(f"Invalid derived metric name: {metric_name}")
            
            formula = metric_data.get('formula', '')
            # Basic validation - check if referenced metrics exist
            referenced_metrics = re.findall(r'\b([a-zA-Z_][a-zA-Z0-9_]*)\b', formula)
            for ref_metric in referenced_metrics:
                if ref_metric not in all_metric_names and ref_metric not in derived_metrics:
                    # Could be a function name, only warn
                    self.warnings.append(f"Derived metric {metric_name} references unknown metric: {ref_metric}")
    
    def _validate_experiment_context(self, context: Dict[str, Any]) -> None:
        """Validate experiment context section."""
        # Validate applicable conditions
        conditions = context.get('applicable_conditions', [])
        condition_names = set()
        
        for condition in conditions:
            condition_name = condition.get('condition_name', '')
            if not condition_name:
                self.errors.append("Empty condition_name in applicable_conditions")
                continue
                
            if condition_name in condition_names:
                self.errors.append(f"Duplicate condition name: {condition_name}")
            condition_names.add(condition_name)
        
        # Validate environment requirements
        env_req = context.get('environment_requirements', {})
        min_corpus_size = env_req.get('min_corpus_size')
        if min_corpus_size is not None and min_corpus_size < 1:
            self.errors.append(f"min_corpus_size must be >= 1, got: {min_corpus_size}")
    
    def _validate_consistency(self, data: Dict[str, Any]) -> None:
        """Validate internal consistency of baseline data."""
        try:
            # Check timestamp consistency
            metadata = data.get('metadata', {})
            created_at = metadata.get('created_at')
            updated_at = metadata.get('updated_at')
            
            if created_at and updated_at:
                try:
                    created_dt = datetime.fromisoformat(created_at.replace('Z', '+00:00'))
                    updated_dt = datetime.fromisoformat(updated_at.replace('Z', '+00:00'))
                    
                    if updated_dt < created_dt:
                        self.errors.append("updated_at cannot be before created_at")
                except ValueError:
                    pass  # Already handled in semantic validation
            
            # Check experiment type consistency
            exp_context = data.get('experiment_context', {})
            exp_type = exp_context.get('experiment_type')
            metrics = data.get('baseline_metrics', {}).get('primary_metrics', {})
            
            # Type-specific metric validation
            if exp_type == 'cognitive_performance':
                expected_metrics = ['intervention_acceptance_rate', 'response_quality_score', 'processing_time_ms']
                for metric in expected_metrics:
                    if metric not in metrics:
                        self.warnings.append(f"Expected metric '{metric}' not found for experiment_type 'cognitive_performance'")
            
            elif exp_type == 'system_performance':
                expected_metrics = ['throughput_items_per_sec', 'memory_usage_mb', 'cpu_utilization_pct']
                for metric in expected_metrics:
                    if metric not in metrics:
                        self.warnings.append(f"Expected metric '{metric}' not found for experiment_type 'system_performance'")
                        
        except Exception as e:
            self.errors.append(f"Consistency validation error: {e}")
    
    def _validate_provenance(self, data: Dict[str, Any]) -> None:
        """Validate provenance information."""
        try:
            provenance = data.get('provenance', {})
            if not provenance:
                self.warnings.append("No provenance information provided")
                return
            
            # Validate source experiments
            source_experiments = provenance.get('source_experiments', [])
            total_weight = 0
            
            for exp in source_experiments:
                weight = exp.get('weight')
                if weight is not None:
                    total_weight += weight
            
            if source_experiments and abs(total_weight - 1.0) > 0.001:
                self.warnings.append(f"Source experiment weights don't sum to 1.0: {total_weight}")
            
            # Validate git context
            git_context = provenance.get('git_context', {})
            commit_sha = git_context.get('commit_sha')
            if commit_sha and not re.match(r'^[a-f0-9]{40}$', commit_sha):
                self.errors.append(f"Invalid git commit SHA format: {commit_sha}")
            
            # Validate Gu Pot origin hashes
            gu_pot = provenance.get('gu_pot_origin', {})
            for hash_field in ['logline_hash', 'tension_hash']:
                hash_value = gu_pot.get(hash_field)
                if hash_value and not re.match(r'^sha256:[a-f0-9]{64}$', hash_value):
                    self.errors.append(f"Invalid {hash_field} format: {hash_value}")
                    
        except Exception as e:
            self.errors.append(f"Provenance validation error: {e}")
    
    def validate_directory(self, directory_path: str, pattern: str = "baseline_set*.json") -> Dict[str, Tuple[bool, List[str], List[str]]]:
        """
        Validate all baseline_set.json files in a directory.
        
        Returns:
            Dictionary mapping file paths to validation results
        """
        results = {}
        directory = Path(directory_path)
        
        if not directory.exists():
            raise FileNotFoundError(f"Directory not found: {directory_path}")
        
        baseline_files = list(directory.glob(pattern))
        
        if not baseline_files:
            self.logger.warning(f"No files matching pattern '{pattern}' found in {directory_path}")
        
        for file_path in baseline_files:
            self.logger.info(f"Validating: {file_path}")
            results[str(file_path)] = self.validate_file(str(file_path))
        
        return results
    
    def generate_report(self, results: Dict[str, Tuple[bool, List[str], List[str]]]) -> str:
        """Generate a validation report."""
        total_files = len(results)
        valid_files = sum(1 for is_valid, _, _ in results.values() if is_valid)
        
        report = []
        report.append("🧙‍♂️ Alchemist Baseline Set Validation Report")
        report.append("=" * 60)
        report.append(f"Total files: {total_files}")
        report.append(f"Valid files: {valid_files}")
        report.append(f"Invalid files: {total_files - valid_files}")
        report.append(f"Success rate: {valid_files/total_files:.1%}" if total_files > 0 else "No files processed")
        report.append("")
        
        for file_path, (is_valid, errors, warnings) in results.items():
            status = "✅ VALID" if is_valid else "❌ INVALID"
            report.append(f"{status}: {Path(file_path).name}")
            
            if errors:
                report.append("  Errors:")
                for error in errors:
                    report.append(f"    - {error}")
            
            if warnings:
                report.append("  Warnings:")
                for warning in warnings:
                    report.append(f"    - {warning}")
            
            if not errors and not warnings:
                report.append("  No issues found")
            
            report.append("")
        
        return "\n".join(report)


def main():
    """CLI interface for baseline set validation."""
    parser = argparse.ArgumentParser(
        description="Validate baseline_set.json files for Alchemist experiments",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Validate a single file
  python validate_baseline_set.py baseline_set.json
  
  # Validate all baseline files in a directory
  python validate_baseline_set.py --directory experiments/baselines/
  
  # Use custom schema
  python validate_baseline_set.py --schema custom_schema.json baseline_set.json
  
  # Generate JSON report
  python validate_baseline_set.py --output-format json baseline_set.json
        """
    )
    
    parser.add_argument(
        'files',
        nargs='*',
        help='Baseline set JSON files to validate'
    )
    
    parser.add_argument(
        '--directory', '-d',
        help='Directory containing baseline set files to validate'
    )
    
    parser.add_argument(
        '--pattern', '-p',
        default='baseline_set*.json',
        help='File pattern for directory validation (default: baseline_set*.json)'
    )
    
    parser.add_argument(
        '--schema', '-s',
        help='Path to JSON schema file'
    )
    
    parser.add_argument(
        '--output-format', '-f',
        choices=['text', 'json'],
        default='text',
        help='Output format for results'
    )
    
    parser.add_argument(
        '--output', '-o',
        help='Output file for results (default: stdout)'
    )
    
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Enable verbose logging'
    )
    
    args = parser.parse_args()
    
    if args.verbose:
        logging.getLogger().setLevel(logging.DEBUG)
    
    try:
        # Initialize validator
        validator = BaselineSetValidator(schema_path=args.schema)
        
        # Collect validation results
        results = {}
        
        # Validate individual files
        for file_path in args.files:
            results[file_path] = validator.validate_file(file_path)
        
        # Validate directory
        if args.directory:
            dir_results = validator.validate_directory(args.directory, args.pattern)
            results.update(dir_results)
        
        if not results:
            print("❌ No files specified for validation")
            print("Use --help for usage information")
            return 1
        
        # Generate report
        if args.output_format == 'json':
            report_data = {
                'summary': {
                    'total_files': len(results),
                    'valid_files': sum(1 for is_valid, _, _ in results.values() if is_valid),
                    'timestamp': datetime.now().isoformat()
                },
                'results': {
                    file_path: {
                        'valid': is_valid,
                        'errors': errors,
                        'warnings': warnings
                    }
                    for file_path, (is_valid, errors, warnings) in results.items()
                }
            }
            report = json.dumps(report_data, indent=2)
        else:
            report = validator.generate_report(results)
        
        # Output results
        if args.output:
            with open(args.output, 'w') as f:
                f.write(report)
            print(f"Report written to: {args.output}")
        else:
            print(report)
        
        # Exit with appropriate code
        all_valid = all(is_valid for is_valid, _, _ in results.values())
        return 0 if all_valid else 1
        
    except Exception as e:
        print(f"❌ Validation failed: {e}")
        return 1


if __name__ == "__main__":
    exit(main())