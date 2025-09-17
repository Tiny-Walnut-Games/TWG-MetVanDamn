#!/usr/bin/env python3
"""
Create missing labels required by Dependabot configuration.

This script creates the labels that Dependabot expects but are missing from the repository.
It uses the GitHub API via the requests library.
"""

import os
import sys
import requests
import json

def create_label(repo_owner, repo_name, token, name, description, color):
    """Create a label using GitHub API."""
    url = f"https://api.github.com/repos/{repo_owner}/{repo_name}/labels"
    headers = {
        'Authorization': f'token {token}',
        'Accept': 'application/vnd.github.v3+json'
    }
    data = {
        'name': name,
        'description': description,
        'color': color
    }
    
    response = requests.post(url, headers=headers, json=data)
    
    if response.status_code == 201:
        print(f"✅ Created label: {name}")
        return True
    elif response.status_code == 422 and 'already_exists' in response.text:
        print(f"⚠️  Label '{name}' already exists")
        return True
    else:
        print(f"❌ Failed to create label '{name}': {response.status_code} - {response.text}")
        return False

def main():
    print("🔒 Creating missing Dependabot labels...")
    
    # Get repository info
    repo_owner = os.environ.get('GITHUB_REPOSITORY_OWNER')
    repo_name = os.environ.get('GITHUB_REPOSITORY', '').split('/')[-1] if os.environ.get('GITHUB_REPOSITORY') else None
    token = os.environ.get('GITHUB_TOKEN')
    
    # If not in GitHub Actions, try to detect from git remote
    if not repo_owner or not repo_name:
        try:
            import subprocess
            result = subprocess.run(['git', 'remote', 'get-url', 'origin'], 
                                  capture_output=True, text=True, cwd=os.path.dirname(__file__))
            if result.returncode == 0:
                remote_url = result.stdout.strip()
                if 'github.com' in remote_url:
                    # Parse GitHub URL
                    if remote_url.startswith('https://github.com/'):
                        parts = remote_url.replace('https://github.com/', '').replace('.git', '').split('/')
                    elif remote_url.startswith('git@github.com:'):
                        parts = remote_url.replace('git@github.com:', '').replace('.git', '').split('/')
                    else:
                        parts = []
                    
                    if len(parts) >= 2:
                        repo_owner = parts[0]
                        repo_name = parts[1]
        except Exception as e:
            print(f"Failed to detect repository from git: {e}")
    
    if not token:
        print("❌ GITHUB_TOKEN environment variable is required.")
        print("   Set it to a personal access token with repo permissions.")
        print("   Or run this script in a GitHub Actions workflow.")
        sys.exit(1)
    
    if not repo_owner or not repo_name:
        print("❌ Could not determine repository owner and name.")
        print("   Set GITHUB_REPOSITORY_OWNER and GITHUB_REPOSITORY environment variables,")
        print("   or run this script from within a git repository.")
        sys.exit(1)
    
    print(f"Target repository: {repo_owner}/{repo_name}")
    
    # Define the labels to create
    labels = [
        {
            'name': 'dependencies',
            'description': 'Pull requests that update dependencies',
            'color': '0366d6'
        },
        {
            'name': 'github-actions',
            'description': 'Pull requests that update GitHub Actions',
            'color': '2188ff'
        },
        {
            'name': 'security',
            'description': 'Pull requests that address security issues',
            'color': 'd73a49'
        }
    ]
    
    # Create each label
    success_count = 0
    for label in labels:
        if create_label(repo_owner, repo_name, token, **label):
            success_count += 1
    
    print(f"\n✅ Successfully processed {success_count}/{len(labels)} labels.")
    print("\nThe following labels are now available for Dependabot:")
    print("  🔵 dependencies (blue) - Pull requests that update dependencies")
    print("  🔵 github-actions (blue) - Pull requests that update GitHub Actions")
    print("  🔴 security (red) - Pull requests that address security issues")
    print("\nDependabot should now be able to create pull requests with proper labels.")

if __name__ == '__main__':
    main()