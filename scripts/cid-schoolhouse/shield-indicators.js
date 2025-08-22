#!/usr/bin/env node
/**
 * Pass-by-Fail Shield Indicator System
 * 
 * Detects expected failures in CI workflows and marks them with shield indicators (🛡)
 * while preserving the actual fail status for CI logic.
 * 
 * Integrates with the existing badge system and GitHub Actions workflow reporting.
 */

const fs = require('fs');
const { execSync } = require('child_process');
const BadgeSystem = require('./badges.js');

class ShieldIndicatorSystem {
    constructor(githubToken) {
        this.githubToken = githubToken;
        this.badgeSystem = new BadgeSystem(githubToken);
    }

    /**
     * Analyze a workflow run and determine if failures should have shield indicators
     * @param {Object} workflowContext - GitHub workflow context
     * @returns {Object} Analysis results with shield recommendations
     */
    analyzeWorkflowForShields(workflowContext) {
        const { 
            workflow_name = '', 
            job_name = '', 
            step_name = '', 
            conclusion = '',
            logs = '',
            repository = '',
            actor = ''
        } = workflowContext;

        const analysis = {
            hasShields: false,
            shields: [],
            recommendations: [],
            badges: []
        };

        // If the conclusion is not a failure, no shield needed
        if (conclusion !== 'failure') {
            return analysis;
        }

        // Check if this is an expected failure that should have a shield
        const failureContext = {
            jobName: job_name,
            stepName: step_name,
            logs: logs,
            errorMessage: '',
            workflow: workflow_name
        };

        const shieldStatus = this.badgeSystem.detectShieldStatus(failureContext);
        
        if (shieldStatus.isShield) {
            analysis.hasShields = true;
            analysis.shields.push({
                type: shieldStatus.badgeType,
                subLabel: shieldStatus.subLabel,
                job: job_name,
                step: step_name,
                emoji: '🛡'
            });
            analysis.badges.push(shieldStatus.badgeType);
            
            analysis.recommendations.push(
                `Mark ${job_name} with ${shieldStatus.badgeType} shield indicator`
            );
        }

        return analysis;
    }

    /**
     * Generate workflow summary with shield indicators
     * @param {Object} analysis - Shield analysis results
     * @returns {string} Markdown summary for workflow
     */
    generateShieldSummary(analysis) {
        if (!analysis.hasShields) {
            return '';
        }

        let summary = `## 🛡 Pass-by-Fail Shield Status\n\n`;
        summary += `*Expected protective fails detected - features working as designed:*\n\n`;
        
        analysis.shields.forEach(shield => {
            summary += `${shield.emoji} **${shield.type}**: ${shield.subLabel}\n`;
            summary += `   └── Job: \`${shield.job}\`${shield.step ? `, Step: \`${shield.step}\`` : ''}\n\n`;
        });

        summary += `### 🧙‍♂️ Shield Lore\n`;
        summary += `*"Refactoring is not a sign of failure; it's a sign of growth. Like molting, but for code."*\n`;
        summary += `— **Code Evolution Theory, Vol. III**\n\n`;
        summary += `These failures are **expected and desired** outcomes that guard scroll lineage `;
        summary += `and trigger protective tripwires. The underlying status remains "fail" for CI logic, `;
        summary += `but the shield appearance signals "guarded on purpose" to contributors.\n\n`;

        return summary;
    }

    /**
     * Post shield status to GitHub issue or PR
     * @param {number} issueNumber - GitHub issue/PR number
     * @param {Object} analysis - Shield analysis results
     */
    postShieldStatus(issueNumber, analysis) {
        if (!analysis.hasShields) {
            console.log('ℹ️ No shields detected, skipping shield status post');
            return Promise.resolve();
        }

        const shieldSummary = this.generateShieldSummary(analysis);
        
        if (!this.githubToken) {
            console.log('ℹ️ No GitHub token provided - would post shield status:');
            console.log(shieldSummary);
            return Promise.resolve();
        }

        try {
            const tempFile = `/tmp/shield-status-${Date.now()}.md`;
            fs.writeFileSync(tempFile, shieldSummary);
            
            execSync(`gh issue comment ${issueNumber} --body-file "${tempFile}"`, {
                env: { ...process.env, GITHUB_TOKEN: this.githubToken },
                stdio: 'inherit'
            });
            
            fs.unlinkSync(tempFile);
            console.log(`🛡 Shield status posted to issue #${issueNumber}`);
            return Promise.resolve();
            
        } catch (error) {
            console.error('❌ Failed to post shield status:', error.message);
            return Promise.reject(error);
        }
    }

    /**
     * Apply shield badges to issue
     * @param {number} issueNumber - GitHub issue/PR number  
     * @param {Object} analysis - Shield analysis results
     */
    applyShieldBadges(issueNumber, analysis) {
        if (!analysis.hasShields || analysis.badges.length === 0) {
            console.log('ℹ️ No shield badges to apply');
            return Promise.resolve();
        }

        // Create a report compatible with the existing badge system
        const report = {
            badges: analysis.badges,
            stats: {
                gaps: 0,
                proposals: 0
            },
            shieldAnalysis: analysis
        };

        return this.badgeSystem.applyBadges(issueNumber, report)
            .then(() => {
                console.log(`🛡 Applied ${analysis.badges.length} shield badges to issue #${issueNumber}`);
            });
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const issueNumber = args.find(arg => arg.startsWith('--issue='))?.split('=')[1];
    const workflowFile = args.find(arg => arg.startsWith('--workflow-context='))?.split('=')[1] || 'workflow-context.json';
    const githubToken = process.env.GITHUB_TOKEN;

    if (!issueNumber) {
        console.error('❌ Issue number required: --issue=123');
        process.exit(1);
    }

    try {
        // Read workflow context from file or environment
        let workflowContext = {};
        if (fs.existsSync(workflowFile)) {
            workflowContext = JSON.parse(fs.readFileSync(workflowFile, 'utf8'));
        } else {
            // Fallback to environment variables
            workflowContext = {
                workflow_name: process.env.GITHUB_WORKFLOW || '',
                job_name: process.env.GITHUB_JOB || '',
                step_name: process.env.GITHUB_STEP || '',
                conclusion: process.env.JOB_CONCLUSION || '',
                logs: process.env.JOB_LOGS || '',
                repository: process.env.GITHUB_REPOSITORY || '',
                actor: process.env.GITHUB_ACTOR || ''
            };
        }

        const shieldSystem = new ShieldIndicatorSystem(githubToken);
        const analysis = shieldSystem.analyzeWorkflowForShields(workflowContext);
        
        if (analysis.hasShields) {
            console.log(`🛡 Shield indicators detected: ${analysis.shields.length} shields found`);
            
            // Use promises instead of async/await to avoid top-level await
            shieldSystem.postShieldStatus(issueNumber, analysis)
                .then(() => shieldSystem.applyShieldBadges(issueNumber, analysis))
                .then(() => {
                    console.log('✅ Shield processing complete');
                })
                .catch((error) => {
                    console.error('❌ Failed to process shield indicators:', error.message);
                    process.exit(1);
                });
        } else {
            console.log('ℹ️ No shield indicators needed for this workflow');
        }
        
    } catch (error) {
        console.error('❌ Failed to process shield indicators:', error.message);
        process.exit(1);
    }
}

module.exports = ShieldIndicatorSystem;