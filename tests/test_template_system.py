#!/usr/bin/env python3
"""
Test suite for Comment Template Engine
Validates template generation and Chronicle Keeper integration
"""

import sys
import os
import tempfile
import subprocess
from pathlib import Path

def test_template_listing():
    """Test that template listing works"""
    print("🧪 Testing template listing...")
    
    result = subprocess.run([
        sys.executable, "src/CommentTemplateEngine/template_engine.py", "--list"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Template listing failed: {result.stderr}")
        return False
    
    if "bug_discovery" not in result.stdout:
        print("❌ Expected templates not found in listing")
        return False
        
    print("✅ Template listing works")
    return True

def test_template_generation():
    """Test template generation with defaults"""
    print("🧪 Testing template generation...")
    
    result = subprocess.run([
        sys.executable, "src/CommentTemplateEngine/template_engine.py",
        "--scenario", "bug_discovery", "--non-interactive"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Template generation failed: {result.stderr}")
        return False
    
    output = result.stdout
    if "📜 Bug Discovery Lore" not in output:
        print("❌ Generated template missing expected content")
        return False
        
    if "Chronicle Keeper Notes:" not in output:
        print("❌ Generated template missing Chronicle Keeper integration")
        return False
        
    print("✅ Template generation works")
    return True

def test_chronicle_keeper_triggers():
    """Test that generated templates contain Chronicle Keeper triggers"""
    print("🧪 Testing Chronicle Keeper trigger integration...")
    
    scenarios = ["bug_discovery", "debugging_ritual", "ci_failure_analysis", "lore_reflection"]
    
    for scenario in scenarios:
        result = subprocess.run([
            sys.executable, "src/CommentTemplateEngine/template_engine.py",
            "--scenario", scenario, "--non-interactive"
        ], capture_output=True, text=True)
        
        if result.returncode != 0:
            print(f"❌ Template generation failed for {scenario}: {result.stderr}")
            return False
        
        output = result.stdout
        # Check for Chronicle Keeper triggers
        if not ("📜" in output or "TLDL:" in output):
            print(f"❌ Template {scenario} missing Chronicle Keeper triggers")
            return False
            
        # Check for TLDL structure (flexible section names)
        required_keywords = ["Lore", "Lessons"]
        discovery_keywords = ["Discovery", "Quest", "Journey"]  # Allow different naming styles
        
        has_discovery_section = any(keyword in output for keyword in discovery_keywords)
        if not has_discovery_section:
            print(f"❌ Template {scenario} missing discovery section (looked for: {discovery_keywords})")
            return False
            
        for section in required_keywords:
            if section not in output:
                print(f"❌ Template {scenario} missing required section: {section}")
                return False
    
    print("✅ All templates contain proper Chronicle Keeper triggers and TLDL structure")
    return True

def test_lda_cli_integration():
    """Test LDA CLI template command integration"""
    print("🧪 Testing LDA CLI integration...")
    
    result = subprocess.run([
        "scripts/lda", "template", "--list"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ LDA template command failed: {result.stderr}")
        return False
    
    if "bug_discovery" not in result.stdout:
        print("❌ LDA CLI not properly integrated with template system")
        return False
        
    print("✅ LDA CLI integration works")
    return True

def test_template_search():
    """Test template search functionality"""
    print("🧪 Testing template search...")
    
    result = subprocess.run([
        "scripts/lda", "template", "--search", "debugging"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"❌ Template search failed: {result.stderr}")
        return False
    
    if "bug_discovery" not in result.stdout and "debugging_ritual" not in result.stdout:
        print("❌ Template search not returning expected results")
        return False
        
    print("✅ Template search works")
    return True

def main():
    """Run all template system tests"""
    print("🧠📜 Auto-Quills Comment Template System Tests")
    print("=" * 50)
    
    # Change to project root
    script_dir = Path(__file__).parent
    project_root = script_dir.parent
    os.chdir(project_root)
    
    tests = [
        test_template_listing,
        test_template_generation,
        test_chronicle_keeper_triggers,
        test_lda_cli_integration,
        test_template_search
    ]
    
    passed = 0
    total = len(tests)
    
    for test in tests:
        try:
            if test():
                passed += 1
            else:
                print()
        except Exception as e:
            print(f"❌ Test failed with exception: {e}")
            print()
    
    print("=" * 50)
    print(f"📊 Test Results: {passed}/{total} passed")
    
    if passed == total:
        print("🎉 All tests passed! Auto-quills system is working correctly.")
        return 0
    else:
        print("⚠️  Some tests failed. Review the output above.")
        return 1

if __name__ == "__main__":
    sys.exit(main())