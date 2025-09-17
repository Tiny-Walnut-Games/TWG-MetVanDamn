#!/usr/bin/env python3
"""
DevTimeTravel Semantic Similarity / Dedupe Prototype
Lightweight textual similarity detection for decision rationale and context fields.
Uses only standard library - no external dependencies.
"""

import os
import sys
import json
import yaml
import re
from pathlib import Path
from typing import Dict, List, Set, Tuple, Any
import string
from collections import defaultdict


class SimilarityAnalyzer:
    """Lightweight textual similarity analyzer using Jaccard similarity"""
    
    # Common English stopwords
    STOPWORDS = {
        'a', 'an', 'and', 'are', 'as', 'at', 'be', 'by', 'for', 'from', 'has', 'he',
        'in', 'is', 'it', 'its', 'of', 'on', 'that', 'the', 'to', 'was', 'will', 'with',
        'but', 'or', 'not', 'this', 'can', 'had', 'have', 'been', 'were', 'said', 'each',
        'which', 'she', 'do', 'how', 'their', 'if', 'would', 'there', 'all', 'we', 'when'
    }
    
    def __init__(self, similarity_threshold: float = 0.9):
        self.similarity_threshold = similarity_threshold
    
    def tokenize(self, text: str) -> Set[str]:
        """Tokenize text into lowercase alphanumeric tokens, removing stopwords"""
        if not text:
            return set()
        
        # Convert to lowercase and extract alphanumeric tokens
        text = text.lower()
        # Replace punctuation with spaces
        text = text.translate(str.maketrans(string.punctuation, ' ' * len(string.punctuation)))
        # Split into words and filter
        tokens = set()
        for word in text.split():
            # Only alphanumeric tokens with length > 2
            if word.isalnum() and len(word) > 2 and word not in self.STOPWORDS:
                tokens.add(word)
        
        return tokens
    
    def jaccard_similarity(self, set1: Set[str], set2: Set[str]) -> float:
        """Calculate Jaccard similarity between two token sets"""
        if not set1 and not set2:
            return 1.0
        if not set1 or not set2:
            return 0.0
        
        intersection = set1.intersection(set2)
        union = set1.union(set2)
        
        return len(intersection) / len(union)
    
    def extract_text_content(self, data: Dict[str, Any]) -> str:
        """Extract relevant text content from snapshot data"""
        content_parts = []
        
        # Common fields to check for text content
        text_fields = [
            'decision_rationale', 'rationale', 'context', 'description', 
            'summary', 'notes', 'objective', 'discovery', 'insights',
            'lessons_learned', 'what_worked', 'challenges', 'outcome'
        ]
        
        def extract_from_dict(d, path=""):
            if isinstance(d, dict):
                for key, value in d.items():
                    current_path = f"{path}.{key}" if path else key
                    if key.lower() in [field.lower() for field in text_fields]:
                        if isinstance(value, str):
                            content_parts.append(value)
                        elif isinstance(value, (list, dict)):
                            content_parts.append(str(value))
                    elif isinstance(value, (dict, list)):
                        extract_from_dict(value, current_path)
            elif isinstance(d, list):
                for i, item in enumerate(d):
                    extract_from_dict(item, f"{path}[{i}]")
        
        extract_from_dict(data)
        return ' '.join(content_parts)
    
    def find_duplicates(self, snapshots: Dict[str, Dict[str, Any]]) -> Dict[str, List[str]]:
        """Find duplicate clusters using similarity threshold"""
        # Extract text content and tokenize
        snapshot_tokens = {}
        for filepath, data in snapshots.items():
            text_content = self.extract_text_content(data)
            snapshot_tokens[filepath] = self.tokenize(text_content)
        
        # Find similarity clusters
        clusters = {}
        processed = set()
        cluster_id = 0
        
        for filepath1, tokens1 in snapshot_tokens.items():
            if filepath1 in processed:
                continue
            
            # Start new cluster
            cluster = [filepath1]
            processed.add(filepath1)
            
            # Find similar snapshots
            for filepath2, tokens2 in snapshot_tokens.items():
                if filepath2 in processed:
                    continue
                
                similarity = self.jaccard_similarity(tokens1, tokens2)
                if similarity >= self.similarity_threshold:
                    cluster.append(filepath2)
                    processed.add(filepath2)
            
            # Only include clusters with multiple items
            if len(cluster) > 1:
                clusters[f"cluster_{cluster_id}"] = cluster
                cluster_id += 1
        
        return clusters


class DedupeAnalyzer:
    """Main dedupe analyzer for DevTimeTravel snapshots"""
    
    def __init__(self, root_path: str = ".devtimetravel", similarity_threshold: float = 0.9):
        self.root_path = Path(root_path)
        self.similarity_analyzer = SimilarityAnalyzer(similarity_threshold)
        self.report_path = self.root_path / "dedupe_report.json"
    
    def load_yaml_file(self, filepath: Path) -> Dict[str, Any]:
        """Load YAML file content"""
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                return yaml.safe_load(f) or {}
        except Exception as e:
            print(f"Warning: Could not load {filepath}: {e}")
            return {}
    
    def collect_snapshots(self) -> Dict[str, Dict[str, Any]]:
        """Collect all snapshots from raw and compacted directories"""
        snapshots = {}
        
        # Search patterns for snapshot files
        search_paths = [
            self.root_path / "snapshots",  # Raw snapshots
            self.root_path / "daily",      # Daily compacted
            self.root_path / "weekly",     # Weekly aggregates
            self.root_path / "monthly",    # Monthly aggregates
        ]
        
        for search_path in search_paths:
            if search_path.exists():
                for yaml_file in search_path.glob("*.yaml"):
                    relative_path = str(yaml_file.relative_to(self.root_path))
                    snapshots[relative_path] = self.load_yaml_file(yaml_file)
                
                for yml_file in search_path.glob("*.yml"):
                    relative_path = str(yml_file.relative_to(self.root_path))
                    snapshots[relative_path] = self.load_yaml_file(yml_file)
        
        return snapshots
    
    def select_representative(self, cluster: List[str]) -> str:
        """Select representative file from cluster (most recent or largest)"""
        if len(cluster) == 1:
            return cluster[0]
        
        # Sort by modification time (most recent first)
        cluster_paths = [(f, (self.root_path / f).stat().st_mtime) for f in cluster]
        cluster_paths.sort(key=lambda x: x[1], reverse=True)
        
        return cluster_paths[0][0]
    
    def analyze_duplicates(self) -> Dict[str, Any]:
        """Run duplicate analysis and generate report"""
        print("🔍 Collecting snapshots for similarity analysis...")
        snapshots = self.collect_snapshots()
        
        if not snapshots:
            print("ℹ️  No snapshots found for analysis")
            return {
                "timestamp": self.get_timestamp(),
                "total_snapshots": 0,
                "clusters": {},
                "statistics": {
                    "total_clusters": 0,
                    "total_duplicates": 0,
                    "potential_savings": 0
                }
            }
        
        print(f"📊 Found {len(snapshots)} snapshots, analyzing similarity...")
        
        # Find duplicate clusters
        clusters = self.similarity_analyzer.find_duplicates(snapshots)
        
        # Process clusters and select representatives
        processed_clusters = {}
        total_duplicates = 0
        
        for cluster_id, cluster_files in clusters.items():
            representative = self.select_representative(cluster_files)
            duplicates = [f for f in cluster_files if f != representative]
            
            processed_clusters[cluster_id] = {
                "representative": representative,
                "duplicates": duplicates,
                "duplicate_count": len(duplicates),
                "total_files": len(cluster_files),
                "similarity_score": self.similarity_analyzer.similarity_threshold
            }
            
            total_duplicates += len(duplicates)
        
        # Generate report
        report = {
            "timestamp": self.get_timestamp(),
            "total_snapshots": len(snapshots),
            "similarity_threshold": self.similarity_analyzer.similarity_threshold,
            "clusters": processed_clusters,
            "statistics": {
                "total_clusters": len(processed_clusters),
                "total_duplicates": total_duplicates,
                "potential_savings": total_duplicates,
                "deduplication_ratio": (total_duplicates / len(snapshots)) if snapshots else 0
            }
        }
        
        return report
    
    def get_timestamp(self) -> str:
        """Get current timestamp in ISO format"""
        from datetime import datetime
        return datetime.now().isoformat()
    
    def save_report(self, report: Dict[str, Any]) -> bool:
        """Save dedupe report to JSON file"""
        try:
            with open(self.report_path, 'w', encoding='utf-8') as f:
                json.dump(report, f, indent=2, sort_keys=True)
            return True
        except Exception as e:
            print(f"❌ Error saving report: {e}")
            return False
    
    def print_report_summary(self, report: Dict[str, Any]):
        """Print human-readable report summary"""
        stats = report["statistics"]
        
        print(f"\n📋 Similarity Analysis Report")
        print(f"   Snapshots analyzed: {report['total_snapshots']}")
        print(f"   Similarity threshold: {report['similarity_threshold']}")
        print(f"   Duplicate clusters found: {stats['total_clusters']}")
        print(f"   Total duplicates: {stats['total_duplicates']}")
        print(f"   Potential space savings: {stats['potential_savings']} files")
        print(f"   Deduplication ratio: {stats['deduplication_ratio']:.1%}")
        
        if report["clusters"]:
            print(f"\n🔍 Duplicate Clusters:")
            for cluster_id, cluster in report["clusters"].items():
                print(f"   {cluster_id}:")
                print(f"     Representative: {cluster['representative']}")
                print(f"     Duplicates ({cluster['duplicate_count']}): {', '.join(cluster['duplicates'])}")
        
        print(f"\n📄 Full report saved to: {self.report_path}")


def main():
    import argparse
    
    parser = argparse.ArgumentParser(description='DevTimeTravel Similarity Analysis and Deduplication')
    parser.add_argument('--root', default='.devtimetravel',
                       help='DevTimeTravel root directory (default: .devtimetravel)')
    parser.add_argument('--threshold', type=float, default=0.9,
                       help='Similarity threshold for duplicate detection (default: 0.9)')
    parser.add_argument('--quiet', action='store_true',
                       help='Suppress output except errors')
    
    args = parser.parse_args()
    
    # Validate threshold
    if not 0.0 <= args.threshold <= 1.0:
        parser.error("Threshold must be between 0.0 and 1.0")
    
    analyzer = DedupeAnalyzer(args.root, args.threshold)
    
    # Run analysis
    report = analyzer.analyze_duplicates()
    
    # Save report
    if analyzer.save_report(report):
        if not args.quiet:
            analyzer.print_report_summary(report)
            print("✅ Similarity analysis complete")
    else:
        print("❌ Failed to save report")
        sys.exit(1)


if __name__ == '__main__':
    main()