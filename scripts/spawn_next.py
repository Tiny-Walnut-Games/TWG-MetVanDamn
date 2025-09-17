#!/usr/bin/env python3
"""
🧙‍♂️ RitualBot Spawn Script - Weekly Auto-Issue Generation

The sacred scroll spawner that reads roadmap.yml and auto-creates focused 
issues for active phases or newly promoted backlog items.

Features:
- Reads roadmap.yml configuration
- Generates single focused issue per week
- Respects user preferences (no auto-assignment unless explicitly enabled)
- Supports manual triggering and opt-out
- Generates GitHub-compatible issue content

🧙‍♂️ "Let the scrolls write themselves, but let the developer choose 
    which quests to embark upon." - Bootstrap Sentinel
"""

import argparse
import datetime
import json
import yaml
from pathlib import Path
from typing import Dict, Any, List, Optional, Tuple


class RitualBotSpawner:
    """The sacred scroll spawner for automated issue generation"""
    
    def __init__(self, roadmap_path: str = "roadmap.yml", dry_run: bool = False):
        self.roadmap_path = Path(roadmap_path)
        self.dry_run = dry_run
        self.roadmap_data = self._load_roadmap()
        
    def _load_roadmap(self) -> Dict[str, Any]:
        """Load roadmap configuration from YAML"""
        if not self.roadmap_path.exists():
            raise FileNotFoundError(f"Roadmap file not found: {self.roadmap_path}")
            
        with open(self.roadmap_path, 'r', encoding='utf-8') as f:
            return yaml.safe_load(f)
    
    def _get_next_task(self) -> Optional[Tuple[str, Dict[str, Any]]]:
        """Find the next task to create an issue for"""
        # First, check active phase for pending tasks
        active_phase = self.roadmap_data.get("active_phase", {})
        if active_phase and active_phase.get("status") == "in_progress":
            checklist = active_phase.get("checklist", [])
            for task in checklist:
                if task.get("status") == "pending" and not task.get("assigned_issue"):
                    return "active_phase", task
        
        # If no active phase tasks, check backlog for high priority items
        backlog = self.roadmap_data.get("backlog", [])
        for task in backlog:
            if task.get("priority") == "high" and not task.get("assigned_issue"):
                return "backlog", task
                
        # Check medium priority backlog items
        for task in backlog:
            if task.get("priority") == "medium" and not task.get("assigned_issue"):
                return "backlog", task
                
        return None
    
    def _generate_issue_content(self, task_source: str, task: Dict[str, Any]) -> Dict[str, str]:
        """Generate GitHub issue content from task"""
        auto_config = self.roadmap_data.get("auto_issue", {})
        template = auto_config.get("issue_template", "")
        
        # Prepare template variables
        if task_source == "active_phase":
            phase = self.roadmap_data.get("active_phase", {})
            phase_name = phase.get("name", "Unknown Phase")
            phase_id = phase.get("id", "unknown")
            exit_criteria = "\n".join([f"- {criteria}" for criteria in phase.get("exit_criteria", [])])
        else:
            phase_name = "Backlog Item"
            phase_id = "backlog"
            exit_criteria = f"- Complete: {task.get('task', 'Unknown task')}"
        
        task_title = task.get("task", task.get("title", "Unknown Task"))
        task_description = task.get("notes", task.get("description", "No description provided"))
        generation_date = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        
        # Format the issue content
        issue_body = template.format(
            phase_name=phase_name,
            phase_id=phase_id,
            task_title=task_title,
            task_description=task_description,
            exit_criteria=exit_criteria,
            generation_date=generation_date,
            related_issues="None"  # Could be enhanced later
        )
        
        # Generate title
        title_template = auto_config.get("title_template", "🧙‍♂️ [RitualBot] Weekly Focus: {task_title}")
        issue_title = title_template.format(
            phase_name=phase_name,
            task_title=task_title
        )
        
        return {
            "title": issue_title,
            "body": issue_body,
            "labels": [auto_config.get("label_prefix", "ritual-auto")]
        }
    
    def _should_auto_assign(self) -> bool:
        """Check if auto-assignment to Copilot is enabled"""
        auto_config = self.roadmap_data.get("auto_issue", {})
        return auto_config.get("assign_to_copilot", False)
    
    def _update_roadmap_with_issue(self, task_source: str, task: Dict[str, Any], issue_number: str):
        """Update roadmap to mark task as having an assigned issue"""
        if task_source == "active_phase":
            checklist = self.roadmap_data["active_phase"]["checklist"]
            for i, checklist_task in enumerate(checklist):
                if checklist_task.get("task") == task.get("task"):
                    checklist[i]["assigned_issue"] = issue_number
                    break
        else:  # backlog
            backlog = self.roadmap_data["backlog"]
            for i, backlog_task in enumerate(backlog):
                if backlog_task.get("task") == task.get("task"):
                    backlog[i]["assigned_issue"] = issue_number
                    break
        
        # Update last_updated timestamp
        self.roadmap_data["metadata"]["last_updated"] = datetime.datetime.now().strftime("%Y-%m-%d")
        
        # Save updated roadmap
        if not self.dry_run:
            with open(self.roadmap_path, 'w', encoding='utf-8') as f:
                yaml.dump(self.roadmap_data, f, default_flow_style=False, sort_keys=False)
    
    def generate_next_issue(self) -> Optional[Dict[str, Any]]:
        """Generate the next weekly issue based on roadmap"""
        
        # Check if auto-issue generation is enabled
        auto_config = self.roadmap_data.get("auto_issue", {})
        if not auto_config.get("enabled", True):
            print("ℹ️ Auto-issue generation is disabled in roadmap.yml")
            return None
        
        # Find next task to create issue for
        next_task = self._get_next_task()
        if not next_task:
            print("ℹ️ No pending tasks found for issue generation")
            return None
        
        task_source, task = next_task
        print(f"📋 Found next task: {task.get('task', 'Unknown task')} (from {task_source})")
        
        # Generate issue content
        issue_content = self._generate_issue_content(task_source, task)
        
        if self.dry_run:
            print("\n🔍 DRY RUN - Issue that would be created:")
            print("=" * 60)
            print(f"Title: {issue_content['title']}")
            print(f"Labels: {', '.join(issue_content['labels'])}")
            print("\nBody:")
            print(issue_content['body'])
            print("=" * 60)
            return issue_content
        
        # In a real implementation, this would create the GitHub issue
        # For now, we'll just simulate and return the content
        print(f"✨ Issue content generated for: {issue_content['title']}")
        print("📝 To create the issue, run with --create-github-issue (not implemented)")
        
        return issue_content
    
    def list_pending_tasks(self):
        """List all pending tasks that could be promoted to issues"""
        print("📋 Pending Tasks Available for Issue Generation:")
        print("=" * 50)
        
        # Active phase tasks
        active_phase = self.roadmap_data.get("active_phase", {})
        if active_phase and active_phase.get("status") == "in_progress":
            print(f"\n🎯 Active Phase: {active_phase.get('name', 'Unknown')}")
            checklist = active_phase.get("checklist", [])
            active_count = 0
            for task in checklist:
                if task.get("status") == "pending" and not task.get("assigned_issue"):
                    print(f"  - {task.get('task', 'Unknown task')}")
                    active_count += 1
            if active_count == 0:
                print("  - No pending tasks")
        
        # Backlog tasks
        print("\n📦 Backlog Tasks:")
        backlog = self.roadmap_data.get("backlog", [])
        priorities = ["high", "medium", "low"]
        
        for priority in priorities:
            priority_tasks = [task for task in backlog if task.get("priority") == priority and not task.get("assigned_issue")]
            if priority_tasks:
                print(f"\n  {priority.title()} Priority:")
                for task in priority_tasks:
                    effort = task.get("estimated_effort", "Unknown")
                    recurring = " (recurring)" if task.get("recurring") else ""
                    print(f"    - {task.get('task', 'Unknown task')} [{effort}]{recurring}")
    
    def get_stats(self) -> Dict[str, Any]:
        """Get roadmap statistics"""
        active_phase = self.roadmap_data.get("active_phase", {})
        upcoming_phases = self.roadmap_data.get("upcoming_phases", [])
        backlog = self.roadmap_data.get("backlog", [])
        
        # Count tasks by status
        pending_active = 0
        completed_active = 0
        
        if active_phase:
            checklist = active_phase.get("checklist", [])
            for task in checklist:
                if task.get("status") == "pending":
                    pending_active += 1
                elif task.get("status") == "completed":
                    completed_active += 1
        
        # Count backlog by priority
        backlog_by_priority = {"high": 0, "medium": 0, "low": 0}
        for task in backlog:
            priority = task.get("priority", "low")
            backlog_by_priority[priority] += 1
        
        return {
            "active_phase": {
                "name": active_phase.get("name", "None"),
                "status": active_phase.get("status", "None"),
                "pending_tasks": pending_active,
                "completed_tasks": completed_active
            },
            "upcoming_phases": len(upcoming_phases),
            "backlog": {
                "total": len(backlog),
                "by_priority": backlog_by_priority
            },
            "metadata": self.roadmap_data.get("metadata", {})
        }


def main():
    """CLI interface for RitualBot spawn operations"""
    parser = argparse.ArgumentParser(
        description="🧙‍♂️ RitualBot Spawn Script - Weekly Auto-Issue Generation",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 scripts/spawn_next.py                    # Generate next issue (dry run)
  python3 scripts/spawn_next.py --execute          # Generate and create issue
  python3 scripts/spawn_next.py --list-pending     # Show all pending tasks
  python3 scripts/spawn_next.py --stats            # Show roadmap statistics
  python3 scripts/spawn_next.py --roadmap custom.yml  # Use custom roadmap file
        """
    )
    
    parser.add_argument('--roadmap', default='roadmap.yml',
                       help='Path to roadmap YAML file (default: roadmap.yml)')
    parser.add_argument('--execute', action='store_true',
                       help='Actually create the issue (default is dry run)')
    parser.add_argument('--list-pending', action='store_true',
                       help='List all pending tasks available for issue generation')
    parser.add_argument('--stats', action='store_true',
                       help='Show roadmap statistics')
    parser.add_argument('--quiet', action='store_true',
                       help='Minimal output mode')
    
    args = parser.parse_args()
    
    try:
        spawner = RitualBotSpawner(args.roadmap, dry_run=not args.execute)
        
        if not args.quiet:
            print("🧙‍♂️ RitualBot Spawn Script")
            print("=" * 40)
        
        if args.list_pending:
            spawner.list_pending_tasks()
            
        elif args.stats:
            stats = spawner.get_stats()
            print("📊 Roadmap Statistics:")
            print("=" * 25)
            print(f"Active Phase: {stats['active_phase']['name']} ({stats['active_phase']['status']})")
            print(f"  - Pending: {stats['active_phase']['pending_tasks']}")
            print(f"  - Completed: {stats['active_phase']['completed_tasks']}")
            print(f"Upcoming Phases: {stats['upcoming_phases']}")
            print(f"Backlog: {stats['backlog']['total']} total")
            for priority, count in stats['backlog']['by_priority'].items():
                print(f"  - {priority.title()}: {count}")
            print(f"Last Updated: {stats['metadata'].get('last_updated', 'Unknown')}")
            
        else:
            # Generate next issue
            result = spawner.generate_next_issue()
            if result and args.execute:
                print("⚠️ GitHub issue creation not yet implemented")
                print("💡 For now, copy the generated content above to create the issue manually")
                
    except FileNotFoundError as e:
        print(f"❌ Error: {e}")
        print("💡 Make sure roadmap.yml exists in the current directory")
        return 1
        
    except yaml.YAMLError as e:
        print(f"❌ YAML parsing error: {e}")
        print("💡 Check your roadmap.yml file for syntax errors")
        return 1
        
    except Exception as e:
        print(f"❌ Unexpected error: {e}")
        return 1
    
    return 0


if __name__ == "__main__":
    exit(main())