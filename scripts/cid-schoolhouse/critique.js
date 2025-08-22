#!/usr/bin/env node
/**
 * CID Schoolhouse - Repository Critique
 * 
 * Analyzes repository context and generates findings, gaps, and proposals
 * with evidence-linked observations and actionable recommendations.
 */

const fs = require('fs');
const path = require('path');

class RepositoryCritic {
    constructor() {
        this.findings = [];
        this.gaps = [];
        this.proposals = [];
        this.badges = [];
    }

    analyzeContext(context) {
        console.log('🧠 CID Schoolhouse performing repository critique...');
        
        this.analyzeDocs(context);
        this.analyzePipelines(context);
        this.analyzeAutomation(context);
        this.analyzeSecurity(context);
        this.analyzeDeveloperExperience(context);
        this.determinebadges(context);
        
        return this.generateReport(context);
    }

    analyzeDocs(context) {
        const docs = context.docs;
        
        // Findings
        if (docs.tldlEntries > 10) {
            this.findings.push({
                category: 'documentation',
                title: 'Active TLDL Chronicle System',
                description: `Repository maintains ${docs.tldlEntries} TLDL entries with living documentation`,
                evidence: 'TLDL/entries/ directory',
                impact: 'high'
            });
        }

        if (docs.copilotInstructions) {
            this.findings.push({
                category: 'documentation', 
                title: 'GitHub Copilot Integration',
                description: 'Repository includes Copilot-specific instructions for enhanced AI assistance',
                evidence: '.github/copilot-instructions.md',
                impact: 'medium'
            });
        }

        // Gaps
        if (!docs.contributing) {
            this.gaps.push({
                category: 'documentation',
                title: 'Missing Contribution Guidelines', 
                description: 'No CONTRIBUTING.md found to guide new contributors',
                risk: 'medium',
                files: ['CONTRIBUTING.md']
            });
        }

        if (!docs.changelog) {
            this.gaps.push({
                category: 'documentation',
                title: 'Missing Changelog',
                description: 'No CHANGELOG.md found for release tracking',
                risk: 'low', 
                files: ['CHANGELOG.md']
            });
        }

        // Proposals
        if (docs.tldlEntries > 5 && !docs.manifestos) {
            this.proposals.push({
                category: 'documentation',
                title: 'Manifesto Creation Opportunity',
                description: 'With active TLDL system, create project manifesto to codify philosophy',
                effort: 'low',
                impact: 'medium',
                files: ['MANIFESTO.md']
            });
        }
    }

    analyzePipelines(context) {
        const pipelines = context.pipelines;
        
        // Findings
        if (pipelines.hasChronicleKeeper) {
            this.findings.push({
                category: 'automation',
                title: 'Chronicle Keeper Automation',
                description: 'Advanced TLDL automation system for preserving development lore',
                evidence: 'scripts/chronicle-keeper/',
                impact: 'high'
            });
        }

        if (pipelines.workflowCount > 2) {
            this.findings.push({
                category: 'ci_cd',
                title: 'Comprehensive CI/CD Pipeline',
                description: `Repository has ${pipelines.workflowCount} GitHub workflows for automation`,
                evidence: '.github/workflows/',
                impact: 'high'
            });
        }

        // Gaps
        if (!pipelines.hasValidation) {
            this.gaps.push({
                category: 'quality',
                title: 'Missing Validation Tools',
                description: 'No automated validation or linting detected',
                risk: 'medium',
                files: ['scripts/validate.sh']
            });
        }

        // Proposals for hardening
        this.proposals.push({
            category: 'ci_cd',
            title: 'Workflow Security Hardening',
            description: 'Pin action versions and add security scanning to existing workflows',
            effort: 'medium',
            impact: 'high',
            files: ['.github/workflows/*.yml']
        });
    }

    analyzeAutomation(context) {
        const signals = context.signals || {};
        const markers = signals.markers || {};
        
        // Look for automation opportunities
        if (markers.todo && markers.todo.length > 5) {
            this.proposals.push({
                category: 'automation',
                title: 'TODO Tracking Automation',
                description: `${markers.todo.length} TODO items could benefit from automated tracking`,
                effort: 'medium',
                impact: 'medium',
                files: ['scripts/todo-tracker.js']
            });
        }

        if (markers.ritual && markers.ritual.length > 0) {
            this.findings.push({
                category: 'process',
                title: 'Ritualized Development Process',
                description: `Found ${markers.ritual.length} RITUAL markers indicating process automation`,
                evidence: 'Search results for RITUAL keyword',
                impact: 'medium'
            });
        }
    }

    analyzeSecurity(context) {
        const configs = context.configs;
        
        // Check for security configurations
        if (configs.gitignore) {
            this.findings.push({
                category: 'security',
                title: 'Git Ignore Configuration',
                description: 'Repository has gitignore file for secret protection',
                evidence: '.gitignore',
                impact: 'medium'
            });
        }

        // Security proposals
        this.proposals.push({
            category: 'security',
            title: 'Dependency Security Scanning',
            description: 'Add dependabot and security workflow for dependency updates',
            effort: 'low',
            impact: 'high',
            files: ['.github/dependabot.yml', '.github/workflows/security.yml']
        });
    }

    analyzeDeveloperExperience(context) {
        const configs = context.configs;
        
        // Developer experience findings
        if (configs.editorConfig) {
            this.findings.push({
                category: 'developer_experience',
                title: 'Editor Configuration',
                description: 'Consistent editor settings across development environments',
                evidence: '.editorconfig',
                impact: 'low'
            });
        }

        if (configs.vscodeSetting) {
            this.findings.push({
                category: 'developer_experience',
                title: 'VS Code Integration',
                description: 'VS Code workspace settings for enhanced development experience',
                evidence: '.vscode/',
                impact: 'medium'
            });
        }

        // Developer experience proposals
        this.proposals.push({
            category: 'developer_experience',
            title: 'Development Environment Scripts',
            description: 'Create setup and run scripts for faster developer onboarding',
            effort: 'medium',
            impact: 'high',
            files: ['scripts/setup.sh', 'scripts/dev.sh']
        });
    }

    determinebadges(context) {
        const badges = [];
        
        // Determine CID badges based on findings
        if (context.pipelines.hasChronicleKeeper && context.docs.tldlEntries > 5) {
            badges.push('Lore-Stamped');
        }
        
        // Shield badge analysis - check for expected fail scenarios
        const shieldSignals = this.analyzeShieldSignals(context);
        if (shieldSignals.length > 0) {
            // Add specific shield badge based on the strongest signal
            const primaryShield = shieldSignals[0];
            badges.push(primaryShield.badgeType);
            
            // Add general keeper shield for multiple signals
            if (shieldSignals.length > 1 && primaryShield.badgeType !== "Keeper's Shield") {
                badges.push('Keeper\'s Shield');
            }
        } else if (context.pipelines.hasValidation) {
            badges.push('Buttsafe Certified');
        } else {
            badges.push('Guarded Fail');
        }
        
        badges.push('CID Studied');
        badges.push('Ritual Complete');
        
        this.badges = badges;
    }

    /**
     * Analyze repository context for shield indicator signals
     * @param {Object} context - Repository analysis context
     * @returns {Array} Shield signals with badge recommendations
     */
    analyzeShieldSignals(context) {
        const signals = [];
        
        // Check workflows for shield patterns
        if (context.pipelines && context.pipelines.workflows) {
            context.pipelines.workflows.forEach(workflow => {
                const workflowContent = workflow.content || '';
                const workflowName = workflow.name || '';
                
                // Look for shield-related keywords in workflow content
                if (workflowContent.match(/shield|guard|keeper|tripwire|defensive/i)) {
                    if (workflowContent.match(/bug.*honor|honor.*bug/i)) {
                        signals.push({ badgeType: 'Bug of Honor', confidence: 0.9 });
                    } else if (workflowContent.match(/buttsafe|cheek|preservation/i)) {
                        signals.push({ badgeType: 'Buttsafe Triggered', confidence: 0.8 });
                    } else if (workflowContent.match(/keeper|guardian/i)) {
                        signals.push({ badgeType: 'Keeper\'s Shield', confidence: 0.7 });
                    } else {
                        signals.push({ badgeType: 'Guarded Pass', confidence: 0.6 });
                    }
                }
                
                // Look for intentional fail patterns
                if (workflowContent.match(/continue-on-error:\s*false.*expected.*fail|intentional.*fail|pass.*by.*fail/i)) {
                    signals.push({ badgeType: 'Pass-by-Fail', confidence: 0.9 });
                }
            });
        }
        
        // Check documentation for shield concepts
        if (context.docs) {
            let docContent = '';
            if (context.docs.readme) docContent += context.docs.readme;
            if (context.docs.manifesto) docContent += context.docs.manifesto;
            if (context.docs.contributing) docContent += context.docs.contributing;
            
            if (docContent.match(/bug.*of.*honor|feature.*wearing.*bug|refactoring.*not.*failure/i)) {
                signals.push({ badgeType: 'Bug of Honor', confidence: 0.8 });
            }
            
            if (docContent.match(/cheek.*preservation|buttsafe|all.*hail.*cheeks/i)) {
                signals.push({ badgeType: 'Buttsafe Triggered', confidence: 0.7 });
            }
        }
        
        // Sort by confidence and return top signals
        return signals
            .sort((a, b) => b.confidence - a.confidence)
            .slice(0, 3); // Limit to top 3 shield signals
    }

    generateReport(context) {
        this.determinebadges(context);
        
        const report = {
            timestamp: new Date().toISOString(),
            summary: this.generateExecutiveSummary(),
            findings: this.findings,
            gaps: this.gaps,
            proposals: this.proposals.slice(0, 10), // Limit to top 10 proposals
            badges: this.badges,
            stats: {
                findings: this.findings.length,
                gaps: this.gaps.length,
                proposals: this.proposals.length
            }
        };

        console.log(`📊 Critique complete: ${report.stats.findings} findings, ${report.stats.gaps} gaps, ${report.stats.proposals} proposals`);
        return report;
    }

    generateExecutiveSummary() {
        const summary = [];
        
        if (this.findings.length > 0) {
            summary.push(`🎯 Repository shows strong development practices with ${this.findings.length} positive findings`);
        }
        
        if (this.gaps.length > 0) {
            summary.push(`⚠️ Identified ${this.gaps.length} areas for improvement`);
        }
        
        if (this.proposals.length > 0) {
            summary.push(`💡 Generated ${this.proposals.length} actionable enhancement proposals`);
        }
        
        const highImpactProposals = this.proposals.filter(p => p.impact === 'high').length;
        if (highImpactProposals > 0) {
            summary.push(`🚀 ${highImpactProposals} high-impact opportunities identified`);
        }
        
        summary.push(`🎓 CID Schoolhouse analysis complete - repository ready for enhancement`);
        
        return summary;
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const contextFile = args.find(arg => arg.startsWith('--context='))?.split('=')[1] || 'out/cid/context.json';
    const outputFile = args.find(arg => arg.startsWith('--out='))?.split('=')[1] || 'out/cid/report.json';
    
    try {
        const context = JSON.parse(fs.readFileSync(contextFile, 'utf8'));
        const critic = new RepositoryCritic();
        const report = critic.analyzeContext(context);
        
        // Ensure output directory exists
        const outputDir = path.dirname(outputFile);
        if (!fs.existsSync(outputDir)) {
            fs.mkdirSync(outputDir, { recursive: true });
        }
        
        fs.writeFileSync(outputFile, JSON.stringify(report, null, 2));
        console.log(`📝 Critique report saved to ${outputFile}`);
        
    } catch (error) {
        console.error('❌ Critique analysis failed:', error.message);
        process.exit(1);
    }
}

module.exports = RepositoryCritic;