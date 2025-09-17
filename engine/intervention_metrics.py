#!/usr/bin/env python3
"""
Behavioral Alignment & Intervention Metrics System

Tracks AI agent interventions, user acceptance rates, and style adaptation
patterns. Provides reflective loops and policy injection capabilities.

🧙‍♂️ "The wise system learns not just from success, but from the patterns
    of its guidance - measuring intervention as much as innovation." - Bootstrap Sentinel
"""

from __future__ import annotations
import json
import time
import datetime
from pathlib import Path
from typing import Dict, Any, List, Optional
from dataclasses import dataclass, asdict
from enum import Enum


class InterventionType(Enum):
    """Classification of intervention types"""
    SOFT_SUGGESTION = "soft_suggestion"
    REWRITE = "rewrite"
    BLOCK = "block"
    STYLE_GUIDANCE = "style_guidance"
    SAFETY_INTERVENTION = "safety_intervention"


class SafetyEventLevel(Enum):
    """Tiered safety event classification for v0.8 milestone"""
    NOTICE = "notice"        # Informational safety guidance
    WARN = "warn"           # Warning about potential issues
    BLOCK = "block"         # Block action due to safety concerns  
    ESCALATE = "escalate"   # Escalate to human oversight


class AcceptanceStatus(Enum):
    """Track user response to interventions"""
    PENDING = "pending"
    ACCEPTED = "accepted"
    REJECTED = "rejected"
    MODIFIED = "modified"
    IGNORED = "ignored"


@dataclass
class InterventionRecord:
    """Single intervention tracking record"""
    intervention_id: str
    timestamp: float
    intervention_type: InterventionType
    context: Dict[str, Any]
    original_input: str
    suggested_output: Optional[str]
    reasoning: str
    acceptance_status: AcceptanceStatus = AcceptanceStatus.PENDING
    user_response: Optional[str] = None
    final_output: Optional[str] = None
    response_time_ms: Optional[float] = None
    # v0.8 Safety & Policy Transparency additions
    safety_event_level: Optional[SafetyEventLevel] = None
    policy_metadata: Optional[Dict[str, Any]] = None
    redaction_applied: bool = False
    audit_trail: Optional[List[Dict[str, Any]]] = None
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization"""
        data = asdict(self)
        data['intervention_type'] = self.intervention_type.value
        data['acceptance_status'] = self.acceptance_status.value
        if self.safety_event_level:
            data['safety_event_level'] = self.safety_event_level.value
        return data
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'InterventionRecord':
        """Create from dictionary"""
        data['intervention_type'] = InterventionType(data['intervention_type'])
        data['acceptance_status'] = AcceptanceStatus(data['acceptance_status'])
        if 'safety_event_level' in data and data['safety_event_level']:
            data['safety_event_level'] = SafetyEventLevel(data['safety_event_level'])
        return cls(**data)


@dataclass
class StyleProfile:
    """User style adaptation profile"""
    user_id: str
    patience_level: float  # 0.0 to 1.0
    intervention_tolerance: float  # 0.0 to 1.0
    preferred_intervention_types: List[InterventionType]
    response_patterns: Dict[str, float]
    adaptation_phase: str  # "learning", "adapting", "stable"
    last_updated: float
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization"""
        data = asdict(self)
        data['preferred_intervention_types'] = [t.value for t in self.preferred_intervention_types]
        return data
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'StyleProfile':
        """Create from dictionary"""
        data['preferred_intervention_types'] = [InterventionType(t) for t in data['preferred_intervention_types']]
        return cls(**data)


class InterventionMetrics:
    """
    Main intervention metrics tracking system
    
    Integrates with existing governance and telemetry systems to provide
    comprehensive behavioral alignment tracking.
    """
    
    def __init__(self, data_path: str = "data/intervention_metrics.json"):
        self.data_path = Path(data_path)
        self.data_path.parent.mkdir(parents=True, exist_ok=True)
        
        self.data = self._load_data()
        self.policies = self._load_policies()
        
    def _load_data(self) -> Dict[str, Any]:
        """Load intervention metrics data"""
        if self.data_path.exists():
            try:
                with open(self.data_path, 'r') as f:
                    data = json.load(f)
                    
                # Convert intervention records back to objects
                if 'interventions' in data:
                    data['interventions'] = [
                        InterventionRecord.from_dict(record) 
                        for record in data['interventions']
                    ]
                
                # Convert style profiles back to objects
                if 'style_profiles' in data:
                    data['style_profiles'] = {
                        user_id: StyleProfile.from_dict(profile)
                        for user_id, profile in data['style_profiles'].items()
                    }
                
                return data
            except Exception as e:
                print(f"⚠️ Warning: Failed to load intervention metrics data: {e}")
        
        return {
            "interventions": [],
            "style_profiles": {},
            "metrics_summary": {
                "total_interventions": 0,
                "acceptance_rate": 0.0,
                "avg_response_time": 0.0,
                "last_updated": time.time()
            }
        }
    
    def _save_data(self):
        """Save intervention metrics data"""
        try:
            # Convert objects to dictionaries for JSON serialization
            save_data = {
                "interventions": [record.to_dict() for record in self.data["interventions"]],
                "style_profiles": {
                    user_id: profile.to_dict() 
                    for user_id, profile in self.data["style_profiles"].items()
                },
                "metrics_summary": self.data["metrics_summary"],
                "last_updated": time.time()
            }
            
            with open(self.data_path, 'w') as f:
                json.dump(save_data, f, indent=2, ensure_ascii=False)
        except Exception as e:
            print(f"❌ Error: Failed to save intervention metrics data: {e}")
    
    def _load_policies(self) -> Dict[str, Any]:
        """Load policy injection rules"""
        policy_path = self.data_path.parent / "intervention_policies.json"
        if policy_path.exists():
            try:
                with open(policy_path, 'r') as f:
                    return json.load(f)
            except Exception as e:
                print(f"⚠️ Warning: Failed to load intervention policies: {e}")
        
        # Default policies
        return {
            "intervention_thresholds": {
                "soft_suggestion": 0.3,
                "rewrite": 0.6,
                "block": 0.9
            },
            "style_adaptation": {
                "learning_phase_duration": 7 * 24 * 3600,  # 7 days
                "adapting_phase_duration": 14 * 24 * 3600,  # 14 days
                "patience_decay_rate": 0.1,
                "tolerance_adjustment_rate": 0.05
            },
            "reflective_templates": {
                "soft_suggestion": "I suggested {suggestion} because {reasoning}. The user's {context} indicated {pattern}.",
                "rewrite": "I recommended rewriting because {reasoning}. The original {input} could be improved by {improvement}.",
                "block": "I blocked this action because {safety_concern}. The {context} presented {risk_level} risk.",
                "style_guidance": "I provided style guidance because {style_issue}. The user's preference for {preference} suggests {adaptation}."
            }
        }
    
    def record_intervention(
        self,
        intervention_type: InterventionType,
        context: Dict[str, Any],
        original_input: str,
        suggested_output: Optional[str] = None,
        reasoning: str = "",
        user_id: str = "default",
        # v0.8 Safety & Policy Transparency additions
        safety_event_level: Optional[SafetyEventLevel] = None,
        enable_redaction: bool = True
    ) -> str:
        """
        Record a new intervention with enhanced safety features
        
        Returns:
            intervention_id for tracking acceptance
        """
        intervention_id = f"int_{int(time.time() * 1000)}"
        
        # Apply reflective loop template
        reflective_reasoning = self._apply_reflective_template(
            intervention_type, reasoning, context, original_input, suggested_output
        )
        
        # v0.8: Apply redaction transforms if enabled
        redaction_metadata = None
        processed_input = original_input
        processed_output = suggested_output
        
        if enable_redaction:
            try:
                from redaction_transforms import RedactionTransforms
                redactor = RedactionTransforms()
                
                # Redact original input
                processed_input, redaction_meta = redactor.apply_redaction(
                    original_input, {"intervention_type": intervention_type.value}
                )
                redaction_metadata = redaction_meta
                
                # Redact suggested output if provided
                if suggested_output:
                    processed_output, _ = redactor.apply_redaction(
                        suggested_output, {"intervention_type": intervention_type.value}
                    )
                    
            except ImportError:
                print("⚠️ Warning: Redaction transforms not available")
        
        # v0.8: Create safety event if safety level specified
        if safety_event_level:
            try:
                from safety_policy_transparency import SafetyPolicyTransparency
                safety_layer = SafetyPolicyTransparency()
                
                safety_event = safety_layer.create_safety_event(
                    safety_level=safety_event_level,
                    event_type=f"intervention_{intervention_type.value}",
                    context=context,
                    reasoning=reflective_reasoning,
                    content=original_input,
                    intervention_id=intervention_id,
                    user_id=user_id
                )
                
                # Add safety event metadata to context
                context["safety_event_id"] = safety_event.event_id
                context["policy_action"] = safety_event.policy_action.value
                
            except ImportError:
                print("⚠️ Warning: Safety policy transparency not available")
        
        record = InterventionRecord(
            intervention_id=intervention_id,
            timestamp=time.time(),
            intervention_type=intervention_type,
            context=context,
            original_input=processed_input,
            suggested_output=processed_output,
            reasoning=reflective_reasoning,
            # v0.8 additions
            safety_event_level=safety_event_level,
            policy_metadata=redaction_metadata,
            redaction_applied=redaction_metadata is not None and redaction_metadata.get("redaction_applied", False),
            audit_trail=[{
                "timestamp": time.time(),
                "action": "intervention_created",
                "metadata": {
                    "redaction_enabled": enable_redaction,
                    "safety_level": safety_event_level.value if safety_event_level else None
                }
            }]
        )
        
        self.data["interventions"].append(record)
        self._update_metrics_summary()
        self._save_data()
        
        # Update style profile if exists
        if user_id in self.data["style_profiles"]:
            self._update_style_profile(user_id, intervention_type, context)
        
        return intervention_id
    
    def _apply_reflective_template(
        self,
        intervention_type: InterventionType,
        reasoning: str,
        context: Dict[str, Any],
        original_input: str,
        suggested_output: Optional[str]
    ) -> str:
        """Apply reflective loop template to generate structured reasoning"""
        template = self.policies["reflective_templates"].get(
            intervention_type.value, 
            "I intervened because {reasoning}"
        )
        
        try:
            return template.format(
                reasoning=reasoning,
                context=context.get("type", "unknown context"),
                pattern=context.get("detected_pattern", "standard pattern"),
                suggestion=suggested_output or "alternative approach",
                input=original_input[:100] + ("..." if len(original_input) > 100 else ""),
                improvement=context.get("improvement_area", "clarity"),
                safety_concern=context.get("safety_concern", "policy violation"),
                risk_level=context.get("risk_level", "medium"),
                style_issue=context.get("style_issue", "formatting"),
                preference=context.get("user_preference", "unknown"),
                adaptation=context.get("suggested_adaptation", "standard approach")
            )
        except KeyError:
            # Fallback if template formatting fails
            return f"I intervened ({intervention_type.value}) because: {reasoning}"
    
    def record_acceptance(
        self,
        intervention_id: str,
        acceptance_status: AcceptanceStatus,
        user_response: Optional[str] = None,
        final_output: Optional[str] = None
    ) -> bool:
        """
        Record user acceptance/rejection of intervention
        
        Returns:
            True if intervention was found and updated
        """
        for record in self.data["interventions"]:
            if record.intervention_id == intervention_id:
                record.acceptance_status = acceptance_status
                record.user_response = user_response
                record.final_output = final_output
                record.response_time_ms = (time.time() - record.timestamp) * 1000
                
                self._update_metrics_summary()
                self._save_data()
                return True
        
        return False
    
    def get_style_profile(self, user_id: str) -> Optional[StyleProfile]:
        """Get user style profile"""
        return self.data["style_profiles"].get(user_id)
    
    def create_style_profile(self, user_id: str) -> StyleProfile:
        """Create new style profile for user"""
        profile = StyleProfile(
            user_id=user_id,
            patience_level=0.5,  # Start neutral
            intervention_tolerance=0.5,  # Start neutral
            preferred_intervention_types=[InterventionType.SOFT_SUGGESTION],
            response_patterns={},
            adaptation_phase="learning",
            last_updated=time.time()
        )
        
        self.data["style_profiles"][user_id] = profile
        self._save_data()
        return profile
    
    def _update_style_profile(
        self,
        user_id: str,
        intervention_type: InterventionType,
        context: Dict[str, Any]
    ):
        """Update style profile based on intervention patterns"""
        profile = self.data["style_profiles"][user_id]
        
        # Update adaptation phase based on time
        time_since_creation = time.time() - profile.last_updated
        learning_duration = self.policies["style_adaptation"]["learning_phase_duration"]
        adapting_duration = self.policies["style_adaptation"]["adapting_phase_duration"]
        
        if time_since_creation > learning_duration + adapting_duration:
            profile.adaptation_phase = "stable"
        elif time_since_creation > learning_duration:
            profile.adaptation_phase = "adapting"
        
        # Track response patterns
        intervention_key = intervention_type.value
        if intervention_key not in profile.response_patterns:
            profile.response_patterns[intervention_key] = 0.0
        
        # This will be updated when acceptance is recorded
        profile.last_updated = time.time()
        self._save_data()
    
    def _update_metrics_summary(self):
        """Update overall metrics summary"""
        interventions = self.data["interventions"]
        total = len(interventions)
        
        if total == 0:
            return
        
        # Calculate acceptance rate
        completed_interventions = [
            i for i in interventions 
            if i.acceptance_status != AcceptanceStatus.PENDING
        ]
        
        if completed_interventions:
            accepted = len([
                i for i in completed_interventions 
                if i.acceptance_status in [AcceptanceStatus.ACCEPTED, AcceptanceStatus.MODIFIED]
            ])
            acceptance_rate = accepted / len(completed_interventions)
        else:
            acceptance_rate = 0.0
        
        # Calculate average response time
        timed_interventions = [
            i for i in completed_interventions 
            if i.response_time_ms is not None
        ]
        
        if timed_interventions:
            avg_response_time = sum(i.response_time_ms for i in timed_interventions) / len(timed_interventions)
        else:
            avg_response_time = 0.0
        
        self.data["metrics_summary"] = {
            "total_interventions": total,
            "acceptance_rate": acceptance_rate,
            "avg_response_time": avg_response_time,
            "last_updated": time.time()
        }
    
    def get_intervention_analytics(self, user_id: Optional[str] = None) -> Dict[str, Any]:
        """Get comprehensive intervention analytics"""
        interventions = self.data["interventions"]
        
        if user_id:
            # Filter by user context if available
            interventions = [
                i for i in interventions 
                if i.context.get("user_id") == user_id
            ]
        
        if not interventions:
            return {"message": "No interventions recorded"}
        
        # Type distribution
        type_counts = {}
        for intervention in interventions:
            type_key = intervention.intervention_type.value
            type_counts[type_key] = type_counts.get(type_key, 0) + 1
        
        # Acceptance by type
        acceptance_by_type = {}
        for intervention_type in InterventionType:
            type_interventions = [
                i for i in interventions 
                if i.intervention_type == intervention_type and i.acceptance_status != AcceptanceStatus.PENDING
            ]
            
            if type_interventions:
                accepted = len([
                    i for i in type_interventions 
                    if i.acceptance_status in [AcceptanceStatus.ACCEPTED, AcceptanceStatus.MODIFIED]
                ])
                acceptance_by_type[intervention_type.value] = accepted / len(type_interventions)
        
        return {
            "total_interventions": len(interventions),
            "type_distribution": type_counts,
            "acceptance_by_type": acceptance_by_type,
            "overall_metrics": self.data["metrics_summary"],
            "style_profiles_count": len(self.data["style_profiles"]),
            "adaptation_phases": {
                phase: len([p for p in self.data["style_profiles"].values() if p.adaptation_phase == phase])
                for phase in ["learning", "adapting", "stable"]
            }
        }
    
    def should_intervene(
        self,
        intervention_type: InterventionType,
        confidence: float,
        user_id: str = "default"
    ) -> bool:
        """
        Determine if intervention should be triggered based on policies and user profile
        
        Args:
            intervention_type: Type of intervention being considered
            confidence: Confidence level (0.0 to 1.0) that intervention is needed
            user_id: User identifier for style adaptation
            
        Returns:
            True if intervention should proceed
        """
        # Check policy thresholds
        threshold = self.policies["intervention_thresholds"].get(
            intervention_type.value, 0.5
        )
        
        if confidence < threshold:
            return False
        
        # Apply style adaptation if profile exists
        profile = self.get_style_profile(user_id)
        if profile:
            # Adjust threshold based on user tolerance
            adjusted_threshold = threshold * (2.0 - profile.intervention_tolerance)
            
            # Check if intervention type is preferred
            if intervention_type not in profile.preferred_intervention_types:
                adjusted_threshold *= 1.2  # Raise threshold for non-preferred types
            
            return confidence >= adjusted_threshold
        
        return True


def main():
    """CLI interface for intervention metrics"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Intervention Metrics System")
    parser.add_argument("--record", type=str, help="Record intervention (type:context:input:output:reasoning)")
    parser.add_argument("--accept", type=str, help="Record acceptance (intervention_id:status:response)")
    parser.add_argument("--analytics", action="store_true", help="Show intervention analytics")
    parser.add_argument("--user", type=str, default="default", help="User ID for operations")
    parser.add_argument("--create-profile", type=str, help="Create style profile for user")
    parser.add_argument("--test-should-intervene", type=str, help="Test intervention decision (type:confidence)")
    
    args = parser.parse_args()
    
    metrics = InterventionMetrics()
    
    if args.record:
        try:
            parts = args.record.split(":", 4)
            if len(parts) != 5:
                print("❌ Error: Record format should be type:context:input:output:reasoning")
                return
            
            intervention_type = InterventionType(parts[0])
            context = {"type": parts[1], "user_id": args.user}
            original_input = parts[2]
            suggested_output = parts[3] if parts[3] else None
            reasoning = parts[4]
            
            intervention_id = metrics.record_intervention(
                intervention_type, context, original_input, suggested_output, reasoning, args.user
            )
            print(f"📝 Intervention recorded: {intervention_id}")
            
        except ValueError as e:
            print(f"❌ Error: Invalid intervention type: {e}")
    
    elif args.accept:
        try:
            parts = args.accept.split(":", 2)
            if len(parts) < 2:
                print("❌ Error: Accept format should be intervention_id:status[:response]")
                return
            
            intervention_id = parts[0]
            status = AcceptanceStatus(parts[1])
            response = parts[2] if len(parts) > 2 else None
            
            if metrics.record_acceptance(intervention_id, status, response):
                print(f"✅ Acceptance recorded for {intervention_id}: {status.value}")
            else:
                print(f"❌ Error: Intervention {intervention_id} not found")
                
        except ValueError as e:
            print(f"❌ Error: Invalid acceptance status: {e}")
    
    elif args.create_profile:
        profile = metrics.create_style_profile(args.create_profile)
        print(f"👤 Style profile created for {args.create_profile}")
        print(f"   Phase: {profile.adaptation_phase}")
        print(f"   Patience: {profile.patience_level}")
        print(f"   Tolerance: {profile.intervention_tolerance}")
    
    elif args.test_should_intervene:
        try:
            parts = args.test_should_intervene.split(":")
            if len(parts) != 2:
                print("❌ Error: Format should be type:confidence")
                return
                
            intervention_type = InterventionType(parts[0])
            confidence = float(parts[1])
            
            should_intervene = metrics.should_intervene(intervention_type, confidence, args.user)
            print(f"🤔 Should intervene ({intervention_type.value}, {confidence}, {args.user}): {should_intervene}")
            
        except (ValueError, IndexError) as e:
            print(f"❌ Error: {e}")
    
    elif args.analytics:
        analytics = metrics.get_intervention_analytics(args.user if args.user != "default" else None)
        print("📊 Intervention Analytics:")
        print("=" * 30)
        
        for key, value in analytics.items():
            if isinstance(value, dict):
                print(f"{key}:")
                for sub_key, sub_value in value.items():
                    print(f"  {sub_key}: {sub_value}")
            else:
                print(f"{key}: {value}")
    
    else:
        parser.print_help()


if __name__ == "__main__":
    main()