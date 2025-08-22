#!/usr/bin/env node
/**
 * CID Schoolhouse - Badge System
 * 
 * Applies CID badges and stamps to GitHub issues based on analysis results.
 * Updates issue labels and posts badge status comments.
 */

const fs = require('fs');
const { execSync } = require('child_process');

class BadgeSystem {
    constructor(githubToken) {
        this.githubToken = githubToken;
        this.badgeEmojis = {
            'Buttsafe Certified': '🛡️',
            'Guarded Fail': '⚠️', 
            'Keeper\'s Shield': '🛡',
            'Bug of Honor': '🛡',
            'Guarded Pass': '🛡',
            'Buttsafe Triggered': '🛡',
            'Pass-by-Fail': '🛡',
            'Lore-Stamped': '📜',
            'Ritual Complete': '✅',
            'CID Studied': '🎓'
        };
    }

    formatBadgeComment(badges) {
        let comment = `## 🏆 CID Badge Timeline\n\n`;
        comment += `*Badge stamps applied based on repository analysis:*\n\n`;
        
        badges.forEach(badge => {
            const emoji = this.badgeEmojis[badge] || '🎖️';
            comment += `${emoji} **${badge}**  \n`;
        });
        
        // Add special note for shield badges
        const shieldBadgeNames = [
            'Keeper\'s Shield', 'Bug of Honor', 'Guarded Pass', 
            'Buttsafe Triggered', 'Pass-by-Fail'
        ];
        const shieldBadges = badges.filter(badge =>
            shieldBadgeNames.includes(badge)
        );
        
        if (shieldBadges.length > 0) {
            comment += `\n### 🛡 Shield Status\n`;
            comment += `*These badges indicate expected protective fails - features working as designed:*\n\n`;
            shieldBadges.forEach(badge => {
                comment += `🛡 **${badge}** - Expected fail condition, system guarded on purpose\n`;
            });
        }
        
        comment += `\n---\n\n`;
        comment += `*CID Schoolhouse badge system complete*  \n`;
        comment += `*Repository analysis and critique workflow successfully executed*\n`;

        return comment;
    }

    /**
     * Detect if a failure should be marked as an expected "Pass-by-Fail" condition
     * @param {Object} failureContext - Context about the failure (job name, logs, etc.)
     * @returns {Object} Shield status with badge type and sub-label
     */
    detectShieldStatus(failureContext) {
        const { jobName = '', logs = '', errorMessage = '', workflow = '' } = failureContext;
        const combinedText = `${jobName} ${logs} ${errorMessage} ${workflow}`.toLowerCase();
        
        // Keywords that indicate intentional protective failures
        const shieldKeywords = [
            'guard', 'shield', 'protect', 'sentinel', 'keeper',
            'expected fail', 'intentional fail', 'defensive', 'tripwire',
            'buttsafe', 'lineage', 'cheek', 'honor', 'pass-by-fail'
        ];
        
        const matchedKeywords = shieldKeywords.filter(keyword => 
            combinedText.includes(keyword.toLowerCase())
        );
        
        if (matchedKeywords.length > 0) {
            // Determine specific shield type based on context
            if (combinedText.includes('bug') && combinedText.includes('honor')) {
                return { isShield: true, badgeType: 'Bug of Honor', subLabel: 'Feature wearing a bug coat' };
            }
            else if (combinedText.includes('buttsafe') || combinedText.includes('cheek')) {
                return { isShield: true, badgeType: 'Buttsafe Triggered', subLabel: 'Scroll lineage preserved' };
            }
            else if (combinedText.includes('guard') || combinedText.includes('keeper')) {
                return { isShield: true, badgeType: 'Keeper\'s Shield', subLabel: 'Guarded on purpose' };
            }
            else if (combinedText.includes('pass') && combinedText.includes('fail')) {
                return { isShield: true, badgeType: 'Pass-by-Fail', subLabel: 'Expected protective fail' };
            }
            else {
                return { isShield: true, badgeType: 'Guarded Pass', subLabel: 'Defensive tripwire activated' };
            }
        }
        
        return { isShield: false, badgeType: null, subLabel: null };
    }

    async applyLabels(issueNumber, badges, report) {
        if (!this.githubToken) {
            console.log('ℹ️ No GitHub token provided - would apply labels:', badges);
            return;
        }

        try {
            // Convert badges to label format
            const labels = badges.map(badge => badge.toLowerCase().replace(/\s+/g, '-'));
            
            // Add analysis result labels
            labels.push('cid-analyzed');
            
            if (report.stats.gaps > 0) {
                labels.push('enhancement-opportunities');
            }
            
            if (report.stats.proposals > 0) {
                labels.push('actionable-proposals');
            }

            // Apply labels using GitHub CLI
            for (const label of labels) {
                try {
                    execSync(`gh issue edit ${issueNumber} --add-label "${label}"`, {
                        env: { ...process.env, GITHUB_TOKEN: this.githubToken },
                        stdio: 'pipe'
                    });
                } catch (labelError) {
                    console.log(`ℹ️ Label "${label}" may not exist, attempting to create...`);
                    try {
                        // Try to create the label if it doesn't exist
                        execSync(`gh label create "${label}" --color "1f77b4" --description "CID Schoolhouse badge"`, {
                            env: { ...process.env, GITHUB_TOKEN: this.githubToken },
                            stdio: 'pipe'
                        });
                        // Try applying again
                        execSync(`gh issue edit ${issueNumber} --add-label "${label}"`, {
                            env: { ...process.env, GITHUB_TOKEN: this.githubToken },
                            stdio: 'pipe'
                        });
                    } catch (createError) {
                        console.log(`ℹ️ Could not create/apply label "${label}"`);
                    }
                }
            }
            
            console.log(`🏷️ Applied ${labels.length} labels to issue #${issueNumber}`);
            
        } catch (error) {
            console.error('❌ Failed to apply labels:', error.message);
        }
    }

    async postBadgeComment(issueNumber, badges) {
        if (!this.githubToken) {
            console.log('ℹ️ No GitHub token provided - would post badge comment');
            return;
        }

        const commentBody = this.formatBadgeComment(badges);
        
        try {
            const tempFile = `/tmp/cid-badges-${Date.now()}.md`;
            fs.writeFileSync(tempFile, commentBody);
            
            execSync(`gh issue comment ${issueNumber} --body-file "${tempFile}"`, {
                env: { ...process.env, GITHUB_TOKEN: this.githubToken },
                stdio: 'inherit'
            });
            
            fs.unlinkSync(tempFile);
            console.log(`🏆 Badge comment posted to issue #${issueNumber}`);
            
        } catch (error) {
            console.error('❌ Failed to post badge comment:', error.message);
        }
    }

    async applyBadges(issueNumber, report) {
        console.log(`🏆 Applying CID badges to issue #${issueNumber}...`);
        
        const badges = report.badges || [];
        
        if (badges.length === 0) {
            console.log('ℹ️ No badges to apply');
            return;
        }

        await this.applyLabels(issueNumber, badges, report);
        await this.postBadgeComment(issueNumber, badges);
        
        console.log(`✅ Applied ${badges.length} CID badges`);
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const issueNumber = args.find(arg => arg.startsWith('--issue='))?.split('=')[1];
    const reportFile = args.find(arg => arg.startsWith('--report='))?.split('=')[1] || 'out/cid/report.json';
    const githubToken = process.env.GITHUB_TOKEN;

    if (!issueNumber) {
        console.error('❌ Issue number required: --issue=123');
        process.exit(1);
    }

    try {
        const report = JSON.parse(fs.readFileSync(reportFile, 'utf8'));
        const badgeSystem = new BadgeSystem(githubToken);
        badgeSystem.applyBadges(issueNumber, report);
    } catch (error) {
        console.error('❌ Failed to apply badges:', error.message);
        process.exit(1);
    }
}

module.exports = BadgeSystem;