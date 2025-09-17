#!/usr/bin/env python3
"""
Alchemist Faculty Python Manifest Scaffold Generator

Generates experiment manifests from Gu Pot issues for the Alchemist Faculty
narrative → evidence distillation pipeline.

Usage:
    python generate_manifest.py --issue-number 123 --output gu_pot/issue-123/
    python generate_manifest.py --issue-url https://github.com/owner/repo/issues/123
    python generate_manifest.py --batch --issues-file issues_list.txt
"""

import argparse
import json
import yaml
import os
import sys
import hashlib
import requests
import re
from datetime import datetime, timezone
from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass, asdict
from pathlib import Path

# Version information
ALCHEMIST_VERSION = "0.1.0"
SCRIPT_VERSION = "0.1.0"

@dataclass
class OriginBinding:
    """Core origin binding schema for claim traceability"""
    type: str = "gu_pot"
    issue_number: int = 0
    issue_url: str = ""
    stage_at_evaluation: str = "distilled"
    logline_hash: str = ""
    tension_hash: str = ""
    irreversible_shift_declared: bool = False
    extracted_on: str = ""
    alchemist_version: str = ALCHEMIST_VERSION

@dataclass
class ExperimentManifest:
    """Complete experiment manifest structure"""
    metadata: Dict
    origin: OriginBinding
    experimental_context: Dict
    validation_criteria: Dict
    execution_config: Dict
    integration: Dict

class GilHubIssueParser:
    """Extracts structured data from GitHub issues"""
    
    def __init__(self, github_token: Optional[str] = None):
        self.github_token = github_token
        self.session = requests.Session()
        if github_token:
            self.session.headers.update({
                'Authorization': f'token {github_token}',
                'Accept': 'application/vnd.github.v3+json'
            })
    
    def parse_issue_url(self, issue_url: str) -> Tuple[str, str, int]:
        """Extract owner, repo, and issue number from GitHub URL"""
        match = re.match(r'https://github\.com/([^/]+)/([^/]+)/issues/(\d+)', issue_url)
        if not match:
            raise ValueError(f"Invalid GitHub issue URL: {issue_url}")
        return match.group(1), match.group(2), int(match.group(3))
    
    def fetch_issue_data(self, owner: str, repo: str, issue_number: int) -> Dict:
        """Fetch issue data from GitHub API"""
        if not self.github_token:
            print("Warning: No GitHub token provided. Using mock data for demonstration.")
            return self._generate_mock_issue_data(owner, repo, issue_number)
        
        url = f"https://api.github.com/repos/{owner}/{repo}/issues/{issue_number}"
        response = self.session.get(url)
        
        if response.status_code == 404:
            raise ValueError(f"Issue #{issue_number} not found in {owner}/{repo}")
        elif response.status_code != 200:
            raise ValueError(f"GitHub API error: {response.status_code} - {response.text}")
        
        return response.json()
    
    def _generate_mock_issue_data(self, owner: str, repo: str, issue_number: int) -> Dict:
        """Generate mock issue data for testing purposes"""
        return {
            "number": issue_number,
            "title": f"Combat system narrative integration - Issue #{issue_number}",
            "body": """## Logline
Combat system feels disconnected from narrative progression

## Tension  
Players invest in story but battles feel like separate minigame

## Irreversible Shift
Integrate XP gain with narrative beats - combat outcomes affect story branches

## Measurable Residue
- Player engagement time in combat +15%
- Story completion rate correlation with combat participation  
- Tutorial skip rate decrease""",
            "html_url": f"https://github.com/{owner}/{repo}/issues/{issue_number}",
            "state": "open",
            "labels": [{"name": "gu-pot:distilled"}, {"name": "narrative"}, {"name": "alchemist:ready"}],
            "created_at": "2025-09-06T02:55:00Z",
            "updated_at": "2025-09-06T02:55:00Z"
        }
    
    def extract_gu_pot_data(self, issue_data: Dict) -> Dict:
        """Extract Gu Pot structure from issue body"""
        body = issue_data.get('body', '')
        
        # Extract logline
        logline_match = re.search(r'## Logline\s*\n(.*?)(?:\n##|\n\n|\Z)', body, re.DOTALL)
        logline = logline_match.group(1).strip() if logline_match else ""
        
        # Extract tension
        tension_match = re.search(r'## Tension\s*\n(.*?)(?:\n##|\n\n|\Z)', body, re.DOTALL)
        tension = tension_match.group(1).strip() if tension_match else ""
        
        # Extract irreversible shift
        shift_match = re.search(r'## Irreversible Shift\s*\n(.*?)(?:\n##|\n\n|\Z)', body, re.DOTALL)
        irreversible_shift = shift_match.group(1).strip() if shift_match else ""
        
        # Extract measurable residue
        residue_match = re.search(r'## Measurable Residue\s*\n(.*?)(?:\n##|\n\n|\Z)', body, re.DOTALL)
        measurable_residue = residue_match.group(1).strip() if residue_match else ""
        
        # Determine stage from labels
        labels = [label['name'] for label in issue_data.get('labels', [])]
        stage = "larva"  # default
        for label in labels:
            if label.startswith('gu-pot:'):
                stage = label.split(':')[1]
                break
        
        return {
            'logline': logline,
            'tension': tension,
            'irreversible_shift': irreversible_shift,
            'measurable_residue': measurable_residue,
            'stage': stage,
            'title': issue_data.get('title', ''),
            'labels': labels,
            'issue_number': issue_data.get('number'),
            'issue_url': issue_data.get('html_url'),
            'created_at': issue_data.get('created_at'),
            'updated_at': issue_data.get('updated_at')
        }

class ManifestGenerator:
    """Generates experiment manifests from Gu Pot data"""
    
    def __init__(self):
        self.timestamp = datetime.now(timezone.utc).isoformat()
    
    def normalize_text_for_hash(self, text: str) -> str:
        """Normalize text for consistent hashing"""
        # Trim, lowercase, collapse internal whitespace to single spaces
        normalized = re.sub(r'\s+', ' ', text.strip().lower())
        return normalized
    
    def generate_hash(self, text: str) -> str:
        """Generate SHA-256 hash with prefix"""
        normalized = self.normalize_text_for_hash(text)
        hash_bytes = hashlib.sha256(normalized.encode('utf-8')).hexdigest()
        return f"sha256:{hash_bytes}"
    
    def extract_metrics_from_residue(self, measurable_residue: str) -> List[str]:
        """Extract metric names from measurable residue text"""
        metrics = []
        lines = measurable_residue.split('\n')
        for line in lines:
            line = line.strip()
            if line.startswith('-') or line.startswith('*'):
                # Extract metric name (first few words before numbers/percentages)
                metric_text = re.sub(r'[-*]\s*', '', line)
                metric_text = re.sub(r'\s*[+\-]?\d+\.?\d*%?.*', '', metric_text)
                if metric_text:
                    # Convert to snake_case metric name
                    metric_name = re.sub(r'[^\w\s]', '', metric_text)
                    metric_name = re.sub(r'\s+', '_', metric_name.strip().lower())
                    metrics.append(metric_name)
        
        return metrics if metrics else ["engagement_metric", "completion_metric", "satisfaction_metric"]
    
    def generate_experiment_id(self, issue_number: int) -> str:
        """Generate deterministic experiment ID"""
        import uuid
        # Use issue number and timestamp for deterministic but unique ID
        seed_string = f"issue-{issue_number}-{self.timestamp}"
        namespace = uuid.UUID('6ba7b810-9dad-11d1-80b4-00c04fd430c8')  # DNS namespace
        return str(uuid.uuid5(namespace, seed_string))
    
    def generate_seeds(self, issue_number: int) -> Dict[str, int]:
        """Generate deterministic seeds for reproducibility"""
        base_seed = issue_number * 42  # Deterministic base
        return {
            "global_seed": base_seed,
            "corpus_seed": base_seed + 123,
            "processing_seed": base_seed + 456,
        }
    
    def create_origin_binding(self, gu_pot_data: Dict) -> OriginBinding:
        """Create origin binding from Gu Pot data"""
        return OriginBinding(
            type="gu_pot",
            issue_number=gu_pot_data['issue_number'],
            issue_url=gu_pot_data['issue_url'],
            stage_at_evaluation=gu_pot_data['stage'],
            logline_hash=self.generate_hash(gu_pot_data['logline']),
            tension_hash=self.generate_hash(gu_pot_data['tension']),
            irreversible_shift_declared=bool(gu_pot_data['irreversible_shift']),
            extracted_on=self.timestamp,
            alchemist_version=ALCHEMIST_VERSION
        )
    
    def generate_manifest(self, gu_pot_data: Dict) -> ExperimentManifest:
        """Generate complete experiment manifest"""
        issue_number = gu_pot_data['issue_number']
        experiment_id = self.generate_experiment_id(issue_number)
        seeds = self.generate_seeds(issue_number)
        metrics = self.extract_metrics_from_residue(gu_pot_data['measurable_residue'])
        
        # Create origin binding
        origin = self.create_origin_binding(gu_pot_data)
        
        # Generate metadata
        metadata = {
            "name": f"Alchemist Experiment - Issue #{issue_number}",
            "description": gu_pot_data['title'],
            "version": "1.0.0",
            "author": "Alchemist Faculty",
            "created": self.timestamp,
            "tags": ["alchemist", "gu-pot", "narrative", "distillation"],
            "experiment_id": experiment_id,
            "source_issue": gu_pot_data['issue_url']
        }
        
        # Experimental context
        experimental_context = {
            "experiment_id": experiment_id,
            "manifest_version": "0.1.0",
            "baseline_metrics": metrics,
            "hypothesis": f"Implementation of '{gu_pot_data['irreversible_shift']}' will improve measured outcomes",
            "expected_outcomes": metrics,
            "seed_configuration": seeds,
            "reproducibility_requirements": {
                "deterministic_execution": True,
                "environment_isolation": True,
                "version_pinning": True
            }
        }
        
        # Validation criteria
        validation_criteria = {
            "min_confidence_threshold": 0.7,
            "min_baseline_improvement": 0.10,  # 10% minimum improvement
            "max_error_rate": 0.05,  # 5% maximum error rate
            "required_statistical_significance": 0.05,  # p < 0.05
            "promotion_gates": [
                "deterministic_reproducibility",
                "baseline_comparison",
                "confidence_scoring",
                "artifact_completeness"
            ]
        }
        
        # Execution configuration
        execution_config = {
            "mode": "adaptive",
            "batch_size": 50,
            "max_workers": 4,
            "timeout_seconds": 1800,  # 30 minutes
            "retry_policy": {
                "max_retries": 3,
                "backoff_multiplier": 2.0,
                "retry_on_timeout": True
            },
            "resource_limits": {
                "max_memory_mb": 2048,
                "max_cpu_cores": 8,
                "max_duration_minutes": 120
            },
            "checkpointing": {
                "enabled": True,
                "interval": 100,
                "resume_on_failure": True
            }
        }
        
        # Integration configuration
        integration = {
            "chronicle_integration": {
                "enabled": True,
                "auto_generate_tldl": True,
                "preserve_experiment_context": True
            },
            "pet_events": {
                "enabled": True,
                "track_milestones": True,
                "evolution_triggers": ["experiment_success", "baseline_improvement"]
            },
            "github_integration": {
                "enabled": True,
                "auto_update_issue": True,
                "apply_labels": True,
                "evidence_comment": True
            }
        }
        
        return ExperimentManifest(
            metadata=metadata,
            origin=origin,
            experimental_context=experimental_context,
            validation_criteria=validation_criteria,
            execution_config=execution_config,
            integration=integration
        )
    
    def save_manifest(self, manifest: ExperimentManifest, output_path: Path, format: str = "yaml") -> Path:
        """Save manifest to file"""
        output_path.mkdir(parents=True, exist_ok=True)
        
        # Convert to dictionary for serialization
        manifest_dict = {
            "metadata": manifest.metadata,
            "origin": asdict(manifest.origin),
            "experimental_context": manifest.experimental_context,
            "validation_criteria": manifest.validation_criteria,
            "execution_config": manifest.execution_config,
            "integration": manifest.integration
        }
        
        if format.lower() == "yaml":
            file_path = output_path / "manifest_v1.yaml"
            with open(file_path, 'w') as f:
                yaml.dump(manifest_dict, f, default_flow_style=False, sort_keys=False)
        else:
            file_path = output_path / "manifest_v1.json"
            with open(file_path, 'w') as f:
                json.dump(manifest_dict, f, indent=2)
        
        return file_path

def main():
    parser = argparse.ArgumentParser(
        description="Generate Alchemist Faculty experiment manifests from Gu Pot issues",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s --issue-number 123 --output gu_pot/issue-123/
  %(prog)s --issue-url https://github.com/owner/repo/issues/123 --format json
  %(prog)s --batch --issues-file issues_list.txt --output-dir gu_pot/
        """
    )
    
    # Input options
    input_group = parser.add_mutually_exclusive_group(required=True)
    input_group.add_argument('--issue-number', type=int, 
                           help='GitHub issue number')
    input_group.add_argument('--issue-url', type=str,
                           help='Full GitHub issue URL')
    input_group.add_argument('--batch', action='store_true',
                           help='Process multiple issues from file')
    
    # Output options
    parser.add_argument('--output', type=str, default='gu_pot/',
                       help='Output directory (default: gu_pot/)')
    parser.add_argument('--output-dir', type=str,
                       help='Base output directory for batch processing')
    parser.add_argument('--format', choices=['yaml', 'json'], default='yaml',
                       help='Output format (default: yaml)')
    
    # Batch processing
    parser.add_argument('--issues-file', type=str,
                       help='File containing issue numbers or URLs (one per line)')
    
    # GitHub options
    parser.add_argument('--github-token', type=str,
                       help='GitHub personal access token')
    parser.add_argument('--repo', type=str,
                       help='Repository in format owner/repo (required for --issue-number)')
    
    # Options
    parser.add_argument('--dry-run', action='store_true',
                       help='Generate manifest but do not save to file')
    parser.add_argument('--verbose', '-v', action='store_true',
                       help='Verbose output')
    parser.add_argument('--version', action='version', 
                       version=f'%(prog)s {SCRIPT_VERSION} (Alchemist Faculty {ALCHEMIST_VERSION})')
    
    args = parser.parse_args()
    
    # Validate arguments
    if args.issue_number and not args.repo:
        parser.error("--repo is required when using --issue-number")
    
    if args.batch and not args.issues_file:
        parser.error("--issues-file is required when using --batch")
    
    # Initialize components
    github_parser = GilHubIssueParser(args.github_token)
    manifest_generator = ManifestGenerator()
    
    try:
        if args.batch:
            # Batch processing
            process_batch(args, github_parser, manifest_generator)
        else:
            # Single issue processing
            process_single_issue(args, github_parser, manifest_generator)
    
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

def process_single_issue(args, github_parser, manifest_generator):
    """Process a single issue"""
    if args.issue_url:
        owner, repo, issue_number = github_parser.parse_issue_url(args.issue_url)
    else:
        owner, repo = args.repo.split('/')
        issue_number = args.issue_number
    
    if args.verbose:
        print(f"Processing issue #{issue_number} from {owner}/{repo}")
    
    # Fetch issue data
    issue_data = github_parser.fetch_issue_data(owner, repo, issue_number)
    gu_pot_data = github_parser.extract_gu_pot_data(issue_data)
    
    # Validate stage
    if gu_pot_data['stage'] not in ['distilled', 'fermenting']:
        print(f"Warning: Issue #{issue_number} is in '{gu_pot_data['stage']}' stage, not 'distilled'")
    
    # Generate manifest
    manifest = manifest_generator.generate_manifest(gu_pot_data)
    
    if args.dry_run:
        print("Generated manifest (dry run):")
        if args.format == 'yaml':
            import yaml
            manifest_dict = {
                "metadata": manifest.metadata,
                "origin": asdict(manifest.origin),
                "experimental_context": manifest.experimental_context,
                "validation_criteria": manifest.validation_criteria,
                "execution_config": manifest.execution_config,
                "integration": manifest.integration
            }
            print(yaml.dump(manifest_dict, default_flow_style=False))
        else:
            print(json.dumps(asdict(manifest), indent=2))
    else:
        # Save manifest
        output_path = Path(args.output)
        file_path = manifest_generator.save_manifest(manifest, output_path, args.format)
        print(f"Manifest saved to: {file_path}")
        
        if args.verbose:
            print(f"Experiment ID: {manifest.experimental_context['experiment_id']}")
            print(f"Origin binding: {manifest.origin.logline_hash}")

def process_batch(args, github_parser, manifest_generator):
    """Process multiple issues from file"""
    base_output = Path(args.output_dir or 'gu_pot')
    
    with open(args.issues_file, 'r') as f:
        issues = [line.strip() for line in f if line.strip()]
    
    print(f"Processing {len(issues)} issues...")
    
    for line in issues:
        try:
            if line.startswith('https://'):
                owner, repo, issue_number = github_parser.parse_issue_url(line)
            else:
                issue_number = int(line)
                if not args.repo:
                    print(f"Skipping issue #{issue_number}: --repo required for issue numbers")
                    continue
                owner, repo = args.repo.split('/')
            
            print(f"Processing issue #{issue_number}...")
            
            # Process issue
            issue_data = github_parser.fetch_issue_data(owner, repo, issue_number)
            gu_pot_data = github_parser.extract_gu_pot_data(issue_data)
            manifest = manifest_generator.generate_manifest(gu_pot_data)
            
            # Save to issue-specific directory
            output_path = base_output / f"issue-{issue_number}"
            file_path = manifest_generator.save_manifest(manifest, output_path, args.format)
            
            print(f"  ✓ Saved: {file_path}")
            
        except Exception as e:
            print(f"  ✗ Error processing {line}: {e}")

if __name__ == '__main__':
    main()