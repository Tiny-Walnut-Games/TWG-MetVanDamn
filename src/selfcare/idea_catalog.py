#!/usr/bin/env python3
"""
Idea Catalog - The Idea Vault of the Self-Care Engine

Manages high-velocity ideation capture with lightweight tagging and promotion workflows.
The sacred repository where creative sparks are preserved and nurtured.

🧙‍♂️ "Every idea, no matter how fleeting, deserves a place in the vault." - Bootstrap Sentinel
"""

import json
import datetime
import uuid
from pathlib import Path
from typing import List, Dict, Optional, Any
from dataclasses import dataclass, asdict
from enum import Enum


class IdeaTag(Enum):
    """Sacred tags for idea classification"""
    URGENT = "!"      # Immediate action required
    QUESTION = "?"    # Needs investigation/research  
    LATER = "⧗"       # Future consideration
    RECYCLE = "♻"     # Reusable component/pattern


@dataclass
class IdeaRecord:
    """Individual idea record in the vault"""
    id: str
    text: str
    timestamp: str
    tag: Optional[str] = None
    promoted: bool = False
    promoted_at: Optional[str] = None
    metadata: Dict[str, Any] = None
    
    def __post_init__(self):
        if self.metadata is None:
            self.metadata = {}


class IdeaCatalog:
    """
    The legendary Idea Vault - captures and manages high-velocity ideation
    
    Provides safe storage for creative overflow with classification,
    promotion workflows, and daily rollover management.
    """
    
    def __init__(self, catalog_path: str = "data/idea_catalog.json"):
        self.catalog_path = Path(catalog_path)
        self.catalog_path.parent.mkdir(parents=True, exist_ok=True)
        self.catalog = self._load_catalog()
    
    def _load_catalog(self) -> Dict[str, Any]:
        """Load catalog from disk or create new one"""
        if self.catalog_path.exists():
            try:
                with open(self.catalog_path, 'r', encoding='utf-8') as f:
                    return json.load(f)
            except (json.JSONDecodeError, FileNotFoundError):
                pass
        
        # Create new catalog with schema
        return {
            "schema_version": "1.0.0",
            "created_at": datetime.datetime.now().isoformat(),
            "last_updated": datetime.datetime.now().isoformat(),
            "last_rollover": None,
            "ideas": [],
            "promoted_count": 0,
            "daily_stats": {}
        }
    
    def _save_catalog(self):
        """Persist catalog to disk"""
        self.catalog["last_updated"] = datetime.datetime.now().isoformat()
        with open(self.catalog_path, 'w', encoding='utf-8') as f:
            json.dump(self.catalog, f, indent=2, ensure_ascii=False)
    
    def add_raw(self, line: str) -> str:
        """
        Add raw idea line to catalog
        
        Args:
            line: Raw idea text
            
        Returns:
            Unique ID of the created idea record
        """
        idea_id = str(uuid.uuid4())[:8]  # Short ID for convenience
        timestamp = datetime.datetime.now().isoformat()
        
        idea = IdeaRecord(
            id=idea_id,
            text=line.strip(),
            timestamp=timestamp
        )
        
        self.catalog["ideas"].append(asdict(idea))
        self._update_daily_stats("added")
        self._save_catalog()
        
        return idea_id
    
    def classify(self, idea_id: str, tag: str) -> bool:
        """
        Classify idea with tag
        
        Args:
            idea_id: ID of idea to classify
            tag: Classification tag (!, ?, ⧗, ♻)
            
        Returns:
            True if successful, False if idea not found
        """
        # Validate tag
        valid_tags = [tag.value for tag in IdeaTag]
        if tag not in valid_tags:
            raise ValueError(f"Invalid tag '{tag}'. Must be one of: {valid_tags}")
        
        # Find and update idea
        for idea in self.catalog["ideas"]:
            if idea["id"] == idea_id:
                idea["tag"] = tag
                self._save_catalog()
                return True
        
        return False
    
    def promote(self, idea_id: str) -> Optional[Dict[str, Any]]:
        """
        Promote idea to structured record for potential melt
        
        Args:
            idea_id: ID of idea to promote
            
        Returns:
            Structured record dict if successful, None if not found
        """
        for idea in self.catalog["ideas"]:
            if idea["id"] == idea_id:
                if idea["promoted"]:
                    return None  # Already promoted
                
                # Mark as promoted
                idea["promoted"] = True
                idea["promoted_at"] = datetime.datetime.now().isoformat()
                
                # Create structured record
                structured_record = {
                    "id": idea["id"],
                    "original_text": idea["text"],
                    "timestamp": idea["timestamp"],
                    "tag": idea["tag"],
                    "promoted_at": idea["promoted_at"],
                    "charter_template": {
                        "problem": f"Investigate: {idea['text']}",
                        "why_now": "Captured during high-velocity ideation",
                        "risk": "Idea may be lost in cognitive overflow",
                        "pitch": idea["text"],
                        "synergy": "TBD - requires analysis",
                        "integrity_boost": "Improved idea management",
                        "prototype": "Conceptual framework",
                        "kill_criteria": "No clear implementation path within 30 days"
                    }
                }
                
                self.catalog["promoted_count"] += 1
                self._update_daily_stats("promoted")
                self._save_catalog()
                
                return structured_record
        
        return None
    
    def list(self, filter_tag: Optional[str] = None, limit: int = 50) -> List[Dict[str, Any]]:
        """
        List ideas with optional filtering
        
        Args:
            filter_tag: Optional tag to filter by
            limit: Maximum number of ideas to return
            
        Returns:
            List of idea records
        """
        ideas = self.catalog["ideas"]
        
        # Filter by tag if specified
        if filter_tag:
            ideas = [idea for idea in ideas if idea.get("tag") == filter_tag]
        
        # Sort by timestamp (newest first) and limit
        ideas = sorted(ideas, key=lambda x: x["timestamp"], reverse=True)
        return ideas[:limit]
    
    def daily_rollover(self) -> Dict[str, Any]:
        """
        Perform daily rollover, consolidating previous day into catalog/history
        
        Returns:
            Rollover summary with statistics
        """
        today = datetime.date.today().isoformat()
        yesterday = (datetime.date.today() - datetime.timedelta(days=1)).isoformat()
        
        # Count ideas from yesterday
        yesterday_ideas = [
            idea for idea in self.catalog["ideas"]
            if idea["timestamp"].startswith(yesterday)
        ]
        
        # Update rollover tracking
        rollover_summary = {
            "rollover_date": today,
            "previous_date": yesterday,
            "ideas_rolled": len(yesterday_ideas),
            "promoted_count": len([idea for idea in yesterday_ideas if idea.get("promoted", False)]),
            "by_tag": {}
        }
        
        # Count by tag
        for idea in yesterday_ideas:
            tag = idea.get("tag", "untagged")
            rollover_summary["by_tag"][tag] = rollover_summary["by_tag"].get(tag, 0) + 1
        
        # Update catalog metadata
        self.catalog["last_rollover"] = today
        if "rollover_history" not in self.catalog:
            self.catalog["rollover_history"] = []
        
        self.catalog["rollover_history"].append(rollover_summary)
        
        # Keep only last 30 days of rollover history
        self.catalog["rollover_history"] = self.catalog["rollover_history"][-30:]
        
        self._save_catalog()
        
        return rollover_summary
    
    def _update_daily_stats(self, action: str):
        """Update daily statistics"""
        today = datetime.date.today().isoformat()
        
        if today not in self.catalog["daily_stats"]:
            self.catalog["daily_stats"][today] = {
                "added": 0,
                "promoted": 0,
                "classified": 0
            }
        
        if action in self.catalog["daily_stats"][today]:
            self.catalog["daily_stats"][today][action] += 1
    
    def get_stats(self) -> Dict[str, Any]:
        """Get catalog statistics"""
        total_ideas = len(self.catalog["ideas"])
        promoted_ideas = len([idea for idea in self.catalog["ideas"] if idea.get("promoted", False)])
        
        # Count by tag
        tag_counts = {}
        for idea in self.catalog["ideas"]:
            tag = idea.get("tag", "untagged")
            tag_counts[tag] = tag_counts.get(tag, 0) + 1
        
        return {
            "total_ideas": total_ideas,
            "promoted_ideas": promoted_ideas,
            "by_tag": tag_counts,
            "catalog_created": self.catalog["created_at"],
            "last_updated": self.catalog["last_updated"],
            "last_rollover": self.catalog.get("last_rollover"),
            "schema_version": self.catalog["schema_version"]
        }


def main():
    """CLI interface for idea catalog management"""
    import argparse
    
    parser = argparse.ArgumentParser(description="Idea Catalog - The Legendary Idea Vault")
    parser.add_argument('--add', help='Add raw idea to catalog')
    parser.add_argument('--classify', nargs=2, metavar=('ID', 'TAG'), 
                       help='Classify idea with tag (!, ?, ⧗, ♻)')
    parser.add_argument('--promote', help='Promote idea by ID')
    parser.add_argument('--list', action='store_true', help='List ideas')
    parser.add_argument('--filter-tag', help='Filter list by tag')
    parser.add_argument('--limit', type=int, default=50, help='Limit number of results')
    parser.add_argument('--stats', action='store_true', help='Show catalog statistics')
    parser.add_argument('--rollover', action='store_true', help='Perform daily rollover')
    parser.add_argument('--catalog-path', default='data/idea_catalog.json', 
                       help='Path to catalog file')
    
    args = parser.parse_args()
    
    catalog = IdeaCatalog(args.catalog_path)
    
    if args.add:
        idea_id = catalog.add_raw(args.add)
        print(f"✨ Idea captured with ID: {idea_id}")
        
    elif args.classify:
        idea_id, tag = args.classify
        if catalog.classify(idea_id, tag):
            print(f"🏷️ Idea {idea_id} classified as '{tag}'")
        else:
            print(f"❌ Idea {idea_id} not found")
            
    elif args.promote:
        record = catalog.promote(args.promote)
        if record:
            print(f"🚀 Idea {args.promote} promoted to structured record")
            print(json.dumps(record, indent=2))
        else:
            print(f"❌ Idea {args.promote} not found or already promoted")
            
    elif args.list:
        ideas = catalog.list(filter_tag=args.filter_tag, limit=args.limit)
        print(f"💡 Idea Vault Contents ({len(ideas)} ideas):")
        print("=" * 50)
        for idea in ideas:
            tag_display = f" [{idea.get('tag', '')}]" if idea.get('tag') else ""
            promoted_display = " 🚀" if idea.get('promoted') else ""
            print(f"{idea['id']}: {idea['text'][:60]}...{tag_display}{promoted_display}")
            
    elif args.stats:
        stats = catalog.get_stats()
        print("📊 Idea Catalog Statistics:")
        print("=" * 30)
        print(f"Total Ideas: {stats['total_ideas']}")
        print(f"Promoted Ideas: {stats['promoted_ideas']}")
        print(f"By Tag: {stats['by_tag']}")
        print(f"Created: {stats['catalog_created']}")
        print(f"Last Updated: {stats['last_updated']}")
        
    elif args.rollover:
        summary = catalog.daily_rollover()
        print(f"🔄 Daily rollover completed:")
        print(f"  Ideas from {summary['previous_date']}: {summary['ideas_rolled']}")
        print(f"  Promoted: {summary['promoted_count']}")
        print(f"  By tag: {summary['by_tag']}")
        
    else:
        parser.print_help()


if __name__ == "__main__":
    main()