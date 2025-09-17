#!/usr/bin/env python3
"""
Behavioral Alignment Demo - Test Script

Demonstrates the v0.4 Behavioral Alignment & Intervention Metrics system
with real scenarios and automated testing.

🧙‍♂️ "Behold! The system that learns from its own wisdom - 
    measuring intervention as keenly as inspiration." - Bootstrap Sentinel
"""

import sys
import time
import json
from pathlib import Path

# Add engine to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent / "engine"))

from intervention_metrics import InterventionMetrics, InterventionType, AcceptanceStatus
from behavioral_governance import BehavioralGovernance


def demo_intervention_lifecycle():
    """Demonstrate complete intervention lifecycle"""
    print("🧠 Demo: Complete Intervention Lifecycle")
    print("=" * 45)
    
    metrics = InterventionMetrics()
    governance = BehavioralGovernance()
    
    # Create a test user profile
    print("👤 Creating user profile for 'demo_user'...")
    profile = metrics.create_style_profile("demo_user")
    print(f"   Phase: {profile.adaptation_phase}")
    print(f"   Patience: {profile.patience_level}")
    print(f"   Tolerance: {profile.intervention_tolerance}")
    
    # Test different intervention scenarios
    scenarios = [
        {
            "type": InterventionType.SOFT_SUGGESTION,
            "context": {"type": "code_style", "user_id": "demo_user"},
            "original": "def func(): x=1; return x",
            "suggested": "def func():\n    x = 1\n    return x",
            "reasoning": "Code style improvement for readability",
            "acceptance": AcceptanceStatus.ACCEPTED
        },
        {
            "type": InterventionType.REWRITE,
            "context": {"type": "error_correction", "user_id": "demo_user"},
            "original": "print('Hello World'",
            "suggested": "print('Hello World')",
            "reasoning": "Missing closing parenthesis",
            "acceptance": AcceptanceStatus.ACCEPTED
        },
        {
            "type": InterventionType.SOFT_SUGGESTION,
            "context": {"type": "optimization", "user_id": "demo_user"},
            "original": "for i in range(len(items)): process(items[i])",
            "suggested": "for item in items: process(item)",
            "reasoning": "More Pythonic iteration pattern",
            "acceptance": AcceptanceStatus.REJECTED
        },
        {
            "type": InterventionType.STYLE_GUIDANCE,
            "context": {"type": "naming", "user_id": "demo_user"},
            "original": "def f(x): return x*2",
            "suggested": "def double_value(number): return number * 2",
            "reasoning": "Descriptive function and parameter names",
            "acceptance": AcceptanceStatus.MODIFIED
        }
    ]
    
    print("\n📝 Recording interventions...")
    intervention_ids = []
    
    for i, scenario in enumerate(scenarios):
        print(f"   {i+1}. {scenario['type'].value}: {scenario['reasoning']}")
        
        intervention_id = metrics.record_intervention(
            scenario["type"],
            scenario["context"],
            scenario["original"],
            scenario["suggested"],
            scenario["reasoning"],
            "demo_user"
        )
        intervention_ids.append((intervention_id, scenario["acceptance"]))
        
        # Small delay to simulate real-world timing
        time.sleep(0.1)
    
    print("\n✅ Recording user responses...")
    for intervention_id, acceptance in intervention_ids:
        response = {
            AcceptanceStatus.ACCEPTED: "Thanks! Applied the suggestion.",
            AcceptanceStatus.REJECTED: "I prefer my current approach.",
            AcceptanceStatus.MODIFIED: "Good idea, but I made some tweaks."
        }.get(acceptance, "No response")
        
        metrics.record_acceptance(intervention_id, acceptance, response)
        print(f"   {intervention_id}: {acceptance.value}")
    
    print("\n📊 Intervention Analytics:")
    analytics = metrics.get_intervention_analytics("demo_user")
    
    print(f"   Total interventions: {analytics['total_interventions']}")
    print(f"   Overall acceptance rate: {analytics['overall_metrics']['acceptance_rate']:.1%}")
    print("   Acceptance by type:")
    for int_type, rate in analytics['acceptance_by_type'].items():
        print(f"     {int_type}: {rate:.1%}")
    
    print("\n🧠 Behavioral Insights:")
    insights = governance.get_behavioral_insights("demo_user")
    alignment = insights['behavioral_alignment']
    print(f"   Status: {alignment['status']}")
    print(f"   Message: {alignment['message']}")
    print("   Recommendations:")
    for rec in alignment['recommendations']:
        print(f"     • {rec}")
    
    return analytics, insights


def demo_policy_injection():
    """Demonstrate policy injection and should_intervene logic"""
    print("\n🔧 Demo: Policy Injection & Decision Making")
    print("=" * 45)
    
    metrics = InterventionMetrics()
    
    print("📋 Current intervention thresholds:")
    for int_type, threshold in metrics.policies["intervention_thresholds"].items():
        print(f"   {int_type}: {threshold}")
    
    print("\n🤔 Testing intervention decisions:")
    test_cases = [
        (InterventionType.SOFT_SUGGESTION, 0.2, "Below threshold"),
        (InterventionType.SOFT_SUGGESTION, 0.5, "Above threshold"),
        (InterventionType.REWRITE, 0.4, "Below threshold"),
        (InterventionType.REWRITE, 0.8, "Above threshold"),
        (InterventionType.BLOCK, 0.7, "Below threshold"),
        (InterventionType.BLOCK, 0.95, "Above threshold")
    ]
    
    for int_type, confidence, expected in test_cases:
        should_intervene = metrics.should_intervene(int_type, confidence, "demo_user")
        result = "✅ INTERVENE" if should_intervene else "❌ SKIP"
        print(f"   {int_type.value} @ {confidence}: {result} ({expected})")


def demo_reflective_loops():
    """Demonstrate reflective loop templates"""
    print("\n🔄 Demo: Reflective Loop Templates")
    print("=" * 40)
    
    metrics = InterventionMetrics()
    
    print("📜 Testing reflective templates:")
    
    # Test each template type
    test_scenarios = [
        {
            "type": InterventionType.SOFT_SUGGESTION,
            "reasoning": "variable naming could be improved",
            "context": {"type": "code_review", "detected_pattern": "unclear naming"},
            "original": "x = get_data()",
            "suggested": "user_data = get_data()"
        },
        {
            "type": InterventionType.BLOCK,
            "reasoning": "potential security vulnerability",
            "context": {"type": "security_check", "safety_concern": "SQL injection risk", "risk_level": "high"},
            "original": "SELECT * FROM users WHERE id = " + "user_input",
            "suggested": None
        }
    ]
    
    for scenario in test_scenarios:
        reflection = metrics._apply_reflective_template(
            scenario["type"],
            scenario["reasoning"],
            scenario["context"],
            scenario["original"],
            scenario["suggested"]
        )
        
        print(f"\n{scenario['type'].value}:")
        print(f"   {reflection}")


def demo_style_adaptation():
    """Demonstrate style adaptation over time"""
    print("\n🎯 Demo: Style Adaptation")
    print("=" * 30)
    
    metrics = InterventionMetrics()
    
    # Create profiles in different phases
    profiles = {
        "new_user": metrics.create_style_profile("new_user"),
        "learning_user": metrics.create_style_profile("learning_user"),
        "experienced_user": metrics.create_style_profile("experienced_user")
    }
    
    # Simulate different adaptation phases
    profiles["learning_user"].adaptation_phase = "adapting"
    profiles["learning_user"].patience_level = 0.3
    profiles["learning_user"].intervention_tolerance = 0.7
    
    profiles["experienced_user"].adaptation_phase = "stable"
    profiles["experienced_user"].patience_level = 0.8
    profiles["experienced_user"].intervention_tolerance = 0.4
    profiles["experienced_user"].preferred_intervention_types = [InterventionType.SOFT_SUGGESTION]
    
    print("👥 User profiles:")
    for user_id, profile in profiles.items():
        print(f"   {user_id}:")
        print(f"     Phase: {profile.adaptation_phase}")
        print(f"     Patience: {profile.patience_level}")
        print(f"     Tolerance: {profile.intervention_tolerance}")
        print(f"     Preferred types: {[t.value for t in profile.preferred_intervention_types]}")
    
    print("\n🔍 Intervention decisions for different users:")
    test_intervention = (InterventionType.REWRITE, 0.7)
    
    for user_id in profiles.keys():
        should_intervene = metrics.should_intervene(test_intervention[0], test_intervention[1], user_id)
        result = "✅ YES" if should_intervene else "❌ NO"
        print(f"   {user_id}: {result} (rewrite @ 0.7 confidence)")


def demo_enhanced_governance():
    """Demonstrate enhanced governance integration"""
    print("\n⚖️ Demo: Enhanced Governance Integration")
    print("=" * 45)
    
    governance = BehavioralGovernance()
    
    # Create a mock cycle report
    cycle_report = {
        "cycle_id": "demo_cycle_001",
        "molten_glyphs": [{"id": "glyph_1"}, {"id": "glyph_2"}],
        "mist_count": 5,
        "top_rooms": [{"id": "room_1"}, {"id": "room_2"}]
    }
    
    print("📊 Enhanced cycle scoring:")
    enhanced_score = governance.enhanced_score_cycle(cycle_report)
    
    print(f"   Base score: {enhanced_score.get('score', 'N/A')}")
    print(f"   Assessment: {enhanced_score.get('assessment', 'N/A')}")
    
    if "intervention_analytics" in enhanced_score:
        analytics = enhanced_score["intervention_analytics"]
        print(f"   Total interventions: {analytics.get('total_interventions', 0)}")
        print(f"   Acceptance rate: {analytics.get('overall_metrics', {}).get('acceptance_rate', 0):.1%}")
    
    print("\n🔍 Response filtering test:")
    test_response = {
        "response_text": "OK",  # Very short response
        "confidence": 0.2  # Low confidence
    }
    
    filtered = governance.enhanced_filter_response(test_response, "demo_user")
    print(f"   Original: {test_response}")
    print(f"   Filtered: {filtered}")
    
    if "intervention_metadata" in filtered:
        meta = filtered["intervention_metadata"]
        print(f"   Intervention triggered: {meta['intervention_type']}")
        print(f"   Reasoning: {meta['reasoning']}")


def main():
    """Run all demos"""
    print("🧙‍♂️ Behavioral Alignment & Intervention Metrics Demo")
    print("=" * 55)
    print("This demo showcases the v0.4 milestone features:")
    print("• Intervention classification (soft suggestion, rewrite, block)")
    print("• Acceptance tracking (user response monitoring)")  
    print("• Style adaptation (patience trend + phase)")
    print("• Reflective loop templates (intervention reasoning)")
    print("• Policy injection (JSON rule sets)")
    print("")
    
    try:
        demo_intervention_lifecycle()
        demo_policy_injection()
        demo_reflective_loops()
        demo_style_adaptation()
        demo_enhanced_governance()
        
        print("\n🎉 Demo completed successfully!")
        print("📁 Intervention data saved to: data/intervention_metrics.json")
        print("⚙️ Policies loaded from: data/intervention_policies.json")
        
    except Exception as e:
        print(f"\n❌ Demo error: {e}")
        import traceback
        traceback.print_exc()


if __name__ == "__main__":
    main()