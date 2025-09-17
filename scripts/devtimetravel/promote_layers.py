#!/usr/bin/env python3
"""
DevTimeTravel Layer Promotion Engine
Promotes daily snapshots to weekly aggregates and weekly to monthly aggregates.
"""

import os
import sys
import json
import yaml
import hashlib
import argparse
from pathlib import Path
from datetime import datetime, timedelta
from typing import Dict, List, Any, Optional
import re


class LayerPromotionEngine:
    """Core engine for promoting DevTimeTravel layers"""
    
    def __init__(self, root_path: str = ".devtimetravel", verbose: bool = False):
        self.root_path = Path(root_path)
        self.verbose = verbose
        self.daily_path = self.root_path / "daily"
        self.weekly_path = self.root_path / "weekly" 
        self.monthly_path = self.root_path / "monthly"
        
        # Ensure directories exist
        for path in [self.daily_path, self.weekly_path, self.monthly_path]:
            path.mkdir(parents=True, exist_ok=True)
    
    def log(self, message: str, level: str = "INFO"):
        """Log messages with timestamp"""
        if level == "VERBOSE" and not self.verbose:
            return
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] [{level}] {message}")
    
    def get_iso_week_dates(self, year: int, week: int) -> tuple:
        """Get start and end dates for an ISO week"""
        # ISO week starts on Monday (weekday 0)
        jan4 = datetime(year, 1, 4)
        week_start = jan4 - timedelta(days=jan4.weekday()) + timedelta(weeks=week-1)
        week_end = week_start + timedelta(days=6)
        return week_start.date(), week_end.date()
    
    def parse_daily_filename(self, filename: str) -> Optional[datetime]:
        """Parse daily snapshot filename to extract date"""
        # Expected format: YYYY-MM-DD-*.yaml
        match = re.match(r'^(\d{4})-(\d{2})-(\d{2})-.*\.ya?ml$', filename)
        if match:
            year, month, day = match.groups()
            return datetime(int(year), int(month), int(day))
        return None
    
    def parse_weekly_filename(self, filename: str) -> Optional[tuple]:
        """Parse weekly snapshot filename to extract year and week"""
        # Expected format: YYYY-Www.yaml
        match = re.match(r'^(\d{4})-W(\d{2})\.ya?ml$', filename)
        if match:
            year, week = match.groups()
            return int(year), int(week)
        return None
    
    def calculate_hash_root(self, child_hashes: List[str]) -> str:
        """Calculate SHA-256 hash of concatenated child file hashes"""
        concatenated = ''.join(sorted(child_hashes))
        return hashlib.sha256(concatenated.encode()).hexdigest()
    
    def load_yaml_file(self, filepath: Path) -> Optional[Dict[str, Any]]:
        """Load YAML file and return content"""
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
    
    def get_file_hash(self, filepath: Path) -> str:
        """Get SHA-256 hash of file content"""
        try:
            with open(filepath, 'rb') as f:
                return hashlib.sha256(f.read()).hexdigest()
        except Exception as e:
            self.log(f"Error hashing {filepath}: {e}", "ERROR")
            return ""
    
    def group_daily_by_week(self) -> Dict[tuple, List[Path]]:
        """Group daily files by ISO week (year, week_number)"""
        weekly_groups = {}
        
        for daily_file in self.daily_path.glob("*.yaml"):
            date = self.parse_daily_filename(daily_file.name)
            if date:
                # Get ISO week
                iso_year, iso_week, _ = date.isocalendar()
                key = (iso_year, iso_week)
                
                if key not in weekly_groups:
                    weekly_groups[key] = []
                weekly_groups[key].append(daily_file)
        
        return weekly_groups
    
    def group_weekly_by_month(self) -> Dict[tuple, List[Path]]:
        """Group weekly files by calendar month (year, month)"""
        monthly_groups = {}
        
        for weekly_file in self.weekly_path.glob("*.yaml"):
            parsed = self.parse_weekly_filename(weekly_file.name)
            if parsed:
                year, week = parsed
                week_start, _ = self.get_iso_week_dates(year, week)
                key = (week_start.year, week_start.month)
                
                if key not in monthly_groups:
                    monthly_groups[key] = []
                monthly_groups[key].append(weekly_file)
        
        return monthly_groups
    
    def promote_daily_to_weekly(self, dry_run: bool = False) -> int:
        """Promote daily snapshots to weekly aggregates"""
        self.log("Starting daily -> weekly promotion")
        promotions = 0
        
        weekly_groups = self.group_daily_by_week()
        
        for (iso_year, iso_week), daily_files in weekly_groups.items():
            weekly_filename = f"{iso_year:04d}-W{iso_week:02d}.yaml"
            weekly_path = self.weekly_path / weekly_filename
            
            # Skip if weekly aggregate already exists (idempotent)
            if weekly_path.exists():
                self.log(f"Weekly aggregate {weekly_filename} already exists, skipping", "VERBOSE")
                continue
            
            self.log(f"Creating weekly aggregate {weekly_filename} from {len(daily_files)} daily files")
            
            if dry_run:
                self.log(f"DRY RUN: Would create {weekly_filename}", "INFO")
                promotions += 1
                continue
            
            # Get week date range
            week_start, week_end = self.get_iso_week_dates(iso_year, iso_week)
            
            # Collect data from daily files
            days = []
            child_hashes = []
            total_metrics = {}
            decision_summaries = []
            
            for daily_file in sorted(daily_files):
                daily_data = self.load_yaml_file(daily_file)
                if daily_data:
                    # Add day entry
                    days.append({
                        'date': self.parse_daily_filename(daily_file.name).strftime('%Y-%m-%d'),
                        'file': daily_file.name,
                        'entries': daily_data.get('entries', 0)
                    })
                    
                    # Collect metrics
                    daily_metrics = daily_data.get('metrics', {})
                    for key, value in daily_metrics.items():
                        if isinstance(value, (int, float)):
                            total_metrics[key] = total_metrics.get(key, 0) + value
                        elif key == 'counts':
                            # Handle nested counts
                            for count_key, count_value in value.items():
                                count_path = f"counts.{count_key}"
                                total_metrics[count_path] = total_metrics.get(count_path, 0) + count_value
                    
                    # Collect decision summaries
                    if 'decision_index' in daily_data:
                        decision_summaries.append(daily_data['decision_index'])
                    
                    # Get file hash
                    child_hashes.append(self.get_file_hash(daily_file))
            
            # Create weekly aggregate
            weekly_data = {
                'week_start': week_start.strftime('%Y-%m-%d'),
                'week_end': week_end.strftime('%Y-%m-%d'),
                'iso_year': iso_year,
                'iso_week': iso_week,
                'days': days,
                'metrics': total_metrics,
                'decision_index': {
                    'summary': f"Weekly aggregate of {len(decision_summaries)} daily decisions",
                    'daily_summaries': decision_summaries
                },
                'child_hashes': child_hashes,
                'hash_root': self.calculate_hash_root(child_hashes),
                'created_at': datetime.now().isoformat(),
                'promotion_type': 'daily_to_weekly'
            }
            
            if self.save_yaml_file(weekly_path, weekly_data):
                self.log(f"Successfully created weekly aggregate: {weekly_filename}")
                promotions += 1
            else:
                self.log(f"Failed to create weekly aggregate: {weekly_filename}", "ERROR")
        
        self.log(f"Completed daily -> weekly promotion: {promotions} promotions")
        return promotions
    
    def promote_weekly_to_monthly(self, dry_run: bool = False) -> int:
        """Promote weekly snapshots to monthly aggregates"""
        self.log("Starting weekly -> monthly promotion")
        promotions = 0
        
        monthly_groups = self.group_weekly_by_month()
        
        for (year, month), weekly_files in monthly_groups.items():
            monthly_filename = f"{year:04d}-{month:02d}.yaml"
            monthly_path = self.monthly_path / monthly_filename
            
            # Skip if monthly aggregate already exists (idempotent)
            if monthly_path.exists():
                self.log(f"Monthly aggregate {monthly_filename} already exists, skipping", "VERBOSE")
                continue
            
            self.log(f"Creating monthly aggregate {monthly_filename} from {len(weekly_files)} weekly files")
            
            if dry_run:
                self.log(f"DRY RUN: Would create {monthly_filename}", "INFO")
                promotions += 1
                continue
            
            # Collect data from weekly files
            weeks = []
            child_hashes = []
            total_metrics = {}
            decision_summaries = []
            
            for weekly_file in sorted(weekly_files):
                weekly_data = self.load_yaml_file(weekly_file)
                if weekly_data:
                    # Add week entry
                    weeks.append({
                        'week_start': weekly_data.get('week_start'),
                        'week_end': weekly_data.get('week_end'),
                        'file': weekly_file.name,
                        'days_count': len(weekly_data.get('days', []))
                    })
                    
                    # Collect metrics
                    weekly_metrics = weekly_data.get('metrics', {})
                    for key, value in weekly_metrics.items():
                        if isinstance(value, (int, float)):
                            total_metrics[key] = total_metrics.get(key, 0) + value
                    
                    # Collect decision summaries
                    if 'decision_index' in weekly_data:
                        decision_summaries.append(weekly_data['decision_index'])
                    
                    # Get file hash
                    child_hashes.append(self.get_file_hash(weekly_file))
            
            # Calculate trend deltas (vs prior month if exists)
            trend_deltas = {}
            prior_month_file = self.monthly_path / f"{year:04d}-{month-1:02d}.yaml"
            if month == 1:
                prior_month_file = self.monthly_path / f"{year-1:04d}-12.yaml"
            
            if prior_month_file.exists():
                prior_data = self.load_yaml_file(prior_month_file)
                if prior_data and 'metrics' in prior_data:
                    for key, current_value in total_metrics.items():
                        prior_value = prior_data['metrics'].get(key, 0)
                        if isinstance(current_value, (int, float)) and isinstance(prior_value, (int, float)):
                            delta = current_value - prior_value
                            trend_deltas[key] = {
                                'current': current_value,
                                'prior': prior_value,
                                'delta': delta,
                                'percent_change': (delta / prior_value * 100) if prior_value != 0 else 0
                            }
            
            # Create monthly aggregate
            monthly_data = {
                'year': year,
                'month': month,
                'month_start': f"{year:04d}-{month:02d}-01",
                'weeks': weeks,
                'metrics': total_metrics,
                'trend_deltas': trend_deltas,
                'decision_index': {
                    'summary': f"Monthly aggregate of {len(decision_summaries)} weekly decisions",
                    'weekly_summaries': decision_summaries
                },
                'child_hashes': child_hashes,
                'hash_root': self.calculate_hash_root(child_hashes),
                'created_at': datetime.now().isoformat(),
                'promotion_type': 'weekly_to_monthly'
            }
            
            if self.save_yaml_file(monthly_path, monthly_data):
                self.log(f"Successfully created monthly aggregate: {monthly_filename}")
                promotions += 1
            else:
                self.log(f"Failed to create monthly aggregate: {monthly_filename}", "ERROR")
        
        self.log(f"Completed weekly -> monthly promotion: {promotions} promotions")
        return promotions


def main():
    parser = argparse.ArgumentParser(description='DevTimeTravel Layer Promotion Engine')
    parser.add_argument('--root', default='.devtimetravel', 
                       help='DevTimeTravel root directory (default: .devtimetravel)')
    parser.add_argument('--promote', choices=['daily->weekly', 'weekly->monthly'], 
                       help='Specific promotion to run')
    parser.add_argument('--all', action='store_true', 
                       help='Run all promotions (daily->weekly then weekly->monthly)')
    parser.add_argument('--dry-run', action='store_true', 
                       help='Preview planned promotions without executing')
    parser.add_argument('--verbose', action='store_true', 
                       help='Enable verbose logging')
    
    args = parser.parse_args()
    
    # Validation
    if not args.promote and not args.all:
        parser.error("Must specify either --promote or --all")
    
    engine = LayerPromotionEngine(args.root, args.verbose)
    
    total_promotions = 0
    
    if args.all or args.promote == 'daily->weekly':
        total_promotions += engine.promote_daily_to_weekly(args.dry_run)
    
    if args.all or args.promote == 'weekly->monthly':
        total_promotions += engine.promote_weekly_to_monthly(args.dry_run)
    
    print(f"\nPromotion complete: {total_promotions} total promotions")
    if args.dry_run:
        print("DRY RUN: No files were actually created")


if __name__ == '__main__':
    main()
DevTimeTravel Layer Promotion System (Skeleton)

Future implementation for promoting snapshots between layers:
- daily → weekly aggregation
- weekly → monthly aggregation  
- monthly → magma/tombstone archival

This is a skeleton implementation that provides argument parsing
and guidance for future development.
"""

import argparse
import sys
from pathlib import Path


def main():
    """Main CLI entry point for layer promotion"""
    parser = argparse.ArgumentParser(
        description="DevTimeTravel Layer Promotion - Promote snapshots between layers"
    )
    
    parser.add_argument(
        "--promote",
        choices=["daily-to-weekly", "weekly-to-monthly", "monthly-to-magma"],
        help="Specify promotion direction"
    )
    
    parser.add_argument(
        "--root",
        default=".devtimetravel",
        help="Root path for DevTimeTravel directory (default: .devtimetravel)"
    )
    
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would be promoted without making changes"
    )
    
    parser.add_argument(
        "--verbose",
        action="store_true", 
        help="Enable verbose output"
    )
    
    args = parser.parse_args()
    
    if not args.promote:
        parser.print_help()
        print("\n🚧 DevTimeTravel Layer Promotion System - Currently Under Construction")
        print("\nThis script is a skeleton for future implementation of layer promotion.")
        print("The following promotion paths are planned:")
        print("  • daily-to-weekly: Aggregate daily snapshots into weekly summaries")
        print("  • weekly-to-monthly: Combine weekly data into monthly archives") 
        print("  • monthly-to-magma: Long-term archival to magma/tombstone layer")
        return 1
    
    print(f"🚧 TODO: Implement {args.promote} promotion")
    print(f"📁 Root directory: {args.root}")
    print(f"🔍 Dry run mode: {args.dry_run}")
    print(f"📝 Verbose mode: {args.verbose}")
    
    print("\n📋 Implementation Guidance for Future Development:")
    print("   1. Load layer promotion configuration")
    print("   2. Scan source layer for eligible snapshots") 
    print("   3. Group snapshots by time period (week/month)")
    print("   4. Apply semantic similarity analysis for intelligent grouping")
    print("   5. Generate aggregated content with metadata preservation")
    print("   6. Create content-addressed filenames with hash segments")
    print("   7. Update index.json with new layer counts")
    print("   8. Generate promotion report")
    print("   9. Integrate with TLDL weekly/monthly archive system")
    
    if args.promote == "daily-to-weekly":
        print("\n🗓️  Daily-to-Weekly Promotion TODO:")
        print("   • Group daily snapshots by calendar week")
        print("   • Preserve decision context and learning insights")
        print("   • Generate weekly development narrative summaries")
        print("   • Link to corresponding TLDL weekly archives")
        
    elif args.promote == "weekly-to-monthly":
        print("\n📅 Weekly-to-Monthly Promotion TODO:")
        print("   • Combine weekly aggregates into monthly themes")
        print("   • Extract patterns and architectural evolution")
        print("   • Create monthly development milestone reports")
        print("   • Integrate with TLDL monthly archive generation")
        
    elif args.promote == "monthly-to-magma":
        print("\n🌋 Monthly-to-Magma Promotion TODO:")
        print("   • Implement long-term archival strategy")
        print("   • Create tombstone references for deep storage")
        print("   • Preserve essential metadata for future reference")
        print("   • Design melt/restore capabilities for emergency access")
    
    print(f"\n⚠️  Currently returning TODO status (exit code 42)")
    print("   This indicates the feature is planned but not yet implemented.")
    
    return 42  # Special exit code indicating "TODO - not implemented yet"


if __name__ == "__main__":
    sys.exit(main())