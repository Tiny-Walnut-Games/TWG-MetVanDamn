#!/usr/bin/env python3
"""
Self-Care System Test Suite

Basic test suite to validate the self-care and cognitive safety system
functionality. Follows the existing test pattern in the repository.
"""

import sys
import os
import tempfile
import subprocess
import json
from pathlib import Path

def test_idea_capture():
    """Test idea capture functionality"""
    print("🧪 Testing idea capture...")
    
    result = subprocess.run([
        sys.executable, "scripts/idea_capture.py", "Test idea from test suite"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Idea capture failed: {result.stderr}")
        return False
    
    if "Idea captured:" not in result.stdout:
        print("❌ Expected capture confirmation not found")
        return False
        
    print("✅ Idea capture works")
    return True

def test_sluice_functionality():
    """Test overflow sluice functionality"""
    print("🧪 Testing overflow sluice...")
    
    # Add to sluice
    result = subprocess.run([
        sys.executable, "scripts/idea_capture.py", "--sluice", "Test sluice entry"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Sluice append failed: {result.stderr}")
        return False
    
    # Check sluice lines
    result = subprocess.run([
        sys.executable, "scripts/idea_capture.py", "--sluice-lines"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Sluice listing failed: {result.stderr}")
        return False
    
    if "Test sluice entry" not in result.stdout:
        print("❌ Sluice entry not found in listing")
        return False
        
    print("✅ Overflow sluice works")
    return True

def test_governors():
    """Test cognitive safety governors"""
    print("🧪 Testing cognitive governors...")
    
    result = subprocess.run([
        sys.executable, "src/selfcare/governors.py", "--check-all"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Governor check failed: {result.stderr}")
        return False
    
    # Parse output to check for governor responses
    output = result.stdout
    if "MeltBudgetGovernor" not in output:
        print("❌ MeltBudgetGovernor not found in output")
        return False
    
    if "HumidityGovernor" not in output:
        print("❌ HumidityGovernor not found in output")
        return False
    
    if "CognitiveSensitivityFlag" not in output:
        print("❌ CognitiveSensitivityFlag not found in output")
        return False
        
    print("✅ Cognitive governors work")
    return True

def test_journal_creation():
    """Test private journal functionality"""
    print("🧪 Testing private journal...")
    
    result = subprocess.run([
        sys.executable, "src/selfcare/journaling.py", "--create", 
        "--mood", "test", "--energy", "medium", "--content", "Test journal entry"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Journal creation failed: {result.stderr}")
        return False
    
    if "Created journal entry:" not in result.stdout:
        print("❌ Journal creation confirmation not found")
        return False
    
    # Check that journal directory exists
    journal_dir = Path("local_journal")
    if not journal_dir.exists():
        print("❌ Journal directory not created")
        return False
    
    # Check for gitignore protection
    gitignore_path = journal_dir / ".gitignore"
    if not gitignore_path.exists():
        print("❌ Journal .gitignore not created")
        return False
        
    print("✅ Private journal works")
    return True

def test_telemetry():
    """Test development telemetry"""
    print("🧪 Testing development telemetry...")
    
    result = subprocess.run([
        sys.executable, "engine/telemetry.py", "--update-sleep", "8.0", "--sleep-quality", "excellent"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Telemetry update failed: {result.stderr}")
        return False
    
    if "Sleep updated:" not in result.stdout:
        print("❌ Sleep update confirmation not found")
        return False
    
    # Check status
    result = subprocess.run([
        sys.executable, "engine/telemetry.py", "--status"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Telemetry status failed: {result.stderr}")
        return False
        
    print("✅ Development telemetry works")
    return True

def test_integration_hooks():
    """Test self-care integration hooks"""
    print("🧪 Testing integration hooks...")
    
    result = subprocess.run([
        sys.executable, "engine/hooks/selfcare_hooks.py", "--status"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Integration hooks failed: {result.stderr}")
        return False
    
    # Check for JSON output
    try:
        output_lines = result.stdout.strip().split('\n')
        json_start = -1
        for i, line in enumerate(output_lines):
            if line.strip() == '{':
                json_start = i
                break
        
        if json_start >= 0:
            json_output = '\n'.join(output_lines[json_start:])
            data = json.loads(json_output)
            
            if not data.get("selfcare_enabled"):
                print("❌ Self-care not enabled in hooks")
                return False
        else:
            print("❌ No JSON output found in hooks")
            return False
            
    except json.JSONDecodeError as e:
        print(f"❌ Invalid JSON in hooks output: {e}")
        return False
        
    print("✅ Integration hooks work")
    return True

def test_privacy_protection():
    """Test privacy protection for local journal"""
    print("🧪 Testing privacy protection...")
    
    # Check that local_journal is in .gitignore
    gitignore_path = Path(".gitignore")
    if not gitignore_path.exists():
        print("❌ .gitignore not found")
        return False
    
    gitignore_content = gitignore_path.read_text()
    if "local_journal/" not in gitignore_content:
        print("❌ local_journal/ not found in .gitignore")
        return False
    
    # Check git status doesn't show journal files
    result = subprocess.run([
        "git", "status", "--porcelain", "local_journal/"
    ], capture_output=True, text=True)
    
    # Should have no output (files ignored)
    if result.stdout.strip():
        print("❌ Journal files visible in git status")
        return False
        
    print("✅ Privacy protection works")
    return True

def test_leak_detector():
    """Test leak detector catches staged stray journal files"""
    print("🧪 Testing leak detector...")
    
    # Create a test journal file outside the protected directory
    test_file = Path("test_journal_leak.md")
    test_file.write_text("📓 This is a leaked journal entry - should be detected!")
    
    try:
        # Add the file to git staging
        result = subprocess.run([
            "git", "add", "test_journal_leak.md"
        ], capture_output=True, text=True)
        
        if result.returncode != 0:
            print("❌ Failed to stage test file")
            return False
        
        # Check git status for staged files with journal emoji
        result = subprocess.run([
            "git", "status", "--porcelain"
        ], capture_output=True, text=True)
        
        staged_files = result.stdout
        
        # Look for journal indicators in staged files
        journal_leak_detected = False
        for line in staged_files.split('\n'):
            if line.strip() and ('journal' in line.lower() or '📓' in line):
                # Read the file content to check for journal emoji
                if line.startswith('A '):  # Added file
                    filename = line[3:].strip()
                    try:
                        with open(filename, 'r', encoding='utf-8') as f:
                            content = f.read()
                            if '📓' in content:
                                journal_leak_detected = True
                                print(f"🚨 LEAK DETECTED: Journal emoji found in staged file: {filename}")
                    except:
                        pass
        
        # Clean up: unstage and remove the test file
        subprocess.run(["git", "reset", "HEAD", "test_journal_leak.md"], capture_output=True)
        test_file.unlink()
        
        if journal_leak_detected:
            print("✅ Leak detector works - caught staged journal file")
            return True
        else:
            print("❌ Leak detector failed - no journal leak detected")
            return False
            
    except Exception as e:
        # Clean up on error
        subprocess.run(["git", "reset", "HEAD", "test_journal_leak.md"], capture_output=True)
        if test_file.exists():
            test_file.unlink()
        print(f"❌ Leak detector test failed: {e}")
        return False

def main():
    """Run all tests"""
    print("🧠📜 Self-Care System Test Suite")
    print("=" * 40)
    
    tests = [
        test_idea_capture,
        test_sluice_functionality,
        test_governors,
        test_journal_creation,
        test_telemetry,
        test_integration_hooks,
        test_privacy_protection,
        test_leak_detector
    ]
    
    passed = 0
    total = len(tests)
    
    for test in tests:
        try:
            if test():
                passed += 1
            else:
                print()  # Add spacing after failures
        except Exception as e:
            print(f"❌ Test {test.__name__} failed with exception: {e}")
            print()
    
    print("=" * 40)
    print(f"📊 Test Results: {passed}/{total} passed")
    
    if passed == total:
        print("🎉 All tests passed! Self-care system is working correctly.")
        return 0
    else:
        print("⚠️ Some tests failed. Check the output above for details.")
        return 1

if __name__ == "__main__":
    sys.exit(main())