#!/usr/bin/env python3
"""
Streak Monitor - Analyze Idea Implementation Health
Jerry's legendary streak analysis for idea charter implementation tracking
"""

import argparse
import json
import os
import re
import yaml
from datetime import datetime, timedelta
from pathlib import Path
from typing import Dict, List, Any, Optional

class StreakAnalyzer:
    """The Bootstrap Sentinel's streak analysis system"""
    
    def __init__(self, tldl_path: str, charters_path: str, evaluation_window: int = 14):
        self.tldl_path = Path(tldl_path)
        self.charters_path = Path(charters_path)
        self.evaluation_window = evaluation_window
        self.cutoff_date = datetime.now() - timedelta(days=evaluation_window)
        
    def find_idea_charters(self) -> List[Dict[str, Any]]:
        """Find all idea charter files and extract metadata"""
        charters = []
        
        # Look in docs directory and charters subdirectory
        charter_paths = []
        if self.charters_path.exists():
            charter_paths.extend(self.charters_path.glob("**/*.md"))
        
        # Also check docs directory for charter files
        docs_path = Path("docs")
        if docs_path.exists():
            charter_paths.extend(docs_path.glob("**/IDEA-*.md"))
        
        for charter_file in charter_paths:
            try:
                with open(charter_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                charter_info = self.parse_charter_metadata(content, charter_file)
                if charter_info:
                    charters.append(charter_info)
                    
            except Exception as e:
                print(f"Warning: Failed to parse charter {charter_file}: {e}")
        
        return charters
    
    def parse_charter_metadata(self, content: str, file_path: Path) -> Optional[Dict[str, Any]]:
        """Extract metadata from charter content"""
        # Look for charter ID pattern
        charter_id_match = re.search(r'IDEA-(\d{4})-(\d{2})-(\d{2})-([^\\s]+)', str(file_path))
        if not charter_id_match:
            charter_id_match = re.search(r'Charter ID.*?IDEA-(\d{4})-(\d{2})-(\d{2})-([^\\s]+)', content)
        
        if not charter_id_match:
            return None
        
        year, month, day, title_slug = charter_id_match.groups()
        charter_date = datetime(int(year), int(month), int(day))
        
        # Extract status
        status_match = re.search(r'Status.*?:\s*([^\\n\\]]+)', content, re.IGNORECASE)
        status = status_match.group(1).strip() if status_match else "Unknown"
        
        # Extract title from content or filename
        title_match = re.search(r'# .*?([^\\n]+)', content)
        title = title_match.group(1).strip() if title_match else title_slug.replace('-', ' ')
        
        return {
            'charter_id': f"IDEA-{year}-{month}-{day}-{title_slug}",
            'title': title,
            'status': status,
            'created_date': charter_date,
            'file_path': str(file_path),
            'within_window': charter_date >= self.cutoff_date
        }
    
    def find_idea_implementations(self, charters: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """Find TLDL entries that reference charter implementations"""
        implementations = []
        
        if not self.tldl_path.exists():
            return implementations
        
        tldl_files = list(self.tldl_path.glob("**/*.md"))
        
        for tldl_file in tldl_files:
            try:
                with open(tldl_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Look for implementation references
                impl_info = self.parse_implementation_references(content, tldl_file, charters)
                if impl_info:
                    implementations.extend(impl_info)
                    
            except Exception as e:
                print(f"Warning: Failed to parse TLDL {tldl_file}: {e}")
        
        return implementations
    
    def parse_implementation_references(self, content: str, file_path: Path, charters: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """Look for charter implementation references in TLDL content"""
        implementations = []
        
        # Extract file date
        date_match = re.search(r'TLDL-(\d{4})-(\d{2})-(\d{2})', str(file_path))
        if not date_match:
            return implementations
        
        year, month, day = date_match.groups()
        tldl_date = datetime(int(year), int(month), int(day))
        
        # Look for charter references
        charter_patterns = [
            r'Charter.*?Reference.*?:\s*([^\\n]+)',
            r'IDEA-(\d{4}-\d{2}-\d{2}-[^\\s]+)',
            r'charter.*?([A-Z][a-zA-Z0-9-]+)',
            r'Implemented.*?([A-Z][a-zA-Z0-9-]+)'
        ]
        
        for pattern in charter_patterns:
            matches = re.finditer(pattern, content, re.IGNORECASE)
            for match in matches:
                charter_ref = match.group(1)
                
                # Match against known charters
                for charter in charters:
                    if (charter_ref in charter['charter_id'] or 
                        charter_ref.lower() in charter['title'].lower()):
                        
                        implementations.append({
                            'charter_id': charter['charter_id'],
                            'charter_title': charter['title'],
                            'implementation_date': tldl_date,
                            'tldl_file': str(file_path),
                            'implementation_type': self.detect_implementation_type(content),
                            'within_window': tldl_date >= self.cutoff_date
                        })
        
        return implementations
    
    def detect_implementation_type(self, content: str) -> str:
        """Detect the type of implementation from TLDL content"""
        content_lower = content.lower()
        
        if any(word in content_lower for word in ['complete', 'finished', 'done', 'implemented']):
            return 'complete'
        elif any(word in content_lower for word in ['partial', 'progress', 'ongoing']):
            return 'partial'
        elif any(word in content_lower for word in ['abandoned', 'cancelled', 'killed']):
            return 'abandoned'
        else:
            return 'in_progress'
    
    def calculate_streak_metrics(self, charters: List[Dict[str, Any]], implementations: List[Dict[str, Any]]) -> Dict[str, Any]:
        """Calculate streak health metrics"""
        # Filter for evaluation window
        window_charters = [c for c in charters if c['within_window']]
        window_implementations = [i for i in implementations if i['within_window']]
        
        total_charters = len(window_charters)
        
        # Count implementations by type
        complete_implementations = len([i for i in window_implementations if i['implementation_type'] == 'complete'])
        partial_implementations = len([i for i in window_implementations if i['implementation_type'] == 'partial'])
        abandoned_implementations = len([i for i in window_implementations if i['implementation_type'] == 'abandoned'])
        
        # Calculate ratios
        implementation_ratio = complete_implementations / total_charters if total_charters > 0 else 0
        
        # Determine streak status based on thresholds
        if implementation_ratio >= 0.30:
            streak_status = 'healthy'
        elif implementation_ratio >= 0.15:
            streak_status = 'concerning'
        else:
            streak_status = 'critical'
        
        # Find stalled charters
        stalled_charters = []
        current_date = datetime.now()
        for charter in window_charters:
            days_since_creation = (current_date - charter['created_date']).days
            if (days_since_creation > 7 and 
                charter['charter_id'] not in [i['charter_id'] for i in window_implementations]):
                stalled_charters.append({
                    'charter_id': charter['charter_id'],
                    'title': charter['title'],
                    'status': charter['status'],
                    'days_stalled': days_since_creation
                })
        
        return {
            'evaluation_window': self.evaluation_window,
            'cutoff_date': self.cutoff_date.isoformat(),
            'analysis_date': datetime.now().isoformat(),
            'total_charters': total_charters,
            'implementations_completed': complete_implementations,
            'implementations_partial': partial_implementations,
            'ideas_abandoned': abandoned_implementations,
            'ideas_in_progress': total_charters - complete_implementations - abandoned_implementations,
            'implementation_ratio': implementation_ratio,
            'streak_status': streak_status,
            'threshold_details': {
                'healthy': 0.30,
                'concerning': 0.15,
                'critical': 0.05
            },
            'stalled_charters': stalled_charters,
            'charter_details': window_charters,
            'implementation_details': window_implementations
        }
    
    def analyze(self) -> Dict[str, Any]:
        """Perform complete streak analysis"""
        print(f"🔍 Analyzing idea implementation streaks over {self.evaluation_window} days...")
        
        # Find all charters and implementations
        charters = self.find_idea_charters()
        implementations = self.find_idea_implementations(charters)
        
        print(f"📊 Found {len(charters)} charters, {len(implementations)} implementations")
        
        # Calculate metrics
        metrics = self.calculate_streak_metrics(charters, implementations)
        
        return metrics

def main():
    parser = argparse.ArgumentParser(description="Analyze idea implementation streaks")
    parser.add_argument('--tldl-path', required=True, help='Path to TLDL entries')
    parser.add_argument('--charters-path', required=True, help='Path to idea charters')
    parser.add_argument('--window', type=int, default=14, help='Evaluation window in days')
    parser.add_argument('--output-format', default='json', choices=['json', 'github'], help='Output format')
    
    args = parser.parse_args()
    
    # Create output directory
    os.makedirs('out', exist_ok=True)
    
    # Perform analysis
    analyzer = StreakAnalyzer(args.tldl_path, args.charters_path, args.window)
    results = analyzer.analyze()
    
    # Output results
    if args.output_format == 'github':
        # Set GitHub Actions outputs
        print(f"::set-output name=streak_status::{results['streak_status']}")
        print(f"::set-output name=implementation_ratio::{results['implementation_ratio']:.3f}")
        print(f"::set-output name=total_charters::{results['total_charters']}")
    
    # Save detailed results
    with open('out/streak-analysis.json', 'w') as f:
        json.dump(results, f, indent=2, default=str)
    
    print(f"✅ Analysis complete - {results['streak_status'].upper()} status detected")
    print(f"📈 Implementation ratio: {results['implementation_ratio']:.3f}")

if __name__ == '__main__':
    main()