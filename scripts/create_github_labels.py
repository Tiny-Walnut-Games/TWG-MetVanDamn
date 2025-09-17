#!/usr/bin/env python3
"""
Script to create missing GitHub labels for Dependabot configuration.

This script creates the labels that are referenced in .github/dependabot.yml
but don't exist in the repository yet.

Usage:
    export GITHUB_TOKEN=your_github_token
    python3 scripts/create_github_labels.py
"""

import requests
import os
import sys

# GitHub API configuration  
GITHUB_TOKEN = os.environ.get('GITHUB_TOKEN')
REPO_OWNER = 'jmeyer1980'
REPO_NAME = 'TWG-TLDA'

# Labels to create (based on dependabot.yml analysis)
MISSING_LABELS = [
    {
        'name': 'javascript',
        'description': 'JavaScript/Node.js related changes',
        'color': 'f1e05a'  # JavaScript yellow
    },
    {
        'name': 'github-actions', 
        'description': 'GitHub Actions workflow changes',
        'color': '2088ff'  # GitHub blue
    },
    {
        'name': 'python',
        'description': 'Python related changes', 
        'color': '3572A5'  # Python blue
    },
    {
        'name': 'ritual-auto',
        'description': 'Automated ritual/maintenance updates',
        'color': '7c3aed'  # Purple for automation
    }
]

def create_label(label_data):
    """Create a single label via GitHub API."""
    url = f"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/labels"
    
    headers = {
        'Authorization': f'token {GITHUB_TOKEN}',
        'Accept': 'application/vnd.github.v3+json',
        'Content-Type': 'application/json'
    }
    
    response = requests.post(url, json=label_data, headers=headers)
    
    if response.status_code == 201:
        print(f"✅ Created label: {label_data['name']}")
        return True
    elif response.status_code == 422:
        print(f"⚠️  Label already exists: {label_data['name']}")
        return True
    else:
        print(f"❌ Failed to create label {label_data['name']}: {response.status_code} - {response.text}")
        return False

def main():
    """Create all missing labels."""
    if not GITHUB_TOKEN:
        print("❌ GITHUB_TOKEN environment variable is required")
        print("   Set it with: export GITHUB_TOKEN=your_token_here")
        print("   Token needs 'repo' scope for this repository")
        sys.exit(1)
    
    print("🏷️  Creating missing GitHub labels for Dependabot...")
    print(f"   Repository: {REPO_OWNER}/{REPO_NAME}")
    print()
    
    success_count = 0
    for label in MISSING_LABELS:
        if create_label(label):
            success_count += 1
    
    print()
    print(f"✅ Successfully processed {success_count}/{len(MISSING_LABELS)} labels")
    
    if success_count == len(MISSING_LABELS):
        print("🎉 All labels created! Dependabot should now work correctly.")
        print("   Try creating a test PR to verify labels are applied properly.")
    else:
        print("⚠️  Some labels failed to create. Check the errors above.")
        print("   You may need to create them manually via GitHub UI.")

if __name__ == '__main__':
    main()