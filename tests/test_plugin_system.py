"""
Tests for Plugin System v0.9

Tests the plugin architecture, sandboxing, and example plugins.
"""

import unittest
import time
import tempfile
import os
from unittest.mock import Mock, patch

# Import our plugin system
import sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'engine'))

from engine.plugins.base_plugin import (
    BasePlugin, CognitiveEventPlugin, PluginMetadata, PluginCapability
)
from engine.plugins.plugin_sandbox import PluginSandbox, SafePluginExecutor, TimeoutError, MemoryLimitError
from engine.plugins.plugin_manager import PluginManager
from engine.plugins.manifest_loader import ManifestLoader, ManifestValidationError
from engine.audio_event_bus import AudioEventBus, AudioEvent, AudioEventType


class MockPlugin(BasePlugin):
    """Mock plugin for testing."""
    
    def initialize(self, context):
        return True
    
    def process_event(self, event):
        return {"processed": True, "event_type": event.event_type.value}


class SlowPlugin(BasePlugin):
    """Plugin that takes too long to execute."""
    
    def initialize(self, context):
        return True
    
    def process_event(self, event):
        time.sleep(2)  # Simulate slow processing
        return {"processed": True}


class TestPluginSystem(unittest.TestCase):
    """Test suite for the plugin system."""
    
    def setUp(self):
        """Set up test fixtures."""
        self.audio_event_bus = AudioEventBus()
        self.plugin_manager = PluginManager(self.audio_event_bus, plugin_dirs=[])
        self.manifest_loader = ManifestLoader()
        
        # Sample metadata
        self.sample_metadata = PluginMetadata(
            name="test_plugin",
            version="1.0.0",
            author="Test Author",
            description="Test plugin",
            capabilities={PluginCapability.EVENT_LISTENER},
            event_subscriptions={AudioEventType.ANCHOR_ACTIVATED}
        )
    
    def test_plugin_metadata_creation(self):
        """Test plugin metadata creation and validation."""
        metadata = PluginMetadata(
            name="test",
            version="1.0.0", 
            author="tester",
            description="A test plugin",
            capabilities={PluginCapability.EVENT_LISTENER, PluginCapability.DATA_PROCESSOR}
        )
        
        self.assertEqual(metadata.name, "test")
        self.assertEqual(len(metadata.capabilities), 2)
        self.assertIn(PluginCapability.EVENT_LISTENER, metadata.capabilities)
    
    def test_base_plugin_interface(self):
        """Test base plugin interface."""
        plugin = MockPlugin(self.sample_metadata)
        
        # Test initialization
        self.assertTrue(plugin.initialize({}))
        
        # Test capabilities
        self.assertEqual(plugin.get_capabilities(), {PluginCapability.EVENT_LISTENER})
        
        # Test event support
        self.assertTrue(plugin.supports_event_type(AudioEventType.ANCHOR_ACTIVATED))
        self.assertFalse(plugin.supports_event_type(AudioEventType.CONFLICT_DETECTED))
        
        # Test stats
        stats = plugin.get_stats()
        self.assertEqual(stats["events_processed"], 0)
        self.assertEqual(stats["errors"], 0)
    
    def test_plugin_sandbox_basic_execution(self):
        """Test basic plugin sandbox execution."""
        sandbox = PluginSandbox(max_memory_mb=100, default_timeout_ms=500)
        plugin = MockPlugin(self.sample_metadata)
        plugin.initialize({})
        
        # Create test event
        event = AudioEvent(
            event_type=AudioEventType.ANCHOR_ACTIVATED,
            timestamp=time.time(),
            data={"test": "data"}
        )
        
        # Execute in sandbox
        result = sandbox.execute_plugin_method(plugin, "process_event", event)
        
        self.assertIsNotNone(result)
        self.assertTrue(result["processed"])
        self.assertEqual(result["event_type"], "anchor_activated")
    
    def test_plugin_sandbox_timeout(self):
        """Test plugin sandbox timeout enforcement."""
        sandbox = PluginSandbox(default_timeout_ms=100)  # Very short timeout
        
        # Create slow plugin with short timeout
        slow_metadata = PluginMetadata(
            name="slow_plugin",
            version="1.0.0",
            author="Test",
            description="Slow plugin",
            capabilities={PluginCapability.EVENT_LISTENER},
            max_execution_time_ms=100
        )
        
        plugin = SlowPlugin(slow_metadata)
        plugin.initialize({})
        
        event = AudioEvent(
            event_type=AudioEventType.ANCHOR_ACTIVATED,
            timestamp=time.time(),
            data={}
        )
        
        # Should raise timeout error
        with self.assertRaises(TimeoutError):
            sandbox.execute_plugin_method(plugin, "process_event", event)
    
    def test_safe_plugin_executor(self):
        """Test safe plugin executor error handling."""
        executor = SafePluginExecutor()
        plugin = MockPlugin(self.sample_metadata)
        plugin.initialize({})
        
        event = AudioEvent(
            event_type=AudioEventType.ANCHOR_ACTIVATED,
            timestamp=time.time(),
            data={"test": "data"}
        )
        
        # Execute successfully
        result = executor.execute_event_processing(plugin, event)
        self.assertIsNotNone(result)
        
        # Check stats
        stats = executor.get_executor_stats()
        self.assertIn("test_plugin", stats)
        self.assertEqual(stats["test_plugin"]["successful_executions"], 1)
    
    def test_manifest_loader_valid_manifest(self):
        """Test manifest loader with valid manifest."""
        manifest_data = {
            "name": "test_plugin",
            "version": "1.0.0",
            "author": "Test Author",
            "description": "A test plugin for validation",
            "capabilities": ["event_listener", "data_processor"],
            "dependencies": [],
            "event_subscriptions": ["anchor_activated"]
        }
        
        # Should validate successfully
        self.assertTrue(self.manifest_loader.validate_manifest_data(manifest_data))
    
    def test_manifest_loader_invalid_manifest(self):
        """Test manifest loader with invalid manifest."""
        invalid_manifest = {
            "name": "",  # Invalid empty name
            "version": "1.0.0",
            "author": "Test Author",
            "description": "Invalid plugin",
            "capabilities": ["invalid_capability"]  # Invalid capability
        }
        
        # Should fail validation
        self.assertFalse(self.manifest_loader.validate_manifest_data(invalid_manifest))
    
    def test_manifest_loader_yaml_file(self):
        """Test manifest loader with YAML file."""
        # Create temporary YAML file
        manifest_content = """
name: "yaml_test_plugin"
version: "1.0.0"
author: "YAML Tester"
description: "Plugin loaded from YAML"
capabilities:
  - "event_listener"
  - "analytics"
event_subscriptions:
  - "summary_generated"
configuration:
  test_setting: true
"""
        
        with tempfile.NamedTemporaryFile(mode='w', suffix='.yaml', delete=False) as f:
            f.write(manifest_content)
            temp_path = f.name
        
        try:
            # Load from file
            manifest_data = self.manifest_loader.load_manifest(temp_path)
            
            self.assertEqual(manifest_data["name"], "yaml_test_plugin")
            self.assertIn("event_listener", manifest_data["capabilities"])
            self.assertIn("summary_generated", manifest_data["event_subscriptions"])
        finally:
            os.unlink(temp_path)
    
    def test_plugin_manager_registration(self):
        """Test plugin manager registration."""
        plugin = MockPlugin(self.sample_metadata)
        
        # Register plugin
        success = self.plugin_manager.register_plugin(plugin)
        self.assertTrue(success)
        
        # Check registration
        stats = self.plugin_manager.get_plugin_stats()
        self.assertEqual(stats["total_plugins"], 1)
        self.assertEqual(stats["enabled_plugins"], 1)
        self.assertIn("test_plugin", stats["plugin_stats"])
        
        # Test event routing
        event = AudioEvent(
            event_type=AudioEventType.ANCHOR_ACTIVATED,
            timestamp=time.time(),
            data={"test": "routing"}
        )
        
        # Publish event - should route to plugin
        self.audio_event_bus.publish(
            AudioEventType.ANCHOR_ACTIVATED,
            {"test": "routing"}
        )
        
        # Brief pause for async processing
        time.sleep(0.1)
        
        # Check plugin processed the event
        plugin_stats = plugin.get_stats()
        self.assertGreater(plugin_stats["events_processed"], 0)
    
    def test_plugin_manager_unregistration(self):
        """Test plugin unregistration."""
        plugin = MockPlugin(self.sample_metadata)
        
        # Register then unregister
        self.plugin_manager.register_plugin(plugin)
        success = self.plugin_manager.unregister_plugin("test_plugin")
        
        self.assertTrue(success)
        self.assertFalse(plugin.enabled)
        
        # Check stats
        stats = self.plugin_manager.get_plugin_stats()
        self.assertEqual(stats["total_plugins"], 0)
    
    def test_example_manifest_creation(self):
        """Test creation of example manifests."""
        example = self.manifest_loader.create_example_manifest("example_plugin")
        
        self.assertEqual(example["name"], "example_plugin")
        self.assertIn("event_listener", example["capabilities"])
        self.assertIn("anchor_activated", example["event_subscriptions"])
        
        # Should validate
        self.assertTrue(self.manifest_loader.validate_manifest_data(example))


if __name__ == "__main__":
    # Simple test runner if pytest not available
    unittest.main(verbosity=2)