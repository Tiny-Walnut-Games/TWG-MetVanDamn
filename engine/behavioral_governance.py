#!/usr/bin/env python3
"""
Governance Integration for Intervention Metrics

Extends the existing governance system to include behavioral alignment
and intervention metrics. Provides seamless integration without breaking
existing functionality.

🧙‍♂️ "The enlightened system learns from its guidance as much as its output - 
    for wisdom lies in knowing when and how to intervene." - Bootstrap Sentinel
"""

from __future__ import annotations
import sys
from pathlib import Path
from typing import Dict, Any, Optional

# Add engine to path for imports
sys.path.insert(0, str(Path(__file__).parent))

try:
    from governance import Governance
    from intervention_metrics import InterventionMetrics, InterventionType, AcceptanceStatus
    GOVERNANCE_AVAILABLE = True
except ImportError:
    GOVERNANCE_AVAILABLE = False


class BehavioralGovernance:
    """
    Enhanced governance system with behavioral alignment and intervention metrics
    
    Extends existing governance.py functionality with intervention tracking,
    style adaptation, and reflective feedback loops.
    """
    
    def __init__(
        self,
        enable_intervention_tracking: bool = True,
        governance_instance: Optional[Governance] = None
    ):
        self.intervention_tracking_enabled = enable_intervention_tracking and GOVERNANCE_AVAILABLE
        
        if self.intervention_tracking_enabled:
            self.governance = governance_instance or Governance()
            self.intervention_metrics = InterventionMetrics()
        else:
            self.governance = None
            self.intervention_metrics = None
    
    def enhanced_score_cycle(
        self,
        cycle_report: Dict[str, Any],
        intervention_context: Optional[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        """
        Enhanced cycle scoring that includes intervention metrics
        
        Args:
            cycle_report: Standard cycle report from governance
            intervention_context: Optional context about interventions during cycle
            
        Returns:
            Enhanced scoring results with intervention metrics
        """
        if not self.intervention_tracking_enabled:
            # Fallback to basic governance if not available
            return {"score": 0.5, "assessment": "Basic assessment - intervention tracking unavailable"}
        
        # Get base governance score
        base_score = self.governance.score_cycle(cycle_report)
        
        # Enhance with intervention metrics if available
        if intervention_context:
            intervention_score = self._calculate_intervention_score(intervention_context)
            enhanced_score = self._blend_scores(base_score, intervention_score)
        else:
            enhanced_score = base_score
        
        # Add intervention analytics
        analytics = self.intervention_metrics.get_intervention_analytics()
        enhanced_score["intervention_analytics"] = analytics
        enhanced_score["behavioral_alignment"] = self._assess_behavioral_alignment(analytics)
        
        return enhanced_score
    
    def _calculate_intervention_score(self, intervention_context: Dict[str, Any]) -> Dict[str, Any]:
        """Calculate scoring contribution from intervention metrics"""
        analytics = self.intervention_metrics.get_intervention_analytics()
        
        # Base intervention score factors
        acceptance_rate = analytics.get("overall_metrics", {}).get("acceptance_rate", 0.0)
        total_interventions = analytics.get("total_interventions", 0)
        
        # Score based on acceptance rate and intervention appropriateness
        intervention_score = 0.5  # Neutral baseline
        
        if total_interventions > 0:
            # Higher acceptance rate = better intervention quality
            intervention_score = 0.3 + (acceptance_rate * 0.4)
            
            # Bonus for balanced intervention use (not too many, not too few)
            intervention_density = min(1.0, total_interventions / 10.0)  # Normalize to 10 as "normal"
            if 0.3 <= intervention_density <= 0.7:
                intervention_score += 0.1  # Bonus for balanced usage
            
            # Penalty for over-intervention
            if intervention_density > 0.9:
                intervention_score -= 0.2
        
        return {
            "intervention_quality_score": intervention_score,
            "acceptance_rate": acceptance_rate,
            "intervention_density": total_interventions,
            "assessment": self._assess_intervention_quality(intervention_score, acceptance_rate)
        }
    
    def _blend_scores(
        self,
        base_score: Dict[str, Any],
        intervention_score: Dict[str, Any]
    ) -> Dict[str, Any]:
        """Blend base governance score with intervention metrics"""
        # Load policy weights for blending
        policies = self.intervention_metrics.policies if self.intervention_metrics else {}
        governance_config = policies.get("governance_integration", {})
        
        base_weight = 1.0 - governance_config.get("intervention_weight", 0.2)
        intervention_weight = governance_config.get("intervention_weight", 0.2)
        
        # Blend numeric scores
        blended_score = (
            base_score["score"] * base_weight +
            intervention_score["intervention_quality_score"] * intervention_weight
        )
        
        # Create enhanced result
        enhanced_result = base_score.copy()
        enhanced_result["score"] = blended_score
        enhanced_result["intervention_contribution"] = intervention_score
        enhanced_result["score_breakdown"] = {
            "base_governance": base_score["score"],
            "intervention_quality": intervention_score["intervention_quality_score"],
            "blended_score": blended_score,
            "weights": {"base": base_weight, "intervention": intervention_weight}
        }
        
        return enhanced_result
    
    def _assess_intervention_quality(self, score: float, acceptance_rate: float) -> str:
        """Provide human-readable intervention quality assessment"""
        if score > 0.8 and acceptance_rate > 0.7:
            return "Excellent intervention quality - high acceptance and appropriate frequency"
        elif score > 0.6 and acceptance_rate > 0.5:
            return "Good intervention quality - balanced guidance with reasonable acceptance"
        elif acceptance_rate < 0.3:
            return "Poor intervention acceptance - consider adjusting intervention style"
        elif score < 0.4:
            return "Excessive intervention detected - reduce frequency or improve targeting"
        else:
            return "Moderate intervention quality - room for improvement in timing or approach"
    
    def _assess_behavioral_alignment(self, analytics: Dict[str, Any]) -> Dict[str, Any]:
        """Assess overall behavioral alignment based on intervention patterns"""
        if not analytics or analytics.get("total_interventions", 0) == 0:
            return {
                "status": "insufficient_data",
                "message": "Not enough intervention data for behavioral assessment",
                "recommendations": ["Begin tracking interventions to enable behavioral analysis"]
            }
        
        acceptance_rate = analytics.get("overall_metrics", {}).get("acceptance_rate", 0.0)
        total_interventions = analytics.get("total_interventions", 0)
        style_profiles = analytics.get("style_profiles_count", 0)
        
        # Determine alignment status
        if acceptance_rate >= 0.8 and total_interventions >= 5:
            status = "excellent"
            message = "Strong behavioral alignment - interventions are well-received and effective"
            recommendations = ["Continue current intervention strategy", "Consider expanding to more contexts"]
        elif acceptance_rate >= 0.6 and total_interventions >= 3:
            status = "good"
            message = "Good behavioral alignment - interventions generally accepted"
            recommendations = ["Fine-tune intervention timing", "Analyze rejection patterns for improvement"]
        elif acceptance_rate >= 0.4:
            status = "moderate"
            message = "Moderate alignment - interventions have mixed reception"
            recommendations = [
                "Review intervention policies",
                "Increase use of soft suggestions",
                "Improve style adaptation"
            ]
        else:
            status = "poor"
            message = "Poor behavioral alignment - interventions frequently rejected"
            recommendations = [
                "Reduce intervention frequency",
                "Focus on soft suggestions only",
                "Review and revise intervention policies",
                "Improve user style profiling"
            ]
        
        return {
            "status": status,
            "message": message,
            "recommendations": recommendations,
            "metrics": {
                "acceptance_rate": acceptance_rate,
                "total_interventions": total_interventions,
                "active_profiles": style_profiles
            }
        }
    
    def record_intervention_during_cycle(
        self,
        intervention_type: InterventionType,
        context: Dict[str, Any],
        original_input: str,
        suggested_output: Optional[str] = None,
        reasoning: str = "",
        user_id: str = "default"
    ) -> str:
        """
        Record an intervention that occurred during a cycle
        
        Returns:
            intervention_id for tracking acceptance
        """
        if not self.intervention_tracking_enabled:
            return "intervention_tracking_disabled"
        
        return self.intervention_metrics.record_intervention(
            intervention_type, context, original_input, suggested_output, reasoning, user_id
        )
    
    def enhanced_filter_response(
        self,
        response: Dict[str, Any],
        user_id: str = "default",
        context: Optional[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        """
        Enhanced response filtering with intervention awareness
        
        Args:
            response: Response to be filtered
            user_id: User identifier for style adaptation
            context: Additional context for intervention decisions
            
        Returns:
            Filtered response with potential intervention metadata
        """
        if not self.intervention_tracking_enabled:
            # Fallback to basic governance
            return self.governance.filter_response(response) if self.governance else response
        
        # Apply base governance filtering
        filtered_response = self.governance.filter_response(response)
        
        # Check if intervention is needed
        context = context or {}
        confidence = filtered_response.get("confidence", 0.5)
        
        # Determine intervention type based on confidence and content
        intervention_needed = False
        intervention_type = None
        intervention_reasoning = ""
        
        if confidence < 0.3:
            # Low confidence might need intervention
            if self.intervention_metrics.should_intervene(InterventionType.SOFT_SUGGESTION, 0.5, user_id):
                intervention_needed = True
                intervention_type = InterventionType.SOFT_SUGGESTION
                intervention_reasoning = "Low confidence response detected - suggesting alternative approach"
        
        elif len(filtered_response.get("response_text", "")) < 10:
            # Very short response might need expansion
            if self.intervention_metrics.should_intervene(InterventionType.REWRITE, 0.4, user_id):
                intervention_needed = True
                intervention_type = InterventionType.REWRITE
                intervention_reasoning = "Response too brief - suggesting expansion for clarity"
        
        # Record intervention if triggered
        if intervention_needed and intervention_type:
            intervention_context = {
                "type": "response_filtering",
                "user_id": user_id,
                "original_confidence": confidence,
                "response_length": len(filtered_response.get("response_text", "")),
                **context
            }
            
            intervention_id = self.record_intervention_during_cycle(
                intervention_type,
                intervention_context,
                str(response),
                str(filtered_response),
                intervention_reasoning,
                user_id
            )
            
            filtered_response["intervention_metadata"] = {
                "intervention_id": intervention_id,
                "intervention_type": intervention_type.value,
                "reasoning": intervention_reasoning
            }
        
        return filtered_response
    
    def get_behavioral_insights(self, user_id: Optional[str] = None) -> Dict[str, Any]:
        """Get comprehensive behavioral insights and recommendations"""
        if not self.intervention_tracking_enabled:
            return {"message": "Intervention tracking not available"}
        
        analytics = self.intervention_metrics.get_intervention_analytics(user_id)
        alignment = self._assess_behavioral_alignment(analytics)
        
        # Get style profile if available
        style_profile = None
        if user_id and self.intervention_metrics:
            profile = self.intervention_metrics.get_style_profile(user_id)
            if profile:
                style_profile = {
                    "adaptation_phase": profile.adaptation_phase,
                    "patience_level": profile.patience_level,
                    "intervention_tolerance": profile.intervention_tolerance,
                    "preferred_types": [t.value for t in profile.preferred_intervention_types]
                }
        
        return {
            "analytics": analytics,
            "behavioral_alignment": alignment,
            "style_profile": style_profile,
            "recommendations": self._generate_improvement_recommendations(analytics, alignment)
        }
    
    def _generate_improvement_recommendations(
        self,
        analytics: Dict[str, Any],
        alignment: Dict[str, Any]
    ) -> List[str]:
        """Generate specific recommendations for improving intervention quality"""
        recommendations = []
        
        acceptance_rate = analytics.get("overall_metrics", {}).get("acceptance_rate", 0.0)
        total_interventions = analytics.get("total_interventions", 0)
        
        if total_interventions < 5:
            recommendations.append("Begin more active intervention tracking to gather behavioral data")
        
        if acceptance_rate < 0.5:
            recommendations.append("Consider reducing intervention frequency")
            recommendations.append("Focus more on soft suggestions rather than direct rewrites")
            recommendations.append("Improve style profiling to better match user preferences")
        
        if acceptance_rate > 0.8 and total_interventions > 10:
            recommendations.append("Intervention strategy is working well - consider expanding scope")
            recommendations.append("Use current patterns as templates for similar contexts")
        
        acceptance_by_type = analytics.get("acceptance_by_type", {})
        for intervention_type, rate in acceptance_by_type.items():
            if rate < 0.3:
                recommendations.append(f"Reduce use of {intervention_type} interventions (low acceptance: {rate:.1%})")
            elif rate > 0.8:
                recommendations.append(f"Leverage {intervention_type} interventions more (high acceptance: {rate:.1%})")
        
        return recommendations


def main():
    """CLI interface for behavioral governance"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Behavioral Governance System")
    parser.add_argument("--score-cycle", type=str, help="Score cycle with intervention context (JSON string)")
    parser.add_argument("--insights", action="store_true", help="Show behavioral insights")
    parser.add_argument("--user", type=str, default="default", help="User ID for operations")
    parser.add_argument("--test-filter", type=str, help="Test response filtering (JSON response)")
    
    args = parser.parse_args()
    
    governance = BehavioralGovernance()
    
    if args.score_cycle:
        try:
            import json
            cycle_data = json.loads(args.score_cycle)
            
            score_result = governance.enhanced_score_cycle(cycle_data)
            print("📊 Enhanced Cycle Scoring:")
            print("=" * 30)
            
            for key, value in score_result.items():
                if isinstance(value, dict):
                    print(f"{key}:")
                    for sub_key, sub_value in value.items():
                        print(f"  {sub_key}: {sub_value}")
                else:
                    print(f"{key}: {value}")
                    
        except json.JSONDecodeError:
            print("❌ Error: Invalid JSON format for cycle data")
    
    elif args.test_filter:
        try:
            import json
            response_data = json.loads(args.test_filter)
            
            filtered = governance.enhanced_filter_response(response_data, args.user)
            print("🔍 Enhanced Response Filtering:")
            print("=" * 35)
            
            for key, value in filtered.items():
                print(f"{key}: {value}")
                
        except json.JSONDecodeError:
            print("❌ Error: Invalid JSON format for response data")
    
    elif args.insights:
        insights = governance.get_behavioral_insights(args.user if args.user != "default" else None)
        print("🧠 Behavioral Insights:")
        print("=" * 25)
        
        for key, value in insights.items():
            if isinstance(value, (dict, list)):
                print(f"{key}:")
                if isinstance(value, dict):
                    for sub_key, sub_value in value.items():
                        print(f"  {sub_key}: {sub_value}")
                else:
                    for item in value:
                        print(f"  - {item}")
            else:
                print(f"{key}: {value}")
    
    else:
        parser.print_help()


if __name__ == "__main__":
    main()