#!/usr/bin/env python3
"""
TLDA-Warbler-Gemma3 Integration Demo
Shows how TLDA, Warbler (decision engine), and Gemma3 (LLM) work together
"""

import sys
import os

# Add the scripts directory to path
script_dir = os.path.dirname(os.path.abspath(__file__))
sys.path.append(script_dir)

# Import the bridge class directly
import subprocess
import tempfile

class WarblerGemma3Bridge:
    """Bridge between TLDA/Warbler and Docker Model Runner Gemma3"""
    
    def __init__(self):
        self.model_name = "ai/gemma3"
        
    def send_prompt(self, prompt, context=""):
        """Send a prompt to Gemma3 and get response"""
        # Simulate Gemma3 response for demo
        response = f"[Gemma3] For '{prompt[:30]}...': I recommend a modular approach with clear separation of concerns."
        return response
            
    def warbler_decision_assist(self, decision_context, options):
        """Use Gemma3 to assist Warbler's decision making"""
        return f"[Gemma3 Decision] For {len(options)} options, I recommend: {options[0]} - it offers the best balance of flexibility and maintainability."

class TLDAWarblerSystem:
    """Main TLDA system with Warbler decision engine and Gemma3 AI assistant"""
    
    def __init__(self):
        self.ai_bridge = WarblerGemma3Bridge()
        self.session_context = []
        
    def log_activity(self, activity, result):
        """Log activity for TLDL entry generation"""
        entry = {
            "activity": activity,
            "result": result,
            "timestamp": "2025-09-10"  # Would be actual timestamp
        }
        self.session_context.append(entry)
        print(f"📜 TLDL: {activity} -> {result[:50]}...")
        
    def warbler_decide(self, decision_point, options, context=""):
        """Warbler decision-making with AI assistance"""
        print(f"🧠 Warbler Decision Point: {decision_point}")
        
        # Get AI assistance for the decision
        ai_recommendation = self.ai_bridge.warbler_decision_assist(
            f"{context} Decision: {decision_point}",
            options
        )
        
        # Warbler processes the recommendation (simplified)
        decision = f"Selected: {options[0]} (with AI guidance)"
        
        self.log_activity(
            f"Decision: {decision_point}", 
            f"{decision} | AI: {ai_recommendation[:100]}"
        )
        
        return decision, ai_recommendation
    
    def process_development_task(self, task_description):
        """Process a development task using TLDA workflow"""
        print(f"\n🎯 Processing Task: {task_description}")
        
        # Step 1: Analyze task with AI
        analysis = self.ai_bridge.send_prompt(
            f"Analyze this development task and suggest approach: {task_description}"
        )
        
        # Step 2: Warbler makes architectural decisions
        if "NPC" in task_description:
            decision, recommendation = self.warbler_decide(
                "Choose NPC architecture",
                ["Component-based ECS", "State machine", "Behavior trees"],
                task_description
            )
        else:
            decision, recommendation = self.warbler_decide(
                "Choose implementation approach", 
                ["Modular design", "Monolithic", "Plugin-based"],
                task_description
            )
        
        # Step 3: Generate TLDL summary
        self.generate_tldl_summary(task_description)
        
        return {
            "task": task_description,
            "analysis": analysis,
            "decision": decision,
            "recommendation": recommendation
        }
    
    def generate_tldl_summary(self, task):
        """Generate TLDL entry summary"""
        print(f"\n📋 Generating TLDL Entry for: {task}")
        
        summary = f"""
# TLDL-2025-09-10-{task.replace(' ', '')}

## Metadata
- Entry ID: TLDL-2025-09-10-{task.replace(' ', '')}
- Author: TLDA System
- Context: AI-assisted development with Warbler decisions
- Summary: {task} processed with Gemma3 AI assistance

## Activities
"""
        for i, entry in enumerate(self.session_context[-3:], 1):
            summary += f"{i}. {entry['activity']}: {entry['result']}\n"
        
        print(summary)
        return summary

def demo_tlda_warbler_gemma3():
    """Demonstrate the full TLDA-Warbler-Gemma3 integration"""
    print("🚀 TLDA-Warbler-Gemma3 Integration Demo")
    print("=" * 60)
    
    # Initialize the system
    tlda = TLDAWarblerSystem()
    
    # Process different types of development tasks
    tasks = [
        "Implement NPC controller for game AI",
        "Create documentation system for developers",
        "Design API for external integrations"
    ]
    
    results = []
    for task in tasks:
        result = tlda.process_development_task(task)
        results.append(result)
        print("\n" + "—" * 40)
    
    print("\n🎉 Demo Complete! TLDA + Warbler + Gemma3 working together!")
    print("\n💡 This shows how:")
    print("  - TLDA orchestrates the development workflow")
    print("  - Warbler makes cognitive decisions with AI input")  
    print("  - Gemma3 provides AI assistance locally")
    print("  - Everything is documented automatically in TLDL format")
    
    return results

if __name__ == "__main__":
    demo_tlda_warbler_gemma3()
