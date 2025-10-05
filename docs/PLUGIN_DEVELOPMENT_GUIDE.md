# Plugin Development Guide - Cognitive Geo-Thermal Lore Engine v0.9

This guide shows you how to create plugins for the TWG-TLDA Cognitive Geo-Thermal Lore Engine using the new plugin architecture.

## Quick Start

### 1. Create Plugin Directory Structure

```bash
mkdir -p my_plugin/
cd my_plugin/
```

### 2. Create Plugin Manifest (`plugin.yaml`)

```yaml
name: "my_cognitive_plugin"
version: "1.0.0"
author: "Your Name"
description: "My custom cognitive analysis plugin"
capabilities:
  - "event_listener"
  - "data_processor"
dependencies: []
min_engine_version: "0.9.0"
max_memory_mb: 50
max_execution_time_ms: 1000
event_subscriptions:
  - "anchor_activated"
  - "summary_generated"
configuration:
  analysis_mode: "standard"
  sensitivity: 0.5
```

### 3. Create Plugin Implementation (`plugin.py`)

```python
from engine.plugins.base_plugin import CognitiveEventPlugin, PluginMetadata
from engine.audio_event_bus import AudioEvent
from typing import Dict, Any, Optional

class MyCognitivePlugin(CognitiveEventPlugin):
    """My custom cognitive plugin."""
    
    def initialize(self, context: Dict[str, Any]) -> bool:
        """Initialize plugin with system context."""
        try:
            # Load configuration
            config = context.get("configuration", {})
            self.analysis_mode = config.get("analysis_mode", "standard")
            self.sensitivity = config.get("sensitivity", 0.5)
            
            print(f"MyCognitivePlugin initialized with mode: {self.analysis_mode}")
            return True
        except Exception as e:
            print(f"MyCognitivePlugin initialization error: {e}")
            return False
    
    def process_event(self, event: AudioEvent) -> Optional[Dict[str, Any]]:
        """Process cognitive events."""
        
        # Extract content from event
        content = self._extract_content(event)
        if not content:
            return None
        
        # Perform your analysis
        analysis_result = self._analyze_content(content)
        
        # Update cognitive state
        self.cognitive_state.update({
            "last_analysis": analysis_result,
            "events_processed": self.stats["events_processed"] + 1
        })
        
        # Return results
        return {
            "my_analysis": {
                "result": analysis_result,
                "confidence": self._calculate_confidence(content),
                "insights": self._generate_insights(analysis_result)
            }
        }
    
    def _extract_content(self, event: AudioEvent) -> str:
        """Extract text content from event."""
        data = event.data
        content_fields = ["text", "content", "summary", "anchor_text"]
        
        for field in content_fields:
            if field in data and data[field]:
                return str(data[field])
        
        return ""
    
    def _analyze_content(self, content: str) -> float:
        """Perform your custom analysis."""
        # Example: count words as a simple metric
        word_count = len(content.split())
        
        # Apply sensitivity
        score = min(1.0, word_count / 100.0 * self.sensitivity)
        
        return score
    
    def _calculate_confidence(self, content: str) -> float:
        """Calculate confidence in analysis."""
        return min(0.95, len(content) / 100.0)
    
    def _generate_insights(self, result: float) -> list:
        """Generate human-readable insights."""
        insights = []
        
        if result > 0.8:
            insights.append("High complexity content detected")
        elif result > 0.4:
            insights.append("Moderate complexity content")
        else:
            insights.append("Simple content structure")
        
        return insights
```

## Plugin Capabilities

Choose from these capabilities based on your plugin's functionality:

- `EVENT_LISTENER` - Listen to cognitive events
- `EVENT_PUBLISHER` - Publish new events
- `DATA_PROCESSOR` - Process and transform data  
- `SENTIMENT_ANALYSIS` - Emotional context analysis
- `DISCOURSE_TRACKING` - Conversation pattern analysis
- `VISUALIZATION` - Generate visual overlays
- `ANALYTICS` - Provide metrics and insights

## Event Types You Can Subscribe To

- `anchor_activated` - Cognitive anchor activation
- `anchor_reinforced` - Anchor reinforcement
- `conflict_detected` - Cognitive conflict detection
- `summary_generated` - Summary generation
- `cluster_formed` - Concept cluster formation
- `heat_threshold` - Heat threshold events
- `cognitive_cycle_start` - Cycle initiation
- `cognitive_cycle_end` - Cycle completion

## Plugin Testing

Create a test file for your plugin:

```python
import unittest
from my_plugin.plugin import MyCognitivePlugin
from engine.plugins.base_plugin import PluginMetadata, PluginCapability
from engine.audio_event_bus import AudioEvent, AudioEventType
import time

class TestMyCognitivePlugin(unittest.TestCase):
    
    def setUp(self):
        metadata = PluginMetadata(
            name="my_cognitive_plugin",
            version="1.0.0",
            author="Test",
            description="Test plugin",
            capabilities={PluginCapability.EVENT_LISTENER}
        )
        self.plugin = MyCognitivePlugin(metadata)
    
    def test_initialization(self):
        context = {"configuration": {"analysis_mode": "test"}}
        self.assertTrue(self.plugin.initialize(context))
    
    def test_event_processing(self):
        event = AudioEvent(
            event_type=AudioEventType.ANCHOR_ACTIVATED,
            timestamp=time.time(),
            data={"text": "Test content for analysis"}
        )
        
        result = self.plugin.process_event(event)
        self.assertIsNotNone(result)
        self.assertIn("my_analysis", result)

if __name__ == "__main__":
    unittest.main()
```

## Loading Your Plugin

Use the plugin manager to load your plugin:

```python
from engine.plugins.plugin_manager import PluginManager
from engine.plugins.manifest_loader import ManifestLoader
from engine.audio_event_bus import AudioEventBus

# Initialize system
audio_event_bus = AudioEventBus()
plugin_manager = PluginManager(audio_event_bus)
manifest_loader = ManifestLoader()

# Load your plugin
manifest_data = manifest_loader.load_manifest("my_plugin/plugin.yaml")
# ... create metadata and plugin instance
# plugin_manager.register_plugin(plugin_instance)
```

## Best Practices

### 1. Error Handling
Always handle errors gracefully in your plugin methods:

```python
def process_event(self, event):
    try:
        # Your processing logic
        return result
    except Exception as e:
        print(f"Plugin error: {e}")
        return None
```

### 2. Resource Management
Keep within your configured resource limits:

```yaml
max_memory_mb: 50        # Reasonable memory limit
max_execution_time_ms: 1000  # Fast processing
```

### 3. Configuration
Use the configuration system for customizable behavior:

```python
def initialize(self, context):
    config = context.get("configuration", {})
    self.param = config.get("param", "default_value")
```

### 4. Cognitive State
Use cognitive state for analysis across multiple events:

```python
def process_event(self, event):
    # Update state
    self.cognitive_state["pattern_count"] += 1
    
    # Use state in analysis
    if self.cognitive_state["pattern_count"] > 5:
        # Trigger special analysis
        pass
```

## Example Plugins

Study the included example plugins for reference:

- **Sentiment Lens** (`engine/plugins/examples/sentiment_lens/`)
  - Emotional context analysis
  - Lexicon-based sentiment scoring
  - Emotional pattern tracking

- **Discourse Tracker** (`engine/plugins/examples/discourse_tracker/`)  
  - Conversation pattern analysis
  - Discourse marker detection
  - Topic coherence tracking

## Demo and Testing

Run the demo to see the plugin system in action:

```bash
python demo_plugin_system.py
```

Run the tests to validate your plugin:

```bash
python -m tests.test_plugin_system
python -m tests.test_example_plugins
```

## Plugin Manifest Schema

Full manifest specification:

```yaml
# Required fields
name: "string"              # Plugin identifier (alphanumeric + underscore/dash)
version: "semver"           # Semantic version (e.g., "1.0.0")
author: "string"            # Author name
description: "string"       # Plugin description
capabilities: ["array"]     # List of PluginCapability values

# Optional fields  
dependencies: ["array"]     # List of dependency plugin names
min_engine_version: "semver" # Minimum engine version (default: "0.9.0")
max_memory_mb: number       # Memory limit in MB (default: 50)
max_execution_time_ms: number # Timeout in milliseconds (default: 1000)
event_subscriptions: ["array"] # AudioEventType values to subscribe to
configuration: {}           # Plugin-specific configuration object
```

---

🧙‍♂️ **The Bootstrap Sentinel approves of your plugin development journey!** Start with the examples, follow the patterns, and extend the cognitive capabilities of the Lore Engine! 🛡️