#!/usr/bin/env python3
"""
DevTimeTravel Tombstone Melt & Integrity Verification
Manages melting of old aggregates and integrity verification with tombstone creation.
"""

import os
import sys
import json
import yaml
import hashlib
import argparse
from pathlib import Path
from datetime import datetime, timedelta
from typing import Dict, List, Any, Optional, Tuple


class TombstoneMeltVerifier:
    """Manages melting and verification of DevTimeTravel aggregates"""
    
    def __init__(self, root_path: str = ".devtimetravel", verbose: bool = False):
        self.root_path = Path(root_path)
        self.verbose = verbose
        self.weekly_path = self.root_path / "weekly"
        self.monthly_path = self.root_path / "monthly"
        self.tombstones_path = self.root_path / "tombstones"
        
        # Ensure directories exist
        for path in [self.weekly_path, self.monthly_path, self.tombstones_path]:
            path.mkdir(parents=True, exist_ok=True)
    
    def log(self, message: str, level: str = "INFO"):
        """Log messages with timestamp"""
        if level == "VERBOSE" and not self.verbose:
            return
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] [{level}] {message}")
    
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
    
    def save_json_file(self, filepath: Path, data: Dict[str, Any]) -> bool:
        """Save data to JSON file"""
        try:
            with open(filepath, 'w', encoding='utf-8') as f:
                json.dump(data, f, indent=2, sort_keys=True)
            return True
        except Exception as e:
            self.log(f"Error saving {filepath}: {e}", "ERROR")
            return False
    
    def get_file_hash(self, filepath: Path) -> str:
        """Get SHA-256 hash of file content"""
        try:
            with open(filepath, 'rb') as f:
                return hashlib.sha256(f.read()).hexdigest()
        except Exception as e:
            self.log(f"Error hashing {filepath}: {e}", "ERROR")
            return ""
    
    def calculate_hash_root(self, child_hashes: List[str]) -> str:
        """Calculate SHA-256 hash of concatenated child file hashes"""
        concatenated = ''.join(sorted(child_hashes))
        return hashlib.sha256(concatenated.encode()).hexdigest()
    
    def parse_monthly_filename(self, filename: str) -> Optional[Tuple[int, int]]:
        """Parse monthly aggregate filename to extract year and month"""
        import re
        match = re.match(r'^(\d{4})-(\d{2})\.ya?ml$', filename)
        if match:
            year, month = match.groups()
            return int(year), int(month)
        return None
    
    def is_monthly_aggregate_old(self, monthly_file: Path, days_threshold: int = 90) -> bool:
        """Check if monthly aggregate is older than threshold"""
        parsed = self.parse_monthly_filename(monthly_file.name)
        if not parsed:
            return False
        
        year, month = parsed
        # Use last day of the month for comparison
        if month == 12:
            next_month = datetime(year + 1, 1, 1)
        else:
            next_month = datetime(year, month + 1, 1)
        
        month_end = next_month - timedelta(days=1)
        age = datetime.now().date() - month_end.date()
        
        return age.days > days_threshold
    
    def verify_monthly_integrity(self, monthly_file: Path) -> Tuple[bool, str]:
        """Verify integrity of monthly aggregate and its child weeklies"""
        monthly_data = self.load_yaml_file(monthly_file)
        if not monthly_data:
            return False, "Could not load monthly file"
        
        # Check if monthly has required fields
        if 'child_hashes' not in monthly_data or 'hash_root' not in monthly_data:
            return False, "Monthly file missing hash integrity fields"
        
        # Verify hash_root calculation
        recorded_hash_root = monthly_data['hash_root']
        calculated_hash_root = self.calculate_hash_root(monthly_data['child_hashes'])
        
        if recorded_hash_root != calculated_hash_root:
            return False, f"Hash root mismatch: recorded={recorded_hash_root[:8]}..., calculated={calculated_hash_root[:8]}..."
        
        # Check if all weekly files exist and have decision_index
        missing_weeklies = []
        weeklies_without_decisions = []
        hash_mismatches = []
        
        weeks = monthly_data.get('weeks', [])
        child_hashes = monthly_data.get('child_hashes', [])
        
        for i, week_info in enumerate(weeks):
            week_file = self.weekly_path / week_info['file']
            
            if not week_file.exists():
                missing_weeklies.append(week_info['file'])
                continue
            
            # Check decision_index
            weekly_data = self.load_yaml_file(week_file)
            if not weekly_data or 'decision_index' not in weekly_data:
                weeklies_without_decisions.append(week_info['file'])
                continue
            
            # Verify hash if available
            if i < len(child_hashes):
                expected_hash = child_hashes[i]
                actual_hash = self.get_file_hash(week_file)
                if actual_hash and actual_hash != expected_hash:
                    hash_mismatches.append(week_info['file'])
        
        # Report issues
        issues = []
        if missing_weeklies:
            issues.append(f"Missing weekly files: {', '.join(missing_weeklies)}")
        if weeklies_without_decisions:
            issues.append(f"Weekly files without decision_index: {', '.join(weeklies_without_decisions)}")
        if hash_mismatches:
            issues.append(f"Hash mismatches: {', '.join(hash_mismatches)}")
        
        if issues:
            return False, "; ".join(issues)
        
        return True, "All integrity checks passed"
    
    def create_tombstone(self, melted_files: List[Path], monthly_file: Path) -> bool:
        """Create tombstone record for melted weekly files"""
        timestamp = datetime.now().isoformat()
        tombstone_id = f"tombstone_{timestamp.replace(':', '').replace('-', '').split('.')[0]}"
        tombstone_file = self.tombstones_path / f"{tombstone_id}.json"
        
        # Gather information about melted files
        melted_info = []
        for weekly_file in melted_files:
            weekly_data = self.load_yaml_file(weekly_file)
            file_info = {
                "filename": weekly_file.name,
                "path": str(weekly_file.relative_to(self.root_path)),
                "hash": self.get_file_hash(weekly_file),
                "size_bytes": weekly_file.stat().st_size,
                "modified_time": weekly_file.stat().st_mtime,
                "metadata": {
                    "week_start": weekly_data.get('week_start') if weekly_data else None,
                    "week_end": weekly_data.get('week_end') if weekly_data else None,
                    "days_count": len(weekly_data.get('days', [])) if weekly_data else 0
                }
            }
            melted_info.append(file_info)
        
        # Create tombstone record
        tombstone_data = {
            "tombstone_id": tombstone_id,
            "created_at": timestamp,
            "melt_reason": "Monthly aggregate retention policy",
            "monthly_file": {
                "filename": monthly_file.name,
                "path": str(monthly_file.relative_to(self.root_path)),
                "hash": self.get_file_hash(monthly_file)
            },
            "melted_files": melted_info,
            "melt_statistics": {
                "files_melted": len(melted_files),
                "total_size_bytes": sum(info["size_bytes"] for info in melted_info),
                "retention_threshold_days": 90
            },
            "recovery_info": {
                "can_recover": True,
                "recovery_method": "Restore from monthly aggregate child_hashes and backup if available",
                "integrity_verified": True
            }
        }
        
        return self.save_json_file(tombstone_file, tombstone_data)
    
    def scan_melt_candidates(self, days_threshold: int = 90) -> List[Dict[str, Any]]:
        """Scan for monthly aggregates eligible for melting"""
        self.log(f"Scanning for monthly aggregates older than {days_threshold} days")
        
        candidates = []
        
        for monthly_file in self.monthly_path.glob("*.yaml"):
            if self.is_monthly_aggregate_old(monthly_file, days_threshold):
                # Verify integrity
                is_valid, reason = self.verify_monthly_integrity(monthly_file)
                
                candidate = {
                    "monthly_file": monthly_file,
                    "filename": monthly_file.name,
                    "is_eligible": is_valid,
                    "verification_result": reason,
                    "age_days": self.calculate_age_days(monthly_file)
                }
                
                if is_valid:
                    # Find associated weekly files
                    monthly_data = self.load_yaml_file(monthly_file)
                    weekly_files = []
                    if monthly_data and 'weeks' in monthly_data:
                        for week_info in monthly_data['weeks']:
                            week_file = self.weekly_path / week_info['file']
                            if week_file.exists():
                                weekly_files.append(week_file)
                    candidate["weekly_files"] = weekly_files
                
                candidates.append(candidate)
        
        return candidates
    
    def calculate_age_days(self, monthly_file: Path) -> int:
        """Calculate age of monthly file in days"""
        parsed = self.parse_monthly_filename(monthly_file.name)
        if not parsed:
            return 0
        
        year, month = parsed
        if month == 12:
            next_month = datetime(year + 1, 1, 1)
        else:
            next_month = datetime(year, month + 1, 1)
        
        month_end = next_month - timedelta(days=1)
        age = datetime.now().date() - month_end.date()
        return age.days
    
    def melt_aggregates(self, candidates: List[Dict[str, Any]], dry_run: bool = False) -> Dict[str, Any]:
        """Melt eligible aggregates and create tombstones"""
        self.log("Starting melt operation")
        
        stats = {
            "candidates_processed": 0,
            "aggregates_melted": 0,
            "weekly_files_removed": 0,
            "tombstones_created": 0,
            "errors": []
        }
        
        eligible_candidates = [c for c in candidates if c["is_eligible"]]
        self.log(f"Found {len(eligible_candidates)} eligible candidates for melting")
        
        for candidate in eligible_candidates:
            stats["candidates_processed"] += 1
            monthly_file = candidate["monthly_file"]
            weekly_files = candidate.get("weekly_files", [])
            
            self.log(f"Processing {monthly_file.name} with {len(weekly_files)} weekly files")
            
            if dry_run:
                self.log(f"DRY RUN: Would melt {len(weekly_files)} weekly files for {monthly_file.name}")
                stats["aggregates_melted"] += 1
                stats["weekly_files_removed"] += len(weekly_files)
                continue
            
            try:
                # Create tombstone first
                if self.create_tombstone(weekly_files, monthly_file):
                    stats["tombstones_created"] += 1
                    self.log(f"Created tombstone for {monthly_file.name}")
                    
                    # Remove weekly files
                    removed_count = 0
                    for weekly_file in weekly_files:
                        try:
                            weekly_file.unlink()
                            removed_count += 1
                            self.log(f"Removed weekly file: {weekly_file.name}", "VERBOSE")
                        except Exception as e:
                            stats["errors"].append(f"Failed to remove {weekly_file.name}: {e}")
                    
                    stats["weekly_files_removed"] += removed_count
                    stats["aggregates_melted"] += 1
                    self.log(f"Melted {monthly_file.name}: removed {removed_count} weekly files")
                    
                else:
                    stats["errors"].append(f"Failed to create tombstone for {monthly_file.name}")
                    
            except Exception as e:
                stats["errors"].append(f"Error melting {monthly_file.name}: {e}")
        
        return stats
    
    def verify_integrity(self) -> Dict[str, Any]:
        """Verify integrity of all aggregates"""
        self.log("Starting integrity verification")
        
        stats = {
            "monthly_files_checked": 0,
            "monthly_files_valid": 0,
            "monthly_files_invalid": 0,
            "validation_errors": []
        }
        
        for monthly_file in self.monthly_path.glob("*.yaml"):
            stats["monthly_files_checked"] += 1
            
            is_valid, reason = self.verify_monthly_integrity(monthly_file)
            
            if is_valid:
                stats["monthly_files_valid"] += 1
                self.log(f"✅ {monthly_file.name}: {reason}", "VERBOSE")
            else:
                stats["monthly_files_invalid"] += 1
                stats["validation_errors"].append({
                    "file": monthly_file.name,
                    "error": reason
                })
                self.log(f"❌ {monthly_file.name}: {reason}")
        
        return stats


def main():
    parser = argparse.ArgumentParser(description='DevTimeTravel Tombstone Melt & Integrity Verification')
    parser.add_argument('--root', default='.devtimetravel',
                       help='DevTimeTravel root directory (default: .devtimetravel)')
    parser.add_argument('--scan', action='store_true',
                       help='Scan for melt candidates and show report')
    parser.add_argument('--melt', action='store_true',
                       help='Perform melt operation on eligible aggregates')
    parser.add_argument('--verify', action='store_true',
                       help='Verify integrity of all aggregates')
    parser.add_argument('--days', type=int, default=90,
                       help='Minimum age in days for melt eligibility (default: 90)')
    parser.add_argument('--dry-run', action='store_true',
                       help='Preview melt operations without executing')
    parser.add_argument('--verbose', action='store_true',
                       help='Enable verbose logging')
    
    args = parser.parse_args()
    
    # Validation
    if not any([args.scan, args.melt, args.verify]):
        parser.error("Must specify at least one mode: --scan, --melt, or --verify")
    
    verifier = TombstoneMeltVerifier(args.root, args.verbose)
    
    # Run operations
    if args.scan or args.melt:
        candidates = verifier.scan_melt_candidates(args.days)
        
        if args.scan:
            print(f"\n📋 Melt Candidates Report")
            print(f"   Threshold: {args.days} days")
            print(f"   Total candidates: {len(candidates)}")
            
            eligible = [c for c in candidates if c["is_eligible"]]
            print(f"   Eligible for melting: {len(eligible)}")
            
            if eligible:
                print(f"\n✅ Eligible Aggregates:")
                for candidate in eligible:
                    weekly_count = len(candidate.get("weekly_files", []))
                    print(f"     {candidate['filename']} (age: {candidate['age_days']} days, {weekly_count} weekly files)")
            
            ineligible = [c for c in candidates if not c["is_eligible"]]
            if ineligible:
                print(f"\n❌ Ineligible Aggregates:")
                for candidate in ineligible:
                    print(f"     {candidate['filename']}: {candidate['verification_result']}")
        
        if args.melt:
            melt_stats = verifier.melt_aggregates(candidates, args.dry_run)
            print(f"\n🔥 Melt Operation Results")
            print(f"   Aggregates melted: {melt_stats['aggregates_melted']}")
            print(f"   Weekly files removed: {melt_stats['weekly_files_removed']}")
            print(f"   Tombstones created: {melt_stats['tombstones_created']}")
            
            if melt_stats['errors']:
                print(f"   Errors: {len(melt_stats['errors'])}")
                for error in melt_stats['errors'][:5]:  # Show first 5 errors
                    print(f"     - {error}")
            
            if args.dry_run:
                print("   DRY RUN: No files were actually removed")
    
    if args.verify:
        verify_stats = verifier.verify_integrity()
        print(f"\n🔍 Integrity Verification Results")
        print(f"   Monthly files checked: {verify_stats['monthly_files_checked']}")
        print(f"   Valid: {verify_stats['monthly_files_valid']}")
        print(f"   Invalid: {verify_stats['monthly_files_invalid']}")
        
        if verify_stats['validation_errors']:
            print(f"\n❌ Validation Errors:")
            for error in verify_stats['validation_errors']:
                print(f"     {error['file']}: {error['error']}")
    
    print("\n✅ Operations complete")


if __name__ == '__main__':
    main()