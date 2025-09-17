#!/usr/bin/env python3
"""
Living Dev Agent Template - Universal Console Commentary System
Jerry's legendary debugging documentation transformer (sanitized from Unity)

Execution time: ~45ms for session management operations
Transforms debugging sessions into documented learning adventures
"""

import argparse
import json
import os
import sys
import datetime
import re
from pathlib import Path
from typing import List, Dict, Any, Optional

# Color codes for epic console output
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

# Sacred emojis for debugging adventures
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_MAGIC = "🧙‍♂️"
EMOJI_COMMENT = "💬"
EMOJI_SNAPSHOT = "📸"
EMOJI_FUCK = "🔥"
EMOJI_ACHIEVE = "🏆"

class CommentaryEntry:
    """A single commentary entry linking logs with developer insights"""
    
    def __init__(self, log_message: str, log_type: str, developer_comment: str, 
                 context: str = "", tags: str = "", stack_trace: str = "", 
                 timestamp: str = None):
        self.timestamp = timestamp or datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        self.log_message = log_message
        self.log_type = log_type
        self.developer_comment = developer_comment
        self.context = context
        self.tags = tags
        self.stack_trace = stack_trace
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization"""
        return {
            'timestamp': self.timestamp,
            'log_message': self.log_message,
            'log_type': self.log_type,
            'developer_comment': self.developer_comment,
            'context': self.context,
            'tags': self.tags,
            'stack_trace': self.stack_trace
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'CommentaryEntry':
        """Create from dictionary"""
        return cls(
            log_message=data.get('log_message', ''),
            log_type=data.get('log_type', 'INFO'),
            developer_comment=data.get('developer_comment', ''),
            context=data.get('context', ''),
            tags=data.get('tags', ''),
            stack_trace=data.get('stack_trace', ''),
            timestamp=data.get('timestamp')
        )

class ConsoleCommentarySession:
    """Universal console commentary session management"""
    
    def __init__(self, session_name: str = None, workspace_path: str = "."):
        self.session_name = session_name or f"Session-{datetime.datetime.now().strftime('%H%M%S')}"
        self.workspace_path = Path(workspace_path)
        self.commentaries: List[CommentaryEntry] = []
        self.session_start_time = datetime.datetime.now()
        
        # Create debug directory structure
        self.debug_dir = self.workspace_path / "debug"
        self.debug_dir.mkdir(exist_ok=True)
        
        # Session file paths
        self.session_file = self.debug_dir / f"commentary_session_{self.session_name}.json"
        
        # Load existing session if it exists
        self.load_session()

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

    def add_commentary(self, log_message: str, log_type: str, comment: str, 
                      context: str = "", tags: str = "") -> bool:
        """Add a new commentary entry"""
        try:
            entry = CommentaryEntry(
                log_message=log_message,
                log_type=log_type,
                developer_comment=comment,
                context=context,
                tags=tags
            )
            
            self.commentaries.append(entry)
            self.save_session()
            
            self.log_success(f"Added commentary entry (#{len(self.commentaries)})")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to add commentary: {e}")
            return False

    def add_fuck_moment(self, log_message: str, comment: str, context: str = "") -> bool:
        """Add a FUCK moment commentary with special tagging"""
        return self.add_commentary(
            log_message=log_message,
            log_type="ERROR",
            comment=comment,
            context=context,
            tags="FUCK-Moment,Learning-Opportunity,Debug-Session"
        )

    def add_achievement(self, log_message: str, comment: str, context: str = "") -> bool:
        """Add an achievement commentary"""
        return self.add_commentary(
            log_message=log_message,
            log_type="SUCCESS",
            comment=comment,
            context=context,
            tags="Achievement,Success,Milestone"
        )

    def save_session(self) -> bool:
        """Save current session to disk"""
        try:
            session_data = {
                'session_name': self.session_name,
                'session_start_time': self.session_start_time.isoformat(),
                'commentaries': [entry.to_dict() for entry in self.commentaries]
            }
            
            with open(self.session_file, 'w', encoding='utf-8') as f:
                json.dump(session_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save session: {e}")
            return False

    def load_session(self) -> bool:
        """Load existing session from disk"""
        try:
            if not self.session_file.exists():
                return True  # No existing session, that's fine
            
            with open(self.session_file, 'r', encoding='utf-8') as f:
                session_data = json.load(f)
            
            self.session_name = session_data.get('session_name', self.session_name)
            
            # Load commentaries
            commentaries_data = session_data.get('commentaries', [])
            self.commentaries = [CommentaryEntry.from_dict(data) for data in commentaries_data]
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load existing session: {e}")
            return False

    def export_to_tldl(self) -> str:
        """Export session to TLDL format"""
        session_duration = datetime.datetime.now() - self.session_start_time
        
        # Count different types of entries
        fuck_moments = len([c for c in self.commentaries if 'FUCK-Moment' in c.tags])
        achievements = len([c for c in self.commentaries if 'Achievement' in c.tags])
        errors = len([c for c in self.commentaries if c.log_type.upper() in ['ERROR', 'EXCEPTION']])
        warnings = len([c for c in self.commentaries if c.log_type.upper() == 'WARNING'])
        
        tldl_content = f"""# Console Commentary Session: {self.session_name}

**Entry ID:** TLDL-{datetime.datetime.now().strftime('%Y-%m-%d')}-ConsoleCommentary-{self.session_name}  
**Author:** Developer (Console Commentary System)  
**Context:** Debug session with universal console commentary and documentation  
**Summary:** Debugging session with {len(self.commentaries)} annotated console events over {session_duration}

---

## 🎯 Objective

Document and preserve debugging insights using Jerry's legendary console commentary system. Transform "FUCK!!!" moments into structured learning opportunities.

## 🔍 Discovery

### Session Statistics
- **Total Commentary Entries:** {len(self.commentaries)}
- **Session Duration:** {session_duration}
- **FUCK Moments:** {fuck_moments} (learning opportunities documented)
- **Achievements:** {achievements} (successful breakthroughs preserved)
- **Errors Analyzed:** {errors}
- **Warnings Analyzed:** {warnings}

## ⚡ Actions Taken

### Commentary Collection Process
- Set up console commentary session with name: `{self.session_name}`
- Captured real-time debugging events and developer annotations
- Applied Jerry's revolutionary tagging system for categorization
- Used universal console commentary system (sanitized from Unity version)

### Key Debugging Activities
"""

        # Add commentary entries
        if self.commentaries:
            tldl_content += "\n## 🗨️ Developer Commentary Log\n\n"
            
            for i, entry in enumerate(self.commentaries, 1):
                tldl_content += f"### Entry #{i} - {entry.timestamp}\n\n"
                
                if entry.tags:
                    tldl_content += f"**Tags:** `{entry.tags}`\n\n"
                
                tldl_content += f"**Log Type:** {entry.log_type}\n\n"
                tldl_content += f"**Original Message:**\n```\n{entry.log_message}\n```\n\n"
                tldl_content += f"**Developer Commentary:**\n{entry.developer_comment}\n\n"
                
                if entry.context:
                    tldl_content += f"**Context:** {entry.context}\n\n"
                
                if entry.stack_trace:
                    tldl_content += f"**Stack Trace:**\n```\n{entry.stack_trace}\n```\n\n"
                
                tldl_content += "---\n\n"

        # Add insights section
        tldl_content += """## 🧠 Key Insights

### Jerry's Console Commentary Innovation
- **Universal System**: Sanitized from Unity-specific implementation for cross-platform use
- **Session-based Workflow**: Named sessions with persistent storage and TLDL export
- **Tagging System**: FUCK Moments, Achievements, and custom categories for organization
- **Learning Transformation**: Turn debugging disasters into documented victories

### Technical Implementation
- **JSON-based Storage**: Persistent session data with commentary preservation
- **Cross-platform Design**: Python-based system works across different development environments
- **Template Integration**: Seamlessly integrates with Living Dev Agent workflow

## 🚧 Challenges Encountered

### System Design Challenges
- **Unity Sanitization**: Removed Unity-specific reflection and EditorGUI dependencies
- **Universal Compatibility**: Ensured system works across different development platforms
- **Session Persistence**: Implemented reliable JSON-based storage for commentary data

## 📋 Next Steps

- [ ] Test console commentary system across different project types
- [ ] Validate TLDL export format and readability
- [ ] Document best practices for commentary tagging
- [ ] Integrate with CI/CD pipelines for automated documentation

## 🔗 Related Links

- [Original Unity Console Commentary System](ConsoleCommentaryWindow.cs)
- [Living Dev Agent Template](README.md)
- [Jerry's Debugging Revolution TLDL](TLDL-2025-01-15-ConsoleCommentary-CodeSnapshot-DebugRevolution.md)

---

## TLDL Metadata
**Tags**: #console-commentary #debugging #jerry-innovation #universal-system #documentation  
**Complexity**: Medium  
**Impact**: High  
**Team Members**: Developer, Console Commentary System  
**Duration**: {session_duration}  
**Related Epic**: Universal Debugging Documentation System  

---

**Created**: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}  
**Last Updated**: {datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S UTC')}  
**Status**: Complete  

*This debugging session was documented using Jerry's legendary Console Commentary System, now available in universal format for all development platforms.* 🧙‍♂️⚡💬
"""
        
        return tldl_content

    def export_session(self, export_format: str = "tldl") -> str:
        """Export session in specified format"""
        if export_format.lower() == "tldl":
            content = self.export_to_tldl()
            
            # Save to TLDL directory
            tldl_dir = self.workspace_path / "TLDL" / "entries"
            tldl_dir.mkdir(parents=True, exist_ok=True)
            
            filename = f"TLDL-{datetime.datetime.now().strftime('%Y-%m-%d')}-ConsoleCommentary-{self.session_name}.md"
            tldl_file = tldl_dir / filename
            
            with open(tldl_file, 'w', encoding='utf-8') as f:
                f.write(content)
            
            self.log_success(f"Exported session to TLDL: {tldl_file}")
            return str(tldl_file)
        
        else:
            raise ValueError(f"Unsupported export format: {export_format}")

    def list_commentaries(self) -> None:
        """List all commentaries in the session"""
        if not self.commentaries:
            self.log_info("No commentaries in current session")
            return
        
        print(f"\n{Colors.HEADER}📚 Commentary Session: {self.session_name}{Colors.ENDC}")
        print(f"Started: {self.session_start_time.strftime('%Y-%m-%d %H:%M:%S')}")
        print(f"Entries: {len(self.commentaries)}")
        print("=" * 60)
        
        for i, entry in enumerate(self.commentaries, 1):
            tags_display = f" [{entry.tags}]" if entry.tags else ""
            print(f"{i:2d}. {entry.timestamp} - {entry.log_type}{tags_display}")
            print(f"    Comment: {entry.developer_comment[:80]}{'...' if len(entry.developer_comment) > 80 else ''}")
            print()

    def get_session_stats(self) -> Dict[str, Any]:
        """Get session statistics"""
        stats = {
            'session_name': self.session_name,
            'total_entries': len(self.commentaries),
            'session_duration': str(datetime.datetime.now() - self.session_start_time),
            'fuck_moments': len([c for c in self.commentaries if 'FUCK-Moment' in c.tags]),
            'achievements': len([c for c in self.commentaries if 'Achievement' in c.tags]),
            'errors': len([c for c in self.commentaries if c.log_type.upper() in ['ERROR', 'EXCEPTION']]),
            'warnings': len([c for c in self.commentaries if c.log_type.upper() == 'WARNING']),
            'tags_used': list(set(tag.strip() for entry in self.commentaries for tag in entry.tags.split(',') if tag.strip()))
        }
        return stats


def main():
    """Main console commentary interface"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_MAGIC} Universal Console Commentary System {EMOJI_COMMENT}",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 console_commentary.py --session debug-session-1 --list
  python3 console_commentary.py --add-comment "Error in validation" "Found the issue - missing null check"
  python3 console_commentary.py --fuck-moment "Validation failed" "Spent 2 hours debugging this - turns out it was a typo"
  python3 console_commentary.py --achievement "All tests pass" "Finally got the validation system working!"
  python3 console_commentary.py --export tldl
        """
    )
    
    parser.add_argument('--session', default=None, help='Session name (auto-generated if not provided)')
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Actions
    parser.add_argument('--list', action='store_true', help='List all commentaries in session')
    parser.add_argument('--stats', action='store_true', help='Show session statistics')
    parser.add_argument('--export', choices=['tldl'], help='Export session in specified format')
    
    # Add commentary
    parser.add_argument('--add-comment', nargs=2, metavar=('LOG_MESSAGE', 'COMMENT'), 
                       help='Add general commentary')
    parser.add_argument('--fuck-moment', nargs=2, metavar=('LOG_MESSAGE', 'COMMENT'),
                       help='Add FUCK moment commentary')
    parser.add_argument('--achievement', nargs=2, metavar=('LOG_MESSAGE', 'COMMENT'),
                       help='Add achievement commentary')
    
    # Additional options
    parser.add_argument('--log-type', default='INFO', help='Log type for --add-comment')
    parser.add_argument('--context', default='', help='Additional context for commentary')
    parser.add_argument('--tags', default='', help='Custom tags (comma-separated)')
    
    args = parser.parse_args()
    
    try:
        # Create session
        session = ConsoleCommentarySession(
            session_name=args.session,
            workspace_path=args.workspace
        )
        
        # Execute requested action
        if args.list:
            session.list_commentaries()
        
        elif args.stats:
            stats = session.get_session_stats()
            print(f"\n{Colors.HEADER}📊 Session Statistics{Colors.ENDC}")
            for key, value in stats.items():
                print(f"{key.replace('_', ' ').title()}: {value}")
        
        elif args.export:
            exported_file = session.export_session(args.export)
            session.log_success(f"Session exported to: {exported_file}")
        
        elif args.add_comment:
            log_message, comment = args.add_comment
            success = session.add_commentary(
                log_message=log_message,
                log_type=args.log_type,
                comment=comment,
                context=args.context,
                tags=args.tags
            )
            if not success:
                sys.exit(1)
        
        elif args.fuck_moment:
            log_message, comment = args.fuck_moment
            success = session.add_fuck_moment(log_message, comment, args.context)
            if not success:
                sys.exit(1)
            session.log_info(f"FUCK moment documented! {EMOJI_FUCK}", EMOJI_FUCK)
        
        elif args.achievement:
            log_message, comment = args.achievement
            success = session.add_achievement(log_message, comment, args.context)
            if not success:
                sys.exit(1)
            session.log_success(f"Achievement unlocked! {EMOJI_ACHIEVE}", EMOJI_ACHIEVE)
        
        else:
            # No action specified, show current session info
            stats = session.get_session_stats()
            session.log_info(f"Console Commentary Session: {session.session_name}")
            session.log_info(f"Entries: {stats['total_entries']}, Duration: {stats['session_duration']}")
            session.log_info("Use --help to see available commands")
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} Commentary session interrupted{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} Commentary system error: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
