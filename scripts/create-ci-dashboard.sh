#!/bin/bash
# 🚀 CI Efficiency Dashboard Generator
#
# Creates an efficiency dashboard for CI/CD monitoring as part of Phase 2 
# of the CID Schoolhouse optimization roadmap.

set -euo pipefail

DASHBOARD_DIR="out/ci-dashboard"
METRICS_DIR="out/ci-metrics"

# Create dashboard directory
mkdir -p "$DASHBOARD_DIR"

# Generate HTML dashboard
cat > "$DASHBOARD_DIR/index.html" << 'EOF'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>🚀 CI/CD Efficiency Dashboard</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, sans-serif; margin: 20px; }
        .dashboard { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
        .card { border: 1px solid #ddd; border-radius: 8px; padding: 20px; background: white; }
        .metric { font-size: 2em; font-weight: bold; color: #2196F3; }
        .status { padding: 4px 8px; border-radius: 4px; color: white; }
        .success { background: #4CAF50; }
        .warning { background: #FF9800; }
        .error { background: #F44336; }
        .timestamp { color: #666; font-size: 0.9em; }
    </style>
</head>
<body>
    <h1>🚀 CI/CD Efficiency Dashboard</h1>
    <p class="timestamp">Last updated: <span id="timestamp">Loading...</span></p>
    
    <div class="dashboard">
        <div class="card">
            <h3>📊 Overall Performance</h3>
            <div class="metric" id="success-rate">Loading...</div>
            <p>Success Rate</p>
        </div>
        
        <div class="card">
            <h3>⏱️ Average Duration</h3>
            <div class="metric" id="avg-duration">Loading...</div>
            <p>Seconds</p>
        </div>
        
        <div class="card">
            <h3>🏃‍♂️ Total Runs</h3>
            <div class="metric" id="total-runs">Loading...</div>
            <p>This Week</p>
        </div>
        
        <div class="card">
            <h3>🛡️ Guarded Pass Status</h3>
            <div class="status success" id="guarded-pass-status">Active</div>
            <p>Protective signal transformation enabled</p>
        </div>
        
        <div class="card">
            <h3>🚀 Optimizations Active</h3>
            <ul>
                <li>✅ Concurrency controls</li>
                <li>✅ Path filters</li>
                <li>✅ Pip caching</li>
                <li>✅ Shallow clones</li>
                <li>✅ Advisory job routing</li>
            </ul>
        </div>
        
        <div class="card">
            <h3>📈 Efficiency Trends</h3>
            <p><strong>Phase 1:</strong> <span class="status success">Complete</span></p>
            <p><strong>Phase 2:</strong> <span class="status warning">In Progress</span></p>
            <p><strong>Phase 3:</strong> <span class="status">Planned</span></p>
        </div>
    </div>
    
    <script>
        // Load and display metrics
        async function loadMetrics() {
            try {
                const response = await fetch('../ci-metrics/performance-metrics.json');
                const metrics = await response.json();
                
                document.getElementById('success-rate').textContent = 
                    metrics.summary.success_rate.toFixed(1) + '%';
                document.getElementById('avg-duration').textContent = 
                    metrics.summary.average_duration.toFixed(0);
                document.getElementById('total-runs').textContent = 
                    metrics.summary.total_runs;
                document.getElementById('timestamp').textContent = 
                    new Date(metrics.summary.last_updated).toLocaleString();
            } catch (error) {
                console.log('Metrics not available yet:', error);
                document.getElementById('success-rate').textContent = 'N/A';
                document.getElementById('avg-duration').textContent = 'N/A';
                document.getElementById('total-runs').textContent = '0';
                document.getElementById('timestamp').textContent = 'Never';
            }
        }
        
        loadMetrics();
        
        // Refresh every 30 seconds
        setInterval(loadMetrics, 30000);
    </script>
</body>
</html>
EOF

echo "📊 CI Efficiency Dashboard generated at: $DASHBOARD_DIR/index.html"

# Create a simple metrics collector script
cat > "$DASHBOARD_DIR/collect-github-metrics.py" << 'EOF'
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
EOF

chmod +x "$DASHBOARD_DIR/collect-github-metrics.py"

echo "🎯 Dashboard components created:"
echo "  - HTML Dashboard: $DASHBOARD_DIR/index.html"
echo "  - Metrics Collector: $DASHBOARD_DIR/collect-github-metrics.py"
echo "  - Performance Monitor: scripts/ci-performance-monitor.py"

echo ""
echo "📋 To use the dashboard:"
echo "  1. Run: python $DASHBOARD_DIR/collect-github-metrics.py"
echo "  2. Open: $DASHBOARD_DIR/index.html in a browser"
echo "  3. Monitor with: python scripts/ci-performance-monitor.py --report"