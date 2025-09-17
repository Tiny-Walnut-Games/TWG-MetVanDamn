#!/usr/bin/env python3
"""
Pet Events - Record Streak Events
Record streak monitoring events in the pet events log system
"""

import argparse
import json
import uuid
from datetime import datetime
from pathlib import Path

def generate_pet_id(event_type: str = "archivist") -> str:
    """Generate consistent pet ID for streak events"""
    # Use deterministic pet ID for streak monitoring
    if event_type == "streak":
        return "archivist-streak-monitor-001"
    return f"{event_type}-{str(uuid.uuid4())[:8]}"

def create_streak_event(analysis: dict, event_type: str = "STREAK_HEALTH_CHECK") -> dict:
    """Create pet event from streak analysis"""
    
    streak_status = analysis['streak_status']
    implementation_ratio = analysis['implementation_ratio']
    
    # Determine specific event type based on status
    if streak_status == 'healthy':
        event_type = 'STREAK_HEALTHY'
    elif streak_status == 'concerning':
        event_type = 'STREAK_CONCERNING'
    else:
        event_type = 'STREAK_CRITICAL'
    
    # Create pet event data
    pet_event = {
        "timestamp": datetime.now().isoformat() + "Z",
        "event_type": event_type,
        "pet_id": generate_pet_id("streak"),
        "data": {
            "evaluation_window": analysis['evaluation_window'],
            "implementation_ratio": implementation_ratio,
            "total_charters": analysis['total_charters'],
            "implementations_completed": analysis['implementations_completed'],
            "threshold_met": streak_status,
            "streak_status": streak_status,
            "analysis_date": analysis['analysis_date']
        }
    }
    
    # Add status-specific data
    if event_type == 'STREAK_HEALTHY':
        pet_event["data"].update({
            "xp_bonus_awarded": 25,
            "special_ability_unlocked": "enhanced_pattern_recognition"
        })
    elif event_type == 'STREAK_CONCERNING':
        pet_event["data"].update({
            "warning_generated": True,
            "stalled_charters_count": len(analysis.get('stalled_charters', []))
        })
    elif event_type == 'STREAK_CRITICAL':
        pet_event["data"].update({
            "alert_generated": True,
            "mandatory_audit_triggered": True,
            "stalled_charters": analysis.get('stalled_charters', [])
        })
    
    return pet_event

def append_to_pet_log(event: dict, log_file: str):
    """Append event to pet events log"""
    log_path = Path(log_file)
    
    # Create log file if it doesn't exist
    if not log_path.exists():
        log_path.parent.mkdir(parents=True, exist_ok=True)
        log_path.touch()
    
    # Append event as JSON line
    with open(log_path, 'a') as f:
        f.write(json.dumps(event) + '\n')

def main():
    parser = argparse.ArgumentParser(description="Record streak events in pet log")
    parser.add_argument('--analysis-file', required=True, help='Path to streak analysis JSON')
    parser.add_argument('--pet-events-log', required=True, help='Path to pet events log file')
    
    args = parser.parse_args()
    
    # Load streak analysis
    try:
        with open(args.analysis_file, 'r') as f:
            analysis = json.load(f)
    except Exception as e:
        print(f"Error: Could not load analysis file: {e}")
        return 1
    
    # Create pet event
    pet_event = create_streak_event(analysis)
    
    # Log the event
    try:
        append_to_pet_log(pet_event, args.pet_events_log)
        print(f"✅ Recorded {pet_event['event_type']} event for pet {pet_event['pet_id']}")
        print(f"📊 Status: {analysis['streak_status'].upper()}, Ratio: {analysis['implementation_ratio']:.3f}")
    except Exception as e:
        print(f"Error: Could not write to pet log: {e}")
        return 1
    
    return 0

if __name__ == '__main__':
    exit(main())