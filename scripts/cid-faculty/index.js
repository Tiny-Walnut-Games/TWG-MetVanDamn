#!/usr/bin/env node
/**
 * CID Faculty - Main Entry Point
 * 
 * Orchestrates Advisor + Oracle consultation with Smart Usage Meter
 * Implements repository-wide analysis with budget controls and intelligent caching
 */

const fs = require('fs');
const path = require('path');
const { Advisor } = require('./advisor.js');
const { Oracle } = require('./oracle.js');
const { FacultyTLDLGenerator } = require('./faculty-tldl.js');
const { SmartUsageMeter } = require('./usage.js');

class CIDFaculty {
    constructor(config = {}) {
        this.config = {
            roles: config.roles || ['advisor', 'oracle'], // Which faculty to consult
            budgetMinutes: config.budgetMinutes || 6,
            analysis: config.analysis || 'standard', // quick, standard, deep
            scope: config.scope || 'repository-wide',
            dryRun: config.dryRun || false,
            issueNumber: config.issueNumber || 0,
            ...config
        };

        this.meter = new SmartUsageMeter({
            maxRuntimeMinutes: this.config.budgetMinutes,
            dryRun: this.config.dryRun
        });

        this.context = null;
        this.results = {
            advisor: null,
            oracle: null,
            telemetry: null,
            tldlEntry: null
        };

        console.log(`🎓📜 CID Faculty initialized - Roles: ${this.config.roles.join(', ')}, Budget: ${this.config.budgetMinutes}m`);
    }

    /**
     * Main faculty consultation workflow
     */
    async consult(contextData = null) {
        console.log('🎓 Initiating Faculty Consultation...');
        
        if (!this.meter.shouldProceed('faculty-init')) {
            return this.generateConstrainedReport('Budget exhausted before consultation could begin');
        }

        try {
            // 1. Gather repository context
            this.context = contextData || await this.gatherRepositoryContext();
            
            if (!this.context) {
                return this.generateConstrainedReport('Failed to gather repository context');
            }

            // 2. Check for early exit conditions
            if (this.shouldEarlyExit()) {
                return this.generateEarlyExitReport();
            }

            // 3. Consult faculty roles based on configuration
            await this.consultFaculty();

            // 4. Generate comprehensive TLDL entry
            await this.generateFacultyTLDL();

            // 5. Create final consultation report
            return this.generateFinalReport();

        } catch (error) {
            console.error(`❌ Faculty consultation error: ${error.message}`);
            return this.generateErrorReport(error);
        }
    }

    /**
     * Gather repository context for analysis
     */
    async gatherRepositoryContext() {
        console.log('📋 Gathering repository context...');
        
        const context = {
            timestamp: new Date().toISOString(),
            scope: this.config.scope,
            analysis: this.config.analysis,
            
            // Repository structure
            topology: await this.analyzeTopology(),
            
            // Documentation analysis
            docs: await this.analyzeDocs(),
            
            // Configuration files
            configs: await this.analyzeConfigs(),
            
            // GitHub workflows and actions
            pipelines: await this.analyzePipelines(),
            
            // Code signals (TODOs, FIXMEs, etc.)
            signals: await this.analyzeCodeSignals()
        };

        console.log('📋 Repository context gathered successfully');
        return context;
    }

    /**
     * Analyze repository topology and structure
     */
    async analyzeTopology() {
        const topology = {
            totalLOC: 0,
            languages: {},
            workflows: [],
            directories: []
        };

        try {
            // Get basic file structure
            const files = this.getFilesRecursively('.');
            
            topology.totalFiles = files.length;
            
            // Analyze languages
            files.forEach(file => {
                const ext = path.extname(file);
                const lang = this.getLanguageFromExtension(ext);
                if (lang) {
                    topology.languages[lang] = (topology.languages[lang] || 0) + 1;
                }
            });

            // Find GitHub workflows
            const workflowDir = '.github/workflows';
            if (fs.existsSync(workflowDir)) {
                topology.workflows = fs.readdirSync(workflowDir)
                    .filter(f => f.endsWith('.yml') || f.endsWith('.yaml'))
                    .map(f => f.replace(/\.(yml|yaml)$/, ''));
            }

            // Count approximate LOC for major files
            topology.totalLOC = this.estimateLOC(files);

        } catch (error) {
            console.warn(`⚠️ Topology analysis error: ${error.message}`);
        }

        return topology;
    }

    /**
     * Analyze documentation completeness
     */
    async analyzeDocs() {
        const docs = {
            readme: null,
            contributing: null,
            tldlEntries: 0,
            copilotInstructions: null
        };

        try {
            // Check for README
            if (fs.existsSync('README.md')) {
                docs.readme = fs.readFileSync('README.md', 'utf8').slice(0, 1000);
            }

            // Check for CONTRIBUTING
            docs.contributing = fs.existsSync('CONTRIBUTING.md');

            // Count TLDL entries
            if (fs.existsSync('TLDL/entries')) {
                docs.tldlEntries = fs.readdirSync('TLDL/entries')
                    .filter(f => f.endsWith('.md')).length;
            }

            // Check for Copilot instructions
            docs.copilotInstructions = fs.existsSync('.github/copilot-instructions.md');

        } catch (error) {
            console.warn(`⚠️ Documentation analysis error: ${error.message}`);
        }

        return docs;
    }

    /**
     * Analyze configuration files
     */
    async analyzeConfigs() {
        const configs = {
            packageJson: fs.existsSync('package.json'),
            gitignore: fs.existsSync('.gitignore'),
            editorConfig: fs.existsSync('.editorconfig'),
            vscodeSetting: fs.existsSync('.vscode')
        };

        return configs;
    }

    /**
     * Analyze CI/CD pipelines and actions
     */
    async analyzePipelines() {
        const pipelines = {
            workflowCount: 0,
            hasTests: false,
            hasSecurity: false,
            hasDeployment: false
        };

        try {
            const workflowDir = '.github/workflows';
            if (fs.existsSync(workflowDir)) {
                const workflows = fs.readdirSync(workflowDir);
                pipelines.workflowCount = workflows.length;
                
                workflows.forEach(workflow => {
                    const name = workflow.toLowerCase();
                    if (name.includes('test') || name.includes('ci')) {
                        pipelines.hasTests = true;
                    }
                    if (name.includes('security') || name.includes('scan')) {
                        pipelines.hasSecurity = true;
                    }
                    if (name.includes('deploy') || name.includes('release')) {
                        pipelines.hasDeployment = true;
                    }
                });
            }
        } catch (error) {
            console.warn(`⚠️ Pipeline analysis error: ${error.message}`);
        }

        return pipelines;
    }

    /**
     * Analyze code signals (TODOs, FIXMEs, etc.)
     */
    async analyzeCodeSignals() {
        const signals = {
            todos: [],
            fixmes: [],
            hacks: []
        };

        try {
            const files = this.getFilesRecursively('.')
                .filter(f => !f.includes('node_modules') && !f.includes('.git'))
                .filter(f => ['.js', '.ts', '.py', '.md', '.yaml', '.yml'].includes(path.extname(f)))
                .slice(0, 50); // Limit for performance

            files.forEach(file => {
                try {
                    const content = fs.readFileSync(file, 'utf8');
                    const lines = content.split('\n');
                    
                    lines.forEach((line, index) => {
                        const lower = line.toLowerCase();
                        if (lower.includes('todo') || lower.includes('to do')) {
                            signals.todos.push({ file, line: index + 1, text: line.trim() });
                        }
                        if (lower.includes('fixme') || lower.includes('fix me')) {
                            signals.fixmes.push({ file, line: index + 1, text: line.trim() });
                        }
                        if (/\bhack\b/i.test(line) && (lower.includes('//') || lower.includes('#'))) {
                            signals.hacks.push({ file, line: index + 1, text: line.trim() });
                        }
                    });
                } catch (err) {
                    // Skip files that can't be read
                }
            });

        } catch (error) {
            console.warn(`⚠️ Code signals analysis error: ${error.message}`);
        }

        return signals;
    }

    /**
     * Check if we should exit early due to minimal changes
     */
    shouldEarlyExit() {
        // Implement change detection logic here
        // For now, always proceed with analysis
        return false;
    }

    /**
     * Determine if analysis should be upgraded to strategic level
     */
    shouldUpgradeToStrategic(context) {
        // Upgrade to strategic if this appears to be a Living Dev Agent repository
        const indicators = [
            context.docs?.tldlEntries >= 5,
            context.topology?.workflows?.length >= 3,
            context.docs?.readme?.toLowerCase().includes('living dev agent'),
            context.docs?.copilotInstructions,
            context.topology?.languages?.JavaScript && context.configs?.packageJson,
            fs.existsSync('scripts/cid-faculty')
        ];

        return indicators.filter(Boolean).length >= 3;
    }

    /**
     * Consult the configured faculty roles
     */
    async consultFaculty() {
        console.log('🎯 Consulting faculty roles...');

        // Consult Advisor if requested
        if (this.config.roles.includes('advisor')) {
            console.log('👨‍🏫 Consulting The Advisor...');
            if (this.meter.shouldProceed('advisor-consultation')) {
                const advisor = new Advisor({ 
                    dryRun: this.config.dryRun,
                    maxRuntimeMinutes: this.config.budgetMinutes 
                });
                this.results.advisor = await advisor.advise(this.context);
                console.log('✅ Advisor consultation complete');
            } else {
                console.log('⏭️ Skipping Advisor consultation due to budget constraints');
            }
        }

        // Consult Oracle if requested
        if (this.config.roles.includes('oracle')) {
            console.log('🔮 Consulting The Oracle...');
            if (this.meter.shouldProceed('oracle-consultation')) {
                const oracle = new Oracle({ 
                    dryRun: this.config.dryRun,
                    maxRuntimeMinutes: this.config.budgetMinutes 
                });
                this.results.oracle = await oracle.forecast(this.context);
                console.log('✅ Oracle consultation complete');
            } else {
                console.log('⏭️ Skipping Oracle consultation due to budget constraints');
            }
        }
    }

    /**
     * Generate comprehensive TLDL entry from faculty consultations
     */
    async generateFacultyTLDL() {
        if (!this.results.advisor && !this.results.oracle) {
            console.log('⏭️ Skipping TLDL generation - no faculty consultations completed');
            return;
        }

        console.log('📜 Generating Faculty TLDL entry...');
        
        try {
            const generator = new FacultyTLDLGenerator();
            this.results.tldlEntry = generator.generateFromFacultyReports(
                this.results.advisor,
                this.results.oracle,
                this.config.issueNumber
            );
            
            // Write TLDL entry to file
            const tldlDir = 'TLDL/entries';
            if (!fs.existsSync(tldlDir)) {
                fs.mkdirSync(tldlDir, { recursive: true });
            }
            
            const tldlPath = path.join(tldlDir, this.results.tldlEntry.filename);
            fs.writeFileSync(tldlPath, this.results.tldlEntry.content);
            
            console.log(`📜 TLDL entry written to ${tldlPath}`);
            
        } catch (error) {
            console.error(`❌ TLDL generation error: ${error.message}`);
        }
    }

    /**
     * Generate final consultation report
     */
    generateFinalReport() {
        const report = {
            facultyConsultation: {
                timestamp: new Date().toISOString(),
                roles: this.config.roles,
                budget: {
                    allocated: this.config.budgetMinutes,
                    used: this.meter.getElapsedMinutes(),
                    efficiency: this.calculateEfficiencyBadge()
                },
                scope: this.config.scope,
                analysis: this.config.analysis
            },
            results: {
                advisor: this.results.advisor,
                oracle: this.results.oracle,
                tldlEntry: this.results.tldlEntry ? {
                    filename: this.results.tldlEntry.filename,
                    generated: true
                } : null
            },
            telemetry: this.meter.generateTelemetryFooter(),
            summary: this.generateExecutiveSummary()
        };

        console.log('✅ Faculty consultation complete');
        return report;
    }

    /**
     * Generate executive summary of consultation
     */
    generateExecutiveSummary() {
        const summary = [];
        
        summary.push(`🎓📜 CID Faculty consultation completed for ${this.config.scope} analysis`);
        
        if (this.results.advisor) {
            const actionCount = this.results.advisor.actionItems?.length || 0;
            summary.push(`👨‍🏫 Advisor provided ${actionCount} prioritized action items with evidence`);
        }
        
        if (this.results.oracle) {
            const scenarioCount = this.results.oracle.scenarios?.length || 0;
            summary.push(`🔮 Oracle revealed ${scenarioCount} strategic scenarios with risk assessment`);
        }
        
        if (this.results.tldlEntry) {
            summary.push(`📜 Comprehensive TLDL entry generated: ${this.results.tldlEntry.filename}`);
        }
        
        const budgetUsed = this.meter.getElapsedMinutes();
        const efficiency = this.calculateEfficiencyBadge();
        summary.push(`📊 Budget: ${budgetUsed.toFixed(2)}m/${this.config.budgetMinutes}m (${efficiency})`);
        
        return summary;
    }

    /**
     * Calculate efficiency badge based on budget usage
     */
    calculateEfficiencyBadge() {
        const usage = this.meter.getElapsedMinutes() / this.config.budgetMinutes;
        
        if (usage <= 0.5) {
            return '🏆 Budget-Wise';
        } else if (usage <= 0.8) {
            return '✅ Efficient';
        } else if (usage <= 1.0) {
            return '⚠️ Near Limit';
        } else {
            return '❌ Over Budget';
        }
    }

    /**
     * Generate constrained report when budget is exceeded
     */
    generateConstrainedReport(reason) {
        return {
            facultyConsultation: {
                timestamp: new Date().toISOString(),
                constrained: true,
                reason
            },
            telemetry: this.meter.generateTelemetryFooter(),
            summary: [
                '🎓📜 CID Faculty consultation constrained by resource limitations',
                reason,
                'Consider running with extended budget or faculty:proceed label'
            ]
        };
    }

    /**
     * Generate early exit report for minimal changes
     */
    generateEarlyExitReport() {
        return {
            facultyConsultation: {
                timestamp: new Date().toISOString(),
                earlyExit: true,
                reason: 'Change set below analysis threshold'
            },
            telemetry: this.meter.generateTelemetryFooter(),
            summary: [
                '🎓📜 CID Faculty consultation skipped - minimal changes detected',
                'Change set below configured analysis threshold',
                'Consider manual consultation for strategic review'
            ]
        };
    }

    /**
     * Generate error report
     */
    generateErrorReport(error) {
        return {
            facultyConsultation: {
                timestamp: new Date().toISOString(),
                error: true,
                message: error.message
            },
            telemetry: this.meter.generateTelemetryFooter(),
            summary: [
                '❌ CID Faculty consultation encountered an error',
                error.message,
                'Check logs for detailed error information'
            ]
        };
    }

    // Helper methods

    getFilesRecursively(dir) {
        let files = [];
        try {
            const items = fs.readdirSync(dir);
            for (const item of items) {
                if (item.startsWith('.') || item === 'node_modules') continue;
                
                const fullPath = path.join(dir, item);
                const stat = fs.statSync(fullPath);
                
                if (stat.isDirectory()) {
                    files = files.concat(this.getFilesRecursively(fullPath));
                } else {
                    files.push(fullPath);
                }
            }
        } catch (error) {
            // Skip directories we can't read
        }
        return files;
    }

    getLanguageFromExtension(ext) {
        const languages = {
            '.js': 'JavaScript',
            '.ts': 'TypeScript',
            '.py': 'Python',
            '.java': 'Java',
            '.cs': 'C#',
            '.cpp': 'C++',
            '.c': 'C',
            '.go': 'Go',
            '.rs': 'Rust',
            '.php': 'PHP',
            '.rb': 'Ruby',
            '.md': 'Markdown',
            '.yaml': 'YAML',
            '.yml': 'YAML',
            '.json': 'JSON',
            '.xml': 'XML',
            '.html': 'HTML',
            '.css': 'CSS',
            '.scss': 'SCSS',
            '.sh': 'Shell'
        };
        return languages[ext];
    }

    estimateLOC(files) {
        let totalLOC = 0;
        const codeFiles = files.filter(f => {
            const ext = path.extname(f);
            return ['.js', '.ts', '.py', '.java', '.cs', '.cpp', '.c', '.go', '.rs'].includes(ext);
        }).slice(0, 20); // Limit for performance

        codeFiles.forEach(file => {
            try {
                const content = fs.readFileSync(file, 'utf8');
                totalLOC += content.split('\n').length;
            } catch (error) {
                // Skip files we can't read
            }
        });

        return totalLOC;
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    
    async function main() {
        try {
            // Parse CLI arguments
            const config = {
                roles: [],
                budgetMinutes: 6,
                analysis: 'standard',
                scope: 'repository-wide',
                dryRun: args.includes('--dry-run'),
                issueNumber: parseInt(args.find(arg => arg.startsWith('--issue='))?.split('=')[1]) || 0
            };

            // Parse role selection
            if (args.includes('--advisor-only')) {
                config.roles = ['advisor'];
            } else if (args.includes('--oracle-only')) {
                config.roles = ['oracle'];
            } else {
                config.roles = ['advisor', 'oracle'];
            }

            // Parse budget
            const budgetArg = args.find(arg => arg.startsWith('--budget='));
            if (budgetArg) {
                config.budgetMinutes = parseInt(budgetArg.split('=')[1]);
            }

            // Parse analysis depth
            if (args.includes('--quick')) {
                config.analysis = 'quick';
                config.budgetMinutes = 3;
            } else if (args.includes('--deep')) {
                config.analysis = 'deep';
                config.budgetMinutes = 12;
            }

            // Parse scope
            const scopeArg = args.find(arg => arg.startsWith('--scope='));
            if (scopeArg) {
                config.scope = scopeArg.split('=')[1];
            }

            // Show help
            if (args.includes('--help') || args.includes('-h')) {
                console.log('🎓📜 CID Faculty - Advisor + Oracle Consultation System');
                console.log('');
                console.log('Usage: node index.js [options]');
                console.log('');
                console.log('Faculty Roles:');
                console.log('  --advisor-only    Consult only The Advisor (present-state analysis)');
                console.log('  --oracle-only     Consult only The Oracle (strategic forecasting)');
                console.log('  (default)         Consult both Advisor and Oracle');
                console.log('');
                console.log('Budget Control:');
                console.log('  --budget=<min>    Set time budget in minutes (default: 6)');
                console.log('  --quick           Quick analysis (3 minutes)');
                console.log('  --deep            Deep analysis (12 minutes, requires faculty:proceed)');
                console.log('');
                console.log('Analysis Options:');
                console.log('  --scope=<area>    Focus area (default: repository-wide)');
                console.log('  --issue=<num>     Issue number for TLDL cross-linking');
                console.log('  --dry-run         Preview mode without artifacts');
                console.log('');
                console.log('Examples:');
                console.log('  node index.js --advisor-only --quick');
                console.log('  node index.js --oracle-only --scope=security');
                console.log('  node index.js --deep --issue=42');
                return;
            }

            // Initialize and run faculty consultation
            const faculty = new CIDFaculty(config);
            const report = await faculty.consult();

            // Output results
            const outputFile = args.find(arg => arg.startsWith('--out='))?.split('=')[1] || 'out/cid/faculty-consultation.json';
            const outputDir = path.dirname(outputFile);
            
            if (!fs.existsSync(outputDir)) {
                fs.mkdirSync(outputDir, { recursive: true });
            }
            
            fs.writeFileSync(outputFile, JSON.stringify(report, null, 2));
            console.log(`📄 Faculty consultation report written to ${outputFile}`);

            // Print summary
            console.log('\n📊 Consultation Summary:');
            report.summary.forEach(line => console.log(`   ${line}`));

        } catch (error) {
            console.error(`❌ Faculty consultation error: ${error.message}`);
            process.exit(1);
        }
    }

    main();
}

module.exports = { CIDFaculty };