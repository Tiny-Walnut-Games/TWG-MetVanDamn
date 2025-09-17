#!/usr/bin/env python3
"""
Alchemist Faculty Claims Classification System

Classifies experiment claims into categories (validated, regression, anomaly, etc.)
and provides detailed classification reasoning for validation pipeline integration.

Usage:
    python claims_classifier.py --claims-dir gu_pot/issue-123/claims/
    python claims_classifier.py --claim-file claim_001.json --baseline baseline.json
    python claims_classifier.py --batch --input-dir gu_pot/ --output-dir classified/
"""

import argparse
import json
import sys
from pathlib import Path
from typing import Dict, List, Tuple, Optional, Any
from dataclasses import dataclass, asdict
from datetime import datetime, timezone
import statistics
import math

# Version information
SCRIPT_VERSION = "0.1.0"
ALCHEMIST_VERSION = "0.1.0"

@dataclass
class ClassificationResult:
    """Result of claim classification"""
    claim_id: str
    original_category: str
    classified_category: str
    confidence_score: float
    reasoning: List[str]
    classification_metadata: Dict[str, Any]
    anomaly_score: float
    regression_likelihood: float
    improvement_likelihood: float
    timestamp: str

class ClaimsClassifier:
    """Main claims classification engine"""
    
    def __init__(self):
        # Classification thresholds
        self.thresholds = {
            "high_confidence": 0.75,
            "medium_confidence": 0.50,
            "regression_threshold": -0.05,  # 5% negative change
            "improvement_threshold": 0.05,   # 5% positive change
            "anomaly_threshold": 2.0,        # 2 standard deviations
            "min_samples": 3                 # Minimum samples for statistical analysis
        }
        
        # Classification rules
        self.classification_rules = self._initialize_classification_rules()
    
    def _initialize_classification_rules(self) -> Dict[str, Any]:
        """Initialize classification rule set"""
        return {
            "validated": {
                "min_confidence": 0.75,
                "max_anomaly_score": 1.5,
                "requires_baseline_comparison": True,
                "requires_positive_outcome": True
            },
            "regression": {
                "max_confidence": 0.80,  # Even high confidence can be regression
                "min_negative_change": -0.03,  # 3% or more decline
                "requires_baseline_comparison": True,
                "requires_negative_outcome": True
            },
            "anomaly": {
                "min_anomaly_score": 2.0,
                "anomaly_types": ["statistical", "behavioral", "performance"],
                "requires_investigation": True
            },
            "improvement": {
                "min_confidence": 0.60,
                "min_positive_change": 0.05,  # 5% or more improvement
                "requires_baseline_comparison": True
            },
            "hypothesis": {
                "min_confidence": 0.40,
                "max_confidence": 0.74,
                "requires_further_validation": True
            }
        }
    
    def classify_claim(self, claim_data: Dict[str, Any], baseline_data: Optional[Dict[str, Any]] = None) -> ClassificationResult:
        """Classify a single claim"""
        claim_id = claim_data.get("RunId", "unknown")
        original_category = claim_data.get("ClaimType", "unknown")
        confidence_score = claim_data.get("ConfidenceScore", 0.0)
        
        # Calculate anomaly score
        anomaly_score = self._calculate_anomaly_score(claim_data, baseline_data)
        
        # Calculate change likelihoods
        regression_likelihood, improvement_likelihood = self._calculate_change_likelihoods(claim_data, baseline_data)
        
        # Perform classification
        classified_category, reasoning = self._classify_based_on_rules(
            claim_data, confidence_score, anomaly_score, 
            regression_likelihood, improvement_likelihood, baseline_data
        )
        
        # Generate classification metadata
        metadata = self._generate_classification_metadata(
            claim_data, baseline_data, anomaly_score, 
            regression_likelihood, improvement_likelihood
        )
        
        return ClassificationResult(
            claim_id=claim_id,
            original_category=original_category,
            classified_category=classified_category,
            confidence_score=confidence_score,
            reasoning=reasoning,
            classification_metadata=metadata,
            anomaly_score=anomaly_score,
            regression_likelihood=regression_likelihood,
            improvement_likelihood=improvement_likelihood,
            timestamp=datetime.now(timezone.utc).isoformat()
        )
    
    def _calculate_anomaly_score(self, claim_data: Dict[str, Any], baseline_data: Optional[Dict[str, Any]]) -> float:
        """Calculate anomaly score based on statistical deviation"""
        if not baseline_data:
            return 0.0
        
        anomaly_indicators = []
        
        # Check confidence score anomaly
        confidence = claim_data.get("ConfidenceScore", 0.0)
        if confidence > 0.95 or confidence < 0.1:
            anomaly_indicators.append(1.0)  # Very high or low confidence is anomalous
        
        # Check baseline comparison for anomalous patterns
        baseline_comparison = claim_data.get("BaselineComparison", "")
        if "%" in baseline_comparison:
            try:
                # Extract percentage change
                percentage_str = baseline_comparison.split("%")[0].split()[-1]
                change_percent = float(percentage_str)
                
                # Extreme changes are anomalous
                if abs(change_percent) > 50:  # > 50% change
                    anomaly_indicators.append(2.0)
                elif abs(change_percent) > 25:  # > 25% change
                    anomaly_indicators.append(1.5)
            except (ValueError, IndexError):
                pass
        
        # Check for classification flags indicating anomalies
        classification = claim_data.get("Classification", {})
        flags = classification.get("ClassificationFlags", [])
        
        anomaly_flags = ["anomalous_pattern", "statistical_outlier", "unexpected_behavior"]
        for flag in flags:
            if any(anomaly_flag in flag.lower() for anomaly_flag in anomaly_flags):
                anomaly_indicators.append(1.0)
        
        # Check execution success vs confidence mismatch
        success = claim_data.get("Success", True)
        if not success and confidence > 0.7:
            anomaly_indicators.append(1.5)  # High confidence but failed execution
        elif success and confidence < 0.3:
            anomaly_indicators.append(1.0)  # Successful but low confidence
        
        return max(anomaly_indicators) if anomaly_indicators else 0.0
    
    def _calculate_change_likelihoods(self, claim_data: Dict[str, Any], baseline_data: Optional[Dict[str, Any]]) -> Tuple[float, float]:
        """Calculate regression and improvement likelihoods"""
        if not baseline_data:
            return 0.0, 0.0
        
        baseline_comparison = claim_data.get("BaselineComparison", "")
        
        # Parse baseline comparison for numerical changes
        regression_likelihood = 0.0
        improvement_likelihood = 0.0
        
        if "%" in baseline_comparison:
            try:
                # Extract percentage change
                percentage_str = baseline_comparison.split("%")[0].split()[-1]
                change_percent = float(percentage_str)
                
                if "regression" in baseline_comparison.lower() or "decline" in baseline_comparison.lower():
                    change_percent = -abs(change_percent)
                elif "improvement" in baseline_comparison.lower() or "increase" in baseline_comparison.lower():
                    change_percent = abs(change_percent)
                
                # Calculate likelihoods based on magnitude of change
                if change_percent < 0:
                    regression_likelihood = min(1.0, abs(change_percent) / 20.0)  # Max at 20% decline
                    improvement_likelihood = 0.0
                elif change_percent > 0:
                    improvement_likelihood = min(1.0, change_percent / 30.0)  # Max at 30% improvement
                    regression_likelihood = 0.0
                
            except (ValueError, IndexError):
                pass
        
        # Adjust based on success/failure
        success = claim_data.get("Success", True)
        if not success:
            regression_likelihood = max(regression_likelihood, 0.6)
            improvement_likelihood = max(0.0, improvement_likelihood - 0.3)
        
        # Adjust based on confidence score
        confidence = claim_data.get("ConfidenceScore", 0.0)
        if confidence < 0.4:
            regression_likelihood = max(regression_likelihood, 0.3)
        elif confidence > 0.8:
            improvement_likelihood = max(improvement_likelihood, 0.3)
        
        return regression_likelihood, improvement_likelihood
    
    def _classify_based_on_rules(self, claim_data: Dict[str, Any], confidence: float, 
                                anomaly_score: float, regression_likelihood: float, 
                                improvement_likelihood: float, baseline_data: Optional[Dict[str, Any]]) -> Tuple[str, List[str]]:
        """Apply classification rules to determine category"""
        reasoning = []
        
        # Check for anomaly first (highest priority)
        if anomaly_score >= self.thresholds["anomaly_threshold"]:
            reasoning.append(f"High anomaly score ({anomaly_score:.2f}) indicates anomalous behavior")
            reasoning.append("Requires investigation to understand unexpected patterns")
            return "anomaly", reasoning
        
        # Check for regression
        if regression_likelihood > 0.6:
            reasoning.append(f"High regression likelihood ({regression_likelihood:.2f}) indicates performance decline")
            if not claim_data.get("Success", True):
                reasoning.append("Execution failure supports regression classification")
            if baseline_data:
                reasoning.append("Baseline comparison shows negative performance impact")
            return "regression", reasoning
        
        # Check for validated claims
        if confidence >= self.thresholds["high_confidence"] and improvement_likelihood > 0.3:
            reasoning.append(f"High confidence ({confidence:.2f}) with positive outcome")
            if claim_data.get("Success", True):
                reasoning.append("Successful execution supports validation")
            if baseline_data:
                reasoning.append("Baseline comparison shows positive improvement")
            return "validated", reasoning
        
        # Check for improvements
        if improvement_likelihood > 0.5 and confidence >= 0.6:
            reasoning.append(f"Good improvement likelihood ({improvement_likelihood:.2f}) with reasonable confidence")
            reasoning.append("Shows measurable positive change from baseline")
            return "improvement", reasoning
        
        # Check for new phenomena (high anomaly but not negative)
        if anomaly_score > 1.0 and anomaly_score < self.thresholds["anomaly_threshold"] and regression_likelihood < 0.3:
            reasoning.append(f"Moderate anomaly score ({anomaly_score:.2f}) with positive indicators")
            reasoning.append("May represent novel behavioral pattern worth studying")
            return "new_phenomena", reasoning
        
        # Default to hypothesis for medium confidence
        if confidence >= self.thresholds["medium_confidence"]:
            reasoning.append(f"Medium confidence ({confidence:.2f}) requires further validation")
            reasoning.append("Insufficient evidence for promotion to validated status")
            return "hypothesis", reasoning
        
        # Low confidence - classify based on other indicators
        if regression_likelihood > 0.3:
            reasoning.append(f"Low confidence with regression indicators")
            return "regression", reasoning
        elif anomaly_score > 0.5:
            reasoning.append(f"Low confidence with anomalous patterns")
            return "anomaly", reasoning
        else:
            reasoning.append(f"Low confidence ({confidence:.2f}) with unclear outcome")
            reasoning.append("Requires re-evaluation or experimental redesign")
            return "hypothesis", reasoning
    
    def _generate_classification_metadata(self, claim_data: Dict[str, Any], baseline_data: Optional[Dict[str, Any]],
                                        anomaly_score: float, regression_likelihood: float, 
                                        improvement_likelihood: float) -> Dict[str, Any]:
        """Generate detailed classification metadata"""
        metadata = {
            "classification_version": SCRIPT_VERSION,
            "classification_timestamp": datetime.now(timezone.utc).isoformat(),
            "original_claim_type": claim_data.get("ClaimType", "unknown"),
            "confidence_band": self._get_confidence_band(claim_data.get("ConfidenceScore", 0.0)),
            "has_baseline": baseline_data is not None,
            "execution_success": claim_data.get("Success", True),
            "anomaly_score": anomaly_score,
            "regression_likelihood": regression_likelihood,
            "improvement_likelihood": improvement_likelihood,
            "statistical_features": self._extract_statistical_features(claim_data),
            "classification_flags": self._generate_classification_flags(claim_data, anomaly_score, 
                                                                      regression_likelihood, improvement_likelihood)
        }
        
        if baseline_data:
            metadata["baseline_comparison_analysis"] = self._analyze_baseline_comparison(claim_data, baseline_data)
        
        return metadata
    
    def _get_confidence_band(self, confidence: float) -> str:
        """Get confidence band classification"""
        if confidence >= 0.8:
            return "high"
        elif confidence >= 0.6:
            return "medium-high"
        elif confidence >= 0.4:
            return "medium"
        elif confidence >= 0.2:
            return "low"
        else:
            return "very-low"
    
    def _extract_statistical_features(self, claim_data: Dict[str, Any]) -> Dict[str, Any]:
        """Extract statistical features from claim data"""
        features = {}
        
        classification = claim_data.get("Classification", {})
        
        # Extract primary and secondary types
        features["primary_type"] = classification.get("PrimaryType", "unknown")
        features["secondary_type"] = classification.get("SecondaryType", "unknown")
        
        # Count classification flags
        flags = classification.get("ClassificationFlags", [])
        features["flag_count"] = len(flags)
        features["has_automation_flag"] = any("automation" in flag.lower() for flag in flags)
        features["has_confidence_flag"] = any("confidence" in flag.lower() for flag in flags)
        
        # Validation time analysis
        validation_time = claim_data.get("ValidationTime", "")
        if validation_time:
            features["has_validation_timestamp"] = True
            # Could add time-based analysis here
        else:
            features["has_validation_timestamp"] = False
        
        return features
    
    def _generate_classification_flags(self, claim_data: Dict[str, Any], anomaly_score: float,
                                     regression_likelihood: float, improvement_likelihood: float) -> List[str]:
        """Generate classification flags for this claim"""
        flags = []
        
        confidence = claim_data.get("ConfidenceScore", 0.0)
        
        # Confidence-based flags
        if confidence >= 0.9:
            flags.append("very_high_confidence")
        elif confidence <= 0.1:
            flags.append("very_low_confidence")
        
        # Anomaly flags
        if anomaly_score >= 2.5:
            flags.append("severe_anomaly")
        elif anomaly_score >= 1.5:
            flags.append("moderate_anomaly")
        
        # Change type flags
        if regression_likelihood > 0.7:
            flags.append("strong_regression_signal")
        elif improvement_likelihood > 0.7:
            flags.append("strong_improvement_signal")
        
        # Success/confidence mismatch flags
        success = claim_data.get("Success", True)
        if success and confidence < 0.3:
            flags.append("success_low_confidence_mismatch")
        elif not success and confidence > 0.7:
            flags.append("failure_high_confidence_mismatch")
        
        # Baseline comparison flags
        baseline_comparison = claim_data.get("BaselineComparison", "")
        if "%" in baseline_comparison:
            try:
                percentage_str = baseline_comparison.split("%")[0].split()[-1]
                change_percent = abs(float(percentage_str))
                if change_percent > 50:
                    flags.append("extreme_percentage_change")
                elif change_percent > 25:
                    flags.append("large_percentage_change")
            except (ValueError, IndexError):
                flags.append("unparseable_baseline_comparison")
        
        return flags
    
    def _analyze_baseline_comparison(self, claim_data: Dict[str, Any], baseline_data: Dict[str, Any]) -> Dict[str, Any]:
        """Analyze baseline comparison in detail"""
        analysis = {
            "comparison_text": claim_data.get("BaselineComparison", ""),
            "baseline_available": True,
            "comparison_type": "unknown"
        }
        
        comparison_text = claim_data.get("BaselineComparison", "").lower()
        
        # Determine comparison type
        if "improvement" in comparison_text or "increase" in comparison_text:
            analysis["comparison_type"] = "improvement"
        elif "regression" in comparison_text or "decline" in comparison_text or "decrease" in comparison_text:
            analysis["comparison_type"] = "regression"
        elif "no change" in comparison_text or "unchanged" in comparison_text:
            analysis["comparison_type"] = "no_change"
        elif "%" in comparison_text:
            analysis["comparison_type"] = "percentage_change"
        
        # Extract numerical values if possible
        if "%" in comparison_text:
            try:
                percentage_str = comparison_text.split("%")[0].split()[-1]
                change_value = float(percentage_str)
                analysis["extracted_percentage"] = change_value
                analysis["magnitude"] = "large" if abs(change_value) > 10 else "moderate" if abs(change_value) > 5 else "small"
            except (ValueError, IndexError):
                analysis["extraction_failed"] = True
        
        return analysis

class BatchClassifier:
    """Batch processing for claims classification"""
    
    def __init__(self):
        self.classifier = ClaimsClassifier()
        self.results = []
    
    def classify_directory(self, claims_dir: str, baseline_file: Optional[str] = None) -> List[ClassificationResult]:
        """Classify all claims in a directory"""
        claims_path = Path(claims_dir)
        baseline_data = None
        
        if baseline_file:
            try:
                with open(baseline_file, 'r') as f:
                    baseline_data = json.load(f)
            except Exception as e:
                print(f"Warning: Failed to load baseline file {baseline_file}: {e}")
        
        results = []
        
        # Process each claim category directory
        for category_dir in claims_path.iterdir():
            if not category_dir.is_dir():
                continue
            
            print(f"Processing category: {category_dir.name}")
            
            for claim_file in category_dir.glob("*.json"):
                try:
                    with open(claim_file, 'r') as f:
                        claim_data = json.load(f)
                    
                    result = self.classifier.classify_claim(claim_data, baseline_data)
                    result.classification_metadata["source_file"] = str(claim_file)
                    result.classification_metadata["source_category"] = category_dir.name
                    
                    results.append(result)
                    
                except Exception as e:
                    print(f"Error processing {claim_file}: {e}")
        
        self.results = results
        return results
    
    def save_classified_claims(self, output_dir: str, reorganize: bool = True):
        """Save classified claims to output directory"""
        output_path = Path(output_dir)
        output_path.mkdir(parents=True, exist_ok=True)
        
        if reorganize:
            # Create category directories
            categories = set(result.classified_category for result in self.results)
            for category in categories:
                (output_path / category).mkdir(exist_ok=True)
        
        # Save classification results
        for result in self.results:
            if reorganize:
                output_file = output_path / result.classified_category / f"{result.claim_id}_classified.json"
            else:
                output_file = output_path / f"{result.claim_id}_classified.json"
            
            with open(output_file, 'w') as f:
                json.dump(asdict(result), f, indent=2)
        
        # Save classification summary
        summary = self._generate_classification_summary()
        with open(output_path / "classification_summary.json", 'w') as f:
            json.dump(summary, f, indent=2)
    
    def _generate_classification_summary(self) -> Dict[str, Any]:
        """Generate summary of classification results"""
        category_counts = {}
        confidence_stats = []
        anomaly_scores = []
        
        for result in self.results:
            category = result.classified_category
            category_counts[category] = category_counts.get(category, 0) + 1
            confidence_stats.append(result.confidence_score)
            anomaly_scores.append(result.anomaly_score)
        
        return {
            "total_claims": len(self.results),
            "category_distribution": category_counts,
            "confidence_statistics": {
                "mean": statistics.mean(confidence_stats) if confidence_stats else 0,
                "median": statistics.median(confidence_stats) if confidence_stats else 0,
                "stdev": statistics.stdev(confidence_stats) if len(confidence_stats) > 1 else 0
            },
            "anomaly_statistics": {
                "mean": statistics.mean(anomaly_scores) if anomaly_scores else 0,
                "median": statistics.median(anomaly_scores) if anomaly_scores else 0,
                "max": max(anomaly_scores) if anomaly_scores else 0
            },
            "classification_timestamp": datetime.now(timezone.utc).isoformat(),
            "classifier_version": SCRIPT_VERSION
        }

def main():
    parser = argparse.ArgumentParser(
        description="Alchemist Faculty Claims Classification System",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Classify all claims in directory
  python claims_classifier.py --claims-dir gu_pot/issue-123/claims/

  # Single claim with baseline
  python claims_classifier.py --claim-file claim_001.json --baseline baseline.json

  # Batch processing
  python claims_classifier.py --batch --input-dir gu_pot/ --output-dir classified/

  # Reorganize by classified category
  python claims_classifier.py --claims-dir gu_pot/issue-123/claims/ --output-dir classified/ --reorganize
        """
    )
    
    # Input options
    input_group = parser.add_mutually_exclusive_group(required=True)
    input_group.add_argument('--claim-file', type=str,
                            help='Single claim file to classify')
    input_group.add_argument('--claims-dir', type=str,
                            help='Directory containing claim files')
    input_group.add_argument('--batch', action='store_true',
                            help='Batch process multiple experiment directories')
    
    # Batch processing options
    parser.add_argument('--input-dir', type=str,
                       help='Input directory for batch processing')
    parser.add_argument('--pattern', type=str, default="issue-*",
                       help='Pattern for finding experiment directories in batch mode')
    
    # Common options
    parser.add_argument('--baseline', type=str,
                       help='Baseline file for comparison analysis')
    parser.add_argument('--output-dir', type=str,
                       help='Output directory for classified results')
    parser.add_argument('--reorganize', action='store_true',
                       help='Reorganize output by classified category')
    
    # Output options
    parser.add_argument('--verbose', '-v', action='store_true',
                       help='Verbose output')
    parser.add_argument('--json-output', action='store_true',
                       help='Output results as JSON')
    parser.add_argument('--version', action='version', 
                       version=f'%(prog)s {SCRIPT_VERSION} (Alchemist Faculty {ALCHEMIST_VERSION})')
    
    args = parser.parse_args()
    
    # Initialize classifier
    if args.claim_file:
        # Single file classification
        classifier = ClaimsClassifier()
        
        try:
            with open(args.claim_file, 'r') as f:
                claim_data = json.load(f)
        except Exception as e:
            print(f"❌ Error loading claim file: {e}")
            sys.exit(1)
        
        baseline_data = None
        if args.baseline:
            try:
                with open(args.baseline, 'r') as f:
                    baseline_data = json.load(f)
            except Exception as e:
                print(f"Warning: Failed to load baseline file: {e}")
        
        result = classifier.classify_claim(claim_data, baseline_data)
        
        if args.json_output:
            print(json.dumps(asdict(result), indent=2))
        else:
            print(f"🏷️ Classification Result for {result.claim_id}")
            print(f"Original Category: {result.original_category}")
            print(f"Classified Category: {result.classified_category}")
            print(f"Confidence Score: {result.confidence_score:.2%}")
            print(f"Anomaly Score: {result.anomaly_score:.2f}")
            print(f"Regression Likelihood: {result.regression_likelihood:.2%}")
            print(f"Improvement Likelihood: {result.improvement_likelihood:.2%}")
            print("\nReasoning:")
            for reason in result.reasoning:
                print(f"  - {reason}")
    
    elif args.claims_dir:
        # Directory classification
        batch_classifier = BatchClassifier()
        results = batch_classifier.classify_directory(args.claims_dir, args.baseline)
        
        if args.output_dir:
            batch_classifier.save_classified_claims(args.output_dir, args.reorganize)
            print(f"✅ Classified {len(results)} claims saved to {args.output_dir}")
        
        if not args.json_output:
            # Print summary
            summary = batch_classifier._generate_classification_summary()
            print(f"\n📊 Classification Summary")
            print(f"Total Claims: {summary['total_claims']}")
            print("Category Distribution:")
            for category, count in summary['category_distribution'].items():
                print(f"  - {category}: {count}")
    
    elif args.batch:
        # Batch processing
        if not args.input_dir:
            print("❌ --input-dir required for batch processing")
            sys.exit(1)
        
        input_path = Path(args.input_dir)
        experiment_dirs = list(input_path.glob(args.pattern))
        
        if not experiment_dirs:
            print(f"❌ No experiment directories found with pattern {args.pattern}")
            sys.exit(1)
        
        print(f"📂 Found {len(experiment_dirs)} experiment directories")
        
        all_results = []
        for exp_dir in experiment_dirs:
            claims_dir = exp_dir / "claims"
            if claims_dir.exists():
                print(f"Processing {exp_dir.name}...")
                batch_classifier = BatchClassifier()
                results = batch_classifier.classify_directory(str(claims_dir))
                all_results.extend(results)
                
                if args.output_dir:
                    output_exp_dir = Path(args.output_dir) / exp_dir.name
                    batch_classifier.save_classified_claims(str(output_exp_dir), args.reorganize)
        
        print(f"✅ Processed {len(all_results)} total claims across {len(experiment_dirs)} experiments")

if __name__ == '__main__':
    main()