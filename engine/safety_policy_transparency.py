#!/usr/bin/env python3
"""
Safety & Policy Transparency Layer

Provides comprehensive safety event management, policy transparency,
and audit trail capabilities for the v0.8 milestone.

🧙‍♂️ "True safety lies not in secrecy, but in transparent accountability - 
    where every guardian action is both visible and justified." - Bootstrap Sentinel
"""

from __future__ import annotations
import json
import time
import datetime
from pathlib import Path
from typing import Dict, Any, List, Optional, Union
from dataclasses import dataclass, asdict
from enum import Enum

# Import our enhanced intervention system
from intervention_metrics import InterventionType, SafetyEventLevel, AcceptanceStatus, InterventionRecord
from redaction_transforms import RedactionTransforms, RedactionType


class PolicyAction(Enum):
    """Actions taken by policy enforcement"""
    ALLOW = "allow"
    MODIFY = "modify"
    BLOCK = "block"
    ESCALATE = "escalate"
    AUDIT_ONLY = "audit_only"


@dataclass
class SafetyEvent:
    """Comprehensive safety event record"""
    event_id: str
    timestamp: float
    safety_level: SafetyEventLevel
    event_type: str
    context: Dict[str, Any]
    policy_action: PolicyAction
    reasoning: str
    intervention_id: Optional[str] = None
    redaction_metadata: Optional[Dict[str, Any]] = None
    escalation_path: Optional[str] = None
    user_id: str = "default"
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for JSON serialization"""
        data = asdict(self)
        data['safety_level'] = self.safety_level.value
        data['policy_action'] = self.policy_action.value
        return data
    
    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> 'SafetyEvent':
        """Create from dictionary"""
        data['safety_level'] = SafetyEventLevel(data['safety_level'])
        data['policy_action'] = PolicyAction(data['policy_action'])
        return cls(**data)


class SafetyPolicyTransparency:
    """
    Main safety and policy transparency layer
    
    Provides tiered safety events, metadata transparency, audit logging,
    and policy enforcement with full transparency and accountability.
    """
    
    def __init__(
        self,
        audit_log_path: str = "data/safety_audit.jsonl",
        policy_config_path: str = "data/safety_policies.json",
        enable_encrypted_audit: bool = False,
        encryption_key: Optional[str] = None
    ):
        self.audit_log_path = Path(audit_log_path)
        self.policy_config_path = Path(policy_config_path)
        self.redaction_engine = RedactionTransforms()
        
        # Encrypted audit log configuration
        self.enable_encrypted_audit = enable_encrypted_audit
        self.encryption_key = encryption_key
        if enable_encrypted_audit:
            self.encrypted_audit_path = Path(str(audit_log_path).replace('.jsonl', '_encrypted.jsonl'))
            self.encrypted_audit_path.parent.mkdir(exist_ok=True)
        
        # Ensure audit log directory exists
        self.audit_log_path.parent.mkdir(exist_ok=True)
        
        # Load or initialize policy configuration
        self.policies = self._load_safety_policies()
        
        # In-memory cache for recent events (for performance)
        self.recent_events: List[SafetyEvent] = []
        
    def _load_safety_policies(self) -> Dict[str, Any]:
        """Load safety policy configuration"""
        if self.policy_config_path.exists():
            try:
                with open(self.policy_config_path, 'r') as f:
                    return json.load(f)
            except Exception as e:
                print(f"⚠️ Warning: Failed to load safety policies: {e}")
        
        # Default safety policies
        return {
            "safety_thresholds": {
                "notice": 0.1,
                "warn": 0.4,
                "block": 0.7,
                "escalate": 0.9
            },
            "escalation_rules": {
                "high_risk_patterns": [
                    "security_violation",
                    "privacy_breach", 
                    "harmful_content",
                    "policy_violation"
                ],
                "escalation_contacts": ["@guardian", "@overlord"],
                "auto_escalate_threshold": 3
            },
            "transparency_settings": {
                "include_redaction_metadata": True,
                "audit_all_events": True,
                "retention_days": 90,
                "public_summary_enabled": False
            },
            "redaction_policies": {
                "default_mode": "standard",
                "strict_mode_triggers": ["privacy_breach", "pii_exposure"],
                "transparency_exceptions": ["audit_trail", "policy_enforcement"]
            }
        }
    
    def create_safety_event(
        self,
        safety_level: SafetyEventLevel,
        event_type: str,
        context: Dict[str, Any],
        reasoning: str,
        content: Optional[str] = None,
        intervention_id: Optional[str] = None,
        user_id: str = "default"
    ) -> SafetyEvent:
        """
        Create and process a new safety event
        
        Returns the created SafetyEvent with all metadata populated
        """
        event_id = f"safety_{int(time.time() * 1000)}"
        
        # Determine policy action based on safety level and policies
        policy_action = self._determine_policy_action(safety_level, event_type, context)
        
        # Apply redaction if content is provided
        redaction_metadata = None
        if content:
            redacted_content, redaction_meta = self.redaction_engine.apply_redaction(
                content, self._get_redaction_context(safety_level, event_type)
            )
            redaction_metadata = redaction_meta
            
            # Update context with redacted content
            context["original_content_length"] = len(content)
            context["redacted_content_length"] = len(redacted_content)
            context["redaction_applied"] = redaction_meta["redaction_applied"]
        
        # Create safety event
        safety_event = SafetyEvent(
            event_id=event_id,
            timestamp=time.time(),
            safety_level=safety_level,
            event_type=event_type,
            context=context,
            policy_action=policy_action,
            reasoning=reasoning,
            intervention_id=intervention_id,
            redaction_metadata=redaction_metadata,
            user_id=user_id
        )
        
        # Handle escalation if required
        if policy_action == PolicyAction.ESCALATE:
            safety_event.escalation_path = self._initiate_escalation(safety_event)
        
        # Log to audit trail
        self._append_to_audit_log(safety_event)
        
        # Cache recent event
        self.recent_events.append(safety_event)
        if len(self.recent_events) > 100:  # Keep last 100 events in memory
            self.recent_events.pop(0)
        
        return safety_event
    
    def _determine_policy_action(
        self,
        safety_level: SafetyEventLevel,
        event_type: str,
        context: Dict[str, Any]
    ) -> PolicyAction:
        """Determine policy action based on safety level and configuration"""
        
        # Check for high-risk patterns that trigger immediate escalation
        high_risk_patterns = self.policies.get("escalation_rules", {}).get("high_risk_patterns", [])
        if any(pattern in event_type.lower() for pattern in high_risk_patterns):
            return PolicyAction.ESCALATE
        
        # Standard level-based actions
        if safety_level == SafetyEventLevel.NOTICE:
            return PolicyAction.AUDIT_ONLY
        elif safety_level == SafetyEventLevel.WARN:
            return PolicyAction.MODIFY
        elif safety_level == SafetyEventLevel.BLOCK:
            return PolicyAction.BLOCK
        elif safety_level == SafetyEventLevel.ESCALATE:
            return PolicyAction.ESCALATE
        
        return PolicyAction.ALLOW
    
    def _get_redaction_context(
        self,
        safety_level: SafetyEventLevel,
        event_type: str
    ) -> Dict[str, Any]:
        """Get redaction context based on safety level and event type"""
        redaction_policies = self.policies.get("redaction_policies", {})
        
        # Determine redaction mode
        safety_mode = "standard"
        if safety_level in [SafetyEventLevel.BLOCK, SafetyEventLevel.ESCALATE]:
            safety_mode = "strict"
        
        strict_triggers = redaction_policies.get("strict_mode_triggers", [])
        if any(trigger in event_type.lower() for trigger in strict_triggers):
            safety_mode = "strict"
        
        return {
            "safety_mode": safety_mode,
            "safety_level": safety_level.value,
            "event_type": event_type
        }
    
    def _initiate_escalation(self, safety_event: SafetyEvent) -> str:
        """Initiate escalation process for high-priority safety events"""
        escalation_id = f"esc_{safety_event.event_id}"
        
        escalation_contacts = self.policies.get("escalation_rules", {}).get("escalation_contacts", [])
        
        # Log escalation
        escalation_entry = {
            "escalation_id": escalation_id,
            "safety_event_id": safety_event.event_id,
            "timestamp": time.time(),
            "contacts_notified": escalation_contacts,
            "escalation_reason": safety_event.reasoning,
            "status": "initiated"
        }
        
        # This would trigger actual notifications in a real system
        print(f"🚨 ESCALATION INITIATED: {escalation_id} - {safety_event.reasoning}")
        
        return escalation_id
    
    def _append_to_audit_log(self, safety_event: SafetyEvent):
        """Append safety event to audit log with optional encryption"""
        try:
            # Generate transparency metadata
            transparency_metadata = self._generate_transparency_metadata(safety_event)
            
            # Create audit entry
            audit_entry = {
                "timestamp": safety_event.timestamp,
                "event_type": "safety_event",
                "event_data": safety_event.to_dict(),
                "transparency_metadata": transparency_metadata
            }
            
            # Standard audit log (always written)
            with open(self.audit_log_path, 'a') as f:
                f.write(json.dumps(audit_entry, ensure_ascii=False) + '\n')
            
            # Encrypted audit log (if enabled)
            if self.enable_encrypted_audit:
                self._append_to_encrypted_audit_log(audit_entry)
                
        except Exception as e:
            print(f"❌ Error: Failed to write to audit log: {e}")
    
    def _append_to_encrypted_audit_log(self, audit_entry: Dict[str, Any]):
        """Append audit entry to encrypted log (placeholder implementation)"""
        try:
            # For now, this is a placeholder for proper encryption
            # In a full implementation, this would use proper encryption libraries
            encrypted_entry = {
                "timestamp": audit_entry["timestamp"],
                "encrypted_data": f"[ENCRYPTED:{hash(str(audit_entry))}]",
                "encryption_metadata": {
                    "algorithm": "placeholder",
                    "key_id": "default" if not self.encryption_key else hash(self.encryption_key),
                    "encrypted_size": len(str(audit_entry))
                }
            }
            
            with open(self.encrypted_audit_path, 'a') as f:
                f.write(json.dumps(encrypted_entry, ensure_ascii=False) + '\n')
                
        except Exception as e:
            print(f"❌ Error: Failed to write to encrypted audit log: {e}")
    
    def _generate_transparency_metadata(self, safety_event: SafetyEvent) -> Dict[str, Any]:
        """Generate transparency metadata for audit entry"""
        is_automated = safety_event.policy_action != PolicyAction.ESCALATE
        requires_human = safety_event.policy_action == PolicyAction.ESCALATE
        
        return {
            "policy_version": self.policies.get("policy_versioning", {}).get("version", "1.0.0"),
            "transparency_level": "full" if safety_event.safety_level != SafetyEventLevel.ESCALATE else "redacted",
            "automated_action": is_automated,
            "human_oversight_required": requires_human,
            "audit_trail_complete": True
        }
    
    def get_safety_analytics(
        self,
        time_window_hours: int = 24,
        user_id: Optional[str] = None
    ) -> Dict[str, Any]:
        """Get safety analytics for specified time window"""
        cutoff_time = time.time() - (time_window_hours * 3600)
        
        # Filter recent events
        relevant_events = [
            event for event in self.recent_events
            if event.timestamp >= cutoff_time and (not user_id or event.user_id == user_id)
        ]
        
        # Calculate analytics
        total_events = len(relevant_events)
        events_by_level = {}
        events_by_action = {}
        
        for event in relevant_events:
            level = event.safety_level.value
            action = event.policy_action.value
            
            events_by_level[level] = events_by_level.get(level, 0) + 1
            events_by_action[action] = events_by_action.get(action, 0) + 1
        
        return {
            "time_window_hours": time_window_hours,
            "total_safety_events": total_events,
            "events_by_safety_level": events_by_level,
            "events_by_policy_action": events_by_action,
            "escalation_rate": events_by_action.get("escalate", 0) / max(total_events, 1),
            "block_rate": events_by_action.get("block", 0) / max(total_events, 1),
            "user_id": user_id,
            "analysis_timestamp": time.time()
        }
    
    def create_transparency_report(
        self,
        start_time: Optional[float] = None,
        end_time: Optional[float] = None
    ) -> Dict[str, Any]:
        """Create comprehensive transparency report"""
        if not start_time:
            start_time = time.time() - (24 * 3600)  # Last 24 hours
        if not end_time:
            end_time = time.time()
        
        # Read audit log entries in time range
        audit_entries = self._read_audit_log_range(start_time, end_time)
        
        return {
            "report_metadata": {
                "start_time": start_time,
                "end_time": end_time,
                "report_generated": time.time(),
                "total_audit_entries": len(audit_entries)
            },
            "safety_summary": self._summarize_safety_events(audit_entries),
            "policy_enforcement_summary": self._summarize_policy_actions(audit_entries),
            "transparency_notes": self._generate_transparency_report_notes(audit_entries)
        }
    
    def _read_audit_log_range(
        self,
        start_time: float,
        end_time: float
    ) -> List[Dict[str, Any]]:
        """Read audit log entries within time range"""
        entries = []
        
        try:
            if self.audit_log_path.exists():
                with open(self.audit_log_path, 'r') as f:
                    for line in f:
                        try:
                            entry = json.loads(line.strip())
                            if start_time <= entry.get("timestamp", 0) <= end_time:
                                entries.append(entry)
                        except json.JSONDecodeError:
                            continue
        except Exception as e:
            print(f"⚠️ Warning: Error reading audit log: {e}")
        
        return entries
    
    def _summarize_safety_events(self, audit_entries: List[Dict[str, Any]]) -> Dict[str, Any]:
        """Summarize safety events from audit entries"""
        safety_events = [
            entry for entry in audit_entries
            if entry.get("event_type") == "safety_event"
        ]
        
        summary = {
            "total_safety_events": len(safety_events),
            "events_by_level": {},
            "events_by_type": {},
            "redaction_statistics": {
                "events_with_redaction": 0,
                "total_redactions": 0
            }
        }
        
        for entry in safety_events:
            event_data = entry.get("event_data", {})
            
            # Count by safety level
            safety_level = event_data.get("safety_level")
            if safety_level:
                summary["events_by_level"][safety_level] = summary["events_by_level"].get(safety_level, 0) + 1
            
            # Count by event type
            event_type = event_data.get("event_type")
            if event_type:
                summary["events_by_type"][event_type] = summary["events_by_type"].get(event_type, 0) + 1
            
            # Redaction statistics
            redaction_meta = event_data.get("redaction_metadata")
            if redaction_meta and redaction_meta.get("redaction_applied"):
                summary["redaction_statistics"]["events_with_redaction"] += 1
                summary["redaction_statistics"]["total_redactions"] += sum(
                    redaction_meta.get("redaction_counts", {}).values()
                )
        
        return summary
    
    def _summarize_policy_actions(self, audit_entries: List[Dict[str, Any]]) -> Dict[str, Any]:
        """Summarize policy actions from audit entries"""
        safety_events = [
            entry for entry in audit_entries
            if entry.get("event_type") == "safety_event"
        ]
        
        actions_summary = {}
        escalations = []
        
        for entry in safety_events:
            event_data = entry.get("event_data", {})
            policy_action = event_data.get("policy_action")
            
            if policy_action:
                actions_summary[policy_action] = actions_summary.get(policy_action, 0) + 1
            
            if policy_action == "escalate":
                escalations.append({
                    "event_id": event_data.get("event_id"),
                    "escalation_path": event_data.get("escalation_path"),
                    "reasoning": event_data.get("reasoning")
                })
        
        return {
            "policy_actions_summary": actions_summary,
            "escalations": escalations,
            "total_escalations": len(escalations)
        }
    
    def _generate_transparency_report_notes(self, audit_entries: List[Dict[str, Any]]) -> List[str]:
        """Generate human-readable transparency notes"""
        notes = []
        
        total_events = len([e for e in audit_entries if e.get("event_type") == "safety_event"])
        
        if total_events == 0:
            notes.append("No safety events recorded in the specified time period")
        else:
            notes.append(f"Comprehensive audit trail maintained for {total_events} safety events")
            notes.append("All policy actions are logged with full transparency")
            notes.append("Redaction metadata preserved for content protection accountability")
            
            escalations = sum(1 for entry in audit_entries 
                            if entry.get("event_data", {}).get("policy_action") == "escalate")
            if escalations > 0:
                notes.append(f"{escalations} events escalated to human oversight")
        
        return notes


def main():
    """CLI interface for safety policy transparency testing"""
    print("🛡️ Safety & Policy Transparency Layer Demo")
    print("=" * 50)
    
    safety_layer = SafetyPolicyTransparency()
    
    # Demo scenarios
    test_scenarios = [
        {
            "level": SafetyEventLevel.NOTICE,
            "type": "content_guidance",
            "context": {"user_action": "code_generation", "confidence": 0.8},
            "reasoning": "Generated code includes best practices guidance",
            "content": "def process_user_data(email, phone): return sanitize(email, phone)"
        },
        {
            "level": SafetyEventLevel.WARN,
            "type": "potential_pii_exposure",
            "context": {"user_action": "data_processing", "risk_level": "medium"},
            "reasoning": "Detected potential PII in user input",
            "content": "User data: john.doe@example.com, phone: (555) 123-4567"
        },
        {
            "level": SafetyEventLevel.BLOCK,
            "type": "policy_violation",
            "context": {"user_action": "content_generation", "violation_type": "unsafe_content"},
            "reasoning": "Content generation request violates safety policies",
            "content": "Sensitive content blocked for safety"
        },
        {
            "level": SafetyEventLevel.ESCALATE,
            "type": "security_violation",
            "context": {"user_action": "system_access", "severity": "high"},
            "reasoning": "Detected potential security breach requiring immediate human review",
            "content": "Security incident detected - escalating to security team"
        }
    ]
    
    created_events = []
    
    for i, scenario in enumerate(test_scenarios, 1):
        print(f"\n🔍 Scenario {i}: {scenario['type']} ({scenario['level'].value})")
        
        event = safety_layer.create_safety_event(
            safety_level=scenario['level'],
            event_type=scenario['type'],
            context=scenario['context'],
            reasoning=scenario['reasoning'],
            content=scenario['content'],
            user_id=f"demo_user_{i}"
        )
        
        created_events.append(event)
        
        print(f"   Event ID: {event.event_id}")
        print(f"   Policy Action: {event.policy_action.value}")
        print(f"   Redaction Applied: {event.redaction_metadata['redaction_applied'] if event.redaction_metadata else False}")
        if event.escalation_path:
            print(f"   Escalation: {event.escalation_path}")
    
    # Show analytics
    print("\n📊 Safety Analytics (Last 24 hours)")
    analytics = safety_layer.get_safety_analytics()
    print(f"   Total Events: {analytics['total_safety_events']}")
    print(f"   By Level: {analytics['events_by_safety_level']}")
    print(f"   By Action: {analytics['events_by_policy_action']}")
    print(f"   Escalation Rate: {analytics['escalation_rate']:.2%}")
    
    # Generate transparency report
    print("\n📝 Transparency Report")
    report = safety_layer.create_transparency_report()
    print(f"   Audit Entries: {report['report_metadata']['total_audit_entries']}")
    print(f"   Safety Events: {report['safety_summary']['total_safety_events']}")
    print(f"   Escalations: {report['policy_enforcement_summary']['total_escalations']}")
    
    print("\n✅ Demo completed - Check data/safety_audit.jsonl for full audit trail")


if __name__ == "__main__":
    main()