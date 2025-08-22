#!/usr/bin/env node
/**
 * CID Schoolhouse - Build Learning Syllabus
 * 
 * Parses GitHub issue to extract scope, goals, and constraints for the learning session.
 * Creates syllabus configuration that guides the repository analysis.
 */

const fs = require('fs');
const path = require('path');

class SyllabusBuilder {
    constructor() {
        this.syllabus = {
            scope: 'repo-wide',
            timeBudget: '10-15 min',
            fileFences: ['.git/', 'node_modules/', '.idea/', '.vs/', 'bin/', 'obj/', '.DS_Store'],
            goals: [],
            constraints: {},
            outputFormat: {
                executiveTLDL: true,
                findings: true,
                critique: true,
                proposals: true,
                autoIssues: false,
                badges: true
            }
        };
    }

    parseIssueBody(issueBody) {
        if (!issueBody) return this.getDefaultSyllabus();

        console.log('📋 Parsing issue syllabus requirements...');

        // Parse scope
        const scopeMatch = issueBody.match(/- \[x\] Repo-wide/i);
        if (scopeMatch) {
            this.syllabus.scope = 'repo-wide';
        }

        const specificAreaMatch = issueBody.match(/- \[x\] Specific area: (.+)/);
        if (specificAreaMatch) {
            this.syllabus.scope = 'specific';
            this.syllabus.specificArea = specificAreaMatch[1].trim();
        }

        const recentActivityMatch = issueBody.match(/- \[x\] Recent activity: (.+)/);
        if (recentActivityMatch) {
            this.syllabus.scope = 'recent';
            this.syllabus.recentActivity = recentActivityMatch[1].trim();
        }

        // Parse time budget
        const timeBudgetMatch = issueBody.match(/\*\*Time budget:\*\* (.+)/);
        if (timeBudgetMatch) {
            this.syllabus.timeBudget = timeBudgetMatch[1].trim();
        }

        // Parse file fences
        const fileFencesMatch = issueBody.match(/\*\*File fences:\*\* (.+)/);
        if (fileFencesMatch) {
            const additionalFences = fileFencesMatch[1].split(',').map(f => f.trim());
            this.syllabus.fileFences.push(...additionalFences);
        }

        // Parse primary questions
        const questionsMatch = issueBody.match(/\*\*Primary questions:\*\* (.+)/);
        if (questionsMatch) {
            this.syllabus.goals.push(questionsMatch[1].trim());
        }

        // Parse desired outcomes
        const outcomesMatch = issueBody.match(/\*\*Desired outcomes:\*\* (.+)/);
        if (outcomesMatch) {
            this.syllabus.goals.push(outcomesMatch[1].trim());
        }

        // Parse output format checkboxes
        this.syllabus.outputFormat.autoIssues = issueBody.includes('[x] Auto-issues');
        
        return this.syllabus;
    }

    getDefaultSyllabus() {
        console.log('📋 Using default syllabus configuration...');
        
        this.syllabus.goals = [
            'Identify automation opportunities',
            'Assess documentation completeness',
            'Review CI/CD pipeline health',
            'Find security and quality gaps'
        ];
        
        return this.syllabus;
    }

    generateSyllabus(issueNumber, issueBody) {
        console.log(`🎓 Building syllabus for issue #${issueNumber}...`);
        
        const syllabus = this.parseIssueBody(issueBody);
        
        // Add metadata
        syllabus.issueNumber = issueNumber;
        syllabus.timestamp = new Date().toISOString();
        syllabus.version = '1.0.0';
        
        console.log(`📚 Syllabus built - Scope: ${syllabus.scope}, Goals: ${syllabus.goals.length}`);
        return syllabus;
    }

    saveSyllabus(syllabus, outputPath) {
        const outputDir = path.dirname(outputPath);
        if (!fs.existsSync(outputDir)) {
            fs.mkdirSync(outputDir, { recursive: true });
        }
        
        fs.writeFileSync(outputPath, JSON.stringify(syllabus, null, 2));
        console.log(`📚 Syllabus saved to ${outputPath}`);
        return syllabus;
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const issueNumber = args[0] || '0';
    const outputPath = 'out/cid/syllabus.json';
    
    // For now, create a default syllabus since we don't have access to the actual issue body in this context
    // In the real workflow, this would get the issue body from the GitHub event
    const builder = new SyllabusBuilder();
    const syllabus = builder.generateSyllabus(issueNumber, '');
    builder.saveSyllabus(syllabus, outputPath);
}

module.exports = SyllabusBuilder;