#!/usr/bin/env python3
"""
Cognitive Safety Governors - The Guardians of Mental Well-being

Implements safety mechanisms to prevent cognitive overload and manage
development flow. Provides melt budget limits, humidity thresholds,
and cognitive sensitivity monitoring.

🧙‍♂️ "The wise developer knows when to build and when to rest - 
    the governors ensure neither excess nor stagnation." - Bootstrap Sentinel
"""

import json
import datetime
import time
from pathlib import Path
from typing import Dict, Any, Optional
from dataclasses import dataclass
from abc import ABC, abstractmethod


@dataclass
class GovernorState:
    """Base state for all governors"""
    enabled: bool = True
    last_check: Optional[str] = None
    violations: int = 0
    last_violation: Optional[str] = None


class BaseGovernor(ABC):
    """Base class for cognitive safety governors"""
    
    def __init__(self, state_file: Optional[str] = None):
        self.state_file = Path(state_file) if state_file else None
        self.state = self._load_state()
    
    @abstractmethod
    def check(self, context: Dict[str, Any] = None) -> Dict[str, Any]:
        """
        Check governor conditions
        
        Returns:
            Dictionary with check results and recommendations
        """
        pass
    
    @abstractmethod
    def adjust(self, style_bias: Dict[str, Any] = None) -> Dict[str, Any]:
        """
        Adjust behavior based on style bias
        
        Returns:
            Dictionary with adjustment results
        """
        pass
    
    def _load_state(self) -> GovernorState:
        """Load governor state from file or return default"""
        if self.state_file and self.state_file.exists():
            try:
                with open(self.state_file, 'r') as f:
                    data = json.load(f)
                return GovernorState(**data)
            except (json.JSONDecodeError, TypeError):
                pass
        
        return GovernorState()
    
    def _save_state(self):
        """Save governor state to file"""
        if self.state_file:
            self.state_file.parent.mkdir(parents=True, exist_ok=True)
            
            state_dict = {
                'enabled': self.state.enabled,
                'last_check': self.state.last_check,
                'violations': self.state.violations,
                'last_violation': self.state.last_violation
            }
            
            with open(self.state_file, 'w') as f:
                json.dump(state_dict, f, indent=2)


class MeltBudgetGovernor(BaseGovernor):
    """
    Melt Budget Governor - Limits daily melting operations
    
    Prevents cognitive overload by enforcing daily limits on
    intensive development activities (melts).
    """
    
    def __init__(self, melt_max_per_day: int = 2, state_file: str = "data/melt_budget_state.json"):
        self.melt_max_per_day = melt_max_per_day
        self.daily_melts = self._load_daily_melts()
        super().__init__(state_file)
    
    def _load_daily_melts(self) -> Dict[str, int]:
        """Load daily melt tracking data"""
        # This would integrate with existing melt tracking systems
        # For now, return empty dict as placeholder
        return {}
    
    def check(self, context: Dict[str, Any] = None) -> Dict[str, Any]:
        """Check melt budget status"""
        today = datetime.date.today().isoformat()
        current_melts = self.daily_melts.get(today, 0)
        
        self.state.last_check = datetime.datetime.now().isoformat()
        
        budget_exceeded = current_melts >= self.melt_max_per_day
        
        if budget_exceeded and not self.state.last_violation:
            self.state.violations += 1
            self.state.last_violation = self.state.last_check
        
        result = {
            "governor": "MeltBudgetGovernor",
            "status": "EXCEEDED" if budget_exceeded else "OK",
            "current_melts": current_melts,
            "max_melts": self.melt_max_per_day,
            "remaining_budget": max(0, self.melt_max_per_day - current_melts),
            "recommendations": []
        }
        
        if budget_exceeded:
            result["recommendations"].extend([
                "Consider deferring non-critical melts to tomorrow",
                "Focus on documentation and planning activities",
                "Take breaks to prevent cognitive fatigue"
            ])
        elif current_melts >= self.melt_max_per_day * 0.8:
            result["recommendations"].append("Approaching daily melt limit - plan accordingly")
        
        self._save_state()
        return result
    
    def adjust(self, style_bias: Dict[str, Any] = None) -> Dict[str, Any]:
        """Adjust melt budget based on style preferences"""
        if not style_bias:
            return {"adjusted": False, "reason": "No style bias provided"}
        
        # Adjust budget based on cognitive preference
        cognitive_style = style_bias.get("cognitive_load_preference", "balanced")
        
        adjustments = {
            "high": {"multiplier": 1.5, "reason": "High cognitive load preference"},
            "balanced": {"multiplier": 1.0, "reason": "Balanced approach maintained"},
            "low": {"multiplier": 0.5, "reason": "Conservative cognitive load preference"}
        }
        
        if cognitive_style in adjustments:
            adjustment = adjustments[cognitive_style]
            new_budget = max(1, int(self.melt_max_per_day * adjustment["multiplier"]))
            
            result = {
                "adjusted": True,
                "original_budget": self.melt_max_per_day,
                "new_budget": new_budget,
                "reason": adjustment["reason"]
            }
            
            # Don't actually change the base budget, just return recommendation
            result["recommendation"] = f"Consider adjusting daily budget to {new_budget}"
            return result
        
        return {"adjusted": False, "reason": "Unknown cognitive style preference"}


class HumidityGovernor(BaseGovernor):
    """
    Humidity Governor - Monitors development environment 'humidity'
    
    Tracks environmental factors that might impede development flow
    and suggests adjustments to maintain optimal conditions.
    """
    
    def __init__(self, humidity_threshold: float = 0.7, state_file: str = "data/humidity_state.json"):
        self.humidity_threshold = humidity_threshold
        super().__init__(state_file)
    
    def _calculate_humidity(self, context: Dict[str, Any] = None) -> Dict[str, float]:
        """Calculate current development environment humidity"""
        if not context:
            context = {}
        
        # Humidity factors (0.0 = dry, 1.0 = saturated)
        factors = {
            "cognitive_load": context.get("cognitive_load", 0.3),
            "technical_debt": context.get("technical_debt", 0.2),
            "interruption_rate": context.get("interruption_rate", 0.1),
            "complexity_growth": context.get("complexity_growth", 0.15),
            "external_pressure": context.get("external_pressure", 0.25)
        }
        
        # Weighted average (can be customized)
        weights = {
            "cognitive_load": 0.3,
            "technical_debt": 0.25,
            "interruption_rate": 0.2,
            "complexity_growth": 0.15,
            "external_pressure": 0.1
        }
        
        total_humidity = sum(
            factors[factor] * weights[factor] 
            for factor in factors
        )
        
        return {
            "total_humidity": total_humidity,
            "factors": factors,
            "weights": weights
        }
    
    def check(self, context: Dict[str, Any] = None) -> Dict[str, Any]:
        """Check humidity levels"""
        humidity_data = self._calculate_humidity(context)
        current_humidity = humidity_data["total_humidity"]
        
        self.state.last_check = datetime.datetime.now().isoformat()
        
        humidity_high = current_humidity > self.humidity_threshold
        
        if humidity_high and not self.state.last_violation:
            self.state.violations += 1
            self.state.last_violation = self.state.last_check
        
        result = {
            "governor": "HumidityGovernor",
            "status": "HIGH" if humidity_high else "OK",
            "current_humidity": round(current_humidity, 3),
            "threshold": self.humidity_threshold,
            "factors": humidity_data["factors"],
            "recommendations": []
        }
        
        if humidity_high:
            result["recommendations"].extend([
                "Consider reducing scope of current tasks",
                "Address technical debt in small increments",
                "Implement better interruption management",
                "Break complex tasks into smaller chunks"
            ])
            
            # Specific recommendations based on highest factors
            max_factor = max(humidity_data["factors"].items(), key=lambda x: x[1])
            factor_name, factor_value = max_factor
            
            if factor_name == "cognitive_load" and factor_value > 0.5:
                result["recommendations"].append("High cognitive load detected - consider simpler tasks")
            elif factor_name == "technical_debt" and factor_value > 0.5:
                result["recommendations"].append("Technical debt is impacting flow - prioritize cleanup")
        
        self._save_state()
        return result
    
    def adjust(self, style_bias: Dict[str, Any] = None) -> Dict[str, Any]:
        """Adjust humidity threshold based on style preferences"""
        if not style_bias:
            return {"adjusted": False, "reason": "No style bias provided"}
        
        flow_preference = style_bias.get("flow_preference", "balanced")
        
        adjustments = {
            "high_tolerance": {"threshold": 0.8, "reason": "High humidity tolerance"},
            "balanced": {"threshold": 0.7, "reason": "Balanced humidity management"},
            "low_tolerance": {"threshold": 0.5, "reason": "Low humidity tolerance - pristine environment"}
        }
        
        if flow_preference in adjustments:
            adjustment = adjustments[flow_preference]
            
            return {
                "adjusted": True,
                "original_threshold": self.humidity_threshold,
                "recommended_threshold": adjustment["threshold"],
                "reason": adjustment["reason"]
            }
        
        return {"adjusted": False, "reason": "Unknown flow preference"}


class CognitiveSensitivityFlag(BaseGovernor):
    """
    Cognitive Sensitivity Flag - Tracks cognitive state sensitivity
    
    Monitors cognitive sensitivity levels and adjusts development
    recommendations accordingly.
    """
    
    def __init__(self, state_file: str = "data/cognitive_state.json"):
        super().__init__(state_file)
        self.cognitive_state = self._load_cognitive_state()
    
    def _load_cognitive_state(self) -> Dict[str, Any]:
        """Load cognitive state data"""
        state_path = Path("data/cognitive_state.json")
        
        if state_path.exists():
            try:
                with state_path.open('r') as f:
                    return json.load(f)
            except (json.JSONDecodeError, FileNotFoundError):
                pass
        
        # Default state
        default_state = {
            "cognitive_sensitivity": False,
            "melts_today": 0,
            "last_reset_epoch": int(time.time())
        }
        
        # Create the file with default state
        state_path.parent.mkdir(parents=True, exist_ok=True)
        with state_path.open('w') as f:
            json.dump(default_state, f, indent=2)
        
        return default_state
    
    def _save_cognitive_state(self):
        """Save cognitive state to file"""
        state_path = Path("data/cognitive_state.json")
        state_path.parent.mkdir(parents=True, exist_ok=True)
        
        with state_path.open('w') as f:
            json.dump(self.cognitive_state, f, indent=2)
    
    def check(self, context: Dict[str, Any] = None) -> Dict[str, Any]:
        """Check cognitive sensitivity status"""
        if not context:
            context = {}
        
        self.state.last_check = datetime.datetime.now().isoformat()
        
        # Check if daily reset is needed
        current_epoch = int(time.time())
        last_reset = self.cognitive_state.get("last_reset_epoch", 0)
        seconds_since_reset = current_epoch - last_reset
        
        # Reset daily if more than 24 hours
        if seconds_since_reset > 86400:  # 24 hours
            self.cognitive_state["melts_today"] = 0
            self.cognitive_state["last_reset_epoch"] = current_epoch
            self._save_cognitive_state()
        
        sensitivity_active = self.cognitive_state.get("cognitive_sensitivity", False)
        melts_today = self.cognitive_state.get("melts_today", 0)
        
        # Check for sensitivity triggers
        triggers = []
        
        # Sleep quality trigger (if available)
        sleep_quality = context.get("sleep_quality", "good")
        if sleep_quality in ["poor", "none"]:
            triggers.append("poor_sleep")
            
        # High cognitive load trigger
        cognitive_load = context.get("cognitive_load", 0.3)
        if cognitive_load > 0.7:
            triggers.append("high_cognitive_load")
            
        # High melt count trigger
        if melts_today > 2:
            triggers.append("high_melt_count")
        
        # Auto-activate sensitivity if triggers present
        if triggers and not sensitivity_active:
            sensitivity_active = True
            self.cognitive_state["cognitive_sensitivity"] = True
            self._save_cognitive_state()
        
        result = {
            "governor": "CognitiveSensitivityFlag",
            "status": "SENSITIVE" if sensitivity_active else "NORMAL",
            "triggers": triggers,
            "melts_today": melts_today,
            "hours_since_reset": round(seconds_since_reset / 3600, 1),
            "recommendations": []
        }
        
        if sensitivity_active:
            result["recommendations"].extend([
                "Reduce complexity of planned tasks",
                "Increase break frequency",
                "Focus on familiar technologies",
                "Defer learning new concepts to later",
                "Consider pair programming for complex tasks"
            ])
            
            if "poor_sleep" in triggers:
                result["recommendations"].append("Sleep quality impact detected - consider lighter workload")
        
        self._save_state()
        return result
    
    def adjust(self, style_bias: Dict[str, Any] = None) -> Dict[str, Any]:
        """Adjust sensitivity settings based on style preferences"""
        if not style_bias:
            return {"adjusted": False, "reason": "No style bias provided"}
        
        sensitivity_preference = style_bias.get("sensitivity_management", "auto")
        
        if sensitivity_preference == "manual":
            # Allow manual override
            manual_state = style_bias.get("manual_sensitivity", False)
            self.cognitive_state["cognitive_sensitivity"] = manual_state
            self._save_cognitive_state()
            
            return {
                "adjusted": True,
                "mode": "manual",
                "sensitivity_state": manual_state,
                "reason": "Manual sensitivity override applied"
            }
        
        elif sensitivity_preference == "conservative":
            # More conservative thresholds
            return {
                "adjusted": True,
                "mode": "conservative",
                "reason": "Conservative sensitivity thresholds recommended",
                "recommendations": [
                    "Lower cognitive load thresholds",
                    "Earlier sensitivity activation",
                    "More frequent breaks"
                ]
            }
        
        return {"adjusted": False, "reason": "Using automatic sensitivity management"}
    
    def set_sensitivity(self, sensitive: bool, reason: str = "manual"):
        """Manually set cognitive sensitivity state"""
        self.cognitive_state["cognitive_sensitivity"] = sensitive
        self.cognitive_state["sensitivity_reason"] = reason
        self.cognitive_state["sensitivity_set_at"] = datetime.datetime.now().isoformat()
        self._save_cognitive_state()
    
    def record_melt(self):
        """Record a melt operation"""
        self.cognitive_state["melts_today"] = self.cognitive_state.get("melts_today", 0) + 1
        self._save_cognitive_state()


def main():
    """CLI interface for cognitive governors"""
    import argparse
    import json
    
    parser = argparse.ArgumentParser(description="Cognitive Safety Governors")
    parser.add_argument('--check-all', action='store_true', help='Check all governors')
    parser.add_argument('--check-melt', action='store_true', help='Check melt budget governor')
    parser.add_argument('--check-humidity', action='store_true', help='Check humidity governor')
    parser.add_argument('--check-sensitivity', action='store_true', help='Check cognitive sensitivity')
    parser.add_argument('--context', help='JSON context for checks')
    parser.add_argument('--style-bias', help='JSON style bias for adjustments')
    parser.add_argument('--set-sensitivity', choices=['true', 'false'], help='Manually set sensitivity')
    parser.add_argument('--record-melt', action='store_true', help='Record a melt operation')
    
    args = parser.parse_args()
    
    # Parse context and style bias if provided
    context = {}
    style_bias = {}
    
    if args.context:
        try:
            context = json.loads(args.context)
        except json.JSONDecodeError:
            print("❌ Invalid JSON in context")
            return
    
    if args.style_bias:
        try:
            style_bias = json.loads(args.style_bias)
        except json.JSONDecodeError:
            print("❌ Invalid JSON in style bias")
            return
    
    # Initialize governors
    melt_governor = MeltBudgetGovernor()
    humidity_governor = HumidityGovernor()
    sensitivity_flag = CognitiveSensitivityFlag()
    
    if args.check_all or args.check_melt:
        result = melt_governor.check(context)
        print("🚨 Melt Budget Governor:")
        print(json.dumps(result, indent=2))
        print()
    
    if args.check_all or args.check_humidity:
        result = humidity_governor.check(context)
        print("🌊 Humidity Governor:")
        print(json.dumps(result, indent=2))
        print()
    
    if args.check_all or args.check_sensitivity:
        result = sensitivity_flag.check(context)
        print("🧠 Cognitive Sensitivity Flag:")
        print(json.dumps(result, indent=2))
        print()
    
    if args.set_sensitivity:
        sensitive = args.set_sensitivity == 'true'
        sensitivity_flag.set_sensitivity(sensitive, "manual_cli")
        print(f"🧠 Cognitive sensitivity set to: {sensitive}")
    
    if args.record_melt:
        sensitivity_flag.record_melt()
        print("📊 Melt operation recorded")


if __name__ == "__main__":
    main()