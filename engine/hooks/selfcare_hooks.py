#!/usr/bin/env python3
"""
Self-Care Integration Hooks - Optional Cognitive Safety Integration

Provides optional integration hooks for the self-care system that can be
called by run_cycle.py or other workflow systems. Designed to be completely
optional and non-breaking.

🧙‍♂️ "The wise system provides hooks for those who would use them,
    yet remains whole for those who would not." - Bootstrap Sentinel
"""

import sys
from pathlib import Path
from typing import Dict, Any, Optional, List

# Add src to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent.parent / "src"))

try:
    from selfcare.governors import MeltBudgetGovernor, HumidityGovernor, CognitiveSensitivityFlag
    from selfcare.idea_catalog import IdeaCatalog
    from selfcare.sluice_manager import SluiceManager
    SELFCARE_AVAILABLE = True
except ImportError:
    SELFCARE_AVAILABLE = False


class SelfCareHooks:
    """
    Self-Care Integration Hooks - Optional cognitive safety integration
    
    Provides optional hooks that can be integrated into development cycles
    without breaking existing functionality.
    """
    
    def __init__(self, enabled: bool = True):
        self.enabled = enabled and SELFCARE_AVAILABLE
        
        if self.enabled:
            self.melt_governor = MeltBudgetGovernor()
            self.humidity_governor = HumidityGovernor()
            self.sensitivity_flag = CognitiveSensitivityFlag()
            self.idea_catalog = IdeaCatalog()
            self.sluice_manager = SluiceManager()
        else:
            # Create stub objects for compatibility
            self.melt_governor = None
            self.humidity_governor = None
            self.sensitivity_flag = None
            self.idea_catalog = None
            self.sluice_manager = None
    
    def pre_cycle_check(self, context: Dict[str, Any] = None) -> Dict[str, Any]:
        """
        Perform pre-cycle cognitive safety checks
        
        Args:
            context: Optional context dictionary with environment state
            
        Returns:
            Dictionary with safety check results and recommendations
        """
        if not self.enabled:
            return {
                "selfcare_enabled": False,
                "message": "Self-care system not available or disabled"
            }
        
        if context is None:
            context = {}
        
        # Run all governor checks
        melt_check = self.melt_governor.check(context)
        humidity_check = self.humidity_governor.check(context)
        sensitivity_check = self.sensitivity_flag.check(context)
        
        # Aggregate results
        all_recommendations = []
        all_recommendations.extend(melt_check.get("recommendations", []))
        all_recommendations.extend(humidity_check.get("recommendations", []))
        all_recommendations.extend(sensitivity_check.get("recommendations", []))
        
        # Determine overall safety status
        safety_issues = []
        if melt_check.get("status") == "EXCEEDED":
            safety_issues.append("melt_budget_exceeded")
        if humidity_check.get("status") == "HIGH":
            safety_issues.append("high_humidity")
        if sensitivity_check.get("status") == "SENSITIVE":
            safety_issues.append("cognitive_sensitivity")
        
        # Determine recommended action
        if safety_issues:
            if "melt_budget_exceeded" in safety_issues:
                recommended_action = "defer_melts"
            elif "cognitive_sensitivity" in safety_issues:
                recommended_action = "reduce_complexity"
            elif "high_humidity" in safety_issues:
                recommended_action = "simplify_environment"
            else:
                recommended_action = "proceed_with_caution"
        else:
            recommended_action = "proceed_normally"
        
        return {
            "selfcare_enabled": True,
            "safety_status": "issues_detected" if safety_issues else "all_clear",
            "safety_issues": safety_issues,
            "recommended_action": recommended_action,
            "governor_results": {
                "melt_budget": melt_check,
                "humidity": humidity_check,
                "cognitive_sensitivity": sensitivity_check
            },
            "recommendations": all_recommendations
        }
    
    def post_cycle_update(self, cycle_results: Dict[str, Any] = None) -> Dict[str, Any]:
        """
        Update state after development cycle completion
        
        Args:
            cycle_results: Results from the development cycle
            
        Returns:
            Dictionary with update results
        """
        if not self.enabled:
            return {"selfcare_enabled": False}
        
        if cycle_results is None:
            cycle_results = {}
        
        updates = []
        
        # Record melt operations if any occurred
        melts_performed = cycle_results.get("melts_performed", 0)
        if melts_performed > 0:
            for _ in range(melts_performed):
                self.sensitivity_flag.record_melt()
            updates.append(f"Recorded {melts_performed} melt operations")
        
        # Update cognitive sensitivity based on cycle complexity
        cycle_complexity = cycle_results.get("complexity", "medium")
        if cycle_complexity in ["high", "very_high"]:
            # High complexity cycle may trigger sensitivity
            context = {"cognitive_load": 0.8}
            self.sensitivity_flag.check(context)
            updates.append("Updated cognitive sensitivity based on high complexity cycle")
        
        # Capture any overflow ideas if provided
        overflow_ideas = cycle_results.get("overflow_ideas", [])
        for idea in overflow_ideas:
            self.sluice_manager.append_line(idea)
            updates.append(f"Captured overflow idea to sluice")
        
        # Perform daily rollover if needed
        rollover_result = self.idea_catalog.daily_rollover()
        if rollover_result.get("ideas_rolled", 0) > 0:
            updates.append(f"Performed daily rollover: {rollover_result['ideas_rolled']} ideas")
        
        return {
            "selfcare_enabled": True,
            "updates_performed": updates,
            "rollover_summary": rollover_result
        }
    
    def capture_urgent_idea(self, idea_text: str) -> Optional[str]:
        """
        Quickly capture an urgent idea during development
        
        Args:
            idea_text: The idea to capture
            
        Returns:
            Idea ID if successful, None otherwise
        """
        if not self.enabled:
            return None
        
        try:
            # Add to sluice for quick capture
            if self.sluice_manager.append_line(f"URGENT: {idea_text}"):
                return f"sluice_{len(self.sluice_manager.get_lines())}"
            return None
        except Exception:
            return None
    
    def get_current_status(self) -> Dict[str, Any]:
        """Get current self-care system status"""
        if not self.enabled:
            return {
                "selfcare_enabled": False,
                "reason": "System not available or disabled"
            }
        
        # Get quick status from all components
        try:
            catalog_stats = self.idea_catalog.get_stats()
            sluice_stats = self.sluice_manager.get_stats()
            
            # Quick governor checks with minimal context
            melt_status = self.melt_governor.check()
            sensitivity_status = self.sensitivity_flag.check()
            
            return {
                "selfcare_enabled": True,
                "idea_catalog": {
                    "total_ideas": catalog_stats["total_ideas"],
                    "promoted_ideas": catalog_stats["promoted_ideas"]
                },
                "overflow_sluice": {
                    "total_files": sluice_stats["total_files"],
                    "total_lines": sluice_stats["total_lines"]
                },
                "cognitive_state": {
                    "melt_budget_status": melt_status.get("status", "UNKNOWN"),
                    "sensitivity_active": sensitivity_status.get("status") == "SENSITIVE",
                    "melts_today": sensitivity_status.get("melts_today", 0)
                }
            }
        except Exception as e:
            return {
                "selfcare_enabled": True,
                "error": f"Status check failed: {str(e)}"
            }
    
    def adjust_for_style(self, style_bias: Dict[str, Any]) -> Dict[str, Any]:
        """
        Adjust governor settings based on development style preferences
        
        Args:
            style_bias: Dictionary with style preferences
            
        Returns:
            Dictionary with adjustment results
        """
        if not self.enabled:
            return {"selfcare_enabled": False}
        
        adjustments = {}
        
        try:
            # Adjust each governor
            melt_adjustment = self.melt_governor.adjust(style_bias)
            humidity_adjustment = self.humidity_governor.adjust(style_bias)
            sensitivity_adjustment = self.sensitivity_flag.adjust(style_bias)
            
            adjustments = {
                "melt_budget": melt_adjustment,
                "humidity": humidity_adjustment,
                "cognitive_sensitivity": sensitivity_adjustment
            }
            
            return {
                "selfcare_enabled": True,
                "adjustments": adjustments,
                "style_bias_applied": style_bias
            }
            
        except Exception as e:
            return {
                "selfcare_enabled": True,
                "error": f"Style adjustment failed: {str(e)}"
            }


# Global instance for easy integration
selfcare_hooks = SelfCareHooks()


def check_cognitive_safety(context: Dict[str, Any] = None) -> Dict[str, Any]:
    """
    Convenient function for checking cognitive safety status
    
    Args:
        context: Optional context dictionary
        
    Returns:
        Safety check results
    """
    return selfcare_hooks.pre_cycle_check(context)


def update_after_cycle(cycle_results: Dict[str, Any] = None) -> Dict[str, Any]:
    """
    Convenient function for post-cycle updates
    
    Args:
        cycle_results: Results from development cycle
        
    Returns:
        Update results
    """
    return selfcare_hooks.post_cycle_update(cycle_results)


def capture_idea(idea_text: str) -> Optional[str]:
    """
    Convenient function for capturing urgent ideas
    
    Args:
        idea_text: The idea to capture
        
    Returns:
        Idea ID if successful
    """
    return selfcare_hooks.capture_urgent_idea(idea_text)


def main():
    """CLI interface for selfcare hooks"""
    import argparse
    import json
    
    parser = argparse.ArgumentParser(description="Self-Care Integration Hooks")
    parser.add_argument('--pre-check', action='store_true', help='Perform pre-cycle safety check')
    parser.add_argument('--post-update', action='store_true', help='Perform post-cycle update')
    parser.add_argument('--status', action='store_true', help='Show current status')
    parser.add_argument('--capture', help='Capture urgent idea')
    parser.add_argument('--context', help='JSON context for operations')
    parser.add_argument('--cycle-results', help='JSON cycle results for post-update')
    parser.add_argument('--style-bias', help='JSON style bias for adjustments')
    
    args = parser.parse_args()
    
    # Parse JSON inputs if provided
    context = {}
    cycle_results = {}
    style_bias = {}
    
    if args.context:
        try:
            context = json.loads(args.context)
        except json.JSONDecodeError:
            print("❌ Invalid JSON in context")
            return
    
    if args.cycle_results:
        try:
            cycle_results = json.loads(args.cycle_results)
        except json.JSONDecodeError:
            print("❌ Invalid JSON in cycle results")
            return
    
    if args.style_bias:
        try:
            style_bias = json.loads(args.style_bias)
        except json.JSONDecodeError:
            print("❌ Invalid JSON in style bias")
            return
    
    if args.pre_check:
        result = check_cognitive_safety(context)
        print("🛡️ Pre-Cycle Safety Check:")
        print(json.dumps(result, indent=2))
        
    elif args.post_update:
        result = update_after_cycle(cycle_results)
        print("📊 Post-Cycle Update:")
        print(json.dumps(result, indent=2))
        
    elif args.status:
        result = selfcare_hooks.get_current_status()
        print("📋 Self-Care Status:")
        print(json.dumps(result, indent=2))
        
    elif args.capture:
        idea_id = capture_idea(args.capture)
        if idea_id:
            print(f"💡 Idea captured: {idea_id}")
        else:
            print("❌ Failed to capture idea")
            
    elif style_bias:
        result = selfcare_hooks.adjust_for_style(style_bias)
        print("⚙️ Style Adjustments:")
        print(json.dumps(result, indent=2))
        
    else:
        parser.print_help()


if __name__ == "__main__":
    main()