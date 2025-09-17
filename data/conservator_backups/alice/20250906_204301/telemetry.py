#!/usr/bin/env python3
"""
Development Telemetry - Sleep & Energy Tracking Extension

NON-BREAKING extension to provide sleep and energy telemetry for
cognitive safety monitoring. Designed to integrate with existing
telemetry systems without disrupting current functionality.

🧙‍♂️ "Track not just the code, but the coder - 
    for both contribute to the quality of creation." - Bootstrap Sentinel
"""

from __future__ import annotations
import json
import datetime
import time
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, Any, Optional, List


class DevelopmentTelemetry:
    """
    Development Telemetry System - Tracks developer state and environment
    
    NON-BREAKING telemetry system that can be optionally integrated
    with existing development workflows.
    """
    
    def __init__(self, telemetry_path: str = "data/dev_telemetry.json"):
        self.telemetry_path = Path(telemetry_path)
        self.telemetry_path.parent.mkdir(parents=True, exist_ok=True)
        self.data = self._load_telemetry()
    
    def _load_telemetry(self) -> Dict[str, Any]:
        """Load telemetry data or create new structure"""
        if self.telemetry_path.exists():
            try:
                with open(self.telemetry_path, 'r') as f:
                    return json.load(f)
            except (json.JSONDecodeError, FileNotFoundError):
                pass
        
        # Default telemetry structure
        return {
            "schema_version": "1.0.0",
            "created_at": datetime.datetime.now().isoformat(),
            "last_updated": datetime.datetime.now().isoformat(),
            "sleep_tracking": {
                "enabled": True,
                "entries": []
            },
            "energy_tracking": {
                "enabled": True,
                "entries": []
            },
            "flags": {
                "low_sleep": False,
                "low_energy": False,
                "cognitive_sensitivity": False
            },
            "daily_stats": {},
            # NON-BREAKING: Optional field for existing systems
            "hours_slept_last_night": None
        }
    
    def _save_telemetry(self):
        """Save telemetry data to disk"""
        self.data["last_updated"] = datetime.datetime.now().isoformat()
        with open(self.telemetry_path, 'w') as f:
            json.dump(self.data, f, indent=2, ensure_ascii=False)
    
    def update_sleep(self, hours: float, quality: str = "unknown", 
                    notes: str = "") -> Dict[str, Any]:
        """
        Update sleep tracking information (NON-BREAKING extension)
        
        Args:
            hours: Hours of sleep (0.0 - 24.0)
            quality: Sleep quality ("poor", "fair", "good", "excellent")
            notes: Optional notes about sleep
            
        Returns:
            Sleep entry record
        """
        if not 0.0 <= hours <= 24.0:
            raise ValueError("Hours must be between 0.0 and 24.0")
        
        timestamp = datetime.datetime.now().isoformat()
        date = datetime.date.today().isoformat()
        
        # NON-BREAKING: Update the optional field for compatibility
        self.data["hours_slept_last_night"] = hours if hours > 0 else None
        
        # Create sleep entry
        sleep_entry = {
            "date": date,
            "timestamp": timestamp,
            "hours": hours,
            "quality": quality,
            "notes": notes,
            "low_sleep_flag": hours < 4.0
        }
        
        # Add to tracking
        self.data["sleep_tracking"]["entries"].append(sleep_entry)
        
        # Update flags
        self.data["flags"]["low_sleep"] = hours < 4.0
        
        # Update daily stats
        if date not in self.data["daily_stats"]:
            self.data["daily_stats"][date] = {}
        
        self.data["daily_stats"][date]["sleep"] = {
            "hours": hours,
            "quality": quality,
            "low_sleep": hours < 4.0
        }
        
        # Keep only last 30 days of entries
        cutoff_date = (datetime.date.today() - datetime.timedelta(days=30)).isoformat()
        self.data["sleep_tracking"]["entries"] = [
            entry for entry in self.data["sleep_tracking"]["entries"]
            if entry["date"] >= cutoff_date
        ]
        
        self._save_telemetry()
        
        return sleep_entry
    
    # NON-BREAKING: Provide compatibility method name
    def get_telemetry_dict(self) -> Dict[str, Any]:
        """
        Get telemetry data as dictionary (NON-BREAKING compatibility method)
        
        Returns:
            Dictionary containing all telemetry data including optional fields
        """
        return {
            **self.data,
            "low_sleep": self.data.get("flags", {}).get("low_sleep", False)
        }
    
    def update_energy(self, level: str, context: str = "", 
                     factors: Dict[str, Any] = None) -> Dict[str, Any]:
        """Update energy level tracking"""
        valid_levels = ["very_low", "low", "medium", "high", "very_high"]
        if level not in valid_levels:
            raise ValueError(f"Level must be one of: {valid_levels}")
        
        timestamp = datetime.datetime.now().isoformat()
        date = datetime.date.today().isoformat()
        
        if factors is None:
            factors = {}
        
        # Create energy entry
        energy_entry = {
            "date": date,
            "timestamp": timestamp,
            "level": level,
            "context": context,
            "factors": factors,
            "low_energy_flag": level in ["very_low", "low"]
        }
        
        # Add to tracking
        self.data["energy_tracking"]["entries"].append(energy_entry)
        
        # Update flags
        self.data["flags"]["low_energy"] = level in ["very_low", "low"]
        
        # Update daily stats
        if date not in self.data["daily_stats"]:
            self.data["daily_stats"][date] = {}
        
        self.data["daily_stats"][date]["energy"] = {
            "level": level,
            "context": context,
            "low_energy": level in ["very_low", "low"]
        }
        
        # Keep only last 30 days of entries
        cutoff_date = (datetime.date.today() - datetime.timedelta(days=30)).isoformat()
        self.data["energy_tracking"]["entries"] = [
            entry for entry in self.data["energy_tracking"]["entries"]
            if entry["date"] >= cutoff_date
        ]
        
        self._save_telemetry()
        
        return energy_entry
    
    def get_current_state(self) -> Dict[str, Any]:
        """Get current telemetry state"""
        today = datetime.date.today().isoformat()
        today_stats = self.data["daily_stats"].get(today, {})
        
        # Get most recent entries
        recent_sleep = None
        recent_energy = None
        
        if self.data["sleep_tracking"]["entries"]:
            recent_sleep = self.data["sleep_tracking"]["entries"][-1]
        
        if self.data["energy_tracking"]["entries"]:
            recent_energy = self.data["energy_tracking"]["entries"][-1]
        
        return {
            "date": today,
            "flags": self.data["flags"].copy(),
            "today_stats": today_stats,
            "recent_sleep": recent_sleep,
            "recent_energy": recent_energy,
            "hours_slept_last_night": self.data.get("hours_slept_last_night"),
            "low_sleep": self.data.get("flags", {}).get("low_sleep", False),
            "recommendations": self._generate_recommendations()
        }
    
    def _generate_recommendations(self) -> list:
        """Generate recommendations based on current state"""
        recommendations = []
        flags = self.data["flags"]
        
        if flags.get("low_sleep"):
            recommendations.extend([
                "Low sleep detected - consider lighter workload today",
                "Take more frequent breaks to prevent fatigue",
                "Avoid complex problem-solving tasks when possible"
            ])
        
        if flags.get("low_energy"):
            recommendations.extend([
                "Low energy level - focus on familiar tasks",
                "Consider a brief walk or energy-boosting activity",
                "Postpone learning new technologies"
            ])
        
        if flags.get("cognitive_sensitivity"):
            recommendations.extend([
                "Cognitive sensitivity active - reduce task complexity",
                "Increase break frequency and duration",
                "Consider pair programming for support"
            ])
        
        return recommendations


def main():
    """CLI interface for development telemetry"""
    import argparse
    import json
    
    parser = argparse.ArgumentParser(description="Development Telemetry - Sleep & Energy Tracking")
    parser.add_argument('--update-sleep', type=float, help='Update sleep hours')
    parser.add_argument('--sleep-quality', choices=['poor', 'fair', 'good', 'excellent'],
                       default='good', help='Sleep quality')
    parser.add_argument('--sleep-notes', default='', help='Sleep notes')
    parser.add_argument('--update-energy', choices=['very_low', 'low', 'medium', 'high', 'very_high'],
                       help='Update energy level')
    parser.add_argument('--energy-context', default='', help='Energy context')
    parser.add_argument('--status', action='store_true', help='Show current status')
    parser.add_argument('--telemetry-path', default='data/dev_telemetry.json', help='Telemetry file path')
    
    args = parser.parse_args()
    
    telemetry = DevelopmentTelemetry(args.telemetry_path)
    
    if args.update_sleep is not None:
        entry = telemetry.update_sleep(args.update_sleep, args.sleep_quality, args.sleep_notes)
        print(f"😴 Sleep updated: {entry['hours']} hours ({entry['quality']})")
        if entry['low_sleep_flag']:
            print("⚠️ Low sleep detected - consider lighter workload")
            
    elif args.update_energy:
        factors = {}
        if args.energy_context:
            factors['context'] = args.energy_context
            
        entry = telemetry.update_energy(args.update_energy, args.energy_context, factors)
        print(f"⚡ Energy updated: {entry['level']} ({entry['context']})")
        if entry['low_energy_flag']:
            print("⚠️ Low energy detected - consider easier tasks")
            
    elif args.status:
        state = telemetry.get_current_state()
        print("📊 Development Telemetry Status:")
        print("=" * 35)
        print(f"Date: {state['date']}")
        print(f"Flags: {state['flags']}")
        
        if state['recent_sleep']:
            sleep = state['recent_sleep']
            print(f"Recent Sleep: {sleep['hours']}h ({sleep['quality']})")
            
        if state['recent_energy']:
            energy = state['recent_energy']
            print(f"Recent Energy: {energy['level']} ({energy['context']})")
        
        if state['recommendations']:
            print("\nRecommendations:")
            for rec in state['recommendations']:
                print(f"  💡 {rec}")
                
    else:
        parser.print_help()


if __name__ == "__main__":
    main()

@dataclass
class CycleTelemetry:
    """Simple dataclass for cycle metrics aggregation."""
    cycle_id: str = ""
    start_time: float = field(default_factory=time.time)
    end_time: float = 0.0
    
    # Component metrics
    stomp_metrics: Dict[str, Any] = field(default_factory=dict)
    retire_metrics: Dict[str, Any] = field(default_factory=dict)
    evaporation_metrics: Dict[str, Any] = field(default_factory=dict)
    infusion_metrics: Dict[str, Any] = field(default_factory=dict)
    selection_metrics: Dict[str, Any] = field(default_factory=dict)
    governance_metrics: Dict[str, Any] = field(default_factory=dict)
    
    # Output counts
    molten_glyph_count: int = 0
    mist_line_count: int = 0
    castle_room_count: int = 0
    
    def finish_cycle(self):
        """Mark cycle as complete and calculate total time."""
        self.end_time = time.time()
    
    def total_elapsed_ms(self) -> float:
        """Calculate total cycle time in milliseconds."""
        end = self.end_time if self.end_time > 0 else time.time()
        return (end - self.start_time) * 1000
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization."""
        return {
            "cycle_id": self.cycle_id,
            "total_elapsed_ms": self.total_elapsed_ms(),
            "start_time": self.start_time,
            "end_time": self.end_time,
            "components": {
                "stomp": self.stomp_metrics,
                "retire": self.retire_metrics,
                "evaporation": self.evaporation_metrics,
                "infusion": self.infusion_metrics,
                "selection": self.selection_metrics,
                "governance": self.governance_metrics,
            },
            "outputs": {
                "molten_glyphs": self.molten_glyph_count,
                "mist_lines": self.mist_line_count,
                "castle_rooms": self.castle_room_count,
            }
        }

def aggregate_cycle_metrics(telemetry: CycleTelemetry, components: Dict[str, Any]) -> Dict[str, Any]:
    """Aggregate metrics from all cycle components."""
    # Update telemetry with component results
    telemetry.stomp_metrics = components.get("stomp_result", {})
    telemetry.retire_metrics = {"glyphs_created": len(components.get("molten_glyphs", []))}
    telemetry.evaporation_metrics = {"mist_lines_created": components.get("mist_count", 0)}
    telemetry.infusion_metrics = components.get("infusion_result", {})
    telemetry.selection_metrics = {"prompt_assembled": True}
    telemetry.governance_metrics = components.get("governance_result", {})
    
    # Update counts
    telemetry.molten_glyph_count = len(components.get("molten_glyphs", []))
    telemetry.mist_line_count = components.get("mist_count", 0)
    telemetry.castle_room_count = len(components.get("top_rooms", []))
    
    return telemetry.to_dict()

def create_cycle_summary(telemetry: CycleTelemetry, cycle_outputs: Dict[str, Any]) -> Dict[str, Any]:
    """Create final cycle summary for reporting."""
    base_summary = {
        "cycle_report": telemetry.to_dict(),
        "molten_glyphs": cycle_outputs.get("molten_glyphs", []),
        "mist_count": cycle_outputs.get("mist_count", 0),
        "top_rooms": cycle_outputs.get("top_rooms", []),
    }
    
    # Optional: Add intervention metrics if available
    try:
        from behavioral_governance import BehavioralGovernance
        governance = BehavioralGovernance()
        if governance.intervention_tracking_enabled:
            enhanced_summary = governance.enhanced_score_cycle(base_summary)
            base_summary["behavioral_metrics"] = enhanced_summary.get("intervention_analytics", {})
    except ImportError:
        # Behavioral governance not available - continue with base summary
        pass
    
    return base_summary
