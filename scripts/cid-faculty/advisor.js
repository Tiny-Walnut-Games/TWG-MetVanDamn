#!/usr/bin/env node
/**
 * CID Faculty - The Advisor
 * 
 * Provides grounded guidance through current-state audit and prioritized next steps.
 * Delivers 3-7 actionable items with effort/impact tags and evidence links.
 */

const fs = require('fs');
const path = require('path');
const { SmartUsageMeter } = require('./usage.js');
const { ContextCache } = require('./shared/context-cache.js');
const { VisionQueue } = require('./vision-queue.js');

class Advisor {
    constructor(config = {}) {
        this.config = {
            maxItems: config.maxItems || 7,
            minItems: config.minItems || 3,
            dryRun: config.dryRun || false,
            oracleThreshold: config.oracleThreshold || 70, // Priority threshold for Oracle queuing
            ...config
        };
        
        this.meter = new SmartUsageMeter(config);
        this.cache = new ContextCache();
        this.wisdom = [];
        
        // Initialize Vision Queue for Oracle integration
        this.visionQueue = new VisionQueue();
        
        console.log(`🧙‍♂️ The Advisor has arrived - providing grounded guidance`);
    }

    /**
     * Analyze repository context and provide advisor guidance
     */
    async advise(context, syllabus = {}) {
        console.log('🔍 The Advisor begins current-state audit...');
        
        if (!this.meter.shouldProceed('advisor-analysis')) {
            return this.generateNullReport('Budget constraints prevent analysis');
        }

        const cacheKey = this.cache.generateCacheKey(context);
        const cachedResults = this.cache.getCachedFacultyResults(cacheKey, 'advisor');
        
        if (cachedResults) {
            this.meter.trackCacheResult(true);
            console.log('📋 Using cached advisor wisdom');
            return this.addTelemetryToReport(cachedResults);
        }
        
        this.meter.trackCacheResult(false);
        
        // Time the learning phase
        const learnTimer = this.meter.startTiming('advisor', 'learn');
        await this.auditCurrentState(context, syllabus);
        this.meter.endTiming(learnTimer, 'advisor');
        
        if (!this.meter.shouldProceed('advisor-critique')) {
            return this.generateNullReport('Time budget exhausted during audit');
        }
        
        // Time the critique phase
        const critiqueTimer = this.meter.startTiming('advisor', 'critique');
        this.prioritizeActionItems(context);
        this.meter.endTiming(critiqueTimer, 'advisor');
        
        const report = this.generateAdvisoryReport();
        
        // Cache the results
        this.cache.setCachedFacultyResults(cacheKey, 'advisor', report);
        
        return this.addTelemetryToReport(report);
    }

    /**
     * Audit current state of repository
     */
    async auditCurrentState(context, syllabus) {
        console.log('📋 Auditing current repository state...');
        
        // Audit documentation completeness
        this.auditDocumentation(context);
        
        // Audit CI/CD pipeline health
        this.auditPipelines(context);
        
        // Audit automation opportunities  
        this.auditAutomation(context);
        
        // Audit security posture
        this.auditSecurity(context);
        
        // Audit developer experience
        this.auditDeveloperExperience(context);
        
        // Audit technical debt signals
        this.auditTechnicalDebt(context);
        
        console.log(`📋 Audit complete - identified ${this.wisdom.length} potential action items`);
    }

    auditDocumentation(context) {
        const docs = context.docs || {};
        
        if (!docs.readme || !docs.readme.length) {
            this.wisdom.push({
                category: 'documentation',
                title: 'Missing or Empty README',
                description: 'Repository lacks comprehensive README documentation',
                evidence: 'README.md file missing or empty',
                impact: 'high',
                effort: 'low',
                priority: 95
            });
        }

        if (!docs.contributing) {
            this.wisdom.push({
                category: 'documentation',
                title: 'Add Contributing Guidelines',
                description: 'Establish clear contributor guidelines and workflow documentation',
                evidence: 'No CONTRIBUTING.md found',
                impact: 'medium',
                effort: 'medium',
                priority: 70
            });
        }

        if (docs.tldlEntries < 5) {
            this.wisdom.push({
                category: 'documentation',
                title: 'Expand Living Documentation',
                description: 'Increase TLDL entries to build institutional knowledge',
                evidence: `Only ${docs.tldlEntries || 0} TLDL entries found`,
                impact: 'medium',
                effort: 'low',
                priority: 65
            });
        }
    }

    auditPipelines(context) {
        const pipelines = context.pipelines || {};
        const workflows = context.topology?.workflows || [];
        
        if (workflows.length === 0) {
            this.wisdom.push({
                category: 'ci-cd',
                title: 'Establish CI/CD Pipeline',
                description: 'Set up automated testing and deployment workflows',
                evidence: 'No GitHub workflows found',
                impact: 'high',
                effort: 'high',
                priority: 85
            });
        }

        if (workflows.length > 0 && workflows.length < 3) {
            this.wisdom.push({
                category: 'ci-cd',
                title: 'Expand Pipeline Coverage',
                description: 'Add testing, security scanning, and deployment workflows',
                evidence: `Only ${workflows.length} workflow(s) found`,
                impact: 'medium',
                effort: 'medium',
                priority: 75
            });
        }
    }

    auditAutomation(context) {
        const signals = context.signals || {};
        
        if (signals.todos && signals.todos.length > 10) {
            this.wisdom.push({
                category: 'automation',
                title: 'Automate TODO Tracking',
                description: 'Implement automated TODO issue creation and tracking',
                evidence: `${signals.todos.length} TODO items found in code`,
                impact: 'medium',
                effort: 'medium',
                priority: 60
            });
        }

        if (!context.configs?.packageJson) {
            this.wisdom.push({
                category: 'automation',
                title: 'Add Package Management',
                description: 'Set up package.json for dependency and script management',
                evidence: 'No package.json found',
                impact: 'medium',
                effort: 'low',
                priority: 55
            });
        }
    }

    auditSecurity(context) {
        const configs = context.configs || {};
        
        if (!configs.gitignore) {
            this.wisdom.push({
                category: 'security',
                title: 'Add Git Ignore Configuration',
                description: 'Prevent sensitive files from being committed to repository',
                evidence: 'No .gitignore file found',
                impact: 'high',
                effort: 'low',
                priority: 90
            });
        }

        const workflows = context.topology?.workflows || [];
        const hasSecurityWorkflow = workflows.some(w => 
            w.includes('security') || w.includes('scan') || w.includes('audit')
        );

        if (!hasSecurityWorkflow) {
            this.wisdom.push({
                category: 'security',
                title: 'Implement Security Scanning',
                description: 'Add automated vulnerability scanning to CI pipeline',
                evidence: 'No security workflows detected',
                impact: 'high',
                effort: 'medium',
                priority: 80
            });
        }
    }

    auditDeveloperExperience(context) {
        const configs = context.configs || {};
        
        if (!configs.editorConfig) {
            this.wisdom.push({
                category: 'developer-experience',
                title: 'Add Editor Configuration',
                description: 'Standardize coding style across development environments',
                evidence: 'No .editorconfig file found',
                impact: 'low',
                effort: 'low',
                priority: 40
            });
        }

        if (!configs.vscodeSetting) {
            this.wisdom.push({
                category: 'developer-experience',
                title: 'Enhance IDE Integration',
                description: 'Add VS Code workspace settings for better development experience',
                evidence: 'No .vscode directory found',
                impact: 'low',
                effort: 'low',
                priority: 35
            });
        }
    }

    auditTechnicalDebt(context) {
        const signals = context.signals || {};
        const topology = context.topology || {};
        
        // Look for large files or directories that might indicate technical debt
        if (topology.totalLOC > this.config.locThreshold) {
            this.wisdom.push({
                category: 'technical-debt',
                title: 'Consider Code Organization Review',
                description: 'Large codebase may benefit from modularization analysis',
                evidence: `${topology.totalLOC} lines of code detected`,
                impact: 'medium',
                effort: 'high',
                priority: 50
            });
        }

        if (signals.fixmes && signals.fixmes.length > 5) {
            this.wisdom.push({
                category: 'technical-debt',
                title: 'Address FIXME Comments',
                description: 'Resolve outstanding technical debt markers in code',
                evidence: `${signals.fixmes.length} FIXME comments found`,
                impact: 'medium',
                effort: 'medium',
                priority: 45
            });
        }
    }

    /**
     * Prioritize action items based on impact, effort, and context
     */
    prioritizeActionItems(context) {
        console.log('⚖️ Prioritizing action items...');
        
        // Sort by priority score (higher = more urgent)
        this.wisdom.sort((a, b) => b.priority - a.priority);
        
        // Limit to configured range
        const itemCount = Math.min(
            Math.max(this.wisdom.length, this.config.minItems),
            this.config.maxItems
        );
        
        this.wisdom = this.wisdom.slice(0, itemCount);
        
        // Tag high-priority items for Oracle consultation
        this.tagForOracleConsultation(context);
        
        console.log(`⚖️ Prioritization complete - selected ${this.wisdom.length} action items`);
    }

    /**
     * Tag high-priority or complex items for Oracle consultation
     */
    tagForOracleConsultation(context) {
        console.log('🔮 Evaluating items for Oracle consultation...');
        
        let queuedForOracle = 0;
        
        this.wisdom.forEach(item => {
            // Check if item meets Oracle threshold criteria
            const shouldQueue = this.shouldQueueForOracle(item, context);
            
            if (shouldQueue) {
                // Mark item as queued for Oracle
                item.oracleQueued = true;
                item.oracleReason = shouldQueue.reason;
                
                // Add to Vision Queue
                const visionId = this.visionQueue.queueFromAdvisorIntel(item, 
                    `🔮 QUEUED FOR ORACLE: ${shouldQueue.reason}`);
                
                item.visionId = visionId;
                queuedForOracle++;
                
                console.log(`🔮 Queued for Oracle: "${item.title}" (${shouldQueue.reason})`);
            }
        });
        
        if (queuedForOracle > 0) {
            console.log(`🔮 ${queuedForOracle} items tagged for Oracle consultation`);
        }
    }

    /**
     * Determine if an item should be queued for Oracle consultation
     */
    shouldQueueForOracle(item, context) {
        // High priority threshold
        if (item.priority >= this.config.oracleThreshold) {
            return { reason: 'High priority strategic decision' };
        }
        
        // Complex technical decisions
        if (item.category === 'technical-debt' && item.impact === 'high') {
            return { reason: 'Complex technical debt requiring strategic vision' };
        }
        
        // Architecture and transformation decisions
        if (item.title.toLowerCase().includes('architecture') || 
            item.title.toLowerCase().includes('transformation') ||
            item.title.toLowerCase().includes('modernization')) {
            return { reason: 'Architectural decision requiring future-sight' };
        }
        
        // Integration complexity
        if (item.description.toLowerCase().includes('integration') && 
            item.effort === 'high') {
            return { reason: 'Complex integration requiring scenario planning' };
        }
        
        // Process transformation
        if (item.category === 'process' && item.impact === 'high') {
            return { reason: 'Process transformation requiring change management vision' };
        }
        
        return false;
    }

    /**
     * Generate final advisory report
     */
    generateAdvisoryReport() {
        const report = {
            role: 'advisor',
            title: 'The Advisor - Present Wisdom',
            summary: this.generateExecutiveSummary(),
            actionItems: this.wisdom,
            metadata: {
                timestamp: new Date().toISOString(),
                itemCount: this.wisdom.length,
                categories: [...new Set(this.wisdom.map(w => w.category))],
                highPriority: this.wisdom.filter(w => w.priority >= 80).length,
                quickWins: this.wisdom.filter(w => w.effort === 'low' && w.impact !== 'low').length
            }
        };

        console.log(`📋 Advisory report complete - ${report.actionItems.length} prioritized action items`);
        return report;
    }

    generateExecutiveSummary() {
        const categories = [...new Set(this.wisdom.map(w => w.category))];
        const highImpact = this.wisdom.filter(w => w.impact === 'high').length;
        const quickWins = this.wisdom.filter(w => w.effort === 'low' && w.impact !== 'low').length;

        return [
            `Repository audit identified ${this.wisdom.length} actionable improvement opportunities`,
            `Analysis spans ${categories.length} categories: ${categories.join(', ')}`,
            `${highImpact} high-impact items require immediate attention`,
            `${quickWins} quick wins available for immediate implementation`,
            'Prioritization based on impact, effort, and current project context'
        ];
    }

    generateNullReport(reason) {
        return {
            role: 'advisor',
            title: 'The Advisor - Analysis Constrained',
            summary: [
                'Advisory analysis was constrained by resource limitations',
                reason,
                'Consider running with extended budget or faculty:proceed label'
            ],
            actionItems: [],
            metadata: {
                timestamp: new Date().toISOString(),
                constrained: true,
                reason
            },
            telemetry: this.meter.generateTelemetryFooter()
        };
    }

    addTelemetryToReport(report) {
        return {
            ...report,
            telemetry: this.meter.generateTelemetryFooter()
        };
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const contextFile = args.find(arg => arg.startsWith('--context='))?.split('=')[1] || 'out/cid/context.json';
    const outputFile = args.find(arg => arg.startsWith('--out='))?.split('=')[1] || 'out/cid/advisor-report.json';
    const dryRun = args.includes('--dry-run');
    
    async function main() {
        try {
            if (!fs.existsSync(contextFile)) {
                console.error(`❌ Context file not found: ${contextFile}`);
                process.exit(1);
            }

            const context = JSON.parse(fs.readFileSync(contextFile, 'utf8'));
            const advisor = new Advisor({ dryRun });
            
            const report = await advisor.advise(context);
            
            // Ensure output directory exists
            const outputDir = path.dirname(outputFile);
            if (!fs.existsSync(outputDir)) {
                fs.mkdirSync(outputDir, { recursive: true });
            }
            
            fs.writeFileSync(outputFile, JSON.stringify(report, null, 2));
            console.log(`📄 Advisory report written to ${outputFile}`);
            
        } catch (error) {
            console.error(`❌ Advisor error:`, error.message);
            process.exit(1);
        }
    }
    
    main();
}

module.exports = { Advisor };