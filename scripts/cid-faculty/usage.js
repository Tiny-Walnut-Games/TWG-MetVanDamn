#!/usr/bin/env node
/**
 * CID Faculty - Smart Usage Meter
 * 
 * Provides budget control, API call limits, caching, and resource management
 * for faculty roles to prevent burning through action minutes and quotas.
 */

const fs = require('fs');
const path = require('path');

class SmartUsageMeter {
    constructor(config = {}) {
        this.config = {
            maxRuntimeMinutes: config.maxRuntimeMinutes || 6,
            timeboxes: {
                advisor: { learn: 60, critique: 60 }, // seconds
                oracle: { forecast: 90 }              // seconds
            },
            callLimits: {
                github: config.githubApiLimit || 60,
                other: config.otherServiceLimit || 20
            },
            budgetSoftStop: config.budgetSoftStop || 0.8, // 80%
            escalationThreshold: config.escalationThreshold || 0.9, // 90%
            cacheThreshold: config.cacheThreshold || 0.05, // 5% change
            dryRun: config.dryRun || false,
            ...config
        };

        this.startTime = Date.now();
        this.usage = {
            calls: { github: 0, other: 0 },
            timeSpent: { advisor: 0, oracle: 0 },
            cacheHits: 0,
            cacheMisses: 0,
            earlyExits: 0
        };
        
        this.stopReasons = [];
        console.log(`📊 Smart Usage Meter initialized - Budget: ${this.config.maxRuntimeMinutes}m, Dry-run: ${this.config.dryRun}`);
    }

    /**
     * Check if we should proceed with an operation
     */
    shouldProceed(operation, estimatedTime = 0) {
        const elapsed = this.getElapsedMinutes();
        const remaining = this.config.maxRuntimeMinutes - elapsed;
        
        // Check time budget
        if (elapsed >= this.config.maxRuntimeMinutes) {
            this.stopReasons.push(`Time budget exceeded: ${elapsed.toFixed(2)}m/${this.config.maxRuntimeMinutes}m`);
            return false;
        }

        // Check soft stop threshold
        if (elapsed >= (this.config.maxRuntimeMinutes * this.config.budgetSoftStop)) {
            this.stopReasons.push(`Soft budget limit reached: ${elapsed.toFixed(2)}m (${(this.config.budgetSoftStop * 100).toFixed(0)}% of ${this.config.maxRuntimeMinutes}m)`);
            return false;
        }

        // Check API call limits
        if (this.usage.calls.github >= this.config.callLimits.github) {
            this.stopReasons.push(`GitHub API call limit reached: ${this.usage.calls.github}/${this.config.callLimits.github}`);
            return false;
        }

        if (this.usage.calls.other >= this.config.callLimits.other) {
            this.stopReasons.push(`Other service call limit reached: ${this.usage.calls.other}/${this.config.callLimits.other}`);
            return false;
        }

        return true;
    }

    /**
     * Track an API call
     */
    trackCall(service = 'other') {
        if (service === 'github') {
            this.usage.calls.github++;
        } else {
            this.usage.calls.other++;
        }
        
        console.log(`📞 API call tracked: ${service} (${this.usage.calls[service === 'github' ? 'github' : 'other']}/${this.config.callLimits[service === 'github' ? 'github' : 'other']})`);
    }

    /**
     * Start timing an operation
     */
    startTiming(role, operation) {
        const key = `${role}_${operation}`;
        this[`${key}_start`] = Date.now();
        console.log(`⏱️ Started timing: ${role} ${operation}`);
        return key;
    }

    /**
     * End timing an operation
     */
    endTiming(timingKey, role) {
        const startKey = `${timingKey}_start`;
        if (this[startKey]) {
            const duration = (Date.now() - this[startKey]) / 1000; // seconds
            this.usage.timeSpent[role] = (this.usage.timeSpent[role] || 0) + duration;
            delete this[startKey];
            console.log(`⏱️ Completed timing: ${timingKey} (${duration.toFixed(1)}s)`);
            return duration;
        }
        return 0;
    }

    /**
     * Check if change set is too small to warrant processing
     */
    shouldExitEarly(contextDiff) {
        if (!contextDiff || typeof contextDiff !== 'object') {
            return false;
        }

        const changeMetrics = this.calculateChangeSize(contextDiff);
        
        // If change is below threshold, exit early
        if (changeMetrics.changeRatio < this.config.cacheThreshold) {
            this.usage.earlyExits++;
            this.stopReasons.push(`Change set below threshold: ${(changeMetrics.changeRatio * 100).toFixed(2)}% < ${(this.config.cacheThreshold * 100).toFixed(2)}%`);
            return true;
        }

        return false;
    }

    /**
     * Calculate change size for diff-aware caching
     */
    calculateChangeSize(contextDiff) {
        // Simple heuristic based on diff structure
        let totalChanges = 0;
        let totalSize = 0;

        const countChanges = (obj) => {
            if (Array.isArray(obj)) {
                obj.forEach(countChanges);
                totalSize += obj.length;
            } else if (typeof obj === 'object' && obj !== null) {
                Object.keys(obj).forEach(key => {
                    if (key.includes('added') || key.includes('removed') || key.includes('modified')) {
                        totalChanges += Array.isArray(obj[key]) ? obj[key].length : 1;
                    }
                    countChanges(obj[key]);
                    totalSize++;
                });
            }
        };

        countChanges(contextDiff);
        
        return {
            totalChanges,
            totalSize,
            changeRatio: totalSize > 0 ? totalChanges / totalSize : 0
        };
    }

    /**
     * Track cache hit/miss
     */
    trackCacheResult(hit) {
        if (hit) {
            this.usage.cacheHits++;
            console.log(`💾 Cache hit (${this.usage.cacheHits} hits, ${this.usage.cacheMisses} misses)`);
        } else {
            this.usage.cacheMisses++;
            console.log(`💾 Cache miss (${this.usage.cacheHits} hits, ${this.usage.cacheMisses} misses)`);
        }
    }

    /**
     * Get elapsed time in minutes
     */
    getElapsedMinutes() {
        return (Date.now() - this.startTime) / (1000 * 60);
    }

    /**
     * Calculate cache efficiency as a percentage
     */
    getCacheEfficiency() {
        const total = this.usage.cacheHits + this.usage.cacheMisses;
        return total > 0 ? (this.usage.cacheHits / total) * 100 : 0;
    }

    /**
     * Generate usage telemetry report
     */
    generateTelemetryReport() {
        const elapsed = this.getElapsedMinutes();
        const budgetUsed = (elapsed / this.config.maxRuntimeMinutes) * 100;
        const cacheEfficiency = this.getCacheEfficiency();

        return {
            elapsed: elapsed.toFixed(2),
            budgetUsed: budgetUsed.toFixed(1),
            budgetRemaining: (100 - budgetUsed).toFixed(1),
            calls: this.usage.calls,
            timeSpent: this.usage.timeSpent,
            cacheStats: {
                hits: this.usage.cacheHits,
                misses: this.usage.cacheMisses,
                efficiency: cacheEfficiency.toFixed(1)
            },
            earlyExits: this.usage.earlyExits,
            stopReasons: this.stopReasons,
            isBudgetWise: budgetUsed < 50, // Under 50% qualifies for Budget-Wise badge
            dryRun: this.config.dryRun
        };
    }

    /**
     * Generate telemetry footer for output
     */
    generateTelemetryFooter() {
        const report = this.generateTelemetryReport();
        
        let footer = `\n\n---\n\n## 📊 Usage Telemetry\n\n`;
        footer += `- **Time**: ${report.elapsed}m used (${report.budgetUsed}% of ${this.config.maxRuntimeMinutes}m budget)\n`;
        footer += `- **API Calls**: GitHub ${report.calls.github}/${this.config.callLimits.github}, Other ${report.calls.other}/${this.config.callLimits.other}\n`;
        footer += `- **Cache**: ${report.cacheStats.efficiency}% hit rate (${report.cacheStats.hits} hits, ${report.cacheStats.misses} misses)\n`;
        
        if (report.earlyExits > 0) {
            footer += `- **Early Exits**: ${report.earlyExits} (small change detection)\n`;
        }
        
        if (report.stopReasons.length > 0) {
            footer += `- **Stop Reasons**: ${report.stopReasons.join('; ')}\n`;
        }

        if (report.isBudgetWise) {
            footer += `- **🏆 Budget-Wise**: Completed under 50% budget usage\n`;
        }

        if (report.dryRun) {
            footer += `- **Mode**: Dry-run (summaries only)\n`;
        }

        footer += `\n*Generated by Smart Usage Meter*`;
        
        return footer;
    }

    /**
     * Check if escalation is needed (requires faculty:proceed label)
     */
    needsEscalation() {
        const elapsed = this.getElapsedMinutes();
        return elapsed >= (this.config.maxRuntimeMinutes * this.config.escalationThreshold);
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const command = args[0] || 'help';
    
    if (command === 'help') {
        console.log(`
CID Faculty Usage Meter Commands:
  test-budget    - Test budget and timing functionality
  test-cache     - Test cache hit/miss tracking
  test-early     - Test early exit detection
  report         - Generate sample telemetry report
        `);
    } else if (command === 'test-budget') {
        const meter = new SmartUsageMeter({ maxRuntimeMinutes: 0.1, dryRun: true });
        console.log('Testing budget controls...');
        
        console.log('Should proceed initially:', meter.shouldProceed('test'));
        
        // Simulate time passing
        setTimeout(() => {
            console.log('After timeout, should proceed:', meter.shouldProceed('test'));
            console.log('Telemetry:', JSON.stringify(meter.generateTelemetryReport(), null, 2));
        }, 100);
    } else if (command === 'test-cache') {
        const meter = new SmartUsageMeter();
        meter.trackCacheResult(true);
        meter.trackCacheResult(false);
        meter.trackCacheResult(true);
        console.log('Final report:', JSON.stringify(meter.generateTelemetryReport(), null, 2));
    } else if (command === 'test-early') {
        const meter = new SmartUsageMeter();
        const smallDiff = { modified: [], added: [], removed: [] };
        const largeDiff = { modified: ['file1', 'file2', 'file3'], added: ['file4'] };
        
        console.log('Small diff early exit:', meter.shouldExitEarly(smallDiff));
        console.log('Large diff early exit:', meter.shouldExitEarly(largeDiff));
    } else if (command === 'report') {
        const meter = new SmartUsageMeter({ dryRun: true });
        meter.trackCall('github');
        meter.trackCall('other');
        meter.trackCacheResult(true);
        console.log(meter.generateTelemetryFooter());
    }
}

module.exports = { SmartUsageMeter };