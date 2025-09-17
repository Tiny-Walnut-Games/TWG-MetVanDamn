#!/usr/bin/env python3
"""
Living Dev Agent - Shared Timer Service
Persistent timer service for Chronas and Taskmaster synchronization

This service provides:
- Persistent timer storage that survives application restarts
- Synchronization between Chronas and Taskmaster
- Cross-platform timer state management
- Session recovery and continuity
"""

import json
import os
import time
import datetime
import threading
import uuid
from pathlib import Path
from typing import Dict, Optional, Any, List
from dataclasses import dataclass, asdict
from enum import Enum

class TimerState(Enum):
    """Timer state enumeration"""
    STOPPED = "stopped"
    RUNNING = "running" 
    PAUSED = "paused"

@dataclass
class TimerSession:
    """Represents a timer session"""
    session_id: str
    task_name: str
    project: str = ""
    category: str = "development"
    start_time: float = 0.0  # Unix timestamp
    end_time: Optional[float] = None
    total_elapsed: float = 0.0  # Total seconds elapsed
    state: str = TimerState.STOPPED.value
    notes: str = ""
    chronas_session_id: Optional[str] = None
    taskmaster_task_id: Optional[str] = None
    last_heartbeat: float = 0.0  # For detecting stale sessions

class SharedTimerService:
    """Shared timer service for cross-tool synchronization"""
    
    def __init__(self, workspace_dir: str = None):
        """Initialize timer service with workspace directory"""
        self.workspace_dir = Path(workspace_dir) if workspace_dir else Path.cwd()
        self.timer_data_dir = self.workspace_dir / ".timer_data"
        self.timer_data_dir.mkdir(exist_ok=True)
        
        self.sessions_file = self.timer_data_dir / "timer_sessions.json"
        self.active_session_file = self.timer_data_dir / "active_session.json"
        self.sync_lock_file = self.timer_data_dir / "sync.lock"
        
        self._lock = threading.Lock()
        self._load_sessions()
        
    def _load_sessions(self):
        """Load sessions from persistent storage"""
        try:
            if self.sessions_file.exists():
                with open(self.sessions_file, 'r') as f:
                    data = json.load(f)
                    self.sessions = {k: TimerSession(**v) for k, v in data.items()}
            else:
                self.sessions = {}
                
            # Load active session
            if self.active_session_file.exists():
                with open(self.active_session_file, 'r') as f:
                    active_data = json.load(f)
                    self.active_session_id = active_data.get('session_id')
            else:
                self.active_session_id = None
                
        except Exception as e:
            print(f"Warning: Could not load timer sessions: {e}")
            self.sessions = {}
            self.active_session_id = None
    
    def _save_sessions(self):
        """Save sessions to persistent storage"""
        try:
            with self._lock:
                # Save all sessions
                sessions_data = {k: asdict(v) for k, v in self.sessions.items()}
                with open(self.sessions_file, 'w') as f:
                    json.dump(sessions_data, f, indent=2)
                
                # Save active session reference
                active_data = {'session_id': self.active_session_id, 'timestamp': time.time()}
                with open(self.active_session_file, 'w') as f:
                    json.dump(active_data, f, indent=2)
                    
        except Exception as e:
            print(f"Error saving timer sessions: {e}")
    
    def start_session(self, task_name: str, project: str = "", category: str = "development", 
                     chronas_session_id: str = None, taskmaster_task_id: str = None) -> str:
        """Start a new timer session"""
        # Stop any existing active session
        if self.active_session_id and self.active_session_id in self.sessions:
            self.stop_session()
        
        session_id = str(uuid.uuid4())
        session = TimerSession(
            session_id=session_id,
            task_name=task_name,
            project=project,
            category=category,
            start_time=time.time(),
            state=TimerState.RUNNING.value,
            chronas_session_id=chronas_session_id,
            taskmaster_task_id=taskmaster_task_id,
            last_heartbeat=time.time()
        )
        
        self.sessions[session_id] = session
        self.active_session_id = session_id
        self._save_sessions()
        
        print(f"⏱️ Timer started: {task_name} (Session: {session_id[:8]}...)")
        return session_id
    
    def stop_session(self, notes: str = "") -> Optional[TimerSession]:
        """Stop the active timer session"""
        if not self.active_session_id or self.active_session_id not in self.sessions:
            print("⚠️ No active timer session to stop")
            return None
        
        session = self.sessions[self.active_session_id]
        current_time = time.time()
        
        # Calculate total elapsed time
        if session.state == TimerState.RUNNING.value:
            session.total_elapsed += current_time - session.start_time
        
        session.end_time = current_time
        session.state = TimerState.STOPPED.value
        session.notes = notes
        
        stopped_session = session
        self.active_session_id = None
        self._save_sessions()
        
        elapsed_hours = session.total_elapsed / 3600.0
        print(f"✅ Timer stopped: {session.task_name} ({elapsed_hours:.2f}h elapsed)")
        return stopped_session
    
    def pause_session(self) -> bool:
        """Pause the active timer session"""
        if not self.active_session_id or self.active_session_id not in self.sessions:
            return False
        
        session = self.sessions[self.active_session_id]
        if session.state != TimerState.RUNNING.value:
            return False
        
        current_time = time.time()
        session.total_elapsed += current_time - session.start_time
        session.state = TimerState.PAUSED.value
        
        self._save_sessions()
        print(f"⏸️ Timer paused: {session.task_name}")
        return True
    
    def resume_session(self) -> bool:
        """Resume the paused timer session"""
        if not self.active_session_id or self.active_session_id not in self.sessions:
            return False
        
        session = self.sessions[self.active_session_id]
        if session.state != TimerState.PAUSED.value:
            return False
        
        session.start_time = time.time()  # Reset start time for next interval
        session.state = TimerState.RUNNING.value
        
        self._save_sessions()
        print(f"▶️ Timer resumed: {session.task_name}")
        return True
    
    def get_active_session(self) -> Optional[TimerSession]:
        """Get the current active session"""
        if not self.active_session_id or self.active_session_id not in self.sessions:
            return None
        
        session = self.sessions[self.active_session_id]
        
        # Update elapsed time for running sessions
        if session.state == TimerState.RUNNING.value:
            current_time = time.time()
            session.total_elapsed += current_time - session.start_time
            session.start_time = current_time  # Reset for next calculation
            session.last_heartbeat = current_time
            self._save_sessions()
        
        return session
    
    def get_session_status(self) -> Dict[str, Any]:
        """Get current session status"""
        session = self.get_active_session()
        
        if not session:
            return {
                'active': False,
                'session_id': None,
                'task_name': None,
                'elapsed_seconds': 0,
                'elapsed_hours': 0.0,
                'elapsed_formatted': '00:00:00'
            }
        
        elapsed_seconds = int(session.total_elapsed)
        elapsed_hours = session.total_elapsed / 3600.0
        hours, remainder = divmod(elapsed_seconds, 3600)
        minutes, seconds = divmod(remainder, 60)
        elapsed_formatted = f"{hours:02d}:{minutes:02d}:{seconds:02d}"
        
        return {
            'active': True,
            'session_id': session.session_id,
            'task_name': session.task_name,
            'project': session.project,
            'category': session.category,
            'state': session.state,
            'elapsed_seconds': elapsed_seconds,
            'elapsed_hours': elapsed_hours,
            'elapsed_formatted': elapsed_formatted,
            'start_time': datetime.datetime.fromtimestamp(session.start_time).isoformat() if session.start_time else None,
            'chronas_session_id': session.chronas_session_id,
            'taskmaster_task_id': session.taskmaster_task_id
        }
    
    def sync_with_chronas(self, chronas_session_id: str) -> bool:
        """Sync current session with Chronas"""
        if not self.active_session_id or self.active_session_id not in self.sessions:
            return False
        
        session = self.sessions[self.active_session_id]
        session.chronas_session_id = chronas_session_id
        self._save_sessions()
        return True
    
    def sync_with_taskmaster(self, taskmaster_task_id: str) -> bool:
        """Sync current session with TaskMaster"""
        if not self.active_session_id or self.active_session_id not in self.sessions:
            return False
        
        session = self.sessions[self.active_session_id]
        session.taskmaster_task_id = taskmaster_task_id
        self._save_sessions()
        return True
    
    def get_daily_sessions(self, target_date: datetime.date = None) -> List[TimerSession]:
        """Get all sessions for a specific date"""
        if target_date is None:
            target_date = datetime.date.today()
        
        start_of_day = datetime.datetime.combine(target_date, datetime.time.min).timestamp()
        end_of_day = datetime.datetime.combine(target_date, datetime.time.max).timestamp()
        
        daily_sessions = []
        for session in self.sessions.values():
            if session.start_time and start_of_day <= session.start_time <= end_of_day:
                daily_sessions.append(session)
        
        return sorted(daily_sessions, key=lambda s: s.start_time)
    
    def cleanup_stale_sessions(self, max_age_hours: int = 24):
        """Clean up stale sessions older than max_age_hours"""
        current_time = time.time()
        max_age_seconds = max_age_hours * 3600
        
        stale_sessions = []
        for session_id, session in list(self.sessions.items()):
            if session.last_heartbeat and (current_time - session.last_heartbeat) > max_age_seconds:
                if session.state == TimerState.RUNNING.value:
                    # Auto-stop stale running sessions
                    session.state = TimerState.STOPPED.value
                    session.end_time = session.last_heartbeat
                    stale_sessions.append(session_id)
        
        if stale_sessions:
            self._save_sessions()
            print(f"🧹 Cleaned up {len(stale_sessions)} stale timer sessions")
        
        return stale_sessions

# Global shared timer service instance
_timer_service = None

def get_timer_service(workspace_dir: str = None) -> SharedTimerService:
    """Get or create the global timer service instance"""
    global _timer_service
    if _timer_service is None:
        _timer_service = SharedTimerService(workspace_dir)
    return _timer_service

def main():
    """Command-line interface for timer service"""
    import argparse
    
    parser = argparse.ArgumentParser(description="🧙‍♂️ Shared Timer Service")
    parser.add_argument('--workspace', help='Workspace directory path')
    parser.add_argument('--start', help='Start timer with task name')
    parser.add_argument('--project', help='Project name')
    parser.add_argument('--category', default='development', help='Category name')
    parser.add_argument('--stop', action='store_true', help='Stop current timer')
    parser.add_argument('--pause', action='store_true', help='Pause current timer')
    parser.add_argument('--resume', action='store_true', help='Resume paused timer')
    parser.add_argument('--status', action='store_true', help='Show timer status')
    parser.add_argument('--cleanup', action='store_true', help='Clean up stale sessions')
    parser.add_argument('--notes', help='Notes for session')
    
    args = parser.parse_args()
    
    timer_service = get_timer_service(args.workspace)
    
    if args.start:
        timer_service.start_session(args.start, args.project or "", args.category)
    elif args.stop:
        timer_service.stop_session(args.notes or "")
    elif args.pause:
        timer_service.pause_session()
    elif args.resume:
        timer_service.resume_session()
    elif args.cleanup:
        timer_service.cleanup_stale_sessions()
    elif args.status:
        status = timer_service.get_session_status()
        if status['active']:
            print(f"🏃 Active Timer: {status['task_name']}")
            if status['project']:
                print(f"   Project: {status['project']}")
            print(f"   Category: {status['category']}")
            print(f"   Elapsed: {status['elapsed_formatted']} ({status['elapsed_hours']:.2f}h)")
            print(f"   State: {status['state']}")
        else:
            print("🛑 No active timer")
    else:
        parser.print_help()

if __name__ == "__main__":
    main()