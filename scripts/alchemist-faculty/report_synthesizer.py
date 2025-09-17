#!/usr/bin/env python3
"""
Alchemist Faculty Report Synthesis CLI

Synthesizes experiment reports from claims data and validation results.
Creates comprehensive markdown reports suitable for documentation and review.

Usage:
    python report_synthesizer.py --experiment-dir gu_pot/issue-123/
    python report_synthesizer.py --experiment-dir gu_pot/issue-123/ --output report.md
    python report_synthesizer.py --validate-only --experiment-dir gu_pot/issue-123/
"""

import argparse
import json
import os
import sys
import yaml
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List, Optional, Tuple, Any
from dataclasses import dataclass
from collections import defaultdict

# Version information
SCRIPT_VERSION = "0.1.0"
ALCHEMIST_VERSION = "0.1.0"

@dataclass
class ClaimSummary:
    """Summary data for a validated claim"""
    run_id: str
    experiment_name: str
    confidence_score: float
    claim_type: str
    validation_time: str
    success: bool
    baseline_comparison: str
    classification: Dict[str, Any]
    file_path: str

@dataclass
class ExperimentMetrics:
    """High-level experiment metrics"""
    total_claims: int
    validated_claims: int
    hypotheses_count: int
    regressions_count: int
    anomalies_count: int
    success_rate: float
    average_confidence: float
    validation_timespan: Tuple[str, str]

class ClaimsAnalyzer:
    """Analyzes claims data and generates summaries"""
    
    def __init__(self, experiment_dir: Path):
        self.experiment_dir = experiment_dir
        self.claims_dir = experiment_dir / "claims"
    
    def analyze_experiment(self) -> Tuple[List[ClaimSummary], ExperimentMetrics]:
        """Analyze all claims in the experiment directory"""
        claims = self._load_all_claims()
        metrics = self._calculate_metrics(claims)
        return claims, metrics
    
    def _load_all_claims(self) -> List[ClaimSummary]:
        """Load all claim files from the experiment directory"""
        claims = []
        
        if not self.claims_dir.exists():
            return claims
        
        # Process all claim categories
        for category_dir in self.claims_dir.iterdir():
            if not category_dir.is_dir():
                continue
            
            for claim_file in category_dir.glob("*.json"):
                try:
                    claim_data = self._parse_claim_file(claim_file, category_dir.name)
                    if claim_data:
                        claims.append(claim_data)
                except Exception as e:
                    print(f"Warning: Failed to parse {claim_file}: {e}")
        
        return claims
    
    def _parse_claim_file(self, file_path: Path, category: str) -> Optional[ClaimSummary]:
        """Parse individual claim file"""
        try:
            with open(file_path, 'r') as f:
                data = json.load(f)
            
            return ClaimSummary(
                run_id=data.get("RunId", "unknown"),
                experiment_name=data.get("ExperimentName", "unknown"),
                confidence_score=data.get("ConfidenceScore", 0.0),
                claim_type=data.get("ClaimType", category),
                validation_time=data.get("ValidationTime", ""),
                success=data.get("Success", False),
                baseline_comparison=data.get("BaselineComparison", "no data"),
                classification=data.get("Classification", {}),
                file_path=str(file_path)
            )
        except Exception:
            return None
    
    def _calculate_metrics(self, claims: List[ClaimSummary]) -> ExperimentMetrics:
        """Calculate high-level metrics from claims"""
        if not claims:
            return ExperimentMetrics(0, 0, 0, 0, 0, 0.0, 0.0, ("", ""))
        
        # Count by type
        validated_count = sum(1 for c in claims if c.claim_type == "validated")
        hypotheses_count = sum(1 for c in claims if c.claim_type == "hypotheses")
        regressions_count = sum(1 for c in claims if c.claim_type == "regressions")
        anomalies_count = sum(1 for c in claims if c.claim_type == "anomalies")
        
        # Calculate success rate and average confidence
        success_count = sum(1 for c in claims if c.success)
        success_rate = success_count / len(claims) if claims else 0.0
        
        avg_confidence = sum(c.confidence_score for c in claims) / len(claims) if claims else 0.0
        
        # Find validation timespan
        valid_times = [c.validation_time for c in claims if c.validation_time]
        if valid_times:
            min_time = min(valid_times)
            max_time = max(valid_times)
            timespan = (min_time, max_time)
        else:
            timespan = ("", "")
        
        return ExperimentMetrics(
            total_claims=len(claims),
            validated_claims=validated_count,
            hypotheses_count=hypotheses_count,
            regressions_count=regressions_count,
            anomalies_count=anomalies_count,
            success_rate=success_rate,
            average_confidence=avg_confidence,
            validation_timespan=timespan
        )

class ReportGenerator:
    """Generates markdown reports from experiment data"""
    
    def __init__(self):
        self.template = """# Alchemist Faculty Experiment Report

**Generated**: {timestamp}  
**Experiment Directory**: `{experiment_dir}`  
**Report Version**: {version}

## Executive Summary

{executive_summary}

## Experiment Metrics

{metrics_section}

## Claims Analysis

{claims_analysis}

## Validation Details

{validation_details}

## Classification Breakdown

{classification_breakdown}

## Recommendations

{recommendations}

---

*This report was generated by Alchemist Faculty Report Synthesis CLI v{version}*
*For questions about this analysis, consult the Alchemist Faculty documentation.*
"""
    
    def generate_report(self, experiment_dir: Path, claims: List[ClaimSummary], metrics: ExperimentMetrics) -> str:
        """Generate complete markdown report"""
        
        return self.template.format(
            timestamp=datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC"),
            experiment_dir=str(experiment_dir),
            version=SCRIPT_VERSION,
            executive_summary=self._generate_executive_summary(metrics),
            metrics_section=self._generate_metrics_section(metrics),
            claims_analysis=self._generate_claims_analysis(claims),
            validation_details=self._generate_validation_details(claims),
            classification_breakdown=self._generate_classification_breakdown(claims),
            recommendations=self._generate_recommendations(metrics, claims)
        )
    
    def _generate_executive_summary(self, metrics: ExperimentMetrics) -> str:
        """Generate executive summary section"""
        if metrics.total_claims == 0:
            return "No claims were found for this experiment. The validation pipeline may not have completed yet."
        
        summary = f"This experiment processed **{metrics.total_claims} claims** with an overall success rate of **{metrics.success_rate:.1%}**. "
        
        if metrics.validated_claims > 0:
            summary += f"**{metrics.validated_claims} claims were validated** and promoted to evidence status. "
        
        if metrics.regressions_count > 0:
            summary += f"⚠️ **{metrics.regressions_count} regressions were detected** requiring investigation. "
        
        summary += f"Average confidence score: **{metrics.average_confidence:.1%}**."
        
        return summary
    
    def _generate_metrics_section(self, metrics: ExperimentMetrics) -> str:
        """Generate metrics section"""
        return f"""
| Metric | Value |
|--------|-------|
| Total Claims | {metrics.total_claims} |
| Validated Claims | {metrics.validated_claims} |
| Active Hypotheses | {metrics.hypotheses_count} |
| Detected Regressions | {metrics.regressions_count} |
| Notable Anomalies | {metrics.anomalies_count} |
| Success Rate | {metrics.success_rate:.1%} |
| Average Confidence | {metrics.average_confidence:.1%} |
| Validation Timespan | {metrics.validation_timespan[0]} → {metrics.validation_timespan[1]} |
"""
    
    def _generate_claims_analysis(self, claims: List[ClaimSummary]) -> str:
        """Generate claims analysis section"""
        if not claims:
            return "No claims available for analysis."
        
        # Group claims by type
        by_type = defaultdict(list)
        for claim in claims:
            by_type[claim.claim_type].append(claim)
        
        analysis = ""
        for claim_type, type_claims in by_type.items():
            analysis += f"\n### {claim_type.title()} ({len(type_claims)})\n\n"
            
            if type_claims:
                # Sort by confidence score descending
                type_claims.sort(key=lambda c: c.confidence_score, reverse=True)
                
                for claim in type_claims[:5]:  # Show top 5
                    confidence_emoji = "🟢" if claim.confidence_score >= 0.75 else "🟡" if claim.confidence_score >= 0.5 else "🔴"
                    analysis += f"- {confidence_emoji} **{claim.experiment_name}** (Run {claim.run_id})\n"
                    analysis += f"  - Confidence: {claim.confidence_score:.1%}\n"
                    analysis += f"  - Baseline: {claim.baseline_comparison}\n"
                    analysis += f"  - Validated: {claim.validation_time}\n\n"
                
                if len(type_claims) > 5:
                    analysis += f"*... and {len(type_claims) - 5} more claims in this category*\n\n"
        
        return analysis
    
    def _generate_validation_details(self, claims: List[ClaimSummary]) -> str:
        """Generate validation details section"""
        if not claims:
            return "No validation data available."
        
        successful_claims = [c for c in claims if c.success]
        failed_claims = [c for c in claims if not c.success]
        
        details = f"**Successful Validations**: {len(successful_claims)}\n"
        details += f"**Failed Validations**: {len(failed_claims)}\n\n"
        
        if successful_claims:
            details += "#### Top Successful Validations\n\n"
            # Show top 3 by confidence
            top_successful = sorted(successful_claims, key=lambda c: c.confidence_score, reverse=True)[:3]
            for claim in top_successful:
                details += f"- **{claim.experiment_name}** ({claim.confidence_score:.1%} confidence)\n"
                details += f"  - {claim.baseline_comparison}\n\n"
        
        if failed_claims:
            details += "#### Notable Failures\n\n"
            for claim in failed_claims[:3]:  # Show first 3 failures
                details += f"- **{claim.experiment_name}** (Run {claim.run_id})\n"
                details += f"  - Issue: {claim.baseline_comparison}\n\n"
        
        return details
    
    def _generate_classification_breakdown(self, claims: List[ClaimSummary]) -> str:
        """Generate classification breakdown section"""
        if not claims:
            return "No classification data available."
        
        # Collect classification data
        primary_types = defaultdict(int)
        secondary_types = defaultdict(int)
        flags = defaultdict(int)
        
        for claim in claims:
            classification = claim.classification
            if isinstance(classification, dict):
                primary = classification.get("PrimaryType", "unknown")
                secondary = classification.get("SecondaryType", "unknown")
                claim_flags = classification.get("ClassificationFlags", [])
                
                primary_types[primary] += 1
                secondary_types[secondary] += 1
                
                for flag in claim_flags:
                    flags[flag] += 1
        
        breakdown = "#### Primary Classification Types\n\n"
        for ptype, count in sorted(primary_types.items(), key=lambda x: x[1], reverse=True):
            breakdown += f"- **{ptype}**: {count}\n"
        
        breakdown += "\n#### Secondary Classification Types\n\n"
        for stype, count in sorted(secondary_types.items(), key=lambda x: x[1], reverse=True):
            breakdown += f"- **{stype}**: {count}\n"
        
        if flags:
            breakdown += "\n#### Classification Flags\n\n"
            for flag, count in sorted(flags.items(), key=lambda x: x[1], reverse=True):
                breakdown += f"- **{flag}**: {count}\n"
        
        return breakdown
    
    def _generate_recommendations(self, metrics: ExperimentMetrics, claims: List[ClaimSummary]) -> str:
        """Generate recommendations section"""
        recommendations = []
        
        if metrics.total_claims == 0:
            recommendations.append("- **Run validation pipeline**: No claims detected. Execute the experiment validation pipeline to generate claims data.")
        
        if metrics.success_rate < 0.5:
            recommendations.append(f"- **Investigate low success rate**: {metrics.success_rate:.1%} success rate suggests experimental design issues.")
        
        if metrics.average_confidence < 0.6:
            recommendations.append(f"- **Review confidence scoring**: Average confidence of {metrics.average_confidence:.1%} may indicate measurement sensitivity issues.")
        
        if metrics.regressions_count > 0:
            recommendations.append(f"- **Address regressions**: {metrics.regressions_count} regressions detected. Review baseline measurements and experimental controls.")
        
        if metrics.validated_claims > 5:
            recommendations.append("- **Consider promotion**: Multiple validated claims suggest readiness for promotion to serum status.")
        
        if metrics.anomalies_count > 0:
            recommendations.append(f"- **Investigate anomalies**: {metrics.anomalies_count} anomalies detected. May indicate novel phenomena worth further study.")
        
        if not recommendations:
            recommendations.append("- **Experiment appears healthy**: No immediate action items identified. Continue monitoring validation pipeline.")
        
        return "\n".join(recommendations)

class ExperimentValidator:
    """Validates experiment directory structure and data integrity"""
    
    def __init__(self, experiment_dir: Path):
        self.experiment_dir = experiment_dir
    
    def validate_experiment(self) -> Tuple[bool, List[str]]:
        """Validate experiment structure and return (is_valid, error_list)"""
        errors = []
        
        # Check directory exists
        if not self.experiment_dir.exists():
            errors.append(f"Experiment directory does not exist: {self.experiment_dir}")
            return False, errors
        
        # Check for manifest file
        manifest_files = list(self.experiment_dir.glob("manifest*.json")) + list(self.experiment_dir.glob("manifest*.yaml"))
        if not manifest_files:
            errors.append("No manifest file found (expected manifest*.json or manifest*.yaml)")
        
        # Check for claims directory
        claims_dir = self.experiment_dir / "claims"
        if not claims_dir.exists():
            errors.append("Claims directory not found")
        else:
            # Check for claim files
            claim_files = list(claims_dir.rglob("*.json"))
            if not claim_files:
                errors.append("No claim files found in claims directory")
        
        # Check for runs directory (optional but expected)
        runs_dir = self.experiment_dir / "runs"
        if not runs_dir.exists():
            # This is a warning, not an error
            print(f"Warning: No runs directory found at {runs_dir}")
        
        return len(errors) == 0, errors

def main():
    parser = argparse.ArgumentParser(
        description="Alchemist Faculty Report Synthesis CLI",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Generate report for specific experiment
  python report_synthesizer.py --experiment-dir gu_pot/issue-123/

  # Generate report with custom output file
  python report_synthesizer.py --experiment-dir gu_pot/issue-123/ --output detailed_report.md

  # Validate experiment structure only
  python report_synthesizer.py --validate-only --experiment-dir gu_pot/issue-123/

  # Batch processing
  python report_synthesizer.py --batch --experiments-dir gu_pot/
        """
    )
    
    parser.add_argument('--experiment-dir', type=str, required=True,
                       help='Path to experiment directory containing claims and manifest')
    parser.add_argument('--output', type=str,
                       help='Output file path (default: experiment_dir/report/report_TIMESTAMP.md)')
    parser.add_argument('--validate-only', action='store_true',
                       help='Only validate experiment structure, do not generate report')
    parser.add_argument('--verbose', '-v', action='store_true',
                       help='Verbose output')
    parser.add_argument('--version', action='version', 
                       version=f'%(prog)s {SCRIPT_VERSION} (Alchemist Faculty {ALCHEMIST_VERSION})')
    
    args = parser.parse_args()
    
    experiment_dir = Path(args.experiment_dir)
    
    # Validate experiment structure
    validator = ExperimentValidator(experiment_dir)
    is_valid, errors = validator.validate_experiment()
    
    if not is_valid:
        print("❌ Experiment validation failed:")
        for error in errors:
            print(f"  - {error}")
        
        if args.validate_only:
            sys.exit(1)
        else:
            print("\nContinuing with report generation despite validation errors...")
    else:
        print("✅ Experiment structure validation passed")
        
        if args.validate_only:
            print(f"Experiment directory {experiment_dir} is valid for report synthesis.")
            sys.exit(0)
    
    # Analyze experiment
    print(f"📊 Analyzing experiment data in {experiment_dir}...")
    analyzer = ClaimsAnalyzer(experiment_dir)
    
    try:
        claims, metrics = analyzer.analyze_experiment()
        print(f"Found {metrics.total_claims} claims for analysis")
        
        if args.verbose:
            print(f"  - Validated: {metrics.validated_claims}")
            print(f"  - Hypotheses: {metrics.hypotheses_count}")
            print(f"  - Regressions: {metrics.regressions_count}")
            print(f"  - Anomalies: {metrics.anomalies_count}")
    
    except Exception as e:
        print(f"❌ Error analyzing experiment: {e}")
        if args.verbose:
            import traceback
            traceback.print_exc()
        sys.exit(1)
    
    # Generate report
    print("📝 Generating synthesis report...")
    generator = ReportGenerator()
    report_content = generator.generate_report(experiment_dir, claims, metrics)
    
    # Determine output path
    if args.output:
        output_path = Path(args.output)
    else:
        # Default output path
        report_dir = experiment_dir / "report"
        report_dir.mkdir(exist_ok=True)
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        output_path = report_dir / f"report_v{timestamp}.md"
    
    # Write report
    try:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_path, 'w') as f:
            f.write(report_content)
        
        print(f"✅ Report generated: {output_path}")
        
        if args.verbose:
            print(f"Report size: {len(report_content)} characters")
            print(f"Claims analyzed: {len(claims)}")
    
    except Exception as e:
        print(f"❌ Error writing report: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()