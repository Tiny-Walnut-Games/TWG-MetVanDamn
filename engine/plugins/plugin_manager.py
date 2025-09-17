"""
Plugin Manager - Central Plugin Orchestration

Manages plugin lifecycle, registration, and event routing for the 
Cognitive Geo-Thermal Lore Engine plugin system.
"""

import os
import importlib.util
import threading
from pathlib import Path
from typing import Dict, List, Any, Optional, Set, Type
from collections import defaultdict

from .base_plugin import BasePlugin, PluginCapability, PluginMetadata
from .plugin_sandbox import SafePluginExecutor, PluginSandbox
from .manifest_loader import ManifestLoader
from ..audio_event_bus import AudioEventBus, AudioEvent, AudioEventType


class PluginLoadError(Exception):
    """Exception raised when plugin loading fails."""
    pass


class PluginManager:
    """
    Central manager for the plugin system.
    
    Handles plugin discovery, loading, registration, and event routing
    while maintaining sandbox isolation and error recovery.
    """
    
    def __init__(self, audio_event_bus: AudioEventBus, plugin_dirs: Optional[List[str]] = None):
        self.audio_event_bus = audio_event_bus
        self.plugin_dirs = plugin_dirs or ["plugins", "engine/plugins/examples"]
        
        # Plugin registry
        self.plugins: Dict[str, BasePlugin] = {}
        self.plugin_metadata: Dict[str, PluginMetadata] = {}
        
        # Event routing
        self.event_subscribers: Dict[AudioEventType, List[str]] = defaultdict(list)
        
        # Execution management
        self.sandbox = PluginSandbox()
        self.executor = SafePluginExecutor(self.sandbox)
        self.manifest_loader = ManifestLoader()
        
        # System state
        self.enabled = True
        self._lock = threading.Lock()
        
        # Subscribe to all audio events for plugin routing
        self._subscribe_to_audio_events()
    
    def _subscribe_to_audio_events(self):
        """Subscribe to audio events for plugin routing."""
        for event_type in AudioEventType:
            self.audio_event_bus.subscribe(event_type, self._route_event_to_plugins)
    
    def load_plugins_from_directories(self) -> Dict[str, bool]:
        """
        Load plugins from configured directories.
        
        Returns:
            Dictionary mapping plugin names to load success status
        """
        load_results = {}
        
        for plugin_dir in self.plugin_dirs:
            if not os.path.exists(plugin_dir):
                continue
                
            for item in os.listdir(plugin_dir):
                item_path = os.path.join(plugin_dir, item)
                
                # Look for plugin manifests
                if os.path.isdir(item_path):
                    manifest_path = os.path.join(item_path, "plugin.yaml")
                    if os.path.exists(manifest_path):
                        try:
                            success = self._load_plugin_from_directory(item_path)
                            load_results[item] = success
                        except Exception as e:
                            print(f"Failed to load plugin from {item_path}: {e}")
                            load_results[item] = False
        
        return load_results
    
    def _load_plugin_from_directory(self, plugin_dir: str) -> bool:
        """Load a plugin from a directory containing manifest and code."""
        manifest_path = os.path.join(plugin_dir, "plugin.yaml")
        
        # Load manifest
        try:
            manifest_data = self.manifest_loader.load_manifest(manifest_path)
            metadata = self._create_metadata_from_manifest(manifest_data)
        except Exception as e:
            raise PluginLoadError(f"Failed to load manifest: {e}")
        
        # Load plugin code
        plugin_module_path = os.path.join(plugin_dir, "plugin.py")
        if not os.path.exists(plugin_module_path):
            raise PluginLoadError(f"Plugin module not found: {plugin_module_path}")
        
        try:
            plugin_instance = self._load_plugin_module(plugin_module_path, metadata)
        except Exception as e:
            raise PluginLoadError(f"Failed to load plugin module: {e}")
        
        # Register plugin
        return self.register_plugin(plugin_instance)
    
    def _create_metadata_from_manifest(self, manifest_data: Dict[str, Any]) -> PluginMetadata:
        """Create PluginMetadata from loaded manifest data."""
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
    
    def _load_plugin_module(self, module_path: str, metadata: PluginMetadata) -> BasePlugin:
        """Load plugin class from Python module."""
        spec = importlib.util.spec_from_file_location(f"plugin_{metadata.name}", module_path)
        if spec is None or spec.loader is None:
            raise PluginLoadError(f"Could not load module spec from {module_path}")
        
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        
        # Look for plugin class
        plugin_class = None
        for attr_name in dir(module):
            attr = getattr(module, attr_name)
            if (isinstance(attr, type) and 
                issubclass(attr, BasePlugin) and 
                attr != BasePlugin):
                plugin_class = attr
                break
        
        if plugin_class is None:
            raise PluginLoadError(f"No BasePlugin subclass found in {module_path}")
        
        # Instantiate plugin
        return plugin_class(metadata)
    
    def register_plugin(self, plugin: BasePlugin) -> bool:
        """
        Register a plugin with the manager.
        
        Args:
            plugin: Plugin instance to register
            
        Returns:
            True if registration successful, False otherwise
        """
        with self._lock:
            plugin_name = plugin.metadata.name
            
            # Check for conflicts
            if plugin_name in self.plugins:
                print(f"Warning: Plugin '{plugin_name}' already registered, skipping")
                return False
            
            # Initialize plugin
            try:
                context = self._create_plugin_context()
                if not plugin.initialize(context):
                    print(f"Plugin '{plugin_name}' initialization failed")
                    return False
            except Exception as e:
                print(f"Plugin '{plugin_name}' initialization error: {e}")
                return False
            
            # Register plugin
            self.plugins[plugin_name] = plugin
            self.plugin_metadata[plugin_name] = plugin.metadata
            
            # Register for events
            for event_type in plugin.metadata.event_subscriptions:
                self.event_subscribers[event_type].append(plugin_name)
            
            print(f"Successfully registered plugin: {plugin_name} v{plugin.metadata.version}")
            return True
    
    def _create_plugin_context(self) -> Dict[str, Any]:
        """Create initialization context for plugins."""
        return {
            "audio_event_bus": self.audio_event_bus,
            "engine_version": "0.9.0",
            "sandbox_limits": {
                "max_memory_mb": self.sandbox.max_memory_mb,
                "default_timeout_ms": self.sandbox.default_timeout_ms
            }
        }
    
    def _route_event_to_plugins(self, event: AudioEvent) -> None:
        """Route audio event to subscribed plugins."""
        if not self.enabled:
            return
        
        plugin_names = self.event_subscribers.get(event.event_type, [])
        if not plugin_names:
            return
        
        for plugin_name in plugin_names:
            plugin = self.plugins.get(plugin_name)
            if plugin and plugin.enabled:
                # Execute plugin in background thread to avoid blocking
                def execute_plugin():
                    try:
                        result = self.executor.execute_event_processing(plugin, event)
                        if result:
                            # Plugin generated output - could publish new events
                            self._handle_plugin_output(plugin_name, result)
                    except Exception as e:
                        print(f"Error routing event to plugin {plugin_name}: {e}")
                
                thread = threading.Thread(target=execute_plugin)
                thread.daemon = True
                thread.start()
    
    def _handle_plugin_output(self, plugin_name: str, output: Dict[str, Any]) -> None:
        """Handle output from plugin processing."""
        # If plugin wants to publish new events
        if "publish_events" in output:
            for event_data in output["publish_events"]:
                try:
                    event_type = AudioEventType(event_data["event_type"])
                    self.audio_event_bus.publish(
                        event_type,
                        event_data.get("data", {}),
                        event_data.get("intensity", 0.5),
                        event_data.get("affect_layer", f"plugin_{plugin_name}")
                    )
                except Exception as e:
                    print(f"Error publishing event from plugin {plugin_name}: {e}")
    
    def unregister_plugin(self, plugin_name: str) -> bool:
        """Unregister and shutdown a plugin."""
        with self._lock:
            if plugin_name not in self.plugins:
                return False
            
            plugin = self.plugins[plugin_name]
            
            # Remove from event subscriptions
            for event_type, subscribers in self.event_subscribers.items():
                if plugin_name in subscribers:
                    subscribers.remove(plugin_name)
            
            # Shutdown plugin
            try:
                plugin.shutdown()
            except Exception as e:
                print(f"Error shutting down plugin {plugin_name}: {e}")
            
            # Remove from registry
            del self.plugins[plugin_name]
            del self.plugin_metadata[plugin_name]
            
            print(f"Unregistered plugin: {plugin_name}")
            return True
    
    def get_plugin_stats(self) -> Dict[str, Any]:
        """Get comprehensive plugin system statistics."""
        with self._lock:
            plugin_stats = {}
            for name, plugin in self.plugins.items():
                plugin_stats[name] = plugin.get_stats()
            
            return {
                "total_plugins": len(self.plugins),
                "enabled_plugins": sum(1 for p in self.plugins.values() if p.enabled),
                "plugin_stats": plugin_stats,
                "executor_stats": self.executor.get_executor_stats(),
                "active_executions": self.sandbox.get_active_executions()
            }
    
    def shutdown_all_plugins(self) -> None:
        """Shutdown all plugins and disable the system."""
        with self._lock:
            self.enabled = False
            
            for plugin_name in list(self.plugins.keys()):
                self.unregister_plugin(plugin_name)
            
            self.sandbox.force_shutdown_all()
            print("Plugin system shutdown complete")