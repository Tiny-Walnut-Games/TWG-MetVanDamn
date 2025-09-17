#!/usr/bin/env python3
"""
Test suite for Alchemist Faculty Report Synthesis CLI Tool

Tests the core functionality of report_synthesizer.py including:
- Loading and processing claims data
- Generating reports with proper formatting
- Provenance block generation
- File path validation
- Confidence sorting and summary tables
"""

import unittest
import tempfile
import json
import yaml
import os
import sys
from pathlib import Path
from datetime import datetime, timezone

# Add scripts directory to path to import report_synthesizer
sys.path.insert(0, str(Path(__file__).parent.parent / "scripts" / "alchemist-faculty"))

from report_synthesizer import ReportSynthesizer, ClaimData, RunMetadata, ProvenanceBlock

class TestReportSynthesizer(unittest.TestCase):
    """Test cases for the Report Synthesizer"""
    
    def setUp(self):
        """Set up test fixtures"""
        self.temp_dir = tempfile.mkdtemp()
        self.temp_path = Path(self.temp_dir)
        self.synthesizer = ReportSynthesizer()
        
        # Create test data
        self.test_claims = [
            ClaimData(
                RunId="run_001",
                ExperimentName="test_experiment_high_confidence",
                HypothesisId="hyp_001",
                ConfidenceScore=0.85,
                ClaimType="validated",
                ValidationTime="2025-09-06T12:00:00Z",
                Success=True,
                BaselineComparison="15% improvement",
                origin={"issue_number": 123, "issue_url": "https://github.com/test/repo/issues/123"}
            ),
            ClaimData(
                RunId="run_002", 
                ExperimentName="test_experiment_medium_confidence",
                HypothesisId="hyp_002",
                ConfidenceScore=0.65,
                ClaimType="hypothesis",
                ValidationTime="2025-09-06T12:15:00Z",
                Success=True,
                BaselineComparison="5% improvement"
            ),
            ClaimData(
                RunId="run_003",
                ExperimentName="test_experiment_low_confidence", 
                HypothesisId="hyp_003",
                ConfidenceScore=0.35,
                ClaimType="regression",
                ValidationTime="2025-09-06T12:30:00Z",
                Success=False,
                BaselineComparison="-2% regression"
            )
        ]
        
        self.test_run_metadata = {
            "run_001": RunMetadata(
                run_id="run_001",
                timestamp="2025-09-06T11:45:00Z",
                experiment_id="exp_001",
                status="completed",
                git_commit="abc123",
                git_branch="main"
            ),
            "run_002": RunMetadata(
                run_id="run_002", 
                timestamp="2025-09-06T12:00:00Z",
                experiment_id="exp_002",
                status="completed"
            )
        }
        
        self.test_manifest = {
            "metadata": {
                "name": "Test Experiment",
                "description": "Test experiment for validation",
                "experiment_id": "test_exp_001"
            },
            "origin": {
                "type": "gu_pot",
                "issue_number": 123,
                "issue_url": "https://github.com/test/repo/issues/123"
            }
        }
    
    def tearDown(self):
        """Clean up test fixtures"""
        import shutil
        shutil.rmtree(self.temp_dir)
    
    def create_test_files(self):
        """Create test file structure"""
        # Create claims directories
        claims_dir = self.temp_path / "claims"
        (claims_dir / "validated").mkdir(parents=True)
        (claims_dir / "hypotheses").mkdir(parents=True)
        (claims_dir / "regressions").mkdir(parents=True)
        
        # Create claim files
        validated_claim = {
            "RunId": "run_001",
            "ExperimentName": "test_experiment_high_confidence",
            "HypothesisId": "hyp_001",
            "ConfidenceScore": 0.85,
            "ClaimType": "validated",
            "ValidationTime": "2025-09-06T12:00:00Z",
            "Success": True,
            "BaselineComparison": "15% improvement"
        }
        
        with open(claims_dir / "validated" / "claim_run_001.json", "w") as f:
            json.dump(validated_claim, f)
        
        hypothesis_claim = {
            "RunId": "run_002",
            "ExperimentName": "test_experiment_medium_confidence", 
            "HypothesisId": "hyp_002",
            "ConfidenceScore": 0.65,
            "ClaimType": "hypothesis",
            "ValidationTime": "2025-09-06T12:15:00Z",
            "Success": True,
            "BaselineComparison": "5% improvement"
        }
        
        with open(claims_dir / "hypotheses" / "claim_run_002.json", "w") as f:
            json.dump(hypothesis_claim, f)
        
        # Create runs directories
        runs_dir = self.temp_path / "runs"
        (runs_dir / "run_001").mkdir(parents=True)
        (runs_dir / "run_002").mkdir(parents=True)
        
        # Create run metadata files
        run_metadata_001 = {
            "run_id": "run_001",
            "timestamp": "2025-09-06T11:45:00Z",
            "experiment_id": "exp_001",
            "status": "completed"
        }
        
        with open(runs_dir / "run_001" / "run_metadata.json", "w") as f:
            json.dump(run_metadata_001, f)
        
        # Create manifest file
        with open(self.temp_path / "manifest_v1.yaml", "w") as f:
            yaml.dump(self.test_manifest, f)
        
        return claims_dir, runs_dir
    
    def test_load_claims(self):
        """Test loading claims from directory structure"""
        claims_dir, _ = self.create_test_files()
        
        claims = self.synthesizer.load_claims(claims_dir)
        
        self.assertEqual(len(claims), 2)
        self.assertEqual(claims[0].ClaimType, "validated")
        self.assertEqual(claims[1].ClaimType, "hypothesis")
        self.assertAlmostEqual(claims[0].ConfidenceScore, 0.85)
    
    def test_load_run_metadata(self):
        """Test loading run metadata"""
        _, runs_dir = self.create_test_files()
        
        metadata = self.synthesizer.load_run_metadata(runs_dir)
        
        self.assertEqual(len(metadata), 1)  # Only run_001 has metadata file
        self.assertIn("run_001", metadata)
        self.assertEqual(metadata["run_001"].status, "completed")
    
    def test_load_manifest(self):
        """Test loading manifest file"""
        self.create_test_files()
        manifest_path = self.temp_path / "manifest_v1.yaml"
        
        manifest = self.synthesizer.load_manifest(manifest_path)
        
        self.assertEqual(manifest["metadata"]["name"], "Test Experiment")
        self.assertEqual(manifest["origin"]["issue_number"], 123)
    
    def test_validate_evidence_paths(self):
        """Test evidence path validation"""
        claims_dir, runs_dir = self.create_test_files()
        
        claims = self.synthesizer.load_claims(claims_dir)
        
        # Create missing run metadata for run_002
        run_metadata_002 = {
            "run_id": "run_002",
            "timestamp": "2025-09-06T12:00:00Z",
            "experiment_id": "exp_002",
            "status": "completed"
        }
        
        with open(runs_dir / "run_002" / "run_metadata.json", "w") as f:
            json.dump(run_metadata_002, f)
        
        # Should pass validation since we created the run directories and metadata
        valid, missing = self.synthesizer.validate_evidence_paths(claims, self.temp_path)
        self.assertTrue(valid)
        self.assertEqual(len(missing), 0)
        
        # Test with missing run directory
        import shutil
        shutil.rmtree(runs_dir / "run_002")
        
        valid, missing = self.synthesizer.validate_evidence_paths(claims, self.temp_path)
        self.assertFalse(valid)
        self.assertTrue(any("run_002" in path for path in missing))
    
    def test_sort_claims_by_confidence(self):
        """Test confidence-based sorting"""
        sorted_claims = self.synthesizer.sort_claims_by_confidence(self.test_claims)
        
        # Should be sorted highest to lowest confidence
        self.assertEqual(sorted_claims[0].ConfidenceScore, 0.85)
        self.assertEqual(sorted_claims[1].ConfidenceScore, 0.65)  
        self.assertEqual(sorted_claims[2].ConfidenceScore, 0.35)
    
    def test_generate_confidence_bar(self):
        """Test confidence bar generation"""
        # Test various confidence levels
        bar_85 = self.synthesizer.generate_confidence_bar(0.85)
        self.assertIn("85%", bar_85)
        self.assertIn("█", bar_85)
        
        bar_50 = self.synthesizer.generate_confidence_bar(0.50)
        self.assertIn("50%", bar_50)
        
        bar_10 = self.synthesizer.generate_confidence_bar(0.10)
        self.assertIn("10%", bar_10)
        self.assertIn("░", bar_10)
    
    def test_create_provenance_block(self):
        """Test provenance block creation"""
        provenance = self.synthesizer.create_provenance_block(
            self.test_claims, self.test_run_metadata, self.test_manifest
        )
        
        self.assertEqual(len(provenance.claim_ids), 3)
        self.assertEqual(len(provenance.run_ids), 3)
        self.assertEqual(len(provenance.claim_hashes), 3)
        self.assertIn(123, provenance.origin_issue_numbers)
        self.assertIsNotNone(provenance.manifest_hash)
        self.assertEqual(provenance.alchemist_version, "0.1.0")
    
    def test_generate_summary_table(self):
        """Test summary table generation"""
        table = self.synthesizer.generate_summary_table(self.test_claims)
        
        # Check table structure
        self.assertIn("| Claim ID | Type | Confidence | Run ID | Action |", table)
        self.assertIn("run_001", table)
        self.assertIn("85.0%", table)
        self.assertIn("validated", table)
        
        # Should be confidence-sorted (highest first)
        lines = table.split('\n')
        data_lines = [line for line in lines if line.startswith('|') and 'Claim ID' not in line and '---' not in line]
        self.assertTrue(data_lines[0].find("85.0%") > 0)  # First should be highest confidence
    
    def test_generate_claims_section(self):
        """Test claims section generation"""
        validated_claims = [c for c in self.test_claims if c.ClaimType == "validated"]
        
        section = self.synthesizer.generate_claims_section(
            validated_claims,
            "Test Validated Claims",
            "Test description"
        )
        
        self.assertIn("## Test Validated Claims", section)
        self.assertIn("test_experiment_high_confidence", section)
        self.assertIn("85.0%", section)
        self.assertIn("**Evidence:**", section)
        self.assertIn("**Confidence:**", section)
        self.assertIn("15% improvement", section)
    
    def test_generate_markdown_report(self):
        """Test complete markdown report generation"""
        provenance = self.synthesizer.create_provenance_block(
            self.test_claims, self.test_run_metadata, self.test_manifest
        )
        
        report = self.synthesizer.generate_markdown_report(
            self.test_manifest, self.test_claims, self.test_run_metadata, provenance
        )
        
        # Check report structure
        self.assertIn("# Test Experiment", report)
        self.assertIn("## Executive Summary", report)
        self.assertIn("## 📋 Summary Table", report)
        self.assertIn("## ✅ Validated Claims", report)
        self.assertIn("## 🔬 Hypotheses", report)
        self.assertIn("## ⚠️ Regressions", report)
        self.assertIn("## 🔗 Provenance Block", report)
        
        # Check data presence
        self.assertIn("**Total Claims:** 3", report)
        self.assertIn("✅ **Validated Claims:** 1", report)
        self.assertIn("🔬 **Hypotheses:** 1", report)
        self.assertIn("⚠️ **Regressions:** 1", report)
        
        # Check confidence information
        self.assertIn("85.0%", report)
        self.assertIn("65.0%", report)
        self.assertIn("35.0%", report)
    
    def test_save_report(self):
        """Test report saving"""
        report_content = "# Test Report\n\nThis is a test report."
        output_path = self.temp_path / "test_report.md"
        
        saved_path = self.synthesizer.save_report(report_content, output_path)
        
        self.assertTrue(saved_path.exists())
        self.assertEqual(saved_path, output_path)
        
        with open(saved_path, 'r') as f:
            content = f.read()
        
        self.assertEqual(content, report_content)
    
    def test_full_synthesis_workflow(self):
        """Test the complete synthesis workflow"""
        claims_dir, runs_dir = self.create_test_files()
        manifest_path = self.temp_path / "manifest_v1.yaml"
        output_path = self.temp_path / "report_v1.md"
        
        final_path = self.synthesizer.synthesize_report(
            manifest_path, claims_dir, runs_dir, output_path
        )
        
        self.assertTrue(final_path.exists())
        self.assertEqual(final_path, output_path)
        
        # Check report content
        with open(final_path, 'r') as f:
            content = f.read()
        
        self.assertIn("# Test Experiment", content)
        self.assertIn("**Total Claims:** 2", content)  # Only 2 claims created in test files
        self.assertIn("✅ **Validated Claims:** 1", content)
        self.assertIn("🔬 **Hypotheses:** 1", content)
    
    def test_claim_deduplication(self):
        """Test that duplicate claims are removed"""
        # The deduplication happens in synthesize_report method, not in provenance creation
        # Let's test the actual workflow
        claims_dir, runs_dir = self.create_test_files()
        manifest_path = self.temp_path / "manifest_v1.yaml"
        output_path = self.temp_path / "dedup_test_report.md"
        
        # Add a duplicate claim file
        duplicate_claim = {
            "RunId": "run_001",  # Same as existing claim
            "ExperimentName": "test_experiment_high_confidence",  # Same as existing
            "HypothesisId": "hyp_001",
            "ConfidenceScore": 0.85,
            "ClaimType": "validated", 
            "ValidationTime": "2025-09-06T12:00:00Z",
            "Success": True,
            "BaselineComparison": "15% improvement"
        }
        
        claims_dir = self.temp_path / "claims"
        with open(claims_dir / "validated" / "duplicate_claim_run_001.json", "w") as f:
            json.dump(duplicate_claim, f)
        
        # Process through synthesis which handles deduplication
        final_path = self.synthesizer.synthesize_report(
            manifest_path, claims_dir, runs_dir, output_path
        )
        
        # Read the report and verify only unique claims are included
        with open(final_path, 'r') as f:
            content = f.read()
        
        # Count occurrences of the experiment name in the claims sections only
        # Look for the pattern of "### experiment_name" which appears once per claim
        high_confidence_sections = content.count("### test_experiment_high_confidence")
        self.assertEqual(high_confidence_sections, 1, "Duplicate claim not properly deduplicated")

class TestClaimData(unittest.TestCase):
    """Test cases for ClaimData class"""
    
    def test_from_dict(self):
        """Test ClaimData creation from dictionary"""
        data = {
            "RunId": "run_001",
            "ExperimentName": "test_experiment",
            "HypothesisId": "hyp_001", 
            "ConfidenceScore": 0.75,
            "ClaimType": "validated",
            "ValidationTime": "2025-09-06T12:00:00Z",
            "Success": True,
            "BaselineComparison": "10% improvement",
            "origin": {"issue_number": 123}
        }
        
        claim = ClaimData.from_dict(data)
        
        self.assertEqual(claim.RunId, "run_001")
        self.assertEqual(claim.ExperimentName, "test_experiment")
        self.assertEqual(claim.ConfidenceScore, 0.75)
        self.assertTrue(claim.Success)
        self.assertEqual(claim.origin["issue_number"], 123)
    
    def test_from_dict_missing_fields(self):
        """Test ClaimData creation with missing fields"""
        data = {
            "RunId": "run_001"
        }
        
        claim = ClaimData.from_dict(data)
        
        self.assertEqual(claim.RunId, "run_001")
        self.assertEqual(claim.ExperimentName, "")
        self.assertEqual(claim.HypothesisId, "unknown")
        self.assertEqual(claim.ConfidenceScore, 0.0)
        self.assertFalse(claim.Success)

class TestRunMetadata(unittest.TestCase):
    """Test cases for RunMetadata class"""
    
    def test_from_dict(self):
        """Test RunMetadata creation from dictionary"""
        data = {
            "run_id": "run_001",
            "timestamp": "2025-09-06T12:00:00Z",
            "experiment_id": "exp_001",
            "status": "completed",
            "git_commit": "abc123"
        }
        
        metadata = RunMetadata.from_dict(data)
        
        self.assertEqual(metadata.run_id, "run_001")
        self.assertEqual(metadata.timestamp, "2025-09-06T12:00:00Z")
        self.assertEqual(metadata.status, "completed")
        self.assertEqual(metadata.git_commit, "abc123")

if __name__ == '__main__':
    # Run tests
    unittest.main(verbosity=2)