#!/usr/bin/env python3
"""
Streak Monitor - Health Check System
Evaluates streak analysis results against thresholds and determines actions
"""

import argparse
import json
import yaml
import sys
from pathlib import Path

def load_thresholds(config_path: str) -> dict:
    """Load streak thresholds from pet schema configuration"""
    try:
        with open(config_path, 'r') as f:
            config = yaml.safe_load(f)
        return config.get('streak_monitoring', {})
    except Exception as e:
        print(f"Warning: Could not load config from {config_path}: {e}")
        # Fallback thresholds
        return {
            'idea_streak_thresholds': {
                'healthy': 0.3,
                'concerning': 0.15,
                'critical': 0.05
            }
        }

def evaluate_health(analysis: dict, config: dict, alert_threshold: str = 'concerning') -> dict:
    """Evaluate streak health and determine required actions"""
    
    streak_status = analysis['streak_status']
    implementation_ratio = analysis['implementation_ratio']
    thresholds = config.get('idea_streak_thresholds', {})
    
    # Determine alert level
    alert_levels = ['healthy', 'concerning', 'critical']
    alert_threshold_index = alert_levels.index(alert_threshold)
    current_level_index = alert_levels.index(streak_status)
    
    # Check if alert is needed
    alert_needed = current_level_index >= alert_threshold_index
    
    # Determine if worthy of TLDL generation
    tldl_worthy = (
        streak_status == 'critical' or 
        (streak_status == 'concerning' and len(analysis.get('stalled_charters', [])) > 3) or
        (streak_status == 'healthy' and implementation_ratio > 0.5)  # Exceptional performance
    )
    
    # Chronicle Keeper worthiness
    chronicle_worthy = (
        streak_status == 'critical' or
        (streak_status == 'healthy' and implementation_ratio > 0.6)  # Major milestone
    )
    
    # Recommended actions based on status
    actions = []
    if streak_status == 'critical':
        actions.extend([
            'Immediate charter audit required',
            'Review kill criteria for stalled ideas', 
            'Assess resource allocation and capacity',
            'Consider simplifying charter requirements'
        ])
    elif streak_status == 'concerning':
        actions.extend([
            'Review current charter priorities',
            'Check for implementation blockers',
            'Consider pairing/collaboration on stalled ideas',
            'Evaluate charter complexity vs capacity'
        ])
    else:  # healthy
        actions.extend([
            'Maintain current implementation pace',
            'Consider increasing charter complexity',
            'Share successful patterns with team'
        ])
    
    return {
        'alert_needed': alert_needed,
        'tldl_worthy': tldl_worthy,
        'chronicle_worthy': chronicle_worthy,
        'recommended_actions': actions,
        'health_summary': {
            'status': streak_status,
            'ratio': implementation_ratio,
            'threshold_met': streak_status,
            'alert_threshold': alert_threshold,
            'days_evaluated': analysis['evaluation_window']
        }
    }

def main():
    parser = argparse.ArgumentParser(description="Health check for streak analysis")
    parser.add_argument('--analysis-file', required=True, help='Path to streak analysis JSON')
    parser.add_argument('--config', required=True, help='Path to pet schema config')
    parser.add_argument('--alert-threshold', default='concerning', 
                       choices=['healthy', 'concerning', 'critical'],
                       help='Threshold for generating alerts')
    
    args = parser.parse_args()
    
    # Load analysis results
    try:
        with open(args.analysis_file, 'r') as f:
            analysis = json.load(f)
    except Exception as e:
        print(f"Error: Could not load analysis file: {e}")
        sys.exit(1)
    
    # Load configuration
    config = load_thresholds(args.config)
    
    # Perform health evaluation
    health_check = evaluate_health(analysis, config, args.alert_threshold)
    
    # Output GitHub Actions variables
    print(f"::set-output name=alert_needed::{str(health_check['alert_needed']).lower()}")
    print(f"::set-output name=tldl_worthy::{str(health_check['tldl_worthy']).lower()}")
    print(f"::set-output name=chronicle_worthy::{str(health_check['chronicle_worthy']).lower()}")
    
    # Save health check results
    with open('out/health-check.json', 'w') as f:
        json.dump(health_check, f, indent=2)
    
    print(f"🏥 Health check complete: {health_check['health_summary']['status'].upper()}")
    
    if health_check['alert_needed']:
        print(f"🚨 Alert threshold reached: {args.alert_threshold}")
        sys.exit(2)  # Indicate alert condition
    
    sys.exit(0)

if __name__ == '__main__':
    main()