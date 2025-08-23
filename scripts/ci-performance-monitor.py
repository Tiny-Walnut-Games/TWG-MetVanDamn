#!/usr/bin/env python3
"""
🚀 CI Performance Monitor

Tracks and reports CI/CD pipeline performance metrics to support the 
CID Schoolhouse optimization roadmap monitoring (Phase 2).

Usage:
    python scripts/ci-performance-monitor.py --workflow ci --run-id 12345
    python scripts/ci-performance-monitor.py --report --period weekly
"""

import argparse
import json
import time
from datetime import datetime, timedelta, timezone
from pathlib import Path
import sys

class CIPerformanceMonitor:
    """Monitor and track CI/CD pipeline performance metrics"""
    
    def __init__(self, metrics_dir="out/ci-metrics"):
        self.metrics_dir = Path(metrics_dir)
        self.metrics_dir.mkdir(parents=True, exist_ok=True)
        self.metrics_file = self.metrics_dir / "performance-metrics.json"
        
    def load_metrics(self):
        """Load existing metrics from file"""
        if self.metrics_file.exists():
            try:
                with open(self.metrics_file, 'r') as f:
                    return json.load(f)
            except (json.JSONDecodeError, IOError):
                print("⚠️ Warning: Could not load existing metrics, starting fresh")
        
        return {
            "runs": [],
            "summary": {
                "total_runs": 0,
                "average_duration": 0,
                "success_rate": 0,
                "last_updated": None
            }
        }
    
    def save_metrics(self, metrics):
        """Save metrics to file"""
        metrics["summary"]["last_updated"] = datetime.now(timezone.utc).isoformat()
        try:
            with open(self.metrics_file, 'w') as f:
                json.dump(metrics, f, indent=2)
        except IOError as e:
            print(f"❌ Error saving metrics: {e}")
    
    def record_run(self, workflow_name, run_id, duration=None, status="unknown", job_details=None):
        """Record a CI run in metrics"""
        metrics = self.load_metrics()
        
        run_data = {
            "workflow": workflow_name,
            "run_id": run_id,
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "duration_seconds": duration,
            "status": status,
            "jobs": job_details or {}
        }
        
        metrics["runs"].append(run_data)
        
        # Update summary
        metrics["summary"]["total_runs"] = len(metrics["runs"])
        
        # Calculate success rate
        successful_runs = sum(1 for run in metrics["runs"] if run["status"] == "success")
        metrics["summary"]["success_rate"] = (successful_runs / len(metrics["runs"])) * 100 if metrics["runs"] else 0
        
        # Calculate average duration (for runs with duration data)
        duration_runs = [run for run in metrics["runs"] if run.get("duration_seconds")]
        if duration_runs:
            avg_duration = sum(run["duration_seconds"] for run in duration_runs) / len(duration_runs)
            metrics["summary"]["average_duration"] = round(avg_duration, 2)
        
        self.save_metrics(metrics)
        print(f"📊 Recorded run {run_id} for {workflow_name}: {status} ({duration}s)")
        
    def generate_report(self, period="weekly"):
        """Generate performance report"""
        metrics = self.load_metrics()
        
        if not metrics["runs"]:
            print("📊 No CI runs recorded yet")
            return
        
        # Filter runs by period
        now = datetime.now(timezone.utc)
        if period == "daily":
            cutoff = now - timedelta(days=1)
        elif period == "weekly":
            cutoff = now - timedelta(weeks=1)
        elif period == "monthly":
            cutoff = now - timedelta(days=30)
        else:
            cutoff = None  # All time
            
        if cutoff:
            def parse_timestamp(timestamp_str):
                """Parse timestamp handling both old (with Z) and new (timezone-aware) formats"""
                try:
                    # Try parsing as-is first (new format)
                    dt = datetime.fromisoformat(timestamp_str)
                    # If it's naive, assume UTC
                    if dt.tzinfo is None:
                        dt = dt.replace(tzinfo=timezone.utc)
                    return dt
                except ValueError:
                    # Fall back to old format handling  
                    dt = datetime.fromisoformat(timestamp_str.replace('Z', '+00:00'))
                    return dt
            
            filtered_runs = [
                run for run in metrics["runs"] 
                if parse_timestamp(run["timestamp"]) > cutoff
            ]
        else:
            filtered_runs = metrics["runs"]
            
        if not filtered_runs:
            print(f"📊 No CI runs in the last {period}")
            return
            
        # Generate report
        print(f"📊 CI Performance Report ({period})")
        print("=" * 50)
        print(f"Total Runs: {len(filtered_runs)}")
        
        # Success rate
        successful = sum(1 for run in filtered_runs if run["status"] == "success")
        success_rate = (successful / len(filtered_runs)) * 100 if filtered_runs else 0
        print(f"Success Rate: {success_rate:.1f}%")
        
        # Duration stats
        duration_runs = [run for run in filtered_runs if run.get("duration_seconds")]
        if duration_runs:
            durations = [run["duration_seconds"] for run in duration_runs]
            avg_duration = sum(durations) / len(durations)
            min_duration = min(durations)
            max_duration = max(durations)
            
            print(f"Average Duration: {avg_duration:.1f}s")
            print(f"Fastest Run: {min_duration:.1f}s")
            print(f"Slowest Run: {max_duration:.1f}s")
        
        # Workflow breakdown
        workflow_stats = {}
        for run in filtered_runs:
            workflow = run["workflow"]
            if workflow not in workflow_stats:
                workflow_stats[workflow] = {"total": 0, "successful": 0}
            workflow_stats[workflow]["total"] += 1
            if run["status"] == "success":
                workflow_stats[workflow]["successful"] += 1
        
        print("\nWorkflow Breakdown:")
        for workflow, stats in workflow_stats.items():
            success_rate = (stats["successful"] / stats["total"]) * 100
            print(f"  {workflow}: {stats['total']} runs, {success_rate:.1f}% success")
        
        # Optimization impact assessment
        print("\n🚀 Optimization Impact Assessment:")
        if avg_duration < 300:  # 5 minutes
            print("✅ Pipeline duration is excellent (< 5 minutes)")
        elif avg_duration < 600:  # 10 minutes
            print("⚠️ Pipeline duration is acceptable (5-10 minutes)")
        else:
            print("❌ Pipeline duration needs optimization (> 10 minutes)")
            
        if success_rate > 95:
            print("✅ Success rate is excellent (> 95%)")
        elif success_rate > 85:
            print("⚠️ Success rate is acceptable (85-95%)")
        else:
            print("❌ Success rate needs attention (< 85%)")

    def dashboard_summary(self):
        """Generate a brief dashboard summary"""
        metrics = self.load_metrics()
        summary = metrics.get("summary", {})
        
        print("🚀 CI Dashboard Summary")
        print("-" * 30)
        print(f"Total Runs: {summary.get('total_runs', 0)}")
        print(f"Success Rate: {summary.get('success_rate', 0):.1f}%")
        print(f"Avg Duration: {summary.get('average_duration', 0):.1f}s")
        
        if summary.get('last_updated'):
            last_updated = datetime.fromisoformat(summary['last_updated'])
            time_since = datetime.utcnow() - last_updated
            print(f"Last Updated: {time_since.total_seconds():.0f}s ago")


def main():
    parser = argparse.ArgumentParser(description="Monitor CI/CD performance metrics")
    parser.add_argument("--workflow", help="Workflow name to record")
    parser.add_argument("--run-id", help="CI run ID")
    parser.add_argument("--duration", type=float, help="Run duration in seconds")
    parser.add_argument("--status", choices=["success", "failure", "cancelled"], help="Run status")
    parser.add_argument("--report", action="store_true", help="Generate performance report")
    parser.add_argument("--period", choices=["daily", "weekly", "monthly", "all"], 
                       default="weekly", help="Report period")
    parser.add_argument("--dashboard", action="store_true", help="Show dashboard summary")
    
    args = parser.parse_args()
    
    monitor = CIPerformanceMonitor()
    
    if args.report:
        monitor.generate_report(args.period)
    elif args.dashboard:
        monitor.dashboard_summary()
    elif args.workflow and args.run_id:
        monitor.record_run(
            workflow_name=args.workflow,
            run_id=args.run_id,
            duration=args.duration,
            status=args.status or "unknown"
        )
    else:
        # Default: show current dashboard
        monitor.dashboard_summary()


if __name__ == "__main__":
    main()