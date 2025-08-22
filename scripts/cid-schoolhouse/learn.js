#!/usr/bin/env node
/**
 * CID Schoolhouse - Learn Repository Context
 * 
 * Builds a comprehensive context pack of the repository including:
 * - Topology (directories, languages, LOC)
 * - Documentation structure and quality
 * - Pipeline configurations and workflows
 * - Configuration files and dependencies
 * - Development signals (TODOs, markers, coverage)
 */

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

function read(p){ 
    try { 
        return fs.readFileSync(p,'utf8'); 
    } catch { 
        return null; 
    } 
}

function rg(pattern, dir='.') { 
    try { 
        return execSync(`rg -n --no-heading "${pattern}" ${dir}`, {stdio:['ignore','pipe','ignore']}).toString().trim(); 
    } catch { 
        return ''; 
    } 
}

function safeExec(command) {
    try {
        return execSync(command, {stdio:['ignore','pipe','ignore']}).toString().trim();
    } catch {
        return '';
    }
}

function countLines(dir) {
    try {
        const result = safeExec(`find ${dir} -name "*.js" -o -name "*.py" -o -name "*.cs" -o -name "*.md" | grep -v node_modules | head -100 | xargs wc -l | tail -1`);
        return parseInt(result.split(' ')[0]) || 0;
    } catch {
        return 0;
    }
}

class RepositoryLearner {
    constructor() {
        this.projectRoot = process.cwd();
        this.context = {
            timestamp: new Date().toISOString(),
            topology: {},
            docs: {},
            pipelines: {},
            configs: {},
            signals: {}
        };
    }

    learnTopology() {
        console.log('🗂️ Learning repository topology...');
        
        try {
            const dirs = fs.readdirSync('.').filter(d => {
                try {
                    return fs.statSync(d).isDirectory() && !d.startsWith('.') && d !== 'node_modules';
                } catch {
                    return false;
                }
            });

            this.context.topology = {
                dirs: dirs,
                totalLOC: countLines('.'),
                workflows: fs.existsSync('.github/workflows') ? fs.readdirSync('.github/workflows').filter(f => f.endsWith('.yml') || f.endsWith('.yaml')) : [],
                languages: this.detectLanguages(),
                hotspots: this.findHotspots()
            };
        } catch (error) {
            console.error('⚠️ Error learning topology:', error.message);
            this.context.topology = { error: error.message };
        }
    }

    detectLanguages() {
        const langFiles = {
            'JavaScript': safeExec('find . -name "*.js" | grep -v node_modules | wc -l'),
            'Python': safeExec('find . -name "*.py" | wc -l'),
            'C#': safeExec('find . -name "*.cs" | wc -l'),
            'Markdown': safeExec('find . -name "*.md" | wc -l'),
            'YAML': safeExec('find . -name "*.yml" -o -name "*.yaml" | wc -l'),
            'Shell': safeExec('find . -name "*.sh" | wc -l')
        };

        return Object.fromEntries(
            Object.entries(langFiles)
                .map(([lang, count]) => [lang, parseInt(count) || 0])
                .filter(([lang, count]) => count > 0)
        );
    }

    findHotspots() {
        // Find frequently modified files using git
        try {
            const gitLog = safeExec('git log --name-only --pretty="" | sort | uniq -c | sort -nr | head -10');
            return gitLog.split('\n').filter(Boolean).map(line => {
                const parts = line.trim().split(/\s+/);
                return { file: parts[1], changes: parseInt(parts[0]) };
            });
        } catch {
            return [];
        }
    }

    learnDocumentation() {
        console.log('📚 Learning documentation structure...');
        
        this.context.docs = {
            readme: !!read('README.md'),
            contributing: !!read('CONTRIBUTING.md'),
            license: !!read('LICENSE'),
            changelog: !!read('CHANGELOG.md') || !!read('CHANGELOG'),
            tldlIndex: !!read('TLDL/index.md'),
            tldlEntries: fs.existsSync('TLDL/entries') ? fs.readdirSync('TLDL/entries').length : 0,
            docsDir: fs.existsSync('docs') ? fs.readdirSync('docs').filter(f => f.endsWith('.md')).length : 0,
            manifestos: fs.existsSync('MANIFESTO.md'),
            copilotInstructions: !!read('.github/copilot-instructions.md')
        };
    }

    learnPipelines() {
        console.log('⚙️ Learning CI/CD pipelines...');
        
        this.context.pipelines = {
            hasGithubWorkflows: fs.existsSync('.github/workflows'),
            workflowCount: fs.existsSync('.github/workflows') ? fs.readdirSync('.github/workflows').length : 0,
            hasChronicleKeeper: fs.existsSync('scripts/chronicle-keeper'),
            hasValidation: fs.existsSync('scripts/validate_setup.sh') || fs.existsSync('src/SymbolicLinter'),
            issueTemplates: fs.existsSync('.github/ISSUE_TEMPLATE') ? fs.readdirSync('.github/ISSUE_TEMPLATE').length : 0
        };
    }

    learnConfigurations() {
        console.log('⚙️ Learning project configurations...');
        
        this.context.configs = {
            packageJson: !!read('package.json'),
            mcpConfig: !!read('mcp-config.json'),
            agentProfile: !!read('.agent-profile.yaml') || !!read('agent-profile.yaml'),
            devTimeTravel: !!read('docs/devtimetravel_snapshot.yaml'),
            flags: !!read('flags.yaml'),
            gitignore: !!read('.gitignore'),
            editorConfig: !!read('.editorconfig'),
            vscodeSetting: fs.existsSync('.vscode')
        };
    }

    learnSignals() {
        console.log('🔍 Learning development signals...');
        
        const todoPatterns = ['TODO', 'FIXME', 'HACK', 'BUG', 'RITUAL', 'TLDL:'];
        const signals = {};
        
        todoPatterns.forEach(pattern => {
            const results = rg(pattern, 'src scripts docs TLDL').split('\n').filter(Boolean);
            if (results.length > 0) {
                signals[pattern.toLowerCase()] = results.slice(0, 20); // Limit to prevent overflow
            }
        });

        this.context.signals = {
            markers: signals,
            testCoverage: fs.existsSync('coverage') || fs.existsSync('.coverage'),
            hasTests: fs.existsSync('tests') || fs.existsSync('test') || 
                     safeExec('find . -name "*test*" -o -name "*spec*" | grep -v node_modules | wc -l') !== '0'
        };
    }

    async learn() {
        console.log('🎓 CID Schoolhouse beginning repository analysis...');
        
        this.learnTopology();
        this.learnDocumentation();
        this.learnPipelines();
        this.learnConfigurations();
        this.learnSignals();
        
        console.log('📦 Context learning complete');
        return this.context;
    }

    saveContext(outputPath) {
        fs.writeFileSync(outputPath, JSON.stringify(this.context, null, 2));
        console.log(`📦 Context saved to ${outputPath}`);
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const outputPath = args.find(arg => arg.startsWith('--out='))?.split('=')[1] || 'out/cid/context.json';
    
    async function main() {
        const learner = new RepositoryLearner();
        const context = await learner.learn();
        
        // Ensure output directory exists
        const outputDir = path.dirname(outputPath);
        if (!fs.existsSync(outputDir)) {
            fs.mkdirSync(outputDir, { recursive: true });
        }
        
        learner.saveContext(outputPath);
        
        console.log('📊 Learning Statistics:');
        console.log(`- Repository directories: ${context.topology.dirs?.length || 0}`);
        console.log(`- Total lines of code: ${context.topology.totalLOC || 0}`);
        console.log(`- Documentation files: ${context.docs.docsDir || 0} + TLDL entries: ${context.docs.tldlEntries || 0}`);
        console.log(`- Workflows: ${context.pipelines.workflowCount || 0}`);
        console.log(`- Development signals found: ${Object.keys(context.signals.markers || {}).length}`);
    }
    
    main().catch(error => {
        console.error('❌ Repository learning failed:', error.message);
        process.exit(1);
    });
}

module.exports = RepositoryLearner;