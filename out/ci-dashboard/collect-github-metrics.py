#!/usr/bin/env python3
"""
Collect GitHub Actions metrics for dashboard
"""
import os
import requests
import json
from datetime import datetime

def collect_workflow_runs():
    """Collect recent workflow runs from GitHub API"""
    # This would integrate with GitHub API in a real implementation
    # For now, create sample data
    
    sample_data = {
        "runs": [
            {
                "workflow": "ci",
                "run_id": "sample",
                "timestamp": datetime.utcnow().isoformat(),
                "duration_seconds": 180,
                "status": "success"
            }
        ],
        "summary": {
            "total_runs": 1,
            "success_rate": 100.0,
            "average_duration": 180,
            "last_updated": datetime.utcnow().isoformat()
        }
    }
    
    os.makedirs("../ci-metrics", exist_ok=True)
    with open("../ci-metrics/performance-metrics.json", "w") as f:
        json.dump(sample_data, f, indent=2)
    
    print("📊 Sample metrics generated for dashboard")

if __name__ == "__main__":
    collect_workflow_runs()
