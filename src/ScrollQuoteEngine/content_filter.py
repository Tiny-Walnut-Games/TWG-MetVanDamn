#!/usr/bin/env python3
"""
Content Filter Sentinel - Ensures buttsafe compliance for all quotes

This module provides content filtering and validation to ensure all quotes
meet the standards of the Cheekdom Confidentiality Accord.
"""

import re
from typing import List, Dict, Set, Optional, Any
from dataclasses import dataclass


@dataclass 
class FilterResult:
    """Result of content filtering"""
    approved: bool
    score: float
    issues: List[str]
    recommendations: List[str]
    buttsafe_certified: bool


class ContentFilterSentinel:
    """Content filter ensuring quotes are SFW and compliant with Cheekdom standards"""
    
    def __init__(self):
        """Initialize the content filter with validation rules"""
        # Words and phrases that are always acceptable in cheekdom context
        self.approved_cheekdom_terms = {
            'butt', 'butts', 'buttwardens', 'buttsafe', 'cheek', 'cheeks', 
            'cheekdom', 'posterior', 'ergonomic', 'comfortable', 'cushion',
            'chair', 'sitting', 'comfort', 'save the butts'
        }
        
        # Professional development terms that are always safe
        self.professional_terms = {
            'code', 'debug', 'commit', 'merge', 'branch', 'repository', 'git',
            'workflow', 'documentation', 'testing', 'validation', 'linting',
            'development', 'programming', 'software', 'technical', 'algorithm',
            'architecture', 'design', 'implementation', 'maintenance', 'refactor'
        }
        
        # Patterns that indicate potentially problematic content
        self.warning_patterns = [
            r'\b(hate|violence|discrimination)\b',
            r'\b(offensive|inappropriate|explicit)\b',
            r'\b(politics|religion|controversial)\b'
        ]
        
        # Quality indicators for good quotes
        self.quality_patterns = [
            r'\b(wisdom|knowledge|learning|growth)\b',
            r'\b(collaboration|teamwork|community)\b', 
            r'\b(innovation|creativity|solution)\b',
            r'\b(quality|excellence|improvement)\b'
        ]
    
    def evaluate_quote(self, text: str, author: str = "", source: str = "") -> FilterResult:
        """
        Evaluate a quote for buttsafe compliance and professional suitability
        
        Args:
            text: The quote text to evaluate
            author: Quote author
            source: Quote source
            
        Returns:
            FilterResult with approval status and details
        """
        issues = []
        recommendations = []
        score = 0.0
        
        # Convert to lowercase for analysis
        text_lower = text.lower()
        full_text = f"{text} {author} {source}".lower()
        
        # Check length - too short or too long can be problematic
        if len(text.strip()) < 10:
            issues.append("Quote is too short to be meaningful")
            score -= 20
        elif len(text.strip()) > 300:
            recommendations.append("Consider shortening quote for better impact")
            score -= 5
        else:
            score += 10
        
        # Check for warning patterns
        for pattern in self.warning_patterns:
            if re.search(pattern, full_text, re.IGNORECASE):
                issues.append(f"Contains potentially problematic content: {pattern}")
                score -= 30
        
        # Bonus points for quality indicators
        quality_matches = 0
        for pattern in self.quality_patterns:
            if re.search(pattern, full_text, re.IGNORECASE):
                quality_matches += 1
        score += quality_matches * 5
        
        # Bonus for cheekdom-appropriate content
        cheekdom_terms = sum(1 for term in self.approved_cheekdom_terms if term in text_lower)
        professional_terms = sum(1 for term in self.professional_terms if term in text_lower)
        
        if cheekdom_terms > 0:
            score += 15
            recommendations.append("Great use of cheekdom-appropriate terminology")
        
        if professional_terms > 0:
            score += 10
            
        # Check for humor appropriateness (dry, witty, not offensive)
        humor_indicators = [
            r'\b(ironic|irony|wit|witty|clever|dry humor)\b',
            r'\b(amusing|chuckle|smile|grin)\b'
        ]
        
        for pattern in humor_indicators:
            if re.search(pattern, full_text, re.IGNORECASE):
                score += 8
                recommendations.append("Contains appropriate humor")
        
        # Check for development relevance
        dev_relevance = [
            r'\b(bug|error|exception|crash|fail)\b',
            r'\b(feature|requirement|specification)\b',
            r'\b(review|test|deploy|release)\b',
            r'\b(team|collaboration|communication)\b'
        ]
        
        relevance_matches = sum(1 for pattern in dev_relevance 
                              if re.search(pattern, full_text, re.IGNORECASE))
        score += relevance_matches * 3
        
        # Final scoring and approval
        max_score = 100.0
        normalized_score = min(max(score, 0), max_score) / max_score
        
        # Approval criteria
        approved = len(issues) == 0 and normalized_score >= 0.3
        buttsafe_certified = approved and normalized_score >= 0.6
        
        if not approved:
            recommendations.append("Review content for appropriateness and compliance")
        
        if normalized_score < 0.5:
            recommendations.append("Consider enhancing quote with more meaningful content")
        
        return FilterResult(
            approved=approved,
            score=normalized_score,
            issues=issues,
            recommendations=recommendations,
            buttsafe_certified=buttsafe_certified
        )
    
    def batch_evaluate(self, quotes: List[Dict[str, str]]) -> Dict[str, FilterResult]:
        """
        Evaluate multiple quotes at once
        
        Args:
            quotes: List of quote dictionaries with 'text', 'author', 'source' keys
            
        Returns:
            Dictionary mapping quote text to FilterResult
        """
        results = {}
        
        for quote in quotes:
            text = quote.get('text', '')
            author = quote.get('author', '')
            source = quote.get('source', '')
            
            result = self.evaluate_quote(text, author, source)
            results[text] = result
        
        return results
    
    def generate_compliance_report(self, quotes: List[Dict[str, str]]) -> Dict[str, Any]:
        """
        Generate a comprehensive compliance report for a set of quotes
        
        Args:
            quotes: List of quote dictionaries
            
        Returns:
            Detailed compliance report
        """
        results = self.batch_evaluate(quotes)
        
        total_quotes = len(quotes)
        approved_count = sum(1 for r in results.values() if r.approved)
        buttsafe_count = sum(1 for r in results.values() if r.buttsafe_certified)
        
        avg_score = sum(r.score for r in results.values()) / max(total_quotes, 1)
        
        all_issues = []
        all_recommendations = []
        
        for result in results.values():
            all_issues.extend(result.issues)
            all_recommendations.extend(result.recommendations)
        
        # Count unique issues and recommendations
        unique_issues = list(set(all_issues))
        unique_recommendations = list(set(all_recommendations))
        
        report = {
            'total_quotes': total_quotes,
            'approved_quotes': approved_count,
            'buttsafe_certified': buttsafe_count,
            'approval_rate': approved_count / max(total_quotes, 1),
            'buttsafe_rate': buttsafe_count / max(total_quotes, 1),
            'average_score': avg_score,
            'issues': unique_issues,
            'recommendations': unique_recommendations,
            'compliant': approved_count == total_quotes and buttsafe_count >= (total_quotes * 0.8)
        }
        
        return report
    
    def suggest_improvements(self, text: str) -> List[str]:
        """
        Suggest improvements for a quote to increase its score
        
        Args:
            text: Quote text to improve
            
        Returns:
            List of improvement suggestions
        """
        suggestions = []
        
        if len(text) < 20:
            suggestions.append("Expand the quote to provide more meaningful content")
        
        if len(text) > 250:
            suggestions.append("Consider condensing the quote for better impact")
        
        if not any(term in text.lower() for term in self.professional_terms):
            suggestions.append("Add development-related terminology for better relevance")
        
        if not any(term in text.lower() for term in self.approved_cheekdom_terms):
            suggestions.append("Consider incorporating cheekdom-appropriate terminology")
        
        # Check for passive voice and suggest active voice
        passive_patterns = [r'\bwas\s+\w+ed\b', r'\bwere\s+\w+ed\b', r'\bbeen\s+\w+ed\b']
        if any(re.search(pattern, text, re.IGNORECASE) for pattern in passive_patterns):
            suggestions.append("Consider using active voice for more impact")
        
        # Suggest adding wisdom or insight
        wisdom_indicators = ['learn', 'discover', 'realize', 'understand', 'insight']
        if not any(word in text.lower() for word in wisdom_indicators):
            suggestions.append("Consider adding an element of wisdom or insight")
        
        return suggestions


def main():
    """CLI interface for content filtering"""
    import argparse
    import json
    
    parser = argparse.ArgumentParser(description="Content Filter Sentinel - Buttsafe Compliance Checker")
    parser.add_argument('--text', help='Quote text to evaluate')
    parser.add_argument('--author', help='Quote author', default='')
    parser.add_argument('--source', help='Quote source', default='')
    parser.add_argument('--batch-file', help='JSON file with quotes to evaluate')
    parser.add_argument('--suggest', action='store_true', help='Provide improvement suggestions')
    parser.add_argument('--report', action='store_true', help='Generate compliance report')
    
    args = parser.parse_args()
    
    sentinel = ContentFilterSentinel()
    
    if args.text:
        # Evaluate single quote
        result = sentinel.evaluate_quote(args.text, args.author, args.source)
        
        print("🛡️ Content Filter Sentinel Report")
        print("=" * 35)
        print(f"Approved: {'✅ YES' if result.approved else '❌ NO'}")
        print(f"Buttsafe Certified: {'🍑 YES' if result.buttsafe_certified else '❌ NO'}")
        print(f"Score: {result.score:.2f}")
        
        if result.issues:
            print("\nIssues:")
            for issue in result.issues:
                print(f"  ⚠️  {issue}")
        
        if result.recommendations:
            print("\nRecommendations:")
            for rec in result.recommendations:
                print(f"  💡 {rec}")
        
        if args.suggest:
            suggestions = sentinel.suggest_improvements(args.text)
            if suggestions:
                print("\nImprovement Suggestions:")
                for suggestion in suggestions:
                    print(f"  🔧 {suggestion}")
    
    elif args.batch_file:
        # Evaluate batch of quotes
        try:
            with open(args.batch_file, 'r') as f:
                quotes = json.load(f)
            
            if args.report:
                report = sentinel.generate_compliance_report(quotes)
                print("📋 Compliance Report")
                print("=" * 20)
                print(f"Total Quotes: {report['total_quotes']}")
                print(f"Approved: {report['approved_quotes']} ({report['approval_rate']:.1%})")
                print(f"Buttsafe Certified: {report['buttsafe_certified']} ({report['buttsafe_rate']:.1%})")
                print(f"Average Score: {report['average_score']:.2f}")
                print(f"Overall Compliant: {'✅ YES' if report['compliant'] else '❌ NO'}")
            else:
                results = sentinel.batch_evaluate(quotes)
                for text, result in results.items():
                    status = "✅" if result.approved else "❌"
                    buttsafe = "🍑" if result.buttsafe_certified else "❌"
                    print(f"{status} {buttsafe} ({result.score:.2f}) {text[:50]}...")
        
        except Exception as e:
            print(f"Error processing batch file: {e}")
    
    else:
        print("Please provide --text for single evaluation or --batch-file for batch processing")


if __name__ == '__main__':
    main()