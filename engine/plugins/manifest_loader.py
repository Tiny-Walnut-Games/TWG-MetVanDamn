"""
Plugin Manifest Loader

Handles loading and validation of plugin manifest files (YAML) with
schema validation and capability negotiation.
"""

import yaml
import jsonschema
from pathlib import Path
from typing import Dict, Any, List

from .base_plugin import PluginCapability
from ..audio_event_bus import AudioEventType


class ManifestValidationError(Exception):
    """Exception raised when manifest validation fails."""
    pass


class ManifestLoader:
    """
    Loads and validates plugin manifest files.
    
    Provides schema validation and capability verification for plugin
    configuration files.
    """
    
    def __init__(self):
        self.schema = self._get_manifest_schema()
    
    def _get_manifest_schema(self) -> Dict[str, Any]:
        """Get JSON schema for plugin manifest validation."""
        return {
            "type": "object",
            "required": ["name", "version", "author", "description", "capabilities"],
            "properties": {
                "name": {
                    "type": "string",
                    "minLength": 1,
                    "maxLength": 100,
                    "pattern": "^[a-zA-Z0-9_-]+$"
                },
                "version": {
                    "type": "string",
                    "pattern": r"^\d+\.\d+\.\d+$"
                },
                "author": {
                    "type": "string",
                    "minLength": 1,
                    "maxLength": 200
                },
                "description": {
                    "type": "string",
                    "minLength": 1,
                    "maxLength": 1000
                },
                "capabilities": {
                    "type": "array",
                    "items": {
                        "type": "string",
                        "enum": [cap.value for cap in PluginCapability]
                    },
                    "minItems": 1,
                    "uniqueItems": True
                },
                "dependencies": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    },
                    "default": []
                },
                "min_engine_version": {
                    "type": "string",
                    "pattern": r"^\d+\.\d+\.\d+$",
                    "default": "0.9.0"
                },
                "max_memory_mb": {
                    "type": "integer",
                    "minimum": 1,
                    "maximum": 1000,
                    "default": 50
                },
                "max_execution_time_ms": {
                    "type": "integer",
                    "minimum": 10,
                    "maximum": 30000,
                    "default": 1000
                },
                "event_subscriptions": {
                    "type": "array",
                    "items": {
                        "type": "string",
                        "enum": [event.value for event in AudioEventType]
                    },
                    "default": [],
                    "uniqueItems": True
                },
                "configuration": {
                    "type": "object",
                    "default": {}
                }
            },
            "additionalProperties": False
        }
    
    def load_manifest(self, manifest_path: str) -> Dict[str, Any]:
        """
        Load and validate a plugin manifest file.
        
        Args:
            manifest_path: Path to the manifest YAML file
            
        Returns:
            Validated manifest data
            
        Raises:
            ManifestValidationError: If manifest is invalid
        """
        try:
            # Load YAML file
            with open(manifest_path, 'r', encoding='utf-8') as f:
                manifest_data = yaml.safe_load(f)
            
            if not isinstance(manifest_data, dict):
                raise ManifestValidationError("Manifest must be a YAML object")
            
            # Validate against schema
            try:
                jsonschema.validate(manifest_data, self.schema)
            except jsonschema.ValidationError as e:
                raise ManifestValidationError(f"Schema validation failed: {e.message}")
            
            # Additional validation
            self._validate_capabilities(manifest_data)
            self._validate_event_subscriptions(manifest_data)
            self._validate_version_compatibility(manifest_data)
            
            return manifest_data
            
        except FileNotFoundError:
            raise ManifestValidationError(f"Manifest file not found: {manifest_path}")
        except yaml.YAMLError as e:
            raise ManifestValidationError(f"YAML parsing error: {e}")
        except Exception as e:
            raise ManifestValidationError(f"Unexpected error loading manifest: {e}")
    
    def _validate_capabilities(self, manifest_data: Dict[str, Any]) -> None:
        """Validate plugin capabilities are coherent."""
        capabilities = manifest_data["capabilities"]
        
        # Check capability combinations
        if "event_publisher" in capabilities and "event_listener" not in capabilities:
            # Publishers typically also need to listen
            pass  # Allow for now, might want to warn
        
        # Validate sentiment analysis capability requirements
        if "sentiment_analysis" in capabilities:
            event_subs = manifest_data.get("event_subscriptions", [])
            required_events = ["anchor_activated", "summary_generated"]
            if not any(event in event_subs for event in required_events):
                raise ManifestValidationError(
                    "Sentiment analysis plugins should subscribe to cognitive events"
                )
    
    def _validate_event_subscriptions(self, manifest_data: Dict[str, Any]) -> None:
        """Validate event subscriptions are compatible with capabilities."""
        capabilities = manifest_data["capabilities"]
        event_subs = manifest_data.get("event_subscriptions", [])
        
        # If plugin has event_listener capability, it should subscribe to events
        if "event_listener" in capabilities and not event_subs:
            raise ManifestValidationError(
                "Plugins with event_listener capability must specify event_subscriptions"
            )
    
    def _validate_version_compatibility(self, manifest_data: Dict[str, Any]) -> None:
        """Validate version compatibility."""
        min_version = manifest_data.get("min_engine_version", "0.9.0")
        
        # Parse version numbers
        try:
            min_parts = [int(x) for x in min_version.split(".")]
            current_parts = [0, 9, 0]  # Current engine version
            
            # Simple version comparison
            if min_parts > current_parts:
                raise ManifestValidationError(
                    f"Plugin requires engine version {min_version} but current is 0.9.0"
                )
        except ValueError:
            raise ManifestValidationError(f"Invalid version format: {min_version}")
    
    def validate_manifest_data(self, manifest_data: Dict[str, Any]) -> bool:
        """
        Validate manifest data without loading from file.
        
        Args:
            manifest_data: Manifest data dictionary
            
        Returns:
            True if valid, False otherwise
        """
        try:
            jsonschema.validate(manifest_data, self.schema)
            self._validate_capabilities(manifest_data)
            self._validate_event_subscriptions(manifest_data)
            self._validate_version_compatibility(manifest_data)
            return True
        except (jsonschema.ValidationError, ManifestValidationError):
            return False
    
    def create_example_manifest(self, plugin_name: str) -> Dict[str, Any]:
        """
        Create an example manifest for a plugin.
        
        Args:
            plugin_name: Name of the plugin
            
        Returns:
            Example manifest data
        """
        return {
            "name": plugin_name,
            "version": "1.0.0",
            "author": "Plugin Developer",
            "description": f"Example {plugin_name} plugin for cognitive event processing",
            "capabilities": ["event_listener", "data_processor"],
            "dependencies": [],
            "min_engine_version": "0.9.0",
            "max_memory_mb": 50,
            "max_execution_time_ms": 1000,
            "event_subscriptions": ["anchor_activated", "summary_generated"],
            "configuration": {
                "example_setting": "default_value",
                "processing_mode": "standard"
            }
        }
    
    def save_manifest(self, manifest_data: Dict[str, Any], output_path: str) -> None:
        """
        Save manifest data to a YAML file.
        
        Args:
            manifest_data: Manifest data to save
            output_path: Output file path
            
        Raises:
            ManifestValidationError: If manifest data is invalid
        """
        # Validate before saving
        if not self.validate_manifest_data(manifest_data):
            raise ManifestValidationError("Cannot save invalid manifest data")
        
        try:
            with open(output_path, 'w', encoding='utf-8') as f:
                yaml.dump(manifest_data, f, default_flow_style=False, sort_keys=True)
        except Exception as e:
            raise ManifestValidationError(f"Error saving manifest: {e}")