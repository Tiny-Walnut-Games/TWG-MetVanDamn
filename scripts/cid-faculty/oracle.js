#!/usr/bin/env node
/**
 * CID Faculty - The Oracle
 * 
 * Provides strategic forecasting through scenario maps and branching futures.
 * Delivers 2-3 "possible future" maps with risks, prerequisites, and leading indicators.
 */

const fs = require('fs');
const path = require('path');
const { SmartUsageMeter } = require('./usage.js');
const { ContextCache } = require('./shared/context-cache.js');
const { VisionQueue } = require('./vision-queue.js');
const { VisionArchive } = require('./vision-archive.js');

// Constants for probability calculations
const PROBABILITY_LOWER_BOUND = 0.1;
const PROBABILITY_UPPER_BOUND = 0.9;

class Oracle {
    constructor(config = {}) {
        this.config = {
            maxScenarios: config.maxScenarios || 3,
            minScenarios: config.minScenarios || 2,
            forecastHorizon: config.forecastHorizon || 6, // months
            dryRun: config.dryRun || false,
            ...config
        };
        
        this.meter = new SmartUsageMeter(config);
        this.cache = new ContextCache();
        this.visions = [];
        
        // Initialize Vision Queue and Archive for formal Faculty operations
        this.visionQueue = new VisionQueue();
        this.visionArchive = new VisionArchive();
        
        console.log(`🔮 The Oracle has manifested - peering into possible futures`);
    }

    /**
     * The Vision Ritual - Process vision requests from the queue
     */
    async processVisionQueue(context, maxVisions = 1) {
        console.log('🔮 Initiating Vision Ritual - consulting the queue...');
        
        const processedVisions = [];
        
        for (let i = 0; i < maxVisions; i++) {
            const visionRequest = this.visionQueue.dequeue();
            
            if (!visionRequest) {
                console.log('📭 No more visions pending in the queue');
                break;
            }
            
            console.log(`🌟 Processing vision: ${visionRequest.id}`);
            console.log(`   Trigger: ${visionRequest.trigger} - ${visionRequest.triggerReason}`);
            console.log(`   Context: ${visionRequest.contextNotes.slice(0, 100)}...`);
            
            // Perform the vision ritual
            const visionResult = await this.performVisionRitual(visionRequest, context);
            
            if (visionResult) {
                processedVisions.push(visionResult);
                
                // Mark as completed in queue
                this.visionQueue.markCompleted(visionRequest.id, visionResult.archival.reportPath);
                
                console.log(`✅ Vision ${visionRequest.id} completed and archived`);
            } else {
                console.error(`❌ Vision ${visionRequest.id} failed to process`);
            }
        }
        
        return {
            processed: processedVisions.length,
            visions: processedVisions,
            queueStatus: this.visionQueue.getStatus()
        };
    }

    /**
     * Perform individual vision ritual for a queued request
     */
    async performVisionRitual(visionRequest, context) {
        console.log(`🔮 Performing vision ritual for ${visionRequest.id}`);
        
        try {
            // 1. Gather context and intel
            const enrichedContext = this.enrichContextWithVisionRequest(context, visionRequest);
            
            // 2. Generate oracle forecast (existing functionality)
            const oracleReport = await this.forecast(enrichedContext);
            
            // 3. Create Vision Report structure
            const visionData = {
                visionRequest,
                enrichedContext,
                timestamp: new Date().toISOString(),
                status: 'completed'
            };
            
            // 4. Archive the vision with full cross-linking
            const archival = this.visionArchive.archiveVision(visionRequest, visionData, oracleReport);
            
            console.log(`📚 Vision ${visionRequest.id} archived to ${archival.reportPath}`);
            
            return {
                visionRequest,
                oracleReport,
                visionData,
                archival
            };
            
        } catch (error) {
            console.error(`❌ Vision ritual failed: ${error.message}`);
            return null;
        }
    }

    /**
     * Enrich context with vision request details
     */
    enrichContextWithVisionRequest(baseContext, visionRequest) {
        return {
            ...baseContext,
            visionContext: {
                id: visionRequest.id,
                trigger: visionRequest.trigger,
                priority: visionRequest.priority,
                contextNotes: visionRequest.contextNotes,
                sourceIntel: visionRequest.sourceIntel,
                visionType: visionRequest.visionType,
                requestedBy: visionRequest.requestedBy
            }
        };
    }

    /**
     * Queue a vision request (convenience method)
     */
    queueVision(request) {
        return this.visionQueue.enqueue(request);
    }

    /**
     * Get vision queue status
     */
    getQueueStatus() {
        return this.visionQueue.getStatus();
    }
    async forecast(context, syllabus = {}) {
        console.log('🌟 The Oracle begins strategic forecasting...');
        
        if (!this.meter.shouldProceed('oracle-analysis')) {
            return this.generateNullReport('Budget constraints prevent forecasting');
        }

        const cacheKey = this.cache.generateCacheKey(context);
        const cachedResults = this.cache.getCachedFacultyResults(cacheKey, 'oracle');
        
        if (cachedResults) {
            this.meter.trackCacheResult(true);
            console.log('🔮 Using cached oracle visions');
            return this.addTelemetryToReport(cachedResults);
        }
        
        this.meter.trackCacheResult(false);
        
        // Time the forecasting phase
        const forecastTimer = this.meter.startTiming('oracle', 'forecast');
        await this.generateScenarios(context, syllabus);
        this.meter.endTiming(forecastTimer, 'oracle');
        
        if (!this.meter.shouldProceed('oracle-synthesis')) {
            return this.generateNullReport('Time budget exhausted during forecasting');
        }
        
        this.synthesizeVisions(context);
        
        const report = this.generateOracularReport();
        
        // Cache the results
        this.cache.setCachedFacultyResults(cacheKey, 'oracle', report);
        
        return this.addTelemetryToReport(report);
    }

    /**
     * Generate strategic scenarios based on current context
     */
    async generateScenarios(context, syllabus) {
        console.log('🎯 Generating strategic scenarios...');
        
        // Analyze project maturity and trajectory
        const maturity = this.assessProjectMaturity(context);
        
        // Generate scenarios based on different paths
        this.generateGrowthScenario(context, maturity);
        this.generateOptimizationScenario(context, maturity);
        this.generateTransformationScenario(context, maturity);
        
        console.log(`🎯 Generated ${this.visions.length} strategic scenarios`);
    }

    assessProjectMaturity(context) {
        const topology = context.topology || {};
        const docs = context.docs || {};
        const configs = context.configs || {};
        const workflows = topology.workflows || [];
        
        let maturityScore = 0;
        
        // Infrastructure maturity
        if (workflows.length >= 3) maturityScore += 20;
        else if (workflows.length >= 1) maturityScore += 10;
        
        // Documentation maturity
        if (docs.tldlEntries >= 10) maturityScore += 15;
        else if (docs.tldlEntries >= 5) maturityScore += 10;
        else if (docs.tldlEntries >= 1) maturityScore += 5;
        
        // Configuration maturity
        if (configs.packageJson) maturityScore += 10;
        if (configs.editorConfig) maturityScore += 5;
        if (configs.gitignore) maturityScore += 10;
        
        // Code maturity
        if (topology.totalLOC > 10000) maturityScore += 15;
        else if (topology.totalLOC > 1000) maturityScore += 10;
        else if (topology.totalLOC > 100) maturityScore += 5;
        
        // Language diversity
        const languageCount = Object.keys(topology.languages || {}).length;
        if (languageCount >= 3) maturityScore += 15;
        else if (languageCount >= 2) maturityScore += 10;
        else if (languageCount >= 1) maturityScore += 5;
        
        return {
            score: maturityScore,
            level: maturityScore >= 70 ? 'mature' : maturityScore >= 40 ? 'developing' : 'early',
            strengths: this.identifyStrengths(context),
            gaps: this.identifyGaps(context)
        };
    }

    identifyStrengths(context) {
        const strengths = [];
        const topology = context.topology || {};
        const docs = context.docs || {};
        const workflows = topology.workflows || [];
        
        if (docs.tldlEntries >= 5) strengths.push('Rich living documentation');
        if (workflows.length >= 3) strengths.push('Comprehensive CI/CD');
        if (docs.copilotInstructions) strengths.push('AI-assisted development');
        if (topology.totalLOC > 5000) strengths.push('Substantial codebase');
        
        return strengths;
    }

    identifyGaps(context) {
        const gaps = [];
        const configs = context.configs || {};
        const workflows = context.topology?.workflows || [];
        
        if (!configs.packageJson) gaps.push('Package management');
        if (workflows.length === 0) gaps.push('Automation pipeline');
        if (!configs.gitignore) gaps.push('Security configuration');
        
        return gaps;
    }

    generateGrowthScenario(context, maturity) {
        const scenario = {
            name: 'Organic Growth Path',
            timeframe: `${this.config.forecastHorizon} months`,
            probability: this.calculateProbability(maturity, 'growth'),
            description: 'Steady evolution building on current foundation',
            trajectory: this.generateGrowthTrajectory(context, maturity),
            prerequisites: this.generateGrowthPrerequisites(context, maturity),
            risks: this.generateGrowthRisks(context, maturity),
            leadingIndicators: this.generateGrowthIndicators(context),
            outcomes: this.generateGrowthOutcomes(context, maturity)
        };
        
        this.visions.push(scenario);
    }

    generateOptimizationScenario(context, maturity) {
        const scenario = {
            name: 'Efficiency Optimization Path',
            timeframe: `${Math.ceil(this.config.forecastHorizon * 0.7)} months`,
            probability: this.calculateProbability(maturity, 'optimization'),
            description: 'Focus on performance, automation, and technical excellence',
            trajectory: this.generateOptimizationTrajectory(context, maturity),
            prerequisites: this.generateOptimizationPrerequisites(context, maturity),
            risks: this.generateOptimizationRisks(context, maturity),
            leadingIndicators: this.generateOptimizationIndicators(context),
            outcomes: this.generateOptimizationOutcomes(context, maturity)
        };
        
        this.visions.push(scenario);
    }

    generateTransformationScenario(context, maturity) {
        const scenario = {
            name: 'Strategic Transformation Path',
            timeframe: `${this.config.forecastHorizon * 2} months`,
            probability: this.calculateProbability(maturity, 'transformation'),
            description: 'Fundamental architectural and process evolution',
            trajectory: this.generateTransformationTrajectory(context, maturity),
            prerequisites: this.generateTransformationPrerequisites(context, maturity),
            risks: this.generateTransformationRisks(context, maturity),
            leadingIndicators: this.generateTransformationIndicators(context),
            outcomes: this.generateTransformationOutcomes(context, maturity)
        };
        
        this.visions.push(scenario);
    }

    calculateProbability(maturity, scenarioType) {
        const baseProbs = {
            growth: 0.7,
            optimization: 0.5,
            transformation: 0.3
        };
        
        const maturityModifiers = {
            early: { growth: 0.2, optimization: -0.1, transformation: -0.2 },
            developing: { growth: 0.1, optimization: 0.1, transformation: 0.0 },
            mature: { growth: -0.1, optimization: 0.2, transformation: 0.2 }
        };
        
        const baseProb = baseProbs[scenarioType];
        const modifier = maturityModifiers[maturity.level]?.[scenarioType] || 0;
        
        return Math.max(PROBABILITY_LOWER_BOUND, Math.min(PROBABILITY_UPPER_BOUND, baseProb + modifier));
    }

    generateGrowthTrajectory(context, maturity) {
        const milestones = [];
        
        if (maturity.level === 'early') {
            milestones.push('Establish basic CI/CD pipeline');
            milestones.push('Implement core documentation practices');
            milestones.push('Add security and quality gates');
        } else if (maturity.level === 'developing') {
            milestones.push('Expand automation coverage');
            milestones.push('Enhance testing and monitoring');
            milestones.push('Implement advanced workflows');
        } else {
            milestones.push('Optimize performance and scalability');
            milestones.push('Advanced integration patterns');
            milestones.push('Cross-team collaboration tools');
        }
        
        return milestones;
    }

    generateGrowthPrerequisites(context, maturity) {
        const prereqs = ['Current development team capacity', 'Stable main branch'];
        
        if (maturity.level === 'early') {
            prereqs.push('Basic Git workflow established');
        }
        
        if (maturity.gaps.includes('Package management')) {
            prereqs.push('Package management setup');
        }
        
        return prereqs;
    }

    generateGrowthRisks(context, maturity) {
        return [
            'Technical debt accumulation during rapid growth',
            'Resource constraints limiting implementation speed',
            'Integration complexity with existing systems'
        ];
    }

    generateGrowthIndicators(context) {
        return [
            'Increasing commit frequency and PR velocity',
            'Growing test coverage and passing rates',
            'Expanding documentation and knowledge base',
            'Decreasing time-to-deployment'
        ];
    }

    generateGrowthOutcomes(context, maturity) {
        return [
            'Sustainable development velocity increase of 25-40%',
            'Improved code quality and maintainability',
            'Enhanced team productivity and collaboration',
            'Reduced manual work through automation'
        ];
    }

    // Similar methods for optimization and transformation scenarios...
    generateOptimizationTrajectory(context, maturity) {
        return [
            'Performance profiling and bottleneck identification',
            'Automated testing and quality gate enhancement',
            'CI/CD pipeline optimization and parallelization',
            'Resource usage monitoring and alerting'
        ];
    }

    generateOptimizationPrerequisites(context, maturity) {
        return [
            'Existing automation infrastructure',
            'Performance baseline metrics',
            'Team commitment to technical excellence'
        ];
    }

    generateOptimizationRisks(context, maturity) {
        return [
            'Over-optimization leading to complexity',
            'Resource investment with unclear ROI',
            'Team disruption during optimization phases'
        ];
    }

    generateOptimizationIndicators(context) {
        return [
            'Decreasing build and deployment times',
            'Improving system performance metrics',
            'Reducing resource consumption',
            'Increasing automation coverage percentage'
        ];
    }

    generateOptimizationOutcomes(context, maturity) {
        return [
            '50-70% reduction in manual deployment effort',
            'Significant performance improvements',
            'Cost optimization through efficient resource usage',
            'Enhanced system reliability and uptime'
        ];
    }

    generateTransformationTrajectory(context, maturity) {
        return [
            'Architecture evolution planning and design',
            'Legacy system modernization strategy',
            'New technology integration roadmap',
            'Organizational process transformation'
        ];
    }

    generateTransformationPrerequisites(context, maturity) {
        return [
            'Executive sponsorship and budget allocation',
            'Dedicated transformation team',
            'Comprehensive current-state documentation',
            'Risk tolerance for significant changes'
        ];
    }

    generateTransformationRisks(context, maturity) {
        return [
            'High complexity and coordination challenges',
            'Significant resource investment requirements',
            'Potential system instability during transition',
            'Skills gap requiring training or hiring'
        ];
    }

    generateTransformationIndicators(context) {
        return [
            'Successful proof-of-concept implementations',
            'Team skill development and certification progress',
            'Stakeholder engagement and buy-in levels',
            'Technical architecture decision ratification'
        ];
    }

    generateTransformationOutcomes(context, maturity) {
        return [
            'Fundamental capability enhancement',
            'Competitive advantage through modern architecture',
            'Improved scalability and flexibility',
            'Long-term maintainability and evolution capability'
        ];
    }

    synthesizeVisions(context) {
        console.log('🔮 Synthesizing oracular visions...');
        
        // Rank scenarios by probability and impact
        this.visions.sort((a, b) => b.probability - a.probability);
        
        // Limit to configured range
        const scenarioCount = Math.min(
            Math.max(this.visions.length, this.config.minScenarios),
            this.config.maxScenarios
        );
        
        this.visions = this.visions.slice(0, scenarioCount);
        
        console.log(`🔮 Synthesis complete - ${this.visions.length} scenarios selected`);
    }

    generateOracularReport() {
        const report = {
            role: 'oracle',
            title: 'The Oracle - Future Sight',
            summary: this.generatePropheticSummary(),
            scenarios: this.visions,
            metadata: {
                timestamp: new Date().toISOString(),
                scenarioCount: this.visions.length,
                forecastHorizon: this.config.forecastHorizon,
                highProbability: this.visions.filter(v => v.probability >= 0.6).length,
                transformativeScenarios: this.visions.filter(v => v.name.includes('Transformation')).length
            }
        };

        console.log(`🔮 Oracular report complete - ${report.scenarios.length} future scenarios revealed`);
        return report;
    }

    generatePropheticSummary() {
        const avgProbability = this.visions.reduce((sum, v) => sum + v.probability, 0) / this.visions.length;
        const scenarioTypes = this.visions.map(v => v.name.split(' ')[0]).join(', ');
        
        return [
            `Strategic forecasting reveals ${this.visions.length} possible futures over ${this.config.forecastHorizon}-month horizon`,
            `Scenario probability range: ${Math.min(...this.visions.map(v => v.probability)).toFixed(1)} - ${Math.max(...this.visions.map(v => v.probability)).toFixed(1)}`,
            `Primary pathways identified: ${scenarioTypes}`,
            'Each scenario includes trajectory, prerequisites, risks, and leading indicators',
            'Success depends on proactive preparation and adaptive execution'
        ];
    }

    generateNullReport(reason) {
        return {
            role: 'oracle',
            title: 'The Oracle - Vision Obscured',
            summary: [
                'Strategic forecasting was constrained by resource limitations',
                'The future remains shrouded in temporal mists',
                reason,
                'Consider consulting the Oracle again with extended resources'
            ],
            scenarios: [],
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
    const command = args[0] || 'forecast';
    const contextFile = args.find(arg => arg.startsWith('--context='))?.split('=')[1] || 'out/cid/context.json';
    const outputFile = args.find(arg => arg.startsWith('--out='))?.split('=')[1] || 'out/cid/oracle-report.json';
    const dryRun = args.includes('--dry-run');
    const maxVisions = parseInt(args.find(arg => arg.startsWith('--max-visions='))?.split('=')[1]) || 1;
    
    async function main() {
        try {
            if (command === 'help' || command === '--help') {
                console.log('🔮 Oracle Faculty CLI');
                console.log('Usage: node oracle.js [command] [options]');
                console.log('');
                console.log('Commands:');
                console.log('  forecast      Generate strategic forecast (default)');
                console.log('  ritual        Process vision queue using Vision Ritual');
                console.log('  queue-status  Show vision queue status');
                console.log('  queue-add     Add vision to queue');
                console.log('');
                console.log('Options:');
                console.log('  --context=<file>     Context file (default: out/cid/context.json)');
                console.log('  --out=<file>         Output file (default: out/cid/oracle-report.json)');
                console.log('  --max-visions=<n>    Max visions to process in ritual (default: 1)');
                console.log('  --dry-run            Dry run mode');
                return;
            }

            const oracle = new Oracle({ dryRun });
            
            switch (command) {
                case 'ritual':
                    console.log('🔮 Initiating Oracle Vision Ritual...');
                    
                    if (!fs.existsSync(contextFile)) {
                        console.error(`❌ Context file not found: ${contextFile}`);
                        process.exit(1);
                    }
                    
                    const context = JSON.parse(fs.readFileSync(contextFile, 'utf8'));
                    const ritualResult = await oracle.processVisionQueue(context, maxVisions);
                    
                    // Ensure output directory exists
                    const outputDir = path.dirname(outputFile);
                    if (!fs.existsSync(outputDir)) {
                        fs.mkdirSync(outputDir, { recursive: true });
                    }
                    
                    fs.writeFileSync(outputFile, JSON.stringify(ritualResult, null, 2));
                    console.log(`🔮 Vision Ritual complete - ${ritualResult.processed} visions processed`);
                    console.log(`📊 Queue Status: ${ritualResult.queueStatus.pending} pending, ${ritualResult.queueStatus.processed} total processed`);
                    console.log(`📄 Results written to ${outputFile}`);
                    break;
                    
                case 'queue-status':
                    const queueStatus = oracle.getQueueStatus();
                    console.log('\n🔮 Vision Queue Status:');
                    console.log(`   Total: ${queueStatus.total}`);
                    console.log(`   Pending: ${queueStatus.pending}`);
                    console.log(`   Processing: ${queueStatus.processing}`);
                    console.log(`   Processed: ${queueStatus.processed}`);
                    if (queueStatus.nextPriority > 0) {
                        console.log(`   Next Priority: ${queueStatus.nextPriority}`);
                    }
                    break;
                    
                case 'queue-add':
                    const notes = args.slice(1).join(' ') || 'Oracle CLI vision request';
                    const visionId = oracle.queueVision({
                        trigger: 'manual',
                        triggerReason: 'Oracle CLI request',
                        contextNotes: notes,
                        priority: 60,
                        visionType: 'general',
                        requestedBy: 'oracle-cli'
                    });
                    console.log(`✅ Vision queued: ${visionId}`);
                    break;
                    
                case 'forecast':
                default:
                    // Original forecast functionality
                    if (!fs.existsSync(contextFile)) {
                        console.error(`❌ Context file not found: ${contextFile}`);
                        process.exit(1);
                    }

                    const forecastContext = JSON.parse(fs.readFileSync(contextFile, 'utf8'));
                    const report = await oracle.forecast(forecastContext);
                    
                    // Ensure output directory exists
                    const forecastOutputDir = path.dirname(outputFile);
                    if (!fs.existsSync(forecastOutputDir)) {
                        fs.mkdirSync(forecastOutputDir, { recursive: true });
                    }
                    
                    fs.writeFileSync(outputFile, JSON.stringify(report, null, 2));
                    console.log(`🔮 Oracle report written to ${outputFile}`);
                    break;
            }
            
        } catch (error) {
            console.error(`❌ Oracle error:`, error.message);
            process.exit(1);
        }
    }
    
    main();
}

module.exports = { Oracle };