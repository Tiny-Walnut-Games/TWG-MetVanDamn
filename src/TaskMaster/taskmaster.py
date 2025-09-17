#!/usr/bin/env python3
"""
Living Dev Agent Template - Universal TaskMaster System
Jerry's legendary project management and time tracking (sanitized from Unity)

Execution time: ~35ms for task operations
Transforms project management into epic quest tracking
"""

import argparse
import json
import os
import sys
import datetime
import uuid
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple
from enum import Enum

# Import shared timer service for synchronization
sys.path.insert(0, str(Path(__file__).parent.parent))
try:
    from SharedServices.timer_service import get_timer_service, SharedTimerService
    SHARED_TIMER_AVAILABLE = True
except ImportError:
    print("Warning: Shared timer service not available. Running in standalone mode.")
    SHARED_TIMER_AVAILABLE = False

# Color codes for epic task management
class Colors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'

# Sacred emojis for quest management
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_MAGIC = "🧙‍♂️"
EMOJI_TASK = "📋"
EMOJI_TIMER = "⏱️"
EMOJI_QUEST = "🎯"
EMOJI_ACHIEVEMENT = "🏆"

class TaskPriority(Enum):
    """Jerry's task priority system"""
    LOW = 1
    MEDIUM = 2
    HIGH = 3
    CRITICAL = 4
    LEGENDARY = 5  # Jerry's special category for revolutionary features

class TaskStatus(Enum):
    """Epic quest status tracking"""
    BACKLOG = "backlog"
    TODO = "todo"
    IN_PROGRESS = "in_progress"
    BLOCKED = "blocked"
    REVIEW = "review"
    DONE = "done"
    ARCHIVED = "archived"

class TaskComplexity(Enum):
    """Complexity estimation following Jerry's TLDL standards"""
    TRIVIAL = 1
    LOW = 2
    MEDIUM = 3
    HIGH = 4
    LEGENDARY = 5

class TaskData:
    """Universal task data structure (sanitized from Unity TaskMaster)"""
    
    def __init__(self, title: str, description: str = "", priority: TaskPriority = TaskPriority.MEDIUM):
        self.id = str(uuid.uuid4())
        self.title = title
        self.description = description
        self.priority = priority
        self.status = TaskStatus.TODO
        self.complexity = TaskComplexity.MEDIUM
        
        # Timing data
        self.created_date = datetime.datetime.now()
        self.due_date: Optional[datetime.datetime] = None
        self.completed_date: Optional[datetime.datetime] = None
        self.estimated_hours: float = 0.0
        self.actual_hours: float = 0.0
        
        # Jerry's epic categorization
        self.tags: List[str] = []
        self.epic: str = ""
        self.assignee: str = ""
        
        # TLDL integration
        self.related_tldl_entries: List[str] = []
        self.code_references: List[str] = []
        
        # Time tracking
        self.time_sessions: List[Dict[str, Any]] = []
        self.is_timer_active: bool = False
        self.current_session_start: Optional[datetime.datetime] = None

    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary for JSON storage"""
        return {
            'id': self.id,
            'title': self.title,
            'description': self.description,
            'priority': self.priority.value,
            'status': self.status.value,
            'complexity': self.complexity.value,
            'created_date': self.created_date.isoformat(),
            'due_date': self.due_date.isoformat() if self.due_date else None,
            'completed_date': self.completed_date.isoformat() if self.completed_date else None,
            'estimated_hours': self.estimated_hours,
            'actual_hours': self.actual_hours,
            'tags': self.tags,
            'epic': self.epic,
            'assignee': self.assignee,
            'related_tldl_entries': self.related_tldl_entries,
            'code_references': self.code_references,
            'time_sessions': self.time_sessions,
            'is_timer_active': self.is_timer_active,
            'current_session_start': self.current_session_start.isoformat() if self.current_session_start else None
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'TaskData':
        """Deserialize from dictionary"""
        task = cls(
            title=data.get('title', ''),
            description=data.get('description', ''),
            priority=TaskPriority(data.get('priority', TaskPriority.MEDIUM.value))
        )
        
        task.id = data.get('id', str(uuid.uuid4()))
        task.status = TaskStatus(data.get('status', TaskStatus.TODO.value))
        task.complexity = TaskComplexity(data.get('complexity', TaskComplexity.MEDIUM.value))
        
        # Parse dates
        if data.get('created_date'):
            task.created_date = datetime.datetime.fromisoformat(data['created_date'])
        if data.get('due_date'):
            task.due_date = datetime.datetime.fromisoformat(data['due_date'])
        if data.get('completed_date'):
            task.completed_date = datetime.datetime.fromisoformat(data['completed_date'])
        if data.get('current_session_start'):
            task.current_session_start = datetime.datetime.fromisoformat(data['current_session_start'])
        
        # Load other fields
        task.estimated_hours = data.get('estimated_hours', 0.0)
        task.actual_hours = data.get('actual_hours', 0.0)
        task.tags = data.get('tags', [])
        task.epic = data.get('epic', '')
        task.assignee = data.get('assignee', '')
        task.related_tldl_entries = data.get('related_tldl_entries', [])
        task.code_references = data.get('code_references', [])
        task.time_sessions = data.get('time_sessions', [])
        task.is_timer_active = data.get('is_timer_active', False)
        
        return task

class UniversalTaskMaster:
    """Jerry's legendary task management system (universal version)"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.tasks: Dict[str, TaskData] = {}
        
        # Create task management directories
        self.tasks_dir = self.workspace_path / "tasks"
        self.tasks_dir.mkdir(exist_ok=True)
        
        # Task storage
        self.tasks_file = self.tasks_dir / "tasks.json"
        self.reports_dir = self.tasks_dir / "reports"
        self.reports_dir.mkdir(exist_ok=True)
        
        # Load existing tasks
        self.load_tasks()

    def log_info(self, message: str, emoji: str = EMOJI_INFO):
        """Log informational message with epic styling"""
        print(f"{Colors.OKCYAN}{emoji} [INFO]{Colors.ENDC} {message}")

    def log_success(self, message: str, emoji: str = EMOJI_SUCCESS):
        """Log success message with legendary flair"""
        print(f"{Colors.OKGREEN}{emoji} [SUCCESS]{Colors.ENDC} {message}")

    def log_warning(self, message: str, emoji: str = EMOJI_WARNING):
        """Log warning message"""
        print(f"{Colors.WARNING}{emoji} [WARNING]{Colors.ENDC} {message}")

    def log_error(self, message: str, emoji: str = EMOJI_ERROR):
        """Log error message"""
        print(f"{Colors.FAIL}{emoji} [ERROR]{Colors.ENDC} {message}")

    def create_task(self, title: str, description: str = "", priority: TaskPriority = TaskPriority.MEDIUM,
                   epic: str = "", tags: List[str] = None, estimated_hours: float = 0.0) -> str:
        """Create a new epic task"""
        try:
            task = TaskData(title=title, description=description, priority=priority)
            task.epic = epic
            task.tags = tags or []
            task.estimated_hours = estimated_hours
            
            self.tasks[task.id] = task
            self.save_tasks()
            
            self.log_success(f"Created task: {title} (ID: {task.id[:8]})", EMOJI_QUEST)
            return task.id
            
        except Exception as e:
            self.log_error(f"Failed to create task: {e}")
            return ""

    def update_task_status(self, task_id: str, status: TaskStatus) -> bool:
        """Update task status with automatic completion handling"""
        if task_id not in self.tasks:
            self.log_error(f"Task not found: {task_id}")
            return False
        
        task = self.tasks[task_id]
        old_status = task.status
        task.status = status
        
        # Handle completion
        if status == TaskStatus.DONE and old_status != TaskStatus.DONE:
            task.completed_date = datetime.datetime.now()
            self.log_success(f"Quest completed: {task.title}", EMOJI_ACHIEVEMENT)
        
        # Stop timer if task is completed or archived
        if status in [TaskStatus.DONE, TaskStatus.ARCHIVED] and task.is_timer_active:
            self.stop_timer(task_id)
        
        self.save_tasks()
        return True

    def start_timer(self, task_id: str) -> bool:
        """Start time tracking for a task"""
        if task_id not in self.tasks:
            self.log_error(f"Task not found: {task_id}")
            return False
        
        task = self.tasks[task_id]
        
        if task.is_timer_active:
            self.log_warning(f"Timer already active for: {task.title}")
            return False
        
        task.is_timer_active = True
        task.current_session_start = datetime.datetime.now()
        
        # Auto-update status to in progress
        if task.status == TaskStatus.TODO:
            task.status = TaskStatus.IN_PROGRESS
        
        # Integrate with shared timer service for synchronization
        if SHARED_TIMER_AVAILABLE:
            try:
                timer_service = get_timer_service(str(self.workspace_path))
                shared_session_id = timer_service.start_session(
                    task_name=task.title,
                    project=task.epic or "",
                    category=task.category,
                    taskmaster_task_id=task_id
                )
                self.log_info(f"🔗 Synced with shared timer service", EMOJI_MAGIC)
            except Exception as e:
                self.log_warning(f"Failed to sync with shared timer: {e}")
        
        self.save_tasks()
        self.log_success(f"Timer started for: {task.title}", EMOJI_TIMER)
        return True

    def stop_timer(self, task_id: str) -> bool:
        """Stop time tracking and record session"""
        if task_id not in self.tasks:
            self.log_error(f"Task not found: {task_id}")
            return False
        
        task = self.tasks[task_id]
        
        if not task.is_timer_active or not task.current_session_start:
            self.log_warning(f"No active timer for: {task.title}")
            return False
        
        # Calculate session duration
        session_end = datetime.datetime.now()
        session_duration = (session_end - task.current_session_start).total_seconds() / 3600.0  # Convert to hours
        
        # Sync with shared timer service
        if SHARED_TIMER_AVAILABLE:
            try:
                timer_service = get_timer_service(str(self.workspace_path))
                stopped_session = timer_service.stop_session()
                if stopped_session:
                    self.log_info(f"🔗 Synced stop with shared timer service", EMOJI_MAGIC)
            except Exception as e:
                self.log_warning(f"Failed to sync stop with shared timer: {e}")
        
        # Record session
        session = {
            'start_time': task.current_session_start.isoformat(),
            'end_time': session_end.isoformat(),
            'duration_hours': session_duration,
            'notes': ''
        }
        
        task.time_sessions.append(session)
        task.actual_hours += session_duration
        task.is_timer_active = False
        task.current_session_start = None
        
        self.save_tasks()
        self.log_success(f"Timer stopped for: {task.title} (Session: {session_duration:.2f}h)", EMOJI_TIMER)
        return True

    def add_tldl_reference(self, task_id: str, tldl_entry: str) -> bool:
        """Link task to TLDL entry"""
        if task_id not in self.tasks:
            self.log_error(f"Task not found: {task_id}")
            return False
        
        task = self.tasks[task_id]
        if tldl_entry not in task.related_tldl_entries:
            task.related_tldl_entries.append(tldl_entry)
            self.save_tasks()
            self.log_success(f"Linked TLDL entry to task: {task.title}")
        
        return True

    def get_task_summary(self, task_id: str) -> Optional[Dict[str, Any]]:
        """Get comprehensive task summary"""
        if task_id not in self.tasks:
            return None
        
        task = self.tasks[task_id]
        
        # Calculate time metrics
        total_time = sum(session.get('duration_hours', 0) for session in task.time_sessions)
        if task.is_timer_active and task.current_session_start:
            current_session_time = (datetime.datetime.now() - task.current_session_start).total_seconds() / 3600.0
            total_time += current_session_time
        
        return {
            'id': task.id,
            'title': task.title,
            'status': task.status.value,
            'priority': task.priority.name,
            'complexity': task.complexity.name,
            'epic': task.epic,
            'tags': task.tags,
            'estimated_hours': task.estimated_hours,
            'actual_hours': total_time,
            'variance': total_time - task.estimated_hours if task.estimated_hours > 0 else 0,
            'sessions_count': len(task.time_sessions),
            'is_timer_active': task.is_timer_active,
            'tldl_entries': len(task.related_tldl_entries),
            'created_date': task.created_date.strftime('%Y-%m-%d'),
            'days_old': (datetime.datetime.now() - task.created_date).days
        }

    def list_tasks(self, status_filter: Optional[TaskStatus] = None, 
                   epic_filter: Optional[str] = None) -> List[Dict[str, Any]]:
        """List tasks with optional filtering"""
        filtered_tasks = []
        
        for task in self.tasks.values():
            # Apply filters
            if status_filter and task.status != status_filter:
                continue
            if epic_filter and task.epic != epic_filter:
                continue
            
            summary = self.get_task_summary(task.id)
            if summary:
                filtered_tasks.append(summary)
        
        # Sort by priority and creation date
        filtered_tasks.sort(key=lambda x: (x['priority'], x['created_date']), reverse=True)
        return filtered_tasks

    def generate_epic_report(self, epic_name: str) -> Dict[str, Any]:
        """Generate comprehensive epic progress report"""
        epic_tasks = [task for task in self.tasks.values() if task.epic == epic_name]
        
        if not epic_tasks:
            return {'error': f'No tasks found for epic: {epic_name}'}
        
        # Calculate metrics
        total_tasks = len(epic_tasks)
        completed_tasks = len([t for t in epic_tasks if t.status == TaskStatus.DONE])
        in_progress_tasks = len([t for t in epic_tasks if t.status == TaskStatus.IN_PROGRESS])
        blocked_tasks = len([t for t in epic_tasks if t.status == TaskStatus.BLOCKED])
        
        total_estimated = sum(t.estimated_hours for t in epic_tasks)
        total_actual = sum(t.actual_hours for t in epic_tasks)
        
        # Status breakdown
        status_breakdown = {}
        for status in TaskStatus:
            count = len([t for t in epic_tasks if t.status == status])
            if count > 0:
                status_breakdown[status.value] = count
        
        return {
            'epic_name': epic_name,
            'total_tasks': total_tasks,
            'completed_tasks': completed_tasks,
            'completion_percentage': (completed_tasks / total_tasks * 100) if total_tasks > 0 else 0,
            'in_progress_tasks': in_progress_tasks,
            'blocked_tasks': blocked_tasks,
            'total_estimated_hours': total_estimated,
            'total_actual_hours': total_actual,
            'time_variance': total_actual - total_estimated,
            'status_breakdown': status_breakdown,
            'average_task_time': total_actual / completed_tasks if completed_tasks > 0 else 0
        }

    def export_to_tldl(self, task_id: str) -> str:
        """Export task to TLDL format"""
        if task_id not in self.tasks:
            return ""
        
        task = self.tasks[task_id]
        
        # Sanitize title for filename
        safe_title = "".join(c for c in task.title if c.isalnum() or c in (' ', '-', '_')).rstrip()
        safe_title = safe_title.replace(' ', '-')
        
        tldl_content = f"""# {task.title}

**Entry ID:** TLDL-{datetime.datetime.now().strftime('%Y-%m-%d')}-Task-{safe_title}  
**Author:** TaskMaster System  
**Context:** Task management and time tracking for {task.epic or 'General Development'}  
**Summary:** {task.description or f'Task tracking for {task.title}'}

---

## 🎯 Objective

{task.description or f'Complete task: {task.title}'}

### Task Details
- **Priority:** {task.priority.name}
- **Complexity:** {task.complexity.name}
- **Epic:** {task.epic or 'N/A'}
- **Status:** {task.status.value.replace('_', ' ').title()}
- **Estimated Time:** {task.estimated_hours}h
- **Actual Time:** {task.actual_hours:.2f}h

## ⚡ Actions Taken

### Time Tracking Summary
- **Total Sessions:** {len(task.time_sessions)}
- **Time Variance:** {task.actual_hours - task.estimated_hours:+.2f}h
- **Efficiency:** {(task.estimated_hours / task.actual_hours * 100) if task.actual_hours > 0 else 0:.1f}%

"""

        # Add time sessions
        if task.time_sessions:
            tldl_content += "### Work Sessions\n"
            for i, session in enumerate(task.time_sessions, 1):
                start_time = datetime.datetime.fromisoformat(session['start_time'])
                duration = session['duration_hours']
                tldl_content += f"{i}. {start_time.strftime('%Y-%m-%d %H:%M')} - {duration:.2f}h\n"
            tldl_content += "\n"

        # Add related TLDL entries
        if task.related_tldl_entries:
            tldl_content += "## 🔗 Related TLDL Entries\n\n"
            for entry in task.related_tldl_entries:
                tldl_content += f"- [{entry}]({entry})\n"
            tldl_content += "\n"

        # Add code references
        if task.code_references:
            tldl_content += "## 📁 Code References\n\n"
            for ref in task.code_references:
                tldl_content += f"- `{ref}`\n"
            tldl_content += "\n"

        tldl_content += f"""## 🧠 Key Insights

### Task Management Insights
- Task created: {task.created_date.strftime('%Y-%m-%d')}
- Days to completion: {(task.completed_date - task.created_date).days if task.completed_date else 'In progress'}
- Priority level: {task.priority.name} ({task.priority.value}/5)
- Complexity assessment: {task.complexity.name} ({task.complexity.value}/5)

### Jerry's TaskMaster Integration
This task was managed using Jerry's legendary TaskMaster system, providing:
- Epic-based organization for large feature development
- Time tracking with session management
- TLDL integration for documentation continuity
- Priority and complexity estimation for project planning

## 📋 Next Steps

{"- [x] Task completed successfully" if task.status == TaskStatus.DONE else "- [ ] Continue task development"}
- [ ] Review time estimation accuracy for future planning
- [ ] Update related TLDL entries with outcomes
- [ ] Archive task data for project metrics

---

## TLDL Metadata
**Tags**: #task-management #time-tracking #jerry-taskmaster #{task.epic.lower().replace(' ', '-') if task.epic else 'general'}  
**Complexity**: {task.complexity.name}  
**Impact**: Medium  
**Team Members**: {task.assignee or 'TaskMaster System'}  
**Duration**: {task.actual_hours:.2f}h  
**Related Epic**: {task.epic or 'General Development'}  

---

**Created**: {task.created_date.strftime('%Y-%m-%d %H:%M:%S UTC')}  
**Last Updated**: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}  
**Status**: {'Complete' if task.status == TaskStatus.DONE else 'In Progress'}  

*This task was managed and documented using Jerry's legendary TaskMaster system.* 🧙‍♂️📋⏱️
"""

        return tldl_content

    def save_tasks(self) -> bool:
        """Save tasks to persistent storage"""
        try:
            tasks_data = {
                'version': '1.0',
                'last_updated': datetime.datetime.now().isoformat(),
                'tasks': {task_id: task.to_dict() for task_id, task in self.tasks.items()}
            }
            
            with open(self.tasks_file, 'w', encoding='utf-8') as f:
                json.dump(tasks_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save tasks: {e}")
            return False

    def load_tasks(self) -> bool:
        """Load tasks from persistent storage"""
        try:
            if not self.tasks_file.exists():
                return True  # No existing tasks, that's fine
            
            with open(self.tasks_file, 'r', encoding='utf-8') as f:
                tasks_data = json.load(f)
            
            # Load tasks
            for task_id, task_dict in tasks_data.get('tasks', {}).items():
                self.tasks[task_id] = TaskData.from_dict(task_dict)
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load existing tasks: {e}")
            return False


def main():
    """Main TaskMaster interface"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_MAGIC} Jerry's Universal TaskMaster System {EMOJI_TASK}",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 taskmaster.py --create "Fix validation bug" --epic "Quality Assurance" --priority high
  python3 taskmaster.py --list --status in_progress
  python3 taskmaster.py --start-timer abc123def
  python3 taskmaster.py --stop-timer abc123def
  python3 taskmaster.py --update-status abc123def done
  python3 taskmaster.py --epic-report "Debug Revolution"
        """
    )
    
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Task creation
    parser.add_argument('--create', help='Create new task with title')
    parser.add_argument('--description', default='', help='Task description')
    parser.add_argument('--priority', choices=['low', 'medium', 'high', 'critical', 'legendary'], 
                       default='medium', help='Task priority')
    parser.add_argument('--epic', default='', help='Epic name for task organization')
    parser.add_argument('--estimated-hours', type=float, default=0.0, help='Estimated hours for task')
    parser.add_argument('--tags', nargs='*', default=[], help='Tags for task categorization')
    
    # Task management
    parser.add_argument('--update-status', nargs=2, metavar=('TASK_ID', 'STATUS'),
                       help='Update task status')
    parser.add_argument('--start-timer', help='Start timer for task ID')
    parser.add_argument('--stop-timer', help='Stop timer for task ID')
    parser.add_argument('--add-tldl', nargs=2, metavar=('TASK_ID', 'TLDL_ENTRY'),
                       help='Link TLDL entry to task')
    
    # Reporting
    parser.add_argument('--list', action='store_true', help='List all tasks')
    parser.add_argument('--status', choices=[s.value for s in TaskStatus], help='Filter by status')
    parser.add_argument('--epic-filter', help='Filter by epic name')
    parser.add_argument('--task-details', help='Show detailed task information')
    parser.add_argument('--epic-report', help='Generate epic progress report')
    parser.add_argument('--export-tldl', help='Export task to TLDL format')
    
    args = parser.parse_args()
    
    try:
        # Create TaskMaster instance
        taskmaster = UniversalTaskMaster(workspace_path=args.workspace)
        
        # Handle task creation
        if args.create:
            priority_map = {
                'low': TaskPriority.LOW,
                'medium': TaskPriority.MEDIUM,
                'high': TaskPriority.HIGH,
                'critical': TaskPriority.CRITICAL,
                'legendary': TaskPriority.LEGENDARY
            }
            
            task_id = taskmaster.create_task(
                title=args.create,
                description=args.description,
                priority=priority_map[args.priority],
                epic=args.epic,
                tags=args.tags,
                estimated_hours=args.estimated_hours
            )
            
            if task_id:
                print(f"Task ID: {task_id}")
            
        # Handle status update
        elif args.update_status:
            task_id, status_str = args.update_status
            try:
                status = TaskStatus(status_str)
                taskmaster.update_task_status(task_id, status)
            except ValueError:
                taskmaster.log_error(f"Invalid status: {status_str}")
        
        # Handle timer operations
        elif args.start_timer:
            taskmaster.start_timer(args.start_timer)
        
        elif args.stop_timer:
            taskmaster.stop_timer(args.stop_timer)
        
        # Handle TLDL linking
        elif args.add_tldl:
            task_id, tldl_entry = args.add_tldl
            taskmaster.add_tldl_reference(task_id, tldl_entry)
        
        # Handle reporting
        elif args.list:
            status_filter = TaskStatus(args.status) if args.status else None
            tasks = taskmaster.list_tasks(status_filter=status_filter, epic_filter=args.epic_filter)
            
            if not tasks:
                taskmaster.log_info("No tasks found matching criteria")
            else:
                print(f"\n{Colors.HEADER}📋 TaskMaster Quest Log{Colors.ENDC}")
                print(f"Found {len(tasks)} tasks")
                print("=" * 80)
                
                for task in tasks:
                    status_emoji = "✅" if task['status'] == 'done' else "🔄" if task['status'] == 'in_progress' else "📋"
                    timer_status = " ⏱️" if task['is_timer_active'] else ""
                    
                    print(f"{status_emoji} {task['title'][:50]:<50} | {task['priority']:<8} | {task['actual_hours']:>6.1f}h{timer_status}")
                    if task['epic']:
                        print(f"    Epic: {task['epic']}")
                    print()
        
        elif args.task_details:
            summary = taskmaster.get_task_summary(args.task_details)
            if summary:
                print(f"\n{Colors.HEADER}🎯 Task Details{Colors.ENDC}")
                for key, value in summary.items():
                    print(f"{key.replace('_', ' ').title()}: {value}")
            else:
                taskmaster.log_error("Task not found")
        
        elif args.epic_report:
            report = taskmaster.generate_epic_report(args.epic_report)
            if 'error' in report:
                taskmaster.log_error(report['error'])
            else:
                print(f"\n{Colors.HEADER}🏆 Epic Progress Report: {report['epic_name']}{Colors.ENDC}")
                print("=" * 60)
                print(f"Total Tasks: {report['total_tasks']}")
                print(f"Completion: {report['completion_percentage']:.1f}% ({report['completed_tasks']}/{report['total_tasks']})")
                print(f"In Progress: {report['in_progress_tasks']}")
                print(f"Blocked: {report['blocked_tasks']}")
                print(f"Time Estimated: {report['total_estimated_hours']:.1f}h")
                print(f"Time Actual: {report['total_actual_hours']:.1f}h")
                print(f"Time Variance: {report['time_variance']:+.1f}h")
                print(f"Avg Task Time: {report['average_task_time']:.1f}h")
        
        elif args.export_tldl:
            tldl_content = taskmaster.export_to_tldl(args.export_tldl)
            if tldl_content:
                # Save to TLDL directory
                tldl_dir = taskmaster.workspace_path / "TLDL" / "entries"
                tldl_dir.mkdir(parents=True, exist_ok=True)
                
                task = taskmaster.tasks[args.export_tldl]
                safe_title = "".join(c for c in task.title if c.isalnum() or c in (' ', '-', '_')).rstrip()
                safe_title = safe_title.replace(' ', '-')
                filename = f"TLDL-{datetime.datetime.now().strftime('%Y-%m-%d')}-Task-{safe_title}.md"
                
                tldl_file = tldl_dir / filename
                with open(tldl_file, 'w', encoding='utf-8') as f:
                    f.write(tldl_content)
                
                taskmaster.log_success(f"Exported task to TLDL: {tldl_file}")
            else:
                taskmaster.log_error("Task not found for export")
        
        else:
            # No action specified, show status
            active_timers = [task for task in taskmaster.tasks.values() if task.is_timer_active]
            total_tasks = len(taskmaster.tasks)
            completed_tasks = len([task for task in taskmaster.tasks.values() if task.status == TaskStatus.DONE])
            
            taskmaster.log_info(f"TaskMaster Status: {total_tasks} total tasks, {completed_tasks} completed")
            if active_timers:
                taskmaster.log_info(f"Active timers: {len(active_timers)}")
                for task in active_timers:
                    elapsed = (datetime.datetime.now() - task.current_session_start).total_seconds() / 3600.0
                    print(f"  ⏱️ {task.title} - {elapsed:.2f}h elapsed")
            
            taskmaster.log_info("Use --help to see available commands")
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} TaskMaster interrupted{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} TaskMaster error: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
