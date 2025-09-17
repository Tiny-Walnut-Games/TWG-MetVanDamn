#!/usr/bin/env python3
"""
Streaming Ingestion Pipeline - Performance Optimization for v0.6

Implements adaptive backpressure and batch processing for high-throughput 
fragment ingestion without overwhelming the summarization ladder.

🧙‍♂️ "Like a river that knows when to rush and when to pool,
    the pipeline must adapt its flow to the castle's capacity." - Bootstrap Sentinel
"""

from typing import List, Dict, Any, Optional, Callable, Deque
import time
import asyncio
import threading
from collections import deque
from dataclasses import dataclass
from enum import Enum
import queue


class PipelineState(Enum):
    """Pipeline operational states."""
    STOPPED = "stopped"
    STARTING = "starting" 
    RUNNING = "running"
    BACKPRESSURE = "backpressure"
    DRAINING = "draining"
    ERROR = "error"


class BackpressureLevel(Enum):
    """Backpressure severity levels."""
    NONE = "none"
    LOW = "low"
    MEDIUM = "medium"
    HIGH = "high"
    CRITICAL = "critical"


@dataclass
class Fragment:
    """Data fragment for ingestion."""
    fragment_id: str
    content: str
    metadata: Dict[str, Any]
    priority: int = 1  # 1=low, 5=high
    timestamp: float = 0.0
    retry_count: int = 0
    
    def __post_init__(self):
        if self.timestamp == 0.0:
            self.timestamp = time.time()


@dataclass
class BatchContext:
    """Context for batch processing operations."""
    batch_id: str
    fragments: List[Fragment]
    batch_size: int
    processing_start_time: float
    estimated_completion_time: float = 0.0
    
    def get_processing_duration(self) -> float:
        """Get current processing duration in seconds."""
        return time.time() - self.processing_start_time


@dataclass
class PipelineMetrics:
    """Performance metrics for the streaming pipeline."""
    total_fragments_processed: int = 0
    total_batches_processed: int = 0
    total_processing_time_ms: float = 0.0
    average_batch_size: float = 0.0
    backpressure_events: int = 0
    failed_fragments: int = 0
    throughput_fragments_per_sec: float = 0.0
    queue_depth_max: int = 0
    queue_depth_current: int = 0
    
    def update_throughput(self, window_size_sec: float = 60.0):
        """Update throughput calculation."""
        if self.total_processing_time_ms > 0:
            processing_time_sec = self.total_processing_time_ms / 1000.0
            self.throughput_fragments_per_sec = self.total_fragments_processed / processing_time_sec


class StreamingIngestionPipeline:
    """
    High-performance streaming ingestion pipeline with adaptive backpressure.
    
    Features:
    - Adaptive batch sizing based on processing capacity
    - Backpressure detection and automatic throttling
    - Priority-based fragment ordering
    - Concurrent processing with thread safety
    - Comprehensive performance metrics
    """
    
    def __init__(self,
                 processor_func: Callable[[List[Fragment]], bool],
                 initial_batch_size: int = 10,
                 max_batch_size: int = 100,
                 max_queue_size: int = 1000,
                 backpressure_threshold: float = 0.8,
                 processing_timeout_sec: float = 30.0):
        """
        Initialize streaming pipeline.
        
        Args:
            processor_func: Function to process batches of fragments
            initial_batch_size: Starting batch size
            max_batch_size: Maximum allowed batch size
            max_queue_size: Maximum fragments in queue before backpressure
            backpressure_threshold: Queue utilization threshold for backpressure
            processing_timeout_sec: Timeout for batch processing
        """
        self.processor_func = processor_func
        self.initial_batch_size = initial_batch_size
        self.max_batch_size = max_batch_size
        self.max_queue_size = max_queue_size
        self.backpressure_threshold = backpressure_threshold
        self.processing_timeout_sec = processing_timeout_sec
        
        # Pipeline state
        self.state = PipelineState.STOPPED
        self.backpressure_level = BackpressureLevel.NONE
        
        # Queues and threading
        self.ingestion_queue: queue.PriorityQueue = queue.PriorityQueue(maxsize=max_queue_size)
        self.processing_thread: Optional[threading.Thread] = None
        self.stop_event = threading.Event()
        
        # Adaptive configuration
        self.current_batch_size = initial_batch_size
        self.adaptive_enabled = True
        
        # Performance tracking
        self.metrics = PipelineMetrics()
        self.processing_history: Deque[float] = deque(maxlen=100)  # Last 100 processing times
        
        # Callbacks
        self.backpressure_callback: Optional[Callable[[BackpressureLevel], None]] = None
        self.metrics_callback: Optional[Callable[[PipelineMetrics], None]] = None
        
    def start(self):
        """Start the streaming pipeline."""
        if self.state != PipelineState.STOPPED:
            raise RuntimeError(f"Pipeline already running: {self.state}")
            
        self.state = PipelineState.STARTING
        self.stop_event.clear()
        
        # Start processing thread
        self.processing_thread = threading.Thread(target=self._processing_loop, daemon=True)
        self.processing_thread.start()
        
        self.state = PipelineState.RUNNING
        
    def stop(self, timeout_sec: float = 10.0):
        """Stop the streaming pipeline gracefully."""
        if self.state == PipelineState.STOPPED:
            return
            
        self.state = PipelineState.DRAINING
        self.stop_event.set()
        
        if self.processing_thread:
            self.processing_thread.join(timeout=timeout_sec)
            
        self.state = PipelineState.STOPPED
        
    def ingest_fragment(self, fragment: Fragment, block: bool = True, timeout: float = None) -> bool:
        """
        Ingest a fragment into the pipeline.
        
        Args:
            fragment: Fragment to ingest
            block: Whether to block if queue is full
            timeout: Timeout for blocking operation
            
        Returns:
            True if fragment was ingested, False if rejected due to backpressure
        """
        if self.state not in [PipelineState.RUNNING, PipelineState.BACKPRESSURE]:
            return False
            
        try:
            # Priority queue uses negative priority for max-heap behavior
            priority_key = (-fragment.priority, fragment.timestamp)
            self.ingestion_queue.put((priority_key, fragment), block=block, timeout=timeout)
            
            # Update queue metrics
            self.metrics.queue_depth_current = self.ingestion_queue.qsize()
            self.metrics.queue_depth_max = max(
                self.metrics.queue_depth_max, 
                self.metrics.queue_depth_current
            )
            
            # Check for backpressure
            self._check_backpressure()
            
            return True
            
        except queue.Full:
            # Queue is full - backpressure event
            self.metrics.backpressure_events += 1
            if self.backpressure_callback:
                self.backpressure_callback(BackpressureLevel.CRITICAL)
            return False
            
    def ingest_batch(self, fragments: List[Fragment]) -> int:
        """
        Ingest multiple fragments efficiently.
        
        Returns:
            Number of fragments successfully ingested
        """
        ingested_count = 0
        
        for fragment in fragments:
            if self.ingest_fragment(fragment, block=False):
                ingested_count += 1
            else:
                break  # Stop on first failure to avoid overwhelming
                
        return ingested_count
        
    def _processing_loop(self):
        """Main processing loop running in separate thread."""
        while not self.stop_event.is_set():
            try:
                # Collect batch of fragments
                batch = self._collect_batch()
                
                if not batch.fragments:
                    time.sleep(0.1)  # Brief pause if no work
                    continue
                    
                # Process the batch
                success = self._process_batch(batch)
                
                if success:
                    self._update_metrics(batch)
                    self._adapt_batch_size(batch)
                else:
                    self._handle_processing_failure(batch)
                    
            except Exception as e:
                self.state = PipelineState.ERROR
                print(f"Pipeline error: {e}")
                break
                
    def _collect_batch(self) -> BatchContext:
        """Collect fragments into a batch for processing."""
        fragments = []
        batch_id = f"batch_{int(time.time() * 1000)}"
        start_time = time.time()
        
        # Collect up to current_batch_size fragments
        while len(fragments) < self.current_batch_size:
            try:
                # Non-blocking get with short timeout
                priority_key, fragment = self.ingestion_queue.get(timeout=0.1)
                fragments.append(fragment)
                self.ingestion_queue.task_done()
                
            except queue.Empty:
                break  # No more fragments available
                
        return BatchContext(
            batch_id=batch_id,
            fragments=fragments,
            batch_size=len(fragments),
            processing_start_time=start_time
        )
        
    def _process_batch(self, batch: BatchContext) -> bool:
        """Process a batch of fragments."""
        if not batch.fragments:
            return True
            
        try:
            # Call the processor function with timeout
            success = self.processor_func(batch.fragments)
            
            # Update queue depth after processing
            self.metrics.queue_depth_current = self.ingestion_queue.qsize()
            
            return success
            
        except Exception as e:
            print(f"Batch processing error: {e}")
            return False
            
    def _update_metrics(self, batch: BatchContext):
        """Update performance metrics after successful batch processing."""
        processing_duration = batch.get_processing_duration()
        processing_time_ms = processing_duration * 1000
        
        # Update counters
        self.metrics.total_fragments_processed += len(batch.fragments)
        self.metrics.total_batches_processed += 1
        self.metrics.total_processing_time_ms += processing_time_ms
        
        # Update averages
        self.metrics.average_batch_size = (
            self.metrics.total_fragments_processed / self.metrics.total_batches_processed
        )
        
        # Track processing time history
        self.processing_history.append(processing_time_ms)
        
        # Update throughput
        self.metrics.update_throughput()
        
        # Trigger metrics callback
        if self.metrics_callback:
            self.metrics_callback(self.metrics)
            
    def _adapt_batch_size(self, batch: BatchContext):
        """Adapt batch size based on processing performance."""
        if not self.adaptive_enabled or len(self.processing_history) < 5:
            return
            
        # Calculate recent average processing time
        recent_times = list(self.processing_history)[-5:]
        avg_processing_time = sum(recent_times) / len(recent_times)
        
        # Target processing time (aiming for ~100ms per batch)
        target_time_ms = 100.0
        
        if avg_processing_time < target_time_ms * 0.7:
            # Processing is fast, can increase batch size
            self.current_batch_size = min(
                self.current_batch_size + 2,
                self.max_batch_size
            )
        elif avg_processing_time > target_time_ms * 1.5:
            # Processing is slow, should decrease batch size
            self.current_batch_size = max(
                self.current_batch_size - 1,
                1
            )
            
    def _handle_processing_failure(self, batch: BatchContext):
        """Handle batch processing failure."""
        self.metrics.failed_fragments += len(batch.fragments)
        
        # Retry logic for individual fragments
        for fragment in batch.fragments:
            fragment.retry_count += 1
            
            if fragment.retry_count < 3:  # Max 3 retries
                # Put back in queue with lower priority
                retry_fragment = Fragment(
                    fragment_id=f"{fragment.fragment_id}_retry_{fragment.retry_count}",
                    content=fragment.content,
                    metadata=fragment.metadata,
                    priority=max(1, fragment.priority - 1),  # Lower priority
                    retry_count=fragment.retry_count
                )
                self.ingest_fragment(retry_fragment, block=False)
                
    def _check_backpressure(self):
        """Check and update backpressure level."""
        queue_utilization = self.metrics.queue_depth_current / self.max_queue_size
        
        # Determine backpressure level
        if queue_utilization >= 0.95:
            level = BackpressureLevel.CRITICAL
        elif queue_utilization >= 0.85:
            level = BackpressureLevel.HIGH
        elif queue_utilization >= 0.70:
            level = BackpressureLevel.MEDIUM
        elif queue_utilization >= 0.50:
            level = BackpressureLevel.LOW
        else:
            level = BackpressureLevel.NONE
            
        # Update state if backpressure level changed
        if level != self.backpressure_level:
            self.backpressure_level = level
            
            # Update pipeline state
            if level in [BackpressureLevel.HIGH, BackpressureLevel.CRITICAL]:
                self.state = PipelineState.BACKPRESSURE
            elif self.state == PipelineState.BACKPRESSURE and level <= BackpressureLevel.MEDIUM:
                self.state = PipelineState.RUNNING
                
            # Trigger callback
            if self.backpressure_callback:
                self.backpressure_callback(level)
                
    def get_status(self) -> Dict[str, Any]:
        """Get comprehensive pipeline status."""
        return {
            "state": self.state.value,
            "backpressure_level": self.backpressure_level.value,
            "current_batch_size": self.current_batch_size,
            "queue_depth": self.metrics.queue_depth_current,
            "queue_utilization_pct": (self.metrics.queue_depth_current / self.max_queue_size) * 100,
            "metrics": {
                "total_processed": self.metrics.total_fragments_processed,
                "total_batches": self.metrics.total_batches_processed,
                "throughput_fps": self.metrics.throughput_fragments_per_sec,
                "avg_batch_size": self.metrics.average_batch_size,
                "backpressure_events": self.metrics.backpressure_events,
                "failed_fragments": self.metrics.failed_fragments
            },
            "performance": {
                "avg_processing_time_ms": (
                    sum(self.processing_history) / len(self.processing_history)
                    if self.processing_history else 0.0
                ),
                "recent_processing_times": list(self.processing_history)[-10:],
                "adaptive_enabled": self.adaptive_enabled
            }
        }
        
    def configure_adaptive_mode(self, enabled: bool, target_time_ms: float = 100.0):
        """Configure adaptive batch sizing."""
        self.adaptive_enabled = enabled
        self.target_processing_time = target_time_ms
        
    def set_backpressure_callback(self, callback: Callable[[BackpressureLevel], None]):
        """Set callback for backpressure events."""
        self.backpressure_callback = callback
        
    def set_metrics_callback(self, callback: Callable[[PipelineMetrics], None]):
        """Set callback for metrics updates."""
        self.metrics_callback = callback
        
    def reset_metrics(self):
        """Reset performance metrics."""
        self.metrics = PipelineMetrics()
        self.processing_history.clear()


def create_default_pipeline(processor_func: Callable[[List[Fragment]], bool],
                          performance_profile: str = "balanced") -> StreamingIngestionPipeline:
    """
    Create pipeline with predefined performance profiles.
    
    Args:
        processor_func: Function to process fragment batches
        performance_profile: 'dev', 'balanced', 'performance', 'experiment'
    """
    profiles = {
        "dev": {
            "initial_batch_size": 5,
            "max_batch_size": 20,
            "max_queue_size": 100,
            "backpressure_threshold": 0.7
        },
        "balanced": {
            "initial_batch_size": 10,
            "max_batch_size": 50,
            "max_queue_size": 500,
            "backpressure_threshold": 0.8
        },
        "performance": {
            "initial_batch_size": 25,
            "max_batch_size": 100,
            "max_queue_size": 1000,
            "backpressure_threshold": 0.9
        },
        "experiment": {
            "initial_batch_size": 50,
            "max_batch_size": 200,
            "max_queue_size": 2000,
            "backpressure_threshold": 0.95
        }
    }
    
    config = profiles.get(performance_profile, profiles["balanced"])
    
    return StreamingIngestionPipeline(
        processor_func=processor_func,
        **config
    )