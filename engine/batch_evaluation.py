#!/usr/bin/env python3
"""
Batch Evaluation System - Large Corpus Replay Optimization for v0.6

Implements efficient batch processing for large-scale corpus replay
and bulk evaluation operations with progress tracking and resumption.

🧙‍♂️ "When facing an army of fragments, divide and conquer - 
    but never forget to document the battle plan." - Bootstrap Sentinel
"""

from typing import List, Dict, Any, Optional, Callable, Iterator, Tuple
import time
import json
import hashlib
from dataclasses import dataclass, asdict
from pathlib import Path
from enum import Enum
import threading
from concurrent.futures import ThreadPoolExecutor, as_completed


class BatchStatus(Enum):
    """Batch processing status."""
    PENDING = "pending"
    RUNNING = "running"
    COMPLETED = "completed"
    FAILED = "failed"
    CANCELLED = "cancelled"


class ReplayMode(Enum):
    """Corpus replay modes."""
    SEQUENTIAL = "sequential"
    PARALLEL = "parallel"
    ADAPTIVE = "adaptive"
    PRIORITY_BASED = "priority_based"


@dataclass
class BatchItem:
    """Individual item in a batch for processing."""
    item_id: str
    content: Any
    metadata: Dict[str, Any]
    priority: int = 1
    estimated_processing_time: float = 1.0
    
    def __post_init__(self):
        if not self.item_id:
            # Generate ID from content hash
            content_str = json.dumps(self.content, sort_keys=True)
            self.item_id = hashlib.md5(content_str.encode()).hexdigest()[:12]


@dataclass
class BatchDefinition:
    """Definition of a processing batch."""
    batch_id: str
    items: List[BatchItem]
    batch_size: int
    estimated_duration: float = 0.0
    metadata: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.metadata is None:
            self.metadata = {}
        if self.estimated_duration == 0.0:
            self.estimated_duration = sum(item.estimated_processing_time for item in self.items)


@dataclass
class BatchResult:
    """Result of batch processing."""
    batch_id: str
    status: BatchStatus
    items_processed: int
    items_failed: int
    processing_time: float
    results: List[Any]
    error_details: Dict[str, str] = None
    timestamp: float = 0.0
    
    def __post_init__(self):
        if self.timestamp == 0.0:
            self.timestamp = time.time()
        if self.error_details is None:
            self.error_details = {}


@dataclass
class CorpusReplayProgress:
    """Progress tracking for corpus replay operations."""
    total_items: int
    processed_items: int
    failed_items: int
    total_batches: int
    completed_batches: int
    failed_batches: int
    start_time: float
    estimated_completion_time: float = 0.0
    current_throughput: float = 0.0
    
    def get_progress_percentage(self) -> float:
        """Get completion percentage."""
        return (self.processed_items / self.total_items * 100) if self.total_items > 0 else 0.0
    
    def get_elapsed_time(self) -> float:
        """Get elapsed processing time."""
        return time.time() - self.start_time
    
    def update_throughput(self):
        """Update current throughput calculation."""
        elapsed = self.get_elapsed_time()
        if elapsed > 0:
            self.current_throughput = self.processed_items / elapsed


class BatchEvaluationEngine:
    """
    High-performance batch evaluation engine for large corpus replay.
    
    Features:
    - Configurable batch sizing and parallelism
    - Progress tracking and resumption capabilities
    - Error handling and retry logic
    - Performance optimization based on system resources
    - Checkpoint/resume functionality for long-running operations
    """
    
    def __init__(self,
                 max_workers: int = 4,
                 default_batch_size: int = 50,
                 max_batch_size: int = 200,
                 checkpoint_interval: int = 100,
                 checkpoint_dir: str = "data/batch_checkpoints"):
        """
        Initialize batch evaluation engine.
        
        Args:
            max_workers: Maximum number of worker threads
            default_batch_size: Default size for batches
            max_batch_size: Maximum allowed batch size
            checkpoint_interval: Items between checkpoints
            checkpoint_dir: Directory for checkpoint files
        """
        self.max_workers = max_workers
        self.default_batch_size = default_batch_size
        self.max_batch_size = max_batch_size
        self.checkpoint_interval = checkpoint_interval
        self.checkpoint_dir = Path(checkpoint_dir)
        self.checkpoint_dir.mkdir(parents=True, exist_ok=True)
        
        # State tracking
        self.current_operation_id: Optional[str] = None
        self.progress: Optional[CorpusReplayProgress] = None
        self.batch_results: Dict[str, BatchResult] = {}
        
        # Thread safety
        self._lock = threading.Lock()
        
        # Callbacks
        self.progress_callback: Optional[Callable[[CorpusReplayProgress], None]] = None
        self.batch_completion_callback: Optional[Callable[[BatchResult], None]] = None
        
    def process_corpus(self,
                      corpus: List[Any],
                      processor_func: Callable[[List[Any]], List[Any]],
                      operation_id: str = None,
                      mode: ReplayMode = ReplayMode.ADAPTIVE,
                      batch_size: int = None,
                      resume_from_checkpoint: bool = True) -> Dict[str, Any]:
        """
        Process a large corpus using batch evaluation.
        
        Args:
            corpus: List of items to process
            processor_func: Function to process batches
            operation_id: Unique identifier for this operation
            mode: Processing mode (sequential, parallel, etc.)
            batch_size: Override default batch size
            resume_from_checkpoint: Whether to resume from existing checkpoint
            
        Returns:
            Dictionary containing results and performance metrics
        """
        if operation_id is None:
            operation_id = f"corpus_replay_{int(time.time())}"
            
        self.current_operation_id = operation_id
        
        # Check for existing checkpoint
        checkpoint_data = None
        if resume_from_checkpoint:
            checkpoint_data = self._load_checkpoint(operation_id)
            
        # Initialize or restore progress
        if checkpoint_data:
            self.progress = CorpusReplayProgress(**checkpoint_data['progress'])
            processed_items = checkpoint_data.get('processed_items', [])
            remaining_corpus = [item for i, item in enumerate(corpus) 
                              if i not in processed_items]
        else:
            remaining_corpus = corpus
            self.progress = CorpusReplayProgress(
                total_items=len(corpus),
                processed_items=0,
                failed_items=0,
                total_batches=0,
                completed_batches=0,
                failed_batches=0,
                start_time=time.time()
            )
            
        # Determine optimal batch size
        if batch_size is None:
            batch_size = self._calculate_optimal_batch_size(len(remaining_corpus), mode)
            
        # Create batches
        batches = self._create_batches(remaining_corpus, batch_size, operation_id)
        self.progress.total_batches = len(batches)
        
        # Process batches based on mode
        try:
            if mode == ReplayMode.SEQUENTIAL:
                results = self._process_sequential(batches, processor_func)
            elif mode == ReplayMode.PARALLEL:
                results = self._process_parallel(batches, processor_func)
            elif mode == ReplayMode.ADAPTIVE:
                results = self._process_adaptive(batches, processor_func)
            else:
                results = self._process_priority_based(batches, processor_func)
                
            # Final metrics
            final_metrics = self._calculate_final_metrics()
            
            # Cleanup checkpoint
            self._cleanup_checkpoint(operation_id)
            
            return {
                "operation_id": operation_id,
                "status": "completed",
                "results": results,
                "metrics": final_metrics,
                "progress": asdict(self.progress)
            }
            
        except Exception as e:
            # Save checkpoint on error
            self._save_checkpoint(operation_id)
            
            return {
                "operation_id": operation_id,
                "status": "failed",
                "error": str(e),
                "progress": asdict(self.progress) if self.progress else None
            }
            
    def _calculate_optimal_batch_size(self, corpus_size: int, mode: ReplayMode) -> int:
        """Calculate optimal batch size based on corpus size and mode."""
        if mode == ReplayMode.SEQUENTIAL:
            # Larger batches for sequential processing
            return min(self.max_batch_size, max(self.default_batch_size * 2, corpus_size // 20))
        elif mode == ReplayMode.PARALLEL:
            # Smaller batches for better parallelization
            return min(self.default_batch_size, max(10, corpus_size // (self.max_workers * 4)))
        else:  # ADAPTIVE or PRIORITY_BASED
            # Balanced approach
            return min(self.max_batch_size, max(self.default_batch_size, corpus_size // 50))
            
    def _create_batches(self, corpus: List[Any], batch_size: int, operation_id: str) -> List[BatchDefinition]:
        """Create batch definitions from corpus."""
        batches = []
        
        for i in range(0, len(corpus), batch_size):
            batch_items = []
            batch_corpus = corpus[i:i + batch_size]
            
            for j, item in enumerate(batch_corpus):
                batch_item = BatchItem(
                    item_id=f"{operation_id}_item_{i + j}",
                    content=item,
                    metadata={"corpus_index": i + j, "batch_index": j}
                )
                batch_items.append(batch_item)
                
            batch_def = BatchDefinition(
                batch_id=f"{operation_id}_batch_{len(batches)}",
                items=batch_items,
                batch_size=len(batch_items),
                metadata={"start_index": i, "end_index": i + len(batch_items)}
            )
            batches.append(batch_def)
            
        return batches
        
    def _process_sequential(self, batches: List[BatchDefinition], 
                          processor_func: Callable) -> List[Any]:
        """Process batches sequentially."""
        all_results = []
        
        for batch in batches:
            try:
                batch_result = self._process_single_batch(batch, processor_func)
                all_results.extend(batch_result.results)
                
                # Update progress
                with self._lock:
                    self.progress.completed_batches += 1
                    self.progress.processed_items += batch_result.items_processed
                    self.progress.failed_items += batch_result.items_failed
                    self.progress.update_throughput()
                    
                # Checkpoint periodically
                if self.progress.processed_items % self.checkpoint_interval == 0:
                    self._save_checkpoint(self.current_operation_id)
                    
                # Progress callback
                if self.progress_callback:
                    self.progress_callback(self.progress)
                    
            except Exception as e:
                print(f"Batch processing error: {e}")
                with self._lock:
                    self.progress.failed_batches += 1
                    
        return all_results
        
    def _process_parallel(self, batches: List[BatchDefinition], 
                         processor_func: Callable) -> List[Any]:
        """Process batches in parallel."""
        all_results = []
        
        with ThreadPoolExecutor(max_workers=self.max_workers) as executor:
            # Submit all batch jobs
            future_to_batch = {
                executor.submit(self._process_single_batch, batch, processor_func): batch
                for batch in batches
            }
            
            # Collect results as they complete
            for future in as_completed(future_to_batch):
                batch = future_to_batch[future]
                try:
                    batch_result = future.result()
                    all_results.extend(batch_result.results)
                    
                    # Update progress
                    with self._lock:
                        self.progress.completed_batches += 1
                        self.progress.processed_items += batch_result.items_processed
                        self.progress.failed_items += batch_result.items_failed
                        self.progress.update_throughput()
                        
                    # Progress callback
                    if self.progress_callback:
                        self.progress_callback(self.progress)
                        
                except Exception as e:
                    print(f"Batch {batch.batch_id} failed: {e}")
                    with self._lock:
                        self.progress.failed_batches += 1
                        
        return all_results
        
    def _process_adaptive(self, batches: List[BatchDefinition], 
                         processor_func: Callable) -> List[Any]:
        """Process batches with adaptive parallelism."""
        # Start with a small number of parallel workers
        current_workers = min(2, self.max_workers)
        all_results = []
        batch_queue = list(batches)
        processing_times = []
        
        while batch_queue:
            # Take batches for current worker count
            current_batches = batch_queue[:current_workers]
            batch_queue = batch_queue[current_workers:]
            
            start_time = time.time()
            
            with ThreadPoolExecutor(max_workers=current_workers) as executor:
                futures = [
                    executor.submit(self._process_single_batch, batch, processor_func)
                    for batch in current_batches
                ]
                
                for future in as_completed(futures):
                    try:
                        batch_result = future.result()
                        all_results.extend(batch_result.results)
                        
                        with self._lock:
                            self.progress.completed_batches += 1
                            self.progress.processed_items += batch_result.items_processed
                            self.progress.failed_items += batch_result.items_failed
                            self.progress.update_throughput()
                            
                    except Exception as e:
                        print(f"Adaptive batch processing error: {e}")
                        with self._lock:
                            self.progress.failed_batches += 1
                            
            # Measure performance and adapt
            batch_time = time.time() - start_time
            processing_times.append(batch_time)
            
            # Adapt worker count based on recent performance
            if len(processing_times) >= 3:
                recent_avg = sum(processing_times[-3:]) / 3
                if len(processing_times) >= 6:
                    older_avg = sum(processing_times[-6:-3]) / 3
                    
                    # Increase workers if performance improved
                    if recent_avg < older_avg * 0.9 and current_workers < self.max_workers:
                        current_workers = min(current_workers + 1, self.max_workers)
                    # Decrease workers if performance degraded
                    elif recent_avg > older_avg * 1.2 and current_workers > 1:
                        current_workers = max(current_workers - 1, 1)
                        
        return all_results
        
    def _process_priority_based(self, batches: List[BatchDefinition], 
                              processor_func: Callable) -> List[Any]:
        """Process batches based on priority and estimated processing time."""
        # Sort batches by priority and estimated time
        sorted_batches = sorted(batches, 
                              key=lambda b: (-max(item.priority for item in b.items), 
                                           b.estimated_duration))
        
        return self._process_parallel(sorted_batches, processor_func)
        
    def _process_single_batch(self, batch: BatchDefinition, 
                            processor_func: Callable) -> BatchResult:
        """Process a single batch and return results."""
        start_time = time.time()
        
        try:
            # Extract content from batch items
            batch_content = [item.content for item in batch.items]
            
            # Process the batch
            results = processor_func(batch_content)
            
            processing_time = time.time() - start_time
            
            batch_result = BatchResult(
                batch_id=batch.batch_id,
                status=BatchStatus.COMPLETED,
                items_processed=len(batch.items),
                items_failed=0,
                processing_time=processing_time,
                results=results
            )
            
            # Store result
            self.batch_results[batch.batch_id] = batch_result
            
            # Batch completion callback
            if self.batch_completion_callback:
                self.batch_completion_callback(batch_result)
                
            return batch_result
            
        except Exception as e:
            processing_time = time.time() - start_time
            
            batch_result = BatchResult(
                batch_id=batch.batch_id,
                status=BatchStatus.FAILED,
                items_processed=0,
                items_failed=len(batch.items),
                processing_time=processing_time,
                results=[],
                error_details={"error": str(e)}
            )
            
            self.batch_results[batch.batch_id] = batch_result
            return batch_result
            
    def _save_checkpoint(self, operation_id: str):
        """Save progress checkpoint."""
        if not self.progress:
            return
            
        checkpoint_file = self.checkpoint_dir / f"{operation_id}.json"
        
        # Get processed item indices
        processed_items = []
        for batch_result in self.batch_results.values():
            if batch_result.status == BatchStatus.COMPLETED:
                batch_def = next(
                    (b for b in [] if b.batch_id == batch_result.batch_id), 
                    None
                )
                if batch_def:
                    for item in batch_def.items:
                        processed_items.append(item.metadata.get('corpus_index', 0))
                        
        checkpoint_data = {
            "operation_id": operation_id,
            "timestamp": time.time(),
            "progress": asdict(self.progress),
            "processed_items": processed_items,
            "batch_results": {k: asdict(v) for k, v in self.batch_results.items()}
        }
        
        try:
            with open(checkpoint_file, 'w') as f:
                json.dump(checkpoint_data, f, indent=2)
        except Exception as e:
            print(f"Failed to save checkpoint: {e}")
            
    def _load_checkpoint(self, operation_id: str) -> Optional[Dict[str, Any]]:
        """Load progress checkpoint."""
        checkpoint_file = self.checkpoint_dir / f"{operation_id}.json"
        
        if not checkpoint_file.exists():
            return None
            
        try:
            with open(checkpoint_file, 'r') as f:
                return json.load(f)
        except Exception as e:
            print(f"Failed to load checkpoint: {e}")
            return None
            
    def _cleanup_checkpoint(self, operation_id: str):
        """Remove checkpoint file after successful completion."""
        checkpoint_file = self.checkpoint_dir / f"{operation_id}.json"
        if checkpoint_file.exists():
            try:
                checkpoint_file.unlink()
            except Exception as e:
                print(f"Failed to cleanup checkpoint: {e}")
                
    def _calculate_final_metrics(self) -> Dict[str, Any]:
        """Calculate final performance metrics."""
        if not self.progress:
            return {}
            
        total_time = self.progress.get_elapsed_time()
        success_rate = (
            (self.progress.processed_items / self.progress.total_items * 100)
            if self.progress.total_items > 0 else 0.0
        )
        
        return {
            "total_processing_time_sec": total_time,
            "total_items": self.progress.total_items,
            "processed_items": self.progress.processed_items,
            "failed_items": self.progress.failed_items,
            "success_rate_pct": success_rate,
            "throughput_items_per_sec": self.progress.current_throughput,
            "total_batches": self.progress.total_batches,
            "completed_batches": self.progress.completed_batches,
            "failed_batches": self.progress.failed_batches,
            "average_batch_processing_time": (
                sum(r.processing_time for r in self.batch_results.values()) / 
                len(self.batch_results) if self.batch_results else 0.0
            )
        }
        
    def set_progress_callback(self, callback: Callable[[CorpusReplayProgress], None]):
        """Set callback for progress updates."""
        self.progress_callback = callback
        
    def set_batch_completion_callback(self, callback: Callable[[BatchResult], None]):
        """Set callback for batch completion events."""
        self.batch_completion_callback = callback
        
    def get_current_progress(self) -> Optional[CorpusReplayProgress]:
        """Get current processing progress."""
        return self.progress
        
    def cancel_operation(self):
        """Cancel the current operation."""
        # Implementation would involve setting a cancellation flag
        # and checking it in processing loops
        pass


# Convenience functions for common operations
def batch_process_corpus(corpus: List[Any], 
                        processor_func: Callable,
                        batch_size: int = 50,
                        mode: ReplayMode = ReplayMode.ADAPTIVE,
                        max_workers: int = 4) -> Dict[str, Any]:
    """Convenience function for batch processing a corpus."""
    engine = BatchEvaluationEngine(max_workers=max_workers, default_batch_size=batch_size)
    return engine.process_corpus(corpus, processor_func, mode=mode, batch_size=batch_size)