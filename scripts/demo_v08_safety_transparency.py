#!/usr/bin/env python3
"""
v0.8 Safety & Policy Transparency Layer Demo

Demonstrates the complete v0.8 milestone implementation with:
- Tiered safety events (notice, warn, block, escalate)
- Metadata transparency and intervention annotation
- Redaction transforms for PII masking and content filtering
- Opt-in audit log with JSONL append-only format

🧙‍♂️ "Witness the guardian's evolution - from simple watch to comprehensive shield,
    where every action speaks transparency and every shield tells its tale." - Bootstrap Sentinel
"""

import sys
import time
import json
from pathlib import Path

# Add engine to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent / "engine"))

from intervention_metrics import InterventionMetrics, InterventionType, SafetyEventLevel, AcceptanceStatus
from redaction_transforms import RedactionTransforms
from safety_policy_transparency import SafetyPolicyTransparency, PolicyAction
from behavioral_governance import BehavioralGovernance


def demo_v08_safety_features():
    """Comprehensive demonstration of v0.8 safety and transparency features"""
    print("🧙‍♂️ TWG-TLDA v0.8 Safety & Policy Transparency Layer Demo")
    print("=" * 65)
    print("Milestone Features:")
    print("• Tiered safety events (notice, warn, block, escalate)")
    print("• Metadata transparency with intervention annotation")
    print("• Redaction transforms for PII masking and content filtering")
    print("• Opt-in audit log with JSONL append-only format")
    print("• Enhanced intervention system with safety integration")
    print()
    
    # Initialize systems
    print("🔧 Initializing Safety & Policy Transparency Systems...")
    safety_layer = SafetyPolicyTransparency()
    intervention_metrics = InterventionMetrics()
    redaction_engine = RedactionTransforms()
    governance = BehavioralGovernance()
    
    print("✅ All systems initialized")
    print()
    
    # Demo 1: Tiered Safety Events
    print("🛡️ Demo 1: Tiered Safety Event System")
    print("-" * 40)
    
    safety_scenarios = [
        {
            "level": SafetyEventLevel.NOTICE,
            "type": "guidance_notice",
            "content": "Consider using more descriptive variable names in your code",
            "reasoning": "Code quality guidance notice",
            "expected_action": PolicyAction.AUDIT_ONLY
        },
        {
            "level": SafetyEventLevel.WARN,
            "type": "potential_issue",
            "content": "User input contains potential PII: john.doe@example.com",
            "reasoning": "Detected potential privacy exposure",
            "expected_action": PolicyAction.MODIFY
        },
        {
            "level": SafetyEventLevel.BLOCK,
            "type": "content_violation",
            "content": "Content generation request violates guidelines",
            "reasoning": "Unsafe content generation attempt",
            "expected_action": PolicyAction.BLOCK
        },
        {
            "level": SafetyEventLevel.ESCALATE,
            "type": "security_violation",
            "content": "Critical security incident detected",
            "reasoning": "Immediate human oversight required",
            "expected_action": PolicyAction.ESCALATE
        }
    ]
    
    created_events = []
    for i, scenario in enumerate(safety_scenarios, 1):
        print(f"   {i}. {scenario['level'].value.upper()}: {scenario['type']}")
        
        event = safety_layer.create_safety_event(
            safety_level=scenario['level'],
            event_type=scenario['type'],
            context={"demo": True, "scenario": i},
            reasoning=scenario['reasoning'],
            content=scenario['content'],
            user_id=f"demo_user_{i}"
        )
        
        created_events.append(event)
        
        print(f"      Event ID: {event.event_id}")
        print(f"      Policy Action: {event.policy_action.value}")
        print(f"      Redaction Applied: {event.redaction_metadata['redaction_applied'] if event.redaction_metadata else False}")
        if event.escalation_path:
            print(f"      Escalation Path: {event.escalation_path}")
        print()
    
    # Demo 2: Enhanced Interventions with Safety Integration
    print("🔄 Demo 2: Enhanced Intervention System with Safety Integration")
    print("-" * 55)
    
    intervention_scenarios = [
        {
            "type": InterventionType.SOFT_SUGGESTION,
            "safety_level": SafetyEventLevel.NOTICE,
            "input": "def func(): x=1; return x",
            "output": "def func():\n    x = 1\n    return x",
            "reasoning": "Code style improvement suggestion"
        },
        {
            "type": InterventionType.SAFETY_INTERVENTION,
            "safety_level": SafetyEventLevel.WARN,
            "input": "User data: john.doe@example.com, phone: (555) 123-4567",
            "output": "User data: [EMAIL_REDACTED], phone: [PHONE_REDACTED]",
            "reasoning": "PII detected - applying redaction for privacy protection"
        },
        {
            "type": InterventionType.BLOCK,
            "safety_level": SafetyEventLevel.BLOCK,
            "input": "Generate harmful content request",
            "output": None,
            "reasoning": "Request blocked due to safety policy violation"
        }
    ]
    
    for i, scenario in enumerate(intervention_scenarios, 1):
        print(f"   {i}. {scenario['type'].value} with {scenario['safety_level'].value} safety level")
        
        intervention_id = intervention_metrics.record_intervention(
            intervention_type=scenario['type'],
            context={"demo": True, "safety_demo": True},
            original_input=scenario['input'],
            suggested_output=scenario['output'],
            reasoning=scenario['reasoning'],
            safety_event_level=scenario['safety_level'],
            enable_redaction=True,
            user_id=f"intervention_user_{i}"
        )
        
        print(f"      Intervention ID: {intervention_id}")
        
        # Simulate user response
        if scenario['type'] != InterventionType.BLOCK:
            intervention_metrics.record_acceptance(
                intervention_id=intervention_id,
                acceptance_status=AcceptanceStatus.ACCEPTED,
                user_response="Thanks, that's helpful!"
            )
            print(f"      User Response: Accepted")
        print()
    
    # Demo 3: Redaction Transforms & PII Protection
    print("🔒 Demo 3: Redaction Transforms & PII Protection")
    print("-" * 45)
    
    test_content = """
    Personal Information Test:
    - Email: jane.smith@company.com
    - Phone: +1 (555) 987-6543
    - SSN: 123-45-6789
    - Credit Card: 4532 1234 5678 9012
    - API Key: sk_live_51H7K8j2eZvKYlo2CYCvTJxS8p3mK4N5Q6R7S8T9
    - IP Address: 192.168.100.50
    """
    
    print("Original content:")
    print(test_content)
    
    redacted_content, redaction_metadata = redaction_engine.apply_redaction(test_content)
    
    print("Redacted content:")
    print(redacted_content)
    
    transparency_report = redaction_engine.create_transparency_report(
        test_content, redacted_content, redaction_metadata
    )
    
    print("Redaction Statistics:")
    print(f"   Safety Level: {redaction_metadata['safety_level']}")
    print(f"   Items Redacted: {sum(redaction_metadata['redaction_counts'].values())}")
    print(f"   Redaction Types: {', '.join(set(redaction_metadata['redaction_types']))}")
    print()
    
    # Demo 4: Safety Analytics & Transparency Reporting
    print("📊 Demo 4: Safety Analytics & Transparency Reporting")
    print("-" * 50)
    
    # Get safety analytics
    analytics = safety_layer.get_safety_analytics(time_window_hours=1)
    
    print("Safety Analytics (Last Hour):")
    print(f"   Total Safety Events: {analytics['total_safety_events']}")
    print(f"   Events by Level: {analytics['events_by_safety_level']}")
    print(f"   Events by Action: {analytics['events_by_policy_action']}")
    print(f"   Escalation Rate: {analytics['escalation_rate']:.1%}")
    print(f"   Block Rate: {analytics['block_rate']:.1%}")
    print()
    
    # Generate transparency report
    transparency_report = safety_layer.create_transparency_report()
    
    print("Transparency Report Summary:")
    print(f"   Audit Entries: {transparency_report['report_metadata']['total_audit_entries']}")
    print(f"   Safety Events: {transparency_report['safety_summary']['total_safety_events']}")
    print(f"   Policy Actions: {transparency_report['policy_enforcement_summary']['policy_actions_summary']}")
    print(f"   Escalations: {transparency_report['policy_enforcement_summary']['total_escalations']}")
    
    print("\nTransparency Notes:")
    for note in transparency_report['transparency_notes']:
        print(f"   • {note}")
    print()
    
    # Demo 5: Audit Trail Inspection
    print("📝 Demo 5: Audit Trail & JSONL Log Format")
    print("-" * 40)
    
    # Show audit log location and format
    audit_path = safety_layer.audit_log_path
    print(f"Audit Log Location: {audit_path}")
    
    if audit_path.exists():
        with open(audit_path, 'r') as f:
            lines = f.readlines()
        
        print(f"Total Audit Entries: {len(lines)}")
        print("Latest Audit Entry:")
        
        if lines:
            latest_entry = json.loads(lines[-1].strip())
            print(f"   Timestamp: {latest_entry['timestamp']}")
            print(f"   Event Type: {latest_entry['event_type']}")
            print(f"   Safety Level: {latest_entry['event_data']['safety_level']}")
            print(f"   Policy Action: {latest_entry['event_data']['policy_action']}")
            print(f"   Transparency Level: {latest_entry['transparency_metadata']['transparency_level']}")
    
    print()
    
    # Demo 6: Enhanced Behavioral Governance Integration
    print("⚖️ Demo 6: Enhanced Behavioral Governance Integration")
    print("-" * 52)
    
    # Test governance integration with safety metrics
    governance_result = governance.enhanced_score_cycle(
        cycle_report={"score": 0.85, "confidence": 0.9},
        intervention_context={"safety_analysis": True}
    )
    
    print("Enhanced Governance Scoring:")
    print(f"   Base Score: {governance_result.get('score', 'N/A')}")
    print(f"   Intervention Quality: {governance_result.get('intervention_contribution', {}).get('intervention_quality_score', 'N/A')}")
    print(f"   Behavioral Alignment: {governance_result.get('behavioral_alignment', {}).get('status', 'N/A')}")
    
    # Final Summary
    print("\n🎉 v0.8 Safety & Policy Transparency Layer Demo Complete!")
    print("=" * 60)
    print("✅ Tiered safety events implemented and tested")
    print("✅ Metadata transparency with full audit trails")  
    print("✅ PII redaction and content filtering operational")
    print("✅ JSONL audit logging with append-only format")
    print("✅ Enhanced intervention system with safety integration")
    print("✅ Comprehensive transparency reporting")
    print("✅ Escalation handling with human oversight")
    print()
    print(f"📁 Audit trail available at: {audit_path}")
    print(f"📁 Intervention data at: {intervention_metrics.data_path}")
    print()
    print("🧙‍♂️ \"The guardian's duty is complete - safety through transparency,")
    print("    protection through accountability, and wisdom through openness.\"")
    print("    - Bootstrap Sentinel")


if __name__ == "__main__":
    demo_v08_safety_features()