#!/usr/bin/env python3
"""
Team XP synchronization for shared repositories
Merges and syncs XP data across team members
"""

import sys
import json
import subprocess
from pathlib import Path
from datetime import datetime

# Add parent directory to path for imports
sys.path.append(str(Path(__file__).parent.parent))
from dev_experience import DeveloperExperienceManager

def sync_team_xp():
    """Sync XP data before pushing to shared repository"""
    try:
        workspace_path = Path(__file__).parent.parent.parent.parent
        xp_manager = DeveloperExperienceManager(str(workspace_path))
        
        # Check if there are XP files to sync
        experience_dir = workspace_path / "experience"
        if not experience_dir.exists():
            return True
        
        profiles_file = experience_dir / "developer_profiles.json"
        if not profiles_file.exists():
            return True
        
        # Add XP files to git if they exist
        try:
            subprocess.run(['git', 'add', str(experience_dir)], check=True, capture_output=True)
            print("✅ XP data staged for commit")
        except subprocess.CalledProcessError:
            # Not a problem if git add fails
            pass
        
        # Create summary of local XP state
        total_developers = len(xp_manager.developer_profiles)
        total_contributions = sum(len(p.contributions) for p in xp_manager.developer_profiles.values())
        
        if total_developers > 0:
            print(f"🎮 Syncing XP data: {total_developers} developers, {total_contributions} contributions")
        
        return True
        
    except Exception as e:
        # Don't block pushes on XP sync failures
        return True

def main():
    """Main team sync function"""
    sync_team_xp()

if __name__ == "__main__":
    main()
