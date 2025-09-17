#!/usr/bin/env python3
"""
Chronicle Keeper Auto-Refactoring Integration
Bridges Faculty Standards validation with XP-based automatic refactoring

This script integrates with the existing Chronicle Keeper workflow to provide
costly automatic document refactoring when Faculty standards violations are detected.
"""

import json
import sys
import argparse
from pathlib import Path
from typing import Dict, List, Any, Optional

# Add paths for imports
sys.path.append('src/SymbolicLinter')
sys.path.append('src/DeveloperExperience')

try:
    from faculty_standards_validator import FacultyStandardsValidator, RefactoringNeed
    from dev_experience import DeveloperExperienceManager
    DEPENDENCIES_AVAILABLE = True
except ImportError as e:
    print(f"❌ Missing dependencies: {e}")
    DEPENDENCIES_AVAILABLE = False

class ChronicleKeeperRefactoring:
    """Integrates automatic refactoring with Chronicle Keeper workflows"""
    
    def __init__(self, workspace_path: str = '.'):
        if not DEPENDENCIES_AVAILABLE:
            raise RuntimeError("Required dependencies not available")
            
        self.validator = FacultyStandardsValidator()
        self.xp_manager = DeveloperExperienceManager(workspace_path)
        self.refactoring_history = []
    
    def analyze_document_for_auto_refactoring(self, file_path: str, developer_name: str) -> Dict[str, Any]:
        """
        Analyze document and provide refactoring recommendations with cost analysis
        
        Args:
            file_path: Path to document to analyze
            developer_name: Developer requesting analysis
            
        Returns:
            Analysis results with refactoring options and affordability
        """
        try:
            content = Path(file_path).read_text(encoding='utf-8')
            needs = self.validator.validate_document(file_path, content)
            cost_analysis = self.validator.calculate_total_refactoring_cost(needs)
            
            # Check affordability
            affordability = self.xp_manager.get_refactoring_affordability(
                developer_name, cost_analysis['total_cost']
            )
            
            # Categorize refactoring options by cost tiers
            refactoring_options = self._categorize_refactoring_options(needs, affordability)
            
            return {
                "file_path": file_path,
                "developer": developer_name,
                "total_issues": cost_analysis['needs_count'],
                "total_cost": cost_analysis['total_cost'],
                "affordable": affordability['affordable'],
                "current_xp": affordability['current_xp'],
                "strategic_recommendation": cost_analysis['strategic_recommendation'],
                "refactoring_options": refactoring_options,
                "quick_wins": [self._need_to_dict(need) for need in cost_analysis.get('quick_wins', [])],
                "high_priority": [self._need_to_dict(need) for need in cost_analysis.get('high_priority_needs', [])]
            }
            
        except Exception as e:
            return {
                "error": f"Analysis failed: {e}",
                "file_path": file_path,
                "developer": developer_name
            }
    
    def _categorize_refactoring_options(self, needs: List[RefactoringNeed], affordability: Dict) -> Dict[str, Any]:
        """Categorize refactoring needs into affordable cost tiers"""
        current_xp = affordability['current_xp']
        
        affordable_fixes = []
        stretch_fixes = []
        unaffordable_fixes = []
        
        for need in sorted(needs, key=lambda x: x.final_cost):
            need_dict = self._need_to_dict(need)
            
            if need.final_cost <= current_xp * 0.25:  # <= 25% of XP
                affordable_fixes.append(need_dict)
            elif need.final_cost <= current_xp * 0.75:  # 25-75% of XP
                stretch_fixes.append(need_dict)
            else:
                unaffordable_fixes.append(need_dict)
        
        return {
            "affordable_fixes": affordable_fixes,
            "stretch_fixes": stretch_fixes, 
            "unaffordable_fixes": unaffordable_fixes,
            "recommendations": self._generate_tier_recommendations(
                len(affordable_fixes), len(stretch_fixes), len(unaffordable_fixes), current_xp
            )
        }
    
    def _need_to_dict(self, need: RefactoringNeed) -> Dict[str, Any]:
        """Convert RefactoringNeed to dictionary for JSON serialization"""
        return {
            "description": need.description,
            "cost": need.final_cost,
            "severity": need.severity,
            "type": need.issue_type.value,
            "complexity": need.complexity.value,
            "suggested_fix": need.suggested_fix,
            "copilot_prompt": need.copilot_prompt,
            "line_number": need.line_number
        }
    
    def _generate_tier_recommendations(self, affordable: int, stretch: int, unaffordable: int, xp: int) -> Dict[str, str]:
        """Generate strategic recommendations for each cost tier"""
        recommendations = {}
        
        if affordable > 0:
            recommendations["affordable"] = f"✅ {affordable} low-cost fixes available - excellent XP value"
        if stretch > 0:
            recommendations["stretch"] = f"⚠️ {stretch} moderate-cost fixes - consider strategic timing"
        if unaffordable > 0:
            recommendations["unaffordable"] = f"🚨 {unaffordable} high-cost fixes - need more XP or break into smaller pieces"
            
        if affordable + stretch == 0:
            recommendations["overall"] = "🔥 Emergency Faculty intervention needed - all fixes exceed reasonable XP cost"
        elif affordable > stretch + unaffordable:
            recommendations["overall"] = "🎯 Great refactoring opportunity - mostly affordable fixes available"
        else:
            recommendations["overall"] = "⚖️ Mixed refactoring scenario - prioritize affordable fixes first"
            
        return recommendations
    
    def execute_auto_refactoring(self, file_path: str, developer_name: str, refactoring_type: str = "selective", urgency: str = "normal") -> Dict[str, Any]:
        """
        Execute automatic refactoring with XP cost deduction
        
        Args:
            file_path: Document to refactor
            developer_name: Developer authorizing the refactoring
            refactoring_type: "affordable_only", "selective", "comprehensive" 
            urgency: "normal", "high", "emergency"
            
        Returns:
            Refactoring execution results
        """
        analysis = self.analyze_document_for_auto_refactoring(file_path, developer_name)
        
        if "error" in analysis:
            return analysis
            
        # Determine what to refactor based on type
        selected_fixes = self._select_fixes_by_type(analysis, refactoring_type)
        
        if not selected_fixes:
            return {
                "success": False,
                "message": "No fixes selected or affordable for the specified refactoring type",
                "analysis": analysis
            }
        
        total_cost = sum(fix["cost"] for fix in selected_fixes)
        
        # Execute XP spending
        spending_result = self.xp_manager.spend_xp_for_refactoring(
            developer_name, total_cost, f"Auto-refactoring {len(selected_fixes)} issues in {Path(file_path).name}", urgency
        )
        
        if not spending_result["success"]:
            return {
                "success": False,
                "message": "XP spending failed",
                "spending_error": spending_result["error"],
                "selected_fixes": selected_fixes,
                "total_cost": total_cost
            }
        
        # Record successful refactoring
        refactoring_record = {
            "timestamp": spending_result.get("refactoring_id", "unknown"),
            "file_path": file_path,
            "developer": developer_name,
            "fixes_applied": selected_fixes,
            "total_cost": total_cost,
            "urgency": urgency,
            "refactoring_type": refactoring_type,
            "xp_spent": spending_result["xp_spent"],
            "copilot_coins_earned": spending_result["copilot_coins_earned"]
        }
        
        self.refactoring_history.append(refactoring_record)
        
        return {
            "success": True,
            "message": f"Auto-refactoring completed! {len(selected_fixes)} issues fixed for {total_cost} XP",
            "refactoring_record": refactoring_record,
            "spending_result": spending_result,
            "remaining_issues": analysis["total_issues"] - len(selected_fixes)
        }
    
    def _select_fixes_by_type(self, analysis: Dict, refactoring_type: str) -> List[Dict]:
        """Select which fixes to apply based on refactoring type"""
        options = analysis["refactoring_options"]
        
        if refactoring_type == "affordable_only":
            return options["affordable_fixes"]
        elif refactoring_type == "selective":
            # Include affordable + some stretch fixes if total is reasonable
            selected = options["affordable_fixes"].copy()
            remaining_xp = analysis["current_xp"] - sum(fix["cost"] for fix in selected)
            
            for fix in options["stretch_fixes"]:
                if fix["cost"] <= remaining_xp * 0.5:  # Only if <= 50% of remaining XP
                    selected.append(fix)
                    remaining_xp -= fix["cost"]
                    
            return selected
        elif refactoring_type == "comprehensive":
            # All affordable fixes + all stretch fixes (if affordable)
            all_fixes = options["affordable_fixes"] + options["stretch_fixes"]
            total_cost = sum(fix["cost"] for fix in all_fixes)
            
            if total_cost <= analysis["current_xp"]:
                return all_fixes
            else:
                # Fall back to selective if comprehensive is unaffordable
                return self._select_fixes_by_type(analysis, "selective")
        else:
            return []

def main():
    """CLI interface for Chronicle Keeper Auto-Refactoring"""
    parser = argparse.ArgumentParser(description="Chronicle Keeper Auto-Refactoring System")
    parser.add_argument("--analyze", help="Analyze document for refactoring needs")
    parser.add_argument("--developer", required=True, help="Developer name for XP transactions")
    parser.add_argument("--refactor", help="Execute auto-refactoring on document")
    parser.add_argument("--type", choices=["affordable_only", "selective", "comprehensive"], 
                       default="selective", help="Refactoring scope")
    parser.add_argument("--urgency", choices=["normal", "high", "emergency"], 
                       default="normal", help="Refactoring urgency (affects cost)")
    parser.add_argument("--output", help="Output JSON results to file")
    
    args = parser.parse_args()
    
    if not DEPENDENCIES_AVAILABLE:
        print("❌ Cannot run - missing required dependencies")
        print("Ensure faculty_standards_validator.py and dev_experience.py are available")
        return 1
    
    try:
        refactoring_system = ChronicleKeeperRefactoring()
        
        if args.analyze:
            print(f"🔍 Analyzing {args.analyze} for Faculty standards compliance...")
            result = refactoring_system.analyze_document_for_auto_refactoring(args.analyze, args.developer)
            
            if "error" in result:
                print(f"❌ Analysis failed: {result['error']}")
                return 1
            
            print(f"📊 Analysis Results:")
            print(f"  Issues found: {result['total_issues']}")
            print(f"  Total cost: {result['total_cost']} XP")
            print(f"  Affordable: {'✅ Yes' if result['affordable'] else '❌ No'}")
            print(f"  Current XP: {result['current_xp']}")
            print(f"  Strategic recommendation: {result['strategic_recommendation']}")
            
            options = result['refactoring_options']
            print(f"\n💰 Refactoring Options:")
            print(f"  Affordable fixes: {len(options['affordable_fixes'])}")
            print(f"  Stretch fixes: {len(options['stretch_fixes'])}")
            print(f"  Unaffordable fixes: {len(options['unaffordable_fixes'])}")
            
            for tier, recommendation in options['recommendations'].items():
                print(f"  {tier}: {recommendation}")
            
            if args.output:
                Path(args.output).write_text(json.dumps(result, indent=2))
                print(f"📄 Results saved to {args.output}")
                
        elif args.refactor:
            print(f"🔧 Executing auto-refactoring on {args.refactor}...")
            print(f"  Developer: {args.developer}")
            print(f"  Type: {args.type}")
            print(f"  Urgency: {args.urgency}")
            
            result = refactoring_system.execute_auto_refactoring(
                args.refactor, args.developer, args.type, args.urgency
            )
            
            if result["success"]:
                print(f"✅ {result['message']}")
                record = result["refactoring_record"]
                print(f"📊 Refactoring Summary:")
                print(f"  Fixes applied: {len(record['fixes_applied'])}")
                print(f"  XP spent: {record['xp_spent']}")
                print(f"  CopilotCoins earned: {record['copilot_coins_earned']}")
                print(f"  Remaining issues: {result['remaining_issues']}")
                
                if args.output:
                    Path(args.output).write_text(json.dumps(result, indent=2))
                    print(f"📄 Results saved to {args.output}")
            else:
                print(f"❌ {result['message']}")
                if 'spending_error' in result:
                    print(f"💸 XP Error: {result['spending_error']}")
                return 1
        else:
            parser.print_help()
            
        return 0
        
    except Exception as e:
        print(f"❌ Auto-refactoring system error: {e}")
        return 1

if __name__ == "__main__":
    exit(main())