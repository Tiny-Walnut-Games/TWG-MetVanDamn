#!/usr/bin/env node
/**
 * CID Faculty - Workflow Cooldown System
 * 
 * Prevents infinite loops by tracking recent workflow executions
 * and implementing cooldown periods for issue-based triggers.
 */

const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

class WorkflowCooldown {
    constructor(cooldownDir = 'out/cid/cooldown', defaultCooldownMs = 5 * 60 * 1000) {
        this.cooldownDir = cooldownDir;
        this.defaultCooldownMs = defaultCooldownMs; // 5 minutes default
        this.ensureCooldownDir();
        console.log(`🕐 Workflow cooldown initialized: ${cooldownDir} (${defaultCooldownMs}ms)`);
    }

    ensureCooldownDir() {
        if (!fs.existsSync(this.cooldownDir)) {
            fs.mkdirSync(this.cooldownDir, { recursive: true });
        }
    }

    /**
     * Generate cooldown key for workflow+issue combination
     */
    generateCooldownKey(workflowName, issueNumber, additionalContext = '') {
        const keyData = `${workflowName}:${issueNumber}:${additionalContext}`;
        return crypto.createHash('sha256').update(keyData).digest('hex').substring(0, 16);
    }

    /**
     * Check if workflow is in cooldown for a specific issue
     */
    isInCooldown(workflowName, issueNumber, additionalContext = '') {
        const cooldownKey = this.generateCooldownKey(workflowName, issueNumber, additionalContext);
        const cooldownFile = path.join(this.cooldownDir, `cooldown-${cooldownKey}.json`);
        
        if (!fs.existsSync(cooldownFile)) {
            console.log(`🕐 No cooldown active: ${workflowName} issue #${issueNumber}`);
            return false;
        }

        try {
            const cooldownData = JSON.parse(fs.readFileSync(cooldownFile, 'utf8'));
            const age = Date.now() - cooldownData.timestamp;
            const cooldownPeriod = cooldownData.cooldownMs || this.defaultCooldownMs;
            
            if (age < cooldownPeriod) {
                const remainingMs = cooldownPeriod - age;
                const remainingMin = Math.ceil(remainingMs / (60 * 1000));
                console.log(`🚫 Workflow ${workflowName} in cooldown for issue #${issueNumber} (${remainingMin}m remaining)`);
                return {
                    inCooldown: true,
                    remainingMs,
                    lastExecution: new Date(cooldownData.timestamp).toISOString(),
                    trigger: cooldownData.trigger,
                    executionCount: cooldownData.executionCount
                };
            } else {
                console.log(`🕐 Cooldown expired for ${workflowName} issue #${issueNumber}`);
                // Clean up expired cooldown
                try {
                    fs.unlinkSync(cooldownFile);
                } catch (error) {
                    // Ignore cleanup errors
                }
                return false;
            }
        } catch (error) {
            console.log(`🕐 Cooldown check error: ${error.message}`);
            return false;
        }
    }

    /**
     * Set cooldown for workflow+issue combination
     */
    setCooldown(workflowName, issueNumber, triggerInfo = {}, customCooldownMs = null) {
        const cooldownKey = this.generateCooldownKey(workflowName, issueNumber, triggerInfo.context || '');
        const cooldownFile = path.join(this.cooldownDir, `cooldown-${cooldownKey}.json`);
        
        // Check if there's an existing cooldown to track execution count
        let executionCount = 1;
        try {
            if (fs.existsSync(cooldownFile)) {
                const existing = JSON.parse(fs.readFileSync(cooldownFile, 'utf8'));
                executionCount = (existing.executionCount || 0) + 1;
            }
        } catch (error) {
            // Ignore errors, use default count
        }

        const cooldownData = {
            workflowName,
            issueNumber: parseInt(issueNumber),
            timestamp: Date.now(),
            cooldownMs: customCooldownMs || this.defaultCooldownMs,
            trigger: {
                type: triggerInfo.type || 'unknown',
                source: triggerInfo.source || 'unknown',
                pattern: triggerInfo.pattern || 'unknown'
            },
            executionCount,
            version: '1.0.0'
        };

        try {
            fs.writeFileSync(cooldownFile, JSON.stringify(cooldownData, null, 2));
            const cooldownMin = Math.ceil(cooldownData.cooldownMs / (60 * 1000));
            console.log(`🕐 Cooldown set: ${workflowName} issue #${issueNumber} for ${cooldownMin}m (execution #${executionCount})`);
            
            // If execution count is high, warn about potential issues
            if (executionCount > 3) {
                console.log(`⚠️ WARNING: High execution count (${executionCount}) detected for ${workflowName} on issue #${issueNumber}`);
                console.log(`⚠️ This may indicate a trigger loop that needs investigation`);
            }
        } catch (error) {
            console.log(`🕐 Cooldown set error: ${error.message}`);
        }
    }

    /**
     * Check if a specific trigger pattern would cause a loop
     */
    wouldTriggerLoop(workflowName, triggerPatterns, commentText) {
        const detectedPatterns = [];
        
        for (const pattern of triggerPatterns) {
            if (commentText.includes(pattern)) {
                detectedPatterns.push(pattern);
            }
        }
        
        if (detectedPatterns.length > 0) {
            console.log(`🔄 Loop risk detected in ${workflowName}: patterns ${detectedPatterns.join(', ')}`);
            return {
                wouldLoop: true,
                patterns: detectedPatterns,
                risk: detectedPatterns.length > 1 ? 'high' : 'medium'
            };
        }
        
        return { wouldLoop: false, patterns: [], risk: 'none' };
    }

    /**
     * Clean old cooldown files
     */
    cleanup(maxAgeHours = 24) {
        const maxAge = maxAgeHours * 60 * 60 * 1000;
        const now = Date.now();

        if (!fs.existsSync(this.cooldownDir)) {
            return;
        }

        const files = fs.readdirSync(this.cooldownDir);
        let cleaned = 0;

        files.forEach(file => {
            const filePath = path.join(this.cooldownDir, file);
            try {
                const stats = fs.statSync(filePath);
                if (now - stats.mtime.getTime() > maxAge) {
                    fs.unlinkSync(filePath);
                    cleaned++;
                }
            } catch (error) {
                // Ignore errors on cleanup
            }
        });

        if (cleaned > 0) {
            console.log(`🕐 Cleaned ${cleaned} old cooldown files`);
        }
    }

    /**
     * Get cooldown statistics
     */
    getStats() {
        if (!fs.existsSync(this.cooldownDir)) {
            return { files: 0, activeCooldowns: 0, totalExecutions: 0 };
        }

        const files = fs.readdirSync(this.cooldownDir);
        let activeCooldowns = 0;
        let totalExecutions = 0;
        const now = Date.now();

        files.forEach(file => {
            try {
                const filePath = path.join(this.cooldownDir, file);
                const cooldownData = JSON.parse(fs.readFileSync(filePath, 'utf8'));
                const age = now - cooldownData.timestamp;
                const cooldownPeriod = cooldownData.cooldownMs || this.defaultCooldownMs;
                
                if (age < cooldownPeriod) {
                    activeCooldowns++;
                }
                
                totalExecutions += cooldownData.executionCount || 1;
            } catch (error) {
                // Ignore errors
            }
        });

        return {
            files: files.length,
            activeCooldowns,
            totalExecutions,
            dir: this.cooldownDir
        };
    }

    /**
     * Get detailed cooldown status for debugging
     */
    getCooldownStatus(workflowName, issueNumber) {
        const cooldownKey = this.generateCooldownKey(workflowName, issueNumber);
        const cooldownFile = path.join(this.cooldownDir, `cooldown-${cooldownKey}.json`);
        
        if (!fs.existsSync(cooldownFile)) {
            return null;
        }

        try {
            const cooldownData = JSON.parse(fs.readFileSync(cooldownFile, 'utf8'));
            const age = Date.now() - cooldownData.timestamp;
            const cooldownPeriod = cooldownData.cooldownMs || this.defaultCooldownMs;
            
            return {
                ...cooldownData,
                age,
                remainingMs: Math.max(0, cooldownPeriod - age),
                isActive: age < cooldownPeriod
            };
        } catch (error) {
            return null;
        }
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const command = args[0] || 'help';
    
    if (command === 'help') {
        console.log(`
CID Faculty Workflow Cooldown Commands:
  stats                    - Show cooldown statistics
  status <workflow> <issue>- Check specific cooldown status
  cleanup                  - Clean old cooldown files
  test                     - Test cooldown functionality
        `);
    } else if (command === 'stats') {
        const cooldown = new WorkflowCooldown();
        const stats = cooldown.getStats();
        console.log(`Cooldowns: ${stats.activeCooldowns} active, ${stats.files} total files, ${stats.totalExecutions} total executions`);
    } else if (command === 'status') {
        const workflow = args[1];
        const issue = args[2];
        if (!workflow || !issue) {
            console.log('Usage: status <workflow> <issue>');
            process.exit(1);
        }
        const cooldown = new WorkflowCooldown();
        const status = cooldown.getCooldownStatus(workflow, issue);
        if (status) {
            console.log('Cooldown status:', JSON.stringify(status, null, 2));
        } else {
            console.log('No cooldown active');
        }
    } else if (command === 'cleanup') {
        const cooldown = new WorkflowCooldown();
        cooldown.cleanup();
    } else if (command === 'test') {
        const cooldown = new WorkflowCooldown();
        
        // Test setting and checking cooldown
        console.log('Testing cooldown mechanism...');
        
        const testWorkflow = 'test-workflow';
        const testIssue = '123';
        
        // Check initial state
        console.log('Initial state:', cooldown.isInCooldown(testWorkflow, testIssue));
        
        // Set cooldown
        cooldown.setCooldown(testWorkflow, testIssue, { type: 'test', pattern: 'test' }, 1000); // 1 second
        
        // Check cooldown is active
        console.log('After setting:', cooldown.isInCooldown(testWorkflow, testIssue));
        
        // Wait and check again
        setTimeout(() => {
            console.log('After delay:', cooldown.isInCooldown(testWorkflow, testIssue));
            console.log('Stats:', cooldown.getStats());
        }, 1100);
    }
}

module.exports = { WorkflowCooldown };