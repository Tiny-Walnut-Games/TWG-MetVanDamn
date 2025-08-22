#!/usr/bin/env node
/**
 * CID Schoolhouse - TLDL Entry Generator
 * 
 * Converts CID Schoolhouse report into TLDL entry format that can be processed
 * by the existing Chronicle Keeper TLDL writer system.
 */

const fs = require('fs');
const path = require('path');

class TLDLGenerator {
    constructor() {
        this.quotes = [
            "The Archive is not a place — it's a posture.",
            "When in doubt, validate. When validated, doubt the validation.", 
            "The best commits are like good jokes—brief, clear, and slightly dangerous.",
            "In great workflows, the tools serve the developer, not the other way around."
        ];
    }

    generateFromReport(report) {
        const timestamp = new Date().toISOString().split('T')[0];
        const entryId = `TLDL-${timestamp}-CIDSchoolhouseAnalysis`;
        
        const filename = `${entryId}.md`;
        
        const content = this.buildTLDLContent(report, entryId);
        
        const metadata = {
            tags: ['cid-schoolhouse', 'analysis', 'automation'],
            complexity: 'Medium',
            impact: 'High',
            teamMembers: ['@cid-schoolhouse'],
            duration: 'Automated analysis',
            relatedEpic: 'Repository Enhancement',
            created: new Date().toISOString(),
            lastUpdated: new Date().toISOString(),
            status: 'Complete'
        };

        return {
            filename,
            content,
            metadata
        };
    }

    buildTLDLContent(report, entryId) {
        const randomQuote = this.quotes[Math.floor(Math.random() * this.quotes.length)];
        
        let content = `# ${entryId}\n\n`;
        content += `**Entry ID:** ${entryId}  \n`;
        content += `**Author:** @cid-schoolhouse  \n`;
        content += `**Context:** Repository-wide analysis and critique  \n`;
        content += `**Summary:** 🎓 CID Schoolhouse completed comprehensive repository analysis  \n\n`;
        content += `---\n\n`;
        content += `> *"${randomQuote}"* — **Secret Art of the Living Dev**\n\n`;
        content += `---\n\n`;

        // Discoveries section
        content += `## Discoveries\n\n`;
        if (report.findings && report.findings.length > 0) {
            report.findings.forEach((finding, index) => {
                content += `### ${finding.title}\n`;
                content += `- **Key Finding**: ${finding.description}\n`;
                content += `- **Impact**: ${this.capitalizeFirst(finding.impact)}\n`;
                if (finding.evidence) {
                    content += `- **Evidence**: ${finding.evidence}\n`;
                }
                content += `- **Pattern Recognition**: ${this.categoryIcon(finding.category)} ${this.capitalizeCategory(finding.category)} enhancement opportunity\n\n`;
            });
        }

        // Actions Taken section
        content += `## Actions Taken\n\n`;
        content += `1. **Repository Context Learning**\n`;
        content += `   - **What**: Analyzed repository structure, documentation, pipelines, and configurations\n`;
        content += `   - **Why**: To identify strengths, gaps, and enhancement opportunities\n`;
        content += `   - **How**: Automated scanning of files, directories, and development signals\n`;
        content += `   - **Result**: Comprehensive understanding of repository health and automation potential\n\n`;

        content += `2. **Gap Analysis and Critique**\n`;
        content += `   - **What**: Identified ${report.stats.gaps} areas needing improvement\n`;
        content += `   - **Why**: To surface risks and missing components\n`;
        content += `   - **How**: Pattern matching against best practices and common project needs\n`;
        content += `   - **Result**: Prioritized list of enhancement opportunities\n\n`;

        content += `3. **Proposal Generation**\n`;
        content += `   - **What**: Created ${report.stats.proposals} actionable improvement proposals\n`;
        content += `   - **Why**: To provide concrete next steps for repository enhancement\n`;
        content += `   - **How**: Effort/impact analysis with specific file recommendations\n`;
        content += `   - **Result**: Roadmap for systematic repository improvement\n\n`;

        // Technical Details section
        content += `## Technical Details\n\n`;
        content += `### Analysis Results\n`;
        content += `- **Findings**: ${report.stats.findings} positive observations\n`;
        content += `- **Gaps**: ${report.stats.gaps} improvement areas identified\n`;
        content += `- **Proposals**: ${report.stats.proposals} enhancement recommendations\n`;
        if (report.badges && report.badges.length > 0) {
            content += `- **Badges Earned**: ${report.badges.join(', ')}\n`;
        }
        content += `\n`;

        // Lessons Learned section
        content += `## Lessons Learned\n\n`;
        content += `### What Worked Well\n`;
        content += `- CID Schoolhouse successfully analyzed repository structure and patterns\n`;
        content += `- Automated context learning provided comprehensive project overview\n`;
        content += `- Integration with Chronicle Keeper TLDL system preserved analysis lineage\n\n`;

        content += `### What Could Be Improved\n`;
        content += `- Analysis scope could be expanded with issue-specific constraints\n`;
        content += `- Proposal prioritization could include team capacity considerations\n`;
        content += `- Badge system could reflect more nuanced project characteristics\n\n`;

        content += `### Knowledge Gaps Identified\n`;
        if (report.gaps && report.gaps.length > 0) {
            const highRiskGaps = report.gaps.filter(g => g.risk === 'high');
            if (highRiskGaps.length > 0) {
                content += `- High-risk gaps requiring immediate attention: ${highRiskGaps.length}\n`;
            }
            content += `- Primary improvement categories: ${[...new Set(report.gaps.map(g => g.category))].join(', ')}\n`;
        }
        content += `- Long-term maintenance strategies for sustained repository health\n\n`;

        // Next Steps section
        content += `## Next Steps\n\n`;
        content += `### Immediate Actions (High Priority)\n`;
        if (report.proposals && report.proposals.length > 0) {
            const highImpactProposals = report.proposals.filter(p => p.impact === 'high').slice(0, 3);
            highImpactProposals.forEach(proposal => {
                content += `- [ ] ${proposal.title}: ${proposal.description}\n`;
            });
        }
        content += `\n`;

        content += `### Medium-term Actions (Medium Priority)\n`;
        if (report.proposals && report.proposals.length > 0) {
            const mediumProposals = report.proposals.filter(p => p.impact === 'medium').slice(0, 3);
            mediumProposals.forEach(proposal => {
                content += `- [ ] ${proposal.title}\n`;
            });
        }
        content += `\n`;

        content += `### Long-term Considerations (Low Priority)\n`;
        content += `- [ ] Schedule regular CID Schoolhouse analysis sessions\n`;
        content += `- [ ] Track proposal implementation and measure impact\n`;
        content += `- [ ] Expand analysis scope based on project evolution\n\n`;

        // References section
        content += `## References\n\n`;
        content += `- [CID Schoolhouse Issue](https://github.com/jmeyer1980/living-dev-agent/issues/cid-schoolhouse)\n`;
        content += `- Repository Analysis Context Pack\n`;
        content += `- Chronicle Keeper TLDL System\n\n`;

        // Metadata footer
        content += `---\n\n`;
        content += `## TLDL Metadata\n\n`;
        content += `**Tags**: #cid-schoolhouse #analysis #automation  \n`;
        content += `**Complexity**: Medium  \n`;
        content += `**Impact**: High  \n`;
        content += `**Team Members**: @cid-schoolhouse  \n`;
        content += `**Duration**: Automated analysis  \n`;
        content += `**Related Epic**: Repository Enhancement  \n\n`;
        content += `---\n\n`;
        content += `**Created**: ${new Date().toISOString()}  \n`;
        content += `**Last Updated**: ${new Date().toISOString()}  \n`;
        content += `**Status**: Complete  \n\n`;
        content += `*Generated by CID Schoolhouse - Learning and critique for repository enhancement.*\n\n`;

        return content;
    }

    categoryIcon(category) {
        const icons = {
            'documentation': '📚',
            'automation': '🤖', 
            'ci_cd': '⚙️',
            'security': '🔒',
            'quality': '✅',
            'developer_experience': '👨‍💻',
            'process': '🔄'
        };
        return icons[category] || '📋';
    }

    capitalizeCategory(category) {
        return category
            .split('_')
            .map(word => word.charAt(0).toUpperCase() + word.slice(1))
            .join(' ');
    }

    capitalizeFirst(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const reportFile = args.find(arg => arg.startsWith('--report='))?.split('=')[1] || 'out/cid/report.json';
    const outputFile = args.find(arg => arg.startsWith('--out='))?.split('=')[1] || '/tmp/scroll-entry.json';

    try {
        const report = JSON.parse(fs.readFileSync(reportFile, 'utf8'));
        const generator = new TLDLGenerator();
        const tldlEntry = generator.generateFromReport(report);

        fs.writeFileSync(outputFile, JSON.stringify(tldlEntry, null, 2));
        console.log(`📜 TLDL entry generated: ${outputFile}`);
        
    } catch (error) {
        console.error('❌ TLDL generation failed:', error.message);
        process.exit(1);
    }
}

module.exports = TLDLGenerator;