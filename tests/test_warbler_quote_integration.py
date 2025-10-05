#!/usr/bin/env python3
"""
🧪 Warbler Quote Engine Integration Test Suite
Comprehensive testing for the enhanced Scroll Quote Engine with Warbler integration

Tests both static quote functionality and dynamic Warbler-powered generation
to ensure the sacred wisdom flows correctly through all channels.
"""

import sys
import os
import json
import tempfile
import subprocess
import unittest
from pathlib import Path
from unittest.mock import patch, MagicMock

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root / "src"))

try:
    from ScrollQuoteEngine.warbler_quote_engine import WarblerPoweredScrollEngine, WisdomQuote
except ImportError as e:
    print(f"❌ Failed to import Warbler Quote Engine: {e}")
    sys.exit(1)

class TestWarblerQuoteEngine(unittest.TestCase):
    """Test suite for the Warbler-powered quote engine"""
    
    def setUp(self):
        """Set up test environment"""
        self.test_dir = Path(tempfile.mkdtemp())
        self.quotes_file = self.test_dir / "test_quotes.yaml"
        self.cache_file = self.test_dir / "test_cache.json"
        
        # Create test quotes database
        test_quotes_yaml = """
quotes:
  general:
    - text: "Test quote for general wisdom"
      author: "Test Sage"
      source: "Test Scrolls"
      volume: "Vol. I"
      tags: ["test", "general"]
      buttsafe_certified: true
  
  development:
    - text: "Test development wisdom"
      author: "Code Master"
      source: "Development Arts"
      volume: "Vol. II"
      tags: ["test", "development"] 
      buttsafe_certified: true

database_info:
  version: "test-1.0.0"
  total_quotes: 2
"""
        
        with open(self.quotes_file, 'w') as f:
            f.write(test_quotes_yaml)
        
        # Create test Warbler templates
        self.warbler_pack_dir = self.test_dir / "warbler-pack"
        self.warbler_pack_dir.mkdir()
        pack_dir = self.warbler_pack_dir / "pack"
        pack_dir.mkdir()
        
        test_templates = {
            "packInfo": {
                "name": "test-wisdom-pack",
                "version": "1.0.0",
                "description": "Test templates"
            },
            "templates": [
                {
                    "id": "test_wisdom_template",
                    "version": "1.0.0",
                    "title": "Test Wisdom",
                    "description": "Test template",
                    "content": "{{action}} is {{truth}}.",
                    "intent": "test_wisdom",
                    "requiredSlots": [
                        {
                            "name": "action",
                            "type": "string",
                            "required": True,
                            "description": "An action"
                        },
                        {
                            "name": "truth", 
                            "type": "string",
                            "required": True,
                            "description": "A truth"
                        }
                    ],
                    "tags": ["test", "wisdom"],
                    "category": "test"
                }
            ]
        }
        
        with open(pack_dir / "templates.json", 'w') as f:
            json.dump(test_templates, f, indent=2)
    
    def tearDown(self):
        """Clean up test environment"""
        import shutil
        shutil.rmtree(self.test_dir, ignore_errors=True)
    
    def test_static_quote_loading(self):
        """Test loading of static quotes database"""
        # Create engine with test database
        engine = WarblerPoweredScrollEngine(str(self.quotes_file))
        
        # Verify quotes loaded correctly
        self.assertIn('general', engine.static_quotes)
        self.assertIn('development', engine.static_quotes) 
        self.assertEqual(len(engine.static_quotes['general']), 1)
        self.assertEqual(len(engine.static_quotes['development']), 1)
        
        # Check quote content
        general_quote = engine.static_quotes['general'][0]
        self.assertEqual(general_quote.text, "Test quote for general wisdom")
        self.assertEqual(general_quote.author, "Test Sage")
        self.assertTrue(general_quote.buttsafe_certified)
        self.assertFalse(general_quote.generated)
    
    def test_quote_retrieval_by_category(self):
        """Test quote retrieval by category"""
        engine = WarblerPoweredScrollEngine(str(self.quotes_file))
        
        # Get development quote
        quote = engine.get_quote(category='development')
        self.assertIsInstance(quote, WisdomQuote)
        self.assertEqual(quote.text, "Test development wisdom")
        
        # Get general quote
        quote = engine.get_quote(category='general')
        self.assertEqual(quote.text, "Test quote for general wisdom")
    
    def test_context_aware_selection(self):
        """Test context-aware quote selection"""
        engine = WarblerPoweredScrollEngine(str(self.quotes_file))
        
        # Test development context
        quote = engine.get_quote_for_context('development')
        self.assertIsInstance(quote, WisdomQuote)
        
        # Test documentation context (should fall back to general)
        quote = engine.get_quote_for_context('documentation')
        self.assertIsInstance(quote, WisdomQuote)
    
    def test_quote_formatting(self):
        """Test different quote formatting options"""
        engine = WarblerPoweredScrollEngine(str(self.quotes_file))
        quote = engine.get_quote(category='general')
        
        # Test CLI format
        cli_format = engine.format_quote(quote, 'cli')
        self.assertIn('📜', cli_format)
        self.assertIn('🪶', cli_format)
        self.assertIn(quote.text, cli_format)
        
        # Test markdown format
        md_format = engine.format_quote(quote, 'markdown')
        self.assertIn('>', md_format)
        self.assertIn('**', md_format)
        self.assertIn(quote.text, md_format)
        
        # Test plain format
        plain_format = engine.format_quote(quote, 'plain')
        self.assertIn(quote.text, plain_format)
        self.assertIn(quote.author, plain_format)
    
    def test_generated_quote_cache(self):
        """Test generated quote caching functionality"""
        # Mock the engine to use test directories
        with patch.object(WarblerPoweredScrollEngine, '__init__') as mock_init:
            mock_init.return_value = None
            engine = WarblerPoweredScrollEngine()
            
            # Manually set up test paths
            engine.quotes_path = self.quotes_file
            engine.generated_quotes_cache = self.cache_file
            engine.warbler_pack_path = self.warbler_pack_dir
            engine.static_quotes = {}
            engine.generated_quotes = []
            
            # Test cache saving
            test_quote = WisdomQuote(
                text="Generated test wisdom",
                author="Test Oracle",
                source="Generated Scrolls",
                volume="Vol. Generated",
                generated=True,
                generation_date="2025-01-20T12:00:00Z",
                template_id="test_template"
            )
            
            engine.generated_quotes = [test_quote]
            engine._save_generated_cache()
            
            # Verify cache file was created
            self.assertTrue(self.cache_file.exists())
            
            # Test cache loading
            engine.generated_quotes = []
            loaded_quotes = engine._load_generated_cache()
            
            self.assertEqual(len(loaded_quotes), 1)
            self.assertEqual(loaded_quotes[0].text, "Generated test wisdom")
            self.assertTrue(loaded_quotes[0].generated)
    
    @patch('random.choice')
    def test_template_slot_filling(self, mock_choice):
        """Test Warbler template slot filling"""
        # Set up predictable random choices
        mock_choice.side_effect = lambda x: x[0] if x else ""
        
        # Mock the engine initialization
        with patch.object(WarblerPoweredScrollEngine, '__init__') as mock_init:
            mock_init.return_value = None
            engine = WarblerPoweredScrollEngine()
            
            # Set up necessary attributes
            engine.warbler_pack_path = self.warbler_pack_dir
            engine.generated_quotes_cache = self.cache_file
            engine.static_quotes = {}
            engine.generated_quotes = []
            engine.warbler_available = True
            
            # Import the slot data class
            from ScrollQuoteEngine.warbler_quote_engine import WarblerSlotData
            engine.slot_data = WarblerSlotData()
            
            # Test slot value retrieval
            action_value = engine._get_slot_value('action')
            self.assertIsInstance(action_value, str)
            self.assertGreater(len(action_value), 0)
            
            # Test with unknown slot (should return placeholder)
            unknown_value = engine._get_slot_value('unknown_slot')
            self.assertEqual(unknown_value, '[unknown_slot]')
    
    def test_statistics_generation(self):
        """Test statistics generation"""
        engine = WarblerPoweredScrollEngine(str(self.quotes_file))
        
        stats = engine.show_statistics()
        
        self.assertIn('static_quotes', stats)
        self.assertIn('generated_quotes', stats)
        self.assertIn('total_quotes', stats)
        self.assertIn('categories', stats)
        self.assertIn('warbler_available', stats)
        
        # Verify static quote count
        self.assertEqual(stats['static_quotes'], 2)  # general + development
        self.assertEqual(stats['categories'], 2)

class TestCLIIntegration(unittest.TestCase):
    """Test CLI script integration"""
    
    def setUp(self):
        """Set up CLI test environment"""
        self.project_root = Path(__file__).parent.parent
        self.lda_quote_script = self.project_root / "scripts" / "lda-quote"
    
    def test_lda_quote_script_exists(self):
        """Test that the lda-quote script exists and is executable"""
        self.assertTrue(self.lda_quote_script.exists())
    
    def test_warbler_pack_templates_exist(self):
        """Test that Warbler pack templates exist"""
        templates_file = self.project_root / "packs" / "warbler-pack-wisdom-scrolls" / "pack" / "templates.json"
        self.assertTrue(templates_file.exists())
        
        # Validate template structure
        with open(templates_file, 'r') as f:
            data = json.load(f)
        
        self.assertIn('packInfo', data)
        self.assertIn('templates', data)
        self.assertGreater(len(data['templates']), 0)
        
        # Check first template structure
        template = data['templates'][0]
        required_fields = ['id', 'version', 'title', 'description', 'content', 'requiredSlots']
        for field in required_fields:
            self.assertIn(field, template)
    
    def test_weekly_oracle_script_exists(self):
        """Test that weekly oracle script exists"""
        oracle_script = self.project_root / "scripts" / "weekly-wisdom-oracle.sh"
        self.assertTrue(oracle_script.exists())

class TestWarblerPackValidation(unittest.TestCase):
    """Test Warbler pack validation"""
    
    def setUp(self):
        """Set up validation test environment"""
        self.project_root = Path(__file__).parent.parent
        self.validator_script = self.project_root / "scripts" / "validate-warbler-pack.mjs"
        self.templates_file = self.project_root / "packs" / "warbler-pack-wisdom-scrolls" / "pack" / "templates.json"
    
    def test_pack_validation_script_exists(self):
        """Test that pack validation script exists"""
        self.assertTrue(self.validator_script.exists())
    
    def test_template_file_validation(self):
        """Test template file structure validation"""
        self.assertTrue(self.templates_file.exists())
        
        with open(self.templates_file, 'r') as f:
            data = json.load(f)
        
        # Validate pack info
        pack_info = data.get('packInfo', {})
        self.assertIn('name', pack_info)
        self.assertIn('version', pack_info)
        self.assertIn('description', pack_info)
        
        # Validate templates
        templates = data.get('templates', [])
        self.assertGreater(len(templates), 0)
        
        for template in templates:
            # Required fields
            self.assertIn('id', template)
            self.assertIn('content', template)
            self.assertIn('requiredSlots', template)
            
            # Content validation
            self.assertIsInstance(template['content'], str)
            self.assertGreater(len(template['content']), 0)
            
            # Slots validation
            self.assertIsInstance(template['requiredSlots'], list)
            
            for slot in template['requiredSlots']:
                self.assertIn('name', slot)
                self.assertIn('type', slot)
                self.assertIn('required', slot)

def run_integration_tests():
    """Run full integration test suite"""
    print("🧪 Running Warbler Quote Engine Integration Tests")
    print("=" * 60)
    
    # Create test suite
    loader = unittest.TestLoader()
    suite = unittest.TestSuite()
    
    # Add test classes
    suite.addTests(loader.loadTestsFromTestCase(TestWarblerQuoteEngine))
    suite.addTests(loader.loadTestsFromTestCase(TestCLIIntegration))
    suite.addTests(loader.loadTestsFromTestCase(TestWarblerPackValidation))
    
    # Run tests
    runner = unittest.TextTestRunner(verbosity=2)
    result = runner.run(suite)
    
    # Print summary
    print("\n" + "=" * 60)
    if result.wasSuccessful():
        print("✅ All tests passed! The Warbler Oracle is ready.")
    else:
        print(f"❌ {len(result.failures)} test(s) failed, {len(result.errors)} error(s)")
        print("🔧 The Oracle requires attention before deployment.")
    
    return result.wasSuccessful()

if __name__ == '__main__':
    success = run_integration_tests()
    sys.exit(0 if success else 1)
