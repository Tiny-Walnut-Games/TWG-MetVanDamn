#!/usr/bin/env python3
"""
Test suite for Dev Time Travel (DTT) Vault System
Basic integration tests for the brick-layer snapshot store.
"""

import os
import sys
import tempfile
import shutil
import json
import subprocess
from pathlib import Path


def test_dtt_vault_initialization():
    """Test DTT vault initialization"""
    print("🧪 Testing DTT vault initialization...")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        test_root = Path(temp_dir)
        
        # Create fake git repo
        (test_root / ".git").mkdir()
        
        # Run DTT init command
        scripts_dir = Path(__file__).parent.parent / "scripts"
        result = subprocess.run([
            "python3", str(scripts_dir / "dtt"), "init"
        ], cwd=test_root, capture_output=True, text=True)
        
        assert result.returncode == 0, f"DTT init failed: {result.stderr}"
        
        # Check directory structure
        assert (test_root / ".dtt" / "vault" / "layer-0" / "bricks").exists()
        assert (test_root / ".dtt" / "vault" / "layer-0" / "manifests").exists()
        assert (test_root / ".dtt" / "vault" / "layer-1" / "bricks").exists()
        assert (test_root / ".dtt" / "vault" / "layer-1" / "manifests").exists()
        assert (test_root / ".dtt" / "vault" / "layer-2" / "bricks").exists()
        assert (test_root / ".dtt" / "vault" / "layer-2" / "manifests").exists()
        assert (test_root / ".dtt" / "index").exists()
        assert (test_root / ".dtt" / "logs").exists()
        assert (test_root / ".dtt" / "config.yml").exists()
        
        # Check catalog
        with open(test_root / ".dtt" / "index" / "catalog.json", 'r') as f:
            catalog = json.load(f)
        assert "bricks" in catalog
        assert "layers" in catalog
        assert catalog["created"] is not None
        
        print("✅ DTT vault initialization test passed")


def test_dtt_snapshot_creation():
    """Test snapshot creation and content addressing"""
    print("🧪 Testing DTT snapshot creation...")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        test_root = Path(temp_dir)
        
        # Create fake git repo with some content
        (test_root / ".git").mkdir()
        subprocess.run(["git", "init"], cwd=test_root, capture_output=True)
        subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=test_root, capture_output=True)
        subprocess.run(["git", "config", "user.name", "Test User"], cwd=test_root, capture_output=True)
        (test_root / "test_file.txt").write_text("Hello DTT!")
        subprocess.run(["git", "add", "."], cwd=test_root, capture_output=True)
        subprocess.run(["git", "commit", "-m", "Initial commit"], cwd=test_root, capture_output=True)
        
        # Initialize DTT
        scripts_dir = Path(__file__).parent.parent / "scripts"
        result = subprocess.run([
            "python3", str(scripts_dir / "dtt"), "init"
        ], cwd=test_root, capture_output=True, text=True)
        
        assert result.returncode == 0, f"DTT init failed: {result.stderr}"
        
        # Create snapshot
        result = subprocess.run([
            "python3", str(scripts_dir / "dtt"), "snapshot", 
            "--id", "test-snap", "--description", "Test snapshot"
        ], cwd=test_root, capture_output=True, text=True)
        
        assert result.returncode == 0, f"DTT snapshot failed: {result.stderr}"
        
        # Extract brick ID from output
        lines = result.stdout.strip().split('\n')
        brick_id = None
        for line in lines:
            if "Snapshot created:" in line:
                brick_id = line.split(": ")[1]
                break
                
        assert brick_id is not None, "Could not find brick ID in output"
        assert len(brick_id) == 16  # First 16 chars of SHA-256
        
        # Check files exist
        brick_path = test_root / ".dtt" / "vault" / "layer-0" / "bricks" / f"{brick_id}.tar.gz"
        manifest_path = test_root / ".dtt" / "vault" / "layer-0" / "manifests" / f"{brick_id}.json"
        
        assert brick_path.exists()
        assert manifest_path.exists()
        
        # Check manifest content
        with open(manifest_path, 'r') as f:
            manifest = json.load(f)
        assert manifest["brick_id"] == brick_id
        assert manifest["layer"] == "layer-0"
        assert manifest["description"] == "Test snapshot"
        
        print("✅ DTT snapshot creation test passed")


def test_dtt_vault_verification():
    """Test vault verification"""
    print("🧪 Testing DTT vault verification...")
    
    with tempfile.TemporaryDirectory() as temp_dir:
        test_root = Path(temp_dir)
        
        # Create fake git repo
        (test_root / ".git").mkdir()
        subprocess.run(["git", "init"], cwd=test_root, capture_output=True)
        subprocess.run(["git", "config", "user.email", "test@example.com"], cwd=test_root, capture_output=True)
        subprocess.run(["git", "config", "user.name", "Test User"], cwd=test_root, capture_output=True)
        (test_root / "test_file.txt").write_text("Hello DTT!")
        subprocess.run(["git", "add", "."], cwd=test_root, capture_output=True)
        subprocess.run(["git", "commit", "-m", "Initial commit"], cwd=test_root, capture_output=True)
        
        # Initialize DTT
        scripts_dir = Path(__file__).parent.parent / "scripts"
        result = subprocess.run([
            "python3", str(scripts_dir / "dtt"), "init"
        ], cwd=test_root, capture_output=True, text=True)
        
        assert result.returncode == 0, f"DTT init failed: {result.stderr}"
        
        # Create snapshot
        result = subprocess.run([
            "python3", str(scripts_dir / "dtt"), "snapshot", 
            "--description", "Test snapshot"
        ], cwd=test_root, capture_output=True, text=True)
        
        assert result.returncode == 0, f"DTT snapshot failed: {result.stderr}"
        
        # Verify all bricks
        result = subprocess.run([
            "python3", str(scripts_dir / "dtt"), "verify", "all"
        ], cwd=test_root, capture_output=True, text=True)
        
        assert result.returncode == 0, f"DTT verify failed: {result.stderr}"
        assert "All bricks verified successfully" in result.stdout
        
        print("✅ DTT vault verification test passed")





def run_all_tests():
    """Run all DTT tests"""
    print("🚀 Running DTT Vault Test Suite")
    print("=" * 50)
    
    tests = [
        test_dtt_vault_initialization,
        test_dtt_snapshot_creation,
        test_dtt_vault_verification
    ]
    
    passed = 0
    failed = 0
    
    for test in tests:
        try:
            test()
            passed += 1
        except Exception as e:
            print(f"❌ {test.__name__} failed: {e}")
            failed += 1
            
        print()
        
    print("=" * 50)
    print(f"📊 Test Results: {passed} passed, {failed} failed")
    
    if failed > 0:
        print("❌ Some tests failed")
        return 1
    else:
        print("✅ All tests passed!")
        return 0


if __name__ == "__main__":
    sys.exit(run_all_tests())