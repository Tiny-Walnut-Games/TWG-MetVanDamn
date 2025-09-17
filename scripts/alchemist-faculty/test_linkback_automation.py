#!/usr/bin/env python3
"""
Test script for Alchemist Faculty Linkback Automation

Tests various scenarios and edge cases for the linkback automation system.
"""

import os
import sys
import tempfile
import json
import shutil
from pathlib import Path

# Add the script directory to the path so we can import the module
sys.path.insert(0, os.path.join(os.path.dirname(__file__)))

from linkback_automation import ClaimsDetector, ValidationSummary, ClaimData, EvidenceTemplateRenderer, LinkbackAutomation

def create_test_claims_structure(temp_dir: Path):
    """Create a test claims directory structure"""
    claims_dir = temp_dir / "claims"
    
    # Create directories
    (claims_dir / "validated").mkdir(parents=True, exist_ok=True)
    (claims_dir / "hypotheses").mkdir(parents=True, exist_ok=True)
    (claims_dir / "regressions").mkdir(parents=True, exist_ok=True)
    
    # Create test claim files
    validated_claim = {
        "RunId": "run_0001",
        "ExperimentName": "test_experiment",
        "HypothesisId": "test-hypothesis-1",
        "ConfidenceScore": 0.85,
        "ClaimType": "validated",
        "ValidationTime": "2025-09-06T16:00:00Z",
        "Success": True,
        "BaselineComparison": "15% improvement"
    }
    
    hypothesis_claim = {
        "RunId": "run_0002", 
        "ExperimentName": "test_experiment",
        "HypothesisId": "test-hypothesis-2",
        "ConfidenceScore": 0.80,
        "ClaimType": "hypothesis",
        "ValidationTime": "2025-09-06T16:00:00Z",
        "Success": True,
        "BaselineComparison": "5% improvement"
    }
    
    regression_claim = {
        "RunId": "run_0003",
        "ExperimentName": "test_experiment", 
        "HypothesisId": "test-hypothesis-3",
        "ConfidenceScore": 0.75,
        "ClaimType": "regression",
        "ValidationTime": "2025-09-06T16:00:00Z",
        "Success": False,
        "BaselineComparison": "-10% regression"
    }
    
    # Save claim files
    with open(claims_dir / "validated" / "claim_validated_001.json", 'w') as f:
        json.dump(validated_claim, f)
    
    with open(claims_dir / "hypotheses" / "claim_hypothesis_002.json", 'w') as f:
        json.dump(hypothesis_claim, f)
    
    with open(claims_dir / "regressions" / "claim_regression_003.json", 'w') as f:
        json.dump(regression_claim, f)
    
    # Create integration metadata
    integration_data = {
        "Timestamp": "2025-09-06T16:00:00Z",
        "UnityVersion": "6000.2.0f1",
        "ProjectPath": "/test/project",
        "GitCommit": "abc123",
        "GitBranch": "test-branch",
        "RunsValidated": 3,
        "ClaimsPromoted": 1,
        "AverageConfidence": 0.58,
        "BaselineDelta": 5.0,
        "OutputDirectory": str(claims_dir),
        "ClaimsPaths": [
            str(claims_dir / "validated" / "claim_validated_001.json"),
            str(claims_dir / "hypotheses" / "claim_hypothesis_002.json"),
            str(claims_dir / "regressions" / "claim_regression_003.json")
        ],
        "Stage": "4",
        "Workflow": "school"
    }
    
    with open(claims_dir / "github_integration.json", 'w') as f:
        json.dump(integration_data, f)
    
    return claims_dir

def test_claims_detection():
    """Test claims detection functionality"""
    print("Testing claims detection...")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)
        claims_dir = create_test_claims_structure(temp_path)
        
        detector = ClaimsDetector(str(claims_dir))
        claims = detector.scan_for_new_claims()
        
        assert len(claims) == 3, f"Expected 3 claims, got {len(claims)}"
        
        # Check claim types
        claim_types = [claim.claim_type for claim in claims]
        assert "validated" in claim_types, "Missing validated claim"
        assert "hypothesis" in claim_types, "Missing hypothesis claim"
        assert "regression" in claim_types, "Missing regression claim"
        
        print("✓ Claims detection test passed")

def test_validation_summary():
    """Test validation summary generation"""
    print("Testing validation summary generation...")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)
        claims_dir = create_test_claims_structure(temp_path)
        
        detector = ClaimsDetector(str(claims_dir))
        claims = detector.scan_for_new_claims()
        summary = detector.generate_validation_summary(claims)
        
        assert summary.total_claims == 3, f"Expected 3 total claims, got {summary.total_claims}"
        assert summary.validated_claims == 1, f"Expected 1 validated claim, got {summary.validated_claims}"
        assert summary.hypotheses == 1, f"Expected 1 hypothesis, got {summary.hypotheses}"
        assert summary.regressions == 1, f"Expected 1 regression, got {summary.regressions}"
        assert summary.stage_decision == "serum", f"Expected serum decision, got {summary.stage_decision}"
        
        print("✓ Validation summary test passed")

def test_template_rendering():
    """Test evidence template rendering"""
    print("Testing template rendering...")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)
        claims_dir = create_test_claims_structure(temp_path)
        
        detector = ClaimsDetector(str(claims_dir))
        claims = detector.scan_for_new_claims()
        summary = detector.generate_validation_summary(claims)
        integration_data = detector.get_integration_metadata()
        
        renderer = EvidenceTemplateRenderer()
        evidence_section = renderer.render_evidence_section(summary, claims, integration_data, 123)
        
        assert "## 🧪 Alchemist Evidence Links" in evidence_section, "Missing evidence links header"
        assert "SERUM" in evidence_section, "Missing stage decision"
        assert "3" in evidence_section, "Missing claim count"
        assert "test_experiment" in evidence_section or "unknown" in evidence_section, "Missing experiment name or unknown placeholder"
        
        print("✓ Template rendering test passed")

def test_edge_cases():
    """Test edge cases and error conditions"""
    print("Testing edge cases...")
    
    # Test with empty claims directory
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)
        empty_claims_dir = temp_path / "empty_claims"
        empty_claims_dir.mkdir()
        
        detector = ClaimsDetector(str(empty_claims_dir))
        claims = detector.scan_for_new_claims()
        
        assert len(claims) == 0, f"Expected 0 claims in empty directory, got {len(claims)}"
        
        summary = detector.generate_validation_summary(claims)
        assert summary.stage_decision == "compost", f"Expected compost for no claims, got {summary.stage_decision}"
        
    # Test with missing integration file
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)
        claims_dir = temp_path / "claims"
        claims_dir.mkdir()
        
        detector = ClaimsDetector(str(claims_dir))
        integration_data = detector.get_integration_metadata()
        
        assert integration_data is None, "Expected None for missing integration file"
        
    print("✓ Edge cases test passed")

def test_dry_run_mode():
    """Test dry-run mode functionality"""
    print("Testing dry-run mode...")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        temp_path = Path(temp_dir)
        claims_dir = create_test_claims_structure(temp_path)
        
        automation = LinkbackAutomation(github_token=None, dry_run=True)
        automation.claims_detector.claims_base_dir = claims_dir
        
        # This should work without a GitHub token in dry-run mode
        success = automation.process_issue_linkback("test-owner", "test-repo", 123)
        assert success, "Dry-run mode should succeed without GitHub token"
        
    print("✓ Dry-run mode test passed")

def test_issue_body_update():
    """Test issue body update logic"""
    print("Testing issue body update logic...")
    
    automation = LinkbackAutomation(dry_run=True)
    
    # Test appending to issue without existing evidence section
    original_body = """## Logline
Test issue

## Tension
Some tension"""
    
    evidence_section = "## 🧪 Alchemist Evidence Links\nTest evidence"
    updated_body = automation._update_issue_body(original_body, evidence_section)
    
    assert "## 🧪 Alchemist Evidence Links" in updated_body, "Evidence section not added"
    assert "Test evidence" in updated_body, "Evidence content not added"
    
    # Test replacing existing evidence section
    body_with_evidence = original_body + "\n\n## 🧪 Alchemist Evidence Links\nOld evidence"
    updated_body2 = automation._update_issue_body(body_with_evidence, evidence_section)
    
    assert "Test evidence" in updated_body2, "Evidence section not replaced"
    assert "Old evidence" not in updated_body2, "Old evidence not removed"
    
    print("✓ Issue body update test passed")

def run_all_tests():
    """Run all tests"""
    print("🧪 Starting Alchemist Faculty Linkback Automation Tests\n")
    
    try:
        test_claims_detection()
        test_validation_summary()
        test_template_rendering()
        test_edge_cases()
        test_dry_run_mode()
        test_issue_body_update()
        
        print("\n✅ All tests passed! Linkback automation is working correctly.")
        return True
        
    except Exception as e:
        print(f"\n❌ Test failed: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == '__main__':
    success = run_all_tests()
    sys.exit(0 if success else 1)