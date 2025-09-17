#!/usr/bin/env python3
"""
Regression Dashboard and Time Series Analytics v0.7

Provides regression tracking, time series analysis, and dashboard generation
for experiment results and performance metrics over time.

🧙‍♂️ "In the garden of optimization, regression is the frost that reminds us 
    winter can return - monitor well, and spring will come again." - Bootstrap Sentinel
"""

from __future__ import annotations
import sys
import json
import time
import sqlite3
import datetime
from pathlib import Path
from typing import Dict, Any, List, Optional, Tuple, Union
from dataclasses import dataclass, asdict
from enum import Enum
import statistics

# Add engine to path for imports
sys.path.insert(0, str(Path(__file__).parent))

try:
    from experiment_harness import ExperimentHarness, TimeSeriesMetric
    DEPENDENCIES_AVAILABLE = True
except ImportError:
    DEPENDENCIES_AVAILABLE = False


class RegressionSeverity(Enum):
    """Severity levels for regression detection."""
    MINOR = "minor"      # 5-15% degradation
    MODERATE = "moderate"  # 15-30% degradation  
    MAJOR = "major"      # 30-50% degradation
    CRITICAL = "critical"  # >50% degradation


class TrendDirection(Enum):
    """Direction of performance trends."""
    IMPROVING = "improving"
    STABLE = "stable"
    DEGRADING = "degrading"
    VOLATILE = "volatile"


@dataclass
class RegressionAlert:
    """Alert for detected performance regression."""
    alert_id: str
    timestamp: float
    metric_name: str
    current_value: float
    baseline_value: float
    degradation_pct: float
    severity: RegressionSeverity
    experiment_context: Dict[str, Any]
    trend_context: Dict[str, Any]


@dataclass
class PerformanceTrend:
    """Performance trend analysis for a metric."""
    metric_name: str
    time_window_days: int
    direction: TrendDirection
    slope: float  # Change per day
    r_squared: float  # Trend confidence
    recent_mean: float
    baseline_mean: float
    volatility: float  # Standard deviation
    data_points: int


class RegressionTracker:
    """
    Advanced regression tracking and time series analysis.
    
    Features:
    - SQLite database for time series storage
    - Automated regression detection
    - Trend analysis and forecasting
    - Alert generation and dashboard export
    """
    
    def __init__(self, db_path: str = "experiments/regression_tracking.db"):
        self.db_path = Path(db_path)
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self._init_database()
        
        # Regression detection thresholds
        self.regression_thresholds = {
            RegressionSeverity.MINOR: 0.05,
            RegressionSeverity.MODERATE: 0.15,
            RegressionSeverity.MAJOR: 0.30,
            RegressionSeverity.CRITICAL: 0.50
        }
        
        self.alerts: List[RegressionAlert] = []
    
    def _init_database(self):
        """Initialize SQLite database for time series storage."""
        with sqlite3.connect(self.db_path) as conn:
            conn.execute("""
                CREATE TABLE IF NOT EXISTS time_series_metrics (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp REAL NOT NULL,
                    metric_name TEXT NOT NULL,
                    value REAL NOT NULL,
                    experiment_id TEXT,
                    run_id TEXT,
                    condition_hash TEXT,
                    metadata TEXT,
                    created_at REAL DEFAULT (julianday('now'))
                )
            """)
            
            conn.execute("""
                CREATE INDEX IF NOT EXISTS idx_metric_time 
                ON time_series_metrics (metric_name, timestamp)
            """)
            
            conn.execute("""
                CREATE TABLE IF NOT EXISTS baselines (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    metric_name TEXT NOT NULL UNIQUE,
                    baseline_value REAL NOT NULL,
                    baseline_timestamp REAL NOT NULL,
                    confidence_interval_lower REAL,
                    confidence_interval_upper REAL,
                    sample_size INTEGER,
                    created_at REAL DEFAULT (julianday('now'))
                )
            """)
            
            conn.execute("""
                CREATE TABLE IF NOT EXISTS regression_alerts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    alert_id TEXT NOT NULL UNIQUE,
                    timestamp REAL NOT NULL,
                    metric_name TEXT NOT NULL,
                    current_value REAL NOT NULL,
                    baseline_value REAL NOT NULL,
                    degradation_pct REAL NOT NULL,
                    severity TEXT NOT NULL,
                    experiment_context TEXT,
                    resolved BOOLEAN DEFAULT FALSE,
                    created_at REAL DEFAULT (julianday('now'))
                )
            """)
    
    def record_time_series_data(self, metrics: List[TimeSeriesMetric]):
        """Record time series metrics to database."""
        with sqlite3.connect(self.db_path) as conn:
            for metric in metrics:
                conn.execute("""
                    INSERT INTO time_series_metrics 
                    (timestamp, metric_name, value, experiment_id, run_id, condition_hash, metadata)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    metric.timestamp,
                    metric.metric_name,
                    metric.value,
                    metric.experiment_id,
                    metric.run_id,
                    metric.condition_hash,
                    json.dumps(metric.metadata)
                ))
    
    def set_baseline(self, 
                    metric_name: str, 
                    baseline_value: float,
                    confidence_interval: Optional[Tuple[float, float]] = None,
                    sample_size: Optional[int] = None):
        """Set performance baseline for a metric."""
        with sqlite3.connect(self.db_path) as conn:
            ci_lower, ci_upper = confidence_interval or (None, None)
            
            conn.execute("""
                INSERT OR REPLACE INTO baselines 
                (metric_name, baseline_value, baseline_timestamp, 
                 confidence_interval_lower, confidence_interval_upper, sample_size)
                VALUES (?, ?, ?, ?, ?, ?)
            """, (
                metric_name, 
                baseline_value, 
                time.time(),
                ci_lower,
                ci_upper,
                sample_size
            ))
    
    def compute_baselines_from_recent_data(self, days: int = 7) -> Dict[str, float]:
        """Compute baselines from recent historical data."""
        cutoff_time = time.time() - (days * 24 * 3600)
        baselines = {}
        
        with sqlite3.connect(self.db_path) as conn:
            cursor = conn.execute("""
                SELECT metric_name, AVG(value) as avg_value, COUNT(*) as count
                FROM time_series_metrics 
                WHERE timestamp >= ?
                GROUP BY metric_name
                HAVING count >= 10
            """, (cutoff_time,))
            
            for metric_name, avg_value, count in cursor.fetchall():
                baselines[metric_name] = avg_value
                
                # Set as baseline
                self.set_baseline(metric_name, avg_value, sample_size=count)
        
        return baselines
    
    def check_for_regressions(self, 
                            recent_hours: int = 1,
                            min_data_points: int = 3) -> List[RegressionAlert]:
        """Check recent data for performance regressions."""
        cutoff_time = time.time() - (recent_hours * 3600)
        new_alerts = []
        
        with sqlite3.connect(self.db_path) as conn:
            # Get recent metrics with baselines
            cursor = conn.execute("""
                SELECT 
                    ts.metric_name,
                    AVG(ts.value) as recent_avg,
                    COUNT(ts.value) as data_points,
                    b.baseline_value,
                    MAX(ts.timestamp) as latest_timestamp,
                    MAX(ts.experiment_id) as latest_experiment
                FROM time_series_metrics ts
                JOIN baselines b ON ts.metric_name = b.metric_name
                WHERE ts.timestamp >= ?
                GROUP BY ts.metric_name
                HAVING data_points >= ?
            """, (cutoff_time, min_data_points))
            
            for row in cursor.fetchall():
                metric_name, recent_avg, data_points, baseline_value, latest_timestamp, latest_experiment = row
                
                # Calculate degradation
                if baseline_value > 0:
                    degradation_pct = (baseline_value - recent_avg) / baseline_value
                else:
                    degradation_pct = 0.0
                
                # Check if this is a regression (handle metrics where higher is better)
                is_regression = False
                if "error" in metric_name.lower() or "latency" in metric_name.lower():
                    # For error/latency metrics, higher is worse
                    is_regression = recent_avg > baseline_value * 1.05  # 5% threshold
                    degradation_pct = (recent_avg - baseline_value) / baseline_value
                else:
                    # For throughput/success metrics, lower is worse  
                    is_regression = degradation_pct > 0.05  # 5% threshold
                
                if is_regression:
                    # Determine severity
                    severity = self._classify_regression_severity(abs(degradation_pct))
                    
                    alert = RegressionAlert(
                        alert_id=f"reg_{int(time.time())}_{metric_name}",
                        timestamp=latest_timestamp,
                        metric_name=metric_name,
                        current_value=recent_avg,
                        baseline_value=baseline_value,
                        degradation_pct=degradation_pct,
                        severity=severity,
                        experiment_context={"experiment_id": latest_experiment},
                        trend_context={"data_points": data_points, "hours_analyzed": recent_hours}
                    )
                    
                    new_alerts.append(alert)
                    
                    # Store alert in database
                    self._store_alert(alert)
        
        self.alerts.extend(new_alerts)
        return new_alerts
    
    def analyze_trends(self, 
                      metric_name: str, 
                      days: int = 7) -> Optional[PerformanceTrend]:
        """Analyze performance trends for a specific metric."""
        cutoff_time = time.time() - (days * 24 * 3600)
        
        with sqlite3.connect(self.db_path) as conn:
            cursor = conn.execute("""
                SELECT timestamp, value
                FROM time_series_metrics
                WHERE metric_name = ? AND timestamp >= ?
                ORDER BY timestamp
            """, (metric_name, cutoff_time))
            
            data_points = cursor.fetchall()
        
        if len(data_points) < 10:  # Need minimum data points
            return None
        
        # Extract time series data
        times = [row[0] for row in data_points]
        values = [row[1] for row in data_points]
        
        # Calculate trend using linear regression
        slope, r_squared = self._calculate_trend(times, values)
        
        # Calculate recent vs baseline means
        recent_cutoff = time.time() - (days * 24 * 3600 / 3)  # Last 1/3 of period
        recent_values = [v for t, v in data_points if t >= recent_cutoff]
        baseline_values = [v for t, v in data_points if t < recent_cutoff]
        
        recent_mean = statistics.mean(recent_values) if recent_values else statistics.mean(values)
        baseline_mean = statistics.mean(baseline_values) if baseline_values else statistics.mean(values)
        
        # Determine trend direction
        if abs(slope) < 0.01:  # Minimal change
            direction = TrendDirection.STABLE
        elif r_squared < 0.3:  # Low correlation
            direction = TrendDirection.VOLATILE
        elif slope > 0:
            direction = TrendDirection.IMPROVING if "error" not in metric_name.lower() else TrendDirection.DEGRADING
        else:
            direction = TrendDirection.DEGRADING if "error" not in metric_name.lower() else TrendDirection.IMPROVING
        
        # Calculate volatility
        volatility = statistics.stdev(values) if len(values) > 1 else 0.0
        
        return PerformanceTrend(
            metric_name=metric_name,
            time_window_days=days,
            direction=direction,
            slope=slope,
            r_squared=r_squared,
            recent_mean=recent_mean,
            baseline_mean=baseline_mean,
            volatility=volatility,
            data_points=len(data_points)
        )
    
    def generate_regression_dashboard(self, output_path: str = "experiments/regression_dashboard.html") -> str:
        """Generate HTML regression dashboard."""
        
        # Get recent alerts
        recent_alerts = self.get_recent_alerts(hours=24)
        
        # Get trend analysis for key metrics
        key_metrics = self._get_tracked_metrics()
        trends = {}
        for metric in key_metrics[:10]:  # Limit to top 10 metrics
            trend = self.analyze_trends(metric, days=7)
            if trend:
                trends[metric] = trend
        
        # Generate HTML
        html_content = self._generate_dashboard_html(recent_alerts, trends)
        
        # Write to file
        output_file = Path(output_path)
        output_file.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_file, 'w') as f:
            f.write(html_content)
        
        return str(output_file)
    
    def get_recent_alerts(self, hours: int = 24) -> List[RegressionAlert]:
        """Get recent regression alerts."""
        cutoff_time = time.time() - (hours * 3600)
        alerts = []
        
        with sqlite3.connect(self.db_path) as conn:
            cursor = conn.execute("""
                SELECT alert_id, timestamp, metric_name, current_value, baseline_value,
                       degradation_pct, severity, experiment_context
                FROM regression_alerts
                WHERE timestamp >= ? AND NOT resolved
                ORDER BY timestamp DESC
            """, (cutoff_time,))
            
            for row in cursor.fetchall():
                alert_id, timestamp, metric_name, current_value, baseline_value, degradation_pct, severity, exp_context = row
                
                alerts.append(RegressionAlert(
                    alert_id=alert_id,
                    timestamp=timestamp,
                    metric_name=metric_name,
                    current_value=current_value,
                    baseline_value=baseline_value,
                    degradation_pct=degradation_pct,
                    severity=RegressionSeverity(severity),
                    experiment_context=json.loads(exp_context or "{}"),
                    trend_context={}
                ))
        
        return alerts
    
    def _classify_regression_severity(self, degradation_pct: float) -> RegressionSeverity:
        """Classify regression severity based on degradation percentage."""
        for severity in [RegressionSeverity.CRITICAL, RegressionSeverity.MAJOR, 
                        RegressionSeverity.MODERATE, RegressionSeverity.MINOR]:
            if degradation_pct >= self.regression_thresholds[severity]:
                return severity
        return RegressionSeverity.MINOR
    
    def _store_alert(self, alert: RegressionAlert):
        """Store regression alert in database."""
        with sqlite3.connect(self.db_path) as conn:
            conn.execute("""
                INSERT OR REPLACE INTO regression_alerts
                (alert_id, timestamp, metric_name, current_value, baseline_value,
                 degradation_pct, severity, experiment_context)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?)
            """, (
                alert.alert_id,
                alert.timestamp,
                alert.metric_name,
                alert.current_value,
                alert.baseline_value,
                alert.degradation_pct,
                alert.severity.value,
                json.dumps(alert.experiment_context)
            ))
    
    def _calculate_trend(self, times: List[float], values: List[float]) -> Tuple[float, float]:
        """Calculate linear trend using least squares regression."""
        n = len(times)
        if n < 2:
            return 0.0, 0.0
        
        # Normalize times to days from first timestamp
        time_days = [(t - times[0]) / (24 * 3600) for t in times]
        
        # Calculate means
        mean_time = statistics.mean(time_days)
        mean_value = statistics.mean(values)
        
        # Calculate slope and correlation
        numerator = sum((t - mean_time) * (v - mean_value) for t, v in zip(time_days, values))
        denominator = sum((t - mean_time) ** 2 for t in time_days)
        
        slope = numerator / denominator if denominator > 0 else 0.0
        
        # Calculate R-squared
        ss_tot = sum((v - mean_value) ** 2 for v in values)
        ss_res = sum((v - (mean_value + slope * (t - mean_time))) ** 2 for t, v in zip(time_days, values))
        
        r_squared = 1 - (ss_res / ss_tot) if ss_tot > 0 else 0.0
        
        return slope, max(0.0, r_squared)  # Ensure R-squared is non-negative
    
    def _get_tracked_metrics(self) -> List[str]:
        """Get list of tracked metrics sorted by frequency."""
        with sqlite3.connect(self.db_path) as conn:
            cursor = conn.execute("""
                SELECT metric_name, COUNT(*) as count
                FROM time_series_metrics
                GROUP BY metric_name
                ORDER BY count DESC
            """)
            
            return [row[0] for row in cursor.fetchall()]
    
    def _generate_dashboard_html(self, 
                               alerts: List[RegressionAlert], 
                               trends: Dict[str, PerformanceTrend]) -> str:
        """Generate HTML dashboard content."""
        
        timestamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        
        html = f"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Regression Dashboard - TWG-TLDA</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }}
        .header {{ background: #2c3e50; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; }}
        .section {{ background: white; padding: 20px; margin: 20px 0; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .alert {{ padding: 15px; margin: 10px 0; border-left: 4px solid; border-radius: 4px; }}
        .alert.critical {{ background: #fdeaea; border-color: #e74c3c; }}
        .alert.major {{ background: #fdf2e5; border-color: #f39c12; }}
        .alert.moderate {{ background: #fff3cd; border-color: #ffc107; }}
        .alert.minor {{ background: #e8f5e8; border-color: #28a745; }}
        .trend {{ display: inline-block; margin: 10px; padding: 15px; background: #f8f9fa; border-radius: 6px; min-width: 200px; }}
        .trend.improving {{ border-left: 4px solid #28a745; }}
        .trend.stable {{ border-left: 4px solid #6c757d; }}
        .trend.degrading {{ border-left: 4px solid #dc3545; }}
        .trend.volatile {{ border-left: 4px solid #ffc107; }}
        .metric {{ font-weight: bold; color: #2c3e50; }}
        .value {{ font-size: 1.2em; margin: 5px 0; }}
        .change {{ font-size: 0.9em; }}
        .timestamp {{ color: #6c757d; font-size: 0.9em; }}
        table {{ width: 100%; border-collapse: collapse; }}
        th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }}
        th {{ background-color: #f8f9fa; font-weight: bold; }}
    </style>
</head>
<body>
    <div class="header">
        <h1>🧙‍♂️ TWG-TLDA Regression Dashboard</h1>
        <p>Performance monitoring and regression tracking for cognitive experiments</p>
        <p class="timestamp">Generated: {timestamp}</p>
    </div>

    <div class="section">
        <h2>🚨 Recent Alerts ({len(alerts)})</h2>
        """
        
        if alerts:
            html += """
        <table>
            <tr>
                <th>Severity</th>
                <th>Metric</th>
                <th>Current</th>
                <th>Baseline</th>
                <th>Change</th>
                <th>Time</th>
            </tr>
            """
            
            for alert in alerts:
                severity_class = alert.severity.value
                change_pct = alert.degradation_pct * 100
                alert_time = datetime.datetime.fromtimestamp(alert.timestamp).strftime("%H:%M")
                
                html += f"""
            <tr class="alert {severity_class}">
                <td>{alert.severity.value.upper()}</td>
                <td class="metric">{alert.metric_name}</td>
                <td>{alert.current_value:.3f}</td>
                <td>{alert.baseline_value:.3f}</td>
                <td>{change_pct:+.1f}%</td>
                <td>{alert_time}</td>
            </tr>
                """
            
            html += "</table>"
        else:
            html += '<p style="color: #28a745;">✅ No recent alerts - performance is stable!</p>'
        
        html += """
    </div>

    <div class="section">
        <h2>📈 Performance Trends</h2>
        <div style="display: flex; flex-wrap: wrap;">
        """
        
        for metric_name, trend in trends.items():
            trend_class = trend.direction.value
            slope_indicator = "↗️" if trend.slope > 0 else "↘️" if trend.slope < 0 else "➡️"
            
            html += f"""
        <div class="trend {trend_class}">
            <div class="metric">{metric_name}</div>
            <div class="value">{slope_indicator} {trend.direction.value.title()}</div>
            <div class="change">Recent: {trend.recent_mean:.3f}</div>
            <div class="change">Baseline: {trend.baseline_mean:.3f}</div>
            <div class="change">R²: {trend.r_squared:.3f}</div>
            <div class="change">{trend.data_points} data points</div>
        </div>
            """
        
        html += """
        </div>
    </div>

    <div class="section">
        <h2>📊 Dashboard Statistics</h2>
        <table>
            <tr>
                <th>Metric</th>
                <th>Value</th>
            </tr>
        """
        
        stats = {
            "Total Alerts": len(alerts),
            "Critical Alerts": len([a for a in alerts if a.severity == RegressionSeverity.CRITICAL]),
            "Metrics Tracked": len(trends),
            "Stable Trends": len([t for t in trends.values() if t.direction == TrendDirection.STABLE]),
            "Improving Trends": len([t for t in trends.values() if t.direction == TrendDirection.IMPROVING]),
            "Degrading Trends": len([t for t in trends.values() if t.direction == TrendDirection.DEGRADING])
        }
        
        for stat_name, stat_value in stats.items():
            html += f"""
            <tr>
                <td>{stat_name}</td>
                <td>{stat_value}</td>
            </tr>
            """
        
        html += """
        </table>
    </div>

    <footer style="text-align: center; margin-top: 40px; color: #6c757d;">
        <p>🧙‍♂️ "In the laboratory of minds, regression analysis is the microscope that reveals truth." - Bootstrap Sentinel</p>
    </footer>

</body>
</html>
        """
        
        return html


# CLI interface
def main():
    """CLI interface for regression tracker."""
    import argparse
    
    parser = argparse.ArgumentParser(description="Regression Tracker v0.7")
    parser.add_argument("command", choices=["baseline", "check", "trends", "dashboard", "alerts"])
    parser.add_argument("--metric", help="Specific metric name")
    parser.add_argument("--days", type=int, default=7, help="Number of days for analysis")
    parser.add_argument("--hours", type=int, default=1, help="Number of hours for recent analysis")
    parser.add_argument("--value", type=float, help="Baseline value to set")
    parser.add_argument("--output", default="experiments/regression_dashboard.html", help="Output file path")
    
    args = parser.parse_args()
    
    tracker = RegressionTracker()
    
    try:
        if args.command == "baseline":
            if args.metric and args.value is not None:
                tracker.set_baseline(args.metric, args.value)
                print(f"✅ Set baseline for {args.metric}: {args.value}")
            else:
                baselines = tracker.compute_baselines_from_recent_data(args.days)
                print(f"✅ Computed {len(baselines)} baselines from last {args.days} days:")
                for metric, value in baselines.items():
                    print(f"   {metric}: {value:.3f}")
        
        elif args.command == "check":
            alerts = tracker.check_for_regressions(args.hours)
            print(f"🔍 Found {len(alerts)} regressions in last {args.hours} hours:")
            for alert in alerts:
                print(f"   {alert.severity.value.upper()}: {alert.metric_name} "
                      f"({alert.degradation_pct*100:+.1f}%)")
        
        elif args.command == "trends":
            if args.metric:
                trend = tracker.analyze_trends(args.metric, args.days)
                if trend:
                    print(f"📈 Trend for {args.metric}:")
                    print(f"   Direction: {trend.direction.value}")
                    print(f"   Slope: {trend.slope:.6f}/day")
                    print(f"   R²: {trend.r_squared:.3f}")
                    print(f"   Recent mean: {trend.recent_mean:.3f}")
                    print(f"   Baseline mean: {trend.baseline_mean:.3f}")
                else:
                    print(f"No trend data available for {args.metric}")
            else:
                print("Error: --metric required for trends command")
        
        elif args.command == "dashboard":
            output_path = tracker.generate_regression_dashboard(args.output)
            print(f"📊 Dashboard generated: {output_path}")
        
        elif args.command == "alerts":
            alerts = tracker.get_recent_alerts(args.hours)
            print(f"🚨 {len(alerts)} recent alerts:")
            for alert in alerts:
                timestamp = datetime.datetime.fromtimestamp(alert.timestamp).strftime("%H:%M:%S")
                print(f"   [{timestamp}] {alert.severity.value.upper()}: {alert.metric_name} "
                      f"({alert.degradation_pct*100:+.1f}%)")
        
        return 0
        
    except Exception as e:
        print(f"Error: {e}")
        return 1


if __name__ == "__main__":
    exit(main())