#!/usr/bin/env python3
"""
Agent Profile System Tests
Tests for validating agent profile configurations and LDA CLI functionality.
"""

import unittest
import yaml
import os
import sys
import tempfile
import shutil
from pathlib import Path

# Add the scripts directory to Python path for importing LDA
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '..', 'scripts'))

try:
    from lda import LDAConfig, LDACLICommands
except ImportError:
    print("⚠️ LDA module not found - tests will be limited to YAML validation")
    LDAConfig = None
    LDACLICommands = None

class TestAgentProfileValidation(unittest.TestCase):
    """Test agent profile YAML validation"""
    
    def setUp(self):
        """Set up test environment"""
        self.test_dir = Path(__file__).parent.parent.parent
        self.agent_profile_path = self.test_dir / ".agent-profile.yaml"
        self.legacy_profile_path = self.test_dir / "agent-profile.yaml"
        
    def test_agent_profile_yaml_syntax(self):
        """Test that agent profile YAML files have valid syntax"""
        profiles_to_test = []
        
        if self.agent_profile_path.exists():
            profiles_to_test.append(self.agent_profile_path)
            
        if self.legacy_profile_path.exists():
            profiles_to_test.append(self.legacy_profile_path)
            
        self.assertTrue(len(profiles_to_test) > 0, "No agent profile files found to test")
        
        for profile_path in profiles_to_test:
            with self.subTest(profile=profile_path.name):
                try:
                    with open(profile_path, 'r') as f:
                        yaml_data = yaml.safe_load(f)
                    self.assertIsInstance(yaml_data, dict, f"Profile {profile_path.name} should contain a dictionary")
                except yaml.YAMLError as e:
                    self.fail(f"YAML syntax error in {profile_path.name}: {e}")
                except Exception as e:
                    self.fail(f"Error loading {profile_path.name}: {e}")
    
    def test_required_profile_fields(self):
        """Test that agent profiles contain required fields"""
        if not self.agent_profile_path.exists():
            self.skipTest("No .agent-profile.yaml found")
            
        with open(self.agent_profile_path, 'r') as f:
            profile = yaml.safe_load(f)
            
        # Test structure of the detailed profile
        self.assertIn('agent_personality', profile, "Profile should have agent_personality section")
        self.assertIn('workflow_preferences', profile, "Profile should have workflow_preferences section")
        self.assertIn('behavior_flags', profile, "Profile should have behavior_flags section")
        
        personality = profile.get('agent_personality', {})
        self.assertIn('tone', personality, "agent_personality should specify tone")
        
        workflow = profile.get('workflow_preferences', {})
        self.assertIn('mode', workflow, "workflow_preferences should specify mode")
        
    def test_legacy_profile_compatibility(self):
        """Test that legacy agent-profile.yaml is compatible"""
        if not self.legacy_profile_path.exists():
            self.skipTest("No legacy agent-profile.yaml found")
            
        with open(self.legacy_profile_path, 'r') as f:
            profile = yaml.safe_load(f)
            
        # Test basic structure
        self.assertIn('name', profile, "Legacy profile should have name")
        self.assertIn('description', profile, "Legacy profile should have description")
        
        # Test behavioral settings exist
        if 'behavior' in profile:
            behavior = profile['behavior']
            self.assertIsInstance(behavior, dict, "behavior section should be a dictionary")
            
        # Test pipeline preferences if they exist
        if 'pipeline_preferences' in profile:
            pipelines = profile['pipeline_preferences']
            self.assertIsInstance(pipelines, list, "pipeline_preferences should be a list")
            valid_pipelines = {'URP', 'HDRP', 'BRP', 'SRP'}
            for pipeline in pipelines:
                self.assertIn(pipeline, valid_pipelines, 
                             f"Unknown pipeline preference: {pipeline}")
                             
    def test_tone_values(self):
        """Test that tone values are from expected set"""
        if not self.agent_profile_path.exists():
            self.skipTest("No .agent-profile.yaml found")
            
        with open(self.agent_profile_path, 'r') as f:
            profile = yaml.safe_load(f)
            
        personality = profile.get('agent_personality', {})
        tone = personality.get('tone')
        
        if tone:
            valid_tones = {'professional', 'friendly', 'dry_humor', 'sarcastic', 'enthusiastic'}
            self.assertIn(tone, valid_tones, f"Invalid tone value: {tone}")
            
    def test_mode_values(self):
        """Test that workflow mode values are valid"""
        if not self.agent_profile_path.exists():
            self.skipTest("No .agent-profile.yaml found")
            
        with open(self.agent_profile_path, 'r') as f:
            profile = yaml.safe_load(f)
            
        workflow = profile.get('workflow_preferences', {})
        mode = workflow.get('mode')
        
        if mode:
            valid_modes = {'exploration', 'implementation', 'documentation', 'crisis', 'standard'}
            self.assertIn(mode, valid_modes, f"Invalid workflow mode: {mode}")

class TestLDACLITool(unittest.TestCase):
    """Test LDA CLI tool functionality"""
    
    def setUp(self):
        """Set up test environment"""
        if LDAConfig is None:
            self.skipTest("LDA module not available for testing")
            
        # Create temporary directory for testing
        self.temp_dir = Path(tempfile.mkdtemp())
        self.original_cwd = Path.cwd()
        os.chdir(self.temp_dir)
        
        # Create a minimal git repo for testing
        try:
            os.system("git init >/dev/null 2>&1")
            os.system("git config user.name 'Test User' >/dev/null 2>&1")
            os.system("git config user.email 'test@example.com' >/dev/null 2>&1")
        except:
            pass  # Git setup is optional
            
    def tearDown(self):
        """Clean up test environment"""
        os.chdir(self.original_cwd)
        shutil.rmtree(self.temp_dir, ignore_errors=True)
        
    def test_lda_config_init(self):
        """Test LDA configuration initialization"""
        config = LDAConfig()
        self.assertIsInstance(config, LDAConfig)
        self.assertIsInstance(config.project_root, Path)
        
    def test_default_profile_generation(self):
        """Test default profile generation"""
        config = LDAConfig()
        default_profile = config.get_default_profile()
        
        self.assertIsInstance(default_profile, dict)
        self.assertIn('name', default_profile)
        self.assertIn('description', default_profile)
        self.assertIn('behavior', default_profile)
        self.assertEqual(default_profile['name'], 'default')
        
    def test_profile_loading_when_missing(self):
        """Test profile loading when file doesn't exist"""
        config = LDAConfig()
        profile = config.load_profile()
        
        # Should return default profile when file doesn't exist
        self.assertIsInstance(profile, dict)
        self.assertEqual(profile['name'], 'default')
        
    def test_profile_creation_and_loading(self):
        """Test creating and loading a profile"""
        config = LDAConfig()
        
        # Create a test profile
        test_profile = {
            'name': 'test_profile',
            'description': 'Test profile for unit tests',
            'behavior': {'tone': 'friendly'},
            'flags': {'dry_run': True}
        }
        
        # Write profile to file
        config.agent_profile_path.parent.mkdir(parents=True, exist_ok=True)
        with open(config.agent_profile_path, 'w') as f:
            yaml.dump(test_profile, f)
            
        # Load and verify
        loaded_profile = config.load_profile()
        self.assertEqual(loaded_profile['name'], 'test_profile')
        self.assertEqual(loaded_profile['description'], 'Test profile for unit tests')
        
    def test_lda_cli_commands_init(self):
        """Test LDA CLI commands initialization"""
        commands = LDACLICommands()
        self.assertIsInstance(commands, LDACLICommands)
        self.assertIsInstance(commands.config, LDAConfig)

class TestAgentProfileStructures(unittest.TestCase):
    """Test specific agent profile configuration structures"""
    
    def setUp(self):
        """Set up test data"""
        self.test_dir = Path(__file__).parent.parent.parent
        
    def test_cli_integration_commands(self):
        """Test CLI integration command definitions"""
        profile_path = self.test_dir / ".agent-profile.yaml"
        if not profile_path.exists():
            self.skipTest("No .agent-profile.yaml found")
            
        with open(profile_path, 'r') as f:
            profile = yaml.safe_load(f)
            
        cli_integration = profile.get('cli_integration', {})
        if 'available_commands' in cli_integration:
            commands = cli_integration['available_commands']
            self.assertIsInstance(commands, list, "available_commands should be a list")
            
            # Check that commands are strings
            for cmd in commands:
                self.assertIsInstance(cmd, str, f"Command should be a string: {cmd}")
                self.assertTrue(cmd.startswith('lda '), f"Command should start with 'lda ': {cmd}")
                
    def test_communication_patterns(self):
        """Test communication patterns configuration"""
        profile_path = self.test_dir / ".agent-profile.yaml"
        if not profile_path.exists():
            self.skipTest("No .agent-profile.yaml found")
            
        with open(profile_path, 'r') as f:
            profile = yaml.safe_load(f)
            
        comm_patterns = profile.get('communication_patterns', {})
        if comm_patterns:
            # Test that pattern values are reasonable
            pattern_values = set(comm_patterns.values())
            expected_patterns = {
                'boss_encounter', 'achievement_unlock', 'dungeon_crawl', 
                'guild_meeting', 'scroll_writing'
            }
            
            # At least some expected patterns should be present
            self.assertTrue(len(pattern_values & expected_patterns) > 0,
                           "Should contain some expected communication patterns")
                           
    def test_emergency_settings(self):
        """Test emergency settings configuration"""
        profile_path = self.test_dir / ".agent-profile.yaml"
        if not profile_path.exists():
            self.skipTest("No .agent-profile.yaml found")
            
        with open(profile_path, 'r') as f:
            profile = yaml.safe_load(f)
            
        emergency = profile.get('emergency_settings', {})
        if emergency:
            # Test crisis triggers
            if 'crisis_mode_triggers' in emergency:
                triggers = emergency['crisis_mode_triggers']
                self.assertIsInstance(triggers, list, "crisis_mode_triggers should be a list")
                
            # Test crisis response settings
            if 'crisis_response' in emergency:
                response = emergency['crisis_response']
                self.assertIsInstance(response, dict, "crisis_response should be a dictionary")

def run_tests():
    """Run all agent profile tests"""
    print("🧪 Running Agent Profile System Tests")
    print("=====================================")
    
    # Create test suite
    suite = unittest.TestSuite()
    
    # Add test classes
    for test_class in [TestAgentProfileValidation, TestLDACLITool, TestAgentProfileStructures]:
        tests = unittest.TestLoader().loadTestsFromTestCase(test_class)
        suite.addTests(tests)
    
    # Run tests with verbose output
    runner = unittest.TextTestRunner(verbosity=2, buffer=True)
    result = runner.run(suite)
    
    # Print summary
    print("\n📊 Test Results Summary")
    print("======================")
    print(f"✅ Tests Run: {result.testsRun}")
    print(f"❌ Failures: {len(result.failures)}")
    print(f"⚠️  Errors: {len(result.errors)}")
    print(f"⏭️  Skipped: {len(result.skipped) if hasattr(result, 'skipped') else 0}")
    
    if result.failures:
        print("\n💥 Failures:")
        for test, traceback in result.failures:
            print(f"  - {test}: {traceback.split('AssertionError: ')[-1].split('\\n')[0]}")
    
    if result.errors:
        print("\n🔥 Errors:")
        for test, traceback in result.errors:
            print(f"  - {test}: {traceback.split('\\n')[-2]}")
    
    # Return success/failure
    return len(result.failures) == 0 and len(result.errors) == 0

if __name__ == "__main__":
    success = run_tests()
    sys.exit(0 if success else 1)