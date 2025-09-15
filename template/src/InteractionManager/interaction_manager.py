#!/usr/bin/env python3
"""
Living Dev Agent Template - Universal Interactive Components
Jerry's input handling and UI interaction systems (sanitized from Unity)

Execution time: ~25ms for typical operations
Cross-platform input handling and UI component systems
"""

import argparse
import json
import os
import sys
import datetime
import math
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple, Callable
from dataclasses import dataclass, asdict
from enum import Enum

# Color codes for epic interaction management
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

# Sacred emojis for interaction mastery
EMOJI_SUCCESS = "✅"
EMOJI_WARNING = "⚠️"
EMOJI_ERROR = "❌"
EMOJI_INFO = "🔍"
EMOJI_INPUT = "🎮"
EMOJI_TOUCH = "👆"

class InputType(Enum):
    """Input method types"""
    KEYBOARD = "keyboard"
    MOUSE = "mouse"
    TOUCH = "touch"
    GAMEPAD = "gamepad"

class TouchPhase(Enum):
    """Touch interaction phases"""
    BEGAN = "began"
    MOVED = "moved" 
    ENDED = "ended"
    CANCELED = "canceled"

@dataclass
class InputEvent:
    """Universal input event data"""
    event_type: InputType
    timestamp: float
    position: Tuple[float, float] = (0.0, 0.0)
    delta: Tuple[float, float] = (0.0, 0.0)
    button_id: int = 0
    key_code: str = ""
    phase: TouchPhase = TouchPhase.BEGAN
    properties: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.properties is None:
            self.properties = {}
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'event_type': self.event_type.value,
            'timestamp': self.timestamp,
            'position': self.position,
            'delta': self.delta,
            'button_id': self.button_id,
            'key_code': self.key_code,
            'phase': self.phase.value,
            'properties': self.properties
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'InputEvent':
        """Deserialize from dictionary"""
        return cls(
            event_type=InputType(data['event_type']),
            timestamp=data['timestamp'],
            position=tuple(data.get('position', (0.0, 0.0))),
            delta=tuple(data.get('delta', (0.0, 0.0))),
            button_id=data.get('button_id', 0),
            key_code=data.get('key_code', ''),
            phase=TouchPhase(data.get('phase', TouchPhase.BEGAN.value)),
            properties=data.get('properties', {})
        )

@dataclass
class InteractionComponent:
    """Universal interactive component"""
    component_id: str
    name: str
    bounds: Tuple[float, float, float, float]  # x, y, width, height
    rotation_speed: float = 100.0
    mouse_sensitivity: float = 1.0
    is_active: bool = True
    properties: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.properties is None:
            self.properties = {}
    
    def contains_point(self, x: float, y: float) -> bool:
        """Check if point is within component bounds"""
        bounds_x, bounds_y, bounds_w, bounds_h = self.bounds
        return (bounds_x <= x <= bounds_x + bounds_w and 
                bounds_y <= y <= bounds_y + bounds_h)
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'component_id': self.component_id,
            'name': self.name,
            'bounds': self.bounds,
            'rotation_speed': self.rotation_speed,
            'mouse_sensitivity': self.mouse_sensitivity,
            'is_active': self.is_active,
            'properties': self.properties
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'InteractionComponent':
        """Deserialize from dictionary"""
        return cls(
            component_id=data['component_id'],
            name=data['name'],
            bounds=tuple(data['bounds']),
            rotation_speed=data.get('rotation_speed', 100.0),
            mouse_sensitivity=data.get('mouse_sensitivity', 1.0),
            is_active=data.get('is_active', True),
            properties=data.get('properties', {})
        )

@dataclass
class InteractionSession:
    """Interaction session tracking"""
    session_id: str
    start_time: float
    component_id: str
    input_type: InputType
    start_position: Tuple[float, float]
    current_position: Tuple[float, float] = (0.0, 0.0)
    total_delta: Tuple[float, float] = (0.0, 0.0)
    is_active: bool = True
    events: List[InputEvent] = None
    
    def __post_init__(self):
        if self.events is None:
            self.events = []
    
    def add_event(self, event: InputEvent):
        """Add an input event to this session"""
        self.events.append(event)
        self.current_position = event.position
        
        # Update total delta
        delta_x = self.current_position[0] - self.start_position[0]
        delta_y = self.current_position[1] - self.start_position[1]
        self.total_delta = (delta_x, delta_y)
    
    def get_session_duration(self) -> float:
        """Get session duration in seconds"""
        if self.events:
            return self.events[-1].timestamp - self.start_time
        return 0.0
    
    def to_dict(self) -> Dict[str, Any]:
        """Serialize to dictionary"""
        return {
            'session_id': self.session_id,
            'start_time': self.start_time,
            'component_id': self.component_id,
            'input_type': self.input_type.value,
            'start_position': self.start_position,
            'current_position': self.current_position,
            'total_delta': self.total_delta,
            'is_active': self.is_active,
            'events': [event.to_dict() for event in self.events]
        }
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'InteractionSession':
        """Deserialize from dictionary"""
        session = cls(
            session_id=data['session_id'],
            start_time=data['start_time'],
            component_id=data['component_id'],
            input_type=InputType(data['input_type']),
            start_position=tuple(data['start_position']),
            current_position=tuple(data.get('current_position', (0.0, 0.0))),
            total_delta=tuple(data.get('total_delta', (0.0, 0.0))),
            is_active=data.get('is_active', True)
        )
        
        # Load events
        for event_data in data.get('events', []):
            session.events.append(InputEvent.from_dict(event_data))
        
        return session

class UniversalInteractionManager:
    """Jerry's interaction management system (universal version)"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
        self.components: Dict[str, InteractionComponent] = {}
        self.active_sessions: Dict[str, InteractionSession] = {}
        self.completed_sessions: List[InteractionSession] = []
        
        # Create interaction directories
        self.interactions_dir = self.workspace_path / "interactions"
        self.interactions_dir.mkdir(exist_ok=True)
        
        self.sessions_dir = self.interactions_dir / "sessions"
        self.sessions_dir.mkdir(exist_ok=True)
        
        # Data files
        self.components_file = self.interactions_dir / "components.json"
        self.sessions_file = self.interactions_dir / "interaction_sessions.json"
        
        # Load existing data
        self.load_components()
        self.load_sessions()

    def log_info(self, message: str, emoji: str = EMOJI_INFO):
        """Log informational message with epic styling"""
        print(f"{Colors.OKCYAN}{emoji} [INFO]{Colors.ENDC} {message}")

    def log_success(self, message: str, emoji: str = EMOJI_SUCCESS):
        """Log success message with interaction flair"""
        print(f"{Colors.OKGREEN}{emoji} [SUCCESS]{Colors.ENDC} {message}")

    def log_warning(self, message: str, emoji: str = EMOJI_WARNING):
        """Log warning message"""
        print(f"{Colors.WARNING}{emoji} [WARNING]{Colors.ENDC} {message}")

    def log_error(self, message: str, emoji: str = EMOJI_ERROR):
        """Log error message"""
        print(f"{Colors.FAIL}{emoji} [ERROR]{Colors.ENDC} {message}")

    def create_component(self, component_id: str, name: str, 
                        bounds: Tuple[float, float, float, float],
                        rotation_speed: float = 100.0, mouse_sensitivity: float = 1.0) -> bool:
        """Create a new interactive component"""
        try:
            if component_id in self.components:
                self.log_warning(f"Component '{component_id}' already exists")
                return False
            
            component = InteractionComponent(
                component_id=component_id,
                name=name,
                bounds=bounds,
                rotation_speed=rotation_speed,
                mouse_sensitivity=mouse_sensitivity
            )
            
            self.components[component_id] = component
            self.save_components()
            
            self.log_success(f"Created component: {name} ({component_id})", EMOJI_INPUT)
            return True
            
        except Exception as e:
            self.log_error(f"Failed to create component: {e}")
            return False

    def start_interaction(self, component_id: str, input_type: InputType,
                         position: Tuple[float, float], timestamp: float = None) -> Optional[str]:
        """Start an interaction session"""
        try:
            if component_id not in self.components:
                self.log_error(f"Component '{component_id}' not found")
                return None
            
            component = self.components[component_id]
            if not component.is_active:
                self.log_warning(f"Component '{component_id}' is not active")
                return None
            
            # Check if position is within component bounds
            if not component.contains_point(position[0], position[1]):
                self.log_warning(f"Position {position} is outside component bounds")
                return None
            
            # Generate session ID
            if timestamp is None:
                timestamp = datetime.datetime.now().timestamp()
            
            session_id = f"{component_id}_{input_type.value}_{int(timestamp)}"
            
            # Create session
            session = InteractionSession(
                session_id=session_id,
                start_time=timestamp,
                component_id=component_id,
                input_type=input_type,
                start_position=position
            )
            
            # Create initial event
            initial_event = InputEvent(
                event_type=input_type,
                timestamp=timestamp,
                position=position,
                phase=TouchPhase.BEGAN
            )
            session.add_event(initial_event)
            
            self.active_sessions[session_id] = session
            
            self.log_info(f"Started {input_type.value} interaction on {component.name}")
            return session_id
            
        except Exception as e:
            self.log_error(f"Failed to start interaction: {e}")
            return None

    def update_interaction(self, session_id: str, position: Tuple[float, float],
                          timestamp: float = None) -> bool:
        """Update an active interaction session"""
        try:
            if session_id not in self.active_sessions:
                self.log_warning(f"No active session with ID: {session_id}")
                return False
            
            session = self.active_sessions[session_id]
            component = self.components[session.component_id]
            
            if timestamp is None:
                timestamp = datetime.datetime.now().timestamp()
            
            # Calculate delta from last position
            delta_x = position[0] - session.current_position[0]
            delta_y = position[1] - session.current_position[1]
            
            # Create update event
            update_event = InputEvent(
                event_type=session.input_type,
                timestamp=timestamp,
                position=position,
                delta=(delta_x, delta_y),
                phase=TouchPhase.MOVED
            )
            
            session.add_event(update_event)
            
            # Process the interaction based on input type
            self._process_interaction_update(session, component, update_event)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to update interaction: {e}")
            return False

    def end_interaction(self, session_id: str, position: Tuple[float, float] = None,
                       timestamp: float = None) -> bool:
        """End an active interaction session"""
        try:
            if session_id not in self.active_sessions:
                self.log_warning(f"No active session with ID: {session_id}")
                return False
            
            session = self.active_sessions[session_id]
            
            if timestamp is None:
                timestamp = datetime.datetime.now().timestamp()
            
            if position is None:
                position = session.current_position
            
            # Create end event
            end_event = InputEvent(
                event_type=session.input_type,
                timestamp=timestamp,
                position=position,
                phase=TouchPhase.ENDED
            )
            
            session.add_event(end_event)
            session.is_active = False
            
            # Move to completed sessions
            self.completed_sessions.append(session)
            del self.active_sessions[session_id]
            
            self.save_sessions()
            
            duration = session.get_session_duration()
            self.log_success(f"Ended interaction session (duration: {duration:.2f}s)")
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to end interaction: {e}")
            return False

    def _process_interaction_update(self, session: InteractionSession, 
                                  component: InteractionComponent, event: InputEvent):
        """Process interaction update based on component type and input"""
        try:
            # Calculate rotation amount based on input type and delta
            delta_x, delta_y = event.delta
            
            if session.input_type == InputType.MOUSE:
                # Mouse rotation with sensitivity
                rotation_amount = delta_x * component.mouse_sensitivity
            elif session.input_type == InputType.TOUCH:
                # Touch rotation
                rotation_amount = delta_x
            elif session.input_type == InputType.KEYBOARD:
                # Keyboard rotation (delta would be -1, 0, or 1)
                rotation_amount = delta_x * component.rotation_speed
            else:
                rotation_amount = delta_x
            
            # Store rotation in event properties
            event.properties['rotation_amount'] = rotation_amount
            event.properties['component_rotation_speed'] = component.rotation_speed
            
            # Log significant rotations
            if abs(rotation_amount) > 0.1:
                self.log_info(f"Rotation: {rotation_amount:.2f} degrees", EMOJI_TOUCH)
            
        except Exception as e:
            self.log_warning(f"Failed to process interaction update: {e}")

    def simulate_keyboard_input(self, component_id: str, horizontal_input: float,
                               duration: float = 0.1) -> bool:
        """Simulate keyboard input for rotation"""
        try:
            if abs(horizontal_input) < 0.01:
                return False  # No significant input
            
            timestamp = datetime.datetime.now().timestamp()
            start_position = (0.0, 0.0)  # Keyboard doesn't have position
            
            # Start session
            session_id = self.start_interaction(component_id, InputType.KEYBOARD, start_position, timestamp)
            if not session_id:
                return False
            
            # Create movement event
            delta_position = (horizontal_input, 0.0)
            self.update_interaction(session_id, delta_position, timestamp + duration)
            
            # End session
            self.end_interaction(session_id, delta_position, timestamp + duration)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to simulate keyboard input: {e}")
            return False

    def simulate_mouse_drag(self, component_id: str, start_pos: Tuple[float, float],
                           end_pos: Tuple[float, float], steps: int = 10) -> bool:
        """Simulate mouse drag interaction"""
        try:
            timestamp = datetime.datetime.now().timestamp()
            
            # Start session
            session_id = self.start_interaction(component_id, InputType.MOUSE, start_pos, timestamp)
            if not session_id:
                return False
            
            # Calculate step increments
            step_x = (end_pos[0] - start_pos[0]) / steps
            step_y = (end_pos[1] - start_pos[1]) / steps
            
            # Simulate dragging in steps
            for i in range(1, steps + 1):
                current_x = start_pos[0] + (step_x * i)
                current_y = start_pos[1] + (step_y * i)
                step_timestamp = timestamp + (i * 0.01)  # 10ms per step
                
                self.update_interaction(session_id, (current_x, current_y), step_timestamp)
            
            # End session
            self.end_interaction(session_id, end_pos, timestamp + (steps * 0.01))
            
            self.log_success(f"Simulated mouse drag from {start_pos} to {end_pos}")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to simulate mouse drag: {e}")
            return False

    def get_component_stats(self, component_id: str) -> Optional[Dict[str, Any]]:
        """Get statistics for a component"""
        try:
            if component_id not in self.components:
                return None
            
            component = self.components[component_id]
            
            # Count sessions for this component
            component_sessions = [s for s in self.completed_sessions if s.component_id == component_id]
            active_component_sessions = [s for s in self.active_sessions.values() if s.component_id == component_id]
            
            # Calculate statistics
            total_sessions = len(component_sessions)
            total_events = sum(len(s.events) for s in component_sessions)
            
            input_type_counts = {}
            total_duration = 0.0
            
            for session in component_sessions:
                input_type = session.input_type.value
                input_type_counts[input_type] = input_type_counts.get(input_type, 0) + 1
                total_duration += session.get_session_duration()
            
            average_duration = total_duration / total_sessions if total_sessions > 0 else 0.0
            
            return {
                'component': {
                    'id': component.component_id,
                    'name': component.name,
                    'bounds': component.bounds,
                    'is_active': component.is_active,
                    'rotation_speed': component.rotation_speed,
                    'mouse_sensitivity': component.mouse_sensitivity
                },
                'usage_stats': {
                    'total_sessions': total_sessions,
                    'active_sessions': len(active_component_sessions),
                    'total_events': total_events,
                    'total_duration': total_duration,
                    'average_duration': average_duration,
                    'input_type_counts': input_type_counts
                }
            }
            
        except Exception as e:
            self.log_error(f"Failed to get component stats: {e}")
            return None

    def export_session_data(self, component_id: str = None, output_path: str = None) -> bool:
        """Export session data to JSON"""
        try:
            if output_path is None:
                timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
                output_path = f"interaction_data_{timestamp}.json"
            
            # Filter sessions if component_id specified
            if component_id:
                sessions_to_export = [s for s in self.completed_sessions if s.component_id == component_id]
                active_to_export = {k: v for k, v in self.active_sessions.items() if v.component_id == component_id}
            else:
                sessions_to_export = self.completed_sessions
                active_to_export = self.active_sessions
            
            export_data = {
                'export_timestamp': datetime.datetime.now().isoformat(),
                'component_filter': component_id,
                'components': {cid: comp.to_dict() for cid, comp in self.components.items()},
                'completed_sessions': [session.to_dict() for session in sessions_to_export],
                'active_sessions': {sid: session.to_dict() for sid, session in active_to_export.items()},
                'summary': {
                    'total_components': len(self.components),
                    'completed_sessions_count': len(sessions_to_export),
                    'active_sessions_count': len(active_to_export)
                }
            }
            
            with open(output_path, 'w', encoding='utf-8') as f:
                json.dump(export_data, f, indent=2, ensure_ascii=False)
            
            self.log_success(f"Exported interaction data: {output_path}")
            return True
            
        except Exception as e:
            self.log_error(f"Failed to export session data: {e}")
            return False

    def save_components(self) -> bool:
        """Save components to file"""
        try:
            components_data = {
                'version': '1.0',
                'last_updated': datetime.datetime.now().isoformat(),
                'components': {cid: comp.to_dict() for cid, comp in self.components.items()}
            }
            
            with open(self.components_file, 'w', encoding='utf-8') as f:
                json.dump(components_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save components: {e}")
            return False

    def load_components(self) -> bool:
        """Load components from file"""
        try:
            if not self.components_file.exists():
                return True
            
            with open(self.components_file, 'r', encoding='utf-8') as f:
                components_data = json.load(f)
            
            self.components = {}
            for cid, comp_data in components_data.get('components', {}).items():
                self.components[cid] = InteractionComponent.from_dict(comp_data)
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load components: {e}")
            return False

    def save_sessions(self) -> bool:
        """Save session data to file"""
        try:
            sessions_data = {
                'version': '1.0',
                'last_updated': datetime.datetime.now().isoformat(),
                'completed_sessions': [session.to_dict() for session in self.completed_sessions],
                'active_sessions': {sid: session.to_dict() for sid, session in self.active_sessions.items()}
            }
            
            with open(self.sessions_file, 'w', encoding='utf-8') as f:
                json.dump(sessions_data, f, indent=2, ensure_ascii=False)
            
            return True
            
        except Exception as e:
            self.log_error(f"Failed to save sessions: {e}")
            return False

    def load_sessions(self) -> bool:
        """Load session data from file"""
        try:
            if not self.sessions_file.exists():
                return True
            
            with open(self.sessions_file, 'r', encoding='utf-8') as f:
                sessions_data = json.load(f)
            
            # Load completed sessions
            self.completed_sessions = []
            for session_data in sessions_data.get('completed_sessions', []):
                self.completed_sessions.append(InteractionSession.from_dict(session_data))
            
            # Load active sessions (usually empty on startup)
            self.active_sessions = {}
            for sid, session_data in sessions_data.get('active_sessions', {}).items():
                self.active_sessions[sid] = InteractionSession.from_dict(session_data)
            
            return True
            
        except Exception as e:
            self.log_warning(f"Could not load sessions: {e}")
            return False


def main():
    """Main interaction manager interface"""
    parser = argparse.ArgumentParser(
        description=f"{EMOJI_INPUT} Universal Interaction Manager",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python3 interaction_manager.py --create-component "obj1" "Rotatable Object" 100 100 200 150
  python3 interaction_manager.py --simulate-keyboard "obj1" 1.0 --duration 0.5
  python3 interaction_manager.py --simulate-mouse-drag "obj1" 150 125 250 125
  python3 interaction_manager.py --component-stats "obj1"
  python3 interaction_manager.py --export-data interaction_export.json
        """
    )
    
    parser.add_argument('--workspace', default='.', help='Workspace directory path')
    
    # Component management
    parser.add_argument('--create-component', nargs=6, 
                       metavar=('ID', 'NAME', 'X', 'Y', 'WIDTH', 'HEIGHT'),
                       help='Create interactive component with bounds')
    parser.add_argument('--rotation-speed', type=float, default=100.0,
                       help='Rotation speed for component')
    parser.add_argument('--mouse-sensitivity', type=float, default=1.0,
                       help='Mouse sensitivity for component')
    
    # Interaction simulation
    parser.add_argument('--simulate-keyboard', nargs=2, metavar=('COMPONENT_ID', 'INPUT'),
                       help='Simulate keyboard input (horizontal input -1 to 1)')
    parser.add_argument('--duration', type=float, default=0.1,
                       help='Duration for keyboard input simulation')
    
    parser.add_argument('--simulate-mouse-drag', nargs=5, 
                       metavar=('COMPONENT_ID', 'START_X', 'START_Y', 'END_X', 'END_Y'),
                       help='Simulate mouse drag interaction')
    parser.add_argument('--drag-steps', type=int, default=10,
                       help='Number of steps for mouse drag simulation')
    
    # Information and analysis
    parser.add_argument('--list-components', action='store_true',
                       help='List all components')
    parser.add_argument('--component-stats', help='Get statistics for component')
    parser.add_argument('--export-data', help='Export interaction data to JSON file')
    parser.add_argument('--filter-component', help='Filter export by component ID')
    
    args = parser.parse_args()
    
    try:
        # Create manager instance
        manager = UniversalInteractionManager(workspace_path=args.workspace)
        
        # Handle component creation
        if args.create_component:
            comp_id, name, x, y, width, height = args.create_component
            bounds = (float(x), float(y), float(width), float(height))
            manager.create_component(comp_id, name, bounds, args.rotation_speed, args.mouse_sensitivity)
        
        # Handle keyboard simulation
        elif args.simulate_keyboard:
            component_id, horizontal_input = args.simulate_keyboard
            horizontal_value = float(horizontal_input)
            manager.simulate_keyboard_input(component_id, horizontal_value, args.duration)
        
        # Handle mouse drag simulation
        elif args.simulate_mouse_drag:
            component_id, start_x, start_y, end_x, end_y = args.simulate_mouse_drag
            start_pos = (float(start_x), float(start_y))
            end_pos = (float(end_x), float(end_y))
            manager.simulate_mouse_drag(component_id, start_pos, end_pos, args.drag_steps)
        
        # Handle information requests
        elif args.list_components:
            if manager.components:
                print(f"\n{Colors.HEADER}🎮 Interactive Components{Colors.ENDC}")
                for comp_id, component in manager.components.items():
                    status = "Active" if component.is_active else "Inactive"
                    print(f"  {comp_id}: {component.name} ({status})")
                    print(f"    Bounds: {component.bounds}")
                    print(f"    Rotation Speed: {component.rotation_speed}")
                    print(f"    Mouse Sensitivity: {component.mouse_sensitivity}")
            else:
                manager.log_info("No components found")
        
        elif args.component_stats:
            stats = manager.get_component_stats(args.component_stats)
            if stats:
                print(f"\n{Colors.HEADER}📊 Component Statistics{Colors.ENDC}")
                comp = stats['component']
                usage = stats['usage_stats']
                
                print(f"Component: {comp['name']} ({comp['id']})")
                print(f"Status: {'Active' if comp['is_active'] else 'Inactive'}")
                print(f"Bounds: {comp['bounds']}")
                print(f"Settings: Speed={comp['rotation_speed']}, Sensitivity={comp['mouse_sensitivity']}")
                print()
                print(f"Usage Statistics:")
                print(f"  Total Sessions: {usage['total_sessions']}")
                print(f"  Active Sessions: {usage['active_sessions']}")
                print(f"  Total Events: {usage['total_events']}")
                print(f"  Total Duration: {usage['total_duration']:.2f}s")
                print(f"  Average Duration: {usage['average_duration']:.2f}s")
                
                if usage['input_type_counts']:
                    print(f"  Input Types:")
                    for input_type, count in usage['input_type_counts'].items():
                        print(f"    {input_type}: {count}")
            else:
                manager.log_error(f"Component '{args.component_stats}' not found")
        
        elif args.export_data:
            manager.export_session_data(args.filter_component, args.export_data)
        
        else:
            # No action specified, show status
            manager.log_info(f"Components: {len(manager.components)}")
            manager.log_info(f"Active sessions: {len(manager.active_sessions)}")
            manager.log_info(f"Completed sessions: {len(manager.completed_sessions)}")
            manager.log_info("Use --help to see available commands")
        
    except KeyboardInterrupt:
        print(f"\n{Colors.WARNING}{EMOJI_WARNING} Interaction manager interrupted{Colors.ENDC}")
        sys.exit(1)
    except Exception as e:
        print(f"{Colors.FAIL}{EMOJI_ERROR} Interaction manager error: {e}{Colors.ENDC}")
        sys.exit(1)


if __name__ == "__main__":
    main()
