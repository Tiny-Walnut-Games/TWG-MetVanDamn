#!/usr/bin/env python3
"""
Faculty Standards Validator - Detect Document Refactoring Needs
Part of the Living Dev Agent Costly Automatic Refactoring System

Analyzes documents for Faculty standards compliance and determines
refactoring costs based on the severity and complexity of issues found.

Execution time: ~5ms per document
Integration with XP system for strategic refactoring decisions
"""

import re
import json
from pathlib import Path
from typing import Dict, List, Any, Optional, Tuple
from dataclasses import dataclass
from enum import Enum

class RefactoringType(Enum):
    """Types of refactoring operations with corresponding XP costs"""
    MINOR_FORMAT_FIX = "minor_format_fix"
    METADATA_COMPLETION = "metadata_completion"
    SECTION_RESTRUCTURE = "section_restructure" 
    CONTENT_ENHANCEMENT = "content_enhancement"
    COMPREHENSIVE_REWRITE = "comprehensive_rewrite"
    EMERGENCY_FACULTY_SUMMON = "emergency_faculty_summon"

class RefactoringComplexity(Enum):
    """Complexity levels affecting XP cost multiplier"""
    QUICK_FIX = "quick_fix"
    STANDARD_REFACTOR = "standard_refactor"
    DEEP_REFACTOR = "deep_refactor"
    COPILOT_ASSISTED = "copilot_assisted"

@dataclass
class RefactoringNeed:
    """Represents a detected refactoring need with cost analysis"""
    issue_type: RefactoringType
    complexity: RefactoringComplexity
    description: str
    severity: int  # 1-10 scale
    base_cost: int
    final_cost: int
    file_path: str
    line_number: Optional[int] = None
    suggested_fix: Optional[str] = None
    copilot_prompt: Optional[str] = None

class FacultyStandardsValidator:
    """Validates documents against Faculty standards and calculates refactoring costs"""
    
    def __init__(self):
        self.refactoring_costs = {
            RefactoringType.MINOR_FORMAT_FIX: 25,
            RefactoringType.METADATA_COMPLETION: 50,
            RefactoringType.SECTION_RESTRUCTURE: 75,
            RefactoringType.CONTENT_ENHANCEMENT: 100,
            RefactoringType.COMPREHENSIVE_REWRITE: 150,
            RefactoringType.EMERGENCY_FACULTY_SUMMON: 200
        }
        
        self.complexity_multipliers = {
            RefactoringComplexity.QUICK_FIX: 1.0,
            RefactoringComplexity.STANDARD_REFACTOR: 1.5,
            RefactoringComplexity.DEEP_REFACTOR: 2.0,
            RefactoringComplexity.COPILOT_ASSISTED: 2.5
        }
        
        # Required TLDL metadata fields
        self.required_metadata = [
            "Entry ID", "Author", "Context", "Summary"
        ]
        
        # Required TLDL sections  
        self.required_sections = [
            "Objective", "Discovery", "Actions Taken", "Key Insights", "Next Steps"
        ]
        
        # Common placeholder patterns that need enhancement
        self.placeholder_patterns = [
            r'\[.*?\]',  # [placeholder text]
            r'TODO:',
            r'FIXME:',
            r'TBD',
            r'To be determined',
            r'Fill this in',
            r'Add content here'
        ]

    def validate_document(self, file_path: str, content: str) -> List[RefactoringNeed]:
        """
        Validate a document and return list of refactoring needs
        
        Args:
            file_path: Path to the document
            content: Document content
            
        Returns:
            List of RefactoringNeed objects with cost analysis
        """
        needs = []
        
        # Check if this is a TLDL file
        if file_path.endswith('.md') and 'TLDL' in file_path:
            needs.extend(self._validate_tldl_document(file_path, content))
        else:
            needs.extend(self._validate_general_document(file_path, content))
            
        return needs

    def _validate_tldl_document(self, file_path: str, content: str) -> List[RefactoringNeed]:
        """Validate TLDL-specific requirements"""
        needs = []
        lines = content.split('\n')
        
        # Check metadata fields
        metadata_issues = self._check_metadata_fields(file_path, content)
        needs.extend(metadata_issues)
        
        # Check required sections
        section_issues = self._check_required_sections(file_path, content)
        needs.extend(section_issues)
        
        # Check for placeholder content
        placeholder_issues = self._check_placeholder_content(file_path, content)
        needs.extend(placeholder_issues)
        
        # Check formatting issues
        format_issues = self._check_formatting_issues(file_path, content)
        needs.extend(format_issues)
        
        return needs

    def _validate_general_document(self, file_path: str, content: str) -> List[RefactoringNeed]:
        """Validate general document requirements"""
        needs = []
        
        # Check for basic formatting issues
        format_issues = self._check_formatting_issues(file_path, content)
        needs.extend(format_issues)
        
        # Check for placeholder content
        placeholder_issues = self._check_placeholder_content(file_path, content)
        needs.extend(placeholder_issues)
        
        return needs

    def _check_metadata_fields(self, file_path: str, content: str) -> List[RefactoringNeed]:
        """Check for missing TLDL metadata fields"""
        needs = []
        
        for field in self.required_metadata:
            if f"**{field}:**" not in content and f"{field}:" not in content:
                need = RefactoringNeed(
                    issue_type=RefactoringType.METADATA_COMPLETION,
                    complexity=RefactoringComplexity.STANDARD_REFACTOR,
                    description=f"Missing required metadata field '{field}'",
                    severity=6,
                    base_cost=self.refactoring_costs[RefactoringType.METADATA_COMPLETION],
                    final_cost=self._calculate_final_cost(
                        RefactoringType.METADATA_COMPLETION, 
                        RefactoringComplexity.STANDARD_REFACTOR
                    ),
                    file_path=file_path,
                    suggested_fix=f"Add '**{field}:** [appropriate value]' to document header",
                    copilot_prompt=f"Add missing {field} metadata field to TLDL entry based on content context"
                )
                needs.append(need)
                
        return needs

    def _check_required_sections(self, file_path: str, content: str) -> List[RefactoringNeed]:
        """Check for missing TLDL sections"""
        needs = []
        
        for section in self.required_sections:
            section_patterns = [
                f"## {section}",
                f"# {section}",
                f"### {section}"
            ]
            
            if not any(pattern in content for pattern in section_patterns):
                complexity = RefactoringComplexity.DEEP_REFACTOR if section in ["Discovery", "Key Insights"] else RefactoringComplexity.STANDARD_REFACTOR
                
                need = RefactoringNeed(
                    issue_type=RefactoringType.SECTION_RESTRUCTURE,
                    complexity=complexity,
                    description=f"Missing required section '{section}'",
                    severity=7,
                    base_cost=self.refactoring_costs[RefactoringType.SECTION_RESTRUCTURE],
                    final_cost=self._calculate_final_cost(
                        RefactoringType.SECTION_RESTRUCTURE,
                        complexity
                    ),
                    file_path=file_path,
                    suggested_fix=f"Add '## {section}' section with appropriate content",
                    copilot_prompt=f"Generate {section} section content for TLDL entry based on existing content and context"
                )
                needs.append(need)
                
        return needs

    def _check_placeholder_content(self, file_path: str, content: str) -> List[RefactoringNeed]:
        """Check for placeholder text that needs replacement"""
        needs = []
        lines = content.split('\n')
        
        for line_num, line in enumerate(lines, 1):
            for pattern in self.placeholder_patterns:
                matches = re.findall(pattern, line, re.IGNORECASE)
                if matches:
                    # Determine complexity based on placeholder type
                    complexity = RefactoringComplexity.COPILOT_ASSISTED if any(
                        placeholder in line.lower() for placeholder in ['[what', '[how', '[why', '[important']
                    ) else RefactoringComplexity.STANDARD_REFACTOR
                    
                    need = RefactoringNeed(
                        issue_type=RefactoringType.CONTENT_ENHANCEMENT,
                        complexity=complexity,
                        description=f"Placeholder text found: {matches[0]}",
                        severity=5,
                        base_cost=self.refactoring_costs[RefactoringType.CONTENT_ENHANCEMENT],
                        final_cost=self._calculate_final_cost(
                            RefactoringType.CONTENT_ENHANCEMENT,
                            complexity
                        ),
                        file_path=file_path,
                        line_number=line_num,
                        suggested_fix=f"Replace placeholder with actual content",
                        copilot_prompt=f"Replace placeholder '{matches[0]}' with appropriate content based on document context"
                    )
                    needs.append(need)
                    
        return needs

    def _check_formatting_issues(self, file_path: str, content: str) -> List[RefactoringNeed]:
        """Check for basic formatting issues"""
        needs = []
        lines = content.split('\n')
        
        for line_num, line in enumerate(lines, 1):
            # Check for missing spaces after punctuation
            if re.search(r'[.!?][a-zA-Z]', line):
                need = RefactoringNeed(
                    issue_type=RefactoringType.MINOR_FORMAT_FIX,
                    complexity=RefactoringComplexity.QUICK_FIX,
                    description="Missing space after punctuation",
                    severity=2,
                    base_cost=self.refactoring_costs[RefactoringType.MINOR_FORMAT_FIX],
                    final_cost=self._calculate_final_cost(
                        RefactoringType.MINOR_FORMAT_FIX,
                        RefactoringComplexity.QUICK_FIX
                    ),
                    file_path=file_path,
                    line_number=line_num,
                    suggested_fix="Add space after punctuation",
                    copilot_prompt="Fix punctuation spacing in line"
                )
                needs.append(need)
                
        return needs

    def _calculate_final_cost(self, refactor_type: RefactoringType, complexity: RefactoringComplexity, urgency_modifier: float = 1.0) -> int:
        """Calculate final XP cost for refactoring operation"""
        base_cost = self.refactoring_costs[refactor_type]
        complexity_mult = self.complexity_multipliers[complexity]
        return int(base_cost * complexity_mult * urgency_modifier)

    def calculate_total_refactoring_cost(self, needs: List[RefactoringNeed]) -> Dict[str, Any]:
        """Calculate total cost and provide strategic analysis"""
        if not needs:
            return {
                "total_cost": 0,
                "needs_count": 0,
                "strategic_recommendation": "Document meets Faculty standards - no refactoring needed",
                "cost_breakdown": {}
            }
            
        total_cost = sum(need.final_cost for need in needs)
        cost_breakdown = {}
        
        for need in needs:
            key = f"{need.issue_type.value}_{need.complexity.value}"
            if key not in cost_breakdown:
                cost_breakdown[key] = {"count": 0, "cost": 0}
            cost_breakdown[key]["count"] += 1
            cost_breakdown[key]["cost"] += need.final_cost
            
        # Strategic recommendations based on cost
        if total_cost <= 50:
            recommendation = "Low-cost fixes recommended - invest XP for immediate clarity gains"
        elif total_cost <= 150:
            recommendation = "Moderate investment required - consider strategic timing for XP spending"
        elif total_cost <= 300:
            recommendation = "High-cost refactoring - evaluate critical importance before XP investment"
        else:
            recommendation = "Emergency Faculty standards violation - immediate Copilot assistance recommended"
            
        return {
            "total_cost": total_cost,
            "needs_count": len(needs),
            "strategic_recommendation": recommendation,
            "cost_breakdown": cost_breakdown,
            "high_priority_needs": [need for need in needs if need.severity >= 7],
            "quick_wins": [need for need in needs if need.complexity == RefactoringComplexity.QUICK_FIX]
        }

def main():
    """CLI interface for Faculty Standards validation"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Faculty Standards Validator - Document Refactoring Cost Analysis")
    parser.add_argument("--file", help="Single file to validate")
    parser.add_argument("--path", help="Directory path to validate")
    parser.add_argument("--cost-only", action="store_true", help="Show only cost analysis")
    parser.add_argument("--strategic", action="store_true", help="Show strategic recommendations")
    
    args = parser.parse_args()
    
    validator = FacultyStandardsValidator()
    
    if args.file:
        file_path = Path(args.file)
        if file_path.exists():
            content = file_path.read_text(encoding='utf-8')
            needs = validator.validate_document(str(file_path), content)
            
            if args.cost_only:
                cost_analysis = validator.calculate_total_refactoring_cost(needs)
                print(f"📊 Total Refactoring Cost: {cost_analysis['total_cost']} XP")
                print(f"🔍 Issues Found: {cost_analysis['needs_count']}")
                print(f"💡 Strategic Recommendation: {cost_analysis['strategic_recommendation']}")
            else:
                print(f"🔍 Faculty Standards Analysis: {file_path.name}")
                print(f"📋 Found {len(needs)} refactoring needs:")
                for need in needs:
                    print(f"  • {need.description} (Cost: {need.final_cost} XP)")
                
                if args.strategic:
                    cost_analysis = validator.calculate_total_refactoring_cost(needs)
                    print(f"\n💰 Strategic Analysis:")
                    print(f"  Total Investment: {cost_analysis['total_cost']} XP")
                    print(f"  Recommendation: {cost_analysis['strategic_recommendation']}")
        else:
            print(f"❌ File not found: {args.file}")
    
    elif args.path:
        path = Path(args.path)
        if path.exists():
            total_files = 0
            total_cost = 0
            
            for file_path in path.rglob("*.md"):
                content = file_path.read_text(encoding='utf-8')
                needs = validator.validate_document(str(file_path), content)
                
                if needs:
                    total_files += 1
                    file_cost = sum(need.final_cost for need in needs)
                    total_cost += file_cost
                    
                    print(f"📄 {file_path.name}: {len(needs)} issues, {file_cost} XP")
                    
            print(f"\n📊 Directory Analysis Summary:")
            print(f"Files needing refactoring: {total_files}")
            print(f"Total refactoring cost: {total_cost} XP")
            
            if total_cost > 0:
                if total_cost <= 200:
                    print("💡 Recommendation: Affordable batch refactoring - good XP investment")
                elif total_cost <= 500:
                    print("⚠️ Recommendation: Moderate cost - prioritize critical files first")
                else:
                    print("🚨 Recommendation: High cost - emergency Faculty intervention may be needed")
        else:
            print(f"❌ Path not found: {args.path}")
    else:
        print("📜 Faculty Standards Validator")
        print("Usage: --file <path> or --path <directory>")
        print("Options: --cost-only, --strategic")

if __name__ == "__main__":
    main()