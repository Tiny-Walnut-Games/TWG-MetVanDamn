#!/usr/bin/env python3
"""
Incremental State Diff System - Performance Optimization for v0.6

Provides granular state diff tracking for bridge and UI payloads
to minimize data transfer and improve response times.

🧙‍♂️ "Why send the whole castle when a single stone's change 
    tells the story of progress?" - Bootstrap Sentinel
"""

from typing import Dict, Any, List, Optional, Set, Tuple
import time
import hashlib
import json
from dataclasses import dataclass, asdict
from enum import Enum


class DiffOperation(Enum):
    """Types of diff operations."""
    ADDED = "added"
    MODIFIED = "modified"
    REMOVED = "removed"
    MOVED = "moved"
    UNCHANGED = "unchanged"


@dataclass
class StateDiff:
    """Individual state change with metadata."""
    path: str  # JSON path to the changed value (e.g., "anchors.anchor_123.heat")
    operation: DiffOperation
    old_value: Any = None
    new_value: Any = None
    timestamp: float = 0.0
    change_magnitude: float = 0.0  # 0.0 = minor, 1.0 = major change
    
    def __post_init__(self):
        if self.timestamp == 0.0:
            self.timestamp = time.time()
            
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for serialization."""
        return {
            "path": self.path,
            "operation": self.operation.value,
            "old_value": self.old_value,
            "new_value": self.new_value,
            "timestamp": self.timestamp,
            "change_magnitude": self.change_magnitude
        }


@dataclass
class DiffSummary:
    """Summary of changes in a diff operation."""
    total_changes: int
    operations_count: Dict[str, int]  # operation_type -> count
    changed_paths: List[str]
    change_magnitude_avg: float
    diff_timestamp: float
    diff_size_bytes: int
    compression_ratio: float = 0.0  # How much smaller than full state
    
    def __post_init__(self):
        if self.diff_timestamp == 0.0:
            self.diff_timestamp = time.time()


class IncrementalStateDiff:
    """
    Incremental state diff engine for performance optimization.
    
    Features:
    - Granular path-based change tracking
    - Configurable diff granularity
    - Change magnitude assessment
    - Compression ratio metrics
    - State fingerprinting for validation
    """
    
    def __init__(self, 
                 max_diff_depth: int = 10,
                 ignore_paths: List[str] = None,
                 magnitude_thresholds: Dict[str, float] = None):
        """
        Initialize diff engine.
        
        Args:
            max_diff_depth: Maximum depth to traverse for diffs
            ignore_paths: Paths to ignore in diff calculation
            magnitude_thresholds: Thresholds for change magnitude calculation
        """
        self.max_diff_depth = max_diff_depth
        self.ignore_paths = set(ignore_paths or [
            "timestamp", "last_updated", "creation_timestamp"
        ])
        
        # Change magnitude thresholds
        self.magnitude_thresholds = magnitude_thresholds or {
            "numeric_change_ratio": 0.1,  # 10% change = magnitude 0.1
            "string_similarity": 0.8,     # 80% similarity = minor change
            "list_change_ratio": 0.2      # 20% of list changed = magnitude 0.2
        }
        
        # State tracking
        self.last_state_fingerprint: Optional[str] = None
        self.diff_cache: Dict[str, List[StateDiff]] = {}
        
        # Performance metrics
        self.metrics = {
            "total_diffs_computed": 0,
            "total_changes_tracked": 0,
            "compression_ratios": [],
            "avg_diff_time_ms": 0.0
        }
        
    def compute_diff(self, current_state: Dict[str, Any], 
                    previous_state: Optional[Dict[str, Any]] = None,
                    diff_id: str = None) -> Tuple[List[StateDiff], DiffSummary]:
        """
        Compute incremental diff between current and previous state.
        
        Args:
            current_state: Current system state
            previous_state: Previous state to compare against
            diff_id: Optional ID for caching
            
        Returns:
            Tuple of (changes_list, diff_summary)
        """
        start_time = time.time()
        
        if previous_state is None:
            # First state - everything is new
            return self._create_initial_diff(current_state, diff_id)
            
        # Compute granular diff
        changes = []
        self._compute_recursive_diff(
            current_state, previous_state, 
            changes, path_prefix=""
        )
        
        # Create summary
        summary = self._create_diff_summary(changes, current_state, previous_state)
        
        # Update metrics
        diff_time_ms = (time.time() - start_time) * 1000
        self.metrics["total_diffs_computed"] += 1
        self.metrics["total_changes_tracked"] += len(changes)
        self.metrics["avg_diff_time_ms"] = (
            self.metrics["avg_diff_time_ms"] * (self.metrics["total_diffs_computed"] - 1) +
            diff_time_ms
        ) / self.metrics["total_diffs_computed"]
        
        # Cache if ID provided
        if diff_id:
            self.diff_cache[diff_id] = changes
            
        return changes, summary
        
    def _create_initial_diff(self, state: Dict[str, Any], 
                           diff_id: str = None) -> Tuple[List[StateDiff], DiffSummary]:
        """Create diff for initial state (everything is new)."""
        changes = []
        
        # Mark all values as added
        self._extract_all_paths(state, changes, "", DiffOperation.ADDED)
        
        summary = DiffSummary(
            total_changes=len(changes),
            operations_count={"added": len(changes)},
            changed_paths=[c.path for c in changes],
            change_magnitude_avg=1.0,  # Initial state is major change
            diff_timestamp=time.time(),
            diff_size_bytes=len(json.dumps([c.to_dict() for c in changes])),
            compression_ratio=0.0  # No previous state to compare
        )
        
        return changes, summary
        
    def _compute_recursive_diff(self, current: Any, previous: Any, 
                              changes: List[StateDiff], path_prefix: str, 
                              depth: int = 0):
        """Recursively compute diff between nested structures."""
        if depth >= self.max_diff_depth:
            return
            
        if path_prefix in self.ignore_paths:
            return
            
        # Handle different types
        if isinstance(current, dict) and isinstance(previous, dict):
            self._diff_dict(current, previous, changes, path_prefix, depth)
        elif isinstance(current, list) and isinstance(previous, list):
            self._diff_list(current, previous, changes, path_prefix, depth)
        else:
            # Primitive value comparison
            if current != previous:
                magnitude = self._calculate_change_magnitude(previous, current)
                changes.append(StateDiff(
                    path=path_prefix,
                    operation=DiffOperation.MODIFIED,
                    old_value=previous,
                    new_value=current,
                    change_magnitude=magnitude
                ))
                
    def _diff_dict(self, current: Dict[str, Any], previous: Dict[str, Any], 
                  changes: List[StateDiff], path_prefix: str, depth: int):
        """Diff dictionary structures."""
        current_keys = set(current.keys())
        previous_keys = set(previous.keys())
        
        # Added keys
        for key in current_keys - previous_keys:
            new_path = f"{path_prefix}.{key}" if path_prefix else key
            changes.append(StateDiff(
                path=new_path,
                operation=DiffOperation.ADDED,
                new_value=current[key],
                change_magnitude=0.8  # New additions are significant
            ))
            
        # Removed keys
        for key in previous_keys - current_keys:
            old_path = f"{path_prefix}.{key}" if path_prefix else key
            changes.append(StateDiff(
                path=old_path,
                operation=DiffOperation.REMOVED,
                old_value=previous[key],
                change_magnitude=0.9  # Removals are major changes
            ))
            
        # Modified keys
        for key in current_keys & previous_keys:
            new_path = f"{path_prefix}.{key}" if path_prefix else key
            self._compute_recursive_diff(
                current[key], previous[key], changes, new_path, depth + 1
            )
            
    def _diff_list(self, current: List[Any], previous: List[Any], 
                  changes: List[StateDiff], path_prefix: str, depth: int):
        """Diff list structures with intelligent matching."""
        # Simple approach: compare by index
        max_len = max(len(current), len(previous))
        
        for i in range(max_len):
            item_path = f"{path_prefix}[{i}]"
            
            if i >= len(previous):
                # New item
                changes.append(StateDiff(
                    path=item_path,
                    operation=DiffOperation.ADDED,
                    new_value=current[i],
                    change_magnitude=0.6
                ))
            elif i >= len(current):
                # Removed item
                changes.append(StateDiff(
                    path=item_path,
                    operation=DiffOperation.REMOVED,
                    old_value=previous[i],
                    change_magnitude=0.7
                ))
            else:
                # Potentially modified item
                self._compute_recursive_diff(
                    current[i], previous[i], changes, item_path, depth + 1
                )
                
    def _calculate_change_magnitude(self, old_value: Any, new_value: Any) -> float:
        """Calculate the magnitude of change between values."""
        if old_value is None and new_value is not None:
            return 1.0
        if old_value is not None and new_value is None:
            return 1.0
            
        # Numeric changes
        if isinstance(old_value, (int, float)) and isinstance(new_value, (int, float)):
            if old_value == 0:
                return 1.0 if new_value != 0 else 0.0
            ratio = abs(new_value - old_value) / abs(old_value)
            return min(ratio / self.magnitude_thresholds["numeric_change_ratio"], 1.0)
            
        # String changes
        if isinstance(old_value, str) and isinstance(new_value, str):
            similarity = self._string_similarity(old_value, new_value)
            return 1.0 - similarity
            
        # Default for other types
        return 0.5 if old_value != new_value else 0.0
        
    def _string_similarity(self, str1: str, str2: str) -> float:
        """Calculate string similarity using simple ratio."""
        if not str1 and not str2:
            return 1.0
        if not str1 or not str2:
            return 0.0
            
        # Simple character-based similarity
        max_len = max(len(str1), len(str2))
        common_chars = sum(1 for a, b in zip(str1, str2) if a == b)
        return common_chars / max_len
        
    def _extract_all_paths(self, obj: Any, changes: List[StateDiff], 
                          path_prefix: str, operation: DiffOperation):
        """Extract all paths from object for initial diff."""
        if isinstance(obj, dict):
            for key, value in obj.items():
                new_path = f"{path_prefix}.{key}" if path_prefix else key
                if not isinstance(value, (dict, list)):
                    changes.append(StateDiff(
                        path=new_path,
                        operation=operation,
                        new_value=value,
                        change_magnitude=1.0
                    ))
                else:
                    self._extract_all_paths(value, changes, new_path, operation)
        elif isinstance(obj, list):
            for i, item in enumerate(obj):
                item_path = f"{path_prefix}[{i}]"
                if not isinstance(item, (dict, list)):
                    changes.append(StateDiff(
                        path=item_path,
                        operation=operation,
                        new_value=item,
                        change_magnitude=1.0
                    ))
                else:
                    self._extract_all_paths(item, changes, item_path, operation)
                    
    def _create_diff_summary(self, changes: List[StateDiff], 
                           current_state: Dict[str, Any], 
                           previous_state: Dict[str, Any]) -> DiffSummary:
        """Create summary of diff operation."""
        if not changes:
            return DiffSummary(
                total_changes=0,
                operations_count={},
                changed_paths=[],
                change_magnitude_avg=0.0,
                diff_timestamp=time.time(),
                diff_size_bytes=0
            )
            
        # Count operations
        ops_count = {}
        for change in changes:
            op = change.operation.value
            ops_count[op] = ops_count.get(op, 0) + 1
            
        # Calculate average magnitude
        avg_magnitude = sum(c.change_magnitude for c in changes) / len(changes)
        
        # Calculate size metrics
        diff_data = [c.to_dict() for c in changes]
        diff_size = len(json.dumps(diff_data))
        full_state_size = len(json.dumps(current_state))
        compression_ratio = 1.0 - (diff_size / full_state_size) if full_state_size > 0 else 0.0
        
        self.metrics["compression_ratios"].append(compression_ratio)
        
        return DiffSummary(
            total_changes=len(changes),
            operations_count=ops_count,
            changed_paths=[c.path for c in changes],
            change_magnitude_avg=avg_magnitude,
            diff_timestamp=time.time(),
            diff_size_bytes=diff_size,
            compression_ratio=compression_ratio
        )
        
    def get_cached_diff(self, diff_id: str) -> Optional[List[StateDiff]]:
        """Get cached diff by ID."""
        return self.diff_cache.get(diff_id)
        
    def clear_cache(self, max_age_seconds: float = 3600):
        """Clear old cached diffs."""
        current_time = time.time()
        to_remove = []
        
        for diff_id, changes in self.diff_cache.items():
            if changes and (current_time - changes[0].timestamp) > max_age_seconds:
                to_remove.append(diff_id)
                
        for diff_id in to_remove:
            del self.diff_cache[diff_id]
            
    def get_performance_metrics(self) -> Dict[str, Any]:
        """Get diff engine performance metrics."""
        avg_compression = (
            sum(self.metrics["compression_ratios"]) / len(self.metrics["compression_ratios"])
            if self.metrics["compression_ratios"] else 0.0
        )
        
        return {
            "total_diffs_computed": self.metrics["total_diffs_computed"],
            "total_changes_tracked": self.metrics["total_changes_tracked"],
            "avg_diff_time_ms": self.metrics["avg_diff_time_ms"],
            "avg_compression_ratio": avg_compression,
            "cache_size": len(self.diff_cache),
            "changes_per_diff": (
                self.metrics["total_changes_tracked"] / self.metrics["total_diffs_computed"]
                if self.metrics["total_diffs_computed"] > 0 else 0.0
            )
        }


# Global diff engine instance
_global_diff_engine: Optional[IncrementalStateDiff] = None


def get_global_diff_engine() -> IncrementalStateDiff:
    """Get or create global diff engine instance."""
    global _global_diff_engine
    if _global_diff_engine is None:
        _global_diff_engine = IncrementalStateDiff()
    return _global_diff_engine


def compute_state_diff(current_state: Dict[str, Any], 
                      previous_state: Optional[Dict[str, Any]] = None,
                      diff_id: str = None) -> Tuple[List[StateDiff], DiffSummary]:
    """Convenience function to compute state diff using global engine."""
    engine = get_global_diff_engine()
    return engine.compute_diff(current_state, previous_state, diff_id)