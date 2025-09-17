#!/usr/bin/env node
/**
 * CID Faculty - Comment Trigger Handler
 * 
 * Handles GitHub issue comment triggers for faculty consultation
 * with cooldown protection to prevent infinite loops
 */

const { CIDFaculty } = require('./index.js');
const { WorkflowCooldown } = require('./shared/workflow-cooldown.js');

async function handleCommentTrigger() {
    const args = process.argv.slice(2);
    const comment = args.find(arg => arg.startsWith('--comment='))?.split('=')[1] || '';
    const issueNumber = args.find(arg => arg.startsWith('--issue='))?.split('=')[1] || '0';
    
    console.log('🎓📜 Processing faculty comment trigger...');
    
    // Initialize cooldown system
    const cooldown = new WorkflowCooldown();
    const workflowName = 'cid-faculty';
    
    // Define trigger patterns that could cause loops
    const loopTriggerPatterns = ['TLDL:', '📊 budget-check', '🔮', '🎓', '📜', '👨‍🏫'];
    
    // Check if we're in a cooldown period
    const cooldownCheck = cooldown.isInCooldown(workflowName, issueNumber);
    if (cooldownCheck && cooldownCheck.inCooldown) {
        console.log('🚫 Faculty consultation skipped due to cooldown protection');
        console.log(`⏰ Cooldown expires in ${Math.ceil(cooldownCheck.remainingMs / (60 * 1000))} minutes`);
        console.log(`📊 Execution count: ${cooldownCheck.executionCount}`);
        console.log(`🎯 Last trigger: ${cooldownCheck.trigger.pattern} (${cooldownCheck.trigger.type})`);
        
        // Exit with special code to indicate cooldown skip
        process.exit(42);
    }
    
    // Check if the current comment would create a loop
    const loopRisk = cooldown.wouldTriggerLoop(workflowName, loopTriggerPatterns, comment);
    if (loopRisk.wouldLoop) {
        console.log(`⚠️ Loop risk detected (${loopRisk.risk}): ${loopRisk.patterns.join(', ')}`);
        
        // Set a shorter cooldown for high-risk patterns
        const riskCooldownMs = loopRisk.risk === 'high' ? 10 * 60 * 1000 : 5 * 60 * 1000; // 10min vs 5min
        cooldown.setCooldown(workflowName, issueNumber, {
            type: 'loop-prevention',
            source: 'comment-trigger',
            pattern: loopRisk.patterns.join(',')
        }, riskCooldownMs);
    }

    let config = {
        issueNumber: parseInt(issueNumber),
        budgetMinutes: 6,
        dryRun: false,
        roles: ['advisor', 'oracle']
    };
    
    // Track what triggered this execution
    let triggerInfo = {
        type: 'unknown',
        source: 'comment-trigger',
        pattern: 'unknown'
    };
    
    // Parse comment for specific triggers
    if (comment.includes('📊 budget-check')) {
        console.log('📊 Faculty Budget Status Check');
        console.log('✅ Faculty Available - Ready for consultation');
        console.log('- Standard Budget: 6 minutes (default)');
        console.log('- Quick Analysis: 3 minutes (add --quick flag)');
        console.log('- Deep Analysis: 12 minutes (requires faculty:proceed label)');
        console.log('');
        console.log('🎯 Request consultation by adding cid:faculty label');
        
        triggerInfo = { type: 'budget-check', source: 'comment-trigger', pattern: '📊 budget-check' };
        // Set cooldown for budget check (shorter since it's informational)
        cooldown.setCooldown(workflowName, issueNumber, triggerInfo, 2 * 60 * 1000); // 2 minutes
        return;
    }
    
    if (comment.includes('TLDL:')) {
        // Advisor-focused consultation for TLDL insights
        config.roles = ['advisor'];
        config.budgetMinutes = 3;
        console.log('👨‍🏫 Advisor consultation requested for TLDL insight');
        triggerInfo = { type: 'tldl-consultation', source: 'comment-trigger', pattern: 'TLDL:' };
    }
    
    if (comment.includes('🔮')) {
        // Oracle-focused consultation
        config.roles = ['oracle'];
        console.log('🔮 Oracle consultation requested');
        triggerInfo = { type: 'oracle-consultation', source: 'comment-trigger', pattern: '🔮' };
    }
    
    // Execute faculty consultation
    try {
        // Set cooldown before execution to prevent rapid re-triggers
        cooldown.setCooldown(workflowName, issueNumber, triggerInfo);
        
        const faculty = new CIDFaculty(config);
        const report = await faculty.consult();
        
        console.log('\n📊 Consultation Summary:');
        report.summary.forEach(line => console.log(`   ${line}`));
        
        // Output structured data for GitHub Actions
        if (process.env.GITHUB_ACTIONS) {
            console.log('\n📄 GitHub Actions Output:');
            console.log(`::set-output name=consultation_completed::true`);
            console.log(`::set-output name=budget_used::${report.facultyConsultation.budget.used}`);
            console.log(`::set-output name=efficiency::${report.facultyConsultation.budget.efficiency}`);
            console.log(`::set-output name=cooldown_active::true`);
            console.log(`::set-output name=trigger_type::${triggerInfo.type}`);
        }
        
    } catch (error) {
        console.error(`❌ Faculty consultation error: ${error.message}`);
        // Still set cooldown even on error to prevent error loops
        cooldown.setCooldown(workflowName, issueNumber, {
            ...triggerInfo,
            type: `${triggerInfo.type}-error`
        }, 10 * 60 * 1000); // 10 minute cooldown for errors
        process.exit(1);
    }
}

if (require.main === module) {
    handleCommentTrigger();
}

module.exports = { handleCommentTrigger };