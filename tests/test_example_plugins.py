"""
Integration Tests for Example Plugins

Tests the sentiment lens and discourse tracker plugins with realistic scenarios.
"""

import unittest
import time
import os
import sys

# Import our plugin system
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'engine'))

from engine.plugins.plugin_manager import PluginManager
from engine.plugins.manifest_loader import ManifestLoader
from engine.audio_event_bus import AudioEventBus, AudioEvent, AudioEventType

# Import example plugins directly for testing
from engine.plugins.examples.sentiment_lens.plugin import SentimentLensPlugin
from engine.plugins.examples.discourse_tracker.plugin import DiscourseTrackerPlugin
from engine.plugins.base_plugin import PluginMetadata, PluginCapability


class TestExamplePlugins(unittest.TestCase):
    """Test suite for example plugins."""
    
    def setUp(self):
        """Set up test fixtures."""
        self.audio_event_bus = AudioEventBus()
        self.plugin_manager = PluginManager(self.audio_event_bus, plugin_dirs=[])
        self.manifest_loader = ManifestLoader()
    
    def test_sentiment_lens_plugin_basic(self):
        """Test basic sentiment lens plugin functionality."""
        # Load manifest
        manifest_path = "engine/plugins/examples/sentiment_lens/plugin.yaml"
        manifest_data = self.manifest_loader.load_manifest(manifest_path)
        
        # Create metadata
        metadata = self._create_metadata_from_manifest(manifest_data)
        
        # Create plugin
        plugin = SentimentLensPlugin(metadata)
        
        # Initialize
        context = {"configuration": manifest_data.get("configuration", {})}
        self.assertTrue(plugin.initialize(context))
        
        # Test positive sentiment event
        positive_event = AudioEvent(
            event_type=AudioEventType.ANCHOR_ACTIVATED,
            timestamp=time.time(),
            data={"anchor_text": "This is amazing and fantastic work! I love this brilliant solution."}
        )
        
        result = plugin.process_event(positive_event)
        self.assertIsNotNone(result)
        self.assertIn("sentiment_analysis", result)
        
        sentiment_analysis = result["sentiment_analysis"]
        self.assertGreater(sentiment_analysis["score"], 0.2)  # Should be positive
        self.assertIn("insights", sentiment_analysis)
        
        # Test negative sentiment event
        negative_event = AudioEvent(
            event_type=AudioEventType.CONFLICT_DETECTED,
            timestamp=time.time(),
            data={"conflict_description": "This is terrible and awful. Everything is broken and I hate this problem."}
        )
        
        result = plugin.process_event(negative_event)
        self.assertIsNotNone(result)
        sentiment_analysis = result["sentiment_analysis"]
        self.assertLess(sentiment_analysis["score"], -0.2)  # Should be negative
    
    def test_discourse_tracker_plugin_basic(self):
        """Test basic discourse tracker plugin functionality."""
        # Load manifest
        manifest_path = "engine/plugins/examples/discourse_tracker/plugin.yaml"
        manifest_data = self.manifest_loader.load_manifest(manifest_path)
        
        # Create metadata
        metadata = self._create_metadata_from_manifest(manifest_data)
        
        # Create plugin
        plugin = DiscourseTrackerPlugin(metadata)
        
        # Initialize
        context = {"configuration": manifest_data.get("configuration", {})}
        self.assertTrue(plugin.initialize(context))
        
        # Test event with discourse markers
        discourse_event = AudioEvent(
            event_type=AudioEventType.SUMMARY_GENERATED,
            timestamp=time.time(),
            data={"summary": "However, this approach is problematic. Therefore, we need to consider alternatives. In conclusion, the solution requires further analysis."}
        )
        
        result = plugin.process_event(discourse_event)
        self.assertIsNotNone(result)
        self.assertIn("discourse_analysis", result)
        
        discourse_analysis = result["discourse_analysis"]
        self.assertIn("markers", discourse_analysis)
        self.assertGreater(len(discourse_analysis["markers"]), 0)  # Should find discourse markers
        
        # Check for specific marker categories
        marker_categories = [m["category"] for m in discourse_analysis["markers"]]
        self.assertIn("transition", marker_categories)  # "however", "therefore"
        self.assertIn("summary", marker_categories)     # "in conclusion"
    
    def test_plugin_integration_with_manager(self):
        """Test plugins working together through the plugin manager."""
        # Create and register sentiment lens plugin
        sentiment_manifest = self.manifest_loader.load_manifest("engine/plugins/examples/sentiment_lens/plugin.yaml")
        sentiment_metadata = self._create_metadata_from_manifest(sentiment_manifest)
        sentiment_plugin = SentimentLensPlugin(sentiment_metadata)
        
        # Create and register discourse tracker plugin
        discourse_manifest = self.manifest_loader.load_manifest("engine/plugins/examples/discourse_tracker/plugin.yaml")
        discourse_metadata = self._create_metadata_from_manifest(discourse_manifest)
        discourse_plugin = DiscourseTrackerPlugin(discourse_metadata)
        
        # Register both plugins
        self.assertTrue(self.plugin_manager.register_plugin(sentiment_plugin))
        self.assertTrue(self.plugin_manager.register_plugin(discourse_plugin))
        
        # Verify registration
        stats = self.plugin_manager.get_plugin_stats()
        self.assertEqual(stats["total_plugins"], 2)
        self.assertEqual(stats["enabled_plugins"], 2)
        
        # Publish an event that both plugins should process
        test_event_data = {
            "content": "However, this is an amazing breakthrough! Therefore, I'm excited to see the fantastic results. In conclusion, this work is brilliant."
        }
        
        self.audio_event_bus.publish(
            AudioEventType.SUMMARY_GENERATED,
            test_event_data,
            intensity=0.8
        )
        
        # Allow time for async processing
        time.sleep(0.2)
        
        # Check that both plugins processed events
        sentiment_stats = sentiment_plugin.get_stats()
        discourse_stats = discourse_plugin.get_stats()
        
        self.assertGreater(sentiment_stats["events_processed"], 0)
        self.assertGreater(discourse_stats["events_processed"], 0)
        
        # Get cognitive insights from both plugins
        sentiment_insights = sentiment_plugin.get_cognitive_insights()
        discourse_insights = discourse_plugin.get_cognitive_insights()
        
        self.assertIn("sentiment_analysis", sentiment_insights)
        self.assertIn("discourse_analysis", discourse_insights)
    
    def test_plugin_error_handling(self):
        """Test plugin error handling and recovery."""
        # Create a plugin with invalid configuration
        sentiment_manifest = self.manifest_loader.load_manifest("engine/plugins/examples/sentiment_lens/plugin.yaml")
        sentiment_metadata = self._create_metadata_from_manifest(sentiment_manifest)
        sentiment_plugin = SentimentLensPlugin(sentiment_metadata)
        
        # Initialize with bad context
        bad_context = {"configuration": {"invalid_config": "bad_value"}}
        result = sentiment_plugin.initialize(bad_context)
        self.assertTrue(result)  # Should still initialize gracefully
        
        # Process event with no text content
        empty_event = AudioEvent(
            event_type=AudioEventType.ANCHOR_ACTIVATED,
            timestamp=time.time(),
            data={}  # No text content
        )
        
        result = sentiment_plugin.process_event(empty_event)
        # Should handle gracefully, might return None or minimal result
        
        # Process event with very large text (stress test)
        large_text = "This is a test. " * 1000  # Large but reasonable text
        large_event = AudioEvent(
            event_type=AudioEventType.SUMMARY_GENERATED,
            timestamp=time.time(),
            data={"summary": large_text}
        )
        
        result = sentiment_plugin.process_event(large_event)
        # Should handle without crashing
    
    def test_plugin_cognitive_insights(self):
        """Test cognitive insights generation from plugins."""
        # Create and initialize sentiment plugin
        sentiment_manifest = self.manifest_loader.load_manifest("engine/plugins/examples/sentiment_lens/plugin.yaml")
        sentiment_metadata = self._create_metadata_from_manifest(sentiment_manifest)
        sentiment_plugin = SentimentLensPlugin(sentiment_metadata)
        sentiment_plugin.initialize({"configuration": {}})
        
        # Process several events to build up history
        test_events = [
            ("This is amazing work!", AudioEventType.ANCHOR_ACTIVATED),
            ("This is terrible and frustrating.", AudioEventType.CONFLICT_DETECTED),
            ("Great progress on this project!", AudioEventType.SUMMARY_GENERATED),
            ("This is challenging but interesting.", AudioEventType.ANCHOR_REINFORCED)
        ]
        
        for text, event_type in test_events:
            event = AudioEvent(
                event_type=event_type,
                timestamp=time.time(),
                data={"text": text}
            )
            sentiment_plugin.process_event(event)
        
        # Get cognitive insights
        insights = sentiment_plugin.get_cognitive_insights()
        
        self.assertIn("sentiment_analysis", insights)
        sentiment_insights = insights["sentiment_analysis"]
        
        self.assertIn("sentiment_history_summary", sentiment_insights)
        self.assertIn("emotional_stability", sentiment_insights)
        
        history_summary = sentiment_insights["sentiment_history_summary"]
        self.assertEqual(history_summary["total_events"], 4)
        self.assertGreater(history_summary["positive_events"], 0)
        self.assertGreater(history_summary["negative_events"], 0)
    
    def _create_metadata_from_manifest(self, manifest_data):
        """Helper to create PluginMetadata from manifest data."""
        # Convert string capabilities to enum
        capabilities = set()
        for cap_str in manifest_data.get("capabilities", []):
            try:
                capabilities.add(PluginCapability(cap_str))
            except ValueError:
                print(f"Warning: Unknown capability '{cap_str}' ignored")
        
        # Convert string event types to enum
        event_subscriptions = set()
        for event_str in manifest_data.get("event_subscriptions", []):
            try:
                event_subscriptions.add(AudioEventType(event_str))
            except ValueError:
                print(f"Warning: Unknown event type '{event_str}' ignored")
        
        return PluginMetadata(
            name=manifest_data["name"],
            version=manifest_data["version"],
            author=manifest_data["author"],
            description=manifest_data["description"],
            capabilities=capabilities,
            dependencies=manifest_data.get("dependencies", []),
            min_engine_version=manifest_data.get("min_engine_version", "0.9.0"),
            max_memory_mb=manifest_data.get("max_memory_mb", 50),
            max_execution_time_ms=manifest_data.get("max_execution_time_ms", 1000),
            event_subscriptions=event_subscriptions
        )


if __name__ == "__main__":
    unittest.main(verbosity=2)