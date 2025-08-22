#!/usr/bin/env node
/**
 * CID Faculty - System Pattern Detector
 * 
 * Automatically detects patterns that warrant Oracle consultation.
 * Monitors repeated failures, suspicious patterns, and system anomalies.
 */

const fs = require('fs');
const path = require('path');
const { VisionQueue } = require('./vision-queue.js');

class SystemPatternDetector {
    constructor(config = {}) {
        this.config = {
            failureThreshold: config.failureThreshold || 3, // Repeated failures
            timeWindow: config.timeWindow || 24 * 60 * 60 * 1000, // 24 hours
            ...config
        };
        
        this.visionQueue = new VisionQueue();
        this.patterns = {
            repeatedFailures: new Map(),
            systemAnomalies: []
        };
        
        console.log(`🔍 System Pattern Detector initialized`);
    }

    /**
     * Monitor for repeated build/test failures on same subsystem
     */
    detectRepeatedFailures(buildLog, subsystem) {
        const now = Date.now();
        const key = subsystem || 'unknown';
        
        if (!this.patterns.repeatedFailures.has(key)) {
            this.patterns.repeatedFailures.set(key, []);
        }
        
        const failures = this.patterns.repeatedFailures.get(key);
        failures.push({ timestamp: now, log: buildLog });
        
        // Clean old failures outside time window
        const cutoff = now - this.config.timeWindow;
        const recentFailures = failures.filter(f => f.timestamp > cutoff);
        this.patterns.repeatedFailures.set(key, recentFailures);
        
        // Check threshold
        if (recentFailures.length >= this.config.failureThreshold) {
            this.queueSystemPattern({
                type: 'repeated_failures',
                subsystem: key,
                count: recentFailures.length,
                source: `build/test failures in ${key}`,
                description: `${recentFailures.length} repeated failures in ${key} subsystem within ${this.config.timeWindow / (60 * 60 * 1000)} hours`,
                severity: 'high',
                evidence: recentFailures.map(f => f.log).join('\n---\n')
            });
            
            return true;
        }
        
        return false;
    }

    /**
     * Detect system anomalies from metrics or logs
     */
    detectSystemAnomaly(anomalyData) {
        const pattern = {
            type: 'system_anomaly',
            source: anomalyData.source || 'system monitoring',
            description: anomalyData.description || 'Unspecified system anomaly detected',
            severity: anomalyData.severity || 'medium',
            evidence: anomalyData.evidence || '',
            metrics: anomalyData.metrics || {}
        };
        
        this.queueSystemPattern(pattern);
        return pattern;
    }

    /**
     * Detect performance degradation patterns
     */
    detectPerformanceDegradation(metrics) {
        const thresholds = {
            buildTime: 300, // 5 minutes
            testTime: 180,  // 3 minutes
            deployTime: 600 // 10 minutes
        };
        
        const degradations = [];
        
        Object.entries(thresholds).forEach(([metric, threshold]) => {
            if (metrics[metric] && metrics[metric] > threshold) {
                degradations.push({
                    metric,
                    value: metrics[metric],
                    threshold,
                    severity: metrics[metric] > threshold * 2 ? 'high' : 'medium'
                });
            }
        });
        
        if (degradations.length > 0) {
            const pattern = {
                type: 'performance_degradation',
                source: 'performance monitoring',
                description: `Performance degradation detected in ${degradations.length} metrics`,
                severity: degradations.some(d => d.severity === 'high') ? 'high' : 'medium',
                evidence: JSON.stringify(degradations, null, 2),
                metrics: metrics
            };
            
            this.queueSystemPattern(pattern);
            return pattern;
        }
        
        return null;
    }

    /**
     * Detect dependency update conflicts
     */
    detectDependencyConflicts(packageData) {
        const conflicts = [];
        
        // Check for version conflicts
        if (packageData.conflicts && packageData.conflicts.length > 0) {
            conflicts.push(...packageData.conflicts);
        }
        
        // Check for security vulnerabilities
        if (packageData.vulnerabilities && packageData.vulnerabilities.length > 0) {
            conflicts.push(...packageData.vulnerabilities.map(v => ({
                type: 'security',
                package: v.package,
                severity: v.severity,
                description: v.title
            })));
        }
        
        if (conflicts.length > 0) {
            const pattern = {
                type: 'dependency_conflicts',
                source: 'dependency analysis',
                description: `${conflicts.length} dependency conflicts or vulnerabilities detected`,
                severity: conflicts.some(c => c.severity === 'high' || c.severity === 'critical') ? 'high' : 'medium',
                evidence: JSON.stringify(conflicts, null, 2),
                conflicts: conflicts
            };
            
            this.queueSystemPattern(pattern);
            return pattern;
        }
        
        return null;
    }

    /**
     * Queue a detected pattern for Oracle consultation
     */
    queueSystemPattern(pattern) {
        console.log(`🔍 Pattern detected: ${pattern.type} (${pattern.severity})`);
        
        const visionId = this.visionQueue.queueSystemPattern(pattern, {
            repository: process.cwd(),
            detectedAt: new Date().toISOString()
        });
        
        console.log(`🔮 Pattern queued for Oracle: ${visionId}`);
        return visionId;
    }

    /**
     * Get pattern detection summary
     */
    getPatternSummary() {
        const summary = {
            repeatedFailures: {},
            totalPatterns: this.patterns.systemAnomalies.length,
            queueStatus: this.visionQueue.getStatus()
        };
        
        // Summarize repeated failures
        this.patterns.repeatedFailures.forEach((failures, subsystem) => {
            summary.repeatedFailures[subsystem] = failures.length;
        });
        
        return summary;
    }

    /**
     * Scan build logs for patterns
     */
    scanBuildLogs(logDirectory) {
        if (!fs.existsSync(logDirectory)) {
            console.log(`📂 Log directory not found: ${logDirectory}`);
            return [];
        }
        
        const patterns = [];
        const logFiles = fs.readdirSync(logDirectory).filter(f => f.endsWith('.log'));
        
        logFiles.forEach(logFile => {
            const logPath = path.join(logDirectory, logFile);
            const logContent = fs.readFileSync(logPath, 'utf8');
            
            // Look for failure patterns
            if (logContent.includes('FAILED') || logContent.includes('ERROR')) {
                const subsystem = this.extractSubsystemFromLog(logContent);
                const detected = this.detectRepeatedFailures(logContent, subsystem);
                
                if (detected) {
                    patterns.push({
                        type: 'repeated_failures',
                        file: logFile,
                        subsystem: subsystem
                    });
                }
            }
        });
        
        return patterns;
    }

    extractSubsystemFromLog(logContent) {
        // Simple heuristic to extract subsystem name
        const lines = logContent.split('\n');
        
        for (const line of lines) {
            if (line.includes('FAILED') || line.includes('ERROR')) {
                // Look for patterns like "test/module-name" or "src/component"
                const match = line.match(/(?:test|src)\/([^\/\s]+)/);
                if (match) {
                    return match[1];
                }
            }
        }
        
        return 'unknown';
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const command = args[0] || 'scan';
    
    async function main() {
        try {
            const detector = new SystemPatternDetector();
            
            switch (command) {
                case 'scan':
                    const logDir = args[1] || 'logs';
                    const patterns = detector.scanBuildLogs(logDir);
                    
                    console.log(`\n🔍 Pattern Scan Results:`);
                    console.log(`   Scanned: ${logDir}`);
                    console.log(`   Patterns detected: ${patterns.length}`);
                    
                    patterns.forEach((pattern, i) => {
                        console.log(`   ${i + 1}. ${pattern.type} in ${pattern.subsystem} (${pattern.file})`);
                    });
                    break;
                    
                case 'test-failure':
                    const subsystem = args[1] || 'test-module';
                    const mockLog = `FAILED: ${subsystem}/test.spec.js\nError: Test timeout\n`;
                    
                    // Simulate multiple failures for testing
                    const iterations = parseInt(args[2]) || 1;
                    let triggered = false;
                    
                    for (let i = 0; i < iterations; i++) {
                        const detected = detector.detectRepeatedFailures(mockLog + ` (attempt ${i + 1})`, subsystem);
                        if (detected) {
                            triggered = true;
                            console.log(`🔍 Test failure #${i + 1} triggered pattern detection`);
                            break;
                        } else {
                            console.log(`🔍 Test failure #${i + 1} recorded`);
                        }
                    }
                    
                    if (!triggered && iterations > 1) {
                        console.log(`🔍 ${iterations} failures recorded, no pattern triggered`);
                    }
                    break;
                    
                case 'status':
                    const summary = detector.getPatternSummary();
                    console.log('\n🔍 Pattern Detection Status:');
                    console.log(JSON.stringify(summary, null, 2));
                    break;
                    
                default:
                    console.log('🔍 System Pattern Detector CLI');
                    console.log('Usage: node system-pattern-detector.js [command] [args...]');
                    console.log('');
                    console.log('Commands:');
                    console.log('  scan [dir]     Scan log directory for patterns');
                    console.log('  test-failure <module>  Simulate test failure');
                    console.log('  status         Show detection status');
            }
            
        } catch (error) {
            console.error(`❌ Pattern Detector error:`, error.message);
            process.exit(1);
        }
    }
    
    main();
}

module.exports = { SystemPatternDetector };