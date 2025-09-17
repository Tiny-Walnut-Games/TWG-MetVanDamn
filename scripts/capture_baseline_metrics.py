#!/usr/bin/env python3
"""
🧙‍♂️ Baseline Metrics Capture - Self-Care Phase 1

Captures comprehensive baseline metrics for the self-care system
to establish the starting point for cycle tracking and analysis.

Metrics captured:
- Idea catalog statistics (count, classification, promotion rate)
- Melt budget state and usage patterns
- Cognitive state and energy levels
- Development telemetry snapshot
- System health indicators

🧙‍♂️ "Measure twice, develop once - but first, measure where you are." 
    - Bootstrap Sentinel
"""

import json
import datetime
from pathlib import Path
from typing import Dict, Any, Optional
import subprocess
import sys


class BaselineMetricsCapture:
    """Captures comprehensive baseline metrics for self-care system"""
    
    def __init__(self, output_path: str = "data/baseline_metrics.json"):
        self.output_path = Path(output_path)
        self.timestamp = datetime.datetime.now().isoformat()
    
    def capture_idea_catalog_metrics(self) -> Dict[str, Any]:
        """Capture idea catalog statistics"""
        try:
            # Run the idea catalog stats command
            result = subprocess.run([
                sys.executable, "src/selfcare/idea_catalog.py", "--stats"
            ], capture_output=True, text=True, cwd=".")
            
            if result.returncode != 0:
                return {"error": f"Failed to get stats: {result.stderr}"}
            
            # Parse the output to extract metrics
            lines = result.stdout.strip().split('\n')
            metrics = {"raw_output": result.stdout}
            
            for line in lines:
                if "Total Ideas:" in line:
                    metrics["total_ideas"] = int(line.split(":")[1].strip())
                elif "Promoted Ideas:" in line:
                    metrics["promoted_ideas"] = int(line.split(":")[1].strip())
                elif "Created:" in line:
                    metrics["catalog_created"] = line.split(":")[1].strip()
                elif "Last Updated:" in line:
                    metrics["last_updated"] = line.split(":")[1].strip()
            
            # Load the raw catalog data for additional analysis
            catalog_path = Path("data/idea_catalog.json")
            if catalog_path.exists():
                with open(catalog_path, 'r') as f:
                    catalog_data = json.load(f)
                    metrics["schema_version"] = catalog_data.get("schema_version")
                    metrics["ideas_count_raw"] = len(catalog_data.get("ideas", []))
                    
                    # Analyze tag distribution
                    tag_distribution = {}
                    for idea in catalog_data.get("ideas", []):
                        tag = idea.get("tag", "untagged")
                        tag_distribution[tag] = tag_distribution.get(tag, 0) + 1
                    metrics["tag_distribution"] = tag_distribution
            
            return metrics
            
        except Exception as e:
            return {"error": str(e)}
    
    def capture_cognitive_state(self) -> Dict[str, Any]:
        """Capture current cognitive state metrics"""
        try:
            cognitive_path = Path("data/cognitive_state.json")
            if cognitive_path.exists():
                with open(cognitive_path, 'r') as f:
                    return json.load(f)
            return {"status": "no_cognitive_state_file"}
        except Exception as e:
            return {"error": str(e)}
    
    def capture_melt_budget_state(self) -> Dict[str, Any]:
        """Capture melt budget metrics"""
        try:
            melt_path = Path("data/melt_budget_state.json")
            if melt_path.exists():
                with open(melt_path, 'r') as f:
                    data = json.load(f)
                    # Add some analysis
                    metrics = data.copy()
                    current_budget = data.get("current_budget", 0)
                    max_budget = data.get("max_budget", 2)
                    metrics["budget_utilization_percent"] = (current_budget / max_budget * 100) if max_budget > 0 else 0
                    return metrics
            return {"status": "no_melt_budget_file"}
        except Exception as e:
            return {"error": str(e)}
    
    def capture_development_telemetry(self) -> Dict[str, Any]:
        """Capture development telemetry state"""
        try:
            telemetry_path = Path("data/dev_telemetry.json")
            if telemetry_path.exists():
                with open(telemetry_path, 'r') as f:
                    return json.load(f)
            return {"status": "no_telemetry_file"}
        except Exception as e:
            return {"error": str(e)}
    
    def capture_sluice_metrics(self) -> Dict[str, Any]:
        """Capture overflow sluice metrics"""
        try:
            sluice_dir = Path("overflow_sluice")
            if not sluice_dir.exists():
                return {"status": "no_sluice_directory"}
            
            metrics = {
                "sluice_files": [],
                "total_files": 0,
                "total_lines": 0
            }
            
            for sluice_file in sluice_dir.glob("*.md"):
                file_info = {
                    "filename": sluice_file.name,
                    "size_bytes": sluice_file.stat().st_size,
                    "modified": datetime.datetime.fromtimestamp(sluice_file.stat().st_mtime).isoformat()
                }
                
                # Count lines
                try:
                    with open(sluice_file, 'r', encoding='utf-8') as f:
                        lines = len(f.readlines())
                        file_info["line_count"] = lines
                        metrics["total_lines"] += lines
                except:
                    file_info["line_count"] = 0
                
                metrics["sluice_files"].append(file_info)
                metrics["total_files"] += 1
            
            return metrics
            
        except Exception as e:
            return {"error": str(e)}
    
    def capture_system_health(self) -> Dict[str, Any]:
        """Capture overall system health indicators"""
        try:
            health = {
                "data_directory_exists": Path("data").exists(),
                "selfcare_module_exists": Path("src/selfcare").exists(),
                "test_suite_passes": False,
                "journal_privacy_protected": False
            }
            
            # Check if tests pass
            result = subprocess.run([
                sys.executable, "tests/test_selfcare_system.py"
            ], capture_output=True, text=True, cwd=".")
            
            health["test_suite_passes"] = result.returncode == 0
            health["test_output_summary"] = result.stdout.split('\n')[-3:-1] if result.stdout else []
            
            # Check journal privacy
            gitignore_path = Path(".gitignore")
            if gitignore_path.exists():
                with open(gitignore_path, 'r') as f:
                    gitignore_content = f.read()
                    health["journal_privacy_protected"] = "local_journal/" in gitignore_content
            
            return health
            
        except Exception as e:
            return {"error": str(e)}
    
    def capture_all_metrics(self) -> Dict[str, Any]:
        """Capture comprehensive baseline metrics"""
        baseline = {
            "metadata": {
                "captured_at": self.timestamp,
                "capture_version": "1.0.0",
                "phase": "self_care_phase_1",
                "purpose": "Baseline metrics for Self-Care Phase 1 exit criteria"
            },
            "metrics": {
                "idea_catalog": self.capture_idea_catalog_metrics(),
                "cognitive_state": self.capture_cognitive_state(),
                "melt_budget": self.capture_melt_budget_state(),
                "development_telemetry": self.capture_development_telemetry(),
                "sluice_overflow": self.capture_sluice_metrics(),
                "system_health": self.capture_system_health()
            }
        }
        
        return baseline
    
    def save_baseline(self, baseline: Dict[str, Any]) -> bool:
        """Save baseline metrics to file"""
        try:
            # Ensure data directory exists
            self.output_path.parent.mkdir(exist_ok=True)
            
            with open(self.output_path, 'w') as f:
                json.dump(baseline, f, indent=2, ensure_ascii=False)
            
            return True
        except Exception as e:
            print(f"❌ Failed to save baseline: {e}")
            return False
    
    def export_baseline_summary(self, baseline: Dict[str, Any]) -> str:
        """Generate human-readable baseline summary"""
        metrics = baseline["metrics"]
        
        summary = [
            "🧙‍♂️ Self-Care System Baseline Metrics",
            "=" * 45,
            f"📅 Captured: {baseline['metadata']['captured_at']}",
            f"🎯 Phase: {baseline['metadata']['phase']}",
            "",
            "📊 Key Metrics:",
        ]
        
        # Idea catalog summary
        idea_metrics = metrics.get("idea_catalog", {})
        if "total_ideas" in idea_metrics:
            summary.extend([
                f"  💡 Total Ideas: {idea_metrics['total_ideas']}",
                f"  🚀 Promoted Ideas: {idea_metrics['promoted_ideas']}",
                f"  🏷️ Tag Distribution: {idea_metrics.get('tag_distribution', {})}"
            ])
        
        # Melt budget summary
        melt_metrics = metrics.get("melt_budget", {})
        if "current_budget" in melt_metrics:
            summary.extend([
                f"  🔥 Melt Budget: {melt_metrics['current_budget']}/{melt_metrics['max_budget']}",
                f"  📈 Budget Utilization: {melt_metrics.get('budget_utilization_percent', 0):.1f}%"
            ])
        
        # Sluice summary
        sluice_metrics = metrics.get("sluice_overflow", {})
        if "total_files" in sluice_metrics:
            summary.extend([
                f"  🌊 Sluice Files: {sluice_metrics['total_files']}",
                f"  📝 Total Lines: {sluice_metrics['total_lines']}"
            ])
        
        # System health summary
        health_metrics = metrics.get("system_health", {})
        test_status = "✅" if health_metrics.get("test_suite_passes") else "❌"
        privacy_status = "✅" if health_metrics.get("journal_privacy_protected") else "❌"
        summary.extend([
            "",
            "🛡️ System Health:",
            f"  {test_status} Test Suite",
            f"  {privacy_status} Journal Privacy Protection"
        ])
        
        return "\n".join(summary)


def main():
    """CLI interface for baseline metrics capture"""
    import argparse
    
    parser = argparse.ArgumentParser(
        description="🧙‍♂️ Self-Care Baseline Metrics Capture",
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    
    parser.add_argument('--output', default='data/baseline_metrics.json',
                       help='Output file for baseline metrics')
    parser.add_argument('--export-summary', action='store_true',
                       help='Export human-readable summary to stdout')
    parser.add_argument('--quiet', action='store_true',
                       help='Minimal output mode')
    
    args = parser.parse_args()
    
    try:
        if not args.quiet:
            print("🧙‍♂️ Capturing Self-Care Baseline Metrics...")
            print("=" * 50)
        
        capturer = BaselineMetricsCapture(args.output)
        baseline = capturer.capture_all_metrics()
        
        # Save to file
        if capturer.save_baseline(baseline):
            if not args.quiet:
                print(f"✅ Baseline metrics saved to: {args.output}")
        else:
            print("❌ Failed to save baseline metrics")
            return 1
        
        # Export summary if requested
        if args.export_summary:
            print("\n" + capturer.export_baseline_summary(baseline))
        elif not args.quiet:
            print("\n📊 Baseline Summary:")
            print(capturer.export_baseline_summary(baseline))
        
        return 0
        
    except Exception as e:
        print(f"❌ Baseline capture failed: {e}")
        return 1


if __name__ == "__main__":
    exit(main())