#!/usr/bin/env node
/**
 * CID Faculty - Badge System
 * 
 * Awards faculty-specific badges based on consultation results and usage metrics.
 * Extends existing CID badge system with faculty-specific achievements.
 */

const fs = require('fs');
const { execSync } = require('child_process');
const https = require('https');
const { URL } = require('url');

class FacultyBadgeSystem {
    constructor(githubToken) {
        this.githubToken = githubToken;
        this.facultyBadges = {
            // Efficiency badges
            'Budget-Wise': '🏆',
            'Cache-Master': '💾',
            'Speed-Runner': '⚡',
            
            // Quality badges
            'Wisdom-Bearer': '🧙‍♂️',
            'Vision-Keeper': '🔮',
            'Strategic-Thinker': '🎯',
            
            // Achievement badges
            'Faculty-Consulted': '🎓',
            'Dual-Faculty': '📜',
            'Evidence-Based': '🔍',
            'Future-Ready': '🌟'
        };
        
        this.badgeThresholds = {
            'Budget-Wise': (report) => this.isBudgetWise(report),
            'Cache-Master': (report) => this.hasCacheEfficiency(report),
            'Speed-Runner': (report) => this.isSpeedRunner(report),
            'Wisdom-Bearer': (advisorReport) => advisorReport && this.hasQualityAdvice(advisorReport),
            'Vision-Keeper': (oracleReport) => oracleReport && this.hasQualityVision(oracleReport),
            'Strategic-Thinker': (oracleReport) => oracleReport && this.isStrategicThinker(oracleReport),
            'Faculty-Consulted': (advisorReport, oracleReport) => !!(advisorReport || oracleReport),
            'Dual-Faculty': (advisorReport, oracleReport) => !!(advisorReport && oracleReport),
            'Evidence-Based': (advisorReport) => advisorReport && this.hasStrongEvidence(advisorReport),
            'Future-Ready': (oracleReport) => oracleReport && this.isFutureReady(oracleReport)
        };
    }

    evaluateFacultyBadges(advisorReport, oracleReport) {
        const earnedBadges = [];
        
        Object.entries(this.badgeThresholds).forEach(([badgeName, criteria]) => {
            if (criteria(advisorReport, oracleReport)) {
                earnedBadges.push(badgeName);
            }
        });
        
        console.log(`🏆 Badge evaluation complete - ${earnedBadges.length} badges earned`);
        return earnedBadges;
    }

    // Badge criteria functions
    isBudgetWise(report) {
        if (!report?.telemetry) return false;
        return report.telemetry.includes('Budget-Wise');
    }

    hasCacheEfficiency(report) {
        if (!report?.telemetry) return false;
        // Check for cache hit rate >= 50%
        const cacheMatch = report.telemetry.match(/Cache.*?(\d+\.\d+)%/);
        return cacheMatch && parseFloat(cacheMatch[1]) >= 50.0;
    }

    isSpeedRunner(report) {
        if (!report?.telemetry) return false;
        // Check for completion under 2 minutes
        const timeMatch = report.telemetry.match(/Time.*?(\d+\.\d+)m/);
        return timeMatch && parseFloat(timeMatch[1]) < 2.0;
    }

    hasQualityAdvice(advisorReport) {
        const items = advisorReport.actionItems || [];
        // Quality advice: 3+ items with evidence and varied categories
        const hasEnoughItems = items.length >= 3;
        const hasEvidence = items.every(item => item.evidence);
        const categories = new Set(items.map(item => item.category));
        return hasEnoughItems && hasEvidence && categories.size >= 2;
    }

    hasQualityVision(oracleReport) {
        const scenarios = oracleReport.scenarios || [];
        // Quality vision: 2+ scenarios with prerequisites and risks
        return scenarios.length >= 2 && 
               scenarios.every(s => s.prerequisites && s.risks && s.leadingIndicators);
    }

    isStrategicThinker(oracleReport) {
        const scenarios = oracleReport.scenarios || [];
        // Strategic thinking: includes transformation scenario or high complexity
        return scenarios.some(s => 
            s.name.toLowerCase().includes('transformation') ||
            (s.probability >= 0.6 && s.outcomes && s.outcomes.length >= 3)
        );
    }

    hasStrongEvidence(advisorReport) {
        const items = advisorReport.actionItems || [];
        // Strong evidence: all items have specific file/path references
        return items.length > 0 && items.every(item => 
            item.evidence && 
            (item.evidence.includes('/') || item.evidence.includes('.'))
        );
    }

    isFutureReady(oracleReport) {
        const scenarios = oracleReport.scenarios || [];
        // Future ready: scenarios cover different timeframes and probabilities
        const probabilities = scenarios.map(s => s.probability);
        const hasVariety = Math.max(...probabilities) - Math.min(...probabilities) >= 0.3;
        return scenarios.length >= 3 && hasVariety;
    }

    formatFacultyBadgeComment(badges, advisorReport, oracleReport) {
        let comment = `## 🏆 Faculty Badge Awards\n\n`;
        comment += `*Badge achievements based on consultation quality and efficiency:*\n\n`;
        
        // Group badges by category
        const efficiencyBadges = badges.filter(b => ['Budget-Wise', 'Cache-Master', 'Speed-Runner'].includes(b));
        const qualityBadges = badges.filter(b => ['Wisdom-Bearer', 'Vision-Keeper', 'Strategic-Thinker'].includes(b));
        const achievementBadges = badges.filter(b => ['Faculty-Consulted', 'Dual-Faculty', 'Evidence-Based', 'Future-Ready'].includes(b));
        
        if (efficiencyBadges.length > 0) {
            comment += `### ⚡ Efficiency Awards\n`;
            efficiencyBadges.forEach(badge => {
                const emoji = this.facultyBadges[badge];
                comment += `${emoji} **${badge}** - ${this.getBadgeDescription(badge)}\n`;
            });
            comment += `\n`;
        }
        
        if (qualityBadges.length > 0) {
            comment += `### 🎯 Quality Awards\n`;
            qualityBadges.forEach(badge => {
                const emoji = this.facultyBadges[badge];
                comment += `${emoji} **${badge}** - ${this.getBadgeDescription(badge)}\n`;
            });
            comment += `\n`;
        }
        
        if (achievementBadges.length > 0) {
            comment += `### 🎓 Achievement Awards\n`;
            achievementBadges.forEach(badge => {
                const emoji = this.facultyBadges[badge];
                comment += `${emoji} **${badge}** - ${this.getBadgeDescription(badge)}\n`;
            });
            comment += `\n`;
        }

        // Add consultation summary
        comment += `### 📊 Consultation Summary\n\n`;
        if (advisorReport) {
            comment += `**The Advisor**: ${advisorReport.actionItems?.length || 0} prioritized recommendations\n`;
        }
        if (oracleReport) {
            comment += `**The Oracle**: ${oracleReport.scenarios?.length || 0} strategic scenarios\n`;
        }
        
        comment += `**Total Badges Earned**: ${badges.length}\n\n`;
        
        comment += `---\n\n`;
        comment += `*CID Faculty badge system - recognizing consultation excellence*\n`;

        return comment;
    }

    getBadgeDescription(badgeName) {
        const descriptions = {
            'Budget-Wise': 'Completed consultation under 50% budget allocation',
            'Cache-Master': 'Achieved 50%+ cache efficiency for optimized processing',
            'Speed-Runner': 'Lightning-fast consultation completed under 2 minutes',
            'Wisdom-Bearer': 'Provided comprehensive, evidence-based guidance',
            'Vision-Keeper': 'Delivered detailed strategic scenarios with risk assessment',
            'Strategic-Thinker': 'Demonstrated advanced strategic planning capabilities',
            'Faculty-Consulted': 'Successfully completed faculty consultation',
            'Dual-Faculty': 'Integrated both Advisor and Oracle perspectives',
            'Evidence-Based': 'All recommendations backed by specific evidence',
            'Future-Ready': 'Comprehensive scenario planning with probability analysis'
        };
        
        return descriptions[badgeName] || 'Faculty achievement unlocked';
    }

    async applyFacultyLabels(issueNumber, badges, advisorReport, oracleReport) {
        if (!this.githubToken) {
            console.log('ℹ️ No GitHub token provided - would apply faculty labels:', badges);
            return;
        }

        try {
            // Convert badges to label format
            const labels = badges.map(badge => `faculty:${badge.toLowerCase().replace(/\s+/g, '-')}`);
            
            // Add faculty role labels
            if (advisorReport) labels.push('faculty:advisor');
            if (oracleReport) labels.push('faculty:oracle');
            
            // Add consultation status labels
            labels.push('faculty:complete');
            
            console.log(`🏷️ Applying faculty labels: ${labels.join(', ')}`);
            
            // Apply labels using GitHub API
            const response = await this.makeGitHubRequest(
                `https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/issues/${issueNumber}/labels`,
                'POST',
                { labels }
            );
            
            if (response.ok) {
                console.log('✅ Faculty labels applied successfully');
            } else {
                console.log('⚠️ Error applying faculty labels:', await response.text());
            }
        } catch (error) {
            console.error('❌ Faculty label error:', error.message);
        }
    }

    async postFacultyBadgeComment(issueNumber, badges, advisorReport, oracleReport) {
        if (!this.githubToken) {
            console.log('ℹ️ No GitHub token provided - would post faculty badge comment');
            return;
        }

        try {
            const comment = this.formatFacultyBadgeComment(badges, advisorReport, oracleReport);
            
            const response = await this.makeGitHubRequest(
                `https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/issues/${issueNumber}/comments`,
                'POST',
                { body: comment }
            );
            
            if (response.ok) {
                console.log('✅ Faculty badge comment posted successfully');
            } else {
                console.log('⚠️ Error posting faculty badge comment:', await response.text());
            }
        } catch (error) {
            console.error('❌ Faculty badge comment error:', error.message);
        }
    }

    async makeGitHubRequest(url, method, body = null) {
        const options = {
            method,
            headers: {
                'Authorization': `token ${this.githubToken}`,
                'Content-Type': 'application/json',
                'User-Agent': 'CID-Faculty-Badge-System/1.0'
            }
        };
        
        if (body) {
            options.body = JSON.stringify(body);
        }
        
        // Use native fetch if available (Node 18+), otherwise fallback to https
        if (typeof fetch !== 'undefined') {
            return fetch(url, options);
        } else {
            return this.httpsFallback(url, options);
        }
    }

    async httpsFallback(url, options) {
        return new Promise((resolve, reject) => {
            const parsedUrl = new URL(url);
            const requestOptions = {
                hostname: parsedUrl.hostname,
                port: parsedUrl.port || 443,
                path: parsedUrl.pathname + parsedUrl.search,
                method: options.method,
                headers: options.headers
            };

            const req = https.request(requestOptions, (res) => {
                let data = '';
                
                res.on('data', (chunk) => {
                    data += chunk;
                });
                
                res.on('end', () => {
                    // Create a fetch-like response object
                    const response = {
                        ok: res.statusCode >= 200 && res.statusCode < 300,
                        status: res.statusCode,
                        statusText: res.statusMessage,
                        headers: new Map(Object.entries(res.headers)),
                        text: async () => data,
                        json: async () => {
                            try {
                                return JSON.parse(data);
                            } catch (e) {
                                throw new Error('Invalid JSON response');
                            }
                        }
                    };
                    resolve(response);
                });
            });

            req.on('error', (error) => {
                reject(error);
            });

            if (options.body) {
                req.write(options.body);
            }
            
            req.end();
        });
    }

    // Generate badge statistics
    generateBadgeStats(badges) {
        const categories = {
            efficiency: badges.filter(b => ['Budget-Wise', 'Cache-Master', 'Speed-Runner'].includes(b)),
            quality: badges.filter(b => ['Wisdom-Bearer', 'Vision-Keeper', 'Strategic-Thinker'].includes(b)),
            achievement: badges.filter(b => ['Faculty-Consulted', 'Dual-Faculty', 'Evidence-Based', 'Future-Ready'].includes(b))
        };
        
        return {
            total: badges.length,
            categories,
            score: this.calculateBadgeScore(badges)
        };
    }

    calculateBadgeScore(badges) {
        const scores = {
            'Budget-Wise': 10,
            'Cache-Master': 15,
            'Speed-Runner': 20,
            'Wisdom-Bearer': 25,
            'Vision-Keeper': 25,
            'Strategic-Thinker': 30,
            'Faculty-Consulted': 5,
            'Dual-Faculty': 35,
            'Evidence-Based': 20,
            'Future-Ready': 30
        };
        
        return badges.reduce((total, badge) => total + (scores[badge] || 0), 0);
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const advisorFile = args.find(arg => arg.startsWith('--advisor='))?.split('=')[1];
    const oracleFile = args.find(arg => arg.startsWith('--oracle='))?.split('=')[1];
    const issueNumber = args.find(arg => arg.startsWith('--issue='))?.split('=')[1];
    const githubToken = process.env.GITHUB_TOKEN;
    
    async function main() {
        try {
            let advisorReport = null;
            let oracleReport = null;
            
            if (advisorFile && fs.existsSync(advisorFile)) {
                advisorReport = JSON.parse(fs.readFileSync(advisorFile, 'utf8'));
            }
            
            if (oracleFile && fs.existsSync(oracleFile)) {
                oracleReport = JSON.parse(fs.readFileSync(oracleFile, 'utf8'));
            }
            
            if (!advisorReport && !oracleReport) {
                console.error('❌ No faculty reports found');
                process.exit(1);
            }
            
            const badgeSystem = new FacultyBadgeSystem(githubToken);
            const badges = badgeSystem.evaluateFacultyBadges(advisorReport, oracleReport);
            const stats = badgeSystem.generateBadgeStats(badges);
            
            console.log(`🏆 Badge evaluation results:`);
            console.log(`   Total badges: ${stats.total}`);
            console.log(`   Badge score: ${stats.score}`);
            console.log(`   Badges earned: ${badges.join(', ')}`);
            
            if (issueNumber && githubToken) {
                await badgeSystem.applyFacultyLabels(issueNumber, badges, advisorReport, oracleReport);
                await badgeSystem.postFacultyBadgeComment(issueNumber, badges, advisorReport, oracleReport);
            }
            
        } catch (error) {
            console.error(`❌ Faculty badge system error:`, error.message);
            process.exit(1);
        }
    }
    
    main();
}

module.exports = { FacultyBadgeSystem };