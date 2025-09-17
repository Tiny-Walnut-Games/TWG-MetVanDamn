#!/usr/bin/env python3
"""
Giant-in-the-Well DevTimeTravel Compression System

Implements layer transitions for snapshot compaction:
- raw → compacted → daily aggregate
- Content hash-based deduplication
- Pressure-based compression triggers
- Index generation with layer metrics
"""

import os
import sys
import json
import yaml
import gzip
import shutil
import argparse
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Any, Optional
import subprocess


class SnapshotCompressor:
    """Compresses DevTimeTravel snapshots and manages compaction"""
    
    def __init__(self, root_path: str = ".devtimetravel", verbose: bool = False):
        self.root_path = Path(root_path)
        self.verbose = verbose
        self.snapshots_path = self.root_path / "snapshots"
        self.compacted_path = self.root_path / "compacted"
        self.daily_path = self.root_path / "daily"
        
        # Ensure directories exist
        for path in [self.snapshots_path, self.compacted_path, self.daily_path]:
            path.mkdir(parents=True, exist_ok=True)
    
    def log(self, message: str, level: str = "INFO"):
        """Log messages with timestamp"""
        if level == "VERBOSE" and not self.verbose:
            return
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] [{level}] {message}")
    
    def compress_file(self, source_path: Path, target_path: Path) -> bool:
        """Compress a file using gzip"""
        try:
            with open(source_path, 'rb') as f_in:
                with gzip.open(target_path, 'wb') as f_out:
                    shutil.copyfileobj(f_in, f_out)
            return True
        except Exception as e:
            self.log(f"Error compressing {source_path}: {e}", "ERROR")
            return False
    
    def load_yaml_file(self, filepath: Path) -> Optional[Dict[str, Any]]:
        """Load YAML file content"""
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                return yaml.safe_load(f)
        except Exception as e:
            self.log(f"Error loading {filepath}: {e}", "ERROR")
            return None
    
    def save_yaml_file(self, filepath: Path, data: Dict[str, Any]) -> bool:
        """Save data to YAML file"""
        try:
            with open(filepath, 'w', encoding='utf-8') as f:
                yaml.dump(data, f, default_flow_style=False, sort_keys=True)
            return True
        except Exception as e:
            self.log(f"Error saving {filepath}: {e}", "ERROR")
            return False
    
    def get_file_size(self, filepath: Path) -> int:
        """Get file size in bytes"""
        try:
            return filepath.stat().st_size
        except:
            return 0
    
    def compress_snapshots(self, remove_originals: bool = False) -> Dict[str, Any]:
        """Compress snapshot files"""
        self.log("Starting snapshot compression")
        
        stats = {
            "files_processed": 0,
            "files_compressed": 0,
            "original_size": 0,
            "compressed_size": 0,
            "compression_ratio": 0.0,
            "errors": []
        }
        
        # Process YAML files in snapshots directory
        for yaml_file in self.snapshots_path.glob("*.yaml"):
            stats["files_processed"] += 1
            original_size = self.get_file_size(yaml_file)
            stats["original_size"] += original_size
            
            # Create compressed version
            compressed_file = self.compacted_path / f"{yaml_file.stem}.yaml.gz"
            
            if self.compress_file(yaml_file, compressed_file):
                stats["files_compressed"] += 1
                compressed_size = self.get_file_size(compressed_file)
                stats["compressed_size"] += compressed_size
                
                self.log(f"Compressed {yaml_file.name} ({original_size} -> {compressed_size} bytes)", "VERBOSE")
                
                # Remove original if requested
                if remove_originals:
                    try:
                        yaml_file.unlink()
                        self.log(f"Removed original {yaml_file.name}", "VERBOSE")
                    except Exception as e:
                        stats["errors"].append(f"Failed to remove {yaml_file.name}: {e}")
            else:
                stats["errors"].append(f"Failed to compress {yaml_file.name}")
        
        # Calculate compression ratio
        if stats["original_size"] > 0:
            stats["compression_ratio"] = 1.0 - (stats["compressed_size"] / stats["original_size"])
        
        self.log(f"Compression complete: {stats['files_compressed']}/{stats['files_processed']} files")
        self.log(f"Size reduction: {stats['compression_ratio']:.1%}")
        
        return stats
    
    def compact_daily_snapshots(self) -> Dict[str, Any]:
        """Compact snapshots into daily aggregates"""
        self.log("Starting daily snapshot compaction")
        
        stats = {
            "days_processed": 0,
            "snapshots_compacted": 0,
            "daily_files_created": 0,
            "errors": []
        }
        
        # Group snapshots by date
        daily_groups = {}
        for yaml_file in self.snapshots_path.glob("*.yaml"):
            # Extract date from filename (assuming YYYY-MM-DD prefix)
            try:
                date_str = yaml_file.name[:10]  # First 10 chars should be YYYY-MM-DD
                datetime.strptime(date_str, "%Y-%m-%d")  # Validate format
                
                if date_str not in daily_groups:
                    daily_groups[date_str] = []
                daily_groups[date_str].append(yaml_file)
            except ValueError:
                # Skip files that don't have valid date prefix
                continue
        
        # Create daily aggregates
        for date_str, snapshot_files in daily_groups.items():
            stats["days_processed"] += 1
            daily_file = self.daily_path / f"{date_str}-daily.yaml"
            
            # Skip if daily aggregate already exists
            if daily_file.exists():
                self.log(f"Daily aggregate for {date_str} already exists, skipping", "VERBOSE")
                continue
            
            # Collect snapshot data
            entries = []
            total_decisions = 0
            
            for snapshot_file in snapshot_files:
                snapshot_data = self.load_yaml_file(snapshot_file)
                if snapshot_data:
                    entries.append({
                        "file": snapshot_file.name,
                        "timestamp": snapshot_data.get("timestamp", ""),
                        "type": snapshot_data.get("type", "unknown"),
                        "summary": snapshot_data.get("summary", "")
                    })
                    
                    # Count decisions
                    if "decision" in snapshot_data or "decision_rationale" in snapshot_data:
                        total_decisions += 1
            
            # Create daily aggregate
            daily_data = {
                "date": date_str,
                "entries": len(entries),
                "snapshots": entries,
                "metrics": {
                    "total_snapshots": len(entries),
                    "decisions_made": total_decisions,
                    "counts": {
                        "snapshots": len(entries),
                        "decisions": total_decisions
                    }
                },
                "decision_index": f"Daily aggregate: {total_decisions} decisions from {len(entries)} snapshots",
                "created_at": datetime.now().isoformat(),
                "compaction_type": "daily_aggregate"
            }
            
            if self.save_yaml_file(daily_file, daily_data):
                stats["daily_files_created"] += 1
                stats["snapshots_compacted"] += len(entries)
                self.log(f"Created daily aggregate: {daily_file.name} ({len(entries)} snapshots)")
            else:
                stats["errors"].append(f"Failed to create daily aggregate for {date_str}")
        
        self.log(f"Daily compaction complete: {stats['daily_files_created']} daily files created")
        return stats
    
    def run_similarity_analysis(self) -> bool:
        """Run similarity analysis using dedupe_similarity.py"""
        self.log("Running similarity analysis")
        
        # Check if dedupe script exists
        # Look for dedupe script in the same directory as this script
        current_script_dir = Path(__file__).parent
        dedupe_script = current_script_dir / "dedupe_similarity.py"
        if not dedupe_script.exists():
            self.log("Dedupe similarity script not found, skipping similarity analysis", "WARNING")
            return False
        
        try:
            # Run dedupe analysis
            cmd = [sys.executable, str(dedupe_script), "--root", str(self.root_path)]
            if not self.verbose:
                cmd.append("--quiet")
            
            result = subprocess.run(cmd, capture_output=True, text=True)
            
            if result.returncode == 0:
                self.log("Similarity analysis completed successfully")
                return True
            else:
                self.log(f"Similarity analysis failed: {result.stderr}", "ERROR")
                return False
                
        except Exception as e:
            self.log(f"Error running similarity analysis: {e}", "ERROR")
            return False


def main():
    parser = argparse.ArgumentParser(description='DevTimeTravel Snapshot Compression and Compaction')
    parser.add_argument('--root', default='.devtimetravel',
                       help='DevTimeTravel root directory (default: .devtimetravel)')
    parser.add_argument('--compress', action='store_true',
                       help='Compress snapshot files with gzip')
    parser.add_argument('--compact', action='store_true',
                       help='Compact snapshots into daily aggregates')
    parser.add_argument('--similarity-pass', action='store_true',
                       help='Run similarity analysis after compaction')
    parser.add_argument('--remove-originals', action='store_true',
                       help='Remove original files after compression')
    parser.add_argument('--all', action='store_true',
                       help='Run all operations: compact, compress, similarity analysis')
    parser.add_argument('--verbose', action='store_true',
                       help='Enable verbose logging')
    
    args = parser.parse_args()
    
    # Validation
    if not any([args.compress, args.compact, args.similarity_pass, args.all]):
        parser.error("Must specify at least one operation: --compress, --compact, --similarity-pass, or --all")
    
    compressor = SnapshotCompressor(args.root, args.verbose)
    
    # Check if snapshots directory has content
    snapshots_exist = any(compressor.snapshots_path.glob("*.yaml"))
    
    if not snapshots_exist and (args.compact or args.all):
        print("ℹ️  No snapshots found for compaction")
    
    # Run operations
    if args.all or args.compact:
        if snapshots_exist:
            compact_stats = compressor.compact_daily_snapshots()
            print(f"📦 Compaction: {compact_stats['daily_files_created']} daily files created")
        
    if args.all or args.compress:
        compress_stats = compressor.compress_snapshots(args.remove_originals)
        print(f"🗜️  Compression: {compress_stats['files_compressed']} files compressed")
        if compress_stats['compression_ratio'] > 0:
            print(f"   Size reduction: {compress_stats['compression_ratio']:.1%}")
    
    if (args.all or args.similarity_pass) and (snapshots_exist or args.all):
        if compressor.run_similarity_analysis():
            print("🔍 Similarity analysis completed")
        else:
            print("⚠️  Similarity analysis skipped or failed")
    
    print("✅ Compression operations complete")


if __name__ == '__main__':
import hashlib
import argparse
import time
from pathlib import Path
from typing import Dict, List, Any, Optional
from datetime import datetime, timedelta


class DevTimeTravelCompressor:
    """Core compression engine for DevTimeTravel snapshots"""
    
    def __init__(self, root_path: str = ".devtimetravel", config_path: Optional[str] = None):
        self.root_path = Path(root_path)
        self.config_path = config_path
        self.config = self._load_config()
        
        # Ensure directory structure exists
        self._ensure_directories()
        
    def _load_config(self) -> Dict[str, Any]:
        """Load compression configuration"""
        default_config = {
            "retention": {
                "raw_days": 7,
                "compacted_days": 30,
                "daily_days": 365
            },
            "pressure_thresholds": {
                "raw_max_files": 100,
                "compacted_max_files": 50,
                "daily_max_files": 30
            },
            "compression": {
                "prune_empty_values": True,
                "truncate_large_strings": 1000,
                "content_hash_length": 8
            }
        }
        
        if self.config_path and Path(self.config_path).exists():
            with open(self.config_path, 'r') as f:
                config = yaml.safe_load(f)
                # Merge with defaults
                for key, value in default_config.items():
                    if key not in config:
                        config[key] = value
                    elif isinstance(value, dict):
                        for subkey, subvalue in value.items():
                            if subkey not in config[key]:
                                config[key][subkey] = subvalue
                return config
        
        return default_config
    
    def _ensure_directories(self):
        """Ensure all necessary directories exist"""
        directories = [
            self.root_path / "snapshots" / "raw",
            self.root_path / "snapshots" / "compacted", 
            self.root_path / "snapshots" / "daily",
            self.root_path / "decisions"
        ]
        
        for directory in directories:
            directory.mkdir(parents=True, exist_ok=True)
    
    def compress_snapshots(self, verbose: bool = False) -> Dict[str, Any]:
        """Main compression routine"""
        if verbose:
            print("🕰️ Starting Giant-in-the-Well compression...")
        
        start_time = time.time()
        report = {
            "timestamp": datetime.now().isoformat(),
            "processing_time_seconds": 0,
            "layers_processed": {},
            "migrations": {},
            "pressure_metrics": {},
            "next_actions": []
        }
        
        # Process raw → compacted
        raw_migrations = self._process_raw_to_compacted(verbose)
        report["migrations"]["raw_to_compacted"] = raw_migrations
        
        # Process compacted → daily
        compacted_migrations = self._process_compacted_to_daily(verbose)
        report["migrations"]["compacted_to_daily"] = compacted_migrations
        
        # Calculate pressure metrics
        pressure = self._calculate_pressure_metrics()
        report["pressure_metrics"] = pressure
        
        # Generate next actions
        next_actions = self._generate_next_actions(pressure)
        report["next_actions"] = next_actions
        
        # Update index
        index_data = self._generate_index(pressure)
        self._write_index(index_data)
        
        report["processing_time_seconds"] = round(time.time() - start_time, 2)
        
        if verbose:
            print(f"✅ Compression complete in {report['processing_time_seconds']}s")
            print(f"📊 Pressure levels: Raw={pressure['raw_pressure']:.1f}%, "
                  f"Compacted={pressure['compacted_pressure']:.1f}%")
        
        return report
    
    def _process_raw_to_compacted(self, verbose: bool = False) -> List[Dict[str, Any]]:
        """Process raw snapshots into compacted layer"""
        raw_dir = self.root_path / "snapshots" / "raw"
        compacted_dir = self.root_path / "snapshots" / "compacted" 
        migrations = []
        
        if not raw_dir.exists():
            return migrations
        
        raw_files = list(raw_dir.glob("*.yaml")) + list(raw_dir.glob("*.yml"))
        cutoff_time = datetime.now() - timedelta(days=self.config["retention"]["raw_days"])
        
        for raw_file in raw_files:
            # Check if file is old enough to migrate
            file_time = datetime.fromtimestamp(raw_file.stat().st_mtime)
            if file_time < cutoff_time:
                migration = self._migrate_file(raw_file, compacted_dir, "compacted", verbose)
                if migration:
                    migrations.append(migration)
        
        return migrations
    
    def _process_compacted_to_daily(self, verbose: bool = False) -> List[Dict[str, Any]]:
        """Process compacted snapshots into daily aggregates"""
        compacted_dir = self.root_path / "snapshots" / "compacted"
        daily_dir = self.root_path / "snapshots" / "daily"
        migrations = []
        
        if not compacted_dir.exists():
            return migrations
        
        compacted_files = list(compacted_dir.glob("*.yaml")) + list(compacted_dir.glob("*.yml"))
        cutoff_time = datetime.now() - timedelta(days=self.config["retention"]["compacted_days"])
        
        # Group files by day for aggregation
        daily_groups = {}
        for compacted_file in compacted_files:
            file_time = datetime.fromtimestamp(compacted_file.stat().st_mtime)
            if file_time < cutoff_time:
                day_key = file_time.strftime("%Y-%m-%d")
                if day_key not in daily_groups:
                    daily_groups[day_key] = []
                daily_groups[day_key].append(compacted_file)
        
        # Process each daily group
        for day_key, files in daily_groups.items():
            if len(files) > 1:  # Only aggregate if multiple files
                migration = self._aggregate_daily_files(files, daily_dir, day_key, verbose)
                if migration:
                    migrations.append(migration)
            elif len(files) == 1:
                # Single file migration
                migration = self._migrate_file(files[0], daily_dir, "daily", verbose)
                if migration:
                    migrations.append(migration)
        
        return migrations
    
    def _migrate_file(self, source_file: Path, target_dir: Path, layer: str, verbose: bool = False) -> Optional[Dict[str, Any]]:
        """Migrate a single file between layers"""
        try:
            # Load and compress content
            with open(source_file, 'r', encoding='utf-8') as f:
                content = yaml.safe_load(f)
            
            compressed_content = self._compress_content(content)
            content_hash = self._generate_content_hash(compressed_content)
            
            # Generate new filename with content hash segment
            hash_segment = content_hash[:self.config["compression"]["content_hash_length"]]
            timestamp = datetime.now().strftime("%Y-%m-%d")
            new_filename = f"{timestamp}-{hash_segment}-{source_file.stem}.yaml"
            target_file = target_dir / new_filename
            
            # Check for duplicates
            if target_file.exists():
                if verbose:
                    print(f"⚠️  Duplicate content detected, skipping: {source_file.name}")
                source_file.unlink()  # Remove source duplicate
                return {
                    "source": str(source_file),
                    "target": str(target_file), 
                    "action": "deduplicated",
                    "content_hash": content_hash
                }
            
            # Write compressed content
            with open(target_file, 'w', encoding='utf-8') as f:
                yaml.dump(compressed_content, f, default_flow_style=False, sort_keys=True)
            
            # Remove source file
            source_file.unlink()
            
            if verbose:
                print(f"📦 Migrated {source_file.name} → {layer}/{new_filename}")
            
            return {
                "source": str(source_file),
                "target": str(target_file),
                "action": "migrated",
                "layer": layer,
                "content_hash": content_hash,
                "compression_ratio": self._calculate_compression_ratio(source_file, target_file)
            }
            
        except Exception as e:
            if verbose:
                print(f"❌ Failed to migrate {source_file}: {e}")
            return None
    
    def _aggregate_daily_files(self, files: List[Path], target_dir: Path, day_key: str, verbose: bool = False) -> Optional[Dict[str, Any]]:
        """Aggregate multiple files into a single daily file"""
        try:
            aggregated_content = {
                "metadata": {
                    "aggregation_date": day_key,
                    "source_files": [f.name for f in files],
                    "aggregated_at": datetime.now().isoformat()
                },
                "snapshots": []
            }
            
            # Collect all content
            for file_path in files:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = yaml.safe_load(f)
                    compressed_content = self._compress_content(content)
                    aggregated_content["snapshots"].append({
                        "source_file": file_path.name,
                        "content": compressed_content
                    })
            
            # Generate filename
            content_hash = self._generate_content_hash(aggregated_content)
            hash_segment = content_hash[:self.config["compression"]["content_hash_length"]]
            target_filename = f"{day_key}-{hash_segment}-daily-aggregate.yaml"
            target_file = target_dir / target_filename
            
            # Write aggregated content
            with open(target_file, 'w', encoding='utf-8') as f:
                yaml.dump(aggregated_content, f, default_flow_style=False, sort_keys=True)
            
            # Remove source files
            for file_path in files:
                file_path.unlink()
            
            if verbose:
                print(f"📁 Aggregated {len(files)} files → daily/{target_filename}")
            
            return {
                "source_files": [str(f) for f in files],
                "target": str(target_file),
                "action": "aggregated",
                "layer": "daily",
                "content_hash": content_hash,
                "file_count": len(files)
            }
            
        except Exception as e:
            if verbose:
                print(f"❌ Failed to aggregate files for {day_key}: {e}")
            return None
    
    def _compress_content(self, content: Any) -> Any:
        """Apply compression rules to content"""
        if not self.config["compression"]["prune_empty_values"]:
            return content
        
        return self._prune_empty_values(content)
    
    def _prune_empty_values(self, obj: Any) -> Any:
        """Recursively remove empty/default values"""
        if isinstance(obj, dict):
            result = {}
            for key, value in obj.items():
                if value is not None and value != "" and value != [] and value != {}:
                    pruned_value = self._prune_empty_values(value)
                    if pruned_value is not None and pruned_value != "" and pruned_value != [] and pruned_value != {}:
                        result[key] = pruned_value
            return result
        elif isinstance(obj, list):
            return [self._prune_empty_values(item) for item in obj if item is not None and item != "" and item != []]
        elif isinstance(obj, str):
            # Truncate large strings
            max_length = self.config["compression"]["truncate_large_strings"]
            if len(obj) > max_length:
                return obj[:max_length] + "... [truncated]"
            return obj
        else:
            return obj
    
    def _generate_content_hash(self, content: Any) -> str:
        """Generate SHA-256 hash of content"""
        content_str = json.dumps(content, sort_keys=True, default=str)
        return hashlib.sha256(content_str.encode('utf-8')).hexdigest()
    
    def _calculate_compression_ratio(self, source_file: Path, target_file: Path) -> float:
        """Calculate compression ratio between source and target files"""
        try:
            source_size = source_file.stat().st_size if source_file.exists() else 0
            target_size = target_file.stat().st_size if target_file.exists() else 0
            
            if source_size == 0:
                return 0.0
            
            return round((1 - target_size / source_size) * 100, 2)
        except:
            return 0.0
    
    def _calculate_pressure_metrics(self) -> Dict[str, Any]:
        """Calculate pressure metrics for each layer"""
        metrics = {}
        
        for layer in ["raw", "compacted", "daily"]:
            layer_dir = self.root_path / "snapshots" / layer
            
            if layer_dir.exists():
                files = list(layer_dir.glob("*.yaml")) + list(layer_dir.glob("*.yml"))
                file_count = len(files)
                max_files = self.config["pressure_thresholds"][f"{layer}_max_files"]
                
                # Calculate total size
                total_size = sum(f.stat().st_size for f in files)
                
                # Calculate pressure as percentage of threshold
                pressure = (file_count / max_files) * 100 if max_files > 0 else 0
                
                metrics[f"{layer}_files"] = file_count
                metrics[f"{layer}_size_bytes"] = total_size
                metrics[f"{layer}_pressure"] = round(pressure, 1)
                metrics[f"{layer}_threshold"] = max_files
            else:
                metrics[f"{layer}_files"] = 0
                metrics[f"{layer}_size_bytes"] = 0
                metrics[f"{layer}_pressure"] = 0.0
                metrics[f"{layer}_threshold"] = self.config["pressure_thresholds"][f"{layer}_max_files"]
        
        return metrics
    
    def _generate_next_actions(self, pressure: Dict[str, Any]) -> List[str]:
        """Generate recommended next actions based on pressure"""
        actions = []
        
        if pressure.get("raw_pressure", 0) > 80:
            actions.append("High pressure in raw layer - consider more frequent compaction")
        
        if pressure.get("compacted_pressure", 0) > 80:
            actions.append("High pressure in compacted layer - review daily aggregation settings")
        
        if pressure.get("daily_pressure", 0) > 80:
            actions.append("High pressure in daily layer - implement weekly/monthly promotion")
        
        # Check for empty layers
        if pressure.get("raw_files", 0) == 0:
            actions.append("Raw layer empty - populate with test snapshots if testing")
        
        if not actions:
            actions.append("All layers operating within normal parameters")
        
        return actions
    
    def _generate_index(self, pressure: Dict[str, Any]) -> Dict[str, Any]:
        """Generate index.json content"""
        return {
            "generated_at": datetime.now().isoformat(),
            "version": "1.0.0",
            "layer_counts": {
                "raw": pressure.get("raw_files", 0),
                "compacted": pressure.get("compacted_files", 0), 
                "daily": pressure.get("daily_files", 0)
            },
            "pressure_metrics": {
                "raw": pressure.get("raw_pressure", 0),
                "compacted": pressure.get("compacted_pressure", 0),
                "daily": pressure.get("daily_pressure", 0)
            },
            "total_size_bytes": (
                pressure.get("raw_size_bytes", 0) +
                pressure.get("compacted_size_bytes", 0) +
                pressure.get("daily_size_bytes", 0)
            ),
            "next_actions": self._generate_next_actions(pressure),
            "configuration": {
                "retention_days": {
                    "raw": self.config["retention"]["raw_days"],
                    "compacted": self.config["retention"]["compacted_days"],
                    "daily": self.config["retention"]["daily_days"]
                },
                "pressure_thresholds": self.config["pressure_thresholds"]
            }
        }
    
    def _write_index(self, index_data: Dict[str, Any]):
        """Write index.json file"""
        index_path = self.root_path / "index.json"
        with open(index_path, 'w', encoding='utf-8') as f:
            json.dump(index_data, f, indent=2, sort_keys=True)


def main():
    """Main CLI entry point"""
    parser = argparse.ArgumentParser(
        description="DevTimeTravel Compression - Giant-in-the-Well snapshot compaction system"
    )
    
    parser.add_argument(
        "--root",
        default=".devtimetravel",
        help="Root path for DevTimeTravel directory (default: .devtimetravel)"
    )
    
    parser.add_argument(
        "--config",
        help="Path to configuration YAML file (optional)"
    )
    
    parser.add_argument(
        "--report",
        default="devtimetravel_compress_report.json",
        help="Output path for compression report (default: devtimetravel_compress_report.json)"
    )
    
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Enable verbose output"
    )
    
    args = parser.parse_args()
    
    try:
        # Initialize compressor
        compressor = DevTimeTravelCompressor(args.root, args.config)
        
        # Run compression
        report = compressor.compress_snapshots(args.verbose)
        
        # Write report
        with open(args.report, 'w', encoding='utf-8') as f:
            json.dump(report, f, indent=2, sort_keys=True)
        
        if args.verbose:
            print(f"📄 Report written to: {args.report}")
        
        # Exit with appropriate code
        if any("High pressure" in action for action in report["next_actions"]):
            sys.exit(2)  # Warning exit code for high pressure
        else:
            sys.exit(0)  # Success
        
    except Exception as e:
        print(f"❌ Compression failed: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()