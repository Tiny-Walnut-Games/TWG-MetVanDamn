#!/usr/bin/env python3
"""
Git commit tracker for XP system
Analyzes commits and awards XP automatically
"""

import sys
import subprocess
import json
import re
from pathlib import Path

# Add parent directory to path for imports
sys.path.append(str(Path(__file__).parent.parent))
from dev_experience import DeveloperExperienceManager, ContributionType, QualityLevel

def get_commit_info():
    """Get information about the latest commit"""
    try:
        # Get commit hash
        commit_hash = subprocess.check_output(['git', 'rev-parse', 'HEAD'], stderr=subprocess.DEVNULL).decode().strip()
        
        # Get commit message
        commit_msg = subprocess.check_output(['git', 'log', '-1', '--pretty=%B'], stderr=subprocess.DEVNULL).decode().strip()
        
        # Get author
        author = subprocess.check_output(['git', 'log', '-1', '--pretty=%an'], stderr=subprocess.DEVNULL).decode().strip()
        
        # Get changed files
        changed_files = subprocess.check_output([
            'git', 'diff-tree', '--no-commit-id', '--name-only', '-r', commit_hash
        ], stderr=subprocess.DEVNULL).decode().strip().split('\n')
        
        # Get diff stats
        diff_stats = subprocess.check_output([
            'git', 'diff-tree', '--no-commit-id', '--numstat', '-r', commit_hash
        ], stderr=subprocess.DEVNULL).decode().strip()
        
        return {
            'hash': commit_hash,
            'message': commit_msg,
            'author': author,
            'files': [f for f in changed_files if f],
            'diff_stats': diff_stats
        }
        
    except Exception as e:
        print(f"Warning: Could not get commit info: {e}")
        return None

def analyze_commit_quality(commit_info):
    """Analyze commit quality and determine XP award"""
    if not commit_info:
        return ContributionType.CODE_CONTRIBUTION, QualityLevel.GOOD, {}
    
    message = commit_info['message'].lower()
    files = commit_info['files']
    
    # Analyze contribution type
    contrib_type = ContributionType.CODE_CONTRIBUTION
    
    if any(keyword in message for keyword in ['fix', 'bug', 'debug', 'fuck', 'race condition']):
        contrib_type = ContributionType.DEBUGGING_SESSION
    elif any(keyword in message for keyword in ['doc', 'readme', 'comment', 'tldl']):
        contrib_type = ContributionType.DOCUMENTATION
    elif any(keyword in message for keyword in ['test', 'spec', 'coverage']):
        contrib_type = ContributionType.TEST_COVERAGE
    elif any(keyword in message for keyword in ['refactor', 'clean', 'optimize']):
        contrib_type = ContributionType.REFACTORING
    elif any(keyword in message for keyword in ['architecture', 'design', 'system']):
        contrib_type = ContributionType.ARCHITECTURE
    elif any(keyword in message for keyword in ['review', 'feedback']):
        contrib_type = ContributionType.CODE_REVIEW
    elif any(keyword in message for keyword in ['innovation', 'template', 'integration']):
        contrib_type = ContributionType.INNOVATION
    
    # Analyze quality level
    quality = QualityLevel.GOOD  # Default
    
    # Check for quality indicators
    if len(message) > 100:  # Detailed commit message
        quality = QualityLevel.EXCELLENT
    
    if any(keyword in message for keyword in ['legendary', 'epic', 'amazing', 'breakthrough']):
        quality = QualityLevel.LEGENDARY
    elif any(keyword in message for keyword in ['major', 'significant', 'important', 'integration', 'cross-platform']):
        quality = QualityLevel.EPIC
    elif any(keyword in message for keyword in ['enhance', 'improve', 'comprehensive']):
        quality = QualityLevel.EXCELLENT
    elif any(keyword in message for keyword in ['minor', 'small', 'quick']):
        quality = QualityLevel.GOOD
    elif any(keyword in message for keyword in ['wip', 'temp', 'hack', 'broken']):
        quality = QualityLevel.NEEDS_WORK
    
    # File-based quality assessment
    if len(files) > 10:  # Large commit
        if quality == QualityLevel.GOOD:
            quality = QualityLevel.EXCELLENT
        elif quality == QualityLevel.EXCELLENT:
            quality = QualityLevel.EPIC
    
    # Calculate metrics
    metrics = {
        'files_changed': len(files),
        'commit_hash': commit_info['hash'][:8],
        'message_length': len(commit_info['message'])
    }
    
    # Parse diff stats for line counts
    if commit_info['diff_stats']:
        total_additions = 0
        total_deletions = 0
        for line in commit_info['diff_stats'].split('\n'):
            if '\t' in line:
                parts = line.split('\t')
                if len(parts) >= 2:
                    try:
                        additions = int(parts[0]) if parts[0] != '-' else 0
                        deletions = int(parts[1]) if parts[1] != '-' else 0
                        total_additions += additions
                        total_deletions += deletions
                    except ValueError:
                        pass
        
        metrics['lines_added'] = total_additions
        metrics['lines_deleted'] = total_deletions
        metrics['net_lines'] = total_additions - total_deletions
    
    return contrib_type, quality, metrics

def main():
    """Main commit tracking function"""
    try:
        # Get workspace path (go up from hooks directory)
        workspace_path = Path(__file__).parent.parent.parent.parent
        
        # Get commit information
        commit_info = get_commit_info()
        if not commit_info:
            # Not a Git repository or no commits
            return
        
        # Analyze commit
        contrib_type, quality, metrics = analyze_commit_quality(commit_info)
        
        # Create XP manager and record contribution
        xp_manager = DeveloperExperienceManager(str(workspace_path))
        
        contribution_id = xp_manager.record_contribution(
            developer_name=commit_info['author'],
            contribution_type=contrib_type,
            quality_level=quality,
            description=f"Git commit: {commit_info['message'][:100]}...",
            files_affected=commit_info['files'],
            metrics=metrics
        )
        
        if contribution_id:
            print(f"🎉 XP awarded for commit {commit_info['hash'][:8]} by {commit_info['author']}")
        
    except Exception as e:
        # Silently fail to avoid disrupting Git workflow
        pass

if __name__ == "__main__":
    main()
