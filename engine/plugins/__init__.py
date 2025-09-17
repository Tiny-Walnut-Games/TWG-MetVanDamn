"""
Plugin System for Cognitive Geo-Thermal Lore Engine v0.9

Provides a sandboxed plugin architecture for extending cognitive event processing
with timeout controls, memory guards, and capability negotiation.
"""

from .base_plugin import BasePlugin, PluginCapability, PluginMetadata
from .plugin_manager import PluginManager  
from .plugin_sandbox import PluginSandbox
from .manifest_loader import ManifestLoader

__all__ = [
    'BasePlugin',
    'PluginCapability', 
    'PluginMetadata',
    'PluginManager',
    'PluginSandbox',
    'ManifestLoader'
]