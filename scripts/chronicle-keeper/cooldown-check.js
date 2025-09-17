#!/usr/bin/env node
/**
 * Chronicle Keeper - Cooldown Check
 * 
 * Standalone cooldown checker for Chronicle Keeper workflow
 * Uses the same system as CID Faculty but with different defaults
 */

const { WorkflowCooldown } = require('../cid-faculty/shared/workflow-cooldown.js');

async function checkChronicleKeeperCooldown() {
    const args = process.argv.slice(2);
    const issueNumber = args.find(arg => arg.startsWith('--issue='))?.split('=')[1] || '0';
    const eventName = args.find(arg => arg.startsWith('--event='))?.split('=')[1] || 'unknown';
    const commentBody = args.find(arg => arg.startsWith('--comment='))?.split('=')[1] || '';
    
    console.log('📜 Checking Chronicle Keeper cooldown protection...');
    
    // Initialize cooldown system with longer default for chronicle keeper (3 minutes)
    const cooldown = new WorkflowCooldown('out/chronicle/cooldown', 3 * 60 * 1000);
    const workflowName = 'chronicle-keeper';
    
    // Define trigger patterns that could cause loops
    const loopTriggerPatterns = ['TLDL:', '📜', 'chronicle', 'lore', '🧠'];
    
    // Only check cooldown for comment events (not for issues, PRs, etc.)
    if (eventName === 'issue_comment') {
        // Check if we're in a cooldown period
        const cooldownCheck = cooldown.isInCooldown(workflowName, issueNumber);
        if (cooldownCheck && cooldownCheck.inCooldown) {
            console.log('🚫 Chronicle Keeper skipped due to cooldown protection');
            console.log(`⏰ Cooldown expires in ${Math.ceil(cooldownCheck.remainingMs / (60 * 1000))} minutes`);
            console.log(`📊 Execution count: ${cooldownCheck.executionCount}`);
            console.log(`🎯 Last trigger: ${cooldownCheck.trigger.pattern} (${cooldownCheck.trigger.type})`);
            
            // Exit with special code to indicate cooldown skip
            process.exit(42);
        }
        
        // Check if the current comment would create a loop
        const loopRisk = cooldown.wouldTriggerLoop(workflowName, loopTriggerPatterns, commentBody);
        if (loopRisk.wouldLoop) {
            console.log(`⚠️ Loop risk detected (${loopRisk.risk}): ${loopRisk.patterns.join(', ')}`);
            
            // Set cooldown for risky patterns
            const riskCooldownMs = loopRisk.risk === 'high' ? 5 * 60 * 1000 : 3 * 60 * 1000; // 5min vs 3min
            cooldown.setCooldown(workflowName, issueNumber, {
                type: 'loop-prevention',
                source: 'comment-trigger',
                pattern: loopRisk.patterns.join(',')
            }, riskCooldownMs);
        }
        
        // Set cooldown for this execution
        cooldown.setCooldown(workflowName, issueNumber, {
            type: 'comment-trigger',
            source: 'chronicle-keeper',
            pattern: loopRisk.patterns[0] || 'general'
        });
    } else {
        console.log(`✅ Chronicle Keeper proceeding for ${eventName} event (no cooldown needed)`);
    }
    
    console.log('✅ Chronicle Keeper cooldown check passed');
    process.exit(0);
}

if (require.main === module) {
    checkChronicleKeeperCooldown().catch(error => {
        console.error(`❌ Chronicle Keeper cooldown check error: ${error.message}`);
        process.exit(1);
    });
}

module.exports = { checkChronicleKeeperCooldown };