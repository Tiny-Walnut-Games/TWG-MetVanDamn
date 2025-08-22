#!/usr/bin/env python3
"""
Agent Profile System Test Runner
Validates agent profile configurations and LDA CLI functionality in a simple, reliable way.
"""

import os
import sys
import yaml
import subprocess
import tempfile
import shutil
from pathlib import Path

def run_test(test_name, test_func):
    """Run a test function and return success/failure"""
    print(f"🧪 Running: {test_name}")
    try:
        test_func()
        print(f"   ✅ PASSED")
        return True
    except Exception as e:
        print(f"   ❌ FAILED: {str(e)}")
        return False

def test_agent_profile_yaml_syntax():
    """Test that agent profile YAML files have valid syntax"""
    # Test main profile
    with open('.agent-profile.yaml', 'r') as f:
        profile = yaml.safe_load(f)
    assert isinstance(profile, dict), 'Profile must be a dictionary'
    
    # Test legacy profile if exists
    if Path('agent-profile.yaml').exists():
        with open('agent-profile.yaml', 'r') as f:
            legacy_profile = yaml.safe_load(f)
        assert isinstance(legacy_profile, dict), 'Legacy profile must be a dictionary'

def test_required_profile_fields():
    """Test that agent profiles contain required fields"""
    with open('.agent-profile.yaml', 'r') as f:
        profile = yaml.safe_load(f)
    
    required_sections = ['agent_personality', 'workflow_preferences', 'behavior_flags']
    for section in required_sections:
        assert section in profile, f'Missing required section: {section}'
    
    personality = profile.get('agent_personality', {})
    assert 'tone' in personality, 'agent_personality must specify tone'
    
    workflow = profile.get('workflow_preferences', {})
    assert 'mode' in workflow, 'workflow_preferences must specify mode'

def test_valid_field_values():
    """Test that field values are from expected sets"""
    with open('.agent-profile.yaml', 'r') as f:
        profile = yaml.safe_load(f)
    
    # Validate tone
    personality = profile.get('agent_personality', {})
    tone = personality.get('tone')
    if tone:
        valid_tones = {'professional', 'friendly', 'dry_humor', 'sarcastic', 'enthusiastic'}
        assert tone in valid_tones, f'Invalid tone: {tone}'
    
    # Validate mode
    workflow = profile.get('workflow_preferences', {})
    mode = workflow.get('mode')
    if mode:
        valid_modes = {'exploration', 'implementation', 'documentation', 'crisis', 'standard'}
        assert mode in valid_modes, f'Invalid mode: {mode}'

def test_lda_cli_availability():
    """Test that LDA CLI tool is available and functional"""
    lda_path = Path('scripts/lda')
    assert lda_path.exists(), 'LDA CLI tool not found at scripts/lda'
    
    # Test that it can show help
    result = subprocess.run([sys.executable, str(lda_path), '--help'], 
                          capture_output=True, text=True)
    assert result.returncode == 0, f'LDA CLI help failed: {result.stderr}'

def test_lda_cli_basic_commands():
    """Test basic LDA CLI commands"""
    original_cwd = Path.cwd()
    try:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_path = Path(temp_dir)
            os.chdir(temp_path)
            
            # Initialize git repo
            subprocess.run(['git', 'init'], capture_output=True, check=False)
            subprocess.run(['git', 'config', 'user.name', 'Test User'], capture_output=True, check=False)
            subprocess.run(['git', 'config', 'user.email', 'test@example.com'], capture_output=True, check=False)
            
            # Test LDA init
            project_root = Path(__file__).parent.parent.parent
            lda_path = project_root / 'scripts' / 'lda'
            
            env = os.environ.copy()
            env['PYTHONPATH'] = str(project_root / 'scripts')
            
            result = subprocess.run([sys.executable, str(lda_path), 'init', '--quiet', '--force'],
                                  capture_output=True, text=True, env=env)
            assert result.returncode == 0, f'LDA init failed: {result.stderr}'
            
            # Verify profile was created
            profile_path = temp_path / 'agent-profile.yaml'
            assert profile_path.exists(), 'LDA init did not create profile'
    finally:
        os.chdir(original_cwd)

def test_pipeline_preferences_validation():
    """Test pipeline preferences in legacy profile if present"""
    legacy_profile_path = Path('agent-profile.yaml')
    if not legacy_profile_path.exists():
        return  # Optional test
        
    with open(legacy_profile_path, 'r') as f:
        profile = yaml.safe_load(f)
    
    if 'pipeline_preferences' in profile:
        pipelines = profile['pipeline_preferences']
        assert isinstance(pipelines, list), 'pipeline_preferences must be a list'
        
        valid_pipelines = {'URP', 'HDRP', 'BRP', 'SRP'}
        for pipeline in pipelines:
            assert pipeline in valid_pipelines, f'Invalid pipeline: {pipeline}'

def test_cli_integration_commands():
    """Test CLI integration command definitions"""
    with open('.agent-profile.yaml', 'r') as f:
        profile = yaml.safe_load(f)
    
    cli_integration = profile.get('cli_integration', {})
    if 'available_commands' in cli_integration:
        commands = cli_integration['available_commands']
        assert isinstance(commands, list), 'available_commands must be a list'
        
        for cmd in commands:
            assert isinstance(cmd, str), f'Command must be string: {cmd}'
            assert cmd.startswith('lda '), f'Command must start with "lda ": {cmd}'

def test_project_context_validation():
    """Test project context if present"""
    with open('.agent-profile.yaml', 'r') as f:
        profile = yaml.safe_load(f)
    
    project_context = profile.get('project_context', {})
    if project_context:
        assert 'name' in project_context, 'project_context must have name'
        assert 'domain' in project_context, 'project_context must have domain'

def main():
    """Run all agent profile tests"""
    print("🧬 Agent Profile System - Test Suite")
    print("===================================")
    print()
    
    # Change to project root
    project_root = Path(__file__).parent.parent.parent
    original_cwd = Path.cwd()
    os.chdir(project_root)
    
    try:
        # Define tests
        tests = [
            ("Agent Profile YAML Syntax", test_agent_profile_yaml_syntax),
            ("Required Profile Fields", test_required_profile_fields),
            ("Valid Field Values", test_valid_field_values),
            ("LDA CLI Tool Availability", test_lda_cli_availability),
            ("LDA CLI Basic Commands", test_lda_cli_basic_commands),
            ("Pipeline Preferences Validation", test_pipeline_preferences_validation),
            ("CLI Integration Commands", test_cli_integration_commands),
            ("Project Context Validation", test_project_context_validation),
        ]
        
        # Run tests
        passed = 0
        failed = 0
        results = []
        
        for test_name, test_func in tests:
            if run_test(test_name, test_func):
                passed += 1
                results.append(f"✅ {test_name}")
            else:
                failed += 1
                results.append(f"❌ {test_name}")
            print()
        
        # Print results
        print("📊 Agent Profile System Test Results")
        print("====================================")
        print()
        
        for result in results:
            print(result)
        
        print()
        print("📈 Summary:")
        print(f"   ✅ Passed: {passed}")
        print(f"   ❌ Failed: {failed}")
        print(f"   📊 Total:  {passed + failed}")
        
        if failed == 0:
            print()
            print("🎉 All tests passed! Agent Profile System is healthy.")
            print("🛡️ The cheeks are preserved! All hail the Cheeks! 🙌")
            return 0
        else:
            print()
            print("💥 Some tests failed. Agent Profile System needs attention.")
            print("🛡️ Cheek preservation protocols activated. Fix the issues above.")
            return 1
            
    finally:
        os.chdir(original_cwd)

if __name__ == "__main__":
    sys.exit(main())