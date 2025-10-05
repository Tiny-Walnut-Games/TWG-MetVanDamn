#!/usr/bin/env python3
"""
Sacred AI Collaboration Detection System
Analyzes commits for AI assistance and assesses TLDL worthiness
"""
import json
import re
import sys
import os
from pathlib import Path

def detect_ai_collaboration_markers(commit_message, changed_files):
    """Detect AI collaboration markers in commit and files"""
    ai_markers = [
        '🧙‍♂️', 'AI:', 'Copilot:', 'Co-authored-by:',
        'Sacred Symbol', 'boss encounter', 'achievement',
        'dungeon crawl', 'quest complete', 'lore update'
    ]

    collaboration_score = 0
    detected_markers = []

    # Check commit message
    for marker in ai_markers:
        if marker.lower() in commit_message.lower():
            collaboration_score += 10
            detected_markers.append(marker)

    return collaboration_score, detected_markers

def assess_technical_significance(changed_files, diff_stats):
    """Assess technical significance of changes"""
    significance_indicators = {
        'new_feature': 25,      # New .cs files or major additions
        'architecture': 30,     # System-level changes
        'integration': 20,      # Multiple package changes
        'documentation': 15,    # Significant doc updates
        'testing': 10,          # Test additions
        'bugfix': 5             # Bug fixes
    }

    score = 0
    detected_types = []

    # Analyze file patterns
    cs_files = [f for f in changed_files if f.endswith('.cs')]
    md_files = [f for f in changed_files if f.endswith('.md')]
    test_files = [f for f in changed_files if 'test' in f.lower()]

    if len(cs_files) > 3:
        score += significance_indicators['integration']
        detected_types.append('integration')

    if any('system' in f.lower() for f in cs_files):
        score += significance_indicators['architecture']
        detected_types.append('architecture')

    if len(md_files) > 1:
        score += significance_indicators['documentation']
        detected_types.append('documentation')

    if test_files:
        score += significance_indicators['testing']
        detected_types.append('testing')

    return score, detected_types

def calculate_tldl_worthiness(ai_score, tech_score, file_count):
    """Calculate if changes warrant TLDL creation"""
    total_score = ai_score + tech_score

    # Bonus for substantial changes
    if file_count > 5:
        total_score += 10

    # TLDL worthy if score > 30 OR forced
    is_worthy = total_score > 30 or os.getenv('FORCE_TLDL') == 'true'

    return total_score, is_worthy

if __name__ == "__main__":
    # Get commit info from environment
    commit_message = os.getenv('COMMIT_MESSAGE', '')
    changed_files = os.getenv('CHANGED_FILES', '').split('\n')
    file_count = len([f for f in changed_files if f.strip()])

    # Detect collaboration and significance
    ai_score, ai_markers = detect_ai_collaboration_markers(commit_message, changed_files)
    tech_score, tech_types = assess_technical_significance(changed_files, {})
    total_score, is_worthy = calculate_tldl_worthiness(ai_score, tech_score, file_count)

    # Output results
    result = {
        'ai_collaboration_score': ai_score,
        'technical_significance_score': tech_score,
        'total_significance_score': total_score,
        'tldl_worthy': is_worthy,
        'detected_ai_markers': ai_markers,
        'detected_tech_types': tech_types,
        'file_count': file_count
    }

    print(json.dumps(result, indent=2))

    # Set GitHub outputs
    print("::set-output name=score::{}".format(total_score))
    print("::set-output name=worthy::{}".format(is_worthy))
    print("::set-output name=ai_markers::{}".format(','.join(ai_markers)))