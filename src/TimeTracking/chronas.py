#!/usr/bin/env python3
"""
Living Dev Agent Template - Universal Time Tracking (Chronas)
Jerry's legendary time tracking system (sanitized from Unity)

Execution time: ~20ms for time operations
Cross-platform time tracking with session management
"""

import argparse
import json
import os
import sys
import datetime
from pathlib import Path
from typing import List, Dict, Any, Optional

# Import shared timer service for synchronization
sys.path.insert(0, str(Path(__file__).parent.parent))
try:
    from SharedServices.timer_service import get_timer_service, SharedTimerService
    SHARED_TIMER_AVAILABLE = True
except ImportError:
    print("Warning: Shared timer service not available. Running in standalone mode.")
    SHARED_TIMER_AVAILABLE = False

# Color codes for epic time management
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

# Sacred emojis for time mastery
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_MAGIC = "🧙‍♂️"
EMOJI_TIMER = "⏱️"
EMOJI_CHRONAS = "🕰️"
EMOJI_CLOCK = "⏰"

class TimeCard:
    """Universal time card for session tracking"""
    
    def __init__(self, task_name: str, start_time: datetime.datetime = None):
        self.task_name = task_name
        self.start_time = start_time or datetime.datetime.now()
        self.end_time: Optional[datetime.datetime] = None
        self.duration_seconds: float = 0.0
        self.notes: str = ""
        self.category: str = "development"
        self.project: str = ""
        
    def complete_session(self, notes: str = "") -> float:
        """Complete the time session and return duration in hours"""
        self.end_time = datetime.datetime.now()
        self.duration_seconds = (self.end_time - self.start_time).total_seconds()
        self.notes = notes
        return self.duration_seconds / 3600.0  # Return hours
    
    def get_duration_hours(self) -> float:
        """Get current duration in hours"""
        if self.end_time:
            return self.duration_seconds / 3600.0
        else:
            # Calculate current elapsed time
            elapsed = (datetime.datetime.now() - self.start_time).total_seconds()
            return elapsed / 3600.0
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'task_name': self.task_name,
            'start_time': self.start_time.isoformat(),
            'end_time': self.end_time.isoformat() if self.end_time else None,
            'duration_seconds': self.duration_seconds,
            'notes': self.notes,
            'category': self.category,
            'project': self.project
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'TimeCard':
        """Deserialize from dictionary"""
        card = cls(
            task_name=data['task_name'],
            start_time=datetime.datetime.fromisoformat(data['start_time'])
        )
        
        if data.get('end_time'):
            card.end_time = datetime.datetime.fromisoformat(data['end_time'])
        
        card.duration_seconds = data.get('duration_seconds', 0.0)
        card.notes = data.get('notes', '')
        card.category = data.get('category', 'development')
        card.project = data.get('project', '')
        
        return card

class UniversalChronas:
    """Jerry's legendary time tracking system (universal version)"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.current_session: Optional[TimeCard] = None
        self.time_cards: List[TimeCard] = []
        
        # Create time tracking directories
        self.time_dir = self.workspace_path / "time_tracking"
        self.time_dir.mkdir(exist_ok=True)
        
        # Storage files
        self.session_file = self.time_dir / "current_session.json"
        self.history_file = self.time_dir / "time_history.json"
        
        # Load existing data
        self.load_current_session()
        self.load_time_history()

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

    def start_session(self, task_name: str, project: str = "", category: str = "development") -> bool:
        """Start a new time tracking session"""
        if self.current_session:
            self.log_warning(f"Session already active: {self.current_session.task_name}")
            elapsed = self.current_session.get_duration_hours()
            self.log_info(f"Current session duration: {elapsed:.2f}h")
            return False
        
        self.current_session = TimeCard(task_name=task_name)
        self.current_session.project = project
        self.current_session.category = category
        
        # Integrate with shared timer service for synchronization
        shared_session_id = None
        if SHARED_TIMER_AVAILABLE:
            try:
                timer_service = get_timer_service(str(self.workspace_path))
                shared_session_id = timer_service.start_session(
                    task_name=task_name,
                    project=project,
                    category=category,
                    chronas_session_id=id(self.current_session)  # Use object id as chronas session id
                )
                self.log_info(f"🔗 Synced with shared timer service", EMOJI_MAGIC)
            except Exception as e:
                self.log_warning(f"Failed to sync with shared timer: {e}")
        
        self.save_current_session()
        self.log_success(f"Started tracking: {task_name}", EMOJI_TIMER)
        
        if project:
            self.log_info(f"Project: {project}")
        if category != "development":
            self.log_info(f"Category: {category}")
        
        return True

    def stop_session(self, notes: str = "") -> bool:
        """Stop current time tracking session"""
        if not self.current_session:
            self.log_warning("No active session to stop")
            return False
        
        duration_hours = self.current_session.complete_session(notes)
        
        # Sync with shared timer service
        if SHARED_TIMER_AVAILABLE:
            try:
                timer_service = get_timer_service(str(self.workspace_path))
                stopped_session = timer_service.stop_session(notes)
                if stopped_session:
                    self.log_info(f"🔗 Synced stop with shared timer service", EMOJI_MAGIC)
            except Exception as e:
                self.log_warning(f"Failed to sync stop with shared timer: {e}")
        
        # Archive the completed session
        self.time_cards.append(self.current_session)
        
        task_name = self.current_session.task_name
        self.current_session = None
        
        # Save data
        self.save_time_history()
        self.clear_current_session()
        
        self.log_success(f"Stopped tracking: {task_name}", EMOJI_TIMER)
        self.log_info(f"Session duration: {duration_hours:.2f}h")
        
        if notes:
            self.log_info(f"Notes: {notes}")
        
        return True

    def get_session_status(self) -> Dict[str, Any]:
        """Get current session status"""
        status = {'active': False, 'shared_timer_active': False}
        
        # Check shared timer service first
        if SHARED_TIMER_AVAILABLE:
            try:
                timer_service = get_timer_service(str(self.workspace_path))
                shared_status = timer_service.get_session_status()
                status['shared_timer_active'] = shared_status['active']
                
                # If shared timer is active but we don't have a local session, sync
                if shared_status['active'] and not self.current_session:
                    self.log_info("🔄 Found active shared timer session, syncing locally", EMOJI_MAGIC)
                    # Create local session from shared timer
                    self.current_session = TimeCard(task_name=shared_status['task_name'])
                    self.current_session.project = shared_status.get('project', '')
                    self.current_session.category = shared_status.get('category', 'development')
                    # Approximate start time based on elapsed time
                    elapsed_seconds = shared_status.get('elapsed_seconds', 0)
                    self.current_session.start_time = datetime.datetime.now() - datetime.timedelta(seconds=elapsed_seconds)
                    self.save_current_session()
                    
                # Update status with shared timer info
                status.update(shared_status)
                
            except Exception as e:
                self.log_warning(f"Failed to check shared timer status: {e}")
        
        if not self.current_session:
            return status
        
        elapsed_hours = self.current_session.get_duration_hours()
        
        status.update({
            'active': True,
            'task_name': self.current_session.task_name,
            'project': self.current_session.project,
            'category': self.current_session.category,
            'start_time': self.current_session.start_time.strftime('%Y-%m-%d %H:%M:%S'),
            'elapsed_hours': elapsed_hours,
            'elapsed_formatted': self.format_duration(elapsed_hours * 3600)
        })
        
        return status

    def get_daily_summary(self, date: datetime.date = None) -> Dict[str, Any]:
        """Get summary for specific date"""
        target_date = date or datetime.date.today()
        
        # Filter time cards for target date
        daily_cards = [
            card for card in self.time_cards
            if card.start_time.date() == target_date and card.end_time
        ]
        
        # Include current session if it's from today
        if (self.current_session and 
            self.current_session.start_time.date() == target_date):
            # Create a temporary card for current session
            temp_card = TimeCard(
                task_name=self.current_session.task_name,
                start_time=self.current_session.start_time
            )
            temp_card.duration_seconds = (datetime.datetime.now() - self.current_session.start_time).total_seconds()
            temp_card.project = self.current_session.project
            temp_card.category = self.current_session.category
            daily_cards.append(temp_card)
        
        if not daily_cards:
            return {
                'date': target_date.strftime('%Y-%m-%d'),
                'total_hours': 0.0,
                'sessions': [],
                'projects': {},
                'categories': {}
            }
        
        # Calculate totals
        total_seconds = sum(card.duration_seconds for card in daily_cards)
        total_hours = total_seconds / 3600.0
        
        # Group by project and category
        projects = {}
        categories = {}
        
        for card in daily_cards:
            # Project totals
            if card.project:
                if card.project not in projects:
                    projects[card.project] = 0.0
                projects[card.project] += card.duration_seconds / 3600.0
            
            # Category totals
            if card.category not in categories:
                categories[card.category] = 0.0
            categories[card.category] += card.duration_seconds / 3600.0
        
        # Session details
        sessions = []
        for card in daily_cards:
            sessions.append({
                'task_name': card.task_name,
                'project': card.project,
                'category': card.category,
                'start_time': card.start_time.strftime('%H:%M'),
                'duration_hours': card.duration_seconds / 3600.0,
                'notes': card.notes,
                'is_current': card == daily_cards[-1] and self.current_session is not None
            })
        
        return {
            'date': target_date.strftime('%Y-%m-%d'),
            'total_hours': total_hours,
            'total_formatted': self.format_duration(total_seconds),
            'sessions': sessions,
            'session_count': len(daily_cards),
            'projects': projects,
            'categories': categories
        }

    def export_timesheet(self, start_date: datetime.date, end_date: datetime.date) -> str:
        """Export timesheet for date range"""
        # Filter cards for date range
        timesheet_cards = [
            card for card in self.time_cards
            if (card.start_time.date() >= start_date and 
                card.start_time.date() <= end_date and 
                card.end_time)
        ]
        
        if not timesheet_cards:
            return f"No time entries found for {start_date} to {end_date}"
        
        # Generate timesheet content
        content = f"""# Timesheet Report

**Period:** {start_date} to {end_date}  
**Generated:** {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')}  
**Total Entries:** {len(timesheet_cards)}

---

## Summary

"""
        
        # Calculate totals
        total_seconds = sum(card.duration_seconds for card in timesheet_cards)
        total_hours = total_seconds / 3600.0
        
        content += f"**Total Time:** {total_hours:.2f}h ({self.format_duration(total_seconds)})\n\n"
        
        # Project breakdown
        projects = {}
        for card in timesheet_cards:
            if card.project:
                if card.project not in projects:
                    projects[card.project] = 0.0
                projects[card.project] += card.duration_seconds / 3600.0
        
        if projects:
            content += "### By Project\n\n"
            for project, hours in sorted(projects.items()):
                percentage = (hours / total_hours * 100) if total_hours > 0 else 0
                content += f"- **{project}:** {hours:.2f}h ({percentage:.1f}%)\n"
            content += "\n"
        
        # Daily breakdown
        content += "## Daily Breakdown\n\n"
        
        # Group by date
        daily_groups = {}
        for card in timesheet_cards:
            date_key = card.start_time.date()
            if date_key not in daily_groups:
                daily_groups[date_key] = []
            daily_groups[date_key].append(card)
        
        for date_key in sorted(daily_groups.keys()):
            cards = daily_groups[date_key]
            daily_total = sum(card.duration_seconds for card in cards) / 3600.0
            
            content += f"### {date_key.strftime('%Y-%m-%d')} - {daily_total:.2f}h\n\n"
            
            for card in sorted(cards, key=lambda x: x.start_time):
                start_time = card.start_time.strftime('%H:%M')
                duration = card.duration_seconds / 3600.0
                project_info = f" ({card.project})" if card.project else ""
                
                content += f"- **{start_time}** - {card.task_name}{project_info} - {duration:.2f}h\n"
                if card.notes:
                    content += f"  *{card.notes}*\n"
            
            content += "\n"
        
        return content

    @staticmethod
    def format_duration(seconds: float) -> str:
        """Format duration in human-readable format"""
        hours = int(seconds // 3600)
        minutes = int((seconds % 3600) // 60)
        
        if hours > 0:
            return f"{hours}h {minutes}m"
        else:
            return f"{minutes}m"

    def save_current_session(self) -> bool:
        """Save current session to file"""
        try:
            if self.current_session:
                session_data = {
                    'session': self.current_session.to_dict()
                }
                
                with open(self.session_file, 'w', encoding='utf-8') as f:
                    json.dump(session_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save session: {e}")
            return False

    def load_current_session(self) -> bool:
        """Load current session from file"""
        try:
            if not self.session_file.exists():
                return True
            
            with open(self.session_file, 'r', encoding='utf-8') as f:
                session_data = json.load(f)
            
            if 'session' in session_data:
                self.current_session = TimeCard.from_dict(session_data['session'])
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load session: {e}")
            return False

    def clear_current_session(self) -> bool:
        """Clear current session file"""
        try:
            if self.session_file.exists():
                self.session_file.unlink()
            return True
        except Exception as e:
            self.log_error(f"Failed to clear session: {e}")
            return False

    def save_time_history(self) -> bool:
        """Save time history to file"""
        try:
            history_data = {
                'version': '1.0',
                'last_updated': datetime.datetime.now().isoformat(),
                'time_cards': [card.to_dict() for card in self.time_cards]
            }
            
            with open(self.history_file, 'w', encoding='utf-8') as f:
                json.dump(history_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save history: {e}")
            return False

    def load_time_history(self) -> bool:
        """Load time history from file"""
        try:
            if not self.history_file.exists():
                return True
            
            with open(self.history_file, 'r', encoding='utf-8') as f:
                history_data = json.load(f)
            
            self.time_cards = [
                TimeCard.from_dict(card_data) 
                for card_data in history_data.get('time_cards', [])
            ]
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load history: {e}")
            return False


def main():
    """Main Chronas time tracking interface"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_MAGIC} Universal Chronas Time Tracking {EMOJI_CHRONAS}",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 chronas.py --start "Debug validation bug" --project "Template System"
  python3 chronas.py --stop --notes "Fixed the recursive parsing issue"
  python3 chronas.py --status
  python3 chronas.py --daily-summary
  python3 chronas.py --timesheet --start-date 2025-01-01 --end-date 2025-01-07
        """
    )
    
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Session management
    parser.add_argument('--start', help='Start tracking task with name')
    parser.add_argument('--project', default='', help='Project name for session')
    parser.add_argument('--category', default='development', help='Category for session')
    parser.add_argument('--stop', action='store_true', help='Stop current session')
    parser.add_argument('--notes', default='', help='Notes for completed session')
    
    # Status and reporting
    parser.add_argument('--status', action='store_true', help='Show current session status')
    parser.add_argument('--daily-summary', help='Show daily summary (YYYY-MM-DD, default: today)')
    parser.add_argument('--timesheet', action='store_true', help='Generate timesheet report')
    parser.add_argument('--start-date', help='Start date for timesheet (YYYY-MM-DD)')
    parser.add_argument('--end-date', help='End date for timesheet (YYYY-MM-DD)')
    
    args = parser.parse_args()
    
    try:
        # Create Chronas instance
        chronas = UniversalChronas(workspace_path=args.workspace)
        
        # Handle session start
        if args.start:
            chronas.start_session(
                task_name=args.start,
                project=args.project,
                category=args.category
            )
        
        # Handle session stop
        elif args.stop:
            chronas.stop_session(notes=args.notes)
        
        # Handle status check
        elif args.status:
            status = chronas.get_session_status()
            
            if status['active']:
                print(f"\n{Colors.HEADER}⏱️ Active Session{Colors.ENDC}")
                print(f"Task: {status['task_name']}")
                if status['project']:
                    print(f"Project: {status['project']}")
                print(f"Category: {status['category']}")
                print(f"Started: {status['start_time']}")
                print(f"Elapsed: {status['elapsed_formatted']} ({status['elapsed_hours']:.2f}h)")
            else:
                chronas.log_info("No active session", EMOJI_CLOCK)
        
        # Handle daily summary
        elif args.daily_summary is not None:
            if args.daily_summary:
                target_date = datetime.datetime.strptime(args.daily_summary, '%Y-%m-%d').date()
            else:
                target_date = datetime.date.today()
            
            summary = chronas.get_daily_summary(target_date)
            
            print(f"\n{Colors.HEADER}📅 Daily Summary: {summary['date']}{Colors.ENDC}")
            print(f"Total Time: {summary['total_formatted']} ({summary['total_hours']:.2f}h)")
            print(f"Sessions: {summary['session_count']}")
            
            if summary['projects']:
                print("\nBy Project:")
                for project, hours in summary['projects'].items():
                    print(f"  {project}: {hours:.2f}h")
            
            if summary['categories']:
                print("\nBy Category:")
                for category, hours in summary['categories'].items():
                    print(f"  {category}: {hours:.2f}h")
            
            if summary['sessions']:
                print("\nSessions:")
                for session in summary['sessions']:
                    current_marker = " (current)" if session['is_current'] else ""
                    project_info = f" ({session['project']})" if session['project'] else ""
                    print(f"  {session['start_time']} - {session['task_name']}{project_info} - {session['duration_hours']:.2f}h{current_marker}")
                    if session['notes']:
                        print(f"    Notes: {session['notes']}")
        
        # Handle timesheet generation
        elif args.timesheet:
            if not args.start_date or not args.end_date:
                chronas.log_error("Both --start-date and --end-date required for timesheet")
                sys.exit(1)
            
            start_date = datetime.datetime.strptime(args.start_date, '%Y-%m-%d').date()
            end_date = datetime.datetime.strptime(args.end_date, '%Y-%m-%d').date()
            
            timesheet_content = chronas.export_timesheet(start_date, end_date)
            
            # Save timesheet to file
            timesheet_file = chronas.time_dir / f"timesheet_{start_date}_{end_date}.md"
            with open(timesheet_file, 'w', encoding='utf-8') as f:
                f.write(timesheet_content)
            
            chronas.log_success(f"Timesheet saved: {timesheet_file}")
            print(f"\n{timesheet_content}")
        
        else:
            # No action specified, show current status
            status = chronas.get_session_status()
            total_cards = len(chronas.time_cards)
            
            chronas.log_info(f"Chronas Status: {total_cards} recorded sessions")
            
            if status['active']:
                chronas.log_info(f"Active session: {status['task_name']} ({status['elapsed_formatted']})")
            else:
                chronas.log_info("No active session")
            
            chronas.log_info("Use --help to see available commands")
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} Chronas interrupted{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} Chronas error: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
