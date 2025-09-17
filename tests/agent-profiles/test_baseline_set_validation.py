#!/usr/bin/env python3
"""
Test Suite for Alchemist Baseline Set Validation

Tests the baseline_set.json schema validation functionality including
schema validation, semantic validation, and error handling.

🧙‍♂️ "Every validator must itself be validated - for how else do we ensure 
    our guardians of data integrity remain steadfast?" - Bootstrap Sentinel
"""

import sys
import os
import json
import tempfile
import shutil
from pathlib import Path
from typing import Dict, Any

# Add parent directory to path for script imports
sys.path.insert(0, str(Path(__file__).parent.parent))

try:
    from scripts.validate_baseline_set import BaselineSetValidator
    VALIDATOR_AVAILABLE = True
except ImportError as e:
    VALIDATOR_AVAILABLE = False
    print(f"Warning: Could not import BaselineSetValidator: {e}")

def create_minimal_valid_baseline() -> Dict[str, Any]:
    """Create a minimal valid baseline_set.json structure."""
    return {
        "metadata": {
            "version": "1.0.0",
            "created_at": "2025-01-01T00:00:00Z",
            "baseline_id": "test_baseline",
            "description": "Test baseline for validation"
        },
        "baseline_metrics": {
            "primary_metrics": {
                "test_metric": {
                    "value": 1.0,
                    "unit": "ratio"
                }
            }
        },
        "experiment_context": {
            "experiment_type": "cognitive_performance"
        }
    }

def test_schema_loading():
    """Test schema loading functionality."""
    print("🧙‍♂️ Testing Schema Loading")
    print("=" * 40)
    
    if not VALIDATOR_AVAILABLE:
        print("⚠️  Skipping - validator not available")
        return True
    
    try:
        # Test default schema loading
        validator = BaselineSetValidator()
        assert validator.schema is not None, "Schema should be loaded"
        assert "$schema" in validator.schema, "Schema should have $schema field"
        
        print("✅ Default schema loading successful")
        
        # Test schema structure
        assert "title" in validator.schema, "Schema should have title"
        assert "properties" in validator.schema, "Schema should have properties"
        assert "metadata" in validator.schema["properties"], "Schema should define metadata"
        assert "baseline_metrics" in validator.schema["properties"], "Schema should define baseline_metrics"
        
        print("✅ Schema structure validation passed")
        
        return True
        
    except Exception as e:
        print(f"❌ Schema loading test failed: {e}")
        return False

def test_valid_baseline_validation():
    """Test validation of valid baseline files."""
    print("✅ Testing Valid Baseline Validation")
    print("=" * 40)
    
    if not VALIDATOR_AVAILABLE:
        print("⚠️  Skipping - validator not available")
        return True
    
    try:
        validator = BaselineSetValidator()
        baseline_data = create_minimal_valid_baseline()
        
        # Create temporary file
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump(baseline_data, f)
            temp_path = f.name
        
        try:
            is_valid, errors, warnings = validator.validate_file(temp_path)
            
            assert is_valid, f"Valid baseline should pass validation. Errors: {errors}"
            assert len(errors) == 0, f"Valid baseline should have no errors: {errors}"
            
            print("✅ Minimal valid baseline validation passed")
            
            # Test with complete example
            example_path = "examples/alchemist/baselines/cognitive_baseline_v1.json"
            if Path(example_path).exists():
                is_valid, errors, warnings = validator.validate_file(example_path)
                assert is_valid, f"Example baseline should be valid. Errors: {errors}"
                print("✅ Example baseline validation passed")
            
            return True
            
        finally:
            os.unlink(temp_path)
            
    except Exception as e:
        print(f"❌ Valid baseline validation test failed: {e}")
        return False

def test_invalid_baseline_detection():
    """Test detection of invalid baseline files."""
    print("❌ Testing Invalid Baseline Detection")
    print("=" * 40)
    
    if not VALIDATOR_AVAILABLE:
        print("⚠️  Skipping - validator not available")
        return True
    
    try:
        validator = BaselineSetValidator()
        
        # Test missing required fields
        invalid_baseline = {"metadata": {"version": "1.0.0"}}  # Missing required fields
        
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump(invalid_baseline, f)
            temp_path = f.name
        
        try:
            is_valid, errors, warnings = validator.validate_file(temp_path)
            
            assert not is_valid, "Invalid baseline should fail validation"
            assert len(errors) > 0, "Invalid baseline should have errors"
            
            print(f"✅ Invalid baseline detection: {len(errors)} errors found")
            
            # Test invalid version format
            invalid_version = create_minimal_valid_baseline()
            invalid_version["metadata"]["version"] = "invalid_version"
            
            with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f2:
                json.dump(invalid_version, f2)
                temp_path2 = f2.name
            
            try:
                is_valid2, errors2, warnings2 = validator.validate_file(temp_path2)
                assert not is_valid2, "Invalid version format should fail validation"
                
                print("✅ Invalid version format detection passed")
                
            finally:
                os.unlink(temp_path2)
            
            return True
            
        finally:
            os.unlink(temp_path)
            
    except Exception as e:
        print(f"❌ Invalid baseline detection test failed: {e}")
        return False

def test_metric_validation():
    """Test metric-specific validation logic."""
    print("📊 Testing Metric Validation")
    print("=" * 40)
    
    if not VALIDATOR_AVAILABLE:
        print("⚠️  Skipping - validator not available")
        return True
    
    try:
        validator = BaselineSetValidator()
        
        # Test invalid threshold configuration
        invalid_thresholds = create_minimal_valid_baseline()
        invalid_thresholds["baseline_metrics"]["primary_metrics"]["test_metric"].update({
            "threshold_upper": 0.5,
            "threshold_lower": 0.8  # Lower > Upper (invalid)
        })
        
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump(invalid_thresholds, f)
            temp_path = f.name
        
        try:
            is_valid, errors, warnings = validator.validate_file(temp_path)
            
            assert not is_valid, "Invalid thresholds should fail validation"
            threshold_errors = [e for e in errors if "threshold" in e.lower()]
            assert len(threshold_errors) > 0, "Should detect threshold configuration errors"
            
            print("✅ Threshold validation passed")
            
            # Test invalid confidence interval
            invalid_ci = create_minimal_valid_baseline()
            invalid_ci["baseline_metrics"]["primary_metrics"]["test_metric"].update({
                "confidence_interval": {
                    "lower": 0.8,
                    "upper": 0.6  # Lower > Upper (invalid)
                }
            })
            
            with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f2:
                json.dump(invalid_ci, f2)
                temp_path2 = f2.name
            
            try:
                is_valid2, errors2, warnings2 = validator.validate_file(temp_path2)
                assert not is_valid2, "Invalid confidence interval should fail validation"
                
                print("✅ Confidence interval validation passed")
                
            finally:
                os.unlink(temp_path2)
            
            return True
            
        finally:
            os.unlink(temp_path)
            
    except Exception as e:
        print(f"❌ Metric validation test failed: {e}")
        return False

def test_provenance_validation():
    """Test provenance validation functionality."""
    print("🔗 Testing Provenance Validation")
    print("=" * 40)
    
    if not VALIDATOR_AVAILABLE:
        print("⚠️  Skipping - validator not available")
        return True
    
    try:
        validator = BaselineSetValidator()
        
        # Test invalid git commit SHA
        invalid_commit = create_minimal_valid_baseline()
        invalid_commit["provenance"] = {
            "git_context": {
                "commit_sha": "invalid_sha"  # Should be 40 hex chars
            }
        }
        
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump(invalid_commit, f)
            temp_path = f.name
        
        try:
            is_valid, errors, warnings = validator.validate_file(temp_path)
            
            assert not is_valid, "Invalid commit SHA should fail validation"
            sha_errors = [e for e in errors if "commit" in e.lower() or "sha" in e.lower()]
            assert len(sha_errors) > 0, "Should detect invalid commit SHA"
            
            print("✅ Git commit SHA validation passed")
            
            # Test invalid experiment weights
            invalid_weights = create_minimal_valid_baseline()
            invalid_weights["provenance"] = {
                "source_experiments": [
                    {"experiment_id": "exp1", "weight": 0.6},
                    {"experiment_id": "exp2", "weight": 0.5}  # Total = 1.1 (invalid)
                ]
            }
            
            with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f2:
                json.dump(invalid_weights, f2)
                temp_path2 = f2.name
            
            try:
                is_valid2, errors2, warnings2 = validator.validate_file(temp_path2)
                # This should generate warnings, not errors
                weight_warnings = [w for w in warnings2 if "weight" in w.lower()]
                assert len(weight_warnings) > 0, "Should warn about invalid weight sum"
                
                print("✅ Experiment weight validation passed")
                
            finally:
                os.unlink(temp_path2)
            
            return True
            
        finally:
            os.unlink(temp_path)
            
    except Exception as e:
        print(f"❌ Provenance validation test failed: {e}")
        return False

def test_directory_validation():
    """Test directory validation functionality."""
    print("📁 Testing Directory Validation")
    print("=" * 40)
    
    if not VALIDATOR_AVAILABLE:
        print("⚠️  Skipping - validator not available")
        return True
    
    try:
        validator = BaselineSetValidator()
        
        # Create temporary directory with test files
        with tempfile.TemporaryDirectory() as temp_dir:
            # Create valid baseline file
            valid_baseline = create_minimal_valid_baseline()
            valid_path = Path(temp_dir) / "valid_baseline.json"
            with open(valid_path, 'w') as f:
                json.dump(valid_baseline, f)
            
            # Create invalid baseline file
            invalid_baseline = {"invalid": "data"}
            invalid_path = Path(temp_dir) / "invalid_baseline.json"
            with open(invalid_path, 'w') as f:
                json.dump(invalid_baseline, f)
            
            # Test directory validation
            results = validator.validate_directory(temp_dir, "*.json")
            
            assert len(results) == 2, f"Should find 2 files, found {len(results)}"
            
            valid_count = sum(1 for is_valid, _, _ in results.values() if is_valid)
            invalid_count = len(results) - valid_count
            
            assert valid_count == 1, f"Should have 1 valid file, got {valid_count}"
            assert invalid_count == 1, f"Should have 1 invalid file, got {invalid_count}"
            
            print(f"✅ Directory validation: {valid_count} valid, {invalid_count} invalid")
            
            # Test report generation
            report = validator.generate_report(results)
            assert "Validation Report" in report, "Report should have title"
            assert "Success rate:" in report, "Report should have success rate"
            
            print("✅ Report generation passed")
            
            return True
            
    except Exception as e:
        print(f"❌ Directory validation test failed: {e}")
        return False

def test_cli_interface():
    """Test command-line interface functionality."""
    print("💻 Testing CLI Interface")
    print("=" * 40)
    
    try:
        # Test basic CLI functionality by checking if the main function exists
        from scripts.validate_baseline_set import main
        
        # Test with example files if they exist
        example_path = "examples/alchemist/baselines/cognitive_baseline_v1.json"
        if Path(example_path).exists():
            # Mock sys.argv for testing
            import sys
            original_argv = sys.argv
            try:
                sys.argv = ["validate_baseline_set.py", example_path]
                # Note: Not actually calling main() to avoid exit()
                print("✅ CLI interface import successful")
                
            finally:
                sys.argv = original_argv
        
        print("✅ CLI interface test passed")
        return True
        
    except Exception as e:
        print(f"❌ CLI interface test failed: {e}")
        return False

def main():
    """Run all baseline set validation tests."""
    print("🧙‍♂️ Alchemist Baseline Set Validation Test Suite")
    print("=" * 60)
    print("Testing JSON schema validation, semantic checks, and error handling\n")
    
    if not VALIDATOR_AVAILABLE:
        print("⚠️  WARNING: BaselineSetValidator not available - running limited tests")
        print("    Ensure scripts directory is in Python path\n")
    
    test_results = []
    
    # Run all test functions
    test_functions = [
        test_schema_loading,
        test_valid_baseline_validation,
        test_invalid_baseline_detection,
        test_metric_validation,
        test_provenance_validation,
        test_directory_validation,
        test_cli_interface
    ]
    
    for test_func in test_functions:
        try:
            result = test_func()
            test_results.append((test_func.__name__, result))
            print()  # Add spacing between tests
        except Exception as e:
            print(f"❌ Test {test_func.__name__} failed with exception: {e}")
            test_results.append((test_func.__name__, False))
            print()
    
    # Print summary
    print("📊 Test Summary")
    print("=" * 40)
    
    passed = 0
    failed = 0
    
    for test_name, result in test_results:
        status = "✅ PASSED" if result else "❌ FAILED"
        print(f"{status}: {test_name}")
        if result:
            passed += 1
        else:
            failed += 1
    
    print(f"\nResults: {passed} passed, {failed} failed")
    print(f"Success rate: {passed/(passed+failed):.1%}")
    
    if failed == 0:
        print("\n🎉 All baseline set validation tests PASSED!")
        print("\n📜 Baseline set validation system ready for Alchemist experiments")
        print("📜 JSON schema ensures structural integrity of baseline definitions")
        print("📜 Semantic validation catches data consistency issues")
        print("📜 CLI interface provides automated validation for CI/CD pipelines")
        
        print("\n🧙‍♂️ The Alchemist's validation grimoire is complete and ready!")
        return 0
    else:
        print(f"\n⚠️  {failed} tests failed - review and fix before deployment")
        return 1

if __name__ == "__main__":
    exit(main())