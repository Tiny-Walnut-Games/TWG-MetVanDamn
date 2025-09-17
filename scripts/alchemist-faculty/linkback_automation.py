#!/usr/bin/env python3
"""
Alchemist Faculty Linkback Automation


Automates the process of updating Gu Pot issues with new validated claims and evidence links.
Detects newly validated claims, updates Evidence Links sections, posts summary comments,
and applies appropriate promotion/completion labels.

Usage:
    python linkback_automation.py --claims-dir assets/experiments/school/claims/
    python linkback_automation.py --issue-number 123 --repo owner/repo
    python linkback_automation.py --batch --config linkback_config.yaml
"""

import argparse
import json
import yaml
import os
import sys
import re
import requests
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass, asdict

# Version information
ALCHEMIST_VERSION = "0.1.0"
SCRIPT_VERSION = "0.1.0"

@dataclass
class ClaimData:
    """Structure for claim validation data"""
    run_id: str
    experiment_name: str
    hypothesis_id: str
    confidence_score: float
    claim_type: str
    validation_time: str
    success: bool
    baseline_comparison: str
    file_path: str

@dataclass
class ValidationSummary:
    """Summary of validation results"""
    total_claims: int
    validated_claims: int
    hypotheses: int
    regressions: int
    average_confidence: float
    baseline_delta: float
    stage_decision: str
    promotion_rationale: str

class ClaimsDetector:
    """Detects and processes newly validated claims"""
    
    def __init__(self, claims_base_dir: str = "assets/experiments/school/claims/"):
        self.claims_base_dir = Path(claims_base_dir)
        self.integration_file = self.claims_base_dir / "github_integration.json"
    
    def scan_for_new_claims(self) -> List[ClaimData]:
        """Scan claims directories for new validation results"""
        claims = []
        
        # Scan validated claims
        validated_dir = self.claims_base_dir / "validated"
        if validated_dir.exists():
            claims.extend(self._load_claims_from_dir(validated_dir, "validated"))
        
        # Scan hypotheses
        hypotheses_dir = self.claims_base_dir / "hypotheses"
        if hypotheses_dir.exists():
            claims.extend(self._load_claims_from_dir(hypotheses_dir, "hypothesis"))
        
        # Scan regressions
        regressions_dir = self.claims_base_dir / "regressions"
        if regressions_dir.exists():
            claims.extend(self._load_claims_from_dir(regressions_dir, "regression"))
        
        return claims
    
    def _load_claims_from_dir(self, directory: Path, claim_type: str) -> List[ClaimData]:
        """Load claim data from JSON files in directory"""
        claims = []
        
        for json_file in directory.glob("*.json"):
            try:
                with open(json_file, 'r') as f:
                    data = json.load(f)
                
                claim = ClaimData(
                    run_id=data.get('RunId', ''),
                    experiment_name=data.get('ExperimentName', ''),
                    hypothesis_id=data.get('HypothesisId', ''),
                    confidence_score=data.get('ConfidenceScore', 0.0),
                    claim_type=claim_type,  # Use the directory-based type
                    validation_time=data.get('ValidationTime', ''),
                    success=data.get('Success', False),
                    baseline_comparison=data.get('BaselineComparison', ''),
                    file_path=str(json_file)
                )
                claims.append(claim)
                
            except (json.JSONDecodeError, KeyError) as e:
                print(f"Warning: Failed to parse claim file {json_file}: {e}")
        
        return claims
    
    def get_integration_metadata(self) -> Optional[Dict]:
        """Get GitHub integration metadata if available"""
        if not self.integration_file.exists():
            return None
        
        try:
            with open(self.integration_file, 'r') as f:
                return json.load(f)
        except (json.JSONDecodeError, IOError) as e:
            print(f"Warning: Failed to read integration metadata: {e}")
            return None
    
    def generate_validation_summary(self, claims: List[ClaimData]) -> ValidationSummary:
        """Generate summary of validation results"""
        if not claims:
            return ValidationSummary(0, 0, 0, 0, 0.0, 0.0, "compost", "No claims found")
        
        validated = [c for c in claims if c.claim_type == "validated"]
        hypotheses = [c for c in claims if c.claim_type == "hypothesis"]
        regressions = [c for c in claims if c.claim_type == "regression"]
        
        # Calculate average confidence
        total_confidence = sum(c.confidence_score for c in claims)
        avg_confidence = total_confidence / len(claims)
        
        # Determine stage decision based on validation results
        stage_decision = self._determine_stage_decision(validated, hypotheses, regressions, avg_confidence)
        promotion_rationale = self._generate_promotion_rationale(stage_decision, validated, hypotheses, regressions, avg_confidence)
        
        # Get baseline delta from integration metadata
        integration_data = self.get_integration_metadata()
        baseline_delta = integration_data.get('BaselineDelta', 0.0) if integration_data else 0.0
        
        return ValidationSummary(
            total_claims=len(claims),
            validated_claims=len(validated),
            hypotheses=len(hypotheses),
            regressions=len(regressions),
            average_confidence=avg_confidence,
            baseline_delta=baseline_delta,
            stage_decision=stage_decision,
            promotion_rationale=promotion_rationale
        )
    
    def _determine_stage_decision(self, validated: List[ClaimData], hypotheses: List[ClaimData], 
                                regressions: List[ClaimData], avg_confidence: float) -> str:
        """Determine stage decision based on validation criteria"""
        # Serum: High confidence validated claims
        if validated and avg_confidence >= 0.75:
            return "serum"
        
        # Antitoxin: Defensive value or stability improvement
        if hypotheses and avg_confidence >= 0.5 and not regressions:
            return "antitoxin"
        
        # Compost: Learning extracted but not promoted
        return "compost"
    
    def _generate_promotion_rationale(self, stage_decision: str, validated: List[ClaimData], 
                                    hypotheses: List[ClaimData], regressions: List[ClaimData], 
                                    avg_confidence: float) -> str:
        """Generate rationale for promotion decision"""
        if stage_decision == "serum":
            return f"High confidence validation achieved with {len(validated)} validated claims (avg confidence: {avg_confidence:.2f})"
        elif stage_decision == "antitoxin":
            return f"Defensive value confirmed with {len(hypotheses)} hypotheses validated (avg confidence: {avg_confidence:.2f})"
        else:
            return f"Learning extracted from {len(validated + hypotheses + regressions)} claims but promotion criteria not met"

class GitHubIntegration:
    """Handles GitHub API integration for issue updates"""
    
    def __init__(self, github_token: Optional[str] = None):
        self.github_token = github_token
        self.session = requests.Session()
        if github_token:
            self.session.headers.update({
                'Authorization': f'token {github_token}',
                'Accept': 'application/vnd.github.v3+json',
                'User-Agent': f'Alchemist-Faculty-Linkback/{SCRIPT_VERSION}'
            })
    
    def get_issue(self, owner: str, repo: str, issue_number: int, dry_run: bool = False) -> Optional[Dict]:
        """Fetch issue data from GitHub API"""
        if not self.github_token:
            if dry_run:
                print("Dry-run mode: Using mock issue data")
                return self._get_mock_issue(owner, repo, issue_number)
            else:
                print("Warning: No GitHub token provided. Cannot fetch issue data.")
                return None
        
        url = f"https://api.github.com/repos/{owner}/{repo}/issues/{issue_number}"
        try:
            response = self.session.get(url)
            response.raise_for_status()
            return response.json()
        except requests.RequestException as e:
            print(f"Error fetching issue #{issue_number}: {e}")
            return None
    
    def _get_mock_issue(self, owner: str, repo: str, issue_number: int) -> Dict:
        """Generate mock issue data for dry-run mode"""
        return {
            "number": issue_number,
            "title": f"Mock Gu Pot Issue #{issue_number}",
            "body": """## Logline
Mock combat system narrative integration issue

## Tension  
Players invest in story but battles feel like separate minigame

## Irreversible Shift
Integrate XP gain with narrative beats - combat outcomes affect story branches

## Measurable Residue
- Player engagement time in combat +15%
- Story completion rate correlation with combat participation  
- Tutorial skip rate decrease

## Current Evidence Links
(This section would be updated by linkback automation)""",
            "html_url": f"https://github.com/{owner}/{repo}/issues/{issue_number}",
            "state": "open",
            "labels": [{"name": "gu-pot:distilled"}, {"name": "narrative"}, {"name": "alchemist:ready"}],
            "created_at": "2025-09-06T02:55:00Z",
            "updated_at": "2025-09-06T02:55:00Z"
        }
    
    def update_issue(self, owner: str, repo: str, issue_number: int, updated_body: str) -> bool:
        """Update issue body with evidence links"""
        if not self.github_token:
            print("Warning: No GitHub token provided. Cannot update issue.")
            return False
        
        url = f"https://api.github.com/repos/{owner}/{repo}/issues/{issue_number}"
        data = {"body": updated_body}
        
        try:
            response = self.session.patch(url, json=data)
            response.raise_for_status()
            print(f"✓ Successfully updated issue #{issue_number}")
            return True
        except requests.RequestException as e:
            print(f"Error updating issue #{issue_number}: {e}")
            return False
    
    def add_labels(self, owner: str, repo: str, issue_number: int, labels: List[str]) -> bool:
        """Add labels to issue based on stage decision"""
        if not self.github_token:
            print("Warning: No GitHub token provided. Cannot add labels.")
            return False
        
        url = f"https://api.github.com/repos/{owner}/{repo}/issues/{issue_number}/labels"
        data = {"labels": labels}
        
        try:
            response = self.session.post(url, json=data)
            response.raise_for_status()
            print(f"✓ Applied labels to issue #{issue_number}: {', '.join(labels)}")
            return True
        except requests.RequestException as e:
            print(f"Error adding labels to issue #{issue_number}: {e}")
            return False
    
    def post_comment(self, owner: str, repo: str, issue_number: int, comment_body: str) -> bool:
        """Post summary comment to issue"""
        if not self.github_token:
            print("Warning: No GitHub token provided. Cannot post comment.")
            return False
        
        url = f"https://api.github.com/repos/{owner}/{repo}/issues/{issue_number}/comments"
        data = {"body": comment_body}
        
        try:
            response = self.session.post(url, json=data)
            response.raise_for_status()
            print(f"✓ Posted summary comment to issue #{issue_number}")
            return True
        except requests.RequestException as e:
            print(f"Error posting comment to issue #{issue_number}: {e}")
            return False

class EvidenceTemplateRenderer:
    """Renders evidence links templates with validation data"""
    
    def __init__(self, template_path: str = "templates/alchemist/issue-evidence-links-template.md"):
        self.template_path = Path(template_path)
    
    def load_template(self) -> str:
        """Load the evidence links template"""
        if not self.template_path.exists():
            # Fallback to embedded template
            return self._get_embedded_template()
        
        try:
            with open(self.template_path, 'r') as f:
                content = f.read()
            
            # Extract the evidence section template
            match = re.search(r'## Evidence Links Section Template\s*\n```markdown\n(.*?)\n```', content, re.DOTALL)
            if match:
                return match.group(1)
            else:
                return self._get_embedded_template()
        except IOError:
            return self._get_embedded_template()
    
    def _get_embedded_template(self) -> str:
        """Embedded fallback template"""
        return """## 🧪 Alchemist Evidence Links

> **Experimental Status**: {stage_decision} | **Confidence**: {confidence_score:.2f} | **Baseline Δ**: {baseline_delta}%
> **Validation Time**: `{validation_time}` | **Claims**: {total_claims}

### 📊 Validation Summary
{validation_summary}

### 🎯 Promotion Decision: **{stage_decision}**
{promotion_rationale}

### 📁 Evidence Artifacts

#### Generated Claims
{claim_files}

### 🔗 Trace Links
- **Claims Directory**: [`{claims_directory}`]({claims_directory})
- **Integration Metadata**: [`{integration_file}`]({integration_file})

---
*Updated by Alchemist Faculty v{alchemist_version} on {update_timestamp}*"""
    
    def render_evidence_section(self, summary: ValidationSummary, claims: List[ClaimData], 
                              integration_data: Optional[Dict] = None, 
                              issue_number: Optional[int] = None) -> str:
        """Render evidence section with validation data"""
        template = self.load_template()
        
        # Get experiment ID from integration data or generate placeholder
        experiment_id = "unknown"
        if integration_data:
            experiment_id = integration_data.get('ExperimentName', 'unknown')
        elif claims:
            experiment_id = claims[0].experiment_name
        
        # Prepare template variables
        template_vars = {
            'stage_decision': summary.stage_decision.upper(),
            'confidence_score': summary.average_confidence,
            'baseline_delta': summary.baseline_delta,
            'experiment_id': experiment_id,
            'run_timestamp': datetime.now(timezone.utc).isoformat(),
            'validation_time': datetime.now(timezone.utc).isoformat(),
            'total_claims': summary.total_claims,
            'validation_summary': self._format_validation_summary(summary),
            'promotion_rationale': summary.promotion_rationale,
            'claim_files': self._format_claim_files(claims),
            'artifact_inventory': self._format_artifact_inventory(claims),
            'issue_number': issue_number or 'unknown',
            'claims_directory': 'assets/experiments/school/claims/',
            'integration_file': 'assets/experiments/school/claims/github_integration.json',
            'logline_hash': 'sha256:pending',
            'tension_hash': 'sha256:pending',
            'extracted_on': datetime.now(timezone.utc).isoformat(),
            'alchemist_version': ALCHEMIST_VERSION,
            'update_timestamp': datetime.now(timezone.utc).isoformat()
        }
        
        return template.format(**template_vars)
    
    def _format_validation_summary(self, summary: ValidationSummary) -> str:
        """Format validation summary for template"""
        lines = [
            f"- **Total Claims Processed**: {summary.total_claims}",
            f"- **Validated Claims**: {summary.validated_claims}",
            f"- **Hypotheses**: {summary.hypotheses}",
            f"- **Regressions**: {summary.regressions}",
            f"- **Average Confidence**: {summary.average_confidence:.2f}",
            f"- **Baseline Delta**: {summary.baseline_delta}%"
        ]
        return '\n'.join(lines)
    
    def _format_claim_files(self, claims: List[ClaimData]) -> str:
        """Format claim files list for template"""
        if not claims:
            return "No claim files generated."
        
        lines = []
        by_type = {}
        for claim in claims:
            if claim.claim_type not in by_type:
                by_type[claim.claim_type] = []
            by_type[claim.claim_type].append(claim)
        
        for claim_type, type_claims in by_type.items():
            lines.append(f"\n**{claim_type.title()} ({len(type_claims)} files):**")
            for claim in type_claims:
                rel_path = claim.file_path.replace(str(Path.cwd()), '').lstrip('/')
                lines.append(f"- [{claim.run_id}]({rel_path}) (confidence: {claim.confidence_score:.2f})")
        
        return '\n'.join(lines)
    
    def _format_artifact_inventory(self, claims: List[ClaimData]) -> str:
        """Format artifact inventory for template"""
        if not claims:
            return "No artifacts generated."
        
        lines = [
            "**Validation Artifacts:**",
            "- Claims validation results",
            "- Confidence scoring metrics", 
            "- Baseline comparison data",
            "",
            "**File Inventory:**"
        ]
        
        for claim in claims:
            rel_path = claim.file_path.replace(str(Path.cwd()), '').lstrip('/')
            lines.append(f"- [{os.path.basename(claim.file_path)}]({rel_path})")
        
        return '\n'.join(lines)
    
    def render_summary_comment(self, summary: ValidationSummary, claims: List[ClaimData]) -> str:
        """Render summary comment for issue"""
        comment = f"""🧪 **Alchemist Faculty Update**

Experimental validation complete for this Gu Pot issue:
- **Stage Decision**: {summary.stage_decision.upper()}
- **Confidence Score**: {summary.average_confidence:.2f}
- **Total Claims**: {summary.total_claims} ({summary.validated_claims} validated, {summary.hypotheses} hypotheses, {summary.regressions} regressions)
- **Baseline Delta**: {summary.baseline_delta}%

**Promotion Rationale**: {summary.promotion_rationale}

See updated issue description for complete evidence links and validation details.

*Automated by Alchemist Faculty v{ALCHEMIST_VERSION}*"""
        
        return comment
    
    def get_stage_labels(self, stage_decision: str) -> List[str]:
        """Get labels to apply based on stage decision"""
        base_labels = ["alchemist:processed"]
        
        if stage_decision == "serum":
            return base_labels + ["alchemist:serum", "validation:passed", "ready-for-implementation"]
        elif stage_decision == "antitoxin":
            return base_labels + ["alchemist:antitoxin", "defensive-value", "stability-improvement"]
        else:  # compost
            return base_labels + ["alchemist:compost", "learning-extracted", "closed-with-value"]

class LinkbackAutomation:
    """Main automation orchestrator"""
    
    def __init__(self, github_token: Optional[str] = None, dry_run: bool = False):
        self.claims_detector = ClaimsDetector()
        self.github_integration = GitHubIntegration(github_token)
        self.template_renderer = EvidenceTemplateRenderer()
        self.dry_run = dry_run
    
    def process_issue_linkback(self, owner: str, repo: str, issue_number: int) -> bool:
        """Process linkback automation for a specific issue"""
        print(f"Processing linkback for issue #{issue_number} in {owner}/{repo}...")
        
        # Detect validated claims
        claims = self.claims_detector.scan_for_new_claims()
        if not claims:
            print("No validated claims found.")
            return False
        
        print(f"Found {len(claims)} claims to process")
        
        # Generate validation summary
        summary = self.claims_detector.generate_validation_summary(claims)
        integration_data = self.claims_detector.get_integration_metadata()
        
        # Get current issue
        issue_data = self.github_integration.get_issue(owner, repo, issue_number, self.dry_run)
        if not issue_data:
            print("Failed to fetch issue data")
            return False
        
        # Render evidence section
        evidence_section = self.template_renderer.render_evidence_section(
            summary, claims, integration_data, issue_number)
        
        # Update issue body
        updated_body = self._update_issue_body(issue_data['body'], evidence_section)
        
        if self.dry_run:
            print("DRY RUN - Would update issue with:")
            print(f"Evidence Section:\n{evidence_section}")
            print(f"Labels: {self.template_renderer.get_stage_labels(summary.stage_decision)}")
            return True
        
        # Update issue
        success = self.github_integration.update_issue(owner, repo, issue_number, updated_body)
        if not success:
            return False
        
        # Apply labels
        labels = self.template_renderer.get_stage_labels(summary.stage_decision)
        self.github_integration.add_labels(owner, repo, issue_number, labels)
        
        # Post summary comment
        comment = self.template_renderer.render_summary_comment(summary, claims)
        self.github_integration.post_comment(owner, repo, issue_number, comment)
        
        print(f"✓ Linkback automation completed for issue #{issue_number}")
        return True
    
    def _update_issue_body(self, current_body: str, evidence_section: str) -> str:
        """Update issue body with evidence section"""
        # Check if evidence section already exists
        if "## 🧪 Alchemist Evidence Links" in current_body:
            # Replace existing section
            pattern = r'## 🧪 Alchemist Evidence Links.*?(?=\n##|\n---|\Z)'
            updated_body = re.sub(pattern, evidence_section, current_body, flags=re.DOTALL)
        else:
            # Append new section
            updated_body = current_body.rstrip() + "\n\n" + evidence_section
        
        return updated_body

def main():
    parser = argparse.ArgumentParser(
        description="Alchemist Faculty Linkback Automation",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s --issue-number 123 --repo owner/repo
  %(prog)s --claims-dir assets/experiments/school/claims/ --issue-number 123 --repo owner/repo
  %(prog)s --dry-run --issue-number 123 --repo owner/repo
        """
    )
    
    # Required arguments
    parser.add_argument('--issue-number', type=int, required=True,
                       help='GitHub issue number to update')
    parser.add_argument('--repo', type=str, required=True,
                       help='Repository in format owner/repo')
    
    # Optional arguments
    parser.add_argument('--claims-dir', type=str, default='assets/experiments/school/claims/',
                       help='Claims directory to scan (default: assets/experiments/school/claims/)')
    parser.add_argument('--github-token', type=str,
                       help='GitHub personal access token')
    parser.add_argument('--dry-run', action='store_true',
                       help='Show what would be done without making changes')
    parser.add_argument('--verbose', '-v', action='store_true',
                       help='Verbose output')
    parser.add_argument('--version', action='version',
                       version=f'%(prog)s {SCRIPT_VERSION} (Alchemist Faculty {ALCHEMIST_VERSION})')
    
    args = parser.parse_args()
    
    # Parse repository
    try:
        owner, repo = args.repo.split('/', 1)
    except ValueError:
        print("Error: --repo must be in format 'owner/repo'", file=sys.stderr)
        sys.exit(1)
    
    # Get GitHub token from environment if not provided
    github_token = args.github_token or os.environ.get('GITHUB_TOKEN')
    
    if not github_token and not args.dry_run:
        print("Warning: No GitHub token provided. Use --github-token or set GITHUB_TOKEN environment variable.")
        print("Running in dry-run mode...")
        args.dry_run = True
    
    # Initialize automation
    automation = LinkbackAutomation(github_token, args.dry_run)
    automation.claims_detector.claims_base_dir = Path(args.claims_dir)
    
    try:
        success = automation.process_issue_linkback(owner, repo, args.issue_number)
        if success:
            print("Linkback automation completed successfully!")
            sys.exit(0)
        else:
            print("Linkback automation failed")
            sys.exit(1)
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        if args.verbose:
            import traceback
            traceback.print_exc()
        sys.exit(1)

if __name__ == '__main__':
    main()