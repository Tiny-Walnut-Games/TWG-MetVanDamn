#!/usr/bin/env node
/**
 * CID Faculty - Enhanced TLDL Generator
 * 
 * Generates TLDL entries with faculty-specific sections:
 * - "Present Wisdom" for Advisor insights
 * - "Future Sight" for Oracle forecasts
 */

const fs = require('fs');
const path = require('path');

class FacultyTLDLGenerator {
    constructor() {
        this.facultyQuotes = {
            advisor: [
                "The present is the only classroom where wisdom truly teaches.",
                "Good advice is like good code—specific, actionable, and tested.",
                "Current reality is the foundation upon which all futures are built.",
                "The wise advisor speaks in priorities, not possibilities."
            ],
            oracle: [
                "The future is not predicted, it's prepared for through present choices.",
                "Every scenario is a lesson waiting to be learned.",
                "Strategic foresight is the art of seeing consequences before they arrive.",
                "In the branches of possibility, the prepared mind finds certainty."
            ],
            combined: [
                "Wisdom speaks of the present; vision reveals the future.",
                "The greatest strategies bridge current reality with future possibility.",
                "Today's actions write tomorrow's history.",
                "In the marriage of present insight and future vision lies true guidance."
            ]
        };
    }

    generateFromFacultyReports(advisorReport, oracleReport, issueNumber) {
        const timestamp = new Date().toISOString().split('T')[0];
        const facultyTypes = [];
        
        if (advisorReport) facultyTypes.push('Advisor');
        if (oracleReport) facultyTypes.push('Oracle');
        
        const facultyTag = facultyTypes.join('');
        const entryId = `TLDL-${timestamp}-CIDFaculty${facultyTag}Consultation`;
        
        const content = this.buildFacultyTLDLContent(advisorReport, oracleReport, entryId, issueNumber);
        
        const metadata = {
            tags: ['cid-faculty', 'analysis', 'strategy', 'consultation'],
            complexity: this.assessComplexity(advisorReport, oracleReport),
            impact: 'High',
            teamMembers: ['@cid-faculty', '@cid-advisor', '@cid-oracle'].filter((_, i) => 
                (i === 0) || (i === 1 && advisorReport) || (i === 2 && oracleReport)
            ),
            duration: 'Faculty consultation',
            relatedEpic: 'Strategic Development',
            created: new Date().toISOString(),
            lastUpdated: new Date().toISOString(),
            status: 'Complete',
            facultyRoles: facultyTypes
        };

        return {
            filename: `${entryId}.md`,
            content,
            metadata
        };
    }

    buildFacultyTLDLContent(advisorReport, oracleReport, entryId, issueNumber) {
        const hasAdvisor = !!advisorReport;
        const hasOracle = !!oracleReport;
        const quoteCategory = hasAdvisor && hasOracle ? 'combined' : hasAdvisor ? 'advisor' : 'oracle';
        const randomQuote = this.facultyQuotes[quoteCategory][
            Math.floor(Math.random() * this.facultyQuotes[quoteCategory].length)
        ];
        
        let content = `# ${entryId}\n\n`;
        content += `**Entry ID:** ${entryId}  \n`;
        content += `**Author:** @cid-faculty  \n`;
        content += `**Context:** Faculty consultation for Issue #${issueNumber}  \n`;
        content += `**Summary:** 🎓📜 CID Faculty provided `;
        
        if (hasAdvisor && hasOracle) {
            content += `comprehensive guidance through Advisor present-state analysis and Oracle strategic forecasting`;
        } else if (hasAdvisor) {
            content += `grounded guidance through Advisor present-state analysis and prioritized recommendations`;
        } else {
            content += `strategic foresight through Oracle scenario forecasting and future planning`;
        }
        content += `  \n\n`;
        
        content += `---\n\n`;
        content += `> *"${randomQuote}"* — **Faculty Codex, Living Dev Agent**\n\n`;
        content += `---\n\n`;

        // Faculty Consultation Results
        content += `## Faculty Consultation Results\n\n`;
        
        if (hasAdvisor) {
            content += this.buildAdvisorSection(advisorReport);
        }
        
        if (hasOracle) {
            content += this.buildOracleSection(oracleReport);
        }

        // Combined Discoveries  
        content += `## Discoveries\n\n`;
        content += this.buildCombinedDiscoveries(advisorReport, oracleReport);

        // Actions Taken
        content += `## Actions Taken\n\n`;
        content += this.buildActionsSection(advisorReport, oracleReport);

        // Next Steps (combining both faculty insights)
        content += `## Next Steps\n\n`;
        content += this.buildCombinedNextSteps(advisorReport, oracleReport);

        // Technical Details
        content += `## Technical Details\n\n`;
        content += this.buildTechnicalDetails(advisorReport, oracleReport);

        // Lessons Learned
        content += `## Lessons Learned\n\n`;
        content += this.buildLessonsLearned(advisorReport, oracleReport);

        // References
        content += `## References\n\n`;
        content += `### Internal Links\n`;
        content += `- [Faculty Consultation Issue #${issueNumber}](https://github.com/jmeyer1980/living-dev-agent/issues/${issueNumber})\n`;
        content += `- [TLDL Index](../index.md)\n`;
        content += `- [CID Faculty System](../../scripts/cid-faculty/)\n\n`;
        
        content += `### Faculty Resources\n`;
        content += `- The Advisor: Grounded guidance and present-state analysis\n`;
        content += `- The Oracle: Strategic forecasting and scenario planning\n`;
        content += `- Smart Usage Meter: Resource management and budget control\n\n`;

        // TLDL Metadata
        content += `---\n\n`;
        content += `## TLDL Metadata\n\n`;
        content += `**Tags**: #cid-faculty #consultation #strategy #analysis  \n`;
        content += `**Complexity**: ${this.assessComplexity(advisorReport, oracleReport)}  \n`;
        content += `**Impact**: High  \n`;
        content += `**Team Members**: @cid-faculty  \n`;
        content += `**Duration**: Faculty consultation session  \n`;
        content += `**Related Epic**: Strategic Development & Process Enhancement  \n\n`;
        content += `---\n\n`;
        content += `**Created**: ${new Date().toISOString()}  \n`;
        content += `**Last Updated**: ${new Date().toISOString()}  \n`;
        content += `**Status**: Complete  \n\n`;

        // Add telemetry from reports if available
        if ((advisorReport?.telemetry || oracleReport?.telemetry)) {
            content += `### Usage Telemetry\n\n`;
            if (advisorReport?.telemetry) {
                content += `**Advisor**: ${this.extractTelemetrySummary(advisorReport.telemetry)}\n`;
            }
            if (oracleReport?.telemetry) {
                content += `**Oracle**: ${this.extractTelemetrySummary(oracleReport.telemetry)}\n`;
            }
            content += `\n`;
        }

        content += `*Generated by CID Faculty - Advisor + Oracle consultation system*\n\n`;

        return content;
    }

    buildAdvisorSection(report) {
        let content = `### Present Wisdom - The Advisor\n\n`;
        content += `> ${report.summary.join(' ')}\n\n`;
        
        if (report.actionItems && report.actionItems.length > 0) {
            content += `#### Prioritized Action Items\n\n`;
            report.actionItems.forEach((item, index) => {
                content += `**${index + 1}. ${item.title}** (${item.impact} impact, ${item.effort} effort)\n`;
                content += `- **Description**: ${item.description}\n`;
                content += `- **Evidence**: ${item.evidence}\n`;
                content += `- **Category**: ${item.category}\n`;
                content += `- **Priority Score**: ${item.priority}\n\n`;
            });
        }

        return content;
    }

    buildOracleSection(report) {
        let content = `### Future Sight - The Oracle\n\n`;
        content += `> ${report.summary.join(' ')}\n\n`;
        
        if (report.scenarios && report.scenarios.length > 0) {
            content += `#### Strategic Scenarios\n\n`;
            report.scenarios.forEach((scenario, index) => {
                content += `**${index + 1}. ${scenario.name}** (${(scenario.probability * 100).toFixed(0)}% probability)\n`;
                content += `- **Timeline**: ${scenario.timeframe}\n`;
                content += `- **Description**: ${scenario.description}\n`;
                
                if (scenario.prerequisites && scenario.prerequisites.length > 0) {
                    content += `- **Prerequisites**: ${scenario.prerequisites.slice(0, 3).join(', ')}\n`;
                }
                
                if (scenario.risks && scenario.risks.length > 0) {
                    content += `- **Key Risks**: ${scenario.risks.slice(0, 2).join(', ')}\n`;
                }
                
                if (scenario.outcomes && scenario.outcomes.length > 0) {
                    content += `- **Expected Outcomes**: ${scenario.outcomes.slice(0, 2).join(', ')}\n`;
                }
                content += `\n`;
            });
        }

        return content;
    }

    buildCombinedDiscoveries(advisorReport, oracleReport) {
        let content = '';

        if (advisorReport) {
            content += `### Current State Analysis (Advisor)\n`;
            const categories = advisorReport.metadata?.categories || [];
            const quickWins = advisorReport.metadata?.quickWins || 0;
            const highPriority = advisorReport.metadata?.highPriority || 0;

            content += `- **Analysis Scope**: ${categories.length} categories analyzed (${categories.join(', ')})\n`;
            content += `- **Priority Items**: ${highPriority} high-priority improvements identified\n`;
            content += `- **Quick Wins**: ${quickWins} low-effort, high-impact opportunities available\n`;
            content += `- **Evidence-Based**: All recommendations include specific evidence and impact assessment\n\n`;
        }

        if (oracleReport) {
            content += `### Strategic Forecast (Oracle)\n`;
            const scenarioCount = oracleReport.metadata?.scenarioCount || 0;
            const highProbScenarios = oracleReport.metadata?.highProbability || 0;
            const horizon = oracleReport.metadata?.forecastHorizon || 6;

            content += `- **Forecast Horizon**: ${horizon}-month strategic outlook\n`;
            content += `- **Scenario Analysis**: ${scenarioCount} possible futures evaluated\n`;
            content += `- **High-Probability Paths**: ${highProbScenarios} scenarios with >60% likelihood\n`;
            content += `- **Risk Assessment**: Each scenario includes prerequisite and risk analysis\n\n`;
        }

        return content;
    }

    buildActionsSection(advisorReport, oracleReport) {
        let content = '';

        content += `1. **Faculty Consultation Executed**\n`;
        content += `   - **What**: `;
        if (advisorReport && oracleReport) {
            content += `Dual faculty analysis combining present-state audit and strategic forecasting\n`;
        } else if (advisorReport) {
            content += `Advisor consultation for present-state audit and prioritized guidance\n`;
        } else {
            content += `Oracle consultation for strategic forecasting and scenario planning\n`;
        }
        content += `   - **Why**: Provide evidence-based recommendations and strategic direction\n`;
        content += `   - **How**: Automated analysis with smart usage meter and context caching\n`;
        content += `   - **Result**: Comprehensive consultation with actionable outcomes\n\n`;

        if (advisorReport) {
            content += `2. **Present-State Audit (Advisor)**\n`;
            content += `   - **What**: Repository health assessment and improvement prioritization\n`;
            content += `   - **Why**: Identify immediate opportunities and quick wins\n`;
            content += `   - **How**: Evidence-based analysis across ${advisorReport.metadata?.categories?.length || 'multiple'} categories\n`;
            content += `   - **Result**: ${advisorReport.actionItems?.length || 0} prioritized action items with effort/impact ratings\n\n`;
        }

        if (oracleReport) {
            content += `3. **Strategic Forecasting (Oracle)**\n`;
            content += `   - **What**: Multi-scenario future planning and risk assessment\n`;
            content += `   - **Why**: Enable proactive strategic decision making\n`;
            content += `   - **How**: Probability-weighted scenario analysis with prerequisite mapping\n`;
            content += `   - **Result**: ${oracleReport.scenarios?.length || 0} strategic pathways with leading indicators\n\n`;
        }

        return content;
    }

    buildCombinedNextSteps(advisorReport, oracleReport) {
        let content = '';

        if (advisorReport && advisorReport.actionItems) {
            content += `### Immediate Actions (Advisor Recommendations)\n`;
            const highPriorityItems = advisorReport.actionItems
                .filter(item => item.priority >= 80)
                .slice(0, 3);
            
            highPriorityItems.forEach(item => {
                content += `- [ ] **${item.title}**: ${item.description} (${item.effort} effort)\n`;
            });
            content += `\n`;

            const quickWins = advisorReport.actionItems
                .filter(item => item.effort === 'low' && item.impact !== 'low')
                .slice(0, 3);
            
            if (quickWins.length > 0) {
                content += `### Quick Wins (High ROI)\n`;
                quickWins.forEach(item => {
                    content += `- [ ] **${item.title}**: ${item.description}\n`;
                });
                content += `\n`;
            }
        }

        if (oracleReport && oracleReport.scenarios) {
            content += `### Strategic Preparation (Oracle Guidance)\n`;
            const topScenario = oracleReport.scenarios[0];
            if (topScenario) {
                content += `- [ ] **Prepare for ${topScenario.name}**: Focus on ${topScenario.prerequisites?.slice(0, 2).join(' and ') || 'key prerequisites'}\n`;
                content += `- [ ] **Monitor Leading Indicators**: Track ${topScenario.leadingIndicators?.slice(0, 2).join(' and ') || 'progress signals'}\n`;
                content += `- [ ] **Risk Mitigation**: Address ${topScenario.risks?.slice(0, 1).join('') || 'primary risks'}\n\n`;
            }
        }

        content += `### Long-term Considerations\n`;
        content += `- [ ] Schedule quarterly faculty consultations for strategic alignment\n`;
        content += `- [ ] Implement progress tracking for recommended actions\n`;
        content += `- [ ] Review and adjust strategic scenarios based on outcomes\n\n`;

        return content;
    }

    buildTechnicalDetails(advisorReport, oracleReport) {
        let content = '';

        if (advisorReport || oracleReport) {
            content += `### Faculty Analysis Metrics\n\n`;
            
            if (advisorReport) {
                content += `**Advisor Results:**\n`;
                content += `- Action Items: ${advisorReport.actionItems?.length || 0}\n`;
                content += `- Categories Analyzed: ${advisorReport.metadata?.categories?.length || 0}\n`;
                content += `- High Priority Items: ${advisorReport.metadata?.highPriority || 0}\n`;
                content += `- Quick Wins Identified: ${advisorReport.metadata?.quickWins || 0}\n\n`;
            }
            
            if (oracleReport) {
                content += `**Oracle Results:**\n`;
                content += `- Scenarios Generated: ${oracleReport.scenarios?.length || 0}\n`;
                content += `- Forecast Horizon: ${oracleReport.metadata?.forecastHorizon || 6} months\n`;
                content += `- High-Probability Scenarios: ${oracleReport.metadata?.highProbability || 0}\n`;
                content += `- Transformative Pathways: ${oracleReport.metadata?.transformativeScenarios || 0}\n\n`;
            }
        }

        return content;
    }

    buildLessonsLearned(advisorReport, oracleReport) {
        let content = '';

        content += `### What Worked Well\n`;
        content += `- Faculty consultation system provided structured analysis approach\n`;
        content += `- Smart usage meter enabled efficient resource management\n`;
        content += `- Context caching reduced processing overhead for iterative analysis\n`;
        
        if (advisorReport && oracleReport) {
            content += `- Combined Advisor + Oracle perspective balanced immediate needs with strategic vision\n`;
        }
        content += `\n`;

        content += `### What Could Be Improved\n`;
        content += `- Faculty analysis could benefit from stakeholder priority input\n`;
        content += `- Scenario modeling could include more quantitative risk assessment\n`;
        content += `- Integration with project management tools for action tracking\n\n`;

        content += `### Knowledge Gaps Identified\n`;
        content += `- Long-term ROI measurement for implemented recommendations\n`;
        content += `- Cross-team coordination patterns for strategic initiatives\n`;
        content += `- Automated progress tracking for faculty-recommended actions\n\n`;

        return content;
    }

    extractTelemetrySummary(telemetryText) {
        // Extract key metrics from telemetry footer
        const timeMatch = telemetryText.match(/Time.*?(\d+\.\d+)m.*?(\d+\.\d+)%/);
        const callsMatch = telemetryText.match(/GitHub (\d+)\/(\d+).*?Other (\d+)\/(\d+)/);
        const cacheMatch = telemetryText.match(/Cache.*?(\d+\.\d+)%/);
        const budgetWise = telemetryText.includes('Budget-Wise');

        let summary = '';
        if (timeMatch) {
            summary += `${timeMatch[1]}m (${timeMatch[2]}% budget)`;
        }
        if (callsMatch) {
            summary += `, API: ${callsMatch[1]}/${callsMatch[2]} GH + ${callsMatch[3]}/${callsMatch[4]} other`;
        }
        if (cacheMatch) {
            summary += `, Cache: ${cacheMatch[1]}%`;
        }
        if (budgetWise) {
            summary += `, 🏆 Budget-Wise`;
        }

        return summary || 'Telemetry data available';
    }

    assessComplexity(advisorReport, oracleReport) {
        let complexity = 'Medium';
        
        const totalItems = (advisorReport?.actionItems?.length || 0) + (oracleReport?.scenarios?.length || 0);
        
        if (totalItems <= 3) {
            complexity = 'Low';
        } else if (totalItems >= 8) {
            complexity = 'High';
        }
        
        // Boost complexity if both faculty consulted
        if (advisorReport && oracleReport) {
            complexity = complexity === 'Low' ? 'Medium' : 'High';
        }
        
        return complexity;
    }
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const advisorFile = args.find(arg => arg.startsWith('--advisor='))?.split('=')[1];
    const oracleFile = args.find(arg => arg.startsWith('--oracle='))?.split('=')[1];
    const issueNumber = args.find(arg => arg.startsWith('--issue='))?.split('=')[1] || '0';
    const outputFile = args.find(arg => arg.startsWith('--out='))?.split('=')[1] || '/tmp/faculty-tldl-entry.json';
    
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
            
            const generator = new FacultyTLDLGenerator();
            const tldlEntry = generator.generateFromFacultyReports(advisorReport, oracleReport, issueNumber);
            
            const output = {
                type: 'faculty-consultation',
                category: 'faculty-consultation',
                lore_worthy: true,
                parsing_timestamp: new Date().toISOString(),
                ...tldlEntry
            };
            
            fs.writeFileSync(outputFile, JSON.stringify(output, null, 2));
            console.log(`📜 Faculty TLDL entry generated: ${outputFile}`);
            
        } catch (error) {
            console.error(`❌ Faculty TLDL generation error:`, error.message);
            process.exit(1);
        }
    }
    
    main();
}

module.exports = { FacultyTLDLGenerator };