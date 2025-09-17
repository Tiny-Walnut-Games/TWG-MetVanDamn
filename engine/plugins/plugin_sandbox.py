"""
Plugin Sandbox - Safe Execution Environment

Provides isolated execution contexts for plugins with timeout controls,
memory limits, and error recovery mechanisms.
"""

import time
import threading
import os
import signal
from typing import Dict, Any, Optional, Callable
from contextlib import contextmanager

from .base_plugin import BasePlugin, PluginMetadata
from ..audio_event_bus import AudioEvent

# Graceful handling of optional dependencies
try:
    import psutil
    PSUTIL_AVAILABLE = True
except ImportError:
    PSUTIL_AVAILABLE = False


class PluginSandboxError(Exception):
    """Exception raised when plugin execution violates sandbox constraints."""
    pass


class TimeoutError(PluginSandboxError):
    """Exception raised when plugin execution exceeds timeout."""
    pass


class MemoryLimitError(PluginSandboxError):
    """Exception raised when plugin exceeds memory limit."""
    pass


class PluginSandbox:
    """
    Sandboxed execution environment for plugins.
    
    Provides timeout controls, memory monitoring, and error isolation
    to ensure plugins cannot compromise system stability.
    """
    
    def __init__(self, max_memory_mb: int = 50, default_timeout_ms: int = 1000):
        self.max_memory_mb = max_memory_mb
        self.default_timeout_ms = default_timeout_ms
        self.active_executions = {}
        self._lock = threading.Lock()
    
    def execute_plugin_method(self, 
                              plugin: BasePlugin, 
                              method_name: str, 
                              *args, 
                              **kwargs) -> Any:
        """
        Execute a plugin method in a sandboxed environment.
        
        Args:
            plugin: The plugin instance
            method_name: Name of the method to execute
            *args: Positional arguments
            **kwargs: Keyword arguments
            
        Returns:
            Method execution result
            
        Raises:
            TimeoutError: If execution exceeds timeout
            MemoryLimitError: If memory usage exceeds limit
            PluginSandboxError: For other sandbox violations
        """
        execution_id = f"{plugin.metadata.name}_{method_name}_{time.time()}"
        timeout_ms = plugin.metadata.max_execution_time_ms or self.default_timeout_ms
        memory_limit_mb = plugin.metadata.max_memory_mb or self.max_memory_mb
        
        with self._track_execution(execution_id):
            return self._execute_with_constraints(
                plugin, method_name, timeout_ms, memory_limit_mb, *args, **kwargs
            )
    
    @contextmanager
    def _track_execution(self, execution_id: str):
        """Track active plugin execution."""
        with self._lock:
            self.active_executions[execution_id] = {
                "start_time": time.time(),
                "thread_id": threading.current_thread().ident
            }
        try:
            yield execution_id
        finally:
            with self._lock:
                self.active_executions.pop(execution_id, None)
    
    def _execute_with_constraints(self, 
                                  plugin: BasePlugin, 
                                  method_name: str,
                                  timeout_ms: int,
                                  memory_limit_mb: int,
                                  *args, 
                                  **kwargs) -> Any:
        """Execute plugin method with timeout and memory constraints."""
        
        # Get the method to execute
        if not hasattr(plugin, method_name):
            raise PluginSandboxError(f"Plugin {plugin.metadata.name} has no method '{method_name}'")
        
        method = getattr(plugin, method_name)
        
        # Track initial memory if psutil is available
        initial_memory_mb = 0
        psutil_working = PSUTIL_AVAILABLE
        if psutil_working:
            try:
                process = psutil.Process()
                initial_memory_mb = process.memory_info().rss / (1024 * 1024)
            except Exception:
                # Fallback if psutil fails
                psutil_working = False
        
        # Execute with timeout
        result = None
        exception = None
        
        def target():
            nonlocal result, exception
            try:
                result = method(*args, **kwargs)
            except Exception as e:
                exception = e
        
        thread = threading.Thread(target=target)
        thread.daemon = True
        thread.start()
        
        # Monitor execution
        start_time = time.time()
        timeout_seconds = timeout_ms / 1000.0
        
        while thread.is_alive():
            elapsed = time.time() - start_time
            
            # Check timeout
            if elapsed > timeout_seconds:
                # Force thread termination (best effort)
                thread.join(timeout=0.1)
                if thread.is_alive():
                    # Thread didn't terminate cleanly
                    pass  # In production, might need more aggressive termination
                raise TimeoutError(
                    f"Plugin {plugin.metadata.name}.{method_name} exceeded timeout of {timeout_ms}ms"
                )
            
            # Check memory usage (only if psutil available and working)
            if psutil_working:
                try:
                    process = psutil.Process()
                    current_memory_mb = process.memory_info().rss / (1024 * 1024)
                    memory_usage = current_memory_mb - initial_memory_mb
                    
                    if memory_usage > memory_limit_mb:
                        thread.join(timeout=0.1)
                        raise MemoryLimitError(
                            f"Plugin {plugin.metadata.name}.{method_name} exceeded memory limit of {memory_limit_mb}MB"
                        )
                except Exception:
                    # If memory monitoring fails, continue without it
                    psutil_working = False
            
            # Brief sleep to avoid busy waiting
            time.sleep(0.01)
        
        # Wait for thread to complete
        thread.join()
        
        # Check for exceptions
        if exception:
            if isinstance(exception, (TimeoutError, MemoryLimitError, PluginSandboxError)):
                raise exception
            else:
                # Wrap other exceptions in sandbox error
                raise PluginSandboxError(f"Plugin execution error: {exception}")
        
        return result
    
    def get_active_executions(self) -> Dict[str, Any]:
        """Get information about currently active plugin executions."""
        with self._lock:
            current_time = time.time()
            return {
                exec_id: {
                    **info,
                    "elapsed_time": current_time - info["start_time"]
                }
                for exec_id, info in self.active_executions.items()
            }
    
    def force_shutdown_all(self) -> None:
        """Force shutdown of all active plugin executions."""
        with self._lock:
            # In a real implementation, might need more aggressive cleanup
            self.active_executions.clear()


class SafePluginExecutor:
    """
    High-level interface for safe plugin execution.
    
    Combines sandboxing with error handling and statistics tracking.
    """
    
    def __init__(self, sandbox: Optional[PluginSandbox] = None):
        self.sandbox = sandbox or PluginSandbox()
        self.execution_stats = {}
    
    def execute_event_processing(self, plugin: BasePlugin, event: AudioEvent) -> Optional[Dict[str, Any]]:
        """
        Safely execute plugin event processing.
        
        Args:
            plugin: Plugin to execute
            event: Event to process
            
        Returns:
            Processing result or None if execution failed
        """
        start_time = time.time()
        error_occurred = False
        result = None
        
        try:
            # Execute in sandbox
            result = self.sandbox.execute_plugin_method(
                plugin, 'process_event', event
            )
            
        except (TimeoutError, MemoryLimitError, PluginSandboxError) as e:
            error_occurred = True
            print(f"Plugin sandbox error for {plugin.metadata.name}: {e}")
            
        except Exception as e:
            error_occurred = True
            print(f"Unexpected plugin error for {plugin.metadata.name}: {e}")
        
        finally:
            # Update plugin statistics
            execution_time_ms = (time.time() - start_time) * 1000
            plugin.update_stats(execution_time_ms, error_occurred)
            
            # Update executor statistics
            plugin_name = plugin.metadata.name
            if plugin_name not in self.execution_stats:
                self.execution_stats[plugin_name] = {
                    "total_executions": 0,
                    "successful_executions": 0,
                    "sandbox_violations": 0
                }
            
            stats = self.execution_stats[plugin_name]
            stats["total_executions"] += 1
            if not error_occurred:
                stats["successful_executions"] += 1
            else:
                stats["sandbox_violations"] += 1
        
        return result
    
    def get_executor_stats(self) -> Dict[str, Any]:
        """Get executor-level statistics."""
        return dict(self.execution_stats)